using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using Xunit;

namespace AbilityKit.World.DI.Tests;

public sealed class WorldTestInjectorTests
{
    [Fact]
    public void Builder_InjectsRequiredFieldsWithoutContainer()
    {
        var dep = new FieldDependency();
        var contract = new ContractDependency();

        var target = WorldTestInjector.For(new MultiFieldService())
            .With(dep)
            .With<IDependencyContract>(contract)
            .Build();

        Assert.Same(dep, target.Field);
        Assert.Same(contract, target.Contract);
    }

    [Fact]
    public void Inject_WithDictionary_AssignsMembers()
    {
        var dep = new FieldDependency();
        var deps = new Dictionary<Type, object>
        {
            [typeof(FieldDependency)] = dep,
            [typeof(IDependencyContract)] = new ContractDependency(),
        };

        var target = WorldTestInjector.Inject(new MultiFieldService(), deps);

        Assert.Same(dep, target.Field);
        Assert.NotNull(target.Contract);
    }

    [Fact]
    public void Inject_SkipsMissingOptionalDependency()
    {
        var target = WorldTestInjector.For(new MultiFieldService())
            .With(new FieldDependency())
            .With<IDependencyContract>(new ContractDependency())
            .Build();

        Assert.Null(target.Optional);
    }

    [Fact]
    public void Inject_ThrowsWhenRequiredDependencyMissing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            WorldTestInjector.For(new MultiFieldService())
                .With(new FieldDependency())
                .Build());

        Assert.Contains("Required world service injection failed", ex.Message);
    }

    private interface IDependencyContract
    {
    }

    private sealed class ContractDependency : IDependencyContract
    {
    }

    private sealed class FieldDependency
    {
    }

    private sealed class OptionalDependency
    {
    }

    private sealed class MultiFieldService
    {
        [WorldInject] private FieldDependency _field = null!;

        [WorldInject(typeof(IDependencyContract))]
        private IDependencyContract _contract = null!;

        [WorldInject(required: false)] private OptionalDependency? _optional;

        public FieldDependency Field => _field;
        public IDependencyContract Contract => _contract;
        public OptionalDependency? Optional => _optional;
    }
}
