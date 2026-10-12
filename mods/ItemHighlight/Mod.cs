using System;
using System.Collections.Generic;
using Il2CppEekCharacterEngine;
using Il2CppEekCharacterEngine.Canvas;
using Il2CppEekCharacterEngine.Components;
using Il2CppEekCharacterEngine.Interaction;
using Il2CppEekEvents.Stories;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ItemHighlight.Mod), "ItemHighlight", "0.1.0", "gabriel")]
[assembly: MelonGame("Eek", "House Party")]

namespace ItemHighlight
{
    // Duas teclas controlam, de forma independente, o destaque dos itens
    // interativos à vista: uma liga e desliga o nome, com cor por tipo de
    // ação; a outra passa pelos níveis de um brilho rosa pulsante nos
    // coletáveis. Assim o jogador não precisa passar o mouse em tudo.
    public class Mod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        private static MelonPreferences_Entry<bool>[] _show;
        private static MelonPreferences_Entry<bool> _glowUse;
        private static HotKey _labelsKey;
        private static HotKey _glowKey;

        internal static bool LabelsVisible;

        // Níveis do brilho, em ordem. Confirmado no jogo: 0,3 basta para itens
        // escuros e foscos, mas a garrafa de cerveja e a folha de papel só
        // aparecem bem a partir de 2,5. Por isso a tecla passa pelos níveis,
        // em vez de usar uma intensidade fixa.
        private static readonly float[] GlowLevels = { 0.3f, 0.6f, 1.2f, 2.5f, 5f };
        // -1: brilho desligado.
        private static int _glowLevel = -1;

        internal static bool GlowVisible => _glowLevel >= 0;
        internal static float GlowIntensity => GlowVisible ? GlowLevels[_glowLevel] : 0f;

        // Se a categoria está ligada na configuração.
        internal static bool Shows(Category category) => _show[(int)category].Value;
        internal static bool GlowUse => _glowUse.Value;

        private static readonly HashSet<string> _reportedErrors = new();

        // Registra cada erro uma vez por local, sem inundar o log a cada quadro.
        internal static void ReportError(string where, Exception ex)
        {
            if (_reportedErrors.Add($"{where}:{ex.GetType().Name}"))
                Log.Error($"Erro em {where} (o jogo segue normalmente): {ex}");
        }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            MelonPreferences_Category category = MelonPreferences.CreateCategory("ItemHighlight");
            var labelsKey = Lang.Entry(category.CreateEntry("LabelsKey", "H"),
                "Tecla dos nomes", "Tecla que mostra e esconde o nome dos itens (nome do KeyCode do Unity, por exemplo H, G, F6).",
                "Names key", "Key that shows and hides item names (Unity KeyCode name, for example H, G, F6).");
            var glowKey = Lang.Entry(category.CreateEntry("GlowKey", "J"),
                "Tecla do brilho", "Tecla que passa pelos níveis do brilho (0,3, 0,6, 1,2, 2,5 e 5) e depois o desliga (nome do KeyCode do Unity).",
                "Glow key", "Key that steps through the glow levels (0.3, 0.6, 1.2, 2.5 and 5) and then turns it off (Unity KeyCode name).");
            var startLabels = Lang.Entry(category.CreateEntry("StartLabels", false),
                "Nomes ligados ao abrir o jogo", "Se os nomes começam ligados ao abrir o jogo.",
                "Names on at startup", "Whether names start turned on when the game opens.");
            var startGlow = Lang.Entry(category.CreateEntry("StartGlow", false),
                "Brilho ligado ao abrir o jogo", "Se o brilho começa ligado, no primeiro nível, ao abrir o jogo.",
                "Glow on at startup", "Whether the glow starts on, at the first level, when the game opens.");
            _glowUse = Lang.Entry(category.CreateEntry("GlowUse", false),
                "Brilho também nos itens de uso", "Faz brilhar também os itens de uso (fogão, lixeira...), além dos coletáveis.",
                "Glow on usable items too", "Also makes usable items glow (stove, trash can...), besides collectibles.");
            // Na ordem de Category.
            _show = new[]
            {
                Lang.Entry(category.CreateEntry("ShowCollectible", true),
                    "Itens que podem ser pegos", "Destaca itens que podem ser pegos (nome dourado).",
                    "Items you can pick up", "Highlights items you can pick up (gold name)."),
                Lang.Entry(category.CreateEntry("ShowUse", true),
                    "Itens de uso", "Destaca itens com outras ações, como beber, ligar ou vasculhar (nome ciano).",
                    "Usable items", "Highlights items with other actions, such as drink, turn on or search (cyan name)."),
                Lang.Entry(category.CreateEntry("ShowOpenClose", false),
                    "Portas, gavetas e armários", "Destaca portas, gavetas e armários (nome verde-claro).",
                    "Doors, drawers and cabinets", "Highlights doors, drawers and cabinets (light green name)."),
                Lang.Entry(category.CreateEntry("ShowPosition", false),
                    "Lugares para sentar e deitar", "Destaca onde sentar, deitar ou subir (nome cinza-claro).",
                    "Places to sit and lie down", "Highlights where to sit, lie down or climb (light gray name)."),
                Lang.Entry(category.CreateEntry("ShowInspect", false),
                    "Itens só para examinar", "Destaca itens que só podem ser examinados (nome branco).",
                    "Inspect-only items", "Highlights items that can only be inspected (white name)."),
            };
            LabelsVisible = startLabels.Value;
            _glowLevel = startGlow.Value ? 0 : -1;
            _labelsKey = new HotKey("LabelsKey", labelsKey.Value, KeyCode.H);
            _glowKey = new HotKey("GlowKey", glowKey.Value, KeyCode.J);
            // A tecla nova vale na hora quando a opção muda com o jogo aberto.
            labelsKey.OnEntryValueChanged.Subscribe((_, value) => _labelsKey = new HotKey("LabelsKey", value, KeyCode.H));
            glowKey.OnEntryValueChanged.Subscribe((_, value) => _glowKey = new HotKey("GlowKey", value, KeyCode.J));
            Log.Msg($"Carregado. {_labelsKey.Name} mostra e esconde os nomes; {_glowKey.Name} passa pelos níveis do brilho.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Os itens da cena anterior foram destruídos; não guardar referências a eles.
            Glow.RestoreAll();
            Items.Clear();
        }

