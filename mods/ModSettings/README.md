# ModSettings

*[English version](README.en.md) · [Todos os mods](../../README.md)*

Janela, dentro do jogo, para ajustar as opções dos mods sem editar o
`MelonPreferences.cfg`. `F6` abre e fecha.

- À esquerda, os mods instalados que têm opções; à direita, as opções do mod
  escolhido, com nome, descrição e o botão `Padrão`.
- Ligado/Desligado escolhe-se com um clique. Números e textos editam-se
  clicando no campo, digitando e apertando Enter (ou clicando fora); `Esc`
  durante a edição desfaz.
- Não há botão de salvar: cada mudança vale na hora e fica gravada. Um valor
  inválido fica em vermelho e não é gravado.
- Na partida, a janela abre a pausa do jogo por baixo e, ao fechar, a fecha
  junto. Se você já estava na pausa, ela continua aberta.
- Os textos seguem o idioma escolhido no jogo: português ou, nos demais
  idiomas, inglês.
- A rolagem da lista de opções é pela roda do mouse.

O menu encontra sozinho as opções que cada mod registra no MelonLoader;
nenhum mod depende dele, e mods de outros autores também aparecem. Os mods
sem opções, como o SkipIntro, não aparecem.

O mod não altera nenhum arquivo do jogo nem os saves.

## Compatibilidade

Testado por último em House Party 1.5.2.14289 com MelonLoader 0.7.3. Veja os
detalhes no [README principal](../../README.md#compatibilidade).

## Instalação

1. Instale o MelonLoader, conforme o [README principal](../../README.md#instalação).
2. Baixe o [`ModSettings.dll`](../../dist/1.0.0/ModSettings.dll) (na página do
   arquivo, botão de download) e coloque na pasta `Mods` da raiz do jogo.
3. Abra o jogo e aperte `F6`.

Para desinstalar, apague o `Mods\ModSettings.dll`. Os outros mods continuam
funcionando, configuráveis pelo `MelonPreferences.cfg`.

## Configuração

A tecla aparece na própria janela, em ModSettings. No
`UserData\MelonPreferences.cfg`:

```toml
[ModSettings]
OpenKey = "F6"
```

- `OpenKey`: tecla que abre e fecha a janela, pelo nome do `KeyCode` do
  Unity (por exemplo `F6`, `F4`, `K`). O jogo já usa `F10` para salvar e
  várias outras teclas; veja as livres em
  [teclas do jogo](../../docs/teclas-do-jogo.md).

## Limitações conhecidas

- Algumas opções só valem ao abrir o jogo, como "Nomes ligados ao abrir o
  jogo" do ItemHighlight; a descrição de cada uma indica isso.
- Opções de tipos que a janela não sabe editar aparecem só para leitura.
- O campo de texto é simples: digita e apaga no fim, sem cursor no meio nem
  colar.
