# FIFA Street 2 — etapa 1.29: runner Release direto

Data: 2026-10-13, America/Sao_Paulo. Otimização da validação obrigatória,
sem alteração de gameplay, decoders, critérios de gates ou acesso à ISO.

## Pesquisa e mudança

A [documentação oficial do .NET](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run)
distingue execução de um projeto por `dotnet run` da execução de uma DLL
já compilada por `dotnet arquivo.dll`. A matriz recompila em Release antes
de testar; portanto não precisa reavaliar o projeto a cada smoke/runtime.

`tools/run-clean-room-authored-matrix.ps1` mantém o build obrigatório e
converte somente o prefixo exato já usado por todos os comandos:

```text
dotnet run --project <FifaStreetPc.csproj> -c Release --no-build -- <argumentos>
→ dotnet <bin/Release/net8.0/FifaStreetPc.dll> <argumentos>
```

`tools/clean-room-release-invocation.ps1` é um adaptador puro de arrays;
não executa aplicações, não concatena comandos de shell e não altera arquivos.
Projeto diferente, Debug, flags fora desse prefixo, ausência de separador ou
payload são rejeitados. A matriz também rejeita DLL ausente após build e
propaga qualquer exit code de falha. Nenhum smoke ou comparação foi removido.

Escopo conferido: projeto `OutputType=Exe`, `TargetFramework=net8.0`, sem
`launchSettings.json`. O MSBuild retornou `RunWorkingDirectory` vazio e
`TargetPath=D:\projeto zeta\fifa_pc\bin\Release\net8.0\FifaStreetPc.dll`.
Diretório atual, ambiente, ordem e conteúdo dos argumentos são preservados;
não foram introduzidos overrides de runtime nem execução de XBE.

## Medição focal e testes

Uma comparação local de self-test produziu:

- `dotnet run`: 2.979 ms, exit 0, 100 testes;
- DLL direta: 2.341 ms, exit 0, 100 testes;
- stdout completo idêntico nos dois caminhos.

É uma medição focal de uma amostra por caminho, não uma média controlada,
speedup garantido da matriz ou prazo de conclusão do jogo. A eliminação do
frontend de projeto é repetida em cada invocação, mas o custo dos decoders,
IO, geração de frames e controles independentes permanece.

`tools/test-clean-room-release-invocation.ps1`: 10/10 testes, exit 0.
Dois casos positivos preservam paths com espaços/Unicode, quote literal,
payload múltiplo e `--` dentro dos argumentos; oito negativos conferem
prefixo incompleto e mutação de cada um de seus sete tokens.
Esse teste do runner foi incluído na matriz; não altera a contagem de
100 self-tests da aplicação .NET.

## Validação completa e comparação de artefatos

Log: `fifa_pc/stage-1.29-matrix-2026-10-13.log`. Os resultados finais de
Release, self-test, smokes, headless/windowed, replay e ISO foram conferidos
no encerramento: exit `0`, `completed-authored-and-candidate-opt-in`,
Release 0 warnings/0 errors, 100/100 self-tests da aplicação e 10/10 do
runner. Cobertura authored: 10 cenários, 777 frames, 54 eventos de áudio e
1.554 amostras de animação. Captura Win32 P6 960×540 não uniforme aprovada.
A busca de nomes de áudio levou 2.552 ms nesta matriz.
ISO: 2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Foram preservados os SHA-256 de oito relatórios da etapa 1.28 para confronto
após a execução: declaração, controle de declaração, fontes de índices,
parâmetros EAGL, busca textual de áudio e summary, cobertura integrada e
readiness. As comparações trace/replay de cada cenário continuam obrigatórias.

Confronto final: **8/8 SHA-256 idênticos**, antes e depois do ajuste do runner:

| Relatório em `fifa_pc/` | SHA-256 nas duas execuções |
| --- | --- |
| `eagl-material-declaration-audit.tsv` | `D439A519D72E2F9718EAD9A312CD8C3997C3A60A43EC9398014F966F79488F6B` |
| `eagl-material-declaration-control.tsv` | `70AA5EDE3857520172410B6260EC7671095FAA565027593BAD2882236B1BF286` |
| `eagl-indexed-draw-source-audit.tsv` | `1EBF623119A7C53B883A9820C26EAE1F3614BF884C135F154980B4AD72CB0CBA` |
| `eagl-render-parameter-audit.tsv` | `30FB26CA3856FC0741695C58F2BD1313482BAF4E6F40BDB16C98CEF5B11DE8D6` |
| `audio-event-name-asset-text-search.tsv` | `9B8995864273ADF880CD02BFA2E10F5F08BA8C9F44B8D398D33F1284390A1ADD` |
| `audio-event-name-asset-text-search-summary.tsv` | `04EF71651802C2F093C53312683264E35425BA699BD8A62711657D3D57035BF2` |
| `clean-room-integrated-coverage.tsv` | `44E935DC8D2D02C2C1552389D05830E2387E0C4B6D45B79DD7AD980DEDD7FE08` |
| `project-readiness.tsv` | `D8CB96F8E734D481BDEC68C705ECCBDB9F4B0F44CD35FDAB192BDA1A8A1D8AE4` |

Readiness global permanece `NOT_READY`. A próxima etapa funcional continua
a resolução do parent e o decoder estático das instruções do VS, conforme
[o recorte de rendering](FIFA_STREET_2_MATERIAL_DECLARACAO_2026-10-13.md).
