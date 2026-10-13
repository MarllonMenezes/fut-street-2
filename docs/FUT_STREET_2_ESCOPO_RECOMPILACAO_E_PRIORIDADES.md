# Fut Street 2 — escopo e prioridades da recompilação

## Objetivo

Fut Street 2 pretende traduzir estaticamente o código original de FIFA Street 2
para um executável nativo de PC, no estilo de projeto de jogo recompilado como
[re:Blue](https://github.com/zolaware/reblue). Não é uma recriação de gameplay
com regras novas. O recompiler deve traduzir o código original; o runtime do
projeto deve fornecer serviços de plataforma compatíveis e os caminhos de
renderização, áudio, input e arquivos necessários para esse código funcionar.
O XBE não é recompilado nem executado no PC como XBE.

O re:Blue é um exemplo de recompilação estática de Blue Dragon e requer que a
pessoa forneça suas próprias imagens legais; o repositório não inclui os assets
originais. É uma referência de objetivo e organização, não um runtime que possa
ser reutilizado diretamente para o Xbox original. Para esta arquitetura x86 do
Xbox original, XboxRecomp é a ferramenta em avaliação; sua própria documentação
separa o pipeline que gera C do projeto por jogo que monta o executável e
implementa o runtime específico.

## Estado verificável

Na geração mais recente registrada em 2026-10-13, o XboxRecomp v0.13.1
produziu C para 15.702/15.702 funções; 64 unidades foram compiladas em Release
e o host linkou. Restam 71 destinos stub e 94 instruções sem lowering (29
mnemônicos). CTest passou 4/4 e o smoke `--help` passou. Um bring-up controlado
anterior alcançou a entrada traduzida e atividade de worker/timer, mas não
validou boot/menu; nenhum XBE foi executado no marco 1.49. Não existe
executável jogável validado. Portanto, isso é avanço de tradução/compilação —
não prova de fidelidade ou conclusão de gates. Estado global: `NOT_READY`.

## Caminho crítico recomendado

1. **Congelar a base reproduzível:** fixar a versão/commit do XboxRecomp e os
   patches locais; guardar relatórios e o código derivado apenas localmente.
2. **Fechar a tradução alcançável com evidência:** classificar os 71 stubs, os
   154 no-ops e os imports. Corrigir o detector/lifter quando houver um caso
   reproduzível; criar override só com evidência de endereço, bytes, ABI, fluxo
   e comportamento esperado. Não buscar “zero stubs” à força.
3. **Decidir e implementar o bring-up:** memória/sections do guest, imports do
   kernel usados no boot, ABI, exceções, threads, arquivos e chamadas indiretas.
   Antes de executar o host com `--launch`, registrar autorização explícita e
   definir dados locais de teste; não rodar o XBE diretamente.
4. **Destravar vídeo e rendering:** instrumentar o caminho que o código original
   realmente chama; converter comandos/estados e dados gráficos para APIs do PC;
   integrar texturas, geometrias, shaders e animações reais por evidência.
5. **Destravar áudio e input:** atender às APIs chamadas pelo jogo, mapear
   eventos e assets de áudio reais, e validar sincronização, mixagem e controles.
6. **Validar a experiência original:** boot até menu, partida, gameplay e
   encerramento; comparar imagens, ações, áudio e estabilidade usando a cópia
   legítima local como entrada. Os testes authored/headless continuam sendo
   regressão de ferramentas, nunca substitutos dessa validação.

Os gates de assets, rendering, gameplay/animação e áudio dependem da integração
ao código recompilado. Ler ou decodificar um asset isoladamente não fecha o gate.
Não estimar uma data final até o primeiro boot funcional; os riscos dominantes
são imports/ABI/controle indireto e os caminhos de GPU/áudio não testados.

## Limite de “sem emulação”

Para este projeto, “sem emulação” deve significar que a CPU do Xbox não é
interpretada nem recompilada dinamicamente durante a execução: o código
traduzido roda como código nativo. Isso não elimina a necessidade de uma camada
de compatibilidade para APIs, memória, input, vídeo e áudio. A tradução de
chamadas D3D8 para uma API gráfica de PC pode ser um backend/compatibilidade; já
executar comandos NV2A por um emulador de GPU ou simular a APU Xbox é outra
decisão arquitetural e permanece excluído da configuração atual.

O runtime upstream de XboxRecomp contém componentes opcionais de emulação NV2A
e APU. Eles não devem entrar no host enquanto a política atual proíbe emulação.
Sem um backend gráfico e de áudio aceitável dentro desse limite, rendering e
áudio não podem ser considerados resolvidos. Um smoke que só compila o host ou
desativa processamento de pushbuffer não satisfaz esses gates.

## O que manter, rebaixar ou arquivar

| Área existente | Tratamento recomendado | Motivo |
| --- | --- | --- |
| `native_recomp/`, patches do XboxRecomp, bootstrap e testes C | Manter no núcleo | Caminho da tradução estática e do runtime nativo. |
| Analisadores de XBE/assets e decoders .NET | Manter como ferramentas auxiliares quando alimentarem evidência ou assets integráveis | São úteis para identificar formatos e validar dados, mas não recompilam o jogo. |
| Protótipo authored de gameplay/renderização/áudio em `fifa_pc` | Tirar do caminho crítico; no futuro, mover para `prototype/` ou repositório/branch de arquivo | Regras e imagens novas não são código recompilado nem prova de fidelidade. |
| Demos, fixtures sintéticas e replay do protótipo | Manter apenas como testes unitários explicitamente rotulados | Validam determinismo e infraestrutura, não o comportamento de FIFA Street 2. |
| Relatórios exploratórios duplicados ou sem consumidor | Parar de gerar; arquivar localmente ou consolidar em relatório de evidência | Evita confundir volume de auditorias com progresso de execução. |
| ISO, XBE extraído, assets, C gerado e objetos | Manter locais/ignorados; não publicar | São entrada ou derivados proprietários; a ISO permanece intacta e somente de leitura operacional. |

Esta recomendação não apaga nem move arquivos agora. Primeiro preservar a
história Git e separar os testes necessários; depois uma mudança de organização
pode retirar o protótipo do build e da página inicial sem destruir artefatos.

## Referências técnicas

- [re:Blue — repositório e requisitos de assets próprios](https://github.com/zolaware/reblue)
- [XboxRecomp — pipeline e arquitetura](https://github.com/sp00nznet/xboxrecomp)
- [XboxRecomp — guia de início e separação entre toolkit e projeto por jogo](https://github.com/sp00nznet/xboxrecomp/blob/main/docs/GETTING_STARTED.md)
