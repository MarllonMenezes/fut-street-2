# Fut Street 2

Projeto comunitário de **recompilação estática de FIFA Street 2 (Xbox original) para PC**. A meta é traduzir o código x86 do jogo para código nativo compilável e reconstruir, com evidência, os serviços de sistema necessários para executá-lo no computador — preservando a lógica original do jogo, em vez de recriar o gameplay do zero.

> **Estado: em desenvolvimento — ainda não é um jogo recompilado jogável.** A tradução e a compilação de código gerado já foram exercitadas, mas inicialização completa, fidelidade e os gates de rendering, assets, gameplay e áudio não estão aprovados.

## Abordagem

- Usar o [XboxRecomp](https://github.com/sp00nznet/xboxrecomp) como base para análise e tradução estática do código Xbox x86 para C, com uma pilha de patches versionada em [`native_recomp/patches`](native_recomp/patches).
- Compilar o código traduzido para o host e implementar apenas compatibilidade sustentada por evidências do executável e dos call sites. Stubs, hipóteses e instruções ainda não traduzidas não contam como funcionalidades implementadas.
- Manter fora do caminho de execução qualquer emulação de CPU, GPU ou APU. Não incorporar código ou assets proprietários do jogo.

## Estado verificável

O último marco publicado é a etapa 1.55 (14/10/2026): a pilha de patches 0001–0017 passou 551 testes (38 ignorados); a geração traduziu 15.600/15.600 funções, sem falhas, em 61 unidades C, e o build CMake Release e o CTest do alvo não-emulação passaram (2/2). Permanecem 16 sites sem lowering em 10 mnemônicos. Isso **não** demonstra boot, execução correta nem equivalência com o jogo. O XBE não foi executado nesse marco; headless e replay do protótipo auxiliar não validam o jogo recompilado. A prontidão permanece `NOT_READY`.

## Conteúdo

- [`native_recomp/`](native_recomp): integração CMake, suporte nativo e patches incrementais para a tradução XboxRecomp.
- [`tools/`](tools): utilitários de análise e testes de regressão usados pelo fluxo de recompilação.
- [`fifa_pc/`](fifa_pc) e [`pc_compat/`](pc_compat): ferramentas e protótipos auxiliares de investigação; não são o jogo recompilado nem substituem sua lógica original.

Os fontes C gerados, builds, XBE, ISO e assets extraídos são insumos locais e não fazem parte deste repositório. A ISO original deve permanecer somente para leitura. Use apenas uma cópia de jogo obtida legalmente.

## Build e validação

A compilação nativa requer uma geração local do XboxRecomp para preencher `RECOMP_GENERATED_DIR`; os arquivos gerados não são versionados. Exemplo para o alvo da biblioteca:

```powershell
cmake -S native_recomp -B artifacts/fut-street2-native/build -G Ninja `
  -DRECOMP_GENERATED_DIR="C:/caminho/local/xboxrecomp/gen"
cmake --build artifacts/fut-street2-native/build --config Release --parallel
ctest --test-dir artifacts/fut-street2-native/build -C Release --output-on-failure
```

O projeto auxiliar .NET pode ser compilado e testado sem a ISO:

```powershell
dotnet build fifa_pc/FifaStreetPc.csproj -c Release
dotnet fifa_pc/bin/Release/net8.0/FifaStreetPc.dll self-test
```

Esses comandos validam compilação e testes do código-fonte, não os gates do jogo original. Não execute nem distribua o XBE ou assets proprietários.

## Licença e créditos

Este repositório não inclui uma licença de redistribuição do jogo nem reivindica direitos sobre FIFA Street 2. Consulte as licenças do XboxRecomp e de quaisquer dependências antes de redistribuí-las. O trabalho específico deste projeto ainda não tem uma licença open-source definida.
