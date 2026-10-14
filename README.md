# Fut Street 2

> [!IMPORTANT]
> **Importante**
>
> Fut Street 2 é um projeto não oficial e este repositório **NÃO contém a ISO, o XBE, assets ou outros recursos proprietários** de FIFA Street 2. Para reproduzir as análises e a compilação local, você precisa fornecer uma cópia do jogo obtida legalmente. O projeto ainda está em desenvolvimento e não é um jogo recompilado jogável.

> [!IMPORTANT]
> **Important**
>
> Fut Street 2 is an unofficial project, and this repository **does NOT include the FIFA Street 2 ISO, XBE, assets, or other proprietary resources**. To reproduce the local analysis and build, you must provide a legally obtained copy of the game. The project is still under development and is not a playable recompiled game.

Projeto comunitário de **recompilação estática de FIFA Street 2 (Xbox original) para PC**. A meta é traduzir o código x86 do jogo para código nativo compilável e reconstruir, com evidência, os serviços de sistema necessários para executá-lo no computador — preservando a lógica original do jogo, em vez de recriar o gameplay do zero.

> **Estado: em desenvolvimento — ainda não é um jogo recompilado jogável.** A tradução e a compilação de código gerado já foram exercitadas, mas inicialização completa, fidelidade e os gates de rendering, assets, gameplay e áudio não estão aprovados.

## Estado verificável — release 2.61

A primeira execução controlada do programa nativo recompilado entrou em
`0x0024D8BE`, realizou chamadas de kernel e criou threads. Ela interrompeu a
inicialização em `OUT DX,AL` (`0x00298030`). Esse site foi alcançado em runtime
apesar de estar fora dos grafos estáticos diretos dos marcos anteriores.

