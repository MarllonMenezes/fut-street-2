# Continuidade do projeto FIFA Street 2 para PC

## Avanço mais recente — readiness consumindo o subgate de payloads EVT — 2026-10-06

O `project-readiness-audit` agora consome
`fifa_pc/audio-named-event-runtime-summary.tsv` e registra o subgate
`audio-named-event-runtime` como `verified-candidate`, com `17/17` candidatos,
`17/17` metadados validados, `17/17` payloads preparados e `17/17` cabeçalhos
WAVE válidos. O gate semântico `audio` permanece `partially-integrated` e
`gate_passed=false`, pois `direct_selection_binding=not-proven` continua sem
evidência independente de gameplay→evento→amostra/ABKC.

Validação real após a alteração: build Release `0/0`, self-test `72/72`, matriz
authored/candidate completa, smokes, runtime headless e replays determinísticos
passaram. Full-stack authored gerado/replay coincide, SHA-256
`727F8A81A11F2F5798E6A5E2C7488C938C98DEF0A2CCC0E7179F1A0273D8072D`; audit
candidate SHA-256 `DEF0B77FEF42B1474E58ED06CEC471BDBDB1502741D2C6CF3DCD8F9D07E8E6EB`;
DLL Release SHA-256
`A17E88262121F0C0057DE43D01F205237016F491A1BB69908BA7582E62EC5975`.
O relatório full-stack agora persiste `opponent_ai_enabled=True`, e o
readiness exige esse marcador para considerar a integração authored verificada;
isso documenta somente a IA nova do runtime managed, não regras originais.
Readiness segue `NOT_READY`, TSV SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
`original_semantics=not-claimed` permanece explícito e a ISO não foi escrita:
`2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — payloads EVT nomeados preparados no runtime managed — 2026-10-06

Foi implementado e integrado à matriz o audit
`audio-named-event-runtime-audit`. A execução real confirmou `17/17` candidatos
EVT nomeados com metadados validados, `17/17` preparados pelo runtime managed,
`17/17` cabeçalhos RIFF/WAVE válidos e `2.914.952` bytes PCM preparados. Os
artefatos são `fifa_pc/audio-named-event-runtime.tsv` (SHA-256
`1F4D60065756162D29E665DCB794D9D31F34FACC88DBD5FC38E16D70E8503CDF`) e
`fifa_pc/audio-named-event-runtime-summary.tsv` (SHA-256
`DFA25F7E59B247C836879445140D152DA65E7613534013CC52E3B7B7057211D5`).

O status é `verified-runtime-named-payload`, com proveniência
`exact-EVT-name+parallel-data-range; managed-runtime-named-payload-only` e
`original_semantics=not-claimed`. Isso comprova somente a preparação runtime
dos payloads nomeados: não comprova evento de gameplay→amostra, seleção ABKC ou
binding original. O gate `audio` permanece `partially-integrated` e
`NOT_READY`; assets continuam candidate-only, rendering segue not-proven e
animation-deformation segue structural-only.

Validação real desta etapa: build Release `0/0`, self-test `72/72`, smokes
relevantes, runtime headless, matriz authored/candidate e replays determinísticos
passaram. O full-stack authored gerado/replay coincide, SHA-256
`40C04455E9090663DE6F5ED3C950491B6D46043FD5BC472C56CC337B70B32B06`; DLL
Release SHA-256 `965FA17D8B58B56688351A3183B5EB41ED0DBA64F4E1F327D4669E7B1CC26A2F`.
Readiness permanece `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
A ISO não foi escrita: `2013003776` bytes, atributo `Archive`, LastWriteTimeUtc
`2026-01-22T10:03:26.0859951Z`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — completude dos streams candidate — 2026-10-06

O audit de proveniência candidate passou a registrar também a completude dos
streams de comandos de geometria e dos streams de estado `SHAPENAME`. A matriz
confirmou `candidate_geometry_streams_complete=24` e
`candidate_shape_state_streams_complete=24`, e o
`candidate-integrated-runtime-audit` agora exige esses dois marcadores antes
de aceitar o cenário candidate. Isso fortalece a evidência estrutural da
representação candidata, mas não promove binding de estado→geometria, material,
UV ou GPU.

Validação real: Release `0/0`, self-test `71/71`, smokes relevantes, runtime
headless e replays determinísticos; full-stack gerado/replay coincide, SHA-256
`588AD564F93E4B2BB048FB121ABF9D0C76A310F69643DB4A0A0AE3AD7730FB49`.
Relatório candidate SHA-256
`CB3364DC07F8FB90817BBB473F39D637256256AC210B6DDA747BC412B8B66863`; resumo
`00F678130853D38B3F2AB190B96F77B1CA850C81F13788ECF230B4BDC90A207D`; audit
integrado `22A7DC9360E35839C8581ECF1553A9418CFED637B0AE6DF38FC1426E00F48E3B`;
DLL Release `75137D91EECCAB5444E8FE40D5564F5FFEB95313639DAFDB1B982501819855CF`.
Readiness permanece `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
`original_semantics=not-claimed` continua explícito. A ISO não foi escrita:
`2013003776` bytes, atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — audit candidate consumindo proveniência estrutural — 2026-10-06

`candidate-integrated-runtime-audit` passou a consumir, na mesma execução, o
resumo `eagl-candidate-asset-provenance-audit-summary.tsv`. O gate candidate
agora exige `candidate_asset_provenance_valid=True` e
`candidate_mesh_state_crosscheck_valid=True`; a matriz gera o resumo antes do
audit. Resultado real: `24/24` candidatos, objetos `.ord`, companions `.orl`,
bounds e geometria finita/indexada.

Release `0/0`, self-test `71/71` e matriz completa passaram. Os relatórios
full-stack gerado/replay coincidem, SHA-256
`9569E64F059CFE2E1070448886786D19335996759054FD9B8277CF49327B071D`; o audit
candidate tem SHA-256
`42A23A7F851D59A00FACE9E72C0AF28A65DA4F5FDCC5278FD283EABE228BC97E`; DLL
Release `43E2F4FB1C9E61F3734DF3FDACE9FA5CFD6A5FF40FB272A9CB41DFDA5AEA061A`.
Readiness permanece `NOT_READY`, SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
`original_semantics=not-claimed` continua explícito; nenhum binding original de
material, UV, skinning, rigging, animação, morph ou GPU foi inventado. A ISO
permanece intacta, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço mais recente — cross-check candidate de malha e estado — 2026-10-06

`eagl-candidate-asset-provenance-audit` foi ampliado para reler, além da
cadeia `.big`→`.xsh`→SHPX→pixels, os objetos `.ord/.orl` candidate. A execução
real confirmou `24/24` objetos, `24/24` companions, bounds válidos, geometria
finita/indexada e `SHAPENAME` presente nos primitivos em `24/24` casos.
Relatório SHA-256:
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo:
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.

Build Release `0/0`, self-test `71/71` e matriz completa passaram. Os
relatórios full-stack gerado/replay coincidem, SHA-256
`5B42B2CCB1A548D0B6EDA60DF1E0DD05B4D73E06CDF9C287CE24165555B02A73`; DLL
Release SHA-256
`77921A6EFF6715DD13B808C489D75FB465B351FE3D8FB84466DC00599F3FA05C`.
Readiness: `NOT_READY`, TSV SHA-256
`8D2AC83F12F889E79E1FD8AE25C0B7088F3CB89D5C6DC9B0916E5D6897E570A1`.
`original_semantics=not-claimed` permanece explícito; o cross-check não cria
bindings de estado→geometria, material, UV, GPU, skinning, rigging, animação ou
semântica original. A ISO mantém SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa atual — cross-check persistido SHPX/TGA — 2026-10-06

`shpx-export-crosscheck` foi implementado e integrado à matriz. A execução real
releu `data/aptanims/player1.xsh` da ISO sem escrita e comparou as 46 TGAs
persistidas de `exports_fifa_layer3/player1`: 46 presentes, 46 cabeçalhos
válidos e 46/46 buffers RGBA idênticos. Relatório SHA-256:
`8EB31145E1A05CDED83C3DEA703BD1A9B24755260A650F672FC34545118AC68E`; resumo:
`B126B7D0AEA480A299F207BC6F688D82B1A95D769EEFFB58F19F063DDC3FDD4F`.

A bateria final passou em Release `0/0`, self-test `71/71`, smokes relevantes,
runtime headless e replays determinísticos; os relatórios full-stack coincidem
com SHA-256 `E65191996143C3F04C856473EB23000E0CD8C96BE936B70D810BEA308C7FCA2C`.
O resultado é somente pixel-level de exportação gerada pelo workspace
(`workspace-generated-TGA; provenance-not-independent`) e não altera
`NOT_READY` nem cria bindings originais. A ISO mantém SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Avanço atual — proveniência da cadeia candidate EAGL/SHPX — 2026-10-06

`eagl-candidate-asset-provenance-audit` verificou 24/24 cadeias same-trunk:
entradas nested `.big`, `.xsh`, payloads SHPX, entradas internas exatas e
pixels RGBA recalculados. Relatório SHA-256:
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo:
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.
Além da cadeia de origem, foram confirmados `24/24` `.ord`, `24/24` `.orl`,
bounds válidos, geometria finita/indexada e `SHAPENAME` nos primitivos. Isso é
cross-check estrutural candidate-only; não é binding de estado, material, UV ou
GPU. Status `verified-candidate-source-chain`; `candidate-only` e
`original_semantics=not-claimed` permanecem explícitos.

## Bateria final verificada da etapa — 2026-10-06

Build Release: `0/0`; self-test: `68/68`. A matriz executou nove cenários
authored e um cenário candidate opt-in; os dez pares de replay de match, input,
animação e áudio foram reexecutados com `matched=True`. O cenário candidate
produziu 140 frames, 280 samples de animação, 8 eventos de áudio, 8/8
preparados/tocados e 0 sem rota; os relatórios de integração são byte a byte
iguais, SHA-256
`24561111E7BCE6CE2A11171F0960F048FA57076677C8CCB432A3209CF1CA257B`.

O gate agregado `clean-room-authored-runtime` foi verificado em `9/9`; a
auditoria candidate gerenciada está `verified-candidate`; o readiness permanece
`NOT_READY`, TSV SHA-256
`A23E61B9474FBD07E409F0D2ED2B08CD5DC94E884429F25363CAD6289DA35222`; a ISO
continua sem escrita, `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
DLL Release usada na etapa: SHA-256
`1F9F9744A13CCF4525611154F0787560567891478100B5A42FF930C8F8E6FD64`.

## Avanço verificado — auditoria SHPX de entradas de imagem — 2026-10-06

Foi adicionada `shpx-image-entry-audit`, com self-test e integração na matriz.
A execução real releu `data/aptanims/player1.xsh` da ISO sem escrever nela e
validou `46/46` entradas PAL8 de `128x128`, tabela monotônica e tamanho
declarado consistente. O relatório `fifa_pc/shpx-image-entry-audit.tsv`
preserva o hash da origem, o hash da tabela ordenada e o hash RGBA de cada
entrada; o SHA-256 da origem foi
`4ADAFBC2B34EA033F0E51A883F89F151B634EAF3D7A52B279E8DC12939B5C3C7`.

O status `verified-image-entries` é estritamente estrutural/visual. Não cria
binding de timing, pose, rigging, skinning, animação, material, UV, morph ou
GPU; `timing_evidence=not-proven` e `original_semantics=not-claimed` continuam
explícitos.

O `project-readiness-audit` passou a cruzar o relatório candidate gerenciado no
gate separado `assets-candidate-runtime`, verificado em `24/24` e `passed=True`.
Isso melhora a rastreabilidade do caminho candidate sem promover `assets` ou
`rendering`, que continuam `candidate-integrated`/`False` e `not-proven`/`False`.

## Atualização verificada — intake de timing externo — 2026-10-06

Foi adicionado `external-animation-timing-audit` para validar observações
temporais externas, sem convertê-las em frames, poses, rig, skinning ou
semântica original. O workspace não contém a entrada externa; o relatório
registrou `status=missing-evidence`, `0` observações e `0` capturas,
mantendo `direct_timing_binding=not-proven`.

Relatório SHA-256:
`3E82F4FF84E4095CA2844950A97A56FB5F165954BF3B3379A2BD4ED0DC026EDC`;
resumo:
`4FF80AD21FB2848191CC38519BE7E6BA9E86E3B34C23FA259879B8908836928E`.

## Atualização verificada — integração candidate opt-in — 2026-10-06

A matriz repetiu um cenário cross-system com material candidate e ambos os
slots habilitados. Os quatro replays deram `matched=True`; foram
observados 140 frames, 8 eventos de áudio, 8/8 preparados/tocados, 0 sem
rota e checksum `088EC5D10096A738`. Os relatórios gerado/replay têm
SHA-256 igual:
`F8F03C7DC83BFD33EAD3D58526C1B19A1790A44209C7BF938819F55AB754187A`.
É integração candidate opt-in, não binding original;
`render_group_state=unproven`.

A auditoria gerenciada `candidate-integrated-runtime-audit` confirma flags
candidate, hash do assembly, frame PPM, traces e replays. Seu relatório tem
SHA-256 `EAA0FCF14FD4C9F90CBE71C4B801A3A9B4898DF15ABAB2164C7C7B08A8B9C93A`,
status `verified-candidate` e `original_semantics=not-claimed`.

## Atualização verificada — correlação exhaustive de áudio — 2026-10-06

O novo comando `audio-event-payload-wave-correlation-exhaustive-audit` tentou
todos os índices candidatos dentro de `512` por banco: `368` candidatos em
`16` bancos, `239` decodificados, `0` pares PCM idênticos e `0` hashes de
evento correspondentes. A medição é estrutural/negativa e mantém a seleção
original como `direct_selection_binding=not-proven`.

Relatório SHA-256:
`BDE7CF7E58E09E17D2BBB8932E413249D51264424E5A1C32E8B5E30D404E345E`;
resumo SHA-256:
`DD798636EC35EF78E07E47176A0220C9A919601F2FF47D5CD51FE8DB41DEF23B`.

## Atualização verificada — cenário first-to-goals integrado — 2026-10-06

O cenário authored `first-to-goals` entrou na matriz cross-system: `80`
frames, `HOME 1 - 0 AWAY`, `FULL_TIME`, `7` eventos de match/áudio e `160`
samples de animação. Os quatro replays foram `matched=True` e o frame PPM
`960x540` passou com `341.782` pixels authored visíveis.

Os relatórios de integração gerado/replay têm SHA-256
`D37356F7DE6F7EE9F04FFB83BC1D5973F6C278C1F3EAAC9C59D3D98A3089F119`.
As coberturas de gameplay e áudio agora são `9/9`, `49/49` eventos e `0`
sem rota; `original_semantics=not-claimed` permanece.

## Atualização verificada — invariantes de gameplay authored — 2026-10-06

O comando `clean-room-gameplay-invariant-audit` verificou `9/9` cenários
authored determinísticos, totalizando `677` frames e `49` eventos. Foram
checados estado finito, bounds de posição/altura da bola, relógio e placar
monotônicos, sequência de eventos, término esperado e ações requeridas.

Relatório `fifa_pc/clean-room-gameplay-invariants.tsv`, SHA-256
`B6C4F29036835A22BFB36D6401F2C31A37B6C66DC62D3EB477A8C5396C6433EE`.
É uma verificação do runtime authored novo e mantém `original_semantics=not-claimed`;
o gate geral de gameplay continua `NOT_READY`.

## Atualização verificada — correlação estrutural de áudio ampliada — 2026-10-06

O controle `audio-event-payload-wave-correlation` recebeu o parâmetro
`--max-bank-attempts`; a medição registrada usou `64` tentativas de banco.
Foram decodificados `17/17` candidatos EVT e `15/16` bancos ABKC/AST, sem
pares PCM idênticos e sem hashes de evento correspondentes (`0/0`). O resumo
registra `bounded_policy=first_64_bank_candidates`.

Relatório SHA-256:
`8ED4E39C6748CEC54706D9F107DCFE7D887C941ED43FDF854829C60568104E3D`;
resumo SHA-256:
`4D949566DB53FA348C939388D280D94C9F825BB19BB19BA6B31FAA70C2C2A38D`.
É evidência estrutural/negativa apenas; a seleção original permanece
`direct_selection_binding=not-proven`, e `original_semantics=not-claimed`.

## Atualização verificada — cobertura integrada authored por cenário — 2026-10-06

`CleanRoomIntegratedCoverageAudit` agora cruza os nove cenários authored com
seus relatórios de renderização, personagem/cena, animação, áudio e quatro
traces/replays. A auditoria verifica somente a integração do runtime managed;
não é binding de asset nem semântica do jogo original.

Foram verificados `9/9` cenários, `677` frames, `1.354` samples de animação e
`49` eventos de áudio. O relatório
`fifa_pc/clean-room-integrated-coverage.tsv` tem SHA-256
`DC6E37059884B9AC450D4FD73738D9D294EC8CE1A08C707CCD49AA61EC876A7B`.
Os nove cenários também geraram PPMs authored `960x540` válidos, com `341.782`
pixels visíveis no frame final de cada cenário.
Release `0/0`, self-test `65/65`; o full-stack authored-only gerado/reexecutado
é byte a byte igual, SHA-256
`BF2F85A42960ED5DCA0E1C84DB4481BAA66749263C31FDA6708C51224C67589A`.
Cada integration report registra também o SHA-256 do assembly Release atual:
`9CF41DD8555721A043BF58C6456755D2ABA3970EE417D73653EB0006775F37BB`.
Readiness `NOT_READY`, com o gate `authored-integrated-coverage` verificado e
TSV SHA-256 `EDE51254C0D754EF46463183ADE17FCA6104B941E06032506BA96535494983AC`.

Isso não altera as lacunas de evidência independente para bindings originais;
`original_semantics` permanece `not-claimed`.

## Atualização verificada — manifesto de personagem authored — 2026-10-06

`CleanRoomCharacterAssetManifest` organiza os IDs authored
`clean-room-character-silhouette-v1` e `clean-room-character-palette-v1` sob
`clean-room-character-manifest-v1`. É uma camada do PC novo; não decodifica
nem reivindica mesh, material, UV, skinning, rigging, morph, animação ou GPU da
ISO.

