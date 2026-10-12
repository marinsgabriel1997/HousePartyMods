# Jump

*[English version](README.en.md) · [Todos os mods](../../README.md)*

Faz o personagem pular com o `Espaço`. O modo de combate, que o jogo põe no
`Espaço`, passa para o `Alt`.

- O pulo tem cerca de 0,8 m e dura menos de um segundo. Dá para andar e
  correr no ar e pousar em móveis.
- Parado ou andando, o personagem faz a animação de pulo do jogo. Correndo,
  pula sem ela, porque a animação dá um tranco na câmera.
- Só pula do chão, um pulo de cada vez, e não pula em diálogos, menus,
  pausa e cenas.
- No mapeamento de controles do jogo (`F8`), o combate aparece no `Alt`, e o
  `Espaço` fica reservado para o pulo: o jogo não aceita pôr outra ação nele.
- A troca do combate não fica gravada nos controles do jogo. Sem o mod, o
  combate volta ao `Espaço`.
- Se você tiver posto o combate em outra tecla pelo `F8`, o mod não mexe
  nela.
- Se der erro, o mod registra uma vez no log e o jogo segue sem pular.

O mod não altera nenhum arquivo do jogo nem os saves.

## Compatibilidade

Testado por último em House Party 1.5.2.14289 com MelonLoader 0.7.3. Veja os
detalhes no [README principal](../../README.md#compatibilidade).

## Instalação

1. Instale o MelonLoader, conforme o [README principal](../../README.md#instalação).
2. Baixe o [`Jump.dll`](../../dist/1.0.0/Jump.dll) (na página do
   arquivo, botão de download) e coloque na pasta `Mods` da raiz do jogo.
3. Abra o jogo, comece ou carregue uma partida e aperte `Espaço`.

Para desinstalar, apague o `Mods\Jump.dll`. O combate volta ao `Espaço`.

## Configuração

Pela janela do [ModSettings](../ModSettings/README.md) (`F6`), com os nomes
das opções no idioma do jogo, ou em `UserData\MelonPreferences.cfg`:

```toml
[Jump]
Key = "Space"
Height = 0.8
Animation = true
CombatOnAlt = true
```

- `Key`: tecla do pulo, pelo nome do `KeyCode` do Unity (por exemplo
  `Space`, `V`, `G`). O jogo já usa várias teclas; G, K, L, N, V, X e Z
  estão livres nos controles padrão.
- `Height`: altura do pulo em metros, de 0,1 a 3.
- `Animation`: com `false`, o personagem pula sem a animação também parado
  e andando.
- `CombatOnAlt`: com `false`, o combate fica no `Espaço`, e o `Espaço` pula
  e liga o combate ao mesmo tempo. A troca só acontece com o pulo no
  `Espaço`.

## Limitações conhecidas

- Também pula em modo de combate.
- Pular permite alcançar lugares que o jogo não previa, como o alto de
  móveis. Gatilhos da história ligados a uma área podem disparar ao chegar
  nela pulando, como se você tivesse andado até lá.
- Se uma versão nova do jogo mudar a forma de gravar os controles, o `Alt`
  pode ficar gravado. Nesse caso, restaure o padrão do combate no `F8`.
