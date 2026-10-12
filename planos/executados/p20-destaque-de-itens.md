# p20 — Destaque de itens no cenário (ItemHighlight)

## Objetivo

Duas teclas ligam e desligam, de forma independente, o destaque dos itens
interativos à vista: o nome sobre o item, com cor por tipo de ação, como em
Baldur's Gate 3 e Divinity, e um brilho rosa pulsante (cor tema do jogo) nos
coletáveis, para o jogador não ter de passar
o mouse em tudo. O mod é independente (`mods/ItemHighlight`,
DLL própria) e só altera a exibição; não modifica arquivos do jogo nem os
saves.

## Contexto

- Observado no código descompilado:
  - `ItemManager.GetAllItems()` lista todos os `InteractiveItem`;
  - `InteractiveItem.Interactions` é `List<string>` com o nome das ações;
  - `DisplayName`, `_inventoried` e `IsRendererEnabled` estão disponíveis;
  - `ClickManager.Highlight()` é de UI, e o jogo (HDRP) não tem contorno ou
    brilho próprio para objetos. O visual será do mod.
- Confirmado no jogo com `dev/Probe` (2026-10-11):
  - 292 itens, 286 ativos;
  - os personagens também são `InteractiveItem` (`[Inspect, Talk]`);
  - `canBeGrabbed` vale `True` para sofá, fogão e personagens e não serve;
  - coletável = item com `Take` em `Interactions` (60 com `[Inspect,Take]`,
    mais variações como `Take One`, `Eat`, `Drink`);
  - frequências: 73 sem ações, 35 `[Open]`, 31 `[Inspect]`, 29 `[Sit]`,
    14 `[Close]`, 8 `[Select]`, além de ações únicas (`Tamper`, `Rummage`,
    `Untie`, `Turn On`...). Há ações com parâmetro, como `Remove;Top` e
    `Use With...`;
  - `IsRendererEnabled` é `False` em portas e assentos ativos com ações (28
    `[Open]`, 20 `[Sit]`), que não têm renderer próprio, e por isso não serve
    de filtro.
  - O `Input` antigo do Unity funcionou na sonda (F8), sem recorrer ao
    Input System.
- Teclas do jogo, lidas no `InputActionAsset` (`sharedassets2.assets`):
  W/A/S/D, B, C, E, F, I, M, O, P, Q, R, T, U, Y, 1, 2, Tab, Espaço, Shift,
  Ctrl, Enter, Esc, crase, setas, Page Up/Down, F5, F8, F9, F10 e F11. O
  jogador pode remapear. As teclas G, H, J, K, L, N, V, X e Z estão livres.

## Escopo

Incluído:

- Duas teclas independentes e configuráveis: `H` mostra e esconde os
  nomes; `J` passa pelos níveis do brilho (0,3, 0,6, 1,2, 2,5 e 5) e, depois
  do último, o desliga.
- Itens exibidos: ativos na cena, com ao menos uma ação, fora do
  inventário, na frente da câmera e com linha de visão a partir da câmera
  (itens atrás de paredes ou de outros objetos não aparecem). Sem limite de
  distância: a linha de visão já restringe ao que o jogador vê.
- Personagens ficam de fora: itens com `Talk` ou dentro de um `Character`
  (inclui o jogador, que tem ações como `Remove;Top`).
- Nome do item (`DisplayName`) na tela, sobre o item, com cor por categoria.
  A categoria é a de maior prioridade entre as ações do item:
  1. **coletável**: `Take`, `Take One`;
  2. **uso**: qualquer ação fora das demais categorias (`Drink`, `Eat`,
     `Select`, `Turn On`, `Tamper`, `Rummage`...);
  3. **abrir/fechar**: `Open`, `Close`;
  4. **posição**: `Sit`, `Lay Down`, `Climb`;
  5. **examinar**: só `Inspect`.
- Por padrão só aparecem coletável e uso; cada categoria tem uma opção para
  ligar ou desligar.
- Cores dos nomes: coletável dourado, uso ciano, abrir/fechar verde-claro
  (o azul-claro ficaria parecido com o ciano), posição cinza-claro, examinar
  branco.
