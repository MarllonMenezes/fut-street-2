# FIFA Street 2 — estado real e plano de otimização

## Atualização de direção — 2026-10-13

O usuário confirmou recompilação estática do código original como objetivo,
não uma recriação em .NET. Leia
[direção vigente](FUT_STREET_2_DIRECAO_RECOMPILACAO.md).
O plano abaixo preserva decisões do escopo anterior: a restrição de usar
XboxRecomp somente para análise não é a definição do produto desejado.
Nenhum runtime foi adotado por esta atualização; stack e política de GPU/áudio
pendem de confirmação. XBE ainda não recompilado; `NOT_READY`.

Data da avaliação inicial: 2026-10-09, America/Sao_Paulo.
Pesquisa e aplicação mais recentes: 2026-10-13;
ver [pesquisa de aceleração e etapa 1.26](FIFA_STREET_2_ACELERACAO_PESQUISA_2026-10-13.md).
Otimização aplicada à validação:
[busca de áudio com artefatos idênticos, etapa 1.27](FIFA_STREET_2_VALIDACAO_OTIMIZADA_2026-10-13.md).
Workspace: `D:\projeto zeta`.
Objetivo mantido: reimplementação clean-room em .NET, com os gates de
rendering, assets, gameplay e áudio integrados e verificados. Animação e
deformação continuam dependências explícitas. O projeto permanece `NOT_READY`.

Este documento organiza o trabalho e referencia avanços documentados; os
critérios de aprovação continuam vigentes e o projeto não está concluído.

## 1. Estamos no início, na metade ou no final?

Já existe uma base executável, com leitura de containers, decoders de payload,
simulação, renderer de software, áudio e replay. Porém, ainda estamos em uma
fase inicial/intermediária de comprovar e integrar o comportamento e os assets
originais. Não há evidência para afirmar que o projeto completo passou da
metade, nem que esteja na fase de acabamento.

O runtime atual é um protótipo útil para implementação e testes. Por exemplo,
`StreetMatch.cs` mantém posições Home/Away e uma bola, e `PreviewRenderer.cs`
desenha uma quadra authored e personagens procedurais ou candidatos rígidos.
Esses componentes não demonstram, por si, a reprodução do jogo inteiro.

| Área | Estado confirmado | O que falta para o gate |
| --- | --- | --- |
| Integridade da ISO | `verified` | Preservar bytes e repetir a verificação exigida |
| Gameplay | `clean-room-integrated` | Gate atual aprovado; cobertura e comparação comportamental mais amplas continuam necessárias para alegar completude do jogo |
| Assets | `candidate-integrated` | Provar a ligação entre mesh, textura e material usados pelo draw |
| Rendering | `not-proven` | Provar estado → geometry/draw e interpretação independente de UV/material |
| Áudio | `partially-integrated` | Provar EVT → banco/sample → seleção/playback, além das rotas authored |
| Animação/deformação | `structural-only` | Decoder validado para timing → pose → rig → skinning/morph |

Há um gate funcional principal aprovado entre os quatro pedidos inicialmente;
animação/deformação também permanece pendente. Essa contagem não representa
25% do esforço: os gates têm tamanhos diferentes e dependem de descobertas.
Os subgates verdes de estrutura ou de runtime authored não medem fidelidade
original. Fechar o relatório atual também não substitui uma lista de requisitos
do produto completo.

## 2. O gargalo observado

Os últimos experimentos reconciliaram relatórios e confirmaram ausência de
bindings. São controles válidos, mas os seguintes domínios ficaram intactos:

| Controle atual | Resultado relevante |
| --- | --- |
| State/geometry/draw global | `36 → 36` modelos; redução `0`; bindings únicos `0` |
| Catálogo/pose/timing global | `7.429 → 7.429` modelos; redução `0`; bindings únicos `0` |
| Contexto estático de áudio global | `952 → 952` modelos; redução `0`; seletores únicos `0` |
| Identidade PCM EVT/bancos | `17` eventos e `241` candidatos decodificados; matches exatos `0` |
| Observações externas de timing | `missing-evidence`; observações válidas `0` |

Reparsear um TSV por outra implementação comprova reprodutibilidade. Isso
sozinho não fornece evidência independente do significado de seus campos.
Um relatório novo precisa responder uma pergunta que as fontes disponíveis
possam distinguir. Recombinar os mesmos nomes, counts e assinaturas sem uma
chave adicional pode repetir a ambiguidade indefinidamente.

O caminho mais rápido dentro do objetivo atual é investir no dado ou contrato
que falta e em uma integração verificável. Não há garantia de que uma fonte
externa resolverá todos os bindings; ela precisa ser avaliada pelo que prova.

## 3. Ordem de trabalho recomendada

