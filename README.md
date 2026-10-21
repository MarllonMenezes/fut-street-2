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

## Estado verificável — 21 de outubro de 2026

**NOT_READY — ainda não é um jogo recompilado jogável.** O host nativo foi compilado e executado com saída PCM experimental e captura de quadros. A tela original de carregamento já foi inspecionada em 640×480; menu, cenário e partida continuam sem comprovação.

A integração mais recente durou 300 segundos e foi encerrada pelo timeout.
Os caches de vértices e fragmentos agora retêm até 32 programas cada, por conteúdo
exato, dispositivo e modo. A execução também ultrapassou dois saltos originais de
retorno ao início da fila circular: os comandos concluíram na GPU antes da atualização de GET.
Onze testes focais passaram. Houve 1.053 quadros com apresentação/ISR confirmados;
a captura final ainda mostra o logotipo original e “LOADING…”. Menu e partida
continuam pendentes. O próximo experimento rastreia a tela de carregamento no
caminho original da interface e suas condições de transição.

| Etapa | Evidência mais recente |
| --- | --- |
| Compilação e testes | Build passou; onze testes focais passaram: caches/execução de shaders, texturas, alpha, AA2, backend, comandos e salto da fila, arrays, estados de software, pipeline e contratos de áudio. |
| Streaming | O mixer original voltou a chamar os leitores. O registro alvo recebeu pelo menos 145 blocos, com consumo liberando slots; antes parava em 29. |
| Saída PCM nativa | O Windows aceitou PCM de seis canais, 48 kHz e 16 bits. A posição real do dispositivo avançou e houve envio de pacotes não silenciosos produzidos pelo mixer original. Conteúdo audível, canais e latência ainda não foram validados. |
| Boot e rendering | Capturas mostram a arte original e “LOADING…”. Antes do cache de fragmentos, foram 772 compilações em 60 segundos; depois, sete em toda a execução de 300 segundos, com reutilização por comparação integral do código gerado. Nas janelas de cerca de um minuto, os recibos DXGI/ISR passaram de 171 para 232. Menu e funcionamento em 1920×1080 ainda não comprovados. |
| Controles e gameplay | Entrada na lógica original, cenário carregado e partida curta ainda não comprovados. |

A saída PCM exige `-NativePcmOutput` no runner e uma build com
`FUT_STREET2_STREAM_CALLBACK_TRACE_HOOK=ON`. A opção é desligada por padrão.
Ela usa o buffer final do mixer original e a posição de reprodução do Windows.
O avanço de 32 amostras do cursor de escrita e o mapeamento 5.1 são experimentais.
Isso não implementa o DSP/APU nem comprova equivalência de áudio Xbox.
O bypass de espera de efeitos ainda foi necessário e completou 2.195 esperas
artificialmente na integração mais recente. Seu orçamento padrão continua 4.096;
o experimento longo usou o limite explícito de 16.384.

As aproximações gráficas permanecem experimentais e restritas aos casos
observados. Não há alegação de fidelidade NV2A. ISO/XBE, assets, código
recompilado proprietário, builds, saves e logs permanecem locais.

A opção experimental `-HighResolutionGpuWait` reduz a pausa entre consultas de
conclusão da GPU, preservando a exigência de conclusão real e os limites de espera.
`-CapturePresentedFrames` salva até 64 superfícies enviadas à apresentação no
diretório isolado do runner. Ambas são desligadas por padrão; as capturas ficam locais.
`-UntexturedAa2Approximation` habilita somente o estado sem textura capturado e
também é desligado por padrão. O timeout do runner é finito, com máximo de 300
segundos; `-FrameCaptureIntervalMs` controla o intervalo após as oito primeiras
capturas, preservando o teto de 64 arquivos.

`RECOMP_FRAGMENT_SHADER_CACHE=0` permite comparar a execução sem o cache de
fragmentos. `RECOMP_VERTEX_SHADER_CACHE=0` desabilita ambos os caches, preservando
o controle de diagnóstico anterior. `RECOMP_FRAGMENT_COMPILER_PROFILE=1` registra
custos e identificadores de compilação com saída limitada; os hashes do perfil
não são usados como chaves do cache. O runner registra essas opções efetivas.

### O que falta

1. Identificar a condição que mantém a tela LOADING e comprovar a transição para o menu e o cenário.
2. Resolver o próximo bloqueio funcional observado em execuções limitadas, preservando o consumo original do streaming.
3. Comprovar uma imagem completa em 1920×1080.
4. Validar controles e uma partida curta com jogadores, bola, regras e placar conduzidos pela lógica recompilada original.
5. Validar áudio, canais e latência, resolver as dependências restantes e retirar o bypass artificial de efeitos.

Build, boot, rendering, resolução, áudio e gameplay são critérios separados.
O estado permanece **NOT_READY** até comprovação funcional.
## Abordagem

