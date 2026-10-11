using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Il2CppEekCharacterEngine;
using Il2CppEekCharacterEngine.Interaction;
using Il2CppEekCharacterEngine.Components;
using Il2CppEekEvents;
using Il2CppEekEvents.Dialogues;
using Il2CppEekEvents.Stories;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(Probe.Mod), "Probe", "0.1.0", "gabriel")]
[assembly: MelonGame("Eek", "House Party")]

namespace Probe
{
    // Sonda de desenvolvimento (não é publicada). F8 grava em UserData/Probe/
    // o que o jogo sabe sobre itens do cenário e falas liberadas, para avaliar
    // as ideias do IDEIAS.md com dados reais.
    public class Mod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        private static bool _legacyInputBroken;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Log.Msg("Carregado. F8 grava o relatório em UserData/Probe/.");
        }

        public override void OnLateInitializeMelon()
        {
            CDialogue.add_OnPlayerResponseSelected(new Action<string, IResponse>((talkingTo, response) =>
            {
                try { Watcher.OnChosen(talkingTo, response?.TryCast<Response>()); }
                catch (Exception ex) { Log.Error($"Erro ao registrar escolha: {ex}"); }
            }));
        }

        public override void OnUpdate()
        {
            try { Watcher.Tick(); }
            catch (Exception ex) { Log.Error($"Erro na vigia: {ex}"); }

            if (!F8Pressed())
                return;
            try { Dump(); }
            catch (Exception ex) { Log.Error($"Erro no relatório: {ex}"); }
        }

        // F9 é o salvamento rápido do jogo; por isso F8.
        // O jogo pode ter só o Input System novo ativo; nesse caso o Input
        // antigo lança exceção e passamos a usar o novo.
        private static bool F8Pressed()
        {
            if (!_legacyInputBroken)
            {
                try { return Input.GetKeyDown(KeyCode.F8); }
                catch (Exception ex)
                {
                    _legacyInputBroken = true;
                    Log.Msg($"Input antigo indisponível ({ex.GetType().Name}); usando o Input System.");
                }
            }
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.f8Key.wasPressedThisFrame;
        }

        private static void Dump()
        {
            var report = new StringBuilder();
            report.AppendLine($"Probe {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            DumpItems(report);
            DumpDialogues(report);

            string dir = Path.Combine(MelonEnvironment.UserDataDirectory, "Probe");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, $"probe-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(file, report.ToString());
            Log.Msg($"Relatório gravado em {file}");
        }

        // ---------- Ideia: destaque de coletáveis ----------

        private static Vector3? PlayerPosition()
        {
            PlayerCharacter player = PlayerCharacter.Player;
            return player == null ? null : player.transform.position;
        }

        private static string Describe(InteractiveItem item, StoryData story, Vector3? playerPos)
        {
            GameObject go = item.gameObject;
            string distance = playerPos.HasValue ? $"{Vector3.Distance(playerPos.Value, item.transform.position):0.0}m" : "?";
            string interactions = item.Interactions == null ? "" : string.Join(",", ToList(item.Interactions));
            bool hasInteractions = story != null && item.HasInteractions(story);
            return $"{go.name} | \"{item.DisplayName}\" | ativo={go.activeInHierarchy} render={item.IsRendererEnabled} " +
                   $"pegável={item.canBeGrabbed} inventário={item._inventoried} interações={hasInteractions} " +
                   $"[{interactions}] dist={distance} perto={item.InPlayerInteractionRange()}";
        }

        private static void DumpItems(StringBuilder report)
        {
            var items = ItemManager.GetAllItems();
            StoryData story = GameManager.GetActiveStory();
            Vector3? playerPos = PlayerPosition();

            int active = 0, visible = 0, withInteractions = 0, grabbable = 0, inventoried = 0;
            var lines = new List<string>();
            for (int i = 0; i < (items?.Count ?? 0); i++)
            {
                InteractiveItem item = items[i];
                if (item == null)
                    continue;
                try
                {
                    if (item.gameObject.activeInHierarchy) active++;
                    if (item.gameObject.activeInHierarchy && item.IsRendererEnabled) visible++;
                    if (story != null && item.HasInteractions(story)) withInteractions++;
                    if (item.canBeGrabbed) grabbable++;
                    if (item._inventoried) inventoried++;
                    lines.Add(Describe(item, story, playerPos));
                }
                catch (Exception ex)
                {
                    lines.Add($"#{i} erro: {ex.Message}");
                }
            }

            report.AppendLine();
            report.AppendLine("== ITENS ==");
            report.AppendLine($"total={items?.Count ?? 0} ativos={active} visíveis={visible} com interações={withInteractions} " +
                              $"pegáveis={grabbable} no inventário={inventoried}");
            lines.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string line in lines)
                report.AppendLine(line);
        }

        // ---------- Ideia: indicador de fala nova ----------

        private static void DumpDialogues(StringBuilder report)
        {
            var stories = GameManager.GetCharacterStories();
            int shownDialogues = 0, selectedResponses = 0;
            var body = new StringBuilder();

            for (int s = 0; s < (stories?.Count ?? 0); s++)
            {
                CharacterStoryData cs = stories[s];
                if (cs?.Dialogues == null)
                    continue;

                Dialogue start = null;
                for (int d = 0; d < cs.Dialogues.Count; d++)
                {
                    Dialogue dialogue = cs.Dialogues[d];
                    if (dialogue == null)
                        continue;
                    if (dialogue.Shown) shownDialogues++;
                    selectedResponses += CountSelected(dialogue.Responses);
                    if (dialogue.ID == cs.DialogueID)
                        start = dialogue;
                }
                selectedResponses += CountSelected(cs.GlobalResponses);

                body.AppendLine();
                body.AppendLine($"-- {cs.CharacterName}: diálogo inicial {cs.DialogueID} " +
                                (start == null ? "(não encontrado)" : $"visto={start.Shown} respostas={start.Responses?.Count ?? 0}"));
                if (start != null)
                    DumpResponses(body, "inicial", start.Responses);
                DumpResponses(body, "global", cs.GlobalResponses);
            }

            report.AppendLine();
            report.AppendLine("== DIÁLOGOS ==");
            report.AppendLine($"personagens={stories?.Count ?? 0} diálogos vistos={shownDialogues} respostas escolhidas={selectedResponses}");
            report.Append(body);
        }

        private static void DumpResponses(StringBuilder body, string kind, Il2CppSystem.Collections.Generic.List<Response> responses)
        {
            for (int r = 0; r < (responses?.Count ?? 0); r++)
            {
                Response response = responses[r];
                if (response == null)
                    continue;

                string passes;
                try
                {
                    bool hasCriteria = response.ResponseCriteria != null && response.ResponseCriteria.Count > 0;
                    passes = !hasCriteria ? "sem critério" : CriteriaEvaluator.MeetsCritera(response.ResponseCriteria) ? "passa" : "falha";
                }
                catch (Exception ex)
                {
                    passes = $"erro: {ex.Message}";
                }

                bool isNew = passes == "passa" && !response.Selected;
                body.AppendLine($"  {(isNew ? "NOVA " : "     ")}[{kind}] {passes,-12} escolhida={response.Selected} " +
                                $"Next={response.Next} {Shorten(response.Text)}");
            }
        }

        private static int CountSelected(Il2CppSystem.Collections.Generic.List<Response> responses)
        {
            int count = 0;
            for (int r = 0; r < (responses?.Count ?? 0); r++)
                if (responses[r]?.Selected == true) count++;
            return count;
        }

        private static List<string> ToList(Il2CppSystem.Collections.Generic.List<string> list)
        {
            var result = new List<string>();
            for (int i = 0; i < list.Count; i++)
                result.Add(list[i]);
            return result;
        }

        private static string Shorten(string text) =>
            text == null ? "" : text.Length <= 70 ? text : text.Substring(0, 67) + "...";
    }

    // Reavalia de tempos em tempos o critério das respostas (iniciais e
    // globais) de todos os personagens e registra o que passou a valer ou
    // deixou de valer, com o tempo gasto na avaliação.
    internal static class Watcher
    {
        private const float IntervalSeconds = 2f;

        // Depois de carregar, o jogo leva um tempo para restaurar os valores do
        // save; avaliar antes disso gera mudanças falsas.
        private const float SettleSeconds = 5f;

        private static float _nextCheck;
        private static bool _outOfGame = true;
        private static int _loads;
        private static readonly Dictionary<string, bool> _state = new();

        // Respostas escolhidas nesta sessão do jogo (personagem, objeto da resposta).
        private static readonly List<(string Speaker, Response Response)> _chosen = new();

        public static void OnChosen(string talkingTo, Response response)
        {
            if (response == null)
                return;
            _chosen.Add((talkingTo, response));
            Mod.Log.Msg($"ESCOLHEU {talkingTo}: \"{response.Text}\" (escolhida={response.Selected})");
        }

        public static void Tick()
        {
            if (Time.realtimeSinceStartup < _nextCheck)
                return;
            _nextCheck = Time.realtimeSinceStartup + IntervalSeconds;

            if (PlayerCharacter.Player == null)
            {
                // Fora da partida (menu, carregamento): recomeça do zero na próxima.
                _state.Clear();
                _outOfGame = true;
                return;
            }

            if (_outOfGame)
            {
                _outOfGame = false;
                _loads++;
                _nextCheck = Time.realtimeSinceStartup + SettleSeconds;
                return;
            }

            var stories = GameManager.GetCharacterStories();
            if (stories == null)
                return;

            bool firstPass = _state.Count == 0;
            if (firstPass)
                ReportChosenAfterLoad();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var changes = new List<string>();
            int evaluated = 0;

            for (int s = 0; s < stories.Count; s++)
            {
                CharacterStoryData cs = stories[s];
                if (cs == null)
                    continue;
                Check(cs, "global", cs.GlobalResponses, changes, ref evaluated);
                for (int d = 0; d < (cs.Dialogues?.Count ?? 0); d++)
                {
                    Dialogue dialogue = cs.Dialogues[d];
                    if (dialogue != null && dialogue.ID == cs.DialogueID)
                        Check(cs, $"inicial {dialogue.ID}", dialogue.Responses, changes, ref evaluated);
                }
            }
            watch.Stop();

            if (firstPass)
                Mod.Log.Msg($"Vigia: {evaluated} respostas avaliadas em {watch.Elapsed.TotalMilliseconds:0.0} ms.");
            foreach (string change in changes)
                Mod.Log.Msg(change);
            if (changes.Count > 0)
                Mod.Log.Msg($"  (avaliação levou {watch.Elapsed.TotalMilliseconds:0.0} ms)");
        }

        // Depois de um carregamento: as respostas escolhidas antes dele continuam
        // marcadas? Se o save é anterior à escolha e a marca continua, o jogo não
        // limpa a marca ao carregar.
        private static void ReportChosenAfterLoad()
        {
            Mod.Log.Msg($"PARTIDA #{_loads} (carregamento {_loads}): {_chosen.Count} respostas escolhidas antes nesta sessão do jogo.");
            foreach ((string speaker, Response response) in _chosen)
            {
                Response current = Find(speaker, response.Text);
                string state = current == null ? "não encontrada" :
                    $"escolhida={current.Selected} mesmo objeto={current.Pointer == response.Pointer}";
                Mod.Log.Msg($"  {speaker}: \"{response.Text}\" agora {state}");
            }
        }

        private static Response Find(string speaker, string text)
        {
            CharacterStoryData cs = GameManager.GetCharacterStory(speaker);
            if (cs == null)
                return null;
            Response found = FindIn(cs.GlobalResponses, text);
            for (int d = 0; found == null && d < (cs.Dialogues?.Count ?? 0); d++)
                found = FindIn(cs.Dialogues[d]?.Responses, text);
            return found;
        }

        private static Response FindIn(Il2CppSystem.Collections.Generic.List<Response> responses, string text)
        {
            for (int r = 0; r < (responses?.Count ?? 0); r++)
                if (responses[r] != null && responses[r].Text == text)
                    return responses[r];
            return null;
        }

        private static void Check(CharacterStoryData cs, string kind, Il2CppSystem.Collections.Generic.List<Response> responses,
            List<string> changes, ref int evaluated)
        {
            for (int r = 0; r < (responses?.Count ?? 0); r++)
            {
                Response response = responses[r];
                if (response == null)
                    continue;

                bool hasCriteria = response.ResponseCriteria != null && response.ResponseCriteria.Count > 0;
                bool passes = !hasCriteria || CriteriaEvaluator.MeetsCritera(response.ResponseCriteria);
                evaluated++;

                string key = $"{cs.CharacterName}|{kind}|{response.Id}|{response.Next}|{response.Text}";
                if (_state.TryGetValue(key, out bool before) && before != passes)
                    changes.Add($"{(passes ? "LIBEROU " : "BLOQUEOU")} {cs.CharacterName} [{kind}] escolhida={response.Selected} " +
                                $"Next={response.Next} \"{response.Text}\"");
                _state[key] = passes;
            }
        }
    }
}
