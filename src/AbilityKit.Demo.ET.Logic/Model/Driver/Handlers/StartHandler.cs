using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Services;

namespace ET.Logic
{
    /// <summary>
    /// 启动处理器
    /// </summary>
    [LifecycleHandler(LifecyclePhase.Start)]
    public sealed class StartHandler : IStartHandler
    {
        public LifecyclePhase Phase => LifecyclePhase.Start;

        public void Handle(ETMobaBattleDriver driver)
        {
            // 从 World.Services 获取 InputSink（由 MobaWorldBootstrapModule 注册的 MobaLobbyInputSink）
            if (driver.World?.Services == null ||
                !driver.World.Services.TryResolve<IWorldInputSink>(out var inputSink) ||
                inputSink == null)
            {
                Log.Error("[StartHandler] IWorldInputSink not registered in World.Services!");
                throw new InvalidOperationException("IWorldInputSink must be registered");
            }

            // 尝试转换为 ETMobaInputSink（如果可用，用于注册回调）
            if (inputSink is ETMobaInputSink etMobaInputSink)
            {
                // 注册移动命令回调 - 用于调试和追踪（仅 ETMobaInputSink 支持）
                etMobaInputSink.OnMoveCommand += (actorId, unused, x, z) =>
                {
                    Log.Debug($"[StartHandler] Move command: ActorId={actorId} -> ({x:F2}, {z:F2})");
                };
                Log.Info("[StartHandler] Using ETMobaInputSink with callbacks");
            }
            else
            {
                // 使用 MobaLobbyInputSink，它不支持 OnMoveCommand 回调
                Log.Info("[StartHandler] Using MobaLobbyInputSink (callbacks not available)");
            }

            driver.InputSink = inputSink;

            driver.IsRunning = true;
            driver.LastTickTime = GetCurrentTimeSeconds();
            driver.CurrentFrame = 0;
            driver.LogicTimeSeconds = 0;

            Log.Info("[StartHandler] Done - IWorldInputSink obtained from World.Services");
        }

        private static double GetCurrentTimeSeconds()
        {
            return (double)Environment.TickCount64 / 1000.0;
        }
    }
}