Release `0/0`, self-test `62/62`. O authored-only de 140 frames registrou
`authored_character_asset_frames=140`, checksum `F26A04CC3CBEBDEA` e relatório
SHA-256 `C0969EFA056C678494D70E97941A6EB6D1748E448F640FD2F84B4291F1900F33`.
O full-stack candidato preservou `088EC5D10096A738`; os quatro replays foram
`matched=True`, com relatórios iguais e SHA-256
`BF9FA14EB4CC5EC1580535EF05D4F97AA0F256278FA9E7CE3311F71E5EE82DE8`.
Readiness `NOT_READY`, agora com `character-authored-assets=verified-authored`,
SHA-256 `6C86A5895D66F822D0E06A2A41C89F04C37001FA0B829DE132431BC08B507330`.
ISO preservada: `2013003776` bytes, `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

O próximo trabalho continua sendo evidência independente para os bindings e
semânticas originais. Os manifests authored não promovem os gates de assets,
rendering, gameplay, áudio ou deformação de animação.

## Atualização verificada — manifesto de animação authored — 2026-10-06

O novo `CleanRoomAnimationAssetManifest` organiza os perfis procedurais
authored do animador managed sob `clean-room-animation-manifest-v1`, incluindo
as ações de gameplay já cobertas e uma resolução explícita para `Unknown`.
Não há leitura de AXT nem afirmação de frames, timing, pose, rigging, skinning,
morphs ou semântica original.

Release `0/0`, self-test `60/60`, headless de 80 frames com `6/6` eventos de
áudio authored, full-stack de 140 frames e checksum
`088EC5D10096A738`; replays determinísticos de match, input, animação e áudio
passaram. Os relatórios de integração coincidem byte a byte, SHA-256
`70DE5BAB17EDE85DDF847F8C1D8B5185E944AD4051AFFF534CE2AAE4B93AAA83`.
Os smokes authored de `JUGGLE`, `STEAL` e `GAMEBREAKER` também passaram.
Readiness `NOT_READY`, SHA-256
`CE868EA45F8CA772764D1BA8296A7BD7200EA2575150D572084DA0F887A8D153`.
ISO preservada, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização verificada — manifesto de áudio authored — 2026-10-06

O manifesto `clean-room-audio-manifest-v1` organiza os oito candidatos de cue
da política authored e é validado antes do mixer preparar cada candidato.
Full-stack: `8/8` preparados/tocados, `0` sem rota, PCM de `1.180.164` bytes e
SHA-256 `2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`.
Release `0/0`, self-test `61/61`, replays determinísticos `matched=True`.
Relatórios de integração iguais, SHA-256
`4DD380D1896481F5EEE4CACF1E46E1D3E4B2BF5CF5B42847C6FF924174B2B0F0`.
Readiness `NOT_READY`, SHA-256
`F485A2845003F98867ECCF31496761971AE6C574B06CA9EDACEF19BDEE471327`.
O manifesto é authored e não é binding original EVT/ABKC/sample.

## Atualização verificada — correlação estrutural de áudio e bateria final — 2026-10-06

O novo controle `audio-event-payload-wave-correlation` decodificou `17/17`
candidatos EVT e `15/16` bancos ABKC/AST sob limite explícito; não encontrou
pares PCM idênticos. A evidência permanece estrutural/negativa e não define
seleção de evento, sample, gameplay ou semântica original. Relatório SHA-256:
`00251215E62B4437C5B0EE3917C12E8DF66C1E278F1982EAF3AE95232E0432BD`; resumo:
`C5AEC72F58CAC11EFD2942D6F598C3401A5433475E4A3C7EF728D7893E6448F4`.

Release: `0/0`; self-test: `59/59`; smokes reais e runtime headless passaram.
O full-stack de 140 frames teve replays determinísticos de match, input,
animação e áudio byte a byte iguais. Readiness: `NOT_READY`, SHA-256
`4982801DDA56466467E7DC6197BDB22EF481EE41BFF5FE07F971BDB27B8B672D`.
A ISO segue intocada, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização verificada — digest PCM do áudio authored — 2026-10-06

O mixer .NET agora registra o PCM concatenado das cues authored reproduzidas no
gameplay. Foram verificados `1.180.164` bytes com SHA-256
`2729BC1961EF4E469732FA539754A1E28C9FBA55F5E6BEF47D8B58FF21C1A77E`, igual no
replay. O par de relatórios de integração tem SHA-256
`706E895FE9BAFD6A34BF6FB5C74A4C93110F1B6ECE193C18E8C625CD74C57823`.
Isso não decodifica seleção original EVT/ABKC. Release `0/0`, self-test `59/59`,
readiness `NOT_READY`, SHA-256
`336D91B1DB56E0BDF32C96BC0F093E707DA63EA04C7D06302AF3EB1A4CDBDFE9`.

## Atualização verificada — manifesto de assets authored da cena — 2026-10-06

Quadra, gols e bola agora são assets authored managed descritos pelo manifesto
`clean-room-court-v1|clean-room-goal-v1|clean-room-ball-v1` em
`CleanRoomSceneAssets.cs`. A execução verificou `140/140` frames e preservou o
checksum `088EC5D10096A738`. Os relatórios de integração coincidem byte a byte,
SHA-256 `CA03EC0029301E7336E0CA839DAE717A60260F2F130584E6C5051AA0AA8E72FD`.
Readiness `NOT_READY`, SHA-256
`661F2013630F999E2A79E27682A64253E7DA671D946ADD183B8E0FA0E22976E1`.
Nenhum binding original foi inferido.

## Atualização verificada — formatos XPR0 — 2026-10-06

O decoder managed de `XPR0` foi ampliado para `R5G6B5`, `A8R8G8B8`,
`X8R8G8B8`, `DXT1`, `DXT3` e `DXT5`, com fixtures no self-test. A alteração
expande a decodificação de payload no nível base e não inventa bindings de
material, UV, GPU, mesh ou semântica original. As texturas reais
`titleimage.xbx` (`128x128`, `DXT1`) e `saveimage.xbx` (`64x64`, `DXT1`) foram
revalidadas pelo `texture-preview` em Release, sem escrita na ISO.

Build: `0/0`; self-test: `58/58`; readiness: `NOT_READY`. A ISO preserva o
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Atualização estrutural — âncoras de payload — 2026-10-06

O auditor `animation-bank-catalog-payload-anchor-audit` encontrou `19/19`
bancos completos, `1584/1584` linhas com duas âncoras `_e/_r`, `3168` offsets
e ordem crescente em todos os bancos. Os gaps são apenas intervalos observados
entre strings do payload; não são boundaries de records nem semântica de
frame, pose, rig, timing ou skinning. Relatório SHA-256
`B63D77DF1E99F08576829FB016A1DEE5CB4C71F97F92E75E442826A36E481FBD`, resumo
`372E58900EF0F173652F923DD3F49BC777B76006437D53675D91C662626E0ECA`.
Readiness `NOT_READY`, SHA-256
`C733311CF7340C1A8B5E0C8170B2534BFAB7F0CAA30426B810FEFDB3B85F3198`;
self-test `58/58`.

## Atualização da etapa — 2026-10-06

Foi implementado o controle TSV
`animation-morph-relocation-profile-audit`, sem modificar a ISO ou aplicar
qualquer binding. Ele compara exclusivamente os conjuntos de gaps de
relocação presentes no catálogo estrutural morph com os histogramas de gaps
dos bancos de animação. A execução real encontrou 594 linhas morph em 393
layouts, 393 layouts parseáveis, 19 bancos válidos, 53 layouts com algum
banco contendo o conjunto de gaps, 0 igualdades exatas e 159 pares de
contenção. Esses números medem recorrência sintática entre relatórios já
observados; não são timing externo, vínculo morph→animação, decoder de frame,
pose, rig, peso, skinning, material, UV, GPU ou comando. O readiness segue
`NOT_READY` e a área de animação/deformação segue `structural-only`.

Artefatos novos:

- `fifa_pc/Runtime/AnimationMorphRelocationProfileAudit.cs`
- `fifa_pc/animation-morph-relocation-profile-audit.tsv`
- `fifa_pc/animation-morph-relocation-profile-audit-summary.tsv`

O `project-readiness-audit` agora consome esse resumo como controle de
proveniência e o registra na nota `animation-deformation`, incluindo
`393` layouts parseáveis, `159` pares de contenção e `0` igualdades exatas.
Isso melhora a rastreabilidade do relatório sem alterar o gate: não existe
binding de morph para animação, pose, rig, timing ou skinning.

Fechamento verificável desta etapa: Release passou sem avisos/erros; self-test
passou `48/48`; os smokes de animação (`19/19`), morph (`391/391`), material
candidate (`24/24`), geometria (`24/24`, sem rejeições) e EVT nomeado (`17/17`)
passaram. O preview candidate authored produziu `F2DD798FDD206BD9`, o headless
com candidato produziu `29EACF0AE7B12B58`, o cenário com oponente/áudio
`651E41284E9242BC`, e os replays juggle/rodada retornaram `matched=True` com
checksums `9F0F1D791CE6070F` e `404D21B5A0571826`. A ISO permaneceu em
`2013003776` bytes, timestamp `2026-01-22T07:03:26.0859951-03:00`, atributo
`Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Depois, a integração opt-in `--candidate-home-character` foi adicionada ao
preview, headless e host Win32. Ela usa o candidato same-trunk como slot home
rígido e mantém o jogador away authored; não aplica qualquer rig, skinning,
morph ou animação ao candidato. O preview produziu
`AB8D372F87FAA6B5` e o headless de 80 frames `DB0F059C0AC82133`.
O cenário combinado de 140 frames com oponente authored, `--play-audio`,
material candidate e candidate-home produziu `2641FCCE8245A44B`, com `3`
cues candidate preparados/tocados e `4` eventos sem rota.
Em seguida, o mixer recebeu uma política authored explícita para as ações
conhecidas (`intro`, `attackgen`, `panna`, `attackgoal`, `goal`, `gb_goal`,
`tackle` e `outro`). O mesmo cenário foi repetido com `7` cues preparados e
tocados, `0` eventos sem rota e o mesmo checksum; isso não é binding de evento
ou seleção de áudio original.
O cenário authored curto de `0,25` segundo verificou `FULL_TIME -> outro` com
uma cue preparada/tocada, zero eventos sem rota e checksum
`F39849417B16C774`.
As 14 rotas authored agora têm uma fonte única em
`fifa_pc/Runtime/CleanRoomAudioCuePolicy.cs`, consumida pelo mixer, self-test
e readiness; `UNKNOWN` continua sem rota.

Este arquivo transfere o contexto técnico para outro chat. Leia-o antes de
alterar o projeto; depois confira os arquivos e o estado atual do workspace,
pois este resumo não substitui a inspeção do código.

## Objetivo e limites

- Projeto ativo: FIFA Street 2, em `D:\projeto zeta`.
- ISO de trabalho: `D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso`.
- Objetivo do usuário: reconstruir o jogo para PC em .NET, sem emulação.
- O `default.xbe` disponível é binário compilado. Não há código-fonte nem
  projeto/build originais conhecidos. Portanto, o trabalho atual é uma
  reimplementação clean-room, não uma recompilação literal do código original.
- A imagem ISO deve ser tratada como somente leitura. Não extrair/alterar ou
  redistribuir conteúdo do jogo sem solicitação explícita.
- O projeto Star Wars presente na pasta ainda não foi iniciado; o foco atual é
  FIFA Street 2.

Não afirmar que o jogo completo está pronto ou que pode ser jogado como o
original. `run-windowed` e `render-preview` são infraestrutura/cena de teste;
os OBJ exportados são candidatos de diagnóstico, não assets finais.

## Estado validado na última sessão

- `dotnet build -c Release fifa_pc\FifaStreetPc.csproj`: sucesso, 0 avisos,
  0 erros.
- `dotnet run -c Release --no-build --project fifa_pc -- self-test`: 48 testes
  passaram (RefPack, BIG, SHPX, APT, ELF/EAGL, vertex buffers, XPR, áudio,
  backend de catálogo de áudio, gameplay simples e framebuffer).
- `render-preview` agora aceita `--authored-hud` para QA visual offline. A
  execução com personagens clean-room e material candidato gravou
  `fifa_pc/candidate-preview-authored-hud.ppm`, exibiu quadra, title image,
  mesh candidato, placar, tempo e medidor authored, e produziu checksum
  `F2DD798FDD206BD9`. A inspeção visual cobre somente a apresentação nova do
  runtime; não prova UV, material, rigging, skinning, animação ou GPU da ISO.
- A bateria final após essa integração passou em Release (`0` avisos/erros),
  self-test (`47/47`), smokes de animação/material/geometria/morph/áudio e nos
  headless default, gamebreaker, replay e material candidato. Os checksums
  foram respectivamente `C0249FD97A01C3F4`, `3311E2054549E154`,
  `2C3F702AB025DA33` e `CFA4BCBEE162C121`; o readiness segue `NOT_READY`.
  A ISO continua com SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- O controle estrutural novo `animation-morph-relocation-profile-audit` foi
  repetido no estado atual: 594 linhas em 393 layouts morph, 393 parseáveis,
  19 bancos válidos, 53 layouts com contenção, 0 igualdades exatas e 159 pares
  de contenção. Isso é sobreposição opaca de gaps entre relatórios separados,
  não binding morph→animação ou decoder de frame/pose/rig/timing/skinning.
- A etapa mais recente também foi validada em Release: a pose authored agora
  possui perfis de ação, o smoke visual produziu `C0249FD97A01C3F4` (e
  `29EACF0AE7B12B58` com material candidato), e o encerramento curto produziu
  `FULL_TIME` uma vez, `match_frame=16` e checksum `082272ACA18EFFE9`. Os
  smokes estruturais e de áudio permaneceram sem rejeições novas.
- A correspondência estado→geometria foi ampliada para comparar também os
  símbolos de relocation carregados nos comandos. Nos 269 pares reais houve
  `0` pares com símbolo compartilhado, além de `0` igualdades raw/boundary e
  prefixo máximo de 8 bytes; isso reforça a evidência negativa sem promover
  nenhum comando a semântica de GPU.
- O host Win32 agora aceita `--match-seconds <segundos>` com a mesma validação
  positiva do headless e cria a partida com o relógio authored correspondente.
  Isso mantém `FULL_TIME`/`match_result` consistentes entre os dois hosts, sem
  mudar a classificação de regras originais.
- O comando `project-readiness-audit` agora materializa
  `fifa_pc/project-readiness.tsv`: a ISO está verificada pelo tamanho e SHA-256,
  assets são `candidate-integrated`, renderização é `not-proven`, gameplay é
  `clean-room-integrated`, áudio é `partially-integrated` e
  animação/deformação permanece `structural-only`. A auditoria não trata essas
  categorias como prontas e registra, para cada uma, a evidência que ainda
  falta; nenhum binding de UV, material, skinning, rigging, timing ou GPU foi
  inventado.
- O novo `eagl-state-geometry-order-audit` preserva somente os intervalos da
  ordem observada entre grupos geométricos candidatos e primitivas com
  `SHAPENAME`. A execução real encontrou 38 grupos, 180 primitivas de estado,
  62 intervalos, 36 intervalos não vazios, 180 estados particionados sem sobra,
  streams completos nos 36 runs não vazios e 17 perfis opacos. Isso é controle
  de ordem/completude, não associação de estado a mesh, material, UV, skinning,
  rigging, animação ou GPU; rendering continua `not-proven`.
- A execução real de 80 frames em `--demo` passou a reportar frame 80,
  sequência de ações 5, última ação `GOAL_HOME`, posse `None`, um drible e
  o histograma `GOAL_HOME=1,KICKOFF=1,PASS=1,PASS_COMPLETE=1,SHOT=1,TRICK=1`.
  A mesma telemetria foi observada nos modos de material candidato e áudio;
  ela descreve somente a simulação clean-room.
- O modo opt-in `--aimed-demo` exercita a mira vertical clean-room no loop
  headless: emite `MoveY=0.75` no disparo, conclui o mesmo gol e produziu o
  checksum `6251CBFCB6A56869`. O demo padrão mantém `MoveY=0` e o checksum
  `5405DE72BE1B4FC1`; nenhum dos dois resultados é binding de regra original.
- O modo opt-in `--round-demo` agora exercita o ciclo clean-room pós-gol:
  intervalo determinístico de 0,5 s, reposicionamento, nova posse e ação
  `RESTART`. `GameRuntime.Report()` expõe rodada, reinício pendente e a flag do
  modo. Em 120 frames com áudio WinMM, produziu rodada 2 e checksum
  `8107B8CF3046E9EB`. Isso amplia a simulação nova sem afirmar semântica
  original de kickoff; o caminho padrão não mudou.
- O runtime authored agora possui o modo separado `--gamebreaker-demo` e a
  oitava coluna opcional `gamebreaker` nos TSVs. Quatro `TRICK` carregam um
  medidor PC-only até 100; o disparo emite `GAMEBREAKER`, zera o medidor e
  chega a `GOAL_HOME`. A fixture de 40 frames
  `fifa_pc/gameplay-clean-room-gamebreaker-input.tsv` produziu 7 eventos; o
  trace e o replay coincidiram com SHA-256
  `BC17D33716D221F1495434E12D4C3836957FB4BB83D0E9AA73AAC8CC7B43A4EB`, e a
  fixture tem SHA-256
  `81F9DFDC8AB37681F5309C92D1BD8704333258E08876A621CE92172E18A67D6C`.
  Esse é comportamento novo da simulação clean-room: não é controle, medidor,
  animação, áudio ou binding GPU decodificado da ISO; o evento fica sem rota
  authored no mixer.
  Com `--clean-room-characters`, os checksums verificados foram
  `3311E2054549E154` no demo de 80 frames e `2C3F702AB025DA33` no replay de 40
  frames; ambos medem apenas a apresentação PC authored.
- O host Win32 agora encaminha a tecla `G` para `MatchInput.Gamebreaker` quando
  a janela está em foco. Isso integra o controle authored ao caminho interativo
  além do headless; continua sendo uma escolha nova do runtime managed e não
evidência de input ou regra original.
  O self-test verifica essa ponte com um estado de teclado sintético.
- O host Win32 agora encaminha também `N` para `MatchInput.Panna`; o self-test
  verifica `N` e `G` isoladamente. São controles authored do runtime e não
  evidência de mapeamentos de input originais.
- O caminho Win32 passou a desenhar um HUD authored no framebuffer (`PreviewRenderer`
  com flag explícita): placar, tempo e barra do medidor. O self-test verifica a
  diferença visual sem alterar os checksums headless; textos/glyphs são UI nova,
  não interpretação de textura, fonte, layout, material, UV, animação ou GPU da
  ISO.
- `render-preview` agora aceita `--authored-hud` para materializar esse mesmo HUD
  em um PPM de QA visual offline. A opção permanece explícita, não altera o
  headless e não promove a UI authored a evidência da ISO.
