namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// 战斗启动配置提供者接口
    /// </summary>
    public interface IBattleStartConfigProvider
    {
        /// <summary>
        /// 获取战斗启动配置
        /// </summary>
        BattleStartConfig Config { get; }

        /// <summary>
        /// 构建战斗启动计划
        /// </summary>
        BattleStartPlan BuildPlan();
    }
}
