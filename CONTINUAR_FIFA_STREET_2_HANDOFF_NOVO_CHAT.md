# FIFA Street 2 — handoff para novo chat

## Etapa 1.07 — controle independente de nomes EVT em todos os assets de áudio — 2026-10-08

`Runtime/AudioEventNameAssetTextSearchAudit.cs` adiciona `audio-event-name-asset-text-search-audit`, integrado à matriz, ao self-test e ao `ProjectReadinessAudit`. Foram reparseados `17` nomes do índice textual de `eventspa.evt` e buscados como ASCII exato nos `60` assets de `data/audio` fora do EVT: `60/60` válidos, `1.737.085.606` bytes, `0` pares asset/nome com hit e `0` ocorrências.

`tools/analysis/audio_event_name_asset_text_search_control.py` reparseia independentemente as `1.020` linhas do produto cartesiano asset/nome e confirma `1.020/1.020` linhas válidas, `1.020/1.020` sem hit e `0` duplicidades. O resultado é controle negativo estrutural; não seleciona ABKC/AST/BNK, sample, evento ou playback. `semantic_binding=not-proven`, `direct_selection_binding=not-proven`, `original_semantics=not-claimed`; readiness permanece `NOT_READY`.

Validação fechada: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`; Release `0/0`, self-test `93/93`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras. Readiness segue `NOT_READY`, com assets `candidate-integrated`, rendering `not-proven`, gameplay `clean-room-integrated`, áudio `partially-integrated` e animação-deformação `structural-only`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`; XBE não executado nem recompilado.

Hashes SHA-256: auditor `439D5AB62558278E5CC80CA41AB7CAED8EC7C6DC86ED46EF518363E0E3FB20F5`; controle Python `4BB18D4534BFD5E57F568DAA0627CACDF0F2AA9DE7CEBD46AB61C1C03552F90C`; relatório `9B8995864273ADF880CD02BFA2E10F5F08BA8C9F44B8D398D33F1284390A1ADD`; summary `04EF71651802C2F093C53312683264E35425BA699BD8A62711657D3D57035BF2`; controle `B745AED8D07629B5000183DE9B35C3B59544193422AD1B73825265C4B4351961`; controle summary `886FCD7A7E36E23D56D175D4D111F7C0E38F74708525A79ECACF03397D860174`; readiness `23FC205E7488ECBFCB2F0FF0A5DE25A5F13EAA879E8AFACFD5EAE262348F0FF2`.

Próxima investigação: `EVT→ABKC→sample→playback`, `state→draw/UV/material` ou timing/pose/rig/deformação, apenas com evidência discriminativa.

## Etapa 1.06 — controle independente state/fetch + slot numérico + textura/raster candidata — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_state_slot1_candidate_uv_ambiguity_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. A reanálise independente cobre `2364/2364` primitivas globais e, no subconjunto candidato, `218` grupos (`216` completos, `2` incompletos), `180` estados SHAPENAME (`178` completos). Os `178/178` grupos SHAPENAME completos têm o perfil near-unit observado no slot `1` e `0` em outros slots; textura/atributo e influência raster candidatas são `24/24`; vínculos únicos estado→geometria: `0`.

O resultado é controle estrutural negativo: não nomeia o slot `1` como UV, não prova state→draw/material e não promove textura/raster candidata para semântica original. O subgate `rendering-original-state-slot1-candidate-uv-ambiguity-control` está `verified-structural-negative`, `24/24`, com `semantic_binding=not-proven`, `original_semantics=not-claimed`; o projeto segue `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `92/92`, smokes, headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256: controle `FCFF267936F6B03FD94174600D443424CEDD399E9634B253DD20E511D25B6449`; relatório `A61381C76D19CCCFBD995EBB928770B975F0EF923A383F8192ADCADC36F6ACE1`; summary `BFAF6A5AF309D92E0CAB5BBABB6270BA0D6326E3E516914E8C3B5079BFBD5C17`; `ProjectReadinessAudit.cs` `20099B92DC1BCA0F58F577A1A8EF7ADB3BE9F2BFDBD172F04A09B0EC774DE363`; matriz `CD626E7E0D90FC4F8BEE016C20978E6F69CE69BB7D0DF12F704609DC2853BEDC`.

Próximo avanço: evidência original única `state→draw` com UV/material ou `EVT→ABKC→sample→playback`, sem inventar bindings.

## Etapa 1.05 — controle independente de ambiguidade nominal FIFAST2.csi↔EVT — 2026-10-08

Foi adicionado `tools/analysis/fifast2_csi_evt_name_ambiguity_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. A reanálise independente dos `100` nomes de `FIFAST2.csi` e `17` candidatos nomeados de `eventspa.evt` encontrou `100/100` linhas CSI e `17/17` EVT válidas, `17` nomes EVT únicos; `10` EVT sem sobreposição, `3` com uma sobreposição, `4` ambíguos e `7` com alguma sobreposição. Não houve nome exato e `0` bindings únicos provados.

O resultado é controle de ambiguidade nominal, não binding: não atribui significado ao CSI, não escolhe ABKC/AST/BNK ou amostra, não conecta gameplay a playback e não promove `Goal`, `GB`, `Tackle` ou `Panna` por texto. O subgate `audio-fifast2-csi-evt-name-ambiguity-control` está `verified-structural-negative`, `17/17`, com `semantic_binding=not-proven`, `direct_selection_binding=not-proven` e `original_semantics=not-claimed`; o projeto segue `NOT_READY`.

Validação: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `92/92`, smokes, runtime headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256: controle `FA75AD1D77492032A54992A8611A5DDF7D75DB046EC2D82948D417653E88A942`; relatório `E334171FE7B0BC9A417895C8424A6FC132332BDDF5AA051132A1D20B8C059098`; summary `0C23206306F0959DE7AA035F91F838074E8F278E614C71F6D3F2F9292CC7444F`; `ProjectReadinessAudit.cs` `3366D1C53283818812B2CE3119339CE40D274C3A28CE186FB14893C4B3B685C2`; matriz `97D982C73A045366B774750550370E76357A7247C0ADD692068381D2DFD55A46`.

Próximo avanço: evidência original discriminativa `state→draw` com UV/material independente ou `EVT→ABKC→sample→playback`, sem promover sobreposição nominal.

## Etapa 1.04 — coocorrência independente command-descriptor e papéis numéricos — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_command_slot_role_cooccurrence_control.py`. O controle une `2364/2364` primitivas do command-descriptor às `11820` linhas de papéis numéricos: `2364/2364` ordinais atuais estão presentes, coincidentes e únicos, sem matches deslocados; há `2362` grupos completos e `2` incompletos, com perfil float2 de faixa unitária no slot `1` em `2362/2362` grupos completos e nenhum em outro slot.

Isto permanece uma coocorrência estrutural, sem atribuição de slot a UV, posição, material, atributo, geometria, draw ou GPU, nem semântica de skinning, rigging, animação ou morph. O subgate `rendering-original-command-slot-role-cooccurrence-control` está `verified-structural-negative`, `2362/2362`; `semantic_binding=not-proven`, `original_semantics=not-claimed`, e o projeto segue `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `92/92`, smokes, headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256: controle `CEB60207BDEE7450D2C77A13B9BD0DF09047CAA7AFA81B4810023A97E575FA512`; relatório `5ABFB9D19F0E031B49CF8F62A0179A0CDD8D0E740290BCAEAA399883363D5CB3`; summary `6ACCCC0125CE33344EA73C98576C8BFB2A043FB5683BB6C4D9C71A81762CF6C`; `ProjectReadinessAudit.cs` `C7334F270FC4C846B570C2D00D710D76C4D375C670DF5879E8B2B5EF2592E2EA`; matriz `33143D52276B6C78094934E72CC56BD15C013B80C82F763114B15BEA7809FE6C`.

Próximo avanço: evidência original discriminativa state→draw/UV/material ou EVT→ABKC→sample→playback, sem promoção por coocorrência.

## Etapa 1.03 — coocorrência independente estado/fetch e papéis numéricos por primitiva — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_state_numeric_slot_cooccurrence_control.py`, integrado à matriz e ao `ProjectReadinessAudit`. A junção independente do estado/fetch original com os papéis numéricos fechou `2364/2364` linhas, `2364` grupos, `2362` grupos completos de cinco slots e preservou `2` incompletos. Nos grupos completos, `2362/2362` têm único perfil float2 de faixa unitária no slot bruto `1`, `0` têm esse perfil em outro slot; `752` linhas têm `SHAPENAME` e `1612` têm `SetPrimitiveType`.

O resultado é coocorrência estrutural negativa, não associação estado→draw/geométrica/UV/material/GPU nem semântica de skinning, rigging, animação ou morph. O subgate `rendering-original-state-numeric-slot-cooccurrence-control` está `verified-structural-negative`, `2362/2362`; `semantic_binding=not-proven`, `original_semantics=not-claimed`, e o projeto continua `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `92/92`, smokes, headless/windowed, replays determinísticos e ISO verificada; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Próximo avanço: evidência discriminativa state→draw/UV/material ou EVT→ABKC→sample→playback, sem promoção por coocorrência.

## Etapa 1.02 — discriminação independente de papéis numéricos por slot de primitiva — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_numeric_slot_role_discrimination_control.py`, integrado à matriz e ao `ProjectReadinessAudit`. A junção independente dos relatórios originais numérico/topológico fechou `11820/11820` linhas, `2364` grupos, `2362` grupos completos de cinco slots e `2` casos incompletos explicitamente preservados. Em todos os `2362` grupos completos há exatamente um perfil float2 de faixa unitária no slot bruto `1`, nenhum grupo completo o apresenta em outro slot; o controle de bounds tem `2361` matches no slot `0` e `1` sem match. `semantic_binding=not-proven` e `original_semantics=not-claimed`.

O subgate `rendering-original-numeric-slot-role-discrimination-control` está `verified-structural`, `2362/2362`, mas não promove UV, posição, material, atributo, geometria, skinning, rigging, animação, morph ou GPU. Assets/rendering/áudio/animação continuam não passados; gameplay clean-room permanece passado e o projeto segue `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, headless/windowed, replays determinísticos e hash da ISO; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Próximo avanço: evidência independente state→draw/UV/material ou EVT→ABKC→sample→playback, sem inventar bindings.

## Etapa 1.01 — controle independente do universo completo de dados para hashes EVT — 2026-10-08

Foi implementado `fifa_pc/Runtime/AudioEventDataAssetHashSearchAudit.cs` e integrado o comando `audio-event-data-asset-hash-universe-audit` à matriz, ao self-test e ao `ProjectReadinessAudit`. A varredura cobre todos os assets em `data`, exclui somente `data/audio/eventspa.evt` e reparseia os 17 candidatos como palavras little-endian alinhadas: `121/121` assets válidos, `2.008.984.464` bytes, uma linha-fonte excluída e um único hit em `data/movies/advoffn.vp6`, `0x34214F6B`, `3panna_goal`, offset `0xA58D88`.

`tools/analysis/audio_event_data_asset_hash_universe_control.py` confirma independentemente os `121/121` caminhos, bytes e o hit único. Como o hit está dentro de payload comprimido VP6 de filme, ele é somente evidência estrutural e não prova áudio, ABKC, sample, evento ou playback. Mantêm-se `direct_selection_binding=not-proven` e `original_semantics=not-claimed`; nenhum binding foi promovido. Os dois subgates do universo de dados estão `verified-structural`, `121/121`; os gates principais não resolvidos permanecem não passados e o projeto continua `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `92/92`, smokes relevantes, headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Sem execução ou recompilação do XBE.

Próxima etapa: buscar evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings e sem marcar o projeto como concluído.

## Etapa 1.00 — controle independente do universo completo de assets de áudio — 2026-10-08

Foi implementado `fifa_pc/Runtime/AudioEventAudioAssetHashSearchAudit.cs` e integrado o comando `audio-event-audio-asset-hash-universe-audit` à matriz, ao self-test e ao `ProjectReadinessAudit`. O controle reparseia os 17 candidatos derivados do índice textual de `eventspa.evt` como palavras little-endian alinhadas e varre todos os assets em `data/audio`, excluindo somente o EVT-fonte: `60/60` assets válidos, `1.737.085.606` bytes, `0` assets com hits, `0` pares asset/hash e `0` ocorrências alinhadas. Relatórios: `fifa_pc/audio-event-audio-asset-hash-universe.tsv` e `fifa_pc/audio-event-audio-asset-hash-universe-summary.tsv`.

O subgate `audio-event-audio-asset-hash-universe-control` passou como `verified-structural-negative`, `60/60`. É um controle negativo estrutural independente sobre o universo de assets; não seleciona ABKC/AST/BNK/sample, não estabelece evento de gameplay ou playback e não promove semântica. `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Os gates principais permanecem assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` não passado, animação-deformação `structural-only` não passada e o projeto `NOT_READY`.

Validação: matriz completa exit code `0`, build Release `0/0`, self-test `91/91`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. SHA-256: `AudioEventAudioAssetHashSearchAudit.cs` `9C0E3E6B34974BF2EB6BF04096CEE540934307A0788DF2AEFFC1FEFFCAF9FE05`, `Program.cs` `766DC833320707B891A75A559D337F2A2DF3BCD5270B1569C7C1DB3840B38529`, `SelfTestRunner.cs` `D13FA778813CCB60BB0122C0043F6B74954FE95348B526950867477EC089014F`, `ProjectReadinessAudit.cs` `5190DDECF723EBA9085FC8B4674414539F32C4630359E522727F3736E9B1DFF5`, matriz `D188A79CAA07E6299E5FCAA1268F6972B822ABE840128CB43D9A15C2632D6D90`, relatório `F5E9D9DF191173DAFEB5BC05D69FFA1075E18CF24CBAE194BACC85D730837002`, summary `DE86B53F51D842D1DD15799FC535CF4F1D2BD337D512786014850F9957C45DA2` e readiness `55DBF81BD71989DE4BD55F6249045C46EBD8C3BE6FD2DC01A8D47503F87C48AA`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: obter evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings, sem executar/recompilar o XBE e sem marcar o projeto como concluído.

## Etapa 0.99 — discriminação independente EVT→PCP — 2026-10-08

O novo `tools/analysis/audio_event_evt_pcp_range_discrimination_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. A reparse independente reconcilia `17/17` nomes do `eventspa.evt` com ranges únicos de `pcpahdr/pcpadat`, cobrindo `17/17` âncoras de string, ranges de cabeçalho/dados, índices paralelos, prefixos `SCHl`, nomes, offsets e linhas de cabeçalho; o join de runtime fecha `17/17` com `0` ausentes. As buscas raw de 32 bits continuam negativas para ABK, `pcpahdr` e `pcpadat`.

O subgate `audio-event-evt-pcp-range-discrimination-control` passou como `verified-structural-negative`, `17/17`. É evidência EVT→range PCP somente: não seleciona ABKC/AST/sample, não estabelece evento de gameplay ou playback, mantém `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. O gate de áudio segue `partially-integrated` não passado; assets/rendering/animação-deformação continuam abertos, gameplay clean-room verificado e o projeto `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `90/90`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. Hashes: controle `6679AC5C2AA0118F8B54D5E51AFFDBEA4446310A75C891D75845FBB960667B0F`, `ProjectReadinessAudit.cs` `0F86927682D97D111B0FE7E74B2A977EA3F7944830D3BBED1E3ECA027A1F948A`, matriz `6375B68D2D377A4CB00D233D1DE6E52C5A6EF95B4E5139324C78E972AEB64E91`, relatório `FD6775F92E5A2FD1C3B065527CA1700EC943D67C299FBE6D4329DA8286E15BD2`, summary `9BD952F8207419933C68BE4BD53A2DC4006A549E637CEDF2947BCE7E7413CE86`, readiness `A270A70B1E9AB64775E5D856460E640F8A7C94EA4C5D1B8B3A08379E0CC516A5`. ISO somente leitura: `2013003776` bytes, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: obter evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings e sem executar/recompilar o XBE.

## Etapa 0.98 — identidade independente do payload de geometria candidato — 2026-10-08

O audit original de topologia agora calcula `payload_sha256` nos intervalos `__VB` validados. O novo `tools/analysis/eagl_original_candidate_geometry_payload_identity_control.py` faz reparse independente e junta `38` grupos de geometria aos cinco slots originais, à associação de fetch e à identidade raw: `38/38` grupos completos e `190/190` símbolos, metadados e hashes de payload exatos. Status `verified-independent-original-candidate-geometry-payload-identity-control`, `original_semantics=not-claimed`.

O subgate `rendering-original-candidate-geometry-payload-identity-control` passou como `verified-structural-negative`, `38/38`; é identidade de bytes e estrutura apenas, sem promoção a posição, UV, material, skinning, rigging, animação, morph, draw ou GPU. Gates principais: assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room verificado, áudio `partially-integrated` não passado, animação-deformação `structural-only` não passado e projeto `NOT_READY`.

Validação: matriz completa exit code `0`, Release `0/0`, self-test `90/90`, smokes, runtimes headless/windowed e replays; `10` cenários, `777` frames, `54` eventos e `1554` samples. Hashes SHA-256: controle `122CD41BEA4F936FDA11A5B504794FA06AF61FDB94A61C7FCD6D5E50A9397830`, auditor de topologia `A519959A04EB908D011ACEE9A803609C39C08174123184603BF9525E8142A379`, `ProjectReadinessAudit.cs` `6CBDE0D1E624161EC5CF6D7B393A8DBCA1C8CC8DB34AF6872F14019F6E14259A`, matriz `FBE00A4CABEBDD8E3322A734095410B4244C1955AE3131C04C41F347EC4DE437`, relatório `C2674850F4CFA7D8A173D28BE87F848893CB9A99ADEFDBE8966E8324CCDC687A`, summary `49ED6FC5AADF625DDF20266083F2FADE7AAC28E8673C1D71166B8FE8D88C6CE1` e readiness `439467451E3F16ABA72F7CEDBB5FBC3CB4409D2C06EA4592E111C5D2593702FF`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência discriminativa state→draw/UV/material, EVT→ABKC→sample→playback ou timing/pose/rig/deformação, sem inventar bindings e sem executar/recompilar o XBE.

## Etapa 0.97 — controle independente da discriminação do perfil estado→geometria — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_state_profile_discrimination_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O reparse independente dos relatórios de perfil e adjacência, com deduplicação da identidade `(model_archive, object_name, state_layer, state_primitive)`, cobriu `269` linhas em `24` objetos, `38` candidatos de geometria e `180` primitivas de estado; `27` assinaturas distintas e `269/269` contagens de tie foram reproduzidas.

O controle é negativo, mas discriminativo: `89/180` primitivas de estado têm múltiplos candidatos de geometria; `91/180` ficam em objetos com uma única geometria, sem competição; `0` têm zero candidatos; `15` grupos de assinatura têm múltiplos candidatos; `0` pares de command shape exatos e `0` bindings únicos estado→geometria foram provados. Status `verified-independent-original-state-profile-discrimination-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Relatórios: `fifa_pc/eagl-original-state-profile-discrimination-control.tsv` e summary.

O readiness registra `rendering-original-state-profile-discrimination-control` como `verified-structural-negative`, `24/24`, sem promover perfil de estado ou objeto de geometria única a state→draw, UV, material, comando ou GPU. O gate principal de rendering permanece `not-proven`; assets continuam `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e o projeto `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes SHA-256: script `10BF27EB1D15145BF046D3B9DA2E35F007E9B24B091DCB57D963FAD7E6A95436`, relatório `58C5C2F672F775F36EA1A5FC7D783664AF5D3AB9593DACFB0BC77DC93D3DE0C8`, summary `899DDE1BB50ED1E09F3559CB7CCAB901131F56CA52F1150D57AE817C959D8AFB`, matriz `740D370CF96A842C64F90EC55C7F3DF97BDE409E584A3FFEFBE6D06CCD250223`, audit `A91895863CBA11E793EBBFC1C7DE82EC1ED6B27351CBA6B60C0639B571015D08`, readiness `70A663983FAFEBB5EB12ED742A0F17A5CA8C8F4CACBAD2F036962012C59724B7`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar com evidência discriminativa de EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação, sem inventar bindings.

## Etapa 0.96 — controle independente da seleção do decoder de bancos de áudio — 2026-10-08

Foi adicionado `tools/analysis/audio_bank_decoder_selection_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. A junção independente entre o decode em lote e a enumeração exaustiva ABKC/AST/BNK reconciliou `16/16` bancos: `15/15` candidatos aceitos bateram em identidade, tipo e bytes de onda, e `1/1` banco rejeitado bateu na política de rejeição. Status `verified-independent-audio-bank-decoder-selection-control`, `direct_selection_binding=not-proven`, `original_semantics=not-claimed`.

É somente uma confirmação estrutural do decoder gerenciado. Não prova seletor original de EVT, sample, evento ou playback. O readiness registra `audio-bank-decoder-selection-control` como `verified-structural-negative`, `16/16`, sem fechar o gate principal de áudio; o projeto permanece `NOT_READY` com assets `candidate-integrated`, rendering `not-proven`, áudio `partially-integrated`, animação-deformação `structural-only` e gameplay clean-room verificado.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. SHA-256: auditor `05E354332F47DBFF208E8F4889131A93736B6BCC184F6DDD99229F7DC968B903`, relatório `33E78F74BC593BB05C13DAFDFAA0598D3CB5D28AB4A880FC8E35697C344BA862`, summary `97EBA29D401FFA85DFEC948B31ACA166176A53E64D95441E8A8A279C4C3488B6`, matriz `0EBB914E3422067085AA3F4EB80517B774F1CE427E0048407C14EEB8FECF908E`, audit `011E2C23F5DE7360A78345F635ABF46F8A89BE4E5599B5B90DF50B81F796A2FA`, readiness `EF7275DE13DFFC595934E5BB3D95C66CF6ED3E3AD988F237737F6A53110F67EE`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência discriminativa EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação, sem promover bindings não provados.

## Etapa 0.95 — controle independente da ambiguidade estado→material candidato — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_state_candidate_material_ambiguity_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. O reparse independente cruzou intervalos de estado originais, `SHAPENAME`, fontes candidatas de textura e cardinalidade de geometria adjacente: `24` objetos, `218` linhas de renderização, `62` de ordem, `180` de estado e `38` de geometria.

Foram encontrados `36` intervalos de estado não vazios; `12` têm dois candidatos adjacentes, `24` têm um e `0` têm zero. As validações fecharam em `180/180` junções de estado, `180/180` matches de token, `180/180` fontes candidatas, `24/24` assets válidos e `24/24` controles completos. Bindings únicos estado→geometria e estado→material: `0`; adjacência, `SHAPENAME` e textura candidata permanecem evidência estrutural/negativa, não semântica original. Status `verified-independent-original-state-candidate-material-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`.

O readiness registra `rendering-original-state-candidate-material-ambiguity-control` como `verified-structural-negative`, `24/24`; o gate principal continua `rendering=not-proven`. Assets continuam `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e o status permanece `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa `completed-authored-and-candidate-opt-in` exit code `0`, smokes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. SHA-256: script `FDF5A04EF7EBA97DAF7E4D093AEE0E14602AC561090B4B2A67498DE2D9DDA9DB`, relatório `3A596D0D251D8B24995EFE167881F5D1EE2FEC80491AC010DFB9DA35EFA1F832`, summary `A4569B1BD91FA455FF233B0E1931D0B76C8262A5B3A51B1386EA094E85EDD289`, matriz `625910D5E524C3A82E9379E4CB12D26EA4F17CE243C103D892B059E288FAA147`, audit `1C316A8C86E7E111DC02D911ACA21C1DC7E81522BE8685257CFF0D5AF0F79A85`, readiness `4A747F9A8F5C0D8286F7CED075C0535A7FB978CBC9D32E7E781D70E37CB1FEB9`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar somente com evidência discriminativa de UV/material, EVT→ABKC→sample→playback ou timing/pose/rig/deformação; não inventar bindings.

## Etapa 0.94 — controle independente da classificação original geometria/draw — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_geometry_draw_classification_control.py`, integrado à matriz e ao `ProjectReadinessAudit`. O reparse independente juntou, por chave de primitiva, `218/218` linhas do `eagl-original-render-chain.tsv` a `218/218` linhas do `eagl-candidate-fetch-association.tsv`, sem chaves faltantes, cobrindo `24/24` objetos candidatos e `24/24` classificações de objeto completas.

A classificação coincidiu em `218/218`: `38` linhas de geometria e `180` linhas de estado nas duas fontes; os `24/24` objetos têm mais estados que geometrias. `state_to_geometry_unique_binding_proven_rows=0`. Status `verified-independent-original-geometry-draw-classification-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. É evidência estrutural de classificação apenas; não prova state-run→draw, UV, material, sampling, GPU, skinning, rigging, animação ou morph.

O readiness registra `rendering-original-geometry-draw-classification` como `verified-structural`, `218/218`, sem fechar rendering. Os gates principais continuam abertos: assets `candidate-integrated`, rendering `not-proven`, áudio `partially-integrated` e animação-deformação `structural-only`; gameplay clean-room está verificado e o status permanece `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes: auditor `2A7EDF85FEA1C982429A448294A3B40140CB5C557452349054ABA0CD88D3B7CE`, relatório `4FD523C19F725825862F8DFCCF0AFA626CF60BB3790046FC5EE89D2FC1AF6B6E`, summary `69ECC533AB16C9573B8EB0B18EF6027279DAA3AB28BA3BBCB880D0D080D00C94`, matriz `950D951607B618C2CB59C5005E191E9A3B05FB47BF430CA4B9B2795280553639`, agregador `032C73872F08EAF7916CFD8571CD59C13061BBF66E0FEB0DFFC8C0BEA63E713B`, readiness `0DABB2380CF5EF99F304FE696F7426DDC012884049AB9A4398D64543990BFA85`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.93 — controle independente da ambiguidade pose-bank→animation-bank — 2026-10-08

Foi adicionado `tools/analysis/animation_morph_pose_bank_ambiguity_control.py` e o controle foi integrado à matriz. Ele reconcilia o índice de pose-bank, os perfis ELF de animation-bank e o audit de relocations de morph: `391` linhas de pose-bank, `19/19` animation-banks ELF-válidos com metadados de símbolo e `393/393` layouts de morph parse-válidos; `159` layouts contêm pares de offsets, sem conjunto ou histograma de gaps exatamente correspondente a uma identidade de animation-bank.

Nenhuma semântica foi promovida: existe apenas `1` nome normalizado genérico (`PosePaletteBank`), nenhuma correspondência de nome com animation-bank, nenhuma correspondência de metadado de símbolo e `0` bindings pose-bank→animation-bank únicos provados. Status `verified-independent-animation-pose-bank-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`; não há prova de frame, pose, rig, timing, skinning ou morph original. O readiness registra `animation-morph-pose-bank-ambiguity-control` como `verified-structural-negative`, `391/391`, sem fechar o gate principal de animação-deformação.

Validação: Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes: auditor `6D06722268A77D919C93E698BBECE14A4D02E30337F3365D46004E8DCDE0297E`, relatório `007E51BF59267432C846B801256C0A356511EDEC5993AFB87362361454106A99`, summary `1CA4359A5B13236426FC93642B7B7F6D69D43A1D35ECB0EB738E2FFDED61CDA8`, matriz `1387E6D9723E3DFC2A04C0825698F640A95FE2A34F744D0F4602B962424F4638`, agregador `DA8118AC61A5217BDED8935AAE586689FDF7878612F3F02936114603B7EE5FB1`, readiness `CB8DF38A2ECA8FFEF203BD488E60794B3DFB285321876B2E778F90ABCD3C42E7`. Readiness permanece `NOT_READY`; gameplay clean-room está verificado e os gates de assets/rendering/áudio/animação-deformação seguem abertos. ISO somente leitura, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.92 — controle independente da ambiguidade estado→geometria — 2026-10-08

Foi adicionado `tools/analysis/eagl_original_state_geometry_binding_ambiguity_control.py` e o controle foi integrado à matriz para reconciliar, sem atribuir semântica, adjacência, perfis, ordem e partições. Resultado: `24` objetos unidos, `269` perfis, `62` intervalos ordenados e `24` partições, sem identidades faltantes; cardinalidades de perfil/ordem e conjuntos de índices de estado reconciliados em `24/24`. Todos os `24/24` objetos têm mais estados que geometrias.

Nenhuma promoção sem evidência foi feita: `geometry_group_state_exact=0`, `exact_command_shape=0` e binding estado→geometria único provado `0/24`. Status `verified-independent-original-state-geometry-binding-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`; o resultado não prova estado→draw, UV, material ou GPU originais. Relatórios: `fifa_pc/eagl-original-state-geometry-binding-ambiguity-control.tsv` e summary. O agregador `project-readiness.tsv` registra o controle como `verified-structural-negative`, `24/24`, sem fechar o gate principal.

