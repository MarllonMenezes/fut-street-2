# FIFA Street 2 — handoff técnico para continuação em outro chat

## Direção vigente — 2026-10-13

O objetivo confirmado é **recompilação estática do código original para PC**,
não recriar o jogo em .NET. Leia primeiro
[direção e limites](docs/FUT_STREET_2_DIRECAO_RECOMPILACAO.md).
O snapshot abaixo preserva o objetivo anterior como histórico. O protótipo
.NET não é o jogo recompilado; XBE ainda não recompilado, `NOT_READY`.
Stack do núcleo e política de GPU/áudio pendem de confirmação.

Data do snapshot: 2026-10-08, horário de São Paulo  
Workspace: `D:\projeto zeta`  
Objetivo: reimplementação clean-room em .NET, sem emulação e sem executar o XBE.

## Etapa 1.07 — controle independente de nomes EVT em todos os assets de áudio — 2026-10-08

Foi adicionada a auditoria clean-room `AudioEventNameAssetTextSearchAudit` para testar uma associação textual independente entre o índice nomeado de `eventspa.evt` e o universo de `data/audio`. O resultado real é `17` candidatos, `60/60` assets válidos, `1.737.085.606` bytes, `1` EVT-fonte excluído e nenhum nome encontrado nos assets (`0` pares e `0` ocorrências). O controle Python independente fecha `1.020/1.020` linhas da matriz asset/nome, todas sem hit e sem duplicidade.

Isso não prova seleção de amostra, ABKC/AST/BNK, evento, playback ou semântica original: `semantic_binding=not-proven`, `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. A matriz foi validada com exit code `0` (`completed-authored-and-candidate-opt-in`), Release `0/0`, self-test `93/93`, smokes, headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras. O readiness continua `NOT_READY`; áudio permanece parcialmente integrado e os gates de rendering e animação-deformação permanecem abertos.

Hashes SHA-256: auditor `439D5AB62558278E5CC80CA41AB7CAED8EC7C6DC86ED46EF518363E0E3FB20F5`; controle Python `4BB18D4534BFD5E57F568DAA0627CACDF0F2AA9DE7CEBD46AB61C1C03552F90C`; relatório `9B8995864273ADF880CD02BFA2E10F5F08BA8C9F44B8D398D33F1284390A1ADD`; summary `04EF71651802C2F093C53312683264E35425BA699BD8A62711657D3D57035BF2`; controle `B745AED8D07629B5000183DE9B35C3B59544193422AD1B73825265C4B4351961`; controle summary `886FCD7A7E36E23D56D175D4D111F7C0E38F74708525A79ECACF03397D860174`; readiness `23FC205E7488ECBFCB2F0FF0A5DE25A5F13EAA879E8AFACFD5EAE262348F0FF2`.

ISO preservada somente para leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

## Etapa 1.06 — controle independente state/fetch + slot numérico + textura/raster candidata — 2026-10-08

Foi criado `tools/analysis/eagl_original_state_slot1_candidate_uv_ambiguity_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia `2364` linhas state/fetch e `11820` linhas de papéis; há `2364/2364` primitivas globais unidas e, entre os objetos candidatos, `218` grupos (`216` completos, `2` incompletos), `180` estados SHAPENAME (`178` completos). O perfil near-unit observado no slot `1` ocorre em `178/178` grupos SHAPENAME completos e em `216/216` grupos completos; outros slots: `0`. A validação candidata de textura/atributo e a influência raster cobrem `24/24`; state→geometria único provado: `0`.

É somente controle estrutural negativo. Não atribui semântica UV ao slot `1`, não estabelece state→draw/material e não promove textura/raster candidata para semântica original. O readiness registra `rendering-original-state-slot1-candidate-uv-ambiguity-control` como `verified-structural-negative`, `24/24`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; rendering continua `not-proven` e o projeto `NOT_READY`.

Validação: Release `0/0`, self-test `92/92`, matriz completa exit code `0`, smokes, runtimes headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos e `1554` samples. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256: controle `FCFF267936F6B03FD94174600D443424CEDD399E9634B253DD20E511D25B6449`; relatório `A61381C76D19CCCFBD995EBB928770B975F0EF923A383F8192ADCADC36F6ACE1`; summary `BFAF6A5AF309D92E0CAB5BBABB6270BA0D6326E3E516914E8C3B5079BFBD5C17`; `ProjectReadinessAudit.cs` `20099B92DC1BCA0F58F577A1A8EF7ADB3BE9F2BFDBD172F04A09B0EC774DE363`; matriz `CD626E7E0D90FC4F8BEE016C20978E6F69CE69BB7D0DF12F704609DC2853BEDC`.

Próxima investigação: associação original única `state→draw` com UV/material ou `EVT→ABKC→sample→playback`; este controle não altera os gates principais.

## Etapa 1.05 — controle independente de ambiguidade nominal FIFAST2.csi↔EVT — 2026-10-08

Foi criado `tools/analysis/fifast2_csi_evt_name_ambiguity_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia os `100` nomes CSI e os `17` candidatos nomeados EVT com tokenização determinística: `100/100` linhas CSI e `17/17` EVT válidas, `17` nomes EVT únicos; `10` sem sobreposição, `3` com uma sobreposição, `4` ambíguos e `7` com alguma sobreposição. Não houve nome exato e `0` bindings únicos provados.

Trata-se somente de controle negativo de nomes. Ele não dá semântica ao CSI, não seleciona ABKC/AST/BNK ou amostra, não estabelece gameplay→playback e não promove `Goal`, `GB`, `Tackle` ou `Panna`. O subgate `audio-fifast2-csi-evt-name-ambiguity-control` está `verified-structural-negative`, `17/17`, com `semantic_binding=not-proven`, `direct_selection_binding=not-proven` e `original_semantics=not-claimed`; rendering, áudio, animação e o readiness continuam não prontos.

Validação: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `92/92`, smokes, runtime headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos e `1554` samples. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256: controle `FA75AD1D77492032A54992A8611A5DDF7D75DB046EC2D82948D417653E88A942`; relatório `E334171FE7B0BC9A417895C8424A6FC132332BDDF5AA051132A1D20B8C059098`; summary `0C23206306F0959DE7AA035F91F838074E8F278E614C71F6D3F2F9292CC7444F`; `ProjectReadinessAudit.cs` `3366D1C53283818812B2CE3119339CE40D274C3A28CE186FB14893C4B3B685C2`; matriz `97D982C73A045366B774750550370E76357A7247C0ADD692068381D2DFD55A46`.

Próxima investigação: associação original única `state→draw` com UV/material independente ou `EVT→ABKC→sample→playback`; sobreposição nominal segue apenas controle.

## Etapa 1.04 — coocorrência independente command-descriptor e papéis numéricos — 2026-10-08

Foi criado `tools/analysis/eagl_original_command_slot_role_cooccurrence_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. A junção independente por primitiva cobriu `2364/2364` descriptors e `11820` linhas numéricas; `2364/2364` ordinais atuais estão presentes, coincidentes e unicamente identificados, sem match deslocado. Os grupos numéricos mantêm `2364` grupos de cinco, `2362` completos e `2` incompletos; os `2362` completos têm o perfil float2 de faixa unitária exclusivamente no slot bruto `1`.

O controle não converte essa relação em state→draw, posição, UV, material, atributo, geometria, GPU, skinning, rigging, animação ou morph. O readiness registra `rendering-original-command-slot-role-cooccurrence-control` como `verified-structural-negative`, `2362/2362`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; rendering permanece `not-proven` e o projeto `NOT_READY`.

Validação: Release `0/0`, self-test `92/92`, matriz completa exit code `0`, smokes, runtime headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos e `1554` samples. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256: controle `CEB60207BDEE7450D2C77A13B9BD0DF09047CAA7AFA81B4810023A97E575FA512`; relatório `5ABFB9D19F0E031B49CF8F62A0179A0CDD8D0E740290BCAEAA399883363D5CB3`; summary `6ACCCC0125CE33344EA73C98576C8BFB2A043FB5683BB6C4D9C71A81762CF6C`; `ProjectReadinessAudit.cs` `C7334F270FC4C846B570C2D00D710D76C4D375C670DF5879E8B2B5EF2592E2EA`; matriz `33143D52276B6C78094934E72CC56BD15C013B80C82F763114B15BEA7809FE6C`.

Próxima investigação: associação original única state→draw com UV/material independente ou EVT→ABKC→sample→playback.

## Etapa 1.03 — coocorrência independente estado/fetch e papéis numéricos por primitiva — 2026-10-08

