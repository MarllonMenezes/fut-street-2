# FIFA Street 2 — status de recompilação

## Etapa 1.07 — controle de nomes EVT contra o universo de áudio

`fifa_pc/Runtime/AudioEventNameAssetTextSearchAudit.cs` e `tools/analysis/audio_event_name_asset_text_search_control.py` foram integrados à matriz, ao self-test e ao `ProjectReadinessAudit`. A busca streaming usa os `17` nomes candidatos reparseados do índice textual de `data/audio/eventspa.evt` e cobre os `60` assets restantes: `60/60` válidos, `1.737.085.606` bytes, `0` pares asset/nome com hit e `0` ocorrências; o controle independente reconcilia `1.020/1.020` linhas sem hit e `0` duplicidades.

É uma observação estrutural negativa de namespace. Não promove EVT→ABKC/sample/playback, não identifica evento original e não altera os gates principais: áudio segue parcialmente integrado, rendering `not-proven`, animação-deformação `structural-only`, gameplay clean-room integrado e readiness `NOT_READY`. Validação completa: matriz exit code `0`, Release `0` avisos/`0` erros, self-test `93/93`, smokes relevantes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras.

Hashes SHA-256: auditor `439D5AB62558278E5CC80CA41AB7CAED8EC7C6DC86ED46EF518363E0E3FB20F5`; controle Python `4BB18D4534BFD5E57F568DAA0627CACDF0F2AA9DE7CEBD46AB61C1C03552F90C`; relatório `9B8995864273ADF880CD02BFA2E10F5F08BA8C9F44B8D398D33F1284390A1ADD`; summary `04EF71651802C2F093C53312683264E35425BA699BD8A62711657D3D57035BF2`; controle `B745AED8D07629B5000183DE9B35C3B59544193422AD1B73825265C4B4351961`; controle summary `886FCD7A7E36E23D56D175D4D111F7C0E38F74708525A79ECACF03397D860174`; readiness `23FC205E7488ECBFCB2F0FF0A5DE25A5F13EAA879E8AFACFD5EAE262348F0FF2`.

## Etapa 1.06 — controle independente state/fetch + slot numérico + textura/raster candidata — 2026-10-08

`tools/analysis/eagl_original_state_slot1_candidate_uv_ambiguity_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. A reparse independente fechou `2364/2364` primitivas globais; no subconjunto candidato, `218` grupos (`216` completos, `2` incompletos), `180` estados SHAPENAME (`178` completos), com o perfil near-unit no slot `1` em `178/178` grupos SHAPENAME completos, zero em outro slot e zero vínculos únicos state→geometria. Textura/atributo e influência raster candidatas validaram `24/24` objetos.

É controle estrutural negativo: não identifica UV, não prova state→draw/material e não promove candidatos para semântica original. O subgate `rendering-original-state-slot1-candidate-uv-ambiguity-control` está `verified-structural-negative` (`24/24`), com `semantic_binding=not-proven` e `original_semantics=not-claimed`; o readiness permanece `NOT_READY`.

Validação: Release `0/0`, self-test `92/92`, matriz completa exit code `0`, smokes, runtimes e replays determinísticos; ISO somente leitura, `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Hashes SHA-256: controle `FCFF267936F6B03FD94174600D443424CEDD399E9634B253DD20E511D25B6449`; relatório `A61381C76D19CCCFBD995EBB928770B975F0EF923A383F8192ADCADC36F6ACE1`; summary `BFAF6A5AF309D92E0CAB5BBABB6270BA0D6326E3E516914E8C3B5079BFBD5C17`; auditor `20099B92DC1BCA0F58F577A1A8EF7ADB3BE9F2BFDBD172F04A09B0EC774DE363`; matriz `CD626E7E0D90FC4F8BEE016C20978E6F69CE69BB7D0DF12F704609DC2853BEDC`.

Próxima investigação: associação original única `state→draw` com UV/material ou `EVT→ABKC→sample→playback`.

## Etapa 1.05 — controle independente de ambiguidade nominal FIFAST2.csi↔EVT — 2026-10-08

O controle `tools/analysis/fifast2_csi_evt_name_ambiguity_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. A reparse independente de `100` nomes `FIFAST2.csi` e `17` candidatos nomeados `eventspa.evt` obteve `100/100` linhas CSI e `17/17` EVT válidas, `17` nomes EVT únicos; `10` EVT sem sobreposição, `3` com uma, `4` ambíguos e `7` com alguma sobreposição. Não houve nome exato nem binding único provado.

É evidência nominal negativa, não binding: não atribui semântica ao CSI, não escolhe ABKC/AST/BNK ou amostra, não liga gameplay a playback e não promove nomes como `Goal`, `GB`, `Tackle` ou `Panna`. O subgate `audio-fifast2-csi-evt-name-ambiguity-control` está `verified-structural-negative` (`17/17`), com `semantic_binding=not-proven`, `direct_selection_binding=not-proven` e `original_semantics=not-claimed`; o readiness permanece `NOT_READY`.

Validação: matriz completa `completed-authored-and-candidate-opt-in`, exit code `0`, Release `0/0`, self-test `92/92`, smokes, runtimes headless/windowed, replays determinísticos e SHA-256 da ISO verificados; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Sem execução ou recompilação do XBE.

Hashes SHA-256: controle `FA75AD1D77492032A54992A8611A5DDF7D75DB046EC2D82948D417653E88A942`; relatório `E334171FE7B0BC9A417895C8424A6FC132332BDDF5AA051132A1D20B8C059098`; summary `0C23206306F0959DE7AA035F91F838074E8F278E614C71F6D3F2F9292CC7444F`; `ProjectReadinessAudit.cs` `3366D1C53283818812B2CE3119339CE40D274C3A28CE186FB14893C4B3B685C2`; matriz `97D982C73A045366B774750550370E76357A7247C0ADD692068381D2DFD55A46`.

Próxima investigação: evidência discriminativa `state→draw` com UV/material ou `EVT→ABKC→sample→playback`.

## Etapa 1.04 — coocorrência independente command-descriptor e papéis numéricos — 2026-10-08

O controle `tools/analysis/eagl_original_command_slot_role_cooccurrence_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. A junção independente por primitiva reconciliou `2364/2364` descriptors com `11820` linhas de papéis numéricos; `2364/2364` ordinais atuais estão presentes, coincidentes e únicos, com zero matches deslocados. Foram preservados `2362` grupos completos e `2` incompletos; todos os completos têm o único perfil float2 de faixa unitária no slot bruto `1` e nenhum o tem em outro slot.

Trata-se somente de coocorrência estrutural negativa, sem atribuir ao slot semântica de UV, posição, material, atributo, geometria, draw ou GPU, nem de skinning, rigging, animação ou morph. O subgate `rendering-original-command-slot-role-cooccurrence-control` está `verified-structural-negative` (`2362/2362`), enquanto rendering continua `not-proven`, com `semantic_binding=not-proven`, `original_semantics=not-claimed` e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `92/92`, matriz completa exit code `0`, smokes, runtime headless/windowed, replays determinísticos e SHA-256 da ISO verificados; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Sem execução ou recompilação do XBE.

Próxima investigação: evidência discriminativa state→draw/UV/material ou EVT→ABKC→sample→playback.

## Etapa 1.03 — coocorrência independente estado/fetch e papéis numéricos por primitiva — 2026-10-08

O controle `tools/analysis/eagl_original_state_numeric_slot_cooccurrence_control.py` foi integrado à matriz e ao `ProjectReadinessAudit`. Ele une independentemente o estado/fetch original ao relatório de papéis numéricos por primitiva: `2364/2364` junções, `2364` grupos, `2362` grupos completos de cinco slots e `2` incompletos preservados. Todos os grupos completos têm um único perfil float2 de faixa unitária no slot bruto `1` (`2362/2362`), nenhum em outro slot; `752` linhas têm `SHAPENAME` e `1612` têm `SetPrimitiveType`.

O resultado é somente coocorrência estrutural negativa. Não atribui estado a draw, geometria, UV, material, skinning, rigging, animação, morph ou GPU. O subgate `rendering-original-state-numeric-slot-cooccurrence-control` está `verified-structural-negative` (`2362/2362`), mas o gate principal de rendering permanece `not-proven`, com `semantic_binding=not-proven`, `original_semantics=not-claimed` e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `92/92`, matriz completa exit code `0`, smokes, runtime headless/windowed, replays determinísticos e SHA-256 da ISO verificados; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras de animação. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Sem execução ou recompilação do XBE.

Hashes SHA-256: controle `CEB60207BDEE7450D2C77A13B9BD0DF09047CAA7AFA81B4810023A97E575FA512`; relatório `5ABFB9D19F0E031B49CF8F62A0179A0CDD8D0E740290BCAEAA399883363D5CB3`; summary `6ACCCC0125CE33344EA73C98576C8BFB2A043FB5683BB6C4D9C71A81762CF6C`; `ProjectReadinessAudit.cs` `C7334F270FC4C846B570C2D00D710D76C4D375C670DF5879E8B2B5EF2592E2EA`; matriz `33143D52276B6C78094934E72CC56BD15C013B80C82F763114B15BEA7809FE6C`.

Próxima investigação: evidência discriminativa state→draw/UV/material ou EVT→ABKC→sample→playback.

## Etapa 1.02 — discriminação independente de papéis numéricos por slot de primitiva — 2026-10-08

O novo controle `tools/analysis/eagl_original_numeric_slot_role_discrimination_control.py` une os relatórios originais numérico e topológico por chave de símbolo e verifica `11820/11820` linhas, `2364` grupos de primitiva, `2362` grupos completos e `2` incompletos preservados. Nos grupos completos, `2362/2362` têm um único perfil float2 de faixa unitária no slot bruto `1`; zero têm esse perfil em outro slot. O teste de bounds encontra `2361` correspondências no slot `0` e uma ausência. Trata-se apenas de discriminação estrutural: `semantic_binding=not-proven`, `original_semantics=not-claimed`, sem atribuir UV, posição, material, geometria ou GPU.

O subgate `rendering-original-numeric-slot-role-discrimination-control` está `verified-structural` (`2362/2362`), enquanto o gate principal de rendering permanece `not-proven`; assets, áudio e animação-deformação seguem não passados e o projeto continua `NOT_READY`.

Validação: Release `0/0`, self-test `92/92`, matriz completa exit code `0`, smokes, runtime headless/windowed, replays determinísticos e SHA-256 da ISO verificados; `10` cenários, `777` frames, `54` eventos de áudio e `1554` amostras de animação. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Sem execução ou recompilação do XBE.

Próxima investigação: evidência independente state→draw/UV/material ou EVT→ABKC→sample→playback.

## Etapa 1.01 — controle independente do universo completo de dados para hashes EVT — 2026-10-08

Foi implementado `fifa_pc/Runtime/AudioEventDataAssetHashSearchAudit.cs` e integrado o comando `audio-event-data-asset-hash-universe-audit` à matriz, ao self-test e ao `ProjectReadinessAudit`. O controle varre `data` inteiro, exclui somente `data/audio/eventspa.evt` e verifica os 17 candidatos como palavras little-endian alinhadas: `121/121` assets válidos, `2.008.984.464` bytes, uma linha-fonte excluída e um hit em `data/movies/advoffn.vp6`, `0x34214F6B`, `3panna_goal`, offset `0xA58D88`.

O reparse independente em `tools/analysis/audio_event_data_asset_hash_universe_control.py` confirma exatamente os caminhos, bytes e o hit. Como a ocorrência está em payload comprimido VP6 de filme, trata-se de evidência estrutural, não de áudio, ABKC, sample, evento ou playback; `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Os subgates do universo de dados estão `verified-structural`, `121/121`, mas os gates principais de assets/rendering/áudio/animação continuam não passados e readiness permanece `NOT_READY`.

Validação: matriz exit code `0`, Release `0/0`, self-test `92/92`, smokes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` amostras. ISO preservada somente para leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. XBE não executado nem recompilado.

Próxima etapa: continuar a busca por evidência discriminativa EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação, sem inventar bindings e sem concluir o projeto.

## Etapa 1.00 — controle independente do universo completo de assets de áudio — 2026-10-08

Foi implementado `fifa_pc/Runtime/AudioEventAudioAssetHashSearchAudit.cs` e integrado o comando `audio-event-audio-asset-hash-universe-audit` à matriz, ao self-test e ao `ProjectReadinessAudit`. O controle reparseia 17 candidatos do índice textual de `eventspa.evt` como palavras little-endian alinhadas e varre todos os assets em `data/audio`, excluindo somente o EVT-fonte: `60/60` assets válidos, `1.737.085.606` bytes, `0` assets com hits, `0` pares asset/hash e `0` ocorrências alinhadas. Relatórios: `fifa_pc/audio-event-audio-asset-hash-universe.tsv` e `fifa_pc/audio-event-audio-asset-hash-universe-summary.tsv`.

O subgate `audio-event-audio-asset-hash-universe-control` está `verified-structural-negative`, `60/60`. A evidência é estrutural e negativa: não seleciona ABKC/AST/BNK/sample, não estabelece evento de gameplay ou playback e não atribui semântica original. `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Readiness permanece `NOT_READY`: assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` e animação-deformação `structural-only`, ambos não passados.

Validação: matriz completa exit code `0`, build Release `0/0`, self-test `91/91`, smokes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. SHA-256: `AudioEventAudioAssetHashSearchAudit.cs` `9C0E3E6B34974BF2EB6BF04096CEE540934307A0788DF2AEFFC1FEFFCAF9FE05`, `Program.cs` `766DC833320707B891A75A559D337F2A2DF3BCD5270B1569C7C1DB3840B38529`, `SelfTestRunner.cs` `D13FA778813CCB60BB0122C0043F6B74954FE95348B526950867477EC089014F`, `ProjectReadinessAudit.cs` `5190DDECF723EBA9085FC8B4674414539F32C4630359E522727F3736E9B1DFF5`, matriz `D188A79CAA07E6299E5FCAA1268F6972B822ABE840128CB43D9A15C2632D6D90`, relatório `F5E9D9DF191173DAFEB5BC05D69FFA1075E18CF24CBAE194BACC85D730837002`, summary `DE86B53F51D842D1DD15799FC535CF4F1D2BD337D512786014850F9957C45DA2` e readiness `55DBF81BD71989DE4BD55F6249045C46EBD8C3BE6FD2DC01A8D47503F87C48AA`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar com evidência discriminativa para EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação; sem inventar bindings, executar ou recompilar o XBE.

## Etapa 0.99 — discriminação independente EVT→PCP — 2026-10-08

Foi implementado `tools/analysis/audio_event_evt_pcp_range_discrimination_control.py` e integrado à matriz e ao `ProjectReadinessAudit`. A reparse independente reconcilia `17/17` nomes do `eventspa.evt` com ranges únicos de `pcpahdr/pcpadat`, incluindo `17/17` âncoras de string, ranges de cabeçalho/dados, índices paralelos, prefixos `SCHl`, nomes, offsets e linhas de cabeçalho; o join de runtime também fecha `17/17`, com zero nomes ausentes. A busca raw de 32 bits continua negativa para ABK, `pcpahdr` e `pcpadat`.

O subgate `audio-event-evt-pcp-range-discrimination-control` ficou `verified-structural-negative`, `17/17`. A evidência é estrutural EVT→range PCP somente e não prova seleção EVT→ABKC→sample, evento de gameplay ou playback; `direct_selection_binding=not-proven` e `original_semantics=not-claimed`. Readiness segue `NOT_READY`: assets `candidate-integrated`, rendering `not-proven`, gameplay clean-room passado, áudio `partially-integrated` e animação-deformação `structural-only`, com os gates não principais ainda abertos.

Validação: matriz completa exit code `0`, build Release `0/0`, self-test `90/90`, smokes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. Hashes SHA-256: controle `6679AC5C2AA0118F8B54D5E51AFFDBEA4446310A75C891D75845FBB960667B0F`, `ProjectReadinessAudit.cs` `0F86927682D97D111B0FE7E74B2A977EA3F7944830D3BBED1E3ECA027A1F948A`, matriz `6375B68D2D377A4CB00D233D1DE6E52C5A6EF95B4E5139324C78E972AEB64E91`, relatório `FD6775F92E5A2FD1C3B065527CA1700EC943D67C299FBE6D4329DA8286E15BD2`, summary `9BD952F8207419933C68BE4BD53A2DC4006A549E637CEDF2947BCE7E7413CE86`, readiness `A270A70B1E9AB64775E5D856460E640F8A7C94EA4C5D1B8B3A08379E0CC516A5`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência discriminativa para EVT→ABKC→sample→playback, state→draw/UV/material ou timing/pose/rig/deformação; sem inventar bindings, executar ou recompilar o XBE.

## Etapa 0.98 — identidade independente do payload de geometria candidato — 2026-10-08

Foi adicionado `payload_sha256` ao audit da topologia original, sempre sobre o intervalo `__VB` validado, e implementado `tools/analysis/eagl_original_candidate_geometry_payload_identity_control.py`. A reparse independente cruza `38` grupos de geometria com cinco slots originais, associação de fetch e identidade raw: `38/38` grupos completos e `190/190` símbolos, metadados e hashes SHA-256 exatos. Status `verified-independent-original-candidate-geometry-payload-identity-control`, `original_semantics=not-claimed`.

O subgate `rendering-original-candidate-geometry-payload-identity-control` passou como `verified-structural-negative`, `38/38`. A conclusão é apenas identidade estrutural dos bytes; não há promoção para posição, UV, material, skinning, rigging, animação, morph, draw ou semântica GPU. Readiness permanece `NOT_READY`: assets `candidate-integrated` não passado, rendering `not-proven` não passado, gameplay clean-room passado, áudio `partially-integrated` não passado e animação-deformação `structural-only` não passado.

Validação: matriz completa exit code `0`, build Release `0/0`, self-test `90/90`, smokes, runtime headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos e `1554` samples. Hashes SHA-256: controle `122CD41BEA4F936FDA11A5B504794FA06AF61FDB94A61C7FCD6D5E50A9397830`, auditor de topologia `A519959A04EB908D011ACEE9A803609C39C08174123184603BF9525E8142A379`, `ProjectReadinessAudit.cs` `6CBDE0D1E624161EC5CF6D7B393A8DBCA1C8CC8DB34AF6872F14019F6E14259A`, matriz `FBE00A4CABEBDD8E3322A734095410B4244C1955AE3131C04C41F347EC4DE437`, relatório `C2674850F4CFA7D8A173D28BE87F848893CB9A99ADEFDBE8966E8324CCDC687A`, summary `49ED6FC5AADF625DDF20266083F2FADE7AAC28E8673C1D71166B8FE8D88C6CE1` e readiness `439467451E3F16ABA72F7CEDBB5FBC3CB4409D2C06EA4592E111C5D2593702FF`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: evidência discriminativa para state→draw/UV/material, EVT→ABKC→sample→playback ou timing/pose/rig/deformação; sem inventar bindings, executar ou recompilar o XBE.

## Etapa 0.97 — controle independente da discriminação do perfil estado→geometria — 2026-10-08

Foi integrado `tools/analysis/eagl_original_state_profile_discrimination_control.py`. O reparse independente dos relatórios de perfil e adjacência, deduplicando `(model_archive, object_name, state_layer, state_primitive)`, cobriu `269` linhas em `24` objetos, `38` candidatos de geometria e `180` primitivas de estado; `27` assinaturas distintas e `269/269` contagens de tie foram reproduzidas.

O resultado permanece negativo: `89/180` primitivas de estado têm múltiplos candidatos de geometria, enquanto `91/180` pertencem a objetos com uma única geometria e não discriminam; `0` têm zero candidatos; `15` grupos de assinatura têm múltiplos candidatos; `0` pares de command shape exatos e `0` bindings únicos estado→geometria foram provados. Status `verified-independent-original-state-profile-discrimination-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Relatórios: `fifa_pc/eagl-original-state-profile-discrimination-control.tsv` e summary.

O agregador registra `rendering-original-state-profile-discrimination-control` como `verified-structural-negative`, `24/24`, sem promover perfil de estado ou geometria única a state→draw, UV, material, comando ou GPU. O gate principal de rendering continua `not-proven`; assets seguem `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e o projeto `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` exit code `0`, smokes relevantes, headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes: script `10BF27EB1D15145BF046D3B9DA2E35F007E9B24B091DCB57D963FAD7E6A95436`, relatório `58C5C2F672F775F36EA1A5FC7D783664AF5D3AB9593DACFB0BC77DC93D3DE0C8`, summary `899DDE1BB50ED1E09F3559CB7CCAB901131F56CA52F1150D57AE817C959D8AFB`, matriz `740D370CF96A842C64F90EC55C7F3DF97BDE409E584A3FFEFBE6D06CCD250223`, `ProjectReadinessAudit.cs` `A91895863CBA11E793EBBFC1C7DE82EC1ED6B27351CBA6B60C0639B571015D08`, readiness `70A663983FAFEBB5EB12ED742A0F17A5CA8C8F4CACBAD2F036962012C59724B7`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência discriminativa para EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação, sem inventar bindings.

## Etapa 0.96 — controle independente da seleção do decoder de bancos de áudio — 2026-10-08

Foi integrado `tools/analysis/audio_bank_decoder_selection_control.py`. A reconciliação independente entre o decoder em lote e a varredura exaustiva ABKC/AST/BNK cobriu `16/16` bancos: `15/15` aceitos coincidiram em identidade, tipo e bytes de onda e `1/1` rejeição coincidiu na política esperada. Status `verified-independent-audio-bank-decoder-selection-control`, `direct_selection_binding=not-proven`, `original_semantics=not-claimed`.

O controle é estrutural e não atribui semântica original à seleção de EVT, sample, evento ou playback. O readiness registra o subgate `audio-bank-decoder-selection-control` como `verified-structural-negative`, `16/16`; o gate de áudio principal permanece `partially-integrated` e o projeto `NOT_READY`. Não houve execução ou recompilação do XBE e a ISO continua somente leitura.

Validação: Release `0/0`, self-test `90/90`, matriz `completed-authored-and-candidate-opt-in` exit code `0`, smokes, headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes: auditor `05E354332F47DBFF208E8F4889131A93736B6BCC184F6DDD99229F7DC968B903`, relatório `33E78F74BC593BB05C13DAFDFAA0598D3CB5D28AB4A880FC8E35697C344BA862`, summary `97EBA29D401FFA85DFEC948B31ACA166176A53E64D95441E8A8A279C4C3488B6`, matriz `0EBB914E3422067085AA3F4EB80517B774F1CE427E0048407C14EEB8FECF908E`, audit `011E2C23F5DE7360A78345F635ABF46F8A89BE4E5599B5B90DF50B81F796A2FA`, readiness `EF7275DE13DFFC595934E5BB3D95C66CF6ED3E3AD988F237737F6A53110F67EE`. ISO: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência discriminativa para EVT→ABKC→sample→playback, UV/material ou timing/pose/rig/deformação.

## Etapa 0.95 — controle independente da ambiguidade estado→material candidato — 2026-10-08

Foi integrado `tools/analysis/eagl_original_state_candidate_material_ambiguity_control.py`, que repara independentemente intervalos de estado originais, `SHAPENAME`, fontes candidatas de textura e cardinalidade de geometrias adjacentes. A cobertura foi de `24` objetos, `218` linhas de renderização, `62` linhas de ordem, `180` linhas de estado e `38` linhas de geometria.

Houve `36` intervalos de estado não vazios: `12` com dois candidatos adjacentes, `24` com um e `0` com zero. As validações foram `180/180` junções de estado, `180/180` matches de token, `180/180` fontes candidatas, `24/24` assets válidos e `24/24` controles completos. Bindings únicos estado→geometria e estado→material permaneceram `0`; adjacência e `SHAPENAME` não foram promovidos a draw, material, UV ou sampling.

Status `verified-independent-original-state-candidate-material-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. O agregador registra `rendering-original-state-candidate-material-ambiguity-control` como `verified-structural-negative`, `24/24`, sem alterar o gate principal `rendering=not-proven`. Assets seguem `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Hashes SHA-256: script `FDF5A04EF7EBA97DAF7E4D093AEE0E14602AC561090B4B2A67498DE2D9DDA9DB`, relatório `3A596D0D251D8B24995EFE167881F5D1EE2FEC80491AC010DFB9DA35EFA1F832`, summary `A4569B1BD91FA455FF233B0E1931D0B76C8262A5B3A51B1386EA094E85EDD289`, matriz `625910D5E524C3A82E9379E4CB12D26EA4F17CE243C103D892B059E288FAA147`, audit `1C316A8C86E7E111DC02D911ACA21C1DC7E81522BE8685257CFF0D5AF0F79A85`, readiness `4A747F9A8F5C0D8286F7CED075C0535A7FB978CBC9D32E7E781D70E37CB1FEB9`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar com evidência discriminativa de UV/material, EVT→ABKC→sample→playback ou timing/pose/rig/deformação, sem promover bindings não provados.

## Etapa 0.94 — controle independente da classificação original geometria/draw — 2026-10-08

Foi integrado `tools/analysis/eagl_original_geometry_draw_classification_control.py`, que junta independentemente o `eagl-original-render-chain.tsv` ao `eagl-candidate-fetch-association.tsv` por chave de primitiva. O controle cobriu `24/24` objetos, `218/218` linhas originais e `218/218` linhas candidatas, sem chaves faltantes, com `24/24` classificações de objeto completas.

A classificação estrutural coincidiu em `218/218`: `38` linhas de geometria e `180` linhas de estado em ambas as fontes. Os `24/24` objetos têm mais estados que geometrias e `state_to_geometry_unique_binding_proven_rows=0`. Status `verified-independent-original-geometry-draw-classification-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. O resultado não prova state-run→draw, UV, material, sampling, GPU, skinning, rigging, animação ou morph.

O readiness registra `rendering-original-geometry-draw-classification` como `verified-structural`, `218/218`, sem alterar o gate principal `rendering=not-proven`. Assets seguem `candidate-integrated`, áudio `partially-integrated`, animação-deformação `structural-only`, gameplay clean-room verificado e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Hashes: auditor `2A7EDF85FEA1C982429A448294A3B40140CB5C557452349054ABA0CD88D3B7CE`, relatório `4FD523C19F725825862F8DFCCF0AFA626CF60BB3790046FC5EE89D2FC1AF6B6E`, summary `69ECC533AB16C9573B8EB0B18EF6027279DAA3AB28BA3BBCB880D0D080D00C94`, matriz `950D951607B618C2CB59C5005E191E9A3B05FB47BF430CA4B9B2795280553639`, agregador `032C73872F08EAF7916CFD8571CD59C13061BBF66E0FEB0DFFC8C0BEA63E713B`, readiness `0DABB2380CF5EF99F304FE696F7426DDC012884049AB9A4398D64543990BFA85`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.93 — controle independente da ambiguidade pose-bank→animation-bank — 2026-10-08

Foi integrado `tools/analysis/animation_morph_pose_bank_ambiguity_control.py`, que reconcilia independentemente o índice de pose-bank, os perfis ELF de animation-bank e o audit de relocations de morph. O controle validou `391` linhas de pose-bank, `19/19` animation-banks ELF-válidos com metadados de símbolo e `393/393` layouts de morph parse-válidos; `159` layouts contêm pares de offsets, mas nenhum tem conjunto ou histograma de gaps exatamente correspondente a uma identidade de animation-bank.

O resultado não promove semântica: existe apenas `1` nome normalizado genérico (`PosePaletteBank`), não há correspondência de nome ou de metadado de símbolo com animation-bank e foram provados `0` bindings pose-bank→animation-bank únicos. Status `verified-independent-animation-pose-bank-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Não foi estabelecido frame, pose, rig, timing, skinning ou morph original. O agregador `project-readiness.tsv` registra `animation-morph-pose-bank-ambiguity-control` como `verified-structural-negative`, `391/391`, sem alterar o gate principal de animação-deformação.

Validação pós-etapa: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação. Auditor SHA-256 `6D06722268A77D919C93E698BBECE14A4D02E30337F3365D46004E8DCDE0297E`, relatório `007E51BF59267432C846B801256C0A356511EDEC5993AFB87362361454106A99`, summary `1CA4359A5B13236426FC93642B7B7F6D69D43A1D35ECB0EB738E2FFDED61CDA8`, matriz `1387E6D9723E3DFC2A04C0825698F640A95FE2A34F744D0F4602B962424F4638`, readiness `CB8DF38A2ECA8FFEF203BD488E60794B3DFB285321876B2E778F90ABCD3C42E7`. Readiness permanece `NOT_READY`; gameplay clean-room verificado, gates de assets/rendering/áudio/animação-deformação abertos. ISO somente leitura: `2013003776` bytes, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.92 — controle independente da ambiguidade estado→geometria — 2026-10-08

Foi integrado o controle `tools/analysis/eagl_original_state_geometry_binding_ambiguity_control.py`, que reconcilia os relatórios de adjacência, perfil, ordem e partição sem inventar semântica. São `24` objetos unidos, `269` perfis, `62` intervalos ordenados e `24` partições, sem identidades faltantes; perfil, ordem e conjuntos de índices de estado foram reconciliados em `24/24`. Todos os objetos têm mais estados que geometrias.

O controle não promove bindings: `geometry_group_state_exact=0`, `exact_command_shape=0` e binding estado→geometria único provado `0/24`. Status `verified-independent-original-state-geometry-binding-ambiguity-control`, `semantic_binding=not-proven`, `original_semantics=not-claimed`. Não foi estabelecido estado→draw, UV, material ou GPU original. Relatórios: `fifa_pc/eagl-original-state-geometry-binding-ambiguity-control.tsv` e summary. O agregador `project-readiness.tsv` registra o controle como `verified-structural-negative`, `24/24`, sem alterar o gate de rendering.

Validação pós-etapa: Release `0/0`, self-test `90/90`, matriz completa exit code `0`, smokes relevantes, runtimes headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54` eventos de áudio e `1554` samples. Readiness permanece `NOT_READY`; gameplay clean-room verificado, gates de assets/rendering/áudio/animação-deformação abertos. Auditor SHA-256 `4038AACD60D9D5210E96A9F0EA6DFAF1D5F1D4579683C480AED36BDE3F08E2EB`, agregador `E68D78D5411AAA9DEF72E1047D1F9D5FAB68E91A5E388258A93BF4129991FD6F`, relatório `B02761A085DBD5C7996BF40F149B097AA8AC130B77D4A8DE88621870A15AF0FF`, summary `7C60550132683EF27301AC355D40FF317235332F78F376328FDB31677B453206`, readiness `BB120BFB808DF9B0635DB320CCC120180E1E0C658BD24963C88756A74FE387B4`. ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.91 — controle de influência raster isolada para casos de composição — 2026-10-08

O `CandidateRenderInfluenceAudit` foi ampliado com comparação raster isolada do
objeto candidato para os casos em que composição, oclusão ou cobertura escondem
a diferença na cena completa. O resultado é candidate-only: não promove UV,
material, sampling, estado, draw ou GPU originais.

Resultado: `24/24` objetos com geometria finita/indexada e influência de textura
candidata no raster; `24/24` na comparação isolada e `20/24` na cena completa.
Checksums texturizados e sólidos distintos: `24/24`. Status
`verified-authored`, `original_semantics=not-claimed`. O subgate
`assets-candidate-attribute-raster-influence` permanece
`verified-candidate`, `passed=True`, `observed=24`, `expected=24`.

Readiness segue `NOT_READY`: assets, rendering, áudio e deformação continuam
abertos; gameplay clean-room permanece verificado. Validação: Release `0/0`,
self-test `90/90`, matriz completa exit code `0`, smokes, runtimes
headless/windowed e replays determinísticos; `10` cenários, `777` frames, `54`
eventos de áudio e `1554` samples de animação.

Hashes: relatório `EA487AC1B69906BEF0121B19588643814E118493B3A7ED8094DA134C065AFF14`,
auditor `4C1D43BF7157B782EE56290EA88C71428D37066A5CBA3D2494476E04BBCF12ED`,
`Program.cs` `C0C06AEC1147F9FA8AA699E649551FFD1A99CCC513576AE3F4FCA286AF21236F`,
readiness `D2704B5697D2B8EFBF2F2825CE1DC0A49E36089EED77227E7DD862A8CD3811A1`
e DLL Release `82DC30C3751939707D9B356D6BEDB3141A3B3DB3C40F5C2242D8BA0B7ACA25DA`.
ISO somente leitura: `2013003776` bytes, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.90 — auditoria dinâmica de influência do atributo candidato — 2026-10-08

`CandidateAttributeRasterInfluenceAudit` (`fifa_pc/Runtime`) e o comando
`eagl-candidate-attribute-raster-influence-audit` foram integrados à matriz.
Para cada objeto same-trunk, o caminho candidato compara o raster do mesh/textura
com baseline e com a transformação determinística do atributo opaco slot 1
`(x,y) -> (1-x,y)`.

Resultado: `24/24` geometria finita/indexada, `24/24` cardinalidade válida,
`24/24` perturbações alterando pixels, `24` checksums baseline distintos e
`24` checksums perturbados distintos. Status
`verified-candidate-attribute-raster-influence`; `original_semantics=not-claimed`.
Isto valida somente a influência no renderer candidato gerenciado. Não é uma
identificação de UV, material ou sampling, nem binding original de estado, draw
ou GPU. O subgate está `verified-candidate`, `passed=True`, `observed=24`,
`expected=24`; o readiness principal continua `NOT_READY`.

Hashes: relatório
`67F2B35CEA616716987E59442C25DF5C641D3A0CA47376E7892CC711EFA1DC47`, auditor
`08FC231178A1A7393442EC8A29884E9AADAFB2BCC5680B20B212E142C8620546`, readiness
`3D043101C88871F40E1EEE8B3B7707BC3311BE15174AB584FF570A4114449CF1`,
`Program.cs` `69ECDD321F8243981A32DBB2E9D7D3BF3CE9096656155BFEC9E9A07B78E00358`,
matriz `3F8CC844430215B34BF83C4ED8AA43AFEEBADA1CBD4CB504F49CC10937616D9C` e
DLL Release `2CCC224AB6715F6D79CEC8707DC8F3D6AA98611D7AFB744468E8723A5FF9B81D`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes relevantes, runtime headless/windowed e replays determinísticos; `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples. ISO somente
leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximo passo: evidência independente de estado→draw/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.89 — controle independente de compatibilidade estrutural atributo/textura candidata — 2026-10-08

Foi implementado `eagl_candidate_attribute_texture_compatibility_control.py`.
O join independente por objeto reparseia os perfis numéricos candidatos e a
cadeia same-trunk de textura. Nos `24/24` joins, inputs, dimensões positivas,
metadados `PAL8` e igualdade de pixels decodificados foram válidos. As faixas
opacas foram finitas/normalizadas em `24/24` rows e a cardinalidade de valores
coincidiu com os vértices em `24/24` rows.

Status: `verified-independent-candidate-attribute-texture-compatibility-control`.
É controle estrutural candidato, não binding: `semantic_binding=not-proven` e
`original_semantics=not-claimed`; slot 1 não foi identificado como UV e não
houve promoção de material, sampling, estado, draw ou GPU. O subgate foi
registrado como `verified-structural-negative`, `observed=24`, `expected=24`.
Assets, rendering, áudio e deformação originais continuam não fechados e o
readiness continua `NOT_READY`.

Hashes: relatório
`C49F4BB877646835CFA00720667503A64B2E9F64E5A3A5557AAE675D6BB8D3B9`, resumo
`750E4B79966EF5775BE0C631B99A523F26230D3EEA926EACB27C4DE45400C859`,
readiness `B16C9A047528917D2082971B9D4D671B2AA0DFB798D0916AC1D03A0F7C10A736`,
script `7B7912822CCFCAB4A74049AB5D6B569EFF84706931A35BC8EDA9D2356C6D185A`,
matrix `73A629EB52715005FD9EFCDD0B4B108943BF597438E2CFA8539C028BCF4B669F`,
auditor de readiness `F98DAD67C21F0CFCEB0071EA9E4F66DC37E69154F7F670DAD57F63CEBFB11E45`.

