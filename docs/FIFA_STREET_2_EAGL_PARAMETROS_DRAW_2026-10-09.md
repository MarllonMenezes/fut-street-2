# FIFA Street 2 — parâmetros EAGL e submissão indexada

Registro 1.25, 2026-10-09. Escopo: análise estática do XBE e parser de dados
em .NET. Nenhuma instrução Xbox foi executada; o renderer não foi alterado.

## Resultado reproduzível

O parser novo `fifa_pc/Runtime/EaglRenderParameterAudit.cs` percorre os pares
de parâmetros associados a cada comando, usando o contrato observado nos
handlers do XBE. A seleção dos 883 objetos é a mesma da auditoria original,
sem carregar malhas ou texturas candidatas para estabelecer os vínculos.

| Evidência | Resultado |
| --- | ---: |
| Descritores/primitivas | 2.364 |
| Comandos com layout válido | 36.779 / 36.779 |
| Pares de parâmetros consumidos | 32.051 |
| Comandos 9 com base exata de registro `__VB` | 11.818 / 11.818 |
| Stride do comando igual ao stride do registro | 11.818 / 11.818 |
| Referências de buffer com seletor dinâmico ainda necessário | 7.092 |
| Comandos 26 com condição diferente de zero | 2.364 / 2.364 |
| Parâmetros do comando 15 com símbolo `TAR`/`SHAPENAME` | 2.364 |
| Parâmetros do comando 16 com símbolo `GeoPrimState` | 1.612 |
| Descritores com ambos os parâmetros de estado | 1.612 |

O vínculo de base de buffer usa o endereço do par e o início do registro,
sem comparar ordinais nos nomes ou escolher posições por bounding box.
As funções auxiliares de rendering foram localizadas com o mapa estático do
XboxRecomp; o controle reproduzível usa Capstone e os bytes da mesma ISO,
sem depender do checkout temporário do XboxRecomp.

## Contrato estático dos parâmetros

O call site `0x0019E9B0` passa o descritor e o render method para
`0x00197050`. Este inicializa o cursor de parâmetros em `descritor + 4` e
obtém o cursor de comandos do render method. Os IDs `4` e `26` não consomem
pares. Os IDs observados `2,5,6,9,14,15,16,20,29,31` avançam o cursor em
8 bytes por comando. O tamanho do comando é um campo separado.

O parser rejeita ID desconhecido, tamanho/header incompatível e leitura além
da seção ou do próximo descritor. Após erro, não tenta recuperar o cursor.
Relocation nomeada permanece uma referência externa: seu word raw zero não
é interpretado como endereço zero da seção. Somente a relocation local
tipo 2 com nome vazio permite inspecionar o alvo local neste controle.

## Comando 9: base e estado de buffer

Em `0x001A3FD0`, o endereço passado ao wrapper é:

```text
registro = par.word1 + comando.word1 * seletor(0x00345BCC) - 20
slot     = comando.word2
```

O wrapper `0x001A29B0` lê o ponteiro no início do registro e o stride em
`registro + 8`. A rotina da seção D3D em `0x002923F0` grava esses valores em
uma tabela com entradas de 12 bytes indexadas pelo slot. O parser verifica
`par.word1 - 20` contra registros `__VB` validados: todos os 11.818 casam
exatamente. O word 3 do comando coincide com o stride desses registros;
esse word não foi identificado como o valor lido pelo handler.

Para 7.092 comandos o multiplicador não é zero. A base está identificada,
mas o buffer efetivamente selecionado depende do valor dinâmico de
`0x00345BCC`. Não se fixou esse valor em zero e não se aplicou o resultado
ao renderer. Formatos de posição, UV, normal, peso e skinning continuam sem
identificação independente nesta etapa.

## Comando 26: submissão indexada condicional

O caminho observado é:

```text
ID 26 → 0x001A4660 → 0x001A2B10 → 0x00293FB0 (D3D)
```

