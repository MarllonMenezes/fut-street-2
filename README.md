# Fut Street 2

Projeto não oficial cujo objetivo é a **recompilação estática de FIFA Street 2
do Xbox original para PC**: traduzir o código original para código compilável
e executá-lo nativamente, sem intérprete nem JIT de CPU durante a execução.
A referência de abordagem é o
[re:Blue](https://github.com/zolaware/reblue), não uma recriação do gameplay
com código novo. O nome público do projeto é **Fut Street 2**.

**Estado atual: `NOT_READY` — ainda não existe um executável jogável recompilado.**
O XboxRecomp v0.13.1, com quatro patches locais versionados, gerou C para
15.702/15.702 funções no passe mais recente; 64 unidades C foram compiladas e
o host linkou sem símbolos indefinidos. CTest 2/2 passou. Restam 71 stubs e
154 instruções no-op. Um bring-up controlado carregou as seções do XBE e chamou
a entrada traduzida, mas parou em serviços/operações ainda sem implementação;
isso não é um boot validado nem prova de fidelidade. O protótipo authored em
.NET 8 é separado e não demonstra fidelidade ao original.

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

Último avanço registrado: [etapa 1.41 — bloqueio do vblank sintético](docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md).
O host agora recusa `RECOMP_VBLANK`, que ativaria uma interrupção GPU fixa
simulada. O GPIO LPC PM em `0x80C0` ainda aguarda validação independente; não há
binding confirmado nem boot/menu. Os 71 stubs e 154 no-ops permanecem por
auditar. Isso não fecha gates de fidelidade. A
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