- Ações com parâmetro são comparadas pelo nome antes do `;`.
- `Use With...` (usar um item do inventário no objeto) é ignorada na
  classificação: quase todo objeto fixo da casa a tem (fogão, micro-ondas,
  impressora, lavadora), e ela punha todos em "uso". Assim, objetos com só
  `Inspect` e `Use With...` passam a "examinar".
- Brilho: pulso rosa lento (2,5 s, chegando a zero entre um pulso e
  outro) no próprio item, por padrão só nos coletáveis à vista; a opção
  `GlowUse` inclui os itens de uso. As opções `Show*` de categoria valem
  para o brilho também. Ao deixar de valer (destaque desligado, item fora de
  vista, pego, pausa, menu ou conversa), o item volta ao normal.
- O destaque fica escondido em pausa, menus e conversas, como as marcas do
  NewTalkMarker.
- Configuração: `LabelsKey` (`H`), `GlowKey` (`J`), `StartLabels` e
  `StartGlow` (estado inicial ao abrir o jogo, desligados; o brilho começa
  no primeiro nível), `GlowUse` (desligada) e `ShowCollectible`, `ShowUse`
  (ligadas), `ShowOpenClose`, `ShowPosition`, `ShowInspect` (desligadas).

Fora do escopo:

- contorno ou halo em volta do objeto (técnicas B e C da fase 2) e halo
  2D na tela, descartado no teste;
- compensar o brilho em itens pequenos;
- filtrar itens de quest ou quebra-cabeça. Todos aparecem, e o jogador
  decide quando usar a tecla;
- separar rótulos sobrepostos. A linha de visão e as categorias padrão
  devem bastar; reavaliar se o teste mostrar o contrário.

## Estado desejado

1. Ao abrir o jogo, nada é destacado.
2. Apertar `H` mostra o nome dos itens à vista, com a cor da categoria;
   apertar de novo esconde. `J` liga o brilho no primeiro nível e, a cada
   toque, passa ao próximo; depois do último, desliga. Nomes e brilho não se
   afetam.
3. Battery Pack aparece como coletável. Portas, sofás e itens só de examinar
   (como micro-ondas e impressora) só aparecem ao ligar as opções
   correspondentes, com as próprias cores.
4. Personagens e itens no inventário não aparecem.
5. Itens atrás de paredes, de outros objetos ou da câmera não aparecem;
   itens sobre uma prateleira à vista aparecem.
6. Em pausa, menus e conversas, nada aparece; ao voltar, o destaque continua
   como estava.
7. Pegar um item faz o rótulo e o brilho dele sumirem. Carregar outro save
   não deixa rótulos ou brilhos antigos nem erros.
8. Com o brilho ligado, os coletáveis à vista pulsam em rosa, na força do
   nível escolhido; vaso, fogão e lixeira não pulsam (a menos que `GlowUse`
   esteja ligada). Ao desligar, os itens voltam ao normal.

## Proposta técnica

- **Lista de itens:** `ItemManager.GetAllItems()` relida a cada 1 s
  enquanto o destaque está ligado. O jogo só preenche `Interactions` ao
  calcular as ações do item (no hover do mouse), por isso o mod chama
  `HasInteractions(história ativa)` antes de ler a lista. Confirmado no jogo:
  sem essa chamada só apareciam itens que já tinham recebido o mouse. A
  leitura de todos de uma vez custou 11,8 ms; por isso ações, categoria e
  nome são recalculados em rodízio, alguns itens por quadro, e guardados
  por item. Objetos destruídos são descartados, e a lista é
  esvaziada ao trocar de cena. Ao carregar um save, a releitura a cada 1 s
  substitui as referências antigas; o desenho confere cada item antes de
  usá-lo. A lista é lida enquanto nomes ou brilho estão ligados. As teclas
  são ignoradas enquanto o destaque estaria escondido (por
  exemplo, ao digitar o nome de um save).
- **Linha de visão:** `Physics.Linecast` da câmera ao centro do
  `Renderer.bounds` do item (ou à posição do item, sem renderer), ignorando
  triggers. O item é visível se o raio não bate em nada ou bate num
  collider do próprio item. Feito em `OnUpdate`, só para itens das
  categorias ligadas e na frente da câmera.
- **Posição do rótulo:** topo, na tela, do `Renderer.bounds` do item (cantos
  projetados), com folga fixa de poucos pixels; sem renderer ativo,
  `transform.position`. Observado no jogo: com o deslocamento de 15 cm no
  mundo, o nome ficava bem acima de itens vistos de perto.