- A apresentação `--clean-room-characters` ganhou um perfil procedural authored
  para `GAMEBREAKER`, coberto por self-test e separado dos payloads AXT/morph.
  Isso integra a ação ao renderer managed sem inferir pose, rig, skinning ou
  timing original.
- O `project-readiness-audit` agora registra também
  `authored_gamebreaker_trace_replay=verified`, `events=7`, presença de
  `GAMEBREAKER` e `GOAL_HOME` quando os dois traces authored estão presentes.
  Isso melhora a cobertura do artefato sem alterar `NOT_READY` nem atribuir
  semântica original à mecânica.
- O runtime authored ganhou o modo opt-in `--juggle-demo`, a nona coluna TSV
  `juggle` e a tecla `J` no host Win32. Quatro `JUGGLE` elevam a bola e formam
  uma cadeia determinística antes do chute; a fixture de 56 frames produziu 7
  eventos e o replay esperado reportou `matched=True`. O readiness registra
  `authored_juggle_trace_replay=verified`; a mecânica, a altura e a pose são
  authored PC e não bindings de input, física, frame, animação, áudio ou GPU da
  ISO.
- O mesmo `--round-demo` foi reexecutado com trace authored: 7 eventos,
  `match_round=2`, `match_last_action=RESTART`, checksum
  `404D21B5A0571826` e `matched=True` ao usar
  `--match-trace-expected`. Os artefatos
  `fifa_pc/gameplay-round-demo-trace.tsv` e
  `fifa_pc/gameplay-round-demo-replayed.tsv` coincidiram com SHA-256
  `B3BDFEEC8FD2EB6479E7B797C1031AE9EE348451C4BFE4C4E172B993E8872EBB`.
  Isso é determinismo authored e não semântica original de kickoff.
- `run-headless` aceita `--match-seconds <segundos>` para um relógio authored
  de teste. Em 30 frames com `--demo --match-seconds 0.25
  --clean-room-characters`, o runtime reportou `match_ended=True`,
  `match_last_action=FULL_TIME`, `match_result=DRAW` e checksum
  `082272ACA18EFFE9`; ticks posteriores não avançam o frame. É uma transição
  clean-room, não tempo ou regra original;
  `FULL_TIME` continua sem rota de áudio (`unmapped-clean-room-event`).
- A pose authored de `--clean-room-characters` agora possui perfis determinísticos
  para posse, passe, drible, chute, gol, reinício e `FULL_TIME`, com movimento
  de braços, tronco, cabeça e pernas. O smoke de 80 frames produziu checksum
  `C0249FD97A01C3F4`; com material candidato, `29EACF0AE7B12B58`. Isso integra
  apresentação e gameplay novos, sem interpretar AXT/morph como animação.
- O renderer software e o host Win32 agora aceitam `--clean-room-characters`.
  Essa opção desenha uma pose procedural authored de corpo, cabeça, braços e
  pernas, com orientação Home/Away e acentos de posse/ação determinísticos.
  O self-test verifica a pose e o framebuffer; isso é apresentação nova do PC,
  não decoder ou binding de AXT, morph, rigging ou skinning.
- `run-headless` aceita `--frame-output <saida.ppm>` para materializar o último
  frame da cena em QA visual, recusando o caminho da ISO como destino. O artefato
  não altera o gate de assets candidatos nem cria semântica de GPU.
- `StreetMatch` agora preserva um trace authored imutável de eventos com
  sequência, frame, placar e posse; o headless exporta esse trace por
  `--match-trace-output <saida.tsv>`. A execução real de 80 frames gerou 6
  eventos (`KICKOFF`, `TRICK`, `PASS`, `PASS_COMPLETE`, `SHOT` e `GOAL_HOME`)
  em `fifa_pc/runtime-gameplay-trace.tsv`, com SHA-256
  `2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`.
  Isso é telemetria clean-room para verificar a integração nova entre
  gameplay, renderização e áudio; não é lista de eventos original nem binding
  de cue.
- A rota de áudio clean-room foi ampliada de forma explícita para
  `TRICK -> panna` e `PANNA -> panna`, além de `GOAL_HOME -> goal` e
  `GOAL_AWAY -> goal`.
  `panna` é uma row nomeada observada/decodificada; a associação é uma escolha
  authored do runtime, não um binding original. No demo padrão, 2/5 eventos
  têm candidato preparado e 3 ficam sem rota; com AI, 3/6 são preparados e 3
  ficam sem rota. O self-test separa rota preparada, candidato indisponível e
  evento sem rota.
- `run-headless` agora aceita `--input-script <entrada.tsv>` para consumir uma
  sequência authored de seis colunas (`frame`, `move_x`, `move_y`, `pass`,
  `shoot`, `trick`) ou uma sétima coluna opcional `panna`. O leitor valida frames contíguos, eixos finitos em
  `[-1,1]` e booleanos permitidos; o exemplo
  `fifa_pc/gameplay-clean-room-input.tsv` foi executado em 8 frames e exportou
  `fifa_pc/gameplay-script-trace.tsv`. Como a fixture termina antes da posse,
  o trace contém somente `KICKOFF`; isso é validação do parser/runtime novo,
  não evidência de regra ou input do original.
  Os SHA-256 verificados foram
  `B2F2969BFF1FA22387158B197EEF09479D8BDD682D26B6AB139B11F0A2AE22E8` para a
  fixture e `6288D477F63B66EB445F24E6C55D0F3BD0635B0D0BF7F5480CAD10754AD16D52`
  para o trace.
- A fixture authored de 80 frames `fifa_pc/gameplay-clean-room-demo-input.tsv`
  também foi executada pelo novo leitor de arquivo. Ela reproduziu o cenário
  `--demo` com checksum `C0249FD97A01C3F4`, os 6 eventos esperados e
  `GOAL_HOME`; o trace equivalente manteve SHA-256
  `2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`.
  Isso confirma a integração da entrada authored com a simulação clean-room,
  não uma reconstrução de input ou regra original.
- A fixture authored de 140 frames
  `fifa_pc/gameplay-clean-room-round-input.tsv` também pode ser combinada com
  `--round-demo`, mantendo o input explícito separado da política de reset.
  A execução produziu 7 eventos, rodada 2, `RESTART`, checksum
  `404D21B5A0571826` e `matched=True`; os traces tiveram SHA-256
  `B3BDFEEC8FD2EB6479E7B797C1031AE9EE348451C4BFE4C4E172B993E8872EBB`.
  Isso continua sendo comportamento authored do PC.
- O relatório `project-readiness.tsv` registra esse gate como
  `authored_round_trace_replay=verified` e `authored_round_trace_events=7` na
  nota de gameplay. Essa verificação é do runtime novo e não promove regras
  originais nem altera `NOT_READY`.
  O self-test cobre esse gate com dois traces sintéticos idênticos e exige a
  presença de gol e `RESTART` antes de registrar a verificação.
- `run-headless` agora aceita `--match-trace-expected <entrada.tsv>`. O arquivo
  esperado é validado antes da abertura da ISO e comparado linha a linha com o
  trace authored reexecutado; divergência informa a primeira linha e termina
  com falha, e a ISO ou o mesmo caminho de saída são recusados. A fixture de 80
  frames foi reexecutada com `matched=True`, 6 eventos e checksum
  `C0249FD97A01C3F4`; o arquivo
  `fifa_pc/gameplay-script-demo-replayed-stage.tsv` coincidiu byte a byte com
  `fifa_pc/gameplay-script-demo-trace.tsv`, SHA-256
  `2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`. Esse
   gate verifica determinismo e integração da simulação authored, não replay,
   input, evento ou semântica do jogo original.
 - A camada gameplay ganhou a regra authored opt-in
   `--match-rule first-to-goals --target-goals <N>`. Com a fixture de 80 frames
   e alvo 1, o runtime terminou em `HOME 1 - 0 AWAY`, emitiu `FULL_TIME` no frame
   70 e manteve relógio restante; o caminho padrão `TIMED` preservou o checksum
   `C0249FD97A01C3F4`. A escolha foi tratada como referência comportamental
   externa limitada, documentada em `docs/FIFA_EXTERNAL_BEHAVIOR_EVIDENCE.md`,
   e não como regra, limite ou timing decodificado da ISO.
 - O fixture authored `fifa_pc/gameplay-clean-room-panna-input.tsv` cobre a
   sétima coluna opcional `panna` e `--match-rule goals-or-panna`. Em 31 frames
   produziu `PANNA`, `FULL_TIME` no frame 31 e preparou uma row `panna`; o
   `GameRuntime` passou a drenar todos os eventos novos do trace, inclusive
   dois eventos emitidos no mesmo tick. Isso corrige a entrega ao consumidor de
   áudio authored sem criar binding de cue original.
 - A validação final desta etapa foi repetida após a regeneração dos relatórios:
   Release e self-test `45/45` passaram; os smokes relevantes de animação,
   SHPX/geometria, material, geometria, atributos, correspondência de estado e
   áudio retornaram exit 0. O cenário `goals-or-panna` entregou 2 eventos de
   áudio, preparou 1 candidato e encerrou no frame 31; AI+áudio produziu
   `651E41284E9242BC`, 7 eventos, 3 preparados e 3 reproduzidos. A auditoria
   permanece `NOT_READY`. A ISO manteve tamanho `2013003776`, timestamp
   `2026-01-22T07:03:26.0859951-03:00`, atributo `Archive` e SHA-256
   `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`;
   `IsReadOnly=False` é preexistente e nenhum caminho da aplicação escreve nela.
 - A integração de catálogo agora valida os `.ast` durante `GameAssetCatalog.Scan`.
  Na ISO de trabalho foram aceitos 6/6 streams `SCHl`; `CatalogAudioMixer` monta
  e, quando recebe o opener read-only do host, pré-decodifica os seis streams
  para PCM/WAV preparado. O catálogo também correlaciona exatamente 17 nomes
  da string table PA com candidatos header/data; os 17 envelopes são legíveis e
  7 têm canais no header observado; os outros 10 têm hipótese mono única
  validada por layout/frames `SCDl`. As associações de gameplay clean-room implementadas
  explicitamente são `GOAL_HOME -> goal`, `GOAL_AWAY -> goal` e
  `TRICK -> panna`: o headless real prepara as rows nomeadas e o host Win32
  pode enviá-las ao WinMM de forma assíncrona. `panna` é uma escolha authored
  para o evento `TRICK`, não um vínculo original. Os demais eventos continuam
  sem binding demonstrado ou mixagem automática.
- A nova auditoria `audio-sccl-channel-layout-audit` validou os 144 ranges PA
  sem tag de canais observado como mono único (144/144), com 0/144 hipóteses
  estéreo válidas e nenhum caso ambíguo. O catálogo preserva `channels=0` no
  header, mas prepara os 10 candidatos nomeados usando o campo separado de
  layout de payload; a preparação subiu para 17/17 e 2.914.952 bytes PCM.
  Isso é evidência de payload e não binding EVT/ABKC/gameplay.
- O mesmo catálogo agora valida cabeçalho, dimensões e payload dos `XPR0`.
  A ISO contém 2 texturas (`saveimage.xbx` e `titleimage.xbx`) e ambas foram
  aceitas (2/2) durante o `run-headless` real; a decodificação DXT1 também foi
  exercitada durante a montagem (2/2 `texture_decodes_validated`).
- `GameAssetCatalog.Scan` também monta os catálogos estruturais de
  `data/anims.big` durante o runtime: o headless real reporta 19 catálogos com
  contagens válidas, 19 pares `.ord/.orl`, 19 ELF válidos, 19 símbolos
  coincidentes e 19 hashes `.data` normalizados distintos. Isso é integração de
  asset estrutural; não é seleção, playback ou decoder de frames/poses.
- O novo `animation-bank-runtime-smoke` confirma que o catálogo preserva as
  linhas observadas dos `.axt`: 19 bancos, 1.584 entradas, 1.578 nomes
  distintos, uma tag de versão e 19/19 contagens, pares, ELF e símbolos
  validados. Nomes e timestamps permanecem metadata estrutural, sem semântica
  de frame, pose, rig, timing ou skinning.
- O novo `animation-bank-name-index` grava as 1.584 linhas dos catálogos em
  `fifa_pc/animation-bank-name-index.tsv`, com banco, versão, diretório,
  timestamp, identidade ELF/símbolo e recorrência textual entre bancos. Foram
  observados 1.578 nomes distintos e 6 nomes em mais de um banco. O artefato
  serve para selecionar amostras de investigação, sem promover nomes a frames,
  poses, rig, timing ou skinning.
- O novo `animation-bank-catalog-compare` compara somente os catálogos `.axt`:
  produziu 19 perfis e 171 pares; os 6 nomes compartilhados entre bancos têm o
  mesmo timestamp observado, mas houve 0 conjuntos de nomes idênticos, 0 nomes
  na mesma posição e 0 prefixos ordenados comuns. Os TSVs de perfis e pares são
  diagnóstico textual/temporal, não decoder de frames, poses, rig ou timing.
- O novo `animation-bank-elf-structure` materializa headers de seção e
  relocations dos ELF dos bancos: 19/19 ELF válidos, 114 linhas, 6 seções por
  banco, 19 perfis de seção distintos e 19 perfis de relocation distintos.
  Os relatórios preservam nomes/tipos/flags/tamanhos/offsets observados, sem
  interpretar payload, `.data` ou símbolos como frames, poses, rig ou timing.
- O novo `animation-bank-data-relocation-audit` mede somente os offsets
  relativos e gaps adjacentes das relocations que apontam para `.data`: 19/19
  ELF válidos, 35.900 relocations, 19/19 bancos com relocations, 19 perfis de
  offsets distintos e 15 contagens distintas de gaps. O maior banco tem
  29.152 offsets distintos e 291 gaps distintos; o TSV também preserva o
  histograma e o hash offset/tipo, sem decoder de payload ou semântica de
  frame, pose, rig, timing ou skinning.
- O novo `eagl-morph-animation-name-negative-control` cruzou, sem normalização,
  os 2.360 nomes completos de shape dos 594 morphs com os 1.578 nomes de
  arquivo e 19 nomes de banco dos catálogos de animação. Houve 0 coincidências
  exatas em ambos os conjuntos. Isso é somente um controle lexical negativo:
  não prova ausência semântica nem cria binding de animação, pose, rig ou timing.
- O novo `animation-bank-tail-compare` compara os 32 bytes iniciados no
  símbolo `__AnimationBank:::`. Não houve pares crus iguais; após mascarar
  relocations, houve 12 pares iguais, 30/32 offsets comuns em todos os bancos e
  a word little-endian nos offsets 4–5 igualou `2 * catalog_file_count` e
  `2 * declared_file_count` em 19/19 casos. O contador declarado no cabeçalho
  também igualou o número de entradas parseadas em 19/19. É correspondência
  numérica comprovada por duas fontes de contagem, não semântica de campo nem
  decoder de frames, poses, rig ou timing.
- O novo `animation-bank-sidecar-boundary-audit` comparou a divisão física dos
  19 pares `.ord/.orl`: não houve seção ELF cruzando a fronteira; em 19/19
  casos, somente a seção `.data` file-backed não vazia ficou em `.ord`, e as
  outras quatro seções, a seção de relocation e a tabela de seções ficaram em
  `.orl`. O relatório preserva ranges e até 16 bytes de contexto de cada lado,
  sem converter sidecar, relocation ou bytes em records, frames, poses, rig,
  timing ou comandos.
- O novo `eagl-morph-sidecar-boundary-audit` aplicou o mesmo recorte somente
  aos 594 objetos com símbolo `__Morph:::` comprovado: 391 BIGs aninhados
  válidos, 5 seções file-backed não vazias por objeto, 1 em `.ord` e 4 em
  `.orl`, relocation e tabela de seções em `.orl` em 594/594 e 0 seções
  cruzando a fronteira. Os 886 pares encontrados apenas pelo sufixo nominal
  `_morph.o` não foram tratados como morph comprovado. Isso é composição física
  de sidecar, não vínculo de morph, mesh, pose, rig, peso, frame ou skinning.
- O novo `eagl-morph-shpx-structure-audit` comparou os 594 rows morph com os
  pacotes `.xsh` same-stem de `data/models.big`: 591/594 foram validados, 3
  ausências foram preservadas (`tops0001`, `tops0020`, `tops0210`) e 0 pacotes
  foram rejeitados. Dezessete dos 393 layouts têm perfis SHPX variáveis, todos
  dentro dos 38 layouts ambíguos. Isso é evidência de header/tabela SHPX, não
  binding de morph, textura, material, UV, mesh, pose, rig ou GPU.
- A auditoria `eagl-morph-shpx-geometry-correlation-audit` juntou os relatórios
  SHPX e geometria candidata por layout: 393/393 layouts unidos, 17 com ambas
  as camadas variáveis, 21 somente com geometria variável e 355 estáveis. Nos
  38 layouts ambíguos, 17 caem na classe de co-variação. Isso é uma seleção
  estrutural para investigação futura, não um pareamento SHPX→mesh ou binding.
  Os SHA-256 dos relatórios são
  `BB9EE9ED18DEB9007685B02C8C8EDE48E8B9E9EE9A6722179F19449A43507B7B` e
  `9D28EDB9056D7FC32982587071F3E4E7435FBD2C5630122AE71925655390CF5E`.
- O novo `eagl-morph-elf-header-audit` validou 594/594 objetos morph e encontrou
  um único perfil de header core, range de tabela e metadata `__Morph:::` em
  cada um dos 393 layouts, inclusive nos 38 ambíguos. Isso é controle negativo
  de header, não decoder de payload, pose, rig, peso, frame ou timing.
- O scan também percorre, sem extração persistente, os dois pacotes APT
  `fe.bh`/`ig.bh`: 284 BIGs internos, 1.106 entradas, 284 `.apt` e `.const`,
  279 `.o`, 259 `.xsh`, 259 conjuntos completos e 230 assinaturas compostas
  foram validados no runtime. Os 279 objetos não expõem `__Morph:::`; isso é
  controle estrutural negativo, não prova de ausência de animação.
- O comando `apt-animation-negative-control` cruza os relatórios estruturais
  APT com `animation-bank-name-index.tsv` sem comparar words como campos:
  foram 284 linhas APT, 279 objetos válidos, 259 conjuntos completos e 1.584
  linhas de catálogo de animação; os resultados foram 0 `__Morph:::`, 0
  candidatos `*_morph` e 0 coincidências textuais de stems com nomes de banco
  ou nomes de arquivo. O relatório é
  `fifa_pc/apt-animation-negative-control.tsv`; isso não prova ausência de
  animação nem decodifica timing, pose ou frame.
