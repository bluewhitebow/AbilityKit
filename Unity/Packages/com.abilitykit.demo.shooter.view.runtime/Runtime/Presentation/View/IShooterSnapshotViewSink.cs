#nullable enable

namespace AbilityKit.Demo.Shooter.View
{
    public interface IShooterSnapshotViewSink
    {
        void ApplySnapshot(in ShooterSnapshotViewBatch batch);

        void Clear();
    }
}
