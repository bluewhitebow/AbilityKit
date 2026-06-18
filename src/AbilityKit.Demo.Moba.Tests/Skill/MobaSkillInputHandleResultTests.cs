using AbilityKit.Demo.Moba.Services;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Skill;

public sealed class MobaSkillInputHandleResultTests
{
    [Fact]
    public void Failed_creates_stable_input_failure_code()
    {
        var result = MobaSkillInputHandleResult.Failed("skill.input.invalidSlot", "Invalid skill slot.");

        Assert.False(result.Success);
        Assert.Equal("skill.input.invalidSlot", result.Code);
        Assert.Equal("Input", result.Failure.Source);
        Assert.Equal("Invalid skill slot.", result.Message);
    }

    [Fact]
    public void From_cast_preserves_structured_cast_failure_code()
    {
        var failure = new MobaSkillCastFailure("Preparation", null, "skill.cast.slotNotFound", "Skill not found in slot.");
        var cast = MobaSkillCastResult.Failed("Skill not found in slot.", in failure);

        var result = MobaSkillInputHandleResult.FromCast(in cast);

        Assert.False(result.Success);
        Assert.Equal("skill.cast.slotNotFound", result.Code);
        Assert.Equal("Preparation", result.Failure.Source);
        Assert.Equal("Skill not found in slot.", result.Message);
    }

    [Fact]
    public void From_cast_maps_success_to_input_success_message()
    {
        var cast = MobaSkillCastResult.From(true, null, default);

        var result = MobaSkillInputHandleResult.FromCast(in cast, "skill.input.cast.started");

        Assert.True(result.Success);
        Assert.Equal("skill.input.cast.started", result.Message);
        Assert.False(result.Failure.HasValue);
    }
}
