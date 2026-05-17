using System;
using System.Collections.Generic;
using System.Text;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Console.Core.Input;
using AbilityKit.Demo.Moba.Console.Events;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 平台的输入适配器
    ///
    /// 实现 IConsoleInputSink 接口，将表现层输入转发到 BattleEventBus
    ///
    /// 架构说明：
    /// - ConsoleInputFeature 调用 Submit() 将输入命令发送到这里
    /// - 这里解析命令并发布 BattleEventBus 事件
    /// - 不执行任何逻辑，只做输入转发
    ///
    /// 职责边界：
    /// - ✅ 解析 PlayerInputCommand
    /// - ✅ 发布 MoveInputProcessedEvent
    /// - ✅ 发布 SkillExecutedEvent
    /// - ❌ 不做伤害计算（逻辑层职责）
    /// - ❌ 不做冷却管理（逻辑层职责）
    /// </summary>
    public sealed class ConsoleInputSink : IConsoleInputSink
    {
        public void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (inputs == null || inputs.Count == 0) return;

            for (int i = 0; i < inputs.Count; i++)
            {
                var cmd = inputs[i];
                HandleCommand(cmd);
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

            Platform.Log.Input($"[ConsoleInputSink] Move: Actor#{actorId} dx={dx:F2} dz={dz:F2}");

            // 发布移动事件
            var moveEvent = new MoveInputProcessedEvent
            {
                ActorId = actorId,
                Dx = dx,
                Dz = dz
            };
            BattleEventBus.Publish(in moveEvent);
        }

        private void HandleSkillInput(PlayerInputCommand cmd)
        {
            if (cmd.Payload == null || cmd.Payload.Length == 0) return;

            var actorId = ParsePlayerId(cmd.Player);
            var (slot, phase, aimPos) = DeserializeSkillInput(cmd.Payload);

            Platform.Log.Skill($"[ConsoleInputSink] Skill: Actor#{actorId} Slot{slot} Phase={phase}");

            // 发布技能执行事件（冷却和伤害由逻辑层处理）
            var skillEvent = new SkillExecutedEvent
            {
                ActorId = actorId,
                Slot = slot,
                Success = true,
                FailReason = null
            };
            BattleEventBus.Publish(in skillEvent);
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
                var json = Encoding.UTF8.GetString(payload);
                ParseJsonPair(json, "dx", ref dx);
                ParseJsonPair(json, "dz", ref dz);
            }
            catch (Exception ex)
            {
                Platform.Log.Warn($"[ConsoleInputSink] Failed to deserialize move: {ex.Message}");
            }
        }

        private static (int slot, SkillInputPhase phase, Vec3 aimPos) DeserializeSkillInput(byte[] payload)
        {
            int slot = 0;
            var phase = SkillInputPhase.Press;
            var aimPos = Vec3.Zero;

            try
            {
                var json = Encoding.UTF8.GetString(payload);

                ParseJsonPair(json, "slot", ref slot);
                ParseJsonPair(json, "phase", ref phase);

                float aimX = 0, aimZ = 0;
                ParseJsonPair(json, "aimX", ref aimX);
                ParseJsonPair(json, "aimZ", ref aimZ);
                aimPos = new Vec3(aimX, 0, aimZ);
            }
            catch (Exception ex)
            {
                Platform.Log.Warn($"[ConsoleInputSink] Failed to deserialize skill input: {ex.Message}");
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
    }
}
