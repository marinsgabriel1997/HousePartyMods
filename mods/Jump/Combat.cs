using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jump
{
    // O modo de combate do jogo fica no Espaço. Com o pulo no Espaço, o mod
    // passa o combate para o Alt no teclado, por override do Input System,
    // sem gravar a troca: sem o mod, o combate volta ao Espaço.
    //
    // Confirmado no jogo:
    // - Há um PlayerInputMap original, que o mapeamento de controles (F8)
    //   mostra, e cópias "(Clone)" criadas no menu e a cada carregamento, que a
    //   partida usa. A troca vale para todos.
    // - "Restaurar padrão" no F8 remove o override ou grava um para o próprio
    //   Espaço; por isso a troca confere a tecla efetiva, a cada segundo.
    // - O jogo grava os overrides ativos ao fechar, por
    //   SaveBindingOverridesAsJson; a troca do mod sai do texto gravado. Um Alt
    //   escolhido no F8 é gravado como <Keyboard>/leftAlt e fica.
    internal static class Combat
    {
        private const string ActionName = "Player/CombatMode";
        private const string SpacePath = "<Keyboard>/space";
        // Caminho próprio do mod, diferente do <Keyboard>/leftAlt que o F8 grava.
        private const string AltPath = "<Keyboard>/alt";

        private static MelonPreferences_Entry<bool> _enabled;
        private static float _nextCheck;

        // Entrada do combate com o Alt do mod no JSON dos overrides.
        private static readonly Regex OwnOverride = new(
            @"\{[^{}]*""action"":""" + Regex.Escape(ActionName) + @"""[^{}]*""path"":""" + Regex.Escape(AltPath) + @"""[^{}]*\},?");

        public static void Init(MelonPreferences_Category category)
        {
            _enabled = Lang.Entry(category.CreateEntry("CombatOnAlt", true),
                "Combate no Alt", "Com o pulo no Espaço, passa o modo de combate do Espaço para o Alt (sem gravar a troca nos controles do jogo).",
                "Combat on Alt", "With jump on Space, moves combat mode from Space to Alt (without saving the change in the game's controls).");
        }

        private static bool Active => _enabled.Value && Mod.Key?.BindingPath == SpacePath;

        // Aplica ou desfaz a troca em todos os conjuntos de ações, uma vez por segundo.
        public static void Tick()
        {
            if (Time.unscaledTime < _nextCheck)
                return;
            _nextCheck = Time.unscaledTime + 1f;
            bool active = Active;
            var assets = Resources.FindObjectsOfTypeAll(Il2CppType.Of<InputActionAsset>());
            for (int i = 0; i < (assets?.Length ?? 0); i++)
            {
                var action = assets[i]?.TryCast<InputActionAsset>()?.FindAction(ActionName, false);
                if (action == null)
                    continue;
                var bindings = action.bindings;
                for (int b = 0; b < bindings.Count; b++)
                {
                    var binding = bindings[b];
                    if (active && binding.effectivePath == SpacePath)
                        action.ApplyBindingOverride(b, AltPath);
                    else if (!active && binding.overridePath == AltPath)
                        action.RemoveBindingOverride(b);
                }
            }
        }

        // No F8, a tecla do pulo conta como já usada, para nenhuma ação ir para ela.
        [HarmonyPatch(typeof(RebindActionUI), nameof(RebindActionUI.IsDuplicate))]
        private static class DuplicatePatch
        {
            private static void Postfix(RebindActionUI __instance, InputControl rebindControl, ref bool __result)
            {
                try
                {
                    if (__result || !Guard.Is<RebindActionUI>(__instance) || rebindControl == null)
                        return;
                    string jump = Mod.Key?.BindingPath;
                    // Caminho do controle: "/Keyboard/space"; da tecla: "<Keyboard>/space".
                    if (jump != null && "/" + jump.Replace("<", "").Replace(">", "") == rebindControl.path)
                        __result = true;
                }
                catch (Exception ex) { Mod.ReportError(nameof(DuplicatePatch), ex); }
            }
        }

        // Tira a troca do mod do que o jogo grava.
        [HarmonyPatch(typeof(InputActionRebindingExtensions), nameof(InputActionRebindingExtensions.SaveBindingOverridesAsJson), typeof(IInputActionCollection2))]
        private static class SavePatch
        {
            private static void Postfix(ref string __result)
            {
                try
                {
                    if (!string.IsNullOrEmpty(__result))
                        __result = OwnOverride.Replace(__result, "").Replace(",]", "]");
                }
                catch (Exception ex) { Mod.ReportError(nameof(SavePatch), ex); }
            }
        }
    }
}
