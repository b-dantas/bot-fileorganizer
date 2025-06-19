namespace bot_fileorganizer.Models
{
    /// <summary>
    /// Configurações do aplicativo
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Último diretório utilizado pelo usuário
        /// </summary>
        public string LastDirectory { get; set; } = string.Empty;
    }
}
