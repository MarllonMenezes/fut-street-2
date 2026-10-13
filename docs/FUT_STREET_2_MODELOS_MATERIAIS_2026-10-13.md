# Fut Street 2 — etapa 1.31: referências explícitas modelo/material

O nome público passa a **Fut Street 2**. Título Win32, banners .NET, metadados
AssemblyTitle/Product e README foram atualizados; um self-test garante a
identidade e a compatibilidade do assembly `FifaStreetPc`. Pasta, namespace,
CLI, ISO, nomes internos dos assets e documentos históricos são preservados.
Nenhum identificador ou byte do jogo original foi renomeado.

## Avanço implementado

Novo auditor .NET `fifa_pc/Runtime/EaglModelMaterialReferenceAudit.cs`:
`eagl-model-material-reference-audit <iso> --report <tsv> --summary <tsv>`.
Reabre a ISO e percorre os objetos originais, sem promover junções nominais
ou perfis geométricos a prova. O controle Python
`tools/analysis/eagl_model_material_reference_control.py` enumera novamente
os modelos, layers, descriptors e render methods por ELF e compara cada
campo do relatório .NET; os offsets do relatório não orientam a busca.

Resultado focal: **2.364 primitivas em 883 objetos**, três definições nomeadas
em `data/initstaz.big!initstay.big!mat.o`. Declaração de 44 bytes/5 elementos
exatamente igual nas 2.364 linhas e nas três fontes, SHA-256
`2205EC6F6DEB9CB77F0A6B23755DBB2C98C833355CABF8B4C61F5C8DD8468CBE`.

| Programa nomeado | Primitivas | Instruções | SHA-256 do VS |
| --- | ---: | ---: | --- |
| FifastCharacterSkin0__EAGLMicroCodeVS | 1.416 | 46 | B37951FBB3188E7843A9492EF675B4CBEA9EBA75CAA412942746BF1212CDFB67 |
| FifastCharacterCloth0__EAGLMicroCodeVS | 594 | 46 | D17E05FE970D37E5D67B7FD3649AFC9F8F58D27CFE4EBF92DEA0737943CD363B |
| FifastCharacterEye0__EAGLMicroCodeVS | 354 | 47 | 307153C2F396F2FBAC94F855AF3BB6F82FD4CF1B9DFD6A264A90EE02B364B609 |

Registro serializado completo: 52 bytes, referência de comandos em +0x00,
count=1 em +0x04, declaração local em +0x08, tabelas VS/PS em +0x0C/+0x14,
parent nomeado em **+0x1C e +0x2C**. Os dois parents são iguais em 2.364/2.364
linhas e coerentes com VS/PS nomeados, sem addend. Handles/estado em
+0x10/+0x18/+0x24 são zero; +0x28 é 0xFFFFFFFF. A leitura antiga restrita ao
prefixo deixava +0x2C de fora: não há necessidade de inventar um deslocamento
de +0x1C para +0x2C para explicar os bytes serializados.

Não afirmamos que `ParentRM_*` resolve automaticamente `__RM:::ParentRM_*`
no loader: esse alias e a resolução efetiva ainda exigem prova. O nome VS/PS
tem definição exata, distinta dessa questão do prefixo do parent.

## Controles e limites

Relocations devem ser únicas, do tipo observado e com nomes/addends coerentes;
campos numéricos e declaração não podem conter relocation sobreposta. Se o
universo de objetos ficar incompleto ou um registro não for suportado,
nenhum relatório verificado novo é emitido. Self-tests cobrem nomes errados,
campos adulterados, bounds, comandos ausentes, duplicidades e promoção
indevida de readiness.

O controle estático do XBE verifica 4 trechos/53 sites, incluindo consumo
de +0x2C em 0x1971E0, constructor em 0x197290 e cópia de declaração/tabelas/
handles em 0x196E90. São 16 controles negativos. Isso confirma campos do
consumidor, **não uma instância do loader, instalação de shader ou draw**.
Nenhuma operação do shader/XBE é executada, recompilada ou emulada.

