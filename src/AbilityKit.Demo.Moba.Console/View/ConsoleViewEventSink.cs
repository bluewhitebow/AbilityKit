using AbilityKit.Demo.Moba.Console.Events;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 版本的 View 事件接收器
    ///
    /// 负责订阅 BattleEventBus 事件并调用 View 接口进行表现
    ///
    /// 职责边界：
    /// - ✅ 订阅 BattleEventBus
    /// - ✅ 调用 View 接口进行表现（飘字、HP 显示等）
    /// - ❌ 不做任何数值计算
    /// - ❌ 不持有逻辑层引用
    ///
    /// 架构说明：
    /// - 与 moba.view 的 BattleViewEventSink 对齐
    /// - 表现层组件，主动订阅事件，不被外部调用
    /// </summary>
    public sealed class ConsoleViewEventSink : IDisposable
    {
        private readonly IConsoleBattleView _battleView;
        private bool _disposed;

        public ConsoleViewEventSink(IConsoleBattleView battleView)
        {
            _battleView = battleView ?? throw new ArgumentNullException(nameof(battleView));
        }

        /// <summary>
        /// 初始化：订阅 BattleEventBus
        /// </summary>
        public void Initialize()
        {
            BattleEventBus.Subscribe<DamageEvent>(OnDamage);
            BattleEventBus.Subscribe<HealEvent>(OnHeal);
            BattleEventBus.Subscribe<BuffAppliedEvent>(OnBuffApplied);
            BattleEventBus.Subscribe<EntityDestroyedEvent>(OnEntityDestroyed);
            BattleEventBus.Subscribe<EntityCreatedEvent>(OnEntityCreated);
            BattleEventBus.Subscribe<SkillExecutedEvent>(OnSkillExecuted);
            BattleEventBus.Subscribe<ProjectileHitEvent>(OnProjectileHit);

            Log.Trace("[ConsoleViewEventSink] Initialized and subscribed to events");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            BattleEventBus.Unsubscribe<DamageEvent>(OnDamage);
            BattleEventBus.Unsubscribe<HealEvent>(OnHeal);
            BattleEventBus.Unsubscribe<BuffAppliedEvent>(OnBuffApplied);
            BattleEventBus.Unsubscribe<EntityDestroyedEvent>(OnEntityDestroyed);
            BattleEventBus.Unsubscribe<EntityCreatedEvent>(OnEntityCreated);
            BattleEventBus.Unsubscribe<SkillExecutedEvent>(OnSkillExecuted);
            BattleEventBus.Unsubscribe<ProjectileHitEvent>(OnProjectileHit);

            Log.Trace("[ConsoleViewEventSink] Disposed and unsubscribed from events");
        }

        #region Event Handlers

        /// <summary>
        /// 处理伤害事件
        /// </summary>
        private void OnDamage(DamageEvent evt)
        {
            if (evt.TargetId <= 0) return;
            if (evt.Damage == 0) return;

            _battleView.ShowFloatingText(evt.TargetId, $"-{evt.Damage:F0}", false);
            _battleView.UpdateEntityHp(evt.TargetId, evt.CurrentHp, evt.MaxHp);

            if (evt.IsDead)
            {
                _battleView.ShowFloatingText(evt.TargetId, "DIED!", false);
            }

            Log.Trace($"[View] Damage: Actor#{evt.TargetId} took {evt.Damage:F0} from Actor#{evt.SourceId}, HP: {evt.CurrentHp}/{evt.MaxHp}");
        }

        /// <summary>
        /// 处理治疗事件
        /// </summary>
        private void OnHeal(HealEvent evt)
        {
            if (evt.TargetId <= 0) return;
            if (evt.Amount == 0) return;

            _battleView.ShowFloatingText(evt.TargetId, $"+{evt.Amount:F0}", true);
            _battleView.UpdateEntityHp(evt.TargetId, evt.CurrentHp, evt.MaxHp);

            Log.Trace($"[View] Heal: Actor#{evt.TargetId} +{evt.Amount:F0}, HP: {evt.CurrentHp}/{evt.MaxHp}");
        }

        /// <summary>
        /// 处理 Buff 添加事件
        /// </summary>
        private void OnBuffApplied(BuffAppliedEvent evt)
        {
            if (evt.TargetId <= 0) return;
            _battleView.ShowBuffApply(evt.TargetId, evt.BuffId, evt.CasterId);
            Log.Trace($"[View] Buff: Actor#{evt.TargetId} gained Buff#{evt.BuffId}");
        }

        /// <summary>
        /// 处理实体销毁事件
        /// </summary>
        private void OnEntityDestroyed(EntityDestroyedEvent evt)
        {
            if (evt.ActorId <= 0) return;

            _battleView.ShowFloatingText(evt.ActorId, "DIED!", false);
            _battleView.UpdateEntityHp(evt.ActorId, 0, 0);

            Log.Trace($"[View] Death: Actor#{evt.ActorId} died");
        }

        /// <summary>
        /// 处理实体创建事件
        /// </summary>
        private void OnEntityCreated(EntityCreatedEvent evt)
        {
            if (evt.ActorId <= 0) return;

            _battleView.RegisterEntity(
                evt.ActorId,
                evt.Name ?? $"Actor#{evt.ActorId}",
                "Character",
                evt.HP,
                evt.MaxHp,
                evt.X,
                0,
                evt.Z);

            Log.Trace($"[View] Entity: Actor#{evt.ActorId} ({evt.Name}) created at ({evt.X:F1}, {evt.Z:F1})");
        }

        /// <summary>
        /// 处理技能执行事件
        /// </summary>
        private void OnSkillExecuted(SkillExecutedEvent evt)
        {
            if (evt.Success)
            {
                Log.Trace($"[View] Skill: Actor#{evt.ActorId} executed skill in slot {evt.Slot}");
            }
            else
            {
                Log.Trace($"[View] Skill: Actor#{evt.ActorId} failed skill in slot {evt.Slot}: {evt.FailReason}");
            }
        }

        /// <summary>
        /// 处理弹道命中事件
        /// </summary>
        private void OnProjectileHit(ProjectileHitEvent evt)
        {
            Log.Trace($"[View] Projectile: Hit {evt.ProjectileId} on Actor#{evt.TargetId}");
        }

        #endregion
    }
}
