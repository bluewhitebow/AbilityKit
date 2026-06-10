using System.Collections.Generic;
using AbilityKit.Ability.Host.Extensions.Client.FrameSync;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Networking;

public sealed class ClientPredictionReconciliationCoordinatorTests
{
    [Fact]
    public void ReconcileAfterAuthoritativeSnapshotTrimsConfirmedInputsAndReplaysPendingFrames()
    {
        var coordinator = new ClientPredictionReconciliationCoordinator<int>();
        coordinator.RecordLocalInput(0, new[] { 10 });
        coordinator.RecordLocalInput(1, new[] { 20 });
        coordinator.RecordLocalInput(2, new[] { 30 });

        var currentFrame = 1;
        var submitted = new List<int>();

        var result = coordinator.ReconcileAfterAuthoritativeSnapshot(
            replayTargetFrame: 3,
            predictedHashBeforeCorrection: 333u,
            authoritativeFrame: 1,
            authoritativeStateHash: 101u,
            importedStateHash: 101u,
            confirmedFrame: 1,
            getCurrentFrame: () => currentFrame,
            computeStateHash: () => (uint)(currentFrame * 100),
            submitInput: (frame, inputs) =>
            {
                submitted.Add(frame * 10 + inputs.Length);
                return inputs.Length;
            },
            stepFrame: () =>
            {
                currentFrame++;
                return true;
            });

        Assert.True(result.AuthoritativeHashMatched);
        Assert.Equal(3, result.PredictedFrameBeforeCorrection);
        Assert.Equal(333u, result.PredictedHashBeforeCorrection);
        Assert.Equal(1, result.AuthoritativeFrame);
        Assert.Equal(101u, result.AuthoritativeStateHash);
        Assert.Equal(101u, result.ImportedStateHash);
        Assert.Equal(2, result.ReplayTicks);
        Assert.Equal(3, result.FinalFrame);
        Assert.Equal(300u, result.FinalStateHash);
        Assert.Equal(3, result.PendingInputFramesBeforeCorrection);
        Assert.Equal(2, result.PendingInputFramesAfterTrim);
        Assert.Equal(2, result.PendingInputFramesAfterReplay);
        Assert.Equal(2, coordinator.PendingInputFrameCount);
        Assert.Equal(new[] { 11, 21 }, submitted);
    }
}
