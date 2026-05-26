using System;

namespace ET.AbilityKit.Demo.View
{
    /// <summary>
    /// 视图层单位实体
    /// 用于 ET 框架的实体标识
    ///
    /// Design:
    /// - 纯 ET.Entity 子类，不存储业务数据
    /// - Id 由 ET 框架自动生成，与 moba.core 的 ActorId 无关
    /// - 用于视图层实体管理和事件路由
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETUnitViewEntity : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 对应的 ActorId（moba.core 逻辑层 ID）
        /// 由 ActorSpawnEventHandler 设置
        /// </summary>
        public int ActorId { get; set; }

        public void Awake()
        {
        }

        public void Destroy()
        {
            ActorId = 0;
        }
    }
}
