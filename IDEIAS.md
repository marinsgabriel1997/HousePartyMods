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

- **Status:** ideia
- **O que faz:** destaca itens que o jogador pode pegar ou usar no cenário,
  como a maioria dos jogos faz (contorno, brilho ou ícone), para não ter que
  passar o mouse em tudo.
- **Pistas** (primeira olhada no código descompilado):
  - `ClickManager.Highlight()` (virtual) e `CharacterClickManager.Highlight()`:
    o jogo já tem um destaque de objeto clicável, provavelmente o do mouse em
    cima. Se der para ligá-lo sem o mouse, o visual fica igual ao do jogo.
  - `InteractiveItem`, `InteractiveItemBehavior`, `ItemManager`, `CInventory`,
    `InventoryObject`: candidatos para listar o que existe na cena e o que
    pode ir para o inventário.
- **Dúvidas / o que investigar:**
  - Como distinguir "coletável" (vai para o inventário) de "interativo" (porta,
    som, luz)? Destacar só um tipo ou os dois com cores diferentes?
  - Destacar sempre, só ao segurar uma tecla, ou só num raio perto do jogador?
  - Itens escondidos de propósito (parte de quebra-cabeça) estragariam a
    graça? Talvez uma opção para não destacar itens de quest.
  - Custo: muitos objetos destacados ao mesmo tempo pesam no desempenho?

## Indicador de personagem com fala nova

- **Status:** ideia
- **O que faz:** mostra quais personagens liberaram fala nova desde a última
  interação. Opções de visual, em ordem de preferência:
  1. balão em cima do personagem;
  2. lista num canto da tela com os personagens que têm novidade;
  3. um menu que abre com uma tecla.
- **Pistas:**
  - `ThoughtBubble` e `ThoughtBubbleManager.Show(actionId, text)`: o jogo já
    tem balões; talvez dê para reaproveitar o visual.
  - Do RelationshipHints: `CharacterManager.GetNonPlayerCharacter(nome).Dialogue`
    dá o `CDialogue` do personagem, e `CriteriaEvaluator.MeetsCritera` avalia
    condições com o estado atual. As respostas do diálogo 0 (o "ocioso") com
    critério provavelmente são as falas que se liberam ao longo da história.
- **Dúvidas / o que investigar:**
  - O que conta como "fala nova"? Hipótese: uma resposta do diálogo 0 cujo
    critério passou a ser verdadeiro e que o jogador ainda não escolheu.
  - Como saber o que já foi visto? O jogo deve guardar (critérios do tipo
    "diálogo visto"); usar o mesmo dado mantém compatibilidade com saves.
  - Quando recalcular: a cada mudança de valor (`ValueStore`) ou a cada X
    segundos, para não pesar.
  - Personagens fora da tela ou em outro cômodo: a lista no canto resolve.
