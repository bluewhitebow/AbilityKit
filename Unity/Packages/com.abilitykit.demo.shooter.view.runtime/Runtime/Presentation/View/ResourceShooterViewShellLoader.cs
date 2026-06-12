namespace AbilityKit.Demo.Shooter.View
{
    internal sealed class ResourceShooterViewShellLoader : IShooterViewShellLoader
    {
        private readonly ShooterViewResourceProvider _resources;

        public ResourceShooterViewShellLoader(ShooterViewResourceProvider resources)
        {
            _resources = resources;
        }

        public object LoadShell(ShooterViewEntityKind kind, int modelId)
        {
            return _resources.LoadModelPrefab(modelId);
        }
    }
}