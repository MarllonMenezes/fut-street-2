# Fut Street 2 PC — clean-room runtime

Este projeto é código novo para PC. Ele não executa o XBE, não depende de SDK
Xbox e não modifica a imagem original.

O nome público do projeto e aplicativo é **Fut Street 2** desde 2026-10-13.
Título Win32, banners .NET e metadados do produto usam esse nome. `fifa_pc`,
`FifaStreetPc.csproj`, namespace e assembly são identificadores técnicos
preservados para compatibilidade com os runners existentes. A ISO original
e os documentos históricos não foram renomeados nem alterados retroativamente.

## Etapa 1.07 — controle independente de nomes EVT em assets de áudio

`Runtime/AudioEventNameAssetTextSearchAudit.cs` adiciona `audio-event-name-asset-text-search-audit` e o controle `tools/analysis/audio_event_name_asset_text_search_control.py` reparseia seus TSVs. Os `17` nomes candidatos de `data/audio/eventspa.evt` foram buscados como ASCII exato em todos os `60` assets restantes de `data/audio`: `60/60` válidos, `1.737.085.606` bytes, `0` pares asset/nome com hit e `0` ocorrências. O controle independente fecha `1.020/1.020` linhas asset×nome, todas sem hit, sem duplicidades e com summary reconciliado.

É controle estrutural negativo, sem promoção de seleção ABKC/AST/BNK, sample, evento ou playback: `semantic_binding=not-proven`, `direct_selection_binding=not-proven`, `original_semantics=not-claimed`. Áudio continua parcialmente integrado; assets/rendering/animação-deformação e readiness continuam não passados. Validação completa: matriz exit code `0`, Release `0/0`, self-test `93/93`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras.

Hashes SHA-256: auditor `439D5AB62558278E5CC80CA41AB7CAED8EC7C6DC86ED46EF518363E0E3FB20F5`; controle Python `4BB18D4534BFD5E57F568DAA0627CACDF0F2AA9DE7CEBD46AB61C1C03552F90C`; relatório `9B8995864273ADF880CD02BFA2E10F5F08BA8C9F44B8D398D33F1284390A1ADD`; summary `04EF71651802C2F093C53312683264E35425BA699BD8A62711657D3D57035BF2`; controle `B745AED8D07629B5000183DE9B35C3B59544193422AD1B73825265C4B4351961`; controle summary `886FCD7A7E36E23D56D175D4D111F7C0E38F74708525A79ECACF03397D860174`; readiness `23FC205E7488ECBFCB2F0FF0A5DE25A5F13EAA879E8AFACFD5EAE262348F0FF2`.

## Etapa 1.06 — controle independente state/fetch + slot numérico + textura/raster candidata — 2026-10-08

`tools/analysis/eagl_original_state_slot1_candidate_uv_ambiguity_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia `2364` primitivas globais e, no subconjunto candidato, `218` grupos (`216` completos, `2` incompletos) e `180` estados SHAPENAME (`178` completos). O perfil near-unit observado no slot `1` aparece em `178/178` grupos SHAPENAME completos e em `216/216` grupos completos, zero em outros slots; textura/atributo e influência raster candidatas validam `24/24`; vínculos únicos state→geometria: `0`.

O resultado é controle estrutural negativo: não chama o slot `1` de UV, não prova state→draw/material e não promove textura/raster candidata a semântica original. O readiness registra `rendering-original-state-slot1-candidate-uv-ambiguity-control` como `verified-structural-negative`, `24/24`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; rendering permanece `not-proven` e o projeto `NOT_READY`.

Validação: Release `0/0`, self-test `92/92`, matriz completa exit code `0`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não foi executado nem recompilado.

Hashes SHA-256: controle `FCFF267936F6B03FD94174600D443424CEDD399E9634B253DD20E511D25B6449`; relatório `A61381C76D19CCCFBD995EBB928770B975F0EF923A383F8192ADCADC36F6ACE1`; summary `BFAF6A5AF309D92E0CAB5BBABB6270BA0D6326E3E516914E8C3B5079BFBD5C17`; `ProjectReadinessAudit.cs` `20099B92DC1BCA0F58F577A1A8EF7ADB3BE9F2BFDBD172F04A09B0EC774DE363`; matriz `CD626E7E0D90FC4F8BEE016C20978E6F69CE69BB7D0DF12F704609DC2853BEDC`.

Próxima investigação: evidência independente única state→draw/UV/material ou EVT→ABKC→sample→playback, sem inventar bindings.

## Etapa 1.05 — controle independente de ambiguidade nominal FIFAST2.csi↔EVT — 2026-10-08

`tools/analysis/fifast2_csi_evt_name_ambiguity_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia independentemente `100` nomes CSI e `17` candidatos nomeados EVT: `100/100` linhas CSI e `17/17` EVT válidas, `17` nomes EVT únicos; `10` sem sobreposição, `3` com uma, `4` ambíguos e `7` com alguma sobreposição. Não houve nome exato nem binding único provado.

O resultado é apenas controle negativo de nomes: não dá semântica ao CSI, não seleciona ABKC/AST/BNK ou amostra, não conecta gameplay a playback e não promove `Goal`, `GB`, `Tackle` ou `Panna`. O readiness registra `audio-fifast2-csi-evt-name-ambiguity-control` como `verified-structural-negative`, `17/17`, com `semantic_binding=not-proven`, `direct_selection_binding=not-proven` e `original_semantics=not-claimed`; o projeto permanece `NOT_READY`.

Validação: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, runtimes headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não foi executado nem recompilado.

Hashes SHA-256: controle `FA75AD1D77492032A54992A8611A5DDF7D75DB046EC2D82948D417653E88A942`; relatório `E334171FE7B0BC9A417895C8424A6FC132332BDDF5AA051132A1D20B8C059098`; summary `0C23206306F0959DE7AA035F91F838074E8F278E614C71F6D3F2F9292CC7444F`; `ProjectReadinessAudit.cs` `3366D1C53283818812B2CE3119339CE40D274C3A28CE186FB14893C4B3B685C2`; matriz `97D982C73A045366B774750550370E76357A7247C0ADD692068381D2DFD55A46`.

Próxima investigação: `state→draw` com evidência independente de UV/material ou `EVT→ABKC→sample→playback`, sem inventar bindings.

## Etapa 1.04 — coocorrência independente command-descriptor e papéis numéricos — 2026-10-08

`tools/analysis/eagl_original_command_slot_role_cooccurrence_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. A junção independente por primitiva fechou `2364/2364` descriptors com `11820` linhas numéricas; `2364/2364` ordinais atuais estão presentes, coincidentes e unicamente identificados, sem matches deslocados. Os grupos mantêm `2362` completos e `2` incompletos, com perfil float2 de faixa unitária exclusivamente no slot bruto `1` em `2362/2362` grupos completos.

O resultado é controle estrutural negativo: não promove o slot a UV, posição, material, atributo, geometria, draw ou GPU e não atribui skinning, rigging, animação ou morph. O readiness registra `rendering-original-command-slot-role-cooccurrence-control` como `verified-structural-negative`, `2362/2362`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; rendering permanece `not-proven` e o projeto `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não foi executado nem recompilado.

Hashes SHA-256: controle `CEB60207BDEE7450D2C77A13B9BD0DF09047CAA7AFA81B4810023A97E575FA512`; relatório `5ABFB9D19F0E031B49CF8F62A0179A0CDD8D0E740290BCAEAA399883363D5CB3`; summary `6ACCCC0125CE33344EA73C98576C8BFB2A043FB5683BB6C4D9C71A81762CF6C`; `ProjectReadinessAudit.cs` `C7334F270FC4C846B570C2D00D710D76C4D375C670DF5879E8B2B5EF2592E2EA`; matriz `33143D52276B6C78094934E72CC56BD15C013B80C82F763114B15BEA7809FE6C`.

Próxima investigação: state→draw/UV/material com evidência única ou EVT→ABKC→sample→playback, sem inventar bindings.

## Etapa 1.03 — coocorrência independente estado/fetch e papéis numéricos por primitiva — 2026-10-08

`tools/analysis/eagl_original_state_numeric_slot_cooccurrence_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. A junção independente do estado/fetch original com os papéis numéricos fechou `2364/2364` linhas, `2364` grupos, `2362` grupos completos de cinco slots e preservou `2` incompletos. Nos grupos completos, `2362/2362` têm perfil float2 de faixa unitária exclusivamente no slot bruto `1`, `0` em outro slot; `752` linhas têm `SHAPENAME` e `1612` têm `SetPrimitiveType`.

É controle estrutural negativo: não promove estado→draw, geometria, UV, material, GPU, skinning, rigging, animação ou morph. O readiness registra `rendering-original-state-numeric-slot-cooccurrence-control` como `verified-structural-negative`, `2362/2362`, com `semantic_binding=not-proven` e `original_semantics=not-claimed`; rendering permanece `not-proven` e o projeto `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não foi executado nem recompilado.

Próxima investigação: state→draw/UV/material com evidência única ou EVT→ABKC→sample→playback, sem inventar bindings.

## Etapa 1.02 — discriminação independente de papéis numéricos por slot de primitiva — 2026-10-08

`tools/analysis/eagl_original_numeric_slot_role_discrimination_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. A junção original numérico/topológica fechou `11820/11820` linhas, `2364` primitivas, `2362` grupos completos de cinco slots e preservou `2` grupos incompletos. Cada grupo completo tem exatamente um perfil float2 de faixa unitária no slot bruto `1` (`2362/2362`), zero em outros slots; o controle de bounds registra `2361` matches no slot `0` e `1` ausência. Isto é evidência estrutural, não prova UV, posição, material, geometria, skinning, rigging, animação, morph ou GPU: `semantic_binding=not-proven`, `original_semantics=not-claimed`.

O readiness registra o subgate `rendering-original-numeric-slot-role-discrimination-control` como `verified-structural`, `2362/2362`; o gate principal de rendering permanece `not-proven`, assets `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only` e o projeto `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não foi executado nem recompilado.

Próxima investigação: state→draw/UV/material com evidência independente ou EVT→ABKC→sample→playback, sem inventar bindings.

## Etapa 1.01 — controle independente do universo completo de dados para hashes EVT — 2026-10-08

`Runtime/AudioEventDataAssetHashSearchAudit.cs` adiciona o comando `audio-event-data-asset-hash-universe-audit`, integrado à matriz, ao self-test e ao `ProjectReadinessAudit`. A varredura de todo `data`, excluindo somente `data/audio/eventspa.evt`, validou `121/121` assets e `2.008.984.464` bytes. Houve um único hit alinhado em `data/movies/advoffn.vp6`: `0x34214F6B`, nome `3panna_goal`, offset `0xA58D88`.

`tools/analysis/audio_event_data_asset_hash_universe_control.py` confirma independentemente os `121/121` caminhos, bytes e o hit. Ele está em payload comprimido VP6 de filme e não prova áudio, ABKC, sample, evento ou playback; `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Os subgates do universo de dados estão `verified-structural`, `121/121`, sem alterar os gates principais: assets/rendering/áudio/animação permanecem não passados, gameplay clean-room está passado e readiness segue `NOT_READY`.

Validação: matriz exit code `0`, Release `0/0`, self-test `92/92`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não foi executado nem recompilado.

Próxima etapa: buscar evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings e sem concluir o projeto.

## Etapa 1.00 — controle independente do universo completo de assets de áudio — 2026-10-08

`Runtime/AudioEventAudioAssetHashSearchAudit.cs` adiciona o comando `audio-event-audio-asset-hash-universe-audit`, integrado à matriz, ao self-test e ao `ProjectReadinessAudit`. Ele reparseia os 17 candidatos do índice textual de `eventspa.evt` como palavras little-endian alinhadas e varre todos os assets em `data/audio`, excluindo somente o EVT-fonte: `60/60` assets válidos, `1.737.085.606` bytes, zero assets com hits, zero pares asset/hash e zero ocorrências alinhadas. Relatórios: `audio-event-audio-asset-hash-universe.tsv` e `audio-event-audio-asset-hash-universe-summary.tsv`.

O subgate `audio-event-audio-asset-hash-universe-control` está `verified-structural-negative`, `60/60`. É evidência estrutural negativa apenas; não seleciona ABKC/AST/BNK/sample, não estabelece evento de gameplay ou playback e não atribui semântica original. `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Os gates principais permanecem assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` não passado, animação-deformação `structural-only` não passada e readiness `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `91/91`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings e sem executar/recompilar o XBE.

## Etapa 0.99 — discriminação independente EVT→PCP — 2026-10-08

`tools/analysis/audio_event_evt_pcp_range_discrimination_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia independentemente os TSVs e reconcilia `17/17` nomes do `eventspa.evt` com ranges únicos de `pcpahdr/pcpadat`, cobrindo âncoras de string, ranges de cabeçalho/dados, índices paralelos, prefixos `SCHl`, nomes, offsets e linhas de cabeçalho em `17/17`; o join de runtime fecha `17/17`, sem nomes ausentes. As buscas raw de 32 bits permanecem zero para ABK, `pcpahdr` e `pcpadat`.

O subgate `audio-event-evt-pcp-range-discrimination-control` é `verified-structural-negative`, `17/17`. Ele cobre apenas EVT→range PCP e não atribui seleção de ABKC/AST/sample, evento de gameplay ou playback; `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Os gates principais continuam assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` não passado, animação-deformação `structural-only` não passado e readiness `NOT_READY`.

Validação: matriz exit code `0`, Release `0/0`, self-test `90/90`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. Hashes: controle `6679AC5C2AA0118F8B54D5E51AFFDBEA4446310A75C891D75845FBB960667B0F`, `ProjectReadinessAudit.cs` `0F86927682D97D111B0FE7E74B2A977EA3F7944830D3BBED1E3ECA027A1F948A`, matriz `6375B68D2D377A4CB00D233D1DE6E52C5A6EF95B4E5139324C78E972AEB64E91`, relatório `FD6775F92E5A2FD1C3B065527CA1700EC943D67C299FBE6D4329DA8286E15BD2`, summary `9BD952F8207419933C68BE4BD53A2DC4006A549E637CEDF2947BCE7E7413CE86`, readiness `A270A70B1E9AB64775E5D856460E640F8A7C94EA4C5D1B8B3A08379E0CC516A5`. ISO somente leitura: `2013003776` bytes, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings e sem executar/recompilar o XBE.

## Etapa 0.98 — identidade independente do payload de geometria candidato — 2026-10-08

O audit original de topologia agora inclui `payload_sha256` dos intervalos `__VB` validados. `tools/analysis/eagl_original_candidate_geometry_payload_identity_control.py` reparseia independentemente os relatórios e confirma `38/38` grupos de geometria completos, com `190/190` símbolos, metadados e hashes SHA-256 de payload exatos. Status `verified-independent-original-candidate-geometry-payload-identity-control`, `original_semantics=not-claimed`.

O subgate `rendering-original-candidate-geometry-payload-identity-control` está `verified-structural-negative`, `38/38`. É evidência estrutural de identidade de bytes, não de posição, UV, material, skinning, rigging, animação, morph, draw ou GPU. Os gates principais continuam assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` não passado, animação-deformação `structural-only` não passado e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. Hashes SHA-256: controle `122CD41BEA4F936FDA11A5B504794FA06AF61FDB94A61C7FCD6D5E50A9397830`, auditor de topologia `A519959A04EB908D011ACEE9A803609C39C08174123184603BF9525E8142A379`, `ProjectReadinessAudit.cs` `6CBDE0D1E624161EC5CF6D7B393A8DBCA1C8CC8DB34AF6872F14019F6E14259A`, matriz `FBE00A4CABEBDD8E3322A734095410B4244C1955AE3131C04C41F347EC4DE437`, relatório `C2674850F4CFA7D8A173D28BE87F848893CB9A99ADEFDBE8966E8324CCDC687A`, summary `49ED6FC5AADF625DDF20266083F2FADE7AAC28E8673C1D71166B8FE8D88C6CE1` e readiness `439467451E3F16ABA72F7CEDBB5FBC3CB4409D2C06EA4592E111C5D2593702FF`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência discriminativa em state→draw/UV/material, EVT→ABKC→sample→playback ou timing/pose/rig/deformação, sem inventar bindings.

## Etapa 0.97 — controle independente da discriminação do perfil estado→geometria — 2026-10-08

`tools/analysis/eagl_original_state_profile_discrimination_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. O controle reparseia os relatórios de perfil e adjacência, deduplica a identidade `(model_archive, object_name, state_layer, state_primitive)` e reproduz `269/269` contagens de tie em `269` linhas, `24` objetos, `38` candidatos de geometria e `180` primitivas de estado; há `27` assinaturas distintas.

O resultado é uma discriminação estrutural negativa: `89/180` primitivas de estado têm múltiplos candidatos de geometria; `91/180` estão em objetos com uma única geometria e não oferecem competição; `0` têm zero candidatos; `15` grupos de assinatura têm múltiplos candidatos; `0` pares de command shape exatos e `0` bindings únicos estado→geometria foram provados. Status `verified-independent-original-state-profile-discrimination-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Relatórios: `fifa_pc/eagl-original-state-profile-discrimination-control.tsv` e summary.

O readiness registra `rendering-original-state-profile-discrimination-control` como `verified-structural-negative`, `24/24`, sem converter perfil de estado ou objeto de geometria única em state→draw, UV, material, comando ou GPU. O gate principal de rendering permanece `not-proven`; assets seguem `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes SHA-256: script `10BF27EB1D15145BF046D3B9DA2E35F007E9B24B091DCB57D963FAD7E6A95436`, relatório `58C5C2F672F775F36EA1A5FC7D783664AF5D3AB9593DACFB0BC77DC93D3DE0C8`, summary `899DDE1BB50ED1E09F3559CB7CCAB901131F56CA52F1150D57AE817C959D8AFB`, matriz `740D370CF96A842C64F90EC55C7F3DF97BDE409E584A3FFEFBE6D06CCD250223`, audit `A91895863CBA11E793EBBFC1C7DE82EC1ED6B27351CBA6B60C0639B571015D08`, readiness `70A663983FAFEBB5EB12ED742A0F17A5CA8C8F4CACBAD2F036962012C59724B7`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar com evidência discriminativa de EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação; não inventar bindings.

## Etapa 0.96 — controle independente da seleção do decoder de bancos de áudio — 2026-10-08

`tools/analysis/audio_bank_decoder_selection_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. O controle cruza independentemente `16` bancos do decoder em lote com `242` candidatos exaustivos ABKC/AST/BNK: `15/15` aceitos coincidem em identidade, tipo e bytes de onda e `1/1` rejeição coincide com a política esperada. Resultado: `16/16` controles válidos, status `verified-independent-audio-bank-decoder-selection-control`, `direct_selection_binding=not-proven`, `original_semantics=not-claimed`.

Isso confirma apenas a consistência do decoder gerenciado e não promove seleção original de EVT, sample, evento ou playback. O readiness registra `audio-bank-decoder-selection-control` como `verified-structural-negative`, `16/16`; o gate de áudio permanece `partially-integrated` e o projeto `NOT_READY`. Validação: Release `0/0`, self-test `90/90`, matriz exit code `0`, smokes relevantes, headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. Auditor `05E354332F47DBFF208E8F4889131A93736B6BCC184F6DDD99229F7DC968B903`, relatório `33E78F74BC593BB05C13DAFDFAA0598D3CB5D28AB4A880FC8E35697C344BA862`, summary `97EBA29D401FFA85DFEC948B31ACA166176A53E64D95441E8A8A279C4C3488B6`, matriz `0EBB914E3422067085AA3F4EB80517B774F1CE427E0048407C14EEB8FECF908E`, audit `011E2C23F5DE7360A78345F635ABF46F8A89BE4E5599B5B90DF50B81F796A2FA`, readiness `EF7275DE13DFFC595934E5BB3D95C66CF6ED3E3AD988F237737F6A53110F67EE`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar com evidência discriminativa EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação; não inventar bindings.

## Etapa 0.95 — controle independente da ambiguidade estado→material candidato — 2026-10-08

`tools/analysis/eagl_original_state_candidate_material_ambiguity_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. O controle cruza intervalos de estado originais, tokens `SHAPENAME`, fontes candidatas de textura e cardinalidade de geometrias adjacentes, sem atribuir semântica original.

A cobertura foi de `24` objetos, `218` linhas de renderização, `62` de ordem, `180` de estado e `38` de geometria. Houve `36` intervalos não vazios: `12` com dois candidatos adjacentes, `24` com um e `0` com zero. Foram validadas `180/180` junções de estado, `180/180` matches de token, `180/180` fontes candidatas, `24/24` assets válidos e `24/24` controles completos. Bindings únicos estado→geometria e estado→material: `0`; a adjacência não prova draw, material, UV ou sampling.

Status `verified-independent-original-state-candidate-material-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. O readiness registra `rendering-original-state-candidate-material-ambiguity-control` como `verified-structural-negative`, `24/24`, sem fechar rendering. O projeto permanece `NOT_READY`: assets `candidate-integrated`, rendering `not-proven`, áudio `partially-integrated`, animação-deformação `structural-only` e gameplay clean-room verificado.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes SHA-256: script `FDF5A04EF7EBA97DAF7E4D093AEE0E14602AC561090B4B2A67498DE2D9DDA9DB`, relatório `3A596D0D251D8B24995EFE167881F5D1EE2FEC80491AC010DFB9DA35EFA1F832`, summary `A4569B1BD91FA455FF233B0E1931D0B76C8262A5B3A51B1386EA094E85EDD289`, matriz `625910D5E524C3A82E9379E4CB12D26EA4F17CE243C103D892B059E288FAA147`, audit `1C316A8C86E7E111DC02D911ACA21C1DC7E81522BE8685257CFF0D5AF0F79A85`, readiness `4A747F9A8F5C0D8286F7CED075C0535A7FB978CBC9D32E7E781D70E37CB1FEB9`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência original discriminativa de UV/material, EVT→ABKC→sample→playback ou timing/pose/rig/deformação; não inventar bindings.

## Etapa 0.94 — controle independente da classificação original geometria/draw — 2026-10-08

`tools/analysis/eagl_original_geometry_draw_classification_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. O controle junta por chave de primitiva `218/218` linhas do render chain original a `218/218` linhas da associação candidata, sem chaves faltantes, em `24/24` objetos e `24/24` classificações completas.

A classificação coincidiu em `218/218`: `38` linhas de geometria e `180` de estado em ambas as fontes; os `24/24` objetos têm mais estados que geometrias. `state_to_geometry_unique_binding_proven_rows=0`. Status `verified-independent-original-geometry-draw-classification-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. É classificação estrutural apenas; não prova state-run→draw, UV, material, sampling, GPU, skinning, rigging, animação ou morph.

O readiness registra `rendering-original-geometry-draw-classification` como `verified-structural`, `218/218`, sem fechar o gate de rendering. O projeto permanece `NOT_READY`: assets `candidate-integrated`, rendering `not-proven`, áudio `partially-integrated`, animação-deformação `structural-only` e gameplay clean-room verificado.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes: auditor `2A7EDF85FEA1C982429A448294A3B40140CB5C557452349054ABA0CD88D3B7CE`, relatório `4FD523C19F725825862F8DFCCF0AFA626CF60BB3790046FC5EE89D2FC1AF6B6E`, summary `69ECC533AB16C9573B8EB0B18EF6027279DAA3AB28BA3BBCB880D0D080D00C94`, matriz `950D951607B618C2CB59C5005E191E9A3B05FB47BF430CA4B9B2795280553639`, agregador `032C73872F08EAF7916CFD8571CD59C13061BBF66E0FEB0DFFC8C0BEA63E713B`, readiness `0DABB2380CF5EF99F304FE696F7426DDC012884049AB9A4398D64543990BFA85`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.93 — controle independente da ambiguidade pose-bank→animation-bank — 2026-10-08

`tools/analysis/animation_morph_pose_bank_ambiguity_control.py` foi integrado à matriz para reconciliar o índice de pose-bank, os perfis ELF de animation-bank e o audit de relocations de morph. Foram validadas `391` linhas de pose-bank, `19/19` animation-banks ELF-válidos com metadados de símbolo e `393/393` layouts de morph parse-válidos; `159` layouts contêm pares de offsets, sem conjunto ou histograma de gaps exatamente correspondente a uma identidade de animation-bank.

O resultado é um controle negativo: há somente `1` nome normalizado genérico (`PosePaletteBank`), nenhuma correspondência de nome ou metadado de símbolo e `0` bindings pose-bank→animation-bank únicos provados. Status `verified-independent-animation-pose-bank-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Não prova frame, pose, rig, timing, skinning ou morph original. O `project-readiness.tsv` registra o controle como `verified-structural-negative`, `391/391`, sem fechar o gate de animação-deformação.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes: auditor `6D06722268A77D919C93E698BBECE14A4D02E30337F3365D46004E8DCDE0297E`, relatório `007E51BF59267432C846B801256C0A356511EDEC5993AFB87362361454106A99`, summary `1CA4359A5B13236426FC93642B7B7F6D69D43A1D35ECB0EB738E2FFDED61CDA8`, matriz `1387E6D9723E3DFC2A04C0825698F640A95FE2A34F744D0F4602B962424F4638`, readiness `CB8DF38A2ECA8FFEF203BD488E60794B3DFB285321876B2E778F90ABCD3C42E7`. O projeto continua `NOT_READY`; gameplay clean-room está verificado e assets/rendering/áudio/animação-deformação permanecem abertos. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.92 — controle independente da ambiguidade estado→geometria — 2026-10-08

`tools/analysis/eagl_original_state_geometry_binding_ambiguity_control.py` foi
integrado à matriz e reconcilia independentemente adjacência, perfis, ordem e
partições. O controle uniu `24/24` objetos, `269` perfis, `62` intervalos e
`24` partições sem identidades faltantes; cardinalidades de perfil/ordem e
conjuntos de índices de estado fecharam em `24/24`, e os `24/24` objetos têm
mais estados que geometrias.

É um controle negativo: `geometry_group_state_exact=0`, `exact_command_shape=0`
e binding estado→geometria único provado `0/24`. Status
`verified-independent-original-state-geometry-binding-ambiguity-control`,
`semantic_binding=not-proven`, `original_semantics=not-claimed`. Não prova
estado→draw, UV, material ou GPU originais. Relatórios:
`fifa_pc/eagl-original-state-geometry-binding-ambiguity-control.tsv` e summary.
O `project-readiness.tsv` registra o controle como
`verified-structural-negative`, `24/24`, sem fechar o gate de rendering.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes relevantes, runtimes headless/windowed e replays determinísticos; `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples. O projeto
continua `NOT_READY`; gameplay clean-room está verificado e assets/rendering/
áudio/animação-deformação permanecem abertos. ISO somente leitura: `2013003776`
bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Auditor SHA-256 `4038AACD60D9D5210E96A9F0EA6DFAF1D5F1D4579683C480AED36BDE3F08E2EB`,
agregador `E68D78D5411AAA9DEF72E1047D1F9D5FAB68E91A5E388258A93BF4129991FD6F`,
relatório `B02761A085DBD5C7996BF40F149B097AA8AC130B77D4A8DE88621870A15AF0FF`,
readiness `BB120BFB808DF9B0635DB320CCC120180E1E0C658BD24963C88756A74FE387B4`.

## Etapa 0.91 — controle de influência raster isolada para casos de composição — 2026-10-08

`CandidateRenderInfluenceAudit` agora compara também o raster do objeto
candidato isoladamente quando a composição da cena completa oculta a mudança.
Foram verificados `24/24` objetos com geometria finita/indexada e influência de
textura candidata, incluindo `24/24` isolados; `20/24` diferiram na cena
completa. Checksums texturizados e sólidos distintos: `24/24`.

Status `verified-authored`, `original_semantics=not-claimed`: a evidência é
somente do renderer candidato gerenciado e não identifica UV, material,
sampling, estado, draw ou GPU originais. O subgate permanece
`verified-candidate`, `passed=True`, `observed=24`, `expected=24`; o projeto
continua `NOT_READY`, com assets/rendering/áudio/animação abertos e gameplay
clean-room verificado.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtimes headless/windowed e replays determinísticos; `10` cenários,
`777` frames, `54` eventos de áudio e `1554` samples. ISO somente leitura:
`2013003776` bytes, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Relatório `EA487AC1B69906BEF0121B19588643814E118493B3A7ED8094DA134C065AFF14`,
auditor `4C1D43BF7157B782EE56290EA88C71428D37066A5CBA3D2494476E04BBCF12ED`,
`Program.cs` `C0C06AEC1147F9FA8AA699E649551FFD1A99CCC513576AE3F4FCA286AF21236F`,
readiness `D2704B5697D2B8EFBF2F2825CE1DC0A49E36089EED77227E7DD862A8CD3811A1`
e DLL Release `82DC30C3751939707D9B356D6BEDB3141A3B3DB3C40F5C2242D8BA0B7ACA25DA`.

## Etapa 0.90 — auditoria dinâmica de influência do atributo candidato — 2026-10-08

`CandidateAttributeRasterInfluenceAudit` e o comando
`eagl-candidate-attribute-raster-influence-audit` comparam, no caminho
candidate-only, o raster baseline com o raster do mesmo mesh/textura após a
perturbação determinística do atributo opaco slot 1 `(x,y) -> (1-x,y)`.

O resultado foi `24/24` em geometria finita/indexada, cardinalidade válida e
mudança de raster; houve `24` checksums baseline e `24` perturbados distintos.
Status `verified-candidate-attribute-raster-influence`,
`original_semantics=not-claimed`. É influência comprovada apenas no renderer
candidato gerenciado: não identifica slot 1 como UV, nem atribui material,
sampling, estado, draw ou GPU originais. Readiness registrou o subgate
`assets-candidate-attribute-raster-influence` como `verified-candidate`,
`passed=True`, `observed=24`, `expected=24`; o projeto continua `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtimes headless/windowed e replays determinísticos; `10` cenários,
`777` frames, `54` eventos de áudio e `1554` samples. ISO somente leitura:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Artefatos: relatório
`67F2B35CEA616716987E59442C25DF5C641D3A0CA47376E7892CC711EFA1DC47`, auditor
`08FC231178A1A7393442EC8A29884E9AADAFB2BCC5680B20B212E142C8620546`, readiness
`3D043101C88871F40E1EEE8B3B7707BC3311BE15174AB584FF570A4114449CF1`,
`Program.cs` `69ECDD321F8243981A32DBB2E9D7D3BF3CE9096656155BFEC9E9A07B78E00358`,
matriz `3F8CC844430215B34BF83C4ED8AA43AFEEBADA1CBD4CB504F49CC10937616D9C` e
DLL Release `2CCC224AB6715F6D79CEC8707DC8F3D6AA98611D7AFB744468E8723A5FF9B81D`.

Próxima etapa: continuar evidência independente de estado→draw/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.89 — controle independente de compatibilidade estrutural atributo/textura candidata — 2026-10-08

`tools/analysis/eagl_candidate_attribute_texture_compatibility_control.py` faz
um join independente dos `24` perfis numéricos candidatos com os `24` assets
same-trunk. Inputs, dimensões positivas, `PAL8` e pixels decodificados foram
válidos em `24/24` rows; as faixas opacas foram finitas/normalizadas em `24/24`
rows e `slot1_values` coincidiu com vértices em `24/24` rows.

Status: `verified-independent-candidate-attribute-texture-compatibility-control`,
`semantic_binding=not-proven`, `original_semantics=not-claimed`. O controle não
identifica slot 1 como UV, não atribui sampling/material e não estabelece
binding original de estado, draw ou GPU. O subgate é
`verified-structural-negative`, `observed=24`, `expected=24`; assets/rendering
originais, áudio e deformação continuam abertos e readiness é `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtimes headless/windowed e replays determinísticos. Foram confirmados
`10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. ISO
somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Artefatos: relatório
`C49F4BB877646835CFA00720667503A64B2E9F64E5A3A5557AAE675D6BB8D3B9`, resumo
`750E4B79966EF5775BE0C631B99A523F26230D3EEA926EACB27C4DE45400C859`,
readiness `B16C9A047528917D2082971B9D4D671B2AA0DFB798D0916AC1D03A0F7C10A736`,
script `7B7912822CCFCAB4A74049AB5D6B569EFF84706931A35BC8EDA9D2356C6D185A` e
DLL Release `0DCBBBA702C26D6BA01024227E3DF036DD0CA727029A4982A037376FCBEADE79`.

Próxima etapa: continuar evidência independente de estado→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.88 — controle estrutural independente de estado/geometria/bounds — 2026-10-08

`tools/analysis/eagl_original_render_state_geometry_bounds_control.py` faz um
join independente, restrito aos `24` objetos candidatos, entre a partição de
estado/geometria e o controle de bounds/fetch. Foram verificados `24/24`
objetos, `24/24` entradas válidas, `24/24` joins e `24/24` rows completas;
`218` grupos de bounds, `216` completos e `2` esperadamente incompletos.
Houve `218/218` coincidências no slot `0`, `0` em slots não-zero e `0` pares
command-shape exatos.

Status: `verified-independent-original-render-state-geometry-bounds-structural-control`;
`semantic_binding=not-proven`, `original_semantics=not-claimed`. O controle é
estrutural e não atribui significado original a estado, draw, geometria,
posição, UV, material, skinning, rigging, animação, morph ou GPU. O subgate é
`verified-structural-negative`, `observed=24`, `expected=24`; os gates originais
de assets/rendering, áudio e deformação continuam abertos e readiness é
`NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtimes headless/windowed e replays determinísticos. Foram confirmados
`10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. ISO
somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Artefatos: relatório
`DFEBD93D163A551FA28905D9A05512E858F7172ECFAF285FAAEB03A35172ACDD`, resumo
`C0BC3EC18E8483589CFD37F8530CE5058063B424922EB1E670C9DA7261BAFCAA`,
readiness `E163BF21175BACA20B8D2CF8000443BFEAF0E37A64512F1C163BB5C91FBC2FB2`,
script `A078E08FC3B3A08B55CE4A1B73F3B74C13A6DD915B4D0DF1A68C9447CC4F4627`
e DLL Release `DD20BA2ADBFB50EFE5C783EDA7ED73E3BDDBEBDFE39861A68F70296B2F475893`.

Próxima etapa: continuar evidência independente de estado→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.87 — controle lexical independente de categoria/ordinal dos slots candidatos — 2026-10-08

`tools/analysis/eagl_original_candidate_slot_category_control.py` reparseia
independentemente os relatórios de fetch candidato e identidade bruta. Foram
verificados `218` rows, `24` objetos, `216` primitivas completas, `2` rows
esperadamente não materializadas e `1080` slots; formato, categoria `10 + slot`
e ordinal da primitiva foram coerentes em `1080/1080` rows cada.

Status: `verified-independent-original-candidate-slot-category-ordinal-control`,
`original_semantics=not-claimed`. O controle mantém os símbolos `__GPvb` opacos:
não define slot, posição, geometria, UV, material, skinning, rigging, animação,
morph, draw ou GPU. O subgate é `verified-structural-negative`, com
`observed=218`, `expected=218`; assets/rendering originais permanecem não
comprovados, áudio permanece `partially-integrated` e readiness é `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtimes headless/windowed e replays determinísticos. Foram confirmados
`10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. ISO
somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Artefatos: relatório
`95ED83D5CDD52216EBEA25E3C660E4D4134A287EAAB91D7768CBEB1AEC5AAD24`, resumo
`EF7DEF8F1835E35E60BC4A25A8BF3C173C9290A028FDB5CC9133A4E42F8AF42F`.

Próxima etapa: buscar evidência independente de render-state→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.86 — identidade bruta independente dos streams candidatos — 2026-10-08

`eagl-original-candidate-raw-stream-identity-audit` e o reparse independente
`tools/analysis/eagl_original_candidate_raw_stream_identity_control.py` releram
os 24 objetos EAGL e os ranges `__VB` da ISO em modo somente leitura. O
resultado foi `218` primitivas candidatas, `216` completas, `1080` slots
materializados, `1090` streams brutos, `1080/1080` slots válidos, metadados
exatos e hashes SHA-256 de payload, `216` identidades completas, `2` rows
esperadamente não materializadas e `0` rejeições.

Isso é somente um controle de identidade de bytes, com status
`verified-independent-original-candidate-raw-stream-identity-control` e
`original_semantics=not-claimed`: não atribui significado a slot, geometria,
UV, material, skinning, rigging, animação, morph, draw ou GPU. O subgate está
`verified-structural-negative` (`observed=216`, `expected=218`); assets e
rendering originais permanecem não comprovados, áudio permanece
`partially-integrated` e readiness permanece `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes relevantes, runtime headless/windowed e replays determinísticos. Foram
confirmados 10 cenários authored, 777 frames, 54 eventos de áudio e 1554
samples de animação. ISO preservada somente para leitura: `2013003776` bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar evidência independente de render-state→draw,
UV/material, EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.85 — controle negativo de identidade bruta EVT/ABKC/AST — 2026-10-08

