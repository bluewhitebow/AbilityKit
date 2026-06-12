namespace AbilityKit.Demo.Shooter.View
{
    internal sealed class ShooterViewHandleQuery
    {
        private readonly ShooterViewHandleStore _handles;

        public ShooterViewHandleQuery(ShooterViewHandleStore handles)
        {
            _handles = handles;
        }

        public bool TryGetShellGameObject(uint entityId, out object go)
        {
            go = null;
            if (_handles.TryGet(entityId, out var handle))
            {
                go = handle.GameObject;
                return go != null;
            }
            return false;
        }

        public bool TryGetAttachRoot(uint entityId, out object transform)
        {
            transform = null;
            if (_handles.TryGet(entityId, out var handle) && handle.GameObject != null)
            {
                transform = handle.GameObject;
                return true;
            }
            return false;
        }

        public void ForEachShellGameObject(System.Action<int, uint, object> visitor)
        {
            foreach (var handle in _handles.GetAll())
            {
                if (handle.GameObject != null)
                {
                    visitor(handle.ModelId, handle.EntityId, handle.GameObject);
                }
            }
        }
    }
}