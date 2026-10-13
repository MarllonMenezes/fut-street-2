# Fut Street 2 — primeira tradução e compilação nativa

Data: 2026-10-13. Repositório público: `MarllonMenezes/fut-street-2`.

## O que foi feito

- Extraído localmente o `default.xbe` da ISO com leitura; a ISO original não
  foi modificada.
- Fixado o XboxRecomp oficial na release v0.13.1, commit
  `193e2995ffaec871bb3be43a43bd66aa5fccfeb1`.
- Analisadas as seis seções executáveis: 15.605 funções foram classificadas
  para tradução; o gerador terminou 15.605/15.605 sem falha de função.
- Geradas 65 unidades C e compiladas com Zig/Clang 21.1 para objetos COFF
  Windows x86_64. Um target CMake agora empacota essas unidades em biblioteca
  estática, sem incluir os arquivos derivados no repositório.
- A biblioteca resultante contém 65 membros. O target usa `-O0` durante esta
  verificação inicial de compilação; isso não é uma configuração final de
  desempenho nem uma build final do jogo.

## O que isso prova — e o que não prova

Prova que o pipeline conseguiu analisar o XBE, gerar C para as funções
encontradas, compilar as unidades emitidas e arquivá-las. Não prova que o
executável final linka, inicia, renderiza, reproduz áudio ou executa gameplay
fiel ao original. Nenhum XBE ou executável recompilado foi executado.

O relatório do gerador registra 86 destinos de chamada tratados com stubs e
161 instruções (39 mnemonics) emitidas como no-op. Esses itens podem afetar
correção e precisam ser localizados e auditados. Os resultados do analisador
ABI são hipóteses até serem validados contra convenções e call sites reais.
Não há bindings de serviços do Xbox declarados como implementados.

O runtime gráfico/áudio do XboxRecomp não foi integrado. Como o projeto exige
deixar explícito o que envolve emulação, GPU/APU permanecem fora desta etapa.
Os smoke, runtime headless de 80 frames e replay determinístico executados
foram do protótipo .NET existente; eles não exercitam o XBE nem o código C
gerado.

## Correção incremental do lifter

O primeiro passe mostrou seis `movdqa`/`movdqu` emitidos como no-op. O patch
reproduzível `native_recomp/patches/0001-lift-movdqa-movdqu.patch` os encaminha
para a infraestrutura existente de movimento XMM de 128 bits e inclui teste
para carga e armazenamento. A geração foi repetida: 15.605/15.605 funções,
86 stubs, 155 instruções no-op em 37 mnemonics (antes: 161 em 39). As 65
unidades foram recompiladas e o archive local atualizado com 65 membros.

O patch preserva os dados transferidos, mas não reproduz a exceção de
alinhamento arquitetural de `movdqa`; portanto é avanço limitado, não semântica
completa. A suíte upstream agora marca `602 passed, 40 skipped`. Não foi
executado o XBE nem um executável do jogo.

## Auditoria estática dos limites restantes

Os 155 no-ops estão distribuídos em `.text` (137), `D3D` (7), `XGRPH` (5),
`XPP` (4) e `DSOUND` (2). O cruzamento com o classificador local coloca 35
deles em funções rotuladas como CRT/render/game-vtable e 120 em funções
`unknown`. Essas etiquetas também são heurísticas, mas bastam para mostrar que
não é seguro presumir que os no-ops são código morto.

Dos 86 stubs de chamada, 79 não têm metadado `ret N` recuperado; nesses casos
o stub gerado só consome o endereço de retorno (`g_esp += 4`). Os outros sete
obtiveram um tamanho explícito de retorno. Isso não valida convenção de chamada
nem limpeza de argumentos: o equilíbrio da pilha e o comportamento dos alvos
continuam sem prova. O feedback iterativo de chamadas indiretas do XboxRecomp
depende de observações de execução; não foi usado porque o XBE e o executável
recompilado não foram executados.

## Fronteira do runtime e requisito sem emulação

Também auditei o `CMakeLists.txt` da release fixada e o template de novo jogo.
O alvo guarda-chuva `xboxrecomp` vincula `xbox_apu` (emulação do MCPX APU) e
`xbox_nv2a` (emulação da GPU NV2A), além de camadas de kernel, D3D8→D3D11,
DirectSound, USB, input e vídeo. O template de jogo usa esse alvo guarda-chuva
por padrão. A biblioteca estática compilada neste projeto **não** inclui nem
compila esses runtimes.

Para respeitar o requisito atual de não usar emulação, não integrar o alvo
guarda-chuva nem os componentes APU/NV2A. O caminho restante é mapear as APIs
originais para APIs nativas do host e traduzir chamadas/command streams do
gráfico para D3D11; áudio precisa de uma ponte nativa equivalente. Isso é
trabalho de compatibilidade por título, não um runtime já disponível nem
bindings que possamos presumir. É necessário confirmar se essa tradução direta
de APIs e comandos é permitida dentro de "sem emulação"; sem esse limite claro,
o link final e os gates de rendering/áudio não podem ser definidos corretamente.