- O novo `apt-cross-package-profile-audit` compara a chave exata de header APT,
  header `.const` e extensões entre os perfis FE/IG: 80 chaves FE, 51 IG, 27
  compartilhadas, 53 exclusivas FE e 24 exclusivas IG. Nenhuma chave
  compartilhada teve word alinhada igual ou símbolo `__Morph:::`; isso é
  recorrência estrutural, não timing, pose, frame ou animação.
- O `GameAssetCatalog.Scan` agora repete também o inventário same-trunk de
  material candidato no runtime: 24 matches, 24 objetos distintos, 1 shape
  (`tops`), 24 texturas decodificadas e 24 texturas `PAL8` foram observados no
  headless real. Esse catálogo só registra a cadeia textual e a decodificação
  validada; não cria UV global, material GPU, skinning, rigging ou animação.
- `StreetMatch` agora tem posse explícita (`Home`, `Away`, `None`), passe com
  trajetória até o adversário, drible com contador e ações observáveis; o chute
  mantém a transição de bola para gol. O self-test cobre movimento, drible,
  passe completo, chute e HUD, e o renderer colore a bola conforme a posse.
  Fora de passe/chute em voo, a bola acompanha a posição do jogador que detém a
  posse; essa invariável é coberta no self-test e pertence somente à simulação
  clean-room.
  O chute Home também aceita `MoveY` no instante do disparo, adicionando uma
  componente vertical determinística à trajetória. Isso é controle novo da
  reimplementação clean-room, coberto por self-test; não é binding ou semântica
  decodificada da ISO. O demo padrão usa `MoveY=0`, preservando a trajetória reta
  e o checksum headless de referência.
  A nova telemetria `ActionCounts` registra `KICKOFF` e as transições da
  simulação; o relatório headless também expõe frame, sequência, última ação,
  posse, dribles e posições finais dos jogadores/bola. Os asserts do self-test verificam `TRICK`, `PASS`,
  `PASS_COMPLETE`, `SHOT` e `GOAL_HOME` sem atribuir semântica ao jogo
  original.
- Foi acrescentado o modo opcional `--opponent-ai` ao runtime: depois que a
  posse da casa é observada, o adversário pressiona, pode recuperar a bola e
  finaliza para o gol esquerdo. A execução real de 140 frames produziu
  `HOME 0 - 2 AWAY`, `SHOT_AWAY=2` e `GOAL_AWAY=2`; o modo padrão não mudou.
  Isso é gameplay clean-room novo e verificável, não uma alegação sobre regras,
  IA ou eventos decodificados do original.
- `render-preview` passou a carregar e blitar `data/titleimage.xbx` depois da
  validação do catálogo; jogadores e quadra permanecem cena clean-room de teste,
  sem apresentar a textura como prova de geometria EAGL. O self-test agora
  verifica também o blit de textura no framebuffer.
- A mesma prévia standalone agora aceita `--candidate-material [nested.big]
  [SHAPENAME] [objeto.ord]`. A amostra `tops0030.big/tops` produziu mesh de
  2 grupos, 341 vértices, 337 triângulos e checksum `C1DB074C7140C585`; o
  estado textual do objeto é exato, o estado dos grupos permanece `unproven` e
  a saída recusa a própria ISO. Isso estende a integração candidata sem criar
  UV, material GPU, skinning, rigging ou semântica original.
- O host windowed carrega `star0001-head.ord` via `RefPack`/BIG/ELF e desenha a
  malha candidata preenchida com cor provisória e contorno sobre o jogador;
  `eagl-candidate-smoke` verifica o mesmo caminho em modo não interativo (3
  grupos, 534 vértices, 528 triângulos). Isso ainda não é o modelo final:
  materiais, skinning, deformação e rigging continuam pendentes.
- `eagl-candidate-preview` também produziu um PPM real combinando a malha
  candidata, a cena de gameplay e `titleimage.xbx`; o resultado é uma prévia
  de integração visual, não uma validação de topologia/material final.
- Após a preservação do atributo genérico do slot 1, a mesma prévia foi
  regenerada em `fifa_pc/candidate-integrated-preview-slot1.ppm`; ela mantém
  3 grupos, 534 vértices e 528 triângulos e continua sendo apenas uma
  verificação do caminho de renderização gerenciado.
- A opção diagnóstica `eagl-candidate-preview ... --slot1-profile` agora
  colore cada triângulo pela média dos dois componentes do `Slot1Attributes`;
  `fifa_pc/candidate-slot1-profile-preview.ppm` foi gerado e o self-test
  confirma que esse caminho alcança o framebuffer. É uma visualização do
  atributo, não um material ou uma textura final.
- O comando `eagl-candidate-smoke --commands` agora expõe os bytes crus dos 75
  comandos do `star0001-head` por primitiva. Os cinco slots `id=9` e seus
  strides permanecem reproduzíveis; os valores auxiliares não coincidem
  diretamente com offsets/tamanhos dos `__VB` e continuam sem semântica.
 - O smoke específico agora aceita `eagl-candidate-object-smoke ... --commands`,
  permitindo repetir a preservação de IDs, bytes, relocations e estados em um
  BIG/ORD escolhido. Em `tops0030` foram observadas 13 primitivas e 13 fluxos
   completos; as primitivas geométricas `PT_TRIANGLESTRIP` e as primitivas TAR
   com `SHAPENAME=tops,1` continuam separadas, sem binding inventado.
 - O comando `eagl-candidate-state-correspondence` compara, em 24 objetos
   `tops`, as primitivas geométricas com as primitivas que carregam
   `SHAPENAME`, apenas por bytes, forma de fronteira, prefixo e IDs crus. Foram
   observados 38/180 primitivas e 269 pares; 0/269 bytes exatos, 0/269 formas
   de fronteira exatas, 269/269 fluxos completos e prefixo máximo de 8 bytes.
 - O controle `eagl-state-geometry-adjacency-audit` materializa a comparação
   por objeto em `fifa_pc/eagl-state-geometry-adjacency.tsv`: 24/24 linhas,
   0 pares com o mesmo índice, 48 pares adjacentes, 269 pares na mesma camada
   e 0/24 grupos geométricos com `SHAPENAME` exato. É um controle negativo,
   não uma heurística convertida em binding.
   É evidência estrutural/negativa, não associação de estado a mesh ou GPU.
 - O controle `eagl-state-geometry-profile-audit` acrescenta a comparação
   opaca de sequência de IDs/tamanhos, contagem e bytes dos comandos por par.
   Nos 24 objetos houve 269 linhas, 0 sequências exatas, 0 pares no mesmo
   índice, 48 vizinhos, 1 perfil geométrico distinto e 3 perfis de estado;
   4 linhas tinham perfil de estado não repetido, sem igualdade de sequência.
   Isso continua sem associação de estado a mesh, material, UV ou GPU; o
   relatório é `fifa_pc/eagl-state-geometry-profile-audit.tsv`.
 - O controle `eagl-state-geometry-order-audit` materializa, em
   `fifa_pc/eagl-state-geometry-order-audit.tsv`, os intervalos ordenados por
   camada. Foram produzidas 62 linhas em 24 objetos: 36 intervalos não vazios
   e 180/180 primitivas com `SHAPENAME` particionadas; os 36 runs não vazios
   têm streams completos e há 17 perfis opacos. A auditoria registra somente
   índices, fronteiras, contagens, bytes e completude; não promove ordem a
   binding de estado, material, UV, skinning, rigging, animação ou GPU.
 - O novo `eagl-candidate-attribute-audit` mediu 24 objetos, 38 grupos e
   7.403 valores slot-1: 38/38 grupos têm perfil completo, finito e próximo
   da faixa unitária. Nenhum grupo carrega diretamente `SHAPENAME` ou clamp,
   enquanto 180 primitivas carregam esses estados; a auditoria não promove
   slot-1 a UV nem cria binding.
- A etapa adicional de companions paralelos também foi compilada e testada:
  `audio-structure-smoke` confirma `pcpahdr/pcpadat` em 283/283 ranges e
  `djhdr_en/djdat_en` em 48/48, com todos os ranges de dados começando em
  `SCHl`; o self-test cobre a mesma regra com fixture sintético.
- Em `data/models.big`: 1.115 entradas BIGF; 391 arquivos BIG aninhados válidos,
  2.658 entradas internas; 1.772 ELF válidos; 885 caixas `__BBOX`; 11.950
  descritores `__VB` válidos; 4.780 streams float3 finitos; 2.389 float3 dentro
  das bounds de objeto; 2.390 primitivas de render.
- O leitor encontra layout coerente para 2.387 primitivas com cinco slots do
  comando `id=9` e strides correspondentes aos grupos `__GPvb`; as 1.615
  primitivas explicitamente `PT_TRIANGLESTRIP` passam essa checagem. Isso não
  prova por si só o binding efetivamente usado no draw call.
- O diagnóstico verbose agora expõe também o terceiro word de cada `id=9`
  (`AuxiliaryValue`). Em `star0001-head`, ele repete em alguns slots e é zero
  em outros, mas os valores observados não coincidem diretamente com
  `ByteLength`, `DataOffset` ou offsets dos records `__VB`; portanto continuam
  sem semântica e não foram usados para ligar a malha ao draw.
- O leitor EAGL agora preserva o fluxo de comandos de cada primitiva como
  evidência neutra: índice, offset, ID, tamanho, bytes completos e relocations,
  além de um indicador de término/erro. O parser de `id=9` reutiliza essa mesma
  cadeia, e o self-test cobre fluxo completo e fluxo truncado. No objeto real
  `star0001-head.ord`, são 5 primitivas, 5/5 fluxos completos e 75 comandos,
  com IDs observados `2=5,4=5,5=3,6=4,9=25,14=5,15=5,16=3,20=6,26=5,29=5,31=5`.
  Esses IDs ainda não recebem semântica de textura/material/draw sem evidência
  independente.
- O inventário global passou a contar essa evidência semântica neutra em todos
  os objetos: 2.390/2.390 fluxos completos, 0 incompletos, 37.111 comandos;
  distribuição `2=2.389,4=2.390,5=1.616,6=1.970,9=11.948,14=2.390,
  15=2.390,16=1.616,20=3.232,26=2.390,29=2.390,31=2.390`. Isso confirma
  cobertura estrutural do parser, não o significado dos comandos nem a
  reconstrução final dos draws.
- A relação de fetch de vértices agora é materializada em
  `EaglVertexFetchBinding`: slot, stride e símbolo `__GPvb` precisam coincidir
  para os cinco slots. O inventário confirma 2.387/2.390 primitivas com esse
  binding explícito; o mesh candidato usa o stream do slot 0 nesses casos.
  Isso ainda não prova material, índice, textura, skinning ou binding completo
  do draw.
- O leitor de vertex buffers agora mede também streams finitos `float2` e
  expõe somente o perfil numérico de cada faixa, com uma classificação
  conservadora `near_unit_range` (tolerância de 0,01), sem chamá-los de UV.
  No inventário real há 2.390/2.390 streams `float2` finitos e dentro dessa
  faixa. Entre as primitivas com os cinco bindings explícitos, 2.387/2.387
  têm um `float2` finito no slot 1 e 2.387/2.387 permanecem na faixa unitária;
  772 delas também carregam os tokens de estado
  `XBOXEXTOBJ_SetClampU/V`; 772 também expõem `SHAPENAME` no mesmo perfil.
  Isso fortalece a hipótese de atributo de textura para esse slot, mas não é
  prova independente de semântica UV nem de qual imagem deve ser ligada ao
  valor de `SHAPENAME`; o renderer continua sem material final.
- A medição compacta foi aplicada também ao inventário global: os 2.387 draws
  com cinco bindings produzem 7.161 perfis finitos, distribuídos em
  `slot 0 / 3 componentes = 2.387`, `slot 1 / 2 = 2.387` e
  `slot 3 / 3 = 2.387`. Isso confirma repetição estrutural da leitura, não a
  semântica dos atributos nem o binding final do draw.
- Foi adicionado `EaglMorphReader` para objetos ELF standalone que contêm
  `__Morph:::`. Ele valida somente strings nomeadas e relocations internas:
  no inventário real, 594 objetos têm o símbolo, com 2.372 nomes `BS_*`,
  1.142 nomes de atributo (`Coordinates`/`Normals`), 4.719 ponteiros internos
  e 3.514 destinos resolvidos; as janelas limitadas também somam 4.719 campos
  relocados, com 2.372 links `BS_*`, 1.142 links de
  `Coordinates`/`Normals` e 42 assinaturas de offsets observadas. A amostra
  `star0001-head_morph.o` expõe quatro
  shape names, os dois atributos e oito relocations. O modo `--payload` agora
  expõe 8 janelas hexadecimais limitadas ao redor desses campos para comparar
  agrupamentos de bytes; os ponteiros com destino textual também são separados
  em links `BS_*` e `Coordinates`/`Normals`. Records de morph, vértices
  deformados, ossos e poses ainda não foram decodificados.
- A investigação seguinte adicionou `EaglMorphSiblingReader` e os comandos
  `eagl-morph-sibling-smoke`/`eagl-morph-sibling-inventory`. Eles reconhecem
  somente o padrão nominal observado `*_morph.o` → `*.ord` + `*.orl` dentro do
  mesmo BIG, validam o ELF dividido e registram bounds/síntese de relocations.
  No inventário real, 391/391 BIGs foram lidos, 594/594 morphs encontraram
  irmãos `.ord/.orl` válidos, 594/594 tinham símbolos geométricos e 593/594
  tinham bounds; 42 assinaturas de relocação foram observadas. Isso fortalece
  a correlação estrutural, mas não autoriza aplicar payload a vértices nem
  inferir pesos, ossos, poses, frames ou animação.
- O `GameAssetCatalog.Scan` passou a carregar somente um resumo estrutural dos
  mesmos irmãos, e `eagl-morph-sibling-runtime-smoke` confirmou 391/391 BIGs
  internos, 594 evidências, 594 pares `.ord/.orl`, ELF e geometria, 593 bounds
  e 593 ocorrências de `__PosePaletteBank:::`. Nenhum payload é aplicado e o
  símbolo não recebe semântica de pose, osso, peso, frame ou skinning.
- O catálogo de animação agora também retém os registros ELF crus dos símbolos
  `__AnimationBank:::`: 19 registros e 19 assinaturas de metadados distintas,
  sempre com seção 1, tamanho 4 e `Info=17`. A comparação estrutural no índice
  morph não encontrou coincidência com o único registro de
  `__PosePaletteBank:::` (`table_section=4`, `symbol_index=3`, `symbol_section=1`,
  `value=0`, `size=4`, `Info=17`). Isso é controle negativo de layout ELF, não
  prova de ausência de vínculo e não cria decoder de pose/animação.
- O novo `eagl-morph-pose-bank-index` preserva esses símbolos por BIG e faz
  comparação textual e controle de metadados ELF crus com os bancos `.axt`:
  391 linhas, 1
  símbolo distinto (`PosePaletteBank`), 1 registro ELF distinto
  (`table_section=4`, `symbol_index=3`, `symbol_section=1`, `value=0`,
  `size=4`, `Info=17`) e 0 matches. O TSV agora conserva esses campos brutos;
  eles permanecem evidência de ELF e não recebem semântica de pose. Esse
  controle negativo não prova
  ausência de vínculo e não cria decoder de pose, rig, peso, frame, timing ou
  skinning.
- A assinatura relativa dos campos relocados reduz a dependência de bases
  absolutas: o perfil mais frequente reúne 173 morphs com gaps
  `0x0,0x4,0x8,0xC,0x24,0x28,0x34` e um campo posterior variável. Isso é
  repetição de layout bruto; não foi convertido em record, índice, peso ou
  atributo.
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
- Uma camada refinada resume por hash somente os headers de seção, posições e
  tamanhos dos símbolos geométricos e offsets relativos/tipos de relocation do
  ELF. Ela produziu 444 assinaturas, 447 pares morph-layout/layout e 32 layouts
  morph com múltiplos layouts companion; isso continua evidência bruta.
- Os streams de comandos foram resumidos de modo opaco por primitive/layer,
  offsets, assignments textuais, IDs/tamanhos, hashes dos bytes crus e relocation
  text. O inventário produziu 592 assinaturas, 593 pares morph-layout/stream e
  38 layouts morph com múltiplos streams, sem semântica GPU atribuída.
- Uma assinatura estrutural pareada combina somente as camadas já validadas do
  morph e do irmão: layout morph, janelas normalizadas, layout ELF, stream opaco,
  sequência de registros `__VB`, perfil numérico, topologia candidata e
  coincidências numéricas cross-file. O
  inventário resultou em 593 assinaturas pareadas; 355 dos 393 layouts morph têm
  composição única e 38 têm composições múltiplas. Essa chave organiza evidência
  repetível, mas não atribui morph, rig, pose, peso, material, UV, skinning,
  animação ou comando GPU.
- A comparação cross-file de words little-endian alinhadas confronta o payload
  morph somente com metadata numérica validada do companion, mascarando strings
  nomeadas e words de relocation. Foram observadas 406 assinaturas de
  coincidência; 12 layouts morph têm múltiplas formas. Isso é igualdade bruta de
  campos comparados, não um binding de contagem, índice, peso, pose ou frame.
- O comando de inventário aceita `--catalog <saida.tsv>` para salvar, fora da
  ISO, uma linha por morph com os nomes dos pares, estabilidade do layout e as
  assinaturas opacas. O catálogo verificado tem 594 linhas; 355 layouts são
  estáveis e 239 linhas pertencem aos 38 layouts ambíguos. Isso é seleção de
  amostras, não uma tabela de bindings.
- A opção `--ambiguity-report <saida.tsv>` resume os 393 layouts e marca os 38
  ambíguos pela camada que varia: stream de comandos (38), layout companion
  (32), registros `__VB` (27), coincidência numérica (12), janelas de payload
  (11), mesh candidato (6) e perfil numérico (5). São contagens de variação
  observada, não atribuições de semântica.
