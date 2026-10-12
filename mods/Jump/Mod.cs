using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using Il2CppEekCharacterEngine;
using Il2CppEekCharacterEngine.Canvas;
using Il2CppEekCharacterEngine.Components;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Jump.Mod), "Jump", "0.1.0", "gabriel")]
[assembly: MelonGame("Eek", "House Party")]

namespace Jump
{
    // Uma tecla faz o personagem do jogador pular. O jogo não tem pulo: no ar,
    // ele desce em velocidade constante e, no chão, prende o personagem ao
    // piso. Por isso o mod leva a altura do personagem a uma parábola logo
    // depois do movimento do próprio jogo, a cada quadro, e o jogo continua
    // cuidando do andar.
    public class Mod : MelonMod
    {
        private const float Gravity = 9.81f;
        // Andando, ~1,4 m/s; correndo, ~4 m/s. Correndo, a animação de pulo do
        // jogo dá um tranco na câmera e fica de fora.
        private const float RunSpeed = 2.5f;
        // Abaixo disto em relação à saída (pulo de um degrau), a queda volta a
        // ser do próprio jogo.
        private const float HandOffDrop = 0.5f;
        private const float MaxSeconds = 3f;

        internal static MelonLogger.Instance Log;
        internal static HotKey Key;
        private static MelonPreferences_Entry<float> _height;
        private static MelonPreferences_Entry<bool> _animation;

        private static bool _jumping;
        private static float _startTime;
        private static float _startY;
        private static float _launchSpeed;
        private static Vector3 _lastPosition;
        private static float _horizontalSpeed;

        private static readonly HashSet<string> _reportedErrors = new();

        // Registra cada erro uma vez por local, sem inundar o log a cada quadro.
        internal static void ReportError(string where, Exception ex)
        {
            if (_reportedErrors.Add($"{where}:{ex.GetType().Name}"))
                Log.Error($"Erro em {where} (o jogo segue sem pular): {ex}");
        }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            MelonPreferences_Category category = MelonPreferences.CreateCategory("Jump");
            var key = Lang.Entry(category.CreateEntry("Key", "Space"),
                "Tecla do pulo", "Tecla que faz o personagem pular (nome do KeyCode do Unity, por exemplo Space, V, G).",
                "Jump key", "Key that makes the character jump (Unity KeyCode name, for example Space, V, G).");
            _height = Lang.Entry(category.CreateEntry("Height", 0.8f),
                "Altura do pulo", "Altura do pulo em metros (de 0,1 a 3).",
                "Jump height", "Jump height in meters (from 0.1 to 3).");
            _animation = Lang.Entry(category.CreateEntry("Animation", true),
                "Animação de pulo", "Toca a animação de pulo do jogo ao pular parado ou andando (correndo, ela fica de fora).",
                "Jump animation", "Plays the game's jump animation when jumping while standing or walking (not while running).");
            Combat.Init(category);

            Key = new HotKey("Key", key.Value, KeyCode.Space);
            // A tecla nova vale na hora quando a opção muda com o jogo aberto.
            key.OnEntryValueChanged.Subscribe((_, value) => Key = new HotKey("Key", value, KeyCode.Space));
            Log.Msg($"Carregado. {Key.Name} pula.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // O personagem é recriado a cada carregamento.
            _jumping = false;
        }

        public override void OnUpdate()
        {
            try { Lang.Tick(); }
            catch (Exception ex) { ReportError(nameof(Lang), ex); }
            try { Combat.Tick(); }
            catch (Exception ex) { ReportError(nameof(Combat), ex); }

            try
            {
                var player = PlayerCharacter.Player;
                if (player == null)
                {
                    _jumping = false;
                    return;
                }
                MeasureSpeed(player);
                if (Key.Pressed() && CanJump(player))
                    Start(player);
            }
            catch (Exception ex)
            {
                _jumping = false;
                ReportError(nameof(OnUpdate), ex);
            }
        }

        // Só do chão, fora de outro pulo e com o controle livre. Em combate,
        // pula também (decisão do usuário).
        private static bool CanJump(PlayerCharacter player)
        {
            var controller = player.Controller;
            if (_jumping || controller == null || !controller.isGrounded)
                return false;
            if (Time.timeScale == 0f || CanvasEvents.IsPauseWhenOpenActive() || CDialogue.CurrentlyOnDisplay != null)
                return false;
            return !CutSceneManager.IsAnyCutScenePlaying && !player.IgnoringMovementInput;
        }

