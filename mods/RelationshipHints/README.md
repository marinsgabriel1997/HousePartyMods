# RelationshipHints

*[English version](README.en.md) · [Todos os mods](../../README.md)*

Mostra, antes de cada opção do menu de respostas, o efeito que ela terá na sua
relação com o personagem:

```
+1A -1R | I like your glasses. They make you look smart!
```

- **A** = amizade, **R** = romance. Com o jogo em outro idioma, a amizade
  aparece como **F** (*friendship*).
- Valores positivos aparecem em **verde**, negativos em **vermelho**.
- Resposta sem efeito fica sem prefixo.

O efeito é calculado **durante o jogo**, com o estado atual. Assim, respostas
condicionais mostram o resultado certo para o momento. Exemplo real, com a
Katherine: "So how about letting me get you that drink now?" mostra `-1A -1R`
antes de você ver um certo diálogo dela e `+1A +1R` depois.

O mod não altera nenhum arquivo do jogo, nem `.character` nem os arquivos de
idioma. Ele só muda o texto exibido no menu, então funciona com qualquer
idioma, inclusive com traduções da comunidade.

## Marcadores

| Marcador | Significado |
|---|---|
| `+1A`, `-2R` | soma ou subtrai pontos de amizade ou romance |
| `?A`, `?R` | há efeito, mas não dá para prever com segurança (por exemplo, depende de sorteio) |
| `=5A` | define a relação com um valor fixo, em vez de somar |
| `Madison: -3A` | efeito na relação com outro personagem, não com quem está falando |

## Compatibilidade

Testado por último em House Party 1.5.2.14289 com MelonLoader 0.7.3. Veja os
detalhes no [README principal](../../README.md#compatibilidade).

## Instalação

1. Instale o MelonLoader, conforme o [README principal](../../README.md#instalação).
2. Baixe o `RelationshipHints.dll` na página de
   [Releases](../../../../releases?q=RelationshipHints&expanded=true) e
   coloque na pasta `Mods` da raiz do jogo.
3. Abra o jogo e converse com alguém.

Para desinstalar, apague o `Mods\RelationshipHints.dll`.

## Configuração

Pela janela do [ModSettings](../ModSettings/README.md) (`F6`), com os nomes
das opções no idioma do jogo, ou em `UserData\MelonPreferences.cfg`:

```toml
[RelationshipHints]
DiagnosticLog = false
```

Com `DiagnosticLog = true`, o mod registra em `MelonLoader\Latest.log`, a cada
menu, o efeito previsto de cada resposta e o caminho que levou a ele. A cada
escolha, compara a previsão com a mudança real de relação aplicada pelo jogo e
marca `OK`, `DIVERGE` ou `INCERTO`. Se encontrar um marcador errado, ligue essa
opção, reproduza o caso e anexe o log ao abrir uma issue.

## Como o efeito é calculado

Para cada resposta exibida, o mod percorre os mesmos dados que o jogo usa:

1. os eventos da própria resposta (`ResponseEvents`);
2. os desvios de diálogo (evento 120) que ela dispara, avaliando as condições
   com o avaliador de critérios do próprio jogo. Quando o desvio vai para o
   próprio personagem, ele substitui o `Next`, como no jogo;
3. o diálogo seguinte (`Next`): eventos de abertura (`StartEvents`) e de
   fechamento (`CloseEvents`).

No diálogo 0, a conversa "ociosa" do personagem, só contam os eventos de
relação. Os desvios dele escolhem o que o NPC diz, independentemente da sua
escolha.

O efeito vira `?` quando há critério aleatório (`CoinFlip`) ou quando a
condição depende de algo que a própria resposta muda antes de ser avaliada.

### Limitações conhecidas

- Os caminhos de `CloseEvents` e de efeito em outros personagens ainda foram
  pouco testados no jogo.
- Traduções que já trazem marcadores de relação embutidos no texto vão
  aparecer com o marcador duplicado.
