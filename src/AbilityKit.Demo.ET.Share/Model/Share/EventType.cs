namespace ET
{
    /// <summary>
    /// 场景切换开始事件
    /// </summary>
    public struct SceneChangeStart
    {
    }

    /// <summary>
    /// 场景切换完成事件
    /// </summary>
    public struct SceneChangeFinish
    {
    }

    /// <summary>
    /// 客户端场景创建后事件
    /// </summary>
    public struct AfterCreateClientScene
    {
    }

    /// <summary>
    /// 当前场景创建后事件
    /// </summary>
    public struct AfterCreateCurrentScene
    {
    }

    /// <summary>
    /// 应用启动初始化完成事件
    /// </summary>
    public struct AppStartInitFinish
    {
    }

    /// <summary>
    /// 登录完成事件
    /// </summary>
    public struct LoginFinish
    {
        public long PlayerId;
        public string PlayerName;
    }

    /// <summary>
    /// 进入地图完成事件
    /// </summary>
    public struct EnterMapFinish
    {
    }
    
    /// <summary>
    /// 请求进入战斗事件
    /// </summary>
    public struct RequestEnterBattle
    {
        public long PlayerId;
        public string PlayerName;
    }
    
    /// <summary>
    /// 战斗场景创建完成事件
    /// </summary>
    public struct BattleSceneInitFinish
    {
        public long PlayerId;
        public string PlayerName;
    }
    
    /// <summary>
    /// 战斗开始事件
    /// </summary>
    public struct BattleStart
    {
        public long BattleId;
    }
    
    /// <summary>
    /// 战斗结束事件
    /// </summary>
    public struct BattleEnd
    {
        public long BattleId;
        public bool IsVictory;
    }
}
