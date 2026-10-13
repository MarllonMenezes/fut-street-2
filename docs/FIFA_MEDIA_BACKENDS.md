# FIFA Street 2 — fronteiras de vídeo e áudio

## Vídeo

O XBE declara `D3D8` e `XGRAPHC`, além das seções internas `D3D` e `XGRPH`.
O backend PC ainda não foi escolhido porque primeiro precisamos registrar os
call sites usados pelo jogo. A interface mínima prevista é:

- criação/configuração do dispositivo;
- buffers e texturas;
- upload de assets;
- estados de renderização;
- apresentação de frame;
- sincronização com o loop do jogo.

O backend deve ser substituível. A primeira implementação poderá usar uma API
gráfica pública do sistema, mas só depois que os formatos e estados realmente
usados forem identificados.

O gate de prontidão atual está materializado em
`fifa_pc/project-readiness.tsv` pelo comando `project-readiness-audit`. Ele
confirma a integridade da ISO, mas mantém vídeo/renderização como
`not-proven`: a cadeia same-trunk de mesh e textura é candidata, e a auditoria
de correspondência estado→geometria ainda tem `0` pares de bytes crus exatos.
Portanto, a presença de textura decodificada não é tratada como prova de UV,
material GPU, topologia final, skinning, rigging ou comando GPU.
A correspondência adicional por símbolos de relocation dos comandos também
resultou em 0/269 pares compartilhados; isso continua sendo evidência negativa,
não autorização para inventar uma associação de draw.
O runtime de gameplay também possui o modo opt-in `--round-demo`, que verifica
um reinício de rodada clean-room após gol; isso é uma política da simulação e
não altera a classificação estrutural/candidata dos formatos de vídeo.
O argumento `--match-seconds` permite verificar o fim authored da partida:
`FULL_TIME` é emitido uma vez e o tick seguinte não avança o frame. Isso valida
o ciclo do runtime novo, não uma duração original nem uma semântica de GPU.
O mesmo argumento está disponível no host `run-windowed`, que passa a usar a
mesma duração authored do `StreetMatch`; o estado resultante continua separado
de qualquer regra de tempo observada no conteúdo original.
O renderer software e o host Win32 ainda oferecem `--clean-room-characters`,
uma apresentação procedural authored para tornar os atores visualmente
animados, agora com perfis authored para posse, passe, drible, chute, gol,
reinício e `FULL_TIME`. Ela não representa animações originais nem cria
bindings para AXT, morph, rigging, skinning ou GPU.
O `run-headless` também pode salvar o último framebuffer com
`--frame-output <saida.ppm>`; essa saída é somente um artefato de QA do
renderer e nunca escreve na ISO.

## Áudio

O XBE declara `DSOUND`. Os assets mostram duas famílias relevantes:

- `.abk` com assinatura `ABKC`;
- `.ast` com assinatura `SCHl`.

O backend inicial deve separar carregamento de container, decodificação e saída
de áudio. O decoder agora reconhece EA-XA v3 nos streams Xbox `SCHl`, percorre
blocos `SCDl` e exporta frames PCM/ADPCM para WAV PCM16. O caminho foi aplicado
ao `data/audio/GBREAK.ast` da ISO: estéreo, 24 kHz, 566 blocos.

