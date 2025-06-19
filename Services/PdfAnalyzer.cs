using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

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
