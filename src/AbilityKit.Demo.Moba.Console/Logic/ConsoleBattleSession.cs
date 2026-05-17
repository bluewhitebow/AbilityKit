using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Core.Input;
using AbilityKit.Demo.Moba.Console.Events;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Logic
{
    /// <summary>
    /// Console 平台的战斗会话接口
    ///
    /// 连接表现层和逻辑层，表现层通过此接口提交输入，
    /// 逻辑层通过 IEventBus 发布事件，表现层订阅事件进行渲染
    /// </summary>
    public interface IConsoleBattleSession : IDisposable
    {
        /// <summary>
        /// 提交输入命令到逻辑层
        /// </summary>
        void SubmitInput(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);

        /// <summary>
        /// 获取本地玩家 ActorId
        /// </summary>
        int LocalActorId { get; }

        /// <summary>
        /// 帧推进
        /// </summary>
        void Step(float deltaTime);
    }

    /// <summary>
    /// Console 战斗会话实现
    ///
    /// 封装逻辑层运行时：
    /// - 输入处理（Move, SkillInput）
    /// - 技能执行
    /// - 事件发布
    ///
    /// 职责边界：
    /// - ✅ 处理输入命令
    /// - ✅ 执行技能逻辑
    /// - ✅ 发布 Damage/Heal/Buff 等事件
    /// - ❌ 不做渲染
    /// - ❌ 不持有 UI 引用
    /// </summary>
    public sealed class ConsoleBattleSession : IConsoleBattleSession
    {
        private readonly EC.IECWorld _world;
        private readonly int _localActorId;
        private readonly CooldownManager _cooldownManager;

        private bool _disposed;

        public EC.IECWorld World => _world;
        public int LocalActorId => _localActorId;

        public ConsoleBattleSession(EC.IECWorld world, int localActorId)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _localActorId = localActorId;
            _cooldownManager = new CooldownManager();
        }

        public void SubmitInput(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (inputs == null || inputs.Count == 0) return;

            for (int i = 0; i < inputs.Count; i++)
            {
                HandleCommand(inputs[i]);
            }
        }

        private void HandleCommand(PlayerInputCommand cmd)
        {
            switch (cmd.OpCode)
            {
                case ConsoleOpCode.Move:
                    HandleMove(cmd);
                    break;
                case ConsoleOpCode.SkillInput:
                    HandleSkillInput(cmd);
                    break;
            }
        }

        private void HandleMove(PlayerInputCommand cmd)
        {
            if (cmd.Payload == null || cmd.Payload.Length == 0) return;

            DeserializeMove(cmd.Payload, out var dx, out var dz);
            var actorId = ParsePlayerId(cmd.Player);

            // 发布移动事件（逻辑层处理后发布）
            var moveEvent = new MoveInputProcessedEvent
            {
                ActorId = actorId,
                Dx = dx,
                Dz = dz
            };
            BattleEventBus.Publish(in moveEvent);

            Platform.Log.Input($"[Logic] Move: Actor#{actorId} dx={dx:F2} dz={dz:F2}");
        }

        private void HandleSkillInput(PlayerInputCommand cmd)
        {
            if (cmd.Payload == null || cmd.Payload.Length == 0) return;

            var actorId = ParsePlayerId(cmd.Player);
            var (slot, phase, aimPos) = DeserializeSkillInput(cmd.Payload);

            // 检查冷却
            if (_cooldownManager.IsOnCooldown(slot))
            {
                Platform.Log.Skill($"[Logic] Skill{slot} on cooldown");
                var failEvent = new SkillExecutedEvent
                {
                    ActorId = actorId,
                    Slot = slot,
                    Success = false,
                    FailReason = "On cooldown"
                };
                BattleEventBus.Publish(in failEvent);
                return;
            }

            // 执行技能
            var skillId = GetSkillIdBySlot(slot);
            _cooldownManager.StartCooldown(slot, 30); // 1秒@30FPS

            Platform.Log.Skill($"[Logic] Skill: Actor#{actorId} Slot{slot} SkillId={skillId} Phase={phase}");

            // 发布技能执行事件
            var successEvent = new SkillExecutedEvent
            {
                ActorId = actorId,
                Slot = slot,
                Success = true,
                FailReason = null
            };
            BattleEventBus.Publish(in successEvent);

            // 模拟伤害（简化：只有 slot 1 和 2 有伤害）
            if (slot == 1 || slot == 2)
            {
                SimulateDamage(actorId, skillId, slot);
            }
        }

        private void SimulateDamage(int casterId, int skillId, int slot)
        {
            // 简化伤害计算
            var damage = slot == 1 ? 50f : 30f;
            var targetId = FindNearestEnemy(casterId);

            if (targetId > 0)
            {
                var damageEvent = new DamageEvent
                {
                    SourceId = casterId,
                    TargetId = targetId,
                    Damage = damage,
                    SkillId = skillId,
                    CurrentHp = 100f, // 简化
                    MaxHp = 100f,
                    IsDead = false
                };
                BattleEventBus.Publish(in damageEvent);
            }
        }

        private int FindNearestEnemy(int casterId)
        {
            // 简化：返回第一个非本地玩家的 actor
            return casterId == _localActorId ? 2 : _localActorId;
        }

        public void Step(float deltaTime)
        {
            _cooldownManager.Step();
        }

        private static int ParsePlayerId(PlayerId player)
        {
            if (int.TryParse(player.Value, out var id))
            {
                return id;
            }
            return 0;
        }

        private static void DeserializeMove(byte[] payload, out float dx, out float dz)
        {
            dx = 0f;
            dz = 0f;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(payload);
                ParseJsonPair(json, "dx", ref dx);
                ParseJsonPair(json, "dz", ref dz);
            }
            catch (Exception ex)
            {
                Platform.Log.Warn($"[Logic] Failed to deserialize move: {ex.Message}");
            }
        }

        private static (int slot, SkillInputPhase phase, Vec3 aimPos) DeserializeSkillInput(byte[] payload)
        {
            int slot = 0;
            var phase = SkillInputPhase.Press;
            var aimPos = Vec3.Zero;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(payload);

                ParseJsonPair(json, "slot", ref slot);
                ParseJsonPair(json, "phase", ref phase);

                float aimX = 0, aimZ = 0;
                ParseJsonPair(json, "aimX", ref aimX);
                ParseJsonPair(json, "aimZ", ref aimZ);
                aimPos = new Vec3(aimX, 0, aimZ);
            }
            catch (Exception ex)
            {
                Platform.Log.Warn($"[Logic] Failed to deserialize skill input: {ex.Message}");
            }

            return (slot, phase, aimPos);
        }

        private static void ParseJsonPair(string json, string key, ref int value)
        {
            var search = $"\"{key}\":";
            var idx = json.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0) return;

            var start = idx + search.Length;
            var end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;

            if (end > start && int.TryParse(json.Substring(start, end - start), out var result))
            {
                value = result;
            }
        }

        private static void ParseJsonPair(string json, string key, ref float value)
        {
            var search = $"\"{key}\":";
            var idx = json.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0) return;

            var start = idx + search.Length;
            var end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-')) end++;

            if (end > start && float.TryParse(json.Substring(start, end - start), out var result))
            {
                value = result;
            }
        }

        private static void ParseJsonPair(string json, string key, ref SkillInputPhase phase)
        {
            int value = 0;
            ParseJsonPair(json, key, ref value);
            phase = (SkillInputPhase)value;
        }

        private static int GetSkillIdBySlot(int slot)
        {
            return slot switch
            {
                1 => 101,
                2 => 102,
                3 => 103,
                _ => 100 + slot
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cooldownManager.Dispose();
        }

        /// <summary>
        /// 冷却管理器（逻辑层）
        /// </summary>
        private sealed class CooldownManager : IDisposable
        {
            private readonly int[] _cooldowns = new int[4];
            private bool _disposed;

            public bool IsOnCooldown(int slot)
            {
                return slot >= 1 && slot <= 3 && _cooldowns[slot] > 0;
            }

            public void StartCooldown(int slot, int frames)
            {
                if (slot >= 1 && slot <= 3)
                {
                    _cooldowns[slot] = frames;
                }
            }

            public void Step()
            {
                for (int i = 1; i <= 3; i++)
                {
                    if (_cooldowns[i] > 0)
                    {
                        _cooldowns[i]--;
                    }
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
            }
        }
    }
}
