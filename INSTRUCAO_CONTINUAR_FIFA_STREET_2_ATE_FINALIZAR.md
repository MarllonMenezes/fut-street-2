# Instrução para continuar o FIFA Street 2 até finalizar

## Direção vigente — 2026-10-13

O objetivo do **Fut Street 2** é recompilação estática do código original de
FIFA Street 2 do Xbox original para PC, não uma recriação do gameplay.
Leia primeiro [direção e limites](docs/FUT_STREET_2_DIRECAO_RECOMPILACAO.md).
Esta atualização substitui a definição histórica de reimplementação .NET
como produto final. O usuário autorizou continuar a tradução e compilação
estática até existir recompilação comprovada. C/C++ é o núcleo traduzido; .NET
fica como ferramenta/harness auxiliar. Isso substitui registros históricos
que proibiam recompilar o XBE, mas não autoriza executar o XBE nem iniciar o
host com `--launch` sem autorização específica de bring-up. Não integrar
emulação de GPU/APU sem autorização explícita. `NOT_READY`.

Continue o projeto FIFA Street 2 no workspace `D:\projeto zeta` até que os
gates de renderização, assets, gameplay e áudio estejam integrados e
verificados.

## Leitura inicial obrigatória

Leia completamente, sem pular linhas:

1. `D:\projeto zeta\CONTINUAR_FIFA_STREET_2_HANDOFF.md`
2. `D:\projeto zeta\CONTINUAR_FIFA_STREET_2_HANDOFF_NOVO_CHAT.md`
3. `D:\projeto zeta\CONTINUAR_FIFA_STREET_2_HANDOFF_2026-10-08.md`
4. `D:\projeto zeta\docs\FIFA_RECOMPILATION_STATUS.md`
5. `D:\projeto zeta\fifa_pc\README.md`

Confira o estado real do workspace antes de alterar qualquer coisa. Preserve
as alterações existentes e não use comandos destrutivos.

## Restrições

- Trabalhe na recompilação estática do código original; preserve o protótipo
  .NET somente como ferramenta/harness, nunca como substituto do jogo original.
- Gere/traduza e compile estaticamente o XBE usando o pipeline fixado. Não
  execute o XBE nem o host com `--launch` sem autorização específica. Não
  integre emulação de GPU/APU sem autorização explícita; reporte dependências
  sem solução sem inventar bindings.
- Não diga que o XBE foi recompilado sem geração, build e validação comprovados.
- Mantenha esta ISO somente para leitura:
  `D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso`.
- Não invente bindings para UVs, materiais, skinning, rigging, animação,
  morphs, áudio ou comandos GPU.
- Separe sempre evidência comprovada, caminho candidato e semântica ainda não
  decodificada.
- Preserve `original_semantics=not-claimed` quando a evidência não permitir
  uma atribuição.
- Não marque o projeto como concluído e não inicie code review antes de os
  gates realmente estarem fechados.

## Trabalho

Continue pela próxima etapa recomendada no handoff, priorizando:

1. estado de renderização original→geometria;
2. UV/material apenas com evidência independente, repetível e discriminativa;
3. EVT→ABKC→sample→playback para áudio original;
4. timing→pose→rig→skinning/morph com decoder validado.

Implemente cada avanço em código novo, adicione self-test ou smoke adequado,
gere relatórios reprodutíveis, atualize o readiness e documente os números
reais. Se um teste não encontrar binding, registre o resultado negativo em vez
de promover uma coincidência.

## Validação por etapa

Ao concluir cada avanço, execute pelo menos:

```powershell
dotnet build .\fifa_pc\FifaStreetPc.csproj -c Release --no-restore
dotnet run --project .\fifa_pc\FifaStreetPc.csproj -c Release --no-build -- self-test
pwsh -NoProfile -File .\tools\run-clean-room-authored-matrix.ps1 -Workspace 'D:\projeto zeta'
```

Inclua os smokes relevantes, runtime headless, runtime windowed quando
aplicável e replay determinístico. Confirme ao final:

## Registro de execução — etapa 1.24 — 2026-10-09