### A. Definir a cobertura final e a próxima pergunta verificável

1. Manter uma lista curta de requisitos e evidências para o jogo completo:
   rendering, personagens/equipes, quadras, regras, controles, fluxo de menus,
   modos, persistência e áudio. Cada item deve ficar como comprovado, parcial,
   ausente ou ainda não avaliado; não presumir implementação a partir de um
   teste de partida simples.
2. Referenciar os relatórios existentes e preservar seus controles negativos.
   Não apagar histórico nem alterar gates para facilitar aprovação.
3. Para cada experimento, escrever a hipótese, a fonte nova, os resultados
   que distinguem alternativas e a condição de abandono antes de programar.
4. Priorizar rendering: resolver state/draw e UV/material pode também
   destravar parte do gate de assets e tornar o progresso visível no runtime.

Entregável: um backlog com dependências e um experimento de rendering que
possa ser refutado. Um novo título de etapa ou mais linhas de relatório não
contam como descoberta sem esse efeito.

### B. Fechar uma primeira cadeia de rendering com prova

Selecionar um objeto já extraído e auditado, por exemplo `tops0030.ord`, e
seguir bytes, offsets e relocations até os consumidores relevantes. O recorte
serve para descobrir e validar o contrato; não encerra o gate global.

Investigar explicitamente:

- quais registros fornecem indices e atributos de cada primitiva;
- como o consumidor estabelece a relação entre state e draw;
- qual declaração, operação ou comparação independente identifica UV;
- como textura, sampler e material são selecionados;
- quais interpretações alternativas produzem resultados distintos.

Fontes possíveis: bytes originais lidos da ISO, análise estática direcionada do
consumidor e referências de formato com proveniência. Evitar expandir todo o
grafo de chamadas quando não existe uma pergunta sobre um campo específico.
Os papéis de campos devem vir de evidência e controles, não de uma imagem
visualmente plausível ou de valores na faixa 0–1.

Entregável: parser/decoder novo com offsets rastreáveis, teste que rejeita uma
interpretação errada, cena de comparação e replay. Validar depois em objetos
com estruturas diferentes e em um conjunto reservado que não orientou a
hipótese. Se persistirem interpretações equivalentes, continuar como candidato.

### C. Investigar áudio e animação em frentes independentes

**Áudio:** procurar o contrato ou seletor que relaciona o evento ao banco e à
amostra. Não assumir que um PCM EVT deva ser idêntico a um sample de banco:
o resultado negativo atual não identifica o algoritmo de seleção. O teste
necessário deve acompanhar a chave/índice observado até o decoder e a rota de
playback, incluindo casos que distingam uma escolha errada.

**Animação:** partir de um registro/binário cujo layout possa ser relacionado
ao consumidor ou a observações verificáveis. Resolver tempo, transformação e
hierarquia antes de aplicar skinning. Datas de catálogo e semelhança de nomes
não identificam duração, frames, rig ou pesos.

Capturas existentes da edição Xbox/USA, fornecidas com origem e versão, podem
ajudar a comparar movimentos, entradas e áudio. Cada observação deve registrar
captura, edição, cenário, input e base temporal. Um vídeo de gameplay não
revela automaticamente nome de banco, asset interno ou índice de sample.
Campos internos só devem ser preenchidos quando houver evidência dessa relação.

O validador `ExternalAnimationTimingEvidenceAudit.cs` verifica formato e
consistência das observações. Ele mantém `direct_timing_binding=not-proven`;
preencher um TSV sintaticamente válido não fecha timing/pose/rig.

Entregáveis: uma seleção de áudio e uma sequência de animação demonstradas
com proveniência, controles e integração, seguidas da generalização para os
dados restantes. Falta de evidência externa não elimina as alternativas de
análise estática local; escolher a fonte conforme a pergunta.

### D. Integrar e ampliar a cobertura do jogo

Depois da primeira cadeia comprovada, integrar gradualmente os dados no runtime
.NET e testar uma partida controlada. Comparar comportamento com referências
adequadas à edição e registrar divergências. Generalizar o decoder antes de
ampliar cenas e conteúdo; validar rejeições e casos incompletos.

Concluir todos os requisitos do backlog e todos os gates com provas adequadas
ao seu escopo. Uma cena, um som ou uma animação são marcos intermediários.
O gameplay atual aprovado continua sujeito à cobertura do produto, incluindo
regras, participantes, modos e interação que o objetivo final exigir.

## 4. Como reduzir tempo desperdiçado sem enfraquecer validação

- Escolher um único experimento principal por frente e medir o que ele
  resolveu. Se não houver informação nova, registrar o negativo e trocar a
  fonte ou a hipótese antes de criar outro join equivalente.
