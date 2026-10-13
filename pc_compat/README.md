# FIFA Street 2 PC compatibility host

Este é o primeiro host compilável da camada PC. Ele contém somente contratos
para os serviços observados no XBE; não executa o binário Xbox e não inclui
nenhum asset do jogo.

Comandos:

```text
dotnet run --project pc_compat -- status
dotnet run --project pc_compat -- validate-contract
dotnet run --project pc_compat -- inspect "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso"
dotnet run --project pc_compat -- fs-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/audio/AMBSTRM.abk
dotnet run --project pc_compat -- big-smoke "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/anims.big
dotnet run --project pc_compat -- kernel-smoke
dotnet run --project pc_compat -- kernel-abi-smoke
dotnet run --project pc_compat -- kernel-scheduler-smoke
dotnet run --project pc_compat -- cpu-probe "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 500
```

`kernel-smoke` valida o ciclo de vida de handles. `PsCreateSystemThreadEx`
fica em modo record-only: o entry point Xbox é guardado, mas nunca chamado
como delegate .NET.

`kernel-abi-smoke` preserva os 10 argumentos observados no call site sem
nomeá-los prematuramente. `kernel-scheduler-smoke` executa somente um callback
gerenciado previamente registrado para um endereço Xbox; o ponteiro original
continua desabilitado.

`cpu-probe` executa uma pequena máquina x86 gerenciada sobre o entry point e
interrompe no primeiro opcode/serviço ainda não implementado. Isso é uma prova
de execução gerenciada, não ainda um emulador completo do Xbox. O resultado
validado está em `docs/FIFA_MANAGED_EXECUTION.md`.

Os ordinals `187` (`NtClose`) e `255` (`PsCreateSystemThreadEx`) foram
confirmados nos call sites do startup. Implementações reais só entram depois
da validação dos ABIs, estruturas e regras de thread do Xbox.
