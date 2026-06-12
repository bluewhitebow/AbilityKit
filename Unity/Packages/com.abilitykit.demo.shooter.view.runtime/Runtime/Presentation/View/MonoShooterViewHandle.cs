namespace AbilityKit.Demo.Shooter.View
{
    public sealed class MonoShooterViewHandle
    {
        public uint EntityId;
        public IMonoShooterViewHandleRegistry Registry;

        public void OnDestroy()
        {
            Registry?.OnMonoViewHandleDestroyed(this);
        }
    }
}