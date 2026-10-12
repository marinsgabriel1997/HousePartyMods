using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Probe
{
    // Fase 1 do p40: janela de teste (Shift+F7) para saber quais controles do
    // IMGUI sobreviveram no jogo, como fica o cursor e se cliques e teclas
    // chegam ao jogo com a janela aberta. TextField, sliders, ScrollView e
    // GUILayout foram removidos do jogo; o campo de texto e a rolagem daqui
    // são feitos à mão com Event.current.
    internal static class TestWindow
    {
        private const int WindowId = 0x50524F;

        private static bool _open;
        private static Rect _rect = new(80, 80, 460, 520);
        private static GUI.WindowFunction _draw;

        private static readonly Dictionary<string, string> _results = new();
        private static readonly List<InputAction> _disabled = new();

        private static bool _savedVisible;
        private static CursorLockMode _savedLock;
        private static bool _paused;
        private static bool _openedPause;
        private static float _savedTimeScale = 1f;

        private static int _clicks;
        private static bool _toggle;
        private static string _text = "digite aqui";
        private static bool _typing;
        private static int _firstLine;
        private static string _cursorWhere;

        public static void Toggle()
        {
            _open = !_open;
            if (_open)
            {
                _savedVisible = Cursor.visible;
                _savedLock = Cursor.lockState;
                Mod.Log.Msg($"[janela] aberta; cursor antes: visível={_savedVisible} trava={_savedLock} timeScale={Time.timeScale}");
                _cursorWhere = null;
                OpenGamePause();
                // Com a pausa por baixo, desligar as ações impede que cliques e
                // Esc acertem os botões dela.
                SetGameInput(false);
            }
            else
            {
                SetGameInput(true);
                SetPaused(false);
                // Com a pausa ainda aberta, o cursor fica como a pausa quer.
                bool restore = CloseGamePause() || Il2CppEekCharacterEngine.PlayerCharacter.Player == null;
                if (restore)
                {
                    Cursor.visible = _savedVisible;
                    Cursor.lockState = _savedLock;
                }
                Mod.Log.Msg($"[janela] fechada; cursor devolvido={restore}: visível={Cursor.visible} trava={Cursor.lockState}");
            }
        }

        // O jogo trava o cursor de novo a cada quadro; forçar no Update não
        // bastou. Força também no LateUpdate e no OnGUI e registra onde o
        // cursor estava travado, para saber em que ponto o jogo o muda.
        public static void Tick(string where)
        {
            if (!_open)
                return;
            if ((!Cursor.visible || Cursor.lockState != CursorLockMode.None) && _cursorWhere != where)
            {
                _cursorWhere = where;
                Mod.Log.Msg($"[janela] cursor travado pelo jogo antes de {where}: visível={Cursor.visible} trava={Cursor.lockState}");
            }
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public static void OnGUI()
        {
            if (!_open)
                return;
            Tick("OnGUI");
            Try("GUI.Window", () =>
            {
                _draw ??= DelegateSupport.ConvertDelegate<GUI.WindowFunction>(new Action<int>(Draw));
                _rect = GUI.Window(WindowId, _rect, _draw, "Probe: teste de janela");
            });
        }

        private static void Draw(int id)
        {
            float y = 24;
            Try("GUI.Box", () => GUI.Box(new Rect(10, y, 440, 30), "GUI.Box"));
            y += 36;
            Try("GUI.Button", () =>
            {
                if (GUI.Button(new Rect(10, y, 210, 26), $"GUI.Button ({_clicks})"))
                    _clicks++;
            });
            Try("GUI.Toggle", () => _toggle = GUI.Toggle(new Rect(230, y, 220, 26), _toggle, "GUI.Toggle"));
            y += 32;
            Try("campo de texto próprio", () => TextBox(new Rect(10, y, 440, 24)));
            y += 32;
            Try("GUI.contentColor", () =>
            {
                Color before = GUI.contentColor;
                GUI.contentColor = Color.red;
                GUI.Label(new Rect(10, y, 210, 22), "texto vermelho (contentColor)");
                GUI.contentColor = before;
            });
            Try("GUI.backgroundColor", () =>
            {
                Color before = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                GUI.Button(new Rect(230, y, 220, 22), "fundo verde (backgroundColor)");
                GUI.backgroundColor = before;
            });
            y += 30;

            Try("GUI.Button bloqueio", () =>
            {
                if (GUI.Button(new Rect(10, y, 210, 26), _disabled.Count > 0 ? $"Religar ações ({_disabled.Count})" : "Desligar ações do jogo"))
                    SetGameInput(_disabled.Count > 0);
                if (GUI.Button(new Rect(230, y, 220, 26), _paused ? "Despausar" : "Pausar (timeScale 0)"))
                    SetPaused(!_paused);
            });
            y += 34;

            Try("rolagem própria", () => ScrollList(new Rect(10, y, 440, 150)));
            y += 156;

            Try("GUI.BeginGroup", () =>
            {
                GUI.BeginGroup(new Rect(10, y, 440, 26));
                GUI.Label(new Rect(0, 2, 440, 22), "dentro de GUI.BeginGroup");
                GUI.EndGroup();
            });
            y += 30;

            Try("Event.current", () =>
            {
                Event e = Event.current;
                GUI.Label(new Rect(10, y, 440, 22), $"evento: {e?.type} tecla={e?.keyCode} mouse={e?.mousePosition}");
            });
            y += 24;
            GUI.Label(new Rect(10, y, 440, 22), $"falhas: {Failures()}");

            Try("GUI.DragWindow", () => GUI.DragWindow(new Rect(0, 0, 10000, 22)));
        }

        // Campo de texto feito à mão: clique para editar, digite, Enter ou
        // clique fora para terminar.
        private static void TextBox(Rect rect)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                bool inside = rect.Contains(e.mousePosition);
                if (inside != _typing)
                    Mod.Log.Msg($"[janela] campo de texto {(inside ? "em edição" : "fora de edição")}");
                _typing = inside;
            }
            if (_typing && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace && _text.Length > 0)
                    _text = _text.Substring(0, _text.Length - 1);
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
                    _typing = false;
                else if (e.character != '\0' && !char.IsControl(e.character))
                    _text += e.character;
                e.Use();
            }
            GUI.Box(rect, "");
            bool caret = _typing && (int)(Time.realtimeSinceStartup * 2) % 2 == 0;
            GUI.Label(new Rect(rect.x + 6, rect.y + 2, rect.width - 12, rect.height - 4), _text + (caret ? "|" : ""));
        }

        // Lista com rolagem feita à mão: roda do mouse sobre a área e só as
        // linhas visíveis desenhadas.
        private static void ScrollList(Rect rect)
        {
            const int total = 16, visible = 6;
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                _firstLine = Mathf.Clamp(_firstLine + (e.delta.y > 0 ? 1 : -1), 0, total - visible);
                e.Use();
            }
            GUI.Box(rect, "");
            for (int i = 0; i < visible; i++)
                GUI.Label(new Rect(rect.x + 6, rect.y + 4 + i * 24, rect.width - 12, 22), $"linha {_firstLine + i + 1} de {total} (roda do mouse)");
        }

        private static void Try(string what, Action action)
        {
            try
            {
                action();
                if (!_results.ContainsKey(what))
                {
                    _results[what] = "ok";
                    Mod.Log.Msg($"[janela] {what} ok");
                }
            }
            catch (Exception ex)
            {
                if (!_results.TryGetValue(what, out string before) || before == "ok")
                    Mod.Log.Error($"[janela] {what} falhou: {ex.GetType().Name}: {ex.Message}");
                _results[what] = ex.GetType().Name;
            }
        }

        private static string Failures()
        {
            var failed = new List<string>();
            foreach (var pair in _results)
                if (pair.Value != "ok")
                    failed.Add(pair.Key);
            return failed.Count == 0 ? "nenhuma" : string.Join(", ", failed);
        }

        // Desliga as ações habilitadas do jogo (movimento, câmera, interação,
        // UI) e as religa depois, para ver se isso basta para isolar a janela.
        private static void SetGameInput(bool on)
        {
            if (on)
            {
                foreach (InputAction action in _disabled)
                    action?.Enable();
                if (_disabled.Count > 0)
                    Mod.Log.Msg($"[janela] {_disabled.Count} ações religadas");
                _disabled.Clear();
                return;
            }
            var enabled = InputSystem.ListEnabledActions();
            for (int i = 0; i < (enabled?.Count ?? 0); i++)
            {
                InputAction action = enabled[i];
                if (action == null)
                    continue;
                action.Disable();
                _disabled.Add(action);
            }
            Mod.Log.Msg($"[janela] {_disabled.Count} ações do jogo desligadas");
        }

        // Na partida, abre a pausa do jogo como o Esc faz: OnInput_Options é o
        // evento que a ação UI/Options dispara.
        private static void OpenGamePause()
        {
            _openedPause = false;
            if (Il2CppEekCharacterEngine.PlayerCharacter.Player == null)
                return;
            bool before = Il2CppEekCharacterEngine.Canvas.CanvasEvents.IsPauseWhenOpenActive();
            if (!before)
            {
                Il2Cpp.PlayerControlManager.OnInput_Options?.Invoke();
                _openedPause = Il2CppEekCharacterEngine.Canvas.CanvasEvents.IsPauseWhenOpenActive();
            }
            Mod.Log.Msg($"[janela] pausa do jogo: antes={before} depois={Il2CppEekCharacterEngine.Canvas.CanvasEvents.IsPauseWhenOpenActive()} " +
                        $"timeScale={Time.timeScale}");
        }

        // Fecha a pausa só se foi a janela que a abriu; se o jogador já estava
        // na pausa, ela continua. Devolve true se a pausa saiu.
        private static bool CloseGamePause()
        {
            if (!_openedPause)
                return false;
            _openedPause = false;
            if (Il2CppEekCharacterEngine.Canvas.CanvasEvents.IsPauseWhenOpenActive())
                Il2Cpp.PlayerControlManager.OnInput_Options?.Invoke();
            bool closed = !Il2CppEekCharacterEngine.Canvas.CanvasEvents.IsPauseWhenOpenActive();
            Mod.Log.Msg($"[janela] pausa do jogo fechada={closed} timeScale={Time.timeScale}");
            return closed;
        }

        private static void SetPaused(bool paused)
        {
            if (paused == _paused)
                return;
            _paused = paused;
            if (paused)
            {
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
                Time.timeScale = _savedTimeScale;
            Mod.Log.Msg($"[janela] pausa={paused} timeScale={Time.timeScale}");
        }
    }
}
