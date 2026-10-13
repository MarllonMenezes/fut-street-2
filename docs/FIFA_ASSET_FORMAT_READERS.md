# Leitores estruturais de assets para PC

## Cross-check textual XBE→XDVDFS — 2026-10-06

`xbe_asset_literal_audit.py` reabre o `default.xbe` e a árvore XDVDFS da ISO
sem escrever nenhum dos dois. A execução encontrou `108` literais de assets
únicos (`702` ocorrências) e `84` correspondências textuais (`81` com
prefixo `data/`, `3` por basename único). Essa é uma relação textual
XBE↔caminho, não uma prova de carregamento nem um decoder: não atribui
material, UV, mesh, áudio, evento, animação, rigging, skinning, morph ou GPU.
O status é `verified-textual-xbe-path-crosscheck` e
`original_semantics=not-claimed`.

Relatório `fifa_pc/xbe-asset-literal-audit.tsv`, SHA-256
`95C13FC106CA4F24586AD972DA8A5189B05DE80F9FE5C45612FCD4E2334550AA`; resumo
`fifa_pc/xbe-asset-literal-summary.tsv`, SHA-256
`5F9731B918075E217BF63D66761228D2B43F10F4712482FB0BEB4D7B37739580`.

## Cross-check textual XBE→entradas BIG aninhadas — 2026-10-06

`xbe_asset_nested_literal_audit.py` percorre os índices BIG de topo e as
entradas `.big` aninhadas em modo somente leitura, com RefPack limitado apenas
para alcançar o próximo índice. O relatório indexou `4.796` caminhos internos
únicos e encontrou `13/108` correspondências de literais do XBE (`10` únicas e
`3` ambíguas), como `models/skel0000.ord` e `models/shdw0001.ord`. É somente
evidência textual de nome/índice; não atribui material, UV, mesh, áudio,
evento, animação, rigging, skinning, morph ou GPU. Status:
`verified-textual-xbe-nested-big-entry-crosscheck`;
`original_semantics=not-claimed`.

Relatório `fifa_pc/xbe-asset-nested-literal-audit.tsv`, SHA-256
`8570C2962A3BC5C7820111A8B56324D8E3DBD3784B4C81A6013EACD053A364CA`; resumo
`fifa_pc/xbe-asset-nested-literal-summary.tsv`, SHA-256
`9B0E93677A6D0CB2B30DB57D2907ECFF5274B26F18B7DF7BA2F8769C9348551C`.

## Join estático XBE→BIG→xrefs — 2026-10-06

`xbe_nested_literal_xref_audit.py` juntou as `13` correspondências de entradas
BIG com xrefs estáticos e contexto de chamadas locais: `8` literais com xrefs,
`10` xrefs, `70` linhas de contexto e `24` destinos executáveis distintos no
conjunto ambíguo `stadium.big`. É evidência de coocorrência textual/endereço
somente; não atribui loader, material, UV, mesh, áudio, evento, animação,
rigging, skinning, morph ou GPU. Status
`verified-static-nested-entry-xref-join`; `original_semantics=not-claimed`.

Relatório `fifa_pc/xbe-nested-literal-xref-audit.tsv`, SHA-256
`63418DEF893A32A5401D2993481CE11CB2A17215A7E19A860413181D11C4CEA2`; resumo
`fifa_pc/xbe-nested-literal-xref-summary.tsv`, SHA-256
`6B84A9CBA014F2148D3E290443C15A3DB2384BA09DDA9D1DB28F057E0279A409`.

## Clusters estáticos de áudio no XBE — 2026-10-06

O auditor `xbe_audio_static_cluster_audit.py` separa literais textuais de
áudio dos xrefs do XBE e agrega apenas contexto local de chamadas diretas.
Foram medidos `58` xrefs, `23` literais, `147` contextos, `108` contextos com
destino executável, `56` destinos e `14` destinos com múltiplos literais.
Somente `audio/tunavars.txt` alcançou uma entrada BIG aninhada; `10` literais
alcançaram caminhos XDVDFS. Não houve literal `.evt` nesse conjunto textual.
O resultado é `verified-static-audio-literal-context`, não recuperação de
função nem seleção original de evento, amostra ou ABKC.

Relatórios: `fifa_pc/xbe-audio-static-literal-audit.tsv`,
`fifa_pc/xbe-audio-static-call-clusters.tsv` e
`fifa_pc/xbe-audio-static-summary.tsv`. `original_semantics=not-claimed`.

## Inventário dos payloads BIG nomeados pelo XBE — 2026-10-06

O inventário somente-leitura `xbe_nested_payload_inventory.py` seguiu as
correspondências do índice BIG e registrou `41` caminhos concretos, `32`
hashes distintos e `15` wrappers RefPack. A camada de bytes observou `2` ELF,
`2` sidecars `.orl`, `1` BILG, `20` SHPX cru, `2` SHPX RefPack e `12` índices
BIG RefPack. Isso comprova somente cabeçalho, caminho, tamanho e hash; não
atribui semântica de mesh, material, UV, rigging, skinning, animação ou GPU.

Relatório `fifa_pc/xbe-nested-payload-inventory.tsv`, SHA-256
`232235B37AACC2A943E1EFFFCBE853F99E3A3F281C5E16A705E0241665709BC9`; resumo
`fifa_pc/xbe-nested-payload-summary.tsv`, SHA-256
`668F8FCEEBDFA76CC2770B663A78DB7C9D413D3FAEE8570E57E28562401004C8`.
`original_semantics=not-claimed`.

## Readiness consumindo o subgate de payloads EVT — 2026-10-06

O `project-readiness-audit` agora lê
`audio-named-event-runtime-summary.tsv` e registra
`audio-named-event-runtime=verified-candidate`: `17/17` candidatos EVT com
metadados validados, payloads preparados e cabeçalhos WAVE válidos. Esse
subgate é candidate-only e não fecha o gate `audio`, que continua
`partially-integrated`; seleção gameplay→evento→amostra/ABKC e semântica
original permanecem `not-proven`/`not-claimed`.

Após a integração, Release `0/0`, self-test `72/72`, matriz authored/candidate,
smokes, runtime headless e replays determinísticos passaram. Readiness TSV:
`fifa_pc/project-readiness.tsv`, SHA-256
`B26A76789252194395CF68C23166215C9DDAE51CCB2D57E3D00D505C9A3B3479`.
O relatório full-stack registra `opponent_ai_enabled=True`; esse campo é uma
prova do runtime authored e não um binding de IA original.

## Payloads EVT nomeados preparados no runtime managed — 2026-10-06

O audit `audio-named-event-runtime-audit` agora faz parte da matriz e relê os
17 candidatos EVT nomeados por nome exato e faixa de dados paralela. A execução
real validou `17/17` metadados, preparou `17/17` payloads no runtime managed,
confirmou `17/17` cabeçalhos RIFF/WAVE e totalizou `2.914.952` bytes PCM.
Relatório `fifa_pc/audio-named-event-runtime.tsv`, SHA-256
`1F4D60065756162D29E665DCB794D9D31F34FACC88DBD5FC38E16D70E8503CDF`; resumo
`fifa_pc/audio-named-event-runtime-summary.tsv`, SHA-256
`DFA25F7E59B247C836879445140D152DA65E7613534013CC52E3B7B7057211D5`.

O resultado é `verified-runtime-named-payload`, com proveniência
`exact-EVT-name+parallel-data-range; managed-runtime-named-payload-only` e
`original_semantics=not-claimed`. É evidência de preparação dos payloads
nomeados, não de seleção gameplay→amostra/ABKC nem de semântica original; o
gate de áudio permanece `partially-integrated`.

## Completude dos streams candidate — 2026-10-06

O audit de proveniência candidate passou a registrar a completude dos streams
de comandos de geometria e dos streams de estado `SHAPENAME`. A execução real
confirmou `24/24` para ambos, e o `candidate-integrated-runtime-audit` exige
esses marcadores antes de validar o cenário candidate. Isso continua sendo
evidência estrutural candidate-only; não é binding original de estado,
geometria, material, UV ou GPU.

Relatório candidate:
`fifa_pc/eagl-candidate-asset-provenance-audit.tsv`, SHA-256
`CB3364DC07F8FB90817BBB473F39D637256256AC210B6DDA747BC412B8B66863`; resumo:
`00F678130853D38B3F2AB190B96F77B1CA850C81F13788ECF230B4BDC90A207D`.
Audit integrado:
`fifa_pc/candidate-integrated-runtime-audit.tsv`, SHA-256
`22A7DC9360E35839C8581ECF1553A9418CFED637B0AE6DF38FC1426E00F48E3B`.

## Integração candidate com proveniência estrutural — 2026-10-06

O `candidate-integrated-runtime-audit` agora lê o resumo
`eagl-candidate-asset-provenance-audit-summary.tsv` antes de aceitar o runtime
candidate. A matriz confirmou `24/24` cadeias, objetos `.ord`, companions
`.orl`, bounds e geometria finita/indexada; o audit registrou
`candidate_asset_provenance_valid=True` e
`candidate_mesh_state_crosscheck_valid=True`.