`audio-event-abk-raw-identity-audit` e o reparse independente
`tools/analysis/audio_event_abk_raw_identity_control.py` reabriram a ISO em
modo somente leitura. Foram validados `17` ranges EVT, `16` bancos ABKC, `270`
entradas `SoundEntries` não-dummy e `70/70` ranges AST com `SCHl`. Os `17`
hashes EVT são distintos; os controles encontraram `0` pares de hash bruto
completo, `0` eventos com match, `0` pares de prefixo e `0` rejeições.

Isso é apenas um controle negativo estrutural: não seleciona ABKC/sample, não
liga EVT a gameplay/playback e não reivindica semântica original
(`direct_selection_binding=not-proven`, `original_semantics=not-claimed`). O
subgate foi registrado como `verified-structural-negative`; o gate de áudio
original continua `partially-integrated`, rendering continua `not-proven` e
readiness continua `NOT_READY`.

Validação: Release `0/0`, self-test `89/89`, matriz completa exit code `0`,
smokes relevantes, runtimes headless/windowed e replays determinísticos. A
matriz authored cobriu 10 cenários, 777 frames, 54 eventos de áudio e 1554
samples de animação. ISO preservada somente para leitura: `2013003776` bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar evidência independente de estado→geometria/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.84 — controle independente de identidade command/payload — 2026-10-08

`eagl-original-primitive-stream-command-payload-identity-audit` compara os
pacotes brutos command 9 dos objetos EAGL originais com identidades SHA-256
limitadas dos candidatos `__VB` (record, descritor e cabeça/cauda do payload).
O resultado real foi `2364` primitivas, `2362` fetches completos, `10240`
candidatos, `10240` identidades distintas, `0` duplicatas e `0` matches exatos;
`2364/2364` ordinais atuais têm identidade única. O controle Python independente
reproduziu os contadores e `2364/2364` rows coerentes.

É um controle negativo de bytes, com `original_semantics=not-claimed`: não
define UV, material, geometria, normal, peso, skinning, rigging, animação,
morph, comando ou GPU. O subgate é `verified-structural-negative`, enquanto
assets/rendering originais permanecem não comprovados e readiness é `NOT_READY`.

Validação da etapa: Release `0/0`, self-test `88/88`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. ISO somente
leitura, intacta: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar evidência independente para estado→geometria/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.83 — controle independente de slot 1/categoria numérica 11 — 2026-10-08

`eagl-original-candidate-slot-profile-audit` reprocessa os relatórios
`eagl-original-vertex-stream-numeric.tsv` e
`eagl-candidate-fetch-association.tsv` com chave composta por arquivo,
objeto e símbolo. Na ISO, `216/216` primitivas candidatas completas unem slot
1 ao stream original do mesmo objeto com símbolo, stride, byte-length e
element-count exatos; `216/216` têm categoria numérica original `11` e perfil
float2 finito/próximo de unidade. O reparse contou `11820` streams originais,
`218` primitivas candidatas e `216` completas.

Esse resultado é apenas controle estrutural independente:
`original_semantics=not-claimed`. Não atribui a slot 1/categoria 11 qualquer
semântica de UV, material, normal, peso, skinning, rigging, animação, morph
ou GPU. Readiness permanece `NOT_READY`; os gates originais de assets,
rendering, áudio e animação/deformação continuam abertos.

Validação: matriz exit code `0`, Release `0/0`, self-test `87/87`, smokes,
runtimes e replays determinísticos. ISO preservada sem escrita:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.82 — controle posicional de campos `.off` para words CSI — 2026-10-08

`fifast2-csi-opaque-field-audit` comparou os `100` words opacos de
`FIFAST2.csi` com as três colunas e o ordinal das linhas de `pcpahdr.off` e
`pcpadat.off`, sem atribuir semântica aos campos. O resultado foi
`pcpahdr any_column=1/100` (`column2=1`), `pcpadat any_column=0/100` e
`row_ordinal=3/100` em cada tabela. São coincidências estruturais fracas;
`opaque_word_semantics=not-decoded`, `direct_selection_binding=not-proven` e
`original_semantics=not-claimed` permanecem.

Matriz authored/candidate opt-in concluída com exit code `0`: build Release
`0/0`, self-test `86/86`, smokes, runtimes headless/windowed e replays
determinísticos passaram; readiness continua `NOT_READY`. A ISO segue sem
escrita, com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.81 — controle de boundaries dos words CSI — 2026-10-08

`fifast2-csi-opaque-range-audit` comparou os `100` words opacos com as tabelas
reais de `pcpahdr` e `pcpadat`. Ambas têm `283` rows e cobrem integralmente os
companions. Houve containment `100/100`, mas nenhuma coincidência com início ou
fim de range (`0/100` em cada teste); isso não identifica header, data, sample
ou playback.

O status é `verified-structural-csi-opaque-range-boundary-control`, mantendo
`opaque_word_semantics=not-decoded`, `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`. Release `0/0`, self-test `85/85`, readiness
`NOT_READY`, ISO preservada com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.80 — namespace de hashes tuna/CSI — 2026-10-08

`fifast2-csi-tuna-audit` validou somente a estrutura: `tuna.big` é um `BIGF`
de `6313` bytes com `tunavars.txt` (`106/106` hashes compatíveis com o
candidato DJB2-like `0x1505`/`0x21`), e `FIFAST2.csi` possui `100/100` rows
válidas com `89` nomes em comum. Os words opacos não coincidem com os hashes
(`0/89`); estar dentro do comprimento de `pcpahdr.big` é apenas evidência de
faixa, não associação a header, sample ou playback.

`direct_selection_binding=not-proven`, `opaque_word_semantics=not-decoded` e
`original_semantics=not-claimed`. O gate principal de áudio continua parcial;
rendering e animação/deformação continuam não comprovados. Release `0/0`,
self-test `84/84`, readiness `NOT_READY`, ISO preservada com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.79 — índice estrutural candidato de FIFAST2.csi — 2026-10-08

O comando `fifast2-csi-audit` valida `FIFAST2.csi` como candidato estrutural
somente: `MOIR`, `100/100` registros válidos, `100` nomes únicos, tabela em
`0x5F4`, word reservado zero em `100/100`, `12` words opacos dentro do asset e
`98` pares adjacentes não-decrescentes. O cruzamento com os `17` nomes EVT teve
`0` matches exatos e `22` por substring.

Isso não prova seleção EVT, trigger, sample, ABKC ou playback; os campos opacos
continuam sem semântica (`direct_selection_binding=not-proven`,
`original_semantics=not-claimed`). Release `0/0`, self-test `83/83`, Python
`57/57`, matriz completa, runtime headless e replays determinísticos passaram.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.78 — layout de canal candidato único em streams AST — 2026-10-08

Headers SCHl observados com `0` canais agora passam por uma validação
candidate-only dos layouts `1` e `2`; somente um layout que valida todos os
blocos é aceito, sem reescrever o header. As rows correspondentes recebem
`decoded-payload-channel-layout-candidate`.

Os dois casos antes rejeitados (`GBREAK` sound `4` e `GBREAKLC` sound `6`)
aceitaram unicamente `1` canal. A auditoria bounded/all passou a `241/241`
candidatos de banco, com `17/17` eventos e `0` matches PCM exatos. Isso não
atribui EVT, sample, gameplay ou playback original;
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.

Validação: Release `0/0`, self-test `82/82`, smokes e controles de áudio
passaram; a matriz completa 0.78 terminou com
`matrix_status=completed-authored-and-candidate-opt-in`, incluindo runtime
headless e replay determinístico. ISO preservada com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.77 — enumeração ABKC por entradas não-dummy observadas — 2026-10-08

O catálogo conserva as entradas ABKC não-dummy observadas e os decoders de
batch/correlação enumeram apenas índices RAM com `SoundType=0`, mantendo um
fallback explícito para fixtures que não forneçam essa lista. Na ISO, o
controle bounded/all obteve `17/17` eventos, `16` bancos, `241` candidatos e
`239` decodificações; `259/259` rows comparadas ficaram idênticas e não houve
match PCM exato. `CONTROL.abk` continua sem candidato e os dois streams AST
com `0 channels` continuam rejeitados pelo decoder.

Isso é controle estrutural de enumeração/payload, não seleção EVT→amostra:
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.
Rendering continua `not-proven`, áudio `partially-integrated` e
animation/deformation `structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `57/57`, matriz 0.77,
smokes, runtime headless e replay determinístico. ISO preservada com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.76 — join original↔candidate dos grupos de geometria por stream — 2026-10-08

O audit candidate validou `24/24` objetos e `38/38` grupos com primitiva,
cinco streams completos, escolha candidate no slot `0` e geometria
finita/indexada. O join independente coincidiu com o slot `0` original em
nome `__VB`, stride, byte-length, element-count, vértices e triângulos em
`38/38`; `2326` primitivas originais ficaram fora da cobertura candidate.

Isso é metadata estrutural do caminho candidate, não binding de posição, UV,
material, normal, peso, skinning, rigging, animação, morph, draw ou GPU.
`original_semantics=not-claimed`; o subgate é
`rendering-original-candidate-geometry-stream-control=verified-structural-negative`
`38/38`. Os gates principais continuam `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `57/57`, matriz 0.76,
smokes, runtime headless/windowed e replays determinísticos; ISO preservada
com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a investigação independente de atributos/UV/material,
deformação original e EVT→amostra sem inventar semântica.

## Etapa 0.75 — join original↔candidate de registros `__VB` por slot — 2026-10-08

Nos `216` layouts candidate completos, o controle independente comparou
`1080/1080` slots materializados ao relatório original: nomes `__VB`, aux,
strides, byte-length e element-count coincidiram. Os dois layouts incompletos
continuam não materializados, sem inferência. Isso é identidade/metadata
estrutural, não binding de posição, UV, material, skinning, rigging, animação,
morph, comando GPU ou semântica original.

Validação: Release `0/0`, self-test `82/82`, Python `56/56`, matriz 0.75,
smokes, runtime headless/windowed e replays determinísticos; ISO preservada
com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Os gates principais continuam `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Próxima etapa: continuar a investigação independente de atributos/UV/material,
deformação original e EVT→amostra sem inventar semântica.

## Etapa 0.74 — join original↔candidate de comando/fetch por primitiva — 2026-10-08

O controle independente por `(model_archive, model_object, layer, primitive)`
reconciliou `218/218` primitivas candidate com o render-chain original em
contagem/completude de comandos e fetch, sequência de estado, tipo e layout.
`216/218` tiveram IDs/records candidate materializados; os outros dois são os
layouts incompletos conhecidos e permanecem explicitamente não materializados.
Isso é evidência estrutural/negativa, não binding de posição, UV, material,
skinning, rigging, animação, morph, comando GPU ou semântica original.

Validação: Release `0/0`, self-test `82/82`, Python `55/55`, matriz 0.74,
smokes, runtime headless/windowed e replays determinísticos; ISO preservada
com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Os gates principais continuam `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Próxima etapa: continuar a busca independente de binding primitiva→vértice/
atributo/UV/material, deformação original e EVT→amostra sem inventar semântica.

## Etapa 0.73 — sequência de índices SHAPENAME — 2026-10-08

O controle candidate de shape-state reparseou `(LayerIndex, PrimitiveIndex)`
por objeto e o comparou independentemente ao estado original: `24/24`
sequências, conjuntos e contagens coincidiram, com `24/24` streams completos.
É evidência estrutural/negativa apenas; não cria bindings de vértices, UV,
material, skinning, rigging, animação, morph, comando GPU ou semântica original.

Validação: Release `0/0`, self-test `82/82`, Python `54/54`, matriz 0.73,
smokes, runtime headless/windowed e replays determinísticos; ISO preservada
com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Os gates principais continuam `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

## Etapa 0.72 — controle de população shape-state — 2026-10-08

O controle independente comparou a população de primitivas de estado original
com os shape-state streams candidatos: `24/24` contagens iguais, `24/24`
contagens de `SHAPENAME` iguais e `24/24` streams completos. O mesmo controle
preservou `24/24` same-trunk, cadeia de origem válida, entrada SHPX exata e
hash de pixels coincidente.

Isso é apenas controle estrutural/negativo; não é binding primitiva→vértice,
UV, material, skinning, rigging, animação, morph, comando GPU ou semântica
original. `rendering=not-proven`, `audio=partially-integrated` e
`animation-deformation=structural-only` continuam.

Validação: Release `0/0`, self-test `82/82`, Python `53/53`, matriz 0.72,
smokes, runtime headless/windowed e replays determinísticos; ISO preservada
com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a decodificação independente da associação de
primitiva, atributos/UV/material, deformação e EVT→amostra.

## Etapa 0.71 — controle SHAPENAME↔asset candidato — 2026-10-08

O controle independente juntou estado/geometria original e proveniência de
asset candidato: `24/24` objetos, `24/24` matches lexicais de `SHAPENAME`,
`24/24` same-trunk, `24/24` cadeias de origem válidas, `24/24` entradas SHPX
exatas e `24/24` hashes de pixels decodificados coincidentes.

Isso valida identidade estrutural de asset somente. Não é binding de UV,
material, vértices, skinning, rigging, animação, morph, comando GPU ou
semântica original. O gate principal de rendering continua `not-proven`; os
gates de assets, áudio e animação permanecem respectivamente
`candidate-integrated`, `partially-integrated` e `structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `52/52`, matriz 0.71,
smokes, runtime headless/windowed, replays determinísticos e ISO preservada
com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a investigação independente de estado de
renderização/UV/material, deformação de animação e seleção EVT→amostra.

## Etapa 0.70 — seleção de áudio original ainda não decodificada — 2026-10-08

O controle independente sintetizou cinco fontes negativas: `17` words EVT sem
hits em ABK/PCPAHDR/PCPADAT; zero hits alinhados/não alinhados no XBE; payload
com `17/17` eventos decodificados, `16` bancos, `368` candidatos, `239`
decodificados e `0` pares PCM exatos; join de faixas `17/17`; e `15/15` rotas
authored clean-room. O readiness registra
`audio-event-selection-negative-synthesis=verified-structural-negative` `5/5`.

Isso é evidência negativa/estrutural, não um binding EVT→ABKC/sample: mantidos
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.
`audio=partially-integrated`, rendering original `not-proven` e
animação/deformação `structural-only` continuam deliberadamente separados.

Validação: Release `0/0`, self-test `82/82`, Python `51/51`, matriz 0.70,
smokes, runtime headless/windowed e replays determinísticos; ISO preservada
com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar as associações originais de estado/geometry/UV/material,
deformação e EVT→amostra sem inventar bindings.

## Etapa 0.69 — rotas authored de áudio verificadas independentemente — 2026-10-08

O controle reparseou os traces authored e juntou as rotas ação→candidato aos
payloads EVT nomeados: `10/10` cenários, `54/54` eventos tocados, `15/15` rotas,
`8` candidatos únicos e `15/15` rotas preparadas com header WAV válido. Isso
verifica somente a integração clean-room; não recupera seleção original de
amostra, gameplay ou playback. `original_semantics=not-claimed`,
`audio-authored-candidate-route-control=verified-authored` 15/15 e
`audio=partially-integrated` permanecem.

Validação: Release `0/0`, self-test `82/82`, Python `50/50`, matriz 0.69,
smokes, runtime headless/windowed e replays; ISO preservada com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a seleção EVT→amostra e os decoders/associações de
renderização e animação, mantendo authored, candidato e original separados.

## Etapa 0.68 — controle estado original ↔ atributo/influência candidata — 2026-10-08

O controle independente juntou `24/24` objetos candidatos aos estados originais
por chave nominal e validou `218/218` registros sem tokens literais de
textura/material/UV/atributo/deformação. Os perfis candidatos `slot1` foram
completos/finito/near-unit em `24/24`; geometria finita/indexada em `24/24`,
influência raster por textura em `20/24` e `mesh_shape_state_exact=0/24`.

Isso não cria binding de UV/material nem estado→geometria; permanece
`original_semantics=not-claimed`, com
`rendering-original-state-candidate-attribute-control=verified-structural-negative`
24/24 e `rendering=not-proven`. Validação: Release `0/0`, self-test `82/82`,
Python `49/49`, matriz, smokes, runtime headless/windowed e replays; ISO
preservada com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar seleção EVT→amostra e os decoders de renderização/
animação, mantendo authored, candidato e original separados.

## Etapa 0.67 — join EVT paralelo com payload runtime — 2026-10-08

O controle independente preservou `17/17` eventos nomeados entre as faixas
PCPAHDR/PCPADAT e o payload gerenciado: header row, offset e comprimento
coincidiram, e metadados, preparação e headers WAV passaram em `17/17`.
Isso confirma apenas o caminho candidato; não cria seleção de ABKC/sample,
gameplay ou playback original. `original_semantics=not-claimed` e o gate de
áudio permanece `partially-integrated`.

Validação: Release 0/0, self-test `82/82`, Python `48/48`, matriz, smokes,
runtime headless/windowed e replays passaram. ISO preservada, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.66 — join estrutural com fetch slots opacos — 2026-10-08

O controle independente juntou `11820/11820` streams aos registros de
topologia, com `2364/2364` grupos completos. O primeiro raw corresponde ao
fetch slot `0` em todos os grupos, tem match de bounds em `2363/2364` e nenhum
registro posterior tem match. Isso não identifica posição, geometria, UV,
material, skinning, rigging, animação, morph ou GPU;
`semantic_binding=not-proven` e `original_semantics=not-claimed`.

Validação: Release 0/0, self-test `82/82`, Python `47/47`, matriz, smokes,
runtime headless/windowed e replays passaram. ISO preservada, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
O gate de renderização original permanece `not-proven`.

## Etapa 0.65 — correspondência estrutural de bounds — 2026-10-08

O relatório por stream agrupou `11820` streams em `2364` grupos completos de
cinco registros. O primeiro registro raw teve stride 12 em todos os grupos,
match de bounds em `2363/2364` e nenhum match nos registros posteriores. Isso
é evidência candidata/estrutural, não binding de posição, UV ou material;
`semantic_binding=not-proven` e `original_semantics=not-claimed`.

Validação: Release 0/0, self-test `82/82`, Python `46/46`, matriz, smokes,
runtime headless/windowed e replays aprovados. ISO preservada, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.64 — controle de ambiguidade numérica — 2026-10-08

O controle independente dos payloads `__VB` confirmou `883/883` objetos
válidos: `494` com cardinalidade de bounds `1`, `388` com cardinalidade maior
que `1` e `1` sem match. É apenas medição estrutural no nível de objeto;
`semantic_binding=not-proven`, `original_semantics=not-claimed` e o gate de
renderização original permanece `not-proven`.

Validação: Release 0/0, self-test `82/82`, Python `45/45`, matriz, smokes,
runtime headless/windowed e replays passaram. A ISO permanece sem escrita, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL Release `CF863DA6CE14326060C7E4A4CBA8FE9E5FC4347C504C683A5422B980E30BF986`;
relatório `C637C4D1EF8DE77D2544140CB7229410DE83EA40D8604F70A8FD59148419CC82`;
summary `6F137E8CC1681F8B5F3AE1E0EA4E31BA62DEF4F87A640095183D6CDB16A075ED`;
readiness `FF5AA4D98EA0CFE74FF516B151598842BE2E09F64DEFDEC417468A6CB149E7AD`.

## Etapa 0.63 — controle do caminho textual EVT — 2026-10-08

`audio_event_asset_path_index_control.py` cruza os literais `EVENTSPA.EVT` do
relatório XBE com o índice EVT: 4 linhas, 9 ocorrências, 4/4 no único caminho
`data/audio/eventspa.evt`, 17 nomes e 1.061 pares candidatos; xrefs executáveis
diretos: 0. É controle textual/negativo, não seleção de amostra ou playback.
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`;
`audio=partially-integrated` permanece.

Validação: Release 0/0, self-test 82/82, teste Python aprovado, matriz completa
exit_code=0 com smokes, runtime headless/windowed e replays determinísticos.
ISO sem escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.62 — controle independente das políticas de payload de áudio — 2026-10-08

`audio_event_payload_policy_control.py` comparou os relatórios gerados com
limite de 512 e 100.000 candidatos por bank. Coincidiram 386/386 linhas — 17
eventos e 369 candidatos de bank — em identidade, status, decode, bytes, hashes
PCM e contadores; houve 0 pares de hash PCM exato.

É apenas cobertura/reprodutibilidade: não seleciona amostra nem cria ligação
EVT→ABKC/AST/BNK, gameplay ou playback; `original_semantics=not-claimed`.
O readiness registra `audio-event-payload-policy-control=verified-structural-negative`
386/386 e mantém `audio=partially-integrated`.

Validação: Release 0/0, self-test 82/82, matriz completa exit_code=0, smokes,
runtime headless/windowed e replays determinísticos. ISO sem escrita:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `599EB85BD0AC52B328B6831A0F7B75464C0E673E9512395B7165647C64F22C26`;
relatório `F3D87BDCCD0F27B584C3B48BCB7B98AFDCB941AE57647DC37893621AEE1B1D94`;
summary `C0CEB3E9B359ACE60D9D126BCCE56427BFA034E5822FC3FDD7EE45903D8F9297`.

Próxima etapa: buscar evidência independente de seleção EVT→amostra e
avançar os decoders de renderização/animação, sem inventar bindings.

## Etapa 0.61 — join independente estado/geometria ↔ payload original — 2026-10-07

`eagl_original_state_geometry_payload_join_control.py` juntou 24/24 objetos
`tops*.big` ao relatório original independente de render/payload. Todos os
joins de identidade, contadores de primitivas, comandos completos e fetches
passaram; 24/24 objetos têm cinco streams de payload por primitiva, ranges
distintos e 0 sobreposições.

Isso é apenas consistência de chaves e cardinalidade. Não atribui payload a
posição, UV, material, atributo, skinning, rigging, animação, morph, comando
ou GPU; `original_semantics=not-claimed`. O readiness registra
`rendering-original-state-geometry-payload-join-control=verified-structural-negative`
24/24 e mantém `rendering=not-proven`.

Validação: Release 0/0, self-test 82/82, matriz completa exit_code=0, smokes,
runtime headless/windowed e replays determinísticos. ISO sem escrita:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `76F273539B1E1D14494B556D2D021F539D1F7EF7F6DB9F182F8237C317DD4FDE`;
relatório `7D0236122EB806FB288DFCE6F1C1923ADEF7E9CF81EE0A7DD0C0FF27D6E33F93`;
summary `48C79333F7C74C36632F7E5C660C677D5265950C4CFCA9D0AE9917E58413EF26`.

Próxima etapa: continuar a investigação estrutural de renderização e áudio,
sem inventar bindings.

## Etapa 0.60 — partição independente de estados e geometria — 2026-10-07

`eagl_original_state_geometry_partition_control.py` reprocessa os relatórios
de adjacência, perfil opaco e ordem da renderização original. Reconciliou
24/24 objetos, 269/269 perfis, 62/62 intervalos, 38 pares geométricos,
180 estados e 180/180 índices cobertos. O status é
`verified-independent-original-state-geometry-partition-control`.

O controle é estrutural: `object_state_exact=True` em 24/24,
`geometry_group_state_exact=False` em 24/24, streams completos em 269/269 e
zero formas de comando exatas. Não cria binding de materiais, UVs, skinning,
rigging, animação, morphs, comandos ou GPU; `original_semantics=not-claimed`.
O readiness registra `rendering-original-state-geometry-partition-control`
como `verified-structural-negative` 24/24, sem alterar
`rendering=not-proven`.

Validação: Release 0/0, self-test 82/82, smokes de adjacência/perfil/ordem,
matriz completa exit_code=0, runtime headless/windowed e replays
determinísticos. ISO sem escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `2BA8CC8D4DD40AE174787E76B66C02DF18A449FF33AD2DE3FA59E4B673AAFA60`;
relatório `4F788E12FDB3CC447C302DA8E195E3E14B4FAAFEEDDC258CFB7A75A291AC4D0D`;
summary `9C20A83D49482D34CEB1F12E3A249C616F1D541E5CA9C5AE59DEA04CAA46AE4A`.

Próxima etapa: continuar a associação original de renderização e a seleção
original de áudio, sem inventar bindings.

## Etapa 0.59 — controle negativo de words EVT no XBE — 2026-10-07

AudioEventXbeHashSearchAudit procurou os 17 words do índice textual de
eventspa.evt dentro dos 3.293.184 bytes do default.xbe, em offsets alinhados e
em todos os offsets de byte. Foram encontradas 0 ocorrências alinhadas e 0
não alinhadas, com 17 nomes e 17 words únicos.

É um controle negativo limitado: não executa o XBE nem prova ausência de uma
rota indireta. Nenhum word foi promovido a função, evento, amostra ABKC ou
semântica de gameplay. O readiness registra
audio-event-xbe-hash-search=verified-structural, com
direct_selection_binding=not-proven e original_semantics=not-claimed; o gate
principal de áudio permanece partially-integrated=false.

Validação: Release 0/0, self-test 82/82, matriz completa com exit_code=0,
smokes relevantes, runtime headless/windowed e replays determinísticos. ISO
sem escrita: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL: CFF31EF95E08040AB38A3581FF55BECD2DC340B56DF27D4E1ED6580DADC0FC3F.

Próxima etapa: investigar associação estrutural original de renderização além
de intervalos, fetch e adjacência, sem inventar bindings.

## Etapa 0.58 — controle estrutural EVT ↔ ranges paralelos de áudio — 2026-10-07

AudioEventParallelRangeAudit revalida os 17 nomes EVT contra o início textual
exato em eventspa.evt, a mesma linha nos companions pcpahdr.off/.big e
pcpadat.off/.big, os limites dos ranges e o prefixo SCHl. Foram confirmados
17/17 âncoras, ranges de header, ranges de dados, índices paralelos, prefixos
SCHl e nomes/offsets/linhas únicos; as duas tabelas têm 283/283 linhas.

É evidência estrutural apenas: não há seleção de amostra ABKC, ligação com
gameplay ou semântica original. O readiness registra
audio-event-parallel-range=verified-structural, com
direct_selection_binding=not-proven e original_semantics=not-claimed; o gate
principal de áudio permanece partially-integrated=false.

Validação: Release 0/0, self-test 81/81, matriz completa com exit_code=0,
smokes relevantes, runtime headless/windowed e replays determinísticos. ISO
sem escrita: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL: 6D6CC0AF1ECE8AF58ECE20E9D6DE76F02686ABB053D5C08417EB0DAD5648FB2C.

Próxima etapa: continuar a busca independente por seleção original de áudio e
associação original de renderização, sem inventar bindings.

## Etapa 0.57 — gates agregados da integração clean-room — 2026-10-07

O readiness agora separa os agregados authored: `assets-clean-room` 10/10,
`rendering-clean-room` 140/140 frames, `audio-clean-room` 54/54 eventos e
`animation-clean-room` 1554/1554 samples; `gameplay` permanece
`clean-room-integrated=true`.

Esses gates verificam manifests authored, framebuffer gerenciado, rotas de
áudio authored e traces/replays. Não promovem candidate mesh, UV, material,
skinning, rigging, morph, timing ou GPU a semântica original. Os gates originais
continuam `assets=candidate-integrated=false`, `rendering=not-proven`,
`audio=partially-integrated=false` e `animation-deformation=structural-only=false`.
Release 0/0, self-test 80/80, Python 43/43 e matriz completa com exit_code=0
passaram. ISO sem escrita: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL: 4BB5F349628F3BFA8C6A9A425B33BCB41DBFC1135C4FEFA65F351AF5A44774B8.

Próxima etapa: investigar seleção original de áudio e associação original de
renderização sem reutilizar rotas authored nem inventar bindings.

## Etapa 0.56 — gate principal de gameplay clean-room — 2026-10-07

O gate principal `gameplay` agora é `clean-room-integrated=true`, porque o
runtime authored, a cobertura de ações, as invariantes e a cobertura integrada
passam conjuntamente: 10/10 cenários, 54 eventos, 777 frames e replays
determinísticos. Isso não decodifica nem reivindica as regras originais;
`original_semantics=not-claimed` permanece explícito.

Assets ainda são `candidate-integrated=false`, renderização original é
`not-proven`, áudio original é `partially-integrated=false` e
animation-deformation é `structural-only=false`. Release 0/0, self-test 80/80,
Python 43/43 e matriz completa com exit_code=0 passaram. ISO sem escrita:
2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL: 2CB105E28AAE2C9BE49C61A845D97B6AF243933E658E5783E210343682F938CD.

Próxima etapa: continuar a integração clean-room de assets, renderização e
áudio, mantendo a evidência original separada.

## Etapa 0.55 — controle independente de regiões comando/payload — 2026-10-07

tools/analysis/eagl_original_command_payload_region_control.py reparseia
render-chain e topologia original e cruza a chave
model_archive/model_object/layer/primitive. Em 2364/2364 primitivas, as cinco
regiões de payload são válidas e não sobrepostas, a seção coincide, o intervalo
de comando é válido/disjunto e fica depois do payload. A lacuna bruta é `0x10`
em 2364/2364; os dois casos sem slot 2 permanecem explícitos.

Isso comprova apenas relações de bytes, intervalos e offsets:
original_semantics=not-claimed. Não há binding para posição, geometria, UV,
material, skinning, rigging, animação, morph, comando ou GPU. O subgate
rendering-original-command-payload-region-control é verified-structural e o
readiness geral continua NOT_READY.

Release 0/0, self-test 80/80, Python 43/43, matriz completa, smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. ISO sem escrita:
2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL: C90B6FD374D53B9CA29A60872C5A1B4E15977E59A99605BCF91BCCA9DC1C97BA.

Próxima etapa: buscar associação original independente além da coerência de
offsets e intervalos, sem inventar bindings.

## Etapa 0.54 — controle independente fetch/topologia por primitiva — 2026-10-07

tools/analysis/eagl_original_primitive_fetch_topology_control.py cruza
independentemente render-chain e topologia pela chave primitiva. Foram
confirmados 2364/2364 joins, cinco linhas de topologia por primitiva, IDs de
fetch coincidentes em 2364/2364 e concordância da completude em 2364/2364.
Também foram preservados 11820/11820 ordinais e ranges válidos, 11818 slots
efetivos, 11810 structural_match e 2362 grupos completos.

As duas exceções são primitivas sem slot 2 e permanecem incompletas; nenhum
valor foi inventado. O controle é estrutural, original_semantics=not-claimed,
e não liga slots/streams a posição, geometria, UV, material, skinning, rigging,
animação, morph, comando ou GPU. O subgate
rendering-original-primitive-fetch-topology-control é verified-structural;
renderização principal e readiness geral continuam NOT_READY.

Release 0/0, self-test 80/80, Python 42/42, matriz completa, smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. ISO sem escrita:
2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: buscar associação original independente além da coerência
estrutural de slots, sem promover candidatos.

## Etapa 0.53 — controle independente estado/fetch ↔ adjacência — 2026-10-07

tools/analysis/eagl_original_state_fetch_adjacency_control.py reparseia os
relatórios estruturais e faz join por chave primitiva. Foram confirmados
2364/2364 joins; state relocation 2364, SHAPENAME 752, SetPrimitiveType 1612,
fetch completo de cinco slots 2362, overlap state/fetch 750/750 e overlap
state/primitive-type 0/0. A adjacência exata ocorreu em 2364/2364, com
categorias 14:2010 e 15:354.

Esse controle é estrutural/coocorrente e não inventa bindings: geometria, UV,
material, skinning, rigging, animação, morph e GPU continuam não decodificados.
original_semantics=not-claimed. O subgate
rendering-original-state-fetch-adjacency-control é verified-structural, mas
renderização principal e readiness geral continuam NOT_READY.

Release 0/0, self-test 80/80, Python 41/41, matriz completa, smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. ISO sem escrita:
2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: buscar evidência independente de semântica de campo/atributo ou
associação estado→geometria/UV/material, sem promover candidatos.

## Etapa 0.52 — perfil bruto da adjacência comando/descritor __VB — 2026-10-07

O audit .NET e o controle Python independente registram agora categoria e
stride do stream exato na relação estrutural command_stream_offset =
descriptor_offset + 0x0C: 2364/2364 primitivas, categoria 14 em 2010,
categoria 15 em 354, stride 2 em 2364/2364. Isso não atribui semântica a
campo, atributo, geometria, UV, material, skinning, rigging, animação, morph,
comando ou GPU; original_semantics é not-claimed.

O subgate é verified-structural, mas renderização principal e readiness geral
continuam not-proven/NOT_READY. Audit/summary:
46E952F58C8A40507E3A1A2D9C741CD91DBA89611F09D5B860D7CF290AFD8C56 /
E5B5B6D8751D7174E5E609684A77A24503CC7CFE054E538A30F77A4E7E86CF37.
Controle/summary:
41BC4DCD41AFD0496AE3B265C5AD5410C1EA85A990084C9D7D9FBB05D82D02DC /
E5134201C80EF61C1B76690DB0B6450A375E2D6772E7DF3AEB93686723AE92DF.
Release 0/0, self-test 80/80, Python 40/40, matriz completa, smokes, runtime
headless/windowed e replays determinísticos passaram. DLL:
7C6750FE5FB3B9820367E6E2A654F8779C6CB2BBF99210BDE65571BD03FE3E1A.
Readiness: 533D08F678639A36808D01E2D2D08CFBAF42B74B486AF5643FBAD5963FAADC23.
ISO sem escrita: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: buscar semântica independente de campo/atributo ou associação
estado→geometria/UV/material, sem inventar bindings.

## Etapa 0.51 — adjacência estrutural comando/descritor `__VB` — 2026-10-07

O audit `eagl-original-primitive-stream-command-descriptor-adjacency-audit` e
seu controle Python independente verificaram a relação bruta, na mesma seção,
`command_stream_offset = descriptor_offset + 0x0C` em `2364/2364` primitivas.
Foram observados `10240` candidatos, `2364` identidades, `2364` identidades
atuais únicas, `2364` pares na mesma seção e `0` identidades deslocadas. Isso
não atribui significado a campo, atributo, geometria, UV, material, skinning,
rigging, animação, morph, comando ou GPU; `original_semantics=not-claimed`.

O readiness registra `verified-structural` apenas para esse subgate; a
renderização principal continua `not-proven` e o readiness geral `NOT_READY`.
Audit/summary: `4DC8C24FFF144C4FE83659B1F8F8B5D5C83756637EBF5F15230DF04E3481D717` /
`00ABD1EF407D5FD733CB55BEB220608413AB54177584B5F1E855C57F0A56B79E`.
Controle/summary: `DEFA5803260CD6CE977D0411ED325793166DA4FD7DABD02281FD56368C3D3975` /
`081BD81C79F80E97D33192F315E8CDAC958F8D478F38703F3997C340E13E0B14`.
Release `0/0`, self-test `80/80`, Python `40/40`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. DLL:
`CCE12ACE31724C7142EF9E338BD1E547AA362D4C957C6FBDC4CE67D044FA1877`.
Readiness: `DB51CD27DD5F9D0BCFE185B219D4987E3DFAFD066E525750820201E85C191E30`.
ISO sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar semântica independente de campo/atributo ou associação
estado→geometria/UV/material, sem inventar bindings.

## Gate separado de áudio candidate-only — 2026-10-06

`audio-candidate-runtime` agora é `verified-candidate`: o full-stack opt-in
preparou/reproduziu `8/8` eventos, com `0` não mapeados e replay coincidente.
Isso valida apenas áudio candidate-only authored no runtime; a seleção original
EVT/ABKC/amostra continua não provada.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`A392BB170AD47F640D354A06F9360A9CDD97071F2194785385F90ABC028C09A7`.
Readiness:
`A694DD3E15444DEDDAB0ADA3A9EA67026497A06690CE11A9629349208C018EEE`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — cadeia estrutural bruta de renderização EAGL — 2026-10-07

`eagl-original-render-chain-audit` verifica a cadeia bruta
`__Model -> descriptor -> render method -> global parameters -> command stream`
sem usar mesh candidate ou textura SHPX. Foram validados `388/388` pacotes,
`883/883` objetos e `2364/2364` primitivas, todas com streams completos;
`SHAPENAME` aparece em `752` linhas e `SetPrimitiveType` em `1612` linhas como
texto opaco.

É evidência estrutural, não decodificação de comandos, funções, GPU,
geometria, UV ou material. `original_semantics=not-claimed`; o subgate
`rendering-original-render-chain-structure` é `verified-structural`, enquanto
assets continuam candidate-only, rendering principal continua `not-proven` e
readiness continua `NOT_READY`.

Relatórios: `eagl-original-render-chain.tsv` (SHA-256
`22BE05FEA7A6EC6ACCC9792C3ADED3BE1DB639E9BDD34F35E0BCFD258B9207BB`) e
`eagl-original-render-chain-summary.tsv` (SHA-256
`66E8F22E71851056985FB1FF2AFECDAAEF7B87B39C66961A1483E75EA38E1E7F`). Release
`0/0`, self-test `74/74`, Python `32/32`, matriz completa, smokes, runtime
headless, captura windowed e replays passaram. A ISO segue somente leitura com
`2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: associação independente estado→geometria/UV/material ou
controle negativo adicional, preservando `216` layouts candidate completos e
`2` assets parcialmente completos.

