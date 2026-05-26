using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Services;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    /// <summary>
    /// 移动输入处理器
    /// </summary>
    [InputHandler(3003)] // Move = 3003
    public sealed class MoveInputHandler : ISubmittableInputHandler
    {
        public const int MoveOpCode = 3003;

        public int OpCode => MoveOpCode;

        public bool CanHandle(int opCode) => opCode == MoveOpCode;

        public void Handle(ETMobaBattleDriver driver, int frame, PlayerInputCommand input)
        {
            // 解析移动坐标
            MobaMoveCodec.Deserialize(input.Payload, out var targetX, out var targetZ);
            int actorId = PlayerIdUtils.ToActorId(input.Player);

            Log.Debug($"[MoveInputHandler] Handle: ActorId={actorId}, Target=({targetX:F2}, {targetZ:F2})");

            // 提交移动输入
            Submit(driver, actorId, targetX, targetZ);
        }

        public void Submit(ETMobaBattleDriver driver, int actorId, float targetX, float targetZ)
        {
            // 使用 driver.InputSink 提交输入
            // driver.InputSink 在 StartHandler 中初始化
            var inputSink = driver.InputSink;
            if (inputSink == null)
            {
                Log.Error($"[MoveInputHandler] InputSink is null! Driver not started properly.");
                return;
            }

            var payload = MobaMoveCodec.Serialize(targetX, targetZ);
            var playerId = PlayerIdUtils.ToPlayerId(actorId);
            var frameIndex = new FrameIndex(driver.CurrentFrame);
            var command = new PlayerInputCommand(frameIndex, playerId, MoveOpCode, payload);

            inputSink.Submit(frameIndex, new List<PlayerInputCommand> { command });
            Log.Debug($"[MoveInputHandler] Submit: ActorId={actorId}, Position=({targetX:F2}, {targetZ:F2})");
        }
    }
}
