namespace bot_fileorganizer.Models
{
    /// <summary>
    /// Representa um registro de arquivo com informações sobre o nome original e o novo nome proposto
    /// </summary>
    public class FileRecord
    {
        /// <summary>
        /// Caminho completo do arquivo
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Nome original do arquivo
        /// </summary>
        public string OriginalName { get; set; } = string.Empty;
        
        /// <summary>
        /// Novo nome proposto para o arquivo
        /// </summary>
        public string ProposedName { get; set; } = string.Empty;
        
        /// <summary>
        /// Indica se a proposta de renomeação foi aceita
        /// </summary>
        public bool Accepted { get; set; }
        
        /// <summary>
        /// Data e hora da operação
        /// </summary>
        public DateTime OperationDate { get; set; }
        
        /// <summary>
        /// Título extraído do PDF, se disponível
        /// </summary>
        public string? ExtractedTitle { get; set; }
        
        /// <summary>
        /// Autor extraído do PDF, se disponível
        /// </summary>
        public string? ExtractedAuthor { get; set; }
        
        /// <summary>
        /// Indica se o arquivo foi identificado como um e-book
        /// </summary>
        public bool IsEbook { get; set; }
    }
}