## Gate separado de renderização candidate-only — 2026-10-06

`rendering-candidate-runtime` agora é `verified-candidate`: o full-stack
candidate-only validou `140/140` frames, `24` assets, `280` samples authored de
animação, `8/8` eventos de áudio e replay/framebuffer válidos. O gate original
`rendering` continua `not-proven`; nenhuma semântica original de estado, UV,
material ou GPU foi promovida.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`C2E72072AC40D16F7B3376B5DFD91F3EE85E7C50425A264EFE6AD66B5F99F800`.
Readiness:
`0A317FA850F5D5F16C23898E8291FF1D1DCABD7D11ABBB406A50E71631068736`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Call-sites bounded do destino aninhado — 2026-10-06

`xbe_render_indirect_indexed_cfg_nested_call_site_audit.py` encontrou `2`
call-sites para `0x0026E278`, `1` destino distinto. Ambos exibem localmente
`fld`, `fmul`, `call` e `ret 8`; nenhum tem `push edx`, `lea [ecx+...]` ou
`ret 4`. A observação é estrutural bounded, sem função/objeto/semântica
recuperada; `original_semantics=not-claimed` e readiness `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`AA55B7737D13A532ACA1B1CDC5553B93A9A6B9DC5A439A15F6C901A74B868567`.
Readiness:
`1442E380FE5C11D1D0D702A448E3B950733A6D8D8BF534191A0B49829A65248E`.
Nested call-site audit/summary:
`4F760A2CB3F8F47BC4BD2A0AB2A03D263A481AE6C50853BE3DAFB9DD084A8E56` /
`870483285414A5E646CDC7C51DC1F23EAFC60387AD9E91005B7AB4DDC88AF15D`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## CFG bounded do destino aninhado — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_nested_call_target_cfg_audit.py`
inspecionou `0x0026E278`, o único destino direto encontrado dentro do slice de
`0x0016BF90`: `89` instruções lineares, `37` alcançáveis, `7` blocos, `3`
branches condicionais, `2` incondicionais, `4` alvos internos, `1` retorno e
nenhuma chamada direta adicional. É evidência local bounded, sem função
recuperada ou semântica promovida; `original_semantics=not-claimed` e
readiness `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `30/30`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`395813624F6C4AD23B4FA1D11BF44DF7C6697C2189282E32319200BCAD737982`.
Readiness:
`0F5A8B50AB5D8D04A300AB0BA883782D4D6017546400753C727EB86D03FE196A`.
Nested audit/summary:
`5E73FAF3C58277F98B08DC4F9030673E381000B6188AB5018BB7AB9B564336E3` /
`2C18CBB0DC2B53C4E4795945F0786B5011E747C12DC3CCFFB7A1FDAEF4C17D33`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Join bounded entre tabela e call-sites indexados — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_call_site_join_audit.py` cruzou os
alvos da tabela indexada com os call-sites alcançáveis sem executar o XBE:
`81` linhas de alvo, `1` base, `24` call-sites, `24/24` joined, `1` alvo
distinto, `27` índices distintos, `27` ocorrências e `3` duplicatas. Os
`24/24` call-sites mantiveram forma local completa. Isso permanece evidência
estrutural bounded; `original_semantics=not-claimed` e readiness `NOT_READY`.
Nenhum binding de material, UV, skinning, rigging, animação, morph ou GPU foi
atribuído.

Validação independente: Release `0/0`, self-test `72/72`, Python `29/29`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`CF1E344282E9237943573EF27A7F24A1F12B662476175A819337DCF0B50A9959`.
Readiness:
`EC3F4027BE15A0829D65EF6E76E6B65E0BB3BDD68A4FC2A6B3E732D1AC7F9BBD`.
Join audit/summary:
`6742EB92434168D7A04C695BB77DCF8632AC1534ACB702E144401AA557FD81AE` /
`71BA5028FCDFBDF276848DC9FD50619EA615D08948CFB806C58D164B832FFBC9`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## CFG bounded do destino direto indexado — 2026-10-06

O destino distinto `0x0016BF90` recebeu um CFG bounded de `256` bytes, sem
seguir chamadas. Foram observadas `76` instruções lineares, `23` alcançáveis,
`7` blocos básicos, `2` branches condicionais, `2` chamadas diretas e `0`
transferências indiretas; `1/1` `ret` foi alcançado. Isso continua fluxo
local candidato, não função recuperada ou semântica.

Status `verified-bounded-static-render-indirect-indexed-cfg-call-target-cfg-slices`;
`original_semantics=not-claimed`; nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído. Readiness: `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `28/28`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`06483C77B64112CCFE79F635DCE1B4252F7C1D2FD92A7BB6E6744B71B11F1FC8`.
Readiness:
`975217DC8EFDDA58C4D35A9218EF93E1D467A82701B4A85233475181AF76EB60`.
Audit/summary:
`8CBBF5C159106F6D98CE05D65A88ACD76179E863A89763202649B431D450EE77` /
`6250D59561DE79E73027693C34E8853DC33EBE4A91941AAE6ABCDFFD0AA85ED2`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Contexto bounded dos 24 call-sites indexados — 2026-10-06

`xbe_render_indirect_indexed_cfg_call_site_audit.py` registrou o contexto local
das `24` chamadas alcançáveis nos CFGs indexados. Todas convergem para
`0x0016BF90`; em `24/24` aparece `lea` baseado em `ecx` com deslocamento
distinto, `push edx` antes da preparação e `ret 4` após a chamada. Isso é uma
regularidade estrutural de instruções bounded, não um wrapper ou função
recuperada, nem binding de objeto/campo/material/UV/skinning/rigging/animação/
morph/GPU. `original_semantics=not-claimed`; readiness: `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `27/27`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`FE1DEAB66AC78E1FDA154848394C6EAA3A06FB3A87E563EC5609CE0A313F1E37`.
Readiness:
`B18D55EC12C80D7888B4EC441A1315C17DF9A7BD7AA036E08EEC7C6FC794EB6C`.
Audit/summary:
`1BC39F705C0AAADFC34AAD030CFB0E29B2A87197E71A9E3E4363A34B16650474` /
`227EC2C511556952E038E9AB725B76B5E68459AA3F2BF8EF57B987BEF2219A92`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Destino direto bounded dos CFGs indexados — 2026-10-06

O auditor dos CFGs indexados preserva a diferença entre `24` ocorrências de
chamada (`24` alvos únicos somados por slice) e `1` endereço distinto global:
`0x0016BF90`. Esse endereço mapeia em `.text`, offset `0x0015BF90`, e sua
janela bounded de `256` bytes contém `76` instruções e `ret`; nenhum prólogo foi
reconhecido. O auditor não segue chamadas nem recupera uma função.

Status `verified-bounded-static-render-indirect-indexed-cfg-call-target-slices`;
`original_semantics=not-claimed`; nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído. Readiness: `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `26/26`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`0FF23943CA552774AC25810A2B4B6D0C066F13862EB36A3D70BA787C9E44274A`.
Readiness:
`73AB08762847C2F91C282C5A4C749863B692B8DB61E9B42A850D1655C7379328`.
Audit/summary:
`41E108CCEC548410358EF649AD9F3DBB24448175CF1E4E44D9C388F9FB1463A2` /
`E329BE8965964C3995347AFF7442B7BF403E947B7D0412BDD44D97512F86414C`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Distinção de chamadas diretas e controle negativo de CFG — 2026-10-06

O relatório CFG dos `81` candidatos da tabela indexada agora distingue
ocorrências de endereços distintos: foram registradas `24` chamadas diretas
(`24` alvos únicos somados por slice), mas apenas `1` endereço distinto global,
`0x0016BF90`. Isso é fluxo local bounded; não recupera função, ponteiro,
chamada seguida ou semântica.

O join compara esse endereço com `87` alvos de chamadas diretas de
renderização, `64` alvos de janela e `235` nós do grafo bounded. As três
interseções foram `0`, com status `verified-structural-negative`. Isso é um
controle negativo de endereços, não prova independência comportamental.
`original_semantics=not-claimed`; nenhum binding de material, UV, skinning,
rigging, animação, morph ou GPU foi atribuído. Readiness: `NOT_READY`.

Validação final: Release `0/0`, self-test `72/72`, Python `25/25`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`361A00A077A987AF3A826CC807AF6D141D228E0698C40B4F81A8948BF6AD5F9E`.
Readiness:
`C8A4D27666203A19FE35E8300C66F1CA4B914585FB80F9329127BB77F4CDEA3F`.
CFG audit/summary:
`B2D8030BF2EA4F940AD23943B80BDF52455E909B0EEB3359EEBAAF5C6C485500` /
`9164CA4C7E7E73B9DB4D6AE213335724128854CB1E748EE8CE69798117F17FE6`.
CFG join audit/summary:
`6AEBD1BB735025409B55D76760E18722AA45F46094BEE53EA2B3FE9B96FD79FF` /
`5FAA0A61F854CC448B6B9B27BB0217A6F3DCF5430E53EEA6EC9E35AE9C4246A2`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## CFG bounded dos candidatos da tabela — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_audit.py` aplicou alcançabilidade
bounded aos `81` alvos distintos da tabela: `81/81` CFGs alcançáveis e `81/81`
`ret` alcançáveis, `7.295` instruções lineares, `421` alcançáveis, `105` blocos
básicos, `24` chamadas diretas não seguidas e `0` transferências indiretas na
janela de `256` bytes.

Status `verified-bounded-static-render-indirect-indexed-cfg-slices`: evidência
de fluxo local, não recuperação de função, limites, ponteiros, chamadas ou
semântica original. `original_semantics=not-claimed`; nenhum binding de
material, UV, skinning, rigging, animação, morph ou GPU foi atribuído. Readiness
permanece `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `24/24`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`5A3E8814C7AE038932856353E65E5E2322CC326A514BBDCB44356D8E0813714C`.
Audit/summary:
`3DF517090ABE885BB013E6F012A23B6CF99EF4482DA12BBF56E014B3FC9E3756` /
`57E7E7F2138EC6C6FD707D1FA5956DF542DD77AFD89F04CE7E90A28D624D21D5`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Controle negativo de interseção dos alvos — 2026-10-06

O auditor `xbe_render_indirect_indexed_target_join_audit.py` compara apenas
conjuntos de endereços bounded: `81` alvos distintos da tabela indexada contra
`87` alvos de chamadas diretas e `64` alvos da janela de render. As interseções
foram `0` e `0`.

Status `verified-structural-negative`: isso é controle negativo de endereço,
não prova independência de comportamento e não recupera funções, chamadas,
ponteiros ou semântica original. `original_semantics=not-claimed`; nenhum
binding de material, UV, skinning, rigging, animação, morph ou GPU foi atribuído.
Readiness permanece `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `23/23`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`D4289C65CD3E4FCD0953C3DD625D6EC1544E641F348821D4A96A693B74CD0ED3`.
Audit/summary:
`5A8BE06EAD3A8FD27920F2151D88C262E4245D1E7A50C5789BB2F33AD79A16C9` /
`6E38D14E644E9A7034ED4B5424428FD067AE7DB8E226ED82C9E3E11CD2F5DDB1`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Slices bounded dos alvos da tabela indexada — 2026-10-06

O auditor desassemblou janelas de até `256` bytes nos candidatos executáveis da
tabela, preservando a distinção entre ocorrência e alvo distinto: `87`
ocorrências, `81` alvos distintos, `81/81` mapeados/desassemblados, `81/81`
com `ret` na janela e `0` prólogos reconhecidos.

São slices estáticas candidatas, não funções recuperadas e não semântica
original. Status:
`verified-bounded-static-render-indirect-indexed-target-slices`;
`original_semantics=not-claimed`; readiness `NOT_READY`. Nenhum binding de
material, UV, skinning, rigging, animação, morph ou GPU foi atribuído.

Validação: Release `0/0`, self-test `72/72`, Python `22/22`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`0C23802D85483FA565E1BE497610922B7C160ACFD80137F963FFDE07D2039210`.
Audit/summary:
`FA7A2DB7409B43C8A42EB7E8A5FD5C5A988CD06758C75A2C71FD7A28E19C8399` /
`BD71ED8F44CB15754045E9DDF5E58F828A55D0BE47FEBEEA26B2C434EA5DBEC7`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Auditoria limitada da tabela de indireção de render — 2026-10-06

O auditor estático leu somente a base da única transferência indireta
indexada sem registrador-base observada. A leitura limitada terminou após duas
words não executáveis, sem atingir o teto de `256` entries: `89` words
legíveis, `87` executáveis, `2` não executáveis, `0` ilegíveis, `81`
executáveis distintas e prefixo contíguo máximo de `87`.

Isso é evidência de prefixo estrutural candidato, não tabela completa,
recuperação de função/ponteiro ou semântica original. Status:
`verified-bounded-static-render-indirect-indexed-table`; `original_semantics=not-claimed`.
Materiais, UVs, skinning, rigging, animação, morphs e GPU não foram ligados.
Readiness permanece `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `21/21`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`EF02D432D4C534F0AA1F572B7E3551E2B75C14B2AEB4AA7F03F278C0B72D3A7E`.
Audit/summary:
`0638A839ACF1191386669065B3E714AA8AEB4F2B0928D5983B48892587B1E895` /
`58271102E9D61725E85F0AB082AB4BF99556651A5141BAF3AE548461AE5C780A`.
ISO sem escrita, SHA-256:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Observabilidade do áudio candidato no runtime — 2026-10-06

O relatório de integração agora separa explicitamente os contadores de
payloads candidatos dos eventos authored. A matriz real preparou `17/17`
candidatos EVT nomeados (`2.914.952` bytes PCM, `0` rejeições) e observou `16`
bancos candidatos (`15` decodificados, `1` rejeitado). O fluxo authored manteve
`8/8` eventos preparados/tocados e `0` não mapeados.

Isso é evidência de preparação/decodificação no runtime gerenciado, não vínculo
original de evento, sample ou playback. `original_semantics=not-claimed` e
readiness `NOT_READY` permanecem.

Validação: Release `0/0`, self-test `72/72`, Python `20/20`, matriz completa
com smokes, runtime headless/windowed e replays determinísticos `matched=True`.
DLL Release:
`97C05064D768A18D73F74ECC5DBA6DA36DA03024148A071E9444D73EFFD2776E`.
A ISO segue sem escrita, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Controle de multiplicidade morph/animação — 2026-10-06

O auditor `animation-morph-relocation-profile-audit` agora mantém o conjunto e
o histograma de gaps consecutivos de cada layout morph. A execução sobre os
artefatos reais confirmou `594` linhas, `393/393` layouts válidos e `19` bancos:
`53` layouts e `159` pares têm contenção do conjunto; correspondência exata foi
`0` por conjunto e `0` layouts/`0` pares pelo histograma de multiplicidade.

Isso permanece evidência estrutural, sem binding ou semântica original de
frames, poses, timing, rigging, skinning, morph, materiais, UVs ou GPU.
`original_semantics=not-claimed`; o readiness geral permanece `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `19/19`, matriz completa
com smokes, runtime headless/windowed e replays determinísticos.
DLL: `9776FD7946552D26CABF479C2DF5EA145C5CD76317A119C2FAF4F94C86276B51`.
Audit: `D30560728E8BC701D1765558FF012D3CC36DF72E89421E255F6D6F1845379691`.
Summary: `FF42635A3E711AD2644BD9946A129541C7A99FC8E17DD781E53C18344FD8D9A2`.
A ISO segue sem escrita, com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Startup do host Win32 Release

O modo `run-windowed` foi iniciado com personagens clean-room e material
candidate explicitamente opt-in. O host confirmou a resolução da cadeia
candidate e permaneceu vivo durante o smoke; o resultado é
`verified-windowed-startup-only`. Isso não é QA visual independente nem
binding original de mesh, material, UV, skinning, rigging, animação, morph,
GPU ou áudio; `original_semantics=not-claimed` permanece explícito.

## Captura determinística do host Win32

`run-windowed` aceita `--run-seconds <segundos>` para um smoke com término
natural e `--capture-frame <saida.ppm>` para persistir o primeiro frame do
`SoftwareFrameBuffer`. A execução validada com dois segundos, personagens
clean-room e material candidate opt-in terminou com código `0` e produziu
`fifa_pc/windowed-runtime-capture.ppm` (P6 `960x540`, `1.555.215` bytes,
checksum `0F83CEFDD78C58BA`, SHA-256
`E1D1FE0294EA554F601F384C15D87B3FEA4C4649BE93EC2527BC10CA8269AD15`).

Isso é `verified-windowed-frame-capture` com proveniência
`managed-software-framebuffer`: confirma rasterização no loop Win32, mas não é
QA visual independente e não promove bindings originais.
`original_semantics=not-claimed`.

## Fechamento verificável da etapa

Build Release: `0/0`; self-test: `72/72`; testes Python: `11/11`; matriz
authored/candidate opt-in: concluída com smokes, runtime headless e replays
determinísticos `matched=True`.

DLL Release SHA-256:
`FF3514309EEB796CD009FED5B9D9B1A4A48E2A5C4B2B3A92C660085B85503917`.
Readiness: `NOT_READY`, SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
A ISO continua sem escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Trace authored de TACKLE

O readiness possui o subgate `gameplay-tackle-trace`, verificado com `5/5`
eventos e replay idêntico: `KICKOFF`, `STEAL`, `TACKLE`, `STEAL` e
`SHOT_AWAY`. SHA-256 do trace e replay:
`AC986AB9737AD4A498B9353C693B624C646EE8901208B0C5FCDC63C530552B53`.

Isso é cobertura authored clean-room, não binding ou semântica original;
`original_semantics=not-claimed`. A ISO segue somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Slices dos words candidatos de renderização

`xbe_render_indirect_reference_slice_audit.py` disassemblou boundedamente os
`3` words distintas observadas nas `8` referências de indireção. Os `3/3`
words caíram em seção executável, foram disassemblados em `256` bytes e têm
`ret` alcançável. `prologue_like_candidate_words=0`.

É observação estrutural candidate-only: não há execução, chamada, limite de
função ou binding inventado para materiais, UVs, skinning, rigging, animação,
morphs ou GPU. O subgate está `verified-structural`, enquanto o gate de
renderização continua `not-proven` e `original_semantics=not-claimed`.

Relatórios: `xbe-render-indirect-reference-slice-audit.tsv` e
`xbe-render-indirect-reference-slice-summary.tsv`. SHA-256:
`16FB2AA7CDC423A1C70D4588C6C5731FD5FACEEE1D59F7BDB0E3A3ACA253444C` e
`4175B05F115DCD37BC9048C37369D8A3E42F12B30F2128E7AB3464BD64C67AF5`.

Validação: Release `0/0`, self-test `72/72`, Python `19/19`, matriz authored/
candidate opt-in, smokes, runtime headless e replays determinísticos. A ISO
segue somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Readiness do aimed input trace

O leitor de readiness prefere os traces atuais `gameplay-authored-aimed-*` e
mantém fallback para o fixture legado `gameplay-clean-room-aimed-*`. Assim, o
subgate `gameplay-aimed-input-trace` é `verified-authored` com `80/80`
amostras e `frame=67` contendo `move_y=0.75` e `shoot=True`.

É uma verificação determinística do runtime clean-room, não um binding original
de controle; `original_semantics=not-claimed`. Release `0/0`, self-test `72/72`,
runtime aimed e replay de match/input/animação/áudio passaram, além dos testes
Python `18/18`. O input trace e o replay têm SHA-256
`0A1D6B3B30E96B3ECA8A66A0811119AC03A82130914315B547C9276505B010A4`.
O readiness geral segue `NOT_READY` e a ISO não foi alterada.

## Words referenciadas por indireções de renderização

`tools/analysis/xbe_render_indirect_reference_audit.py` faz leituras bounded de
quatro bytes nas referências sem base explícita encontradas nas `22`
transferências indiretas. O inventário atual contém `8` referências (`7`
absolutas e `1` base de tabela indexada); todas as `8` leituras são válidas e
estão em seções executáveis, com `3` endereços e `3` words brutas distintas.

Isso é apenas evidência estrutural de bytes referenciados. Não executa nem
segue os endereços e não atribui materiais, UVs, skinning, rigging, animação,
morphs ou comandos GPU. Os words permanecem candidatos, com
`original_semantics=not-claimed`; o subgate está `verified-structural` e o gate
principal de renderização permanece `not-proven`.

Relatórios: `xbe-render-indirect-reference-audit.tsv` e
`xbe-render-indirect-reference-summary.tsv`. SHA-256:
`9E32F0744ACB8BDA565993876F81F2EDE789C574F08FA56BAA198524E0B25836` e
`3FF5EBDE33A7ECB8A96BCE8AD8B4CB0609C95BE0FA682DBE56BD8ED7796BE38A`.

A validação passou Release `0/0`, self-test `72/72`, testes Python `18/18`,
matriz authored/candidate opt-in, smokes, runtime headless, replays
determinísticos e captura windowed. A ISO segue somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Inventário estático de transferências indiretas — 2026-10-06

O auditor `tools/analysis/xbe_render_indirect_transfer_audit.py` percorre os
CFGs bounded dos 64 destinos de renderização e registra `call`/saltos indiretos
alcançáveis sem desreferenciar os operandos. A execução encontrou `22`
transferências em `15` destinos: `21` chamadas, `1` salto, `21` operandos de
memória, `1` de registrador, `7` memórias absolutas e `11` formas distintas.

É evidência estrutural limitada. Os destinos indiretos continuam sem resolução;
não há recuperação completa de funções nem bindings de materiais, UVs,
skinning, rigging, animação, morphs ou GPU. O subgate é
`verified-structural`, o gate integrado de renderização segue `not-proven` e
`original_semantics=not-claimed`.

Relatórios: `xbe-render-indirect-transfer-audit.tsv` SHA-256
`265A0ACC8B3369F34D68EE5FD5563587978977CD83097C1EEBDF5ED743F7E926` e
`xbe-render-indirect-transfer-summary.tsv` SHA-256
`37D34311B33DD25DB1CB034DB2D0C3733D0AB843313D405D22647728087C7F6E`.
Validação: Release `0/0`, self-test `72/72`, Python `17/17`, matriz completa,
smokes, runtime headless, replay determinístico e captura windowed.

## Slices one-hop de chamadas do renderizador

`tools/analysis/xbe_render_call_target_audit.py` coleta os alvos de chamadas
diretas dos CFGs estáticos limitados de renderização. A etapa produziu `87`
alvos diretos únicos, `87/87` disassemblados, `75` terminados por `ret` e `28`
com prólogo semelhante a função. Chamadas não são seguidas recursivamente e
transferências indiretas continuam sem resolução.

É evidência estrutural do XBE retail, não recuperação completa de funções nem
execução. Não foram atribuídos bindings de materiais, UVs, skinning, rigging,
animação, morphs ou GPU; `original_semantics=not-claimed`. O subgate
`rendering-static-xbe-call-target-slices` está `verified-structural`, mas o
gate integrado de renderização continua `not-proven`.

Verificação: Release `0/0`, self-test `72/72`, testes Python `15/15`, matriz
authored/candidate opt-in, smokes, runtime headless e replays determinísticos.
A ISO permanece somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Regressão Win32 na matriz

`tools/run-clean-room-authored-matrix.ps1` também executa `run-windowed` por
dois segundos e valida o PPM `P6 960x540` de `1.555.215` bytes como não
uniforme. Isso é uma regressão authored/managed do loop Win32 e não promove
bindings ou semântica original.

## Readiness após o subgate Win32

O auditor registra `rendering-authored-windowed-runtime=verified-authored`
(`1/1`) para a captura PPM. O estado global segue `NOT_READY`: o gate
original `rendering` continua `not-proven`; assets, áudio e deformação ainda
mantêm, respectivamente, os estados candidate-only, parcial e
structural-only. A proveniência é `managed-software-framebuffer` e
`original_semantics=not-claimed`.

Hashes correntes: DLL Release
`9CCE2B05D2B76B64BEB685630D7DB30F94EC267317CBF1685C7231F8017546C1`,
readiness TSV
`ACF4212182C6BE8F44D07FFE424B76D3C82EA7E1A411D9512040F25DFC6E547F`,
captura PPM
`E1D1FE0294EA554F601F384C15D87B3FEA4C4649BE93EC2527BC10CA8269AD15` e ISO
preservada
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Contexto estático de renderização no XBE

O audit somente-leitura encontrou `494` referências, `88` literais distintos,
`3917` contextos com chamada executável e `854` destinos executáveis
relacionados a D3D/renderização/geometria. Isso aparece no readiness como
`rendering-static-xbe-context=verified-structural`, mas não promove o gate
original `rendering`: é coocorrência estática, sem execução ou binding.
`original_semantics=not-claimed`.

Validação: Release `0/0`, self-test `72/72`, testes Python `12/12` e matriz
completa authored/candidate aprovados; a ISO permaneceu inalterada.

Relatórios: `xbe-render-static-literal-audit.tsv`,
`xbe-render-static-call-clusters.tsv` e `xbe-render-static-summary.tsv`.
Hashes correntes: `8575E3823B9AE7A48BC81594832BF833B5532DE6F25A29B078924C774803CA06`,
`FDA08E3F34F11FB4C4F9B17DCF8AD6FA2477D52CD30DF8E84831F356DA01DE59` e
`DF4959A878D1B9A901C0EF70CBADE437F7116287BCA5368D0DAA4E11D4E820E3`.

## Cross-check textual do XBE

O auditor `tools/analysis/xbe_asset_literal_audit.py` encontrou `108`
literais de assets únicos (`702` ocorrências) no `default.xbe`, com `84`
correspondências textuais na árvore XDVDFS da própria ISO. Isso é somente
evidência textual de caminho (`verified-textual-xbe-path-crosscheck`);
`original_semantics=not-claimed` permanece e nenhum binding de material, UV,
áudio, animação, rigging, skinning, morph ou GPU é inferido.

## Cross-check textual XBE→entradas BIG aninhadas

O auditor `tools/analysis/xbe_asset_nested_literal_audit.py` indexou `4.796`
caminhos internos BIG únicos e encontrou `13/108` correspondências textuais
(`10` únicas, `3` ambíguas), incluindo `models/skel0000.ord`. A evidência é
somente de nome/índice; não cria binding de mesh, material, UV, áudio, evento,
animação, rigging, skinning, morph ou GPU. Status
`verified-textual-xbe-nested-big-entry-crosscheck`,
`original_semantics=not-claimed`.

Relatório: `fifa_pc/xbe-asset-nested-literal-audit.tsv`, SHA-256
`8570C2962A3BC5C7820111A8B56324D8E3DBD3784B4C81A6013EACD053A364CA`; resumo:
`fifa_pc/xbe-asset-nested-literal-summary.tsv`, SHA-256
`9B0E93677A6D0CB2B30DB57D2907ECFF5274B26F18B7DF7BA2F8769C9348551C`.

## Join estático XBE→BIG→xrefs

O auditor `tools/analysis/xbe_nested_literal_xref_audit.py` juntou `13`
correspondências BIG com xrefs estáticos: `8` literais com xrefs exatos, `10`
xrefs, `70` linhas de contexto local e `24` destinos executáveis distintos no
conjunto ambíguo `stadium.big`. É apenas coocorrência candidata de
texto/endereço; não cria binding de loader, mesh, material, UV, áudio, evento,
animação, rigging, skinning, morph ou GPU. Status
`verified-static-nested-entry-xref-join`, `original_semantics=not-claimed`.

