using System;
using System.Collections.Generic;
using AbilityKit.Game.View.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests;

public sealed class PhaseStateFeatureRegistryTests
{
    [Fact]
    public void Enter_InvokesRegisteredBinding()
    {
        var events = new List<string>();
        var ctx = new TestContext(4);
        var plan = new PhaseFeaturePlan<TestContext, IPhaseFeature<TestContext>>()
            .Add("feature", (in TestContext c) => new TestFeature("feature", c.Value, events));
        var binding = new PhaseStateFeatureBinding<TestContext, IPhaseFeature<TestContext>>(
            "state",
            feature => feature.OnAttach(in ctx),
            plan);
        var registry = new PhaseStateFeatureRegistry<string, TestContext, IPhaseFeature<TestContext>>()
            .Add("state", binding);

        var installed = registry.Enter("state", in ctx);

        Assert.Equal(1, installed);
        Assert.Equal(new[] { "attach:feature:4" }, events);
    }

    [Fact]
    public void Enter_WithMissingBinding_ReturnsZeroAndReportsFailure()
    {
        var failures = new List<string>();
        var registry = new PhaseStateFeatureRegistry<string, TestContext, IPhaseFeature<TestContext>>(failures.Add);
        var ctx = new TestContext(1);
 
        var installed = registry.Enter("missing", in ctx);
 
        Assert.Equal(0, installed);
        Assert.Equal(new[] { "Phase state binding not registered: missing" }, failures);
    }
 
    [Fact]
    public void Exit_InvokesRegisteredBinding()
    {
        var events = new List<string>();
        var ctx = new TestContext(4);
        var binding = new PhaseStateFeatureBinding<TestContext, IPhaseFeature<TestContext>>(
            "state",
            _ => { },
            onExit: (in TestContext c) => events.Add($"exit:{c.Value}"));
        var registry = new PhaseStateFeatureRegistry<string, TestContext, IPhaseFeature<TestContext>>()
            .Add("state", binding);
 
        var exited = registry.Exit("state", in ctx);
 
        Assert.True(exited);
        Assert.Equal(new[] { "exit:4" }, events);
    }
 
    [Fact]
    public void Exit_WithMissingBinding_ReturnsFalseAndReportsFailure()
    {
        var failures = new List<string>();
        var registry = new PhaseStateFeatureRegistry<string, TestContext, IPhaseFeature<TestContext>>(failures.Add);
        var ctx = new TestContext(1);
 
        var exited = registry.Exit("missing", in ctx);
 
        Assert.False(exited);
        Assert.Equal(new[] { "Phase state binding not registered: missing" }, failures);
    }

    [Fact]
    public void Set_ReplacesExistingBinding()
    {
        var events = new List<string>();
        var ctx = new TestContext(8);
        var firstPlan = new PhaseFeaturePlan<TestContext, IPhaseFeature<TestContext>>()
            .Add("first", (in TestContext c) => new TestFeature("first", c.Value, events));
        var secondPlan = new PhaseFeaturePlan<TestContext, IPhaseFeature<TestContext>>()
            .Add("second", (in TestContext c) => new TestFeature("second", c.Value, events));
        var registry = new PhaseStateFeatureRegistry<string, TestContext, IPhaseFeature<TestContext>>()
            .Set("state", new PhaseStateFeatureBinding<TestContext, IPhaseFeature<TestContext>>(
                "first",
                feature => feature.OnAttach(in ctx),
                firstPlan))
            .Set("state", new PhaseStateFeatureBinding<TestContext, IPhaseFeature<TestContext>>(
                "second",
                feature => feature.OnAttach(in ctx),
                secondPlan));

        var installed = registry.Enter("state", in ctx);

        Assert.Equal(1, installed);
        Assert.Equal(new[] { "attach:second:8" }, events);
    }

    [Fact]
    public void Add_WithDuplicateKey_Throws()
    {
        var binding = new PhaseStateFeatureBinding<TestContext, IPhaseFeature<TestContext>>("state", _ => { });
        var registry = new PhaseStateFeatureRegistry<string, TestContext, IPhaseFeature<TestContext>>()
            .Add("state", binding);

        Assert.Throws<ArgumentException>(() => registry.Add("state", binding));
    }

    private readonly record struct TestContext(int Value);

    private sealed class TestFeature : IPhaseFeature<TestContext>
    {
        private readonly string _id;
        private readonly int _createdWith;
        private readonly List<string> _events;

        public TestFeature(string id, int createdWith, List<string> events)
        {
            _id = id;
            _createdWith = createdWith;
            _events = events;
        }

        public void OnAttach(in TestContext ctx)
        {
            _events.Add($"attach:{_id}:{_createdWith}");
        }

        public void OnDetach(in TestContext ctx)
        {
            _events.Add($"detach:{_id}:{ctx.Value}");
        }

        public void Tick(in TestContext ctx, float deltaTime)
        {
            _events.Add($"tick:{_id}:{deltaTime:0.00}");
        }
    }
}
