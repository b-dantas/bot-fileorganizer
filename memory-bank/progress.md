# Progress

Atualmente o bot não foi criado. A primeira versão deve se concentrar em arquivos do tipo PDF, prefencialmente em arquivos de e-book, nome o nome do arquivo deve seguir um padrão como:

> Livro - Nome Autor - Título.pdf

É necessário respeitar o padrão de nomeação de arquivos do Windows, levando em conta a restrição de caracteres especiais e o tamanho máximo do nome do arquivo.

Para cada arquivo, o bot deve verificar se o nome já está no padrão. Caso não esteja, deve propor a renomeação do arquivo. O usuário deve ter a opção de aceitar ou recusar a renomeação. O bot deve ser capaz de listar os arquivos que não estão no padrão e permitir que o usuário escolha quais arquivos deseja renomear.

O bot precisa ter a capacidade de ler o conteúdo dos arquivos PDF para extrair informações como título e autor, caso esses dados não estejam disponíveis no nome do arquivo. Isso pode ser feito utilizando bibliotecas como iTextSharp ou PdfSharp.

É necessário o bot deduzir se o arquivo é um e-book ou não, para isso pode-se utilizar heurísticas avaliando o seu conteúdo se há menção de ser um livro, independente do idioma.

## Atualização (19/06/2025)

O bot foi inicializado com a seguinte estrutura:

1. **Estrutura de Arquivos**:
   - `Program.cs`: Interface de console e ponto de entrada da aplicação
   - `Models/FileRecord.cs`: Modelo para armazenar informações dos arquivos
   - `Data/FileRepository.cs`: Persistência em JSON
   - `Services/FileOrganizerService.cs`: Lógica principal de organização de arquivos
   - `Services/PdfAnalyzer.cs`: Análise de arquivos PDF

2. **Funcionalidades Implementadas**:
   - Interface de console com menu interativo
   - Seleção de diretório pelo usuário
   - Listagem de arquivos PDF não padronizados
   - Geração de propostas de renomeação
   - Aceitação/rejeição de propostas pelo usuário
   - Persistência de operações em arquivo JSON
   - Exibição de histórico de operações

3. **Pendências**:
   - Implementação da extração real de metadados de PDFs (atualmente simulada)
   - Integração com biblioteca de manipulação de PDFs (iTextSharp)
   - Aprimoramento das heurísticas para identificação de e-books
   - Testes com diferentes tipos de arquivos PDF
   - Armazenar no arquivo JSON o último diretório inicial utilizado pelo usuário
   - Realizar as operações de renomeação em lotes de 10 arquivos por vez, para evitar problemas de performance e garantir que o usuário possa revisar as mudanças antes de aplicá-las.
   - Persistir no histórico de operações os arquivos onde foi optado pela não renomeação, de modo que não seja ofertado novamente na próxima execução do bot.

O bot está funcional e pode ser executado, permitindo ao usuário selecionar um diretório, listar arquivos não padronizados, processar arquivos e visualizar o histórico de operações.

## Atualização (19/06/2025) - Versão 1.1.0

Foram implementadas todas as pendências identificadas anteriormente:

1. **Novas Funcionalidades**:
   - Integração com a biblioteca iText7 para manipulação de PDFs
   - Implementação da extração real de metadados de PDFs (título e autor)
   - Aprimoramento das heurísticas para identificação de e-books
   - Processamento em lotes de 10 arquivos por vez
   - Persistência de arquivos rejeitados para não serem ofertados novamente
   - Armazenamento do último diretório utilizado pelo usuário

2. **Novos Arquivos**:
   - `Models/AppSettings.cs`: Modelo para armazenar configurações do aplicativo
   - `Data/AppSettingsRepository.cs`: Persistência das configurações em JSON

3. **Melhorias Implementadas**:
   - **Extração de Metadados**: Agora o bot extrai metadados reais dos PDFs, incluindo título e autor, utilizando a biblioteca iText7. Se os metadados não estiverem disponíveis, o bot tenta extrair informações do conteúdo do PDF.
   - **Identificação de E-books**: Implementadas heurísticas mais avançadas para identificar se um arquivo PDF é um e-book, baseadas no número de páginas, conteúdo e palavras-chave em diferentes idiomas.
   - **Processamento em Lotes**: Os arquivos são processados em lotes de 10, permitindo ao usuário revisar cada lote antes de prosseguir para o próximo.
   - **Persistência de Rejeições**: Arquivos cuja renomeação foi rejeitada pelo usuário são marcados e não são ofertados novamente em execuções futuras.
   - **Último Diretório**: O último diretório utilizado pelo usuário é armazenado e carregado automaticamente na próxima execução do aplicativo.

4. **Melhorias na Interface**:
   - Adicionadas informações sobre o processamento em lotes
   - Melhorada a exibição do histórico para incluir o status de rejeição
   - Adicionadas mensagens de confirmação mais claras para o usuário

O bot agora está mais completo e robusto, atendendo a todos os requisitos iniciais. A extração de metadados de PDFs e a identificação de e-books foram significativamente aprimoradas, proporcionando uma melhor experiência ao usuário.
