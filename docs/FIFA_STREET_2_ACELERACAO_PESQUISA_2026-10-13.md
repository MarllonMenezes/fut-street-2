# FIFA Street 2 — pesquisa de aceleração e aplicação

## Atualização de direção — 2026-10-13

Esta pesquisa documenta o escopo anterior de reimplementação .NET. O objetivo
agora confirmado é recompilação estática do código original para PC, não
recriação do gameplay. Leia
[direção vigente](FUT_STREET_2_DIRECAO_RECOMPILACAO.md).
As decisões históricas abaixo não constituem adoção nem rejeição definitiva
do runtime XboxRecomp no novo escopo. Stack e política de GPU/áudio pendem de
confirmação; XBE ainda não recompilado; `NOT_READY`.

Pesquisa em 2026-10-13, America/Sao_Paulo. Objetivo: reduzir o custo das
descobertas que bloqueiam a reimplementação .NET. Os critérios dos gates e
a política de acesso somente leitura à ISO permanecem vigentes.

## Resultado da pesquisa

Há ferramentas úteis para encurtar a análise. Nenhuma das fontes consultadas
demonstrou um port completo de FIFA Street 2 aplicável diretamente a este
workspace. Isso é um limite da pesquisa, não uma afirmação de inexistência.
O ganho mais concreto está em localizar consumidores dos dados, verificar
seus contratos nos bytes e implementá-los em decoders novos.

