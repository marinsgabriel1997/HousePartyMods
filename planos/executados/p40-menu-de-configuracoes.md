# p40 — Menu de configurações dos mods (ModSettings)

## Objetivo

Permitir ajustar as opções dos mods dentro do jogo, numa janela aberta por
tecla, sem editar `UserData\MelonPreferences.cfg` à mão. O mod é independente
(`mods/ModSettings`, DLL própria) e nenhum outro mod passa a depender dele.

## Contexto

- Observado no código: os quatro mods com opções (RelationshipHints,
  NewTalkMarker, ItemHighlight, além do Probe) já as registram no
  MelonPreferences com `CreateCategory`/`CreateEntry`, todas com descrição em
  português. O SkipIntro não tem opções.
- Observado no código: a maior parte das opções é lida na hora (`.Value` a
  cada uso): `MaxDistance` e `DiagnosticLog` do NewTalkMarker,
  `DiagnosticLog` do RelationshipHints, `Show*` e `GlowUse` do ItemHighlight.
  Só são lidas ao iniciar: `LabelsKey` e `GlowKey` (viram `HotKey` no
  `OnInitializeMelon`) e `StartLabels`/`StartGlow`, que por natureza valem ao
  abrir o jogo.
- Observado no `MelonPreferences.cfg` local: há entradas órfãs de versões
  antigas do ItemHighlight (`ToggleKey`, `ShowLabels`...). Hipótese: o
  MelonLoader as mantém no arquivo, mas não como entradas registradas, então
  não apareceriam numa lista feita a partir das entradas.
- Confirmado no jogo (p20): `GUI.Label` funciona, mas `GUI.DrawTexture` e
  `GUI.color` foram removidos do jogo ("Method unstripping failed"). Não se
  sabe se `GUI.Window`, `GUI.Button`, `GUI.Toggle`, `GUI.TextField`,
  `GUI.HorizontalSlider` e `GUI.BeginScrollView` sobreviveram.
- O jogo tem teclas que não aparecem na tela de controles (relato do
  usuário). Não há registro das teclas usadas, e os mods já escolheram `H`,
  `J` e `F7` (Probe) sem esse levantamento.
- Não se sabe se o jogo usa `F10`, nem como ele trata cursor e cliques
  (câmera, interação) enquanto uma janela do mod está aberta.

## Escopo

Incluído:

- **Descoberta automática:** o menu lista as categorias registradas no
  MelonPreferences que tenham entradas visíveis (ignora `IsHidden`) e monta
  um controle por entrada, pelo tipo:
  - `bool` → caixa de marcar;
  - `int`/`float`/`double` → campo numérico;
  - `string` → campo de texto;
  - `enum` → botão que alterna entre os valores;
  - outros tipos → valor só para leitura.
- Nome da entrada (`DisplayName`) e descrição (`Description`) aparecem junto
  do controle.
- **Janela:** lista de mods à esquerda, opções do mod escolhido à direita,
  com rolagem e botão `Padrão` por opção. Sem botões de salvar ou cancelar:
  cada mudança é aplicada e gravada na hora (`MelonPreferences.Save()`);
  campos de texto gravam ao terminar a edição (Enter, Tab ou clique fora),
  e `Esc` durante a edição a desfaz. Valor inválido não é aplicado e fica
  em vermelho.
- **Abertura:** tecla `F6` (opção `OpenKey` do próprio mod) abre e fecha;
  `Esc` também fecha. Na partida, abrir a janela também abre a pausa do
  jogo, como o `Esc` faz; enquanto a janela está aberta, as ações do jogo
  ficam desligadas, para que cliques e teclas não acertem a pausa por baixo.
  Ao fechar, as ações voltam e, se foi a janela que abriu a pausa, ela
  fecha junto; se o jogador já estava na pausa, ela continua.
- **Nomes em português:** cada mod preenche o `display_name` das suas
  opções no `CreateEntry` (metadado padrão do MelonPreferences, sem
  depender do ModSettings); o arquivo continua com os identificadores.
