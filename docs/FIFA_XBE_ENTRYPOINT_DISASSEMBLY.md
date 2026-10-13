# FIFA Street 2 — disassembly inicial do entry point

Disassembly x86 de uma janela limitada a partir do entry point retail. O XBE foi lido diretamente do XDVDFS; a imagem original não foi alterada.

- Executável: `default.xbe`
- Entry point: `0x0024D8BE`
- Offset no XBE: `0x0023D8BE`
- Seção: `.text`
- Janela: `0x0024D8BE`–`0x0024D9BE`

| Endereço | Bytes | Instrução | Alvo |
|---:|---|---|---:|
| `0x0024D8BE` | `8b 0d 18 01 01 00` | `mov ecx, dword ptr [0x10118]` | `` |
| `0x0024D8C4` | `a1 08 01 01 00` | `mov eax, dword ptr [0x10108]` | `` |
| `0x0024D8C9` | `2b c1` | `sub eax, ecx` | `` |
| `0x0024D8CB` | `05 00 00 01 00` | `add eax, 0x10000` | `` |
| `0x0024D8D0` | `3b 01` | `cmp eax, dword ptr [ecx]` | `` |
| `0x0024D8D2` | `73 02` | `jae 0x24d8d6` | `` |
| `0x0024D8D4` | `89 01` | `mov dword ptr [ecx], eax` | `` |
| `0x0024D8D6` | `a1 6c 07 30 00` | `mov eax, dword ptr [0x30076c]` | `` |
| `0x0024D8DB` | `2b 05 5c 07 30 00` | `sub eax, dword ptr [0x30075c]` | `` |
| `0x0024D8E1` | `8b 0d 60 07 30 00` | `mov ecx, dword ptr [0x300760]` | `` |
| `0x0024D8E7` | `56` | `push esi` | `` |
| `0x0024D8E8` | `8d 44 08 0f` | `lea eax, [eax + ecx + 0xf]` | `` |
| `0x0024D8EC` | `57` | `push edi` | `` |
| `0x0024D8ED` | `83 e0 f0` | `and eax, 0xfffffff0` | `` |
| `0x0024D8F0` | `6a fc` | `push -4` | `` |
| `0x0024D8F2` | `59` | `pop ecx` | `` |
| `0x0024D8F3` | `83 c0 04` | `add eax, 4` | `` |
| `0x0024D8F6` | `a3 50 13 36 00` | `mov dword ptr [0x361350], eax` | `` |
| `0x0024D8FB` | `99` | `cdq` | `` |
| `0x0024D8FC` | `f7 f9` | `idiv ecx` | `` |
| `0x0024D8FE` | `8b 0d 64 07 30 00` | `mov ecx, dword ptr [0x300764]` | `` |
| `0x0024D904` | `33 f6` | `xor esi, esi` | `` |
| `0x0024D906` | `56` | `push esi` | `` |
| `0x0024D907` | `56` | `push esi` | `` |
| `0x0024D908` | `56` | `push esi` | `` |
| `0x0024D909` | `68 4a d8 24 00` | `push 0x24d84a` | `` |
| `0x0024D90E` | `56` | `push esi` | `` |
| `0x0024D90F` | `56` | `push esi` | `` |
| `0x0024D910` | `89 01` | `mov dword ptr [ecx], eax` | `` |
| `0x0024D912` | `e8 23 12 00 00` | `call 0x24eb3a` | `0x0024EB3A` |
| `0x0024D917` | `8b f8` | `mov edi, eax` | `` |
| `0x0024D919` | `3b fe` | `cmp edi, esi` | `` |
| `0x0024D91B` | `75 0a` | `jne 0x24d927` | `` |
| `0x0024D91D` | `56` | `push esi` | `` |
| `0x0024D91E` | `6a 01` | `push 1` | `` |
| `0x0024D920` | `6a 01` | `push 1` | `` |
| `0x0024D922` | `e8 bb 33 00 00` | `call 0x250ce2` | `0x00250CE2` |
| `0x0024D927` | `57` | `push edi` | `` |
| `0x0024D928` | `e8 9f 0e 00 00` | `call 0x24e7cc` | `0x0024E7CC` |
| `0x0024D92D` | `5f` | `pop edi` | `` |
| `0x0024D92E` | `5e` | `pop esi` | `` |
| `0x0024D92F` | `c3` | `ret` | `` |
| `0x0024D930` | `55` | `push ebp` | `` |
| `0x0024D931` | `8b ec` | `mov ebp, esp` | `` |
| `0x0024D933` | `51` | `push ecx` | `` |
| `0x0024D934` | `51` | `push ecx` | `` |
| `0x0024D935` | `8b 45 08` | `mov eax, dword ptr [ebp + 8]` | `` |
| `0x0024D938` | `89 45 fc` | `mov dword ptr [ebp - 4], eax` | `` |
| `0x0024D93B` | `8d 48 01` | `lea ecx, [eax + 1]` | `` |
| `0x0024D93E` | `8a 10` | `mov dl, byte ptr [eax]` | `` |
| `0x0024D940` | `40` | `inc eax` | `` |
| `0x0024D941` | `84 d2` | `test dl, dl` | `` |
| `0x0024D943` | `75 f9` | `jne 0x24d93e` | `` |
| `0x0024D945` | `2b c1` | `sub eax, ecx` | `` |
| `0x0024D947` | `66 89 45 f8` | `mov word ptr [ebp - 8], ax` | `` |
| `0x0024D94B` | `8b 45 f8` | `mov eax, dword ptr [ebp - 8]` | `` |
| `0x0024D94E` | `40` | `inc eax` | `` |
| `0x0024D94F` | `66 89 45 fa` | `mov word ptr [ebp - 6], ax` | `` |
| `0x0024D953` | `8d 4d f8` | `lea ecx, [ebp - 8]` | `` |
| `0x0024D956` | `b8 01 00 00 00` | `mov eax, 1` | `` |
| `0x0024D95B` | `cd 2d` | `int 0x2d` | `` |
| `0x0024D95D` | `cc` | `int3` | `` |
| `0x0024D95E` | `c9` | `leave` | `` |
| `0x0024D95F` | `c2 04 00` | `ret 4` | `` |
| `0x0024D962` | `ff 74 24 0c` | `push dword ptr [esp + 0xc]` | `` |
| `0x0024D966` | `ff 74 24 0c` | `push dword ptr [esp + 0xc]` | `` |
| `0x0024D96A` | `ff 74 24 0c` | `push dword ptr [esp + 0xc]` | `` |
| `0x0024D96E` | `e8 33 53 00 00` | `call 0x252ca6` | `0x00252CA6` |
| `0x0024D973` | `0f b6 c0` | `movzx eax, al` | `` |
| `0x0024D976` | `c2 0c 00` | `ret 0xc` | `` |
| `0x0024D979` | `a1 54 13 36 00` | `mov eax, dword ptr [0x361354]` | `` |
| `0x0024D97E` | `c3` | `ret` | `` |
| `0x0024D97F` | `ff 74 24 04` | `push dword ptr [esp + 4]` | `` |
| `0x0024D983` | `6a 00` | `push 0` | `` |
| `0x0024D985` | `ff 35 54 13 36 00` | `push dword ptr [0x361354]` | `` |
| `0x0024D98B` | `e8 16 53 00 00` | `call 0x252ca6` | `0x00252CA6` |
| `0x0024D990` | `f6 d8` | `neg al` | `` |
| `0x0024D992` | `1b c0` | `sbb eax, eax` | `` |
| `0x0024D994` | `f7 d0` | `not eax` | `` |
| `0x0024D996` | `23 44 24 04` | `and eax, dword ptr [esp + 4]` | `` |
| `0x0024D99A` | `c2 04 00` | `ret 4` | `` |
| `0x0024D99D` | `8b 44 24 04` | `mov eax, dword ptr [esp + 4]` | `` |
| `0x0024D9A1` | `ff 74 24 08` | `push dword ptr [esp + 8]` | `` |
| `0x0024D9A5` | `c1 e8 03` | `shr eax, 3` | `` |
| `0x0024D9A8` | `83 e0 08` | `and eax, 8` | `` |
| `0x0024D9AB` | `50` | `push eax` | `` |
| `0x0024D9AC` | `ff 35 54 13 36 00` | `push dword ptr [0x361354]` | `` |
| `0x0024D9B2` | `e8 74 4b 00 00` | `call 0x25252b` | `0x0025252B` |
| `0x0024D9B7` | `c2 08 00` | `ret 8` | `` |
| `0x0024D9BA` | `55` | `push ebp` | `` |
| `0x0024D9BB` | `8b ec` | `mov ebp, esp` | `` |

## Leitura inicial

O entry point começa com carregamentos e cálculos de estado de runtime, chama rotinas internas e termina dentro da janela limitada. O prólogo `push ebp; mov ebp, esp` observado depois pertence à rotina seguinte, em `0x0024D930`. Os alvos são endereços do próprio `.text`; ainda não são nomes de funções sem análise de fluxo e de referências cruzadas.
