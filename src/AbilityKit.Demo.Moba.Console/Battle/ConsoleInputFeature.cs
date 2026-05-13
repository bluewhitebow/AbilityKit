using System;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Services;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// ????
    /// </summary>
    public readonly struct PlayerInputCommand
    {
        public int Frame { get; init; }
        public int PlayerId { get; init; }
        public int OpCode { get; init; }
        public byte[] Payload { get; init; }

        public PlayerInputCommand(int frame, int playerId, int opCode, byte[] payload)
        {
            Frame = frame;
            PlayerId = playerId;
            OpCode = opCode;
            Payload = payload;
        }

        public override string ToString()
        {
            return $"[Cmd Frame={Frame} Player={PlayerId} OpCode={OpCode}]";
        }
    }

    /// <summary>
    /// ????????
    /// </summary>
    public readonly struct LocalPlayerInputEvent
    {
        public int PlayerId { get; init; }
        public int OpCode { get; init; }
        public byte[] Payload { get; init; }

        public LocalPlayerInputEvent(int playerId, int opCode, byte[] payload)
        {
            PlayerId = playerId;
            OpCode = opCode;
            Payload = payload;
        }
    }

    /// <summary>
    /// ??????
    /// </summary>
    public sealed class BattleLocalInputQueue
    {
        private readonly System.Collections.Generic.Queue<LocalPlayerInputEvent> _queue = new();

        public void Enqueue(LocalPlayerInputEvent evt)
        {
            _queue.Enqueue(evt);
        }

        public void Flush()
        {
            _queue.Clear();
        }

        public int Count => _queue.Count;
    }

    /// <summary>
    /// ???? Codec
    /// </summary>
    public static class MobaMoveCodec
    {
        public static byte[] Serialize(float dx, float dz)
        {
            return System.Text.Encoding.UTF8.GetBytes($"{{\"dx\":{dx:F4},\"dz\":{dz:F4}}}");
        }

        public static (float dx, float dz) Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length < 4)
                return (0f, 0f);

            var json = System.Text.Encoding.UTF8.GetString(payload);
            float dx = 0f, dz = 0f;

            foreach (var pair in json.Trim('{', '}').Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length != 2) continue;

                var key = kv[0].Trim('"');
                var val = float.TryParse(kv[1].Trim(), out var f) ? f : 0f;

                if (key == "dx") dx = f;
                else if (key == "dz") dz = f;
            }

            return (dx, dz);
        }
    }

    /// <summary>
    /// ????
    /// </summary>
    public sealed class ConsoleInputFeature : IGameModule<ConsoleBattleContext>, IGameModuleTick<ConsoleBattleContext>
    {
        private ConsoleBattleContext _ctx;
        private BattleLocalInputQueue _inputQueue;
        private SkillExecutor _skillExecutor;
        private BattleServices _battleServices;
        private bool _initialized;

        private float _lastMoveDx;
        private float _lastMoveDz;
        private bool _wasMoving;

        public void SetServices(SkillExecutor skillExecutor, BattleServices battleServices)
        {
            _skillExecutor = skillExecutor;
            _battleServices = battleServices;
        }

        public void OnAttach(ConsoleBattleContext context)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
            _inputQueue = new BattleLocalInputQueue();
            _initialized = true;
            Log.Input($"[Input] Attached - PlayerId: {_ctx.LocalActorId}");
        }

        public void OnDetach(ConsoleBattleContext context)
        {
            _ctx = null;
            _inputQueue = null;
            _initialized = false;
            Log.Input($"[Input] Detached");
        }

        public void Tick(ConsoleBattleContext context, float deltaTime)
        {
            if (!_initialized || _ctx == null || _ctx.State != BattleState.InMatch) return;
            ProcessInput();
        }

        public void ProcessInput()
        {
            if (_ctx == null || _inputQueue == null) return;

            _ctx.LastFrame++;

            ProcessMoveInput();
            ProcessSkillInput();

            // ??????????
            _skillExecutor?.Step(_ctx.LocalActorId);

            _inputQueue.Flush();
        }

        private void ProcessMoveInput()
        {
            var dx = _ctx.HudMoveDx;
            var dz = _ctx.HudMoveDz;
            var isMoving = Math.Abs(dx) > 0.01f || Math.Abs(dz) > 0.01f;

            if (isMoving || _wasMoving)
            {
                var payload = MobaMoveCodec.Serialize(dx, dz);
                _inputQueue.Enqueue(new LocalPlayerInputEvent(_ctx.LocalActorId, (int)MobaOpCode.Move, payload));

                // ????????
                _battleServices?.OnMoveInput(_ctx.LocalActorId, dx, dz);

                Log.Input($"[Input] Move: dx={dx:F2}, dz={dz:F2}");
            }

            _wasMoving = isMoving;
            _lastMoveDx = dx;
            _lastMoveDz = dz;
        }

        private void ProcessSkillInput()
        {
            // ??????
            var slot = _ctx.HudSkillClickSlot;
            if (slot > 0)
            {
                var opCode = slot switch
                {
                    1 => (int)MobaOpCode.Skill1,
                    2 => (int)MobaOpCode.Skill2,
                    3 => (int)MobaOpCode.Skill3,
                    _ => (int)MobaOpCode.SkillInput
                };

                // ?????????
                if (_skillExecutor != null)
                {
                    var result = _skillExecutor.CastBySlot(_ctx.LocalActorId, slot);
                    if (!result.Success)
                    {
                        Log.Skill($"[Input] Skill{slot} failed: {result.FailReason}");
                    }
                }
                else
                {
                    // ?????????
                    var skillEvent = SkillInputEvent.CreatePress(slot);
                    var payload = SkillInputCodec.Serialize(in skillEvent);
                    _inputQueue.Enqueue(new LocalPlayerInputEvent(_ctx.LocalActorId, opCode, payload));
                }

                Log.Input($"[Input] Skill{slot} pressed");
                _ctx.HudSkillClickSlot = 0;
            }

            // ??????
            if (_ctx.HudSkillAimSubmit && _ctx.HudSkillAimSubmitSlot > 0)
            {
                var skillEvent = SkillInputEvent.CreateRelease(
                    _ctx.HudSkillAimSubmitSlot,
                    _ctx.HudSkillAimSubmitDx,
                    _ctx.HudSkillAimSubmitDz);

                // ?????????????
                if (_skillExecutor != null)
                {
                    var result = _skillExecutor.CastBySlot(
                        _ctx.LocalActorId,
                        _ctx.HudSkillAimSubmitSlot,
                        _ctx.HudSkillAimSubmitDx,
                        _ctx.HudSkillAimSubmitDz);

                    if (!result.Success)
                    {
                        Log.Skill($"[Input] Skill{_ctx.HudSkillAimSubmitSlot} aim release failed: {result.FailReason}");
                    }
                }
                else
                {
                    var payload = SkillInputCodec.Serialize(in skillEvent);
                    _inputQueue.Enqueue(new LocalPlayerInputEvent(_ctx.LocalActorId, (int)MobaOpCode.SkillInput, payload));
                }

                Log.Input($"[Input] Skill{_ctx.HudSkillAimSubmitSlot} aim released");
                _ctx.HudSkillAimSubmit = false;
            }
        }

        public void SetMoveInput(float dx, float dz)
        {
            if (_ctx == null) return;
            _ctx.HudMoveDx = dx;
            _ctx.HudMoveDz = dz;
            _ctx.HudHasMove = Math.Abs(dx) > 0.01f || Math.Abs(dz) > 0.01f;
        }

        public void ClickSkill(int slot)
        {
            if (_ctx == null) return;
            _ctx.HudSkillClickSlot = slot;
        }

        public void AimSkill(int slot, float dx, float dz)
        {
            if (_ctx == null) return;
            _ctx.HudSkillAiming = true;
            _ctx.HudSkillAimSlot = slot;
            _ctx.HudSkillAimDx = dx;
            _ctx.HudSkillAimDz = dz;
        }

        public void ReleaseSkillAim(int slot, float dx, float dz)
        {
            if (_ctx == null) return;
            _ctx.HudSkillAimSubmit = true;
            _ctx.HudSkillAimSubmitSlot = slot;
            _ctx.HudSkillAimSubmitDx = dx;
            _ctx.HudSkillAimSubmitDz = dz;
        }
    }
}
