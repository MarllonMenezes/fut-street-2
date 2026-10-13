# Decoder RefPack do runtime PC

Os blocos `VivF` da imagem começam com `10 FB` e carregam o tamanho expandido
em 24 bits big-endian. O runtime agora decodifica essa variante RefPack com um
buffer limitado ao tamanho declarado pelo bloco.

O decoder implementa os comandos LZ de literais e referências curtas, médias e
longas, além do comando de parada. Ele rejeita:

- cabeçalho ou assinatura incompatível;
- tamanho declarado diferente do registro `VivF`;
- comando que ultrapasse o bloco comprimido;
- back-reference fora do histórico produzido;
- saída maior ou menor que o tamanho expandido declarado;
- blocos acima do limite de segurança de 256 MiB.

Validação na imagem local:

```text
fe.bh: 163/163 blocos, 59.537.466 bytes expandidos
db.bh: 29/29 blocos, 193.652 bytes expandidos
ig.bh: 121/121 blocos, 34.618.985 bytes expandidos
```

O decoder ainda retorna bytes descompactados; a interpretação desses bytes como
estruturas de animação, banco de dados ou modelo permanece em módulos separados.

Referência pública de implementação do formato: [RefPack decoder](https://github.com/gbataglini/sims4-workspace/blob/main/util/datamining/refpack.py).
