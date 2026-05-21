using EC = AbilityKit.World.ECS;
using AbilityKit.Demo.Moba.Console.Battle.ECS.Components;
using AbilityKit.Demo.Moba.Console.Battle.ECS.Entities;

namespace AbilityKit.Demo.Moba.Console.Battle.ECS
{
    /// <summary>
    /// 战斗实体查询接口
    /// 提供统一的实体数据查询接口
    /// 对齐 Unity BattleEntityQuery
    /// </summary>
    public interface IBattleEntityQuery
    {
        /// <summary>
        /// ECS 世界
        /// </summary>
        EC.IECWorld World { get; }

        /// <summary>
        /// 实体查找器
        /// </summary>
        BattleEntityLookup Lookup { get; }

        /// <summary>
        /// 根据 NetId 解析实体
        /// </summary>
        bool TryResolve(BattleNetId netId, out EC.IEntity entity);

        /// <summary>
        /// 获取变换组件
        /// </summary>
        bool TryGetTransform(BattleNetId netId, out BattleTransformComponent transform);

        /// <summary>
        /// 获取角色组件
        /// </summary>
        bool TryGetCharacter(BattleNetId netId, out BattleCharacterComponent character);

        /// <summary>
        /// 获取投射物组件
        /// </summary>
        bool TryGetProjectile(BattleNetId netId, out BattleProjectileComponent projectile);

        /// <summary>
        /// 获取实体元数据
        /// </summary>
        bool TryGetMeta(BattleNetId netId, out BattleEntityMetaComponent meta);
    }

    /// <summary>
    /// 战斗实体查询实现
    /// </summary>
    public sealed class BattleEntityQuery : IBattleEntityQuery
    {
        public BattleEntityQuery(EC.IECWorld world, BattleEntityLookup lookup)
        {
            World = world ?? throw new System.ArgumentNullException(nameof(world));
            Lookup = lookup ?? throw new System.ArgumentNullException(nameof(lookup));
        }

        public EC.IECWorld World { get; }
        public BattleEntityLookup Lookup { get; }

        public bool TryResolve(BattleNetId netId, out EC.IEntity entity)
        {
            return Lookup.TryResolve(World, netId, out entity);
        }

        public bool TryGetTransform(BattleNetId netId, out BattleTransformComponent transform)
        {
            transform = null;
            if (!TryResolve(netId, out var e)) return false;
            return e.TryGetRef(out transform) && transform != null;
        }

        public bool TryGetCharacter(BattleNetId netId, out BattleCharacterComponent character)
        {
            character = null;
            if (!TryResolve(netId, out var e)) return false;
            return e.TryGetRef(out character) && character != null;
        }

        public bool TryGetProjectile(BattleNetId netId, out BattleProjectileComponent projectile)
        {
            projectile = null;
            if (!TryResolve(netId, out var e)) return false;
            return e.TryGetRef(out projectile) && projectile != null;
        }

        public bool TryGetMeta(BattleNetId netId, out BattleEntityMetaComponent meta)
        {
            meta = null;
            if (!TryResolve(netId, out var e)) return false;
            return e.TryGetRef(out meta) && meta != null;
        }
    }
}
