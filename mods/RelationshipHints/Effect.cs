using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RelationshipHints
{
    // Efeito previsto de uma resposta na relação de um personagem com o Player.
    internal sealed class RelationDelta
    {
        public int Friendship;
        public int Romance;
        public bool FriendshipUncertain;
        public bool RomanceUncertain;

        // Eventos "definir valor" (Option2 == 0): o resultado é o valor, não a
        // variação. Somas depois de um "definir" se acumulam em Friendship/Romance.
        public int? FriendshipSet;
        public int? RomanceSet;

        public bool IsEmpty =>
            Friendship == 0 && Romance == 0 && !FriendshipUncertain && !RomanceUncertain
            && FriendshipSet == null && RomanceSet == null;
    }

    internal sealed class Effect
    {
        // Chave: nome do personagem cuja relação com o Player muda.
        public readonly Dictionary<string, RelationDelta> ByCharacter = new();

        // Passos seguidos no cálculo, para o log de diagnóstico.
        public readonly List<string> Trace = new();

        public RelationDelta For(string character)
        {
            if (!ByCharacter.TryGetValue(character, out RelationDelta delta))
            {
                delta = new RelationDelta();
                ByCharacter[character] = delta;
            }
            return delta;
        }

        private const string PositiveColor = "#5EE65E";
        private const string NegativeColor = "#FF5A5A";
        internal const string Transparent = "#00000000";

        // Ex.: "+1A -1R" para o falante; "Madison: -3A" para outros personagens.
        public string Format(string speaker, FormatMode mode = FormatMode.Plain)
        {
            var parts = new List<string>();
            foreach (KeyValuePair<string, RelationDelta> pair in ByCharacter.OrderBy(p => p.Key == speaker ? 0 : 1))
            {
                if (pair.Value.IsEmpty)
                    continue;

                string body = FormatDelta(pair.Value, mode);
                parts.Add(pair.Key == speaker ? body : $"{Neutral($"{pair.Key}:", mode)} {body}");
            }
            return string.Join(Neutral(";", mode) + " ", parts);
        }

        private static string FormatDelta(RelationDelta d, FormatMode mode)
        {
            var tokens = new List<string>();
            // Amizade: A em português, F (friendship) nos demais idiomas.
            Append(tokens, d.Friendship, d.FriendshipSet, d.FriendshipUncertain, Lang.T("A", "F"), mode);
            Append(tokens, d.Romance, d.RomanceSet, d.RomanceUncertain, "R", mode);
            return string.Join(" ", tokens);
        }

        private static void Append(List<string> tokens, int delta, int? set, bool uncertain, string letter, FormatMode mode)
        {
            if (uncertain)
                tokens.Add(Neutral($"?{letter}", mode));
            else if (set != null)
                tokens.Add(Neutral($"={set + delta}{letter}", mode));
            else if (delta != 0)
                tokens.Add(Colored($"{delta:+0;-0}{letter}", delta > 0 ? PositiveColor : NegativeColor, mode));
        }

        // Trechos sem cor própria: visíveis no texto do botão, invisíveis na camada.
        private static string Neutral(string text, FormatMode mode) =>
            mode == FormatMode.Overlay ? $"<color={Transparent}>{text}</color>" : text;

        // Trechos coloridos: visíveis na camada, invisíveis no texto do botão
        // (que recebe a tinta de destaque do jogo).
        private static string Colored(string text, string color, FormatMode mode) => mode switch
        {
            FormatMode.Base => $"<color={Transparent}>{text}</color>",
            FormatMode.Overlay => $"<color={color}>{text}</color>",
            _ => text,
        };
    }

    internal enum FormatMode
    {
        // Texto simples, para o log.
        Plain,
        // Texto do botão: números coloridos ficam transparentes, ocupando o espaço.
        Base,
        // Camada sobreposta: só os números coloridos ficam visíveis.
        Overlay,
    }
}