Validação: Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes: auditor `4038AACD60D9D5210E96A9F0EA6DFAF1D5F1D4579683C480AED36BDE3F08E2EB`, agregador `E68D78D5411AAA9DEF72E1047D1F9D5FAB68E91A5E388258A93BF4129991FD6F`, matriz `B078D5F079CBC48E6231BB2DCE7D67ADFA566A53D18E6D56A406AFEAD224BC1A`, relatório `B02761A085DBD5C7996BF40F149B097AA8AC130B77D4A8DE88621870A15AF0FF`, summary `7C60550132683EF27301AC355D40FF317235332F78F376328FDB31677B453206`, readiness `BB120BFB808DF9B0635DB320CCC120180E1E0C658BD24963C88756A74FE387B4`. Readiness permanece `NOT_READY`; gameplay clean-room está verificado e os gates de assets/rendering/áudio/animação-deformação seguem abertos. ISO somente leitura, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.91 — controle de influência raster isolada para casos de composição — 2026-10-08

`CandidateRenderInfluenceAudit` agora usa também uma comparação raster isolada
do objeto candidato quando a composição da cena completa oculta a diferença.
O critério continua restrito ao renderer candidato gerenciado e não promove
semântica original.

Resultado: `24/24` objetos com geometria finita/indexada e influência de textura
candidata no raster, incluindo `24/24` na comparação isolada; `20/24` diferiram
na cena completa. Foram obtidos `24` checksums texturizados e `24` sólidos
distintos. Status `verified-authored`; `original_semantics=not-claimed`.
Não há identificação de UV, material, sampling, estado, draw ou GPU originais.

O subgate `assets-candidate-attribute-raster-influence` continua
`verified-candidate`, `passed=True`, `observed=24`, `expected=24`. Os gates
principais permanecem `assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`, todos
sem fechamento; gameplay clean-room segue verificado e o status é `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes relevantes, runtime headless/windowed e replays determinísticos; `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples. ISO somente
leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Hashes: relatório `EA487AC1B69906BEF0121B19588643814E118493B3A7ED8094DA134C065AFF14`,
auditor `4C1D43BF7157B782EE56290EA88C71428D37066A5CBA3D2494476E04BBCF12ED`,
`Program.cs` `C0C06AEC1147F9FA8AA699E649551FFD1A99CCC513576AE3F4FCA286AF21236F`,
readiness `D2704B5697D2B8EFBF2F2825CE1DC0A49E36089EED77227E7DD862A8CD3811A1`
e DLL Release `82DC30C3751939707D9B356D6BEDB3141A3B3DB3C40F5C2242D8BA0B7ACA25DA`.

## Etapa 0.90 — auditoria dinâmica de influência do atributo candidato — 2026-10-08

Foi implementada `CandidateAttributeRasterInfluenceAudit` em
`fifa_pc/Runtime/CandidateAttributeRasterInfluenceAudit.cs`, com o comando
`eagl-candidate-attribute-raster-influence-audit` integrado à matriz. O
controle renderiza, no mesmo caminho candidato e com o mesmo mesh/textura, um
baseline e uma variante que transforma deterministicamente o atributo opaco
slot 1 de `(x,y)` em `(1-x,y)`.

Nos `24` objetos same-trunk, houve `24/24` geometrias finitas/indexadas,
`24/24` cardinalidades válidas, `24/24` mudanças de raster e `24` checksums
baseline/`24` perturbados distintos. Status:
`verified-candidate-attribute-raster-influence`; `original_semantics=not-claimed`.
É evidência dinâmica somente do caminho gerenciado candidato: slot 1 não foi
chamado de UV, nem recebeu semântica de material/sampling, estado, draw ou GPU.

Readiness registrou `assets-candidate-attribute-raster-influence` como
`verified-candidate`, `passed=True`, `observed=24`, `expected=24`. Os gates
principais continuam não fechados (`assets=candidate-integrated`,
`rendering=not-proven`, `audio=partially-integrated`,
`animation-deformation=structural-only`), e o status permanece `NOT_READY`;
gameplay clean-room segue verificado.

Hashes: relatório
`67F2B35CEA616716987E59442C25DF5C641D3A0CA47376E7892CC711EFA1DC47`,
auditor `08FC231178A1A7393442EC8A29884E9AADAFB2BCC5680B20B212E142C8620546`,
readiness `3D043101C88871F40E1EEE8B3B7707BC3311BE15174AB584FF570A4114449CF1`,
`Program.cs` `69ECDD321F8243981A32DBB2E9D7D3BF3CE9096656155BFEC9E9A07B78E00358`,
matriz `3F8CC844430215B34BF83C4ED8AA43AFEEBADA1CBD4CB504F49CC10937616D9C`,
DLL Release `2CCC224AB6715F6D79CEC8707DC8F3D6AA98611D7AFB744468E8723A5FF9B81D`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtime headless/windowed e replays determinísticos; `10` cenários,
`777` frames, `54` eventos de áudio e `1554` samples de animação. ISO somente
leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência independente de estado→draw/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Avanço verificável — controle independente de compatibilidade estrutural atributo/textura candidata — 2026-10-08

Foi implementado `tools/analysis/eagl_candidate_attribute_texture_compatibility_control.py`.
O reparse independente juntou os `24` perfis de atributo candidato aos `24`
assets/texturas same-trunk. Houve `24/24` joins e inputs válidos; dimensões,
metadados `PAL8` e pixels decodificados foram válidos em `24/24` rows. As
faixas numéricas opacas foram finitas e normalizadas em `24/24` rows, e
`slot1_values` coincidiu com a contagem de vértices em `24/24` rows.

Status: `verified-independent-candidate-attribute-texture-compatibility-control`;
`semantic_binding=not-proven` e `original_semantics=not-claimed`. Isso não
chama o atributo de UV, não seleciona material/sampling e não prova binding
original de estado, draw ou GPU. O subgate
`assets-candidate-attribute-texture-compatibility-control` foi registrado como
`verified-structural-negative`, `observed=24`, `expected=24`; assets/rendering
originais, áudio e deformação continuam sem os bindings necessários e o
readiness permanece `NOT_READY`.

Hashes: relatório `C49F4BB877646835CFA00720667503A64B2E9F64E5A3A5557AAE675D6BB8D3B9`,
resumo `750E4B79966EF5775BE0C631B99A523F26230D3EEA926EACB27C4DE45400C859`,
readiness `B16C9A047528917D2082971B9D4D671B2AA0DFB798D0916AC1D03A0F7C10A736`,
script `7B7912822CCFCAB4A74049AB5D6B569EFF84706931A35BC8EDA9D2356C6D185A`,
matrix `73A629EB52715005FD9EFCDD0B4B108943BF597438E2CFA8539C028BCF4B669F`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtime headless/windowed e replays determinísticos; `10` cenários,
`777` frames, `54` eventos de áudio e `1554` samples de animação. ISO somente
leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de estado→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Avanço verificável — controle estrutural independente de estado/geometria/bounds — 2026-10-08

Foi implementado `tools/analysis/eagl_original_render_state_geometry_bounds_control.py`.
O controle junta independentemente os relatórios de partição
estado/geometria e bounds/fetch, restrito aos `24` objetos candidatos. Houve
`24/24` objetos particionados, `24/24` entradas de bounds válidas, `24/24`
joins e `24/24` rows completas. Foram observados `218` grupos de bounds, sendo
`216` grupos completos e `2` esperadamente incompletos; `218/218` coincidem no
slot `0`, `0` coincidem em slots não-zero e há `0` pares command-shape exatos.

Status: `verified-independent-original-render-state-geometry-bounds-structural-control`;
`semantic_binding=not-proven` e `original_semantics=not-claimed`. A evidência
é somente estrutural, sem promoção de estado→draw/geometria, posição, UV,
material, skinning, rigging, animação, morph ou GPU. O subgate
`rendering-original-state-geometry-bounds-control` foi registrado como
`verified-structural-negative`, `observed=24`, `expected=24`; assets/rendering
originais, áudio e deformação continuam sem os bindings necessários e o
readiness permanece `NOT_READY`.

Hashes: relatório `DFEBD93D163A551FA28905D9A05512E858F7172ECFAF285FAAEB03A35172ACDD`,
resumo `C0BC3EC18E8483589CFD37F8530CE5058063B424922EB1E670C9DA7261BAFCAA`,
readiness `E163BF21175BACA20B8D2CF8000443BFEAF0E37A64512F1C163BB5C91FBC2FB2`,
script `A078E08FC3B3A08B55CE4A1B73F3B74C13A6DD915B4D0DF1A68C9447CC4F4627`
e DLL Release `DD20BA2ADBFB50EFE5C783EDA7ED73E3BDDBEBDFE39861A68F70296B2F475893`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtime headless/windowed e replays determinísticos; `10` cenários,
`777` frames, `54` eventos de áudio e `1554` samples de animação. ISO somente
leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de estado→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Avanço verificável — controle lexical independente de categoria/ordinal dos slots candidatos — 2026-10-08

Foi implementado `tools/analysis/eagl_original_candidate_slot_category_control.py`
com reparse independente do fetch candidato e do controle de identidade bruta.
Foram reconciliados `218` rows, `24` objetos, `216` primitivas completas,
`2` rows esperadamente não materializadas e `1080` slots. Todos os `1080`
símbolos têm formato válido; `1080/1080` preservam a categoria `10 + slot` e
`1080/1080` preservam o ordinal da primitiva.

Status: `verified-independent-original-candidate-slot-category-ordinal-control`;
`original_semantics=not-claimed`. Isso é somente estrutura lexical dos nomes
`__GPvb`, não semântica de posição, UV, material, normal, peso, skinning,
rigging, animação, morph, draw ou GPU. O subgate novo é
`verified-structural-negative`, com `observed=218`, `expected=218`; assets
seguem `candidate-integrated`, rendering `not-proven`, áudio
`partially-integrated` e readiness `NOT_READY`.

Hashes: relatório
`95ED83D5CDD52216EBEA25E3C660E4D4134A287EAAB91D7768CBEB1AEC5AAD24`, resumo
`EF7DEF8F1835E35E60BC4A25A8BF3C173C9290A028FDB5CC9133A4E42F8AF42F`,
readiness `9B82157CB82C3F53935E0561CA2CEB31852CA3E5A6EFD77E5FD982F7658CF145`
e DLL Release `DD55D70B0B1941433095F50B6B6626D48F28643491A4EB0756FCA08936D9D82E`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtime headless/windowed e replays determinísticos. Foram confirmados
`10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de
animação. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de render-state→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Avanço verificável — identidade bruta independente dos streams candidatos — 2026-10-08

Foi implementado `eagl-original-candidate-raw-stream-identity-audit` com
reparse independente em `tools/analysis/eagl_original_candidate_raw_stream_identity_control.py`.
O controle relê os 24 objetos EAGL da ISO e compara, por chave de objeto,
primitiva e layer, o relatório de fetch candidato com os ranges `__VB` brutos.
Foram verificados `218` rows candidatos, `216` primitivas completas, `1080`
slots materializados, `1080/1080` metadados e hashes de payload exatos, `216`
identidades completas, `2` rows esperadamente não materializadas e `0`
rejeições.

Status: `verified-independent-original-candidate-raw-stream-identity-control`
e `verified-structural-negative`. É controle de identidade de bytes, não
binding: não atribui significado aos slots nem promove geometria, UV, material,
skinning, rigging, animação, morph ou GPU (`original_semantics=not-claimed`).
O subgate tem `observed=216`, `expected=218`; assets continuam
`candidate-integrated`, rendering `not-proven`, áudio `partially-integrated` e
readiness `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtime headless/windowed e replays determinísticos aplicáveis. A
matriz confirmou 10 cenários authored, 777 frames, 54 eventos de áudio e 1554
samples de animação. ISO preservada somente para leitura: `2013003776` bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de render-state→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Avanço verificável — controle negativo de identidade bruta EVT/ABKC/AST — 2026-10-08

Foi implementado `audio-event-abk-raw-identity-audit` com reparse
independente em `tools/analysis/audio_event_abk_raw_identity_control.py`.
O controle lê a ISO em modo somente leitura e confere `17` ranges de dados EVT,
`16` bancos ABKC, `270` entradas `SoundEntries` não-dummy e `70/70` ranges AST
com `SCHl`. As comparações deram `17` hashes EVT distintos, `0` pares de hash
bruto completo, `0` eventos com match, `0` pares de prefixo e `0` rejeições.

Statuses: `verified-bounded-audio-event-abk-raw-identity-negative-control` e
`verified-independent-audio-event-abk-raw-identity-control`. É uma evidência
negativa de identidade bruta, não um binding: não escolhe ABKC/sample, não
liga EVT a gameplay/playback e mantém `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`. O novo subgate é
`verified-structural-negative`; o gate de áudio original segue
`partially-integrated`, rendering `not-proven` e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `89/89`, matriz completa com exit code `0`,
smokes, runtime headless/windowed e replays determinísticos aplicáveis. A
matriz confirmou 10 cenários, 777 frames, 54 eventos de áudio e 1554 samples
de animação. A ISO permaneceu intacta e somente leitura: `2013003776` bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de estado→geometria/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Avanço verificável — controle independente de identidade command/payload — 2026-10-08

O novo comando `eagl-original-primitive-stream-command-payload-identity-audit`
reprocessa os EAGL originais da ISO e compara os pacotes brutos de command 9
com identidades SHA-256 limitadas de registro `__VB`, descritor e cabeça/cauda
do payload. Foram observados `2364` primitivas, `2362` fetches completos,
`10240` candidatos, `10240` identidades distintas, `0` duplicatas e `0`
correspondências exatas command/payload; o ordinal atual foi único em `2364/2364`.
O controle Python independente reproduziu os contadores e `2364/2364` rows
coerentes.

Statuses: `verified-bounded-original-primitive-stream-command-payload-identity-control`
e `verified-independent-original-primitive-stream-command-payload-identity-control`.
É evidência negativa de identidade bruta, sem semântica atribuída a slot,
geometria, UV, material, skinning, rigging, animação, morph, comando ou GPU;
`original_semantics=not-claimed`. O subgate é `verified-structural-negative`,
mas os gates originais de assets/rendering continuam abertos e readiness é
`NOT_READY`.

Matriz completa concluída com exit code `0`: Release `0/0`, self-test `88/88`,
smokes, runtimes headless/windowed e replays determinísticos passaram. ISO
intacta e somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência independente para estado→geometria/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Avanço verificável — controle independente de slot 1/categoria numérica 11 — 2026-10-08

O comando `eagl-original-candidate-slot-profile-audit` reprocessa, sem
alterar a ISO, `eagl-original-vertex-stream-numeric.tsv` e
`eagl-candidate-fetch-association.tsv`. A associação usa arquivo, objeto e
símbolo do stream porque o símbolo isolado se repete.

O resultado real é `216/216` primitivas candidatas completas com slot 1
reconciliado ao stream original do mesmo objeto, com símbolo, stride,
byte-length e element-count exatos. Também há categoria original `11` e
perfil float2 finito/próximo de unidade em `216/216`; o conjunto contém
`11820` rows originais, `218` primitivas candidatas e `216` completas.
Status: `verified-independent-original-candidate-slot1-category11-profile-control`.

O controle não interpreta slot 1 nem categoria 11 como UV, material, normal,
peso, skinning, rigging, animação, morph ou comando GPU. A semântica original
continua `not-claimed` e o readiness continua `NOT_READY`.

Release `0/0`, self-test `87/87`, matriz authored/candidate opt-in, smokes,
runtimes headless/windowed e replays determinísticos passaram. A ISO segue
intacta: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência independente para estado de renderização→geometria,
UV/material, timing/pose/rig/deformação e EVT→ABKC→sample→playback. Os gates
originais correspondentes ainda não estão fechados.

## Avanço verificável — controle posicional de campos `.off` para words CSI — 2026-10-08

O comando `fifast2-csi-opaque-field-audit` fez uma comparação puramente
posicional entre os `100` words opacos de `FIFAST2.csi` e as três colunas mais o
ordinal das linhas de `pcpahdr.off`/`pcpadat.off`. As tabelas completas têm
`283` rows e continuam partições válidas; os campos não foram nomeados como
offset, tamanho, identificador, header, data, sample ou playback.

Na ISO: `pcpahdr any_column=1/100` (`column1=0`, `column2=1`, `column3=0`),
`pcpadat any_column=0/100`, e `row_ordinal=3/100` em cada tabela. Isso é
coincidência estrutural fraca, não discrimina seleção e não altera
`opaque_word_semantics=not-decoded`, `direct_selection_binding=not-proven` ou
`original_semantics=not-claimed`.

Matriz authored/candidate opt-in concluída com exit code `0`: Release `0/0`,
self-test `86/86`, smokes, runtimes headless/windowed e replays determinísticos
passaram; readiness continua `NOT_READY`. ISO intacta: `2013003776` bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: concentrar o trabalho nos bindings independentes de
renderização/UV/material e timing/pose/rig/deformação; EVT→ABKC/sample/playback
permanece sem semântica decodificada.

## Avanço verificável — controle de boundaries dos words opacos CSI — 2026-10-08

O controle `fifast2-csi-opaque-range-audit` cruzou os `100` words opacos com as
partições reais de `pcpahdr.off`/`.big` e `pcpadat.off`/`.big`. As duas tabelas
têm `283` rows, cobrem integralmente `35572` e `601688832` bytes, e todos os
words estão em algum range.

Não houve nenhum boundary coincidente: `pcpahdr exact_start/end=0/0` e
`pcpadat exact_start/end=0/0`. O containment de header ocupa `80` rows e o de
data somente a primeira, portanto não identifica registros. O status é
`verified-structural-csi-opaque-range-boundary-control`; semântica opaca
`not-decoded`, binding direto `not-proven` e semântica original `not-claimed`.

Release `0/0`, self-test `85/85`, smoke real e readiness `NOT_READY`. ISO
preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: testar somente sinais independentes de subcampo/identificador;
na ausência deles, manter o CSI não decodificado e avançar nos gates de
renderização, UV/material e deformação.

## Avanço verificável — namespace de hashes `tunavars.txt` cruzado com CSI — 2026-10-08

O novo audit `fifast2-csi-tuna-audit` confirmou estruturalmente, em modo ISO
read-only, `tuna.big` (`BIGF`, `6313` bytes, uma entrada `tunavars.txt`) e suas
`106` rows de hashes. Todas `106/106` reproduzem o candidato
`djb2-like-seed-0x1505-multiply-0x21`. `FIFAST2.csi` tem `100/100` registros
válidos e `89` nomes em comum com tuna; `11` nomes CSI não estão em tuna e `17`
nomes tuna não estão no CSI.

Os words opacos CSI não coincidem com os hashes tuna (`0/89`). Os `100/100`
estão dentro do comprimento de `pcpahdr.big`, mas isso é apenas faixa numérica;
como `pcpahdr.off` cobre o asset inteiro, não foi demonstrada associação a
registro, subcampo, sample, ABKC ou playback. Status:
`verified-structural-tuna-csi-name-hash-crosscheck`,
`direct_selection_binding=not-proven`, `opaque_word_semantics=not-decoded` e
`original_semantics=not-claimed`.

Release `0/0`, self-test `84/84`, auditoria real e readiness `NOT_READY`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: discriminar words CSI contra limites/subestruturas reais de
`pcpahdr`/`pcpadat`, sem convertê-los em seletores; continuam abertas as
lacunas de rendering, UV/material e timing/pose/rig/skinning.

## Avanço verificável — índice estrutural candidato de FIFAST2.csi — 2026-10-08

O comando `fifast2-csi-audit` agora valida estruturalmente
`data/audio/FIFAST2.csi` em modo somente leitura: magic `MOIR`, `100/100`
registros válidos, `100` nomes únicos, tabela de nomes iniciando em `0x5F4`,
word reservado zero em `100/100`, `12` words opacos dentro do asset e `98`
pares adjacentes não-decrescentes. Entre os `17` nomes textuais EVT e os nomes
CSI houve `0` matches exatos e `22` matches por substring.

O resultado permanece candidate-only: não há binding provado para trigger,
evento, sample, ABKC ou playback. `direct_selection_binding=not-proven` e
`original_semantics=not-claimed` continuam explícitos. Release `0/0`, self-test
`83/83`, Python `57/57`, matriz completa, runtime headless, smokes e replays
determinísticos passaram. ISO: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: estado→geometria/UV/material, EVT→amostra e timing/pose/rig/
skinning/morph, sempre mantendo a semântica original não reivindicada.

## Avanço verificável — layout de canal candidato único em streams AST — 2026-10-08

Quando um header SCHl observado declara `0` canais, o decoder tenta os layouts
de `1` e `2` canais sem reescrever os bytes. Somente um layout único validado
em todos os blocos é aceito, e a row recebe o status explícito
`decoded-payload-channel-layout-candidate`.

Os dois streams rejeitados anteriores (`GBREAK.abk` sound `4` e
`GBREAKLC.abk` sound `6`) aceitaram unicamente `1` canal. O exhaustive
bounded/all passou a `241/241` candidatos de banco, com `17/17` eventos,
`0` matches PCM exatos e `259/259` rows iguais no controle bounded-versus-all.
`CONTROL.abk` segue sem candidato. Isto não cria EVT→amostra, gameplay ou
playback original: `direct_selection_binding=not-proven` e
`original_semantics=not-claimed` permanecem.

Release `0/0`, self-test `82/82`, smokes de áudio, controle de política e
síntese negativa passaram; a matriz completa 0.78 terminou com
`matrix_status=completed-authored-and-candidate-opt-in`, incluindo runtime
headless e replay determinístico. ISO intacta: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73D8CDEBFA005B2`.

Próxima etapa: continuar a investigação independente de seleção EVT→amostra,
UV/material e deformação.

## Avanço verificável — enumeração ABKC por entradas observadas — 2026-10-08

O catálogo agora conserva as entradas ABKC não-dummy observadas pelo parser e
os caminhos de batch/correlação enumeram somente índices RAM com
`SoundType=0`. Isso evita que contadores declarados com entradas dummy gerem
candidatos artificiais; o fallback por contador continua disponível apenas
para fixtures sem essa evidência.

Na reexecução bounded/all da ISO foram obtidos `17/17` eventos nomeados,
`16` bancos, `241` candidatos não-dummy e `239` decodificações. O controle
independente comparou `259/259` rows (`17` eventos e `242` rows de banco),
com identidade, status, decode, bytes WAV, hashes PCM e contadores exatos
iguais; `0` pares PCM exatos. `CONTROL.abk` segue sem candidato, e os dois
streams AST rejeitados por `0 channels` seguem rejeitados, sem inferência de
silêncio ou semântica.

O resultado é avanço de enumeração/validação estrutural, não é seleção
EVT→amostra, binding de gameplay ou playback original:
`direct_selection_binding=not-proven`, `original_semantics=not-claimed` e
`audio=partially-integrated` permanecem. Rendering continua `not-proven` e
animation/deformation `structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `57/57`, matriz 0.77,
smokes relevantes, runtime headless e replay determinístico. ISO intacta:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de seleção EVT→amostra e de
atributos/UV/material/deformação sem promover candidatos.

## Avanço verificável — join original↔candidate dos grupos de geometria por stream — 2026-10-08

O novo `eagl-candidate-geometry-stream-audit` reparseou `24/24` objetos e
`38/38` grupos candidate. Cada grupo tem primitiva correspondente, fonte no
slot candidate `0`, cinco streams completos e geometria finita/indexada.

O controle independente `eagl_original_candidate_geometry_stream_control.py`
reuniu esses grupos às `2364` chaves originais de slot `0`: `38/38` coincidiram
em símbolo `__VB`, stride, byte-length, element-count, vértices e triângulos.
As `2326` chaves originais sem grupo candidate continuam explicitamente fora do
join. A escolha candidate do slot `0` e a igualdade de metadata permanecem
estruturais; não são binding de posição, UV, material, normal, peso, skinning,
rigging, animação, morph, draw ou GPU. `original_semantics=not-claimed`.

O readiness adiciona
`rendering-original-candidate-geometry-stream-control=verified-structural-negative`
`38/38`; permanecem `rendering=not-proven`, `audio=partially-integrated` e
`animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `57/57`, matriz 0.76,
smokes, runtime headless/windowed e replays determinísticos; ISO intacta com
2013003776 bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar decodificação independente de atributos/UV/material,
deformação e EVT→amostra sem inventar bindings.

## Avanço verificável — join original↔candidate de registros `__VB` por slot — 2026-10-08

O controle `eagl_original_candidate_stream_record_control.py` reparseou a
topologia original e os fetch records candidate por chave de primitiva e slot.
Nos `216` layouts completos, `1080/1080` slots materializados coincidiram em
nome `__VB`, aux, fetch/stream stride, byte-length e element-count; `1080/1080`
records ficaram válidos. Os dois layouts incompletos não foram sintetizados:
ficaram como dois records não materializados esperados. As `2146` primitivas
originais sem candidate continuam fora da cobertura do join.

O readiness adiciona
`rendering-original-candidate-stream-record-control=verified-structural-negative`
`1080/1080`, com `original_semantics=not-claimed`. A evidência é somente de
identidade/metadata estrutural: não é binding para posição, UV, material,
skinning, rigging, animação, morph, draw ou GPU. Permanecem
`rendering=not-proven`, `audio=partially-integrated` e
`animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `56/56`, matriz 0.75,
smokes, runtime headless/windowed e replays determinísticos; ISO intacta com
2013003776 bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: investigar campos de atributo/UV/material, deformação e
EVT→amostra com prova independente, sem promover a correspondência estrutural.

## Avanço verificável — join original↔candidate de comando/fetch por primitiva — 2026-10-08

O controle `eagl_original_candidate_primitive_fetch_control.py` juntou por
`(model_archive, model_object, layer, primitive)` as `218` primitivas candidate
com o relatório original. Houve `218/218` matches de contagem de comando,
completude, contagem de fetch, completude de cinco slots, texto de estado e
flag de tipo. A sequência de IDs/records foi materializada em `216/218`; os
dois casos restantes são os layouts incompletos já observados e ficaram
registrados, sem inferência, como dois `slot_records` não materializados.
Não houve candidate sem chave original (`0`); as `2146` primitivas originais
fora da cobertura candidate continuam explicitamente fora do join.

O readiness registra
`rendering-original-candidate-primitive-fetch-control=verified-structural-negative`
`218/218`, com `original_semantics=not-claimed`. É evidência estrutural de
reconciliação, não binding de slot/comando para posição, UV, material,
skinning, rigging, animação, morph, draw ou GPU. Permanecem
`rendering=not-proven`, `audio=partially-integrated` e
`animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `55/55`, matriz 0.74,
smokes, runtime headless/windowed e replays determinísticos; ISO intacta com
2013003776 bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente para primitiva→vértice/atributo/
UV/material, deformação e EVT→amostra sem promover candidatos.

## Avanço verificável — sequência de índices SHAPENAME original↔candidate — 2026-10-08

O novo comando `eagl-candidate-shape-state-primitive-audit` reparseou, por
objeto, os índices e camadas das primitivas candidate com `SHAPENAME`. O join
independente com `eagl-state-geometry-adjacency.tsv` encontrou `24/24`
sequências, `24/24` conjuntos e `24/24` contagens exatamente iguais; os
`24/24` streams candidate estavam completos.

É controle estrutural/negativo, não binding: não atribui primitiva a vértice,
UV, material, skinning, rigging, animação, morph, comando GPU ou semântica
original. O readiness adiciona
`rendering-original-candidate-shapename-primitive-index-control=verified-structural-negative`
`24/24`; `rendering=not-proven`, `audio=partially-integrated` e
`animation-deformation=structural-only` permanecem.

Validação: Release `0/0`, self-test `82/82`, Python `54/54`, matriz 0.73,
smokes, runtime headless/windowed e replays determinísticos; ISO intacta com
2013003776 bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca por decodificação independente de
primitiva→vértice/atributo/UV/material, deformação e EVT→amostra.

## Avanço verificável — população de shape-state por primitiva — 2026-10-08

O controle independente comparou, por objeto, a quantidade de primitivas de
estado original com a população de shape-state candidata. Foram obtidos
`24/24` matches de contagem, `24/24` contagens de tokens `SHAPENAME` iguais,
`24/24` streams shape-state candidatos completos, além de `24/24` same-trunk,
cadeia de origem válida, entrada SHPX exata e hash de pixels coincidente.

É evidência estrutural/negativa: não cria vínculo primitiva→vértice, UV,
material, skinning, rigging, animação, morph, comando GPU ou semântica
original. Readiness adiciona
`rendering-original-state-candidate-primitive-shape-control=verified-structural-negative`
`24/24`; `rendering=not-proven`, `audio=partially-integrated` e
`animation-deformation=structural-only` permanecem.

Validação: Release `0/0`, self-test `82/82`, Python `53/53`, matriz 0.72,
smokes, runtime headless/windowed e replays determinísticos; ISO intacta com
2013003776 bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca por decodificação independente de primitiva,
atributos/UV/material, deformação e EVT→amostra.

## Avanço verificável — controle independente SHAPENAME↔asset candidato — 2026-10-08

O novo controle reparseou independentemente o estado original e a cadeia de
assets candidatos. Dos `24` objetos, `24/24` foram unidos; `24/24` tokens
`SHAPENAME` coincidiram lexicalmente com o shape candidato, `24/24` ficaram no
mesmo tronco, `24/24` mantiveram cadeia de origem válida, `24/24` entradas SHPX
foram exatas e `24/24` hashes de pixels decodificados coincidiram.

É evidência estrutural de identidade de asset, não binding de UV/material,
vértice, skinning, rigging, animação, morph, comando GPU ou semântica original.
O readiness adiciona
`rendering-original-shapename-candidate-asset-control=verified-structural-negative`
`24/24`; os gates principais permanecem `assets=candidate-integrated`,
`rendering=not-proven`, `audio=partially-integrated` e
`animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `52/52`, matriz 0.71,
smokes, runtime headless/windowed e replays determinísticos; ISO intacta com
2013003776 bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: investigar estado de renderização/UV/material, deformação de
animação e EVT→amostra sem inventar semântica.