Validação: build Release `0/0`, self-test `90/90`, matriz completa com exit
code `0`, smokes, runtimes headless/windowed e replays determinísticos; `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação.
A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar evidência independente de estado→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.88 — controle estrutural independente de estado/geometria/bounds — 2026-10-08

Foi implementado `eagl_original_render_state_geometry_bounds_control.py` com
join independente, restrito aos `24` objetos candidatos, entre a partição de
estado/geometria e o controle de bounds/fetch. O resultado tem `24/24` objetos
particionados, `24/24` entradas de bounds válidas, `24/24` joins e `24/24`
rows completas; há `218` grupos de bounds, `216` completos e `2`
esperadamente incompletos. O slot `0` coincide em `218/218` grupos, slots
não-zero em `0` grupos e pares command-shape exatos em `0` rows.

Status: `verified-independent-original-render-state-geometry-bounds-structural-control`.
É evidência estrutural negativa, não binding: `semantic_binding=not-proven` e
`original_semantics=not-claimed`; não atribui estado a draw/geometria, posição,
UV, material, skinning, rigging, animação, morph ou GPU. O subgate foi
registrado como `verified-structural-negative`, `observed=24`, `expected=24`.
Assets, rendering, áudio e deformação originais continuam não fechados e o
readiness continua `NOT_READY`.

Hashes: relatório
`DFEBD93D163A551FA28905D9A05512E858F7172ECFAF285FAAEB03A35172ACDD`, resumo
`C0BC3EC18E8483589CFD37F8530CE5058063B424922EB1E670C9DA7261BAFCAA`,
readiness `E163BF21175BACA20B8D2CF8000443BFEAF0E37A64512F1C163BB5C91FBC2FB2`,
script `A078E08FC3B3A08B55CE4A1B73F3B74C13A6DD915B4D0DF1A68C9447CC4F4627`
e DLL Release `DD20BA2ADBFB50EFE5C783EDA7ED73E3BDDBEBDFE39861A68F70296B2F475893`.

Validação: build Release `0/0`, self-test `90/90`, matriz completa com exit
code `0`, smokes, runtimes headless/windowed e replays determinísticos; `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação.
A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar evidência independente de estado→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.87 — controle lexical independente de categoria/ordinal dos slots candidatos — 2026-10-08

Foi implementado `eagl_original_candidate_slot_category_control.py` com
reparse independente dos relatórios de fetch e identidade bruta. Foram
verificados `218` rows, `24` objetos, `216` primitivas completas, `2` rows
esperadamente não materializadas e `1080` slots. O formato dos símbolos foi
válido em `1080/1080` rows; categoria e ordinal foram coerentes em `1080/1080`
rows cada; não houve chaves faltantes.

Status: `verified-independent-original-candidate-slot-category-ordinal-control`.
É evidência lexical/estrutural, não binding: não atribui significado a slot,
categoria ou ordinal e não promove posição, geometria, UV, material, skinning,
rigging, animação, morph, draw ou GPU (`original_semantics=not-claimed`). O
subgate foi registrado como `verified-structural-negative`, com
`observed=218`, `expected=218`; assets continuam não promovidos, rendering
continua `not-proven`, áudio continua `partially-integrated` e readiness é
`NOT_READY`.

Hashes: relatório
`95ED83D5CDD52216EBEA25E3C660E4D4134A287EAAB91D7768CBEB1AEC5AAD24`, resumo
`EF7DEF8F1835E35E60BC4A25A8BF3C173C9290A028FDB5CC9133A4E42F8AF42F`,
readiness `9B82157CB82C3F53935E0561CA2CEB31852CA3E5A6EFD77E5FD982F7658CF145`
e DLL Release `DD55D70B0B1941433095F50B6B6626D48F28643491A4EB0756FCA08936D9D82E`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtime headless/windowed e replays determinísticos aplicáveis; `10`
cenários, `777` frames, `54` eventos de áudio e `1554` samples de animação.
ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar evidência independente de estado→geometria/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.86 — identidade bruta independente dos streams candidatos — 2026-10-08

Foi implementado `eagl-original-candidate-raw-stream-identity-audit` com
controle Python independente. A releitura somente leitura da ISO revalidou os
24 objetos EAGL e seus ranges `__VB`; foram observados `218` rows candidatos,
`216` primitivas completas, `1080` slots materializados, `1090` streams brutos,
`1080/1080` slots válidos, metadados exatos e hashes SHA-256 de payload, `216`
identidades completas, `2` rows esperadamente não materializadas e `0`
rejeições.

Statuses: `verified-independent-original-candidate-raw-stream-identity-control`
e `verified-structural-negative`. É apenas identidade estrutural bruta: não
define slot, draw, geometria, UV, material, skinning, rigging, animação, morph
ou GPU e mantém `original_semantics=not-claimed`. O subgate está aprovado com
`observed=216`, `expected=218`; assets continuam não promovidos, rendering
continua `not-proven`, áudio continua `partially-integrated` e readiness é
`NOT_READY`.

Validação: Release `0/0`, self-test `90/90`, matriz completa exit code `0`,
smokes, runtime headless/windowed e replays determinísticos aplicáveis; 10
cenários authored, 777 frames, 54 eventos de áudio e 1554 samples de animação.
ISO somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de render-state→draw, UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.85 — controle negativo de identidade bruta EVT/ABKC/AST — 2026-10-08

Foi implementado `audio-event-abk-raw-identity-audit` com controle Python
independente. A análise somente leitura da ISO revalidou `17` ranges EVT,
`16` bancos ABKC, `270` entradas `SoundEntries` não-dummy e `70/70` ranges AST
com `SCHl`. Os `17` hashes brutos EVT são distintos; houve `0` pares de hash
SHA-256 completo, `0` eventos com match, `0` pares de prefixo e `0` rejeições.

Statuses: `verified-bounded-audio-event-abk-raw-identity-negative-control` e
`verified-independent-audio-event-abk-raw-identity-control`. O controle não
seleciona amostra, não estabelece EVT→ABKC→sample→playback e não atribui
semântica original; `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`. O subgate novo é
`verified-structural-negative`; readiness continua `NOT_READY`, com áudio
original ainda `partially-integrated` e rendering original `not-proven`.

Validação: Release `0/0`, self-test `89/89`, matriz completa exit code `0`,
smokes, runtime headless/windowed e replays determinísticos aplicáveis. Foram
confirmados 10 cenários authored, 777 frames, 54 eventos de áudio e 1554
samples de animação. ISO somente leitura: `2013003776` bytes, `Archive`,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de estado→geometria/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.84 — controle independente de identidade command/payload — 2026-10-08

Foi implementado `eagl-original-primitive-stream-command-payload-identity-audit`
com uma reanálise dos objetos EAGL originais da ISO. Para cada grupo ordinal de
cinco streams, o controle gera uma identidade SHA-256 limitada a record,
descritor e cabeça/cauda do payload, e compara os pacotes brutos de command 9
por contenção exata, sem inventar binding. Os números reais foram `2364`
primitivas, `2362` fetches completos, `10240` candidatos, `10240` identidades
distintas, `0` duplicatas, `0` correspondências exatas e `2364/2364` ordinais
atuais com identidade única. O script independente Python reproduziu esses
contadores e `2364/2364` rows coerentes.

Statuses: `verified-bounded-original-primitive-stream-command-payload-identity-control`
e `verified-independent-original-primitive-stream-command-payload-identity-control`.
O resultado é um controle negativo de identidade bruta, não uma decodificação:
`original_semantics=not-claimed`; nenhuma semântica de geometria, UV, material,
normal, peso, skinning, rigging, animação, morph, comando ou GPU foi atribuída.
O subgate foi registrado como `verified-structural-negative`; rendering ainda é
`not-proven`, assets continuam não promovidos e readiness permanece `NOT_READY`.

Validação: matriz completa com exit code `0`, build Release `0/0`, self-test
`88/88`, smokes, runtime headless/windowed e replays determinísticos. ISO
preservada somente para leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar prova independente para estado→geometria/UV/material,
EVT→ABKC→sample→playback e timing/pose/rig/deformação.

## Etapa 0.83 — controle independente de slot 1/categoria numérica 11 — 2026-10-08

Foi adicionado `eagl-original-candidate-slot-profile-audit`, uma reanálise
independente dos relatórios `eagl-original-vertex-stream-numeric.tsv` e
`eagl-candidate-fetch-association.tsv`. A chave usa arquivo, objeto e símbolo
do stream, pois o símbolo isolado se repete entre objetos.

Na ISO, `216/216` primitivas candidatas completas possuem registro de slot 1
que reconcilia com o stream original do mesmo objeto: símbolo, stride,
byte-length e element-count coincidem; o stream original também apresenta
categoria numérica `11` e perfil float2 finito/próximo de unidade. O controle
reproduziu `11820` rows originais, `218` primitivas candidatas e `216`
primitivas candidatas completas. O resultado é
`verified-independent-original-candidate-slot1-category11-profile-control`.

Essa é uma associação numérica estrutural: slot 1 e categoria 11 permanecem
labels opacos. Não foram chamados de UV, material, normal, peso, skinning,
rigging, animação, morph ou comando GPU; `original_semantics=not-claimed`.
O readiness continua `NOT_READY` e os gates principais de assets, rendering,
áudio e animação/deformação original continuam abertos.

Validação completa: matriz authored/candidate opt-in com exit code `0`, build
Release `0/0`, self-test `87/87`, smokes relevantes, runtimes headless e
windowed e replays determinísticos passaram. A ISO não foi escrita:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de estado de renderização para
geometria e interpretação UV/material, além de timing/pose/rig/deformação e
do caminho EVT→ABKC→sample→playback. Nenhuma dessas semânticas está sendo
promovida por este controle.

## Etapa 0.82 — controle posicional de campos `.off` para words CSI — 2026-10-08

Foi adicionado `fifast2-csi-opaque-field-audit`, que compara cada um dos `100`
words opacos de `FIFAST2.csi` com as três colunas numéricas e com o ordinal de
linha das tabelas completas `pcpahdr.off` e `pcpadat.off`. Os nomes de coluna
são apenas posicionais; não foram interpretados como offset, tamanho,
identificador, header, data, sample ou playback.

Na ISO, `pcpahdr` teve `1/100` registros com alguma igualdade de coluna
(`column1=0`, `column2=1`, `column3=0`), e `pcpadat` teve `0/100`. O ordinal
de linha coincidiu em `3/100` em cada tabela. Esses poucos matches não são
discriminativos contra tabelas de `283` rows e, portanto, não abrem um
binding: `opaque_word_semantics=not-decoded`, `direct_selection_binding=
not-proven` e `original_semantics=not-claimed`.

Validação completa: matriz authored/candidate opt-in concluída com exit code
`0`, build Release `0/0`, self-test `86/86`, smokes, runtime headless e
windowed, replays determinísticos e readiness ainda `NOT_READY`. A ISO não
foi escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: priorizar evidência independente do caminho original de
renderização/UV/material e de timing/pose/rig/deformação; o caminho
EVT→ABKC/sample/playback também continua sem binding comprovado.

## Etapa 0.81 — controle de ranges e boundaries dos words opacos CSI — 2026-10-08

O comando `fifast2-csi-opaque-range-audit` compara os `100` words opacos do
CSI apenas com as tabelas `.off` de `pcpahdr` e `pcpadat`, usando os
comprimentos reais dos companions. As duas tabelas têm `283` rows, são válidas
e particionam integralmente seus assets (`pcpahdr.big=35572` bytes;
`pcpadat.big=601688832` bytes).

Os `100/100` words caem em algum range de `pcpahdr` e `pcpadat`, mas nenhum
coincide com início ou fim de range: `pcpahdr exact_start/end=0/0` e
`pcpadat exact_start/end=0/0`. O containment de `pcpahdr` atravessa `80` rows
distintas; em `pcpadat`, todos caem na primeira row. Como as tabelas cobrem os
assets inteiros, containment isolado não é binding de header, data, subcampo,
sample ou playback.

O resultado é `verified-structural-csi-opaque-range-boundary-control`, com
`opaque_word_semantics=not-decoded`, `direct_selection_binding=not-proven` e
`original_semantics=not-claimed`. O gate principal `audio` continua parcial;
rendering, UV/material e animação/deformação continuam sem prova final.

Validação: build Release `0/0`, self-test `85/85`, smoke real do controle e
readiness ainda `NOT_READY`. A ISO permaneceu sem escrita: `2013003776` bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência de subcampos/identificadores que discrimine
os words CSI além de ranges completos; se não houver sinal independente,
mantê-los como semântica não decodificada e priorizar os gates de renderização
e deformação.

## Etapa 0.80 — namespace de hashes `tunavars.txt` cruzado com `FIFAST2.csi` — 2026-10-08

O comando `fifast2-csi-tuna-audit` agora cruza, em leitura somente, o arquivo
`data/audio/tuna.big`, sua entrada `tunavars.txt`, `FIFAST2.csi` e o tamanho de
`pcpahdr.big`. O archive é `BIGF`, tem `6313` bytes, uma entrada
`tunavars.txt` e `106` rows nomeadas. As `106/106` rows reproduzem o candidato
de hash observado estaticamente (`seed=0x1505`, `multiply=0x21`, bytes ASCII).

O CSI mantém `100/100` registros válidos; `89` nomes coincidem exatamente com
`tunavars.txt`, `11` nomes CSI não aparecem na tabela e `17` nomes da tabela não
aparecem no CSI. Nenhum dos `89` words opacos coincide com o hash da tuna.
Embora `100/100` words estejam numericamente dentro do comprimento de
`pcpahdr.big`, isso é apenas evidência de faixa; o `.off` de `pcpahdr` cobre o
arquivo inteiro e nenhum binding de registro, subcampo, sample ou playback foi
demonstrado.

Portanto o resultado é `verified-structural-tuna-csi-name-hash-crosscheck`, com
`direct_selection_binding=not-proven`, `opaque_word_semantics=not-decoded` e
`original_semantics=not-claimed`. Não prova EVT→ABKC/amostra nem reprodução
original. O gate principal `audio` continua `partially-integrated`; os gates de
rendering e deformação também continuam sem prova final.

Validação desta etapa: build Release `0` warnings/`0` errors, self-test
`84/84`, auditoria real na ISO, readiness `NOT_READY`. A ISO não foi escrita:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar associação independente entre os words opacos CSI e
registros/subcampos reais de `pcpahdr`/`pcpadat`, mantendo-os como candidatos
até haver discriminação verificável; em paralelo continuam abertas a
renderização original, atributos UV/material e timing/pose/rig/skinning.

## Etapa 0.79 — índice estrutural candidato de FIFAST2.csi — 2026-10-08

`FIFAST2.csi` foi auditado em modo somente leitura como um candidato
estrutural de índice de nomes `MOIR`: `100/100` registros válidos, `100` nomes
únicos, offsets de nome a partir de `0x5F4`, word reservado zero em `100/100`,
`12` words opacos dentro do asset e `98` pares adjacentes não-decrescentes.
O cruzamento com `17` nomes textuais EVT teve `0` matches exatos e `22` por
substring.

Não foi atribuído significado aos words opacos, nem binding para seleção,
trigger, evento, sample, ABKC ou playback: `direct_selection_binding=not-proven`
e `original_semantics=not-claimed`. O novo smoke passou junto da matriz
completa (`completed-authored-and-candidate-opt-in`), Release `0/0`, self-test
`83/83`, Python `57/57`, runtime headless e replays determinísticos.

ISO sem escrita: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Rendering original, seleção EVT→amostra e animação/deformação continuam sem
gate comprovado.

## Etapa 0.78 — layout de canal candidato único em streams AST — 2026-10-08

Para um header SCHl observado com `0` canais, o decoder agora valida os
layouts candidatos de `1` e `2` canais sem modificar o header e aceita somente
quando há exatamente um layout que percorre todos os blocos. A row fica
explicitamente marcada como `decoded-payload-channel-layout-candidate`.

`GBREAK.abk` sound `4` e `GBREAKLC.abk` sound `6`, antes rejeitados, aceitaram
unicamente `1` canal. O exhaustive bounded/all passou a `241/241` candidatos
de banco decodificados; eventos `17/17`, rows comparadas `259/259`, matches
PCM exatos `0`. `CONTROL.abk` continua sem candidato.

É validação de payload candidato, não seleção EVT→amostra ou playback
original. `direct_selection_binding=not-proven`, `original_semantics=not-claimed`
e `audio=partially-integrated` permanecem; rendering segue `not-proven` e
animation/deformation `structural-only`.

Validação: Release `0/0`, self-test `82/82`, smokes bounded/all, controle de
política e síntese negativa independentes passaram; a matriz completa 0.78
terminou com `matrix_status=completed-authored-and-candidate-opt-in`, incluindo
runtime headless e replay determinístico. ISO sem escrita:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.77 — enumeração ABKC por entradas não-dummy observadas — 2026-10-08

`AudioBankAsset` passou a conservar as entradas de som ABKC não-dummy
observadas pelo parser. O batch decoder e a correlação de payload usam essa
lista para enumerar somente índices RAM com `SoundType=0`; o fallback por
contador permanece somente para fixtures sem a lista observada.

Resultado bounded/all: `17/17` eventos nomeados, `16` bancos, `241` candidatos
ABKC não-dummy, `239` decodificados; o controle independente comparou
`259/259` rows (`17` eventos + `242` rows de banco) com todos os campos de
identidade, status, decode, bytes WAV, hash PCM e contador exato iguais, e
`0` pares PCM exatos. `CONTROL.abk` não expõe candidato; `GBREAK.abk` e
`GBREAKLC.abk` mantêm um stream AST rejeitado por `0 channels`. Esses casos
continuam sem inferência de silêncio ou de seleção original.

É evidência estrutural do conjunto de candidatos e do decoder gerenciado,
não seleção EVT→amostra nem playback original. `direct_selection_binding` é
`not-proven`, `original_semantics=not-claimed` e o gate `audio` continua
`partially-integrated`; `rendering=not-proven` e
`animation-deformation=structural-only` permanecem.

Validação: Release `0/0`, self-test `82/82`, Python `57/57`, matriz completa
0.77, smokes relevantes, runtime headless e replay determinístico. ISO sem
escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.76 — join original↔candidate dos grupos de geometria por stream — 2026-10-08

`eagl-candidate-geometry-stream-audit` validou `24/24` objetos e `38/38`
grupos candidate com primitiva correspondente, escolha de fonte candidate no
slot `0`, cinco streams completos e geometria finita/indexada. O controle
independente `eagl_original_candidate_geometry_stream_control.py` juntou os
grupos às `2364` chaves originais de slot `0`: `38/38` coincidiram em símbolo
`__VB`, stride, byte-length, element-count, número de vértices e cardinalidade
do strip; `2326` chaves originais permanecem sem grupo candidate.

É controle de identidade/metadata estrutural do caminho candidate. A escolha do
slot `0` não é promovida a posição, UV, material, normal, peso, skinning,
rigging, animação, morph, draw ou GPU, e `original_semantics=not-claimed`.
O subgate `rendering-original-candidate-geometry-stream-control` está
`verified-structural-negative` `38/38`; os gates principais permanecem
`assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `57/57`, matriz completa
0.76 com smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de atributos/UV/material,
deformação e EVT→amostra; metadata de stream não basta para fechar rendering.

## Etapa 0.75 — join original↔candidate de registros `__VB` por slot — 2026-10-08

`eagl_original_candidate_stream_record_control.py` reparseou a topologia
original (`11820` registros, `2364` primitivas) e os `218` primitivas candidate.
Nos `216` layouts candidate completos, `1080/1080` slots materializados
coincidiram em nome `__VB`, valor auxiliar, fetch/stream stride, byte-length e
element-count; `1080/1080` records foram exatos. Os dois layouts incompletos
foram mantidos como records não materializados esperados. O relatório também
preserva `2146` chaves originais sem candidate, sem tratar ausência como match.

O subgate `rendering-original-candidate-stream-record-control` está
`verified-structural-negative` `1080/1080`. Isso não atribui qualquer slot a
posição, UV, material, skinning, rigging, animação, morph, draw ou GPU, e
`original_semantics=not-claimed`. Os gates continuam
`assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `56/56`, matriz completa
0.75 com smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar decodificação independente dos campos de atributo/UV/
material, deformação e EVT→amostra; a correspondência de records não será
promovida sozinha a semântica.

## Etapa 0.74 — join original↔candidate de comando/fetch por primitiva — 2026-10-08

`eagl_original_candidate_primitive_fetch_control.py` reparseou e juntou por
chave `(model_archive, model_object, layer, primitive)` o render-chain original
e o relatório candidate de fetch. Das `218` primitivas candidate, `218/218`
coincidiram em contagem de comandos, completude, contagem de fetch,
completude de cinco slots, texto de estado e flag de tipo; `216/218` também
tiveram sequência de IDs/records candidate materializada. Os dois restantes
coincidem com os dois layouts incompletos e foram preservados como records não
materializados, não como inferência. O join não encontrou chave candidate sem
original (`0`); `2146` primitivas originais ficaram sem candidate por limitação
de cobertura.

O subgate `rendering-original-candidate-primitive-fetch-control` está
`verified-structural-negative` `218/218`. Isso não atribui slots ou comandos a
posição, UV, material, skinning, rigging, animação, morph, draw, GPU ou
semântica original: `original_semantics=not-claimed`. Os gates continuam
`assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `55/55`, matriz completa
0.74 com smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente de primitiva→vértice/atributo/
UV/material, deformação original e seleção EVT→amostra; lacunas sem prova
continuam não comprovadas.

## Etapa 0.73 — controle de sequência de índices SHAPENAME — 2026-10-08

O comando candidate `eagl-candidate-shape-state-primitive-audit` expôs a
sequência de `(LayerIndex, PrimitiveIndex)` das primitivas com `SHAPENAME`.
`eagl_original_candidate_shapename_primitive_index_control.py` juntou-a ao
relatório original por objeto: `24/24` sequências, `24/24` conjuntos e
`24/24` contagens coincidiram; `24/24` streams candidate estavam completos.

O resultado é `verified-structural-negative` no subgate
`rendering-original-candidate-shapename-primitive-index-control`. Não é
decodificação de primitiva→vértice, UV, material, skinning, rigging, animação,
morph, comando GPU ou semântica original. Os gates principais continuam
`assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `54/54`, matriz completa
0.73, smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência independente de associação com dados de
vértice/atributos/UV/material, deformação de animação e seleção EVT→amostra.

## Etapa 0.72 — controle da população original de shape-state — 2026-10-08

`eagl_original_state_candidate_primitive_shape_control.py` juntou os mesmos
`24` objetos por chave independente e comparou a população de primitivas de
estado original com os shape-state candidates. Resultado: `24/24` contagens de
primitivas iguais, `24/24` contagens de tokens `SHAPENAME` iguais, `24/24`
streams shape-state candidatos completos e, no mesmo join, `24/24` same-trunk,
cadeias de origem válidas, entradas SHPX exatas e hashes de pixels coincidentes.

O subgate é `verified-structural-negative` `24/24`. A contagem não decodifica
primitiva→vértice, UV, material, skinning, rigging, animação, morph, comando
GPU ou semântica original. Os gates principais permanecem
`assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `53/53`, matriz completa
0.72, smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar a regra decodificada para associação de primitiva,
atributos/UV/material, deformação de animação e seleção EVT→amostra, sem
promover candidatos.

## Etapa 0.71 — controle independente SHAPENAME↔asset candidato — 2026-10-08

`eagl_original_shapename_candidate_asset_control.py` juntou os relatórios de
estado/geometria e proveniência candidata por objeto. O controle obteve
`24/24` rows unidos, `24/24` tokens `SHAPENAME` compatíveis, `24/24` objetos no
mesmo tronco, `24/24` cadeias de origem válidas, `24/24` entradas SHPX exatas e
`24/24` hashes de pixels decodificados coincidentes.

O resultado é `verified-structural-negative` no readiness para o controle de
identidade lexical/estrutural de asset. Não atribui UV, material, vértices,
skinning, rigging, animação, morph, comando GPU ou binding original. Portanto,
`assets=candidate-integrated`, `rendering=not-proven`,
`audio=partially-integrated` e `animation-deformation=structural-only`
continuam sem promoção.

Validação: Release `0/0`, self-test `82/82`, Python `52/52`, matriz completa
0.71, smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência independente para estado de renderização/
UV/material, deformação de animação e seleção EVT→amostra, sem atribuir
semântica a candidatos.

## Etapa 0.70 — síntese negativa da seleção EVT→amostra — 2026-10-08

`audio_event_selection_negative_synthesis.py` revalidou cinco fontes
independentes: índice EVT (`17` words; zero hits em ABK/PCPAHDR/PCPADAT), busca
no XBE (zero hits alinhados/não alinhados), correlação payload/PCM (`17/17`
eventos decodificados, `16` bancos, `368` candidatos, `239` decodificados,
`0` pares exatos), join de faixas nomeadas (`17/17`) e rotas authored (`15/15`).
O readiness registra o controle `verified-structural-negative` `5/5`.

O resultado é uma delimitação negativa, não seleção original nem decoder.
`direct_selection_binding=not-proven` e `original_semantics=not-claimed` foram
preservados; `audio` continua `partially-integrated`, rendering original
`not-proven` e animação/deformação `structural-only`.

Validação: Release `0/0`, self-test `82/82`, Python `51/51`, matriz completa
0.70, smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar bindings originais de renderização/animação e a
seleção EVT→amostra sem atribuir semântica a candidatos.

## Etapa 0.69 — controle independente das rotas authored de áudio — 2026-10-08

`audio_authored_candidate_route_control.py` reparseou os traces authored e
reconstruiu `15/15` rotas ação→candidato: `10/10` cenários, `54/54` eventos
tocados, `8` candidatos únicos e `15/15` rotas presentes no runtime nomeado,
preparadas e com header WAV válido. O catálogo nomeado completo continua com
`17` candidatos; nenhum candidato usado ficou ausente.

É validação independente do runtime clean-room e dos replays, não uma
decodificação de EVT→ABKC/sample, gameplay original ou playback original.
Mantidos `original_semantics=not-claimed`,
`audio-authored-candidate-route-control=verified-authored` 15/15 e
`audio=partially-integrated`.

Validação: Release `0/0`, self-test `82/82`, Python `50/50`, matriz completa
0.69, smokes, runtime headless/windowed e replays determinísticos. ISO sem
escrita: 2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar seleção EVT→amostra e os bindings originais de
renderização/animação sem inventar semântica.

## Etapa 0.68 — join independente estado original com atributo/influência candidata — 2026-10-08

O controle `eagl_original_state_candidate_attribute_control.py` juntou por chave
de objeto `24/24` candidatos aos estados originais correspondentes. Foram
validadas `218/218` linhas de estado e todas mantiveram zero tokens literais de
textura/material/UV/atributo/deformação. Os atributos candidatos `slot1` foram
completos, finitos e near-unit em `24/24`; a geometria foi finita/indexada em
`24/24`, a textura influenciou o raster em `20/24` e
`mesh_shape_state_exact=0/24`.

O resultado é estrutural e negativo: não é binding original de estado,
geometria, UV ou material, não prova ausência semântica de comandos opacos e
mantém `original_semantics=not-claimed`. Readiness registra
`rendering-original-state-candidate-attribute-control=verified-structural-negative`
24/24; `rendering=not-proven` continua.

Validação: Release `0/0`, self-test `82/82`, Python `49/49`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. ISO sem escrita:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: continuar seleção EVT→amostra e decoders/associações de
renderização e animação sem inventar bindings.

## Etapa 0.67 — join independente EVT paralelo com payload runtime — 2026-10-08

O controle independente juntou `17/17` eventos nomeados do relatório paralelo
PCPAHDR/PCPADAT ao payload nomeado gerenciado. Header row, offset e comprimento
coincidiram em `17/17`; os `17/17` metadados foram validados, preparados e
convertidos em headers WAV válidos.

Isso é proveniência/range do caminho candidato, não seleção de ABKC/sample,
ligação a gameplay ou playback original. Readiness:
`audio-event-parallel-runtime-join-control=verified-structural-negative` 17/17;
`audio=partially-integrated` permanece e `original_semantics=not-claimed`.

Validação: Release 0/0, self-test `82/82`, Python `48/48`, matriz, smokes,
runtime headless/windowed e replays determinísticos aprovados. ISO preservada:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `DB82C0D3C592EF8158829CB824F2812F769A4C9165D3F617F4B2D38B9F5F57E4`;
controle `5B15C5A54916670E1CB9BBEB573ECC9CF0F04F90D00157EC66337495BFC6BD21`;
summary `8F5C982EA17AC3F50478029C37A1F52CAD858D18AE5C6192DCE8A829C8EEB0FF`;
readiness `77FE7E7D7EC8E1AABA325E3C21583AF8F86AA312BCB0A31DF5D755F762BC9A6A`;
log `4737F1A78814A8CBF248406187F430B6915FBF97C8FE3C5C6BC7CB5E55209C37`.

Próxima etapa: buscar seleção EVT→amostra e associação independente de
estado/UV/material, mantendo a separação entre evidência e semântica original.

## Etapa 0.66 — join independente de streams numéricos com fetch slots opacos — 2026-10-08

O controle independente validou `11820/11820` joins entre streams numéricos e
topologia, `2364/2364` grupos completos de cinco registros e fetch slot raw `0`
no primeiro registro de cada grupo. O primeiro raw tem stride compatível em
`2364/2364`, match de bounds em `2363/2364` e nenhum match posterior; os dois
casos incompletos de slots permanecem explícitos.

É evidência estrutural negativa, não binding semântico. Não foram atribuídos
posição, geometria, UV, material, skinning, rigging, animação, morph ou GPU;
`semantic_binding=not-proven` e `original_semantics=not-claimed`. Readiness:
`rendering-original-bounds-fetch-slot-join-control=verified-structural-negative`
2364/2364; `rendering=not-proven` permanece.

Validação: Release 0/0, self-test `82/82`, Python `47/47`, matriz, smokes,
runtime headless/windowed e replays determinísticos aprovados. ISO preservada:
2013003776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `51B2B70A129421A5F05BA04FA2331109A439FF5D3EAAD2CEF9C19E351E00A836`;
controle `E8BE8F200ED47B6423EF43244BB41BED8CFC76FC38C343B108BFA5C9B4996187`;
summary `04681A4777F27672E69A22B1A0194277F30D806A7BF7F7B09B9B19E80887933D`;
readiness `FB761D99DF155D83BFC371D1AADC9F77DCE0E05C4F0E91994BE47F41DC1C0B28`;
log `6EEBD42AE2CBCA515F9EFA1A869EC969484B18FC35D6E2D8646DC539F388DDE5`.

Próxima etapa: buscar associação independente estado/UV/material e seleção
EVT→amostra, mantendo a separação entre prova candidata e semântica original.

## Etapa 0.65 — correspondência independente de bounds no primeiro registro raw — 2026-10-08

O audit por stream e o controle independente agruparam `11820/11820` streams
em `2364/2364` grupos objeto/ordinal completos. Todas as sequências têm cinco
registros e categorias contínuas; o primeiro registro raw tem stride 12 em
`2364/2364`, corresponde aos bounds em `2363/2364`, e nenhum registro posterior
corresponde. As famílias raw observadas são `category_key=10` em 2010 grupos e
`category_key=11` em 354.

É evidência estrutural/candidata, não binding de posição ou geometria. Nenhuma
semântica de posição, UV, material, skinning, rigging, animação, morph ou GPU
foi atribuída; `semantic_binding=not-proven` e `original_semantics=not-claimed`.
Readiness: `rendering-original-bounds-consistent-record-control=verified-structural-negative` 2364/2364; o gate original continua `not-proven`.

Validação: Release 0/0, self-test `82/82`, Python `46/46`, matriz, smokes,
runtime headless/windowed e replays aprovados. ISO preservada: 2013003776
bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL `C17C7DE0FC2E6960F69304A5A2841C06DD6C1EB280B674DF344C0542DE00D661`;
stream report `A6DA833F84E07FFCF8388A215B6618406383C61854B40CCE0A8A33498141FF12`;
controle `2F9056749FEAABA25A0C38A6EB10F19AECCE5F23F0961757968D609BFF0F0551`;
summary `C59DA46CC903A5F3E23D95AA628E9EBCA19880766458796C2668682E2530A072`;
readiness `3B21349B7153257FB04C6AFE19C6511BCF47BC8B4756E4E290B2EC4EE8E0D9A4`;
log `5672C8714ABBFF8053007DAFD0E42B576E06C14E95F8D0E4D54AC7E04DB5ADE3`.

Próxima etapa: buscar associação independente estado/UV/material e seleção
EVT→amostra, mantendo a separação entre prova candidata e semântica original.

## Etapa 0.64 — cardinalidade independente dos matches numéricos — 2026-10-08

O controle `eagl_original_numeric_stream_ambiguity_control.py` reparseou o
relatório dos payloads `__VB` e classificou os matches de `float3` com os bounds
declarados apenas no nível de objeto. Foram validadas `883/883` linhas: `494`
com um match, `388` com múltiplos e `1` sem match; o histograma completo ficou
`0=1,1=494,4=196,5=160,6=1,7=4,8=9,9=7,10=8,11=1,12=1,13=1`.

É evidência estrutural negativa de ambiguidade, não binding de posição ou
geometria. Nenhum stream/slot foi promovido a posição, UV, material, skinning,
rigging, animação, morph ou GPU; `semantic_binding=not-proven` e
`original_semantics=not-claimed`. Readiness: `rendering-original-numeric-stream-ambiguity-control=verified-structural-negative` 883/883; o gate original de renderização continua `not-proven`.

Validação: Release 0/0, self-test `82/82`, Python `45/45`, matriz, smokes,
runtime headless/windowed e replays determinísticos aprovados. ISO preservada: 2013003776 bytes,
`Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL Release `CF863DA6CE14326060C7E4A4CBA8FE9E5FC4347C504C683A5422B980E30BF986`;
relatório `C637C4D1EF8DE77D2544140CB7229410DE83EA40D8604F70A8FD59148419CC82`;
summary `6F137E8CC1681F8B5F3AE1E0EA4E31BA62DEF4F87A640095183D6CDB16A075ED`;
readiness `FF5AA4D98EA0CFE74FF516B151598842BE2E09F64DEFDEC417468A6CB149E7AD`;
log `F15BC9C41A4AEBB470EA29FE5D5064588A8F1D3F36B01C8CD3B13F02D84C2016`.

Próxima etapa: buscar seleção EVT→amostra e decoders de renderização/animação,
sem promover controles negativos a semântica original.

## Etapa 0.63 — controle independente do caminho textual do asset EVT — 2026-10-08

Foi adicionado `audio_event_asset_path_index_control.py`. Ele cruza o relatório
textual de literais de asset do XBE com o relatório independente do índice de
`eventspa.evt`. Foram encontrados 4 formas textuais do mesmo basename, 9
ocorrências no total, todas convergindo para um único caminho ISO
`data/audio/eventspa.evt`; o índice conserva 17 nomes e 1.061 pares candidatos.
Nenhuma das 4 linhas tem xref executável direto (`xbe_literal_xref_count=0`).

Isso é somente proveniência textual e controle negativo: não recupera loader,
função ou ID, não seleciona ABKC/AST/BNK e não liga evento a gameplay ou
playback. O readiness registra
`audio-event-asset-path-index-control=verified-structural-negative` 4/4;
`direct_selection_binding=not-proven` e `original_semantics=not-claimed`.
O gate principal de áudio continua `partially-integrated`, e a ISO segue sem
escrita.

Validação da etapa: build Release 0/0, self-test 82/82, teste Python do
controle aprovado, matriz completa exit_code=0 com smokes de áudio/renderização,
runtime headless/windowed e replays determinísticos. A matriz terminou com
`matrix_status=completed-authored-and-candidate-opt-in`; ISO 2013003776 bytes,
atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL Release `E538D9DAC739B39952BD1F0D604A050263EF1F8D54EB88828CB49D632D4411C2`;
relatório `5118A4CC14D83879FFBD4EF2436CF8529A65F361E67E9AE9A70EC3882DBE03B5`;
summary `BF0F1E664FB14A507C3A11718534273935065F4B46A5E4E522D610A8F1941E56`;
readiness `171A0683581456334BEBCB751E8100E08632D7B7316786BF0C2A222B0688CE8B`;
log `0B7B3DD1380A35AC5B6F63ED298AD534DFFF7A8CC6A3A554A41F901279751988`.

Próxima etapa: continuar a busca por seleção EVT→amostra e os decoders de
renderização/animação, sem promover controles textuais a semântica original.

## Etapa 0.62 — controle independente das políticas de payload de áudio — 2026-10-08

Foi adicionado `audio_event_payload_policy_control.py`, que compara
independentemente os relatórios EVT/ABKC gerados com limite de 512 candidatos
por bank e com limite de 100.000 candidatos. Os dois relatórios coincidiram em
386/386 linhas: 17 eventos e 369 candidatos de bank, com identidade, status de
decode, bytes de onda, hashes PCM e contadores de match iguais. Houve 0 pares
de hash PCM exato.

Isso é um controle de cobertura/reprodutibilidade, não seleção original. Não
liga EVT a ABKC/AST/BNK, gameplay ou playback e mantém
`original_semantics=not-claimed`. O readiness registra
`audio-event-payload-policy-control=verified-structural-negative` 386/386; o
gate principal de áudio permanece `partially-integrated`.