Isso continua sendo evidência candidate-only. Não atribui binding original de
estado, material, UV, skinning, rigging, animação, morph ou GPU. Relatório do
audit: `fifa_pc/candidate-integrated-runtime-audit.tsv`, SHA-256
`42A23A7F851D59A00FACE9E72C0AF28A65DA4F5FDCC5278FD283EABE228BC97E`.

## Cross-check persistido de exportações SHPX — 2026-10-06

Foi adicionado `shpx-export-crosscheck`. A execução releu
`data/aptanims/player1.xsh` da ISO em modo somente leitura e comparou as 46
imagens decodificadas pelo leitor managed com as 46 TGAs já persistidas em
`exports_fifa_layer3/player1`: `46/46` arquivos presentes, `46/46` cabeçalhos
TGA válidos e `46/46` buffers RGBA idênticos. O relatório é
`fifa_pc/shpx-export-crosscheck.tsv`, SHA-256
`8EB31145E1A05CDED83C3DEA703BD1A9B24755260A650F672FC34545118AC68E`; o
resumo é `fifa_pc/shpx-export-crosscheck-summary.tsv`, SHA-256
`B126B7D0AEA480A299F207BC6F688D82B1A95D769EEFFB58F19F063DDC3FDD4F`.

Esse cross-check comprova apenas a preservação dos pixels no formato TGA
gerado pelo workspace. A proveniência é registrada como
`workspace-generated-TGA; provenance-not-independent`; portanto o resultado
não é uma captura independente e não promove timing, pose, rigging, skinning,
animação, morph, material, UV, GPU ou semântica original.

## Proveniência da cadeia candidate EAGL/SHPX — 2026-10-06

`eagl-candidate-asset-provenance-audit` releu os 24 vínculos same-trunk a
partir de `data/models.big`: `24/24` entradas `.big` lidas, `24/24` entradas
`.xsh` lidas, `24/24` payloads SHPX válidos, `24/24` entradas internas exatas
e `24/24` buffers RGBA iguais a uma segunda decodificação do payload. Relatório:
`fifa_pc/eagl-candidate-asset-provenance-audit.tsv`, SHA-256
`450FA2D4B1731DCEEA91B72EB614182960B0B8C3E0D7A6694C3D2FBC26B3102C`; resumo:
`fifa_pc/eagl-candidate-asset-provenance-audit-summary.tsv`, SHA-256
`22D60528E4E445A2A93B13192643561BE1A20DDD3FDF594E61B8C3DC7807E5BA`.

Além da cadeia de origem, a mesma execução releu `24/24` objetos `.ord`,
`24/24` companions `.orl`, confirmou bounds em `24/24` casos, geometria
finita/indexada em `24/24` e `24/24` linhas primitivas com `SHAPENAME`.
Esse cross-check é estrutural da representação candidata; não afirma que o
estado de renderização está associado à geometria nem atribui semântica a
material, UV ou GPU.

O status é `verified-candidate-source-chain`, com
`candidate_provenance=same-trunk-source-chain; candidate-only` e
`original_semantics=not-claimed`. A auditoria confirma origem, limites e
reprodutibilidade dos dados candidate; não prova material GPU, UV, skinning,
rigging, animação ou binding original.

O runtime clean-room agora possui leitores seguros para os formatos observados
na ISO:

| Formato | Validação atual |
|---|---|
| `SHPX` / `.xsh` | cabeçalho/tabela; exportação TGA para DXT1/DXT3 e PAL8 com paleta anexada/swizzle |
| `Apt Data:` / `.apt` | cabeçalho, campos de seção e limites do asset; inventário estrutural dos companions `.const`/`.o` EAGL/`.xsh` |
| `Apt constant file` / `.const` | cabeçalho e campos de tamanho |
| ELF32 little-endian / `.o` | classe, endianess, tipo, máquina, seções, símbolos e relocações |
| ELF32 MIPS / `.ord` + `.orl` | reconstrução do ELF dividido; símbolos de geometria, bounds `__BBOX` e descritores `__VB` validados |
| ELF32 MIPS / morph `.o` | símbolo `__Morph:::`, strings `BS_*`/`Coordinates`/`Normals` e relocations internas; payload de deformação ainda não decodificado |
| `XPR0` / `.xbx` | cabeçalho, `header_size`, registrador de formato, dimensões e limites; decoder DXT1 para RGBA |
| `SCHl` / `.ast` | metadados Xbox EA-XA v3, evidência de campos/chunks (incluindo SCCl), blocos SCDl, frames ADPCM/PCM, WAV PCM16, reprodução WinMM síncrona limitada e validação no catálogo runtime |
| `ABKC` / `.abk` | assinatura, words neutros de evidência em `0x04`, `0x08`, `0x14` e `0x1C`, limites e contagem de assinaturas embutidas |
| `.off` + `.big` | CSV estrutural de três colunas numéricas e faixa limitada ao `.big` de mesmo tronco |
| `.evt` | prefixo observado, words iniciais e limites; sem interpretação de eventos |

Exemplos validados:

```text
player1.xsh: versão G359, 46 entradas até o offset 384
player1.xsh: primeira entrada `02`, payload em 496 com 17664 bytes
saveimage.xbx: 4096 bytes, 64x64 DXT1, payload de 2048 bytes
titleimage.xbx: 10240 bytes, 128x128 DXT1, payload de 8192 bytes
ArrBLeft.apt: header_size=28
ArrBLeft.const: header_size=28
ArrBLeft.o: ELF32, type=1, machine=8, 6 seções
ArrBLeft.o: seções .data, .rel.data, .shstrtab, .symtab e .strtab
ArrBLeft.xsh: registro DXT3 de 64x64, exportado como TGA com canal alfa
GBREAK.ast: Xbox EA-XA v3, 2 canais, 24 kHz, 905528 quadros PCM em 566 blocos
```

SHPX é um pacote de registros de imagem, não um formato de malha. A conversão
cobre DXT1, DXT3 e o caso PAL8 observado em `player1.xsh`: imagem `0x7B`,
paleta BGRA de 256 cores no attachment `0x2A` e flag de swizzle `0x2000`.
Os 46 quadros 128x128 foram exportados. DXT5 e outros formatos não observados
continuam sem conversão. A geometria e o tempo/sequenciamento da animação ainda
precisam ser reconstruídos a partir de APT e dos objetos ELF/MIPS.

A auditoria `shpx-image-entry-audit` releu `data/aptanims/player1.xsh` diretamente
da ISO em modo somente leitura: as 46 entradas foram decodificadas, todas como
PAL8 128x128, com tabela monotônica e tamanho declarado consistente. O relatório
`fifa_pc/shpx-image-entry-audit.tsv` preserva o SHA-256 da origem, o hash da
tabela ordenada e um hash de pixels RGBA para cada entrada. Isso é evidência de
entradas de imagem decodificáveis e ordem estrutural da tabela; não é evidência
de temporização, pose, rigging, skinning, sequência de animação ou binding de
material, UV ou GPU.

O comando `apt-structure-inventory` percorre todos os BIGs internos de um
pacote `VivF` sem gravar os blocos. Na ISO de trabalho, `fe.bh` possui 163
entradas `.apt` e `.const` legíveis, 161/161 `.o` EAGL estruturalmente legíveis,
145 `.xsh` e 145 conjuntos com os quatro companions; `ig.bh` possui 121, 121,
118/118, 114 e 114 respectivamente.
Os headers `.apt` não formam uma única família: há 68 padrões de campos em
`fe.bh` e 44 em `ig.bh`, e algumas ocorrências têm campos fora do tamanho do
asset. O leitor reporta essa condição explicitamente e não interpreta os
words como timing, pose, morph ou material. A correlação por tronco também é
parcial: em `fe.bh`, os 163 `.apt` têm `.const`, 161 têm `.o` e 145 têm `.xsh`,
com 145 conjuntos completos; os padrões repetidos aparecem tanto com quanto
sem `.xsh`, portanto essa presença não é usada como regra de animação.

Uma assinatura estrutural adicional combina o header APT validado, o header
`.const` validado quando presente, a assinatura estrutural do `.o` EAGL, a
tabela SHPX validada do `.xsh`, as extensões same-stem e os tamanhos dos
companions. O inventário produziu 133 perfis em `fe.bh` e 97 em `ig.bh`; 161/161
e 118/118 objetos `.o` e 145/145 e 114/114 tabelas SHPX dos conjuntos
correspondentes foram lidos, sem rejeições no inventário. Isso mede organização
de pacote, não timing,
poses, morphs, materiais ou frames.
O comando `apt-animation-negative-control` cruza os dois relatórios TSV com
`animation-bank-name-index.tsv` somente por texto e presença estrutural. A
execução atual cobriu 284 linhas APT e 1.584 linhas de catálogo, com 279
objetos válidos, 259 conjuntos completos, 0 `__Morph:::`, 0 candidatos
`*_morph` e 0 coincidências exatas de stems com nomes de banco ou arquivos de
animação. O relatório é controle negativo; não interpreta ausência como prova
de que APT não contenha timing, pose ou frame.
O comando `apt-cross-package-profile-audit` compara a chave exata de header APT,
header `.const` e extensões entre os perfis FE/IG. Foram observadas 80 chaves
FE, 51 IG, 27 compartilhadas, 53 exclusivas FE e 24 exclusivas IG; nenhuma
compartilhada teve word alinhada igual ou símbolo `__Morph:::`. É recorrência
de perfil de container/tabela, não decoder de timing, pose, frame ou animação.

