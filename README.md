# Fut Street 2

Projeto não oficial cujo objetivo é a **recompilação estática de FIFA Street 2
do Xbox original para PC**: traduzir o código original para código compilável
e executá-lo nativamente, sem intérprete nem JIT de CPU durante a execução.
A referência de abordagem é o
[re:Blue](https://github.com/zolaware/reblue), não uma recriação do gameplay
com código novo. O nome público do projeto é **Fut Street 2**.

**Estado atual: `NOT_READY` — ainda não existe um executável jogável recompilado.**
No avanço registrado mais recente, XboxRecomp v0.13.1, fixado no commit
`193e2995ffaec871bb3be43a43bd66aa5fccfeb1` e com patches locais 0001–0014,
gerou C para 15.673/15.673 funções detectadas, sem falhas de geração. As 64
unidades C foram compiladas em Release e o host linkou; CTest passou 4/4 e o
smoke `--help` passou. A auditoria da geração atual registra 26 destinos ainda
stubados e 93 instruções sem lowering, em 28 mnemônicos. Na etapa 1.52 os stubs
caíram de 46 para 26 ao recuperar branches para corpos compartilhados; a
contagem de entradas mudou com os limites/aliases detectados, portanto não deve
ser comparada isoladamente.
Esses resultados provam tradução/compilação, não boot ou fidelidade. O XBE não
foi executado nesse marco; headless/replay authored não são evidência do jogo
recompilado. O protótipo .NET 8 é separado e não demonstra fidelidade ao original.

O [XboxRecomp](https://github.com/sp00nznet/xboxrecomp) é um candidato para
a tradução x86 → C do Xbox original, não um port de FIFA Street 2 já integrado.
A stack autorizada para o núcleo recompilado é C/C++, com .NET em ferramentas
auxiliares. Ainda faltam completar/auditar o runtime do Xbox, resolver
stubs/opcodes e validar gameplay, rendering, assets e áudio. A camada D3D8→D3D11,
vídeo e input foi ligada no primeiro host `.exe`; não incluímos os emuladores
NV2A/APU. Veja a
[direção vigente](docs/FUT_STREET_2_DIRECAO_RECOMPILACAO.md) e o
[relatório da primeira tradução/build](docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md).

Workspace: `D:\projeto zeta`. Código e comandos: [fifa_pc/README.md](fifa_pc/README.md).
`fifa_pc/FifaStreetPc.csproj` e `FifaStreetPc.dll` continuam com identificadores
técnicos anteriores para preservar compatibilidade dos testes e scripts.
A ISO `FIFA Street 2 (USA) (En,Es).iso` é exclusivamente entrada somente leitura.

## Continuidade

Leia primeiro a [direção vigente](docs/FUT_STREET_2_DIRECAO_RECOMPILACAO.md)
e depois, integralmente, os quatro documentos de continuidade:

- [Handoff principal](CONTINUAR_FIFA_STREET_2_HANDOFF.md)
- [Handoff para novo chat](CONTINUAR_FIFA_STREET_2_HANDOFF_NOVO_CHAT.md)
- [Handoff de 2026-10-08](CONTINUAR_FIFA_STREET_2_HANDOFF_2026-10-08.md)
- [Instruções até finalizar os gates](INSTRUCAO_CONTINUAR_FIFA_STREET_2_ATE_FINALIZAR.md)

Os nomes desses arquivos são históricos e estão preservados. Novos avanços
devem ser acrescentados sem apagar evidências anteriores. O estado é `NOT_READY`:
gates de rendering, assets originais, áudio e animação/deformação ainda abertos.
Gameplay authored é integrado, sem alegação de equivalência ao jogo original.

Último avanço registrado: etapa 1.52, no
[relatório de recompilação nativa](docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md).
A pilha versionada 0001–0014 foi validada sobre XboxRecomp v0.13.1 fixado; a
suíte `tools/disasm` + `tools/recomp` passou 542 testes (38 skips), e o build
CMake Release/CTest 4/4 passou. O patch 0012 filtra uma string referenciada
como dado; 0013 recupera branches condicionais de retorno; 0014 reconhece
destinos de jump dentro de corpos pais sobrepostos somente quando seus limites
terminais concordam. A última geração registra 26 stubs e 93 instruções sem
lowering (28 mnemônicos). Nenhum XBE foi executado no marco, e os
gates de boot, rendering, assets, gameplay original e áudio continuam
`NOT_READY`. Os arquivos C gerados e builds não são versionados. A evidência
para `0x80CE` em outro kernel não define o `0x80C0` do jogo; a hipótese
`field_pin` do xemu continua especulativa. A
[direção, prioridades e itens fora do caminho crítico](docs/FUT_STREET_2_ESCOPO_RECOMPILACAO_E_PRIORIDADES.md)
explica o que manter e o que arquivar sem apagar histórico.
Readiness: `NOT_READY`.

## Código-fonte no GitHub

Repositório público: [MarllonMenezes/fut-street-2](https://github.com/MarllonMenezes/fut-street-2),
branch `main`. Publicado em 2026-10-13 a pedido do usuário.

Este repositório contém apenas fontes, testes e documentação. A ISO, os assets
originais, mídia extraída, relatórios gerados e builds são locais e ignorados.
Instruções de build/testes e limites de reprodução:
[publicação source-only](docs/GITHUB_PUBLICACAO.md).
Para usar o Codex Cloud sem enviar insumos locais, siga
[continuação segura no Cloud](docs/CODEX_CLOUD_CONTINUACAO.md); tarefas Cloud
podem trabalhar no código versionado, mas não fechar os gates dependentes do XBE.
O estado e as ressalvas do primeiro link nativo estão em
[recompilação nativa](docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md).

```powershell
dotnet build fifa_pc/FifaStreetPc.csproj -c Release
dotnet fifa_pc/bin/Release/net8.0/FifaStreetPc.dll self-test
```