O comando `audio-export` grava o WAV sem carregar o stream inteiro na memória.
Também há `audio-play`, que decodifica até 32 MiB para memória e reproduz o WAV
síncrono pelo WinMM no Windows. Os bancos `ABKC` ainda não estão associados a
eventos/amostras do jogo. O catálogo agora valida os streams `.ast` na montagem
e `run-headless`/`run-windowed` usam `CatalogAudioMixer` para contabilizar os
streams aceitos/rejeitados e pré-decodificar os streams válidos para PCM quando
o opener read-only está disponível. Além disso, 17 nomes da string table PA são
correlacionados com rows header/data paralelas; 7 têm canais no header observado
e 10 têm uma hipótese mono única validada por layout e frames `SCDl`. O mixer
pré-prepara os 17 payloads nomeados, preservando `channels=0` como fato do
header e a hipótese como campo separado de candidato. A única regra clean-room
de gameplay é `GOAL_HOME -> goal`: ela prepara a row PA nomeada, e o host Win32
a envia ao WinMM assíncrono. Isso não é uma tabela original de cues nem vínculo
EVT→ABKC; os demais eventos continuam sem mixagem automática.
O headless permanece silencioso por padrão; `run-headless ... --demo
--play-audio` habilita explicitamente a mesma saída WinMM assíncrona no Windows
e expõe `gameplay_audio_played` no relatório. Essa opção verifica a saída da
cue `goal`, não cria bindings para os demais nomes.
O comando `audio-validate` percorre todos os blocos/tabelas `SCDl` dos streams
sem construir um WAV em memória. Na ISO, os 6 `.ast` foram aceitos (6/6), com
0 rejeições; `GBREAK.ast` e `GBREAKLC.ast` percorreram 566 blocos e 905.528
frames cada.
O decoder também expõe a evidência do primeiro chunk de um range; isso mostrou
que os ranges PA usam `SCCl` antes dos dados. O walker preserva esse envelope
sem interpretar seu payload e segue para `SCDl`; os 144 ranges sem `0x82` no
metadata `SCHl` continuam com `channels=0` observado. A auditoria separada
`audio-sccl-channel-layout-audit` validou os 144/144 como mono único e 0/144
como estéreo ao decodificar os frames, permitindo preparar payload candidato sem
reescrever o header ou criar binding de evento.
`audio-sccl-inspect ... --rejected` amostra esses 144 casos separadamente: a
cadeia continua `SCCl -> SCDl`, a taxa observada é 22.050 Hz e os tags
`0x05/0x06/0x80/0x84/0x85/0xA0/0x8C` permanecem presentes, mas `0x82` não
aparece. Os payloads `SCCl` variam e não formam, sozinhos, uma regra independente
para preencher canais; por isso nenhum range é habilitado apenas pela ausência
do tag. A preparação de payload candidato usa somente a auditoria separada que
validou uma hipótese única em todos os frames.

O diagnóstico `asset-format-smoke ... .abk --verbose` continua expondo as
palavras brutas do cabeçalho/índice inicial de um ABKC sem lhes atribuir nomes
de campo. O comando estrutural `audio-structure-smoke` adiciona somente
checagens neutras:

- os 16 `.abk` observados têm `ABKC`, a palavra little-endian em `0x14`
  coincide com o tamanho do próprio arquivo e a palavra em `0x1C` é `0x5C`;
  nenhum contém as assinaturas `SCHl`/`SCDl` embutidas;
- os 16 `.off` são CSVs ASCII com 48 ou 283 linhas, três colunas numéricas e
  uma vírgula final vazia; as duas primeiras colunas formam faixas monotônicas
  que cabem no `.big` de mesmo tronco em todos os 16 pares. Além disso, as 48
  faixas de cada `djdat_*.off` e as 283 de `pcpadat.off` começam com `SCHl`;
  os companions `djhdr_*.big`/`pcpahdr.big` são ranges legíveis de cabeçalhos
  binários, não streams `SCHl`;
- os dois `.evt` começam com `03 12 3C 07`. O word little-endian em `0x04`
  está dentro do arquivo (`0x290` e `0x1E70`), mas não é o tamanho total. Há
  runs de texto no final, incluindo `play_hitz_eng` e `panna_goal`, porém não
  há nomes ABKC nem assinatura `ABKC` nesses arquivos.
- Em `eventspa.evt`, os 283 words iniciais distintos dos 283 ranges de
  `pcpahdr.big` reaparecem como words little-endian zero-extended alinhados:
  283 valores cobertos, 314 ocorrências. Isso demonstra uma relação binária
  reproduzível entre o evento PA e o companion de headers, sem ainda nomear o
  word como ID ou provar a amostra correspondente. Para DJ, os companions de
  header têm poucos words iniciais distintos e a cobertura é ambígua.
- Uma inspeção paralela, por ordem de linha, confirma 283 ranges em
  `pcpahdr.off` e `pcpadat.off`, com todos os ranges de header legíveis e
  283/283 ranges de dados iniciando em `SCHl`. A mesma checagem para
  `djhdr_en.off`/`djdat_en.off` confirma 48/48 ranges e 48/48 prefixos `SCHl`.
  Isso estabelece uma relação estrutural entre os catálogos de header e de
  dados, mas não assume que o índice de linha seja um ID de amostra.
- A comparação de conteúdo por ordem de linha é deliberadamente limitada: lê
  o range completo do header e no máximo os primeiros 64 bytes de cada range
  de dados. Nas duas famílias, a terceira coluna coincide em todos os pares,
  mas nenhum comprimento de header coincide com o comprimento do stream,
  nenhum prefixo de até 16 bytes coincide e nenhum par compartilha um word
  alinhado não nulo nessa janela de dados. Portanto, a cardinalidade/ordem não
  é evidência suficiente de um registro de áudio diretamente copiável.