Relatório: `fifa_pc/xbe-nested-literal-xref-audit.tsv`, SHA-256
`63418DEF893A32A5401D2993481CE11CB2A17215A7E19A860413181D11C4CEA2`; resumo:
`fifa_pc/xbe-nested-literal-xref-summary.tsv`, SHA-256
`6B84A9CBA014F2148D3E290443C15A3DB2384BA09DDA9D1DB28F057E0279A409`.

## Clusters estáticos de áudio no XBE

`tools/analysis/xbe_audio_static_cluster_audit.py` reuniu `58` xrefs de `23`
literais de áudio, `147` contextos locais, `108` destinos executáveis e `56`
destinos distintos. `14` destinos coocorrem com múltiplos literais. Apenas
`audio/tunavars.txt` teve correspondência BIG aninhada e `10` literais tiveram
caminho XDVDFS textual. Nenhum literal `.evt` apareceu nos xrefs selecionados;
isso é controle negativo textual, não prova sobre eventos binários.

Relatórios: `fifa_pc/xbe-audio-static-literal-audit.tsv`,
`fifa_pc/xbe-audio-static-call-clusters.tsv` e
`fifa_pc/xbe-audio-static-summary.tsv`. O status é
`verified-static-audio-literal-context`; não há seleção de amostra/ABKC,
binding ou semântica original. `original_semantics=not-claimed`.

## Inventário dos payloads BIG nomeados pelo XBE

`tools/analysis/xbe_nested_payload_inventory.py` registrou `41` caminhos
concretos, `32` hashes distintos e `15` payloads RefPack alcançados pelas
correspondências BIG. Foram observados `2` ELF, `2` sidecars `.orl`, `1` BILG,
`20` SHPX cru, `2` SHPX RefPack e `12` índices BIG RefPack. A saída é
estrutural — tamanho, cabeçalho e hash — e não cria bindings de mesh, material,
UV, rigging, skinning, animação ou GPU.

Relatório: `fifa_pc/xbe-nested-payload-inventory.tsv`, SHA-256
`232235B37AACC2A943E1EFFFCBE853F99E3A3F281C5E16A705E0241665709BC9`; resumo:
`fifa_pc/xbe-nested-payload-summary.tsv`, SHA-256
`668F8FCEEBDFA76CC2770B663A78DB7C9D413D3FAEE8570E57E28562401004C8`.
`original_semantics=not-claimed`.

## Readiness do payload EVT nomeado

O `project-readiness-audit` consome o resumo do audit runtime e registra
`audio-named-event-runtime=verified-candidate` com `17/17` candidatos, payloads
preparados e cabeçalhos WAVE válidos. Isso não promove semântica original: o
gate `audio` permanece `partially-integrated`/`NOT_READY`, com seleção direta
gameplay→evento→amostra/ABKC ainda não comprovada.

Depois da integração, Release `0/0`, self-test `72/72`, matriz, smokes, runtime
headless e replays determinísticos passaram. Readiness atual:
`fifa_pc/project-readiness.tsv`, SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
O relatório full-stack registra `opponent_ai_enabled=True`; esse campo é
exclusivamente authored e não reivindica regras originais.

## Payloads EVT nomeados preparados no runtime managed

O comando `audio-named-event-runtime-audit` foi integrado à matriz e validou
`17/17` candidatos EVT nomeados, preparou `17/17` payloads no runtime managed,
confirmou `17/17` cabeçalhos RIFF/WAVE e `2.914.952` bytes PCM:

```text
FifaStreetPc audio-named-event-runtime <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]
```

Relatório: `fifa_pc/audio-named-event-runtime.tsv`, SHA-256
`1F4D60065756162D29E665DCB794D9D31F34FACC88DBD5FC38E16D70E8503CDF`; resumo:
`fifa_pc/audio-named-event-runtime-summary.tsv`, SHA-256
`DFA25F7E59B247C836879445140D152DA65E7613534013CC52E3B7B7057211D5`.
Status `verified-runtime-named-payload`; proveniência
`exact-EVT-name+parallel-data-range; managed-runtime-named-payload-only`.
`original_semantics=not-claimed` permanece explícito: o audit não atribui
evento de gameplay, seleção ABKC/amostra ou binding original. O gate de áudio
continua `partially-integrated`/`NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, matriz, smokes, runtime headless e
replays determinísticos passaram. A ISO permanece somente leitura, com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Completude dos streams candidate

O audit de proveniência candidate confirmou `24/24` streams de comandos de
geometria completos e `24/24` streams de estado `SHAPENAME` completos. O audit
integrado exige ambos os marcadores. Isso reforça somente a representação
candidate; `original_semantics=not-claimed` permanece explícito e não há
binding original inferido para estado→geometria, material, UV ou GPU.

Validação da etapa: Release `0/0`, self-test `71/71`, smokes, runtime headless
e replays determinísticos passaram. Full-stack gerado/replay tem SHA-256
`588AD564F93E4B2BB048FB121ABF9D0C76A310F69643DB4A0A0AE3AD7730FB49`;
`candidate-integrated-runtime-audit.tsv` tem SHA-256
`22A7DC9360E35839C8581ECF1553A9418CFED637B0AE6DF38FC1426E00F48E3B`; o DLL
Release tem SHA-256
`75137D91EECCAB5444E8FE40D5564F5FFEB95313639DAFDB1B982501819855CF`.
Readiness permanece `NOT_READY`. A ISO segue somente leitura, com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Integração candidate auditada

O `candidate-integrated-runtime-audit` exige o resumo de proveniência da cadeia
candidate antes de validar o runtime. A matriz atual confirmou
`candidate_asset_provenance_valid=True`,
`candidate_mesh_state_crosscheck_valid=True` e `24/24` candidatos com objetos
`.ord`, companions `.orl`, bounds e geometria finita/indexada. Relatório atual:
`fifa_pc/candidate-integrated-runtime-audit.tsv`, SHA-256
`42A23A7F851D59A00FACE9E72C0AF28A65DA4F5FDCC5278FD283EABE228BC97E`.

Esse é um gate candidate-only; `original_semantics=not-claimed` permanece
explícito e nenhum binding original de material, UV, skinning, rigging, animação,
morph ou GPU é inferido.

## Cross-check SHPX/TGA

`shpx-export-crosscheck` lê um `.xsh` da ISO sem escrever nela, decodifica as
entradas com o leitor managed e compara os pixels RGBA com TGAs persistidas:

```text
FifaStreetPc shpx-export-crosscheck <caminho-do-iso> <arquivo.xsh> <diretório-export> [--report <saida.tsv>] [--summary <saida.tsv>]
```

Na execução de `data/aptanims/player1.xsh`, foram confirmadas 46/46 entradas,
com `verified-export-pixel-match`. A proveniência é
`workspace-generated-TGA; provenance-not-independent` e
`original_semantics=not-claimed`; o comando não atribui timing, pose, rigging,
skinning, animação, morph, material, UV ou GPU.

`eagl-candidate-asset-provenance-audit` relê a cadeia candidate
`data/models.big` → nested `.big`/`.ord` → `.xsh` → entrada SHPX e recalcula os
pixels. A execução real verificou 24/24 cadeias e 24/24 matches RGBA. O mesmo
passo releu 24/24 objetos `.ord`, 24/24 companions `.orl`, confirmou bounds e
geometria finita/indexada em 24/24 e encontrou `SHAPENAME` nos primitivos em
24/24. Isso é
proveniência candidate (`same-trunk-source-chain; candidate-only`), não binding
original de estado→geometria, material, UV, GPU, skinning, rigging ou animação.

Relatório atual: `fifa_pc/eagl-candidate-asset-provenance-audit.tsv`, SHA-256
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo,
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.

## Fechamento verificável da etapa SHPX — 2026-10-06

Release `0/0`, self-test `69/69` e matriz authored/candidate concluída. O par
full-stack gerado/replay coincide byte a byte, SHA-256
`05B6A34950B6D9462DAF6561CC03041D19FB5203A8E401A2693F054D9AD3281F`; o DLL
Release tem SHA-256
`DA9E629F205087991C1B995D52E41D41E301A12094A86F2DEBD013CE8C22E476`.
`fifa_pc/shpx-image-entry-audit.tsv` tem SHA-256
`E220D9811A04BC323B899AF21C339F64E5ED5B23A9A513599B12D3D9AF5F6859` e
validou `46/46` entradas PAL8 128x128. O readiness permanece `NOT_READY`, e a
ISO continua intacta com `2013003776` bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Auditoria estática adicional do XBE — 2026-10-06

`tools/analysis/xbe_string_xref_audit.py` leu `default.xbe` somente para
análise, com Capstone 5.0.9, e encontrou 1.929 strings literais de interesse e
944 referências x86 candidatas em 944 instruções de origem (939 imediatas e 5
de memória absoluta). As referências incluem literais observados de áudio,
`.xsh`, `.big`, `D3DTexture` e `EAGLAnimationBuffer`.

Relatórios: `fifa_pc/xbe-string-xref-audit.tsv` e
`fifa_pc/xbe-string-xref-summary.tsv`. SHA-256 do XBE lido:
`5fabb5b763be0cc8823e814ee343ac5be6950305330f46c5170e08eaadea910b`.
O resultado é `candidate-x86-reference-only`; não executa o XBE e não cria
bindings de materiais, UVs, skinning, rigging, animação, morph ou GPU.

O contexto local opcional gerou `fifa_pc/xbe-string-xref-call-context.tsv`
com 6.251 linhas, 790/944 referências com chamadas diretas próximas e 1.141
destinos executáveis distintos. É somente contexto candidato; não define
funções ou bindings.

O refinamento `--call-clusters` gerou
`fifa_pc/xbe-string-xref-call-clusters.tsv`: 1.141 agrupamentos por destino
executável observado, 306 com pelo menos duas strings em coocorrência local.
Isso não recupera funções nem atribui subsistemas; o relatório mantém
`original_semantics=not-claimed`.

A validação final da etapa repetiu Release `0/0`, self-test `70/70`, smokes,
runtime headless e replays determinísticos com `matched=True`; o readiness
continua `NOT_READY` e a ISO permaneceu inalterada.

## Bateria final verificada da etapa — 2026-10-06

Release: `0` avisos e `0` erros; self-test: `68/68`. A matriz executou os
nove cenários authored e um cenário candidate opt-in; os dez pares de replay
determinístico (`match`, `input`, `animation`, `audio`) deram `matched=True`.
O cenário candidate teve `140` frames, `280` samples de animação, `8` eventos
de áudio, `8/8` preparados/tocados e `0` sem rota. O relatório candidate
gerado/reexecutado é byte a byte igual, SHA-256
`24561111E7BCE6CE2A11171F0960F048FA57076677C8CCB432A3209CF1CA257B`.

A cobertura integrada authored está em `9/9` e a auditoria de invariantes
authored está em `9/9`; o gate agregado `clean-room-authored-runtime` está
verificado em `9/9`; a auditoria gerenciada candidate está
`verified-candidate`; o readiness continua `NOT_READY`. A ISO
permanece somente leitura, com `2013003776` bytes, atributo `Archive` e
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

A matriz desta etapa pode ser reproduzida por
`D:\projeto zeta\tools\run-clean-room-authored-matrix.ps1`. O orquestrador
gera e reexecuta os nove cenários authored e o cenário candidate, roda as
auditorias (incluindo `candidate-integrated-runtime-audit`), os smokes relevantes
e o readiness, e
interrompe se tamanho, timestamp, atributos ou SHA-256 da ISO mudarem.

A auditoria candidate gerenciada está em
`fifa_pc/candidate-integrated-runtime-audit.tsv`, SHA-256
`9ABDABC1246F4D161A498609F698CC3AB05FBFB10315BF2F81C08EF995E06A6D`.
Ela confirma flags candidate, hash do assembly atual, traces, replays e frame
PPM, mas mantém `original_semantics=not-claimed`; não cria bindings globais de
material, UV, skinning, rigging, animação ou GPU.

O readiness cruza esse TSV no gate separado `assets-candidate-runtime`, agora
`verified-candidate` (`24/24`, `passed=True`). Isso é apenas rastreabilidade do
runtime candidate; os gates originais `assets` e `rendering` seguem sem
promoção, com `candidate-integrated`/`False` e `not-proven`/`False`.

## Avanço verificado — intake de timing externo — 2026-10-06

Foi adicionado o comando `external-animation-timing-audit`, que valida um TSV
de observações temporais fornecido externamente: proveniência, hash do arquivo,
índices de amostra, tempo não negativo, monotonicidade por captura e
repetição por par banco/asset. O auditor não transforma essas observações em
frames, poses, rig, skinning ou bindings originais; sempre mantém
`direct_timing_binding=not-proven` e `original_semantics=not-claimed`.

O workspace atual não contém o TSV externo exigido. A execução verificável
produziu `status=missing-evidence`, `0` linhas e `0` capturas. Relatório
`fifa_pc/external-animation-timing-audit.tsv`, SHA-256
`3E82F4FF84E4095CA2844950A97A56FB5F165954BF3B3379A2BD4ED0DC026EDC`;
resumo, SHA-256
`4FF80AD21FB2848191CC38519BE7E6BA9E86E3B34C23FA259879B8908836928E`.
Esse resultado mantém o gate de animação/deformação aberto.

## Avanço verificado — integração candidate opt-in — 2026-10-06

A matriz passou a executar também um cenário cross-system separado com
`--candidate-material`, `--candidate-home-character` e
`--candidate-away-character`. A geração e o replay produziram
`matched=True` para match, input, animação e áudio, com 140 frames,
280 samples de animação, 8 eventos, 8/8 preparados/tocados e 0 sem rota. O
relatório exigiu e confirmou material candidate validado e os dois slots
habilitados; seu checksum de render foi
`088EC5D10096A738`, com 69 checksums distintos.

Os relatórios de integração gerado/replay coincidem byte a byte, SHA-256
`F8F03C7DC83BFD33EAD3D58526C1B19A1790A44209C7BF938819F55AB754187A`.
Isso é integração candidata opt-in do runtime managed; `render_group_state`
continua `unproven` e nenhum binding global de material, UV, skinning,
rigging, animação ou GPU foi criado. O gate original de assets/rendering não
foi promovido.

O avanço desta etapa adicionou `Runtime/CandidateIntegratedRuntimeAudit.cs` e
o comando `candidate-integrated-runtime-audit`. A auditoria gerenciada confirma
os dois relatórios, os oito traces, os quatro replays e o PPM do cenário
candidate; status `verified-candidate`, sem alterar os gates originais.

## Avanço verificado — auditoria de metadata temporal dos catálogos AXT — 2026-10-06

Foi adicionada `animation-bank-catalog-timestamp-audit`. Ela comparou o
timestamp numérico com a data textual presente na mesma linha de cada catálogo
`.axt`: `19/19` bancos e `1.584/1.584` linhas tiveram correspondência exata,
com offsets locais inferidos `-420` e `-480` minutos. Isso confirma uma relação
de metadata da linha do catálogo; não é binding de duração, frame, pose, rig,
skinning ou timing de animação.

O relatório tem SHA-256
`611B60012B5F37A639E2DC5AD47E829A6178FEA409A9D6CE4CA1360640222C32` e o
resumo `E167D7254E82C36D6A49276B2721A828FBD52EA048CBADAFEBCE6E33959D37FE`.
A DLL Release usada para gerar/reexecutar a matriz tem SHA-256
`D0660B1FEF4F2781152EDB95797145201F99FA4A2E0780B914CF031F0713E459`.
O readiness foi regenerado, SHA-256
`282D82CC21E3EE600D6DCDFF6A95B754579AB9975D2F02F661BCB039F4DC1339`, e
continua `NOT_READY`; `direct_timing_binding=not-proven`.

## Avanço verificado — correlação exhaustive de áudio — 2026-10-06

Foi adicionado `audio-event-payload-wave-correlation-exhaustive-audit`, que
percorre todos os índices de som candidatos dentro de um limite explícito por
banco. A execução com `512` por banco observou `368` candidatos em `16`
bancos, decodificou `239`, e encontrou `0` pares PCM idênticos e `0` hashes de
evento correspondentes. Isso é controle estrutural/negativo; não é seleção
EVT/ABKC/sample nem semântica original.

Relatório SHA-256:
`BDE7CF7E58E09E17D2BBB8932E413249D51264424E5A1C32E8B5E30D404E345E`;
resumo SHA-256:
`DD798636EC35EF78E07E47176A0220C9A919601F2FF47D5CD51FE8DB41DEF23B`.
O readiness registrou `audio_payload_exhaustive_report=verified-structural-negative`,
mantendo `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`.

## Atualização verificada — invariantes de gameplay authored — 2026-10-06

Foi adicionado `clean-room-gameplay-invariant-audit`, que acompanha os nove
cenários authored determinísticos e verifica estado finito, limites de posição
e altura da bola, monotonicidade de relógio/placar, ordem de eventos, condição
de término e presença das ações requeridas. O resultado foi `9/9` cenários,
`677` frames e `49` eventos, status `verified-authored`.

Relatório `fifa_pc/clean-room-gameplay-invariants.tsv`, SHA-256
`B6C4F29036835A22BFB36D6401F2C31A37B6C66DC62D3EB477A8C5396C6433EE`.
Isso verifica somente invariantes do runtime clean-room novo; regras originais
continuam `not-claimed`. O self-test ficou em `65/65` e o readiness segue
`NOT_READY`, com SHA-256
`EDE51254C0D754EF46463183ADE17FCA6104B941E06032506BA96535494983AC`.

## Atualização verificada — cenário first-to-goals integrado — 2026-10-06

O cenário authored `first-to-goals` foi acrescentado à matriz cross-system.
Em `80` frames, o runtime terminou `HOME 1 - 0 AWAY` com `FULL_TIME`,
produziu `7` eventos de match/áudio, `160` samples de animação e os quatro
replays determinísticos deram `matched=True`. O frame PPM `960x540` foi
validado com `341.782` pixels authored visíveis.

Os relatórios de integração gerado/replay são iguais, SHA-256
`D37356F7DE6F7EE9F04FFB83BC1D5973F6C278C1F3EAAC9C59D3D98A3089F119`.
As auditorias `gameplay-coverage` e `gameplay-audio-coverage` passaram `9/9`
cenários, `49/49` eventos, `0` sem rota e `14/14` ações; os relatórios têm,
respectivamente, SHA-256 `22A615C2CB14477D8EB4DCC085AAA2515F97CF074466B2B562F9D394FD24A056`
e `CC9F605B290DF8DA552703F45DB344D94320D4FC1FC6D5FEAB193B01B8C7D449`.
Isso é cobertura authored do runtime novo; `original_semantics=not-claimed`.

## Atualização verificada — correlação estrutural de áudio ampliada — 2026-10-06

`audio-event-payload-wave-correlation-audit` agora aceita
`--max-bank-attempts`. A medição registrada usou `64` candidatos de banco:
`17/17` candidatos EVT decodificados, `15/16` bancos ABKC/AST decodificados,
`0` pares de hashes PCM idênticos e `0` hashes de evento com correspondência.
O resumo registra `bounded_policy=first_64_bank_candidates`.

Relatório: `audio-event-payload-wave-correlation.tsv`, SHA-256
`8ED4E39C6748CEC54706D9F107DCFE7D887C941ED43FDF854829C60568104E3D`;
resumo, SHA-256
`4D949566DB53FA348C939388D280D94C9F825BB19BB19BA6B31FAA70C2C2A38D`.
Isso reforça apenas a evidência estrutural/negativa: `direct_selection_binding`
continua `not-proven`, `audio` continua parcial e
`original_semantics=not-claimed`.

## Atualização verificada — cobertura integrada authored por cenário — 2026-10-06

`Runtime/CleanRoomIntegratedCoverageAudit.cs` adiciona o comando
`clean-room-integrated-coverage-audit`. Ele cruza, para os nove cenários
authored (`full-stack`, `aimed`, `round`, `gamebreaker`, `juggle`, `panna`,
`full-time`, `steal` e `first-to-goals`), o relatório de integração com renderização, assets
authored de personagem/cena, animação procedural, áudio e os quatro replays.
Isso é um gate do runtime PC novo; não promove bindings originais.

Resultado: `9/9` cenários, `677` frames renderizados, `1.354` samples de
animação, `49` eventos de áudio, todos preparados/tocados, traces válidos e
replays `matched=True`. O relatório
`clean-room-integrated-coverage.tsv` tem SHA-256
`DC6E37059884B9AC450D4FD73738D9D294EC8CE1A08C707CCD49AA61EC876A7B`.
Além do checksum de cada cenário, a auditoria validou os nove frames PPM
`960x540`, com `341.782` pixels authored visíveis por frame.
Release: `0/0`; self-test: `65/65`; o full-stack authored-only
gerado/reexecutado é byte a byte igual, SHA-256
`BF2F85A42960ED5DCA0E1C84DB4481BAA66749263C31FDA6708C51224C67589A`.
Cada integration report também registra o SHA-256 do assembly Release atual:
`9CF41DD8555721A043BF58C6456755D2ABA3970EE417D73653EB0006775F37BB`.

O readiness continua `NOT_READY`, mas agora expõe
`authored-integrated-coverage=verified-authored`, com SHA-256
`EDE51254C0D754EF46463183ADE17FCA6104B941E06032506BA96535494983AC`.
Os gates originais de assets/renderização, gameplay, áudio e deformação ainda
exigem evidência independente; `original_semantics=not-claimed` permanece.

## Atualização verificada — manifesto de personagem authored — 2026-10-06

`Runtime/CleanRoomCharacterAssets.cs` adiciona o manifesto
`clean-room-character-manifest-v1`, com IDs authored para silhueta e paleta.
`CleanRoomCharacterRenderer` valida esse manifesto antes de desenhar; os IDs
descrevem somente assets criados para o runtime managed e não são bindings de
mesh, material, UV, skinning, rigging, morph, animação ou GPU da ISO.

Release: `0` avisos e `0` erros; self-test: `62/62`. O smoke authored-only de
140 frames registrou `authored_character_asset_frames=140`, checksum
`F26A04CC3CBEBDEA` e relatório SHA-256
`C0969EFA056C678494D70E97941A6EB6D1748E448F640FD2F84B4291F1900F33`.
O full-stack candidato manteve checksum `088EC5D10096A738`; os quatro replays
de match, input, animação e áudio retornaram `matched=True`, com relatórios
byte a byte iguais e SHA-256 `BF9FA14EB4CC5EC1580535EF05D4F97AA0F256278FA9E7CE3311F71E5EE82DE8`.
O headless de 80 frames tocou `6/6` cues e produziu checksum
`641ED53BD0DAE660`; o preview authored gerou checksum `09A0C9D4F496D075`.

O readiness continua `NOT_READY`, agora com `character-authored-assets` em
`verified-authored`, SHA-256 `6C86A5895D66F822D0E06A2A41C89F04C37001FA0B829DE132431BC08B507330`.
A ISO permaneceu sem escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
`original_semantics` continua `not-claimed`.

## Atualização verificada — manifesto de animação authored — 2026-10-06

`Runtime/CleanRoomAnimationAssets.cs` agora descreve o manifesto
`clean-room-animation-manifest-v1`, consumido pelo `CleanRoomPlayerAnimator`
para resolver somente perfis procedurais authored (`KICKOFF`, `TRICK/PANNA`,
`JUGGLE`, `SHOT`, `PASS`, `STEAL`, `GOAL`, `GAMEBREAKER`, `RESTART` e
`FULL_TIME`). Isso organiza assets da reimplementação managed; não decodifica
AXT nem afirma frames, timing, pose, rigging, skinning ou morphs originais.

Release: `0` avisos e `0` erros; self-test: `60/60`. O headless de 80 frames
tocou `6/6` eventos authored. O full-stack de 140 frames preservou o checksum
`088EC5D10096A738`; os replays determinísticos de match, input, animação e
áudio retornaram `matched=True`. Os dois relatórios de integração são byte a
byte iguais, SHA-256 `70DE5BAB17EDE85DDF847F8C1D8B5185E944AD4051AFFF534CE2AAE4B93AAA83`.
Os smokes opt-in também passaram: `JUGGLE=4` em 80 frames, `STEAL=1` com
oponente/áudio e `GAMEBREAKER=1` após quatro `TRICK`; os checksums authored
foram `8054AF1DFEEB0157`, `AAD987077877F844` e `56FBB193D71CA682`.
O readiness continua `NOT_READY`, SHA-256
`CE868EA45F8CA772764D1BA8296A7BD7200EA2575150D572084DA0F887A8D153`.
A ISO permaneceu com tamanho `2013003776`, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Os gates semânticos originais continuam separados; `original_semantics` segue
`not-claimed`.

## Atualização verificada — manifesto de áudio authored — 2026-10-06

`Runtime/CleanRoomAudioAssets.cs` adiciona o manifesto
`clean-room-audio-manifest-v1` para os oito candidatos de cue usados pela
política authored. O `CatalogAudioMixer` valida o candidato contra esse
manifesto antes de preparar o WAV/PCM; isso é uma garantia do runtime novo e
não uma tabela EVT→ABKC→sample original.

O full-stack verificou `8/8` eventos preparados/tocados, `0` sem rota e
`1.180.164` bytes PCM com SHA-256
`2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`. Release:
`0/0`; self-test: `61/61`; os replays de match/input/animação/áudio foram
`matched=True`. Os relatórios de integração são iguais, SHA-256
`4DD380D1896481F5EEE4CACF1E46E1D3E4B2BF5CF5B42847C6FF924174B2B0F0`.
Readiness: `NOT_READY`, SHA-256
`F485A2845003F98867ECCF31496761971AE6C574B06CA9EDACEF19BDEE471327`.

## Atualização verificada — correlação estrutural de áudio e bateria final — 2026-10-06

`audio-event-payload-wave-correlation` decodificou `17/17` candidatos EVT e
`15/16` bancos candidatos ABKC/AST sob uma política limitada de candidatos.
Nenhum dos `0` pares PCM coincidentes foi encontrado (`exact_pcm_hash_pairs=0`),
portanto o resultado é somente controle estrutural/negativo: não atribui seleção
de evento, sample, gameplay ou semântica original. O relatório tem SHA-256
`00251215E62B4437C5B0EE3917C12E8DF66C1E278F1982EAF3AE95232E0432BD` e o resumo
`C5AEC72F58CAC11EFD2942D6F598C3401A5433475E4A3C7EF728D7893E6448F4`.

O fechamento da etapa teve build Release `0/0`, self-test `59/59`, previews
XPR reais, smoke de material candidato, auditoria de influência raster e
headless verificados. O full-stack de 140 frames reexecutou match, input,
animação e áudio com `matched=True`; os cinco pares de relatórios são byte a
byte iguais. O readiness permanece `NOT_READY`, com SHA-256
`4982801DDA56466467E7DC6197BDB22EF481EE41BFF5FE07F971BDB27B8B672D`.
`original_semantics=not-claimed` continua obrigatório.

## Atualização verificada — digest PCM do áudio authored — 2026-10-06

O `CatalogAudioMixer` registra o PCM concatenado das cues authored usadas pelo
gameplay, sem o header WAV. O full-stack de 140 frames preparou `1.180.164`
bytes com SHA-256
`2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`; o replay
determinístico produziu o mesmo valor. Os relatórios de integração gerado e
reexecutado são iguais, SHA-256
`706E895FE9BAFD6A34BF6FB5C74A4C93110F1B6ECE193C18E8C625CD74C57823`.
É verificação do caminho authored do PC, não binding de seleção EVT/ABKC
original. Self-test: `59/59`; readiness: `NOT_READY`.

## Atualização verificada — manifesto de assets authored da cena — 2026-10-06

`Runtime/CleanRoomSceneAssets.cs` concentra os assets authored de quadra, gols
e bola em um manifesto consumido pelo `PreviewRenderer`:
`clean-room-court-v1|clean-room-goal-v1|clean-room-ball-v1`. O runtime verificou
`140/140` frames desses assets e manteve o checksum `088EC5D10096A738`.
Os relatórios de integração gerado/reexecutado são iguais, SHA-256
`CA03EC0029301E7336E0CA839DAE717A60260F2F130584E6C5051AA0AA8E72FD`.
É integração authored do PC; não cria binding original de mesh, material, UV ou
GPU. Readiness: `NOT_READY`, SHA-256
`661F2013630F999E2A79E27682A64253E7DA671D946ADD183B8E0FA0E22976E1`.

## Atualização verificada — cobertura de formatos XPR0 — 2026-10-06

O decoder managed de `XPR0` foi ampliado para os códigos de formato já
identificados no header: `R5G6B5`, `A8R8G8B8`, `X8R8G8B8`, `DXT1`, `DXT3` e
`DXT5`. A implementação valida o nível base disponível e produz RGBA em
memória; não interpreta isso como material, UV, estado de renderização ou
comando GPU da ISO. Os formatos adicionais são cobertos por fixtures do
self-test, enquanto as duas texturas XPR0 presentes no catálogo real continuam
`titleimage.xbx` (`128x128`, `DXT1`) e `saveimage.xbx` (`64x64`, `DXT1`).

Os dois `texture-preview` reais foram reexecutados em Release, com a ISO aberta
somente para leitura, e produziram os PPMs `titleimage.ppm` e `saveimage.ppm`.
Build Release: `0` avisos e `0` erros; self-test: `58/58`. O readiness não
muda de `NOT_READY`, pois cobertura de payload não cria binding de asset ou
semântica original. A ISO permanece com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização verificada — âncoras de nomes nos bancos — 2026-10-06

`Runtime/AnimationBankCatalogPayloadAnchorAudit.cs` mede, por banco, os
offsets das duas strings técnicas `_e/_r` encontradas pelo auditor anterior.
Na ISO foram observados `19/19` bancos completos, `1584/1584` linhas com duas
âncoras, `3168` offsets, `1584` gaps intra-nome, `1568` gaps entre linhas e
ordem crescente em `19/19` bancos. São intervalos estruturais observados, não
boundaries de records nem bindings de frame, pose, rig, timing ou skinning.

Relatórios: `animation-bank-catalog-payload-anchor-audit.tsv` com SHA-256
`B63D77DF1E99F08576829FB016A1DEE5CB4C71F97F92E75E442826A36E481FBD` e o
resumo com `372E58900EF0F173652F923DD3F49BC777B76006437D53675D91C662626E0ECA`.
O readiness continua `NOT_READY`, agora com SHA-256
`C733311CF7340C1A8B5E0C8170B2534BFAB7F0CAA30426B810FEFDB3B85F3198`.
Build Release: `0/0`; self-test: `58/58`; `original_semantics=not-claimed`.

## Atualização verificada — asset de personagem authored — 2026-10-06

`Runtime/CleanRoomCharacterRenderer.cs` adiciona um asset de personagem
procedural authored, com geometria de silhueta e paleta criadas no runtime
managed. É código novo e não reivindica mesh, material, UV, skinning, rigging,
morph, animação ou comando GPU extraído da ISO. O renderer só usa esse caminho
quando os slots candidatos estão desligados.

O smoke authored-only executou `140/140` frames com o asset e produziu
checksum `243EF331249601A5` (`122` checksums distintos). O relatório
`gameplay-authored-character-integration.tsv` registra
`authored_character_asset_frames=140` e tem SHA-256
`78FEC9A56961C0EC35DC3024DB81157451B4FA4B62EC094C0AF61F26219CCA5E`.
No full-stack candidato, os slots candidatos ficaram ativos e o contador do
asset authored foi `0`, deliberadamente; o checksum candidato foi
`088EC5D10096A738`.

Build Release: `0` avisos e `0` erros. Self-test: `58/58`. Readiness: `NOT_READY`,
TSV com SHA-256
`0C537BBEA1140423C73F39A4B4EEC32B8292D630065B3F7A5876767867B25A1A`.
Coverage authored: `8/8` cenários, `42` eventos e `14` ações, SHA-256
`E8655638CFE171E10A14DA5B7C7CBFC6C5C3FFED2B3D879BE10F42ADA96C996D`.
Os replays full-stack de match, input, animação e áudio permaneceram
`matched=True`; os relatórios de integração têm SHA-256
`AF8A2B97C05E438B267536A612D8ADE90CE21ED1C20F40A32D25FE28E92CBBFC`.

## Atualização verificada — nomes de animação no payload — 2026-10-06

O comando `animation-bank-catalog-payload-name-audit` cruza os nomes dos
catálogos `.axt` com strings ASCII observadas no `.data` do ELF correspondente
(`.ord` + `.orl`). A leitura encontrou `19/19` bancos válidos e `1584/1584`
linhas com os pares técnicos `<nome>_e`/`<nome>_r`, todos com offsets e sem
ausências. Isso comprova uma repetição estrutural de identidade
catálogo↔payload, mas não fornece limites de records nem semântica de frame,
pose, rig, timing, skinning ou animação.

Relatórios: `animation-bank-catalog-payload-name-audit.tsv` com SHA-256
`3D2CD9FBA876636011CFED8876204C1BC6F84A23334F8A06D42CA276671F4900` e o
resumo com SHA-256
`FAE1691C64C4A99B968F7DFC8F349368892D924F10C44CBF5315178BEC3A673D`.
O readiness incorporou a linha `animation-catalog-payload-names=verified-structural`
e continua `NOT_READY`, com SHA-256
`0C537BBEA1140423C73F39A4B4EEC32B8292D630065B3F7A5876767867B25A1A`.
O build Release ficou em 0/0 e o self-test em `58/58`.

## Atualização verificada — separação dos gates authored — 2026-10-06

O relatório de prontidão agora distingue explicitamente o runtime authored que
já foi exercitado dos gates que ainda exigem semântica original comprovada. A
execução atual verifica `assets-candidate-runtime` (`24/24`, `0` rejeições),
`rendering-authored-runtime` (`140` frames, `69` checksums distintos),
`gameplay-authored-runtime` (`8` eventos),
`gameplay-authored-coverage` (`8/8` cenários, `42` eventos, `14` ações),
`audio-authored-runtime` (`8/8`
preparados/tocados, `0` sem rota) e `animation-authored-runtime` (`280`
amostras). Essas linhas descrevem somente o runtime novo e os caminhos
candidatos opt-in.

Os gates originais permanecem separados e falsos: assets continuam
`candidate-integrated`, renderização `not-proven`, áudio
`partially-integrated` e animação/deformação `structural-only`. Nenhuma linha
cria binding de UV, material, skinning, rigging, morph, timing, comando GPU ou
seleção original de áudio; o readiness geral continua `NOT_READY`.
O TSV atual `fifa_pc/project-readiness.tsv` tem SHA-256
`75A8E6DC7B88729B92FBA635D26819738EF651F69F0964BF92E5696D85C9F6EC`.

O comando `gameplay-coverage-audit` grava
`fifa_pc/gameplay-coverage.tsv` e valida os traces/replays authored de
full-stack, aimed, rodada, gamebreaker, juggle, panna, FULL_TIME e STEAL.
Ele não interpreta regras do original.
O SHA-256 do relatório de gameplay coverage é
`E8655638CFE171E10A14DA5B7C7CBFC6C5C3FFED2B3D879BE10F42ADA96C996D`.

## Atualização verificada — integração authored com input — 2026-10-06

O relatório cross-system agora inclui também a evidência do input consumido:
`input_samples=140` e `input_trace_written=True`. A execução Release com os
dois slots de material candidato, gameplay authored, oponente e WinMM produziu
140 frames, 8 eventos de partida, 140 samples de input e 8 eventos de áudio;
os 8 eventos foram preparados/tocados e 0 ficaram sem rota. Os replays de
partida, input e áudio retornaram `matched=True`, e os três pares têm conteúdo
idêntico. Os relatórios de integração gerado/reexecutado também são idênticos,
com SHA-256
`322EDD999DBBA2CBE5952A934DC85B60640A9930D7D2458B811C60474FE90376`.

Essa é evidência repetível da integração authored do runtime PC; não comprova
input, render state, UV, material, skinning, rigging, animação, GPU ou áudio
original. O controle de renderização continua deliberadamente aberto: o smoke
registrou 0 pares de estado exatos e 0 símbolos de relocação compartilhados.
O readiness permaneceu `NOT_READY`; o relatório atual tem SHA-256
`4C1AD42C9CA32E08ECFBCC374C73294A8367B7D9202EA3A1A471A546531097F9`.
Os smokes repetidos registraram 24/24 materiais e geometrias válidos, 20
influências raster candidatas verificadas e 42/42 eventos de áudio na cobertura
authored, sem alterar os gates semânticos.

O full-stack também exporta `--animation-trace-output` e aceita
`--animation-trace-expected`. O trace tem duas amostras por frame, uma para
cada jogador, totalizando 280 amostras em 140 frames; o replay authored foi
`matched=True` e o par tem SHA-256
`57C124E9CDD044CD8DE0C2319C9AD11FC35930E0589C199B87C4DB0007342D09`.
Esse trace serializa somente a pose procedural nova consumida pelo renderer
PC. Ele não é decoder de AXT/morph nem prova de rig, skinning, timing ou
animação original.

Na auditoria bounded de áudio da mesma etapa, 17/17 candidatos EVT tiveram
header e payload decodificados, 144/144 layouts mono foram validados e 15/16
bancos ABKC foram decodificados. A busca de palavras alinhadas encontrou 0
ocorrências em `pcpahdr.big`, `pcpadat.big` e nos 16 bancos. Esses números são
evidência de payload e de um controle negativo de seleção; não fornecem
evento→sample original.

## Prontidão atual

O comando abaixo materializa a matriz de gates atual, incluindo hash/tamanho da
ISO e os limites entre evidência comprovada, candidato e semântica ainda não
decodificada:

```text
dotnet run --project fifa_pc -- project-readiness-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\project-readiness.tsv"
```

O resultado atual é `NOT_READY`: a integridade da ISO está verificada; assets
estão integrados apenas como candidatos; a renderização não tem associação
estado→geometria comprovada; gameplay é uma simulação clean-room; áudio tem
decoders e uma política de rotas authored; e animação/deformação permanece
estrutural. O relatório não tenta transformar nenhuma dessas lacunas em
bindings de UV, material, skinning, rigging, timing ou GPU. A coluna de notas
do gameplay também registra o replay authored de rodada quando os traces
`gameplay-script-round-trace.tsv` e `gameplay-script-round-replayed.tsv` estão
presentes e têm o mesmo hash; isso não altera o gate de regras originais.
O mesmo relatório verifica `authored_gamebreaker_trace_replay=verified` quando
`gameplay-clean-room-gamebreaker-trace.tsv` e seu replay estão presentes, com
7 eventos, `GAMEBREAKER` e `GOAL_HOME`; isso também é evidência do runtime novo,
não de regra original.
Ele verifica `authored_juggle_trace_replay=verified` quando os traces authored
de juggle estão presentes, com 7 eventos, `JUGGLE` e `GOAL_HOME`; essa nota
também descreve somente o runtime clean-room.
Também verifica `authored_steal_trace_replay=verified` para o cenário
`--steal-demo --opponent-ai`, com `KICKOFF` e `STEAL`; isso cobre a rota
authored `STEAL->tackle` sem alegar uma IA ou regra original.
Também verifica `authored_panna_trace_replay=verified` para a fixture de panna
com 3 eventos (`KICKOFF`, `PANNA`, `FULL_TIME`) e
`authored_full_time_trace_replay=verified` para o encerramento authored com 2
eventos. Esses traces ampliam a verificação do runtime novo, sem alegar regras
ou eventos equivalentes aos originais.
O relatório também possui o gate separado `full-stack-integration`, que só
confirma a execução conjunta authored dos subsistemas no PC; ele não fecha os
gates semânticos de rendering, assets ou áudio original.
O gate de áudio também registra `authored_audio_trace_replay=verified` para o
trace combinado de 8 rotas, incluindo `KICKOFF -> intro`, e
`authored_steal_audio_trace_replay=verified` para as 2 rotas de `STEAL`.
Esses campos confirmam apenas a repetição textual da política authored do PC.
O smoke cross-system `--integration-report` também foi exercitado com os dois
slots candidatos: 140 frames, 8 eventos de gameplay, 8 cues preparados/tocados,
0 sem rota e checksum `088EC5D10096A738`. O TSV registra
`original_semantics=not-claimed` e o replay do relatório foi byte a byte igual;
isso comprova a integração authored do runtime, não bindings originais.
O relatório também registra `distinct_render_checksums=69` nos 140 frames;
isso comprova variação da apresentação authored durante o runtime integrado,
mas não é evidência de pose, rigging, skinning ou animação original.
O SHA-256 igual dos dois relatórios TSV é
`F0329A2C5189CC326D9099783FC771C16BD8227B48B71DB989D27E9DB3E2DE8F`.

Nesta etapa foi acrescentado o controle estrutural
`animation-morph-relocation-profile-audit`. Ele cruza apenas os conjuntos
opacos de gaps de relocação já publicados para os 393 layouts morph com os
histogramas dos 19 bancos de animação: 393/393 layouts foram parseados,
53 tiveram algum banco contendo o conjunto de gaps e 0 tiveram igualdade
exata de conjunto, em 159 pares de contenção. O resultado é um controle de
recorrência entre relatórios separados; não é evidência independente de
vínculo nem decoder de frame, pose, rig, timing, skinning, material, UV, GPU
ou animação. Artefatos:

```text
dotnet run --project fifa_pc -- animation-morph-relocation-profile-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\animation-bank-data-relocation-audit.tsv" "D:\projeto zeta\fifa_pc\animation-morph-relocation-profile-audit.tsv" --summary "D:\projeto zeta\fifa_pc\animation-morph-relocation-profile-audit-summary.tsv"
```

O controle não promoveu nenhum binding e o readiness permanece `NOT_READY`.

O `project-readiness-audit` também lê o resumo desse controle e registra os
contadores na nota `animation-deformation`; essa integração é somente de
rastreabilidade. O gate continua `structural-only`, pois não há evidência
independente de timing, pose, rig ou skinning.

A política authored de áudio está centralizada em
`fifa_pc/Runtime/CleanRoomAudioCuePolicy.cs`; o mixer e o readiness usam o
mesmo manifesto de 14 rotas. Isso evita divergência entre execução, teste e
documentação sem transformar a política em binding original.

No catálogo real, o readiness confirma `14/14` rotas com candidato nomeado
utilizável e `8` cues únicos; isso verifica disponibilidade do caminho authored,
não seleção de evento original.

Executar o primeiro loop headless:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 3
```

