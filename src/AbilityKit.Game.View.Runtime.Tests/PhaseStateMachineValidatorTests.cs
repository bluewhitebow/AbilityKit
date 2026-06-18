using AbilityKit.Game.View.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests;

public sealed class PhaseStateMachineValidatorTests
{
    private enum TestState
    {
        Boot,
        Lobby,
        Battle
    }

    private enum TestEvent
    {
        BootCompleted,
        EnterBattle,
        ReturnLobby
    }

    [Fact]
    public void Validate_WithValidStateMachine_ReturnsValidResult()
    {
        var spec = new PhaseStateMachineSpec<TestState, TestEvent>("Root")
            .AddState(TestState.Boot)
            .AddState(TestState.Lobby)
            .AddState(TestState.Battle)
            .SetStartState(TestState.Boot)
            .AddTransition(TestEvent.BootCompleted, TestState.Boot, TestState.Lobby)
            .AddTransition(TestEvent.EnterBattle, TestState.Lobby, TestState.Battle, "battle_requested");

        var result = new PhaseStateMachineValidator<TestState, TestEvent>()
            .Validate(spec, new[] { "battle_requested" });

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithoutStartState_ReportsError()
    {
        var spec = new PhaseStateMachineSpec<TestState, TestEvent>("Root")
            .AddState(TestState.Boot);

        var result = new PhaseStateMachineValidator<TestState, TestEvent>().Validate(spec);

        Assert.False(result.IsValid);
        Assert.Contains("State machine 'Root' has no start state.", result.Errors);
    }

    [Fact]
    public void Validate_WithUnregisteredStartState_ReportsError()
    {
        var spec = new PhaseStateMachineSpec<TestState, TestEvent>("Root")
            .AddState(TestState.Lobby)
            .SetStartState(TestState.Boot);

        var result = new PhaseStateMachineValidator<TestState, TestEvent>().Validate(spec);

        Assert.False(result.IsValid);
        Assert.Contains("State machine 'Root' start state is not registered: Boot", result.Errors);
    }

    [Fact]
    public void Validate_WithDuplicatedState_ReportsError()
    {
        var spec = new PhaseStateMachineSpec<TestState, TestEvent>("Root")
            .AddState(TestState.Boot)
            .AddState(TestState.Boot)
            .SetStartState(TestState.Boot);

        var result = new PhaseStateMachineValidator<TestState, TestEvent>().Validate(spec);

        Assert.False(result.IsValid);
        Assert.Contains("State machine 'Root' has duplicated state: Boot", result.Errors);
    }

    [Fact]
    public void Validate_WithTransitionReferencingUnknownStates_ReportsErrors()
    {
        var spec = new PhaseStateMachineSpec<TestState, TestEvent>("Root")
            .AddState(TestState.Boot)
            .SetStartState(TestState.Boot)
            .AddTransition(TestEvent.EnterBattle, TestState.Lobby, TestState.Battle);

        var result = new PhaseStateMachineValidator<TestState, TestEvent>().Validate(spec);

        Assert.False(result.IsValid);
        Assert.Contains("State machine 'Root' transition 0 source state is not registered: Lobby", result.Errors);
        Assert.Contains("State machine 'Root' transition 0 target state is not registered: Battle", result.Errors);
    }

    [Fact]
    public void Validate_WithUnknownConditionId_ReportsError()
    {
        var spec = new PhaseStateMachineSpec<TestState, TestEvent>("Root")
            .AddState(TestState.Boot)
            .AddState(TestState.Lobby)
            .SetStartState(TestState.Boot)
            .AddTransition(TestEvent.BootCompleted, TestState.Boot, TestState.Lobby, "missing_condition");

        var result = new PhaseStateMachineValidator<TestState, TestEvent>()
            .Validate(spec, new[] { "known_condition" });

        Assert.False(result.IsValid);
        Assert.Contains("State machine 'Root' transition 0 references unknown condition id: missing_condition", result.Errors);
    }

    [Fact]
    public void StartState_WhenNotConfigured_Throws()
    {
        var spec = new PhaseStateMachineSpec<TestState, TestEvent>("Root");

        Assert.Throws<System.InvalidOperationException>(() => spec.StartState);
    }

    [Fact]
    public void Freeze_RejectsLaterMutation()
    {
        var spec = new PhaseStateMachineSpec<TestState, TestEvent>("Root")
            .AddState(TestState.Boot)
            .SetStartState(TestState.Boot)
            .AddTransition(TestEvent.BootCompleted, TestState.Boot, TestState.Lobby)
            .Freeze();

        Assert.True(spec.IsFrozen);
        Assert.Throws<System.InvalidOperationException>(() => spec.AddState(TestState.Lobby));
        Assert.Throws<System.InvalidOperationException>(() => spec.SetStartState(TestState.Lobby));
        Assert.Throws<System.InvalidOperationException>(() => spec.AddTransition(TestEvent.EnterBattle, TestState.Lobby, TestState.Battle));
    }
}
