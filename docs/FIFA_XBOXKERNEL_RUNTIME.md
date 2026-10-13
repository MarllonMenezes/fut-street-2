# FIFA Street 2 — prioridade do runtime XBOXKRNL

O host compilável em `pc_compat/` começa como contrato. A implementação deve
ser incremental e testada por ordinal, porque o XBE chama ABI Xbox x86, não uma
API Windows diretamente.

## Primeiros imports confirmados em call sites

| Ordinal | Export | Evidência | Prioridade |
|---:|---|---|---:|
| 187 | `NtClose` | wrapper em `0x0024E7CC`, thunk `0x002A9078` | alta |
| 255 | `PsCreateSystemThreadEx` | startup auxiliar em `0x0024EB3A`, thunk `0x002A9134` | alta |

## Grupos dos demais imports

Depois dos dois contratos acima, a ordem de implementação é:

1. memória virtual/contígua e handles;
2. threads, eventos, waits e timers;
3. arquivos, diretórios, volumes e leitura do XDVDFS;
4. sincronização e status NT;
5. display/GPU;
6. criptografia somente se um call site demonstrar necessidade.

Os 128 ordinals encontrados no XBE estão preservados em
[FIFA_XBE_ANALYSIS.md](FIFA_XBE_ANALYSIS.md). Nenhum ordinal novo deve ser
inventado para preencher lacunas.

## Critério de implementação

Cada import terá:

- assinatura ABI documentada a partir dos call sites;
- teste de argumentos, retorno e erro;
- backend PC isolado;
- comportamento explícito para operações sem equivalente seguro no PC.

Até que isso esteja pronto, `pc_compat` permanece em `contract-only` e não
tenta executar o XBE.

## Implementação atual

`XboxKernelRuntime` já mantém handles de thread do host, implementa o ciclo de
vida de `NtClose` e registra pedidos de `PsCreateSystemThreadEx`. O modo atual é
`record-only`: o entry point Xbox e o contexto são preservados como endereços
opacos, mas nenhum ponteiro Xbox é convertido em delegate ou executado.

O call site analisado preserva 10 argumentos posicionais em
`XboxPsCreateSystemThreadExCall`; os nomes dos parâmetros permanecem
`unresolved` até haver mais referências cruzadas. O scheduler gerenciado aceita
somente rotinas registradas explicitamente pelo host, permitindo testar
threads/handles sem executar endereços do XBE.
