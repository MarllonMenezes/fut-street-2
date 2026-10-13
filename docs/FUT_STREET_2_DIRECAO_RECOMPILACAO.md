# Fut Street 2 — direção: recompilação estática

Direção esclarecida pelo usuário em 2026-10-13, America/Sao_Paulo.

## Objetivo, não resultado já alcançado

O objetivo é **recompilar estaticamente o código original de FIFA Street 2
do Xbox original para PC**, traduzindo-o para código compilável e executando
a lógica original nativamente. Não é recriar o gameplay com código novo.

O [re:Blue](https://github.com/zolaware/reblue) é a referência de abordagem:
traduz o código original de Blue Dragon por recompilação estática e usa o
ReXGlue SDK. Isso não significa usar a mesma ferramenta para este título.

O [XboxRecomp](https://github.com/sp00nznet/xboxrecomp) descreve um pipeline
de x86 do Xbox original para C, compilado em executável nativo para PC.
É candidato técnico, não suporte específico de FIFA Street 2 comprovado
neste workspace. Gerar C não basta: ainda há runtime, chamadas indiretas,
serviços do console e integração gráfica/áudio a validar.

## Estado real preservado — atualização 2026-10-13

- C/C++ está autorizado para o núcleo traduzido; .NET permanece para ferramentas
  auxiliares e o protótipo existente. O objetivo continua sendo executar a
  lógica original via tradução estática, sem intérprete/JIT de CPU.
- XboxRecomp v0.13.1 foi fixado no commit `193e2995ffaec871bb3be43a43bd66aa5fccfeb1`.
  A análise classificou 15.605 funções; a geração de C concluiu 15.605/15.605.
- As 65 unidades C foram compiladas como objetos COFF x86_64 Windows e os 65
  objetos foram arquivados numa biblioteca estática local. Isso é uma etapa
  intermediária de tradução/compilação, **não** um executável do jogo, não um
  link completo do runtime, não prova de execução ou de equivalência.
- O gerador ainda substituiu 86 destinos de chamada por stubs e emitiu 155
  instruções como no-op (37 mnemonics, após corrigir dois movimentos SSE
  inteiros usados no XBE). Esses casos precisam de auditoria/correção; não se
  presume que sejam irrelevantes; o classificador marca 35 em funções CRT/
  render/game-vtable e 120 em funções desconhecidas. Dos 86 stubs, 79 não têm
  metadado `ret N` recuperado. O ABI contém inferências e ainda não está
  validado semanticamente.
- O arquivo XBE não foi executado como um XBE. Um host nativo experimental
  carregou suas seções e chamou o código traduzido, mas não validou boot/menu;
  os detalhes e limites estão na etapa 1.38 do relatório nativo. A ISO
  permaneceu somente leitura e com o SHA-256 esperado. XBE, C gerado, objetos e
  biblioteca ficam fora do GitHub.
- Rendering original, assets, áudio e animação/deformação continuam sem gates
  aprovados. Testes/headless/replay do protótipo .NET não validam o original.
  Readiness permanece `NOT_READY`.

## Política de runtime e hardware

A recompilação estática do CPU foi iniciada. Isso não autoriza esconder emulação
de hardware sob o termo "nativo": runtime de GPU/APU do XboxRecomp não foi
integrado nem executado. Primeiro é preciso avaliar mapeamento de chamadas,
dependências, alternativas nativas e quais componentes realmente exigem
emulação; qualquer decisão que mude o requisito "sem emulação" deve ser
explicitada antes da integração. Não inventar bindings nem tratar stubs como
implementações.

## Continuidade e proteção dos dados

Esta direção prevalece sobre as antigas descrições de reimplementação .NET
como objetivo final e de XboxRecomp somente como auxiliar estático. Registros
históricos e resultados anteriores são preservados, não reescritos como
evidência de recompilação. Nenhum código funcional mudou nesta atualização.

Preservar todas as alterações. A ISO
`D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso` continua exclusivamente
entrada somente leitura, com SHA-256 esperado
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Não publicar ISO, XBE, assets originais ou dumps neste repositório.
Não inventar bindings, declarar conclusão ou iniciar code review.

## Continuação após a etapa 1.43

Foi versionado `native_recomp/patches/0005-lift-wbinvd-coherent-host-memory.patch`.
O `WBINVD` alcançado no bring-up em `0x002954E4` é traduzido para uma barreira
C11 sequencialmente consistente sobre memória coerente do host. Isso preserva
ordenação das operações de memória traduzidas; não simula invalidação de caches
nem implementa flush para GPU/DMA. Testes isolados do lifter passaram `2/2` e o
chunk que contém o endereço compilou em uma biblioteca estática de validação.
Uma geração completa isolada produziu novamente as 15.702 funções em 62
arquivos, com 71 stubs e 139 instruções não traduzidas (36 mnemonics), contra
154 antes. O archive com as 64 unidades C compilou em Release e o host foi
linkado; nenhum XBE foi iniciado. Os arquivos gerados e o build permanecem em
`%TEMP%`, fora do Git; a saída gerada pelo host local anterior não foi
substituída.

A etapa 1.42 consultou evidência pública sobre a faixa LPC PM. Um kernel público
usa `0x80CE` para seleção de encoder, mas isso não define o `0x80C0` lido/escrito
por FIFA Street 2; o bit chamado `field_pin` no xemu é uma hipótese explícita,
não um binding. A próxima etapa é resolver por evidência os acessos D3D
`IN/OUT` a `0x80C0`, auditar stubs/opcodes no-op alcançáveis e completar
serviços de kernel necessários ao bring-up. O link e o primeiro bring-up
controlado já foram realizados, mas não validam menu nem gates do jogo. Manter
Release, self-tests, smokes relevantes, headless, replay determinístico quando
aplicável e SHA-256 da ISO em cada marco, sempre distinguindo testes do protótipo
.NET de validações do núcleo recompilado. Não há prazo de conclusão.

## Continuação após a etapa 1.45 (2026-10-13)

A pilha versionada 0001–0006 foi aplicada em ordem a um clone limpo do
XboxRecomp v0.13.1, fixado no commit `193e2995ffaec871bb3be43a43bd66aa5fccfeb1`;
os hunks dos patches 0002/0003 foram corrigidos. O patch 0006 implementa seis
sites packed-word XMM/MMX, reduzindo as instruções sem lowering de 139/36
mnemonics para 133/32; 71 stubs permanecem. A geração completa e o CMake Release
foram refeitos, o host linkou e `--help` + CTest 3/3 passaram. A suíte focada
Python `tools/disasm` + `tools/recomp`: 517 passou, 38 skipped. A coleta global
de pytest ainda depende do pacote ausente `pefile` para `tools/fusion`.
Build/tradução não provam boot ou fidelidade. Nenhum XBE/`--launch` foi executado;
headless/replay não exercitam os opcodes nativos alterados. ISO somente leitura;
readiness `NOT_READY`. Próxima tarefa: resolver o próximo grupo de opcodes/stubs
por evidência estática, sem assumir binding ou alcançabilidade.

## Validação desta atualização documental — 2026-10-13

Registro histórico da primeira atualização documental, antes do trabalho de
tradução/build nativo descrito em
[FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md](FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md).
Naquela rodada, nenhum fonte funcional, runtime, binding ou gate foi alterado.

- Release: 0 avisos e 0 erros.
- Self-tests .NET: 105/105; testes do runner Release: 10/10.
- Smoke de identidade/metadados, runtime headless de 80 frames e replay de
  match/input/animation/audio byte-idêntico: aprovados.
- Runtime windowed do protótipo e captura 960×540: aprovados.
- Links de direção nos dez documentos existentes: 10/10 válidos;
  `git diff --check`: aprovado.
- ISO conferida antes/depois: SHA-256 esperado, tamanho, atributos e
  LastWriteTimeUtc preservados.

Scripts: `tools/test-fut-street-product-identity.ps1` e
`tools/test-clean-room-release-invocation.ps1`. Esta rodada é focal, não uma
nova matriz completa. Ela não testa recompilação nem execução do código
original e não altera o estado `NOT_READY`.