- **Tamanho:** janela, letras e linhas escalam com a altura da tela (base
  1080); fundo opaco; opções booleanas como duas opções de rádio
  (Ligado/Desligado).
- **Idioma do jogo:** os textos dos mods seguem o idioma escolhido no jogo
  (`TranslationManager.languageCurrent`): português para "Portuguese
  (Brazil)" e inglês para os demais. Cada mod tem as próprias traduções e
  as aplica sozinho, sem depender do ModSettings; ao trocar o idioma com o
  jogo aberto, os textos acompanham. Entram:
  - a janela do ModSettings;
  - nome e descrição das opções de todos os mods;
  - letras do RelationshipHints: `A`/`R` (amizade/romance) em português e
    `F`/`R` (friendship/romance) em inglês;
  - nomes dos itens do ItemHighlight, se o jogo não os traduzir sozinho
    (a investigar).
- **Teclas do jogo:** levantamento das teclas que o jogo usa, registrado em
  `docs/teclas-do-jogo.md` como referência para escolher teclas dos mods.
- **ItemHighlight:** `LabelsKey` e `GlowKey` passam a valer na hora, ao
  mudarem. Registro no CHANGELOG dele.
- README em português e inglês, CHANGELOG, linha nos READMEs da raiz (e a
  menção ao menu na seção de configuração) e projeto na solução.

Fora do escopo:

- API para os mods registrarem opções (decidido: só descoberta automática).
- Botão "Mods" no menu principal ou de pausa do jogo (fica para depois).
- Faixas e sliders por mod (exigiriam API ou metadados).
- Idiomas além de português e inglês (os demais idiomas do jogo mostram
  inglês).
- Mensagens do log, que continuam em português.

## Estado desejado

Com o ModSettings instalado, `F6` abre uma janela no menu principal e
durante a partida com os mods instalados e suas opções; o jogador muda
valores, salva, e as mudanças valem na hora e continuam valendo ao reabrir o
jogo. Sem o ModSettings, os demais mods funcionam como antes.

## Riscos e premissas

- **Controles IMGUI removidos do jogo:** se `GUI.Window`, `Button`,
  `Toggle` ou `TextField` falharem, a alternativa é desenhar com
  `GUI.Label` (que funciona) e tratar cliques e teclas pelo `Event.current`
  ou pelo Input. Se nem isso servir, a troca para uGUI é mudança de
  arquitetura e exige revisar este plano.
- **Cliques vazando para o jogo:** confirmado na fase 1 que, sem bloqueio, a
  câmera, o clique e as teclas agem no jogo. A pausa do jogo por baixo e as
  ações desligadas devem resolver; leituras diretas do teclado (WASD, `H`
  do ItemHighlight) ficam sem efeito na pausa, mas isso precisa ser
  confirmado.
- **Mudança de valor com efeito inesperado:** valores aplicados na hora
  atingem mods que não esperam a troca. Premissa observada no código: os
  nossos leem `.Value` a cada uso, exceto as teclas tratadas neste plano.
- **Mods de terceiros:** aparecem pela descoberta automática; tipos não
  previstos ficam só para leitura.
- Falhas do menu são registradas uma vez no log e não interrompem o jogo
  nem os outros mods.

## Critérios de aceite

1. `F6` abre e fecha a janela no menu principal e durante a partida; `Esc`
   também fecha.
2. Aparecem RelationshipHints, NewTalkMarker, ItemHighlight e ModSettings,
   com as descrições; entradas órfãs do arquivo não aparecem.
3. Mudar uma opção Ligado/Desligado, um número e um texto grava o
   `MelonPreferences.cfg` na hora, sem botão de salvar; `Esc` durante a
   edição de um campo a desfaz; `Padrão` restaura e grava o valor padrão.
4. Sem reiniciar, valem na hora: `MaxDistance` do NewTalkMarker, `ShowUse`
   do ItemHighlight e `LabelsKey` do ItemHighlight.