Validação: build Release 0/0, self-test 82/82, matriz completa com exit_code=0,
smokes de áudio/renderização, runtime headless/windowed e replays
determinísticos. A ISO permaneceu sem escrita, com 2013003776 bytes, atributo
`Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL Release: `599EB85BD0AC52B328B6831A0F7B75464C0E673E9512395B7165647C64F22C26`;
relatório `F3D87BDCCD0F27B584C3B48BCB7B98AFDCB941AE57647DC37893621AEE1B1D94`;
summary `C0CEB3E9B359ACE60D9D126BCCE56427BFA034E5822FC3FDD7EE45903D8F9297`;
readiness `1FFB1B5187F5DF3AB307AEB78E20D2DE616CC8A83986D73CA5281E101DC0E49D`;
log da matriz `5878DB73ABA59EDAF70B5C5F9300DE82CDBB5D706A1F788A2422B7E44CD230BD`.

Próxima etapa: obter evidência independente de seleção EVT→amostra e avançar
os decoders de renderização/animação sem inventar bindings.

## Etapa 0.61 — join independente estado/geometria ↔ payload original — 2026-10-07

Foi adicionado o controle Python `eagl_original_state_geometry_payload_join_control.py`.
Ele cruza os 24 objetos `tops*.big` da partição estado/geometria com o
relatório independente de objetos render/payload original. O join encontrou
24/24 objetos, 0 ausências, 24/24 matches de identidade e 24/24 linhas com
contadores estruturais válidos. A soma geometria+estado bate com as primitivas
de renderização em 24/24; comandos completos e fetches batem em 24/24; há
cinco streams de payload por primitiva em 24/24, com ranges distintos e 0
sobreposições.

Isso confirma apenas consistência de chaves e contadores entre relatórios
independentes. Não atribui payload a posição, UV, material, atributo de
vértice, skinning, rigging, animação, morph, comando ou GPU:
`original_semantics=not-claimed`. O readiness registra
`rendering-original-state-geometry-payload-join-control` como
`verified-structural-negative` 24/24; `rendering` continua `not-proven`.

Validação: build Release 0/0, self-test 82/82, matriz completa com exit_code=0,
smokes relevantes, runtime headless/windowed e replays determinísticos. A ISO
permaneceu sem escrita, com 2013003776 bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL Release: `76F273539B1E1D14494B556D2D021F539D1F7EF7F6DB9F182F8237C317DD4FDE`;
relatório `7D0236122EB806FB288DFCE6F1C1923ADEF7E9CF81EE0A7DD0C0FF27D6E33F93`;
summary `48C79333F7C74C36632F7E5C660C677D5265950C4CFCA9D0AE9917E58413EF26`;
readiness `6678BF3B2546A8A802ACB96707CBA660032E06D282CBAE7265AD0AF147BDFED3`;
log da matriz `2B2A1FC0AA3286D9DD7E0A9EB1577F4AA43BD8976A423D0D54A10B373246E649`.

Próxima etapa: continuar a investigação estrutural de renderização e áudio,
sem promover cruzamentos de relatórios a semântica original.

## Etapa 0.60 — partição independente de estados e geometria — 2026-10-07

Foi adicionado o controle Python
`eagl_original_state_geometry_partition_control.py`, que reprocessa
independentemente os relatórios de adjacência, perfil opaco e intervalos
ordenados da renderização original. A verificação juntou 24/24 objetos,
269/269 linhas de perfil e 62/62 intervalos; reconciliou 38 pares geométricos,
180 estados e 180/180 índices de estado cobertos pelos intervalos. O resultado
é `verified-independent-original-state-geometry-partition-control`.

O controle confirma apenas cardinalidade, chaves, cobertura de índices e
forma dos intervalos. Em todos os 24 objetos, `object_state_exact=True`, mas
`geometry_group_state_exact=False`; os 269 perfis têm streams completos e
zero pares de forma de comando exata. Isso não liga estados a mesh groups,
materiais, UVs, skinning, rigging, animação, morphs, comandos ou GPU:
`original_semantics=not-claimed`. O readiness registra
`rendering-original-state-geometry-partition-control=verified-structural-negative`
24/24, sem alterar o gate principal `rendering=not-proven`.

Validação: build Release 0/0, self-test 82/82, smokes originais de
adjacência/perfil/ordem, matriz completa com exit_code=0, runtime headless e
windowed e replays determinísticos. A ISO permaneceu sem escrita, com
2013003776 bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL Release: `2BA8CC8D4DD40AE174787E76B66C02DF18A449FF33AD2DE3FA59E4B673AAFA60`;
relatório `4F788E12FDB3CC447C302DA8E195E3E14B4FAAFEEDDC258CFB7A75A291AC4D0D`;
summary `9C20A83D49482D34CEB1F12E3A249C616F1D541E5CA9C5AE59DEA04CAA46AE4A`;
readiness `D3B754FABCA3A2376B4DD5FA93D98C852D63718B76F156852FFAC98F8E0DD720`;
log da matriz `7FD111FBBD63895829980A0FC9FBDAA0EE7CBB8B6314F362C1F54596C216CD28`.

Próxima etapa: continuar a associação original de renderização e a seleção
original de áudio, preservando os gates semânticos não comprovados.

## Etapa 0.59 — controle negativo de words EVT no XBE — 2026-10-07

Foi adicionado AudioEventXbeHashSearchAudit. Ele procurou os 17 words
observados no índice textual de eventspa.evt dentro dos 3.293.184 bytes do
default.xbe, em offsets alinhados e em todos os offsets de byte. O resultado
foi 0 ocorrências alinhadas, 0 não alinhadas, 17 nomes únicos e 17 words
únicos.

Esse resultado elimina apenas a hipótese simples de que esses words apareçam
literalmente no XBE; não prova ausência de uma rota indireta. Não houve
execução do XBE, recuperação de função, seleção de amostra ABKC ou binding de
gameplay. O readiness registra audio-event-xbe-hash-search como
verified-structural, com direct_selection_binding=not-proven e
original_semantics=not-claimed. O gate principal de áudio continua
partially-integrated=false.

Validação: Release 0/0, self-test 82/82, matriz completa com exit_code=0,
smokes de áudio/renderização, runtime headless/windowed e replays
determinísticos. A ISO permaneceu sem escrita, com 2013003776 bytes, atributo
Archive e SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
CFF31EF95E08040AB38A3581FF55BECD2DC340B56DF27D4E1ED6580DADC0FC3F;
XBE hash-search report
3BB4A38AEB8765B4B56E0F3121C4FF2B7B736D690F10BAF2CBB5DB531963C2C6;
summary 01336D1D9E8A2EA34188DF53BB8506F64DBEEEACDF1B76E2AA56DA120E885880;
readiness 0A2EA4A9A4492B7FDE148286E4C8B7176E0198AEDF00AD751F3BC38D8C54F501.

Próxima etapa: investigar associação estrutural original de renderização além
de intervalos, fetch e adjacência, sem inventar bindings de materiais, UVs,
skinning, rigging, animação, morphs ou GPU.

## Etapa 0.58 — controle estrutural EVT ↔ ranges paralelos de áudio — 2026-10-07

Foi adicionado AudioEventParallelRangeAudit, que revalida os 17 nomes EVT
observados contra o início textual exato em eventspa.evt, a mesma linha nos
companions pcpahdr.off/.big e pcpadat.off/.big, os limites dos ranges e o
prefixo SCHl dos dados. O resultado foi 17/17 âncoras, 17/17 ranges de
header, 17/17 ranges de dados, 17/17 índices paralelos, 17/17 prefixos
SCHl, 17/17 nomes/offsets/linhas únicos, com 283/283 linhas nas duas tabelas.

Isso confirma apenas a cadeia estrutural de texto, índice e payload nomeado.
Não escolhe amostras ABKC, não liga nome a evento de gameplay e não afirma
semântica original: direct_selection_binding=not-proven e
original_semantics=not-claimed. A linha audio-event-parallel-range do
readiness está verified-structural; o gate principal de áudio continua
partially-integrated=false, e os gates authored permanecem separados.

Validação: Release 0/0, self-test 81/81, matriz completa com exit_code=0,
smokes de áudio/renderização, runtime headless/windowed e replays
determinísticos. A ISO permaneceu sem escrita, com 2013003776 bytes, atributo
Archive e SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
6D6CC0AF1ECE8AF58ECE20E9D6DE76F02686ABB053D5C08417EB0DAD5648FB2C;
parallel-range report
F117F7C32189AEBC5A7882FE8E498170F656E91DAB6831F7834E94E2CA6AD670;
summary 5F078EF529C37BD3D0532B8A8A15DD32D807F8DCC0A13EDC7EB1C13E85CD8240;
readiness 4405CF95CA4B6D5EA690B209D756345C49E88F4AEF6A30C74747C87E0E336461.

Próxima etapa: continuar a busca independente por seleção original de áudio e
associação original de renderização, sem reutilizar rotas authored nem
inventar bindings.

## Etapa 0.57 — gates agregados da integração clean-room — 2026-10-07

Foram adicionados ao readiness gates separados para não misturar a integração
clean-room com a semântica original: `assets-clean-room` passou 10/10 cenários,
`rendering-clean-room` passou 140/140 frames,
`audio-clean-room` passou 54/54 eventos tocados e `animation-clean-room` passou
1554/1554 samples authored. O gate principal de gameplay permanece
`clean-room-integrated=true`.

Esses gates confirmam os manifests authored, o framebuffer gerenciado, as rotas
de áudio authored e os traces/replays determinísticos. Eles não promovem
candidate mesh, UV, material, skinning, rigging, morph, timing ou comandos GPU
originais. Os gates originais continuam: assets
`candidate-integrated=false`, rendering `not-proven`, áudio
`partially-integrated=false` e animation-deformation `structural-only=false`;
`original_semantics=not-claimed`.

Validação: Release 0/0, self-test 80/80, Python 43/43, matriz completa com
`exit_code=0`, smokes relevantes, runtime headless/windowed e replays
determinísticos. A ISO permaneceu sem escrita, com 2013003776 bytes, atributo
Archive e SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
4BB5F349628F3BFA8C6A9A425B33BCB41DBFC1135C4FEFA65F351AF5A44774B8;
readiness D126BE88409DDF706F6B320DFAB8EEF16725DACF888E47155193F6B5F82EB2C5.

Próxima etapa: investigar a seleção original de áudio e a associação original
de renderização sem reutilizar as rotas authored nem inventar bindings.

## Etapa 0.56 — gate principal de gameplay clean-room — 2026-10-07

O readiness tinha uma inconsistência: o runtime authored já passava a
cobertura integrada, invariantes e replays, mas a linha principal
`gameplay=clean-room-integrated` permanecia com `gate_passed=false`. O auditor
agora marca esse gate como verdadeiro somente quando as quatro evidências
authored estão presentes: runtime de partida, cobertura de ações, invariantes e
`clean-room-integrated-coverage` completo. A mudança não promove regras do
original: as notas registram `original_semantics=not-claimed`.

O resultado real é gameplay `clean-room-integrated=true`, com 10/10 cenários
integrados, 54 eventos authored, 777 frames, replays determinísticos válidos e
as ações cobertas. Assets continuam `candidate-integrated=false`, renderização
original `not-proven`, áudio `partially-integrated=false` e
animation-deformation `structural-only=false`, pois ainda faltam os bindings e
decoders independentes correspondentes.

Validação: Release 0/0, self-test 80/80, Python 43/43, matriz completa com
`exit_code=0`, smokes relevantes, runtime headless/windowed e replays
determinísticos. A ISO ficou sem escrita, com 2013003776 bytes, atributo
Archive e SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.
DLL Release:
2CB105E28AAE2C9BE49C61A845D97B6AF243933E658E5783E210343682F938CD;
readiness C8CBF5E05C3A205C29615BF6501E8550FFE80414A6F1B41BB4C1C7AB99893ACE.

Próxima etapa: continuar a integração clean-room de assets, renderização e
áudio, enquanto a evidência original permanece separada e não decodificada.

## Etapa 0.55 — controle independente de regiões comando/payload — 2026-10-07

Foi adicionado o controle independente
tools/analysis/eagl_original_command_payload_region_control.py. Ele reparseia
os relatórios originais e cruza somente
model_archive/model_object/layer/primitive. Em 2364/2364 primitivas, as cinco
regiões de payload foram válidas e não sobrepostas; a seção coincidiu em
2364/2364, o intervalo de comando foi válido e disjunto do payload em
2364/2364, e o comando começou depois do maior fim de payload em 2364/2364.

A lacuna bruta entre o maior fim de payload e command_stream_offset foi
0x10 em 2364/2364. Os dois casos sem slot 2 permanecem identificados, sem
preenchimento artificial. Isso é evidência de intervalos, seções e offsets;
não atribui bytes a posição, geometria, UV, material, skinning, rigging,
animação, morph, comando ou GPU. original_semantics=not-claimed.

O readiness registra rendering-original-command-payload-region-control como
verified-structural. Os gates principais permanecem NOT_READY: assets,
renderização original, gameplay original, áudio original e animação/deformação
continuam sem a evidência semântica necessária. A validação teve Release 0/0,
self-test 80/80, Python 43/43, matriz
completed-authored-and-candidate-opt-in, smokes relevantes, runtime
headless/windowed e replays determinísticos passando. A ISO ficou sem escrita,
com 2013003776 bytes, atributo Archive e SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Hashes: DLL Release
C90B6FD374D53B9CA29A60872C5A1B4E15977E59A99605BCF91BCCA9DC1C97BA;
relatório F195DD9E929ED75068638D943008997CD8F198930C461F3AE688159311E78841;
summary 90C3F725B7F21BC1C23C884CA9A8E35949F6ED6E142DBC11794146B142EA44ED;
readiness D48F89BC8DA712F915569FD0CF792A61C4302CFAAC23031E009C6E437FDD9202.

Próxima etapa: buscar associação original independente além da coerência de
offsets e intervalos, sem inventar bindings.

## Etapa 0.54 — controle independente fetch/topologia por primitiva — 2026-10-07

Foi adicionado o controle independente
tools/analysis/eagl_original_primitive_fetch_topology_control.py. Ele reparseia
render-chain e topologia original e cruza somente a chave
model_archive/model_object/layer/primitive. O join foi completo em 2364/2364;
cada primitiva possui cinco linhas de topologia, os IDs de fetch coincidem em
2364/2364 e a concordância da completude também foi 2364/2364.

Foram preservadas 11820/11820 correspondências de ordinal e de range válido,
11818 slots efetivos, 11810 linhas com structural_match e 2362 grupos completos
de cinco slots. As duas exceções são explícitas e correspondem à ausência do
slot 2; não foram convertidas em erro nem preenchidas artificialmente.
original_semantics=not-claimed. O controle não atribui slot ou stream a
posição, geometria, UV, material, skinning, rigging, animação, morph, comando
ou GPU.

O readiness registra rendering-original-primitive-fetch-topology-control como
verified-structural. A validação teve Release 0/0, self-test 80/80, Python
42/42, matriz completed-authored-and-candidate-opt-in, smokes relevantes,
runtime headless/windowed e replays determinísticos passando. Os gates
principais permanecem NOT_READY; a ISO ficou sem escrita, com 2013003776
bytes, atributo Archive e SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: buscar associação original independente além da coerência
estrutural de slots, sem promover campos, UVs, materiais, skinning, rigging,
animação, morphs ou comandos GPU.

## Etapa 0.53 — controle independente estado/fetch ↔ adjacência — 2026-10-07

Foi adicionado o controle independente
tools/analysis/eagl_original_state_fetch_adjacency_control.py. Ele reparseia
os TSVs de render-chain e de adjacência e faz join por
model_archive/model_object/layer/primitive, sem promover semântica. O join foi
completo em 2364/2364 linhas; state relocation apareceu em 2364, SHAPENAME em
752, SetPrimitiveType em 1612 e fetch completo de cinco slots em 2362. O
overlap state/fetch reportado e recomputado foi 750/750; o overlap
state/primitive-type foi 0/0.

O join com a adjacência exata foi 2364/2364; state+adjacência 2364/2364;
state+fetch+adjacência 2362/2364; SHAPENAME+adjacência 752/2364 e
SetPrimitiveType+adjacência 1612/2364. Os histogramas de categoria foram
14:2010 e 15:354, tanto no conjunto completo quanto no subconjunto com state.
Isso é controle estrutural e de coocorrência por chave primitiva: não prova
state→geometria, UV, material, skinning, rigging, animação, morph ou GPU.
original_semantics=not-claimed.

O readiness registra
rendering-original-state-fetch-adjacency-control como verified-structural,
mas os gates principais continuam NOT_READY: assets
candidate-integrated, rendering not-proven, audio parcialmente integrado e
animation-deformation structural-only. A validação da etapa teve Release 0/0,
self-test 80/80, Python 41/41, matriz completed-authored-and-candidate-opt-in,
smokes relevantes, runtime headless/windowed e replays determinísticos
passando. A ISO permaneceu sem escrita, com 2013003776 bytes, atributo Archive
e SHA-256 F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

## Etapa 0.52 — perfil bruto da adjacência comando/descritor — 2026-10-07

O estágio 0.51 foi refinado sem promover semântica: o audit .NET e o controle
Python independente agora registram quais streams validados satisfazem a
adjacência exata, além da categoria e do stride observados. Nos dados reais,
foram 2364 streams exatos em 2364 primitivas; a categoria foi 14 em 2010 casos
e 15 em 354, com stride 2 em 2364/2364. O controle reproduziu os mesmos
histogramas. Isso continua sendo classificação de bytes e offsets; não é
binding de campo, atributo, geometria, UV, material, skinning, rigging,
animação, morph, comando ou GPU. original_semantics=not-claimed.

O subgate permanece verified-structural e o readiness geral permanece
NOT_READY; renderização original, assets originais, gameplay original,
animação/deformação e seleção original de áudio continuam não provados.
Relatórios: audit
46E952F58C8A40507E3A1A2D9C741CD91DBA89611F09D5B860D7CF290AFD8C56 /
E5B5B6D8751D7174E5E609684A77A24503CC7CFE054E538A30F77A4E7E86CF37;
controle 41BC4DCD41AFD0496AE3B265C5AD5410C1EA85A990084C9D7D9FBB05D82D02DC /
E5134201C80EF61C1B76690DB0B6450A375E2D6772E7DF3AEB93686723AE92DF.

Verificação final: Release 0/0, self-test 80/80, Python 40/40, matriz
completed-authored-and-candidate-opt-in, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
7C6750FE5FB3B9820367E6E2A654F8779C6CB2BBF99210BDE65571BD03FE3E1A.
Readiness: 533D08F678639A36808D01E2D2D08CFBAF42B74B486AF5643FBAD5963FAADC23.
A ISO permaneceu somente leitura: 2013003776 bytes, Archive, SHA-256
F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2.

Próxima etapa: buscar evidência independente de semântica de campo/atributo
ou associação estado→geometria/UV/material, sem inventar bindings.

## Etapa 0.51 — adjacência estrutural comando/descritor `__VB` — 2026-10-07

O novo audit .NET e o controle Python independente verificaram, sem semântica
promovida, a relação exata `command_stream_offset = descriptor_offset + 0x0C`
na mesma seção original: `2364/2364` primitivas completas, `10240` candidatos,
`2364` identidades, `2364` identidades atuais únicas, `2364` pares na mesma
seção e `0` identidades deslocadas. O relatório é uma associação estrutural de
offsets, não uma leitura de campo ou binding de geometria, UV, material,
skinning, rigging, animação, morph, comando ou GPU.

O readiness registra o subgate como `verified-structural`, mas permanece
`NOT_READY`; a renderização principal continua `not-proven` e assets, gameplay,
animação e áudio originais continuam separados dos caminhos candidate/authored.
Hashes: audit `4DC8C24FFF144C4FE83659B1F8F8B5D5C83756637EBF5F15230DF04E3481D717`,
summary `00ABD1EF407D5FD733CB55BEB220608413AB54177584B5F1E855C57F0A56B79E`,
controle `DEFA5803260CD6CE977D0411ED325793166DA4FD7DABD02281FD56368C3D3975`,
controle-summary `081BD81C79F80E97D33192F315E8CDAC958F8D478F38703F3997C340E13E0B14`.
O render-chain atualizado é `C6C80BB202ECE7E08AD283338C40629C86421BCCF310F35DD908E793AEBFB84B`
e seu summary `36D8765FB5E3DC1E00ABDF59C8477384575F5D912A1B737F2BB1A971BCD4C8CF`.

Verificação final da etapa: Release `0/0`, self-test `80/80`, Python `40/40`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. DLL:
`CCE12ACE31724C7142EF9E338BD1E547AA362D4C957C6FBDC4CE67D044FA1877`.
Readiness: `DB51CD27DD5F9D0BCFE185B219D4987E3DFAFD066E525750820201E85C191E30`.
A ISO permaneceu somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: obter evidência independente de semântica de campo/atributo ou
associação estado→geometria/UV/material, preservando a classificação como
comprovada, candidata ou ainda não decodificada.

## Gate separado de áudio candidate-only — 2026-10-06

O readiness agora registra `audio-candidate-runtime` como
`verified-candidate`: o full-stack opt-in preparou e reproduziu `8/8` eventos,
com `0` não mapeados e replay coincidente. Esse gate comprova a integração de
áudio candidate-only com o runtime gerenciado; não promove seleção original de
EVT/ABKC/amostra.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`A392BB170AD47F640D354A06F9360A9CDD97071F2194785385F90ABC028C09A7`.
Readiness:
`A694DD3E15444DEDDAB0ADA3A9EA67026497A06690CE11A9629349208C018EEE`.
Full-stack/candidate audit:
`BAA44CB18CDA07380EBC98F546E9081D96DC183716E8D70DB933FC3E4D567940` /
`2160A6CE559D9F03B2F0D6BA01307FACF3E7BA885976070D8D27A21E14E11594`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — cadeia estrutural bruta de renderização EAGL — 2026-10-07

Foi adicionada `fifa_pc/Runtime/EaglOriginalRenderChainAudit.cs` e o comando
`eagl-original-render-chain-audit`. Sem carregar mesh candidate ou textura
SHPX, a auditoria verificou a cadeia estrutural `__Model -> descriptor ->
render method -> global parameters -> command stream` em `388/388` pacotes,
`883/883` objetos e `2364/2364` primitivas; todas as `2364` streams ficaram
completas. `SHAPENAME` aparece em `752` linhas e `SetPrimitiveType` em `1612`,
preservados apenas como texto de estado opaco.

Isso não decodifica comandos, funções, GPU, geometria, UV ou material.
`original_semantics=not-claimed`; o subgate
`rendering-original-render-chain-structure` é `verified-structural`, mas os
gates principais de renderização e assets continuam abertos e o readiness
permanece `NOT_READY`.

Relatórios: `fifa_pc/eagl-original-render-chain.tsv` (SHA-256
`22BE05FEA7A6EC6ACCC9792C3ADED3BE1DB639E9BDD34F35E0BCFD258B9207BB`) e
`fifa_pc/eagl-original-render-chain-summary.tsv` (SHA-256
`66E8F22E71851056985FB1FF2AFECDAAEF7B87B39C66961A1483E75EA38E1E7F`). O
`project-readiness.tsv` tem SHA-256
`D1426A68192DDFD25844A6B0F4F09069CB3F6FD423937341D252E863056C8D6C`.

Verificação: Release `0/0`, self-test `74/74`, Python `32/32`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, captura
windowed e replays determinísticos passaram. A ISO permaneceu somente leitura,
com `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar associação independente entre estado bruto e
geometria/UV/material ou registrar um controle negativo adicional, mantendo
separados os `216` layouts candidate completos e os `2` assets parcialmente
completos.

## Gate separado de renderização candidate-only — 2026-10-06

O readiness agora registra `rendering-candidate-runtime` como
`verified-candidate`, sem alterar o gate original `rendering=not-proven`. A
verificação usa o cenário full-stack opt-in: `140/140` frames, `24` assets
candidatos, `280` samples authored de animação, `8/8` eventos de áudio
preparados/reproduzidos, replay coincidente e framebuffer válido. Isso prova a
integração candidate-only do runtime gerenciado; não prova estado original de
renderização, UV, material ou GPU.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`C2E72072AC40D16F7B3376B5DFD91F3EE85E7C50425A264EFE6AD66B5F99F800`.
Readiness:
`0A317FA850F5D5F16C23898E8291FF1D1DCABD7D11ABBB406A50E71631068736`.
Full-stack/candidate audit/material batch:
`CCF18766366ED94F8E8442A83F882CFF1504875EB0B460388841C499E86508CA` /
`C679F93F8B63E083A6FE27C15456FEC6874708891DFD8F41E4F0E0B2238A3869` /
`CE06E0502DFCD99E1354C5279089B1A11BCD6D9C00832D42447DFCE4469AAAD0`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Call-sites bounded do destino aninhado — 2026-10-06

Foi adicionado `xbe_render_indirect_indexed_cfg_nested_call_site_audit.py`.
Filtrando o contexto do slice de `0x0016BF90` pelos destinos internos, foram
observados `2` call-sites para `0x0026E278`, com `1` destino distinto. Nos dois
casos o contexto local contém `fld`, `fmul`, `call` e `ret 8`; `0/2` têm
`push edx`/`lea [ecx+...]`/`ret 4`, e não há deslocamento `lea` distinto. Isso
é apenas contexto de instruções bounded e não decodifica função, objeto ou
semântica.

Status: `verified-bounded-static-render-indirect-indexed-nested-call-sites`;
`original_semantics=not-claimed`; nenhum material, UV, skinning, rigging,
animação, morph ou GPU foi ligado. Readiness: `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `31/31`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`AA55B7737D13A532ACA1B1CDC5553B93A9A6B9DC5A439A15F6C901A74B868567`.
Readiness:
`1442E380FE5C11D1D0D702A448E3B950733A6D8D8BF534191A0B49829A65248E`.
Nested call-site audit/summary:
`4F760A2CB3F8F47BC4BD2A0AB2A03D263A481AE6C50853BE3DAFB9DD084A8E56` /
`870483285414A5E646CDC7C51DC1F23EAFC60387AD9E91005B7AB4DDC88AF15D`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## CFG bounded do destino aninhado — 2026-10-06

O novo auditor `xbe_render_indirect_indexed_cfg_nested_call_target_cfg_audit.py`
inspecionou somente o próximo destino direto observado dentro do slice de
`0x0016BF90`: `0x0026E278`. A janela bounded de `256` bytes contém `89`
instruções lineares, `37` alcançáveis, `7` blocos, `3` branches condicionais,
`2` incondicionais, `4` alvos internos, `1` retorno alcançável e `0` chamadas
diretas adicionais. Isso é caminho local candidato; não é função recuperada e
não segue uma nova chamada.

Status: `verified-bounded-static-render-indirect-indexed-cfg-nested-call-target-cfg-slices`;
`original_semantics=not-claimed`; nenhum material, UV, skinning, rigging,
animação, morph ou GPU foi ligado. Readiness: `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `30/30`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`395813624F6C4AD23B4FA1D11BF44DF7C6697C2189282E32319200BCAD737982`.
Readiness:
`0F5A8B50AB5D8D04A300AB0BA883782D4D6017546400753C727EB86D03FE196A`.
Nested audit/summary:
`5E73FAF3C58277F98B08DC4F9030673E381000B6188AB5018BB7AB9B564336E3` /
`2C18CBB0DC2B53C4E4795945F0786B5011E747C12DC3CCFFB7A1FDAEF4C17D33`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Join bounded entre tabela e call-sites indexados — 2026-10-06

Foi adicionado `xbe_render_indirect_indexed_cfg_call_site_join_audit.py` para
cruzar, sem execução, os alvos da tabela indexada com o contexto dos call-sites
alcançáveis. O join preservou `81` linhas de alvos, `1` base de tabela, `24`
call-sites, `24/24` linhas joined, `1` alvo distinto, `27` índices distintos,
`27` ocorrências de entrada e `3` ocorrências duplicadas sobre os call-sites;
`24/24` call-sites mantiveram forma local completa. Isso é evidência bounded
estática, não identificação de função, objeto, campo ou semântica.

Status: `verified-bounded-static-render-indirect-indexed-call-site-join`;
`original_semantics=not-claimed`; nenhum material, UV, skinning, rigging,
animação, morph ou GPU foi ligado. Readiness: `NOT_READY`.

Validação independente: Release `0/0`, self-test `72/72`, Python `29/29`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`CF1E344282E9237943573EF27A7F24A1F12B662476175A819337DCF0B50A9959`.
Readiness:
`EC3F4027BE15A0829D65EF6E76E6B65E0BB3BDD68A4FC2A6B3E732D1AC7F9BBD`.
Join audit/summary:
`6742EB92434168D7A04C695BB77DCF8632AC1534ACB702E144401AA557FD81AE` /
`71BA5028FCDFBDF276848DC9FD50619EA615D08948CFB806C58D164B832FFBC9`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## CFG bounded do destino direto indexado — 2026-10-06

Sobre o único destino distinto `0x0016BF90`, a auditoria aplicou CFG bounded
de `256` bytes sem seguir suas chamadas. O ISO produziu `76` instruções
lineares, `23` alcançáveis, `7` blocos básicos, `2` branches condicionais,
`2` chamadas diretas e `0` transferências indiretas; `1/1` `ret` foi alcançado.
Isso é fluxo local candidato, não limite de função ou semântica.

Status: `verified-bounded-static-render-indirect-indexed-cfg-call-target-cfg-slices`;
`original_semantics=not-claimed`; nenhum material, UV, skinning, rigging,
animação, morph ou GPU foi ligado. Readiness: `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `28/28`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`06483C77B64112CCFE79F635DCE1B4252F7C1D2FD92A7BB6E6744B71B11F1FC8`.
Readiness:
`975217DC8EFDDA58C4D35A9218EF93E1D467A82701B4A85233475181AF76EB60`.
Audit/summary:
`8CBBF5C159106F6D98CE05D65A88ACD76179E863A89763202649B431D450EE77` /
`6250D59561DE79E73027693C34E8853DC33EBE4A91941AAE6ABCDFFD0AA85ED2`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Contexto bounded dos 24 call-sites indexados — 2026-10-06

Foi adicionada a auditoria `xbe_render_indirect_indexed_cfg_call_site_audit.py`.
Ela preserva o contexto local das `24` chamadas alcançáveis nos CFGs indexados:
as `24` convergem para `0x0016BF90`; em `24/24` há `lea` com base `ecx` e
deslocamento distinto, `push edx` antes da preparação, e `ret 4` após a chamada.
Essas regularidades são observações de instruções bounded, não identificação
de wrapper, função, objeto, campo, material, UV, skinning, rigging, animação,
morph ou GPU. `original_semantics=not-claimed`; readiness: `NOT_READY`.

Status: `verified-bounded-static-render-indirect-indexed-call-sites`.
Validação: Release `0/0`, self-test `72/72`, Python `27/27`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`FE1DEAB66AC78E1FDA154848394C6EAA3A06FB3A87E563EC5609CE0A313F1E37`.
Readiness:
`B18D55EC12C80D7888B4EC441A1315C17DF9A7BD7AA036E08EEC7C6FC794EB6C`.
Audit/summary:
`1BC39F705C0AAADFC34AAD030CFB0E29B2A87197E71A9E3E4363A34B16650474` /
`227EC2C511556952E038E9AB725B76B5E68459AA3F2BF8EF57B987BEF2219A92`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Destino direto bounded dos CFGs indexados — 2026-10-06

O novo auditor inspecionou os destinos diretos observados nos CFGs da tabela
indexada. As `24` ocorrências (`24` alvos únicos somados por slice) reduzem-se
a `1` endereço distinto, `0x0016BF90`; ele mapeia em `.text`, offset de arquivo
`0x0015BF90`, com janela bounded de `256` bytes, `76` instruções e `ret`
observado. Não houve prólogo reconhecido. Isso é apenas mapeamento e slice
estática do destino; não segue chamadas, não recupera função e não atribui
semântica.

Status: `verified-bounded-static-render-indirect-indexed-cfg-call-target-slices`;
`original_semantics=not-claimed`; nenhum material, UV, skinning, rigging,
animação, morph ou GPU foi ligado. Readiness: `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `26/26`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos. DLL:
`0FF23943CA552774AC25810A2B4B6D0C066F13862EB36A3D70BA787C9E44274A`.
Readiness:
`73AB08762847C2F91C282C5A4C749863B692B8DB61E9B42A850D1655C7379328`.
Audit/summary:
`41E108CCEC548410358EF649AD9F3DBB24448175CF1E4E44D9C388F9FB1463A2` /
`E329BE8965964C3995347AFF7442B7BF403E947B7D0412BDD44D97512F86414C`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Distinção de chamadas diretas e controle negativo de CFG — 2026-10-06

O relatório CFG dos `81` candidatos da tabela indexada foi refinado para
preservar a diferença entre ocorrência e endereço distinto: há `24` chamadas
diretas registradas nos slices (`24` alvos únicos somados por linha), mas
somente `1` endereço distinto global, `0x0016BF90`. Isso continua sendo uma
observação bounded de fluxo local; não é limite de função, seguimento de
chamada, ponteiro ou semântica.

O novo join compara esse conjunto unitário com `87` alvos de chamadas diretas
de renderização, `64` alvos das janelas de renderização e `235` nós do grafo
bounded. As três interseções foram `0`. O resultado
`verified-structural-negative` é controle negativo de conjuntos de endereços,
não prova independência comportamental. `original_semantics=not-claimed`;
nenhum material, UV, skinning, rigging, animação, morph ou GPU foi ligado.
Readiness: `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `25/25`, matriz
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
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## CFG bounded dos candidatos da tabela — 2026-10-06

O auditor aplicou alcançabilidade estática bounded aos `81` alvos distintos da
tabela indexada. O ISO produziu `81/81` CFGs alcançáveis e `81/81` `ret`
alcançáveis, com `7.295` instruções lineares, `421` alcançáveis, `105` blocos
básicos, `24` chamadas diretas registradas sem seguimento e `0` transferências
indiretas dentro das janelas de `256` bytes.

O resultado `verified-bounded-static-render-indirect-indexed-cfg-slices` é
somente fluxo local estático: não recupera funções, limites, ponteiros,
chamadas ou semântica. `original_semantics=not-claimed`; nenhum material, UV,
skinning, rigging, animação, morph ou GPU foi ligado. Readiness: `NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `24/24`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`5A3E8814C7AE038932856353E65E5E2322CC326A514BBDCB44356D8E0813714C`.
Readiness:
`0C9D676A829523BFF1B62A72BFF985222213B868855D05ED7B57414B61E12D38`.
CFG audit/summary:
`3DF517090ABE885BB013E6F012A23B6CF99EF4482DA12BBF56E014B3FC9E3756` /
`57E7E7F2138EC6C6FD707D1FA5956DF542DD77AFD89F04CE7E90A28D624D21D5`.
ISO: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Controle negativo de interseção dos alvos — 2026-10-06

O novo auditor compara somente conjuntos de endereços bounded: `81` alvos
distintos candidatos da tabela indexada, `87` alvos do auditor de chamadas
diretas e `64` alvos da janela de renderização. As interseções foram `0` e `0`.

O resultado `verified-structural-negative` é apenas um controle negativo de
endereço; não prova independência de comportamento, nem recuperação de função,
chamada, ponteiro ou semântica. `original_semantics=not-claimed`; nenhum
material, UV, skinning, rigging, animação, morph ou GPU foi ligado. Readiness:
`NOT_READY`.

Validação: Release `0/0`, self-test `72/72`, Python `23/23`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`D4289C65CD3E4FCD0953C3DD625D6EC1544E641F348821D4A96A693B74CD0ED3`.
Readiness:
`128EA296B76FBF9E49214008CAADD3E257E2E77087BA1F06414E5CDCE6C3B30A`.
Join audit/summary:
`5A8BE06EAD3A8FD27920F2151D88C262E4245D1E7A50C5789BB2F33AD79A16C9` /
`6E38D14E644E9A7034ED4B5424428FD067AE7DB8E226ED82C9E3E11CD2F5DDB1`.
ISO: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Slices bounded dos alvos da tabela indexada — 2026-10-06

O auditor agora faz desassemblagem de janelas de até `256` bytes nos candidatos
executáveis do prefixo da tabela indexada, sem execução e sem delimitar
funções. O ISO produziu `87` ocorrências, `81` alvos distintos, `81/81`
mapeados/desassemblados, `81/81` com `ret` na janela e `0` prólogos
reconhecidos.

É evidência estrutural candidata, não recuperação de função, ponteiro ou
semântica. Status:
`verified-bounded-static-render-indirect-indexed-target-slices`;
`original_semantics=not-claimed`; readiness continua `NOT_READY`. Não foram
atribuídos materiais, UVs, skinning, rigging, animação, morphs ou GPU.

