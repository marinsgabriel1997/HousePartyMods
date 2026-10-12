# Ideias de mods

Lista de ideias para avaliar. Status: `ideia` → `avaliando` → `viável` →
`em desenvolvimento` → `publicado`, ou `descartada` (com o motivo).

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

- **Status:** em desenvolvimento (`mods/ItemHighlight`, plano
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

- **Status:** em desenvolvimento (`mods/NewTalkMarker`, plano
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

- **Status:** ideia
- **O que faz:** uma tecla faz o personagem do jogador pular.
- **Pistas:** o controle de movimento do jogador (`Il2CppEekCharacterEngine`).
- **Dúvidas / o que investigar:**
  - Se o controle do jogador aceita movimento vertical ou se prende o
    personagem ao chão (NavMesh).
  - Se há animação de pulo que possa ser reaproveitada.
  - Qual tecla está livre (`Espaço` precisa ser conferido).

## Pular aberturas ao abrir o jogo

- **Status:** em desenvolvimento (`mods/SkipIntro`, plano
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

- **Status:** ideia
- **O que faz:** ao carregar um save, o jogo para ao fim da primeira tela de
  carregamento e espera um clique antes da segunda. O mod daria esse clique
  sozinho.
- **Dúvidas / o que investigar:**
  - Qual componente espera o clique e como o jogo recebe a confirmação.
  - Se a pausa tem função (dicas na tela, carregamento ainda em andamento).
  - Novo jogo também carrega no mesmo modelo de duas telas com click.

## Menu de configurações dos mods

- **Status:** em desenvolvimento (`mods/ModSettings`, plano
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
  - Um botão "Mods" no menu do jogo.
  - Mostrar também os mods sem opções.
