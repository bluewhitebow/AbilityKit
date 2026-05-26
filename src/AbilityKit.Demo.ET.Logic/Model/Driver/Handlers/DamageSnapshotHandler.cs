using System;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 伤害事件快照处理器
    /// </summary>
    [SnapshotHandler(SnapshotType.Delta)]
    public sealed class DamageSnapshotHandler : ISnapshotHandler
    {
        public SnapshotType SnapshotType => SnapshotType.Delta;

        public bool CanHandle(in FrameSnapshotData snapshot)
        {
            return snapshot.DamageEvents != null && snapshot.DamageEvents.Count > 0;
        }

        public void Handle(ETMobaBattleDriver driver, in FrameSnapshotData snapshot)
        {
            Log.Debug($"[DamageSnapshotHandler] Frame={snapshot.FrameIndex}, Count={snapshot.DamageEvents?.Count ?? 0}");

            driver.ViewSink?.OnDamageEventSnapshot(in snapshot);
        }
    }
}
