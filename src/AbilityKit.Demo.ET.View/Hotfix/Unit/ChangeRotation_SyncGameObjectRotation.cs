using System;

namespace ET.AbilityKit.Demo.View
{
    /// <summary>
    /// 旋转同步到 GameObject
    /// </summary>
    public static class ChangeRotation_SyncGameObjectRotation
    {
        public static void Run(long unitId, float rotation)
        {
            Log.Info($"[HotfixView] SyncGameObjectRotation: Unit {unitId} -> {rotation}");
        }
    }
}