## Avanço verificável — síntese negativa independente da seleção de áudio original — 2026-10-08

O controle `audio_event_selection_negative_synthesis.py` juntou cinco fontes
independentes de evidência sem promover uma seleção original. Foram
confirmados: `17` words EVT sem ocorrências alinhadas em ABK/PCPAHDR/PCPADAT;
zero ocorrências alinhadas ou não alinhadas desses words no XBE; `17/17`
payloads EVT decodificados contra `16` bancos, `368` candidatos e `239` sons
decodificados, com `0` pares PCM exatos; join de faixas nomeadas `17/17`; e
`15/15` rotas authored limpas. Readiness registra
`audio-event-selection-negative-synthesis=verified-structural-negative` `5/5`.

É uma síntese negativa auditável, não prova de ausência global e não é decoder:
`direct_selection_binding=not-proven`, `original_semantics=not-claimed` e
`audio=partially-integrated` permanecem. Rendering original continua
`not-proven` e animação/deformação continua `structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `51/51`, matriz 0.70 com
smokes, runtime headless/windowed e replays determinísticos; ISO intacta com
2013003776 bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: investigar bindings originais de renderização/animação e
EVT→amostra sem inventar semântica.

## Avanço verificável — rotas authored de áudio ↔ payloads nomeados — 2026-10-08

O controle independente reparseou os traces de áudio authored e os juntou ao
runtime nomeado: `10/10` cenários, `54/54` eventos tocados, `15/15` rotas
ação→candidato, `8` candidatos únicos, `15/15` rotas preparadas e com header
WAV válido, sem ausências no catálogo de `17` candidatos.

É prova do caminho clean-room e dos replays, não seleção original. Não cria
EVT→ABKC/sample, não recupera IDs de gameplay e não reivindica playback
original; `original_semantics=not-claimed`, `audio=partially-integrated` e o
novo readiness `audio-authored-candidate-route-control=verified-authored`
15/15 permanecem separados do gate original.

Validação: Release `0/0`, self-test `82/82`, Python `50/50`, matriz 0.69,
smokes, runtime headless/windowed e replays determinísticos; ISO intacta com
2013003776 bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: seleção EVT→amostra e associações originais de
renderização/animação, sem inventar bindings.

## Avanço verificável — estado original ↔ atributo/influência candidata — 2026-10-08

O novo controle independente juntou por chave de objeto os tokens de estado
original aos relatórios candidatos: `24/24` objetos candidatos e `24/24` joins,
com `218/218` linhas de estado válidas e sem tokens literais de textura,
material, UV, atributo ou deformação. Os perfis `slot1` candidatos foram
completos/finito/near-unit em `24/24`; a geometria foi finita/indexada em
`24/24`, o raster mudou por textura em `20/24` e `mesh_shape_state_exact` foi
`0/24`.

É um controle estrutural negativo: não atribui semântica a `slot1`, não prova
estado→geometria/UV/material e não afirma ausência semântica em comandos opacos.
Mantidos `original_semantics=not-claimed`,
`rendering-original-state-candidate-attribute-control=verified-structural-negative`
24/24 e `rendering=not-proven`.

Validação: Release `0/0`, self-test `82/82`, Python `49/49`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos; ISO intacta com
2013003776 bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: seleção EVT→amostra e associação independente de estado/UV/material,
sem promover evidência candidata a semântica original.

## Avanço verificável — join independente EVT paralelo → payload runtime — 2026-10-08

O controle independente juntou `17/17` nomes EVT entre as faixas paralelas
PCPAHDR/PCPADAT e o relatório de payload nomeado do runtime. Header row, offset
e comprimento coincidiram em `17/17`; metadados, preparação e headers WAV também
passaram `17/17`.

É evidência de identidade/range do caminho candidato gerenciado, não seleção de
ABKC/sample, gameplay ou playback original. Mantidos
`original_semantics=not-claimed` e `audio=partially-integrated`; o readiness
registra `audio-event-parallel-runtime-join-control` como
`verified-structural-negative` 17/17.

Validação: Release 0/0, self-test `82/82`, Python `48/48`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos passaram. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `DB82C0D3C592EF8158829CB824F2812F769A4C9165D3F617F4B2D38B9F5F57E4`;
controle `5B15C5A54916670E1CB9BBEB573ECC9CF0F04F90D00157EC66337495BFC6BD21`;
summary `8F5C982EA17AC3F50478029C37A1F52CAD858D18AE5C6192DCE8A829C8EEB0FF`;
readiness `77FE7E7D7EC8E1AABA325E3C21583AF8F86AA312BCB0A31DF5D755F762BC9A6A`;
log `4737F1A78814A8CBF248406187F430B6915FBF97C8FE3C5C6BC7CB5E55209C37`.

Próxima etapa: seleção EVT→amostra e associação independente estado/UV/material,
sem promover a cadeia candidata a semântica original.

## Avanço verificável — join independente entre streams numéricos e fetch slots opacos — 2026-10-08

O controle independente juntou `11820/11820` streams numéricos aos
`11820/11820` registros de topologia por chave opaca. Há `2364/2364` grupos
completos de cinco streams; o primeiro registro raw aponta para fetch slot `0`
em `2364/2364`, tem stride compatível em `2364/2364`, coincide com bounds em
`2363/2364` e nenhum registro posterior coincide. Os dois grupos com slots
incompletos foram mantidos como evidência não decodificada.

Não é binding de posição, geometria, UV, material, skinning, rigging, animação,
morph ou GPU; `semantic_binding=not-proven` e `original_semantics=not-claimed`.
Readiness: `rendering-original-bounds-fetch-slot-join-control` é
`verified-structural-negative` 2364/2364; renderização original continua
`not-proven`.

Validação: Release 0/0, self-test `82/82`, Python `47/47`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos passaram. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `51B2B70A129421A5F05BA04FA2331109A439FF5D3EAAD2CEF9C19E351E00A836`;
controle `E8BE8F200ED47B6423EF43244BB41BED8CFC76FC38C343B108BFA5C9B4996187`;
summary `04681A4777F27672E69A22B1A0194277F30D806A7BF7F7B09B9B19E80887933D`;
readiness `FB761D99DF155D83BFC371D1AADC9F77DCE0E05C4F0E91994BE47F41DC1C0B28`;
log `6EEBD42AE2CBCA515F9EFA1A869EC969484B18FC35D6E2D8646DC539F388DDE5`.

Próxima etapa: investigar bindings de estado/UV/material e EVT→amostra sem
promover a associação numérica a semântica original.

## Avanço verificável — bounds no primeiro registro raw — 2026-10-08

O relatório por stream e o controle independente agruparam `11820` streams em
`2364` grupos objeto/ordinal. Todos os grupos têm cinco registros e categoria
contínua; o primeiro registro raw tem stride 12 em `2364/2364`, contém os bounds
em `2363/2364`, e nenhum registro posterior contém match. As famílias observadas
foram `category_key=10` (2010) e `category_key=11` (354).

Isso ainda não é binding de posição: não atribui posição, geometria, UV,
material, skinning, rigging, animação, morph ou GPU;
`semantic_binding=not-proven`, `original_semantics=not-claimed`. O readiness
registra `rendering-original-bounds-consistent-record-control` como
`verified-structural-negative` 2364/2364; renderização original segue
`not-proven`.

Validação: Release 0/0, self-test `82/82`, Python `46/46`, matriz completa,
smokes, runtime headless/windowed e replays passaram. ISO sem escrita:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `C17C7DE0FC2E6960F69304A5A2841C06DD6C1EB280B674DF344C0542DE00D661`;
stream report `A6DA833F84E07FFCF8388A215B6618406383C61854B40CCE0A8A33498141FF12`;
controle `2F9056749FEAABA25A0C38A6EB10F19AECCE5F23F0961757968D609BFF0F0551`;
summary `C59DA46CC903A5F3E23D95AA628E9EBCA19880766458796C2668682E2530A072`;
readiness `3B21349B7153257FB04C6AFE19C6511BCF47BC8B4756E4E290B2EC4EE8E0D9A4`;
log `5672C8714ABBFF8053007DAFD0E42B576E06C14E95F8D0E4D54AC7E04DB5ADE3`.

Próxima etapa: investigar bindings de estado/UV/material e EVT→amostra sem
promover essa associação numérica a semântica original.

## Avanço verificável — cardinalidade independente dos matches numéricos — 2026-10-08

`eagl_original_numeric_stream_ambiguity_control.py` reparseou o relatório
numérico original em `883/883` objetos: `494` tinham exatamente um stream
`float3` dentro dos bounds, `388` tinham múltiplos e `1` não tinha nenhum.
Esse resultado mede ambiguidade no nível de objeto; não identifica posição,
geometria, primitiva ou atributo e não cria binding semântico. O readiness
registra `rendering-original-numeric-stream-ambiguity-control` como
`verified-structural-negative` 883/883; renderização original continua
`not-proven`, `semantic_binding=not-proven` e `original_semantics=not-claimed`.

Validação: Release 0/0, self-test `82/82`, Python `45/45`, matriz, smokes,
runtime headless/windowed e replays determinísticos passaram; a ISO permaneceu
sem escrita e conservou 2013003776 bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `CF863DA6CE14326060C7E4A4CBA8FE9E5FC4347C504C683A5422B980E30BF986`;
relatório `C637C4D1EF8DE77D2544140CB7229410DE83EA40D8604F70A8FD59148419CC82`;
summary `6F137E8CC1681F8B5F3AE1E0EA4E31BA62DEF4F87A640095183D6CDB16A075ED`;
readiness `FF5AA4D98EA0CFE74FF516B151598842BE2E09F64DEFDEC417468A6CB149E7AD`;
log `F15BC9C41A4AEBB470EA29FE5D5064588A8F1D3F36B01C8CD3B13F02D84C2016`.

Próxima etapa: seleção EVT→amostra e decoders de renderização/animação, sem
inventar bindings.

Data do handoff: 2026-10-06  
Workspace: `D:\projeto zeta`  
Projeto: reimplementação clean-room em .NET, sem emulação.

## Avanço verificável — controle independente do caminho textual do asset EVT — 2026-10-08

O novo controle `audio_event_asset_path_index_control.py` juntou os literais
textuais de `EVENTSPA.EVT` no XBE ao índice de `eventspa.evt`. Foram validadas
4 linhas, 9 ocorrências, 4/4 convergências para o único caminho ISO
`data/audio/eventspa.evt`, 17 nomes EVT e 1.061 pares candidatos; nenhuma linha
tem xref executável direto.

É evidência textual/negativa: não recupera loader, não seleciona ABKC/AST/BNK,
não liga gameplay a playback e mantém `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`. O readiness adiciona
`audio-event-asset-path-index-control` como `verified-structural-negative`
4/4; o gate principal de áudio permanece `partially-integrated`.

Próxima etapa: continuar a busca de seleção EVT→amostra e os decoders de
renderização/animação, preservando authored, candidato e original separados.

Validação deste avanço: build Release 0/0, self-test 82/82, teste Python do
controle aprovado, matriz completa exit_code=0, smokes, runtime headless/windowed
e replays determinísticos. ISO sem escrita: 2013003776 bytes, `Archive`,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `E538D9DAC739B39952BD1F0D604A050263EF1F8D54EB88828CB49D632D4411C2`;
relatório `5118A4CC14D83879FFBD4EF2436CF8529A65F361E67E9AE9A70EC3882DBE03B5`;
summary `BF0F1E664FB14A507C3A11718534273935065F4B46A5E4E522D610A8F1941E56`;
readiness `171A0683581456334BEBCB751E8100E08632D7B7316786BF0C2A222B0688CE8B`;
log `0B7B3DD1380A35AC5B6F63ED298AD534DFFF7A8CC6A3A554A41F901279751988`.

## Avanço verificável — controle independente das políticas de payload de áudio — 2026-10-08

O controle `audio_event_payload_policy_control.py` comparou as políticas de
512 e 100.000 candidatos por bank. Foram iguais 386/386 linhas — 17 eventos e
369 candidatos de bank — em identidade, decode, bytes, hashes PCM e contadores;
0 pares de hash PCM exato.

O resultado só prova cobertura/reprodutibilidade. Não seleciona amostra, não
liga EVT a gameplay/playback e mantém `original_semantics=not-claimed`. O
readiness registra `audio-event-payload-policy-control` como
`verified-structural-negative` 386/386; áudio principal continua
`partially-integrated`.

Validação: Release 0/0, self-test 82/82, matriz completa exit_code=0, smokes,
runtime headless/windowed e replays determinísticos. ISO preservada:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `599EB85BD0AC52B328B6831A0F7B75464C0E673E9512395B7165647C64F22C26`;
relatório `F3D87BDCCD0F27B584C3B48BCB7B98AFDCB941AE57647DC37893621AEE1B1D94`;
summary `C0CEB3E9B359ACE60D9D126BCCE56427BFA034E5822FC3FDD7EE45903D8F9297`;
readiness `1FFB1B5187F5DF3AB307AEB78E20D2DE616CC8A83986D73CA5281E101DC0E49D`;
log `5878DB73ABA59EDAF70B5C5F9300DE82CDBB5D706A1F788A2422B7E44CD230BD`.

Próxima etapa: continuar a busca de seleção EVT→amostra e os decoders de
renderização/animação, sem promover controles estruturais a bindings.

## Avanço verificável — join independente estado/geometria ↔ payload original — 2026-10-07

O novo controle `eagl_original_state_geometry_payload_join_control.py` juntou
24/24 objetos `tops*.big` ao relatório independente render/payload, sem
ausências. Em 24/24 bateram identidade, contadores de primitivas, comandos
completos, fetches e cinco streams de payload por primitiva; houve 0 ranges
sobrepostos.

É evidência de chaves e cardinalidade, não binding semântico. Não atribui
payload a posição, UV, material, atributo de vértice, skinning, rigging,
animação, morph, comando ou GPU; `original_semantics=not-claimed`. O readiness
registra `rendering-original-state-geometry-payload-join-control` como
`verified-structural-negative` 24/24 e mantém `rendering=not-proven`.

Validação: Release 0/0, self-test 82/82, matriz completa exit_code=0, smokes,
runtime headless/windowed e replays determinísticos. ISO preservada:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `76F273539B1E1D14494B556D2D021F539D1F7EF7F6DB9F182F8237C317DD4FDE`;
relatório `7D0236122EB806FB288DFCE6F1C1923ADEF7E9CF81EE0A7DD0C0FF27D6E33F93`;
summary `48C79333F7C74C36632F7E5C660C677D5265950C4CFCA9D0AE9917E58413EF26`;
readiness `6678BF3B2546A8A802ACB96707CBA660032E06D282CBAE7265AD0AF147BDFED3`;
log `2B2A1FC0AA3286D9DD7E0A9EB1577F4AA43BD8976A423D0D54A10B373246E649`.

Próxima etapa: continuar os controles independentes de renderização e áudio,
sem promover resultados estruturais a semântica original.

## Avanço verificável — partição independente de estados e geometria — 2026-10-07

O controle `eagl_original_state_geometry_partition_control.py` reprocessa
independentemente os três relatórios originais de adjacência, perfil opaco e
ordem. Reconciliou 24/24 objetos, 269/269 perfis, 62/62 intervalos, 38 pares
geométricos, 180 estados e 180/180 índices cobertos. Status:
`verified-independent-original-state-geometry-partition-control`.

O resultado é estrutural e negativo: `object_state_exact=True` em 24/24,
`geometry_group_state_exact=False` em 24/24, streams completos em 269/269 e
zero formas de comando exatas. Não há binding de materiais, UVs, skinning,
rigging, animação, morphs, comandos ou GPU; `original_semantics=not-claimed`.
O readiness registra o controle como `verified-structural-negative` 24/24 e
mantém o gate principal de renderização `not-proven`.

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
original de áudio, mantendo authored, candidato e original semânticos
separados.

## Avanço verificável — controle negativo de words EVT no XBE — 2026-10-07

AudioEventXbeHashSearchAudit procurou os 17 words do índice textual de
eventspa.evt dentro dos 3.293.184 bytes do default.xbe, em offsets alinhados e
em todos os offsets de byte. O resultado foi 0 ocorrências alinhadas, 0 não
alinhadas, 17 nomes únicos e 17 words únicos.

Isso elimina apenas a hipótese literal simples e não prova ausência de uma
rota indireta. O XBE não foi executado; não houve recuperação de funções,
seleção de amostras ABKC ou binding de gameplay. O readiness registra
audio-event-xbe-hash-search como verified-structural, com
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

AudioEventParallelRangeAudit revalida os 17 nomes EVT contra o início textual
exato em eventspa.evt, a mesma linha nos companions pcpahdr.off/.big e
pcpadat.off/.big, os limites dos ranges e o prefixo SCHl. Foram confirmados
17/17 âncoras, 17/17 ranges de header, 17/17 ranges de dados, 17/17 índices
paralelos, 17/17 prefixos SCHl, 17/17 nomes/offsets/linhas únicos e 283/283
linhas nas duas tabelas.

Isso é apenas evidência estrutural. Não seleciona amostras ABKC, não liga
nomes a gameplay e não reivindica semântica original:
direct_selection_binding=not-proven e original_semantics=not-claimed. O
readiness registra audio-event-parallel-range como verified-structural; o gate
principal de áudio continua partially-integrated=false.

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

O readiness separa agora os agregados authored dos gates originais: assets
clean-room 10/10 cenários, rendering clean-room 140/140 frames, áudio authored
54/54 eventos e animação authored 1554/1554 samples. Gameplay permanece
`clean-room-integrated=true`.

Os agregados confirmam manifests authored, framebuffer managed, rotas authored e
traces/replays determinísticos. Não promovem candidate mesh, UV, material,
skinning, rigging, morph, timing ou comandos GPU. Os gates originais continuam
assets `candidate-integrated=false`, rendering `not-proven`, áudio
`partially-integrated=false` e animation-deformation `structural-only=false`,
com `original_semantics=not-claimed`.

Validação: Release 0/0, self-test 80/80, Python 43/43, matriz completa com
exit_code=0, smokes relevantes, runtime headless/windowed e replays
determinísticos. ISO preservada: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
4BB5F349628F3BFA8C6A9A425B33BCB41DBFC1135C4FEFA65F351AF5A44774B8;
readiness D126BE88409DDF706F6B320DFAB8EEF16725DACF888E47155193F6B5F82EB2C5.

Próxima etapa: investigar seleção original de áudio e associação original de
renderização, sem reutilizar rotas authored nem inventar bindings.

## Avanço verificável — gate principal de gameplay clean-room — 2026-10-07

O auditor agora marca `gameplay=clean-room-integrated` como verdadeiro somente
quando runtime authored, cobertura de ações, invariantes e cobertura integrada
estão todos válidos. A evidência atual confirma 10/10 cenários, 54 eventos,
777 frames e replays determinísticos; as regras originais continuam fora da
afirmação, com `original_semantics=not-claimed`.

Os gates de assets, renderização original, áudio original e
animation-deformation permanecem respectivamente
`candidate-integrated=false`, `not-proven`, `partially-integrated=false` e
`structural-only=false`. Validação: Release 0/0, self-test 80/80, Python
43/43, matriz completa com exit_code=0, smokes relevantes, runtime
headless/windowed e replays determinísticos. ISO preservada:
2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
2CB105E28AAE2C9BE49C61A845D97B6AF243933E658E5783E210343682F938CD;
readiness C8CBF5E05C3A205C29615BF6501E8550FFE80414A6F1B41BB4C1C7AB99893ACE.

Próxima etapa: continuar a integração clean-room de assets, renderização e
áudio, sem promover a evidência original.

## Avanço verificável — controle independente de regiões comando/payload — 2026-10-07

Foi adicionado
tools/analysis/eagl_original_command_payload_region_control.py. O controle
reparseia render-chain e topologia e cruza somente
model_archive/model_object/layer/primitive. Em 2364/2364 primitivas, as cinco
regiões de payload foram válidas e não sobrepostas; a seção coincidiu, o
intervalo de comando foi válido e disjunto e o comando ficou depois do payload
em 2364/2364. A lacuna bruta entre o maior fim de payload e
command_stream_offset foi 0x10 em 2364/2364.

Os dois casos sem slot 2 continuam registrados, sem preenchimento. É evidência
estrutural de intervalos/offsets apenas, com original_semantics=not-claimed:
não há binding de bytes para posição, geometria, UV, material, skinning,
rigging, animação, morph, comando ou GPU. O subgate
rendering-original-command-payload-region-control é verified-structural, mas o
readiness geral continua NOT_READY.

Validação: Release 0/0, self-test 80/80, Python 43/43, matriz completa,
smokes relevantes, runtime headless/windowed e replays determinísticos. ISO
preservada, 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
C90B6FD374D53B9CA29A60872C5A1B4E15977E59A99605BCF91BCCA9DC1C97BA;
readiness D48F89BC8DA712F915569FD0CF792A61C4302CFAAC23031E009C6E437FDD9202.

Próxima etapa: buscar associação original independente além da coerência de
offsets e intervalos, mantendo separados comprovado, candidato e não
decodificado.

## Avanço verificável — controle independente fetch/topologia por primitiva — 2026-10-07

O controle
tools/analysis/eagl_original_primitive_fetch_topology_control.py reparseia
render-chain e topologia original e cruza a chave
model_archive/model_object/layer/primitive. O resultado foi 2364/2364 joins,
cinco linhas de topologia por primitiva, IDs de fetch coincidentes em 2364/2364
e concordância da completude em 2364/2364.

Foram mantidos 11820/11820 ordinais e ranges válidos, 11818 slots efetivos,
11810 linhas com structural_match e 2362 grupos completos de cinco slots. As
duas exceções são a ausência observada do slot 2; não houve preenchimento nem
promoção. Isso permanece estrutural, com original_semantics=not-claimed, sem
binding de slot/stream para posição, geometria, UV, material, skinning, rigging,
animação, morph, comando ou GPU.

O subgate é verified-structural, mas o readiness geral continua NOT_READY.
Release 0/0, self-test 80/80, Python 42/42, matriz completa, smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. ISO preservada:
2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: buscar associação original independente além da coerência
estrutural de slots, mantendo separados comprovado, candidato e não decodificado.

## Avanço verificável — controle independente estado/fetch ↔ adjacência — 2026-10-07

O novo controle
tools/analysis/eagl_original_state_fetch_adjacency_control.py reparseia os
relatórios render-chain e de adjacência e faz join independente pela chave
model_archive/model_object/layer/primitive. O resultado foi 2364/2364 joins:
state relocation 2364, SHAPENAME 752, SetPrimitiveType 1612 e fetch completo
de cinco slots 2362. O overlap state/fetch reportado e recomputado foi 750/750;
state/primitive-type foi 0/0.

A adjacência exata esteve presente em 2364/2364; state+adjacência em
2364/2364; state+fetch+adjacência em 2362/2364; SHAPENAME+adjacência em
752/2364; SetPrimitiveType+adjacência em 1612/2364. Categorias: 14:2010 e
15:354. É evidência estrutural e de coocorrência, não binding nem semântica:
state→geometria, UV, material, skinning, rigging, animação, morph e GPU
continuam não decodificados. original_semantics=not-claimed.

O subgate rendering-original-state-fetch-adjacency-control é
verified-structural, mas o readiness geral continua NOT_READY. Release 0/0,
self-test 80/80, Python 41/41, matriz completa, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO preservada:
2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: procurar evidência independente de semântica de
campo/atributo ou associação estado→geometria/UV/material, mantendo a
distinção entre comprovado, candidato e não decodificado.

## Avanço verificável — perfil bruto da adjacência comando/descritor — 2026-10-07

O audit .NET e o controle Python passaram a registrar a categoria e o stride do
stream __VB que satisfaz a adjacência bruta command_stream_offset =
descriptor_offset + 0x0C. O resultado real foi: 2364 streams exatos em 2364
primitivas, categoria 14 em 2010, categoria 15 em 354 e stride 2 em 2364. O
controle independente reproduziu os mesmos valores.

É somente evidência estrutural de offsets e bytes; não decodifica campo,
atributo, geometria, UV, material, skinning, rigging, animação, morph, comando
ou GPU. original_semantics=not-claimed, subgate verified-structural,
readiness geral NOT_READY.

Relatórios: audit
46E952F58C8A40507E3A1A2D9C741CD91DBA89611F09D5B860D7CF290AFD8C56,
summary E5B5B6D8751D7174E5E609684A77A24503CC7CFE054E538A30F77A4E7E86CF37,
controle 41BC4DCD41AFD0496AE3B265C5AD5410C1EA85A990084C9D7D9FBB05D82D02DC,
summary E5134201C80EF61C1B76690DB0B6450A375E2D6772E7DF3AEB93686723AE92DF.

Validação: Release 0/0, self-test 80/80, Python 40/40, matriz completa,
smokes, runtime headless/windowed e replays determinísticos passaram. DLL
7C6750FE5FB3B9820367E6E2A654F8779C6CB2BBF99210BDE65571BD03FE3E1A;
readiness 533D08F678639A36808D01E2D2D08CFBAF42B74B486AF5643FBAD5963FAADC23.
A ISO permanece sem escrita, com SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: buscar semântica independente de campo/atributo ou associação
estado→geometria/UV/material, sem inventar bindings.

## Avanço verificável — adjacência estrutural comando/descritor `__VB` — 2026-10-07

Foi adicionado `EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.cs`,
o comando `eagl-original-primitive-stream-command-descriptor-adjacency-audit` e
o controle independente
`tools/analysis/eagl_original_primitive_stream_command_descriptor_adjacency_control.py`.
Preservando a seção original, a auditoria observou em `2364/2364` primitivas
comandos completos cujo `command_stream_offset` é exatamente `descriptor_offset
+ 0x0C` para um descritor `__VB` validado; foram `10240` candidatos,
`2364` candidatos de identidade, `2364` identidades únicas atuais,
`2364` pares na mesma seção e `0` identidades deslocadas. O controle Python
reproduziu os mesmos contadores e status.

Isso é apenas adjacência de bytes em offsets relativos à seção. Não atribui o
delta a campo, atributo, posição, geometria, UV, material, skinning, rigging,
animação, morph, comando ou GPU. `original_semantics=not-claimed`. O readiness
registra `rendering-original-primitive-stream-command-descriptor-adjacency`
como `verified-structural`, mas o gate principal de renderização continua
`not-proven` e o readiness geral continua `NOT_READY`.

Relatórios: audit
`4DC8C24FFF144C4FE83659B1F8F8B5D5C83756637EBF5F15230DF04E3481D717`, summary
`00ABD1EF407D5FD733CB55BEB220608413AB54177584B5F1E855C57F0A56B79E`, controle
`DEFA5803260CD6CE977D0411ED325793166DA4FD7DABD02281FD56368C3D3975` e summary
`081BD81C79F80E97D33192F315E8CDAC958F8D478F38703F3997C340E13E0B14`.
O render-chain atualizado tem SHA-256
`C6C80BB202ECE7E08AD283338C40629C86421BCCF310F35DD908E793AEBFB84B` e seu
summary `36D8765FB5E3DC1E00ABDF59C8477384575F5D912A1B737F2BB1A971BCD4C8CF`.

Validação: Release `0/0`, self-test `80/80`, Python `40/40`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`CCE12ACE31724C7142EF9E338BD1E547AA362D4C957C6FBDC4CE67D044FA1877`.
Readiness: `DB51CD27DD5F9D0BCFE185B219D4987E3DFAFD066E525750820201E85C191E30`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência independente de semântica de campo/atributo
ou associação estado→geometria/UV/material, mantendo este resultado somente
estrutural e sem inventar bindings.

## Avanço verificável — gate separado de áudio candidate-only — 2026-10-06

O readiness agora registra `audio-candidate-runtime` como
`verified-candidate`: o full-stack opt-in preparou/reproduziu `8/8` eventos,
com `0` não mapeados e replay coincidente. É integração candidate-only authored;
seleção original EVT/ABKC/amostra continua não provada.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram.

DLL:
`A392BB170AD47F640D354A06F9360A9CDD97071F2194785385F90ABC028C09A7`.
Readiness:
`A694DD3E15444DEDDAB0ADA3A9EA67026497A06690CE11A9629349208C018EEE`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — gate separado de renderização candidate-only — 2026-10-06

O readiness agora registra `rendering-candidate-runtime` como
`verified-candidate`, sem alterar `rendering=not-proven`. O full-stack opt-in
validou `140/140` frames, `24` assets candidatos, `280` samples authored de
animação, `8/8` eventos de áudio, replay coincidente e framebuffer válido. É
integração candidate-only do runtime; não é binding original de estado, UV,
material ou GPU.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram.

DLL:
`C2E72072AC40D16F7B3376B5DFD91F3EE85E7C50425A264EFE6AD66B5F99F800`.
Readiness:
`0A317FA850F5D5F16C23898E8291FF1D1DCABD7D11ABBB406A50E71631068736`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — call-sites bounded do destino aninhado — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_nested_call_site_audit.py` registrou
`2` call-sites para `0x0026E278` dentro do slice de `0x0016BF90`, com `1`
destino distinto. Os dois contextos exibem `fld`, `fmul`, `call` e `ret 8`; não
há `push edx`, `lea [ecx+...]` ou `ret 4`. O relatório permanece somente
estrutural e bounded; não recupera função nem semântica.

