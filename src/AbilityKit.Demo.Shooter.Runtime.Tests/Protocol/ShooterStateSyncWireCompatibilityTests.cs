using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Protocol;

public sealed class ShooterStateSyncWireCompatibilityTests
{
    [Theory]
    [InlineData(true, ShooterOpCodes.Snapshot.PackedState)]
    [InlineData(false, ShooterOpCodes.Snapshot.PackedStateDelta)]
    public void PackedSnapshotWireRoundTripPreservesFullAndDeltaPayloads(bool isFullSnapshot, int expectedPayloadOpCode)
    {
        var runtime = CreateRuntime("packed-wire", 6100);
        var baseline = runtime.ExportPackedSnapshot(7001ul, isFullSnapshot: true, authorityOverride: true);
        if (!isFullSnapshot)
        {
            Assert.True(runtime.ImportPackedSnapshot(in baseline));
            runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 1f, 0f, 1f, false) });
            Assert.True(runtime.Tick(1f / 30f));
        }

        var packed = runtime.ExportPackedSnapshot(7001ul, isFullSnapshot, authorityOverride: true);
        var packedBytes = ShooterPackedSnapshotCodec.Serialize(in packed);
        var wire = new WireStateSyncSnapshotPush
        {
            WorldId = packed.WorldId,
            Frame = packed.Frame,
            Timestamp = 321.25d,
            ServerTicks = packed.ServerTick,
            Actors = null,
            IsFullSnapshot = isFullSnapshot,
            PayloadOpCode = expectedPayloadOpCode,
            Payload = packedBytes
        };

        var wireBytes = WireRoomGatewayBinary.Serialize(in wire);
        var restoredWire = WireRoomGatewayBinary.Deserialize<WireStateSyncSnapshotPush>(wireBytes);
        var restoredPacked = ShooterPackedSnapshotCodec.Deserialize(restoredWire.Payload!);

        Assert.Equal(packed.WorldId, restoredWire.WorldId);
        Assert.Equal(packed.Frame, restoredWire.Frame);
        Assert.Equal(isFullSnapshot, restoredWire.IsFullSnapshot);
        Assert.Equal(expectedPayloadOpCode, restoredWire.PayloadOpCode);
        Assert.Equal(packedBytes, restoredWire.Payload);
        Assert.Equal(ShooterPackedSnapshotCodec.CurrentVersion, restoredPacked.Version);
        Assert.Equal(packed.SnapshotFlags, restoredPacked.SnapshotFlags);
        Assert.Equal(packed.EntityCount, restoredPacked.EntityCount);
        Assert.Equal(packed.StateHash, restoredPacked.StateHash);
    }

    [Theory]
    [InlineData(ShooterPureStateSnapshotKinds.FullBaseline, true, ShooterOpCodes.Snapshot.PureState)]
    [InlineData(ShooterPureStateSnapshotKinds.Delta, false, ShooterOpCodes.Snapshot.PureStateDelta)]
    public void PureStateSnapshotWireRoundTripPreservesFullAndDeltaPayloads(int snapshotKind, bool isFullSnapshot, int expectedPayloadOpCode)
    {
        var snapshot = new ShooterPureStateSnapshotPayload(
            ShooterPureStateSyncCodec.CurrentVersion,
            8002ul,
            44,
            4400,
            snapshotKind,
            isFullSnapshot ? 0 : 40,
            isFullSnapshot ? 0u : 0x00AB_CDEFu,
            0x1020_3040u,
            ShooterPureStateSyncSettings.Default,
            new[]
            {
                new ShooterPureStateEntityDelta(
                    11,
                    ShooterPackedEntityKinds.Player,
                    ShooterPureStateEntityLayers.KeyInteraction,
                    isFullSnapshot ? ShooterPureStateDeltaKinds.Spawn : ShooterPureStateDeltaKinds.Update,
                    3,
                    1000,
                    2000,
                    30,
                    40,
                    0,
                    0,
                    75,
                    ShooterPureStateEntityFlags.Alive | ShooterPureStateEntityFlags.Visible)
            },
            new[]
            {
                new ShooterPureStateVisibilityHint(
                    11,
                    ShooterPackedEntityKinds.Player,
                    ShooterPureStateEntityLayers.KeyInteraction,
                    ShooterPureStateEntityFlags.Visible,
                    250)
            });
        var payload = ShooterPureStateSyncCodec.Serialize(in snapshot);
        var wire = new WireStateSyncSnapshotPush
        {
            WorldId = snapshot.WorldId,
            Frame = snapshot.Frame,
            Timestamp = 456.75d,
            ServerTicks = snapshot.ServerTick,
            Actors = null,
            IsFullSnapshot = isFullSnapshot,
            PayloadOpCode = expectedPayloadOpCode,
            Payload = payload
        };

        var wireBytes = WireRoomGatewayBinary.Serialize(in wire);
        var restoredWire = WireRoomGatewayBinary.Deserialize<WireStateSyncSnapshotPush>(wireBytes);
        var restoredSnapshot = ShooterPureStateSyncCodec.Deserialize(restoredWire.Payload!);

        Assert.Equal(snapshot.WorldId, restoredWire.WorldId);
        Assert.Equal(snapshot.Frame, restoredWire.Frame);
        Assert.Equal(isFullSnapshot, restoredWire.IsFullSnapshot);
        Assert.Equal(expectedPayloadOpCode, restoredWire.PayloadOpCode);
        Assert.Equal(payload, restoredWire.Payload);
        Assert.Equal(ShooterPureStateSyncCodec.CurrentVersion, restoredSnapshot.Version);
        Assert.Equal(snapshot.SnapshotKind, restoredSnapshot.SnapshotKind);
        Assert.Equal(snapshot.BaselineFrame, restoredSnapshot.BaselineFrame);
        Assert.Equal(snapshot.BaselineHash, restoredSnapshot.BaselineHash);
        Assert.Equal(snapshot.StateHash, restoredSnapshot.StateHash);
        Assert.Single(restoredSnapshot.Entities);
        Assert.Single(restoredSnapshot.VisibilityHints);
    }

    private static ShooterBattleRuntimePort CreateRuntime(string matchId, int seed)
    {
        var runtime = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            matchId,
            30,
            seed,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 3f, 0f)
            });

        Assert.True(runtime.StartGame(in start));
        runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, true) });
        Assert.True(runtime.Tick(1f / 30f));
        return runtime;
    }
}
