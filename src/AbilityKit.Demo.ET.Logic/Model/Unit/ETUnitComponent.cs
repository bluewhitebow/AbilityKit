using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 单位管理器组件
    /// 管理所有 ETUnit 实例
    /// Key: ActorId（运行时自增 ID）
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETUnitComponent : Entity, IAwake, IDestroy
    {
        // Unit dictionary - keyed by ActorId
        internal readonly Dictionary<int, ETUnit> Units = new();

        public void Awake()
        {
        }

        public void Destroy()
        {
            foreach (var unit in Units.Values)
            {
                unit.Dispose();
            }
            Units.Clear();
        }
    }
}
