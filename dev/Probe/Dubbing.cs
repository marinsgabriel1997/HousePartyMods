using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HarmonyLib;
using Il2Cpp;
using Il2CppEekCharacterEngine;
using Il2CppCrazyMinnow.SALSA;
using Il2CppEekCharacterEngine.Components;
using Il2CppEekEvents.Characters;
using Il2CppEekEvents.Dialogues;
using Il2CppInterop.Runtime;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Probe
{
    // Fase 1 do p70: como trocar a voz de uma fala por um .ogg externo.
    // Registra o caminho da fala no CAudio (OnDialogueStarted,
    // TryPlayDialogueAudio e a corrotina, a fila de substituição do TTS) e da
    // conversa de fundo com voz, e testa a troca conforme Probe.DubMode:
    // - off: só registra;
    // - override: no OnDialogueStarted de quem fala, OverrideDialogueAudio com
    //   o clip externo;
    // - add: idem, por AddDialogueOverrideClip;
    // - source: troca o clip no AudioSource do personagem quando a corrotina
    //   da fala termina com o original tocando.
    // Probe.DubChatter troca o clip da conversa de fundo. Probe.DubPreload
    // carrega antes os .ogg da história ativa; sem ele, carrega na hora,
    // bloqueando (mede se dá). Probe.DubCompressed mantém o Vorbis comprimido
    // na memória. Arquivos em
    // UserData/Dubbing/pt-BR/<história>/<personagem>/<clip>.ogg.
    internal static class Dubbing
    {
        private const string Language = "pt-BR";
        private const float SyncTimeoutMs = 3000f;
        private const int ParallelLoads = 8;

        private static MelonPreferences_Entry<string> _mode;
        private static MelonPreferences_Entry<bool> _chatter;
        private static MelonPreferences_Entry<bool> _preload;
        private static MelonPreferences_Entry<bool> _compressed;

        // Clips carregados, por caminho do arquivo. Sobrevivem aos carregamentos
        // de partida (DontUnloadUnusedAsset); não são objetos da cena.
        private static readonly Dictionary<string, AudioClip> _clips = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Queue<string> _toLoad = new();
        private static readonly List<(string Path, UnityWebRequest Request, Stopwatch Watch)> _loading = new();
        private static string _preloadedStory;
        private static Stopwatch _preloadWatch;
        private static int _preloadTotal;

        // Fala em andamento, para registrar o que o AudioSource toca.
        private static CAudio _watched;
        private static string _expected;
        private static string _lastWatch;
        private static float _watchStart;

        // Teste da boca (SALSA): original não troca; nada só troca o clip do
        // AudioSource; campo também põe o clip em Salsa3D.audioClip; set usa
        // Salsa3D.SetAudioClip.
        private static readonly string[] LipVariants = { "original", "nada", "campo", "set" };
        private static int _lipCycle;
        private static string _lipVariant;
        private static float _lipMax;
        private static bool _lipLogged;

        public static void Init(HarmonyLib.Harmony harmony)
        {
            var category = MelonPreferences.CreateCategory("Probe");
            _mode = category.CreateEntry("DubMode", "off", display_name: "Dublagem: modo (teste)",
                description: "off, override, add ou source.");
            _chatter = category.CreateEntry("DubChatter", false, display_name: "Dublagem: conversas de fundo (teste)",
                description: "Troca o clip em PlayVoicedBackgroundChatter.");
            _preload = category.CreateEntry("DubPreload", false, display_name: "Dublagem: carregar antes (teste)",
                description: "Carrega os .ogg da história ao entrar na partida; sem isso, carrega na hora da fala.");
            _compressed = category.CreateEntry("DubCompressed", true, display_name: "Dublagem: manter comprimido (teste)",
                description: "Mantém o Vorbis comprimido na memória (DownloadHandlerAudioClip.compressed).");

            ReportShared();
            Patch(harmony, typeof(CAudio), nameof(CAudio.OnDialogueStarted), nameof(StartedPrefix));
            Patch(harmony, typeof(CAudio), nameof(CAudio.TryPlayDialogueAudio), nameof(TryPlayPrefix));
            Patch(harmony, typeof(CAudio), nameof(CAudio.SendDialogueTextForTextToSpeech), nameof(TtsPrefix));
            Patch(harmony, typeof(CAudio), nameof(CAudio.OverrideDialogueAudio), nameof(OverridePrefix));
            Patch(harmony, typeof(CAudio), nameof(CAudio.AddDialogueOverrideClip), nameof(AddPrefix));
            Patch(harmony, typeof(CAudio), nameof(CAudio.CheckDialogueAudioQueue), nameof(QueuePrefix));
            Patch(harmony, typeof(CAudio), nameof(CAudio.PlayVoicedBackgroundChatter), nameof(ChatterPrefix));
            Patch(harmony, typeof(CAudio._TryPlayDialogueAudio_d__95), nameof(CAudio._TryPlayDialogueAudio_d__95.MoveNext),
                nameof(MoveNextPostfix), postfix: true);
            Log($"pronto, modo={_mode.Value} conversas={_chatter.Value} carregar antes={_preload.Value} comprimido={_compressed.Value}");
        }

        private static void Patch(HarmonyLib.Harmony harmony, Type type, string method, string hook, bool postfix = false)
        {
            var target = AccessTools.Method(type, method);
            var patch = new HarmonyMethod(AccessTools.Method(typeof(Dubbing), hook));
            if (postfix)
                harmony.Patch(target, postfix: patch);
            else
                harmony.Patch(target, prefix: patch);
        }

        public static void Tick()
        {
            if (PlayerCharacter.Player == null)
            {
                _watched = null;
                return;
            }
            Preload();
            Watch();
        }

        // ---------- Arquivos ----------

        private static string Root => Path.Combine(MelonEnvironment.UserDataDirectory, "Dubbing", Language);

        private static string FileFor(string story, string character, string clip) =>
            Path.Combine(Root, story ?? "", character ?? "", clip + ".ogg");

        private static string StoryName()
        {
            try { return GameManager.GetActiveStoryName(); }
            catch (Exception ex) { return $"(erro {ex.GetType().Name})"; }
        }

        private static string CharacterName(CAudio audio)
        {
            try { return audio.Character?.gameObject?.name; }
            catch (Exception ex) { return $"(erro {ex.GetType().Name})"; }
        }

        private static void Preload()
        {
            string story = StoryName();
            if (_preload.Value && story != _preloadedStory)
            {
                _preloadedStory = story;
                string dir = Path.Combine(Root, story ?? "");
                if (Directory.Exists(dir))
                {
                    foreach (string file in Directory.GetFiles(dir, "*.ogg", SearchOption.AllDirectories))
                        if (!_clips.ContainsKey(file))
                            _toLoad.Enqueue(file);
                    _preloadTotal = _toLoad.Count;
                    _preloadWatch = Stopwatch.StartNew();
                    Log($"carregar antes: {_preloadTotal} arquivos em {dir}");
                }
                else
                    Log($"carregar antes: pasta ausente {dir}");
            }

            while (_loading.Count < ParallelLoads && _toLoad.Count > 0)
            {
                string file = _toLoad.Dequeue();
                _loading.Add((file, Start(file), Stopwatch.StartNew()));
            }

            for (int i = _loading.Count - 1; i >= 0; i--)
            {
                var (file, request, watch) = _loading[i];
                if (!request.isDone)
                    continue;
                _loading.RemoveAt(i);
                Finish(file, request, watch, quiet: true);
            }

            if (_preloadWatch != null && _toLoad.Count == 0 && _loading.Count == 0)
            {
                long bytes = 0;
                foreach (var clip in _clips.Values)
                    if (clip != null) bytes += (long)clip.samples * clip.channels * 4;
                Log($"carregar antes: {_preloadTotal} em {_preloadWatch.Elapsed.TotalMilliseconds:0} ms; " +
                    $"{_clips.Count} clips na memória, ~{bytes / 1048576.0:0.0} MB em PCM float");
                _preloadWatch = null;
            }
        }

        private static UnityWebRequest Start(string file)
        {
            string url = new Uri(file).AbsoluteUri;
            var handler = new DownloadHandlerAudioClip(url, AudioType.OGGVORBIS) { compressed = _compressed.Value };
            var request = new UnityWebRequest(url, "GET", handler, null);
            request.SendWebRequest();
            return request;
        }

        private static AudioClip Finish(string file, UnityWebRequest request, Stopwatch watch, bool quiet)
        {
            AudioClip clip = null;
            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                    clip = request.downloadHandler.Cast<DownloadHandlerAudioClip>().audioClip;
                if (clip == null)
                    Log($"falha ao carregar {file}: {request.result} {request.error}");
                else
                {
                    clip.name = Path.GetFileNameWithoutExtension(file);
                    clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    _clips[file] = clip;
                    if (!quiet)
                        Log($"carregado {clip.name} em {watch.Elapsed.TotalMilliseconds:0.0} ms: {clip.length:0.00}s " +
                            $"{clip.frequency} Hz {clip.channels} canal(is) estado={clip.loadState} tipo={clip.loadType}");
                }
            }
            finally { request.Dispose(); }
            return clip;
        }

        // Carrega o arquivo, se existir: do cache, ou na hora, bloqueando.
        private static AudioClip Get(string file)
        {
            if (_clips.TryGetValue(file, out AudioClip cached) && cached != null)
                return cached;
            if (!File.Exists(file))
                return null;
            var watch = Stopwatch.StartNew();
            UnityWebRequest request = Start(file);
            while (!request.isDone && watch.Elapsed.TotalMilliseconds < SyncTimeoutMs)
                System.Threading.Thread.Sleep(1);
            if (!request.isDone)
            {
                Log($"carregar na hora: sem resposta em {SyncTimeoutMs:0} ms ({file})");
                request.Abort();
                request.Dispose();
                return null;
            }
            return Finish(file, request, watch, quiet: false);
        }

        // ---------- Diálogo ----------

        private static string Describe(IDialogue dialogue, int alt)
        {
            Dialogue d = dialogue?.TryCast<Dialogue>();
            return d == null ? $"(diálogo {(dialogue == null ? "nulo" : "não Dialogue")}) alt={alt}"
                : $"#{d.ID} alt={alt} falante={d.CurrentSpeaker} para={d.SpeakingToCharacterName} \"{Shorten(d.Text)}\"";
        }

        private static string ClipName(CAudio audio, IDialogue dialogue, int alt)
        {
            Dialogue d = dialogue?.TryCast<Dialogue>();
            if (d == null)
                return null;
            // Hipótese a conferir no log: prefixo + id, e ".alt" nas variantes.
            return $"{audio._dialoguePrefix}{d.ID}{(alt > 0 ? $".{alt}" : "")}";
        }

        // Chamado em todos os CAudio a cada fala (rodada 1); só o de quem fala
        // interessa. Nos modos override e add, testa a fila de substituição.
        private static void StartedPrefix(CAudio __instance, CharacterBase character, IDialogue dialogue, int altId)
        {
            try
            {
                if (!Guard.Is<CAudio>(__instance))
                    return;
                string self = CharacterName(__instance);
                if (self != character?.gameObject?.name)
                    return;
                string clip = ClipName(__instance, dialogue, altId);
                string file = FileFor(StoryName(), self, clip);
                Log($"OnDialogueStarted em {self} {Describe(dialogue, altId)} clip previsto={clip} existe={File.Exists(file)} " +
                    $"fila={__instance.GetDialogueOverrideQueueCount}");

                _watched = __instance;
                _expected = clip;
                _lastWatch = null;
                _watchStart = Time.realtimeSinceStartup;
                _lipVariant = null;

                string mode = _mode.Value;
                if (mode != "override" && mode != "add")
                    return;
                AudioClip ours = Get(file);
                if (ours == null)
                    return;
                var result = new TTSResult { Name = clip, RequestId = 0, SessionId = 0, Clip = ours };
                if (mode == "override")
                    __instance.OverrideDialogueAudio(result);
                else
                    __instance.AddDialogueOverrideClip(result);
                Log($"  {mode}: enviado {ours.name} ({ours.length:0.00}s), fila agora={__instance.GetDialogueOverrideQueueCount}");
            }
            catch (Exception ex) { Mod.Log.Error($"[dub] {ex}"); }
        }

        private static void TryPlayPrefix(CAudio __instance, IDialogue dialogue, int alternateId)
        {
            try { if (Guard.Is<CAudio>(__instance)) Log($"TryPlayDialogueAudio em {CharacterName(__instance)} {Describe(dialogue, alternateId)}"); }
            catch (Exception ex) { Mod.Log.Error($"[dub] {ex}"); }
        }

        // Na rodada 1, a corrotina espera o carregamento no primeiro passo e, no
        // fim, o clipID já toca no AudioSource. No modo source, troca ali, no
        // mesmo quadro em que o original começou.
        private static void MoveNextPostfix(CAudio._TryPlayDialogueAudio_d__95 __instance, bool __result)
        {
            try
            {
                if (!Guard.Is<CAudio._TryPlayDialogueAudio_d__95>(__instance))
                    return;
                CAudio audio = __instance.__4__this;
                AudioSource source = audio?._audio;
                string clipID = __instance._clipID_5__2;
                Log($"  corrotina estado={__instance.__1__state} continua={__result} clipID={clipID} " +
                    $"tocando={source?.clip?.name}({source?.isPlaying}) fila={audio?.GetDialogueOverrideQueueCount}");
                if (__result || _mode.Value != "source" || source == null || !source.isPlaying || source.clip?.name != clipID)
                    return;
                AudioClip original = source.clip;
                AudioClip ours = Get(FileFor(StoryName(), CharacterName(audio), clipID));
                if (ours == null)
                    return;

                // Boca: alterna a cada fala como avisar o SALSA do clip novo e
                // compara, no fim da fala, o quanto ela abriu (average).
                _lipVariant = LipVariants[_lipCycle++ % LipVariants.Length];
                _lipMax = 0f;
                _lipLogged = false;
                Salsa3D salsa = audio._salsa;
                var probe = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<float>(256);
                bool readable = ours.GetData(probe, 0);
                Log($"  boca: variante={_lipVariant} salsa={(salsa == null ? "nulo" : "ok")} " +
                    $"mesma fonte={salsa?.audioSrc?.Pointer == source.Pointer} clip do salsa={salsa?.audioClip?.name} " +
                    $"GetData={readable} amostras={ours.samples}");
                if (_lipVariant == "original")
                    return;
                if (_lipVariant == "set" && salsa != null)
                    salsa.SetAudioClip(ours);
                if (_lipVariant == "campo" && salsa != null)
                    salsa.audioClip = ours;
                source.clip = ours;
                source.Play();
                Log($"  source: trocado {clipID} ({original.length:0.00}s) por {ours.name} ({ours.length:0.00}s) " +
                    $"tocando={source.clip?.name}({source.isPlaying}) clip do salsa={salsa?.audioClip?.name} " +
                    $"fonte do salsa toca={salsa?.audioSrc?.clip?.name}");
            }
            catch (Exception ex) { Mod.Log.Error($"[dub] {ex}"); }
        }

        private static void TtsPrefix(CAudio __instance, long requestId, string characterName, string text)
        {
            try { if (Guard.Is<CAudio>(__instance)) Log($"SendDialogueTextForTextToSpeech req={requestId} {characterName} \"{Shorten(text)}\""); }
            catch (Exception ex) { Mod.Log.Error($"[dub] {ex}"); }
        }

        private static void OverridePrefix(CAudio __instance, TTSResult result)
        {
            try { if (Guard.Is<CAudio>(__instance)) Log($"OverrideDialogueAudio {result?.Name} req={result?.RequestId} sessão={result?.SessionId} clip={result?.Clip?.name}"); }
            catch (Exception ex) { Mod.Log.Error($"[dub] {ex}"); }
        }

        private static void AddPrefix(CAudio __instance, TTSResult result)
        {
            try { if (Guard.Is<CAudio>(__instance)) Log($"AddDialogueOverrideClip {result?.Name} req={result?.RequestId} sessão={result?.SessionId} clip={result?.Clip?.name}"); }
            catch (Exception ex) { Mod.Log.Error($"[dub] {ex}"); }
        }

        // Provavelmente chamado a cada quadro: só registra com fila.
        private static void QueuePrefix(CAudio __instance)
        {
            try
            {
                if (Guard.Is<CAudio>(__instance) && __instance.GetDialogueOverrideQueueCount > 0)
                    Log($"CheckDialogueAudioQueue em {CharacterName(__instance)} fila={__instance.GetDialogueOverrideQueueCount} " +
                        $"tocando={__instance._audio?.clip?.name}({__instance._audio?.isPlaying})");
            }
            catch (Exception ex) { Mod.Log.Error($"[dub] {ex}"); }
        }

        private static void ChatterPrefix(CAudio __instance, BackgroundChatter chatter, ref AudioClip clip)
        {
            try
            {
                if (!Guard.Is<CAudio>(__instance))
                    return;
                string character = CharacterName(__instance);
                string file = clip == null ? null : FileFor(StoryName(), character, clip.name);
                Log($"PlayVoicedBackgroundChatter em {character} #{chatter?.Id} clip={clip?.name} ({clip?.length:0.00}s) " +
                    $"\"{Shorten(chatter?.Text)}\" arquivo existe={file != null && File.Exists(file)}");
                if (!_chatter.Value || file == null)
                    return;
                AudioClip ours = Get(file);
                if (ours == null)
                    return;
                clip = ours;
                Log($"  conversa: trocado por {ours.name} ({ours.length:0.00}s)");
            }
            catch (Exception ex) { Mod.Log.Error($"[dub] {ex}"); }
        }

        // Registra o que o AudioSource do personagem toca durante a fala e, no
        // modo source, troca o clip original pelo externo.
        private static void Watch()
        {
            if (_watched == null)
                return;
            if (Time.realtimeSinceStartup - _watchStart > 30f)
            {
                _watched = null;
                return;
            }
            AudioSource source = _watched._audio;
            if (source == null)
                return;
            string playing = source.clip?.name;

            Salsa3D salsa = _watched._salsa;
            if (_lipVariant != null && salsa != null)
            {
                if (source.isPlaying)
                    _lipMax = Math.Max(_lipMax, salsa.average);
                else if (!_lipLogged && Time.realtimeSinceStartup - _watchStart > 0.5f)
                {
                    _lipLogged = true;
                    Log($"  boca: variante={_lipVariant} average máximo={_lipMax:0.0000} clip={playing}");
                }
            }

            string state = $"tocando={playing}({source.isPlaying}) principal={_watched.IsMainDialoguePlaying} " +
                           $"falando={_watched.IsSpeaking} fila={_watched.GetDialogueOverrideQueueCount}";
            if (state == _lastWatch)
                return;
            _lastWatch = state;
            Log($"  +{Time.realtimeSinceStartup - _watchStart:0.00}s {state}");
        }

        // ---------- Métodos fundidos ----------

        // Endereço do código nativo de cada método patcheado, comparado com os
        // de todos os métodos do assembly do CAudio.
        private static unsafe void ReportShared()
        {
            var targets = new[]
            {
                (typeof(CAudio), "OnDialogueStarted"), (typeof(CAudio), "TryPlayDialogueAudio"),
                (typeof(CAudio), "SendDialogueTextForTextToSpeech"), (typeof(CAudio), "OverrideDialogueAudio"),
                (typeof(CAudio), "AddDialogueOverrideClip"), (typeof(CAudio), "CheckDialogueAudioQueue"),
                (typeof(CAudio), "PlayVoicedBackgroundChatter"), (typeof(CAudio._TryPlayDialogueAudio_d__95), "MoveNext"),
            };
            var codes = new Dictionary<IntPtr, string>();
            foreach (var (type, method) in targets)
            {
                IntPtr code = NativeCode(type, method);
                codes[code] = $"{type.Name}.{method}";
            }
            var shared = new Dictionary<IntPtr, List<string>>();
            foreach (Type type in typeof(CAudio).Assembly.GetTypes())
            {
                System.Reflection.FieldInfo[] fields;
                try { fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); }
                catch { continue; }
                foreach (var field in fields)
                {
                    if (!field.Name.StartsWith("NativeMethodInfoPtr_"))
                        continue;
                    IntPtr info;
                    try { info = (IntPtr)field.GetValue(null); }
                    catch { break; }
                    if (info == IntPtr.Zero)
                        continue;
                    IntPtr code = *(IntPtr*)info;
                    if (!codes.ContainsKey(code))
                        continue;
                    if (!shared.TryGetValue(code, out var list))
                        shared[code] = list = new List<string>();
                    list.Add($"{type.Name}.{field.Name.Substring("NativeMethodInfoPtr_".Length)}");
                }
            }
            foreach (var (code, name) in codes)
            {
                var list = shared.TryGetValue(code, out var l) ? l : new List<string>();
                Log($"nativo {name}=0x{code.ToInt64():X}{(list.Count > 1 ? $" DIVIDIDO com {list.Count - 1}: {string.Join(", ", list)}" : "")}");
            }
        }

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

        private static void Log(string message) => Mod.Log.Msg($"[dub] {message}");

        private static string Shorten(string text) =>
            text == null ? "" : text.Length <= 60 ? text : text.Substring(0, 57) + "...";
    }

    // Métodos nativos podem ser compartilhados entre classes; confere a classe real.
    internal static class Guard
    {
        public static bool Is<T>(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase obj) where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
        {
            if (obj == null || obj.Pointer == IntPtr.Zero)
                return false;
            IntPtr actual = IL2CPP.il2cpp_object_get_class(obj.Pointer);
            return IL2CPP.il2cpp_class_is_assignable_from(Il2CppClassPointerStore<T>.NativeClassPtr, actual);
        }
    }
}