Validação: Release `0/0`, self-test `72/72`, Python `22/22`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`0C23802D85483FA565E1BE497610922B7C160ACFD80137F963FFDE07D2039210`.
Readiness:
`55D15A89A45E91B8ED86626BF1E1C46D9F447C9FB0DC15E6D221D649C636A5AA`.
Audit/summary:
`FA7A2DB7409B43C8A42EB7E8A5FD5C5A988CD06758C75A2C71FD7A28E19C8399` /
`BD71ED8F44CB15754045E9DDF5E58F828A55D0BE47FEBEEA26B2C434EA5DBEC7`.
ISO: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Varredura limitada da tabela de transferência indireta — 2026-10-06

Foi implementado um auditor somente leitura para a única transferência
indireta indexada sem registrador-base encontrada nas janelas de render. A
varredura limitada não atingiu o teto de `256` entries e terminou após duas
words não executáveis. No ISO: `1` row/base indexada, `89` words legíveis,
`87` executáveis, `2` não executáveis, `0` ilegíveis, `81` executáveis
distintas e prefixo executável contíguo máximo de `87`.

O status é `verified-bounded-static-render-indirect-indexed-table`: prefixo
estrutural candidato classificado por pertencimento à seção executável. Não é
recuperação de tabela/função/ponteiro nem binding; `original_semantics=not-claimed`.
O gate geral de renderização continua `not-proven` e readiness `NOT_READY`.
Materiais, UVs, skinning, rigging, animação, morphs e comandos GPU continuam
sem semântica atribuída.

Validação: Release `0/0`, self-test `72/72`, Python `21/21`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`EF02D432D4C534F0AA1F572B7E3551E2B75C14B2AEB4AA7F03F278C0B72D3A7E`.
Readiness:
`0FB62081FFAB468D6FA8985C5F160BD3480FCC9E28FB5E17475211EA07BBC36D`.
Audit/summary:
`0638A839ACF1191386669065B3E714AA8AEB4F2B0928D5983B48892587B1E895` /
`58271102E9D61725E85F0AB082AB4BF99556651A5141BAF3AE548461AE5C780A`.
ISO: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Observabilidade explícita do áudio candidato no runtime — 2026-10-06

O artefato de integração agora distingue o caminho authored dos payloads
candidatos preparados durante a inicialização do mixer. A execução real
registrou `17` candidatos EVT nomeados preparados (`2.914.952` bytes PCM e
`0` rejeições) e `16` bancos candidatos observados (`15` decodificados e `1`
rejeitado). Os cenários authored continuam com `8/8` eventos preparados e
tocados, `0` não mapeados. São contadores de decodificação/observabilidade;
não criam vínculo EVT→evento, ABKC→sample ou semântica original.

Validação: Release `0/0`, self-test `72/72`, Python `20/20`, matriz completa
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos. Readiness: `NOT_READY`,
`original_semantics=not-claimed`.

DLL Release:
`97C05064D768A18D73F74ECC5DBA6DA36DA03024148A071E9444D73EFFD2776E`.
Readiness:
`F5A26E1A1002363CDD238BAF64382084DD0BD6C333628A2309FBA5525E3DD92F`.
A ISO permanece somente leitura, `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Interseção exata entre transferências e referências — 2026-10-06

O join de renderização agora cruza também o endereço exato da janela e da
instrução. Foram encontrados `4` janelas e `8` rows de referência coincidentes
com instruções de transferência indireta. A conclusão permanece estrutural,
com `original_semantics=not-claimed`; nenhum binding de função, ponteiro,
material, UV, skinning, rigging, animação, morph ou GPU foi inferido.

Verificação: Release `0/0`, self-test `72/72`, Python `20/20`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`3AD11C6CB09CE3FA2B59F317C612C049B670D3A59C8274FB63FD4D1DD5BB4E3D`.
Join audit/summary:
`59E06AC832B3CFF34DD7F00392052EC9CE83BD0C1785525F0AF8411286C8C7DD` /
`A101B0E7E24548621F6F3EC2A1CB892F996176C4D12D0131DFB3305F8B73802D`.
Readiness: `6F0919D54C5C897041E6BAF4237455B234E4BDF7321A3910488DE6D93B54FB01`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Join estrutural de renderização/indireções — 2026-10-06

`xbe_render_indirect_join_audit.py` agora cruza exatamente as `64` janelas de
render com `22` transferências indiretas (`21` calls, `1` jump), `8` referências
e `3` words brutas. Resultado: `15` janelas com transferências, `4` com
referências e `3/3` slices executáveis/terminadas por `ret`.

O status é `verified-static-render-indirect-window-join`, com
`original_semantics=not-claimed`. O subgate de readiness é estrutural; o gate
geral de renderização continua `not-proven`. Nenhum binding de material, UV,
skinning, rigging, animação, morph ou GPU foi inferido.

Validação: Release `0/0`, self-test `72/72`, Python `20/20`, matriz completa,
smokes, runtime headless/windowed e replays determinísticos. DLL:
`7D6D245F1E591C968F6C9D0DFECF3AD7773A3CE2DF25B36889AB987483A2E983`.
Join audit/summary:
`E10ADF511CDDFF4F1604DA4EFDBB312848FBEF2491F1E8764DD469919DB08A68` /
`233EDEB9C3F5D82FCF446742DE65F7C9585A21D47FAD29DF02D1150596FDD188`.
Readiness: `879B82F85C986E71F1F224E8006D650F4EE91D580EC7998C30295AA1E64E41BE`.
A ISO segue somente leitura, `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Controle estrutural de multiplicidade morph/animação — 2026-10-06

O auditor de perfil de relocations passou a comparar também a multiplicidade
dos gaps consecutivos, preservando-a no relatório como `morph_gap_histogram`.
No catálogo real: `594` linhas de morph, `393` layouts válidos e `19` bancos de
animação. O resultado foi `53` layouts/`159` pares por contenção de conjunto,
`0` por conjunto exato e `0` layouts/`0` pares por histograma exato.

O resultado não sustenta uma associação morph/animação: é apenas controle
estrutural de recorrência. Frames, poses, timing, rigging, skinning, morphs,
materiais, UVs, GPU e bindings originais continuam semântica não decodificada;
`original_semantics=not-claimed`. A matriz agora regenera e verifica o relatório
e o resumo antes dos demais artefatos.

Verificação da etapa: Release `0/0`, self-test `72/72`, Python `19/19`, matriz
authored/candidate opt-in completa, smokes, runtime headless/windowed, replays
determinísticos e readiness `NOT_READY`.

DLL Release: `9776FD7946552D26CABF479C2DF5EA145C5CD76317A119C2FAF4F94C86276B51`.
Audit: `D30560728E8BC701D1765558FF012D3CC36DF72E89421E255F6D6F1845379691`.
Summary: `FF42635A3E711AD2644BD9946A129541C7A99FC8E17DD781E53C18344FD8D9A2`.
Readiness: `1F2092C75C86088F1D21014CB02587E2727107268301A8F944AD4F00D12E5FF3`.
A ISO permanece somente leitura: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Startup do host Win32 Release — 2026-10-06

O executável Release `fifa_pc/bin/Release/net8.0/FifaStreetPc.exe` foi
iniciado no modo `run-windowed` com personagens clean-room e material
candidate opt-in. O processo permaneceu vivo durante o smoke e confirmou
`candidate_material_status=validated-candidate`,
`render_characters=clean-room-procedural`, os dois slots candidate habilitados
e `texture_decodes_validated=2`. O harness encerrou o processo após a
observação (`EXIT_CODE=1` corresponde apenas ao interrupt).

O status é `verified-windowed-startup-only`: não substitui screenshot/QA visual
independente e não promove qualquer binding original de mesh, material, UV,
skinning, rigging, animação, morph, GPU ou áudio. `original_semantics=not-claimed`.

## Captura determinística do framebuffer Win32 — 2026-10-06

O host Release passou a aceitar `--run-seconds` e `--capture-frame`. O smoke
com personagens clean-room e material candidate opt-in terminou com
`EXIT_CODE=0` após dois segundos e gravou
`fifa_pc/windowed-runtime-capture.ppm`: P6 `960x540`, `1.555.215` bytes,
checksum `0F83CEFDD78C58BA`, SHA-256
`E1D1FE0294EA554F601F384C15D87B3FEA4C4649BE93EC2527BC10CA8269AD15`.

`status=verified-windowed-frame-capture` e
`provenance=managed-software-framebuffer`: a captura comprova execução da
rasterização no loop Win32, mas não substitui screenshot independente nem
promove bindings de mesh, material, UV, skinning, rigging, animação, morph,
GPU ou áudio. `original_semantics=not-claimed`.

## Fechamento da etapa — 2026-10-06

O build Release passou com `0/0`, o self-test com `72/72`, os testes Python
com `11/11` e a matriz authored/candidate opt-in terminou com smokes, runtime
headless e replays determinísticos `matched=True`.

DLL Release SHA-256:
`FF3514309EEB796CD009FED5B9D9B1A4A48E2A5C4B2B3A92C660085B85503917`.
O readiness continua `NOT_READY`, SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
A ISO continua intacta, com `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa — cobertura authored explícita de TACKLE

O readiness agora separa o subgate `gameplay-tackle-trace`. O trace e replay
determinísticos verificam `5/5` eventos: `KICKOFF`, `STEAL`, `TACKLE`, `STEAL`
e `SHOT_AWAY`, com SHA-256 idêntico
`AC986AB9737AD4A498B9353C693B624C646EE8901208B0C5FCDC63C530552B53`.

É apenas cobertura authored clean-room; não prova regra, controle ou posse
originais, e `original_semantics=not-claimed` permanece explícito.

Verificação: Release `0/0`, self-test `72/72`, Python `19/19`, matriz completa,
smokes, runtime headless, replays determinísticos e captura windowed. O projeto
continua em andamento e a ISO permanece somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa — slices estáticos dos words de indireções

Foi implementada `tools/analysis/xbe_render_indirect_reference_slice_audit.py`.
O relatório agrupa os `8` rows de referência em `3` words distintas e faz
disassembly bounded de `256` bytes somente quando o word cai em seção
executável. Os `3/3` candidates foram disassemblados e todos têm `ret`
alcançável; `prologue_like_candidate_words=0`.

Isso não é execução nem recuperação de função: os words permanecem candidatos,
sem chamadas seguidas e sem semântica de geometria, materiais, UVs, skinning,
rigging, animação, morphs ou GPU. O subgate
`rendering-static-xbe-indirect-reference-word-slices` está
`verified-structural`; renderização geral continua `not-proven`.

Relatórios: [audit](D:\projeto zeta\fifa_pc\xbe-render-indirect-reference-slice-audit.tsv)
e [summary](D:\projeto zeta\fifa_pc\xbe-render-indirect-reference-slice-summary.tsv).
SHA-256: `16FB2AA7CDC423A1C70D4588C6C5731FD5FACEEE1D59F7BDB0E3A3ACA253444C` e
`4175B05F115DCD37BC9048C37369D8A3E42F12B30F2128E7AB3464BD64C67AF5`.

Verificação: Release `0/0`, self-test `72/72`, testes Python `19/19`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. O projeto permanece em andamento e a ISO
continua somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — formas estruturais dos acessores da tabela indexada — 2026-10-07

Foi adicionada `tools/analysis/xbe_render_indirect_indexed_accessor_audit.py`.
Ela observa, em no máximo `256` bytes e até o primeiro `ret` observado, os
destinos executáveis candidatos da tabela indexada já enumerada. A execução no
mesmo XBE retail encontrou `87` ocorrências, `81` destinos distintos, `81/81`
destinos mapeados/disassemblados e `81/81` terminados por `ret`.

As formas observadas foram: `1` fallback `pop/pop/ret`, `55` formas
`mov-memory/ret`, `24` formas `lea/push/call/ret` e `1` outra forma. Também
foram registrados `107` deslocamentos de memória distintos e `1` alvo direto
distinto de chamada. Isso é somente regularidade de bytes/instruções em uma
janela bounded: não houve execução do XBE, recuperação de limites de função,
identificação de campos ou criação de binding para geometria, materiais, UVs,
skinning, rigging, animação, morphs ou comandos GPU. O artefato marca
`status=verified-bounded-static-render-indirect-indexed-accessor-shapes` e
`original_semantics=not-claimed`.

Relatórios: `fifa_pc/xbe-render-indirect-indexed-accessor-audit.tsv` (SHA-256
`B492296364C4BD853BBD922E1DCBE3B7A1069CC943CDDEA13745F3A387DED5D5`) e
`fifa_pc/xbe-render-indirect-indexed-accessor-summary.tsv` (SHA-256
`6B162EC7EBC4C7E60620B30A73211B5FCED7C3F74AB9844B1D9746F45DF31033`). O
readiness registra `rendering-static-xbe-indirect-indexed-accessor-shapes`
como `verified-structural`, mas o gate principal de renderização permanece
`not-proven`; assets continuam `candidate-integrated`, áudio
`partially-integrated` e animação/deformação `structural-only`.

Validação final: Release sem avisos/erros, self-test `72/72`, testes Python
`32/32`, matriz `completed-authored-and-candidate-opt-in`, smokes relevantes,
runtime headless, replays determinísticos e captura windowed. A DLL Release
tem SHA-256 `ECD30A5D7FD001B11DEA44D1752C1225E58A4CEAD64CDF00420E01F7E11C1B49`
e o readiness final tem SHA-256
`6D079FB267E09EDAB54655B270CA8FF34F681821F788BD176942B7EEDC09BC35`.
A ISO permaneceu somente leitura, com tamanho `2013003776`, atributo `Archive`
e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — associação estrutural de slots e registros `__VB` — 2026-10-07

Foi implementada `fifa_pc/Runtime/EaglCandidateFetchAssociationAudit.cs` e o
comando `eagl-candidate-fetch-association-audit`. Ele cruza, apenas no caminho
candidate same-trunk, os slots observados nos streams de comandos com os
registros `__VB` já validados por símbolo, stride, comprimento e contagem de
elementos. O resultado real foi `24/24` assets carregados, `218` primitivas,
`216` layouts completos de cinco slots e `1080/1080` registros de slot válidos;
`22/24` assets têm todas as primitivas nesse layout e `0` candidato foi
descartado.

O subgate `rendering-candidate-fetch-record-association` é
`verified-structural`. Ele preserva números, nomes de símbolos e metadados
brutos; não chama slots de UV, material, normal, peso, skinning ou comando GPU,
não recupera função e não promove o caminho candidate ao gate original.
`original_semantics=not-claimed` permanece. Assets continuam
`candidate-integrated` e rendering continua `not-proven`.

Relatórios: `fifa_pc/eagl-candidate-fetch-association.tsv` (SHA-256
`4F78ECAAE17900566A2A4D56E6CF17E1595965AC3D332184C356ED5EB3586FAF`) e
`fifa_pc/eagl-candidate-fetch-association-summary.tsv` (SHA-256
`1B5619DAD490E07B3B3075B0DDFC37E3A4FF6027114206A50EF438074F558235`).

Validação final: Release sem avisos/erros, self-test `73/73`, testes Python
`32/32`, matriz `completed-authored-and-candidate-opt-in`, smokes, runtime
headless, captura windowed e replays determinísticos com `matched=True`. DLL
Release: SHA-256
`8382DBC400647A9F9290EA1A7809CDE01A29751F8EF54828AF598E01489334C1`;
readiness: SHA-256
`E763C2762DBB784721A93A24170B98D6CD22427D0C710F23C3BFF8A64D34E22C`.
A ISO manteve `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Correção — caminho do trace aimed de input

O readiness foi ajustado para preferir os artefatos atuais
`gameplay-authored-aimed-input-trace.tsv` e seu replay, mantendo fallback para
o fixture legado do self-test. A divergência anterior vinha do cabeçalho antigo
sem a coluna `tackle`.

O subgate `gameplay-aimed-input-trace` agora está `verified-authored` (`80/80`)
com amostra vertical em `frame=67` (`move_y=0.75`, `shoot=True`). Isso não é
evidência de binding original de controle; `original_semantics=not-claimed`.

Verificação: Release `0/0`, self-test `72/72`, runtime aimed de `80` frames,
replay determinístico de match/input/animação/áudio com `matched=True` e testes
Python `18/18`. Input trace e replay: SHA-256
`0A1D6B3B30E96B3ECA8A66A0811119AC03A82130914315B547C9276505B010A4`.
O projeto continua em andamento (`NOT_READY`) e a ISO permanece somente
leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa seguinte — words referenciadas por indireções do renderizador

Foi implementada `tools/analysis/xbe_render_indirect_reference_audit.py` para
ler somente words bounded nas referências de memória sem base explícita do
inventário de `22` transferências indiretas. O relatório encontrou `8`
referências (`7` absolutas e `1` base de tabela indexada); `8/8` leituras são
válidas e `8/8` caem em seções executáveis, com `3` endereços e `3` words brutas
distintas.

É evidência estática de bytes referenciados, não resolução de função, alvo ou
binding. Não houve execução nem chamada de endereço, e nenhum binding de
geometria, materiais, UVs, skinning, rigging, animação, morphs ou GPU foi
inventado. O subgate `rendering-static-xbe-indirect-reference-words` está
`verified-structural`; o gate de renderização continua `not-proven` e
`original_semantics=not-claimed`.

Relatórios: [audit](D:\projeto zeta\fifa_pc\xbe-render-indirect-reference-audit.tsv)
e [summary](D:\projeto zeta\fifa_pc\xbe-render-indirect-reference-summary.tsv).
SHA-256: `9E32F0744ACB8BDA565993876F81F2EDE789C574F08FA56BAA198524E0B25836` e
`3FF5EBDE33A7ECB8A96BCE8AD8B4CB0609C95BE0FA682DBE56BD8ED7796BE38A`.

Verificação: Release `0/0`, self-test `72/72`, testes Python `18/18`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. O projeto permanece em andamento e a ISO
continua somente leitura com `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa seguinte — inventário estático de transferências indiretas — 2026-10-06

`tools/analysis/xbe_render_indirect_transfer_audit.py` revisita as janelas CFG
de renderização e registra operandos de `call`/salto indiretos alcançáveis sem
desreferenciar seus destinos. Foram observadas `22` transferências em `15`
alvos (`21` calls e `1` jump), com `21` operandos de memória, `1` registrador,
`7` memórias absolutas e `11` formas distintas.

O resultado é `verified-structural`, não recuperação de função nem binding.
Ponteiros permanecem não resolvidos; nenhuma semântica de material, UV,
skinning, rigging, animação, morph ou GPU foi atribuída. O subgate
`rendering-static-xbe-indirect-transfers` passou, mas o gate original de
renderização continua `not-proven` e `original_semantics=not-claimed`.

Relatório: `fifa_pc/xbe-render-indirect-transfer-audit.tsv`, SHA-256
`265A0ACC8B3369F34D68EE5FD5563587978977CD83097C1EEBDF5ED743F7E926`; resumo:
`37D34311B33DD25DB1CB034DB2D0C3733D0AB843313D405D22647728087C7F6E`.
Verificação: Release `0/0`, self-test `72/72`, Python `17/17`, matriz completa,
smokes, runtime headless, replay determinístico e captura windowed. A ISO
continua somente leitura, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — controle defensivo authored `TACKLE` — 2026-10-06

Foi implementado no runtime managed clean-room um controle defensivo authored
com `MatchInput.Tackle`, décima coluna opcional do script de input, tecla `K`
no host Win32 e demo determinístico. A regra, explicitamente nova, transfere a
posse apenas na borda do input sob posse authored do adversário e proximidade;
aplica proteção de 0,20 s e +10 no medidor de gamebreaker. Nenhuma semântica do
input, colisão, posse, animação ou áudio original foi reivindicada.

O cenário validado é `KICKOFF → STEAL → TACKLE → STEAL → SHOT_AWAY`. A cadeia
`TACKLE` usa o perfil de apresentação authored de `STEAL` e a rota de áudio
clean-room `tackle`; isso não é binding original EVT/ABKC/sample. Materiais,
UVs, skinning, rigging, animação, morphs e GPU continuam sem bindings inventados.

Fechamento: Release `0/0`, self-test `72/72`, Python `16/16`, matriz completa
`completed-authored-and-candidate-opt-in`, 10/10 cenários verificados, 54
eventos gameplay/áudio, 54 preparados/reproduzidos, 777 frames renderizados,
1.554 amostras authored e replays determinísticos coincidentes. O readiness
continua `NOT_READY`; renderização original é `not-proven`, assets permanecem
candidatos, áudio geral parcial e animação/deformação estrutural-only.

A ISO permaneceu somente leitura (`2013003776` bytes, `Archive`), SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Relatório
de readiness SHA-256
`4E4991215D3E53EE1EAC27CBF5D5D5B813A112F14096A20B5549DDBA620930F6`; DLL
Release SHA-256
`EAB3E92D2ACE4C11F2D6FFFDFFB9281F50643F311AA22541E5236A73CD0636DD`.

## Etapa atual — alvos diretos de chamada do renderizador

Foi implementada `tools/analysis/xbe_render_call_target_audit.py`, uma
análise estática one-hop sobre chamadas diretas observadas nos CFGs limitados
dos 64 alvos de renderização. Foram encontrados `87` alvos únicos; `87` foram
mapeados e disassemblados, `75` alcançaram `ret` e `28` exibiram prólogo
semelhante a função. A análise não segue chamadas recursivamente e não resolve
transferências indiretas.

O subgate `rendering-static-xbe-call-target-slices` é
`verified-structural`. Isso não confirma limites de função, execução do XBE
ou semântica de renderização, e não cria bindings para materiais, UVs,
skinning, rigging, animação, morphs ou comandos GPU. O gate principal de
renderização permanece `not-proven`, com `original_semantics=not-claimed`.

Verificação: Release `0/0`, self-test `72/72`, testes Python `15/15`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. O projeto não está concluído. A ISO segue
somente leitura, com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Regressão Win32 na matriz — 2026-10-06

A matriz authored/candidate passou a executar o host `run-windowed` por dois
segundos e validar automaticamente o primeiro PPM: `P6 960x540`,
`1.555.215` bytes e `nonuniform=True`. Isso verifica repetidamente o loop de
rasterização managed; não promove bindings originais nem altera os gates
`assets`, `rendering`, `audio` ou `animation-deformation`.

## Readiness após o subgate Win32 — 2026-10-06

O relatório atual registra `rendering-authored-windowed-runtime` como
`verified-authored`, `1/1`, porque a captura PPM foi persistida e validada.
Isso não altera o estado global `NOT_READY`: `rendering` original permanece
`not-proven`, `assets` é candidate-only, `audio` é parcial e
`animation-deformation` é structural-only. Proveniência:
`managed-software-framebuffer`; `original_semantics=not-claimed`.

Hashes correntes após Release/matriz:

- DLL Release: `9CCE2B05D2B76B64BEB685630D7DB30F94EC267317CBF1685C7231F8017546C1`.
- Readiness TSV: `ACF4212182C6BE8F44D07FFE424B76D3C82EA7E1A411D9512040F25DFC6E547F`.
- Captura PPM: `E1D1FE0294EA554F601F384C15D87B3FEA4C4649BE93EC2527BC10CA8269AD15`.
- ISO preservada: `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Contexto estático de renderização no XBE — 2026-10-06

O audit somente-leitura `xbe_render_static_cluster_audit.py` encontrou `494`
referências, `88` literais distintos, `3949` contextos, `3917` contextos com
chamada executável e `854` destinos executáveis distintos relacionados a
D3D/renderização/geometria. O readiness registra
`rendering-static-xbe-context=verified-structural` (`88/1`).

É evidência de xref/coocorrência estática, sem execução do XBE e sem binding;
não altera o gate original `rendering=not-proven` nem interpreta mesh,
material, UV, skinning, rigging, animação, morph ou GPU.

Validação: Release `0/0`, self-test `72/72`, testes Python `12/12` e matriz
authored/candidate completa com runtime, replays, smokes e ISO preservada.

Hashes: literal audit
`8575E3823B9AE7A48BC81594832BF833B5532DE6F25A29B078924C774803CA06`;
clusters `FDA08E3F34F11FB4C4F9B17DCF8AD6FA2477D52CD30DF8E84831F356DA01DE59`;
resumo `DF4959A878D1B9A901C0EF70CBADE437F7116287BCA5368D0DAA4E11D4E820E3`;
readiness `651F94A67005984CB2D586A6D0FA0090BFCFBD722A37ED19F7E7DB8FCFABE3BE`.

## Cross-check textual XBE→XDVDFS — 2026-10-06

O auditor somente-leitura `tools/analysis/xbe_asset_literal_audit.py`
comparou literais de caminhos embutidos no `default.xbe` com a árvore de
arquivos XDVDFS da mesma ISO. O resultado foi `108` literais únicos (`702`
ocorrências), com `84` correspondências textuais (`81` por prefixo `data/` e
`3` por basename único). O resultado confirma apenas texto e presença do
caminho; não prova carregamento ou uso em runtime e não promove qualquer
binding de mesh, material, UV, áudio, evento, animação, rigging, skinning,
morph ou GPU. `status=verified-textual-xbe-path-crosscheck` e
`original_semantics=not-claimed`.

Relatório `fifa_pc/xbe-asset-literal-audit.tsv`, SHA-256
`95C13FC106CA4F24586AD972DA8A5189B05DE80F9FE5C45612FCD4E2334550AA`; resumo
`fifa_pc/xbe-asset-literal-summary.tsv`, SHA-256
`5F9731B918075E217BF63D66761228D2B43F10F4712482FB0BEB4D7B37739580`.
Comando reprodutível:

```powershell
python tools/analysis/xbe_asset_literal_audit.py "FIFA Street 2 (USA) (En,Es).iso" --report fifa_pc/xbe-asset-literal-audit.tsv --summary fifa_pc/xbe-asset-literal-summary.tsv
```

## Cross-check textual XBE→entradas BIG aninhadas — 2026-10-06

O auditor somente-leitura `tools/analysis/xbe_asset_nested_literal_audit.py`
validou os índices BIG de topo e percorreu entradas `.big` aninhadas usando
somente o RefPack limitado necessário para chegar aos índices. Foram
indexados `4.796` caminhos internos únicos e encontradas `13/108`
correspondências textuais (`10` únicas, `3` ambíguas), incluindo
`models/skel0000.ord` e `models/shdw0001.ord`. O resultado é evidência de
nome/índice e não prova de carregamento ou binding de mesh, material, UV,
áudio, evento, animação, rigging, skinning, morph ou GPU;
`status=verified-textual-xbe-nested-big-entry-crosscheck` e
`original_semantics=not-claimed`.

Relatório `fifa_pc/xbe-asset-nested-literal-audit.tsv`, SHA-256
`8570C2962A3BC5C7820111A8B56324D8E3DBD3784B4C81A6013EACD053A364CA`; resumo
`fifa_pc/xbe-asset-nested-literal-summary.tsv`, SHA-256
`9B0E93677A6D0CB2B30DB57D2907ECFF5274B26F18B7DF7BA2F8769C9348551C`.
Comando reprodutível:

```powershell
python tools/analysis/xbe_asset_nested_literal_audit.py "FIFA Street 2 (USA) (En,Es).iso" --report fifa_pc/xbe-asset-nested-literal-audit.tsv --summary fifa_pc/xbe-asset-nested-literal-summary.tsv
```

## Join estático XBE→BIG→xrefs — 2026-10-06

`xbe_nested_literal_xref_audit.py` cruzou as `13` correspondências BIG com os
xrefs estáticos e o contexto de chamadas locais do `default.xbe`: `8` literais
com xrefs exatos, `10` xrefs, `70` linhas de contexto e `24` destinos
executáveis distintos no conjunto ambíguo `stadium.big`. Isso é somente
coocorrência candidata de texto/endereço; não recupera funções nem atribui
loader, material, UV, mesh, áudio, evento, animação, rigging, skinning, morph
ou GPU. Status `verified-static-nested-entry-xref-join`;
`original_semantics=not-claimed`.

Relatório `fifa_pc/xbe-nested-literal-xref-audit.tsv`, SHA-256
`63418DEF893A32A5401D2993481CE11CB2A17215A7E19A860413181D11C4CEA2`; resumo
`fifa_pc/xbe-nested-literal-xref-summary.tsv`, SHA-256
`6B84A9CBA014F2148D3E290443C15A3DB2384BA09DDA9D1DB28F057E0279A409`.

## Clusters estáticos de áudio no XBE — 2026-10-06

`xbe_audio_static_cluster_audit.py` seleciona literais textuais de áudio e os
cruza com xrefs e chamadas diretas locais já observadas. A execução registrou
`58` xrefs em `23` literais, `147` contextos, `108` destinos executáveis e
`56` destinos distintos; `14` destinos coocorrem com mais de um literal.
Houve `1` correspondência BIG aninhada (`audio/tunavars.txt`) e `10`
correspondências XDVDFS. Não apareceu literal `.evt` no conjunto de xrefs;
isso é somente controle negativo textual, não uma conclusão sobre os dados
binários de eventos.

Relatórios: `fifa_pc/xbe-audio-static-literal-audit.tsv`,
`fifa_pc/xbe-audio-static-call-clusters.tsv` e
`fifa_pc/xbe-audio-static-summary.tsv`. Status
`verified-static-audio-literal-context`; a análise não recupera funções, não
seleciona evento/amostra/ABKC e não cria binding ou semântica original.

## Inventário dos payloads BIG nomeados pelo XBE — 2026-10-06

`xbe_nested_payload_inventory.py` seguiu as correspondências BIG em modo
somente leitura e registrou `41` caminhos concretos, `32` hashes distintos e
`15` payloads RefPack. A classificação estrutural observou `2` ELF 32-bit,
`2` sidecars ELF `.orl`, `1` BILG, `20` SHPX cru, `2` SHPX RefPack, `12`
índices BIG RefPack e `1` payload textual. Os pares `.ord/.orl` são apenas
proveniência e cabeçalhos; não são decoder nem binding de mesh, rig, material,
UV, skinning, animação ou GPU.

Relatório `fifa_pc/xbe-nested-payload-inventory.tsv`, SHA-256
`232235B37AACC2A943E1EFFFCBE853F99E3A3F281C5E16A705E0241665709BC9`; resumo
`fifa_pc/xbe-nested-payload-summary.tsv`, SHA-256
`668F8FCEEBDFA76CC2770B663A78DB7C9D413D3FAEE8570E57E28562401004C8`.
Status `verified-nested-payload-inventory`; `original_semantics=not-claimed`.

Comando reprodutível:

```powershell
python tools/analysis/xbe_nested_literal_xref_audit.py fifa_pc/xbe-asset-nested-literal-audit.tsv fifa_pc/xbe-string-xref-audit.tsv fifa_pc/xbe-string-xref-call-context.tsv --report fifa_pc/xbe-nested-literal-xref-audit.tsv --summary fifa_pc/xbe-nested-literal-xref-summary.tsv
```

## Readiness consumindo o subgate de payloads EVT — 2026-10-06

O `project-readiness-audit` passou a consumir o resumo do audit de runtime dos
EVT nomeados e agora registra `audio-named-event-runtime=verified-candidate`:
`17/17` candidatos, metadados, payloads preparados e cabeçalhos WAVE válidos.
O gate original `audio` permanece `partially-integrated` e falso, pois a
seleção direta gameplay→evento→amostra/ABKC continua `not-proven`; nenhuma
semântica original foi promovida.

Validação real: Release `0/0`, self-test `72/72`, matriz authored/candidate,
smokes, runtime headless e replays determinísticos passaram. Full-stack authored
gerado/replay coincide, SHA-256
`727F8A81A11F2F5798E6A5E2C7488C938C98DEF0A2CCC0E7179F1A0273D8072D`; audit
candidate SHA-256 `DEF0B77FEF42B1474E58ED06CEC471BDBDB1502741D2C6CF3DCD8F9D07E8E6EB`;
DLL Release SHA-256
`A17E88262121F0C0057DE43D01F205237016F491A1BB69908BA7582E62EC5975`.
O relatório full-stack também registra `opponent_ai_enabled=True`, e o
readiness verifica esse marcador como parte da integração authored; isso não
promove regras originais.
Readiness `NOT_READY`, SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
A ISO mantém `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Payloads EVT nomeados preparados no runtime managed — 2026-10-06

O `audio-named-event-runtime-audit` foi implementado e incluído na matriz.
Foram validados `17/17` candidatos EVT nomeados, preparados `17/17` payloads no
runtime managed, confirmados `17/17` cabeçalhos RIFF/WAVE e preparados
`2.914.952` bytes PCM. Relatório:
`fifa_pc/audio-named-event-runtime.tsv` (SHA-256
`1F4D60065756162D29E665DCB794D9D31F34FACC88DBD5FC38E16D70E8503CDF`); resumo:
`fifa_pc/audio-named-event-runtime-summary.tsv` (SHA-256
`DFA25F7E59B247C836879445140D152DA65E7613534013CC52E3B7B7057211D5`).

Status: `verified-runtime-named-payload`. Proveniência:
`exact-EVT-name+parallel-data-range; managed-runtime-named-payload-only`;
`original_semantics=not-claimed`. A etapa não estabelece gameplay event→sample,
seleção ABKC ou binding original; por isso `audio` permanece
`partially-integrated` e `NOT_READY`. Assets seguem candidate-only, rendering
segue not-proven e animation-deformation segue structural-only.

Release `0/0`, self-test `72/72`, smokes relevantes, runtime headless, matriz
authored/candidate e replays determinísticos passaram. Full-stack authored
gerado/replay coincide, SHA-256
`40C04455E9090663DE6F5ED3C950491B6D46043FD5BC472C56CC337B70B32B06`; DLL
Release SHA-256 `965FA17D8B58B56688351A3183B5EB41ED0DBA64F4E1F327D4669E7B1CC26A2F`.
Readiness: `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
A ISO mantém `2013003776` bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — completude dos streams candidate — 2026-10-06

O resumo de proveniência candidate agora registra completude dos streams de
comandos de geometria e dos streams de estado `SHAPENAME`. A matriz confirmou
`candidate_geometry_streams_complete=24` e
`candidate_shape_state_streams_complete=24`; o audit integrado exige ambos.
Esse avanço permanece candidate-only e não cria bindings originais de
estado→geometria, material, UV ou GPU.

Release `0/0`, self-test `71/71`, smokes relevantes, runtime headless e
replays determinísticos passaram. Full-stack gerado/replay coincide, SHA-256
`588AD564F93E4B2BB048FB121ABF9D0C76A310F69643DB4A0A0AE3AD7730FB49`;
relatório candidate SHA-256
`CB3364DC07F8FB90817BBB473F39D637256256AC210B6DDA747BC412B8B66863`, resumo
`00F678130853D38B3F2AB190B96F77B1CA850C81F13788ECF230B4BDC90A207D`, audit
integrado `22A7DC9360E35839C8581ECF1553A9418CFED637B0AE6DF38FC1426E00F48E3B` e
DLL Release `75137D91EECCAB5444E8FE40D5564F5FFEB95313639DAFDB1B982501819855CF`.
Readiness segue `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
A ISO não foi escrita e mantém SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — audit candidate consumindo proveniência estrutural — 2026-10-06

O `candidate-integrated-runtime-audit` passou a exigir o resumo de proveniência
candidate produzido antes dele na matriz. O resultado confirmou
`candidate_asset_provenance_valid=True`,
`candidate_mesh_state_crosscheck_valid=True` e `24/24` candidatos com objetos
`.ord`, companions `.orl`, bounds e geometria finita/indexada.

Release `0/0`, self-test `71/71`, matriz authored/candidate, smokes, runtime
headless e replays determinísticos passaram. Full-stack gerado/replay coincide,
SHA-256 `9569E64F059CFE2E1070448886786D19335996759054FD9B8277CF49327B071D`;
audit candidate SHA-256
`42A23A7F851D59A00FACE9E72C0AF28A65DA4F5FDCC5278FD283EABE228BC97E`; DLL
Release SHA-256
`43E2F4FB1C9E61F3734DF3FDACE9FA5CFD6A5FF40FB272A9CB41DFDA5AEA061A`.
Readiness segue `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
Nenhuma semântica original foi promovida; a ISO preserva SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — cross-check candidate de malha e estado — 2026-10-06

O auditor candidate foi ampliado para reler os objetos `.ord/.orl` e os
primitivos dos 24 vínculos same-trunk. O resultado real foi `24/24` objetos,
`24/24` companions, bounds válidos, geometria finita/indexada e
`SHAPENAME` presente nos primitivos em `24/24` casos. Relatório SHA-256:
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo:
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.