- O relatório também compara cada camada dos 38 layouts ambíguos por igualdade
  exata contra os 355 layouts estáveis: há interseção em `numeric_descriptor`
   (38 layouts), `candidate_mesh` (32), `payload_window` (26), `vertex_record`
   (25) e `companion_layout` (2), mas não em `command_stream` ou
   `companion_numeric_match`. Essa recorrência entre populações não prova
   semântica nem causalidade.
 - O comando `morph-ambiguity-summary` reagrupa somente esses rótulos e
   produziu 8 composições, cobrindo os mesmos 38 layouts ambíguos e 239 linhas.
   O resumo preserva contagens de sobreposição com controles estáveis e hashes
   opacos de layout; não combina camadas como se fossem campos semânticos.
 - O comando `morph-ambiguity-byte-summary` juntou essas 8 composições ao
   relatório byte-level por `morph_structural_layout`: 239/239 linhas tiveram
   correspondência. Foram 213/239 igualdades de bytes e limites de comando,
   0/239 assinaturas estruturais de comando exatas, 0/239 igualdades de registro,
   assinatura ou limite numérico e 222/239 igualdades de categoria valor/token.
   São métricas dos relatórios, não semântica de GPU, morph ou animação.
  - O comando `morph-ambiguity-numeric-summary` juntou o relatório de gaps pela
   mesma chave: os 17 gaps ocupam 10 layouts ambíguos em 4 composições; 28 dos
   38 layouts ambíguos não aparecem no relatório de gaps. As métricas agrupadas
   preservam 7/17 igualdades de fronteira relativa, 5/17 de janela de payload e
    0/17 de boundary numérico ou offset canônico absoluto, sem interpretar os
    tokens ou offsets.
  - O novo comando `morph-ambiguity-group-metadata` materializa uma linha por
    layout ambíguo, juntando por chave exata os perfis da matriz de ambiguidade,
    os matches byte-level e os gaps numéricos. Foram geradas 38 linhas, com
    239 linhas byte associadas e 17 linhas de gap; 28 layouts não possuem gap.
    A saída é uma matriz de contagens e fronteiras observadas, não um decoder ou
    binding de morph, pose, rig, peso, frame, timing, skinning, material, UV ou
    GPU.
  - O novo comando `morph-ambiguity-independent-audit` reagrupa o catálogo
    bruto e confere independentemente os oito contadores de perfil. A execução
    real auditou 594 linhas em 393 layouts, com 0 inconsistências de
    metadata/contagem e 0 divergências entre os rótulos `ambiguous/stable` e a
    variabilidade medida nas oito camadas. Nos 38 ambíguos, 26 têm mesh candidata estável e
    companion variável, 6 têm mesh candidata variável e 38/38 variam em alguma
    camada estrutural observada. Isso é triagem cross-layer, não binding nem
    decoder de morph, rig, pose, peso, animação, material, UV, skinning ou GPU.
  - O novo `morph-candidate-geometry-ambiguity-audit` compara separadamente a
    coluna `candidate_geometry_signature` com os rótulos já publicados: 38/38
    layouts geométricos variáveis são ambíguos, 0 são estáveis, 0 ambíguos ficam
    sem variação geométrica e há 0 divergências. Isso é apenas consistência de
    evidência candidata.
  - O novo comando `eagl-morph-name-state-audit` cruza somente os nomes `BS_*`
    preservados nos morphs com os valores textuais `SHAPENAME` dos companions.
    A execução real encontrou 594 linhas, 2.360 nomes morph distintos,
    391/594 rows com estado e 0/594 coincidências cruas ou normalizadas. É um
    controle lexical negativo; não é binding de morph, grupo, material, UV,
    peso, osso, pose, frame ou animação.
  - O novo comando `eagl-morph-animation-name-negative-control` cruzou, sem
    normalização, esses 2.360 nomes completos com 1.578 nomes de arquivo e 19
    nomes de banco de animação. Nas 594/594 linhas morph completas, houve 0
    coincidências exatas em ambos os conjuntos. Isso não prova ausência
    semântica nem cria binding de animação, pose, rig ou timing.
  - O novo comando `audio-event-named-decode-smoke` tentou decodificar os 17
    candidatos nomeados pelo EVT usando seus ranges header/data. Sete nomes
    (chance, intro, beat, goal, outro, gb_goal e miss) produziram WAVs válidos,
    totalizando 2.123.056 bytes; dez foram rejeitados porque a metadata
    observada informa zero canais. Nenhum nome foi ligado automaticamente a
    gameplay ou ABKC.
  - O novo comando `audio-abk-batch-decode-smoke` tentou índices bounded nos
    16 bancos ABKC: 15 bancos produziram WAV, 1 foi rejeitado por ausência de
    candidato (`data/audio/CONTROL.abk`), totalizando 10.051.852 bytes, com
    fontes AST=6, BNK=9 e none=1. A evidência não cria binding de EVT/gameplay.
  - `CatalogAudioMixer.Initialize` passou a montar esses candidatos bounded no
    runtime normal com opener read-only. O headless real reporta 15 clips ABKC
    preparados, 15 bancos decodificados, 1 rejeitado e fontes AST=6, BNK=9 e
    none=1; isso integra payload, não cues de gameplay.
  - Na mesma montagem, o mixer pré-prepara os 7 candidatos EVT nomeados com
    canais utilizáveis, reportando 7 waves e 2.122.748 bytes PCM no headless.
    Os 10 candidatos com canais zero não são forçados; a única rota de evento
    clean-room continua sendo `GOAL_HOME/GOAL_AWAY -> goal`.
- `--byte-report <saida.tsv>` acrescenta comparação alinhada dos bytes crus dos
  streams de comando e dos registros canônicos UTF-8 de coincidência numérica
  contra os 355 controles estáveis. Das 239 linhas ambíguas, 213 têm bytes de
  comando exatamente iguais a algum controle, embora 0/239 tenham assinatura
  estrutural de comando exata; 26 não têm igualdade byte-level exata e 0/239
  têm igualdade exata no registro numérico. Isso não decodifica comandos,
  materiais, atributos ou campos do morph.
- A comparação passou a registrar também a partição observada por primitiva,
  usando somente ordem, quantidade de comandos, tamanho, completude e hash
  bruto de cada segmento. O inventário encontrou 252 assinaturas de partição,
  402 pares morph-layout/partição e 9 layouts com múltiplas partições; 213/239
  linhas ambíguas coincidiram com um controle estável. A igualdade continua
  sendo evidência estrutural, não binding ou decoder.
- A coincidência numérica foi dividida em sequência de offsets (394 assinaturas,
  394 pares e 1 layout variável) e tokens valor/categoria sem offset
  (283 assinaturas, 399 pares e 6 layouts variáveis). Os tokens coincidiram em
  222/239 linhas ambíguas, enquanto o registro canônico completo permaneceu
  em 0/239; isso é normalização textual explícita, não decodificação de
  campos do morph.
- O modo `--numeric-gap-report` gera um recorte reproduzível das 17 linhas que
  não coincidiram por valor/categoria, em 10 layouts; os controles mais próximos
  formam 8 conjuntos textuais. A saída fica em
  `fifa_pc/morph-sibling-numeric-gap-report.tsv` e continua sendo triagem, não
  decodificação.
- A camada `TokenSequenceComparer` mede apenas a sequência ordenada desses
  tokens nos 17 gaps: todos mantiveram delta absoluto de quantidade igual a
  zero e prefixo comum máximo de 4 tokens; 15 tiveram 8 diferenças mínimas e 2
  tiveram 16. Na melhor comparação de cada linha, a porção textual anterior ao
  delimitador concentrou 8 ou 16 diferenças e a porção posterior teve zero;
  essa separação sintática não nomeia tokens nem cria binding. O TSV registra
  ainda os índices ordinais zero-based dessas divergências: quatro padrões
  foram observados, com frequências 7, 6, 2 e 2; são posições na sequência
  textual, não índices de geometria, osso, pose ou GPU.
- `NumericTokenSequenceComparer` compara os `CanonicalMatches` alinhados com o
  controle mais próximo usando somente offset, word observado, sufixo textual
  e os quatro bytes little-endian derivados desse word. Nos 17 gaps, os words
  divergentes foram 8 ou 16, os sufixos divergiram zero vezes, os bytes
  divergentes foram 8 ou 16, e as posições de offset divergentes ficaram em
  21, 24, 26, 42 ou 58 conforme o tamanho. Isso não decodifica nenhum campo.
- O relatório mantém também a sequência assinada de deltas entre offsets
  alinhados no campo `nearest_control_canonical_offset_delta_signature`; os 17
  gaps produziram 10 assinaturas distintas. São diferenças de limites
  observados, não deslocamentos de vértices ou animação.
- `NumericTokenSequenceComparer` também gera
  `relative_offset_boundary_signature`, removendo a base absoluta e mantendo
  somente os deltas entre offsets adjacentes observados. Nos 17 gaps, 7/17
  coincidem exatamente com algum controle estável e 10/17 não coincidem; há 8
  perfis relativos. Isso organiza fronteiras repetíveis sem chamar os campos
  de records, vértices, ossos ou frames.
- A sequência de oito hashes das janelas normalizadas também foi comparada ao
  mesmo controle: nenhum dos 17 gaps teve igualdade completa; houve de 2 a 8
  hashes diferentes, de 0 a 6 iguais e prefixo comum de 0 a 2. Isso permanece
  comparação de vizinhança normalizada, não decodificação de record.
- `--numeric-gap-correlation-report` cruza os mesmos gaps pelas 10 assinaturas
  de delta de offset e resume a recorrência independente das janelas e dos
  limites brutos. O TSV real contém 12 perfis globais de janela e 10 de limite;
  3 grupos repetem uma janela, 6 repetem um limite e somente 2/17 linhas
  coincidem com um limite de controle estável. A agregação usa hashes e
  contagens observadas, sem atribuir record, vértice, osso, pose ou comando.
  A ampliação da correlação conta separadamente, contra todos os 355 controles
  estáveis, 7/17 coincidências de fronteira relativa, 5/17 sequências completas
  de oito janelas normalizadas, 0/17 boundaries numéricos do companion e 0/17
  offsets canônicos absolutos; o limite bruto permanece em 2/17. Esses números
  não formam um score composto nem um binding.
- O perfil estrutural dos descritores `__VB` foi comparado sem nomear atributos:
  235 assinaturas, 407 pares morph-layout/descritor e 13 perfis morph com mais
  de uma assinatura de descritor. Streams, bytes, elementos, strides, flags e
  seções continuam metadata bruta validada, não posições, UVs, normais, pesos
  ou deformações.
- A sequência relativa dos registros `__VB` foi resumida por hash usando apenas
  seção, offsets relativos, descriptor/data offsets, bytes, stride e flags:
  358 assinaturas, 431 pares morph-layout/sequência e 27 layouts morph com
  múltiplas sequências. Isso continua evidência estrutural, sem nomear
  atributos ou deformações.
- O perfil numérico neutro acrescenta apenas finitude de palavras float
  empacotadas e containment de streams de stride 12 nas bounds declaradas:
  12 assinaturas, 398 pares morph-layout/número e 5 perfis morph com múltiplas
  assinaturas numéricas. Nenhum componente recebeu semântica.
- A topologia candidata do companion foi resumida somente por grupos, vértices,
  triângulos e primitivas ignoradas: 593 companions têm assinatura, 221 valores
  distintos incluindo o caso vazio, 399 pares morph-layout/topologia e 6 perfis
  morph com múltiplas topologias. Isso não é topologia original nem deformação.
- A mesma leitura agora preserva a presença de símbolos nomeados
  `__PosePaletteBank:::`: 593/594 morphs têm esse símbolo. Os perfis das
  assinaturas repetidas mostram combinações estáveis de 4 nomes `BS_*`, 1 ou
  2 atributos e o símbolo de palette; isso permanece evidência estrutural de
  símbolos/layout, sem provar palette de poses, pesos, ossos ou aplicação de
  animação.
- O modo `eagl-object-symbol-smoke ... --raw` foi usado no companion
  `star0001-head.orl`: são 6.032 bytes, com prefixo de nomes de seções
  (`.symtab`, `.strtab`, `.shstrtab`, `.data`, `.rel.data`), mas o arquivo
  isolado não é um ELF aceito pelo leitor. O diagnóstico preserva apenas esses
  bytes como evidência; nenhuma semântica de link foi inventada.
- Exportação candidata de `star0001-head`: 3 grupos, 534 vértices, 528 faces,
  dois grupos ignorados. Índices OBJ dentro dos limites; vínculo ao draw,
  semântica completa dos slots e topologia final seguem sem confirmação.
- A geometria candidata agora passa por `EaglCandidateMeshBuilder`, uma camada
  gerenciada de vértices/triângulos que o exportador OBJ reutiliza. No objeto real
  `star0001-head`, a saída nova manteve 3 grupos, 534 vértices e 528 triângulos;
  ela continua sendo candidata até confirmar binding, materiais e animação.
- Os grupos da mesma malha agora carregam 534 valores `float2` do slot 1, um
  por vértice nos 3 grupos aceitos de `star0001-head`; a associação é
  estrutural ao fetch e permanece nomeada como `Slot1Attributes`, não como
  UV. O self-test verifica contagem e valores de extremidade no fixture, e o
  smoke real confirma `groups_with_complete_slot1_attribute_profile=3`.
- O mesmo mesh candidato agora preserva perfis numéricos de todos os slots cujo
  stride completo é composto por floats finitos. No objeto real são 9 perfis:
  slots `0=3`, `1=3` e `3=3` nos grupos aceitos. Isso amplia a evidência de
  atributos ligados sem rotular componentes como UV, normal, peso ou material;
  streams não compatíveis continuam sem perfil atribuído.
- O novo `eagl-shapename-index` percorre 886 pares `.ord/.orl` e as 724
  entradas `.xsh` de `data/models.big`: encontrou 774 ocorrências de estado
  (`head,1=522`, `tops,1=252`), mas nenhum match exato de nome completo ou
  prefixo com os basenames `.xsh`.
  Isso rejeita apenas o binding textual direto; não resolve materiais nem
  escolhe texturas: `tops` é prefixo de 152 basenames, enquanto
  `head` não é prefixo de nenhum, e esse resultado é ambíguo. Os 724 pacotes
  também foram descompactados em memória: 1.545 registros internos válidos e
  14 nomes distintos; `tops` coincide com nome interno em 224 pacotes, mas
  `head` não coincide. Os prefixos de arquivo observados são `star`, `tops`,
  `tats`, `bott`, `logo` e `ball`; os estados `head` em pacotes `star` apontam
  apenas para nomes internos `face`/`hair`, sem cadeia textual exata. Nenhuma
  família adicional foi promovida a candidato.
- A primeira ligação reproduzível de recurso EAGL foi isolada em
  `EaglShapeMaterialLoader`: para `tops0030.big`, o objeto `tops0030.ord`
  contém `SHAPENAME=tops,1`, o mesmo tronco fornece `tops0030.xsh`, e o pacote
  SHPX contém o registro interno exato `tops` (offset 112, 33.024 bytes).
  O decoder PAL8 produziu uma textura 256x124 com 126.976 bytes RGBA, sempre
  em memória e sem escrever na ISO. `eagl-material-smoke` e o self-test cobrem
  o vínculo textual/estrutural.
- O objeto `tops0030.ord` também passou pelo `EaglCandidateMeshBuilder`:
  2 grupos, 341 vértices, 337 triângulos e 13 primitivas, com 11 primitivas
  carregando `SHAPENAME=tops,1`. O novo `eagl-material-preview` aplicou a
  textura decodificada ao caminho candidato de `Slot1Attributes` e gerou
  `fifa_pc/tops0030-material-preview.ppm`; os 2 grupos foram texturizados e o
  checksum do framebuffer foi `FB81D8B0DBB5E819`. Isso verifica a integração
  software do recurso, não prova que slot 1 seja UV em todos os modelos nem
  que o draw GPU original tenha sido reconstruído.
- O inventário `eagl-material-batch-smoke` generalizou essa regra sem usar
  prefixos ambíguos: 24 arquivos `.big` distintos passaram a cadeia
  same-trunk `.big/.xsh` + `SHAPENAME` normalizado + nome interno SHPX exato,
  todos na família `tops`, com decodificação PAL8 válida. A distribuição inclui
  dimensões observadas de 256x124 até 257x256. Isso fortalece a hipótese para
  uniformes, mas não extrapola para `head`, `face`, `hair` ou outros recursos.
- O novo `eagl-material-batch-preview` carregou novamente os 24 vínculos, gerou
  24 PPMs P6 e um manifesto com 0 rejeições e 24 checksums distintos. Foram
  rasterizados 14 meshes de dois grupos e 10 de um grupo, totalizando 7.403
  vértices, 7.327 triângulos e 38 grupos texturizados. Continua sendo integração
  software candidata; não prova UV, material GPU, skinning ou deformação.
- O novo `eagl-material-runtime-batch-smoke` percorre os mesmos 24 vínculos pela
  cadeia integrada `titleimage.xbx` + mesh/textura candidata +
  `SoftwareGameplayRenderer`/`PreviewRenderer`: 24/24 frames, 0 rejeições, 24
  checksums distintos e 38 grupos candidatos texturizados. Isso verifica a
  integração por lote do runtime software, sem promover o caminho a binding
  global de UV, material GPU, skinning, rigging ou comandos.
  Com `--report fifa_pc/eagl-material-runtime-batch.tsv`, cada frame candidato
  fica materializado em TSV e passa a ser consumido pelo gate de prontidão como
  rastreabilidade de runtime, sem alterar `rendering=not-proven`.
- A inspeção visual das prévias `tops0030` e `tops0040` encontrou fragmentação e
  alongamento fortes na malha rasterizada. O novo comando
  `eagl-candidate-geometry-audit` mede os 24 meshes pela mesma projeção inteira
  do renderer e grava `fifa_pc/eagl-candidate-geometry-audit.tsv`. O resultado
  foi 24/24 linhas, 0 índices inválidos, 0 vértices não finitos e 0 índices
  repetidos; houve 46 áreas 3D exatamente zero, 48 áreas XY zero e 196
  triângulos degenerados após arredondamento, contra 7.131 visíveis. Isso não
  prova topologia original nem justifica inventar reinícios, índices, UVs ou
  bindings. As versões PNG derivadas para inspeção visual
  (`tops0030-material-preview.png` e `tops0040-material-preview.png`) são
  somente artefatos de QA.
- Como recorte adicional, `MorphCandidateGeometryAudit` mede uma assinatura
  numérica dos meshes candidatos dos irmãos morph, sem alterar os rótulos de
  ambiguidade existentes. Na ISO foram observados 594 linhas em 393 layouts;
  38 layouts variam nessa assinatura e 32 deles mantêm uma única assinatura de
  mesh candidata enquanto a geometria numérica varia. Um layout tem uma linha
  sem assinatura por ausência de bounds; 393 layouts foram completos e
  ELF-validos e 392 tiveram bounds em todas as linhas. Isso continua sendo
  evidência da representação candidata, não topologia original, UV, material,
  skinning, rigging, animação ou comando GPU.
  A correlação entre as assinaturas de payload e geometria candidata encontrou
  38 layouts variáveis no par, 27 com payload estável e geometria variável e 11
  com ambas as camadas variáveis. Esse resultado é somente triagem estrutural.