Status `verified-bounded-static-render-indirect-indexed-nested-call-sites`;
`original_semantics=not-claimed`. Não há binding de material, UV, skinning,
rigging, animação, morph ou GPU; readiness segue `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram.

DLL:
`AA55B7737D13A532ACA1B1CDC5553B93A9A6B9DC5A439A15F6C901A74B868567`.
Readiness:
`1442E380FE5C11D1D0D702A448E3B950733A6D8D8BF534191A0B49829A65248E`.
Nested call-site audit/summary:
`4F760A2CB3F8F47BC4BD2A0AB2A03D263A481AE6C50853BE3DAFB9DD084A8E56` /
`870483285414A5E646CDC7C51DC1F23EAFC60387AD9E91005B7AB4DDC88AF15D`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — CFG bounded do destino aninhado — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_nested_call_target_cfg_audit.py`
inspecionou `0x0026E278`, único destino direto encontrado dentro do slice de
`0x0016BF90`: `89` instruções lineares, `37` alcançáveis, `7` blocos, `3`
branches condicionais, `2` incondicionais, `4` alvos internos, `1` retorno
alcançável e nenhuma chamada direta adicional. Não houve execução nem novo
seguimento de chamada; isso não é função recuperada.

Status `verified-bounded-static-render-indirect-indexed-cfg-nested-call-target-cfg-slices`;
`original_semantics=not-claimed`. Não há binding de material, UV, skinning,
rigging, animação, morph ou GPU; readiness segue `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `30/30`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram.

DLL:
`395813624F6C4AD23B4FA1D11BF44DF7C6697C2189282E32319200BCAD737982`.
Readiness:
`0F5A8B50AB5D8D04A300AB0BA883782D4D6017546400753C727EB86D03FE196A`.
Nested audit/summary:
`5E73FAF3C58277F98B08DC4F9030673E381000B6188AB5018BB7AB9B564336E3` /
`2C18CBB0DC2B53C4E4795945F0786B5011E747C12DC3CCFFB7A1FDAEF4C17D33`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — join bounded entre tabela e call-sites indexados — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_call_site_join_audit.py` cruzou,
sem executar o XBE, `81` linhas de alvos, `1` base de tabela, `24` call-sites,
`24/24` linhas joined, `1` alvo distinto, `27` índices distintos, `27`
ocorrências e `3` duplicatas sobre os call-sites. Os `24/24` call-sites
mantiveram forma local completa. O resultado é estrutural e bounded; não define
função, objeto, campo ou semântica.

Status `verified-bounded-static-render-indirect-indexed-call-site-join`;
`original_semantics=not-claimed`. Não há binding de material, UV, skinning,
rigging, animação, morph ou GPU; readiness segue `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `29/29`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram.

DLL:
`CF1E344282E9237943573EF27A7F24A1F12B662476175A819337DCF0B50A9959`.
Readiness:
`EC3F4027BE15A0829D65EF6E76E6B65E0BB3BDD68A4FC2A6B3E732D1AC7F9BBD`.
Join audit/summary:
`6742EB92434168D7A04C695BB77DCF8632AC1534ACB702E144401AA557FD81AE` /
`71BA5028FCDFBDF276848DC9FD50619EA615D08948CFB806C58D164B832FFBC9`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — CFG bounded do destino direto indexado — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_call_target_cfg_audit.py` aplicou
CFG bounded de `256` bytes ao único destino distinto `0x0016BF90`: `76`
instruções lineares, `23` alcançáveis, `7` blocos, `2` branches condicionais,
`2` chamadas diretas, `0` transferências indiretas e `1/1` `ret` alcançado.
Chamadas internas permanecem não seguidas; isso não é função recuperada.

Status `verified-bounded-static-render-indirect-indexed-cfg-call-target-cfg-slices`;
`original_semantics=not-claimed`. Não há binding de material, UV, skinning,
rigging, animação, morph ou GPU; readiness segue `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `28/28`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram.

DLL:
`06483C77B64112CCFE79F635DCE1B4252F7C1D2FD92A7BB6E6744B71B11F1FC8`.
Readiness:
`975217DC8EFDDA58C4D35A9218EF93E1D467A82701B4A85233475181AF76EB60`.
Audit/summary:
`8CBBF5C159106F6D98CE05D65A88ACD76179E863A89763202649B431D450EE77` /
`6250D59561DE79E73027693C34E8853DC33EBE4A91941AAE6ABCDFFD0AA85ED2`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — contexto bounded dos call-sites indexados — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_call_site_audit.py` preserva o
contexto local das `24` chamadas alcançáveis: `24/24` convergem para
`0x0016BF90`, com `lea` baseado em `ecx` e deslocamento distinto, `push edx`
antes da preparação e `ret 4` após a chamada. Isso é regularidade de bytes e
instruções bounded, não wrapper/função recuperada ou semântica.

Status `verified-bounded-static-render-indirect-indexed-call-sites`;
`original_semantics=not-claimed`. Não há binding de material, UV, skinning,
rigging, animação, morph ou GPU; readiness segue `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `27/27`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram.

DLL:
`FE1DEAB66AC78E1FDA154848394C6EAA3A06FB3A87E563EC5609CE0A313F1E37`.
Readiness:
`B18D55EC12C80D7888B4EC441A1315C17DF9A7BD7AA036E08EEC7C6FC794EB6C`.
Audit/summary:
`1BC39F705C0AAADFC34AAD030CFB0E29B2A87197E71A9E3E4363A34B16650474` /
`227EC2C511556952E038E9AB725B76B5E68459AA3F2BF8EF57B987BEF2219A92`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — slice bounded do destino direto indexado — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_call_target_audit.py` preserva a
distinção entre `24` ocorrências de chamada (`24` alvos únicos somados por
slice) e `1` endereço distinto global, `0x0016BF90`. O endereço mapeia em
`.text`, offset `0x0015BF90`, com janela de `256` bytes, `76` instruções e
`ret`; não houve prólogo reconhecido. Nenhuma chamada foi seguida e isso não
é função recuperada.

Status `verified-bounded-static-render-indirect-indexed-cfg-call-target-slices`;
`original_semantics=not-claimed`. Não há binding de material, UV, skinning,
rigging, animação, morph ou GPU; readiness continua `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `26/26`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram.

DLL:
`0FF23943CA552774AC25810A2B4B6D0C066F13862EB36A3D70BA787C9E44274A`.
Readiness:
`73AB08762847C2F91C282C5A4C749863B692B8DB61E9B42A850D1655C7379328`.
Audit/summary:
`41E108CCEC548410358EF649AD9F3DBB24448175CF1E4E44D9C388F9FB1463A2` /
`E329BE8965964C3995347AFF7442B7BF403E947B7D0412BDD44D97512F86414C`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — distinção de chamadas diretas e join CFG — 2026-10-06

O CFG bounded dos `81` candidatos da tabela indexada registra `24` chamadas
diretas nos slices (`24` alvos únicos somados por linha), porém somente `1`
endereço distinto global, `0x0016BF90`. É fluxo local estático: nenhum destino
foi seguido e não foram recuperados limites de função, ponteiros ou semântica.

O auditor `xbe_render_indirect_indexed_cfg_join_audit.py` compara esse conjunto
com `87` alvos diretos de renderização, `64` alvos de janela e `235` nós do
grafo bounded. As três interseções foram `0`; status
`verified-structural-negative`, somente controle negativo de endereços.
`original_semantics=not-claimed`; nenhum material, UV, skinning, rigging,
animação, morph ou GPU foi ligado. Readiness segue `NOT_READY`.

Validação final: Release `0/0`, self-test `72/72`, Python `25/25`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram.

DLL:
`361A00A077A987AF3A826CC807AF6D141D228E0698C40B4F81A8948BF6AD5F9E`.
Readiness:
`C8A4D27666203A19FE35E8300C66F1CA4B914585FB80F9329127BB77F4CDEA3F`.
CFG audit/summary:
`B2D8030BF2EA4F940AD23943B80BDF52455E909B0EEB3359EEBAAF5C6C485500` /
`9164CA4C7E7E73B9DB4D6AE213335724128854CB1E748EE8CE69798117F17FE6`.
CFG join audit/summary:
`6AEBD1BB735025409B55D76760E18722AA45F46094BEE53EA2B3FE9B96FD79FF` /
`5FAA0A61F854CC448B6B9B27BB0217A6F3DCF5430E53EEA6EC9E35AE9C4246A2`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — CFG bounded dos candidatos da tabela — 2026-10-06

O auditor `xbe_render_indirect_indexed_cfg_audit.py` aplicou alcançabilidade
bounded às `81` entradas distintas do relatório de alvos. Foram observados
`81/81` CFGs alcançáveis e `81/81` `ret` alcançáveis, com `7.295` instruções
lineares, `421` alcançáveis, `105` blocos básicos, `24` chamadas diretas
registradas sem seguimento e `0` transferências indiretas nas janelas de `256`
bytes.

É evidência de fluxo estático local, não recuperação de função, limite,
ponteiro, chamada ou semântica. Status
`verified-bounded-static-render-indirect-indexed-cfg-slices`;
`original_semantics=not-claimed`. Materiais, UVs, skinning, rigging, animação,
morphs e GPU continuam sem binding. Readiness segue `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `24/24`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram.

DLL:
`5A3E8814C7AE038932856353E65E5E2322CC326A514BBDCB44356D8E0813714C`.
Readiness:
`0C9D676A829523BFF1B62A72BFF985222213B868855D05ED7B57414B61E12D38`.
CFG audit/summary:
`3DF517090ABE885BB013E6F012A23B6CF99EF4482DA12BBF56E014B3FC9E3756` /
`57E7E7F2138EC6C6FD707D1FA5956DF542DD77AFD89F04CE7E90A28D624D21D5`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — controle negativo de interseção dos alvos — 2026-10-06

Foi adicionado `tools/analysis/xbe_render_indirect_indexed_target_join_audit.py`.
O auditor faz apenas interseção de conjuntos de endereços bounded entre os `81`
alvos distintos da tabela indexada, os `87` alvos do relatório de chamadas
diretas e os `64` alvos da janela de renderização. Os resultados foram `0`
interseções candidato↔chamada direta e `0` candidato↔janela de render.

É somente um controle negativo estrutural de endereços; não prova
independência de comportamento e não recupera função, chamada, ponteiro ou
semântica. Status `verified-structural-negative`,
`original_semantics=not-claimed`. Não foram atribuídos bindings de materiais,
UVs, skinning, rigging, animação, morphs ou GPU; readiness segue `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `23/23`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram.

DLL:
`D4289C65CD3E4FCD0953C3DD625D6EC1544E641F348821D4A96A693B74CD0ED3`.
Readiness:
`128EA296B76FBF9E49214008CAADD3E257E2E77087BA1F06414E5CDCE6C3B30A`.
Join audit/summary:
`5A8BE06EAD3A8FD27920F2151D88C262E4245D1E7A50C5789BB2F33AD79A16C9` /
`6E38D14E644E9A7034ED4B5424428FD067AE7DB8E226ED82C9E3E11CD2F5DDB1`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — slices limitadas dos alvos candidatos da tabela — 2026-10-06

O auditor expandiu as `87` ocorrências executáveis da tabela indexada e
desassemblou uma janela bounded de até `256` bytes para cada alvo distinto.
Foram observados `81` alvos distintos, `81/81` mapeados, `81/81`
desassemblados, `81/81` com `ret` na janela e `0` com prólogo reconhecido.

Isso não recupera funções, limites, ponteiros ou chamadas; são slices estáticas
candidatas. Status:
`verified-bounded-static-render-indirect-indexed-target-slices`;
`original_semantics=not-claimed`. Materiais, UVs, skinning, rigging, animação,
morphs e GPU continuam sem binding. Readiness segue `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `22/22`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos passaram.
DLL:
`0C23802D85483FA565E1BE497610922B7C160ACFD80137F963FFDE07D2039210`.
Readiness:
`55D15A89A45E91B8ED86626BF1E1C46D9F447C9FB0DC15E6D221D649C636A5AA`.
Audit/summary:
`FA7A2DB7409B43C8A42EB7E8A5FD5C5A988CD06758C75A2C71FD7A28E19C8399` /
`BD71ED8F44CB15754045E9DDF5E58F828A55D0BE47FEBEEA26B2C434EA5DBEC7`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — varredura limitada da tabela de transferência indireta — 2026-10-06

O novo auditor estático leu a base da única transferência indireta indexada
sem registrador-base identificada nas janelas de render. A leitura foi
limitada a `256` entries e terminou antes do limite após duas words não
executáveis. O resultado real foi: `1` row indexada, `1` base distinta, `89`
words legíveis, `87` executáveis, `2` não executáveis, `0` ilegíveis, `81`
words executáveis distintas e prefixo executável contíguo máximo de `87`.

É evidência estrutural de um prefixo candidato, não recuperação completa de
tabela, ponteiros, funções, tamanho, chamada ou semântica. Status:
`verified-bounded-static-render-indirect-indexed-table`; provenance:
`same-ISO-retail-XBE+bounded-read-at-indexed-indirect-table-base`;
`original_semantics=not-claimed`. Não foram atribuídos bindings de materiais,
UVs, skinning, rigging, animação, morphs ou GPU; readiness segue `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `21/21`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos passaram.
DLL Release:
`EF02D432D4C534F0AA1F572B7E3551E2B75C14B2AEB4AA7F03F278C0B72D3A7E`.
Readiness:
`0FB62081FFAB468D6FA8985C5F160BD3480FCC9E28FB5E17475211EA07BBC36D`.
Audit/summary:
`0638A839ACF1191386669065B3E714AA8AEB4F2B0928D5983B48892587B1E895` /
`58271102E9D61725E85F0AB082AB4BF99556651A5141BAF3AE548461AE5C780A`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — observabilidade explícita do áudio candidato — 2026-10-06

O relatório de integração passou a registrar separadamente a preparação de
payloads candidatos. Nos cenários reais: `17` candidatos EVT nomeados
preparados, `2.914.952` bytes PCM, `0` rejeições; `16` bancos candidatos
observados, `15` decodificados e `1` rejeitado. O caminho authored continuou
com `8/8` eventos preparados/tocados e `0` não mapeados. Isso não é binding de
evento, sample ou semântica original; `original_semantics=not-claimed` e
readiness `NOT_READY` permanecem.

Validação: Release `0/0`, self-test `72/72`, Python `20/20`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos `matched=True`.

DLL Release:
`97C05064D768A18D73F74ECC5DBA6DA36DA03024148A071E9444D73EFFD2776E`.
Readiness:
`F5A26E1A1002363CDD238BAF64382084DD0BD6C333628A2309FBA5525E3DD92F`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — interseção exata entre transferências e referências — 2026-10-06

O join passou a comparar também o par exato de endereço da janela e da
instrução. A execução real encontrou `4` janelas com interseção e `8` rows de
referência coincidentes com instruções de transferência indireta. Isso apenas
localiza observações estáticas; não recupera funções/ponteiros e não atribui
materiais, UVs, skinning, rigging, animação, morphs, GPU ou bindings.

Validação repetida: Release `0/0`, self-test `72/72`, Python `20/20`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos. Status: `verified-static-render-indirect-window-join`,
`original_semantics=not-claimed`.
DLL:
`3AD11C6CB09CE3FA2B59F317C612C049B670D3A59C8274FB63FD4D1DD5BB4E3D`.
Join audit/summary:
`59E06AC832B3CFF34DD7F00392052EC9CE83BD0C1785525F0AF8411286C8C7DD` /
`A101B0E7E24548621F6F3EC2A1CB892F996176C4D12D0131DFB3305F8B73802D`.
Readiness:
`6F0919D54C5C897041E6BAF4237455B234E4BDF7321A3910488DE6D93B54FB01`.
A ISO permanece somente leitura, `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — join estrutural das janelas de renderização e indireções — 2026-10-06

Foi adicionado `tools/analysis/xbe_render_indirect_join_audit.py` para unir
por endereço virtual as `64` janelas de render, `22` transferências indiretas
(`21` calls e `1` jump), `8` referências e `3` words brutas candidatas. As
`3/3` words têm slices executáveis, todos com `ret` alcançável; o join cobre
`15` janelas com transferências e `4` com referências.

O resultado é `verified-static-render-indirect-window-join`, com
`original_semantics=not-claimed`. É cobertura estrutural somente: não há
recuperação de ponteiros/funções nem binding inventado para materiais, UVs,
skinning, rigging, animação, morphs ou GPU. O readiness registra o subgate
`rendering-static-xbe-indirect-window-join` como `verified-structural`, sem
alterar o gate geral de renderização.

Validação: Release `0/0`, self-test `72/72`, Python `20/20`, matriz completa
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos. DLL:
`7D6D245F1E591C968F6C9D0DFECF3AD7773A3CE2DF25B36889AB987483A2E983`.
Join audit/summary:
`E10ADF511CDDFF4F1604DA4EFDBB312848FBEF2491F1E8764DD469919DB08A68` /
`233EDEB9C3F5D82FCF446742DE65F7C9585A21D47FAD29DF02D1150596FDD188`.
Readiness:
`879B82F85C986E71F1F224E8006D650F4EE91D580EC7998C30295AA1E64E41BE`.
A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — controle estrutural por histograma de relocations — 2026-10-06

O relatório `animation-morph-relocation-profile-audit.tsv` agora registra o
histograma dos gaps consecutivos de cada layout morph e a comparação por
multiplicidade com os bancos de animação. A execução real encontrou `594`
linhas de catálogo, `393/393` layouts parseáveis e `19` bancos válidos:
`53` layouts/`159` pares continuam contendo o conjunto de gaps, mas não houve
correspondência exata nem por conjunto (`0`) nem pelo histograma (`0` layouts,
`0` pares).

É evidência estrutural negativa/controle de recorrência, não binding. Nenhuma
semântica de frame, pose, timing, rigging, skinning, morph, material, UV,
comando GPU ou animação original foi atribuída; `original_semantics=not-claimed`.

Validação: Release `0/0`, self-test `72/72`, Python `19/19`, matriz completa
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos. O readiness segue `NOT_READY`.

DLL Release SHA-256:
`9776FD7946552D26CABF479C2DF5EA145C5CD76317A119C2FAF4F94C86276B51`.
Relatório/resumo SHA-256:
`D30560728E8BC701D1765558FF012D3CC36DF72E89421E255F6D6F1845379691` /
`FF42635A3E711AD2644BD9946A129541C7A99FC8E17DD781E53C18344FD8D9A2`.
ISO: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — startup do host Win32 Release — 2026-10-06

O executável Release `fifa_pc/bin/Release/net8.0/FifaStreetPc.exe` iniciou o
modo `run-windowed` com personagens clean-room e material candidate opt-in.
Durante o smoke, o host permaneceu ativo e confirmou
`candidate_material_status=validated-candidate`,
`render_characters=clean-room-procedural`, os slots home/away candidate
habilitados e `texture_decodes_validated=2`. O harness interrompeu o processo
após a observação (`EXIT_CODE=1` é somente o código do interrupt).

O resultado é `verified-windowed-startup-only`: não houve captura visual
independente e não foi atribuído binding original de mesh, material, UV,
skinning, rigging, animação, morph, GPU ou áudio; `original_semantics=not-claimed`.

## Avanço verificável — captura determinística do framebuffer Win32 — 2026-10-06

O modo `run-windowed` agora aceita `--run-seconds` e
`--capture-frame <saida.ppm>`. O smoke Release com personagens clean-room e
material candidate opt-in terminou naturalmente com `EXIT_CODE=0`, gravou
`fifa_pc/windowed-runtime-capture.ppm`, confirmou P6 `960x540`,
`1.555.215` bytes e checksum `0F83CEFDD78C58BA`. SHA-256 da captura:
`E1D1FE0294EA554F601F384C15D87B3FEA4C4649BE93EC2527BC10CA8269AD15`.

Status: `verified-windowed-frame-capture`, proveniência
`managed-software-framebuffer`. Isso prova somente que o loop Win32 chegou à
rasterização e produziu uma imagem não uniforme; não é screenshot independente
nem binding original de mesh, material, UV, skinning, rigging, animação, morph,
GPU ou áudio. `original_semantics=not-claimed` permanece.

## Fechamento verificável da etapa — 2026-10-06

Build Release: `0` avisos/`0` erros; self-test: `72/72`; testes Python:
`11/11`; matriz authored/candidate opt-in: concluída com smokes, runtime
headless e replays determinísticos `matched=True`.

DLL Release SHA-256:
`FF3514309EEB796CD009FED5B9D9B1A4A48E2A5C4B2B3A92C660085B85503917`.
Readiness: `NOT_READY`, SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
A ISO permaneceu sem escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — trace authored explícito de TACKLE

Foi criado o subgate `gameplay-tackle-trace`. O trace e replay do cenário têm
`5/5` eventos idênticos: `KICKOFF`, `STEAL`, `TACKLE`, `STEAL` e `SHOT_AWAY`.
O resultado é `verified-authored`; não é evidência de que `TACKLE` seja uma
regra ou binding original.

O próximo requisito permanece evidência independente do controle defensivo e
da transferência de posse. Trace e replay: SHA-256
`AC986AB9737AD4A498B9353C693B624C646EE8901208B0C5FCDC63C530552B53`.

Validação: Release `0/0`, self-test `72/72`, testes Python `19/19`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. A ISO manteve `2013003776` bytes,
`Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — slices dos words candidatos de indireções

`tools/analysis/xbe_render_indirect_reference_slice_audit.py` disassemblou,
em janelas bounded de `256` bytes, os `3` words brutas distintas obtidas do
inventário de `8` referências. Resultado: `3/3` slices disassemblados, `3/3`
com `ret` alcançável e `0` prologue-like claims.

É evidência estrutural somente: nenhum word foi executado, nenhuma chamada foi
seguida, nenhum limite de função foi declarado e nenhum binding de geometria,
material, UV, skinning, rigging, animação, morph ou GPU foi criado. O subgate
`rendering-static-xbe-indirect-reference-word-slices` está
`verified-structural`; renderização continua `not-proven` e
`original_semantics=not-claimed`.

Relatórios: `fifa_pc/xbe-render-indirect-reference-slice-audit.tsv` e
`fifa_pc/xbe-render-indirect-reference-slice-summary.tsv`. SHA-256, na ordem:
`16FB2AA7CDC423A1C70D4588C6C5731FD5FACEEE1D59F7BDB0E3A3ACA253444C` e
`4175B05F115DCD37BC9048C37369D8A3E42F12B30F2128E7AB3464BD64C67AF5`.

Validação: Release `0/0`, self-test `72/72`, testes Python `19/19`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. A ISO manteve `2013003776` bytes,
`Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Correção da etapa — readiness do trace aimed de input

O readiness agora prefere os arquivos `gameplay-authored-aimed-input-trace.tsv`
e `gameplay-authored-aimed-input-replayed.tsv`, produzidos pela matriz atual,
mas conserva fallback para os fixtures `gameplay-clean-room-*` do self-test.
Isso corrige a divergência de cabeçalho causada pela coluna authored `tackle`.

O subgate `gameplay-aimed-input-trace` está `verified-authored` com `80/80`
amostras; a amostra de `frame=67` contém `move_y=0.75` e `shoot=True`.
Isso não promove `MoveY` a semântica original: continua sendo apenas trace
determinístico do runtime clean-room.

Validação: Release `0/0`, self-test `72/72`, runtime aimed de `80` frames,
replay determinístico de match/input/animação/áudio com todos os resultados
`matched=True` e testes Python `18/18`. O input trace e o replay têm SHA-256
`0A1D6B3B30E96B3ECA8A66A0811119AC03A82130914315B547C9276505B010A4`.
Readiness geral: `NOT_READY`; ISO inalterada com `2013003776` bytes,
`Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa seguinte — words referenciadas por indireções do renderizador

`tools/analysis/xbe_render_indirect_reference_audit.py` leu, de forma bounded
e somente estrutural, as referências de memória sem base explícita encontradas
no inventário anterior de `22` transferências indiretas. Foram identificadas
`8` referências: `7` absolutas e `1` base de tabela indexada. As `8/8` palavras
foram lidas em limites válidos e `8/8` estão em seções executáveis; são `3`
endereços de referência e `3` words brutas distintas.

O resultado não segue alvos, não executa chamadas e não cria binding para
geometria, material, UV, skinning, rigging, animação, morph ou GPU. Os words
são apenas endereços/caminhos candidatos, com `original_semantics=not-claimed`.
O subgate `rendering-static-xbe-indirect-reference-words` está
`verified-structural`; o gate integrado de renderização segue `not-proven`.

Relatórios: `fifa_pc/xbe-render-indirect-reference-audit.tsv` e
`fifa_pc/xbe-render-indirect-reference-summary.tsv`. SHA-256, na ordem:
`9E32F0744ACB8BDA565993876F81F2EDE789C574F08FA56BAA198524E0B25836` e
`3FF5EBDE33A7ECB8A96BCE8AD8B4CB0609C95BE0FA682DBE56BD8ED7796BE38A`.

Validação: Release `0/0`, self-test `72/72`, testes Python `18/18`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. A ISO manteve `2013003776` bytes,
`Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa seguinte — transferências indiretas de renderização — 2026-10-06

O novo auditor estático percorreu os 64 CFGs bounded já selecionados e
inventariou `22` `call`/saltos indiretos alcançáveis em `15` alvos: `21` calls,
`1` jump, `21` operandos de memória e `1` de registrador. Sete operandos usam
memória absoluta e foram observadas `11` formas distintas.

Isso é evidência estrutural do XBE retail, com operandos x86 preservados sem
desreferência. Não resolve ponteiros nem limites de função, não executa o XBE e
não cria bindings de materiais, UVs, skinning, rigging, animação, morphs ou GPU;
`original_semantics=not-claimed`. O subgate
`rendering-static-xbe-indirect-transfers` é `verified-structural`, enquanto o
gate integrado de renderização permanece `not-proven`.

Relatórios: `fifa_pc/xbe-render-indirect-transfer-audit.tsv` SHA-256
`265A0ACC8B3369F34D68EE5FD5563587978977CD83097C1EEBDF5ED743F7E926` e resumo
`37D34311B33DD25DB1CB034DB2D0C3733D0AB843313D405D22647728087C7F6E`.
Release `0/0`, self-test `72/72`, Python `17/17` e matriz completa passaram.
ISO inalterada: SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — controle defensivo authored `TACKLE` — 2026-10-06

O runtime managed clean-room agora possui `MatchInput.Tackle`, coluna 10
opcional nos scripts, tecla `K` no host Win32 e um demo determinístico de 100
frames. A regra authored só atua na borda do input quando o adversário tem a
posse e está dentro do raio de posse: transfere a posse, aplica 0,20 s de
proteção e adiciona 10 ao medidor. Não é uma reconstrução do input, colisão,
animação, áudio ou semântica do título original.

`TACKLE` reutiliza apenas o perfil defensivo authored de `STEAL` e a rota
clean-room `TACKLE -> tackle`; a rota de áudio segue candidate-only. Não foram
criados bindings de materiais, UVs, skinning, rigging, animação, morphs ou GPU.

Validação: cenário `KICKOFF → STEAL → TACKLE → STEAL → SHOT_AWAY`, 10/10
cenários authored, 54 eventos gameplay/áudio, 54 preparados, 54 reproduzidos,
777 frames, 1.554 amostras de animação e replays determinísticos coincidentes.
Release `0/0`, self-test `72/72`, Python `16/16`, matriz
`completed-authored-and-candidate-opt-in`. Readiness continua `NOT_READY`:
assets candidatos, renderização original `not-proven`, áudio geral parcial e
animação estrutural-only.

ISO preservada somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
`fifa_pc/project-readiness.tsv` tem SHA-256
`4E4991215D3E53EE1EAC27CBF5D5D5B813A112F14096A20B5549DDBA620930F6`; DLL
Release: `EAB3E92D2ACE4C11F2D6FFFDFFB9281F50643F311AA22541E5236A73CD0636DD`.

## Registro da etapa — slices estáticos one-hop de alvos de chamada do renderizador

