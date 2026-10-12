# p70 — Dublagens localizadas (Dubbing)

## Objetivo

Um mod troca, na hora de tocar, as vozes em inglês das falas por dublagens
em outro idioma, lidas de arquivos `.ogg` fora do jogo. O jogador escolhe o
idioma entre as dublagens instaladas; fala sem dublagem toca o original. O
mod é independente (`mods/Dubbing`, DLL própria) e não altera bundles,
`.character`, arquivos de idioma nem saves.

## Contexto

Observado nos assemblies gerados e nos arquivos do jogo 1.5.2.14289, sem
teste no jogo:

- As vozes são `AudioClip` do Unity em bundles (dados FSB5), não FMOD
  Studio. Falas em `StreamingAssets/Base/Dialogue/English/
  base_dialogue_<story>_<personagem>`, clip
  `base_dialogue_<story>_<personagem>_<id>`; conversas de fundo em
  `Base/BackgroundChatter/English/base_backgroundchatter_<personagem>`.
  As regras de nomes de clips (variantes, takes extras, typos) estão em
  `house-party-dubbing/docs/adapters/house_party/README.md`.
- `CAudio` (por personagem) tem um caminho de substituição do áudio da fala:
  `AddDialogueOverrideClip(TTSResult)` (1 chamador),
  `OverrideDialogueAudio(TTSResult)` (público, 0 chamadores),
  `CheckDialogueAudioQueue`, `GetDialogueOverrideQueueCount` e o campo
  `_audioQueue`. `TTSResult` traz `Name`, `RequestId`, `SessionId` e `Clip`.
