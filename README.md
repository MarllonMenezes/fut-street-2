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

## Estado verificável — 27 de outubro de 2026

**NOT_READY — ainda não é um jogo recompilado jogável.** Cenário apresentado,
controles em partida e partida curta ainda não foram comprovados.

Na execução nativa limitada a 300 segundos, o quad original de 320×240
concluiu com 76.800 invocações de pixel shader e gravação da superfície do
jogo, usando as opções experimentais descritas abaixo. A execução parou no
desenho seguinte: uma textura de formato `00011129` ainda não é aceita.
Esse resultado não comprova apresentação do quad nem gameplay. Uma tentativa
anterior atingiu o limite na tela de carregamento. **Os 45 testes de
integração passaram.**

O experimento quincunx 1280×480 → 320×240 RGB565/Z16 é separado e desligado por
padrão. O modelo de cinco amostras inclui deslocamento experimental de 0,001
texel para estabilizar a seleção em D3D; testes de pixels RGB565 e Z16 passaram
no WARP e na GPU local. Fase, minificação e fidelidade NV2A não foram medidas.
Um segundo opt-in restringe o dithering RGB565 ao estado observado, usando a
quantização D3D sem adicionar dithering espacial. Não há medição de fidelidade
NV2A. O próximo passo é investigar a textura recusada no desenho seguinte.

`native-presented-latest.bmp` só é promovido após recibo DXGI e aposentadoria
correlacionada da fila original; a imagem pendente fica separada. Cada execução
nativa tem timeout de até 300 segundos e logs, saves e capturas isolados no D:.
ISO/XBE e demais insumos originais são preservados.

AA2/quincunx, omissão do ruído BC1, filtragem, PCM e bypass artificial da espera
de áudio continuam experimentais, sem fidelidade NV2A ou DSP comprovada.
Assets, código proprietário gerado, capturas e relatórios locais não são
publicados. As referências xemu/nxdk orientam a semântica, sem cópia de
implementação externa. O REA não realizou análise nativa: Ghidra/IDA não estão
configurados e sua captura de processos não suporta Windows neste ambiente.

### Opções de diagnóstico e reprodução

`-Quincunx320Approximation` habilita somente o experimento 1280×480 →
320×240 RGB565/Z16; é separado e desligado por padrão.

`-Rgb565DitherApproximation`, junto da opção anterior, habilita a aproximação
sem dithering espacial para o estado RGB565 de 320×240 observado; também é
desligada por padrão.

`-ProjectiveAa2Approximation`, junto de `-Aa2Approximation`, habilita o
experimento de interpolação em perspectiva. É desligado por padrão e exige
prova separada das coordenadas; o caminho afim continua exigindo W=1.
A família BC2 com o filtro projetivo exige também `-Bc2Aa2FamilyApproximation`.
Os casos BC1 observados também exigem `-Dxt1NoiseApproximation`: o ruído
NV2A desconhecido continua omitido e identificado. Mipmaps são carregados
integralmente, sem substituir a textura pelo nível zero.

A ponte de entrada exige `FUT_STREET2_NATIVE_INPUT_HOOK=ON` na build e
`-NativeGamepadInput` no runner. É desligada por padrão. O provedor físico usa
Windows XInput. `-NativeKeyboardInput` adiciona o teclado na porta 0: Enter
para Start, Backspace para Back, setas para direcional, Z/X/C/V para A/B/X/Y,
WASD para analógico esquerdo e IJKL para direito. A janela nativa precisa ter
foco; perder o foco solta todas as teclas.

`-NativeInputVirtualOnly` desliga o provedor físico. Para reprodução limitada,
`-NativeInputScript '140000:start:400,220000:a:5000'` gera as entradas nos
tempos indicados, em milissegundos desde a primeira consulta de entrada.
O runner identifica essas entradas como automatizadas e registra as opções.
Isso não substitui validação com um controle físico.

A saída PCM exige `-NativePcmOutput` no runner e uma build com
`FUT_STREET2_STREAM_CALLBACK_TRACE_HOOK=ON`. É desligada por padrão e usa o
buffer final do mixer original: seis canais, 48 kHz e 16 bits. O avanço de 32
amostras do cursor de escrita e o mapeamento 5.1 são experimentais.

`RECOMP_READBACK_RESOURCE_CACHE=0` desliga o novo cache de alocações para
comparações no mesmo binário. `RECOMP_GRAPHICS_STAGE_TIMING=1` registra seus
contadores com limite de saída. A retenção fica restrita a quatro texturas
BGRA8/R24G8, com dimensões máximas de 1920×1080; isso não habilita novos modos
de rendering nem comprova suporte funcional a essa resolução.

`RECOMP_FRAGMENT_SHADER_CACHE=0` permite comparar sem o cache de fragmentos.
`RECOMP_VERTEX_SHADER_CACHE=0` desabilita ambos os caches de shaders.
`RECOMP_FRAGMENT_COMPILER_PROFILE=1` registra custos e identificadores limitados;
os hashes do perfil não são usados como chaves. O runner registra essas opções.

`-HighResolutionGpuWait` reduz a pausa entre consultas, exigindo conclusão real
da GPU. `-CapturePresentedFrames` salva até 64 superfícies no diretório isolado;
`-FrameCaptureIntervalMs` controla o intervalo após as oito primeiras capturas.
Essas opções são desligadas por padrão. O timeout máximo é de 300 segundos.
`-UntexturedAa2Approximation` também permanece desligado por padrão.

O rastreamento da interface exige `FUT_STREET2_FRONTEND_LOADING_TRACE_HOOK=ON`
e `-FrontendLoadingTrace`. É desligado por padrão, com até 64 linhas por ponto
e até 512 passos do interpretador dentro da chamada de preload. Leituras de
texto e da pilha da VM são protegidas e limitadas. Não altera registradores,
condições de fase ou resultados das rotinas. O setter de propriedades também
registra até 16 entradas por ponto, somente dentro da chamada de preload.

As aproximações gráficas e o bypass de áudio permanecem experimentais, sem
alegação de fidelidade NV2A ou DSP. O orçamento padrão de conclusões artificiais
é 4.096; as integrações longas usam explicitamente 16.384. ISO/XBE, assets,
código recompilado proprietário, builds, saves e logs permanecem locais no D:.

### O que falta

1. Passar pela seleção de lado e pelo estado Ready na lógica original; concluir a composição da interface.
2. Carregar um cenário e resolver o próximo bloqueio funcional observado com execuções limitadas.
3. Comprovar uma imagem completa em 1920×1080.
4. Validar controles e uma partida curta conduzida pela lógica recompilada original.
5. Validar áudio, canais e latência e retirar o bypass artificial de efeitos.

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

O gate da geração privada validada exige exatamente 15.614 entradas de
dispatch, incluindo os quatorze pontos de entrada indiretos recuperados. Uma
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
