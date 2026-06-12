using AbilityKit.Game.View.Presentation;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests;

public sealed class PresentationFacadeTests
{
    [Fact]
    public void ApplyAndPublish_ProjectsStoresAndPublishesBatch()
    {
        var projector = new TestProjector();
        var store = new ViewModelStore<TestBatch>();
        var stream = new ViewStream<TestBatch>();
        var facade = new TestFacade(projector, store, stream);
        TestBatch? published = null;
        stream.BatchApplied += batch => published = batch;
        var snapshot = new TestSnapshot(100, 12, 4);

        var applied = facade.Apply(snapshot);

        Assert.Equal(new TestBatch(100, 12, 4, 104), applied);
        Assert.Equal(applied, facade.Current);
        Assert.Equal(applied, published);
    }

    [Fact]
    public void Clear_ResetsStore()
    {
        var facade = new TestFacade(new TestProjector(), new ViewModelStore<TestBatch>(), new ViewStream<TestBatch>());
        facade.Apply(new TestSnapshot(1, 2, 3));

        facade.Clear();

        Assert.Equal(default, facade.Current);
    }

    private readonly record struct TestSnapshot(ulong WorldId, int Frame, int Value);

    private readonly record struct TestBatch(ulong WorldId, int Frame, ulong Sequence, int Value) : IViewBatch;

    private sealed class TestProjector : IViewModelProjector<TestSnapshot, TestBatch>
    {
        public TestBatch Project(in TestSnapshot snapshot)
        {
            return new TestBatch(snapshot.WorldId, snapshot.Frame, (ulong)snapshot.Value, snapshot.Value + 100);
        }
    }

    private sealed class TestFacade : PresentationFacade<TestSnapshot, TestBatch>
    {
        public TestFacade(
            IViewModelProjector<TestSnapshot, TestBatch> projector,
            IViewModelStore<TestBatch> store,
            IViewStream<TestBatch> stream)
            : base(projector, store, stream)
        {
        }

        public TestBatch Apply(in TestSnapshot snapshot)
        {
            return ApplyAndPublish(in snapshot);
        }
    }
}
