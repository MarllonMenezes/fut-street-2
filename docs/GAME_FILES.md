# Inventário inicial dos arquivos dos jogos

Data da análise: 2026-10-04  
Escopo: inspeção somente leitura dos arquivos existentes em `D:\projeto zeta`.

Os ISOs originais não foram alterados, extraídos por sobrescrita ou copiados para o projeto.

## 1. Entradas encontradas

### FIFA Street 2

| Campo | Valor |
|---|---|
| Caminho | `D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso` |
| Tipo | imagem de disco |
| Tamanho | 2.013.003.776 bytes |
| Magic no início do arquivo | zero-filled; não é um cabeçalho útil no offset zero |
| PVD ISO9660 | `CD001` no setor 16 (`0x8000`) |
| Identificador Xbox | `MICROSOFT*XBOX*MEDIA` em `0x10000` |
| Executável candidato | `default.xbe` encontrado no offset 540.706 |
| Cabeçalho executável | `XBEH` no offset `0x84800` (542.720) |

Conclusão comprovada: esta imagem é de Xbox original, não uma imagem Xbox 360. O executável encontrado é um XBE, portanto a análise deverá considerar a arquitetura x86 do Xbox original e suas APIs, separadamente do jogo de Xbox 360.

O detalhamento do executável, suas seções, bibliotecas e imports está em
`docs/FIFA_XBE_ANALYSIS.md`.

### Star Wars: The Force Unleashed

| Campo | Valor |
|---|---|
| Caminho do ISO | `D:\projeto zeta\Star Wars - The Force Unleashed (World) (En,Fr,De,Es,It)\Star Wars - The Force Unleashed (World) (En,Fr,De,Es,It).iso` |
| Tipo | imagem de disco Xbox 360 |
| Tamanho | 7.835.492.352 bytes |
| PVD ISO9660 | `CD001` no setor 16 (`0x8000`) |
| Volume ID observado | `XGD2DVD_NTSC` |
| Identificador Xbox 360 | `XGD2DVD` em `0x10000` |
| Executável candidato | `default.xex` encontrado no offset 1.005.660.194 |
| Cabeçalho XEX2 candidato | `XEX2` no offset `0x51F44000` (1.374.961.664) |
| Bytes iniciais do candidato | `58 45 58 32 00 00 00 01 00 00 40 00 ...` |

Conclusão comprovada: esta imagem contém uma área de dados Xbox 360 XGD2 e referências a um executável XEX. O offset é um offset bruto do ISO; ainda não foi tratado como offset de arquivo lógico até a enumeração do filesystem Xbox 360.

## 2. Árvore dos arquivos disponíveis

```text
D:\projeto zeta\
├── FIFA Street 2 (USA) (En,Es).iso
└── Star Wars - The Force Unleashed (World) (En,Fr,De,Es,It)\
    ├── AUDIO_TS\
    ├── VIDEO_TS\
    │   ├── VIDEO_TS.BUP
    │   ├── VIDEO_TS.IFO
    │   ├── VIDEO_TS.VOB
    │   ├── VTS_01_0.BUP
    │   ├── VTS_01_0.IFO
    │   ├── VTS_01_0.VOB
    │   └── VTS_01_1.VOB
    └── Star Wars - The Force Unleashed (World) (En,Fr,De,Es,It).iso
```

O conteúdo `VIDEO_TS` é um conjunto DVD-Video externo ao ISO do jogo. Foram confirmados:

- `VIDEO_TS.IFO`: magic `DVDVIDEO-VMG`;
- arquivos `.VOB`: fluxo MPEG-PS com início `00 00 01 BA`;
- arquivos `.BUP`: cópias/backup de estruturas IFO, conforme a nomenclatura DVD-Video.

Esses arquivos externos não foram associados ao conteúdo do jogo sem evidência adicional.

## 3. Inventário interno das imagens

O leitor local `tools/analysis/list_xdvdfs.py` enumerou o filesystem XDVDFS sem
extrair ou reescrever os ISOs.

| Imagem | Entradas | Diretórios | Arquivos | Dados dos arquivos |
|---|---:|---:|---:|---:|
| FIFA Street 2 | 130 | 7 | 123 | 2.012.286.112 bytes |
| The Force Unleashed | 1.391 | 15 | 1.376 | 6.789.176.862 bytes |

### Diretórios principais

```text
FIFA Street 2
├── data/
│   ├── aptanims/
│   ├── audio/
│   ├── database/
│   ├── hud/
│   ├── movies/
│   └── stadium/
└── default.xbe

The Force Unleashed
├── $SystemUpdate/
├── ExternalPacks/
├── FMV/
├── LevelPacks/
├── Scum/
├── Shared/
└── default.xex
```

