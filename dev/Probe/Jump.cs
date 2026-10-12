using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppEekCharacterEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Probe
{
    // Fase 1 do p60: testa como fazer o jogador pular.
    // - V: a altura segue uma parábola (velocidade inicial e gravidade). Logo
    //   depois do movimento do jogo (TryMoveController), o CharacterController
    //   leva o personagem à altura da parábola; o jogo continua cuidando do
    //   andar. Termina ao tocar o chão ou, se cair abaixo da altura de saída,
    //   devolve a queda ao jogo. Com Probe.JumpAnimation, envia também JumpUp.
    //   Só pula do chão e fora de outro pulo. Com Probe.JumpSkipFalling, o
    //   jogo não roda a própria queda (ProcessFalling) durante o pulo.
    // - V ou Espaço, com qualquer modificador: correr usa Shift.
    // - Com Probe.JumpMoveCombat, o modo de combate (Player/CombatMode) sai do
    //   Espaço e vai para o Alt, por override, em todos os PlayerInputMap
    //   (inclusive o original, que o F8 mostra), sempre que a tecla efetiva
    //   for o Espaço. O F8 recusa o Espaço para qualquer ação (IsDuplicate).
    // AddForce (descartado) e o JumpUp sozinho foram testados antes; ver o p60.
    // Cada teste registra por ~3 s a altura e o estado de queda, só quando
    // algo muda.
    internal static class Jump
    {
        private const float WatchSeconds = 3f;
        private const float Gravity = 9.81f;
        // Abaixo disto em relação à saída, o pulo vira queda do próprio jogo.
        private const float HandOffDrop = 0.5f;
        // Andando, ~1,4 m/s; correndo, ~4 m/s. Correndo, a animação dá um
        // tranco na câmera e fica de fora.
        private const float RunSpeed = 2.5f;

        private static MelonPreferences_Entry<float> _speed;
        private static MelonPreferences_Entry<bool> _animation;
        private static MelonPreferences_Entry<bool> _skipFalling;
        private static MelonPreferences_Entry<bool> _moveCombat;
        private static float _nextCombatCheck;

        private static float _watchUntil;
        private static float _startTime;
        private static float _startY;
        private static string _lastState;
        private static bool _jumping;
        private static Vector3 _lastPosition;
        private static float _horizontalSpeed;

        public static void Init()
        {
            var category = MelonPreferences.CreateCategory("Probe");
            _speed = category.CreateEntry("JumpSpeed", 4f, display_name: "Velocidade do pulo (teste, V)",
                description: "Velocidade inicial para cima, em m/s. A altura é v²/2g: 4 m/s dá ~0,8 m.");
            _animation = category.CreateEntry("JumpAnimation", true, display_name: "Animação no pulo (teste, V)",
                description: "Envia o evento JumpUp ao pular com V.");
            _skipFalling = category.CreateEntry("JumpSkipFalling", false, display_name: "Sem queda do jogo no pulo (teste, V)",
                description: "Não deixa o jogo rodar a própria queda (ProcessFalling) enquanto o pulo com V está no ar.");
            _moveCombat = category.CreateEntry("JumpMoveCombat", true, display_name: "Combate no Alt (teste)",
                description: "Move o modo de combate do Espaço para o Alt, em memória, para o Espaço pular.");
        }

        public static void Tick()
        {
            if (Time.unscaledTime >= _nextCombatCheck)
            {
                _nextCombatCheck = Time.unscaledTime + 1f;
                MoveCombat();
            }

            var player = PlayerCharacter.Player;
            if (player == null)
            {
                _jumping = false;
                return;
            }

            MeasureSpeed(player);
            if (VPressed())
                Start(player);

            if (Time.time <= _watchUntil)
                Watch(player);
        }

        private static void Start(PlayerCharacter player)
        {
            var controller = player.Controller;
            if (_jumping || controller == null || !controller.isGrounded)
            {
                Mod.Log.Msg($"[pulo] V ignorado: pulando={_jumping} chão={(controller != null && controller.isGrounded)} caindo={player.IsFalling}");
                return;
            }
            _startTime = Time.time;
            _startY = player.transform.position.y;
            _watchUntil = Time.time + WatchSeconds;
            _lastState = null;
            bool running = _horizontalSpeed > RunSpeed;
            Mod.Log.Msg($"[pulo] V: parábola {_speed.Value} m/s (altura {_speed.Value * _speed.Value / (2 * Gravity):F2} m), " +
                $"animação={_animation.Value} semQueda={_skipFalling.Value} horizontal={_horizontalSpeed:F1}m/s correndo={running} " +
                $"jogo: CurrentMovementSpeed={player.CurrentMovementSpeed:F2} GetSpeed={player.GetSpeed:F2} _moveSpeed={player._moveSpeed:F2} _runSpeed={player._runSpeed:F2}");
            _jumping = true;
            if (_animation.Value && !running)
                SendJumpUp(player);
        }

        private static void SendJumpUp(PlayerCharacter player)
        {
            player.SendEvent((int)CEvents.JumpUp, ComponentTypes.All, false, (Il2CppReferenceArray<Il2CppSystem.Object>)null);
        }

        // Depois do movimento do jogo, leva o personagem à altura da parábola.
        private static void FollowArc(PlayerCharacter player)
        {
            var controller = player.Controller;
            if (controller == null)
            {
                _jumping = false;
                return;
            }
            float t = Time.time - _startTime;
            float target = _startY + _speed.Value * t - 0.5f * Gravity * t * t;
            float before = player.transform.position.y;
            controller.Move(Vector3.up * (target - before));
            float after = player.transform.position.y;
            bool descending = t > _speed.Value / Gravity;

            if (descending && (controller.isGrounded || after > target + 0.01f))
            {
                _jumping = false;
                Mod.Log.Msg($"[pulo] pousou em {t:F2}s, altura final {after - _startY:F2} (chão={controller.isGrounded})");
            }
            else if (target < _startY - HandOffDrop || t > WatchSeconds)
            {
                _jumping = false;
                Mod.Log.Msg($"[pulo] entregou a queda ao jogo em {t:F2}s, altura {after - _startY:F2}");
            }
        }

        // Passa o combate do Espaço para o Alt em todos os conjuntos de ações,
        // sempre que a tecla efetiva voltar a ser o Espaço ("restaurar padrão"
        // no F8 remove o override; também grava um override para o próprio Espaço).
        private static void MoveCombat()
        {
            if (!_moveCombat.Value)
            {
                LogCombat();
                return;
            }
            var assets = Resources.FindObjectsOfTypeAll(Il2CppType.Of<InputActionAsset>());
            for (int i = 0; i < (assets?.Length ?? 0); i++)
            {
                var asset = assets[i]?.TryCast<InputActionAsset>();
                var action = asset?.FindAction("Player/CombatMode", false);
                if (action == null)
                    continue;
                var bindings = action.bindings;
                for (int b = 0; b < bindings.Count; b++)
                {
                    if (bindings[b].effectivePath != "<Keyboard>/space")
                        continue;
                    InputActionRebindingExtensions.ApplyBindingOverride(action, b, "<Keyboard>/alt");
                    Mod.Log.Msg($"[pulo] {asset.name} ({asset.Pointer:X}): combate Espaço -> [{InputActionRebindingExtensions.GetBindingDisplayString(action, default, null)}] " +
                        OwnJson(action));
                }
            }
        }

        // Com a troca desligada, registra uma vez a tecla do combate de cada
        // conjunto, para ver se a troca ficou gravada pelo jogo.
        private static readonly HashSet<IntPtr> _logged = new();

        private static void LogCombat()
        {
            var assets = Resources.FindObjectsOfTypeAll(Il2CppType.Of<InputActionAsset>());
            for (int i = 0; i < (assets?.Length ?? 0); i++)
            {
                var asset = assets[i]?.TryCast<InputActionAsset>();
                var action = asset?.FindAction("Player/CombatMode", false);
                if (action == null || !_logged.Add(asset.Pointer))
                    continue;
                Mod.Log.Msg($"[pulo] troca desligada; {asset.name}: combate [{InputActionRebindingExtensions.GetBindingDisplayString(action, default, null)}] " +
                    OwnJson(action));
            }
        }

        // O jogo grava os remapeamentos com SaveBindingOverridesAsJson. A troca
        // do mod (<Keyboard>/alt no combate) sai do texto gravado, para o combate
        // voltar ao Espaço sem o mod. Um Alt escolhido no F8 é <Keyboard>/leftAlt.
        private static readonly System.Text.RegularExpressions.Regex OwnOverride = new(
            @"\{[^{}]*""action"":""Player/CombatMode""[^{}]*""path"":""<Keyboard>/alt""[^{}]*\},?");

        // Leituras da própria sonda não passam pelo filtro.
        private static bool _self;

        private static string OwnJson(InputAction action)
        {
            _self = true;
            try { return InputActionRebindingExtensions.SaveBindingOverridesAsJson(action); }
            finally { _self = false; }
        }

        private static string StripOwnOverride(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;
            string stripped = OwnOverride.Replace(json, "").Replace(",]", "]");
            return stripped;
        }

        [HarmonyPatch(typeof(InputActionRebindingExtensions), nameof(InputActionRebindingExtensions.SaveBindingOverridesAsJson), typeof(IInputActionCollection2))]
        private static class SaveCollectionPatch
        {
            private static void Postfix(ref string __result)
            {
                try
                {
                    string stripped = StripOwnOverride(__result);
                    Mod.Log.Msg($"[pulo] SaveBindingOverridesAsJson(conjunto): {(stripped == __result ? "sem troca do mod" : "troca do mod retirada")} -> {stripped}");
                    __result = stripped;
                }
                catch (Exception ex) { Mod.Log.Error($"[pulo] {ex}"); }
            }
        }

        [HarmonyPatch(typeof(InputActionRebindingExtensions), nameof(InputActionRebindingExtensions.SaveBindingOverridesAsJson), typeof(InputAction))]
        private static class SaveActionPatch
        {
            private static void Postfix(InputAction action, ref string __result)
            {
                try
                {
                    if (_self)
                        return;
                    string stripped = StripOwnOverride(__result);
                    if (stripped != __result)
                        Mod.Log.Msg($"[pulo] SaveBindingOverridesAsJson({action?.name}): troca do mod retirada -> {stripped}");
                    __result = stripped;
                }
                catch (Exception ex) { Mod.Log.Error($"[pulo] {ex}"); }
            }
        }

        // No F8, o Espaço conta como tecla já usada: o pulo é nele.
        [HarmonyPatch(typeof(Il2Cpp.RebindActionUI), nameof(Il2Cpp.RebindActionUI.IsDuplicate))]
        private static class DuplicatePatch
        {
            private static void Postfix(InputAction candidateAction, InputControl rebindControl, ref bool __result)
            {
                try
                {
                    string path = rebindControl?.path;
                    Mod.Log.Msg($"[pulo] F8: {candidateAction?.name} -> {path} (duplicada pelo jogo={__result})");
                    if (_moveCombat.Value && path == "/Keyboard/space")
                    {
                        __result = true;
                        Mod.Log.Msg("[pulo] F8: Espaço recusado");
                    }
                }
                catch (Exception ex) { Mod.Log.Error($"[pulo] {ex}"); }
            }
        }

        // Velocidade horizontal do último quadro.
        private static void MeasureSpeed(PlayerCharacter player)
        {
            Vector3 position = player.transform.position;
            Vector3 step = position - _lastPosition;
            step.y = 0;
            _lastPosition = position;
            _horizontalSpeed = Time.deltaTime > 0 ? step.magnitude / Time.deltaTime : 0;
        }

        private static void Watch(PlayerCharacter player)
        {
            var controller = player.Controller;
            float height = player.transform.position.y - _startY;
            float horizontal = _horizontalSpeed;
            string state = $"altura={height:F2} horizontal={horizontal:F1}m/s chão={(controller != null && controller.isGrounded)} caindo={player.IsFalling} " +
                $"força={player.addForceVelocity} warp={player.IsWarping} pulando={_jumping} " +
                $"movimentoBloqueado={player.IgnoringMovementInput}";
            if (state == _lastState)
                return;
            _lastState = state;
            Mod.Log.Msg($"[pulo] {Time.time - _startTime:F2}s {state}");
        }

        // Durante o pulo com JumpSkipFalling, o jogo não roda a própria queda.
        [HarmonyPatch(typeof(Character), nameof(Character.ProcessFalling))]
        private static class FallingPatch
        {
            private static bool Prefix(Character __instance)
            {
                try { return !(_jumping && _skipFalling.Value && __instance != null && __instance.IsPlayer); }
                catch (Exception ex)
                {
                    Mod.Log.Error($"[pulo] {ex}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerCharacter), nameof(PlayerCharacter.TryMoveController))]
        private static class MovePatch
        {
            private static void Postfix(PlayerCharacter __instance)
            {
                try
                {
                    if (_jumping && __instance != null && __instance.IsPlayer)
                        FollowArc(__instance);
                }
                catch (Exception ex)
                {
                    _jumping = false;
                    Mod.Log.Error($"[pulo] {ex}");
                }
            }
        }

        private static bool VPressed()
        {
            Keys.Self = true;
            try
            {
                var keyboard = Keyboard.current;
                return keyboard != null && (keyboard.vKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            }
            finally { Keys.Self = false; }
        }
    }
}