## Scaffold versionado

`native_recomp/CMakeLists.txt` espera que `RECOMP_GENERATED_DIR` aponte para
uma geração local do XboxRecomp. Em Windows com Zig, o wrapper
`native_recomp/zig-ar.cmd` dispatcha o subcomando de arquivamento do Zig:

```powershell
$env:FUT_STREET2_ZIG = 'C:/tools/zig/zig.exe'
cmake -S native_recomp -B artifacts/fut-street2-native/build -G Ninja `
  -DRECOMP_GENERATED_DIR='C:/private/recomp/gen' `
  -DRECOMP_ARCHIVER='D:/projeto zeta/native_recomp/zig-ar.cmd'
cmake --build artifacts/fut-street2-native/build --config Release --parallel 4
```

O exemplo pressupõe que o usuário já gerou os arquivos localmente. Não
versionar nem publicar ISO, XBE, C gerado, assets extraídos, objetos, biblioteca,
relatórios que revelem dados do jogo ou credenciais.

## Verificações deste marco

- XboxRecomp: `601 passed, 40 skipped` na validação inicial. Após o patch do
  lifter: `602 passed, 40 skipped`; os skips continuam sem serem tratados como
  aprovados.
- Projeto .NET: build Release, 0 avisos/erros; self-test `105 passed`.
- Smokes do protótipo: identidade/metadados, headless `80 frames`, janela
  `960x540`, replay byte-idêntico de match/input/animation/audio e runner
  `10 passed`. São validações do protótipo, não do jogo recompilado.
- SHA-256 da ISO antes/depois: `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`;
  tamanho `2,013,003,776` bytes, atributo e horário de modificação preservados.
- `git diff --check`: executado antes da publicação.

Estado global continua `NOT_READY`. Próximo marco: auditoria detalhada dos
stubs/opcodes no-op, imports e dependências de runtime, seguida de plano para
link; não executar o XBE sem uma etapa explícita de bring-up.

## Etapa 1.32 — primeiro executável host linkado sem NV2A/APU (2026-10-13)

- Evidência de startup: o contrato do loader aponta entrypoint retail
  `0x0024D8BE`; `recomp_dispatch.c` contém a mesma VA apontando para
  `xbe_entry_point`. O novo bootstrap chama `recomp_lookup` com essa VA após
  mapear o XBE, inicializar kernel/thunks e definir `g_esp = XBOX_STACK_TOP`.
- O CMake agora oferece `fut_street_2_no_emulation_kernel`, compilando 18 fontes
  do kernel de XboxRecomp v0.13.1. A lista exclui `nv2a_pb_scan.c`,
  `nv2a_pb_exec.c`, `nv2a_vsh_interp.c` e `nv2a_combiner.c`; os hooks opcionais
  são satisfeitos pela implementação disabled versionada. Os alvos APU/NV2A e
  o guarda-chuva upstream `xboxrecomp` não são linkados.
- Foram adicionados `recomp_manual_lookup.c` (fallback sem overrides de título)
  e diagnósticos para indirect calls/untranslated instructions, incluindo o
  trap opt-in `RECOMP_UNIMPL_TRAP`. O alvo de suporte tem teste CTest.
- As bibliotecas nativas selecionadas D3D8→D3D11, vídeo e XInput, mais import
  libraries para `d3dcompiler_47.dll`/`xinput1_4.dll`, fecharam o link. Os nomes
  exportados nas duas `.def` foram confirmados contra as DLLs do host.
- `fut_street_2_recompiled_app.exe` linkou com `--no-undefined`, tamanho
  `165,368,832` bytes. CTest: `2/2` (suporte no-emulação e `--help`). O smoke
  `--help` não carrega o XBE; nenhuma outra execução do `.exe` foi feita.
- O bootstrap exige `--launch <default.xbe> <game-data-directory>` e recusa
  `RECOMP_AC97_READY`/`RECOMP_PB_EXEC`; isso não garante semântica dos demais
  imports. A etapa demonstra link e CLI, não boot do jogo, equilíbrio de ABI,
  funcionamento de stubs, rendering, gameplay, animação ou áudio. O runtime
  DirectSound não está incluído neste link.
- Compilação Release nativa passou, mas registrou warnings oriundos dos fontes
  upstream do kernel (incluindo formatos `printf` e comentários); não foram
  silenciados nem corrigidos localmente. O arquivo gerado permanece temporário,
  fora do GitHub. ISO continua read-only.

Estado ainda `NOT_READY`; não executar o XBE nem este host `.exe` com `--launch`
sem autorização explícita de bring-up. Próxima ação: revisar com evidência os
stubs de kernel chamados pelo XBE e a semântica de `RECOMP_UNIMPL`/indirect
dispatch, depois fechar o caminho de áudio e validar startup controlado.

