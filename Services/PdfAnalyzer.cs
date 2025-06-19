using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;

namespace bot_fileorganizer.Services
{
    /// <summary>
    /// Representa o resultado da extração de metadados com nível de confiança
    /// </summary>
    public class MetadataExtractionResult
    {
        /// <summary>
        /// Valor extraído
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Nível de confiança na extração (0-100%)
        /// </summary>
        public double Confidence { get; set; }
        
        /// <summary>
        /// Método usado para extrair o valor
        /// </summary>
        public string ExtractionMethod { get; set; }
        
        public MetadataExtractionResult(string value, double confidence, string extractionMethod)
        {
            Value = value;
            Confidence = Math.Clamp(confidence, 0, 100);
            ExtractionMethod = extractionMethod;
        }
    }
    
    /// <summary>
    /// Representa um autor extraído de um documento
    /// </summary>
    public class Author
    {
        /// <summary>
        /// Nome do autor
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Afiliação do autor (se disponível)
        /// </summary>
        public string? Affiliation { get; set; }
        
        /// <summary>
        /// Email do autor (se disponível)
        /// </summary>
        public string? Email { get; set; }
        
        public Author(string name, string? affiliation = null, string? email = null)
        {
            Name = name;
            Affiliation = affiliation;
            Email = email;
        }
        
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Affiliation) && !string.IsNullOrEmpty(Email))
            {
                return $"{Name} ({Affiliation}, {Email})";
            }
            else if (!string.IsNullOrEmpty(Affiliation))
            {
                return $"{Name} ({Affiliation})";
            }
            else if (!string.IsNullOrEmpty(Email))
            {
                return $"{Name} ({Email})";
            }
            else
            {
                return Name;
            }
        }
    }
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
        
        // Palavras-chave que indicam que um documento é uma apresentação
        private readonly string[] _presentationKeywords = new[] {
            "apresentação", "presentation", "slide", "slides", "powerpoint", "keynote",
            "palestra", "lecture", "conferência", "conference", "webinar", "workshop",
            "seminário", "seminar", "palestrante", "speaker", "audiência", "audience",
            "projetor", "projector", "datashow", "handout", "material de apoio"
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
                // Obtém o tipo de documento para usar heurísticas específicas
                string documentType = IdentifyDocumentType(filePath);
                
                // Obtém uma lista de possíveis títulos com níveis de confiança
                var titleCandidates = ExtractTitleCandidates(filePath, documentType);
                
                // Se encontrou algum candidato, retorna o de maior confiança
                if (titleCandidates.Count > 0)
                {
                    var bestCandidate = titleCandidates.OrderByDescending(c => c.Confidence).First();
                    return bestCandidate.Value;
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
        /// Extrai candidatos a título de um arquivo PDF com níveis de confiança
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <param name="documentType">Tipo de documento</param>
        /// <returns>Lista de candidatos a título com níveis de confiança</returns>
        private List<MetadataExtractionResult> ExtractTitleCandidates(string filePath, string documentType)
        {
            var candidates = new List<MetadataExtractionResult>();
            
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // 1. Tenta extrair dos metadados do PDF
                    var info = document.GetDocumentInfo();
                    if (info != null && !string.IsNullOrWhiteSpace(info.GetTitle()))
                    {
                        candidates.Add(new MetadataExtractionResult(
                            info.GetTitle(),
                            90, // Alta confiança para metadados oficiais
                            "Metadados do PDF"
                        ));
                    }
                    
                    // 2. Extrai texto das primeiras páginas para análise
                    string firstPageText = ExtractTextFromPage(document, 1);
                    string firstPagesText = ExtractTextFromPages(document, 1, Math.Min(3, document.GetNumberOfPages()));
                    
                    if (!string.IsNullOrWhiteSpace(firstPageText))
                    {
                        // 3. Tenta encontrar título baseado no tipo de documento
                        ExtractTitleByDocumentType(firstPageText, firstPagesText, documentType, candidates);
                        
                        // 4. Tenta encontrar título usando heurísticas gerais
                        ExtractTitleUsingGeneralHeuristics(firstPageText, candidates);
                        
                        // 5. Tenta detectar título multi-linha
                        ExtractMultiLineTitle(firstPageText, candidates);
                    }
                }
                
                return candidates;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao extrair candidatos a título: {ex.Message}");
                return candidates;
            }
        }
        
        /// <summary>
        /// Extrai título baseado no tipo de documento
        /// </summary>
        private void ExtractTitleByDocumentType(string firstPageText, string firstPagesText, string documentType, List<MetadataExtractionResult> candidates)
        {
            string[] lines = firstPageText.Split('\n');
            
            switch (documentType)
            {
                case "Paper Científico":
                    // Em papers científicos, o título geralmente está antes do abstract
                    int abstractIndex = firstPagesText.IndexOf("abstract", StringComparison.OrdinalIgnoreCase);
                    if (abstractIndex > 0)
                    {
                        // Pega até 200 caracteres antes do abstract
                        int startIndex = Math.Max(0, abstractIndex - 200);
                        string textBeforeAbstract = firstPagesText.Substring(startIndex, abstractIndex - startIndex);
                        string[] linesBeforeAbstract = textBeforeAbstract.Split('\n');
                        
                        // Procura por linhas que parecem ser um título
                        foreach (var line in linesBeforeAbstract.Reverse()) // Começa da mais próxima do abstract
                        {
                            string trimmedLine = line.Trim();
                            if (trimmedLine.Length > 10 && trimmedLine.Length < 200 && 
                                !trimmedLine.Contains("abstract", StringComparison.OrdinalIgnoreCase) &&
                                char.IsUpper(trimmedLine.FirstOrDefault()))
                            {
                                candidates.Add(new MetadataExtractionResult(
                                    trimmedLine,
                                    80,
                                    "Título antes do abstract"
                                ));
                                break;
                            }
                        }
                    }
                    break;
                    
                case "Artigo":
                    // Em artigos, o título geralmente está em destaque no início
                    for (int i = 0; i < Math.Min(5, lines.Length); i++)
                    {
                        string line = lines[i].Trim();
                        if (line.Length > 10 && line.Length < 150 && 
                            char.IsUpper(line.FirstOrDefault()) && 
                            !line.EndsWith(":") && 
                            !Regex.IsMatch(line, @"^\d+\."))
                        {
                            candidates.Add(new MetadataExtractionResult(
                                line,
                                75,
                                "Título de artigo"
                            ));
                            break;
                        }
                    }
                    break;
                    
                case "E-book":
                    // Em e-books, o título pode estar na capa ou na página de título
                    // Procura por linhas isoladas no início que parecem ser um título
                    for (int i = 0; i < Math.Min(15, lines.Length); i++)
                    {
                        string line = lines[i].Trim();
                        if (line.Length > 5 && line.Length < 100 && 
                            !line.Contains("©") && !line.Contains("Copyright") &&
                            !line.Contains("ISBN", StringComparison.OrdinalIgnoreCase) &&
                            !Regex.IsMatch(line, @"^\d+$"))
                        {
                            candidates.Add(new MetadataExtractionResult(
                                line,
                                70,
                                "Título de e-book"
                            ));
                            break;
                        }
                    }
                    break;
                    
                case "Apresentação":
                    // Em apresentações, o título geralmente está no primeiro slide
                    // Procura pela primeira linha não vazia que não seja um cabeçalho ou rodapé
                    for (int i = 0; i < Math.Min(5, lines.Length); i++)
                    {
                        string line = lines[i].Trim();
                        if (line.Length > 5 && line.Length < 100 && 
                            !line.Contains("©") && !line.Contains("Copyright") &&
                            !Regex.IsMatch(line, @"^\d+$") && !Regex.IsMatch(line, @"^\d+/\d+$"))
                        {
                            candidates.Add(new MetadataExtractionResult(
                                line,
                                75,
                                "Título de apresentação"
                            ));
                            break;
                        }
                    }
                    break;
                    
                case "Jornal":
                case "Revista":
                    // Em jornais e revistas, pode haver múltiplos títulos (manchetes)
                    // Procura por linhas em destaque no início
                    for (int i = 0; i < Math.Min(10, lines.Length); i++)
                    {
                        string line = lines[i].Trim();
                        if (line.Length > 10 && line.Length < 150 && 
                            char.IsUpper(line.FirstOrDefault()) && 
                            !line.EndsWith(":") && 
                            !Regex.IsMatch(line, @"^\d+\."))
                        {
                            candidates.Add(new MetadataExtractionResult(
                                line,
                                65,
                                "Manchete de jornal/revista"
                            ));
                            break;
                        }
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Extrai título usando heurísticas gerais
        /// </summary>
        private void ExtractTitleUsingGeneralHeuristics(string firstPageText, List<MetadataExtractionResult> candidates)
        {
            string[] lines = firstPageText.Split('\n');
            
            // Procura por linhas que parecem ser um título nas primeiras 10 linhas
            for (int i = 0; i < Math.Min(10, lines.Length); i++)
            {
                string line = lines[i].Trim();
                
                // Critérios básicos para um título
                if (line.Length > 5 && line.Length < 100 && 
                    !line.Contains("©") && !line.Contains("Copyright") &&
                    !Regex.IsMatch(line, @"^\d+$") && !Regex.IsMatch(line, @"^\d+/\d+$"))
                {
                    // Calcula um nível de confiança baseado em características do texto
                    double confidence = 60; // Confiança base
                    
                    // Bônus se a linha começar com letra maiúscula
                    if (line.Length > 0 && char.IsUpper(line[0]))
                    {
                        confidence += 5;
                    }
                    
                    // Bônus se a linha não terminar com pontuação (títulos geralmente não terminam com ponto)
                    if (line.Length > 0 && !".,:;?!".Contains(line[line.Length - 1]))
                    {
                        confidence += 5;
                    }
                    
                    // Penalidade se a linha for muito curta
                    if (line.Length < 10)
                    {
                        confidence -= 10;
                    }
                    
                    // Penalidade se a linha contiver muitos números
                    int digitCount = line.Count(char.IsDigit);
                    if (digitCount > line.Length / 4)
                    {
                        confidence -= 10;
                    }
                    
                    candidates.Add(new MetadataExtractionResult(
                        line,
                        confidence,
                        "Heurística geral"
                    ));
                    
                    break; // Pega apenas o primeiro candidato que atende aos critérios
                }
            }
        }
        
        /// <summary>
        /// Extrai título multi-linha
        /// </summary>
        private void ExtractMultiLineTitle(string firstPageText, List<MetadataExtractionResult> candidates)
        {
            string[] lines = firstPageText.Split('\n');
            
            // Procura por sequências de linhas que parecem formar um título
            for (int i = 0; i < Math.Min(8, lines.Length - 1); i++)
            {
                string line1 = lines[i].Trim();
                string line2 = lines[i + 1].Trim();
                
                // Verifica se as duas linhas consecutivas parecem formar um título
                if (line1.Length > 5 && line1.Length < 100 && 
                    line2.Length > 0 && line2.Length < 100 &&
                    !line1.Contains("©") && !line1.Contains("Copyright") &&
                    !line2.Contains("©") && !line2.Contains("Copyright") &&
                    char.IsUpper(line1.FirstOrDefault()) &&
                    !line1.EndsWith(".") && !line1.EndsWith(":") &&
                    !Regex.IsMatch(line1, @"^\d+\.") && !Regex.IsMatch(line2, @"^\d+\."))
                {
                    // Combina as linhas em um único título
                    string combinedTitle = line1;
                    
                    // Verifica se a primeira linha termina com hífen, preposição ou conjunção
                    bool shouldAddSpace = true;
                    string[] connectors = new[] { "de", "da", "do", "das", "dos", "e", "ou", "a", "o", "em", "para", "por", "com" };
                    
                    // Se a primeira linha termina com hífen, não adiciona espaço
                    if (line1.EndsWith("-"))
                    {
                        combinedTitle = line1.Substring(0, line1.Length - 1);
                        shouldAddSpace = false;
                    }
                    // Se a primeira linha termina com conector, adiciona espaço
                    else
                    {
                        string lastWord = line1.Split(' ').LastOrDefault() ?? "";
                        if (connectors.Contains(lastWord.ToLower()))
                        {
                            shouldAddSpace = true;
                        }
                    }
                    
                    // Adiciona a segunda linha com ou sem espaço
                    combinedTitle += (shouldAddSpace ? " " : "") + line2;
                    
                    candidates.Add(new MetadataExtractionResult(
                        combinedTitle,
                        70, // Confiança alta para títulos multi-linha
                        "Título multi-linha"
                    ));
                    
                    // Verifica se há uma terceira linha que também faz parte do título
                    if (i + 2 < lines.Length)
                    {
                        string line3 = lines[i + 2].Trim();
                        if (line3.Length > 0 && line3.Length < 100 &&
                            !line3.Contains("©") && !line3.Contains("Copyright") &&
                            !Regex.IsMatch(line3, @"^\d+\."))
                        {
                            // Adiciona a terceira linha
                            combinedTitle += (shouldAddSpace ? " " : "") + line3;
                            
                            candidates.Add(new MetadataExtractionResult(
                                combinedTitle,
                                65, // Confiança um pouco menor para títulos de três linhas
                                "Título multi-linha (3 linhas)"
                            ));
                        }
                    }
                    
                    break; // Pega apenas o primeiro candidato que atende aos critérios
                }
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
                // Obtém o tipo de documento para usar heurísticas específicas
                string documentType = IdentifyDocumentType(filePath);
                
                // Obtém uma lista de possíveis autores com níveis de confiança
                var authorCandidates = ExtractAuthorCandidates(filePath, documentType);
                
                // Se encontrou algum candidato, retorna o de maior confiança
                if (authorCandidates.Count > 0)
                {
                    var bestCandidate = authorCandidates.OrderByDescending(c => c.Confidence).First();
                    return bestCandidate.Value;
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
        /// Extrai candidatos a autor de um arquivo PDF com níveis de confiança
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <param name="documentType">Tipo de documento</param>
        /// <returns>Lista de candidatos a autor com níveis de confiança</returns>
        private List<MetadataExtractionResult> ExtractAuthorCandidates(string filePath, string documentType)
        {
            var candidates = new List<MetadataExtractionResult>();
            
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // 1. Tenta extrair dos metadados do PDF
                    var info = document.GetDocumentInfo();
                    if (info != null && !string.IsNullOrWhiteSpace(info.GetAuthor()))
                    {
                        candidates.Add(new MetadataExtractionResult(
                            info.GetAuthor(),
                            90, // Alta confiança para metadados oficiais
                            "Metadados do PDF"
                        ));
                    }
                    
                    // 2. Extrai texto das primeiras páginas para análise
                    string firstPageText = ExtractTextFromPage(document, 1);
                    string firstPagesText = ExtractTextFromPages(document, 1, Math.Min(3, document.GetNumberOfPages()));
                    
                    if (!string.IsNullOrWhiteSpace(firstPagesText))
                    {
                        // 3. Tenta encontrar autor baseado no tipo de documento
                        ExtractAuthorByDocumentType(firstPageText, firstPagesText, documentType, candidates);
                        
                        // 4. Procura por padrões comuns que indicam autoria
                        ExtractAuthorUsingPatterns(firstPagesText, candidates);
                        
                        // 5. Tenta detectar múltiplos autores
                        ExtractMultipleAuthors(firstPagesText, candidates);
                    }
                }
                
                return candidates;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao extrair candidatos a autor: {ex.Message}");
                return candidates;
            }
        }
        
        /// <summary>
        /// Extrai autor baseado no tipo de documento
        /// </summary>
        private void ExtractAuthorByDocumentType(string firstPageText, string firstPagesText, string documentType, List<MetadataExtractionResult> candidates)
        {
            string[] lines = firstPageText.Split('\n');
            
            switch (documentType)
            {
                case "Paper Científico":
                    // Em papers científicos, os autores geralmente estão logo após o título
                    // e antes do abstract, muitas vezes com afiliações
                    int abstractIndex = firstPagesText.IndexOf("abstract", StringComparison.OrdinalIgnoreCase);
                    if (abstractIndex > 0)
                    {
                        // Pega até 300 caracteres antes do abstract
                        int startIndex = Math.Max(0, abstractIndex - 300);
                        string textBeforeAbstract = firstPagesText.Substring(startIndex, abstractIndex - startIndex);
                        
                        // Procura por padrões de e-mail que geralmente indicam autores em papers
                        var emailMatches = Regex.Matches(textBeforeAbstract, @"[\w\.-]+@[\w\.-]+\.\w+");
                        if (emailMatches.Count > 0)
                        {
                            // Pega as linhas próximas aos e-mails
                            string[] emailLines = textBeforeAbstract.Split('\n');
                            foreach (var line in emailLines)
                            {
                                if (line.Contains('@') || 
                                    (line.Length > 3 && line.Length < 100 && 
                                     !line.Contains("abstract", StringComparison.OrdinalIgnoreCase) &&
                                     !line.Contains("keywords", StringComparison.OrdinalIgnoreCase)))
                                {
                                    candidates.Add(new MetadataExtractionResult(
                                        line.Trim(),
                                        75,
                                        "Autor de paper científico"
                                    ));
                                }
                            }
                        }
                    }
                    break;
                    
                case "Artigo":
                    // Em artigos, o autor geralmente está logo após o título
                    bool foundTitle = false;
                    for (int i = 0; i < Math.Min(10, lines.Length); i++)
                    {
                        string line = lines[i].Trim();
                        
                        // Se já encontrou o título, a próxima linha não vazia pode ser o autor
                        if (foundTitle)
                        {
                            if (!string.IsNullOrWhiteSpace(line) && line.Length < 100 &&
                                !line.StartsWith("http") && !line.Contains("©") && !line.Contains("Copyright"))
                            {
                                candidates.Add(new MetadataExtractionResult(
                                    line,
                                    70,
                                    "Autor após título de artigo"
                                ));
                                break;
                            }
                        }
                        
                        // Identifica uma linha que parece ser um título
                        if (line.Length > 10 && line.Length < 150 && 
                            char.IsUpper(line.FirstOrDefault()) && 
                            !line.EndsWith(":") && 
                            !Regex.IsMatch(line, @"^\d+\."))
                        {
                            foundTitle = true;
                        }
                    }
                    break;
                    
                case "E-book":
                    // Em e-books, o autor geralmente está na capa ou na página de título
                    // Procura por padrões como "por [Nome]" ou linhas isoladas que parecem ser um autor
                    for (int i = 0; i < Math.Min(20, lines.Length); i++)
                    {
                        string line = lines[i].Trim();
                        
                        // Verifica se a linha contém "por" ou "by" seguido de um nome
                        if ((line.StartsWith("por ", StringComparison.OrdinalIgnoreCase) || 
                             line.StartsWith("by ", StringComparison.OrdinalIgnoreCase)) &&
                            line.Length > 5 && line.Length < 100)
                        {
                            candidates.Add(new MetadataExtractionResult(
                                line.Substring(line.IndexOf(' ')).Trim(),
                                80,
                                "Autor de e-book com prefixo"
                            ));
                            break;
                        }
                        
                        // Verifica se é uma linha isolada que parece ser um nome de autor
                        if (line.Length > 3 && line.Length < 50 && 
                            !line.Contains("©") && !line.Contains("Copyright") &&
                            !line.Contains("ISBN", StringComparison.OrdinalIgnoreCase) &&
                            !Regex.IsMatch(line, @"^\d+$") &&
                            line.Split(' ').Length >= 2 && // Pelo menos nome e sobrenome
                            char.IsUpper(line.FirstOrDefault()))
                        {
                            candidates.Add(new MetadataExtractionResult(
                                line,
                                65,
                                "Possível autor de e-book"
                            ));
                        }
                    }
                    break;
                    
                case "Apresentação":
                    // Em apresentações, o autor/apresentador geralmente está no primeiro slide
                    // ou em um slide de título
                    for (int i = 0; i < Math.Min(10, lines.Length); i++)
                    {
                        string line = lines[i].Trim();
                        
                        // Procura por linhas que parecem ser um nome de apresentador
                        if (line.Length > 3 && line.Length < 50 && 
                            !line.Contains("©") && !line.Contains("Copyright") &&
                            !Regex.IsMatch(line, @"^\d+$") && !Regex.IsMatch(line, @"^\d+/\d+$") &&
                            line.Split(' ').Length >= 2) // Pelo menos nome e sobrenome
                        {
                            // Verifica se a linha anterior ou posterior contém palavras-chave como "apresentado por"
                            bool hasPresenterContext = false;
                            if (i > 0)
                            {
                                string prevLine = lines[i - 1].Trim().ToLower();
                                if (prevLine.Contains("apresentado por") || prevLine.Contains("presented by") ||
                                    prevLine.Contains("palestrante") || prevLine.Contains("speaker"))
                                {
                                    hasPresenterContext = true;
                                }
                            }
                            
                            if (i < lines.Length - 1)
                            {
                                string nextLine = lines[i + 1].Trim().ToLower();
                                if (nextLine.Contains("apresentado por") || nextLine.Contains("presented by") ||
                                    nextLine.Contains("palestrante") || nextLine.Contains("speaker"))
                                {
                                    hasPresenterContext = true;
                                }
                            }
                            
                            if (hasPresenterContext)
                            {
                                candidates.Add(new MetadataExtractionResult(
                                    line,
                                    75,
                                    "Apresentador com contexto"
                                ));
                                break;
                            }
                            else
                            {
                                candidates.Add(new MetadataExtractionResult(
                                    line,
                                    60,
                                    "Possível apresentador"
                                ));
                            }
                        }
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Extrai autor usando padrões comuns
        /// </summary>
        private void ExtractAuthorUsingPatterns(string text, List<MetadataExtractionResult> candidates)
        {
            // Procura por padrões comuns que indicam autoria
            string[] authorPatterns = new[] { 
                "autor:", "author:", "by:", "por:", "escrito por", "written by",
                "autoria de", "authored by", "criado por", "created by"
            };
            
            foreach (var pattern in authorPatterns)
            {
                int index = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    // Extrai o texto após o padrão até a próxima quebra de linha
                    int startIndex = index + pattern.Length;
                    int endIndex = text.IndexOf('\n', startIndex);
                    if (endIndex > startIndex)
                    {
                        string author = text.Substring(startIndex, endIndex - startIndex).Trim();
                        if (!string.IsNullOrWhiteSpace(author) && author.Length < 100)
                        {
                            candidates.Add(new MetadataExtractionResult(
                                author,
                                85, // Alta confiança para padrões explícitos
                                $"Padrão '{pattern}'"
                            ));
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Extrai múltiplos autores
        /// </summary>
        private void ExtractMultipleAuthors(string text, List<MetadataExtractionResult> candidates)
        {
            // Procura por padrões de múltiplos autores separados por vírgulas, "e", "and", etc.
            string[] lines = text.Split('\n');
            
            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                
                // Verifica se a linha parece conter múltiplos autores
                if (trimmedLine.Length > 5 && trimmedLine.Length < 200 &&
                    (trimmedLine.Contains(",") || trimmedLine.Contains(" e ") || 
                     trimmedLine.Contains(" and ") || trimmedLine.Contains(" & ")))
                {
                    // Verifica se a linha contém palavras-chave que indicam autoria
                    bool hasAuthorContext = false;
                    string[] authorKeywords = new[] { "autor", "author", "by", "escrito", "written" };
                    
                    foreach (var keyword in authorKeywords)
                    {
                        if (trimmedLine.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            hasAuthorContext = true;
                            break;
                        }
                    }
                    
                    // Verifica se a linha contém padrões de nome (pelo menos dois nomes próprios)
                    bool hasNamePattern = Regex.IsMatch(trimmedLine, @"\b[A-Z][a-z]+\s+[A-Z][a-z]+\b");
                    
                    if (hasAuthorContext || hasNamePattern)
                    {
                        candidates.Add(new MetadataExtractionResult(
                            trimmedLine,
                            hasAuthorContext ? 80 : 65,
                            "Múltiplos autores"
                        ));
                    }
                }
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
        /// Calcula o percentual de confiança de que um arquivo PDF é uma apresentação
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>Percentual de confiança (0-100%)</returns>
        public double CalculatePresentationConfidence(string filePath)
        {
            try
            {
                double confidence = 0;
                
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    // Fator 1: Número de páginas (slides)
                    int pageCount = document.GetNumberOfPages();
                    if (pageCount < 5)
                    {
                        confidence -= 10; // Penalidade para apresentações muito curtas
                    }
                    else if (pageCount >= 10 && pageCount <= 60)
                    {
                        confidence += 20; // Bônus para número típico de slides
                    }
                    else if (pageCount > 100)
                    {
                        confidence -= 30; // Penalidade para documentos muito longos
                    }
                    
                    // Fator 2: Palavras-chave encontradas
                    string text = ExtractTextFromPages(document, 1, Math.Min(pageCount, 5));
                    int keywordsFound = 0;
                    
                    foreach (string keyword in _presentationKeywords)
                    {
                        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            keywordsFound++;
                        }
                    }
                    
                    // Adiciona pontos com base na proporção de palavras-chave encontradas
                    confidence += (keywordsFound * 100.0 / _presentationKeywords.Length) * 0.4;
                    
                    // Fator 3: Densidade de texto (apresentações geralmente têm menos texto por página)
                    double avgTextLength = 0;
                    for (int i = 1; i <= Math.Min(pageCount, 5); i++)
                    {
                        string pageText = ExtractTextFromPage(document, i);
                        avgTextLength += pageText.Length;
                    }
                    avgTextLength /= Math.Min(pageCount, 5);
                    
                    // Se a média de caracteres por página for baixa, provavelmente é uma apresentação
                    if (avgTextLength < 500)
                    {
                        confidence += 25;
                    }
                    else if (avgTextLength > 2000)
                    {
                        confidence -= 15; // Penalidade para páginas com muito texto
                    }
                    
                    // Fator 4: Padrões visuais típicos de slides (difícil de detectar apenas com texto)
                    // Verificamos se há padrões de marcadores ou numeração
                    if (Regex.IsMatch(text, @"(?m)^\s*[•\-\*]\s+", RegexOptions.Multiline)) // Marcadores
                    {
                        confidence += 15;
                    }
                    if (Regex.IsMatch(text, @"(?m)^\s*\d+\.\s+", RegexOptions.Multiline)) // Numeração
                    {
                        confidence += 10;
                    }
                    
                    // Fator 5: Nome do arquivo
                    string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                    if (fileName.Contains("apresentação") || fileName.Contains("presentation") || 
                        fileName.Contains("slide") || fileName.Contains("palestra"))
                    {
                        confidence += 30;
                    }
                }
                
                return Math.Clamp(confidence, 0, 100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao calcular confiança de apresentação: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo PDF é uma apresentação
        /// </summary>
        /// <param name="filePath">Caminho do arquivo PDF</param>
        /// <returns>True se for uma apresentação, False caso contrário</returns>
        public bool IsPresentation(string filePath)
        {
            return CalculatePresentationConfidence(filePath) >= 40; // Limiar de confiança para considerar como apresentação
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
                new DocumentTypeResult("Apresentação", CalculatePresentationConfidence(filePath)),
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
