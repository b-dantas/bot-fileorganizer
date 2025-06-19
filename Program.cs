using bot_fileorganizer.Data;
using bot_fileorganizer.Models;
using bot_fileorganizer.Services;

namespace bot_fileorganizer
{
    class Program
    {
        private static FileOrganizerService? _fileOrganizerService;
        private static bool _running = true;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Bot Organizador de Arquivos";

            // Inicializa os serviços
            var repository = new FileRepository();
            var settingsRepository = new AppSettingsRepository();
            var pdfAnalyzer = new PdfAnalyzer();
            _fileOrganizerService = new FileOrganizerService(repository, pdfAnalyzer, settingsRepository);

            // Exibe mensagem de boas-vindas
            ExibirBoasVindas();

            // Loop principal
            while (_running)
            {
                ExibirMenuPrincipal();
                string? opcao = Console.ReadLine();

                switch (opcao)
                {
                    case "1":
                        SelecionarDiretorio();
                        break;
                    case "2":
                        ListarArquivosNaoPadronizados();
                        break;
                    case "3":
                        ProcessarArquivos();
                        break;
                    case "4":
                        ExibirHistorico();
                        break;
                    case "5":
                        AtualizarMetadados();
                        break;
                    case "0":
                        _running = false;
                        break;
                    default:
                        Console.WriteLine("\nOpção inválida. Tente novamente.");
                        break;
                }
            }

            Console.WriteLine("\nObrigado por usar o Bot Organizador de Arquivos!");
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        static void ExibirBoasVindas()
        {
            Console.Clear();
            Console.WriteLine("=================================================");
            Console.WriteLine("      BOT ORGANIZADOR DE ARQUIVOS - v1.1.1       ");
            Console.WriteLine("=================================================");
            Console.WriteLine("\nBem-vindo ao Bot Organizador de Arquivos!");
            Console.WriteLine("Este aplicativo ajuda a padronizar nomes de arquivos PDF.");
            //Console.WriteLine("\nPressione qualquer tecla para continuar...");
            //Console.ReadKey();
        }

        static void ExibirMenuPrincipal()
        {
            Console.Clear();
            Console.WriteLine("=================================================");
            Console.WriteLine("                  MENU PRINCIPAL                 ");
            Console.WriteLine("=================================================");

            // Exibe o diretório atual, se houver
            string diretorioAtual = _fileOrganizerService?.GetCurrentDirectory() ?? "Nenhum diretório selecionado";
            Console.WriteLine($"\nDiretório atual: {diretorioAtual}");

            Console.WriteLine("\nEscolha uma opção:");
            Console.WriteLine("1. Selecionar diretório");
            Console.WriteLine("2. Listar arquivos não padronizados");
            Console.WriteLine("3. Processar arquivos");
            Console.WriteLine("4. Exibir histórico de operações");
            Console.WriteLine("5. Atualizar metadados de arquivos padronizados");
            Console.WriteLine("0. Sair");
            Console.Write("\nOpção: ");
        }

        static void SelecionarDiretorio()
        {
            Console.Clear();
            Console.WriteLine("=================================================");
            Console.WriteLine("              SELECIONAR DIRETÓRIO               ");
            Console.WriteLine("=================================================");
            Console.WriteLine("\nDigite o caminho completo do diretório:");
            string? caminho = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(caminho))
            {
                Console.WriteLine("\nCaminho inválido.");
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                return;
            }

            bool sucesso = _fileOrganizerService?.SetDirectory(caminho) ?? false;

            if (sucesso)
            {
                Console.WriteLine($"\nDiretório selecionado com sucesso: {caminho}");
            }
            else
            {
                Console.WriteLine($"\nDiretório não encontrado: {caminho}");
            }

            Console.WriteLine("\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
        }

        static void ListarArquivosNaoPadronizados()
        {
            if (_fileOrganizerService == null)
            {
                return;
            }

            Console.Clear();
            Console.WriteLine("=================================================");
            Console.WriteLine("         ARQUIVOS PDF NÃO PADRONIZADOS           ");
            Console.WriteLine("=================================================");

            string diretorioAtual = _fileOrganizerService.GetCurrentDirectory();
            if (string.IsNullOrEmpty(diretorioAtual))
            {
                Console.WriteLine("\nNenhum diretório selecionado. Selecione um diretório primeiro.");
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                return;
            }

            var arquivosNaoPadronizados = _fileOrganizerService.ListNonStandardizedFiles();

            if (arquivosNaoPadronizados.Count == 0)
            {
                Console.WriteLine("\nTodos os arquivos PDF no diretório já estão padronizados.");
            }
            else
            {
                Console.WriteLine($"\nForam encontrados {arquivosNaoPadronizados.Count} arquivos não padronizados:");
                for (int i = 0; i < arquivosNaoPadronizados.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(arquivosNaoPadronizados[i])}");
                }
            }

            Console.WriteLine("\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
        }

