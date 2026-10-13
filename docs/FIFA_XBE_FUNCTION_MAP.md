# FIFA Street 2 — mapa conservador de chamadas

O mapa foi produzido decodificando as seções executáveis como x86 32-bit e mantendo somente `call` diretos cujo destino cai em outra seção executável. Isso ainda não recupera limites perfeitos de funções.

- Entry point: `0x0024D8BE`
- Instruções decodificadas por seção: `{'.text': 860274, 'XGRPH': 3376, 'DSOUND': 12080, 'D3D': 21729, 'XPP': 10792, '.rdata': 188665, '.data': 52588, 'DOLBY': 12654}`
- Calls diretos aceitos: `44710`
- Destinos únicos: `8816`

## Alvos prioritários

| Endereço | Referências diretas |
|---:|---:|
| `0x0024D8BE` | 0 |
| `0x0024EB3A` | 6 |
| `0x00250CE2` | 4 |
| `0x0024E7CC` | 36 |
| `0x0024D373` | 5 |
| `0x0024CCA7` | 49 |

## Destinos mais referenciados

| Endereço | Referências |
|---:|---:|
| `0x00255DF0` | 1303 |
| `0x0015B660` | 925 |
| `0x00034AD0` | 917 |
| `0x0015B470` | 897 |
| `0x0026E7E5` | 765 |
| `0x001D7126` | 757 |
| `0x0015F7F0` | 553 |
| `0x000F59B0` | 378 |
| `0x0026E278` | 369 |
| `0x0002E780` | 360 |
| `0x0026E895` | 291 |
| `0x0025DCB0` | 280 |
| `0x001D70A2` | 277 |
| `0x001DBA07` | 276 |
| `0x00031760` | 271 |
| `0x001752E0` | 263 |
| `0x0015F7B0` | 205 |
| `0x0026ECC6` | 203 |
| `0x00255BB0` | 189 |
| `0x00255EE0` | 185 |
| `0x000320A0` | 173 |
| `0x001D4CF6` | 167 |
| `0x001D9DC0` | 166 |
| `0x000E5E90` | 163 |
| `0x00257A00` | 151 |
| `0x0025DBD0` | 141 |
| `0x0025DC50` | 137 |
| `0x00011110` | 136 |
| `0x0010DA80` | 134 |
| `0x000FE740` | 131 |
| `0x0025C0E0` | 121 |
| `0x0025C100` | 121 |
| `0x001D7843` | 120 |
| `0x00205A9A` | 120 |
| `0x0010EA40` | 114 |
| `0x001D4EF5` | 109 |
| `0x000113B0` | 108 |
| `0x0009CEA0` | 107 |
| `0x00223CDF` | 106 |
| `0x0015DA20` | 106 |
| `0x00237AB4` | 105 |
| `0x00237AC2` | 104 |
| `0x0024BC9A` | 100 |
| `0x0024BD78` | 100 |
| `0x0024BA3A` | 100 |
| `0x00068CE0` | 98 |
| `0x0020C86A` | 98 |
| `0x0026F120` | 97 |
| `0x002937B0` | 94 |
| `0x0015D860` | 93 |
| `0x00205F76` | 93 |
| `0x001D78C3` | 92 |
| `0x001A0530` | 91 |
| `0x001D7A23` | 91 |
| `0x002713F8` | 90 |
| `0x001D486C` | 90 |
| `0x0002E800` | 83 |
| `0x0014F890` | 82 |
| `0x0025DE30` | 81 |
| `0x0026F0B5` | 81 |
| `0x00205DE9` | 81 |
| `0x0027064D` | 80 |
| `0x00223D61` | 79 |
| `0x000555F0` | 79 |
| `0x00223D70` | 78 |
| `0x00219A37` | 77 |
| `0x000C0B10` | 75 |
| `0x002626F0` | 75 |
| `0x0026EA22` | 73 |
| `0x0028D7C0` | 72 |
| `0x0025DDD0` | 70 |
| `0x00114F40` | 69 |
| `0x00068F40` | 68 |
| `0x0003C570` | 67 |
| `0x00223B9C` | 65 |
| `0x0026E8A0` | 65 |
| `0x000DFA90` | 65 |
| `0x00198040` | 63 |
| `0x0003DC40` | 62 |
| `0x0025C690` | 62 |

Próximo refinamento: confirmar limites por fluxo básico e cruzar, como
referências ainda candidatas, o relatório `fifa_pc/xbe-string-xref-audit.tsv`
com strings/formatos de asset. Esse cruzamento não autoriza binding sem
evidência independente.
