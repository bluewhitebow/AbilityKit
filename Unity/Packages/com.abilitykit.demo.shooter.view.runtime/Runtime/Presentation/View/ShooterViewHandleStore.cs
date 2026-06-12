using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.View
{
    internal sealed class ShooterViewHandleStore
    {
        private readonly Dictionary<uint, ShooterViewHandle> _handles = new Dictionary<uint, ShooterViewHandle>();

        public bool TryGet(uint entityId, out ShooterViewHandle handle)
        {
            return _handles.TryGetValue(entityId, out handle);
        }

        public void Set(uint entityId, in ShooterViewHandle handle)
        {
            _handles[entityId] = handle;
        }

        public bool Remove(uint entityId)
        {
            return _handles.Remove(entityId);
        }

        public int Count => _handles.Count;

        public Dictionary<uint, ShooterViewHandle>.ValueCollection.Enumerator GetEnumerator()
        {
            return _handles.Values.GetEnumerator();
        }

        public IEnumerable<ShooterViewHandle> GetAll()
        {
            return _handles.Values;
        }
    }
}