namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaResolvedShootProjectileParams
    {
        public MobaResolvedShootProjectileParams(int launcherId, int projectileId, int countPerShot, float fanAngleDeg, int durationMs)
        {
            LauncherId = launcherId;
            ProjectileId = projectileId;
            CountPerShot = countPerShot < 1 ? 1 : countPerShot;
            FanAngleDeg = fanAngleDeg < 0f ? 0f : fanAngleDeg;
            DurationMs = durationMs < 0 ? 0 : durationMs;
        }

        public int LauncherId { get; }
        public int ProjectileId { get; }
        public int CountPerShot { get; }
        public float FanAngleDeg { get; }
        public int DurationMs { get; }
    }
}
