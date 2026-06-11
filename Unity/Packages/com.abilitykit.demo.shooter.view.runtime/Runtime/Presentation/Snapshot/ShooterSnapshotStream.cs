#nullable enable

using System;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterSnapshotStream
    {
        public event Action<ShooterSnapshotViewBatch>? SnapshotApplied;

        public void Publish(in ShooterSnapshotViewBatch batch)
        {
            SnapshotApplied?.Invoke(batch);
        }
    }
}
