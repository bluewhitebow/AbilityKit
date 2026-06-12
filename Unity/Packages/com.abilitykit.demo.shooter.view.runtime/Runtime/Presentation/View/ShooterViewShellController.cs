namespace AbilityKit.Demo.Shooter.View
{
    internal sealed class ShooterViewShellController
    {
        private readonly IShooterViewShellLoader _shellLoader;
        private readonly IMonoShooterViewHandleRegistry _registry;

        public ShooterViewShellController(
            IShooterViewShellLoader shellLoader,
            IMonoShooterViewHandleRegistry registry)
        {
            _shellLoader = shellLoader;
            _registry = registry;
        }

        public object CreateShell(uint entityId, ShooterViewEntityKind kind, int modelId)
        {
            var shell = _shellLoader.LoadShell(kind, modelId);
            if (shell == null)
                return null;

            return shell;
        }

        public void DestroyShell(object shell)
        {
        }
    }
}