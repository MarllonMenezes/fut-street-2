# FIFA Street 2 — índices dos arquivos BIG

Parser somente leitura. Foram lidos os headers e índices internos dos
arquivos `.big`; nenhum conteúdo foi extraído ou regravado.

Imagem: `D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso`

| Arquivo | Magic | Tamanho | Entradas | Índice | Status |
|---|---|---:|---:|---:|---|
| `data/anims.big` | `BIG4` | 5.533.040 | 58 | 1215 | `OK` |
| `data/aptanims/aptfnts.big` | `BIGF` | 73.208 | 3 | 80 | `OK` |
| `data/aptanims/feart.big` | `BIG4` | 10.697.696 | 143 | 2936 | `OK` |
| `data/aptanims/splines.big` | `BIGF` | 5.907 | 10 | 209 | `OK` |
| `data/audio/djdat_bo.big` | `SCHl` | 68.484.864 | — | — | `unsupported magic` |
| `data/audio/djdat_en.big` | `SCHl` | 93.049.344 | — | — | `unsupported magic` |
| `data/audio/djdat_fr.big` | `SCHl` | 93.845.760 | — | — | `unsupported magic` |
| `data/audio/djdat_ge.big` | `SCHl` | 67.302.912 | — | — | `unsupported magic` |
| `data/audio/djdat_it.big` | `SCHl` | 50.037.760 | — | — | `unsupported magic` |
| `data/audio/djdat_rr.big` | `SCHl` | 68.532.736 | — | — | `unsupported magic` |
| `data/audio/djdat_sp.big` | `SCHl` | 85.337.856 | — | — | `unsupported magic` |
| `data/audio/djhdr_bo.big` | `0x0d00ffff` | 2.856 | — | — | `unsupported magic` |
| `data/audio/djhdr_en.big` | `0x2200ffff` | 3.432 | — | — | `unsupported magic` |
| `data/audio/djhdr_fr.big` | `0x2800ffff` | 3.868 | — | — | `unsupported magic` |
| `data/audio/djhdr_ge.big` | `0x2e00ffff` | 3.184 | — | — | `unsupported magic` |
| `data/audio/djhdr_it.big` | `0x3400ffff` | 3.080 | — | — | `unsupported magic` |
| `data/audio/djhdr_rr.big` | `0x1700ffff` | 3.368 | — | — | `unsupported magic` |
| `data/audio/djhdr_sp.big` | `0x3a00ffff` | 3.364 | — | — | `unsupported magic` |
| `data/audio/loadloop.big` | `BIGF` | 10.099.728 | 58 | 1584 | `OK` |
| `data/audio/pcpadat.big` | `SCHl` | 601.688.832 | — | — | `unsupported magic` |
| `data/audio/pcpahdr.big` | `0x9520ffff` | 35.572 | — | — | `unsupported magic` |
| `data/audio/tuna.big` | `BIGF` | 6.313 | 1 | 45 | `OK` |
| `data/database/db.big` | `BIGF` | 78.063 | 29 | 739 | `OK` |
| `data/datafile.big` | `BIG4` | 859.923 | 82 | 1902 | `OK` |
| `data/hud/feman.big` | `BIG4` | 1.072 | 4 | 105 | `OK` |
| `data/hud/fonts.big` | `BIG4` | 155.703 | 9 | 213 | `OK` |
| `data/hud/ingame.big` | `BIG4` | 1.017.712 | 16 | 358 | `OK` |
| `data/hud/widescrn.big` | `BIG4` | 174.117 | 9 | 213 | `OK` |
| `data/initprgz.big` | `BIGF` | 1.970 | 1 | 46 | `OK` |
| `data/initstaz.big` | `BIGF` | 37.198 | 1 | 45 | `OK` |
| `data/loadart.big` | `BIG4` | 143.376 | 4 | 103 | `OK` |
| `data/locale.big` | `BIGF` | 1.488.824 | 35 | 756 | `OK` |
| `data/models.big` | `BIGF` | 42.399.499 | 1115 | 23439 | `OK` |
| `data/preloadz.big` | `BIGF` | 4.975.248 | 1 | 44 | `OK` |
| `data/stadium/amsterda.big` | `BIGF` | 2.067.029 | 1 | 44 | `OK` |
| `data/stadium/barra.big` | `BIGF` | 1.972.211 | 1 | 44 | `OK` |
| `data/stadium/berlin.big` | `BIGF` | 2.558.212 | 1 | 44 | `OK` |
| `data/stadium/brazil.big` | `BIGF` | 2.417.299 | 1 | 44 | `OK` |
| `data/stadium/cameroon.big` | `BIGF` | 2.311.631 | 1 | 44 | `OK` |
| `data/stadium/capitch.big` | `BIGF` | 11.510.299 | 350 | 10201 | `OK` |
| `data/stadium/marseill.big` | `BIGF` | 2.064.852 | 1 | 44 | `OK` |
| `data/stadium/mexico.big` | `BIGF` | 2.349.153 | 1 | 44 | `OK` |
| `data/stadium/NewYork.big` | `BIGF` | 2.210.041 | 1 | 44 | `OK` |
| `data/stadium/roma.big` | `BIGF` | 2.961.742 | 1 | 44 | `OK` |
| `data/stadium/westway.big` | `BIGF` | 2.521.157 | 1 | 44 | `OK` |

## Amostra: `data/anims.big`

| Nome | Offset | Tamanho |
|---|---:|---:|
| `banklist.axt` | 1216 | 47 |
| `def_bank.axt` | 1280 | 84348 |
| `def_bank.ord` | 85632 | 3593808 |
| `def_bank.orl` | 3679488 | 233648 |
| `defbank2.axt` | 3913152 | 10757 |
| `defbank2.ord` | 3923968 | 474672 |
| `defbank2.orl` | 4398656 | 31040 |
| `close_up.axt` | 4429696 | 2143 |
| `close_up.ord` | 4431872 | 85616 |
| `close_up.orl` | 4517504 | 5024 |
| `goalbank.axt` | 4522560 | 1264 |
| `goalbank.ord` | 4523840 | 145120 |

A estrutura BIG4/BIGF e seus campos endian mistos foram tratados como índice; codecs e formatos dos arquivos internos continuam separados.
Referência pública de layout: https://github.com/OpenSAGE/Docs/blob/master/file-formats/big/index.rst