O arquivo `default.xbe` ocupa 3.293.184 bytes e começa com `XBEH` no offset
bruto `0x84800`. O arquivo `default.xex` ocupa 17.154.048 bytes e começa com
`XEX2` no offset bruto `0x51F44000`, que corresponde ao setor 541.544 da
partição XDVDFS Xbox 360.

## 4. Extensões observadas

| Extensão | Quantidade observada | Observação |
|---|---:|---|
| `.iso` | 2 | imagens de disco; uma do FIFA e uma dentro da pasta fornecida para The Force Unleashed |
| `.big` | 45 | FIFA; arquivos com assinatura `BIGF` em amostras |
| `.abk` | 16 | FIFA; amostra com assinatura `ABKC` |
| `.off` | 16 | FIFA; tabelas associadas a dados de áudio |
| `.bh` | 15 | FIFA; amostra com assinatura `VivFP` |
| `.vp6` | 11 | FIFA; vídeos/streams; amostra começa com `SCHl` |
| `.ast` | 6 | FIFA; streams; amostra começa com `SCHl` |
| `.xsh` | 3 | FIFA; amostra com assinatura `SHPX` |
| `.viv` | 2 | FIFA; amostra começa com `BIGF` |
| `.evt` | 2 | FIFA; eventos/áudio por nomenclatura, ainda não confirmado |
| `.xbx` | 2 | FIFA; amostra `saveimage.xbx` começa com `XPR0` |
| `.csi` | 1 | FIFA; `FIFAST2.csi`, candidato estrutural `MOIR` de índice de nomes; seleção e finalidade ainda não confirmadas |
| `.mpf` | 1 | FIFA; `pathsong.mpf`, finalidade ainda não confirmada |
| `.mus` | 1 | FIFA; `prime.mus`, finalidade ainda não confirmada |
| `.pmb` | 1 | FIFA; `patterns.pmb`, finalidade ainda não confirmada |
| `.xbe` | 1 | FIFA; executável principal |
| `.xd` | 572 | The Force Unleashed; pacotes binários; amostra começa com `7F 3A DE 7A` |
| `.xp` | 572 | The Force Unleashed; pacotes binários; amostra começa com `7F 3A DE 7A` em uma variante |
| `.lp` | 107 | The Force Unleashed; pacotes por nível/asset; variante amostrada começa com `0F F5 12 ED` |
| `.wmv` | 45 | The Force Unleashed; vídeo Windows Media, amostra começa com `30 26 B2 75` |
| `.csv` | 42 | The Force Unleashed; conteúdo interno ainda não analisado |
| `.z` | 34 | The Force Unleashed; arquivos XML comprimidos; conteúdo ainda não descomprimido |
| `.xex` | 1 | The Force Unleashed; executável principal |
| `.txt` | 1 | The Force Unleashed; `ExternalPacks/List.txt` |
| `.dds` | 1 | The Force Unleashed; `FailureAsset.dds`, magic `DDS ` |
| `.BUP` | 2 | DVD-Video externo |
| `.IFO` | 2 | DVD-Video externo |
| `.VOB` | 4 | DVD-Video externo |

Os números acima são contagens do filesystem interno, exceto os arquivos
DVD-Video, que ficam fora das imagens de jogo na pasta fornecida.

## 5. Archives e containers encontrados

### Confirmados

- ISO/ISO9660: ambos possuem o identificador `CD001` no setor 16.
- Container de mídia Xbox original: `MICROSOFT*XBOX*MEDIA` no ISO de FIFA Street 2.
- Container de mídia Xbox 360 XGD2: `XGD2DVD_NTSC`/`XGD2DVD` no ISO de The Force Unleashed.
- DVD-Video externo: diretório `VIDEO_TS` com IFO/BUP/VOB.
- Arquivos `.big`/`.viv` do FIFA com assinaturas observadas `BIGF`; a família
  do container é identificada, mas o índice interno ainda não foi decodificado.
- Pacotes `.lp`, `.xp` e `.xd` do The Force Unleashed; as assinaturas foram
  registradas, mas seus campos ainda são `UNKNOWN`.

### Ainda não confirmados

- archives específicos do jogo;
- compressão dos assets;
- formato dos níveis, modelos, texturas, áudio ou scripts;
- arquivos adicionais de executáveis ou DLLs dentro das imagens.

## 6. Formatos identificados por evidência