5. Na partida, `F6` abre a pausa junto com a janela; clicar na janela não
   aciona os botões da pausa; fechar a janela fecha a pausa e o controle
   volta ao normal. Aberta a partir da pausa, a janela fecha e a pausa fica.
6. Número inválido fica em vermelho, não é gravado e não gera erro no log.
7. Build da solução sem erros.
8. Com o jogo em português, a janela, os nomes e descrições das opções e as
   letras `A`/`R` aparecem em português; com o jogo em inglês (ou outro
   idioma), em inglês, com `F`/`R`.
9. Trocar o idioma no jogo atualiza os textos sem reiniciar.

## Fases

### Fase 1 — Investigação com o Probe

- [x] Testar no jogo `GUI.Window`, `GUI.DragWindow`, `GUI.Button`,
  `GUI.Toggle`, `GUI.TextField`, `GUI.Box` e `GUI.BeginScrollView`, numa
  janela de teste aberta por tecla.
- [x] Levantar as teclas que o jogo usa, inclusive as que não aparecem na
  tela de controles, por três caminhos:
  - ações e bindings do Input System carregados em memória
    (`InputActionAsset`), listados pelo Probe;
  - teclas consultadas pelo Input antigo (`Input.GetKey*` com `KeyCode`),
    registradas por um patch do Probe enquanto se joga;
  - nomes de classes e métodos ligados a teclas nos assemblies gerados
    (atalhos de depuração, captura de tela, console).
