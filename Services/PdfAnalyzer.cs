namespace bot_fileorganizer.Services
{
    /// <summary>
    /// Serviço para análise de arquivos PDF
    /// </summary>
    public class PdfAnalyzer
    {
        /// <summary>
        /// Extrai o título de um arquivo PDF
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Título extraído ou null se não for possível extrair</returns>
        public string? ExtractTitle(string filePath)
        {
            // TODO: Implementar extração real usando biblioteca PDF
            // Por enquanto, retorna uma simulação baseada no nome do arquivo
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // Tenta extrair o título do nome do arquivo
                string[] parts = fileName.Split('-');
                if (parts.Length >= 3)
                {
                    return parts[2].Trim();
                }
                
                return null;
            }
            catch
            {
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
            // TODO: Implementar extração real usando biblioteca PDF
            // Por enquanto, retorna uma simulação baseada no nome do arquivo
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // Tenta extrair o autor do nome do arquivo
                string[] parts = fileName.Split('-');
                if (parts.Length >= 2)
                {
                    return parts[1].Trim();
                }
                
                return null;
            }
            catch
            {
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
            // TODO: Implementar verificação real usando biblioteca PDF
            // Por enquanto, assume que todos os PDFs são e-books
            try
            {
                string extension = Path.GetExtension(filePath);
                return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