O relatório `animation-bank-payload-comparison.tsv` compara os 19 payloads
`.data` validados em 171 pares, primeiro com bytes/words alinhados crus e
depois mascarando somente as words cobertas por relocations. Não há par
exatamente igual; o maior overlap normalizado tem 5.429 bytes e 129 words,
enquanto o maior trecho contíguo igual mede 40 bytes ou 10 words, tanto cru
quanto normalizado. Essas medidas registram recorrência de payload e limites
potenciais; não decodificam records, frames, poses, ossos, pesos, rigging,
timing, skinning ou comandos GPU.

O comando `animation-bank-catalog-compare` acrescenta uma comparação
independente somente dos `.axt`: 19 perfis e 171 pares, com 1.584 entradas e
1.578 nomes distintos. Os 6 nomes compartilhados entre bancos também têm o
mesmo timestamp observado, mas não houve conjunto de nomes, posição ou prefixo
ordenado comum entre pares. Esses resultados são sobreposição catalog-only e não
interpretam nomes/timestamps como frames, poses, rigging, timing ou skinning.

O comando `animation-bank-elf-structure` expõe uma camada independente de
headers ELF: os 19 pares `.ord/.orl` foram validados, gerando 114 linhas de
seção (6 por banco), 19 hashes de perfil de seção e 19 hashes de perfil de
relocation distintos. Os 19 símbolos de banco e a distância observada de 28
bytes ao fim de `.data` foram preservados. Isso é layout de header/relocation,
não interpretação do payload como records, frames, poses, rigging ou timing.

O comando `animation-bank-tail-compare` acrescenta a comparação dos 32 bytes
iniciados no símbolo `__AnimationBank:::`. Em 19/19 bancos, a word little-endian
nos offsets 4–5 é igual a `2 * catalog_file_count` e
`2 * declared_file_count`. O contador declarado do `.axt` coincide com as
entradas parseadas em 19/19; após mascarar relocations, 30/32 offsets são
comuns em todos e 12 pares têm cauda normalizada idêntica. Essa é uma
correspondência numérica/byte-level reproduzível, não a atribuição de semântica
de header, record, frame, pose ou timing.

O comando `animation-bank-data-relocation-audit` separa uma medição de layout
dos offsets relativos das relocations que apontam para `.data`. Na ISO foram
validados 19/19 ELF e 35.900 relocations; os 19 bancos têm pelo menos uma
relocation, os 19 perfis de offsets são distintos e há 15 contagens distintas
de gaps adjacentes. O maior banco tem 29.152 offsets distintos e 291 gaps
distintos. O TSV preserva também o histograma de gaps e o hash da sequência
offset/tipo. Nenhum offset, gap ou hash recebe semântica de frame, pose, rig,
timing, skinning, material, UV, GPU ou comando.

`animation-bank-sidecar-boundary-audit` acrescenta uma auditoria independente
da composição física dos sidecars. Os 19 pares `.ord/.orl` não têm seção ELF
cruzando a fronteira: somente `.data` file-backed não vazia fica em `.ord`, as
outras quatro seções, a seção de relocation e a tabela de seções ficam em
`.orl`. O TSV também guarda até 16 bytes de contexto de cada lado. Esses são
ranges e bytes observados; não constituem decoder de payload, frames, poses,
rig, timing ou comandos.

`eagl-morph-sidecar-boundary-audit` aplica a mesma medição aos 594 objetos que
expõem o símbolo `__Morph:::` validado nos BIGs aninhados: todos têm 5 seções
file-backed não vazias, com 1 em `.ord` e 4 em `.orl`; a relocation e a tabela
de seções ficam em `.orl` em 594/594, sem seção cruzando a fronteira. O filtro
por símbolo é intencional: os 886 pares obtidos apenas do sufixo `_morph.o` não
são tratados como morph comprovado. A saída continua sendo evidência de
containers/ranges, não vínculo de morph, mesh, pose, rig, peso ou animação.

`eagl-morph-shpx-structure-audit` cruza esses 594 rows, sem usar o payload como
semântica, com o `.xsh` same-stem de `data/models.big`. Foram encontrados e
validados 591/594 pacotes, 3 nomes não têm `.xsh` correspondente
(`tops0001`, `tops0020`, `tops0210`) e 0 pacotes foram rejeitados. Dezessete
dos 393 layouts têm mais de um perfil de header/tabela SHPX, todos dentro dos
38 layouts ambíguos. A recorrência é uma comparação independente de presença,
versão, contagem e tabela, não vínculo de morph, textura, material, UV, mesh,
pose, rig, peso, frame, timing ou GPU.

`eagl-morph-shpx-geometry-correlation-audit` cruza o relatório SHPX com a
assinatura de geometria candidata do catálogo somente por
`morph_structural_layout`. A execução real uniu 393/393 layouts: 17 têm ambas
as camadas variáveis, 21 somente a geometria e 355 permanecem estáveis; 17 dos
38 layouts ambíguos têm co-variação. Como os relatórios não fornecem um
pareamento de entrada, essa saída continua sendo triagem estrutural e não
autoriza binding de SHPX, mesh, morph, UV, material, pose ou GPU.

`eagl-morph-elf-header-audit` faz um controle separado no `.o` morph: os
594/594 objetos são ELF válidos e todos os 393 layouts, inclusive os 38
ambíguos, têm um único perfil de header core, range da tabela e metadata do
símbolo `__Morph:::`. A uniformidade é evidência negativa de header; não
decodifica o payload nem atribui semântica de morph, pose, rig, peso, frame,
timing ou comando GPU.

`data/models.big` contém 391 objetos internos RefPack; eles expandem para
2.658 entradas `.ord`, `.orl` e `.o`. O leitor combina os pares `.ord/.orl`
para recuperar a tabela ELF MIPS, lê bounds dos símbolos `__BBOX` e reconhece
os símbolos/camadas `__Model`, `__VB` e `__RM`. No inventário atual, 1.772
ELFs MIPS (machine `0x0008`) foram analisados, 885 bounds foram decodificados
e 11.950 descritores de vertex buffer passaram pelas checagens de limites e
consistência do cabeçalho apontado. Desses, 4.780 são streams finitos de
float3; 2.389 cabem nas bounds do objeto (2.035 do grupo `__GPvb_10` e 354 de
`__GPvb_11`). Isso identifica candidatos a posições, não confirma semântica:
índices, associação ao draw call, rigging e animação ainda não foram
reconstruídos em assets finais, e nenhum OBJ validado/GLTF é produzido.
O leitor também mede streams de dois `float` consecutivos como perfil numérico:
2.390/2.390 são finitos e ficam na faixa próxima de `[0,1]`. No binding
estrutural, 2.387/2.387 primitivas têm esse perfil no slot 1, e 772 dessas
também expõem `SHAPENAME` e os estados de clamp `XBOXEXTOBJ_SetClampU/V`.
A correlação é útil para a investigação de atributos, mas não autoriza nomear
o stream como UV nem escolher uma textura para `SHAPENAME`.
O leitor também tenta medir cada binding cujo stride inteiro é composto por
floats finitos, preservando mínimo/máximo por componente sem atribuir semântica.
No mesh candidato real isso produz 9 perfis nos slots `0=3`, `1=3` e `3=3`
entre os grupos aceitos; os demais bindings não recebem um perfil compacto.
Os grupos aceitos preservam adicionalmente as atribuições de estado nomeadas e
os IDs dos comandos de seus fluxos completos. Em `star0001-head`, são 3 grupos
com estado preservado e IDs de comando; eles carregam `GeoPrimState`, enquanto
os `SHAPENAME` observados ficam em primitivas que não passaram o gate do strip.
Essa associação continua evidência estrutural e não é convertida em material,
textura ou binding final de draw.
No inventário global, os 2.387 draws que passam os cinco bindings produzem
7.161 perfis: 2.387 no slot 0/3 componentes, 2.387 no slot 1/2 componentes e
2.387 no slot 3/3 componentes. Essa repetição valida a regra de leitura, não
o significado dos atributos.
Na primeira amostra, relocações dos parâmetros globais referem-se a estado de
primitiva e textura, não diretamente aos `__VB`; por isso a associação dos
buffers ao draw e a topologia não são inferidas pelo exportador.

O parser também percorre `__Model` → layer → descritor, interpreta o estado
`PT_TRIANGLESTRIP` e lê os cinco slots do comando `id=9`. No lote, 2.389 de
2.390 primitivas têm um único candidato float3 com ordinal correspondente e
dentro das bounds; 2.387 primitivas têm os cinco slots com strides coerentes
com os grupos `__GPvb`, incluindo as 1.615 strips explícitas. Essa coerência
reforça a associação do layout, mas não prova sozinha o binding usado pelo
draw call nem a semântica completa dos parâmetros do comando.
O inventário verbose também exibe o terceiro word de cada slot (`AuxiliaryValue`).
Na amostra real, ele se repete em alguns atributos e zera em outros, mas não
coincide diretamente com `ByteLength`, `DataOffset` ou offsets dos records
`__VB`; o valor permanece evidência não tipada e não é tratado como binding.

