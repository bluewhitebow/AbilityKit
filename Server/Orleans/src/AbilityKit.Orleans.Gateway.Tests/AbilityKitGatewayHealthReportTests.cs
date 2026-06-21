using System;
using System.Collections.Generic;
using Xunit;

namespace AbilityKit.Orleans.Gateway.HttpApi.Tests;

public sealed class AbilityKitGatewayHealthReportTests
{
    [Fact]
    public void Ready_should_include_deployment_diagnostics()
    {
        var deployment = new AbilityKitGatewayDeploymentDiagnostics(
            "gateway",
            "cn-shanghai",
            "server-01",
            "cluster-a",
            "node-a",
            "silo-a",
            new AbilityKitGatewayRuntimeDiagnostics("C:/workspace", "Development", true, "1.0.0"),
            new AbilityKitGatewayGameplayDiagnostics(2, new[] { "moba", "shooter" }, new[] { "default" }));

        var report = AbilityKitGatewayHealthReport.Ready(
            "127.0.0.1:3000",
            deployment,
            new[] { "session:ok", "room:ok" },
            new[] { "low-memory" },
            "ok");

        Assert.Equal("Ready", report.Status);
        Assert.Equal("127.0.0.1:3000", report.Service);
        Assert.Equal("ok", report.Message);
        Assert.Same(deployment, report.Deployment);
        Assert.Equal(new[] { "session:ok", "room:ok" }, report.Diagnostics);
        Assert.Equal(new[] { "low-memory" }, report.Warnings);
    }

    [Fact]
    public void Ready_should_default_diagnostics_and_warnings_to_empty()
    {
        var deployment = new AbilityKitGatewayDeploymentDiagnostics(
            "gateway",
            "cn-shanghai",
            "server-01",
            null,
            null,
            null,
            null,
            null);

        var report = AbilityKitGatewayHealthReport.Ready(
            "http://localhost:5000",
            deployment);

        Assert.NotNull(report);
        Assert.Equal("Ready", report.Status);
        Assert.Empty(report.Diagnostics);
        Assert.Empty(report.Warnings);
    }
}
