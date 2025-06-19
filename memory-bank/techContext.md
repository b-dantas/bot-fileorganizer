# Tech Context

Aplicação Console .NET 9, onde o usuário pode interagir com o bot através da seleção de comandos.

A persistência das alterações realizadas deve estar em um arquivo JSON.

## Detalhes Técnicos Atuais

### Tecnologias Utilizadas
- .NET 9
- C# 12
- System.Text.Json para serialização/deserialização JSON

### Arquitetura
O projeto segue uma arquitetura simples, com separação de responsabilidades:

1. **Camada de Apresentação**:
   - Interface de console implementada em `Program.cs`
   - Menus interativos para navegação do usuário

2. **Camada de Serviços**:
   - `FileOrganizerService`: Coordena as operações de análise e renomeação de arquivos
   - `PdfAnalyzer`: Responsável pela extração de informações de arquivos PDF

3. **Camada de Dados**:
   - `FileRepository`: Gerencia a persistência das operações em arquivo JSON
   - Utiliza System.Text.Json para serialização/deserialização

4. **Modelos**:
   - `FileRecord`: Representa um registro de arquivo com informações sobre o nome original e o novo nome proposto

### Fluxo de Dados
1. O usuário seleciona um diretório para análise
2. O sistema lista os arquivos PDF que não seguem o padrão de nomenclatura
3. O usuário opta por processar os arquivos
4. O sistema analisa cada arquivo e propõe um novo nome
5. O usuário aceita ou rejeita cada proposta
6. As operações são registradas em um arquivo JSON

### Pendências Técnicas
- Integração com biblioteca de manipulação de PDFs (iTextSharp)
- Implementação de extração real de metadados de PDFs
- Aprimoramento das heurísticas para identificação de e-books