O leitor também preserva os fluxos de comandos EAGL sem nomear sua semântica:
cada comando mantém índice, offset relativo à seção, ID, tamanho em words,
bytes crus e relocations, e o fluxo informa se terminou no terminador observado
ou se foi truncado/malformado. O leitor de `id=9` usa essa mesma representação,
evitando dois parsers divergentes. Em `star0001-head.ord`, o smoke real encontrou
5 primitivas, 5 fluxos completos e 75 comandos; os IDs foram apenas
inventariados (`2,4,5,6,9,14,15,16,20,26,29,31`), sem tratá-los como textura,
material ou binding de draw.
O inventário morph também registra uma assinatura de partição por primitiva,
limitada à ordem, contagem de comandos, tamanho, completude e hash dos bytes
crus. Essa assinatura permite comparar limites observados sem interpretar IDs,
payloads ou operações GPU.

Os símbolos de estado também são separados em atribuições estruturais
`nome=valor`, sem convertê-las em semântica de material PC. A inspeção real
expõe, entre outros, `SetTransparencyMethod`, `SetAlphaBlendMode`,
`SetAlphaTestEnable`, `SetAlphaCompareValue`, `SHAPENAME` e
`XBOXEXTOBJ_SetClampU/V`; 2.389/2.390 primitivas com candidato de posição têm
ao menos uma atribuição. Isso ainda não valida skinning, rigging, textura ou
o significado final desses estados no draw.

O comando `eagl-shapename-index` compara os valores literais preservados em
`SHAPENAME` com os basenames das entradas `.xsh` do próprio `data/models.big`.
Na amostra completa, os 886 pares `.ord/.orl` válidos produziram 774
ocorrências (`head,1` e `tops,1`) contra 724 entradas `.xsh`; não houve match
exato com o basename normalizado (`head`/`tops`). `tops` aparece apenas como
prefixo de 152 basenames e `head` de nenhum, uma relação ambígua. Isso rejeita
somente a hipótese de binding textual direto; não resolve a tabela de materiais
nem autoriza escolher uma imagem. O mesmo índice descompacta 724/724 pacotes
`.xsh`, valida 1.545 registros internos e encontra 14 nomes internos distintos;
`tops` coincide com um nome interno em 224 pacotes, enquanto `head` não
coincide, ainda sem evidência de qual pacote deve acompanhar cada modelo.

Há agora um loader de material restrito a vínculos reproduzíveis no mesmo
tronco. `eagl-material-smoke` exige que o objeto `.ord` contenha o
`SHAPENAME`, deriva o `.xsh` do basename do `.big`, valida o pacote SHPX e
seleciona a entrada interna com o mesmo nome. Em `tops0030.big`, a cadeia real é
`tops0030.ord` (`SHAPENAME=tops,1`) → `tops0030.xsh` → registro `tops` no
offset 112, com 33.024 bytes; a imagem PAL8 decodificada mede 256x124 e tem
126.976 bytes RGBA.

O comando `eagl-candidate-object-smoke` confirma para esse objeto 2 grupos,
341 vértices, 337 triângulos e 13 primitivas. O comando
`eagl-material-preview` usa a textura decodificada no rasterizador software
candidato de `Slot1Attributes` e gera um PPM; a execução real texturizou os 2
grupos e produziu o checksum `FB81D8B0DBB5E819`. A operação continua limitada a
esse caminho candidato: não afirma que todo slot 1 é UV, não resolve skinning
ou deformação e não atribui semântica final aos comandos de draw do Xbox.

O comando `eagl-material-batch-smoke` aplica a mesma regra de forma conservadora
aos arquivos `.big` que têm `.xsh` homônimo. A execução real encontrou 24
matches distintos, todos na família `tops`, com `SHAPENAME=tops,1` normalizado,
entrada SHPX interna `tops` e decodificação PAL8 válida. O resultado fortalece
somente essa família; `head`, `face`, `hair` e outros nomes continuam sem um
binding independente.
Uma segunda execução em `tops0040.big` confirmou o caminho com textura PAL8
256x256 e malha candidata de 2 grupos, 527 vértices e 523 triângulos; os 2
grupos foram texturizados e o checksum foi `F130B0BAB3361DDF`.

Essa cadeia também pode ser passada ao runtime headless, mas somente por uma
opção explícita:

```text
dotnet run --project fifa_pc -- run-headless "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 80 --demo --candidate-material
```

O modo padrão usa `tops0030.big`/`tops` e o primeiro objeto `.ord` compatível;
os argumentos opcionais permitem informar `nested.big`, `SHAPENAME` e
`objeto.ord`. Em 80 frames, a integração produziu checksum
`CFA4BCBEE162C121`, enquanto o modo padrão permaneceu em
`5405DE72BE1B4FC1`. A textura candidata só entra se o vínculo same-trunk,
o registro SHPX interno e a malha gated forem validados; caso contrário o
renderer conserva o fallback sólido. Essa é uma regra de integração do
runtime novo, não uma prova de que `Slot1Attributes` são UV em geral ou de
que os comandos GPU, materiais, skinning, rigging e animação originais foram
decodificados.
Uma execução independente de `tops0040.big` no mesmo caminho do
`GameRuntime` validou textura PAL8 256x256, malha de 527 vértices/523
triângulos e 80 frames, com checksum `3A2D00B09DC319F6`. A repetição está
limitada à família `tops` e continua sendo candidata.
No gameplay clean-room, o modo opt-in `run-headless ... --round-demo` também
exercita o pós-gol com intervalo, reposicionamento e `RESTART`; essa política
fica fora da leitura dos formatos e não altera a classificação dos assets como
candidatos.
O parâmetro `--match-seconds` acrescenta um relógio authored de teste: ao
zerar, o runtime emite `FULL_TIME` uma vez e congela o frame seguinte. Esse
evento não é usado para inferir timing de asset e continua sem binding de áudio.
O preview e o runtime também aceitam `--clean-room-characters`, uma camada
procedural authored que desenha os atores a partir do estado da partida. Ela
agora escolhe perfis authored para posse, passe, drible, chute, gol, reinício e
`FULL_TIME`, mas não consome nem reclassifica os bytes de AXT/morph como
animação, rig ou skinning.
O modo headless aceita `--frame-output <saida.ppm>` para materializar o último
frame em QA; o destino é validado para não coincidir com a ISO.
Ele também aceita `--match-trace-output <saida.tsv>` para registrar a sequência
authored de eventos com `sequence`, `frame`, placar e posse. A execução real
validada escreveu 6 eventos, de `KICKOFF` a `GOAL_HOME`; esse arquivo apenas
verifica a simulação nova e sua integração com os consumidores do runtime, sem
atribuir IDs, timing ou semântica aos assets observados.
Para testes reproduzíveis de controle, `run-headless` aceita
`--input-script <entrada.tsv>`. O formato authored tem as colunas
`frame`, `move_x`, `move_y`, `pass`, `shoot`, `trick` e aceita a coluna opcional
`panna`; o leitor rejeita frames
fora de sequência, eixos não finitos/fora de `[-1,1]` e booleanos desconhecidos.
`fifa_pc/gameplay-clean-room-input.tsv` é apenas uma fixture de parser/runtime,
não um input extraído do jogo original nem um binding para assets.
Na execução validada, a fixture teve SHA-256
`B2F2969BFF1FA22387158B197EEF09479D8BDD682D26B6AB139B11F0A2AE22E8` e o
trace resultante teve SHA-256
`6288D477F63B66EB445F24E6C55D0F3BD0635B0D0BF7F5480CAD10754AD16D52`.
O arquivo authored de 80 frames `fifa_pc/gameplay-clean-room-demo-input.tsv`
também reproduziu a sequência clean-room do `--demo`, com checksum
`C0249FD97A01C3F4` e 6 eventos; seu trace manteve SHA-256
`2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`. Essa
reprodução valida o parser e o runtime, não uma semântica de input extraída.
O mesmo caminho aceita `--match-trace-expected <entrada.tsv>` para comparar
textualmente o trace authored reexecutado com um esperado; a fixture de 80
frames passou com `matched=True`, 6 eventos e checksum `C0249FD97A01C3F4`, e o
trace reexportado coincidiu byte a byte com SHA-256
`2A71B2040527E1E292EC0B4F88FF7A8D07F6082012F25F4C9B1D9DFCABB19380`. A opção
é um gate de determinismo do runtime novo: não interpreta input, eventos,
timing ou semântica do jogo original e não permite usar a ISO como esperado ou
como destino de saída.
O runtime ainda expõe a regra authored opt-in
`--match-rule first-to-goals --target-goals <N>`; o caso validado terminou em
`FULL_TIME` no frame 70. A referência comportamental externa e sua limitação de
plataforma estão registradas em `docs/FIFA_EXTERNAL_BEHAVIOR_EVIDENCE.md`; nada
desse modo é usado para inferir formato ou binding de asset.
O fixture `fifa_pc/gameplay-clean-room-panna-input.tsv` produziu `PANNA` e
`FULL_TIME` no frame 31 com `goals-or-panna`; o consumidor de áudio também drena
ambos os eventos quando são emitidos no mesmo tick, preparando `panna` apenas
como rota authored.
O runtime também preserva a distinção entre objeto e grupo geométrico: os
24 objetos carregados mantêm `SHAPENAME=tops,1`, mas nenhum dos 24 grupos
geométricos candidatos o carrega diretamente. Os estados aparecem em
primitivas separadas em `tops0030`; portanto a textura candidata continua
opt-in, e a implementação não inventa herança para UV, material ou GPU.

