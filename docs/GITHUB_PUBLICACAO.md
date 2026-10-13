# Publicação do Fut Street 2 no GitHub

Objetivo público vigente: **recompilação estática de FIFA Street 2 do Xbox
original para PC**, não uma recriação do gameplay. Estado: `NOT_READY`;
a recompilação do código original ainda não foi implementada nem validada.
Veja [direção vigente](FUT_STREET_2_DIRECAO_RECOMPILACAO.md).

Conteúdo atual do repositório: protótipo authored .NET, ferramentas próprias
de análise/teste e documentação de continuidade. Não é uma distribuição do
jogo original nem um port recompilado já funcional. Os comandos abaixo
validam a base .NET existente, não a execução do código original.

## Arquivos que não são publicados

A lista permitida em `.gitignore` exclui ISO, executáveis, assets originais,
exports, áudio/imagens, relatórios TSV/CSV, logs, builds, caches e outros
projetos presentes no mesmo workspace. Nenhum desses arquivos é apagado
localmente. `.gitattributes` preserva os bytes/line endings dos fontes para
manter a rastreabilidade dos hashes históricos.

O repositório não recebe automaticamente uma licença de código aberto:
a escolha de licença fica com o titular do projeto. Assets do jogo original
não estão incluídos nem recebem qualquer licença por este repositório.

## Build e testes sem ISO

Requer .NET SDK 8. Na raiz do checkout:

```powershell
dotnet build fifa_pc/FifaStreetPc.csproj -c Release
dotnet fifa_pc/bin/Release/net8.0/FifaStreetPc.dll self-test
pwsh -NoProfile -File tools/test-clean-room-release-invocation.ps1
```

Os self-tests .NET usam fixtures próprias e não precisam da ISO. O host
windowed usa Windows; os smokes de dados/runtime e as ferramentas Python
que inspecionam o jogo exigem os arquivos locais correspondentes. Capstone
é uma dependência das análises estáticas Python; não é usado para emulação.

## Testes que dependem do workspace local

Não enviar a ISO ao GitHub. Para executar os testes que exigem dados do
jogo, forneça localmente a imagem preservada nos caminhos esperados ou use
os parâmetros disponíveis dos scripts. O README e os handoffs documentam
o workspace original. O SHA-256 esperado da ISO é:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

A matriz atual também consome relatórios intermediários já gerados no
workspace; um clone só dos fontes não contém esses relatórios. Não alegar
que a matriz completa funciona num clone vazio sem preparar essas entradas.
Build e self-test independentes devem passar antes de publicar.

Nenhum code review ou pull request faz parte desta publicação.

## Publicação verificada — 2026-10-13

Repositório **público**, conforme escolha explícita do usuário:
[MarllonMenezes/fut-street-2](https://github.com/MarllonMenezes/fut-street-2),
branch `main`, remote local `origin` via HTTPS. Primeiro snapshot completo
publicado: `2d396380ea6506a569ea6ef8ea3606d185268125`. Identidade de commit
usa o endereço GitHub noreply da conta, não o e-mail pessoal do Git global.

Pacote completo: 304 arquivos de código, testes e documentação. Uma primeira
checagem source-only detectou que a lista permitida omitira os dois arquivos
`Program.cs` da raiz dos projetos; a lista foi corrigida antes do envio,
sem alterar esses fontes. O snapshot completo foi extraído com `git archive`
em pasta temporária isolada, sem ISO/assets/relatórios, e passou em:

- Release: 0 warnings/0 errors.
- Self-tests .NET: 105/105.
- Testes do runner Release: 10/10.

No workspace original, testes Python passaram 57/57. Estes incluem leituras
da ISO local; não se alega que todos sejam independentes da ISO. Os bytes e
metadados da ISO foram novamente conferidos e permaneceram iguais ao esperado.
Tipos/tamanhos de arquivos e assinaturas conhecidas de credenciais foram
checados antes do envio; não foram publicados binários ou dumps do jogo.

O upload não fecha gates do jogo nem altera o estado `NOT_READY`. Os logs e
relatórios de validação anteriores permanecem apenas no workspace original.
