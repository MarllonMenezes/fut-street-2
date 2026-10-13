# FIFA Street 2 — análise do XBE

Data: 2026-10-04  
Entrada: `D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso`  
Executável interno: `default.xbe`  
Offset bruto no ISO: `0x84800`  
Tamanho: `3.293.184` bytes

A análise foi feita diretamente sobre a extensão do arquivo no XDVDFS. O XBE
original não foi extraído para substituição, modificado ou assinado.

## Resumo

O arquivo é um XBE retail do Xbox original com:

- base address `0x00010000`;
- 11 seções;
- entry point codificado que decodifica com a chave retail para `0x0024D8BE`;
- 128 entradas na tabela de thunks do `XBOXKRNL`;
- seis bibliotecas declaradas: `XAPILIB`, `XGRAPHC`, `DSOUND`, `XBOXKRNL`, `D3D8` e `LIBCMT`;
- TLS presente, sem callbacks registrados;
- hashes SHA-1 das 11 seções compatíveis com os bytes do XBE usando a convenção XBE de prefixar o tamanho da seção;
- tabela de imports de debug não presente (`DebugImportTable = 0`).

Isso comprova dependências reais de sistema, gráficos, áudio e runtime C. Ainda
não comprova o comportamento interno de cada subsistema nem fornece código-fonte
reconstruível automaticamente.

## Header XBE

Todos os valores multi-byte abaixo foram lidos como little-endian, conforme o
formato XBE.

| Campo | Valor |
|---|---:|
| Magic | `XBEH` |
| Base address | `0x00010000` |
| Size of headers | `0x000009A8` |
| Size of image | `0x0035C580` |
| XBE header size | `0x00000184` |
| Certificate pointer | `0x00010184` → arquivo `0x184` |
| Number of sections | `11` |
| Section headers pointer | `0x00010370` → arquivo `0x370` |
| Init flags | `0x00000005` |
| Entry point codificado | `0xA8D88F15` |
| Entry point retail | `0x0024D8BE` |
| Entry point debug candidato | `0x3C5D125E` |
| TLS pointer | `0x0030075C` |
| Stack commit | `0x00020000` |
| Heap reserve | `0x00100000` |
| Heap commit | `0x00001000` |
| PE base address | `0x00010C20` |
| PE image size | `0x003809A0` |
| Debug import pointer | `0x00000000` |
| Number of library versions | `6` |
| Kernel thunk codificado | `0x5B47D096` |
| Kernel thunk retail | `0x002A9020` |

O entry point retail cai na seção `.text`. O candidato decodificado com a chave
debug não cai em uma seção executável, portanto a hipótese retail é a
interpretação consistente com esta imagem.

## Certificado

O certificado começa no offset `0x184` do XBE e tem tamanho `0x1EC` (492 bytes).

| Campo | Valor |
|---|---:|
| Título | `FIFA STREET` |
| Title ID | `0x45410085` |
| Allowed media types | `0x00000202` |
| Game region | `0x00000001` |
| Game rating | `0x00000004` |
| Disk number | `0` |
| Version | `2` |
| Original certificate size | `492` |
| Runtime security flags | `0x00000002` |

Os 16 alternate title IDs estão zerados. Os flags foram preservados como
valores brutos; a interpretação de cada bit não é usada como evidência de
funcionalidade do jogo.

## Seções

`file_address` e `file_size` são relativos ao início do XBE, não ao início do
ISO. `virtual_address` é o endereço de carregamento no espaço Xbox.

