using System;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// 位置同步到 GameObject
    /// </summary>
    public static class ChangePosition_SyncGameObjectPos
    {
        public static void Run(long unitId, float x, float y)
        {
            Log.Info($"[HotfixView] SyncGameObjectPos: Unit {unitId} -> ({x}, {y})");
        }
    }
}
