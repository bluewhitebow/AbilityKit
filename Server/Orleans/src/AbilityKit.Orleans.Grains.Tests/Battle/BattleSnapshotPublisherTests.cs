using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host.Extensions.Server.BattleHost;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Battle;

public sealed class BattleSnapshotPublisherTests
{
    [Fact]
    public void PublishTo_WhenObserverIsNull_ReturnsZeroWithoutCreatingSnapshot()
    {
        var factoryCalls = 0;
        var sent = new List<(string Observer, TestSnapshot Snapshot)>();
        var errors = new List<(string Observer, Exception Exception)>();
        var publisher = new BattleSnapshotPublisher<string, TestSnapshot>(
            (frame, isFullSnapshot) =>
            {
                factoryCalls++;
                return new TestSnapshot(frame, isFullSnapshot);
            },
            (observer, snapshot) => sent.Add((observer, snapshot)),
            (observer, exception) => errors.Add((observer, exception)));

        var result = publisher.PublishTo(null!, 12, isFullSnapshot: true);

        Assert.Equal(0, result);
        Assert.Equal(0, factoryCalls);
        Assert.Empty(sent);
        Assert.Empty(errors);
    }

    [Fact]
    public void PublishTo_WhenObserverIsValid_SendsOnlyTargetObserver()
    {
        var factoryCalls = 0;
        var sent = new List<(string Observer, TestSnapshot Snapshot)>();
        var errors = new List<(string Observer, Exception Exception)>();
        var publisher = new BattleSnapshotPublisher<string, TestSnapshot>(
            (frame, isFullSnapshot) =>
            {
                factoryCalls++;
                return new TestSnapshot(frame, isFullSnapshot);
            },
            (observer, snapshot) => sent.Add((observer, snapshot)),
            (observer, exception) => errors.Add((observer, exception)));

        var result = publisher.PublishTo("observer-b", 34, isFullSnapshot: true);

        Assert.Equal(1, result);
        Assert.Equal(1, factoryCalls);
        var sentItem = Assert.Single(sent);
        Assert.Equal("observer-b", sentItem.Observer);
        Assert.Equal(34, sentItem.Snapshot.Frame);
        Assert.True(sentItem.Snapshot.IsFullSnapshot);
        Assert.Empty(errors);
    }

    [Fact]
    public void PublishTo_WhenSenderThrows_ReportsErrorAndReturnsZero()
    {
        var expected = new InvalidOperationException("send failed");
        var sent = new List<string>();
        var errors = new List<(string Observer, Exception Exception)>();
        var publisher = new BattleSnapshotPublisher<string, TestSnapshot>(
            (frame, isFullSnapshot) => new TestSnapshot(frame, isFullSnapshot),
            (observer, snapshot) =>
            {
                sent.Add(observer);
                throw expected;
            },
            (observer, exception) => errors.Add((observer, exception)));

        var result = publisher.PublishTo("observer-b", 34, isFullSnapshot: true);

        Assert.Equal(0, result);
        Assert.Equal(new[] { "observer-b" }, sent);
        var error = Assert.Single(errors);
        Assert.Equal("observer-b", error.Observer);
        Assert.Same(expected, error.Exception);
    }

    private readonly record struct TestSnapshot(int Frame, bool IsFullSnapshot);
}
