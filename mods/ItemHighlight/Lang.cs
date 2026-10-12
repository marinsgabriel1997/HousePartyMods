using System;
using System.Collections.Generic;
using Il2CppEekCharacterEngine;
using MelonLoader;
using UnityEngine;

namespace ItemHighlight
{
    // Idioma dos textos do mod, conforme o idioma escolhido no jogo:
    // português com "Portuguese (Brazil)", inglês nos demais. Confirmado no
    // jogo: TranslationManager.languageCurrent vale "English" até o menu
    // principal carregar o idioma salvo; por isso o valor é relido a cada
    // segundo, o que também acompanha a troca de idioma com o jogo aberto.
    internal static class Lang
    {
        private const string PortugueseName = "Portuguese (Brazil)";

        public static string Current { get; private set; } = "English";
        public static bool Portuguese => Current == PortugueseName;

        private static float _nextCheck;
        private static readonly List<(MelonPreferences_Entry Entry, string PtName, string PtDescription, string EnName, string EnDescription)> _entries = new();

        public static string T(string portuguese, string english) => Portuguese ? portuguese : english;

        // Registra o nome e a descrição de uma opção nos dois idiomas; o menu
        // de configurações mostra o do idioma atual.
        public static MelonPreferences_Entry<T> Entry<T>(MelonPreferences_Entry<T> entry,
            string ptName, string ptDescription, string enName, string enDescription)
        {
            _entries.Add((entry, ptName, ptDescription, enName, enDescription));
            Apply(entry, ptName, ptDescription, enName, enDescription);
            return entry;
        }

        // Chamado no OnUpdate. Devolve true quando o idioma mudou.
        public static bool Tick()
        {
            if (Time.realtimeSinceStartup < _nextCheck)
                return false;
            _nextCheck = Time.realtimeSinceStartup + 1f;
            string current = TranslationManager.languageCurrent;
            if (string.IsNullOrEmpty(current) || current == Current)
                return false;
            Current = current;
            foreach (var e in _entries)
                Apply(e.Entry, e.PtName, e.PtDescription, e.EnName, e.EnDescription);
            return true;
        }

        private static void Apply(MelonPreferences_Entry entry, string ptName, string ptDescription, string enName, string enDescription)
        {
            entry.DisplayName = T(ptName, enName);
            entry.Description = T(ptDescription, enDescription);
        }
    }
}