## Etapa 1.33 — auditoria estática dos destinos stub (2026-10-13)

- Foi adicionada a ferramenta `tools/analysis/xboxrecomp_unresolved_target_audit.py`
  para cruzar stubs gerados, intervalos do mapa de funções e limites exatos de
  seção do `default_analysis.json`. Ela preserva separadamente a flag XBE de
  executável e o nome da seção; não deduz que endereço em seção executável seja
  código nem que um destino no meio de uma função seja uma nova função.
- No conjunto local desta geração: 86 destinos, dos quais 74 caem dentro de um
  intervalo de função, 4 coincidem com intervalos sobrepostos, 1 termina
  exatamente no limite de uma função e 7 ficam fora dos intervalos conhecidos.
  Por seção: `.text` 76, `DSOUND` 2, `XPP` 2, `.rdata` 2 e 4 sem seção mapeada.
- Duas entradas apontadas para `.rdata` pertencem a offsets não alinhados em
  relação ao endereço da tabela de thunks declarado pelo XBE. Isso é sinal de
  destino não confiável, não autorização para criar binding ou tratar os bytes
  como função. Os quatro destinos sem seção e os grupos de intervalos
  sobrepostos também permanecem sem interpretação semântica.
- 79 stubs não têm evidência gerada de `ret N`; sete têm esse metadado. Nenhum
  desses valores valida ABI, limpeza de argumentos ou correção de chamada.
- A primeira contagem exploratória foi invalidada porque usava limites de seção
  fixos incompatíveis com o XBE real; esta etapa usa o mapa de seções extraído
  do próprio XBE e a ferramenta falha em intervalos sobrepostos de seção.
- Quatro testes cobrem parsing, duplicatas, limites de intervalos e a distinção
  entre flag executável e classificação funcional. O relatório detalhado pode
  conter endereços derivados do jogo e deve permanecer local; somente código e
  estatísticas agregadas são versionados.

Readiness continua `NOT_READY`; nenhum stub foi promovido a implementação nem
nenhum código do título foi executado. Próximo passo: correlacionar cada alvo
interior/intervalo ambíguo com suas instruções chamadoras e limites reais antes
de alterar a descoberta de funções; não usar seeds inferidas como observação de
runtime. Audio/graphics e boot permanecem gates pendentes.

## Etapa 1.34 — xrefs dos stubs e teste conservador de coalescência (2026-10-13)

- A auditoria aceita agora `--xrefs-json` opcional e relaciona cada endereço
  stub a referências estáticas diretas, mantendo `call`, `jump` e `cond_jump`
  separados e distinguindo fontes com intervalo único de fontes com funções
  sobrepostas. Um desvio não é promovido a binding nem prova automática de
  limite incorreto.
- Na geração fixada: 86 destinos stub; 93 xrefs diretos para 63 destinos, sendo
  58 condicionais e 35 incondicionais, sem nenhum xref `call`. Vinte e três
  desses desvios chegam ao alvo dentro de um único intervalo-fonte; outros 56
  partem de intervalos sobrepostos. Os demais não estabelecem ownership local.
- Caso focal `0x0017A0CA`: cinco saltos diretos, quatro de blocos com ownership
  de intervalo único e um de origem sobreposta. O C gerado tem um tail-jump em
  `0x0017AAAE` emitido como chamada ao stub. O alvo está no intervalo do corpo
  `0x00179F60–0x0017AC02`, mas há aliases de tail jump sobrepostos.
- Testei a recuperação com as entradas de alias `0x0017A4EB` e `0x0017AB20`.
  O XboxRecomp recusou a coalescência porque `0x0017A4EB` tem evidência
  independente de entrada. O resultado é preservado: não contornei a proteção,
  não alterei o tradutor e não promovi esse alvo como falso split comprovado.
  A geração de teste terminou na validação, antes de traduzir o XBE.
- Um novo teste cobre a separação de xrefs por intervalos únicos/sobrepostos
  e agregação; suíte de análise: 62/62. Bounds e saída do probe ficaram
  em Temp; nenhum XBE foi executado.

O próximo passo é auditar, para candidatos de alias, todas as referências de
código e dados e a origem do `tail_jump_alias`; só fornecer bounds ao coalescer
quando a entrada independente estiver explicada. Em paralelo, seguir os stubs
sem xref direto e as instruções no-op. Os gates principais continuam `NOT_READY`.

## Etapa 1.35 — tail-jumps de aliases descobertos tardiamente (2026-10-13)

- A inspeção estática de `0x0026E9E7` mostrou um caso concreto: uma entrada
  `tail_jump_alias` descoberta depois do passe inicial terminava em tail-jump
  para o interior de outra função. Como os aliases tardios não voltavam ao
  passe de tail-jumps, a tradução deixava o destino como stub e perdia o
  comportamento de retorno `ret 16` daquele bloco.
