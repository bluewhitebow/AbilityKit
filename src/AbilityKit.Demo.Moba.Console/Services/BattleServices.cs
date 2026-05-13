using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 战斗服务接口
    /// 提供战斗系统需要的基础服务
    /// </summary>
    public interface IBattleServices
    {
        /// <summary>
        /// 获取角色信息
        /// </summary>
        ActorInfo? GetActor(int actorId);

        /// <summary>
        /// 查找指定位置的角色
        /// </summary>
        int FindActorAtPosition(float x, float z);

        /// <summary>
        /// 应用伤害
        /// </summary>
        void ApplyDamage(int targetActorId, float damage, int sourceActorId, int skillId);

        /// <summary>
        /// 技能施放事件
        /// </summary>
        void OnSkillCast(int casterActorId, int skillId, int slot);

        /// <summary>
        /// 移动输入
        /// </summary>
        void OnMoveInput(int actorId, float dx, float dz);
    }

    /// <summary>
    /// 角色信息
    /// </summary>
    public sealed class ActorInfo
    {
        public int ActorId { get; set; }
        public string Name { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Hp { get; set; }
        public float HpMax { get; set; }
        public float Attack { get; set; }
        public float Defense { get; set; }
        public int TeamId { get; set; }

        public ActorInfo() { }

        public ActorInfo(int actorId, string name)
        {
            ActorId = actorId;
            Name = name;
        }
    }

    /// <summary>
    /// 战斗服务实现
    /// </summary>
    public sealed class BattleServices : IBattleServices
    {
        private readonly BattleViewServices _viewServices;
        private readonly Dictionary<int, ActorInfo> _actors = new();
        private readonly Dictionary<(int, int), int> _positionIndex = new();

        public BattleServices(BattleViewServices viewServices)
        {
            _viewServices = viewServices ?? throw new ArgumentNullException(nameof(viewServices));
        }

        /// <summary>
        /// 注册角色
        /// </summary>
        public void RegisterActor(ActorInfo actor)
        {
            if (actor == null) return;
            _actors[actor.ActorId] = actor;
            UpdatePositionIndex(actor);
            Log.Entity($"[BattleServices] Registered actor: #{actor.ActorId} {actor.Name}");
        }

        /// <summary>
        /// 移除角色
        /// </summary>
        public void UnregisterActor(int actorId)
        {
            if (_actors.TryGetValue(actorId, out var actor))
            {
                RemovePositionIndex(actor);
                _actors.Remove(actorId);
                Log.Entity($"[BattleServices] Unregistered actor: #{actorId}");
            }
        }

        /// <summary>
        /// 更新角色位置
        /// </summary>
        public void UpdateActorPosition(int actorId, float x, float y, float z)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;

            RemovePositionIndex(actor);
            actor.X = x;
            actor.Y = y;
            actor.Z = z;
            UpdatePositionIndex(actor);
        }

        /// <summary>
        /// 更新角色属性
        /// </summary>
        public void UpdateActorStats(int actorId, float? hp = null, float? hpMax = null)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;

            if (hp.HasValue) actor.Hp = hp.Value;
            if (hpMax.HasValue) actor.HpMax = hpMax.Value;

            if (hp.HasValue)
            {
                _viewServices?.ShowDamage(actorId, actor.Hp, actor.HpMax);
            }
        }

        /// <summary>
        /// 获取角色信息
        /// </summary>
        public ActorInfo? GetActor(int actorId)
        {
            return _actors.TryGetValue(actorId, out var actor) ? actor : null;
        }

        /// <summary>
        /// 查找指定位置的角色
        /// </summary>
        public int FindActorAtPosition(float x, float z)
        {
            return FindActorAtPosition(x, z, 1.5f);
        }

        /// <summary>
        /// 查找指定位置的角色（带范围）
        /// </summary>
        public int FindActorAtPosition(float x, float z, float range)
        {
            foreach (var actor in _actors.Values)
            {
                var dx = actor.X - x;
                var dz = actor.Z - z;
                var dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist <= range)
                {
                    return actor.ActorId;
                }
            }
            return 0;
        }

        /// <summary>
        /// 应用伤害
        /// </summary>
        public void ApplyDamage(int targetActorId, float damage, int sourceActorId, int skillId)
        {
            if (!_actors.TryGetValue(targetActorId, out var target)) return;

            var actualDamage = Math.Max(1f, damage); // 至少造成1点伤害
            target.Hp = Math.Max(0f, target.Hp - actualDamage);

            Log.Damage($"[Damage] #{sourceActorId} dealt {actualDamage:F1} damage to #{targetActorId} (Skill:{skillId}). HP: {target.Hp:F0}/{target.HpMax:F0}");

            // 通知视图层
            _viewServices?.ShowDamage(targetActorId, target.Hp, target.HpMax);

            // 检查死亡
            if (target.Hp <= 0)
            {
                OnActorDied(targetActorId, sourceActorId);
            }
        }

        /// <summary>
        /// 技能施放事件
        /// </summary>
        public void OnSkillCast(int casterActorId, int skillId, int slot)
        {
            if (!_actors.TryGetValue(casterActorId, out var caster)) return;

            Log.Skill($"[Skill] Actor #{casterActorId} casted skill {skillId} (slot {slot})");
            _viewServices?.ShowSkillEffect(casterActorId, skillId);
        }

        /// <summary>
        /// 移动输入
        /// </summary>
        public void OnMoveInput(int actorId, float dx, float dz)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;

            // 简单移动处理
            const float moveSpeed = 5f;
            actor.X += dx * moveSpeed * 0.033f; // 假设 30 FPS
            actor.Z += dz * moveSpeed * 0.033f;

            UpdatePositionIndex(actor);
            Log.Sync($"[Move] Actor #{actorId} -> ({actor.X:F1}, {actor.Z:F1})");
        }

        /// <summary>
        /// 角色死亡
        /// </summary>
        private void OnActorDied(int actorId, int killerActorId)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;

            Log.Battle($"[Battle] Actor #{actorId} ({actor.Name}) was killed by #{killerActorId}");

            // 通知视图层
            _viewServices?.ShowDeath(actorId, killerActorId);

            // 移除角色
            UnregisterActor(actorId);
        }

        private void UpdatePositionIndex(ActorInfo actor)
        {
            var key = ((int)Math.Round(actor.X), (int)Math.Round(actor.Z));
            _positionIndex[key] = actor.ActorId;
        }

        private void RemovePositionIndex(ActorInfo actor)
        {
            var key = ((int)Math.Round(actor.X), (int)Math.Round(actor.Z));
            _positionIndex.Remove(key);
        }
    }

    /// <summary>
    /// 战斗视图服务接口
    /// </summary>
    public interface BattleViewServices
    {
        void ShowDamage(int actorId, float hp, float hpMax);
        void ShowSkillEffect(int actorId, int skillId);
        void ShowDeath(int actorId, int killerActorId);
    }
}