- Esse caminho atende a integração ElevenLabs (`Il2CppEekAI`):
  `SendDialogueTextForTextToSpeech` dispara `CAudio.OnTTSRequest`, e o
  resultado volta por `AddDialogueOverrideClip`. Os textos do jogo sugerem
  que isso ocorre quando falta o arquivo da fala ("File doesn't exist.
  Loading original story.. Sending for TTS"). Varrendo bundles, cenas e
  `*.assets` com UnityPy, não há instância de `AIManager`,
  `ElevenLabsData` (que guarda a chave) nem `VACreator`: a integração
  parece inativa no jogo publicado.
- A fala toca pela corrotina `CAudio.TryPlayDialogueAudio(IDialogue, int
  alternateId)` (campo `_clipID`), iniciada a partir de
  `OnDialogueStarted(CharacterBase, IDialogue, int)`.
  `CAudio.GetClipName(AudioDataTypes, int)` monta nomes de clip.
- Conversa de fundo com voz: `CAudio.PlayVoicedBackgroundChatter(
  BackgroundChatter, AudioClip)` recebe o clip pronto.
- Carregar áudio externo: `DownloadHandlerAudioClip(url, AudioType)` (OGG
  Vorbis decodificado pelo Unity), `AudioClip.Create` + `SetData` e
  `AssetBundle.LoadFromFile` existem nos assemblies gerados.
- A sincronia labial usa SALSA (`CAudio._salsa`). Hipótese: analisa o som
  tocado e acompanha qualquer clip.
- O `house-party-dubbing` (AGLE) gera `.ogg` por clip e hoje os injeta nos
  bundles; os arquivos deste mod seriam a mesma saída, sem a injeção.

Não se sabe se a fila de substituição funciona sem o `AIManager`, se
`TryPlayDialogueAudio` a consulta antes do bundle, o que o `TTSResult`
precisa trazer, como obter o nome do clip da fala no ponto do patch, se a
duração da fala e da legenda acompanham o clip novo, nem quanto custa
carregar um `.ogg` na hora.

## Escopo

Incluído:

- Falas de diálogo dos `.character` (base e variantes) e conversas de fundo
  com voz, das histórias que usam os bundles de `Base/Dialogue/English` e
  `Base/BackgroundChatter/English`.
- Dublagens em `UserData/Dubbing/<idioma>/<história>/<personagem>/`, um
  `.ogg` por clip com o nome do clip original (`<nome do clip>.ogg`). História
  e personagem usam os nomes das pastas e arquivos do jogo em
  `StreamingAssets/Mods/Stories/<história>/<personagem>.character`, por
  exemplo `UserData/Dubbing/pt-BR/Original Story/Amy/
  base_dialogue_originalstory_amy_12.ogg`. Conversas de fundo ficam na pasta
  do personagem que fala. O mod procura o arquivo só nesse caminho: trocar
  um áudio é substituir um arquivo. O mod lista as pastas de idioma
  presentes.
- Opção de idioma no MelonPreferences (aparece no ModSettings), com
  "desligado" como padrão; textos em português e inglês como nos outros
  mods.
- Fala sem arquivo na dublagem escolhida toca o original.
- Isolamento de falhas: arquivo ausente, corrompido ou erro no patch
  registra uma vez no log e a fala toca o original.
- README em português e inglês, CHANGELOG, linha nos READMEs da raiz,
  projeto na solução, `IDEIAS.md` e o `Il2CppEekAI` em
  `docs/investigacao-do-jogo.md`.

Fora do escopo:

- Falas soltas de bundles genéricos (peixe falante, `Britt`, motorista em
  `base_audio`/`base_cutscenes`), cenas e sons sem fala.
- Tradução de texto e legendas (o jogo já tem `Mods/Languages`).
- Gerar dublagens, chamar a ElevenLabs ou qualquer serviço externo.
- Empacotar as dublagens em bundle próprio, salvo se a fase 1 mostrar que
  arquivos soltos são inviáveis (nesse caso, revisar o plano).
- Mudanças no `house-party-dubbing`.

## Estado desejado

Com uma dublagem instalada e escolhida, ao conversar com um personagem a
fala toca no idioma da dublagem, com sincronia labial, sem atraso
perceptível nem som duplicado; a conversa só avança quando a fala dublada
termina, como no original. Falas sem dublagem tocam em inglês. Com a opção
desligada ou sem o mod, o jogo toca o original.

## Riscos e premissas

- A fila de substituição pode depender do `AIManager` ou de
  `RequestId`/`SessionId` coerentes; se não servir, o caminho alternativo é
  trocar o clip carregado do bundle antes de tocar, a definir na fase 1.
- Carregar `.ogg` na hora pode atrasar a fala; pode ser preciso carregar
  antes os arquivos do personagem ao iniciar o diálogo, com custo de
  memória.
- Dublagem mais longa ou mais curta que o original pode cortar a fala ou
  avançar a conversa antes do fim, se o jogo medir a duração pelo clip
  original ou pelo texto.
- `TryPlayDialogueAudio` é corrotina: o patch pode ter de ficar no
  `MoveNext` da classe gerada, que é frágil entre versões do jogo.
- Métodos nativos podem ser compartilhados entre classes (`p50`); patches
  conferem a classe real (`Guard`) e o endereço nativo.
- `AudioClip` criados pelo mod não devem ser guardados entre carregamentos
  de partida se o jogo os destruir; liberar os clips ao trocar de idioma ou
  de partida.
- Premissa: história, personagem e nome do clip identificam a fala; as
  irregularidades de nome são resolvidas por quem gera a dublagem.
- O mod precisa obter, no ponto do patch, o nome da história ativa e do
  personagem que fala iguais aos das pastas do jogo; se o jogo usar outros
  nomes (chave em minúsculas, nome de exibição), o mod converte, a definir
  na fase 1.
- Áudios de teste são gerados localmente (por exemplo, com `ffmpeg` a
  partir do original) e não são versionados.

## Critérios de aceite

- Com a dublagem escolhida, falas de diálogo com arquivo tocam o `.ogg`, com
  sincronia labial e sem o original por baixo; as sem arquivo tocam o
  original.
- Conversas de fundo com arquivo tocam o `.ogg`.
- Dublagem mais longa que o original toca até o fim antes de a conversa
  seguir; mais curta não deixa silêncio longo.
- Sem atraso perceptível entre a legenda e o início da fala.
- Trocar o idioma no ModSettings vale para a fala seguinte; desligar volta
  ao original.
- Substituir um `.ogg` na pasta vale, no máximo, a partir do carregamento
  seguinte da partida; arquivo na pasta de outro personagem ou história não
  é usado.
- Depois de carregar outro save e trocar de personagem, a dublagem continua.
- Arquivo corrompido não trava a conversa: toca o original e registra uma
  vez no log.
- Sem erros do mod no `MelonLoader\Latest.log`; bundles e arquivos do jogo
  inalterados.

## Fases

### Fase 1 — Investigação no jogo

- [x] Gerar `.ogg` de teste para algumas falas de um personagem e uma
      conversa de fundo, alguns mais longos e outros mais curtos que o
      original.
- [x] No `dev/Probe`, registrar `OnDialogueStarted`, `TryPlayDialogueAudio`
      (início e `MoveNext`), `CheckDialogueAudioQueue`,
      `SendDialogueTextForTextToSpeech`, `OverrideDialogueAudio`,
      `AddDialogueOverrideClip` e `PlayVoicedBackgroundChatter`, com o nome
      do clip, a história ativa, o personagem que fala, a fila e o
      `AudioSource` que toca; conferir métodos fundidos.
- [x] Carregar um `.ogg` por `DownloadHandlerAudioClip` e medir o tempo;
      comparar com `AudioClip.Create` + `SetData` se o primeiro falhar.
- [ ] Testar a fila: ao iniciar a fala, chamar `OverrideDialogueAudio` ou
      `AddDialogueOverrideClip` com o clip carregado; ver se toca no lugar
      do original, se o original some e o que o `TTSResult` precisa trazer.
      Não executado: a troca no `AudioSource` atendeu ao critério antes.
- [x] Se a fila não servir, testar trocar o clip antes de tocar (no
      carregamento do bundle ou no `AudioSource`).
- [x] Testar a conversa de fundo trocando o clip em
      `PlayVoicedBackgroundChatter`.
- [x] Conferir no jogo: sincronia labial, duração e avanço da conversa com
      clips longos e curtos, falas com variantes. Carregar outro save fica
      para os critérios de aceite da fase 2.

Resultados (2026-10-12, no log do Probe e confirmados pelo usuário no jogo,
com a Amy da Original Story; áudios de teste da voz do Windows e da
ElevenLabs em português):

- Nenhum dos métodos patcheados divide código nativo com outro método do
  `Il2CppEekCharacterEngine`.
- `OnDialogueStarted(character, dialogue, altId)` é chamado no `CAudio` de
  todos os personagens a cada fala; só o de `character` fala.
- A corrotina `_TryPlayDialogueAudio_d__95` dá dois passos: no primeiro
  (estado 1) espera o carregamento; no último (`MoveNext` devolve `false`)
  o clip já toca no `CAudio._audio`. `_clipID_5__2` é o nome do clip:
  `_dialoguePrefix` + `ID`, mais `.<altId>` nas variantes
  (`base_dialogue_originalstory_amy_0.1`).
- `GameManager.GetActiveStoryName()` devolve `Original Story` e o
  `gameObject.name` do personagem é `Amy`: batem com as pastas de
  `Mods/Stories`. Outras histórias não foram conferidas.
- Funciona: no postfix do último `MoveNext`, trocar `_audio.clip` pelo clip
  externo e chamar `Play()`. A troca ocorre no mesmo quadro em que o original
  começa (4 a 10 ms depois); o original não foi ouvido (confirmado pelo
  usuário). A conversa espera o clip novo, longo ou curto, como no original,
  e avançar interrompe o áudio.
- A boca acompanha o clip novo sem mexer no SALSA: o `Salsa3D` usa o mesmo
  `AudioSource` e mediu a mesma abertura (`average` máximo de 0,010 a 0,013)
  com o original, só trocando o clip, pondo o clip em `audioClip` ou por
  `SetAudioClip` (confirmado pelo usuário, depois de aumentar o brilho).
- Conversa de fundo: trocar o parâmetro `clip` no prefixo de
  `PlayVoicedBackgroundChatter` toca o externo (confirmado pelo usuário).
- `DownloadHandlerAudioClip` com `file://` carrega um `.ogg` de 2 a 17 s em
  3 a 10 ms, bloqueando no mesmo quadro, sem travada percebida. Com
  `compressed = true`, o clip ainda vem `DecompressOnLoad`. Carregar antes
  os 366 arquivos da Amy levou ~16 s e ~515 MB em PCM: descartado; o mod
  carrega cada fala na hora.

Fica sem verificação: a fila de substituição do TTS, outras histórias e
personagens com nomes compostos (`Phone Call`).

Critério de conclusão: um mecanismo confirmado no jogo que toca um `.ogg`
externo no lugar de uma fala de diálogo e de uma conversa de fundo,
achando o arquivo pela história, pelo personagem e pelo nome do clip, com
sincronia labial e a conversa
seguindo a duração do clip novo, e o custo de carregamento conhecido.

### Fase 2 — Mod

- [x] Criar `mods/Dubbing` com o mecanismo da fase 1, a escolha de idioma,
      a volta ao original e o isolamento de falhas.
- [x] Adicionar à solução e compilar sem avisos.
- [x] Teste no jogo, só com o mod e os demais mods do repositório: os
      critérios de aceite. Aceito pelo usuário (2026-10-12) sem o Probe,
      sem confirmação item a item.

Resultados (2026-10-12, com os 9 `.ogg` de teste da Amy num jogo cujos
bundles já têm a dublagem injetada pelo AGLE):

- No log: o arquivo corrompido (fala 133) foi recusado com um aviso e a
  conversa seguiu; carregar o mesmo save de novo e o autosave, e desligar a
  dublagem no ModSettings, sem erros do mod.
- Sem registro no log, por não haver mensagem de sucesso: as falas trocadas,
  a conversa de fundo, religar a dublagem e outros personagens. Ficam como
  aceitos pelo usuário.

Critério de conclusão: critérios de aceite confirmados pelo usuário no
jogo.

### Fase 2.5 — Validação com a dublagem completa

Os bundles do jogo de teste já têm a dublagem injetada pelo AGLE, e o banco
do AGLE tem 7.666 dublagens `pt-br` ativas (PCM 24 kHz mono, com história,
personagem e nome do clip). A validação usa essa dublagem pelo mod, com os
bundles originais. A exportação é um script temporário, fora dos dois
repositórios, que só lê o banco; não é recurso do mod nem do AGLE.

- [x] Backup dos bundles alterados pelo AGLE (`Base/Dialogue/English`,
      `Base/BackgroundChatter/English`, `base_audio` e
      `explicitcontent_cutscenes`, com os `.manifest`) em
      `Documents/HousePartyBackups/bundles-dublados-2026-10-12`, com
      `SHA256SUMS.txt`; 80 arquivos, conferidos com os do jogo.
- [x] Usuário restaura os arquivos originais pela Steam (verificar
      integridade). Os 38 bundles alterados pelo AGLE passaram a diferir do
      backup; os `.manifest` e os índices `English` não tinham sido
      alterados.
- [x] Exportar as dublagens ativas para
      `UserData/Dubbing/pt-BR/<história>/<personagem>/<clip>.ogg`, sem as
      falas soltas (Sancho, Heckler). 7.607 arquivos, 678 MB (Vorbis
      q4), sem erros nem nomes repetidos; ficam de fora 39 falas da
      `Phone Call` sem clip no banco.
- [x] Teste no jogo: diálogos e conversas de fundo de vários personagens e
      das três histórias, `Phone Call`, travadas ao começar as falas e erros
      no log. O usuário confirmou (2026-10-12) que ligar e desligar a
      dublagem no ModSettings alterna a fala entre o inglês e o pt-BR; no
      log, o mod carregou com `pt-BR` e não registrou erros. Ficam sem
      verificação, por decisão do usuário de fechar o plano: outras
      histórias, conversas de fundo e outros personagens com a dublagem
      completa, travadas e a `Phone Call`, que só fala no fim de caminhos
      longos da história.

Critério de conclusão: com os bundles originais, as falas e conversas de
fundo dubladas tocam pelo mod, confirmadas pelo usuário no jogo. Atendido
para as falas de diálogo; a conversa de fundo foi confirmada só com os
áudios de teste da fase 2.

O backup dos bundles dublados fica com o usuário, fora do repositório. Falas
sem clip original (o banco do AGLE tem 39 dubladas sem clip) ficam
registradas no `IDEIAS.md` como pendentes de investigação.

### Fase 3 — Documentação

- [x] README em português e inglês (formato e local dos arquivos de
      dublagem), CHANGELOG `[Unreleased]`, linha nos READMEs da raiz,
      `IDEIAS.md` e o `Il2CppEekAI` em `docs/investigacao-do-jogo.md`
      (também o `Il2CppSALSA`). O mod não usa tecla; `docs/teclas-do-jogo.md`
      não muda.

Critério de conclusão: documentação equivalente nos dois idiomas.
