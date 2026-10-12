using System.Collections.Generic;
using Il2CppEekCharacterEngine;
using Il2CppEekCharacterEngine.Canvas;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ModSettings
{
    // Isola o jogo enquanto a janela está aberta. Confirmado no jogo: sem
    // isso, a câmera gira, o clique interage e as teclas agem por baixo da
    // janela. Na partida, a janela abre a pausa do jogo (o jogo para e solta o
    // cursor) e desliga as ações do Input System, para que cliques e teclas
    // não acertem a pausa por baixo.
    internal static class GameInput
    {
        private static readonly List<InputAction> _disabled = new();
        private static bool _captured;
        private static bool _openedPause;
        private static bool _savedVisible;
        private static CursorLockMode _savedLock;

        public static void Capture()
        {
            if (_captured)
                return;
            _captured = true;
            _savedVisible = Cursor.visible;
            _savedLock = Cursor.lockState;
            OpenPause();

            var enabled = InputSystem.ListEnabledActions();
            for (int i = 0; i < (enabled?.Count ?? 0); i++)
            {
                InputAction action = enabled[i];
                if (action == null)
                    continue;
                action.Disable();
                _disabled.Add(action);
            }
        }

        public static void Release()
        {
            if (!_captured)
                return;
            _captured = false;
            foreach (InputAction action in _disabled)
                action?.Enable();
            _disabled.Clear();

            // Com a pausa do jogador ainda aberta, o cursor fica como a pausa quer.
            if (ClosePause() || PlayerCharacter.Player == null)
            {
                Cursor.visible = _savedVisible;
                Cursor.lockState = _savedLock;
            }
        }

        // O jogo esconde e trava o cursor a cada quadro na partida.
        public static void KeepCursorFree()
        {
            if (!_captured)
                return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // OnInput_Options é o evento que o Esc (ação UI/Options) dispara; abre
        // e fecha a pausa. Se o jogador já estava na pausa, ela fica.
        private static void OpenPause()
        {
            _openedPause = false;
            if (PlayerCharacter.Player == null || CanvasEvents.IsPauseWhenOpenActive())
                return;
            Il2Cpp.PlayerControlManager.OnInput_Options?.Invoke();
            _openedPause = CanvasEvents.IsPauseWhenOpenActive();
        }

        // Fecha a pausa só se foi a janela que a abriu. Devolve true se a pausa saiu.
        private static bool ClosePause()
        {
            if (!_openedPause)
                return false;
            _openedPause = false;
            if (PlayerCharacter.Player != null && CanvasEvents.IsPauseWhenOpenActive())
                Il2Cpp.PlayerControlManager.OnInput_Options?.Invoke();
            return !CanvasEvents.IsPauseWhenOpenActive();
        }
    }
}