XboxRecomp foi avaliado como ferramenta para acelerar a navegação estática do
XBE Original Xbox, não como base de runtime/recompilação. Somente parser e
disassembler estáticos foram usados; não usar recompilador, runtime nativo ou
harness de execução. Não importar código gerado. O produto continua sendo
reimplementação clean-room em .NET, sem emulação e sem execução do XBE.

Resultado: 12 IDs de comando EAGL observados em 2.364 streams completas
(36.779 ocorrências) indexam 12/12 slots executáveis no prefixo estrutural de
42 entradas da tabela `0x0031A930`; há 39 destinos distintos. Isso não revela
comportamento ou semântica dos handlers. Subgate estrutural passou; gates
principais permanecem abertos e readiness `NOT_READY`.

Validação: Release `0/0`, self-test `96/96`, matriz
`completed-authored-and-candidate-opt-in`, smokes, runtimes headless/windowed,
replays determinísticos; 10 cenários, 777 frames, 54 eventos de áudio, 1.554
amostras. ISO mantida somente para leitura: `2013003776` bytes, `Archive`,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Continuar com evidência independente; preservar `original_semantics=not-claimed`,
não inventar bindings, não alegar recompilação, não concluir o projeto e não
iniciar code review.

```powershell
$iso = Get-Item -LiteralPath 'D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso'
Get-FileHash -LiteralPath $iso.FullName -Algorithm SHA256
$iso.Length
$iso.Attributes
```

Compare o hash e o tamanho com o handoff e registre qualquer diferença sem
continuar silenciosamente.

## Critério de encerramento

Só pare quando houver integração e verificação suficientes para todos os
gates, com documentação e artefatos reproduzíveis. Até lá, continue o
trabalho entre mensagens e não declare conclusão apenas porque build, runtime
authored ou caminho candidate passaram.

## Registro da última etapa — 2026-10-08

