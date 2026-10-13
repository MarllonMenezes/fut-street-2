# Fut Street 2

Projeto não oficial cujo objetivo é a **recompilação estática de FIFA Street 2
do Xbox original para PC**: traduzir o código original para código compilável
e executá-lo nativamente, sem intérprete nem JIT de CPU durante a execução.
A referência de abordagem é o
[re:Blue](https://github.com/zolaware/reblue), não uma recriação do gameplay
com código novo. O nome público do projeto é **Fut Street 2**.

**Estado atual: `NOT_READY` — a recompilação do jogo original ainda não foi
implementada nem validada.** O código existente contém ferramentas de análise,
leitores de assets, testes e um protótipo authored em .NET 8. Esse protótipo
não é o jogo original recompilado e seus testes não demonstram fidelidade ao
original. Até este registro, o XBE não foi executado nem recompilado.

O [XboxRecomp](https://github.com/sp00nznet/xboxrecomp) é um candidato para
a tradução x86 → C do Xbox original, não um port de FIFA Street 2 já integrado.
A stack do núcleo e a política para componentes de GPU/áudio ainda precisam
ser confirmadas: ausência de emulação de CPU não significa ausência de toda
emulação de hardware. A mudança de objetivo não adotou esse runtime.
Veja [direção vigente e limites](docs/FUT_STREET_2_DIRECAO_RECOMPILACAO.md).

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

Último avanço técnico do protótipo/análise: [etapa 1.31 — referências de modelo/material](docs/FUT_STREET_2_MODELOS_MATERIAIS_2026-10-13.md),
2.364 primitivas reconciliadas independentemente com três programas nomeados.
Matriz completa aprovada e validação final de identidade do produto aprovada.
Repetir a checagem de nome: `pwsh -NoProfile -File tools/test-fut-street-product-identity.ps1`.

## Código-fonte no GitHub

Repositório público: [MarllonMenezes/fut-street-2](https://github.com/MarllonMenezes/fut-street-2),
branch `main`. Publicado em 2026-10-13 a pedido do usuário.

Este repositório contém apenas fontes, testes e documentação. A ISO, os assets
originais, mídia extraída, relatórios gerados e builds são locais e ignorados.
Instruções de build/testes e limites de reprodução:
[publicação source-only](docs/GITHUB_PUBLICACAO.md).

```powershell
dotnet build fifa_pc/FifaStreetPc.csproj -c Release
dotnet fifa_pc/bin/Release/net8.0/FifaStreetPc.dll self-test
```