Foi criado `tools/analysis/eagl_original_state_numeric_slot_cooccurrence_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O controle une, por chave de primitiva, o estado/fetch original ao controle numérico: `2364/2364` junções, `2364` grupos, `2362` grupos completos de cinco slots e `2` incompletos preservados. Nos grupos completos há `2362/2362` perfis float2 de faixa unitária exclusivamente no slot bruto `1`, nenhum em outro slot; `752` linhas têm `SHAPENAME` e `1612` têm `SetPrimitiveType`.

É evidência de coocorrência/cardinalidade, não prova de state→draw, geometria, UV, material, GPU, skinning, rigging, animação ou morph. O readiness registra `rendering-original-state-numeric-slot-cooccurrence-control` como `verified-structural-negative`, `2362/2362`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; o gate principal de rendering segue `not-proven` e o projeto segue `NOT_READY`.

Validação: Release `0/0`, self-test `92/92`, matriz completa exit code `0`, smokes, runtime headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos e `1554` samples. ISO somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Próxima investigação: procurar evidência única state→draw/UV/material ou EVT→ABKC→sample→playback, mantendo resultados ambíguos como controles estruturais.

## Etapa 1.02 — discriminação independente de papéis numéricos por slot de primitiva — 2026-10-08

Foi criado `tools/analysis/eagl_original_numeric_slot_role_discrimination_control.py`. O controle reparseia e une independentemente os TSVs originais de payload numérico e topologia por `model_archive/model_object/symbol_name`: `11820` linhas unidas, `2364` grupos, `2362` grupos completos de cinco slots e `2` grupos incompletos mantidos. Os grupos completos têm `2362/2362` com único perfil float2 de faixa unitária no slot bruto `1`, zero em outros slots, `2361` matches de bounds no slot `0` e `1` sem match. O relatório registra `semantic_binding=not-proven` e `original_semantics=not-claimed`; não é binding UV/posição/material.

O readiness registra `rendering-original-numeric-slot-role-discrimination-control` como `verified-structural`, `2362/2362`. O gate principal de rendering continua `not-proven`; assets continuam `candidate-integrated`, áudio `partially-integrated` e animação-deformação `structural-only`, sem alteração para `READY`.

Validação: Release `0/0`, self-test `92/92`, matriz completa exit code `0`, smokes, runtime headless/windowed, replays e hash da ISO. Foram verificados `10` cenários, `777` frames, `54` eventos e `1554` samples. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`; XBE não executado/recompilado.

Próxima investigação: state→draw/UV/material com evidência independente ou EVT→ABKC→sample→playback; sem promoção semântica por correlação isolada.

## Etapa 1.01 — controle independente do universo completo de dados para hashes EVT — 2026-10-08

Foi adicionado `fifa_pc/Runtime/AudioEventDataAssetHashSearchAudit.cs` e integrado o comando `audio-event-data-asset-hash-universe-audit` à matriz, ao self-test e ao `ProjectReadinessAudit`. A auditoria varre todos os assets em `data`, excluindo apenas `data/audio/eventspa.evt`, e reparseia os 17 candidatos do índice textual como palavras little-endian alinhadas: `121/121` assets válidos, `2.008.984.464` bytes, uma linha-fonte excluída e um único hit em `data/movies/advoffn.vp6`, hash `0x34214F6B`, nome `3panna_goal`, offset `0xA58D88`.

O controle independente `tools/analysis/audio_event_data_asset_hash_universe_control.py` reconcilia os `121/121` caminhos, bytes e a ocorrência. O hit está em payload comprimido VP6 de filme, portanto não é binding de áudio: não prova ABKC, sample, evento ou playback. `direct_selection_binding=not-proven` e `original_semantics=not-claimed`; nenhum binding foi promovido. Os subgates `audio-event-data-asset-hash-universe-control` e `audio-event-data-asset-hash-universe-independent-reparse` estão `verified-structural`, `121/121`; assets, rendering, áudio e animação-deformação permanecem não passados, gameplay clean-room segue passado e readiness continua `NOT_READY`.

Validação: matriz completa exit code `0`, build Release `0/0`, self-test `92/92`, smokes relevantes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras. ISO somente leitura, `2013003776` bytes, atributo `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Não houve execução ou recompilação do XBE.

Próxima etapa: continuar com evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings e sem marcar o projeto como concluído.

## Etapa 1.00 — controle independente do universo completo de assets de áudio — 2026-10-08

Foi adicionado `fifa_pc/Runtime/AudioEventAudioAssetHashSearchAudit.cs` e integrado o comando `audio-event-audio-asset-hash-universe-audit` à matriz, ao self-test e ao `ProjectReadinessAudit`. A auditoria reparseia os 17 candidatos do índice textual de `eventspa.evt` como palavras little-endian alinhadas e varre cada asset de `data/audio`, excluindo apenas o EVT-fonte: `60/60` assets válidos, `1.737.085.606` bytes, zero assets com hits, zero pares asset/hash e zero ocorrências alinhadas. Relatórios: `fifa_pc/audio-event-audio-asset-hash-universe.tsv` e `fifa_pc/audio-event-audio-asset-hash-universe-summary.tsv`.

O subgate `audio-event-audio-asset-hash-universe-control` passou como `verified-structural-negative`, `60/60`. A conclusão é um controle negativo estrutural independente; não há seleção ABKC/AST/BNK/sample, evento de gameplay ou playback, e não há promoção semântica. `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Os gates principais continuam assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` não passado, animação-deformação `structural-only` não passada e readiness `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `91/91`, smokes relevantes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. SHA-256: `AudioEventAudioAssetHashSearchAudit.cs` `9C0E3E6B34974BF2EB6BF04096CEE540934307A0788DF2AEFFC1FEFFCAF9FE05`, `Program.cs` `766DC833320707B891A75A559D337F2A2DF3BCD5270B1569C7C1DB3840B38529`, `SelfTestRunner.cs` `D13FA778813CCB60BB0122C0043F6B74954FE95348B526950867477EC089014F`, `ProjectReadinessAudit.cs` `5190DDECF723EBA9085FC8B4674414539F32C4630359E522727F3736E9B1DFF5`, matriz `D188A79CAA07E6299E5FCAA1268F6972B822ABE840128CB43D9A15C2632D6D90`, relatório `F5E9D9DF191173DAFEB5BC05D69FFA1075E18CF24CBAE194BACC85D730837002`, summary `DE86B53F51D842D1DD15799FC535CF4F1D2BD337D512786014850F9957C45DA2` e readiness `55DBF81BD71989DE4BD55F6249045C46EBD8C3BE6FD2DC01A8D47503F87C48AA`. A ISO permaneceu somente leitura, `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca por evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings, sem executar/recompilar o XBE e sem marcar o projeto como concluído.

## Etapa 0.99 — discriminação independente EVT→PCP — 2026-10-08

Foi adicionado `tools/analysis/audio_event_evt_pcp_range_discrimination_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O reparse independente cruza os nomes do `eventspa.evt` com ranges únicos de `pcpahdr/pcpadat`: `17/17` linhas nomeadas, âncoras de string, ranges de cabeçalho/dados, índices paralelos, prefixos `SCHl`, offsets e linhas de cabeçalho; o join com runtime fecha `17/17`, sem nomes ausentes. O controle raw de 32 bits para ABK, `pcpahdr` e `pcpadat` permanece zero.

O subgate `audio-event-evt-pcp-range-discrimination-control` é `verified-structural-negative`, `17/17`. A evidência é restrita a EVT→range PCP e não promove seleção de ABKC/AST/sample, evento de gameplay ou playback; `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Os gates principais não mudaram: assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` não passado, animação-deformação `structural-only` não passado e readiness `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `90/90`, smokes relevantes, headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes SHA-256: controle `6679AC5C2AA0118F8B54D5E51AFFDBEA4446310A75C891D75845FBB960667B0F`, `ProjectReadinessAudit.cs` `0F86927682D97D111B0FE7E74B2A977EA3F7944830D3BBED1E3ECA027A1F948A`, matriz `6375B68D2D377A4CB00D233D1DE6E52C5A6EF95B4E5139324C78E972AEB64E91`, relatório `FD6775F92E5A2FD1C3B065527CA1700EC943D67C299FBE6D4329DA8286E15BD2`, summary `9BD952F8207419933C68BE4BD53A2DC4006A549E637CEDF2947BCE7E7413CE86`, readiness `A270A70B1E9AB64775E5D856460E640F8A7C94EA4C5D1B8B3A08379E0CC516A5`. A ISO permaneceu somente leitura, `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar bindings discriminativos para EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings e sem executar/recompilar o XBE.

## Etapa 0.98 — identidade independente do payload de geometria candidato — 2026-10-08

O audit de topologia original passou a registrar `payload_sha256` para cada intervalo `__VB` validado. O controle `tools/analysis/eagl_original_candidate_geometry_payload_identity_control.py` reparseia os TSVs e cruza os `38` grupos de geometria com cinco slots originais, fetch candidato e identidade raw: `38/38` grupos completos, `190/190` símbolos, metadados e hashes exatos. Status `verified-independent-original-candidate-geometry-payload-identity-control`, `original_semantics=not-claimed`.

O readiness registra `rendering-original-candidate-geometry-payload-identity-control` como `verified-structural-negative`, `38/38`. A evidência é somente estrutural/byte-a-byte; não prova posição, UV, material, skinning, rigging, animação, morph, draw ou GPU. Os gates principais seguem assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` não passado, animação-deformação `structural-only` não passado e readiness `NOT_READY`.

Validação: matriz completa com exit code `0`, Release sem warnings/erros, self-test `90/90`, smokes, headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes SHA-256: controle `122CD41BEA4F936FDA11A5B504794FA06AF61FDB94A61C7FCD6D5E50A9397830`, auditor de topologia `A519959A04EB908D011ACEE9A803609C39C08174123184603BF9525E8142A379`, `ProjectReadinessAudit.cs` `6CBDE0D1E624161EC5CF6D7B393A8DBCA1C8CC8DB34AF6872F14019F6E14259A`, matriz `FBE00A4CABEBDD8E3322A734095410B4244C1955AE3131C04C41F347EC4DE437`, relatório `C2674850F4CFA7D8A173D28BE87F848893CB9A99ADEFDBE8966E8324CCDC687A`, summary `49ED6FC5AADF625DDF20266083F2FADE7AAC28E8673C1D71166B8FE8D88C6CE1` e readiness `439467451E3F16ABA72F7CEDBB5FBC3CB4409D2C06EA4592E111C5D2593702FF`. ISO somente leitura, `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca por evidência discriminativa em state→draw/UV/material, EVT→ABKC→sample→playback ou timing/pose/rig/deformação, sem inventar bindings.

