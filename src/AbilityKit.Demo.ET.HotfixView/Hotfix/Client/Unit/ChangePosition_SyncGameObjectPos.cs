using System;

namespace ET
{
    /// <summary>
    /// 位置同步到 GameObject
    /// </summary>
    public static class ChangePosition_SyncGameObjectPos
    {
        public static void Run(long unitId, float x, float y)
        {
            Console.WriteLine($"[HotfixView] SyncGameObjectPos: Unit {unitId} -> ({x}, {y})");
        }
    }
}
