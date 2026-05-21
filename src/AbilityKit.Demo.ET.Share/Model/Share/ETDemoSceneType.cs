namespace ET
{
    /// <summary>
    /// ET Demo 场景类型定义
    /// 扩展自 statesync 的 SceneType
    /// </summary>
    public static partial class SceneType
    {
        /// <summary>
        /// 登录场景
        /// </summary>
        public const int DemoLogin = PackageType.StateSync * 1000 + 100;
        
        /// <summary>
        /// 战斗场景
        /// </summary>
        public const int DemoBattle = PackageType.StateSync * 1000 + 101;
    }
}