        public override void OnUpdate()
        {
            try
            {
                // Os nomes guardados estão no idioma anterior; lê de novo.
                if (Lang.Tick())
                {
                    Glow.RestoreAll();
                    Items.Clear();
                }
                // Em pausa e menus as teclas são ignoradas, para não alternar ao digitar o nome de um save.
                bool hidden = Highlighter.Hidden();
                if (!hidden && _labelsKey.Pressed())
                    LabelsVisible = !LabelsVisible;
                if (!hidden && _glowKey.Pressed())
                {
                    // Desligado → primeiro nível → ... → último nível → desligado.
                    _glowLevel = _glowLevel + 1 < GlowLevels.Length ? _glowLevel + 1 : -1;
                    Log.Msg(GlowVisible ? $"Brilho {GlowIntensity}." : "Brilho desligado.");
                }
                if ((LabelsVisible || GlowVisible) && !hidden)
                    Items.Tick();
                // Roda também com o brilho desligado, para devolver os itens ao normal.
                Glow.Tick(hidden);
            }
            catch (Exception ex) { ReportError(nameof(OnUpdate), ex); }
        }

        public override void OnGUI()
        {
            try { Highlighter.Draw(); }
            catch (Exception ex) { ReportError(nameof(Highlighter), ex); }
        }
    }

    // Leitura de uma tecla configurada.
    internal sealed class HotKey
    {
        private static bool _legacyInputBroken;
        private readonly KeyCode _key;
        private readonly UnityEngine.InputSystem.Key _newKey;

        public string Name => _key.ToString();

        public HotKey(string option, string name, KeyCode fallback)
        {
            if (!Enum.TryParse(name?.Trim(), true, out _key))
            {
                Mod.Log.Warning($"{option} \"{name}\" não é um KeyCode válido; usando {fallback}.");
                _key = fallback;
            }
            // Nomes iguais nos dois sistemas para letras e F1–F12; os demais ficam sem reserva.
            if (!Enum.TryParse(_key.ToString(), true, out _newKey))
                _newKey = UnityEngine.InputSystem.Key.None;
        }

        // O jogo pode ter só o Input System novo ativo; nesse caso o Input
        // antigo lança exceção e passamos a usar o novo.
        public bool Pressed()
        {
            if (!_legacyInputBroken)
            {
                try { return Input.GetKeyDown(_key); }
                catch (Exception ex)
                {
                    _legacyInputBroken = true;
                    Mod.Log.Msg($"Input antigo indisponível ({ex.GetType().Name}); usando o Input System.");
                }
            }
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && _newKey != UnityEngine.InputSystem.Key.None && keyboard[_newKey].wasPressedThisFrame;
        }
    }

    // Categoria de um item, da maior para a menor prioridade.
    internal enum Category { Collectible, Use, OpenClose, Position, Inspect }

    internal sealed class Entry
    {
        public InteractiveItem Item;
        public Renderer Renderer;
        public string Name;
        // Nulo: o item não é destacável (sem ações, personagem) ou ainda não foi lido.
        public Category? Category;
        public bool InSight;
        // Renderers que brilham (sem partículas) e se cada um já tinha um
        // MaterialPropertyBlock antes do brilho.
        public List<Renderer> GlowRenderers;
        public List<bool> HadBlock;
        public bool Glowing;
    }

    // Itens destacáveis. Ler as ações de todos de uma vez custou 11,8 ms no
    // jogo; por isso ações e linha de visão são calculadas em rodízio, alguns
    // itens por quadro, e guardadas por item.
    internal static class Items
    {
        private const float RefreshSeconds = 1f;
        private const int ReadsPerFrame = 10;
        private const int SightChecksPerFrame = 40;
        private const int MaxPassThrough = 4;

        // Todos os itens do jogo, na ordem do rodízio.
        public static readonly List<Entry> All = new();
        private static readonly Dictionary<IntPtr, Entry> _byPointer = new();
        private static float _nextRefresh;
        private static int _nextRead;
        private static int _nextSight;

        public static void Clear()
        {
            All.Clear();
            _byPointer.Clear();
            _nextRefresh = 0f;
            _nextRead = 0;
            _nextSight = 0;
        }

        public static void Tick()
        {
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                Refresh();
            }
            if (All.Count == 0)
                return;

            StoryData story = GameManager.GetActiveStory();
            for (int i = 0; i < Math.Min(ReadsPerFrame, All.Count); i++)
            {
                Entry entry = All[_nextRead++ % All.Count];
                try { Read(entry, story); }
                catch (Exception ex) { Mod.ReportError(nameof(Read), ex); }
            }

            Camera camera = Camera.main;
            if (camera == null)
                return;
            Vector3 eye = camera.transform.position;
            for (int i = 0; i < Math.Min(SightChecksPerFrame, All.Count); i++)
            {
                Entry entry = All[_nextSight++ % All.Count];
                try { entry.InSight = Shown(entry) && InSight(entry, camera, eye); }
                catch (Exception ex)
                {
                    entry.InSight = false;
                    Mod.ReportError(nameof(InSight), ex);
                }
            }
        }

        // Relê a lista do jogo, mantendo o que já foi calculado de cada item.
        // Ao carregar um save os itens são recriados, e as entradas antigas saem aqui.
        private static void Refresh()
        {
            var items = ItemManager.GetAllItems();
            var current = new Dictionary<IntPtr, Entry>();
            var previous = new List<Entry>(All);
            All.Clear();
            for (int i = 0; i < (items?.Count ?? 0); i++)
            {
                InteractiveItem item = items[i];
                if (item == null || current.ContainsKey(item.Pointer))
                    continue;
                if (!_byPointer.TryGetValue(item.Pointer, out Entry entry))
                    entry = new Entry { Item = item };
                current[item.Pointer] = entry;
                All.Add(entry);
            }
            _byPointer.Clear();
            foreach (var pair in current)
                _byPointer[pair.Key] = pair.Value;

            // Item que saiu da lista do jogo não pode continuar brilhando.
            foreach (Entry entry in previous)
            {
                if (entry.Glowing && (entry.Item == null || !current.ContainsKey(entry.Item.Pointer)))
                    Glow.Restore(entry);
            }
        }

        // O jogo guarda o nome em inglês e o traduz para o idioma escolhido
        // (confirmado no jogo: "Flask" vira "Cantil"); sem tradução, o nome
        // volta igual. Vale para todos os idiomas do jogo.
        private static string Translate(string name)
        {
            try
            {
                string translated = TranslationManager.TranslateByText(name, Lang.Current);
                return string.IsNullOrWhiteSpace(translated) ? name : translated;
            }
            catch (Exception ex)
            {
                Mod.ReportError(nameof(Translate), ex);
                return name;
            }
        }

        private static void Read(Entry entry, StoryData story)
        {
            InteractiveItem item = entry.Item;
            if (item == null || !item.gameObject.activeInHierarchy)
                return;
            // Personagens (inclusive o jogador) também são InteractiveItem.
            if (item.GetComponentInParent<Character>() != null)
            {
                entry.Category = null;
                return;
            }

            // O jogo só preenche Interactions ao calcular as ações do item (no
            // hover do mouse); antes disso a lista vem vazia. Confirmado no jogo.
            if (story != null)
                item.HasInteractions(story);
            entry.Category = Classify(item.Interactions);
            if (entry.Category == null)
                return;

            string name = item.DisplayName;
            entry.Name = string.IsNullOrWhiteSpace(name) ? item.gameObject.name : Translate(name);
            // Portas e assentos não têm renderer próprio (IsRendererEnabled falso).
            if (entry.Renderer == null)
                entry.Renderer = item.GetComponentInChildren<Renderer>();
            if (entry.GlowRenderers == null)
            {
                entry.GlowRenderers = new List<Renderer>();
                // Partículas (fogo, fumaça) presas ao item não brilham; só o objeto.
                foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.TryCast<MeshRenderer>() != null || renderer.TryCast<SkinnedMeshRenderer>() != null)
                        entry.GlowRenderers.Add(renderer);
                }
            }
        }

        // Se o item deve ser considerado agora: categoria ligada, na cena e fora do inventário.
        public static bool Shown(Entry entry) =>
            entry.Category != null && Mod.Shows(entry.Category.Value) &&
            entry.Item != null && entry.Item.gameObject.activeInHierarchy && !entry.Item._inventoried;

        // Raio da câmera ao centro do item. Personagens no caminho (inclusive o
        // jogador, à frente da câmera) não bloqueiam: o raio continua depois deles.
        private static bool InSight(Entry entry, Camera camera, Vector3 eye)
        {
            Vector3 target = entry.Renderer != null && entry.Renderer.enabled
                ? entry.Renderer.bounds.center
                : entry.Item.transform.position;
            if (camera.WorldToScreenPoint(target).z <= 0f)
                return false;

            Transform itemTransform = entry.Item.transform;
            Vector3 from = eye;
            for (int i = 0; i < MaxPassThrough; i++)
            {
                if (!Physics.Linecast(from, target, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    return true;
                Transform blocker = hit.collider.transform;
                if (blocker.IsChildOf(itemTransform))
                    return true;
                if (blocker.GetComponentInParent<Character>() == null)
                    return false;
                from = hit.point + (target - from).normalized * 0.05f;
            }
            return false;
        }

        // A categoria do item é a de maior prioridade entre as suas ações.
        // Ações com parâmetro ("Remove;Top") contam pelo nome antes do ";".
        // "Use With..." (usar um item do inventário no objeto) é ignorada:
        // quase todo objeto fixo da casa a tem, e ela punha fogão, micro-ondas
        // e impressora em "uso". Sem ações, ou com Talk (personagem), o item
        // não é destacado.
        private static Category? Classify(Il2CppSystem.Collections.Generic.List<string> interactions)
        {
            Category? best = null;
            for (int i = 0; i < (interactions?.Count ?? 0); i++)
            {
                string action = interactions[i];
                if (string.IsNullOrWhiteSpace(action))
                    continue;
                int separator = action.IndexOf(';');
                if (separator >= 0)
                    action = action.Substring(0, separator);
                action = action.Trim();
                if (action.StartsWith("Use With", StringComparison.OrdinalIgnoreCase))
                    continue;

                Category category;
                switch (action)
                {
                    case "Talk": return null;
                    case "Take":
                    case "Take One": category = Category.Collectible; break;
                    case "Open":
                    case "Close": category = Category.OpenClose; break;
                    case "Sit":
                    case "Lay Down":
                    case "Climb": category = Category.Position; break;
                    case "Inspect": category = Category.Inspect; break;
                    default: category = Category.Use; break;
                }
                if (best == null || category < best.Value)
                    best = category;
            }
            return best;
        }
    }

    // Brilho rosa pulsante nos coletáveis à vista (e nos itens de uso, com
    // GlowUse), pela emissão do HDRP. Confirmado no protótipo (dev/Probe): a
    // emissão por MaterialPropertyBlock acende os materiais HDRP/Lit; 0,3 em
    // âmbar ficou discreto, e o usuário pediu o rosa, cor tema do jogo.
    internal static class Glow
    {
        private const float PulseSeconds = 2.5f;
        private static readonly Color Pink = new(1f, 0.24f, 0.6f);
        private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");
        private static readonly int EmissiveExposureWeight = Shader.PropertyToID("_EmissiveExposureWeight");
        private static MaterialPropertyBlock _block;

        public static void Tick(bool hidden)
        {
            bool active = Mod.GlowVisible && !hidden;
            // Sobe e desce suavemente, chegando a zero entre um pulso e outro.
            float pulse = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * 2f * Mathf.PI / PulseSeconds);
            Color emission = Pink * (Mod.GlowIntensity * pulse);

            foreach (Entry entry in Items.All)
            {
                try
                {
                    if (active && Glows(entry) && entry.InSight && Items.Shown(entry))
                        Apply(entry, emission);
                    else if (entry.Glowing)
                        Restore(entry);
                }
                catch (Exception ex) { Mod.ReportError(nameof(Glow), ex); }
            }
        }

        // Vaso, fogão e lixeira (uso) pulsando chamavam atenção demais no
        // jogo; por padrão só os coletáveis brilham.
        private static bool Glows(Entry entry) =>
            entry.Category == Category.Collectible || (entry.Category == Category.Use && Mod.GlowUse);

        private static void Apply(Entry entry, Color emission)
        {
            if (entry.GlowRenderers == null)
                return;
            _block ??= new MaterialPropertyBlock();
            if (!entry.Glowing)
            {
                entry.HadBlock = new List<bool>(entry.GlowRenderers.Count);
                foreach (Renderer renderer in entry.GlowRenderers)
                    entry.HadBlock.Add(renderer != null && HasBlock(renderer));
                entry.Glowing = true;
            }
            foreach (Renderer renderer in entry.GlowRenderers)
            {
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(_block);
                // Peso 0: a emissão não depende da exposição da câmera.
                _block.SetFloat(EmissiveExposureWeight, 0f);
                _block.SetColor(EmissiveColor, emission);
                renderer.SetPropertyBlock(_block);
            }
        }

        // Devolve o item ao normal: sem bloco, se não tinha um; senão, com a
        // emissão do material.
        public static void Restore(Entry entry)
        {
            entry.Glowing = false;
            if (entry.GlowRenderers == null)
                return;
            _block ??= new MaterialPropertyBlock();
            for (int i = 0; i < entry.GlowRenderers.Count; i++)
            {
                Renderer renderer = entry.GlowRenderers[i];
                try
                {
                    if (renderer == null)
                        continue;
                    if (entry.HadBlock == null || i >= entry.HadBlock.Count || !entry.HadBlock[i])
                    {
                        renderer.SetPropertyBlock(null);
                        continue;
                    }
                    Material material = renderer.sharedMaterial;
                    renderer.GetPropertyBlock(_block);
                    _block.SetFloat(EmissiveExposureWeight,
                        material != null && material.HasProperty(EmissiveExposureWeight) ? material.GetFloat(EmissiveExposureWeight) : 1f);
                    _block.SetColor(EmissiveColor,
                        material != null && material.HasProperty(EmissiveColor) ? material.GetColor(EmissiveColor) : Color.black);
                    renderer.SetPropertyBlock(_block);
                }
                catch (Exception ex) { Mod.ReportError(nameof(Restore), ex); }
            }
        }

        public static void RestoreAll()
        {
            foreach (Entry entry in Items.All)
            {
                if (entry.Glowing)
                    Restore(entry);
            }
        }

        // Se o método tiver sido removido do jogo, presume que havia bloco e
        // restaura pela emissão do material, que é o caminho conservador.
        private static bool HasBlock(Renderer renderer)
        {
            try { return renderer.HasPropertyBlock(); }
            catch (Exception ex)
            {
                Mod.ReportError(nameof(HasBlock), ex);
                return true;
            }
        }
    }

    // Desenha o nome dos itens à vista, logo acima de cada item na tela.
    internal static class Highlighter
    {
        // Folga entre o topo do item na tela e o nome. Um deslocamento no
        // mundo (15 cm) deixava o nome bem acima de itens vistos de perto.
        private const float GapPixels = 4f;
        private const float FallbackHeight = 0.3f;
        private const int FontSize = 15;
        private static GUIStyle[] _styles;
        private static GUIStyle _shadow;
        private static readonly Vector3[] _corners = new Vector3[8];

        // Escondido em pausa, menus e conversas, como as marcas do NewTalkMarker.
        public static bool Hidden() =>
            Time.timeScale == 0f || CanvasEvents.IsPauseWhenOpenActive() || CDialogue.CurrentlyOnDisplay != null;

        public static void Draw()
        {
            if (!Mod.LabelsVisible || Event.current.type != EventType.Repaint || Items.All.Count == 0 || Hidden())
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            EnsureStyles();
            foreach (Entry entry in Items.All)
            {
                try
                {
                    // O item pode ter sido pego, desligado ou destruído desde a última verificação.
                    if (!entry.InSight || !Items.Shown(entry))
                        continue;
                    if (!Anchor(entry, camera, out float x, out float top))
                        continue;

                    // GUI tem a origem no canto superior; a tela, no inferior.
                    float height = FontSize * 1.6f;
                    var rect = new Rect(x - 200f, Screen.height - top - GapPixels - height, 400f, height);
                    var offset = new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height);
                    GUI.Label(offset, entry.Name, _shadow);
                    GUI.Label(rect, entry.Name, _styles[(int)entry.Category.Value]);
                }
                catch (Exception ex) { Mod.ReportError(nameof(Highlighter), ex); }
            }
        }

        // Ponto da tela (coordenadas da tela, origem embaixo) onde o nome se
        // apoia: centro horizontal do item e o topo dos cantos do volume dele
        // projetados. Sem renderer, um ponto fixo acima da posição do item.
        private static bool Anchor(Entry entry, Camera camera, out float x, out float top)
        {
            x = top = 0f;
            if (entry.Renderer == null || !entry.Renderer.enabled)
            {
                Vector3 point = camera.WorldToScreenPoint(entry.Item.transform.position + Vector3.up * FallbackHeight);
                x = point.x;
                top = point.y;
                return point.z > 0f;
            }

            Bounds bounds = entry.Renderer.bounds;
            Vector3 min = bounds.min, max = bounds.max;
            _corners[0] = new Vector3(min.x, min.y, min.z);
            _corners[1] = new Vector3(max.x, min.y, min.z);
            _corners[2] = new Vector3(min.x, max.y, min.z);
            _corners[3] = new Vector3(max.x, max.y, min.z);
            _corners[4] = new Vector3(min.x, min.y, max.z);
            _corners[5] = new Vector3(max.x, min.y, max.z);
            _corners[6] = new Vector3(min.x, max.y, max.z);
            _corners[7] = new Vector3(max.x, max.y, max.z);

            Vector3 center = camera.WorldToScreenPoint(bounds.center);
            if (center.z <= 0f)
                return false;
            x = center.x;
            top = center.y;
            foreach (Vector3 corner in _corners)
            {
                Vector3 screen = camera.WorldToScreenPoint(corner);
                // Canto atrás da câmera (item muito perto) não serve de referência.
                if (screen.z > 0f)
                    top = Mathf.Max(top, screen.y);
            }
            return true;
        }

        private static void EnsureStyles()
        {
            if (_styles != null)
                return;
            // Na ordem de Category. O construtor de cópia do GUIStyle não existe no jogo (removido pelo IL2CPP).
            _styles = new[]
            {
                NewStyle(new Color(1f, 0.8f, 0.2f)),      // coletável: dourado
                NewStyle(new Color(0.3f, 0.9f, 1f)),      // uso: ciano
                NewStyle(new Color(0.55f, 0.95f, 0.55f)), // abrir/fechar: verde-claro
                NewStyle(new Color(0.75f, 0.75f, 0.75f)), // posição: cinza-claro
                NewStyle(Color.white),                    // examinar: branco
            };
            _shadow = NewStyle(new Color(0f, 0f, 0f, 0.85f));
        }

        private static GUIStyle NewStyle(Color color)
        {
            var style = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = FontSize };
            style.normal.textColor = color;
            return style;
        }
    }
}
