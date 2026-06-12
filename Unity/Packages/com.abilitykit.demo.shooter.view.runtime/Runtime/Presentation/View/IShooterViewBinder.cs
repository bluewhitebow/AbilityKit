namespace AbilityKit.Demo.Shooter.View
{
    public interface IShooterViewBinder
    {
        bool InterpolationEnabled { get; set; }
        void Sync(in ShooterSnapshotViewBatch batch);
        void TickInterpolation(float deltaTime);
        void Clear();
        void RebindAll();
    }
}