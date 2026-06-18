using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Services.Snapshot;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Smoke;

public sealed class MobaSnapshotBufferConsumptionTests
{
    [Fact]
    public void Snapshot_buffer_copy_to_keeps_entries_for_repeated_consumers()
    {
        var emitter = new TestBufferedEmitter();
        emitter.AddEntry(1);
        emitter.AddEntry(2);
        var first = new List<int>();
        var second = new List<int>();

        var firstCount = emitter.CopyEntriesTo(first);
        var secondCount = emitter.CopyEntriesTo(second);

        Assert.Equal(2, firstCount);
        Assert.Equal(2, secondCount);
        Assert.Equal(new[] { 1, 2 }, first);
        Assert.Equal(new[] { 1, 2 }, second);
        Assert.Equal(2, emitter.BufferedCount);
    }

    [Fact]
    public void Snapshot_buffer_drain_to_transfers_entries_and_clears_owner_buffer()
    {
        var emitter = new TestBufferedEmitter();
        emitter.AddEntry(3);
        emitter.AddEntry(4);
        var drained = new List<int>();

        var count = emitter.DrainEntriesTo(drained);

        Assert.Equal(2, count);
        Assert.Equal(new[] { 3, 4 }, drained);
        Assert.Equal(0, emitter.BufferedCount);
        Assert.Equal(0, emitter.DrainEntriesTo(new List<int>()));
    }

    private sealed class TestBufferedEmitter : LogicWorldSnapshotBufferEmitterBase<TestBufferedEmitter, int>
    {
        public TestBufferedEmitter() : base(initialCapacity: 4, maxRetainedCapacity: 16)
        {
        }

        public int BufferedCount => Count;

        public void AddEntry(int entry)
        {
            Add(entry);
        }

        public int CopyEntriesTo(IList<int> destination)
        {
            return CopyTo(destination);
        }

        public int DrainEntriesTo(IList<int> destination)
        {
            return DrainTo(destination);
        }

        protected override WorldStateSnapshot CreateSnapshot(int[] entries)
        {
            return new WorldStateSnapshot(1, entries.Length == 0 ? System.Array.Empty<byte>() : new[] { (byte)entries.Length });
        }
    }
}
