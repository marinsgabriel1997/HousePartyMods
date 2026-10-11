# p10 — Marcadores de fala nova (NewTalkMarker)

## Objetivo

Mostrar um "!" sobre o personagem quando surge uma fala que antes não estava
disponível para ele, em consequência do que o jogador fez durante a partida.
O mod é independente (`mods/NewTalkMarker`, DLL própria) e só altera a
exibição; não modifica arquivos do jogo nem os saves.

## Contexto

- Protótipo 0.1.0 em `mods/NewTalkMarker`: marca qualquer resposta disponível
  ainda não escolhida. Reprovado no jogo: personagens ficam marcados o tempo
  todo (por exemplo, Katherine, por falas que já estavam disponíveis).
- Confirmado no jogo com `dev/Probe`:
  - `Response.Selected` vem do save; ao carregar, o jogo recria os objetos
    `Response`, e os valores só são restaurados alguns segundos depois.
  - A liberação de fala é detectável: depois da conversa com Katherine,
    "Katherine seems cool." passou a valer para Stephanie.
  - As falas do menu inicial ficam quase todas em `GlobalResponses`, além das
    respostas do diálogo inicial (`CharacterStoryData.DialogueID`).
  - Avaliar as 708 respostas leva ~7 ms; um personagem por quadro não pesa.
  - Algumas respostas oscilam com o momento (posição, dança, estar ocupado).
  - Falas de entrega de item dependem de critérios `PlayerInventory`/`Item`.
  - O azul ("só falta item"), testado na fase 2, foi retirado: marcava falas
    genéricas escondidas (por exemplo, "Do you know what this means in
    Patrick's notebook?", que exige o caderno) em todo personagem conhecido, e
    não capturava a tarefa da bebida da Katherine, cuja entrega é feita como
    item, fora do menu, e avança uma quest.
- Confirmado no jogo (fase 1): salvamento rápido, manual e automático passam
  por `HousePartySaveLoadManager.Save(nome)` na thread principal, com o nome
  sem extensão (`quick save`, `teste mod`, `auto save`). No automático, só
  `CreateAutoSaveData` roda em outra thread, e o `auto save.save` é gravado
  depois, em `SaveAutoSaveToFile` (visto ao carregar outro save). Carregar
  chama `Load(nome)`; `LoadSavedGame` e `OnSaveGameLoaded` vêm ~20 s depois.
  Os arquivos `.newtalkmarker` não aparecem na lista de saves. Apagar chama
  `Delete(nome)`, e o mod apaga o arquivo dele junto.
- Observado no código (não confirmado no jogo): `HousePartySaveLoadManager`
  tem `Save(filename)`, `Load(fileName)` e `Delete(saveName)`;
  `SaveLoadManager` tem `AutoSaveName`, `QuickSaveName`,
  `PerformAutoSave()`/`SaveAutoSaveToFile()` e `GetFileSaveFolder()`;
  `CDialogue.OnUpdateDialogueResponses` informa as respostas exibidas no menu.

## Escopo

Incluído:

- Estados de cada fala nova:
  - **amarelo**: fala nova disponível ainda não vista no menu;
  - **verde**: fala nova vista no menu, mas não escolhida.
- Uma exclamação por estado presente no personagem, lado a lado e sempre na
  mesma ordem (amarelo, depois verde): "!" amarelo, "!" verde ou "! !".
- Ponto de partida: na primeira vez que o mod vê uma partida (jogo novo ou
  save sem registro do mod), tudo o que está disponível conta como conhecido
  e não marca.
- Fala nova que deixa de valer antes de ser vista perde a marca.
- Só o menu inicial do personagem: `GlobalResponses` e respostas do diálogo
  inicial.
- Persistência por save: o estado do mod acompanha cada save (manual, rápido e
  automático) e é restaurado ao carregá-lo.
- Log de diagnóstico opcional (desligado por padrão).

Fora do escopo: falas que surgem no meio de uma conversa; marca de tarefa
pendente (azul), registrada no `IDEIAS.md` para investigação pelas quests;
destaque de itens no cenário (outra ideia do `IDEIAS.md`); opções em cinza no
menu de respostas.

## Estado desejado

1. Jogo novo ou save antigo sem registro: ninguém tem "!".
2. Conversar com A e sair sem esgotar as perguntas: A continua sem "!".
3. A conversa com B libera uma fala em A: A fica amarelo.
4. Abrir a conversa com A, ver a fala nova e sair sem escolher: A fica verde.
5. Escolher a fala nova: a marca some.
6. Personagem com fala nova não vista e fala vista não escolhida: "! !"
   amarelo e verde.
7. Salvar, fazer outras coisas e carregar o save: as marcas voltam a ser as
   do momento em que o save foi feito.

## Proposta técnica

- **Chave estável da resposta:** personagem + origem (global ou diálogo
  inicial) + `Response.Id`; sem Id, a posição na lista. Nunca o texto, que
  muda com a tradução.
- **Avaliação:** um personagem por quadro, após 5 s de estabilização depois
  de carregar. Classificação de cada resposta: disponível (critérios
  duradouros passam) ou indisponível. Critérios de momento (proximidade, visão, pose, zona,
  porta, sorteio...) são ignorados, como no protótipo.
- **Conversa com o próprio personagem:** uma fala de A liberada pela conversa
  com A (por exemplo, por `MetByPlayer` ou por diálogos de A) que aparece no
  menu durante essa conversa conta como conhecida: faz parte da exploração e
  não fica verde. Se a conversa termina sem mostrá-la (há sequências que
  encerram em vez de voltar ao início), A fica amarelo. O personagem da
  conversa vem de `CDialogue.DisplayDialogue` e é reavaliado a cada quadro até
  3 s depois do fim dela.
- **Estado do mod por resposta:** conhecida, nova (amarelo) ou vista
  (verde). Transições: desconhecida → nova ao surgir depois do ponto de
  partida; nova → vista quando aparece no menu exibido (`DialogueUI.OnNewResponses`,
  o mesmo ponto já usado pelo RelationshipHints); qualquer uma →
  conhecida quando `Selected`; nova/vista → removida quando deixa de valer.
- **Persistência:** arquivo JSON ao lado do save, na pasta informada por
  `SaveLoadManager.GetFileSaveFolder()`, com o nome do save e extensão própria
  (`<save>.newtalkmarker`), para o jogo não listá-lo como save. Gravado quando
  o jogo salva, lido ao carregar e apagado com o save. O arquivo `.save` do
  jogo não é alterado. Sem registro, aplica-se o ponto de partida.
- **Desenho:** IMGUI sobre `Character.Head`, como no protótipo, com uma
  exclamação por estado presente; oculto em pausa, menus e conversas.

## Riscos e premissas

- Hipótese: os patches em `Save`/`Load`/`Delete` recebem o nome do save de
  todos os tipos de salvamento; o automático pode seguir outro caminho ou
  rodar fora da thread principal. A fase 1 valida isso.
- Observado no disco: os saves ficam numa pasta única,
  `%USERPROFILE%\AppData\LocalLow\Eek\House Party\Saves\` (`auto save.save`,
  `quick save.save`), com `steam_autocloud.vdf`. Um arquivo do mod por save
  acompanha o save 1:1, sem depender da história.
- Hipótese: o jogo ignora arquivos de outra extensão nessa pasta (não aparecem
  na lista de saves nem causam erro). A fase 1 valida isso.
- Não verificado no jogo: a restauração do salvamento automático (gravação
  adiada até `SaveAutoSaveToFile`).
- Não verificado: se o Steam Cloud sincroniza o arquivo do mod. Sem
  sincronização, as marcas não acompanham o save em outro computador.
- Respostas sem `Id` dependem da posição na lista; uma atualização do jogo
  pode invalidar o registro (efeito: marcas antigas se perdem, sem erro).
- A lista de critérios "de momento" é hipótese a calibrar com o diagnóstico.
  Observado no primeiro teste da fase 2: piscavam falas com `State`
  (`IsDancing`), `Value CurrentMoveTarget` e grupos `NotCurrentlyOccupied*`,
  agora ignorados.
- Hipótese: sem o azul, obter um item pode liberar de uma vez uma fala
  genérica (como a do caderno do Patrick) em todos os personagens já
  conhecidos depois do ponto de partida, deixando todos amarelos. É fala nova
  de fato; observar se incomoda.
- Patches IL2CPP usam a verificação de tipo nativo (`Guard`), com o código
  compilado no próprio mod.

## Critérios de aceite

- Os passos 1 a 7 do estado desejado observados no jogo, incluindo o caso
  Katherine → Stephanie.
- Salvamento manual, rápido e automático preservam as marcas ao carregar.
- Sem "!" piscando por causa de posição ou atividade dos personagens.
- Sem erros do mod no `Latest.log`; jogo segue normalmente se o mod falhar.
- Funciona com o jogo em outro idioma (chaves não dependem de texto).

## Fases

### Fase 1 — Ciclo de save e load

- [x] Patches de diagnóstico em `Save`, `Load`, `Delete` e salvamento
      automático, registrando nome do save, thread e momento
      (`mods/NewTalkMarker/SaveCycle.cs`).
- [x] Gravar um arquivo de teste `<save>.newtalkmarker` ao lado de cada save.
- [x] Validar no jogo: salvamento manual, rápido (F9), automático, carregar
      e apagar; o arquivo de teste não aparece na lista de saves nem causa
      erro.

Conclusão: o log identifica corretamente o save em todos os tipos de
salvamento e carregamento testados.

### Fase 2 — Estados e cores, sem persistência

- [x] Ponto de partida, classificação das respostas e estados
      amarelo/verde.
- [x] Marcar como vista a partir de `DialogueUI.OnNewResponses`.
- [x] Desenho com a cor do estado e diagnóstico das transições.
- [x] Retirar o azul e desenhar uma exclamação por estado presente.
- [x] Validar no jogo os passos 1 a 6 do estado desejado.

Conclusão: passos 1 a 6 observados no jogo, sem marcas piscando.

### Fase 3 — Persistência por save

- [x] Gravar o estado ao salvar e restaurá-lo ao carregar; apagar com o save.
      No automático, guardar o estado em memória no `Save` e gravar o arquivo
      só em `SaveAutoSaveToFile`, junto com o `auto save.save`. Ao carregar,
      nada é avaliado até `OnSaveGameLoaded` (limite de 120 s) mais os 5 s
      de estabilização; só então o estado lido é aplicado.
- [x] Save sem registro aplica o ponto de partida.
- [x] Validar no jogo o passo 7 com salvamento manual, rápido e automático.
      Manual e rápido confirmados no jogo; o automático não foi testado
      (ocorre a cada ~10 min de jogo) e foi aceito pelo usuário sem teste.

Conclusão: marcas restauradas corretamente nos três tipos de salvamento.

### Fase 4 — Documentação

- [x] README do mod em português e inglês; índice no README da raiz.
- [x] `mods/NewTalkMarker/CHANGELOG.md` com `[Unreleased]`.
- [x] Atualizar a entrada em `IDEIAS.md`.

Conclusão: documentação revisada e equivalente nos dois idiomas.
