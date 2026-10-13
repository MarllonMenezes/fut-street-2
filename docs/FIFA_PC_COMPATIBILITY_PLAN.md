# FIFA Street 2 — início da camada de compatibilidade para PC

Este documento define a primeira fronteira técnica para uma futura execução em
PC sem modificar o ISO original e sem depender de SDK proprietário da Microsoft.
Ele não afirma que o jogo já é executável no PC.

## Estado comprovado

- O executável é um XBE x86 retail com base `0x00010000`.
- O entry point retail é `0x0024D8BE`, na seção `.text`.
- A instrução inicial confirmada é `mov ecx, dword ptr [0x10118]`.
- A rotina acessa endereços de TLS em `0x30075C`, `0x300760`, `0x300764` e
  `0x30076C`.
- Ela chama rotinas internas em `0x0024EB3A`, `0x00250CE2` e `0x0024E7CC`.
- O alvo `0x0024EB3A` usa `XBOXKRNL` ordinal `255`
  (`PsCreateSystemThreadEx`); `0x0024E7CC` usa ordinal `187` (`NtClose`).
- As bibliotecas declaradas são `XAPILIB`, `XGRAPHC`, `DSOUND`, `XBOXKRNL`,
  `D3D8` e `LIBCMT`.

Esses fatos vêm da análise estática; os endereços ainda não foram convertidos
em nomes de funções do jogo.

## Fronteiras da camada PC

### 1. Loader de imagem

Responsabilidades:

- ler o `default.xbe` diretamente do XDVDFS ou de uma cópia de trabalho;
- validar header, seções e hashes antes de qualquer execução;
- mapear as seções nos endereços virtuais esperados;
- preparar TLS, stack e heap compatíveis com o contrato observado;
- iniciar o entry point somente depois que os contratos mínimos estiverem
  implementados.

O loader não deve alterar o XBE, remover assinatura ou regravar o ISO.

### 2. Runtime Xbox compatível

Os 128 imports do `XBOXKRNL` devem ser tratados por ordinal público e só então
receber uma implementação host. A ordem inicial de implementação é:

1. memória virtual e memória contígua;
2. threads, eventos, espera, timers e seções críticas;
3. arquivos, diretórios, volumes e leitura do conteúdo do disco;
4. tempo, sincronização e finalização;
5. vídeo/display e alocação relacionada à GPU.

Cada import deve ter teste independente e um registro de diferenças entre a
semântica Xbox e a semântica Windows/PC. Não serão criadas funções ou ordinals
que não estejam presentes no binário analisado.

### 3. Serviços multimídia

`D3D8`, `XGRAPHC` e `DSOUND` formam fronteiras separadas. A implementação PC
deve começar com interfaces internas estáveis, deixando o backend gráfico e o
backend de áudio substituíveis. O backend não será escolhido como se fosse uma
prova de compatibilidade: primeiro serão identificados os formatos, estados e
chamadas realmente usados pelo jogo.

### 4. Sistema de dados

Os nomes encontrados no ISO mostram arquivos `.big`, `.abk`, `.ast`, `.vp6`,
`.xsh` e outros formatos específicos. O próximo trabalho de dados é mapear os
headers e índices desses containers em modo somente leitura. A camada deverá
apresentar caminhos virtuais equivalentes aos usados pelo jogo, mantendo o ISO
como fonte imutável.

## Próximo marco verificável

Construir um mapa de funções a partir do entry point:

- seguir as chamadas `0x0024EB3A`, `0x00250CE2`, `0x0024E7CC` e suas referências;
- identificar a rotina de startup, inicialização de filesystem e primeiro ponto
  de carregamento de assets;
- separar código de runtime/compiler de código específico do jogo;
- localizar o primeiro loop ou dispatcher de alto nível;
- adicionar testes de leitura para os primeiros containers de dados.

O mapa está em [FIFA_XBE_ENTRYPOINT_MAP.md](FIFA_XBE_ENTRYPOINT_MAP.md) e a
decodificação de instruções em
[FIFA_XBE_ENTRYPOINT_DISASSEMBLY.md](FIFA_XBE_ENTRYPOINT_DISASSEMBLY.md).
