namespace AbilityKit.Demo.Moba.Triggering
{
    public enum MobaPresentationCueStage : byte
    {
        None = 0,
        ConditionPassed = 1,
        ConditionFailed = 2,
        BeforeAction = 3,
        Executed = 4,
        Interrupted = 5,
        Skipped = 6
    }
}
