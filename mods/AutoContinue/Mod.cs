using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

[assembly: MelonInfo(typeof(AutoContinue.Mod), "AutoContinue", "1.0.0", "gabriel")]
[assembly: MelonGame("Eek", "House Party")]

namespace AutoContinue
{
    // Segue sozinho da primeira tela de carregamento para a segunda. A tela
    // só pergunta se houve clique depois de carregar e mostrar o aviso; o mod
    // responde "clicou" e deixa o próprio jogo seguir para a próxima cena.
    public class Mod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        private static bool _errorReported;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Log.Msg("Carregado.");
        }

        // Registra o erro uma vez, sem inundar o log a cada quadro.
        internal static void ReportError(Exception ex)
        {
            if (_errorReported)
                return;
            _errorReported = true;
            Log.Error($"Erro ao continuar o carregamento (o jogo volta a esperar o clique): {ex}");
        }
    }

    // LoadingScreenManager.WasButtonPressed tem o mesmo código nativo que
    // TransitionalSceneManager.UserInputForTransitionReceived (o jogo funde as
    // duas funções). Corrigir pelos dois métodos põe dois desvios no mesmo
    // código e derruba o jogo, inclusive junto do SkipIntro, que corrige o da
    // base. Por isso o patch é no da base e age só na tela de carregamento.
    [HarmonyPatch(typeof(TransitionalSceneManager), nameof(TransitionalSceneManager.UserInputForTransitionReceived))]
    internal static class ContinuePatch
    {
        private static void Postfix(TransitionalSceneManager __instance, ref bool __result)
        {
            try
            {
                if (__result || !Guard.Is<LoadingScreenManager>(__instance))
                    return;
                var screen = new LoadingScreenManager(__instance.Pointer);
                var prompt = screen._ClickToContinueObject;
                // Só com o carregamento pronto e o aviso de clique na tela.
                if (screen._loaded && prompt != null && prompt.activeInHierarchy)
                    __result = true;
            }
            catch (Exception ex) { Mod.ReportError(ex); }
        }
    }
}
