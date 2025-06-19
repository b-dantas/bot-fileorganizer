using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;

namespace bot_fileorganizer.Services
{
    /// <summary>
    /// Representa o resultado da detecção de tipo de documento com percentual de confiança
    /// </summary>
    public class DocumentTypeResult
    {
        /// <summary>
        /// Tipo de documento detectado
        /// </summary>
        public string DocumentType { get; set; }
        
        /// <summary>
        /// Percentual de confiança na detecção (0-100%)
        /// </summary>
        public double Confidence { get; set; }
        
        public DocumentTypeResult(string documentType, double confidence)
        {
            DocumentType = documentType;
            Confidence = Math.Clamp(confidence, 0, 100);
        }
    }
    
    /// <summary>
    /// Serviço para análise de arquivos PDF
    /// </summary>
    public class PdfAnalyzer
    {
        // Palavras-chave que indicam que um documento é um e-book em diferentes idiomas
        private readonly string[] _ebookKeywords = new[] { 
            "livro", "book", "capítulo", "chapter", "autor", "author",
            "editora", "publisher", "isbn", "edição", "edition",
            "copyright", "todos os direitos reservados", "all rights reserved"
        };
        
        // Palavras-chave que indicam que um documento é um artigo
        private readonly string[] _articleKeywords = new[] {
            "artigo", "article", "revista", "magazine", "journal", "publicação", "publication"
        };

        // Palavras-chave que indicam que um documento é um paper científico
        private readonly string[] _scientificPaperKeywords = new[] {
            "paper", "research", "pesquisa", "abstract", "resumo", "methodology", "metodologia",
            "conclusion", "conclusão", "references", "referências", "doi", "peer-reviewed"
        };

        // Palavras-chave que indicam que um documento é um jornal
        private readonly string[] _newspaperKeywords = new[] {
            "jornal", "newspaper", "notícia", "news", "reportagem", "report", "editorial",
            "manchete", "headline", "daily", "diário", "weekly", "semanal"
        };
        
        // Palavras-chave que indicam que um documento é uma revista
        private readonly string[] _magazineKeywords = new[] {
            "revista", "magazine", "time", "veja", "newsweek", "edition", "edição",
            "volume", "issue", "número", "mensal", "monthly", "semanal", "weekly",
            "editorial", "editor", "coluna", "column", "feature", "reportagem"
        };

        /// <summary>
        /// Extrai o título de um arquivo PDF
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Título extraído ou null se não for possível extrair</returns>
        public string? ExtractTitle(string filePath)
        {
            try
            {
                // Tenta extrair o título dos metadados do PDF
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Verifica se há metadados disponíveis
                    var info = document.GetDocumentInfo();
                    if (info != null && !string.IsNullOrWhiteSpace(info.GetTitle()))
                    {
                        return info.GetTitle();
                    }

                    // Se não encontrar nos metadados, tenta extrair da primeira página
                    string firstPageText = ExtractTextFromPage(document, 1);
                    if (!string.IsNullOrWhiteSpace(firstPageText))
                    {
                        // Tenta encontrar um título na primeira página
                        // Geralmente está nas primeiras linhas
                        string[] lines = firstPageText.Split('\n');
                        for (int i = 0; i < Math.Min(10, lines.Length); i++)
                        {
                            string line = lines[i].Trim();
                            if (line.Length > 5 && line.Length < 100 && !line.Contains("©") && !line.Contains("Copyright"))
                            {
                                return line;
                            }
                        }
                    }
                }

                // Se não conseguir extrair do PDF, tenta extrair do nome do arquivo
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string[] parts = fileName.Split('-');
                if (parts.Length >= 3)
                {
                    return parts[2].Trim();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao extrair título do PDF: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extrai o autor de um arquivo PDF
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Autor extraído ou null se não for possível extrair</returns>
        public string? ExtractAuthor(string filePath)
        {
            try
            {
                // Tenta extrair o autor dos metadados do PDF
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Verifica se há metadados disponíveis
                    var info = document.GetDocumentInfo();
                    if (info != null && !string.IsNullOrWhiteSpace(info.GetAuthor()))
                    {
                        return info.GetAuthor();
                    }

                    // Se não encontrar nos metadados, tenta extrair das primeiras páginas
                    string firstPagesText = ExtractTextFromPages(document, 1, 3);
                    if (!string.IsNullOrWhiteSpace(firstPagesText))
                    {
                        // Procura por padrões comuns que indicam autoria
                        string[] authorPatterns = new[] { 
                            "autor:", "author:", "by:", "por:", "escrito por", "written by" 
                        };
                        
                        foreach (var pattern in authorPatterns)
                        {
                            int index = firstPagesText.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                            if (index >= 0)
                            {
                                // Extrai o texto após o padrão até a próxima quebra de linha
                                int startIndex = index + pattern.Length;
                                int endIndex = firstPagesText.IndexOf('\n', startIndex);
                                if (endIndex > startIndex)
                                {
                                    string author = firstPagesText.Substring(startIndex, endIndex - startIndex).Trim();
                                    if (!string.IsNullOrWhiteSpace(author) && author.Length < 100)
                                    {
                                        return author;
                                    }
                                }
                            }
                        }
                    }
                }

                // Se não conseguir extrair do PDF, tenta extrair do nome do arquivo
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string[] parts = fileName.Split('-');
                if (parts.Length >= 2)
                {
                    return parts[1].Trim();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao extrair autor do PDF: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calcula o percentual de confiança de que um arquivo PDF é um e-book
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Percentual de confiança (0-100%)</returns>
        public double CalculateEbookConfidence(string filePath)
        {
            try
            {
                double confidence = 0;
                
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Fator 1: Número de páginas
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount < 5)
                    {
                        confidence -= 30; // Penalidade para documentos muito curtos
                    }
                    else if (pageCount > 30)
                    {
                        confidence += 20; // Bônus para documentos longos
                    }
                    
                    // Fator 2: Palavras-chave encontradas
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 5));
                    int keywordsFound = 0;
                    
                    foreach (string keyword in _ebookKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            keywordsFound++;
                        }
                    }
                    
                    // Adiciona pontos com base na proporção de palavras-chave encontradas
                    confidence += (keywordsFound * 100.0 / _ebookKeywords.Length) * 0.4;
                    
                    // Fator 3: Nome do arquivo
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("livro") || fileName.Contains("book"))
                    {
                        confidence += 15;
                    }
                    if (fileName.StartsWith("livro - ") || fileName.Contains("ebook"))
                    {
                        confidence += 25;
                    }
                }
                
