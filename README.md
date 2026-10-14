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

## Estado verificável — release 2.70

A descrição de textura que bloqueava a terceira submissão agora é retida,
com os campos originais e bits de presença separados nos quatro estágios:
offset, formato, endereço, controle/pitch, filtro, dimensões e palette.
Esses setters não fazem upload antecipado nem executam uma amostragem.

`graphics_textures.c` valida a forma observada: textura linear 2D A8R8G8B8,
um mip, seleção exata do contexto DMA A/B e limites de memória/pitch.
`graphics_native_textures.c` importa os pixels para textura/SRV D3D11 e
renova o upload após escritas da CPU. No uso por draw, o backend primeiro
grava na memória guest qualquer superfície alterada pela GPU usada como fonte.
Falha de leitura/criação preserva o recurso existente.

A amostragem nativa implementada cobre filtros MIN/MAG iguais point ou linear,
nível zero e coordenadas projetadas em espaço de texels, com wrap, mirror,
clamp-to-edge e border. **O filtro original `0x04072000` continua pendente**:
ele exige convolução quincunx, não linear comum. Não foi substituído por outro
filtro; a [referência consultada](https://github.com/xemu-project/xemu/blob/36334d95b86f9c3c6ae34efb9af06785273f8e1a/hw/xbox/nv2a/pgraph/glsl/psh.c)
também marca quincunx como incompleto. Signed channels, alpha kill, color key,
filtros mistos e formas de armazenamento ainda não traduzidas são recusados
no uso, sem liberar um draw dependente.

Build Release e CTest passaram `18/18`. Os testes WARP conferem todos os
pixels importados, preservação de padding, refresh de memória, limites e
falhas sem descarte. Amostras point/linear, projeção e endereçamento são
comparados com cálculo independente; o receiver real verifica os contextos
DMA A/B com bases distintas e writeback GPU → guest → textura.
**Esses testes usam dados/shader sintéticos, não o fragment program do jogo.**

A execução recompilada, limitada a cinco segundos, passou do offset `304`
ao `588` da terceira submissão. O bloqueio atual é `0x0318`, valor zero,
controle de parâmetros de pontos sem handler. Os dois clears originais e
as duas primeiras submissões continuam concluídos, com marcador `7`.
A terceira submissão não foi concluída, não chegou ao primeiro draw e ainda
não exercitou o upload de textura no jogo. ISO intacta; novas saídas no D:.

Próximo alvo: controles e tradução do fragment program exigidos pelo primeiro
draw, incluindo a convolução observada. SAMPLEALPHA/RDI, geometria, demais
shaders, apresentação de frame, gameplay e áudio continuam pendentes.
`NOT_READY` permanece.

## Abordagem

- Usar o [XboxRecomp](https://github.com/sp00nznet/xboxrecomp) como base para análise e tradução estática do código Xbox x86 para C, com uma pilha de patches versionada em [`native_recomp/patches`](native_recomp/patches).
- Compilar o código traduzido para o host e implementar apenas compatibilidade sustentada por evidências do executável e dos call sites. Stubs, hipóteses e instruções ainda não traduzidas não contam como funcionalidades implementadas.
- Manter fora do caminho de execução qualquer emulação de CPU, GPU ou APU. Não incorporar código ou assets proprietários do jogo.

## Conteúdo

- [`native_recomp/`](native_recomp): integração CMake, suporte nativo e patches incrementais para a tradução XboxRecomp.
- [`tools/`](tools): utilitários de análise e testes de regressão usados pelo fluxo de recompilação.
- [`fifa_pc/`](fifa_pc) e [`pc_compat/`](pc_compat): ferramentas e protótipos auxiliares de investigação; não são o jogo recompilado nem substituem sua lógica original.

Os fontes C gerados, builds, XBE, ISO e assets extraídos são insumos locais e não fazem parte deste repositório. A ISO original deve permanecer somente para leitura. Use apenas uma cópia de jogo obtida legalmente.

## Build e validação

A compilação nativa requer uma geração local do XboxRecomp para preencher `RECOMP_GENERATED_DIR`; os arquivos gerados não são versionados. Exemplo para o alvo da biblioteca:

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

Somente aliases lineares demonstrados e inicializadores constantes de slots BSS são classificados; os 7 alvos dinâmicos continuam explicitamente não resolvidos, embora o relatório preserve a forma de proveniência de cada família e os aliases de endereço usados pelos writers candidatos. A auditoria de stack shape dos callbacks é somente contagem de pushes observados e não constitui prova de tipos, ABI, execução ou fidelidade dos serviços Xbox.

Esses comandos validam compilação e testes do código-fonte, não os gates do jogo original. Não execute nem distribua o XBE ou assets proprietários.

## Licença e créditos

Este repositório não inclui uma licença de redistribuição do jogo nem reivindica direitos sobre FIFA Street 2. Consulte as licenças do XboxRecomp e de quaisquer dependências antes de redistribuí-las. O trabalho específico deste projeto ainda não tem uma licença open-source definida.
