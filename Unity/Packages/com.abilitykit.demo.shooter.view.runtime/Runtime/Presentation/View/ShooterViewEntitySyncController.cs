using System.Numerics;

namespace AbilityKit.Demo.Shooter.View
{
    internal sealed class ShooterViewEntitySyncController
    {
        private readonly ShooterViewHandleStore _handles;
        private readonly ShooterViewShellController _shells;
        private readonly ShooterViewTransformController _transforms;
        private readonly ShooterViewResourceProvider _resources;

        public ShooterViewEntitySyncController(
            ShooterViewHandleStore handles,
            ShooterViewShellController shells,
            ShooterViewTransformController transforms,
            ShooterViewResourceProvider resources)
        {
            _handles = handles;
            _shells = shells;
            _transforms = transforms;
            _resources = resources;
        }

        public void Sync(in ShooterSnapshotViewBatch batch)
        {
            foreach (var entity in batch.EntityChanges)
            {
                if (entity.Alive)
                {
                    SyncEntity((uint)entity.Key.EntityId, entity.Key.Kind);
                }
                else
                {
                    _handles.Remove((uint)entity.Key.EntityId);
                }
            }
        }

        private void SyncEntity(uint entityId, ShooterViewEntityKind kind)
        {
            if (!_handles.TryGet(entityId, out var handle))
            {
                handle = new ShooterViewHandle
                {
                    EntityId = entityId,
                    Version = 0,
                    Destroyed = false,
                    PosBuffer = new ShooterViewPositionSampleBuffer()
                };
            }

            _handles.Set(entityId, handle);
        }
    }
}