`tools/analysis/xbe_render_call_target_audit.py` foi integrado à matriz.
Partindo dos 64 CFGs limitados, a análise encontrou `42` janelas com
chamadas diretas e `87` alvos diretos únicos; todos os `87` foram mapeados e
disassemblados, `75` terminaram por `ret` e `28` têm prólogo semelhante a
função. Chamadas não são seguidas recursivamente; transferências indiretas
continuam sem resolução.

O relatório é evidência estrutural one-hop, sem execução do XBE, sem limite de
função comprovado e sem binding para geometria, materiais, UVs, skinning,
rigging, animação, morphs ou GPU. O subgate
`rendering-static-xbe-call-target-slices` é `verified-structural`, enquanto o
gate principal de renderização permanece `not-proven`; `original_semantics`
continua `not-claimed`.

Validação da etapa: Release `0/0`, self-test `72/72`, testes Python `15/15`,
matriz completa `completed-authored-and-candidate-opt-in`, smokes, runtime
headless, replays determinísticos e captura windowed. A ISO ficou inalterada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Regressão Win32 incorporada à matriz — 2026-10-06

A matriz `tools/run-clean-room-authored-matrix.ps1` agora inicia o host
`run-windowed` por dois segundos com os flags candidate/clean-room explícitos e
valida automaticamente a captura PPM: `header=P6 960x540`,
`bytes=1555215`, `nonuniform=True`. É uma regressão do framebuffer managed,
sem promoção de bindings ou semântica original; os gates originais continuam
separados.

## Readiness após o subgate Win32 — 2026-10-06

O auditor de readiness agora registra `rendering-authored-windowed-runtime`
como `verified-authored`, `1/1`, usando a captura PPM persistida. O relatório
atual permanece `NOT_READY`: o gate original `rendering` continua
`not-proven`, `assets` continua candidate-only, `audio` continua parcial e
`animation-deformation` continua structural-only. A captura é apenas
`provenance=managed-software-framebuffer`, com
`original_semantics=not-claimed`.

Artefatos correntes após o build Release e a matriz:

- DLL Release — SHA-256
  `9CCE2B05D2B76B64BEB685630D7DB30F94EC267317CBF1685C7231F8017546C1`.
- `project-readiness.tsv` — SHA-256
  `ACF4212182C6BE8F44D07FFE424B76D3C82EA7E1A411D9512040F25DFC6E547F`.
- `windowed-runtime-capture.ppm` — SHA-256
  `E1D1FE0294EA554F601F384C15D87B3FEA4C4649BE93EC2527BC10CA8269AD15`.
- ISO preservada, `2.013.003.776` bytes, atributo `Archive`, SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificável — contexto estático de renderização no XBE — 2026-10-06

O novo `tools/analysis/xbe_render_static_cluster_audit.py` agrupou, somente
com os xrefs e contextos locais do XBE, `494` referências, `88` literais
distintos, `3949` linhas de contexto, `3917` contextos com chamada executável
e `854` destinos executáveis distintos. O readiness registra
`rendering-static-xbe-context=verified-structural` (`88/1`).

A classificação é apenas estática/coocorrente, com
`retail-XBE-static-xref+local-call-context; no-execution; no-binding`; não há
recuperação de função, decodificação GPU ou atribuição de mesh, material, UV,
skinning, rigging, animação ou morph. O gate original `rendering` continua
`not-proven`; `original_semantics=not-claimed` permanece explícito.

Validação: build Release `0/0`, self-test `72/72`, testes Python `12/12` e
matriz completa authored/candidate com runtime, replays, smokes e ISO
preservada aprovados.

Hashes: literal audit
`8575E3823B9AE7A48BC81594832BF833B5532DE6F25A29B078924C774803CA06`;
clusters `FDA08E3F34F11FB4C4F9B17DCF8AD6FA2477D52CD30DF8E84831F356DA01DE59`;
resumo `DF4959A878D1B9A901C0EF70CBADE437F7116287BCA5368D0DAA4E11D4E820E3`;
readiness `651F94A67005984CB2D586A6D0FA0090BFCFBD722A37ED19F7E7DB8FCFABE3BE`;
DLL Release `53963535363931315FCA1511EAC9F38F6925AE6C019788086872BD30C1C59176`.

## Avanço verificável — cross-check textual XBE→XDVDFS — 2026-10-06

O novo `tools/analysis/xbe_asset_literal_audit.py` cruzou, em modo somente
leitura, literais de caminhos do `default.xbe` com a árvore XDVDFS da própria
ISO. Foram encontrados `108` literais únicos (`702` ocorrências), com `84`
correspondências textuais (`81` por prefixo `data/` e `3` por basename único).
Isso é evidência textual de presença e não prova de uso, carregamento ou
binding de mesh, material, UV, áudio, evento, animação, rigging, skinning,
morph ou GPU; `original_semantics=not-claimed` permanece explícito.

Relatório `fifa_pc/xbe-asset-literal-audit.tsv`, SHA-256
`95C13FC106CA4F24586AD972DA8A5189B05DE80F9FE5C45612FCD4E2334550AA`;
resumo `fifa_pc/xbe-asset-literal-summary.tsv`, SHA-256
`5F9731B918075E217BF63D66761228D2B43F10F4712482FB0BEB4D7B37739580`.

## Avanço verificável — cross-check textual XBE→entradas BIG aninhadas — 2026-10-06

`tools/analysis/xbe_asset_nested_literal_audit.py` percorreu, sem escrever na
ISO, os índices BIG de topo e as entradas `.big` aninhadas alcançáveis por
RefPack limitado. Foram indexados `4.796` caminhos internos únicos; `13/108`
literais do XBE tiveram correspondência textual (`10` únicas e `3` ambíguas),
incluindo `models/skel0000.ord` e `models/shdw0001.ord`. As ambiguidades
abrangem `pitch.xsh`, `standard.xsh` e `stadium.big`. O resultado é somente
proveniência textual de índice/nome e não cria binding de mesh, material, UV,
áudio, evento, animação, rigging, skinning, morph ou GPU;
`original_semantics=not-claimed` permanece explícito.

Relatório `fifa_pc/xbe-asset-nested-literal-audit.tsv`, SHA-256
`8570C2962A3BC5C7820111A8B56324D8E3DBD3784B4C81A6013EACD053A364CA`;
resumo `fifa_pc/xbe-asset-nested-literal-summary.tsv`, SHA-256
`9B0E93677A6D0CB2B30DB57D2907ECFF5274B26F18B7DF7BA2F8769C9348551C`.

## Avanço verificável — join de entradas BIG com xrefs estáticos do XBE — 2026-10-06

`tools/analysis/xbe_nested_literal_xref_audit.py` juntou as `13`
correspondências BIG com `xbe-string-xref-audit.tsv` e seu contexto de chamadas
locais. O resultado contém `8` literais com xrefs exatos, `10` xrefs, `70`
linhas de contexto e `24` destinos executáveis distintos para a ocorrência
ambígua de `stadium.big`. É uma coocorrência estática/candidata, não uma
recuperação de função ou binding; `original_semantics=not-claimed` permanece.

Relatório `fifa_pc/xbe-nested-literal-xref-audit.tsv`, SHA-256
`63418DEF893A32A5401D2993481CE11CB2A17215A7E19A860413181D11C4CEA2`;
resumo `fifa_pc/xbe-nested-literal-xref-summary.tsv`, SHA-256
`6B84A9CBA014F2148D3E290443C15A3DB2384BA09DDA9D1DB28F057E0279A409`.

## Avanço verificável — clusters estáticos de áudio no XBE — 2026-10-06

O novo `xbe_audio_static_cluster_audit.py` reuniu literais de áudio, xrefs e
contexto local de chamadas sem executar o XBE. Foram observados `58` xrefs,
`23` literais, `147` contextos, `108` contextos com destino executável,
`56` destinos e `14` destinos com múltiplos literais. Apenas `1` literal teve
correspondência BIG aninhada (`audio/tunavars.txt`) e `10` tiveram caminho
XDVDFS textual. Nenhum literal `.evt` apareceu nos xrefs selecionados; isso é
apenas um controle negativo textual e não prova ausência de eventos binários.

Relatórios: `fifa_pc/xbe-audio-static-literal-audit.tsv`,
`fifa_pc/xbe-audio-static-call-clusters.tsv` e
`fifa_pc/xbe-audio-static-summary.tsv` (SHA-256 disponíveis nos próprios
artefatos e no handoff principal). A classificação é
`verified-static-audio-literal-context`; não há recuperação de função,
seleção de amostra, binding gameplay→evento→ABKC ou semântica original.
`original_semantics=not-claimed` permanece explícito.

## Avanço verificável — inventário dos payloads BIG nomeados pelo XBE — 2026-10-06

`xbe_nested_payload_inventory.py` seguiu as correspondências BIG sem escrever
na ISO e registrou `41` caminhos, `32` hashes distintos e `15` wrappers
RefPack. Foram observados `2` payloads ELF, `2` sidecars ELF `.orl`, `1` BILG,
`20` SHPX, `2` SHPX RefPack e `12` índices BIG RefPack. Os pares
`models/skel0000.ord/.orl` e `models/shdw0001.ord/.orl` permanecem somente
evidência estrutural de bytes/caminho; não foi atribuído significado de mesh,
rigging, material, UV, skinning, animação ou GPU.

Relatórios: `fifa_pc/xbe-nested-payload-inventory.tsv` (SHA-256
`232235B37AACC2A943E1EFFFCBE853F99E3A3F281C5E16A705E0241665709BC9`) e
`fifa_pc/xbe-nested-payload-summary.tsv` (SHA-256
`668F8FCEEBDFA76CC2770B663A78DB7C9D413D3FAEE8570E57E28562401004C8`).
Status `verified-nested-payload-inventory`; `original_semantics=not-claimed`.

## Avanço mais recente — readiness consumindo o subgate de payloads EVT — 2026-10-06

O readiness agora registra `audio-named-event-runtime=verified-candidate` com
`17/17` candidatos, metadados e payloads preparados e cabeçalhos WAVE válidos.
Isso permanece separado do gate original: `audio` continua
`partially-integrated`, `gate_passed=false`, `direct_selection_binding=not-proven`
e `original_semantics=not-claimed`.

Após a alteração, Release `0/0`, self-test `72/72`, matriz authored/candidate,
smokes, runtime headless e replays determinísticos passaram. Full-stack
authored gerado/replay: SHA-256
`727F8A81A11F2F5798E6A5E2C7488C938C98DEF0A2CCC0E7179F1A0273D8072D`; audit
candidate: `DEF0B77FEF42B1474E58ED06CEC471BDBDB1502741D2C6CF3DCD8F9D07E8E6EB`;
DLL Release: `A17E88262121F0C0057DE43D01F205237016F491A1BB69908BA7582E62EC5975`.
O relatório full-stack persiste `opponent_ai_enabled=True`; o readiness exige
esse marcador como evidência da IA authored do runtime, sem reivindicar regras
originais.
Readiness `NOT_READY`, SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
A ISO continua sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — payloads EVT nomeados preparados no runtime managed — 2026-10-06

Foi adicionado à matriz o audit `audio-named-event-runtime-audit`. Resultado
real: `17/17` candidatos EVT nomeados com metadados válidos, `17/17` preparados
no runtime managed, `17/17` cabeçalhos RIFF/WAVE válidos e `2.914.952` bytes
PCM. Relatório `fifa_pc/audio-named-event-runtime.tsv`, SHA-256
`1F4D60065756162D29E665DCB794D9D31F34FACC88DBD5FC38E16D70E8503CDF`; resumo
`fifa_pc/audio-named-event-runtime-summary.tsv`, SHA-256
`DFA25F7E59B247C836879445140D152DA65E7613534013CC52E3B7B7057211D5`.

O status é `verified-runtime-named-payload`; a proveniência é
`exact-EVT-name+parallel-data-range; managed-runtime-named-payload-only` e
`original_semantics=not-claimed`. Isso não prova a associação gameplay→evento,
evento→amostra, seleção ABKC ou semântica original. O gate de áudio permanece
`partially-integrated`/`NOT_READY`; assets continuam candidate-only, rendering
not-proven e animation-deformation structural-only.

Build Release `0/0`, self-test `72/72`, smokes, runtime headless, matriz
authored/candidate e replays determinísticos passaram. Full-stack authored
gerado/replay: SHA-256
`40C04455E9090663DE6F5ED3C950491B6D46043FD5BC472C56CC337B70B32B06`; DLL
Release: `965FA17D8B58B56688351A3183B5EB41ED0DBA64F4E1F327D4669E7B1CC26A2F`.
Readiness permanece `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
A ISO não foi escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — completude dos streams candidate — 2026-10-06

O audit candidate passou a registrar a completude dos streams de comandos de
geometria e dos streams de estado `SHAPENAME`. A execução real confirmou
`candidate_geometry_streams_complete=24` e
`candidate_shape_state_streams_complete=24`; o audit de runtime exige os dois
marcadores. Isso é evidência estrutural candidate-only e não atribui binding
original de estado→geometria, material, UV ou GPU.

Release `0/0`, self-test `71/71`, matriz authored/candidate, smokes, runtime
headless e replays determinísticos passaram. Full-stack gerado/replay:
`588AD564F93E4B2BB048FB121ABF9D0C76A310F69643DB4A0A0AE3AD7730FB49`;
relatório candidate:
`CB3364DC07F8FB90817BBB473F39D637256256AC210B6DDA747BC412B8B66863`;
resumo:
`00F678130853D38B3F2AB190B96F77B1CA850C81F13788ECF230B4BDC90A207D`;
audit integrado:
`22A7DC9360E35839C8581ECF1553A9418CFED637B0AE6DF38FC1426E00F48E3B`;
DLL Release:
`75137D91EECCAB5444E8FE40D5564F5FFEB95313639DAFDB1B982501819855CF`.
Readiness permanece `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
A ISO não foi escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — audit candidate consumindo proveniência estrutural — 2026-10-06

O `candidate-integrated-runtime-audit` agora consome o resumo de proveniência
estrutural antes de validar o runtime candidate. A execução verificou
`candidate_asset_provenance_valid=True`,
`candidate_mesh_state_crosscheck_valid=True` e `24/24` candidatos, mantendo
`original_semantics=not-claimed`.

Release `0/0`, self-test `71/71`, matriz, smokes, runtime headless e replays
passaram. Full-stack gerado/replay: SHA-256
`9569E64F059CFE2E1070448886786D19335996759054FD9B8277CF49327B071D`; audit
candidate: `42A23A7F851D59A00FACE9E72C0AF28A65DA4F5FDCC5278FD283EABE228BC97E`;
DLL Release: `43E2F4FB1C9E61F3734DF3FDACE9FA5CFD6A5FF40FB272A9CB41DFDA5AEA061A`.
Readiness `NOT_READY`; a ISO não foi escrita e mantém SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — cross-check candidate de malha e estado — 2026-10-06

O auditor candidate foi ampliado para reler os objetos `.ord/.orl` por trás
dos 24 vínculos. A execução confirmou `24/24` objetos, `24/24` companions,
bounds válidos, geometria finita/indexada e `SHAPENAME` nos primitivos em
`24/24` casos. Relatório SHA-256:
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo:
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.

Release `0/0`, self-test `71/71`, matriz completa, smokes, runtime headless e
replays determinísticos passaram. Full-stack gerado/replay: SHA-256
`5B42B2CCB1A548D0B6EDA60DF1E0DD05B4D73E06CDF9C287CE24165555B02A73`; DLL
Release: `77921A6EFF6715DD13B808C489D75FB465B351FE3D8FB84466DC00599F3FA05C`.
Readiness `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
O resultado continua candidate-only, com `original_semantics=not-claimed`,
semântica de estado→geometria/material/UV/GPU não atribuída e ISO preservada
com SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — cross-check persistido SHPX/TGA — 2026-10-06

O comando `shpx-export-crosscheck` foi implementado com self-test, smoke na
matriz e documentação. A leitura somente leitura de
`data/aptanims/player1.xsh` comparou 46 TGAs persistidas em
`exports_fifa_layer3/player1`: `46/46` presentes, `46/46` cabeçalhos válidos e
`46/46` pixels RGBA idênticos. Relatório SHA-256:
`8EB31145E1A05CDED83C3DEA703BD1A9B24755260A650F672FC34545118AC68E`; resumo:
`B126B7D0AEA480A299F207BC6F688D82B1A95D769EEFFB58F19F063DDC3FDD4F`.

Validação final: Release `0/0`, self-test `71/71`, matriz authored/candidate,
smokes, runtime headless e replays determinísticos passaram. Os relatórios
full-stack coincidem, SHA-256
`E65191996143C3F04C856473EB23000E0CD8C96BE936B70D810BEA308C7FCA2C`; DLL
Release SHA-256
`1F02DE91890B648AAF5605FAE605A2AE20149CEA37613EA493A0892F716E33CF`.

Esse resultado verifica somente pixels de TGAs geradas pelo workspace,
`export_provenance=workspace-generated-TGA; provenance-not-independent`, e
mantém `original_semantics=not-claimed`. O readiness segue `NOT_READY`, sem
bindings originais de timing, pose, rigging, skinning, animação, morph,
material, UV ou GPU. A ISO preservada mantém
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço atual — proveniência da cadeia candidate EAGL/SHPX — 2026-10-06

O comando `eagl-candidate-asset-provenance-audit` verificou os 24 vínculos
same-trunk: 24/24 nested `.big`, 24/24 `.xsh`, 24/24 payloads válidos,
24/24 entradas SHPX exatas e 24/24 matches RGBA após segunda decodificação.
Relatório SHA-256:
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo:
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.

O mesmo cross-check releu `24/24` objetos `.ord` e `24/24` companions `.orl`,
com bounds, geometria finita/indexada e `SHAPENAME` nos primitivos em `24/24`.
Isso é evidência estrutural candidate-only; não cria binding de estado,
material, UV, GPU, skinning, rigging, animação ou semântica original.

O resultado é `verified-candidate-source-chain`, com
`candidate_provenance=same-trunk-source-chain; candidate-only` e
`original_semantics=not-claimed`; não cria bindings originais.

## Fechamento verificável da etapa SHPX — 2026-10-06

Build Release `0/0`, self-test `69/69` e matriz authored/candidate concluída
com sucesso. O par full-stack gerado/replay coincide byte a byte, SHA-256
`05B6A34950B6D9462DAF6561CC03041D19FB5203A8E401A2693F054D9AD3281F`; o DLL
Release tem SHA-256
`DA9E629F205087991C1B995D52E41D41E301A12094A86F2DEBD013CE8C22E476`.

`shpx-image-entry-audit.tsv` tem SHA-256
`E220D9811A04BC323B899AF21C339F64E5ED5B23A9A513599B12D3D9AF5F6859` e
validou `46/46` entradas PAL8 128x128. O readiness permanece `NOT_READY`,
SHA-256 `A23E61B9474FBD07E409F0D2ED2B08CD5DC94E884429F25363CAD6289DA35222`.
O relatório candidate atualizado tem SHA-256
`7295054F7C6AFECB0B919AF1EF9BE6CDA870052BBC228BB723B879397A26BB6B`.

A ISO permaneceu intacta: `2013003776` bytes, `Archive`, timestamp preservado e
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização verificada — bateria final da etapa — 2026-10-06

Release: `0/0`; self-test: `68/68`. A matriz executou nove cenários authored
e um cenário candidate opt-in; os dez pares de replay determinístico de match,
input, animação e áudio deram `matched=True`. O cenário candidate teve 140
frames, 280 samples, 8 eventos de áudio, 8/8 preparados/tocados e 0 sem rota;
os relatórios de integração coincidem byte a byte, SHA-256
`24561111E7BCE6CE2A11171F0960F048FA57076677C8CCB432A3209CF1CA257B`.

Gate agregado `clean-room-authored-runtime`: `verified-authored`, `9/9`.
Readiness: `NOT_READY`, TSV SHA-256
`A23E61B9474FBD07E409F0D2ED2B08CD5DC94E884429F25363CAD6289DA35222`.
ISO sem escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

A etapa pode ser reproduzida com
`D:\projeto zeta\tools\run-clean-room-authored-matrix.ps1`, que executa a
matriz authored de nove cenários e o cenário candidate, as auditorias (incluindo
`candidate-integrated-runtime-audit`), os smokes relevantes, readiness e a
verificação de integridade da ISO.

O relatório gerenciado candidate é
`fifa_pc/candidate-integrated-runtime-audit.tsv`, SHA-256
`9ABDABC1246F4D161A498609F698CC3AB05FBFB10315BF2F81C08EF995E06A6D`.
Ele confirma flags, hash do assembly, PPM, traces e replays como evidência
candidate; `original_semantics=not-claimed` permanece explícito.

O readiness agora cruza esse TSV gerenciado no gate separado
`assets-candidate-runtime`, verificado em `24/24` com `passed=True`. Os gates
originais `assets` e `rendering` permanecem sem promoção:
`candidate-integrated`/`False` e `not-proven`/`False`.

## Atualização verificada — intake de timing externo — 2026-10-06

O comando `external-animation-timing-audit` valida um TSV externo de
observações temporais sem atribuir seus campos a frames, poses, rig, skinning
ou semântica original. Ele registra hash do arquivo, proveniência,
monotonicidade por captura e repetição por par banco/asset.

Não existe esse TSV no workspace atual. O resultado foi
`status=missing-evidence`, `0` observações e `0` capturas, mantendo
`direct_timing_binding=not-proven` e `original_semantics=not-claimed`.
Relatório SHA-256:
`3E82F4FF84E4095CA2844950A97A56FB5F165954BF3B3379A2BD4ED0DC026EDC`;
resumo:
`4FF80AD21FB2848191CC38519BE7E6BA9E86E3B34C23FA259879B8908836928E`.

## Atualização verificada — integração candidate opt-in — 2026-10-06

Foi adicionado à matriz um cenário cross-system separado com material candidate
e os dois slots candidatos. Match, input, animação e áudio deram
`matched=True`; foram verificados 140 frames, 280 samples, 8 eventos,
8/8 preparados/tocados e 0 sem rota. O checksum de render foi
`088EC5D10096A738` e houve 69 checksums distintos.

Os relatórios de integração gerado/replay coincidem, SHA-256
`F8F03C7DC83BFD33EAD3D58526C1B19A1790A44209C7BF938819F55AB754187A`.
É somente uma rota candidata opt-in; `render_group_state=unproven` e
`original_semantics=not-claimed` continuam registrados.

## Atualização verificada — auditoria de metadata temporal dos catálogos AXT — 2026-10-06

`animation-bank-catalog-timestamp-audit` comparou o timestamp numérico com a
data textual presente na mesma linha `.axt`: `19/19` bancos e `1.584/1.584`
linhas tiveram correspondência exata, com offsets locais `-420` e `-480`
minutos. Isso é metadata do catálogo, não timing de frame, pose, rig ou
animação original; `direct_timing_binding=not-proven` permanece explícito.

Relatório SHA-256:
`611B60012B5F37A639E2DC5AD47E829A6178FEA409A9D6CE4CA1360640222C32`; resumo:
`E167D7254E82C36D6A49276B2721A828FBD52EA048CBADAFEBCE6E33959D37FE`.
DLL Release usada na matriz: SHA-256
`D0660B1FEF4F2781152EDB95797145201F99FA4A2E0780B914CF031F0713E459`.
Readiness atual: `NOT_READY`, SHA-256
`282D82CC21E3EE600D6DCDFF6A95B754579AB9975D2F02F661BCB039F4DC1339`.

## Atualização verificada — correlação exhaustive de áudio — 2026-10-06

O auditor percorreu todos os índices de som candidatos com limite de `512`
por banco: `368` candidatos em `16` bancos, `239` decodificados, `0` pares
PCM idênticos e `0` hashes de evento correspondentes. É evidência estrutural
negativa, não binding de evento/sample nem semântica original.

Relatório SHA-256:
`BDE7CF7E58E09E17D2BBB8932E413249D51264424E5A1C32E8B5E30D404E345E`;
resumo SHA-256:
`DD798636EC35EF78E07E47176A0220C9A919601F2FF47D5CD51FE8DB41DEF23B`.

## Atualização verificada — invariantes de gameplay authored — 2026-10-06

O auditor `clean-room-gameplay-invariant-audit` verificou `9/9` cenários
authored, `677` frames e `49` eventos. Cada cenário passou pelos controles de
estado finito, bounds de posição e altura, relógio/placar monotônicos, ordem de
eventos, condição de término e ações requeridas.

Relatório `fifa_pc/clean-room-gameplay-invariants.tsv`, SHA-256
`B6C4F29036835A22BFB36D6401F2C31A37B6C66DC62D3EB477A8C5396C6433EE`.
É evidência do runtime clean-room authored, sem promoção de regras originais;
`original_semantics=not-claimed` e readiness `NOT_READY` permanecem.

## Atualização verificada — correlação estrutural de áudio ampliada — 2026-10-06

O auditor `audio-event-payload-wave-correlation` passou a aceitar
`--max-bank-attempts`; a medição com `64` tentativas de banco decodificou
`17/17` candidatos EVT e `15/16` bancos ABKC/AST. Não houve pares PCM idênticos
nem hashes de evento com correspondência (`0/0`), e o resumo registra
`bounded_policy=first_64_bank_candidates`.

Relatório SHA-256:
`8ED4E39C6748CEC54706D9F107DCFE7D887C941ED43FDF854829C60568104E3D`;
resumo SHA-256:
`4D949566DB53FA348C939388D280D94C9F825BB19BB19BA6B31FAA70C2C2A38D`.
O resultado é estrutural/negativo; `direct_selection_binding=not-proven`,
o gate original de áudio continua parcial e `original_semantics=not-claimed`.

## Atualização verificada — cenário first-to-goals integrado — 2026-10-06

O cenário authored `first-to-goals` foi integrado ao smoke cross-system:
`80` frames, `HOME 1 - 0 AWAY`, `FULL_TIME`, `7` eventos de match/áudio e
`160` samples de animação. Os quatro replays deram `matched=True` e o frame
PPM authored foi validado.

Relatórios de integração gerado/replay, SHA-256
`D37356F7DE6F7EE9F04FFB83BC1D5973F6C278C1F3EAAC9C59D3D98A3089F119`.
Gameplay e áudio authored agora cobrem `9/9` cenários, `49/49` eventos e `0`
sem rota; isso não reivindica semântica original.

## Atualização verificada — cobertura integrada authored por cenário — 2026-10-06

O comando `clean-room-integrated-coverage-audit` verifica os nove cenários
authored com renderização, personagem/cena authored, animação, áudio e quatro
replays determinísticos. O resultado foi `9/9` cenários, `677` frames,
`1.354` samples de animação e `49` eventos de áudio.

Relatório: `fifa_pc/clean-room-integrated-coverage.tsv`, SHA-256
`DC6E37059884B9AC450D4FD73738D9D294EC8CE1A08C707CCD49AA61EC876A7B`.
Os nove PPMs authored `960x540` foram validados, com `341.782` pixels
visíveis no frame final de cada cenário.
Release `0/0`; self-test `65/65`; full-stack authored-only gerado/reexecutado
byte a byte igual, SHA-256
`BF2F85A42960ED5DCA0E1C84DB4481BAA66749263C31FDA6708C51224C67589A`.
Cada integration report também registra o SHA-256 do assembly Release atual:
`9CF41DD8555721A043BF58C6456755D2ABA3970EE417D73653EB0006775F37BB`.
Readiness `NOT_READY`, com `authored-integrated-coverage=verified-authored`,
SHA-256 `EDE51254C0D754EF46463183ADE17FCA6104B941E06032506BA96535494983AC`.
Essa evidência é authored do PC novo; `original_semantics=not-claimed`.

## Atualização verificada — manifesto de personagem authored — 2026-10-06

`CleanRoomCharacterAssetManifest` fornece
`clean-room-character-manifest-v1` para silhueta e paleta authored do runtime
managed. O manifesto não interpreta mesh, material, UV, skinning, rigging,
morph, animação ou comandos GPU da ISO.

Release `0/0`; self-test `62/62`; authored-only de 140 frames com checksum
`F26A04CC3CBEBDEA` e relatório SHA-256
`C0969EFA056C678494D70E97941A6EB6D1748E448F640FD2F84B4291F1900F33`.
Full-stack candidato: checksum `088EC5D10096A738`, replays de
match/input/animação/áudio `matched=True`, relatórios byte a byte iguais,
SHA-256 `BF9FA14EB4CC5EC1580535EF05D4F97AA0F256278FA9E7CE3311F71E5EE82DE8`.
Readiness `NOT_READY`, com `character-authored-assets=verified-authored`,
SHA-256 `6C86A5895D66F822D0E06A2A41C89F04C37001FA0B829DE132431BC08B507330`.
ISO sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização verificada — manifesto de animação authored — 2026-10-06

`CleanRoomAnimationAssets.cs` adiciona o manifesto
`clean-room-animation-manifest-v1`, e o animador managed o usa para resolver
perfis authored sem alegar equivalência com AXT, frames, timing, pose, rigging,
skinning ou morphs originais. O perfil `Unknown` permanece sem binding.

Release `0/0`; self-test `60/60`; headless de 80 frames com `6/6` cues authored;
full-stack de 140 frames, checksum `088EC5D10096A738`; replays de
match/input/animação/áudio `matched=True`. Relatórios de integração iguais,
SHA-256 `70DE5BAB17EDE85DDF847F8C1D8B5185E944AD4051AFFF534CE2AAE4B93AAA83`.
Os smokes opt-in de `JUGGLE`, `STEAL` e `GAMEBREAKER` passaram e cobriram os
perfis de ação adicionais.
Readiness `NOT_READY`, SHA-256
`CE868EA45F8CA772764D1BA8296A7BD7200EA2575150D572084DA0F887A8D153`.
ISO sem escrita, `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

