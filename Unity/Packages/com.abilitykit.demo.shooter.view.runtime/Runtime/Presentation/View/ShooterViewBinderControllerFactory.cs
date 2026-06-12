namespace AbilityKit.Demo.Shooter.View
{
    internal sealed class ShooterViewBinderControllerFactory
    {
        public ShooterViewShellController CreateShells(
            IShooterViewShellLoader shellLoader,
            IMonoShooterViewHandleRegistry registry)
        {
            return new ShooterViewShellController(shellLoader, registry);
        }

        public ShooterViewShellController CreateShells(
            ShooterViewResourceProvider resources,
            IMonoShooterViewHandleRegistry registry)
        {
            return CreateShells(new ResourceShooterViewShellLoader(resources), registry);
        }

        public ShooterViewTransformController CreateTransforms(ShooterViewHandleStore handles)
        {
            return new ShooterViewTransformController(handles);
        }

        public ShooterViewEntitySyncController CreateSync(
            ShooterViewHandleStore handles,
            ShooterViewShellController shells,
            ShooterViewTransformController transforms,
            ShooterViewResourceProvider resources)
        {
            return new ShooterViewEntitySyncController(handles, shells, transforms, resources);
        }

        public ShooterViewLifecycleController CreateLifecycle(
            ShooterViewHandleStore handles,
            ShooterViewShellController shells,
            ShooterViewTransformController transforms)
        {
            return new ShooterViewLifecycleController(handles, shells, transforms);
        }

        public ShooterViewHandleQuery CreateQueries(ShooterViewHandleStore handles)
        {
            return new ShooterViewHandleQuery(handles);
        }
    }
}