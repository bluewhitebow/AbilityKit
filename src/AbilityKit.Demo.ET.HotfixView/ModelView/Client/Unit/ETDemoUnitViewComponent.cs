using System;

namespace ET
{
    /// <summary>
    /// 视图层事件监听器 - 监听实体创建销毁等事件
    /// </summary>
    public class ETDemoViewEventListener
    {
        public void OnUnitCreate(long unitId, string name)
        {
            Console.WriteLine($"[HotfixView] Unit created: {name} ({unitId})");
        }

        public void OnUnitDestroy(long unitId)
        {
            Console.WriteLine($"[HotfixView] Unit destroyed: {unitId}");
        }

        public void OnPositionChanged(long unitId, float x, float y)
        {
            Console.WriteLine($"[HotfixView] Unit {unitId} position changed to ({x}, {y})");
        }

        public void OnRotationChanged(long unitId, float rotation)
        {
            Console.WriteLine($"[HotfixView] Unit {unitId} rotation changed to {rotation}");
        }
    }
}