- Foi versionado o patch
  `native_recomp/patches/0002-detect-late-alias-tail-jumps.patch`, que repete a
  descoberta de tail-jumps após materializar aliases, com limite de oito
  rodadas, e inclui teste sintético. Aplicação em checkout limpo do XboxRecomp
  v0.13.1 (após patch 0001) e teste focal: 1/1. Na suíte upstream da árvore
  com ambos patches: 603 passaram, 40 foram ignorados.
- Nova análise estática: 15.667 funções detectadas, 1.284 classificadas como
  `tail_jump_alias`, 118 como `tail_jump_target`; 15.679/15.679 funções
  traduzidas sem falhas em 64 unidades C. O C de `0x0026E9E7` agora preserva o
  corpo, chamada interna e limpeza de 16 bytes, é chamado pelo branch e pelo
  alias, e deixou a lista de stubs não resolvidos.
- A cobertura também revelou outros destinos: total atual 87 stubs (era 86) e
  154 instruções no-op (eram 155). Portanto a contagem agregada de stubs subiu
  por um nesta geração; o ganho comprovado é apenas a tradução do bloco focal,
  não uma redução global nem correção semântica dos demais stubs.
- Build nativo Release do host recompilado concluiu; link sem símbolos
  indefinidos e CTest 2/2. O build produziu warnings do C upstream; eles foram
  mantidos visíveis. Nenhum XBE nem executável com `--launch` foi iniciado.
  O teste `--help` não carrega dados do jogo.
- A ISO manteve-se fora das entradas de escrita; sua verificação SHA-256 de
  fechamento será repetida com os demais gates antes da publicação desta etapa.

`NOT_READY` permanece. Este avanço corrige uma lacuna de descoberta no
tradutor, mas não prova ABI, boot, rendering, assets, gameplay, animação ou
áudio. Próximo passo: repetir a auditoria de stubs na nova geração, separar os
novos alvos por causa e continuar o caminho de inicialização/áudio com evidência;
não executar o XBE sem autorização explícita de bring-up.

## Etapa 1.36 — aliases de branches condicionais entre funções (2026-10-13)

- A nova auditoria da etapa 1.35 revelou blocos atingidos por `jcc` de uma
  função para uma instrução interior de outra função. O C gerado encaminhava
  esses destinos a stubs que apenas retornavam. Caso focal: seis destinos no
  corpo iniciado em `0x00201595`, todos com xref condicional direto de uma
  função distinta e ownership único no mapa estático.
- Foi versionado
  `native_recomp/patches/0003-alias-cross-function-conditional-branches.patch`.
  O passe acrescenta uma entrada alias ao destino sem coalescer nem truncar o
  corpo proprietário; exige origem e destino dentro de exatamente um intervalo
  de função e destino em boundary de instrução decodificada. Entradas com
  ownership ambíguo/sobreposto são deliberadamente mantidas sem resolução.
- Aplicação limpa na release XboxRecomp v0.13.1 após patches 0001/0002; testes
  focais dos patches 0002/0003: 3/3. Suíte completa com dependências presentes:
  605 aprovados, 40 ignorados.
- Reanálise estática: 15.683 funções, 1.298 `tail_jump_alias` e 120
  `tail_jump_target`. Geração terminou 15.695/15.695 sem falhas, em 62 unidades
  C divididas e 64 unidades C compiladas pelo CMake (incluindo dispatch e
  stubs). Os seis alvos focais aparecem como funções e deixaram a lista de
  stubs.
- Auditoria agregada de stubs: 81 (eram 87 após a etapa 1.35), 154 instruções
  no-op (sem alteração). Dezessete stubs da lista anterior desapareceram e
  onze novos alvos apareceram por cobertura/ownership reclassificados; não se
  atribui semântica aos novos destinos sem análise individual. Distribuição
  atual: 53 interiores, 22 com intervalos sobrepostos, 6 descobertos; por
  seção: `.text` 73, `DSOUND` 2, `.rdata` 2 e 4 não mapeados.
- Build Release nativo do host concluiu com warnings do tradutor/código gerado;
  CTest 2/2. Apenas o smoke `--help` foi invocado. Nenhum XBE ou host com
  `--launch` foi iniciado. Isso verifica geração, compilação e link — não boot,
  ABI ou fidelidade.

`NOT_READY` permanece. Próximo passo: classificar os onze alvos recém-expostos
e os 22 casos de ownership sobreposto, procurando evidência de entrada real e
validando os deltas de no-op/stub por categoria. Em paralelo, avançar o kernel
e o percurso de áudio com evidência; não iniciar o XBE sem autorização explícita
de bring-up.

## Etapa 1.37 — aliases de branch com fim de corpo compartilhado (2026-10-13)

