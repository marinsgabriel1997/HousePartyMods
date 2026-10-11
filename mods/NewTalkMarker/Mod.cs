using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppEekCharacterEngine;
using Il2CppEekCharacterEngine.Canvas;
using Il2CppEekCharacterEngine.Components;
using Il2CppEekEvents;
using Il2CppEekEvents.Dialogues;
using Il2CppEekEvents.Stories;
using Il2CppEekUI;
using MelonLoader;
using UnityEngine;
using Il2CppCriteriaList = Il2CppSystem.Collections.Generic.List<Il2CppEekEvents.Criteria>;
using Il2CppIResponseList = Il2CppSystem.Collections.Generic.List<Il2CppEekEvents.IResponse>;
using Il2CppResponseList = Il2CppSystem.Collections.Generic.List<Il2CppEekEvents.Dialogues.Response>;

[assembly: MelonInfo(typeof(NewTalkMarker.Mod), "NewTalkMarker", "0.1.0", "gabriel")]
[assembly: MelonGame("Eek", "House Party")]

namespace NewTalkMarker
{
    // Mostra um "!" sobre o personagem quando surge uma fala que antes não
    // estava disponível para ele: amarelo (nova) e verde (vista no menu, mas
    // não escolhida), um "!" por estado presente.
    public class Mod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        private static MelonPreferences_Entry<bool> _diagnosticLog;
        internal static MelonPreferences_Entry<float> MaxDistance;

        internal static bool DiagnosticEnabled => _diagnosticLog?.Value ?? false;

        internal static void Diag(string message)
        {
            if (DiagnosticEnabled)
                Log.Msg(message);
        }

        private static readonly HashSet<string> _reportedErrors = new();