Release `0/0`, self-test `71/71`, matriz authored/candidate, smokes, runtime
headless e replays determinísticos passaram. Full-stack gerado/replay coincide,
SHA-256 `5B42B2CCB1A548D0B6EDA60DF1E0DD05B4D73E06CDF9C287CE24165555B02A73`;
DLL Release SHA-256
`77921A6EFF6715DD13B808C489D75FB465B351FE3D8FB84466DC00599F3FA05C`.
Readiness `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
O cross-check é candidate-only, mantém `original_semantics=not-claimed` e não
atribui estado→geometria, material, UV, GPU, skinning, rigging ou animação.
ISO preservada: SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Cross-check persistido de imagem SHPX — 2026-10-06

O novo `shpx-export-crosscheck` confirmou `46/46` imagens PAL8 128x128 de
`data/aptanims/player1.xsh` contra as TGAs persistidas no workspace: `46/46`
arquivos presentes, cabeçalhos válidos e pixels RGBA idênticos. Relatório:
`fifa_pc/shpx-export-crosscheck.tsv`, SHA-256
`8EB31145E1A05CDED83C3DEA703BD1A9B24755260A650F672FC34545118AC68E`; resumo:
`fifa_pc/shpx-export-crosscheck-summary.tsv`, SHA-256
`B126B7D0AEA480A299F207BC6F688D82B1A95D769EEFFB58F19F063DDC3FDD4F`.

O resultado é uma verificação de bytes/pixels de exportação gerada pelo próprio
workspace (`workspace-generated-TGA; provenance-not-independent`). Ele não é
evidência independente e não altera `NOT_READY`, nem cria bindings de timing,
pose, rigging, skinning, animação, morph, material, UV, GPU ou semântica
original.

A validação final da etapa passou em Release `0/0`, self-test `71/71`, matriz
authored/candidate, smokes relevantes, runtime headless e replays determinísticos.
Os relatórios full-stack gerado/replay coincidem, SHA-256
`E65191996143C3F04C856473EB23000E0CD8C96BE936B70D810BEA308C7FCA2C`; o DLL
Release tem SHA-256
`1F02DE91890B648AAF5605FAE605A2AE20149CEA37613EA493A0892F716E33CF`.
Readiness: `NOT_READY`; ISO preservada com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Proveniência da cadeia candidate EAGL/SHPX — 2026-10-06

O novo `eagl-candidate-asset-provenance-audit` reabriu os 24 vínculos
same-trunk em `data/models.big` e verificou `24/24` entradas `.big`, `24/24`
`.xsh`, `24/24` payloads SHPX, `24/24` entradas internas exatas e `24/24`
decodificações RGBA idênticas. Relatório SHA-256:
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo:
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.

O cross-check estrutural adicional confirmou `24/24` `.ord`, `24/24` `.orl`,
bounds válidos, geometria finita/indexada e `SHAPENAME` presente nos primitivos
em `24/24` candidatos. Isso é evidência de leitura estrutural da representação
managed candidata, não associação estado→geometria nem semântica original.

Esse é um gate de proveniência candidate, não um gate semântico original:
`candidate_provenance=same-trunk-source-chain; candidate-only` e
`original_semantics=not-claimed`. O readiness geral continua `NOT_READY`.

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

## Auditoria adicional — referências estáticas de strings no XBE

Foi adicionada `tools/analysis/xbe_string_xref_audit.py`, uma análise
somente leitura do `default.xbe` armazenado na ISO. Com Capstone 5.0.9 no
ambiente de análise, o relatório encontrou 1.929 strings literais de
interesse e 944 referências x86 candidatas em 944 instruções de origem
(939 operandos imediatos e 5 operandos de memória absoluta). As referências
incluem literais observados para áudio, `.xsh`, `.big`, `D3DTexture` e
`EAGLAnimationBuffer`.

Artefatos: `fifa_pc/xbe-string-xref-audit.tsv` e
`fifa_pc/xbe-string-xref-summary.tsv`; SHA-256 do XBE:
`5fabb5b763be0cc8823e814ee343ac5be6950305330f46c5170e08eaadea910b`.
O resultado é `candidate-x86-reference-only`: não executa o XBE, não define
limites de função e não prova qualquer binding de materiais, UVs, skinning,
rigging, animação, morph ou GPU. O gate de prontidão permanece `NOT_READY`.

O refinamento `--call-context` cruzou essas referências com chamadas diretas
somente dentro de uma janela local de 128 bytes: 6.251 linhas, 790/944
referências com chamada próxima e 1.141 destinos executáveis distintos. O
artefato é `fifa_pc/xbe-string-xref-call-context.tsv`, SHA-256
`DF778BC8F3620A730F201842CCAE653FA0249BD94A819DC04A772250317927F0`.
Esse contexto continua candidato; não define funções, subsistemas ou
bindings e não altera nenhum gate.

O teste de análise `python -m unittest tools.tests.test_fifa_analysis -v`
passou `6/6`; o resumo atualizado tem SHA-256
`19A633D47CC2FA5CE30D800AE6C52ABD63CDF785546F31DC190A892E72A862BD`.

## Refinamento posterior — clusters locais de chamadas do XBE — 2026-10-06

O mesmo auditor estático ganhou `--call-clusters`, que agrupa somente por
endereço de destino executável dentro do contexto local de 128 bytes. Foram
materializados 1.141 clusters e 306 com pelo menos duas strings distintas em
coocorrência. Relatório: `fifa_pc/xbe-string-xref-call-clusters.tsv`, SHA-256
`9A5F416ED66485B2D4E0D2087E3E400711627AA0352CCAB9E6194EC818AF4CF3`; resumo
atualizado: `fifa_pc/xbe-string-xref-summary.tsv`, SHA-256
`19A633D47CC2FA5CE30D800AE6C52ABD63CDF785546F31DC190A892E72A862BD`.

O teste Python passou `6/6`. Os clusters são apenas coocorrência local: não
recuperam funções, não nomeiam subsistemas e não criam bindings de assets,
materiais, UVs, skinning, rigging, animação, morph ou GPU. `NOT_READY` permanece.

Após essa auditoria, a validação final repetiu Release `0/0`, self-test
`69/69`, matriz authored/candidate completa, smokes relevantes, runtime
headless e replays determinísticos com `matched=True`. Os relatórios full-stack
gerado/replay permaneceram iguais, SHA-256
`05B6A34950B6D9462DAF6561CC03041D19FB5203A8E401A2693F054D9AD3281F`; a ISO
permaneceu em `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Bateria final verificada da etapa — 2026-10-06

Release sem avisos/erros e self-test `68/68`. A matriz executou nove cenários
authored e um cenário candidate opt-in; os dez pares de replay determinístico
de match, input, animação e áudio retornaram `matched=True`. O cenário candidate
teve 140 frames, 280 samples de animação, 8 eventos de áudio, 8/8 preparados/
tocados e 0 sem rota. Os relatórios de integração são byte a byte iguais,
SHA-256 `24561111E7BCE6CE2A11171F0960F048FA57076677C8CCB432A3209CF1CA257B`.

O gate agregado `clean-room-authored-runtime` foi verificado em `9/9`; a
auditoria gerenciada candidate está `verified-candidate`. O readiness permanece
`NOT_READY`, com TSV SHA-256
`A23E61B9474FBD07E409F0D2ED2B08CD5DC94E884429F25363CAD6289DA35222`. A ISO
permanece intacta: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

A reprodução da etapa está automatizada em
`D:\projeto zeta\tools\run-clean-room-authored-matrix.ps1`: ela gera e
reexecuta os nove cenários authored e o cenário candidate, roda as auditorias,
os smokes relevantes e readiness, e
verifica que tamanho, timestamp, atributos e SHA-256 da ISO não foram
alterados.

O relatório `fifa_pc/candidate-integrated-runtime-audit.tsv` tem SHA-256
`9ABDABC1246F4D161A498609F698CC3AB05FBFB10315BF2F81C08EF995E06A6D`.
Ele verifica flags candidate, hash do assembly, traces, replays e PPM como
evidência do runtime managed; `original_semantics=not-claimed` permanece
explícito e nenhum binding original de material, UV, skinning, rigging,
animação ou GPU foi atribuído.

Nesta etapa, o readiness passou a exigir esse audit gerenciado somente no gate
separado `assets-candidate-runtime`, que está `verified-candidate` (`24/24`,
`passed=True`). Os gates originais `assets` e `rendering` continuam sem
promoção, respectivamente `candidate-integrated`/`False` e `not-proven`/`False`.

## Avanço verificado — intake de timing externo — 2026-10-06

O novo `external-animation-timing-audit` valida apenas um TSV de observações
temporais externas explicitamente fornecido. Ele registra hash, proveniência,
ordem monotônica dos samples e repetição por par banco/asset, sem atribuir
semântica a frame, pose, rig, skinning ou aos payloads originais.

Não há esse TSV externo no workspace atual; a execução retornou
`status=missing-evidence`, `0` observações e `0` capturas. O relatório tem
SHA-256 `3E82F4FF84E4095CA2844950A97A56FB5F165954BF3B3379A2BD4ED0DC026EDC` e
o resumo `4FF80AD21FB2848191CC38519BE7E6BA9E86E3B34C23FA259879B8908836928E`.
`direct_timing_binding=not-proven` e `original_semantics=not-claimed` ficam
explícitos; o gate de animação/deformação não foi promovido.

## Avanço verificado — integração candidate opt-in — 2026-10-06

A matriz agora executa um cenário cross-system separado com
`--candidate-material` e ambos os slots candidatos. Match, input,
animação e áudio foram reexecutados com `matched=True`; o caso teve
140 frames, 280 samples, 8 eventos, 8/8 preparados/tocados e 0 sem rota. O
relatório confirmou material validado, home/away candidatos habilitados,
checksum `088EC5D10096A738` e 69 checksums distintos.

Os relatórios de integração coincidem byte a byte, SHA-256
`F8F03C7DC83BFD33EAD3D58526C1B19A1790A44209C7BF938819F55AB754187A`.
O caminho continua candidato/opt-in: `render_group_state=unproven`,
`original_semantics=not-claimed`; nenhum binding de material, UV,
skinning, rigging, animação ou GPU original foi inferido.

## Avanço verificado — auditoria de metadata temporal dos catálogos AXT — 2026-10-06

`animation-bank-catalog-timestamp-audit` comparou o timestamp numérico com a
data textual da própria linha de catálogo. O resultado foi `19/19` bancos e
`1.584/1.584` linhas exatas, com offsets locais inferidos `-420` e `-480`
minutos. É correspondência de metadata do catálogo, não evidência de duração,
frame, pose, rig, skinning ou timing de animação.

Relatório SHA-256:
`611B60012B5F37A639E2DC5AD47E829A6178FEA409A9D6CE4CA1360640222C32`; resumo:
`E167D7254E82C36D6A49276B2721A828FBD52EA048CBADAFEBCE6E33959D37FE`. O
Release DLL usado pela matriz tem SHA-256
`D0660B1FEF4F2781152EDB95797145201F99FA4A2E0780B914CF031F0713E459`. O
readiness regenerado tem SHA-256
`282D82CC21E3EE600D6DCDFF6A95B754579AB9975D2F02F661BCB039F4DC1339` e
continua `NOT_READY`, com `direct_timing_binding=not-proven`.

## Avanço verificado — correlação exhaustive de áudio — 2026-10-06

O comando `audio-event-payload-wave-correlation-exhaustive-audit` percorre
todos os índices de som candidatos dentro do limite `512` por banco. Foram
observados `368` candidatos em `16` bancos e `239` decodificados; não houve
pares PCM idênticos nem hashes de evento correspondentes (`0/0`). A conclusão
é estrutural/negativa e não cria binding de evento, sample, gameplay ou
semântica original.

Relatório SHA-256:
`BDE7CF7E58E09E17D2BBB8932E413249D51264424E5A1C32E8B5E30D404E345E`; resumo:
`DD798636EC35EF78E07E47176A0220C9A919601F2FF47D5CD51FE8DB41DEF23B`.
O readiness registra essa evidência como `verified-structural-negative`, com
`direct_selection_binding=not-proven`.

## Avanço verificado — invariantes de gameplay authored — 2026-10-06

O novo auditor `clean-room-gameplay-invariant-audit` verificou `9/9`
cenários authored determinísticos, `677` frames e `49` eventos. Ele exige
estado finito, posições e altura da bola dentro dos bounds, relógio e placar
monotônicos, sequência de eventos coerente, término esperado e ações
requeridas presentes. Status: `verified-authored`.

Relatório `fifa_pc/clean-room-gameplay-invariants.tsv`, SHA-256
`B6C4F29036835A22BFB36D6401F2C31A37B6C66DC62D3EB477A8C5396C6433EE`.
Esse resultado é authored do runtime novo e não decodifica regras originais;
`original_semantics=not-claimed` permanece.

## Avanço verificado — correlação estrutural de áudio ampliada — 2026-10-06

`AudioEventPayloadWaveCorrelationAudit` passou a aceitar
`--max-bank-attempts`. A medição com limite de `64` candidatos de banco
decodificou `17/17` candidatos EVT e `15/16` bancos ABKC/AST; encontrou `0`
pares PCM idênticos e `0` hashes de evento com correspondência. A política
ficou explícita como `first_64_bank_candidates`.

O relatório tem SHA-256
`8ED4E39C6748CEC54706D9F107DCFE7D887C941ED43FDF854829C60568104E3D` e o
resumo tem SHA-256
`4D949566DB53FA348C939388D280D94C9F825BB19BB19BA6B31FAA70C2C2A38D`.
O resultado continua `verified-structural`: não prova seleção de evento,
sample ou banco; `direct_selection_binding=not-proven` e
`original_semantics=not-claimed` permanecem.

## Avanço verificado — cenário first-to-goals integrado — 2026-10-06

O cenário authored `first-to-goals` foi acrescentado ao smoke cross-system:
`80` frames, vitória `HOME 1 - 0`, `FULL_TIME`, `7` eventos de match/áudio e
`160` samples de animação. Os quatro replays foram `matched=True` e o frame
PPM authored foi validado.

Os relatórios de integração gerado/replay têm SHA-256
`D37356F7DE6F7EE9F04FFB83BC1D5973F6C278C1F3EAAC9C59D3D98A3089F119`.
As auditorias de gameplay e áudio authored passaram `9/9` cenários,
`49/49` eventos e `0` sem rota; isso não altera os gates semânticos originais.

## Avanço verificado — cobertura integrada authored por cenário — 2026-10-06

Foi criado `fifa_pc/Runtime/CleanRoomIntegratedCoverageAudit.cs` e o comando
`clean-room-integrated-coverage-audit`. O auditor exige que cada um dos nove
cenários authored tenha renderização, personagem/cena authored, animação,
áudio e traces/replays determinísticos no mesmo relatório de runtime. Ele não
atribui semântica à ISO.

O resultado é `9/9` cenários, `677` frames, `1.354` samples de animação e
`49` eventos de áudio preparados/tocados, com todos os replays coincidentes.
O relatório tem SHA-256
`DC6E37059884B9AC450D4FD73738D9D294EC8CE1A08C707CCD49AA61EC876A7B`.
Cada um dos nove cenários também possui um PPM authored `960x540` válido, com `341.782`
pixels visíveis no frame final; isso valida somente o framebuffer managed.
Release `0/0`; self-test `65/65`; o full-stack authored-only gerado/reexecutado
é byte a byte igual, SHA-256
`BF2F85A42960ED5DCA0E1C84DB4481BAA66749263C31FDA6708C51224C67589A`.
Cada integration report registra o SHA-256 do assembly Release atual:
`9CF41DD8555721A043BF58C6456755D2ABA3970EE417D73653EB0006775F37BB`.
Readiness `NOT_READY`, com o novo gate authored verificado, TSV SHA-256
`EDE51254C0D754EF46463183ADE17FCA6104B941E06032506BA96535494983AC`.

Os gates de assets/renderização, gameplay, áudio e animação original continuam
sem promoção; `original_semantics=not-claimed`.

## Avanço verificado — manifesto de personagem authored — 2026-10-06

`fifa_pc/Runtime/CleanRoomCharacterAssets.cs` formaliza
`clean-room-character-manifest-v1` para os IDs authored de silhueta e paleta.
O renderer valida o manifesto, que cobre somente assets criados no runtime
managed; não é binding de mesh, material, UV, skinning, rigging, morph,
animação ou GPU original.

Release: `0/0`; self-test: `62/62`. O authored-only de 140 frames registrou
`authored_character_asset_frames=140`, checksum `F26A04CC3CBEBDEA` e relatório
SHA-256 `C0969EFA056C678494D70E97941A6EB6D1748E448F640FD2F84B4291F1900F33`.
O full-stack candidato manteve checksum `088EC5D10096A738`; match/input/
animação/áudio reexecutaram com `matched=True` e os relatórios são byte a byte
iguais, SHA-256 `BF9FA14EB4CC5EC1580535EF05D4F97AA0F256278FA9E7CE3311F71E5EE82DE8`.
O readiness continua `NOT_READY`, com `character-authored-assets` verificado,
SHA-256 `6C86A5895D66F822D0E06A2A41C89F04C37001FA0B829DE132431BC08B507330`.
ISO: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Os smokes de 80 frames e preview authored passaram; o smoke EAGL continua
evidência neutra. A validação de candidatos de áudio aceitou `139` faixas e
rejeitou `144` envelopes `SCCl` de formato inválido, sem promover seleção
EVT/ABKC/sample.

## Avanço verificado — manifesto de animação authored — 2026-10-06

Foi adicionado `fifa_pc/Runtime/CleanRoomAnimationAssets.cs` com o manifesto
`clean-room-animation-manifest-v1`. O animador managed resolve perfis authored
para as ações já implementadas, mantendo `Unknown` sem rota. Isso é uma camada
de organização do runtime novo; não é decoder de AXT e não cria evidência de
frames, timing, pose, rigging, skinning ou morphs originais.

Release: `0/0`; self-test: `60/60`; headless de 80 frames com `6/6` eventos de
áudio authored; full-stack de 140 frames com checksum
`088EC5D10096A738`; replays determinísticos de match/input/animação/áudio com
`matched=True`. Os relatórios de integração são byte a byte iguais, SHA-256
`70DE5BAB17EDE85DDF847F8C1D8B5185E944AD4051AFFF534CE2AAE4B93AAA83`.
Os smokes authored adicionais passaram para `JUGGLE`, `STEAL` e
`GAMEBREAKER`, incluindo os perfis de ação fora do demo padrão.
Readiness: `NOT_READY`, SHA-256
`CE868EA45F8CA772764D1BA8296A7BD7200EA2575150D572084DA0F887A8D153`.
ISO: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

O novo gate `animation-authored-assets=verified-authored` confirma apenas o
manifesto authored. `animation-deformation` continua `structural-only` e os
gates originais de assets, rendering, gameplay e áudio não foram promovidos.

## Avanço verificado — manifesto de áudio authored — 2026-10-06

`fifa_pc/Runtime/CleanRoomAudioAssets.cs` formaliza
`clean-room-audio-manifest-v1` para os oito candidatos de cue da política
authored. O mixer só prepara um candidato nomeado depois da validação desse
manifesto. Isso organiza assets do PC novo; não decodifica seleção original
EVT/ABKC/sample.

Full-stack: `8/8` preparados/tocados, `0` sem rota, PCM de `1.180.164` bytes e
SHA-256 `2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`.
Release `0/0`, self-test `61/61`, replays match/input/animação/áudio com
`matched=True`. Relatórios de integração iguais, SHA-256
`4DD380D1896481F5EEE4CACF1E46E1D3E4B2BF5CF5B42847C6FF924174B2B0F0`.
Readiness `NOT_READY`, SHA-256
`F485A2845003F98867ECCF31496761971AE6C574B06CA9EDACEF19BDEE471327`.

O gate `audio-authored-assets` é `verified-authored`; o gate geral `audio`
continua `partially-integrated` porque a seleção original permanece
`direct_selection_binding=not-proven`.

## Avanço verificado — correlação estrutural de áudio e bateria final — 2026-10-06

A auditoria `audio-event-payload-wave-correlation` validou a decodificação de
`17/17` candidatos EVT e `15/16` bancos candidatos ABKC/AST na política limitada;
`exact_pcm_hash_pairs=0`. Isso é um controle estrutural negativo e não prova
seleção de evento, sample ou semântica original. SHA-256 do relatório:
`00251215E62B4437C5B0EE3917C12E8DF66C1E278F1982EAF3AE95232E0432BD`; resumo:
`C5AEC72F58CAC11EFD2942D6F598C3401A5433475E4A3C7EF728D7893E6448F4`.

Build Release: `0` avisos/erros; self-test: `59/59`; smokes de textura/material,
headless de 80 frames e full-stack de 140 frames verificados. Os replays de
match/input/animação/áudio coincidiram byte a byte. O readiness continua
`NOT_READY`, SHA-256 `4982801DDA56466467E7DC6197BDB22EF481EE41BFF5FE07F971BDB27B8B672D`.
A ISO permaneceu sem escrita, com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço verificado — digest PCM do áudio authored — 2026-10-06

O caminho managed de áudio passou a registrar o PCM concatenado das cues
authored entregues ao gameplay, sem o header WAV. O full-stack verificou
`1.180.164` bytes e o digest
`2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`; o replay
foi idêntico. Os relatórios de integração têm SHA-256
`706E895FE9BAFD6A34BF6FB5C74A4C93110F1B6ECE193C18E8C625CD74C57823`.
Essa é evidência do runtime authored, não da seleção original EVT/ABKC.
Self-test: `59/59`; readiness: `NOT_READY`, SHA-256
`336D91B1DB56E0BDF32C96BC0F093E707DA63EA04C7D06302AF3EB1A4CDBDFE9`.

## Avanço verificado — manifesto de assets authored da cena — 2026-10-06

O renderer managed passou a consumir o manifesto authored
`clean-room-court-v1|clean-room-goal-v1|clean-room-ball-v1`, implementado em
`fifa_pc/Runtime/CleanRoomSceneAssets.cs`. Quadra, gols e bola foram verificados
em `140/140` frames, com checksum de renderização preservado em
`088EC5D10096A738`. Os relatórios de integração são byte a byte iguais, SHA-256
`CA03EC0029301E7336E0CA839DAE717A60260F2F130584E6C5051AA0AA8E72FD`.
Readiness: `NOT_READY`, SHA-256
`661F2013630F999E2A79E27682A64253E7DA671D946ADD183B8E0FA0E22976E1`.
Essa camada authored não é binding de renderização original.

## Avanço verificado — formatos XPR0 — 2026-10-06

`XprTextureDecoder` agora cobre, em código managed, os formatos de payload
reconhecidos pelo header `XPR0`: `R5G6B5`, `A8R8G8B8`, `X8R8G8B8`, `DXT1`,
`DXT3` e `DXT5`. O decoder limita-se ao nível base disponível e converte os
bytes para RGBA; isso é cobertura de formato, não prova de associação a
material, UV, estado de renderização ou comando GPU original.

No catálogo real, as duas texturas XPR0 continuam validadas como
`titleimage.xbx` (`128x128`, `DXT1`) e `saveimage.xbx` (`64x64`, `DXT1`). Os
dois previews foram reexecutados em Release, sem escrita na ISO. O build
terminou com `0` avisos/erros e o self-test com `58/58`. O readiness continua
`NOT_READY`; a ISO preserva o SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço estrutural — âncoras de payload de animação — 2026-10-06

O auditor `AnimationBankCatalogPayloadAnchorAudit` mede os offsets das duas
strings técnicas `_e/_r` por linha do relatório de nomes. A execução observou
`19/19` bancos, `1584/1584` linhas completas, `3168` âncoras, `1584` gaps
intra-nome, `1568` gaps entre linhas e ordem crescente em `19/19` bancos.
Esses números são somente layout/ordem observados; não são boundaries de
records e não criam semântica de frame, pose, rig, timing ou skinning.

O relatório tem SHA-256
`B63D77DF1E99F08576829FB016A1DEE5CB4C71F97F92E75E442826A36E481FBD` e o
resumo `372E58900EF0F173652F923DD3F49BC777B76006437D53675D91C662626E0ECA`.
O readiness continua `NOT_READY`, com SHA-256
`C733311CF7340C1A8B5E0C8170B2534BFAB7F0CAA30426B810FEFDB3B85F3198`;
self-test `58/58`.

## Restrição fundamental

O material disponível contém a imagem Xbox e o `default.xbe`, que é código de
máquina x86 já compilado. Não foram encontrados arquivos de fonte, projeto de
build, símbolos completos ou dependências originais. Um compilador .NET não
consegue recompilar um XBE diretamente para IL/C# sem uma etapa de
descompilação e reconstrução manual.

## Estratégias separadas

- **Recompilação real:** exige o código-fonte e os arquivos de build. É o
  caminho solicitado pelo usuário.
- **Reimplementação clean-room:** usa o XBE apenas como especificação observada
  e produz código novo; não é uma recompilação do original.
- **Emulação/compatibilidade:** executa o código de máquina com CPU e APIs
  simuladas; não será usado como produto final neste projeto.

O `pc_compat/Runtime/X86BootProbe.cs` permanece somente como instrumento de
diagnóstico reversível. Ele não faz parte de uma entrega de recompilação.

## Avanço verificado — personagem authored separado — 2026-10-06

O runtime .NET ganhou `fifa_pc/Runtime/CleanRoomCharacterRenderer.cs`, um
asset procedural authored de silhueta/paleta. Essa apresentação é código novo
e permanece separada de qualquer hipótese sobre mesh, material, UV, skinning,
rigging, morph, animação ou comandos GPU da ISO.

O caminho authored-only foi exercitado por `140/140` frames, com checksum
`243EF331249601A5` e `authored_character_asset_frames=140`; o relatório
`fifa_pc/gameplay-authored-character-integration.tsv` tem SHA-256
`78FEC9A56961C0EC35DC3024DB81157451B4FA4B62EC094C0AF61F26219CCA5E`.
O full-stack com candidatos permanece uma medição independente, com checksum
`088EC5D10096A738` e contador authored igual a zero enquanto os slots candidatos
estão ativos.

O build Release fechou com `0` avisos/erros e o self-test com `58/58`.
O readiness permanece `NOT_READY`, SHA-256
`0C537BBEA1140423C73F39A4B4EEC32B8292D630065B3F7A5876767867B25A1A`.
O coverage authored permanece `8/8` cenários, `42` eventos e `14` ações;
os quatro replays full-stack retornaram `matched=True`. A ISO continua sem
escrita, tamanho `2013003776`, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço estrutural — nomes de catálogo no payload — 2026-10-06

Foi adicionado o auditor `animation-bank-catalog-payload-name-audit`. Ele lê
somente os pares `.ord/.orl` e compara os nomes `.axt` com strings ASCII do
`.data`, preservando os offsets observados. O resultado foi `19/19` bancos
validados e `1584/1584` nomes encontrados como pares técnicos `_e`/`_r`, com
`0` ausências. A evidência confirma identidade estrutural catálogo↔payload,
mas não decodifica record boundaries, frames, poses, rig, timing ou skinning.

O relatório detalhado tem SHA-256
`3D2CD9FBA876636011CFED8876204C1BC6F84A23334F8A06D42CA276671F4900`; o resumo
tem `FAE1691C64C4A99B968F7DFC8F349368892D924F10C44CBF5315178BEC3A673D`.
O readiness atualizado, ainda `NOT_READY`, tem SHA-256
`0C537BBEA1140423C73F39A4B4EEC32B8292D630065B3F7A5876767867B25A1A`.

## Gate de prontidão da reimplementação

O estado verificável está reunido em `fifa_pc/project-readiness.tsv`, gerado
por `project-readiness-audit`. A leitura atual é `NOT_READY`: a ISO preserva o
SHA-256 esperado e os assets/áudio já têm caminhos candidatos executáveis, mas
renderização final, animação/deformação, regras de gameplay mais amplas e
seleção geral de áudio ainda não têm evidência suficiente para serem chamados
de integrados. A matriz mantém explícitos os estados `candidate-only` e
`structural-only`; nenhum campo vira binding de UV, material, skinning,
rigging, timing ou GPU por inferência.

Atualização de rastreabilidade em 2026-10-06: o readiness passou a expor
também linhas separadas para o runtime authored/candidato já exercitado. O
estado real é `assets-candidate-runtime=verified-candidate` (`24/24`, `0`
rejeições), `rendering-authored-runtime=verified-authored` (`140` frames, `69`
checksums distintos), `gameplay-authored-runtime=verified-authored` (`8`
eventos), `gameplay-authored-coverage=verified-authored` (`8/8` cenários, `42`
eventos, `14` ações), `audio-authored-runtime=verified-authored` (`8/8` preparados/tocados,
`0` sem rota) e `animation-authored-runtime=verified-authored` (`280` amostras).
Essas linhas são evidência do runtime managed e dos caminhos candidatos; não
alteram os gates semânticos `assets`, `rendering`, `audio` ou
`animation-deformation`, que continuam respectivamente candidato, não provado,
parcial e estrutural. O readiness geral continua `NOT_READY`, e
`original_semantics=not-claimed` permanece obrigatório.
O relatório atual `fifa_pc/project-readiness.tsv` tem SHA-256
`75A8E6DC7B88729B92FBA635D26819738EF651F69F0964BF92E5696D85C9F6EC`.
O relatório `fifa_pc/gameplay-coverage.tsv` tem SHA-256
`E8655638CFE171E10A14DA5B7C7CBFC6C5C3FFED2B3D879BE10F42ADA96C996D`.

Atualização de integração authored em 2026-10-06: o relatório
`CleanRoomIntegrationReport` passou a registrar também o input consumido pelo
runtime. A execução Release com material candidato nos dois slots produziu
140 frames, 140 samples de input, 8 eventos de partida e 8 eventos de áudio;
foram preparados/tocados 8/8 cues e houve 0 eventos sem rota. Os replays de
match, input e áudio retornaram `matched=True`, e os relatórios de integração
gerado/reexecutado são byte a byte iguais. O SHA-256 dos dois relatórios de
integração é
`322EDD999DBBA2CBE5952A934DC85B60640A9930D7D2458B811C60474FE90376`.
Isso comprova somente a integração authored do novo runtime PC. Não é prova de
binding original de input, estado EAGL, UV, material, skinning, rigging,
animação, GPU ou seleção de áudio.

Na mesma bateria, o build Release terminou com 0 avisos/erros, o self-test
passou `54/54`, o headless padrão de 80 frames produziu
`5405DE72BE1B4FC1`, e a cobertura authored de áudio confirmou `8/8` cenários,
`42/42` eventos preparados/tocados, `0` sem rota e `14/14` ações. Os smokes de
material/runtime e geometria confirmaram `24/24` linhas válidas; o smoke de
influência confirmou `20` influências raster candidatas. O controle de
correspondência encontrou `0` pares de estado exatos e `0` símbolos de
relocação compartilhados, portanto o gate de renderização permanece
`not-proven`. O readiness segue `NOT_READY`, com SHA-256
`4C1AD42C9CA32E08ECFBCC374C73294A8367B7D9202EA3A1A471A546531097F9`.

Integração authored de apresentação em 2026-10-06: o runtime passou a
serializar a pose procedural usada pelo renderer em
`CleanRoomAuthoredAnimationTraceSerializer`. O full-stack produziu 280
amostras em 140 frames (home e away por frame), e o replay retornou
`matched=True`; o SHA-256 dos dois traces é
`57C124E9CDD044CD8DE0C2319C9AD11FC35930E0589C199B87C4DB0007342D09`.
O relatório de integração agora registra `animation_samples=280` e
`animation_trace_written=True`. Isso comprova a execução determinística da
animação procedural authored do PC, não um decoder de AXT/morph nem binding de
pose, rig, skinning ou timing original. Por isso o gate
`animation-deformation` continua `structural-only`.

Auditoria bounded de áudio repetida em 2026-10-06: o decoder nomeado validou
17/17 candidatos EVT; a auditoria SCCl validou 144/144 layouts mono únicos;
o smoke ABKC decodificou 15/16 bancos, com um banco sem candidato bounded.
Uma busca adicional de palavras alinhadas não encontrou ocorrências em
`pcpahdr.big`, `pcpadat.big` ou nos 16 bancos. Isso fortalece somente a camada
de payload e o controle negativo; a seleção original evento→sample continua
`not-proven`, e a política authored do PC não é promovida a binding original.

Atualização estrutural de 2026-10-06: o comando
`animation-morph-relocation-profile-audit` compara somente conjuntos de gaps
de relocação observados em relatórios separados. No estado atual, 393/393
layouts morph foram parseados; 53 layouts tiveram algum dos 19 bancos com
contenção do conjunto de gaps, 0 tiveram conjunto exatamente igual e foram
observados 159 pares de contenção. A saída é controle de recorrência opaca,
não associação morph→animação e não fornece semântica de frame, pose, rig,
timing, skinning, material, UV, GPU ou comando. O gate continua
`animation-deformation=structural-only` e o readiness continua `NOT_READY`.

O `project-readiness-audit` passou a consumir o resumo TSV desse controle e a
copiar seus contadores para a nota do gate de animação/deformação. Isso torna
explícita a proveniência do relatório (`393` layouts parseáveis, `159` pares
de contenção e `0` igualdades exatas), sem promover a recorrência sintática a
binding ou decoder.

O manifesto único da política authored de áudio está em
`fifa_pc/Runtime/CleanRoomAudioCuePolicy.cs`; mixer, self-test e readiness
consomem a mesma lista de 14 rotas. Trata-se de comportamento novo do PC, não
de uma tabela de eventos recuperada.

O readiness atual confirma `14/14` rotas authored com candidato nomeado
utilizável e `8` cues únicos no catálogo, sem alterar a classificação parcial
do áudio.
Ele também registra `authored_audio_trace_replay=verified` para o par combinado
de 8 eventos, com `KICKOFF -> intro` e todas as rotas tocadas, e
`authored_steal_audio_trace_replay=verified` para o par de 2 eventos do
cenário `STEAL`. Isso é repetição textual do runtime authored; não é seleção
original de EVT/ABKC.

Correção de entrega authored verificada em 2026-10-06: `GameRuntime` e o host
Win32 passaram a iniciar o cursor de áudio em `-1`, preservando o evento
inicial de sequência `0` (`KICKOFF`). O cenário combinado de 140 frames agora
produz `8/8` cues preparados/tocados, `0` eventos sem rota e checksum
`2641FCCE8245A44B`; o `gameplay-audio-combined-trace.tsv` tem 8 eventos e o
replay com `--audio-trace-expected` retornou `matched=True`. A política e o
trace são authored clean-room; isso não promove a evidência de áudio para
seleção original de EVT/ABKC. Os dois arquivos de trace de áudio têm SHA-256
`FEBA7B454A0A82C33A3E2128FBC622A3C6AF957D1FB83CB6AB7DF156EBB6D07C`.

Foi acrescentado o relatório `CleanRoomIntegrationReport`: a execução
cross-system com material candidato nos slots home e away, gameplay authored,
oponente e WinMM produziu 140 frames, 8 eventos, `8/8` cues preparados/tocados,
0 sem rota e checksum `088EC5D10096A738`. O relatório e sua reexecução foram
comparados byte a byte; `distinct_render_checksums=69` foi observado nos 140
frames. Isso comprova variação da apresentação authored durante o runtime, não
pose, rigging, skinning ou animação original; `original_semantics=not-claimed`
permanece explícito.
Após a inclusão do contador de variação, os dois relatórios de integração
continuaram byte a byte iguais com SHA-256
`F0329A2C5189CC326D9099783FC771C16BD8227B48B71DB989D27E9DB3E2DE8F`; o
`project-readiness.tsv` atual tem SHA-256
`426855D566E382F940742062FA7F39C3854FE1DBEE842CF045C042BB10D5CCFB`.
O SHA-256 igual dos dois TSVs de integração é
`F0329A2C5189CC326D9099783FC771C16BD8227B48B71DB989D27E9DB3E2DE8F`.
O `project-readiness-audit` agora expõe esse resultado como o gate separado
`full-stack-integration=verified-authored`; ele é um gate de integração do PC,
não uma promoção dos gates `rendering`, `assets` ou `audio` para semântica
original.

Atualização authored de mira em 2026-10-06: o cenário headless opt-in
`--aimed-demo` foi materializado em `gameplay-clean-room-aimed-trace.tsv` com
6 eventos, incluindo `SHOT` e `GOAL_HOME`; o replay por
`--match-trace-expected` retornou `matched=True` e produziu o par
`gameplay-clean-room-aimed-replay.tsv`. O checksum de renderização foi
`801159854A2BC6F8`. O readiness agora expõe a linha separada
`gameplay-aimed-trace=verified-authored` (`6/6`), mas o gate geral de gameplay
continua falso: isso é replay determinístico do runtime novo, não semântica de
mira decodificada da ISO. O SHA-256 igual dos dois traces é
`2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`.
O readiness também possui a linha separada
`audio-aimed-trace=verified-authored`: o mesmo cenário com `--play-audio`
produziu 6/6 cues, `SHOT->attackgoal`, `GOAL_HOME->goal`, todos tocados e
replay de áudio determinístico. Isso continua sendo política authored, não
seleção original de EVT/ABK.
O aimed-demo também passou a exportar/reproduzir o input consumido pelo runtime:
80 amostras, com `frame=67`, `move_y=0.75` e `shoot=True`. A linha separada
`gameplay-aimed-input-trace=verified-authored` registra esse controle authored;
isso não atribui binding de controle ou trajetória à ISO. O SHA-256 igual do
par de input traces é
`A1BCF11B578F5BE9A8DF767E162D7314A6E3398A688D7F2FCA438F536002E44E`.