As prévias `tops0030-material-preview.ppm` e `tops0040-material-preview.ppm`
foram inspecionadas visualmente e mostram fragmentação/alongamento fortes. A
auditoria `eagl-candidate-geometry-audit` mede a representação sem tentar
corrigi-la: nos 24 matches, 0 índices estavam fora dos grupos, 0 vértices eram
não finitos e 0 triângulos tinham índices repetidos; 46 tinham área 3D
exatamente zero, 48 tinham área XY exatamente zero e 196 eram degenerados após
o arredondamento da projeção, contra 7.131 visíveis. Esses números descrevem o
mesh candidato e o rasterizador software, não provam a topologia do jogo nem
permitem inventar índices, reinícios de strip, UVs, materiais, skinning,
rigging, animação ou comandos GPU.

O comando `eagl-candidate-projection-preview` também gera vistas geométricas
`xy`, `xz` e `yz` do mesmo candidato, sem aplicar textura nem escolher uma
câmera. As seis vistas reais de `tops0030` e `tops0040` mudaram a silhueta,
mas mantiveram fragmentação; nenhuma projeção foi promovida a convenção de
eixos do jogo. Esse artefato de QA não altera o caminho renderizado nem resolve
topologia, UV, material, skinning, rigging, animação ou comandos GPU.

Para inspecionar os candidatos selecionados apenas por float3 finito e bounds,
o comando abaixo exporta uma nuvem OBJ com primitivas `p`, nunca faces:

```text
dotnet run --project fifa_pc -- eagl-position-cloud "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head.ord
```

O arquivo é criado em `exports_fifa_mesh_candidates/` e não sobrescreve um
arquivo existente. É um artefato de diagnóstico, não um modelo jogável.

Para exportar faces apenas dos strips declarados, use:

```text
dotnet run --project fifa_pc -- eagl-strip-candidate-export "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head.ord
```

No exemplo, saíram três grupos, 534 vértices e 528 triângulos; dois grupos
foram omitidos por não terem um tipo strip explícito. A exportação agora exige
também layout de slots/strides coerente com `id=9`. Todos os índices OBJ são
válidos; ainda falta confirmar no formato original o binding de draw e se a
ordem do stream representa toda a topologia pretendida.

Internamente, o exportador reutiliza `EaglCandidateMeshBuilder`, que materializa
os candidatos como vértices e triângulos gerenciados antes de serializar OBJ.
O leitor agora materializa também bindings estruturais `slot → stream` quando
os cinco slots, strides e símbolos `__GPvb` coincidem; 2.387/2.390 primitivas
do inventário passam essa regra. Isso torna os índices e o fetch de vértices
testáveis e reutilizáveis por um renderer futuro, mas não transforma a seleção
em uma malha final: materiais, atributos além de posição, binding completo do
draw, rigging e animação continuam sem confirmação.
Os grupos gerenciados preservam ainda os valores finitos do slot 1 em
`Slot1Attributes`: no `star0001-head`, são 534 valores para 534 vértices nos
três grupos aceitos. O campo continua deliberadamente genérico; não é
serializado como UV nem ligado a uma textura sem uma prova independente.
Eles também preservam os perfis compactos dos bindings finitos, separados do
campo `Slot1Attributes`, para que medições futuras possam comparar os slots sem
transformar números em materiais ou pesos.

O comando `eagl-candidate-smoke` percorre o mesmo caminho de leitura em modo
somente leitura e o host windowed desenha esse mesh preenchido com uma cor
provisória e contornado sobre o jogador. No objeto `star0001-head`, a etapa
produz 3 grupos, 534 vértices e 528 triângulos; a visualização ainda não é
tratada como prova de materiais, skinning, deformação ou rigging finais.
`eagl-candidate-preview` gera um PPM combinando essa malha, `titleimage.xbx` e a
cena de gameplay gerenciada para inspeção visual do caminho de renderização.
Com `--slot1-profile`, o mesmo comando colore cada triângulo pela média dos
dois componentes de `Slot1Attributes`; essa opção é uma visualização de
diagnóstico e não atribui semântica UV ou material ao atributo.

Inventariar os objetos de modelo, incluindo caixas delimitadoras:

```text
dotnet run --project fifa_pc -- big-model-inventory "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big
```

Adicionar `--verbose` mostra uma amostra de seções, símbolos, descritores e
comandos de renderização usada durante a investigação do formato.
No smoke de um objeto específico, `eagl-candidate-smoke --commands` imprime
os bytes crus de cada comando e suas relocations; isso permite comparar os
payloads sem atribuir semântica especulativa aos IDs.
O comando `eagl-candidate-object-smoke <iso> <nested.big> <objeto.ord>
--commands` oferece a mesma saída limitada ao objeto selecionado. Em
`tops0030`, os 13 fluxos terminam corretamente; as primitivas com estado
`PT_TRIANGLESTRIP` e as primitivas TAR com `SHAPENAME=tops,1` são preservadas
separadamente, portanto essa saída não é um binding de draw ou material.
O comando `eagl-candidate-state-correspondence <iso> [--report <saida.tsv>]`
compara essas populações em todos os 24 objetos `tops` verificados. Foram
produzidos 269 pares, com 0 igualdades exatas de bytes, 0 igualdades de forma
de fronteira, prefixo comum máximo de 8 bytes e 269/269 fluxos completos. A
comparação continua sendo evidência bruta; não associa `SHAPENAME` a mesh,
material, UV, skinning, rigging, animação ou GPU.
O controle `eagl-state-geometry-adjacency-audit <iso> [--report <saida.tsv>]`
registra os índices/camadas das duas populações por objeto, sem tentar resolver
uma correspondência. A execução real teve 24/24 linhas, 0 pares com o mesmo
índice, 48 pares adjacentes, 269 pares na mesma camada e 0/24 grupos
geométricos com `SHAPENAME` exato. O artefato
`fifa_pc/eagl-state-geometry-adjacency.tsv` é um controle negativo de
adjacência; não cria binding de mesh, material, UV ou GPU.
O controle `eagl-state-geometry-profile-audit <iso> [--report <saida.tsv>]`
compara uma assinatura opaca da sequência de IDs/tamanhos dos comandos, além
de suas contagens e bytes, para cada par de grupo geométrico e primitiva com
`SHAPENAME`. A execução real materializou 269 linhas em 24 objetos: 0
sequências exatas, 0 pares no mesmo índice, 48 pares vizinhos, 1 perfil de
comando geométrico distinto e 3 perfis de estado; 4 linhas têm perfil de
estado não repetido, sem que isso produza associação. O relatório
`fifa_pc/eagl-state-geometry-profile-audit.tsv` permanece evidência de perfil
opaco e não interpreta comandos, materiais, UVs, skinning, rigging, animação
ou GPU.
O comando `eagl-candidate-attribute-audit <iso> [--report <saida.tsv>]` mede
os valores completos do slot-1 sem atribuir nome de atributo. Na execução
real, 24 objetos produziram 38 grupos e 7.403 valores finitos próximos da
faixa unitária; nenhum grupo carregou diretamente `SHAPENAME` ou clamp,
embora 180 primitivas carregassem esses estados. Essa separação não é binding
de UV, material ou draw.

O inventário também resume os fluxos preservados: na ISO atual são 2.390/2.390
completos, 0 incompletos, 2.387/2.390 bindings explícitos de fetch e 37.111
comandos. A distribuição observada é
`2=2389,4=2390,5=1616,6=1970,9=11948,14=2390,15=2390,16=1616,20=3232,26=2390,29=2390,31=2390`.
Esses números são cobertura do parser e evidência bruta; os IDs não são
interpretados como materiais, texturas ou draw calls efetivos.

Objetos morph standalone também são inspecionados sem alterar a ISO. O comando
abaixo percorre um `.o` dentro de um BIG aninhado e mostra as seções, símbolos,
strings nomeadas e ponteiros internos resolvidos:

```text
dotnet run --project fifa_pc -- eagl-object-symbol-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/models.big star0001.big star0001-head_morph.o
```

Na amostra real, `star0001-head_morph.o` tem quatro nomes `BS_*`, os atributos
`Coordinates` e `Normals` e 8 relocations; 594 objetos do inventário têm
`__Morph:::`. Isso é evidência de catálogo/ponteiros, não uma prova de que os
records já possam ser aplicados como blend shapes ou animação. A opção
`--payload` do mesmo comando agora imprime janelas hexadecimais limitadas ao
redor dos campos relocados. Isso permite comparar cabeçalhos e agrupamentos
entre objetos sem transformar words repetidos em contagens de vértices, ossos
ou poses; no inventário, os 594 objetos produziram 4.719 janelas/campos e 42
assinaturas distintas de offsets; 2.372 links de shape e 1.142 links de
atributo foram resolvidos por destino textual. O leitor também separa, apenas quando o
destino da relocation é explicitamente nomeado, ponteiros para `BS_*` e para
`Coordinates`/`Normals`; isso vincula nomes a campos, mas não decodifica os
records. O decoder de records continua pendente.
O modo `--raw` também permite olhar um `.orl` isolado sem forçar o parser ELF:
`star0001-head.orl` tem 6.032 bytes e começa com nomes de seções (`.symtab`,
`.strtab`, `.shstrtab`, `.data`, `.rel.data`), mas não é aceito como ELF. Essa
saída é somente evidência bruta; a reconstrução `.ord/.orl` continua sendo a
única rota tipada e não há ainda um parser independente de records `.orl`.

