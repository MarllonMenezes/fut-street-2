# FIFA Street 2 — etapa 1.28: declaração de material e envelope VS

Data: 2026-10-13, America/Sao_Paulo. Decoder novo em .NET, somente leitura
de assets; análise estática do XBE, sem executá-lo, recompilá-lo ou simular GPU.

## Resultado funcional delimitado

A declaração referenciada por `__RM:::ParentRM_FifastCharacterSkin` em
`data/initstaz.big!initstay.big!mat.o` deixou de ser apenas uma lista de words:
o consumidor estático permite decodificar os cinco pares stream/registro,
seus formatos brutos, offsets dentro do elemento e larguras em bytes.
O bloco tem 44 bytes, incluindo o terminador `0xFFFFFFFF`.

| Stream | Registro | Formato bruto | Offset no elemento | Largura |
| ---: | ---: | --- | ---: | ---: |
| 0 | 0 | `0x32` | 0 | 12 bytes |
| 1 | 1 | `0x22` | 0 | 8 bytes |
| 2 | 2 | `0x40` | 0 | 4 bytes |
| 3 | 3 | `0x32` | 0 | 12 bytes |
| 4 | 4 | `0x15` | 0 | 2 bytes |

O VS nomeado possui header little-endian com versão `0x2078` e count `46`.
O consumidor lê esse count e usa 16 bytes por instrução: 736 bytes de corpo,
740 bytes com header. O size `4` declarado pelo símbolo ELF não delimita o
programa. Não foram decodificados opcodes, operações ou semântica de entradas.

Isso **não** identifica posição, UV, normal, cor, pesos ou índices de rig.
Não prova herança do parent no modelo, seleção do buffer dinâmico, modo de
primitiva, sampling/material ou resultado visual. O renderer não recebeu
um binding especulativo. Readiness global continua `NOT_READY`.

## Evidência nos dados originais

Fonte: 38.970 bytes, SHA-256
`75DBC11557C6F3E4A32D6D88372476398B3A4C435E3907C2902E38579FB29430`.
Seção ELF 1:

O header do container observado tem `e_type=1`, `e_machine=8`. Esses campos
são metadados do asset EAGL; não foi executado código desse container nem
inferido um alvo de CPU para a reimplementação.

- Parent em `0x1458`; `+4` contém count `1`.
- `Parent + 8` possui uma única relocation local tipo 2 e word `0x1400`:
  base da declaração decodificada.
- `Parent + 0x0C` possui relocation local para `0x1450`; o word zero nessa
  tabela tem relocation nomeada para `FifastCharacterSkin0__EAGLMicroCodeVS`.
- Esse símbolo está em `0x1490`; envelope de 740 bytes sem relocations.

SHA-256 da declaração: `2205EC6F6DEB9CB77F0A6B23755DBB2C98C833355CABF8B4C61F5C8DD8468CBE`.
SHA-256 do envelope VS: `B37951FBB3188E7843A9492EF675B4CBEA9EBA75CAA412942746BF1212CDFB67`.

## Evidência no consumidor estático

XBE: 3.293.184 bytes, SHA-256
`5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B`.
O controle valida quatro trechos delimitados e 51 endereços de instrução:

1. `0x001970E0..0x00197195`: preparação dos arrays `+0x10` e `+0x18`;
   consumidor recebe declaração de `+8` e programa da tabela `+0x0C`.
2. `0x001A3B30..0x001A3B4B`: wrapper transmite os dois argumentos à
   criação em `0x00292080`, com flags zero e armazenamento de saída.
3. `0x00291DE0..0x00291F79`: parser de tokens. Máscara `0xE0000000`
   separa famílias; stream usa `0xF`, registro usa `0x1F`, formato é o byte
   em bits 16..23. Selecionar stream reinicia o offset; ler registro grava
   stream/offset/formato e incrementa o offset pela largura calculada.
4. `0x00292080..0x002921BE`: lê os dois words do header VS, reconhece
   `0x2078`, chama o parser de declaração e prepara o corpo do programa.

As duas tabelas de largura em `0x0029C760` são conferidas nos bytes do XBE:

```text
01 02 04 00 01 02 04 00
00 01 02 03 04 00 00 03
```

Largura = tabela1[formato & 15] × tabela2[formato >> 4], conforme os loads
e a multiplicação observados. SHA-256 dos 16 bytes:
`2F6CF9E3BD99EB8EEB79080DD0867A4E0A68D14B7A3220A07BA704F8882FBB3D`.
O decoder aceita somente os quatro formatos e famílias comprovados neste
recorte; skip, constantes, tesselação e outros tokens falham fechados.

