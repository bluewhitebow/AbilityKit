using System;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// 视图层事件监听器
    /// 监听实体创建销毁等事件
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETViewEventListener: Entity, IAwake
    {
        public void Awake()
        {
        }

        /// <summary>
        /// 单位创建回调
        /// </summary>
        public void OnUnitCreate(long unitId, string name)
        {
            Log.Info($"[ETView] Unit created: {name} ({unitId})");
        }

        /// <summary>
        /// 单位销毁回调
        /// </summary>
        public void OnUnitDestroy(long unitId)
        {
            Log.Info($"[ETView] Unit destroyed: {unitId}");
        }

        /// <summary>
        /// 位置变化回调
        /// </summary>
        public void OnPositionChanged(long unitId, float x, float y)
        {
            Log.Info($"[ETView] Unit {unitId} position changed to ({x}, {y})");
        }

        /// <summary>
        /// 旋转变化回调
        /// </summary>
        public void OnRotationChanged(long unitId, float rotation)
        {
            Log.Info($"[ETView] Unit {unitId} rotation changed to {rotation}");
        }
    }
}
