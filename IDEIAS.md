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

- **Status:** viável. Formato escolhido: segurar uma tecla mostra o nome dos
  itens próximos (como Baldur's Gate 3 e Divinity), com cor por tipo de ação
- **O que faz:** destaca itens que o jogador pode pegar ou usar no cenário,
  como a maioria dos jogos faz (contorno, brilho ou ícone), para não ter que
  passar o mouse em tudo.
- **Pistas** (código descompilado, 2026-10-11):
  - ~~`ClickManager.Highlight()`~~: é de UI (botões, ponteiro), não de objetos
    do cenário. O jogo (HDRP) não tem sistema de contorno ou brilho próprio,
    então o visual terá de ser do mod. Caminho mais simples: ícone na tela
    sobre cada item (posição do mundo convertida para a tela), sem mexer em
    shaders.
  - `ItemManager.GetAllItems()`: lista de todos os `InteractiveItem`.
  - `InteractiveItem`: `HasInteractions(story)`, `Interactions`,
    `canBeGrabbed`, `_inventoried`, `IsRendererEnabled`, `DisplayName`,
    `InPlayerInteractionRange()`, `DistanceToInteraction`.
  - Evento estático `InteractiveItem.OnMouseOver(item)`: o hover do jogo.
  - História ativa: `GameManager.GetActiveStory()`.
- **Confirmado no jogo** (sonda, 2026-10-11):
  - 292 itens, 286 ativos. Os personagens também são `InteractiveItem`
    (ações `[Inspect, Talk]`).
  - `canBeGrabbed` não serve: vale `True` para sofá, fogão e personagens.
  - Coletável = item cuja lista `Interactions` contém `Take` (43 itens, por
    exemplo, Battery Pack). As outras ações (`Open`, `Close`, `Sit`,
    `Inspect`...) permitem cores por tipo.
- **Dúvidas / o que investigar:**
  - Destacar só os coletáveis ou também os interativos, com cores diferentes?
  - Destacar sempre, só ao segurar uma tecla, ou só num raio perto do jogador?
  - Itens escondidos de propósito (parte de quebra-cabeça) estragariam a
    graça? Talvez uma opção para não destacar itens de quest.
  - Custo: muitos objetos destacados ao mesmo tempo pesam no desempenho?

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
