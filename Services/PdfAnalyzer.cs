using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;

namespace bot_fileorganizer.Services
{
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
        /// Verifica se um arquivo PDF é um e-book
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for um e-book, False caso contrário</returns>
        public bool IsEbook(string filePath)
        {
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Verifica o número de páginas (e-books geralmente têm várias páginas)
                    if (document.GetNumberOfPages() < 5)
                    {
                        return false;
                    }

                    // Extrai texto das primeiras páginas para buscar palavras-chave
                    string text = ExtractTextFromPages(document, 1, 5);
                    
                    // Verifica se o texto contém palavras-chave que indicam que é um livro
                    foreach (string keyword in _ebookKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    // Verifica se o nome do arquivo contém indicações de que é um livro
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("livro") || fileName.Contains("book") || 
                        fileName.StartsWith("livro - ") || fileName.Contains("ebook"))
                    {
                        return true;
                    }

                    // Se o documento tiver muitas páginas, provavelmente é um e-book
                    if (document.GetNumberOfPages() > 30)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar se o PDF é um e-book: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é um artigo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for um artigo, False caso contrário</returns>
        public bool IsArticle(string filePath)
        {
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Artigos geralmente têm poucas páginas
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount > 30)
                    {
                        return false;
                    }

                    // Extrai texto das primeiras páginas para buscar palavras-chave
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 3));
                    
                    // Verifica se o texto contém palavras-chave que indicam que é um artigo
                    foreach (string keyword in _articleKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    // Verifica se o nome do arquivo contém indicações de que é um artigo
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("artigo") || fileName.Contains("article"))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar se o PDF é um artigo: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é um paper científico
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for um paper científico, False caso contrário</returns>
        public bool IsScientificPaper(string filePath)
        {
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Papers científicos geralmente têm entre 5 e 30 páginas
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount < 3 || pageCount > 50)
                    {
                        return false;
                    }

                    // Extrai texto das primeiras páginas para buscar palavras-chave
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 5));
                    
                    // Verifica se o texto contém palavras-chave que indicam que é um paper científico
                    foreach (string keyword in _scientificPaperKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    // Verifica padrões comuns em papers científicos
                    if (text.Contains("abstract", StringComparison.OrdinalIgnoreCase) && 
                        text.Contains("introduction", StringComparison.OrdinalIgnoreCase) &&
                        text.Contains("conclusion", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // Verifica se o nome do arquivo contém indicações de que é um paper científico
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("paper") || fileName.Contains("research") || 
                        fileName.Contains("study") || fileName.Contains("scientific"))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar se o PDF é um paper científico: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é um jornal
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for um jornal, False caso contrário</returns>
        public bool IsNewspaper(string filePath)
        {
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Jornais geralmente têm muitas páginas
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount < 4)
                    {
                        return false;
                    }

                    // Extrai texto das primeiras páginas para buscar palavras-chave
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 3));
                    
                    // Verifica se o texto contém palavras-chave que indicam que é um jornal
                    foreach (string keyword in _newspaperKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    // Verifica padrões comuns em jornais como datas no formato de jornal
                    if (Regex.IsMatch(text, @"\b\d{1,2}\s+de\s+[a-zA-Z]+\s+de\s+\d{4}\b") || // Data em português
                        Regex.IsMatch(text, @"\b[a-zA-Z]+\s+\d{1,2},\s+\d{4}\b")) // Data em inglês
                    {
                        return true;
                    }

                    // Verifica se o nome do arquivo contém indicações de que é um jornal
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("jornal") || fileName.Contains("newspaper") || 
                        fileName.Contains("gazette") || fileName.Contains("daily"))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar se o PDF é um jornal: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é uma revista
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for uma revista, False caso contrário</returns>
        public bool IsMagazine(string filePath)
        {
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Revistas geralmente têm entre 20 e 200 páginas
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount < 10 || pageCount > 300)
                    {
                        return false;
                    }

                    // Extrai texto das primeiras páginas para buscar palavras-chave
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 5));
                    
                    // Verifica se o texto contém palavras-chave que indicam que é uma revista
                    foreach (string keyword in _magazineKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    // Verifica padrões específicos de revistas como "Vol. X, No. Y" ou "Issue Z"
                    if (Regex.IsMatch(text, @"vol\.\s*\d+", RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(text, @"issue\s*\d+", RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(text, @"no\.\s*\d+", RegexOptions.IgnoreCase))
                    {
                        return true;
                    }

                    // Verifica se o nome do arquivo contém indicações de que é uma revista
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("magazine") || fileName.Contains("revista") || 
                        fileName.Contains("time") || fileName.Contains("veja"))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar se o PDF é uma revista: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Identifica o tipo de documento PDF
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Tipo de documento identificado</returns>
        public string IdentifyDocumentType(string filePath)
        {
            if (IsEbook(filePath)) return "E-book";
            if (IsMagazine(filePath)) return "Revista";
            if (IsArticle(filePath)) return "Artigo";
            if (IsScientificPaper(filePath)) return "Paper Científico";
            if (IsNewspaper(filePath)) return "Jornal";
            return "Documento PDF";
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
