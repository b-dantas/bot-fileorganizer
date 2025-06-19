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
        private readonly AppSettingsRepository _settingsRepository;
        private string _currentDirectory = string.Empty;
        
        /// <summary>
        /// Tamanho padrão do lote para processamento de arquivos
        /// </summary>
        public const int DefaultBatchSize = 10;

        /// <summary>
        /// Inicializa uma nova instância do serviço de organização de arquivos
        /// </summary>
        /// <param name="repository">Repositório para persistência de dados</param>
        /// <param name="pdfAnalyzer">Analisador de PDFs</param>
        /// <param name="settingsRepository">Repositório de configurações</param>
        public FileOrganizerService(FileRepository repository, PdfAnalyzer pdfAnalyzer, AppSettingsRepository settingsRepository)
        {
            _repository = repository;
            _pdfAnalyzer = pdfAnalyzer;
            _settingsRepository = settingsRepository;
            
            // Carrega o último diretório utilizado, se disponível
            string lastDirectory = _settingsRepository.GetLastDirectory();
            if (!string.IsNullOrEmpty(lastDirectory) && Directory.Exists(lastDirectory))
            {
                _currentDirectory = lastDirectory;
            }
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
                
                // Salva o diretório nas configurações
                _settingsRepository.SetLastDirectory(directoryPath);
                
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
            
            // Verifica se o nome segue o padrão "[Tipo] - Autor - Título"
            string[] parts = fileName.Split('-');
            if (parts.Length < 3)
            {
                return false;
            }

            string prefix = parts[0].Trim();
            // Verifica se o prefixo é um dos tipos válidos
            if (!IsValidDocumentTypePrefix(prefix))
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
        /// Verifica se o prefixo é um tipo de documento válido
        /// </summary>
        /// <param name="prefix">Prefixo a ser verificado</param>
        /// <returns>True se o prefixo for válido, False caso contrário</returns>
        private bool IsValidDocumentTypePrefix(string prefix)
        {
            string[] validPrefixes = { "Livro", "Revista", "Artigo", "Paper", "Jornal", "Documento" };
            return validPrefixes.Any(p => prefix.Equals(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Lista arquivos PDF que não seguem o padrão de nomenclatura
        /// </summary>
        /// <returns>Lista de caminhos de arquivos não padronizados</returns>
        public List<string> ListNonStandardizedFiles()
        {
            List<string> allPdfFiles = ListPdfFiles();
            
            // Obtém os arquivos já rejeitados
            var rejectedFiles = _repository.GetAllRecords()
                .Where(r => r.Rejected)
                .Select(r => r.FilePath)
                .ToList();
            
            // Filtra os arquivos não padronizados e que não foram rejeitados
            return allPdfFiles
                .Where(file => !IsFileNameStandardized(file) && !rejectedFiles.Contains(file))
                .ToList();
        }

        /// <summary>
        /// Gera uma proposta de novo nome para um arquivo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <param name="documentType">Tipo de documento (opcional)</param>
        /// <returns>Proposta de novo nome</returns>
        public string GenerateProposedName(string filePath, string? documentType = null)
        {
            string? title = _pdfAnalyzer.ExtractTitle(filePath);
            string? author = _pdfAnalyzer.ExtractAuthor(filePath);
            
            // Se não conseguir extrair título ou autor, usa valores padrão
            title ??= "Desconhecido";
            author ??= "Desconhecido";
            
            // Se o tipo de documento não for fornecido, identifica-o
            documentType ??= _pdfAnalyzer.IdentifyDocumentType(filePath);
            
            // Obtém o prefixo com base no tipo de documento
            string prefix = GetPrefixFromDocumentType(documentType);
            
            // Gera o novo nome no formato "[Tipo] - Autor - Título.pdf"
            string newName = $"{prefix} - {author} - {title}.pdf";
            
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
        /// Obtém o prefixo com base no tipo de documento
        /// </summary>
        /// <param name="documentType">Tipo de documento</param>
        /// <returns>Prefixo correspondente</returns>
        private string GetPrefixFromDocumentType(string documentType)
        {
            return documentType switch
            {
                "E-book" => "Livro",
                "Revista" => "Revista",
                "Artigo" => "Artigo",
                "Paper Científico" => "Paper",
                "Jornal" => "Jornal",
                _ => "Documento"
            };
        }

        /// <summary>
        /// Cria um registro para um arquivo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <param name="documentType">Tipo de documento (opcional)</param>
        /// <returns>Registro do arquivo</returns>
        public FileRecord CreateFileRecord(string filePath, string? documentType = null)
        {
            string originalName = Path.GetFileName(filePath);
            documentType ??= _pdfAnalyzer.IdentifyDocumentType(filePath);
            string proposedName = GenerateProposedName(filePath, documentType);
            
            return new FileRecord
            {
                FilePath = filePath,
                OriginalName = originalName,
                ProposedName = proposedName,
                Accepted = false,
                Rejected = false,
                OperationDate = DateTime.Now,
                ExtractedTitle = _pdfAnalyzer.ExtractTitle(filePath),
                ExtractedAuthor = _pdfAnalyzer.ExtractAuthor(filePath),
                IsEbook = _pdfAnalyzer.IsEbook(filePath),
                DocumentType = documentType,
                MetadataUpdateRejected = false
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
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao renomear arquivo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Marca um arquivo como rejeitado
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>True se o arquivo for marcado como rejeitado com sucesso, False caso contrário</returns>
        public bool RejectFile(string filePath)
        {
            try
            {
                var record = _repository.GetRecordByPath(filePath);
                
                if (record != null)
                {
                    record.Rejected = true;
                    _repository.UpdateRecord(record);
                }
                else
                {
                    // Se não existir um registro, cria um novo com a flag de rejeitado
                    var newRecord = CreateFileRecord(filePath);
                    newRecord.Rejected = true;
                    _repository.AddRecord(newRecord);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao marcar arquivo como rejeitado: {ex.Message}");
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
        /// <param name="documentTypes">Dicionário de tipos de documentos por caminho (opcional)</param>
        /// <returns>Lista de registros de arquivos</returns>
        public List<FileRecord> ProcessFiles(List<string> filePaths, Dictionary<string, string>? documentTypes = null)
        {
            var records = new List<FileRecord>();
            
            foreach (var filePath in filePaths)
            {
                string? documentType = null;
                if (documentTypes != null && documentTypes.ContainsKey(filePath))
                {
                    documentType = documentTypes[filePath];
                }
                records.Add(CreateFileRecord(filePath, documentType));
            }
            
            _repository.AddRecords(records);
            return records;
        }

        /// <summary>
        /// Processa arquivos em lotes
        /// </summary>
        /// <param name="filePaths">Lista de caminhos de arquivos</param>
        /// <param name="batchSize">Tamanho do lote</param>
        /// <returns>Lista de lotes de registros de arquivos</returns>
        public List<List<FileRecord>> ProcessFilesInBatches(List<string> filePaths, int batchSize = DefaultBatchSize)
        {
            var batches = new List<List<FileRecord>>();
            
            // Divide os arquivos em lotes
            for (int i = 0; i < filePaths.Count; i += batchSize)
            {
                var batchFiles = filePaths.Skip(i).Take(batchSize).ToList();
                var batchRecords = ProcessFiles(batchFiles);
                batches.Add(batchRecords);
            }
            
            return batches;
        }

        /// <summary>
        /// Obtém um lote específico de arquivos não padronizados
        /// </summary>
        /// <param name="batchIndex">Índice do lote (começando em 0)</param>
        /// <param name="batchSize">Tamanho do lote</param>
        /// <returns>Lista de caminhos de arquivos do lote</returns>
        public List<string> GetNonStandardizedFilesBatch(int batchIndex, int batchSize = DefaultBatchSize)
        {
            var allFiles = ListNonStandardizedFiles();
            
            // Verifica se o índice do lote é válido
            if (batchIndex < 0 || batchIndex >= Math.Ceiling((double)allFiles.Count / batchSize))
            {
                return new List<string>();
            }
            
            // Retorna o lote específico
            return allFiles.Skip(batchIndex * batchSize).Take(batchSize).ToList();
        }

        /// <summary>
        /// Obtém o número total de lotes de arquivos não padronizados
        /// </summary>
        /// <param name="batchSize">Tamanho do lote</param>
        /// <returns>Número total de lotes</returns>
        public int GetTotalBatches(int batchSize = DefaultBatchSize)
        {
            var allFiles = ListNonStandardizedFiles();
            return (int)Math.Ceiling((double)allFiles.Count / batchSize);
        }

        /// <summary>
        /// Lista arquivos PDF que seguem o padrão de nomenclatura
        /// </summary>
        /// <returns>Lista de caminhos de arquivos padronizados</returns>
        public List<string> ListStandardizedFiles()
        {
            List<string> allPdfFiles = ListPdfFiles();
            
            // Obtém os arquivos que já tiveram atualização de metadados rejeitada
            var rejectedMetadataUpdates = _repository.GetAllRecords()
                .Where(r => r.MetadataUpdateRejected)
                .Select(r => r.FilePath)
                .ToList();
            
            // Filtra os arquivos padronizados e que não tiveram atualização de metadados rejeitada
            return allPdfFiles
                .Where(file => IsFileNameStandardized(file) && !rejectedMetadataUpdates.Contains(file))
                .ToList();
        }

        /// <summary>
        /// Extrai título e autor do nome do arquivo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>Tupla contendo autor e título</returns>
        public (string Author, string Title) ExtractInfoFromFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string[] parts = fileName.Split('-');
            
            if (parts.Length >= 3 && IsValidDocumentTypePrefix(parts[0].Trim()))
            {
                string author = parts[1].Trim();
                string title = parts[2].Trim();
                
                // Se houver mais partes após o título, concatena-as
                if (parts.Length > 3)
                {
                    for (int i = 3; i < parts.Length; i++)
                    {
                        title += " - " + parts[i].Trim();
                    }
                }
                
                return (author, title);
            }
            
            // Se não conseguir extrair, retorna valores vazios
            return (string.Empty, string.Empty);
        }

        /// <summary>
        /// Extrai o tipo de documento do nome do arquivo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>Tipo de documento extraído ou string vazia se não for possível extrair</returns>
        public string ExtractDocumentTypeFromFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string[] parts = fileName.Split('-');
            
            if (parts.Length >= 3)
            {
                string prefix = parts[0].Trim();
                if (IsValidDocumentTypePrefix(prefix))
                {
                    return prefix switch
                    {
                        "Livro" => "E-book",
                        "Revista" => "Revista",
                        "Artigo" => "Artigo",
                        "Paper" => "Paper Científico",
                        "Jornal" => "Jornal",
                        _ => "Documento PDF"
                    };
                }
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Atualiza os metadados de um arquivo PDF com base no nome do arquivo
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>True se os metadados forem atualizados com sucesso, False caso contrário</returns>
        public bool UpdateFileMetadata(string filePath)
        {
            try
            {
                // Extrai título e autor do nome do arquivo
                var (author, title) = ExtractInfoFromFileName(filePath);
                
                if (string.IsNullOrEmpty(author) || string.IsNullOrEmpty(title))
                {
                    return false;
                }
                
                // Atualiza os metadados do arquivo
                bool success = _pdfAnalyzer.UpdateMetadata(filePath, title, author);
                
                if (success)
                {
                    // Cria um registro da operação
                    var record = new FileRecord
                    {
                        FilePath = filePath,
                        OriginalName = Path.GetFileName(filePath),
                        ProposedName = Path.GetFileName(filePath), // Mesmo nome, pois não estamos renomeando
                        Accepted = true,
                        Rejected = false,
                        OperationDate = DateTime.Now,
                        ExtractedTitle = title,
                        ExtractedAuthor = author,
                        IsEbook = _pdfAnalyzer.IsEbook(filePath),
                        DocumentType = _pdfAnalyzer.IdentifyDocumentType(filePath),
                        MetadataUpdateRejected = false
                    };
                    
                    _repository.AddRecord(record);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar metadados: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Marca um arquivo como rejeitado para atualização de metadados
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <returns>True se o arquivo for marcado como rejeitado com sucesso, False caso contrário</returns>
        public bool RejectMetadataUpdate(string filePath)
        {
            try
            {
                var record = _repository.GetRecordByPath(filePath);
                
                if (record != null)
                {
                    record.MetadataUpdateRejected = true;
                    _repository.UpdateRecord(record);
                }
                else
                {
                    // Se não existir um registro, cria um novo com a flag de rejeição de metadados
                    var newRecord = CreateFileRecord(filePath);
                    newRecord.MetadataUpdateRejected = true;
                    _repository.AddRecord(newRecord);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao marcar rejeição de atualização de metadados: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtém um lote específico de arquivos padronizados
        /// </summary>
        /// <param name="batchIndex">Índice do lote (começando em 0)</param>
        /// <param name="batchSize">Tamanho do lote</param>
        /// <returns>Lista de caminhos de arquivos do lote</returns>
        public List<string> GetStandardizedFilesBatch(int batchIndex, int batchSize = DefaultBatchSize)
        {
            var allFiles = ListStandardizedFiles();
            
            // Verifica se o índice do lote é válido
            if (batchIndex < 0 || batchIndex >= Math.Ceiling((double)allFiles.Count / batchSize))
            {
                return new List<string>();
            }
            
            // Retorna o lote específico
            return allFiles.Skip(batchIndex * batchSize).Take(batchSize).ToList();
        }

        /// <summary>
        /// Processa um arquivo, criando um registro e salvando no repositório
        /// </summary>
        /// <param name="filePath">Caminho do arquivo</param>
        /// <param name="documentType">Tipo de documento (opcional)</param>
        /// <returns>Registro do arquivo</returns>
        public FileRecord ProcessFile(string filePath, string? documentType = null)
        {
            var record = CreateFileRecord(filePath, documentType);
            _repository.AddRecord(record);
            return record;
        }

        /// <summary>
        /// Obtém o número total de lotes de arquivos padronizados
        /// </summary>
        /// <param name="batchSize">Tamanho do lote</param>
        /// <returns>Número total de lotes</returns>
        public int GetTotalStandardizedBatches(int batchSize = DefaultBatchSize)
        {
            var allFiles = ListStandardizedFiles();
            return (int)Math.Ceiling((double)allFiles.Count / batchSize);
        }
    }
}
