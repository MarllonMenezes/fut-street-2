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
