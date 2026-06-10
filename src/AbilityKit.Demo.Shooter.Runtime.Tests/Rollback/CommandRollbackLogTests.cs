using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.FrameSync.Rollback;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Rollback;

public sealed class CommandRollbackLogTests
{
    [Fact]
    public void RollbackAfterRunsCommandsFromNewestToOldestAndRemovesRolledBackEntries()
    {
        var log = new CommandRollbackLog();
        var rolledBack = new List<string>();

        log.Record(new FrameIndex(1), () => rolledBack.Add("f1-a"));
        log.Record(new FrameIndex(2), () => rolledBack.Add("f2-a"));
        log.Record(new FrameIndex(2), () => rolledBack.Add("f2-b"));
        log.Record(new FrameIndex(3), () => rolledBack.Add("f3-a"));

        var count = log.RollbackAfter(new FrameIndex(1));

        Assert.Equal(3, count);
        Assert.Equal(new[] { "f3-a", "f2-b", "f2-a" }, rolledBack);
        Assert.Equal(1, log.Count);
    }

    [Fact]
    public void CommandRollbackStateProviderCanBeRegisteredWithRollbackCoordinator()
    {
        var log = new CommandRollbackLog();
        var rolledBack = new List<int>();
        log.Record(new FrameIndex(1), () => rolledBack.Add(1));
        log.Record(new FrameIndex(2), () => rolledBack.Add(2));

        var registry = new RollbackRegistry();
        registry.Register(new CommandRollbackStateProvider(log));
        var coordinator = new RollbackCoordinator(registry, new RollbackSnapshotRingBuffer(8));
        coordinator.CaptureAndStore(new FrameIndex(1));

        Assert.True(coordinator.TryRestore(new FrameIndex(1)));
        Assert.Equal(new[] { 2 }, rolledBack);
        Assert.Equal(1, log.Count);
    }
}