| # | Nome | Flags | Virtual address | Virtual size | File address | File size | SHA-1 |
|---:|---|---:|---:|---:|---:|---:|---|
| 0 | `.text` | `0x16` | `0x00110000` | `0x271AD8` | `0x001000` | `0x271AD8` | OK |
| 1 | `XGRPH` | `0x07` | `0x00282AE0` | `0x23C1` | `0x273000` | `0x23C0` | OK |
| 2 | `DSOUND` | `0x07` | `0x00284EC0` | `0x87FC` | `0x276000` | `0x858C` | OK |
| 3 | `D3D` | `0x07` | `0x0028D6C0` | `0x13AB0` | `0x27F000` | `0x101E8` | OK |
| 4 | `XPP` | `0x07` | `0x002A1180` | `0x7E84` | `0x290000` | `0x7E84` | OK |
| 5 | `.rdata` | `0x06` | `0x002A9020` | `0x625AC` | `0x298000` | `0x6259C` | OK |
| 6 | `.data` | `0x07` | `0x0030B5E0` | `0x560FC` | `0x2FB000` | `0x1BF94` | OK |
| 7 | `DOLBY` | `0x26` | `0x003616E0` | `0x7180` | `0x317000` | `0x716C` | OK |
| 8 | `$$XTIMAGE` | `0x38` | `0x00368860` | `0x2800` | `0x31F000` | `0x2800` | OK |
| 9 | `$$XSIMAGE` | `0x38` | `0x0036B060` | `0x1000` | `0x322000` | `0x1000` | OK |
| 10 | `.XTLID` | `0x38` | `0x0036C060` | `0x520` | `0x323000` | `0x520` | OK |

Os nomes `XGRPH`, `DSOUND`, `D3D`, `DOLBY`, `$$XTIMAGE` e `$$XSIMAGE` são
evidências de conteúdo/linkagem gráfica, áudio e imagem. Não foram tratados
como DLLs independentes: são seções do próprio XBE.

## TLS

O TLS aponta para o offset de arquivo `0x2EF73C`, dentro da imagem carregada.

| Campo | Valor |
|---|---:|
| Raw data start | `0x00000000` |
| Raw data end | `0x00000000` |
| TLS index address | `0x003551FC` |
| TLS callbacks | `0x00000000` |
| Size of zero fill | `0x94` |
| Characteristics raw | `00 00 00 00 5C 3F 3F 5C` |

Não há callback TLS identificado no header.

## Bibliotecas declaradas

Todas usam a versão observada `1.0.5849`; os flags foram mantidos no valor bruto.

| Biblioteca | Versão | Flags |
|---|---:|---:|
| `XAPILIB` | `1.0.5849` | `0x4001` |
| `XGRAPHC` | `1.0.5849` | `0x4009` |
| `DSOUND` | `1.0.5849` | `0x400B` |
| `XBOXKRNL` | `1.0.5849` | `0x4001` |
| `D3D8` | `1.0.5849` | `0x400F` |
| `LIBCMT` | `1.0.5849` | `0x4001` |

Isso é evidência direta de dependência de XAPI, XGRAPH, Direct3D 8 Xbox,
DirectSound Xbox, kernel Xbox e runtime C.

## Imports do kernel

O ponteiro codificado com a chave retail `0x5B6D40B6` produz o endereço virtual
`0x002A9020`, que é mapeado para o offset de arquivo `0x298000`, início da
seção `.rdata`. A tabela possui 128 thunks, terminando em zero.

Os ordinals foram comparados com a tabela pública de exports do `xboxkrnl`.
Os nomes abaixo são identificação nominal do export; não substituem a análise
da chamada e dos argumentos no código do jogo.

### Categorias observadas

- sincronização: `RtlInitializeCriticalSection`, `RtlEnterCriticalSection`,
  `RtlLeaveCriticalSection`, eventos, semáforos, mutants e waits;
- filesystem: `NtCreateFile`, `NtOpenFile`, `NtReadFile`, `NtWriteFile`,
  `NtQueryDirectoryFile`, `NtQueryInformationFile`, `NtFsControlFile`,
  `NtQueryVolumeInformationFile`, `IoCreateSymbolicLink`;
