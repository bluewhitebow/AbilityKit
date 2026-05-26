using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETUnitComponent System
    /// 管理 ETUnit 生命周期
    ///
    /// 设计说明：
    /// - 作为状态同步客户端，只管理单位数据
    /// - 数据由快照更新，不自己做计算
    /// - 不包含任何游戏业务逻辑（伤害、Buff、移动等）
    /// - 使用 Component 实例字段存储单位字典（不使用静态字典）
    /// </summary>
    [EntitySystemOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETUnitComponent self)
        {
            // Instance field is initialized in Component constructor
        }

        [EntitySystem]
        private static void Destroy(this ETUnitComponent self)
        {
            // Units are disposed in Component.Destroy()
        }

        #region Basic CRUD

        /// <summary>
        /// 创建单位
        /// </summary>
        public static ETUnit CreateUnit(
            this ETUnitComponent self,
            int actorId,
            int entityCode,
            ActorKind kind,
            string name,
            float x = 0,
            float y = 0,
            float maxHp = 100f,
            float attack = 10f,
            float defense = 5f,
            float moveSpeed = 5f,
            bool isLocalPlayer = false)
        {
            var unit = self.AddChild<ETUnit>();
            unit.ActorId = actorId;
            unit.EntityCode = entityCode;
            unit.Kind = kind;
            unit.Name = name;
            unit.X = x;
            unit.Y = y;
            unit.MaxHp = maxHp;
            unit.Hp = maxHp;
            unit.Attack = attack;
            unit.Defense = defense;
            unit.MoveSpeed = moveSpeed;
            unit.IsLocalPlayer = isLocalPlayer;
            unit.RenderX = x;
            unit.RenderY = y;

            // 使用 ActorId 作为 key
            self.Units[actorId] = unit;

            Log.Info($"[ETUnit] Unit created: {name} (ActorId={actorId}, EntityCode={entityCode}) at ({x}, {y})");

            return unit;
        }

        /// <summary>
        /// 获取单位
        /// </summary>
        public static ETUnit? GetUnit(this ETUnitComponent self, int actorId)
        {
            return self.Units.TryGetValue(actorId, out var unit) ? unit : null;
        }

        /// <summary>
        /// 获取所有单位
        /// </summary>
        public static IEnumerable<ETUnit> GetAllUnits(this ETUnitComponent self)
        {
            return self.Units.Values;
        }

        /// <summary>
        /// 获取特定类型的单位
        /// </summary>
        public static IEnumerable<ETUnit> GetUnitsByKind(this ETUnitComponent self, ActorKind kind)
        {
            foreach (var unit in self.Units.Values)
            {
                if (unit.Kind == kind)
                    yield return unit;
            }
        }

        /// <summary>
        /// 获取本地玩家单位
        /// </summary>
        public static ETUnit? GetLocalPlayerUnit(this ETUnitComponent self)
        {
            foreach (var unit in self.Units.Values)
            {
                if (unit.IsLocalPlayer)
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// 获取第一个单位
        /// </summary>
        public static ETUnit? GetFirstUnit(this ETUnitComponent self)
        {
            foreach (var unit in self.Units.Values)
            {
                return unit;
            }
            return null;
        }

        /// <summary>
        /// 移除单位
        /// </summary>
        public static void RemoveUnit(this ETUnitComponent self, int actorId)
        {
            if (self.Units.TryGetValue(actorId, out var unit))
            {
                self.Units.Remove(actorId);
                unit.Dispose();
                Log.Info($"[ETUnit] Unit removed: ActorId={actorId}");
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// 获取单位数量
        /// </summary>
        public static int UnitCount(this ETUnitComponent self)
        {
            return self.Units.Count;
        }

        /// <summary>
        /// 获取存活单位数量
        /// </summary>
        public static int AliveUnitCount(this ETUnitComponent self)
        {
            int count = 0;
            foreach (var unit in self.Units.Values)
            {
                if (!unit.IsDead)
                    count++;
            }
            return count;
        }

        #endregion

        #region ❌ 已删除的业务逻辑

        // ❌ ExecuteDamage() - 伤害由 moba.core 计算，通过快照更新
        // ❌ FindUnitsInRange() - 范围查询由 moba.core 处理
        // ❌ Tick() - 移动和冷却由 moba.core 计算，通过快照更新
        // ❌ OnUnitDead() - 死亡由 moba.core 检测，通过快照更新

        #endregion
    }
}