## Etapa 0.97 — controle independente da discriminação do perfil estado→geometria — 2026-10-08

O novo `tools/analysis/eagl_original_state_profile_discrimination_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. Ele reparseia independentemente os relatórios de perfil e adjacência, deduplica a identidade `(model_archive, object_name, state_layer, state_primitive)` e recompõe os ties do perfil. O controle cobriu `269` linhas de perfil em `24` objetos, `38` candidatos de geometria, `180` primitivas de estado, `27` assinaturas distintas e `269/269` contagens de tie reproduzidas.

O resultado permanece negativo e discriminativo: `89/180` primitivas de estado continuam compatíveis com múltiplos candidatos de geometria; `91/180` pertencem a objetos com uma única geometria e, portanto, não oferecem competição; `0` têm zero candidatos; `15` grupos de assinatura têm múltiplos candidatos; há `0` pares de command shape exatos e `0` bindings únicos estado→geometria provados. Relatórios: `fifa_pc/eagl-original-state-profile-discrimination-control.tsv` e summary. Status `verified-independent-original-state-profile-discrimination-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`.

O readiness registra `rendering-original-state-profile-discrimination-control` como `verified-structural-negative`, `24/24`, sem promover igualdade de perfil ou objeto de geometria única a state→draw, UV, material, comando ou semântica GPU. O gate principal de rendering continua `not-proven`; assets seguem `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e readiness `NOT_READY`.

Validação completa: build Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes SHA-256: script `10BF27EB1D15145BF046D3B9DA2E35F007E9B24B091DCB57D963FAD7E6A95436`, relatório `58C5C2F672F775F36EA1A5FC7D783664AF5D3AB9593DACFB0BC77DC93D3DE0C8`, summary `899DDE1BB50ED1E09F3559CB7CCAB901131F56CA52F1150D57AE817C959D8AFB`, matriz `740D370CF96A842C64F90EC55C7F3DF97BDE409E584A3FFEFBE6D06CCD250223`, audit `A91895863CBA11E793EBBFC1C7DE82EC1ED6B27351CBA6B60C0639B571015D08`, readiness `70A663983FAFEBB5EB12ED742A0F17A5CA8C8F4CACBAD2F036962012C59724B7`. ISO: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`, somente leitura.

Próxima etapa: buscar novamente evidência discriminativa para EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação, sem inventar bindings.

## Etapa 0.96 — controle independente da seleção do decoder de bancos de áudio — 2026-10-08

O novo controle `tools/analysis/audio_bank_decoder_selection_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. Ele cruza `16` linhas do decoder em lote com `242` linhas de candidatos exaustivos ABKC/AST/BNK: `15/15` bancos decodificados coincidem em identidade, tipo e bytes de onda e `1/1` rejeição coincide com a política esperada; `16/16` controles são válidos. Status `verified-independent-audio-bank-decoder-selection-control`, `direct_selection_binding=not-proven`, `original_semantics=not-claimed`.

O resultado não estabelece seleção original de EVT, identidade de sample, evento de gameplay ou playback. O subgate `audio-bank-decoder-selection-control` está `verified-structural-negative`, `16/16`, mas áudio continua `partially-integrated` e readiness `NOT_READY`; assets, rendering e animação-deformação também continuam abertos, enquanto gameplay clean-room segue verificado.

Validação completa: Release `0/0`, self-test `90/90`, matriz exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Auditor `05E354332F47DBFF208E8F4889131A93736B6BCC184F6DDD99229F7DC968B903`, relatório `33E78F74BC593BB05C13DAFDFAA0598D3CB5D28AB4A880FC8E35697C344BA862`, summary `97EBA29D401FFA85DFEC948B31ACA166176A53E64D95441E8A8A279C4C3488B6`, matriz `0EBB914E3422067085AA3F4EB80517B774F1CE427E0048407C14EEB8FECF908E`, audit `011E2C23F5DE7360A78345F635ABF46F8A89BE4E5599B5B90DF50B81F796A2FA`, readiness `EF7275DE13DFFC595934E5BB3D95C66CF6ED3E3AD988F237737F6A53110F67EE`. ISO: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`, somente leitura.

Próxima etapa: obter evidência discriminativa de EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação, sem inventar bindings.

## Etapa 0.95 — controle independente da ambiguidade estado→material candidato — 2026-10-08

O novo `tools/analysis/eagl_original_state_candidate_material_ambiguity_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. Ele cruza, por objeto, intervalos de estado originais, tokens `SHAPENAME`, fontes candidatas de textura e a cardinalidade de geometrias adjacentes sem promover semântica original.

Resultado: `24` objetos, `218` linhas de renderização, `62` de ordem, `180` de estado e `38` de geometria; `36` intervalos não vazios, dos quais `12` têm dois candidatos adjacentes, `24` têm um e `0` têm zero. Foram reconciliadas `180/180` linhas de estado, `180/180` tokens, `180/180` fontes candidatas, `24/24` assets válidos e `24/24` controles de objeto. Não há binding único provado estado→geometria nem estado→material (`0`/`0`). A adjacência não escolhe draw, material, UV ou sampling.

Status `verified-independent-original-state-candidate-material-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. O readiness registra o subcontrole `rendering-original-state-candidate-material-ambiguity-control` como `verified-structural-negative`, `24/24`, sem fechar rendering. Assets permanecem `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa `completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes: script `FDF5A04EF7EBA97DAF7E4D093AEE0E14602AC561090B4B2A67498DE2D9DDA9DB`, relatório `3A596D0D251D8B24995EFE167881F5D1EE2FEC80491AC010DFB9DA35EFA1F832`, summary `A4569B1BD91FA455FF233B0E1931D0B76C8262A5B3A51B1386EA094E85EDD289`, matriz `625910D5E524C3A82E9379E4CB12D26EA4F17CE243C103D892B059E288FAA147`, audit `1C316A8C86E7E111DC02D911ACA21C1DC7E81522BE8685257CFF0D5AF0F79A85`, readiness `4A747F9A8F5C0D8286F7CED075C0535A7FB978CBC9D32E7E781D70E37CB1FEB9`. ISO sem escrita: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa recomendada: buscar evidência original discriminativa de UV/material, EVT→ABKC→sample→playback ou timing/pose/rig/deformação, mantendo `not-proven` e sem inventar bindings.

## Etapa 0.94 — controle independente da classificação original geometria/draw — 2026-10-08

O novo `tools/analysis/eagl_original_geometry_draw_classification_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. Ele reúne por chave de primitiva `218/218` linhas do render chain original com `218/218` linhas da associação candidata, sem chaves faltantes, em `24/24` objetos e `24/24` classificações completas.

Foram confirmadas `38` linhas classificadas como geometria e `180` como estado em ambas as fontes, com coincidência de classificação em `218/218`; nos `24/24` objetos há mais estados que geometrias. O resultado independente permanece estrutural: `state_to_geometry_unique_binding_proven_rows=0`, status `verified-independent-original-geometry-draw-classification-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Não há promoção para state-run→draw, UV, material, sampling, GPU, skinning, rigging, animação ou morph.

O `project-readiness.tsv` registra `rendering-original-geometry-draw-classification` como `verified-structural`, `218/218`, mas o gate principal de rendering continua `not-proven`; assets continuam `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e readiness `NOT_READY`.

Validação pós-etapa: Release `0/0`, self-test `90/90`, matriz completa `completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Auditor `2A7EDF85FEA1C982429A448294A3B40140CB5C557452349054ABA0CD88D3B7CE`, relatório `4FD523C19F725825862F8DFCCF0AFA626CF60BB3790046FC5EE89D2FC1AF6B6E`, summary `69ECC533AB16C9573B8EB0B18EF6027279DAA3AB28BA3BBCB880D0D080D00C94`, matriz `950D951607B618C2CB59C5005E191E9A3B05FB47BF430CA4B9B2795280553639`, agregador `032C73872F08EAF7916CFD8571CD59C13061BBF66E0FEB0DFFC8C0BEA63E713B`, readiness `0DABB2380CF5EF99F304FE696F7426DDC012884049AB9A4398D64543990BFA85`. ISO sem escrita: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.93 — controle independente da ambiguidade pose-bank→animation-bank — 2026-10-08

O novo `tools/analysis/animation_morph_pose_bank_ambiguity_control.py` foi integrado à matriz. Ele reconcilia independentemente o índice de pose-bank, os perfis ELF de animation-bank e o audit de relocations de morph: `391` linhas de pose-bank, `19/19` animation-banks ELF-válidos com metadados de símbolo e `393/393` layouts de morph parse-válidos; `159` layouts contêm pares de offsets, porém nenhum tem conjunto ou histograma de gaps exatamente correspondente a uma identidade de animation-bank.

O controle é deliberadamente negativo: há `1` nome normalizado genérico (`PosePaletteBank`), nenhuma correspondência pose-name→animation-bank, nenhuma correspondência por metadado de símbolo e `0` bindings pose-bank→animation-bank únicos provados. Status `verified-independent-animation-pose-bank-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Não foi inferido frame, pose, rig, timing, skinning ou morph original. O `project-readiness.tsv` registra o subcontrole `animation-morph-pose-bank-ambiguity-control` como `verified-structural-negative`, `391/391`, sem fechar o gate de animação-deformação.