Como correlação estrutural adicional, `eagl-morph-sibling-smoke` reconhece
somente o padrão de filename observado `*_morph.o` → `*.ord` + `*.orl` dentro
do mesmo BIG e valida o irmão pela reconstrução ELF dividida. O inventário
`eagl-morph-sibling-inventory` percorreu os 391 BIGs internos de
`data/models.big`: 594 morphs encontraram irmãos `.ord/.orl` válidos, todos os
594 irmãos tinham símbolos geométricos e 593 tinham bounds; 42 assinaturas de
relocação foram observadas. Isso é evidência de pacote/nomenclatura e
repetição de layout, não um binding de morph. Nenhum word foi promovido a peso,
índice, osso, pose ou frame.
O mesmo inventário encontrou `__PosePaletteBank:::` em 593/594 morphs. As
assinaturas repetidas preservam ainda os perfis de contagem de 4 nomes `BS_*`,
1 ou 2 atributos e esse símbolo; essa regularidade é apenas evidência de
layout/símbolos e não decodifica palette de poses, pesos, ossos ou animação.
Os offsets também são reportados relativamente ao primeiro campo: o perfil
mais frequente cobre 173 morphs com gaps `0x0,0x4,0x8,0xC,0x24,0x28,0x34` e
um campo posterior variável. Essa normalização só mede repetição de layout
bruto; não nomeia records nem atributos.
Uma impressão SHA-256 do payload da seção, mascarando apenas strings nomeadas
e words de relocation, foi distinta nos 594/594 morphs. A repetição, portanto,
fica limitada aos offsets/estrutura observados; não há template bruto
compartilhado suficiente para promover um decoder de records.
Uma comparação local adicional usa janelas de raio 16 em torno dos campos de
relocation, mascarando apenas strings nomeadas e words de relocation antes do
hash. Os 594 morphs produziram 326 assinaturas locais, 404 pares
morph-layout/janela e 11 layouts morph com múltiplas assinaturas locais. Isso
mede conteúdo bruto repetido ou variável dentro das janelas, sem atribuir
records, pesos, ossos, poses ou frames.
Uma assinatura estrutural independente, baseada apenas em tamanho/posição da
seção, strings observadas e campos/alvos de relocation, formou 393 perfis em
594 objetos; o maior perfil repetiu 42 vezes. Esses perfis são evidência de
layout bruto e não nomeiam records, pesos, ossos, poses ou frames.
A correlação com os companions `.ord/.orl` preserva somente contagens/totais de
seções, símbolos, relocations, classes de símbolos geométricos e presença de
bounds. Ela produziu 417 assinaturas de companion e 443 pares de perfis; 32
dos 393 perfis morph aparecem com mais de uma assinatura companion. Isso
impede tratar o layout como seletor de geometria ou binding de morph.
Uma camada refinada calcula um hash somente dos headers de seção, posições e
tamanhos dos símbolos geométricos e offsets relativos/tipos de relocation. Ela
produziu 444 assinaturas, 447 pares morph-layout/layout e 32 layouts morph com
múltiplos layouts companion; continua evidência estrutural sem binding.
Os streams de comandos são resumidos separadamente, sem interpretação, por
primitive/layer, offsets, assignments textuais, IDs/tamanhos, hashes dos bytes
crus e relocation. Foram 592 assinaturas, 593 pares morph-layout/stream e 38
layouts morph com múltiplos streams; nenhum ID foi nomeado como operação GPU.
Os descritores `__VB` dos mesmos companions também são resumidos somente por
streams, bytes, elementos, strides, flags e seções: 235 assinaturas, 407 pares
morph-layout/descritor e 13 perfis morph com múltiplas assinaturas de descritor.
Essa é metadata validada de buffer; não há atribuição a posição, normal, UV,
peso ou deformação.
A ordem relativa dos registros `__VB` acrescenta uma assinatura hash somente de
seção, offsets relativos, descriptor/data offsets, bytes, stride e flags. O
inventário produziu 358 assinaturas, 431 pares morph-layout/sequência e 27
layouts morph com múltiplas sequências; isso continua semântica não decodificada.
A medição numérica neutra conta apenas palavras float empacotadas finitas e a
containment dos streams de stride 12 nas bounds declaradas: 12 perfis, 398
pares morph-layout/número e 5 perfis morph com mais de uma assinatura numérica.
O resultado permanece estrutural e não decodifica componentes.
A topologia candidata dos companions é resumida separadamente por grupos,
vértices, triângulos e primitivas ignoradas: 593 companions produziram
assinatura, com 221 valores distintos incluindo o caso vazio, 399 pares
morph-layout/topologia e 6 perfis morph com múltiplas topologias. Isso não
estabelece a topologia original nem um binding de deformação.
Uma auditoria complementar (`--geometry-report` e `--geometry-summary` no
`eagl-morph-sibling-inventory`) mede a geometria numérica dessa mesma
representação candidata: finitude, índices repetidos, áreas zero, degeneração
raster e perfil de arestas. O resultado real tem 594 linhas em 393 layouts;
38 layouts variam, 32 com topologia candidata estável e geometria variável.
Um layout tem uma linha sem assinatura por ausência de bounds; 393 layouts são
completos e ELF-validos e 392 têm bounds em todas as linhas. Essa camada é QA
de candidato e não altera os rótulos de ambiguidade nem decodifica topologia,
UV, material, skinning, rigging, animação ou comandos GPU.
A correlação opcional (`--geometry-correlation-report` e
`--geometry-correlation-summary`) compara essa assinatura com a assinatura de
payload pareada: 38 layouts variam no par, 27 têm payload estável e geometria
variável e 11 variam em ambas as camadas. É uma camada de triagem, não uma
decodificação semântica.
O comando independente `morph-candidate-geometry-ambiguity-audit` cruza a
coluna geométrica com os rótulos publicados: 38/38 layouts com geometria
variável são ambíguos, 0 são estáveis, 0 ambíguos ficam sem variação geométrica
e há 0 divergências. Isso é consistência de QA, não decodificação de morph.
Para comparar repetições sem escolher uma semântica, o inventário combina agora
as camadas independentes em uma assinatura estrutural pareada: layout morph,
janelas de payload normalizadas, layout ELF companion, stream de comandos opaco,
sequência de registros `__VB`, perfil numérico, topologia candidata e
coincidências numéricas cross-file. O resultado
real foi de 593 assinaturas pareadas; 355 dos 393 layouts morph têm composição
única e 38 têm composições múltiplas. Essa chave é apenas uma ferramenta de
repetição/triagem e não um binding de morph, rig, pose, peso, material, UV,
skinning, animação ou GPU.
Uma camada cross-file compara words little-endian alinhadas do payload morph com
valores de metadata numérica já validados no companion, excluindo strings
nomeadas e words de relocation. O inventário produziu 406 assinaturas de
coincidência e 12 layouts morph com múltiplas formas. Isso registra igualdade de
campos comparados; não converte matches em contagens de vértices, índices,
pesos, poses ou frames.
O comando `eagl-morph-sibling-inventory` também pode receber `--catalog
<saida.tsv>` para gravar, fora da ISO, uma linha por morph com o par nominal,
estabilidade do layout e as assinaturas estruturais. O catálogo real tem 594
linhas; 355 layouts são estáveis e 239 linhas pertencem aos 38 layouts
ambíguos. O arquivo é um índice de triagem, não uma tabela de bindings.
O catálogo inclui `candidate_geometry_signature` como coluna de QA da
representação candidata, sem atribuir semântica de topologia ou morph.
As opções `--geometry-report <saida.tsv>` e `--geometry-summary <saida.tsv>`
gravam, respectivamente, o perfil por layout e sua distribuição agregada; os
artefatos verificados são `fifa_pc/morph-sibling-candidate-geometry-audit.tsv`
e `fifa_pc/morph-sibling-candidate-geometry-audit-summary.tsv`.
As opções `--geometry-correlation-report <saida.tsv>` e
`--geometry-correlation-summary <saida.tsv>` gravam a correlação de triagem;
os artefatos verificados são `fifa_pc/morph-sibling-candidate-geometry-correlation.tsv`
e `fifa_pc/morph-sibling-candidate-geometry-correlation-summary.tsv`.
O par `fifa_pc/morph-sibling-candidate-geometry-ambiguity-audit.tsv` e seu
resumo registra a checagem independente desses rótulos.
Com `--ambiguity-report <saida.tsv>`, o mesmo comando grava uma matriz de 393
layouts. Os 38 layouts ambíguos variam, respectivamente, em stream de comandos
(38), layout companion (32), registros `__VB` (27), coincidência numérica (12),
janelas de payload (11), mesh candidato (6) e perfil numérico (5). A matriz
descreve variação estrutural, não causalidade ou semântica.
Ela também reporta a interseção exata dessas camadas com os 355 layouts
estáveis: `numeric_descriptor` aparece em 38 dos ambíguos, `candidate_mesh` em
32, `payload_window` em 26, `vertex_record` em 25 e `companion_layout` em 2;
`command_stream` e `companion_numeric_match` não têm interseção. O resultado é
 apenas recorrência de assinatura entre populações, não um binding.