- A revisão estática da regra 1.36 encontrou dois falsos positivos potenciais:
  branches para dentro da própria função de origem e alvos com múltiplos donos
  que, apesar de intervalos sobrepostos, descrevem o mesmo fim de corpo. A nova
  regra ignora branches internos e famílias já compartilhadas; permite alvo
  sobreposto apenas quando todos os donos concordam no mesmo `end`. Fontes com
  mais de um dono e alvos cujos donos discordam continuam sem alias.
- O patch `0003-alias-cross-function-conditional-branches.patch` foi ajustado e
  aplicado num checkout limpo do XboxRecomp v0.13.1 após 0001/0002. Testes
  focais: `22 passed`; suíte upstream: `607 passed, 40 skipped`.
- Reanálise da ISO sem escrita e sem executar o XBE: 15.690 entradas no mapa de
  funções; geração C: `15.702/15.702`, zero falhas, 62 arquivos-fonte, 64
  unidades compiladas. Stubs caíram de 81 para 71; permanecem 154 instruções
  emitidas como no-op. Dos 71 stubs, o auditor classifica 52 como interiores,
  13 com intervalos sobrepostos e 6 sem cobertura; isso descreve incerteza
  estática, não garante que todos sejam alcançáveis nem autoriza preenchê-los
  com retornos inventados.
- Build nativo Release e link sem símbolos indefinidos concluíram; CTest `2/2`.
  O self-test .NET passou `105/105`; o protótipo managed headless completou 80
  frames e reproduziu os traces de match/input/animation/audio. Esse replay é
  somente teste determinístico do protótipo authored, não do código recompilado
  nem prova de equivalência ao original.
- ISO permaneceu com tamanho `2.013.003.776` bytes, atributo `Archive`, data
  UTC `2026-01-22T10:03:26.0859951Z` e SHA-256
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
  Nenhum XBE ou host com `--launch` foi executado.

`NOT_READY` permanece. O avanço removeu dez stubs de uma geração para outra,
mas ainda não valida entradas/ABI, imports de kernel, inicialização, rendering,
integração de assets, gameplay, deformação ou áudio. Ver também
`FUT_STREET_2_ESCOPO_RECOMPILACAO_E_PRIORIDADES.md` para a trilha recomendada e
o que deve sair do caminho crítico sem apagar o histórico.

## Etapa 1.38 — espera de objetos dispatcher no bring-up (2026-10-13)

- O primeiro bring-up controlado do host recompilado chegou à entrada traduzida
  `0x0024D8BE`, carregou as 11 seções do XBE e resolveu os 128/128 thunks. O
  primeiro worker (`0x0024EAA2`) ficou repetindo `KeWaitForMultipleObjects`
  (ordinal 158) milhões de vezes em poucos segundos, com status de falha.
- A inspeção do call site `0x0024EC53` em `D3D.asm` mostrou `WaitAny` sobre um
  objeto local na stack. O header guest é montado diretamente (`Type=1`,
  `Size=4`), sem `KeInitializeEvent`; portanto, interpretar o endereço como um
  HANDLE nativo era incorreto. O patch `0004` faz a ponte consultar primeiro
  shadows existentes e, para eventos que o título constrói diretamente, usa o
  adaptador já existente e opt-in `ke_guest_event`. O estado de sincronização
  guest só é consumido depois de retorno bem-sucedido.
- A build Release e CTest `2/2` passaram. No host, `_putenv_s` e a variável de
  processo habilitam o adaptador antes da inicialização do kernel. Com o
  processo iniciado sem variável externa, o evento em `0x01181E00` foi
  reconhecido como evento de sincronização, a espera retornou `STATUS_WAIT_0`
  e o worker avançou para o ciclo de timers/DPCs. O volume caiu de milhões de
  falhas em segundos para aproximadamente 1.170 esperas em 20 s. Isso elimina
  o loop de falha observado, mas não demonstra que o ciclo periódico seja o
  único comportamento esperado nem que o jogo tenha concluído o boot.
- O processo nativo foi encerrado pelo limite diagnóstico de 20 s; não fechou
  espontaneamente e não exibiu um menu validado. Ainda foram atingidos
  `RECOMP_UNIMPL` em `D3D` para `out dx, al` em `0x00298030` (porta `0x80C0`) e
  `wbinvd` em `0x002954E4`. Os caminhos de I/O de hardware permanecem sem
  semântica; nenhum valor foi inventado. Esses pontos bloqueiam afirmações de
  rendering correto.
- Verificação de regressão .NET: Release com 0 avisos/erros, self-test `105`,
  headless `80 frames`, replay de match/input/animação/áudio byte-idêntico e
  smoke windowed `960x540`. São testes do runtime authored, não do XBE
  recompilado. CTest do host nativo: `2/2`; execução nativa: entrada traduzida
  e wait/timer observados, sem aprovação de rendering, assets, gameplay ou
  áudio.
