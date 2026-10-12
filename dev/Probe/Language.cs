using System;
using System.Text;
using Il2CppEekCharacterEngine;
using Il2CppEekCharacterEngine.Interaction;
using UnityEngine;

namespace Probe
{
    // Fase 4 do p40: qual valor TranslationManager.languageCurrent tem, desde
    // quando, e se os nomes dos itens já vêm traduzidos pelo jogo.
    internal static class Language
    {
        private static string _last = "(não lido)";
        private static float _nextCheck;

        public static void Log(string when)
        {
            string current;
            try { current = TranslationManager.languageCurrent ?? "(nulo)"; }
            catch (Exception ex) { current = $"erro: {ex.GetType().Name}: {ex.Message}"; }
            if (current == _last && when == "quadro")
                return;
            _last = current;
            Mod.Log.Msg($"[idioma] {Time.realtimeSinceStartup:F1}s {when}: languageCurrent=\"{current}\"");
        }

        public static void Tick()
        {
            if (Time.realtimeSinceStartup < _nextCheck)
                return;
            _nextCheck = Time.realtimeSinceStartup + 1f;
            Log("quadro");
        }

        public static void Dump(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("== IDIOMA ==");
            report.AppendLine($"languageCurrent=\"{TranslationManager.languageCurrent}\"");
            try
            {
                var languages = TranslationManager.GetLanguages();
                var names = new StringBuilder();
                for (int i = 0; i < (languages?.Count ?? 0); i++)
                    names.Append($"\"{languages[i]}\" ");
                report.AppendLine($"idiomas: {names}");
            }
            catch (Exception ex) { report.AppendLine($"idiomas: erro {ex.Message}"); }
            foreach (var method in typeof(TranslationManager).GetMethods())
                if (method.Name == nameof(TranslationManager.TranslateByText))
                    report.AppendLine($"TranslateByText({string.Join(", ", Array.ConvertAll(method.GetParameters(), p => $"{p.ParameterType.Name} {p.Name}"))})");

            var items = ItemManager.GetAllItems();
            int shown = 0;
            for (int i = 0; i < (items?.Count ?? 0) && shown < 25; i++)
            {
                InteractiveItem item = items[i];
                if (item == null || !item.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(item.DisplayName))
                    continue;
                shown++;
                string translated;
                try { translated = TranslationManager.TranslateByText(item.DisplayName, TranslationManager.languageCurrent); }
                catch (Exception ex) { translated = $"erro: {ex.Message}"; }
                report.AppendLine($"  DisplayName=\"{item.DisplayName}\" TranslateByText=\"{translated}\" objeto={item.gameObject.name}");
            }
        }
    }
}
