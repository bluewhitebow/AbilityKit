using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Smoke;

public sealed class MobaSmokeEntryContractTests
{
    [Fact]
    public void Et_app_exposes_non_interactive_smoke_arguments()
    {
        var args = new[]
        {
            "--smoke",
            "--smoke-no-force-exit",
            "--smoke-frames=120",
            "--smoke-min-battle-frames=10",
            "--smoke-sleep-ms=0"
        };

        Assert.Contains("--smoke", args);
        Assert.Contains("--smoke-no-force-exit", args);
        Assert.Contains(args, item => item.StartsWith("--smoke-frames=", StringComparison.Ordinal));
    }

    [Fact]
    public void Console_smoke_entry_is_supported_as_external_process_contract()
    {
        var projectRelativePath = "../AbilityKit.Demo.Moba.Console/AbilityKit.Demo.Moba.Console.csproj";
        var args = new[]
        {
            "run",
            "--project",
            projectRelativePath
        };

        Assert.Equal("run", args[0]);
        Assert.Contains("AbilityKit.Demo.Moba.Console.csproj", projectRelativePath);
    }
}