O handler testa o word 2 do comando contra zero e, se diferente, passa
início zero e o contador em `0x003458D8`. O wrapper obtém a base dos índices
em `0x00345D00` e o modo em `0x00345C70`. A rotina D3D escreve os headers
`0x000417FC`, `0x40001800 | (quantidade << 18)` e `0x00041808`; a sobra
ímpar usa leitura de 16 bits com extensão por zero. O fechamento escreve
`0x000417FC` seguido de zero.

Os métodos `0x17FC`, `0x1800` e `0x1808` são respectivamente
`SET_BEGIN_END`, `ARRAY_ELEMENT16` e `ARRAY_ELEMENT32`, conforme as
[definições públicas do nxdk, versão fixada](https://github.com/XboxDev/nxdk/blob/7e8d4fc3d00ca59e88298e912438ac215cf93392/lib/pbkit/nv_regs.h).
Isso permite a identificação limitada de submissão indexada condicional.
O word 2 do comando 26 é uma condição nesse caminho: o valor observado `5`
não deve ser transformado automaticamente em um tipo de primitiva.

## Consequência para a investigação de estados

O campo `state_symbol` do relatório antigo observa a relocation do primeiro
par. Ele não inventaria os parâmetros dos comandos seguintes. A enumeração
completa agora mostra `TAR`/`SHAPENAME` no comando 15 e `GeoPrimState` no
comando 16 dentro de 1.612 descritores compartilhados. Portanto, os antigos
controles negativos de sobreposição do primeiro parâmetro têm escopo mais
estreito; não excluem essa associação direta entre parâmetros.

Não se altera retroativamente o significado ou o resultado desses controles.
A próxima investigação deve partir dos parâmetros completos, do índice e do
seletor dinâmico, antes de tentar novas atribuições globais por coincidência.

## Controles e limites

`tools/analysis/eagl_render_parameter_static_control.py` confere 20 trechos
estáticos, 106 endereços de instrução, os 12 alvos da tabela e os dois TSVs
de comandos/parâmetros. Seis mutações em cópias na memória testam rejeição de
cursor, consumo, stride, ponteiro externo, linha duplicada e call removido.
O readiness verifica SHA-256 dos relatórios usados para evitar evidência
desatualizada. O self-test .NET cobre limites, comando desconhecido, header
malformado, relocation externa, seletor pendente e relatório alterado.

Subgate: `rendering-original-parameter-and-indexed-draw-control`.
Binding limitado: `verified-static-command26-conditional-indexed-submission`.
`runtime_selector=unresolved`, `vertex_attribute_binding=not-proven`,
`full_rendering_binding=not-proven`, `original_semantics=not-claimed`.
Os gates principais continuam sujeitos às evidências de rendering, assets,
áudio e deformação; este avanço não declara o projeto concluído.

Relatórios: `fifa_pc/eagl-render-parameter-audit.tsv`,
`fifa_pc/eagl-render-parameter-audit-summary.tsv`,
`fifa_pc/eagl-render-parameter-static-control.tsv` e
`fifa_pc/eagl-render-parameter-static-control-summary.tsv`.

ISO: `2013003776` bytes, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
XBE lido: `3293184` bytes, SHA-256
`5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B`.


Validação final das etapas 1.25/1.26, em 2026-10-13: Release 0 warnings/
0 errors, self-test 98/98 e matriz completa exit 0,
`completed-authored-and-candidate-opt-in`. Smokes relevantes, runtime
headless/windowed e replays passaram; 10 cenários, 777 frames, 54 eventos de
áudio e 1.554 amostras de animação. Probe material adicional: exit 0.
Log: `fifa_pc/stage-1.26-matrix-2026-10-13.log`.
Os dois subgates novos estão `verified-static-binding`; os gates principais
permanecem assets candidate-integrated, rendering not-proven, gameplay
clean-room-integrated, áudio partially-integrated e deformação structural-only.
Readiness: NOT_READY. ISO somente leitura, 2013003776 bytes, Archive,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