- A inspeção neutra do envelope dos primeiros chunks confirma `SCHl`, tamanho
  declarado dentro do range, `PT` e plataforma 7 em 283/283 streams PA e
  48/48 streams DJ. O parser detalhado agora percorre 283/283 PA e 48/48 DJ;
  a correção foi ajustar o avanço após um comprimento estendido, que já havia
  sido consumido antes da validação do limite. O self-test cobre esse caso sem
  atribuir semântica nova ao campo.
- Na comparação de words alinhados dos headers com os valores extraídos dos
  chunks `SCHl`, plataforma 7 e versão 3 aparecem em 48/48 headers de cada
  idioma DJ e em 0/283 headers PA; os demais valores extraídos não aparecem
  nos headers PA nem nos headers DJ, salvo uma ocorrência de `channels` e de
  `sample_rate` em `djhdr_it`. Isso é apenas uma correlação binária dentro dos
  ranges, ainda insuficiente para nomear campos ou bindings. Os offsets dos
  matches variam entre os idiomas e nenhum offset único cobre os 48 registros;
  portanto os words não formam um campo estável demonstrado.
- A comparação dos valores de metadados `SCHl` com words alinhados dos eventos
  também não produziu seleção demonstrável: `eventsdj.evt` encontrou somente
  ocorrências repetidas de `platform`, `version`, `channels` e um `header_size`,
  enquanto `eventspa.evt` encontrou apenas `channels`. Não houve match de
  `codec`, `sample_rate` ou `declared_samples` em PA; esses matches comuns não
  apontam para uma linha única e não são bindings de reprodução.
- Em `eventspa.evt`, as 314 ocorrências dos 283 words iniciais distintos de
  `pcpahdr` ficam entre `0x74` e `0x1E68`, todas antes do word em `0x04`
  (`0x1E70`); os gaps entre ocorrências variam de 8 a 84 bytes. Isso sustenta
  apenas a existência de um bloco de catálogo/registros antes dos runs de
  texto, sem demonstrar o tamanho ou a semântica de cada registro.
- Uma janela relativa de words em torno dessas ocorrências encontrou repetição
  estrutural: `+4` bytes é zero em 314/314 ocorrências; em `-12`, o valor mais
  comum é `1` em 277/314, e em `-8`, o valor mais comum é `4` em 244/314.
  A diversidade nos demais offsets impede chamar a janela de record fixo ou
  nomear esses valores como flags/IDs.
- Entre as ocorrências, 10 values duplicados somam 41 ocorrências; todos os 10
  aparecem em múltiplas janelas de contexto. Assim, as duplicações não são
  cópias do mesmo record bruto e a igualdade do word não basta para selecionar
  uma entrada.
- As 314 janelas completas de contexto são distintas: não houve família de
  contexto repetida e a maior frequência foi 1. A repetição observada está em
  campos relativos isolados, não em records binários idênticos.
- A comparação de espaçamento agora cobre os dois EVT: `eventspa.evt` mantém
  314 ocorrências PA entre `0x74` e `0x1E68`, com gaps de 8 a 84 bytes e gap 24
  em 257 casos; `eventsdj.evt` tem ocorrências isoladas por idioma, exceto duas
  no italiano separadas por 220 bytes. Isso diferencia as janelas observadas,
  mas não define records nem um binding de reprodução.
- `audio-event-text-smoke` mostra que os nomes PA (`panna_goal`, `beat`,
  `chance`, `gb_goal` e outros) formam uma string table contígua a partir de
  `0x2090`; os bytes vizinhos não fornecem uma referência direta comprovada a
  `pcpahdr` ou ABKC.
- `audio-event-index-smoke` encontra 17 pares alinhados de offset relativo +
  word na região anterior à string table; todos pertencem à família observada
  `word & 0xFFFF == 0x4F6B`. Nenhum dos 17 words aparece exatamente nos `.abk`
  enumerados. Essa evidência permanece local ao EVT e não é promovida a ID
  ABKC ou seletor de amostra.
- O comando `audio-event-candidate-smoke` materializa a correlação exata dos
  words alinhados de `eventspa.evt` com `pcpahdr.off`: 314 candidatos, 283 rows
  distintas e 314/314 rows paralelas disponíveis em `pcpadat.off`; os 283
  ranges de dados começam em `SCHl`. Esses objetos são candidatos estruturais,
  não cues reproduzíveis, pois ainda não há associação demonstrada entre os
  nomes/IDs do EVT e uma row específica.