Foi adicionada a etapa estrutural do intervalo do catálogo no tail de
`__AnimationBank`: 19 bancos e 1.584 entradas validados, com controle
independente, sem promoção de semântica. Release `0/0`, self-test `94/94`,
matriz código 0, smokes/runtime/replays passaram e a ISO permaneceu com
2013003776 bytes, atributo `Archive` e SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B`.
O readiness continua `NOT_READY`; as instruções de não executar/recompilar o
XBE, não inventar bindings e não iniciar code review permanecem vigentes.

## Registro da etapa seguinte — 2026-10-08

Foi adicionada a auditoria estrutural das relocations dos pares no tail de
`__AnimationBank`: 19 bancos, 1.584 pares, duas relocations por par nos
offsets esperados, zero multiplicidades inesperadas, ambos os words dentro de
`.data` em 1.584/1.584 linhas, zero símbolos conhecidos, 1.584 pares raw
distintos e 19 layouts completos. O relatório independente reproduziu os
mesmos contadores. Isso não estabelece ordinal → catálogo nem semântica de
record/frame/pose/rig/timing/skinning/morph/playback; manter
`candidate_record_binding=not-proven` e `original_semantics=not-claimed`.

Foram concluídos build Release `0/0`, self-test `95/95`, matriz código 0,
smokes, runtime headless/windowed, replays determinísticos e SHA-256 da ISO.
O readiness continua `NOT_READY`; não executar/recompilar o XBE, não inventar
bindings, não marcar conclusão e não iniciar code review.

## Registro da etapa seguinte — 2026-10-08

Foi implementada a auditoria das janelas de relocation dos targets dos pares
do tail: 19 bancos, 1.584 linhas, 1.584 janelas completas e controle
independente reproduzido. As multiplicidades reais foram preservadas no
detalhe; nenhum par ofereceu uma única relocation por lado, nenhum segundo
nível foi resolvido e nenhum símbolo conhecido foi encontrado. Isso é
evidência estrutural, não binding de animação. O readiness continua
`NOT_READY`; manter todas as restrições de clean-room, ISO somente leitura,
não execução/recompilação do XBE, sem bindings inventados, sem conclusão e
sem code review.


## Registro das etapas 1.25 e 1.26 — 2026-10-13

A etapa 1.25 implementou o parser .NET dos parâmetros consumidos pelos
handlers EAGL: 2.364 descritores, 36.779 comandos, 32.051 pares, 11.818
bases exatas de registros __VB e 11.818 strides compatíveis. Foram encontrados
2.364 parâmetros TAR e 1.612 GeoPrimState; 1.612 descritores contêm ambos.
O relatório antigo de state observa somente o primeiro par. O comando 26
chega a submissão indexada condicional; seu word 2 não foi tratado como modo
de primitiva. Controle: 20 trechos/106 sites/6 mutações negativas.

Na etapa 1.26, o decoder novo
`fifa_pc/Runtime/EaglIndexedDrawSourceAudit.cs` liga contagem (comando 2),
ponteiro de índices (14) e draw (26) no mesmo descritor: 2.363 fontes locais
verificadas entre 2.364, 1 pendente e 0 rejeitadas. Foram decodificados
1.739.618 bytes de índices unsigned little-endian de 16 bits. O controle
`tools/analysis/eagl_indexed_draw_source_control.py` releu os payloads,
pares, comandos e relocations diretamente na ISO, independentemente do .NET:
5 trechos estáticos, 37 sites e 4 mutações negativas passaram. Os 2.363
ranges são compatíveis com seus registros-base. A pendência é
`tops0260.big!tops0260.ord`, layer 0, primitive 7, sem comando 2 local;
não foi inventada contagem herdada. 7.092 referências de buffer da etapa
1.25 dependem do seletor dinâmico, ainda não resolvido.

Pesquisa na internet e plano aplicável:
`docs/FIFA_STREET_2_ACELERACAO_PESQUISA_2026-10-13.md`;
contrato completo: `docs/FIFA_STREET_2_EAGL_PARAMETROS_DRAW_2026-10-09.md`.
XboxRecomp continua auxiliar estático; Ghidra/nxdk/vgmstream são referências
avaliadas, sem adotar runtime de recompilação ou emulação.

Nova fonte para a etapa seguinte:
`data/initstaz.big!initstay.big!mat.o`, 38.970 bytes, SHA-256
`75DBC11557C6F3E4A32D6D88372476398B3A4C435E3907C2902E38579FB29430`.
O probe `tools/analysis/eagl_material_parent_probe.py` passou, exit 0,
3 símbolos/168 bytes de janelas: ParentRM_FifastCharacterSkin em 0x1458,
FifastCharacterSkin0__EAGLMicroCodeVS em 0x1490 e MicroCodePS em 0x1780,
todos na seção 1. Isso é definição/símbolo e janela raw, ainda sem prova da
instalação do shader no draw ou de seus atributos.

Próxima etapa recomendada: seguir o parent em mat.o, a declaração/programa
instalados e seus consumidores até posição/UV/material; resolver também o
produtor/domínio de 0x00345BCC. Não repetir joins globais de nomes sem uma
fonte nova que possa discriminar alternativas. Manter clean-room .NET,
ISO somente leitura, original_semantics=not-claimed, sem execução/recompilação
do XBE, sem declarar conclusão do projeto e sem iniciar code review.



Validação final das etapas 1.25/1.26, em 2026-10-13: Release 0 warnings/
0 errors, self-test 98/98 e matriz completa exit 0,
`completed-authored-and-candidate-opt-in`. Smokes relevantes, runtime
headless/windowed e replays passaram; 10 cenários, 777 frames, 54 eventos de
áudio e 1.554 amostras de animação. Probe material adicional: exit 0.
Log: `fifa_pc/stage-1.26-matrix-2026-10-13.log`.
Os dois subgates novos estão `verified-static-binding`; os gates principais
permanecem assets candidate-integrated, rendering not-proven, gameplay
clean-room-integrated, áudio partially-integrated e deformação structural-only.
Readiness: NOT_READY. ISO somente leitura, 2013003776 bytes, Archive,
SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

## Etapa 1.27 — validação otimizada e probe de material (2026-10-13)

- Busca streaming de nomes de áudio: `Span<byte>.IndexOf`, preservando overlaps, EOF e hits entre chunks; sem binding novo.
- Resultado focal: 1.737.085.606 bytes/60 assets em 4.995 ms; resultado da matriz: 3.047 ms. Report e summary mantêm os mesmos SHA-256 anteriores.
- Probe somente leitura: `data/initstaz.big!initstay.big!mat.o`, três símbolos/168 bytes de janelas brutas; não é decoder de shader.
- Validação final: Release 0 warnings/0 errors, 99/99 self-tests, smokes, headless/windowed e replays determinísticos; matriz exit 0 / `completed-authored-and-candidate-opt-in`.
- Cobertura authored: 10 cenários, 777 frames, 54 eventos de áudio, 1.554 amostras de animação.
- Log: `fifa_pc/stage-1.27-matrix-2026-10-13.log`. Detalhes: `docs/FIFA_STREET_2_VALIDACAO_OTIMIZADA_2026-10-13.md`.
- Próximo recorte funcional: provar o consumidor da declaração em `mat.o` e sua associação aos cinco buffers; não atribuir UV/rig/material por suposição.
- Readiness global permanece `NOT_READY`; gameplay clean-room passa, rendering/assets/áudio/deformação continuam com lacunas documentadas.
- ISO: 2013003776 bytes, Archive, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`, sem escrita.

