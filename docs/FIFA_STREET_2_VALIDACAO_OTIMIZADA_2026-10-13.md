# FIFA Street 2 — etapa 1.27: busca de áudio otimizada

Data: 2026-10-13, America/Sao_Paulo. Escopo: custo da validação; não altera
a seleção de samples ou os critérios dos gates de fidelidade.

## Mudança aplicada

`AudioEventNameAssetTextSearchAudit.Search` percorria cada posição do asset
para cada padrão e chamava `SequenceEqual`. A busca agora usa `IndexOf`
sobre spans de bytes para encontrar apenas as ocorrências candidatas.
O [código oficial do .NET 8](https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/SpanHelpers.Byte.cs)
documenta o caminho de busca otimizado, com alternativas vetorizadas e
fallback conforme o hardware. Foi utilizada a API do runtime, sem copiar
essa implementação para o projeto.

O cursor avança um byte depois de cada hit para preservar sobreposições.
Somente posições anteriores à fronteira estável do bloco são contabilizadas;
o tail continua retido para comparar matches entre leituras. Mantêm-se a
busca ASCII exata, os counts, os primeiros oito offsets, o tamanho de bloco
de 1 MiB em produção e o tratamento do final do stream.

O método ficou internal com chunk size configurável exclusivamente para
testar fronteiras pequenas. Não foi criado um novo binding de evento/áudio.
A matriz agora registra `smoke_elapsed_ms`, permitindo medir quais smokes
consomem tempo nas próximas etapas. O probe de material parent também foi
incluído na matriz, mantendo seu caráter de inventário sem gate semântico.

## Testes e medição focal

Self-test: `99/99`, exit `0`; Release `0` warnings/`0` errors. O teste novo
confronta a busca streaming com uma implementação escalar independente em
28 inputs, 7 tamanhos de chunk e 7 padrões: 196 combinações de streams,
com casos vazios, truncados, repetidos, sobrepostos, aleatórios e EOF.
Há ainda um caso cruzando a fronteira de produção de 1 MiB. Counts e os
primeiros oito offsets precisam coincidir exatamente.

A execução focal real, cronometrada incluindo o comando `dotnet run`, levou
`4.995 ms`: 17 nomes, 60/60 assets, 1.737.085.606 bytes, 0 hits.
Isso mede esta auditoria neste computador; não é prazo para terminar o jogo.
Não foi medido um baseline antigo com o mesmo cronômetro, portanto não se
atribui um multiplicador preciso ao ganho nem se extrapola para toda a matriz.

Artefatos idênticos ao resultado anterior, antes da alteração:

| Arquivo | SHA-256 antes e depois |
| --- | --- |
| `audio-event-name-asset-text-search.tsv` | `9B8995864273ADF880CD02BFA2E10F5F08BA8C9F44B8D398D33F1284390A1ADD` |
| `audio-event-name-asset-text-search-summary.tsv` | `04EF71651802C2F093C53312683264E35425BA699BD8A62711657D3D57035BF2` |

O controle independente Python desses relatórios permanece vigente.
`direct_selection_binding=not-proven`, `original_semantics=not-claimed`.

Log da validação completa:
`fifa_pc/stage-1.27-matrix-2026-10-13.log`.

Matriz final concluída com exit `0` e status
`completed-authored-and-candidate-opt-in`: Release 0 warnings/0 errors,
99/99 self-tests, smokes relevantes, runtime headless/windowed e replays
determinísticos. Cobertura authored: 10 cenários, 777 frames, 54 eventos de
áudio e 1.554 amostras de animação. Nesta matriz a busca levou `3.047 ms`.
O probe confirmou três símbolos e 168 bytes de janelas brutas.
Readiness global `NOT_READY`; nenhum gate de fidelidade foi relaxado.
ISO: 2013003776 bytes, Archive, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Trabalho funcional seguinte

A prioridade de rendering continua a fonte nova
`data/initstaz.big!initstay.big!mat.o`, sua definição de
`ParentRM_FifastCharacterSkin`, as declarações/programas e seus consumidores.
A otimização reduz custo de repetir a validação obrigatória, mas não fecha
as lacunas de shader/atributo/UV/material, áudio original ou deformação.
