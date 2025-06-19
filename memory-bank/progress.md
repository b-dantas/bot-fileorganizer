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

## Novos Recursos a Serem Implementados

- ~~Nova opção para alterar os metadados, atualizando as informações de título e autor dos arquivos que seguem o padrão de nomenclatura. A extração dessas informações é feita diretamente do nome do arquivo.~~ (Implementado na versão 1.1.1)

## Atualização (19/06/2025) - Versão 1.1.1

Foi implementada a funcionalidade para atualizar os metadados dos arquivos PDF que já seguem o padrão de nomenclatura:

1. **Nova Funcionalidade**:
   - Adição de uma nova opção no menu principal: "Atualizar metadados de arquivos padronizados"
   - Implementação da extração de título e autor diretamente do nome do arquivo
   - Atualização dos metadados internos dos arquivos PDF usando a biblioteca iText7
   - Processamento em lotes, seguindo o mesmo padrão das outras funcionalidades
   - Registro das operações no histórico

2. **Alterações Realizadas**:
   - **PdfAnalyzer.cs**: Adicionado método `UpdateMetadata` para atualizar os metadados de um arquivo PDF
   - **FileOrganizerService.cs**: Adicionados métodos para listar arquivos padronizados, extrair informações do nome do arquivo e atualizar metadados
   - **Program.cs**: Adicionada nova opção no menu principal e implementado método `AtualizarMetadados`
   - Atualização da versão do aplicativo para 1.1.1

## Atualização (19/06/2025) - Correção de Dependência

Foi corrigido um problema de dependência relacionado à biblioteca iText7:

1. **Problema Identificado**:
   - Erro ao executar o método `UpdateMetadata` devido à falta da dependência do BouncyCastle, necessária para operações criptográficas da biblioteca iText7
   - Mensagem de erro: `System.NotSupportedException: Either com.itextpdf:bouncy-castle-adapter or com.itextpdf:bouncy-castle-fips-adapter dependency must be added in order to use BouncyCastleFactoryCreator`

2. **Solução Implementada**:
   - Adicionada a dependência `itext7.bouncy-castle-adapter` versão 9.2.0 ao projeto
   - Esta dependência é necessária para que a biblioteca iText7 possa realizar operações criptográficas ao manipular arquivos PDF

## Atualização (19/06/2025) - Melhoria na Atualização de Metadados

Foi implementada uma melhoria no método de atualização de metadados:

1. **Problema Identificado**:
   - O método `UpdateMetadata` excluía o arquivo original antes de substituí-lo pelo arquivo com metadados atualizados
   - Isso poderia causar perda de dados caso ocorresse algum erro durante o processo

2. **Solução Implementada**:
   - Modificado o método `UpdateMetadata` para preservar o arquivo original
   - O arquivo com metadados atualizados agora é salvo na mesma pasta com o sufixo "_updated"
   - Uma mensagem é exibida informando o nome do novo arquivo criado
   - Esta abordagem é mais segura, pois mantém o arquivo original intacto

3. **Fluxo da Nova Funcionalidade**:
   - O usuário seleciona a opção "Atualizar metadados de arquivos padronizados" no menu principal
   - O sistema lista os arquivos PDF que seguem o padrão de nomenclatura
   - Os arquivos são processados em lotes de 10
   - Para cada arquivo, o sistema extrai o título e o autor do nome do arquivo e atualiza os metadados do PDF
   - Ao final, é exibido um resumo da operação com o número de arquivos atualizados com sucesso e com erro

O bot agora está ainda mais completo, permitindo não apenas padronizar os nomes dos arquivos, mas também garantir que os metadados internos dos PDFs estejam consistentes com os nomes dos arquivos, facilitando a organização e a busca em leitores de PDF.

## Atualização (19/06/2025) - Expansão da Identificação de Tipos de Documentos

Foi implementada uma expansão na funcionalidade de identificação de tipos de documentos:

1. **Problema Identificado**:
   - O sistema apenas identificava se um arquivo PDF era um e-book ou não
   - Não havia diferenciação entre outros tipos de documentos como revistas, artigos, papers científicos ou jornais

2. **Solução Implementada**:
   - Adicionados novos métodos para identificar diferentes tipos de documentos:
     - `IsArticle`: Identifica se um PDF é um artigo
     - `IsScientificPaper`: Identifica se um PDF é um paper científico
     - `IsNewspaper`: Identifica se um PDF é um jornal
     - `IsMagazine`: Identifica se um PDF é uma revista (incluindo revistas como Time Magazine)
   - Adicionado método `IdentifyDocumentType` que retorna o tipo de documento identificado
   - Atualizado o modelo `FileRecord` para incluir o tipo de documento
   - Atualizada a interface do usuário para exibir o tipo de documento durante o processamento

3. **Melhorias na Confirmação do Usuário**:
   - Implementada confirmação individual para cada operação de atualização de metadados
   - O usuário agora pode ver o tipo de documento e os metadados atuais antes de confirmar a atualização
   - Adicionada persistência das rejeições de atualização de metadados para não ofertar novamente os arquivos rejeitados
   - Melhorado o resumo da operação para incluir o número de arquivos rejeitados pelo usuário

4. **Heurísticas de Identificação**:
   - Implementadas heurísticas específicas para cada tipo de documento, baseadas em:
     - Número de páginas típico para cada tipo de documento
     - Palavras-chave específicas em diferentes idiomas
     - Padrões de formatação comuns (como datas em jornais ou referências em papers)
     - Análise do nome do arquivo