Executar a sequência determinística de integração de gameplay:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo
```

Para exercitar a variante clean-room de chute com mira vertical, use o modo
opt-in `--aimed-demo`:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --aimed-demo
```

Esse cenário emite `MoveY=0.75` no disparo, reporta
`input_mode: deterministic-aimed-demo` e produziu o checksum
`801159854A2BC6F8`. É uma integração de controle nova e verificável; não é
semântica decodificada do jogo original. O modo `--demo` continua sendo a
referência reta `5405DE72BE1B4FC1`.

O par authored do cenário também é auditado pelo readiness. A execução com
`--match-trace-output fifa_pc/gameplay-clean-room-aimed-trace.tsv` produziu 6
eventos (`SHOT` e `GOAL_HOME` incluídos); a reexecução com
`--match-trace-expected` retornou `matched=True` e escreveu
`gameplay-clean-room-aimed-replay.tsv`. A linha separada
`gameplay-aimed-trace` fica `verified-authored`, enquanto o gate geral de
gameplay continua falso. Isso comprova apenas o replay determinístico do
runtime novo e não uma regra de mira decodificada da ISO.
O readiness também expõe `audio-aimed-trace=verified-authored` quando o mesmo
cenário é repetido com `--play-audio`: 6 eventos, `SHOT->attackgoal`,
`GOAL_HOME->goal`, todos tocados. Essa linha é cobertura da política authored,
não seleção original de EVT/ABK.
O comando aceita também `--input-trace-output` e
`--input-trace-expected`; no aimed-demo, o par
`gameplay-clean-room-aimed-input-trace.tsv` / replay contém 80 amostras e
confirma `frame=67`, `MoveY=0.75`, `shoot=True`. Isso torna observável a
entrega do controle authored sem alegar binding de controle original.

Para verificar explicitamente a saída WinMM da cue clean-room `goal` (Windows;
isso produz áudio audível), acrescente `--play-audio`:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --play-audio
```

O script exercita posse, drible, passe completo, recuperação e gol, além de
verificar o encaminhamento das ações ao mixer; ele não atribui essas ações a
IDs ou cues originais de áudio.
O relatório também mostra `match_action_counts`, `match_action_sequence`,
`match_last_action`, `match_possession`, `match_tricks`, `match_pannas` e o
medidor authored `match_gamebreaker_meter`. Na sequência
demo validada, o histograma esperado é
`GOAL_HOME=1,KICKOFF=1,PASS=1,PASS_COMPLETE=1,SHOT=1,TRICK=1`; esses nomes
descrevem somente a simulação clean-room e não são bindings de eventos do
conteúdo original.
Ele também imprime `match_home_position`, `match_away_position` e
`match_ball_position`, permitindo verificar o estado final da simulação sem
atribuir semântica aos formatos de asset.

Para materializar a sequência authored em um artefato auditável, use
`--match-trace-output`:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\runtime-gameplay-trace.tsv"
```

O TSV registra `sequence`, `frame`, ação, placar e posse. Na execução validada
foram 6 linhas de evento incluindo `KICKOFF`, `TRICK`, `PASS`,
`PASS_COMPLETE`, `SHOT` e `GOAL_HOME`; o arquivo é telemetria da simulação
managed authored e não um inventário de eventos ou bindings recuperados do
jogo original.

Para fornecer controles authored por arquivo, use `--input-script` com um TSV
de seis colunas (`frame`, `move_x`, `move_y`, `pass`, `shoot`, `trick`). Uma
sétima coluna opcional `panna` habilita um controle authored explícito:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-input.tsv" --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-script-trace.tsv"
```

O leitor exige frames inteiros contíguos começando em zero, eixos finitos em
`[-1,1]` e booleanos `0`, `1`, `false` ou `true`. A sétima coluna opcional
`panna`, a oitava `gamebreaker` e a nona `juggle` expõem controles authored
explícitos; `gamebreaker` só dispara quando o medidor chega a 100. O exemplo é uma fixture de
controle authored e curta: termina antes da posse da bola, portanto seu trace
contém somente `KICKOFF`; isso verifica o caminho de entrada, não uma regra do
jogo original. `--input-script` não pode ser combinado com os demos
determinísticos.
Na execução validada, o fixture teve SHA-256
`B2F2969BFF1FA22387158B197EEF09479D8BDD682D26B6AB139B11F0A2AE22E8` e o
trace curto teve SHA-256
`6288D477F63B66EB445F24E6C55D0F3BD0635B0D0BF7F5480CAD10754AD16D52`.
Uma fixture authored de 80 frames, em
`fifa_pc/gameplay-clean-room-demo-input.tsv`, reproduziu o cenário `--demo`
com checksum `C0249FD97A01C3F4`, 6 eventos e `GOAL_HOME`; seu trace teve SHA-256
`2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`. Essa
igualdade verifica o caminho de entrada e a simulação nova, não uma regra ou
input do jogo original.

O fixture `gameplay-clean-room-panna-input.tsv` exercita essa sétima coluna com
`--match-rule goals-or-panna --target-goals 9 --target-pannas 1`. Ele produz o
evento authored `PANNA`, encerra com `FULL_TIME` no frame 31 e prepara uma row
`panna`; a associação é nova no runtime e não um binding de áudio ou evento da
ISO.

O fixture `gameplay-clean-room-gamebreaker-input.tsv` usa as oito colunas para
carregar o medidor com quatro `TRICK` e disparar `GAMEBREAKER`. Em 40 frames a
execução produziu 7 eventos (`KICKOFF`, quatro `TRICK`, `GAMEBREAKER` e
`GOAL_HOME`), `match_gamebreakers=1` e `match_gamebreaker_meter=0/100`. O trace
`gameplay-clean-room-gamebreaker-trace.tsv` foi reexecutado com
`--match-trace-expected`, coincidindo byte a byte com
`gameplay-clean-room-gamebreaker-replayed.tsv`; ambos têm SHA-256
`BC17D33716D221F1495434E12D4C3836957FB4BB83D0E9AA73AAC8CC7B43A4EB` e a
fixture tem SHA-256
`81F9DFDC8AB37681F5309C92D1BD8704333258E08876A621CE92172E18A67D6C`.
Também existe o demo separado `--gamebreaker-demo`. Tudo isso é uma mecânica
nova do runtime clean-room: não afirma controle, medidor, animação, áudio ou
binding GPU decodificado da ISO.
Com `--clean-room-characters`, o demo de 80 frames produziu checksum
`3311E2054549E154`; a fixture/replay de 40 frames produziu
`2C3F702AB025DA33`. Esses checksums são apenas da apresentação PC authored.

No host Win32, a mesma entrada authored está disponível pela tecla `G` quando a
janela está em foco:

```text
dotnet run --project fifa_pc -- run-windowed "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --clean-room-characters
```

`G` é somente um controle novo do runtime managed; a integração não implica
binding de controle ou regra do jogo original.
No mesmo host, `N` emite `Panna`; ambos são controles authored documentados na
ajuda da janela e não bindings de input original.
`J` emite `Juggle`, também como controle authored explícito.
O host também desenha um HUD authored no framebuffer Win32: placar, tempo e
barra do medidor. Ele é ativado somente nesse caminho, portanto não altera os
checksums headless de referência. Esses textos e glyphs são UI nova do PC, não
uma leitura de textura, fonte, layout ou comando GPU da ISO.
Para QA visual offline, `render-preview` aceita `--authored-hud` e grava o
mesmo HUD authored no PPM; a opção é explícita e não altera o caminho headless.
Exemplo completo do preview candidato:

```text
dotnet run --project fifa_pc -- render-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\candidate-preview-authored-hud.ppm" --clean-room-characters --authored-hud --candidate-material
```

A execução validada produziu checksum `F2DD798FDD206BD9`. A imagem demonstra
somente a apresentação PC composta (quadra, title image, mesh candidato e HUD
authored); não comprova UV, material final, rigging, skinning, animação ou GPU
original.

## Interseção exata do join de renderização — 2026-10-06

Além do join por janela, o auditor compara o par exato de endereço da janela
e da instrução. A execução real encontrou `4` janelas e `8` rows coincidentes
entre transferências indiretas e referências. Isso é evidência estática
localizada, não função/ponteiro recuperado nem binding de material, UV,
skinning, rigging, animação, morph ou GPU; `original_semantics=not-claimed`.

Validação: Release `0/0`, self-test `72/72`, Python `20/20`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`3AD11C6CB09CE3FA2B59F317C612C049B670D3A59C8274FB63FD4D1DD5BB4E3D`.
Join audit/summary:
`59E06AC832B3CFF34DD7F00392052EC9CE83BD0C1785525F0AF8411286C8C7DD` /
`A101B0E7E24548621F6F3EC2A1CB892F996176C4D12D0131DFB3305F8B73802D`.
Readiness: `6F0919D54C5C897041E6BAF4237455B234E4BDF7321A3910488DE6D93B54FB01`.
A ISO segue sem escrita e mantém SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Join estrutural das janelas de renderização e indireções — 2026-10-06

O auditor `xbe_render_indirect_join_audit.py` une por endereço virtual as
`64` janelas de render, `22` transferências indiretas (`21` calls, `1` jump),
`8` referências e `3` words brutas. Foram confirmadas `3/3` slices
executáveis e terminadas por `ret`; `original_semantics=not-claimed` permanece
explícito.

O subgate `rendering-static-xbe-indirect-window-join` é
`verified-structural`; renderização geral continua `not-proven`. Nenhum
binding de material, UV, skinning, rigging, animação, morph ou GPU foi criado.

Validação: Release `0/0`, self-test `72/72`, Python `20/20`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`7D6D245F1E591C968F6C9D0DFECF3AD7773A3CE2DF25B36889AB987483A2E983`.
Relatórios join audit/summary:
`E10ADF511CDDFF4F1604DA4EFDBB312848FBEF2491F1E8764DD469919DB08A68` /
`233EDEB9C3F5D82FCF446742DE65F7C9585A21D47FAD29DF02D1150596FDD188`.
Readiness: `879B82F85C986E71F1F224E8006D650F4EE91D580EC7998C30295AA1E64E41BE`.
A ISO segue somente leitura e mantém SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
O flag opt-in `--candidate-home-character` integra o mesmo mesh/textura
candidate no slot home rígido da partida e deixa o jogador away no caminho
procedural authored:

```text
dotnet run --project fifa_pc -- render-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\candidate-home-character-preview.ppm" --clean-room-characters --candidate-home-character --authored-hud --candidate-material
```

Essa execução produziu checksum `AB8D372F87FAA6B5`; o headless de 80 frames
produziu `DB0F059C0AC82133`. O modo é uma colocação rígida authored do candidato
para integração de cena, sem atribuir rig, skinning, morph, animação, UV ou
semântica de GPU original.
O modo paralelo `--candidate-away-character` coloca o mesmo candidato
rigidamente no slot away; com ambos os flags, os dois slots recebem a mesma
malha candidate. Isso amplia somente a apresentação authored e continua sem
binding de rig, skinning, morph, animação, UV ou GPU original:

```text
dotnet run --project fifa_pc -- render-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\candidate-away-character-preview.ppm" --clean-room-characters --candidate-away-character --authored-hud --candidate-material
```

O preview away produziu `59D25C3C88B9CF69`; o preview com ambos os slots
produziu `0F83CEFDD78C58BA`, e o headless away de 80 frames produziu
`72CEA666935DBFCB`.
O perfil visual authored também cobre `STEAL` com uma pose procedural de tackle
no jogador away. Ele é aplicado somente pelo `CleanRoomPlayerAnimator`, sem
ler AXT/morph nem criar rig, skinning ou animação original; no cenário com
oponente, material, áudio e slot away o checksum foi `30D789D3BCBB915A`; com
slot home candidato, a variante simétrica produziu `BA2CD81182DF970E`.
No cenário combinado de 140 frames, com oponente authored, `--play-audio`,
material candidate e candidate-home, o runtime produziu checksum
`2641FCCE8245A44B`, preparou/tocou `8/8` cues candidate e deixou `0` eventos
explicitamente sem rota. Com ambos os slots candidatos no headless de 80 frames,
o checksum verificado foi `641ED53BD0DAE660`.

O trace authored `gameplay-audio-combined-trace.tsv` registra os mesmos 8
eventos de rota, incluindo `KICKOFF -> intro`; a repetição com
`--audio-trace-expected` retornou `matched=True`, com `8/8` preparados e
tocados e `0` eventos sem rota. Esse trace verifica a entrega determinística
da política nova do PC; não é seleção de cue nem binding EVT/ABKC original.

Para materializar e reexecutar esse trace authored:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --demo --opponent-ai --play-audio --clean-room-characters --candidate-home-character --candidate-away-character --candidate-material --audio-trace-output "D:\projeto zeta\fifa_pc\gameplay-full-stack-audio-trace.tsv" --integration-report "D:\projeto zeta\fifa_pc\gameplay-full-stack-integration.tsv"
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --demo --opponent-ai --play-audio --clean-room-characters --candidate-home-character --candidate-away-character --candidate-material --audio-trace-expected "D:\projeto zeta\fifa_pc\gameplay-full-stack-audio-trace.tsv" --audio-trace-output "D:\projeto zeta\fifa_pc\gameplay-full-stack-audio-replayed.tsv" --integration-report "D:\projeto zeta\fifa_pc\gameplay-full-stack-replayed-integration.tsv"
```

O esperado é validado antes da abertura da ISO e comparado linha a linha; os
nomes/cues permanecem a política authored do PC.
Para incluir a pose procedural authored no mesmo smoke, acrescente
`--animation-trace-output` na geração e
`--animation-trace-expected`/`--animation-trace-output` na repetição. O
resultado esperado é de 280 samples em 140 frames; esse trace não interpreta
animação original.
Quando `--clean-room-characters` está ativo, a ação `GAMEBREAKER` também usa
um perfil procedural de celebração authored. Ele é parte da apresentação PC
nova e não interpreta AXT, morph, rigging, skinning ou timing da ISO.

O modo opt-in `--juggle-demo` acrescenta quatro ações `JUGGLE` authored, com
altura de bola e cadeia determinísticas, antes de um chute. A fixture de 56
frames `gameplay-clean-room-juggle-input.tsv` produz 7 eventos (`KICKOFF`,
quatro `JUGGLE`, `SHOT` e `GOAL_HOME`); o trace e o replay esperado coincidem
com `matched=True`. A coluna `juggle` e a tecla `J` são controles PC authored,
sem afirmar input, física, frame, animação, áudio ou binding GPU original.

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-input.tsv" --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-trace.tsv"
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-input.tsv" --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-clean-room-gamebreaker-replayed.tsv"
```

Para reexecutar uma fixture authored e exigir igualdade textual do trace,
forneça `--match-trace-expected` junto de `--input-script`. O arquivo esperado
é lido antes da ISO, exige o cabeçalho do trace e não pode ser a ISO nem o mesmo
destino de `--match-trace-output`:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-demo-input.tsv" --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-script-demo-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-script-demo-replayed-stage.tsv"
```

Essa verificação passou com `matched=True`, 6 eventos e checksum
`C0249FD97A01C3F4`; o trace reexportado coincidiu byte a byte com o esperado,
SHA-256 `2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`.
Isso é um gate de determinismo e integração da simulação clean-room authored,
não replay, input, evento ou semântica decodificada do jogo original.

A simulação também possui uma regra authored opt-in para encerrar por placar:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-demo-input.tsv" --clean-room-characters --match-rule first-to-goals --target-goals 1
```

Esse cenário terminou em `HOME 1 - 0 AWAY`, `FULL_TIME` no frame 70 e sem
consumir o relógio. A opção foi orientada por referências comportamentais
externas registradas em
`docs/FIFA_EXTERNAL_BEHAVIOR_EVIDENCE.md`; ela é authored e não prova um modo,
limite ou timing específico da ISO. O modo padrão continua `TIMED` e preserva
o checksum `C0249FD97A01C3F4`.

O modo padrão mantém o adversário estático para que a sequência seja
reproduzível. Para exercitar a pressão e a finalização do adversário na
simulação clean-room, use explicitamente:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --demo --opponent-ai
```

Essa execução produziu `HOME 0 - 2 AWAY`, com `SHOT_AWAY=2` e `GOAL_AWAY=2`.
O modo não atribui essa IA às regras ou eventos do jogo original.

Para cobrir deterministicamente a disputa de posse authored, use o fixture
`--steal-demo` junto com `--opponent-ai`:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --steal-demo --opponent-ai --clean-room-characters --candidate-home-character --candidate-material --play-audio
```

Essa execução produziu `KICKOFF=1,STEAL=1`, posse `Away`, áudio `1/1`
preparado/tocado, zero eventos sem rota e checksum `BFA4E813A3D0EA19`.
`STEAL` e a pressão do oponente são regras novas do runtime clean-room.

Para exercitar um ciclo de rodada completo após o gol, use o modo opt-in
`--round-demo`:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 120 --round-demo
```

Esse modo é uma política nova do runtime: após `GOAL_HOME` aguarda 0,5 s,
reposiciona os jogadores, inicia a rodada seguinte com o autor do gol em posse
e registra `RESTART`. O relatório expõe `match_round`,
`match_restart_pending` e `clean_room_round_reset`. Não é uma reconstrução da
regra original de kickoff; o caminho padrão permanece sem essa opção e mantém
seus checksums de referência.

O ciclo também pode ser verificado como trace authored reexecutável:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --round-demo --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-round-demo-trace.tsv"
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --round-demo --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-round-demo-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-round-demo-replayed.tsv"
```

A execução produziu 7 eventos, `match_round=2`, `match_last_action=RESTART`,
checksum `404D21B5A0571826`, `matched=True` e SHA-256 igual
`B3BDFEEC8FD2EB6479E7B797C1031AE9EE348451C4BFE4C4E172B993E8872EBB` nos
dois TSVs. Isso é um gate de determinismo do runtime novo, não uma regra
decodificada da ISO.

Um input authored explícito também pode habilitar o reset de rodada sem usar a
fonte demo, combinando `--input-script` com `--round-demo`:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-round-input.tsv" --round-demo --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-script-round-trace.tsv"
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-round-input.tsv" --round-demo --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-script-round-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-script-round-replayed.tsv"
```

Essa combinação mantém o input vindo exclusivamente do TSV e usa
`--round-demo` apenas para a política authored de reinício. A execução validada
produziu 7 eventos, rodada 2, `RESTART`, `matched=True` e checksum
`404D21B5A0571826`; os traces tiveram SHA-256
`B3BDFEEC8FD2EB6479E7B797C1031AE9EE348451C4BFE4C4E172B993E8872EBB`.
`--demo` e `--aimed-demo` continuam mutuamente exclusivos com
`--input-script`.

No host Win32 interativo, a mesma política pode ser habilitada sem o script
determinístico por `--round-reset`:

```text
dotnet run --project fifa_pc -- run-windowed "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --clean-room-characters --round-reset
```

O intervalo, reposicionamento e `RESTART` continuam sendo comportamento
authored clean-room; a opção não é uma reconstrução da regra original.

Para exercitar o encerramento clean-room com um relógio curto, use
`--match-seconds`:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 30 --demo --match-seconds 0.25 --clean-room-characters
```

O valor precisa ser positivo. A execução validada expôs
`match_duration_seconds: 0,250`, `match_ended: True` e
`match_last_action: FULL_TIME`; `match_result` passa a ser `DRAW`,
`HOME_WIN` ou `AWAY_WIN` conforme o placar. Depois desse evento o frame não
avança mais.
Essa duração, a transição e o congelamento são authored no runtime clean-room,
não uma medição ou regra recuperada do jogo original. O evento `FULL_TIME` usa
`outro` somente na política de cue authored, sem binding de áudio original.

A camada atual monta o filesystem XDVDFS somente leitura, cataloga arquivos de
`data`, valida índices BIG4/BIGF conhecidos, streams `.ast` `SCHl` e texturas
`XPR0` com decode real, e executa um loop de runtime gerenciado. O núcleo de
partida atualiza movimento, posse, passe, drible, bola, placar e relógio; o
mixer monta/contabiliza áudio validado e pré-decodifica os seis streams `.ast`
quando recebe o filesystem read-only. A política authored atual prepara rows PA
nomeadas para `KICKOFF/RESTART -> intro`,
`PASS/PASS_COMPLETE/JUGGLE -> attackgen`, `TRICK/PANNA -> panna`,
`SHOT/SHOT_AWAY -> attackgoal`, `GOAL_HOME/GOAL_AWAY -> goal`,
`GAMEBREAKER -> gb_goal`, `STEAL -> tackle` e `FULL_TIME -> outro`; o host
Win32 pode reproduzi-las assíncronamente. Essas rotas são escolhas authored do
runtime e não associações recuperadas do conteúdo original. Ações desconhecidas
continuam sem seleção automática; ações repetidas são distinguidas por sequência.
Com um opener read-only, o mesmo `CatalogAudioMixer` também monta os candidatos
bounded de ABKC durante `Initialize`: na ISO são 16 bancos, 15 candidatos
decodificados, 1 rejeitado (`data/audio/CONTROL.abk`) e fontes AST=6, BNK=9 e
none=1. Esse preparo é integração de payload observada, não seleção de cue,
binding de evento ou semântica original.
Fora dos estados de passe/chute em voo, a bola acompanha o jogador que detém a
posse; essa é uma invariável nova e testada da simulação clean-room, não um
binding ou significado recuperado do jogo original.
O chute do jogador Home aceita `MoveY` no instante de disparo: valores positivos
e negativos adicionam uma componente vertical determinística à trajetória. Isso é
um comportamento de controle novo da reimplementação clean-room; não é uma
semântica decodificada do XBE/ISO. O demo padrão mantém `MoveY=0` no chute e,
portanto, conserva sua trajetória reta e seu checksum de referência.
O host windowed usa a mesma rota de ações antes da atualização do mixer.
Sem `--play-audio`, o headless mantém a saída de áudio desativada para que os
testes sejam determinísticos e silenciosos; com a opção, o relatório inclui
`audio_output: winmm-async` e `gameplay_audio_played` quando eventos roteados ocorrem.
O relatório também expõe `gameplay_audio_unmapped_events` e
`last_gameplay_audio_route`. No cenário combinado de 140 frames com
`--opponent-ai --play-audio`, 8 de 8 eventos foram preparados/reproduzidos e
0 ficaram sem rota. Isso não atribui semântica aos eventos do jogo original.
O cenário authored curto (`--match-seconds 0.25`) também emitiu
`FULL_TIME` uma vez, preparou/reproduziu `outro` e terminou com
`gameplay_audio_unmapped_events=0`; checksum de renderização
`F39849417B16C774`.
O comando `gameplay-audio-coverage-audit <workspace> [--report <saida.tsv>]`
agrega os traces/replays authored de full-stack, round, gamebreaker, juggle,
panna, FULL_TIME, STEAL e aimed. A execução atual verificou 8/8 cenários,
42/42 eventos preparados e tocados, 0 sem rota, as 14 ações da política e 8
cues únicos. O cenário aimed cobre o trace com `SHOT` e `GOAL_HOME`; o par de
traces de áudio tem SHA-256
`7FA076ADE12B81EC41213F4C34113D35F6F0D1A8FCC14F540C66776BB0CE8F32`. O
relatório `gameplay-audio-coverage.tsv` continua explicitamente
`verified-authored` e `original_semantics=not-claimed`; isso não é seleção de
áudio original.

Para gerar uma prévia visual da malha EAGL candidata sobre a cena gerenciada:

```text
dotnet run --project fifa_pc -- eagl-candidate-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\candidate-integrated-preview.ppm"
```

Para visualizar, apenas como diagnóstico, a média dos dois componentes do
atributo genérico preservado no slot 1:

```text
dotnet run --project fifa_pc -- eagl-candidate-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\candidate-slot1-profile-preview.ppm" --slot1-profile
```

O resultado usa preenchimento sólido com cor provisória e contorno; é
diagnóstico. Binding completo do draw, materiais, skinning, rigging e animação
ainda não são considerados validados.

Para visualizar atores proceduralmente authored a partir do estado da partida:

```text
dotnet run --project fifa_pc -- render-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\clean-room-characters-preview.ppm" --clean-room-characters
```

`--clean-room-characters` desenha corpo, cabeça, braços e pernas de forma
determinística, agora com perfis authored distintos para posse, passe, drible,
chute, gol, reinício e `FULL_TIME`, e mantém mesh/texture candidates separados.
É uma camada visual nova, não um decoder de AXT, morph, rigging ou skinning;
sem a opção, o preview conserva os shapes legados. Em 80 frames com `--demo`,
o caminho authored produziu checksum `C0249FD97A01C3F4`; combinado ao material
candidato, `29EACF0AE7B12B58`. Esses checksums verificam somente a apresentação
PC nova.

O mesmo `render-preview` aceita o caminho candidato de forma explícita:

```text
dotnet run --project fifa_pc -- render-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\render-preview-candidate-stage.ppm" --clean-room-characters --candidate-material tops0030.big tops
```

Essa execução validada produziu `candidate_material_status=validated-candidate`,
mesh de 2 grupos/341 vértices/337 triângulos e checksum
`C1DB074C7140C585`. O caminho continua diagnóstico: o estado textual do objeto
é exato, o estado dos grupos é `unproven`, e não há binding promovido para UV,
material GPU, skinning, rigging ou comandos originais. O comando também recusa
usar a ISO como arquivo de saída.

O runtime headless pode preservar o último framebuffer para QA visual:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --clean-room-characters --frame-output "D:\projeto zeta\fifa_pc\runtime-characters-demo.ppm"
```