Validação pós-etapa: Release `0/0`, self-test `90/90`, matriz completa `completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Auditor SHA-256 `6D06722268A77D919C93E698BBECE14A4D02E30337F3365D46004E8DCDE0297E`, relatório `007E51BF59267432C846B801256C0A356511EDEC5993AFB87362361454106A99`, summary `1CA4359A5B13236426FC93642B7B7F6D69D43A1D35ECB0EB738E2FFDED61CDA8`, matriz `1387E6D9723E3DFC2A04C0825698F640A95FE2A34F744D0F4602B962424F4638`, agregador `DA8118AC61A5217BDED8935AAE586689FDF7878612F3F02936114603B7EE5FB1`, readiness `CB8DF38A2ECA8FFEF203BD488E60794B3DFB285321876B2E778F90ABCD3C42E7`. Readiness continua `NOT_READY`; gameplay clean-room verificado; assets/rendering/áudio/animação-deformação ainda abertos. ISO sem escrita: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.92 — controle independente da ambiguidade estado→geometria — 2026-10-08

O novo `tools/analysis/eagl_original_state_geometry_binding_ambiguity_control.py` reconcilia independentemente os relatórios de adjacência, perfil, ordem e partição. Foram unidos `24/24` objetos, `269` linhas de perfil, `62` intervalos ordenados e `24` partições, sem identidades faltantes; cardinalidades de perfil, ordem e conjuntos de índices de estado fecharam em `24/24`. Em todos os objetos, a contagem de estados supera a de geometrias.

O resultado é um controle de ambiguidade, não uma descoberta semântica: `geometry_group_state_exact=0`, `exact_command_shape=0` e bindings estado→geometria únicos provados `0/24`. Status `verified-independent-original-state-geometry-binding-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Não há prova de estado→draw, UV/material ou GPU originais. Relatórios: `fifa_pc/eagl-original-state-geometry-binding-ambiguity-control.tsv` e summary. O `project-readiness.tsv` registra este controle como `verified-structural-negative`, `24/24`, sem fechar o gate de rendering.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Readiness continua `NOT_READY`: gameplay clean-room verificado; assets/rendering/áudio/animação-deformação ainda abertos. Auditor SHA-256 `4038AACD60D9D5210E96A9F0EA6DFAF1D5F1D4579683C480AED36BDE3F08E2EB`, agregador `E68D78D5411AAA9DEF72E1047D1F9D5FAB68E91A5E388258A93BF4129991FD6F`, relatório `B02761A085DBD5C7996BF40F149B097AA8AC130B77D4A8DE88621870A15AF0FF`, summary `7C60550132683EF27301AC355D40FF317235332F78F376328FDB31677B453206`, readiness `BB120BFB808DF9B0635DB320CCC120180E1E0C658BD24963C88756A74FE387B4`. ISO sem escrita: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.91 — controle de influência raster isolada para casos de composição — 2026-10-08

O `CandidateRenderInfluenceAudit` passou a comparar também o objeto candidato
isoladamente quando composição, oclusão ou cobertura da cena completa oculta a
mudança no frame agregado. O controle continua candidate-only e não atribui
semântica original.

Foram verificados `24/24` objetos com geometria finita/indexada e influência de
textura candidata no raster, `24/24` pela comparação isolada e `20/24` pela
cena completa. Os checksums texturizados e sólidos foram distintos em `24/24`.
Status `verified-authored`; `original_semantics=not-claimed`. Não foi inferido
UV, material, sampling, estado, draw ou GPU originais.

O subgate de influência candidata segue `verified-candidate`, `passed=True`,
`observed=24`, `expected=24`. Os gates principais seguem abertos (`assets`
candidate-integrated, `rendering` not-proven, `audio` partially-integrated e
`animation-deformation` structural-only); gameplay clean-room segue verificado
e o readiness é `NOT_READY`.

Validação pós-etapa: build Release `0/0`, self-test `90/90`, matriz completa
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtimes headless/windowed e replays determinísticos; `10` cenários, `777`
frames, `54` eventos de áudio e `1554` samples de animação. Relatório SHA-256
`EA487AC1B69906BEF0121B19588643814E118493B3A7ED8094DA134C065AFF14`, auditor
`4C1D43BF7157B782EE56290EA88C71428D37066A5CBA3D2494476E04BBCF12ED`,
`Program.cs` `C0C06AEC1147F9FA8AA699E649551FFD1A99CCC513576AE3F4FCA286AF21236F`,
readiness `D2704B5697D2B8EFBF2F2825CE1DC0A49E36089EED77227E7DD862A8CD3811A1`
e DLL Release `82DC30C3751939707D9B356D6BEDB3141A3B3DB3C40F5C2242D8BA0B7ACA25DA`.
ISO sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.90 — auditoria dinâmica de influência do atributo candidato — 2026-10-08

Foi adicionada `CandidateAttributeRasterInfluenceAudit` e o comando
`eagl-candidate-attribute-raster-influence-audit`. O auditor renderiza, no
caminho candidate-only, o mesmo mesh/textura em baseline e após a perturbação
determinística do atributo opaco slot 1 `(x,y) -> (1-x,y)`.

O controle passou em `24/24` objetos: geometria finita/indexada, cardinalidade
válida e mudança efetiva do raster; baseline e variante produziram `24`
checksums distintos cada. Status `verified-candidate-attribute-raster-influence`,
com `original_semantics=not-claimed`. A conclusão é limitada ao consumo do
valor pelo renderer gerenciado candidato; não identifica UV, material, sampling
nem binding original de estado/draw/GPU.

O subgate `assets-candidate-attribute-raster-influence` está
`verified-candidate`, `passed=True`, `observed=24`, `expected=24`. Os gates
principais permanecem `assets=candidate-integrated`/false,
`rendering=not-proven`/false, `audio=partially-integrated`/false e
`animation-deformation=structural-only`/false; readiness segue `NOT_READY`.

Artefatos: relatório SHA-256
`67F2B35CEA616716987E59442C25DF5C641D3A0CA47376E7892CC711EFA1DC47`,
auditor `08FC231178A1A7393442EC8A29884E9AADAFB2BCC5680B20B212E142C8620546`,
readiness `3D043101C88871F40E1EEE8B3B7707BC3311BE15174AB584FF570A4114449CF1`,
`Program.cs` `69ECDD321F8243981A32DBB2E9D7D3BF3CE9096656155BFEC9E9A07B78E00358`,
matriz `3F8CC844430215B34BF83C4ED8AA43AFEEBADA1CBD4CB504F49CC10937616D9C` e
DLL Release `2CCC224AB6715F6D79CEC8707DC8F3D6AA98611D7AFB744468E8723A5FF9B81D`.

Validação pós-etapa: build Release `0/0`, self-test `90/90`, matriz completa
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos; `10` cenários, `777`
frames, `54` eventos de áudio e `1554` samples de animação. ISO sem escrita:
`2013003776` bytes, `Archive`, última escrita
`2026-01-22T07:03:26.0859951-03:00`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo passo recomendado: continuar estado→draw/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação, sem marcar o projeto
como concluído.

## Etapa 0.89 — controle independente de compatibilidade estrutural atributo/textura candidata — 2026-10-08

Foi implementado `tools/analysis/eagl_candidate_attribute_texture_compatibility_control.py`.
O controle reparseia e junta por objeto os perfis numéricos candidatos e a
cadeia independente de asset/textura. Nos `24/24` joins, os inputs estavam
válidos, as dimensões de textura eram positivas, o formato era `PAL8`, os
pixels decodificados coincidiam e as faixas numéricas opacas eram finitas e
normalizadas em `24/24` rows. A cardinalidade dos `slot1_values` também
coincidiu com a contagem de vértices em `24/24` rows.

Status: `verified-independent-candidate-attribute-texture-compatibility-control`;
`semantic_binding=not-proven` e `original_semantics=not-claimed`. A faixa
normalizada e a cardinalidade são controles estruturais de candidato: não
identificam slot 1 como UV, não atribuem sampling/material e não estabelecem
binding original de estado, draw ou GPU. Readiness registrou o subgate
`assets-candidate-attribute-texture-compatibility-control` como
`verified-structural-negative`, `passed=True`, `observed=24`, `expected=24`.
Os gates principais permanecem: assets `candidate-integrated=false`, rendering
`not-proven=false`, áudio `partially-integrated=false`,
animation-deformation `structural-only=false` e `NOT_READY`.

Artefatos: relatório SHA-256
`C49F4BB877646835CFA00720667503A64B2E9F64E5A3A5557AAE675D6BB8D3B9`, resumo
`750E4B79966EF5775BE0C631B99A523F26230D3EEA926EACB27C4DE45400C859`,
readiness `B16C9A047528917D2082971B9D4D671B2AA0DFB798D0916AC1D03A0F7C10A736`,
script `7B7912822CCFCAB4A74049AB5D6B569EFF84706931A35BC8EDA9D2356C6D185A`,
matrix `73A629EB52715005FD9EFCDD0B4B108943BF597438E2CFA8539C028BCF4B669F`,
auditor de readiness `F98DAD67C21F0CFCEB0071EA9E4F66DC37E69154F7F670DAD57F63CEBFB11E45`
e DLL Release `0DCBBBA702C26D6BA01024227E3DF036DD0CA727029A4982A037376FCBEADE79`.

Validação pós-etapa: build Release `0/0`, self-test `90/90`, matriz
`completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos. A matriz confirmou `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação.
A ISO não foi escrita: `2013003776` bytes, `Archive`, última escrita
`2026-01-22T07:03:26.0859951-03:00`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo passo recomendado: continuar evidência independente de estado→draw,
UV/material sem promoção semântica, EVT→ABKC→sample→playback e
timing/pose/rig/deformação. Não marcar o projeto como concluído.

