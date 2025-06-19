using System.Text.Json;
using bot_fileorganizer.Models;

namespace bot_fileorganizer.Data
{
    /// <summary>
    /// Repositório para persistência das configurações do aplicativo
    /// </summary>
    public class AppSettingsRepository
    {
        private readonly string _settingsFilePath;
        private AppSettings _settings;

        /// <summary>
        /// Inicializa uma nova instância do repositório de configurações
        /// </summary>
        /// <param name="settingsFilePath">Caminho para o arquivo JSON de configurações</param>
        public AppSettingsRepository(string settingsFilePath = "appsettings.json")
        {
            _settingsFilePath = settingsFilePath;
            _settings = new AppSettings();
            LoadSettings();
        }

        /// <summary>
        /// Carrega as configurações do arquivo JSON
        /// </summary>
        private void LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(jsonContent);
                    if (settings != null)
                    {
                        _settings = settings;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao carregar configurações: {ex.Message}");
                    _settings = new AppSettings();
                }
            }
        }

        /// <summary>
        /// Salva as configurações no arquivo JSON
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string jsonContent = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar configurações: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtém o último diretório utilizado
        /// </summary>
        /// <returns>Caminho do último diretório utilizado</returns>
        public string GetLastDirectory()
        {
            return _settings.LastDirectory;
        }

        /// <summary>
        /// Define o último diretório utilizado
        /// </summary>
        /// <param name="directory">Caminho do diretório</param>
        public void SetLastDirectory(string directory)
        {
            _settings.LastDirectory = directory;
            SaveSettings();
        }

        /// <summary>
        /// Obtém as configurações do aplicativo
        /// </summary>
        /// <returns>Configurações do aplicativo</returns>
        public AppSettings GetSettings()
        {
            return _settings;
        }

        /// <summary>
        /// Define as configurações do aplicativo
        /// </summary>
        /// <param name="settings">Configurações do aplicativo</param>
        public void SetSettings(AppSettings settings)
        {
            _settings = settings;
            SaveSettings();
        }
    }
}
