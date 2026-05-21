using System.Collections.Generic;

namespace ET.AbilityKit.Demo.ET.Logic
{
    /// <summary>
    /// 单位管理器 System
    /// </summary>
    [EntitySystemOf(typeof(DemoUnitComponent))]
    [FriendOf(typeof(DemoUnitComponent))]
    [FriendOf(typeof(DemoUnit))]
    public static partial class DemoUnitComponentSystem
    {
        /// <summary>
        /// 单位字典
        /// </summary>
        private static readonly Dictionary<long, DemoUnit> Units = new();

        [EntitySystem]
        private static void Awake(this DemoUnitComponent self)
        {
            Log.Info($"[DemoUnitComponent] DemoUnitComponent awake");
            Units.Clear();
        }

        [EntitySystem]
        private static void Destroy(this DemoUnitComponent self)
        {
            foreach (var unit in Units.Values)
            {
                unit.Dispose();
            }
            Units.Clear();
        }

        /// <summary>
        /// 创建单位
        /// </summary>
        public static DemoUnit CreateUnit(this DemoUnitComponent self, string name, DemoUnitType unitType, float x, float y, float maxHp = 100f)
        {
            var unit = self.AddChild<DemoUnit>();
            unit.Name = name;
            unit.UnitType = unitType;
            unit.X = x;
            unit.Y = y;
            unit.MaxHp = maxHp;
            unit.Hp = maxHp;

            Units[unit.InstanceId] = unit;
            Log.Info($"[DemoUnitComponent] Unit created: {name} ({unit.InstanceId}) at ({x}, {y}), HP: {maxHp}");

            return unit;
        }

        /// <summary>
        /// 获取单位
        /// </summary>
        public static DemoUnit GetUnit(this DemoUnitComponent self, long unitId)
        {
            return Units.TryGetValue(unitId, out var unit) ? unit : null;
        }

        /// <summary>
        /// 获取所有单位
        /// </summary>
        public static IReadOnlyCollection<DemoUnit> GetAllUnits(this DemoUnitComponent self)
        {
            return Units.Values;
        }

        /// <summary>
        /// 移除单位
        /// </summary>
        public static void RemoveUnit(this DemoUnitComponent self, long unitId)
        {
            if (Units.Remove(unitId, out var unit))
            {
                unit.Dispose();
                Log.Info($"[DemoUnitComponent] Unit removed: {unit.Name}");
            }
        }

        /// <summary>
        /// 查找范围内的单位
        /// </summary>
        public static List<DemoUnit> FindUnitsInRange(this DemoUnitComponent self, float x, float y, float range)
        {
            var result = new List<DemoUnit>();
            foreach (var unit in Units.Values)
            {
                float dx = unit.X - x;
                float dy = unit.Y - y;
                float distance = (float)System.Math.Sqrt(dx * dx + dy * dy);
                if (distance <= range)
                {
                    result.Add(unit);
                }
            }
            return result;
        }
    }
}