- memória: `MmAllocateContiguousMemory`, `MmAllocateContiguousMemoryEx`,
  `MmFreeContiguousMemory`, `MmQueryAllocationSize`, `MmGetPhysicalAddress`,
  `MmLockUnlockBufferPages`, `NtAllocateVirtualMemory`, `NtFreeVirtualMemory`;
- threads/IRQL: `PsCreateSystemThreadEx`, `PsTerminateSystemThread`,
  `KeDelayExecutionThread`, `KeWaitForSingleObject`, timers, DPCs e IRQL;
- gráficos/hardware: `AvSetDisplayMode`, `AvSendTVEncoderOption`,
  `MmClaimGpuInstanceMemory`, `HalBootSMCVideoMode`, `XeLoadSection`;
- criptografia/identidade: `XcSHAInit`, `XcSHAUpdate`, `XcSHAFinal`, `XcHMAC`,
  `XboxHDKey`, `XboxSignatureKey`, `XboxAlternateSignatureKeys`;
- energia/disco: `HalReturnToFirmware`, `HalInitiateShutdown`,
  `HalIsResetOrShutdownPending`, `IoDismountVolumeByName`.

### Ordinals preservados

```text
291,277,294,24,166,180,182,171,179,189,16,289,225,186,205,193,
222,192,221,234,99,301,187,226,305,128,156,304,178,165,164,49,
202,210,37,207,161,129,65,145,159,113,107,74,218,236,40,219,
67,215,203,69,200,233,211,198,196,250,246,190,197,143,259,124,
224,302,206,238,258,255,150,125,158,97,336,335,337,323,325,354,
91,356,326,149,322,83,87,81,17,15,359,358,47,324,199,184,269,
217,340,360,44,160,98,109,151,279,327,328,357,173,175,119,176,
142,139,23,137,100,153,1,2,3,4,46,8,168,312,95
```

## Strings e evidências de subsistemas

Foram encontradas 14.470 sequências ASCII imprimíveis com pelo menos quatro
caracteres. Exemplos relevantes:

| Evidência | Indicação limitada |
|---|---|
| `C:\\fifast2\\source\\common\\ai\\...` | caminhos de fonte/debug preservados para IA |
| `...\\gamelib\\geomlib\\collisionmesh.cpp` | geometria e colisão |
| `...\\common\\ai\\physics\\ball\\ball.cpp` | física da bola |
| `...\\common\\animation\\...` | animação e warpers |
| `...\\common\\render\\eyecontroller.cpp` | render/câmera |
| `...\\common\\usercontrol\\...` | controle do jogador |
| `AUDIO/AMBSTRM.ABK`, `AUDIO/AMBSTRM.AST` | carregamento de áudio/streams |
| `%s.big`, `Load Header Bigfile`, `BigFile` | uso de bigfiles |
| `D3DTexture`, `EAGLAnimationBuffer`, `D3DPalette` | estruturas e runtime gráfico |
| `FileSystem Initialized.`, `FileSystemErrorCallback` | filesystem do jogo |
| `Loading model assets`, `Loading stadium`, `Load Anim Banks` | pipeline de assets |
| `AptLoader`, erros de ActionScript | UI/Flash-like runtime APT |

Os caminhos de fonte são evidência de strings de diagnóstico ou símbolos
parciais, não uma recuperação do código-fonte original.

### Auditoria estática de referências a strings

Como refinamento somente leitura, `tools/analysis/xbe_string_xref_audit.py`
decodifica as seções marcadas como executáveis como x86 32-bit com Capstone e
registra somente operandos imediatos ou de memória absoluta que coincidem com
endereços de strings interessantes. Na imagem atual foram encontradas 1.929
strings literais de interesse e 944 referências candidatas em 944 instruções
de origem: 939 candidatos por imediato e 5 por memória absoluta. O conjunto
inclui referências literais observadas para `AUDIO/AMBSTRM.ABK`,
`AUDIO/AMBSTRM.AST`, `.xsh`, `D3DTexture`, `EAGLAnimationBuffer` e vários
`.big`.