- **Desenho:** IMGUI em `OnGUI` (mesma técnica do NewTalkMarker, com código
  próprio, sem dependência entre DLLs): `WorldToScreenPoint`, descartando
  `z <= 0`, com sombra para legibilidade.
- **Brilho:** emissão do HDRP (`_EmissiveColor`, com
  `_EmissiveExposureWeight` 0 para não depender da exposição da câmera) via
  `MaterialPropertyBlock`, a cada quadro, nos `MeshRenderer` e
  `SkinnedMeshRenderer` do item (partículas presas ao item ficam de fora).
  Confirmado no protótipo: 117 materiais `HDRP/Lit` aceitam a emissão. Ao
  restaurar, o bloco é removido se o renderer não tinha um antes; se tinha,
  a emissão volta aos valores do material.
- **Teclas:** `Input.GetKeyDown` de `LabelsKey` e `GlowKey`, com a mesma reserva da sonda:
  se o `Input` antigo lançar exceção, passa ao Input System.
- **Estado oculto:** mesma checagem do NewTalkMarker (`Time.timeScale`,
  `CanvasEvents.IsPauseWhenOpenActive()`, `CDialogue.CurrentlyOnDisplay`).
- Sem patches Harmony previstos. Se algum for necessário, usar `Guard`.
- Falhas são capturadas e registradas uma vez, para não inundar o log; o
  jogo segue normalmente.

## Riscos e premissas

- Hipótese: `DisplayName` pode vir traduzido ou não, conforme o idioma do
  jogo. Nos dois casos o rótulo funciona.
- Confirmado no jogo: sem linha de visão e com todas as categorias, a casa
  inteira aparecia através das paredes, com nomes sobrepostos.
- Hipótese: o raio pode esconder itens visíveis cujo centro fica encostado
  em outro collider (por exemplo, garrafa deitada na prateleira), e mostrar
  itens atrás de vidros sem collider. Calibrar no jogo.
- Hipótese: sem limite de distância, itens vistos de longe por janelas e
  portas abertas aparecem. Reavaliar no teste.
- Observado na sonda: "Sleeping Mask" aparece ativo, com `Take` e sem
  renderer. Pode ser um item escondido ou vestido por alguém e gerar rótulo
  sem objeto visível. Verificar no jogo.
- Itens sem `DisplayName` usam o nome do `GameObject`. Aceito.
- A tecla `H` pode conflitar com um remapeamento do jogador; por isso é
  configurável.
- Custo: até ~200 raios e projeções por quadro devem ser baratos; medir no
  jogo se houver queda perceptível.
- Confirmado no jogo: com "uso" no brilho e `Use With...` na classificação,
  vaso, fogão e lixeira pulsavam; o usuário pediu que não.
- Confirmado no protótipo: itens pequenos (como a chave) quase não aparecem
  com o brilho; o nome continua sendo o destaque principal deles.
- Hipótese: materiais sem `_EmissiveColor` (shaders próprios) não brilham.
  Aceito.
- Confirmado no jogo: a mesma intensidade não serve para todos os
  materiais. 0,3 basta para itens escuros e foscos da garagem, mas a garrafa
  de cerveja e a folha de papel ficaram fracas em 1,2 e boas em 2,5. Por
  isso a tecla passa por níveis, e o jogador escolhe a força.
- `MaterialPropertyBlock` tira o renderer do SRP Batcher enquanto brilha;
  custo pequeno para dezenas de itens. Medir se houver queda.

## Critérios de aceite

- Passos 1 a 8 do estado desejado observados no jogo.
- Cores das categorias distinguíveis e legíveis em cômodos claros e escuros.
- Sem erros do mod no `Latest.log`; jogo segue normalmente se o mod falhar.
- Sem queda perceptível de desempenho com o destaque ligado.

## Fases

### Fase 1 — Mod base e rótulos

- [x] Criar `mods/ItemHighlight` (csproj, `Mod.cs`, entrada no
      `HousePartyMods.slnx`), versão 0.1.0.