- O comando `eagl-candidate-projection-preview` gera comparações neutras `xy`,
  `xz` e `yz` da mesma malha, em
  `fifa_pc/candidate-projection-previews-stage-1`. As seis vistas de
  `tops0030`/`tops0040` alteraram as silhuetas, mas não eliminaram a
  fragmentação; nenhuma delas foi escolhida como eixo, câmera ou transformação
  do runtime. O resultado continua sendo diagnóstico, sem inventar semântica
  para vértices, UVs, materiais, skinning, rigging, animação ou GPU.
- A mesma cadeia same-trunk agora é montada pelo `GameAssetCatalog.Scan` durante
  o `run-headless`, sem persistir os bytes da textura: o relatório expõe
  `candidate_material_matches=24`, `candidate_material_objects=24`,
  `candidate_material_shapes=1`, `candidate_material_textures_decoded=24` e
  `candidate_material_pal8=24`. Isso integra a presença candidata ao runtime,
  mas mantém a distinção entre textura decodificada e binding GPU/UV final.
- Uma segunda prévia independente (`tops0040.big`) manteve o vínculo e passou
  por dimensões/topologia diferentes: textura PAL8 256x256, 2 grupos, 527
  vértices, 523 triângulos, 2 grupos texturizados e checksum
  `F130B0BAB3361DDF`. O artefato é `fifa_pc/tops0040-material-preview.ppm`.
- A mesma amostra `tops0040.big` também passou pelo `GameRuntime` headless
  opt-in em 80 frames, com textura e malha candidatas validadas e checksum
  `3A2D00B09DC319F6`. Isso reforça somente a família `tops`, sem generalizar
  para `head`, `face`, `hair` ou outros prefixos.
- O próximo passo foi integrado de forma opt-in ao `GameRuntime` headless:
  `run-headless ... --candidate-material` carrega, por padrão, a cadeia
  verificável `tops0030.big` → `tops0030.ord` → `SHAPENAME=tops,1` →
  `tops0030.xsh/tops`, monta a malha e passa a textura candidata ao
  `SoftwareGameplayRenderer`. A execução real de 80 frames produziu checksum
  `CFA4BCBEE162C121`; o caminho padrão permaneceu em
  `5405DE72BE1B4FC1`. O modo aceita também
  `--candidate-material <nested.big> <SHAPENAME> [objeto.ord]` e mantém
  fallback sólido quando não há textura/malha candidata válida. O uso de
  `Slot1Attributes` como coordenadas de amostragem é deliberadamente limitado
  a este modo opt-in; não prova UV universal, material final, skinning,
  rigging, animação ou semântica de comandos GPU.
- Nesta etapa, o mesmo caminho candidato foi integrado ao host Win32 com a
  opção explícita `run-windowed ... --candidate-material [nested.big]
  [SHAPENAME] [objeto.ord]`. A opção carrega textura e mesh somente quando a
  cadeia same-trunk é validada e os entrega ao `PreviewRenderer`; sem ela, o
  host conserva `star0001-head.ord` e cor provisória. Falha no candidato
  retorna a `candidate_material_status=unavailable` e usa o fallback sólido.
  Isso conecta render candidato e janela interativa no código novo, mas não
  decodifica nem inventa UV, material GPU, skinning, rigging, animação ou
  comandos.
- A auditoria de estado adicionada ao caminho candidato confirmou
  `SHAPENAME=tops,1` em 24/24 objetos carregados, mas em 0/24 grupos
  geométricos candidatos. Em `tops0030`, as 11 primitivas com esse estado são
  separadas dos 2 grupos usados pela malha candidata; o runtime mantém a
  correlação como não comprovada e não a transforma em binding. O batch smoke
  continuou em 24/24 frames, 0 rejeições, 24 checksums distintos e 38 grupos
  texturizados, sempre como integração candidata opt-in.
- Os três grupos aceitos também preservam as atribuições de estado nomeadas e
  os IDs dos comandos de seus fluxos completos. O smoke real informa
  `groups_with_preserved_state=3` e três grupos com IDs; esses grupos usam
  `GeoPrimState`, enquanto os estados `SHAPENAME` observados estão em
  primitivas que não passaram o gate do strip candidato. A relação continua
  evidência estrutural, sem resolver textura ou material.
- A nova frente APT percorre os BIGs internos de `fe.bh`/`ig.bh` sem gravar os
  blocos. `fe.bh` contém 163 `.apt`, 163 `.const`, 161 `.o`, 145 `.xsh` e
  145 conjuntos completos; `ig.bh` contém 121, 121, 118, 114 e 114. Todos os
  headers `.apt` e `.const` foram lidos, mas os campos APT variam por família:
  em `fe.bh`, 131 `.apt` e 122 `.const` têm fields dentro do asset, contra
  32 e 41 fora; em `ig.bh`, são 87 e 82 dentro, contra 34 e 39 fora. Há 68/44
  padrões de header respectivamente. O novo inventário separa parsing de
  header de `fields_within_asset` e não interpreta os words como timing, pose,
  morph ou material. A comparação por tronco mostra ainda que headers
  repetidos aparecem com e sem `.xsh`; a presença do companion não é uma
  regra independente de animação.
- A assinatura estrutural APT recém-adicionada combina somente header APT,
  header `.const` validado quando presente, assinatura estrutural do `.o` EAGL,
  tabela SHPX validada do `.xsh`, extensões same-stem e tamanhos dos companions.
  Ela produziu 133 perfis em `fe.bh` e 97 em `ig.bh`; 161/161 e 118/118
  objetos `.o` foram lidos estruturalmente e as 145/145 e 114/114 entradas
  `.xsh` tiveram tabelas lidas, sem rejeições. Isso organiza a evidência de pacote,
  mas não atribui timing, poses, morphs, materiais ou frames.
 - O argumento `apt-structure-inventory ... --report <saida.tsv>` agora grava uma
  linha estrutural por `.apt`, incluindo validade dos companions, hash da
  assinatura de pacote e somente a presença literal de símbolos morph no `.o`.
  Os relatórios reais têm 163 linhas em `fe.bh` e 121 em `ig.bh`; nenhum dos
  279 objetos `.o` válidos expõe `__Morph:::` e nenhum stem produz candidato
   nominal `*_morph`. Isso afasta uma ligação direta entre esses `.o` APT e o
   catálogo morph, mas não decodifica animação nem prova ausência de timing.
 - O argumento opcional `--profile-report <saida.tsv>` agrupa os pacotes por
   padrão de header APT, header `.const` e extensões companion. Foram observados
   80 perfis em `fe.bh` e 51 em `ig.bh`; os 284 pacotes forneceram 568 words
   alinhados para comparação, com 0 iguais e 568 diferentes. É evidência bruta
   de não-recorrência nessa camada, não um decoder de timing, pose, frame ou
   animação.
 - A nova frente `animation-bank-inventory` lê os sidecars textuais `.axt` de
   `data/anims.big` e cruza seus nomes de banco com os pares same-bank `.ord/.orl`.
   Foram validadas 20 entradas `.axt`, 19 catálogos, 1.584 entradas de arquivo,
   19/19 ELF combinados válidos e 19/19 símbolos literais
   `__AnimationBank:::BANK` coincidentes. Isso prova identidade estrutural de
   banco, não frames, poses, rigging ou timing. A extensão neutra do relatório
   encontrou 19 hashes de layout ELF distintos; todos têm seção `.data` e o
   símbolo fica 28 bytes antes do fim dessa seção. A assinatura `.data`
   normalizada, mascarando somente words de relocation, também produziu 19
   hashes distintos; não há template byte-a-byte comum sob essa regra. É
   regularidade e evidência negativa de bytes, não uma regra de records, frames,
   poses, rigging ou timing.
   A comparação complementar `animation-bank-payload-compare` gerou 171 pares
   entre os 19 `.data` validados, contando somente bytes/words alinhados crus e
   após a mesma máscara de relocations. Não houve payload bruto ou normalizado
   exatamente igual; o maior overlap normalizado teve 5.429 bytes e 129 words
   iguais. O maior trecho contíguo igual foi de 40 bytes ou 10 words, tanto no
   payload cru quanto no normalizado. Isso restringe a triagem de payloads
   repetíveis sem decodificar frames, poses, ossos, pesos, rigging, timing ou
   skinning.
   A auditoria independente `animation-bank-data-relocation-audit` registrou
   somente offsets relativos e gaps adjacentes das relocations de `.data`:
   19/19 ELF válidos, 35.900 relocations, 19/19 bancos com relocations, 19
   perfis de offsets distintos e 15 contagens distintas de gaps. O maior banco
   tem 29.152 offsets distintos e 291 gaps distintos. O TSV preserva o
   histograma de gaps e o hash offset/tipo; nenhum valor foi promovido a frame,
   pose, rig, timing, skinning, material, UV, GPU ou decoder.
- Artefatos OBJ existentes em `exports_fifa_mesh_candidates`:
  - `star0001-head-points.obj` — nuvem de pontos diagnóstica.
  - `star0001-head-strips.obj` — exportação provisória anterior.
  - `star0001-head-strips-layout-checked.obj` — exportação candidata mais
    recente, com gate adicional de coerência de layout.
  - Áudio: parser/decoder para streams Xbox EA-XA v3 `SCHl` (blocos `SCDl`) gera
  WAV PCM16; `GBREAK.ast` foi decodificado (estéreo, 24 kHz, 905.528 quadros,
  566 blocos). `audio-play` usa reprodução síncrona Windows até 32 MiB. O
  catálogo e os hosts já montam/validam os 6 streams encontrados na ISO. Há
  também 17 candidatos PA nomeados (7 com canais válidos), mas isso não prova
  uma tabela original de cues nem uma relação com ABKC. As regras clean-room
  explícitas `GOAL_HOME -> goal`, `GOAL_AWAY -> goal` e `TRICK -> panna`
  preparam rows PA validadas; as demais ações permanecem sem seleção automática.
  O comando `audio-validate` percorreu todos os blocos dos 6 `.ast` sem alocar
  WAV: 6/6 aceitos, 0 rejeitados; `GBREAK.ast` e `GBREAKLC.ast` têm 566 blocos
  e 905.528 frames cada.
  A rota de ações do `GameRuntime` para o mixer está integrada e testada; o
  `CatalogAudioMixer` agora pré-decodifica os streams válidos quando recebe o
  opener read-only e prepara `goal` para `GOAL_HOME`/`GOAL_AWAY` ou `panna`
  para a ação authored `TRICK`. No host Win32 essas rows são enviadas ao
  playback assíncrono; no headless o preparo é contado sem tocar áudio. Nenhuma
  outra ação recebe associação automática; as escolhas são regras novas
  clean-room.
  A telemetria agora distingue rotas não mapeadas, candidato indisponível,
  rejeição de decode, preparo e reprodução. Na execução real padrão foram
  observados 5 eventos, 3 sem rota e 2 candidatos preparados; com AI,
  6 eventos, 4 não mapeados e 2 cues tocadas. Isso documenta a lacuna restante
  sem inventar seleção para passe, drible, chute ou kickoff.
  `run-headless ... 80 --demo` agora executa uma sequência determinística de
  posse, drible, passe completo, recuperação e gol com o mesmo renderer e
  mixer do catálogo real, deixando os eventos observáveis no relatório sem
  inventar IDs de áudio.
  O leitor `AudioAbkReader` também percorre a cadeia observada
  `ABKC → módulos → players → sample tables → entradas`, distingue sons RAM e
  referências externas e valida offsets externos contra `SCHl` no `.ast`
  companheiro. No catálogo real, 16/16 bancos são estruturais e há 39
  referências externas, representadas por 70 ranges (incluindo segmentos de
  loop). O mixer ganhou `TryPrepareBankSound` lazy para decodificar um range
  ABKC→AST sob demanda; o self-test cobre essa rota com fixture sintético. A
  montagem normal não pré-carrega esses ranges e nenhuma associação EVT→som
  foi inventada.
  A mesma montagem valida o `BNK` embutido quando o ABKC o aponta: 13 bancos
  têm BNK e 231 entradas de cabeçalho estão dentro dos limites. Os envelopes
  variáveis `PT`/`GSTR` agora são analisados dentro das janelas limitadas pelos
  offsets seguintes, com tags preservadas como evidência bruta; os descritores
  Xbox IMA `0x14` também são reconhecidos no catálogo; os 231 ranges de blocos
  derivados desses descritores passam pela validação sem rejeições. O comando
  `audio-abk-decode` já permite selecionar explicitamente um som RAM e gerar WAV
  PCM16 em código gerenciado, sem modificar a ISO; `audio-abk-play` reutiliza a
  mesma rota para reprodução WinMM síncrona explícita. Isso não estabelece
  vínculo original EVT→ABKC. As associações runtime adicionadas são as regras
  explícitas `GOAL_HOME -> goal` e `GOAL_AWAY -> goal`, validadas contra a row
  PA nomeada; os demais eventos continuam sem reprodução automática.
  A rota `audio-event-candidate-decode` também decodifica explicitamente uma
  row válida de `pcpadat.big` para WAV PCM16, mantendo `SCCl` estrutural e
  recusando rows sem canais válidos; isso amplia o backend sem inventar o
  vínculo EVT.
  `audio-sccl-inspect ... --rejected` agora amostra diretamente rows com
  `channels=0`: elas também têm cadeia `SCCl -> SCDl`, taxa 22.050 Hz e os tags
  `0x05/0x06/0x80/0x84/0x85/0xA0/0x8C`, mas não `0x82`. Os payloads `SCCl`
  variam (`0x26`, entre outros) e não constituem regra independente de canais;
  esses ranges continuam rejeitados e fora do playback.
  A row 0 real foi verificada novamente: estéreo, 22.050 Hz, 107.574 frames e
  WAV PCM16 de 430.340 bytes; a row 7 foi rejeitada de forma controlada por
  `channels=0`, sem criar arquivo de saída.
  A revalidação do catálogo real registrou `231/231` headers variáveis
  parseados, `231/231` descritores Xbox IMA e `231/231` ranges de blocos válidos.
  `PLAYER.abk`, som explícito 1, foi decodificado novamente para WAV PCM16 de
  26.564 bytes sem sobrescrever o artefato anterior.
  A entrega usa `ActionSequence`, então ações consecutivas com o mesmo nome são
  preservadas e ticks sem ação não duplicam eventos.
- O loop Win32 windowed agora usa a mesma rota `ActionSequence` antes de chamar
  `IAudioMixer.Update`; portanto o encaminhamento gameplay→áudio não fica
  restrito ao headless.
- Foi acrescentado `asset-format-smoke ... .abk --verbose`, que imprime palavras
  brutas dos primeiros 256 bytes para investigação. Em quatro bancos amostrados,
  `0x14` coincide com o tamanho do arquivo e a palavra em `0x1C` é `0x5C`; os
  significados dos demais campos ainda não estão estabelecidos. Não tratar
  esses valores como offsets/tamanhos tipados sem validação adicional.
- Nesta sessão foi acrescentado `audio-structure-smoke`, um leitor limitado que
  validou, em modo somente leitura, 16 `.abk`, 16 `.off` e 2 `.evt`: todos os
  ABKC têm `0x14 == tamanho` e `0x1C == 0x5C`; todas as tabelas `.off` têm 48
  ou 283 linhas ASCII, faixas monotônicas dentro do `.big` de mesmo tronco e
  uma coluna vazia final; os dois EVT compartilham `03 12 3C 07` e têm o word
  em `0x04` dentro do arquivo, mas diferente do tamanho total. Nenhum evento
  foi associado diretamente a uma amostra ABKC.
- O mesmo diagnóstico agora verifica os prefixos dos companions: 48/48 faixas
  de cada `djdat_*.off` e 283/283 de `pcpadat.off` começam com `SCHl`; os
  companions `djhdr`/`pcpahdr` são ranges legíveis de cabeçalhos binários. Os
  `.evt` expõem runs de texto como `play_hitz_eng` e `panna_goal`, mas os nomes
  e IDs ainda não foram ligados a registros ABKC.
- A verificação headless real de 60 frames continua estável após a integração
  Xbox IMA: 6/6 AST válidos, 231/231 ranges BNK válidos, 60 frames renderizados
  e checksum `3D73DE7D7E6B8591`. A ISO manteve SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- A sequência `run-headless ... 80 --demo` também foi verificada na ISO:
  80 frames, 5 eventos encaminhados ao mixer, último evento `GOAL_HOME`, placar
  `HOME 1 - 0 AWAY`, checksum `5405DE72BE1B4FC1` e modo explicitamente
  `deterministic-demo`.
- `run-headless ... 80 --demo --play-audio` agora oferece uma verificação
  explícita do caminho WinMM assíncrono no Windows. O padrão headless continua
  silencioso; a opção só habilita a saída para as regras clean-room já
  demonstradas `GOAL_HOME -> goal` e `GOAL_AWAY -> goal`, sem adicionar
  bindings originais.
- Essa verificação foi executada na ISO: 80 frames, checksum
  `5405DE72BE1B4FC1`, `gameplay_audio_prepared=1`,
  `gameplay_audio_played=1`, `gameplay_audio_unmapped_events=4` e
  `gameplay_audio_decode_rejected=0`. A repetição
  sem `--play-audio` manteve `gameplay_audio_played=0` e o mesmo checksum.
- Uma comparação conservadora dos words little-endian dos `.evt` com os
  offsets das linhas `.off` encontrou nenhum match em `djdat`/`pcpadat`.
  `eventspa.evt` tem apenas matches repetidos em alguns companions de header
  (`1540` vinte vezes, `32768` seis vezes e poucos valores isolados), sem
  reprodução dos payloads; esses valores permanecem ambíguos.
- Uma correlação estrutural mais forte foi adicionada ao leitor: os 283 words
  iniciais distintos dos ranges de `pcpahdr.big` aparecem como words alinhados
  zero-extended em `eventspa.evt`, cobrindo 283/283 registros em 314
  ocorrências. Isso prova uma relação binária com o catálogo de headers PA,
  mas ainda não prova seleção de amostra nem relação direta com ABKC.
- A etapa seguinte foi implementada no leitor sem atribuir semântica: a
  comparação paralela por ordem de linha confirma `pcpahdr.off`/`pcpadat.off`
  com 283/283 ranges e `djhdr_en.off`/`djdat_en.off` com 48/48; todos os
  ranges de dados desses pares começam em `SCHl`, e os ranges de header são
  legíveis. O resultado sustenta uma relação estrutural de companions, mas
  ainda não transforma o índice em ID de amostra.
