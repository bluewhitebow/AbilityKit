using System;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 战斗视图事件接口
    /// </summary>
    public interface IBattleViewEventSink
    {
        /// <summary>
        /// 角色生成事件
        /// </summary>
        void OnActorSpawn(ActorSpawnEvent evt);

        /// <summary>
        /// 角色销毁事件
        /// </summary>
        void OnActorDespawn(ActorDespawnEvent evt);

        /// <summary>
        /// 角色位置快照
        /// </summary>
        void OnActorTransformSnapshot(ActorTransformEvent evt);

        /// <summary>
        /// 伤害事件
        /// </summary>
        void OnDamageEvent(DamageEvent evt);

        /// <summary>
        /// 技能施放事件
        /// </summary>
        void OnSkillCastEvent(SkillCastEvent evt);
    }

    #region 事件结构体

    public readonly struct ActorSpawnEvent
    {
        public int ActorId { get; }
        public int EntityCode { get; }
        public string Name { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float MaxHp { get; }
        public float MaxMp { get; }
        public int TeamId { get; }

        public ActorSpawnEvent(int actorId, int entityCode, string name, float x, float y, float z, float maxHp, float maxMp, int teamId)
        {
            ActorId = actorId;
            EntityCode = entityCode;
            Name = name;
            X = x;
            Y = y;
            Z = z;
            MaxHp = maxHp;
            MaxMp = maxMp;
            TeamId = teamId;
        }
    }

    public readonly struct ActorDespawnEvent
    {
        public int ActorId { get; }
        public int KillerActorId { get; }

        public ActorDespawnEvent(int actorId, int killerActorId = 0)
        {
            ActorId = actorId;
            KillerActorId = killerActorId;
        }
    }

    public readonly struct ActorTransformEvent
    {
        public int ActorId { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float DirX { get; }
        public float DirZ { get; }

        public ActorTransformEvent(int actorId, float x, float y, float z, float dirX = 0, float dirZ = 1)
        {
            ActorId = actorId;
            X = x;
            Y = y;
            Z = z;
            DirX = dirX;
            DirZ = dirZ;
        }
    }

    public readonly struct DamageEvent
    {
        public int TargetActorId { get; }
        public int SourceActorId { get; }
        public float Damage { get; }
        public int SkillId { get; }
        public float CurrentHp { get; }
        public float MaxHp { get; }
        public bool IsCritical { get; }

        public DamageEvent(int targetActorId, int sourceActorId, float damage, int skillId, float currentHp, float maxHp, bool isCritical = false)
        {
            TargetActorId = targetActorId;
            SourceActorId = sourceActorId;
            Damage = damage;
            SkillId = skillId;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            IsCritical = isCritical;
        }
    }

    public readonly struct SkillCastEvent
    {
        public int CasterActorId { get; }
        public int SkillId { get; }
        public int Slot { get; }
        public float AimX { get; }
        public float AimZ { get; }
        public int TargetActorId { get; }

        public SkillCastEvent(int casterActorId, int skillId, int slot, float aimX, float aimZ, int targetActorId)
        {
            CasterActorId = casterActorId;
            SkillId = skillId;
            Slot = slot;
            AimX = aimX;
            AimZ = aimZ;
            TargetActorId = targetActorId;
        }
    }

    #endregion

    /// <summary>
    /// Console 战斗视图事件处理器
    /// </summary>
    public sealed class ConsoleBattleViewEventSink : IBattleViewEventSink
    {
        private readonly IConsoleBattleView _battleView;

        public ConsoleBattleViewEventSink(IConsoleBattleView battleView)
        {
            _battleView = battleView ?? throw new ArgumentNullException(nameof(battleView));
        }

        public void OnActorSpawn(ActorSpawnEvent evt)
        {
            Log.View($"[View] Actor spawned: #{evt.ActorId} {evt.Name} at ({evt.X:F1}, {evt.Z:F1})");
            _battleView.RegisterEntity(evt.ActorId, evt.Name, "Character", evt.MaxHp, evt.MaxHp, evt.X, evt.Y, evt.Z);
        }

        public void OnActorDespawn(ActorDespawnEvent evt)
        {
            Log.View($"[View] Actor despawned: #{evt.ActorId}");
            if (evt.KillerActorId != 0)
            {
                _battleView.ShowFloatingText(evt.ActorId, "DIED!", false);
            }
        }

        public void OnActorTransformSnapshot(ActorTransformEvent evt)
        {
            // 使用 RegisterEntity 更新位置，或者调用 UpdateEntityHp 之类的方法
            // 由于 IConsoleBattleView 没有 UpdateEntityPosition 方法，这里只记录日志
            Log.View($"[View] Actor #{evt.ActorId} moved to ({evt.X:F1}, {evt.Z:F1})");
        }

        public void OnDamageEvent(DamageEvent evt)
        {
            Log.Damage($"[Damage] #{evt.SourceActorId} -> #{evt.TargetActorId}: -{evt.Damage:F0} HP ({evt.CurrentHp:F0}/{evt.MaxHp:F0})");
            _battleView.UpdateEntityHp(evt.TargetActorId, evt.CurrentHp, evt.MaxHp);
            _battleView.ShowFloatingText(evt.TargetActorId, $"-{evt.Damage:F0}", evt.IsCritical);

            if (evt.CurrentHp <= 0)
            {
                _battleView.ShowFloatingText(evt.TargetActorId, "DEAD!", false);
            }
        }

        public void OnSkillCastEvent(SkillCastEvent evt)
        {
            Log.Skill($"[Skill] Actor #{evt.CasterActorId} cast skill {evt.SkillId} (slot {evt.Slot})");

            if (evt.TargetActorId != 0)
            {
                Log.Skill($"[Skill]   Target: #{evt.TargetActorId}");
            }
            else if (evt.AimX != 0 || evt.AimZ != 0)
            {
                Log.Skill($"[Skill]   Position: ({evt.AimX:F1}, {evt.AimZ:F1})");
            }
        }
    }
}