| Recurso público | Utilidade neste projeto | Decisão e limite |
| --- | --- | --- |
| [XboxRecomp: decompilação](https://github.com/sp00nznet/xboxrecomp/blob/main/docs/DECOMP.md) | Separação de funções, referências e análise estática sem rodar o recompilador | Continuar usando como mapa auxiliar; verificar trechos delimitados contra o XBE fixado |
| [Ghidra](https://github.com/NationalSecurityAgency/ghidra) | Disassembly, decompilação e análise de referências | Candidato para consumidores maiores; nomes e pseudocódigo são hipóteses até conferência dos bytes |
| [nxdk: registradores NV2A fixados](https://github.com/XboxDev/nxdk/blob/7e8d4fc3d00ca59e88298e912438ac215cf93392/lib/pbkit/nv_regs.h) | Definições de formatos de array de vértices, índices, shader e textura | Referência pública para interpretar os consumidores; constantes de hardware não ligam, sozinhas, um asset a UV/material |
| [vgmstream: ABK/SCHl](https://github.com/vgmstream/vgmstream/blob/master/src/meta/ea_schl_abk.c) e [ABK/EAAC](https://github.com/vgmstream/vgmstream/blob/master/src/meta/ea_eaac_abk.c) | Parsers públicos de variantes de bancos EA e seleção de subsounds | Referência de formato e possível controle de PCM; verificar a variante antes de comparar. Não prova EVT→sample |
| [envytools: encoding XF/Kelvin](https://envytools.readthedocs.io/en/latest/hw/graph/xf/isa.html) | Campos de instrução, fontes, máscaras e opcodes publicados | Próxima referência para um decoder estático; conferir variante, ordem dos words e operandos ativos nos 736 bytes. Não adotar execução/simulação |
| [nv2a-trace](https://github.com/XboxDev/nv2a-trace) | Inspeção de métodos GPU em um Xbox conectado | Depende de hardware e execução do título; fora das ações autorizadas atuais |
| [nv2a_vsh_debug](https://github.com/abaire/nv2a_vsh_debug) | Depuração por simulação de shaders | Não adotar o simulador; o projeto mantém a exigência de não usar emulação |

O [README do XboxRecomp](https://github.com/sp00nznet/xboxrecomp) descreve
saída C e bibliotecas de compatibilidade/hardware. Esse caminho tem escopo
diferente do produto .NET solicitado. Os resultados publicados para outros
jogos não fornecem prazo comprovado para FIFA Street 2 nem fecham seus gates.
O checkout local de análise segue fixado em
`193e2995ffaec871bb3be43a43bd66aa5fccfeb1`; a pesquisa na versão pública
atual não atualizou silenciosamente essa dependência.

## O que deve acelerar de fato

1. **Investigar consumidores de campos.** Usar a tabela de handlers como
   ponto de entrada. Exigir uma cadeia endereço→operação→dado original,
   antes de tentar joins de nomes ou atribuições globais.
2. **Fechar uma cadeia pequena de rendering.** Partir de um objeto conhecido
   como `tops0030.ord`, com contagem/índices identificados, e investigar modo
   de primitiva, cópia de buffer selecionada, declaração de atributos,
   shader e material. Integrar um recorte verificável ao renderer somente
   depois de identificar esses campos. Um recorte estático não fecha
   animação ou o jogo inteiro.
3. **Usar dados completos.** O relatório antigo de state observa somente o
   primeiro par de parâmetros. A etapa 1.25 inventaria todos os pares;
   controles antigos de sobreposição não devem excluir associações em pares
   posteriores. Essa correção do recorte é mais útil que repetir os mesmos
   controles com outro agrupamento.
4. **Validar bytes por uma implementação independente.** Reutilizar os
   leitores BIG/RefPack existentes e comparar os payloads da ISO com o decoder
   .NET. Um segundo parse do mesmo TSV mede consistência, mas não substitui
   a releitura independente dos dados de origem.
5. **Concluir etapas coerentes.** Durante a investigação, executar testes
   focados. Ao fechar cada etapa, executar a matriz completa exigida pelo
   usuário: Release, self-test, smokes, headless/windowed, replay e SHA-256.
   Evitar criar uma etapa para cada recombinação negativa de relatórios.
6. **Usar áudio externo como controle de formato.** A implementação atual
   já decodifica candidatos de áudio. O maior bloqueio é seleção de evento,
   não apenas extração de PCM. Investigar o consumidor EVT/ABKC e seus campos
   de seleção antes de ampliar buscas exaustivas de nomes/hashes.

Não há medição que permita prometer um multiplicador de velocidade ou uma
data final. Medir avanço por contratos resolvidos e integrações verificadas,
não pelo número de auditorias ou porcentagem de subgates verdes.

## Aplicação concreta — etapa 1.26

Foi implementado `fifa_pc/Runtime/EaglIndexedDrawSourceAudit.cs`. O decoder
liga o último comando 2 e o último comando 14 anteriores ao comando 26 no
mesmo descritor. Para os endereços locais independentes do seletor dinâmico,
o comando 2 fornece a contagem no alvo do par e o comando 14 fornece a base
dos índices. O payload é decodificado como índices unsigned little-endian
de 16 bits, conforme o caminho do consumidor verificado estaticamente.

- `2.364` submissões inventariadas; `2.363` fontes locais verificadas;
  `1` pendente; `0` fontes rejeitadas.
- `1.739.618` bytes de índices decodificados e conferidos diretamente na ISO
  pelo controle Python independente.
- `2.363/2.363` ranges compatíveis com todos os registros-base de buffer
  associados no descritor; isso não resolve os buffers selecionados pelo runtime.
- A pendência é `tops0260.big!tops0260.ord`, layer `0`, primitive `7`: falta
  comando 2 no descritor. Não foi herdada uma contagem de outra primitiva.
- Controle estático: `5` trechos, `37` endereços de instrução; `4` controles
  negativos sobre cópias na memória.

Relatórios: `fifa_pc/eagl-indexed-draw-source-audit.tsv`, seu summary,
`fifa_pc/eagl-indexed-draw-source-control.tsv` e seu summary. O readiness
tem um subgate limitado `rendering-original-local-indexed-draw-source`.
`runtime_selector=unresolved`, `vertex_attributes=not-proven`,
`full_rendering_binding=not-proven`, `original_semantics=not-claimed`.

## Próximo experimento discriminativo

Pergunta: qual declaração e shader vinculam os slots efetivamente usados a
atributos, e quais operações levam esses atributos à posição e ao sampling
de textura? Inspecionar os consumidores de TAR, os headers
`SET_VERTEX_DATA_ARRAY_FORMAT` e o programa de vértices carregado. As rotinas
de upload de constantes (`0x001A2970`) não bastam para identificar o layout
de entrada. Manter separado o seletor em `0x00345BCC`; `7.092` referências
de buffer da etapa 1.25 dependem dele.

Condição de avanço: conferir bytes, formato, slots e consumo por uma fonte
independente, e então integrar um draw verificável. Se a declaração/programa
não for localizada, registrar o limite e seguir a chamada que a instala,
sem chamar float2 unitário de UV nem atribuir modo de primitiva ao word 2
do comando 26.

### Pistas para retomar a investigação

Inspeção estática preliminar, ainda fora de um novo controle/readiness:

- O comando 4 lê campos `+0x10` e `+0x18` do render method corrente, e os
  wrappers `0x001A2920` e `0x001A2950` chegam respectivamente a
  `0x002927F0` e `0x002929D0` na seção D3D. Essas chamadas são o próximo
  recorte para investigar instalação de programas/declarações; não atribuir
  formato de atributo somente pelo endereço da chamada.
- No método de `tops0030.ord`, primitiva 0, offset `0x2B68`, o campo
  `+0x10` serializado é zero, sem relocation; em `+0x1C` há relocation
  externa para `ParentRM_FifastCharacterSkin`. A declaração completa pode
  depender do processo que resolve esse parent; o zero não é uma declaração.
- Há diferentes writers de `0x00345BCC`: `0x0019E91B` usa um word com
  extensão de sinal; `0x0019F20F` grava zero; `0x0019FCFF`, `0x001A2FCE`
  e `0x001A30FB` obtêm um campo `+0xC0` de objeto. Isso impede assumir
  seletor constante zero ou chamá-lo automaticamente de alternância entre
  duas cópias. Identificar primeiro o produtor e o domínio desse campo.

### Fonte de material localizada e probe reproduzível

O arquivo `data/initstaz.big!initstay.big!mat.o` contém três símbolos definidos
na seção 1 relacionados ao parent encontrado no modelo:

| Símbolo | Offset na seção | Size declarado pelo símbolo |
| --- | --- | ---: |
| `__RM:::ParentRM_FifastCharacterSkin` | `0x1458` | 4 |
| `FifastCharacterSkin0__EAGLMicroCodeVS` | `0x1490` | 4 |
| `FifastCharacterSkin0__EAGLMicroCodePS` | `0x1780` | 4 |

Payload: `38.970` bytes; SHA-256
`75DBC11557C6F3E4A32D6D88372476398B3A4C435E3907C2902E38579FB29430`.
`tools/analysis/eagl_material_parent_probe.py` reproduz a extração somente
leitura, confere esse hash e os três símbolos únicos, e inventaria janelas
delimitadas de 56 bytes, words raw e relocations. As janelas são sondagens,
não comprimentos de registros inferidos a partir do size 4.

Smoke executado com exit `0`; relatórios
`fifa_pc/eagl-material-parent-probe.tsv` e seu summary, 3 símbolos/168 bytes
de janelas. Não foi criado um gate semântico para esse probe. O primeiro
word bruto no símbolo VS é `0x002E2078`; a estrutura e as instruções ainda
precisam de decoder validado. A coincidência de nomes fornece uma fonte
concreta para seguir a resolução do parent, mas não prova sozinha que o
runtime instala esse shader no draw ou que um slot seja UV.

Inspeção adicional raw da mesma fonte fixada: em `ParentRM + 0x08` há
relocation local para `0x1400`; em `+0x0C` há referência local para `0x1450`,
cujo word tem relocation nomeada para o símbolo VS; em `+0x14` a referência
local aponta para `0x1454`, com relocation nomeada para PS. O bloco `0x1400`
tem a sequência abaixo, seguida do word `0xFFFFFFFF` em `0x1428`:

```text
0x20000000  0x40320000
0x20000001  0x40220001
0x20000002  0x40400002
0x20000003  0x40320003
0x20000004  0x40150004
```

São offsets/words/relocations observados por leitura delimitada, ainda fora
de um decoder de declaração validado. Não atribuir a esses tokens um formato
ou significado de atributo antes de conferir o consumidor. O size dos
símbolos VS/PS permanece 4; ele não informa o tamanho de seus programas.

Próxima etapa recomendada naquele recorte: analisar `mat.o`, o consumidor dos
render methods e a resolução do parent antes de ampliar buscas globais.


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

## Aplicações seguintes da pesquisa

A etapa 1.27 otimizou a busca de nomes por byte spans: varredura real de
1,74 GB em cerca de 3–5 segundos, preservando os dois SHA-256 dos relatórios
anteriores. Matriz completa exit 0, 99 self-tests e ISO intacta. Detalhes e
fonte oficial do .NET: [validação otimizada](FIFA_STREET_2_VALIDACAO_OTIMIZADA_2026-10-13.md).

A etapa 1.28 implementou o decoder limitado da declaração em `mat.o` e
do envelope VS: cinco campos stream/registro/formato/largura, 44 bytes de
declaração e 46 instruções/740 bytes de programa com header. O consumidor
retail e a extração fresca independente conferem esse layout. Detalhes,
limites e encerramento da matriz: [declaração e envelope](FIFA_STREET_2_MATERIAL_DECLARACAO_2026-10-13.md).
Não há promoção para UV/posição/rig nem claim de herança completa do parent.

Outra otimização aplicada à matriz (etapa 1.29): preservar o build Release
e executar sua DLL diretamente nos smokes e runtimes. A medição focal
teve stdout idêntico e 100 testes em ambos os caminhos. Escopo, fonte
oficial e validação: [runner Release direto](FIFA_STREET_2_RUNNER_RELEASE_2026-10-13.md).