## Implementação e controles

- `fifa_pc/Runtime/EaglMaterialDeclarationAudit.cs`: extração BIG/RefPack
  limitada, símbolos únicos definidos, relocations locais/nomeadas exatas,
  limites e alinhamento de seção, declaração e envelope VS.
- CLI: `eagl-material-declaration-audit <iso> --report <tsv> --summary <tsv>`.
- `tools/analysis/eagl_material_declaration_control.py`: releitura fresca
  da ISO, decode Python independente usando as tabelas originais, comparação
  exata de todas as colunas e validação das instruções fixadas.
- Sete controles negativos em cópias na memória: stream, registro, largura,
  comprimento do programa, promoção de herança, identidade da fonte e
  remoção da chamada ao consumidor.
- Self-test novo: declaração real sintética, layout interleaved, truncamento,
  ausência de stream/terminador, duplicação de registro/símbolo, formatos e
  tokens não suportados, índices/alinhamento/overflow de endereço, referências
  externas, relocations dentro do payload, envelope excessivo/truncado e
  readiness ausente/stale ou indevidamente promovido.
- Readiness limitado: `rendering-material-declaration-and-vs-envelope`,
  `verified-static-layout`, sem relaxar qualquer gate principal.

Relatórios do smoke focal (exit 0):

| Arquivo | SHA-256 |
| --- | --- |
| `fifa_pc/eagl-material-declaration-audit.tsv` | `D439A519D72E2F9718EAD9A312CD8C3997C3A60A43EC9398014F966F79488F6B` |
| `fifa_pc/eagl-material-declaration-control.tsv` | `70AA5EDE3857520172410B6260EC7671095FAA565027593BAD2882236B1BF286` |

Validação focal: Release 0 warnings/0 errors, 100/100 self-tests, decoder
real e controle independente exit 0. A matriz completa está registrada em
`fifa_pc/stage-1.28-matrix-2026-10-13.log` preserva a primeira tentativa,
que falhou no smoke novo após uma checagem extra equivocada de `e_machine=3`.
A releitura do header comprovou `8`; a guarda e a fixture foram corrigidas
para o valor observado e foi adicionado um teste que rejeita `3`.
A repetição completa é `fifa_pc/stage-1.28-matrix-retry-2026-10-13.log`;
seu encerramento será documentado apenas após observar o resultado final e
a verificação SHA-256 da ISO.

Encerramento observado da repetição: exit `0`,
`matrix_status: completed-authored-and-candidate-opt-in`, Release 0 warnings/
0 errors, 100/100 self-tests, smokes relevantes, headless/windowed e replays
determinísticos aprovados. Cobertura authored: 10 cenários, 777 frames,
54 eventos de áudio, 1.554 amostras de animação. O subgate delimitado passou;
readiness global permanece `NOT_READY`. ISO: 2013003776 bytes, Archive,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Próxima investigação recomendada

Provar a transformação/resolução do render method serializado e a herança
do parent antes de associar a declaração a todos os draws de modelos.
Pista estática preliminar: `0x001971E0` lê um ponteiro runtime em `+0x2C`,
testa `+0x24` e copia `+0x10/+0x18`, preparando o parent com `0x001970E0`
se necessário. `0x00197290` instala esse ponteiro em `+0x2C`.
Esses trechos ainda não estão no controle desta etapa: não igualar esse
campo ao `+0x1C` serializado que contém a referência externa no modelo.

Em seguida, decodificar as 46 instruções por especificação pública e
controle independente para provar consumo de entradas e saídas, sem
interpretador/emulador. Manter separado o seletor `0x00345BCC` e as cópias
dinâmicas dos buffers. Os gates de áudio e deformação continuam exigindo
seus próprios consumidores/contratos, não dedução pelo número de subgates.

Referência primária localizada para a próxima etapa:
[envytools, encoding XF/Kelvin](https://envytools.readthedocs.io/en/latest/hw/graph/xf/isa.html).
Ela documenta campos/opcodes, mas distingue ISAs e espaços de armazenamento.
Uma sondagem bitwise somente leitura do corpo original observou word 0 zero
nas 46 instruções e bit final apenas no índice 45. Isso é compatibilidade
preliminar do envelope, não decoder de operandos validado. Não contar todo
mux de entrada como leitura efetiva: operandos inativos precisam ser
filtrados pelo opcode antes de construir dependências de registros.
