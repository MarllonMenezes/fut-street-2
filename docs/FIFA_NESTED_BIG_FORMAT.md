# BIGs internos dos pacotes de animação

Depois da expansão RefPack, os blocos de `data/aptanims/fe.bh` e
`data/aptanims/ig.bh` contêm arquivos `BIGF` completos. O leitor BIG do
runtime foi ampliado para inspecionar buffers descompactados sem gravá-los em
disco.

Validação na imagem local:

```text
fe.bh: 163 blocos BIGF, 632 entradas internas
ig.bh: 121 blocos BIGF, 474 entradas internas
```

Exemplo do primeiro bloco de `fe.bh`:

```text
magic: BIGF
archive_size: 7808
entries: 4
index_end: 100
ArrBLeft.apt   offset=128  size=312
ArrBLeft.const offset=512  size=32
ArrBLeft.o     offset=640  size=2774
ArrBLeft.xsh   offset=3456 size=4352
```

O objeto ELF/MIPS tem seções `.data`, `.rel.data`, `.symtab` e tabelas de
strings. Os símbolos `__Model`, `__BBOX`, `__VB` e `__RM`, junto das
relocações, apontam para estruturas de modelo e renderização EAGL; o `.apt`
também precisa ser decodificado para fechar as referências e animações. O
SHPX correspondente é imagem: seu registro `0x61`
contém DXT3 64x64, conversível para TGA alfa com:

```text
dotnet run --project fifa_pc -- vivf-nested-shpx-export "D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso" data/aptanims/fe.bh 0 ArrBLeft.xsh "D:\projeto zeta\exports\ArrBLeft"
```

Esse comando gerou `000-1.tga` em `exports_fifa_layer2/shpx_ArrBLeft`.

O comando `apt-structure-inventory` também compara, sem escrever os blocos,
o header APT validado, o header `.const` validado quando presente, a assinatura
estrutural do `.o` EAGL, as extensões same-stem, a tabela SHPX validada do `.xsh`
e os tamanhos dos companions. Na imagem de trabalho foram observados 133
perfis estruturais em `fe.bh` e 97 em `ig.bh`; 161/161 e 118/118 objetos `.o` e
145/145 e 114/114 tabelas SHPX dos conjuntos correspondentes foram lidos, sem
rejeições no inventário. Essa correlação documenta a organização do BIG interno;
não decodifica
timing, poses, morphs, materiais ou frames.

Os pacotes `db.bh` não foram classificados como BIG interno: seus blocos
expandidos têm outra estrutura e serão encaminhados a um parser separado.
