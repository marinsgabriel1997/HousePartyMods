# p60 — Pular com o personagem (Jump)

## Objetivo

Uma tecla faz o personagem do jogador pular: sobe, volta ao chão pela queda
do próprio jogo e segue andando normalmente. O mod é independente
(`mods/Jump`, DLL própria) e não modifica arquivos do jogo nem os saves.

## Contexto

Observado nos assemblies gerados (`Il2CppEekCharacterEngine.dll`), sem teste
no jogo:

- `PlayerCharacter` herda de `Character`, que move o corpo por um
  `UnityEngine.CharacterController` (`Controller`, `EnableController`).
- `Character` tem queda própria: `ProcessFalling`, `IsFalling`,
  `_currentlyFalling`, `_fallingDuration`, `_fallStartYVector`,
  `ShouldForceStopFalling` e `ResetFalling`.
- `Character.AddForce(Vector3)` alimenta `addForceVelocity`
  (`CalculateAddForceVelocity`). Também há `AddToFrameMovement(Vector3)`.
- Eventos de personagem `CEvents.JumpUp`, `JumpDown` e `JumpAndFall` (também
  em `CharacterActions`, usados pelos eventos da história), enviados por
  `CharacterBase.SendEvent(int type, ComponentTypes to, bool queue, ...)`.
  Hipótese: tocam animações de pulo no `Mecanim`.
- Volta ao chão navegável: `WarpToClosestMoveTarget`,
  `TryWarpSmoothlyToNearestNavMesh` e `WarpToSafety`.
- `PlayerCharacter.IgnoringMovementInput` e `IgnoreMovementInputsFor`
  indicam quando o jogo bloqueia o movimento.
- `Espaço` é `Player/CombatMode` (`docs/teclas-do-jogo.md`). Livres: `G`,
  `K`, `L`, `N`, `U`, `V`, `X`, `Z`, `3` a `0`, `F1` a `F4`.

Não se sabe se a força vertical é aplicada, se a queda do jogo devolve o
personagem ao chão sem efeitos (desequilíbrio, ragdoll, `LostBalance`,
dano), nem se a volta ao NavMesh puxa o jogador no ar.

## Escopo

Incluído:

- Tecla configurável (padrão `Espaço`, pedido do usuário) que faz o
  personagem pular pelo mecanismo escolhido na fase 1, com altura
  configurável. A animação de pulo do jogo acompanha o pulo parado ou
  andando, não correndo.
- Mover o modo de combate (`Player/CombatMode`) do `Espaço` para o `Alt` no
  teclado, por override de binding do Input System, em todos os
  `PlayerInputMap` (o mapeamento de controles, `F8`, mostra `Alt`), sempre
  que a tecla efetiva do combate for o `Espaço` e a tecla do pulo também.
  Se o jogador tiver posto o combate em outra tecla, o mod não mexe.
- No `F8`, recusar a tecla do pulo para qualquer ação, como tecla já usada.
- Não gravar a troca: ao salvar os controles, o jogo não grava o `Alt` do
  mod, e sem o mod o combate volta ao `Espaço`.
- Opção para desligar a troca do combate.
- Pular só no chão, com o controle livre: fora de menus, diálogos, cenas,
  pausa e com o movimento liberado pelo jogo. Em modo de combate, pula
  também (decisão do usuário: `CCombat.InCombat` não indicou o modo de
  combate e `CombatActive` fica `true` fora dele).
- Opções no MelonPreferences (aparecem no ModSettings), com textos em
  português e inglês como nos outros mods.
- README em português e inglês, CHANGELOG, linha nos READMEs da raiz,
  projeto na solução, tecla em `docs/teclas-do-jogo.md` e atualização do
  `IDEIAS.md`.

Fora do escopo:

- Pulo duplo, pulo no ar, pulo com controle (`Gamepad`).
- Mudar o combate no controle (`Gamepad`) ou outras teclas do jogo.
- Animação de pulo se a fase 1 mostrar que não existe ou não combina com o
  movimento; nesse caso, o pulo é só do corpo e da câmera.

## Estado desejado

