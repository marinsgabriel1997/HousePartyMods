using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

[assembly: MelonInfo(typeof(SkipIntro.Mod), "SkipIntro", "0.1.0", "gabriel")]
[assembly: MelonGame("Eek", "House Party")]

namespace SkipIntro
{
    // Pula o vídeo da Eek! Games e o aviso ao abrir o jogo. As duas telas já
    // avançam quando o jogador aperta uma tecla; o mod responde "tecla
    // apertada" por elas e deixa o próprio jogo carregar a cena seguinte.
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
            Log.Error($"Erro ao pular a abertura (o jogo segue normalmente): {ex}");
        }
    }

    // UserInputForTransitionReceived só verifica se houve tecla; o Update da
    // tela chama a transição quando ela responde true. Outras telas herdam a
    // mesma base, por isso o patch age só nessas duas.
    [HarmonyPatch(typeof(TransitionalSceneManager), nameof(TransitionalSceneManager.UserInputForTransitionReceived))]
    internal static class SkipPatch
    {
        private static void Postfix(TransitionalSceneManager __instance, ref bool __result)
        {
            try
            {
                if (__result)
                    return;
                if (Guard.Is<EekGamesIntroManager>(__instance) || Guard.Is<DisclaimerManager>(__instance))
                    __result = true;
            }
            catch (Exception ex) { Mod.ReportError(ex); }
        }
    }
}
