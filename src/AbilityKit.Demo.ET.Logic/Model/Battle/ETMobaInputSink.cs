using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Protocol.Moba.StateSync;
using MobaOpCode = AbilityKit.Demo.Moba.Services.MobaOpCode;
using MobaEntityManager = AbilityKit.Demo.Moba.Services.EntityManager.MobaEntityManager;

namespace ET.Logic
{
    /// <summary>
    /// ET Demo 专用的输入处理器
    ///
    /// 设计说明：
    /// - 实现 IWorldInputSink 接口
    /// - 接收 PlayerInputCommand 并转换为 moba.core 的 MoveInputComponent
    /// - 通过 IWorldResolver 获取 MobaEntityManager 服务
    /// - 同时保持 OnMoveCommand 回调用于 ET.View 渲染
    /// </summary>
    public sealed class ETMobaInputSink : IWorldInputSink, IWorldInitializable
    {
        /// <summary>
        /// 输入命令回调（用于 ET.View 渲染）
        /// </summary>
        public Action<int, int, float, float>? OnMoveCommand;

        /// <summary>
        /// 技能命令回调
        /// </summary>
        public Action<int, int, int, float, float>? OnSkillCommand;

        private bool _disposed;
        private IWorldResolver? _services;
        private MobaEntityManager? _entityManager;

        public ETMobaInputSink()
        {
        }

        /// <summary>
        /// 实现 IWorldInitializable - 从服务容器获取 MobaEntityManager
        /// </summary>
        public void OnInit(IWorldResolver services)
        {
            _services = services;
            if (services != null)
            {
                services.TryResolve(out _entityManager);
            }
        }

        /// <summary>
        /// 处理输入提交
        /// </summary>
        public void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (_disposed || inputs == null || inputs.Count == 0)
            {
                return;
            }

            for (int i = 0; i < inputs.Count; i++)
            {
                var cmd = inputs[i];
                var actorId = ParseActorId(cmd.Player);
                var opCode = cmd.OpCode;

                // 根据 OpCode 分发到不同处理
                switch (opCode)
                {
                    case (int)MobaOpCode.Move:
                        HandleMove(actorId, cmd);
                        break;

                    case (int)MobaOpCode.SkillInput:
                        HandleSkillInput(actorId, cmd);
                        break;

                    default:
                        Log.Debug($"[ETMobaInputSink] Unknown OpCode: {opCode}");
                        break;
                }
            }
        }

        /// <summary>
        /// 处理移动命令
        /// 1. 通过 moba.core MobaEntityManager 设置 MoveInputComponent
        /// 2. 触发 OnMoveCommand 回调用于 ET.View 渲染
        /// </summary>
        private void HandleMove(int actorId, PlayerInputCommand cmd)
        {
            if (actorId <= 0)
            {
                Log.Debug($"[ETMobaInputSink] HandleMove: invalid actorId={actorId}");
                return;
            }

            // 解析移动坐标
            if (cmd.Payload == null || cmd.Payload.Length == 0)
            {
                Log.Debug($"[ETMobaInputSink] HandleMove: empty payload");
                return;
            }

            MobaMoveCodec.Deserialize(cmd.Payload, out var x, out var z);

            // 1. 通过 MobaEntityManager 设置 moba.core 实体的 MoveInput 组件
            ApplyMoveInputToMobaEntity(actorId, x, z);

            // 2. 触发回调用于 ET.View 渲染
            OnMoveCommand?.Invoke(actorId, actorId, x, z);

            Log.Info($"[ETMobaInputSink] HandleMove: ActorId={actorId}, Target=({x:F2}, {z:F2})");
        }

        /// <summary>
        /// 通过 MobaEntityManager 设置 moba.core 实体的 MoveInput 组件
        /// </summary>
        private void ApplyMoveInputToMobaEntity(int actorId, float x, float z)
        {
            if (_entityManager == null)
            {
                Log.Warning($"[ETMobaInputSink] ApplyMoveInput: MobaEntityManager is null, skipping moba.core integration");
                return;
            }

            try
            {
                // 计算移动方向向量 (target - current)
                float dx = x;
                float dz = z;

                if (_entityManager.TryGetActorEntity(actorId, out var entity) && entity != null)
                {
                    if (entity.hasMoveInput)
                    {
                        entity.ReplaceMoveInput(dx, dz);
                    }
                    else
                    {
                        entity.AddMoveInput(dx, dz);
                    }
                    Log.Debug($"[ETMobaInputSink] ApplyMoveInput: ActorId={actorId}, Dx={dx:F2}, Dz={dz:F2}");
                }
                else
                {
                    Log.Warning($"[ETMobaInputSink] ApplyMoveInput: ActorEntity not found for ActorId={actorId}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaInputSink] ApplyMoveInput failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理技能命令
        /// Payload 格式: int(slot) + float(targetX) + float(targetZ)
        /// </summary>
        private void HandleSkillInput(int actorId, PlayerInputCommand cmd)
        {
            if (actorId <= 0)
            {
                return;
            }

            if (cmd.Payload == null || cmd.Payload.Length < 12)
            {
                Log.Debug($"[ETMobaInputSink] HandleSkillInput: invalid payload for ActorId={actorId}");
                return;
            }

            // 解析技能槽位和目标坐标
            int slot = BitConverter.ToInt32(cmd.Payload, 0);
            float targetX = BitConverter.ToSingle(cmd.Payload, 4);
            float targetZ = BitConverter.ToSingle(cmd.Payload, 8);

            Log.Info($"[ETMobaInputSink] HandleSkillInput: ActorId={actorId}, Slot={slot}, Target=({targetX:F2}, {targetZ:F2})");

            // 触发回调：(actorId, targetId=actorId, slot, targetX, targetZ)
            OnSkillCommand?.Invoke(actorId, actorId, slot, targetX, targetZ);
        }

        /// <summary>
        /// 解析 PlayerId 为 ActorId
        /// </summary>
        private static int ParseActorId(PlayerId playerId)
        {
            if (string.IsNullOrEmpty(playerId.Value))
            {
                return 0;
            }

            if (int.TryParse(playerId.Value, out var actorId))
            {
                return actorId;
            }

            return 0;
        }

        public void Dispose()
        {
            _disposed = true;
            _services = null;
            _entityManager = null;
            OnMoveCommand = null;
            OnSkillCommand = null;
        }
    }
}
