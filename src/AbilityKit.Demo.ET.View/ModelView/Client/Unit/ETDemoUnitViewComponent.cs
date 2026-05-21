using System;

namespace ET
{
    /// <summary>
    /// ET Demo Unit View Component
    /// 负责渲染实体
    /// </summary>
    public class ETDemoUnitViewComponent
    {
        public long UnitId { get; private set; }
        public string UnitName { get; private set; }
        public float X { get; set; }
        public float Y { get; set; }

        public ETDemoUnitViewComponent(long unitId, string unitName, float x, float y)
        {
            UnitId = unitId;
            UnitName = unitName;
            X = x;
            Y = y;
        }

        public void UpdatePosition(float x, float y)
        {
            X = x;
            Y = y;
            Console.WriteLine($"[ETDemoUnitView] Unit {UnitName} ({UnitId}) position updated to ({X}, {Y})");
        }

        public void OnDead()
        {
            Console.WriteLine($"[ETDemoUnitView] Unit {UnitName} ({UnitId}) is dead, hiding view");
        }
    }
}
