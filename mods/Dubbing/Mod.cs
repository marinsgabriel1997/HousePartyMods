using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Il2CppEekCharacterEngine.Components;
using Il2CppEekEvents.Characters;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Dubbing.Mod), "Dubbing", "0.1.0", "gabriel")]
[assembly: MelonGame("Eek", "House Party")]

namespace Dubbing
{
    // Toca dublagens em .ogg no lugar das vozes originais, na hora de tocar,
    // sem alterar os bundles do jogo. Fala sem arquivo toca o original.
    public class Mod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        private static MelonPreferences_Entry<string> _language;
        private static string _reportedLanguage;
        private static readonly HashSet<string> _reportedErrors = new();

        // Pasta da dublagem escolhida, ou null se desligada ou ausente.
        internal static string Language
        {
            get
            {
                string value = _language.Value?.Trim();
                if (string.IsNullOrEmpty(value))
                    return null;
                bool valid = value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && value != "." && value != ".."
                             && Directory.Exists(Path.Combine(Clips.Root, value));
                if (valid)
                    return value;
                if (_reportedLanguage != value)
                {
                    _reportedLanguage = value;
                    Log.Warning($"Dublagem \"{value}\" não encontrada em {Clips.Root}; as falas tocam o original.");
                }
                return null;
            }
        }

        // Registra cada erro uma vez por local, sem inundar o log a cada fala.
        internal static void ReportError(string where, Exception ex)
        {
            if (_reportedErrors.Add($"{where}:{ex.GetType().Name}"))
                Log.Error($"Erro em {where} (a fala toca o original): {ex}");
        }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            List<string> installed = Clips.Installed();
            string list = installed.Count == 0 ? "-" : string.Join(", ", installed);
            MelonPreferences_Category category = MelonPreferences.CreateCategory("Dubbing");
            _language = Lang.Entry(category.CreateEntry("Language", ""),
                "Dublagem",
                $"Nome da pasta da dublagem em UserData/Dubbing (instaladas ao abrir o jogo: {list}). Vazio desliga.",
                "Dubbing",
                $"Name of the dubbing folder in UserData/Dubbing (installed when the game started: {list}). Empty turns it off.");
            Log.Msg($"Carregado. Dublagens instaladas: {list}. Escolhida: {(string.IsNullOrEmpty(_language.Value) ? "nenhuma" : _language.Value)}.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) => Clips.Reset();

        public override void OnUpdate() => Lang.Tick();
    }

    // A corrotina da fala termina com o clip original já tocando no
    // AudioSource do personagem (_clipID = nome do clip). A troca ocorre no
    // mesmo quadro, antes de o original ser ouvido. A sincronia labial (SALSA)
    // lê o mesmo AudioSource e acompanha o clip novo.
    [HarmonyPatch(typeof(CAudio._TryPlayDialogueAudio_d__95), nameof(CAudio._TryPlayDialogueAudio_d__95.MoveNext))]
    internal static class DialoguePatch
    {
        private static void Postfix(CAudio._TryPlayDialogueAudio_d__95 __instance, bool __result)
        {
            try
            {
                if (__result || !Guard.Is<CAudio._TryPlayDialogueAudio_d__95>(__instance))
                    return;
                CAudio audio = __instance.__4__this;
                AudioSource source = audio?._audio;
                string clipName = __instance._clipID_5__2;
                if (source == null || !source.isPlaying || source.clip == null || source.clip.name != clipName)
                    return;
                AudioClip dubbed = Clips.For(audio, clipName);
                if (dubbed == null)
                    return;
                source.clip = dubbed;
                source.Play();
            }
            catch (Exception ex) { Mod.ReportError("diálogo", ex); }
        }
    }

    // A conversa de fundo com voz recebe o clip pronto.
    [HarmonyPatch(typeof(CAudio), nameof(CAudio.PlayVoicedBackgroundChatter))]
    internal static class ChatterPatch
    {
        private static void Prefix(CAudio __instance, BackgroundChatter chatter, ref AudioClip clip)
        {
            try
            {
                if (clip == null || !Guard.Is<CAudio>(__instance))
                    return;
                AudioClip dubbed = Clips.For(__instance, clip.name);
                if (dubbed != null)
                    clip = dubbed;
            }
            catch (Exception ex) { Mod.ReportError("conversa de fundo", ex); }
        }
    }
}