O comando independente `morph-ambiguity-summary <ambiguity-report.tsv>
<summary.tsv>` agrupa os 38 layouts ambíguos pela composição exata das camadas
de variação. A execução atual produziu 8 composições e 239 linhas; o resultado
retém contagens de overlap com controles estáveis e hashes opacos das chaves de
layout para rastreabilidade. Não há interpretação de campos ou atribuição a
morph, pose, rig, peso, frame, timing ou skinning.
O comando `morph-ambiguity-byte-summary <ambiguity-report.tsv>
<byte-report.tsv> <summary.tsv>` junta as 8 composições ao relatório byte-level
pela chave estrutural exata. A execução associou 239/239 linhas; 213/239
repetiram bytes e limites de comando, 0/239 tiveram assinatura estrutural de
comando exata e 222/239 coincidiram apenas na categoria valor/token. Isso
continua sendo evidência alinhada dos relatórios, não uma decodificação de
comandos, morph, pose, rig ou skinning.
O comando `morph-ambiguity-numeric-summary <ambiguity-report.tsv>
<numeric-gap-report.tsv> <summary.tsv>` cruza os gaps pela mesma chave
estrutural: 17 linhas cobrem 10 layouts em 4 composições, enquanto 28 layouts
ambíguos não têm linha de gap. Foram preservadas as métricas de fronteira
relativa, janela de payload, boundary numérico e offset canônico sem atribuir
semântica aos tokens ou offsets.
O comando `morph-ambiguity-group-metadata <ambiguity-report.tsv>
<byte-report.tsv> <numeric-gap-report.tsv> <metadata.tsv>` junta, por chave
estrutural exata, os perfis de variação dos 38 layouts ambíguos aos relatórios
byte-level e de gaps. A saída real tem 38 linhas, 239 linhas byte associadas e
17 linhas de gap; 28 layouts não possuem linha de gap. Ela contém contagens e
fronteiras observadas apenas, sem decodificar morph, pose, rig, peso, frame,
timing, skinning, material, UV ou GPU.
O comando `morph-ambiguity-independent-audit <structural-catalog.tsv>
<ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]` reagrupa o
catálogo bruto e confere independentemente os oito contadores de perfil. Foram
auditadas 594 linhas em 393 layouts, com 0 inconsistências de metadata/contagem e
0 divergências entre os rótulos `ambiguous/stable` e a variabilidade medida nas
oito camadas.
Nos 38 ambíguos, 26 preservam mesh candidata estável com companion variável,
6 variam também na mesh candidata e 38/38 variam em alguma camada estrutural
medida. Os hashes resultantes são rastreabilidade dos conjuntos observados,
não bindings nem semântica de morph, rig, pose, peso, animação, material, UV,
skinning ou GPU.
O comando `eagl-morph-name-state-audit <morph-structural-catalog.tsv>
<saida.tsv>` faz somente um cruzamento lexical entre os nomes `BS_*` preservados
pelos 594 morphs e os valores crus de `SHAPENAME` dos companions. A execução
encontrou 2.360 nomes morph distintos, 391 linhas de estado companion em 594
linhas completas e 0/594 coincidências, tanto exatas quanto normalizadas antes
da vírgula. Esse resultado negativo é controle lexical; não decodifica morph,
mesh-group, material, UV, skinning, rigging, animação ou GPU e não cria binding.
O comando `eagl-morph-animation-name-negative-control` faz um controle lexical
independente entre os mesmos nomes completos de shape e o índice de animação.
Na ISO, os 594 rows morph estavam completos, contra 1.578 nomes de arquivo e
19 nomes de banco; houve 0 coincidências exatas em ambos os conjuntos. A
comparação não normaliza tokens e não prova ausência de relação semântica,
timing, pose ou rig.
O comando `audio-event-named-decode-smoke <iso> [--report <saida.tsv>]` tenta
decodificar os ranges associados aos 17 nomes extraídos do EVT. Na ISO, 7
candidatos têm canais no header observado e 10 têm uma hipótese mono única
validada por layout e frames `SCDl`; os 17 produziram WAVs válidos, totalizando
2.915.700 bytes. Isso acrescenta evidência de payload de áudio, sem criar
binding automático de evento, ABKC ou gameplay.
O comando `audio-abk-batch-decode-smoke <iso> [--report <saida.tsv>]
[--max-attempts <n>]` percorre os 16 bancos ABKC e tenta índices bounded
observados nos ranges AST e BNK. Na ISO, 15 bancos decodificaram pelo menos um
candidato, totalizando 10.051.852 bytes WAV, com fontes AST=6, BNK=9 e none=1;
`data/audio/CONTROL.abk` foi rejeitado por não oferecer candidato de índice.
Esse resultado valida payload e decoder gerenciado, não cria binding para EVT,
gameplay, materiais, UVs, skinning, rigging, animação ou GPU.
Quando o runtime recebe um opener read-only, `CatalogAudioMixer.Initialize`
repete essa seleção bounded e mantém os WAVs preparados em cache: a execução
headless observou 15 bancos preparados e 1 rejeitado, com fontes AST=6, BNK=9
e none=1. O mesmo ciclo pré-prepara os 17 candidatos EVT nomeados: 7 têm canais
no header observado e 10 têm uma hipótese mono única validada por layout e
frames `SCDl`, totalizando 2.914.952 bytes PCM. Esse cache não escolhe eventos
nem cria associação de gameplay.
O relatório opcional `--byte-report <saida.tsv>` compara os bytes crus dos
streams de comando e os registros canônicos UTF-8 da coincidência numérica com
uma linha de controle de cada layout estável. Na execução atual, 213/239 linhas
ambíguas têm igualdade byte a byte no stream de comando, embora 0/239 tenham
assinatura estrutural de comando exata; 26/239 não têm igualdade e 0/239 têm
igualdade exata no registro numérico. Essa comparação não interpreta
comandos, materiais, atributos ou números.
O comando `eagl-material-batch-preview` percorre os 24 vínculos já verificados
e rasteriza cada mesh/textura candidata em PPM, com manifesto de estado e
checksum. A execução atual gerou 24/24 imagens, 0 rejeições e 24 checksums
distintos; 14 meshes têm dois grupos e 10 têm um, totalizando 7.403 vértices,
7.327 triângulos e 38 grupos texturizados. Isso continua semântica candidata:
não é prova de UV, material GPU, skinning ou deformação.
O smoke `eagl-material-runtime-batch-smoke` também pode receber
`--report <saida.tsv>` para materializar uma linha por frame candidato. O
relatório `fifa_pc/eagl-material-runtime-batch.tsv` registra a cadeia
`titleimage.xbx` + mesh/textura + `SoftwareGameplayRenderer`/`PreviewRenderer`
com checksum, estado textual do objeto e estado do grupo marcado como
`exact`/`unproven`. A execução atual tem 24/24 frames, 0 rejeições, 24
checksums distintos e 38 grupos texturizados; isso é rastreabilidade de
integração candidata, não binding final de UV, material, skinning, rigging ou
GPU.

O decoder EA-XA converte `.ast` Xbox v3 em WAV PCM16 e o comando `audio-play`
faz reprodução síncrona no dispositivo Windows para WAVs decodificados de até
32 MiB. A investigação estrutural dos arquivos relacionados pode ser repetida
com:

```text
dotnet run --project fifa_pc -- audio-structure-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

A validação integral dos seis streams `.ast` pode ser executada sem alocar o
WAV final:

```text
dotnet run --project fifa_pc -- audio-validate "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Na ISO, o comando aceitou 6/6 streams e rejeitou 0; ele percorre os blocos
`SCDl`, tabelas de canais e contagens de frames.

Para inspecionar a correlação estrutural entre `eventspa.evt` e o catálogo
PA, use:

