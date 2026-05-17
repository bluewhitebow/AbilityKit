using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Logic;
using PlayerId = AbilityKit.Ability.Host.PlayerId;

namespace AbilityKit.Demo.Moba.Console.Core.Input
{
    /// <summary>
    /// 输入特征模块（表现层）
    ///
    /// 职责边界：
    /// - ✅ 采集玩家输入
    /// - ✅ 调用 IConsoleBattleSession.SubmitInput() 转发输入
    /// - ❌ 不执行技能逻辑
    /// - ❌ 不计算伤害
    /// - ❌ 不直接发布事件
    ///
    /// 架构说明：
    /// - 表现层持有 IConsoleBattleSession
    /// - 通过 SubmitInput() 将输入转发到逻辑层
    /// - 逻辑层处理后发布事件，表现层通过 ConsoleViewEventSink 订阅
    /// </summary>
    public sealed class ConsoleInputFeature : IInputFeature, IGameModule<ConsoleBattleContext>, IGameModuleTick<ConsoleBattleContext>
    {
        private ConsoleBattleContext _ctx;
        private IConsoleBattleSession _session;
        private BattleLocalInputQueue _inputQueue;
        private bool _initialized;

        private float _lastMoveDx;
        private float _lastMoveDz;
        private bool _wasMoving;

        public int LocalActorId => _ctx?.LocalActorId ?? 0;

        /// <summary>
        /// 设置战斗会话（逻辑层）
        /// </summary>
        public void SetSession(IConsoleBattleSession session)
        {
            _session = session;
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
            if (!_initialized || _ctx == null || _ctx.State != BattleState.InMatch)
            {
                return;
            }
            ProcessInput();
        }

        public void ProcessInput()
        {
            if (_ctx == null || _inputQueue == null)
            {
                return;
            }

            _ctx.LastFrame++;

            ProcessMoveInput();
            ProcessSkillInput();

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
                var cmd = new PlayerInputCommand(
                    new FrameIndex(_ctx.LastFrame),
                    new PlayerId(_ctx.LocalActorId.ToString()),
                    ConsoleOpCode.Move,
                    payload);

                // 通过 Session 提交到逻辑层
                _session?.SubmitInput(new FrameIndex(_ctx.LastFrame), new[] { cmd });

                // 记录本地输入
                _inputQueue.Enqueue(new LocalPlayerInputEvent(_ctx.LocalActorId, ConsoleOpCode.Move, payload));

                Log.Input($"[Input] Move: dx={dx:F2}, dz={dz:F2}");
            }

            _wasMoving = isMoving;
            _lastMoveDx = dx;
            _lastMoveDz = dz;
        }

        private void ProcessSkillInput()
        {
            // 处理技能点击
            var slot = _ctx.HudSkillClickSlot;
            if (slot > 0)
            {
                var payload = ConsoleSkillInputCodec.Serialize(slot, SkillInputPhase.Press);
                var cmd = new PlayerInputCommand(
                    new FrameIndex(_ctx.LastFrame),
                    new PlayerId(_ctx.LocalActorId.ToString()),
                    ConsoleOpCode.SkillInput,
                    payload);

                // 通过 Session 提交到逻辑层
                _session?.SubmitInput(new FrameIndex(_ctx.LastFrame), new[] { cmd });

                Log.Input($"[Input] Skill{slot} pressed");
                _ctx.HudSkillClickSlot = 0;
            }

            // 处理技能瞄准释放
            if (_ctx.HudSkillAimSubmit && _ctx.HudSkillAimSubmitSlot > 0)
            {
                var aimX = _ctx.HudSkillAimSubmitDx;
                var aimZ = _ctx.HudSkillAimSubmitDz;

                var aimPos = new Vec3(aimX, 0, aimZ);
                var payload = ConsoleSkillInputCodec.Serialize(_ctx.HudSkillAimSubmitSlot, SkillInputPhase.Release, aimPos);

                var cmd = new PlayerInputCommand(
                    new FrameIndex(_ctx.LastFrame),
                    new PlayerId(_ctx.LocalActorId.ToString()),
                    ConsoleOpCode.SkillInput,
                    payload);

                // 通过 Session 提交到逻辑层
                _session?.SubmitInput(new FrameIndex(_ctx.LastFrame), new[] { cmd });

                Log.Input($"[Input] Skill{_ctx.HudSkillAimSubmitSlot} aim released at ({aimX:F1}, {aimZ:F1})");
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

    /// <summary>
    /// 本地玩家输入事件
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
    /// 本地输入队列
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
    /// 移动编码器
    /// </summary>
    public static class MobaMoveCodec
    {
        public static byte[] Serialize(float dx, float dz)
        {
            return System.Text.Encoding.UTF8.GetBytes($"{{\"dx\":{dx:F4},\"dz\":{dz:F4}}}");
        }
    }

    /// <summary>
    /// 技能输入编码器（Console 版本）
    /// </summary>
    public static class ConsoleSkillInputCodec
    {
        public static byte[] Serialize(int slot, SkillInputPhase phase, Vec3 aimPos = default)
        {
            var json = $"{{\"slot\":{slot},\"phase\":{(int)phase},\"aimX\":{aimPos.X:F2},\"aimZ\":{aimPos.Z:F2}}}";
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}
