using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

namespace ModSettings
{
    // Janela IMGUI. Confirmado no jogo: Window, Button, Toggle, Box, Label e
    // Event.current funcionam, mas TextField, sliders, ScrollView e GUILayout
    // foram removidos ("Method unstripping failed"). Por isso o campo de texto
    // e a rolagem são feitos à mão, com os eventos do IMGUI.
    internal static class Window
    {
        private const int WindowId = 0x4D5345;

        public static bool IsOpen { get; private set; }

        // Medidas pensadas para 1080 linhas, multiplicadas por _scale para a
        // janela e o texto acompanharem telas maiores.
        private static float _scale = 1f;
        private static float S(float value) => value * _scale;
        private static float RowHeight => S(78);
        private static float ListWidth => S(220);
        private static float Margin => S(12);
        private static float TitleHeight => S(36);
        private static float BottomHeight => S(52);

        private static Rect _rect;
        private static GUI.WindowFunction _drawFunction;
        private static List<ModPage> _pages = new();
        private static string _selected;
        private static int _firstRow;
        private static string _status = "";

        // Campo de texto em edição e o texto dele antes da edição (para o Esc).
        private static Option _editing;
        private static string _beforeEdit;

        private static float _stylesScale;
        private static GUIStyle _name;
        private static GUIStyle _description;
        private static GUIStyle _value;
        private static GUIStyle _title;
        private static GUIStyle _panel;
        private static GUIStyle _list;
        private static Texture2D _panelTexture;
        private static Texture2D _listTexture;

        public static void Open()
        {
            _pages = ModPage.Discover();
            if (_pages.Count > 0 && _pages.Find(p => p.Name == _selected) == null)
                _selected = _pages[0].Name;
            _firstRow = 0;
            _status = "";
            _editing = null;
            _scale = Mathf.Clamp(Screen.height / 1080f, 1f, 3f);
            float width = Mathf.Min(S(1000), Screen.width - 40f);
            float height = Mathf.Min(S(700), Screen.height - 40f);
            _rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            IsOpen = true;
            GameInput.Capture();
        }

        // Cada mudança já foi gravada; um campo ainda em edição é gravado ao fechar.
        public static void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            StopEditing();
            GameInput.Release();
        }

        public static void Draw()
        {
            if (!IsOpen)
                return;
            GameInput.KeepCursorFree();
            EnsureStyles();
            HandleKeys(Event.current);
            if (!IsOpen)
                return;
            _drawFunction ??= DelegateSupport.ConvertDelegate<GUI.WindowFunction>(new Action<int>(DrawContents));
            _rect = GUI.Window(WindowId, _rect, _drawFunction, "");
        }

