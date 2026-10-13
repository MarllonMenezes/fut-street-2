# Handoff — FIFA Street 2 clean-room em .NET

## Direção vigente — 2026-10-13

O objetivo confirmado é **recompilação estática do código original para PC**,
não recriar o jogo em .NET. Leia primeiro
[direção e limites](docs/FUT_STREET_2_DIRECAO_RECOMPILACAO.md).
O título e os registros abaixo são históricos. O protótipo .NET não é o
jogo recompilado; XBE ainda não recompilado, readiness `NOT_READY`.
Stack do núcleo e política de GPU/áudio pendem de confirmação.

## Etapa 1.07 — controle independente de nomes EVT em todos os assets de áudio — 2026-10-08

Foi implementado `fifa_pc/Runtime/AudioEventNameAssetTextSearchAudit.cs` e integrado o comando `audio-event-name-asset-text-search-audit` à matriz, ao self-test e ao `ProjectReadinessAudit`. A auditoria reparseia os `17` nomes candidatos do índice textual de `data/audio/eventspa.evt` e busca cada nome como ASCII exato em todos os `60` assets restantes de `data/audio`, em streaming: `60/60` válidos, `1.737.085.606` bytes, `1` fonte excluída, `0` pares asset/nome com hit e `0` ocorrências.

O controle independente `tools/analysis/audio_event_name_asset_text_search_control.py` reparseia as `1.020` linhas do relatório (`60` assets × `17` nomes), confirma `1.020/1.020` linhas válidas e sem hit, `0` duplicidades e reconciliação exata do summary. É evidência estrutural negativa de namespace, não seleção de ABKC/AST/BNK, sample, evento ou playback; `semantic_binding=not-proven`, `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. O áudio continua parcialmente integrado, o rendering continua `not-proven`, animação-deformação `structural-only` e readiness `NOT_READY`.

Validação fechada: matriz `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `93/93`, smokes relevantes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras. Readiness final desta etapa: `NOT_READY`; assets `candidate-integrated`, rendering `not-proven`, gameplay `clean-room-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`; XBE não executado nem recompilado.

Hashes SHA-256: `AudioEventNameAssetTextSearchAudit.cs` `439D5AB62558278E5CC80CA41AB7CAED8EC7C6DC86ED46EF518363E0E3FB20F5`; controle Python `4BB18D4534BFD5E57F568DAA0627CACDF0F2AA9DE7CEBD46AB61C1C03552F90C`; relatório `9B8995864273ADF880CD02BFA2E10F5F08BA8C9F44B8D398D33F1284390A1ADD`; summary `04EF71651802C2F093C53312683264E35425BA699BD8A62711657D3D57035BF2`; controle TSV `B745AED8D07629B5000183DE9B35C3B59544193422AD1B73825265C4B4351961`; controle summary `886FCD7A7E36E23D56D175D4D111F7C0E38F74708525A79ECACF03397D860174`; `ProjectReadinessAudit.cs` `669299F9947DA9247FBCB8C1884193C4783D8A65C214EBE7F4025A0DA0B86140`; `SelfTestRunner.cs` `654130E57F3CAFD134348A9F5F3A487D1336E412CEB135983887F018F78C3FCB`; matriz `C8E30DE579EDCFFD9A2360815010AFAC519361E69E9CB4F46F7963077C783342`; readiness `23FC205E7488ECBFCB2F0FF0A5DE25A5F13EAA879E8AFACFD5EAE262348F0FF2`.

Próximo avanço: após a matriz, continuar procurando evidência discriminativa `EVT→ABKC→sample→playback`, `state→draw/UV/material` ou timing/pose/rig/deformação, sem inventar bindings e sem concluir o projeto.

## Etapa 1.06 — controle independente state/fetch + slot numérico + textura/raster candidata — 2026-10-08

Foi implementado `tools/analysis/eagl_original_state_slot1_candidate_uv_ambiguity_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia independentemente `2364` linhas de state/fetch e `11820` linhas de papéis numéricos: a junção global cobre `2364/2364` primitivas; no subconjunto dos candidatos há `218` grupos, `216` completos e `2` incompletos, `180` grupos com `SHAPENAME` e `178` completos. Todos os `178/178` grupos SHAPENAME completos têm o perfil near-unit observado no slot `1`, `0` em outros slots; a compatibilidade candidata textura/atributo e a influência raster são `24/24`; vínculos estado→geometria únicos provados: `0`.

É um controle estrutural negativo, não uma identificação de UV: não chama o slot `1` de UV, não estabelece state→draw/material e não promove textura/raster candidata para semântica original. O subgate `rendering-original-state-slot1-candidate-uv-ambiguity-control` está `verified-structural-negative`, `24/24`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; rendering continua `not-proven` e o projeto `NOT_READY`.

Validação: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, runtimes headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras de animação. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256: controle `FCFF267936F6B03FD94174600D443424CEDD399E9634B253DD20E511D25B6449`; relatório `A61381C76D19CCCFBD995EBB928770B975F0EF923A383F8192ADCADC36F6ACE1`; summary `BFAF6A5AF309D92E0CAB5BBABB6270BA0D6326E3E516914E8C3B5079BFBD5C17`; `ProjectReadinessAudit.cs` `20099B92DC1BCA0F58F577A1A8EF7ADB3BE9F2BFDBD172F04A09B0EC774DE363`; matriz `CD626E7E0D90FC4F8BEE016C20978E6F69CE69BB7D0DF12F704609DC2853BEDC`.

Próximo avanço: evidência original discriminativa `state→draw` com UV/material ou `EVT→ABKC→sample→playback`; este controle não fecha gate semântico.

## Etapa 1.05 — controle independente de ambiguidade nominal FIFAST2.csi↔EVT — 2026-10-08

Foi implementado `tools/analysis/fifast2_csi_evt_name_ambiguity_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia independentemente os `100` nomes de `FIFAST2.csi` e os `17` candidatos nomeados de `eventspa.evt`, com tokenização determinística de snake_case/camelCase/acrônimos: `100/100` linhas CSI e `17/17` EVT válidas, `17` nomes EVT únicos; `10` EVT sem sobreposição, `3` com uma sobreposição nominal, `4` ambíguos e `7` com alguma sobreposição. Não houve nome exato e `0` bindings únicos provados.

É controle de nomes, não binding: não atribui significado ao campo opaco CSI, não escolhe ABKC/AST/BNK ou amostra, não liga gameplay a playback e não promove `Goal`, `GB`, `Tackle` ou `Panna` por semelhança textual. O subgate `audio-fifast2-csi-evt-name-ambiguity-control` está `verified-structural-negative`, `17/17`, com `semantic_binding=not-proven`, `direct_selection_binding=not-proven` e `original_semantics=not-claimed`; readiness permanece `NOT_READY`, e os gates principais de assets/rendering/áudio/animação continuam não passados.

Validação da etapa: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, runtime headless/windowed, replays determinísticos e SHA-256 da ISO verificados; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras de animação. A ISO permaneceu somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256 dos artefatos da etapa: controle `FA75AD1D77492032A54992A8611A5DDF7D75DB046EC2D82948D417653E88A942`; relatório `E334171FE7B0BC9A417895C8424A6FC132332BDDF5AA051132A1D20B8C059098`; summary `0C23206306F0959DE7AA035F91F838074E8F278E614C71F6D3F2F9292CC7444F`; `ProjectReadinessAudit.cs` `3366D1C53283818812B2CE3119339CE40D274C3A28CE186FB14893C4B3B685C2`; matriz `97D982C73A045366B774750550370E76357A7247C0ADD692068381D2DFD55A46`.

Próximo avanço: buscar evidência discriminativa `state→draw` com UV/material independente ou `EVT→ABKC→sample→playback`; a sobreposição nominal permanece apenas controle negativo.

## Etapa 1.04 — coocorrência independente command-descriptor e papéis numéricos — 2026-10-08

Foi implementado `tools/analysis/eagl_original_command_slot_role_cooccurrence_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. A junção por primitiva entre o command-descriptor original e os papéis numéricos fechou `2364/2364` linhas e `2364/2364` primitivas com ordinal presente, coincidente e unicamente identificado; não houve match de ordinal deslocado. Os papéis preservam `2364` grupos de cinco linhas, `2362` grupos completos, `2` incompletos, `2362/2362` perfis float2 de faixa unitária no slot bruto `1` e `0` em outro slot.

É controle estrutural negativo: não chama slot de UV, posição, material, atributo, geometria, draw ou GPU e não estabelece skinning, rigging, animação ou morph. O subgate `rendering-original-command-slot-role-cooccurrence-control` está `verified-structural-negative`, `2362/2362`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; o gate principal de rendering e o readiness continuam `NOT_READY`.

Validação da etapa: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, runtime headless/windowed, replays determinísticos e SHA-256 da ISO verificados; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras de animação. A ISO permaneceu somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256 dos artefatos da etapa: controle `CEB60207BDEE7450D2C77A13B9BD0DF09047CAA7AFA81B4810023A97E575FA512`; relatório `5ABFB9D19F0E031B49CF8F62A0179A0CDD8D0E740290BCAEAA399883363D5CB3`; summary `6ACCCC0125CE33344EA73C98576C8BFB2A043FB5683BB6C4D9C71A81762CF6C`; `ProjectReadinessAudit.cs` `C7334F270FC4C846B570C2D00D710D76C4D375C670DF5879E8B2B5EF2592E2EA`; matriz `33143D52276B6C78094934E72CC56BD15C013B80C82F763114B15BEA7809FE6C`.

Próximo avanço: buscar uma associação original discriminativa state→draw com UV/material independente ou EVT→ABKC→sample→playback; não promover a coocorrência atual.

## Etapa 1.03 — coocorrência independente estado/fetch e papéis numéricos por primitiva — 2026-10-08

Foi implementado `tools/analysis/eagl_original_state_numeric_slot_cooccurrence_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle une independentemente o relatório original de estado/fetch ao controle de papéis numéricos por `(model_archive, model_object, layer, primitive)`: `2364/2364` linhas unidas, `2364` grupos, `2362` grupos completos de cinco slots e `2` grupos incompletos preservados. Entre os grupos completos, `2362/2362` têm exatamente um perfil float2 de faixa unitária no slot bruto `1`, e `0` têm o perfil em outro slot; `752` linhas têm estado `SHAPENAME` e `1612` têm `SetPrimitiveType`.

Esta coocorrência é apenas controle estrutural negativo: não associa estado a draw, geometria, UV, material, skinning, rigging, animação, morph ou GPU. O subgate `rendering-original-state-numeric-slot-cooccurrence-control` está `verified-structural-negative`, `2362/2362`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; os gates principais permanecem não passados e o readiness continua `NOT_READY`.

Validação da etapa: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, runtime headless/windowed, replays determinísticos e SHA-256 da ISO verificados; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras de animação. A ISO permaneceu somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Próximo avanço: buscar associação discriminativa state→draw/UV/material ou a cadeia EVT→ABKC→sample→playback; qualquer resultado estrutural sem seleção única deve permanecer como controle e não fechar os gates.

## Etapa 1.02 — discriminação independente de papéis numéricos por slot de primitiva — 2026-10-08

Foi implementado `tools/analysis/eagl_original_numeric_slot_role_discrimination_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle faz uma junção independente, por chave original `model_archive/model_object/symbol_name`, entre `eagl-original-vertex-stream-numeric.tsv` e `eagl-original-primitive-stream-topology.tsv`: `11820/11820` linhas unidas, `2364` grupos de primitiva, `2362` grupos completos de cinco slots e `2` grupos incompletos preservados. Nos grupos completos, `2362/2362` têm exatamente um perfil float2 de faixa unitária no slot bruto `1`, `0` grupos têm esse perfil em outro slot; `2361` têm correspondência de bounds no slot `0` e `1` não tem. O histograma de slot do perfil é `1=2364` considerando também os dois grupos incompletos.

Esta é evidência estrutural de cardinalidade/perfil e controle negativo, não uma promoção para UV, posição, material, atributo, geometria, skinning, rigging, animação, morph ou GPU. O novo subgate `rendering-original-numeric-slot-role-discrimination-control` está `verified-structural`, `2362/2362`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; os gates principais de assets/rendering/áudio/animação permanecem não passados e o readiness continua `NOT_READY`.

Validação da etapa: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, runtime headless/windowed, replays determinísticos e SHA-256 da ISO verificados; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras de animação. A ISO permaneceu somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Próximo avanço: buscar evidência independente de state→draw e UV/material ou, em paralelo, um encadeamento EVT→ABKC→sample→playback; qualquer resultado sem associação discriminativa deve permanecer como controle estrutural e não fechar os gates.

## Etapa 1.01 — controle independente do universo completo de dados para hashes EVT — 2026-10-08

Foi implementado `fifa_pc/Runtime/AudioEventDataAssetHashSearchAudit.cs` e integrado o comando `audio-event-data-asset-hash-universe-audit` à matriz, ao self-test e ao `ProjectReadinessAudit`. O controle varre todos os assets em `data`, excluindo somente `data/audio/eventspa.evt`, e reparseia os 17 candidatos do índice textual como palavras little-endian alinhadas: `121/121` assets válidos, `2.008.984.464` bytes, `1` linha-fonte excluída e `1` ocorrência alinhada em `data/movies/advoffn.vp6`, hash `0x34214F6B`, nome `3panna_goal`, offset `0xA58D88`.

O controle independente `tools/analysis/audio_event_data_asset_hash_universe_control.py` reconcilia exatamente os `121/121` caminhos, bytes e o hit único. A ocorrência está em payload comprimido VP6 de filme; ela é evidência estrutural de presença de palavra/hash e não prova áudio, ABKC, sample, evento ou playback. Portanto `direct_selection_binding=not-proven` e `original_semantics=not-claimed`; nenhum binding foi promovido. Os subgates `audio-event-data-asset-hash-universe-control` e `audio-event-data-asset-hash-universe-independent-reparse` estão `verified-structural`, `121/121`, enquanto assets, rendering, áudio e animação-deformação permanecem não passados, gameplay clean-room continua passado e readiness segue `NOT_READY`.

Validação da etapa: matriz completa exit code `0`, build Release `0/0`, self-test `92/92`, smokes relevantes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras de animação. A ISO permaneceu somente leitura, com `2013003776` bytes, atributo `Archive` e SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Não houve execução ou recompilação do XBE.

Próxima etapa: continuar apenas com evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação; não inventar bindings nem marcar o projeto como concluído.

## Etapa 1.00 — controle independente do universo completo de assets de áudio — 2026-10-08

Foi implementado `fifa_pc/Runtime/AudioEventAudioAssetHashSearchAudit.cs` e integrado o comando `audio-event-audio-asset-hash-universe-audit` à matriz, ao self-test e ao `ProjectReadinessAudit`. O controle reparseia os 17 candidatos derivados do índice textual de `eventspa.evt` como palavras little-endian alinhadas e varre todos os assets em `data/audio`, excluindo somente o EVT-fonte: `60/60` assets válidos, `1.737.085.606` bytes, `0` assets com hits, `0` pares asset/hash e `0` ocorrências alinhadas. Relatórios: `fifa_pc/audio-event-audio-asset-hash-universe.tsv` e `fifa_pc/audio-event-audio-asset-hash-universe-summary.tsv`.

O subgate `audio-event-audio-asset-hash-universe-control` está `verified-structural-negative`, `60/60`. Isto é um controle negativo estrutural independente sobre o universo de assets; não seleciona ABKC/AST/BNK/sample, não estabelece evento de gameplay ou playback e não promove semântica. `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Os gates principais permanecem assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` não passado, animação-deformação `structural-only` não passada e readiness `NOT_READY`.

Validação da etapa: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, build Release `0/0`, self-test `91/91`, smokes relevantes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. SHA-256: `AudioEventAudioAssetHashSearchAudit.cs` `9C0E3E6B34974BF2EB6BF04096CEE540934307A0788DF2AEFFC1FEFFCAF9FE05`, `Program.cs` `766DC833320707B891A75A559D337F2A2DF3BCD5270B1569C7C1DB3840B38529`, `SelfTestRunner.cs` `D13FA778813CCB60BB0122C0043F6B74954FE95348B526950867477EC089014F`, `ProjectReadinessAudit.cs` `5190DDECF723EBA9085FC8B4674414539F32C4630359E522727F3736E9B1DFF5`, matriz `D188A79CAA07E6299E5FCAA1268F6972B822ABE840128CB43D9A15C2632D6D90`, relatório `F5E9D9DF191173DAFEB5BC05D69FFA1075E18CF24CBAE194BACC85D730837002`, summary `DE86B53F51D842D1DD15799FC535CF4F1D2BD337D512786014850F9957C45DA2` e readiness `55DBF81BD71989DE4BD55F6249045C46EBD8C3BE6FD2DC01A8D47503F87C48AA`. A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar somente com evidência discriminativa para EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação; não inventar bindings, não executar/recompilar o XBE e não marcar o projeto como concluído.

## Etapa 0.99 — discriminação independente EVT→PCP — 2026-10-08

Foi implementado `tools/analysis/audio_event_evt_pcp_range_discrimination_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia independentemente os TSVs e reconcilia `17/17` nomes do `eventspa.evt` com ranges únicos de `pcpahdr/pcpadat`: `17/17` âncoras de string, ranges de cabeçalho e dados, índices paralelos, prefixos `SCHl`, nomes, offsets de string e linhas de cabeçalho. A junção de runtime também fecha `17/17`, com `0` nomes ausentes. O controle negativo de buscas raw de 32 bits permanece zero para ABK, `pcpahdr` e `pcpadat` (`True` nos três campos).

O novo subgate `audio-event-evt-pcp-range-discrimination-control` está `verified-structural-negative`, `17/17`. Isto prova somente a reconciliação estrutural EVT→ranges PCP e os controles negativos; não prova seleção EVT→ABKC→sample, evento de gameplay, playback, `direct_selection_binding` (`not-proven`) nem `original_semantics` (`not-claimed`). O gate principal de áudio permanece `partially-integrated` e não passado; assets seguem `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, animação-deformação `structural-only` não passada e readiness `NOT_READY`.

Validação: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, build Release `0/0`, self-test `90/90`, smokes relevantes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes SHA-256: controle `6679AC5C2AA0118F8B54D5E51AFFDBEA4446310A75C891D75845FBB960667B0F`, `ProjectReadinessAudit.cs` `0F86927682D97D111B0FE7E74B2A977EA3F7944830D3BBED1E3ECA027A1F948A`, matriz `6375B68D2D377A4CB00D233D1DE6E52C5A6EF95B4E5139324C78E972AEB64E91`, relatório `FD6775F92E5A2FD1C3B065527CA1700EC943D67C299FBE6D4329DA8286E15BD2`, summary `9BD952F8207419933C68BE4BD53A2DC4006A549E637CEDF2947BCE7E7413CE86`, readiness `A270A70B1E9AB64775E5D856460E640F8A7C94EA4C5D1B8B3A08379E0CC516A5`. A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência discriminativa para EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação; não inventar bindings nem executar/recompilar o XBE.

## Etapa 0.98 — identidade independente do payload de geometria candidato — 2026-10-08

Foi ampliado o audit de topologia original com `payload_sha256`, calculado sobre o intervalo `__VB` validado, e foi implementado `tools/analysis/eagl_original_candidate_geometry_payload_identity_control.py`. O controle independente relê quatro TSVs e cruza os `38` grupos de geometria, cada um com cinco slots: `38/38` grupos completos, `190/190` símbolos, metadados e hashes SHA-256 de payload exatos; nenhum key ficou ausente. Status `verified-independent-original-candidate-geometry-payload-identity-control`, `original_semantics=not-claimed`.

O novo subgate `rendering-original-candidate-geometry-payload-identity-control` está `verified-structural-negative`, `38/38`. Isso prova apenas identidade de bytes, símbolos, stride, tamanho, cardinalidade e strip entre os relatórios; não atribui posição, UV, material, skinning, rigging, animação, morph, draw ou semântica GPU. Os gates principais continuam: assets `candidate-integrated` (não passado), rendering `not-proven` (não passado), gameplay clean-room `true`, áudio `partially-integrated` (não passado), animação-deformação `structural-only` (não passado) e readiness `NOT_READY`.

Validação da etapa: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, build Release sem warnings/erros, self-test `90/90`, smokes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes SHA-256: controle `122CD41BEA4F936FDA11A5B504794FA06AF61FDB94A61C7FCD6D5E50A9397830`, `EaglVertexBufferReader.cs` `AF254F024A4F27F22C64B6FC5FC3707CC2576EDFCD7A796ABE8CD9FB5EAE4A61`, auditor de topologia `A519959A04EB908D011ACEE9A803609C39C08174123184603BF9525E8142A379`, `ProjectReadinessAudit.cs` `6CBDE0D1E624161EC5CF6D7B393A8DBCA1C8CC8DB34AF6872F14019F6E14259A`, matriz `FBE00A4CABEBDD8E3322A734095410B4244C1955AE3131C04C41F347EC4DE437`, relatório `C2674850F4CFA7D8A173D28BE87F848893CB9A99ADEFDBE8966E8324CCDC687A`, summary `49ED6FC5AADF625DDF20266083F2FADE7AAC28E8673C1D71166B8FE8D88C6CE1` e readiness `439467451E3F16ABA72F7CEDBB5FBC3CB4409D2C06EA4592E111C5D2593702FF`. A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência discriminativa para state→draw/UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação; não inventar bindings nem executar/recompilar o XBE.

## Etapa 0.97 — controle independente da discriminação do perfil estado→geometria — 2026-10-08

Foi implementado `tools/analysis/eagl_original_state_profile_discrimination_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia independentemente os relatórios de perfil e adjacência, deduplica a identidade `(model_archive, object_name, state_layer, state_primitive)` e recompõe os ties: `269` linhas de perfil, `24` objetos, `38` candidatos de geometria, `180` primitivas de estado, `27` assinaturas distintas e `269/269` matches de tie.

A discriminação não fecha o binding: `89/180` primitivas de estado têm múltiplos candidatos de geometria; `91/180` pertencem a objetos com uma única geometria e não escolhem entre candidatos; `0` têm zero candidatos; `15` grupos de assinatura têm múltiplos candidatos; `0` pares de command shape exatos e `0` bindings únicos estado→geometria foram provados. Status `verified-independent-original-state-profile-discrimination-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Relatórios: `fifa_pc/eagl-original-state-profile-discrimination-control.tsv` e summary.

O `project-readiness.tsv` registra `rendering-original-state-profile-discrimination-control` como `verified-structural-negative`, `24/24`; isso não promove perfil de estado ou objeto de geometria única a state→draw, UV, material, comando ou GPU. Os gates principais permanecem: assets `candidate-integrated`, rendering `not-proven`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes SHA-256: script `10BF27EB1D15145BF046D3B9DA2E35F007E9B24B091DCB57D963FAD7E6A95436`, relatório `58C5C2F672F775F36EA1A5FC7D783664AF5D3AB9593DACFB0BC77DC93D3DE0C8`, summary `899DDE1BB50ED1E09F3559CB7CCAB901131F56CA52F1150D57AE817C959D8AFB`, matriz `740D370CF96A842C64F90EC55C7F3DF97BDE409E584A3FFEFBE6D06CCD250223`, audit `A91895863CBA11E793EBBFC1C7DE82EC1ED6B27351CBA6B60C0639B571015D08`, readiness `70A663983FAFEBB5EB12ED742A0F17A5CA8C8F4CACBAD2F036962012C59724B7`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar apenas com evidência discriminativa para EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação; não inventar bindings nem executar/recompilar o XBE.

## Etapa 0.96 — controle independente da seleção do decoder de bancos de áudio — 2026-10-08

Foi implementado `tools/analysis/audio_bank_decoder_selection_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle junta independentemente o decode em lote limitado de `16` bancos com a varredura exaustiva de `242` linhas ABKC/AST/BNK: `16/16` identidades de banco, `15/15` índices/tipos/bytes de onda dos bancos aceitos e `1/1` política de rejeição foram reconciliados. Resultado: `16/16` controles válidos, status `verified-independent-audio-bank-decoder-selection-control`, `direct_selection_binding=not-proven` e `original_semantics=not-claimed`.

Isto valida somente a consistência do primeiro candidato aceito pelo decoder gerenciado contra a enumeração exaustiva. Não promove essa escolha a seletor original de EVT, identidade de sample, evento de gameplay ou playback. O readiness registra `audio-bank-decoder-selection-control` como `verified-structural-negative`, `16/16`; o gate principal de áudio permanece `partially-integrated`. Assets continuam `candidate-integrated`, rendering `not-proven`, animação-deformação `structural-only`, gameplay clean-room verificado e o projeto permanece `NOT_READY`.

Validação: build Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. SHA-256: auditor `05E354332F47DBFF208E8F4889131A93736B6BCC184F6DDD99229F7DC968B903`, relatório `33E78F74BC593BB05C13DAFDFAA0598D3CB5D28AB4A880FC8E35697C344BA862`, summary `97EBA29D401FFA85DFEC948B31ACA166176A53E64D95441E8A8A279C4C3488B6`, matriz `0EBB914E3422067085AA3F4EB80517B774F1CE427E0048407C14EEB8FECF908E`, `ProjectReadinessAudit.cs` `011E2C23F5DE7360A78345F635ABF46F8A89BE4E5599B5B90DF50B81F796A2FA` e readiness `EF7275DE13DFFC595934E5BB3D95C66CF6ED3E3AD988F237737F6A53110F67EE`. A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar apenas com evidência discriminativa para a cadeia EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação; não inventar bindings nem executar/recompilar o XBE.

## Etapa 0.95 — controle independente da ambiguidade estado→material candidato — 2026-10-08

Foi implementado `tools/analysis/eagl_original_state_candidate_material_ambiguity_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle junta independentemente intervalos de estado originais, tokens `SHAPENAME`, fontes candidatas de textura e cardinalidade de geometria adjacente, sem atribuir a nenhum deles semântica original.

O reparse cobriu `24` objetos candidatos, `218` linhas de renderização, `62` linhas de ordem, `180` linhas de estado e `38` linhas de geometria original. Houve `36` intervalos de estado não vazios: `12` com dois candidatos de geometria adjacentes, `24` com um e `0` com zero. As junções fecharam em `180/180` linhas de estado, `180/180` matches de token `SHAPENAME`, `180/180` fontes candidatas, `24/24` assets candidatos válidos e `24/24` controles de objeto completos. Nenhum binding único estado→geometria ou estado→material foi provado (`0` em ambos); a adjacência é somente cardinalidade, não atribuição de draw, material, UV ou sampling.

Status: `verified-independent-original-state-candidate-material-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. O readiness registra `rendering-original-state-candidate-material-ambiguity-control` como `verified-structural-negative`, `24/24`, sem fechar o gate principal de rendering. Assets continuam `candidate-integrated`, rendering `not-proven`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e readiness `NOT_READY`.

Validação da etapa: build Release `0/0`, self-test `90/90`, matriz completa `completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes SHA-256: script `FDF5A04EF7EBA97DAF7E4D093AEE0E14602AC561090B4B2A67498DE2D9DDA9DB`, relatório `3A596D0D251D8B24995EFE167881F5D1EE2FEC80491AC010DFB9DA35EFA1F832`, summary `A4569B1BD91FA455FF233B0E1931D0B76C8262A5B3A51B1386EA094E85EDD289`, matriz `625910D5E524C3A82E9379E4CB12D26EA4F17CE243C103D892B059E288FAA147`, `ProjectReadinessAudit.cs` `1C316A8C86E7E111DC02D911ACA21C1DC7E81522BE8685257CFF0D5AF0F79A85`, readiness `4A747F9A8F5C0D8286F7CED075C0535A7FB978CBC9D32E7E781D70E37CB1FEB9`. A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência ainda discriminativa para UV/material ou para a cadeia EVT→ABKC→sample→playback e timing/pose/rig/deformação; não promover adjacência, `SHAPENAME` ou textura candidata a binding original.

## Etapa 0.94 — controle independente da classificação original geometria/draw — 2026-10-08

Foi implementado `tools/analysis/eagl_original_geometry_draw_classification_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle junta, por chave de primitiva, o `eagl-original-render-chain.tsv` ao `eagl-candidate-fetch-association.tsv`, cobrindo os `24` objetos candidatos: `218/218` linhas originais foram unidas a `218/218` linhas candidatas, sem chaves faltantes, com `24/24` classificações de objeto completas.

A classificação estrutural coincidiu em `218/218`: `38` linhas marcadas como geometria e `180` como estado tanto na fonte original quanto na associação candidata; em `24/24` objetos há mais estados que geometrias. O controle também registra `state_to_geometry_unique_binding_proven_rows=0`. Status `verified-independent-original-geometry-draw-classification-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Isto classifica apenas registros estruturais; não prova state-run→draw, UV, material, sampling, comando GPU, skinning, rigging, animação ou morph.

O `project-readiness.tsv` registra `rendering-original-geometry-draw-classification` como `verified-structural`, `218/218`, sem fechar o gate principal de rendering. Assets permanecem `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e readiness `NOT_READY`.

Validação pós-etapa: Release `0/0`, self-test `90/90`, matriz completa `completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes: auditor `2A7EDF85FEA1C982429A448294A3B40140CB5C557452349054ABA0CD88D3B7CE`, relatório `4FD523C19F725825862F8DFCCF0AFA626CF60BB3790046FC5EE89D2FC1AF6B6E`, summary `69ECC533AB16C9573B8EB0B18EF6027279DAA3AB28BA3BBCB880D0D080D00C94`, matriz `950D951607B618C2CB59C5005E191E9A3B05FB47BF430CA4B9B2795280553639`, agregador `032C73872F08EAF7916CFD8571CD59C13061BBF66E0FEB0DFFC8C0BEA63E713B`, readiness `0DABB2380CF5EF99F304FE696F7426DDC012884049AB9A4398D64543990BFA85`. A ISO continua somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.93 — controle independente da ambiguidade pose-bank→animation-bank — 2026-10-08

Foi implementado `tools/analysis/animation_morph_pose_bank_ambiguity_control.py`, integrado à matriz, para reconciliar independentemente o índice de pose-bank, os perfis ELF de animation-bank e o audit de relocations de morph. O controle validou `391` linhas de pose-bank, `19/19` animation-banks ELF-válidos com metadados de símbolo e `393/393` layouts de morph parse-válidos; `159` layouts contêm pares de offsets, mas nenhum possui conjunto ou histograma de gaps exatamente correspondente a uma identidade de animation-bank.

O resultado permanece negativo: há apenas `1` nome normalizado genérico (`PosePaletteBank`), nenhuma correspondência de nome com animation-bank, nenhuma correspondência de metadado de símbolo e `0` bindings pose-bank→animation-bank únicos provados. Status `verified-independent-animation-pose-bank-ambiguity-control`, `semantic_binding=not-proven` e `original_semantics=not-claimed`. Isto não prova frame, pose, rig, timing, skinning ou morph original. O `project-readiness.tsv` registra `animation-morph-pose-bank-ambiguity-control` como `verified-structural-negative`, `391/391`, sem alterar o gate principal de animação-deformação.

Validação pós-etapa: Release `0/0`, self-test `90/90`, matriz completa `completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes: auditor `6D06722268A77D919C93E698BBECE14A4D02E30337F3365D46004E8DCDE0297E`, relatório `007E51BF59267432C846B801256C0A356511EDEC5993AFB87362361454106A99`, summary `1CA4359A5B13236426FC93642B7B7F6D69D43A1D35ECB0EB738E2FFDED61CDA8`, matriz `1387E6D9723E3DFC2A04C0825698F640A95FE2A34F744D0F4602B962424F4638`, agregador `DA8118AC61A5217BDED8935AAE586689FDF7878612F3F02936114603B7EE5FB1`, readiness `CB8DF38A2ECA8FFEF203BD488E60794B3DFB285321876B2E778F90ABCD3C42E7`.

O readiness continua `NOT_READY`: gameplay clean-room está verificado, enquanto assets/rendering/áudio/animação-deformação permanecem abertos. A ISO continua somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.92 — controle independente da ambiguidade estado→geometria — 2026-10-08

Foi implementado `tools/analysis/eagl_original_state_geometry_binding_ambiguity_control.py`, integrado à matriz, para reconciliar independentemente os relatórios de adjacência, perfil, ordem e partição. Os `24` objetos foram unidos sem identidade faltante: `269` linhas de perfil, `62` intervalos ordenados e `24` partições; cardinalidade de perfil, cardinalidade de ordem e conjuntos de índices de estado reconciliados em `24/24`. Em `24/24`, a contagem de estados é maior que a de geometrias.

O controle permanece negativo: `geometry_group_state_exact=0`, `exact_command_shape=0` e bindings estado→geometria únicos provados `0/24`. Status `verified-independent-original-state-geometry-binding-ambiguity-control`, `semantic_binding=not-proven` e `original_semantics=not-claimed`. Isto não prova estado→draw, UV, material ou semântica GPU original.

Artefatos: `fifa_pc/eagl-original-state-geometry-binding-ambiguity-control.tsv` e seu summary. O agregador `project-readiness.tsv` agora expõe este controle como `rendering-original-state-geometry-binding-ambiguity-control`, `verified-structural-negative`, `24/24`, sem alterar o gate principal. Validação pós-etapa: Release `0/0`, self-test `90/90`, matriz completa `completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes: auditor `4038AACD60D9D5210E96A9F0EA6DFAF1D5F1D4579683C480AED36BDE3F08E2EB`, agregador `E68D78D5411AAA9DEF72E1047D1F9D5FAB68E91A5E388258A93BF4129991FD6F`, matriz `B078D5F079CBC48E6231BB2DCE7D67ADFA566A53D18E6D56A406AFEAD224BC1A`, relatório `B02761A085DBD5C7996BF40F149B097AA8AC130B77D4A8DE88621870A15AF0FF`, summary `7C60550132683EF27301AC355D40FF317235332F78F376328FDB31677B453206` e readiness `BB120BFB808DF9B0635DB320CCC120180E1E0C658BD24963C88756A74FE387B4`.

O readiness continua `NOT_READY`: gameplay clean-room está verificado, enquanto assets/rendering/áudio/animação-deformação permanecem abertos. A ISO continua somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.91 — controle de influência raster isolada para casos de composição — 2026-10-08

O `CandidateRenderInfluenceAudit` foi ajustado para aceitar, além da comparação
da cena completa, uma comparação isolada do objeto candidato quando composição,
oclusão ou cobertura de outros objetos esconde a diferença no frame agregado.
Isso corrige o critério de observação sem promover semântica original: a
evidência continua sendo somente do caminho candidato gerenciado.

O controle passou em `24/24` objetos com geometria finita/indexada e influência
de textura candidata no raster (`24/24` isolados); `20/24` também diferiram na
cena completa. Houve `24` checksums texturizados e `24` checksums sólidos
distintos. Status `verified-authored`, com `original_semantics=not-claimed`.
Isto não identifica UV, material, sampling, estado, draw ou GPU originais.

O readiness atual registra `assets-candidate-attribute-raster-influence` como
`verified-candidate`, `passed=True`, `observed=24`, `expected=24`. Os gates
principais permanecem abertos: assets `candidate-integrated`, rendering
`not-proven`, áudio `partially-integrated` e deformação `structural-only`;
gameplay clean-room segue verificado e o readiness permanece `NOT_READY`.

Validação: build Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes relevantes, runtimes headless/windowed e replays determinísticos; `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação.
Hashes: relatório `EA487AC1B69906BEF0121B19588643814E118493B3A7ED8094DA134C065AFF14`,
`CandidateRenderInfluenceAudit.cs`
`4C1D43BF7157B782EE56290EA88C71428D37066A5CBA3D2494476E04BBCF12ED`,
`Program.cs` `C0C06AEC1147F9FA8AA699E649551FFD1A99CCC513576AE3F4FCA286AF21236F`,
readiness `D2704B5697D2B8EFBF2F2825CE1DC0A49E36089EED77227E7DD862A8CD3811A1`
e DLL Release `82DC30C3751939707D9B356D6BEDB3141A3B3DB3C40F5C2242D8BA0B7ACA25DA`.
ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — auditoria dinâmica de influência do atributo candidato — 2026-10-08

Foi implementada a auditoria `CandidateAttributeRasterInfluenceAudit` em
`fifa_pc/Runtime/CandidateAttributeRasterInfluenceAudit.cs`, exposta pelo
comando `eagl-candidate-attribute-raster-influence-audit` e integrada à matriz
em `tools/run-clean-room-authored-matrix.ps1`. Para cada um dos `24` objetos
same-trunk, o caminho candidato renderiza um baseline e uma variante com a
perturbação determinística do atributo opaco slot 1 `(x,y) -> (1-x,y)`, usando
o mesmo mesh/textura candidato.

O resultado foi `24/24` com geometria finita/indexada, cardinalidade de
atributo válida e mudança de pixels no raster; houve `24` checksums baseline e
`24` checksums perturbados distintos. Status:
`verified-candidate-attribute-raster-influence`. Isso demonstra somente que o
caminho gerenciado candidato consome esses valores para produzir um raster
texturizado diferente. Não identifica slot 1 como UV, não atribui material ou
sampling e não estabelece binding original de estado, draw ou GPU;
`original_semantics=not-claimed`.

O subgate `assets-candidate-attribute-raster-influence` está
`verified-candidate`, `passed=True`, `observed=24`, `expected=24`. Os gates
principais seguem abertos: assets `candidate-integrated`, rendering
`not-proven`, áudio `partially-integrated`, deformação `structural-only` e
readiness `NOT_READY`; gameplay clean-room continua verificado.

Artefatos e hashes SHA-256: relatório
`67F2B35CEA616716987E59442C25DF5C641D3A0CA47376E7892CC711EFA1DC47`,
`CandidateAttributeRasterInfluenceAudit.cs`
`08FC231178A1A7393442EC8A29884E9AADAFB2BCC5680B20B212E142C8620546`,
`ProjectReadinessAudit.cs`
`0D416A44B8A2BA8E02CE8E7A6C1AF895CDCE5862386593DFD525196381B9923C`,
`Program.cs`
`69ECDD321F8243981A32DBB2E9D7D3BF3CE9096656155BFEC9E9A07B78E00358`,
matriz `3F8CC844430215B34BF83C4ED8AA43AFEEBADA1CBD4CB504F49CC10937616D9`,
DLL Release `2CCC224AB6715F6D79CEC8707DC8F3D6AA98611D7AFB744468E8723A5FF9B81D`
e readiness `3D043101C88871F40E1EEE8B3B7707BC3311BE15174AB584FF570A4114449CF1`.

Validação: build Release `0/0`, self-test `90/90`, matriz completa
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos; `10` cenários, `777`
frames, `54` eventos de áudio e `1554` samples de animação. A ISO permaneceu
somente leitura: `2013003776` bytes, `Archive`, última escrita
`2026-01-22T07:03:26.0859951-03:00`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a evidência independente de estado→draw e UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação, sem promover bindings.

## Avanço verificável — controle independente de compatibilidade estrutural atributo/textura candidata — 2026-10-08

Foi implementado `tools/analysis/eagl_candidate_attribute_texture_compatibility_control.py`.
Ele junta independentemente, por objeto, os `24` perfis numéricos de atributo
candidate aos `24` assets same-trunk. Inputs, dimensões positivas, metadados
`PAL8` e pixels decodificados foram válidos em `24/24` rows; as faixas opacas
foram finitas/normalizadas em `24/24`, e `slot1_values` coincidiu com a
contagem de vértices em `24/24` rows.

Status: `verified-independent-candidate-attribute-texture-compatibility-control`,
com `semantic_binding=not-proven` e `original_semantics=not-claimed`. A
evidência é compatibilidade estrutural candidata: não identifica slot 1 como
UV, não atribui sampling/material e não prova binding original de estado, draw
ou GPU. O subgate `assets-candidate-attribute-texture-compatibility-control`
está `verified-structural-negative`, `observed=24`, `expected=24`; assets seguem
`candidate-integrated`, rendering `not-proven`, áudio `partially-integrated`,
deformação `structural-only` e readiness `NOT_READY`.

Artefatos: relatório
`C49F4BB877646835CFA00720667503A64B2E9F64E5A3A5557AAE675D6BB8D3B9`, resumo
`750E4B79966EF5775BE0C631B99A523F26230D3EEA926EACB27C4DE45400C859`,
readiness `B16C9A047528917D2082971B9D4D671B2AA0DFB798D0916AC1D03A0F7C10A736`,
script `7B7912822CCFCAB4A74049AB5D6B569EFF84706931A35BC8EDA9D2356C6D185A`,
matrix `73A629EB52715005FD9EFCDD0B4B108943BF597438E2CFA8539C028BCF4B669F` e
DLL Release `0DCBBBA702C26D6BA01024227E3DF036DD0CA727029A4982A037376FCBEADE79`.

Validação da etapa: build Release `0/0`, self-test `90/90`, matriz
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos aplicáveis; `10` cenários,
`777` frames, `54` eventos de áudio e `1554` samples de animação. A ISO foi
revalidada sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente de render-state→draw,
UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação, sem
promover bindings a partir deste controle.

## Avanço verificável — controle estrutural independente de estado/geometria/bounds — 2026-10-08

Foi implementado `tools/analysis/eagl_original_render_state_geometry_bounds_control.py`.
Ele cruza independentemente, somente dentro dos `24` objetos candidatos, a
partição original de estado/geometria com o controle bruto de bounds/fetch.
Foram confirmados `24/24` objetos particionados, `24/24` entradas de bounds
válidas, `24/24` joins e `24/24` rows completas; `218` grupos de bounds, `216`
completos e `2` esperadamente incompletos. O slot `0` coincide em `218/218`,
slots não-zero em `0` e pares command-shape exatos em `0`.

Status: `verified-independent-original-render-state-geometry-bounds-structural-control`,
com `semantic_binding=not-proven` e `original_semantics=not-claimed`. Isso
confirma apenas cardinalidade, partição e posição relativa do primeiro fetch;
não promove estado→draw/geometria, posição, UV, material, skinning, rigging,
animação, morph ou GPU. O subgate
`rendering-original-state-geometry-bounds-control` está
`verified-structural-negative`, `observed=24`, `expected=24`; assets seguem
`candidate-integrated`, rendering `not-proven`, áudio `partially-integrated`,
deformação `structural-only` e readiness `NOT_READY`.

Artefatos: relatório
`DFEBD93D163A551FA28905D9A05512E858F7172ECFAF285FAAEB03A35172ACDD`, resumo
`C0BC3EC18E8483589CFD37F8530CE5058063B424922EB1E670C9DA7261BAFCAA`,
readiness `E163BF21175BACA20B8D2CF8000443BFEAF0E37A64512F1C163BB5C91FBC2FB2`,
script `A078E08FC3B3A08B55CE4A1B73F3B74C13A6DD915B4D0DF1A68C9447CC4F4627`
e DLL Release `DD20BA2ADBFB50EFE5C783EDA7ED73E3BDDBEBDFE39861A68F70296B2F475893`.

Validação da etapa: build Release `0/0`, self-test `90/90`, matriz
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos aplicáveis; `10` cenários,
`777` frames, `54` eventos de áudio e `1554` samples de animação. A ISO foi
revalidada sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente de render-state→draw,
UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação, sem
promover bindings a partir deste controle.

## Avanço verificável — controle lexical independente de categoria/ordinal dos slots candidatos — 2026-10-08

Foi implementado `tools/analysis/eagl_original_candidate_slot_category_control.py`.
Ele reparseia independentemente `eagl-candidate-fetch-association.tsv` e
`eagl-original-candidate-raw-stream-identity.tsv`, preservando os símbolos
`__GPvb` como vocabulário opaco. O resultado real foi: `218` rows candidatas,
`24` objetos, `218` rows unidas, `216` primitivas completas, `2` rows
esperadamente não materializadas, `1080` slots, `1080/1080` símbolos com
formato válido, `1080/1080` categorias coerentes com `10 + slot` e
`1080/1080` ordinais coerentes com a primitiva.

Status: `verified-independent-original-candidate-slot-category-ordinal-control`
e `original_semantics=not-claimed`. A relação é apenas lexical/estrutural:
não prova que qualquer slot seja posição, UV, material, normal, peso,
skinning, rigging, animação, morph, draw ou GPU. O subgate
`assets-candidate-slot-category-ordinal-control` foi registrado como
`verified-structural-negative`, `observed=218`, `expected=218`; assets seguem
`candidate-integrated`, rendering `not-proven`, áudio `partially-integrated` e
readiness `NOT_READY`.

Artefatos: relatório TSV SHA-256
`95ED83D5CDD52216EBEA25E3C660E4D4134A287EAAB91D7768CBEB1AEC5AAD24`, resumo
`EF7DEF8F1835E35E60BC4A25A8BF3C173C9290A028FDB5CC9133A4E42F8AF42F`,
readiness `9B82157CB82C3F53935E0561CA2CEB31852CA3E5A6EFD77E5FD982F7658CF145`
e DLL Release `DD55D70B0B1941433095F50B6B6626D48F28643491A4EB0756FCA08936D9D82E`.

Validação da etapa: build Release `0/0`, self-test `90/90`, matriz
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos aplicáveis; `10` cenários,
`777` frames, `54` eventos de áudio e `1554` samples de animação. A ISO foi
revalidada sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente de render-state→draw,
UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação, sem
promover bindings a partir deste controle lexical.

## Avanço verificável — identidade bruta independente dos streams candidatos — 2026-10-08

Foi implementado `eagl-original-candidate-raw-stream-identity-audit` e o
controle independente `tools/analysis/eagl_original_candidate_raw_stream_identity_control.py`.
O controle reabre a mesma ISO somente para leitura, revalida os 24 objetos
EAGL e os ranges `__VB`, e calcula SHA-256 do payload bruto de cada fetch-slot
materializado. O resultado real foi: `24` objetos, `218` primitivas candidatas,
`216` primitivas completas, `1080` slots candidatos, `1090` streams brutos
recarregados, `1080/1080` slots válidos, metadados exatos e hashes de payload;
`216` identidades completas, `2` rows esperadamente não materializadas e `0`
rejeições.

Statuses: `verified-independent-original-candidate-raw-stream-identity-control`
e `verified-structural-negative`. A evidência confirma apenas a identidade
bruta entre o relatório candidato e a releitura da ISO; não define o
significado de slot, associação de draw, UV, material, skinning, rigging,
animação, morph ou GPU (`original_semantics=not-claimed`). O subgate foi
registrado com `observed=216`, `expected=218`, enquanto assets continuam
`candidate-integrated`, rendering `not-proven`, áudio `partially-integrated` e
readiness `NOT_READY`.

Validação da etapa: build Release `0/0`, self-test `90/90`, matriz completa
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos aplicáveis. Foram
confirmados 10 cenários authored, 777 frames, 54 eventos de áudio e 1554
samples de animação. A ISO permaneceu intacta e somente leitura:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando prova independente de render-state→draw,
UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação; nenhum
binding semântico original foi promovido.

## Avanço verificável — controle negativo de identidade bruta EVT/ABKC/AST — 2026-10-08

Foi implementado o comando `audio-event-abk-raw-identity-audit` e o controle
independente `tools/analysis/audio_event_abk_raw_identity_control.py`. Ambos
reabrem a ISO somente para leitura, preservam as 17 faixas de dados nomeadas
por EVT, validam os 16 bancos ABKC, as 270 entradas `SoundEntries` não-dummy e
as 70 faixas AST delimitadas, todas com assinatura `SCHl`, e comparam SHA-256
completo e prefixos brutos. O resultado real foi: `17` rows EVT, `17` hashes
EVT distintos, `0` pares de hash bruto completo, `0` eventos com match completo,
`0` pares de prefixo e `0` rejeições.

Statuses: `verified-bounded-audio-event-abk-raw-identity-negative-control` e
`verified-independent-audio-event-abk-raw-identity-control`. Isso é somente
um controle negativo de identidade; não seleciona amostra ABKC, não liga EVT a
gameplay/playback e não atribui semântica original (`direct_selection_binding=not-proven`,
`original_semantics=not-claimed`). O subgate foi registrado como
`verified-structural-negative`, mas o gate de áudio original continua
`partially-integrated`, rendering continua `not-proven` e readiness continua
`NOT_READY`.

Validação da etapa: build Release `0/0`, self-test `89/89`, matriz completa
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtime headless/windowed e todos os replays determinísticos aplicáveis. A
matriz confirmou 10 cenários authored, 777 frames, 54 eventos de áudio e 1554
samples de animação, sem alterar a ISO: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar procurando evidência independente de estado→geometria
e UV/material, de EVT→ABKC→sample→playback e de timing/pose/rig/deformação;
nenhum binding semântico original foi promovido por este controle.

## Avanço verificável — controle independente de identidade command/payload — 2026-10-08

Foi adicionado `eagl-original-primitive-stream-command-payload-identity-audit`,
que reprocessa os objetos EAGL brutos da ISO. Para cada ordinal candidato, o
controle constrói uma identidade SHA-256 limitada do registro `__VB`, descritor
e cabeça/cauda do payload, e compara os cinco pacotes brutos de command 9 por
contenção exata de bytes. O resultado observado foi: `2364` primitivas,
`2362` fetches completos, `10240` candidatos, `10240` identidades distintas,
`0` duplicatas, `0` correspondências command/payload exatas e `2364/2364`
ordinais atuais com identidade única. O reparse Python independente reproduziu
todos esses contadores e marcou `2364/2364` rows coerentes.

Status: `verified-bounded-original-primitive-stream-command-payload-identity-control`
e `verified-independent-original-primitive-stream-command-payload-identity-control`.
Isso é um controle negativo de bytes: não atribui semântica a slot, registro,
payload, geometria, UV, material, normal, peso, skinning, rigging, animação,
morph, comando ou GPU; `original_semantics=not-claimed`. O novo subgate aparece
como `verified-structural-negative`, mas rendering continua `not-proven` e o
readiness continua `NOT_READY`.

Validação final da etapa: Release `0/0`, self-test `88/88`, matriz completa com
smokes, runtime headless/windowed e replays determinísticos passou com exit code
`0`. A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente de estado→geometria e
UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação; nenhum gate
original foi promovido por este controle.

## Avanço verificável — controle independente de slot 1/categoria numérica 11 — 2026-10-08

Foi adicionado `eagl-original-candidate-slot-profile-audit`, que reprocessa
independentemente `eagl-original-vertex-stream-numeric.tsv` e
`eagl-candidate-fetch-association.tsv`, usando a chave composta por arquivo,
objeto e símbolo do stream. O símbolo isolado não é globalmente único.

Na ISO, `216/216` primitivas candidatas completas de cinco slots têm slot 1
associado ao stream original do mesmo objeto, com símbolo, stride,
byte-length e element-count exatos. Os `216/216` também satisfazem categoria
numérica original `11` e perfil float2 finito/próximo de unidade; o reparse
contou `11820` rows originais, `218` primitivas candidatas e `216` completas.
Status: `verified-independent-original-candidate-slot1-category11-profile-control`.

Isso permanece controle estrutural independente, não decodificação semântica:
slot 1 e categoria 11 são labels opacos. Não há afirmação de UV, material,
normal, peso, skinning, rigging, animação, morph ou comando GPU;
`original_semantics=not-claimed`. O readiness continua `NOT_READY`.

Matriz concluída com exit code `0`: Release `0/0`, self-test `87/87`, smokes,
runtimes headless/windowed e replays determinísticos passaram. ISO preservada
sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar prova independente de estado→geometria e
UV/material, timing/pose/rig/deformação e EVT→ABKC→sample→playback; os gates
originais de assets, rendering, áudio e animação/deformação continuam abertos.

## Avanço verificável — controle posicional de campos `.off` para words CSI — 2026-10-08

`fifast2-csi-opaque-field-audit` comparou os `100` words opacos de
`FIFAST2.csi` com as três colunas numéricas e o ordinal das linhas de
`pcpahdr.off` e `pcpadat.off`, usando as tabelas completas da ISO. As colunas
foram tratadas somente como posições observáveis; nenhuma recebeu semântica de
offset, tamanho, identificador, header, data, sample ou playback.

O resultado real foi `pcpahdr any_column=1/100` (`column1=0`, `column2=1`,
`column3=0`) e `pcpadat any_column=0/100`. Houve `row_ordinal=3/100` em cada
tabela. As coincidências são fracas e não discriminam os records; o status
permanece `verified-structural-csi-opaque-off-field-control`, com
`opaque_word_semantics=not-decoded`, `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`.

Matriz authored/candidate opt-in concluída com exit code `0`: build Release
`0/0`, self-test `86/86`, smokes, runtime headless/windowed e replays
determinísticos passaram; readiness continua `NOT_READY`. ISO preservada sem
escrita: `2013003776` bytes, `Archive`,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: priorizar bindings independentes de renderização/UV/material e
timing/pose/rig/deformação; EVT→ABKC/sample/playback continua não decodificado.

## Avanço verificável — controle de boundaries dos words opacos CSI — 2026-10-08

`fifast2-csi-opaque-range-audit` comparou, em leitura somente, os `100` words
CSI com `pcpahdr.off`/`pcpahdr.big` e `pcpadat.off`/`pcpadat.big`. As duas
tabelas têm `283` rows válidas e particionam integralmente os companions
(`35572` e `601688832` bytes).

Todos os words ficam dentro de ranges, mas `0/100` coincide com início e
`0/100` com fim em ambos os assets. O containment de header usa `80` rows
distintas e o de data apenas `1`, o que reforça que o teste de faixa é fraco e
não identifica registro ou subcampo. Resultado:
`verified-structural-csi-opaque-range-boundary-control`,
`opaque_word_semantics=not-decoded`, `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`.

Release `0/0`, self-test `85/85`, smoke real e readiness `NOT_READY`; ISO
intacta: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar subcampos/identificadores independentes, sem tratar
containment como seleção; depois priorizar os gates de renderização,
UV/material e deformação que continuam abertos.

## Avanço verificável — namespace de hashes `tunavars.txt` cruzado com CSI — 2026-10-08

Foi adicionado `fifast2-csi-tuna-audit`, que lê `tuna.big`,
`tunavars.txt`, `FIFAST2.csi` e `pcpahdr.big` somente através do filesystem
read-only da ISO. `tuna.big` é `BIGF`, tem `6313` bytes, uma entrada
`tunavars.txt` e `106` rows. As `106/106` rows coincidem com o candidato
independente de hash `seed=0x1505; hash=hash*0x21+byte ASCII`.

O CSI tem `100/100` registros válidos e `89` nomes em comum com tuna; há `11`
nomes CSI fora da tabela e `17` nomes tuna fora do CSI. Os words opacos do CSI
coincidem com hashes tuna em `0/89`. Os `100/100` words ficam dentro do
comprimento de `pcpahdr.big`, mas esse teste é somente de faixa e não atribui
registro ou subcampo; `pcpahdr.off` cobre o asset inteiro.

O resultado é estrutural: `direct_selection_binding=not-proven`,
`opaque_word_semantics=not-decoded`, `original_semantics=not-claimed`. Não há
prova de EVT→ABKC/sample nem playback original. Release `0/0`, self-test
`84/84`, auditoria real e readiness `NOT_READY`; ISO intacta com
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: testar apenas associações independentes dos words CSI contra
limites/subestruturas reais de `pcpahdr`/`pcpadat`; seguirão abertas as lacunas
de rendering, UV/material, animação/deformação e seleção original de áudio.

## Avanço verificável — índice estrutural candidato de FIFAST2.csi — 2026-10-08

Foi adicionado o comando `fifast2-csi-audit`, que lê `data/audio/FIFAST2.csi`
somente via stream de leitura e preserva os campos não decodificados. O
formato observado `MOIR` tem `100/100` registros válidos e nomes ASCII únicos;
os offsets começam em `0x5F4`, o word reservado é zero em `100/100` registros,
há `12` words opacos dentro do asset e `98` pares adjacentes não-decrescentes.

O cruzamento textual com os `17` nomes EVT produziu `0` sobreposições exatas e
`22` por substring. Isso é apenas um candidato de índice de nomes: não prova
seleção direta, trigger, sample, ABKC ou playback. O relatório marca
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.

Validação: Release `0/0`, self-test `83/83`, testes Python `57/57`, matriz
completa `completed-authored-and-candidate-opt-in`, runtime headless, smokes
relevantes e replays determinísticos passaram. A ISO permanece sem escrita:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência independente para estado→geometria/UV/material,
seleção EVT→amostra e timing/pose/rig/skinning/morph.

## Avanço verificável — layout de canal candidato único em streams AST — 2026-10-08

O decoder gerenciado agora tenta, somente quando o header SCHl observado tem
`0` canais, os layouts candidatos de `1` e `2` canais. A validação percorre
todos os blocos sem alterar o header; o caminho só é aceito quando exatamente
um layout passa e é marcado no relatório como
`decoded-payload-channel-layout-candidate`. Não há promoção para evento,
sample selector, gameplay ou playback original.

Nos dois casos antes rejeitados, `GBREAK.abk`/sound `4` e `GBREAKLC.abk`/sound
`6`, o layout `1` foi único e decodificável. O exhaustive bounded/all passou a
`241/241` candidatos de banco decodificados, mantendo `17/17` eventos,
`0` matches PCM exatos e `259/259` rows idênticas no controle de políticas.
`CONTROL.abk` continua corretamente sem candidato. A enumeração e a
proveniência continuam estruturais; `direct_selection_binding=not-proven` e
`original_semantics=not-claimed` permanecem.

Validação da alteração: Release `0/0`, self-test `82/82`, smokes bounded/all,
controle de política e síntese negativa independentes passaram. A matriz
completa 0.78 também terminou com `matrix_status=completed-authored-and-candidate-opt-in`,
incluindo runtime headless e replay determinístico. A ISO segue
somente para leitura, com 2013003776 bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca independente de seleção EVT→amostra,
UV/material e deformação.

## Avanço verificável — enumeração ABKC por entradas observadas — 2026-10-08

O runtime passou a materializar no catálogo somente as entradas de som ABKC
não-dummy observadas pelo parser, preservando a distinção entre evidência de
tabela e semântica de seleção. `AudioBankBatchDecoder` e a auditoria
`audio-event-payload-wave-correlation` agora enumeram os índices RAM cujo
`SoundType=0`, em vez de tentar o maior contador declarado que também inclui
entradas dummy. O fallback por contador continua restrito a fixtures sem a
lista de entradas observadas.

Na ISO, a reexecução bounded/all produziu `17/17` eventos nomeados
decodificados, `16` bancos ABKC, `241` candidatos de som não-dummy e `239`
payloads decodificados. O controle independente de políticas comparou
`259/259` rows (`17` eventos + `242` rows de banco) com identidade, status,
decode, tamanho WAV, hash PCM e contador de match exatos iguais; houve `0`
pares PCM exatos. Permanecem explicitamente não validados `CONTROL.abk` sem
candidato e um stream AST de `GBREAK.abk` e `GBREAKLC.abk` rejeitado pelo
decoder por `0 channels`; isso não foi convertido em silêncio ou binding.

Esse avanço corrige a enumeração estrutural, mas não seleciona uma amostra a
partir de EVT, não liga evento ao gameplay e não prova playback original.
`direct_selection_binding=not-proven`, `original_semantics=not-claimed` e o
gate principal `audio=partially-integrated` permanecem. Rendering continua
`not-proven`, assets candidate permanecem separados e
animation/deformation continua `structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `57/57`, matriz completa
com authored/candidate opt-in, smokes relevantes, runtime headless e replay
determinístico passaram. A ISO permaneceu somente para leitura:
`2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca independente de seleção EVT→amostra e de
atributos/UV/material/deformação, sem inventar bindings.

## Avanço verificável — join original↔candidate dos grupos de geometria por stream — 2026-10-08

Foi adicionado o audit candidate `eagl-candidate-geometry-stream-audit` e o
controle independente `eagl_original_candidate_geometry_stream_control.py`.
O caminho candidate reparseou `24/24` objetos e `38/38` grupos de geometria:
todos têm primitiva correspondente, fonte candidate registrada no slot `0`,
layout de cinco streams completo e geometria finita/indexada.

O join independente, limitado às `2364` chaves originais de slot `0`, encontrou
`38/38` grupos candidate. Nome `__VB`, stride, byte-length, element-count,
quantidade de vértices e cardinalidade do triangle strip coincidiram em
`38/38`; `2326` primitivas originais continuam fora da cobertura candidate.
O slot `0` é somente a escolha do leitor candidate para construir a nuvem; a
correspondência não o transforma em posição, UV, material, normal, peso,
skinning, rigging, animação, morph, draw ou GPU. `original_semantics=not-claimed`.

O readiness registra
`rendering-original-candidate-geometry-stream-control=verified-structural-negative`
`38/38`, enquanto `assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only` continuam
inalterados.

Validação: Release `0/0`, self-test `82/82`, Python `57/57`, matriz completa
0.76 com smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca independente de atributos/UV/material,
deformação e seleção EVT→amostra, sem promover o slot candidate a semântica.

## Avanço verificável — join original↔candidate de registros `__VB` por slot — 2026-10-08

Foi adicionado `eagl_original_candidate_stream_record_control.py`. O controle
independente reparseou o relatório original de topologia (`11820` rows,
`2364` primitivas) e os `218` primitivas candidate. Nos `216` layouts completos,
`1080/1080` slots materializados coincidiram exatamente em nome de stream
`__VB`, valor auxiliar, fetch stride, stream stride, byte-length e
element-count; `1080/1080` records ficaram válidos. Os `2` layouts incompletos
continuam explícitos como `candidate_slot_records_unmaterialized_expected`, sem
preenchimento inferido. A cobertura candidate não existe para `2146` chaves
originais, e isso permanece registrado como missing por escopo.

O readiness registra
`rendering-original-candidate-stream-record-control=verified-structural-negative`
`1080/1080`, com `original_semantics=not-claimed`. Essa é uma reconciliação de
metadata/identidade estrutural; não atribui nenhum slot ou stream a posição,
UV, material, skinning, rigging, animação, morph, draw ou GPU. Os gates
principais continuam `assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `56/56`, matriz completa
0.75 com smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência independente que decodifique campos de
atributo/UV/material e deformação/seleção de áudio, sem promover essa
correspondência estrutural a semântica.

## Avanço verificável — join original↔candidate de comando/fetch por primitiva — 2026-10-08

Foi adicionado `eagl_original_candidate_primitive_fetch_control.py`. O join
independente por `(model_archive, model_object, layer, primitive)` comparou
`2364` primitivas originais com as `218` primitivas candidate disponíveis:
`218/218` contagens de comando, completude de stream, contagens de fetch,
completude de cinco slots, texto de estado e flag de tipo coincidiram. Em
`216/218`, a sequência de IDs de fetch e os `slot_records` candidate foram
materializados; os `2/218` restantes são exatamente os dois casos de layout
incompleto, mantidos explicitamente como `candidate_slot_records_unmaterialized_expected`.
Não houve chave candidate sem correspondente original (`0`), enquanto os
`2146` originais sem candidate continuam fora do escopo do relatório.

O readiness registra
`rendering-original-candidate-primitive-fetch-control=verified-structural-negative`
`218/218`, com `original_semantics=not-claimed`. Isso valida apenas a
reconciliação estrutural de chaves, contagens, texto e IDs; não transforma
slots ou comandos em posição, UV, material, skinning, rigging, animação, morph,
draw ou semântica GPU. Os gates principais continuam
`assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `55/55`, matriz completa
0.74 com smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca independente por binding de primitiva a
vértices/atributos/UV/material, deformação original e seleção EVT→amostra; se
a prova não existir, manter cada lacuna explicitamente não comprovada.

## Avanço verificável — sequência de índices SHAPENAME original↔candidate — 2026-10-08

Foi adicionado o controle `eagl-candidate-shape-state-primitive-audit` e o
join independente `eagl_original_candidate_shapename_primitive_index_control.py`.
Por chave `(model_archive, object_name)`, os `24/24` objetos preservam a mesma
sequência `layer:primitive`, o mesmo conjunto e a mesma contagem entre o
`SHAPENAME` observado no relatório original e o parser candidate. Os `24/24`
streams candidate foram completos e os dois lados ficaram válidos.

O readiness registra
`rendering-original-candidate-shapename-primitive-index-control=verified-structural-negative`
`24/24`. Essa coincidência de índice/ordem é evidência estrutural apenas: não
decodifica primitiva→vértice, UV, material, skinning, rigging, animação, morph,
comando GPU ou semântica original. Os gates principais continuam
`assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `54/54`, matriz completa
0.73 com smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar uma regra comprovada de associação de primitiva a dados
de vértice/atributos/UV/material, além de deformação original e seleção
EVT→amostra; se a prova não existir, manter cada lacuna como não comprovada.

## Avanço verificável — população de shape-state por primitiva — 2026-10-08

Foi adicionado `eagl_original_state_candidate_primitive_shape_control.py`.
Além do join lexical anterior, ele comparou a população por objeto: em
`24/24` objetos, a contagem de primitivas de estado original coincide com a
contagem de shape-state do candidato e com a quantidade de tokens
`SHAPENAME`; `24/24` streams shape-state candidatos estão completos. O mesmo
join preserva `24/24` same-trunk, `24/24` cadeia de origem válida, `24/24`
entradas SHPX exatas e `24/24` hashes de pixels coincidentes.

O readiness registra
`rendering-original-state-candidate-primitive-shape-control=verified-structural-negative`
`24/24`. Isso não é binding de primitiva para vértices, UV, material,
skinning, rigging, animação, morph, comando GPU ou semântica original; os
gates principais continuam `assets=candidate-integrated`,
`rendering=not-proven`, `audio=partially-integrated` e
`animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `53/53`, matriz completa
0.72 com smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar uma regra decodificada para a associação de primitiva,
atributos/UV/material, deformação de animação e seleção EVT→amostra; se a
prova não existir, manter cada lacuna como não comprovada.

## Avanço verificável — controle independente SHAPENAME↔asset candidato — 2026-10-08

Foi adicionado `eagl_original_shapename_candidate_asset_control.py`. O
controle juntou, por chave independente de objeto, os `24` rows de estado/
geometria original e os `24` rows da cadeia candidata. Houve `24/24` joins,
`24/24` tokens `SHAPENAME` lexicalmente compatíveis com o shape candidato,
`24/24` same-trunk, `24/24` cadeias de origem válidas, `24/24` entradas SHPX
exatas e `24/24` hashes de pixels decodificados coincidentes.

Isso é controle positivo de identidade estrutural de asset, não promoção de
semântica: não atribui UV, material, vértices, skinning, rigging, animação,
morph, comando GPU ou binding original. O readiness registra
`rendering-original-shapename-candidate-asset-control=verified-structural-negative`
`24/24`, enquanto `assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`
continuam deliberadamente separados.

Validação: Release `0/0`, self-test `82/82`, Python `52/52`, matriz completa
0.71 com smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a decodificação independente de estado de
renderização/UV/material, deformação de animação e seleção EVT→amostra, sem
inventar bindings.

## Avanço verificável — síntese negativa independente da seleção de áudio original — 2026-10-08

Foi adicionado `tools/analysis/audio_event_selection_negative_synthesis.py` e o
readiness passou a registrar `audio-event-selection-negative-synthesis` como
`verified-structural-negative` `5/5`. A síntese revalidou cinco fontes sem
promover semântica: índice EVT com `17` words candidatos e zero ocorrências
alinhadas em ABK/PCPAHDR/PCPADAT; busca desses `17` words no XBE com zero
ocorrências alinhadas e não alinhadas; payload com `17/17` eventos decodificados,
`16` bancos, `368` candidatos de som, `239` decodificados e `0` pares PCM
exatos; join de faixa nomeada `17/17`; e as `15` rotas authored clean-room.

Isso estreita o caminho de seleção EVT→amostra, mas não prova ausência global
nem cria um decoder: continuam `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`. O gate `audio` permanece
`partially-integrated`; rendering original permanece `not-proven` e deformação
de animação permanece `structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `51/51`, matriz completa
0.70 com smokes, runtime headless/windowed e replays determinísticos aprovados.
ISO sem escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Artefatos: `audio-event-selection-negative-synthesis.tsv` e seu summary.

Próxima etapa: continuar bindings originais de estado/geometry/UV/material,
deformação de animação e seleção EVT→amostra, sem promover candidatos.

## Avanço verificável — rotas authored de áudio reconstituídas dos traces — 2026-10-08

Foi adicionado `tools/analysis/audio_authored_candidate_route_control.py`.
O controle reparseou independentemente os traces authored e juntou suas rotas
ação→candidato ao catálogo de payloads EVT nomeados do runtime: `10/10`
cenários válidos, `54/54` eventos tocados, `15/15` rotas, `15` ações,
`8` candidatos únicos e `15/15` rotas com candidato runtime preparado e header
WAV válido; nenhum candidato da rota ficou ausente entre os `17` eventos
nomeados.

Isso confirma a integração e replay do caminho authored clean-room, não uma
tabela de eventos original: não recupera IDs originais, não seleciona
ABKC/sample e não prova gameplay ou playback original. Mantidos
`original_semantics=not-claimed`; readiness registra
`audio-authored-candidate-route-control=verified-authored` 15/15, sem alterar
`audio=partially-integrated`.

Validação: Release `0/0`, self-test `82/82`, Python `50/50`, matriz completa
0.69 com smokes, runtime headless/windowed e replays determinísticos aprovados.
ISO sem escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Próxima etapa: continuar a busca por seleção EVT→amostra e bindings originais
de renderização/animação, sem inventar semântica.

## Avanço verificável — join independente estado original ↔ atributos/influência candidata — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_state_candidate_attribute_control.py`.
O controle reparseou e juntou por `(model_archive, model_object)` os relatórios
de tokens de estado original, atributos candidatos e influência raster candidata:
`24/24` objetos candidatos, `24/24` joins, `218/218` registros de estado válidos
e `218/218` sem tokens literais de textura, material, UV, atributo de vértice ou
deformação. Os perfis candidatos `slot1` ficaram completos, finitos e near-unit
em `24/24`; a geometria candidata foi finita/indexada em `24/24`, houve influência
de textura no raster em `20/24` e `mesh_shape_state_exact=0/24`.

Isso confirma somente identidade estrutural entre relatórios independentes e
influência/atributo do candidato. Não transforma `slot1` em UV/material, não
associa estado original a geometria e não prova ausência semântica em comandos
opacos; `original_semantics=not-claimed`. Readiness registra
`rendering-original-state-candidate-attribute-control=verified-structural-negative`
24/24, sem alterar `rendering=not-proven` ou qualquer gate original.

Validação: Release `0/0`, self-test `82/82`, Python `49/49`, matriz completa com
smokes, runtime headless/windowed e replays determinísticos aprovados. A ISO
permaneceu sem escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Próxima etapa: continuar a busca independente por associação original de
estado/UV/material e seleção EVT→amostra, sem inventar bindings.

## Avanço verificável — join independente EVT paralelo → payload runtime — 2026-10-08

Foi adicionado `audio_event_parallel_runtime_join_control.py`. O controle
independente juntou por nome EVT os relatórios de faixa paralela PCPAHDR/PCPADAT
e de payload nomeado do runtime: `17/17` linhas, nomes únicos `17/17`, header
row `17/17`, offset `17/17`, comprimento `17/17`, metadados validados
`17/17`, payloads preparados `17/17` e headers WAV válidos `17/17`.

Isso confirma a preservação de identidade/range no caminho candidato gerenciado,
mas não seleciona ABKC/sample, não liga evento a gameplay e não prova playback
original; `original_semantics=not-claimed`. Readiness:
`audio-event-parallel-runtime-join-control=verified-structural-negative`
17/17, sem alterar `audio=partially-integrated`.

Validação: Release 0/0, self-test `82/82`, Python `48/48`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos aprovados. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `DB82C0D3C592EF8158829CB824F2812F769A4C9165D3F617F4B2D38B9F5F57E4`;
controle `5B15C5A54916670E1CB9BBEB573ECC9CF0F04F90D00157EC66337495BFC6BD21`;
summary `8F5C982EA17AC3F50478029C37A1F52CAD858D18AE5C6192DCE8A829C8EEB0FF`;
readiness `77FE7E7D7EC8E1AABA325E3C21583AF8F86AA312BCB0A31DF5D755F762BC9A6A`;
log `4737F1A78814A8CBF248406187F430B6915FBF97C8FE3C5C6BC7CB5E55209C37`.

Próxima etapa: buscar seleção EVT→amostra e, em paralelo, associação
independente estado/UV/material; a integração original ainda não está provada.

## Avanço verificável — join independente entre streams numéricos e fetch slots opacos — 2026-10-08

O controle `eagl_original_bounds_fetch_slot_join_control.py` refez o join por
`(model_archive, model_object, ordinal, category)` entre os `11820` streams
numéricos e `11820` registros de topologia. Foram obtidos `11820/11820` joins
de identidade, `2364/2364` grupos completos de cinco streams, fetch slot raw
`0` no primeiro registro em `2364/2364` grupos, match de stride em
`2364/2364`, match de bounds em `2363/2364` e nenhum match posterior. Os dois
casos de slots incompletos foram preservados como evidência opaca.

Isso continua sendo consistência estrutural, não posição, geometria, UV,
material, skinning, rigging, animação, morph ou comando GPU. O primeiro raw não
recebeu semântica; `semantic_binding=not-proven` e
`original_semantics=not-claimed`. Readiness:
`rendering-original-bounds-fetch-slot-join-control=verified-structural-negative`
2364/2364, sem alterar `rendering=not-proven`.

Validação: Release 0/0, self-test `82/82`, Python `47/47`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos aprovados. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `51B2B70A129421A5F05BA04FA2331109A439FF5D3EAAD2CEF9C19E351E00A836`;
stream report `A6DA833F84E07FFCF8388A215B6618406383C61854B40CCE0A8A33498141FF12`;
controle `E8BE8F200ED47B6423EF43244BB41BED8CFC76FC38C343B108BFA5C9B4996187`;
summary `04681A4777F27672E69A22B1A0194277F30D806A7BF7F7B09B9B19E80887933D`;
readiness `FB761D99DF155D83BFC371D1AADC9F77DCE0E05C4F0E91994BE47F41DC1C0B28`;
log `6EEBD42AE2CBCA515F9EFA1A869EC969484B18FC35D6E2D8646DC539F388DDE5`.

Próxima etapa: buscar, de modo independente, bindings de estado/UV/material
e seleção EVT→amostra; nenhum resultado estrutural será promovido a semântica
original sem prova.

## Avanço verificável — correspondência independente de bounds no primeiro registro raw — 2026-10-08

O audit passou a emitir `eagl-original-vertex-stream-numeric.tsv` por stream
validado. O controle independente agrupou os registros pela chave opaca
`(model_archive, model_object, ordinal)`: `11820/11820` linhas válidas,
`2364/2364` grupos completos de cinco registros e sequências contínuas. O
primeiro registro raw teve stride 12 em `2364/2364` grupos; a contenção nos
bounds ocorreu nele em `2363/2364` grupos e em nenhum registro posterior. As
famílias do primeiro raw foram `category_key=10` em 2010 grupos e `11` em 354.

Isso é associação numérica/estrutural, não posição ou geometria decodificada.
O primeiro registro não foi chamado de posição e não recebeu binding de UV,
material, skinning, rigging, animação, morph ou GPU;
`semantic_binding=not-proven` e `original_semantics=not-claimed`. Readiness:
`rendering-original-bounds-consistent-record-control=verified-structural-negative`
2364/2364, sem alterar `rendering=not-proven`.

Validação: Release 0/0, self-test `82/82`, Python `46/46`, matriz completa,
smokes, runtime headless/windowed e replays. ISO sem escrita: 2013003776 bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `C17C7DE0FC2E6960F69304A5A2841C06DD6C1EB280B674DF344C0542DE00D661`;
stream report `A6DA833F84E07FFCF8388A215B6618406383C61854B40CCE0A8A33498141FF12`;
controle `2F9056749FEAABA25A0C38A6EB10F19AECCE5F23F0961757968D609BFF0F0551`;
summary `C59DA46CC903A5F3E23D95AA628E9EBCA19880766458796C2668682E2530A072`;
readiness `3B21349B7153257FB04C6AFE19C6511BCF47BC8B4756E4E290B2EC4EE8E0D9A4`;
log `5672C8714ABBFF8053007DAFD0E42B576E06C14E95F8D0E4D54AC7E04DB5ADE3`.

Próxima etapa: usar a associação como evidência candidata de geometria e
buscar bindings independentes de estado, UV/material e EVT→amostra.

## Avanço verificável — controle independente da cardinalidade dos matches numéricos — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_numeric_stream_ambiguity_control.py`.
Ele reparseia `eagl-original-vertex-payload.tsv` e classifica, somente no nível
de objeto, a quantidade de streams `float3` que cabem nos bounds declarados:
`883/883` entradas válidas, `494` com um match, `388` com múltiplos matches e
`1` sem match (`0=1,1=494,4=196,5=160,6=1,7=4,8=9,9=7,10=8,11=1,12=1,13=1`).

Isso é um controle de cardinalidade/ambiguidade, não uma prova de posição ou
geometria. Mesmo os `494` casos de cardinalidade única não recebem binding de
stream, primitiva, UV, material, skinning, rigging, animação, morph ou GPU;
`semantic_binding=not-proven` e `original_semantics=not-claimed`. O readiness
registra `rendering-original-numeric-stream-ambiguity-control` como
`verified-structural-negative` 883/883, sem alterar `rendering=not-proven`.

Validação da etapa: build Release 0/0, self-test `82/82`, Python `45/45`, matriz
completa com smokes, runtime headless/windowed e replays determinísticos; a ISO
foi preservada com 2013003776 bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL Release `CF863DA6CE14326060C7E4A4CBA8FE9E5FC4347C504C683A5422B980E30BF986`;
relatório `C637C4D1EF8DE77D2544140CB7229410DE83EA40D8604F70A8FD59148419CC82`;
summary `6F137E8CC1681F8B5F3AE1E0EA4E31BA62DEF4F87A640095183D6CDB16A075ED`;
readiness `FF5AA4D98EA0CFE74FF516B151598842BE2E09F64DEFDEC417468A6CB149E7AD`;
log `F15BC9C41A4AEBB470EA29FE5D5064588A8F1D3F36B01C8CD3B13F02D84C2016`.

Próxima etapa: continuar a busca de seleção EVT→amostra e de decoders de
renderização/animação, mantendo os bindings sem prova como não decodificados.

## Avanço verificável — controle independente do caminho textual do asset EVT — 2026-10-08

Foi adicionado `tools/analysis/audio_event_asset_path_index_control.py`.
Ele juntou o relatório textual de literais do XBE ao índice de
`data/audio/eventspa.evt`: 4 formas textuais, 9 ocorrências, 4/4 linhas
mapeadas para um único caminho ISO, 17 nomes EVT e 1.061 pares candidatos.
As 4 linhas mantêm `xbe_literal_xref_count=0`.

O resultado é somente proveniência e controle negativo. Não recupera o loader,
não transforma string em função/ID, não seleciona ABKC/AST/BNK e não reivindica
playback original; `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`. O readiness adiciona
`audio-event-asset-path-index-control=verified-structural-negative` 4/4, sem
mudar `audio=partially-integrated`.

Validação: build Release 0/0, self-test 82/82, teste Python do controle
aprovado, matriz completa exit_code=0 com smokes de áudio/renderização,
runtime headless/windowed e replays determinísticos. A matriz terminou com
`matrix_status=completed-authored-and-candidate-opt-in`; a ISO permaneceu sem
escrita, com 2013003776 bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL Release `E538D9DAC739B39952BD1F0D604A050263EF1F8D54EB88828CB49D632D4411C2`;
relatório `5118A4CC14D83879FFBD4EF2436CF8529A65F361E67E9AE9A70EC3882DBE03B5`;
summary `BF0F1E664FB14A507C3A11718534273935065F4B46A5E4E522D610A8F1941E56`;
readiness `171A0683581456334BEBCB751E8100E08632D7B7316786BF0C2A222B0688CE8B`;
log `0B7B3DD1380A35AC5B6F63ED298AD534DFFF7A8CC6A3A554A41F901279751988`.

Próxima etapa: continuar a busca de seleção EVT→amostra e os decoders de
renderização/animação, sem inventar bindings.

## Avanço verificável — controle independente das políticas de payload de áudio — 2026-10-08

`audio_event_payload_policy_control.py` comparou os relatórios EVT/ABKC com
limites de 512 e 100.000 candidatos por bank. Coincidiram 386/386 linhas,
compostas por 17 eventos e 369 candidatos de bank; status de decode, bytes,
hashes PCM e contadores foram iguais, com 0 pares de hash PCM exato.

É apenas cobertura e reprodutibilidade. Não estabelece EVT→ABKC/AST/BNK,
gameplay ou playback; `original_semantics=not-claimed`. O readiness adiciona
`audio-event-payload-policy-control=verified-structural-negative` 386/386,
sem mudar o gate principal de áudio, ainda `partially-integrated`.

Validação: Release 0/0, self-test 82/82, matriz completa exit_code=0, smokes,
runtime headless/windowed e replays determinísticos. ISO preservada:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `599EB85BD0AC52B328B6831A0F7B75464C0E673E9512395B7165647C64F22C26`;
relatório `F3D87BDCCD0F27B584C3B48BCB7B98AFDCB941AE57647DC37893621AEE1B1D94`;
summary `C0CEB3E9B359ACE60D9D126BCCE56427BFA034E5822FC3FDD7EE45903D8F9297`;
readiness `1FFB1B5187F5DF3AB307AEB78E20D2DE616CC8A83986D73CA5281E101DC0E49D`;
log `5878DB73ABA59EDAF70B5C5F9300DE82CDBB5D706A1F788A2422B7E44CD230BD`.

Próxima etapa: buscar evidência independente de seleção EVT→amostra e
avançar renderização/animação sem inventar bindings.

## Avanço verificável — join independente estado/geometria ↔ payload original — 2026-10-07

`eagl_original_state_geometry_payload_join_control.py` cruzou os 24 objetos
`tops*.big` da partição estado/geometria com o relatório original independente
de render/payload. O resultado foi 24/24 objetos presentes, 0 ausências,
24/24 identidades, contadores estruturais válidos, soma geometria+estado igual
às primitivas em 24/24 e cinco streams de payload por primitiva em 24/24;
ranges distintos e 0 sobreposições.

É apenas consistência de chaves e contadores: não atribui payload a posição,
UV, material, atributo, skinning, rigging, animação, morph, comando ou GPU.
`original_semantics=not-claimed`. O readiness adiciona
`rendering-original-state-geometry-payload-join-control=verified-structural-negative`
24/24; o gate `rendering` continua `not-proven`.

Validação: Release 0/0, self-test 82/82, matriz completa exit_code=0, smokes,
runtime headless/windowed e replays determinísticos. ISO preservada:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `76F273539B1E1D14494B556D2D021F539D1F7EF7F6DB9F182F8237C317DD4FDE`;
relatório `7D0236122EB806FB288DFCE6F1C1923ADEF7E9CF81EE0A7DD0C0FF27D6E33F93`;
summary `48C79333F7C74C36632F7E5C660C677D5265950C4CFCA9D0AE9917E58413EF26`;
readiness `6678BF3B2546A8A802ACB96707CBA660032E06D282CBAE7265AD0AF147BDFED3`;
log `2B2A1FC0AA3286D9DD7E0A9EB1577F4AA43BD8976A423D0D54A10B373246E649`.

Próxima etapa: continuar a investigação estrutural de renderização e áudio,
mantendo authored, candidato e semântica original separados.

## Avanço verificável — partição independente de estados e geometria — 2026-10-07

Foi adicionado o controle Python
`eagl_original_state_geometry_partition_control.py`, que reprocessa os
relatórios de adjacência, perfil opaco e intervalos ordenados da renderização
original. Foram reconciliados 24/24 objetos, 269/269 perfis, 62/62 intervalos,
38 pares geométricos, 180 estados e 180/180 índices de estado cobertos.
Status: `verified-independent-original-state-geometry-partition-control`.

É evidência estrutural somente: em 24/24 objetos,
`object_state_exact=True` e `geometry_group_state_exact=False`; os 269 perfis
estão completos e há 0 pares de forma de comando exata. Não foi criado
binding para mesh groups, materiais, UVs, skinning, rigging, animação, morphs,
comandos ou GPU; `original_semantics=not-claimed`. O readiness adiciona
`rendering-original-state-geometry-partition-control=verified-structural-negative`
24/24, mantendo `rendering=not-proven`.

Validação: Release 0/0, self-test 82/82, smokes de adjacência/perfil/ordem,
matriz completa exit_code=0, runtime headless/windowed e replays
determinísticos. ISO preservada: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `2BA8CC8D4DD40AE174787E76B66C02DF18A449FF33AD2DE3FA59E4B673AAFA60`;
relatório `4F788E12FDB3CC447C302DA8E195E3E14B4FAAFEEDDC258CFB7A75A291AC4D0D`;
summary `9C20A83D49482D34CEB1F12E3A249C616F1D541E5CA9C5AE59DEA04CAA46AE4A`;
readiness `D3B754FABCA3A2376B4DD5FA93D98C852D63718B76F156852FFAC98F8E0DD720`;
log `7FD111FBBD63895829980A0FC9FBDAA0EE7CBB8B6314F362C1F54596C216CD28`.

Próxima etapa: continuar a associação original de renderização e a seleção
original de áudio, sem reutilizar rotas authored como semântica original.

## Avanço verificável — controle negativo de words EVT no XBE — 2026-10-07

AudioEventXbeHashSearchAudit procurou os 17 words do índice textual observado
em eventspa.evt dentro dos 3.293.184 bytes do default.xbe, em offsets alinhados
e em todos os offsets de byte. Foram encontradas 0 ocorrências alinhadas e 0
não alinhadas, com 17 nomes únicos e 17 words únicos.

Isso elimina apenas a hipótese literal simples; não prova ausência de uma rota
indireta. O XBE não foi executado, nenhuma função foi recuperada e nenhum word
foi convertido em evento, amostra ABKC ou playback. O readiness registra
audio-event-xbe-hash-search como verified-structural, mantendo
direct_selection_binding=not-proven e original_semantics=not-claimed. O gate
principal de áudio continua partially-integrated=false.

Validação: Release 0/0, self-test 82/82, matriz completa com exit_code=0,
smokes relevantes, runtime headless/windowed e replays determinísticos. ISO
preservada: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
CFF31EF95E08040AB38A3581FF55BECD2DC340B56DF27D4E1ED6580DADC0FC3F;
relatório 3BB4A38AEB8765B4B56E0F3121C4FF2B7B736D690F10BAF2CBB5DB531963C2C6;
summary 01336D1D9E8A2EA34188DF53BB8506F64DBEEEACDF1B76E2AA56DA120E885880;
readiness 0A2EA4A9A4492B7FDE148286E4C8B7176E0198AEDF00AD751F3BC38D8C54F501.

Próxima etapa: investigar associação estrutural original de renderização além
de intervalos, fetch e adjacência, sem inventar bindings de materiais, UVs,
skinning, rigging, animação, morphs ou GPU.

## Avanço verificável — controle estrutural EVT ↔ ranges paralelos de áudio — 2026-10-07

Foi adicionado AudioEventParallelRangeAudit. Ele revalida os 17 nomes EVT
contra o início textual exato em eventspa.evt, a mesma linha em
pcpahdr.off/.big e pcpadat.off/.big, os limites dos ranges e o prefixo SCHl.
Foram confirmados 17/17 âncoras, 17/17 ranges de header, 17/17 ranges de
dados, 17/17 índices paralelos, 17/17 prefixos SCHl, 17/17 nomes/offsets/linhas
únicos e 283/283 linhas nas duas tabelas.

O resultado é estrutural: não escolhe amostras ABKC, não cria ligação com
eventos de gameplay e não reivindica semântica original. Mantém
direct_selection_binding=not-proven e original_semantics=not-claimed. O
readiness registra audio-event-parallel-range como verified-structural, sem
alterar o gate principal de áudio, que continua partially-integrated=false.

Validação: Release 0/0, self-test 81/81, matriz completa com exit_code=0,
smokes relevantes, runtime headless/windowed e replays determinísticos. ISO
preservada: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
6D6CC0AF1ECE8AF58ECE20E9D6DE76F02686ABB053D5C08417EB0DAD5648FB2C;
relatório F117F7C32189AEBC5A7882FE8E498170F656E91DAB6831F7834E94E2CA6AD670;
summary 5F078EF529C37BD3D0532B8A8A15DD32D807F8DCC0A13EDC7EB1C13E85CD8240;
readiness 4405CF95CA4B6D5EA690B209D756345C49E88F4AEF6A30C74747C87E0E336461.

Próxima etapa: continuar a busca independente por seleção original de áudio e
associação original de renderização, sem reutilizar rotas authored nem
inventar bindings.

## Avanço verificável — gates agregados da integração clean-room — 2026-10-07

O readiness agora separa os agregados authored dos gates de semântica original.
Passaram: `assets-clean-room` 10/10 cenários,
`rendering-clean-room` 140/140 frames, `audio-clean-room` 54/54 eventos
tocados e `animation-clean-room` 1554/1554 samples. `gameplay` continua
`clean-room-integrated=true`.

Isso confirma manifests authored, framebuffer managed, rotas authored e
traces/replays determinísticos, sem transformar candidate mesh, UV, material,
skinning, rigging, morph, timing ou GPU em semântica original. Os gates
originais permanecem assets `candidate-integrated=false`, rendering
`not-proven`, áudio `partially-integrated=false` e animation-deformation
`structural-only=false`; `original_semantics=not-claimed`.

Validação: Release 0/0, self-test 80/80, Python 43/43, matriz completa com
exit_code=0, smokes relevantes, runtime headless/windowed e replays
determinísticos. ISO preservada: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
4BB5F349628F3BFA8C6A9A425B33BCB41DBFC1135C4FEFA65F351AF5A44774B8;
readiness D126BE88409DDF706F6B320DFAB8EEF16725DACF888E47155193F6B5F82EB2C5.

Próxima etapa: investigar seleção original de áudio e associação original de
renderização sem reutilizar rotas authored nem inventar bindings.

## Avanço verificável — gate principal de gameplay clean-room — 2026-10-07

O readiness tinha a linha `gameplay=clean-room-integrated` com
`gate_passed=false` apesar de o runtime authored já possuir cobertura,
invariantes, integração e replays completos. O auditor foi corrigido para
passar esse gate somente quando essas quatro evidências authored estão válidas.
Isso não promove as regras originais: `original_semantics=not-claimed` continua
explícito.

O gate agora é `clean-room-integrated=true`: 10/10 cenários integrados, 54
eventos authored, 777 frames e replays determinísticos válidos. Assets seguem
`candidate-integrated=false`, renderização original `not-proven`, áudio
`partially-integrated=false` e animation-deformation `structural-only=false`.

Validação: Release 0/0, self-test 80/80, Python 43/43, matriz completa com
exit_code=0, smokes relevantes, runtime headless/windowed e replays
determinísticos. ISO preservada: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
2CB105E28AAE2C9BE49C61A845D97B6AF243933E658E5783E210343682F938CD;
readiness C8CBF5E05C3A205C29615BF6501E8550FFE80414A6F1B41BB4C1C7AB99893ACE.

Próxima etapa: continuar a integração clean-room de assets, renderização e
áudio, mantendo a evidência original separada.

## Avanço verificável — controle independente de regiões comando/payload — 2026-10-07

Foi adicionado
tools/analysis/eagl_original_command_payload_region_control.py. O controle
reparseia render-chain e topologia e faz join apenas pela identidade
model_archive/model_object/layer/primitive. Foram confirmadas cinco regiões de
payload válidas e não sobrepostas em 2364/2364 primitivas; seção coincidente,
intervalo de comando válido e não sobreposto e comando após o payload em
2364/2364. A lacuna bruta `command_stream_offset - maior_fim_payload` foi
`0x10` em 2364/2364.

Os dois casos sem slot 2 continuam explícitos. O resultado é estrutural apenas:
original_semantics=not-claimed; nenhum byte/intervalo foi ligado a posição,
geometria, UV, material, skinning, rigging, animação, morph, comando ou GPU.
O subgate rendering-original-command-payload-region-control está
verified-structural e readiness geral continua NOT_READY.

A etapa passou Release 0/0, self-test 80/80, Python 43/43, matriz completa,
smokes relevantes, runtime headless/windowed e replays determinísticos. A ISO
segue sem escrita: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
C90B6FD374D53B9CA29A60872C5A1B4E15977E59A99605BCF91BCCA9DC1C97BA;
relatório F195DD9E929ED75068638D943008997CD8F198930C461F3AE688159311E78841;
summary 90C3F725B7F21BC1C23C884CA9A8E35949F6ED6E142DBC11794146B142EA44ED;
readiness D48F89BC8DA712F915569FD0CF792A61C4302CFAAC23031E009C6E437FDD9202.

Próxima etapa: procurar associação original independente além da coerência de
offsets e intervalos, sem inventar bindings.

## Avanço verificável — controle independente fetch/topologia por primitiva — 2026-10-07

Foi adicionado
tools/analysis/eagl_original_primitive_fetch_topology_control.py. O controle
reparseia os relatórios render-chain e topologia e faz join pela identidade
model_archive/model_object/layer/primitive. Foram confirmados 2364/2364 joins,
cinco linhas de topologia por primitiva, IDs de fetch coincidentes em 2364/2364
e concordância da completude em 2364/2364.

O controle preservou 11820/11820 ordinais e ranges válidos, 11818 slots
efetivos, 11810 linhas com structural_match e 2362 grupos completos. As duas
exceções são as primitivas que não têm slot 2; elas continuam registradas como
incompletas, sem preenchimento artificial. É evidência estrutural apenas:
original_semantics=not-claimed; nenhum slot/stream foi ligado a posição,
geometria, UV, material, skinning, rigging, animação, morph, comando ou GPU.

O subgate rendering-original-primitive-fetch-topology-control está
verified-structural e readiness geral continua NOT_READY. A etapa passou
Release 0/0, self-test 80/80, Python 42/42, matriz completa, smokes relevantes,
runtime headless/windowed e replays determinísticos. A ISO segue sem escrita:
2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: procurar associação original independente além da coerência de
slots, sem inventar bindings.

## Avanço verificável — controle independente estado/fetch ↔ adjacência — 2026-10-07

Foi adicionado
tools/analysis/eagl_original_state_fetch_adjacency_control.py. O controle
reparseia independentemente render-chain e adjacência, juntando por
model_archive/model_object/layer/primitive. Houve 2364/2364 joins; state
relocation em 2364, SHAPENAME em 752, SetPrimitiveType em 1612 e fetch
completo de cinco slots em 2362. O overlap state/fetch reportado e
recomputado foi 750/750; state/primitive-type foi 0/0.

O join exato com a adjacência foi 2364/2364; state+adjacência 2364/2364;
state+fetch+adjacência 2362/2364; SHAPENAME+adjacência 752/2364 e
SetPrimitiveType+adjacência 1612/2364. As categorias exatas foram 14:2010 e
15:354. O resultado é apenas controle estrutural/coocorrência, com
original_semantics=not-claimed; não prova estado→geometria, UV, material,
skinning, rigging, animação, morph ou GPU.

O subgate
rendering-original-state-fetch-adjacency-control está verified-structural,
enquanto readiness geral continua NOT_READY. A etapa passou Release 0/0,
self-test 80/80, Python 41/41, matriz completa, smokes relevantes, runtime
headless/windowed e replays determinísticos. A ISO continua sem escrita:
2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: continuar a busca por evidência independente de semântica de
campo/atributo ou associação estado→geometria/UV/material, sem inventar
bindings.

## Avanço verificável — perfil bruto da adjacência comando/descritor — 2026-10-07

O audit .NET e o controle Python foram refinados para registrar o stream
validado que satisfaz command_stream_offset = descriptor_offset + 0x0C,
incluindo categoria e stride, sem atribuir semântica. Nos dados reais foram
2364 streams exatos em 2364 primitivas: categoria 14 em 2010, categoria 15 em
354, stride 2 em todos. O controle independente reproduziu os mesmos
histogramas.

Isso é evidência estrutural de bytes/offsets, não decodificação de campo,
atributo, geometria, UV, material, skinning, rigging, animação, morph, comando
ou GPU. original_semantics=not-claimed; o subgate continua
verified-structural e readiness continua NOT_READY.

Hashes: audit 46E952F58C8A40507E3A1A2D9C741CD91DBA89611F09D5B860D7CF290AFD8C56
/ E5B5B6D8751D7174E5E609684A77A24503CC7CFE054E538A30F77A4E7E86CF37;
controle 41BC4DCD41AFD0496AE3B265C5AD5410C1EA85A990084C9D7D9FBB05D82D02DC
/ E5134201C80EF61C1B76690DB0B6450A375E2D6772E7DF3AEB93686723AE92DF.

Validação: Release 0/0, self-test 80/80, Python 40/40, matriz completa,
smokes relevantes, runtime headless/windowed e replays determinísticos
passaram. DLL 7C6750FE5FB3B9820367E6E2A654F8779C6CB2BBF99210BDE65571BD03FE3E1A;
readiness 533D08F678639A36808D01E2D2D08CFBAF42B74B486AF5643FBAD5963FAADC23.
A ISO segue sem escrita: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: procurar evidência independente de semântica de campo/atributo
ou associação estado→geometria/UV/material, sem promover candidatos.

## Avanço verificável — adjacência estrutural comando/descritor `__VB` — 2026-10-07

Foi adicionado `EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.cs`,
o comando `eagl-original-primitive-stream-command-descriptor-adjacency-audit` e
um controle Python independente. Em `2364/2364` primitivas com comando completo,
`command_stream_offset == descriptor_offset + 0x0C` para um descritor `__VB`
validado na mesma seção. A cobertura foi `10240` candidatos, `2364` identidades,
`2364` identidades atuais únicas, `2364` pares na mesma seção e `0` falsos
deslocamentos; o controle independente reproduziu os mesmos contadores.

É somente adjacência bruta de offsets; não é decodificação de campo, atributo,
geometria, UV, material, skinning, rigging, animação, morph, comando ou GPU.
`original_semantics=not-claimed`. O subgate
`rendering-original-primitive-stream-command-descriptor-adjacency` é
`verified-structural`; o gate principal de renderização continua `not-proven` e
readiness continua `NOT_READY`.

Hashes: audit
`4DC8C24FFF144C4FE83659B1F8F8B5D5C83756637EBF5F15230DF04E3481D717`, summary
`00ABD1EF407D5FD733CB55BEB220608413AB54177584B5F1E855C57F0A56B79E`, controle
`DEFA5803260CD6CE977D0411ED325793166DA4FD7DABD02281FD56368C3D3975` / summary
`081BD81C79F80E97D33192F315E8CDAC958F8D478F38703F3997C340E13E0B14`.
Render-chain atualizado: `C6C80BB202ECE7E08AD283338C40629C86421BCCF310F35DD908E793AEBFB84B`;
summary `36D8765FB5E3DC1E00ABDF59C8477384575F5D912A1B737F2BB1A971BCD4C8CF`.

Validação: Release `0/0`, self-test `80/80`, Python `40/40`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL
`CCE12ACE31724C7142EF9E338BD1E547AA362D4C957C6FBDC4CE67D044FA1877`;
readiness `DB51CD27DD5F9D0BCFE185B219D4987E3DFAFD066E525750820201E85C191E30`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar semântica independente de campo/atributo ou associação
estado→geometria/UV/material, sem promover candidatos nem inventar bindings.

## Avanço verificável — gate separado de áudio candidate-only — 2026-10-06

O readiness passou a consumir `audio-candidate-runtime` como
`verified-candidate`: o full-stack opt-in preparou e reproduziu `8/8` eventos,
com `0` não mapeados e replay coincidente. Isso comprova somente a integração
candidate-only authored; não promove seleção original EVT/ABKC/amostra.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`A392BB170AD47F640D354A06F9360A9CDD97071F2194785385F90ABC028C09A7`.
Readiness:
`A694DD3E15444DEDDAB0ADA3A9EA67026497A06690CE11A9629349208C018EEE`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — gate separado de renderização candidate-only — 2026-10-06

O readiness passou a consumir `rendering-candidate-runtime` como
`verified-candidate`, mantendo o gate original `rendering=not-proven`. O
full-stack opt-in validou `140/140` frames, `24` assets candidatos, `280`
samples authored de animação, `8/8` eventos de áudio preparados/reproduzidos,
replay coincidente e framebuffer válido. Isso comprova apenas a integração
candidate-only do runtime gerenciado; não define estado original, UV, material
ou GPU.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`C2E72072AC40D16F7B3376B5DFD91F3EE85E7C50425A264EFE6AD66B5F99F800`.
Readiness:
`0A317FA850F5D5F16C23898E8291FF1D1DCABD7D11ABBB406A50E71631068736`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — call-sites bounded do destino aninhado — 2026-10-06

Foi adicionado `tools/analysis/xbe_render_indirect_indexed_cfg_nested_call_site_audit.py`.
O contexto do slice de `0x0016BF90` contém `2` call-sites para `0x0026E278`,
com `1` destino distinto. Ambos mostram `fld`, `fmul`, `call` e `ret 8`; `0/2`
mostram `push edx`, `lea [ecx+...]` ou `ret 4`. É evidência local bounded,
sem recuperação de função, objeto, binding ou semântica.

Status `verified-bounded-static-render-indirect-indexed-nested-call-sites`,
provenance `same-ISO-retail-XBE+bounded-one-hop-nested-call-site-context;no-execution;no-call-following;no-function-boundary-claim;no-binding`
e `original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído; readiness permanece `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`AA55B7737D13A532ACA1B1CDC5553B93A9A6B9DC5A439A15F6C901A74B868567`.
Readiness:
`1442E380FE5C11D1D0D702A448E3B950733A6D8D8BF534191A0B49829A65248E`.
Nested call-site audit/summary:
`4F760A2CB3F8F47BC4BD2A0AB2A03D263A481AE6C50853BE3DAFB9DD084A8E56` /
`870483285414A5E646CDC7C51DC1F23EAFC60387AD9E91005B7AB4DDC88AF15D`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — CFG bounded do destino aninhado — 2026-10-06

Foi adicionado `tools/analysis/xbe_render_indirect_indexed_cfg_nested_call_target_cfg_audit.py`.
O auditor inspecionou, sem execução e sem seguir uma nova chamada, o único
destino direto encontrado dentro do slice de `0x0016BF90`, `0x0026E278`: a
janela de `256` bytes tem `89` instruções lineares, `37` alcançáveis, `7`
blocos, `3` branches condicionais, `2` incondicionais, `4` alvos internos,
`1` retorno alcançável e `0` chamadas diretas adicionais. Isso permanece
fluxo local candidato, não função recuperada ou semântica.

Status `verified-bounded-static-render-indirect-indexed-cfg-nested-call-target-cfg-slices`,
provenance `same-ISO-retail-XBE+bounded-CFG-over-one-hop-nested-direct-call-destinations;no-execution;no-call-following;no-function-boundary-claim;no-binding`
e `original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído; readiness permanece `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `30/30`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`395813624F6C4AD23B4FA1D11BF44DF7C6697C2189282E32319200BCAD737982`.
Readiness:
`0F5A8B50AB5D8D04A300AB0BA883782D4D6017546400753C727EB86D03FE196A`.
Nested audit/summary:
`5E73FAF3C58277F98B08DC4F9030673E381000B6188AB5018BB7AB9B564336E3` /
`2C18CBB0DC2B53C4E4795945F0786B5011E747C12DC3CCFFB7A1FDAEF4C17D33`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — join bounded entre tabela e call-sites indexados — 2026-10-06

Foi adicionado `tools/analysis/xbe_render_indirect_indexed_cfg_call_site_join_audit.py`.
Sem executar o XBE, o join preservou `81` linhas de alvos da tabela, `1` base,
`24` call-sites, `24/24` joined, `1` alvo distinto, `27` índices distintos,
`27` ocorrências de entrada e `3` duplicatas sobre os call-sites; `24/24`
call-sites têm forma local completa. Isso é apenas evidência bounded estática:
não recupera função, objeto, campo ou semântica.

Status `verified-bounded-static-render-indirect-indexed-call-site-join`,
provenance `same-ISO-retail-XBE+bounded-indexed-table-target-and-CFG-call-site-join;no-execution;no-call-following;no-function-boundary-claim;no-binding`
e `original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído; readiness permanece `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `29/29`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`CF1E344282E9237943573EF27A7F24A1F12B662476175A819337DCF0B50A9959`.
Readiness:
`EC3F4027BE15A0829D65EF6E76E6B65E0BB3BDD68A4FC2A6B3E732D1AC7F9BBD`.
Join audit/summary:
`6742EB92434168D7A04C695BB77DCF8632AC1534ACB702E144401AA557FD81AE` /
`71BA5028FCDFBDF276848DC9FD50619EA615D08948CFB806C58D164B832FFBC9`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — CFG bounded do destino direto indexado — 2026-10-06

Foi adicionado `tools/analysis/xbe_render_indirect_indexed_cfg_call_target_cfg_audit.py`.
No único destino global `0x0016BF90`, o CFG de `256` bytes produziu `76`
instruções lineares, `23` alcançáveis, `7` blocos básicos, `2` branches
condicionais, `2` chamadas diretas, `0` transferências indiretas e `1/1` `ret`
alcançado. As chamadas internas não foram seguidas; nenhum limite de função
ou semântica foi declarado.

Status `verified-bounded-static-render-indirect-indexed-cfg-call-target-cfg-slices`,
provenance `same-ISO-retail-XBE+bounded-CFG-over-indexed-CFG-call-target-slices;no-execution;no-call-following;no-function-boundary-claim;no-binding`
e `original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído; readiness permanece `NOT_READY`.

Validação final: Release `0/0`, self-test `72/72`, Python `28/28`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`06483C77B64112CCFE79F635DCE1B4252F7C1D2FD92A7BB6E6744B71B11F1FC8`.
Readiness:
`975217DC8EFDDA58C4D35A9218EF93E1D467A82701B4A85233475181AF76EB60`.
Audit/summary:
`8CBBF5C159106F6D98CE05D65A88ACD76179E863A89763202649B431D450EE77` /
`6250D59561DE79E73027693C34E8853DC33EBE4A91941AAE6ABCDFFD0AA85ED2`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — contexto bounded dos call-sites indexados — 2026-10-06

Foi adicionada `tools/analysis/xbe_render_indirect_indexed_cfg_call_site_audit.py`.
Nos `24` calls-sites alcançáveis dos CFGs indexados, `24/24` convergem para
`0x0016BF90`; `24/24` preservam `lea` baseado em `ecx` com deslocamento
distinto, `push edx` antes da preparação e `ret 4` após a chamada. O relatório
registra somente contexto local de instruções bounded: não define wrapper,
função, objeto, campo ou semântica.

Status `verified-bounded-static-render-indirect-indexed-call-sites`, provenance
`same-ISO-retail-XBE+bounded-indexed-CFG-reachable-call-site-context;no-execution;no-call-following;no-function-boundary-claim;no-binding`
e `original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído; readiness permanece `NOT_READY`.

Validação final: Release `0/0`, self-test `72/72`, Python `27/27`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`FE1DEAB66AC78E1FDA154848394C6EAA3A06FB3A87E563EC5609CE0A313F1E37`.
Readiness:
`B18D55EC12C80D7888B4EC441A1315C17DF9A7BD7AA036E08EEC7C6FC794EB6C`.
Audit/summary:
`1BC39F705C0AAADFC34AAD030CFB0E29B2A87197E71A9E3E4363A34B16650474` /
`227EC2C511556952E038E9AB725B76B5E68459AA3F2BF8EF57B987BEF2219A92`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — slice bounded do destino direto indexado — 2026-10-06

Foi adicionado `tools/analysis/xbe_render_indirect_indexed_cfg_call_target_audit.py`.
O auditor preserva `24` ocorrências de chamada (`24` alvos únicos somados por
slice) e `1` endereço distinto global, `0x0016BF90`. Esse destino mapeia em
`.text`, offset de arquivo `0x0015BF90`, com `256` bytes disponíveis, `76`
instruções disassembladas e `ret` observado; não houve prólogo reconhecido.
Nenhuma chamada foi seguida e nenhum limite de função foi declarado.

Status `verified-bounded-static-render-indirect-indexed-cfg-call-target-slices`,
provenance `same-ISO-retail-XBE+bounded-indexed-CFG-direct-call-destinations;no-execution;no-call-following;no-function-boundary-claim;no-binding`
e `original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído; readiness permanece `NOT_READY`.

Validação final: Release `0/0`, self-test `72/72`, Python `26/26`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`0FF23943CA552774AC25810A2B4B6D0C066F13862EB36A3D70BA787C9E44274A`.
Readiness:
`73AB08762847C2F91C282C5A4C749863B692B8DB61E9B42A850D1655C7379328`.
Audit/summary:
`41E108CCEC548410358EF649AD9F3DBB24448175CF1E4E44D9C388F9FB1463A2` /
`E329BE8965964C3995347AFF7442B7BF403E947B7D0412BDD44D97512F86414C`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — distinção de chamadas diretas e join CFG — 2026-10-06

O auditor CFG dos `81` candidatos da tabela indexada passou a registrar a
distinção entre ocorrência e endereço distinto: há `24` chamadas diretas nos
slices (`24` alvos únicos somados por linha), mas somente `1` endereço distinto
global, `0x0016BF90`. Nenhum destino foi seguido e isso não declara limites de
função, ponteiros ou semântica.

Foi adicionado `tools/analysis/xbe_render_indirect_indexed_cfg_join_audit.py`.
O join compara o conjunto unitário com `87` alvos diretos de renderização,
`64` alvos das janelas de renderização e `235` nós do grafo bounded. As três
interseções foram `0`; o status é `verified-structural-negative`, apenas um
controle negativo de endereços. `original_semantics=not-claimed`; nenhum
binding de material, UV, skinning, rigging, animação, morph ou GPU foi
atribuído; readiness permanece `NOT_READY`.

Validação final: Release `0/0`, self-test `72/72`, Python `25/25`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`361A00A077A987AF3A826CC807AF6D141D228E0698C40B4F81A8948BF6AD5F9E`.
Readiness:
`C8A4D27666203A19FE35E8300C66F1CA4B914585FB80F9329127BB77F4CDEA3F`.
CFG audit/summary:
`B2D8030BF2EA4F940AD23943B80BDF52455E909B0EEB3359EEBAAF5C6C485500` /
`9164CA4C7E7E73B9DB4D6AE213335724128854CB1E748EE8CE69798117F17FE6`.
CFG join audit/summary:
`6AEBD1BB735025409B55D76760E18722AA45F46094BEE53EA2B3FE9B96FD79FF` /
`5FAA0A61F854CC448B6B9B27BB0217A6F3DCF5430E53EEA6EC9E35AE9C4246A2`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — CFG bounded dos candidatos da tabela — 2026-10-06

Foi adicionado `tools/analysis/xbe_render_indirect_indexed_cfg_audit.py`.
Aplicando a mesma rotina de alcançabilidade bounded aos `81` alvos distintos
da tabela indexada, o ISO real produziu `81/81` CFGs alcançáveis e `81/81`
com `ret` alcançável: `7.295` instruções lineares, `421` alcançáveis, `105`
blocos básicos, `24` chamadas diretas registradas sem seguimento e `0`
transferências indiretas observadas dentro das janelas de `256` bytes.

Isso é alcançabilidade estática local, não recuperação de limites de função,
chamadas, ponteiros ou semântica. Status
`verified-bounded-static-render-indirect-indexed-cfg-slices`, provenance
`same-ISO-retail-XBE+bounded-reachability-over-indexed-table-candidates;no-execution;no-call-following;no-function-boundary-claim;no-binding`
e `original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído; readiness permanece `NOT_READY`.

Validação final: Release `0/0`, self-test `72/72`, Python `24/24`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram. DLL:
`5A3E8814C7AE038932856353E65E5E2322CC326A514BBDCB44356D8E0813714C`.
Readiness:
`0C9D676A829523BFF1B62A72BFF985222213B868855D05ED7B57414B61E12D38`.
CFG audit/summary:
`3DF517090ABE885BB013E6F012A23B6CF99EF4482DA12BBF56E014B3FC9E3756` /
`57E7E7F2138EC6C6FD707D1FA5956DF542DD77AFD89F04CE7E90A28D624D21D5`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — controle negativo de interseção dos alvos — 2026-10-06

Foi adicionado `tools/analysis/xbe_render_indirect_indexed_target_join_audit.py`.
Ele compara somente conjuntos de endereços, sem executar, chamar ou delimitar
funções: os `81` alvos distintos candidatos da tabela indexada contra os `87`
alvos do auditor de chamadas diretas e os `64` alvos da janela de renderização.
O ISO real produziu `0` interseções com o primeiro conjunto e `0` com o segundo.

Isso é um controle negativo estrutural de conjuntos de endereços bounded; não
prova que os comportamentos sejam independentes, nem recupera função, chamada,
ponteiro ou semântica. Status `verified-structural-negative`, provenance
`same-ISO-retail-XBE+bounded-address-set-intersection;no-execution;no-call;no-function-boundary-claim;no-binding`
e `original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído; readiness permanece `NOT_READY`.

Validação final: Release `0/0`, self-test `72/72`, Python `23/23`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram. DLL:
`D4289C65CD3E4FCD0953C3DD625D6EC1544E641F348821D4A96A693B74CD0ED3`.
Readiness:
`128EA296B76FBF9E49214008CAADD3E257E2E77087BA1F06414E5CDCE6C3B30A`.
Join audit/summary:
`5A8BE06EAD3A8FD27920F2151D88C262E4245D1E7A50C5789BB2F33AD79A16C9` /
`6E38D14E644E9A7034ED4B5424428FD067AE7DB8E226ED82C9E3E11CD2F5DDB1`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — slices limitadas dos alvos candidatos da tabela — 2026-10-06

Foi adicionada `tools/analysis/xbe_render_indirect_indexed_target_audit.py`.
Ela expande as `87` ocorrências executáveis do prefixo indexado, preserva os
índices de entrada e faz desassemblagem somente de uma janela máxima de `256` bytes
por alvo distinto. No ISO real foram observados `81` alvos distintos, `81/81`
alvos mapeados e desassemblados, `81/81` com `ret` dentro da janela e `0`
com forma de prólogo reconhecida.

O resultado continua sendo slice estática candidata, não recuperação de
funções, limites, ponteiros, chamadas ou semântica. Status:
`verified-bounded-static-render-indirect-indexed-target-slices`, provenance
`same-ISO-retail-XBE+bounded-disassembly-of-indexed-table-candidates` e
`original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído; readiness permanece `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `22/22`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos passaram. DLL:
`0C23802D85483FA565E1BE497610922B7C160ACFD80137F963FFDE07D2039210`.
Readiness:
`55D15A89A45E91B8ED86626BF1E1C46D9F447C9FB0DC15E6D221D649C636A5AA`.
Audit/summary:
`FA7A2DB7409B43C8A42EB7E8A5FD5C5A988CD06758C75A2C71FD7A28E19C8399` /
`BD71ED8F44CB15754045E9DDF5E58F828A55D0BE47FEBEEA26B2C434EA5DBEC7`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — varredura limitada da tabela de transferência indireta — 2026-10-06

Foi adicionada `tools/analysis/xbe_render_indirect_indexed_table_audit.py`.
Partindo da única transferência indireta indexada sem registrador-base já
observada, o auditor faz somente leitura limitada na base candidata e
classifica cada word pela associação à seção executável do XBE. No ISO real,
houve `1` row indexada e `1` base distinta: `89` words legíveis, `87`
classificadas como executáveis, `2` não executáveis, `0` ilegíveis, `81`
words executáveis distintas e prefixo executável contíguo máximo de `87`.
A leitura terminou após duas words não executáveis; não atingiu o limite de
`256` entries.

Isso é um prefixo estrutural candidato, não uma tabela completa nem uma
recuperação de ponteiros, funções, comprimento, chamada ou semântica. O
relatório tem status `verified-bounded-static-render-indirect-indexed-table`,
proveniência `same-ISO-retail-XBE+bounded-read-at-indexed-indirect-table-base`
e `original_semantics=not-claimed`. Nenhum binding de material, UV, skinning,
rigging, animação, morph ou comando GPU foi atribuído; o gate geral de
renderização continua `not-proven` e a readiness continua `NOT_READY`.

Validação final da etapa: Release `0/0`, self-test `72/72`, Python `21/21`,
matriz `completed-authored-and-candidate-opt-in`, smokes, runtime
headless/windowed e replays determinísticos passaram. DLL Release SHA-256:
`EF02D432D4C534F0AA1F572B7E3551E2B75C14B2AEB4AA7F03F278C0B72D3A7E`.
Readiness SHA-256:
`0FB62081FFAB468D6FA8985C5F160BD3480FCC9E28FB5E17475211EA07BBC36D`.
Audit/summary da tabela:
`0638A839ACF1191386669065B3E714AA8AEB4F2B0928D5983B48892587B1E895` /
`58271102E9D61725E85F0AB082AB4BF99556651A5141BAF3AE548461AE5C780A`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — observabilidade explícita do áudio candidato no runtime — 2026-10-06

O relatório `CleanRoomIntegrationReport` agora separa os eventos authored dos
candidatos de áudio preparados pelo mesmo ciclo do runtime. Nos cenários reais
foram registrados `17` candidatos EVT nomeados preparados (`2.914.952` bytes
PCM, `0` rejeições) e `16` bancos candidatos observados (`15` decodificados,
`1` rejeitado), enquanto o caminho authored permaneceu em `8/8` eventos
preparados e tocados, sem eventos não mapeados. Esses contadores são
observabilidade de payload candidato; não atribuem EVT a gameplay, ABKC a
sample ou seleção original.

Os novos campos são
`named_event_candidate_waves_prepared`,
`named_event_candidate_pcm_bytes_prepared`,
`named_event_candidate_decode_rejected`,
`observed_bank_candidate_count`, `observed_bank_candidate_decoded` e
`observed_bank_candidate_rejected`. O runtime continua opt-in para playback e
mantém `original_semantics=not-claimed`; o readiness segue `NOT_READY`.

Fechamento da etapa: Release `0/0`, self-test `72/72`, Python `20/20`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos com `matched=True`. DLL Release SHA-256:
`97C05064D768A18D73F74ECC5DBA6DA36DA03024148A071E9444D73EFFD2776E`.
Readiness SHA-256:
`F5A26E1A1002363CDD238BAF64382084DD0BD6C333628A2309FBA5525E3DD92F`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — interseção exata entre transferências e referências — 2026-10-06

O join estrutural agora também compara o par exato
`source_target_virtual_address + instruction_virtual_address` entre o
inventário de transferências indiretas e o inventário de referências. Nos
artefatos reais, `4` janelas contêm essa interseção e `8` rows de referência
coincidem com instruções de transferência. Isso é localização estática
repetível; não é prova de função, ponteiro, material, UV ou binding.

O status continua `verified-static-render-indirect-window-join`, com
`original_semantics=not-claimed`. Build Release `0/0`, self-test `72/72`,
Python `20/20`, matriz completa, smokes, runtime headless/windowed e replays
determinísticos passaram novamente. DLL Release SHA-256:
`3AD11C6CB09CE3FA2B59F317C612C049B670D3A59C8274FB63FD4D1DD5BB4E3D`.
Join audit/summary SHA-256:
`59E06AC832B3CFF34DD7F00392052EC9CE83BD0C1785525F0AF8411286C8C7DD` /
`A101B0E7E24548621F6F3EC2A1CB892F996176C4D12D0131DFB3305F8B73802D`.
Readiness SHA-256:
`6F0919D54C5C897041E6BAF4237455B234E4BDF7321A3910488DE6D93B54FB01`.
A ISO segue sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — join estrutural das janelas de renderização e indireções — 2026-10-06

Foi implementado `tools/analysis/xbe_render_indirect_join_audit.py`. Ele faz
somente joins exatos por endereço virtual entre as `64` janelas de render,
`22` transferências indiretas (`21` calls, `1` jump), `8` referências e `3`
words brutas candidatas, que correspondem a `3/3` slices executáveis e
`3/3` slices terminadas por `ret`. O resultado real foi `15` janelas com
transferências e `4` janelas com referências.

O relatório é `verified-static-render-indirect-window-join` e
`original_semantics=not-claimed`. Isso fecha apenas a cobertura estrutural do
inventário; não recupera ponteiros/funções e não atribui materiais, UVs,
skinning, rigging, animação, morphs, comandos GPU ou bindings originais. O
readiness adicionou o subgate `rendering-static-xbe-indirect-window-join` como
`verified-structural`, sem promover o gate geral de renderização.

Validação da etapa: build Release `0/0`, self-test `72/72`, testes Python
`20/20`, matriz `completed-authored-and-candidate-opt-in`, smokes, runtime
headless/windowed e replays determinísticos. DLL Release SHA-256:
`7D6D245F1E591C968F6C9D0DFECF3AD7773A3CE2DF25B36889AB987483A2E983`.
Join audit/summary SHA-256:
`E10ADF511CDDFF4F1604DA4EFDBB312848FBEF2491F1E8764DD469919DB08A68` /
`233EDEB9C3F5D82FCF446742DE65F7C9585A21D47FAD29DF02D1150596FDD188`.
Readiness SHA-256:
`879B82F85C986E71F1F224E8006D650F4EE91D580EC7998C30295AA1E64E41BE`.
A ISO permanece sem escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Use este arquivo como ponto de partida em outro chat. O projeto está em
`D:\projeto zeta` e **não está concluído**.

## Avanço verificável — controle de multiplicidade entre relocations de morph e animação — 2026-10-06

`AnimationMorphRelocationProfileAudit` passou a preservar, além do conjunto de
gaps, o histograma de multiplicidade dos gaps consecutivos (`morph_gap_histogram`)
e a comparação exata desse histograma com os 19 bancos de animação válidos.
Nos artefatos reais foram lidas `594` linhas de morph, `393` layouts distintos
parseáveis e `19` bancos de animação. O controle por conjunto continua em
`53` layouts/`159` pares de contenção e `0` pares de conjunto exato; o controle
mais estrito por histograma também resultou em `0` layouts e `0` pares exatos.

Isso é somente um controle estrutural de recorrência/ausência. Não decodifica
frames, poses, timing, rig, skinning, morphs, materiais, UVs, comandos GPU ou
bindings originais; `original_semantics=not-claimed` permanece explícito.
O relatório agora é regenerado e validado pela matriz, não apenas pelo
self-test.

Fechamento da etapa: build Release `0/0`, self-test `72/72`, testes Python
`19/19`, matriz `completed-authored-and-candidate-opt-in`, smokes, runtime
headless, host windowed, replays determinísticos e SHA-256 da ISO verificado.
DLL Release SHA-256:
`9776FD7946552D26CABF479C2DF5EA145C5CD76317A119C2FAF4F94C86276B51`.
Relatório e resumo SHA-256:
`D30560728E8BC701D1765558FF012D3CC36DF72E89421E255F6D6F1845379691` e
`FF42635A3E711AD2644BD9946A129541C7A99FC8E17DD781E53C18344FD8D9A2`.
Readiness SHA-256:
`1F2092C75C86088F1D21014CB02587E2727107268301A8F944AD4F00D12E5FF3`.
A ISO permaneceu sem escrita, com `2013003776` bytes, atributo `Archive` e
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — startup do host Win32 Release — 2026-10-06

O executável Release `fifa_pc/bin/Release/net8.0/FifaStreetPc.exe` foi
iniciado com `run-windowed`, personagens clean-room e material candidate
explicitamente opt-in. O host permaneceu vivo durante o smoke e imprimiu
`candidate_material_status=validated-candidate`,
`render_characters=clean-room-procedural`,
`candidate_home_character=enabled-rigid-candidate` e
`candidate_away_character=enabled-rigid-candidate`, além de iniciar o
catálogo (`data_files=122`, `texture_decodes_validated=2`). O processo foi
encerrado pelo harness depois da observação de startup (`EXIT_CODE=1` do
interrupt do harness).

Isso comprova somente inicialização do caminho Win32 e resolução das opções;
não é uma captura visual independente nem prova de binding original de
mesh, material, UV, skinning, rigging, animação, morph, GPU ou áudio. O host
continua candidate/clean-room opt-in e `original_semantics=not-claimed`.

## Avanço verificável — captura determinística do framebuffer Win32 — 2026-10-06

O host passou a aceitar `--run-seconds` e `--capture-frame <saida.ppm>`.
Isso permite um smoke Win32 que termina naturalmente e persiste o primeiro
frame produzido pelo mesmo `SoftwareFrameBuffer` usado no loop da janela. A
execução Release com `--run-seconds 2`, personagens clean-room e material
candidate opt-in terminou com `EXIT_CODE=0`, gravou
`fifa_pc/windowed-runtime-capture.ppm`, confirmou o cabeçalho P6 `960x540`,
`1.555.215` bytes e o checksum de framebuffer
`0F83CEFDD78C58BA`. O arquivo capturado tem SHA-256
`E1D1FE0294EA554F601F384C15D87B3FEA4C4649BE93EC2527BC10CA8269AD15`.

O status é `verified-windowed-frame-capture`, com proveniência
`managed-software-framebuffer`; isso confirma que o loop Win32 chegou à
rasterização e produziu uma imagem não uniforme, mas não é screenshot
independente da composição do desktop e não cria binding original de mesh,
material, UV, skinning, rigging, animação, morph, GPU ou áudio.
`original_semantics=not-claimed` permanece explícito.

## Fechamento verificável da etapa — 2026-10-06

Após a alteração, o build Release terminou com `0` avisos e `0` erros; o
self-test passou `72/72`; `python -m unittest tools.tests.test_fifa_analysis`
passou `11/11`; e `tools/run-clean-room-authored-matrix.ps1` terminou com
`matrix_status=completed-authored-and-candidate-opt-in`. A matriz executou os
cenários authored, o cenário candidate opt-in, smokes, runtime headless e
replays determinísticos; os pares de replay retornaram `matched=True`.

Hash atual do DLL Release: `FF3514309EEB796CD009FED5B9D9B1A4A48E2A5C4B2B3A92C660085B85503917`.
O readiness continua deliberadamente `NOT_READY`, SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
A ISO continuou somente leitura, com `2013003776` bytes, atributo `Archive` e
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Regressão Win32 incorporada à matriz — 2026-10-06

`tools/run-clean-room-authored-matrix.ps1` agora executa também o host
`run-windowed` com `--run-seconds 2`, todos os flags candidate/clean-room
explícitos e `--capture-frame`. A própria matriz validou
`header=P6 960x540`, `bytes=1555215` e `nonuniform=True`; isso transforma a
captura em uma regressão repetível do loop Win32 managed. O subgate continua
authored/managed e não altera os gates `assets`, `rendering`, `audio` ou
`animation-deformation` originais.

## Readiness após o subgate Win32 — 2026-10-06

O auditor de readiness agora registra `rendering-authored-windowed-runtime`
como `verified-authored`, `1/1`, usando a captura PPM persistida. O relatório
atual permanece `NOT_READY`: o gate original `rendering` continua
`not-proven`, `assets` continua candidate-only, `audio` continua parcial e
`animation-deformation` continua structural-only. A captura é apenas
`provenance=managed-software-framebuffer`, com
`original_semantics=not-claimed`.

Após o build Release e a matriz, os artefatos correntes são:

- DLL Release — SHA-256
  `9CCE2B05D2B76B64BEB685630D7DB30F94EC267317CBF1685C7231F8017546C1`.
- `fifa_pc/project-readiness.tsv` — SHA-256
  `ACF4212182C6BE8F44D07FFE424B76D3C82EA7E1A411D9512040F25DFC6E547F`.
- `fifa_pc/windowed-runtime-capture.ppm` — SHA-256
  `E1D1FE0294EA554F601F384C15D87B3FEA4C4649BE93EC2527BC10CA8269AD15`.
- ISO preservada, `2.013.003.776` bytes, atributo `Archive`, SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — contexto estático de renderização no XBE — 2026-10-06

`tools/analysis/xbe_render_static_cluster_audit.py` passou a agrupar, em
modo somente leitura, literais relacionados a D3D/renderização/geometria e
suas chamadas executáveis próximas a partir dos relatórios de xrefs do XBE.
O resultado atual contém `494` referências, `88` literais distintos, `3949`
linhas de contexto, `3917` contextos com chamada executável e `854` destinos
executáveis distintos. O readiness registra
`rendering-static-xbe-context=verified-structural` (`88/1`).

Isso é evidência estática de contexto/coocorrência, com proveniência
`retail-XBE-static-xref+local-call-context; no-execution; no-binding`; não
recupera funções, não decodifica comandos GPU e não atribui mesh, material,
UV, skinning, rigging, animação ou morph. O gate original `rendering` continua
`not-proven` e `original_semantics=not-claimed`.

Validação da etapa: build Release `0` avisos/`0` erros, self-test `72/72`,
testes Python `12/12` e matriz completa authored/candidate com runtime,
replays, smokes e verificação da ISO aprovados.

Relatórios: `fifa_pc/xbe-render-static-literal-audit.tsv` — SHA-256
`8575E3823B9AE7A48BC81594832BF833B5532DE6F25A29B078924C774803CA06`;
`fifa_pc/xbe-render-static-call-clusters.tsv` — SHA-256
`FDA08E3F34F11FB4C4F9B17DCF8AD6FA2477D52CD30DF8E84831F356DA01DE59`;
resumo — SHA-256
`DF4959A878D1B9A901C0EF70CBADE437F7116287BCA5368D0DAA4E11D4E820E3`.
Após a matriz, o readiness tem SHA-256
`651F94A67005984CB2D586A6D0FA0090BFCFBD722A37ED19F7E7DB8FCFABE3BE` e a
DLL Release tem SHA-256
`53963535363931315FCA1511EAC9F38F6925AE6C019788086872BD30C1C59176`.

## Avanço verificável — cross-check textual XBE→XDVDFS — 2026-10-06

Foi adicionado `tools/analysis/xbe_asset_literal_audit.py`, somente-leitura,
que cruza literais de caminhos encontrados no `default.xbe` com a árvore
XDVDFS da mesma ISO. A execução real encontrou `108` literais únicos (`702`
ocorrências), dos quais `84` têm correspondência textual na árvore (`81`
por prefixo `data/` e `3` por basename único). Isso confirma apenas texto do
XBE e presença de caminho; não confirma que o caminho foi carregado nem cria
binding de mesh, material, UV, áudio, evento, animação, rigging, skinning,
morph ou GPU. `status=verified-textual-xbe-path-crosscheck`,
`original_semantics=not-claimed`.

Relatório: `fifa_pc/xbe-asset-literal-audit.tsv`, SHA-256
`95C13FC106CA4F24586AD972DA8A5189B05DE80F9FE5C45612FCD4E2334550AA`;
resumo: `fifa_pc/xbe-asset-literal-summary.tsv`, SHA-256
`5F9731B918075E217BF63D66761228D2B43F10F4712482FB0BEB4D7B37739580`.
O XBE lido teve SHA-256
`5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EADEA910B`.

## Avanço verificável — cross-check textual XBE→entradas BIG aninhadas — 2026-10-06

Foi adicionado `tools/analysis/xbe_asset_nested_literal_audit.py`. Em modo
somente leitura, ele valida índices BIG de topo, percorre entradas `.big`
aninhadas e decodifica somente o RefPack limitado necessário para alcançar o
próximo índice. Foram indexados `4.796` caminhos internos únicos e `13` dos
`108` literais de asset do XBE tiveram correspondência textual: `10` únicas e
`3` ambíguas. Exemplos únicos incluem `models/skel0000.ord` e
`models/shdw0001.ord`; as ambiguidades incluem `pitch.xsh`, `standard.xsh` e
`stadium.big`. Isso confirma apenas nome/índice textual; não confirma
carregamento, seleção, material, UV, mesh, áudio, evento, animação, rigging,
skinning, morph ou GPU. `original_semantics=not-claimed`.

Relatório: `fifa_pc/xbe-asset-nested-literal-audit.tsv`, SHA-256
`8570C2962A3BC5C7820111A8B56324D8E3DBD3784B4C81A6013EACD053A364CA`;
resumo: `fifa_pc/xbe-asset-nested-literal-summary.tsv`, SHA-256
`9B0E93677A6D0CB2B30DB57D2907ECFF5274B26F18B7DF7BA2F8769C9348551C`.
Status: `verified-textual-xbe-nested-big-entry-crosscheck`.

## Avanço verificável — join de entradas BIG com xrefs estáticos do XBE — 2026-10-06

Foi adicionado `tools/analysis/xbe_nested_literal_xref_audit.py`, que cruza
as `13` correspondências BIG anteriores com o relatório de xrefs e o contexto
de chamadas locais do `default.xbe`. O join encontrou `8` literais com xrefs
exatos, `10` xrefs no total, `70` linhas de contexto local e `24` destinos
executáveis distintos no caso ambíguo `stadium.big`. O resultado é apenas
coocorrência estática de texto/endereço; não recupera funções nem cria
binding de loader, mesh, material, UV, áudio, evento, animação, rigging,
skinning, morph ou GPU. `original_semantics=not-claimed`.

Relatório: `fifa_pc/xbe-nested-literal-xref-audit.tsv`, SHA-256
`63418DEF893A32A5401D2993481CE11CB2A17215A7E19A860413181D11C4CEA2`;
resumo: `fifa_pc/xbe-nested-literal-xref-summary.tsv`, SHA-256
`6B84A9CBA014F2148D3E290443C15A3DB2384BA09DDA9D1DB28F057E0279A409`.
Status: `verified-static-nested-entry-xref-join`.

## Avanço verificável — clusters estáticos de áudio no XBE — 2026-10-06

Foi adicionado `tools/analysis/xbe_audio_static_cluster_audit.py`. Ele
seleciona somente literais textuais claramente relacionados a áudio, cruza os
xrefs já auditados com o contexto local de chamadas diretas e reanexa as
correspondências textuais XDVDFS/BIG. O resultado atual contém `58` xrefs de
`23` literais, `147` linhas de contexto, `108` contextos com destino
executável, `56` destinos distintos e `14` destinos com mais de um literal de
áudio. Apenas `1` literal tem correspondência BIG aninhada
(`audio/tunavars.txt`); `10` têm correspondência textual XDVDFS. Não houve
literal `.evt` entre os xrefs selecionados (`0` linhas), o que é um controle
negativo do texto do XBE, não prova de ausência de eventos em dados binários.

Relatórios: `fifa_pc/xbe-audio-static-literal-audit.tsv` (SHA-256
`6C8595C928BF81CB6F2D24ADC95558B7E0A0860FBEBDEF9988C6E381537AB7A8`),
`fifa_pc/xbe-audio-static-call-clusters.tsv` (SHA-256
`F94064F2376FEF148ED7D6CB5EBA865DED694A21A602437069D155359C6B16F2`) e
`fifa_pc/xbe-audio-static-summary.tsv` (SHA-256
`6B99625A17F55F3ED60382ED0D4FB1021B5A3265CF172ECF83067069A61842DF`).
Esses relatórios são somente coocorrência estática; não recuperam funções,
não selecionam evento/amostra/ABKC e não criam binding ou semântica original.
`original_semantics=not-claimed` permanece explícito.

## Avanço verificável — inventário dos payloads BIG nomeados pelo XBE — 2026-10-06

Foi adicionado `tools/analysis/xbe_nested_payload_inventory.py`, que segue as
13 linhas correspondentes do relatório BIG e registra somente bytes alcançados,
tamanho, cabeçalho, formato estrutural e SHA-256. Foram inventariados `41`
caminhos concretos, `32` hashes distintos e `15` payloads RefPack. Os formatos
observados incluem `2` ELF 32-bit, `2` sidecars ELF `.orl`, `1` BILG, `20`
SHPX cru, `2` SHPX encapsulados em RefPack, `12` índices BIG encapsulados e
`1` payload textual. Exemplos unívocos: `models/skel0000.ord` começa com
ELF e `models/skel0000.orl` com `.symtab/.strtab`; isso é validação de bytes e
pareamento de caminho, não interpretação de mesh, rig, material, UV ou
skinning.

Relatório `fifa_pc/xbe-nested-payload-inventory.tsv`, SHA-256
`232235B37AACC2A943E1EFFFCBE853F99E3A3F281C5E16A705E0241665709BC9`; resumo
`fifa_pc/xbe-nested-payload-summary.tsv`, SHA-256
`668F8FCEEBDFA76CC2770B663A78DB7C9D413D3FAEE8570E57E28562401004C8`.
Status `verified-nested-payload-inventory`; `original_semantics=not-claimed`.

## Avanço mais recente — readiness consumindo o subgate de payloads EVT — 2026-10-06

O `project-readiness-audit` passou a ler o resumo
`audio-named-event-runtime` e confirmou o subgate candidate `17/17`:
metadados validados, payloads preparados e cabeçalhos WAVE válidos. Esse
subgate não altera o gate original `audio`, que permanece
`partially-integrated`/`gate_passed=false`; a seleção direta gameplay→evento→
amostra/ABKC continua `not-proven` e `original_semantics=not-claimed`.

Release `0/0`, self-test `72/72`, matriz completa, smokes, runtime headless e
replays determinísticos passaram. Full-stack authored gerado/replay:
`727F8A81A11F2F5798E6A5E2C7488C938C98DEF0A2CCC0E7179F1A0273D8072D`; audit
candidate:
`DEF0B77FEF42B1474E58ED06CEC471BDBDB1502741D2C6CF3DCD8F9D07E8E6EB`; DLL
Release:
`A17E88262121F0C0057DE43D01F205237016F491A1BB69908BA7582E62EC5975`.
O relatório full-stack persiste `opponent_ai_enabled=True`, exigido pelo
readiness authored; isso é somente IA nova do runtime managed e não semântica
original.
Readiness `NOT_READY`, SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
A ISO permanece intacta, com `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Registro da etapa — trace authored explícito de TACKLE

O readiness passou a registrar o cenário authored `gameplay-tackle-trace` em
separado. O trace e seu replay têm `5/5` eventos idênticos:
`KICKOFF`, `STEAL`, `TACKLE`, `STEAL` e `SHOT_AWAY`. A ação `TACKLE` está
presente, mas continua explicitamente authored clean-room; a evidência não
altera a semântica original nem o gate geral de gameplay.

O subgate está `verified-authored`, com próximo requisito de evidência
independente para controle defensivo e transferência de posse originais. O
trace e replay têm SHA-256 idêntico:
`AC986AB9737AD4A498B9353C693B624C646EE8901208B0C5FCDC63C530552B53`.

Validação: Release `0/0`, self-test `72/72`, testes Python `19/19`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed passaram. A ISO continua somente leitura,
com `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Registro da etapa — slices dos words candidatos de indireções

Foi adicionada `tools/analysis/xbe_render_indirect_reference_slice_audit.py`.
Partindo exclusivamente dos `8` rows do inventário anterior, ela agrupou `3`
words brutas distintas e disassemblou cada uma em janela executável bounded de
`256` bytes. Os `3/3` candidates foram disassemblados e os `3/3` têm `ret`
alcançável; não houve prologue-like claim (`0`).

Esse resultado é somente uma caracterização estática de bytes candidatos. Não
executa os words, não segue chamadas, não recupera limites de função e não
atribui materiais, UVs, skinning, rigging, animação, morphs ou comandos GPU.
O subgate `rendering-static-xbe-indirect-reference-word-slices` está
`verified-structural`; o gate principal de renderização segue `not-proven` e
`original_semantics=not-claimed`.

Artefatos: `fifa_pc/xbe-render-indirect-reference-slice-audit.tsv` (SHA-256
`16FB2AA7CDC423A1C70D4588C6C5731FD5FACEEE1D59F7BDB0E3A3ACA253444C`) e
`fifa_pc/xbe-render-indirect-reference-slice-summary.tsv` (SHA-256
`4175B05F115DCD37BC9048C37369D8A3E42F12B30F2128E7AB3464BD64C67AF5`).

Validação: Release `0/0`, self-test `72/72`, testes Python `19/19`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed passaram. A ISO permanece somente leitura,
com `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Correção da etapa — readiness do trace aimed de input

O readiness ainda procurava o arquivo legado
`gameplay-clean-room-aimed-input-trace.tsv`, cujo cabeçalho não contém o campo
`tackle` acrescentado ao contrato atual. A matriz já produzia o artefato
autoritativo `gameplay-authored-aimed-input-trace.tsv`; o leitor agora prefere
esse arquivo e mantém fallback para o fixture legado do self-test.

Com a correção, o subgate `gameplay-aimed-input-trace` passou a
`verified-authored`, com `80/80` amostras, incluindo `frame=67`,
`move_y=0.75` e `shoot=True`. Isso continua sendo comportamento authored
clean-room, não evidência de mapeamento original de controle; o próximo
requisito segue sendo evidência independente para qualquer semântica original.

Validação da correção: Release `0/0`, self-test `72/72`, runtime aimed de `80`
frames, replay de match/input/animação/áudio todos `matched=True` e testes
Python `18/18`. SHA-256 do input trace e replay: ambos
`0A1D6B3B30E96B3ECA8A66A0811119AC03A82130914315B547C9276505B010A4`.
O projeto permanece `NOT_READY`; a ISO segue somente leitura com
`2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Registro da etapa — cadeia estrutural bruta de renderização EAGL — 2026-10-07

Foi adicionado `eagl-original-render-chain-audit`, que inspeciona os objetos
`.ord/.orl` same-trunk sem carregar mesh candidate ou textura SHPX. A cadeia
`__Model -> descriptor -> render method -> global parameters -> command stream`
passou em `388/388` pacotes, `883/883` objetos e `2364/2364` primitivas com
streams completos; `SHAPENAME` aparece em `752` linhas e `SetPrimitiveType` em
`1612`, sempre como estado não decodificado.

O subgate `rendering-original-render-chain-structure` é
`verified-structural`, mas não associa estado a geometria, UV, material ou GPU
e não muda `NOT_READY`; `original_semantics=not-claimed` permanece. Relatório:
`fifa_pc/eagl-original-render-chain.tsv`, SHA-256
`22BE05FEA7A6EC6ACCC9792C3ADED3BE1DB639E9BDD34F35E0BCFD258B9207BB`; resumo:
`fifa_pc/eagl-original-render-chain-summary.tsv`, SHA-256
`66E8F22E71851056985FB1FF2AFECDAAEF7B87B39C66961A1483E75EA38E1E7F`.

Validação: Release `0/0`, self-test `74/74`, Python `32/32`, matriz completa
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, captura
windowed e replays determinísticos. A ISO manteve `2013003776` bytes,
atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.40. Procurar associação independente entre os estados brutos e os grupos de
      geometria/UV/material, ou registrar controle negativo explícito; manter
      separados os `216` layouts candidate completos e os `2` incompletos.
1. Repetir Release, self-test, Python, smokes, runtime, replay e SHA-256 da ISO
   após qualquer mudança; manter `NOT_READY`.
2. Não marcar o projeto como concluído e não iniciar code review.

## Registro da etapa — formas estruturais dos acessores da tabela indexada — 2026-10-07

Foi implementada `tools/analysis/xbe_render_indirect_indexed_accessor_audit.py`
e sua integração na matriz. A auditoria observa, em janelas de até `256`
bytes, os destinos executáveis da tabela indexada e para no primeiro `ret`
observado. No XBE retail sem escrita foram confirmadas `87` ocorrências,
`81` destinos distintos, `81/81` mapeados/disassemblados e `81/81` terminados
por `ret`.

As formas bounded foram `1` fallback `pop/pop/ret`, `55` formas
`mov-memory/ret`, `24` formas `lea/push/call/ret` e `1` outra forma. Há `107`
deslocamentos de memória distintos e `1` alvo direto distinto de chamada.
Esses números comprovam somente uma regularidade estrutural observada; não
comprovam execução, limites de função, layout de objeto ou semântica. Não foi
criado binding para geometria, materiais, UVs, skinning, rigging, animação,
morphs ou GPU; `original_semantics=not-claimed` permanece.

Artefatos: `fifa_pc/xbe-render-indirect-indexed-accessor-audit.tsv` e
`fifa_pc/xbe-render-indirect-indexed-accessor-summary.tsv`. O readiness agora
registra `rendering-static-xbe-indirect-indexed-accessor-shapes` como
`verified-structural`, sem promover o gate principal de renderização, que
continua `not-proven`.

Validação: Release `0/0`, self-test `72/72`, testes Python `32/32`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime headless,
replays determinísticos e captura windowed passaram. Assets continuam
candidate-only, áudio parcial e animação/deformação estrutural. A ISO ficou
inalterada, com tamanho `2013003776`, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Registro da etapa — associação estrutural de slots e registros `__VB` — 2026-10-07

Foi implementado `fifa_pc/Runtime/EaglCandidateFetchAssociationAudit.cs` e o
comando `eagl-candidate-fetch-association-audit`. A auditoria somente candidate
same-trunk cruzou os slots observados nos streams com os registros `__VB`
validados por símbolo, stride, comprimento e contagem de elementos. Foram
confirmados `24/24` assets carregados, `218` primitivas, `216` layouts completos
de cinco slots, `1080/1080` registros válidos, `22/24` assets com todas as
primitivas nesse layout e `0` rejeições.

Isso é associação de metadados estruturais, não binding semântico. Os slots,
nomes e números continuam opacos; não foram nomeados como UV, material,
normal, peso, skinning, rigging, animação, morph ou GPU. `original_semantics` é
`not-claimed`; o subgate `rendering-candidate-fetch-record-association` é
`verified-structural`, sem promoção de assets candidate ou do gate principal de
renderização.

Artefatos: `fifa_pc/eagl-candidate-fetch-association.tsv` e
`fifa_pc/eagl-candidate-fetch-association-summary.tsv`.

Validação: Release `0/0`, self-test `73/73`, testes Python `32/32`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, captura
windowed e replays determinísticos passaram. A ISO permaneceu sem escrita,
com `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.39. Buscar associação independente fora do caminho candidate entre estado de
      renderização e geometria/UV/material, mantendo os `216` layouts completos
      e os `2` assets parcialmente completos como evidência separada.
1. Não converter slots, strides, símbolos ou formatos de comando em semântica;
   repetir Release, self-test, smokes, runtime, replay e SHA-256 da ISO após
   qualquer mudança.
2. Manter `NOT_READY`, não marcar o projeto como concluído e não iniciar code
   review.

## Próxima etapa recomendada

0.38. Preservar o subgate de formas dos acessores como controle estrutural e
      procurar somente evidência independente de associação entre estado de
      renderização e geometria/UV/material, ou evidência externa verificável.
      Não transformar `mov`, `lea`, `push`, `call` ou `ret` em campos, funções
      ou bindings por inferência.
1. Reexecutar o `project-readiness-audit` após qualquer mudança e manter
   `NOT_READY` enquanto os gates originais permanecerem abertos.
2. Continuar assets, rendering, gameplay, áudio e animação somente com
   proveniência separada e resultados reproduzíveis; não marcar o projeto como
   concluído e não iniciar code review.

## Próxima etapa recomendada após a varredura de indireção

A interseção bounded dos `81` alvos candidatos com os `87` alvos de chamadas
diretas e os `64` alvos da janela de render também foi concluída, com
interseções `0`/`0`. Esse resultado é apenas controle negativo estrutural: não
prova independência de comportamento nem recuperação de funções, chamadas ou
semântica.

O CFG bounded dos mesmos candidatos também foi concluído: `81/81` CFGs e
`81/81` `ret` alcançáveis, `421` instruções alcançáveis e `105` blocos básicos.
Isso continua sendo fluxo local estático, sem declarar limites de função ou
semântica.

A contagem também foi separada entre ocorrências e endereços distintos: `24`
chamadas diretas foram registradas nos slices, mas há somente `1` endereço
distinto global (`0x0016BF90`). O join com `87` alvos diretos, `64` alvos de
janela e `235` nós do grafo produziu interseções `0`/`0`/`0`; isso permanece
controle negativo estrutural, sem recuperação de função ou semântica.

Esse único destino também foi mapeado em `.text` e disassemblado em uma janela
bounded de `256` bytes: `76` instruções, `ret` observado e nenhum prólogo
reconhecido. O resultado ainda é caminho candidato e não uma função recuperada;
chamadas não foram seguidas.

Os `24` call-sites também foram registrados com contexto local: todos mostram
`lea [ecx+deslocamento]`, `push edx` antes da preparação e `ret 4` após a
chamada, com `24` deslocamentos distintos. Isso é regularidade estrutural, não
identificação de wrapper, função, objeto ou binding.

A varredura indexada concluída nesta etapa fornece apenas um prefixo candidato
de `87` words executáveis. O próximo recorte deve buscar, se existir,
recuperação independente de comprimento, ponteiro e limite de função da tabela
ou uma observação externa repetível que permita validar renderização, áudio ou
animação. Sem essa evidência, manter `rendering=not-proven`,
`assets=candidate-only`, `audio=partially-integrated` e
`animation-deformation=structural-only`.

Continuar somente com leituras bounded e reprodutíveis, sem execução do XBE e
sem inventar bindings de materiais, UVs, skinning, rigging, animação, morphs ou
comandos GPU. A próxima mudança deve repetir Release, self-test, testes Python,
smokes, runtime, replays e SHA-256 da ISO antes de ser registrada.

## Registro da etapa — words referenciadas por indireções do renderizador

Foi adicionada `tools/analysis/xbe_render_indirect_reference_audit.py` como
continuação estritamente estrutural do inventário de `22` transferências
indiretas. A análise fez somente leituras bounded de quatro bytes nas
referências sem base explícita: `8` referências, sendo `7` absolutas e `1`
base de tabela indexada. Todas as `8/8` palavras foram lidas dentro de limites
válidos e `8/8` caíram em seções executáveis; há `3` endereços de referência e
`3` words brutas distintas.

Isso não é resolução de alvo nem binding. Nenhuma referência foi chamada ou
seguida em runtime, e nenhum significado foi atribuído a endereço, material,
UV, skinning, rigging, animação, morph ou comando GPU. Os words permanecem
caminhos candidatos; `original_semantics=not-claimed`. O readiness registra o
subgate `rendering-static-xbe-indirect-reference-words=verified-structural`,
enquanto o gate principal de renderização permanece `not-proven`.

Artefatos: `fifa_pc/xbe-render-indirect-reference-audit.tsv` (SHA-256
`9E32F0744ACB8BDA565993876F81F2EDE789C574F08FA56BAA198524E0B25836`) e
`fifa_pc/xbe-render-indirect-reference-summary.tsv` (SHA-256
`3FF5EBDE33A7ECB8A96BCE8AD8B4CB0609C95BE0FA682DBE56BD8ED7796BE38A`).

Validação: Release `0/0`, self-test `72/72`, testes Python `18/18`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed passaram. A ISO continua somente leitura,
com `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Registro da etapa — inventário de transferências indiretas de renderização — 2026-10-06

Foi adicionada `tools/analysis/xbe_render_indirect_transfer_audit.py`. Ela
revisita os 64 CFGs limitados de renderização e registra somente instruções
alcançáveis `call`/salto cujo destino não é imediato. O resultado foi `22`
transferências em `15` alvos: `21` chamadas indiretas e `1` salto indireto;
`21` usam operandos de memória, `1` registrador, `7` têm memória absoluta e há
`11` formas de operando distintas.

Os operandos são preservados como observação x86 bounded. Nenhum ponteiro foi
desreferenciado, nenhum alvo indireto foi promovido a função, e não foram
atribuídos materiais, UVs, skinning, rigging, animação, morphs ou GPU. O novo
subgate `rendering-static-xbe-indirect-transfers` é
`verified-structural`; o gate original de renderização continua `not-proven`.

Relatórios: `fifa_pc/xbe-render-indirect-transfer-audit.tsv`, SHA-256
`265A0ACC8B3369F34D68EE5FD5563587978977CD83097C1EEBDF5ED743F7E926`, e
`fifa_pc/xbe-render-indirect-transfer-summary.tsv`, SHA-256
`37D34311B33DD25DB1CB034DB2D0C3733D0AB843313D405D22647728087C7F6E`.

Validação: Release `0/0`, self-test `72/72`, testes Python `17/17`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replay
determinístico e captura windowed passaram. A ISO ficou somente leitura,
`2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Registro da etapa — controle defensivo authored `TACKLE` — 2026-10-06

Foi integrada uma cadeia defensiva authored no runtime clean-room .NET:
`MatchInput.Tackle`, décima coluna opcional dos scripts de input, tecla `K` no
host Win32 e `TackleDemoInputSource`. Sob posse authored do adversário, dentro
do raio de posse e na borda do input, a regra transfere a bola para a casa,
concede 0,20 s de proteção e adiciona 10 ao medidor de gamebreaker. Essa é uma
regra nova do runtime; não é input, colisão, animação, áudio ou semântica
original decodificada.

A apresentação usa o perfil defensivo authored compartilhado com `STEAL`; a
política de áudio adiciona somente a rota clean-room `TACKLE -> tackle`. Ela é
candidate-only, não um vínculo EVT/ABKC/sample original. Nenhuma semântica de
materiais, UVs, skinning, rigging, animação, morphs ou GPU foi inventada.

O cenário de 100 frames produziu `KICKOFF, STEAL, TACKLE, STEAL, SHOT_AWAY`;
match/input/animação/áudio tiveram replay determinístico coincidente. A matriz
completa passou com 10/10 cenários, 54 eventos de gameplay e áudio, 777 frames
renderizados, 1.554 amostras de animação e zero eventos de áudio não mapeados.
Release: `0` avisos/erros; self-test: `72/72`; testes Python: `16/16`;
`matrix_status=completed-authored-and-candidate-opt-in`.

O readiness continua `NOT_READY`: assets são candidatos, renderização original
segue `not-proven`, áudio geral permanece parcial e animação/deformação é apenas
estrutural. A ISO não foi escrita: tamanho `2013003776`, atributo `Archive`,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Artefatos: `fifa_pc/project-readiness.tsv` SHA-256
`4E4991215D3E53EE1EAC27CBF5D5D5B813A112F14096A20B5549DDBA620930F6` e DLL
Release SHA-256
`EAB3E92D2ACE4C11F2D6FFFDFFB9281F50643F311AA22541E5236A73CD0636DD`.

## Registro da etapa — slices estáticos one-hop de alvos de chamada do renderizador

Foi adicionada `tools/analysis/xbe_render_call_target_audit.py`. Ela coleta
somente os alvos de chamadas diretas encontrados nos 64 CFGs limitados de
renderização, sem seguir chamadas recursivamente e sem tentar resolver
transferências indiretas. O resultado foi `64` janelas-fonte, `42` com
chamadas diretas, `87` alvos diretos únicos, `87/87` mapeados/disassemblados,
`75` terminados por `ret` e `28` com prólogo semelhante a função.

Isso é evidência estrutural one-hop do XBE retail da mesma ISO: não é
recuperação completa de funções, não executa o XBE, não resolve indireções e
não atribui materiais, UVs, skinning, rigging, animação, morphs ou comandos
GPU. `no-function-boundary-claim`, `no-binding` e
`original_semantics=not-claimed` permanecem.

Artefatos: `fifa_pc/xbe-render-call-target-audit.tsv` e
`fifa_pc/xbe-render-call-target-summary.tsv`. O subgate
`rendering-static-xbe-call-target-slices` está `verified-structural`; o gate
principal de renderização segue `not-proven`. Assets continuam
`candidate-integrated`, áudio `partially-integrated`, animação/deformação
`structural-only` e gameplay original não reivindicado.

Validação: Release `0/0`, self-test `72/72`, testes Python `15/15`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed passaram. A cobertura authored final foi
`9/9` cenários, `677` frames, `49` eventos de áudio e `1354` amostras de
animação; não ficou processo `FifaStreetPc` ativo. A ISO continua somente
leitura, com `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — payloads EVT nomeados preparados no runtime managed — 2026-10-06

O novo `audio-named-event-runtime-audit`, integrado à matriz, validou
`17/17` candidatos EVT nomeados, preparou `17/17` no runtime managed, confirmou
`17/17` cabeçalhos RIFF/WAVE e preparou `2.914.952` bytes PCM. Relatório:
`fifa_pc/audio-named-event-runtime.tsv`, SHA-256
`1F4D60065756162D29E665DCB794D9D31F34FACC88DBD5FC38E16D70E8503CDF`; resumo:
`fifa_pc/audio-named-event-runtime-summary.tsv`, SHA-256
`DFA25F7E59B247C836879445140D152DA65E7613534013CC52E3B7B7057211D5`.

O resultado é `verified-runtime-named-payload`, com proveniência
`exact-EVT-name+parallel-data-range; managed-runtime-named-payload-only` e
`original_semantics=not-claimed`. Não foi atribuído evento de gameplay,
seleção ABKC/amostra ou semântica original. O gate de áudio permanece
`partially-integrated`/`NOT_READY`; assets são candidate-only, rendering não
proven e deformação/animação structural-only.

Release `0/0`, self-test `72/72`, smokes, runtime headless, matriz completa e
replays determinísticos passaram. Full-stack authored gerado/replay:
`40C04455E9090663DE6F5ED3C950491B6D46043FD5BC472C56CC337B70B32B06`; DLL
Release:
`965FA17D8B58B56688351A3183B5EB41ED0DBA64F4E1F327D4669E7B1CC26A2F`.
Readiness permanece `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
A ISO permanece sem escrita, com `2013003776` bytes, atributo `Archive` e
SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — completude dos streams candidate — 2026-10-06

O audit de proveniência candidate agora verifica, em `24/24` casos, que os
streams de comandos de geometria e os streams de estado `SHAPENAME` estão
completos: `candidate_geometry_streams_complete=24` e
`candidate_shape_state_streams_complete=24`. O audit integrado exige os dois
marcadores e passou na matriz. A evidência continua candidate-only; não foi
atribuído binding original de estado→geometria, material, UV ou GPU.

Release `0/0`, self-test `71/71`, smokes relevantes, runtime headless e
replays determinísticos passaram. Full-stack gerado/replay: SHA-256
`588AD564F93E4B2BB048FB121ABF9D0C76A310F69643DB4A0A0AE3AD7730FB49`;
relatório candidate:
`CB3364DC07F8FB90817BBB473F39D637256256AC210B6DDA747BC412B8B66863`;
resumo:
`00F678130853D38B3F2AB190B96F77B1CA850C81F13788ECF230B4BDC90A207D`;
audit integrado:
`22A7DC9360E35839C8581ECF1553A9418CFED637B0AE6DF38FC1426E00F48E3B`;
DLL Release:
`75137D91EECCAB5444E8FE40D5564F5FFEB95313639DAFDB1B982501819855CF`.
Readiness continua `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
`original_semantics=not-claimed` permanece explícito. A ISO não foi escrita:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — audit candidate consumindo proveniência estrutural — 2026-10-06

`candidate-integrated-runtime-audit` passou a consumir o resumo de proveniência
gerado na mesma matriz. Agora o runtime candidate só é `verified-candidate`
quando `candidate_asset_provenance_valid=True` e
`candidate_mesh_state_crosscheck_valid=True`. Resultado: `24/24` candidatos,
objetos `.ord`, companions `.orl`, bounds e geometria finita/indexada.

Release `0/0`, self-test `71/71`, smokes, runtime headless e replays passaram.
Full-stack gerado/replay: SHA-256
`9569E64F059CFE2E1070448886786D19335996759054FD9B8277CF49327B071D`; audit
candidate: `42A23A7F851D59A00FACE9E72C0AF28A65DA4F5FDCC5278FD283EABE228BC97E`;
DLL Release: `43E2F4FB1C9E61F3734DF3FDACE9FA5CFD6A5FF40FB272A9CB41DFDA5AEA061A`.
Readiness continua `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
Nenhuma semântica original foi promovida e a ISO permanece intacta, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — cross-check candidate de malha e estado — 2026-10-06

O auditor `eagl-candidate-asset-provenance-audit` agora relê também os
objetos `.ord/.orl` candidate. A execução real confirmou `24/24` objetos,
`24/24` companions, bounds válidos, geometria finita/indexada e
`SHAPENAME` nos primitivos em `24/24` casos. Relatório SHA-256:
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo:
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.

Release `0/0`, self-test `71/71`, matriz authored/candidate, smokes, runtime
headless e replays determinísticos passaram. Os relatórios full-stack coincidem,
SHA-256 `5B42B2CCB1A548D0B6EDA60DF1E0DD05B4D73E06CDF9C287CE24165555B02A73`;
DLL Release SHA-256
`77921A6EFF6715DD13B808C489D75FB465B351FE3D8FB84466DC00599F3FA05C`.
Readiness `NOT_READY`, TSV SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
O cross-check permanece candidate-only e `original_semantics=not-claimed`;
não cria binding de estado, material, UV, GPU, skinning, rigging, animação ou
semântica original. ISO preservada: SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — cross-check persistido SHPX/TGA — 2026-10-06

Foi implementado `shpx-export-crosscheck`, com self-test, CLI, smoke na matriz
e documentação. A execução real releu `data/aptanims/player1.xsh` da ISO sem
escrever nela e comparou as exportações persistidas em
`exports_fifa_layer3/player1`: `46/46` entradas, `46/46` cabeçalhos TGA válidos
e `46/46` buffers RGBA idênticos. Relatório:
`fifa_pc/shpx-export-crosscheck.tsv`, SHA-256
`8EB31145E1A05CDED83C3DEA703BD1A9B24755260A650F672FC34545118AC68E`; resumo:
`fifa_pc/shpx-export-crosscheck-summary.tsv`, SHA-256
`B126B7D0AEA480A299F207BC6F688D82B1A95D769EEFFB58F19F063DDC3FDD4F`.

A validação final desta etapa passou em build Release `0/0`, self-test `71/71`,
matriz authored/candidate, smokes relevantes, runtime headless e replays
determinísticos. Os relatórios full-stack gerado/replay coincidem, SHA-256
`E65191996143C3F04C856473EB23000E0CD8C96BE936B70D810BEA308C7FCA2C`; o DLL
Release tem SHA-256
`1F02DE91890B648AAF5605FAE605A2AE20149CEA37613EA493A0892F716E33CF`.

O cross-check é somente de pixels da exportação gerada pelo workspace:
`export_provenance=workspace-generated-TGA; provenance-not-independent` e
`original_semantics=not-claimed`. Não promove assets/rendering nem cria
bindings de timing, pose, rigging, skinning, animação, morph, material, UV ou
GPU. O readiness continua `NOT_READY`, SHA-256
`264728053E62E2491DC9BD645F130F2C49807086BC5C316D4E32FDB648F7EFE1`.

A ISO continua sem escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço atual — proveniência da cadeia candidate EAGL/SHPX — 2026-10-06

`eagl-candidate-asset-provenance-audit` reabriu os 24 vínculos same-trunk em
`data/models.big`: `24/24` entradas `.big`, `24/24` `.xsh`, `24/24` payloads
SHPX válidos, `24/24` entradas internas exatas e `24/24` pixels RGBA iguais à
segunda decodificação. Relatório SHA-256:
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo:
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.

O cross-check estrutural adicional confirmou `24/24` objetos `.ord`, `24/24`
companions `.orl`, bounds válidos, geometria finita/indexada e presença de
`SHAPENAME` nos primitivos em `24/24` candidatos. Isso continua sem promover
estado→geometria, material, UV, GPU, skinning, rigging, animação ou semântica
original.

Status `verified-candidate-source-chain`; proveniência
`same-trunk-source-chain; candidate-only`; `original_semantics=not-claimed`.
Isso fortalece a rastreabilidade dos assets candidate sem promover material,
UV, GPU, skinning, rigging, animação ou binding original.

## Fechamento verificável da etapa SHPX — 2026-10-06

Build Release `0/0`, self-test `69/69` e matriz authored/candidate concluída.
O par full-stack gerado/replay coincide byte a byte, SHA-256
`05B6A34950B6D9462DAF6561CC03041D19FB5203A8E401A2693F054D9AD3281F`; o DLL
Release tem SHA-256
`DA9E629F205087991C1B995D52E41D41E301A12094A86F2DEBD013CE8C22E476`.
A auditoria SHPX tem SHA-256
`E220D9811A04BC323B899AF21C339F64E5ED5B23A9A513599B12D3D9AF5F6859` e
validou `46/46` entradas PAL8 128x128. O readiness continua `NOT_READY`,
SHA-256 `A23E61B9474FBD07E409F0D2ED2B08CD5DC94E884429F25363CAD6289DA35222`.
O relatório candidate atualizado tem SHA-256
`7295054F7C6AFECB0B919AF1EF9BE6CDA870052BBC228BB723B879397A26BB6B`.

A ISO permaneceu intacta: `2013003776` bytes, `Archive`, timestamp preservado e
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização desta etapa — bateria final verificada — 2026-10-06

Build Release: `0/0`; self-test: `68/68`. A matriz executou nove cenários
authored e um cenário candidate opt-in; os dez pares de replay determinístico
retornaram `matched=True`. O cenário candidate produziu 140 frames, 280
samples de animação, 8 eventos de áudio, 8/8 preparados/tocados e 0 sem rota;
os relatórios de integração são byte a byte iguais, SHA-256
`24561111E7BCE6CE2A11171F0960F048FA57076677C8CCB432A3209CF1CA257B`.

O gate agregado `clean-room-authored-runtime` foi verificado em `9/9`.
Readiness: `NOT_READY`, TSV SHA-256
`A23E61B9474FBD07E409F0D2ED2B08CD5DC94E884429F25363CAD6289DA35222`.
ISO preservada: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Para reproduzir esta etapa, execute
`D:\projeto zeta\tools\run-clean-room-authored-matrix.ps1`. O script gera e
reexecuta os nove cenários authored e o cenário candidate, executa as
auditorias (incluindo `candidate-integrated-runtime-audit`), os smokes relevantes
e readiness, e
falha se a ISO mudar em tamanho, timestamp, atributos ou SHA-256.

O relatório gerenciado candidate é
`fifa_pc/candidate-integrated-runtime-audit.tsv`, SHA-256
`9ABDABC1246F4D161A498609F698CC3AB05FBFB10315BF2F81C08EF995E06A6D`.
Ele verifica hash do assembly, flags candidate, frame PPM, traces e replays;
continua sendo evidência `verified-candidate`, com
`original_semantics=not-claimed`.

O `project-readiness-audit` passou a exigir esse relatório para o gate separado
`assets-candidate-runtime`, que agora está `verified-candidate` (`24/24`,
`passed=True`). Isso não promove os gates originais `assets` ou `rendering`,
que continuam respectivamente `candidate-integrated`/`False` e
`not-proven`/`False`.

## Atualização desta etapa — intake de timing externo — 2026-10-06

Foi adicionado `external-animation-timing-audit` para receber, quando existir,
um TSV externo com observações temporais explicitamente identificadas. O
validador confere hash do arquivo, proveniência, índice/tempo não negativos,
monotonicidade por captura e repetição por par banco/asset. Ele não decodifica
frames, poses, rig, skinning ou semântica original.

Nenhum TSV externo está presente no workspace. A execução produziu
`status=missing-evidence`, `0` observações, `0` capturas,
`direct_timing_binding=not-proven` e `original_semantics=not-claimed`.
Relatório SHA-256:
`3E82F4FF84E4095CA2844950A97A56FB5F165954BF3B3379A2BD4ED0DC026EDC`;
resumo:
`4FF80AD21FB2848191CC38519BE7E6BA9E86E3B34C23FA259879B8908836928E`.

## Atualização desta etapa — integração candidate opt-in — 2026-10-06

A matriz passou a gerar e reexecutar um cenário cross-system separado com
`--candidate-material`, `--candidate-home-character` e
`--candidate-away-character`. Match, input, animação e áudio deram
`matched=True`; foram verificados 140 frames, 280 samples, 8 eventos,
8/8 preparados/tocados e 0 sem rota. O relatório exigiu material validado e os
dois slots candidatos habilitados; checksum de render:
`088EC5D10096A738`, 69 checksums distintos.

Os relatórios gerado/replay são iguais, SHA-256:
`F8F03C7DC83BFD33EAD3D58526C1B19A1790A44209C7BF938819F55AB754187A`.
Esse resultado é candidate/opt-in; `render_group_state=unproven` e
`original_semantics=not-claimed` permanecem explícitos.

## Atualização desta etapa — auditoria de metadata temporal dos catálogos AXT — 2026-10-06

O auditor `animation-bank-catalog-timestamp-audit` comparou o timestamp
numérico com a data textual da mesma linha `.axt`: `19/19` bancos e
`1.584/1.584` linhas coincidiram, com offsets locais inferidos `-420` e `-480`
minutos. Isso é evidência de metadata do catálogo; não promove duração,
frames, poses, rigging, skinning ou timing de animação.

Relatório SHA-256:
`611B60012B5F37A639E2DC5AD47E829A6178FEA409A9D6CE4CA1360640222C32`; resumo:
`E167D7254E82C36D6A49276B2721A828FBD52EA048CBADAFEBCE6E33959D37FE`.
DLL Release usada na matriz: SHA-256
`D0660B1FEF4F2781152EDB95797145201F99FA4A2E0780B914CF031F0713E459`.
Readiness regenerado: SHA-256
`282D82CC21E3EE600D6DCDFF6A95B754579AB9975D2F02F661BCB039F4DC1339`, ainda
`NOT_READY`, com `direct_timing_binding=not-proven`.

## Atualização desta etapa — correlação exhaustive de áudio — 2026-10-06

Foi adicionada a auditoria `audio-event-payload-wave-correlation-exhaustive-audit`.
Com limite de `512` índices por banco, foram percorridos `368` candidatos em
`16` bancos e decodificados `239`; houve `0` pares PCM idênticos e `0` hashes
de evento correspondentes. Isso reforça somente evidência estrutural negativa;
não promove seleção EVT/ABKC/sample e mantém `direct_selection_binding=not-proven`.

Relatório SHA-256:
`BDE7CF7E58E09E17D2BBB8932E413249D51264424E5A1C32E8B5E30D404E345E`;
resumo SHA-256:
`DD798636EC35EF78E07E47176A0220C9A919601F2FF47D5CD51FE8DB41DEF23B`.

## Atualização desta etapa — cenário first-to-goals integrado — 2026-10-06

O cenário authored `first-to-goals` foi adicionado à matriz cross-system.
Ele executou `80` frames, terminou `HOME 1 - 0 AWAY` com `FULL_TIME`, gerou
`7` eventos de match/áudio e `160` samples de animação; os quatro replays
retornaram `matched=True` e o PPM authored foi validado.

Relatórios de integração gerado/replay, SHA-256
`D37356F7DE6F7EE9F04FFB83BC1D5973F6C278C1F3EAAC9C59D3D98A3089F119`.
As coberturas authored de gameplay e áudio passaram `9/9` cenários, `49/49`
eventos e `0` não mapeados, sem alterar a separação semântica original.

## Atualização desta etapa — invariantes de gameplay authored — 2026-10-06

O novo `clean-room-gameplay-invariant-audit` acompanhou nove cenários
determinísticos authored e validou estado finito, bounds de jogadores/bola,
altura da bola, relógio e placar monotônicos, sequência de eventos, término e
ações requeridas. Resultado: `9/9` cenários, `677` frames e `49` eventos,
`verified-authored`.

Relatório: `fifa_pc/clean-room-gameplay-invariants.tsv`, SHA-256
`B6C4F29036835A22BFB36D6401F2C31A37B6C66DC62D3EB477A8C5396C6433EE`.
Isso permanece separado das regras da ISO: `original_semantics=not-claimed`.

## Atualização desta etapa — correlação estrutural de áudio ampliada — 2026-10-06

`audio-event-payload-wave-correlation-audit` agora permite limitar
explicitamente as tentativas de banco com `--max-bank-attempts`. A execução
registrada com `64` encontrou `17/17` candidatos EVT e `15/16` bancos ABKC/AST
decodificados, `0` pares PCM idênticos e `0` hashes de evento correspondentes.
Política: `first_64_bank_candidates`.

Relatório SHA-256:
`8ED4E39C6748CEC54706D9F107DCFE7D887C941ED43FDF854829C60568104E3D`;
resumo SHA-256:
`4D949566DB53FA348C939388D280D94C9F825BB19BB19BA6B31FAA70C2C2A38D`.
Isso é somente evidência estrutural/negativa; não promove o gate de áudio,
mantém `direct_selection_binding=not-proven` e não reivindica semântica
original.

## Atualização desta etapa — cobertura integrada authored por cenário — 2026-10-06

Foi adicionado `fifa_pc/Runtime/CleanRoomIntegratedCoverageAudit.cs` com o
comando `clean-room-integrated-coverage-audit`. Os nove cenários authored
foram executados com renderização, assets authored de personagem/cena,
animação, áudio e quatro replays determinísticos no mesmo contrato.

Resultado: `9/9` cenários, `677` frames, `1.354` samples de animação e `49`
eventos de áudio; relatório SHA-256
`DC6E37059884B9AC450D4FD73738D9D294EC8CE1A08C707CCD49AA61EC876A7B`.
Os nove PPMs authored `960x540` também passaram, com `341.782` pixels
visíveis no frame final de cada cenário.
Release `0/0`; self-test `65/65`; full-stack authored-only gerado/reexecutado
byte a byte igual, SHA-256
`BF2F85A42960ED5DCA0E1C84DB4481BAA66749263C31FDA6708C51224C67589A`.
Cada integration report registra o SHA-256 do assembly Release atual:
`9CF41DD8555721A043BF58C6456755D2ABA3970EE417D73653EB0006775F37BB`.
Readiness `NOT_READY`, com `authored-integrated-coverage=verified-authored`,
TSV SHA-256 `EDE51254C0D754EF46463183ADE17FCA6104B941E06032506BA96535494983AC`.
Os gates originais continuam sem promoção e `original_semantics=not-claimed`.

## Atualização desta etapa — manifesto de personagem authored — 2026-10-06

Foi adicionado `fifa_pc/Runtime/CleanRoomCharacterAssets.cs` com o manifesto
`clean-room-character-manifest-v1`. Ele identifica apenas a silhueta e a
paleta authored do runtime managed; não cria binding de mesh, material, UV,
skinning, rigging, morph, animação ou GPU original.

Release `0/0`; self-test `62/62`; authored-only de 140 frames com
`authored_character_asset_frames=140`, checksum `F26A04CC3CBEBDEA` e relatório
SHA-256 `C0969EFA056C678494D70E97941A6EB6D1748E448F640FD2F84B4291F1900F33`.
Full-stack candidato com checksum `088EC5D10096A738`; os quatro replays
retornaram `matched=True`; relatórios iguais, SHA-256
`BF9FA14EB4CC5EC1580535EF05D4F97AA0F256278FA9E7CE3311F71E5EE82DE8`.
Readiness `NOT_READY`, SHA-256
`6C86A5895D66F822D0E06A2A41C89F04C37001FA0B829DE132431BC08B507330`, com o
gate `character-authored-assets` em `verified-authored`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização desta etapa — manifesto de animação authored — 2026-10-06

Foi criado `fifa_pc/Runtime/CleanRoomAnimationAssets.cs` com o manifesto
`clean-room-animation-manifest-v1`. O `CleanRoomPlayerAnimator` usa esse
manifesto para selecionar perfis procedurais authored sem modificar a saída
numérica verificada. A camada é exclusivamente do PC novo: não interpreta AXT,
frames, timing, pose, rigging, skinning ou morphs da ISO.

Release `0/0`; self-test `60/60`; headless de 80 frames com `6/6` eventos de
áudio authored; full-stack de 140 frames com render checksum
`088EC5D10096A738`; replays de match/input/animação/áudio `matched=True`.
Os smokes opt-in de `JUGGLE`, `STEAL` e `GAMEBREAKER` também passaram.
Os relatórios de integração são iguais, SHA-256
`70DE5BAB17EDE85DDF847F8C1D8B5185E944AD4051AFFF534CE2AAE4B93AAA83`.
Readiness `NOT_READY`, SHA-256
`CE868EA45F8CA772764D1BA8296A7BD7200EA2575150D572084DA0F887A8D153`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo passo: continuar com evidência independente para bindings originais e
decodificação semântica. Não promover `assets`, `rendering`, `gameplay`,
`audio` ou `animation-deformation` apenas com os manifests authored.

## Atualização desta etapa — manifesto de áudio authored — 2026-10-06

`fifa_pc/Runtime/CleanRoomAudioAssets.cs` adiciona
`clean-room-audio-manifest-v1` para os oito candidatos de cue da política PC.
O mixer valida o nome contra o manifesto antes de preparar o candidato, sem
afirmar seleção original EVT/ABKC/sample.

Release `0/0`; self-test `61/61`; full-stack com `8/8` cues preparadas/tocadas,
`0` sem rota e PCM de `1.180.164` bytes com SHA-256
`2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`.
Replays match/input/animação/áudio `matched=True`; relatórios de integração
iguais, SHA-256
`4DD380D1896481F5EEE4CACF1E46E1D3E4B2BF5CF5B42847C6FF924174B2B0F0`.
Readiness `NOT_READY`, SHA-256
`F485A2845003F98867ECCF31496761971AE6C574B06CA9EDACEF19BDEE471327`.

O próximo avanço deve buscar evidência independente para seleção original de
áudio ou para renderização/rigging, sem promover esse manifesto authored.

## Atualização desta etapa — correlação estrutural de áudio e bateria final — 2026-10-06

Foi adicionada a auditoria `audio-event-payload-wave-correlation`, que decodifica
os candidatos nomeados de EVT e um conjunto limitado dos candidatos ABKC/AST e
compara hashes PCM. Na ISO, foram observados `17/17` candidatos EVT decodificados
e `15/16` bancos candidatos decodificados; houve `0` pares PCM idênticos e
`0` hashes de evento com correspondência. Isso é controle estrutural/negativo;
não prova seleção de evento, sample, gameplay ou semântica original. O relatório
tem SHA-256 `00251215E62B4437C5B0EE3917C12E8DF66C1E278F1982EAF3AE95232E0432BD`
e o resumo `C5AEC72F58CAC11EFD2942D6F598C3401A5433475E4A3C7EF728D7893E6448F4`.

O Release fechou com `0` avisos/erros e o self-test com `59/59`. Os smokes reais
de XPR, material candidato e influência raster passaram; o headless de 80 frames
passou com `6/6` eventos de áudio tocados. O full-stack de 140 frames passou e
os replays de match/input/animação/áudio retornaram `matched=True`; os cinco
relatórios gerado/reexecutado ficaram byte a byte iguais. O readiness segue
`NOT_READY`, agora com SHA-256 `4982801DDA56466467E7DC6197BDB22EF481EE41BFF5FE07F971BDB27B8B672D`.

A ISO continua sem escrita: tamanho `2013003776`, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Os gates semânticos de assets, renderização, áudio e animação/deformação não
foram promovidos; `direct_selection_binding=not-proven` e
`original_semantics=not-claimed` permanecem explícitos.

## Atualização desta etapa — digest PCM do áudio authored — 2026-10-06

O `CatalogAudioMixer` agora registra também o PCM concatenado das cues authored
entregues ao caminho de gameplay, excluindo o header WAV. Isso verifica o
conteúdo efetivamente preparado para reprodução no runtime novo, sem atribuir
seleção EVT/ABKC original. No full-stack foram registrados `1.180.164` bytes e
SHA-256 `2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`;
o replay produziu o mesmo digest. O relatório de integração gerado e reexecutado
é byte a byte igual, SHA-256 `706E895FE9BAFD6A34BF6FB5C74A4C93110F1B6ECE193C18E8C625CD74C57823`.

O self-test permanece `59/59`, o readiness continua `NOT_READY`, com SHA-256
`336D91B1DB56E0BDF32C96BC0F093E707DA63EA04C7D06302AF3EB1A4CDBDFE9`, e
`direct_selection_binding=not-proven`/`original_semantics=not-claimed` seguem
inalterados.

## Atualização desta etapa — manifesto de assets authored da cena — 2026-10-06

Quadra, gols e bola foram extraídos do desenho inline para
`fifa_pc/Runtime/CleanRoomSceneAssets.cs`, com manifesto explícito
`clean-room-court-v1|clean-room-goal-v1|clean-room-ball-v1`. O renderer usa esse
conjunto somente como asset authored managed; não há associação com geometria,
material, UV ou estado GPU da ISO. O full-stack verificou `140/140` frames de
assets de cena e manteve o checksum de renderização `088EC5D10096A738`.

Os relatórios de integração gerado e reexecutado são byte a byte iguais, SHA-256
`CA03EC0029301E7336E0CA839DAE717A60260F2F130584E6C5051AA0AA8E72FD`; o
readiness continua `NOT_READY`, agora com SHA-256
`661F2013630F999E2A79E27682A64253E7DA671D946ADD183B8E0FA0E22976E1`.
O asset authored não promove o gate original de renderização.

## Atualização desta etapa — cobertura de formatos XPR0 — 2026-10-06

`fifa_pc/Runtime/XprTextureDecoder.cs` agora decodifica, no nível base
disponível, os códigos `R5G6B5`, `A8R8G8B8`, `X8R8G8B8`, `DXT1`, `DXT3` e
`DXT5` reconhecidos por `XprReader`. Isso amplia a cobertura de payload do
runtime managed, mas não atribui material, UV, estado GPU ou associação de
renderização à ISO. Fixtures do self-test cobrem os seis formatos.

No conteúdo real, foram reexecutados `texture-preview` para
`data/titleimage.xbx` (`128x128`, `DXT1`) e `data/saveimage.xbx`
(`64x64`, `DXT1`), ambos com a ISO somente para leitura. Os PPMs foram escritos no
workspace. Build Release: `0` avisos/erros; self-test: `58/58`. O readiness
continua `NOT_READY`, sem alterar os gates de assets/renderização. A ISO
mantém tamanho `2013003776`, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização desta etapa — intervalos de âncoras de payload — 2026-10-06

O novo `animation-bank-catalog-payload-anchor-audit` usa somente o relatório
de nomes já materializado e mede os offsets das duas strings técnicas `_e/_r`
por linha. O resultado real foi `19/19` bancos, `1584/1584` linhas completas,
`3168` âncoras, `1584` gaps intra-nome, `1568` gaps entre âncoras de linhas
diferentes e ordem estritamente crescente em `19/19` bancos. Isso é evidência
de layout/ordem dos offsets observados; os gaps não são boundaries de records e
não decodificam frame, pose, rig, timing, skinning ou animação.

Relatório: `fifa_pc/animation-bank-catalog-payload-anchor-audit.tsv`, SHA-256
`B63D77DF1E99F08576829FB016A1DEE5CB4C71F97F92E75E442826A36E481FBD`; resumo:
`372E58900EF0F173652F923DD3F49BC777B76006437D53675D91C662626E0ECA`.
O readiness adicionou `animation-catalog-payload-anchors=verified-structural`
e permanece `NOT_READY`, SHA-256
`C733311CF7340C1A8B5E0C8170B2534BFAB7F0CAA30426B810FEFDB3B85F3198`.
Self-test: `58/58`; semântica original: `not-claimed`.

## Regras obrigatórias

- A implementação é clean-room em .NET; não executar o XBE e não afirmar que o
  XBE foi recompilado.
- Preservar a ISO como somente leitura:
  `D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso`.
- Preservar alterações existentes no workspace; não usar `git reset --hard`,
  `git checkout --` ou operações destrutivas.
- Implementar, testar e documentar cada avanço.
- Manter explícitos os limites entre evidência estrutural, candidato e
  semântica comprovada.
- Não marcar o projeto como concluído e não iniciar code review antes de haver
  renderização, gameplay, assets e áudio integrados e verificados.

## Atualização desta etapa — asset de personagem authored separado — 2026-10-06

Foi adicionado `fifa_pc/Runtime/CleanRoomCharacterRenderer.cs`, código novo em
.NET que desenha uma silhueta procedural authored com paleta explícita. Ele não
é uma leitura de mesh, material, UV, skinning, rigging, morph, animação ou
comando GPU da ISO. O `PreviewRenderer` usa esse caminho somente quando os
slots de personagem candidato estão desabilitados.

O runtime authored-only foi executado em Release por 140 frames: `140/140`
frames passaram pelo asset de personagem authored, checksum de renderização
`243EF331249601A5` e `122` checksums distintos. O relatório é
`fifa_pc/gameplay-authored-character-integration.tsv`, com SHA-256
`78FEC9A56961C0EC35DC3024DB81157451B4FA4B62EC094C0AF61F26219CCA5E`.
O full-stack candidato continua separado: seus slots candidatos ficam ativos,
o checksum é `088EC5D10096A738` e `authored_character_asset_frames=0` por
desenho; isso não transforma o candidato em semântica original.

Após a alteração, o build Release terminou com `0` avisos e `0` erros e o
self-test passou `58/58`. O readiness foi regenerado e continua `NOT_READY`,
com SHA-256 `0C537BBEA1140423C73F39A4B4EEC32B8292D630065B3F7A5876767867B25A1A`.
O coverage authored continua `8/8` cenários, `42` eventos e `14` ações, com
SHA-256 `E8655638CFE171E10A14DA5B7C7CBFC6C5C3FFED2B3D879BE10F42ADA96C996D`.
Os replays full-stack de match/input/animação/áudio continuam `matched=True`;
os relatórios de integração gerado e reexecutado são iguais, com SHA-256
`AF8A2B97C05E438B267536A612D8ADE90CE21ED1C20F40A32D25FE28E92CBBFC`.

O hash da ISO permanece `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`,
com tamanho `2013003776` e atributo `Archive`. Nenhum binding original foi
declarado; rendering, materiais/UVs, rigging/animação e seleção original de
áudio continuam lacunas semânticas.

O novo `animation-bank-catalog-payload-name-audit` compara, por offset, cada
nome do catálogo `.axt` com strings ASCII do `.data` ELF do par `.ord/.orl`.
Foram validados `19/19` bancos e `1584/1584` nomes, todos como pares técnicos
`_e`/`_r`, sem ausências. Isso é evidência estrutural de identidade
catálogo↔payload; não é boundary de record nem decoder de frame, pose, rig,
timing ou skinning. O relatório detalhado tem SHA-256
`3D2CD9FBA876636011CFED8876204C1BC6F84A23334F8A06D42CA276671F4900` e o
resumo tem SHA-256
`FAE1691C64C4A99B968F7DFC8F349368892D924F10C44CBF5315178BEC3A673D`.
O readiness que incorpora essa linha `verified-structural` tem SHA-256
`0C537BBEA1140423C73F39A4B4EEC32B8292D630065B3F7A5876767867B25A1A`.

## Atualização desta etapa — gates authored separados dos gates semânticos — 2026-10-06

O `project-readiness-audit` agora materializa linhas separadas para os caminhos
que já são verificáveis no runtime managed, sem alterar os gates conservadores
da ISO. A execução real registra:

- `assets-candidate-runtime=verified-candidate`, com `24` caminhos de material
  candidato exercitados e `0` rejeições;
- `rendering-authored-runtime=verified-authored`, com `140` frames e `69`
  checksums distintos;
- `gameplay-authored-runtime=verified-authored`, com `8` eventos e trace
  determinístico escrito;
- `gameplay-authored-coverage=verified-authored`, com `8/8` cenários, `42`
  eventos e todas as `14` ações authored cobertas;
- `audio-authored-runtime=verified-authored`, com `8/8` eventos preparados e
  tocados, `0` sem rota;
- `animation-authored-runtime=verified-authored`, com `280` amostras da pose
  procedural e trace escrito.

As linhas semânticas continuam independentes: `assets` permanece
`candidate-integrated`/falso, `rendering` permanece `not-proven`/falso, `audio`
permanece `partially-integrated`/falso e `animation-deformation` permanece
`structural-only`/falso. Nenhuma linha nova atribui UV, material, skinning,
rigging, morph, timing, comando GPU ou seleção original de áudio à ISO. O
readiness geral continua `NOT_READY` por desenho.

O novo auditor é somente de trace/replay authored; não comprova regras da ISO.
O self-test passou `55/55` e o build Release passou com `0` avisos e `0` erros.
O relatório `fifa_pc/gameplay-coverage.tsv` foi escrito com `8/8` cenários
verificados; o readiness atualizado continua `NOT_READY`.
Artefatos: `fifa_pc/Runtime/GameplayCoverageAudit.cs` e
`fifa_pc/gameplay-coverage.tsv`.
SHA-256 do coverage: `E8655638CFE171E10A14DA5B7C7CBFC6C5C3FFED2B3D879BE10F42ADA96C996D`;
SHA-256 do readiness final: `75A8E6DC7B88729B92FBA635D26819738EF651F69F0964BF92E5696D85C9F6EC`.
O relatório atualizado `fifa_pc/project-readiness.tsv` tem SHA-256
`9822B80B29821C0551DAA1FB9B493E54223DAF077BABC2DEE06262D4A95C0D2B`.

## Atualização desta etapa — input no full-stack e bateria final — 2026-10-06

O relatório `CleanRoomIntegrationReport` foi ampliado para registrar
`input_samples` e `input_trace_written`. A execução Release full-stack com
material candidato home/away, gameplay authored, oponente e áudio produziu
140 frames, 140 samples de input, 8 eventos de partida e 8 eventos de áudio;
houve `8/8` preparados/tocados e `0` sem rota. Os replays de match, input e
áudio retornaram `matched=True`; os pares têm SHA-256, respectivamente:

- match: `B76AA3B7E562564387568CD6872E1A58397130BEFBAD9B68803297EDA068AF3C`;
- input: `E1E2AA0616CDB08681150E956954309851D80999D8CCD47A546033B19A713AD7`;
- áudio: `FEBA7B454A0A82C33A3E2128FBC622A3C6AF957D1FB83CB6AB7DF156EBB6D07C`.

Os dois relatórios de integração são byte a byte iguais e têm SHA-256
`FD228937D93B4305DC68FADA8A272EBB137B3B520F53AA7FAFF690736AAA7408`.
Isso valida a integração authored do runtime PC, não semântica original.

Bateria final da etapa: Release `0/0`, self-test `53/53`, headless padrão de
80 frames com checksum `5405DE72BE1B4FC1`, coverage authored de áudio `8/8`
cenários e `42/42` eventos preparados/tocados, smokes de material/runtime e
geometria `24/24`, e smoke de influência raster candidata `20` linhas. O
controle estado→geometria continua negativo (`0` pares exatos, `0` símbolos
de relocação compartilhados), logo os gates de assets/renderização/áudio
original permanecem abertos e o readiness segue `NOT_READY`.

ISO preservada: tamanho `2013003776`, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Readiness atual: `fifa_pc/project-readiness.tsv`, SHA-256
`1B9B377DE8BC55D68CC6F8F8AA07518FF7CE8800460F7BB58D32BCA6DA9E68C2`.

## Atualização desta etapa — trace da animação authored — 2026-10-06

O runtime passou a registrar a pose procedural authored que o renderer usa,
sem consultar ou interpretar AXT, morph, rig ou skinning da ISO. O novo trace
`fifa_pc/gameplay-full-stack-animation-trace.tsv` contém duas amostras por
frame, home e away, totalizando 280 amostras no full-stack de 140 frames.
O replay com `--animation-trace-expected` retornou `matched=True`; o SHA-256
igual do par é
`57C124E9CDD044CD8DE0C2319C9AD11FC35930E0589C199B87C4DB0007342D09`.

`CleanRoomIntegrationReport` agora registra `animation_samples=280` e
`animation_trace_written=True`, e o readiness continua
`full-stack-integration=verified-authored`. Essa é evidência do novo caminho
procedural clean-room, não evidência de frame, pose, rig, skinning, timing ou
animação original; o gate `animation-deformation` permanece
`structural-only`.

Fechamento verificável da etapa: Release `0/0`, self-test `54/54`, headless
padrão de 80 frames com checksum `5405DE72BE1B4FC1`, coverage authored de
áudio `8/8` cenários e `42/42` eventos, smokes de material/runtime `24/24` e
geometria `24/24`, influência raster candidata `20/24`, e ISO preservada com
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
O readiness atualizado tem SHA-256
`75A8E6DC7B88729B92FBA635D26819738EF651F69F0964BF92E5696D85C9F6EC` e segue
`NOT_READY`.

## Atualização desta etapa — controle bounded de áudio — 2026-10-06

Foram repetidos os smokes de payload sem alterar a política authored: o
decoder nomeado validou `17/17` candidatos EVT; a auditoria SCCl validou
`144/144` layouts mono únicos; e o smoke ABKC decodificou `15/16` bancos,
mantendo um banco sem candidato bounded. A busca de palavras alinhadas não
encontrou ocorrências em `pcpahdr.big`, `pcpadat.big` ou nos 16 bancos.
Isso é evidência comprovada de payload e um controle negativo de seleção, não
um vínculo original evento→sample. O gate de áudio permanece
`partially-integrated`; as rotas authored continuam explicitamente separadas.

Artefatos novos desta etapa:

- `fifa_pc/Runtime/CleanRoomAuthoredAnimationTraceSerializer.cs`;
- `fifa_pc/gameplay-full-stack-animation-trace.tsv`;
- `fifa_pc/gameplay-full-stack-animation-replayed.tsv`.

## Atualização desta etapa — trace authored de mira — 2026-10-06

O cenário headless opt-in `--aimed-demo` foi reexecutado após o build Release
e gerou o par determinístico
`fifa_pc/gameplay-clean-room-aimed-trace.tsv` /
`fifa_pc/gameplay-clean-room-aimed-replay.tsv`. Ambos têm 6 eventos; a
sequência contém `SHOT` no frame 67 e `GOAL_HOME` no frame 69. O replay com
`--match-trace-expected` retornou `matched=True`, e o checksum de renderização
foi `801159854A2BC6F8`.
O SHA-256 igual dos dois traces é
`2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`.

O `project-readiness-audit` passou a expor a linha separada
`gameplay-aimed-trace`, com `status=verified-authored`, `passed=True`,
`observed=6` e `expected=6`. O gate geral `gameplay` continua
`clean-room-integrated`/`passed=False` e o readiness continua `NOT_READY`.
Esse trace comprova somente a repetição do controle authored novo; não cria
binding de mira, trajetória ou regra equivalente do jogo original.

O self-test foi ampliado para validar a nova linha do readiness. Fechamento
da etapa: Release `0/0`, self-test `53/53`, aimed trace/replay `matched=True`.
O mesmo cenário aimed foi repetido com `--play-audio`: `6/6` cues preparados e
tocados, `0` sem rota e replay de áudio `matched=True`. O agregador de coverage
agora inclui aimed e verificou `8/8` cenários, `42/42` eventos preparados e
tocados, `0` sem rota, `14/14` ações e `8` cues únicos. O par de traces de
áudio aimed tem SHA-256
`7FA076ADE12B81EC41213F4C34113D35F6F0D1A8FCC14F540C66776BB0CE8F32`; o
relatório de coverage tem SHA-256
`3C0DBC250C19F9259CC37EABF3C7A49A56F8C1CAD6072BDA729C38AF93C369D9`.
O readiness também registra a linha `audio-aimed-trace=verified-authored`:
6 eventos authored, `SHOT->attackgoal`, `GOAL_HOME->goal`, todos tocados e
replay determinístico de áudio. A linha não altera o gate geral de áudio,
que continua parcial e sem seleção original comprovada.
O runtime também exporta o input authored do aimed-demo: 80 amostras, com
`frame=67`, `MoveY=0.75` e `shoot=True`; o replay do par de input traces
retornou `matched=True`. O SHA-256 igual do par é
`A1BCF11B578F5BE9A8DF767E162D7314A6E3398A688D7F2FCA438F536002E44E`.

## Atualização desta etapa — 2026-10-06

Foi implementado o controle estrutural
`animation-morph-relocation-profile-audit`. Ele lê somente o catálogo morph e
o relatório de gaps de relocação dos bancos de animação já materializados fora
da ISO. A execução real validou 594 linhas em 393 layouts morph, 393 layouts
parseáveis e 19 bancos válidos; 53 layouts tiveram contenção em algum banco,
0 tiveram igualdade exata de conjunto e houve 159 pares de contenção. Essa é
uma comparação sintática entre relatórios separados, não evidência
independente de vínculo, e não decodifica frame, pose, rig, timing, skinning,
material, UV, GPU, comando ou animação. O gate permanece
`animation-deformation=structural-only` e o readiness permanece `NOT_READY`.

Artefatos:

- `fifa_pc/Runtime/AnimationMorphRelocationProfileAudit.cs`
- `fifa_pc/animation-morph-relocation-profile-audit.tsv`
- `fifa_pc/animation-morph-relocation-profile-audit-summary.tsv`

O `project-readiness-audit` passou a ler esse resumo estrutural e a registrar
os contadores na nota de `animation-deformation`. A leitura continua
`structural-only`/`NOT_READY`: os contadores documentam a origem e a
recorrência sintática dos relatórios, mas não criam binding morph→animação,
pose, rig, timing ou skinning.

Fechamento verificável desta etapa: Release passou sem avisos/erros; self-test
passou `48/48`; animação `19/19`, morph `391/391`, material candidate `24/24`,
geometria `24/24` sem rejeições e eventos nomeados `17/17` passaram. O preview
candidate authored produziu `F2DD798FDD206BD9`; os headless candidate e
oponente/áudio produziram `29EACF0AE7B12B58` e `651E41284E9242BC`. Os replays
de juggle e rodada retornaram `matched=True`, com checksums
`9F0F1D791CE6070F` e `404D21B5A0571826`. A ISO permaneceu com
`2013003776` bytes, timestamp `2026-01-22T07:03:26.0859951-03:00`, atributo
`Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Avanço posterior: `--candidate-home-character` foi integrado ao preview,
headless e host Win32 como modo opt-in. O mesh/textura candidate same-trunk é
colocado rigidamente no slot home e o away permanece authored; não há rig,
skinning, morph ou animação aplicados ao candidato. Checksums verificados:
preview `AB8D372F87FAA6B5` e headless `DB0F059C0AC82133`.
O cenário combinado de 140 frames com oponente authored, `--play-audio`,
material candidate e candidate-home produziu `2641FCCE8245A44B`, com `3`
cues candidate preparados/tocados e `4` eventos sem rota.
O modo authored também aceita `--candidate-away-character` em preview,
headless e host Win32: preview away `59D25C3C88B9CF69`, ambos os slots
`0F83CEFDD78C58BA` e headless away `72CEA666935DBFCB`. A extensão é rígida e
candidate, sem binding de rig, skinning, morph, animação, UV ou GPU original.
Depois, a política authored do mixer passou a cobrir as ações conhecidas do
runtime com os cues nomeados `intro`, `attackgen`, `panna`, `attackgoal`,
`goal`, `gb_goal`, `tackle` e `outro`. A repetição combinada manteve o checksum
`2641FCCE8245A44B`, com `7` cues preparados/tocados e `0` eventos sem rota;
isso não é binding de evento ou seleção de áudio original.
O cenário authored curto de `0,25` segundo também verificou `FULL_TIME -> outro`
com uma cue preparada/tocada, zero eventos sem rota e checksum
`F39849417B16C774`.
As 14 rotas authored agora têm uma fonte única em
`fifa_pc/Runtime/CleanRoomAudioCuePolicy.cs`, consumida pelo mixer, self-test
e readiness; `UNKNOWN` continua sem rota.

Fechamento verificável mais recente desta etapa: build Release `0` avisos/erros
e self-test `53/53`; smoke material/runtime `24/24` com zero rejeições;
auditoria morph/animação com `594` rows, `393` layouts parseáveis, `19`
bancos, `159` pares de contenção e `0` pares exatos; EVT nomeado `17/17`;
ABK `15/16` bancos decodificados, com 1 rejeição preservada. O preview
candidate-home repetiu `AB8D372F87FAA6B5`; o headless combinado de 140 frames
repetiu `2641FCCE8245A44B`, com `8/8` cues preparados/tocados e zero eventos
sem rota; `FULL_TIME -> outro` foi verificado com `1/1` cue. O replay authored
de juggle retornou `matched=True` e os traces tiveram SHA-256 idêntico
`04920791F3B97933F8BB4C912D2DBB283878A66BC75A9C00092CB3F84400B3A1`.
O readiness continua `NOT_READY`; a ISO manteve tamanho `2013003776`,
timestamp `2026-01-22T07:03:26.0859951-03:00`, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

A cobertura authored do readiness foi ampliada com dois pares de traces:
`gameplay-clean-room-panna-trace.tsv`/replay, com 3 eventos incluindo
`PANNA` e `FULL_TIME`, e `gameplay-clean-room-full-time-trace.tsv`/replay,
com 2 eventos incluindo `FULL_TIME`. Ambos retornaram `matched=True`; são
verificações do runtime novo e não regras ou eventos decodificados do original.

Checksums: panna `CFCF8FC4F0CB4380` e `FULL_TIME` `509B0C35DD390FFF`.
Os SHA-256 dos traces foram iguais aos respectivos replays:
`C14B39694F929825A1707687AC2BE9FFBC7F2DE12045C028B867612B2B86F8BF` e
`C025B66A918312E27AE23CDDF9777951F95F0C14F2DF07CB491FF8F062C72801`.

A nova fixture `--steal-demo --opponent-ai` produziu `KICKOFF=1,STEAL=1`,
checksum `BFA4E813A3D0EA19`, áudio `tackle` `1/1` e replay `matched=True`.
Os dois traces têm SHA-256
`46D917414660878DDA0B319A72018473D4BD44858E6A7116730931C11F3DECA2`.
Essa é uma regra authored para cobertura do runtime, não IA ou posse original.

Continuação verificável da etapa: `eagl-material-runtime-batch-smoke` passou os
24 vínculos candidatos por home, away e ambos os slots authored, com `24/24`
frames em cada configuração, `0` rejeições e `24` checksums distintos por
slot. O relatório e o readiness registram essa cobertura como caminho
candidato, sem inventar binding global de UV/material/GPU, skinning, rigging ou
animação. O headless com ambos os slots produziu `641ED53BD0DAE660`. O cenário
STEAL com slot away, oponente, material e áudio produziu
`301745CFDC42BE25`, `1/1` cue `tackle` tocada e replay `matched=True`.

O renderer clean-room recebeu também um perfil visual authored de tackle para
`STEAL` em `CleanRoomPlayerAnimator`. A alteração é procedural e não decodifica
AXT/morph nem inventa rig, skinning ou animação original. A execução com slot
away, material e áudio produziu checksum `30D789D3BCBB915A`; o trace de dois
eventos permaneceu reprodutível com `matched=True`.
Com o slot home candidato e o jogador away procedural, a variante simétrica
também retornou `matched=True`, `tackle` `1/1` e checksum `BA2CD81182DF970E`.

Correção final da entrega authored de áudio: o cursor inicial de
`GameRuntime`/Win32 foi ajustado para `-1`, de modo que o evento `KICKOFF` de
sequência `0` não seja descartado. O cenário combinado de 140 frames agora
produz `8/8` cues preparados/tocados, `0` eventos sem rota e checksum
`2641FCCE8245A44B`; `gameplay-audio-combined-trace.tsv` tem 8 eventos e o
replay textual retornou `matched=True`. Isso é uma verificação do runtime
clean-room authored, não uma seleção ou binding original EVT/ABKC.
O SHA-256 igual dos traces combinado gerado/replay é
`FEBA7B454A0A82C33A3E2128FBC622A3C6AF957D1FB83CB6AB7DF156EBB6D07C`; o par
`STEAL` gerado/replay coincide em
`B6BEDBFDF812288F820AB359EE98005A9512526B3288540AF84F385E76D6D0D7`.
O relatório `CleanRoomIntegrationReport` agora materializa a verificação
cross-system com os dois slots candidatos: 140 frames, checksum
`088EC5D10096A738`, 8 eventos de gameplay, 8/8 cues preparados/tocados, 0 sem
rota e `original_semantics=not-claimed`. O relatório da repetição coincidiu
byte a byte; essa é integração authored do PC, não binding original. O
relatório também registra `distinct_render_checksums=69` em 140 frames; isso é
somente variação da apresentação authored do runtime, não evidência de pose,
rigging, skinning ou animação original.
O SHA-256 igual dos dois relatórios é
`F0329A2C5189CC326D9099783FC771C16BD8227B48B71DB989D27E9DB3E2DE8F`.
O trace de partida full-stack gerado/replay também coincide em
`B76AA3B7E562564387568CD6872E1A58397130BEFBAD9B68803297EDA068AF3C`.
O `project-readiness-audit` registra isso no gate separado
`full-stack-integration=verified-authored`; os gates semânticos de assets,
rendering e áudio continuam deliberadamente independentes e abertos.
O readiness também registra esses pares como replays authored de áudio
verificados (`8` eventos combinados, incluindo `KICKOFF -> intro`, e `2`
eventos no cenário `STEAL`), sem promover a política a seleção original.

## Estado verificado

Última verificação:

- `dotnet build -c Release fifa_pc\FifaStreetPc.csproj`: sucesso, 0 avisos e
  0 erros.
- `dotnet run -c Release --no-build --project fifa_pc -- self-test`: 53/53.
- O `render-preview` passou a aceitar `--authored-hud` para QA visual offline.
  A execução real com `--clean-room-characters --authored-hud --candidate-material`
  gravou `fifa_pc/candidate-preview-authored-hud.ppm`, validou o candidato
  `tops0030.big/tops0030.ord/tops`, exibiu placar/tempo/medidor authored e
  produziu checksum `F2DD798FDD206BD9`. A inspeção visual confirmou quadra,
  title image, personagens clean-room, mesh candidato e HUD; isso é evidência
  de apresentação do runtime, não prova de UV, material, rigging, skinning,
  animação ou GPU original.
- A bateria final desta etapa foi repetida depois da integração do flag de
  preview: Release `0` avisos/erros, self-test `47/47`, animação `19/19`,
  material runtime `24/24`, geometria `24/24` sem rejeição, morph `391/391`,
  áudio nomeado `17/17` e ABK `15/16` com `1` rejeição catalogada. Os headless
  produziram `C0249FD97A01C3F4` (demo), `3311E2054549E154` (gamebreaker),
  `2C3F702AB025DA33` (replay) e `CFA4BCBEE162C121` (material candidato). O
  readiness continua `NOT_READY`; a ISO mantém tamanho `2013003776`, timestamp
  `2026-01-22T07:03:26.0859951-03:00`, atributo `Archive` e SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- A nova etapa de gameplay authored adicionou `--juggle-demo`, a nona coluna
  `juggle` nos TSVs, altura/cadeia determinísticas da bola e a tecla `J` no
  host Win32. A fixture de 56 frames
  `gameplay-clean-room-juggle-input.tsv` produziu 7 eventos (`KICKOFF`, quatro
  `JUGGLE`, `SHOT`, `GOAL_HOME`); o replay esperado retornou `matched=True` e o
  checksum da apresentação foi `9F0F1D791CE6070F`. O readiness passou a
  registrar `authored_juggle_trace_replay=verified`; tudo isso é comportamento
  novo do runtime clean-room, sem afirmar input, física, frame, rig, skinning,
  animação, áudio ou GPU original.
- A validação final desta etapa repetiu os smokes relevantes de animação,
  SHPX/geometria, material, geometria, atributos, correspondência de estado e
  áudio com exit 0; o replay esperado reportou `matched=True`, o headless
  clean-room terminou com `C0249FD97A01C3F4`, IA+áudio com
  `651E41284E9242BC` e `FULL_TIME` com `082272ACA18EFFE9`. A auditoria de
  prontidão permanece `NOT_READY`. A ISO manteve tamanho `2013003776`,
  timestamp `2026-01-22 07:03:26` e SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`;
  `IsReadOnly=False` é o atributo de arquivo preexistente, enquanto todos os
  caminhos do programa continuam abrindo a ISO somente para leitura.
- O novo smoke authored `first-to-goals` foi repetido após o build: com alvo 1,
  terminou em `FIRST_TO_GOALS`, `HOME 1 - 0 AWAY`, `FULL_TIME` no frame 70 e
  `match_result=HOME_WIN`; os argumentos inválidos (regra desconhecida, alvo
  ausente ou alvo em modo `TIMED`) foram recusados com exit 2. A documentação
  de referência externa limitada está em
  `D:\projeto zeta\docs\FIFA_EXTERNAL_BEHAVIOR_EVIDENCE.md`.
- A validação final desta etapa foi repetida depois da regeneração dos relatórios:
  Release e self-test `45/45` passaram; os smokes de animação, SHPX/geometria,
  material, geometria, atributos, correspondência de estado e áudio retornaram
  exit 0. O cenário `goals-or-panna` produziu `PANNA` e `FULL_TIME` no frame 31,
  entregou 2 eventos ao consumidor de áudio e preparou 1 candidato; o cenário
  AI+áudio reportou checksum `651E41284E9242BC`, 7 eventos, 3 preparados e 3
  reproduzidos. O relatório `project-readiness.tsv` foi regenerado e permanece
  `NOT_READY`. A ISO manteve tamanho `2013003776`, timestamp
  `2026-01-22T07:03:26.0859951-03:00`, atributo `Archive` e SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`;
  `IsReadOnly=False` continua sendo o atributo preexistente, sem escrita pela
  aplicação.
- O smoke de renderização candidata agora aceita `--report` e materializa
  `fifa_pc/eagl-material-runtime-batch.tsv` com uma linha por amostra. A
  execução real confirmou `24/24` frames, `0` rejeições, `24` checksums
  distintos e `38` grupos texturizados; `project-readiness-audit` passou a
  registrar esses contadores em `assets` sem alterar `rendering=not-proven`.
  O relatório também recusa explicitamente usar a própria ISO como destino
  (`exit 2`), preservando a imagem de trabalho sem escrita.
- O controle negativo `eagl-state-geometry-adjacency-audit` foi executado
  contra os 24 objetos candidatos: `24/24` linhas válidas, `0` pares no mesmo
  índice de primitiva, `48` pares em índices adjacentes, `269` pares na mesma
  camada, `24/24` estados de objeto exatos e `0/24` grupos geométricos com
  `SHAPENAME` exato. Esses números registram uma coincidência estrutural
  controlada e não autorizam binding de estado para mesh, material, UV,
  skinning, rigging, animação ou GPU; `project-readiness-audit` os inclui sem
  alterar `rendering=not-proven`.
- O controle complementar `eagl-state-geometry-profile-audit` comparou apenas
  perfis opacos de sequência de IDs/tamanhos, contagem e bytes dos comandos.
  Foram geradas `269` linhas em `24` objetos: `0` sequências exatas, `0` pares
  no mesmo índice, `48` vizinhos de índice, `1` perfil de comando geométrico
  distinto e `3` perfis de estado; `4` linhas têm perfil de estado não
  repetido, mas sem igualdade de sequência. Esse resultado é evidência
  estrutural adicional, não associação nem semântica de material, UV, skinning,
  rigging, animação ou GPU; `rendering` permanece `not-proven`.
- O novo controle `eagl-state-geometry-order-audit` registra intervalos da
  ordem observada entre os grupos geométricos candidatos e as primitivas com
  `SHAPENAME`, sem convertê-los em associação. A execução real produziu 38
  grupos, 180 primitivas de estado, 62 intervalos, 36 intervalos não vazios,
  180 estados particionados sem sobra, streams completos nos 36 runs não
  vazios e 17 perfis de sequência opacos. O artefato é
  `fifa_pc/eagl-state-geometry-order-audit.tsv`; `rendering` continua
  `not-proven`.
- Após esse avanço, o Release compilou com 0 avisos/erros, o self-test passou
  com `47/47` e o novo smoke de ordem validou `24/24` objetos sem rejeições.
  Esse controle acrescenta evidência de ordem e completude, não binding de
  material, UV, skinning, rigging, animação ou GPU.
- A etapa authored seguinte adicionou ao `StreetMatch` um medidor/ação
  `GAMEBREAKER` opt-in, com a coluna TSV opcional `gamebreaker` e o demo
  separado `--gamebreaker-demo`. Quatro `TRICK` authored carregam o medidor até
  100; o disparo zera o valor e alcança `GOAL_HOME`. A fixture de 40 frames
  `gameplay-clean-room-gamebreaker-input.tsv` produziu 7 eventos; o trace e o
  replay coincidiram byte a byte com SHA-256
  `BC17D33716D221F1495434E12D4C3836957FB4BB83D0E9AA73AAC8CC7B43A4EB`, e a
  fixture tem SHA-256
  `81F9DFDC8AB37681F5309C92D1BD8704333258E08876A621CE92172E18A67D6C`.
  Isso é integração authored clean-room, não evidência de controle, medidor,
  animação, áudio ou GPU do conteúdo original; o mixer deixa `GAMEBREAKER`
  explicitamente sem rota.
- O host Win32 agora encaminha a tecla `G` (janela em foco) para o campo
  authored `MatchInput.Gamebreaker`; a ajuda de `run-windowed` documenta o
  controle. O build verifica a integração do caminho interativo, sem atribuir a
tecla ao input original e sem criar qualquer binding de renderização,
  animação, áudio ou GPU.
- O caminho Win32 agora passa uma flag explícita ao `PreviewRenderer` para
  desenhar um HUD authored no framebuffer: placar, tempo e barra do medidor. O
  self-test compara o framebuffer com e sem esse HUD; o headless não habilita a
  flag, preservando os checksums existentes e a separação em relação aos assets
  candidatos. Os textos/glyphs são UI nova do PC e não evidência de fonte,
  layout, textura, material, UV, animação ou GPU da ISO.
- `render-preview` agora aceita `--authored-hud` para QA visual offline do mesmo
  caminho e grava a apresentação em PPM. A opção é explícita, não altera os
  checksums headless e não transforma a UI authored em evidência original.
- `CleanRoomPlayerAnimator` agora possui um perfil procedural authored para
  `GAMEBREAKER` (celebração com alcance/elevação/lean determinísticos). O
  self-test compara esse perfil com idle, trick e shot; isso não é binding de
  nome, frame, pose, rig, skinning ou timing dos payloads observados.
- O self-test também injeta um estado de teclado sintético e exige que somente
  `G` produza `Gamebreaker=true`; isso valida a ponte de controle sem abrir uma
  janela ou depender de estado global do Windows.
- A ponte de teclado authored também mapeia `N` para `Panna`; o self-test exige
  `Panna=true` quando somente `N` está pressionada. A ajuda do host documenta
  `N` e `G`; nenhuma das teclas é apresentada como input original.
- A validação terminal desta etapa repetiu Release (`0` avisos, `0` erros) e
  self-test (`47/47`). Os smokes relevantes retornaram exit 0: animation
  `19/19`, material runtime `24/24`, geometria/correspondência/perfil/ordem,
  morph `391/391`, áudio nomeado `17/17` e ABK `15/16` com `1` rejeição já
  catalogada. O headless candidato manteve `29EACF0AE7B12B58`; IA+áudio manteve
  `651E41284E9242BC`; o novo demo gamebreaker produziu
  `GAMEBREAKER=1`, `GOAL_HOME=1`, 7 eventos no trace scriptado e
  `matched=True` no replay. O readiness permanece `NOT_READY`. A ISO mantém
  tamanho `2013003776`, timestamp `2026-01-22T07:03:26.0859951-03:00`, atributo
  `Archive`, `IsReadOnly=False` preexistente e SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- O readiness passou a validar também os traces do gamebreaker authored:
  `authored_gamebreaker_trace_replay=verified`, `events=7`, ação
  `GAMEBREAKER` e `GOAL_HOME`. Isso é uma verificação adicional do runtime
  managed e mantém o gate de gameplay falso e a leitura geral `NOT_READY`.
- Após o perfil de celebração, o demo gamebreaker de 80 frames com personagens
  authored produziu `3311E2054549E154`; a fixture/replay scriptada de 40 frames
  produziu `2C3F702AB025DA33` e `matched=True`. São checksums da apresentação
  authored nova, não pose ou animação decodificada.
- A validação final desta etapa repetiu o Release (`0` avisos, `0` erros), o
  self-test (`46/46`), os smokes de catálogo, material runtime, geometria,
  correspondência, perfil, áudio e morph, além de dois runtimes headless. O
  headless com material candidato produziu `29EACF0AE7B12B58`; o cenário
  AI+áudio produziu `651E41284E9242BC` com `3` candidatos reproduzidos e `4`
  eventos sem rota. A auditoria de prontidão segue `NOT_READY`; a ISO mantém
  tamanho `2013003776`, timestamp `2026-01-22T07:03:26.0859951-03:00`, atributo
  `Archive` e SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- O `project-readiness-audit` passou a registrar no gate de gameplay
  `authored_round_trace_replay=verified` e `authored_round_trace_events=7`
  quando os traces scriptados estão presentes e têm o mesmo hash. Isso melhora
  a evidência do runtime authored, mas não transforma a simulação em regras
  originais nem altera `readiness=NOT_READY`.
- O gate foi endurecido para exigir também exatamente 7 eventos e a presença de
  pelo menos um gol e uma ação `RESTART`; a execução atual reportou
  `authored_round_trace_has_goal=True` e
  `authored_round_trace_has_restart=True`. A validação final repetiu Release
  (`0` avisos, `0` erros), self-test `46/46`, material `24/24`, geometria sem
  rejeições, perfil `269` linhas, áudio, morph, headless e readiness, todos
  com exit `0`. A ISO permaneceu com SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- O self-test agora cobre esse gate com dois traces TSV sintéticos idênticos,
  incluindo `GOAL_HOME` e `RESTART`, e verifica as notas
  `authored_round_trace_replay=verified`, `events=7`, `has_goal=True` e
  `has_restart=True`. Isso protege a validação do artefato sem atribuir
  semântica original aos eventos.
- O cenário authored `--round-demo` também foi exportado e reexecutado com
  `--match-trace-expected`: 7 eventos, `match_round=2`,
  `match_last_action=RESTART`, checksum `404D21B5A0571826` e `matched=True`.
  `gameplay-round-demo-trace.tsv` e `gameplay-round-demo-replayed.tsv` têm o
  mesmo SHA-256 `B3BDFEEC8FD2EB6479E7B797C1031AE9EE348451C4BFE4C4E172B993E8872EBB`.
  Isso é determinismo do runtime authored, não semântica original de kickoff.
- O `run-headless` agora aceita combinar `--input-script` com `--round-demo`:
  o TSV fornece todos os controles e a opção habilita somente o reset authored.
  A fixture de 140 frames `gameplay-clean-room-round-input.tsv` passou com os
  mesmos 7 eventos, rodada 2, `RESTART`, checksum `404D21B5A0571826` e
  `matched=True`; `--demo` e `--aimed-demo` continuam incompatíveis com input
  explícito. Os traces scriptados têm SHA-256
  `B3BDFEEC8FD2EB6479E7B797C1031AE9EE348451C4BFE4C4E172B993E8872EBB`.
- Após essa alteração, a validação repetiu os smokes de material runtime,
  geometria, correspondência, perfil, áudio e morph, além do headless candidato
  e do `project-readiness-audit`: todos retornaram exit `0`; a combinação
  proibida `--input-script --demo` retornou exit `2`. O Release continua com
  `0` avisos/erros e o self-test com `46/46`. A ISO manteve tamanho
  `2013003776`, timestamp `2026-01-22T07:03:26.0859951-03:00`, atributo
  `Archive` e SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Nesta etapa, a pose authored foi ampliada para perfis determinísticos de
  posse, passe, drible, chute, gol, `RESTART` e `FULL_TIME`; o smoke de 80
  frames produziu `C0249FD97A01C3F4`, e com material candidato
  `29EACF0AE7B12B58`. O cenário curto de encerramento produziu
  `match_ended=True`, `match_frame=16`, `FULL_TIME=1` e checksum
  `082272ACA18EFFE9`; o áudio desse evento continua explicitamente sem binding.
- A bateria final repetiu os smokes: 19 catálogos de animação, 24 frames de
  material candidato sem rejeições, 269 pares de correspondência sem igualdade
  exata nem símbolo de relocation compartilhado, 24 geometrias sem rejeição, 17/17 candidatos EVT decodificados,
  15/16 bancos ABK decodificados e 144/144 layouts mono únicos. A auditoria
  segue `NOT_READY`; o SHA-256 da ISO segue
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- A auditoria `project-readiness-audit` agora gera
  `fifa_pc/project-readiness.tsv` com os gates atuais: integridade da ISO
  verificada pelo tamanho/SHA-256; assets `candidate-integrated`; renderização
  `not-proven`; gameplay `clean-room-integrated`; áudio
  `partially-integrated`; e animação/deformação `structural-only`. O relatório
  é um gate de evidência, não uma conclusão de prontidão: lista a evidência
  necessária para cada lacuna e preserva a distinção entre candidato,
  estrutura e semântica. Nenhum binding de UV, material, skinning, rigging,
  timing ou GPU foi criado.
- A etapa estrutural seguinte adicionou `AnimationBankDataRelocationAudit` e o
  comando `animation-bank-data-relocation-audit`. O relatório real validou
  19/19 ELF, 35.900 relocations direcionadas a `.data`, 19/19 bancos com
  relocations, 19 perfis de offsets distintos e 15 contagens distintas de gaps;
  o máximo observado foi 29.152 offsets distintos e 291 gaps distintos no mesmo
  banco. O TSV registra apenas offsets relativos, gaps, histogramas e hash da
  sequência offset/tipo; não atribui semântica a payload, frame, pose, rig,
  timing, skinning, material, UV, GPU ou comando.
- O controle `eagl-morph-animation-name-negative-control` cruzou, sem
  normalização, os 2.360 nomes completos de shape dos 594 morphs com os 1.578
  nomes de arquivo e 19 nomes de banco dos catálogos de animação. Houve 0
  coincidências exatas em ambos os conjuntos. Isso é controle lexical negativo,
  não prova de ausência semântica e não cria binding de animação, pose, rig ou
  timing.
- A camada gameplay agora preserva um trace authored imutável em
  `StreetMatch.EventHistory`, com sequência, frame, placar e posse por evento;
  `run-headless` exporta esse trace por `--match-trace-output <saida.tsv>`.
  A execução real de 80 frames gerou 6 eventos (`KICKOFF`, `TRICK`, `PASS`,
  `PASS_COMPLETE`, `SHOT`, `GOAL_HOME`) em
  `fifa_pc/runtime-gameplay-trace.tsv`, SHA-256
  `2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`.
  O self-test verifica seed, contiguidade e correspondência da última ação;
  isso é telemetria da simulação clean-room e não cria eventos, cues ou regras
  atribuídos ao conteúdo original.
- `run-headless` agora aceita `--input-script <entrada.tsv>` para consumir um
  TSV authored com `frame`, `move_x`, `move_y`, `pass`, `shoot` e `trick`. O
  leitor também aceita a sétima coluna opcional `panna`, além de validar sequência começando em zero, eixos finitos em `[-1,1]` e
  booleanos `0/1/false/true`; `--demo` e `--aimed-demo` permanecem
  incompatíveis, enquanto `--round-demo` pode ser usado apenas para habilitar
  o reset authored. A
  fixture `fifa_pc/gameplay-clean-room-input.tsv` passou por 8 frames e gerou
  `fifa_pc/gameplay-script-trace.tsv` com somente `KICKOFF`, pois termina antes
  da posse; isso é smoke do caminho novo, não semântica de input original. Os
  SHA-256 foram `B2F2969BFF1FA22387158B197EEF09479D8BDD682D26B6AB139B11F0A2AE22E8`
  para a fixture e `6288D477F63B66EB445F24E6C55D0F3BD0635B0D0BF7F5480CAD10754AD16D52`
  para o trace.
- A fixture authored de 80 frames `fifa_pc/gameplay-clean-room-demo-input.tsv`
  reproduziu o cenário `--demo` via `--input-script`, com checksum
  `C0249FD97A01C3F4`, 6 eventos e `GOAL_HOME`; o trace resultante preservou
  SHA-256 `2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`.
  Essa igualdade é da simulação clean-room e não atribui input ou regra ao
  jogo original.
- A fixture authored de 140 frames
  `fifa_pc/gameplay-clean-room-round-input.tsv` combina input TSV explícito com
  `--round-demo`: produziu 7 eventos, `match_round=2`,
  `match_last_action=RESTART`, checksum `404D21B5A0571826` e `matched=True`.
  `fifa_pc/gameplay-script-round-trace.tsv` e
  `fifa_pc/gameplay-script-round-replayed.tsv` tiveram SHA-256
  `B3BDFEEC8FD2EB6479E7B797C1031AE9EE348451C4BFE4C4E172B993E8872EBB`.
  Esse é um gate authored de input/política/replay, não uma captura de input
  original.
- Esta etapa adicionou `MatchTraceSerializer` e a opção
  `--match-trace-expected <entrada.tsv>` ao `run-headless`. O esperado é
  validado antes da abertura da ISO e comparado textualmente ao trace da
  reexecução; divergência informa a primeira linha, e a ISO ou o mesmo caminho
  de saída são recusados. A fixture de 80 frames passou com `matched=True`, 6
  eventos e checksum `C0249FD97A01C3F4`; o artefato
  `fifa_pc/gameplay-script-demo-replayed-stage.tsv` coincidiu byte a byte com
  `fifa_pc/gameplay-script-demo-trace.tsv`, ambos com SHA-256
  `2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`. Isso é
  um gate de determinismo/integridade authored, não replay, input, evento ou
  semântica decodificada do jogo original.
- A camada gameplay agora possui a regra authored opt-in
  `--match-rule first-to-goals --target-goals <N>`. Com a fixture de 80 frames
  e alvo 1, terminou em `HOME 1 - 0 AWAY`, emitiu `FULL_TIME` no frame 70 e
  manteve relógio restante; o modo padrão `TIMED` preservou
  `C0249FD97A01C3F4`. A referência comportamental externa limitada está em
  `docs/FIFA_EXTERNAL_BEHAVIOR_EVIDENCE.md`; esse modo não é regra, limite ou
  timing decodificado da ISO.
- O fixture authored `fifa_pc/gameplay-clean-room-panna-input.tsv` cobre a
  sétima coluna opcional `panna` e `--match-rule goals-or-panna`. Em 31 frames
  produziu `PANNA`, `FULL_TIME` no frame 31, preparou uma row `panna` e deixou
  explícitos `gameplay_audio_events=2`, `gameplay_audio_prepared=1` e um evento
  `FULL_TIME` sem rota. O `GameRuntime` e o host Win32 agora drenam todos os
  eventos novos de `EventHistory`, inclusive múltiplos eventos no mesmo tick;
  isso corrige a entrega authored sem atribuir cue ou binding à ISO.
- A nova `eagl-morph-shpx-geometry-correlation-audit` cruzou os relatórios
  SHPX e geometria candidata por layout: 393/393 unidos, 17 com ambas as
  camadas variáveis, 21 somente com geometria variável e 355 estáveis; 17 dos
  38 layouts ambíguos têm co-variação. Um layout permanece sem bounds. Isso é
  triagem estrutural entre fontes separadas, não pareamento SHPX→mesh nem
  binding de morph, UV, material, pose, rig, skinning, animação, timing ou GPU.
  Os SHA-256 dos relatórios são
  `BB9EE9ED18DEB9007685B02C8C8EDE48E8B9E9EE9A6722179F19449A43507B7B` e
  `9D28EDB9056D7FC32982587071F3E4E7435FBD2C5630122AE71925655390CF5E`.
- A integração clean-room de áudio agora tem quatro rotas authored explícitas:
  `TRICK -> panna`, `PANNA -> panna`, `GOAL_HOME -> goal` e
  `GOAL_AWAY -> goal`. `panna` é uma row nomeada observada e decodificada;
  a escolha para `TRICK` é do runtime novo e não afirma vínculo original. No
  demo padrão, 2/5 eventos têm candidato preparado e 3 ficam sem rota; com
  AI, 3/6 são preparados/tocados e 3 ficam sem rota. O self-test cobre tanto
  a rota preparada quanto `candidate-unavailable` e
  `unmapped-clean-room-event`.
- A revalidação desta etapa também passou nos smokes de animação (19/19),
  material runtime (24/24), geometria (24/24), atributos (24/24), correspondência
  estado/topologia (269/269) e morph (594/594). Os smokes de áudio mantiveram
  17/17 candidatos nomeados decodificados e 15/16 bancos ABK decodificados, com
  1 banco sem candidato delimitado aceito. A auditoria separada de layout
  confirmou 144/144 ranges sem canais observados como mono único, 0/144 como
  estéreo e 0 casos ambíguos.
- O `CatalogAudioMixer` agora pré-prepara no runtime os 17 candidatos EVT
  nomeados, totalizando 2.914.952 bytes PCM, antes de qualquer evento gameplay.
  Os 10 casos sem tag de canais continuam registrados como `channels=0` no
  header e usam somente a hipótese mono de payload validada pelos frames; isso
  é cache de payload candidato, não binding de evento.
- O inventário morph correlacionado passou com 0 issues, 594 linhas em 393
  layouts, 38 layouts variáveis no par payload/geometria, 27 com payload estável
  e geometria variável e 11 com ambas as camadas variáveis. Essa correlação é
  somente triagem estrutural.
- A auditoria independente `morph-candidate-geometry-ambiguity-audit` cruzou o
  catálogo com a matriz de ambiguidade: 38/38 layouts com geometria variável
  também são ambíguos, 0 são estáveis, 0 layouts ambíguos ficaram sem variação
  geométrica e houve 0 divergências de rótulo; 1 layout permanece sem bounds.
- A execução `run-headless ... 80 --demo` agora reporta
  `match_frame=80`, `match_action_sequence=5`,
  `match_last_action=GOAL_HOME`, `match_possession=None`,
  `match_tricks=1` e o histograma
  `GOAL_HOME=1,KICKOFF=1,PASS=1,PASS_COMPLETE=1,SHOT=1,TRICK=1`.
  Os modos candidate-material e play-audio preservaram a mesma sequência;
  isso verifica a simulação nova, não eventos originais.
- O gameplay ganhou um modo de oponente clean-room opt-in: `--opponent-ai`
  pressiona após a posse observada, pode recuperar a bola e finaliza no gol
  esquerdo. Em 140 frames com `--demo --opponent-ai`, o runtime produziu
  `HOME 0 - 2 AWAY`, `SHOT_AWAY=2` e `GOAL_AWAY=2`; o modo padrão preservou o
  checksum `5405DE72BE1B4FC1`. Isso é comportamento novo verificável, não uma
  reconstrução de regras ou eventos do jogo original.
- Fora de passe/chute em voo, `StreetMatch` agora sincroniza `BallPosition` com
  o jogador que possui a bola. O self-test cobre essa invariável e também o
  caminho de disputa/roubo da IA; o ajuste não alterou o checksum padrão.
- O chute Home usa `MoveY` no instante do disparo para acrescentar uma componente
  vertical determinística à trajetória. É comportamento de controle novo,
  coberto por self-test, sem alegação de semântica original; o demo padrão usa
  `MoveY=0` e mantém a trajetória reta/checksum de referência.
- O modo opt-in `--aimed-demo` exercita esse controle no runtime headless:
  emite `MoveY=0.75` no disparo, mantém o caminho de gol e produziu o checksum
  `6251CBFCB6A56869`. O `--demo` padrão permanece em `5405DE72BE1B4FC1`.
  Ambos são cenários clean-room; nenhum checksum atribui semântica original.
- O modo opt-in `--round-demo` acrescenta um ciclo pós-gol clean-room: 0,5 s de
  intervalo, reposicionamento, posse determinística e ação `RESTART`, com
  `match_round`, `match_restart_pending` e `clean_room_round_reset` na
  telemetria. É comportamento novo coberto por self-test; não é uma regra de
  kickoff decodificada e não altera o caminho padrão.
- `run-headless` aceita `--match-seconds <segundos>` para testar o encerramento
  clean-room. Em 30 frames com `--demo --match-seconds 0.25
  --clean-room-characters`, produziu `match_ended=True`,
  `match_last_action=FULL_TIME`, `match_result=DRAW` e checksum
  `082272ACA18EFFE9`; depois do fim, o frame permanece congelado. Essa
  transição é authored, não uma regra de duração original, e seu evento de
  áudio segue sem binding.
- `CleanRoomPlayerAnimator` agora aplica perfis authored determinísticos para
  posse, passe, drible, chute, gol, `RESTART` e `FULL_TIME`, movimentando braços,
  tronco, cabeça e pernas no renderer. O smoke de 80 frames produziu
  `C0249FD97A01C3F4`; com material candidato, `29EACF0AE7B12B58`. É integração
  nova de apresentação/gameplay, não decoder de AXT/morph, rigging ou skinning.
- `run-windowed` agora aceita `--match-seconds <segundos>` e passa a duração
  authored validada ao mesmo `StreetMatch`; assim `FULL_TIME`/`match_result`
  têm a mesma política do headless. O argumento inválido foi testado e rejeitado
  sem alterar a ISO.
- `run-windowed` também aceita `--round-reset`, que habilita a mesma política
  authored de intervalo, reposicionamento e `RESTART` já exercitada no headless.
  O host Win32 mantém oponentes e áudio como caminhos clean-room explícitos;
  essa opção não interpreta kickoff, timing ou eventos originais.
- O renderer software e o host Win32 aceitam `--clean-room-characters`, que
  desenha uma pose procedural authored dos atores a partir do estado da partida.
  O caminho é opt-in, determinístico e separado dos candidatos EAGL e dos
  bancos de animação; não interpreta AXT/morph nem cria binding de rigging ou
  skinning.
- `eagl-material-batch-smoke`: 24/24 vínculos same-trunk verificados, todos na
  família `tops`; `eagl-morph-sibling-inventory`: 594/594 irmãos completos,
  594/594 ELF/geometria, 594 hashes de payload normalizado distintos e 393
  assinaturas estruturais, com maior perfil repetido em 42 objetos; as janelas
  locais normalizadas produziram 326 assinaturas, 404 pares com layout e 11
  layouts morph com múltiplas assinaturas locais; a
  correlação companion produziu 417 assinaturas e 443 pares, com 32 perfis
  morph associados a múltiplas assinaturas companion; o hash refinado de layout
  ELF produziu 444 assinaturas, 447 pares e 32 layouts morph com múltiplos
  layouts companion; o hash opaco dos streams de comandos produziu 592
  assinaturas, 593 pares e 38 layouts morph com múltiplos streams; os
  descritores `__VB`
  produziram 235 assinaturas, 407 pares e 13 perfis morph com múltiplos
  descritores; a sequência relativa dos registros `__VB` produziu 358
  assinaturas, 431 pares e 27 perfis morph com múltiplas sequências; a leitura
  numérica produziu 12 assinaturas, 398 pares e 5
  perfis morph com múltiplas assinaturas numéricas; a topologia candidata
  produziu 593 assinaturas, 399 pares e 6 perfis morph com múltiplas
  topologias; a comparação cross-file de words alinhadas produziu 406
  assinaturas e 12 layouts morph com múltiplas formas de coincidência.
- `eagl-material-batch-preview`: 24/24 prévias candidatas geradas, 0 rejeições,
  24 checksums distintos e manifesto TSV; todos os recursos são `PAL8`/`tops`,
  com 14 meshes de dois grupos e 10 de um grupo, totalizando 7.403 vértices,
  7.327 triângulos e 38 grupos texturizados. Isso verifica a cadeia software
  por objeto, não UV, material GPU, skinning ou geometria original.
- A inspeção visual das prévias `tops0030` e `tops0040` confirmou rasterização
  de textura e triângulos, mas também mostrou fragmentação/alongamento fortes.
  As versões PNG derivadas para inspeção são
  `fifa_pc/tops0030-material-preview.png` e
  `fifa_pc/tops0040-material-preview.png`; elas não substituem os PPMs nem
  constituem evidência de topologia original.
- Na revalidação final, o headless padrão terminou em 80 frames com checksum
  `5405DE72BE1B4FC1`; o modo AI+áudio terminou em 140 frames com `HOME 0 - 2
  AWAY`, checksum `651E41284E9242BC`, 2 eventos tocados e 0 rejeições; a amostra
  `tops0040.big tops` terminou em 80 frames com checksum `3A2D00B09DC319F6`.
- O comando `eagl-candidate-projection-preview` gerou vistas neutras `xy`,
  `xz` e `yz` para `tops0030` e `tops0040`. As seis vistas alteraram as
  silhuetas, mas não eliminaram a fragmentação; nenhuma projeção foi escolhida
  como eixo, câmera ou transformação do runtime. Os PPMs ficam em
  `fifa_pc/candidate-projection-previews-stage-1` e permanecem QA geométrico.
- `run-headless ... 80 --demo`: checksum de frame
  `5405DE72BE1B4FC1`, placar HOME 1 - 0 AWAY.
- `run-headless ... 80 --demo --candidate-material`: cadeia same-trunk
  `tops0030.big`/`tops0030.ord`/`tops0030.xsh` validada, 80 frames e checksum
  de frame `CFA4BCBEE162C121`; o modo padrão não mudou.
- `run-headless ... 80 --demo --candidate-material tops0040.big tops`:
  segunda amostra independente da família `tops`, com textura PAL8 256x256,
  malha de 527 vértices/523 triângulos e checksum `3A2D00B09DC319F6`.
- Fallback candidato verificado com `--candidate-material tops0030.big head`:
  a seleção foi rejeitada explicitamente, o runtime voltou ao mesh sólido
  candidato e terminou sem erro.
- `run-headless ... 80 --demo --play-audio`: prepara e toca as rows authored
  `panna` e `goal` para `TRICK`/`GOAL_HOME`, com 2 reproduções e sem rejeição
  de decode. A regra clean-room simétrica `GOAL_AWAY -> goal` reutiliza a
  mesma row, sem afirmar vínculo original.
- O mixer agora expõe a rota de cada evento: `unmapped-clean-room-event`,
  `candidate-unavailable`, `candidate-decode-rejected`,
  `prepared-clean-room-candidate` ou `played-clean-room-candidate`. Na ISO,
  o demo padrão teve 5 eventos, 3 sem rota e 2 preparados; o demo com AI teve
  6 eventos, 3 sem rota e 3 preparados/tocados. Isso torna explícito o que ainda não
  tem seleção automática, sem preencher cues originais.
- `apt-structure-inventory`: `fe.bh` tem 163/163 APT válidos, 161/161 objetos
  `.o` EAGL estruturais válidos, 145 conjuntos completos, 145/145 tabelas SHPX
  válidas (0 rejeitadas) e 133 assinaturas estruturais de APT+companions; `ig.bh`
  tem 121/121 APT válidos, 118/118 objetos `.o` válidos, 114 conjuntos
  completos, 114/114 tabelas SHPX válidas (0 rejeitadas) e 97 assinaturas. A
  assinatura inclui somente
  header APT, header `.const` validado quando presente, tabela SHPX, extensões
  same-stem e tamanhos dos arquivos.
- `--profile-report` correlacionou os headers APT/`.const` por padrão de header e
  companions: 80 perfis em `fe.bh` e 51 em `ig.bh`, cobrindo 284 pacotes e 568
  words alinhados comparados; 0 coincidiram e 568 diferiram. Isso é comparação
  bruta de words, não semântica de timing, pose, frame ou animação.
- `animation-bank-inventory` validou `data/anims.big`: 20 entradas `.axt`,
  incluindo o índice com 5 nomes, e 19 catálogos com 1.584 entradas de arquivo.
  Todos os 19 catálogos têm par `.ord/.orl` válido e ELF MIPS combinado; os 19
  expõem exatamente um símbolo `__AnimationBank:::` que coincide com o nome do
  banco. Os 19 hashes de layout ELF são distintos nesta comparação; todos
  possuem seção `.data` e o símbolo fica a 28 bytes do fim dessa seção. A
  normalização conservadora do payload `.data`, mascarando somente words de
  relocation, também produziu 19 hashes distintos em 19 bancos; não há template
  byte-a-byte compartilhado sob essa regra. Isso prova identidade de
  catálogo/banco e regularidades estruturais, não frames, poses, rigging ou
  timing.
- `GameAssetCatalog.Scan` agora incorpora essa mesma validação estrutural ao
  runtime: as execuções headless reais reportam 19/19 contagens de catálogo,
  pares `.ord/.orl`, ELF válidos, símbolos coincidentes e 19 payload hashes
  normalizados distintos. Isso integra a presença dos assets ao catálogo do
  runtime, mas ainda não seleciona ou interpreta animações.
- `animation-bank-runtime-smoke` agora verifica esse caminho do catálogo sem
  usar o leitor direto: encontrou 19 bancos, 1.584 entradas AXT, 1.578 nomes
  distintos, 1 tag de versão, 19 contagens declaradas válidas, 19 pares
  `.ord/.orl`, 19 ELF válidos e 19 símbolos coincidentes. As linhas preservam
  nomes e timestamps observados, mas continuam sem semântica de frame, pose,
  rig, timing ou skinning.
- O catálogo de runtime também retém os registros ELF crus dos 19 símbolos
  `__AnimationBank:::`. O smoke observou 19 registros e 19 assinaturas de
  metadados distintas; todas têm `symbol_section=1`, `size=4` e `Info=17`, com
  variação observada em tabela/índice/valor. O índice morph compara essa tupla
  crua com o registro `__PosePaletteBank:::` (`4|3|1|0|4|17`) e encontrou 0/391
  coincidências. É um controle estrutural negativo, não prova de ausência de
  vínculo semântico e não autoriza decoder de pose, rig ou animação.
- `animation-bank-name-index` agora grava uma linha TSV por entrada textual,
  preservando banco, catálogo, versão, diretório, timestamp, estado do ELF/
  símbolo e recorrência do nome entre bancos. O relatório real tem 1.584 linhas,
  1.578 nomes distintos e 6 nomes em mais de um banco; isso é seleção
  diagnóstica, não um decoder de frames, poses, rig ou timing.
- `animation-bank-catalog-compare` acrescenta uma comparação independente
  somente dos catálogos `.axt`: os 19 bancos têm 1.584 entradas e 1.578 nomes
  distintos; os 6 nomes compartilhados entre bancos também compartilham o
  timestamp observado, mas não houve conjunto de nomes idêntico, nome na mesma
  posição ou prefixo ordenado comum entre qualquer par. Os relatórios são
  `animation-bank-catalog-profiles.tsv` e `animation-bank-catalog-pairs.tsv`;
  isso organiza sobreposição textual/temporal observada e não atribui semântica
  de frame, pose, rig, timing ou skinning.
- `animation-bank-elf-structure` materializa headers de seção e relocations dos
  19 ELF: 114 linhas de seção, 6 seções por banco, 19 hashes de perfil de seção
  distintos e 19 hashes de perfil de relocation distintos. Os 19 bancos têm
  metadata `__AnimationBank:::` observada e distância de 28 bytes ao fim de
  `.data`; os nomes/tamanhos de seção, flags, offsets e tipos de relocation são
  preservados sem interpretar `.data` como records de frame/pose ou timing. Os
  relatórios são `animation-bank-elf-profiles.tsv` e
  `animation-bank-elf-sections.tsv`.
- `animation-bank-tail-compare` compara os 32 bytes iniciados no offset do
  símbolo `__AnimationBank:::`. Não houve pares crus idênticos, mas houve 12
  pares normalizados idênticos e 12 hashes normalizados distintos; 30/32
  offsets são comuns em todos os bancos. A palavra little-endian observada nos
  offsets 4–5 equivale a `2 * catalog_file_count` e a
  `2 * declared_file_count` em 19/19 casos; o contador declarado do cabeçalho
  também confere com as entradas parseadas em 19/19. Isso é uma igualdade
  numérica reproduzível entre fontes de contagem independentes, não a
  atribuição de semântica a um campo ou decoder de frames/poses/timing. O
  relatório é `animation-bank-tail-comparison.tsv`.
- `animation-bank-sidecar-boundary-audit` acrescentou uma auditoria independente
  da divisão física `.ord`/`.orl`: os 19 pares foram observados, todos os 19
  colocam as 5 seções file-backed não vazias em uma divisão de 1 seção `.ord`
  e 4 seções `.orl`, todos os 19 deixam a região ELF de relocation em `.orl`,
  e todos os 19 deixam a tabela de seções em `.orl`, sem seção cruzando a
  fronteira. Isso é somente posição/range de sidecar e contexto limitado de
  bytes; não é semântica de payload, relocations, frames, poses, rig ou timing.
  Os relatórios são `animation-bank-sidecar-boundary-audit.tsv` e
  `animation-bank-sidecar-boundary-summary.tsv`.
- `eagl-morph-sidecar-boundary-audit` repetiu a auditoria nos objetos morph com
  símbolo `__Morph:::` validado: 391 BIGs aninhados válidos, 594 observações,
  5 seções file-backed não vazias por objeto, 1 seção em `.ord` e 4 em `.orl`,
  relocation e tabela de seções em `.orl` em 594/594, sem seção cruzando a
  fronteira. O primeiro recorte nominal encontrou 886 pares, mas foi descartado
  e não é usado: somente as 594 linhas com símbolo morph comprovado permanecem
  no relatório. Isso é evidência de composição física, não vínculo de morph,
  geometria, pose, rig, peso, frame, timing ou skinning.
- `eagl-morph-shpx-structure-audit` acrescentou uma comparação independente
  entre os 594 rows do catálogo morph e o pacote `.xsh` same-stem do
  `data/models.big`: 591/594 pacotes foram encontrados e validados, 3 não têm
  `.xsh` correspondente (`tops0001`, `tops0020`, `tops0210`) e 0 foram
  rejeitados por formato. Foram 393 layouts, 17 com mais de um perfil de
  tabela SHPX; os 17 também são layouts ambíguos no relatório existente. Isso
  mede somente presença, versão, contagem e tabela SHPX; não cria vínculo de
  morph, textura, material, UV, mesh, pose, rig, peso, frame, timing ou GPU.
- `eagl-morph-elf-header-audit` repetiu a seleção pelos 594 rows e inspecionou
  somente o ELF do `.o` morph: 594/594 foram válidos, sem divergência de
  contagem, com 393/393 layouts tendo exatamente 1 perfil de header core, 1
  perfil de range da tabela e 1 perfil de metadata do símbolo `__Morph:::`.
  Isso inclui os 38 layouts ambíguos e funciona como controle negativo de
  header; não decodifica payload, morph, pose, rig, peso, frame, timing ou GPU.
- `GameAssetCatalog.Scan` também repete, em modo candidato e somente leitura,
  a cadeia same-trunk modelo→`SHAPENAME`→SHPX→entrada interna: o headless real
  reporta 24 matches, 24 objetos distintos, 1 shape (`tops`), 24 texturas
  decodificadas e 24 texturas PAL8. Esses números tornam a evidência de assets
  observada no runtime, mas não promovem slot 1 a UV global nem criam material,
  skinning, rigging ou comandos GPU.
- `eagl-morph-sibling-runtime-smoke` agora repete pelo caminho do catálogo o
  inventário dos irmãos morph: 391/391 BIGs internos validados, 594 evidências,
  594 pares `.ord/.orl`, ELF e geometria, 593 bounds e 593 símbolos
  `__PosePaletteBank:::`. O resumo não retém payloads nem atribui esses símbolos
  a poses, ossos, pesos, frames ou skinning.
- `eagl-morph-pose-bank-index` preserva os nomes desses símbolos por BIG e os
  compara por igualdade textual e metadados ELF crus com os 19 bancos `.axt`:
  foram indexadas 391 linhas, 1 símbolo distinto (`PosePaletteBank`) e 0
  matches.
  Isso é controle negativo de recorrência nominal, não prova de ausência de
  vínculo nem decoder de pose, rig, peso, frame, timing ou skinning.
- O índice agora também preserva, por linha, os metadados ELF observados do
  registro do símbolo: tabela, índice, seção, valor, tamanho e `Info`. O caso
  real foi 1 registro distinto repetido nas 391 linhas (`table_section=4`,
  `symbol_index=3`, `symbol_section=1`, `value=0`, `size=4`, `Info=17`);
  esses campos são evidência de símbolo/ELF, não foram convertidos em pose,
  osso, peso, índice ou palette.
- O host Win32 agora aceita o mesmo caminho candidato de forma explícita:
  `run-windowed ... --candidate-material [nested.big] [SHAPENAME] [objeto.ord]`
  carrega a malha e a textura somente após a validação same-trunk e passa a
  imagem ao `PreviewRenderer`; sem a opção, o caminho anterior permanece com
  `star0001-head.ord` e cor provisória. Se a cadeia opt-in falhar, o host
  registra `candidate_material_status=unavailable` e usa o fallback sólido.
  Isso integra o caminho de renderização candidato ao host interativo, mas não
  prova UV, material GPU, skinning, rigging, animação ou semântica de comandos.
- `render-preview` agora aceita também `--candidate-material [nested.big]
  [SHAPENAME] [objeto.ord]`, usando a mesma cadeia same-trunk no quadro
  standalone. A amostra `tops0030.big/tops` validou objeto textual, produziu
  2 grupos/341 vértices/337 triângulos e checksum `C1DB074C7140C585`; o grupo
  geométrico continua `unproven`. O comando recusa a ISO como saída e mantém
  a distinção entre integração candidata e semântica original.
- `animation-bank-payload-compare` compara os 19 `.data` validados em 171 pares,
  por bytes/words alinhados crus e depois de mascarar somente relocations. Não
  houve par com payload bruto ou normalizado exatamente igual; o maior par teve
  5.429 bytes e 129 words normalizados iguais no overlap. A métrica de trechos
  contíguos encontrou no máximo 40 bytes (crus e normalizados) ou 10 words
  (crus e normalizados) iguais. Isso é uma medida de recorrência alinhada e
  controle de payload, não um decoder de records, frames, poses, ossos, pesos,
  rigging, timing ou skinning.
- ISO: tamanho `2013003776`, atributo `Archive`, SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Não há recompilação do XBE; o runtime é código gerenciado novo.

Validação da etapa anterior: Release sem avisos/erros, self-test 35/35,
`eagl-material-batch-smoke` 24/24, `eagl-material-runtime-batch-smoke` 24/24
com 0 rejeições, headless padrão em 80 frames com checksum
`5405DE72BE1B4FC1`, headless candidato `tops0040.big tops` em 80 frames com
checksum `3A2D00B09DC319F6`, e áudio WinMM com `gameplay_audio_played=1` e
`gameplay_audio_decode_rejected=0`. O modo `--opponent-ai` passou em 140 frames
com `HOME 0 - 2 AWAY`, `SHOT_AWAY=2` e `GOAL_AWAY=2`. A ISO continuou somente
leitura e manteve o SHA-256 acima.
O `morph-ambiguity-group-metadata` também passou com 38/38 layouts ambíguos,
239 linhas byte associadas, 17 linhas de gap e 28 layouts sem gap.
O novo `morph-ambiguity-independent-audit` passou com 594 linhas de catálogo,
393 layouts, 355 estáveis e 38 ambíguos; houve 0/393 inconsistências de
metadados ou contagem e 0/393 divergências entre os rótulos `ambiguous/stable`
e a variabilidade medida nas oito camadas. Nos 38 ambíguos, 26 mantêm a assinatura de mesh
candidata estável enquanto o companion varia, 6 variam também na assinatura de
mesh candidata e 38/38 variam em pelo menos uma camada estrutural observada.
Isso é auditoria cross-layer reproduzível, não binding nem decoder.
O novo `eagl-morph-name-state-audit` preservou os nomes `BS_*` dos 594 morphs
e os valores `SHAPENAME` dos companions: foram 2.360 nomes morph distintos,
391/594 rows com estado textual observado e 2 valores de estado distintos,
mas 0/594 coincidências cruas ou normalizadas. Isso é um controle lexical
negativo entre duas fontes observadas; não autoriza binding de morph a grupo,
material, UV, peso, osso, pose, frame ou animação.
O novo `audio-event-named-decode-smoke` passou com 17/17 candidatos nomeados
inspecionados e 7/17 ranges decodificados integralmente, totalizando 2.123.056
bytes WAV. Os 10 restantes foram rejeitados de forma explícita porque a
metadata SCHl observada informa zero canais; nenhum nome foi convertido em
binding de gameplay, e a rota clean-room existente continua limitada às
escolhas authored `goal` e `panna`.
Na montagem normal, o mixer também pré-prepara os 7 candidatos decodificáveis
como cache nomeado: o headless reporta 7 waves e 2.122.748 bytes PCM, enquanto
a rota clean-room de gameplay continua explicitamente limitada às escolhas
authored `goal` e `panna`.
O novo `audio-abk-batch-decode-smoke` passou por 16 bancos ABKC: 15 tiveram
um candidato AST/BNK bounded decodificado, totalizando 10.051.852 bytes WAV,
com fontes `ast=6`, `bnk=9` e `none=1`. O único rejeitado foi
`data/audio/CONTROL.abk`, sem candidato de índice disponível. Isso valida
somente caminhos de payload observados e o decoder gerenciado; não cria
seleção EVT, binding de gameplay ou semântica de bancos.
O `CatalogAudioMixer` passou a executar essa montagem bounded no ciclo normal
do runtime quando recebe o opener read-only: o headless real reporta
`audio_abk_clips_prepared=15`, `audio_abk_candidate_banks_decoded=15`,
`audio_abk_candidate_banks_rejected=1` e
`audio_abk_candidate_sources=ast=6,bnk=9,none=1`. A montagem continua sem
selecionar cues para eventos.
O `eagl-candidate-state-correspondence` passou em 24/24 objetos, com 0/269
correspondências exatas de bytes ou forma de fronteira e 269/269 fluxos
completos.
O novo `eagl-state-geometry-adjacency-audit` preserva a relação por objeto em
`fifa_pc/eagl-state-geometry-adjacency.tsv`: 24/24 linhas, 0 pares com o mesmo
índice, 48 pares adjacentes, 269 pares na mesma camada e 0/24 grupos
geométricos com `SHAPENAME` exato. Isso é controle negativo de adjacência, não
binding promovido.
O novo `eagl-candidate-attribute-audit` passou em 24/24 objetos e 38/38
grupos: todos os 7.403 valores slot-1 observados têm perfil completo, finito
e em faixa próxima de unidade. Nenhum dos 38 grupos carrega diretamente
`SHAPENAME` ou clamp, enquanto 180 primitivas carregam ambos os estados.
Isso separa numericamente as populações, mas não chama slot-1 de UV nem
transfere estado para a geometria.
O `animation-bank-catalog-compare` passou com 19 perfis e 171 pares: 6 nomes
aparecem em múltiplos bancos, os 6 têm timestamp compartilhado, e não houve
conjunto de nomes, posição ou prefixo ordenado comum entre pares.
O `animation-bank-elf-structure` passou com 19/19 ELF válidos, 114 linhas de
seção, 6 seções por banco, 19 hashes de seção distintos e 19 hashes de
relocation distintos; todos os 19 tinham metadata de símbolo de banco e
distância de cauda 28. Isso continua sendo evidência de headers/layout, não
decoder de payload, frame, pose, rig, timing ou skinning.
O `animation-bank-tail-compare` passou com 19/19 caudas de 32 bytes: 0/171
pares crus iguais, 12 pares normalizados iguais, 30/32 offsets normalizados
comuns em todos os bancos e 19/19 igualdades entre a palavra observada nos
offsets 4–5 e `2 * catalog_file_count`. A nova checagem também confirmou
19/19 `declared_file_count_matches_catalog` e 19/19 igualdades entre a mesma
palavra e `2 * declared_file_count`. Nenhuma semântica foi atribuída à palavra
ou aos bytes restantes.
O `animation-bank-sidecar-boundary-audit` confirmou independentemente 19/19
divisões sem seção cruzada: em cada ELF, 1 seção file-backed não vazia fica em
`.ord`, 4 ficam em `.orl`, a seção de relocation fica em `.orl` e a tabela de
seções também fica em `.orl`. Isso descreve somente ranges físicos dos
sidecars; não transforma `.orl`, relocation ou bytes de fronteira em records,
frames, poses, rig, timing ou comandos.
Validação desta extensão: build Release sem avisos/erros, self-test 43/43,
`animation-bank-runtime-smoke` 19/19, `animation-bank-catalog-compare` 19
perfis/171 pares, `animation-bank-elf-structure` 19/19 ELF, render/material
24/24, geometria 24/24, estado 24/24, áudio SCHl 6/6, áudio ABKC 15/16
decodificados, headless padrão 80 frames com checksum
`5405DE72BE1B4FC1`, headless IA+áudio 140 frames com checksum
`651E41284E9242BC` e 2 reproduções sem rejeição, e candidato `tops0040.big`
80 frames com checksum `3A2D00B09DC319F6`. A ISO manteve 2.013.003.776 bytes,
atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
O novo `eagl-candidate-geometry-audit` também passou em 24/24 linhas, sem
rejeições, registrando 0 índices inválidos, 0 vértices não finitos, 0 índices
repetidos, 46 áreas 3D zero, 48 áreas XY zero, 196 degenerados na projeção e
7.131 triângulos visíveis.
O novo `eagl-morph-shpx-structure-audit` passou com 594/594 rows observados,
591 pacotes `.xsh` same-stem validados, 3 ausências explícitas e 0 rejeições;
17 dos 393 layouts têm perfis SHPX variáveis, todos dentro dos 38 layouts
ambíguos já identificados. A ausência de `.xsh` é preservada como evidência,
não tratada como erro de parser.
O novo `eagl-morph-elf-header-audit` passou com 594/594 ELF válidos, 393/393
layouts com um único perfil de header core, range e metadata `__Morph:::` e
0 divergências nos 38 layouts ambíguos. Isso é controle negativo de header,
não decoder de payload ou semântica de animação.
O controle independente da geometria morph também passou com 38/38 layouts
variáveis em layouts ambíguos, 0 em estáveis, 0 ambíguos sem variação geométrica
e 0 divergências de rótulo.
Com `--opponent-ai --play-audio`, a mesma row `goal` foi preparada/tocada para
`GOAL_AWAY`, também sem rejeição de decode; essa é uma regra nova do runtime.
Na mesma matriz, o relatório registrou 3 eventos sem rota no demo padrão
(5 eventos totais) e 3 no demo com AI (6 eventos totais), mantendo explícita a
lacuna de cues não decodificados.
O índice `eagl-morph-pose-bank-index` também passou com 391 linhas, 1 símbolo
distinto, 1 registro ELF distinto, 0 matches nominais e 0 coincidências de
metadados ELF com os 19 registros de bancos de animação.

Na etapa seguinte, o controle de chute Home passou a aceitar `MoveY` no instante
do disparo e converte esse input em uma componente vertical determinística. O
self-test 43/43 cobre uma trajetória ascendente; como o demo mantém `MoveY=0`,
os checksums headless permaneceram `5405DE72BE1B4FC1` (80 frames),
`651E41284E9242BC` (140 frames com IA, com e sem áudio) e
`3A2D00B09DC319F6` (80 frames com material candidato). Isso é controle novo da
reimplementação clean-room, não semântica decodificada da ISO.
Na mesma execução foram repetidos os smokes relevantes: animação 19/19,
SHPX 594/594 rows observados (591 válidos, 3 ausências e 0 inválidos), ELF
morph 594/594 válidos, material 24/24, geometria 24/24 sem índices inválidos,
estado 269/269, morph runtime 594/594, eventos nomeados 17 candidatos/7
decodificados antes da auditoria de layout; a repetição final passou 17/17,
com 144/144 hipóteses mono únicas nos ranges sem canais observados, e ABK
15/16 bancos decodificados. A ISO permaneceu com
2.013.003.776 bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Na repetição final pós-áudio, o build Release continuou sem avisos/erros, o
self-test passou em 44/44, `audio-event-named-decode-smoke` passou 17/17,
`audio-sccl-channel-layout-audit` passou 144/144 mono únicos e o headless
reportou 17 waves nomeadas preparadas, 2.914.952 bytes PCM e 0 rejeições. Os
checksums permaneceram `5405DE72BE1B4FC1` (padrão),
`6251CBFCB6A56869` (aimed-demo), `651E41284E9242BC` (IA+áudio) e
`3A2D00B09DC319F6` (material candidato).

Esta continuação adicionou o gate `project-readiness-audit`: a execução real
produziu `readiness=NOT_READY`, `iso-integrity=verified`,
`assets=candidate-integrated`, `rendering=not-proven`,
`gameplay=clean-room-integrated`, `audio=partially-integrated` e
`animation-deformation=structural-only`. O relatório também registrou
`state_rows=269` e `exact_state_pairs=0`; portanto, a ausência de associação
estado→geometria continua explícita. A nova auditoria não muda o runtime nem a
ISO e não atribui semântica a campos undecodificados.
O avanço de gameplay seguinte foi verificado com `run-headless ... 120
--round-demo --play-audio`: a rodada terminou em `match_round=2`,
`match_last_action=RESTART`, posse Home sincronizada com a bola e checksum
`8107B8CF3046E9EB`. Os checksums de referência permaneceram
`5405DE72BE1B4FC1`, `6251CBFCB6A56869`, `651E41284E9242BC` e
`3A2D00B09DC319F6` nos modos já existentes.
O avanço visual opt-in foi verificado com `run-headless ... 80 --demo
--clean-room-characters`, produzindo checksum
`C0249FD97A01C3F4`; a combinação com material candidato produziu
`29EACF0AE7B12B58`. A pose procedural é authored e não foi usada para
preencher os gates de UV, material, animação original, rigging ou skinning.
O self-test final desta extensão passou em 45/45.
O headless agora aceita `--frame-output <saida.ppm>` para preservar o último
frame do renderer em QA visual; o comando recusa sobrescrever a ISO.

Avanços desta continuação:

- A integração candidata agora audita o estado depois de carregar a malha:
  os 24 objetos same-trunk preservaram `SHAPENAME=tops,1` em suas primitivas
  inspecionadas (`24/24`), mas `0/24` grupos geométricos candidatos carregam
  esse estado diretamente. Em `tops0030`, há 11 primitivas com `SHAPENAME` e
  2 grupos de malha; a separação é mantida como evidência, não convertida em
  binding de textura, UV, material ou comando GPU. O smoke integrado continua
  candidato e opt-in: 24/24 frames, 0 rejeições, 24 checksums distintos e 38
  grupos texturizados.
- O novo comando `apt-animation-negative-control` cruza somente os relatórios
  TSV estruturais APT (`fe`/`ig`) com `animation-bank-name-index.tsv`. A ISO
  produziu 284 linhas APT, 279 objetos válidos, 259 conjuntos estruturais
  completos e 1.584 linhas de catálogo de animação; houve 0 símbolos
  `__Morph:::`, 0 candidatos nominais `*_morph` e 0 coincidências textuais de
  stems com nomes de banco ou nomes de arquivo. O resultado está em
  `fifa_pc/apt-animation-negative-control.tsv`; isso é controle negativo, não
  prova de ausência de animação nem decoder de timing/pose/frame.
- O novo `apt-cross-package-profile-audit` compara somente a chave exata
  `header APT + header .const + extensões` entre os perfis FE/IG. A execução
  encontrou 80 chaves FE, 51 IG, 27 compartilhadas, 53 exclusivas FE e 24
  exclusivas IG; nenhuma das 27 compartilhadas teve word alinhada igual e
  nenhuma teve linha com símbolo `__Morph:::`. Isso mede recorrência de
  containers/tabelas observadas, não timing, pose, frame, morph, rig ou
  animação.
- A inspeção visual das prévias `tops0030` e `tops0040` confirmou rasterização
  de textura e triângulos, mas também mostrou fragmentação/alongamento fortes.
  As versões PNG derivadas para inspeção são
  `fifa_pc/tops0030-material-preview.png` e
  `fifa_pc/tops0040-material-preview.png`; elas não substituem os PPMs nem
  constituem evidência de topologia original.
- O comando `eagl-candidate-geometry-audit` mede a representação candidata e a
  mesma projeção inteira do renderer, sem escolher nova sequência, reinício,
  UV ou binding. Nos 24 matches houve 0 índices inválidos, 0 vértices não
  finitos e 0 triângulos com índices repetidos; foram observados 46 triângulos
  de área 3D exatamente zero, 48 de área XY zero e 196 degenerados após a
  projeção, contra 7.131 visíveis. O relatório é
  `fifa_pc/eagl-candidate-geometry-audit.tsv`; esses números não decodificam
  topologia, materiais, skinning, rigging, animação ou comandos GPU.
- A nova auditoria `MorphCandidateGeometryAudit`, exposta como opções
  `--geometry-report`/`--geometry-summary` do inventário morph, mede uma
  assinatura numérica adicional da malha candidata por layout: grupos,
  vértices, triângulos, finitude, índices repetidos, áreas zero, degeneração
  raster e perfil de arestas. Na ISO foram observados 393 layouts e 594 linhas;
  38 layouts variam nesse perfil, 32 deles com assinatura de mesh candidata
  estável e geometria variável. Houve 1 layout com linha sem assinatura por
  ausência de bounds; os 393 layouts foram completos e ELF-validos, e 392
  tiveram bounds em todas as linhas. O relatório é
  `fifa_pc/morph-sibling-candidate-geometry-audit.tsv` e seu resumo. Isso mede
  somente a representação candidata e não altera os rótulos de ambiguidade nem
  cria topologia, UV, material, skinning, rigging, animação ou comandos GPU.
- O comando independente `morph-candidate-geometry-ambiguity-audit` compara a
  coluna `candidate_geometry_signature` do catálogo com os rótulos já publicados,
  sem alterar a matriz: 38 layouts variáveis coincidem com 38 ambíguos, 0 com
  layouts estáveis, 0 ambíguos sem variação geométrica e 0 divergências. Isso é
  consistência de evidência candidata, não binding ou decoder.
- A correlação opcional `--geometry-correlation-report`/`--geometry-correlation-summary`
  compara apenas a assinatura de payload pareada com a assinatura geométrica
  candidata. O resultado real contém 393 layouts: 38 variam no par
  payload/geometria, 27 mantêm o payload estável enquanto a geometria candidata
  varia e 11 variam em ambas as camadas. É uma priorização de amostras, não
  evidência de morph, pose, peso, rigging, animação ou binding de GPU.
 - O smoke específico `eagl-candidate-object-smoke ... --commands` agora
  preserva a mesma evidência crua para qualquer BIG/ORD selecionado. Em
  `tops0030`, são 13 primitivas e 13 fluxos completos; as primitivas
   `PT_TRIANGLESTRIP` que passam o gate geométrico permanecem separadas das
   primitivas TAR com `SHAPENAME=tops,1`, sem binding inferido.
- O novo `eagl-candidate-state-correspondence` compara essas duas populações
  somente por bytes crus, forma de fronteira (contagem/tamanho/completude),
  prefixo comum e IDs observados. Nos 24 objetos `tops` houve 38 primitivas
  geométricas, 180 primitivas com `SHAPENAME` e 269 pares: 0/269 bytes crus
  exatos, 0/269 formas de fronteira exatas, todos os 269 fluxos completos e
 prefixo máximo de 8 bytes. O relatório é evidência negativa/estrutural; não
 autoriza associar estado a mesh, material, UV, skinning, rigging, animação ou
 GPU.
 - O novo `eagl-candidate-attribute-audit` mede, por objeto same-trunk, os
   perfis completos do slot-1 e mantém separados os estados de grupo e de
   primitiva. Foram auditados 24 objetos, 38 grupos e 7.403 valores; os 38
   grupos são finitos e próximos da faixa unitária, mas `group_shape_state_count`
   e `group_clamp_state_count` ficaram em zero, contra 180 primitivas com cada
   estado. O TSV é diagnóstico numérico, não binding de UV, material, skinning,
   rigging, animação ou GPU.

## O que já está implementado

### Runtime e gameplay

- `StreetMatch` tem posse, movimento, passe, drible, chute, gol, relógio e HUD
  determinísticos. O modo opcional `enableOpponentAi` acrescenta pressão,
  recuperação e finalização do adversário; o caminho default permanece sem AI
  para manter a sequência de demonstração estável. Quando a bola não está em
  voo, a posição da bola acompanha o possuidor; isso é uma regra nova da
  simulação, não semântica decodificada do original.
- `StreetMatch.ActionCounts` agora registra `KICKOFF` e cada transição
  observável; `GameRuntime.Report()` expõe frame, sequência, última ação,
  posse, dribles, histograma ordenado e as posições finais de casa, adversário
  e bola. O self-test comprova separadamente
  `TRICK`, `PASS`, `PASS_COMPLETE`, `SHOT` e `GOAL_HOME`. Isso é
  telemetria da simulação nova, não uma alegação sobre eventos ou IDs originais.
- `GameRuntime`, renderer software, host headless e host Win32 estão ligados.
- A cena clean-room usa as texturas XPR0 reais validadas (`saveimage.xbx` e
  `titleimage.xbx`) sem alegar que elas provam a geometria original.
- Áudio EA-XA `SCHl` e Xbox IMA em ABKC/BNK têm leitores/decoders gerenciados.
  As associações de gameplay clean-room são `GOAL_HOME -> goal`,
  `GOAL_AWAY -> goal` e `TRICK -> panna`; as rows nomeadas são reutilizadas
  sem afirmar vínculo original EVT→som.
- `GameAssetCatalog.Scan` também incorpora a validação estrutural de
  `data/anims.big`: o runtime headless reporta 19 catálogos, 19 pares
  `.ord/.orl`, 19 ELF válidos, 19 símbolos coincidentes e 19 hashes `.data`
  normalizados distintos. Isso monta presença/identidade de asset, sem
  interpretar ou reproduzir animações.
- O mesmo scan agora percorre estruturalmente `data/aptanims/fe.bh` e
  `data/aptanims/ig.bh` sem gravar blocos: o headless reporta 2 pacotes, 284
  BIGs internos, 1.106 entradas, 284 `.apt`/`.const` válidos, 279 `.o` válidos,
  259 `.xsh` válidos, 259 conjuntos completos e 230 assinaturas compostas.
  Nenhuma das 279 inspeções `.o` expõe símbolo morph. Isso integra containers e
  companions ao catálogo, não timing, pose, morph, frame ou material.

### Catálogo e formatos

- BIG, RefPack, XDVDFS/VIVF, XPR0, SHPX, ELF/EAGL, APT e áudio têm leitores
  limitados e testes.
- Inventário real conhecido:
  - 29 BIGs validados;
  - 15 pacotes VIVF e 15 companions relacionados;
  - 6 streams AST válidos;
  - 16 ABK estruturais, 39 referências AST e 70 ranges;
  - 13 BNKs, 231 headers/ranges Xbox IMA válidos;
  - 2 XPR0 decodificados.

### Pacotes APT de animação

- `AptReader.GetStructuralSignature` e
  `AptReader.GetCompanionStructuralSignature` comparam somente metadata
  validada dos headers APT/`.const`, presença e tamanhos dos companions
  `.const`, `.o` e `.xsh` do mesmo stem no BIG interno.
- `AptReader.GetPackageStructuralSignature` acrescenta a assinatura estrutural
  do `.o` EAGL validado e a tabela SHPX validada do `.xsh` same-stem,
  preservando seções, símbolos, relocations, classes, bounds, nomes, offsets e
  tamanhos sem tratá-los como materiais ou frames.
- Os perfis observados são 133 em `fe.bh` e 97 em `ig.bh`. Isso documenta a
  organização estrutural dos pacotes, mas não converte campos em timing,
  poses, morphs, materiais ou frames.
- `apt-structure-inventory ... --report <saida.tsv>` agora materializa uma
  linha por `.apt`, preservando somente headers, companions, validade dos
  objetos/tabelas, hash da assinatura estrutural e presença literal de
  símbolos morph no `.o`. Os relatórios reais têm 163 linhas em `fe.bh` e 121
  em `ig.bh`; há 161/163 e 118/121 objetos `.o` válidos, 145/163 e 114/121
  conjuntos com `.xsh`, zero símbolos `__Morph:::` e zero candidatos nominais
  `*_morph` nos `.o` APT. Isso é uma ausência de correlação direta observada,
  não prova de que os pacotes não contenham animação.
- `apt-structure-inventory ... --profile-report <saida.tsv>` grava perfis por
  padrão de header APT, header `.const` e conjunto de extensões companion. Os
  relatórios reais têm 80 perfis em `fe.bh` e 51 em `ig.bh`; nos 568 words
  alinhados comparados, nenhum foi igual. O resultado é um controle estrutural
  negativo; não atribui os words a timing, pose, frame ou comando.

### Catálogos de bancos de animação

- `AnimationBankReader` interpreta somente a estrutura textual dos sidecars
  `.axt`: versão, nome do banco, diretório, quantidade declarada e nomes com
  timestamp bruto.
- `animation-bank-inventory` cruza os `.axt` com os pares same-bank `.ord/.orl`,
  valida o ELF combinado e compara apenas o símbolo literal
  `__AnimationBank:::BANK` com o nome do catálogo. O relatório real tem 19/19
  pares, 19/19 ELF válidos, 19/19 símbolos coincidentes e 19 hashes de layout
  metadata distintos; a distância observada do símbolo ao fim de `.data` é 28
  em todos os bancos. `AnimationBankReader` também calcula uma assinatura
  SHA-256 do `.data` combinado depois de zerar exclusivamente as words cobertas
  por relocations; os 19 hashes normalizados são distintos. Essa comparação é
  evidência negativa de um template byte-a-byte comum, não um decoder de
  records, frames, poses, ossos, pesos, rigging, timing ou skinning.
- As entradas de catálogo continuam nomes/timestamps observados. Nenhum campo
  foi promovido a frame, pose, osso, rig, peso, timing ou skinning.

### EAGL e mesh candidato

- `data/models.big`: 1.115 entradas, 391 BIGs internos, 886 pares `.ord/.orl`,
  2.390 primitivas e 37.111 comandos preservados como dados neutros.
- 2.387/2.390 primitivas passam o binding estrutural de cinco slots; os
  valores do slot 1 são preservados como `Slot1Attributes`, sem chamá-los de UV
  globalmente.
- `EaglCandidateMeshBuilder` cria somente grupos gated por strip, bounds e
  layout coerente; skinning, rigging, deformação, índice original e draw GPU
  continuam sem prova.
- `EaglMorphReader` preserva nomes `BS_*`, `Coordinates`, `Normals`, ponteiros,
  relocações e janelas de bytes; morph/ossos/poses ainda não são aplicados.
  A rechecagem do inventário encontrou 594 objetos com `__Morph:::`, 2.372
  nomes `BS_*`, 1.142 nomes de atributo, 4.719 ponteiros/janelas e 42
  assinaturas de relocação; isso continua evidência estrutural, não uma regra
  suficiente para decodificar pesos, ossos, poses ou frames.
- `EaglMorphSiblingReader` e os comandos `eagl-morph-sibling-smoke`/
  `eagl-morph-sibling-inventory` correlacionam apenas o padrão nominal
  observado `*_morph.o` → `*.ord/.orl` no mesmo BIG. O inventário real validou
  391/391 BIGs, 594/594 irmãos completos, 594/594 irmãos com símbolos
  geométricos e 593/594 com bounds; as assinaturas de relocação permanecem
  evidência de layout, não binding de morph ou animação.
- A inspeção nomeada também preserva `__PosePaletteBank:::`: 593/594 morphs
  expõem esse símbolo. Os perfis das 42 assinaturas repetidas mantêm 4 nomes
  `BS_*`, 1 ou 2 atributos e, em quase todos, esse símbolo; isso ainda não
  prova palette de poses, pesos, ossos ou aplicação de animação.
- A assinatura relativa dos offsets foi adicionada para remover a dependência
  da base absoluta: o perfil mais frequente reúne 173 morphs com gaps
  `0x0,0x4,0x8,0xC,0x24,0x28,0x34` e um campo posterior variável. É apenas
  repetição de layout bruto, sem semântica de record, índice ou atributo.
- A comparação de bytes normalizados mascara somente strings nomeadas e words
  de relocation; os 594 morphs produziram 594 hashes distintos. Isso rejeita,
  nesta regra conservadora, um template byte-a-byte compartilhado e mantém o
  payload de records sem decoder.
- Uma comparação local adicional usa janelas de raio 16 em torno dos campos de
  relocation, mascarando somente strings nomeadas e words de relocation antes
  do hash. Os 594 morphs produziram 326 assinaturas locais, 404 pares
  morph-layout/janela e 11 layouts morph com mais de uma assinatura local.
  Isso mede variação de bytes dentro de layouts repetidos; não identifica
  records, pesos, ossos, poses ou frames.
- A assinatura estrutural independente compara somente tamanho/posição da
  seção, strings observadas e campos/alvos de relocation: são 393 perfis em 594
  morphs, com maior repetição de 42 objetos. É repetição de layout bruto, sem
  conversão para records, pesos, ossos, poses ou frames.
- A correlação com o ELF companion registra somente contagens/totais de seções,
  símbolos, relocations, classes geométricas e bounds: 417 assinaturas de
  companion e 443 pares morph-layout/companion. Em 32 dos 393 perfis morph há
  mais de uma assinatura companion; o layout não seleciona uma geometria nem
  autoriza binding.
- Uma camada refinada calcula apenas um hash do layout ELF: headers de seção,
  posições/tamanhos dos símbolos geométricos e offsets relativos/tipos de
  relocation. Ela produziu 444 assinaturas, 447 pares morph-layout/layout e 32
  layouts morph com múltiplos layouts companion; continua evidência bruta, sem
  binding de geometria ou comando.
- O stream de comandos também é resumido opacamente por primitive/layer,
  offsets, assignments textuais observados, IDs/tamanhos, hashes dos bytes crus
  e relocation text. Foram observadas 592 assinaturas, 593 pares morph-layout/
  stream e 38 layouts morph com múltiplos streams; nenhum ID recebeu semântica
  GPU.
- Uma assinatura estrutural pareada combina somente as camadas independentes já
  validadas: layout morph, janelas de payload normalizadas, layout ELF companion,
  stream de comandos opaco, sequência de registros `__VB`, perfil numérico e
  topologia candidata, além da assinatura de coincidências numéricas cross-file.
  O inventário produziu 593 assinaturas pareadas distintas;
  355 dos 393 layouts morph têm uma única composição pareada estável e 38 têm
  mais de uma. Isso seleciona amostras repetíveis, mas não é binding de morph,
  rig, pose, peso, material, UV, skinning, animação ou GPU.
- Uma camada adicional compara apenas words little-endian alinhadas do payload
  morph com valores de metadata numérica já validados no companion, excluindo
  strings nomeadas e words de relocation observadas. Foram produzidas 406
  assinaturas de coincidência, e 12 layouts morph têm mais de uma forma de
  coincidência. Os nomes das categorias (`stride`, `flags`, contagens e offsets)
  descrevem somente os campos comparados; nenhum match foi convertido em peso,
  índice, pose, frame ou outro significado de asset.
- O inventário aceita `--catalog <saida.tsv>` e gera um catálogo fora da ISO
  com uma linha por morph, nome do irmão, contagem do layout, estabilidade ou
  ambiguidade e todas as assinaturas opacas usadas na correlação. O catálogo
  real gerado tem 594 linhas: 355 layouts estáveis e 239 linhas pertencentes aos
  38 layouts ambíguos. Ele é ferramenta de triagem, não uma tabela de bindings.
- A opção `--ambiguity-report <saida.tsv>` gera uma linha por layout morph e
  explicita apenas quais camadas de assinatura variam dentro dele. O relatório
  real tem 393 layouts, dos quais 38 ambíguos: 38 variam no stream de comandos,
  32 no layout companion, 27 nos registros `__VB`, 12 na coincidência numérica,
  11 nas janelas de payload, 6 no mesh candidato e 5 no perfil numérico. Essas
   contagens são variação observada, não causalidade nem semântica decodificada.
 - O novo comando `morph-ambiguity-summary` reagrupa somente os rótulos já
   observados no `morph-sibling-ambiguity-report.tsv`: os 38 layouts ambíguos e
   239 linhas foram separados em 8 composições de camadas. O TSV preserva
   contagens de sobreposição com controles estáveis e SHA-256 opacos das chaves
   de layout para rastreabilidade. Isso é uma agregação de evidência estrutural,
   não um decoder de morph, pose, rig, peso, frame, timing ou skinning.
 - O comando `morph-ambiguity-byte-summary` juntou as 8 composições ao relatório
   byte-level pela chave `morph_structural_layout`: as 239/239 linhas ambíguas
   tiveram correspondência e nenhuma ficou sem linha. Em 213/239 linhas houve
   igualdade de bytes e de limites do stream de comando com algum controle
   estável, mas 0/239 tiveram assinatura estrutural de comando exata; a camada
   de registro numérico teve 0/239 igualdades de registro, assinatura ou limite,
   e 222/239 igualdades de categoria valor/token. São contagens dos campos do
    relatório e faixas de limites observados, não semântica de comando, material,
    morph, pose, rig, peso, frame, timing ou skinning.
 - O comando `morph-ambiguity-numeric-summary` juntou o relatório de gaps pela
   mesma chave estrutural: os 17 gaps caem em 10 layouts ambíguos distribuídos
   por 4 das 8 composições; os outros 28 layouts ambíguos não possuem linha de
   gap nesse relatório. As 17 linhas produziram 7 igualdades de fronteira
   relativa, 5 de janela de payload e 0 de boundary numérico ou offset canônico
    absoluto, conforme as métricas existentes. A saída apenas organiza limites,
    diferenças e prefixos observados; não cria semântica para tokens ou offsets.
  - O novo comando `morph-ambiguity-group-metadata` juntou, por chave estrutural
    exata, os 38 layouts ambíguos aos relatórios byte-level e de gaps. A saída
  `morph-sibling-ambiguity-group-metadata.tsv` tem 38 linhas, 239 linhas byte
  associadas e 17 linhas de gap; 28 layouts não têm gap. Ela expõe somente
  contagens de perfis, igualdade de fronteiras e presença/ausência de metadata
  observada, sem transformar grupos em morph, pose, rig, peso, frame, timing,
  skinning, material, UV ou GPU.
 - O novo comando `morph-ambiguity-independent-audit` reagrupa
   independentemente as 594 linhas do catálogo estrutural e confere os oito
   contadores de perfil do relatório de ambiguidade. A execução real gerou
   `morph-sibling-ambiguity-independent-audit.tsv` e seu resumo: 393 layouts,
   0 inconsistências de metadata/contagem e 0 divergências de rótulo
   `ambiguous/stable` contra a medição independente, 26 ambíguos com mesh candidata
   estável e companion variável, 6 com mesh candidata variável e 38 com pelo
   menos uma camada variável. “Independente” aqui significa reagrupamento
   separado dos campos já observados no TSV; não significa semântica
   independente nem autoriza binding de morph, rig, pose, peso, animação,
   material, UV, skinning ou GPU.
 - O novo comando `audio-event-named-decode-smoke` cruza os candidatos nomeados
   por texto no EVT com seus ranges de header/data e tenta a decodificação
   somente quando os canais observados são utilizáveis. A execução real validou
7 candidatos (chance, intro, beat, goal, outro, gb_goal, miss) e rejeitou 10
   por zero canais. O relatório é evidência de payload de áudio, não
   associação de evento, ABKC ou regra de gameplay.
 - A auditoria posterior `audio-sccl-channel-layout-audit` testou os 144 rows
   com `channels=0` observado contra hipóteses mono/estéreo e decodificação de
   todos os frames: 144/144 foram mono únicos, 0/144 foram estéreo e 0 ficaram
   ambíguos. O header não é alterado; o resultado é armazenado como hipótese
   separada e permitiu preparar os 17/17 candidatos nomeados no mixer.
 - O novo comando `audio-abk-batch-decode-smoke` percorre os 16 bancos ABKC e
   tenta no máximo oito índices já observados por banco, primeiro nos ranges AST
   e depois nos índices BNK. A execução real validou 15 bancos, rejeitou 1
   (`data/audio/CONTROL.abk`, sem candidato disponível), produziu 10.051.852
   bytes WAV e registrou fontes `ast=6`, `bnk=9`, `none=1`. O relatório
   `audio-abk-batch-decode.tsv` é evidência bounded de payload; não atribui
   banco a EVT, gameplay, material, UV, skinning, rigging, animação ou GPU.
 - O `CatalogAudioMixer` agora usa a mesma seleção bounded no ciclo normal de
   `Initialize` do headless e do host Win32 quando há opener read-only. A
   telemetria conserva 16 bancos observados, 15 decodificados, 1 rejeitado e
   fontes `ast=6`, `bnk=9`, `none=1`; nenhum banco é ligado a evento.
 - O comando `audio-event-index-search <iso> --data --report <saida.tsv>` agora
   grava `audio-event-index-evidence.tsv`: 1.061 pares candidatos, 17 offsets
   de strings/words distintos, 16 `.abk` pesquisados e zero ocorrências
   alinhadas nos bancos, em `pcpahdr.big` e em `pcpadat.big`. O relatório é
   explicitamente um controle negativo limitado à busca alinhada, com
   `status=no-direct-selection-binding`, `direct_selection_binding=not-proven`
   e `original_semantics=not-claimed`; não cria binding EVT→linha, ABKC ou
   playback.
 - O novo `gameplay-audio-coverage-audit <workspace> [--report <saida.tsv>]`
   agrega os traces/replays authored de full-stack, round, gamebreaker, juggle,
   panna, FULL_TIME e STEAL. A execução real verificou `7/7` cenários,
   `36/36` eventos preparados/tocados, `0` sem rota, `14/14` ações da política
   e `8` cues únicos. O relatório é `verified-authored` com
   `original_semantics=not-claimed`; não representa seleção original EVT/ABKC.
   SHA-256 do relatório: `1FF0AF219F61FBBFB0892C58EA3731FB8AC58E2D6EC06CCCE7DABD356FFCFE7A`.
 - Na bateria final, o headless padrão de 80 frames produziu checksum
   `5405DE72BE1B4FC1`; o smoke `STEAL` de 80 frames com material candidato,
   oponente e áudio produziu `BA2CD81182DF970E`, com `2/2` cues authored
   preparadas/tocadas (`KICKOFF->intro`, `STEAL->tackle`) e zero eventos sem
   rota.
- A opção `--byte-report <saida.tsv>` compara, contra uma linha de controle de
  cada um dos 355 layouts estáveis, os bytes crus dos streams de comando e os
  bytes UTF-8 dos registros canônicos de coincidência numérica. O relatório
  atual tem 239 linhas ambíguas: 213 têm stream de comando exatamente igual a
  pelo menos um controle, mas 0 têm a assinatura estrutural de comando exata;
  26 não têm igualdade byte-level exata e nenhuma das 239 tem registro
  canônico numérico ou assinatura numérica exatamente iguais. A igualdade de bytes não
  reconcilia as assinaturas estruturais diferentes e não atribui semântica a
  comandos GPU, materiais, atributos ou números.
- Como controle adicional, cada camada dos 38 layouts ambíguos foi comparada por
  igualdade exata com a população dos 355 layouts estáveis. Houve interseção em
  `numeric_descriptor` em 38 layouts, `candidate_mesh` em 32,
  `payload_window` em 26, `vertex_record` em 25 e `companion_layout` em 2;
  não houve interseção em `command_stream` nem em
  `companion_numeric_match`. Isso é recorrência de assinatura entre populações,
  não classificação semântica ou causalidade.
- A nova assinatura `command_stream_boundary_signature` separa cada partição
  de primitiva do fluxo bruto por ordem, quantidade de comandos, tamanho,
  completude e SHA-256 dos bytes. No inventário foram observadas 252
  assinaturas, 402 pares layout/partição e 9 layouts com mais de uma partição.
  Entre as 239 linhas ambíguas, 213 também coincidiram exatamente nessa
  partição com algum controle estável. Isso é uma comparação de limites
  observados; não atribui significado aos comandos ou aos seus payloads.
- A coincidência numérica foi separada em duas normalizações explícitas:
  sequência dos offsets dos registros (394 assinaturas, 394 pares e 1 layout
  com variação) e tokens valor/categoria sem o offset (283 assinaturas, 399
  pares e 6 layouts com variação). A segunda coincidiu em 222/239 linhas
  ambíguas, mas a comparação canônica completa continuou em 0/239. Esses
  tokens são apenas campos textuais observados; não são pesos, índices, poses
  ou outros records decodificados.
- O novo `--numeric-gap-report` isolou as 17 linhas sem coincidência de
  valor/categoria, distribuídas em 10 layouts. Nenhuma coincidiu por boundary
  ou por tokens; os controles mais próximos formam 8 conjuntos textuais.
  O artefato é triagem reproduzível, não uma seleção de morph ou de rig.
  A coluna `relative_offset_boundary_signature` agora remove a base absoluta e
  preserva somente os deltas entre offsets adjacentes observados: 7/17 linhas
  coincidem exatamente com algum controle estável, 10/17 não coincidem, em 8
  perfis relativos. Isso é repetição de fronteiras observadas, não records.
  A correlação foi ampliada para contar a interseção contra todos os 355
  controles estáveis, por camada independente: 2/17 linhas têm limite bruto de
  comando igual, 7/17 têm a fronteira relativa igual e 5/17 têm a sequência
  completa das oito janelas normalizadas igual. Nenhuma tem boundary numérico
  do companion ou offsets canônicos absolutos iguais. Essas contagens não são
  um score combinado nem um binding; continuam sendo triagem estrutural.
  A comparação adicional por sequência ordenada de tokens mostrou, nos 17
  casos, delta absoluto de quantidade igual a zero e prefixo comum máximo de 4
  tokens; 15 casos tiveram no mínimo 8 posições diferentes e 2 tiveram 16.
  `TokenSequenceComparer` mede somente igualdade posicional, comprimento e
  prefixo; na melhor comparação de cada linha, as diferenças ficaram na
  porção textual antes do delimitador (`8` ou `16`) e a porção posterior teve
  `0` diferenças. Isso é uma separação sintática observada, não uma atribuição
  de valor, categoria ou campo semântico. O relatório também registra os
  índices ordinais zero-based das posições divergentes; os 17 casos formaram
  quatro padrões de posição, repetidos respectivamente em 7, 6, 2 e 2 linhas.
  Esses índices são posições na sequência textual, não índices de vértice,
  osso, pose ou comando.
- A comparação adicional de `CanonicalMatches` contra o mesmo controle textual
  mais próximo preserva o word observado, o offset observado, o sufixo textual
  e os quatro bytes little-endian do word. Os 17 casos continuam não-exatos:
  há 8 ou 16 words diferentes, zero diferenças no sufixo, e 8 ou 16 bytes
  little-endian diferentes. As diferenças de offset observadas aparecem em
  21, 24, 26, 42 ou 58 posições conforme o comprimento; isso é comparação de
  payload alinhado, não reconstrução de record ou binding.
  O campo `nearest_control_canonical_offset_delta_signature` preserva a
  sequência assinada desses deltas; os 17 casos produziram 10 assinaturas
  distintas. A sequência é uma diferença de limites observados, não um
  deslocamento de vértice ou de animação.
- O mesmo controle também é comparado pela sequência de oito hashes das janelas
  normalizadas do payload. Nenhum dos 17 casos coincide exatamente nessa
  sequência: as diferenças observadas variam de 2 a 8 hashes, os iguais de 0 a
  6 e o prefixo comum de 0 a 2. São hashes de vizinhanças normalizadas, não
  uma identificação de record ou de asset.
- O argumento `--numeric-gap-correlation-report <saida.tsv>` cruza os 17 gaps
  pelo `nearest_control_canonical_offset_delta_signature`, sem nomear os
  campos. O relatório real produziu 10 grupos de delta; dentro desses grupos
  há 12 perfis globais de oito janelas normalizadas e 10 perfis globais de
  limites brutos de comando. Três grupos repetem o mesmo perfil de janela e
  seis repetem o mesmo perfil de limite; somente 2/17 linhas coincidem com um
  limite bruto de algum dos 355 controles estáveis, sempre com no máximo um
  controle. A mesma saída registra também 7/17 fronteiras relativas iguais,
  5/17 sequências completas de janela iguais e 0/17 coincidências exatas de
  boundary numérico ou offsets canônicos. Cada camada permanece isolada; isso
  é correlação estrutural reproduzível, não decoder nem binding.
- O perfil estrutural dos descritores `__VB` foi comparado sem nomear atributos:
  235 assinaturas, 407 pares morph-layout/descritor e 13 perfis morph com mais
  de uma assinatura de descritor. Streams, bytes, elementos, strides, flags e
  seções continuam metadata bruta validada, não posições, UVs, normais, pesos
  ou deformações.
- A sequência relativa dos registros `__VB` também foi resumida por hash,
  preservando somente seção, offsets relativos, descriptor/data offsets, bytes,
  stride e flags. Foram observadas 358 assinaturas, 431 pares morph-layout/
  sequência e 27 layouts morph com múltiplas sequências. Isso mede a ordem e
  os campos crus dos registros, não atribui qualquer stream a um atributo.
- O perfil numérico neutro acrescenta apenas finitude de palavras float
  empacotadas e containment de streams de stride 12 nas bounds declaradas:
  12 assinaturas, 398 pares morph-layout/número e 5 perfis morph com múltiplas
  assinaturas numéricas. Nenhum componente recebeu semântica.
- A topologia candidata do companion foi resumida somente por grupos, vértices,
  triângulos e primitivas ignoradas: 593 companions têm assinatura, 221 valores
  distintos incluindo o caso vazio, 399 pares morph-layout/topologia e 6 perfis
  morph com múltiplas topologias. Isso não é topologia original nem deformação.

### Material EAGL candidato recém-integrado

Foi implementado um vínculo conservador e reproduzível no mesmo tronco:

`tops0030.big` → `tops0030.ord` com `SHAPENAME=tops,1` → `tops0030.xsh` →
registro SHPX interno exato `tops`.

- `EaglShapeMaterialLoader.cs` valida essa cadeia e decodifica a imagem PAL8.
- `SoftwareFrameBuffer.FillTexturedTriangle` rasteriza uma imagem RGBA em um
  triângulo candidato, com alpha e amostragem limitada.
- `EaglCandidateMeshRenderer.DrawSlot1TextureCandidate` usa os valores
  genéricos do slot 1 somente no caminho opt-in do preview; não estabelece que
  slot 1 seja UV em todos os objetos.
- `eagl-material-smoke` verifica um caso específico.
- `eagl-material-batch-smoke` verifica a regra em lote.
- `eagl-material-runtime-batch-smoke` percorre os mesmos 24 vínculos e passa
  cada mesh/textura pela cadeia integrada `SoftwareGameplayRenderer` /
  `PreviewRenderer`, junto com `titleimage.xbx`; foram produzidos 24/24 frames,
  0 rejeições, 24 checksums distintos e 38 grupos candidatos texturizados.
  A opção `--report` agora materializa uma linha por amostra em
  `fifa_pc/eagl-material-runtime-batch.tsv`; o gate de prontidão lê esses dados
  como rastreabilidade de runtime candidato, sem promover o estado do grupo a
  binding de material ou GPU.
  Isso aumenta a cobertura de integração, mas continua sendo candidato e não
  atribui semântica global a UV, material GPU, skinning, rigging ou comandos.
- `eagl-material-preview` gera uma prévia PPM com malha e textura candidatas.
- `run-headless ... --candidate-material` liga essa cadeia ao
  `SoftwareGameplayRenderer` de forma explícita; aceita
  `--candidate-material [nested.big] [SHAPENAME] [objeto.ord]` e mantém
  fallback sólido quando o recurso candidato não é validado. O modo padrão
  continua sem textura candidata e preserva o checksum anterior.

Resultados reais:

- `tops0030.big`: 2 grupos, 341 vértices, 337 triângulos, textura PAL8
  256x124, 2 grupos texturizados, checksum
  `FB81D8B0DBB5E819`.
- `tops0040.big`: 2 grupos, 527 vértices, 523 triângulos, textura PAL8
  256x256, 2 grupos texturizados, checksum
  `F130B0BAB3361DDF`.
- Batch: 24 arquivos `.big` distintos passaram a regra same-trunk completa;
  todos são da família `tops` e usam a entrada interna `tops`.
- A auditoria `eagl-shapename-index` foi repetida após a correlação: os 724
  pacotes `.xsh` têm prefixos `star`, `tops`, `tats`, `bott`, `logo` e `ball`,
  mas os estados `SHAPENAME` observados continuam somente `head,1` (522) e
  `tops,1` (252). Não houve coincidência exata de nome completo ou prefixo com
  basename `.xsh`; entre entradas SHPX internas, houve somente a coincidência
  de prefixo `tops`. Os pacotes `star` carregam estados `head,1` mas nomes
  internos `face`/`hair`, portanto não autorizam uma cadeia material. Nenhuma
  família adicional foi promovida a candidato.
- Artefatos:
  - `D:\projeto zeta\fifa_pc\tops0030-material-preview.ppm`
  - `D:\projeto zeta\fifa_pc\tops0040-material-preview.ppm`
  - `D:\projeto zeta\fifa_pc\tops0030-material-preview.png`
  - `D:\projeto zeta\fifa_pc\tops0040-material-preview.png`
  - `D:\projeto zeta\fifa_pc\eagl-candidate-geometry-audit.tsv`
  - `D:\projeto zeta\fifa_pc\eagl-material-runtime-batch.tsv`
  - `D:\projeto zeta\fifa_pc\render-preview-stage.ppm`
  - `D:\projeto zeta\fifa_pc\render-preview-candidate-stage.ppm`
  - `D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit.tsv`
  - `D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit-summary.tsv`
  - `D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-correlation.tsv`
  - `D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-correlation-summary.tsv`
  - `D:\projeto zeta\fifa_pc\Runtime\MorphCandidateGeometryAmbiguityAudit.cs`
  - `D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-ambiguity-audit.tsv`
  - `D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-ambiguity-audit-summary.tsv`
  - `D:\projeto zeta\fifa_pc\candidate-projection-previews-stage-1\`

Isso é integração software candidata, não material final do jogo. Não
extrapolar para `head`, `face`, `hair`, outros prefixos ou para a semântica dos
comandos GPU sem nova evidência independente.

## Arquivos importantes desta etapa

- `D:\projeto zeta\fifa_pc\Runtime\EaglShapeMaterialLoader.cs`
- `D:\projeto zeta\fifa_pc\Runtime\ShapeImageTextureLoader.cs`
- `D:\projeto zeta\fifa_pc\Runtime\SoftwareFrameBuffer.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglCandidateMeshRenderer.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglCandidateMeshBuilder.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglCandidateGeometryAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\MorphCandidateGeometryAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglPrimitiveStateCorrespondence.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglStateGeometryAdjacencyAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglStateGeometryProfileAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglStateGeometryOrderAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglMorphReader.cs`
- `D:\projeto zeta\fifa_pc\Runtime\TokenSequenceComparer.cs`
- `D:\projeto zeta\fifa_pc\Runtime\NumericTokenSequenceComparer.cs`
- `D:\projeto zeta\fifa_pc\Runtime\NumericGapCorrelation.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AptStructuralCorrelation.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AptAnimationNegativeControl.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AptCrossPackageProfileAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AnimationBankReader.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AnimationBankCatalogComparison.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AnimationBankElfStructureReport.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AnimationBankDataRelocationAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AnimationBankTailComparison.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AnimationBankSidecarBoundaryAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglMorphSidecarBoundaryAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\GameAssetCatalog.cs`
- `D:\projeto zeta\fifa_pc\animation-bank-payload-comparison.tsv`
- `D:\projeto zeta\fifa_pc\apt-structure-fe.tsv`
- `D:\projeto zeta\fifa_pc\apt-structure-ig.tsv`
- `D:\projeto zeta\fifa_pc\apt-structure-fe-profiles.tsv`
- `D:\projeto zeta\fifa_pc\apt-structure-ig-profiles.tsv`
- `D:\projeto zeta\fifa_pc\apt-cross-package-profile-audit.tsv`
- `D:\projeto zeta\fifa_pc\apt-cross-package-profile-audit-summary.tsv`
- `D:\projeto zeta\fifa_pc\eagl-candidate-geometry-audit.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit-summary.tsv`
- `D:\projeto zeta\fifa_pc\animation-bank-inventory.tsv`
- `D:\projeto zeta\fifa_pc\animation-bank-name-index.tsv`
- `D:\projeto zeta\fifa_pc\animation-bank-catalog-profiles.tsv`
- `D:\projeto zeta\fifa_pc\animation-bank-catalog-pairs.tsv`
- `D:\projeto zeta\fifa_pc\animation-bank-elf-profiles.tsv`
- `D:\projeto zeta\fifa_pc\animation-bank-elf-sections.tsv`
- `D:\projeto zeta\fifa_pc\animation-bank-tail-comparison.tsv`
- `D:\projeto zeta\fifa_pc\animation-bank-sidecar-boundary-audit.tsv`
- `D:\projeto zeta\fifa_pc\animation-bank-sidecar-boundary-summary.tsv`
- `D:\projeto zeta\fifa_pc\eagl-morph-sidecar-boundary-audit.tsv`
- `D:\projeto zeta\fifa_pc\eagl-morph-sidecar-boundary-summary.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\MorphShpxStructureAudit.cs`
- `D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit-summary.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\MorphElfHeaderAudit.cs`
- `D:\projeto zeta\fifa_pc\morph-sibling-elf-header-audit.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-elf-header-audit-summary.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\EaglMorphSiblingReader.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglMorphNameStateAudit.cs`
- `D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv`
- `D:\projeto zeta\fifa_pc\eagl-morph-name-state-audit.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\EaglMorphAnimationNameNegativeControl.cs`
- `D:\projeto zeta\fifa_pc\eagl-morph-animation-name-negative-control.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\MorphAmbiguitySummarizer.cs`
- `D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-summary.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\MorphAmbiguityByteSummarizer.cs`
- `D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-byte-summary.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\MorphAmbiguityNumericSummarizer.cs`
- `D:\projeto zeta\fifa_pc\Runtime\MorphAmbiguityGroupSummarizer.cs`
- `D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-numeric-summary.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-group-metadata.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\MorphAmbiguityIndependentAudit.cs`
- `D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-independent-audit.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-independent-audit-summary.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\AudioNamedEventCandidateDecoder.cs`
- `D:\projeto zeta\fifa_pc\Runtime\AudioEventIndexEvidenceReport.cs`
- `D:\projeto zeta\fifa_pc\audio-event-index-evidence.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\GameplayAudioCoverageAudit.cs`
- `D:\projeto zeta\fifa_pc\gameplay-audio-coverage.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\CandidateRenderInfluenceAudit.cs`
- `D:\projeto zeta\fifa_pc\candidate-render-influence-audit.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\CandidateIntegratedRuntimeAudit.cs`
- `D:\projeto zeta\fifa_pc\candidate-integrated-runtime-audit.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\AudioScclChannelLayoutAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaSchlAudioDecoder.cs`
- `D:\projeto zeta\fifa_pc\audio-sccl-channel-layout-audit.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\ProjectReadinessAudit.cs`
- `D:\projeto zeta\fifa_pc\project-readiness.tsv`
- `D:\projeto zeta\fifa_pc\audio-named-event-decode.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\AudioBankBatchDecoder.cs`
- `D:\projeto zeta\fifa_pc\audio-abk-batch-decode.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\EaglCandidateAttributeAudit.cs`
- `D:\projeto zeta\fifa_pc\eagl-candidate-attribute-audit.tsv`
- `D:\projeto zeta\fifa_pc\eagl-candidate-state-correspondence.tsv`
- `D:\projeto zeta\fifa_pc\eagl-state-geometry-adjacency.tsv`
- `D:\projeto zeta\fifa_pc\eagl-state-geometry-profile-audit.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-byte-comparison-report.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-report.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-correlation-report.tsv`
- `D:\projeto zeta\fifa_pc\morph-pose-bank-index.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\GameAssetCatalog.cs`
- `D:\projeto zeta\fifa_pc\Runtime\GameRuntime.cs`
- `D:\projeto zeta\fifa_pc\material-batch-previews-stage-3\manifest.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\EaglObjectReader.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglVertexBufferReader.cs`
- `D:\projeto zeta\fifa_pc\Runtime\HeadlessBackends.cs`
- `D:\projeto zeta\fifa_pc\Runtime\PreviewRenderer.cs`
- `D:\projeto zeta\fifa_pc\Runtime\CleanRoomPlayerAnimator.cs`
- `D:\projeto zeta\fifa_pc\clean-room-characters-preview.ppm`
- `D:\projeto zeta\fifa_pc\runtime-characters-demo.ppm`
- `D:\projeto zeta\fifa_pc\Runtime\StreetMatch.cs`
- `D:\projeto zeta\fifa_pc\runtime-gameplay-trace.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\MatchInputScript.cs`
- `D:\projeto zeta\fifa_pc\Runtime\MatchTraceSerializer.cs`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-input.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-script-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-demo-input.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-round-input.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-script-demo-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-script-demo-replayed-stage.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-script-round-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-script-round-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-round-demo-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-round-demo-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-panna-input.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-input.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-input.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-panna-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-panna-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-full-time-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-full-time-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-steal-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-steal-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-steal-tackle-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-clean-room-steal-tackle-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-audio-combined-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-audio-combined-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-audio-steal-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-audio-steal-replayed.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\GameplayAudioTraceSerializer.cs`
- `D:\projeto zeta\fifa_pc\Runtime\CleanRoomIntegrationReport.cs`
- `D:\projeto zeta\fifa_pc\gameplay-full-stack-integration.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-full-stack-replayed-integration.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-full-stack-match-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-full-stack-match-replayed.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-full-stack-audio-trace.tsv`
- `D:\projeto zeta\fifa_pc\gameplay-full-stack-audio-replayed.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\MorphShpxGeometryCorrelationAudit.cs`
- `D:\projeto zeta\fifa_pc\morph-sibling-shpx-geometry-correlation.tsv`
- `D:\projeto zeta\fifa_pc\morph-sibling-shpx-geometry-correlation-summary.tsv`
- `D:\projeto zeta\fifa_pc\Runtime\Win32GameHost.cs`
- `D:\projeto zeta\fifa_pc\Runtime\ShpxExportCrosscheck.cs`
- `D:\projeto zeta\fifa_pc\Runtime\EaglCandidateAssetProvenanceAudit.cs`
- `D:\projeto zeta\fifa_pc\Runtime\SelfTestRunner.cs`
- `D:\projeto zeta\fifa_pc\Program.cs`
- `D:\projeto zeta\CONTINUAR_FIFA_STREET_2.md`
- `D:\projeto zeta\fifa_pc\README.md`
- `D:\projeto zeta\docs\FIFA_ASSET_FORMAT_READERS.md`
- `D:\projeto zeta\docs\FIFA_RECOMPILATION_STATUS.md`
- `D:\projeto zeta\docs\FIFA_EXTERNAL_BEHAVIOR_EVIDENCE.md`

## Avanço mais recente: influência observável no rasterizador candidato

Foi adicionada a auditoria `eagl-candidate-render-influence-audit`. Ela percorre
os 24 vínculos same-trunk e compara o mesmo mesh candidato com textura
candidata e com fallback sólido no `SoftwareGameplayRenderer`, mantendo a cena
authored equivalente. A geometria foi finita/indexada em `24/24`; o checksum
texturizado diferiu do sólido na cena em `20/24`. `tops0080`, `tops0140`,
`tops0260` e `tops0340` ficaram explicitamente como
`observed-candidate-raster-unchanged` nessa cena. O controle isolado em buffer
neutro encontrou influência texturizada em `24/24`, sem converter isso em
sucesso de binding.

O relatório é `fifa_pc/candidate-render-influence-audit.tsv`, SHA-256
`886419C9E716086C87BB36E2DF9728FF734E1D883FCCDEEDFC683A20048BB6E2`. Ele
preserva contagens de triângulos visíveis, diversidade/alpha RGBA e diferenças
RGB por pixel nos dois controles, mas a
conclusão é somente influência do candidato no rasterizador gerenciado:
`original_semantics=not-claimed`; não há promoção do slot genérico a UV,
material, skinning, rigging, animação ou comandos GPU. A readiness continua
`NOT_READY`, com assets `candidate-integrated`/`passed=False` e rendering
`not-proven`.

Bateria final: build Release sem avisos/erros, self-test `53/53`, material/runtime
`24/24`, headless padrão de 80 frames com checksum `5405DE72BE1B4FC1`, full-stack
de 140 frames com checksum `088EC5D10096A738`, match/áudio reproduzidos com
`matched=True`, cobertura de áudio `8/8` cenários e `42/42` eventos preparados/
tocados, com `0` não mapeados; influência candidata na cena `20/24` e no
controle isolado `24/24`. A ISO ficou em `2013003776` bytes, atributo
`Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Os relatórios de integração gerado/replay continuam iguais, agora com SHA-256
`F0329A2C5189CC326D9099783FC771C16BD8227B48B71DB989D27E9DB3E2DE8F`; o
readiness atual tem SHA-256
`426855D566E382F940742062FA7F39C3854FE1DBEE842CF045C042BB10D5CCFB`.

## Próxima etapa recomendada

0.33. O join entre a tabela indexada e os call-sites bounded foi concluído:
     `81` linhas de alvo, `1` base, `24/24` call-sites joined, `27` índices
     distintos, `27` ocorrências e `3` duplicatas. Preservar as duplicatas é
     importante para a evidência estrutural, mas isso ainda não fornece
     limites de função, layout de objeto ou semântica; o próximo avanço deve
     procurar evidência independente, sem promover índices a bindings.
0.34. O único destino direto aninhado dentro do slice de `0x0016BF90` foi
     inspecionado em outra janela bounded: `0x0026E278`, `89` instruções
     lineares, `37` alcançáveis, `7` blocos, `1` retorno e `0` chamadas
     adicionais. Esse resultado é apenas caminho local candidato e não autoriza
     seguir chamadas, recuperar função ou atribuir semântica.
0.35. Os dois call-sites internos de `0x0016BF90` para `0x0026E278` foram
     preservados em relatório: `2` ocorrências, `1` alvo distinto e `0/2` com
     `push edx`/`lea [ecx+...]`/`ret 4`; ambos têm `ret 8`. Isso ainda é
     contexto estrutural, não semântica original.
0.36. O runtime candidate-only de renderização foi registrado em gate separado:
     `rendering-candidate-runtime=verified-candidate`, com `140/140` frames,
     `24` assets, `280` samples authored, `8/8` eventos de áudio e
     replay/framebuffer válidos. Manter `rendering=not-proven` até evidência
     independente de estado → geometria/UV/material.
0.37. O runtime candidate-only de áudio foi registrado em gate separado:
     `audio-candidate-runtime=verified-candidate`, com `8/8` eventos preparados
     e reproduzidos, `0` não mapeados e replay coincidente. Manter o gate
     original parcial até evidência independente de seleção EVT/ABKC/amostra.

0.25. O controle authored de gameplay desta etapa foi ampliado e verificado:
     `clean-room-gameplay-invariant-audit` passou `9/9` cenários, `677` frames
     e `49` eventos, cobrindo finitude, bounds, monotonicidade, sequência,
     término e ações requeridas. Isso continua separado das regras originais.

0.5. Executar `project-readiness-audit` como gate antes de promover qualquer
    nova integração. O relatório atual confirma a ISO, mas mantém assets como
    candidatos, renderização como `not-proven`, gameplay como clean-room,
    áudio como parcial e animação/deformação como estrutural. As colunas
    `next_required_evidence` são a lista operacional do que ainda falta; o
    comando retorna sucesso por ter auditado, mesmo quando `readiness=NOT_READY`.
    A auditoria complementar de adjacência não fechou o gate: há 0/24 pares
    no mesmo índice, 48 pares adjacentes e 269 pares na mesma camada, mas
    nenhum desses padrões fornece a associação independente necessária para
    transferir estado a grupos de geometria ou para validar material, UV ou
    comandos GPU.

0. O caminho candidato same-trunk já está integrado tanto ao headless quanto ao
   host Win32, sempre opt-in. O próximo avanço de renderização deve continuar
   usando casos verificáveis e ampliar somente quando houver evidência
   independente de mesh, SHPX e estado; não transformar o `Slot1Attributes`
   atual em binding global. A auditoria mais recente confirmou o estado no
   objeto em 24/24 casos, mas não confirmou associação direta aos grupos
   geométricos candidatos (0/24); manter essa lacuna explícita. A auditoria
   geométrica confirmou 0 índices inválidos, 0 vértices não finitos e 0 índices
   repetidos nos 24 meshes, mas encontrou 46 áreas 3D zero, 48 áreas XY zero e
   196 triângulos degenerados após a projeção inteira, contra 7.131 visíveis.
    O novo relatório de correspondência estado/topologia também encontrou 0/269
    igualdades exatas de bytes ou forma de fronteira, com prefixo máximo de 8
    bytes; isso não cria uma associação negativa absoluta, mas não fornece a
    evidência necessária para transferir `SHAPENAME` aos grupos. Esses números
    não justificam corrigir strip, índices, UVs ou bindings por aparência; o
    próximo avanço deve buscar evidência independente de topologia e estado
    antes de alterar a cadeia. O novo relatório
    `morph-sibling-candidate-geometry-audit.tsv` acrescenta uma medição
    numérica do mesh candidato sem alterar a matriz de ambiguidade: 38/393
    layouts variam, 32/38 mantêm mesh candidata estável com geometria variável
    e 1 layout tem linha sem bounds. Usar esse relatório apenas para selecionar
    amostras de comparação adicional; não corrigir a malha nem inferir UV,
    material, rigging ou animação a partir desses perfis.
    A correlação `morph-sibling-candidate-geometry-correlation.tsv` acrescenta
    38 layouts com par payload/geometria variável, 27 com payload estável e
    geometria variável e 11 com ambas as camadas variáveis; ela também é somente
    triagem estrutural.
    A auditoria independente
    `morph-sibling-candidate-geometry-ambiguity-audit.tsv` cruzou essa coluna
    com a matriz existente e encontrou 38/38 variáveis em layouts ambíguos,
    0 em layouts estáveis, 0 ambíguos sem variação e 0 divergências; usar isso
    apenas como controle de consistência.

1. Usar `animation-bank-inventory.tsv` e
   `animation-bank-payload-comparison.tsv` e o novo
   `animation-bank-name-index.tsv` como vínculo estrutural comprovado entre
   catálogos `.axt` e bancos `.ord/.orl`: os 19 nomes de banco coincidem com
   `__AnimationBank:::`. A normalização que mascara apenas words de relocation
   produziu 19 hashes `.data` distintos, portanto não revelou um template
   byte-a-byte comum. A comparação pareada também não encontrou payload bruto
   ou normalizado exatamente igual; a recorrência parcial máxima observada foi
   5.429 bytes/129 words normalizados no overlap; o maior trecho contíguo foi
   40 bytes ou 10 words, tanto cru quanto normalizado. A próxima análise deve
   continuar limitada a limites, layouts e payloads repetíveis antes de qualquer
   tentativa de frame/pose/timing; o símbolo e as assinaturas negativas não são
   decoders. O smoke `animation-bank-runtime-smoke` confirma que essa metadata
   chega ao catálogo do runtime; o próximo passo deve usá-la apenas para
   seleção/diagnóstico estrutural, não para mover a partida sem um decoder
   validado.
   A comparação independente `animation-bank-catalog-compare` agora registra
   perfis dos 19 catálogos e 171 pares: houve 6 nomes em múltiplos bancos, todos
   com timestamp observado igual, mas 0 pares com conjunto distinto de nomes
   idêntico, 0 nomes na mesma posição e 0 prefixos ordenados comuns. Essa é uma
   matriz de sobreposição textual/temporal observada; o próximo recorte deve
   cruzá-la somente com headers/constantes/tabelas ou timing externo repetível,
   sem converter nomes/timestamps em frames, poses ou seletores.
   O relatório `animation-bank-elf-structure` já materializou essa camada de
   headers/layout: os 19 ELF têm 6 seções cada, 19 perfis de seção e 19 perfis
   de relocation distintos, com metadata de símbolo e cauda `.data` observadas
   em 19/19. Como não há recorrência de layout suficiente para um template
   comum, o próximo passo deve cruzar somente campos independentes de header,
   constantes/tabelas ou timing externo; não preencher records a partir de
   tamanhos, offsets ou contagens.
   A comparação `animation-bank-tail-compare` encontrou 32 bytes por cauda e
   uma igualdade 19/19 entre a word little-endian nos offsets 4–5 e o dobro da
   contagem catalogada, além de 30/32 offsets normalizados comuns. A etapa
   seguinte confirmou independentemente 19/19 contagens declaradas do `.axt`
   iguais ao número de entradas parseadas e 19/19 igualdades da mesma word com
   `2 * declared_file_count`. Isso é evidência numérica comprovada, mas ainda
   não dá semântica ao campo; não usar a word para seleção/runtime.
   A nova auditoria `animation-bank-sidecar-boundary-audit` observou em 19/19
    bancos uma divisão sem cruzamento: somente `.data` fica em `.ord`, as
    demais seções file-backed, a relocation e a tabela de seções ficam em
    `.orl`. O resultado é candidato/estrutura de sidecar, não um decoder; usar
    essa fronteira apenas para limitar comparações futuras de payload.
    A auditoria independente `animation-bank-data-relocation-audit` agora
    mediu somente os offsets relativos e gaps adjacentes das relocations de
    `.data`: 19/19 ELF válidos, 35.900 relocations, 19/19 bancos com
    relocations, 19 perfis de offsets distintos e 15 contagens distintas de
    gaps; o maior banco tem 29.152 offsets distintos e 291 gaps distintos.
    Essa medição confirma diversidade de layout, não fornece a regra ausente
    para frames, poses, rig, timing ou skinning. A próxima frente permanece o
    cruzamento independente dos grupos morph/companions, sem converter offsets
    em records.
2. Continuar a investigação estrutural de morph/rig/animação além da
   correlação nominal já validada, usando o novo relatório que cruza as 10
   assinaturas de deltas de offset com os perfis de oito janelas normalizadas e
   com os limites brutos repetidos. A nova assinatura relativa de fronteiras
   coincide com controles estáveis em 7/17 gaps e não coincide em 10/17, sem
   atribuir semântica aos offsets. Os 17 gaps já têm comparação alinhada de
   tokens, words, little-endian bytes, offsets, hashes de janela e grupos de
   correlação. A versão atual também registra contra os 355 controles 2/17
   limites brutos, 7/17 fronteiras relativas, 5/17 sequências completas de
   janela e 0/17 boundaries numéricos ou offsets canônicos absolutos; isso
   ainda não é um decoder. Usar os 355 layouts com composição
   pareada estável como amostras de controle e separar os 38 layouts com
   composições múltiplas pelas camadas de variação do relatório. Essa separação
    já foi materializada em 8 composições pelo `morph-ambiguity-summary` e
    cruzada, sem perdas, com as 239 linhas do byte-level por
    `morph_structural_layout`; os 17 gaps numéricos já foram distribuídos em
    10 layouts de 4 composições pelo `morph-ambiguity-numeric-summary`; o
    novo `morph-ambiguity-group-metadata` materializa esse recorte em 38 linhas,
    preservando as contagens de perfis, as fronteiras byte-level e os gaps
    numéricos por layout. A nova auditoria independente reagrupou diretamente o
    catálogo e confirmou 0 inconsistências de contagem/perfil; nos 38 ambíguos,
    26 têm mesh candidata estável com companion variável, 6 têm mesh candidata
    variável e 38 variam em alguma camada medida. O próximo recorte deve usar
    essa matriz apenas para selecionar grupos que tenham comparação
    independente adicional de mesh, SHPX, header ou timing; a matriz em si não
    é decoder. O relatório
   byte-level mostrou 213/239 igualdades exatas de bytes crus de comando com
   controles estáveis, 213/239 também por partição e 222/239 por tokens
   valor/categoria; os offsets canônicos completos continuam em 0/239. Os
   casos restantes devem ser comparados por limites e campos observados, sem
   promover nenhuma coincidência a decoder. Isso ainda é triagem estrutural.
   O novo `eagl-morph-name-state-audit` cruzou somente as strings `BS_*` dos
   morphs com os valores `SHAPENAME` dos companions: 2.360 nomes distintos,
   391/594 rows com estado e 0/594 matches. Essa ausência de recorrência textual
   é um controle negativo, não prova de ausência semântica; não criar binding a
   partir dela.
   O novo `eagl-morph-animation-name-negative-control` cruzou esses 2.360
   nomes completos, sem normalização, com 1.578 nomes de arquivo e 19 nomes de
   banco de animação: 0 coincidências exatas em 594/594 rows morph completos.
   Isso elimina somente uma hipótese lexical direta; a próxima investigação
   ainda precisa de timing/pose/rig independente e não pode transformar a
   ausência textual em prova de ausência semântica.
   A auditoria `eagl-morph-sidecar-boundary-audit` confirmou, nos 594 objetos
   com `__Morph:::` comprovado, a mesma separação física observada nos bancos
   de animação: 1 seção file-backed em `.ord`, 4 em `.orl`, relocation e tabela
   ELF em `.orl`, sem cruzamentos. Usar essa regularidade somente para limitar
   leituras futuras dos companions; ela não fornece a regra ausente para
   associar morph a mesh, pose, osso, peso ou frame.
   A comparação independente `eagl-morph-shpx-structure-audit` preservou o
   mesmo recorte por layout: 594/594 rows observados, 591/594 pacotes `.xsh`
   same-stem validados, 3 ausências explícitas e 0 rejeições. Houve 17 layouts
   com perfis de tabela SHPX variáveis, todos dentro dos 38 layouts ambíguos;
   isso é uma coincidência de cobertura estrutural que ainda não associa
   entrada SHPX a morph, mesh, material, UV ou estado de renderização.
   A auditoria seguinte `eagl-morph-shpx-geometry-correlation-audit` juntou
   esses dados à assinatura de geometria candidata em 393/393 layouts: 17
   ambos variáveis, 21 somente geometria variável e 355 estáveis. Usar esse
   resultado apenas para priorizar novas comparações independentes; ele não
   fornece pareamento por entrada nem decoder.
   A comparação seguinte de header também não encontrou variação: 393/393
   layouts mantêm um único perfil de header core, range e símbolo morph. O
   próximo recorte seguro deve procurar somente timing externo repetível; na
   ausência dessa evidência, manter os perfis como triagem e não criar
   bindings.
3. Usar os relatórios `apt-structure-fe.tsv` e `apt-structure-ig.tsv` como
   controle negativo para a investigação de animação: os 279 `.o` válidos não
   expõem `__Morph:::` nem o padrão nominal `*_morph`. A próxima comparação
    deve cruzar somente headers/constantes/tabelas SHPX e evidência externa de
    timing repetível. O novo profile report formou 80 perfis FE e 51 IG, com 568
    words alinhados comparados e 0 iguais; isso reforça somente a ausência de
    recorrência bruta nessa comparação, sem transformar ausência de símbolo ou
    diferença de words em decoder ou prova de ausência de animação.
    A auditoria `animation-bank-catalog-timestamp-audit` verificou `1.584/1.584`
    correspondências entre timestamp e data textual da própria linha `.axt`,
    com offsets locais `-420`/`-480`. Isso é metadata do catálogo e não timing
    de animação; manter `direct_timing_binding=not-proven` e procurar somente
    evidência externa repetível para o próximo recorte.
   O índice `morph-pose-bank-index.tsv` acrescenta um controle separado: os
   391 BIGs têm 391 linhas, mas somente o símbolo normalizado `PosePaletteBank`,
   1 registro ELF distinto (`table_section=4`, `symbol_index=3`,
   `symbol_section=1`, `value=0`, `size=4`, `Info=17`) e 0 igualdade nominal
   ou estrutural com os 19 símbolos de bancos `.axt`; isso não encerra a
   investigação nem autoriza interpretar o símbolo como pose ou palette.
   O comando `apt-animation-negative-control` materializa esse controle sem
   cruzar words como se fossem campos: no estado atual foram 0 coincidências
   de stem com nomes de bancos/arquivos de animação e 0 símbolos/candidatos
   `*_morph`.
   O novo `apt-cross-package-profile-audit` compara a chave exata de header APT,
   header `.const` e extensões: há 80 chaves FE, 51 IG, 27 compartilhadas, 53
   somente FE e 24 somente IG. Nenhuma chave compartilhada tem word alinhada
   igual ou símbolo morph. Isso é recorrência estrutural de perfil, não
   evidência de timing, pose, frame ou decoder.
4. Só depois avaliar mais famílias de material quando surgir evidência
   independente. A auditoria atual não encontrou uma segunda cadeia além de
   `tops`; não inventar bindings para `head`, `face`, `hair`, `bott`, `ball` ou
   `tats` a partir de nomes próximos.
5. Repetir a validação da cadeia candidata em mais amostras `tops` somente se
   houver uma comparação independente de mesh, SHPX e estado; não promovê-la a
   tabela global.
6. Atualizar este handoff e os quatro documentos principais após cada avanço.

## Registro da etapa atual — auditoria SHPX de entradas de imagem

Foi adicionada a auditoria `shpx-image-entry-audit` em
`fifa_pc/Runtime/ShpxImageEntryAudit.cs`, com comando CLI, self-test e
documentação. A execução real em `data/aptanims/player1.xsh` validou `46/46`
entradas PAL8 de `128x128`, tabela monotônica e tamanho declarado consistente,
mantendo hashes da origem, da tabela ordenada e do RGBA decodificado por entrada
em `fifa_pc/shpx-image-entry-audit.tsv`. SHA-256 da origem:
`4ADAFBC2B34EA033F0E51A883F89F151B634EAF3D7A52B279E8DC12939B5C3C7`.

O status `verified-image-entries` é somente evidência de entradas de imagem
decodificáveis e ordem estrutural. Timing, pose, rigging, skinning, animação,
material, UV, morph e GPU continuam `not-proven`/`not-claimed`; o projeto
permanece em andamento.

## Comandos mínimos de retomada

Executar em `D:\projeto zeta`:

```powershell
dotnet build -c Release fifa_pc\FifaStreetPc.csproj
dotnet run -c Release --no-build --project fifa_pc -- self-test
dotnet run -c Release --no-build --project fifa_pc -- shpx-image-entry-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/player1.xsh --report "D:\projeto zeta\fifa_pc\shpx-image-entry-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- project-readiness-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\project-readiness.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-inventory.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-payload-compare "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-payload-comparison.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-runtime-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-name-index "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-name-index.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-catalog-timestamp-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-catalog-timestamp-audit.tsv" --summary "D:\projeto zeta\fifa_pc\animation-bank-catalog-timestamp-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-catalog-compare "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --profile-report "D:\projeto zeta\fifa_pc\animation-bank-catalog-profiles.tsv" --pair-report "D:\projeto zeta\fifa_pc\animation-bank-catalog-pairs.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-elf-structure "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --profile-report "D:\projeto zeta\fifa_pc\animation-bank-elf-profiles.tsv" --section-report "D:\projeto zeta\fifa_pc\animation-bank-elf-sections.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-data-relocation-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-data-relocation-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-sidecar-boundary-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-sidecar-boundary-audit.tsv" --summary "D:\projeto zeta\fifa_pc\animation-bank-sidecar-boundary-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sidecar-boundary-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --report "D:\projeto zeta\fifa_pc\eagl-morph-sidecar-boundary-audit.tsv" --summary "D:\projeto zeta\fifa_pc\eagl-morph-sidecar-boundary-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-shpx-structure-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-shpx-geometry-correlation-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-shpx-geometry-correlation.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-shpx-geometry-correlation-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-elf-header-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-elf-header-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-elf-header-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-tail-compare "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-tail-comparison.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-material-batch-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- eagl-material-runtime-batch-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-material-runtime-batch.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-render-influence-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\candidate-render-influence-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-morph-relocation-profile-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\animation-bank-data-relocation-audit.tsv" "D:\projeto zeta\fifa_pc\animation-morph-relocation-profile-audit.tsv" --summary "D:\projeto zeta\fifa_pc\animation-morph-relocation-profile-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-geometry-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-candidate-geometry-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-attribute-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-candidate-attribute-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-projection-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops "D:\projeto zeta\fifa_pc\candidate-projection-previews-stage-1" tops0030.ord
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-object-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops0030.ord --commands
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-state-correspondence "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-candidate-state-correspondence.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-state-geometry-adjacency-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-state-geometry-adjacency.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-state-geometry-profile-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-state-geometry-profile-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-state-geometry-order-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-state-geometry-order-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-material-batch-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\material-batch-previews-stage-3"
dotnet run -c Release --no-build --project fifa_pc -- eagl-material-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops
dotnet run -c Release --no-build --project fifa_pc -- eagl-material-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops "D:\projeto zeta\fifa_pc\tops0030-material-preview.ppm"
dotnet run -c Release --no-build --project fifa_pc -- render-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\render-preview-candidate-stage.ppm" --clean-room-characters --candidate-material tops0030.big tops
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-input.tsv" --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-script-trace.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-demo-input.tsv" --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-script-demo-trace.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-demo-input.tsv" --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-script-demo-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-script-demo-replayed-stage.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --round-demo --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-round-demo-trace.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --round-demo --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-round-demo-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-round-demo-replayed.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-round-input.tsv" --round-demo --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-script-round-trace.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-round-input.tsv" --round-demo --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-script-round-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-script-round-replayed.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --gamebreaker-demo --clean-room-characters
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-input.tsv" --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-trace.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-input.tsv" --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-replayed.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --juggle-demo --clean-room-characters
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-input.tsv" --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-trace.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-input.tsv" --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-replayed.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-demo-input.tsv" --clean-room-characters --match-rule first-to-goals --target-goals 1
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 31 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-panna-input.tsv" --clean-room-characters --match-rule goals-or-panna --target-goals 9 --target-pannas 1
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\runtime-gameplay-trace.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --clean-room-characters
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --clean-room-characters --frame-output "D:\projeto zeta\fifa_pc\runtime-characters-demo.ppm"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --demo --opponent-ai
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --demo --opponent-ai --play-audio
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --candidate-material
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --candidate-material tops0040.big tops
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --play-audio
dotnet run -c Release --no-build --project fifa_pc -- run-windowed "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --candidate-material tops0040.big tops
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sibling-runtime-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-pose-bank-index "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\morph-pose-bank-index.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sibling-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" star0001.big
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sibling-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --geometry-report "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit.tsv" --geometry-summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit-summary.tsv" --geometry-correlation-report "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-correlation.tsv" --geometry-correlation-summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-correlation-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sibling-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --catalog "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-name-state-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\eagl-morph-name-state-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-animation-name-negative-control "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\animation-bank-name-index.tsv" "D:\projeto zeta\fifa_pc\eagl-morph-animation-name-negative-control.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sibling-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --ambiguity-report "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-byte-summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-byte-comparison-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-byte-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-numeric-summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-numeric-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-group-metadata "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-byte-comparison-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-group-metadata.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-independent-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-independent-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-independent-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-candidate-geometry-ambiguity-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-ambiguity-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-ambiguity-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- audio-event-named-decode-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\audio-named-event-decode.tsv"
dotnet run -c Release --no-build --project fifa_pc -- audio-sccl-channel-layout-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\audio-sccl-channel-layout-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- audio-abk-batch-decode-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\audio-abk-batch-decode.tsv" --max-attempts 8
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sibling-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --byte-report "D:\projeto zeta\fifa_pc\morph-sibling-byte-comparison-report.tsv" --numeric-gap-report "D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-report.tsv" --numeric-gap-correlation-report "D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-correlation-report.tsv"
dotnet run -c Release --no-build --project fifa_pc -- apt-structure-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh --report "D:\projeto zeta\fifa_pc\apt-structure-fe.tsv" --profile-report "D:\projeto zeta\fifa_pc\apt-structure-fe-profiles.tsv"
dotnet run -c Release --no-build --project fifa_pc -- apt-structure-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/ig.bh --report "D:\projeto zeta\fifa_pc\apt-structure-ig.tsv" --profile-report "D:\projeto zeta\fifa_pc\apt-structure-ig-profiles.tsv"
dotnet run -c Release --no-build --project fifa_pc -- apt-animation-negative-control "D:\projeto zeta\fifa_pc\apt-structure-fe.tsv" "D:\projeto zeta\fifa_pc\apt-structure-ig.tsv" "D:\projeto zeta\fifa_pc\animation-bank-name-index.tsv" --report "D:\projeto zeta\fifa_pc\apt-animation-negative-control.tsv"
dotnet run -c Release --no-build --project fifa_pc -- apt-cross-package-profile-audit "D:\projeto zeta\fifa_pc\apt-structure-fe-profiles.tsv" "D:\projeto zeta\fifa_pc\apt-structure-ig-profiles.tsv" "D:\projeto zeta\fifa_pc\apt-cross-package-profile-audit.tsv" --summary "D:\projeto zeta\fifa_pc\apt-cross-package-profile-audit-summary.tsv"
Get-FileHash -LiteralPath "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" -Algorithm SHA256
```

O projeto deve continuar sendo tratado como em andamento.

## Registro da etapa — referências estáticas de strings no XBE

Foi executada `tools/analysis/xbe_string_xref_audit.py` em modo somente
leitura sobre `default.xbe`, usando Capstone 5.0.9. O relatório registrou
1.929 strings literais de interesse e 944 referências x86 candidatas em 944
instruções de origem: 939 operandos imediatos e 5 operandos de memória
absoluta. Há referências textuais candidatas para áudio, `.xsh`, `.big`,
`D3DTexture` e `EAGLAnimationBuffer`.

Saídas: `fifa_pc/xbe-string-xref-audit.tsv` e
`fifa_pc/xbe-string-xref-summary.tsv`; SHA-256 do XBE lido:
`5fabb5b763be0cc8823e814ee343ac5be6950305330f46c5170e08eaadea910b`.
Status: `candidate-x86-reference-only`; sem execução do XBE, sem limites de
função confirmados e sem novos bindings. `original_semantics=not-claimed` e
readiness `NOT_READY` permanecem.

A validação final da etapa repetiu Release `0/0`, self-test `70/70`, smokes
relevantes, runtime headless e replays determinísticos com `matched=True`. Os
relatórios full-stack gerado/replay são iguais e a ISO permaneceu inalterada,
com SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Comando reprodutível:

```powershell
python tools\analysis\xbe_string_xref_audit.py "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\xbe-string-xref-audit.tsv" --summary "D:\projeto zeta\fifa_pc\xbe-string-xref-summary.tsv"
```

O mesmo comando pode receber `--call-context
"D:\projeto zeta\fifa_pc\xbe-string-xref-call-context.tsv"`. Esse relatório
localiza 6.251 relações de contexto em janela de 128 bytes, com 790/944
referências tendo uma chamada direta próxima e 1.141 destinos executáveis
distintos. Ele continua `candidate-local-x86-call-context`, sem limites de
função, sem semântica de subsistema e sem bindings.

O refinamento adicional `--call-clusters
"D:\projeto zeta\fifa_pc\xbe-string-xref-call-clusters.tsv"` materializou
1.141 agrupamentos por destino observado, 306 com pelo menos duas strings em
coocorrência local. Relatório SHA-256
`9A5F416ED66485B2D4E0D2087E3E400711627AA0352CCAB9E6194EC818AF4CF3`; resumo
SHA-256 `19A633D47CC2FA5CE30D800AE6C52ABD63CDF785546F31DC190A892E72A862BD`.
O teste Python passou `6/6`; a medida não recupera funções e não cria
bindings.
## Registro da etapa — janelas estáticas de destinos de renderização

Foi adicionada `tools/analysis/xbe_render_target_audit.py`, uma auditoria
somente leitura que seleciona os 64 destinos mais estáveis dos agrupamentos
de contexto de renderização, limita cada janela a 384 bytes e a encerra no
primeiro `ret` observado. O resultado foi `64/64` destinos disassemblados,
`63/64` terminados por `ret`, `7` com prólogo semelhante a função, `102`
chamadas diretas e `18` indiretas. Isso é evidência de janelas x86 limitadas
no XBE retail da mesma ISO; não executa o XBE, não recupera funções com
certeza e não cria binding de geometria, UV, material, skinning, rigging,
animação, morph ou comando GPU. `original_semantics=not-claimed` permanece.

Artefatos: `fifa_pc/xbe-render-target-audit.tsv` (SHA-256
`3FF54B4F5D5AF71147CC89F5400E27F2C0DEA562A7FD6657313543EEF99A403A`) e
`fifa_pc/xbe-render-target-summary.tsv` (SHA-256
`F2B41AC9545C50ADA19939C15E2E94496239A1FB9E4628EBA4DD7E062B89BB94`). O
XBE analisado continua com SHA-256
`5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B`.

`project-readiness.tsv` agora registra
`rendering-static-xbe-target-windows=verified-structural`; o gate principal
de renderização continua `not-proven`/reprovado. Também permanecem sem
promoção: assets em caminho candidato, áudio `partially-integrated`,
animação/deformação `structural-only` e gameplay apenas authored clean-room.

Validação final desta etapa: Release `0` avisos/erros, self-test `72/72`,
testes Python `13/13`, matriz completa `completed-authored-and-candidate-opt-in`,
smokes relevantes, runtime headless, replays determinísticos e captura
windowed já verificados; nenhum processo `FifaStreetPc` ficou ativo. A ISO
foi somente lida, tamanho `2013003776`, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Registro da etapa — slices estáticos de controle de fluxo de renderização

Foi adicionada `tools/analysis/xbe_render_cfg_audit.py`. A auditoria relê os
64 alvos já selecionados, decodifica uma janela máxima de 2048 bytes e segue
somente arestas de saltos diretos que caem em instruções da mesma janela.
Chamadas diretas são apenas contadas, não seguidas; transferências indiretas e
alvos fora da janela permanecem não resolvidos. O resultado foi `64/64`
slices com CFG alcançável, `62/64` com `ret` alcançável e `441` blocos
básicos, com `127` chamadas diretas e `22` transferências indiretas observadas.

Isso é controle de fluxo estático limitado, não recuperação completa de
funções e não execução do XBE. Não atribui semântica a geometria, materiais,
UVs, skinning, rigging, animação, morphs ou comandos GPU; o relatório marca
`original_semantics=not-claimed` e `no-function-boundary-claim`.

Artefatos: `fifa_pc/xbe-render-cfg-audit.tsv` (SHA-256
`5000D88598D42E2238279B47AA47364B830776ED1C2BC8DABE2B6874C2CEAECA`) e
`fifa_pc/xbe-render-cfg-summary.tsv` (SHA-256
`EC0FE95F012EE104C19065F41C806ABFD974DBD34E25876DA545C0CB99DABA5B`).
`project-readiness.tsv` agora registra
`rendering-static-xbe-cfg-slices=verified-structural`; o gate principal de
renderização permanece `not-proven` porque ainda falta associar estado de
renderização a geometria e validar independentemente UV/material.

Validação: Release `0/0`, self-test `72/72`, testes Python `14/14`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed passaram. A ISO continua somente leitura,
com tamanho `2013003776`, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Registro da etapa — correlação de áudio sem limite efetivo de candidatos

Foi repetida a auditoria `audio-event-payload-wave-correlation-exhaustive-audit`
com `--max-sound-candidates 100000`, valor acima de todos os índices expostos
pelos headers observados. A execução percorreu `368` candidatos de som em `16`
bancos; `239` decodificaram. Os `17/17` candidatos EVT também decodificaram,
mas houve `0` pares PCM exatos e `0` hashes de evento com correspondência.

Esse resultado é uma negativa estrutural de correlação de payload dentro do
conjunto enumerado; não identifica evento, sample, índice, ABKC ou semântica de
gameplay. `direct_selection_binding=not-proven` e
`original_semantics=not-claimed` permanecem. O relatório é
`fifa_pc/audio-event-payload-wave-correlation-all.tsv` (SHA-256
`BDE7CF7E58E09E17D2BBB8932E413249D51264424E5A1C32E8B5E30D404E345E`) e o
resumo é `fifa_pc/audio-event-payload-wave-correlation-all-summary.tsv` (SHA-256
`CC767B8DCE09E74CDB78FCA6898A9C4960DBC386DBC82B26F87A5C389DD084FD`).

O readiness registra essa negativa em `audio_payload_all_*`, mas o gate
principal de áudio continua `partially-integrated`; as rotas authored
clean-room continuam separadas de EVT/ABKC/sample original.

Validação: Release `0/0`, self-test `72/72`, testes Python `14/14`, matriz
completa, smokes, runtime headless e replays determinísticos aprovados. A ISO
permanece somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Registro da etapa — grafo estático limitado de chamadas do renderizador

Foi adicionada `tools/analysis/xbe_render_call_graph_audit.py`. A análise
parte dos `87` alvos diretos já observados e segue somente chamadas imediatas
diretas até profundidade máxima `2`, com janelas de `256` bytes e limite de
`512` nós. O resultado comprovado foi `235/235` nós executáveis
disassemblados, `326` arestas diretas únicas, `200` nós com `ret` alcançável e
`0` truncamentos pelo limite de nós.

Esse grafo é interprocedural apenas no sentido estrutural e limitado: não
recupera limites de função, não executa o XBE, não resolve indireções e não
atribui materiais, UVs, skinning, rigging, animação, morphs ou comandos GPU.
`original_semantics=not-claimed` permanece. O readiness agora registra
`rendering-static-xbe-call-graph=verified-structural`; o gate principal de
renderização continua `not-proven`.

Artefatos: `fifa_pc/xbe-render-call-graph-nodes.tsv` (SHA-256
`340DFC27050E6A9E29018A9C946C90895A8E98780442FF6BB722D9E08D3722B0`),
`fifa_pc/xbe-render-call-graph-edges.tsv` (SHA-256
`F7B7291B616470BEB41A5491B4F798423775BE398FD70F796CE7E9B08CC8073C`) e
`fifa_pc/xbe-render-call-graph-summary.tsv` (SHA-256
`16671B4DF9A4EE10D9DBE1B7833372751ACA4D7B8916BBBDD172C6AAAF4AA4E8`).

Validação: Release `0/0`, self-test `72/72`, testes Python `16/16`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed passaram. Assets, áudio, gameplay original
e animação/deformação mantêm respectivamente os estados candidato, parcial,
clean-room e estrutural. A ISO continua somente leitura, com
`2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Atualização desta etapa — controle negativo de população de estado EAGL — 2026-10-07

O caminho bruto same-trunk agora registra, sem semântica promovida, os slots
observados nos streams de `__Model`. Resultado real: `2364/2364` streams
completos, `2364` linhas com slots, `2362` sequências `0..4` completas, `750`
linhas com coocorrência `SHAPENAME`+slots e `0` linhas com sobreposição
`SHAPENAME`+`SetPrimitiveType`.

Os `750` casos são somente observação estrutural: não associam estado a mesh,
UV, material ou GPU. Não foram inventados bindings de normal, peso, skinning,
rigging, animação ou morph. O readiness ganhou o subgate
`rendering-original-state-population-negative-control=verified-structural-negative`,
sem alterar `rendering=not-proven` ou `NOT_READY`.

Relatórios e hashes: `fifa_pc/eagl-original-render-chain.tsv` —
`629C6D7A836CAA46A84E09A4EEA3DE8F3AC11ADE871219B7E205DF710C07B66C`;
`fifa_pc/eagl-original-render-chain-summary.tsv` —
`16FAF6D2F477A621445015A87C05E04896F20F3CA5000B6634E9CBB15D647DF7`;
`fifa_pc/project-readiness.tsv` —
`F4DBDEC11A306181CB2943AC1077A8B930CA6401EE40961281AC67D134F354DD`.

Release `0/0`, self-test `74/74`, Python `32/32`, matriz completa, smokes,
headless, windowed e replays determinísticos passaram. DLL Release:
`DA0C35BEFF7C600F1E9CF9277EA854FBC6CBE5F8E3745C26ACB6DE50AE12E445`. ISO
somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.41. Procurar evidência original independente que sobreviva aos controles
      negativos para estado→geometria/UV/material, sem promover a população de
      slots ou os `216` layouts candidate completos.
1. Manter `NOT_READY`, repetir a bateria final após mudanças e não iniciar
   code review nem marcar o projeto como concluído.

## Etapa 0.41 concluída — população de estado original como controle negativo

Foi criado `tools/analysis/eagl_original_state_population_control.py` e ele foi
integrado à matriz. No relatório bruto same-trunk: `883` objetos, `2364`
primitivas, `752` linhas SHAPENAME, `1612` SetPrimitiveType, `0`
sobreposições, `0` pares de mesmo índice, `777` adjacentes, `1904` na mesma
camada, `0` hashes de comando iguais e `0` formas de fronteira iguais.

O resultado é somente populacional/estrutural. Não constitui associação
estado→geometria/UV/material e não atribui semântica a skinning, rigging,
animação, morph ou GPU. `rendering-original-state-population-control` é
`verified-structural-negative`; renderização principal permanece `not-proven`
e o readiness permanece `NOT_READY`.

Artefatos: `fifa_pc/eagl-original-state-population-control.tsv`
(`1DB67C35A1AFF669606666E86802E76117E160D05562C8B0E2F3B9CE487BE83F`) e
`fifa_pc/eagl-original-state-population-control-summary.tsv`
(`6D47AFC8F8826B849985D618645DD59924C1D35D14FEFA879B38B75B58C8E7E4`).
Release, self-test `74/74`, Python `33/33`, matriz completa, smokes, runtime
headless/windowed e replays determinísticos passaram. A ISO está inalterada,
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.42. Buscar evidência original independente adicional para
estado→geometria/UV/material. Se a evidência não existir, preservar a lacuna
como `not-proven` e continuar os subsistemas ainda não decodificados sem
inventar bindings. Repetir a bateria final após qualquer mudança; não concluir
o projeto e não iniciar code review.

## Etapa 0.42 concluída — estrutura bruta original de `__Model`/`__VB`

O audit same-trunk foi implementado e executado sem abrir escrita na ISO. Ele
validou `883` objetos, `883` símbolos `__Model`, `882` `__BBOX`, `2364` `__RM`
e `11820/11820` registros `__VB` por estrutura delimitada, stride,
comprimento e contagem observada. `882` objetos possuem bounds e `883` possuem
streams de vértices estruturalmente validados.

O resultado é caminho comprovado de inventário estrutural bruto. Ele não
associa primitivas a streams e não decodifica geometria, UV, material,
skinning, rigging, animação, morph ou GPU. Portanto
`vertex_record_control_status=verified-bounded-original-vertex-record-structure`,
mas `rendering=not-proven`, `original_semantics=not-claimed` e readiness
`NOT_READY` permanecem inalterados.

Artefatos: `fifa_pc/eagl-original-render-chain.tsv`
(`629C6D7A836CAA46A84E09A4EEA3DE8F3AC11ADE871219B7E205DF710C07B66C`),
`fifa_pc/eagl-original-render-chain-summary.tsv`
(`36D8765FB5E3DC1E00ABDF59C8477384575F5D912A1B737F2BB1A971BCD4C8CF`) e
`fifa_pc/project-readiness.tsv`
(`93065B27B628D955F4261B062F99C347B6B32269785F99AAD70F15BEAEA4FE9B`).
DLL Release:
`0BF974EA15DD900F6D57AE1FEA14BAD7095B36A552BFE398996D686EEED748CD`.

Validação final: Release `0/0`, self-test `74/74`, Python `33/33`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime headless
e windowed, replays determinísticos e nenhuma instância residual. ISO
preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.43. Procurar associação original independente entre registros `__VB` e
primitivas/streams, mantendo a distinção entre evidência comprovada, caminho
candidato e semântica não decodificada. Se a prova não existir, preservar
`not-proven` e avançar nos subsistemas restantes sem inventar bindings.
1. Repetir a bateria final após cada alteração; não concluir o projeto e não
iniciar code review.

## Etapa 0.43 concluída — associação estrutural opaca `__VB`↔fetch

Foi criado `eagl-original-vertex-record-association-audit`. No caminho bruto
original, ele compara a sequência de cinco strides observada nos fetch slots
com a sequência única de cinco categorias/ordinais consecutivos nas chaves
textuais dos registros `__VB`. Resultado real: `2364` primitivas, `2364` com
registro ordinal, `2362` sequências opacas únicas, `2362` layouts completos e
`0` pacotes/objetos rejeitados.

O resultado é estrutural e opaco. Não é binding de atributo e não atribui
geometria, UV, material, skinning, rigging, animação, morph ou GPU;
`original_semantics=not-claimed`. O subgate
`rendering-original-opaque-vertex-record-association` é
`verified-structural`, enquanto renderização principal continua `not-proven` e
readiness `NOT_READY`.

Artefatos: `fifa_pc/eagl-original-vertex-record-association.tsv`
(`162D94FA23DB73C469392FFD5AA037FA2D029E7E06D47B32C2692004BA7B9ECD`),
`fifa_pc/eagl-original-vertex-record-association-summary.tsv`
(`257C8F331CB6D9B3E9BB99E18F1DAEA99298779A8B035E78126B14DB26D118CE`) e
`fifa_pc/project-readiness.tsv`
(`5C6468CB5D769B00EB88F0B3213D7BA293CEB8949815A5E5F3951B0526B8EBF9`). DLL
Release: `030372D0DBDB657E53D09871B18B46B021F469F6F6BA112B8281C14E1A7C3FD1`.

Validação final: Release `0/0`, self-test `75/75`, Python `33/33`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime headless
e windowed, replays determinísticos e nenhuma instância residual. ISO
preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.44. Aplicar controles independentes contra a associação opaca e procurar
evidência original de geometria/UV/material fora de nomes, strides e comandos
não decodificados. Manter a distinção entre evidência, candidato e semântica;
não promover bindings sem prova adicional.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.46 concluída — controle independente dos payloads originais `__VB`

O controle independente `tools/analysis/eagl_original_vertex_payload_control.py`
reparseou `fifa_pc/eagl-original-vertex-payload.tsv` sem reutilizar a lógica
.NET. O resultado foi `883/883` objetos, `883/883` assinaturas numéricas
coerentes, `883/883` metadados de ranges válidos e `883/883` statuses de entrada
válidos, com status
`verified-independent-original-numeric-payload-control`.

O resultado confirma somente coerência estrutural/negativa independente. Não
decodifica nem atribui posição, geometria, UV, material, skinning, rigging,
animação, morph ou GPU. `original_semantics=not-claimed`, renderização
principal continua `not-proven` e o readiness continua `NOT_READY`.

Hashes: controle
`0DD9A6EB1E5EEC72D309CCACDC7E7C69D632CF8E02722EB4EB77E14D4052D013`, resumo
`6E817B35190ACBC726EC70D09C2E8516D2002AF3C97643BC188CAA0EF8D60A04`, relatório
original `EC69F3A7D0A3F56FB0178AE5A39B68258FFE749F57F5C0BEDCB9A27E93C99E3C`,
resumo original `9487A2A19F9F9FC8E9403C068FBA54A895289695C3BF2055D6AEDB0FF4D63C21`,
readiness `A5731AC06117F6BE6B98BCCC5259519E4697F51FBC0215672FEB5349D5E1C2B0` e
DLL Release `A4A05B3728A9D724F7B69CAB6E17CB42756BA2FF4EA2DA70E7D00DE5B480C55F`.

Validação: Release `0/0`, self-test `76/76`, Python `35/35`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A ISO não foi escrita:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.47. Buscar evidência original independente de geometria, UV, material, áudio
ou animação ainda não decodificada, mantendo `not-proven` e sem inventar
bindings.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.45 concluída — estrutura numérica dos payloads originais `__VB`

Foi implementada `eagl-original-vertex-payload-audit` no caminho bruto
original. Ela valida ranges dos payloads `__VB`, mede perfis numéricos finitos,
registra observações `float2` sem chamá-las de UV, mede floats de três
componentes e contenção nos bounds declarados, e controla sobreposição de
ranges. Resultado: `388/388` pacotes, `883/883` objetos, `11820` streams
validados, perfis finitos em `883` objetos, contenção em `882`, ranges
distintos em `883`, zero sobreposições e zero rejeições.

É evidência estrutural/numericamente comprovada, não binding. Nenhum campo foi
promovido a posição, geometria, UV, material, skinning, rigging, animação,
morph ou GPU; `original_semantics=not-claimed`. O subgate
`rendering-original-numeric-vertex-payload-structure` é
`verified-structural`, enquanto renderização principal continua `not-proven` e
readiness `NOT_READY`.

Artefatos: `fifa_pc/eagl-original-vertex-payload.tsv`
(`EC69F3A7D0A3F56FB0178AE5A39B68258FFE749F57F5C0BEDCB9A27E93C99E3C`), resumo
(`9487A2A19F9F9FC8E9403C068FBA54A895289695C3BF2055D6AEDB0FF4D63C21`) e
readiness (`819BB6F05291BC2B8B3AFCD9961851B79EE3A586FE4A682344992E7D3C4949B0`).
DLL Release: `BF1B3B40F4382A7CB51BFADD89C4C277133AF2FE5E81D06B46D8FD2AE1234665`.

Validação final: Release `0/0`, self-test `76/76`, Python `34/34`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram. ISO preservada: `2013003776` bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.46. Buscar evidência original independente de geometria, UV ou material além
dos perfis numéricos e ranges; na ausência de prova, manter `not-proven` e não
inventar bindings. Não concluir o projeto nem iniciar code review.

## Etapa 0.44 concluída — controle independente da associação opaca

`tools/analysis/eagl_original_vertex_record_association_control.py` reparseou
o TSV original sem reutilizar a implementação .NET. Ele reproduziu `2362/2362`
sequências opacas exatas em `2364` linhas e `883` objetos; o controle por ordem
de strides invertida e ordinal deslocado resultou em `0/0`.

Isso confirma a estabilidade estrutural da observação, não um binding
semântico. Geometria, UV, material, skinning, rigging, animação, morph e GPU
continuam não decodificados; `rendering=not-proven`, `original_semantics` é
`not-claimed` e readiness é `NOT_READY`.

Artefatos: `fifa_pc/eagl-original-vertex-record-association-control.tsv`
(`3D5F57E04D65C4956E5F26AB994ABD5DA1C2ED79DEE1D905C4BC5DE62F75513A`) e
`fifa_pc/eagl-original-vertex-record-association-control-summary.tsv`
(`26E1DABB216D91957886D8C97ED3ADF5DC5BA448D8D36E260CBFA05C60FF9765`).
Readiness: `EC399C25E4E12B19618F3FCA02C29ECC4F9E8532CF8AA0547DFA21F83003A699`.

Validação final: Release `0/0`, self-test `75/75`, Python `34/34`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime headless
e windowed, replays determinísticos e nenhuma instância residual. DLL Release:
`56D7443171E80962DBA901970AE8764A59BBCAD10456B76B4904E9387B304A04`. ISO
preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.45. Buscar evidência original independente fora das chaves textuais,
strides e comandos opacos, sobretudo geometria/UV/material. Na ausência de
prova, manter `not-proven` e não inventar bindings.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.47 concluída — inventário literal dos estados EAGL originais

`eagl-original-state-token-audit` catalogou os estados dos `2364/2364`
primitivos originais. Foram observadas `752` linhas com `SHAPENAME` e clamps,
`1612` com `SetPrimitiveType` e `9` nomes literais de atribuição. O controle
independente reparseou `2364/2364` linhas e encontrou zero tokens literais de
textura, material, coordenada UV, atributo de vértice ou deformação.

É um controle negativo do vocabulário textual, não prova de ausência nos bytes
opacos. Não foram atribuídos significados de geometria, UV, material, skinning,
rigging, animação, morph ou GPU; `original_semantics=not-claimed`, renderização
principal segue `not-proven` e readiness segue `NOT_READY`.

Hashes: auditoria
`28FB82B938346D64FE88EB7A8ECBC99463A84EC603740A7F184228786610B9BF`, resumo
`ADE5409FE2F120B76794EB6DCADD98BAF8A2C2719A97C3348C04BD68C127AACB`, controle
`680E54D4878C3CA561237554F30CB0C5AA5C0917F5A39B9F6F8888412DA7FBE9`, resumo
do controle `099BFFB9C1AEC88F31ECFF0EC048C4D7E760CDBC471641B92ED76F8213E4028D`,
readiness `D9CCE7315FBBDBAC5FE043C632464B7C3DCFCC2B9F14AA65E8AFCF2112C6A0A0` e
DLL Release `582EB7872C8EC8E983EFAF9E7249DE077C98F0818000D6E95BF4873350EB594B`.

Validação: Release `0/0`, self-test `77/77`, Python `36/36`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A ISO não foi escrita:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.48. Procurar evidência original fora do vocabulário textual dos estados,
especialmente ligação verificável entre comandos opacos, payloads e
imagem/atributos, mantendo `not-proven` e sem inventar bindings.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.48 concluída — junção independente por objeto entre renderização e payload `__VB`

O novo controle `eagl_original_render_payload_object_join.py` reparseou as
duas fontes TSV e fez a junção somente pela identidade bruta de objeto.
Resultado: `883/883` objetos unidos, nenhum objeto exclusivo de uma fonte,
`2364` primitivas, `11820` streams numéricos e `883/883` linhas com identidade,
status e contadores estruturais válidos. Chaves invertidas e deslocadas deram
`0/0`.

É evidência independente no nível de objeto, não associação de primitiva com
stream e não binding de geometria, posição, UV, material, atributo, skinning,
rigging, animação, morph ou GPU. Status
`verified-independent-original-render-payload-object-join`,
`original_semantics=not-claimed`, renderização principal `not-proven` e
readiness `NOT_READY`.

Hashes: relatório
`D33B945D730077098359A129AE5E52EF0AFEA661EDF33EF4C910AF343D170985`, resumo
`6B95F71A207E2E0DEEFDE91A2E8E2C6F0216444B9F434110C2F772BAFA673DEE`, readiness
`0DE5599D135AEEEA38EA42840012ECA0E63CDAAF0AF08059BE793094A1FB08EA`, DLL
Release `8C2381ED22645B458B2EC4C273270D09A31C94AFEB7FBDF44B82C3F4F5F1A2CC`.

Validação final: Release `0/0`, self-test `77/77`, Python `37/37`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram. ISO preservada: `2013003776` bytes,
`Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.49. Buscar associação original independente entre primitivas, streams e
atributos ou comandos/payloads, mantendo `not-proven` e sem inventar bindings.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.49 concluída — topologia estrutural primitiva/stream `__VB`

Foi implementado o audit C# `eagl-original-primitive-stream-topology-audit`
com relatório detalhado e o controle independente Python
`eagl_original_primitive_stream_topology_control.py`. Resultado comprovado:
`2364` primitivas, `11820` streams, `11820/11820` ordinais correspondentes,
`11814/11820` strides correspondentes, `11820/11820` ranges de payload válidos,
`2362` primitivas completas, `883/883` objetos com ranges não sobrepostos e
`0` matches no controle de slots invertidos.

O controle de ordinais deslocados encontrou `1479` linhas. Isso registra uma
ambiguidade real de layouts repetidos; não é evidência de associação única e
não foi promovida a binding. Status do audit:
`verified-bounded-original-primitive-stream-topology`; controle:
`verified-independent-original-primitive-stream-topology-control`;
`original_semantics=not-claimed`.

Escopo mantido: topologia estrutural de fetch slot, categoria/ordinal opacos,
stride e range. Sem posição, geometria, UV, material, atributo de vértice,
skinning, rigging, animação, morph ou comando GPU. O gate de renderização
original continua `not-proven` e o readiness continua `NOT_READY`.

Hashes: relatório
`6A9CF9A6B92939552050C499261876724191D518DB1470E79CD7338F67A1BEE0`, resumo
`C10159DE7C3A7CC38EE8625E91D7D05679187092E534E51840B256899B0D4612`, controle
`4FFFE8E3C76D7C9389F44AD60C0F43EBBDC114D339C531012AC1B84BC837FA97`, resumo
do controle `777AA578F901AA94FEE0EC3307300C4A5DE9F6BF4E66F73BCFB9D675853479B9`,
readiness `7818A02C248A03351C6902C96EC8324BC755AA92FE70C35D1C7F4D36F051709A`
e DLL Release
`C873228A606ED79CB02F79B0258748220FE22EAEB1ECE2E14C813A79E143D294`.

Validação: Release `0/0`, self-test `78/78`, testes Python `38/38`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.50. Discriminar os layouts/ordinais repetidos com evidência independente de
comando/payload; só depois avaliar atributos/geométrica/UV. Até lá, manter
`not-proven` e não inventar bindings.

## Etapa 0.50 concluída — controle negativo de comandos/payloads opacos

Foi implementado o audit C# `eagl-original-primitive-stream-opaque-discrimination-audit`
e o controle independente `eagl_original_primitive_stream_opaque_discrimination_control.py`.
O procedimento reparseia as palavras brutas dos comandos de fetch no mesmo
objeto EAGL e compara apenas com os registros/descritores `__VB` validados e
uma janela limitada do payload. A operação permanece opaca e estrutural.

Evidência comprovada: `388/388` pacotes, `883/883` objetos, `2364` primitivas,
`2362` fetches completos, `10240` candidatos ordinais, `10220` candidatos por
stride, `0` matches por metadata, `0` matches por prefixo de payload, `0`
discriminações únicas e `1479` candidatos de stride deslocado. O controle
independente confirmou `2364/2364` linhas coerentes. O resultado negativo não
discrimina os layouts repetidos; portanto não foi promovido a binding.

Status do audit: `verified-bounded-original-primitive-stream-opaque-discrimination`;
controle: `verified-independent-original-primitive-stream-opaque-discrimination-control`;
`original_semantics=not-claimed`. Não foram inventados bindings de posição,
geometria, UV, material, atributo de vértice, skinning, rigging, animação,
morph ou GPU. A readiness geral continua `NOT_READY` e a renderização original
continua `not-proven`.

Hashes: relatório
`845C2A9AA02ED9FBB6F2CC3DD73E143A7822735E19F3E1A65E55BC703F68007B`, resumo
`B857C8D31DFE2CF42A63E0AEA79AFA4697F0D1D86BC430AD8018757C2968BBC9`, controle
`00C438AB565BC6774E2E22F3929FB9F7F6B3B611C0FF59351E37F2766B1ACB5A`, resumo
do controle `496ED3A7CB97A1E4EB5E05F88102636FB5D3A86EF1002C05F399BA9CD2FFE1A8`,
readiness `C056B78C45EB75DAF0CEB31320F10E6FFAC852C6FC5301595E7CAD92748DF43D`
e DLL Release `8209763D4DD2AD6328309FCC5440495CCC04E0B3ED37603296DF9B4A5E85A959`.

Validação: Release `0/0`, self-test `79/79`, testes Python `39/39`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.51. Obter identidade independente de comando/payload ou semântica de
registro decodificada para discriminar os candidatos repetidos; manter
`not-proven` e não inventar bindings.

## Etapa 1.08 — intervalo estrutural do catálogo no tail de `__AnimationBank` — 2026-10-08

Foi implementado `fifa_pc/Runtime/AnimationBankCatalogTailRangeAudit.cs`, com
o comando `animation-bank-catalog-tail-range-audit`, e o controle independente
`tools/analysis/animation_bank_catalog_tail_range_control.py`. A leitura ficou
restrita à ISO em modo somente leitura, ao catálogo `.axt` e aos payloads ELF
`.ord/.orl`; nenhum XBE foi executado ou recompilado.

Resultado reproduzível: 19/19 bancos e 1.584/1.584 entradas do catálogo
validados. O tail de 32 bytes, a contagem `2x` do catálogo, o intervalo dentro
de `.data`, o alinhamento, o comprimento `8x` e a contagem de relocations
`2x` passaram em 19/19 bancos; linhas sem relocations no intervalo: 0.
Status da fonte: `verified-structural-animation-bank-tail-range`. Controle:
`verified-independent-animation-bank-catalog-tail-range-control`.

Isto é apenas uma regularidade estrutural repetível. Não foi atribuído
significado de record, frame, pose, rig, timing, skinning, morph ou seleção de
animação: `candidate_record_binding=not-proven` e
`original_semantics=not-claimed`. A linha estrutural foi adicionada ao
readiness, mas não promoveu nenhum gate: readiness continua `NOT_READY`, com
rendering, assets, áudio original e deformação não provados; gameplay continua
`clean-room-integrated` apenas no escopo authored.

Validação da etapa: Release `0` warnings/`0` errors, self-test `94/94`, matriz
`completed-authored-and-candidate-opt-in` com código 0, 10 cenários, 777
frames, 54 eventos de áudio e 1.554 amostras de animação; smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.

Artefatos principais: `AnimationBankCatalogTailRangeAudit.cs` SHA-256
`794C29BB63E9958EDCDEBB3190E7E46E7554A945BE2B6FF15887C4D7BCD05CF4`, controle
Python `26219EC1EBC237F6F62BB0658A71D1BDBC6BA007D4D08FEF4E17A0867798125D`,
relatório fonte `DBA27512014C574DAA09899DB64EA41D70473D39101F67BB529261C9B72FDE9D`,
resumo fonte `B309D4B28D8379A1A3048AFDCC93E5F4BBFEB76D6E9B3D24E8B75182DEF74561`,
relatório independente `B6D1E4AECDD99593B21FF5C0D6CF8FAE4FAEC8719DBD6094BC9CA23A06613E6B`,
resumo independente `717BB90B128E80BE187775B520F8021BAEC1A13C6AFAF92B5C7E8EADC6BCD46B`.

## Próxima etapa recomendada

Prosseguir em três frentes independentes: estado original → draw/UV/material;
`EVT→ABKC→sample→playback`; e timing/pose/rig/deformação. Qualquer resultado
negativo deve permanecer controle negativo. Não declarar conclusão, não
inventar bindings e não iniciar code review.

## Etapa 1.09 — relocations dos pares no tail de `__AnimationBank` — 2026-10-08

Foi implementado `fifa_pc/Runtime/AnimationBankTailPairRelocationAudit.cs`, o
comando `animation-bank-tail-pair-relocation-audit` e o controle independente
`tools/analysis/animation_bank_tail_pair_relocation_control.py`. A análise usa
somente a ISO em modo somente leitura, o `.data` delimitado, o tail dos 19
ELFs de animação e os catálogos `.axt/.ord/.orl`; nenhum XBE foi executado ou
recompilado.

O resultado reproduzível foi: 19/19 bancos, 1.584/1.584 linhas de pares,
1.584/1.584 pares com exatamente duas relocations nos offsets esperados,
zero linhas com multiplicidade inesperada, 1.584/1.584 primeiras e segundas
palavras dentro de `.data`, zero palavras em símbolos conhecidos, 1.584 pares
raw distintos e 19/19 layouts completos. Cada banco produziu uma assinatura
estrutural de pares distinta; o relatório detalhado contém 1.584 linhas.
Status da fonte: `verified-structural-animation-bank-tail-pair-relocations`.
Controle: `verified-independent-animation-bank-tail-pair-relocation-control`.

Isso continua sendo evidência estrutural, não um decoder: não foi provada a
associação ordinal → nome do catálogo, nem o significado de record, frame,
pose, rig, timing, skinning, morph ou playback. Os words permanecem ponteiros/
addends opacos: `candidate_record_binding=not-proven` e
`original_semantics=not-claimed`. A linha entrou no readiness como
`verified-structural=true`, mas não promoveu nenhum gate; readiness continua
`NOT_READY`, com assets, rendering, áudio original e deformação semântica não
provados, e gameplay aprovado apenas no escopo clean-room authored.

Validação da etapa: build Release com `0` warnings e `0` errors, self-test
`95/95`, matriz `completed-authored-and-candidate-opt-in` com código 0, 10
cenários, 777 frames, 54 eventos de áudio e 1.554 amostras de animação;
smokes relevantes, runtime headless/windowed e replays determinísticos
passaram. ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Hashes principais: `AnimationBankTailPairRelocationAudit.cs`
`E4D8A8D0847FDD81C5FB1C008F7A3D797F275BCEFBD09B565068B3E52E60A28A`, controle
Python `E857E5CAC1A784F72688FD60BFE57F7DBA64DF330AEEF89F0426A610736354FB`,
relatório fonte `998C02C9D3915A8BD3B561E777A5F1BE2C7EF92CF397AC925B9781E2DB9163E8`,
resumo fonte `959289314FD652787CDF05B7751E553ED58F1EDA75B573A33AA3554F57A28510`,
detalhe `3A845F2D8F66AC6A04B088C7FD7F25C01423ACD4F369D79DDD475A24DFAA73E7`,
controle `348201F46B9E75C2566FE0C73565B3A317100A8B5B6364C87C5B2992C761CDB3` e
resumo independente `41C667AB04F7A0C3CB10E8C1B2CA6C20F8C0A1BB11A1FB5FAFBBF49F7BA82242`.

## Próxima etapa recomendada

Usar a estrutura de target-prefix/relocations apenas se surgir um join
independente e demonstrável com os nomes do catálogo; caso contrário, seguir
nas três frentes: estado original → draw/UV/material,
`EVT→ABKC→sample→playback` e timing/pose/rig/deformação. Manter resultados
negativos como controles, não declarar conclusão, não inventar bindings e não
iniciar code review.

## Etapa 1.10 — janelas de relocation dos targets do tail — 2026-10-08

Foi implementado o audit `fifa_pc/Runtime/AnimationBankTailPairTargetChainAudit.cs`
e o controle independente `tools/analysis/animation_bank_tail_pair_target_chain_control.py`.
Ele reabre cada par raw do tail de `__AnimationBank` e audita uma janela
delimitada de 32 bytes em cada target, preservando multiplicidade, símbolos e
words como dados estruturais. A ISO, `.data`, ELFs e catálogos continuam em
modo somente leitura; nenhum XBE foi executado ou recompilado.

Resultado: 19/19 bancos, 1.584/1.584 linhas e 1.584/1.584 pares com ambas as
janelas não vazias; status fonte
`verified-structural-animation-bank-tail-pair-target-chain` e controle
`verified-independent-animation-bank-tail-pair-target-chain-control`. A
multiplicidade observada no detalhe foi first `2:1, 3:17, 4:1554, 5:12` e
second `3:328, 4:1237, 6:19`. Não houve nenhuma linha com exatamente uma
relocation em cada janela, nenhuma resolução de segundo nível e nenhum match
com símbolo conhecido (`0` em todos esses contadores). Portanto a hipótese de
um único hop não se confirmou; `candidate_record_binding=not-proven` e
`original_semantics=not-claimed` permanecem inalterados.

A nova linha do readiness é `verified-structural=true` apenas para essa
regularidade de janelas e não promoveu nenhum gate: readiness segue
`NOT_READY`, com assets, rendering, áudio original e deformação semântica não
provados; gameplay segue clean-room authored.

Validação da etapa: Release `0` warnings/`0` errors, self-test `96/96`, matriz
`completed-authored-and-candidate-opt-in` com código 0, 10 cenários, 777
frames, 54 eventos de áudio e 1.554 amostras de animação; smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: tratar a multiplicidade das janelas como controle estrutural e
buscar evidência independente de record/frame/pose/rig/timing, ou retornar às
frentes estado original → draw/UV/material e `EVT→ABKC→sample→playback`.
Não inventar bindings, não declarar conclusão, não executar/recompilar XBE e
não iniciar code review.

## Etapa 1.11 — identidade SHA-256 do command stream candidato — 2026-10-08

Foi ampliado `fifa_pc/Runtime/EaglCandidateFetchAssociationAudit.cs` para
preservar, em cada linha candidata reconciliada, o SHA-256 dos bytes brutos do
command stream obtido por `EaglModelReader.GetRawCommandStreamBytes`. O controle
independente `tools/analysis/eagl_original_candidate_primitive_fetch_control.py`
reparseia as duas fontes por chave de primitiva e exige igualdade do hash,
além dos controles anteriores de contagem, completude, slots e estado.

O resultado foi `218/218` hashes de command stream iguais em linhas unidas;
`218/218` streams completos, `218/218` contagens de comando iguais,
`216/216` sequências de cinco slots materializadas e `2` linhas candidatas
explicitamente não materializadas. O status é
`verified-independent-original-candidate-primitive-fetch-control`, com
`original_semantics=not-claimed`. É identidade estrutural de bytes: não prova
state→draw, posição, UV, material, sampling, GPU, skinning, rigging, animação,
morph ou qualquer binding semântico original.

Arquivos de implementação e integração: `EaglCandidateFetchAssociationAudit.cs`,
`EaglOriginalCandidateRawStreamIdentityAudit.cs`, `SelfTestRunner.cs`,
`eagl_original_candidate_primitive_fetch_control.py`, `ProjectReadinessAudit.cs`
e `run-clean-room-authored-matrix.ps1`. O readiness registra a nova contagem,
mas rendering continua `not-proven`, assets `candidate-integrated`, áudio
`partially-integrated`, deformação `structural-only`, gameplay aprovado apenas
no escopo clean-room authored e o projeto permanece `NOT_READY`.

Validação: build Release `0` warnings/`0` errors, self-test `96/96`, matriz
`completed-authored-and-candidate-opt-in` com código 0, 10 cenários, 777
frames, 54 eventos de áudio e 1.554 amostras de animação; smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. A ISO permaneceu
somente para leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.

Próxima etapa: buscar evidência independente discriminativa de estado original
→ draw/UV/material, ou avançar na cadeia `EVT→ABKC→sample→playback` e no
timing/pose/rig/deformação. Tratar hashes iguais como controle estrutural;
não inventar bindings, não concluir o projeto, não executar/recompilar XBE e
não iniciar code review.

## Etapa 1.12 — ambiguidade do UID de runtime no state→geometry — 2026-10-08

Foi implementado o controle independente
`tools/analysis/eagl_original_state_uid_ambiguity_control.py`, integrado ao
`tools/run-clean-room-authored-matrix.ps1` e ao
`fifa_pc/Runtime/ProjectReadinessAudit.cs`. Ele reparseia os relatórios por
objeto e por `RUNTIME_ALLOC::UID`, sem atribuir ao UID semântica de draw,
material, UV, geometria, skinning, rigging, animação, morph ou GPU.

Resultado: `24/24` objetos unidos, `218` linhas render-scoped, `38` linhas de
geometria e `180` de estado; `38/38` geometrias e `180/180` estados têm UID
válido, com `24` domínios compartilhados. Todas as `38/38` geometrias
compartilham UID com estados, mas `0` têm um único candidato e `38` têm
múltiplos candidatos; os `24/24` objetos permanecem ambíguos e o binding
único provado é `0`. Status:
`verified-independent-original-state-runtime-uid-ambiguity-control`,
`semantic_binding=not-proven`, `original_semantics=not-claimed`.

A nova linha do readiness é `verified-structural-negative=true` somente para
o controle de ambiguidade. Rendering original permanece `not-proven`, assets
`candidate-integrated`, áudio `partially-integrated`, deformação
`structural-only`, e gameplay permanece aprovado apenas como clean-room
authored; os gates authored/candidate opt-in seguem verificados e o projeto
continua `NOT_READY`.

Validação: Release `0` warnings/`0` errors, self-test `96/96`, matriz
`completed-authored-and-candidate-opt-in` com código 0, 10 cenários, 777
frames, 54 eventos de áudio e 1.554 amostras de animação; smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. ISO somente para
leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência independente discriminativa de estado
original → draw/UV/material, `EVT→ABKC→sample→playback` ou
timing/pose/rig/deformação. Não promover o UID, não inventar bindings, não
concluir o projeto, não executar/recompilar XBE e não iniciar code review.

## Etapa 1.13 — UID + SHA-256 do command stream não discrimina state→geometry — 2026-10-08

Foi implementado o controle independente
`tools/analysis/eagl_original_state_uid_command_hash_ambiguity_control.py`,
integrado à matriz e ao `fifa_pc/Runtime/ProjectReadinessAudit.cs`. O controle
reparseia os relatórios por objeto, por `RUNTIME_ALLOC::UID` e pelo SHA-256 dos
bytes brutos do command stream. Ele mede cardinalidade zero/um/muitos sem
interpretar bytes como draw, material, UV, geometria, skinning, rigging,
animação, morph ou GPU.

Resultado: `24/24` objetos unidos, `218` linhas render-scoped, `38` geometrias
e `180` estados; `38/38` geometrias e `180/180` estados com UID válido e `24`
domínios UID compartilhados. Todas as `38/38` geometrias compartilham UID com
estados, mas `0` têm candidato único e `38` têm múltiplos candidatos. Ao
adicionar o hash bruto do command stream, `0` geometrias têm candidato
UID+hash, `38` têm zero e `0` têm binding único provado. Os `24/24` objetos
continuam ambíguos. Portanto o hash do command stream não transforma o UID em
discriminador state→geometry.

Status: `verified-independent-original-state-uid-command-hash-ambiguity-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. A nova linha
do readiness é `verified-structural-negative=true` apenas para esse controle;
nenhum gate semântico foi promovido. Rendering original permanece
`not-proven`, assets `candidate-integrated`, áudio `partially-integrated`,
deformação `structural-only`, gameplay clean-room authored e o projeto segue
`NOT_READY`.

Validação: Release `0` warnings/`0` errors, self-test `96/96`, matriz código 0,
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras de animação; smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO somente para leitura:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência independente discriminativa de estado
original → draw/UV/material, `EVT→ABKC→sample→playback` ou
timing/pose/rig/deformação. Não inventar bindings, não concluir o projeto, não
executar/recompilar XBE e não iniciar code review.

## Etapa 1.14 — identidade PCM nomeada EVT e controle negativo ABK — 2026-10-08

Foi implementado e integrado à matriz e ao readiness o controle independente
`tools/analysis/audio_event_named_payload_identity_control.py`. Ele reparseia e
une por chave os relatórios de runtime nomeado, decoder EVT e correlação
exaustiva de payloads, sem promover seleção de amostra ou semântica original.

Resultado: `17` linhas de runtime, `17` de decode nomeado e `17` linhas de
evento foram unidas sem faltas; `17/17` WAVE bytes, `17/17` contagens PCM e
`17/17` SHA-256 PCM coincidiram entre as fontes. A correlação contém `242`
linhas de banco, `241` decodificadas, mas `0` hashes de evento coincidem com
hashes de banco e há `0` pares PCM exatos. O status é
`verified-independent-audio-event-named-payload-identity-control`, com
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.

Isso valida a identidade do payload WAVE/PCM nomeado EVT e a consistência da
entrada candidate clean-room; não prova `EVT→ABKC→sample`, vínculo de evento de
gameplay ou playback original. A nova linha do readiness é
`verified-structural-negative=true` somente para esse controle. O gate de áudio
permanece `partially-integrated`, rendering original `not-proven`, assets
`candidate-integrated`, deformação `structural-only`, gameplay authored clean-room
e o projeto segue `NOT_READY`.

Validação: build Release `0` warnings/`0` errors, self-test `96/96`, matriz
código 0 com `completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames,
54 eventos de áudio e 1.554 amostras de animação; smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO somente para leitura:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar com evidência independente de `EVT→ABKC→sample→playback`
ou de estado original → draw/UV/material e timing/pose/rig/deformação; não
inventar bindings, não concluir, não executar/recompilar XBE e não iniciar code
review.

## Etapa 1.15 — identidade PCM das rotas de áudio candidate clean-room — 2026-10-08

Foi implementado e integrado à matriz, ao runtime e ao readiness o controle
independente `tools/analysis/audio_authored_named_payload_identity_control.py`.
As linhas de trace authored passaram a carregar `wave_bytes`, `pcm_bytes` e
`pcm_sha256`, permitindo reconciliar cada reprodução candidate com o payload
nomeado EVT preparado pelo runtime gerenciado.

Resultado: `10/10` cenários verificados, `54/54` linhas de trace tocadas e
`54/54` identidades reconciliadas; `54/54` matches de WAVE bytes, `54/54`
matches de bytes PCM e `54/54` matches de SHA-256 PCM. O relatório runtime
contém `17` candidatos nomeados, dos quais `8` rotas candidate únicas foram
usadas pelo conjunto authored; não houve `invalid_trace_identity` nem
`missing_runtime_candidates`. Status:
`verified-independent-authored-audio-named-payload-identity-control`;
`direct_selection_binding=not-proven`; `original_semantics=not-claimed`.

Isso demonstra somente que a rota limpa authored emite a identidade exata do
payload WAVE/PCM do candidato EVT nomeado. Não demonstra os IDs originais de
evento, a seleção `EVT→ABKC→sample`, a ligação de gameplay ou o playback
original; portanto não promove o gate principal de áudio. O readiness registra
a nova linha `audio-authored-named-payload-identity-control` como
`verified-authored`, enquanto áudio permanece `partially-integrated`, rendering
original `not-proven`, assets `candidate-integrated`, deformação
`structural-only`, gameplay clean-room integrado e o projeto `NOT_READY`.

Validação: build Release com `0` warnings/`0` errors, self-test `96/96`, matriz
código 0 com `completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames,
54 eventos de áudio e 1.554 amostras de animação; smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO somente para leitura:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de `EVT→ABKC→sample→playback`, de
estado original → draw/UV/material ou de timing/pose/rig/deformação; não
inventar bindings, não concluir, não executar/recompilar XBE e não iniciar code
review.

## Etapa 1.16 — enumeração global state→geometry sem promoção — 2026-10-08

Foi implementado e integrado à matriz o controle independente
`tools/analysis/eagl_original_state_geometry_global_assignment_control.py`.
Ele reprocessa, por chave de objeto, as fronteiras dos intervalos ordenados e o
relatório de partição original, enumerando globalmente todas as atribuições
permitidas pela evidência observada. O domínio usa apenas o próximo primitivo
antes do primeiro intervalo, o primitivo anterior depois do último e ambos os
vizinhos nos intervalos internos; não assume ownership anterior/próximo, que
cada state-run seja draw call ou cardinalidade one-to-one.

O controle reconciliou `62` linhas de ordem, `24` linhas de partição e `24`
objetos, sem objetos ausentes. Houve `36` intervalos não vazios, `12`
intervalos ambíguos, `12` objetos com múltiplos modelos de atribuição sem
restrição e `12` objetos com domínio singleton. A enumeração produziu soma de
`36` modelos sem restrição; a enumeração one-to-one produziu `24` modelos
únicos, mantidos somente como diagnóstico de uma hipótese de cardinalidade não
demonstrada. Os `24` conjuntos de índices de state foram reconciliados e
`unique_state_geometry_assignment_proven_rows=0`.

Status: `verified-independent-original-state-geometry-global-assignment-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. Domínios
singleton e o resultado one-to-one não promovem ownership de state, draw call,
geometria, UV ou material; portanto o controle é explicitamente negativo e não
altera a conclusão do rendering original.

O readiness registra a linha
`rendering-original-state-geometry-global-assignment-control` como
`verified-structural-negative`, mas rendering original continua `not-proven`.
Assets continuam `candidate-integrated`, áudio `partially-integrated`,
deformação `structural-only`, gameplay clean-room `clean-room-integrated` e o
projeto permanece `NOT_READY`.

Validação da etapa: build Release com `0` warnings e `0` errors, self-test
`96/96`, matriz código `0` com status
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras de animação; smokes relevantes de rendering, assets,
gameplay, áudio e animação, runtime headless/windowed e replays determinísticos
passaram. ISO somente para leitura: `2013003776` bytes, atributo `Archive`,
SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência discriminativa de estado original
→ draw/UV/material, `EVT→ABKC→sample→playback` ou
timing→pose→rig→skinning/deformação; não inventar bindings, não executar ou
recompilar XBE, não marcar o projeto como concluído e não iniciar code review.

## Etapa 1.20 — payload identity não reduz state→geometry — 2026-10-08

Foi implementado e integrado à matriz o controle independente
`tools/analysis/eagl_original_state_geometry_payload_identity_global_assignment_control.py`.
Ele reconcilia a enumeração global original state→geometry com a identidade
independente dos payloads de geometry candidatos e seus hashes brutos de slots.
O teste mede se essa identidade de payload reduz o domínio global de estados;
ela permanece evidência de geometry candidata e não é tratada como ownership de
state, draw, UV, material ou GPU.

O controle validou `24` objetos globais, `38` linhas de identidade de payload,
`24` objetos joined, `38` geometry completas e `190` linhas de hash bruto de
slots. Não houve objeto sem identidade (`0`), e a cardinalidade bateu em
`24/24`. A identidade reduziu `0` domínios de state. Permaneceram `12`
objetos com múltiplos modelos e `12` singleton; os somatórios antes/depois
foram `36/36` para os modelos gerais e `24/24` para one-to-one. Nenhum binding
único state→geometry→payload foi provado (`0`).

Status: `verified-independent-original-state-geometry-payload-identity-global-assignment-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. O readiness
registra `rendering-original-state-geometry-payload-identity-global-assignment-control`
como `verified-structural-negative`, passado em `24/24`, sem alterar o gate
principal. Assets continuam `candidate-integrated`, rendering original
`not-proven`, áudio `partially-integrated`, deformação
`structural-only`, gameplay `clean-room-integrated` e o projeto permanece
`NOT_READY`.

Validação da etapa: build Release com `0` warnings e `0` errors, self-test
`96/96`, matriz código exit `0` com status
`completed-authored-and-candidate-opt-in`, `10` cenários, `777` frames, `54`
eventos de áudio e `1.554` amostras de animação. Smokes relevantes de
rendering, assets, gameplay, áudio e animação, runtime headless/windowed e
replays determinísticos passaram; os traces de replay coincidiram. ISO somente
para leitura: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente e discriminativa de
state→draw/UV/material, `EVT→ABKC→sample→playback` ou
timing→pose→rig→skinning/deformação; manter os domínios não atribuídos
explícitos, não inventar bindings, não executar ou recompilar XBE, não marcar o
projeto como concluído e não iniciar code review.

## Etapa 1.22 — metadata de catálogo não reduz pose→banco — 2026-10-08

O controle independente
`tools/analysis/animation_catalog_pose_timing_global_assignment_control.py` foi
adicionado à matriz, ao `ProjectReadinessAudit` e ao `SelfTestRunner`. Ele
reconcilia timestamps AXT, âncoras de payload, índice de pose e contenção
morph/relocation sem atribuir semântica de timing, frame, pose, rig, skinning,
deformação ou playback.

Resultado: `19` bancos, `1.584` entradas e timestamps válidos, `19/19` bancos
com pares de âncoras completos (`3.168` âncoras), `391` poses distintas e `393`
layouts morph válidos. A contenção foi `159` pares em `53` layouts (`8` únicos,
`45` ambíguos), com zero gap-set exato. Os `391` poses não têm chave de join
com catálogo ou morph; o domínio pose→banco ficou `19→19`, os modelos globais
`7.429→7.429` e `unique_pose_animation_binding_proven_rows=0`.

Status `verified-independent-animation-catalog-pose-timing-global-assignment-control`,
`semantic_binding=not-proven`, `original_semantics=not-claimed`. Readiness:
`verified-structural-negative`, `391/391`; o gate de animação/deformação segue
`structural-only` e o projeto segue `NOT_READY`.

Validação: Release `0/0`, self-test `96/96`, matriz código `0`, smokes,
headless/windowed e replays determinísticos aprovados. ISO preservada somente
para leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência discriminativa de timing→pose→rig→skinning,
state→draw/UV/material ou `EVT→ABKC→sample→playback`; sem inventar bindings,
executar/recompilar XBE, marcar conclusão ou iniciar code review.

## Etapa 1.21 — classificação geometry/draw não reduz state→geometry — 2026-10-08

Foi implementado e integrado à matriz o controle independente
`tools/analysis/eagl_original_state_geometry_draw_classification_global_assignment_control.py`.
Ele junta, por objeto e por primitive key, a enumeração global state→geometry,
a classificação estrutural geometry/draw, e a identidade independente dos
payloads candidatos. O objetivo foi testar se a classificação observável dos
38 primitives de geometry, apoiada pela identidade de payload, reduz o domínio
global dos states sem converter marcador de geometry em ownership de draw.

O controle validou `24` objetos globais, `218` linhas de classificação e `38`
identidades de payload, sem objetos faltantes. Foram reconciliadas `38/38`
linhas geometry classificadas com payload identity, `218/218` classificações,
`180` state markers e `180` state rows sem payload identity. Todos os `24`
objetos tinham mais states que geometry. Os modelos antes/depois ficaram
`36/36` e os modelos one-to-one `24/24`; a classificação reduziu `0` domínios
e nenhum binding único state→geometry→draw foi provado (`0`).

Status: `verified-independent-original-state-geometry-draw-classification-global-assignment-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. O readiness
registra `rendering-original-state-geometry-draw-classification-global-assignment-control`
como `verified-structural-negative`, passado em `24/24`, sem alterar o gate
principal. Assets continuam `candidate-integrated`, rendering original
`not-proven`, áudio `partially-integrated`, deformação
`structural-only`, gameplay `clean-room-integrated` e o projeto permanece
`NOT_READY`.

Validação da etapa: build Release com `0` warnings e `0` errors, self-test
`96/96`, matriz código exit `0` com status
`completed-authored-and-candidate-opt-in`, `10` cenários, `777` frames, `54`
eventos de áudio e `1.554` amostras de animação. Smokes relevantes de
rendering, assets, gameplay, áudio e animação, runtime headless/windowed e
replays determinísticos passaram; os traces de replay coincidiram. ISO somente
para leitura: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente e discriminativa de
state→draw/UV/material, `EVT→ABKC→sample→playback` ou
timing→pose→rig→skinning/deformação; manter os domínios não atribuídos
explícitos, não inventar bindings, não executar ou recompilar XBE, não marcar o
projeto como concluído e não iniciar code review.

## Etapa 1.17 — produto global state→geometry→UV/material sem promoção — 2026-10-08

Foi implementado e integrado à matriz o controle independente
`tools/analysis/eagl_original_state_geometry_uv_material_global_assignment_control.py`.
Ele reconcilia, por chave de objeto, a enumeração global de state→geometry com
o controle independente do papel candidato do slot 1 e com as fontes candidatas
de material. O resultado é deliberadamente um produto de domínios estruturais:
singleton de slot candidato e singleton de fonte same-trunk não são tratados
como prova de UV, material ou draw.

Foram reconciliadas `24` linhas globais, `24` linhas UV e `62` linhas de
intervalos de material, cobrindo `24` objetos sem faltas. A fonte candidata UV
teve `216` grupos completos e `2` incompletos; `216` grupos tinham slot 1
próximo de unidade e `0` outros slots tinham esse perfil. Os controles de
atributo/raster e material candidato foram válidos em `24/24` objetos, com
`24/24` fontes de estado→candidato completas. O produto estrutural totalizou
`36` modelos: `12` objetos permaneceram com múltiplos modelos por geometry e
`12` tiveram um único modelo estrutural. Isso não é binding semântico;
`unique_state_geometry_uv_material_binding_proven_rows=0`.

Status: `verified-independent-original-state-geometry-uv-material-global-assignment-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. O readiness
registra `rendering-original-state-geometry-uv-material-global-assignment-control`
como `verified-structural-negative`, sem alterar o gate principal. Rendering
original continua `not-proven`, assets `candidate-integrated`, áudio
`partially-integrated`, deformação `structural-only`, gameplay
`clean-room-integrated` e o projeto permanece `NOT_READY`.

Validação da etapa: build Release com `0` warnings e `0` errors, self-test
`96/96`, matriz código `0` com status
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras de animação; smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO somente para leitura:
`2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca por evidência discriminativa de
`EVT→ABKC→sample→playback`, state→draw/UV/material ou
timing→pose→rig→skinning/deformação; não inventar bindings, não executar ou
recompilar XBE, não marcar o projeto como concluído e não iniciar code review.

## Etapa 1.18 — enumeração global EVT→ABKC/sample sem promoção — 2026-10-08

Foi implementado e integrado à matriz o controle independente
`tools/analysis/audio_event_sample_global_assignment_control.py`. Ele repara,
por nome de evento, o relatório completo de payloads EVT, o controle de
ambiguidade textual `FIFAST2.csi`, o join de runtime paralelo, a síntese
negativa anterior e as rotas authored. A enumeração separa três domínios:
identidade PCM exata, nomes CSI fracos e o pool de amostras de banco
decodificadas. O pool não é multiplicado nem atribuído a eventos, porque não há
no material observado uma chave EVT→ABKC/sample que sustente essa relação.

O controle reconciliou `17` linhas EVT e `242` linhas de banco; `17` eventos e
`241` linhas de banco foram decodificados. Todos os `17` joins EVT/CSI/runtime
foram válidos, a síntese negativa teve `5/5` fontes válidas e as rotas authored
totalizaram `15`. O pool estrutural não atribuído contém `241` candidatos.

Não houve correspondência PCM exata (`event_exact_pcm_match_rows=0`,
`event_hashes_with_match=0`), portanto os `17` eventos têm domínio de identidade
de sample exata vazio. O domínio textual fraco CSI ficou em `10` eventos sem
match, `3` com um match e `4` ambíguos, somando `38` opções textuais; o produto
de identidade exata com nome fraco totalizou `0` modelos. Nenhuma binding
EVT→ABKC/sample foi promovida:
`unique_evt_abkc_sample_binding_proven_rows=0`.

Status: `verified-independent-original-audio-event-sample-global-assignment-control`;
`semantic_binding=not-proven`; `direct_selection_binding=not-proven`;
`original_semantics=not-claimed`. O readiness registra
`audio-event-sample-global-assignment-control` como
`verified-structural-negative`, gate passado, observado `17/17`; o gate de
áudio continua `partially-integrated`, rendering original `not-proven`, assets
`candidate-integrated`, deformação `structural-only`, gameplay
`clean-room-integrated` e o projeto permanece `NOT_READY`.

Validação da etapa: build Release com `0` warnings e `0` errors, self-test
`96/96`, matriz código `0` com status
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras de animação; smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO somente para leitura:
`2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a procurar evidência independente e discriminativa
para selecionar um sample original ou ligar estado a draw/UV/material e timing
a pose/rig/skinning; manter os domínios não atribuídos explícitos, não inventar
bindings, não executar ou recompilar XBE, não marcar o projeto como concluído e
não iniciar code review.

## Etapa 1.19 — state→geometry→candidate render sem promoção — 2026-10-08

Foi implementado e integrado à matriz o controle independente
`tools/analysis/eagl_original_state_geometry_candidate_render_global_assignment_control.py`.
Ele reconcilia, pela chave de objeto, a enumeração global original
state→geometry, o batch de material/render candidato, a cadeia de proveniência
dos candidatos e o join independente state→geometry→payload. O objetivo foi
medir a interseção estrutural com o runtime candidato sem converter
`object_state` em ownership de grupo de render.

As quatro fontes foram válidas e reconciliadas em `24/24/24/24` linhas, com
`24` objetos joined e nenhuma chave faltante. A soma de candidatos de geometry
foi `38` e a soma de grupos geometry candidatos também `38`; a cardinalidade
foi igual em `24/24` objetos. O estado de objeto candidato foi exato em
`24/24`, a cadeia de fonte candidata e o join de payload foram válidos em
`24/24`, mas `render_group_state_match_rows=0`. Portanto o controle não
encontrou evidência de correspondência state→grupo de render.

A enumeração global manteve `12` objetos com múltiplos modelos geometry e `12`
com domínio singleton. A soma sem restrição foi `36` modelos e a enumeração
one-to-one produziu `24` modelos, ambos apenas diagnósticos de cardinalidade;
`unique_state_geometry_candidate_render_binding_proven_rows=0`. Nenhum desses
resultados promove binding de state, draw, UV, material, grupo de render ou
semântica original.

Status: `verified-independent-original-state-geometry-candidate-render-global-assignment-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. O readiness
registra `rendering-original-state-geometry-candidate-render-global-assignment-control`
como `verified-structural-negative`, gate passado `24/24`; rendering original
continua `not-proven`, assets `candidate-integrated`, áudio
`partially-integrated`, deformação `structural-only`, gameplay
`clean-room-integrated` e o projeto permanece `NOT_READY`.

Validação da etapa: build Release com `0` warnings e `0` errors, self-test
`96/96`, matriz código exit `0` com status
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras de animação; smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A matriz também
revalidou o relatório final de readiness. ISO somente para leitura:
`2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a procurar evidência independente e discriminativa
para state→draw/UV/material, `EVT→ABKC→sample→playback` ou
timing→pose→rig→skinning/deformação; manter os domínios não atribuídos
explícitos, não inventar bindings, não executar ou recompilar XBE, não marcar o
projeto como concluído e não iniciar code review.

## Registro da etapa executada — 1.23 — 2026-10-09

Foi implementado e integrado à matriz o controle independente
`tools/analysis/audio_event_static_context_global_assignment_control.py`.
Ele reconcilia, sem executar o XBE, o contexto estático de literais de áudio do
retail-XBE, o índice de literais/candidatos de caminho de `EVENTSPA.EVT` e o
domínio decodificado de payloads EVT/ABKC. O objetivo foi testar se esse join
global reduz o domínio de seleção de evento para banco/sample, mantendo
explícita a distinção entre contexto estático, payload preparado e playback.

O controle validou `58` linhas de xref estático, `23` literais únicos, `147`
linhas de contexto, `108` chamadas executáveis, `56` alvos distintos, `14`
alvos com múltiplos literais, `1` match aninhado e `10` matches XDVDFS. Para
`EVENTSPA.EVT`, foram validados `17` nomes, `1.061` pares candidatos, `4`
linhas literais e `9` ocorrências, mas `0` xrefs literais de evento e apenas
`1` caminho distinto casado. O domínio de payload teve `17` eventos e `241`
candidatos de banco, sem match PCM exato (`0`) e sem hash de evento com match
(`0`). Todos os `17` eventos permaneceram sem xref estático de evento; o pool
de alvos estáticos foi `56`, os modelos globais ficaram `952→952`, a redução
foi `0` e nenhum seletor estático único foi provado (`0`).

Status: `verified-independent-audio-event-static-context-global-assignment-control`;
`semantic_binding=not-proven`; `direct_selection_binding=not-proven`;
`original_semantics=not-claimed`. O readiness registra
`audio-event-static-context-global-assignment-control` como
`verified-structural-negative`, passado em `17/17`; o gate de áudio continua
`partially-integrated`, rendering original `not-proven`, assets
`candidate-integrated`, deformação `structural-only`, gameplay
`clean-room-integrated` e o projeto permanece `NOT_READY`.

Validação da etapa: build Release com `0` warnings e `0` errors, self-test
`96/96`, matriz código com exit `0` e status
`completed-authored-and-candidate-opt-in`; os cenários authored/candidate,
smokes relevantes, runtime headless/windowed e replays determinísticos
passaram, com traces coincidentes. A matriz reportou `10` cenários, `777`
frames, `54` eventos de áudio e `1.554` amostras de animação. ISO somente para
leitura: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente e discriminativa para
`EVT→ABKC→sample→playback`, state→draw/UV/material ou
timing→pose→rig→skinning/deformação; manter os domínios não atribuídos
explícitos, não inventar bindings, não executar ou recompilar XBE, não marcar o
projeto como concluído e não iniciar code review.

## Registro da etapa executada — 1.24 — 2026-10-09

Avaliei e usei `sp00nznet/xboxrecomp` apenas como ferramenta de análise
estática do XBE Original Xbox. A escolha é tecnicamente pertinente ao alvo
(XBE Xbox original), ao contrário de PS2Recomp, mas o pipeline/runtime de
recompilação do repositório não foi adotado: o produto continua sendo uma
reimplementação clean-room em .NET, sem emulação. Foram usados somente o
parser XBE e o disassembler estático; não foi usado o recompilador, harness de
conformance que executa código, runtime ou código gerado. Checkout analisado:
`193e2995ffaec871bb3be43a43bd66aa5fccfeb1`, fora do workspace.

O parse identificou o título retail `0x45410085`, XDK `5849`, entry point
`0x0024D8BE`, 11 seções e `.text` de `0x271AD8`. A disassembly heurística
reportou 15.288 possíveis funções, 858.228 instruções e 156.896 xrefs; esses
limites são pistas heurísticas, não prova de funções ou semântica. No dispatcher
EAGL em `0x0031A930`, o controle estrutural confirmou prefixo de 42 entradas
executáveis, 39 destinos distintos e primeiro word não executável
`0x00000001` no índice 42. Das 2.364 streams originais completas, foram
reconciliadas 36.779 ocorrências e 12 IDs observados; os 12/12 indexam slots
executáveis da tabela. Isso prova somente o split estático do cabeçalho
comando/tamanho e o índice→slot/destino da tabela, não o comportamento dos
handlers, seus limites funcionais ou significado dos comandos.

Implementação integrada: `fifa_pc/Runtime/EaglOriginalRenderChainAudit.cs`,
`fifa_pc/Runtime/ProjectReadinessAudit.cs`, teste de fixture em
`SelfTestRunner.cs`, matriz e controle independente
`tools/analysis/eagl_original_render_command_dispatch_control.py`; relatórios
`fifa_pc/eagl-original-render-command-dispatch-control.tsv` e
`fifa_pc/eagl-original-render-command-dispatch-control-summary.tsv`. O novo
subgate é `verified-structural` 12/12; semântica continua `not-proven` e
`original_semantics=not-claimed`. Não houve promoção de rendering, assets,
UV/material, skinning/rigging, animação, morph, áudio ou GPU; readiness segue
`NOT_READY`.

Validação: Release `0` warnings/`0` errors, self-test `96/96`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime headless
e windowed e replays determinísticos passaram. Cobertura authored: 10 cenários,
777 frames, 54 eventos de áudio, 1.554 amostras de animação. ISO permaneceu
somente leitura, `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo trabalho: usar a tabela e os handlers como mapa de navegação para
investigação estática discriminativa; jamais inferir comportamento por nome,
prólogo ou posição. Consultar [XboxRecomp](https://github.com/sp00nznet/xboxrecomp)
e seu [guia de decompilação estática](https://github.com/sp00nznet/xboxrecomp/blob/main/docs/DECOMP.md)
como ferramenta auxiliar somente. Manter clean-room .NET, não executar ou
recompilar o XBE, não inventar bindings, não concluir o projeto nem iniciar
code review.


## Registro das etapas 1.25 e 1.26 — 2026-10-13

A etapa 1.25 implementou o parser .NET dos parâmetros consumidos pelos
handlers EAGL: 2.364 descritores, 36.779 comandos, 32.051 pares, 11.818
bases exatas de registros __VB e 11.818 strides compatíveis. Foram encontrados
2.364 parâmetros TAR e 1.612 GeoPrimState; 1.612 descritores contêm ambos.
O relatório antigo de state observa somente o primeiro par. O comando 26
chega a submissão indexada condicional; seu word 2 não foi tratado como modo
de primitiva. Controle: 20 trechos/106 sites/6 mutações negativas.

Na etapa 1.26, o decoder novo
`fifa_pc/Runtime/EaglIndexedDrawSourceAudit.cs` liga contagem (comando 2),
ponteiro de índices (14) e draw (26) no mesmo descritor: 2.363 fontes locais
verificadas entre 2.364, 1 pendente e 0 rejeitadas. Foram decodificados
1.739.618 bytes de índices unsigned little-endian de 16 bits. O controle
`tools/analysis/eagl_indexed_draw_source_control.py` releu os payloads,
pares, comandos e relocations diretamente na ISO, independentemente do .NET:
5 trechos estáticos, 37 sites e 4 mutações negativas passaram. Os 2.363
ranges são compatíveis com seus registros-base. A pendência é
`tops0260.big!tops0260.ord`, layer 0, primitive 7, sem comando 2 local;
não foi inventada contagem herdada. 7.092 referências de buffer da etapa
1.25 dependem do seletor dinâmico, ainda não resolvido.

Pesquisa na internet e plano aplicável:
`docs/FIFA_STREET_2_ACELERACAO_PESQUISA_2026-10-13.md`;
contrato completo: `docs/FIFA_STREET_2_EAGL_PARAMETROS_DRAW_2026-10-09.md`.
XboxRecomp continua auxiliar estático; Ghidra/nxdk/vgmstream são referências
avaliadas, sem adotar runtime de recompilação ou emulação.

Nova fonte para a etapa seguinte:
`data/initstaz.big!initstay.big!mat.o`, 38.970 bytes, SHA-256
`75DBC11557C6F3E4A32D6D88372476398B3A4C435E3907C2902E38579FB29430`.
O probe `tools/analysis/eagl_material_parent_probe.py` passou, exit 0,
3 símbolos/168 bytes de janelas: ParentRM_FifastCharacterSkin em 0x1458,
FifastCharacterSkin0__EAGLMicroCodeVS em 0x1490 e MicroCodePS em 0x1780,
todos na seção 1. Isso é definição/símbolo e janela raw, ainda sem prova da
instalação do shader no draw ou de seus atributos.

Próxima etapa recomendada: seguir o parent em mat.o, a declaração/programa
instalados e seus consumidores até posição/UV/material; resolver também o
produtor/domínio de 0x00345BCC. Não repetir joins globais de nomes sem uma
fonte nova que possa discriminar alternativas. Manter clean-room .NET,
ISO somente leitura, original_semantics=not-claimed, sem execução/recompilação
do XBE, sem declarar conclusão do projeto e sem iniciar code review.



Validação final das etapas 1.25/1.26, em 2026-10-13: Release 0 warnings/
0 errors, self-test 98/98 e matriz completa exit 0,
`completed-authored-and-candidate-opt-in`. Smokes relevantes, runtime
headless/windowed e replays passaram; 10 cenários, 777 frames, 54 eventos de
áudio e 1.554 amostras de animação. Probe material adicional: exit 0.
Log: `fifa_pc/stage-1.26-matrix-2026-10-13.log`.
Os dois subgates novos estão `verified-static-binding`; os gates principais
permanecem assets candidate-integrated, rendering not-proven, gameplay
clean-room-integrated, áudio partially-integrated e deformação structural-only.
Readiness: NOT_READY. ISO somente leitura, 2013003776 bytes, Archive,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 1.27 — validação otimizada e probe de material (2026-10-13)

- Busca streaming de nomes de áudio: `Span<byte>.IndexOf`, preservando overlaps, EOF e hits entre chunks; sem binding novo.
- Resultado focal: 1.737.085.606 bytes/60 assets em 4.995 ms; resultado da matriz: 3.047 ms. Report e summary mantêm os mesmos SHA-256 anteriores.
- Probe somente leitura: `data/initstaz.big!initstay.big!mat.o`, três símbolos/168 bytes de janelas brutas; não é decoder de shader.
- Validação final: Release 0 warnings/0 errors, 99/99 self-tests, smokes, headless/windowed e replays determinísticos; matriz exit 0 / `completed-authored-and-candidate-opt-in`.
- Cobertura authored: 10 cenários, 777 frames, 54 eventos de áudio, 1.554 amostras de animação.
- Log: `fifa_pc/stage-1.27-matrix-2026-10-13.log`. Detalhes: `docs/FIFA_STREET_2_VALIDACAO_OTIMIZADA_2026-10-13.md`.
- Próximo recorte funcional: provar o consumidor da declaração em `mat.o` e sua associação aos cinco buffers; não atribuir UV/rig/material por suposição.
- Readiness global permanece `NOT_READY`; gameplay clean-room passa, rendering/assets/áudio/deformação continuam com lacunas documentadas.
- ISO: 2013003776 bytes, Archive, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`, sem escrita.

## Etapa 1.28 — declaração de material e envelope VS (2026-10-13)

- Decoder clean-room .NET: `EaglMaterialDeclarationAudit.cs`, CLI `eagl-material-declaration-audit`.
- Fonte fixada `data/initstaz.big!initstay.big!mat.o`: cinco campos stream/registro/formato, declaração de 44 bytes; larguras 12/8/4/12/2. Sem atribuir UV/posição/rig.
- Header VS validado pelo consumidor: versão 0x2078, 46 instruções de 16 bytes, 740 bytes incluindo header. Não há execução ou tradução do shader.
- Controle independente relê a ISO e confere 4 trechos/51 sites do XBE, tabelas de largura e 7 controles negativos.
- Readiness limitado `rendering-material-declaration-and-vs-envelope`: `verified-static-layout`; herança do parent, atributos e rendering completo continuam `not-proven`.
- Primeira matriz falhou por uma guarda adicional equivocada de ELF e_machine=3. Header original=8, guarda/fixture corrigidas com teste negativo para 3. Log inicial preservado.
- Repetição final: Release 0 warnings/0 errors, 100/100 self-tests, smokes, headless/windowed e replays; exit 0 / `completed-authored-and-candidate-opt-in`.
- Log válido: `fifa_pc/stage-1.28-matrix-retry-2026-10-13.log`. Detalhes: `docs/FIFA_STREET_2_MATERIAL_DECLARACAO_2026-10-13.md`.
- Próxima etapa funcional: transformação/resolução do render method serializado e parent runtime +0x2C; decoder estático das instruções com operandos ativos. Não igualar +0x1C serializado e +0x2C runtime por suposição.
- ISO intacta, 2013003776 bytes, Archive, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Readiness global `NOT_READY`; nenhum gate principal foi relaxado.

## Etapa 1.29 — runner Release direto (2026-10-13)

- A matriz mantém build Release obrigatório; os mesmos comandos de aplicação são executados pela DLL compilada, eliminando a reavaliação de projeto por `dotnet run` em cada chamada.
- Adaptador puro `tools/clean-room-release-invocation.ps1`: prefixo exato, arrays de argumentos preservados, sem comandos interpolados, sem XBE/emulação. Dez testes positivos/negativos incluídos na matriz.
- Validação final: Release 0 warnings/0 errors, 100/100 self-tests .NET + 10/10 do runner, smokes, headless/windowed e replay determinístico; exit 0 / `completed-authored-and-candidate-opt-in`.
- Confronto com a etapa 1.28: oito relatórios selecionados byte-idênticos por SHA-256, incluindo readiness e cobertura. A busca de nomes de áudio levou 2.552 ms nesta matriz.
- Cobertura authored inalterada: 10 cenários, 777 frames, 54 eventos de áudio e 1.554 amostras de animação.
- Log: `fifa_pc/stage-1.29-matrix-2026-10-13.log`. Detalhes/hashes/fonte oficial: `docs/FIFA_STREET_2_RUNNER_RELEASE_2026-10-13.md`.
- Próximo avanço funcional: resolver parent serializado→runtime e provar operandos ativos/entradas/saídas do VS em `mat.o`; manter seletor 0x00345BCC e áudio/deformação separados.
- Nenhum prazo ou porcentagem total é inferido da redução de custo da validação. Gameplay clean-room passa; assets/rendering original/áudio/deformação continuam pendentes.
- ISO intacta, acesso somente leitura, 2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Readiness global `NOT_READY`; projeto não concluído, sem code review.


## Etapa 1.30 — operandos estáticos do vertex program (2026-10-13)

- Decoder .NET `EaglVertexProgramAudit.cs` e controle independente que extrai novamente a ISO e usa packing 128-bit: 46/46 instruções, entradas ativas 0/1/3/4, 8 referências de slots, 6 constantes relativas, 6 instruções pareadas, 9 escritas de saída nos endereços 0/3/4/9. Não executa shader/XBE e não prova liveness por componente ou draw efetivo.
- Controle: 5 trechos/65 sites/18 mutações negativas; upload DWORD sem reordenação no trecho 0x291D70. Subgate `rendering-vertex-program-opcode-operands` passou como `verified-static-instruction-layout`.
- Validação final: Release 0/0, 102/102 self-tests .NET + 10/10 runner, smokes, headless/windowed e replays; matriz exit 0 / `completed-authored-and-candidate-opt-in`.
- Log: `fifa_pc/stage-1.30-matrix-2026-10-13.log`; contrato/fontes/hashes: `docs/FIFA_STREET_2_SHADER_OPERANDOS_2026-10-13.md`.
- Próxima evidência: sondagem fresh encontrou parent em +0x1C E +0x2C nos 2364 render methods e referências explícitas Skin=1416, Cloth=594, Eye=354. Validar o registro completo/declaração/definições antes de afirmar resolução do loader ou instalar rendering original; a pista anterior do prefixo era incompleta.
- ISO preservada, somente leitura, 2013003776 bytes, Archive, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Readiness continua `NOT_READY`; sem execução/recompilação/emulação, bindings inventados, conclusão do projeto ou code review.

## Fut Street 2 — etapa 1.31 e identidade pública (2026-10-13)

- Nome público vigente: **Fut Street 2**, conforme pedido do usuário. Título Win32, banners .NET e metadados AssemblyTitle/Product atualizados e testados. Workspace, ISO, `fifa_pc`, `FifaStreetPc.csproj`/DLL/namespace, comandos e nomes internos de assets permanecem preservados por compatibilidade/rastreabilidade; registros históricos não foram apagados.
- Novo auditor .NET `EaglModelMaterialReferenceAudit.cs`, comando `eagl-model-material-reference-audit`, e controle Python com enumeração independente fresh de modelos/layers/descriptors: 2364 primitivas em 883 objetos; declaração byte-idêntica de 44 bytes/5 elementos e referências nomeadas exatas de VS/PS para três fontes em `mat.o`: Skin=1416, Cloth=594, Eye=354; programas VS de 46/46/47 instruções.
- Registro completo de RM tem 52 bytes. Parents externos em +0x1C E +0x2C coincidem nas 2364 linhas. Isso corrige a pista antiga incompleta do prefixo; não prova alias `ParentRM_*`→`__RM:::ParentRM_*` no loader. Handles/estado serializados +0x10/+0x18/+0x24=0; +0x28=0xFFFFFFFF. Sem seleção de buffer, instalação de shader, semântica de atributo ou draw original alegados.
- Controle independente: 4 trechos/53 sites do consumidor XBE e 16 negativos. Subgate `rendering-model-material-program-references`: `verified-static-model-reference`, 2364/2364. `runtime_loader_resolution=not-proven`, `selected_buffers=not-proven`, `full_rendering_binding=not-proven`, `original_semantics=not-claimed`.
- Matriz completa exit 0 / `completed-authored-and-candidate-opt-in`: Release 0/0, 105/105 self-tests .NET + 10/10 runner, smokes, headless/windowed e replays determinísticos. Cobertura authored: 10 cenários, 777 frames, 54 eventos de áudio, 1554 amostras de animação. Log `fifa_pc/stage-1.31-matrix-2026-10-13.log`.
- Depois da matriz, corrigida uma última linha de banner windowed ainda antiga; validação focal final exit 0: Release 0/0, 105 testes, metadados, headless 80 frames, replay match/input/animation/audio byte-idêntico, windowed e captura 960x540. Script `tools/test-fut-street-product-identity.ps1`; log `fifa_pc/stage-1.31-product-identity-2026-10-13.log`. Essa rodada final é focal, não uma segunda matriz completa.
- Contrato, hashes, fontes e limites: `docs/FUT_STREET_2_MODELOS_MATERIAIS_2026-10-13.md`. Nova entrada de documentação: `README.md`. Relatórios anteriores de declaração e operandos mantiveram seus hashes; readiness ganhou somente o subgate delimitado.
- Próxima etapa: investigar resolução de parent/handles e o caminho de seleção/instalação de draw, buffers e constantes, incluindo 0x00345BCC. Pista adicional somente leitura: literais `ParentRM_`/`RM` em 0x002D1DE0/0x002D1DEC, consumidor 0x00138A70 e busca chamada em 0x00138AE1→0x001987F0. É pista, não prova de loader, alias ELF ou instância; não foi promovida a gate.
- Gates principais: assets `candidate-integrated` (não passou), rendering `not-proven` (não passou), gameplay authored `clean-room-integrated` (passou sem fidelidade original alegada), áudio `partially-integrated` e animação/deformação `structural-only` (não passaram). Não deduzir porcentagem/ETA ou fim do projeto pelo número de subgates.
- ISO intacta nas duas rodadas, exclusivamente entrada somente leitura: 2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Readiness global `NOT_READY`; continuar clean-room .NET, sem emulação/recompilação/execução do XBE, bindings inventados, conclusão do projeto ou code review.

## Publicação GitHub do Fut Street 2 (2026-10-13)

- Repositório público, conforme escolha explícita do usuário: [MarllonMenezes/fut-street-2](https://github.com/MarllonMenezes/fut-street-2), branch `main`. Workspace agora tem Git local; remote `origin` aponta para esse repositório via HTTPS. Snapshot completo inicial `2d396380ea6506a569ea6ef8ea3606d185268125` enviado e verificado contra o remoto.
- Apenas 304 arquivos de código, testes e documentação. `.gitignore` usa lista permitida: ISO, XBE, arquivos de jogo, mídia extraída, exports, TSV/CSV/logs, bin/obj/caches e projeto Star Wars não entram no repositório. Nenhum arquivo local foi apagado. `.gitattributes` impede conversão automática de line endings para preservar bytes/hashes históricos.
- Checagem inicial isolada detectou a omissão de `Program.cs` no pacote; lista corrigida antes de publicar. Snapshot completo extraído sem ISO/assets/relatórios: Release 0/0, self-tests .NET 105/105 e runner 10/10. Testes Python no workspace original: 57/57. Checagens de tamanho/tipo e assinaturas de credenciais conhecidas passaram.
- Build/self-test não precisam da ISO. Matriz completa e análises de dados requerem a ISO local e relatórios intermediários que não são distribuídos; preparar essas entradas antes de tentar a matriz em um novo clone. Instruções: `docs/GITHUB_PUBLICACAO.md`.
- Commit usa GitHub noreply para não publicar o e-mail pessoal do Git global. Nenhuma licença open-source foi escolhida automaticamente. Não foi criado pull request nem iniciado code review.
- ISO preservada: 2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Upload não muda gates ou readiness: `NOT_READY`; retomar a próxima investigação da etapa 1.31.