Verificação operacional mais recente: build Release sem avisos/erros, self-test
`53/53`, smokes de animação/morph/material/geometria/áudio aprovados, preview
candidate authored com checksum `F2DD798FDD206BD9`, headless candidate com
`29EACF0AE7B12B58`, oponente/áudio com `651E41284E9242BC` e replays
determinísticos de juggle/rodada com `matched=True`. A ISO permanece inalterada
com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Para QA visual offline, `render-preview ... --authored-hud` materializa o mesmo
placar/tempo/medidor authored usado pelo caminho Win32. A execução com o
candidato `tops0030.big/tops0030.ord/tops` produziu o PPM
`fifa_pc/candidate-preview-authored-hud.ppm` e checksum
`F2DD798FDD206BD9`; a inspeção confirmou a apresentação composta, sem alterar
o headless e sem elevar a evidência candidata a binding original.

Avanço de integração de cena: o novo flag opt-in
`--candidate-home-character` coloca o mesh/textura candidate no slot home
rígido da partida e mantém o jogador away no renderer procedural clean-room.
O preview validado produziu `AB8D372F87FAA6B5` e o headless de 80 frames
`DB0F059C0AC82133`. Isso integra o candidato à cena executável, mas não cria
binding de rig, skinning, morph, animação, UV ou GPU original; o gate de assets
continua `candidate-integrated` e o de rendering continua `not-proven`.
O novo flag opt-in `--candidate-away-character` aplica a mesma colocação
rígida candidate ao slot away; com ambos os flags, os dois slots são
renderizados pelo mesmo mesh/textura candidate. Os checksums verificados foram
`59D25C3C88B9CF69` (preview away), `0F83CEFDD78C58BA` (ambos os slots) e
`72CEA666935DBFCB` (headless away, 80 frames). Isso amplia a apresentação
authored da cena, sem criar binding de rig, skinning, morph, animação, UV ou
GPU original; os gates permanecem `candidate-integrated` e `not-proven`.
O smoke `eagl-material-runtime-batch-smoke` foi então ampliado para executar
cada um dos 24 vínculos nos três slots: home, away e ambos. A execução real
confirmou `24/24` em cada slot, `0` rejeições e `24` checksums distintos em cada
configuração; o relatório e o readiness registram esses campos como cobertura
do caminho candidato, não como binding global de UV, material ou GPU.
No cenário combinado de 140 frames com oponente authored, `--play-audio`,
material candidate e candidate-home, foram observados checksum
`2641FCCE8245A44B`, `3` cues candidate preparados/tocados e `4` eventos sem
rota. A política authored foi então ampliada para as ações conhecidas do
runtime; a repetição manteve `2641FCCE8245A44B`, com `7` cues preparados/tocados
e `0` eventos sem rota. Isso continua sendo seleção clean-room authored, não
binding original; a lacuna de evidência de seleção da ISO permanece explícita.
O cenário authored curto de `0,25` segundo também verificou
`FULL_TIME -> outro`: uma cue preparada/tocada, zero eventos sem rota e
checksum `F39849417B16C774`.

Bateria final da etapa em 2026-10-06: build Release `0/0`, self-test `53/53`,
smoke material/runtime `24/24` em home, away e ambos, com zero rejeições e 24
checksums distintos por slot, auditoria de relocação com
`594` morph rows, `393` layouts parseáveis, `19` bancos, `159` pares de
contenção e `0` pares exatos, EVT nomeado `17/17` e ABK `15/16` bancos
decodificados. O preview candidate-home repetiu `AB8D372F87FAA6B5`; o runtime
combinado de 140 frames repetiu `2641FCCE8245A44B`, com `8/8` cues preparados/
tocados e zero eventos sem rota. O replay authored de juggle retornou
`matched=True`, com SHA-256 idêntico nos dois traces:
`04920791F3B97933F8BB4C912D2DBB283878A66BC75A9C00092CB3F84400B3A1`.
O headless com ambos os slots candidatos produziu `641ED53BD0DAE660`; o cenário
STEAL com slot away, oponente e áudio produziu `301745CFDC42BE25` e replay
`matched=True`.
O renderer clean-room passou a dar a `STEAL` um perfil procedural authored de
tackle via `CleanRoomPlayerAnimator`; isso altera somente a apresentação nova
do PC e não interpreta AXT, morph, rig, skinning ou animação da ISO. Com
material, áudio e slot away, o checksum de 80 frames foi
`30D789D3BCBB915A`, e o replay do trace de dois eventos permaneceu
`matched=True`.
Na variante simétrica, com o slot home candidato e o jogador away procedural,
o mesmo trace também retornou `matched=True`, áudio `tackle` `1/1` e checksum
`BA2CD81182DF970E`.
O smoke cross-system com ambos os slots candidatos produziu
`088EC5D10096A738`, gerou 8 eventos e reproduziu `8/8` cues, com 0 eventos
sem rota; os traces e o `CleanRoomIntegrationReport` foram reexecutados com
`matched=True`/igualdade byte a byte. O self-test passou `53/53` após a
inclusão desse relatório no readiness.
O readiness permaneceu `NOT_READY` e a ISO manteve tamanho `2013003776`,
timestamp `2026-01-22T07:03:26.0859951-03:00`, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Cobertura authored adicional: a fixture de panna foi executada com a regra
`GOALS_OR_PANNA`, produziu `PANNA` e `FULL_TIME` em 3 eventos e seu replay
retornou `matched=True`; a fixture de relógio curto produziu 2 eventos e
também retornou `matched=True`. O readiness agora registra esses dois pares de
trace/replay (`authored_panna_trace_replay` e
`authored_full_time_trace_replay`). Isso reforça a verificação do runtime
clean-room, mas não promove regras authored a semântica original.

Checksums dessa execução: panna `CFCF8FC4F0CB4380` e `FULL_TIME`
`509B0C35DD390FFF`. Os SHA-256 dos traces gerados (iguais aos respectivos
replays) foram `C14B39694F929825A1707687AC2BE9FFBC7F2DE12045C028B867612B2B86F8BF`
para panna e `C025B66A918312E27AE23CDDF9777951F95F0C14F2DF07CB491FF8F062C72801`
para `FULL_TIME`.

O cenário authored `--steal-demo --opponent-ai` também foi integrado ao
readiness: 2 eventos (`KICKOFF`, `STEAL`), replay `matched=True`, checksum
`BFA4E813A3D0EA19` no modo candidate-home/material e áudio `tackle` preparado
e tocado (`1/1`), sem eventos não mapeados. O SHA-256 dos dois traces foi
`46D917414660878DDA0B319A72018473D4BD44858E6A7116730931C11F3DECA2`.
Isso é cobertura authored da rota `STEAL->tackle`, não evidência de IA ou
semântica de posse do jogo original.

## Próximo passo válido

Se existir código-fonte, ele precisa ser colocado no projeto para que possamos
inventariar módulos, dependências, arquitetura alvo e iniciar a conversão para
.NET/PC. Sem essa entrada, o trabalho possível neste diretório é a
reimplementação clean-room já iniciada.

## Primeiro artefato clean-room

Enquanto não há fontes, foi criado `fifa_pc/`, um runtime novo para PC que
monta o ISO em modo somente leitura, cataloga 122 arquivos reais de `data`, valida 29
índices BIG4/BIGF e executa um loop headless gerenciado. Ele compila para
`fifa_pc/bin/Release/net8.0/FifaStreetPc.exe`. A partida clean-room e a janela
Win32 funcionam, mas o jogo completo ainda depende da conversão dos assets,
regras originais e mixagem de áudio.

O runtime passou a expor telemetria de gameplay independente do formato da
ISO: `StreetMatch` contabiliza o kickoff e cada transição observável, enquanto
`GameRuntime.Report()` imprime frame, sequência de ação, última ação, posse e
dribles e as posições finais dos atores/bola. O self-test verifica movimento, `TRICK`, `PASS`,
`PASS_COMPLETE`, `SHOT` e `GOAL_HOME`. Essa instrumentação torna a
sequência nova verificável, mas não decodifica eventos, IDs ou regras do jogo
original.

O gameplay clean-room também ganhou um modo opcional de oponente: com
`--opponent-ai`, o adversário pressiona após a posse observada, recupera a bola
e pode finalizar no gol esquerdo. Em 140 frames, a execução real produziu
`HOME 0 - 2 AWAY`, `SHOT_AWAY=2` e `GOAL_AWAY=2`; o caminho padrão continua sem
AI e preserva sua sequência determinística. Essa lógica é nova e não é uma
decodificação das regras ou da IA original.
Além disso, fora de passe/chute em voo, a bola agora acompanha a posição do
possuidor. O self-test verifica essa invariável e o ajuste preserva o checksum
do caminho padrão; trata-se de estado novo do runtime, não de semântica
decodificada do jogo original.
O chute do jogador Home também aceita `MoveY` no instante do disparo e acrescenta
uma componente vertical determinística à trajetória. Esse controle é coberto por
self-test e pertence somente à reimplementação clean-room; o demo mantém
`MoveY=0`, preservando os checksums headless de referência. Não há atribuição de
binding, regra original ou semântica recuperada à ISO.
O modo headless opt-in `--aimed-demo` integra esse caminho ao runtime completo:
ele emite `MoveY=0.75` no disparo e produziu o checksum
`801159854A2BC6F8`. Isso é uma verificação de controle novo, não um binding de
evento, regra ou asset original.
O modo opt-in `--round-demo` também cobre o ciclo clean-room após um gol: espera
0,5 s, reposiciona os atores e inicia nova rodada com `RESTART`, expondo a
telemetria de rodada no relatório. Essa política é authored no runtime e não é
apresentada como kickoff original decodificado.
Esse ciclo foi adicionalmente exportado e reexecutado com
`--match-trace-expected`: foram 7 eventos, `match_round=2`,
`match_last_action=RESTART`, checksum `404D21B5A0571826` e `matched=True`.
Os dois traces têm SHA-256
`B3BDFEEC8FD2EB6479E7B797C1031AE9EE348451C4BFE4C4E172B993E8872EBB`.
Esse gate comprova somente a reprodução do runtime authored.
O relógio de teste `--match-seconds` também foi integrado: em uma duração
positiva curta, `StreetMatch` emite `FULL_TIME` uma vez e deixa de avançar nos
ticks seguintes; `GameRuntime.Report()` também expõe o resultado authored
`DRAW`, `HOME_WIN` ou `AWAY_WIN`. O self-test e o headless verificam essa
transição; ela é uma política clean-room, não timing decodificado, e permanece
sem binding de áudio.
A apresentação também aceita `--clean-room-characters` para desenhar uma pose
procedural determinística de corpo/cabeça/membros no renderer software e no
host Win32. Ela agora separa perfis authored de posse, passe, drible, chute,
gol, reinício e `FULL_TIME`. Essa camada é authored no PC e não interpreta os
bancos AXT, morph, rigging ou skinning observados.
O headless exporta opcionalmente o último framebuffer via `--frame-output` para
QA visual, mantendo a ISO fora de qualquer destino de escrita.
Também exporta, por `--match-trace-output <saida.tsv>`, a sequência authored de
eventos da partida com frame, placar e posse. A execução validada gerou 6
eventos (`KICKOFF`, `TRICK`, `PASS`, `PASS_COMPLETE`, `SHOT` e `GOAL_HOME`);
esse TSV é telemetria do runtime novo para verificar a integração entre
gameplay, renderização e áudio, não uma decodificação de eventos do original.
O headless também aceita `--input-script <entrada.tsv>` para um TSV authored de
seis colunas (`frame`, `move_x`, `move_y`, `pass`, `shoot`, `trick`), com uma
sétima coluna opcional `panna`. A leitura
exige frames contíguos começando em zero, eixos finitos em `[-1,1]` e booleanos
`0/1/false/true`. A fixture curta `fifa_pc/gameplay-clean-room-input.tsv`
passou em 8 frames e gerou `fifa_pc/gameplay-script-trace.tsv` com somente o
`KICKOFF` inicial, pois não chega à posse; isso verifica a nova entrada
authored, não semântica original.
Os SHA-256 verificados da fixture e do trace são, respectivamente,
`B2F2969BFF1FA22387158B197EEF09479D8BDD682D26B6AB139B11F0A2AE22E8` e
`6288D477F63B66EB445F24E6C55D0F3BD0635B0D0BF7F5480CAD10754AD16D52`.
Uma segunda fixture authored de 80 frames, `fifa_pc/gameplay-clean-room-demo-input.tsv`,
reproduziu o cenário `--demo` pelo leitor de arquivo com checksum
`C0249FD97A01C3F4`, 6 eventos e `GOAL_HOME`; o trace teve SHA-256
`2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`. Isso é
verificação da integração clean-room, não evidência de input original.
Para transformar essa sequência em um gate de reexecução, `run-headless` aceita
`--match-trace-expected <entrada.tsv>`. O arquivo esperado é validado antes da
abertura da ISO e comparado textualmente ao trace produzido; a fixture de 80
frames passou com `matched=True`, 6 eventos e checksum `C0249FD97A01C3F4`, e o
trace reexportado em `fifa_pc/gameplay-script-demo-replayed-stage.tsv` teve o
mesmo SHA-256 `2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`.
O leitor permite ainda combinar input explícito com `--round-demo`; nesse caso
o TSV fornece os controles e a opção habilita somente o reset authored. A
fixture de 140 frames `fifa_pc/gameplay-clean-room-round-input.tsv` produziu 7
eventos, `match_round=2`, `RESTART`, checksum `404D21B5A0571826` e `matched=True`;
os traces tiveram SHA-256
`B3BDFEEC8FD2EB6479E7B797C1031AE9EE348451C4BFE4C4E172B993E8872EBB`.
`--demo` e `--aimed-demo` continuam incompatíveis com input explícito.
O `project-readiness-audit` registra essa verificação na nota de gameplay como
`authored_round_trace_replay=verified` e `authored_round_trace_events=7` quando
os dois artefatos estão presentes; o gate continua falso porque isso não prova
regras originais mais amplas.
O self-test também cobre esse caminho com dois traces authored sintéticos
idênticos e exige gol e `RESTART` antes de aceitar a nota como verificada.
Isso verifica determinismo da simulação authored e não cria replay, input,
evento ou semântica do jogo original.
Também foi adicionado o modo authored opt-in
`--match-rule first-to-goals --target-goals <N>`. Com a fixture de 80 frames e
alvo 1, o runtime encerrou em `HOME 1 - 0 AWAY`, com `FULL_TIME` no frame 70 e
relógio restante; o modo padrão `TIMED` preservou `C0249FD97A01C3F4`. A escolha
é apenas um cenário de gameplay authored orientado por referências externas
documentadas em `docs/FIFA_EXTERNAL_BEHAVIOR_EVIDENCE.md`, não uma regra, limite
ou timing decodificado da ISO.
O fixture `fifa_pc/gameplay-clean-room-panna-input.tsv` cobre a coluna authored
opcional `panna` e a regra `goals-or-panna`; em 31 frames produziu `PANNA`,
`FULL_TIME` no frame 31 e preparou uma row `panna`. O `GameRuntime` agora drena
todos os eventos novos do trace, inclusive quando `PANNA` e `FULL_TIME` são
emitidos no mesmo tick; isso corrige perda de eventos no consumidor de áudio,
sem atribuir cue ou binding à ISO.
O mixer também mantém quatro rotas clean-room explicitamente authored:
`GOAL_HOME -> goal`, `GOAL_AWAY -> goal`, `TRICK -> panna` e `PANNA -> panna`.
`panna` é um nome
de candidato observado e decodificado; a escolha para o evento authored `TRICK`
não afirma vínculo com o evento original. No demo padrão, 2 de 5 eventos têm
candidato preparado e 3 permanecem sem rota; no cenário AI, 3 de 6 são
preparados/reproduzidos e 3 permanecem sem rota.

A camada gameplay também ganhou o modo opt-in `--gamebreaker-demo` e a oitava
coluna authored `gamebreaker` no TSV. Quatro ações `TRICK` carregam um medidor
novo até 100; o disparo emite `GAMEBREAKER`, zera o medidor e alcança
`GOAL_HOME`. A fixture de 40 frames `fifa_pc/gameplay-clean-room-gamebreaker-input.tsv`
produziu 7 eventos, e o trace/replay
`fifa_pc/gameplay-clean-room-gamebreaker-trace.tsv` /
`fifa_pc/gameplay-clean-room-gamebreaker-replayed.tsv` coincidiu byte a byte com
SHA-256 `BC17D33716D221F1495434E12D4C3836957FB4BB83D0E9AA73AAC8CC7B43A4EB`;
a fixture tem SHA-256
`81F9DFDC8AB37681F5309C92D1BD8704333258E08876A621CE92172E18A67D6C`.
É uma mecânica nova do runtime clean-room, sem afirmar controle, medidor,
animação, áudio ou binding GPU decodificado da ISO; `GAMEBREAKER` permanece
sem rota no mixer.
Com personagens authored, as execuções verificadas produziram
`3311E2054549E154` no demo de 80 frames e `2C3F702AB025DA33` no replay de 40
frames; são checksums da apresentação nova e não evidência de pose ou animação
original.

O `project-readiness-audit` passou a verificar também o replay authored do
gamebreaker: quando os dois TSVs estão presentes, registra
`authored_gamebreaker_trace_replay=verified`, 7 eventos, `GAMEBREAKER` e
`GOAL_HOME`. O gate de gameplay continua falso porque esse cenário é uma regra
nova do runtime, não uma reconstrução das regras da ISO.

A camada gameplay também possui o modo opt-in `--juggle-demo` e a nona coluna
authored `juggle`. Quatro ações `JUGGLE` mantêm uma altura/cadeia de bola
determinística e precedem um `SHOT`/`GOAL_HOME`; a fixture
`fifa_pc/gameplay-clean-room-juggle-input.tsv` tem 56 frames e o trace/replay
tem 7 eventos com `matched=True`. O readiness registra
`authored_juggle_trace_replay=verified`; isso é comportamento novo do runtime,
não decoder de input, física, frame, animação, áudio ou GPU da ISO. A tecla `J`
no host Win32 é igualmente authored.

O host Win32 agora também encaminha a tecla `G` (quando a janela está em foco)
para o campo authored `MatchInput.Gamebreaker`. Isso fecha a passagem do
controle authored do headless para o host interativo, mas continua sendo uma
decisão nova do runtime e não evidência de input ou regra da ISO.
O host Win32 também encaminha `N` para o campo authored `MatchInput.Panna`, ao
lado de `G` para `Gamebreaker`. O self-test cobre os dois mapeamentos com estado
de teclado sintético; isso não atribui essas teclas ao input original.
`J` é encaminhado para `MatchInput.Juggle`; o self-test cobre o mapeamento
isolado, mantendo a mesma separação de proveniência.
O caminho Win32 também desenha um HUD authored no framebuffer: placar, tempo e
barra do medidor. O self-test compara a apresentação com e sem esse HUD; o
headless não o habilita, preservando os checksums de referência e a separação
em relação aos assets candidatos. Os textos/glyphs são UI nova do PC, sem
atribuir fonte, layout ou comando GPU à ISO.
O comando `render-preview` também aceita `--authored-hud` para QA visual offline
do mesmo caminho, sempre como opção explícita e sem alterar o headless.
Com `--clean-room-characters`, `GAMEBREAKER` também tem um perfil procedural de
celebração separado dos dados observados. O self-test verifica seus parâmetros
contra idle, trick e shot; isso integra somente apresentação authored e não
cria binding de AXT, morph, rigging, skinning ou timing.
Na frente de áudio, a auditoria `audio-sccl-channel-layout-audit` testou os
144 ranges PA cujo header observado não possui `0x82`: 144/144 decodificaram
consistentemente como mono, 0/144 como estéreo e nenhum caso ficou ambíguo.
Essa hipótese permanece separada do header observado (`channels=0`) e agora
permite preparar os 10 candidatos EVT nomeados antes rejeitados, elevando a
preparação nomeada para 17/17 e 2.914.952 bytes PCM. Isso amplia evidência de
payload, não cria binding EVT/ABKC ou gameplay.

O runtime também abre entradas individuais dos BIG diretamente do ISO por
streams limitados. A entrada `banklist.axt` de `data/anims.big` foi validada
com 47 bytes, sem extração e sem alteração da imagem original. Essa camada
prepara o próximo trabalho: decodificar os formatos de animação, textura,
modelo e áudio e conectá-los a sistemas novos de PC.

O parser AnimationBankReader avançou essa frente sem atribuir semântica a
payloads: data/anims.big contém 20 sidecars .axt, sendo 19 catálogos com 1.584
entradas de arquivo. Cada catálogo foi ligado ao par .ord/.orl do mesmo banco;
os 19 ELF MIPS combinados são válidos e os 19 expõem o símbolo literal
__AnimationBank:::BANK correspondente ao nome catalogado. Essa é evidência de
identidade estrutural de banco, não uma decodificação de frames, poses, rig,
pesos ou timing.
O mesmo relatório gerou 19 hashes de layout ELF distintos; os 19 bancos têm
uma seção .data validada e o símbolo de banco fica 28 bytes antes do fim dela.
Essa distância e esses hashes são apenas metadata de seção/relocation, sem
interpretar o payload como records, frames, poses ou timing.
A normalização conservadora adicionada ao mesmo relatório copia somente a seção
.data e zera as words cobertas por relocations antes do SHA-256. Os 19 bancos
produziram 19 hashes de payload normalizado distintos; não foi observado um
template byte-a-byte comum nessa regra. Isso é evidência estrutural negativa e
não decodifica records, frames, poses, ossos, pesos, rigging, timing ou skinning.
O comando `animation-bank-payload-compare` acrescenta uma comparação pareada
dos 19 `.data` validados: são 171 pares, medidos por bytes/words alinhados crus
e depois da mesma máscara de relocations. Nenhum par tem payload bruto ou
normalizado exatamente igual; o maior overlap normalizado contém 5.429 bytes e
129 words iguais. O maior trecho contíguo igual mede 40 bytes ou 10 words,
tanto cru quanto normalizado. Essa recorrência parcial é somente evidência de
payload alinhado e não atribui semântica a qualquer campo.
Essa validação foi integrada ao `GameAssetCatalog.Scan`: o runtime headless
agora monta os 19 catálogos, pares `.ord/.orl` e ELF associados e reporta
19/19 contagens de catálogo, ELF válidos, símbolos coincidentes e hashes `.data`
normalizados distintos. Trata-se de integração estrutural de assets, não de
seleção, playback ou decodificação de frames/poses.
As entradas textuais também são preservadas em cada `AnimationBankAsset`. O
`animation-bank-runtime-smoke` valida o caminho completo do catálogo e reporta
19 bancos, 1.584 entradas AXT, 1.578 nomes distintos, uma tag de versão e
19/19 contagens, pares `.ord/.orl`, ELF e símbolos válidos. Nomes e timestamps
continuam metadata observada; não são tratados como frames, poses, rig, timing
ou skinning.
O comando `animation-bank-name-index` acrescenta um TSV por entrada textual,
com banco, versão, diretório, timestamp, estado ELF/símbolo e recorrência do
nome entre bancos. O índice real tem 1.584 linhas, 1.578 nomes distintos e 6
nomes compartilhados; ele só organiza evidência para a próxima investigação,
sem atribuir semântica de frame, pose, rig, timing ou skinning.
O novo `animation-bank-catalog-compare` acrescenta uma camada independente de
comparação somente dos catálogos `.axt`: 19 perfis e 171 pares, com 1.584
entradas e 1.578 nomes distintos. Os 6 nomes compartilhados entre bancos têm
timestamp observado igual, mas não houve conjunto de nomes idêntico, nome na
mesma posição ou prefixo ordenado comum entre pares. Os TSVs resultantes são
diagnóstico de sobreposição textual/temporal; não são decoder de frames, poses,
rig, timing ou skinning.
O comando `animation-bank-elf-structure` acrescenta a leitura estrutural dos
headers ELF e relocations: foram validados 19/19 pares, com 114 linhas de
seção, 6 seções por banco, 19 hashes de seção distintos e 19 hashes de
relocation distintos. Os 19 símbolos de banco têm metadata observada e
distância 28 ao fim de `.data`; isso não interpreta payload, tamanhos ou
offsets como records de frame, pose, rig ou timing.
O comando `animation-bank-tail-compare` encontrou 32 bytes iniciados no
símbolo `__AnimationBank:::` em cada um dos 19 bancos. A word little-endian nos
offsets 4–5 iguala `2 * catalog_file_count` e `2 * declared_file_count` em
19/19. O contador declarado do cabeçalho também confere com as entradas
parseadas em 19/19; após mascarar relocations, 30/32 offsets são comuns em
todos e 12 pares têm cauda normalizada idêntica. Isso é evidência byte-level e
correspondência numérica reproduzível por duas fontes de contagem, sem atribuir
semântica de campo, frame, pose, rig, timing ou skinning.
O comando `animation-bank-data-relocation-audit` acrescentou uma auditoria
independente somente dos offsets relativos e gaps adjacentes das relocations de
`.data`: 19/19 ELF válidos, 35.900 relocations, 19/19 bancos com relocations,
19 perfis de offsets distintos e 15 contagens distintas de gaps. O máximo foi
29.152 offsets distintos e 291 gaps distintos em um banco. O relatório também
preserva histogramas e hash offset/tipo; isso é evidência de layout, não um
decoder de payload nem semântica de frame, pose, rig, timing ou skinning.
O comando `animation-bank-sidecar-boundary-audit` acrescenta uma checagem
independente da fronteira física dos sidecars: os 19 pares `.ord/.orl` não têm
seções ELF cruzando a divisão; somente `.data` file-backed não vazia fica em
`.ord`, enquanto quatro outras seções, a seção de relocation e a tabela de
seções ficam em `.orl`. Os relatórios preservam ranges e contexto limitado de
bytes. Isso documenta composição física, não um decoder de payload, frame, pose,
rig, timing ou comando.
O comando `eagl-morph-sidecar-boundary-audit` repete a checagem nos 594 objetos
com símbolo `__Morph:::` validado: 1 seção file-backed fica em `.ord`, 4 em
`.orl`, relocation e tabela de seções ficam em `.orl` em 594/594 e não há
seções cruzando a fronteira. Os 886 pares obtidos somente pelo nome `_morph.o`
foram excluídos dessa contagem. Isso é composição de sidecar, não integração de
morph, pose, rig, peso, frame ou skinning.
O comando `eagl-morph-shpx-structure-audit` acrescenta uma checagem independente
dos pacotes `.xsh` same-stem: 594/594 rows foram observados, 591 pacotes foram
validados, 3 ausências (`tops0001`, `tops0020`, `tops0210`) foram preservadas e
0 pacotes foram rejeitados. Dezessete layouts têm perfis SHPX variáveis, todos
entre os 38 layouts ambíguos; isso é header/tabela observada, não vínculo de
morph, textura, material, UV, mesh, pose, rig, peso, frame, timing ou GPU.
O comando `eagl-morph-shpx-geometry-correlation-audit` cruzou os dois relatórios
por layout, sem parear linhas internas: 393/393 layouts foram unidos, 17 têm
variação simultânea SHPX/geometria, 21 variam somente na geometria e 355 são
estáveis. Nos 38 layouts ambíguos, 17 têm co-variação. O resultado é triagem
estrutural, não vínculo de textura, mesh, morph, UV, material, pose, rig,
skinning, animação, timing ou GPU.
O comando `eagl-morph-elf-header-audit` validou os 594/594 objetos morph e
encontrou um único perfil de header core, range da tabela e metadata do símbolo
`__Morph:::` em cada um dos 393 layouts, inclusive nos 38 ambíguos. Isso é um
controle negativo de header, não decoder de payload ou semântica de animação.
O mesmo catálogo agora percorre estruturalmente `fe.bh` e `ig.bh` sem persistir
os blocos: são 2 pacotes, 284 BIGs internos, 1.106 entradas, 284 `.apt`, 284
`.const`, 279 `.o`, 259 `.xsh`, 259 conjuntos completos e 230 assinaturas
compostas. Nenhum dos 279 `.o` expõe `__Morph:::`. Isso fortalece a validação
de containers e companions, mas continua sendo controle estrutural negativo,
não uma decodificação de timing, pose, morph, frame ou material.

O primeiro parser estrutural específico foi adicionado para `VivF`: os 15
pacotes `.bh` foram validados, incluindo suas tabelas e registros sentinela.
Os 15 índices também foram ligados aos contêineres companheiros `.big`/`.viv`,
com verificação de limites de cada bloco e do offset sentinela. O payload
RefPack é descomprimido em memória limitada e as entradas BIG internas já
alimentam parsers de assets; o XBE original segue sem execução.

Cada bloco `VivF` agora pode ser aberto como um stream limitado pela API
`GameAssetStore.OpenVivFBlock`. O primeiro e o último bloco de `fe.bh` foram
testados contra `fe.viv`; essa janela será usada pelo decodificador quando o
codec interno estiver identificado.

O codec foi identificado nos blocos como RefPack `10 FB` e o decoder seguro foi
implementado em `RefPackDecoder.cs`. A validação em lote passou por 313 blocos
de `fe.bh`, `db.bh` e `ig.bh`, sempre produzindo exatamente o tamanho expandido
declarado. Uma parte dos arquivos expandidos já é classificada como BIGF e
inspecionada; os demais formatos continuam exigindo parsers específicos.

Nos pacotes de animação, a expansão revelou uma segunda camada: todos os 163
blocos de `fe.bh` e os 121 de `ig.bh` são BIGF internos válidos, totalizando
632 e 474 entradas respectivamente. O `db.bh` foi mantido separado porque seu
payload expandido não é BIGF.

Os leitores estruturais dos assets incluem SHPX, `Apt Data:`, `Apt constant
file`, ELF32 e XPR0. SHPX é contêiner de imagens, não de malhas. A conversão
SHPX cobre DXT1/DXT3 e imagens PAL8 com paleta anexada para TGA alfa. Em
`player1.xsh`, o decoder reconhece os registros 8-bit `0x7B`, a paleta BGRA
`0x2A` e o swizzle `0x2000`; os 46 quadros de 128x128 foram exportados.

O comando `apt-structure-inventory` agora percorre os BIGs internos dos
pacotes de animação sem escrever os blocos. Em `fe.bh`, foram lidos 163
entradas `.apt` e `.const`, 161/161 `.o` EAGL estruturalmente válidos, 145 `.xsh`
e 145 conjuntos com os quatro companions; em `ig.bh`, 121, 121, 118/118, 114 e
114. Os padrões de header variam
por família e algumas ocorrências têm campos fora do tamanho do asset, então o
runtime distingue header parseado de `fields_within_asset` e ainda não trata
os words como timing, pose, morph ou material. A presença de `.const` é
uniforme no pacote, mas `.o` e `.xsh` não: headers repetidos aparecem com e
sem `.xsh`, então esse companion não é usado como regra de animação.

Uma assinatura estrutural adicional combina o header APT validado, o header
`.const` validado quando presente, a assinatura estrutural do `.o` EAGL, a
tabela SHPX validada do `.xsh`, as extensões same-stem e os tamanhos dos
companions. O inventário produziu 133 perfis em `fe.bh` e 97 em `ig.bh`; 161/161
e 118/118 objetos `.o` e 145/145 e 114/114 tabelas SHPX dos conjuntos
correspondentes foram lidos, sem rejeições no inventário. Isso mede organização
de pacote, não timing,
poses, morphs, materiais ou frames.

O loader de textura `XPR0` agora interpreta o `header_size` real e o
registrador de formato. Os dois assets visuais encontrados no ISO são DXT1:
`saveimage.xbx` é 64x64 e `titleimage.xbx` é 128x128. O decoder converte o
nível base para RGBA em memória e o comando `texture-preview` exporta PPM.
DXT3, DXT5 e formatos não observados continuam rejeitados explicitamente em
XPR0. Em SHPX, o primeiro exemplo de animação, `ArrBLeft.xsh`, é uma imagem
DXT3 de 64x64 e foi exportada para TGA. `player1.xsh` contém 46 quadros PAL8;
a paleta anexada de cada quadro foi associada e os 46 foram exportados.

Na etapa atual, `shpx-image-entry-audit` releu `player1.xsh` da ISO e produziu
`fifa_pc/shpx-image-entry-audit.tsv`: 46/46 entradas PAL8 128x128 foram
decodificadas, com tabela monotônica, tamanho declarado consistente, hash da
tabela ordenada e hash RGBA por entrada. O status é apenas
`verified-image-entries`; isso não promove as imagens a frames temporizados e
não cria bindings de pose, rigging, skinning, animação, material, UV, morph ou
GPU.

O leitor ELF32 agora expõe nomes de seções, símbolos e relocações. Os objetos
de personagem em `data/models.big` são blocos RefPack que contêm arquivos
`.ord`, `.orl` e `.o`; `.ord` e `.orl` formam juntos um ELF32 MIPS dividido,
enquanto `.o` contém dados de morph. O leitor reconstrói o ELF dividido,
interpreta `__BBOX` como seis floats e inventariou 885 caixas válidas entre 886
pares `.ord/.orl`, sem objetos inválidos. Os símbolos `__Model`, `__VB` e
`__RM` e os comandos das cinco camadas do exemplo confirmam as referências aos
buffers/render methods. O novo leitor valida 11.950 descritores `__VB` pelas
bounds da seção e pelo cabeçalho proprietário; 4.780 streams de float3 são
finitos, e 2.389 cabem nas bounds dos modelos (predominantemente `__GPvb_10`,
com candidatos também em `__GPvb_11`). O leitor percorre `__Model` e os
descritores de render, decodifica cinco slots do comando `id=9` e valida
coerência entre strides dos slots e os grupos `__GPvb`: 2.387 de 2.390
primitivas passam essa checagem, incluindo todas as 1.615 strips explícitas.
Isso melhora a evidência de layout, mas o binding efetivo do draw e a
topologia completa ainda não estão provados. Foi adicionado um exportador OBJ
diagnóstico (`eagl-position-cloud`) que grava candidatos como pontos `p`, sem
inferir triângulos, para inspeção visual dos dados extraídos. Na amostra, as
relocações de parâmetros globais referem-se a estado de primitiva/textura,
não diretamente aos buffers.
Como avanço adicional, o leitor mede perfis finitos de `float2`: 2.390/2.390
streams ficam na faixa próxima de `[0,1]`; entre os bindings explícitos,
2.387/2.387 primitivas têm esse perfil no slot 1 e 772 também expõem
`SHAPENAME` com `XBOXEXTOBJ_SetClampU/V`. Essa evidência é mantida como perfil
numérico de atributo. Ela ainda não prova semântica UV, escolha de textura para
`SHAPENAME`, binding final do draw ou material PC.
Na cadeia de mesh gerenciada, o `EaglCandidateMeshBuilder` preserva os
`Slot1Attributes` junto aos vértices aceitos: no `star0001-head`, são 3 grupos,
534 vértices e 534 valores finitos. O campo é deliberadamente genérico e não
é tratado como UV nem usado para selecionar textura.
O preview tem uma opção `--slot1-profile` que leva esses valores até o
framebuffer por cores médias de triângulo; a opção é diagnóstica e não altera
o caminho padrão do runtime.

O exportador de strips exige estado explícito `PT_TRIANGLESTRIP`, candidato
único dentro das bounds e layout de slots/strides coerente com o comando
`id=9`. A nova execução gerou
`exports_fifa_mesh_candidates/star0001-head-strips-layout-checked.obj`: três
grupos, 534 vértices e 528 faces; dois grupos foram omitidos. Os índices são
válidos, mas o binding do draw ainda não foi provado, então o OBJ não é
considerado asset final nem malha jogável. No inventário completo, 2.387 de
2.390 primitivas passam a checagem de layout; as 1.615 strips explícitas
também passam.

O diagnóstico `eagl-shapename-index` passou a descompactar os 724 pacotes
`.xsh` de `data/models.big` em memória e validou 1.545 registros SHPX internos,
com 14 nomes distintos. `SHAPENAME=tops,1` coincide com o registro interno
`tops` em 224 pacotes, mas o mesmo nome aparece em 152 basenames de arquivo;
`SHAPENAME=head,1` não coincide com nome interno. A auditoria repetida também
encontrou zero coincidências de nome completo ou prefixo entre os estados e os
basenames `.xsh`; os prefixos de arquivo são `star`, `tops`, `tats`, `bott`,
`logo` e `ball`. Os estados `head` em pacotes `star` têm entradas internas
`face`/`hair`, mas isso não forma uma cadeia textual exata. Essa correlação
ainda não seleciona uma imagem por modelo e não foi ligada ao renderer.

