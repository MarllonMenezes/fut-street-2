# FIFA Street 2 — mapa inicial do entry point

Este artefato é uma triagem estática somente leitura. Ele não substitui
um disassembler completo: chamadas relativas e prólogos são candidatos
e precisam ser confirmados por decodificação de instruções e fluxo de controle.

- Executável: `default.xbe` dentro de `D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso`
- Entry point retail: `0x0024D8BE`
- Offset no XBE: `0x0023D8BE`
- Seção: `.text`

## Janela de bytes

Virtual: `0x0024D83E`–`0x0024D93E`
Arquivo: `0x0023D83E`–`0x0023D93E`

```text
e8 64 f4 ff ff 33 c0 5e c9 c2 04 00 e8 5d 37 00 00 e8 04 35 00 00 64 a1 20 00 00 00 8b 80 50 02 00 00 85 c0 74 05 8b 48 24 eb 02 33 c9 85 c9 74 27 57 64 a1 28 00 00 00 64 8b 3d 04 00 00 00 8b 15 fc 51 35 00 8b 14 97 2b 50 28 c6 01 01 81 c2 08 00 00 00 89 51 04 5f e8 84 36 00 00 e8 27 36 00 00 6a 00 6a 00 6a 00 e8 75 8c df ff 83 c4 0c 6a 00 6a 01 6a 01 e8 29 34 00 00 33 c0 c2 04 00 8b 0d 18 01 01 00 a1 08 01 01 00 2b c1 05 00 00 01 00 3b 01 73 02 89 01 a1 6c 07 30 00 2b 05 5c 07 30 00 8b 0d 60 07 30 00 56 8d 44 08 0f 57 83 e0 f0 6a fc 59 83 c0 04 a3 50 13 36 00 99 f7 f9 8b 0d 64 07 30 00 33 f6 56 56 56 68 4a d8 24 00 56 56 89 01 e8 23 12 00 00 8b f8 3b fe 75 0a 56 6a 01 6a 01 e8 bb 33 00 00 57 e8 9f 0e 00 00 5f 5e c3 55 8b ec 51 51 8b 45 08 89 45 fc 8d 48 01
```
## Transferências relativas próximas ao entry point

| Tipo | Origem | Destino | Offset |
|---|---:|---:|---:|
| call | `0x0024D83E` | `0x0024CCA7` | `0x0023D83E` |
| call | `0x0024D84A` | `0x00250FAC` | `0x0023D84A` |
| call | `0x0024D84F` | `0x00250D58` | `0x0023D84F` |
| call | `0x0024D896` | `0x00250F1F` | `0x0023D896` |
| call | `0x0024D89B` | `0x00250EC7` | `0x0023D89B` |
| call | `0x0024D8A6` | `0x00046520` | `0x0023D8A6` |
| call | `0x0024D8B4` | `0x00250CE2` | `0x0023D8B4` |
| call | `0x0024D912` | `0x0024EB3A` | `0x0023D912` |
| call | `0x0024D922` | `0x00250CE2` | `0x0023D922` |
| call | `0x0024D928` | `0x0024E7CC` | `0x0023D928` |

## Triagem da seção `.text`

Foram encontrados 47365 candidatos `call/jmp rel32` na seção do entry point, apontando para 9812 destinos únicos dentro de seções executáveis.

## Próxima etapa

Confirmar esses candidatos com um disassembler x86 e construir o primeiro mapa de funções: startup, inicialização de arquivos, loop principal e chamadas para XAPILIB/D3D/DSOUND.
Os prólogos abaixo são apenas indícios e não foram usados como prova de limites de função.

Total de candidatos de prólogo: 3280.
