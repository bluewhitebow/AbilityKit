using System.Numerics;

namespace AbilityKit.Game.View
{
    public interface IViewHandle
    {
        uint EntityId { get; }
        int ModelId { get; set; }
        object Shell { get; set; }
        bool HasPendingPosition { get; set; }
        Vector3 PendingPosition { get; set; }
        bool IsDestroyed { get; set; }
        int Version { get; set; }
    }
}