        private static void Start(PlayerCharacter player)
        {
            float height = Mathf.Clamp(_height.Value, 0.1f, 3f);
            _launchSpeed = Mathf.Sqrt(2f * Gravity * height);
            _startTime = Time.time;
            _startY = player.transform.position.y;
            _jumping = true;
            if (_animation.Value && _horizontalSpeed <= RunSpeed)
                player.SendEvent((int)CEvents.JumpUp, ComponentTypes.All, false, (Il2CppReferenceArray<Il2CppSystem.Object>)null);
        }

        // Velocidade horizontal do último quadro, para saber se está correndo.
        // As velocidades guardadas pelo jogo não servem: correndo,
        // CurrentMovementSpeed fica em 0.
        private static void MeasureSpeed(PlayerCharacter player)
        {
            Vector3 position = player.transform.position;
            Vector3 step = position - _lastPosition;
            step.y = 0;
            _lastPosition = position;
            _horizontalSpeed = Time.deltaTime > 0 ? step.magnitude / Time.deltaTime : 0;
        }

        // Depois do movimento do jogo, leva o personagem à altura da parábola.
        // As colisões são do CharacterController: bate no teto e pousa em móveis.
        private static void FollowArc(PlayerCharacter player)
        {
            var controller = player.Controller;
            if (controller == null)
            {
                _jumping = false;
                return;
            }
            float t = Time.time - _startTime;
            float target = _startY + _launchSpeed * t - 0.5f * Gravity * t * t;
            controller.Move(Vector3.up * (target - player.transform.position.y));
            float y = player.transform.position.y;
            bool descending = t > _launchSpeed / Gravity;
            // Pousou, no chão ou em algo mais alto que a parábola.
            if (descending && (controller.isGrounded || y > target + 0.01f))
                _jumping = false;
            else if (target < _startY - HandOffDrop || t > MaxSeconds)
                _jumping = false;
        }

        // PlayerCharacter.TryMoveController move o personagem a cada quadro,
        // fora do FixedUpdate. Métodos nativos podem ser compartilhados entre
        // classes; confere a classe real.
        [HarmonyPatch(typeof(PlayerCharacter), nameof(PlayerCharacter.TryMoveController))]
        private static class MovePatch
        {
            private static void Postfix(PlayerCharacter __instance)
            {
                if (!_jumping)
                    return;
                try
                {
                    if (Guard.Is<PlayerCharacter>(__instance) && __instance.IsPlayer)
                        FollowArc(__instance);
                }
                catch (Exception ex)
                {
                    _jumping = false;
                    ReportError(nameof(FollowArc), ex);
                }
            }
        }
    }

    internal sealed class HotKey
    {
        private static bool _legacyInputBroken;
        private readonly KeyCode _key;
        private readonly UnityEngine.InputSystem.Key _newKey;

        public string Name => _key.ToString();

        // Caminho da tecla no Input System (por exemplo "<Keyboard>/space"), ou
        // null. Calculado na hora: o teclado pode não existir ao carregar o mod.
        public string BindingPath
        {
            get
            {
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard == null || _newKey == UnityEngine.InputSystem.Key.None)
                    return null;
                return "<Keyboard>/" + keyboard[_newKey].name;
            }
        }

        public HotKey(string option, string name, KeyCode fallback)
        {
            if (!Enum.TryParse(name?.Trim(), true, out _key))
            {
                Mod.Log.Warning($"{option} \"{name}\" não é um KeyCode válido; usando {fallback}.");
                _key = fallback;
            }
            // Nomes iguais nos dois sistemas para letras, F1–F12 e Space; os demais ficam sem reserva.
            if (!Enum.TryParse(_key.ToString(), true, out _newKey))
                _newKey = UnityEngine.InputSystem.Key.None;
        }

        // O jogo pode ter só o Input System novo ativo; nesse caso o Input
        // antigo lança exceção e passamos a usar o novo. Vale com modificadores:
        // correr usa Shift.
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
}