`--frame-output` grava somente a saída PPM do renderer novo e rejeita
explicitamente o caminho da ISO como destino.

O leitor EAGL também conserva os comandos de renderização encontrados nos
objetos (ID, tamanho, bytes e relocations) como evidência neutra. No objeto
`star0001-head` os 5 fluxos terminam corretamente; isso ainda não atribui
semântica de textura/material nem transforma a malha candidata em asset final.
`eagl-candidate-smoke --commands` imprime essa evidência por primitiva para
comparação entre objetos.
Para limitar a mesma evidência a um BIG/ORD específico:

```text
dotnet run --project fifa_pc -- eagl-candidate-object-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops0030.ord --commands
dotnet run --project fifa_pc -- eagl-candidate-attribute-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-candidate-attribute-audit.tsv"
```

Essa opção preserva comandos crus, relocations e atribuições textuais,
inclusive quando `SHAPENAME` está em primitivas diferentes das que passam o
gate geométrico; nenhum ID recebe semântica de draw.
O comando `eagl-candidate-state-correspondence <iso> [--report <saida.tsv>]`
compara, nos 24 objetos `tops` verificados, as primitivas geométricas com as
primitivas que carregam `SHAPENAME`. Foram observados 269 pares, sem igualdade
exata de bytes ou forma de fronteira, com prefixo máximo de 8 bytes e todos os
fluxos completos. A etapa seguinte também comparou símbolos de relocation
presentes nos comandos: 0/269 pares compartilharam símbolo. Isso é evidência
estrutural/negativa, não associação de
estado a mesh, material, UV, skinning, rigging, animação ou GPU.
O controle complementar `eagl-state-geometry-adjacency-audit <iso>
[--report <saida.tsv>]` registra, por objeto, os índices e camadas observados
nas duas populações. A execução atual produziu 24/24 linhas, 0 pares com o
mesmo índice, 48 pares apenas adjacentes, 269 pares na mesma camada e 0/24
grupos geométricos com `SHAPENAME` exato. Esses números são um controle de
heurística de adjacência e não autorizam associação de estado, material, UV,
skinning, rigging, animação ou GPU; o artefato é
`fifa_pc/eagl-state-geometry-adjacency.tsv`.
O controle complementar `eagl-state-geometry-profile-audit <iso>
[--report <saida.tsv>]` compara somente a sequência opaca de IDs/tamanhos,
contagem e bytes dos comandos por par geometria--estado. Nos 24 objetos foram
produzidas 269 linhas, com 0 pares de sequência exata, 0 pares no mesmo índice,
48 vizinhos de índice, 1 perfil de comando geométrico distinto e 3 perfis de
estado. Houve 4 linhas com perfil de estado não repetido, mas isso não cria
uma associação porque não há igualdade de sequência; o artefato é
`fifa_pc/eagl-state-geometry-profile-audit.tsv` e continua sendo controle
opaco, sem semântica de material, UV ou GPU.
O controle complementar `eagl-state-geometry-order-audit <iso>
[--report <saida.tsv>]` preserva a ordem dos intervalos entre candidatos
geométricos e primitivas com `SHAPENAME`, sem tratar nenhum intervalo como
estado aplicado a uma malha. Na ISO foram observados 38 grupos geométricos,
180 primitivas com `SHAPENAME`, 62 intervalos e 36 intervalos não vazios; as
180 primitivas foram particionadas sem sobra, os 36 streams não vazios estavam
completos e houve 17 perfis opacos de run. Esse recorte é uma medição de ordem
e completude, não evidência independente de material, UV, skinning, rigging,
animação ou GPU; o artefato é
`fifa_pc/eagl-state-geometry-order-audit.tsv`.
O comando `eagl-candidate-attribute-audit <iso> [--report <saida.tsv>]` mede
separadamente os perfis numéricos completos do slot-1 e as populações de
estado. Na ISO, 24/24 objetos e 38/38 grupos têm valores slot-1 finitos e
próximos da faixa unitária, com 7.403 valores; 0/38 grupos carregam
`SHAPENAME` ou clamp diretamente, contra 180 primitivas com esses estados.
Isso é evidência de dados e limites, não prova de UV ou material.
Os símbolos de estado são adicionalmente expostos como atribuições estruturais
`nome=valor` — por exemplo, transparência, alpha-test, `SHAPENAME` e clamp —
sem converter esses tokens em material, skinning ou estado final do renderer.

Para inspecionar um objeto morph standalone dentro de um BIG aninhado:

```text
dotnet run --project fifa_pc -- eagl-object-symbol-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head_morph.o
```

O comando lista apenas evidência ELF, strings nomeadas e relocations; não
interpreta o payload como blend shape, ossos ou poses. A opção `--payload`
adiciona janelas hexadecimais limitadas ao redor dos campos relocados; elas
servem para comparar layouts entre objetos, sem nomear os bytes como records.
Para inspecionar o companion `.orl` sem tentar tratá-lo como ELF, use `--raw`:

```text
dotnet run --project fifa_pc -- eagl-object-symbol-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head.orl --raw
```

Na amostra, o arquivo tem 6.032 bytes e começa com nomes de seções; o modo
reporta apenas bytes e o fato de o ELF reader não aceitá-lo. Não há parser de
link `.orl` nem semântica de morph derivada desse prefixo.
No inventário atual, 594 morphs produziram 4.719 campos relocados em 42
assinaturas de offsets. O leitor separa ponteiros cujo destino textual é
explicitamente `BS_*`, `Coordinates` ou `Normals` — 2.372 links de shape e
1.142 de atributo —; isso ainda não é um decoder de deformação.

Para comparar o padrão de nomes com os irmãos geométricos no mesmo BIG
aninhado, sem criar um binding de animação:

```text
dotnet run --project fifa_pc -- eagl-morph-sibling-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" star0001.big --verbose
dotnet run --project fifa_pc -- eagl-morph-sibling-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --geometry-report "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit.tsv" --geometry-summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit-summary.tsv" --geometry-correlation-report "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-correlation.tsv" --geometry-correlation-summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-correlation-summary.tsv"
dotnet run --project fifa_pc -- morph-ambiguity-summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-summary.tsv"
dotnet run --project fifa_pc -- morph-ambiguity-byte-summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-byte-comparison-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-byte-summary.tsv"
dotnet run --project fifa_pc -- morph-ambiguity-numeric-summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-numeric-summary.tsv"
dotnet run --project fifa_pc -- morph-ambiguity-group-metadata "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-byte-comparison-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-group-metadata.tsv"
dotnet run --project fifa_pc -- morph-ambiguity-independent-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-independent-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-independent-audit-summary.tsv"
dotnet run --project fifa_pc -- morph-candidate-geometry-ambiguity-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-ambiguity-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-ambiguity-audit-summary.tsv"
dotnet run --project fifa_pc -- eagl-morph-name-state-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\eagl-morph-name-state-audit.tsv"
dotnet run --project fifa_pc -- audio-event-named-decode-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\audio-named-event-decode.tsv"
dotnet run --project fifa_pc -- audio-abk-batch-decode-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\audio-abk-batch-decode.tsv" --max-attempts 8
dotnet run --project fifa_pc -- eagl-morph-sibling-runtime-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

O smoke `star0001.big` encontrou 1/1 candidato
`star0001-head_morph.o` → `star0001-head.ord/.orl`, com ELF, 32 símbolos
geométricos e bounds. O inventário real percorreu 391/391 BIGs, encontrou 594
morphs e 594/594 irmãos `.ord/.orl` válidos; 593 têm bounds decodificados e
42 assinaturas de relocação são repetidas. Essa correlação é apenas evidência
de nomenclatura, ELF e bounds no mesmo pacote; pesos, ossos, poses, frames e
animação continuam sem decoder.
O smoke de runtime repete o caminho do `GameAssetCatalog.Scan` e confirma
391/391 BIGs internos, 594 evidências, 594 pares completos, 594 ELF/geometria,
593 bounds e 593 símbolos `__PosePaletteBank:::`. É um resumo estrutural e não
aplica morph, pose ou skinning.
O mesmo catálogo retém os registros ELF crus dos 19 símbolos
`__AnimationBank:::`: o smoke observou 19 registros e 19 assinaturas de
metadados distintas, todas com seção 1, tamanho 4 e `Info=17`. A comparação
adicional do índice morph não encontrou coincidência com o registro
`__PosePaletteBank:::` (`table_section=4`, `symbol_index=3`, `symbol_section=1`,
`value=0`, `size=4`, `Info=17`). Essa não-coincidência é controle estrutural,
não prova de ausência de vínculo nem semântica de pose/animação.
Para comparar o nome e os metadados ELF crus desses símbolos com os bancos de
animação já validados, use:

```text
dotnet run --project fifa_pc -- eagl-morph-pose-bank-index "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\morph-pose-bank-index.tsv"
```

O índice atual contém 391 linhas e 1 símbolo distinto (`PosePaletteBank`), com
1 registro ELF distinto (`table_section=4`, `symbol_index=3`,
`symbol_section=1`, `value=0`, `size=4`, `Info=17`) e 0 igualdades textuais aos
nomes dos 19 bancos `.axt`, além de 0 coincidências de metadados ELF com os
símbolos dos bancos. O TSV conserva esses metadados brutos. Isso é
evidência negativa de recorrência nominal e metadados ELF observados nesta
camada; não prova ausência de vínculo nem atribui semântica de pose, rig, peso,
frame, timing ou skinning.
Ao normalizar os offsets pelo primeiro campo, o perfil mais frequente reúne
173 morphs com os mesmos gaps `0x0,0x4,0x8,0xC,0x24,0x28,0x34` e um campo
posterior variável; isso é repetição de layout relativo, não uma identificação
de record ou atributo.
Uma impressão SHA-256 do payload normalizado, mascarando somente strings
nomeadas e words de relocation, foi distinta nos 594/594 morphs. Nesta regra
conservadora não há template byte-a-byte compartilhado suficiente para
promover um decoder de records.
Uma comparação local adicional usa janelas de raio 16 em torno dos campos de
relocation, mascarando somente strings nomeadas e words de relocation antes do
hash. Os 594 morphs produziram 326 assinaturas locais, 404 pares
morph-layout/janela e 11 layouts morph com múltiplas assinaturas locais. Isso
mede variação de bytes dentro de layouts repetidos, sem identificar records,
pesos, ossos, poses ou frames.
Uma assinatura estrutural separada compara somente tamanho/posição da seção,
strings observadas e campos/alvos de relocation; ela encontrou 393 perfis
distintos em 594 objetos, com o maior perfil repetido em 42. Isso documenta
repetição de layout bruto sem transformar os perfis em records, pesos, ossos,
poses ou frames.
A correlação adicional com o ELF companion registra apenas contagens/totais de
seções, símbolos, relocations, classes de símbolos geométricos e bounds. Foram
417 assinaturas de companion e 443 pares morph-layout/companion; 32 dos 393
perfis de morph aparecem com mais de uma assinatura de companion. Logo, o
layout do morph não seleciona sozinho uma geometria nem autoriza um binding.
Uma camada refinada calcula um hash de headers de seção, posições/tamanhos dos
símbolos geométricos e offsets relativos/tipos de relocation: 444 assinaturas,
447 pares morph-layout/layout e 32 layouts morph com múltiplos layouts companion.
O hash continua estrutural e não representa um binding.
Os streams de comandos também têm uma assinatura opaca de primitive/layer,
offsets, assignments textuais, IDs/tamanhos, hashes dos bytes crus e relocation:
592 assinaturas, 593 pares morph-layout/stream e 38 layouts morph com múltiplos
streams. Nenhum ID foi interpretado como comando GPU.
O inventário também calcula uma assinatura estrutural pareada com as camadas
independentes já validadas do morph e do irmão: layout, janelas normalizadas,
layout ELF, stream opaco, registros `__VB`, perfil numérico, topologia candidata
e coincidências numéricas cross-file.
Foram observadas 593 assinaturas pareadas; 355 dos 393 layouts morph têm uma
composição única e 38 têm mais de uma. Isso organiza amostras repetíveis e não
é binding de morph, rig, pose, peso, material, UV, skinning, animação ou GPU.
Também há uma assinatura de coincidência numérica cross-file: words
little-endian alinhadas do payload morph são comparadas apenas com metadata
numérica validada do companion, excluindo strings nomeadas e words de relocation.
O inventário produziu 406 assinaturas e 12 layouts morph com múltiplas formas de
coincidência. As categorias são campos comparados, não semântica de peso,
índice, pose, frame ou outro asset.
Para selecionar amostras sem perder o vínculo nominal, o inventário aceita
`--catalog <saida.tsv>` e grava uma linha por morph fora da ISO. O catálogo
verificado tem 594 linhas, com 355 layouts estáveis e 239 linhas distribuídas
nos 38 layouts ambíguos; as assinaturas continuam opacas e não são bindings.
O TSV inclui a coluna `candidate_geometry_signature` como evidência de QA da
representação candidata; ela não atribui topologia ou semântica ao asset.
`--ambiguity-report <saida.tsv>` gera a matriz dos 393 layouts. Nos 38
ambíguos, a variação observada aparece em stream de comandos (38), layout
companion (32), registros `__VB` (27), coincidência numérica (12), janelas de
payload (11), mesh candidato (6) e perfil numérico (5). Isso não prova
causalidade nem semântica.
O relatório também compara cada camada ambígua com os 355 layouts estáveis por
igualdade exata: houve interseção em `numeric_descriptor` (38 layouts),
`candidate_mesh` (32), `payload_window` (26), `vertex_record` (25) e
`companion_layout` (2), sem interseção em `command_stream` ou
`companion_numeric_match`. É recorrência de assinatura, não binding.
O comando `morph-ambiguity-summary <ambiguity-report.tsv> <summary.tsv>` separa
os 38 layouts ambíguos por composição exata das camadas que variam. A execução
real produziu 8 composições e 239 linhas; o resultado mantém contagens de
interseção com controles estáveis e SHA-256 opacos das chaves de layout. É uma
agregação de evidência estrutural, não um decoder de morph, pose, rig, peso,
frame, timing ou skinning.
O comando `morph-ambiguity-byte-summary <ambiguity-report.tsv>
<byte-report.tsv> <summary.tsv>` cruza essas composições com o relatório
byte-level pela chave estrutural exata: 239/239 linhas foram associadas,
213/239 repetiram bytes e limites de comando, 0/239 tiveram assinatura
estrutural de comando exata, 0/239 coincidiram em registro/assinatura/limite
numérico e 222/239 coincidiram somente na categoria valor/token observada.
Isso continua sendo triagem de bytes e limites, sem semântica de GPU, morph ou
animação.
O comando `morph-ambiguity-numeric-summary <ambiguity-report.tsv>
<numeric-gap-report.tsv> <summary.tsv>` cruza o relatório de gaps pela mesma
chave: os 17 gaps ocupam 10 layouts ambíguos em 4 composições; 28 dos 38
layouts ambíguos não aparecem no relatório de gaps. As métricas preservam
7/17 igualdades de fronteira relativa, 5/17 de janela de payload e 0/17 de
boundary numérico ou offset canônico absoluto, sem interpretar tokens ou
offsets.
O comando `morph-ambiguity-group-metadata <ambiguity-report.tsv>
<byte-report.tsv> <numeric-gap-report.tsv> <metadata.tsv>` materializa uma
linha por layout ambíguo e junta, pela chave estrutural exata, as contagens de
perfis, igualdade de bytes/fronteiras e presença dos gaps. A execução real
produziu 38 linhas, associou 239 linhas byte e 17 linhas de gap; 28 layouts não
têm gap. Esses campos são metadata observada e continuam sem semântica de
morph, pose, rig, peso, frame, timing, skinning, material, UV ou GPU.
O comando `morph-ambiguity-independent-audit <structural-catalog.tsv>
<ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]` reagrupa o
catálogo bruto por chave e confere independentemente os oito contadores de
perfil publicados. Na execução real foram auditadas 594 linhas em 393 layouts,
com 0 inconsistências de metadata/contagem e 0 divergências entre os rótulos
`ambiguous/stable` e a variabilidade medida nas oito camadas. Entre os 38 ambíguos, 26 têm mesh
candidata estável com companion variável, 6 têm mesh candidata variável e
38/38 têm alguma camada estrutural variável. Os hashes de conjuntos e amostras
servem para rastreabilidade; não são bindings nem semântica de morph, rig,
pose, peso, animação, material, UV, skinning ou GPU.
O comando `morph-candidate-geometry-ambiguity-audit` faz um controle separado
da nova coluna geométrica: na ISO, 38/38 layouts com geometria variável também
são ambíguos, 0 são estáveis, 0 layouts ambíguos ficaram sem variação geométrica
e houve 0 divergências de rótulo. O resultado continua sendo QA de candidato,
não um decoder.
O comando `audio-event-named-decode-smoke <iso> [--report <saida.tsv>]` tenta
decodificar os ranges de áudio associados aos 17 nomes extraídos do EVT. Na
ISO, 7 candidatos têm canais no header observado e 10 usam uma hipótese mono
única validada pelo layout/frames `SCDl`; os 17 decodificaram para WAV,
totalizando 2.915.700 bytes, sem criar binding automático de evento, ABKC ou
gameplay. Na montagem normal, `CatalogAudioMixer.Initialize` pré-prepara os 17
payloads nomeados e o headless reporta 2.914.952 bytes PCM. O campo observado
`channels=0` não é sobrescrito: a hipótese permanece metadata de candidato e a
rota `panna` para `TRICK` é uma escolha authored explícita, não um binding
original.
O comando `audio-abk-batch-decode-smoke <iso> [--report <saida.tsv>]
[--max-attempts <n>]` tenta um conjunto bounded de índices já observados em
cada banco ABKC. Na ISO, 15/16 bancos decodificaram pelo menos um candidato,
com 10.051.852 bytes WAV e fontes AST=6, BNK=9 e none=1; o único rejeitado foi
`data/audio/CONTROL.abk`, sem candidato de índice disponível. Isso valida
payload AST/BNK e o caminho de decoder, sem selecionar eventos, gameplay,
materiais, UVs, skinning, rigging, animação ou comandos GPU.
Com `--byte-report <saida.tsv>`, o inventário compara os bytes crus dos streams
de comando e os registros canônicos UTF-8 da coincidência numérica contra uma
linha de controle de cada layout estável. O relatório verificado tem 239 linhas:
213/239 têm bytes de comando exatamente iguais a algum controle, mas 0/239 têm
assinatura estrutural de comando exata; 26/239 não têm igualdade byte-level e
0/239 têm igualdade exata no registro numérico. Isso é
triagem alinhada de bytes, não semântica GPU ou binding.
O mesmo relatório inclui `command_stream_boundary_signature`, que separa as
primitivas pelos limites observados e conserva somente ordem, contagem,
tamanho, completude e hash dos bytes. Foram observadas 252 assinaturas, 402
pares layout/partição e 9 layouts com variação; 213/239 linhas ambíguas
coincidiram também nessa partição com um controle. Isso não interpreta IDs,
payloads ou operações GPU.
O comando `eagl-morph-name-state-audit <morph-structural-catalog.tsv>
<saida.tsv>` cruza somente os nomes `BS_*` preservados nos morphs com os
valores textuais `SHAPENAME` dos companions. Na ISO, foram 594 linhas, 2.360
nomes morph distintos, 391/594 rows com estado e 0/594 coincidências cruas ou
normalizadas. Isso é controle lexical negativo e não cria binding de morph,
grupo, material, UV, skinning, rigging, pose, frame ou animação.

O controle negativo `eagl-morph-animation-name-negative-control` cruza, sem
normalização, os mesmos 2.360 nomes completos de shape com os 1.578 nomes de
arquivo e 19 nomes de banco do índice de animação. Na ISO foram observadas
594/594 linhas morph completas e 0 coincidências exatas em ambos os conjuntos.
Isso rejeita apenas uma associação textual direta; não prova ausência de
relação semântica e não autoriza binding de animação, pose, rig ou timing.
O argumento `--numeric-gap-report <saida.tsv>` gera apenas as 17 linhas sem
coincidência valor/categoria, em 10 layouts, com 8 conjuntos de nearest
controls textuais. O arquivo
`morph-sibling-numeric-gap-report.tsv` não atribui semântica aos tokens.
Para os registros numéricos, o relatório mantém uma coluna de sequência de
offsets e outra de tokens valor/categoria sem offset. Elas produziram,
respectivamente, 394 e 283 assinaturas; os tokens coincidiram em 222/239
linhas ambíguas, mas o registro canônico completo continuou em 0/239. São
normalizações de comparação, não records ou bindings decodificados.
O comparador `TokenSequenceComparer` também mede igualdade posicional, delta de
comprimento e prefixo comum dentro dos 17 gaps: todos tiveram delta absoluto de
quantidade igual a zero e prefixo comum máximo de 4 tokens; 15 tiveram 8
diferenças mínimas e 2 tiveram 16. Na melhor comparação de cada linha, a
porção textual antes do delimitador concentrou todas as diferenças e a porção
posterior teve zero. O TSV também registra os índices ordinais zero-based das
posições divergentes, que formaram quatro padrões com frequências 7, 6, 2 e 2;
eles são posições textuais, não índices de geometria ou animação.
`NumericTokenSequenceComparer` acrescenta a comparação alinhada dos
`CanonicalMatches`: offset, word observado, sufixo textual e os quatro bytes
little-endian do word. Nos 17 gaps, houve 8 ou 16 words divergentes, zero
diferenças de sufixo, 8 ou 16 bytes divergentes e 21/24/26/42/58 offsets
divergentes conforme o tamanho. É evidência de payload observado, não binding.
O campo `nearest_control_canonical_offset_delta_signature` preserva os deltas
assinados entre offsets alinhados; os 17 gaps produziram 10 assinaturas
distintas. Essa sequência descreve somente limites observados.
O relatório também inclui `relative_offset_boundary_signature`, que remove a
base absoluta e mantém apenas os deltas entre offsets adjacentes. Foram 8
perfis relativos; 7/17 gaps coincidem com algum controle estável e 10/17 não
coincidem. É uma comparação de fronteiras observadas, não uma identificação de
records, vértices, ossos ou frames.
  As oito janelas de payload normalizadas também são comparadas ao controle mais
  próximo: nenhum dos 17 gaps coincide em todas; há 2–8 diferenças, 0–6 hashes
  iguais e prefixo comum de 0–2. Isso não identifica records ou assets.
  O argumento `--numeric-gap-correlation-report <saida.tsv>` agrupa os mesmos
  gaps pelas 10 assinaturas de delta de offset e cruza a recorrência dos perfis
  de janela com os limites brutos do stream. O relatório real contém 12 perfis
  globais de janela e 10 de limite; 3 grupos repetem uma janela, 6 repetem um
  limite e somente 2/17 linhas coincidem com algum limite de controle estável.
  A correlação ampliada contra todos os 355 controles estáveis registra também
  7/17 coincidências de fronteira relativa, 5/17 sequências completas de oito
  janelas normalizadas e 0/17 coincidências exatas de boundary numérico do
  companion ou de offsets canônicos absolutos. As camadas são mantidas
  separadas; esses hashes e contagens são triagem estrutural, não decoder nem
  binding.
O mesmo vínculo agora compara o perfil validado dos descritores `__VB`, apenas
por streams, bytes, elementos, strides, flags e seções: 235 assinaturas,
407 pares morph-layout/descritor e 13 perfis morph com múltiplas assinaturas de
descritor. Esses números continuam metadata estrutural; nenhum stream foi
nomeado como posição, normal, UV, peso ou deformação.
A ordem relativa dos registros também é preservada em uma assinatura hash de
seção, offsets, bytes, stride e flags: 358 assinaturas, 431 pares
morph-layout/sequência e 27 layouts morph com múltiplas sequências. Isso não
atribui significado a nenhum stream.
A leitura numérica neutra acrescenta somente finitude de palavras float
empacotadas e containment dos streams de stride 12 nas bounds declaradas:
foram 12 perfis numéricos, 398 pares morph-layout/número e 5 perfis morph com
múltiplas assinaturas numéricas. Isso não atribui semântica aos componentes.
A topologia candidata do mesmo companion também foi resumida somente por
grupos, vértices, triângulos e primitivas ignoradas: 593 companions produziram
assinatura, com 221 valores distintos (incluindo o caso sem assinatura), 399
pares morph-layout/topologia e 6 perfis morph com múltiplas topologias. Isso
continua uma métrica de candidato, não a topologia original nem deformação.

O inventário também preserva um símbolo estrutural observado no mesmo objeto:
593/594 morphs têm `__PosePaletteBank:::`. As 42 assinaturas de relocação
agrupam-se repetidamente com 4 nomes `BS_*`, 1 ou 2 atributos e esse símbolo;
isso é correlação de símbolos/layout, não prova de palette de poses, pesos,
ossos ou aplicação de animação.

Uma segunda amostra same-trunk também passou pelo ciclo completo do runtime:
`run-headless ... --candidate-material tops0040.big tops` validou textura PAL8
256x256, malha de 527 vértices/523 triângulos e 80 frames, com checksum
`3A2D00B09DC319F6`. Isso reforça somente a família `tops`; não cria bindings
para `head`, `face`, `hair` ou outros prefixos.

Abrir uma entrada individual de um BIG diretamente do ISO:

```text
dotnet run --project fifa_pc -- asset-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/anims.big banklist.axt
```

O comando usa uma janela de leitura limitada ao tamanho da entrada; não extrai
arquivos e não modifica a imagem original.

Gerar um inventário de formatos e assinaturas:

```text
dotnet run --project fifa_pc -- inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Esse inventário identifica contêineres de animação, pacotes de imagens SHPX,
texturas, áudio e vídeo.

Os pacotes `VivF` também têm sua tabela estrutural validada: tamanho declarado,
quantidade de registros, registro sentinela e offsets monotônicos. O parser não
descomprime nem interpreta o payload até o codec ser confirmado.

Abrir um bloco comprimido específico pelo índice:

```text
dotnet run --project fifa_pc -- vivf-block-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh 0
```

O bloco é exposto como stream limitado ao intervalo validado no `.viv` ou
`.big`, pronto para o futuro decodificador.

Testar a decodificação RefPack de um bloco:

```text
dotnet run --project fifa_pc -- vivf-decode-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh 0
```

O decoder aloca somente o tamanho expandido declarado pelo bloco e rejeita
comandos truncados, back-references inválidas e saídas com tamanho diferente.

Validar todos os blocos de um pacote:

```text
dotnet run --project fifa_pc -- vivf-validate-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh
```

Inspecionar o BIG interno produzido após a expansão de um bloco:

```text
dotnet run --project fifa_pc -- vivf-nested-big-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh 0
```

Inventariar a estrutura APT de um pacote inteiro, incluindo os companions do
mesmo BIG interno:

```text
dotnet run --project fifa_pc -- apt-structure-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh
```

Para materializar uma linha TSV por `.apt`, acrescente `--report <saida.tsv>`:

```text
dotnet run --project fifa_pc -- apt-structure-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh --report "D:\projeto zeta\fifa_pc\apt-structure-fe.tsv"
```

Na ISO atual, `fe.bh` tem 163/163 `.apt` e `.const` estruturalmente legíveis,
161/161 `.o` EAGL estruturalmente legíveis, 145 `.xsh` e 145 conjuntos com os
quatro companions. `ig.bh` tem 121/121 `.apt` e `.const`, 118/118 `.o` EAGL,
114 `.xsh` e 114 conjuntos completos.
Os campos APT variam entre famílias e alguns ficam fora do tamanho do asset;
por isso o inventário separa parsing do header de `fields_within_asset` e não
atribui timing, poses, morphs ou materiais.
O inventário também calcula uma assinatura somente estrutural de cada conjunto:
header APT validado, header `.const` validado quando presente, assinatura do
`.o` EAGL validado, extensões same-stem, tabela SHPX validada do `.xsh` e
tamanhos dos companions. Foram encontrados 133 perfis em `fe.bh` e 97 em
`ig.bh`; 161/161 e 118/118 objetos `.o` e 145/145 e 114/114 tabelas SHPX dos
conjuntos correspondentes foram lidos, sem rejeições no inventário.
Isso não decodifica timing,
poses, morphs, materiais ou frames.
O argumento --profile-report <saida.tsv> gera um perfil por combinação de
header APT, header .const e extensões dos companions. Na ISO atual, foram
observados 80 perfis em fe.bh e 51 em ig.bh, cobrindo 284 pacotes; as palavras
alinhadas comparadas somaram 568, com 0 igualdades e 568 diferenças. A
comparação é de words brutas dentro de padrões estruturais e não atribui
semântica a timing, pose, frame ou animação.
O relatório por pacote também verifica somente a presença literal de `__Morph:::`
e o padrão nominal `*_morph` nos `.o` APT: nos 279 objetos válidos de `fe.bh` e
`ig.bh`, ambos permaneceram ausentes. Isso documenta ausência de ligação direta
observada, não prova que os pacotes não contenham dados de animação.
O controle negativo textual reproduzível pode ser gerado com:

```text
dotnet run --project fifa_pc -- apt-animation-negative-control "D:\projeto zeta\fifa_pc\apt-structure-fe.tsv" "D:\projeto zeta\fifa_pc\apt-structure-ig.tsv" "D:\projeto zeta\fifa_pc\animation-bank-name-index.tsv" --report "D:\projeto zeta\fifa_pc\apt-animation-negative-control.tsv"
dotnet run --project fifa_pc -- apt-cross-package-profile-audit "D:\projeto zeta\fifa_pc\apt-structure-fe-profiles.tsv" "D:\projeto zeta\fifa_pc\apt-structure-ig-profiles.tsv" "D:\projeto zeta\fifa_pc\apt-cross-package-profile-audit.tsv" --summary "D:\projeto zeta\fifa_pc\apt-cross-package-profile-audit-summary.tsv"
```

Na ISO atual, o controle cruzou 284 linhas APT com 1.584 linhas de nomes de
animação: 279 objetos válidos, 259 conjuntos estruturais completos, 0
`__Morph:::`, 0 candidatos `*_morph` e 0 coincidências exatas de stems com
nomes de banco ou arquivos de animação. Ele compara somente texto e presença
estrutural; não converte ausência ou coincidência em timing, pose, frame ou
binding.
O comando `apt-cross-package-profile-audit` compara chaves exatas de header APT,
header `.const` e extensões entre os perfis FE/IG. Na ISO foram observadas 80
chaves FE, 51 IG, 27 compartilhadas, 53 somente FE e 24 somente IG; nenhuma
chave compartilhada teve word alinhada igual ou símbolo `__Morph:::`. Isso é
recorrência de perfil de container/tabela, não timing, pose, frame ou animação.
O `GameAssetCatalog.Scan` repete essa leitura estrutural durante `run-headless`.
O relatório runtime expõe `apt_packages`, `apt_nested_archives`,
`apt_nested_entries`, `apt_valid_entries`, `apt_valid_objects`,
`apt_valid_shapes`, `apt_complete_companion_sets` e
`apt_structural_signatures`; na ISO atual os valores agregados são
`2`, `284`, `1106`, `284`, `279`, `259`, `259` e `230`. Esses contadores
confirmam containers e companions, sem criar bindings de timing, pose, morph,
frame ou material.

O comando animation-bank-inventory percorre os sidecars .axt de data/anims.big
e cruza cada catálogo com os pares .ord/.orl do mesmo banco:

dotnet run --project fifa_pc -- animation-bank-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-inventory.tsv"

Na ISO atual há 20 entradas .axt, das quais 19 são catálogos com 1.584 nomes
de arquivos. Os 19 pares .ord/.orl foram combinados como ELF MIPS válidos e
cada um expõe exatamente um símbolo __AnimationBank::: coincidente com o nome
do catálogo. Isso comprova identidade de banco e não decodifica frames, poses,
rigging, timing ou skinning.
O relatório também preserva a seção .data e o hash do layout de seções e
relocations: foram 19 hashes distintos, e a distância observada do símbolo ao
fim de .data foi 28 em todos os bancos. Essa é uma regularidade estrutural, não
uma interpretação de records, frames ou poses.
Ele também grava `normalized_data_payload_sha256`: o hash usa somente os bytes
da seção `.data` e zera as words cobertas por relocations. Os 19 hashes
normalizados são distintos, portanto essa regra não encontrou um template
byte-a-byte comum; isso não decodifica records, frames, poses, ossos, pesos,
rigging, timing ou skinning.
O comando complementar compara os 19 payloads em pares, sem sair da evidência
de bytes/words alinhados:

```text
dotnet run --project fifa_pc -- animation-bank-payload-compare "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-payload-comparison.tsv"
```

São 171 pares; nenhum payload bruto ou normalizado foi exatamente igual. O
maior overlap normalizado teve 5.429 bytes e 129 words iguais; o maior trecho
contíguo igual foi de 40 bytes ou 10 words, tanto cru quanto normalizado. A
comparação continua sendo recorrência estrutural de payload, não um decoder de records,
frames, poses, ossos, pesos, rigging, timing ou skinning.
Durante `run-headless`, `GameAssetCatalog.Scan` repete essa validação e expõe no
relatório `animation_banks`, `animation_banks_complete_pairs`,
`animation_banks_elf_validated`, `animation_banks_symbol_matches` e
`animation_banks_distinct_normalized_payloads`. Na ISO atual, cada contador é
19. A integração confirma presença e identidade estrutural dos assets, não
seleção nem reprodução de animação.

O smoke do caminho completo de runtime é:

```text
dotnet run --project fifa_pc -- animation-bank-runtime-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Ele confirma 19 bancos, 1.584 entradas de catálogo, 1.578 nomes distintos e
19/19 pares `.ord/.orl`, ELF e símbolos correspondentes. O catálogo preserva
nomes e timestamps como metadata estrutural; o smoke não os trata como frames,
poses, rigging, timing ou skinning.

Para indexar cada linha textual dos catálogos por banco, preservando também a
contagem de nomes que aparecem em mais de um banco:

```text
dotnet run --project fifa_pc -- animation-bank-name-index "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-name-index.tsv"
```

O índice validado contém 1.584 linhas, 1.578 nomes distintos e 6 nomes
presentes em mais de um banco. Ele é uma ferramenta de seleção diagnóstica para
próxima investigação; nomes, timestamps e recorrência não são promovidos a
frames, poses, rig, timing ou skinning.

O comando `animation-bank-catalog-timestamp-audit` verifica a correspondência
entre o timestamp numérico e a data textual da própria linha. As 1.584 linhas
coincidem, o que classifica esse campo como metadata de catálogo observada;
`direct_timing_binding=not-proven` permanece sem alteração:

```text
dotnet run --project fifa_pc -- animation-bank-catalog-timestamp-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-catalog-timestamp-audit.tsv" --summary "D:\projeto zeta\fifa_pc\animation-bank-catalog-timestamp-audit-summary.tsv"
```

A comparação independente dos catálogos `.axt` pode ser gerada com:

```text
dotnet run --project fifa_pc -- animation-bank-catalog-compare "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --profile-report "D:\projeto zeta\fifa_pc\animation-bank-catalog-profiles.tsv" --pair-report "D:\projeto zeta\fifa_pc\animation-bank-catalog-pairs.tsv"
```

Ela produziu 19 perfis e 171 pares: os 6 nomes compartilhados entre bancos
também compartilham o timestamp observado, mas não houve conjunto de nomes,
posição ou prefixo ordenado comum entre pares. São métricas catalog-only; não
há interpretação de frames, poses, rig, timing ou skinning.

Para expor somente os headers de seção e o layout de relocations dos ELF dos
bancos:

```text
dotnet run --project fifa_pc -- animation-bank-elf-structure "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --profile-report "D:\projeto zeta\fifa_pc\animation-bank-elf-profiles.tsv" --section-report "D:\projeto zeta\fifa_pc\animation-bank-elf-sections.tsv"
```

O relatório real validou 19/19 ELF, 114 linhas de seção e 6 seções por banco;
os 19 hashes de perfil de seção e os 19 hashes de relocation foram distintos.
Os 19 bancos preservaram metadata do símbolo `__AnimationBank:::` e distância
observada de 28 bytes ao fim de `.data`. Isso é evidência de header/layout,
sem interpretar payload, tamanhos, offsets ou relocations como frames, poses,
rig, timing ou skinning.

A comparação da cauda ancorada no símbolo pode ser gerada com:

```text
dotnet run --project fifa_pc -- animation-bank-tail-compare "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-tail-comparison.tsv"
```

Nos 19 bancos, a cauda tem 32 bytes; não houve pares crus iguais, 12 pares
foram iguais após mascarar relocations e 30/32 offsets normalizados foram
comuns em todos. A word little-endian nos offsets 4–5 igualou o dobro da
contagem catalogada e o dobro da contagem declarada no cabeçalho em 19/19
casos; as duas contagens também coincidiram em 19/19 bancos. Isso é recorrência
e correspondência numérica observada, não semântica de campo ou decoder de
animação.

Uma auditoria complementar, ainda sem interpretar payload, pode ser gerada
com:

```text
dotnet run --project fifa_pc -- animation-bank-data-relocation-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-data-relocation-audit.tsv"
```

O relatório real validou 19/19 ELF e 35.900 relocations que apontam para
`.data`; os 19 bancos têm pelo menos uma relocation, 19 perfis de offsets
distintos, 15 contagens distintas de gaps adjacentes e, no máximo, 291 offsets
distintos/gaps distintos em um banco. A auditoria registra somente offsets
relativos, gaps e hashes estruturais; não lê words de payload nem cria
semântica de frame, pose, rig, timing, skinning, material, UV ou GPU.

A auditoria complementar da fronteira física `.ord/.orl` pode ser gerada com:

```text
dotnet run --project fifa_pc -- animation-bank-sidecar-boundary-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-sidecar-boundary-audit.tsv" --summary "D:\projeto zeta\fifa_pc\animation-bank-sidecar-boundary-summary.tsv"
```

Na ISO, os 19 pares não têm nenhuma seção ELF cruzando a fronteira: somente
`.data` fica em `.ord`, as outras quatro seções file-backed, a relocation e a
tabela de seções ficam em `.orl`. O relatório preserva ranges e até 16 bytes de
contexto de cada lado. Isso confirma composição física dos sidecars, não
semântica de payload, frames, poses, rig, timing ou comandos.

Para repetir essa medição nos irmãos morph, filtrando somente objetos com o
símbolo `__Morph:::` comprovado:

```text
dotnet run --project fifa_pc -- eagl-morph-sidecar-boundary-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --report "D:\projeto zeta\fifa_pc\eagl-morph-sidecar-boundary-audit.tsv" --summary "D:\projeto zeta\fifa_pc\eagl-morph-sidecar-boundary-summary.tsv"
```

O resultado atual cobre 391 BIGs e 594 objetos: em todos, 1 seção file-backed
fica em `.ord`, 4 em `.orl`, relocation e tabela de seções ficam em `.orl` e
nenhuma seção cruza a divisão. Os 886 pares encontrados apenas pelo sufixo
nominal `_morph.o` ficam fora da contagem comprovada. Isso continua sendo
evidência de container/range, não binding de morph, mesh, pose, rig, peso ou
animação.

Para comparar, de forma independente, os pacotes SHPX same-stem dos rows morph:

```text
dotnet run --project fifa_pc -- eagl-morph-shpx-structure-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit-summary.tsv"
```

O relatório atual cobre 594/594 rows, valida 591 pacotes `.xsh`, registra 3
ausências (`tops0001`, `tops0020`, `tops0210`) e rejeita 0 pacotes. Dezessete
dos 393 layouts têm perfis SHPX variáveis e todos pertencem aos 38 layouts
ambíguos já medidos. Isso é presença, versão, contagem e tabela SHPX; não é
binding de morph, textura, material, UV, mesh, pose, rig, peso, frame, timing
ou GPU.

O comando `eagl-morph-shpx-geometry-correlation-audit` cruza esse relatório
SHPX com as assinaturas de geometria candidata do catálogo, em nível de layout:

```text
dotnet run --project fifa_pc -- eagl-morph-shpx-geometry-correlation-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-shpx-geometry-correlation.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-shpx-geometry-correlation-summary.tsv"
```

O relatório real uniu 393/393 layouts: 17 têm variação simultânea SHPX e
geometria candidata, 21 variam somente na geometria e 355 permanecem estáveis;
17 dos 38 layouts ambíguos estão na primeira classe. Um layout continua sem
bounds de geometria. Isso é co-variação estrutural entre dois relatórios
separados, não pareamento de entrada SHPX com mesh, morph, UV, material, pose,
rig, skinning, animação, timing ou GPU.

O header ELF dos mesmos objetos morph pode ser comparado com:

```text
dotnet run --project fifa_pc -- eagl-morph-elf-header-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-elf-header-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-elf-header-audit-summary.tsv"
```

Os 594/594 ELF foram válidos e os 393 layouts tiveram exatamente um perfil de
header core, range da tabela e metadata `__Morph:::`; os 38 layouts ambíguos
também não variaram nesses campos. Isso é controle estrutural negativo, não
decoder de morph, pose, rig, peso, frame, timing ou comandos GPU.

Para ver bytes/words alinhados de uma entrada pequena sem gravá-la:

```text
dotnet run --project fifa_pc -- vivf-nested-asset-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh 0 ArrBLeft.apt --hex --words
```

As opções são diagnóstico limitado; o filesystem virtual e a ISO continuam
somente leitura.

Inspecionar uma entrada interna de imagens SHPX:

```text
dotnet run --project fifa_pc -- vivf-nested-asset-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh 0 ArrBLeft.xsh
```

Converter quadros SHPX suportados — DXT1, DXT3 e PAL8 com paleta anexada — para TGA de 32 bits com alfa:

```text
dotnet run --project fifa_pc -- vivf-nested-shpx-export "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh 0 ArrBLeft.xsh "D:\projeto zeta\exports\ArrBLeft"
```

Os registros SHPX são imagens/quadros, não malhas. O conversor cobre DXT1,
DXT3 e o PAL8 com paleta anexada observado em `player1.xsh`; os 46 quadros
foram exportados. Malhas e temporização/sequenciamento ainda dependem da
reconstrução das estruturas APT/ELF/MIPS.

A auditoria de entradas `shpx-image-entry-audit` valida novamente o pacote
`data/aptanims/player1.xsh` sem gravar na ISO. O resultado atual é
`verified-image-entries`: 46/46 entradas decodificadas como PAL8 128x128, com
SHA-256 por imagem e hashes da tabela e da ordem de decodificação em
`fifa_pc/shpx-image-entry-audit.tsv`. O relatório continua deliberadamente
sem semântica de timing, pose, rigging, skinning, animação, material, UV, morph
ou GPU.

```powershell
dotnet run -c Release --no-build --project fifa_pc -- shpx-image-entry-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/player1.xsh --report "D:\projeto zeta\fifa_pc\shpx-image-entry-audit.tsv"
```

O inventário de modelos ELF/MIPS valida os descritores `__VB`, mede streams
float3 contra `__BBOX` e percorre `__Model`/layers/render descriptors. No lote
atual, 2.389 de 2.390 primitivas têm um único candidato de vértices; 2.387
passam a validação de coerência entre os cinco slots do comando `id=9` e os
strides dos grupos `__GPvb`, agora exposta como binding estrutural explícito.
Todas as 1.615 que declaram explicitamente `PT_TRIANGLESTRIP` passam. O
binding completo do buffer ao draw call, materiais e atributos ainda não estão
provados. O inventário também mede 2.390 streams `float2` finitos, todos em
faixa próxima de `[0,1]`; 2.387/2.387 bindings explícitos têm esse perfil no
slot 1, e 772 combinam-no com `SHAPENAME` e os tokens de clamp
`XBOXEXTOBJ_SetClampU/V`.
Isso permanece evidência numérica de atributo, não uma decisão de UV/material
nem uma ligação automática ao `SHAPENAME`.
Nos mesmos 2.387 bindings completos, o perfil compacto de floats finitos soma
7.161 ocorrências: 2.387 no slot 0 com 3 componentes, 2.387 no slot 1 com 2
componentes e 2.387 no slot 3 com 3 componentes. Os demais slots não passam
essa checagem conservadora de stride inteiro em floats.

Exportar uma nuvem OBJ de candidatos para inspeção:

```text
dotnet run --project fifa_pc -- eagl-position-cloud "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head.ord
```

Exportar faces provisórias apenas para strips explícitos e com um único stream
candidato:

```text
dotnet run --project fifa_pc -- eagl-strip-candidate-export "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head.ord
```

O segundo comando produziu três grupos, 534 vértices e 528 triângulos para
`star0001-head`; os índices foram validados, mas a malha continua provisória
até confirmar a associação ao draw. Nenhum dos exports é um modelo jogável.
Na cadeia gerenciada, esses três grupos também preservam 534 valores
`Slot1Attributes`, um por vértice, como evidência genérica do segundo fetch;
o nome não implica UV e nenhum material é aplicado. A opção `--slot1-profile`
apenas visualiza essa evidência por cores médias de triângulo.
O mesh também preserva perfis compactos de floats finitos dos bindings ligados:
no objeto real são 9 perfis, distribuídos como `slot 0=3`, `slot 1=3` e
`slot 3=3` entre os grupos aceitos. Isso continua sendo uma medição numérica;
não rotula os componentes como UV, normal, peso ou material.
Os três grupos aceitos também preservam as atribuições de estado nomeadas e os
IDs dos comandos do fluxo completo. O smoke real informa
`groups_with_preserved_state=3` e três grupos com IDs; os grupos aceitos usam
estado `GeoPrimState`, enquanto os estados `SHAPENAME` aparecem em primitivas
que não entram no strip candidato. Essa correlação permanece evidência, sem
resolver `SHAPENAME` para textura ou tratar IDs como material/draw.

Para comparar esses valores de estado com os nomes literais dos `.xsh` no
mesmo `data/models.big`, use o índice somente leitura:

```text
dotnet run --project fifa_pc -- eagl-shapename-index "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big
```

O resultado atual percorre 886 pares `.ord/.orl`, encontra 774 ocorrências em
`SHAPENAME` (`head,1` e `tops,1`) e 724 entradas `.xsh`; não há match exato nem
com o basename normalizado (`head`/`tops`) entre esses valores e os `.xsh`.
`tops` é apenas prefixo de 152 basenames e `head` de nenhum, o que é ambíguo e
não autoriza escolher um recurso. Isso é evidência negativa para um binding
textual direto, não uma prova de que os estados sejam inúteis: o material ainda
exige decodificação do fluxo/recursos. O índice também descompacta os 724
pacotes `.xsh` em memória, valida 1.545 registros internos e encontra 14 nomes
internos distintos; `tops` coincide com um nome interno, mas aparece em 224
pacotes, enquanto `head` não coincide. A coincidência isolada continua
insuficiente para escolher uma imagem.

Quando o vínculo puder ser reproduzido no mesmo tronco, o loader de material
usa uma regra mais forte e explícita: `eagl-material-smoke` abre o `.ord`, exige
`SHAPENAME`, abre o `.xsh` correspondente ao basename do `.big`, valida o
registro SHPX interno exato e decodifica a imagem. Na amostra real:

```text
dotnet run --project fifa_pc -- eagl-material-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops
```

O resultado é `tops0030.ord` + `tops0030.xsh` + entrada `tops`, PAL8 256x124,
126.976 bytes RGBA. `eagl-candidate-object-smoke` lê a geometria correspondente
(2 grupos, 341 vértices, 337 triângulos, 13 primitivas), e
`eagl-material-preview` rasteriza a textura no caminho candidato de
`Slot1Attributes`:

```text
dotnet run --project fifa_pc -- eagl-material-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops "D:\projeto zeta\fifa_pc\tops0030-material-preview.ppm"
```

Esse preview produziu 2 grupos texturizados e checksum
`FB81D8B0DBB5E819`. É integração software verificável, não uma atribuição
global de semântica UV, skinning, deformação ou comandos GPU.

Para exercitar a mesma cadeia no `GameRuntime` headless, use o modo explícito
de material candidato:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --candidate-material
```

Sem argumentos adicionais, esse modo usa `tops0030.big`/`tops` e encontra o
objeto `.ord` carregando `SHAPENAME=tops,1`. Também aceita
`--candidate-material <nested.big> <SHAPENAME> [objeto.ord]`. A textura só é
usada quando a cadeia same-trunk e a decodificação são validadas; caso
contrário o renderer conserva o mesh candidato com fallback sólido. A execução
real de 80 frames produziu `CFA4BCBEE162C121`; o modo padrão continua em
`5405DE72BE1B4FC1`. Isso é uma integração software opt-in: os valores de
`Slot1Attributes` são usados como coordenadas somente neste caminho candidato,
sem afirmar UV universal, skinning, rigging, animação ou semântica dos comandos
GPU.

Para repetir somente a regra same-trunk em todos os modelos que têm `.big` e
`.xsh` homônimos, use:

```text
dotnet run --project fifa_pc -- eagl-material-batch-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Na ISO atual, 24 arquivos `.big` distintos passaram a validação completa e
decodificaram texturas PAL8 válidas; todos pertencem à família `tops`. O lote
não atribui semântica aos prefixos ambíguos `head`, `face` ou `hair`.
Uma segunda prévia real, `tops0040.big`, usou PAL8 256x256 e produziu 2 grupos,
527 vértices, 523 triângulos e checksum `F130B0BAB3361DDF` em
`fifa_pc/tops0040-material-preview.ppm`.

Para repetir a integração software candidata em todos os 24 vínculos verificados:

```text
dotnet run --project fifa_pc -- eagl-material-batch-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\material-batch-previews-stage-3"
```

O manifesto atual registra 24/24 PPMs P6, 0 rejeições e 24 checksums distintos:
14 meshes têm dois grupos e 10 têm um grupo, totalizando 7.403 vértices, 7.327
triângulos e 38 grupos texturizados. É uma prévia candidata por objeto; não
prova UV, material GPU, skinning ou deformação.
O `GameAssetCatalog.Scan` também repete a validação same-trunk no runtime
headless. Na ISO atual, `candidate_material_matches=24`,
`candidate_material_objects=24`, `candidate_material_shapes=1`,
`candidate_material_textures_decoded=24` e `candidate_material_pal8=24`.
Esses contadores registram somente a cadeia textual e a imagem decodificada;
não são uma tabela global de UV/material nem uma interpretação de comandos GPU.

Para verificar a integração software do lote no mesmo caminho usado pelo
runtime, opcionalmente materializando um relatório por amostra:

```text
dotnet run --project fifa_pc -- eagl-material-runtime-batch-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-material-runtime-batch.tsv"
```

O smoke passa cada um dos 24 meshes/texturas pela cadeia com `titleimage.xbx`,
`SoftwareGameplayRenderer` e `PreviewRenderer`. A execução validada produziu
24/24 frames, 0 rejeições, 24 checksums distintos e 38 grupos texturizados.
Também exercitou os três slots authored: `candidate_home_slot_checksum`,
`candidate_away_slot_checksum` e `candidate_both_slot_checksum` foram válidos
em 24/24 amostras, com 24 checksums distintos em cada coluna. A opção `--report`
grava uma linha por amostra em
`fifa_pc/eagl-material-runtime-batch.tsv`; o `project-readiness-audit` usa
somente esses contadores para rastrear a integração candidata. Continua sendo
integração candidata; não estabelece UV global, material GPU, skinning, rigging
ou semântica dos comandos originais.
O smoke também verifica o estado preservado no objeto: `SHAPENAME=tops,1`
aparece em 24/24 objetos, mas em 0/24 grupos geométricos candidatos. Em outras
palavras, a presença textual no objeto não é tratada como herança automática
para a malha; essa associação permanece `unproven` no manifesto e no preview.

Para medir uma propriedade ainda mais estreita do renderer gerenciado, a nova
auditoria compara, para cada vínculo same-trunk, o mesmo mesh candidato com a
textura candidata e com o fallback sólido, mantendo o restante da cena
idêntico:

```text
dotnet run --project fifa_pc -- eagl-candidate-render-influence-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\candidate-render-influence-audit.tsv"
```

O resultado real tem geometria finita/indexada em `24/24` amostras e diferença
de checksum texturizado--sólido na cena authored em `20/24`; quatro linhas (`tops0080`,
`tops0140`, `tops0260` e `tops0340`) permanecem como
`observed-candidate-raster-unchanged` nessa cena. O controle isolado, em buffer
neutro e com a mesma malha, encontrou influência texturizada em `24/24`; o
relatório registra triângulos visíveis, diversidade/alpha dos bytes RGBA e as
contagens de pixels RGB diferentes nos dois controles, mas não usa isso para inferir UV,
material original, skinning, rigging, animação ou GPU. SHA-256 do relatório:
`886419C9E716086C87BB36E2DF9728FF734E1D883FCCDEEDFC683A20048BB6E2`.

As prévias `tops0030-material-preview.ppm` e
`tops0040-material-preview.ppm` rasterizam a cadeia candidata, mas a inspeção
visual mostra fragmentação/alongamento fortes. O comando abaixo audita os gates
objetivos do mesh e a mesma projeção inteira usada pelo renderer, sem atribuir
semântica aos streams:

```text
dotnet run --project fifa_pc -- eagl-candidate-geometry-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-candidate-geometry-audit.tsv"
```

Na execução validada, os 24 objetos tiveram 0 índices inválidos, 0 vértices
não finitos e 0 índices repetidos; foram contados 46 triângulos de área 3D
exatamente zero, 48 de área XY zero e 196 degenerados após arredondamento,
contra 7.131 triângulos visíveis. Isso é uma auditoria objetiva do caminho
candidato, não uma correção de topologia, UV, material, skinning, rigging,
animação ou comandos GPU. As versões PNG derivadas para inspeção visual são
`tops0030-material-preview.png` e `tops0040-material-preview.png`.

O `MorphCandidateGeometryAudit` do inventário morph também pode gerar uma medição geométrica adicional dos
meshes candidatos, sem alterar a matriz de ambiguidade:

```text
dotnet run --project fifa_pc -- eagl-morph-sibling-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --geometry-report "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit.tsv" --geometry-summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit-summary.tsv"
```

O resultado real tem 594 linhas em 393 layouts; 38 layouts variam na
assinatura numérica e, em 32 deles, a assinatura topológica candidata fica
estável enquanto o perfil geométrico varia. Um layout tem linha sem assinatura
por ausência de bounds; os 393 layouts foram completos e ELF-validos e 392
tiveram bounds em todas as linhas. A medição continua restrita à representação
candidata: não é decoder de topologia original, UV, material, skinning,
rigging, animação ou comandos GPU.
A correlação adicional encontrou 38 layouts variáveis no par
payload/geometria, 27 com payload estável e geometria variável e 11 com ambas
as camadas variáveis; também é somente triagem e não um binding.
O controle independente `morph-candidate-geometry-ambiguity-audit` cruzou essa
coluna com os rótulos publicados e encontrou 38/38 variáveis em layouts
ambíguos, 0 em layouts estáveis, 0 ambíguos sem variação geométrica e 0
divergências de rótulo.

Para comparar a geometria candidata nos três planos cartesianos, sem escolher
um eixo ou uma câmera para o runtime, use:

```text
dotnet run --project fifa_pc -- eagl-candidate-projection-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops "D:\projeto zeta\fifa_pc\candidate-projection-previews-stage-1" tops0030.ord
```

O comando gera PPMs `xy`, `xz` e `yz` com preenchimento/contorno da mesma
malha candidata. A comparação real de `tops0030` e `tops0040` alterou as
silhuetas, mas não eliminou a fragmentação; portanto nenhum plano foi
promovido a eixo/câmera do jogo. Esses arquivos são QA geométrico e não
estabelecem topologia original, UV, material GPU, skinning, rigging, animação
ou semântica dos comandos.

Gerar uma prévia PPM de um quadro específico, com transparência composta em
fundo quadriculado:

```text
dotnet run --project fifa_pc -- shpx-frame-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/player1.xsh 0 "D:\projeto zeta\exports\player1-frame-02.ppm"
```

Inspecionar um asset diretamente no filesystem virtual:

```text
dotnet run --project fifa_pc -- asset-format-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/titleimage.xbx
```

Para bancos ABKC, `--verbose` mostra as palavras brutas dos primeiros 256 bytes
como apoio à investigação; ainda não atribui semântica aos campos internos:

```text
dotnet run --project fifa_pc -- asset-format-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/audio/AMBSTRM.abk --verbose
```

Investigar a estrutura limitada dos bancos e seus arquivos de áudio
relacionados, sem extrair ou modificar a ISO:

```text
dotnet run --project fifa_pc -- audio-structure-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Validar todos os blocos `SCDl` dos streams `.ast`, sem produzir WAV:

```text
dotnet run --project fifa_pc -- audio-validate "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Para materializar candidatos estruturais entre `eventspa.evt` e o catálogo
PA, sem reproduzir áudio:

```text
dotnet run --project fifa_pc -- audio-event-candidate-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Inspecionar os candidatos que têm nome exato na string table e uma row PA
paralela, sem tratar o nome como semântica original:

```text
dotnet run --project fifa_pc -- audio-event-named-candidates "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Na ISO atual, o comando encontra 17 candidatos nomeados e 17 passam a inspeção
do envelope. Sete expõem canais no header observado; os outros dez têm
`channels=0`, mas o audit independente `audio-sccl-channel-layout-audit`
validou uma hipótese mono única em 144/144 ranges, incluindo esses candidatos.
O runtime prepara os 17 payloads nomeados em cache, preservando ambos os fatos
(`channels=0` observado e `candidate_channels=1` validado). O runtime usa
`goal` para `GOAL_HOME`/`GOAL_AWAY` e, por escolha authored explícita, `panna`
para `TRICK`; esses caminhos continuam sendo candidatos clean-room, não
bindings originais. Os outros nomes permanecem sem binding de gameplay.

Validar os ranges candidatos sem alocar WAV:

```text
dotnet run --project fifa_pc -- audio-event-candidate-validate "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Uma row válida pode ser decodificada explicitamente para WAV para verificar o
backend `pcpadat` sem atribuir o áudio a um nome do EVT:

```text
dotnet run --project fifa_pc -- audio-event-candidate-decode "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 0 "D:\projeto zeta\fifa_pc\event-row-0-verified.wav"
```

O decoder genérico continua rejeitando uma row cujo header observado tenha
`channels=0` quando não há hipótese de layout separadamente validada. Para os
144 ranges zero-channel, a nova auditoria testa mono/estéreo contra todos os
frames `SCDl`: 144/144 passam somente como mono, 0/144 como estéreo, sem casos
ambíguos. Essa hipótese é usada apenas na preparação de payload candidato; a
seleção automática permanece desativada para todos os eventos sem associação
independente, e somente as duas regras clean-room de gol usam o candidato
nomeado validado.

Na ISO, 139/283 ranges passam como `SCDl`; os 144 restantes são rejeitados
porque o metadata `SCHl` não contém o tag `0x82`, fazendo o decoder representar
`channels=0`. O comando
`audio-sccl-inspect` confirmou `SCCl` como primeiro chunk em 283/283 ranges;
os 139 decodificáveis também começam assim, e o walker o pula como envelope
estrutural antes dos chunks `SCDl`. Nos primeiros casos, `SCCl` tem 12 bytes,
payload variável e é seguido por `SCDl`; a regra de canais e a semântica do
payload ainda não foram determinadas.

Para registrar essa evidência sem alocar WAV:

```text
dotnet run --project fifa_pc -- audio-sccl-inspect "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 12
```

Para amostrar especificamente os ranges que o decoder representa com
`channels=0`:

```text
dotnet run --project fifa_pc -- audio-sccl-inspect "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 4 --rejected
```

Na ISO, esses casos continuam com cadeia `SCCl -> SCDl`, taxa 22.050 Hz e
tags `0x05/0x06/0x80/0x84/0x85/0xA0/0x8C`, mas sem `0x82`; os payloads `SCCl`
variam e não preenchem o header observado. A hipótese mono só é aceita quando
o layout e todos os frames decodificam; ela não autoriza binding de evento ou
reprodução automática desses nomes.

Para repetir a auditoria de layout sem escrever WAV:

```text
dotnet run --project fifa_pc -- audio-sccl-channel-layout-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\audio-sccl-channel-layout-audit.tsv"
```

Na ISO atual, os seis streams `.ast` passam essa validação integral. O comando
continua sendo diagnóstico; a integração de gameplay separada prepara a row
nomeada `goal` quando recebe `GOAL_HOME` ou `GOAL_AWAY`. Essa simetria é uma
regra nova do runtime, não vínculo EVT→som comprovado.

O comando confirma invariantes observados nos 16 `.abk`, valida as faixas dos
16 `.off` contra os `.big` de mesmo tronco e verifica os prefixos `SCHl` dos
segmentos de dados. Também registra o prefixo, runs de texto e limites dos 2
`.evt`, além de comparar por ordem de linha os pares `pcpahdr`/`pcpadat` e
`djhdr_en`/`djdat_en`. Ele não escolhe amostras, não interpreta IDs de eventos
e não trata os words desconhecidos como offsets tipados. A comparação de
conteúdo lê o header inteiro e no máximo 64 bytes do stream, sem interpretar
os campos; ela registra apenas igualdades e interseções binárias observáveis.
O diagnóstico também separa o envelope neutro `SCHl` do parser detalhado de
metadados: os envelopes PA/DJ são válidos em 283/283 e 48/48 streams, e o
decoder estrutural percorre ambas as famílias após corrigir o salto de um
comprimento estendido já consumido.
O comando `audio-event-index-smoke` expõe 17 pares alinhados de offset relativo
e word para a string table de `eventspa.evt`; a família observada é
`word & 0xFFFF == 0x4F6B`, mas nenhum dos 17 words aparece exatamente nos `.abk`
enumerados. Esses valores permanecem evidência local do EVT, não IDs ABKC ou
seletores de amostra.
O comando `audio-event-index-search` repete essa verificação diretamente nos
companions `pcpahdr.big` e, com `--data`, `pcpadat.big`, usando busca alinhada
em streaming e sem criar vínculo de evento ou seleção de amostra. Na ISO atual,
os 17 words tiveram zero ocorrências nos dois companions.
Com `--report <saida.tsv>`, a mesma execução também percorre os 16 `.abk` e
materializa `audio-event-index-evidence.tsv`: 1.061 pares candidatos, 17
offsets/words distintos, zero ocorrências alinhadas nos 16 bancos e nos dois
companions. O relatório marca `status=no-direct-selection-binding`,
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`; isso
é um controle negativo limitado à busca alinhada, não uma prova de ausência de
qualquer rota interna nem um binding de evento, ABKC ou playback.
Os valores dos chunks também são comparados aos words dos `.evt`, mas os
matches encontrados são repetidos e não selecionam uma linha de áudio por si
sós. A correlação nomeada descrita acima é usada somente para os cues clean-room
explícitos `GOAL_HOME -> goal`, `GOAL_AWAY -> goal` e `TRICK -> panna`; a
mixagem dos demais eventos continua pendente. Em `eventspa.evt`, os matches dos words
iniciais de `pcpahdr` formam um bloco entre `0x74` e `0x1E68`, antes do limite
`0x1E70`, mas o formato dos records ainda não foi nomeado.
Os offsets relativos mostram repetição local (`+4` zero em 314/314), porém a
estrutura ainda não foi convertida em records ou IDs semânticos. Os 10 values
duplicados observados aparecem em múltiplos contextos relativos, e as 314
janelas completas são distintas. A métrica de espaçamento também compara
`eventsdj.evt`: PA tem gaps de 8 a 84 bytes, com gap 24 em 257 casos; DJ tem
uma ocorrência por idioma, salvo duas no italiano separadas por 220 bytes. Isso
é evidência estrutural, não seleção de amostras.

Decodificar uma textura XPR0 DXT1 para um quadro PPM:

```text
dotnet run --project fifa_pc -- texture-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/titleimage.xbx "D:\projeto zeta\fifa_pc\titleimage.ppm"
```

Decodificar EA-XA v3 de um stream SCHl e gerar WAV PCM16:

```text
dotnet run --project fifa_pc -- audio-export "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/audio/GBREAK.ast "D:\projeto zeta\exports\GBREAK.wav"
```

O decoder percorre blocos `SCDl` e aceita frames EA-XA ADPCM e PCM em streams
compatíveis; `SCCl` é reconhecido como variante estrutural, mas ainda não é
decodificado.
O mapeamento geral dos bancos ABKC para eventos e a mixagem durante o gameplay
ainda estão pendentes; as únicas rotas clean-room integradas são o candidato PA
nomeado `goal` para `GOAL_HOME` e `GOAL_AWAY`. Para reproduzir diretamente um
`.ast` compatível no Windows:

```text
dotnet run --project fifa_pc -- audio-play "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/audio/GBREAK.ast
```

`audio-play` decodifica para PCM16 em memória (limite de 32 MiB) e toca de forma
síncrona no dispositivo de áudio do Windows; `audio-export` continua sendo a
opção para streams maiores e para salvar WAV.

Para inspecionar a cadeia ABKC e validar referências externas contra os ASTs:

```text
dotnet run --project fifa_pc -- audio-abk-inspect "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --validate-streams
```

O catálogo também conserva esses ranges e o mixer pode preparar um range sob
demanda. Os candidatos EVT nomeados válidos são pré-preparados em cache, mas a
seleção por evento ABKC continua desativada até haver uma associação
independente entre EVT e entrada ABKC; o cue `goal` usa a row PA nomeada e não
é apresentado como vínculo ABKC original.
Os 13 bancos que contêm `BNK` também têm sua tabela de offsets validada durante
a montagem. Os headers variáveis `PT`/`GSTR` são analisados dentro dos limites
de cada entrada, preservando as tags como evidência bruta; no catálogo atual,
os 231 headers parseados expõem codec `0x14` e os 231 ranges de blocos apontados
passam pela validação estrutural Xbox IMA.

Para decodificar explicitamente um som RAM do `BNK` para WAV, sem atribuir o som
a um evento:

```text
dotnet run --project fifa_pc -- audio-abk-decode "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/audio/PLAYER.abk 1 "D:\projeto zeta\fifa_pc\player-sound-1-verified.wav"
```

Essa rota valida o descritor observado e decodifica Xbox IMA em código gerenciado.
Ela exige seleção explícita do índice, cria um arquivo novo e não toca nem mapeia
o áudio durante o gameplay; a associação EVT→som segue sem prova independente.
`audio-abk-play` usa a mesma seleção e envia o WAV PCM16 ao dispositivo WinMM
de forma síncrona, ainda como diagnóstico explícito e não como reprodução por
evento.

Executar os testes internos dos parsers sem usar a ISO:

```text
dotnet run --project fifa_pc -- self-test
```

Gerar um quadro de teste com o framebuffer de software:

```text
dotnet run --project fifa_pc -- render-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\preview.ppm"
```

O arquivo PPM é uma cena clean-room controlada pelo estado da partida; ele não
é uma conversão final dos modelos Xbox. A prévia usa `titleimage.xbx` validada
como textura real do volume, enquanto jogadores e quadra continuam sendo a
cena gerenciada de teste.

Executar a janela nativa interativa no Windows:

```text
dotnet run --project fifa_pc -- run-windowed "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Use `WASD` para mover, `P` para passe, `Espaço` para chute, `T` para drible e
`K` para o controle defensivo authored `TACKLE`.
Esse host já é código .NET/Win32 novo e desenha, quando validado, a malha
experimental preenchida e contornada de `star0001-head` sobre o jogador. A reprodução ao vivo e a
conversão das referências APT/MIPS para materiais, animações e geometria final
continuam em implementação.
O host ativa o mesmo oponente clean-room opcional para pressionar e finalizar;
isso é uma mecânica nova de runtime, não uma regra original comprovada.

