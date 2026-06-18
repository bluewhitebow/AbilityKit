namespace AbilityKit.Orleans.Contracts.Battle;

public static class BattleResultStatusCodes
{
    public const string RejectedNotInitialized = "RejectedNotInitialized";
    public const string RejectedWorldMismatch = "RejectedWorldMismatch";
    public const string RejectedNullInput = "RejectedNullInput";
    public const string RejectedByInputBuffer = "RejectedByInputBuffer";
    public const string RejectedNullRequest = "RejectedNullRequest";
    public const string RejectedNullPlayer = "RejectedNullPlayer";
}