- Reutilizar os parsers, dumps e controles existentes. Para caches de extração
  somente leitura, identificar ISO, entrada, offset, tamanho, hash e versão
  do decoder; não confundir cache antigo com execução atual.
- Durante desenvolvimento, usar fixtures pequenas e smokes direcionados.
  Ao fechar uma etapa, executar toda a validação já exigida. O plano não
  autoriza omitir a matriz final ou a leitura integral inicial dos handoffs.
- Paralelizar leituras e investigação de áreas independentes. Builds, suites
  e produtores dos mesmos relatórios devem manter ordem e saídas isoladas.
  Não executar simultaneamente matrizes que sobrescrevam os mesmos TSVs.
- Manter um resumo atual de decisões com links para o histórico. Reduzir
  duplicação futura de texto sem apagar arquivos nem dispensar as instruções
  de leitura vigentes.
- Medir duração das fases e custo das varreduras antes de alterar o runner.
  A matriz tem aproximadamente 293 KB e o auditor de readiness 822 KB;
  tamanho não demonstra sozinho gargalo de execução.
- Adiar troca de engine/backend e otimização de FPS até medir sua necessidade.
  Nenhuma dessas trocas, por si, prova os bindings que bloqueiam os gates.

Nenhuma dessas alterações de código, cache ou runner foi aplicada por este
documento. São próximas ações recomendadas, sujeitas aos mesmos controles.

## 5. Prazos e checkpoints

Não há base para uma data confiável de conclusão. A estimativa anterior de
“algumas semanas” foi otimista e não estava sustentada por uma taxa medida de
fechamento de funcionalidades originais. Prometer horas, dias ou semanas
agora confundiria implementação previsível com descoberta de contratos.

O próximo checkpoint deve ser o resultado do primeiro experimento que
discrimine uma relação state/draw ou UV/material. Se o resultado reduzir a
ambiguidade, implementar e medir quanto tempo leva para validar, integrar e
generalizar. Se não reduzir, registrar qual informação falta e mudar o
experimento; não converter essa tentativa em previsão de conclusão.

Após uma primeira cadeia funcional com evidência, estimar separadamente:

1. trabalho conhecido: decoders, integração, requisitos e regressões;
2. incerteza restante: contratos ainda desconhecidos e disponibilidade de fontes;
3. cobertura: quantidade e diversidade de assets, comportamentos e modos.

O prazo total depende dessas três partes. A investigação pode continuar
indefinidamente se os dados disponíveis não distinguirem os contratos. Mais
horas de geração de relatórios não garantem que essa incerteza desapareça.

## 6. Validação e restrições mantidas

Ao encerrar cada etapa de implementação:

```powershell
dotnet build .\fifa_pc\FifaStreetPc.csproj -c Release --no-restore
dotnet run --project .\fifa_pc\FifaStreetPc.csproj -c Release --no-build -- self-test
pwsh -NoProfile -File .\tools\run-clean-room-authored-matrix.ps1 -Workspace 'D:\projeto zeta'
```

Incluir smokes relevantes, runtime headless, runtime windowed quando aplicável
e replay determinístico. Conferir a ISO ao final:

```powershell
$zetaIso = Get-Item -LiteralPath 'D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso'
Get-FileHash -LiteralPath $zetaIso.FullName -Algorithm SHA256
$zetaIso.Length
$zetaIso.Attributes
```

Estado da ISO conferido novamente nesta avaliação:

- tamanho: `2013003776` bytes;
- atributo: `Archive` — a restrição de somente leitura é do fluxo de acesso;
- SHA-256: `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Continuam vigentes: código novo em .NET, sem emulação, sem execução do XBE,
sem alegação de recompilação, sem bindings inventados, preservação das
alterações e da ISO, sem declarar o projeto concluído e sem iniciar code
review. Documentos históricos sobre loader/compatibilidade não substituem
essas instruções atuais.

## 7. Fontes locais e alcance desta avaliação

- [Readiness atual](../fifa_pc/project-readiness.tsv).
- [Cobertura integrada](../fifa_pc/clean-room-integrated-coverage.tsv):
  `10/10` cenários, `777` frames, `54` eventos de áudio e `1.554` amostras
  de animação; escopo `verified-authored`.
- [Rendering global](../fifa_pc/eagl-original-state-geometry-draw-classification-global-assignment-control-summary.tsv).
- [Pose/timing global](../fifa_pc/animation-catalog-pose-timing-global-assignment-control-summary.tsv).
- [Áudio global](../fifa_pc/audio-event-static-context-global-assignment-control-summary.tsv).
- [Evidência externa de timing](../fifa_pc/external-animation-timing-audit-summary.tsv).
- [Validador de observações](../fifa_pc/Runtime/ExternalAnimationTimingEvidenceAudit.cs).
- [Simulação atual](../fifa_pc/Runtime/StreetMatch.cs) e
  [renderer atual](../fifa_pc/Runtime/PreviewRenderer.cs).
- [Handoff, registro 1.23](../CONTINUAR_FIFA_STREET_2_HANDOFF.md) e
  [instruções vigentes](../INSTRUCAO_CONTINUAR_FIFA_STREET_2_ATE_FINALIZAR.md).

A Stage 1.23 registra Release sem warnings/erros, self-test `96/96` e matriz
com `completed-authored-and-candidate-opt-in`. São resultados da validação
anterior, não uma execução nova feita para este Markdown. Nesta avaliação
foram lidos os relatórios atuais, os trechos de implementação relevantes e foi
recalculado o SHA-256 da ISO. O runtime e os gates não foram alterados.

## 8. XboxRecomp como acelerador estático, não como runtime — registro 1.24

Para este título, `sp00nznet/xboxrecomp` é mais útil que PS2Recomp para
inspeção do executável porque o alvo é XBE Original Xbox. Adote somente os
passos de parser/disassembly/decomp estática para obter mapa de dispatch,
call-sites e hipóteses de fronteira que depois possam ser testadas contra
assets e relatórios independentes. Não adote a saída de recompilação C, o
runtime Win32 nem os componentes de hardware do upstream: isso mudaria o
escopo clean-room .NET e traria comportamento de emulação. Também não rode
harnesses que executem funções do XBE.

Teste concreto nesta etapa: 12 IDs presentes em 2.364 streams EAGL completas
foram ligados ao índice de 12 slots executáveis na tabela estática de 42
entradas. Isso fornece um mapa de navegação de handlers, reduzindo o custo de
encontrar e comparar call-sites. Não reduz a incerteza semântica: nenhuma
função/handler foi atribuída a draw, UV, material, vertex fetch ou estado GPU;
o gate rendering segue `not-proven`. Os limites de função emitidos pelo
disassembler são heurísticos e só servem como candidatos.

Referências: [código XboxRecomp](https://github.com/sp00nznet/xboxrecomp),
[guia de decompilação](https://github.com/sp00nznet/xboxrecomp/blob/main/docs/DECOMP.md),
[controle local](../tools/analysis/eagl_original_render_command_dispatch_control.py)
e [readiness](../fifa_pc/project-readiness.tsv). O build/runtime continuam
100% .NET authored/candidate, com semântica original explicitamente não
reivindicada.

## 9. Aplicação após a pesquisa de 2026-10-13

As etapas 1.25 e 1.26 passaram do índice de dispatch ao consumidor: a
enumeração completa revelou 1.612 descritores com parâmetros TAR e
GeoPrimState juntos; 11.818 referências ligam bases exatas de `__VB`, e
2.363 draws têm contagem e índices locais identificados. O controle da
etapa 1.26 releu 1.739.618 bytes diretamente da ISO, sem depender apenas
da coerência dos TSVs.

O caminho prioritário agora é declaração de atributos/shader/material e
resolução do seletor dinâmico. As antigas auditorias do primeiro parâmetro
permanecem válidas nesse recorte, mas não demonstram ausência de outros
parâmetros de estado no descritor. Os gates principais continuam abertos.
Veja o [plano com fontes públicas e limites](FIFA_STREET_2_ACELERACAO_PESQUISA_2026-10-13.md)
e o [contrato dos parâmetros/draw](FIFA_STREET_2_EAGL_PARAMETROS_DRAW_2026-10-09.md).

## 10. Ganho medido e consumidor de declaração — etapas 1.27/1.28

A validação já ganhou uma otimização concreta: busca de nomes de áudio
sobre 1,74 GB em aproximadamente 3–5 segundos, com relatórios byte-idênticos.
O resultado não é uma previsão para concluir o jogo. Veja
[medição e regressões](FIFA_STREET_2_VALIDACAO_OTIMIZADA_2026-10-13.md).

O recorte funcional também avançou: o consumidor da declaração de `mat.o`
foi localizado e conferido, permitindo um decoder .NET de cinco campos e
do envelope do VS de 46 instruções. A próxima lacuna é resolução do parent,
semântica dessas instruções e seletor dos buffers, não outra busca global
com os mesmos TSVs. Veja [evidência delimitada](FIFA_STREET_2_MATERIAL_DECLARACAO_2026-10-13.md).

Também foi aplicado um [runner Release direto](FIFA_STREET_2_RUNNER_RELEASE_2026-10-13.md):
build obrigatório preservado, DLL já compilada nos mesmos smokes/runtime,
prefixo exato validado e dez testes para preservação/rejeição de argumentos.
Essa alteração reduz overhead de invocação; não resolve bindings sem evidência.
