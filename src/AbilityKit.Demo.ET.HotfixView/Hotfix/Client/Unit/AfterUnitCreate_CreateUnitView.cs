using System;

namespace ET
{
    /// <summary>
    /// 实体创建后创建视图
    /// </summary>
    public static class AfterUnitCreate_CreateUnitView
    {
        public static void Run(long unitId, string unitName)
        {
            Console.WriteLine($"[HotfixView] AfterUnitCreate_CreateUnitView: {unitName} ({unitId})");
        }
    }
}