        static void ProcessarArquivos()
        {
            if (_fileOrganizerService == null)
            {
                return;
            }

            Console.Clear();
            Console.WriteLine("=================================================");
            Console.WriteLine("              PROCESSAR ARQUIVOS                 ");
            Console.WriteLine("=================================================");

            string diretorioAtual = _fileOrganizerService.GetCurrentDirectory();
            if (string.IsNullOrEmpty(diretorioAtual))
            {
                Console.WriteLine("\nNenhum diretório selecionado. Selecione um diretório primeiro.");
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                return;
            }

            var arquivosNaoPadronizados = _fileOrganizerService.ListNonStandardizedFiles();

            if (arquivosNaoPadronizados.Count == 0)
            {
                Console.WriteLine("\nTodos os arquivos PDF no diretório já estão padronizados.");
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nForam encontrados {arquivosNaoPadronizados.Count} arquivos não padronizados.");
            Console.WriteLine("\nDeseja processar os arquivos em lotes? (S/N)");
            string? resposta = Console.ReadLine();

            if (resposta?.Trim().ToUpper() != "S")
            {
                Console.WriteLine("\nOperação cancelada pelo usuário.");
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                return;
            }

            // Processa os arquivos em lotes
            int totalLotes = _fileOrganizerService.GetTotalBatches();
            int loteAtual = 0;

            while (loteAtual < totalLotes)
            {
                Console.Clear();
                Console.WriteLine("=================================================");
                Console.WriteLine($"            PROCESSANDO LOTE {loteAtual + 1} DE {totalLotes}           ");
                Console.WriteLine("=================================================");

                // Obtém os arquivos do lote atual
                var arquivosDoLote = _fileOrganizerService.GetNonStandardizedFilesBatch(loteAtual);
                
                if (arquivosDoLote.Count == 0)
                {
                    Console.WriteLine("\nNão há mais arquivos para processar.");
                    break;
                }

                Console.WriteLine($"\nProcessando {arquivosDoLote.Count} arquivos do lote {loteAtual + 1}:");
                for (int i = 0; i < arquivosDoLote.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(arquivosDoLote[i])}");
                }

                Console.WriteLine("\nDeseja processar este lote? (S/N)");
                resposta = Console.ReadLine();

                if (resposta?.Trim().ToUpper() != "S")
                {
                    Console.WriteLine("\nLote ignorado pelo usuário.");
                    loteAtual++;
                    continue;
                }

                // Processa os arquivos do lote e obtém as propostas de renomeação
                var registros = _fileOrganizerService.ProcessFiles(arquivosDoLote);

                // Exibe as propostas e solicita confirmação para cada uma
                foreach (var registro in registros)
                {
                    Console.Clear();
                    Console.WriteLine("=================================================");
                    Console.WriteLine("            PROPOSTA DE RENOMEAÇÃO               ");
                    Console.WriteLine("=================================================");
                    Console.WriteLine($"\nArquivo original: {registro.OriginalName}");
                    Console.WriteLine($"Novo nome proposto: {registro.ProposedName}");
                    Console.WriteLine("\nAceitar esta proposta? (S/N)");
                    
                    resposta = Console.ReadLine();
                    
                    if (resposta?.Trim().ToUpper() == "S")
                    {
                        bool sucesso = _fileOrganizerService.RenameFile(registro.FilePath, registro.ProposedName);
                        
                        if (sucesso)
                        {
                            Console.WriteLine("\nArquivo renomeado com sucesso!");
                        }
                        else
                        {
                            Console.WriteLine("\nErro ao renomear o arquivo.");
                        }
                    }
                    else
                    {
                        // Marca o arquivo como rejeitado para não ser oferecido novamente
                        _fileOrganizerService.RejectFile(registro.FilePath);
                        Console.WriteLine("\nProposta rejeitada. O arquivo não será oferecido novamente.");
                    }
                    
                    Console.WriteLine("\nPressione qualquer tecla para continuar...");
                    Console.ReadKey();
                }

                loteAtual++;

                // Verifica se há mais lotes para processar
                if (loteAtual < totalLotes)
                {
                    Console.Clear();
                    Console.WriteLine("=================================================");
                    Console.WriteLine("              CONTINUAR PROCESSAMENTO            ");
                    Console.WriteLine("=================================================");
                    Console.WriteLine($"\nLote {loteAtual} de {totalLotes} concluído.");
                    Console.WriteLine("\nDeseja continuar para o próximo lote? (S/N)");
                    
                    resposta = Console.ReadLine();
                    
                    if (resposta?.Trim().ToUpper() != "S")
                    {
                        Console.WriteLine("\nProcessamento interrompido pelo usuário.");
                        break;
                    }
                }
            }

            Console.WriteLine("\nProcessamento concluído!");
            Console.WriteLine("\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
        }

        static void ExibirHistorico()
        {
            if (_fileOrganizerService == null)
            {
                return;
            }

            Console.Clear();
            Console.WriteLine("=================================================");
            Console.WriteLine("           HISTÓRICO DE OPERAÇÕES                ");
            Console.WriteLine("=================================================");

            // Obter o histórico do repositório
            var repository = new FileRepository();
            var registros = repository.GetAllRecords();

            if (registros.Count == 0)
            {
                Console.WriteLine("\nNenhuma operação registrada.");
            }
            else
            {
                Console.WriteLine($"\nForam encontradas {registros.Count} operações:");
                
                foreach (var registro in registros)
                {
                    string status = registro.Accepted ? "Aceito" : (registro.Rejected ? "Rejeitado" : "Pendente");
                    Console.WriteLine($"\nData: {registro.OperationDate}");
                    Console.WriteLine($"Arquivo: {registro.OriginalName}");
                    Console.WriteLine($"Novo nome: {registro.ProposedName}");
                    Console.WriteLine($"Status: {status}");
                    Console.WriteLine("--------------------------------------------------");
                }
            }

            Console.WriteLine("\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
        }

        static void AtualizarMetadados()
        {
            if (_fileOrganizerService == null)
            {
                return;
            }

            Console.Clear();
            Console.WriteLine("=================================================");
            Console.WriteLine("        ATUALIZAR METADADOS DE ARQUIVOS          ");
            Console.WriteLine("=================================================");

            string diretorioAtual = _fileOrganizerService.GetCurrentDirectory();
            if (string.IsNullOrEmpty(diretorioAtual))
            {
                Console.WriteLine("\nNenhum diretório selecionado. Selecione um diretório primeiro.");
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                return;
            }

            var arquivosPadronizados = _fileOrganizerService.ListStandardizedFiles();

            if (arquivosPadronizados.Count == 0)
            {
                Console.WriteLine("\nNão foram encontrados arquivos padronizados no diretório.");
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nForam encontrados {arquivosPadronizados.Count} arquivos padronizados.");
            Console.WriteLine("\nDeseja atualizar os metadados destes arquivos? (S/N)");
            string? resposta = Console.ReadLine();

            if (resposta?.Trim().ToUpper() != "S")
            {
                Console.WriteLine("\nOperação cancelada pelo usuário.");
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                return;
            }

            int arquivosAtualizados = 0;
            int arquivosComErro = 0;
            int arquivosRejeitados = 0;
            int totalArquivos = arquivosPadronizados.Count;
            int arquivoAtual = 0;

            // Processa cada arquivo individualmente
            foreach (var arquivo in arquivosPadronizados)
            {
                arquivoAtual++;
                
                Console.Clear();
                Console.WriteLine("=================================================");
                Console.WriteLine("            ATUALIZAÇÃO DE METADADOS             ");
                Console.WriteLine("=================================================");
                Console.WriteLine($"\nProcessando arquivo {arquivoAtual} de {totalArquivos}");
                
                string nomeArquivo = Path.GetFileName(arquivo);
                var (autor, titulo) = _fileOrganizerService.ExtractInfoFromFileName(arquivo);
                var pdfAnalyzer = new PdfAnalyzer();
                
                Console.WriteLine($"\nArquivo: {nomeArquivo}");
                Console.WriteLine($"Tipo de documento: {pdfAnalyzer.IdentifyDocumentType(arquivo)}");
                Console.WriteLine($"Autor atual: {pdfAnalyzer.ExtractAuthor(arquivo) ?? "Não disponível"}");
                Console.WriteLine($"Título atual: {pdfAnalyzer.ExtractTitle(arquivo) ?? "Não disponível"}");
                Console.WriteLine($"Novo autor proposto: {autor}");
                Console.WriteLine($"Novo título proposto: {titulo}");
                
                Console.WriteLine("\nDeseja atualizar os metadados deste arquivo? (S/N)");
                string? respostaIndividual = Console.ReadLine();
                
                if (respostaIndividual?.Trim().ToUpper() == "S")
                {
                    bool sucesso = _fileOrganizerService.UpdateFileMetadata(arquivo);
                    
                    if (sucesso)
                    {
                        Console.WriteLine("\nMetadados atualizados com sucesso!");
                        arquivosAtualizados++;
                    }
                    else
                    {
                        Console.WriteLine("\nErro ao atualizar metadados.");
                        arquivosComErro++;
                    }
                }
                else
                {
                    Console.WriteLine("\nOperação ignorada pelo usuário.");
                    // Registrar que o usuário optou por não atualizar este arquivo
                    _fileOrganizerService.RejectMetadataUpdate(arquivo);
                    arquivosRejeitados++;
                }
                
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
            }

            Console.Clear();
            Console.WriteLine("=================================================");
            Console.WriteLine("              RESUMO DA OPERAÇÃO                 ");
            Console.WriteLine("=================================================");
            Console.WriteLine($"\nTotal de arquivos processados: {arquivosAtualizados + arquivosComErro + arquivosRejeitados}");
            Console.WriteLine($"Arquivos atualizados com sucesso: {arquivosAtualizados}");
            Console.WriteLine($"Arquivos rejeitados pelo usuário: {arquivosRejeitados}");
            Console.WriteLine($"Arquivos com erro: {arquivosComErro}");
            Console.WriteLine("\nProcessamento concluído!");
            Console.WriteLine("\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
        }
    }
}