Como avanço controlado, `EaglShapeMaterialLoader` implementa somente o caso em
que a relação pode ser reproduzida no mesmo tronco. Em `tops0030.big`, ele
valida `tops0030.ord` com `SHAPENAME=tops,1`, abre `tops0030.xsh`, encontra a
entrada interna `tops` e converte a imagem PAL8 256x124 para RGBA. O comando
`eagl-material-preview` combinou esse recurso com a malha candidata do objeto
(2 grupos, 341 vértices e 337 triângulos), texturizando os 2 grupos no
framebuffer software e gerando `fifa_pc/tops0030-material-preview.ppm`, com
checksum `FB81D8B0DBB5E819`.
Esse é um caminho de integração clean-room verificável, não uma reconstrução
final: UV universal, skinning, deformação, rigging e semântica dos comandos GPU
continuam sem prova independente.
O inventário same-trunk ampliou a verificação para 24 arquivos `.big` distintos:
todos os matches decodificados pertencem à família `tops` e usam o nome interno
`tops`. Essa repetição não é tratada como uma tabela global e não foi
extrapolada para `head`, `face`, `hair` ou outros recursos.

Como controle negativo para a frente de animação, `apt-structure-inventory` agora
aceita `--report <saida.tsv>` e preserva uma linha por `.apt`, com validade dos
companions, hash da assinatura estrutural e presença literal de símbolos morph no
`.o`. Os relatórios gerados têm 163 linhas em `fe.bh` e 121 em `ig.bh`; entre os
279 objetos `.o` válidos, nenhum expõe `__Morph:::` e nenhum stem corresponde ao
padrão nominal `*_morph`. Isso afasta uma ligação direta observada entre esses
companions APT e o catálogo morph, mas não decodifica timing, poses ou frames e
não prova ausência de animação.

AptStructuralCorrelation acrescenta um relatório por combinação de padrão de
header APT, header .const e extensões companion. Os relatórios reais formaram
80 perfis em fe.bh e 51 em ig.bh, cobrindo 284 pacotes e 568 words alinhados;
nenhuma das 568 comparações foi igual. Isso é uma comparação de recorrência
bruta usada como controle negativo: não converte words em timing, pose, frame
ou animação e não prova ausência desses dados.

O comando `apt-animation-negative-control` acrescenta um controle textual
separado entre esses relatórios e `animation-bank-name-index.tsv`. Na ISO, ele
cruzou 284 linhas APT com 1.584 linhas de catálogo, encontrou 279 objetos
válidos, 259 conjuntos estruturais completos, 0 símbolos `__Morph:::`, 0
candidatos `*_morph` e 0 coincidências exatas de stems com nomes de banco ou
arquivos de animação. O resultado (`fifa_pc/apt-animation-negative-control.tsv`)
continua sendo evidência negativa de nomes/presença, não decoder de timing,
pose ou frame.
O comando `apt-cross-package-profile-audit` cruza somente a chave exata de
header APT, header `.const` e extensões entre os perfis FE/IG. A execução
encontrou 80 chaves FE, 51 IG, 27 compartilhadas, 53 exclusivas FE e 24
exclusivas IG; nenhuma compartilhada teve word alinhada igual ou símbolo
`__Morph:::`. Isso é recorrência estrutural de perfil, não decoder de timing,
pose, frame ou animação.

O decoder RefPack também reconhece o tamanho declarado nos cabeçalhos curto
`0x10 FB` e estendido `0x90 FB`; ambos os caminhos têm teste sintético.

O núcleo `StreetMatch` agora possui estado determinístico de jogadores, bola,
placar, relógio e entradas básicas. Ele está ligado ao ciclo `GameRuntime` e
passa por testes internos. Um framebuffer RGB software e um host Win32 novo
foram adicionados: `run-windowed` abre uma janela de PC, lê WASD/P/Espaço/T e
apresenta a cena de teste com `titleimage.xbx` decodificado, sem executar o XBE.
O argumento opt-in `--round-reset` leva ao host Win32 a política authored de
reinício de rodada após gol, compartilhando o mesmo `StreetMatch` do headless;
isso é integração de gameplay nova, não kickoff ou regra original decodificada.
O caminho `render-preview` também
gera um quadro PPM reproduzível para inspeção automatizada.
Ele agora aceita `--candidate-material [nested.big] [SHAPENAME] [objeto.ord]`
e entrega a mesma cadeia candidate same-trunk ao `PreviewRenderer`; a amostra
`tops0030.big/tops` produziu 2 grupos, 341 vértices, 337 triângulos e checksum
`C1DB074C7140C585`. A opção é explicitamente diagnóstica e recusa gravar sobre
a ISO; `rendering` permanece `not-proven` porque o estado de grupo e a
semântica UV/material original continuam não decodificados.

Os leitores de áudio reconhecem bancos `ABKC` e streams `SCHl`. EA-XA v3 Xbox
decodifica frames ADPCM/PCM de blocos `SCDl`, exporta WAV PCM16 em fluxo e tem
um comando de reprodução síncrona Windows para streams de até 32 MiB em memória.
`GBREAK.ast` da ISO foi convertido: estéreo, 24 kHz, 905528 quadros, 566
blocos; o catálogo real aceita 6/6 streams AST. A cadeia `ABKC→BNK` também
valida 13 bancos, 231 headers variáveis `PT`/`GSTR` e 231/231 ranges Xbox IMA.
Sons RAM podem ser decodificados para WAV por seleção explícita com
`audio-abk-decode`; a rota de ações já chega ao mixer, mas a associação
ABKC/BNK→EVT e a mixagem geral de gameplay ainda não estão demonstradas. As
As rotas clean-room integradas são `GOAL_HOME -> goal`, `GOAL_AWAY -> goal` e
`TRICK -> panna`, reutilizando rows PA nomeadas validadas; `panna` é uma escolha
authored do runtime e isso não afirma vínculo original
EVT→som. A janela e o preview continuam sendo infraestrutura de runtime, não
renderização final dos assets do jogo.
O `CatalogAudioMixer` agora reporta também eventos não mapeados, candidato
indisponível, rejeição de decode, preparo e reprodução. A execução padrão real
teve 5 eventos, 3 sem rota e 2 candidatos preparados; a execução com AI teve 6
eventos, 3 sem rota e 3 candidatos preparados/tocados. Essa telemetria torna a integração parcial
auditável sem inventar uma tabela EVT→som.

Na etapa seguinte, o vínculo same-trunk de material candidato foi ligado de
forma opt-in ao `SoftwareGameplayRenderer` usado pelo `GameRuntime` headless.
`run-headless ... --candidate-material` carrega a cadeia reproduzível
`tops0030.big` → `tops0030.ord` → `SHAPENAME=tops,1` → `tops0030.xsh/tops`,
mantém a malha gated e rasteriza a imagem PAL8 através de `Slot1Attributes`.
O smoke real de 80 frames produziu checksum `CFA4BCBEE162C121`; sem a opção,
o checksum continuou `5405DE72BE1B4FC1`. O caminho ainda é candidato e possui
fallback sólido: não promove slot 1 a UV global nem atribui semântica final aos
comandos GPU, materiais, skinning, rigging ou animação. O projeto permanece
em andamento e não há recompilação do XBE.

A amostra `tops0040.big` também percorreu o `GameRuntime` headless com a opção
de material candidato: textura PAL8 256x256, mesh candidata de 527 vértices e
523 triângulos, 80 frames e checksum `3A2D00B09DC319F6`. A repetição fortalece
somente a família `tops` e não estabelece bindings para outras famílias.

O `GameAssetCatalog.Scan` passou a registrar essa mesma cadeia candidata durante
o runtime headless. A ISO atual reporta 24 matches same-trunk, 24 objetos
distintos, 1 shape distinto (`tops`), 24 texturas decodificadas e 24 texturas
PAL8. O catálogo preserva somente nomes, dimensões, formato e tamanho RGBA da
imagem validada; não transforma o resultado em UV global, material GPU,
skinning, rigging ou comando de draw.

Nesta etapa, o caminho same-trunk também foi exposto no host Win32 por opção
explícita: `run-windowed ... --candidate-material [nested.big] [SHAPENAME]
[objeto.ord]`. Quando a cadeia é validada, o host passa a mesma malha e a
textura candidata ao `PreviewRenderer`; sem a opção, mantém o mesh
`star0001-head` com cor provisória. Falhas retornam ao fallback e são
reportadas como indisponibilidade do candidato. Isso integra os caminhos de
renderização novos, mas continua sem alegar bindings globais de UV/material,
skinning, rigging, animação ou GPU.

A investigação estrutural de morph avançou com `EaglMorphSiblingReader`.
`eagl-morph-sibling-inventory` percorre os 391 BIGs internos de
`data/models.big` e aplica apenas o padrão nominal observado
`*_morph.o` → `*.ord/.orl` no mesmo pacote. Os 594 morphs encontrados tiveram
irmão `.ord/.orl` válido e símbolos geométricos; 593 também tinham bounds, com
42 assinaturas de relocação repetidas. O resultado continua evidência de
arquivo/ELF/bounds: não há aplicação a vértices nem interpretação de pesos,
ossos, poses, frames ou animação.
O leitor também registra `__PosePaletteBank:::` sem lhe atribuir semântica:
593/594 morphs expõem esse símbolo, e os perfis das assinaturas repetidas
mantêm 4 nomes `BS_*` com 1 ou 2 atributos. Essa repetição é evidência de
estrutura nomeada; palette de poses, pesos, ossos e animação continuam não
decodificados.
Essa evidência também foi integrada ao catálogo sem reter payloads: o comando
`eagl-morph-sibling-runtime-smoke` confirma 391/391 BIGs internos, 594 objetos
observados, 594 pares completos/ELF/geometria, 593 bounds e 593 símbolos de
palette. O smoke continua explicitamente estrutural e não executa deformação.
O catálogo de animação também preserva os registros ELF crus dos 19 símbolos
`__AnimationBank:::`. Foram observadas 19 assinaturas de metadados distintas;
todas têm seção 1, tamanho 4 e `Info=17`. Comparados com o registro morph
`table_section=4`, `symbol_index=3`, `symbol_section=1`, `value=0`, `size=4`,
`Info=17`, não houve coincidência estrutural. É controle negativo de layout
ELF, não evidência suficiente para ausência de vínculo ou para decodificar pose,
rig ou animação.
O comando `eagl-morph-pose-bank-index` preserva os símbolos por BIG e compara
o nome normalizado e os metadados ELF crus com os bancos de animação: 391 linhas, 1 símbolo
distinto (`PosePaletteBank`), 1 registro ELF distinto (`table_section=4`,
`symbol_index=3`, `symbol_section=1`, `value=0`, `size=4`, `Info=17`) e 0
matches textuais. O TSV também preserva esses metadados brutos. É um controle
negativo de recorrência nominal e um registro de metadados ELF observados, não
uma prova de ausência de vínculo ou um decoder de pose/rig/skinning.
O comando `eagl-morph-animation-name-negative-control` adiciona um controle
lexical separado entre os 2.360 nomes completos de shape dos 594 morphs e os
1.578 nomes de arquivo/19 nomes de banco dos catálogos de animação. Houve
0 coincidências exatas em ambos os conjuntos. Como a comparação não normaliza
tokens e não observa timing, ela não prova ausência semântica nem autoriza
binding de animação, pose, rig ou skinning.
Uma assinatura relativa dos campos relocados também foi adicionada: o perfil
mais frequente reúne 173 morphs com gaps `0x0,0x4,0x8,0xC,0x24,0x28,0x34` e um
campo posterior variável. Essa é uma métrica de repetição de layout bruto,
sem atribuição a records, índices ou atributos.
O hash do payload normalizado, que mascara apenas strings nomeadas e words de
relocation, foi distinto em 594/594 objetos. Logo, não há nesta amostra um
template byte-a-byte comum que justifique decodificar records de morph.
Uma comparação local adicional usa janelas de raio 16 em torno dos campos de
relocation, mascarando apenas strings nomeadas e words de relocation antes do
hash. Os 594 morphs produziram 326 assinaturas locais, 404 pares
morph-layout/janela e 11 layouts morph com múltiplas assinaturas locais. Isso
é variação de bytes dentro de layouts repetidos, não uma regra para records,
pesos, ossos, poses ou frames.
A assinatura estrutural independente, limitada a tamanho/posição de seção,
strings observadas e campos/alvos de relocation, produziu 393 perfis em 594
objetos; o maior perfil repetiu 42 vezes. Isso é repetição de layout, não
decodificação de records, pesos, ossos, poses ou frames.
A comparação estrutural dos companions `.ord/.orl`, limitada a contagens/totais
de seções, símbolos, relocations, classes geométricas e bounds, produziu 417
assinaturas e 443 pares morph-layout/companion. Trinta e dois dos 393 perfis
morph têm mais de uma assinatura companion; não há seletor geométrico
comprovado.
O hash refinado de layout ELF, limitado a headers de seção, posições/tamanhos
dos símbolos geométricos e offsets relativos/tipos de relocation, produziu 444
assinaturas, 447 pares morph-layout/layout e 32 layouts morph com múltiplos
layouts companion. Isso continua metadata estrutural, sem binding.
O stream de comandos recebeu uma assinatura opaca baseada em primitive/layer,
offsets, assignments textuais, IDs/tamanhos, hashes dos bytes crus e relocation:
592 assinaturas, 593 pares morph-layout/stream e 38 layouts morph com múltiplos
streams. Nenhum ID foi interpretado como operação GPU.
Também foi adicionada uma chave estrutural pareada que reúne apenas as camadas
independentes já validadas do morph e do companion: layout morph, janelas de
payload normalizadas, layout ELF, stream opaco, sequência de registros `__VB`,
perfil numérico, topologia candidata e coincidências numéricas cross-file. O
inventário produziu 593 chaves; 355 dos
393 layouts morph têm uma composição única e 38 têm mais de uma. A chave ajuda a
selecionar amostras repetíveis, mas não constitui binding ou decodificação de
morph, rig, pose, peso, material, UV, skinning, animação ou GPU.
Uma comparação cross-file adicional usa apenas words little-endian alinhadas do
payload morph e valores de metadata numérica validados no companion; strings
nomeadas e words de relocation observadas são excluídas. Foram observadas 406
assinaturas de coincidência e 12 layouts morph com múltiplas formas. Isso é
igualdade estrutural de campos comparados, não semântica de peso, índice, pose ou
frame.
Uma auditoria complementar (`MorphCandidateGeometryAudit`) mede, por layout,
finitude, índices repetidos, áreas zero, degeneração raster e perfil de arestas
dos meshes candidatos. Na ISO foram observadas 594 linhas em 393 layouts; 38
layouts variam, e 32 preservam uma assinatura de mesh candidata única enquanto
a geometria numérica varia. Um layout tem linha sem assinatura por ausência de
bounds; 393 layouts foram completos/ELF-validos e 392 tiveram bounds em todas as
linhas. Os relatórios continuam QA da representação candidata e não corrigem
topologia nem criam bindings de UV, material, skinning, rigging ou animação.
Sua correlação opcional com a assinatura de payload pareada encontrou 38 layouts
variáveis no par, 27 com payload estável e geometria candidata variável e 11 com
ambas as camadas variáveis; isso serve apenas para priorizar amostras.
O inventário passou a aceitar `--catalog <saida.tsv>` e registrar uma linha por
morph fora da ISO, mantendo nomes, estabilidade do layout e assinaturas opacas.
O catálogo verificado tem 594 linhas: 355 layouts estáveis e 239 linhas nos 38
layouts ambíguos. Ele não promove nenhuma correspondência a binding.
Sua coluna `candidate_geometry_signature` é apenas uma assinatura de QA da
representação candidata.
As opções `--geometry-report <saida.tsv>` e `--geometry-summary <saida.tsv>`
materializam o perfil geométrico adicional por layout e sua distribuição, sem
alterar o relatório de ambiguidade.
As opções `--geometry-correlation-report <saida.tsv>` e
`--geometry-correlation-summary <saida.tsv>` materializam a correlação entre
payload e geometria candidata, também sem alterar o relatório de ambiguidade.
O comando independente `morph-candidate-geometry-ambiguity-audit` cruza a
coluna geométrica com os rótulos já publicados: 38/38 layouts geométricos
variáveis são ambíguos, 0 são estáveis, 0 ambíguos ficam sem variação geométrica
e há 0 divergências de rótulo. É uma auditoria de consistência candidata.
O relatório opcional `--ambiguity-report <saida.tsv>` separa os 393 layouts e
mostra, nos 38 ambíguos, variação em stream de comandos (38), layout companion
(32), registros `__VB` (27), coincidência numérica (12), janelas de payload (11),
mesh candidato (6) e perfil numérico (5). Isso é somente variação observada.
O mesmo relatório cruza cada camada dos 38 layouts ambíguos, por igualdade
exata, com os 355 layouts estáveis: a interseção ocorre em
`numeric_descriptor` (38), `candidate_mesh` (32), `payload_window` (26),
`vertex_record` (25) e `companion_layout` (2), e não ocorre em `command_stream`
 ou `companion_numeric_match`. Essa estatística não promove nenhuma assinatura a
 semântica ou binding.
O comando `morph-ambiguity-summary <ambiguity-report.tsv> <summary.tsv>` separa
os 38 layouts ambíguos pela composição exata das camadas observadas. Foram
produzidas 8 composições cobrindo 239 linhas; o TSV mantém contagens de overlap
com controles estáveis e SHA-256 opacos das chaves estruturais. A separação
organiza a triagem, mas não combina camadas nem decodifica morph, pose, rig,
peso, frame, timing ou skinning.
O comando `morph-ambiguity-byte-summary <ambiguity-report.tsv>
<byte-report.tsv> <summary.tsv>` cruza as 8 composições com o relatório
byte-level por chave estrutural exata: 239/239 linhas foram associadas,
213/239 repetiram bytes e limites de comando, 0/239 tiveram assinatura
estrutural de comando exata, 0/239 coincidiram em registro/assinatura/limite
numérico e 222/239 coincidiram na categoria valor/token. Esses são indicadores
dos relatórios, não semântica de GPU, morph ou animação.
O comando `morph-ambiguity-numeric-summary <ambiguity-report.tsv>
<numeric-gap-report.tsv> <summary.tsv>` cruza as mesmas composições com os
17 gaps numéricos: 10 layouts ambíguos aparecem em 4 composições e 28 layouts
ambíguos não têm linha de gap. As métricas mantêm 7/17 igualdades de fronteira
relativa, 5/17 de janela de payload e 0/17 de boundary numérico ou offset
canônico absoluto. Isso organiza limites observados, sem interpretar tokens.
O comando `morph-ambiguity-group-metadata <ambiguity-report.tsv>
<byte-report.tsv> <numeric-gap-report.tsv> <metadata.tsv>` materializa a
matriz por layout ambíguo. A execução associou 239 linhas byte e 17 gaps às 38
linhas de layout; 28 layouts não têm gap. O resultado agrupa somente perfis,
contagens e fronteiras observadas, sem atribuir semântica a morph, pose, rig,
peso, frame, timing, skinning, material, UV ou comandos GPU.
O comando `morph-ambiguity-independent-audit <structural-catalog.tsv>
<ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]` reagrupa o
catálogo estrutural e compara os oito contadores publicados no relatório.
Foram auditadas 594 linhas em 393 layouts, com 0 inconsistências de
metadata/contagem e 0 divergências entre os rótulos `ambiguous/stable` e a
variabilidade medida nas oito camadas. Nos 38 ambíguos, 26 têm mesh candidata estável com
companion variável, 6 têm mesh candidata variável e todos os 38 variam em
alguma camada medida. Isso é controle de consistência e triagem estrutural,
não decoder ou binding.
O comando `eagl-morph-name-state-audit <morph-structural-catalog.tsv>
<saida.tsv>` faz somente uma comparação lexical entre os nomes `BS_*` dos 594
morphs e os valores crus `SHAPENAME` dos companions. Foram observados 2.360
nomes morph distintos, 391 linhas de estado em 594 linhas completas e 0/594
coincidências, tanto exatas quanto normalizadas antes da vírgula. É um controle
negativo de strings; não decodifica morph, mesh-group, material, UV, skinning,
rigging, animação ou GPU e não cria binding.
O comando `audio-event-named-decode-smoke <iso> [--report <saida.tsv>]` tenta
decodificar os ranges dos 17 nomes extraídos do EVT. Sete candidatos têm canais
no header observado e dez têm uma hipótese mono única validada por layout e
frames `SCDl`; os 17 produziram WAVs válidos, totalizando 2.915.700 bytes.
Isso é evidência de payload de áudio, não binding automático de evento, ABKC ou
gameplay.
O comando `audio-abk-batch-decode-smoke <iso> [--report <saida.tsv>]
[--max-attempts <n>]` tenta índices bounded já observados nos 16 bancos ABKC.
Quinze bancos produziram WAV, um foi rejeitado (`data/audio/CONTROL.abk`, sem
candidato disponível), com 10.051.852 bytes no total e fontes AST=6, BNK=9 e
none=1. Isso é evidência de payload/decoder, não seleção de evento, binding de
gameplay ou interpretação de material, UV, skinning, rigging, animação ou GPU.
O ciclo normal de `CatalogAudioMixer.Initialize`, tanto no headless quanto no
host Win32 com opener read-only, agora monta os mesmos candidatos bounded:
15 bancos decodificados, 1 rejeitado e fontes AST=6, BNK=9 e none=1. Isso
integra payload observado ao runtime, sem converter banco em cue ou evento.
O comando `audio-event-index-search <iso> --data --report <saida.tsv>` também
foi formalizado como `audio-event-index-evidence.tsv`. Na ISO íntegra, o
relatório preserva 1.061 pares candidatos e 17 offsets/words distintos, mas
registra zero ocorrências alinhadas nos 16 `.abk`, em `pcpahdr.big` e em
`pcpadat.big`. Esse é um resultado negativo limitado à busca alinhada: o TSV
mantém `direct_selection_binding=not-proven` e `original_semantics=not-claimed`,
sem criar associação EVT→linha, ABKC ou playback.
O `gameplay-audio-coverage-audit` agora agrega os traces/replays authored de
full-stack, aimed, round, gamebreaker, juggle, panna, FULL_TIME e STEAL. A
execução verificou 8/8 cenários, 42/42 eventos preparados/tocados, 0 sem rota,
as 14 ações da política e 8 cues únicos. O relatório é `verified-authored`
apenas para o runtime clean-room e mantém `original_semantics=not-claimed`.
O par de traces de áudio do cenário aimed tem SHA-256
`7FA076ADE12B81EC41213F4C34113D35F6F0D1A8FCC14F540C66776BB0CE8F32` e o
relatório de coverage tem SHA-256
`3C0DBC250C19F9259CC37EABF3C7A49A56F8C1CAD6072BDA729C38AF93C369D9`.
Ele também pré-prepara os 17 candidatos EVT nomeados, totalizando 2.914.952
bytes PCM no headless. Nos dez casos, o header observado continua registrado
como `channels=0` e a hipótese mono fica em campo separado; a reprodução
clean-room segue limitada a `goal` e não representa uma tabela EVT→som original.
O inventário também aceita `--byte-report <saida.tsv>` para comparar bytes crus
de comando e registros canônicos UTF-8 de coincidência numérica contra os 355
controles estáveis. O resultado tem 239 linhas ambíguas: 213 têm stream de
comando exatamente igual a algum controle, embora 0/239 tenham assinatura
estrutural de comando exata; 26 não têm igualdade byte-level e 0/239 coincidem
exatamente no registro numérico. É uma medida alinhada de repetição,
não uma decodificação de GPU, material ou morph.
O mesmo relatório passou a registrar a partição observada por primitiva, usando
somente ordem, contagem, tamanho, completude e SHA-256 dos segmentos crus.
Foram observadas 252 assinaturas, 402 pares layout/partição e 9 layouts com
mais de uma partição; 213/239 linhas ambíguas coincidem também nessa camada
com um controle estável. A medida continua estrutural e não cria bindings.
Os registros numéricos também passaram a ser comparados por sequência de
offsets e por tokens valor/categoria sem offset: 394 e 283 assinaturas,
respectivamente. Os tokens coincidem em 222/239 linhas ambíguas, mas o registro
canônico completo continua em 0/239; trata-se de normalização textual de
triagem, não de records de morph decodificados.
O argumento `--numeric-gap-report` materializa as 17 linhas restantes em 10
layouts e 8 conjuntos de controles mais próximos. Esse recorte facilita a
investigação sem transformar tokens ou proximidade em binding.
O comparador `TokenSequenceComparer` acrescenta uma leitura posicional neutra
desse recorte: todos os 17 gaps mantêm a mesma quantidade de tokens na melhor
comparação, com prefixo comum máximo de 4; 15 têm 8 diferenças mínimas e 2 têm
16. Na melhor comparação de cada linha, a porção textual anterior ao
delimitador teve 8 ou 16 diferenças e a porção posterior teve zero. Isso é
apenas medida de sequência observada, não decodificação de registros ou
semântica de morph. O TSV registra também os índices ordinais zero-based das
diferenças; os 17 casos formam quatro padrões, com frequências 7, 6, 2 e 2.
Esses índices são textuais e não representam vértices, ossos, poses ou comandos.
O relatório também compara o `CanonicalMatches` alinhado ao controle mais
próximo: preserva offsets, words observados, sufixos e seus quatro bytes
little-endian. Nos 17 gaps, há 8 ou 16 words divergentes, zero diferenças de
sufixo, 8 ou 16 bytes divergentes e 21/24/26/42/58 offsets divergentes. Isso
continua sendo evidência estrutural, não uma tabela de records.
O campo `nearest_control_canonical_offset_delta_signature` preserva os deltas
assinados dos offsets alinhados; os 17 gaps produziram 10 assinaturas distintas.
Isso descreve limites observados, não deslocamentos de geometria ou animação.
O comparador também produz `relative_offset_boundary_signature`, removendo a
base absoluta e mantendo somente os deltas entre offsets adjacentes observados.
O relatório real tem 8 perfis relativos: 7/17 gaps coincidem exatamente com
algum controle estável e 10/17 não coincidem. Essa é uma comparação de
fronteiras, não uma decodificação de records, vértices, ossos ou frames.
As oito janelas de payload normalizadas foram comparadas na mesma linha de
controle: nenhum dos 17 gaps teve sequência exata; as diferenças ficaram em
2–8 hashes, com 0–6 iguais e prefixo comum de 0–2. Essa é evidência de
vizinhança normalizada, não um decoder de records.
O novo argumento `--numeric-gap-correlation-report <saida.tsv>` agrupa esses
17 gaps pelas 10 sequências assinadas e cruza, sem semântica, os perfis de
janelas e os limites crus do stream. O relatório gerado tem 12 perfis globais
de janela e 10 de limite; 3 grupos repetem uma janela, 6 repetem um limite e
somente 2/17 linhas coincidem com algum limite de um controle estável. São
contagens e hashes de recorrência estrutural, não bindings de morph, rig,
animação, material ou GPU.
Na ampliação seguinte, a mesma correlação contou separadamente os 355 controles
estáveis: 7/17 gaps coincidem na fronteira relativa, 5/17 na sequência completa
das oito janelas normalizadas, 0/17 no boundary numérico do companion e 0/17 nos
offsets canônicos absolutos; o limite bruto de comando permanece em 2/17. As
camadas não são combinadas em um score e nenhuma recebe semântica de campo.
O caminho `eagl-material-batch-preview` também foi executado sobre os 24
vínculos same-trunk `tops`: 24/24 PPMs candidatos, 0 rejeições e 24 checksums
distintos, com 14 meshes de dois grupos e 10 de um grupo. Os 7.403 vértices,
7.327 triângulos e 38 grupos texturizados permanecem evidência da integração
software candidata, não binding final de UV/material/GPU.
O smoke `eagl-material-runtime-batch-smoke` ampliou essa verificação para a
cadeia integrada `titleimage.xbx` + `SoftwareGameplayRenderer` +
`PreviewRenderer`: 24/24 frames candidatos, 0 rejeições, 24 checksums distintos
e 38 grupos texturizados. A cobertura adicional continua opt-in e não atribui
semântica global a UV, material GPU, skinning, rigging ou comandos.
Com `--report fifa_pc/eagl-material-runtime-batch.tsv`, a execução também
preserva uma linha por amostra; o gate de prontidão lê esse relatório apenas
para confirmar que o caminho candidato foi exercitado no runtime, sem promovê-lo
a binding original.
Uma auditoria adicional confirmou `SHAPENAME=tops,1` em 24/24 objetos, mas em
0/24 grupos geométricos candidatos. Como os estados observados estão em
primitivas separadas dos grupos que passam o gate geométrico, a implementação
mantém a relação como não comprovada e não a usa para inventar binding.
As prévias visuais `tops0030` e `tops0040` também mostraram fragmentação e
alongamento fortes. O novo `eagl-candidate-geometry-audit` separa essa
observação de gates objetivos: nos 24 candidatos houve 0 índices inválidos,
0 vértices não finitos e 0 triângulos com índices repetidos; a auditoria contou
46 áreas 3D exatamente zero, 48 áreas XY exatamente zero e 196 triângulos que
ficam degenerados depois da projeção inteira, contra 7.131 visíveis. O relatório
é `fifa_pc/eagl-candidate-geometry-audit.tsv`; ele não autoriza escolher outra
sequência de strip, reinícios, UVs ou bindings sem evidência independente.
Uma comparação adicional produziu vistas `xy`, `xz` e `yz` para `tops0030` e
`tops0040`. As silhuetas mudaram, mas a fragmentação permaneceu em todos os
planos; por isso nenhuma convenção de eixo, câmera ou transformação foi
escolhida para o runtime. Os PPMs em
`fifa_pc/candidate-projection-previews-stage-1` são somente QA da geometria
candidata.
O smoke de objeto passou a aceitar `--commands`, repetindo para um BIG/ORD
específico a preservação dos fluxos completos, IDs, bytes crus, relocations e
atribuições textuais. Em `tops0030`, são 13 primitivas/13 fluxos completos; as
primitivas geométricas explicitamente `PT_TRIANGLESTRIP` continuam distintas
das primitivas TAR que carregam `SHAPENAME=tops,1`. Isso amplia a evidência
inspecionável sem decodificar comandos GPU.
O novo `eagl-candidate-state-correspondence` cruzou as duas populações nos 24
objetos `tops`: 269 pares, 0 igualdades exatas de bytes, 0 igualdades da forma
de fronteira, prefixo máximo de 8 bytes e 269/269 fluxos completos. O resultado
é uma auditoria estrutural/negativa e não cria binding de estado, mesh,
material, UV, skinning, rigging, animação ou GPU.
O controle adicional `eagl-state-geometry-adjacency-audit` preserva essa
separação por objeto: 24/24 linhas, 0 pares com o mesmo índice, 48 pares
adjacentes, 269 pares na mesma camada e 0/24 grupos geométricos com estado
`SHAPENAME` exato. A saída `fifa_pc/eagl-state-geometry-adjacency.tsv` serve
somente para impedir uma heurística de adjacência; rendering continua
`not-proven`.
O controle seguinte `eagl-state-geometry-profile-audit` comparou a sequência
opaca de IDs/tamanhos, contagem e bytes dos comandos por par geometria--estado.
Foram produzidas 269 linhas em 24 objetos, com 0 sequências exatas, 0 pares no
mesmo índice, 48 vizinhos de índice, 1 perfil geométrico e 3 perfis de estado;
4 linhas têm perfil de estado não repetido, mas sem igualdade de sequência.
Isso é diagnóstico estrutural adicional e não fornece o vínculo independente
necessário; o artefato é `fifa_pc/eagl-state-geometry-profile-audit.tsv` e
rendering permanece `not-proven`.
O controle `eagl-state-geometry-order-audit` acrescenta uma partição por
intervalos da ordem observada: nos 24 objetos same-trunk foram registrados 38
grupos geométricos, 180 primitivas com `SHAPENAME`, 62 intervalos e 36 runs não
vazios. As 180 primitivas foram particionadas sem sobra, todos os 36 runs não
vazios tinham streams completos e surgiram 17 perfis opacos de sequência.
Essa saída somente registra fronteiras e bytes de streams; não associa um run
a um grupo, não decodifica comando GPU e não cria bindings de material, UV,
skinning, rigging ou animação. O artefato é
`fifa_pc/eagl-state-geometry-order-audit.tsv`; rendering continua
`not-proven`.
O novo `eagl-candidate-attribute-audit` separa os perfis numéricos do slot-1
dos estados de primitivas nos 24 objetos same-trunk: 38/38 grupos têm perfil
completo, finito e próximo da faixa unitária, somando 7.403 valores, mas
0/38 grupos carregam `SHAPENAME` ou clamp diretamente contra 180 primitivas
com esses estados. Isso continua evidência de representação, não decoder de
UV, material, skinning, rigging, animação ou GPU.
Os descritores `__VB` foram comparados pela mesma regra neutra, preservando
somente streams, bytes, elementos, strides, flags e seções: 235 assinaturas,
407 pares morph-layout/descritor e 13 perfis morph com múltiplos descritores.
Isso não transforma streams em posições, UVs, normais, pesos ou deformações.
A ordem relativa dos registros foi resumida separadamente por hash de seção,
offsets relativos, descriptor/data offsets, bytes, stride e flags: 358
assinaturas, 431 pares morph-layout/sequência e 27 layouts morph com múltiplas
sequências. Isso permanece metadata estrutural sem atributo nomeado.
A medição numérica adicional ficou limitada à finitude de palavras float
empacotadas e à containment de streams de stride 12 nas bounds declaradas:
12 assinaturas, 398 pares morph-layout/número e 5 perfis morph com múltiplos
perfis numéricos. Continua sem semântica de componentes.
A topologia candidata foi comparada somente por grupos, vértices, triângulos e
primitivas ignoradas: 593 companions têm assinatura, 221 valores distintos
incluindo o caso vazio, 399 pares morph-layout/topologia e 6 perfis morph com
múltiplas topologias. Isso não demonstra topologia original ou deformação.

## Auditoria de influência no rasterizador candidato

Foi adicionada a auditoria `eagl-candidate-render-influence-audit`, que executa
os 24 vínculos same-trunk no mesmo `SoftwareGameplayRenderer` e compara o
checksum de uma cena com textura candidata ao checksum da mesma malha com
fallback sólido. O resultado validado registrou geometria finita/indexada em
24/24 linhas e influência observável na cena authored em 20/24. `tops0080`,
`tops0140`, `tops0260` e `tops0340` tiveram
`observed-candidate-raster-unchanged` nessa cena, mas o controle isolado em
buffer neutro encontrou influência texturizada em 24/24. Isso separa cobertura
da cena de cobertura da operação de rasterização candidata; não é uma prova de
binding original. O relatório mantém triângulos visíveis, contagens de
cores/alpha RGBA e diferenças RGB por pixel nos dois controles.

Artefato: `fifa_pc/candidate-render-influence-audit.tsv`;
SHA-256: `886419C9E716086C87BB36E2DF9728FF734E1D883FCCDEEDFC683A20048BB6E2`.
O gate de assets continua `candidate-integrated` com `passed=False`, o gate de
renderização continua `not-proven`, e `original_semantics=not-claimed`.

Bateria final após essa ampliação: Release `0` avisos/erros, self-test `53/53`,
smoke material/runtime `24/24` sem rejeições, geometria `24/24` com 7.131
triângulos raster-visíveis, runtime headless padrão de 80 frames com checksum
`5405DE72BE1B4FC1`, full-stack de 140 frames com checksum
`088EC5D10096A738`, replay de match e áudio `matched=True`, cobertura authored
de áudio `8/8` cenários, `42/42` preparados/tocados e `0` não mapeados;
influência candidata na cena `20/24` e no controle isolado `24/24`. A ISO
permanece com tamanho `2013003776`, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Etapa atual — evidência estática limitada de destinos de renderização

Foi implementada a auditoria `tools/analysis/xbe_render_target_audit.py`.
Ela parte dos clusters de contexto render candidatos, escolhe uma seleção
determinística dos 64 destinos mais fortes e disassembla no máximo 384 bytes
por destino, parando no primeiro `ret`. O relatório contém `64` candidatos,
`64` janelas disassembladas, `63` janelas terminadas por `ret` e `7` prólogos
semelhantes a função. O resultado é somente evidência estrutural do XBE
retail: não houve execução do XBE, recuperação de função comprovada ou
criação de bindings; `original_semantics=not-claimed`.

