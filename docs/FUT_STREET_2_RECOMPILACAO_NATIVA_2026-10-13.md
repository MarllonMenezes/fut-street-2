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