- A comparação de conteúdo por índice agora lê o header inteiro e no máximo
  64 bytes do stream. Nos pares PA e DJ, a terceira coluna coincide em todos
  os registros, mas nenhum comprimento de header coincide com o do stream,
  nenhum prefixo de até 16 bytes coincide e não há words alinhados não nulos
  compartilhados nessa janela. Isso é uma evidência negativa útil: a ordem
  paralela não é, sozinha, um registro de áudio copiável.
- A inspeção neutra do envelope dos chunks confirma `SCHl`, tamanho declarado
  dentro do range, `PT` e plataforma 7 em 283/283 streams PA e 48/48 streams
  DJ. O parser detalhado agora percorre 283/283 PA e 48/48 DJ depois de
  corrigir o salto após um comprimento estendido já consumido; o self-test
  cobre esse caso com fixture sintético.
- Na comparação de words alinhados dos headers com os valores extraídos dos
  chunks, plataforma 7 e versão 3 aparecem em 48/48 headers de cada idioma DJ
  e em 0/283 headers PA. Houve ainda uma ocorrência de `channels` e de
  `sample_rate` em `djhdr_it`; isso continua sendo correlação binária, não
  semântica. Os offsets dos matches variam entre idiomas e nenhum offset único
  cobre os 48 registros DJ, portanto não há campo estável demonstrado.
- A correlação dos valores de metadados `SCHl` com os `.evt` encontrou somente
  ocorrências repetidas: DJ em `platform`, `version`, `channels` e um
  `header_size`; PA apenas em `channels`. Não houve match de `codec`,
  `sample_rate` ou `declared_samples` que selecionasse uma linha de áudio.
  Esses resultados não autorizam integração de reprodução/mixagem.
- Em `eventspa.evt`, as 314 ocorrências dos 283 words iniciais distintos de
  `pcpahdr` ficam entre `0x74` e `0x1E68`, todas antes do word de limite
  `0x1E70`; os gaps variam de 8 a 84 bytes. Isso delimita um bloco de
  catálogo/registros anterior aos runs de texto, mas não define records ou IDs.
- A janela relativa de words encontrou repetição local: `+4` bytes é zero em
  314/314 ocorrências; em `-12`, o valor mais comum é `1` em 277/314, e em
  `-8`, o valor mais comum é `4` em 244/314. A diversidade restante impede
  tratar a janela como record fixo ou nomear os words como flags/IDs.
- Dez values duplicados somam 41 ocorrências e todos aparecem em múltiplas
  janelas de contexto. Portanto, a igualdade do word não identifica uma cópia
  do mesmo record bruto.
- As 314 janelas completas de contexto são distintas, sem família repetida e
  com frequência máxima 1. A repetição observada está em campos relativos
  isolados, não em records binários idênticos.
- A comparação neutra de espaçamento foi adicionada ao smoke: `eventspa.evt`
  mantém as 314 ocorrências PA entre `0x74` e `0x1E68`, com gaps de 8 a 84
  bytes e gap 24 em 257 casos; `eventsdj.evt` só tem uma ocorrência para cada
  idioma DJ, exceto duas no italiano separadas por 220 bytes. A diferença de
  janelas não é suficiente para nomear records ou selecionar áudio.
- `audio-event-text-smoke` foi adicionado para inspecionar os runs de texto e
  seus bytes vizinhos sem atribuir IDs. Na `eventspa.evt`, os nomes de gameplay
  formam uma string table contígua a partir de `0x2090` (`panna_goal`, `beat`,
  `chance`, `gb_goal`, etc.); os bytes vizinhos imediatos não expõem ponteiros
  claros para `pcpahdr`/ABKC. Isso é evidência negativa adicional, não um
  binding de reprodução.
- `audio-event-index-smoke` agora expõe a região alinhada imediatamente anterior
  à string table como pares de offset relativo + word seguinte. Foram isolados
  17 pares reproduzíveis para os nomes de `eventspa.evt`, todos com a família
  binária observada `word & 0xFFFF == 0x4F6B`, incluindo `3panna_goal` em
  `0x1E70`. A busca exata desses 17 words em todos os `.abk` enumerados não
  encontrou ocorrências; portanto continuam sendo evidência de índice/hash
  local do EVT, não IDs ABKC nem seletores de amostra. O leitor e o self-test
  cobrem offsets zero e não-zero sem atribuir semântica.
- A hipótese de que esses 17 words seriam chaves diretas dos companions PA
  foi testada com busca alinhada little-endian: houve 0 ocorrências em
  `pcpahdr.big` (35.572 bytes) e 0 em `pcpadat.big` (601.688.832 bytes). A
  busca do companion de dados foi streaming, sem materializá-lo e sem alterar
  a ISO; portanto os words continuam sem binding EVT→row.
- A leitura EAGL agora também separa os tokens `nome=valor` dos símbolos de
  estado de cada primitiva em `StateAssignments`, preservando valores como
  `SetTransparencyMethod`, `SetAlphaBlendMode`, `SetAlphaTestEnable`,
  `SetAlphaCompareValue`, `SHAPENAME` e `XBOXEXTOBJ_SetClampU/V`. Na ISO,
  2.389/2.390 primitivas com candidato de posição expõem ao menos uma
  atribuição; isso é evidência textual de estado, não ainda um material PC,
  skinning ou regra final de renderização.
- Foi adicionado `audio-event-candidate-smoke`: ele correlaciona cada word
  alinhado de `eventspa.evt` com o leading word exato das linhas de
  `pcpahdr.off` e expõe a linha paralela de `pcpadat.off` quando a cardinalidade
  coincide. Na ISO, são 314 candidatos, 283 words distintos, 283 linhas distintas
  e 314/314 candidatos com linha paralela; os 283 ranges `pcpadat` começam em
  `SCHl`. Isso ainda é um catálogo estrutural para a maioria dos casos. O novo
  `audio-event-named-candidates` faz a correlação exata dos 17 nomes da string
  table com uma row header/data paralela: na ISO, 17 candidatos passam a
  inspeção de envelope e 7 têm canais válidos. O runtime usa somente o nome
  `goal` para as ações clean-room explícitas `GOAL_HOME` e `GOAL_AWAY`; os
  outros nomes não são promovidos a cues originais nem selecionados
  automaticamente.
- A validação integral desses ranges encontrou uma divisão real: 139/283
  candidatos passam o decoder EA-XA `SCDl`, totalizando 7.633 blocos e
  11.118.229 frames; 144/283 são rejeitados porque o metadata `SCHl` não
  contém o tag `0x82`, fazendo o decoder representar `channels=0` (a
  frequência observada nesses casos é 22050 Hz). A inspeção
  `audio-sccl-inspect` confirmou `SCCl` como primeiro chunk em 283/283 ranges,
  inclusive nos 139 que decodificam; portanto `SCCl` não é, por si só, a causa
  da rejeição.
- `EaSchlAudioDecoder.InspectChunkSequence` agora registra headers e prefixos
  limitados sem decodificar variantes. Na amostra real, o envelope `SCCl` tem
  12 bytes e payloads variáveis (o primeiro rejeitado observado tem `0x26`),
  seguido por `SCDl`; o walker pula `SCCl` explicitamente e valida os `SCDl`
  seguintes quando os metadados de canais são válidos. `InspectHeaderFields`
  também expõe os tags/valores presentes sem nomear campos desconhecidos; rows
  válidas exibem `0x82/1=2`, enquanto as 144 rejeitadas não exibem o tag `0x82`
  e o decoder portanto as representa como `channels=0`. Ainda é necessária
  uma regra independente antes de preencher esse campo.

## Arquivos importantes

- `fifa_pc/Runtime/EaglModelReader.cs` — cadeia model/layer/render descriptor,
  estados de primitiva e leitura dos slots `id=9`.
- `fifa_pc/Runtime/EaglVertexBufferReader.cs` — validação de descritores e
  medição de streams float3, float2 e perfis compactos de floats finitos.
- `fifa_pc/Runtime/EaglTriangleStripExporter.cs` e
  `fifa_pc/Runtime/EaglPositionCloudExporter.cs` — exports OBJ diagnósticos.
- `fifa_pc/Runtime/EaglCandidateMeshBuilder.cs` — mesh gerenciado candidato
  usando bindings estruturais completos de vertex fetch quando disponíveis e
  preservando os perfis genéricos do slot 1, estados nomeados e IDs de
  comandos dos grupos aceitos;
  ainda sem alegar binding completo do draw.
- `fifa_pc/Runtime/EaglCandidateAssetLoader.cs` e
  `fifa_pc/Runtime/EaglCandidateMeshRenderer.cs` — carregamento somente leitura
  e rasterização experimental preenchida/contornada no host windowed.
- `fifa_pc/Runtime/EaglMorphReader.cs` — strings/relocations nomeadas de
  morph e janelas limitadas de bytes ao redor dos campos relocados, sem
  presumir layout de deformação.
 - `fifa_pc/Runtime/EaglMorphSiblingReader.cs` — correlação nominal candidata
   entre morph `.o` e irmãos `.ord/.orl`, com validação ELF/bounds somente.
 - `fifa_pc/Runtime/MorphAmbiguitySummarizer.cs` e o comando
   `morph-ambiguity-summary` — agregação das composições de variação já
   observadas; o artefato é `fifa_pc/morph-sibling-ambiguity-summary.tsv`.
 - `fifa_pc/Runtime/MorphAmbiguityByteSummarizer.cs` e o comando
   `morph-ambiguity-byte-summary` — junção exata das composições com métricas
   byte-level; o artefato é `fifa_pc/morph-sibling-ambiguity-byte-summary.tsv`.
  - `fifa_pc/Runtime/MorphAmbiguityNumericSummarizer.cs` e o comando
    `morph-ambiguity-numeric-summary` — junção dos gaps e limites por composição;
    o artefato é `fifa_pc/morph-sibling-ambiguity-numeric-summary.tsv`.
  - `fifa_pc/Runtime/MorphAmbiguityGroupSummarizer.cs` e o comando
   `morph-ambiguity-group-metadata` — matriz por layout ambíguo dos perfis,
   fronteiras e gaps observados; o artefato é
   `fifa_pc/morph-sibling-ambiguity-group-metadata.tsv`.
  - `fifa_pc/Runtime/MorphAmbiguityIndependentAudit.cs` e o comando
    `morph-ambiguity-independent-audit` — reagrupamento independente dos
   perfis publicados contra o catálogo bruto; os artefatos são
   `fifa_pc/morph-sibling-ambiguity-independent-audit.tsv` e
   `fifa_pc/morph-sibling-ambiguity-independent-audit-summary.tsv`.
 - `fifa_pc/Runtime/EaglMorphNameStateAudit.cs` e o comando
   `eagl-morph-name-state-audit` — comparação lexical de nomes morph e
   `SHAPENAME` preservados; o artefato é
   `fifa_pc/eagl-morph-name-state-audit.tsv`.
 - `fifa_pc/Runtime/EaglMorphAnimationNameNegativeControl.cs` e o comando
   `eagl-morph-animation-name-negative-control` — controle lexical exato entre
   nomes de shape morph e nomes de banco/arquivo de animação; o artefato é
   `fifa_pc/eagl-morph-animation-name-negative-control.tsv`, sem binding.
  - `fifa_pc/Runtime/AudioNamedEventCandidateDecoder.cs` e o comando
    `audio-event-named-decode-smoke` — decodificação controlada dos
    candidatos nomeados do EVT; o artefato é
    `fifa_pc/audio-named-event-decode.tsv`.
  - `fifa_pc/Runtime/AudioBankBatchDecoder.cs` e o comando
    `audio-abk-batch-decode-smoke` — tentativa bounded de payload AST/BNK
    por banco; o artefato é `fifa_pc/audio-abk-batch-decode.tsv`.
  - `fifa_pc/Runtime/EaglCandidateAttributeAudit.cs` e o comando
    `eagl-candidate-attribute-audit` — perfis numéricos slot-1 e populações
    de estado separadas; o artefato é
    `fifa_pc/eagl-candidate-attribute-audit.tsv`.
 - `fifa_pc/Runtime/EaglPrimitiveStateCorrespondence.cs` e o comando
   `eagl-candidate-state-correspondence` — comparação bruta entre primitivas
   de geometria e estado; o artefato é
   `fifa_pc/eagl-candidate-state-correspondence.tsv`.
 - `fifa_pc/Runtime/EaglStateGeometryAdjacencyAudit.cs` e o comando
   `eagl-state-geometry-adjacency-audit` — controle negativo dos índices de
   primitiva e camadas entre estado e geometria candidata; o artefato é
   `fifa_pc/eagl-state-geometry-adjacency.tsv`. A execução observou 0 pares no
   mesmo índice, 48 pares adjacentes e 269 pares na mesma camada, sem promover
   adjacência a binding de estado, mesh, material, UV, skinning, rigging,
   animação ou GPU.
- `fifa_pc/Runtime/EaglStateGeometryOrderAudit.cs` e o comando
  `eagl-state-geometry-order-audit` — controle de intervalos da ordem
  observada entre candidatos geométricos e estados com `SHAPENAME`; o artefato
  é `fifa_pc/eagl-state-geometry-order-audit.tsv`. Ele não cria binding nem
  semântica de material, UV, skinning, rigging, animação ou GPU.
- `fifa_pc/Runtime/MorphCandidateGeometryAudit.cs` e as opções
  `--geometry-report`/`--geometry-summary`/`--geometry-correlation-report`/
  `--geometry-correlation-summary` de
  `eagl-morph-sibling-inventory` — perfil numérico adicional da representação
  mesh candidata por layout morph, sem alterar os rótulos de ambiguidade; os
  artefatos são `fifa_pc/morph-sibling-candidate-geometry-audit.tsv` e
  `fifa_pc/morph-sibling-candidate-geometry-audit-summary.tsv`, além da
  correlação em `fifa_pc/morph-sibling-candidate-geometry-correlation.tsv` e
  `fifa_pc/morph-sibling-candidate-geometry-correlation-summary.tsv`.
- `fifa_pc/Runtime/MorphCandidateGeometryAmbiguityAudit.cs` e o comando
  `morph-candidate-geometry-ambiguity-audit` — controle independente entre a
  assinatura geométrica candidata e a matriz de ambiguidade; os artefatos são
  `fifa_pc/morph-sibling-candidate-geometry-ambiguity-audit.tsv` e
  `fifa_pc/morph-sibling-candidate-geometry-ambiguity-audit-summary.tsv`.
- `fifa_pc/Runtime/ByteSequenceComparer.cs` — comparação alinhada de bytes
  observados, sem interpretação de campos.
- `fifa_pc/Runtime/AudioAssetReader.cs` — validação básica ABKC/SCHl.
- `fifa_pc/Runtime/AudioAbkReader.cs` — cadeia estrutural ABKC e ranges externos
  para AST, tabela BNK embutida, headers variáveis e validação de ranges Xbox
  IMA, sem semântica de eventos.
- `fifa_pc/Runtime/XboxImaAudioDecoder.cs` — validação e decodificação gerenciada
  dos blocos Xbox IMA observados nos BNK, com saída WAV PCM16 explícita.
- `fifa_pc/Runtime/AudioStructureReader.cs` — evidência estrutural limitada de
  ABKC, CSV `.off` + companion `.big` e envelope `.evt`, sem semântica inventada;
  também fornece busca alinhada em streaming para words candidatos.
- `fifa_pc/Runtime/EaSchlAudioDecoder.cs` — decodificação EA-XA.
- `fifa_pc/Runtime/AudioRangeStream.cs` — visão somente leitura limitada a um
  range de companion para validação de streams candidatos.
 - `fifa_pc/Runtime/AptReader.cs` e o comando `apt-structure-inventory` — header
  APT/constant e inventário somente leitura dos companions internos, sem
  atribuir semântica a timing ou pose; a opção `--report` materializa
   `fifa_pc/apt-structure-fe.tsv` e `fifa_pc/apt-structure-ig.tsv` com a
   presença estrutural literal de símbolos morph.
 - `fifa_pc/Runtime/AptStructuralCorrelation.cs` — perfis de header APT/`.const`
   e companions com comparação de words alinhadas, sem semântica de campos;
   os artefatos são `fifa_pc/apt-structure-fe-profiles.tsv` e
   `fifa_pc/apt-structure-ig-profiles.tsv`.
 - `fifa_pc/Runtime/AptAnimationNegativeControl.cs` e o comando
   `apt-animation-negative-control` — controle textual separado entre os
   relatórios APT e o índice de nomes de animação, sem binding semântico; o
   artefato é `fifa_pc/apt-animation-negative-control.tsv`.
 - `fifa_pc/Runtime/AptCrossPackageProfileAudit.cs` e o comando
   `apt-cross-package-profile-audit` — recorrência exata de perfis FE/IG por
   header APT, header `.const` e extensões, sem semântica de timing; os
   artefatos são `fifa_pc/apt-cross-package-profile-audit.tsv` e
   `fifa_pc/apt-cross-package-profile-audit-summary.tsv`.
 - `fifa_pc/Runtime/AnimationBankReader.cs` e o comando
   `animation-bank-inventory` — catálogo `.axt`, pares `.ord/.orl`, símbolo
   `__AnimationBank:::` e hash `.data` normalizado somente por relocations, sem
   decoder de frames, poses, rig ou timing; o artefato é
   `fifa_pc/animation-bank-inventory.tsv`. O comando
   `animation-bank-name-index` acrescenta
   `fifa_pc/animation-bank-name-index.tsv` com uma linha por nome/timestamp
   observado, sem promover metadata a animação.
 - `fifa_pc/Runtime/AnimationBankCatalogComparison.cs` e o comando
   `animation-bank-catalog-compare` — perfis e pares de sobreposição somente
   dos catálogos `.axt`; os artefatos são
   `fifa_pc/animation-bank-catalog-profiles.tsv` e
   `fifa_pc/animation-bank-catalog-pairs.tsv`, sem semântica de frame, pose,
   rig, timing ou skinning.
 - `fifa_pc/Runtime/AnimationBankElfStructureReport.cs` e o comando
   `animation-bank-elf-structure` — headers de seção e layout de relocations;
   os artefatos são `fifa_pc/animation-bank-elf-profiles.tsv` e
   `fifa_pc/animation-bank-elf-sections.tsv`, sem decoder de payload.
 - `fifa_pc/Runtime/AnimationBankDataRelocationAudit.cs` e o comando
   `animation-bank-data-relocation-audit` — offsets relativos, gaps adjacentes,
   histogramas e hash estrutural das relocations de `.data`; o artefato é
   `fifa_pc/animation-bank-data-relocation-audit.tsv`, sem semântica atribuída.
 - `fifa_pc/Runtime/EaglMorphAnimationNameNegativeControl.cs` e o comando
   `eagl-morph-animation-name-negative-control` — controle lexical exato entre
   nomes de shape morph e nomes de banco/arquivo de animação; o artefato é
   `fifa_pc/eagl-morph-animation-name-negative-control.tsv`, sem binding.
 - `fifa_pc/Runtime/AnimationBankTailComparison.cs` e o comando
   `animation-bank-tail-compare` — comparação do trecho ancorado no símbolo,
   com bytes crus, bytes mascarados e relação numérica catalogada; o artefato é
   `fifa_pc/animation-bank-tail-comparison.tsv`, sem semântica atribuída.
 - `fifa_pc/Runtime/AnimationBankSidecarBoundaryAudit.cs` e o comando
   `animation-bank-sidecar-boundary-audit` — ranges ELF na fronteira física
   `.ord/.orl` e contexto limitado; os artefatos são
   `fifa_pc/animation-bank-sidecar-boundary-audit.tsv` e
   `fifa_pc/animation-bank-sidecar-boundary-summary.tsv`, sem decoder.
 - `fifa_pc/Runtime/EaglMorphSidecarBoundaryAudit.cs` e o comando
   `eagl-morph-sidecar-boundary-audit` — fronteira física dos companions dos
   594 objetos `__Morph:::` validados; os artefatos são
   `fifa_pc/eagl-morph-sidecar-boundary-audit.tsv` e
   `fifa_pc/eagl-morph-sidecar-boundary-summary.tsv`, sem semântica atribuída.
 - `fifa_pc/Runtime/MorphShpxStructureAudit.cs` e o comando
   `eagl-morph-shpx-structure-audit` — comparação independente de presença,
   versão, contagem e tabela SHPX same-stem dos rows morph; os artefatos são
  `fifa_pc/morph-sibling-shpx-structure-audit.tsv` e
  `fifa_pc/morph-sibling-shpx-structure-audit-summary.tsv`, sem binding
  atribuído.
 - `fifa_pc/Runtime/MorphShpxGeometryCorrelationAudit.cs` e o comando
   `eagl-morph-shpx-geometry-correlation-audit` — co-variação de SHPX e
   geometria candidata por layout; os artefatos são
   `fifa_pc/morph-sibling-shpx-geometry-correlation.tsv` e
   `fifa_pc/morph-sibling-shpx-geometry-correlation-summary.tsv`, sem binding.
 - `fifa_pc/Runtime/MorphElfHeaderAudit.cs` e o comando
   `eagl-morph-elf-header-audit` — comparação independente de header ELF e
   metadata do símbolo `__Morph:::`; os artefatos são
   `fifa_pc/morph-sibling-elf-header-audit.tsv` e
   `fifa_pc/morph-sibling-elf-header-audit-summary.tsv`, sem decoder atribuído.
 - `fifa_pc/Program.cs` e o comando `animation-bank-payload-compare` —
   comparação pareada de bytes/words alinhados do `.data` bruto e com a máscara
   de relocations, incluindo o maior trecho contíguo repetido (40 bytes/10
   words); o artefato é
   `fifa_pc/animation-bank-payload-comparison.tsv`, sem semântica de campos.
