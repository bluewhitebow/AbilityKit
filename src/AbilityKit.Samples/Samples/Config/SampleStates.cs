using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Samples.Config
{
    /// <summary>
    /// 待机状态
    /// </summary>
    [StateTypeId("Idle")]
    public sealed class IdleState
    {
        public float PatrolRadius { get; set; } = 10f;
        public float DetectionRange { get; set; } = 15f;
    }

    /// <summary>
    /// 追逐状态
    /// </summary>
    [StateTypeId("Chase")]
    public sealed class ChaseState
    {
        public float MoveSpeed { get; set; } = 5f;
        public float ChaseDistance { get; set; } = 2f;
    }

    /// <summary>
    /// 攻击状态
    /// </summary>
    [StateTypeId("Attack")]
    public sealed class AttackState
    {
        public float AttackRange { get; set; } = 1.5f;
        public float AttackDamage { get; set; } = 50f;
        public float AttackCooldown { get; set; } = 1.5f;
    }

    /// <summary>
    /// 死亡状态
    /// </summary>
    [StateTypeId("Dead")]
    public sealed class DeadState
    {
        public float FadeDuration { get; set; } = 2f;
    }
}
