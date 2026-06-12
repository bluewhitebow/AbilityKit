using System.Collections.Generic;
using AbilityKit.Game.View.Modules;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests;

public sealed class ModuleHostTests
{
    [Fact]
    public void TrySortByDependencies_OrdersDependenciesBeforeDependents()
    {
        var events = new List<string>();
        var input = new TestModule("input", events);
        var view = new TestModule("view", events, "session");
        var session = new TestModule("session", events, "input");
        var host = new ModuleHost<TestContext, TestModule>(new List<TestModule> { view, session, input });

        var sorted = host.TrySortByDependencies();

        Assert.True(sorted);
        Assert.Collection(
            host.Modules,
            m => Assert.Equal("input", m.Id),
            m => Assert.Equal("session", m.Id),
            m => Assert.Equal("view", m.Id));
    }

    [Fact]
    public void AttachTickRebindAndDetach_UseStableOrderAndReverseDetach()
    {
        var events = new List<string>();
        var first = new TestModule("first", events);
        var second = new TestModule("second", events);
        var host = new ModuleHost<TestContext, TestModule>(new List<TestModule> { first, second });
        var ctx = new TestContext(7);

        host.Attach(in ctx);
        host.Tick(in ctx, 0.25f);
        host.RebindAll(in ctx);
        host.Detach(in ctx);

        Assert.Equal(
            new[]
            {
                "attach:first:7",
                "attach:second:7",
                "tick:first:0.25",
                "tick:second:0.25",
                "rebind:first",
                "rebind:second",
                "detach:second:7",
                "detach:first:7"
            },
            events);
    }

    [Fact]
    public void TrySortByDependencies_ReturnsFalseForMissingDependency()
    {
        var failures = new List<string>();
        var events = new List<string>();
        var host = new ModuleHost<TestContext, TestModule>(
            new List<TestModule> { new TestModule("view", events, "missing") },
            failures.Add);

        var sorted = host.TrySortByDependencies();

        Assert.False(sorted);
        Assert.Contains(failures, message => message.Contains("missing module 'missing'"));
    }

    private readonly record struct TestContext(int Value);

    private sealed class TestModule : IGameModule<TestContext>, IGameModuleTick<TestContext>, IGameModuleRebind<TestContext>, IGameModuleId, IGameModuleDependencies
    {
        private readonly List<string> _events;
        private readonly string[] _dependencies;

        public TestModule(string id, List<string> events, params string[] dependencies)
        {
            Id = id;
            _events = events;
            _dependencies = dependencies;
        }

        public string Id { get; }
        public IEnumerable<string> Dependencies => _dependencies;

        public void OnAttach(in TestContext ctx)
        {
            _events.Add($"attach:{Id}:{ctx.Value}");
        }

        public void OnDetach(in TestContext ctx)
        {
            _events.Add($"detach:{Id}:{ctx.Value}");
        }

        public void Tick(in TestContext ctx, float deltaTime)
        {
            _events.Add($"tick:{Id}:{deltaTime:0.00}");
        }

        public void RebindAll(in TestContext ctx)
        {
            _events.Add($"rebind:{Id}");
        }
    }
}
