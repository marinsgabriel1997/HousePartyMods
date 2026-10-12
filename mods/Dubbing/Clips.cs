using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Il2CppEekCharacterEngine;
using Il2CppEekCharacterEngine.Components;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Dubbing
{
    // Dublagens em UserData/Dubbing/<idioma>/<história>/<personagem>/<clip>.ogg,
    // com os nomes do jogo: a história como em Mods/Stories, o personagem como o
    // objeto dele na cena e o nome do clip original.
    internal static class Clips
    {
        // Carregar um .ogg de 2 a 17 s levou 3 a 10 ms no jogo. Acima disto, a
        // fala toca o original.
        private const double TimeoutMs = 2000;

        public static string Root => Path.Combine(MelonEnvironment.UserDataDirectory, "Dubbing");

        // Clip do mod em uso por personagem (CAudio), para liberar o anterior
        // ao trocar de fala. Cada fala é carregada na hora: guardar todas
        // ocuparia centenas de MB por personagem.
        private static readonly Dictionary<IntPtr, AudioClip> _inUse = new();
        private static readonly HashSet<string> _reportedFiles = new(StringComparer.OrdinalIgnoreCase);

        public static List<string> Installed()
        {
            var languages = new List<string>();
            if (Directory.Exists(Root))
                foreach (string dir in Directory.GetDirectories(Root))
                    languages.Add(Path.GetFileName(dir));
            languages.Sort(StringComparer.OrdinalIgnoreCase);
            return languages;
        }

        // Os objetos do jogo são recriados a cada carregamento.
        public static void Reset() => _inUse.Clear();

        // Clip dublado de uma fala do personagem, ou null para tocar o original.
        public static AudioClip For(CAudio audio, string clipName)
        {
            string language = Mod.Language;
            if (language == null || string.IsNullOrEmpty(clipName))
                return null;
            string story = GameManager.GetActiveStoryName();
            string character = audio.Character?.gameObject?.name;
            if (string.IsNullOrEmpty(story) || string.IsNullOrEmpty(character))
                return null;

            string file = Path.Combine(Root, language, story, character, clipName + ".ogg");
            if (!File.Exists(file))
                return null;
            AudioClip clip = Load(file);
            if (clip == null)
                return null;
            Release(audio);
            _inUse[audio.Pointer] = clip;
            return clip;
        }

        // Libera o clip anterior do personagem, se nenhuma das fontes dele o
        // estiver usando e se não estiver na fila de descarga do próprio jogo
        // (_unloadQueue), que o descarregaria depois de destruído. Os que ficam
        // saem com os assets sem uso ao carregar a próxima cena.
        private static void Release(CAudio audio)
        {
            if (!_inUse.TryGetValue(audio.Pointer, out AudioClip previous) || previous == null)
                return;
            _inUse.Remove(audio.Pointer);
            if (UsedBy(audio._audio, previous) || UsedBy(audio._sfxAudio, previous) || Queued(audio, previous))
                return;
            UnityEngine.Object.Destroy(previous);
        }

        private static bool Queued(CAudio audio, AudioClip clip)
        {
            var queue = audio._unloadQueue;
            for (int i = 0; i < (queue?.Count ?? 0); i++)
                if (queue[i] != null && queue[i].Pointer == clip.Pointer)
                    return true;
            return false;
        }

        private static bool UsedBy(AudioSource source, AudioClip clip) =>
            source != null && source.clip != null && source.clip.Pointer == clip.Pointer;

        private static AudioClip Load(string file)
        {
            string url = new Uri(file).AbsoluteUri;
            var request = new UnityWebRequest(url, "GET", new DownloadHandlerAudioClip(url, AudioType.OGGVORBIS), null);
            try
            {
                var watch = Stopwatch.StartNew();
                request.SendWebRequest();
                // Bloqueia: a fala já começou a tocar e a troca precisa ser no
                // mesmo quadro para o original não ser ouvido.
                while (!request.isDone && watch.Elapsed.TotalMilliseconds < TimeoutMs)
                    System.Threading.Thread.Sleep(1);
                if (!request.isDone)
                {
                    request.Abort();
                    ReportFile(file, $"sem resposta em {TimeoutMs:0} ms");
                    return null;
                }
                AudioClip clip = request.result == UnityWebRequest.Result.Success
                    ? request.downloadHandler.Cast<DownloadHandlerAudioClip>().audioClip
                    : null;
                if (clip == null || clip.length <= 0f)
                {
                    // Arquivo que não é Ogg Vorbis válido: a leitura dá certo,
                    // mas o clip vem vazio (confirmado com bytes aleatórios).
                    ReportFile(file, request.result == UnityWebRequest.Result.Success
                        ? "não é um Ogg Vorbis válido" : $"{request.result} {request.error}");
                    return null;
                }
                clip.name = Path.GetFileNameWithoutExtension(file);
                return clip;
            }
            finally { request.Dispose(); }
        }

        // Cada arquivo com problema é registrado uma vez.
        private static void ReportFile(string file, string reason)
        {
            if (_reportedFiles.Add(file))
                Mod.Log.Warning($"Não foi possível carregar {file} ({reason}); a fala toca o original.");
        }
    }
}
