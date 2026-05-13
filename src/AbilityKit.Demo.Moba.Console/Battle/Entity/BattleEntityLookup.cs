using System;
using System.Collections.Generic;
using AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// 实体查找�?- 将网�?ID 映射到实�?
    /// </summary>
    public sealed class BattleEntityLookup
    {
        private readonly Dictionary<int, IEntityId> _netIdToEntityId = new();

        public int Count => _netIdToEntityId.Count;

        /// <summary>
        /// 绑定网络 ID 到实�?
        /// </summary>
        public void Bind(BattleNetId netId, IEntity entity)
        {
            if (entity.World == null) throw new ArgumentException("Entity has no world", nameof(entity));
            _netIdToEntityId[netId.Value] = entity.Id;
        }

        /// <summary>
        /// 尝试解析网络 ID 到实�?
        /// </summary>
        public bool TryResolve(IECWorld world, BattleNetId netId, out IEntity entity)
        {
            entity = default;
            if (world == null) return false;
            if (!_netIdToEntityId.TryGetValue(netId.Value, out var id)) return false;
            if (!world.IsAlive(id)) return false;
            entity = world.Wrap(id);
            return true;
        }

        /// <summary>
        /// 解除绑定
        /// </summary>
        public bool Unbind(BattleNetId netId)
        {
            return _netIdToEntityId.Remove(netId.Value);
        }

        /// <summary>
        /// 通过实体 ID 解除绑定
        /// </summary>
        public bool UnbindByEntityId(IEntityId id)
        {
            foreach (var kv in _netIdToEntityId)
            {
                if (kv.Value.Equals(id))
                {
                    return _netIdToEntityId.Remove(kv.Key);
                }
            }
            return false;
        }

        /// <summary>
        /// 清除所有绑�?
        /// </summary>
        public void Clear()
        {
            _netIdToEntityId.Clear();
        }
    }
}
