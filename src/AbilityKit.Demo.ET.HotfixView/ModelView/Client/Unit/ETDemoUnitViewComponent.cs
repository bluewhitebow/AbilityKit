using System;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// 视图层事件监听器 - 监听实体创建销毁等事件
    /// </summary>
    public class ETDemoViewEventListener
    {
        public void OnUnitCreate(long unitId, string name)
        {
            Log.Info($"[HotfixView] Unit created: {name} ({unitId})");
        }

        public void OnUnitDestroy(long unitId)
        {
            Log.Info($"[HotfixView] Unit destroyed: {unitId}");
        }

        public void OnPositionChanged(long unitId, float x, float y)
        {
            Log.Info($"[HotfixView] Unit {unitId} position changed to ({x}, {y})");
        }

        public void OnRotationChanged(long unitId, float rotation)
        {
            Log.Info($"[HotfixView] Unit {unitId} rotation changed to {rotation}");
        }
    }
}