- A ISO continuou fora das entradas de escrita; SHA-256 antes/depois dos
  smokes: `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.

`NOT_READY` continua. Próximo passo: modelar com evidência os acessos de I/O
`IN/OUT` à porta `0x80C0` no caminho D3D e determinar uma tradução nativa que
não se passe por emulação. Em paralelo, manter a matriz de instruções no-op e
stubs sendo reduzida por caso comprovado. Não declarar rendering, gameplay ou
áudio funcional a partir do bring-up do kernel.

## Etapa 1.39 — auditoria estática dos acessos D3D à porta 0x80C0 (2026-10-13)

- A leitura de `0x00296F1C` está em `sub_00296EA0`, registrada como rotina de
  interrupção no bring-up. Depois de verificar flags do contexto D3D, transforma
  o bit 5 do byte lido e grava estado em `[esi + 0x1D0]`; em seguida atualiza
  estado de fila e chama `KeInsertQueueDpc`. A transformação condicional do bit
  depende dos bits altos do campo `[esi + 8]`.
- A leitura em `0x0029711D`, dentro de `sub_002970A0`, combina o bit derivado do
  `IN` com estado local, grava novamente `[esi + 0x1D0]` e depois sinaliza um
  evento kernel. O mesmo fluxo consulta estado de dispositivo enquanto espera.
  Isso mostra dependência observável de estado, mas não revela qual evento de
  hardware o bit representa.
- A escrita observada em `0x00298030` envia `BL` para a mesma porta. A revisão
  integral de `sub_00297F04` corrigiu a hipótese anterior: `EBX` é zerado e
  incrementado no prólogo, permanece preservado pelas chamadas segundo o ABI
  x86 e não é novamente escrito antes do `OUT`; portanto, o valor enviado é
  `1`. O fluxo inicializa buffers e chama duas vezes uma função obtida por
  ponteiro antes de continuar a configuração. O valor conhecido ainda não
  identifica a semântica do registrador/porta.
- `wbinvd` em `0x002954E4` segue uma escrita guest em `[0x80000000]` e precede
  `sub_00293320`. A proximidade sugere relação com o caminho de hardware/cache,
  mas não prova uma equivalência com fence de CPU ou com comando gráfico nativo.
- A consulta da tabela pública de portas do XboxDevWiki coloca `0x8000–0x80FF`
  na faixa rotulada `LPC PM`; portanto `0x80C0` está nessa faixa, não na faixa
  de memória mapeada de registradores NV2A (`0xFD000000–0xFDFFFFFF`). A própria
  tabela marca o mapa I/O como incompleto (`FIXME`) e não documenta o offset
  `0xC0`; isso localiza a classe do barramento, mas não define o significado do
  registrador nem do bit 5. Fonte: [XboxDevWiki — Memory / I/O port map](https://xboxdevwiki.net/Memory).
- O bring-up existente observou `OUT` e `wbinvd`, mas não registrou execução das
  duas leituras em `sub_00296EA0`/`sub_002970A0`. Busca em fontes primárias
  públicas pelo endereço/porta não estabeleceu a semântica. Nenhum adapter ou
  valor substituto foi implementado nesta etapa.
- Repetição diagnóstica isolada: carregou 11/11 seções e resolveu 128/128
  thunks, chamou `0x0024D8BE` e observou novamente apenas `OUT`/`wbinvd`; as
  leituras `IN` não apareceram no log em 8 s. O host foi encerrado no limite
  diagnóstico, sem menu/boot validado. Save e logs ficaram em `%TEMP%`; não
  houve processo remanescente. SHA-256 da ISO após o run:
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.

Conclusão operacional: manter essas instruções como não resolvidas e investigar
o estado de dispositivo/estrutura, a origem de `BL`, o ponteiro de callback e
uma referência técnica primária para a porta. Um retorno constante, no-op,
barreira genérica ou evento vblank presumido poderia alterar sincronização e
corromper o rendering. O README foi sincronizado com a etapa 1.38; nenhum arquivo
do XBE/ISO foi alterado ou publicado.

## Etapa 1.40 — correspondência do GPIO LPC PM com o bit lido pelo D3D (2026-10-13)

- A implementação de referência do xemu em seu PR público `#2851` define
  `XBOX_PM_IO_BASE = 0x8000` e `XBOX_PM_GPIO_BASE = 0xC0`, registrando a janela
  GPIO em `0x80C0`. Para offset zero, ela alterna um estado chamado
  `field_pin` e retorna esse estado no bit 5. O próprio comentário da
  implementação pergunta se seria o “field pin” do encoder de TV; o PR também
  deixa investigação de GPIO como TODO. O PR permanece aberto.