- `fifa_pc/Runtime/GameRuntime.cs`, `Win32GameHost.cs`, `StreetMatch.cs` —
  runtime/headless, janela e estado de partida em desenvolvimento.
- `fifa_pc/Runtime/SelfTestRunner.cs` — testes sintéticos executados por
  `self-test`.
- `fifa_pc/Runtime/ShpxExportCrosscheck.cs` e os relatórios
  `fifa_pc/shpx-export-crosscheck*.tsv` — cross-check pixel-level das TGAs
  persistidas, com proveniência não independente.
- `docs/FIFA_RECOMPILATION_STATUS.md`, `docs/FIFA_ASSET_FORMAT_READERS.md`,
  `docs/FIFA_MEDIA_BACKENDS.md` e `fifa_pc/README.md` — documentação de estado.

O fixture sintético EAGL foi ajustado recentemente: os descritores proprietários
agora ficam nos offsets 800–832, evitando sobreposição com o fluxo de comandos
sintético em 448–528.

## Próxima frente recomendada

1. A inspeção de `.abk`, `.off`, `.evt` e chunks `SCHl` está concluída como
   evidência estrutural. As associações de gameplay clean-room permanecem
   `GOAL_HOME -> goal` e `GOAL_AWAY -> goal`; não adicionar outros cues sem
   uma relação independente.
2. A primeira camada reproduzível de material EAGL foi integrada apenas como
   caminho candidato: `EaglShapeMaterialLoader` e `eagl-material-preview`
   validam o caso `tops0030.big`/`tops0030.ord`/`tops0030.xsh`, decodificam o
   registro interno `tops` e o rasterizam sobre a malha candidata. Ampliar isso
   exige repetir a prova por tronco e separar casos sem `slot 1` completo; não
   promover os valores a UV global nem atribuir semântica aos comandos GPU.
   Essa é uma pista, não uma seleção de textura. A próxima implementação deve
   comparar a família do modelo, o pacote XSH e o registro interno antes de
   ligar qualquer imagem ao renderer.
3. Usar o inventário de bancos `.axt` como vínculo estrutural comprovado:
    os 19 catálogos válidos têm pares `.ord/.orl` válidos e seus símbolos
    `__AnimationBank:::` coincidem com o nome do banco. A próxima implementação
   deve considerar que a normalização conservadora do `.data` já produziu 19
   hashes distintos; continue comparando somente payloads combinados e layouts
   repetíveis, sem nomear words como frames, poses, ossos, pesos ou timing.
   A comparação de cauda agora também cruza o contador declarado no `.axt` com
   as entradas parseadas: 19/19 cabeçalhos conferem e 19/19 igualdades
   independentes reproduzem a word como `2 * declared_file_count`. Isso
   continua sendo controle de header/contagem, não decoder.
 4. Continuar `EaglMorphReader` com os `.o` morph: relacionar somente irmãos
   `.ord/.orl`, tamanhos, strings e relocations que tenham repetição verificável.
   Não converter `BS_*`, words ou janelas em pesos, ossos ou poses sem uma
   regra validada em múltiplos objetos.
   A etapa byte-level agora mede 213/239 igualdades de bytes crus e também por
   partição; a normalização valor/categoria dos registros coincide em 222/239,
   enquanto o registro canônico completo continua em 0/239. A correlação
   adicional dos 17 casos já separa, contra 355 controles, 2/17 limites brutos,
   7/17 fronteiras relativas, 5/17 sequências completas de janela e 0/17
    offsets canônicos. O comando `morph-ambiguity-group-metadata` já separa os
    38 layouts variáveis pelas camadas de limites e metadata, sem combinar essas
    coincidências nem atribuir semântica. O próximo recorte deve usar essa
    matriz somente para selecionar grupos com evidência independente adicional
    de mesh, SHPX, header ou timing. A nova auditoria estado/topologia também
    não encontrou igualdade exata de bytes ou forma de fronteira nos 269 pares;
    portanto essa associação continua sem prova e não foi aplicada ao renderer.
   O recorte SHPX independente já foi executado: 591/594 pacotes same-stem
   válidos, 3 ausências explícitas e 17 layouts com perfis variáveis, todos
   ambíguos. O próximo avanço seguro deve exigir evidência independente de
   timing externo repetível antes de criar qualquer binding; o controle de
   header ELF já encontrou um perfil único em todos os 393 layouts.
 5. APT já tem inventário estrutural completo em `fe.bh`/`ig.bh`, mas os 68/44
  padrões de header e fields fora do asset impedem um decoder de timing. Só
  promover campos após correlação repetível com `.const`, `.o` e `.xsh`. O novo
   relatório por pacote encontrou zero `__Morph:::` e zero stems `*_morph` nos
   279 `.o` válidos; use isso como controle negativo, não como prova de ausência
   de animação. A correlação adicional formou 80 perfis FE e 51 IG; em 568 words
   alinhados comparados, 0 foram iguais. Isso é somente controle estrutural e
   não transforma diferença ou recorrência em timing, pose, frame ou animação.
 6. Preservar e ampliar a execução integrada: gameplay determinístico, render
   candidato headless/Win32 e áudio WinMM já têm smoke real; ainda faltam
   geometria/material finais, deformação/animação e mais bindings de áudio.
 7. Atualizar a documentação e repetir build, self-tests e validações da ISO a
   cada mudança relevante; não iniciar code review antes da integração final.

Critério de conclusão do projeto: um runtime .NET que carrega os assets
necessários e executa uma partida substancialmente fiel, com renderização,
controles, regras e áudio integrados. Não confundir compilar o projeto auxiliar
`FifaStreetPc.dll/.exe` com ter recompilado ou concluído o jogo.

## Comandos de validação

Executar do diretório `D:\projeto zeta`:

```powershell
dotnet build -c Release fifa_pc\FifaStreetPc.csproj
dotnet run -c Release --no-build --project fifa_pc -- self-test
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-inventory.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-payload-compare "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-payload-comparison.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-runtime-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-catalog-compare "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --profile-report "D:\projeto zeta\fifa_pc\animation-bank-catalog-profiles.tsv" --pair-report "D:\projeto zeta\fifa_pc\animation-bank-catalog-pairs.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-elf-structure "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --profile-report "D:\projeto zeta\fifa_pc\animation-bank-elf-profiles.tsv" --section-report "D:\projeto zeta\fifa_pc\animation-bank-elf-sections.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-data-relocation-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-data-relocation-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-tail-compare "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-tail-comparison.tsv"
dotnet run -c Release --no-build --project fifa_pc -- animation-bank-sidecar-boundary-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\animation-bank-sidecar-boundary-audit.tsv" --summary "D:\projeto zeta\fifa_pc\animation-bank-sidecar-boundary-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sidecar-boundary-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --report "D:\projeto zeta\fifa_pc\eagl-morph-sidecar-boundary-audit.tsv" --summary "D:\projeto zeta\fifa_pc\eagl-morph-sidecar-boundary-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-shpx-structure-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-shpx-structure-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-elf-header-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-elf-header-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-elf-header-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --demo --opponent-ai
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 140 --demo --opponent-ai --play-audio
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --play-audio
dotnet run -c Release --no-build --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --candidate-material
dotnet run -c Release --no-build --project fifa_pc -- apt-structure-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh --report "D:\projeto zeta\fifa_pc\apt-structure-fe.tsv" --profile-report "D:\projeto zeta\fifa_pc\apt-structure-fe-profiles.tsv"
dotnet run -c Release --no-build --project fifa_pc -- apt-structure-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/ig.bh --report "D:\projeto zeta\fifa_pc\apt-structure-ig.tsv" --profile-report "D:\projeto zeta\fifa_pc\apt-structure-ig-profiles.tsv"
dotnet run -c Release --no-build --project fifa_pc -- big-model-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big
dotnet run -c Release --no-build --project fifa_pc -- eagl-shapename-index "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-object-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops0030.ord --commands
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-state-correspondence "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-candidate-state-correspondence.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-material-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops
dotnet run -c Release --no-build --project fifa_pc -- eagl-material-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops "D:\projeto zeta\fifa_pc\tops0030-material-preview.ppm"
dotnet run -c Release --no-build --project fifa_pc -- eagl-material-batch-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-geometry-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-candidate-geometry-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-attribute-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\eagl-candidate-attribute-audit.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-projection-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" tops0030.big tops "D:\projeto zeta\fifa_pc\candidate-projection-previews-stage-1" tops0030.ord
dotnet run -c Release --no-build --project fifa_pc -- asset-format-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/audio/AMBSTRM.abk --verbose
dotnet run -c Release --no-build --project fifa_pc -- audio-structure-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- audio-abk-inspect "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --validate-streams
dotnet run -c Release --no-build --project fifa_pc -- audio-abk-decode "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/audio/PLAYER.abk 1 "D:\projeto zeta\fifa_pc\player-sound-1-verified.wav"
dotnet run -c Release --no-build --project fifa_pc -- audio-validate "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- audio-event-candidate-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- audio-event-text-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 32
dotnet run -c Release --no-build --project fifa_pc -- audio-event-index-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- audio-event-index-search "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --data
dotnet run -c Release --no-build --project fifa_pc -- audio-event-candidate-validate "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- audio-event-named-candidates "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- audio-event-candidate-decode "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 0 "D:\projeto zeta\fifa_pc\event-row-0-verified.wav"
dotnet run -c Release --no-build --project fifa_pc -- audio-sccl-inspect "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 12
dotnet run -c Release --no-build --project fifa_pc -- audio-sccl-inspect "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 4 --rejected
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --commands
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\candidate-integrated-preview.ppm"
dotnet run -c Release --no-build --project fifa_pc -- eagl-candidate-preview "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" "D:\projeto zeta\fifa_pc\candidate-slot1-profile-preview.ppm" --slot1-profile
dotnet run -c Release --no-build --project fifa_pc -- eagl-object-symbol-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head_morph.o
dotnet run -c Release --no-build --project fifa_pc -- eagl-object-symbol-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head_morph.o --payload
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sibling-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" star0001.big
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sibling-runtime-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-pose-bank-index "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\morph-pose-bank-index.tsv"
dotnet run -c Release --no-build --project fifa_pc -- eagl-morph-sibling-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big --geometry-report "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit.tsv" --geometry-summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-audit-summary.tsv" --geometry-correlation-report "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-correlation.tsv" --geometry-correlation-summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-correlation-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-byte-summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-byte-comparison-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-byte-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-numeric-summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-numeric-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-group-metadata "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-byte-comparison-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-numeric-gap-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-group-metadata.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-ambiguity-independent-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-independent-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-independent-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- morph-candidate-geometry-ambiguity-audit "D:\projeto zeta\fifa_pc\morph-sibling-structural-catalog.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-ambiguity-report.tsv" "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-ambiguity-audit.tsv" --summary "D:\projeto zeta\fifa_pc\morph-sibling-candidate-geometry-ambiguity-audit-summary.tsv"
dotnet run -c Release --no-build --project fifa_pc -- audio-event-named-decode-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\audio-named-event-decode.tsv"
dotnet run -c Release --no-build --project fifa_pc -- audio-abk-batch-decode-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\audio-abk-batch-decode.tsv" --max-attempts 8
dotnet run -c Release --no-build --project fifa_pc -- eagl-object-symbol-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head.orl --raw
```

Para exportar outra amostra OBJ, forneça um caminho novo: o exportador usa
`CreateNew` e não substitui arquivos existentes.

## Prompt para continuar em outro chat

Copie e cole o texto abaixo no novo chat:

```text
Continue o projeto FIFA Street 2 para PC neste workspace, a partir do estado
atual dos arquivos. Primeiro leia por completo
`D:\projeto zeta\CONTINUAR_FIFA_STREET_2.md` e confira os arquivos relevantes;
não suponha que o resumo substitui o estado real do código.

O objetivo é uma reimplementação clean-room em .NET para PC, sem emulação.
Não há fonte/build originais conhecidos, então não diga que estamos
recompilando literalmente o XBE. Preserve a ISO
`D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso` como somente leitura e
preserve alterações já presentes no workspace.

Prossiga pela próxima etapa recomendada no MD: ABKC, `.off`, `.evt` e os
chunks `SCHl` já têm leitores e diagnósticos estruturais; não repita essas
frentes sem uma hipótese nova verificável. O leitor EAGL preserva comandos,
estados e bindings de vertex fetch, o inventário real confirma 2.390/2.390
fluxos completos e `eagl-shapename-index` mostrou que `head,1`/`tops,1` não
selecionam diretamente basenames `.xsh` (`tops` ainda é prefixo ambíguo de 152
nomes). Use essa evidência para investigar binding/atributos efetivos, morphs
`.o`, rig/animação e a relação APT/companions. O inventário
`animation-bank-inventory` já provou somente a identidade entre 19 catálogos
`.axt` e seus símbolos `__AnimationBank:::`; a normalização do `.data` mascarando
somente words de relocation produziu 19 hashes distintos, sem template
byte-a-byte comum. Compare os payloads desses bancos por layouts repetíveis, sem
atribuir semântica especulativa a words, relocations ou IDs. Implemente apenas
parsers/testes justificados pelos dados e depois rode build, self-tests e
validações da ISO.
Se essa etapa revelar um bloqueio técnico real, avance para uma investigação
segura relacionada em vez de marcar o projeto como concluído.

Mantenha claro que os OBJ atuais são candidatos, o binding EAGL ainda não foi
provado e o runtime ainda não é o jogo completo. Continue trabalhando no
projeto; não inicie code review até os requisitos de execução integrada
(renderização, gameplay, assets e áudio) estarem implementados e verificados.
```

## Avanço desta retomada — referências estáticas de strings no XBE

Foi executada a auditoria somente leitura
`tools/analysis/xbe_string_xref_audit.py` sobre `default.xbe`, com Capstone
5.0.9. Ela encontrou 1.929 strings literais de interesse e 944 referências
x86 candidatas em 944 instruções de origem (939 imediatas e 5 de memória
absoluta), incluindo literais observados de áudio, `.xsh`, `.big`,
`D3DTexture` e `EAGLAnimationBuffer`.

Os artefatos são `fifa_pc/xbe-string-xref-audit.tsv` e
`fifa_pc/xbe-string-xref-summary.tsv`; o XBE lido tem SHA-256
`5fabb5b763be0cc8823e814ee343ac5be6950305330f46c5170e08eaadea910b`.
O resultado é apenas `candidate-x86-reference-only`, com
`original_semantics=not-claimed`; não cria binding de material, UV, skinning,
rigging, animação, morph ou GPU. O readiness permanece `NOT_READY`.

O refinamento `--call-clusters` gerou
`fifa_pc/xbe-string-xref-call-clusters.tsv` com 1.141 agrupamentos por destino
executável observado, 306 com pelo menos duas strings em coocorrência local.
O relatório tem SHA-256
`9A5F416ED66485B2D4E0D2087E3E400711627AA0352CCAB9E6194EC818AF4CF3`; o
resumo atualizado tem SHA-256
`19A633D47CC2FA5CE30D800AE6C52ABD63CDF785546F31DC190A892E72A862BD`.
O teste Python passou `6/6`. Isso continua sendo coocorrência candidate-only,
sem recuperação de função, subsistema ou binding.

A bateria final após a auditoria passou em Release `0/0`, self-test `70/70`,
smokes relevantes, runtime headless e replays determinísticos (`matched=True`).
Os relatórios full-stack gerado/replay são iguais; a ISO continua com SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