O patch `native_recomp/patches/0026-lower-tv-encoder-init-write.patch`
traduz essa escrita para um helper de compatibilidade. O tradutor exige uma
preparação de `DX=0x80C0` no mesmo bloco; o host aceita somente o valor `1`
observado nesse caminho. A referência independente é o
[handler GPIO do xemu](https://github.com/xemu-project/xemu/blob/36334d95b86f9c3c6ae34efb9af06785273f8e1a/hw/xbox/acpi_xbox.c),
que não aplica efeito à escrita no offset zero. Isso sustenta uma compatibilidade
restrita para bring-up; não comprova o comportamento do hardware original.
Portas e valores de escrita não suportados encerram o processo com diagnóstico.

`tools/run-host-bringup.ps1` executa o recompilado com timeout de até 60 segundos,
logs, saves e temporários isolados no D:. `RECOMP_DIAGNOSTIC_TRAP=1` interrompe
chamadas indiretas não resolvidas, alvos inválidos, leituras de I/O não suportadas
e instruções sem tradução antes de aplicar retornos artificiais.

A geração completa com patches 0001–0026 traduziu `15.600/15.600` funções, sem
falhas; resta o site `SGDT` sem lowering. O build Release passou, os testes do
host passaram `8/8` e a suíte Python do XboxRecomp passou `704` testes com `2`
skips (o módulo opcional `tools/fusion/test_lifter_diff.py` foi excluído).
Os testes públicos passaram `161/161`; build e self-test do protótipo .NET
passaram, mas validam apenas seus próprios componentes (`105` testes).
A ISO manteve o SHA-256 original.

Após a correção, a execução ultrapassou o `OUT` e atingiu o timeout de 15
segundos, com esperas de eventos ainda presentes. Não há aprovação de boot
completo ou primeiro frame. O próximo passo é localizar a espera no caminho
de inicialização e conectar os serviços gráficos necessários ao host nativo.
O trace de inicialização/D3D avançou até `sub_00293500`: sua chamada a
`sub_002931B0` retornou, mas não há retorno observado da rotina exterior.
O ASM mostra ali uma espera por progresso do consumidor de comandos em
`0x00293637`. Essa é a hipótese focal seguinte, ainda sem confirmação do
contador em memória no momento do timeout. O coletor drena stdout/stderr
simultaneamente para evitar que o próprio volume de trace bloqueie o teste.
`NOT_READY` permanece. A estimativa histórica de `31%` não tinha uma métrica
funcional suficiente e não deve ser interpretada como percentual medido.

## Estado verificável — release 2.60

O release 2.60 adiciona o patch
`native_recomp/patches/0025-guard-indirect-tail-targets.patch` à pilha do
XboxRecomp. O macro `RECOMP_ITAIL`, usado por saltos indiretos em cauda, agora
aplica a mesma guarda de intervalo de código usada pelos `ICALL`: alvos fora
das seções executáveis e fora da faixa sintética de thunks kernel são
registrados e rejeitados antes de qualquer lookup manual, normal ou kernel.

O patch foi aplicado em uma cópia limpa da pilha 0001–0024; o teste
comportamental do tail-dispatch passou e a suíte focal completa passou em
`701` testes, com `2` skips. Isso endurece a segurança do dispatch, mas não
resolve os `7` callbacks dinâmicos nem atribui destinos a eles. O XBE não é
executado, `NOT_READY` permanece e a estimativa conservadora do PoC jogável
continua em aproximadamente `31% concluído / 69% restante`.

## Estado verificável — release 2.59

O release 2.59 adiciona
`tools/analysis/recomp_translation_coverage_gate.py`, que junta três
evidências estáticas do mesmo artefato: o alcance direto a partir do entry
point `0x0024D8BE`, o alinhamento entre `summary.json`, C gerado e ASM, e o
contexto dos dois sites ainda sem lowering. Contra o artefato 25f local, o
gate confirma `78` funções diretamente alcançáveis, `80` após os tail-jumps
observados, zero traduções ausentes nesses escopos e os dois sites
`SGDT [esp+6]` (`0x00253FD8`) e `OUT DX,AL` (`0x00298030`) fora de ambos.

Isso fecha uma regressão de cobertura, não implementa semântica de hardware:
`SGDT`/`OUT` continuam sem lowering, `hardware_semantics=not-proven`, o XBE
não é executado, `NOT_READY` permanece e a estimativa conservadora do PoC
jogável continua em aproximadamente `31% concluído / 69% restante`.

## Estado verificável — release 2.58

O release 2.58 endurece
`tools/analysis/recomp_host_preflight_evidence_gate.py`: uma saída aprovada
agora precisa demonstrar explicitamente `Loaded 11/11 sections` e
`xbox_MemoryLayoutInit: complete`, além das guardas dinâmicas, do entry point
e, no modo runtime, da ponte `128/128`. Isso evita que um marcador final seja
aceito sem a etapa correspondente de carregamento da imagem.

A correção foi repetida contra o host local nos modos estático e runtime, sem
`--launch` e sem execução guest. É uma melhoria de força do gate, não uma
aprovação de boot ou fidelidade; `NOT_READY` permanece e a estimativa
conservadora do PoC jogável continua em aproximadamente `31% concluído / 69%
restante`.

## Estado verificável — release 2.57

O release 2.57 adiciona
`tools/analysis/recomp_host_preflight_evidence_gate.py`, um gate que torna
reproduzível a fronteira entre preparar o host e executar o jogo. No artefato
local 25f, o modo estático carregou `11/11` seções da XBE, inicializou o mapa
de memória e resolveu o entry point traduzido. O modo runtime de preparação
também inicializou a ponte kernel com `128/128` entradas resolvidas (`128`
bridged, `0` stub).

Os dois modos passaram sem `--launch`; o gate exige os marcadores de guardas
dinâmicos, entry point e ponte quando aplicável, e rejeita evidência de chamada
ao entry point. Isso confirma preparação e wiring do host, não boot: nenhum
código guest foi executado, rendering, assets, gameplay, animação e áudio
continuam sem aprovação, `NOT_READY` permanece e a estimativa conservadora do
PoC jogável continua em aproximadamente `31% concluído / 69% restante`.

## Estado verificável — release 2.56

O release 2.56 adiciona
`tools/analysis/eagl_static_render_chain_control.py`, um gate estático que
fecha a consistência entre quatro relatórios independentes da cadeia EAGL:
fontes de draw indexado, parâmetros de render, declaração de material e
referências modelo/material. Contra os relatórios locais atuais, ele confirma
`2364` linhas de draw, `2364` primitivas, `2364` modelos, `3` definições de
material, `5` elementos de declaração e `46` instruções do programa VS, além
de conferir a identidade da mesma XBE e os hashes dos artefatos relacionados.

O gate reduz o risco de uma etapa consumir um relatório incompatível com a
outra, mas não transforma a cadeia estática em rendering executável. O
seletor em runtime, a ligação de atributos, a escolha de buffers, o binding
completo e a semântica original continuam explicitamente não provados; o XBE
não é executado, `NOT_READY` permanece e a estimativa conservadora do PoC
jogável continua em aproximadamente `31% concluído / 69% restante`.

## Estado verificável — release 2.55

O release 2.55 adiciona
`tools/analysis/recomp_kernel_service_contract_gate.py`, um gate estático
para os serviços kernel usados pelos `48` sites de thunk do subgrafo direto.
Com a fonte `kernel_thunks.c`, ele confirma cobertura explícita de resolver e
bridge em `48/48` sites, `19/19` targets definidos, `19/19` assinaturas
compatíveis e `0` targets ausentes.

O gate também preserva os riscos em vez de escondê-los: os targets
`xbox_MmPersistContiguousMemory` e `xbox_NtOpenSymbolicLinkObject` continuam
marcados como stub/no-op, enquanto
`xbox_NtQueryInformationFile`, `xbox_NtSetInformationFile` e
`xbox_RtlNtStatusToDosError` mantêm tokens `STATUS_NOT_IMPLEMENTED`. Os
call sites e argumentos constantes observados ficam fechados no contrato para
detectar regressões.

Isso é cobertura de fonte, ABI estrutural e classificação de risco; não prova
efeitos colaterais, status codes, handles ou semântica Xbox. O XBE não é
executado, nenhum serviço é promovido a equivalente original e a estimativa
conservadora do PoC jogável permanece em aproximadamente `31% concluído / 69%
restante`.

## Estado verificável — release 2.54

O release 2.54 adiciona
`tools/analysis/recomp_unimplemented_context_control.py`, um gate estático
para os dois opcodes que ainda permanecem sem lowering no artefato 25f. O
controle confirma `OUT DX,AL` em `0x00298030` dentro de `sub_00297F04`, com
`DX=0x80C0`, `AL` vindo de `BL` e duas chamadas indiretas logo depois; também
confirma `SGDT [esp+6]` em `0x00253FD8` dentro de `sub_00253FD0`, seguido pelo
trecho observado `CLI`/`XCHG`/`STI`/`LJMP`.

Isso registra contexto de instruções e ownership de função, mas não atribui
semântica ao hardware, ao encoder, à GDT ou ao salto de segmento. Os dois
sites continuam deliberadamente sem lowering, o XBE não é executado e a
estimativa conservadora do PoC jogável permanece em aproximadamente `31%
concluído / 69% restante`.

## Estado verificável — release 2.53

O release 2.53 adiciona
`tools/analysis/eagl_selector_helper_effect_control.py`, que verifica os
helpers alcançados pelo envelope do seletor contra formas estáticas dos seus
callees. O gate cobre `12` helpers e `40` arestas de chamada observadas. Entre
os efeitos preservados estão a leitura/retorno de `0x00345D88`, a atualização
dos pares indexados `0x00345C80`/`0x00345CC0`, as gravações em
`0x00345D00`/`0x00345D08` e o rearranjo SIMD de quatro lanes de origem para
oito destinos em `sub_00254349`.

Isso é uma auditoria de instruções, chamadas e destinos globais observados;
não identifica esses valores como estados GPU, materiais, shaders ou
buffers. O XBE não é executado, `runtime_selected_buffer` e
`rendering_binding` continuam não provados, e a estimativa conservadora do
PoC permanece em aproximadamente `31% concluído / 69% restante`.

## Estado verificável — release 2.52

O release 2.52 classifica a forma downstream dos `33` consumidores do
seletor EAGL. O gate recupera `28` helpers de composição de endereço e cinco
formas especiais observadas no ASM: `sub_001A3E00` retorna o endereço
derivado, `sub_001A3E40` carrega o índice e grava três globals, `sub_001A43A0`
faz duas leituras float indexadas, `sub_001A4760` persiste o endereço em
`0x00345FA4` e `sub_001A48F0` faz uma leitura word indexada.

Essas são classificações de instruções e destinos observados; não são nomes
de atributos, buffers, materiais ou comandos GPU. O XBE não é executado, os
bindings de runtime permanecem não provados e a prontidão continua
`NOT_READY`, com a estimativa conservadora do PoC em aproximadamente `31%
concluído / 69% restante`.

## Estado verificável — release 2.51

O release 2.51 fecha mais uma fronteira estática do seletor EAGL: todas as
`31` funções que contêm os `33` consumidores também referenciam diretamente
os dois anchors globais usados pelo helper (`0x003458D0` e `0x003458D4`). O
gate exige `consumer_anchor_counts=33/33` para os dois endereços e falha se
uma função perder qualquer anchor. Isso preserva a correlação do seletor com
o envelope de estado observado no ASM, sem chamar esse padrão de buffer GPU
ou afirmar um dataflow completo em runtime.

O resultado ainda é estático: não prova alias entre os anchors e objetos em
runtime, escolha de buffer, instalação de shader, submissão de draw ou
equivalência de GPU. O XBE não é executado; a prontidão permanece `NOT_READY`
e a estimativa conservadora do PoC jogável continua em aproximadamente `31%
concluído / 69% restante`.

## Estado verificável — release 2.50

O release 2.50 amplia o gate do seletor EAGL para preservar o envelope dos
consumidores, não apenas sua contagem. Os `33` reads de `0x00345BCC` agora
são associados a `31` funções entre `0x001A3E00` e `0x001A4B40`, e cada
função tem sua família de chamadas diretas verificada a partir dos cabeçalhos
do ASM. O inventário mantém `4` helpers sem chamada direta e as demais
famílias repetíveis de acesso/estado, sem transformar esses nomes em uma
interpretação semântica do rendering.

Esse envelope reduz a superfície estática que precisa ser reproduzida, mas
ainda não prova alias de runtime, escolha de buffer, instalação de shader,
submissão de draw ou equivalência de GPU. O XBE não é executado; a prontidão
permanece `NOT_READY` e a estimativa conservadora do PoC jogável continua em
aproximadamente `31% concluído / 69% restante`.

## Estado verificável — release 2.49

O release 2.49 estende
`tools/analysis/eagl_dynamic_buffer_selector_control.py` para auditar também
os consumidores estáticos do seletor EAGL em `0x00345BCC`. Contra o
disassembly integral 25f e a ISO local, o domínio agora fecha em `5` writers
e `33` leituras: `32` consumidores usam o valor como fator de índice em
`IMUL`, e `1` carrega o valor para um registro antes de multiplicar
(`0x001A3E46`). Os endereços são verificados individualmente, impedindo que
uma leitura nova ou uma mudança de forma passe despercebida.

Isso prova somente o domínio estático de produção e consumo do seletor; não
prova qual buffer é escolhido em runtime, o alias entre os objetos e os
draws, a ligação com rendering, shaders ou a semântica original. O XBE não é
executado; a prontidão permanece `NOT_READY` e a estimativa conservadora do
PoC jogável continua em aproximadamente `31% concluído / 69% restante`.

## Estado verificável — release 2.48

O release 2.48 adiciona
`tools/analysis/eagl_dynamic_buffer_selector_control.py`, um controle
estático independente para o seletor EAGL em `0x00345BCC`. Contra o
disassembly integral 25f e a ISO local, foram recuperados exatamente `5`
writers: `1` word com extensão de sinal vindo de tabela (`0x0019E91B`), `1`
literal zero (`0x0019F20F`) e `3` valores vindos de campos de objeto `+0xC0`
(`0x0019FCFF`, `0x001A2FCE` e `0x001A30FB`).

Essa classificação prova que o seletor não pode ser reduzido estaticamente a
uma constante única e preserva o domínio dos produtores observados. Ela não
escolhe o buffer em runtime, não prova o alias entre os objetos e os draws,
não instala shaders e não fecha o rendering original. O XBE não é executado;
a prontidão permanece `NOT_READY` e a estimativa conservadora do PoC jogável
continua em aproximadamente `31% concluído / 69% restante`.

## Estado verificável — release 2.47

O release 2.47 adiciona
`tools/analysis/recomp_fixed_callback_slot_evidence_gate.py`. No C gerado
integral, o inventário reproduzível mantém `0` writers provados para os slots
BSS `0x00360A70` e `0x00360AA8`, e exatamente `1` writer para
`0x00360AB4`, que grava o endereço conhecido `0x002774F9`. Os três call sites
observados também permanecem estáveis: `0x00274FC5`, `0x00276E59` e
`0x00277532`, com a classificação correspondente de cada slot.

O gate separa `2` slots sem inicializador local provado de `1` ponteiro de
função já identificado. Isso é evidência estática do inventário de writers e
da classificação dos call sites; não prova que o kernel não preencha os dois
slots em runtime, nem que qualquer callback seja seguro para execução. O XBE,
o C gerado e o runtime host não são executados. A prontidão permanece
`NOT_READY` e a estimativa conservadora do PoC jogável continua em
aproximadamente `31% concluído / 69% restante`.

## Estado verificável — release 2.46

O release 2.46 adiciona
`tools/analysis/recomp_dynamic_callback_evidence_gate.py`, um gate estático
específico do artefato integral para impedir regressões na classificação dos
callbacks dinâmicos. Contra a geração limpa 25f, ele preserva os `7` sites
exatos (`0x0024D3B1`, `0x0024D4C7`, `0x00250612`, `0x002512A4`,
`0x00252C7D`, `0x00252CF1` e `0x0026E75E`) e as famílias `1` de memória,
`1` de campo de objeto, `1` de registro indexado e `4` protegidas pelo PRCB.

O gate também exige evidência de origem local, contagem de pushes e guard
condicional em `7/7` sites, além de `0` writers diretos provados. A
classificação de inicialização permanece conservadora: `1` valor zerado na
imagem, `4` caches PRCB zerados no host e `2` inicializações ainda não
provadas em runtime (campo de objeto e registro indexado). O produtor do
campo de objeto, a fronteira do frame indexado e a raiz estática da tabela
continuam documentados, sem promover nenhum destino a callback resolvido.

Esse gate é uma proteção contra regressão de evidência, não um resolver. Ele
falha se aparecer, desaparecer ou mudar de família um site conhecido e não
executa o XBE, o C gerado ou o runtime host. A prontidão permanece `NOT_READY`,
a estimativa conservadora do PoC jogável continua em aproximadamente `31%
concluído / 69% restante`, e boot, rendering, assets, gameplay, animação,
áudio e a semântica completa dos serviços Xbox continuam pendentes.

## Estado verificável — release 2.45

O release 2.45 adiciona o contrato host-only
`no_emulation_kernel_file_contract_test`. Contra a pilha XboxRecomp usada no
host, o teste exercita `XboxFilePositionInformation` (`14`),
`XboxFileNetworkOpenInformation` (`34`), reserva/altera tamanho de arquivo e
as conversões observadas de `NTSTATUS` para erro DOS. O teste usa somente um
arquivo temporário do host e lookups guest nulos; não chama o dispatch nem o
entry point recompilado. O CTest completo passou em `7/7`.

Esse marco reduz uma lacuna verificável dos serviços de arquivo, mas não prova
que toda a semântica Xbox esteja coberta nem que o jogo atravesse esse caminho.
Boot, rendering, assets, gameplay, animação e áudio continuam pendentes, assim
como `SGDT`/`OUT`, callbacks dinâmicos e a semântica completa dos serviços Xbox.
A prontidão permanece `NOT_READY`, a estimativa conservadora do PoC jogável
continua em aproximadamente `31% concluído / 69% restante`, e o XBE não é
executado.

## Estado verificável — release 2.44

O release 2.44 adiciona `tools/run-host-preflight-gate.ps1`, um gate
reutilizável que executa somente `--preflight` no host compilado e recusa
variáveis de emulação ou hardware sintético. No artefato 25f local, o modo
estático passou carregando `11/11` seções do XBE, inicializando o mapa de
memória, validando os guards dos callbacks em zero e resolvendo o entry point,
sem executar código guest. O modo de runtime também passou sem chamar o entry
point: o bridge resolveu `128/128` thunks e confirmou dispatch, memória e entry
point traduzido.

Esse marco aprova apenas a preparação do host e a fronteira de entrada; não
aprova boot, execução do jogo, rendering, assets, gameplay, animação ou áudio.
`SGDT`/`OUT`, callbacks dinâmicos e a semântica completa dos serviços Xbox
continuam pendentes. A prontidão permanece `NOT_READY` e a estimativa
conservadora do PoC jogável continua em aproximadamente `31% concluído / 69%
restante`; o XBE não é executado.

## Estado verificável — release 2.43

O release 2.43 transforma o link do host opcional em um gate reproduzível por
`tools/build-host-link-gate.ps1`. Contra o artefato completo 25f e uma cópia
limpa da pilha XboxRecomp 0001–0024, o host foi configurado em Release,
compilou as `62` unidades C geradas e o kernel bridge, e foi ligado contra as
bibliotecas seletivas de D3D8/video/input fornecidas por `-HostLibrary`, sem
símbolos não resolvidos.
O CTest seguro passou em `6/6`: runtime support, packed-word ops, dispatch,
`--help` e as duas recusas de preflight. O gate não executa o XBE.

Esse marco prova a integração de compilação/link do host, não a execução do
jogo nem a fidelidade do runtime. Os dois opcodes `SGDT`/`OUT` continuam sem
lowering, os callbacks dinâmicos e a semântica completa dos serviços Xbox ainda
não estão aprovados, e os gates de boot, rendering, assets, gameplay, animação
e áudio continuam pendentes. A prontidão permanece `NOT_READY`, a estimativa
conservadora do PoC jogável continua em aproximadamente `31% concluído / 69%
restante`.

## Estado verificável — release 2.42

O release 2.42 adiciona um gate estático de evidência para as instruções que
ainda não têm lowering. A checagem cruza o `summary.json` da recompilação,
os marcadores `RECOMP_UNIMPL` no C gerado e a linha correspondente no ASM
recuperado; no artefato integral 25f, os `2/2` sites coincidem exatamente:
`SGDT [esp + 6]` em `0x00253FD8` e `OUT DX,AL` em `0x00298030`. O gate também
tem testes negativos para divergência entre contagem, endereço, mnemônico e
operandos, evitando que uma regressão editorial seja confundida com avanço de
tradução.

Esse marco melhora a rastreabilidade da tradução, mas não implementa esses
opcodes. A análise estática mantém `SGDT` como operação dependente da GDT e
`OUT` como acesso ao hardware de vídeo Xbox; ambos continuam sem lowering até
que exista evidência independente suficiente. A suíte Python do projeto passou
em `121/121`, a suíte focal do XboxRecomp em `570` testes com `38` skips, o
build nativo permaneceu em Release com CTest `3/3`, e o .NET continuou com `0`
warnings, `0` errors e self-test `105/105`.

O resultado continua sendo evidência estática e de validação de ferramentas:
não prova boot, equivalência de hardware/GDT, rendering, assets, gameplay,
animação ou áudio. A prontidão permanece `NOT_READY`, a estimativa conservadora
do PoC jogável continua em aproximadamente `31% concluído / 69% restante` e o
XBE não é executado.

## Estado verificável — release 2.41

O release 2.41 refaz a análise integral do XBE com a pilha atual de `24`
patches, sem o modo `text-only`. A recuperação da tabela indexada enviesada
em `sub_0026FB60` agora registra `0x0026FBC4–0x0026FBD0` como dados; o byte em
`0x0026FBC9` pertence a esse slot de tabela e não é uma instrução `STI`
executável. Portanto o falso site `STI 0x0026FBC9` foi removido da tradução
sem adicionar um lowering especulativo.

A geração limpa integral produziu `15.600/15.600` funções, sem falhas, `61`
unidades C e `1.775.604` linhas de C, com `0` alvos de chamada não resolvidos
gerados como stub. Restam somente `2` instruções não implementadas no
artefato: `OUT DX,AL` em `0x00298030` e `SGDT` em `0x00253FD8`; o `STI` real
em `0x00253FE8` já é traduzido pelo helper host de máscara de interrupção. O
build nativo Release passou e o CTest passou em `3/3`.

Os gates auxiliares também passaram: a suíte focal do XboxRecomp teve
`570` testes aprovados e `38` skips; a regressão Python teve `118/118`; o
build .NET terminou com `0` warnings e `0` errors; e o self-test do produto
passou em `105/105`. A auditoria indireta permanece em `62` sites, com `48`
thunks de kernel, `7` callbacks dinâmicos sem destino provado e `4/4`
callbacks protegidos pelo PRCB com disposição de retorno classificada.

Esse resultado é evidência estática da correção da fronteira entre código e
dados; não prova boot, execução, equivalência de hardware/GDT, ABI, rendering,
assets, gameplay, animação ou áudio. A prontidão permanece `NOT_READY`, a
estimativa conservadora do PoC jogável continua em aproximadamente `31%
concluído / 69% restante` e o XBE não é executado.

## Estado verificável — release 2.40

O release 2.40 recupera uma única instrução `DAA` em `0x00270EA2`, mas
somente quando o armamento exato da tabela observado no título é reconhecido:
`CMP`/`SUB` definem `CF`/`AF` nas duas entradas e `NOP`, `LODSB` e `PUSH CS`
não alteram essas flags antes do `DAA`. O lowering é específico para essa
forma e tem teste negativo; outras formas continuam `RECOMP_UNIMPL`.

A pilha limpa de `24` patches aplicou em ordem sem erro e a suíte focal passou
em `570/570` testes, com `38` skips. A geração limpa emitiu
`15.600/15.600` funções, sem falhas, em `61` unidades e `1.775.817` linhas
de C. Restam `3` instruções não implementadas: `OUT DX,AL` (`0x00298030`),
`SGDT` (`0x00253FD8`) e `STI` (`0x0026FBC9`). O build nativo Release passou
e o CTest passou em `3/3`.

Essa redução é evidência de tradução estática para um padrão de controle
específico; ela não prova execução do bloco, equivalência completa de flags,
ABI, boot, rendering, assets, gameplay, animação ou áudio. A auditoria
indireta continua encontrando `62` sites, dos quais `48` são thunks de kernel
e `7` callbacks dinâmicos sem destino provado. A prontidão permanece
`NOT_READY` e a estimativa conservadora do PoC jogável continua em
aproximadamente `31% concluído / 69% restante`. O XBE não é executado.

## Estado verificável — release 2.39

O release 2.39 valida a pilha completa de `23` patches do XboxRecomp em uma
cópia limpa, em ordem sequencial, incluindo a correção do cabeçalho do hunk
do patch `0023` (`NtOpenSymbolicLinkObject`/`NtQuerySymbolicLinkObject`). A
ponte continua sendo apenas uma validação estática e de compilação para o
link simbólico de `D:`; isso não prova montagem de DVD, acesso a assets ou
execução do jogo.

Na geração limpa, foram emitidas `15.600/15.600` funções, sem falhas, em `61`
unidades e `1.775.809` linhas de C. Permanecem somente `4` instruções não
implementadas (`daa`, `out`, `sgdt` e `sti`), fora de uma afirmação de
completude. O build nativo Release da biblioteca passou, o CTest passou em
`3/3` e o alvo `xbox_kernel` compilou `23/23` objetos. A suíte de regressão
Python passou em `118/118`; o build .NET terminou com `0` warnings e `0`
errors, e o self-test do produto passou em `105/105`.

A auditoria estática continua encontrando `62` sites indiretos, dos quais
`48` são thunks de kernel, `7` callbacks dinâmicos sem destino provado e `4`
callbacks protegidos pelo PRCB com disposição de retorno classificada em
`4/4` (`2` sem observação antes da transferência e `2` sobrescritos antes da
observação). Esses números não provam ABI, valores de runtime, semântica
Xbox, fidelidade de rendering/gameplay/áudio ou boot do executável.

A prontidão permanece `NOT_READY` e a estimativa conservadora do PoC jogável
continua em aproximadamente `31% concluído / 69% restante`. Todo o trabalho
de recompilação e auditoria permanece estático; o XBE não é executado.

## Estado verificável — release 2.38

O release 2.38 separa, dentro dos `4` callbacks protegidos pelo PRCB, a
disposição limitada do valor retornado pela chamada indireta. Em `2` sites o
retorno não é observado antes da próxima transferência de controle local; nos
outros `2`, o registrador `eax` é sobrescrito antes de qualquer observação.
Esse resultado vem somente da janela sintática já auditada no C gerado.

O gate confirma `prcb_callback_return_disposition_sites=4`,
`prcb_callback_return_disposition_unclassified_sites=0` e
`semantics=not-claimed`. Ele não prova tipo, ABI, implementação do callback,
valor do PRCB no runtime ou semântica Xbox, e não resolve nenhum dos `7`
callbacks dinâmicos. A suíte Python passou em `118/118`, a prontidão permanece
`NOT_READY` e a estimativa conservadora do PoC jogável continua em aproximadamente
`31% concluído / 69% restante`. A análise permanece estática e não executa o
XBE.

## Estado verificável — release 2.37

O release 2.37 fecha um gate de forma para o callback de memória em
`0x0024D3B1`. A auditoria recupera, no código C gerado, o range
`[0x0030BC94, 0x0030BC98)`: span de `4` bytes, stride de `4` e exatamente `1`
slot. Os endpoints literais, o incremento do registrador, a comparação do
limite e o guard não-zero foram observados localmente. No caminho alternativo,
há um candidato de chamada direta para `sub_002504AF` antes do retorno; a
auditoria mantém essa relação como candidato de fluxo, não como semântica
provada.

A imagem estática mantém os valores do range zerados e não há writer direto
provado no C gerado. Portanto o gate comprova uma forma delimitada de polling e
branch, mas não prova registro externo, ABI do callback, destino runtime ou
semântica original. Os `7` callbacks dinâmicos continuam sem destino final
provado. A suíte Python passou em `117/117`, a prontidão permanece `NOT_READY`
e a estimativa conservadora do PoC jogável continua em aproximadamente
`31% concluído / 69% restante`. A análise permanece estática e não executa o
XBE.

## Estado verificável — release 2.36

O release 2.36 classifica os valores observados nos `8` writers candidatos
do campo de objeto `+0x584`, todos fora do subgrafo diretamente alcançável.
A auditoria encontra `2` escritas constantes zero, `2` constantes que não
coincidem com nenhum início de função gerada, `1` leitura de volta do próprio
campo e `3` fontes não constantes ainda não resolvidas.

Esse gate reduz a forma dos candidatos, mas não prova que qualquer writer
alcance o mesmo objeto de `0x002512A4`, nem que as fontes não constantes sejam
funções. Nenhuma constante foi promovida a callback; os `7` callbacks
dinâmicos continuam sem destino runtime provado. A suíte Python passou em
`116/116`, a prontidão permanece `NOT_READY` e a estimativa conservadora do
PoC jogável continua em aproximadamente `31% concluído / 69% restante`. A
análise permanece estática e não executa o XBE.

## Estado verificável — release 2.35

O release 2.35 junta a evidência do frame de callback indexado com a raiz
`0x00361360`. Foram observados `3` seeds estáticos do helper
`sub_002710F4` (`0x003034F8`, `0x00303F58` e `0x00303F68`) e os slots
correspondentes (`0x003034F8`, `0x00303F58` e `0x00303F74`); nenhum coincide
com a raiz indexada. Os layouts também permanecem distintos: stride `0x28`
na família de registros da raiz e stride `0xC` no consumidor
`sub_0026E70A`.

Esse é um limite estático verificável, não uma prova de que não exista alias
ou propagação de ponteiro em runtime: o status continua
`not-proven-runtime-alias`, nenhum callback é resolvido e nenhuma semântica de
tabela é implementada. A suíte Python passou em `115/115`, a prontidão
permanece `NOT_READY` e a estimativa conservadora do PoC jogável continua em
aproximadamente `31% concluído / 69% restante`. A análise permanece estática e
não executa o XBE.

## Estado verificável — release 2.34

O release 2.34 fecha um gate de fronteira dos produtores para os dois
callbacks que continuam sem destino runtime provado. No callback de campo de
objeto, o único caller direto observado passa o objeto derivado de
`MEM32(ebp + 8)`, não escreve localmente em `+0x584` e deixa `8` writers
candidatos fora do subgrafo direto. No callback de registro indexado, os `2`
callers diretos passam o frame `ebp - 0x10`; não há escrita direta observada no
campo de tabela `+0x8`, há `2` escritas no índice `+0xC`, e o inicializador da
raiz cobre `32` registros com o campo de destino zerado.

Essa fronteira é verificável, mas a identidade entre a raiz inicializada e o
objeto consumido pelo callback indexado continua não provada; os `47` writers
indexados fora do subgrafo seguem candidatos sem binding. Portanto o release
não resolve nenhum destino, não implementa semântica de objeto/tabela e não
afirma execução original. A suíte Python passou em `114/114`, a prontidão
permanece `NOT_READY` e a estimativa conservadora do PoC jogável continua em
aproximadamente `31% concluído / 69% restante`. A análise permanece estática e
não executa o XBE.

## Estado verificável — release 2.33

O release 2.33 adiciona ao auditor um gate conservador para os `7` callbacks
indiretos que ainda não têm destino final provado. Todos os `7/7` agora têm
uma classe de estado inicial observável, sem converter valores zerados em
bindings ou destinos inventados:

- `1` alvo de memória está zerado no snapshot do XBE, com guard não-zero e sem
  writer direto provado;
- `4` callbacks dependem de campos do cache PRCB; o layout PRCB sintético do
  host começa zerado e cobre os offsets observados, mas isso não prova o valor
  que o kernel Xbox teria em runtime;
- `2` continuam explicitamente sem inicialização runtime provada: `1` campo
  de objeto em `+0x584` e `1` registro indexado.

O gate comprova somente a classificação estática/conservadora (`1` snapshot
quiescente, `4` quiescentes no layout inicial do host e `2` potencialmente
inicializáveis em runtime). Ele não resolve os callbacks, não implementa o
kernel Xbox e não afirma semântica original. A suíte Python passou em
`113/113`, a prontidão permanece `NOT_READY` e a estimativa conservadora do
PoC jogável continua em aproximadamente `31% concluído / 69% restante`. A
análise permanece estática e não executa o XBE.

## Abordagem

- Usar o [XboxRecomp](https://github.com/sp00nznet/xboxrecomp) como base para análise e tradução estática do código Xbox x86 para C, com uma pilha de patches versionada em [`native_recomp/patches`](native_recomp/patches).
- Compilar o código traduzido para o host e implementar apenas compatibilidade sustentada por evidências do executável e dos call sites. Stubs, hipóteses e instruções ainda não traduzidas não contam como funcionalidades implementadas.
- Manter fora do caminho de execução qualquer emulação de CPU, GPU ou APU. Não incorporar código ou assets proprietários do jogo.

## Estado verificável — release 2.32

O release 2.32 reaudita as chamadas indiretas do artefato C completo contra a
metadata do XBE e a pilha de bridges do kernel. Foram observados `62` sites
indiretos em `32` funções; `48` sites usam thunks do kernel, distribuídos por
`19` ordinais únicos. Os `48/48` têm caso explícito no resolver, bridge e
contagem de argumentos, sem lacunas de stack; os corpos dos `19/19` destinos
também foram encontrados no código host. Isso valida o encanamento estático
dos thunks, não a fidelidade completa de cada API.

O limite restante é explícito: `7` callbacks continuam sem destino final
provado (`1` alvo de memória, `1` campo de objeto, `1` registro indexado e `4`
dependentes do cache PRCB). Não há writer direto provado para essas famílias no
subgrafo das `78` funções; os `55` writers encontrados fora dele continuam
apenas candidatos. Os guards PRCB começam zerados no layout host auditado, mas
isso não prova o comportamento do kernel Xbox nem resolve os callbacks.

A suíte Python continua em `112/112`, a prontidão permanece `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.31

O release 2.31 transforma a auditoria de `RECOMP_UNIMPL` em um gate que
também considera o alcance auxiliar por `tail jmp`. No C gerado completo do
estágio 23, `15.600/15.600` funções foram emitidas e restaram somente `2`
sites não implementados: `sgdt [esp + 6]` em `0x00253FD8` e `out dx, al` em
`0x00298030`.

Os dois sites estão fora das `78` funções diretamente alcançáveis e também
fora das `80` funções quando as duas arestas diretas `tail jmp` são seguidas.
Isso comprova apenas que o alcance estrutural auditado não contém esses
opcodes; eles continuam deliberadamente sem implementação e não devem ser
tratados como resolvidos. A compilação CMake do artefato gerado foi concluída,
com CTest auxiliar em `3/3` no build de bibliotecas e `6/6` no build host,
incluindo apenas smokes e rejeições de preflight — não boot ou execução do
XBE.

A suíte Python passou `112/112`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.30

O release 2.30 amplia a auditoria estrutural para transferências diretas
`tail jmp` emitidas no C gerado. Foram observadas `1.833` arestas explícitas;
somente `2` partem das `78` funções no subgrafo estrito do entry point
`0x0024D8BE`: `sub_00274612 (0x00274612) -> 0x002746B7` e
`sub_00277237 (0x00277237) -> 0x002772DC`.

Os dois destinos existem na metadata e nas definições C traduzidas. Ao seguir
essas duas arestas como um alcance auxiliar, o subgrafo passa de `78` para `80`
funções, sem destinos externos e sem traduções faltantes. Essa expansão é uma
checagem de cobertura da tradução gerada; não é prova de execução, não substitui
o subgrafo direto e não resolve os sete callbacks dinâmicos sem destino final
provado.

A suíte Python passou `111/111`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.29

O release 2.29 amplia a auditoria do alcance de tradução para arestas
`fallthrough` que não aparecem necessariamente em `calls_to`. A geração C
contém `38` transferências explícitas desse tipo; os `38/38` alvos existem
tanto na metadata de funções quanto nas definições C geradas. Nenhuma das
arestas parte das `78` funções diretamente alcançáveis pelo entry point
`0x0024D8BE`, portanto o resultado não altera artificialmente o subgrafo
direto nem inventa alcance de runtime.

O caso relevante para o consumidor indexado é a aresta estática
`sub_00271140 (0x00271140) -> 0x00271148`; ela foi registrada, mas permanece
fora do subgrafo direto. Isso preserva a distinção entre uma transferência
presente na fonte traduzida e uma função provada alcançável pelo entry point.
As arestas não resolvem o alvo dinâmico em `sub_0026E70A`, e os sete callbacks
dinâmicos continuam sem destino final provado.

A suíte Python passou `110/110`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.28

O release 2.28 audita a multiplicidade dos writers do campo de objeto
`+0x584`. A varredura completa encontrou `8` sites em `8` entradas de função;
há `1` grupo de intervalos sobrepostos com `3` entradas:
`0x00252107` (`sub_00252107`), `0x00252148` (`sub_00252148`) e
`0x00252156` (`sub_00252156`). Todas terminam em `0x0025252B`, e as duas
últimas aparecem na metadata como `tail_jump_alias`. A evidência correta,
portanto, é “três entradas sobrepostas no mesmo intervalo”, não três
implementações independentes confirmadas.

Esse agrupamento é uma normalização da metadata e dos sites gerados; não prova
que as entradas sejam aliases idênticos em runtime, que qualquer writer seja
alcançado, que o endereço-base seja a mesma instância usada pelo callback em
`0x002512A4`, ou que os valores escritos sejam funções válidas. O subgrafo
direto ainda tem `0` writer provado para o campo, e os sete callbacks
dinâmicos continuam sem destino final provado.

A suíte Python passou `109/109`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.27

O release 2.27 cruza os seeds empilhados pelos chamadores com os slots
consumidos por `sub_0026E70A`. O helper `sub_002710F4` recebe os seeds
`0x003034F8`, `0x00303F58` e `0x00303F68`; os três sites observados usam o
índice de frame `0`, `0` e `1`, respectivamente. Aplicando o stride de `0xC`
do registro do consumidor e lendo somente a imagem estática do XBE, os `3/3`
slots têm guard bruto `0x00000000` e alvo bruto em início de função conhecido:
`0x002732AE`, `0x00277B45` e `0x00277C46`.

Isso é uma evidência estática de candidatos de slot, não prova de execução,
alcance em runtime, inicialização efetiva da tabela ou identidade semântica
entre esses ponteiros e callbacks jogáveis. O guard zero indica que, na imagem
observada, o caminho localmente não habilita a chamada; o alvo não-zero é
somente um valor estático candidato. A busca delimitada nas fontes C geradas
também não encontrou escrita direta para esses seeds. Os sete callbacks
dinâmicos continuam sem destino final provado.

A suíte Python passou `108/108`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.26

O release 2.26 aprofunda a relação entre a raiz indexada e o consumidor
`sub_0026E70A`. No chamador diretamente alcançável `sub_00277AB1`, a fonte
gerada lê `0x00361360`, calcula o endereço de um registro com stride de
`0x28` (`40` bytes) e acessa o campo desse registro. Mais adiante, a chamada
ao consumidor passa o objeto local `ebp - 0x10` depois do helper
`sub_002710F4`; a varredura encontrou `0` escrita direta ligando esse frame ao
registro da raiz (`verified-bounded-root-record-frame-separation`).

Isso é evidência de duas formas locais coexistindo no mesmo fluxo, não uma
prova de que os objetos não possam compartilhar memória por meio do helper,
de alias indireto ou de estado do runtime. A raiz ainda tem `0` writer direto
no subgrafo e `1` writer observado no C completo; o consumidor continua com
layout próprio de registro de `0xC` bytes e o callback permanece dinâmico. Os
sete callbacks seguem sem destino final provado.

A suíte Python passou `107/107`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.25

O release 2.25 amplia a auditoria do cache PRCB para distinguir o subgrafo
diretamente alcançável do conjunto completo de funções nomeadas no C gerado.
O detector procura a forma local `FS:[0x20] -> +0x250` seguida de escrita,
incluindo aliases lineares do endereço. O resultado foi `0` writers provados
nas `78` funções diretamente alcançáveis e também `0` writers em todas as
funções geradas (`prcb_cache_all_writer_sites=0`); portanto não surgiu nenhum
writer adicional fora do subgrafo (`prcb_cache_outside_direct_writer_sites=0`).

Isso fortalece somente a evidência estática sobre o C gerado: não prova que o
kernel Xbox não preencha o cache em runtime, nem resolve os quatro callbacks
protegidos por esse cache. O layout host continua sendo uma inicialização
quiescente do preflight, não uma implementação do kernel, e os sete callbacks
dinâmicos seguem sem destino final provado.

A suíte Python passou `106/106`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.24

O release 2.24 faz uma checagem limitada de ligação dos writers candidatos
ao alvo indexado. Os `47` sites distribuídos em `28` funções foram agrupados
por corpo de função e cada corpo foi varrido pela literal da raiz
`0x00361360`. Nenhum dos `47` sites nem das `28` funções contém uma
referência direta observada a essa literal:
`verified-bounded-no-root-reference-in-writer-function-bodies`.

Esse é um resultado negativo delimitado: ele informa somente que a expressão
da raiz não apareceu no corpo local do writer. Não exclui uma base recebida
por argumento, propagação por registradores, chamadas indiretas, alias
transitivo ou uma ligação que só possa ser demonstrada em runtime. O
inicializador da raiz descrito no release 2.23 continua sendo uma instância
separada, sem alias estático provado para esses writers; os sete callbacks
dinâmicos seguem sem destino final provado.

A suíte Python passou `106/106`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.23

O release 2.23 audita a inicialização estática da raiz indexada
`0x00361360` em `sub_002772E2` (`recomp_0051.c`). A forma observada publica
a raiz, grava capacidade `0x20` e percorre um span de `0x500` bytes com stride
de `0x28`: a aritmética corresponde a `32` registros. Dentro desse laço, o
dword de cada registro em `+0x8` recebe uma forma de zero; o código também
observa `+0x0 = 0xFFFFFFFF`, `+0x4 = 0` e `+0x5 = 0xA`. Um segundo laço aplica
o byte `0xC1` em `+0x4` no prefixo de `3` registros (`0x78` bytes).

Esse marco prova somente a forma do inicializador gerado: não prova sucesso do
allocator, execução dos laços, identidade entre esses registros de `0x28` e a
tabela consumida por `sub_0026E70A`, nem um callback válido. Os writers da raiz
e da capacidade continuam fora do subgrafo direto (`1` e `4` shapes globais,
respectivamente), e os `47` writers candidatos do alvo indexado continuam sem
ligação estática provada com essa instância. Os sete callbacks dinâmicos seguem
sem destino final provado.

A suíte Python passou `105/105`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

## Estado verificável — release 2.22

O release 2.22 amplia a auditoria do objeto passado ao consumidor indexado
`sub_0026E70A`: o C gerado contém `6` chamadas diretas inventariadas. Quatro
usam o formato frame-local `ebp - 0x10` (`3` com o helper
`sub_002710F4` observado na mesma função e `1` herdando o frame publicado),
uma usa um registrador carregado de `MEM32(ebp + 0xC)` e uma usa um registrador
com atribuição local zero. No subgrafo iniciado em `0x0024D8BE`, os `2/2`
chamadores observados continuam no formato frame-local publicado.

Isso fecha a classificação da forma dos argumentos, não a semântica do
objeto: não prova que os seis sites sejam alcançados, que o objeto zerado seja
válido ou que qualquer destino dinâmico seja executado. A tabela independente
em `0x00361360` continua sem alias estático provado para o frame, e o alvo do
consumidor continua sendo `MEM32(ebx + esi * 4 + 8)`. Os sete callbacks
dinâmicos permanecem sem destino final provado.

A suíte Python passou `104/104`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise segue estática e não executa o XBE.

## Estado verificável — release 2.21

O release 2.21 aprofunda a cadeia do callback indexado
`sub_00273217/sub_00277AB1 -> sub_0026E70A`. A nova auditoria confirma, em
`2/2` chamadores diretos do subgrafo, que o argumento-objeto é construído como
`ebp - 0x10` depois de `sub_002710F4`; no helper, a forma observada é
`MEM32(XBOX_FS_BASE) = eax` após `eax = ebp - 16`. Isso registra a proveniência
estática do frame publicado em `FS`, não um tipo C nem um callback resolvido.

O mesmo relatório mantém a separação estrutural já observada no consumidor:
`objeto+0x8` fornece a tabela, `objeto+0xC` fornece o índice, o stride do
registro é `0xC`, o guard está em `+0x4` e o alvo dinâmico em `+0x8`. Em
`sub_00277AB1`, a expressão independente da raiz BSS
`MEM32(ecx * 4 + 0x00361360)` também é observada, mas não há escrita direta
que ligue essa raiz ao frame `ebp - 0x10`; portanto
`dynamic_indexed_callback_frame_provenance_table_root_link_status` continua
`not-proven-frame-object-to-indexed-table-root`. Os sete callbacks dinâmicos
seguem sem destino final provado.

A suíte Python passou `103/103`, a prontidão continua `NOT_READY` e a
estimativa conservadora do PoC jogável permanece em aproximadamente
`31% concluído / 69% restante`. A análise continua estática e não executa o
XBE.

### Release 2.20 — layout PRCB zerado no preflight

O release 2.20 audita a dependência de layout que mantém os quatro callbacks
protegidos pelo PRCB fora do preflight. Contra a fonte pinned
`xbox_memory_layout.c`, a auditoria verifica `FAKE_PRCB_VA=0x00761000`, um
bloco zerado de `0x400` bytes e a atribuição de `FS:[0x20]` para esse bloco.
Combinada com a proveniência dos quatro loads
`FS:[0x20] -> +0x250 -> +0x14`, os `0` writers diretos e os guards locais,
essa evidência mostra que os quatro sites começam quiescentes no host
preflight. Ela não resolve os ponteiros dos callbacks, não afirma que um
kernel Xbox real inicialize o campo da mesma forma e não executa código guest.

O relatório continua conservador: `unresolved_dynamic_sites=7`; o novo gate
apenas torna explícita a condição inicial do host. A suíte Python passou
`102/102`, a prontidão continua `NOT_READY` e a estimativa do PoC jogável
permanece em aproximadamente `31% concluído / 69% restante`.

### Release 2.19 — auditoria da tabela `RECOMP_ITAIL`

O release 2.19 fecha uma lacuna de auditoria dos saltos indiretos de cauda.
No subgrafo diretamente alcançável pela entrada `0x0024D8BE`, há `1`
`RECOMP_ITAIL`, em `sub_00272683` (`recomp_0052.c:9827`). A nova checagem
reconhece somente o padrão local de `switch` que compara `_jt` com os braços
traduzidos e lê, sem executar o XBE, a tabela em `0x00272DEA`. As `8/8`
entradas do XBE coincidem exatamente com os `8/8` alvos locais:
`indirect_tail_switch_verified_sites=1`,
`indirect_tail_switch_mismatched_sites=0` e
`indirect_tail_switch_unresolved_sites=0`.

Isso prova a consistência estática dessa tabela de fallback; não prova que um
índice runtime válido seja produzido, que o salto aconteça, nem que qualquer
alvo arbitrário seja seguro. O macro `RECOMP_ITAIL` continua sendo uma rota de
lookup registrada, não uma prova de execução do código guest. Os sete
callbacks dinâmicos seguem sem destino final provado, portanto a prontidão
continua `NOT_READY` e a estimativa conservadora do PoC jogável permanece em
aproximadamente `31% concluído / 69% restante`.

A suíte Python desta release passou `101/101`; os gates de compilação e os
testes CMake da release anterior continuam verdes.

### Release 2.18 — gate da tabela de despacho gerada

O release 2.18 fecha uma lacuna de verificação do despacho indireto: os testes
de suporte continuam exercitando apenas a barreira de segurança com lookup
ausente, mas agora há também um gate que lê a tabela C gerada real. Sem
executar qualquer função traduzida, ele confirma as `15.600` entradas exatas,
um VA de início conhecido, a ausência de um VA interno sem entrada e a
ausência de um VA arbitrário. Isso valida estaticamente a tabela usada pela
rota `VA guest -> função traduzida` de `RECOMP_ICALL_SAFE_AT`; não prova que
callbacks dinâmicos reais sejam inicializados ou alcançados em runtime.

O release 2.18 mantém a proveniência local completa dos sete callbacks
dinâmicos e dos nove chamadores diretos do subgrafo. A auditoria continua
inventariando todos os C gerados: são `dynamic_callback_all_caller_provenance_sites=27`,
dos quais `dynamic_callback_outside_direct_caller_provenance_sites=18` ficam
explicitamente separados do caminho iniciado em `0x0024D8BE`.

O teste Python passou `100/100`; a nova suíte CMake soma `3/3` na biblioteca e
`6/6` no host. A prontidão segue `NOT_READY` e a estimativa conservadora do
PoC jogável permanece em aproximadamente `31% concluído / 69% restante`.

### Release 2.17 — proveniência do writer de `objeto+0x584`

O release 2.17 mantém a proveniência local completa dos sete callbacks
dinâmicos e dos nove chamadores diretos do subgrafo. A auditoria agora também
inventaria todos os C gerados: são `dynamic_callback_all_caller_provenance_sites=27`,
dos quais `dynamic_callback_outside_direct_caller_provenance_sites=18` ficam
explicitamente separados do caminho iniciado em `0x0024D8BE`. Para o callback
indexado `sub_0026E70A`, há seis formas de chamada direta no C gerado: duas no
subgrafo e quatro fora dele. Esse inventário preserva os `PUSH32` e atribuições
lineares, mas não transforma uma chamada fora do subgrafo em alcance de
runtime.

Para os quatro callbacks protegidos pelo PRCB, a auditoria agora separa uma
proveniência específica do cache: `prcb_callback_cache_provenance_sites=4`,
`prcb_callback_cache_provenance_complete_sites=4` e
`prcb_callback_cache_provenance_missing_sites=0`. Em todos os sites, a forma
linear observada é `FS:[0x20] -> +0x250 -> +0x14`, com o mesmo registrador
`eax` preservado entre o carregamento do cache e a leitura do campo de callback;
os quatro também mantêm o guard local já observado. Isso registra somente a
forma estática e a condição local: não resolve o conteúdo do cache, o ponteiro
final, a implementação do callback ou seu comportamento em runtime.

Para os dois slots BSS ainda sem writer global provado
(`runtime_fixed_slot_all_writer_counts` igual a `0` para `0x00360A70` e
`0x00360AA8`), a auditoria agora registra os consumidores e seus chamadores:
`fixed_callback_consumer_provenance_sites=5`, sendo `3` chamadores no subgrafo
direto e `2` fora dele. O slot `0x00360A70` é lido em `sub_00274FB8` e aparece
em quatro chamadas diretas, com um push de argumento em cada shape observado;
duas estão no caminho direto e duas fora. O slot `0x00360AA8` é lido em
`sub_00276E45` e aparece em uma chamada direta sem push explícito. Isso é
evidência estática do consumidor e da forma de entrada, não prova de tipo,
implementação do callback, preenchimento runtime do slot ou segurança de
invocação.

A auditoria também registra o primeiro uso limitado do valor retornado por cada
callback unresolved: `callback_return_use_provenance_sites=9`, cobrindo os sete
callbacks dinâmicos e os dois consumidores BSS. Um retorno é comparado depois
da chamada (`0x00274FC5`, consumidor de `0x00360A70`), dois são sobrescritos
antes de qualquer observação (`0x00252C7D` e `0x00252CF1`) e seis não têm uso
observável antes da próxima transferência de controle. Essa classificação é
somente uma janela sintática pós-chamada; não prova tipo, signedness, ABI ou
semântica do retorno.

Para os serviços kernel, a mesma janela agora cobre todos os `48` sites de
thunk: `kernel_return_use_provenance_sites=48`, com `19` retornos comparados em
controle, `4` lidos/encaminhados, `14` sobrescritos antes de nova observação e
`11` sem uso observável antes da próxima transferência. Essa separação ajuda a
priorizar os serviços cujo resultado influencia o fluxo do jogo, mas continua
sendo apenas evidência do C traduzido; não prova semântica Xbox, equivalência
do bridge host, tipos ou efeitos colaterais.

Os `19/19` targets host selecionados pelos thunks agora têm suas expressões de
retorno textuais inventariadas, totalizando `kernel_return_contract_sites=66`:
`26` retornos de sucesso/zero, `29` símbolos de status, `4`
`STATUS_NOT_IMPLEMENTED`, `1` expressão delegada, `4` expressões gerais e `2`
retornos vazios. Essa evidência conecta o call site ao corpo host e expõe áreas
prioritárias, mas não prova que os códigos de status, ponteiros, efeitos
colaterais ou regras de ownership sejam compatíveis com o Xbox.

A checagem seguinte atravessa a fronteira entre o adaptador e o corpo do
bridge: `kernel_bridge_body_status=verified-source-body-stack-index-coverage-with-explicit-stub-exception`
para os `48` sites dos `19` targets usados. Todos os `19/19` corpos
`bridge_*` foram localizados; em `47/48` sites, os índices `STACK_ARG`
observados ficam dentro da contagem ABI esperada e da janela de argumentos
empurrados pelo caller. O único caso sem `STACK_ARG` identificado é o ordinal
178 (`bridge_MmPersistContiguousMemory`); a análise agora o classifica como
`kernel_bridge_body_explicit_no_arg_stub_sites=1`, porque o corpo contém um
`No-op stub` explícito e o host correspondente também é um stub. Assim, ele
não é contado como uma lacuna de leitura ABI (`kernel_bridge_body_stack_index_gap_sites=0`),
mas continua sendo uma lacuna semântica e não uma aprovação da chamada. A fonte também mostra
atribuição a `g_eax` em `18/19` targets, `XBOX_TO_NATIVE` em `8/19` e
`BRIDGE_MEM32` em `9/19`. Essa checagem registra somente formas textuais do
corpo host e a comparação estática de janelas; não prova tipos, convenção
completa, tradução de ponteiros, semântica Xbox ou efeitos colaterais.

A auditoria de disposição de retorno dos mesmos bridges também fecha o
contrato de publicação de resultado: `kernel_bridge_body_return_disposition_status=verified-source-return-disposition-coverage`,
com `kernel_bridge_body_explicit_g_eax_return_sites=18`, uma chamada terminal
conhecida em `xbox_HalReturnToFirmware`
(`kernel_bridge_body_terminal_host_return_sites=1`) e
`kernel_bridge_body_return_disposition_missing_sites=0`. Isso confirma apenas
que nenhum dos `19` corpos observados deixa o `g_eax` sem uma forma estática de
publicação ou sem o caminho terminal identificado; não prova que o valor
publicado ou a terminação sejam compatíveis com o Xbox.

A auditoria agora cruza também o bridge com a assinatura da implementação
host: `kernel_implementation_signature_status=verified-source-signature-parameter-counts`,
com `kernel_implementation_signature_match_targets=19/19`,
`kernel_implementation_signature_mismatch_targets=0` e
`kernel_implementation_signature_match_sites=48`. Os parâmetros foram
contados nas definições `xbox_*` locais e comparados à quantidade declarada no
`stdcall_args_for_ordinal`; isso é evidência textual de cardinalidade, não
prova de tipos, convenção completa, tradução de ponteiros ou semântica Xbox.

A checagem seguinte classifica a delegação efetiva de cada bridge para a
implementação host: `kernel_bridge_delegation_status=verified-source-bridge-delegation-classification`,
com `kernel_bridge_delegation_targets=19`. São `8`
`direct-implementation-delegation`, `4` `bridge-local-adaptation`, `5`
`bridge-local-no-direct-host-call` e `2`
`bridge-local-adaptation-over-host-stub`. Essa separação distingue cardinalidade
ABI correta de chamada direta, adaptação local e ausência de delegação para o
`xbox_*` correspondente; continua sendo uma classificação textual da fonte e
não prova equivalência semântica, tipos, efeitos colaterais ou compatibilidade
Xbox.

A auditoria também compara os `STACK_ARG` usados pelo corpo de cada bridge com
a contagem de argumentos declarada para o ordinal: `kernel_bridge_argument_use_status=verified-source-argument-read-classification`,
com `kernel_bridge_argument_use_targets=19`. Em `17/19` targets todos os slots
esperados são lidos; `PsCreateSystemThreadEx` lê somente `4/10` slots e fornece
os demais valores localmente, enquanto `MmPersistContiguousMemory` permanece o
único `explicit-stub-without-argument-read`. Não há target com índice fora da
contagem esperada (`kernel_bridge_argument_use_outside_expected_targets=0`) nem
corpo ausente. Isso mede somente consumo textual de argumentos: uma leitura
completa não prova tipos, valores, convenção completa ou semântica Xbox, e uma
leitura parcial pode ser uma adaptação deliberada ou uma lacuna.

A origem BSS da família de registros indexados também foi inventariada: a raiz da tabela em
`0x00361360` e a capacidade em `0x0036135C` têm
`dynamic_indexed_callback_table_status=verified-static-indexed-table-root-provenance`.
O C gerado mostra `1` write da raiz, `4` writes de capacidade (o primeiro
define `0x20`) e `3` writes de registros indexados; todos estão fora do
subgrafo diretamente alcançável (`0` writes diretos). Essa evidência conecta a
rotina de construção da família de registros, mas não prova que ela seja o
contêiner recebido pelo consumidor `sub_0026E70A`; os valores vêm de
registradores e não permitem recuperar com segurança os callbacks finais ou
sua identidade runtime.

A release 2.17 preserva e documenta explicitamente o layout do consumidor indexado da raiz
BSS: `dynamic_indexed_callback_consumer_layout_status=verified-indexed-callback-consumer-layout`.
Em `sub_0026E70A`, o objeto é carregado de `esp+0x20`, o contêiner é lido de
`objeto+8`, o índice de `objeto+0xC` é transformado por `i = i + i*2`, e o
registro resultante tem stride estático de `12` bytes. O guard lê
`registro+4` e o alvo do `icall` lê `registro+8` no site `0x0026E75E`.
O campo `objeto+8` não é ligado estaticamente à raiz `0x00361360` pelo corpo
do consumidor (`dynamic_indexed_callback_consumer_layout_table_root_link_status=not-proven-from-consumer-body`),
portanto os writers da raiz não são contados como inicializadores provados do
callback dinâmico. Essa distinção reduz uma hipótese anterior sem resolver o
ponteiro runtime.

Na release 2.16, a mesma correlação de campos foi aplicada a todos os seis
chamadores observados de `sub_0026E70A`, não apenas aos dois dentro do
subgrafo direto: `dynamic_callback_container_field_all_provenance_sites=6` e
`dynamic_callback_container_field_all_write_sites=5`. Quatro chamadores ficam
fora do subgrafo e fornecem três dessas escritas; nas quatro chamadas com
ponteiro local `ebp-0x10`, não há write local provado para o campo `+8`, e as
cinco escritas encontradas no campo `+0xC` vêm de três registradores e duas
constantes. As duas chamadas restantes têm expressões de objeto não resolvidas
(`MEM32(ebp+0xC)` e `0`), portanto essa ampliação não prova identidade de
objeto, preenchimento do contêiner ou callback final.

Para o terceiro callback dinâmico, a release 2.17 também registra a origem
local imediata dos writers fora do subgrafo: são `8` writes de
`objeto+0x584`, com `4` contextos completos após a última chamada direta,
`3` contextos parciais e `1` ainda não resolvido. Em `3` writes aparece a
mesma forma `base = MEM32(ebp-0x1C)` e `valor = MEM32(ebp-0x54)` no C gerado
(equivalente aos offsets locais `-28` e `-84`); as fontes imediatas dos valores
foram classificadas como `4` leituras de memória, `3` constantes e `1`
desconhecida. Isso melhora a proveniência do campo, mas não prova dominância,
identidade do objeto, que o valor seja callable ou que qualquer writer esteja
no caminho de runtime do callback.

A checagem do callback indexado continua em
`dynamic_callback_container_field_provenance_sites=2`: nos dois chamadores do
subgrafo direto, o argumento de objeto tem a forma local `ebp - 16`, os campos
`+8` e `+0xC` são carregados pelo callback e há uma escrita observada para o
campo mapeado `ebp - 4` (`+0xC`), com valor vindo de registrador; não há escrita
local provada para `ebp - 8` (`+8`). A checagem do campo de objeto `+0x584`
registra `dynamic_callback_object_field_provenance_sites=1` e
`dynamic_callback_object_field_write_sites=0`: `sub_00251CD6` passa `edi`,
carregado de `MEM32(ebp + 8)`, mas não há uma escrita provada de
`MEM32(edi + 0x584)=...` antes da chamada. Tudo isso é evidência de forma,
argumento e escrita linear; não prova dominância de fluxo, identidade de
objeto, tipo, ABI, inicialização em runtime ou destino final do callback.

O piloto read-only do REA também permanece documentado: o MCP local respondeu
com `170/170` ferramentas e `6/6` prompts, mas não há Hopper, Ghidra ou IDA
local; ao receber o `default.xbe`, `rea analyze` retornou
`target_unavailable / unsupported binary format`. Portanto o REA não acrescenta
evidência ao XBE neste release e não foi usado para criar bindings. A suíte
Python passou `100/100`; a prontidão segue `NOT_READY` e a estimativa conservadora
do PoC jogável permanece em aproximadamente `31% concluído / 69% restante`.

## Histórico verificável

O último marco publicado é a etapa 1.97 (14/10/2026): a auditoria estática do caminho iniciado em `0x0024D8BE` continua cobrindo 62 chamadas indiretas em 32 das 78 funções diretamente alcançáveis. Quatro dos sete alvos não resolvidos foram identificados como callbacks protegidos pelo padrão `FS:[0x20] -> +0x250` e agora o host preflight verifica que esse cache do PRCB começa em zero; a análise também não encontrou nenhuma escrita direta provada nesse campo dentro do subgrafo gerado alcançável (`prcb_cache_direct_writer_sites=0`); a checagem agora cobre tanto `MEM32(base + 0x250)=...` quanto aliases `base + 0x250` calculados em registrador antes de `MEM32(registrador)=...`, sem encontrar writer novo no subgrafo direto. Isso demonstra apenas que o código local do jogo não fornece um inicializador provado para o cache; não prova que o kernel não o preencha em runtime. Os outros três permanecem callbacks/tabelas dinâmicos sem destino provado, agora separados por proveniência em alvo de memória, campo de objeto em `+0x584` e registro indexado `base + índice*12 + 8`. A auditoria de writers não encontrou nenhuma escrita de shape provada para essas três fontes no subgrafo diretamente alcançável (`dynamic_target_direct_writer_sites=0`, com contagem zero para alvo de memória, campo de objeto e registro indexado); isso elimina apenas inicializadores locais simples, não resolve os ponteiros nem substitui a origem runtime dos objetos. A varredura de todo o C gerado agora também rastreia aliases lineares em que o endereço é calculado em um registrador antes de `MEM32(registrador)`: encontrou 8 writers de shape para campo de objeto e 47 para registro indexado, todos fora do subgrafo direto (`dynamic_target_outside_direct_writer_sites=55`, sendo 8 de objeto e 47 indexados). A classificação dos valores nesses 55 sites encontrou 7 registradores e 1 constante nos 8 writers de objeto, e 29 registradores, 1 constante e 17 expressões computadas nos 47 writers indexados (`dynamic_target_outside_direct_writer_value_kinds`); isso continua sendo evidência de forma/valor atribuído, não prova de que o endereço-base seja a mesma instância consumida pelos callbacks. Os 55 sites adicionais são somente evidência de forma/alias; todos permanecem candidatos de fases não alcançadas e não provam que inicializem os mesmos objetos ou registros dos callbacks. A auditoria também preserva a forma de stack dos sete callbacks dinâmicos: `dynamic_call_stack_shape_sites=7` e `dynamic_call_stack_shape_missing_sites=0`, com 16, 8, 8, 12, 8, 8 e 0 bytes empilhados nos sites `0x0024D3B1`, `0x0024D4C7`, `0x00250612`, `0x002512A4`, `0x00252C7D`, `0x00252CF1` e `0x0026E75E`; isso é evidência de pushes observados, não prova de tipos, ABI ou destino. Uma nova checagem de fluxo registra `dynamic_conditional_skip_branch_sites=6`: há um salto local que pode pular a chamada nos quatro callbacks PRCB, no campo de objeto `+0x584` e no registro indexado; para o callback de memória em `0x0024D3B1`, a auditoria também observa `dynamic_conditional_nonzero_guard_sites=1`, com `CMP_NE(_fa,_fb)` levando ao call apenas quando `MEM32(ecx)` é diferente de zero. O conjunto desses dois padrões cobre os sete callbacks dinâmicos (`dynamic_conditional_guard_sites=7`, `dynamic_conditional_guard_missing_sites=0`); isso prova somente uma condição local observada antes de cada chamada, não que os destinos sejam válidos ou que os guards sejam sempre satisfeitos. O alvo de memória em `0x0030BC94–0x0030BC98` não tem esse bypass local. Para o slot global percorrido por `sub_0024D373`, a auditoria recupera conservadoramente o intervalo `0x0030BC94–0x0030BC98` e confirma que os dois dwords estão zerados na imagem; isso ainda não prova qual rotina poderá preenchê-los em runtime. Dos três slots BSS verificados como zero antes de qualquer guest call, `0x00360AB4` agora tem inicialização constante demonstrada: `sub_00277509` escreve `0x002774F9` antes do call em `0x00277532`, e `0x002774F9` existe no código traduzido. O inventário global, agora cobrindo tanto `MEM32(endereço)=qualquer valor` (inclusive constantes) quanto `registrador=slot; MEM32(registrador)=...`, confirma 1 writer para `0x00360AB4` e nenhum writer absoluto ou derivado por registrador para `0x00360A70` ou `0x00360AA8` (`runtime_fixed_slot_all_writer_counts`); esses dois slots continuam sem writer provado em qualquer função gerada. Permanecem 48 sites em 19 slots únicos da tabela de 128 thunks do kernel, 4 ponteiros fixos para funções conhecidas e 1 ponteiro BSS com inicializador de função provado. Quando a fonte local `kernel_thunks.c` é fornecida à auditoria, os 48 sites têm cases explícitos no resolver host (`kernel_resolver_case_sites=48`, `kernel_resolver_missing_sites=0`); o `kernel_bridge.c` também tem adaptador explícito para os 48 sites (`kernel_bridge_case_sites=48`, `kernel_bridge_missing_sites=0`), e a comparação de stack-shape confirma a quantidade de argumentos empilhados em todos os 48 sites (`kernel_abi_argument_count_sites=48`, `kernel_abi_argument_count_mismatch_sites=0`). Uma segunda checagem localiza definições de fonte para os 19 ordinais únicos usados (`kernel_implementation_defined_targets=19/19`). Uma auditoria adicional percorre o corpo dessas definições e encontra algum marcador textual em 5 dos 19 targets (`kernel_implementation_body_marker_target_count=5`): apenas 2 têm `stub`/`no-op` explícito (`kernel_implementation_explicit_stub_or_noop_target_count=2`, `xbox_MmPersistContiguousMemory` e `xbox_NtOpenSymbolicLinkObject`). Esses dois targets aparecem efetivamente no caminho direto, nos ordinais 178 e 203, nos sites `0x0024D3F0` (12 bytes) e `0x00250244` (8 bytes) (`kernel_implementation_explicit_stub_or_noop_call_sites`); no ordinal 178, a análise recupera `arg[1]=0x00001000` de uma atribuição literal a `esi` e `arg[2]=0x00000001` de push literal, enquanto o ordinal 203 não tem argumento constante recuperável; isso é evidência estática de valores empilhados, não prova de tipos, conteúdo válido ou semântica do stub. No mesmo `sub_0025021A`, a ordem observada é ordinal 203 (open) -> 215 (query) -> 187 (close), registrada como `kernel_symbolic_link_lifecycle_status=open-query-close-order-observed`; isso prova apenas a ordem local, não a identidade do handle nem a compatibilidade completa de objeto, caminho, buffer ou lifetime. No call site de ordinal 203, a análise também recupera da memória do XBE a `XBOX_ANSI_STRING` em `0x00325874`, com texto `\\??\\D:` (`kernel_symbolic_link_guest_path_sites=1`), e encontra no host o alvo literal `\\Device\\CdRom0` (`kernel_symbolic_link_host_query_target`); a ponte host agora valida estaticamente que o ordinal 203 aceita o caminho observado `\\??\\D:`, grava o handle sintético `0xDEAD0001`, exige esse handle no ordinal 215 e mantém o mesmo alvo (`kernel_symbolic_link_host_bridge_status=bridge-path-and-handle-validation-observed`); isso demonstra uma correspondência estática mais estreita para a unidade `D:`, mas não valida genericamente o Object Manager, o runtime ou todo o gerenciamento de handles. Enquanto 3 têm somente o token `STATUS_NOT_IMPLEMENTED` (`kernel_implementation_status_not_implemented_token_target_count=3`), que pode ser um case default ou um mapeamento de status; isso é evidência de risco semântico do host, não prova de que cada call site observado entre no trecho marcado. Para os três call sites dos ordinais 211/226, a auditoria também recupera os argumentos constantes da stack e observa as classes 34, 14 e 14 (`XboxFileNetworkOpenInformation`, `XboxFilePositionInformation`, `XboxFilePositionInformation`), todas presentes nos `case` das implementações host correspondentes (`kernel_file_info_class_sites=3`, `kernel_file_info_class_covered_sites=3`, `kernel_file_info_class_missing_sites=0`). Essa checagem comprova apenas cobertura estática do caso observado; não prova que estruturas, handles, status codes ou efeitos colaterais estejam totalmente compatíveis com Xbox. O conjunto anterior comprova apenas cobertura do resolver, presença do adaptador de ABI, compatibilidade da contagem de argumentos e presença de corpo no host: não aprova tipos, valores, efeitos colaterais, semântica Xbox ou adequação de qualquer aproximação/stub ao call site. Nenhum dos 7 callbacks dinâmicos foi declarado resolvido. A auditoria mantém `OUT DX,AL` em `0x00298030` e `SGDT` em `0x00253FD8` fora do lowering; o relatório de alcance direto agora preserva owner, arquivo/linha e texto dos dois sites (`unimplemented_sites=2`, `unimplemented_sites_in_directly_reachable_function_scopes=0`), confirmando que nenhum deles pertence às 78 funções diretamente alcançáveis. Isso não aprova a semântica de hardware nem a execução; imports e serviços continuam sem inferência. Contra o XBE local e diretórios isolados, os preflights estático e runtime confirmaram o mapa de memória, os guards de PRCB, os dois slots de callback em memória `0x0030BC94/0x0030BC98`, os três slots BSS, o dispatch, o entry point `0x0024D8BE` e `128/128` thunks resolvidos (`128 bridged`, `0 stub`), encerrando antes de qualquer instrução guest. A suíte de suporte do host agora também exerce os caminhos `RECOMP_ICALL_SAFE_AT` de alvo fora do código, alvo sem resolver e `RECOMP_ICALL_SAFE_AT_CC`, verificando retorno zero, ausência de chamada de ponteiro e o contrato de `g_esp`; esse teste não liga o dispatch gerado nem executa código guest. A geração estática permanece em `15.600/15.600` funções, zero falhas e `1.775.604` linhas em 63 arquivos de funções; o CMake recompilou 64 unidades C. A biblioteca passou `2/2` e o host passou `5/5`, incluindo rejeição de XBE ausente, `--help`, suporte runtime, estado `CLI`/`STI`, operações packed-word e rejeição de `RECOMP_VBLANK` antes de qualquer acesso ao XBE. `OUT DX,AL` e `SGDT` continuam sem lowering por dependerem de efeitos de hardware/GDT não comprovados. O XBE não foi executado; o runtime preflight é somente validação de preparação e a prontidão permanece `NOT_READY`. A estimativa conservadora para um possível PoC jogável permanece aproximadamente em 31% concluído e 69% restante; ainda faltam principalmente os 3 callbacks/tabelas dinâmicos, os quatro callbacks PRCB quando o cache for populado, os dois slots BSS sem writer provado, semântica real dos serviços kernel, execução do entry point, entrega de interrupções, compatibilidade de hardware, rendering, assets, gameplay, animação e áudio.

## Conteúdo

- [`native_recomp/`](native_recomp): integração CMake, suporte nativo e patches incrementais para a tradução XboxRecomp.
- [`tools/`](tools): utilitários de análise e testes de regressão usados pelo fluxo de recompilação.
- [`fifa_pc/`](fifa_pc) e [`pc_compat/`](pc_compat): ferramentas e protótipos auxiliares de investigação; não são o jogo recompilado nem substituem sua lógica original.

Os fontes C gerados, builds, XBE, ISO e assets extraídos são insumos locais e não fazem parte deste repositório. A ISO original deve permanecer somente para leitura. Use apenas uma cópia de jogo obtida legalmente.

## Build e validação

A compilação nativa requer uma geração local do XboxRecomp para preencher `RECOMP_GENERATED_DIR`; os arquivos gerados não são versionados. Exemplo para o alvo da biblioteca:

```powershell
cmake -S native_recomp -B artifacts/fut-street2-native/build -G Ninja `
  -DRECOMP_GENERATED_DIR="C:/caminho/local/xboxrecomp/gen"
cmake --build artifacts/fut-street2-native/build --config Release --parallel
ctest --test-dir artifacts/fut-street2-native/build -C Release --output-on-failure
```

Depois de compilar o host opcional, a fronteira de preflight pode ser
verificada sem executar o entry point recompilado:

```powershell
python tools/analysis/recomp_host_preflight_evidence_gate.py `
  --executable C:/caminho/local/fut_street_2_recompiled_app.exe `
  --xbe C:/caminho/local/game_files/default.xbe `
  --mode static

python tools/analysis/recomp_host_preflight_evidence_gate.py `
  --executable C:/caminho/local/fut_street_2_recompiled_app.exe `
  --xbe C:/caminho/local/game_files/default.xbe `
  --mode runtime `
  --data-directory C:/caminho/local/game-data `
  --save-directory C:/caminho/local/save-data
```

Esse gate usa somente a opção explícita `--preflight`, exige
`guest_execution=none` e nunca substitui um teste de boot do jogo.

O projeto auxiliar .NET pode ser compilado e testado sem a ISO:

```powershell
dotnet build fifa_pc/FifaStreetPc.csproj -c Release
dotnet fifa_pc/bin/Release/net8.0/FifaStreetPc.dll self-test
```

Com `functions.json` e a geração local disponíveis, a cobertura estrutural direta do entry point pode ser auditada sem executar o XBE:

```powershell
python tools/analysis/recomp_entry_call_graph_audit.py `
  --functions C:/caminho/local/disasm/functions.json `
  --generated C:/caminho/local/xboxrecomp/gen
```

Essa auditoria é limitada a chamadas diretas recuperadas estaticamente; ela não aprova chamadas indiretas, callbacks, imports, compatibilidade de hardware ou execução do jogo.

Para fechar a regressão de cobertura do artefato integral, forneça também o
`summary.json` da mesma geração e o diretório `asm` recuperado:

```powershell
python tools/analysis/recomp_translation_coverage_gate.py `
  --functions C:/caminho/local/disasm/functions.json `
  --generated C:/caminho/local/xboxrecomp/gen `
  --summary C:/caminho/local/xboxrecomp/gen/summary.json `
  --asm-dir C:/caminho/local/disasm/asm
```

Esse gate apenas junta os três controles e mantém os sites sem lowering e a
semântica de hardware explicitamente não provados.

Para classificar os sites indiretos do mesmo subgrafo, forneça também o XBE local:

```powershell
python tools/analysis/recomp_indirect_call_audit.py `
  --functions C:/caminho/local/disasm/functions.json `
  --generated C:/caminho/local/xboxrecomp/gen `
  --xbe C:/caminho/local/game_files/default.xbe
```

Para transformar essa classificação em um gate de regressão do artefato
integral, use:

```powershell
python tools/analysis/recomp_dynamic_callback_evidence_gate.py `
  --functions C:/caminho/local/disasm/functions.json `
  --generated C:/caminho/local/xboxrecomp/gen `
  --xbe C:/caminho/local/game_files/default.xbe `
  --kernel-source C:/caminho/local/xboxrecomp/src/kernel/kernel_thunks.c
```

Esse gate é específico dos sete sites atualmente auditados e mantém a
distinção entre evidência estática e inicialização de runtime; saída aprovada
não significa que qualquer callback esteja resolvido ou executável.

A cobertura dos serviços kernel usados por esses sites pode ser fechada com:

```powershell
python tools/analysis/recomp_kernel_service_contract_gate.py `
  --functions C:/caminho/local/disasm/functions.json `
  --generated C:/caminho/local/xboxrecomp/gen `
  --xbe C:/caminho/local/game_files/default.xbe `
  --kernel-source C:/caminho/local/xboxrecomp/src/kernel/kernel_thunks.c
```

O gate exige o resolver, o bridge, as assinaturas e os marcadores de risco
observados; ele mantém `host_service_semantics=not-proven` e não executa o
XBE.

O inventário complementar dos slots BSS fixos pode ser repetido com:

```powershell
python tools/analysis/recomp_fixed_callback_slot_evidence_gate.py `
  --functions C:/caminho/local/disasm/functions.json `
  --generated C:/caminho/local/xboxrecomp/gen `
  --xbe C:/caminho/local/game_files/default.xbe `
  --kernel-source C:/caminho/local/xboxrecomp/src/kernel/kernel_thunks.c
```

Ele mantém a ausência de writer provado separada da resolução do único slot
que tem inicializador de função observado.

O domínio estático do seletor de buffer EAGL pode ser conferido com:

```powershell
python tools/analysis/eagl_dynamic_buffer_selector_control.py `
  --asm C:/caminho/local/disasm/asm/text.asm `
  --xbe C:/caminho/local/game_files/default.xbe
```

O resultado mantém `writer_sites=5`, `consumer_sites=33`,
`runtime_selected_buffer=not-proven` e `rendering_binding=not-proven`; ele
apenas impede assumir que `0x00345BCC` seja constante ou ignorar seus
consumidores estáticos conhecidos. No release 2.50, a saída adicional inclui
`consumer_function_count=31` e `consumer_function_envelope=0x001A3E00-0x001A4B40`.
No release 2.51, ela também inclui
`consumer_anchor_counts=0x3458d0:33,0x3458d4:33`.
No release 2.52, inclui ainda `consumer_shape_counts`, com `28` composições
genéricas e cinco formas especiais downstream.

A consistência entre os relatórios estáticos da cadeia EAGL pode ser conferida
com:

```powershell
python tools/analysis/eagl_static_render_chain_control.py `
  --draw-summary C:/caminho/local/eagl-indexed-draw-source-control-summary.tsv `
  --parameter-summary C:/caminho/local/eagl-render-parameter-static-control-summary.tsv `
  --material-summary C:/caminho/local/eagl-material-declaration-control-summary.tsv `
  --model-summary C:/caminho/local/eagl-model-material-reference-control-summary.tsv
```

Esse gate confere identidade, hashes, contagens e joins entre os quatro
relatórios, mas mantém `runtime_selector=unresolved`,
`full_rendering_binding=not-proven` e `original_semantics=not-claimed`.

Os efeitos dos helpers do seletor podem ser conferidos com:

```powershell
python tools/analysis/eagl_selector_helper_effect_control.py `
  --asm C:/caminho/local/disasm/asm/text.asm `
  --xbe C:/caminho/local/game_files/default.xbe
```

O gate verifica somente formas estáticas e não executa o XBE.

O contexto dos dois opcodes ainda não traduzidos pode ser conferido com:

```powershell
python tools/analysis/recomp_unimplemented_context_control.py `
  --asm-dir C:/caminho/local/disasm/asm
```

O gate exige os owners, instruções e fragmentos locais observados, mas
mantém `hardware_semantics=not-proven` e os dois sites sem lowering.

Opcionalmente, acrescente `--kernel-source` apontando para
`C:/caminho/local/xboxrecomp/src/kernel/kernel_thunks.c` para conferir se os
ordinais de thunk usados pelo subgrafo têm cases explícitos no resolver do
host. Isso mede cobertura de ponteiro do resolver; não aprova ABI, efeitos
colaterais ou semântica Xbox dos serviços.

Somente aliases lineares demonstrados e inicializadores constantes de slots BSS são classificados; os 7 alvos dinâmicos continuam explicitamente não resolvidos, embora o relatório preserve a forma de proveniência de cada família e os aliases de endereço usados pelos writers candidatos. A auditoria de stack shape dos callbacks é somente contagem de pushes observados e não constitui prova de tipos, ABI, execução ou fidelidade dos serviços Xbox.

Esses comandos validam compilação e testes do código-fonte, não os gates do jogo original. Não execute nem distribua o XBE ou assets proprietários.

## Licença e créditos

Este repositório não inclui uma licença de redistribuição do jogo nem reivindica direitos sobre FIFA Street 2. Consulte as licenças do XboxRecomp e de quaisquer dependências antes de redistribuí-las. O trabalho específico deste projeto ainda não tem uma licença open-source definida.
