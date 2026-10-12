using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Probe
{
    // Fase 1 do p40: levanta as teclas que o jogo usa. O relatório (F7) traz as
    // ações do Input System carregadas em memória; os patches registram no log
    // cada tecla consultada diretamente, pelo Input antigo ou pelo teclado do
    // Input System, inclusive atalhos que não aparecem na tela de controles.
    internal static class Keys
    {
        // Leituras de tecla feitas pelo próprio Probe não entram no registro.
        internal static bool Self;

        private static readonly HashSet<string> _seen = new();

        public static void Init(HarmonyLib.Harmony harmony)
        {
            Type input = typeof(Input);
            foreach (string name in new[] { nameof(Input.GetKey), nameof(Input.GetKeyDown), nameof(Input.GetKeyUp) })
            {
                Patch(harmony, AccessTools.Method(input, name, new[] { typeof(KeyCode) }), nameof(LegacyKeyCode));
                Patch(harmony, AccessTools.Method(input, name, new[] { typeof(string) }), nameof(LegacyName));
            }
            foreach (string name in new[] { nameof(Input.GetButton), nameof(Input.GetButtonDown), nameof(Input.GetButtonUp) })
                Patch(harmony, AccessTools.Method(input, name, new[] { typeof(string) }), nameof(LegacyName));
            Patch(harmony, AccessTools.PropertyGetter(input, nameof(Input.anyKeyDown)), nameof(LegacyAny));
            Patch(harmony, AccessTools.PropertyGetter(input, nameof(Input.anyKey)), nameof(LegacyAny));

            Type button = typeof(ButtonControl);
            foreach (string name in new[] { nameof(ButtonControl.isPressed), nameof(ButtonControl.wasPressedThisFrame), nameof(ButtonControl.wasReleasedThisFrame) })
                Patch(harmony, AccessTools.PropertyGetter(button, name), nameof(Button));
        }

        // Um patch por método, cada um isolado: se um deles não existir no jogo, os outros seguem.
        private static void Patch(HarmonyLib.Harmony harmony, MethodBase target, string prefix)
        {
            if (target == null)
            {
                Mod.Log.Warning($"[teclas] método não encontrado para {prefix}");
                return;
            }
            try
            {
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(Keys), prefix));
                Mod.Log.Msg($"[teclas] vigiando {target.DeclaringType?.Name}.{target.Name}({string.Join(", ", Array.ConvertAll(target.GetParameters(), p => p.ParameterType.Name))})");
            }
            catch (Exception ex) { Mod.Log.Warning($"[teclas] não deu para vigiar {target.Name}: {ex.Message}"); }
        }

        private static void Record(string what)
        {
            if (Self || !_seen.Add(what))
                return;
            Mod.Log.Msg($"[teclas] {Time.realtimeSinceStartup:F1}s cena '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}' consultou {what}");
        }

        private static void LegacyKeyCode(MethodBase __originalMethod, KeyCode __0)
        {
            try { Record($"Input.{__originalMethod.Name}(KeyCode.{__0})"); }
            catch (Exception ex) { Mod.Log.Error($"[teclas] {ex.Message}"); }
        }

        private static void LegacyName(MethodBase __originalMethod, string __0)
        {
            try { Record($"Input.{__originalMethod.Name}(\"{__0}\")"); }
            catch (Exception ex) { Mod.Log.Error($"[teclas] {ex.Message}"); }
        }

        private static void LegacyAny(MethodBase __originalMethod)
        {
            try { Record($"Input.{__originalMethod.Name}"); }
            catch (Exception ex) { Mod.Log.Error($"[teclas] {ex.Message}"); }
        }

        private static void Button(MethodBase __originalMethod, ButtonControl __instance)
        {
            try
            {
                if (__instance != null)
                    Record($"{__instance.path}.{__originalMethod.Name}");
            }
            catch (Exception ex) { Mod.Log.Error($"[teclas] {ex.Message}"); }
        }

        public static void Dump(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("== TECLAS: AÇÕES HABILITADAS (Input System) ==");
            var enabled = InputSystem.ListEnabledActions();
            for (int i = 0; i < (enabled?.Count ?? 0); i++)
            {
                InputAction action = enabled[i];
                if (action == null)
                    continue;
                string display, overrides;
                try { display = InputActionRebindingExtensions.GetBindingDisplayString(action, default, null); }
                catch (Exception ex) { display = $"erro: {ex.Message}"; }
                try { overrides = InputActionRebindingExtensions.SaveBindingOverridesAsJson(action); }
                catch (Exception ex) { overrides = $"erro: {ex.Message}"; }
                report.AppendLine($"{action.actionMap?.name}/{action.name} [{display}]" +
                                  (string.IsNullOrEmpty(overrides) || overrides == "{\"bindings\":[]}" ? "" : $" overrides={overrides}"));
            }

            report.AppendLine();
            report.AppendLine("== TECLAS: CONJUNTOS DE AÇÕES EM MEMÓRIA (JSON com os bindings originais) ==");
            var assets = Resources.FindObjectsOfTypeAll(Il2CppType.Of<InputActionAsset>());
            for (int i = 0; i < (assets?.Length ?? 0); i++)
            {
                InputActionAsset asset = assets[i]?.TryCast<InputActionAsset>();
                if (asset == null)
                    continue;
                report.AppendLine($"-- {asset.name} (habilitado={asset.enabled})");
                try { report.AppendLine(asset.ToJson()); }
                catch (Exception ex) { report.AppendLine($"erro: {ex.Message}"); }
            }

            report.AppendLine();
            report.AppendLine("== TECLAS: CONSULTADAS DIRETAMENTE ATÉ AGORA ==");
            var seen = new List<string>(_seen);
            seen.Sort(StringComparer.Ordinal);
            foreach (string what in seen)
                report.AppendLine(what);
        }

        // Para o menu: categorias e entradas registradas no MelonPreferences.
        public static void DumpPreferences(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("== MELONPREFERENCES REGISTRADAS ==");
            foreach (MelonPreferences_Category category in MelonPreferences.Categories)
            {
                report.AppendLine($"[{category.Identifier}] nome=\"{category.DisplayName}\" oculta={category.IsHidden} entradas={category.Entries.Count}");
                foreach (MelonPreferences_Entry entry in category.Entries)
                    report.AppendLine($"  {entry.Identifier} ({entry.GetReflectedType()?.Name}) nome=\"{entry.DisplayName}\" oculta={entry.IsHidden} " +
                                      $"valor={entry.GetValueAsString()} padrão={entry.GetDefaultValueAsString()} validador={entry.Validator?.GetType().Name ?? "-"}");
            }
        }
    }
}
