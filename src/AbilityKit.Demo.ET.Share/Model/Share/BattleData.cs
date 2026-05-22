namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 战斗状态
    /// </summary>
    public enum BattleState
    {
        Idle,
        Loading,
        Ready,
        InProgress,
        Paused,
        Ended,
    }

    /// <summary>
    /// 单位类型
    /// </summary>
    public enum ActorKind
    {
        None,
        Character,
        Monster,
        NPC,
        Building,
    }
}
