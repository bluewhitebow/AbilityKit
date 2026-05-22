using System;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// 实体创建后创建视图
    /// </summary>
    public static class AfterUnitCreate_CreateUnitView
    {
        public static void Run(long unitId, string unitName)
        {
            Log.Info($"[HotfixView] AfterUnitCreate_CreateUnitView: {unitName} ({unitId})");
        }
    }
}
