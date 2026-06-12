using System.Numerics;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterViewBinder : IShooterViewBinder, IMonoShooterViewHandleRegistry
    {
        private readonly ShooterViewHandleStore _handles = new ShooterViewHandleStore();
        private readonly ShooterViewShellController _shells;
        private readonly ShooterViewTransformController _transforms;
        private readonly ShooterViewEntitySyncController _sync;
        private readonly ShooterViewLifecycleController _lifecycle;
        private readonly ShooterViewHandleQuery _queries;

        public ShooterViewBinder(ShooterViewResourceProvider resources = null)
            : this(resources, null)
        {
        }

        internal ShooterViewBinder(ShooterViewResourceProvider resources, ShooterViewBinderControllerFactory controllers)
        {
            resources = ShooterViewResourceProvider.OrDefault(resources);
            controllers ??= new ShooterViewBinderControllerFactory();

            _shells = controllers.CreateShells(resources, this);
            _transforms = controllers.CreateTransforms(_handles);
            _sync = controllers.CreateSync(_handles, _shells, _transforms, resources);
            _lifecycle = controllers.CreateLifecycle(_handles, _shells, _transforms);
            _queries = controllers.CreateQueries(_handles);
        }

        public bool InterpolationEnabled
        {
            get => _transforms.InterpolationEnabled;
            set => _transforms.InterpolationEnabled = value;
        }

        public float BackTimeTicks
        {
            get => _transforms.BackTimeTicks;
            set => _transforms.BackTimeTicks = value;
        }

        public float MaxLagTicks
        {
            get => _transforms.MaxLagTicks;
            set => _transforms.MaxLagTicks = value;
        }

        public bool TryGetShellGameObject(uint entityId, out object go)
        {
            return _queries.TryGetShellGameObject(entityId, out go);
        }

        public bool TryGetInterpolatedPos(uint entityId, out Vector3 pos)
        {
            return _transforms.TryGetInterpolatedPos(entityId, out pos);
        }

        public void Sync(in ShooterSnapshotViewBatch batch)
        {
            _sync.Sync(in batch);
        }

        public void TickInterpolation(float deltaTime)
        {
            _transforms.Tick(deltaTime);
        }

        public void OnDestroyed(uint entityId)
        {
            _lifecycle.OnDestroyed(entityId);
        }

        public void Clear()
        {
            _lifecycle.Clear();
        }

        public void RebindAll()
        {
        }

        void IMonoShooterViewHandleRegistry.OnMonoViewHandleDestroyed(MonoShooterViewHandle handle)
        {
            _lifecycle.OnMonoViewHandleDestroyed(handle);
        }
    }
}