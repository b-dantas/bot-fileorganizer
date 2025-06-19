using bot_fileorganizer.Data;
using bot_fileorganizer.Models;

namespace bot_fileorganizer.Services
{
    /// <summary>
    /// Serviço principal para organização de arquivos
    /// </summary>
    public class FileOrganizerService
    {
        private readonly FileRepository _repository;
        private readonly PdfAnalyzer _pdfAnalyzer;
        private string _currentDirectory = string.Empty;

        /// <summary>
        /// Inicializa uma nova instância do serviço de organização de arquivos
        /// </summary>
        /// <param name="repository">Repositório para persistência de dados</param>
        /// <param name="pdfAnalyzer">Analisador de PDFs</param>
        public FileOrganizerService(FileRepository repository, PdfAnalyzer pdfAnalyzer)
        {
            _repository = repository;
            _pdfAnalyzer = pdfAnalyzer;
        }

        /// <summary>
        /// Define o diretório atual para análise
        /// </summary>
        /// <param name="directoryPath">Caminho do diretório</param>
        /// <returns>True se o diretório existir, False caso contrário</returns>
        public bool SetDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                _currentDirectory = directoryPath;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Obtém o diretório atual
        /// </summary>
        /// <returns>Caminho do diretório atual</returns>
        public string GetCurrentDirectory()
        {
            return _currentDirectory;
        }

        /// <summary>
        /// Lista todos os arquivos PDF no diretório atual
        /// </summary>
        /// <returns>Lista de caminhos de arquivos PDF</returns>
        public List<string> ListPdfFiles()
        {
            if (string.IsNullOrEmpty(_currentDirectory) || !Directory.Exists(_currentDirectory))
            {
                return new List<string>();
            }

            return Directory.GetFiles(_currentDirectory, "*.pdf", SearchOption.TopDirectoryOnly).ToList();
        }

        /// <summary>
        /// Verifica se um arquivo segue o padrão de nomenclatura
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>True se o arquivo seguir o padrão, False caso contrário</returns>
        public bool IsFileNameStandardized(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Verifica se o nome segue o padrão "Livro - Autor - Título"
            string[] parts = fileName.Split('-');
            if (parts.Length < 3)
            {
                return false;
            }

            string prefix = parts[0].Trim();
            if (!prefix.Equals("Livro", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Verifica se autor e título não estão vazios
            if (string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Lista arquivos PDF que não seguem o padrão de nomenclatura
        /// </summary>
        /// <returns>Lista de caminhos de arquivos não padronizados</returns>
        public List<string> ListNonStandardizedFiles()
        {
            List<string> allPdfFiles = ListPdfFiles();
            return allPdfFiles.Where(file => !IsFileNameStandardized(file)).ToList();
        }

        /// <summary>
        /// Gera uma proposta de novo nome para um arquivo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>Proposta de novo nome</returns>
        public string GenerateProposedName(string filePath)
        {
            string? title = _pdfAnalyzer.ExtractTitle(filePath);
            string? author = _pdfAnalyzer.ExtractAuthor(filePath);
            
            // Se não conseguir extrair título ou autor, usa valores padrão
            title ??= "Desconhecido";
            author ??= "Desconhecido";
            
            // Gera o novo nome no formato "Livro - Autor - Título.pdf"
            string newName = $"Livro - {author} - {title}.pdf";
            
            // Substitui caracteres inválidos para nomes de arquivo no Windows
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                newName = newName.Replace(c, '_');
            }
            
            // Limita o tamanho do nome para evitar problemas com caminhos muito longos
            if (newName.Length > 240) // Deixa margem para o caminho
            {
                newName = newName.Substring(0, 240) + ".pdf";
            }
            
            return newName;
        }

        /// <summary>
        /// Cria um registro para um arquivo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>Registro do arquivo</returns>
        public FileRecord CreateFileRecord(string filePath)
        {
            string originalName = Path.GetFileName(filePath);
            string proposedName = GenerateProposedName(filePath);
            
            return new FileRecord
            {
                FilePath = filePath,
                OriginalName = originalName,
                ProposedName = proposedName,
                Accepted = false,
                OperationDate = DateTime.Now,
                ExtractedTitle = _pdfAnalyzer.ExtractTitle(filePath),
                ExtractedAuthor = _pdfAnalyzer.ExtractAuthor(filePath),
                IsEbook = _pdfAnalyzer.IsEbook(filePath)
            };
        }

        /// <summary>
        /// Renomeia um arquivo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <param name="newName">Novo nome</param>
        /// <returns>True se o arquivo for renomeado com sucesso, False caso contrário</returns>
        public bool RenameFile(string filePath, string newName)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }
                
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                string newPath = Path.Combine(directory, newName);
                
                // Verifica se já existe um arquivo com o novo nome
                if (File.Exists(newPath))
                {
                    return false;
                }
                
                File.Move(filePath, newPath);
                
                // Atualiza o registro no repositório
                var record = _repository.GetRecordByPath(filePath);
                if (record != null)
                {
                    record.FilePath = newPath;
                    record.Accepted = true;
                    _repository.UpdateRecord(record);
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Processa um arquivo, criando um registro e salvando no repositório
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>Registro do arquivo</returns>
        public FileRecord ProcessFile(string filePath)
        {
            var record = CreateFileRecord(filePath);
            _repository.AddRecord(record);
            return record;
        }

        /// <summary>
        /// Processa vários arquivos, criando registros e salvando no repositório
        /// </summary>
        /// <param name="filePaths">Lista de caminhos de arquivos</param>
        /// <returns>Lista de registros de arquivos</returns>
        public List<FileRecord> ProcessFiles(List<string> filePaths)
        {
            var records = new List<FileRecord>();
            
            foreach (var filePath in filePaths)
            {
                records.Add(CreateFileRecord(filePath));
            }
            
            _repository.AddRecords(records);
            return records;
        }
    }
}