                return Math.Clamp(confidence, 0, 100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao calcular confiança de e-book: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é um e-book
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for um e-book, False caso contrário</returns>
        public bool IsEbook(string filePath)
        {
            return CalculateEbookConfidence(filePath) >= 40; // Limiar de confiança para considerar como e-book
        }
        
        /// <summary>
        /// Calcula o percentual de confiança de que um arquivo PDF é um artigo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Percentual de confiança (0-100%)</returns>
        public double CalculateArticleConfidence(string filePath)
        {
            try
            {
                double confidence = 0;
                
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Fator 1: Número de páginas
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount > 30)
                    {
                        confidence -= 30; // Penalidade para documentos muito longos
                    }
                    else if (pageCount >= 3 && pageCount <= 15)
                    {
                        confidence += 20; // Bônus para documentos de tamanho típico de artigos
                    }
                    
                    // Fator 2: Palavras-chave encontradas
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 3));
                    int keywordsFound = 0;
                    
                    foreach (string keyword in _articleKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            keywordsFound++;
                        }
                    }
                    
                    // Adiciona pontos com base na proporção de palavras-chave encontradas
                    confidence += (keywordsFound * 100.0 / _articleKeywords.Length) * 0.5;
                    
                    // Fator 3: Nome do arquivo
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("artigo") || fileName.Contains("article"))
                    {
                        confidence += 30;
                    }
                }
                
                return Math.Clamp(confidence, 0, 100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao calcular confiança de artigo: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é um artigo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for um artigo, False caso contrário</returns>
        public bool IsArticle(string filePath)
        {
            return CalculateArticleConfidence(filePath) >= 40; // Limiar de confiança para considerar como artigo
        }
        
        /// <summary>
        /// Calcula o percentual de confiança de que um arquivo PDF é um paper científico
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Percentual de confiança (0-100%)</returns>
        public double CalculateScientificPaperConfidence(string filePath)
        {
            try
            {
                double confidence = 0;
                
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Fator 1: Número de páginas
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount < 3 || pageCount > 50)
                    {
                        confidence -= 20; // Penalidade para documentos muito curtos ou muito longos
                    }
                    else if (pageCount >= 5 && pageCount <= 30)
                    {
                        confidence += 15; // Bônus para documentos de tamanho típico de papers
                    }
                    
                    // Fator 2: Palavras-chave encontradas
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 5));
                    int keywordsFound = 0;
                    
                    foreach (string keyword in _scientificPaperKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            keywordsFound++;
                        }
                    }
                    
                    // Adiciona pontos com base na proporção de palavras-chave encontradas
                    confidence += (keywordsFound * 100.0 / _scientificPaperKeywords.Length) * 0.4;
                    
                    // Fator 3: Estrutura típica de papers científicos
                    if (text.Contains("abstract", StringComparison.OrdinalIgnoreCase))
                    {
                        confidence += 15;
                    }
                    if (text.Contains("introduction", StringComparison.OrdinalIgnoreCase))
                    {
                        confidence += 10;
                    }
                    if (text.Contains("conclusion", StringComparison.OrdinalIgnoreCase))
                    {
                        confidence += 10;
                    }
                    if (text.Contains("references", StringComparison.OrdinalIgnoreCase) || 
                        text.Contains("referências", StringComparison.OrdinalIgnoreCase))
                    {
                        confidence += 15;
                    }
                    
                    // Fator 4: Nome do arquivo
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("paper") || fileName.Contains("research") || 
                        fileName.Contains("study") || fileName.Contains("scientific"))
                    {
                        confidence += 20;
                    }
                }
                
                return Math.Clamp(confidence, 0, 100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao calcular confiança de paper científico: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é um paper científico
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for um paper científico, False caso contrário</returns>
        public bool IsScientificPaper(string filePath)
        {
            return CalculateScientificPaperConfidence(filePath) >= 40; // Limiar de confiança para considerar como paper científico
        }
        
        /// <summary>
        /// Calcula o percentual de confiança de que um arquivo PDF é um jornal
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Percentual de confiança (0-100%)</returns>
        public double CalculateNewspaperConfidence(string filePath)
        {
            try
            {
                double confidence = 0;
                
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Fator 1: Número de páginas
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount < 4)
                    {
                        confidence -= 30; // Penalidade para documentos muito curtos
                    }
                    else if (pageCount >= 8)
                    {
                        confidence += 15; // Bônus para documentos mais longos
                    }
                    
                    // Fator 2: Palavras-chave encontradas
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 3));
                    int keywordsFound = 0;
                    
                    foreach (string keyword in _newspaperKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            keywordsFound++;
                        }
                    }
                    
                    // Adiciona pontos com base na proporção de palavras-chave encontradas
                    confidence += (keywordsFound * 100.0 / _newspaperKeywords.Length) * 0.4;
                    
                    // Fator 3: Padrões de data típicos de jornais
                    if (Regex.IsMatch(text, @"\b\d{1,2}\s+de\s+[a-zA-Z]+\s+de\s+\d{4}\b")) // Data em português
                    {
                        confidence += 25;
                    }
                    if (Regex.IsMatch(text, @"\b[a-zA-Z]+\s+\d{1,2},\s+\d{4}\b")) // Data em inglês
                    {
                        confidence += 25;
                    }
                    
                    // Fator 4: Nome do arquivo
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("jornal") || fileName.Contains("newspaper") || 
                        fileName.Contains("gazette") || fileName.Contains("daily"))
                    {
                        confidence += 30;
                    }
                }
                
                return Math.Clamp(confidence, 0, 100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao calcular confiança de jornal: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é um jornal
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for um jornal, False caso contrário</returns>
        public bool IsNewspaper(string filePath)
        {
            return CalculateNewspaperConfidence(filePath) >= 40; // Limiar de confiança para considerar como jornal
        }
        
        /// <summary>
        /// Calcula o percentual de confiança de que um arquivo PDF é uma revista
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Percentual de confiança (0-100%)</returns>
        public double CalculateMagazineConfidence(string filePath)
        {
            try
            {
                double confidence = 0;
                
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Fator 1: Número de páginas
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount < 10 || pageCount > 300)
                    {
                        confidence -= 20; // Penalidade para documentos fora do intervalo típico
                    }
                    else if (pageCount >= 20 && pageCount <= 200)
                    {
                        confidence += 20; // Bônus para documentos de tamanho típico de revistas
                    }
                    
                    // Fator 2: Palavras-chave encontradas
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 5));
                    int keywordsFound = 0;
                    
                    foreach (string keyword in _magazineKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            keywordsFound++;
                        }
                    }
                    
                    // Adiciona pontos com base na proporção de palavras-chave encontradas
                    confidence += (keywordsFound * 100.0 / _magazineKeywords.Length) * 0.4;
                    
                    // Fator 3: Padrões específicos de revistas
                    if (Regex.IsMatch(text, @"vol\.\s*\d+", RegexOptions.IgnoreCase))
                    {
                        confidence += 15;
                    }
                    if (Regex.IsMatch(text, @"issue\s*\d+", RegexOptions.IgnoreCase))
                    {
                        confidence += 15;
                    }
                    if (Regex.IsMatch(text, @"no\.\s*\d+", RegexOptions.IgnoreCase))
                    {
                        confidence += 15;
                    }
                    
                    // Fator 4: Nome do arquivo
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("magazine") || fileName.Contains("revista") || 
                        fileName.Contains("time") || fileName.Contains("veja"))
                    {
                        confidence += 25;
                    }
                }
                
                return Math.Clamp(confidence, 0, 100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao calcular confiança de revista: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é uma revista
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for uma revista, False caso contrário</returns>
        public bool IsMagazine(string filePath)
        {
            return CalculateMagazineConfidence(filePath) >= 40; // Limiar de confiança para considerar como revista
        }
        
        /// <summary>
        /// Calcula o percentual de confiança para documento genérico
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Percentual de confiança (0-100%)</returns>
        public double CalculateGenericDocumentConfidence(string filePath)
        {
            // Para documentos genéricos, atribuímos uma confiança base baixa
            return 10;
        }
        
        /// <summary>
        /// Identifica o tipo de documento PDF com percentual de confiança
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Resultado contendo o tipo de documento e o percentual de confiança</returns>
        public DocumentTypeResult IdentifyDocumentTypeWithConfidence(string filePath)
        {
            var confidences = GetDocumentTypeConfidences(filePath);
            return confidences.OrderByDescending(c => c.Confidence).First();
        }
        
        /// <summary>
        /// Obtém todos os tipos de documento possíveis com seus percentuais de confiança
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Lista de resultados com tipos e percentuais de confiança</returns>
        public List<DocumentTypeResult> GetDocumentTypeConfidences(string filePath)
        {
            var results = new List<DocumentTypeResult>
            {
                new DocumentTypeResult("E-book", CalculateEbookConfidence(filePath)),
                new DocumentTypeResult("Revista", CalculateMagazineConfidence(filePath)),
                new DocumentTypeResult("Artigo", CalculateArticleConfidence(filePath)),
                new DocumentTypeResult("Paper Científico", CalculateScientificPaperConfidence(filePath)),
                new DocumentTypeResult("Jornal", CalculateNewspaperConfidence(filePath)),
                new DocumentTypeResult("Documento PDF", CalculateGenericDocumentConfidence(filePath))
            };
            
            return results;
        }
        
        /// <summary>
        /// Identifica o tipo de documento PDF (método de compatibilidade)
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Tipo de documento identificado</returns>
        public string IdentifyDocumentType(string filePath)
        {
            return IdentifyDocumentTypeWithConfidence(filePath).DocumentType;
        }

        /// <summary>
        /// Extrai texto de uma página específica do PDF
        /// </summary>
        /// <param name="document">Documento PDF</param>
        /// <param name="pageNumber">Número da página</param>
        /// <returns>Texto extraído da página</returns>
        private string ExtractTextFromPage(PdfDocument document, int pageNumber)
        {
            try
            {
                if (pageNumber <= 0 || pageNumber > document.GetNumberOfPages())
                {
                    return string.Empty;
                }

                var page = document.GetPage(pageNumber);
                var strategy = new SimpleTextExtractionStrategy();
                return PdfTextExtractor.GetTextFromPage(page, strategy);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao extrair texto da página {pageNumber}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extrai texto de um intervalo de páginas do PDF
        /// </summary>
        /// <param name="document">Documento PDF</param>
        /// <param name="startPage">Página inicial</param>
        /// <param name="endPage">Página final</param>
        /// <returns>Texto extraído das páginas</returns>
        private string ExtractTextFromPages(PdfDocument document, int startPage, int endPage)
        {
            try
            {
                var textBuilder = new System.Text.StringBuilder();
                
                int maxPage = Math.Min(endPage, document.GetNumberOfPages());
                for (int i = startPage; i <= maxPage; i++)
                {
                    string pageText = ExtractTextFromPage(document, i);
                    textBuilder.AppendLine(pageText);
                }
                
                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao extrair texto das páginas {startPage}-{endPage}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Atualiza os metadados de um arquivo PDF
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <param name="title">Novo título</param>
        /// <param name="author">Novo autor</param>
        /// <returns>True se os metadados forem atualizados com sucesso, False caso contrário</returns>
        public bool UpdateMetadata(string filePath, string title, string author)
        {
            try
            {
                // Obtém o diretório e o nome do arquivo original
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                
                // Cria um nome para o arquivo atualizado na mesma pasta
                string updatedFileName = $"{fileNameWithoutExt}_updated.pdf";
                string updatedFilePath = Path.Combine(directory, updatedFileName);
                
                // Abre o arquivo original para leitura e o novo para escrita
                using (var reader = new PdfReader(filePath))
                using (var writer = new PdfWriter(updatedFilePath))
                using (var document = new PdfDocument(reader, writer))
                {
                    // Obtém os metadados do documento
                    var info = document.GetDocumentInfo();
                    
                    // Atualiza os metadados
                    info.SetTitle(title);
                    info.SetAuthor(author);
                    
                    // Fecha o documento para aplicar as alterações
                    document.Close();
                }
                
                Console.WriteLine($"Arquivo com metadados atualizados salvo como: {updatedFileName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar metadados do PDF: {ex.Message}");
                return false;
            }
        }
    }
}