O host windowed aceita também `--match-seconds <segundos>` para usar o mesmo
relógio authored do headless; `FULL_TIME` e `match_result` continuam sendo
estado clean-room, não duração ou regra original comprovada.

O host também aceita o caminho de material candidato de forma explícita:

```text
dotnet run --project fifa_pc -- run-windowed "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --candidate-material tops0040.big tops
```

Nesse modo, a mesma cadeia same-trunk usada no headless precisa validar
`tops0040.big`/`tops0040.ord`/`tops0040.xsh`/`tops` antes de a textura PAL8 ser
entregue ao renderer. Sem a opção, ou se a cadeia falhar, o host conserva o
fallback sólido de `star0001-head`. A integração é candidata e opt-in: não
afirma UV global, material GPU, skinning, rigging, animação ou comandos GPU.
O smoke automatizado desta etapa confirmou build Release, self-test 72/72,
batch 24/24, a matriz `project-readiness.tsv` e os caminhos headless
padrão/candidato; a janela interativa não é tratada como prova de semântica
original.

## Controle defensivo authored `TACKLE` — 2026-10-06

O runtime managed recebeu um controle defensivo explícito, separado de qualquer
reconstrução do input original. `MatchInput.Tackle` é opcional, a coluna 10 de
`MatchInputScript` e a tecla `K` no host Win32; `TackleDemoInputSource` exercita
essa cadeia em 100 frames. Quando há posse authored do adversário, proximidade
do jogador da casa e uma borda de input, a regra transfere a posse, concede
0,20 s de proteção e soma 10 ao medidor de gamebreaker. Isso é uma regra nova
do runtime clean-room; não é semântica decodificada do jogo original.

`TACKLE` compartilha somente a apresentação defensiva authored já usada por
`STEAL` e uma rota de áudio clean-room `TACKLE -> tackle`. A rota usa candidato
preparado/reproduzido e não reivindica seleção original de EVT, ABKC ou sample;
nenhum binding de material, UV, skinning, rigging, animação, morph ou GPU foi
criado.

A matriz verificou 10/10 cenários, 54 eventos gameplay/áudio, 54 preparados,
54 reproduzidos, 777 frames renderizados e 1.554 amostras de animação authored;
os replays de match, input, animação e áudio do cenário `tackle` coincidiram.
Release terminou com 0 avisos/erros, self-test `72/72` e testes Python `16/16`.
O readiness permanece `NOT_READY`: assets continuam candidatos, o gate de
renderização original `not-proven`, áudio geral parcial e animação estrutural;
as classificações authored não promovem semântica original.

Artefatos principais: `project-readiness.tsv` SHA-256
`4E4991215D3E53EE1EAC27CBF5D5D5B813A112F14096A20B5549DDBA620930F6`,
`gameplay-coverage.tsv` SHA-256
`CAAE2829969AF021BF9F5A914B5C33BFB72380F7794E1003072AA0199AC71640` e DLL
Release SHA-256
`EAB3E92D2ACE4C11F2D6FFFDFFB9281F50643F311AA22541E5236A73CD0636DD`.
A ISO permaneceu somente leitura, com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Auditoria estática atual do XBE

`tools/analysis/xbe_render_target_audit.py` produz janelas limitadas dos
destinos mais fortes nos clusters de contexto de renderização do XBE retail.
A etapa atual registrou `64/64` destinos disassemblados, `63` terminados por
`ret` e `7` prólogos semelhantes a função. Os artefatos são
`xbe-render-target-audit.tsv` e `xbe-render-target-summary.tsv`.

Essa saída é evidência estrutural/candidata: não executa o XBE, não confirma
limites de função e não inventa bindings para materiais, UVs, skinning,
rigging, animação, morphs ou comandos GPU. `original_semantics=not-claimed`;
o gate integrado de renderização ainda é `not-proven`, embora o subgate
`rendering-static-xbe-target-windows` seja `verified-structural`.

A validação da etapa passou em Release sem avisos/erros, self-test `72/72`,
testes Python `13/13`, matriz authored/candidate opt-in, smokes, runtime
headless, replay determinístico e captura windowed. A ISO continua somente
leitura e seu SHA-256 é
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Auditoria CFG estática de renderização

`tools/analysis/xbe_render_cfg_audit.py` analisa, de forma limitada, os
destinos em `xbe-render-target-audit.tsv`. Ela segue somente saltos diretos
dentro de janelas de 2048 bytes e conta chamadas sem segui-las. A etapa atual
registrou `64/64` CFGs alcançáveis, `62` retornos alcançáveis e `441` blocos
básicos; transferências indiretas permanecem não resolvidas.

Os arquivos `xbe-render-cfg-audit.tsv` e `xbe-render-cfg-summary.tsv` são
evidência estrutural, não limites de função recuperados. Não há execução do
XBE nem binding novo para materiais, UVs, skinning, rigging, animação, morphs
ou GPU; `original_semantics=not-claimed`. O subgate é
`rendering-static-xbe-cfg-slices=verified-structural`, mas a renderização
integrada continua `not-proven`.

A matriz após essa etapa passou Release `0/0`, self-test `72/72`, testes Python
`14/14`, runtime headless, smokes e replays determinísticos. A ISO permanece
somente leitura, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Correlação completa de payloads de áudio

O comando `audio-event-payload-wave-correlation-exhaustive-audit` foi rodado
com `--max-sound-candidates 100000`, cobrindo os `368` candidatos enumerados
nos `16` bancos. `239` decodificaram; os `17` candidatos EVT decodificaram;
foram encontrados `0` pares PCM exatos e `0` hashes EVT correspondentes.

Os arquivos `audio-event-payload-wave-correlation-all.tsv` e
`audio-event-payload-wave-correlation-all-summary.tsv` registram apenas essa
comparação negativa. Ela não fornece seleção evento→sample, binding ABKC ou
semântica original; o gate de áudio segue `partially-integrated` e
`direct_selection_binding=not-proven`.

A matriz após a etapa passou Release `0/0`, self-test `72/72`, testes Python
`14/14`, smokes, runtime headless e replays determinísticos. A ISO continua
somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Grafo estático limitado de chamadas do renderizador

`tools/analysis/xbe_render_call_graph_audit.py` segue chamadas diretas dos
`87` call targets estáticos anteriores. Com profundidade máxima `2`, janelas
de `256` bytes e limite de `512` nós, produziu `235` nós executáveis
disassemblados, `326` arestas únicas, `200` retornos alcançáveis e nenhum
truncamento.

Isso não recupera funções completas nem executa o XBE. Chamadas indiretas
continuam sem resolução; não há bindings inventados para materiais, UVs,
skinning, rigging, animação, morphs ou GPU. O subgate
`rendering-static-xbe-call-graph` é `verified-structural`, o gate integrado de
renderização segue `not-proven` e `original_semantics=not-claimed`.

Relatórios: `xbe-render-call-graph-nodes.tsv`, `xbe-render-call-graph-edges.tsv`
e `xbe-render-call-graph-summary.tsv`. A validação passou Release `0/0`,
self-test `72/72`, testes Python `16/16`, matriz authored/candidate opt-in,
smokes, runtime headless e replays determinísticos. A ISO continua somente
leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Formas estruturais dos acessores indexados — 2026-10-07

`tools/analysis/xbe_render_indirect_indexed_accessor_audit.py` observa os
destinos candidatos da tabela indexada em janelas bounded de `256` bytes, sem
executar o XBE e sem alegar limites de função. O relatório real registrou
`87` ocorrências, `81` destinos distintos, `81/81` mapeados/disassemblados e
`81/81` terminados por `ret`: `1` fallback `pop/pop/ret`, `55` formas
`mov-memory/ret`, `24` formas `lea/push/call/ret` e `1` outra forma.

Esse resultado é evidência estrutural de bytes/instruções, não um renderer
recuperado. Não define campos, materiais, UVs, skinning, rigging, animação,
morphs, comandos GPU ou bindings; `original_semantics=not-claimed`. O novo
subgate `rendering-static-xbe-indirect-indexed-accessor-shapes` é
`verified-structural`; o gate integrado de renderização continua
`not-proven`.

Artefatos: `xbe-render-indirect-indexed-accessor-audit.tsv` e
`xbe-render-indirect-indexed-accessor-summary.tsv`. A validação passou Release
`0/0`, self-test `72/72`, testes Python `32/32`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. A ISO continua somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Associação estrutural de slots e registros `__VB` — 2026-10-07

O comando `eagl-candidate-fetch-association-audit` cruza slots observados nos
streams candidate com registros `__VB` validados por símbolo, stride,
comprimento e contagem de elementos. A execução produziu `24/24` assets
carregados, `218` primitivas, `216` layouts completos de cinco slots e
`1080/1080` registros válidos; `22/24` assets têm todas as primitivas nesse
layout e nenhum candidato foi rejeitado.

Essa é uma associação estrutural do caminho candidate, não um binding semântico:
slots, símbolos e números permanecem opacos. Não foram atribuídos significados
de UV, material, normal, peso, skinning, rigging, animação, morph ou GPU;
`original_semantics=not-claimed`. O subgate
`rendering-candidate-fetch-record-association` é `verified-structural`, enquanto
assets continuam candidatos e o gate original de renderização segue
`not-proven`.

Relatórios: `eagl-candidate-fetch-association.tsv` e
`eagl-candidate-fetch-association-summary.tsv`. A validação passou Release
`0/0`, self-test `73/73`, testes Python `32/32`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. A ISO permanece somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Etapa 0.40 — controle negativo da população de estado EAGL — 2026-10-07

O audit bruto `eagl-original-render-chain-audit` passou a registrar slots como
estrutura, sem nomeá-los como atributos. Resultado same-trunk: `2364/2364`
streams completos, `2364` linhas com slots, `2362` sequências estruturais
completas `0..4`, `750` coocorrências `SHAPENAME`+slots e `0` sobreposições
`SHAPENAME`+`SetPrimitiveType`.

Coocorrência não é binding: UV, material, normal, peso, skinning, rigging,
animação, morph e GPU continuam sem semântica atribuída. O readiness registra
`rendering-original-state-population-negative-control=verified-structural-negative`,
mas renderização principal permanece `not-proven` e o estado geral é
`NOT_READY`.

Relatórios: `eagl-original-render-chain.tsv` SHA-256
`629C6D7A836CAA46A84E09A4EEA3DE8F3AC11ADE871219B7E205DF710C07B66C`;
`eagl-original-render-chain-summary.tsv` SHA-256
`16FAF6D2F477A621445015A87C05E04896F20F3CA5000B6634E9CBB15D647DF7`;
`project-readiness.tsv` SHA-256
`F4DBDEC11A306181CB2943AC1077A8B930CA6401EE40961281AC67D134F354DD`.
Release `0/0`, self-test `74/74`, Python `32/32`, matriz completa, smokes,
runtime headless, captura windowed e replays determinísticos passaram. DLL
Release: `DA0C35BEFF7C600F1E9CF9277EA854FBC6CBE5F8E3745C26ACB6DE50AE12E445`.
ISO preservada, `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar associação original independente adicional, ou preservar
explicitamente a ausência dela enquanto os subsistemas não decodificados
continuam separados do caminho candidate.

## Etapa 0.41 — controle independente da população de estado bruta — 2026-10-07

`tools/analysis/eagl_original_state_population_control.py` comparou, por
objeto bruto same-trunk, as populações observadas `SHAPENAME` e
`SetPrimitiveType` por índice, camada, hash e forma de command stream. Foram
verificados `883` objetos e `2364` linhas: `752` SHAPENAME, `1612`
SetPrimitiveType, `0` sobreposições, `0` pares de mesmo índice, `777` pares
adjacentes, `1904` na mesma camada, `0` hashes e `0` formas de fronteira iguais.

É evidência estrutural negativa, não binding original. Não há promoção para
geometria, UV, material, skinning, rigging, animação, morph ou GPU;
`original_semantics=not-claimed`, `rendering-original-state-population-control`
é `verified-structural-negative` e o projeto segue `NOT_READY`.

Relatório: `eagl-original-state-population-control.tsv`, SHA-256
`1DB67C35A1AFF669606666E86802E76117E160D05562C8B0E2F3B9CE487BE83F`;
resumo: `eagl-original-state-population-control-summary.tsv`, SHA-256
`6D47AFC8F8826B849985D618645DD59924C1D35D14FEFA879B38B75B58C8E7E4`.
Release `0/0`, self-test `74/74`, Python `33/33`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. A ISO continua
somente leitura, `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência original independente adicional ou manter a
lacuna explícita enquanto os subsistemas não decodificados permanecem fora do
caminho candidate. Não concluir o projeto nem iniciar code review.

## Etapa 0.42 — inventário estrutural bruto de `__Model`/`__VB` — 2026-10-07

O audit original same-trunk agora valida os registros delimitados por `__VB`
junto aos símbolos `__Model`, `__BBOX` e `__RM`. Foram observados `883` objetos,
`883` linhas `__Model`, `882` `__BBOX`, `2364` `__RM` e `11820/11820` registros
`__VB` estruturalmente válidos; `882` objetos têm bounds e `883` têm streams
validados.

É evidência comprovada de estrutura e limites de registros, não de binding.
Não foram atribuídos significados para geometria, UV, material, skinning,
rigging, animação, morph ou GPU, e não há promoção de estado para stream.
`rendering-original-vertex-record-structure=verified-structural`, mas o gate
original de renderização continua `not-proven`, `original_semantics=not-claimed`
e o readiness continua `NOT_READY`.

Relatório: `eagl-original-render-chain.tsv` — SHA-256
`629C6D7A836CAA46A84E09A4EEA3DE8F3AC11ADE871219B7E205DF710C07B66C`;
resumo: `eagl-original-render-chain-summary.tsv` — SHA-256
`36D8765FB5E3DC1E00ABDF59C8477384575F5D912A1B737F2BB1A971BCD4C8CF`;
readiness: `project-readiness.tsv` — SHA-256
`93065B27B628D955F4261B062F99C347B6B32269785F99AAD70F15BEAEA4FE9B`.
Release `0/0`, self-test `74/74`, Python `33/33`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. DLL Release:
`0BF974EA15DD900F6D57AE1FEA14BAD7095B36A552BFE398996D686EEED748CD`. ISO
preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar associação original independente entre registros
`__VB` e primitivas/streams; se não houver prova adicional, manter a lacuna
`not-proven` e continuar os subsistemas não decodificados fora do caminho
candidate. Não concluir nem iniciar code review.

## Etapa 0.43 — associação estrutural opaca `__VB`↔fetch — 2026-10-07

`eagl-original-vertex-record-association-audit` compara no caminho original
somente os cinco strides dos fetch slots com uma sequência única de cinco
categorias/ordinais consecutivos das chaves `__VB`. Resultado: `2364`
primitivas, `2364` com registros ordinais, `2362` associações opacas únicas,
`2362` layouts completos e nenhum pacote/objeto rejeitado.

É associação estrutural, não binding semântico. Não foram atribuídos
atributos nem significados de geometria, UV, material, skinning, rigging,
animação, morph ou GPU; `original_semantics=not-claimed`. O subgate
`rendering-original-opaque-vertex-record-association=verified-structural`, mas
renderização principal continua `not-proven` e o projeto continua `NOT_READY`.

Relatório: `eagl-original-vertex-record-association.tsv` — SHA-256
`162D94FA23DB73C469392FFD5AA037FA2D029E7E06D47B32C2692004BA7B9ECD`;
resumo: `eagl-original-vertex-record-association-summary.tsv` — SHA-256
`257C8F331CB6D9B3E9BB99E18F1DAEA99298779A8B035E78126B14DB26D118CE`;
readiness: `project-readiness.tsv` — SHA-256
`5C6468CB5D769B00EB88F0B3213D7BA293CEB8949815A5E5F3951B0526B8EBF9`.
Release `0/0`, self-test `75/75`, Python `33/33`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. DLL Release:
`030372D0DBDB657E53D09871B18B46B021F469F6F6BA112B8281C14E1A7C3FD1`. ISO
preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: aplicar controles independentes e buscar evidência adicional
de geometria/UV/material sem promover nomes, strides ou comandos opacos a
semântica.

## Etapa 0.44 — controle independente da associação opaca — 2026-10-07

`tools/analysis/eagl_original_vertex_record_association_control.py` reparseia
o relatório original por uma implementação independente. Foram reproduzidas
`2362/2362` associações opacas exatas em `2364` linhas e `883` objetos; ordem
de strides invertida e ordinal deslocado deram `0/0`.

Esse resultado é controle estrutural, não binding. Geometria, UV, material,
skinning, rigging, animação, morph e GPU continuam sem semântica atribuída;
renderização principal segue `not-proven`, `original_semantics=not-claimed` e
readiness `NOT_READY`.

Relatório: `eagl-original-vertex-record-association-control.tsv` — SHA-256
`3D5F57E04D65C4956E5F26AB994ABD5DA1C2ED79DEE1D905C4BC5DE62F75513A`;
resumo — SHA-256
`26E1DABB216D91957886D8C97ED3ADF5DC5BA448D8D36E260CBFA05C60FF9765`;
readiness — SHA-256
`EC399C25E4E12B19618F3FCA02C29ECC4F9E8532CF8AA0547DFA21F83003A699`.
Release `0/0`, self-test `75/75`, Python `34/34`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. DLL Release:
`56D7443171E80962DBA901970AE8764A59BBCAD10456B76B4904E9387B304A04`. ISO
preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência original de geometria/UV/material fora das
chaves, strides e comandos opacos, mantendo `not-proven` quando não houver
prova adicional.

## Etapa 0.45 concluída — estrutura numérica dos payloads originais `__VB`

`eagl-original-vertex-payload-audit` reabriu os objetos EAGL originais e
mediu somente estrutura numérica: `388/388` pacotes, `883/883` objetos,
`11820` streams `__VB` validados, perfis finitos em `883` objetos, contenção
nos bounds declarados em `882`, ranges distintos em `883` e zero sobreposições.
Perfis `float2` próximos da faixa unitária são reportados como observação
numérica, não como UV.

O resultado é evidência estrutural comprovada, não binding semântico. Não há
atribuição de posição, geometria, UV, material, skinning, rigging, animação,
morph ou GPU; `original_semantics=not-claimed`. O subgate
`rendering-original-numeric-vertex-payload-structure` é
`verified-structural`, mas renderização principal segue `not-proven` e o
readiness segue `NOT_READY`.

Artefatos: `eagl-original-vertex-payload.tsv`
(`EC69F3A7D0A3F56FB0178AE5A39B68258FFE749F57F5C0BEDCB9A27E93C99E3C`),
`eagl-original-vertex-payload-summary.tsv`
(`9487A2A19F9F9FC8E9403C068FBA54A895289695C3BF2055D6AEDB0FF4D63C21`) e
`project-readiness.tsv`
(`819BB6F05291BC2B8B3AFCD9961851B79EE3A586FE4A682344992E7D3C4949B0`). DLL
Release: `BF1B3B40F4382A7CB51BFADD89C4C277133AF2FE5E81D06B46D8FD2AE1234665`.

Validação final: Release `0/0`, self-test `76/76`, Python `34/34`, matriz
completa, smokes relevantes, runtime headless/windowed e replays determinísticos
passaram. ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.46. Procurar evidência original independente de geometria, UV ou material
além dos perfis numéricos e ranges, sem promover bindings sem prova.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.46 concluída — controle independente dos payloads originais `__VB`

`eagl_original_vertex_payload_control.py` reparseou o TSV original de forma
independente. Foram validados `883/883` objetos, com `883/883` assinaturas
numéricas coerentes, `883/883` metadados de ranges válidos e `883/883` statuses
de entrada válidos. Status: `verified-independent-original-numeric-payload-control`.

É evidência estrutural/negativa independente, não binding semântico. Não há
atribuição de posição, geometria, UV, material, skinning, rigging, animação,
morph ou GPU; `original_semantics=not-claimed`, renderização principal segue
`not-proven` e readiness segue `NOT_READY`.

Hashes: controle TSV
`0DD9A6EB1E5EEC72D309CCACDC7E7C69D632CF8E02722EB4EB77E14D4052D013`, resumo
`6E817B35190ACBC726EC70D09C2E8516D2002AF3C97643BC188CAA0EF8D60A04`, relatório
original `EC69F3A7D0A3F56FB0178AE5A39B68258FFE749F57F5C0BEDCB9A27E93C99E3C`,
resumo original `9487A2A19F9F9FC8E9403C068FBA54A895289695C3BF2055D6AEDB0FF4D63C21`,
readiness `A5731AC06117F6BE6B98BCCC5259519E4697F51FBC0215672FEB5349D5E1C2B0` e
DLL Release `A4A05B3728A9D724F7B69CAB6E17CB42756BA2FF4EA2DA70E7D00DE5B480C55F`.

Validação: Release `0/0`, self-test `76/76`, Python `35/35`, matriz completa,
smokes relevantes, runtime headless/windowed e replays determinísticos
passaram. ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.47. Buscar evidência original independente de geometria, UV, material, áudio
ou animação ainda não decodificada, mantendo `not-proven` e sem inventar
bindings.
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

Validação: Release `0/0`, self-test `77/77`, Python `36/36`, matriz completa,
smokes relevantes, runtime headless/windowed e replays determinísticos
passaram. ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.48. Procurar evidência original fora do vocabulário textual dos estados,
especialmente ligação verificável entre comandos opacos, payloads e
imagem/atributos, mantendo `not-proven` e sem inventar bindings.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.48 concluída — junção independente por objeto entre renderização e payload `__VB`

`eagl_original_render_payload_object_join.py` uniu os relatórios TSV originais
por `(model_archive, model_object)` sem promover semântica. Foram validados
`883/883` objetos unidos, `0` objetos exclusivos de cada fonte, `2364`
primitivas, `11820` streams numéricos e `883/883` contagens/statuses coerentes;
controles de chave invertida e deslocada ficaram em `0/0`.

O resultado confirma apenas identidade/coerência estrutural no nível de objeto.
Não há associação primitiva→stream nem binding de posição, geometria, UV,
material, atributo, skinning, rigging, animação, morph ou GPU. Status
`verified-independent-original-render-payload-object-join`,
`original_semantics=not-claimed`, renderização principal `not-proven` e
readiness `NOT_READY`.

Hashes: relatório
`D33B945D730077098359A129AE5E52EF0AFEA661EDF33EF4C910AF343D170985`, resumo
`6B95F71A207E2E0DEEFDE91A2E8E2C6F0216444B9F434110C2F772BAFA673DEE`, readiness
`0DE5599D135AEEEA38EA42840012ECA0E63CDAAF0AF08059BE793094A1FB08EA`, DLL
Release `8C2381ED22645B458B2EC4C273270D09A31C94AFEB7FBDF44B82C3F4F5F1A2CC`.

Validação: Release `0/0`, self-test `77/77`, Python `37/37`, matriz completa,
smokes relevantes, runtime headless/windowed e replays determinísticos
passaram. ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.49. Buscar associação original independente entre primitivas, streams e
atributos ou comandos/payloads, mantendo `not-proven` e sem inventar bindings.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.49 concluída — topologia estrutural primitiva/stream `__VB`

O comando `eagl-original-primitive-stream-topology-audit` e o controle
`eagl_original_primitive_stream_topology_control.py` juntam, sem semântica,
fetch slots, categorias/ordinais opacos, strides e ranges dos streams `__VB`.
Foram validados `2364` primitivas, `11820` streams, `11820/11820` ordinais,
`11814/11820` strides, `11820/11820` ranges válidos, `2362` primitivas
completas e `883/883` objetos com ranges não sobrepostos. O controle invertido
de slots deu `0`; o ordinal deslocado deu `1479`, preservado como ambiguidade
de layouts repetidos.

Status: `verified-bounded-original-primitive-stream-topology` e
`verified-independent-original-primitive-stream-topology-control`;
`original_semantics=not-claimed`. Nenhum binding de posição, geometria, UV,
material, atributo, skinning, rigging, animação, morph ou GPU foi inventado.
Readiness continua `NOT_READY` e renderização original continua `not-proven`.

Hashes: relatório `6A9CF9A6B92939552050C499261876724191D518DB1470E79CD7338F67A1BEE0`,
resumo `C10159DE7C3A7CC38EE8625E91D7D05679187092E534E51840B256899B0D4612`,
controle `4FFFE8E3C76D7C9389F44AD60C0F43EBBDC114D339C531012AC1B84BC837FA97`,
resumo do controle `777AA578F901AA94FEE0EC3307300C4A5DE9F6BF4E66F73BCFB9D675853479B9`,
readiness `7818A02C248A03351C6902C96EC8324BC755AA92FE70C35D1C7F4D36F051709A` e
DLL Release `C873228A606ED79CB02F79B0258748220FE22EAEB1ECE2E14C813A79E143D294`.

Validação: Release `0/0`, self-test `78/78`, Python `38/38`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos passaram. ISO
preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.50. Procurar evidência independente que discrimine os ordinais/layouts
repetidos e, somente se demonstrado, avance para atributos/geométrica/UV;
manter `not-proven` e não promover bindings.

## Etapa 0.50 concluída — controle negativo de comandos/payloads opacos

Foram adicionados o audit C# `eagl-original-primitive-stream-opaque-discrimination-audit`
e o controle independente `eagl_original_primitive_stream_opaque_discrimination_control.py`.
Eles comparam palavras brutas dos comandos de fetch do mesmo objeto EAGL com
registros/descritores `__VB` validados e com uma janela limitada de payload,
sem atribuir significado aos words.

Foram validados `388/388` pacotes, `883/883` objetos, `2364` primitivas,
`2362` fetches completos, `10240` candidatos ordinais e `10220` candidatos de
stride. O controle encontrou `0` matches por metadata, `0` por prefixo de
payload, `0` discriminações únicas e `1479` candidatos de stride deslocado;
`2364/2364` linhas permaneceram coerentes no controle independente. Portanto
este é um controle negativo verificado: ele não escolhe entre os layouts
repetidos e não autoriza nenhum binding.

Status: `verified-bounded-original-primitive-stream-opaque-discrimination`,
controle `verified-independent-original-primitive-stream-opaque-discrimination-control`,
`original_semantics=not-claimed`. Nenhuma semântica de posição, geometria, UV,
material, atributo, skinning, rigging, animação, morph ou GPU foi inventada.
Readiness continua `NOT_READY`; renderização original continua `not-proven`.

Hashes: relatório `845C2A9AA02ED9FBB6F2CC3DD73E143A7822735E19F3E1A65E55BC703F68007B`,
resumo `B857C8D31DFE2CF42A63E0AEA79AFA4697F0D1D86BC430AD8018757C2968BBC9`,
controle `00C438AB565BC6774E2E22F3929FB9F7F6B3B611C0FF59351E37F2766B1ACB5A`,
resumo do controle `496ED3A7CB97A1E4EB5E05F88102636FB5D3A86EF1002C05F399BA9CD2FFE1A8`,
readiness `C056B78C45EB75DAF0CEB31320F10E6FFAC852C6FC5301595E7CAD92748DF43D` e
DLL Release `8209763D4DD2AD6328309FCC5440495CCC04E0B3ED37603296DF9B4A5E85A959`.

Validação: Release `0/0`, self-test `79/79`, Python `39/39`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos passaram. ISO
preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.51. Buscar identidade independente de comando/payload ou semântica de
registro decodificada que discrimine os candidatos repetidos; não inventar
bindings enquanto isso não for demonstrado.

## Etapa 1.08 — intervalo estrutural do catálogo no tail de `__AnimationBank` — 2026-10-08

Implementados `Runtime/AnimationBankCatalogTailRangeAudit.cs` e o controle
independente `tools/analysis/animation_bank_catalog_tail_range_control.py`.
O audit validou 19/19 bancos e 1.584/1.584 entradas: tail/header, contagem
`2x`, intervalo, alinhamento, comprimento `8x` e relocations `2x` passaram em
19/19, sem intervalos sem relocations. Status fonte
`verified-structural-animation-bank-tail-range`; controle
`verified-independent-animation-bank-catalog-tail-range-control`.

É evidência estrutural apenas: não há binding provado de record/frame/pose/rig,
timing, skinning, morph ou seleção de animação. `candidate_record_binding` é
`not-proven`, `original_semantics` é `not-claimed`, e readiness permanece
`NOT_READY`; rendering, assets, áudio original e deformação continuam sem gate
semântico, enquanto gameplay segue clean-room authored.

Validação: Release `0/0`, self-test `94/94`, matriz código 0, 10 cenários,
777 frames, 54 eventos de áudio, 1.554 amostras de animação, smokes, runtime
headless/windowed e replays determinísticos. ISO preservada: 2.013.003.776
bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.

Próximo trabalho: evidência independente para estado → draw/UV/material,
`EVT→ABKC→sample→playback` e timing/pose/rig/deformação. Não declarar o
projeto concluído, não inventar bindings, não executar/recompilar XBE e não
iniciar code review.

## Etapa 1.09 — relocations dos pares no tail de `__AnimationBank` — 2026-10-08

Implementados `Runtime/AnimationBankTailPairRelocationAudit.cs` e
`tools/analysis/animation_bank_tail_pair_relocation_control.py`. O audit usa
ISO somente leitura, `.data`, ELFs e catálogos de animação delimitados; não
executa nem recompila o XBE.

Foram validados 19/19 bancos e 1.584/1.584 pares: duas relocations por par
nos offsets esperados, zero multiplicidades inesperadas, words de ambos os
lados dentro de `.data` em todas as linhas, zero matches com símbolos
conhecidos, 1.584 pares raw distintos e 19/19 layouts completos. O detalhe
tem 1.584 linhas. Status fonte:
`verified-structural-animation-bank-tail-pair-relocations`; controle:
`verified-independent-animation-bank-tail-pair-relocation-control`.

É evidência estrutural apenas. Não existe join provado ordinal → nome do
catálogo, nem binding de record/frame/pose/rig/timing/skinning/morph/playback;
`candidate_record_binding=not-proven` e `original_semantics=not-claimed`.
Readiness permanece `NOT_READY`; rendering, assets, áudio original e
deformação continuam sem gate semântico, enquanto gameplay segue clean-room
authored.

Validação: Release `0/0`, self-test `95/95`, matriz código 0, 10 cenários,
777 frames, 54 eventos de áudio, 1.554 amostras de animação, smokes, runtime
headless/windowed e replays determinísticos. ISO preservada: 2.013.003.776
bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo trabalho: só usar os target-prefixes/relocations quando houver join
independente demonstrável; continuar estado → draw/UV/material,
`EVT→ABKC→sample→playback` e timing/pose/rig/deformação. Não concluir, não
inventar bindings, não executar/recompilar XBE e não iniciar code review.

## Etapa 1.10 — janelas de relocation dos targets do tail — 2026-10-08

Implementados `Runtime/AnimationBankTailPairTargetChainAudit.cs` e
`tools/analysis/animation_bank_tail_pair_target_chain_control.py`. O audit
observa janelas de 32 bytes nos dois targets raw de cada par, sem interpretar
o conteúdo ou executar/recompilar o XBE.

Foram validados 19/19 bancos, 1.584/1.584 linhas e 1.584/1.584 pares com
janelas não vazias. As multiplicidades foram first `2:1, 3:17, 4:1554, 5:12`
e second `3:328, 4:1237, 6:19`; zero linhas tiveram exatamente uma relocation
em cada janela, zero segundo nível foi resolvido e zero símbolo conhecido foi
encontrado. Status fonte:
`verified-structural-animation-bank-tail-pair-target-chain`; controle:
`verified-independent-animation-bank-tail-pair-target-chain-control`.

Essa regularidade é estrutural apenas: readiness segue `NOT_READY`,
`candidate_record_binding=not-proven` e `original_semantics=not-claimed`;
assets, rendering, áudio original e deformação continuam sem gate semântico.

Validação: Release `0/0`, self-test `96/96`, matriz código 0, 10 cenários,
777 frames, 54 eventos de áudio, 1.554 amostras de animação, smokes, runtime
headless/windowed e replays determinísticos. ISO preservada: 2.013.003.776
bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo trabalho: evidência independente de record/frame/pose/rig/timing,
estado → draw/UV/material e `EVT→ABKC→sample→playback`. Não concluir, não
inventar bindings, não executar/recompilar XBE e não iniciar code review.

## Etapa 1.11 — tabela de dispatch EAGL por ID observável — 2026-10-09

Foi adicionada análise estática do XBE Original Xbox com apoio do parser e
disassembler de XboxRecomp (sem recompilador, runtime ou execução). O prefixo
da tabela EAGL em `0x0031A930` tem 42 slots executáveis/39 destinos. As 2.364
streams completas contêm 36.779 comandos e 12 IDs; os 12/12 apontam
estruturalmente a slots executáveis. A interpretação de handler/comando segue
não decodificada, `semantic_binding=not-proven` e
`original_semantics=not-claimed`.

O novo controle está em `tools/analysis/eagl_original_render_command_dispatch_control.py`;
os relatórios são `eagl-original-render-command-dispatch-control.tsv` e
`eagl-original-render-command-dispatch-control-summary.tsv`. O subgate é
estrutural apenas; rendering permanece `not-proven`, assets
`candidate-integrated`, áudio `partially-integrated`, deformação
`structural-only`, gameplay `clean-room-integrated`, readiness `NOT_READY`.

Release `0/0`, self-test `96/96`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram. Cobertura authored: 10 cenários, 777
frames, 54 eventos, 1.554 amostras. ISO somente leitura:
`2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

XboxRecomp é útil como ferramenta auxiliar de análise estática, não como
substituto do runtime .NET. Veja [repositório](https://github.com/sp00nznet/xboxrecomp)
e [guia estático](https://github.com/sp00nznet/xboxrecomp/blob/main/docs/DECOMP.md).


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