- Usar o [XboxRecomp](https://github.com/sp00nznet/xboxrecomp) como base para análise e tradução estática do código Xbox x86 para C, com uma pilha de patches versionada em [`native_recomp/patches`](native_recomp/patches).
- Compilar o código traduzido para o host e implementar apenas compatibilidade sustentada por evidências do executável e dos call sites. Stubs, hipóteses e instruções ainda não traduzidas não contam como funcionalidades implementadas.
- A lógica x86 do jogo segue recompilada estaticamente, com tradução gráfica para o host. O experimento opcional de DSP é separado do executável do jogo e não constitui aprovação de áudio/APU. Não incorporar código ou assets proprietários do jogo.

## Conteúdo

- [`native_recomp/`](native_recomp): integração CMake, suporte nativo e patches incrementais para a tradução XboxRecomp.
- [`tools/`](tools): utilitários de análise e testes de regressão usados pelo fluxo de recompilação.
- [`fifa_pc/`](fifa_pc) e [`pc_compat/`](pc_compat): ferramentas e protótipos auxiliares de investigação; não são o jogo recompilado nem substituem sua lógica original.

Os fontes C gerados, builds, XBE, ISO e assets extraídos são insumos locais e não fazem parte deste repositório. A ISO original deve permanecer somente para leitura. Use apenas uma cópia de jogo obtida legalmente.

## Build e validação

A compilação nativa requer uma geração local do XboxRecomp para preencher `RECOMP_GENERATED_DIR`; os arquivos gerados não são versionados. Exemplo para o alvo da biblioteca:

O gate da geração privada validada exige exatamente 15.611 entradas de
dispatch, incluindo os onze pontos de entrada indiretos recuperados. Uma
geração anterior não satisfaz esse contrato; confirme a regeneração e o
executável realmente utilizado nos testes.

```powershell
cmake -S native_recomp -B artifacts/fut-street2-native/build -G Ninja `
  -DRECOMP_GENERATED_DIR="D:/caminho/local/xboxrecomp/gen"
cmake --build artifacts/fut-street2-native/build --config Release --parallel
ctest --test-dir artifacts/fut-street2-native/build -C Release --output-on-failure
```

Depois de compilar o host opcional, a fronteira de preflight pode ser
verificada sem executar o entry point recompilado:

```powershell
python tools/analysis/recomp_host_preflight_evidence_gate.py `
  --executable D:/caminho/local/fut_street_2_recompiled_app.exe `
  --xbe D:/caminho/local/game_files/default.xbe `
  --mode static

python tools/analysis/recomp_host_preflight_evidence_gate.py `
  --executable D:/caminho/local/fut_street_2_recompiled_app.exe `
  --xbe D:/caminho/local/game_files/default.xbe `
  --mode runtime `
  --data-directory D:/caminho/local/game-data `
  --save-directory D:/caminho/local/save-data
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
  --functions D:/caminho/local/disasm/functions.json `
  --generated D:/caminho/local/xboxrecomp/gen
```

Essa auditoria é limitada a chamadas diretas recuperadas estaticamente; ela não aprova chamadas indiretas, callbacks, imports, compatibilidade de hardware ou execução do jogo.

Para fechar a regressão de cobertura do artefato integral, forneça também o
`summary.json` da mesma geração e o diretório `asm` recuperado:

```powershell
python tools/analysis/recomp_translation_coverage_gate.py `
  --functions D:/caminho/local/disasm/functions.json `
  --generated D:/caminho/local/xboxrecomp/gen `
  --summary D:/caminho/local/xboxrecomp/gen/summary.json `
  --asm-dir D:/caminho/local/disasm/asm
```

Esse gate apenas junta os três controles e mantém os sites sem lowering e a
semântica de hardware explicitamente não provados.

Para classificar os sites indiretos do mesmo subgrafo, forneça também o XBE local:

```powershell
python tools/analysis/recomp_indirect_call_audit.py `
  --functions D:/caminho/local/disasm/functions.json `
  --generated D:/caminho/local/xboxrecomp/gen `
  --xbe D:/caminho/local/game_files/default.xbe
```

Para transformar essa classificação em um gate de regressão do artefato
integral, use:

```powershell
python tools/analysis/recomp_dynamic_callback_evidence_gate.py `
  --functions D:/caminho/local/disasm/functions.json `
  --generated D:/caminho/local/xboxrecomp/gen `
  --xbe D:/caminho/local/game_files/default.xbe `
  --kernel-source D:/caminho/local/xboxrecomp/src/kernel/kernel_thunks.c
```

Esse gate é específico dos sete sites atualmente auditados e mantém a
distinção entre evidência estática e inicialização de runtime; saída aprovada
não significa que qualquer callback esteja resolvido ou executável.

A cobertura dos serviços kernel usados por esses sites pode ser fechada com:

```powershell
python tools/analysis/recomp_kernel_service_contract_gate.py `
  --functions D:/caminho/local/disasm/functions.json `
  --generated D:/caminho/local/xboxrecomp/gen `
  --xbe D:/caminho/local/game_files/default.xbe `
  --kernel-source D:/caminho/local/xboxrecomp/src/kernel/kernel_thunks.c
```

O gate exige o resolver, o bridge, as assinaturas e os marcadores de risco
observados; ele mantém `host_service_semantics=not-proven` e não executa o
XBE.

O inventário complementar dos slots BSS fixos pode ser repetido com:

```powershell
python tools/analysis/recomp_fixed_callback_slot_evidence_gate.py `
  --functions D:/caminho/local/disasm/functions.json `
  --generated D:/caminho/local/xboxrecomp/gen `
  --xbe D:/caminho/local/game_files/default.xbe `
  --kernel-source D:/caminho/local/xboxrecomp/src/kernel/kernel_thunks.c
```

Ele mantém a ausência de writer provado separada da resolução do único slot
que tem inicializador de função observado.

O domínio estático do seletor de buffer EAGL pode ser conferido com:

```powershell
python tools/analysis/eagl_dynamic_buffer_selector_control.py `
  --asm D:/caminho/local/disasm/asm/text.asm `
  --xbe D:/caminho/local/game_files/default.xbe
```

O resultado mantém `writer_sites=5`, `consumer_sites=33`,
`runtime_selected_buffer=not-proven` e `rendering_binding=not-proven`; ele
apenas impede assumir que `0x00345BCC` seja constante ou ignorar seus
consumidores estáticos conhecidos. A saída também inclui
`consumer_function_count=31`, `consumer_function_envelope=0x001A3E00-0x001A4B40`,
`consumer_anchor_counts=0x3458d0:33,0x3458d4:33` e `consumer_shape_counts`,
com `28` composições genéricas e cinco formas especiais downstream.

A consistência entre os relatórios estáticos da cadeia EAGL pode ser conferida
com:

```powershell
python tools/analysis/eagl_static_render_chain_control.py `
  --draw-summary D:/caminho/local/eagl-indexed-draw-source-control-summary.tsv `
  --parameter-summary D:/caminho/local/eagl-render-parameter-static-control-summary.tsv `
  --material-summary D:/caminho/local/eagl-material-declaration-control-summary.tsv `
  --model-summary D:/caminho/local/eagl-model-material-reference-control-summary.tsv
```

Esse gate confere identidade, hashes, contagens e joins entre os quatro
relatórios, mas mantém `runtime_selector=unresolved`,
`full_rendering_binding=not-proven` e `original_semantics=not-claimed`.

Os efeitos dos helpers do seletor podem ser conferidos com:

```powershell
python tools/analysis/eagl_selector_helper_effect_control.py `
  --asm D:/caminho/local/disasm/asm/text.asm `
  --xbe D:/caminho/local/game_files/default.xbe
```

O gate verifica somente formas estáticas e não executa o XBE.

O contexto dos dois opcodes ainda não traduzidos pode ser conferido com:

```powershell
python tools/analysis/recomp_unimplemented_context_control.py `
  --asm-dir D:/caminho/local/disasm/asm
```

O gate exige os owners, instruções e fragmentos locais observados, mas
mantém `hardware_semantics=not-proven` e os dois sites sem lowering.

Opcionalmente, acrescente `--kernel-source` apontando para
`D:/caminho/local/xboxrecomp/src/kernel/kernel_thunks.c` para conferir se os
ordinais de thunk usados pelo subgrafo têm cases explícitos no resolver do
host. Isso mede cobertura de ponteiro do resolver; não aprova ABI, efeitos
colaterais ou semântica Xbox dos serviços.

Nessa auditoria estática específica, somente aliases lineares demonstrados e inicializadores constantes de slots BSS são classificados; seus sete alvos dinâmicos não recebem aprovação de runtime pelo relatório. Isso não é uma contagem dos bloqueios atuais do executável. O relatório preserva a proveniência de cada família e os aliases de endereço usados pelos writers candidatos. A auditoria de stack shape dos callbacks é somente contagem de pushes observados e não constitui prova de tipos, ABI, execução ou fidelidade dos serviços Xbox.

Esses comandos validam compilação e testes do código-fonte, não os gates do jogo original. Não execute nem distribua o XBE ou assets proprietários.

## Licença e créditos

Este repositório não inclui uma licença de redistribuição do jogo nem reivindica direitos sobre FIFA Street 2. Consulte as licenças do XboxRecomp e de quaisquer dependências antes de redistribuí-las. O trabalho específico deste projeto ainda não tem uma licença open-source definida.
