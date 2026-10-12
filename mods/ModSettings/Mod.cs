using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ModSettings.Mod), "ModSettings", "1.0.0", "gabriel")]
[assembly: MelonGame("Eek", "House Party")]

namespace ModSettings
{
    // Janela, aberta por tecla, com as opções que os mods registram no
    // MelonPreferences. O menu descobre as opções sozinho; nenhum mod depende
    // dele, e ele não conhece nenhum mod.
    public class Mod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        private static MelonPreferences_Entry<string> _openKeyEntry;
        private static HotKey _openKey;

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
            MelonPreferences_Category category = MelonPreferences.CreateCategory("ModSettings");
            // F10 é "salvar o jogo"; F6 está livre (docs/teclas-do-jogo.md).
            _openKeyEntry = Lang.Entry(category.CreateEntry("OpenKey", "F6"),
                "Tecla desta janela", "Tecla que abre e fecha esta janela (nome do KeyCode do Unity, por exemplo F6, F4, K).",
                "Key for this window", "Key that opens and closes this window (Unity KeyCode name, for example F6, F4, K).");
            _openKey = new HotKey("OpenKey", _openKeyEntry.Value, KeyCode.F6);
            _openKeyEntry.OnEntryValueChanged.Subscribe((_, value) => _openKey = new HotKey("OpenKey", value, KeyCode.F6));
            Log.Msg($"Carregado. {_openKey.Name} abre as configurações dos mods.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // As ações e a pausa da cena anterior não valem mais.
            try { Window.Close(); }
            catch (Exception ex) { ReportError(nameof(OnSceneWasLoaded), ex); }
        }

        public override void OnUpdate()
        {
            try
            {
                Lang.Tick();
                if (_openKey.Pressed())
                {
                    if (Window.IsOpen)
                        Window.Close();
                    else
                        Window.Open();
                }
                GameInput.KeepCursorFree();
            }
            catch (Exception ex) { ReportError(nameof(OnUpdate), ex); }
        }

        // O jogo esconde o cursor no Update; forçar de novo depois dele.
        public override void OnLateUpdate()
        {
            try { GameInput.KeepCursorFree(); }
            catch (Exception ex) { ReportError(nameof(OnLateUpdate), ex); }
        }

        public override void OnGUI()
        {
            try { Window.Draw(); }
            catch (Exception ex) { ReportError(nameof(Window), ex); }
        }
    }

    // Leitura de uma tecla configurada.
    internal sealed class HotKey
    {
        private static bool _legacyInputBroken;
        private readonly KeyCode _key;
        private readonly UnityEngine.InputSystem.Key _newKey;

        public string Name => _key.ToString();

        public HotKey(string option, string name, KeyCode fallback)
        {
            if (!Enum.TryParse(name?.Trim(), true, out _key))
            {
                Mod.Log.Warning($"{option} \"{name}\" não é um KeyCode válido; usando {fallback}.");
                _key = fallback;
            }
            // Nomes iguais nos dois sistemas para letras e F1–F12; os demais ficam sem reserva.
            if (!Enum.TryParse(_key.ToString(), true, out _newKey))
                _newKey = UnityEngine.InputSystem.Key.None;
        }

        // O jogo pode ter só o Input System novo ativo; nesse caso o Input
        // antigo lança exceção e passamos a usar o novo.
        public bool Pressed()
        {
            if (!_legacyInputBroken)
            {
                try { return Input.GetKeyDown(_key); }
                catch (Exception ex)
                {
                    _legacyInputBroken = true;
                    Mod.Log.Msg($"Input antigo indisponível ({ex.GetType().Name}); usando o Input System.");
                }
            }
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && _newKey != UnityEngine.InputSystem.Key.None && keyboard[_newKey].wasPressedThisFrame;
        }
    }
}
