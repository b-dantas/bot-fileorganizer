# Bot para Organização de Arquivos

Este projeto é um bot desenvolvido em .NET que facilita a organização de arquivos com base em regras configuradas.

## Funcionalidades

- Classificação automática de arquivos
- Configuração personalizável
- Análise de PDFs
- Registro de configurações e progresso

## Como usar

1. Clone este repositório
2. Instale os pacotes NuGet necessários
3. Configure as regras no arquivo `AppSettings.cs`
4. Execute a aplicação via CLI

## Estrutura do projeto

- `Data/`: Camadas de acesso aos dados
- `Models/`: Classes de entidades
- `Services/`: Lógica de negócio
- `memory-bank/`: Documentação técnica

## Instruções de instalação

```bash
dotnet build
dotnet run --project bot-fileorganizer.csproj
```

## Configuração

1. Edite o arquivo `AppSettings.cs` para configurar:
   - Caminhos de origem e destino
   - Regras de organização
   - Intervalos de processamento

2. Atualize o `memory-bank` para refletir mudanças:

```bash
dotnet tool install --global MemoryBankCLI
```

## Desenvolvimento

Este projeto utiliza:

- .NET Framework 5.0
- Entity Framework Core
- Markdown para documentação
- Markdownlint para formatação

## Autor

Desenvolvido por BotFileOrganizer © 2025
