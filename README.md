# Fut Street 2

Projeto não oficial cujo objetivo é a **recompilação estática de FIFA Street 2
do Xbox original para PC**: traduzir o código original para código compilável
e executá-lo nativamente, sem intérprete nem JIT de CPU durante a execução.
A referência de abordagem é o
[re:Blue](https://github.com/zolaware/reblue), não uma recriação do gameplay
com código novo. O nome público do projeto é **Fut Street 2**.

**Estado atual: `NOT_READY` — ainda não existe um executável jogável recompilado.**
O XboxRecomp v0.13.1 traduziu 15.605 funções do XBE para C; as 65 unidades
geradas foram compiladas e arquivadas localmente. Em 2026-10-13 o primeiro
`.exe` host também linkou sem símbolos indefinidos usando um runtime selecionado
sem os componentes NV2A/APU; o binário temporário só passou no smoke `--help` e
nunca foi inicializado com o XBE. Isso ainda não prova boot nem fidelidade.
Permanecem 86 destinos de chamada substituídos por stubs e 155 instruções
emitidas como no-op pelo gerador. O protótipo authored em .NET 8 é separado e
não demonstra fidelidade ao original.

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

Último avanço da recompilação: [etapa 1.34 — xrefs de stubs e coalescência
conservadora](docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md). A tentativa
de recuperar um alvo de desvio interno foi corretamente rejeitada pela
validação de entrada independente do XboxRecomp; não se forçou a fusão.
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
