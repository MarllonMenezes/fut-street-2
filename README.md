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

## Estado verificável

O último marco publicado é a etapa 1.83 (14/10/2026): a auditoria estática do caminho iniciado em `0x0024D8BE` continua cobrindo 62 chamadas indiretas em 32 das 78 funções diretamente alcançáveis. Quatro dos sete alvos não resolvidos foram identificados como callbacks protegidos pelo padrão `FS:[0x20] -> +0x250` e agora o host preflight verifica que esse cache do PRCB começa em zero; a análise também não encontrou nenhuma escrita direta provada nesse campo dentro do subgrafo gerado alcançável (`prcb_cache_direct_writer_sites=0`). Isso demonstra apenas que o código local do jogo não fornece um inicializador provado para o cache; não prova que o kernel não o preencha em runtime. Os outros três permanecem callbacks/tabelas dinâmicos sem destino provado, agora separados por proveniência em alvo de memória, campo de objeto em `+0x584` e registro indexado `base + índice*12 + 8`. A auditoria de writers não encontrou nenhuma escrita de shape provada para essas três fontes no subgrafo diretamente alcançável (`dynamic_target_direct_writer_sites=0`, com contagem zero para alvo de memória, campo de objeto e registro indexado); isso elimina apenas inicializadores locais simples, não resolve os ponteiros nem substitui a origem runtime dos objetos. A varredura de todo o C gerado agora também rastreia aliases lineares em que o endereço é calculado em um registrador antes de `MEM32(registrador)`: encontrou 8 writers de shape para campo de objeto e 47 para registro indexado, todos fora do subgrafo direto (`dynamic_target_outside_direct_writer_sites=55`, sendo 8 de objeto e 47 indexados). Os 9 casos adicionais são somente evidência de forma/alias; todos permanecem candidatos de fases não alcançadas e não provam que inicializem os mesmos objetos ou registros dos callbacks. A auditoria também preserva a forma de stack dos sete callbacks dinâmicos: `dynamic_call_stack_shape_sites=7` e `dynamic_call_stack_shape_missing_sites=0`, com 16, 8, 8, 12, 8, 8 e 0 bytes empilhados nos sites `0x0024D3B1`, `0x0024D4C7`, `0x00250612`, `0x002512A4`, `0x00252C7D`, `0x00252CF1` e `0x0026E75E`; isso é evidência de pushes observados, não prova de tipos, ABI ou destino. Uma nova checagem de fluxo registra `dynamic_conditional_skip_branch_sites=6`: há um salto local que pode pular a chamada nos quatro callbacks PRCB, no campo de objeto `+0x584` e no registro indexado; essa evidência não resolve o destino nem demonstra que a condição seja sempre nula. O alvo de memória em `0x0030BC94–0x0030BC98` não tem esse bypass local. Para o slot global percorrido por `sub_0024D373`, a auditoria recupera conservadoramente o intervalo `0x0030BC94–0x0030BC98` e confirma que os dois dwords estão zerados na imagem; isso ainda não prova qual rotina poderá preenchê-los em runtime. Dos três slots BSS verificados como zero antes de qualquer guest call, `0x00360AB4` agora tem inicialização constante demonstrada: `sub_00277509` escreve `0x002774F9` antes do call em `0x00277532`, e `0x002774F9` existe no código traduzido. O inventário global, agora cobrindo tanto `MEM32(endereço)=constante` quanto `registrador=slot; MEM32(registrador)=...`, confirma 1 writer para `0x00360AB4` e nenhum writer absoluto ou derivado por registrador para `0x00360A70` ou `0x00360AA8` (`runtime_fixed_slot_all_writer_counts`); esses dois slots continuam sem writer provado em qualquer função gerada. Permanecem 48 sites em 19 slots únicos da tabela de 128 thunks do kernel, 4 ponteiros fixos para funções conhecidas e 1 ponteiro BSS com inicializador de função provado. Quando a fonte local `kernel_thunks.c` é fornecida à auditoria, os 48 sites têm cases explícitos no resolver host (`kernel_resolver_case_sites=48`, `kernel_resolver_missing_sites=0`); o `kernel_bridge.c` também tem adaptador explícito para os 48 sites (`kernel_bridge_case_sites=48`, `kernel_bridge_missing_sites=0`), e a comparação de stack-shape confirma a quantidade de argumentos empilhados em todos os 48 sites (`kernel_abi_argument_count_sites=48`, `kernel_abi_argument_count_mismatch_sites=0`). Uma segunda checagem localiza definições de fonte para os 19 ordinais únicos usados (`kernel_implementation_defined_targets=19/19`). Isso comprova apenas cobertura do resolver, presença do adaptador de ABI, compatibilidade da contagem de argumentos e presença de corpo no host: não aprova tipos, valores, efeitos colaterais, semântica Xbox ou adequação de qualquer aproximação/stub ao call site. Nenhum dos 7 callbacks dinâmicos foi declarado resolvido. A auditoria mantém `OUT DX,AL` em `0x00298030` e `SGDT` em `0x00253FD8` fora do lowering, e não infere imports, semântica dos serviços ou execução. Contra o XBE local e diretórios isolados, os preflights estático e runtime confirmaram o mapa de memória, os guards de PRCB, os dois slots de callback em memória `0x0030BC94/0x0030BC98`, os três slots BSS, o dispatch, o entry point `0x0024D8BE` e `128/128` thunks resolvidos (`128 bridged`, `0 stub`), encerrando antes de qualquer instrução guest. A suíte de suporte do host agora também exerce os caminhos `RECOMP_ICALL_SAFE_AT` de alvo fora do código, alvo sem resolver e `RECOMP_ICALL_SAFE_AT_CC`, verificando retorno zero, ausência de chamada de ponteiro e o contrato de `g_esp`; esse teste não liga o dispatch gerado nem executa código guest. A geração estática permanece em `15.600/15.600` funções, zero falhas e `1.775.604` linhas em 63 arquivos de funções; o CMake recompilou 64 unidades C. A biblioteca passou `2/2` e o host passou `5/5`, incluindo rejeição de XBE ausente, `--help`, suporte runtime, estado `CLI`/`STI`, operações packed-word e rejeição de `RECOMP_VBLANK` antes de qualquer acesso ao XBE. `OUT DX,AL` e `SGDT` continuam sem lowering por dependerem de efeitos de hardware/GDT não comprovados. O XBE não foi executado; o runtime preflight é somente validação de preparação e a prontidão permanece `NOT_READY`. A estimativa conservadora para um possível PoC jogável permanece aproximadamente em 31% concluído e 69% restante; ainda faltam principalmente os 3 callbacks/tabelas dinâmicos, os quatro callbacks PRCB quando o cache for populado, os dois slots BSS sem writer provado, semântica real dos serviços kernel, execução do entry point, entrega de interrupções, compatibilidade de hardware, rendering, assets, gameplay, animação e áudio.

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
