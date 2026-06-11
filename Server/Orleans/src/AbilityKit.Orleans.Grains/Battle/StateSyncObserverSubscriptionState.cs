namespace AbilityKit.Orleans.Grains.Battle;

internal enum StateSyncObserverSubscriptionAction
{
    Subscribe,
    RefreshFullSnapshot,
    SwitchBattle
}

internal readonly record struct StateSyncObserverSubscriptionDecision(
    StateSyncObserverSubscriptionAction Action,
    string PreviousBattleKey);

internal sealed class StateSyncObserverSubscriptionState
{
    public bool IsSubscribed { get; private set; }

    public string CurrentBattleKey { get; private set; } = string.Empty;

    public StateSyncObserverSubscriptionDecision DecideSubscribe(string battleGrainKey)
    {
        if (!IsSubscribed)
        {
            return new StateSyncObserverSubscriptionDecision(StateSyncObserverSubscriptionAction.Subscribe, string.Empty);
        }

        if (string.Equals(CurrentBattleKey, battleGrainKey, StringComparison.Ordinal))
        {
            return new StateSyncObserverSubscriptionDecision(StateSyncObserverSubscriptionAction.RefreshFullSnapshot, CurrentBattleKey);
        }

        return new StateSyncObserverSubscriptionDecision(StateSyncObserverSubscriptionAction.SwitchBattle, CurrentBattleKey);
    }

    public void MarkSubscribed(string battleGrainKey)
    {
        IsSubscribed = true;
        CurrentBattleKey = battleGrainKey;
    }

    public void Clear()
    {
        IsSubscribed = false;
        CurrentBattleKey = string.Empty;
    }
}