O gate novo `animation-authored-assets` é somente `verified-authored`; o gate
original `animation-deformation` permanece `structural-only`. Continuar pela
próxima evidência independente, sem inventar bindings.

## Atualização verificada — manifesto de áudio authored — 2026-10-06

O novo `CleanRoomAudioAssetManifest` (`clean-room-audio-manifest-v1`) cobre os
oito candidatos de cue usados pela política authored. O mixer managed valida o
manifesto antes de preparar o candidato; isso não atribui a seleção aos
eventos EVT ou bancos ABKC originais.

Full-stack: `8/8` cues preparadas/tocadas, `0` sem rota, PCM de `1.180.164`
bytes com SHA-256
`2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`.
Release `0/0`; self-test `61/61`; quatro replays `matched=True`; relatórios de
integração iguais, SHA-256
`4DD380D1896481F5EEE4CACF1E46E1D3E4B2BF5CF5B42847C6FF924174B2B0F0`.
Readiness `NOT_READY`, SHA-256
`F485A2845003F98867ECCF31496761971AE6C574B06CA9EDACEF19BDEE471327`.

`audio-authored-assets=verified-authored`; `audio` geral continua
`partially-integrated` e `direct_selection_binding=not-proven`.

## Atualização verificada — correlação estrutural de áudio e bateria final — 2026-10-06

A auditoria `audio-event-payload-wave-correlation` decodificou `17/17`
candidatos EVT e `15/16` bancos candidatos ABKC/AST dentro da política limitada;
houve `0` pares PCM idênticos. O resultado é evidência estrutural negativa,
não binding de evento, sample, gameplay ou semântica original. Relatório SHA-256:
`00251215E62B4437C5B0EE3917C12E8DF66C1E278F1982EAF3AE95232E0432BD`; resumo:
`C5AEC72F58CAC11EFD2942D6F598C3401A5433475E4A3C7EF728D7893E6448F4`.

Build Release: `0` avisos/erros; self-test: `59/59`; smokes de XPR, material,
influência raster e headless passaram. O full-stack de 140 frames teve replays
`matched=True` para match/input/animação/áudio e os cinco relatórios ficaram
byte a byte iguais. Readiness: `NOT_READY`, SHA-256
`4982801DDA56466467E7DC6197BDB22EF481EE41BFF5FE07F971BDB27B8B672D`.
ISO preservada, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.

## Atualização verificada — digest PCM do áudio authored — 2026-10-06

O mixer managed passou a registrar o PCM concatenado das cues authored,
separado do header WAV. O full-stack produziu `1.180.164` bytes com SHA-256
`2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`; o replay
produziu o mesmo digest. Os relatórios de integração são byte a byte iguais,
SHA-256 `706E895FE9BAFD6A34BF6FB5C74A4C93110F1B6ECE193C18E8C625CD74C57823`.
Isso comprova a entrega authored do PCM, não a seleção original EVT/ABKC.
Self-test: `59/59`; readiness: `NOT_READY`, SHA-256
`336D91B1DB56E0BDF32C96BC0F093E707DA63EA04C7D06302AF3EB1A4CDBDFE9`.

## Atualização verificada — manifesto de assets authored da cena — 2026-10-06

`CleanRoomSceneAssets.cs` agora fornece e o renderer consome o manifesto
`clean-room-court-v1|clean-room-goal-v1|clean-room-ball-v1` para quadra, gols e
bola authored. Foram verificados `140/140` frames; o checksum de renderização
permaneceu `088EC5D10096A738`. O par de relatórios de integração é byte a byte
igual, SHA-256 `CA03EC0029301E7336E0CA839DAE717A60260F2F130584E6C5051AA0AA8E72FD`.
Readiness: `NOT_READY`, SHA-256
`661F2013630F999E2A79E27682A64253E7DA671D946ADD183B8E0FA0E22976E1`.
Isso continua separado dos assets e bindings originais.

## Atualização verificada — formatos XPR0 — 2026-10-06

O decoder managed de `XPR0` cobre agora `R5G6B5`, `A8R8G8B8`, `X8R8G8B8`,
`DXT1`, `DXT3` e `DXT5` no nível base, com fixtures no self-test. Isso é
somente decodificação de payload; não cria binding original de material, UV,
estado GPU ou renderização. As duas texturas reais do catálogo continuam
`titleimage.xbx` (`128x128`, `DXT1`) e `saveimage.xbx` (`64x64`, `DXT1`), e os
dois previews foram reexecutados em Release sem escrita na ISO.

Build: `0` avisos/erros; self-test: `58/58`; readiness: `NOT_READY`. SHA-256
da ISO preservado:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização verificada — intervalos de âncoras de payload — 2026-10-06

`animation-bank-catalog-payload-anchor-audit` mediu os offsets das duas
strings técnicas `_e/_r` por linha no relatório de payload: `19/19` bancos,
`1584/1584` linhas completas, `3168` âncoras e ordem crescente em `19/19`
bancos. Os intervalos são apenas observação estrutural; não são boundaries de
records nem semântica de frame, pose, rig, timing, skinning ou animação.

Relatório SHA-256:
`B63D77DF1E99F08576829FB016A1DEE5CB4C71F97F92E75E442826A36E481FBD`; resumo:
`372E58900EF0F173652F923DD3F49BC777B76006437D53675D91C662626E0ECA`.
Readiness: `NOT_READY`, SHA-256
`C733311CF7340C1A8B5E0C8170B2534BFAB7F0CAA30426B810FEFDB3B85F3198`;
self-test `58/58`.

## Instrução obrigatória

Antes de alterar qualquer arquivo, leia completamente:

`D:\projeto zeta\CONTINUAR_FIFA_STREET_2_HANDOFF.md`

Este arquivo é um resumo operacional para iniciar outro chat; o handoff longo
acima continua sendo a fonte histórica detalhada. Confira o estado real dos
arquivos antes de confiar em qualquer resumo. Preserve alterações existentes e
não faça `git reset`, `checkout` destrutivo ou sobrescrita da ISO.

A ISO deve permanecer sem escrita:

`D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso`

Não alegar recompilação do XBE. Não inventar bindings para materiais, UVs,
skinning, rigging, animação, morphs ou comandos GPU. Sempre separar no código,
nos relatórios e na documentação:

- evidência comprovada por leitura, decodificação ou teste repetível;
- caminho candidato/opt-in, especialmente a cadeia same-trunk de materiais;
- semântica ainda não decodificada ou não comprovada.

## Atualização desta etapa — asset de personagem authored separado — 2026-10-06

`fifa_pc/Runtime/CleanRoomCharacterRenderer.cs` agora fornece um asset de
personagem procedural authored, escrito do zero em .NET, com silhueta e paleta
determinísticas. O caminho não atribui à ISO qualquer mesh, material, UV,
skinning, rigging, morph, animação ou comando GPU. Ele é usado somente quando
os dois slots de personagem candidato estão desligados.

O runtime authored-only verificou `140/140` frames desse asset, com checksum
`243EF331249601A5` e `122` checksums distintos. O relatório
`fifa_pc/gameplay-authored-character-integration.tsv` tem SHA-256
`78FEC9A56961C0EC35DC3024DB81157451B4FA4B62EC094C0AF61F26219CCA5E`.
No full-stack candidato os slots candidatos continuam independentes e
`authored_character_asset_frames=0`; o checksum candidato foi
`088EC5D10096A738`.

Build Release: `0` avisos/erros. Self-test: `58/58`. O readiness atual continua
`NOT_READY`, com SHA-256
`0C537BBEA1140423C73F39A4B4EEC32B8292D630065B3F7A5876767867B25A1A`.
Coverage authored: `8/8` cenários, `42` eventos, `14` ações, SHA-256
`E8655638CFE171E10A14DA5B7C7CBFC6C5C3FFED2B3D879BE10F42ADA96C996D`.
Os quatro replays full-stack continuaram `matched=True`; os relatórios de
integração são iguais, SHA-256
`AF8A2B97C05E438B267536A612D8ADE90CE21ED1C20F40A32D25FE28E92CBBFC`.
O hash da ISO continua
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

O auditor estrutural `animation-bank-catalog-payload-name-audit` confirmou
`19/19` bancos e `1584/1584` nomes de catálogo presentes no `.data` dos pares
`.ord/.orl` como strings técnicas `_e`/`_r`, com `0` ausências. O relatório
detalhado tem SHA-256
`3D2CD9FBA876636011CFED8876204C1BC6F84A23334F8A06D42CA276671F4900` e o
resumo `FAE1691C64C4A99B968F7DFC8F349368892D924F10C44CBF5315178BEC3A673D`.
Isso é somente identidade estrutural catálogo↔payload; não atribui frame, pose,
rig, timing, skinning ou decoder. O readiness atualizado tem SHA-256
`0C537BBEA1140423C73F39A4B4EEC32B8292D630065B3F7A5876767867B25A1A`.

## Atualização desta etapa — gates authored separados — 2026-10-06

O readiness agora separa a integração verificável do runtime PC dos gates que
exigem semântica original. A execução atual marca
`assets-candidate-runtime=verified-candidate` (`24/24`, `0` rejeições),
`rendering-authored-runtime=verified-authored` (`140` frames, `69` checksums
distintos), `gameplay-authored-runtime=verified-authored` (`8` eventos),
`gameplay-authored-coverage=verified-authored` (`8/8` cenários, `42` eventos,
`14` ações),
`audio-authored-runtime=verified-authored` (`8/8` preparados/tocados, `0` sem
rota) e `animation-authored-runtime=verified-authored` (`280` amostras).

Isso não promove nenhum binding original. Os gates `assets`, `rendering`,
`audio` e `animation-deformation` continuam respectivamente
`candidate-integrated`, `not-proven`, `partially-integrated` e
`structural-only`, todos falsos; o readiness geral continua `NOT_READY`.
O auditor `gameplay-coverage-audit` é somente de trace/replay authored; não
comprova regras originais. Build Release passou sem avisos/erros e self-test
passou `55/55`.
Artefatos: `fifa_pc/Runtime/GameplayCoverageAudit.cs` e
`fifa_pc/gameplay-coverage.tsv`.
SHA-256 do coverage: `E8655638CFE171E10A14DA5B7C7CBFC6C5C3FFED2B3D879BE10F42ADA96C996D`;
SHA-256 do readiness final: `75A8E6DC7B88729B92FBA635D26819738EF651F69F0964BF92E5696D85C9F6EC`.
O relatório `fifa_pc/project-readiness.tsv` tem SHA-256
`9822B80B29821C0551DAA1FB9B493E54223DAF077BABC2DEE06262D4A95C0D2B`.

## Avanço desta etapa — input no full-stack e verificação final

O relatório cross-system agora registra também `input_samples` e
`input_trace_written`. A execução Release com ambos os slots candidatos,
gameplay authored, oponente e áudio produziu 140 frames, 140 samples de input,
8 eventos de partida e 8 eventos de áudio; 8/8 foram preparados/tocados e 0
ficaram sem rota. Os replays de match, input e áudio retornaram `matched=True`.
Os SHA-256 dos pares iguais são:

- match: `B76AA3B7E562564387568CD6872E1A58397130BEFBAD9B68803297EDA068AF3C`;
- input: `E1E2AA0616CDB08681150E956954309851D80999D8CCD47A546033B19A713AD7`;
- áudio: `FEBA7B454A0A82C33A3E2128FBC622A3C6AF957D1FB83CB6AB7DF156EBB6D07C`.

Os relatórios `gameplay-full-stack-integration.tsv` e
`gameplay-full-stack-replayed-integration.tsv` são byte a byte iguais, com
SHA-256 `FD228937D93B4305DC68FADA8A272EBB137B3B520F53AA7FAFF690736AAA7408`.
O readiness registra `full-stack-integration=verified-authored`, mas mantém
assets/renderização/áudio geral independentes e abertos; `original_semantics`
continua `not-claimed`.

Fechamento verificável da etapa: build Release `0/0`, self-test `53/53`,
headless padrão 80 frames `5405DE72BE1B4FC1`, cobertura authored `8/8`
cenários e `42/42` eventos, smokes de material/runtime e geometria `24/24`,
influência raster candidata `20`, e controle estado→geometria com 0 pares
exatos/0 relocações compartilhadas. ISO SHA-256 preservado:
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Readiness continua `NOT_READY` (SHA-256
`1B9B377DE8BC55D68CC6F8F8AA07518FF7CE8800460F7BB58D32BCA6DA9E68C2`).

## Avanço desta etapa — trace de animação authored

O renderer agora exporta o trace da pose procedural authored dos dois
jogadores. O full-stack de 140 frames gerou 280 amostras; o replay com
`--animation-trace-expected` retornou `matched=True`. O SHA-256 igual do par é
`57C124E9CDD044CD8DE0C2319C9AD11FC35930E0589C199B87C4DB0007342D09`.
O relatório de integração registra `animation_samples=280` e
`animation_trace_written=True`.

Esse avanço verifica apenas o caminho procedural novo do PC. Não interpreta
AXT, morph, rig, skinning ou timing da ISO e não altera o gate
`animation-deformation=structural-only`. A bateria da etapa passou Release
`0/0`, self-test `54/54`, headless padrão `5405DE72BE1B4FC1`, coverage `8/8`
e `42/42`, material/runtime `24/24`, geometria `24/24` e influência raster
candidata `20/24`. A ISO permanece com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`; o
readiness segue `NOT_READY`.

Auditoria bounded de áudio repetida: EVT nomeado `17/17` validado, SCCl
`144/144` layouts mono válidos, ABKC `15/16` bancos decodificados e `0`
ocorrências alinhadas nos candidatos `pcpahdr.big`, `pcpadat.big` e bancos.
Isso não prova seleção original evento→sample; o gate geral de áudio continua
parcial e a política authored permanece separada.

Artefatos novos: `fifa_pc/Runtime/CleanRoomAuthoredAnimationTraceSerializer.cs`,
`fifa_pc/gameplay-full-stack-animation-trace.tsv` e
`fifa_pc/gameplay-full-stack-animation-replayed.tsv`.

## Avanço desta etapa — trace authored de mira

O cenário `--aimed-demo` foi reexecutado no runtime Release e produziu
`fifa_pc/gameplay-clean-room-aimed-trace.tsv` com 6 eventos, incluindo
`SHOT` no frame 67 e `GOAL_HOME` no frame 69. O replay com
`--match-trace-expected` retornou `matched=True` e escreveu
`fifa_pc/gameplay-clean-room-aimed-replay.tsv`; os dois arquivos têm o mesmo
conteúdo. O checksum de renderização da execução foi
`801159854A2BC6F8`.
O SHA-256 igual dos dois traces é
`2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`.

O `project-readiness-audit` agora inclui a linha
`gameplay-aimed-trace=verified-authored`, `passed=True`, `observed=6` e
`expected=6`. O gate geral de gameplay permanece falso e o readiness continua
`NOT_READY`: o trace documenta apenas comportamento authored do PC e não
promove semântica de mira, trajetória ou regra original.

O self-test foi atualizado para cobrir essa linha. Resultado da etapa:
Release sem avisos/erros e self-test `53/53`.
O mesmo aimed-demo foi repetido com `--play-audio`: `6/6` cues preparados e
tocados, `0` sem rota, replay de áudio `matched=True` e checksum de render
`801159854A2BC6F8`. O agregador `gameplay-audio-coverage-audit` agora inclui
esse cenário: `8/8` cenários, `42/42` eventos, `0` sem rota, `14/14` ações e
`8` cues únicos. O SHA-256 igual dos traces de áudio aimed é
`7FA076ADE12B81EC41213F4C34113D35F6F0D1A8FCC14F540C66776BB0CE8F32`; o
coverage tem SHA-256
`3C0DBC250C19F9259CC37EABF3C7A49A56F8C1CAD6072BDA729C38AF93C369D9`.
O readiness também registra `audio-aimed-trace=verified-authored` com 6
eventos (`SHOT->attackgoal`, `GOAL_HOME->goal`), todos tocados e replay de
áudio determinístico. Isso não altera o gate geral de áudio, que permanece
parcial e sem seleção original comprovada.
O aimed-demo também exporta/reproduz 80 amostras de input; o frame 67 contém
`MoveY=0.75` e `shoot=True`, e o replay do input trace retornou `matched=True`.
O SHA-256 igual do par é
`A1BCF11B578F5BE9A8DF767E162D7314A6E3398A688D7F2FCA438F536002E44E`.

## Estado comprovado na última etapa

- Build Release: passou com 0 avisos e 0 erros.
- Self-test: `53/53`.
- O controle estrutural `animation-morph-relocation-profile-audit` cruzou
  somente os conjuntos de gaps de relocação dos relatórios morph e dos bancos
  de animação: 594 linhas em 393 layouts, 393 parseáveis, 19 bancos válidos,
  53 layouts com contenção, 0 igualdades exatas e 159 pares de contenção.
  Isso é recorrência sintática entre relatórios separados, não vínculo
  morph→animação nem decoder de frame, pose, rig, timing, skinning, material,
  UV, GPU ou comando.
- Gameplay authored: posse, movimento, passe, trick, panna, chute, gol,
  reinício, regras de partida, oponente, mira, gamebreaker e juggle.
- O novo recurso da última etapa é `--juggle-demo`: quatro eventos `JUGGLE`,
  cadeia authored, elevação visual da bola e limpeza ao chutar. Isso é
  comportamento novo do runtime, não uma decodificação da jogabilidade original.
- Traço/replay de juggle: 7 eventos, `matched=True`.
- Demo juggle de 80 frames: checksum de render
  `7F1105CA57B913EE`; histograma `GOAL_HOME=1,JUGGLE=4,KICKOFF=1,SHOT=1`.
- Traço juggle de 56 frames: checksum `9F0F1D791CE6070F`.
- SHA-256 dos dois arquivos de traço: 
  `04920791F3B97933F8BB4C912D2DBB283878A66BC75A9C00092CB3F84400B3A1`.
- Quadro visual de juggle, frame 30: `4FAA97BD30D2D7CD`; o PNG de inspeção é
  `D:\projeto zeta\fifa_pc\juggle-preview-frame.png` e o PPM original é
  `D:\projeto zeta\fifa_pc\juggle-preview-frame.ppm`.
- A execução visual confirmou bola elevada e sombra authored; não é evidência
  de pose, rig ou animação do jogo original.
- Materiais candidatos: `tops0030.big` + `tops0030.ord` + `tops0030.xsh` +
  entrada SHPX `tops`; 24/24 vínculos same-trunk passaram, PAL8 24/24,
  24 checksums distintos. O lote continua `candidate-integrated`, não provado.
- Animação/deformação: 19 bancos e 594 evidências de morph; não há binding
  comprovado de pose, rigging, skinning ou animação.
- Áudio: 17 eventos nomeados decodificados; catálogo ABK com 16 bancos,
  15 decodificados e 1 rejeitado. A política authored agora cobre as ações
  conhecidas do runtime com cues nomeados (`intro`, `attackgen`, `panna`,
  `attackgoal`, `goal`, `gb_goal`, `tackle` e `outro`); isso é seleção authored
  clean-room, não binding de evento original.
- O `project-readiness-audit` agora registra também o resumo estrutural
  morph/animação: 393 layouts parseáveis, 159 pares de contenção e 0
  igualdades exatas. Isso é rastreabilidade de relatórios, não binding de
  morph, pose, rig, timing ou skinning.

Fechamento verificável mais recente: Release sem avisos/erros, self-test
`53/53`, animação `19/19`, morph `391/391`, material candidate `24/24`,
geometria `24/24` sem rejeições e eventos nomeados `17/17`. O preview candidate
authored produziu `F2DD798FDD206BD9`; headless candidate e oponente/áudio
produziram `29EACF0AE7B12B58` e `651E41284E9242BC`. Os replays juggle/rodada
retornaram `matched=True`. A ISO mantém SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

O flag opt-in `--candidate-home-character` também está integrado ao preview,
headless e host Win32. Ele coloca rigidamente o candidato same-trunk no slot
home e deixa o away no caminho authored; não é binding de rig, skinning, morph,
animação, UV ou GPU. Checksums: preview `AB8D372F87FAA6B5` e headless
`DB0F059C0AC82133`.
O cenário combinado com oponente authored, `--play-audio`, material candidate
e candidate-home também passou em 140 frames: checksum
`2641FCCE8245A44B`, `3` cues preparados/tocados e `4` eventos sem rota.

O modo authored foi ampliado com `--candidate-away-character`, disponível em
preview, headless e host Win32. O preview away produziu
`59D25C3C88B9CF69`, ambos os slots `0F83CEFDD78C58BA` e headless away de 80
frames `72CEA666935DBFCB`. É uma colocação rígida do candidato, sem binding
de rig, skinning, morph, animação, UV ou GPU original.

Após a ampliação da política authored de áudio, o mesmo cenário foi repetido
com `8` cues preparados/tocados e `0` eventos sem rota, mantendo o checksum
`2641FCCE8245A44B`. As rotas são explícitas no código como política
clean-room; não são seleção ou binding recuperado da ISO.
O trace adicional `gameplay-audio-combined-trace.tsv` contém 8 rotas, incluindo
o `KICKOFF` de sequência 0; o replay por `--audio-trace-expected` retornou
`matched=True`, com `8/8` preparados/tocados e `0` sem rota. A correção foi
iniciar o cursor de áudio em `-1`; isso valida a entrega authored e não cria
binding original EVT/ABKC.
O SHA-256 igual do trace gerado e do replay combinado é
`FEBA7B454A0A82C33A3E2128FBC622A3C6AF957D1FB83CB6AB7DF156EBB6D07C`; o par
de trace de áudio do cenário `STEAL` também coincide em
`B6BEDBFDF812288F820AB359EE98005A9512526B3288540AF84F385E76D6D0D7`.
O novo smoke cross-system usou material candidato nos dois slots, gameplay
authored com oponente e áudio WinMM: 140 frames, checksum
`088EC5D10096A738`, 8 eventos, 8/8 preparados/tocados e 0 sem rota. O
`gameplay-full-stack-integration.tsv` e seu replay foram iguais e registram
`original_semantics=not-claimed`. O relatório também registra
`distinct_render_checksums=69` em 140 frames; isso é somente variação da
apresentação authored do runtime, não evidência de pose, rigging, skinning ou
animação original.
O SHA-256 igual dos dois relatórios de integração é
`F0329A2C5189CC326D9099783FC771C16BD8227B48B71DB989D27E9DB3E2DE8F`.
O cenário authored curto de `0,25` segundo foi repetido com candidate-home e
`--play-audio`: `FULL_TIME` ocorreu uma vez, `outro` foi preparado/tocado,
zero eventos ficaram sem rota e o checksum foi `F39849417B16C774`.

As 14 rotas authored agora têm uma fonte única em
`fifa_pc/Runtime/CleanRoomAudioCuePolicy.cs`, usada pelo mixer, self-test e
readiness; `UNKNOWN` continua explicitamente sem rota.

O readiness real confirmou `14/14` rotas com candidato nomeado utilizável e
`8` cues únicos; também registrou `authored_audio_trace_replay=verified`
(`8` eventos, `KICKOFF -> intro`, todos tocados) e
`authored_steal_audio_trace_replay=verified` (`2` eventos). Isso é cobertura
do runtime authored, não seleção original.

A cobertura de gameplay authored foi ampliada no readiness: a fixture de panna
tem 3 eventos (`KICKOFF`, `PANNA`, `FULL_TIME`) e replay verificado; a fixture
de relógio curto tem 2 eventos (`KICKOFF`, `FULL_TIME`) e replay verificado.
Os dois traces são comportamento novo do runtime e não equivalência com regras
ou eventos do jogo original.

Checksums: panna `CFCF8FC4F0CB4380` e `FULL_TIME` `509B0C35DD390FFF`.
Os hashes dos pares gerado/replay foram, respectivamente,
`C14B39694F929825A1707687AC2BE9FFBC7F2DE12045C028B867612B2B86F8BF` e
`C025B66A918312E27AE23CDDF9777951F95F0C14F2DF07CB491FF8F062C72801`.

A rota authored `STEAL->tackle` também foi exercitada por
`--steal-demo --opponent-ai`: 2 eventos, `matched=True`, checksum
`BFA4E813A3D0EA19` com candidate-home/material e áudio `1/1`, sem eventos
sem rota. O SHA-256 igual dos dois traces é
`46D917414660878DDA0B319A72018473D4BD44858E6A7116730931C11F3DECA2`.

Foi criado `gameplay-audio-coverage.tsv` para agregar os traces/replays de
full-stack, round, gamebreaker, juggle, panna, FULL_TIME e STEAL. A execução
verificou `7/7` cenários, `36/36` eventos preparados e tocados, `0` eventos
sem rota, cobertura das `14/14` ações da política e `8` cues únicos. O status
é `verified-authored`; `original_semantics=not-claimed` permanece explícito.
O SHA-256 do relatório é
`1FF0AF219F61FBBFB0892C58EA3731FB8AC58E2D6EC06CCCE7DABD356FFCFE7A`.

Nesta continuação, `eagl-material-runtime-batch-smoke` passou os 24 vínculos
pelos três slots authored. Foram confirmados `24/24` frames home, `24/24` away
e `24/24` ambos, com `0` rejeições e `24` checksums distintos em cada slot;
isso permanece cobertura de integração candidata, não binding global de UV,
material, skinning, rigging ou GPU. O headless com ambos os slots produziu
`641ED53BD0DAE660`. O cenário STEAL com slot away, oponente, material e áudio
produziu `301745CFDC42BE25`, `1/1` cue `tackle` tocada e replay
`matched=True`.

O renderer clean-room agora possui um perfil visual authored de tackle para a
ação `STEAL`, implementado em `CleanRoomPlayerAnimator`. O perfil é puramente
procedural e não é decoder de AXT/morph nem binding de rig, skinning ou
animação original. A execução away/material/áudio passou a produzir checksum
`30D789D3BCBB915A`; o trace de 2 eventos continuou com replay `matched=True`.
Com o slot home candidato e o jogador away procedural, a variante simétrica
repetiu o trace, tocou `tackle` `1/1` e produziu `BA2CD81182DF970E`.

Bateria final desta etapa, executada após o build Release: build sem avisos ou
erros; self-test `53/53`; smoke material/runtime `24/24` matches, `24/24`
frames e `24/24` estados de objeto, sem rejeições; auditoria de relocação com
`594` morph rows, `393` layouts parseáveis, `19` bancos, `159` pares de
contenção e `0` pares exatos; EVT nomeado `17/17`; ABK `15/16` bancos
decodificados, com 1 rejeição preservada. O preview candidate-home repetiu
`AB8D372F87FAA6B5`; a execução combinada de 140 frames repetiu
`2641FCCE8245A44B`, com `8/8` cues de gameplay preparados/tocados e `0`
eventos sem rota; o caso curto verificou `FULL_TIME -> outro` com `1/1` cue.
O replay authored de juggle retornou `matched=True`; os traces gerado e
reproduzido têm o mesmo SHA-256
`04920791F3B97933F8BB4C912D2DBB283878A66BC75A9C00092CB3F84400B3A1`.
O readiness final continua `NOT_READY`. A ISO permaneceu com tamanho
`2013003776`, timestamp `2026-01-22T07:03:26.0859951-03:00`, atributo
`Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Estado de prontidão

`project-readiness-audit` continua retornando `NOT_READY`.

| Área | Estado | Limite atual |
|---|---|---|
| ISO | `verified` | Integridade comprovada, não significa que o jogo foi reimplementado. |
| Assets | `candidate-integrated` | 24 materiais candidatos; UV/material global não comprovado. |
| Rendering | `not-proven` | Há rasterização software clean-room e caminhos candidatos, mas não renderização final comprovada. |
| Gameplay | `clean-room-integrated` | Mecânicas authored e traces determinísticos; sem equivalência original alegada. |
| Full-stack integration | `verified-authored` | Um smoke cross-system verificou os dois slots candidatos, gameplay e áudio; não prova semântica original. |
| Audio | `partially-integrated` | Catálogo e rotas parciais; associação completa evento→som não comprovada. |
| Animation/deformation | `structural-only` | Estrutura e morph evidence, sem rig/pose/skinning binding. |

ISO verificada na última bateria:

- tamanho: `2013003776` bytes;
- timestamp: `2026-01-22T07:03:26.0859951-03:00`;
- atributo observado: `Archive` (`IsReadOnly=False` é atributo pré-existente;
  o acesso da aplicação deve continuar sendo por stream somente leitura);
