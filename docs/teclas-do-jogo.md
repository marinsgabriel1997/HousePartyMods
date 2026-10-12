# Teclas do jogo

Referência para escolher as teclas dos mods sem conflitar com o jogo.
Levantamento de 2026-10-11, House Party 1.5.2.14289, feito com o Probe
(plano `p40`).

## Como o jogo lê as teclas

- Quase tudo passa pelo Input System, no conjunto de ações `PlayerInputMap`,
  com os mapas `Player`, `UI` e `System`. As listas abaixo vêm desse conjunto
  carregado em memória durante a partida, não do código.
- Algumas leituras são diretas, fora das ações (registradas pelo Probe):
  - na abertura: qualquer tecla e os botões do mouse;
  - no menu principal: `Enter`;
  - na partida: `W`, `A`, `S`, `D`, `Shift` esquerdo, `Ctrl` esquerdo e os
    botões do mouse.
- O jogador pode remapear as teclas pelo mapeamento de controles (`F8`). As
  tabelas mostram as teclas padrão.
- Muitas teclas não aparecem na tela de controles do jogo (console, depuração,
  salvar e carregar, olhar pelo teclado, esconder a interface).

## Teclado e mouse

`Confirmado` indica efeito verificado no jogo. Nas demais, só se sabe que a
tecla está ligada à ação; o efeito vem do nome da ação.

| Tecla | Ação (mapa/nome) | Efeito |
| --- | --- | --- |
| `W` `A` `S` `D` | `Player/Move`, `UI/Navigate` | andar; navegar na interface |
| Setas | `UI/Navigate`, `UI/ConsoleArrowUp`/`Down` | navegar; histórico do console |
| `Page Up` / `Page Down` | `UI/ConsoleArrowUp`/`Down` | histórico do console |
| `R` `F` `T` `Y` | `Player/Look` | olhar para cima, baixo, esquerda e direita |
| Mouse (movimento) | `Player/Look` | olhar |
| Botão esquerdo | `Player/Interact`, `UI/Click`, `UI/NarratorContinue` | interagir; clicar; avançar o narrador |
| Botão direito | `Player/RightHand`, `UI/Right Click` | mão direita |
| Botão do meio | `UI/Middle Click` | — |
| Roda do mouse | `UI/ScrollWheel` | rolar |
| `Shift` esquerdo | `Player/Run` | correr |
| `Ctrl` esquerdo | leitura direta | — |
| `Espaço` | `Player/CombatMode` | modo de combate (com o Jump, vai para o `Alt`) |
| `C` | `Player/Crouch` | agachar |
| `E` | `UI/PlayerActions` | ações do jogador |
| `Q` | `UI/UIRadial` | menu radial |
| `I` | `UI/Inventory` | inventário |
| `M` | `UI/Memories` | memórias |
| `O` | `UI/Opportunities` | oportunidades |
| `P` | `Player/Penis` | — |
| `B` | `Player/FlashBreasts` | — |
| `1` | `Player/Masturbate` | — |
| `2` | `Player/Pee` | — |
| `Enter` / `Enter` numérico | `Player/Enter`, `UI/Submit` | confirmar |
| `Esc` | `UI/Cancel`, `UI/Options`, `UI/NarratorContinue` | cancelar; opções (pausa) |
| `Tab` | `UI/HideInterface` | esconder a interface |
| `Shift`+`Ctrl`+`U` | `System/HideAllUI` | esconder toda a interface |
| `'` (`backquote`) | `UI/CommandConsole` | console de comandos |
| `F5` | `UI/DebugLogger`, `UI/DebugLog` | registro de depuração |
| `F8` | `UI/ControlMapper`, `UI/InputConfiguration` | mapeamento de controles (confirmado) |
| `F9` | `UI/QuickSave` | salvamento rápido (confirmado) |
| `F10` | `UI/SaveGame` | salvar o jogo |
| `F11` | `UI/LoadGame` | carregar o jogo |

## Controle

O jogo também tem bindings para controle (`Gamepad`), joystick, toque e XR:
analógicos para andar e olhar, `A`/`South` interage e confirma, `B`/`East`
cancela, `X`/`West` modo de combate, `Y`/`North` urinar, gatilhos, ombros
(ações e menu radial), `Start` (opções) e `Select` (salvamento rápido). Os
mods atuais não usam o controle.

## Teclas dos mods

| Tecla | Mod | Função |
| --- | --- | --- |
| `H` | ItemHighlight | mostra e esconde os nomes dos itens |
| `J` | ItemHighlight | passa pelos níveis do brilho |
| `Espaço` | Jump | pula; o modo de combate do jogo passa para o `Alt` |
| `F6` | ModSettings | abre e fecha a janela de configurações |
| `F7` / `Shift`+`F7` | Probe (desenvolvimento) | relatório; janela de teste |

## Livres

Sem uso no jogo nem nos mods, pelo levantamento: `F1` a `F4`, `F12`,
as letras `G`, `K`, `L`, `N`, `U` (sozinha), `V`, `X`, `Z` e os números `3` a
`0`.

- `F12` é a captura de tela padrão do Steam.
- `Shift`+`Tab` abre o overlay do Steam.
- Ao escolher uma tecla, considerar que o jogador pode ter remapeado as do
  jogo.
- O mapeamento de controles (`F8`) grava os remapeamentos ao fechar o jogo,
  inclusive overrides aplicados por mods (`SaveBindingOverridesAsJson`), e
  recusa teclas já usadas por `RebindActionUI.IsDuplicate`. "Restaurar
  padrão" pode gravar um override para a própria tecla padrão. Detalhes no
  plano `p60`.
