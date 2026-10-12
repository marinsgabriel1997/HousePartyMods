# ItemHighlight

*[English version](README.en.md) · [Todos os mods](../../README.md)*

Destaca os itens com que você pode interagir, para não ter de passar o mouse
em tudo. São dois destaques independentes, cada um com sua tecla:

- **Nomes (`H`)**: mostra o nome de cada item à vista, logo acima dele, com
  a cor do tipo de ação. `H` de novo esconde.
- **Brilho (`J`)**: faz os itens que podem ser pegos pulsarem em rosa. Cada
  toque passa ao próximo nível de força (0,3, 0,6, 1,2, 2,5 e 5); depois do
  último, desliga.

| Cor do nome | Tipo de item | Aparece por padrão |
|---|---|---|
| dourado | pode ser pego | sim |
| ciano | outra ação, como beber, ligar ou vasculhar | sim |
| verde-claro | abre ou fecha (portas, gavetas, armários) | não |
| cinza-claro | sentar, deitar ou subir | não |
| branco | só examinar | não |

- Só aparecem os itens à vista: o que está atrás de paredes ou de outros
  objetos fica de fora. Personagens e itens no inventário não são
  destacados.
- Por padrão, só os itens que podem ser pegos brilham. Vaso, fogão e lixeira
  mostram o nome, mas não pulsam.
- Os destaques somem em pausa, menus e conversas e voltam depois. As teclas
  não funcionam nesses momentos, para não alternar ao digitar o nome de um
  save.
- Ao desligar o brilho, os itens voltam à aparência original.
- Os nomes seguem o idioma escolhido no jogo, com as traduções do próprio
  jogo; item sem tradução aparece com o nome em inglês.

O mod não altera nenhum arquivo do jogo nem os saves.

## Compatibilidade

Testado por último em House Party 1.5.2.14289 com MelonLoader 0.7.3. Veja os
detalhes no [README principal](../../README.md#compatibilidade).

## Instalação

1. Instale o MelonLoader, conforme o [README principal](../../README.md#instalação).
2. Baixe o `ItemHighlight.dll` na página de
   [Releases](../../../../releases?q=ItemHighlight&expanded=true) e coloque
   na pasta `Mods` da raiz do jogo.
3. Abra o jogo, comece ou carregue uma partida e aperte `H` ou `J`.

Para desinstalar, apague o `Mods\ItemHighlight.dll`.

## Configuração

Pela janela do [ModSettings](../ModSettings/README.md) (`F6`), com os nomes
das opções no idioma do jogo, ou em `UserData\MelonPreferences.cfg`:

```toml
[ItemHighlight]
LabelsKey = "H"
GlowKey = "J"
StartLabels = false
StartGlow = false
GlowUse = false
ShowCollectible = true
ShowUse = true
ShowOpenClose = false
ShowPosition = false
ShowInspect = false
```

- `LabelsKey` e `GlowKey`: teclas dos nomes e do brilho, pelo nome do
  `KeyCode` do Unity (por exemplo `H`, `G`, `F6`). O jogo já usa várias
  teclas; G, H, J, K, L, N, V, X e Z estão livres nos controles padrão.
- `StartLabels` e `StartGlow`: se os nomes e o brilho começam ligados ao
  abrir o jogo. O brilho começa no primeiro nível.
- `GlowUse`: com `true`, os itens de outras ações (nome ciano) também
  brilham.
- `ShowCollectible`, `ShowUse`, `ShowOpenClose`, `ShowPosition` e
  `ShowInspect`: quais tipos de item são destacados, nos nomes e no brilho.

## Limitações conhecidas

- A força do brilho percebida varia com o material: itens escuros e foscos
  aparecem já no primeiro nível, mas itens claros ou brilhantes, como a
  garrafa de cerveja e folhas de papel, só aparecem bem a partir do 2,5.
  Alguns materiais não brilham.
- Itens pequenos quase não aparecem com o brilho; para eles, os nomes
  ajudam mais.
- Não há limite de distância. Itens vistos de longe por janelas ou portas
  abertas também aparecem, e nomes próximos podem se sobrepor.
- Um item encostado em outro objeto pode ficar sem destaque, e itens atrás
  de vidros podem aparecer.
- Itens escondidos de propósito, como os de quebra-cabeças, também são
  destacados. Use as teclas quando quiser.
