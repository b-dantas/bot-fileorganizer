# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/spec/v2.0.0.html).

## [1.1.3] - 2025-06-19

### Adicionado
- Sistema avançado de extração de metadados com níveis de confiança
- Classes auxiliares `MetadataExtractionResult` e `Author` para representar resultados de extração
- Métodos específicos para extração de título e autor por tipo de documento
- Detecção de títulos multi-linha com análise de conectores e preposições
- Suporte para múltiplos autores com detecção de padrões de separação
- Análise de contexto para melhorar a precisão da extração

### Melhorado
- Algoritmo de extração de título com heurísticas específicas por tipo de documento
- Algoritmo de extração de autor com suporte para múltiplos autores
- Detecção de padrões de autoria em diferentes formatos de documentos
- Análise de posicionamento de autores em relação ao título e outros elementos
- Precisão geral da extração de metadados, especialmente em documentos não-ebook

## [1.1.2] - 2025-06-19

### Adicionado
- Novo tipo de documento: "Apresentação" para detectar slides e apresentações em PDF
- Palavras-chave específicas para identificação de apresentações
- Método `CalculatePresentationConfidence` para calcular o percentual de confiança
- Método `IsPresentation` para verificar se um arquivo é uma apresentação
- Análise de densidade de texto por página para melhor identificação de slides
- Detecção de padrões visuais típicos como marcadores e numeração

## [1.1.1] - 2025-06-19

### Adicionado
- Nova opção no menu principal: "Atualizar metadados de arquivos padronizados"
- Implementação da extração de título e autor diretamente do nome do arquivo
- Classe `DocumentTypeResult` para armazenar o tipo de documento e seu percentual de confiança
- Métodos para calcular o percentual de confiança para cada tipo de documento
- Método `GetDocumentTypeConfidences` que retorna todos os tipos possíveis com seus percentuais
- Exibição do percentual de confiança e lista de outros tipos possíveis na interface

### Alterado
- Modificado o método `ProcessarArquivos` para processar cada arquivo completamente antes de passar para o próximo
- Simplificado o fluxo para processar todos os arquivos padronizados em uma única sequência
- Método `IsFileNameStandardized` agora aceita diferentes prefixos com base no tipo de documento
- Atualizado o método `GenerateProposedName` para usar o tipo de documento identificado
- Substituída a pergunta "Deseja processar os arquivos em lotes?" por "Deseja atualizar os metadados destes arquivos?"

### Corrigido
- Problema de dependência relacionado à biblioteca iText7 (adicionada a dependência `itext7.bouncy-castle-adapter`)
- Método `UpdateMetadata` agora preserva o arquivo original para evitar perda de dados

## [1.1.0] - 2025-06-19

### Adicionado
- Integração com a biblioteca iText7 para manipulação de PDFs
- Implementação da extração real de metadados de PDFs (título e autor)
- Processamento em lotes de 10 arquivos por vez
- Persistência de arquivos rejeitados para não serem ofertados novamente
- Armazenamento do último diretório utilizado pelo usuário
- Novos arquivos: `Models/AppSettings.cs` e `Data/AppSettingsRepository.cs`
- Novos métodos para identificar diferentes tipos de documentos:
  - `IsArticle`: Identifica se um PDF é um artigo
  - `IsScientificPaper`: Identifica se um PDF é um paper científico
  - `IsNewspaper`: Identifica se um PDF é um jornal
  - `IsMagazine`: Identifica se um PDF é uma revista

### Melhorado
- Heurísticas para identificação de e-books baseadas em:
  - Número de páginas típico para cada tipo de documento
  - Palavras-chave específicas em diferentes idiomas
  - Padrões de formatação comuns
  - Análise do nome do arquivo
- Exibição do histórico para incluir o status de rejeição
- Mensagens de confirmação mais claras para o usuário
- Confirmação individual para cada operação de atualização de metadados
- Exibição do tipo de documento e metadados atuais antes da confirmação

## [1.0.0] - 2025-06-19

### Adicionado
- Estrutura inicial do projeto:
  - `Program.cs`: Interface de console e ponto de entrada da aplicação
  - `Models/FileRecord.cs`: Modelo para armazenar informações dos arquivos
  - `Data/FileRepository.cs`: Persistência em JSON
  - `Services/FileOrganizerService.cs`: Lógica principal de organização de arquivos
  - `Services/PdfAnalyzer.cs`: Análise de arquivos PDF
- Interface de console com menu interativo
- Seleção de diretório pelo usuário
- Listagem de arquivos PDF não padronizados
- Geração de propostas de renomeação
- Aceitação/rejeição de propostas pelo usuário
- Persistência de operações em arquivo JSON
- Exibição de histórico de operações

## Sobre o Projeto

Este projeto é um bot organizador de arquivos que se concentra em arquivos do tipo PDF, preferencialmente e-books. O nome do arquivo deve seguir um padrão como:

> Livro - Nome Autor - Título.pdf

É necessário respeitar o padrão de nomeação de arquivos do Windows, levando em conta a restrição de caracteres especiais e o tamanho máximo do nome do arquivo.

Para cada arquivo, o bot verifica se o nome já está no padrão. Caso não esteja, propõe a renomeação do arquivo. O usuário tem a opção de aceitar ou recusar a renomeação. O bot é capaz de listar os arquivos que não estão no padrão e permitir que o usuário escolha quais arquivos deseja renomear.

O bot tem a capacidade de ler o conteúdo dos arquivos PDF para extrair informações como título e autor, caso esses dados não estejam disponíveis no nome do arquivo. Também utiliza heurísticas para deduzir se o arquivo é um e-book ou não, avaliando seu conteúdo independentemente do idioma.
