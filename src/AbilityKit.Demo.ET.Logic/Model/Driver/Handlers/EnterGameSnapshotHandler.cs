using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 进入游戏快照处理器
    /// </summary>
    [SnapshotHandler(SnapshotType.Full)]
    public sealed class EnterGameSnapshotHandler : ISnapshotHandler
    {
        public SnapshotType SnapshotType => SnapshotType.Full;

        public bool CanHandle(in FrameSnapshotData snapshot)
        {
            return snapshot.EnterGame.HasValue;
        }

        public void Handle(ETMobaBattleDriver driver, in FrameSnapshotData snapshot)
        {
            Log.Info($"[EnterGameSnapshotHandler] Frame={snapshot.FrameIndex}, Spawns={snapshot.ActorSpawns?.Count ?? 0}");

            // 通知视图层
            driver.ViewSink?.OnEnterGameSnapshot(in snapshot);
        }
    }
}
