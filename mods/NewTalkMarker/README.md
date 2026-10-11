# NewTalkMarker

*[English version](README.en.md) · [Todos os mods](../../README.md)*

Mostra um **!** sobre a cabeça do personagem quando aparece uma fala nova
para ele, liberada pelo que você fez na partida. Por exemplo, depois de
conversar com a Katherine, a Stephanie ganha a fala "Katherine seems cool." e
fica marcada.

| Marca | Significado |
|---|---|
| **!** amarelo | fala nova que você ainda não viu no menu |
| **!** verde | fala nova que você viu no menu, mas não escolheu |
| **! !** amarelo e verde | o personagem tem as duas |

- Escolher a fala tira a marca dela.
- Ao começar uma partida, ou carregar um save sem registro do mod, o que já
  está disponível conta como conhecido: ninguém aparece marcado.
- O que a conversa com um personagem libera para ele mesmo e aparece no menu
  dessa conversa não marca. Se a conversa termina antes de mostrar a fala
  nova, o personagem fica amarelo.
- A marca fica escondida em pausa, menus e conversas, e só aparece até uma
  distância máxima.

O mod não altera nenhum arquivo do jogo nem os saves. As falas são
identificadas pelo Id, não pelo texto, então funciona com qualquer idioma.

## Marcas por save

As marcas são guardadas junto com cada save (manual, rápido e automático), num
arquivo `<nome do save>.newtalkmarker` na pasta de saves do jogo
(`%USERPROFILE%\AppData\LocalLow\Eek\House Party\Saves`). Ao carregar o save,
as marcas voltam a ser as do momento em que ele foi feito. Elas reaparecem
alguns segundos depois de o jogo terminar de carregar. Apagar o save no jogo
apaga o arquivo do mod junto.

## Compatibilidade

Testado por último em House Party 1.5.2.14289 com MelonLoader 0.7.3. Veja os
detalhes no [README principal](../../README.md#compatibilidade).

## Instalação

1. Instale o MelonLoader, conforme o [README principal](../../README.md#instalação).
2. Baixe o `NewTalkMarker.dll` na página de
   [Releases](../../../../releases?q=NewTalkMarker&expanded=true) e coloque
   na pasta `Mods` da raiz do jogo.
3. Abra o jogo e comece ou carregue uma partida.

Para desinstalar, apague o `Mods\NewTalkMarker.dll`. Os arquivos
`.newtalkmarker` da pasta de saves podem ser apagados.

## Configuração

Em `UserData\MelonPreferences.cfg`:

```toml
[NewTalkMarker]
MaxDistance = 25.0
DiagnosticLog = false
```

- `MaxDistance`: distância máxima, em metros, para mostrar o "!".
- `DiagnosticLog`: com `true`, o mod registra em `MelonLoader\Latest.log` cada
  mudança de marca, com a fala e os critérios dela, e o que grava e lê com os
  saves. Se encontrar uma marca errada, ligue essa opção, reproduza o caso e
  anexe o log ao abrir uma issue.

## Limitações conhecidas

- Só conta o menu inicial do personagem; falas que surgem no meio de uma
  conversa não marcam.
- Condições do momento (onde o personagem está, o que está fazendo, se está
  ocupado) são ignoradas, para a marca não piscar. Por isso, uma fala marcada
  pode só aparecer no menu em outro momento.
- Obter um item pode liberar uma fala genérica em vários personagens de uma
  vez, marcando todos.
- A restauração do salvamento automático não foi testada no jogo, e não foi
  verificado se o Steam Cloud sincroniza os arquivos `.newtalkmarker`. Sem
  eles, o save carrega sem marcas.
