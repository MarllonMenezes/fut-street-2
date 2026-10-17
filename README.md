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

## Estado verificável — 17 de outubro de 2026

**`NOT_READY` — ainda não jogável.** A build Release contém 15.610 entradas
de dispatch. O launch limitado alcançou o triangle strip de vertex arrays,
validou quatro índices e executou o shader recompilado para seis vértices; a
rasterização desse desenho ainda está bloqueada pelo formato da textura usada.

| Etapa | Evidência atual |
| --- | --- |
| Tradução e compilação | Build nativo Release com 15.610 entradas exatas. As entradas observadas `0x00240AFB` e `0x001D1EAD` foram recuperadas do XBE, regeneradas e incluídas no executável testado. |
| Boot | Execução parcial da lógica recompilada confirmada em launches limitados; boot completo e menu ainda não comprovados. |
| Rendering | Recibos DXGI com aposentadoria pela ISR original e recompilação de shader observados. No alvo atual, o host validou o DMA NVM de 64 MiB, buscou quatro índices e executou o shader original para seis vértices. A conversão S32K usa o modelo local S16-para-float, sem validação no NV2A. A preparação do raster parou na textura `0x06610E29` (`control1=0x14000000`); esse strip não produziu pixels nem writeback. Desenhos anteriores escreveram superfícies no guest, mas isso não aprova este desenho nem a fidelidade NV2A. |
| Áudio | Não funcional nem validado. O caminho experimental usa bypass explícito da espera de efeitos no host; o DSP continua isolado. |
| Controles e gameplay | Menu navegável, controles na lógica original e partida curta ainda não comprovados. |

As entradas `0x00240AFB` (dois saltos diretos para funções já registradas) e
`0x001D1EAD` (folha linear terminada em RET, com folha vizinha decodificada)
foram recuperadas com provas estáticas restritas. A build usada nesta etapa
incluiu as 15.610 entradas; os **37/37 gates nativos passaram**. No launch
limitado mais recente, `SET_BEGIN_END=6` foi seguido pelos índices executados
`[0,1,2,3]`. Vertex A/B apontaram para o mesmo descritor IN_MEMORY `0x3D`,
com alvo NVM e span declarada `0x07FFB000` (aproximadamente 128 MiB), limitada
pelo host à região suportada de 64 MiB. O offset `0x00166574` passou pelas verificações de
intervalo; os seis vértices convertidos pelo modelo S16-para-float chegaram ao
shader recompilado. Isso comprova fetch e invocação do shader neste caso, não
a semântica NV2A da conversão nem a rasterização do strip.
O próximo bloqueio é a textura `0x06610E29`, com `control1=0x14000000`, que
o decoder atual recusa como armazenamento não suportado. O processo saiu com
`exit 3` antes do timeout de 60 segundos; o XBE permaneceu inalterado. Logs,
saves e código gerado ficam locais em D:.

As aproximações gráficas são opt-in e restritas aos casos observados:
AA2 em ARGB8 com os baselines aceitos; quincunx somente no resolve de
1280×480 para 640×480; BC1 padrão sem o ruído DXT1 desconhecido somente na
textura 512×512 e desenho AA2 autorizado; P8 32×32 convertido sem perdas
para ARGB8 e rasterizado sob opção AA2 própria. Esses experimentos não
comprovam equivalência NV2A nem autorizam outros formatos ou modos.

O bypass de áudio também exige opção explícita e fica desligado no build
padrão. Sua conclusão artificial serve para investigar os bloqueios seguintes;
não representa execução do DSP ou áudio correto. O caminho sem bypass ainda
aguarda os efeitos de áudio. No experimento DSP isolado, o contrato do
periférico `FFFFB2` e o campo de passo do DMA circular permanecem desconhecidos;
nenhum START original foi observado. O DSP experimental não participa do boot.

### Próximos passos

1. **Resolver a textura do strip:** identificar em fontes locais e nos
   registros executados a organização exata de `0x06610E29` e de
   `control1=0x14000000`; validar endereço, dimensões e span antes de admitir
   leitura. Não inferir formato a partir do nome nem usar aproximação sem
   autorização específica.
2. **Validar rendering do strip:** repetir o launch limitado e exigir desenho
   nativo concluído com pixels e writeback da superfície no guest. A execução
   do shader e a fila aceita não bastam.
3. **Repetir o launch limitado:** localizar o próximo bloqueio sem concluir a
   fila artificialmente.
4. **Chegar ao menu utilizável:** resolver o primeiro bloqueio encontrado
   em cada execução, seja tradução, serviço de host, carregamento ou desenho.
   Não substituir funções ausentes por sucesso fictício.
5. **Validar controles e cenário:** comprovar entrada chegando à lógica
   original, navegação do menu e carregamento de uma partida.
6. **Comprovar uma partida curta:** jogadores, bola, regras e progressão
   executados pela lógica original, com estabilidade e resultado verificável.
7. **Fechar áudio e fidelidade:** resolver os contratos DSP/APU com evidência,
   retirar a dependência do bypass e validar os efeitos das aproximações
   gráficas. Uma PoC com experimentos não equivale ao jogo pronto.

Build, boot, rendering, áudio e gameplay são critérios separados. O estado
permanece `NOT_READY` até a comprovação dos requisitos funcionais. Insumos,
código gerado proprietário, capturas, logs e relatórios locais não são
publicados neste repositório.

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

O gate da geração atual exige exatamente 15.610 entradas de dispatch,
incluindo os dez pontos de entrada indiretos recuperados. Uma geração
anterior não satisfaz esse contrato; confirme a regeneração e o executável
realmente utilizado nos testes.

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