- Isso corresponde de forma específica à leitura guest em `0x80C0`, seguida de
  `SHR 5`, inversão e máscara em `sub_00296EA0`/`sub_002970A0`. A hipótese mais
  forte agora é que esse fluxo consulta sincronização/paridade de campo de
  vídeo por GPIO LPC PM, em vez de um registrador NV2A. A escrita em offset
  zero com valor `1` ainda não tem semântica explicada nessa referência.
- A fonte é uma implementação de emulador, não uma especificação do hardware;
  seu código é explicitamente heurístico/incompleto. Foi consultada somente
  para pesquisa, não copiada nem integrada. Portanto isso orienta a busca, mas
  ainda não autoriza binding no host nativo. O bring-up local de 8 s não chegou
  às instruções `IN`, então não há confirmação dinâmica do ciclo de campo.

Referências consultadas: [PR xemu #2851 — expor portas GPIO](https://github.com/xemu-project/xemu/pull/2851)
e [fonte do controlador GPIO no commit do PR](https://github.com/xemu-project/xemu/blob/5938def/hw/xbox/acpi_xbox.c).
Próximo passo: procurar confirmação independente do registrador/encoder e do
efeito da escrita `1`; só então decidir se há uma tradução nativa de sincronismo
de apresentação compatível, sem simular a GPU ou inventar estado.

## Etapa 1.41 — bloquear o opt-in de vblank sintético no host (2026-10-13)

- A auditoria do bridge do kernel no checkout temporário XboxRecomp v0.13.1
  encontrou `kernel_vblank_tick()`: se `RECOMP_VBLANK` estiver presente, ele
  agenda um tick fixo de aproximadamente 60 Hz, grava bits de interrupção em
  endereços guest NV2A e chama o ISR do vetor 3. Isso é uma fonte sintética de
  evento GPU, não sincronização obtida do backend nativo de apresentação.
- O bootstrap do projeto já bloqueava `RECOMP_AC97_READY` e `RECOMP_PB_EXEC`,
  mas aceitava `RECOMP_VBLANK`. Ele agora também recusa essa variável antes de
  abrir o XBE ou os diretórios de dados. Foi adicionado um teste que passa
  caminhos fictícios e verifica código de saída `2` e a mensagem de recusa;
  portanto não executa o bring-up nesse teste.
- Build nativo Release passou; CTest `3/3`, incluindo
  `recompiled_host_rejects_synthetic_vblank`. Regressão .NET passou: build
  Release `0/0`, self-test `105/105`, runtime authored headless `80 frames`,
  replay match/input/animação/áudio byte-idêntico, smoke windowed `960x540` e
  runner `10/10`. Esses smokes authored não são evidência de fidelidade do XBE.
- O bring-up nativo não foi reexecutado nesta etapa; a mudança só reforça o
  guard e o teste CTest comprova o caminho de rejeição antes de qualquer acesso
  à ISO/XBE. A ISO permaneceu somente leitura; SHA-256:
  `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.

`NOT_READY` permanece. O próximo avanço real do jogo depende de uma fonte nativa
de sincronização de apresentação e de uma semântica confirmada para o GPIO
`0x80C0`; não ligar o vblank sintético para produzir progresso aparente.

## Etapa 1.42 — delimitar evidência pública para LPC PM GPIO (2026-10-13)

- Uma busca dirigida encontrou no PR aberto do xemu #2851 uma evidência
  diferente, que deve ser separada da hipótese de field pin: o autor relata que
  um kernel público lê `0x80CE`, testa o bit `0x20` e decide entre encoders
  Conexant e Focus; para o Xbox 1.0 que o xemu toma como padrão, a resposta
  esperada nesse endereço é `0x00`. Isso documenta um uso de `0x80CE` em outro
  kernel/revisão, não o significado de `0x80C0` usado por FIFA Street 2.
- No mesmo patch do xemu, a leitura do offset `0` (porta `0x80C0`) alterna um
  bit chamado `field_pin`, mas o comentário literal pergunta se seria o pino
  FIELD do encoder. Essa linha é uma hipótese do modelo de emulação, não uma
  especificação nem uma confirmação independente. O patch está aberto e o
  comportamento não foi adotado.
- O Xbox Hardware Design Specification 1.02 §2.6.5–2.6.6 lista GPIOs do MCPX
  associados a VBUS de portas, SMI e status SCART; não estabelece nesse trecho
  uma ligação entre eles e o offset LPC PM `0xC0`, portanto não resolve o
  registrador do jogo. A tabela comunitária de portas continua marcada FIXME.
- Releitura estática do XBE: `IN AL,DX` em `0x00296F1C` lê especificamente
  `DX=0x80C0`, após guardas de estado/contexto; o byte é reduzido ao bit 5,
  invertido e usado para atualizar estado D3D e enfileirar DPC. A escrita em
  `0x00298030` envia o byte `1` ao mesmo offset, mas o efeito dessa escrita
  também permanece desconhecido. Não existe binding confirmado para leitura,
  escrita, ISR ou apresentação.
- Os logs preservados do bring-up não contêm telemetria `[D3D]` que demonstre
  `Present` alcançado; a rota `D3D8_PresentFrame` do checkout temporário também
  não emite essa telemetria. Logo, ainda não há evidência para conectar
  apresentação nativa ao ISR do jogo. O vblank sintético continua bloqueado.

Fontes consultadas: [PR xemu #2851, comentário do autor sobre `0x80CE`](https://github.com/xemu-project/xemu/pull/2851),
[código GPIO no commit do PR](https://github.com/xemu-project/xemu/blob/5938def/hw/xbox/acpi_xbox.c)
e [Xbox Hardware Design Specification 1.02, §2.6.5–2.6.6](https://consolemods.org/wiki/images/a/ae/Xbox_Hardware_Design_Specification_1.02.pdf).
Essas fontes delimitam a hipótese, mas não definem `0x80C0` para este título.

Próxima etapa: buscar evidência independente e específica do offset `0x80C0`
e do `OUT 1` (por exemplo, outro kernel Xbox/documentação de MCPX/LPC ou uma
captura controlada de execução); manter `IN/OUT` sem implementação até isso ser
resolvido. A ligação `Present→ISR` só pode ser avaliada depois de provar que a
rota de apresentação do título é alcançada e de determinar a semântica do
evento. Nenhum XBE foi executado nesta etapa.

`NOT_READY` permanece; nenhum gate principal mudou. A ISO continua somente
leitura e não foi tocada nesta investigação.

Validação local desta etapa documental: `tools/run-clean-room-authored-matrix.ps1`
concluiu com exit 0 (`completed-authored-and-candidate-opt-in`), Release 0
avisos/0 erros, self-test .NET 105/105 e runner 10/10; smokes relevantes,
runtime authored headless/windowed, captura 960×540 e replays determinísticos
dos traces de match/input/animação/áudio passaram. Readiness permaneceu
`NOT_READY`, sem promover resultados authored a evidência do XBE. Build nativo
Release não tinha alvos pendentes; CTest 3/3 passou (`no_emulation_runtime_support`,
`recompiled_host_help`, `recompiled_host_rejects_synthetic_vblank`). Nenhum
`--launch` foi executado. `git diff --check` passou. ISO: 2.013.003.776 bytes,
atributo `Archive`, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 1.43 — tradução parcial de WBINVD para memória coerente (2026-10-13)

- O bring-up registrou execução de `WBINVD` em `0x002954E4`. O código traduzido
  grava um DWORD em `0x80000000` logo antes da instrução e chama
  `sub_00293320` logo depois. O endereço está dentro da janela de RAM contígua
  mapeada pelo host; não há evidência neste caminho de acesso a uma API de
  dispositivo que possa ser confundida com esse opcode.
- A referência primária [Intel SDM, WBINVD](https://cdrdv2-public.intel.com/671143/334569-sdm-vol-2d.pdf)
  define write-back e invalidação das caches internas, além de serialização.
  Um recompiler estático não mantém uma cache de CPU guest separada: para a
  RAM coerente mapeada no host, a tradução versionada em
  `native_recomp/patches/0005-lift-wbinvd-coherent-host-memory.patch` emite
  `atomic_thread_fence(memory_order_seq_cst)` via `RECOMP_WBINVD()`.
- Limite deliberado: a barreira preserva ordenação das operações de memória
  traduzidas; não invalida as caches físicas do host e não implementa
  coerência/flush para GPU ou DMA. Qualquer backend de dispositivo ainda terá
  de tratar sua visibilidade de recursos explicitamente. Nenhum binding Xbox,
  NV2A ou ISR foi criado.
- O patch foi aplicado em uma cópia isolada dos fontes temporários; o checkout
  XboxRecomp com alterações preexistentes permaneceu intacto. Os novos testes
  `test_lifter_wbinvd.py` passaram `2/2`. O chunk gerado contendo o site, após
  aplicação da instrução emitida, compilou em uma biblioteca estática Release
  de validação com o dispatch; houve 38 warnings preexistentes de parênteses
  redundantes, sem erros. Isso valida o lowering e sua compilabilidade, não
  uma nova geração completa nem um boot do jogo.
- A geração usada pelo executável host ainda contém `RECOMP_UNIMPL` nesse site;
  ela não foi regenerada nesta etapa. Assim, as contagens vigentes continuam
  sendo 71 stubs e 154 instruções não traduzidas. Próximo passo: aplicar os
  patches versionados sobre uma cópia limpa/fixada do XboxRecomp, regenerar as
  15.702 funções, compilar o archive completo e observar qual operação alcançada
  passa a ser a próxima dependência real. `OUT 1` em `0x00298030` e a semântica
  de `0x80C0` continuam sem binding.
- Nenhum XBE ou executável recompilado foi lançado; readiness permanece
  `NOT_READY`. Os testes .NET authored não são validação do código original.
