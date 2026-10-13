# FIFA Street 2 — primeiros alvos internos do startup

Rotinas alcançadas por chamadas relativas observadas na janela do entry point. Os limites foram interrompidos no primeiro `ret` identificado e continuam sendo uma classificação estática.

## Alvo `0x0024EB3A`

Offset no XBE: `0x0023EB3A`; seção `.text`.

A chamada indireta em `0x0024EB73` usa a entrada `0x002A9134` da tabela de
thunks, que corresponde ao ordinal `255` de `XBOXKRNL`,
`PsCreateSystemThreadEx`.

| Endereço | Bytes | Instrução | Alvo |
|---:|---|---|---:|
| `0x0024EB3A` | `55` | `push ebp` | `` |
| `0x0024EB3B` | `8b ec` | `mov ebp, esp` | `` |
| `0x0024EB3D` | `8b 45 0c` | `mov eax, dword ptr [ebp + 0xc]` | `` |
| `0x0024EB40` | `85 c0` | `test eax, eax` | `` |
| `0x0024EB42` | `75 05` | `jne 0x24eb49` | `` |
| `0x0024EB44` | `a1 30 01 01 00` | `mov eax, dword ptr [0x10130]` | `` |
| `0x0024EB49` | `8b 4d 18` | `mov ecx, dword ptr [ebp + 0x18]` | `` |
| `0x0024EB4C` | `68 a2 ea 24 00` | `push 0x24eaa2` | `` |
| `0x0024EB51` | `6a 00` | `push 0` | `` |
| `0x0024EB53` | `c1 e9 02` | `shr ecx, 2` | `` |
| `0x0024EB56` | `81 e1 01 ff ff ff` | `and ecx, 0xffffff01` | `` |
| `0x0024EB5C` | `51` | `push ecx` | `` |
| `0x0024EB5D` | `ff 75 14` | `push dword ptr [ebp + 0x14]` | `` |
| `0x0024EB60` | `ff 75 10` | `push dword ptr [ebp + 0x10]` | `` |
| `0x0024EB63` | `ff 75 1c` | `push dword ptr [ebp + 0x1c]` | `` |
| `0x0024EB66` | `ff 35 50 13 36 00` | `push dword ptr [0x361350]` | `` |
| `0x0024EB6C` | `50` | `push eax` | `` |
| `0x0024EB6D` | `6a 00` | `push 0` | `` |
| `0x0024EB6F` | `8d 45 0c` | `lea eax, [ebp + 0xc]` | `` |
| `0x0024EB72` | `50` | `push eax` | `` |
| `0x0024EB73` | `ff 15 34 91 2a 00` | `call dword ptr [0x2a9134]` | `` |
| `0x0024EB79` | `8b c8` | `mov ecx, eax` | `` |
| `0x0024EB7B` | `85 c9` | `test ecx, ecx` | `` |
| `0x0024EB7D` | `7d 22` | `jge 0x24eba1` | `` |
| `0x0024EB7F` | `64 0f b6 05 24 00 00 00` | `movzx eax, byte ptr fs:[0x24]` | `` |
| `0x0024EB87` | `3c 02` | `cmp al, 2` | `` |
| `0x0024EB89` | `73 16` | `jae 0x24eba1` | `` |
| `0x0024EB8B` | `64 a1 28 00 00 00` | `mov eax, dword ptr fs:[0x28]` | `` |
| `0x0024EB91` | `83 78 28 00` | `cmp dword ptr [eax + 0x28], 0` | `` |
| `0x0024EB95` | `74 0a` | `je 0x24eba1` | `` |
| `0x0024EB97` | `51` | `push ecx` | `` |
| `0x0024EB98` | `e8 0a e1 ff ff` | `call 0x24cca7` | `0x0024CCA7` |
| `0x0024EB9D` | `33 c0` | `xor eax, eax` | `` |
| `0x0024EB9F` | `eb 03` | `jmp 0x24eba4` | `0x0024EBA4` |
| `0x0024EBA1` | `8b 45 0c` | `mov eax, dword ptr [ebp + 0xc]` | `` |
| `0x0024EBA4` | `5d` | `pop ebp` | `` |
| `0x0024EBA5` | `c2 18 00` | `ret 0x18` | `` |

---

