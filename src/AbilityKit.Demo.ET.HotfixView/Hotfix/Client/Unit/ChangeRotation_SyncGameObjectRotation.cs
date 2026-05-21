using System;

namespace ET
{
    /// <summary>
    /// 旋转同步到 GameObject
    /// </summary>
    public static class ChangeRotation_SyncGameObjectRotation
    {
        public static void Run(long unitId, float rotation)
        {
            Console.WriteLine($"[HotfixView] SyncGameObjectRotation: Unit {unitId} -> {rotation}");
        }
    }
}
