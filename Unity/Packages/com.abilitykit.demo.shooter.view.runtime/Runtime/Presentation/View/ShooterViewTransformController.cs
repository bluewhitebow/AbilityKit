using System.Numerics;

namespace AbilityKit.Demo.Shooter.View
{
    internal sealed class ShooterViewTransformController
    {
        private readonly ShooterViewHandleStore _handles;
        private float _renderTimeTicks;

        public bool InterpolationEnabled { get; set; } = true;
        public float BackTimeTicks { get; set; } = 1f;
        public float MaxLagTicks { get; set; } = 4f;

        public ShooterViewTransformController(ShooterViewHandleStore handles)
        {
            _handles = handles;
        }

        public void Tick(float deltaTime)
        {
            if (!InterpolationEnabled)
                return;

            _renderTimeTicks += deltaTime * 60f;

            foreach (var handle in _handles.GetAll())
            {
                if (handle.GameObject == null || handle.Destroyed)
                    continue;

                if (handle.HasPendingPos)
                {
                    ApplyPosition(handle.GameObject, handle.PendingPos);
                }
                else if (handle.PosBuffer != null)
                {
                    float targetTime = _renderTimeTicks - BackTimeTicks;
                    if (handle.PosBuffer.TryEvaluate(targetTime, out Vector3 pos, out Quaternion rot))
                    {
                        ApplyPosition(handle.GameObject, pos);
                        ApplyRotation(handle.GameObject, rot);
                    }
                }
            }
        }

        private void ApplyPosition(object gameObject, Vector3 position)
        {
        }

        private void ApplyRotation(object gameObject, Quaternion rotation)
        {
        }

        public bool TryGetInterpolatedPos(uint entityId, out Vector3 pos)
        {
            pos = Vector3.Zero;

            if (!_handles.TryGet(entityId, out var handle))
                return false;

            if (handle.PosBuffer == null)
                return false;

            float targetTime = _renderTimeTicks - BackTimeTicks;
            return handle.PosBuffer.TryEvaluate(targetTime, out pos, out _);
        }

        public float GetRenderTime()
        {
            return _renderTimeTicks;
        }
    }
}