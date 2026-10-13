# FIFA Street 2 — primeira execução gerenciada no .NET

O projeto agora possui um probe x86 32-bit escrito em C# que carrega o XBE em
memória gerenciada, executa um subconjunto de instruções reais e intercepta os
thunks Xbox conhecidos sem chamar ponteiros nativos.

## Marco validado

Com o ISO original, o comando:

```text
dotnet run --project pc_compat -- cpu-probe "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" 500
```

produziu:

```text
initial_eip: 0x0024D8BE
final_eip: 0x00000000
steps: 72
direct_calls: 2
kernel_calls: 2
stop: entry point returned
```

O caminho executado passou por `PsCreateSystemThreadEx` e `NtClose`. Os
endereços Xbox continuam sendo tratados como endereços convidados; somente
callbacks explicitamente registrados pelo host podem ser executados como
delegates .NET.

## Limite atual

Isso ainda não é um emulador completo nem inicia o jogo. Faltam instruções x86
fora do subconjunto inicial, a ramificação de TLS, os demais ordinals do
`XBOXKRNL`, D3D8/XGRAPHC, DSOUND, entrada, threads reais do jogo e os formatos
de assets além do índice BIG4/BIGF.

O probe está em `pc_compat/Runtime/X86BootProbe.cs`; ele foi deliberadamente
feito como uma etapa observável e interrompível para evitar executar bytes não
interpretados como se fossem válidos.