## Etapa 0.88 — controle estrutural independente de estado/geometria/bounds — 2026-10-08

Foi implementado `tools/analysis/eagl_original_render_state_geometry_bounds_control.py`.
O controle faz um join independente, restrito aos `24` objetos candidatos, entre
`eagl-original-state-geometry-partition-control.tsv` e
`eagl-original-bounds-fetch-slot-join-control.tsv`. Foram confirmados `24/24`
objetos particionados, `24/24` entradas de bounds válidas, `24/24` joins e
`24/24` rows completas; os grupos de fetch de bounds somam `218`, com `216`
grupos completos e `2` grupos esperadamente incompletos. Todos os `218` grupos
coincidem no slot `0`, nenhum coincide em slot não-zero e há `0` pares de forma
exata command-shape.

Status: `verified-independent-original-render-state-geometry-bounds-structural-control`;
`semantic_binding=not-proven` e `original_semantics=not-claimed`. Isso é apenas
controle estrutural de cardinalidade, partição e posição relativa do primeiro
fetch; não prova associação estado→draw/geometria, posição, UV, material,
skinning, rigging, animação, morph ou GPU. Readiness registrou
`rendering-original-state-geometry-bounds-control` como
`verified-structural-negative`, `passed=True`, `observed=24`, `expected=24`.
Os gates principais permanecem: assets `candidate-integrated=false`, rendering
`not-proven=false`, áudio `partially-integrated=false`,
animation-deformation `structural-only=false` e `NOT_READY`.

Artefatos: relatório SHA-256
`DFEBD93D163A551FA28905D9A05512E858F7172ECFAF285FAAEB03A35172ACDD`, resumo
`C0BC3EC18E8483589CFD37F8530CE5058063B424922EB1E670C9DA7261BAFCAA`,
readiness `E163BF21175BACA20B8D2CF8000443BFEAF0E37A64512F1C163BB5C91FBC2FB2`,
script `A078E08FC3B3A08B55CE4A1B73F3B74C13A6DD915B4D0DF1A68C9447CC4F4627`
e DLL Release `DD20BA2ADBFB50EFE5C783EDA7ED73E3BDDBEBDFE39861A68F70296B2F475893`.

Validação pós-etapa: build Release `0/0`, self-test `90/90`, matriz
`completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos. A matriz confirmou `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação.
A ISO não foi escrita: `2013003776` bytes, `Archive`, última escrita
`2026-01-22T07:03:26.0859951-03:00`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo passo recomendado: continuar evidência independente de estado→draw,
UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação. Não marcar
o projeto como concluído.

## Etapa 0.87 — controle lexical independente de categoria/ordinal dos slots candidatos — 2026-10-08

Foi implementado `tools/analysis/eagl_original_candidate_slot_category_control.py`.
O controle reparseia o relatório de fetch candidato e o relatório independente
de identidade bruta. Resultado: `218` rows candidatas, `218` rows unidas,
`24` objetos, `216` primitivas completas, `2` rows esperadamente não
materializadas, `1080` slots, `1080/1080` símbolos em formato válido,
`1080/1080` categorias iguais a `10 + slot` e `1080/1080` ordinais iguais à
primitiva; `0` chaves faltantes e `218/218` rows do controle completas.

Status: `verified-independent-original-candidate-slot-category-ordinal-control`.
O resultado é lexical/estrutural e mantém `original_semantics=not-claimed`:
não define o significado de slot, categoria ou ordinal e não promove posição,
geometria, UV, material, atributo, skinning, rigging, animação, morph, draw ou
GPU. Readiness registrou o subgate
`assets-candidate-slot-category-ordinal-control` como
`verified-structural-negative`, `passed=True`, `observed=218`, `expected=218`.
Os gates principais permanecem: assets `candidate-integrated=false`, rendering
`not-proven=false`, áudio `partially-integrated=false`,
animation-deformation `structural-only=false` e `NOT_READY`.

Artefatos: relatório SHA-256
`95ED83D5CDD52216EBEA25E3C660E4D4134A287EAAB91D7768CBEB1AEC5AAD24`, resumo
`EF7DEF8F1835E35E60BC4A25A8BF3C173C9290A028FDB5CC9133A4E42F8AF42F`,
readiness `9B82157CB82C3F53935E0561CA2CEB31852CA3E5A6EFD77E5FD982F7658CF145`
e DLL Release `DD55D70B0B1941433095F50B6B6626D48F28643491A4EB0756FCA08936D9D82E`.

Validação pós-etapa: build Release `0/0`, self-test `90/90`, matriz
`completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos. A matriz confirmou `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação.
A ISO não foi escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo passo recomendado: continuar evidência independente de render-state→draw,
UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação. Não marcar
o projeto como concluído.

## Etapa 0.86 — identidade bruta independente dos streams candidatos — 2026-10-08

Foi implementado `eagl-original-candidate-raw-stream-identity-audit` em C# e o
controle independente `tools/analysis/eagl_original_candidate_raw_stream_identity_control.py`.
O comando reabre a ISO somente para leitura, revalida os ranges `__VB` dos 24
objetos EAGL e calcula SHA-256 sobre cada payload bruto associado a fetch-slot.
Os números observados foram: `218` primitivas candidatas, `216` completas,
`1080` slots candidatos, `1090` streams brutos, `1080/1080` slots válidos,
`1080/1080` metadados exatos e hashes de payload, `216` identidades completas,
`2` rows esperadamente não materializadas e `0` objetos rejeitados.

Statuses: `verified-independent-original-candidate-raw-stream-identity-control`
e `verified-structural-negative`. Trata-se de identidade estrutural bruta,
sem atribuição de significado a slot, draw, geometria, UV, material, skinning,
rigging, animação, morph ou GPU; `original_semantics=not-claimed`. O subgate
foi registrado como `passed=True`, `observed=216`, `expected=218`, mas assets
continuam `candidate-integrated`, rendering `not-proven`, áudio
`partially-integrated` e readiness permanece `NOT_READY`.

Validação pós-etapa: build Release `0/0`, self-test `90/90`, matriz completa
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos. A matriz confirmou 10
cenários authored, 777 frames, 54 eventos de áudio e 1554 samples de animação.
A ISO não foi escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo passo recomendado: continuar evidência independente de render-state→draw,
UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação. Não marcar
o projeto como concluído.

## Etapa 0.85 — controle negativo de identidade bruta EVT/ABKC/AST — 2026-10-08

O próximo avanço implementou `audio-event-abk-raw-identity-audit` em C# e o
reparse independente `tools/analysis/audio_event_abk_raw_identity_control.py`.
O comando reabre a mesma ISO somente para leitura e valida, sem promover
seleção: `17` ranges EVT nomeados, `16` bancos ABKC, `270` entradas
`SoundEntries` não-dummy e `70` ranges AST, com `70/70` prefixos `SCHl` válidos.
Não houve identidade bruta completa entre EVT e AST/ABKC: `0` pares de hash
SHA-256 completo, `0` eventos com match, `0` pares de prefixo e `0` rejeições;
os `17` hashes EVT são distintos.

Statuses: `verified-bounded-audio-event-abk-raw-identity-negative-control` e
`verified-independent-audio-event-abk-raw-identity-control`. O resultado não
estabelece EVT→ABKC→sample→playback, não atribui semântica original e mantém
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.
O subgate correspondente é `verified-structural-negative=True`; o gate de
áudio original continua `partially-integrated=False`, rendering continua
`not-proven=False`, e o readiness permanece `NOT_READY`.

Validação pós-etapa: build Release `0/0`, self-test `89/89`, matriz completa
`completed-authored-and-candidate-opt-in` com exit code `0`, smokes relevantes,
runtime headless/windowed e replays determinísticos. A matriz confirmou 10
cenários authored, 777 frames renderizados, 54 eventos de áudio e 1554 samples
de animação. A ISO não foi escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo passo recomendado: continuar evidência independente de
estado→geometria/UV/material, de EVT→ABKC→sample→playback e de
timing/pose/rig/deformação. Não marcar o projeto como concluído.

## Instrução de abertura

Antes de editar qualquer arquivo, leia completamente, sem pular linhas:

1. `D:\projeto zeta\CONTINUAR_FIFA_STREET_2_HANDOFF.md`
2. `D:\projeto zeta\CONTINUAR_FIFA_STREET_2_HANDOFF_NOVO_CHAT.md`
3. este arquivo: `D:\projeto zeta\CONTINUAR_FIFA_STREET_2_HANDOFF_2026-10-08.md`
4. `D:\projeto zeta\INSTRUCAO_CONTINUAR_FIFA_STREET_2_ATE_FINALIZAR.md`

Depois confira o estado real dos arquivos, relatórios e código. Preserve todas
as alterações existentes.

## Regra obrigatória da ISO

Use a ISO abaixo somente em leitura:

`D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso`

Não monte a ISO com escrita, não altere atributos, não reempacote arquivos e
não gere artefatos dentro dela. O último estado verificado foi:

