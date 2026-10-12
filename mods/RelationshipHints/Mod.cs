using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppEekCharacterEngine.Components;
using Il2CppEekEvents;
using Il2CppEekEvents.Dialogues;
using Il2CppEekEvents.Values;
using Il2CppEekUI;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Il2CppResponseList = Il2CppSystem.Collections.Generic.List<Il2CppEekEvents.IResponse>;

[assembly: MelonInfo(typeof(RelationshipHints.Mod), "RelationshipHints", "0.3.4", "gabriel")]
[assembly: MelonGame("Eek", "House Party")]

namespace RelationshipHints
{
    public class Mod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        private static MelonPreferences_Entry<bool> _diagnosticLog;

        internal static bool DiagnosticEnabled => _diagnosticLog?.Value ?? false;

        // Log de diagnóstico: previsto vs. real a cada escolha. Desligado por padrão.
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
            MelonPreferences_Category category = MelonPreferences.CreateCategory("RelationshipHints");
            _diagnosticLog = Lang.Entry(category.CreateEntry("DiagnosticLog", false),
                "Registro de diagnóstico", "Registra no log o efeito previsto de cada resposta e o compara com a mudança real de relação.",
                "Diagnostic log", "Logs the predicted effect of each reply and compares it with the actual relationship change (log in Portuguese).");
            Log.Msg("Carregado.");
        }

        public override void OnLateInitializeMelon()
        {
            ValueStore.add_OnRelationshipChanged(new Action<string, string, string, int, int>((name, with, type, oldValue, newValue) =>
            {
                try { Tracker.OnRelationshipChanged(name, with, type, oldValue, newValue); }
                catch (Exception ex) { ReportError(nameof(Tracker.OnRelationshipChanged), ex); }
            }));
        }

        public override void OnUpdate()
        {
            try { Lang.Tick(); }
            catch (Exception ex) { ReportError(nameof(Lang), ex); }
            Tracker.CheckTimeout();
        }
    }

    // Prefixos calculados para o menu atual, por resposta.
    internal static class MenuState
    {
        // Algumas respostas (como "...") não têm Id; nesse caso a chave é o
        // próprio objeto.
        public static string Key(Response response) =>
            string.IsNullOrEmpty(response.Id) ? $"ptr:{response.Pointer}" : response.Id;

        public static string Speaker;
        public static readonly Dictionary<string, Effect> Effects = new();

        // Último texto escrito pelo mod em cada componente, para não prefixar
        // duas vezes e para notar quando o jogo troca o texto.
        public static readonly Dictionary<IntPtr, string> Written = new();
    }

    [HarmonyPatch(typeof(DialogueUI), nameof(DialogueUI.OnNewResponses))]
    internal static class OnNewResponsesPatch
    {
        private static void Postfix(DialogueUI __instance, Il2CppResponseList responseList)
        {
            try { Run(__instance, responseList); }
            catch (Exception ex) { Mod.ReportError(nameof(OnNewResponsesPatch), ex); }
        }

        private static void Run(DialogueUI __instance, Il2CppResponseList responseList)
        {
            if (!Guard.Is<DialogueUI>(__instance) || responseList == null)
                return;

            Tracker.Finish("novo menu");

            string speaker = __instance.CurrentSpeaker?.Name;
            MenuState.Speaker = speaker;
            MenuState.Effects.Clear();
            MenuState.Written.Clear();

            Mod.Diag($"MENU {speaker}: {responseList.Count} respostas");
            for (int i = 0; i < responseList.Count; i++)
            {
                Response response = responseList[i]?.TryCast<Response>();
                if (response == null || speaker == null)
                    continue;

                Effect effect = EffectCalculator.Calculate(response, speaker);
                MenuState.Effects[MenuState.Key(response)] = effect;

                string label = effect.Format(speaker);
                Mod.Diag($"  [{i}] {(label.Length > 0 ? label : "(sem efeito)")}  Next={response.Next}  {Shorten(response.Text)}");
                foreach (string step in effect.Trace)
                    Mod.Diag($"        {step}");
            }
        }

        private static string Shorten(string text) =>
            text == null ? "" : text.Length <= 60 ? text : text.Substring(0, 57) + "...";
    }

    [HarmonyPatch(typeof(DialogueUI), nameof(DialogueUI.Update))]
    internal static class DialogueUIUpdatePatch
    {
        private static void Postfix(DialogueUI __instance)
        {
            try { Run(__instance); }
            catch (Exception ex) { Mod.ReportError(nameof(DialogueUIUpdatePatch), ex); }
        }

        private static void Run(DialogueUI __instance)
        {
            if (!Guard.Is<DialogueUI>(__instance) || MenuState.Effects.Count == 0)
                return;

            var texts = __instance.responseTexts;
            var responses = __instance.currentResponseList;
            if (texts == null || responses == null)
                return;

            for (int i = 0; i < texts.Count; i++)
            {
                Text label = texts[i];
                if (label == null)
                    continue;

                string current = label.text;
                if (string.IsNullOrEmpty(current))
                    continue;
                if (MenuState.Written.TryGetValue(label.Pointer, out string written) && written == current)
                    continue;

                if (!responses.ContainsKey(i))
                    continue;
                Response response = responses[i]?.TryCast<Response>();
                if (response == null || !MenuState.Effects.TryGetValue(MenuState.Key(response), out Effect effect))
                    continue;

                string speaker = MenuState.Speaker;
                string updated = current;
                string overlay = null;
                if (!effect.Format(speaker).Equals(string.Empty))
                {
                    updated = $"{effect.Format(speaker, FormatMode.Base)} | {current}";
                    overlay = $"{effect.Format(speaker, FormatMode.Overlay)}<color={Effect.Transparent}> | {current}</color>";
                    label.supportRichText = true;
                    label.text = updated;
                }
                ColorOverlay.Sync(label, overlay);
                MenuState.Written[label.Pointer] = updated;
            }
        }
    }

    // Diagnóstico: o que de fato acontece depois de escolher uma resposta.
    [HarmonyPatch(typeof(CDialogue), nameof(CDialogue.OnPlayerResponse))]
    internal static class OnPlayerResponsePatch
    {
        private static void Prefix(CDialogue __instance, IResponse response)
        {
            try { Run(__instance, response); }
            catch (Exception ex) { Mod.ReportError(nameof(OnPlayerResponsePatch), ex); }
        }

        private static void Run(CDialogue __instance, IResponse response)
        {
            if (!Guard.Is<CDialogue>(__instance))
                return;

            Response r = response?.TryCast<Response>();
            if (r == null)
                return;

            Tracker.Finish("nova escolha");
            if (!Mod.DiagnosticEnabled)
                return;
            MenuState.Effects.TryGetValue(MenuState.Key(r), out Effect predicted);
            Tracker.Start(r, MenuState.Speaker, predicted);
        }
    }

    [HarmonyPatch(typeof(CDialogue), nameof(CDialogue.DisplayDialogue), typeof(int))]
    internal static class DisplayDialogueByIdPatch
    {
        private static void Prefix(CDialogue __instance, int id)
        {
            try { Run(__instance, id); }
            catch (Exception ex) { Mod.ReportError(nameof(DisplayDialogueByIdPatch), ex); }
        }

        private static void Run(CDialogue __instance, int id)
        {
            if (Guard.Is<CDialogue>(__instance))
                Tracker.Note($"DisplayDialogue({id}) em {__instance.Character?.Name}");
        }
    }

    [HarmonyPatch(typeof(CDialogue), nameof(CDialogue.DisplayDialogue), typeof(IDialogue))]
    internal static class DisplayDialoguePatch
    {
        private static void Prefix(CDialogue __instance, IDialogue dia)
        {
            try { Run(__instance, dia); }
            catch (Exception ex) { Mod.ReportError(nameof(DisplayDialoguePatch), ex); }
        }

        private static void Run(CDialogue __instance, IDialogue dia)
        {
            if (!Guard.Is<CDialogue>(__instance))
                return;
            Dialogue d = dia?.TryCast<Dialogue>();
            Tracker.Note($"DisplayDialogue(#{d?.ID}) em {__instance.Character?.Name}");
        }
    }

    [HarmonyPatch(typeof(CDialogue), nameof(CDialogue.AssignDialogueCloseConsequences))]
    internal static class CloseConsequencesPatch
    {
        private static void Prefix(CDialogue __instance)
        {
            try { Run(__instance); }
            catch (Exception ex) { Mod.ReportError(nameof(CloseConsequencesPatch), ex); }
        }

        private static void Run(CDialogue __instance)
        {
            if (!Guard.Is<CDialogue>(__instance))
                return;
            Dialogue d = __instance._currentDialogue?.TryCast<Dialogue>();
            Tracker.Note($"CloseEvents de #{d?.ID} em {__instance.Character?.Name}");
        }
    }

    internal static class Tracker
    {
        private const float TimeoutSeconds = 10f;

        private static Response _response;
        private static string _speaker;
        private static Effect _predicted;
        private static float _startedAt;
        private static readonly List<string> _notes = new();
        private static readonly Dictionary<string, RelationDelta> _actual = new();

        public static void Start(Response response, string speaker, Effect predicted)
        {
            _response = response;
            _speaker = speaker;
            _predicted = predicted;
            _startedAt = Time.realtimeSinceStartup;
            _notes.Clear();
            _actual.Clear();
        }

        public static void Note(string note)
        {
            if (_response != null)
                _notes.Add($"{Time.realtimeSinceStartup - _startedAt:0.00}s {note}");
        }

        public static void OnRelationshipChanged(string name, string with, string type, int oldValue, int newValue)
        {
            if (with != "Player")
                return;

            Mod.Diag($"RELAÇÃO {name}/{with} {type}: {oldValue} -> {newValue}");
            if (_response == null)
                return;

            if (!_actual.TryGetValue(name, out RelationDelta delta))
                _actual[name] = delta = new RelationDelta();

            if (type == "Friendship")
                delta.Friendship += newValue - oldValue;
            else if (type == "Romance")
                delta.Romance += newValue - oldValue;
            Note($"relação {name} {type} {oldValue} -> {newValue}");
        }

        public static void CheckTimeout()
        {
            if (_response != null && Time.realtimeSinceStartup - _startedAt > TimeoutSeconds)
                Finish("tempo esgotado");
        }

        public static void Finish(string reason)
        {
            if (_response == null)
                return;

            var actual = new Effect();
            foreach (KeyValuePair<string, RelationDelta> pair in _actual)
            {
                actual.For(pair.Key).Friendship = pair.Value.Friendship;
                actual.For(pair.Key).Romance = pair.Value.Romance;
            }

            string predicted = _predicted?.Format(_speaker) ?? "(sem previsão)";
            string real = actual.Format(_speaker);
            bool uncertain = predicted.Contains('?') || predicted.Contains('=');
            string verdict = uncertain ? "INCERTO" : predicted == real ? "OK" : "DIVERGE";

            Mod.Diag($"ESCOLHA {_speaker} Next={_response.Next} \"{_response.Text}\" ({reason})");
            Mod.Diag($"  previsto: {(predicted.Length > 0 ? predicted : "(sem efeito)")}  real: {(real.Length > 0 ? real : "(sem efeito)")}  -> {verdict}");
            foreach (string note in _notes)
                Mod.Diag($"    {note}");

            _response = null;
            _predicted = null;
        }
    }
}
