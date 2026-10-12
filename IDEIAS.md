# Ideias de mods

Lista de ideias para avaliar. Status: `ideia` → `avaliando` → `viável` →
`em desenvolvimento` → `concluída`, ou `descartada` (com o motivo).

Modelo:

```markdown
## Nome
- **Status:** ideia
- **O que faz:**
- **Pistas:** classes, métodos ou dados do jogo que podem servir
- **Dúvidas / o que investigar:**
```

---

## Destaque de coletáveis no cenário

- **Status:** concluída (`mods/ItemHighlight`, plano
  `planos/executados/p20-destaque-de-itens.md`)
- **O que faz:** `H` mostra o nome dos itens à vista, com cor por tipo de
  ação; `J` faz os coletáveis pulsarem em rosa, com níveis de força.
  Detalhes no README do mod.
- **Confirmado no jogo** (2026-10-11):
  - O jogo só preenche `InteractiveItem.Interactions` ao calcular as ações
    do item (no hover); o mod chama `HasInteractions(story)` antes de ler.
  - Sem linha de visão, a casa inteira aparecia através das paredes.
  - `Use With...` está em quase todo objeto fixo e não serve para
    classificar.
  - O brilho por emissão do HDRP funciona, mas a força percebida varia com o
    material (a cerveja e o papel precisam de ~2,5; itens escuros, de 0,3).
  - Halo 2D na tela foi testado e descartado. `GUI.DrawTexture`/`GUI.color`
    foram removidos do jogo ("Method unstripping failed").
- **Dúvidas / o que investigar:**
  - Contorno de verdade (como o The Witcher 3) exigiria shader próprio
    compilado no Unity 2022.3 com HDRP.
  - Raio curto que mostra itens atrás de paredes, ativável por tecla.

## Indicador de personagem com fala nova

- **Status:** concluída (`mods/NewTalkMarker`, plano
  `planos/executados/p10-marcadores-de-fala-nova.md`)
- **O que faz:** "!" sobre o personagem quando surge uma fala nova para ele:
  amarelo (não vista no menu) e verde (vista, mas não escolhida), com as
  marcas guardadas por save. Detalhes no README do mod.
- **Confirmado no jogo** (2026-10-11):
  - `Response.Selected` vem do save; ao carregar, o jogo recria os objetos
    `Response` e só restaura os valores em `OnSaveGameLoaded`, ~20 s depois de
    `Load`.
  - As falas do menu inicial ficam quase todas em `GlobalResponses`, além das
    do diálogo inicial (`CharacterStoryData.DialogueID`).
  - Custo: 708 respostas avaliadas em ~7 ms; um personagem por quadro não
    pesa.
  - Critérios que oscilam com o momento e são ignorados: proximidade, visão,
    pose, zona, `State` (dançando, sentado), `Value CurrentMoveTarget` e
    grupos `NotCurrentlyOccupied*`.
- **Dúvidas / o que investigar:**
  - Lista no canto da tela para personagens fora de vista.
  - Se obter um item marca vários personagens de uma vez e se isso incomoda.
- **Marca de tarefa pendente (azul), fora do plano p10:** mostrar que o
  personagem espera algo do jogador (por exemplo, a bebida da Katherine).
  - Testado e retirado (2026-10-11): marcar falas que "só dependem de item"
    pegou falas genéricas escondidas, como "Do you know what this means in
    Patrick's notebook?" (exige o caderno), em todo personagem conhecido.
  - A bebida da Katherine não tem critério de item no menu: a entrega é feita
    como item, fora do menu, e avança a quest `1477ed2c-...` (estado 1 =
    pedido feito). Hipótese: derivar o azul das quests em andamento ligadas ao
    personagem. Investigar como o jogo associa quest e personagem.

## Pular com o personagem

- **Status:** concluída (`mods/Jump`, plano
  `planos/executados/p60-pular-com-o-personagem.md`)
- **O que faz:** `Espaço` faz o personagem pular; o modo de combate passa
  para o `Alt`. Detalhes no README do mod.
- **Confirmado no jogo** (2026-10-12):
  - O jogo não tem pulo: no chão, prende o personagem ao piso; no ar
    (`IsFalling`), desce em velocidade constante. `AddForce` soma à posição
    a cada quadro, sem depender do tempo. O pulo leva a altura a uma
    parábola logo depois de `PlayerCharacter.TryMoveController`.
  - `CEvents.JumpUp` toca uma animação de pulo, sem mover o corpo; correndo,
    dá um tranco na câmera.
  - O `F8` grava os overrides ativos ao fechar; o mod retira a própria troca
    do que é gravado.