- O comando `audio-event-named-candidates` materializa uma correlação mais
  restrita para os nomes da string table: na ISO são 17 nomes com header/data
  paralelos, 17 envelopes aceitos e 7 rows com canais válidos. Essa evidência
  autoriza apenas o binding explícito e clean-room `GOAL_HOME -> goal` no
  runtime; não autoriza promover os outros nomes a cues originais ou inferir
  uma relação ABKC.
- A validação de ranges com `audio-event-candidate-validate` separa os
  resultados: 139/283 passam o decoder `SCDl` (7.633 blocos, 11.118.229
  frames) e 144/283 são rejeitados porque o metadata `SCHl` não contém o tag
  `0x82`, fazendo o decoder representar `channels=0`. `audio-sccl-inspect`
  confirmou `SCCl` como primeiro chunk em
  283/283 ranges, inclusive nos decodificáveis; o walker o preserva como
  envelope estrutural e segue para `SCDl`. Nos primeiros casos, `SCCl` tem 12
  bytes com payload variável e é seguido por `SCDl`; isso ainda não prova a
  semântica do payload, a origem dos canais ou a associação de eventos. A
  inspeção de campos SCHl mostra `0x82/1=2` nos ranges válidos; não há ainda
  regra independente para preencher esse campo nos 144 ranges rejeitados.
  `audio-event-candidate-decode` permite materializar uma row válida de
  `pcpadat.big` por índice explícito em WAV PCM16; rows inválidas são recusadas
  pelo mesmo decoder e nenhum nome do EVT é convertido em cue.

Essas relações de mesmo tronco, limites e prefixos são evidência de que os
`.off` indexam segmentos dos `.big` de dados e cabeçalhos. Elas ainda não são
uma decodificação de eventos: nenhuma referência direta entre os registros
relacionados de `.evt`/`pcpahdr` e entradas de bancos ABKC foi demonstrada. Os significados dos demais
words, dos IDs numéricos próximos aos runs de texto e da terceira coluna
continuam deliberadamente sem nomes semânticos. Uma comparação conservadora
dos words little-endian dos `.evt` com os offsets das linhas `.off` encontrou
nenhum match em `djdat`/`pcpadat`; encontrou apenas valores repetidos em
`djhdr_ge`, `djhdr_sp` e `pcpahdr` para `eventspa` (por exemplo `1540` vinte
vezes e `32768` seis vezes). Como esses valores não reproduzem os payloads dos
headers e são repetidos como words comuns, permanecem candidatos ambíguos, não
bindings de eventos.

Os arquivos originais permanecem somente leitura. A camada de decodificação, a
montagem/validação de streams, a pré-decodificação dos seis `.ast` válidos e a
rota de ações do gameplay até o mixer estão implementadas. A associação
explícita `GOAL_HOME -> goal` prepara uma row PA nomeada e é reproduzida de
forma assíncrona pelo host Win32; no headless ela é verificada sem side effect.
Os outros eventos e a mixagem geral ainda são etapas separadas. A rota usa uma sequência monotônica de
ações, preservando repetições reais e ignorando frames sem nova ação.
O host Win32 windowed também encaminha essa sequência antes de atualizar o
mixer e encerra o playback assíncrono ao sair; a diferença restante é que o
backend só resolve a ação explicitamente documentada para uma amostra de áudio
validada.

Os bancos `ABKC` agora são montados estruturalmente no catálogo: 16 bancos
válidos expõem 39 referências externas e 70 ranges AST, e o mixer oferece
preparação lazy de um range específico. Essa capacidade não é acionada por
ação de gameplay enquanto o vínculo EVT→entrada ABKC não for demonstrado.
O catálogo também valida 13 `BNK` embutidos e 231 headers apontados dentro dos
bancos. Os envelopes variáveis `PT`/`GSTR` agora são analisados dentro de cada
janela limitada, com tags preservadas como evidência; os descritores Xbox IMA
`0x14` e seus 231 ranges de blocos são validados; eles podem ser decodificados por seleção explícita com
`audio-abk-decode` ou enviados ao WinMM por `audio-abk-play`. Isso não cria vínculo com EVT nem reprodução por ação: os
sons RAM continuam fora do mixer automático até a associação ser demonstrada.