Os relatórios são `fifa_pc/xbe-string-xref-audit.tsv` e
`fifa_pc/xbe-string-xref-summary.tsv`; o SHA-256 do XBE lido é
`5fabb5b763be0cc8823e814ee343ac5be6950305330f46c5170e08eaadea910b`.
Esses números são referências estáticas candidatas, não limites de função,
fluxos executados, bindings de asset, materiais, UVs, skinning, rigging,
animação, morph ou comandos GPU. Cada linha permanece com
`status=candidate-x86-reference` e `original_semantics=not-claimed`.

O cruzamento local opcional `--call-context` produziu
`fifa_pc/xbe-string-xref-call-context.tsv`: 6.251 linhas de contexto para as
944 referências, 790/944 com pelo menos uma chamada direta em uma janela de
128 bytes e 1.141 destinos executáveis distintos próximos. A janela é apenas
uma vizinhança de bytes/instruções decodificadas; não define função, chamada
de subsistema, fluxo de asset ou binding. O relatório mantém
`status=candidate-local-x86-call-context` e `original_semantics=not-claimed`.

O refinamento `--call-clusters` agrupou essas linhas somente pelo endereço do
destino executável observado: 1.141 clusters, dos quais 306 coocorrem com pelo
menos duas strings distintas. O relatório é
`fifa_pc/xbe-string-xref-call-clusters.tsv`, SHA-256
`9A5F416ED66485B2D4E0D2087E3E400711627AA0352CCAB9E6194EC818AF4CF3`; o
resumo atualizado é `fifa_pc/xbe-string-xref-summary.tsv`, SHA-256
`19A633D47CC2FA5CE30D800AE6C52ABD63CDF785546F31DC190A892E72A862BD`.
Isso é coocorrência local de referências e chamadas, não recuperação de
funções: não define limites, subsistemas, fluxo, binding ou semântica original;
cada linha permanece `original_semantics=not-claimed`.

## O que ainda não foi provado

- nomes e limites de funções individuais;
- calling convention efetivamente usada por cada função;
- conteúdo do import binding de `D3D8`, `DSOUND` e `XGRAPHC` além da dependência declarada;
- existência de uma tabela de relocação tradicional;
- assinatura RSA do header; esta análise não tentou validar a assinatura;
- formato interno completo de `.big`, `.bh`, `.viv`, `.abk`, `.ast` e `.xsh`;
- equivalência entre os símbolos de debug e funções realmente presentes no XBE.
- correspondência semântica entre qualquer referência literal e um binding
  específico de runtime; o relatório de strings só reduz o espaço de busca.

## Consequência para a estratégia PC

Para FIFA Street 2, uma recompilação direta para Windows x64 não é uma simples
compilação do XBE: o binário é x86 de Xbox, depende de XAPI/D3D8/DirectSound
Xbox, usa memória física/contígua, thunks de kernel, GPU, TV encoder e estruturas
de armazenamento específicas.

O próximo passo técnico mais seguro é:

1. confirmar limites por fluxo básico para o mapa de funções x86 começando pelo
   entry point `0x0024D8BE`;
2. cruzar as referências literais candidatas com chamadas e tabelas de formato,
   sem promovê-las a bindings;
3. identificar o startup, inicialização do filesystem e loop principal;
4. só depois escolher entre tradução x86, camada de compatibilidade ou
   reimplementação parcial.

## Referências de formato

- [XboxDev/xbedump — estrutura XBE](https://github.com/XboxDev/xbedump/blob/master/xbestructure.h)
- [XboxDev/xbedump — tabela de exports do XBOXKRNL](https://github.com/XboxDev/xbedump/blob/master/xboxkrnl.h)
- [XboxDev/xbedump — validação de hashes de seção](https://github.com/XboxDev/xbedump/blob/master/xboxlib.c)
