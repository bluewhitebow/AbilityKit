using System.Numerics;

namespace AbilityKit.Demo.Shooter.View
{
    public struct ShooterViewHandle
    {
        public int Version;
        public bool Destroyed;
        public uint EntityId;
        public int ModelId;
        public object GameObject;
        public MonoShooterViewHandle ViewHandle;
        public Vector3 PendingPos;
        public bool HasPendingPos;
        public ShooterViewPositionSampleBuffer PosBuffer;
    }
}