Com o mod, apertar a tecla na partida faz o personagem subir e cair de volta
em cerca de um segundo, sem cair no chão como ragdoll, sem tomar dano e sem
ser teletransportado. Andar durante o pulo continua funcionando. Nos menus,
diálogos e cenas, a tecla não faz nada.

## Riscos e premissas

- A queda do jogo pode acionar desequilíbrio ou ragdoll (`PuppetMaster`)
  depois de algum tempo no ar. Mitigação: altura limitada; investigar na
  fase 1.
- A volta ao NavMesh pode puxar o jogador no ar ou depois do pulo.
- Pular pode alcançar lugares fora do previsto (cima de móveis, por cima de
  cercas). O jogo tem `WarpToSafety`; alcançar áreas novas é aceito como
  consequência do pulo, desde que não prenda o jogador.
- Gatilhos de zona da história podem disparar ao pular para outra área,
  como se o jogador tivesse andado até lá.
- Métodos nativos podem ser compartilhados entre classes; patches, se
  houver, conferem a classe real.
- Falhas ficam isoladas: com erro, o mod registra uma vez no log e o jogo
  segue sem pular.
- O jogo grava os overrides ativos ao fechar; o mod retira a própria troca
  do texto gravado (fase 1). Se o jogo mudar a forma de gravar, o `Alt`
  pode ficar salvo; nesse caso, "restaurar padrão" no `F8` devolve o
  combate ao `Espaço`.
- `Alt` com `Tab` troca de janela no Windows; `AltGr` envia `Ctrl`+`Alt`.
- O jogo pode recriar o conjunto de ações (`PlayerInputMap(Clone)`) ao
  carregar; a troca precisa valer para todas as cópias.

## Critérios de aceite

- Na partida, a tecla faz o personagem pular e voltar ao chão; repetir
  várias vezes não acumula altura nem trava.
- Sem ragdoll, dano, desequilíbrio ou teletransporte ao pular no chão
  plano.
- Andar e correr durante o pulo funcionam.
- Em diálogo, menu, pausa e cena, a tecla não faz nada.
- Com a tecla padrão, `Espaço` pula e não liga o combate; `Alt` liga e
  desliga o combate. Sem o mod, o combate volta ao `Espaço`.
- Depois de carregar outro save, o pulo continua funcionando.
- Sem erros do mod no `MelonLoader\Latest.log`.

## Fases

### Fase 1 — Investigação no jogo

- [x] No `dev/Probe`, `V` aplica `AddForce` para cima no jogador e registra
      por ~3 s, a cada quadro em que algo muda: altura, `IsFalling`,
      `Controller.isGrounded`, `addForceVelocity` e se houve warp.
- [x] `Shift`+`V` envia `CEvents.JumpUp` ao jogador e registra o que muda
      (posição, estados), para ver se há animação de pulo.
- [x] Se `AddForce` não subir, testar velocidade própria aplicada por
      `AddToFrameMovement` ou pelo `Controller.Move`, deixando a descida para
      a queda do jogo.
- [x] Testar no jogo: chão plano, perto de móveis, andando e correndo.
- [x] Pular correndo sem a animação, para ver se o tranco some.
- [x] Pular com `Espaço` e mover o combate para o `Alt`; conferir o que o
      `F8` mostra.
- [x] Mostrar `Alt` no `F8`, recusar o `Espaço` no `F8` e conferir se a troca
      fica gravada sem o mod.

Resultados (2026-10-12, no log do Probe e confirmados pelo usuário no jogo):

- `AddForce(v)` vira `addForceVelocity = v / 100`. O jogo soma esse vetor à
  posição a cada quadro, sem multiplicar pelo tempo, e o reduz ~8,5% por
  quadro: força 5 não tira do chão, força 400 levou a ~33 m. Altura e
  duração dependem do FPS; descartado.
- No ar, o jogo marca `IsFalling` e desce em velocidade constante (~10 m/s),
  sem aceleração. Sem `IsFalling`, prende o personagem ao chão: subir por
  `Controller.Move` fora do movimento do jogo só rendeu ~5 cm.
- `AddToFrameMovement` só acumula em `FrameMovement` com o personagem no
  chão; não move.
