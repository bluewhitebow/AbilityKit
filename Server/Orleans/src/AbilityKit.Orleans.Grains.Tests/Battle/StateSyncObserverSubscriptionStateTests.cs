using AbilityKit.Orleans.Grains.Battle;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Battle;

public sealed class StateSyncObserverSubscriptionStateTests
{
    [Fact]
    public void DecideSubscribe_WhenNotSubscribed_ReturnsSubscribe()
    {
        var state = new StateSyncObserverSubscriptionState();

        var decision = state.DecideSubscribe("battle-a");

        Assert.Equal(StateSyncObserverSubscriptionAction.Subscribe, decision.Action);
        Assert.Equal(string.Empty, decision.PreviousBattleKey);
    }

    [Fact]
    public void DecideSubscribe_WhenSameBattle_ReturnsRefreshFullSnapshot()
    {
        var state = new StateSyncObserverSubscriptionState();
        state.MarkSubscribed("battle-a");

        var decision = state.DecideSubscribe("battle-a");

        Assert.Equal(StateSyncObserverSubscriptionAction.RefreshFullSnapshot, decision.Action);
        Assert.Equal("battle-a", decision.PreviousBattleKey);
        Assert.True(state.IsSubscribed);
        Assert.Equal("battle-a", state.CurrentBattleKey);
    }

    [Fact]
    public void DecideSubscribe_WhenDifferentBattle_ReturnsSwitchBattle()
    {
        var state = new StateSyncObserverSubscriptionState();
        state.MarkSubscribed("battle-a");

        var decision = state.DecideSubscribe("battle-b");

        Assert.Equal(StateSyncObserverSubscriptionAction.SwitchBattle, decision.Action);
        Assert.Equal("battle-a", decision.PreviousBattleKey);
    }

    [Fact]
    public void Clear_WhenSubscribed_ResetsSubscription()
    {
        var state = new StateSyncObserverSubscriptionState();
        state.MarkSubscribed("battle-a");

        state.Clear();

        Assert.False(state.IsSubscribed);
        Assert.Equal(string.Empty, state.CurrentBattleKey);
    }
}
