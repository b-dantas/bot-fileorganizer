# Project Brief

Trata-se de um bot voltado para organização de arquivos. O objetivo principal é ajustar o nome dos arquivos para que seja mais amigável a pesquisa visual através do Windows Explorer. Preferencialmente todos os arquivos devem ficar em um única pasta, semelhante ao padrão monorepo de código fonte.

## Escopo do Projeto

### Objetivo Principal
Desenvolver um bot em C# que padronize nomes de arquivos PDF, especialmente e-books, seguindo o formato "Livro - Autor - Título.pdf".

### Público-Alvo
Usuários que possuem coleções de e-books em PDF e desejam organizar seus arquivos de forma padronizada para facilitar a busca visual.

### Funcionalidades Principais
1. **Seleção de Diretório**: Permitir que o usuário selecione o diretório onde estão os arquivos a serem organizados.
2. **Análise de Arquivos**: Identificar arquivos PDF que não seguem o padrão de nomenclatura.
3. **Extração de Metadados**: Extrair informações como título e autor dos arquivos PDF.
4. **Propostas de Renomeação**: Gerar propostas de novos nomes para os arquivos, seguindo o padrão "Livro - Autor - Título.pdf".
5. **Confirmação do Usuário**: Permitir que o usuário aceite ou rejeite cada proposta de renomeação.
6. **Renomeação de Arquivos**: Renomear os arquivos conforme aprovado pelo usuário.
7. **Histórico de Operações**: Manter um registro das operações realizadas.

### Restrições e Requisitos
1. Respeitar as restrições de nomenclatura de arquivos do Windows.
2. Limitar o tamanho dos nomes de arquivo para evitar problemas com caminhos muito longos.
3. Identificar corretamente se um arquivo PDF é um e-book.
4. Manter a persistência das operações em um arquivo JSON.
5. Interface de console simples e intuitiva.

### Estado Atual
O projeto foi inicializado com uma estrutura básica que implementa as funcionalidades principais, mas ainda requer aprimoramentos na extração de metadados de PDFs e na identificação de e-books.