- [x] Tecla de alternar, leitura da lista, categorias, filtros e desenho.
- [x] Build da solução sem erros.
- [x] Revisão após o primeiro teste: `HasInteractions` antes de ler as
      ações, leitura em rodízio, linha de visão, opções por categoria e
      remoção de `MaxDistance`.
- [x] Validar no jogo os passos 1 a 7 e os critérios de aceite. Feito junto
      com a validação da fase 3.

Conclusão: passos 1 a 7 observados no jogo, sem erros no log.

### Fase 2 — Investigação do brilho

O usuário quer, além do texto, o item brilhando, com aura, como na maioria
dos jogos; texto e brilho configuráveis separadamente. Técnicas avaliadas,
da mais simples à mais complexa: (A) emissão pulsante do HDRP no próprio
item via `MaterialPropertyBlock`, que com bloom pode formar a aura; (B)
casca colorida (cópia do objeto ampliada, só a cor); (C) contorno com shader
próprio, compilado num AssetBundle do Unity 2022.3 com HDRP.

- [x] Protótipo da técnica A no `dev/Probe` (`Glow.cs`): F7 liga e desliga
      o brilho nos itens interativos até 10 m; F6 alterna a intensidade; o
      log lista os shaders dos itens e quantos materiais têm
      `_EmissiveColor`.
- [x] Avaliar a técnica A no jogo. Confirmado: a emissão acende o item, mas
      não forma aura (sem bloom perceptível). Intensidades de 1 a 30 ficaram
      fortes demais; 0,2, branca e em pulso lento de 2,5 s, ficou discreta,
      mas itens pequenos (como a chave) quase não aparecem. O usuário quer
      algo próximo do destaque do The Witcher 3 (contorno com halo).
- [x] Protótipo de halo 2D no `dev/Probe`: mancha radial branca pulsante
      desenhada na tela sobre o item, maior que ele e com tamanho mínimo
      (48 px); F6 alterna a opacidade; F4 liga e desliga a emissão junto.
- [x] Avaliar o halo 2D no jogo. O primeiro teste falhou: `GUI.DrawTexture`
      ou `GUI.color` foi removido do jogo ("Method unstripping failed"); com
      o halo como fundo de `GUI.Label`, ele apareceu, e o usuário o
      descartou. Fica a emissão pulsante, em âmbar.
- [x] Calibrar no jogo a intensidade da emissão âmbar: 0,3, aprovada pelo
      usuário.
- [x] Revisar este plano com a técnica escolhida.

Conclusão: técnica escolhida, ou brilho descartado, com o resultado do
teste registrado.

### Fase 3 — Brilho no ItemHighlight

- [x] Implementar o brilho e as opções `ShowLabels`, `ShowGlow` e
      `GlowIntensity` (depois substituídas pelas teclas independentes e
      pelos níveis).
- [x] Retirar o protótipo do brilho do `dev/Probe` e o `Probe.dll` da pasta
      `Mods` do jogo.
- [x] Build da solução sem erros.
- [x] Brilho visível no jogo (após remover um ReShade/DLSS instalado por
      fora, que não tinha relação com o mod).
- [x] Revisão após o teste: teclas independentes para nome e brilho, brilho
      rosa só nos coletáveis (`GlowUse`), `Use With...` ignorada na
      classificação, nome de uso em ciano e posição do nome ancorada na
      tela.
- [x] Níveis do brilho na tecla `J` (0,3, 0,6, 1,2, 2,5 e 5), no lugar de
      `GlowIntensity`, após o teste com a cerveja e o papel.
- [x] Validar no jogo os passos 1 a 8 e os critérios de aceite (inclui a
      validação pendente da fase 1). Observados nos testes: nomes sem
      atravessar paredes, posição acima do item, teclas independentes,
      brilho só nos coletáveis e níveis. O usuário aprovou o conjunto após o
      roteiro de validação; não foram medidos desempenho nem verificados a
      "Sleeping Mask" e itens atrás de vidros.

Conclusão: passos 1 a 8 observados no jogo, sem erros no log.

### Fase 4 — Documentação

- [x] README do mod em português e inglês; índice nos READMEs da raiz.
- [x] `mods/ItemHighlight/CHANGELOG.md` com `[Unreleased]`.
- [x] Atualizar a entrada em `IDEIAS.md`.

Conclusão: documentação revisada e equivalente nos dois idiomas.
