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

## Abordagem

- Usar o [XboxRecomp](https://github.com/sp00nznet/xboxrecomp) como base para análise e tradução estática do código Xbox x86 para C, com uma pilha de patches versionada em [`native_recomp/patches`](native_recomp/patches).
- Compilar o código traduzido para o host e implementar apenas compatibilidade sustentada por evidências do executável e dos call sites. Stubs, hipóteses e instruções ainda não traduzidas não contam como funcionalidades implementadas.
- Manter fora do caminho de execução qualquer emulação de CPU, GPU ou APU. Não incorporar código ou assets proprietários do jogo.

## Estado verificável — release 2.12

O release 2.12 mantém a proveniência local completa dos sete callbacks
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
Python passou `96/96`; a prontidão segue `NOT_READY` e a estimativa conservadora
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

Para classificar os sites indiretos do mesmo subgrafo, forneça também o XBE local:

```powershell
python tools/analysis/recomp_indirect_call_audit.py `
  --functions C:/caminho/local/disasm/functions.json `
  --generated C:/caminho/local/xboxrecomp/gen `
  --xbe C:/caminho/local/game_files/default.xbe
```

Opcionalmente, acrescente `--kernel-source` apontando para
`C:/caminho/local/xboxrecomp/src/kernel/kernel_thunks.c` para conferir se os
ordinais de thunk usados pelo subgrafo têm cases explícitos no resolver do
host. Isso mede cobertura de ponteiro do resolver; não aprova ABI, efeitos
colaterais ou semântica Xbox dos serviços.

Somente aliases lineares demonstrados e inicializadores constantes de slots BSS são classificados; os 7 alvos dinâmicos continuam explicitamente não resolvidos, embora o relatório preserve a forma de proveniência de cada família e os aliases de endereço usados pelos writers candidatos. A auditoria de stack shape dos callbacks é somente contagem de pushes observados e não constitui prova de tipos, ABI, execução ou fidelidade dos serviços Xbox.

Esses comandos validam compilação e testes do código-fonte, não os gates do jogo original. Não execute nem distribua o XBE ou assets proprietários.

## Licença e créditos

Este repositório não inclui uma licença de redistribuição do jogo nem reivindica direitos sobre FIFA Street 2. Consulte as licenças do XboxRecomp e de quaisquer dependências antes de redistribuí-las. O trabalho específico deste projeto ainda não tem uma licença open-source definida.
