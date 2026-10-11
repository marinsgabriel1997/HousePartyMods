using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using HarmonyLib;
using Il2Cpp;
using Il2CppEekCharacterEngine;

namespace NewTalkMarker
{
    // Estado do mod guardado com um save: falas conhecidas e marcas, pela chave estável.
    internal sealed class SavedState
    {
        public int Version { get; set; } = 1;
        public List<string> Baselined { get; set; } = new();
        public List<string> Known { get; set; } = new();
        public List<string> New { get; set; } = new();
        public List<string> Seen { get; set; } = new();
    }

    // Grava o estado do mod num arquivo ao lado de cada save, lê ao carregar e
    // apaga com o save. O arquivo .save do jogo não é alterado.
    internal static class SaveCycle
    {
        // Extensão própria, para o jogo não tratar o arquivo do mod como save.
        public const string Extension = ".newtalkmarker";

        // No salvamento automático, o jogo só grava o .save depois (em
        // SaveAutoSaveToFile); o estado do momento do Save espera até lá.
        private static string _pendingAuto;

        public static void Init()
        {
            SaveLoadManager.add_OnSaveGameLoaded(new Action<Il2CppSystem.Object>(_ =>
            {
                try { Scanner.OnSaveGameLoaded(); }
                catch (Exception ex) { Mod.ReportError("OnSaveGameLoaded", ex); }
            }));
        }

        // O nome que o jogo usa pode vir com ou sem extensão.
        private static string StatePath(string saveName)
        {
            string folder = SaveLoadManager.GetFileSaveFolder();
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(saveName))
                return null;
            string name = saveName.EndsWith(".save", StringComparison.OrdinalIgnoreCase)
                ? saveName.Substring(0, saveName.Length - ".save".Length)
                : saveName;
            return Path.Combine(folder, name + Extension);
        }

        private static bool IsAutoSave(string saveName) =>
            string.Equals(saveName, SaveLoadManager.AutoSaveName, StringComparison.OrdinalIgnoreCase);

        public static void OnSaved(string saveName)
        {
            string json = JsonSerializer.Serialize(Scanner.Snapshot());
            if (IsAutoSave(saveName))
            {
                _pendingAuto = json;
                Mod.Diag($"[save] estado do salvamento automático guardado até o jogo gravar o arquivo");
                return;
            }
            Write(saveName, json);
        }

        public static void OnAutoSaveWritten()
        {
            string json = Interlocked.Exchange(ref _pendingAuto, null);
            if (json != null)
                Write(SaveLoadManager.AutoSaveName, json);
        }

        private static void Write(string saveName, string json)
        {
            string path = StatePath(saveName);
            if (path == null)
                return;
            File.WriteAllText(path, json);
            Mod.Diag($"[save] estado gravado: {path}");
        }

        // Sem arquivo (save antigo ou de antes do mod) ou com arquivo ilegível:
        // nulo, e o ponto de partida vale.
        public static SavedState Read(string saveName)
        {
            string path = StatePath(saveName);
            if (path == null || !File.Exists(path))
            {
                Mod.Diag($"[save] sem estado do mod para \"{saveName}\": ponto de partida");
                return null;
            }
            try
            {
                SavedState state = JsonSerializer.Deserialize<SavedState>(File.ReadAllText(path));
                Mod.Diag($"[save] estado lido: {path}");
                return state;
            }
            catch (Exception ex)
            {
                Mod.ReportError($"leitura de {path}", ex);
                return null;
            }
        }

        public static void Delete(string saveName)
        {
            if (IsAutoSave(saveName))
                _pendingAuto = null;
            string path = StatePath(saveName);
            if (path == null || !File.Exists(path))
                return;
            File.Delete(path);
            Mod.Diag($"[save] estado apagado: {path}");
        }
    }

    // Os métodos de SaveLoadManager são virtuais e a versão base pode ser uma
    // função nativa compartilhada com outras classes; os patches ficam nas
    // versões de HousePartySaveLoadManager e conferem o tipo real.
    // Confirmado no jogo: salvamento manual, rápido e automático passam por
    // Save, na thread principal, com o nome sem extensão.

    [HarmonyPatch(typeof(HousePartySaveLoadManager), nameof(HousePartySaveLoadManager.Save))]
    internal static class SavePatch
    {
        private static void Postfix(HousePartySaveLoadManager __instance, string filename, bool __result)
        {
            try
            {
                if (__result && Guard.Is<HousePartySaveLoadManager>(__instance))
                    SaveCycle.OnSaved(filename);
            }
            catch (Exception ex) { Mod.ReportError(nameof(SavePatch), ex); }
        }
    }

    // Estático: sem instância para conferir o tipo.
    [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.SaveAutoSaveToFile))]
    internal static class SaveAutoSaveToFilePatch
    {
        private static void Postfix()
        {
            try { SaveCycle.OnAutoSaveWritten(); }
            catch (Exception ex) { Mod.ReportError(nameof(SaveAutoSaveToFilePatch), ex); }
        }
    }

    [HarmonyPatch(typeof(HousePartySaveLoadManager), nameof(HousePartySaveLoadManager.Load))]
    internal static class LoadPatch
    {
        private static void Prefix(HousePartySaveLoadManager __instance, string fileName)
        {
            try
            {
                if (Guard.Is<HousePartySaveLoadManager>(__instance))
                    Scanner.BeginLoad(SaveCycle.Read(fileName));
            }
            catch (Exception ex) { Mod.ReportError(nameof(LoadPatch), ex); }
        }
    }

    [HarmonyPatch(typeof(HousePartySaveLoadManager), nameof(HousePartySaveLoadManager.Delete))]
    internal static class DeletePatch
    {
        private static void Postfix(HousePartySaveLoadManager __instance, string saveName)
        {
            try
            {
                if (Guard.Is<HousePartySaveLoadManager>(__instance))
                    SaveCycle.Delete(saveName);
            }
            catch (Exception ex) { Mod.ReportError(nameof(DeletePatch), ex); }
        }
    }
}