- tamanho: `2013003776` bytes;
- atributo: `Archive`;
- última escrita observada: `2026-01-22T07:03:26.0859951-03:00`;
- SHA-256: `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Estado comprovado no último avanço

### Etapa 0.84 — identidade bruta command/payload

Foi adicionado `eagl-original-primitive-stream-command-payload-identity-audit`
com reparse dos EAGL originais e controle Python independente. O controle
registrou `2364` primitivas, `2362` fetches completos, `10240` candidatos,
`10240` identidades SHA-256 distintas, `0` duplicatas, `0` correspondências
exatas command/payload e `2364/2364` ordinais atuais com identidade única;
`2364/2364` rows foram coerentes no controle independente. Statuses:
`verified-bounded-original-primitive-stream-command-payload-identity-control`
e `verified-independent-original-primitive-stream-command-payload-identity-control`.

É somente um controle negativo de bytes; `original_semantics=not-claimed` e
nenhum binding semântico foi inventado. O subgate é `verified-structural-negative`,
mas rendering continua `not-proven`, assets continuam não promovidos, e o
readiness permanece `NOT_READY`.

Validação: Release `0/0`, self-test `88/88`, matriz authored/candidate opt-in
com smokes, runtime headless/windowed e replays determinísticos, exit code `0`.
ISO preservada somente para leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência independente de estado→geometria/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

A etapa 0.83 adicionou o comando
`eagl-original-candidate-slot-profile-audit` e os relatórios:

- `fifa_pc/eagl-original-candidate-slot-profile-control.tsv`;
- `fifa_pc/eagl-original-candidate-slot-profile-control-summary.tsv`.

O audit reprocessa independentemente:

- `fifa_pc/eagl-original-vertex-stream-numeric.tsv`;
- `fifa_pc/eagl-candidate-fetch-association.tsv`.

A associação usa arquivo, objeto e símbolo do stream, pois o símbolo sozinho
se repete entre objetos. O resultado real foi:

- `11820` rows de streams originais;
- `218` primitivas candidatas;
- `216` primitivas candidatas completas de cinco slots;
- `216/216` registros de slot 1 associados ao stream original do mesmo objeto;
- `216/216` com símbolo, stride, byte-length e element-count coincidentes;
- `216/216` com categoria numérica original `11`;
- `216/216` com perfil float2 finito/próximo de unidade;
- status: `verified-independent-original-candidate-slot1-category11-profile-control`;
- `original_semantics=not-claimed`.

Esse resultado é evidência estrutural independente. Slot 1 e categoria 11
continuam labels opacos. Não são UV, material, normal, peso, skinning,
rigging, animação, morph ou comando GPU. Não promova essa associação para
semântica original.

## Gates atuais

O arquivo canônico é `fifa_pc/project-readiness.tsv`. O estado observado é:

| Gate | Estado | Interpretação |
|---|---|---|
| `gameplay` | `clean-room-integrated=true` | Gameplay novo em .NET, com cenários determinísticos verificados; não é decodificação das regras originais. |
| `assets` | `candidate-integrated=false` | 24 assets candidatos têm cadeia estrutural, imagem e renderer gerenciado, mas falta binding independente de mesh/texture/material no caminho original. |
| `rendering` | `not-proven=false` | Falta associação comprovada entre estado original, geometria e interpretação independente de UV/material. |
| `audio` | `partially-integrated=false` | Rotas authored funcionam, mas falta seleção original independente EVT→ABKC→sample→playback. |
| `animation-deformation` | `structural-only=false` | Há containers e perfis estruturais, mas falta timing/pose/rig/skinning/morph com decoder validado. |

Os caminhos authored e candidate opt-in estão verificados apenas como
software novo/candidato. Eles não fecham os gates de semântica original.

## Validação já executada

Na etapa 0.83 passaram:

- build Release: `0` warnings / `0` errors;
- self-test: `87/87`;
- matriz authored/candidate opt-in: exit code `0`;
- smokes relevantes, incluindo o audit de slot 1/categoria 11;
- runtime headless e windowed;
- replays determinísticos;
- verificação independente da ISO com o SHA-256 acima.

O readiness continua deliberadamente `NOT_READY`.

## Próxima etapa recomendada

Priorize evidência independente, em ordem de impacto:

1. relação do estado de renderização original com a geometria e seus limites,
   mantendo comandos/fields opacos quando não houver discriminação;
2. interpretação independente de UV/material somente se houver sinais
   repetíveis e discriminativos; não inferir pelo fato de um vetor ser float2,
   finito ou próximo de unidade;
3. caminho de áudio `EVT→ABKC→sample→playback`, sem usar containment ou
   coincidência numérica como seleção direta;
4. timing, pose, rig, skinning e morphs, somente depois de obter evidência de
   correspondência e um decoder validado;
5. integrar somente semânticas que tenham prova suficiente e atualizar o
   readiness com a distinção entre comprovado, candidato e não decodificado.

Se o próximo audit for negativo, documente-o como controle estrutural negativo
e avance para o próximo gate; não invente uma interpretação para produzir
progresso.

## Critério de finalização

Não declare o projeto concluído enquanto renderização, assets, gameplay e
áudio não estiverem integrados e verificados no escopo definido pelos gates.
Antes de uma eventual conclusão, execute novamente build Release, self-test,
smokes, runtime headless, runtime windowed quando relevante, replays
determinísticos e SHA-256 da ISO. Atualize este handoff e os documentos de
status com resultados exatos.

Não diga que o XBE foi recompilado. Não inicie code review. Não altere a ISO.

## Registro da etapa executada — 1.08 — 2026-10-08

O audit `fifa_pc/Runtime/AnimationBankCatalogTailRangeAudit.cs` e seu controle
independente em `tools/analysis/animation_bank_catalog_tail_range_control.py`
registraram uma regularidade estrutural delimitada no tail de `__AnimationBank`:
19/19 bancos, 1.584/1.584 entradas, header/contagem/intervalo/alinhamento/
comprimento/relocations válidos em 19/19 e zero intervalos sem relocations.
Status `verified-structural-animation-bank-tail-range` e controle
`verified-independent-animation-bank-catalog-tail-range-control`.

O resultado não decodifica records, frames, poses, rigs, timing, skinning,
morphs ou seleção de animação. `candidate_record_binding=not-proven` e
`original_semantics=not-claimed`; nenhum gate semântico foi promovido e o
readiness continua `NOT_READY`.

Validação concluída: Release `0/0`, self-test `94/94`, matriz código 0 com
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio, 1.554 amostras de animação, smokes, runtime headless/windowed e
replays determinísticos. ISO preservada, `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.

Continuar com as lacunas independentes de estado → draw/UV/material,
`EVT→ABKC→sample→playback` e timing/pose/rig/deformação. Não concluir, não
inventar bindings, não executar/recompilar XBE e não iniciar code review.

## Registro da etapa executada — 1.09 — 2026-10-08

O novo audit `fifa_pc/Runtime/AnimationBankTailPairRelocationAudit.cs` e seu
controle independente em `tools/analysis/animation_bank_tail_pair_relocation_control.py`
validaram o layout estrutural dos pares no tail de `__AnimationBank`: 19/19
bancos, 1.584/1.584 pares, duas relocations por par nos offsets esperados,
zero multiplicidades inesperadas, 1.584/1.584 palavras de cada lado dentro de
`.data`, zero palavras em símbolos conhecidos, 1.584 pares raw distintos e
19/19 layouts completos. O detalhe gerado tem 1.584 linhas.

Status `verified-structural-animation-bank-tail-pair-relocations` e controle
`verified-independent-animation-bank-tail-pair-relocation-control`. O achado
não prova ordinal → catálogo, record/frame/pose/rig/timing/skinning/morph ou
playback; `candidate_record_binding=not-proven`,
`original_semantics=not-claimed`. Nenhum gate semântico foi promovido e o
readiness continua `NOT_READY`.

Validação: Release `0/0`, self-test `95/95`, matriz código 0,
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras de animação; smokes, runtime headless/windowed e
replays determinísticos passaram. ISO preservada, `2013003776` bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Continuar somente com join independente demonstrável dos target-prefixes, ou
seguir nas lacunas estado → draw/UV/material, `EVT→ABKC→sample→playback` e
timing/pose/rig/deformação. Não concluir, não inventar bindings, não executar
ou recompilar XBE e não iniciar code review.

## Registro da etapa executada — 1.10 — 2026-10-08

O audit `fifa_pc/Runtime/AnimationBankTailPairTargetChainAudit.cs` e o
controle independente `tools/analysis/animation_bank_tail_pair_target_chain_control.py`
auditaram janelas de 32 bytes nos dois targets de cada par do tail.
Resultado: 19/19 bancos, 1.584/1.584 linhas e 1.584/1.584 pares com ambas as
janelas não vazias. As multiplicidades foram first `2:1, 3:17, 4:1554, 5:12`
e second `3:328, 4:1237, 6:19`; zero linhas tiveram um único relocation em
cada janela, zero segundo nível foi resolvido e zero símbolo conhecido foi
encontrado.

Status `verified-structural-animation-bank-tail-pair-target-chain` e controle
`verified-independent-animation-bank-tail-pair-target-chain-control`. O
resultado quantifica uma janela estrutural; não prova hop único,
ordinal → catálogo, record/frame/pose/rig/timing/skinning/morph ou playback.
Readiness segue `NOT_READY` e nenhum gate semântico foi promovido.

