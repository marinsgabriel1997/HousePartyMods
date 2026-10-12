using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

namespace Probe
{
    // Fase 1 do p30: mede quanto tempo o vídeo da Eek! Games e o aviso ficam na
    // tela e testa se responder "tecla apertada" a UserInputForTransitionReceived
    // leva ao menu pelo caminho do próprio jogo. Probe.SkipIntro liga o teste;
    // desligado por padrão, já que o mod SkipIntro faz isso.
    internal static class Intro
    {
        private static MelonPreferences_Entry<bool> _skip;
        private static string _forcedIn;
        private static string _lastTransition;

        public static void Init()
        {
            var category = MelonPreferences.CreateCategory("Probe");
            _skip = category.CreateEntry("SkipIntro", false);
        }

        public static void OnSceneLoaded(int buildIndex, string name)
        {
            Mod.Log.Msg($"[intro] {Time.realtimeSinceStartup:F2}s cena {buildIndex} '{name}' (pular={_skip.Value})");
        }

        // Métodos nativos podem ser compartilhados entre classes; confere a classe real.
        private static bool IsTransitional(TransitionalSceneManager obj)
        {
            if (obj == null || obj.Pointer == IntPtr.Zero)
                return false;
            IntPtr actual = IL2CPP.il2cpp_object_get_class(obj.Pointer);
            return IL2CPP.il2cpp_class_is_assignable_from(Il2CppClassPointerStore<TransitionalSceneManager>.NativeClassPtr, actual);
        }

        [HarmonyPatch(typeof(TransitionalSceneManager), nameof(TransitionalSceneManager.UserInputForTransitionReceived))]
        private static class InputPatch
        {
            private static void Postfix(TransitionalSceneManager __instance, ref bool __result)
            {
                try
                {
                    if (!_skip.Value || __result || !IsTransitional(__instance))
                        return;
                    __result = true;
                    string type = __instance.GetIl2CppType().Name;
                    if (_forcedIn != type)
                    {
                        _forcedIn = type;
                        Mod.Log.Msg($"[intro] {Time.realtimeSinceStartup:F2}s tecla simulada em {type}");
                    }
                }
                catch (Exception ex) { Mod.Log.Error($"[intro] {ex}"); }
            }
        }

        [HarmonyPatch(typeof(TransitionalSceneManager), nameof(TransitionalSceneManager.TryProcessSceneTransition))]
        private static class TransitionPatch
        {
            private static void Prefix(TransitionalSceneManager __instance, string __0)
            {
                try
                {
                    if (!IsTransitional(__instance))
                        return;
                    // Pode ser chamado a cada quadro: registra só quando muda.
                    string state = $"{__instance.GetIl2CppType().Name}.TryProcessSceneTransition('{__0}') carregada={__instance._loadedNextScene} processar={__instance._shouldProcessSceneTransition}";
                    if (state == _lastTransition)
                        return;
                    _lastTransition = state;
                    Mod.Log.Msg($"[intro] {Time.realtimeSinceStartup:F2}s {state}");
                }
                catch (Exception ex) { Mod.Log.Error($"[intro] {ex}"); }
            }
        }
    }
}
