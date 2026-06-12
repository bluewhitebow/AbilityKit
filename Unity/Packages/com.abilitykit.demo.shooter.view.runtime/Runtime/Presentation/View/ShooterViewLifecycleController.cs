using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.View
{
    internal sealed class ShooterViewLifecycleController
    {
        private readonly ShooterViewHandleStore _handles;
        private readonly ShooterViewShellController _shells;
        private readonly ShooterViewTransformController _transforms;

        public ShooterViewLifecycleController(
            ShooterViewHandleStore handles,
            ShooterViewShellController shells,
            ShooterViewTransformController transforms)
        {
            _handles = handles;
            _shells = shells;
            _transforms = transforms;
        }

        public void OnDestroyed(uint entityId)
        {
            if (_handles.TryGet(entityId, out var handle))
            {
                if (handle.GameObject != null)
                {
                    _shells.DestroyShell(handle.GameObject);
                }
                handle.Destroyed = true;
                handle.GameObject = null;
                handle.ViewHandle = null;
                handle.PosBuffer?.Clear();
                _handles.Remove(entityId);
            }
        }

        public void OnMonoViewHandleDestroyed(MonoShooterViewHandle handle)
        {
            OnDestroyed(handle.EntityId);
        }

        public void Clear()
        {
            foreach (var handle in _handles.GetAll())
            {
                if (handle.GameObject != null)
                {
                    _shells.DestroyShell(handle.GameObject);
                }
            }
            var entities = new List<uint>();
            foreach (var handle in _handles.GetAll())
            {
                entities.Add(handle.EntityId);
            }
            foreach (var entityId in entities)
            {
                _handles.Remove(entityId);
            }
        }
    }
}