- Funciona: logo depois de `PlayerCharacter.TryMoveController` (chamado a
  cada quadro, fora do `FixedUpdate`), levar a altura do personagem a uma
  parábola (`v₀ = 4 m/s`, `g = 9,81`, ~0,82 m, ~0,82 s) pelo
  `Controller.Move`. O jogo segue cuidando do andar: a velocidade horizontal
  no ar fica igual à do chão (~1,4 m/s andando, ~4 m/s correndo). Pousa no
  chão e em móveis (um pouso a 0,72 m), sem ragdoll, dano ou warp.
- Pular no ar recomeçava a parábola e acumulava altura até sair voando;
  limitado a pular do chão (`Controller.isGrounded`) fora de outro pulo.
- Rodar ou não a queda do jogo (`ProcessFalling`) durante o pulo não mudou
  o resultado; o mod fica sem esse patch.
- `CEvents.JumpUp` (enviado a `ComponentTypes.All`) toca uma animação de
  pulo, sem mover o corpo nem a câmera nem bloquear o movimento. Andando
  combina; correndo, dá um tranco na câmera. Sem a animação acima de
  2,5 m/s de velocidade horizontal medida (andando ~1,4, correndo ~4), o
  tranco sumiu (confirmado pelo usuário). As velocidades do jogo não servem
  para isso: correndo, `CurrentMovementSpeed` = 0 e `GetSpeed` = 0,02.
- Correr usa `Shift`: a tecla do pulo precisa valer com modificadores.
- Teclas: o jogo grava os remapeamentos do `F8` e os aplica ao
  `PlayerInputMap` original; ao entrar no menu e ao carregar a partida, cria
  cópias `PlayerInputMap(Clone)` com os mesmos overrides, e a partida usa a
  cópia. "Restaurar padrão" no `F8` grava um override para o próprio
  `<Keyboard>/space`, então a troca confere a tecla efetiva
  (`effectivePath`), não a existência de override.
- Trocar só nas cópias (`ApplyBindingOverride` de `<Keyboard>/space` para
  `<Keyboard>/alt`) fez o `Espaço` pular sem ligar o combate e o `Alt` ligar
  o combate (confirmado pelo usuário). O `F8` continuava mostrando `Space`,
  por ler o original, o que confunde o jogador.
- Trocando também no original, o `F8` mostra `Alt` (confirmado pelo
  usuário). A tela é a `RebindActionUI` (exemplo de remapeamento do Unity):
  `IsDuplicate` respondendo `true` para `/Keyboard/space` faz o `F8` recusar
  o `Espaço` e continuar esperando outra tecla (confirmado). "Restaurar
  padrão" remove o override; a sonda reconfere a cada segundo e troca de
  novo.
- O jogo grava os overrides ativos ao fechar, por
  `SaveBindingOverridesAsJson(IInputActionCollection2)`: sem filtro, o
  `<Keyboard>/alt` do mod ficou gravado e voltou sem o mod. Retirando do
  texto devolvido a entrada do combate com `<Keyboard>/alt` (um `Alt`
  escolhido no `F8` é gravado como `<Keyboard>/leftAlt`), a gravação ficou
  `{"bindings":[]}` e, sem a troca, o combate abriu no `Espaço`.

Critério de conclusão: um mecanismo que sobe e volta ao chão sem ragdoll,
dano ou warp, com a altura segura conhecida, e a decisão sobre a animação.

### Fase 2 — Mod

- [x] Criar `mods/Jump` com o mecanismo da fase 1, as condições de controle
      livre, as opções e o isolamento de falhas.
- [x] Adicionar à solução e compilar sem avisos.
- [x] Teste no jogo, só com o mod e os demais mods do repositório: os
      critérios de aceite. Validado pelo usuário (2026-10-12), sem o Probe.

Critério de conclusão: critérios de aceite confirmados pelo usuário no
jogo.

### Fase 3 — Documentação

- [x] README em português e inglês, CHANGELOG `[Unreleased]`, linha nos
      READMEs da raiz, tecla em `docs/teclas-do-jogo.md` e atualização do
      `IDEIAS.md`.

Critério de conclusão: documentação equivalente nos dois idiomas.
