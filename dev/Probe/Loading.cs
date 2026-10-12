using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

namespace Probe
{
    // Fase 1 do p50 (patches aplicados à mão em Init): registra quando a primeira tela de carregamento
    // (LoadingScreenManager) pergunta se houve clique e em que estado está, e
    // testa se responder "clicou" com a tela pronta leva à partida pelo
    // caminho do próprio jogo. Probe.AutoContinue liga o teste.
    internal static class Loading
    {
        private static MelonPreferences_Entry<bool> _continue;
        private static string _lastState;

        public static void Init(HarmonyLib.Harmony harmony)
        {
            var category = MelonPreferences.CreateCategory("Probe");
            _continue = category.CreateEntry("AutoContinue", false, display_name: "Continuar após carregar (teste)",
                description: "Responde \"clicou\" na primeira tela de carregamento quando ela estiver pronta.");

            // O jogo caiu na abertura com estes patches: confere se algum método
            // da tela divide o código nativo com os da base das aberturas, já
            // corrigidos pelo SkipIntro. Só aplica os patches que não dividem.
            var shared = new System.Collections.Generic.HashSet<IntPtr>();
            foreach (var name in new[] { "UserInputForTransitionReceived", "TryProcessSceneTransition", "TryProcessButtonPressForFadeOut", "Update" })
            {
                IntPtr code = NativeCode(typeof(TransitionalSceneManager), name);
                shared.Add(code);
                Mod.Log.Msg($"[carga] TransitionalSceneManager.{name} nativo=0x{code.ToInt64():X}");
            }
            // WasButtonPressed divide o código com UserInputForTransitionReceived
            // (confirmado: mesmo endereço). Patch nos dois caminhos derruba o
            // jogo; o ButtonPatch corrige o da base e filtra pela classe real.
            Patch(harmony, shared, nameof(LoadingScreenManager.WasButtonPressed), null, postfix: true);
            Patch(harmony, shared, nameof(LoadingScreenManager.TranslateContinuePrompt), typeof(PromptPatch), postfix: false);
            Patch(harmony, shared, nameof(LoadingScreenManager.LoadTransitionScene), typeof(TransitionPatch), postfix: false);
            Patch(harmony, shared, nameof(LoadingScreenManager.WaitToStartMainGameScene), typeof(WaitPatch), postfix: false);
        }

        // Endereço do código nativo: primeiro campo do MethodInfo do IL2CPP.
        private static unsafe IntPtr NativeCode(Type type, string method)
        {
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
            {
                if (!field.Name.StartsWith($"NativeMethodInfoPtr_{method}_"))
                    continue;
                var info = (IntPtr)field.GetValue(null);
                return info == IntPtr.Zero ? IntPtr.Zero : *(IntPtr*)info;
            }
            return IntPtr.Zero;
        }

        private static void Patch(HarmonyLib.Harmony harmony, System.Collections.Generic.HashSet<IntPtr> shared, string method, Type patch, bool postfix)
        {
            IntPtr code = NativeCode(typeof(LoadingScreenManager), method);
            bool skip = shared.Contains(code);
            Mod.Log.Msg($"[carga] LoadingScreenManager.{method} nativo=0x{code.ToInt64():X}{(skip ? " DIVIDIDO, sem patch" : "")}");
            if (skip)
                return;
            var hook = new HarmonyMethod(AccessTools.Method(patch, postfix ? "Postfix" : "Prefix"));
            var target = AccessTools.Method(typeof(LoadingScreenManager), method);
            if (postfix)
                harmony.Patch(target, postfix: hook);
            else
                harmony.Patch(target, prefix: hook);
        }

        // Métodos nativos podem ser compartilhados entre classes; confere a classe real.
        private static bool IsLoadingScreen(LoadingScreenManager obj)
        {
            if (obj == null || obj.Pointer == IntPtr.Zero)
                return false;
            IntPtr actual = IL2CPP.il2cpp_object_get_class(obj.Pointer);
            return IL2CPP.il2cpp_class_is_assignable_from(Il2CppClassPointerStore<LoadingScreenManager>.NativeClassPtr, actual);
        }

        private static bool PromptVisible(LoadingScreenManager screen)
        {
            var prompt = screen._ClickToContinueObject;
            return prompt != null && prompt.activeInHierarchy;
        }

        private static void Report(string what)
        {
            Mod.Log.Msg($"[carga] {Time.realtimeSinceStartup:F2}s {what}");
        }

        [HarmonyPatch(typeof(TransitionalSceneManager), nameof(TransitionalSceneManager.UserInputForTransitionReceived))]
        private static class ButtonPatch
        {
            private static void Postfix(TransitionalSceneManager __instance, ref bool __result)
            {
                try
                {
                    if (__instance == null || !IsLoadingScreen(new LoadingScreenManager(__instance.Pointer)))
                        return;
                    var screen = new LoadingScreenManager(__instance.Pointer);
                    bool loaded = screen._loaded;
                    bool prompt = PromptVisible(screen);
                    bool original = __result;
                    if (_continue.Value && !__result && loaded && prompt)
                        __result = true;
                    // Chamado a cada quadro: registra só quando muda.
                    string state = $"WasButtonPressed original={original} resposta={__result} loaded={loaded} aviso={prompt} teste={_continue.Value}";
                    if (state == _lastState)
                        return;
                    _lastState = state;
                    Report(state);
                }
                catch (Exception ex) { Mod.Log.Error($"[carga] {ex}"); }
            }
        }

        private static class PromptPatch
        {
            private static void Prefix(LoadingScreenManager __instance)
            {
                try { if (IsLoadingScreen(__instance)) Report($"TranslateContinuePrompt loaded={__instance._loaded} aviso={PromptVisible(__instance)}"); }
                catch (Exception ex) { Mod.Log.Error($"[carga] {ex}"); }
            }
        }

        private static class TransitionPatch
        {
            private static void Prefix(LoadingScreenManager __instance)
            {
                try { if (IsLoadingScreen(__instance)) Report($"LoadTransitionScene loaded={__instance._loaded} aviso={PromptVisible(__instance)}"); }
                catch (Exception ex) { Mod.Log.Error($"[carga] {ex}"); }
            }
        }

        private static class WaitPatch
        {
            private static void Prefix(LoadingScreenManager __instance)
            {
                try { if (IsLoadingScreen(__instance)) Report($"WaitToStartMainGameScene loaded={__instance._loaded} aviso={PromptVisible(__instance)}"); }
                catch (Exception ex) { Mod.Log.Error($"[carga] {ex}"); }
            }
        }
    }
}
