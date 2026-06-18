using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Share.Config;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Smoke;

public sealed class MobaSkillPipelinePrewarmTests
{
    [Fact]
    public void Prewarm_all_builds_pipeline_cache_from_loaded_skill_configs()
    {
        var configs = CreateConfigDatabase();
        var library = new TableDrivenMobaSkillPipelineLibrary(configs, effects: null);

        var result = library.PrewarmAll();

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(2, result.WarmedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(2, library.CachedSkillCount);
        Assert.True(library.IsCached(1001));
        Assert.True(library.IsCached(1002));
    }

    [Fact]
    public void Prewarm_reports_missing_skill_without_polluting_cache()
    {
        var configs = CreateConfigDatabase();
        var library = new TableDrivenMobaSkillPipelineLibrary(configs, effects: null);

        var result = library.Prewarm(new[] { 1001, 9999 });

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(1, result.WarmedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, library.CachedSkillCount);
        Assert.True(library.IsCached(1001));
        Assert.False(library.IsCached(9999));
    }

    [Fact]
    public void Try_get_after_prewarm_uses_cached_definition_boundary()
    {
        var configs = CreateConfigDatabase();
        var library = new TableDrivenMobaSkillPipelineLibrary(configs, effects: null);

        var prewarm = library.Prewarm(new[] { 1001 });
        var found = library.TryGet(1001, out var preCastConfig, out var preCastPhases, out var castConfig, out var castPhases);

        Assert.True(prewarm.Succeeded);
        Assert.True(found);
        Assert.Null(preCastConfig);
        Assert.Null(preCastPhases);
        Assert.NotNull(castConfig);
        Assert.NotNull(castPhases);
        Assert.Single(castPhases);
        Assert.Equal(1, library.CachedSkillCount);
    }

    private static MobaConfigDatabase CreateConfigDatabase()
    {
        var configs = new MobaConfigDatabase();
        var result = configs.ReloadFromDtoArrays(
            new Dictionary<Type, Array>
            {
                [typeof(SkillDTO)] = new[]
                {
                    CreateSkill(1001, castFlowId: 2001),
                    CreateSkill(1002, castFlowId: 2002),
                },
                [typeof(SkillFlowDTO)] = new[]
                {
                    CreateDelayFlow(2001),
                    CreateDelayFlow(2002),
                },
            },
            strict: false);

        Assert.True(result.Succeeded, result.Error);
        return configs;
    }

    private static SkillDTO CreateSkill(int id, int castFlowId)
    {
        return new SkillDTO
        {
            Id = id,
            Name = "test_skill_" + id,
            CooldownMs = 1000,
            Range = 600,
            Tags = Array.Empty<int>(),
            CastFlowId = castFlowId,
        };
    }

    private static SkillFlowDTO CreateDelayFlow(int id)
    {
        return new SkillFlowDTO
        {
            Id = id,
            Name = "test_flow_" + id,
            Phases = new[]
            {
                new SkillPhaseDTO
                {
                    Type = (int)SkillPhaseType.Delay,
                    PhaseId = "delay_" + id,
                    Delay = new SkillDelayPhaseDTO { DelayMs = 1 },
                },
            },
        };
    }
}