## Etapa 1.28 — declaração de material e envelope VS (2026-10-13)

- Decoder clean-room .NET: `EaglMaterialDeclarationAudit.cs`, CLI `eagl-material-declaration-audit`.
- Fonte fixada `data/initstaz.big!initstay.big!mat.o`: cinco campos stream/registro/formato, declaração de 44 bytes; larguras 12/8/4/12/2. Sem atribuir UV/posição/rig.
- Header VS validado pelo consumidor: versão 0x2078, 46 instruções de 16 bytes, 740 bytes incluindo header. Não há execução ou tradução do shader.
- Controle independente relê a ISO e confere 4 trechos/51 sites do XBE, tabelas de largura e 7 controles negativos.
- Readiness limitado `rendering-material-declaration-and-vs-envelope`: `verified-static-layout`; herança do parent, atributos e rendering completo continuam `not-proven`.
- Primeira matriz falhou por uma guarda adicional equivocada de ELF e_machine=3. Header original=8, guarda/fixture corrigidas com teste negativo para 3. Log inicial preservado.
- Repetição final: Release 0 warnings/0 errors, 100/100 self-tests, smokes, headless/windowed e replays; exit 0 / `completed-authored-and-candidate-opt-in`.
- Log válido: `fifa_pc/stage-1.28-matrix-retry-2026-10-13.log`. Detalhes: `docs/FIFA_STREET_2_MATERIAL_DECLARACAO_2026-10-13.md`.
- Próxima etapa funcional: transformação/resolução do render method serializado e parent runtime +0x2C; decoder estático das instruções com operandos ativos. Não igualar +0x1C serializado e +0x2C runtime por suposição.
- ISO intacta, 2013003776 bytes, Archive, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Readiness global `NOT_READY`; nenhum gate principal foi relaxado.

## Etapa 1.29 — runner Release direto (2026-10-13)

- A matriz mantém build Release obrigatório; os mesmos comandos de aplicação são executados pela DLL compilada, eliminando a reavaliação de projeto por `dotnet run` em cada chamada.
- Adaptador puro `tools/clean-room-release-invocation.ps1`: prefixo exato, arrays de argumentos preservados, sem comandos interpolados, sem XBE/emulação. Dez testes positivos/negativos incluídos na matriz.
- Validação final: Release 0 warnings/0 errors, 100/100 self-tests .NET + 10/10 do runner, smokes, headless/windowed e replay determinístico; exit 0 / `completed-authored-and-candidate-opt-in`.
- Confronto com a etapa 1.28: oito relatórios selecionados byte-idênticos por SHA-256, incluindo readiness e cobertura. A busca de nomes de áudio levou 2.552 ms nesta matriz.
- Cobertura authored inalterada: 10 cenários, 777 frames, 54 eventos de áudio e 1.554 amostras de animação.
- Log: `fifa_pc/stage-1.29-matrix-2026-10-13.log`. Detalhes/hashes/fonte oficial: `docs/FIFA_STREET_2_RUNNER_RELEASE_2026-10-13.md`.
- Próximo avanço funcional: resolver parent serializado→runtime e provar operandos ativos/entradas/saídas do VS em `mat.o`; manter seletor 0x00345BCC e áudio/deformação separados.
- Nenhum prazo ou porcentagem total é inferido da redução de custo da validação. Gameplay clean-room passa; assets/rendering original/áudio/deformação continuam pendentes.
- ISO intacta, acesso somente leitura, 2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Readiness global `NOT_READY`; projeto não concluído, sem code review.


