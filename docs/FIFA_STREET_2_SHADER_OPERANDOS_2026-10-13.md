# Etapa 1.30 — operandos estáticos do vertex program

Decoder novo .NET `EaglVertexProgramAudit.cs`, comando
`eagl-vertex-program-audit <iso> --report <tsv> --summary <tsv>`.
Fonte: `data/initstaz.big!initstay.big!mat.o`, 38.970 bytes,
SHA-256 `75DBC11557C6F3E4A32D6D88372476398B3A4C435E3907C2902E38579FB29430`.
Programa: `FifastCharacterSkin0__EAGLMicroCodeVS`, seção 1, offset 0x1490,
740 bytes, SHA-256
`B37951FBB3188E7843A9492EF675B4CBEA9EBA75CAA412942746BF1212CDFB67`.

## Contrato e resultado

Leitura bounded do header 0x2078 e de quatro DWORDs little-endian por
instrução. Limite do subset: 136 instruções; word 0 e bits reservados altos
de word 1 devem ser zero; FINAL somente na última instrução. Não há
interpretação de ponto flutuante, emulação, execução ou recompilação do XBE.
Relocations, identidade da fonte e envelope continuam verificados pelo
decoder de material existente; a extração foi compartilhada sem alterar
seu relatório.

| Medida real | Resultado |
| --- | --- |
| Instruções reconciliadas | 46/46 |
| Registros de entrada em operandos ativos | 0, 1, 3, 4 |
| Referências de slots ativos a entradas | 8 |
| Instruções com constante relativa | 6 |
| Instruções MAC/ILU pareadas | 6 |
| Instruções com máscara de saída não zero | 9 |
| Endereços de saída no banco de output | 0, 3, 4, 9 |

Atividade significa seleção de slots pelo opcode: MOV/ARL usam A, ADD
usa A/C, MAD usa A/B/C, demais operações binárias usam A/B, ILU não-NOP
usa C. Um slot compartilhado entre MAC e ILU é contado uma única vez.
Não é análise de liveness por componente, resultado numérico, propagação
transitiva de dados ou prova de destino temporário efetivo de instrução
pareada. O campo de temporário compartilhado é explicitamente raw.

Discriminação útil: a instrução 1 tem ILU MOV, C=`v[1].xyzw`, output bank=1,
address=9, mask=15, unidade ILU. As instruções 2–4 usam `v[0]` e constantes
relativas 96–98; 5/7/8 usam `v[3]` com os mesmos índices-base. A instrução 0
usa `v[4].xxxx` no ARL. O registro 2 da declaração não é referenciado por
operandos ativos neste programa. Isso não associa todos os streams de modelo
a este programa nem declara slot 1 globalmente UV: parent serializado,
instalação efetiva, buffers selecionados, constantes e formatos convertidos
ainda precisam de evidência.

## Fontes públicas e controle independente

- [envytools, campos Kelvin](https://envytools.readthedocs.io/en/latest/hw/graph/xf/isa.html): bit positions em palavra lógica.
- [XboxDev, armazenamento NV2A](https://xboxdevwiki.net/NV2A/Vertex_Shader): quatro DWORDs e campos/opcodes.
- [nxdk nv20_shader.h, commit fixado](https://github.com/XboxDev/nxdk/blob/7e8d4fc3d00ca59e88298e912438ac215cf93392/lib/pbkit/nv20_shader.h): campos do upload.
- [nxdk compiler main.c, commit fixado](https://github.com/XboxDev/nxdk/blob/7e8d4fc3d00ca59e88298e912438ac215cf93392/tools/vp20compiler/main.c): seleção dos operandos, em particular ADD A/C e ILU C.

Essas referências descrevem hardware/formato; não importamos código de
runtime, compilador, simulador ou emulador. O decoder novo não executa
operações. A compatibilidade com o asset original é confirmada pela fonte
fixada e pelo consumidor estático.

`tools/analysis/eagl_vertex_program_control.py` extrai novamente da ISO,
confere símbolos/relocations e usa representação diferente: um inteiro
128-bit com word 3 no extremo menos significativo. Todos os campos de
cada linha e o summary .NET são confrontados. Controle de 5 trechos/65 sites
do XBE inclui `0x291D70`: upload em chunks de até 32 DWORDs por `rep movsd`,
método 0xB00, sem reordenação dos DWORDs no trecho. 18 mutações negativas
rejeitam campos falsos, promoção semântica, envelope/FINAL/reserved bits,
truncamento, bytes excedentes e alteração do consumidor de upload.

Self-tests: operandos inativos, ADD A/C, instrução pareada, registro dividido
entre words, 256 swizzles/negações via packing independente, limites e
readiness ausente/stale/promovido. Subgate limitado
`rendering-vertex-program-opcode-operands`: `verified-static-instruction-layout`.
`parent_inheritance_binding=not-proven`, `full_rendering_binding=not-proven`,
`original_semantics=not-claimed`; readiness global continua `NOT_READY`.

## Validação

Focal: Release 0 warnings/0 errors, 102/102 self-tests; decoder e controle
independente exit 0. SHA-256 report:
`458087B9600677BF1C2D64799B0101EA74478B14C1D1B4699E2B163A8F9E766F`;
controle estático:
`421ECA3DCDB2F72131DB57D3489B07A35AD8C182207922F1C2409A7587BBE955`.
Relatório anterior de declaração byte-idêntico:
`D439A519D72E2F9718EAD9A312CD8C3997C3A60A43EC9398014F966F79488F6B`.
Matriz completa finalizada com exit 0: Release 0 warnings/0 errors,
102/102 self-tests .NET e 10/10 do runner, smokes, runtime headless/windowed
e replays determinísticos aprovados. Log:
`fifa_pc/stage-1.30-matrix-2026-10-13.log`;
`matrix_status=completed-authored-and-candidate-opt-in`. ISO preservada:
2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Readiness global `NOT_READY`; subgate novo 46/46, sem relaxar gates principais.

Próximo avanço: validar os campos completos do render method de modelo,
a declaração e as referências explícitas de programa. Sondagem posterior
somente leitura encontrou +0x1C e +0x2C referindo o mesmo parent nos 2364
registros e três nomes de programas: Skin=1416, Cloth=594, Eye=354.
Isto corrige a pista incompleta do prefixo antigo; ainda não é controle
implementado nem prova de resolução do loader. Manter separado o seletor
dinâmico 0x00345BCC e os gates de
áudio/animação/deformação. Não declarar conclusão do projeto.
