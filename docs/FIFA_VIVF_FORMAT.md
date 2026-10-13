# Parser estrutural `VivF`

O runtime PC reconhece os pacotes `.bh` com assinatura `VivF` diretamente no
ISO, em modo somente leitura.

## Estrutura validada

- bytes `0..3`: assinatura ASCII `VivF`;
- bytes `4..7`: tamanho declarado em little-endian;
- bytes `8..11`: quantidade de registros em big-endian;
- bytes `12..15`: fim da tabela em big-endian;
- cada registro ocupa 16 bytes e contém quatro campos big-endian;
- a tabela possui `EntryCount + 1` registros, sendo o último o sentinela;
- os offsets dos registros são monotônicos.

Para cada `.bh`, o runtime também localiza o contêiner companheiro com o mesmo
nome-base (`.big` ou `.viv`) e verifica que:

- `Offset + CompressedSize` de cada registro cabe no companheiro;
- o offset do registro sentinela coincide com o tamanho do companheiro.

O parser valida limites e consistência da tabela, mas ainda não descomprime nem
interpreta o payload referenciado. Isso evita tratar dados comprimidos como se
fossem animações decodificadas antes de confirmar o codec.

Validação na imagem local:

```text
pacotes VivF: 15
índices válidos: 15
pares `.bh`/`.big` ou `.viv` com limites válidos: 15
fe.bh: 163 registros
ig.bh: 121 registros
capitch.bh: 350 registros

O runtime expõe cada registro de dados como um stream limitado, por exemplo:

```text
fe.bh bloco 0: offset 4480, comprimido 3098, expandido 7808
fe.bh bloco 162: offset 20699648, comprimido 2269, expandido 4736
```

Essa janela é a entrada do futuro decodificador; nenhum byte fora do bloco
validado pode ser lido por essa API.
```
