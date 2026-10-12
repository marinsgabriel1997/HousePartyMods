using System;
using System.Collections.Generic;
using System.Globalization;
using MelonLoader;

namespace ModSettings
{
    internal enum Kind { Bool, Integer, Real, Text, Choice, ReadOnly }

    // Uma opção de um mod, com o valor em edição guardado como texto até o
    // jogador salvar.
    internal sealed class Option
    {
        public readonly MelonPreferences_Entry Entry;
        public readonly Kind Kind;
        private readonly Type _type;
        public string Draft;

        public Option(MelonPreferences_Entry entry)
        {
            Entry = entry;
            _type = entry.GetReflectedType();
            Kind = KindOf(_type);
            Reset();
        }

        public string Name => string.IsNullOrEmpty(Entry.DisplayName) ? Entry.Identifier : Entry.DisplayName;
        public string Description => Entry.Description;
        public string Current => Format(Entry.BoxedValue);
        public bool Changed => Draft != Current;
        public bool Valid => TryParse(Draft, out _);

        // Volta o rascunho ao valor atual.
        public void Reset() => Draft = Current;

        public void SetDefault()
        {
            // DefaultValue só existe na entrada genérica (MelonPreferences_Entry<T>).
            object value = Entry.GetType().GetProperty("DefaultValue")?.GetValue(Entry);
            if (value != null)
                Draft = Format(value);
        }

        public void Toggle()
        {
            if (Kind == Kind.Bool && bool.TryParse(Draft, out bool on))
                Draft = Format(!on);
            else if (Kind == Kind.Choice)
            {
                string[] names = Enum.GetNames(_type);
                int index = Array.FindIndex(names, n => n == Draft);
                Draft = names[(index + 1) % names.Length];
            }
        }

        // Aplica o rascunho; o mod dono da opção vê o valor novo na hora.
        public bool Apply()
        {
            if (!Changed || !TryParse(Draft, out object value))
                return false;
            Entry.BoxedValue = value;
            Draft = Current;
            return true;
        }

        private bool TryParse(string text, out object value)
        {
            value = null;
            text ??= "";
            try
            {
                switch (Kind)
                {
                    case Kind.Bool:
                        if (!bool.TryParse(text, out bool on)) return false;
                        value = on;
                        break;
                    case Kind.Integer:
                        if (!long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole)) return false;
                        value = Convert.ChangeType(whole, _type, CultureInfo.InvariantCulture);
                        break;
                    case Kind.Real:
                        // Aceita vírgula ou ponto como separador decimal.
                        if (!double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double real)
                            || double.IsNaN(real) || double.IsInfinity(real)) return false;
                        value = Convert.ChangeType(real, _type, CultureInfo.InvariantCulture);
                        break;
                    case Kind.Text:
                        value = text;
                        break;
                    case Kind.Choice:
                        if (!Enum.IsDefined(_type, text)) return false;
                        value = Enum.Parse(_type, text);
                        break;
                    default:
                        return false;
                }
            }
            catch (OverflowException) { return false; }
            return Entry.Validator == null || Entry.Validator.IsValid(value);
        }

        private static string Format(object value) => value switch
        {
            null => "",
            float f => f.ToString("G", CultureInfo.InvariantCulture),
            double d => d.ToString("G", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };

        private static Kind KindOf(Type type)
        {
            if (type == typeof(bool)) return Kind.Bool;
            if (type == typeof(string)) return Kind.Text;
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return Kind.Real;
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
                || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte)) return Kind.Integer;
            if (type != null && type.IsEnum) return Kind.Choice;
            return Kind.ReadOnly;
        }
    }

    // As opções de um mod (uma categoria do MelonPreferences).
    internal sealed class ModPage
    {
        public string Name;
        public List<Option> Options = new();

        // Categorias e entradas visíveis registradas pelos mods carregados.
        // Entradas que só existem no arquivo, de versões antigas, não são
        // registradas e por isso não aparecem.
        public static List<ModPage> Discover()
        {
            var pages = new List<ModPage>();
            foreach (MelonPreferences_Category category in MelonPreferences.Categories)
            {
                if (category == null || category.IsHidden)
                    continue;
                var page = new ModPage { Name = string.IsNullOrEmpty(category.DisplayName) ? category.Identifier : category.DisplayName };
                foreach (MelonPreferences_Entry entry in category.Entries)
                    if (entry != null && !entry.IsHidden)
                        page.Options.Add(new Option(entry));
                if (page.Options.Count > 0)
                    pages.Add(page);
            }
            pages.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return pages;
        }
    }
}