- **Dúvidas / o que investigar:**
  - Um indicador do modo de combate: `CCombat.InCombat` ficou `false` e
    `CombatActive` `true` fora dele. Pistas: `PlayerCombatMode` e
    `PlayerCharacter.OnCombatModeToggled`.

## Pular aberturas ao abrir o jogo

- **Status:** concluída (`mods/SkipIntro`, plano
  `planos/executados/p30-pular-aberturas.md`)
- **O que faz:** pula o vídeo da Eek! Games e o aviso e abre direto no menu
  principal. Detalhes no README do mod.
- **Confirmado no jogo** (2026-10-11):
  - As telas são as cenas `EekGamesIntro` e `Disclaimer`, controladas por
    `EekGamesIntroManager` e `DisclaimerManager`, que herdam de
    `TransitionalSceneManager`.
  - `UserInputForTransitionReceived` só verifica se houve tecla; respondendo
    `true`, o próprio jogo carrega a cena seguinte. O menu abre em ~1,5 s.
  - Não apareceu splash do Unity antes das cenas.

## Continuar sozinho após o primeiro carregamento

- **Status:** concluída (`mods/AutoContinue`, plano
  `planos/executados/p50-continuar-apos-carregamento.md`)
- **O que faz:** ao carregar um save ou iniciar novo jogo, segue sozinho da
  primeira tela de carregamento para a segunda, sem esperar o clique.
  Detalhes no README do mod.
- **Confirmado no jogo** (2026-10-12):
  - A tela é a cena `LoadingScreen`, controlada por `LoadingScreenManager`;
    só pergunta pelo clique (`WasButtonPressed`) depois de carregar
    (`_loaded`) e mostrar o aviso. Novo jogo passa pela mesma tela.
  - `WasButtonPressed` tem o mesmo código nativo que
    `TransitionalSceneManager.UserInputForTransitionReceived`, corrigido pelo
    SkipIntro. Patches pelos dois métodos derrubam o jogo; o mod corrige o da
    base e confere a classe real.

## Menu de configurações dos mods

- **Status:** concluída (`mods/ModSettings`, plano
  `planos/executados/p40-menu-de-configuracoes.md`)
- **O que faz:** `F6` abre uma janela com os mods instalados e suas opções,
  que valem e ficam gravadas na hora, em português ou inglês conforme o
  idioma do jogo. Detalhes no README do mod.
- **Confirmado no jogo** (2026-10-12):
  - O menu encontra sozinho as opções que os mods registram no
    MelonPreferences; nenhum mod depende dele.
  - Do IMGUI, sobraram `Window`, `Button`, `Toggle`, `Box` e `Label`;
    `TextField`, sliders, `ScrollView` e `GUILayout` foram removidos, e o
    campo de texto e a rolagem são feitos à mão.
  - Sem bloqueio, o jogo reage ao mouse e às teclas por baixo da janela. A
    janela abre a pausa do jogo (evento `PlayerControlManager.OnInput_Options`,
    o mesmo do `Esc`) e desliga as ações do Input System enquanto está aberta.
  - `TranslationManager.languageCurrent` vale `"English"` até o menu
    principal; `TranslateByText` traduz os nomes dos itens.
  - Teclas do jogo levantadas em `docs/teclas-do-jogo.md` (`F10` salva o
    jogo; por isso `F6`).
- **Dúvidas / o que investigar:**
  - Mostrar também os mods sem opções.

## Dublagens localizadas por mod

- **Status:** ideia
- **O que faz:** troca as vozes em inglês por dublagens em outros idiomas,
  gravadas ou geradas por IA, sem mexer no áudio original do jogo. Um mesmo
  mod traz várias dublagens, e o jogador escolhe o idioma.
- **Pistas:** o projeto `house-party-dubbing`
  (`C:\Users\gabriel\Documents\projetos\house-party-dubbing`) já faz a troca
  injetando os áudios nos bundles dos personagens.
- **Dúvidas / o que investigar:**
  - Se um mod consegue trocar os áudios na hora de tocar, sem alterar os
    bundles, o que facilitaria a distribuição.
