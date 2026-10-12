# p50 — Continuar sozinho após o primeiro carregamento (AutoContinue)

## Objetivo

Ao carregar um save, seguir direto da primeira tela de carregamento para a
segunda, sem esperar o clique do jogador. O mod é independente
(`mods/AutoContinue`, DLL própria) e não modifica arquivos do jogo nem os
saves.

## Contexto

- Observado no log do Probe (2026-10-12), ao carregar um save: cena
  `LoadingScreen` (3) → `TransitionScene` (7) → `GameMain` (4). A primeira
  ficou ~16 s, contando a espera pelo clique; a segunda, ~10 s.
- Observado nos assemblies gerados (`Il2CppHouseParty.dll`):
  - `LoadingScreenManager` (`MonoBehaviour`) não herda de
    `TransitionalSceneManager`, a base usada pelo SkipIntro;
  - métodos `Update`, `WasButtonPressed`, `TranslateContinuePrompt`,
    `WaitToStartMainGameScene`, `LoadGame`, `LoadTransitionScene`,
    `LoadRandomCharcter` e `WaitForCharacterLoad`;
  - campos `_loaded`, `_ClickToContinueObject`, `_loadingObject`,
    `_statusBar`, `_blackOutObject`, `_gameLoader` e `_transitionLoader`.
- Hipótese: `Update` mostra `_ClickToContinueObject` quando `_loaded` fica
  `true` e, a partir daí, segue para `TransitionScene` quando
  `WasButtonPressed` responde `true`. Não confirmado.
- Não se sabia se novo jogo passa pela mesma tela nem se a espera tem outra
  função (dica na tela, carregamento ainda em andamento); ver a fase 1.

## Escopo

Incluído:

- Fazer a tela receber "clicou" por um patch Harmony, conferindo a classe
  real do objeto (`Guard`), somente quando `_loaded` for `true` e o aviso de
  clique estiver visível. Assim o próprio jogo segue para a próxima cena.
  Como `WasButtonPressed` divide o código nativo com
  `TransitionalSceneManager.UserInputForTransitionReceived` (fase 1), o
  patch é feito neste, o mesmo método que o SkipIntro corrige.
- README em português e inglês, CHANGELOG, linha nos READMEs da raiz,
  projeto na solução e atualização do `IDEIAS.md`.

Fora do escopo:

- Encurtar o tempo de carregamento ou pular `TransitionScene`.
- Opções de configuração. Para voltar a clicar, basta remover a DLL.

## Estado desejado

Com o mod instalado, ao carregar um save (e, se usar a mesma tela, ao
iniciar novo jogo), a primeira tela de carregamento segue sozinha para a
segunda assim que o carregamento termina, e a partida começa normalmente.

## Riscos e premissas

- `WasButtonPressed` poderia ser chamado antes de o carregamento terminar.
  Na fase 1 só foi chamado com `_loaded` já `true`; o mod confere mesmo
  assim.
- Corrigir `WasButtonPressed` e `UserInputForTransitionReceived` ao mesmo
  tempo derruba o jogo na abertura (fase 1). Patches de mods diferentes no
  mesmo método gerenciado convivem.
- Métodos nativos podem ser compartilhados entre classes; o patch confere a
  classe real.
- Falhas ficam isoladas: com erro, o mod registra uma vez no log e o jogo
  volta a esperar o clique.

## Critérios de aceite

- Carregar um save chega à partida sem clique na primeira tela.
- Novo jogo funciona como sem o mod (sem clique, se usar a mesma tela).
- A partida carregada fica igual à de sem o mod (personagem, posição,
  progresso).
- Sem erros do mod no `MelonLoader\Latest.log`.

## Fases

### Fase 1 — Investigação no jogo

- [x] No `dev/Probe`, registrar as chamadas a `WasButtonPressed` com
      horário, resultado, `_loaded` e se `_ClickToContinueObject` está
      ativo, só quando algo muda.
- [x] Preferência `Probe.AutoContinue` (desligada por padrão) que faz
      `WasButtonPressed` responder `true` com a tela pronta.
- [x] Testar no jogo, sem e com a preferência: carregar um save e iniciar
      novo jogo.

Resultado (2026-10-12, confirmado no jogo e no log):

- O primeiro Probe corrigia `WasButtonPressed` diretamente, e o jogo caiu na
  abertura, sem erro no log. O endereço do código nativo de
  `WasButtonPressed` é o mesmo de
  `TransitionalSceneManager.UserInputForTransitionReceived`
  (`0x7FFC4EE0EDB0`), que o SkipIntro e o Probe já corrigiam. O patch
  passou para esse método, filtrando a classe real `LoadingScreenManager`.
- Sem o teste: `LoadingScreen` carregou em 7,88 s e a primeira chamada veio
  em 10,25 s, já com `loaded=True` e o aviso visível. O clique do usuário
  veio em 18,51 s e `TransitionScene` carregou em 18,52 s.
- Com o teste: a tela seguiu sozinha 2,4 s depois de carregar, e a partida
  abriu normalmente (confirmado pelo usuário). O segundo carregamento,
  depois de voltar ao menu, também passou por `LoadingScreen` e seguiu em
  2,3 s.
- Os prefixos em `TranslateContinuePrompt`, `LoadTransitionScene` e
  `WaitToStartMainGameScene` não registraram chamadas.

Critério de conclusão: saber quando a tela fica pronta, se responder `true`
leva à partida pelo caminho do jogo e se novo jogo usa a mesma tela.

### Fase 2 — Mod

- [x] Criar `mods/AutoContinue` com o mecanismo escolhido e isolamento de
      falhas.
- [x] Adicionar à solução e compilar sem avisos.
- [x] Teste no jogo: carregar um save e iniciar novo jogo, só com o mod.
      Validado pelo usuário (2026-10-12), junto do SkipIntro, sem o Probe e
      sem erros no log.

Critério de conclusão: critérios de aceite confirmados pelo usuário no
jogo.

### Fase 3 — Documentação

- [x] README em português e inglês, CHANGELOG `[Unreleased]`, linha nos
      READMEs da raiz e atualização do `IDEIAS.md`.

Critério de conclusão: documentação equivalente nos dois idiomas.
