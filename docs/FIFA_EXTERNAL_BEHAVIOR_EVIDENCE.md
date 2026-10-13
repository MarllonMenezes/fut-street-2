# Evidência externa de comportamento — FIFA Street 2

Este arquivo separa referências públicas de evidência extraída/validada na ISO.
As referências abaixo podem orientar cenários authored da reimplementação
clean-room, mas não autorizam bindings de mesh, textura, UV, material, áudio,
pose, rigging, skinning, animação ou comandos GPU.

## Referências usadas como contexto

- A entrevista de desenvolvimento publicada pela [GameSpot](https://www.gamespot.com/articles/fifa-street-2-qanda/1100-6144717/)
  descreve, em nível de produto, melhorias de IA, juggling em tempo real,
  gamebreaker e combos de trick; a página também lista Xbox entre as
  plataformas. Isso é contexto comportamental público, não uma captura de
  frames, tabela de input ou dump de dados da ISO.
- O [guia público de controles e modos](https://gamefaqs.gamespot.com/psp/930363-fifa-street-2/faqs/50140)
  descreve controles e exemplos de partidas por tempo e por primeiro a atingir
  uma quantidade de gols. O próprio guia identifica a seção de controles como
  PS2/PSP; portanto ele é referência secundária e não prova específica da
  variante Xbox/USA em trabalho.

## Uso no runtime

O modo `run-headless --match-rule first-to-goals --target-goals <N>` é uma regra
authored opt-in. Ele foi implementado para tornar explícito um cenário
reproduzível de encerramento por placar, não para afirmar que esse modo, esse
limite ou esse timing foram decodificados da ISO. O modo padrão continua
`TIMED` e não mudou seu checksum.

O modo `goals-or-panna` e a coluna authored opcional `panna` seguem a mesma
regra de proveniência. O evento `PANNA`, a contagem `PannaCount` e a rota de
áudio `PANNA -> panna` são comportamentos novos e explícitos; a existência de
uma palavra ou row chamada `panna` nos dados não prova que ela seja um cue
original para esse evento.

Validação authored:

- fixture de 80 frames: `FIRST_TO_GOALS`, alvo `1`, `HOME 1 - 0 AWAY`,
  `match_last_action=FULL_TIME`, `match_frame=70`;
- modo padrão: checksum `C0249FD97A01C3F4`;
- self-test: regra de alvo encerra sem consumir o relógio.
- fixture `gameplay-clean-room-panna-input.tsv`: `PANNA` + `FULL_TIME` no frame
  31, com dois eventos entregues ao consumidor authored de áudio no mesmo tick.

Essas referências não alteram o relatório de prontidão: assets continuam
`candidate-integrated`, rendering `not-proven`, áudio `partially-integrated` e
animação/deformação `structural-only`.
