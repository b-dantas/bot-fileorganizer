# Tech Context

Aplicação Console .NET 9, onde o usuário pode interagir com o bot através da seleção de comandos.

A persistência das alterações realizadas deve estar em um arquivo JSON.

## Detalhes Técnicos Atuais

### Tecnologias Utilizadas

- .NET 9
- C# 12
- System.Text.Json para serialização/deserialização JSON
- iText7 para manipulação de arquivos PDF
- iText7.bouncy-castle-adapter para operações criptográficas

### Arquitetura

O projeto segue uma arquitetura simples, com separação de responsabilidades:

1. **Camada de Apresentação**:
   - Interface de console implementada em `Program.cs`
   - Menus interativos para navegação do usuário

2. **Camada de Serviços**:
   - `FileOrganizerService`: Coordena as operações de análise, renomeação e atualização de metadados de arquivos
   - `PdfAnalyzer`: Responsável pela extração de informações e identificação de tipos de documentos PDF

3. **Camada de Dados**:
   - `FileRepository`: Gerencia a persistência das operações em arquivo JSON
   - `AppSettingsRepository`: Gerencia a persistência das configurações do aplicativo
   - Utiliza System.Text.Json para serialização/deserialização

4. **Modelos**:
   - `FileRecord`: Representa um registro de arquivo com informações sobre o nome original, novo nome proposto, tipo de documento e status de operações
   - `AppSettings`: Armazena configurações do aplicativo, como o último diretório utilizado

### Fluxo de Dados

1. O usuário seleciona um diretório para análise
2. O sistema lista os arquivos PDF que não seguem o padrão de nomenclatura
3. O usuário opta por processar os arquivos em lotes
4. O sistema analisa cada arquivo, identifica seu tipo (e-book, revista, artigo, etc.) e propõe um novo nome
5. O usuário aceita ou rejeita cada proposta individualmente
6. As operações são registradas em um arquivo JSON
7. Arquivos rejeitados são marcados para não serem ofertados novamente
8. O usuário pode optar por atualizar os metadados de arquivos já padronizados
9. O sistema extrai informações do nome do arquivo e atualiza os metadados internos do PDF
10. O arquivo original é preservado e uma nova versão com metadados atualizados é criada

### Recursos Técnicos Implementados

- Integração com biblioteca iText7 para manipulação de PDFs
- Extração real de metadados de PDFs (título, autor)
- Heurísticas avançadas para identificação de diferentes tipos de documentos:
  - E-books: baseado em número de páginas, palavras-chave e conteúdo
  - Revistas: identificação de padrões como "Vol. X, No. Y", palavras-chave específicas
  - Artigos: análise de tamanho e conteúdo
  - Papers científicos: identificação de estrutura acadêmica (abstract, introduction, conclusion)
  - Jornais: detecção de formatos de data e palavras-chave relacionadas
- Processamento em lotes para melhor performance e experiência do usuário
- Persistência de preferências do usuário (rejeições de renomeação e atualização de metadados)
- Atualização segura de metadados com preservação do arquivo original
- Confirmação individual para cada operação de alteração de arquivo
