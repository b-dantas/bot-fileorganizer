using System.Text.Json;
using bot_fileorganizer.Models;

namespace bot_fileorganizer.Data
{
    /// <summary>
    /// Repositório para persistência de dados em JSON
    /// </summary>
    public class FileRepository
    {
        private readonly string _dataFilePath;
        private List<FileRecord> _fileRecords;

        /// <summary>
        /// Inicializa uma nova instância do repositório
        /// </summary>
        /// <param name="dataFilePath">Caminho para o arquivo JSON de persistência</param>
        public FileRepository(string dataFilePath = "filerecords.json")
        {
            _dataFilePath = dataFilePath;
            _fileRecords = new List<FileRecord>();
            LoadData();
        }

        /// <summary>
        /// Carrega os dados do arquivo JSON
        /// </summary>
        private void LoadData()
        {
            if (File.Exists(_dataFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(_dataFilePath);
                    var records = JsonSerializer.Deserialize<List<FileRecord>>(jsonContent);
                    if (records != null)
                    {
                        _fileRecords = records;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao carregar dados: {ex.Message}");
                    _fileRecords = new List<FileRecord>();
                }
            }
        }

        /// <summary>
        /// Salva os dados no arquivo JSON
        /// </summary>
        public void SaveData()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string jsonContent = JsonSerializer.Serialize(_fileRecords, options);
                File.WriteAllText(_dataFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar dados: {ex.Message}");
            }
        }

        /// <summary>
        /// Adiciona um novo registro
        /// </summary>
        /// <param name="record">Registro a ser adicionado</param>
        public void AddRecord(FileRecord record)
        {
            _fileRecords.Add(record);
            SaveData();
        }

        /// <summary>
        /// Adiciona vários registros
        /// </summary>
        /// <param name="records">Registros a serem adicionados</param>
        public void AddRecords(IEnumerable<FileRecord> records)
        {
            _fileRecords.AddRange(records);
            SaveData();
        }

        /// <summary>
        /// Atualiza um registro existente
        /// </summary>
        /// <param name="record">Registro atualizado</param>
        public void UpdateRecord(FileRecord record)
        {
            var existingRecord = _fileRecords.FirstOrDefault(r => r.FilePath == record.FilePath);
            if (existingRecord != null)
            {
                int index = _fileRecords.IndexOf(existingRecord);
                _fileRecords[index] = record;
                SaveData();
            }
        }

        /// <summary>
        /// Obtém todos os registros
        /// </summary>
        /// <returns>Lista de registros</returns>
        public List<FileRecord> GetAllRecords()
        {
            return _fileRecords.ToList();
        }

        /// <summary>
        /// Obtém o histórico de um arquivo específico
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>Registro do arquivo, se existir</returns>
        public FileRecord? GetRecordByPath(string filePath)
        {
            return _fileRecords.FirstOrDefault(r => r.FilePath == filePath);
        }
    }
}