Validação: Release `0/0`, self-test `96/96`, matriz código 0,
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras; smokes, runtime headless/windowed e replays
determinísticos. ISO preservada, `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Continuar com evidência independente de record/frame/pose/rig/timing ou nas
frentes estado → draw/UV/material e `EVT→ABKC→sample→playback`; não concluir,
não inventar bindings, não executar/recompilar XBE e não iniciar code review.

## Registro da etapa executada — 1.11 — 2026-10-08

Foi adicionada ao relatório candidato a coluna `command_bytes_sha256`,
calculada sobre os bytes brutos de cada command stream. O controle independente
`tools/analysis/eagl_original_candidate_primitive_fetch_control.py` reparseou
as fontes e confirmou igualdade em `218/218` linhas reconciliadas. Os demais
controles também passaram: `218/218` contagens e streams completos, `216`
sequências de cinco slots materializadas e `2` linhas candidatas mantidas como
não materializadas.

O status permanece
`verified-independent-original-candidate-primitive-fetch-control`,
`semantic_binding=not-proven` e `original_semantics=not-claimed`. Trata-se de
identidade estrutural de bytes, não de prova de state→draw, posição, UV,
material, sampling, GPU, skinning, rigging, animação, morph ou semântica do
original. O readiness apenas registra a nova evidência; rendering continua
`not-proven`, assets `candidate-integrated`, áudio `partially-integrated`,
deformação `structural-only`, gameplay clean-room authored e projeto
`NOT_READY`.

Arquivos principais: `fifa_pc/Runtime/EaglCandidateFetchAssociationAudit.cs`,
`fifa_pc/Runtime/EaglOriginalCandidateRawStreamIdentityAudit.cs`,
`fifa_pc/Runtime/SelfTestRunner.cs`,
`tools/analysis/eagl_original_candidate_primitive_fetch_control.py`,
`fifa_pc/Runtime/ProjectReadinessAudit.cs` e
`tools/run-clean-room-authored-matrix.ps1`.

Validação: Release `0/0`, self-test `96/96`, matriz código 0,
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54
eventos de áudio e 1.554 amostras de animação; smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO preservada somente
para leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência independente discriminativa de estado
original → draw/UV/material, `EVT→ABKC→sample→playback` ou
timing/pose/rig/deformação. Não promover a igualdade raw a binding, não
concluir o projeto, não executar/recompilar XBE e não iniciar code review.

## Registro da etapa executada — 1.12 — 2026-10-08

Foi adicionado o controle independente
`tools/analysis/eagl_original_state_uid_ambiguity_control.py`, com integração
na matriz e em `ProjectReadinessAudit.cs`. A reparse por objeto e por
`RUNTIME_ALLOC::UID` encontrou `24/24` objetos unidos, `218` linhas
render-scoped, `38` geometrias e `180` estados. Todos os `38/38` geometrias e
`180/180` estados têm UID válido e existem `24` domínios compartilhados;
contudo todas as `38/38` geometrias têm múltiplos estados candidatos pelo UID
(`0` casos de candidato único, `38` múltiplos). Os `24/24` objetos permanecem
ambíguos e o binding único provado é `0`.

Status do controle: `verified-independent-original-state-runtime-uid-ambiguity-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. A linha do
readiness é `verified-structural-negative=true` somente para essa negativa
estrutural. Nenhum gate semântico foi promovido: rendering original segue
`not-proven`, assets `candidate-integrated`, áudio `partially-integrated`,
deformação `structural-only`, gameplay clean-room authored e o projeto segue
`NOT_READY`.

Validação: Release `0/0`, self-test `96/96`, matriz código 0,
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54
eventos de áudio e 1.554 amostras de animação; smokes, runtimes
headless/windowed e replays determinísticos passaram. ISO intacta e somente
leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente discriminativa de estado original
→ draw/UV/material, `EVT→ABKC→sample→playback` ou timing/pose/rig/deformação;
não promover UID a binding, não inventar bindings, não concluir o projeto,
não executar/recompilar XBE e não iniciar code review.

## Registro da etapa executada — 1.13 — 2026-10-08

Foi implementado e integrado o controle independente
`tools/analysis/eagl_original_state_uid_command_hash_ambiguity_control.py`.
Ele reparseia as fontes por objeto, por `RUNTIME_ALLOC::UID` e pelo SHA-256
dos bytes brutos do command stream, mantendo o teste como controle estrutural
de cardinalidade e sem promoção semântica.

Resultado: `24/24` objetos unidos, `218` linhas render-scoped, `38` geometrias,
`180` estados, `24` domínios UID compartilhados, `38/38` geometrias com UID
compartilhado com estados, `0` candidatos UID únicos e `38` candidatos UID
múltiplos. O cruzamento UID+hash produziu `0` candidatos em `38` geometrias,
portanto `0` bindings únicos provados; os `24/24` objetos continuam ambíguos.
Status: `verified-independent-original-state-uid-command-hash-ambiguity-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`.

A linha `rendering-original-state-uid-command-hash-ambiguity-control` do
readiness ficou `verified-structural-negative=true` somente para a negativa
estrutural. Nenhum gate semântico foi promovido: rendering original segue
`not-proven`, assets `candidate-integrated`, áudio `partially-integrated`,
deformação `structural-only`, gameplay clean-room authored e o projeto segue
`NOT_READY`.

Validação: build Release `0/0`, self-test `96/96`, matriz código 0,
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras de animação; smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO intacta e somente
para leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência independente discriminativa de estado original →
draw/UV/material, `EVT→ABKC→sample→playback` ou timing/pose/rig/deformação;
não inventar bindings, não concluir, não executar/recompilar XBE e não iniciar
code review.

## Registro da etapa executada — 1.14 — 2026-10-08

Foi implementado e integrado o controle independente
`tools/analysis/audio_event_named_payload_identity_control.py`, incluindo a
execução na matriz e a linha correspondente em
`fifa_pc/Runtime/ProjectReadinessAudit.cs`. O controle faz um join independente
por chave entre runtime nomeado, decoder EVT e correlação exaustiva, mantendo o
resultado como evidência estrutural negativa.

Os relatórios uniram `17` linhas de runtime, `17` de decode nomeado e `17` de
evento sem faltas. Houve `17/17` matches de WAVE bytes, `17/17` de contagem
PCM e `17/17` de SHA-256 PCM. Foram observadas `242` linhas de banco, `241`
decodificadas, `0` hashes de evento com match de banco e `0` pares PCM exatos.
Status: `verified-independent-audio-event-named-payload-identity-control`;
`direct_selection_binding=not-proven`; `original_semantics=not-claimed`.

O controle confirma a identidade do payload nomeado EVT nas fontes de runtime,
decode e correlação, e a consistência da entrada candidate clean-room. Ele não
seleciona amostra ABKC, não liga nome EVT a evento de gameplay e não demonstra
playback original. Portanto o readiness segue `NOT_READY`; áudio continua
`partially-integrated`, rendering original `not-proven`, assets
`candidate-integrated`, deformação `structural-only` e gameplay clean-room
authored. Nenhum gate semântico foi promovido.

Validação da etapa: build Release com `0` warnings e `0` errors, self-test
`96/96`, matriz código 0 com status
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras de animação; smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO mantida somente para
leitura: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de
`EVT→ABKC→sample→playback` ou de estado original → draw/UV/material e
timing/pose/rig/deformação; não inventar bindings, não concluir, não
executar/recompilar XBE e não iniciar code review.

## Registro da etapa executada — 1.15 — 2026-10-08

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

## Registro da etapa executada — 1.16 — 2026-10-08

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

## Registro da etapa executada — 1.17 — 2026-10-08

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

## Registro da etapa executada — 1.18 — 2026-10-08

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

## Registro da etapa executada — 1.19 — 2026-10-08

Foi implementado e integrado à matriz o controle independente
`tools/analysis/eagl_original_state_geometry_candidate_render_global_assignment_control.py`.
Ele reconcilia, pela chave de objeto, a enumeração global original
state→geometry, o batch de material/render candidato, a cadeia de proveniência
dos candidatos e o join independente state→geometry→payload, mantendo
`object_state` separado de ownership de grupo de render.

As quatro fontes foram válidas e reconciliadas em `24/24/24/24` linhas, com
`24` objetos joined e nenhuma chave faltante. A soma de candidatos de geometry
foi `38` e a soma de grupos geometry candidatos também `38`; a cardinalidade
foi igual em `24/24` objetos. O estado de objeto candidato foi exato em
`24/24`, a cadeia de fonte candidata e o join de payload foram válidos em
`24/24`, mas `render_group_state_match_rows=0`.