## Etapa 1.30 — operandos estáticos do vertex program (2026-10-13)

- Decoder .NET `EaglVertexProgramAudit.cs` e controle independente que extrai novamente a ISO e usa packing 128-bit: 46/46 instruções, entradas ativas 0/1/3/4, 8 referências de slots, 6 constantes relativas, 6 instruções pareadas, 9 escritas de saída nos endereços 0/3/4/9. Não executa shader/XBE e não prova liveness por componente ou draw efetivo.
- Controle: 5 trechos/65 sites/18 mutações negativas; upload DWORD sem reordenação no trecho 0x291D70. Subgate `rendering-vertex-program-opcode-operands` passou como `verified-static-instruction-layout`.
- Validação final: Release 0/0, 102/102 self-tests .NET + 10/10 runner, smokes, headless/windowed e replays; matriz exit 0 / `completed-authored-and-candidate-opt-in`.
- Log: `fifa_pc/stage-1.30-matrix-2026-10-13.log`; contrato/fontes/hashes: `docs/FIFA_STREET_2_SHADER_OPERANDOS_2026-10-13.md`.
- Próxima evidência: sondagem fresh encontrou parent em +0x1C E +0x2C nos 2364 render methods e referências explícitas Skin=1416, Cloth=594, Eye=354. Validar o registro completo/declaração/definições antes de afirmar resolução do loader ou instalar rendering original; a pista anterior do prefixo era incompleta.
- ISO preservada, somente leitura, 2013003776 bytes, Archive, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Readiness continua `NOT_READY`; sem execução/recompilação/emulação, bindings inventados, conclusão do projeto ou code review.

## Fut Street 2 — etapa 1.31 e identidade pública (2026-10-13)

- Nome público vigente: **Fut Street 2**, conforme pedido do usuário. Título Win32, banners .NET e metadados AssemblyTitle/Product atualizados e testados. Workspace, ISO, `fifa_pc`, `FifaStreetPc.csproj`/DLL/namespace, comandos e nomes internos de assets permanecem preservados por compatibilidade/rastreabilidade; registros históricos não foram apagados.
- Novo auditor .NET `EaglModelMaterialReferenceAudit.cs`, comando `eagl-model-material-reference-audit`, e controle Python com enumeração independente fresh de modelos/layers/descriptors: 2364 primitivas em 883 objetos; declaração byte-idêntica de 44 bytes/5 elementos e referências nomeadas exatas de VS/PS para três fontes em `mat.o`: Skin=1416, Cloth=594, Eye=354; programas VS de 46/46/47 instruções.
- Registro completo de RM tem 52 bytes. Parents externos em +0x1C E +0x2C coincidem nas 2364 linhas. Isso corrige a pista antiga incompleta do prefixo; não prova alias `ParentRM_*`→`__RM:::ParentRM_*` no loader. Handles/estado serializados +0x10/+0x18/+0x24=0; +0x28=0xFFFFFFFF. Sem seleção de buffer, instalação de shader, semântica de atributo ou draw original alegados.
- Controle independente: 4 trechos/53 sites do consumidor XBE e 16 negativos. Subgate `rendering-model-material-program-references`: `verified-static-model-reference`, 2364/2364. `runtime_loader_resolution=not-proven`, `selected_buffers=not-proven`, `full_rendering_binding=not-proven`, `original_semantics=not-claimed`.
- Matriz completa exit 0 / `completed-authored-and-candidate-opt-in`: Release 0/0, 105/105 self-tests .NET + 10/10 runner, smokes, headless/windowed e replays determinísticos. Cobertura authored: 10 cenários, 777 frames, 54 eventos de áudio, 1554 amostras de animação. Log `fifa_pc/stage-1.31-matrix-2026-10-13.log`.
- Depois da matriz, corrigida uma última linha de banner windowed ainda antiga; validação focal final exit 0: Release 0/0, 105 testes, metadados, headless 80 frames, replay match/input/animation/audio byte-idêntico, windowed e captura 960x540. Script `tools/test-fut-street-product-identity.ps1`; log `fifa_pc/stage-1.31-product-identity-2026-10-13.log`. Essa rodada final é focal, não uma segunda matriz completa.
- Contrato, hashes, fontes e limites: `docs/FUT_STREET_2_MODELOS_MATERIAIS_2026-10-13.md`. Nova entrada de documentação: `README.md`. Relatórios anteriores de declaração e operandos mantiveram seus hashes; readiness ganhou somente o subgate delimitado.
- Próxima etapa: investigar resolução de parent/handles e o caminho de seleção/instalação de draw, buffers e constantes, incluindo 0x00345BCC. Pista adicional somente leitura: literais `ParentRM_`/`RM` em 0x002D1DE0/0x002D1DEC, consumidor 0x00138A70 e busca chamada em 0x00138AE1→0x001987F0. É pista, não prova de loader, alias ELF ou instância; não foi promovida a gate.
- Gates principais: assets `candidate-integrated` (não passou), rendering `not-proven` (não passou), gameplay authored `clean-room-integrated` (passou sem fidelidade original alegada), áudio `partially-integrated` e animação/deformação `structural-only` (não passaram). Não deduzir porcentagem/ETA ou fim do projeto pelo número de subgates.
- ISO intacta nas duas rodadas, exclusivamente entrada somente leitura: 2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
- Readiness global `NOT_READY`; continuar clean-room .NET, sem emulação/recompilação/execução do XBE, bindings inventados, conclusão do projeto ou code review.