| Evidência | Interpretação limitada |
|---|---|
| `CD001` | descritor ISO9660 presente; não implica que o conteúdo principal seja ISO9660 convencional |
| `MICROSOFT*XBOX*MEDIA` | mídia Xbox original |
| `XBEH` | cabeçalho de executável XBE localizado no ISO de FIFA Street 2 |
| `XGD2DVD_NTSC` / `XGD2DVD` | imagem Xbox 360 XGD2 |
| `XEX2` | cabeçalho/assinatura XEX2 candidato no ISO de The Force Unleashed |
| `DVDVIDEO-VMG` | estrutura DVD-Video no `VIDEO_TS.IFO` externo |
| `00 00 01 BA` | início de pack MPEG-PS em VOB |
| `BIGF` | assinatura observada em `data/models.big` e `data/aptanims/fe.viv` |
| `VivFP` | assinatura observada em `data/aptanims/fe.bh` |
| `ABKC` | assinatura observada em `data/audio/AMBSTRM.abk` |
| `SHPX` | assinatura observada em `data/aptanims/player0.xsh` |
| `XPR0` | assinatura observada em `data/saveimage.xbx` |
| `7F 3A DE 7A` | assinatura observada em amostras `.xp` e `.xd` |
| `DDS ` | cabeçalho DDS observado em `Shared/ShaderNetworks/DefaultTextures/FailureAsset.dds` |
| `30 26 B2 75` | cabeçalho ASF/Windows Media observado em um `.wmv` |

## 7. Formatos desconhecidos

No estado atual, os seguintes itens permanecem `UNKNOWN`:

- campos internos de `.big`, `.bh`, `.viv`, `.abk`, `.ast`, `.xsh`, `.xbx`,
  `.lp`, `.xp` e `.xd`;
- algoritmo e parâmetros de compressão dos arquivos `.z`;
- schemas/índices internos dos pacotes de nível;
- shaders, modelos, texturas, animações, física, scripts e áudio do jogo;
- bibliotecas de middleware;
- significado das ocorrências adicionais de `XEX2` no espaço bruto do ISO;
- qualquer arquivo não reconhecido pelos headers testados.

As ocorrências adicionais não foram classificadas como executáveis: uma assinatura textual isolada em dados binários não é evidência suficiente.

## 8. Middleware

Há evidência nominal de tecnologias de física/animação no conteúdo do The
Force Unleashed:

| Evidência | Grau | Interpretação segura |
|---|---|---|
| `Shared/Assets/DefSchemas/Dmm.schema.xml.z` | forte para dados | existe um schema comprimido com nome `Dmm`; não prova que a biblioteca DMM esteja presente |
| `Shared/Assets/DefSchemas/DmmAffector.schema.xml.z` | forte para dados | existe um schema relacionado a affectors DMM |
| `Shared/Assets/DefSchemas/Euphoria.schema.xml.z` | forte para dados | existe um schema relacionado a Euphoria |
| `Shared/Assets/DefSchemas/Physics.schema.xml.z` | forte para dados | existe um schema de física genérico |
| `Scum/Assets/DefSchemas/LuaScripts.schema.xml.z` | forte para dados | existe um schema relacionado a scripts Lua |
| nomes `LevelPacks/*_x360.lp` | forte para conteúdo | pacotes específicos da plataforma Xbox 360 |

Nenhuma DLL ou assinatura de Havok foi encontrada na listagem por nome. Isso
não permite concluir ausência: a implementação pode estar no XEX ou dentro de
pacotes binários. Para confirmar middleware será necessário analisar o XEX e
descomprimir/parsear os headers dos pacotes.

## 9. Indícios relevantes para o port

1. Os jogos não compartilham a mesma plataforma: FIFA Street 2 é Xbox original; The Force Unleashed é Xbox 360.
2. O FIFA fornece um candidato XBE (`default.xbe`/`XBEH`), enquanto The Force Unleashed fornece um candidato XEX (`default.xex`/`XEX2`).
3. O executável Xbox 360 não pode ser tratado como código x86-64: a próxima análise deverá considerar Xenon PowerPC, big-endian, VMX e APIs Xbox 360.
4. A pasta de Star Wars contém um ISO Xbox 360 e arquivos DVD-Video externos; esses conjuntos devem permanecer separados até haver prova de dependência.

## 10. Cinco maiores obstáculos previstos

1. Duas arquiteturas e dois ambientes de sistema incompatíveis entre si.
2. Enumeração e leitura dos filesystems proprietários antes de acessar os assets reais.
3. Ausência de código-fonte, símbolos e documentação proprietária dos executáveis.
4. Substituição de APIs gráficas, áudio, entrada, filesystem, threading e serviços de plataforma.
5. Conversão correta de estruturas, endianess, SIMD/VMX, compressão e formatos de assets sem confundir assinaturas com formatos.

## 11. Próximo passo recomendado

Usar a listagem criada e adicionar um leitor de headers de XBE/XEX, ainda em
modo somente leitura. Em seguida:

1. listar os caminhos internos;
2. localizar e extrair analiticamente apenas os headers dos executáveis para memória de trabalho;
3. registrar sections, imports, entry point e formatos dos arquivos de dados;
4. só então decidir se cada jogo admite recompilação, tradução parcial ou uma reimplementação de engine.

Não há evidência suficiente para iniciar uma compilação nativa dos jogos neste momento.