## Alvo `0x00250CE2`

Offset no XBE: `0x00240CE2`; seção `.text`.

| Endereço | Bytes | Instrução | Alvo |
|---:|---|---|---:|
| `0x00250CE2` | `55` | `push ebp` | `` |
| `0x00250CE3` | `8b ec` | `mov ebp, esp` | `` |
| `0x00250CE5` | `81 ec 00 0c 00 00` | `sub esp, 0xc00` | `` |
| `0x00250CEB` | `a1 18 01 01 00` | `mov eax, dword ptr [0x10118]` | `` |
| `0x00250CF0` | `81 78 08 00 00 fe ff` | `cmp dword ptr [eax + 8], 0xfffe0000` | `` |
| `0x00250CF7` | `74 45` | `je 0x250d3e` | `` |
| `0x00250CF9` | `57` | `push edi` | `` |
| `0x00250CFA` | `33 c0` | `xor eax, eax` | `` |
| `0x00250CFC` | `b9 00 03 00 00` | `mov ecx, 0x300` | `` |
| `0x00250D01` | `8d bd 00 f4 ff ff` | `lea edi, [ebp - 0xc00]` | `` |
| `0x00250D07` | `f3 ab` | `rep stosd dword ptr es:[edi], eax` | `` |
| `0x00250D09` | `8b 45 08` | `mov eax, dword ptr [ebp + 8]` | `` |
| `0x00250D0C` | `83 a5 04 f4 ff ff 00` | `and dword ptr [ebp - 0xbfc], 0` | `` |
| `0x00250D13` | `89 85 00 f4 ff ff` | `mov dword ptr [ebp - 0xc00], eax` | `` |
| `0x00250D19` | `8b 45 0c` | `mov eax, dword ptr [ebp + 0xc]` | `` |
| `0x00250D1C` | `89 85 08 f4 ff ff` | `mov dword ptr [ebp - 0xbf8], eax` | `` |
| `0x00250D22` | `8b 45 10` | `mov eax, dword ptr [ebp + 0x10]` | `` |
| `0x00250D25` | `89 85 0c f4 ff ff` | `mov dword ptr [ebp - 0xbf4], eax` | `` |
| `0x00250D2B` | `8d 85 00 f4 ff ff` | `lea eax, [ebp - 0xc00]` | `` |
| `0x00250D31` | `50` | `push eax` | `` |
| `0x00250D32` | `6a 00` | `push 0` | `` |
| `0x00250D34` | `e8 3a c6 ff ff` | `call 0x24d373` | `0x0024D373` |
| `0x00250D39` | `5f` | `pop edi` | `` |
| `0x00250D3A` | `c9` | `leave` | `` |
| `0x00250D3B` | `c2 0c 00` | `ret 0xc` | `` |

---

## Alvo `0x0024E7CC`

Offset no XBE: `0x0023E7CC`; seção `.text`.

A chamada indireta em `0x0024E7D0` usa `0x002A9078`, a entrada de ordinal `187`
de `XBOXKRNL`, `NtClose`. A rotina transforma o resultado em sucesso booleano
ou chama a rotina de erro `0x0024CCA7`.

| Endereço | Bytes | Instrução | Alvo |
|---:|---|---|---:|
| `0x0024E7CC` | `ff 74 24 04` | `push dword ptr [esp + 4]` | `` |
| `0x0024E7D0` | `ff 15 78 90 2a 00` | `call dword ptr [0x2a9078]` | `` |
| `0x0024E7D6` | `85 c0` | `test eax, eax` | `` |
| `0x0024E7D8` | `7c 05` | `jl 0x24e7df` | `` |
| `0x0024E7DA` | `33 c0` | `xor eax, eax` | `` |
| `0x0024E7DC` | `40` | `inc eax` | `` |
| `0x0024E7DD` | `eb 08` | `jmp 0x24e7e7` | `0x0024E7E7` |
| `0x0024E7DF` | `50` | `push eax` | `` |
| `0x0024E7E0` | `e8 c2 e4 ff ff` | `call 0x24cca7` | `0x0024CCA7` |
| `0x0024E7E5` | `33 c0` | `xor eax, eax` | `` |
| `0x0024E7E7` | `c2 04 00` | `ret 4` | `` |

---
