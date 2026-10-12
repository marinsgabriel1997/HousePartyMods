# p30 — Pular aberturas ao abrir o jogo (SkipIntro)

## Objetivo

Ao abrir o jogo, ir direto ao menu principal, sem o vídeo da Eek! Games e
sem a tela de aviso. O mod é independente (`mods/SkipIntro`, DLL própria) e
não modifica arquivos do jogo nem os saves.

## Contexto

- Observado nos dados do jogo (`globalgamemanagers`), ordem das cenas no
  build: `EekGamesIntro` → `Disclaimer` → `MainMenu` → `LoadingScreen` →
  `GameMain` → `ForestEnvironment` → `Credits` → `TransitionScene`.
- Observado nos assemblies gerados (`Il2CppHouseParty.dll`):
  - `EekGamesIntroManager` e `DisclaimerManager` herdam de
    `TransitionalSceneManager` (`MonoBehaviour`);
  - `EekGamesIntroManager` tem `_introVid`, `PlayIntroDelayed`, `Update` e
    `TryProcessSceneTransition`;
  - `TransitionalSceneManager` tem `bool UserInputForTransitionReceived()`,
    `TryProcessButtonPressForFadeOut()`, `TryProcessSceneTransition(string)`
    e os campos `_shouldProcessSceneTransition`, `_loadedNextScene` e
    `_timeOfButtonPress`.
  - `UserInputForTransitionReceived` retorna `bool`: só verifica se houve
    tecla, não faz a tela avançar.
- Hipótese: as duas telas já avançam com uma tecla, quando
  `UserInputForTransitionReceived` responde `true`, seguida de um fade. Não
  confirmado.
- O splash do Unity, se houver, roda antes dos mods e fica fora do alcance.
- `EekEvents` tem `ShowDisclaimer` e `EekUI` tem `SkipIntroCutscenes`; o
  segundo parece ser a opção de pular a cena inicial dentro da partida, que
  não faz parte deste plano.

## Escopo

Incluído:

- Em `EekGamesIntro` e `Disclaimer`, avançar pelo caminho do próprio jogo:
  um patch Harmony faz `UserInputForTransitionReceived` responder `true`,
  como se o jogador tivesse apertado uma tecla, conferindo a classe real do
  objeto (`Guard`). Assim o `Awake` e o `Start` dessas telas continuam
  rodando.
- Se esse caminho ainda deixar a tela visível por tempo perceptível (fade,
  vídeo), carregar a próxima cena direto, desde que o teste da fase 1 mostre
  que nada se perde.
- README em português e inglês, CHANGELOG, linha nos READMEs da raiz e
  projeto na solução.

Fora do escopo:

- Splash do Unity.
- Cena inicial dentro da partida (`SkipIntroCutscenes`).
- Tela de carregamento do save (ideia separada no `IDEIAS.md`).

## Estado desejado

Com o mod instalado, o jogo abre no menu principal sem mostrar o vídeo nem
o aviso, ou mostrando no máximo um quadro deles, e o menu funciona como
antes.

## Riscos e premissas

- O `Awake` ou o `Start` dessas telas pode inicializar algo de que o menu
  depende (Steam, idioma, configurações). Por isso o caminho preferido é o
  do próprio jogo, e não carregar `MainMenu` direto.
- Se o aviso gravar que foi exibido, pulá-lo pode fazer o jogo mostrá-lo de
  novo depois. Verificar no teste.
- Nomes de cena como identificação: se o jogo os renomear, o mod
  simplesmente não age.
- Falhas ficam isoladas: com erro, o mod registra uma vez no log e o jogo
  segue com as telas normais.

## Critérios de aceite

- O jogo abre no menu principal sem o vídeo e sem o aviso.
- Menu, novo jogo e carregar save funcionam como sem o mod.
- Sem erros do mod no `MelonLoader\Latest.log`.

## Fases

### Fase 1 — Investigação no jogo

- Dispensado: confirmar quais telas aparecem sem o mod e se uma tecla já
  as pula. O teste abaixo respondeu o essencial.
- [x] No `dev/Probe`, registrar a carga de cada cena com horário e fazer
      `UserInputForTransitionReceived` responder `true` (preferência
      `Probe.SkipIntro`, desligada por padrão depois do teste).
- [x] Medir no log quanto tempo cada tela fica antes do menu com o teste
      ligado. Sem o teste não foi medido.

Resultado (2026-10-11, confirmado no jogo e no log): com o teste ligado, o
usuário viu o menu direto. `EekGamesIntro` carregou em 0,18 s e chamou
`TryProcessSceneTransition('Disclaimer')` em 0,20 s; `Disclaimer` carregou
em 0,81 s e chamou `TryProcessSceneTransition('MainMenu')` em 0,82 s;
`MainMenu` carregou em 1,55 s. Sem erros. Escolhido o patch; a carga direta
da próxima cena não é necessária.

Critério de conclusão: saber se a chamada leva ao menu e em quanto tempo,
e decidir entre ela e a carga direta da próxima cena.

### Fase 2 — Mod

- [x] Criar `mods/SkipIntro` com o mecanismo escolhido e isolamento de
      falhas. O patch age só em `EekGamesIntroManager` e `DisclaimerManager`.
- [x] Adicionar à solução e compilar sem avisos.
- [x] Teste no jogo: abrir o jogo, chegar ao menu, iniciar novo jogo e
      carregar um save. Validado pelo usuário (2026-10-11), só com o
      SkipIntro, sem o Probe.

Critério de conclusão: critérios de aceite confirmados pelo usuário no
jogo.

### Fase 3 — Documentação

- [x] README em português e inglês, CHANGELOG `[Unreleased]`, linha nos
      READMEs da raiz e atualização do `IDEIAS.md`.

Critério de conclusão: documentação equivalente nos dois idiomas.