        // Registra cada erro uma vez por local, sem inundar o log a cada quadro.
        internal static void ReportError(string where, Exception ex)
        {
            if (_reportedErrors.Add($"{where}:{ex.GetType().Name}"))
                Log.Error($"Erro em {where} (o jogo segue normalmente): {ex}");
        }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            MelonPreferences_Category category = MelonPreferences.CreateCategory("NewTalkMarker");
            MaxDistance = category.CreateEntry("MaxDistance", 25f,
                description: "Distância máxima (metros) para mostrar o \"!\" sobre um personagem.");
            _diagnosticLog = category.CreateEntry("DiagnosticLog", false,
                description: "Registra no log cada mudança de marca, com a fala e os critérios dela.");
            SaveCycle.Init();
            Log.Msg("Carregado.");
        }

        public override void OnUpdate()
        {
            try { Scanner.Tick(); }
            catch (Exception ex) { ReportError(nameof(Scanner), ex); }
        }

        public override void OnGUI()
        {
            try { Marker.Draw(); }
            catch (Exception ex) { ReportError(nameof(Marker), ex); }
        }
    }

    // Marca de uma fala nova.
    internal enum Mark { None, Seen, New }

    // Estados presentes num personagem; cada um mostra sua exclamação.
    [Flags]
    internal enum CharacterMarks { None = 0, New = 1, Seen = 2 }

    // Reavalia um personagem por quadro, para não pesar num quadro só.
    internal static class Scanner
    {
        // Depois de carregar, o jogo leva um tempo para restaurar os valores do save.
        private const float SettleSeconds = 5f;

        // Critérios que dependem do momento (onde as pessoas estão, o que estão
        // fazendo), não da história. Ignorados para o "!" não piscar.
        private static readonly HashSet<CompareTypes> Transient = new()
        {
            CompareTypes.InVicinity,
            CompareTypes.Vision,
            CompareTypes.InVicinityAndVision,
            CompareTypes.IsOnlyInVicinityOf,
            CompareTypes.IsOnlyInVisionOf,
            CompareTypes.IsOnlyInVicinityAndVisionOf,
            CompareTypes.IsBeingSpokenTo,
            CompareTypes.Posing,
            CompareTypes.Distance,
            CompareTypes.IsInFrontOf,
            CompareTypes.IsCurrentlyUsing,
            CompareTypes.IsCurrentlyBeingUsed,
            CompareTypes.InZone,
            CompareTypes.SameZoneAs,
            CompareTypes.IsZoneEmpty,
            CompareTypes.Door,
            CompareTypes.CoinFlip,
            CompareTypes.CutScene,
            CompareTypes.ScreenFadeVisible,
            // Estado físico do personagem (dançando, sentado...).
            CompareTypes.State,
        };

        // Observado no jogo: piscam com o que o personagem está fazendo.
        private static bool IsTransient(Criteria c)
        {
            if (Transient.Contains(c.CompareType))
                return true;
            if (c.CompareType == CompareTypes.Value && c.Key == "CurrentMoveTarget")
                return true;
            return c.CompareType == CompareTypes.CriteriaGroup
                && c.Value != null && c.Value.StartsWith("NotCurrentlyOccupied", StringComparison.Ordinal);
        }

        // Falas que o jogador já conhecia: disponíveis no ponto de partida ou já
        // escolhidas. Nunca marcam.
        private static readonly HashSet<string> Known = new();

        // Marca de cada fala nova, pela chave estável.
        private static readonly Dictionary<string, Mark> Marks = new();

        // Histórias que já passaram pelo ponto de partida nesta partida.
        private static readonly HashSet<string> Baselined = new();

        // Chave de cada objeto Response da partida atual, para reconhecer as
        // falas exibidas no menu. O jogo recria os objetos ao carregar.
        private static readonly Dictionary<IntPtr, string> KeyByPointer = new();

        // Marcas exibidas por personagem, pelo nome.
        public static readonly Dictionary<string, CharacterMarks> ByCharacter = new();

        // O que a conversa com um personagem libera para ele mesmo o jogador já
        // vê nela; não marca. A avaliação continua um pouco depois do fim da
        // conversa, porque os efeitos dela podem chegar depois.
        private const float ConversationGraceSeconds = 3f;
        private static string _talkingTo;
        private static float _talkingUntil;

        // Falas exibidas no menu durante a conversa atual.
        private static readonly HashSet<string> ShownInConversation = new();

        private static bool _inGame;
        private static float _resumeAt;
        private static int _next;

        // Carregamento: o jogo restaura os valores do save só no fim
        // (OnSaveGameLoaded, ~20 s depois de Load). Até lá nada é avaliado, e o
        // estado lido do arquivo do mod espera para ser aplicado.
        private const float LoadTimeoutSeconds = 120f;
        private static bool _loading;
        private static float _loadDeadline;
        private static SavedState _pending;

        public static void BeginLoad(SavedState state)
        {
            Reset();
            _pending = state;
            _loading = true;
            _loadDeadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
        }

        public static void OnSaveGameLoaded()
        {
            if (!_loading)
                return;
            _loading = false;
            _resumeAt = Time.realtimeSinceStartup + SettleSeconds;
        }

        // Estado atual para gravar com o save. Se o estado lido ainda não foi
        // aplicado, é ele que vale.
        public static SavedState Snapshot()
        {
            if (_pending != null)
                return _pending;
            var state = new SavedState
            {
                Baselined = new List<string>(Baselined),
                Known = new List<string>(Known),
            };
            foreach (var pair in Marks)
                (pair.Value == Mark.New ? state.New : state.Seen).Add(pair.Key);
            return state;
        }

        private static void Apply(SavedState state)
        {
            Baselined.UnionWith(state.Baselined ?? new());
            Known.UnionWith(state.Known ?? new());
            foreach (string key in state.New ?? new())
                Marks[key] = Mark.New;
            foreach (string key in state.Seen ?? new())
                Marks[key] = Mark.Seen;

            var names = new HashSet<string>();
            foreach (string key in Marks.Keys)
                names.Add(key.Substring(0, key.IndexOf('|')));
            foreach (string name in names)
                Refresh(name);
            Mod.Diag($"estado do save aplicado: {Baselined.Count} personagens, {Known.Count} conhecidas, {Marks.Count} marcas");
        }

        public static void OnConversation(string name)
        {
            if (string.IsNullOrEmpty(name) || name == _talkingTo)
                return;
            _talkingTo = name;
            _talkingUntil = Time.realtimeSinceStartup + ConversationGraceSeconds;
            Mod.Diag($"conversa com {name}");
        }

        public static void Tick()
        {
            if (PlayerCharacter.Player == null)
            {
                // Fora da partida (menu, carregamento). Ao carregar, o jogo recria
                // os dados; nada do que foi calculado vale mais.
                if (_inGame)
                    Reset();
                return;
            }

            if (!_inGame)
            {
                _inGame = true;
                _resumeAt = Time.realtimeSinceStartup + SettleSeconds;
                _next = 0;
            }
            if (_loading)
            {
                if (Time.realtimeSinceStartup < _loadDeadline)
                    return;
                Mod.Log.Warning("O jogo não avisou o fim do carregamento; as marcas seguem com o estado lido.");
                OnSaveGameLoaded();
            }
            if (Time.realtimeSinceStartup < _resumeAt)
                return;

            if (_pending != null)
            {
                Apply(_pending);
                _pending = null;
            }

            // O personagem da conversa é reavaliado a cada quadro, para nada que
            // ela libere escapar para a avaliação seguinte, fora da conversa.
            if (_talkingTo != null)
            {
                if (CDialogue.CurrentlyOnDisplay != null)
                    _talkingUntil = Time.realtimeSinceStartup + ConversationGraceSeconds;
                if (Time.realtimeSinceStartup <= _talkingUntil)
                {
                    CharacterStoryData talking = GameManager.GetCharacterStory(_talkingTo);
                    if (talking != null)
                        Update(talking);
                }
                else
                {
                    Mod.Diag($"fim da conversa com {_talkingTo}");
                    _talkingTo = null;
                    ShownInConversation.Clear();
                }
            }

            var stories = GameManager.GetCharacterStories();
            if (stories == null || stories.Count == 0)
                return;

            if (_next >= stories.Count)
                _next = 0;
            CharacterStoryData story = stories[_next++];
            if (story == null || string.IsNullOrEmpty(story.CharacterName))
                return;

            Update(story);
        }

        private static void Reset()
        {
            _inGame = false;
            _talkingTo = null;
            ShownInConversation.Clear();
            Known.Clear();
            Marks.Clear();
            Baselined.Clear();
            KeyByPointer.Clear();
            ByCharacter.Clear();
        }

        private static void Update(CharacterStoryData story)
        {
            string name = story.CharacterName;
            bool baseline = Baselined.Add(name);
            bool own = name == _talkingTo;

            Visit(name, "g", story.GlobalResponses, baseline, own);
            Dialogue start = FindDialogue(story, story.DialogueID);
            if (start != null)
                Visit(name, $"d{start.ID}", start.Responses, baseline, own);

            if (baseline)
                Mod.Diag($"{name}: ponto de partida registrado");
            Refresh(name);
        }

        private static Dialogue FindDialogue(CharacterStoryData story, int id)
        {
            var dialogues = story.Dialogues;
            for (int i = 0; i < (dialogues?.Count ?? 0); i++)
                if (dialogues[i] != null && dialogues[i].ID == id)
                    return dialogues[i];
            return null;
        }

        // Chave estável: personagem, origem e Id da resposta (ou a posição, se não
        // houver Id). Nunca o texto, que muda com a tradução.
        private static void Visit(string name, string origin, Il2CppResponseList responses, bool baseline, bool own)
        {
            for (int i = 0; i < (responses?.Count ?? 0); i++)
            {
                Response response = responses[i];
                if (response == null)
                    continue;

                string id = response.Id;
                string key = $"{name}|{origin}|{(string.IsNullOrEmpty(id) ? "#" + i : id)}";
                KeyByPointer[response.Pointer] = key;

                if (Known.Contains(key))
                    continue;

                if (response.Selected)
                {
                    Known.Add(key);
                    SetMark(key, Mark.None, response, "escolhida");
                    continue;
                }

                bool available = IsAvailable(response.ResponseCriteria);

                // Ponto de partida: o que já vale é conhecido.
                if (baseline)
                {
                    if (available)
                        Known.Add(key);
                    continue;
                }

                Mark current = Marks.TryGetValue(key, out Mark m) ? m : Mark.None;

                // Liberada pela conversa com o próprio personagem: se apareceu no
                // menu dessa conversa, é conhecida; senão, aguarda o fim dela e,
                // se não aparecer até lá, marca normalmente.
                if (own && current == Mark.None && available)
                {
                    if (ShownInConversation.Contains(key))
                    {
                        Known.Add(key);
                        Mod.Diag($"{key}: conhecida (vista na conversa) \"{response.Text}\"");
                    }
                    continue;
                }

                Mark next = !available ? Mark.None : current == Mark.Seen ? Mark.Seen : Mark.New;
                SetMark(key, next, response, available ? "disponível" : "indisponível");
            }
        }

        private static void SetMark(string key, Mark next, Response response, string reason)
        {
            Mark current = Marks.TryGetValue(key, out Mark m) ? m : Mark.None;
            if (next == current)
                return;

            if (next == Mark.None)
                Marks.Remove(key);
            else
                Marks[key] = next;

            if (Mod.DiagnosticEnabled)
                Mod.Diag($"{key}: {current} -> {next} ({reason}) \"{response?.Text}\" critérios: {DescribeCriteria(response?.ResponseCriteria)}");
        }

        // Recalcula as marcas exibidas do personagem a partir das falas dele.
        private static void Refresh(string name)
        {
            string prefix = name + "|";
            CharacterMarks marks = CharacterMarks.None;
            foreach (var pair in Marks)
            {
                if (!pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (pair.Value == Mark.New)
                    marks |= CharacterMarks.New;
                else if (pair.Value == Mark.Seen)
                    marks |= CharacterMarks.Seen;
            }

            if (marks == CharacterMarks.None)
                ByCharacter.Remove(name);
            else
                ByCharacter[name] = marks;
        }

        // Chamado com as falas exibidas no menu: as novas passam a vistas.
        public static void OnMenuShown(Il2CppIResponseList shown)
        {
            var touched = new HashSet<string>();
            for (int i = 0; i < shown.Count; i++)
            {
                Response response = shown[i]?.TryCast<Response>();
                if (response == null || !KeyByPointer.TryGetValue(response.Pointer, out string key))
                    continue;
                ShownInConversation.Add(key);
                if (Marks.TryGetValue(key, out Mark mark) && mark == Mark.New)
                {
                    SetMark(key, Mark.Seen, response, "exibida no menu");
                    touched.Add(key.Substring(0, key.IndexOf('|')));
                }
            }
            foreach (string name in touched)
                Refresh(name);
        }

        // Avalia só os critérios duradouros, com o avaliador do próprio jogo.
        private static bool IsAvailable(Il2CppCriteriaList criteria)
        {
            var lasting = new Il2CppCriteriaList();
            for (int i = 0; i < (criteria?.Count ?? 0); i++)
                if (criteria[i] != null && !IsTransient(criteria[i]))
                    lasting.Add(criteria[i]);

            return lasting.Count == 0 || CriteriaEvaluator.MeetsCritera(lasting);
        }

        private static string DescribeCriteria(Il2CppCriteriaList criteria)
        {
            if (criteria == null || criteria.Count == 0)
                return "(nenhum)";
            var parts = new List<string>();
            for (int i = 0; i < criteria.Count; i++)
            {
                Criteria c = criteria[i];
                if (c == null)
                    continue;
                string ignored = IsTransient(c) ? " (ignorado)" : "";
                parts.Add($"{c.CompareType} {c.Character} {c.Key} {c.Value} {c.BoolValue}{ignored}".Replace("  ", " "));
            }
            return string.Join("; ", parts);
        }
    }

    // O menu de respostas exibido ao jogador. Mesmo ponto usado pelo RelationshipHints.
    [HarmonyPatch(typeof(DialogueUI), nameof(DialogueUI.OnNewResponses))]
    internal static class OnNewResponsesPatch
    {
        private static void Postfix(DialogueUI __instance, Il2CppIResponseList responseList)
        {
            try
            {
                if (Guard.Is<DialogueUI>(__instance) && responseList != null)
                    Scanner.OnMenuShown(responseList);
            }
            catch (Exception ex) { Mod.ReportError(nameof(OnNewResponsesPatch), ex); }
        }
    }

    // Início de cada diálogo exibido, com o personagem da conversa.
    [HarmonyPatch(typeof(CDialogue), nameof(CDialogue.DisplayDialogue), typeof(IDialogue))]
    internal static class DisplayDialoguePatch
    {
        private static void Prefix(CDialogue __instance)
        {
            try
            {
                if (Guard.Is<CDialogue>(__instance))
                    Scanner.OnConversation(__instance.Character?.Name);
            }
            catch (Exception ex) { Mod.ReportError(nameof(DisplayDialoguePatch), ex); }
        }
    }

    [HarmonyPatch(typeof(CDialogue), nameof(CDialogue.DisplayDialogue), typeof(int))]
    internal static class DisplayDialogueByIdPatch
    {
        private static void Prefix(CDialogue __instance)
        {
            try
            {
                if (Guard.Is<CDialogue>(__instance))
                    Scanner.OnConversation(__instance.Character?.Name);
            }
            catch (Exception ex) { Mod.ReportError(nameof(DisplayDialogueByIdPatch), ex); }
        }
    }

    // Desenha o "!" na tela, sobre a cabeça de cada personagem marcado.
    internal static class Marker
    {
        private const float AboveHead = 0.35f;
        private static GUIStyle _new;
        private static GUIStyle _seen;
        private static GUIStyle _shadow;
        private static readonly List<GUIStyle> _toDraw = new();

        public static void Draw()
        {
            if (Event.current.type != EventType.Repaint || Scanner.ByCharacter.Count == 0)
                return;
            if (Time.timeScale == 0f || CanvasEvents.IsPauseWhenOpenActive() || CDialogue.CurrentlyOnDisplay != null)
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            EnsureStyles();
            Vector3 cameraPos = camera.transform.position;
            float maxDistance = Mod.MaxDistance.Value;

            foreach (var pair in Scanner.ByCharacter)
            {
                // Histórias sem personagem no mundo (telefone, podcast) não têm onde mostrar o "!".
                NonPlayerCharacter npc = CharacterManager.GetNonPlayerCharacter(pair.Key);
                if (npc == null || !npc.gameObject.activeInHierarchy)
                    continue;

                Transform head = npc.Head;
                Vector3 world = (head != null ? head.position : npc.transform.position + Vector3.up * 1.7f) + Vector3.up * AboveHead;
                float distance = Vector3.Distance(cameraPos, world);
                if (distance > maxDistance)
                    continue;

                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                    continue;

                // Menor com a distância, dentro de um limite legível.
                int size = Mathf.Clamp((int)(60f / Mathf.Max(distance, 1f) * 4f), 18, 48);
                // Uma exclamação por estado, sempre na mesma ordem: amarelo, verde.
                _toDraw.Clear();
                if ((pair.Value & CharacterMarks.New) != 0)
                    _toDraw.Add(_new);
                if ((pair.Value & CharacterMarks.Seen) != 0)
                    _toDraw.Add(_seen);

                float step = size * 0.6f;
                float left = screen.x - step * (_toDraw.Count - 1) / 2f;
                _shadow.fontSize = size;
                for (int i = 0; i < _toDraw.Count; i++)
                {
                    GUIStyle style = _toDraw[i];
                    style.fontSize = size;

                    // GUI tem a origem no canto superior; a tela, no inferior.
                    var rect = new Rect(left + step * i - 30f, Screen.height - screen.y - size, 60f, size * 1.2f);
                    var offset = new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height);
                    GUI.Label(offset, "!", _shadow);
                    GUI.Label(rect, "!", style);
                }
            }
        }

        private static void EnsureStyles()
        {
            if (_new != null)
                return;
            // O construtor de cópia do GUIStyle não existe no jogo (removido pelo IL2CPP).
            _new = NewStyle(new Color(1f, 0.85f, 0.1f));
            _seen = NewStyle(new Color(0.3f, 0.9f, 0.3f));
            _shadow = NewStyle(new Color(0f, 0f, 0f, 0.8f));
        }

        private static GUIStyle NewStyle(Color color)
        {
            var style = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            style.normal.textColor = color;
            return style;
        }
    }
}