## Publicação GitHub do Fut Street 2 (2026-10-13)

- Repositório público, conforme escolha explícita do usuário: [MarllonMenezes/fut-street-2](https://github.com/MarllonMenezes/fut-street-2), branch `main`. Workspace agora tem Git local; remote `origin` aponta para esse repositório via HTTPS. Snapshot completo inicial `2d396380ea6506a569ea6ef8ea3606d185268125` enviado e verificado contra o remoto.
- Apenas 304 arquivos de código, testes e documentação. `.gitignore` usa lista permitida: ISO, XBE, arquivos de jogo, mídia extraída, exports, TSV/CSV/logs, bin/obj/caches e projeto Star Wars não entram no repositório. Nenhum arquivo local foi apagado. `.gitattributes` impede conversão automática de line endings para preservar bytes/hashes históricos.
- Checagem inicial isolada detectou a omissão de `Program.cs` no pacote; lista corrigida antes de publicar. Snapshot completo extraído sem ISO/assets/relatórios: Release 0/0, self-tests .NET 105/105 e runner 10/10. Testes Python no workspace original: 57/57. Checagens de tamanho/tipo e assinaturas de credenciais conhecidas passaram.
- Build/self-test não precisam da ISO. Matriz completa e análises de dados requerem a ISO local e relatórios intermediários que não são distribuídos; preparar essas entradas antes de tentar a matriz em um novo clone. Instruções: `docs/GITHUB_PUBLICACAO.md`.
- Commit usa GitHub noreply para não publicar o e-mail pessoal do Git global. Nenhuma licença open-source foi escolhida automaticamente. Não foi criado pull request nem iniciado code review.
- ISO preservada: 2013003776 bytes, Archive, LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`. Upload não muda gates ou readiness: `NOT_READY`; retomar a próxima investigação da etapa 1.31.

## Etapa 1.32 — primeiro executável host linkado sem NV2A/APU (2026-10-13)

- Evidência do entrypoint: loader estático já registra retail VA `0x0024D8BE`; a tabela de dispatch gerada associa a mesma VA a `xbe_entry_point`.
- CMake adicionou um target kernel explícito com 18 fontes upstream, omitindo `nv2a_pb_scan.c`, `nv2a_pb_exec.c`, `nv2a_vsh_interp.c` e `nv2a_combiner.c`; os hooks de scan ficam inertes. Também adicionou fallback manual-lookup sem overrides, diagnósticos ICALL/UNIMPL, bootstrap e teste CTest.
- Foram ligadas bibliotecas selecionadas D3D8→D3D11, vídeo, XInput, imports de sistema e dispatcher gerado. Não usar o target guarda-chuva `xboxrecomp` (inclui `xbox_apu`/`xbox_nv2a`).
- `.exe` local `fut_street_2_recompiled_app.exe`, 165,368,832 bytes, link Release com `--no-undefined`. CTest 2/2: `no_emulation_runtime_support` e `recompiled_host_help`. O único smoke do app foi `--help`; nenhum XBE nem código recompilado foi executado.
- Warnings da compilação nativa vieram dos fontes upstream do kernel e continuam visíveis. A DLL XInput/D3DCompiler e o C/XBE gerado permanecem locais; nada disso é commitado.
- Isso prova link e parsing do CLI somente. Não prova boot, correctness do kernel/API, rendering, gameplay, animação ou áudio; DirectSound não está linkado. ISO inalterada. Readiness continua `NOT_READY`.
- Referência detalhada: `docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md`; scaffold/comandos: `native_recomp/README.md`. Próximo avanço: fechar os imports/stubs de kernel usados no startup e o backend de áudio com evidência antes de qualquer bring-up.

## Etapa 1.33 — auditoria estática dos destinos stub (2026-10-13)

- Ferramenta adicionada: `tools/analysis/xboxrecomp_unresolved_target_audit.py`; testes em `tools/tests/test_fifa_analysis.py` (4 novos casos).
- Join do arquivo `recomp_stubs_unresolved.c` local com `functions.json` e `default_analysis.json` do XBE: 86 destinos; 74 interiores, 4 com intervalos sobrepostos, 1 no fim de função e 7 sem intervalo conhecido. Por seção: `.text` 76, `DSOUND` 2, `XPP` 2, `.rdata` 2, sem seção 4.
- 79 sem `ret N` detectado; 7 com metadado. A primeira classificação com limites fixos foi descartada; usar sempre os limites reais do XBE. Dois destinos em `.rdata` ficam desalinhados em relação ao início da tabela kernel thunk: suspeita estática, não binding.
- Nenhum destino foi tratado como função nova, nenhum código recompilado foi executado e nenhum gate mudou. Detalhes de VA permanecem fora do Git por conterem dados derivados do jogo.
- Relatório e limites: `docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md`. Próximo passo: estudar call sites e limites dessas regiões antes de mudar a descoberta de funções; não fornecer observações dinâmicas sem bring-up autorizado.

## Continuação após a etapa 1.35 (2026-10-13)

- Consulte a etapa 1.35 em `docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md`.
- O patch upstream `native_recomp/patches/0002-detect-late-alias-tail-jumps.patch`
  agora deve ser aplicado depois de `0001` ao reproduzir a nova tradução com
  XboxRecomp v0.13.1. A geração local passou a ter 87 stubs e 154 no-ops; não
  compare contagens brutas sem classificar o delta de cobertura.
- No Cloud, trabalhar somente no repositório público e ambiente publicado;
  ISO, XBE extraído, assets e C derivado ficam locais. Não fornecer credenciais
  pois build/self-test do código versionado não requerem segredos. Consultar
  `docs/CODEX_CLOUD_CONTINUACAO.md` e respeitar seus limites.
- Próxima investigação: refazer a auditoria de stubs com os novos artefatos
  locais e correlacionar destinos recém-expostos sem inventar bindings; manter
  rendering, assets, gameplay e áudio como gates separados. Estado permanece
  `NOT_READY`. Não iniciar o XBE ou `--launch` sem autorização de bring-up.

## Continuação após a etapa 1.36 (2026-10-13)

- Consulte a etapa 1.36 em `docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md`
  e aplique os patches XboxRecomp na ordem 0001, 0002, 0003.
- A auditoria mais recente tem 81 stubs e 154 instruções no-op; dezessete
  stubs prévios desapareceram e onze destinos foram expostos. Classifique os
  deltas usando os relatórios locais, sem publicar VAs em massa, inferir ABI
  ou tratar ownership sobreposto como entrada provada.
- Continue o runtime/kernel e integração de áudio por contratos e evidência.
  Rendering, assets, gameplay, animação e áudio permanecem gates separados;
  nenhuma build host/link smoke substitui execução fiel do XBE. Não iniciar o
  XBE nem `--launch` sem autorização de bring-up.


Atualização de continuidade (etapa 1.46, 2026-10-13): patch XboxRecomp 0007
traduz SALC conforme Intel SDM IA-32. A regeneração confirma 15702/15702
funções, 71 call stubs e 130/31 instruções/mnemonics sem lowering. Testes
focados `520 passed, 38 skipped`; build CMake Release/link, CTest `4/4` e
`--help` passaram. Não houve execução do XBE, headless ou replay original;
gates `NOT_READY`. ISO somente leitura, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Atualização de continuidade (etapa 1.47, 2026-10-13): patch XboxRecomp 0008
traduz AAM/AAD para bases imediatas não nulas; `AAM 0` continua sem lowering
porque dispara `#DE`, ainda sem despacho de exceção guest. Reaplicação limpa
de 0001–0008: testes focados 525 passed/38 skipped, geração 15702/15702,
71 stubs e 116/29 TODOs. CMake Release/link, CTest 4/4 e `--help` passaram.
Não executar XBE/`--launch`; headless/replay original não alegados. Gates
`NOT_READY`. ISO somente leitura, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Atualização de continuidade (etapa 1.48, 2026-10-13): patches 0009–0010
adicionam DAA/DAS/AF e guard conservador de CF no mesmo basic block. Sites sem
evidência de CF permanecem não traduzidos; não inferir flags entre blocos nem
declarar alcançabilidade. Pilha limpa 0001–0010: 531 passed/38 skipped;
geração 15702/15702, zero falhas, 71 stubs, 96 instruções/29 mnemonics sem
lowering; Release/link, CTest 4/4 e `--help` aprovados. Nenhum XBE iniciado;
nenhum boot/headless/replay do jogo alegado. Gates permanecem `NOT_READY`,
projeto não concluído. ISO somente leitura, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Atualização de continuidade (etapa 1.51, 2026-10-13): patch XboxRecomp 0013
recupera uma entrada de branch para trás dentro de corpo pai compartilhado por
alias de cauda, somente quando os owners de origem/alvo concordam no fim. A
suíte `tools/disasm` + `tools/recomp`: 540 passed/38 skipped; geração
15717/15717, zero falhas, 46 stubs, 92 instruções/28 mnemônicos sem lowering.
CMake Release/link do host seletivo, CTest 4/4 e `--help` passaram. Nenhum
XBE/`--launch` iniciado; autorização específica de bring-up ainda necessária.
Sem boot/headless/replay original. Gates continuam `NOT_READY`. ISO somente
leitura, SHA-256 `F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Atualização de continuidade (etapa 1.49, 2026-10-13): patch 0011 traduz AAA/AAS
somente após AF rastreado no mesmo basic block; somente CF é considerado
definido depois. Dois sites AAS foram traduzidos. `AAA 0x000C37FD` e
`AAS 0x00296468` ficam TODO por falta de proveniência de AF. Pilha 0001–0011:
537 passed/38 skipped; geração 15702/15702, zero falhas, 71 stubs, 94 TODOs/29
mnemonics; Release/link, CTest 4/4 e `--help` passaram. Nenhum XBE iniciado,
sem runtime headless/replay do jogo. Gates `NOT_READY`; projeto incompleto. ISO
somente leitura, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.

Atualização de continuidade (etapa 1.52, 2026-10-13): patch XboxRecomp 0014
recupera destinos de tail-jump que ficam dentro de corpos pais sobrepostos,
apenas quando todos os owners do destino concordam no fim. Checkout limpo
v0.13.1 fixado, patches 0001–0014: 542 passed/38 skipped; geração
15673/15673, zero falhas, 26 stubs e 93 instruções/28 mnemônicos sem lowering.
Auditoria: 22 destinos interiores a funções, quatro sem cobertura e três com
XRefs diretos de branch; nenhum stub restante está em range sobreposto. CMake
Release/link, CTest 4/4, `--help` e self-test .NET 105/105 passaram. Nenhum
XBE/`--launch` iniciado; boot e gates de rendering, assets, gameplay, animação
e áudio seguem `NOT_READY`. Próximo passo: investigar os 3 destinos com XRefs
diretos de branch e os 93 sites sem lowering, com evidência de bytes/fluxo e
sem coalescer boundaries ambíguos. Não iniciar XBE/`--launch` sem autorização
específica de bring-up. ISO somente leitura, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.




