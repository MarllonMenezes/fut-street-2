# Continuação segura do Fut Street 2 no Codex Cloud

O Codex Cloud permite trabalhar quando o computador local está desligado, mas
uma tarefa recebe o repositório e o ambiente publicado — não os arquivos
ignorados que existem apenas neste computador. O repositório público não contém
a ISO, o XBE extraído, os assets originais nem o C gerado pelo recompilador.
Portanto, Cloud pode avançar infraestrutura e código-fonte independente; não
pode continuar a tradução deste XBE nem validar os gates do jogo sem esses
insumos. Não enviar esses arquivos para contornar a limitação.

## Preparar o ambiente

1. No Codex, crie uma tarefa em **Work in → Cloud → Create environment** e
   selecione `MarllonMenezes/fut-street-2`.
2. Deixe o ambiente privado (**Only me**) e não adicione secrets, variáveis ou
   credenciais: o projeto não precisa deles para build e self-test do código
   versionado.
3. Deixe o Codex inspecionar e testar a configuração. Revise os comandos e
   arquivos propostos; publique somente o setup do repositório, sem arquivos
   locais. Se o setup perguntar por ISO/XBE, não os forneça.
4. Inicie uma tarefa nova a partir do ambiente publicado. Cada tarefa parte do
   estado publicado e deve guardar os avanços importantes em commits Git.

## Prompt para a tarefa Cloud

> Continue o projeto de recompilação estática clean-room do Xbox original de
> FIFA Street 2, no repositório `MarllonMenezes/fut-street-2`. O objetivo é
> traduzir estaticamente o código do XBE para código nativo e implementar
> compatibilidade de APIs com evidência; não criar um remake e não usar
> emulação de CPU, GPU ou APU. Não execute XBE nem executável do jogo. Não
> invente bindings nem marque o projeto como concluído. Trabalhe somente com os
> arquivos versionados disponíveis nesta tarefa: não peça, baixe, gere ou
> publique ISO, XBE, assets extraídos, C/objetos/bibliotecas derivados do jogo,
> relatórios que revelem dados proprietários ou credenciais. Se o próximo passo
> depender de qualquer um desses insumos locais, pare nesse subpasso, documente
> a dependência e avance em trabalho independente que não dependa deles. Para
> cada mudança, adicione teste e documentação; execute build Release, self-test
> e smokes/replay apenas quando aplicáveis aos arquivos presentes. Declare
> explicitamente quais verificações de jogo não puderam ser executadas. Faça
> commits pequenos e verificáveis; não inicie code review.

## Limite de validação

No Cloud, build Release e self-tests do código versionado podem ser executados
sem a ISO. A matriz local que inspeciona conteúdo da ISO, os relatórios
derivados do XBE e a recompilação/linkagem do código gerado não são reproduzíveis
nesse ambiente publicado. Um resultado verde do harness .NET não é evidência
de que o jogo recompilado renderiza, joga ou reproduz áudio. Esses gates
continuam pendentes até validação local com os arquivos mantidos fora do GitHub.
