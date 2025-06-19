# Progress

Atualmente o bot não foi criado. A primeira versão deve se concentrar em arquivos do tipo PDF, prefencialmente em arquivos de e-book, nome o nome do arquivo deve serguir um padrão como:

> Livro - Autor - Título.pdf

É necessário respeitar o padrão de nomeação de arquivos do Windows, levando em conta a restrição de caracteres especiais e o tamanho máximo do nome do arquivo.

Para cada arquivo, o bot deve verificar se o nome já está no padrão. Caso não esteja, deve propor a renomeação do arquivo. O usuário deve ter a opção de aceitar ou recusar a renomeação.
O bot deve ser capaz de listar os arquivos que não estão no padrão e permitir que o usuário escolha quais arquivos deseja renomear.

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
   - Integração com biblioteca de manipulação de PDFs (iTextSharp ou PdfSharp)
   - Aprimoramento das heurísticas para identificação de e-books
   - Testes com diferentes tipos de arquivos PDF

O bot está funcional e pode ser executado, permitindo ao usuário selecionar um diretório, listar arquivos não padronizados, processar arquivos e visualizar o histórico de operações.