- SHA-256: `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Nesta etapa foi formalizada a busca negativa do índice de eventos de áudio. O
comando `audio-event-index-search <iso> --data --report <saida.tsv>` percorreu
os 1.061 pares candidatos da string table em `0x2090`, os 16 `.abk` e os
companions `pcpahdr.big`/`pcpadat.big`: foram observados 17 offsets/words
distintos e zero ocorrências alinhadas em cada fonte pesquisada. O relatório
`fifa_pc/audio-event-index-evidence.tsv` tem SHA-256
`D5587B4739FC1C150169D456226F43F8907E595996DD89D51B5C3101A777503F` e
preserva `status=no-direct-selection-binding`,
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.
Isso é evidência estrutural negativa limitada à busca alinhada; não prova
ausência de rotas internas e não cria binding de evento, ABKC ou playback.
Na bateria final, o headless padrão de 80 frames passou com checksum
`5405DE72BE1B4FC1`; o smoke `STEAL` de 80 frames com material candidato,
oponente e áudio passou com checksum `BA2CD81182DF970E`, `2/2` cues
preparadas/tocadas (`KICKOFF->intro`, `STEAL->tackle`) e zero eventos sem rota.

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

0.29. O CFG bounded dos candidatos foi concluído: `81/81` CFGs e `81/81`
     `ret` alcançáveis, sem declarar limites de função ou semântica.
0.30. A distinção de chamadas diretas foi preservada: `24` ocorrências nos
     slices, mas `1` endereço distinto global (`0x0016BF90`). O join desse
     conjunto com `87` alvos diretos, `64` alvos de janela e `235` nós do grafo
     produziu interseções `0`/`0`/`0`; isso é apenas controle negativo
     estrutural e não recupera função ou semântica.
0.31. O único destino direto global foi mapeado em `.text` e recebeu um slice
     bounded de `256` bytes (`76` instruções, `ret`, sem prólogo reconhecido).
     Isso continua sendo caminho candidato, não uma função recuperada; não
     seguir chamadas nem promover semântica.
0.32. Os `24` call-sites foram registrados com contexto local: `24/24` têm
     `lea [ecx+deslocamento]`, `push edx` antes da preparação e `ret 4` depois;
     os `24` deslocamentos são distintos. Isso permanece regularidade
     estrutural, não função, wrapper ou binding.
0.33. O join entre a tabela e esses call-sites foi concluído: `81` linhas de
     alvo, `1` base, `24/24` call-sites joined, `27` índices distintos, `27`
     ocorrências e `3` duplicatas. Preservar as duplicatas é importante, mas
     isso ainda não fornece limites de função, layout de objeto ou semântica;
     o próximo avanço deve procurar evidência independente, não promover
     esses índices a bindings.
0.34. O único destino direto dentro do slice de `0x0016BF90` foi auditado em
     uma segunda janela bounded: `0x0026E278`, `89` instruções lineares, `37`
     alcançáveis, `7` blocos, `1` retorno e `0` chamadas adicionais. Isso
     encerra somente esse recorte local; não autoriza seguir chamadas,
     recuperar função ou atribuir semântica.
0.35. Os dois call-sites internos de `0x0016BF90` para `0x0026E278` foram
     registrados: `2` ocorrências, `1` alvo distinto e `0/2` com a forma
     externa `push edx`/`lea [ecx+...]`/`ret 4`; ambos têm `ret 8`. Isso mantém
     a distinção entre contexto local observado e semântica não decodificada.
0.36. A integração candidate-only de renderização foi promovida a um gate
     separado (`rendering-candidate-runtime`): `140/140` frames, `24` assets,
     `280` samples authored, `8/8` eventos de áudio e replay/framebuffer
     válidos. Não alterar o gate original enquanto não houver associação
     independente de estado de renderização a geometria/UV/material.
0.37. A integração candidate-only de áudio também foi registrada em gate
     separado (`audio-candidate-runtime`): `8/8` eventos preparados e
     reproduzidos, `0` não mapeados e replay coincidente. Manter o gate original
     parcial até existir evidência independente de seleção EVT/ABKC/amostra.
0.28. O controle negativo de interseção foi concluído: `81` alvos candidatos,
`87` alvos de chamadas diretas e `64` alvos da janela de render, com
interseções `0`/`0`. Isso permanece evidência estrutural negativa; não prova
independência de comportamento nem recupera funções ou semântica.
1. A varredura limitada da tabela de transferência indireta indexada foi
     concluída: `1` base, `89` words legíveis, `87` executáveis e prefixo
     contíguo máximo de `87`. Isso é somente um candidato estrutural; não
     estabelece comprimento, ponteiros, funções, chamadas ou semântica.
2. Executar novamente `project-readiness-audit` depois de qualquer mudança e
   preservar `NOT_READY` enquanto os gates originais permanecerem abertos.
3. Continuar renderização/indireções, morph/rig/animação e áudio somente com
   evidência independente, repetível e localizada; o próximo avanço útil deve
   procurar recuperação de tabela/função ou evidência externa, não inferir
   bindings a partir deste prefixo.
4. Manter os materiais candidatos explicitamente opt-in, os personagens e
   gameplay authored separados dos dados originais e os checksums do modo
   padrão estáveis.
5. Registrar cada avanço nos MDs existentes com comandos, resultados e hashes;
   não marcar o projeto como concluído nem iniciar code review.

Não marcar o projeto como concluído e não iniciar code review. O objetivo ainda
não foi atingido: renderização final, gameplay, assets e áudio ainda não estão
integrados e verificados como um todo.

## Registro da etapa atual — auditoria SHPX de entradas de imagem

Foi adicionada a auditoria `shpx-image-entry-audit` em
`fifa_pc/Runtime/ShpxImageEntryAudit.cs`, com comando CLI, self-test e
documentação. Ela relê `data/aptanims/player1.xsh` da ISO somente para leitura
e preserva por entrada o índice, nome, offset, tamanho, formato, dimensões,
hash do RGBA decodificado e os hashes agregados da tabela/ordem. O resultado
real é `46/46` entradas decodificadas, `PAL8`, `128x128`, status
`verified-image-entries`; o SHA-256 da origem reportado foi
`4ADAFBC2B34EA033F0E51A883F89F151B634EAF3D7A52B279E8DC12939B5C3C7`.

Esta auditoria é deliberadamente limitada a evidência de imagem e ordem
estrutural da tabela. `timing_evidence=not-proven` e
`original_semantics=not-claimed`; não há binding novo para frames temporizados,
pose, rigging, skinning, animação, material, UV, morph ou GPU. O projeto não
está concluído.

## Comandos mínimos de retomada

Executar no PowerShell, a partir de `D:\projeto zeta`:

```powershell
dotnet build fifa_pc/FifaStreetPc.csproj -c Release --nologo
dotnet run -c Release --no-build --project fifa_pc -- self-test
dotnet run -c Release --no-build --project fifa_pc -- shpx-image-entry-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/player1.xsh --report "D:\projeto zeta\fifa_pc\shpx-image-entry-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- project-readiness-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\project-readiness.tsv"
dotnet run -c Release --no-build --project fifa_pc -- clean-room-gameplay-invariant-audit "D:\projeto zeta" --report "D:\projeto zeta\fifa_pc\clean-room-gameplay-invariants.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-catalog-timestamp-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-catalog-timestamp-audit.tsv" --summary "D:\projeto zeta\fifa_pc\animation-bank-catalog-timestamp-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-material-runtime-batch-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-material-runtime-batch.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-render-influence-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\candidate-render-influence-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-morph-relocation-profile-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\animation-bank-data-relocation-audit.tsv" "D:\projeto zeta\fifa_pc\animation-morph-relocation-profile-audit.tsv" --summary "D:\projeto zeta\fifa_pc\animation-morph-relocation-profile-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --juggle-demo --clean-room-characters
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 56 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-input.tsv" --clean-room-characters --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-trace.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 56 --input-script "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-input.tsv" --clean-room-characters --match-trace-expected "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-trace.tsv" --match-trace-output "D:\projeto zeta\fifa_pc\gameplay-clean-room-juggle-replayed.tsv"
Get-FileHash -Algorithm SHA256 "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Também repetir os smokes relevantes de animação, SHPX/geometria, morph e
áudio descritos no handoff longo. Ao documentar resultados, não substituir
evidência ausente por interpretação plausível.

Comando adicional desta etapa:

```text
dotnet run -c Release --no-build --project fifa_pc -- audio-event-index-search "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --data --report "D:\projeto zeta\fifa_pc\audio-event-index-evidence.tsv"
dotnet run -c Release --no-build --project fifa_pc -- gameplay-audio-coverage-audit "D:\projeto zeta" --report "D:\projeto zeta\fifa_pc\gameplay-audio-coverage.tsv"
```

## Arquivos recentes importantes

- `fifa_pc/Runtime/StreetMatch.cs`
- `fifa_pc/Runtime/MatchInputScript.cs`
- `fifa_pc/Runtime/HeadlessBackends.cs`
- `fifa_pc/Runtime/GameRuntime.cs`
- `fifa_pc/Runtime/PreviewRenderer.cs`
- `fifa_pc/Runtime/CleanRoomPlayerAnimator.cs`
- `fifa_pc/Runtime/Win32GameHost.cs`
- `fifa_pc/Runtime/GameplayAudioTraceSerializer.cs`
- `fifa_pc/Runtime/AudioEventIndexEvidenceReport.cs`
- `fifa_pc/Runtime/GameplayAudioCoverageAudit.cs`
- `fifa_pc/Runtime/CandidateRenderInfluenceAudit.cs`
- `fifa_pc/Runtime/CandidateIntegratedRuntimeAudit.cs`
- `fifa_pc/Runtime/CleanRoomIntegrationReport.cs`
- `fifa_pc/Runtime/ProjectReadinessAudit.cs`
- `fifa_pc/Runtime/ShpxExportCrosscheck.cs`
- `fifa_pc/Runtime/EaglCandidateAssetProvenanceAudit.cs`
- `fifa_pc/Runtime/SelfTestRunner.cs`
- `fifa_pc/shpx-export-crosscheck.tsv`
- `fifa_pc/shpx-export-crosscheck-summary.tsv`
- `fifa_pc/eagl-candidate-asset-provenance-audit.tsv`
- `fifa_pc/eagl-candidate-asset-provenance-audit-summary.tsv`
- `fifa_pc/xbe-string-xref-call-clusters.tsv`
- `fifa_pc/gameplay-clean-room-juggle-input.tsv`
- `fifa_pc/gameplay-clean-room-juggle-trace.tsv`
- `fifa_pc/gameplay-clean-room-juggle-replayed.tsv`
- `fifa_pc/gameplay-clean-room-panna-trace.tsv`
- `fifa_pc/gameplay-clean-room-panna-replayed.tsv`
- `fifa_pc/gameplay-clean-room-full-time-trace.tsv`
- `fifa_pc/gameplay-clean-room-full-time-replayed.tsv`
- `fifa_pc/gameplay-clean-room-steal-trace.tsv`
- `fifa_pc/gameplay-clean-room-steal-replayed.tsv`
- `fifa_pc/gameplay-clean-room-steal-tackle-trace.tsv`
- `fifa_pc/gameplay-clean-room-steal-tackle-replayed.tsv`
- `fifa_pc/gameplay-audio-combined-trace.tsv`
- `fifa_pc/gameplay-audio-combined-replayed.tsv`
- `fifa_pc/gameplay-audio-steal-trace.tsv`
- `fifa_pc/gameplay-audio-steal-replayed.tsv`
- `fifa_pc/gameplay-full-stack-integration.tsv`
- `fifa_pc/gameplay-full-stack-replayed-integration.tsv`
- `fifa_pc/gameplay-full-stack-match-trace.tsv`
- `fifa_pc/gameplay-full-stack-match-replayed.tsv`
- `fifa_pc/gameplay-full-stack-audio-trace.tsv`
- `fifa_pc/gameplay-full-stack-audio-replayed.tsv`
- `fifa_pc/audio-event-index-evidence.tsv`
- `fifa_pc/gameplay-audio-coverage.tsv`
- `fifa_pc/candidate-render-influence-audit.tsv`
- `fifa_pc/project-readiness.tsv`
- `fifa_pc/README.md`
- `docs/FIFA_RECOMPILATION_STATUS.md`
- `CONTINUAR_FIFA_STREET_2.md`
- `CONTINUAR_FIFA_STREET_2_HANDOFF.md`

## Avanço mais recente

A auditoria somente leitura `tools/analysis/xbe_string_xref_audit.py` foi
executada sobre `default.xbe` com Capstone 5.0.9. Resultado: 1.929 strings de
interesse e 944 referências x86 candidatas em 944 instruções (939 imediatas,
5 de memória absoluta), incluindo literais de áudio, `.xsh`, `.big`,
`D3DTexture` e `EAGLAnimationBuffer`.

Relatórios: `fifa_pc/xbe-string-xref-audit.tsv` e
`fifa_pc/xbe-string-xref-summary.tsv`. SHA-256 do XBE:
`5fabb5b763be0cc8823e814ee343ac5be6950305330f46c5170e08eaadea910b`.
É evidência `candidate-x86-reference-only`, não um binding; não reivindica
semântica original e não altera `NOT_READY`.

O refinamento `--call-context` também foi executado: `fifa_pc/xbe-string-xref-call-context.tsv`
tem 6.251 linhas, 790/944 referências com chamada direta próxima e 1.141
destinos executáveis distintos. É apenas contexto local candidato e não define
funções ou bindings.

O refinamento `--call-clusters` gerou `fifa_pc/xbe-string-xref-call-clusters.tsv`
com 1.141 agrupamentos por destino executável observado, 306 com pelo menos
duas strings em coocorrência local. SHA-256 do relatório:
`9A5F416ED66485B2D4E0D2087E3E400711627AA0352CCAB9E6194EC818AF4CF3`; o
resumo atualizado tem SHA-256
`19A633D47CC2FA5CE30D800AE6C52ABD63CDF785546F31DC190A892E72A862BD`.
O teste Python correspondente passou `6/6`. Esse recorte é coocorrência
candidate-only, sem limites de função, subsistema ou binding; `NOT_READY`
permanece.
## Avanço atual — janelas estáticas de renderização

O próximo passo foi concluído sem alterar a ISO: `tools/analysis/xbe_render_target_audit.py`
selecionou os 64 destinos mais fortes dos clusters estáticos de renderização
e produziu janelas x86 limitadas a 384 bytes. Foram disassemblados `64/64`
destinos; `63` chegaram a `ret` dentro da janela e `7` exibiram prólogo
semelhante a função. A saída registra chamadas e operandos apenas como
estrutura observada, com proveniência `same-ISO-retail-XBE+bounded-x86-disassembly-of-ranked-render-context;
no-execution; no-binding` e `original_semantics=not-claimed`.

Não interpretar essas janelas como funções recuperadas nem como bindings de
materiais, UVs, skinning, rigging, animação, morphs ou GPU. O novo relatório é
`fifa_pc/xbe-render-target-audit.tsv` (SHA-256
`3FF54B4F5D5AF71147CC89F5400E27F2C0DEA562A7FD6657313543EEF99A403A`) e o
resumo é `fifa_pc/xbe-render-target-summary.tsv` (SHA-256
`F2B41AC9545C50ADA19939C15E2E94496239A1FB9E4628EBA4DD7E062B89BB94`).

A prontidão registra `rendering-static-xbe-target-windows` como
`verified-structural`, mas o gate original de renderização continua
`not-proven`. Os demais limites continuam explícitos: assets são candidatos,
áudio é `partially-integrated`, animação/deformação é `structural-only` e
gameplay é authored clean-room; portanto o projeto segue em andamento.

Fechamento da etapa: Release sem avisos/erros, self-test `72/72`, testes
Python `13/13`, matriz `completed-authored-and-candidate-opt-in`, smokes,
runtime headless, replays determinísticos e captura windowed concluídos. A ISO
permanece inalterada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Avanço atual — CFG estático limitado dos alvos de renderização

O relatório `fifa_pc/xbe-render-cfg-audit.tsv` amplia as janelas anteriores
sem executar o XBE: cada um dos 64 alvos foi analisado numa janela máxima de
2048 bytes, seguindo apenas saltos diretos para instruções presentes na
janela. Foram obtidos `64/64` CFGs alcançáveis, `62` retornos alcançáveis,
`441` blocos básicos, `127` chamadas diretas contadas e `22` transferências
indiretas não resolvidas.

Chamadas não foram seguidas e nenhum limite de função foi afirmado. A saída é
`verified-static-render-cfg-slices`, com `no-function-boundary-claim`,
`no-binding` e `original_semantics=not-claimed`; não decodifica nem promove
materiais, UVs, skinning, rigging, animação, morphs ou comandos GPU.

SHA-256: relatório `5000D88598D42E2238279B47AA47364B830776ED1C2BC8DABE2B6874C2CEAECA`;
resumo `EC0FE95F012EE104C19065F41C806ABFD974DBD34E25876DA545C0CB99DABA5B`.
O novo subgate `rendering-static-xbe-cfg-slices` é estrutural, mas o gate
original `rendering` continua `not-proven`. Assets, áudio e
animação/deformação também mantêm seus estados candidato/parcial/estrutural.

A matriz completa passou com Release `0/0`, self-test `72/72`, testes Python
`14/14`, smokes, runtime headless e replays determinísticos. A ISO não foi
alterada: SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Avanço atual — correlação completa dos candidatos de áudio enumerados

O smoke da etapa executou a correlação EVT↔payload↔ABKC/AST com
`--max-sound-candidates 100000`. Como o catálogo contém apenas `368` índices
de som candidatos nos `16` bancos, todos foram percorridos: `239` tiveram
decodificação válida. Os `17` candidatos EVT foram decodificados e nenhum
apresentou hash PCM idêntico a um candidato de banco (`0` pares; `0` eventos
com match).

É evidência negativa de payload, não uma seleção original. Nenhum evento,
índice de sample, ABKC, rota de gameplay ou semântica foi inferido;
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.
O relatório tem SHA-256
`BDE7CF7E58E09E17D2BBB8932E413249D51264424E5A1C32E8B5E30D404E345E` e o
resumo `CC767B8DCE09E74CDB78FCA6898A9C4960DBC386DBC82B26F87A5C389DD084FD`.

O gate geral de áudio continua `partially-integrated`; somente a cobertura
authored clean-room está integrada e verificada. A etapa passou Release `0/0`,
self-test `72/72`, testes Python `14/14`, matriz completa e ISO inalterada
com SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — grafo estático limitado de chamadas do renderizador

`tools/analysis/xbe_render_call_graph_audit.py` segue chamadas diretas a
partir dos `87` alvos de renderização anteriores, com profundidade máxima `2`,
janela de `256` bytes e limite de `512` nós. Foram obtidos `235` nós
executáveis disassemblados, `326` arestas únicas, `200` nós com `ret` alcançável
e nenhum truncamento.

É evidência estrutural limitada, não recuperação completa de funções. O XBE
não é executado, chamadas indiretas permanecem sem resolução e nenhum binding
de geometria, material, UV, skinning, rigging, animação, morph ou GPU foi
criado. O subgate `rendering-static-xbe-call-graph` é
`verified-structural`; o gate integrado de renderização permanece
`not-proven`, com `original_semantics=not-claimed`.

Relatórios: `fifa_pc/xbe-render-call-graph-nodes.tsv`,
`fifa_pc/xbe-render-call-graph-edges.tsv` e
`fifa_pc/xbe-render-call-graph-summary.tsv`. SHA-256 dos três, na ordem:
`340DFC27050E6A9E29018A9C946C90895A8E98780442FF6BB722D9E08D3722B0`,
`F7B7291B616470BEB41A5491B4F798423775BE398FD70F796CE7E9B08CC8073C` e
`16671B4DF9A4EE10D9DBE1B7833372751ACA4D7B8916BBBDD172C6AAAF4AA4E8`.

Validação: Release `0/0`, self-test `72/72`, testes Python `16/16`, matriz
completa `completed-authored-and-candidate-opt-in`, smokes, runtime headless,
replays determinísticos e captura windowed. A ISO manteve
`2013003776` bytes, `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização desta etapa — formas estruturais dos acessores indexados — 2026-10-07

Foi adicionada `tools/analysis/xbe_render_indirect_indexed_accessor_audit.py`.
Ela relê a tabela indexada e observa no máximo `256` bytes de cada destino até
o primeiro `ret` observado. O resultado real foi `87` ocorrências, `81`
destinos distintos, `81/81` mapeados/disassemblados e `81/81` terminados por
`ret`. A classificação das janelas foi `1` fallback `pop/pop/ret`, `55`
`mov-memory/ret`, `24` `lea/push/call/ret` e `1` outra forma; houve `107`
deslocamentos distintos e `1` alvo direto distinto de chamada.

Isso permanece evidência estrutural bounded, sem execução do XBE, sem limite de
função recuperado e sem semântica atribuída. Não foram inventados bindings de
geometria, materiais, UVs, skinning, rigging, animação, morphs ou GPU;
`original_semantics=not-claimed`. O readiness registra o subgate
`rendering-static-xbe-indirect-indexed-accessor-shapes=verified-structural`,
mas o gate de renderização continua `not-proven`.

Relatórios: `fifa_pc/xbe-render-indirect-indexed-accessor-audit.tsv` e
`fifa_pc/xbe-render-indirect-indexed-accessor-summary.tsv`. A bateria final
passou Release `0/0`, self-test `72/72`, testes Python `32/32`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. A ISO continua somente leitura, com
`2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização desta etapa — associação estrutural de slots e registros `__VB` — 2026-10-07

Foi adicionado o comando `eagl-candidate-fetch-association-audit`. Ele verifica
somente a cadeia candidate same-trunk entre slots observados e registros `__VB`
validados. Resultado: `24/24` assets carregados, `218` primitivas, `216` layouts
completos de cinco slots, `1080/1080` slots válidos, `22/24` assets completos
em todas as primitivas e `0` rejeições.

O resultado é `verified-bounded-candidate-fetch-record-association` e o
readiness registra `rendering-candidate-fetch-record-association` como
`verified-structural`. Isso não é binding de UV, material, normal, peso,
skinning, rigging, animação, morph ou GPU; os metadados permanecem opacos e
`original_semantics=not-claimed`. O gate original de renderização continua
`not-proven`.

