# Publicação do Fut Street 2 no GitHub

Escopo do repositório: fontes clean-room .NET, ferramentas próprias de
análise/teste e documentação de continuidade. Estado do projeto: `NOT_READY`.
Não é uma distribuição do jogo original nem execução/recompilação do XBE.

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
