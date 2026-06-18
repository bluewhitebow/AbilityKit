using AbilityKit.Demo.Moba.Services;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Smoke;

public sealed class MobaRuntimeLogTests
{
    [Fact]
    public void Message_factory_is_not_called_when_log_level_is_disabled()
    {
        var previousLevel = MobaRuntimeLog.MinimumLevel;
        var previousRuntimeTrace = MobaRuntimeLog.EnableRuntimeTraceLogs;

        try
        {
            MobaRuntimeLog.MinimumLevel = MobaRuntimeLogLevel.Warning;
            MobaRuntimeLog.EnableRuntimeTraceLogs = false;
            var called = false;

            MobaRuntimeLog.Info(
                MobaRuntimeLogModule.Diagnostics,
                MobaRuntimeLogPurpose.RuntimeTrace,
                nameof(MobaRuntimeLogTests),
                () =>
                {
                    called = true;
                    return "disabled runtime trace";
                });

            Assert.False(called);
        }
        finally
        {
            MobaRuntimeLog.MinimumLevel = previousLevel;
            MobaRuntimeLog.EnableRuntimeTraceLogs = previousRuntimeTrace;
        }
    }

    [Fact]
    public void Message_factory_is_called_when_log_level_is_enabled()
    {
        var previousLevel = MobaRuntimeLog.MinimumLevel;
        var previousConfigurationInfo = MobaRuntimeLog.EnableConfigurationInfoLogs;

        try
        {
            MobaRuntimeLog.MinimumLevel = MobaRuntimeLogLevel.Info;
            MobaRuntimeLog.EnableConfigurationInfoLogs = true;
            var called = false;

            MobaRuntimeLog.Info(
                MobaRuntimeLog.Context(MobaRuntimeLogModule.Config, MobaRuntimeLogPurpose.Configuration, nameof(MobaRuntimeLogTests)),
                () =>
                {
                    called = true;
                    return "enabled configuration log";
                });

            Assert.True(called);
        }
        finally
        {
            MobaRuntimeLog.MinimumLevel = previousLevel;
            MobaRuntimeLog.EnableConfigurationInfoLogs = previousConfigurationInfo;
        }
    }

    [Fact]
    public void Input_batch_warning_factory_is_not_called_when_runtime_warning_is_disabled()
    {
        var previousLevel = MobaRuntimeLog.MinimumLevel;

        try
        {
            MobaRuntimeLog.MinimumLevel = MobaRuntimeLogLevel.Error;
            var called = false;

            MobaInputDiagnostics.RecordBatchWarning(
                diagnostics: null,
                key: "input.batch.test",
                messageFactory: () =>
                {
                    called = true;
                    return "disabled input batch warning";
                },
                owner: nameof(MobaRuntimeLogTests));

            Assert.False(called);
        }
        finally
        {
            MobaRuntimeLog.MinimumLevel = previousLevel;
        }
    }

    [Fact]
    public void Warning_once_message_factory_is_suppressed_after_first_write()
    {
        var previousLevel = MobaRuntimeLog.MinimumLevel;

        try
        {
            MobaRuntimeLog.MinimumLevel = MobaRuntimeLogLevel.Warning;
            MobaRuntimeLog.ResetCounters();
            var calls = 0;

            MobaRuntimeLog.WarningOnce(
                "moba-runtime-log-tests-warning-once",
                MobaRuntimeLogModule.Diagnostics,
                MobaRuntimeLogPurpose.Validation,
                nameof(MobaRuntimeLogTests),
                () =>
                {
                    calls++;
                    return "first warning";
                });
            MobaRuntimeLog.WarningOnce(
                "moba-runtime-log-tests-warning-once",
                MobaRuntimeLogModule.Diagnostics,
                MobaRuntimeLogPurpose.Validation,
                nameof(MobaRuntimeLogTests),
                () =>
                {
                    calls++;
                    return "second warning";
                });

            Assert.Equal(1, calls);
        }
        finally
        {
            MobaRuntimeLog.ResetCounters();
            MobaRuntimeLog.MinimumLevel = previousLevel;
        }
    }
}
