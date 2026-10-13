# FIFA Street 2 — contrato do loader PC

O loader PC está sendo separado do runtime. Nesta fase ele só deve validar e
descrever o layout do XBE; não deve chamar o entry point nem executar bytes do
binário Xbox.

## Dados já validados

- base: `0x00010000`;
- entry point retail: `0x0024D8BE`;
- 11 seções;
- entry point dentro de `.text`;
- hashes das 11 seções compatíveis.

O contrato correspondente está em
`pc_compat/Runtime/XbeLoaderContract.cs`. O próximo passo de implementação é
alimentar esse contrato com o leitor XDVDFS e comparar o layout com o relatório
do XBE antes de qualquer tentativa de execução.

Essa primeira implementação já foi feita em `pc_compat/Runtime/XdvdfsReader.cs`
e `pc_compat/Runtime/XbeInspector.cs`. O comando `inspect` confirmou o ISO real
com 130 entradas, 11 seções, hashes válidos e 128 thunks. O comando `fs-smoke`
também abriu `AMBSTRM.abk` e `AMBSTRM.ast` em modo somente leitura, sem carregar
os 230 MB do segundo asset inteiro em memória.
