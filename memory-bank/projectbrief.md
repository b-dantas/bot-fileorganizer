# Project Brief

Trata-se de um bot voltado para organização de arquivos. O objetivo principal é ajustar o nome dos arquivos para que seja mais amigável a pesquisa visual através do Windows Explorer. Preferencialmente todos os arquivos devem ficar em uma única pasta, semelhante ao padrão monorepo de código fonte.

## Escopo do Projeto

### Objetivo Principal

Desenvolver um bot em C# que padronize nomes de arquivos PDF, especialmente e-books, seguindo o formato "Livro - Nome Autor - Título.pdf".

### Público-Alvo

Usuários que possuem coleções PDFs e desejam organizar seus arquivos de forma padronizada para facilitar a busca visual.

### Funcionalidades Principais

1. **Seleção de Diretório**: Permitir que o usuário selecione o diretório onde estão os arquivos a serem organizados.
1. **Análise de Arquivos**: Identificar arquivos PDF que não seguem o padrão de nomenclatura.
1. **Extração de Metadados**: Extrair informações como título e autor dos arquivos PDF.
1. **Propostas de Renomeação**: Gerar propostas de novos nomes para os arquivos, seguindo o padrão "Livro - Autor - Título.pdf".
1. **Confirmação do Usuário**: Permitir que o usuário aceite ou rejeite qualquer operação que envolva alteração do arquivo, seja renomeação ou mudança de metadados.
1. **Histórico de Operações**: Manter um registro das operações realizadas.
1. **Identificação de Tipos de Documentos**: Identificar diferentes tipos de documentos PDF (e-books, revistas, artigos, papers científicos, jornais).
1. **Atualização de Metadados**: Atualizar os metadados internos dos arquivos PDF com base no nome padronizado.

### Restrições e Requisitos

1. Respeitar as restrições de nomenclatura de arquivos do Windows.
2. Limitar o tamanho dos nomes de arquivo para evitar problemas com caminhos muito longos.
3. Identificar corretamente se um arquivo PDF é um e-book, uma revista (incluindo revistas como Time Magazine), um artigo, um paper científico ou um jornal.
4. Manter a persistência das operações em um arquivo JSON.
5. Interface de console simples e intuitiva.
6. Processar arquivos em lotes para melhor controle e performance.
7. Preservar arquivos originais ao atualizar metadados, criando novas versões com sufixo "_updated".
8. Permitir confirmação individual para cada operação de atualização de metadados.
9. Persistir preferências do usuário, como rejeições de renomeação ou atualização de metadados.