Relatórios: `fifa_pc/xbe-render-target-audit.tsv`, SHA-256
`3FF54B4F5D5AF71147CC89F5400E27F2C0DEA562A7FD6657313543EEF99A403A`, e
`fifa_pc/xbe-render-target-summary.tsv`, SHA-256
`F2B41AC9545C50ADA19939C15E2E94496239A1FB9E4628EBA4DD7E062B89BB94`.
As instruções continuam sem semântica atribuída para materiais, UVs,
skinning, rigging, animação, morphs ou comandos GPU.

O novo subgate `rendering-static-xbe-target-windows` é
`verified-structural`; o gate de renderização integrado continua
`not-proven`. Os gates de assets, áudio, gameplay e animação mantêm suas
classificações anteriores, sem promoção por esta análise.

Verificação da etapa: Release `0/0`, self-test `72/72`, testes Python `13/13`,
matriz completa `completed-authored-and-candidate-opt-in`, smokes relevantes,
runtime headless, replay determinístico e captura windowed. A ISO ficou
inalterada e mantém SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Etapa atual — reachability estática limitada dos destinos de renderização

Foi adicionada `tools/analysis/xbe_render_cfg_audit.py` para construir uma
reachability graph conservadora sobre as janelas de renderização já
selecionadas. A análise usa no máximo 2048 bytes por alvo, segue apenas
saltos diretos internos à janela e registra chamadas diretas sem segui-las;
transferências indiretas continuam não resolvidas.

Resultado: `64/64` alvos com CFG alcançável, `62/64` com retorno alcançável,
`441` blocos básicos, `127` chamadas diretas e `22` transferências indiretas.
Isso é evidência estática/estrutural, não recuperação completa de funções,
execução do XBE ou binding de renderização. Os artefatos são
`fifa_pc/xbe-render-cfg-audit.tsv` (SHA-256
`5000D88598D42E2238279B47AA47364B830776ED1C2BC8DABE2B6874C2CEAECA`) e
`fifa_pc/xbe-render-cfg-summary.tsv` (SHA-256
`EC0FE95F012EE104C19065F41C806ABFD974DBD34E25876DA545C0CB99DABA5B`).

O subgate `rendering-static-xbe-cfg-slices` está
`verified-structural`; o gate principal de renderização segue
`not-proven`. Nenhuma semântica foi atribuída a materiais, UVs, skinning,
rigging, animação, morphs ou comandos GPU.

Verificação: Release `0/0`, self-test `72/72`, testes Python `14/14`, matriz
completa `completed-authored-and-candidate-opt-in`, smokes, runtime headless,
replays determinísticos e captura windowed. A ISO manteve o SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Etapa atual — correlação PCM de todos os candidatos enumerados

A auditoria de áudio foi repetida com limite operacional `100000` por banco,
acima dos `368` candidatos realmente enumerados nos `16` bancos. Foram
decodificados `239/368` candidatos de banco e `17/17` candidatos EVT; a
comparação encontrou `0` pares PCM exatos e `0` hashes EVT com correspondência.

Os relatórios são `fifa_pc/audio-event-payload-wave-correlation-all.tsv`,
SHA-256 `BDE7CF7E58E09E17D2BBB8932E413249D51264424E5A1C32E8B5E30D404E345E`,
e `fifa_pc/audio-event-payload-wave-correlation-all-summary.tsv`, SHA-256
`CC767B8DCE09E74CDB78FCA6898A9C4960DBC386DBC82B26F87A5C389DD084FD`.

O resultado é uma negativa estrutural de correlação dentro do conjunto
enumerado. Não comprova ausência global, não seleciona sample e não cria
binding EVT→ABKC→som; `direct_selection_binding=not-proven` permanece e o
gate geral de áudio continua `partially-integrated`.

Verificação: Release `0/0`, self-test `72/72`, testes Python `14/14`, matriz
authored/candidate opt-in, runtime headless, smokes e replays determinísticos.
A ISO manteve SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — grafo estático limitado de chamadas do renderizador

Foi implementada `tools/analysis/xbe_render_call_graph_audit.py`. Ela parte
dos `87` alvos diretos dos CFGs de renderização e segue apenas chamadas
imediatas diretas até profundidade `2`, em janelas de `256` bytes e limite de
`512` nós. A execução encontrou `235/235` nós executáveis disassemblados,
`326` arestas únicas, `200` nós com retorno alcançável e `0` truncamentos.

O resultado é evidência estrutural interprocedural limitada. Não confirma
limites de função, não executa o XBE, não resolve chamadas indiretas e não
atribui semântica ou binding para geometria, materiais, UVs, skinning, rigging,
animação, morphs ou GPU. O subgate
`rendering-static-xbe-call-graph` está `verified-structural`, mas o gate
principal de renderização continua `not-proven`; `original_semantics` permanece
`not-claimed`.

Relatórios: [nodes](D:\projeto zeta\fifa_pc\xbe-render-call-graph-nodes.tsv),
[edges](D:\projeto zeta\fifa_pc\xbe-render-call-graph-edges.tsv) e
[summary](D:\projeto zeta\fifa_pc\xbe-render-call-graph-summary.tsv). SHA-256:
`340DFC27050E6A9E29018A9C946C90895A8E98780442FF6BB722D9E08D3722B0`,
`F7B7291B616470BEB41A5491B4F798423775BE398FD70F796CE7E9B08CC8073C` e
`16671B4DF9A4EE10D9DBE1B7833372751ACA4D7B8916BBBDD172C6AAAF4AA4E8`.

Verificação: Release `0/0`, self-test `72/72`, testes Python `16/16`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, replays
determinísticos e captura windowed. O projeto permanece em andamento e a ISO
continua somente leitura com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
## Etapa 0.40 — controle negativo da população de estado EAGL — 2026-10-07

O audit bruto `eagl-original-render-chain-audit` passou a registrar os slots
observados no stream sem atribuí-los a atributos. Na leitura same-trunk foram
verificados `2364/2364` streams, `2364` linhas com slots, `2362` sequências
estruturais completas `0..4`, `750` linhas com coocorrência de `SHAPENAME` e
slots, e `0` linhas com sobreposição entre `SHAPENAME` e `SetPrimitiveType`.

Os `750` casos de coocorrência são observação bruta, não associação estado→mesh;
nenhum slot foi promovido a UV, material, normal, peso, skinning, rigging,
animação, morph ou GPU. O novo readiness
`rendering-original-state-population-negative-control` é
`verified-structural-negative`; o gate principal de renderização continua
`not-proven` e o projeto continua `NOT_READY`.

Relatórios: `fifa_pc/eagl-original-render-chain.tsv` (SHA-256
`629C6D7A836CAA46A84E09A4EEA3DE8F3AC11ADE871219B7E205DF710C07B66C`) e
`fifa_pc/eagl-original-render-chain-summary.tsv` (SHA-256
`16FAF6D2F477A621445015A87C05E04896F20F3CA5000B6634E9CBB15D647DF7`). O
readiness tem SHA-256
`F4DBDEC11A306181CB2943AC1077A8B930CA6401EE40961281AC67D134F354DD`.

Validação: Release `0/0`, self-test `74/74`, Python `32/32`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless, captura
windowed e replays determinísticos. A DLL Release tem SHA-256
`DA0C35BEFF7C600F1E9CF9277EA854FBC6CBE5F8E3745C26ACB6DE50AE12E445`. A ISO
permanece somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar uma associação original independente que sobreviva a
controles negativos e validação fora do caminho candidate; se ela não existir,
preservar a lacuna como `not-proven` e avançar nos subsistemas ainda não
decodificados sem inventar bindings.

## Etapa 0.41 — controle independente da população de estado bruta — 2026-10-07

Foi adicionado `tools/analysis/eagl_original_state_population_control.py`.
Ele compara, por objeto bruto same-trunk, somente as populações observadas de
`SHAPENAME` e `SetPrimitiveType` contra índice, camada, hash e forma de
fronteira do command stream. O relatório cobriu `883` objetos e `2364` linhas:
`752` SHAPENAME, `1612` SetPrimitiveType, `0` sobreposições, `0` pares com
mesmo índice, `777` pares adjacentes, `1904` pares na mesma camada, `0` hashes
de comando iguais e `0` formas de fronteira iguais.

Esses números são um controle populacional/estrutural; não demonstram
estado→geometria, UV, material, skinning, rigging, animação, morph ou GPU.
`original_semantics=not-claimed`; o subgate
`rendering-original-state-population-control` é
`verified-structural-negative`, enquanto renderização principal e o estado
geral continuam `not-proven`/`NOT_READY`.

Relatórios: `fifa_pc/eagl-original-state-population-control.tsv` (SHA-256
`1DB67C35A1AFF669606666E86802E76117E160D05562C8B0E2F3B9CE487BE83F`) e o
resumo `fifa_pc/eagl-original-state-population-control-summary.tsv` (SHA-256
`6D47AFC8F8826B849985D618645DD59924C1D35D14FEFA879B38B75B58C8E7E4`).

Validação: Release `0/0`, self-test `74/74`, testes Python `33/33`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed,
replays determinísticos e verificação da ISO. A DLL Release tem SHA-256
`75BA0991379DE3ED68783702D9929938ECABEE69B6A7F4FE1A096695F99A5DC1`; o
readiness tem SHA-256
`FD0461990F170868CF5C3EB919B73A4539D6A173B93D93848C3A8332DAED5725`. A ISO
permanece sem escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar outra evidência original independente para
estado→geometria/UV/material; se não existir, preservar explicitamente a
lacuna e continuar os subsistemas não decodificados separados do caminho
candidate. Não marcar concluído nem iniciar code review.

## Etapa 0.42 — inventário estrutural bruto de `__Model`/`__VB` — 2026-10-07

O audit same-trunk foi ampliado para validar, por objeto original, a presença
de símbolos `__Model`, `__BBOX`, `__RM` e `__VB` e a estrutura delimitada dos
registros `__VB` por stride, comprimento e contagem observada. O resultado foi
`883` objetos carregados, `883` linhas `__Model`, `882` linhas `__BBOX`, `2364`
linhas `__RM` e `11820/11820` registros `__VB` validados; `882` objetos têm
bounds e `883` têm streams de vértices estruturalmente validados.

Isso comprova somente a existência e os limites dos registros brutos no
caminho original. Não comprova associação primitiva→stream nem atribui
semântica de geometria, UV, material, skinning, rigging, animação, morph ou
comandos GPU. `vertex_record_control_status` é
`verified-bounded-original-vertex-record-structure`; o gate principal de
renderização permanece `not-proven`, o readiness permanece `NOT_READY` e
`original_semantics=not-claimed`.

Relatórios: `fifa_pc/eagl-original-render-chain.tsv` (SHA-256
`629C6D7A836CAA46A84E09A4EEA3DE8F3AC11ADE871219B7E205DF710C07B66C`) e
`fifa_pc/eagl-original-render-chain-summary.tsv` (SHA-256
`36D8765FB5E3DC1E00ABDF59C8477384575F5D912A1B737F2BB1A971BCD4C8CF`). O
readiness registra `rendering-original-vertex-record-structure` como
`verified-structural` e tem SHA-256
`93065B27B628D955F4261B062F99C347B6B32269785F99AAD70F15BEAEA4FE9B`.

Validação: Release `0/0`, self-test `74/74`, testes Python `33/33`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram. A DLL Release tem SHA-256
`0BF974EA15DD900F6D57AE1FEA14BAD7095B36A552BFE398996D686EEED748CD`. A ISO
permanece somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência original independente que associe registros
de vértices a primitivas/streams e sobreviva aos controles negativos. Se ela
não existir, preservar `not-proven` e continuar os subsistemas ainda não
decodificados sem inventar bindings.

## Etapa 0.43 — associação estrutural opaca `__VB`↔fetch — 2026-10-07

Foi adicionada a auditoria `eagl-original-vertex-record-association-audit`.
Ela permanece no caminho original bruto: compara somente a sequência de cinco
strides observada nos fetch slots com uma sequência única de cinco categorias e
ordinal consecutivos nas chaves textuais dos registros `__VB`. Resultado:
`2364` primitivas, `2364` com registro ordinal, `2362` sequências opacas
únicas e `2362` sequências de cinco fetch slots completas; `0` pacotes e
objetos rejeitados.

Isso é associação estrutural comprovada, não binding semântico. Não nomeia
atributos nem decodifica geometria, UV, material, skinning, rigging, animação,
morph ou comandos GPU. O subgate
`rendering-original-opaque-vertex-record-association` é
`verified-structural`; o gate principal de renderização continua
`not-proven`, `original_semantics=not-claimed` e o readiness permanece
`NOT_READY`.

Relatório: `fifa_pc/eagl-original-vertex-record-association.tsv` (SHA-256
`162D94FA23DB73C469392FFD5AA037FA2D029E7E06D47B32C2692004BA7B9ECD`); resumo
`fifa_pc/eagl-original-vertex-record-association-summary.tsv` (SHA-256
`257C8F331CB6D9B3E9BB99E18F1DAEA99298779A8B035E78126B14DB26D118CE`). O
readiness tem SHA-256
`5C6468CB5D769B00EB88F0B3213D7BA293CEB8949815A5E5F3951B0526B8EBF9`.

Validação: Release `0/0`, self-test `75/75`, testes Python `33/33`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram. A DLL Release tem SHA-256
`030372D0DBDB657E53D09871B18B46B021F469F6F6BA112B8281C14E1A7C3FD1`. A ISO
permanece somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: testar controles independentes contra essa associação opaca e
buscar evidência de geometria/UV/material fora de nomes, strides e comandos
não decodificados; nenhuma semântica deve ser promovida sem prova adicional.

## Etapa 0.44 — controle independente da associação opaca — 2026-10-07

Foi adicionado `tools/analysis/eagl_original_vertex_record_association_control.py`.
Ele reparseia independentemente o TSV original e reproduz `2362/2362`
sequências opacas exatas; os controles de ordem de strides invertida e ordinal
deslocado produziram `0/0`. O controle cobriu `2364` linhas e `883` objetos.

É um controle estrutural negativo/independente, não uma promoção de binding.
Continuam sem semântica atribuída geometria, UV, material, skinning, rigging,
animação, morph e GPU; o gate de renderização permanece `not-proven` e o
readiness `NOT_READY`.

Artefatos: `fifa_pc/eagl-original-vertex-record-association-control.tsv`
(SHA-256 `3D5F57E04D65C4956E5F26AB994ABD5DA1C2ED79DEE1D905C4BC5DE62F75513A`)
e seu resumo (SHA-256
`26E1DABB216D91957886D8C97ED3ADF5DC5BA448D8D36E260CBFA05C60FF9765`). O
readiness tem SHA-256
`EC399C25E4E12B19618F3FCA02C29ECC4F9E8532CF8AA0547DFA21F83003A699`.

Validação: Release `0/0`, self-test `75/75`, testes Python `34/34`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram. A DLL Release tem SHA-256
`56D7443171E80962DBA901970AE8764A59BBCAD10456B76B4904E9387B304A04`. A ISO
permanece somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência original fora das chaves textuais, strides e
comandos opacos — especialmente geometria/UV/material — ou preservar a lacuna
como `not-proven` sem inventar bindings.

## Etapa 0.45 — estrutura numérica dos payloads originais `__VB` — 2026-10-07

Foi adicionada a auditoria `eagl-original-vertex-payload-audit`. Ela reabre
somente os objetos EAGL originais, valida as faixas de payload já aceitas pelo
leitor `__VB`, mede perfis finitos de floats, registra perfis `float2` próximos
da faixa unitária sem chamá-los de UV, mede floats de três componentes e sua
contenção nos bounds declarados, e conta ranges distintos/sobrepostos. Resultado:
`388/388` pacotes, `883/883` objetos, `11820` streams validados, perfis
numéricos em `883` objetos, contenção nos bounds em `882`, ranges distintos em
`883` e `0` pares de ranges sobrepostos; nenhuma rejeição.

Isso é evidência numérica/topológica comprovada no caminho original, não
geometria decodificada nem binding. Nenhum stream recebeu semântica de posição,
UV, material, skinning, rigging, animação, morph ou GPU. O subgate
`rendering-original-numeric-vertex-payload-structure` é
`verified-structural`; o gate principal de renderização continua `not-proven`,
`original_semantics=not-claimed` e o readiness continua `NOT_READY`.

Artefatos: `fifa_pc/eagl-original-vertex-payload.tsv` (SHA-256
`EC69F3A7D0A3F56FB0178AE5A39B68258FFE749F57F5C0BEDCB9A27E93C99E3C`) e o
resumo (SHA-256
`9487A2A19F9F9FC8E9403C068FBA54A895289695C3BF2055D6AEDB0FF4D63C21`). O
readiness tem SHA-256
`819BB6F05291BC2B8B3AFCD9961851B79EE3A586FE4A682344992E7D3C4949B0`.

Validação: Release `0/0`, self-test `76/76`, testes Python `34/34`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtime headless/windowed e
replays determinísticos passaram. A DLL Release tem SHA-256
`BF1B3B40F4382A7CB51BFADD89C4C277133AF2FE5E81D06B46D8FD2AE1234665`. A ISO
permanece somente leitura: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência original independente de geometria, UV ou
material além dos perfis numéricos e ranges, mantendo `not-proven` quando não
houver prova e sem inventar bindings.

## Etapa 0.46 — controle independente da estrutura numérica dos payloads `__VB` — 2026-10-07

Foi adicionado o controle independente
`tools/analysis/eagl_original_vertex_payload_control.py`. Ele reparseia o TSV
dos objetos originais sem reutilizar a implementação .NET e reproduz as
assinaturas numéricas e os metadados de ranges em `883/883` objetos: `883/883`
linhas com assinatura coerente, `883/883` com ranges válidos e `883/883` com o
status de entrada esperado. O resultado é
`verified-independent-original-numeric-payload-control`.

Isto é um controle estrutural/negativo independente, não uma promoção
semântica. Continuam sem atribuição de posição, geometria, UV, material,
skinning, rigging, animação, morph ou GPU; `original_semantics=not-claimed`, o
gate principal de renderização permanece `not-proven` e o readiness permanece
`NOT_READY`.

Artefatos: controle TSV SHA-256
`0DD9A6EB1E5EEC72D309CCACDC7E7C69D632CF8E02722EB4EB77E14D4052D013`, resumo do
controle `6E817B35190ACBC726EC70D09C2E8516D2002AF3C97643BC188CAA0EF8D60A04`,
relatório original `EC69F3A7D0A3F56FB0178AE5A39B68258FFE749F57F5C0BEDCB9A27E93C99E3C`,
resumo original `9487A2A19F9F9FC8E9403C068FBA54A895289695C3BF2055D6AEDB0FF4D63C21`,
readiness `A5731AC06117F6BE6B98BCCC5259519E4697F51FBC0215672FEB5349D5E1C2B0` e
DLL Release `A4A05B3728A9D724F7B69CAB6E17CB42756BA2FF4EA2DA70E7D00DE5B480C55F`.

Validação final: Release `0/0`, self-test `76/76`, testes Python `35/35`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A ISO permaneceu sem
escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar evidência original independente de geometria, UV,
material, áudio ou animação ainda não decodificada, mantendo `not-proven` e
sem inventar bindings.

## Etapa 0.47 — inventário literal dos estados EAGL originais — 2026-10-07

Foi adicionada `eagl-original-state-token-audit`, que reabre o encadeamento
EAGL original e cataloga os nomes literais dos estados sem interpretá-los. Os
`2364/2364` primitivos têm estado válido; `752` linhas contêm
`SHAPENAME`/clamps, `1612` contêm `SetPrimitiveType` e o catálogo observado tem
`9` nomes de atribuição. O controle negativo encontrou `0` linhas com tokens
literais de textura, material, coordenada UV, atributo de vértice ou
deformação.

Esse zero é apenas uma observação do vocabulário textual dos estados; não prova
ausência de semântica nos bytes de comandos opacos. Nenhum nome, clamp ou
estado foi promovido a geometria, UV, material, skinning, rigging, animação,
morph ou GPU. O subgate
`rendering-original-state-token-negative-control` é
`verified-structural-negative`, enquanto renderização principal continua
`not-proven`, `original_semantics=not-claimed` e readiness `NOT_READY`.

Artefatos: auditoria TSV SHA-256
`28FB82B938346D64FE88EB7A8ECBC99463A84EC603740A7F184228786610B9BF`, resumo
`ADE5409FE2F120B76794EB6DCADD98BAF8A2C2719A97C3348C04BD68C127AACB`, controle
independente `680E54D4878C3CA561237554F30CB0C5AA5C0917F5A39B9F6F8888412DA7FBE9`,
resumo do controle `099BFFB9C1AEC88F31ECFF0EC048C4D7E760CDBC471641B92ED76F8213E4028D`,
readiness `D9CCE7315FBBDBAC5FE043C632464B7C3DCFCC2B9F14AA65E8AFCF2112C6A0A0` e
DLL Release `582EB7872C8EC8E983EFAF9E7249DE077C98F0818000D6E95BF4873350EB594B`.

Validação final: Release `0/0`, self-test `77/77`, testes Python `36/36`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A ISO permaneceu sem
escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: procurar evidência original fora do vocabulário textual dos
estados, especialmente uma ligação verificável entre comandos opacos,
payloads e imagem/atributos, mantendo `not-proven` e sem inventar bindings.

## Etapa 0.48 — junção independente por objeto entre cadeia de renderização e payload `__VB` — 2026-10-07

Foi adicionado `eagl_original_render_payload_object_join.py`, que reparseia e
une independentemente os relatórios TSV da cadeia EAGL original e dos payloads
numéricos `__VB` pela identidade bruta `(model_archive, model_object)`. O
controle encontrou `883/883` objetos unidos, `0` objetos somente no relatório de
renderização, `0` somente no relatório de payload, `2364` primitivas e `11820`
streams validados. As contagens estruturais e os statuses foram coerentes em
`883/883` objetos; os controles de chave invertida e deslocada deram `0/0`.

Isso é evidência independente de identidade e coerência de contadores no nível
de objeto. Não associa primitiva a stream e não atribui posição, geometria,
UV, material, atributo de vértice, skinning, rigging, animação, morph ou GPU.
O resultado é `verified-independent-original-render-payload-object-join`, com
`original_semantics=not-claimed`; o gate principal de renderização continua
`not-proven` e o readiness continua `NOT_READY`.

Artefatos: relatório TSV SHA-256
`D33B945D730077098359A129AE5E52EF0AFEA661EDF33EF4C910AF343D170985`, resumo
`6B95F71A207E2E0DEEFDE91A2E8E2C6F0216444B9F434110C2F772BAFA673DEE`, readiness
`0DE5599D135AEEEA38EA42840012ECA0E63CDAAF0AF08059BE793094A1FB08EA` e DLL
Release `8C2381ED22645B458B2EC4C273270D09A31C94AFEB7FBDF44B82C3F4F5F1A2CC`.

Validação final: Release `0/0`, self-test `77/77`, testes Python `37/37`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A ISO permaneceu sem
escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: buscar associação original independente entre primitivas,
streams e atributos ou comandos/payloads, sem promover semântica enquanto a
relação não for demonstrada.

## Etapa 0.49 concluída — topologia estrutural primitiva/stream `__VB`

Foi adicionado o audit `eagl-original-primitive-stream-topology-audit` e o
controle independente `eagl_original_primitive_stream_topology_control.py`.
O relatório reproduziu `2364` primitivas e `11820` streams, com `11820/11820`
ordinais correspondentes, `11814/11820` strides correspondentes,
`11820/11820` ranges de payload válidos, `2362` primitivas completas,
`883/883` objetos sem sobreposição de ranges e controle de slots invertidos em
`0`. O controle de ordinal deslocado encontrou `1479` linhas: a repetição é
ambiguidade observada, não uma associação promovida.

O status é `verified-bounded-original-primitive-stream-topology`, com controle
`verified-independent-original-primitive-stream-topology-control` e
`original_semantics=not-claimed`. Isso comprova apenas a topologia estrutural
entre fetch slots, categorias/ordinais opacos, strides e ranges de payload.
Não decodifica nem atribui posição, geometria, UV, material, atributo de
vértice, skinning, rigging, animação, morph ou comandos GPU. O gate principal
de renderização permanece `not-proven`, o readiness permanece `NOT_READY` e o
próximo passo é obter evidência independente que discrimine os layouts
repetidos antes de qualquer binding.

Artefatos: relatório TSV SHA-256
`6A9CF9A6B92939552050C499261876724191D518DB1470E79CD7338F67A1BEE0`, resumo
`C10159DE7C3A7CC38EE8625E91D7D05679187092E534E51840B256899B0D4612`, controle
`4FFFE8E3C76D7C9389F44AD60C0F43EBBDC114D339C531012AC1B84BC837FA97`, resumo do
controle `777AA578F901AA94FEE0EC3307300C4A5DE9F6BF4E66F73BCFB9D675853479B9`,
readiness `7818A02C248A03351C6902C96EC8324BC755AA92FE70C35D1C7F4D36F051709A`
e DLL Release
`C873228A606ED79CB02F79B0258748220FE22EAEB1ECE2E14C813A79E143D294`.

Validação final: Release `0/0`, self-test `78/78`, testes Python `38/38`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A ISO permaneceu sem
escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 0.50 concluída — controle negativo de comandos/payloads opacos

Foi implementado o audit C# `eagl-original-primitive-stream-opaque-discrimination-audit`
com o controle independente `eagl_original_primitive_stream_opaque_discrimination_control.py`.
O audit reparseia, no mesmo objeto EAGL bruto, as palavras dos comandos de
fetch e as compara somente com os registros/descritores `__VB` validados e com
uma janela limitada de bytes de payload. Isso é uma discriminação opaca e
bounded; não é decodificação de comando nem atribuição de semântica.

Resultado comprovado: `388/388` pacotes de modelo, `883/883` objetos,
`2364` linhas primitivas, `2362` fetches completos, `10240` candidatos
ordinais, `10220` candidatos por stride, `0` matches por metadata, `0` matches
por prefixo de payload, `0` linhas discriminadas unicamente por metadata ou
payload e `1479` candidatos de stride deslocado. No ordinal corrente foram
preservados `2364` candidatos, com `0` matches por metadata e `0` por prefixo
de payload; no controle independente, `2364/2364` linhas foram coerentes.

O resultado negativo é a evidência desta etapa: não houve sinal bruto que
separe os ordinais/layouts repetidos por essas comparações. Status do audit:
`verified-bounded-original-primitive-stream-opaque-discrimination`; status do
controle: `verified-independent-original-primitive-stream-opaque-discrimination-control`;
`original_semantics=not-claimed`. Nenhuma posição, geometria, UV, material,
atributo de vértice, skinning, rigging, animação, morph ou comando GPU foi
inventado. A linha de readiness ficou `verified-structural-negative`, mas o
readiness geral continua `NOT_READY` e a renderização original continua
`not-proven`.

Artefatos e hashes: relatório
`845C2A9AA02ED9FBB6F2CC3DD73E143A7822735E19F3E1A65E55BC703F68007B`, resumo
`B857C8D31DFE2CF42A63E0AEA79AFA4697F0D1D86BC430AD8018757C2968BBC9`, controle
`00C438AB565BC6774E2E22F3929FB9F7F6B3B611C0FF59351E37F2766B1ACB5A`, resumo
do controle `496ED3A7CB97A1E4EB5E05F88102636FB5D3A86EF1002C05F399BA9CD2FFE1A8`,
readiness `C056B78C45EB75DAF0CEB31320F10E6FFAC852C6FC5301595E7CAD92748DF43D`
e DLL Release `8209763D4DD2AD6328309FCC5440495CCC04E0B3ED37603296DF9B4A5E85A959`.

Validação final: Release `0/0`, self-test `79/79`, testes Python `39/39`,
matriz `completed-authored-and-candidate-opt-in`, smokes relevantes, runtime
headless/windowed e replays determinísticos passaram. A ISO permaneceu sem
escrita: `2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próxima etapa: obter uma fonte independente de identidade de comando/payload
ou semântica decodificada de registro que discrimine os candidatos repetidos;
até isso existir, manter `not-proven` e não promover bindings.

## Etapa 1.08 — intervalo estrutural do catálogo no tail de `__AnimationBank` — 2026-10-08

O projeto ganhou o audit C# `fifa_pc/Runtime/AnimationBankCatalogTailRangeAudit.cs`
e o controle independente
`tools/analysis/animation_bank_catalog_tail_range_control.py`. A análise é
clean-room, .NET, somente leitura da ISO e sem execução/recompilação do XBE.

Resultado: 19/19 bancos e 1.584/1.584 entradas; header, contagem `2x`, range,
alinhamento, comprimento `8x` e relocations `2x` válidos em 19/19, com zero
intervalos sem relocations. Status fonte
`verified-structural-animation-bank-tail-range`; controle
`verified-independent-animation-bank-catalog-tail-range-control`.

Essa regularidade não é decoder nem binding: não atribui record, frame, pose,
rig, timing, skinning, morph ou seleção de animação. `candidate_record_binding`
continua `not-proven`, `original_semantics` continua `not-claimed` e a
readiness geral continua `NOT_READY`; rendering/assets/áudio original/
deformação não foram promovidos.

Validação: Release `0/0`, self-test `94/94`, matriz código 0 (`completed-
authored-and-candidate-opt-in`), 10 cenários, 777 frames, 54 eventos de áudio,
1.554 amostras de animação, smokes, runtime headless/windowed e replays
determinísticos. ISO preservada: 2.013.003.776 bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.

Próximas lacunas: estado original → draw/UV/material,
`EVT→ABKC→sample→playback` e timing/pose/rig/deformação, sempre mantendo
resultados negativos como controles e sem declarar conclusão.

## Etapa 1.09 — relocations dos pares no tail de `__AnimationBank` — 2026-10-08

Foi implementado o audit .NET `fifa_pc/Runtime/AnimationBankTailPairRelocationAudit.cs`
e o controle independente `tools/analysis/animation_bank_tail_pair_relocation_control.py`.
A fonte foi analisada em clean-room, com ISO somente leitura, `.data`, ELFs e
catálogos delimitados; não houve execução nem recompilação do XBE.

Resultado: 19/19 bancos e 1.584/1.584 pares validados; cada par possui duas
relocations nos offsets esperados, não há multiplicidade inesperada, ambos os
words estão dentro de `.data` em 1.584/1.584 linhas, zero words coincidem com
símbolos conhecidos, há 1.584 pares raw distintos e 19/19 layouts completos.
O relatório detalhado possui 1.584 linhas. Status fonte
`verified-structural-animation-bank-tail-pair-relocations`; controle
`verified-independent-animation-bank-tail-pair-relocation-control`.

O resultado não é decoder nem binding: a relação ordinal → nome do catálogo
continua não provada, assim como record/frame/pose/rig/timing/skinning/morph e
playback. Portanto `candidate_record_binding=not-proven`,
`original_semantics=not-claimed`, e a readiness segue `NOT_READY`; assets,
rendering, áudio original e deformação semântica não foram promovidos.

Validação: Release `0/0`, self-test `95/95`, matriz código 0
(`completed-authored-and-candidate-opt-in`), 10 cenários, 777 frames, 54
eventos de áudio e 1.554 amostras de animação, smokes, runtime
headless/windowed e replays determinísticos. ISO preservada: 2.013.003.776
bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximas lacunas: somente promover target-prefix/relocations com join
independente demonstrável; de outro modo, continuar estado original →
draw/UV/material, `EVT→ABKC→sample→playback` e timing/pose/rig/deformação,
sem declarar conclusão.

## Etapa 1.10 — janelas de relocation dos targets do tail — 2026-10-08

O projeto ganhou o audit .NET `fifa_pc/Runtime/AnimationBankTailPairTargetChainAudit.cs`
e o controle independente `tools/analysis/animation_bank_tail_pair_target_chain_control.py`.
Cada par raw do tail é seguido apenas por uma janela delimitada de 32 bytes em
cada target; multiplicidade, words e símbolos permanecem opacos. Não houve
execução nem recompilação do XBE.

Resultado: 19/19 bancos, 1.584/1.584 linhas e 1.584/1.584 pares com ambas as
janelas não vazias. As multiplicidades first foram `2:1, 3:17, 4:1554, 5:12`
e second `3:328, 4:1237, 6:19`. Não houve janela com exatamente uma relocation
em cada lado, resolução de segundo nível ou match com símbolo conhecido. O
status fonte é `verified-structural-animation-bank-tail-pair-target-chain` e
o controle é `verified-independent-animation-bank-tail-pair-target-chain-control`.

A evidência entrou como linha estrutural no readiness, mas não promoveu
assets, rendering, áudio original, gameplay original ou deformação: readiness
permanece `NOT_READY`, `candidate_record_binding=not-proven` e
`original_semantics=not-claimed`.

Validação: Release `0/0`, self-test `96/96`, matriz código 0
(`completed-authored-and-candidate-opt-in`), 10 cenários, 777 frames, 54
eventos de áudio, 1.554 amostras de animação, smokes, runtime
headless/windowed e replays determinísticos. ISO preservada: 2.013.003.776
bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Próximas lacunas: evidência independente de record/frame/pose/rig/timing,
estado original → draw/UV/material e `EVT→ABKC→sample→playback`, mantendo a
multiplicidade como controle e sem declarar conclusão.

## Etapa 1.11 — dispatch EAGL indexado por comando, análise estática XboxRecomp — 2026-10-09

Foi usado `sp00nznet/xboxrecomp` somente como parser/disassembler estático do
XBE original; checkout `193e2995ffaec871bb3be43a43bd66aa5fccfeb1` ficou fora do
workspace. Não foram usados o recompilador, o harness que executa código, o
runtime nativo ou saídas C. XboxRecomp corresponde ao hardware alvo Original
Xbox melhor que PS2Recomp, mas não substitui a reimplementação clean-room .NET
e seu runtime upstream não entra neste projeto.

O dispatcher em `0x0031A930` tem prefixo estrutural de 42 slots executáveis,
39 destinos distintos e word `0x00000001` não executável no índice 42. Foram
observadas 36.779 ocorrências em 2.364 streams EAGL completas e 12 IDs únicos;
12/12 indexam slots executáveis. O split de header/tamanho e índice da tabela
é evidência estática, sem decodificar comportamento de handler ou significado
de comando. As 15.288 funções sugeridas pela disassembly são boundaries
heurísticas, não prova.

Controle: `tools/analysis/eagl_original_render_command_dispatch_control.py`;
auditoria .NET em `fifa_pc/Runtime/EaglOriginalRenderChainAudit.cs` e
`ProjectReadinessAudit.cs`; relatórios TSV de dispatch em `fifa_pc/`.
Subgate `rendering-original-command-id-dispatch-control`:
`verified-structural`, `12/12`; `semantic_binding=not-proven`,
`original_semantics=not-claimed`. Rendering permanece `not-proven`; assets
`candidate-integrated`, áudio `partially-integrated`, deformação
`structural-only`, gameplay `clean-room-integrated`; readiness `NOT_READY`.

Validação: Release `0/0`, self-test `96/96`, matriz
`completed-authored-and-candidate-opt-in`, smokes relevantes, runtimes
headless/windowed e replays determinísticos. Cobertura: 10 cenários, 777
frames, 54 eventos de áudio, 1.554 amostras. ISO somente leitura:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

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

## Publicação GitHub do Fut Street 2 (2026-10-13)

- Repositório público, conforme escolha explícita do usuário: [MarllonMenezes/fut-street-2](https://github.com/MarllonMenezes/fut-street-2), branch `main`. Workspace agora tem Git local; remote `origin` aponta para esse repositório via HTTPS. Snapshot completo inicial `2d396380ea6506a569ea6ef8ea3606d185268125` enviado e verificado contra o remoto.
- Apenas 304 arquivos de código, testes e documentação. `.gitignore` usa lista permitida: ISO, XBE, arquivos de jogo, mídia extraída, exports, TSV/CSV/logs, bin/obj/caches e projeto Star Wars não entram no repositório. Nenhum arquivo local foi apagado. `.gitattributes` impede conversão automática de line endings para preservar bytes/hashes históricos.
- Checagem inicial isolada detectou a omissão de `Program.cs` no pacote; lista corrigida antes de publicar. Snapshot completo extraído sem ISO/assets/relatórios: Release 0/0, self-tests .NET 105/105 e runner 10/10. Testes Python no workspace original: 57/57. Checagens de tamanho/tipo e assinaturas de credenciais conhecidas passaram.
- Build/self-test não precisam da ISO. Matriz completa e análises de dados requerem a ISO local e relatórios intermediários que não são distribuídos; preparar essas entradas antes de tentar a matriz em um novo clone. Instruções: `docs/GITHUB_PUBLICACAO.md`.
- Commit usa GitHub noreply para não publicar o e-mail pessoal do Git global. Nenhuma licença open-source foi escolhida automaticamente. Não foi criado pull request nem iniciado code review.
- ISO preservada: 2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Upload não muda gates ou readiness: `NOT_READY`; retomar a próxima investigação da etapa 1.31.




