# Runtime de assets do FIFA Street 2 PC

O projeto `fifa_pc` agora possui uma camada de acesso a assets que:

- abre arquivos virtuais do XDVDFS diretamente da imagem ISO;
- inspeciona arquivos BIG4/BIGF e localiza uma entrada pelo nome;
- expõe cada entrada BIG como um stream limitado ao offset e tamanho dela;
- mantém a imagem original somente para leitura;
- não extrai arquivos para uma pasta temporária.

Exemplo validado com a cópia local:

```text
FIFA Street 2 PC asset smoke
archive: data/anims.big
entry: banklist.axt
size: 47
first_bytes: 4445465F42414E4B2044454642414E4B
mode: ISO read-only, bounded BIG entry stream
```

Isso é código novo gerenciado para PC, não execução do XBE e não emulação do
Xbox. Ainda faltam os decodificadores específicos de animação, textura, modelo,
áudio e os sistemas de renderização, input e gameplay necessários para chegar
ao jogo completo.