        // Teclas: digitação no campo em edição; Esc fecha a janela.
        private static void HandleKeys(Event e)
        {
            if (e == null || e.type != EventType.KeyDown)
                return;
            if (_editing != null)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    _editing.Draft = _beforeEdit;
                    _editing = null;
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Tab)
                    StopEditing();
                else if (e.keyCode == KeyCode.Backspace)
                {
                    if (_editing.Draft.Length > 0)
                        _editing.Draft = _editing.Draft.Substring(0, _editing.Draft.Length - 1);
                }
                else if (e.character != '\0' && !char.IsControl(e.character))
                    _editing.Draft += e.character;
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                Close();
                e.Use();
            }
        }

        private static void DrawContents(int id)
        {
            // Os botões usam o skin padrão; o tamanho da letra dele é
            // trocado só enquanto a janela desenha e depois devolvido.
            GUISkin skin = GUI.skin;
            int buttonFont = skin.button.fontSize;
            try
            {
                skin.button.fontSize = (int)S(15);

                float width = _rect.width, height = _rect.height;
                // O fundo padrão da janela é translúcido; cobre com um fundo sólido.
                GUI.Label(new Rect(0, 0, width, height), "", _panel);
                GUI.Label(new Rect(Margin, 0, width - 2 * Margin, TitleHeight), Lang.T("Configurações dos mods", "Mod settings"), _title);

                float top = TitleHeight;
                float bodyHeight = height - top - BottomHeight - Margin;
                DrawModList(new Rect(Margin, top, ListWidth, bodyHeight));
                ModPage page = _pages.Find(p => p.Name == _selected);
                var area = new Rect(Margin * 2 + ListWidth, top, width - ListWidth - Margin * 3, bodyHeight);
                if (page == null)
                    GUI.Label(area, Lang.T("Nenhum mod com opções foi encontrado.", "No mod with settings was found."), _value);
                else
                    DrawOptions(page, area);
                DrawBottom(page, new Rect(Margin, height - BottomHeight, width - 2 * Margin, S(36)));
                GUI.DragWindow(new Rect(0, 0, 10000, TitleHeight));
            }
            catch (Exception ex) { Mod.ReportError(nameof(DrawContents), ex); }
            finally
            {
                skin.button.fontSize = buttonFont;
            }
        }

        private static void DrawModList(Rect area)
        {
            GUI.Label(area, "", _list);
            float buttonHeight = S(38);
            for (int i = 0; i < _pages.Count; i++)
            {
                ModPage page = _pages[i];
                Color before = GUI.backgroundColor;
                if (page.Name == _selected)
                    GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                var rect = new Rect(area.x + S(8), area.y + S(8) + i * (buttonHeight + S(6)), area.width - S(16), buttonHeight);
                if (GUI.Button(rect, page.Name) && page.Name != _selected)
                {
                    StopEditing();
                    _selected = page.Name;
                    _firstRow = 0;
                }
                GUI.backgroundColor = before;
            }
        }

        private static int VisibleRows(Rect area) => Mathf.Max(1, (int)(area.height / RowHeight));

        private static void DrawOptions(ModPage page, Rect area)
        {
            int visible = VisibleRows(area);
            int maxFirst = Mathf.Max(0, page.Options.Count - visible);
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel && area.Contains(e.mousePosition))
            {
                _firstRow += e.delta.y > 0 ? 1 : -1;
                StopEditing();
                e.Use();
            }
            _firstRow = Mathf.Clamp(_firstRow, 0, maxFirst);

            for (int i = _firstRow; i < Mathf.Min(page.Options.Count, _firstRow + visible); i++)
                DrawOption(page.Options[i], new Rect(area.x, area.y + (i - _firstRow) * RowHeight, area.width, RowHeight));
        }

        private static void DrawOption(Option option, Rect row)
        {
            float line = S(30);
            float defaultWidth = S(100);
            float controlWidth = Mathf.Min(S(260), row.width * 0.35f);
            float nameWidth = row.width - controlWidth - defaultWidth - S(20);
            GUI.Label(new Rect(row.x, row.y, nameWidth, line), option.Name, _name);

            var control = new Rect(row.x + nameWidth + S(10), row.y, controlWidth, line);
            switch (option.Kind)
            {
                case Kind.Bool:
                    // Duas opções, como botões de rádio; a caixa de marcar do
                    // IMGUI não cresce com o tamanho da letra.
                    bool on = option.Draft == bool.TrueString;
                    float half = (control.width - S(6)) / 2f;
                    bool pickOn = Radio(new Rect(control.x, control.y, half, control.height), Lang.T("Ligado", "On"), on) && !on;
                    bool pickOff = Radio(new Rect(control.x + half + S(6), control.y, half, control.height), Lang.T("Desligado", "Off"), !on) && on;
                    if (pickOn || pickOff)
                    {
                        option.Toggle();
                        Commit(option);
                    }
                    break;
                case Kind.Choice:
                    if (GUI.Button(control, option.Draft))
                    {
                        option.Toggle();
                        Commit(option);
                    }
                    break;
                case Kind.ReadOnly:
                    GUI.Label(control, option.Current + Lang.T(" (só leitura)", " (read-only)"), _value);
                    break;
                default:
                    TextBox(option, control);
                    break;
            }
            if (option.Kind != Kind.ReadOnly && GUI.Button(new Rect(row.xMax - defaultWidth, row.y, defaultWidth, line), Lang.T("Padrão", "Default")))
            {
                if (_editing == option)
                    _editing = null;
                option.SetDefault();
                Commit(option);
            }
            if (!string.IsNullOrEmpty(option.Description))
                GUI.Label(new Rect(row.x, row.y + line + S(2), row.width, RowHeight - line - S(6)), option.Description, _description);
        }

        // Uma opção de rádio: a escolhida fica marcada e destacada.
        private static bool Radio(Rect rect, string text, bool selected)
        {
            Color before = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(0.4f, 0.8f, 1f) : new Color(0.5f, 0.5f, 0.5f);
            bool clicked = GUI.Button(rect, (selected ? "● " : "○ ") + text);
            GUI.backgroundColor = before;
            return clicked;
        }

        // Campo de texto feito à mão: clique para editar, Enter ou clique fora
        // para terminar e gravar, Esc desfaz a edição.
        private static void TextBox(Option option, Rect rect)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                if (rect.Contains(e.mousePosition))
                {
                    if (_editing != option)
                    {
                        StopEditing();
                        _editing = option;
                        _beforeEdit = option.Draft;
                    }
                    e.Use();
                }
                else if (_editing == option)
                    StopEditing();
            }
            GUI.Box(rect, "");
            bool caret = _editing == option && (int)(Time.realtimeSinceStartup * 2) % 2 == 0;
            Color before = GUI.contentColor;
            if (!option.Valid)
                GUI.contentColor = new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(rect.x + S(8), rect.y, rect.width - S(16), rect.height), option.Draft + (caret ? "|" : ""), _value);
            GUI.contentColor = before;
        }

        private static void DrawBottom(ModPage page, Rect bar)
        {
            string scroll = "";
            if (page != null)
            {
                int visible = VisibleRows(new Rect(0, 0, 0, _rect.height - TitleHeight - BottomHeight - Margin));
                if (page.Options.Count > visible)
                    scroll = Lang.T($"Opções {_firstRow + 1}–{Mathf.Min(page.Options.Count, _firstRow + visible)} de {page.Options.Count} (roda do mouse).   ",
                        $"Settings {_firstRow + 1}–{Mathf.Min(page.Options.Count, _firstRow + visible)} of {page.Options.Count} (mouse wheel).   ");
            }
            string status = string.IsNullOrEmpty(_status)
                ? Lang.T("As mudanças valem e ficam salvas na hora. F6 ou Esc fecha.", "Changes apply and are saved right away. F6 or Esc closes.")
                : _status;
            GUI.Label(bar, scroll + status, _value);
        }

        // Termina a edição do campo de texto e grava o valor digitado.
        private static void StopEditing()
        {
            Option option = _editing;
            _editing = null;
            if (option != null)
                Commit(option);
        }

        // Aplica a opção na hora (o mod dono vê o valor novo) e grava o
        // arquivo. Valor inválido não é aplicado e fica em vermelho.
        private static void Commit(Option option)
        {
            if (!option.Changed)
                return;
            if (!option.Valid)
            {
                _status = Lang.T($"Valor inválido em \"{option.Name}\"; não foi salvo.", $"Invalid value in \"{option.Name}\"; not saved.");
                return;
            }
            try
            {
                option.Apply();
                MelonPreferences.Save();
                _status = Lang.T($"Salvo: {option.Name} = {option.Current}.", $"Saved: {option.Name} = {option.Current}.");
                Mod.Log.Msg(_status);
            }
            catch (Exception ex) { Mod.ReportError($"{option.Entry.Category?.Identifier}.{option.Entry.Identifier}", ex); }
        }

        // O construtor de cópia do GUIStyle não existe no jogo (removido pelo IL2CPP).
        private static void EnsureStyles()
        {
            // A textura é um objeto do Unity: se for descartada, recria os estilos.
            if (_name != null && _stylesScale == _scale && _panelTexture != null && _listTexture != null)
                return;
            _stylesScale = _scale;
            _panelTexture = SolidTexture(new Color(0.11f, 0.11f, 0.13f));
            _listTexture = SolidTexture(new Color(0.17f, 0.17f, 0.2f));
            _panel = new GUIStyle();
            _panel.normal.background = _panelTexture;
            _list = new GUIStyle();
            _list.normal.background = _listTexture;
            _title = NewStyle(18, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            _name = NewStyle(16, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            _value = NewStyle(15, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
            _value.clipping = TextClipping.Clip;
            _description = NewStyle(13, new Color(0.78f, 0.78f, 0.78f), FontStyle.Normal, TextAnchor.UpperLeft);
            _description.wordWrap = true;
        }

        private static GUIStyle NewStyle(float size, Color color, FontStyle fontStyle, TextAnchor alignment)
        {
            var style = new GUIStyle { fontSize = (int)S(size), fontStyle = fontStyle, alignment = alignment };
            style.normal.textColor = color;
            return style;
        }

        // Textura de 1 pixel opaco para o fundo (GUI.DrawTexture foi removido do jogo).
        private static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