Estas melhorias tornam o bot mais preciso na identificação de diferentes tipos de documentos PDF, permitindo uma melhor organização e categorização dos arquivos. Além disso, o usuário agora tem mais controle sobre o processo de atualização de metadados, podendo tomar decisões informadas para cada arquivo individualmente.

## Atualização (19/06/2025) - Simplificação do Método de Atualização de Metadados

Foi implementada uma simplificação no método de atualização de metadados:

1. **Problema Identificado**:
   - O método `AtualizarMetadados` processava os arquivos em lotes, adicionando complexidade desnecessária ao fluxo
   - A lógica de divisão em lotes tornava a interface menos intuitiva para o usuário

2. **Solução Implementada**:
   - Removido o processamento em lotes do método `AtualizarMetadados`
   - Simplificado o fluxo para processar todos os arquivos padronizados em uma única sequência
   - Mantida a confirmação individual para cada arquivo
   - Substituída a pergunta "Deseja processar os arquivos em lotes?" por "Deseja atualizar os metadados destes arquivos?"
   - Eliminada a lógica de controle de lotes (totalLotes, loteAtual, etc.)

3. **Benefícios da Alteração**:
   - Código mais simples e direto
   - Fluxo mais intuitivo para o usuário
   - Mesma funcionalidade com menos complexidade
   - Melhor experiência do usuário com menos perguntas e decisões

Esta simplificação mantém todas as funcionalidades importantes, como a confirmação individual para cada arquivo, a exibição do tipo de documento e metadados atuais, e o registro de rejeições, mas torna o processo mais direto e fácil de entender para o usuário.

## Atualização (19/06/2025) - Expansão do Padrão de Nomenclatura para Diferentes Tipos de Mídia

Foi implementada uma expansão no padrão de nomenclatura para suportar diferentes tipos de mídia:

1. **Problema Identificado**:
   - O sistema estava configurado para padronizar apenas nomes de e-books com o formato "Livro - Autor - Título.pdf"
   - Não havia suporte para outros tipos de mídia como revistas, artigos, papers científicos e jornais
   - O usuário não tinha a opção de confirmar o tipo de mídia detectado pela heurística

2. **Solução Implementada**:
   - Modificado o método `IsFileNameStandardized` para aceitar diferentes prefixos com base no tipo de documento:
     - "Livro" para e-books
     - "Revista" para revistas
     - "Artigo" para artigos
     - "Paper" para papers científicos
     - "Jornal" para jornais
     - "Documento" para outros tipos
   - Atualizado o método `GenerateProposedName` para usar o tipo de documento identificado pelo PdfAnalyzer
   - Adicionado método `GetPrefixFromDocumentType` para converter o tipo de documento em um prefixo para o nome do arquivo
   - Atualizado o método `ExtractInfoFromFileName` para reconhecer os diferentes prefixos
   - Adicionado método `ExtractDocumentTypeFromFileName` para extrair o tipo de documento do nome do arquivo
   - Implementado método `ConfirmDocumentType` no Program.cs para permitir que o usuário confirme ou altere o tipo de documento detectado

3. **Fluxo de Confirmação de Tipo de Documento**:
   - Durante o processamento de arquivos, o sistema exibe o tipo de documento detectado pela heurística
   - O usuário pode confirmar o tipo detectado ou escolher um tipo diferente
   - O tipo confirmado é usado para gerar o novo nome do arquivo
   - O novo nome segue o padrão "[Tipo] - Autor - Título.pdf"

4. **Benefícios da Alteração**:
   - Melhor organização dos arquivos por tipo de mídia
   - Maior precisão na categorização de documentos
   - Mais controle para o usuário sobre o processo de renomeação
   - Suporte para uma variedade maior de tipos de documentos

Esta expansão torna o bot mais versátil, permitindo a padronização de nomes de arquivos para diferentes tipos de mídia, não apenas e-books. Além disso, a confirmação do tipo de documento pelo usuário melhora a precisão da categorização e dá mais controle sobre o processo de renomeação.

## Atualização (19/06/2025) - Melhoria no Fluxo de Processamento de Arquivos

Foi implementada uma melhoria no fluxo de processamento de arquivos para tornar a experiência do usuário mais intuitiva:

1. **Problema Identificado**:
   - O método `ProcessarArquivos` primeiro solicitava a confirmação do tipo de documento para todos os arquivos do lote
   - Depois, em um segundo loop, exibia as propostas de renomeação para cada arquivo
   - Isso criava uma experiência fragmentada, onde o usuário precisava lembrar quais tipos havia confirmado para cada arquivo

2. **Solução Implementada**:
   - Modificado o método `ProcessarArquivos` para processar cada arquivo completamente antes de passar para o próximo
   - Para cada arquivo:
     1. O usuário confirma o tipo de documento
     2. Imediatamente após, vê a proposta de renomeação para o mesmo arquivo
     3. Decide se aceita ou rejeita a proposta
     4. Só então passa para o próximo arquivo
   - Substituído o processamento em lote por processamento individual, mantendo a organização em lotes

3. **Benefícios da Alteração**:
   - Fluxo mais natural e intuitivo para o usuário
   - Feedback imediato: o usuário vê o resultado da sua escolha de tipo de documento na proposta de renomeação
   - Menos confusão: o usuário não precisa lembrar quais tipos de documentos confirmou para cada arquivo
   - Experiência mais coesa: cada arquivo é processado completamente antes de passar para o próximo

Esta melhoria torna o processo de renomeação mais direto e fácil de entender, proporcionando uma melhor experiência ao usuário sem perder nenhuma funcionalidade.