- [x] Registrar o resultado em `docs/teclas-do-jogo.md`, separando o que foi
  confirmado no jogo do que foi só observado no código, e incluir a
  referência na tabela de referências situacionais do `AGENTS.md` ("antes de
  escolher a tecla de um mod").
- [x] Confirmar que `F10` está livre; se não estiver, escolher outra pelo
  levantamento.
- [x] Verificar o cursor e se cliques e movimento do mouse chegam ao jogo
  com a janela aberta, no menu e na partida.
- [x] Listar, pelo MelonPreferences, categorias, entradas, tipos e
  `IsHidden`, conferindo que as órfãs não aparecem.

Resultados (2026-10-11):

- Confirmado no jogo: funcionam `GUI.Window`, `GUI.DragWindow`, `GUI.Box`,
  `GUI.Button`, `GUI.Toggle`, `GUI.Label`, `GUI.contentColor`,
  `GUI.backgroundColor` e `Event.current`. Falham com "Method unstripping
  failed": `GUI.TextField`, `GUI.HorizontalSlider`, `GUI.BeginScrollView` e
  `GUILayout`. Texto e rolagem terão de ser feitos à mão, com
  `Event.current` (teclas digitadas e roda do mouse) e `GUI.Label`.
- Confirmado no jogo: campo de texto e rolagem feitos à mão com
  `Event.current` funcionam, assim como `GUI.BeginGroup`.
- Confirmado no jogo: na partida, o jogo esconde o cursor a cada quadro no
  `Update`; forçar o cursor visível também no `LateUpdate` e no `OnGUI`
  resolve.
- Confirmado no jogo: sem bloqueio, com a janela aberta, a câmera gira, o
  clique age no jogo e as teclas (WASD, `Esc`, `h` do ItemHighlight) agem.
- Decidido: na partida, abrir a janela abre também a pausa do jogo, pelo
  evento `PlayerControlManager.OnInput_Options` (o mesmo do `Esc`), e
  desliga as ações do jogo enquanto a janela está aberta. Desligar e religar
  as 33 ações no menu principal funcionou.
- Confirmado no jogo: na partida, disparar `OnInput_Options` abre a pausa
  (`timeScale` 0) e dispará-lo de novo ao fechar a janela a fecha
  (`timeScale` 1); com as ações desligadas, os cliques na janela não
  acertam a pausa e o jogo não reage ao mouse nem ao teclado.
- Observado: na partida, o jogo também lê direto `W`, `A`, `S`, `D`,
  `Shift`, `Ctrl` e os botões do mouse, fora das ações; o ItemHighlight lê
  `H` e `J` pelo Input antigo. Essas leituras não são afetadas por desligar
  as ações.
- `F10` é `UI/SaveGame` no jogo. A tecla do menu passa a ser `F6`, livre no
  levantamento.
- O jogo lê quase todas as teclas por ações do Input System
  (`PlayerInputMap`, mapas `Player`, `UI` e `System`); desligar as ações é o
  caminho provável para isolar a janela.
- O MelonPreferences lista só as entradas registradas; as órfãs do arquivo
  não aparecem.

Critério: `docs/teclas-do-jogo.md` escrito; saber quais controles
funcionam, como bloquear a entrada do jogo e qual técnica de desenho usar;
se for preciso uGUI, revisar o plano.

### Fase 2 — Mod ModSettings

- [x] Projeto `mods/ModSettings` na solução, com `OpenKey`.
- [x] Descoberta das categorias e entradas e controles por tipo.
- [x] Janela com lista de mods, opções com rolagem, gravação na hora e
  `Padrão`.
- [x] Cursor e bloqueio da entrada do jogo enquanto aberta.
- [x] Isolamento de falhas e log sem inundação.

Critério: build sem erros e critérios 1, 2, 3, 5 e 6 validados no jogo.

### Fase 3 — Teclas do ItemHighlight na hora

- [x] `LabelsKey` e `GlowKey` recriam a tecla quando o valor muda.
- [x] CHANGELOG do ItemHighlight.

Critério: critério 4 validado no jogo.

### Fase 4 — Idioma do jogo

- [x] Confirmar no jogo os valores de `languageCurrent` (português e
  inglês) e se ele já está definido quando os mods iniciam.
- [x] Verificar se `InteractiveItem.DisplayName` já vem traduzido pelo jogo.
- [x] Traduções em cada mod (ModSettings, ItemHighlight, NewTalkMarker,
  RelationshipHints), aplicadas ao iniciar e ao trocar o idioma.
- [x] Letras do RelationshipHints por idioma; READMEs e CHANGELOG dele.

Resultados (2026-10-12):

- Confirmado no jogo: `languageCurrent` vale `"English"` ao iniciar e nas
  aberturas e passa a `"Portuguese (Brazil)"` quando o menu principal
  carrega. Os mods releem o valor a cada segundo, o que também acompanha a
  troca de idioma. `GetLanguages()` lista os 10 idiomas além do inglês.
- Confirmado no jogo: `InteractiveItem.DisplayName` vem em inglês, e
  `TranslationManager.TranslateByText(nome, idioma)` devolve a tradução do
  jogo ("Flask" → "Cantil", "Sofa (L)" → "Sofá (E)") ou o próprio nome
  quando não há tradução. O ItemHighlight usa isso, então os nomes dos
  itens seguem qualquer idioma do jogo.
- Confirmado no jogo: com o jogo em português, a janela e os nomes e
  descrições das opções aparecem em português.
- Confirmado no jogo: trocar o idioma para inglês e de volta para português
  no menu principal atualiza a janela e as opções sem reiniciar. Na
  partida, conferido em seguida.
- Confirmado no jogo (usuário): partidas em inglês e em português com as
  letras e os nomes dos itens no idioma certo; gravação na hora (log:
  "Salvo: Distância máxima (metros) = 5", "Salvo: Tecla dos nomes = K");
  número inválido em vermelho sem gravar; tecla nova valendo na hora; pausa
  aberta e fechada junto com a janela.

Critério: build sem erros e critérios 8 e 9 validados no jogo.

### Fase 5 — Documentação

- [x] README do mod em português e inglês e CHANGELOG.
- [x] READMEs da raiz: linha na tabela e menção ao menu junto de
  `MelonPreferences.cfg`.
- [x] `IDEIAS.md`: status da ideia e o que foi confirmado no jogo.

Critério: documentação revisada e build final sem erros.
