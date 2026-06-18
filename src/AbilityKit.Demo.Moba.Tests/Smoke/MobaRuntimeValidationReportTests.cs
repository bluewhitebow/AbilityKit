using AbilityKit.Demo.Moba.Services;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Smoke;

public sealed class MobaRuntimeValidationReportTests
{
    [Fact]
    public void Validation_report_exports_structured_dto_for_legacy_entries()
    {
        var report = new MobaRuntimeValidationReport();

        report.Warning("Config Validator", "skill.100.range", "range is negative.", "100");

        var dto = report.ToDto();

        Assert.Equal(0, dto.ErrorCount);
        Assert.Equal(1, dto.WarningCount);
        Assert.False(dto.ShouldBlockStartup);
        Assert.Single(dto.Entries);
        Assert.Equal(MobaRuntimeValidationSeverity.Warning, dto.Entries[0].Severity);
        Assert.Equal("Warning", dto.Entries[0].SeverityName);
        Assert.Equal(MobaRuntimeValidationCategory.General, dto.Entries[0].Category);
        Assert.Equal("General", dto.Entries[0].CategoryName);
        Assert.Equal("moba.validation.warning.config_validator.skill_100_range", dto.Entries[0].Code);
        Assert.Equal("Config Validator", dto.Entries[0].Source);
        Assert.Equal("skill.100.range", dto.Entries[0].Path);
        Assert.Equal("100", dto.Entries[0].BusinessId);
        Assert.Equal(0L, dto.Entries[0].BusinessNumericId);
    }

    [Fact]
    public void Validation_report_preserves_explicit_code_category_and_numeric_business_id()
    {
        var report = new MobaRuntimeValidationReport();

        report.Error(
            "SkillConfig",
            "skill.42.pipeline",
            "pipeline is missing.",
            businessId: "skill:42",
            blocksStartup: true,
            code: "moba.config.skill.pipeline_missing",
            category: MobaRuntimeValidationCategory.Config,
            businessNumericId: 42L);

        var entry = report.Entries[0];
        var dto = report.ToDto();
        var formatted = report.FormatEntry(entry);

        Assert.True(report.ShouldBlockStartup);
        Assert.Equal("moba.config.skill.pipeline_missing", entry.Code);
        Assert.Equal(MobaRuntimeValidationCategory.Config, entry.Category);
        Assert.Equal(42L, entry.BusinessNumericId);
        Assert.Equal("moba.config.skill.pipeline_missing", dto.Entries[0].Code);
        Assert.Equal(MobaRuntimeValidationCategory.Config, dto.Entries[0].Category);
        Assert.Equal(42L, dto.Entries[0].BusinessNumericId);
        Assert.Contains("code=moba.config.skill.pipeline_missing", formatted);
        Assert.Contains("category=Config", formatted);
        Assert.Contains("businessNumericId=42", formatted);
    }
}