Relatórios: `fifa_pc/eagl-candidate-fetch-association.tsv` e
`fifa_pc/eagl-candidate-fetch-association-summary.tsv`. A bateria final passou
Release `0/0`, self-test `73/73`, testes Python `32/32`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, captura
windowed e replays determinísticos. A ISO segue somente leitura com
`2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.39. Procurar evidência independente fora do caminho candidate que associe
      estado de renderização a geometria/UV/material; preservar separados os
      `216` layouts completos e os `2` assets parcialmente completos.
1. Manter `NOT_READY`, repetir a bateria final após mudanças e não converter
   metadados de slots em semântica.
2. Não marcar o projeto como concluído e não iniciar code review.

## Atualização desta etapa — cadeia estrutural bruta de renderização EAGL — 2026-10-07

`eagl-original-render-chain-audit` inspecionou, sem mesh candidate ou SHPX, a
cadeia `__Model -> descriptor -> render method -> global parameters -> command
stream` dos objetos same-trunk. Foram verificados `388/388` pacotes, `883/883`
objetos e `2364/2364` primitivas com streams completos; `752` linhas contêm
`SHAPENAME` e `1612` contêm `SetPrimitiveType` como estado não decodificado.

O subgate `rendering-original-render-chain-structure` é
`verified-structural`, mas não é binding de geometria, UV, material ou GPU;
`original_semantics=not-claimed` e readiness `NOT_READY` permanecem. Relatório:
`fifa_pc/eagl-original-render-chain.tsv`, SHA-256
`22BE05FEA7A6EC6ACCC9792C3ADED3BE1DB639E9BDD34F35E0BCFD258B9207BB`; resumo:
`fifa_pc/eagl-original-render-chain-summary.tsv`, SHA-256
`66E8F22E71851056985FB1FF2AFECDAAEF7B87B39C66961A1483E75EA38E1E7F`.

Validação: Release `0/0`, self-test `74/74`, Python `32/32`, matriz completa
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, captura
windowed e replays determinísticos. A ISO segue com `2013003776` bytes,
atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.40. Buscar associação independente estado→geometria/UV/material ou adicionar
      um controle negativo explícito, sem promover os `216` layouts candidate
      completos nem os `2` incompletos a bindings originais.
1. Repetir a bateria final e manter `NOT_READY`; não marcar concluído e não
   iniciar code review.

## Próxima etapa recomendada

0.38. Manter essa auditoria como controle estrutural e buscar associação
      independente entre estado de renderização e geometria/UV/material, ou
      evidência externa verificável. Os padrões de instrução não devem ser
      promovidos a campos, funções ou bindings.
1. Reexecutar readiness, Release, self-test, smokes, runtime e replay após cada
   mudança; preservar a ISO e manter `NOT_READY`.
2. Não marcar o projeto como concluído e não iniciar code review.
## Atualização desta etapa — controle negativo de população de estado EAGL — 2026-10-07

O audit bruto same-trunk passou a registrar os slots dos streams sem atribuir
atributos ou semântica GPU. Foram verificados `2364/2364` streams completos,
`2364` linhas com slots, `2362` sequências estruturais completas `0..4`, `750`
coocorrências `SHAPENAME`+slots e `0` sobreposições `SHAPENAME`+
`SetPrimitiveType`.

A coocorrência é observacional e não cria binding estado→mesh, UV, material,
normal, peso, skinning, rigging, animação, morph ou GPU. O novo subgate
`rendering-original-state-population-negative-control` é
`verified-structural-negative`; renderização principal continua `not-proven` e
readiness continua `NOT_READY`.

Hashes: `eagl-original-render-chain.tsv` —
`629C6D7A836CAA46A84E09A4EEA3DE8F3AC11ADE871219B7E205DF710C07B66C`;
`eagl-original-render-chain-summary.tsv` —
`16FAF6D2F477A621445015A87C05E04896F20F3CA5000B6634E9CBB15D647DF7`;
`project-readiness.tsv` —
`F4DBDEC11A306181CB2943AC1077A8B930CA6401EE40961281AC67D134F354DD`.

Release `0/0`, self-test `74/74`, Python `32/32`, matriz completa, smokes,
runtime headless, captura windowed e replays determinísticos passaram. DLL
Release: `DA0C35BEFF7C600F1E9CF9277EA854FBC6CBE5F8E3745C26ACB6DE50AE12E445`.
A ISO segue somente leitura com `2013003776` bytes, atributo `Archive` e
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.41. Buscar evidência original independente adicional para
      estado→geometria/UV/material; se não houver, manter a lacuna explícita e
      separar a implementação clean-room dos caminhos candidate.
1. Repetir a bateria final após cada mudança; não marcar concluído e não iniciar
code review.

## Etapa 0.41 concluída — controle estrutural da população de estado original

O novo controle `tools/analysis/eagl_original_state_population_control.py` foi
executado a partir do relatório bruto, sem mesh candidate ou SHPX. Resultado:
`883` objetos, `2364` linhas primitivas, `752` SHAPENAME, `1612`
SetPrimitiveType, `0` sobreposições, `0` pares de mesmo índice, `777` pares
adjacentes, `1904` pares na mesma camada, `0` hashes de comando iguais e `0`
formas de fronteira iguais.

Isso permanece evidência estrutural negativa e não decodifica semântica nem
binding. UV, material, skinning, rigging, animação, morph e GPU continuam
explicitamente não promovidos; readiness mantém `NOT_READY` e renderização
principal `not-proven`.

Relatório: `eagl-original-state-population-control.tsv` — SHA-256
`1DB67C35A1AFF669606666E86802E76117E160D05562C8B0E2F3B9CE487BE83F`;
resumo: `eagl-original-state-population-control-summary.tsv` — SHA-256
`6D47AFC8F8826B849985D618645DD59924C1D35D14FEFA879B38B75B58C8E7E4`.
Release, self-test `74/74`, Python `33/33`, matriz completa, smokes, runtime
headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.42. Procurar evidência original independente adicional para
estado→geometria/UV/material; na ausência dela, manter a lacuna explícita e
continuar os subsistemas não decodificados separados do caminho candidate.
Repetir a bateria final após mudanças, sem marcar concluído e sem iniciar code
review.

## Etapa 0.42 concluída — inventário estrutural bruto de `__Model`/`__VB`

Foi ampliado o audit original same-trunk para validar a estrutura delimitada
dos registros `__VB` por símbolo, stride, comprimento e contagem observada.
Resultado: `883` objetos, `883` `__Model`, `882` `__BBOX`, `2364` `__RM` e
`11820/11820` registros `__VB` validados; `882` objetos com bounds e `883`
com streams de vértices validados.

Isso é evidência estrutural comprovada, não binding original. Não foram
atribuídos significados a geometria, UV, material, skinning, rigging, animação,
morph ou GPU, nem foi associado estado a stream. O novo subgate
`rendering-original-vertex-record-structure` é `verified-structural`; o gate
principal de renderização continua `not-proven`, `original_semantics` continua
`not-claimed` e o readiness continua `NOT_READY`.

Hashes: `eagl-original-render-chain.tsv` —
`629C6D7A836CAA46A84E09A4EEA3DE8F3AC11ADE871219B7E205DF710C07B66C`;
`eagl-original-render-chain-summary.tsv` —
`36D8765FB5E3DC1E00ABDF59C8477384575F5D912A1B737F2BB1A971BCD4C8CF`;
`project-readiness.tsv` —
`93065B27B628D955F4261B062F99C347B6B32269785F99AAD70F15BEAEA4FE9B`;
DLL Release —
`0BF974EA15DD900F6D57AE1FEA14BAD7095B36A552BFE398996D686EEED748CD`.

Release `0/0`, self-test `74/74`, Python `33/33`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.43. Procurar associação original independente entre os registros `__VB` e
primitivas/streams. Na ausência de prova, manter `not-proven` e avançar nos
subsistemas ainda não decodificados sem inventar bindings.
1. Repetir a bateria final após cada alteração; não concluir o projeto e não
iniciar code review.

## Etapa 0.43 concluída — associação estrutural opaca `__VB`↔fetch

Foi implementada a auditoria original que compara os cinco strides observados
nos fetch slots com chaves `__VB` de categoria/ordinal consecutivos. Resultado:
`2364` primitivas, `2364` com registro ordinal, `2362` sequências opacas únicas,
`2362` fetch layouts completos e `0` rejeições.

Isso é evidência estrutural comprovada, não binding semântico. Geometria, UV,
material, skinning, rigging, animação, morph e GPU continuam sem semântica
atribuída; `original_semantics=not-claimed`, renderização principal
`not-proven` e readiness `NOT_READY` permanecem.

Hashes: `eagl-original-vertex-record-association.tsv` —
`162D94FA23DB73C469392FFD5AA037FA2D029E7E06D47B32C2692004BA7B9ECD`;
resumo —
`257C8F331CB6D9B3E9BB99E18F1DAEA99298779A8B035E78126B14DB26D118CE`;
readiness —
`5C6468CB5D769B00EB88F0B3213D7BA293CEB8949815A5E5F3951B0526B8EBF9`;
DLL Release —
`030372D0DBDB657E53D09871B18B46B021F469F6F6BA112B8281C14E1A7C3FD1`.

Release `0/0`, self-test `75/75`, Python `33/33`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.44. Aplicar controles independentes contra a associação opaca e buscar
evidência original de geometria/UV/material fora de nomes, strides e comandos
não decodificados, sem inventar bindings.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.46 concluída — controle independente dos payloads originais `__VB`

`eagl_original_vertex_payload_control.py` reparseou de modo independente o
relatório original e validou `883/883` objetos: `883/883` assinaturas numéricas
coerentes, `883/883` metadados de ranges válidos e `883/883` statuses de entrada
válidos. Status: `verified-independent-original-numeric-payload-control`.

Isto é controle estrutural/negativo, não binding semântico. Posição, geometria,
UV, material, skinning, rigging, animação, morph e GPU seguem sem semântica
atribuída; renderização principal segue `not-proven`,
`original_semantics=not-claimed` e readiness `NOT_READY`.

Hashes: controle
`0DD9A6EB1E5EEC72D309CCACDC7E7C69D632CF8E02722EB4EB77E14D4052D013`, resumo
`6E817B35190ACBC726EC70D09C2E8516D2002AF3C97643BC188CAA0EF8D60A04`, relatório
original `EC69F3A7D0A3F56FB0178AE5A39B68258FFE749F57F5C0BEDCB9A27E93C99E3C`,
resumo original `9487A2A19F9F9FC8E9403C068FBA54A895289695C3BF2055D6AEDB0FF4D63C21`,
readiness `A5731AC06117F6BE6B98BCCC5259519E4697F51FBC0215672FEB5349D5E1C2B0` e
DLL Release `A4A05B3728A9D724F7B69CAB6E17CB42756BA2FF4EA2DA70E7D00DE5B480C55F`.

Release `0/0`, self-test `76/76`, Python `35/35`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.47. Buscar evidência original independente de geometria, UV, material, áudio
ou animação ainda não decodificada, mantendo `not-proven` e sem inventar
bindings.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.45 concluída — estrutura numérica dos payloads originais `__VB`

`eagl-original-vertex-payload-audit` validou no caminho original `388/388`
pacotes, `883/883` objetos e `11820` streams `__VB`; houve perfil finito em
`883` objetos, contenção nos bounds em `882`, ranges distintos em `883`, zero
sobreposições e nenhuma rejeição. `float2` próximo da faixa unitária é apenas
perfil numérico; não foi chamado de UV.

O resultado não promove semântica: posição, geometria, UV, material, skinning,
rigging, animação, morph e GPU continuam não decodificados, com
`original_semantics=not-claimed`. O subgate
`rendering-original-numeric-vertex-payload-structure` é
`verified-structural`, mas renderização principal continua `not-proven` e o
readiness `NOT_READY`.

Hashes: relatório —
`EC69F3A7D0A3F56FB0178AE5A39B68258FFE749F57F5C0BEDCB9A27E93C99E3C`; resumo —
`9487A2A19F9F9FC8E9403C068FBA54A895289695C3BF2055D6AEDB0FF4D63C21`;
readiness — `819BB6F05291BC2B8B3AFCD9961851B79EE3A586FE4A682344992E7D3C4949B0`;
DLL Release — `BF1B3B40F4382A7CB51BFADD89C4C277133AF2FE5E81D06B46D8FD2AE1234665`.

Release `0/0`, self-test `76/76`, Python `34/34`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.46. Buscar evidência original independente de geometria, UV ou material além
dos perfis numéricos e ranges, mantendo `not-proven` e sem inventar bindings.

## Etapa 0.44 concluída — controle independente da associação opaca

O script `eagl_original_vertex_record_association_control.py` reparseou o
relatório original e reproduziu `2362/2362` sequências exatas em `2364` linhas
e `883` objetos. Os controles de ordem invertida e ordinal deslocado deram
`0/0`.

O resultado é controle estrutural negativo, não binding semântico. Geometria,
UV, material, skinning, rigging, animação, morph e GPU permanecem sem
semântica atribuída; renderização principal continua `not-proven`,
`original_semantics=not-claimed` e readiness `NOT_READY`.

Hashes: relatório —
`3D5F57E04D65C4956E5F26AB994ABD5DA1C2ED79DEE1D905C4BC5DE62F75513A`; resumo —
`26E1DABB216D91957886D8C97ED3ADF5DC5BA448D8D36E260CBFA05C60FF9765`;
readiness —
`EC399C25E4E12B19618F3FCA02C29ECC4F9E8532CF8AA0547DFA21F83003A699`;
DLL Release —
`56D7443171E80962DBA901970AE8764A59BBCAD10456B76B4904E9387B304A04`.

Release `0/0`, self-test `75/75`, Python `34/34`, matriz completa, smokes,
runtime headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.45. Buscar evidência original independente fora das chaves, strides e
comandos opacos, especialmente geometria/UV/material, sem inventar bindings.
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

`eagl_original_render_payload_object_join.py` fez uma junção independente dos
relatórios originais por `(model_archive, model_object)`. Foram confirmados
`883/883` objetos unidos, `0` objetos exclusivos em cada fonte, `2364`
primitivas, `11820` streams validados e `883/883` linhas com identidade,
status e contadores estruturais coerentes; controles invertido e deslocado:
`0/0`.

O resultado é apenas estrutural no nível de objeto. Não associa primitiva a
stream e não decodifica posição, geometria, UV, material, atributo, skinning,
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
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.49. Buscar associação original independente entre primitivas, streams e
atributos ou comandos/payloads, mantendo `not-proven` e sem inventar bindings.
1. Repetir a bateria final após qualquer alteração; não concluir o projeto e
não iniciar code review.

## Etapa 0.49 concluída — topologia estrutural primitiva/stream `__VB`

O audit C# `eagl-original-primitive-stream-topology-audit` e o controle
independente `eagl_original_primitive_stream_topology_control.py` validaram
somente a topologia dos dados originais: `2364` primitivas, `11820` streams,
`11820/11820` ordinais, `11814/11820` strides, `11820/11820` ranges válidos,
`2362` primitivas completas e `883/883` objetos com ranges não sobrepostos.
O controle de slots invertidos deu `0`; o controle de ordinal deslocado deu
`1479`, mantido explicitamente como ambiguidade de layouts repetidos.

Status: `verified-bounded-original-primitive-stream-topology`, controle
`verified-independent-original-primitive-stream-topology-control` e
`original_semantics=not-claimed`. Não houve promoção para posição, geometria,
UV, material, atributo, skinning, rigging, animação, morph ou GPU. Renderização
original continua `not-proven`; readiness continua `NOT_READY`.

Artefatos e hashes: relatório
`6A9CF9A6B92939552050C499261876724191D518DB1470E79CD7338F67A1BEE0`, resumo
`C10159DE7C3A7CC38EE8625E91D7D05679187092E534E51840B256899B0D4612`, controle
`4FFFE8E3C76D7C9389F44AD60C0F43EBBDC114D339C531012AC1B84BC837FA97`, resumo do
controle `777AA578F901AA94FEE0EC3307300C4A5DE9F6BF4E66F73BCFB9D675853479B9`,
readiness `7818A02C248A03351C6902C96EC8324BC755AA92FE70C35D1C7F4D36F051709A`
e DLL Release `C873228A606ED79CB02F79B0258748220FE22EAEB1ECE2E14C813A79E143D294`.

Validação final: Release `0/0`, self-test `78/78`, Python `38/38`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A ISO permaneceu sem
escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.50. Discriminar os layouts/ordinais repetidos usando evidência independente
de comando/payload; manter `not-proven` e não inventar bindings.

## Etapa 0.50 concluída — controle negativo de comandos/payloads opacos

Foram implementados o audit C# `eagl-original-primitive-stream-opaque-discrimination-audit`
e o controle independente `eagl_original_primitive_stream_opaque_discrimination_control.py`.
Eles comparam palavras brutas dos comandos de fetch com registros/descritores
`__VB` validados e uma janela limitada de bytes de payload, sem decodificar ou
promover semântica.

Resultado: `388/388` pacotes, `883/883` objetos, `2364` primitivas, `2362`
fetches completos, `10240` candidatos ordinais, `10220` candidatos por stride,
`0` matches por metadata, `0` por prefixo de payload, `0` discriminações únicas
e `1479` candidatos de stride deslocado. O controle independente confirmou
`2364/2364` linhas coerentes. Isso é um controle negativo verificado: os
layouts/ordinais repetidos continuam ambíguos e não houve promoção de binding.

Status: `verified-bounded-original-primitive-stream-opaque-discrimination`,
controle `verified-independent-original-primitive-stream-opaque-discrimination-control`,
`original_semantics=not-claimed`. Permanecem não decodificados posição,
geometria, UV, material, atributo, skinning, rigging, animação, morph e GPU.
Readiness continua `NOT_READY`; renderização original continua `not-proven`.

Artefatos e hashes: relatório
`845C2A9AA02ED9FBB6F2CC3DD73E143A7822735E19F3E1A65E55BC703F68007B`, resumo
`B857C8D31DFE2CF42A63E0AEA79AFA4697F0D1D86BC430AD8018757C2968BBC9`, controle
`00C438AB565BC6774E2E22F3929FB9F7F6B3B611C0FF59351E37F2766B1ACB5A`, resumo do
controle `496ED3A7CB97A1E4EB5E05F88102636FB5D3A86EF1002C05F399BA9CD2FFE1A8`,
readiness `C056B78C45EB75DAF0CEB31320F10E6FFAC852C6FC5301595E7CAD92748DF43D`
e DLL Release `8209763D4DD2AD6328309FCC5440495CCC04E0B3ED37603296DF9B4A5E85A959`.

Validação final: Release `0/0`, self-test `79/79`, Python `39/39`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A ISO permaneceu sem
escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima etapa recomendada

0.51. Buscar identidade independente de comando/payload ou semântica de
registro decodificada que discrimine os candidatos repetidos; manter
`not-proven` e não inventar bindings.

## Etapa 1.08 — intervalo estrutural do catálogo no tail de `__AnimationBank` — 2026-10-08

Foi implementado `fifa_pc/Runtime/AnimationBankCatalogTailRangeAudit.cs` e o
controle independente `tools/analysis/animation_bank_catalog_tail_range_control.py`.
O comando `animation-bank-catalog-tail-range-audit` usa somente a ISO em modo
somente leitura e os catálogos/ELFs delimitados; não houve execução nem
recompilação do XBE.

Foram validados 19/19 bancos e 1.584/1.584 entradas. Tail de 32 bytes,
contagem `2x`, intervalo em `.data`, alinhamento, comprimento `8x` e
relocations `2x` passaram em 19/19; intervalos sem relocations: 0. Status:
`verified-structural-animation-bank-tail-range`; controle:
`verified-independent-animation-bank-catalog-tail-range-control`.

A evidência permanece somente estrutural: não prova record/frame/pose/rig,
timing, skinning, morph ou seleção de animação. Portanto,
`candidate_record_binding=not-proven`, `original_semantics=not-claimed` e a
readiness permanece `NOT_READY`. Rendering e assets continuam não provados,
áudio original continua parcial/não provado, deformação continua estrutural e
gameplay permanece aprovado apenas como clean-room authored.

Validação: Release `0/0`, self-test `94/94`, matriz concluída com código 0,
10 cenários, 777 frames, 54 eventos de áudio e 1.554 amostras de animação;
smokes, runtime headless/windowed e replays determinísticos passaram. ISO sem
alteração: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.

Próxima etapa: buscar evidência independente para estado original →
draw/UV/material, `EVT→ABKC→sample→playback` e timing/pose/rig/deformação,
sem promover bindings ou declarar o projeto concluído.

## Etapa 1.09 — relocations dos pares no tail de `__AnimationBank` — 2026-10-08

Foram implementados `fifa_pc/Runtime/AnimationBankTailPairRelocationAudit.cs`,
o comando `animation-bank-tail-pair-relocation-audit` e o controle
`tools/analysis/animation_bank_tail_pair_relocation_control.py`. A leitura foi
somente da ISO, do `.data`, dos 19 ELFs/catálogos de animação e dos relatórios
delimitados; não houve execução nem recompilação do XBE.

Passaram 19/19 bancos e 1.584/1.584 pares: duas relocations por par nos
offsets esperados, zero multiplicidades inesperadas, 1.584/1.584 primeiras e
segundas palavras dentro de `.data`, zero matches com símbolos conhecidos,
1.584 pares raw distintos e 19/19 layouts completos. O detalhe tem 1.584
linhas. Status: `verified-structural-animation-bank-tail-pair-relocations`;
controle: `verified-independent-animation-bank-tail-pair-relocation-control`.

É evidência estrutural somente. Não há join provado entre ordinal e nome do
catálogo, nem semântica de record/frame/pose/rig/timing/skinning/morph/playback:
`candidate_record_binding=not-proven` e `original_semantics=not-claimed`.
Readiness segue `NOT_READY`; assets/rendering/áudio original/deformação seguem
sem gate semântico, e gameplay segue aprovado apenas como clean-room authored.

Validação: Release `0/0`, self-test `95/95`, matriz código 0, 10 cenários,
777 frames, 54 eventos de áudio e 1.554 amostras de animação; smokes, runtime
headless/windowed e replays determinísticos passaram. ISO intacta:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: só usar target-prefix/relocations se aparecer um join
independente demonstrável; em paralelo, continuar estado → draw/UV/material,
`EVT→ABKC→sample→playback` e timing/pose/rig/deformação, sem promover
bindings, concluir o projeto ou iniciar code review.

## Etapa 1.10 — janelas de relocation dos targets do tail — 2026-10-08

Foram implementados `fifa_pc/Runtime/AnimationBankTailPairTargetChainAudit.cs`
e `tools/analysis/animation_bank_tail_pair_target_chain_control.py`. O audit
usa janelas de 32 bytes nos dois targets raw de cada par e mantém a
multiplicidade e os words como evidência estrutural, sem executar ou
recompilar o XBE.

Foram validados 19/19 bancos, 1.584/1.584 linhas e 1.584/1.584 pares com
janelas não vazias. A multiplicidade detalhada foi first `2:1, 3:17, 4:1554,
5:12` e second `3:328, 4:1237, 6:19`. Nenhuma janela teve exatamente uma
relocation em cada lado, não houve resolução de segundo nível e não houve
símbolo conhecido; a hipótese de um único hop permanece não provada.
Status fonte: `verified-structural-animation-bank-tail-pair-target-chain`;
controle: `verified-independent-animation-bank-tail-pair-target-chain-control`.

Readiness continua `NOT_READY`; a nova linha é estrutural e não promove
assets, rendering, áudio original ou deformação. `candidate_record_binding` é
`not-proven`, `original_semantics` é `not-claimed`, e gameplay permanece
clean-room authored.

Validação: Release `0/0`, self-test `96/96`, matriz código 0, 10 cenários,
777 frames, 54 eventos de áudio e 1.554 amostras; smokes, runtime
headless/windowed e replays determinísticos passaram. ISO intacta:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar com evidência independente de record/frame/pose/rig/
timing ou com estado → draw/UV/material e `EVT→ABKC→sample→playback`, sem
promover bindings, concluir ou iniciar code review.

## Etapa 1.11 — identidade SHA-256 do command stream candidato — 2026-10-08

`fifa_pc/Runtime/EaglCandidateFetchAssociationAudit.cs` passou a registrar o
SHA-256 dos bytes brutos de cada command stream candidato. O controle
independente `tools/analysis/eagl_original_candidate_primitive_fetch_control.py`
reparseia as fontes por chave de primitiva e reconciliou `218/218` hashes,
junto com `218/218` contagens e streams completos; `216` linhas têm a sequência
completa de cinco slots e `2` permanecem explicitamente não materializadas.

Status: `verified-independent-original-candidate-primitive-fetch-control`;
`original_semantics=not-claimed`. A igualdade raw é evidência estrutural de
identidade do command stream e não promove state→draw, posição, UV, material,
sampling, GPU, skinning, rigging, animação, morph ou binding original.
Também foram atualizados o reparse C#, o self-test, `ProjectReadinessAudit.cs`
e a matriz. Rendering segue `not-proven`, assets `candidate-integrated`, áudio
`partially-integrated`, deformação `structural-only` e o projeto `NOT_READY`.

Validação: Release `0/0`, self-test `96/96`, matriz código 0 com 10 cenários,
777 frames, 54 eventos de áudio e 1.554 amostras; smokes, runtimes
headless/windowed e replays determinísticos passaram. ISO intacta e somente
leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência independente discriminativa de estado → draw/UV/
material, `EVT→ABKC→sample→playback` ou timing/pose/rig/deformação; não
inventar bindings, concluir o projeto, executar/recompilar XBE ou iniciar code
review.

## Etapa 1.12 — ambiguidade do UID de runtime no state→geometry — 2026-10-08

O controle independente `tools/analysis/eagl_original_state_uid_ambiguity_control.py`
foi integrado à matriz e ao readiness. A reparse por objeto e por
`RUNTIME_ALLOC::UID` validou `24/24` objetos, `218` linhas render-scoped,
`38` geometrias e `180` estados. Todos os `38/38` geometrias e `180/180`
estados possuem UID válido; há `24` domínios compartilhados, mas cada uma das
`38/38` geometrias mapeia para múltiplos estados (`0` candidatos únicos,
`38` múltiplos) e os `24/24` objetos permanecem ambíguos. Binding único provado:
`0`. Status: `verified-independent-original-state-runtime-uid-ambiguity-control`,
`semantic_binding=not-proven`, `original_semantics=not-claimed`.

A linha correspondente do readiness é `verified-structural-negative=true`,
sem promoção semântica: rendering original continua `not-proven`, assets
`candidate-integrated`, áudio `partially-integrated`, deformação
`structural-only`, gameplay clean-room authored e projeto `NOT_READY`.

Validação: Release `0/0`, self-test `96/96`, matriz código 0, 10 cenários,
777 frames, 54 eventos de áudio e 1.554 amostras de animação; smokes, runtime
headless/windowed e replays determinísticos passaram. ISO preservada:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência independente de estado → draw/UV/material,
`EVT→ABKC→sample→playback` ou timing/pose/rig/deformação. Não inventar
bindings, concluir, executar/recompilar XBE ou iniciar code review.

## Etapa 1.13 — UID + SHA-256 do command stream não discrimina state→geometry — 2026-10-08

O controle independente
`tools/analysis/eagl_original_state_uid_command_hash_ambiguity_control.py`
foi integrado à matriz e ao readiness. Ele reparseia por objeto,
`RUNTIME_ALLOC::UID` e SHA-256 dos bytes brutos do command stream, usando
cardinalidade zero/um/muitos e sem atribuir semântica original aos bytes.

Foram unidos `24/24` objetos em `218` linhas render-scoped, com `38`
geometrias e `180` estados; `38/38` geometrias e `180/180` estados têm UID
válido e existem `24` domínios compartilhados. Todas as `38/38` geometrias
compartilham UID com estados, porém `0` têm candidato único e `38` têm
múltiplos. Com UID + hash do command stream, `0` têm candidato, `38` têm zero
e `0` têm binding único provado; os `24/24` objetos permanecem ambíguos.
Logo, o hash bruto não discrimina state→geometry.

Status: `verified-independent-original-state-uid-command-hash-ambiguity-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. A linha do
readiness é `verified-structural-negative=true` apenas para essa negativa;
rendering original segue `not-proven`, assets `candidate-integrated`, áudio
`partially-integrated`, deformação `structural-only`, gameplay clean-room
authored e projeto `NOT_READY`.

Validação: Release `0/0`, self-test `96/96`, matriz código 0,
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54
eventos de áudio e 1.554 amostras; smokes, runtime headless/windowed e
replays determinísticos passaram. ISO preservada e somente para leitura:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar com evidência independente de estado → draw/UV/
material, `EVT→ABKC→sample→playback` ou timing/pose/rig/deformação; não
inventar bindings, concluir, executar/recompilar XBE ou iniciar code review.

## Etapa 1.14 — identidade PCM nomeada EVT e controle negativo ABK — 2026-10-08

Foi implementado e integrado à matriz e ao readiness o controle independente
`tools/analysis/audio_event_named_payload_identity_control.py`. Ele reparseia e
une por chave os relatórios de runtime nomeado, decoder EVT e correlação
exaustiva de payloads, sem promover seleção de amostra ou semântica original.

Foram unidos `17` eventos de runtime, `17` de decode nomeado e `17` de
correlação; todos os `17/17` WAVE bytes, contagens PCM e SHA-256 PCM coincidem.
A correlação contém `242` linhas de banco, `241` decodificadas, `0` hashes de
evento com match de banco e `0` pares PCM exatos. Status:
`verified-independent-audio-event-named-payload-identity-control`;
`direct_selection_binding=not-proven`; `original_semantics=not-claimed`.

O resultado prova somente a identidade do payload WAVE/PCM nomeado EVT e a
consistência da entrada candidate clean-room. Não prova
`EVT→ABKC→sample`, vínculo de evento de gameplay nem playback original; por
isso a linha é `verified-structural-negative` e nenhum gate semântico foi
promovido. Rendering original continua `not-proven`, assets
`candidate-integrated`, áudio `partially-integrated`, deformação
`structural-only`, gameplay authored clean-room e o projeto `NOT_READY`.

Validação: Release `0/0`, self-test `96/96`, matriz código 0 com
`completed-authored-and-candidate-opt-in`, 10 cenários, 777 frames, 54 eventos
de áudio e 1.554 amostras; smokes relevantes, runtime headless/windowed e
replays determinísticos passaram. ISO preservada e somente para leitura:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar com evidência independente de
`EVT→ABKC→sample→playback` ou de estado original → draw/UV/material e
timing/pose/rig/deformação; não inventar bindings, concluir,
executar/recompilar XBE ou iniciar code review.

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

Foi integrado o controle independente
`tools/analysis/animation_catalog_pose_timing_global_assignment_control.py`.
Ele cruza timestamps AXT agregados, âncoras de payload, índice de pose e
contenção de morph/relocation apenas como evidência estrutural.

Foram validados `19` bancos, `1.584` entradas/timestamps, `3.168` âncoras em
`19/19` bancos, `391` poses e `393` layouts morph. A contenção totalizou `159`
pares em `53` layouts (`8` singleton, `45` ambíguos), sem gap-set exato. Os
`391` poses não possuem join key com catálogo ou morph; a enumeração global
ficou `19→19` candidatos e `7.429→7.429` modelos, com zero binding único.

Status `verified-independent-animation-catalog-pose-timing-global-assignment-control`;
`semantic_binding=not-proven`; `original_semantics=not-claimed`. O readiness
registra o controle como `verified-structural-negative`, `391/391`; o gate de
animação/deformação continua `structural-only` e o projeto continua
`NOT_READY`.

Validação: build Release `0/0`, self-test `96/96`, matriz completa com código
`0`, smokes relevantes, runtime headless/windowed e replays determinísticos
passaram. ISO somente para leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar a busca por evidência original discriminativa de
timing→pose→rig→skinning/deformação, state→draw/UV/material ou
`EVT→ABKC→sample→playback`; não inventar bindings, executar/recompilar XBE,
marcar conclusão ou iniciar code review.

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

Foram validados `58` xrefs estáticos, `23` literais únicos, `147` contextos,
`108` chamadas executáveis, `56` alvos distintos, `14` alvos com múltiplos
literais, `1` match aninhado e `10` matches XDVDFS. Em `EVENTSPA.EVT`, foram
validados `17` nomes e `1.061` pares candidatos, mas `0` xrefs literais de
evento. O domínio de payload teve `17` eventos e `241` candidatos de banco,
sem match PCM exato e sem hash de evento com match. Os `17` eventos ficaram sem
xref estático de evento; os modelos globais ficaram `952→952`, a redução foi
`0` e nenhum seletor único foi provado.

Status: `verified-independent-audio-event-static-context-global-assignment-control`;
`semantic_binding=not-proven`; `direct_selection_binding=not-proven`;
`original_semantics=not-claimed`. O readiness registra o controle como
`verified-structural-negative`, passado em `17/17`, sem alterar os gates
principais: áudio `partially-integrated`, rendering `not-proven`, assets
`candidate-integrated`, deformação `structural-only`, gameplay
`clean-room-integrated` e projeto `NOT_READY`.

Validação: build Release `0` warnings/`0` errors, self-test `96/96`, matriz
exit `0` com `completed-authored-and-candidate-opt-in`; smokes relevantes,
runtime headless/windowed e replays determinísticos passaram. A matriz
reportou `10` cenários, `777` frames, `54` eventos de áudio e `1.554`
amostras de animação. ISO somente para leitura: `2013003776` bytes, atributo
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar buscando evidência independente e discriminativa de
`EVT→ABKC→sample→playback`, state→draw/UV/material ou
timing→pose→rig→skinning/deformação; não inventar bindings, não executar ou
recompilar XBE, não marcar o projeto como concluído e não iniciar code review.

## Registro da etapa executada — 1.24 — 2026-10-09

XboxRecomp foi aplicado somente como analisador estático do XBE Original Xbox,
mais alinhado ao alvo que PS2Recomp. O recompilador, o runtime nativo e o
harness que executa código não foram usados; nada do C/runtime upstream foi
incorporado. O projeto segue clean-room em .NET e sem emulação. Checkout
analisado fora do workspace: `193e2995ffaec871bb3be43a43bd66aa5fccfeb1`.

O controle validou o dispatcher EAGL de base `0x0031A930`: 42 slots
executáveis, 39 destinos diferentes e primeiro word fora da faixa executável
no índice 42. Nas 2.364 streams completas há 36.779 comandos e 12 IDs
distintos; 12/12 correspondem a slots executáveis. Parser XBE: título
`0x45410085`, XDK 5849, entry `0x0024D8BE`, 11 seções, `.text=0x271AD8`.
Disassembly heurística: 15.288 candidatos a função, 858.228 instruções e
156.896 xrefs; boundaries continuam heurísticos. Foi confirmado apenas
header→índice estático da tabela, sem semântica ou comportamento de handler.

O subgate `rendering-original-command-id-dispatch-control` está
`verified-structural` 12/12; `semantic_binding=not-proven` e
`original_semantics=not-claimed`. Não fecha rendering nem qualquer gate
principal; o projeto continua `NOT_READY`. Release `0/0`, self-test `96/96`,
matriz `completed-authored-and-candidate-opt-in`, smokes, runtimes headless e
windowed e replays determinísticos passaram: 10 cenários, 777 frames, 54
eventos, 1.554 amostras. ISO somente leitura, `2013003776` bytes, `Archive`,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo passo: investigar estaticamente destinos e call sites com evidência
discriminativa, sem converter fronteira inferida ou opcode em significado.
Referências: [XboxRecomp](https://github.com/sp00nznet/xboxrecomp) e
[guia de decompilação](https://github.com/sp00nznet/xboxrecomp/blob/main/docs/DECOMP.md).


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