A enumeração manteve `12` objetos com múltiplos modelos geometry e `12` com
domínio singleton. A soma sem restrição foi `36` modelos e a enumeração
one-to-one produziu `24` modelos, apenas como diagnóstico de cardinalidade;
`unique_state_geometry_candidate_render_binding_proven_rows=0`. O resultado
não promove binding de state, draw, UV, material, grupo de render ou semântica
original.

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
headless/windowed e replays determinísticos passaram. ISO somente para leitura:
`2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a procurar evidência independente e discriminativa
para state→draw/UV/material, `EVT→ABKC→sample→playback` ou
timing→pose→rig→skinning/deformação; manter os domínios não atribuídos
explícitos, não inventar bindings, não executar ou recompilar XBE, não marcar o
projeto como concluído e não iniciar code review.

## Registro da etapa executada — 1.20 — 2026-10-08

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

## Registro de continuidade — etapa 1.24 — 2026-10-09

Foi avaliado `sp00nznet/xboxrecomp` como apoio à análise estática do XBE
Original Xbox. Usamos apenas parser e disassembler estáticos; recompilador,
runtime nativo e harness de execução ficaram excluídos. Nenhum output ou
runtime foi incorporado. Checkout externo ao workspace:
`193e2995ffaec871bb3be43a43bd66aa5fccfeb1`.

O dispatcher EAGL em `0x0031A930` contém 42 slots executáveis/39 destinos
distintos; índice 42 contém o primeiro word não executável `0x00000001`.
Foram ligados estaticamente 12/12 IDs encontrados em 2.364 streams completas
(36.779 ocorrências) a slots executáveis. Isso não decodifica handler ou
comando: `semantic_binding=not-proven`, `original_semantics=not-claimed`.
Disassembly sugere boundaries heurísticas e não deve ser tratada como prova.

Resultado integrado ao readiness apenas como `verified-structural`; rendering
permanece `not-proven`, assets `candidate-integrated`, áudio
`partially-integrated`, deformação `structural-only`, gameplay
`clean-room-integrated` e projeto `NOT_READY`. Release `0/0`, self-test
`96/96`, matriz `completed-authored-and-candidate-opt-in`, smokes, runtime
headless/windowed e replay determinístico passaram; 10 cenários, 777 frames,
54 eventos e 1.554 amostras. ISO somente leitura: `2013003776` bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Referências oficiais: [XboxRecomp](https://github.com/sp00nznet/xboxrecomp),
[guia de decompilação](https://github.com/sp00nznet/xboxrecomp/blob/main/docs/DECOMP.md).

## Registro da etapa executada — 1.23 — 2026-10-09

Foi implementado e integrado à matriz o controle independente
`tools/analysis/audio_event_static_context_global_assignment_control.py`.
Sem executar o XBE, ele reconcilia o contexto estático de literais de áudio do
retail-XBE, os literais/candidatos de caminho de `EVENTSPA.EVT` e o domínio
decodificado de payloads EVT/ABKC, sem promover contexto estático a seleção de
sample ou playback.

O resultado validou `58` xrefs estáticos, `23` literais únicos, `147` contextos,
`108` chamadas executáveis, `56` alvos distintos, `14` alvos com múltiplos
literais, `1` match aninhado e `10` matches XDVDFS. Foram validados `17` nomes
EVT e `1.061` pares candidatos, com `0` xrefs literais de evento. O domínio de
payload teve `17` eventos e `241` candidatos de banco; não houve match PCM
exato, nem hash de evento com match. Os `17` eventos permaneceram sem xref
estático; os modelos globais ficaram `952→952`, a redução foi `0` e nenhum
seletor único foi provado.

Status: `verified-independent-audio-event-static-context-global-assignment-control`;
`semantic_binding=not-proven`; `direct_selection_binding=not-proven`;
`original_semantics=not-claimed`. O readiness registra
`audio-event-static-context-global-assignment-control` como
`verified-structural-negative`, passado em `17/17`, sem alterar os gates:
áudio `partially-integrated`, rendering `not-proven`, assets
`candidate-integrated`, deformação `structural-only`, gameplay
`clean-room-integrated` e projeto `NOT_READY`.

Validação: build Release `0` warnings/`0` errors, self-test `96/96`, matriz
exit `0` com `completed-authored-and-candidate-opt-in`; smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. Foram reportados
`10` cenários, `777` frames, `54` eventos de áudio e `1.554` amostras de
animação. ISO somente para leitura: `2013003776` bytes, atributo `Archive`,
SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente e discriminativa para
`EVT→ABKC→sample→playback`, state→draw/UV/material ou
timing→pose→rig→skinning/deformação; manter os domínios não atribuídos
explícitos, não inventar bindings, não executar ou recompilar XBE, não marcar o
projeto como concluído e não iniciar code review.

## Registro da etapa executada — 1.22 — 2026-10-08

Foi implementado e integrado o controle independente
`tools/analysis/animation_catalog_pose_timing_global_assignment_control.py`.
Ele reconcilia os relatórios agregados de timestamps do catálogo AXT, âncoras de
payload por banco, índice de pose e contenção de relocation/morph para testar
globalmente se metadata de catálogo reduz o domínio pose→banco.

O controle validou `19` bancos, `1.584` entradas de catálogo, `1.584` timestamps
válidos, `19/19` bancos com pares completos de âncoras (`3.168` âncoras), `391`
poses distintas, `393` layouts morph válidos e `159` pares de contenção. Houve
`8` layouts com uma única contenção e `45` layouts com contenção ambígua, mas
nenhum gap-set exato. Todos os `391` arquivos de pose ficaram sem chave de join
com catálogo e morph; o domínio pose→banco permaneceu `19→19`, os modelos
globais `7.429→7.429` e nenhum binding único foi provado (`0`).

Status: `verified-independent-animation-catalog-pose-timing-global-assignment-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. O readiness
registra `animation-catalog-pose-timing-global-assignment-control` como
`verified-structural-negative`, passado em `391/391`; o gate principal de
animação/deformação continua `structural-only` e o projeto permanece
`NOT_READY`.

Validação da etapa: build Release `0` warnings/`0` errors, self-test `96/96`,
matriz código `0` com status `completed-authored-and-candidate-opt-in`,
cenários authored/candidate, smokes relevantes, runtime headless/windowed e
replays determinísticos passaram. ISO somente para leitura: `2013003776`
bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente e discriminativa para
`timing→pose→rig→skinning/deformação`, state→draw/UV/material ou
`EVT→ABKC→sample→playback`; manter os domínios não atribuídos explícitos, não
inventar bindings, não executar ou recompilar XBE, não marcar o projeto como
concluído e não iniciar code review.

## Registro da etapa executada — 1.21 — 2026-10-08

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

## Continuação após a etapa 1.43 (2026-10-13)

- Progresso atual é recompilação estática nativa C/C++, não reimplementação do gameplay em .NET. O patch `native_recomp/patches/0005-lift-wbinvd-coherent-host-memory.patch` mapeia somente a ordenação de `WBINVD` em memória coerente para fence C11; não implementa flush de dispositivo.
- Testes do lifter `2/2` e compilação isolada Release do chunk contendo o endereço + dispatch foram aprovados; 38 warnings redundantes preexistentes, zero erros. Alterações existentes no checkout XboxRecomp foram preservadas.
- A geração completa ainda precisa ser refeita; 71 stubs e 154 instruções não traduzidas continuam os valores válidos da geração anterior. Regerar 15.702 funções, build da biblioteca completa e avançar conforme o próximo trap alcançado.
- `0x80C0`/`OUT 1` segue sem binding; sem XBE executado, boot/menu não validados, gates originais abertos e readiness `NOT_READY`. Não usar emulação, não adulterar a ISO nem publicar XBE/assets.

## Publicação GitHub do Fut Street 2 (2026-10-13)

- Repositório público, conforme escolha explícita do usuário: [MarllonMenezes/fut-street-2](https://github.com/MarllonMenezes/fut-street-2), branch `main`. Workspace agora tem Git local; remote `origin` aponta para esse repositório via HTTPS. Snapshot completo inicial `2d396380ea6506a569ea6ef8ea3606d185268125` enviado e verificado contra o remoto.
- Apenas 304 arquivos de código, testes e documentação. `.gitignore` usa lista permitida: ISO, XBE, arquivos de jogo, mídia extraída, exports, TSV/CSV/logs, bin/obj/caches e projeto Star Wars não entram no repositório. Nenhum arquivo local foi apagado. `.gitattributes` impede conversão automática de line endings para preservar bytes/hashes históricos.
- Checagem inicial isolada detectou a omissão de `Program.cs` no pacote; lista corrigida antes de publicar. Snapshot completo extraído sem ISO/assets/relatórios: Release 0/0, self-tests .NET 105/105 e runner 10/10. Testes Python no workspace original: 57/57. Checagens de tamanho/tipo e assinaturas de credenciais conhecidas passaram.
- Build/self-test não precisam da ISO. Matriz completa e análises de dados requerem a ISO local e relatórios intermediários que não são distribuídos; preparar essas entradas antes de tentar a matriz em um novo clone. Instruções: `docs/GITHUB_PUBLICACAO.md`.
- Commit usa GitHub noreply para não publicar o e-mail pessoal do Git global. Nenhuma licença open-source foi escolhida automaticamente. Não foi criado pull request nem iniciado code review.
- ISO preservada: 2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Upload não muda gates ou readiness: `NOT_READY`; retomar a próxima investigação da etapa 1.31.

## Continuação após a etapa 1.44 (2026-10-13)

- Pilha versionada 0001–0005 reproduzida em clone limpo do XboxRecomp v0.13.1, commit `193e2995ffaec871bb3be43a43bd66aa5fccfeb1`; os hunks 0002/0003 foram corrigidos. Python focal 24/24.
- Análise estática/func-ID/ABI repetida; geração nativa `15702/15702`, 62 chunks, 1.753.971 linhas, 139 instruções sem tradução/36 mnemonics (antes 154/37), 71 stubs. Build CMake Release (`-O3 -DNDEBUG`), link, CLI `--help` e CTest seguro 2/2 passaram. Warnings no C gerado/kernel.
- Sem XBE ou `--launch` executados. ISO segue somente leitura; SHA-256 deve ser reconferido ao final de cada etapa. Detalhes em [relatório](docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md).
- Veja detalhes e limites em `docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md`. Readiness `NOT_READY`, não há boot ou gates aprovados.

## Continuação após a etapa 1.45 (2026-10-13)

- Patch 0006 implementa seis operações packed-word XMM/MMX sustentadas pelos operands originais; a geração caiu de 139/36 para 133 no-ops/32 mnemonics. Stubs: 71.
- Suíte upstream focada: 517 passed, 38 skipped; CMake Release completo, link nativo, `--help` e CTest 3/3 passaram. Teste C cobre wrapping, oito lanes XMM, shift negativo/count saturado e high multiply MMX. Coleta global requer o pacote externo `pefile`.
- XBE não executado; nenhuma alegação de boot/gates. ISO somente leitura, SHA-256 verificado. Readiness `NOT_READY`; seguir sem inventar bindings.