```text
dotnet run --project fifa_pc -- audio-event-candidate-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

O comando encontrou 314 matches exatos de words alinhados, cobrindo 283 rows
distintas de `pcpahdr.off`; as rows paralelas de `pcpadat.off` estão disponíveis
e começam em `SCHl`. O resultado é deliberadamente chamado de candidato: os
textos/IDs do `.evt` não selecionam automaticamente essas rows. A exceção
explícita e limitada está documentada por `audio-event-named-candidates`: 17
nomes PA têm correlação header/data, 7 têm canais no header e 10 têm hipótese
mono única validada; o runtime usa `GOAL_HOME -> goal` e `GOAL_AWAY -> goal`
para a rota authored de gol e, por escolha explícita clean-room, `TRICK ->
panna`. Essas escolhas preparam rows nomeadas sem alegar semântica original.

O comando `audio-event-text-smoke` mostra os runs de texto e seus bytes vizinhos.
Na `eventspa.evt`, os nomes (`panna_goal`, `beat`, `chance`, `gb_goal` etc.)
formam uma string table contígua a partir de `0x2090`, sem ponteiro direto
comprovado para `pcpahdr` ou ABKC.

`audio-event-index-smoke` também verifica a faixa alinhada imediatamente anterior
à tabela: 17 pares offset relativo + word seguinte formam a família observada
`word & 0xFFFF == 0x4F6B`, incluindo `3panna_goal` com offset relativo zero.
Nenhum dos 17 words foi encontrado por busca exata nos `.abk` da ISO. O resultado
é mantido como evidência do índice/hash do EVT; não é tratado como ID ABKC ou
seletor de amostra.

O comando `audio-abk-inspect` percorre a cadeia estrutural observada de módulos,
players, sample tables e entradas `ABKC`. Na ISO, 16/16 bancos são aceitos e
39 entradas externas apontam para `SCHl` em ASTs companheiros, totalizando 70
ranges quando segmentos de loop são separados. Com `--validate-streams`, os
ranges podem ser validados pelo decoder EA-XA sem alocar WAV; o parser não
atribui nomes EVT nem transforma índices em seleção automática.
Quando presente, o mesmo comando verifica a tabela `BNK` embutida: 13 bancos
da ISO têm BNK e 231 offsets de cabeçalho ficam dentro do asset. Os headers
`PT`/`GSTR` agora são percorridos com limites derivados dos offsets seguintes;
as tags são conservadas como evidência bruta e os descritores que expõem o
codec Xbox IMA `0x14` são reconhecidos. A validação dos ranges derivados dos
offsets confirmou 231/231 sequências de blocos com headers Xbox IMA válidos,
sem associá-los a eventos.

`audio-abk-decode <iso> <arquivo.abk> <índice-do-som> <saida.wav>` permite uma
seleção explícita de som RAM e gera WAV PCM16 pelo decoder gerenciado Xbox IMA.
Essa operação é uma validação/extração controlada: não infere nomes EVT, não
altera a ISO e não habilita reprodução automática geral no gameplay. O caminho
separado do runtime prepara `goal` para os gols e `panna` para o evento authored
`TRICK`; isso não afirma vínculo original.
`audio-abk-play` reutiliza a mesma rota e toca o resultado no dispositivo WinMM
quando executado no Windows, sempre por seleção explícita.

A validação integral dos ranges candidatos é separada:

```text
dotnet run --project fifa_pc -- audio-event-candidate-validate "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
```

Para inspecionar a sequência de chunks sem decodificar `SCCl` nem alocar WAV:

```text
dotnet run --project fifa_pc -- audio-sccl-inspect "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 12
```

Para testar hipóteses de layout mono/estéreo nos ranges sem `0x82`, sem
escrever WAV ou alterar o header observado:

```text
dotnet run --project fifa_pc -- audio-sccl-channel-layout-audit "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" --report "D:\projeto zeta\fifa_pc\audio-sccl-channel-layout-audit.tsv"
```

Ela confirmou 139/283 ranges como EA-XA `SCDl` decodificáveis, com 7.633 blocos
e 11.118.229 frames. Os outros 144/283 são rejeitados porque o metadata
`SCHl` não contém o tag `0x82`, fazendo o decoder representar `channels=0`;
`SCCl` aparece como primeiro chunk em 283/283
ranges, inclusive nos 139 decodificáveis. `InspectChunkSequence` registra o
envelope sem interpretá-lo; nos primeiros casos ele tem 12 bytes e payload
variável, seguido por `SCDl`. A semântica desse payload e a origem dos canais
zero ainda não foram demonstradas. `InspectHeaderFields` expõe os tags/valores
presentes para comparação: os ranges válidos mostram `0x82/1=2`. A auditoria
separada `audio-sccl-channel-layout-audit` testou os 144 ranges sem esse tag:
144/144 validaram uma hipótese mono única e 0/144 validaram estéreo ao
decodificar todos os frames. O header observado não é reescrito e nenhuma
hipótese vira binding de EVT/ABKC.
`audio-event-candidate-decode <iso> <row> <saida.wav>` permite testar uma row
válida de `pcpadat.big` por seleção explícita e gerar WAV PCM16; o comando não
usa o nome do EVT. As rotas authored `goal` e `panna` são feitas pelo catálogo
runtime, não por este comando diagnóstico.

Esse comando lê os 16 `.abk`, 16 `.off` e 2 `.evt` diretamente do volume,
sem extrair ou escrever na ISO. Confirmou 16 pares `.off`/`.big` com faixas
dentro dos tamanhos dos companions; as faixas de dados começam em `SCHl` em
48/48 segmentos DJ e 283/283 segmentos PA. Os `.evt` compartilham o prefixo
observado e expõem runs de texto, mas ainda não há mapeamento de seus IDs para
amostras ABKC. O diagnóstico também demonstra uma relação binária reproduzível
entre `eventspa.evt` e os 283 ranges de `pcpahdr.big`: os 283 words iniciais
distintos desses ranges aparecem como words little-endian zero-extended em 314
ocorrências do EVT. Essa relação ainda não atribui semântica aos words nem
escolhe uma amostra para reprodução. O host interativo já monta e valida esses
streams no catálogo e reproduz as rows nomeadas `goal` para gols e `panna` para
o evento authored `TRICK`; os demais eventos continuam sem binding. Isso não
converte os nomes em mapeamento original.
O catálogo runtime também valida os dois `XPR0` presentes na ISO
(`saveimage.xbx` e `titleimage.xbx`), incluindo cabeçalho, dimensões e limites
do payload, e executa o decoder da textura durante a montagem. Essa validação
não confunde a textura de título usada pelo preview com uma reconstrução da
geometria EAGL.
O mesmo diagnóstico compara os companions por ordem de linha: `pcpahdr.off`
e `pcpadat.off` têm 283 ranges cada, enquanto `djhdr_en.off` e `djdat_en.off`
têm 48 cada; os ranges de dados de ambos os pares começam em `SCHl` e os de
header são legíveis. Isso é uma invariância estrutural, não uma prova de que
o índice de linha seja um ID semântico.
Uma comparação adicional lê cada header inteiro e até 64 bytes do stream
correspondente: a terceira coluna coincide em todos os pares PA e DJ, mas não
há igualdade de comprimentos, de prefixos de até 16 bytes ou de words alinhados
não nulos nessa janela. A saída é apenas evidência negativa/estrutural; ela não
atribui semântica aos campos.
O envelope neutro dos primeiros chunks confirma `SCHl`, tamanho dentro do
range, `PT` e plataforma 7 em 283/283 streams PA e 48/48 streams DJ. O parser
detalhado percorre agora todos os streams PA e DJ; foi corrigido o avanço após
um comprimento estendido já consumido, com regression test sintético.
Na comparação de words alinhados, plataforma 7 e versão 3 aparecem em 48/48
headers de cada idioma DJ e em 0/283 headers PA; houve ainda uma ocorrência de
`channels` e de `sample_rate` em `djhdr_it`. Essa é uma correlação binária
observável, não uma atribuição de nomes ou bindings aos campos. Os offsets dos
matches variam entre idiomas e nenhum offset único cobre os 48 registros DJ.
Comparar esses valores de metadados com os `.evt` encontrou apenas ocorrências
repetidas: DJ em `platform`, `version`, `channels` e um `header_size`; PA apenas
em `channels`. Não houve match de `codec`, `sample_rate` ou `declared_samples`
que selecionasse uma linha, portanto o host ainda não pode escolher amostras.
Em `eventspa.evt`, as 314 ocorrências dos 283 words iniciais distintos de
`pcpahdr` ficam entre `0x74` e `0x1E68`, antes do word de limite `0x1E70`, com
gaps de 8 a 84 bytes. Essa é uma delimitação estrutural do bloco observado,
não uma definição de records ou IDs.
Na janela relativa, `+4` bytes é zero em 314/314 ocorrências; os valores mais
comuns em `-12` e `-8` são respectivamente `1` em 277/314 e `4` em 244/314.
Isso mostra repetição local, mas ainda não prova um record fixo nem a semântica
dos words.
Dez values duplicados somam 41 ocorrências e todos aparecem em múltiplas
contextos relativos; portanto a igualdade do word não identifica um record
bruto repetido.
As 314 janelas completas são distintas, sem família de contexto repetida;
somente alguns campos relativos isolados se repetem.
A métrica neutra de espaçamento também compara `eventsdj.evt`: PA mantém 314
ocorrências entre `0x74` e `0x1E68`, gaps de 8 a 84 bytes e gap 24 em 257 casos;
DJ tem uma ocorrência por idioma, salvo duas no italiano separadas por 220
bytes. Essa diferença não basta para selecionar uma amostra.
# Startup do host Win32 Release — 2026-10-06

O executável Release iniciou `run-windowed` com a cadeia candidate opt-in e
personagens clean-room, confirmando `candidate_material_status=validated-candidate`,
`render_characters=clean-room-procedural` e dois slots candidate habilitados.
O resultado é somente `verified-windowed-startup-only`; não é evidência visual
independente nem cria binding de mesh, material, UV, skinning, rigging,
animação, morph, GPU ou áudio. `original_semantics=not-claimed`.