Subgate limitado: `rendering-model-material-program-references`,
`verified-static-model-reference`, 2.364/2.364.
`runtime_loader_resolution=not-proven`, `selected_buffers=not-proven`,
`full_rendering_binding=not-proven`, `original_semantics=not-claimed`.
Gates principais não relaxados: assets, rendering, áudio e deformação ainda
abertos; gameplay authored integrado não prova gameplay original.

## Validação

Focal .NET e controle independente exit 0. Relatório SHA-256:
`A8A6BC0A6D79258586F40B740DF327ADBCD5C5B15D518135B79DFDEAF120DC50`;
controle estático:
`11DDCA2A39CEEE01C9609AF75FF99123A4DE6260D8640233A42F45AA062627DC`.
Matriz completa exit 0, `completed-authored-and-candidate-opt-in`: build
Release 0 warnings/0 errors, 105/105 self-tests .NET + 10/10 do runner,
smokes, headless/windowed e replays determinísticos aprovados. Log:
`fifa_pc/stage-1.31-matrix-2026-10-13.log`. Declaração/envelope e operandos
anteriores mantiveram os SHA-256 dos relatórios, respectivamente
`D439A519D72E2F9718EAD9A312CD8C3997C3A60A43EC9398014F966F79488F6B` e
`458087B9600677BF1C2D64799B0101EA74478B14C1D1B4699E2B163A8F9E766F`.

Após a matriz, a conferência encontrou uma última linha de banner windowed
com o nome antigo (já interpolada, fora da substituição inicial dos literais).
Foi corrigida sem mudar gameplay/rendering; a validação focal final reproduz
build Release 0/0, 105/105 self-tests, metadados de produto, headless 80 frames,
replay byte-idêntico match/input/animation/audio e runtime windowed com
captura 960×540. Banners finais .NET e metadados verificados como Fut Street 2.
Comando reproduzível: `pwsh -NoProfile -File tools/test-fut-street-product-identity.ps1`.
Log: `fifa_pc/stage-1.31-product-identity-2026-10-13.log`, exit 0,
`product_identity_status=verified`. Não se alega uma segunda matriz completa
após essa mudança exclusivamente de texto: a rodada final é focal e explícita.

ISO intacta em ambas as rodadas: 2.013.003.776 bytes, atributo Archive,
LastWriteTimeUtc 2026-01-22 10:03:26, SHA-256
`F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2`.
Readiness `NOT_READY`; nenhum gate principal foi promovido indevidamente.

Hashes de código/controle: auditor .NET
`6FC29BF523887FD4DE76295408989A304FC49284F08137CFC950530A222807E7`;
controle Python
`348FFB5DC374D4323974D9A0EE0A60AF3CEC0C8BCE32DC0AA7966D0A5AEBDBD6`;
summary independente
`FF29AA249E78A6FCC41E8635996993FB36A6EC171E03D6FAEF9ECA8B8B42838B`;
readiness
`E34004A66FCE75E4AE5F7F8E171D522873F3CD03731EA62E8A5BF22C6468DF3C`.

## Próxima etapa recomendada

Concentrar a investigação no caminho que seleciona o render method e buffers
de draw (`0x00345BCC`), resolução dos parents/handles e constantes de shader.
Só então conectar entradas numéricas à interpretação/renderização .NET.
Não atribuir slot global a UV/posição ou instalar skinning com palette
inventada. Áudio original precisa da cadeia EVT→seleção→amostra→playback,
independente dessas referências de material. Readiness global `NOT_READY`.

Sondagem adicional somente leitura, fora do subgate: no XBE fixado de SHA-256
`5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B`,
`0x002D1DE0` contém `ParentRM_` e `0x002D1DEC` contém `RM`. O trecho que
começa em `0x00138A70` copia o prefixo para uma string local, acrescenta a
string recebida e chama `0x001987F0` em `0x00138AE1`, passando a categoria
`RM` e uma saída local. Isso fornece um consumidor concreto de busca nomeada
para investigar; **não** prova alias de símbolo ELF, resolução geral do loader,
instância carregada ou parent efetivamente escolhido. Não foi promovido a gate.
