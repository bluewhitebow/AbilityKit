extern alias Gateway;

using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using AbilityKit.Demo.Shooter;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.GameFramework.Network;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Grains.Battle;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using GameFramework.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using GatewayAbstractions = Gateway::AbilityKit.Orleans.Gateway.Abstractions;
using GatewayCore = Gateway::AbilityKit.Orleans.Gateway.Core;
using GatewayHandlers = Gateway::AbilityKit.Orleans.Gateway.Handlers;
using GatewayNetworking = Gateway::AbilityKit.Orleans.Gateway.Networking;

const int tcpGatewayPort = 41001;
const string tcpGatewayHost = "127.0.0.1";

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddSingleton<ServerMobaWorldManager>(sp =>
    new ServerMobaWorldManager(sp.GetRequiredService<ILogger<ServerMobaWorldManager>>()));
builder.Services.AddShooterSmokeGateway(tcpGatewayPort);

builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering(siloPort: 12111, gatewayPort: 31001);
    silo.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "abilitykit-shooter-smoke";
        options.ServiceId = "abilitykit-orleans-shooter-smoke";
    });
});

using var host = builder.Build();
await host.StartAsync();

using var transportCts = new CancellationTokenSource();
var transportServer = host.Services.GetRequiredService<GatewayNetworking.TcpTransportServer>();
var transportTask = Task.Run(() => transportServer.StartAsync(transportCts.Token));
await ShooterSmokeRunner.WaitForTcpAsync(tcpGatewayHost, tcpGatewayPort, TimeSpan.FromSeconds(5));

try
{
    var clusterClient = host.Services.GetRequiredService<IClusterClient>();
    var result = await ShooterSmokeRunner.RunAsync(clusterClient, tcpGatewayHost, tcpGatewayPort);
    Console.WriteLine($"Shooter TCP Gateway smoke passed. RoomId={result.RoomId}, BattleId={result.BattleId}, WorldId={result.WorldId}, TargetFrame={result.TargetFrame}, Inputs={result.InputCount}, LastInput={result.LastInputStatus}@{result.LastAcceptedFrame}/{result.LastCurrentFrame}, ServerTicks={result.LastServerTicks}, ShouldResync={result.ShouldResync}, Frame={result.Frame}, Players={result.ActorCount}, StateHash={result.StateHash}, Snapshot={result.SnapshotApplyResult}@{result.SnapshotFrame}, SnapshotServerTicks={result.SnapshotServerTicks}, SnapshotHash={result.SnapshotStateHash}, SnapshotEntities={result.SnapshotEntityCount}, StaleSnapshot={result.StaleSnapshotResult}, ProjectionApplies={result.ProjectionApplyCount}, ProjectionFullSyncs={result.ProjectionFullSyncApplyCount}, ProjectionAdded={result.ProjectionAddedEntities}, ProjectionRemoved={result.ProjectionRemovedEntities}, ProjectionEntities={result.ProjectionFinalEntityCount}, ProjectionPlayers={result.ProjectionFinalPlayerCount}, ProjectionBullets={result.ProjectionFinalBulletCount}, LateJoinEntry={result.LateJoinEntryKind}, LateJoinTargetFrame={result.LateJoinTargetFrame}, LateJoinProjectionFullSyncs={result.LateJoinProjectionFullSyncApplyCount}, LateJoinProjectionAdded={result.LateJoinProjectionAddedEntities}, LateJoinProjectionEntities={result.LateJoinProjectionFinalEntityCount}, LateJoinProjectionPlayers={result.LateJoinProjectionFinalPlayerCount}, LateJoinProjectionBullets={result.LateJoinProjectionFinalBulletCount}, ReconnectEntry={result.ReconnectEntryKind}, ReconnectTargetFrame={result.ReconnectTargetFrame}, ReconnectProjectionFullSyncs={result.ReconnectProjectionFullSyncApplyCount}, ReconnectProjectionAdded={result.ReconnectProjectionAddedEntities}, ReconnectProjectionEntities={result.ReconnectProjectionFinalEntityCount}, ReconnectProjectionPlayers={result.ReconnectProjectionFinalPlayerCount}, ReconnectProjectionBullets={result.ReconnectProjectionFinalBulletCount}");
}
finally
{
    transportCts.Cancel();
    await transportServer.StopAsync();
    await host.StopAsync();

    if (transportTask.IsFaulted && transportTask.Exception != null)
    {
        _ = transportTask.Exception;
    }
}

internal static class ShooterSmokeGatewayServiceCollectionExtensions
{
    public static IServiceCollection AddShooterSmokeGateway(this IServiceCollection services, int tcpGatewayPort)
    {
        services.Configure<GatewayCore.GatewayOptions>(options =>
        {
            options.RequestTimeoutMs = 30000;
            options.MaxFrameLength = 1024 * 1024;
        });

        services.Configure<GatewayNetworking.TcpTransportOptions>(options =>
        {
            options.Enabled = true;
            options.Host = "127.0.0.1";
            options.Port = tcpGatewayPort;
            options.RequestTimeoutMs = 30000;
            options.MaxFrameLength = 1024 * 1024;
        });

        services.AddSingleton<GatewayCore.GatewaySessionRegistry>();
        services.AddSingleton<GatewayAbstractions.IGatewaySessionRegistry>(sp => sp.GetRequiredService<GatewayCore.GatewaySessionRegistry>());

        services.AddSingleton<GatewayHandlers.GuestLoginHandler>();
        services.AddSingleton<GatewayHandlers.TimeSyncHandler>();
        services.AddSingleton<GatewayHandlers.CreateRoomHandler>();
        services.AddSingleton<GatewayHandlers.JoinRoomHandler>();
        services.AddSingleton<GatewayHandlers.RoomReadyHandler>();
        services.AddSingleton<GatewayHandlers.RoomPickHeroHandler>();
        services.AddSingleton<GatewayHandlers.StartRoomBattleHandler>();
        services.AddSingleton<GatewayHandlers.SubmitBattleInputHandler>();
        services.AddSingleton<GatewayHandlers.SubscribeStateSyncHandler>();
        services.AddSingleton<GatewayHandlers.RequestFullStateSyncHandler>();

        services.AddSingleton<GatewayCore.GatewayHandlerRegistry>(sp =>
        {
            var registry = new GatewayCore.GatewayHandlerRegistry(sp);
            registry.RegisterFromAssembly(typeof(GatewayCore.GatewayHandlerRegistry).Assembly);
            return registry;
        });
        services.AddSingleton<GatewayAbstractions.IGatewayHandlerRegistry>(sp => sp.GetRequiredService<GatewayCore.GatewayHandlerRegistry>());

        services.AddSingleton<GatewayCore.GatewayRequestRouter>();
        services.AddSingleton<GatewayAbstractions.IGatewayRequestRouter>(sp => sp.GetRequiredService<GatewayCore.GatewayRequestRouter>());

        services.AddSingleton<GatewayNetworking.IGatewayTransportEvents, GatewayCore.GatewayTransportHandler>();
        services.AddSingleton<GatewayCore.GatewayTransportHandler>();
        services.AddSingleton<GatewayNetworking.TcpTransportServer>();

        services.AddSingleton<GatewayCore.GatewayPushTargetGrain>();
        services.AddSingleton<AbilityKit.Orleans.Contracts.Battle.IGatewayPushTargetGrain>(sp => sp.GetRequiredService<GatewayCore.GatewayPushTargetGrain>());
        return services;
    }
}

internal static class ShooterSmokeRunner
{
    public static async Task<ShooterSmokeResult> RunAsync(IClusterClient clusterClient, string host, int port)
    {
        if (clusterClient == null) throw new ArgumentNullException(nameof(clusterClient));

        using var channel = new SmokeTcpGameFrameworkNetworkChannel("ShooterSmokeGateway");
        using var connection = GameFrameworkGatewayConnectionFactory.Wrap(channel);
        using var launcher = new ShooterClientNetworkLauncher(connection);

        connection.Open(host, port);
        connection.Tick(0f);

        var login = await LoginGuestAsync(connection);
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var projectedRecorder = new RecordingProjectedViewSink();
        var projectedSink = new ShooterProjectedSnapshotViewSink(projectedRecorder);
        var presentationSession = ShooterPresentationSessionContext.CreateFromFacade(presentation, projectedSink);
        var start = new ShooterStartGamePayload(
            "shooter-smoke-client",
            ShooterGameplay.DefaultTickRate,
            20260610,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 5f, 0f)
            });

        var pushWait = new TaskCompletionSource<ShooterSnapshotPushSmokeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        launcher.GatewayConnection.SnapshotPushDispatched += (opCode, payload, result) =>
        {
            try
            {
                if (TryCapturePackedSnapshotPush(opCode, payload, result, out var pushResult))
                {
                    pushWait.TrySetResult(pushResult);
                }
            }
            catch (Exception ex)
            {
                pushWait.TrySetException(ex);
            }
        };

        var launched = await launcher.CreateReadyStartAndSubscribeAsync(
            host,
            port,
            runtime,
            presentationSession,
            start,
            login.SessionToken,
            ShooterRoomLaunchSpec.CreateDefault("shooter-smoke-client"),
            playerId: 1u,
            timeout: TimeSpan.FromSeconds(10));

        ValidateLaunch(launched);

        var snapshotPush = await pushWait.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (snapshotPush.ApplyResult != ShooterSnapshotApplyResult.AppliedPackedSnapshot)
        {
            throw new InvalidOperationException($"Shooter packed snapshot push was not applied. Result={snapshotPush.ApplyResult}");
        }

        if (presentation.ViewModel.Frame <= 0 || runtime.CurrentFrame <= 0)
        {
            throw new InvalidOperationException("Shooter client runtime/presentation did not advance after snapshot push.");
        }

        if (snapshotPush.PackedFrame != runtime.CurrentFrame || snapshotPush.PackedFrame != presentation.ViewModel.Frame)
        {
            throw new InvalidOperationException($"Shooter packed snapshot frame mismatch. Packed={snapshotPush.PackedFrame}, Runtime={runtime.CurrentFrame}, Presentation={presentation.ViewModel.Frame}");
        }

        if (snapshotPush.PackedStateHash != runtime.ComputeStateHash())
        {
            throw new InvalidOperationException($"Shooter packed snapshot hash mismatch. Packed={snapshotPush.PackedStateHash}, Runtime={runtime.ComputeStateHash()}");
        }

        var inputResults = await SubmitSmokeInputsAsync(launched, TimeSpan.FromSeconds(10));

        var frameBeforeStaleSnapshot = presentation.ViewModel.Frame;
        var staleSnapshotResult = ApplyStaleSnapshotPush(launched.Session, launched.Flow.WorldId, frameBeforeStaleSnapshot);
        if (staleSnapshotResult != ShooterSnapshotApplyResult.IgnoredStaleSnapshot)
        {
            throw new InvalidOperationException($"Shooter stale snapshot was not ignored. Result={staleSnapshotResult}");
        }

        if (presentation.ViewModel.Frame != frameBeforeStaleSnapshot)
        {
            throw new InvalidOperationException("Shooter presentation frame changed after stale snapshot push.");
        }

        var playerCount = CountCurrentEntities(presentation.ViewModel.Current, ShooterViewEntityKind.Player);
        if (playerCount == 0)
        {
            throw new InvalidOperationException("Shooter presentation has no players after snapshot push.");
        }

        var projectionResult = ValidateProjectedPresentation(projectedRecorder, playerCount, "primary client");
        var lateJoin = await RunLateJoinProjectionSmokeAsync(
            host,
            port,
            launched.Flow.RoomId,
            start,
            playerCount,
            TimeSpan.FromSeconds(10));
        var reconnect = await RunReconnectProjectionSmokeAsync(
            host,
            port,
            launched.Flow.RoomId,
            start,
            login.AccountId,
            login.SessionToken,
            playerCount,
            TimeSpan.FromSeconds(10));

        var lastInput = inputResults[inputResults.Count - 1];
        var result = new ShooterSmokeResult(
            login.AccountId,
            launched.Flow.RoomId,
            launched.Flow.BattleId,
            launched.Flow.WorldId,
            launched.Flow.TargetFrame,
            inputResults.Count,
            lastInput.Local.RequestedFrame,
            lastInput.Remote.AcceptedFrame,
            lastInput.Remote.CurrentFrame,
            lastInput.Remote.Status,
            lastInput.Remote.ServerTicks,
            lastInput.Remote.ShouldResync,
            presentation.ViewModel.Frame,
            playerCount,
            runtime.ComputeStateHash(),
            snapshotPush.ApplyResult,
            snapshotPush.WireFrame,
            snapshotPush.WireServerTicks,
            snapshotPush.PayloadOpCode,
            snapshotPush.PackedFrame,
            snapshotPush.PackedServerTick,
            snapshotPush.PackedStateHash,
            snapshotPush.PackedEntityCount,
            staleSnapshotResult,
            projectedRecorder.ApplyCount,
            projectedRecorder.FullSyncApplyCount,
            projectionResult.AddedEntities,
            projectionResult.RemovedEntities,
            projectionResult.ComponentUpdates,
            projectionResult.FinalEntityCount,
            projectionResult.FinalPlayerCount,
            projectionResult.FinalBulletCount,
            lateJoin.AccountId,
            lateJoin.EntryKind,
            lateJoin.TargetFrame,
            lateJoin.ProjectionApplyCount,
            lateJoin.ProjectionFullSyncApplyCount,
            lateJoin.ProjectionAddedEntities,
            lateJoin.ProjectionRemovedEntities,
            lateJoin.ProjectionComponentUpdates,
            lateJoin.ProjectionFinalEntityCount,
            lateJoin.ProjectionFinalPlayerCount,
            lateJoin.ProjectionFinalBulletCount,
            reconnect.EntryKind,
            reconnect.TargetFrame,
            reconnect.ProjectionApplyCount,
            reconnect.ProjectionFullSyncApplyCount,
            reconnect.ProjectionAddedEntities,
            reconnect.ProjectionRemovedEntities,
            reconnect.ProjectionComponentUpdates,
            reconnect.ProjectionFinalEntityCount,
            reconnect.ProjectionFinalPlayerCount,
            reconnect.ProjectionFinalBulletCount);

        ValidateSmokeResult(result);

        await CleanupBattleAsync(clusterClient, result);
        return result;
    }

    public static async Task WaitForTcpAsync(string host, int port, TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        while (!timeoutCts.IsCancellationRequested)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port, timeoutCts.Token);
                return;
            }
            catch when (!timeoutCts.IsCancellationRequested)
            {
                await Task.Delay(50, timeoutCts.Token);
            }
        }

        throw new TimeoutException($"TCP Gateway did not listen on {host}:{port} in time.");
    }

    private static async Task<ShooterSmokeLogin> LoginGuestAsync(AbilityKit.Network.Abstractions.IConnection connection)
    {
        using var requestClient = new RequestClient(connection);
        var request = new WireRoomGuestLoginReq
        {
            GuestId = $"shooter-smoke-{Guid.NewGuid():N}"
        };
        var payload = WireRoomGatewayBinary.Serialize(in request);
        var responsePayload = await requestClient.SendRequestAsync(RoomGatewayOpCodes.GuestLogin, payload, TimeSpan.FromSeconds(10));
        var response = WireRoomGatewayBinary.Deserialize<WireRoomGuestLoginRes>(responsePayload);
        if (!response.Success || string.IsNullOrWhiteSpace(response.SessionToken) || string.IsNullOrWhiteSpace(response.AccountId))
        {
            throw new InvalidOperationException($"Shooter smoke guest login failed: {response.Message}");
        }

        return new ShooterSmokeLogin(response.AccountId, response.SessionToken);
    }

    private static async Task<List<ShooterClientGatewayInputSubmitResult>> SubmitSmokeInputsAsync(
        ShooterClientNetworkLaunchResult launched,
        TimeSpan timeout)
    {
        var results = new List<ShooterClientGatewayInputSubmitResult>(capacity: 3);
        var inputs = new[]
        {
            (MoveX: 1f, MoveY: 0f, AimX: 1f, AimY: 0f, Fire: true),
            (MoveX: 0.5f, MoveY: 0.25f, AimX: 1f, AimY: 0.1f, Fire: false),
            (MoveX: 0f, MoveY: 1f, AimX: 0.25f, AimY: 1f, Fire: true)
        };

        foreach (var input in inputs)
        {
            var submit = await launched.Battle.SubmitLocalInputToGatewayAsync(
                input.MoveX,
                input.MoveY,
                input.AimX,
                input.AimY,
                input.Fire,
                timeout: timeout);

            if (!submit.Remote.Success)
            {
                throw new InvalidOperationException($"Shooter gateway input was rejected. RequestedFrame={submit.Local.RequestedFrame}, Status={submit.Remote.Status}, Message={submit.Remote.Message}");
            }

            if (submit.Remote.AcceptedFrame < submit.Local.RequestedFrame)
            {
                throw new InvalidOperationException($"Shooter gateway accepted frame regressed. RequestedFrame={submit.Local.RequestedFrame}, AcceptedFrame={submit.Remote.AcceptedFrame}");
            }

            if (submit.Remote.CurrentFrame < 0)
            {
                throw new InvalidOperationException("Shooter gateway input response returned invalid current frame.");
            }

            if (string.IsNullOrWhiteSpace(submit.Remote.Status))
            {
                throw new InvalidOperationException("Shooter gateway input response returned empty status.");
            }

            if (submit.Remote.ServerTicks <= 0)
            {
                throw new InvalidOperationException("Shooter gateway input response returned invalid server ticks.");
            }

            results.Add(submit);
        }

        return results;
    }

    private static ShooterSnapshotApplyResult ApplyStaleSnapshotPush(ShooterClientSession session, ulong worldId, int lastAppliedFrame)
    {
        var staleFrame = Math.Max(0, lastAppliedFrame - 1);
        var packed = new ShooterPackedSnapshotPayload(
            ShooterPackedSnapshotCodec.CurrentVersion,
            worldId,
            staleFrame,
            DateTime.UtcNow.Ticks,
            ShooterPackedSnapshotFlags.Full | ShooterPackedSnapshotFlags.AuthorityOverride,
            0,
            0,
            Array.Empty<byte>(),
            Array.Empty<ShooterPackedComponentChunk>());
        var packedPayload = ShooterPackedSnapshotCodec.Serialize(in packed);
        var wire = new WireStateSyncSnapshotPush
        {
            WorldId = worldId,
            Frame = staleFrame,
            Timestamp = 0d,
            IsFullSnapshot = true,
            Actors = null,
            PayloadOpCode = ShooterOpCodes.Snapshot.PackedState,
            Payload = packedPayload,
            ServerTicks = packed.ServerTick
        };
        var payload = WireRoomGatewayBinary.Serialize(in wire);
        return session.ApplyGatewayPush(RoomGatewayOpCodes.SnapshotPushed, payload);
    }

    private static bool TryCapturePackedSnapshotPush(
        uint opCode,
        ArraySegment<byte> payload,
        ShooterSnapshotApplyResult applyResult,
        out ShooterSnapshotPushSmokeResult result)
    {
        result = default;
        if (applyResult != ShooterSnapshotApplyResult.AppliedPackedSnapshot)
        {
            return false;
        }

        if (opCode != RoomGatewayOpCodes.SnapshotPushed)
        {
            return false;
        }

        var wire = WireRoomGatewayBinary.Deserialize<WireStateSyncSnapshotPush>(payload);
        if (wire.ServerTicks <= 0)
        {
            throw new InvalidOperationException("Shooter packed snapshot push returned invalid server ticks.");
        }

        if (wire.PayloadOpCode != ShooterOpCodes.Snapshot.PackedState)
        {
            throw new InvalidOperationException($"Shooter snapshot push returned unexpected payload opCode. Actual={wire.PayloadOpCode}");
        }

        if (wire.Payload == null || wire.Payload.Length == 0)
        {
            throw new InvalidOperationException("Shooter packed snapshot push returned empty payload.");
        }

        var packed = ShooterPackedSnapshotCodec.Deserialize(wire.Payload);
        if (packed.WorldId != wire.WorldId)
        {
            throw new InvalidOperationException($"Shooter packed snapshot world id mismatch. Wire={wire.WorldId}, Packed={packed.WorldId}");
        }

        if (packed.Frame != wire.Frame)
        {
            throw new InvalidOperationException($"Shooter packed snapshot frame mismatch. Wire={wire.Frame}, Packed={packed.Frame}");
        }

        if (packed.ServerTick <= 0)
        {
            throw new InvalidOperationException("Shooter packed snapshot returned invalid packed server tick.");
        }

        result = new ShooterSnapshotPushSmokeResult(
            applyResult,
            wire.WorldId,
            wire.Frame,
            wire.ServerTicks,
            wire.PayloadOpCode,
            packed.WorldId,
            packed.Frame,
            packed.ServerTick,
            packed.StateHash,
            packed.EntityCount);
        return true;
    }

    private static int CountCurrentEntities(ShooterSnapshotViewBatch batch, ShooterViewEntityKind kind)
    {
        var count = 0;
        var changes = batch.EntityChanges;
        for (int i = 0; i < changes.Count; i++)
        {
            if (changes[i].Kind == kind && changes[i].Alive)
            {
                count++;
            }
        }

        return count;
    }

    private static ShooterViewProjectionApplyResult ValidateProjectedPresentation(
        RecordingProjectedViewSink recorder,
        int expectedPlayerCount,
        string label)
    {
        if (recorder.ApplyCount <= 0)
        {
            throw new InvalidOperationException($"Shooter {label} did not project any snapshot view batch.");
        }

        if (recorder.FullSyncApplyCount <= 0)
        {
            throw new InvalidOperationException($"Shooter {label} did not project a full snapshot view batch.");
        }

        var result = recorder.LastFullSyncApplyResult;
        if (result.FinalEntityCount <= 0)
        {
            throw new InvalidOperationException($"Shooter {label} projection has no entities after full sync.");
        }

        if (result.FinalPlayerCount != expectedPlayerCount)
        {
            throw new InvalidOperationException($"Shooter {label} projection player count mismatch. Expected={expectedPlayerCount}, Actual={result.FinalPlayerCount}");
        }

        return result;
    }

    private static async Task<ShooterLateJoinSmokeResult> RunLateJoinProjectionSmokeAsync(
        string host,
        int port,
        string roomId,
        ShooterStartGamePayload start,
        int expectedPlayerCount,
        TimeSpan timeout)
    {
        using var channel = new SmokeTcpGameFrameworkNetworkChannel("ShooterSmokeGatewayLateJoin");
        using var connection = GameFrameworkGatewayConnectionFactory.Wrap(channel);
        using var launcher = new ShooterClientNetworkLauncher(connection);

        connection.Open(host, port);
        connection.Tick(0f);

        var login = await LoginGuestAsync(connection);
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var projectedRecorder = new RecordingProjectedViewSink();
        var projectedSink = new ShooterProjectedSnapshotViewSink(projectedRecorder);
        var presentationSession = ShooterPresentationSessionContext.CreateFromFacade(presentation, projectedSink);

        var pushWait = new TaskCompletionSource<ShooterSnapshotApplyResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        launcher.GatewayConnection.SnapshotPushDispatched += (_, _, result) =>
        {
            if (result == ShooterSnapshotApplyResult.AppliedPackedSnapshot || result == ShooterSnapshotApplyResult.AppliedActorSnapshot)
            {
                pushWait.TrySetResult(result);
            }
        };

        var launched = await launcher.JoinReadyStartAndSubscribeAsync(
            host,
            port,
            runtime,
            presentationSession,
            start,
            login.SessionToken,
            roomId,
            ShooterRoomLaunchSpec.CreateDefault("shooter-smoke-late-join-client"),
            playerId: 2u,
            timeout: timeout);

        ValidateLaunch(launched);
        if (launched.Flow.EntryKind == ShooterRoomGatewayEntryKind.TeamLobby)
        {
            throw new InvalidOperationException("Shooter late join unexpectedly entered team lobby instead of a running battle.");
        }

        var applyResult = await pushWait.Task.WaitAsync(timeout);
        if (applyResult != ShooterSnapshotApplyResult.AppliedPackedSnapshot && applyResult != ShooterSnapshotApplyResult.AppliedActorSnapshot)
        {
            throw new InvalidOperationException($"Shooter late join snapshot push was not applied. Result={applyResult}");
        }

        if (presentation.ViewModel.Frame <= 0 || runtime.CurrentFrame <= 0)
        {
            throw new InvalidOperationException("Shooter late join runtime/presentation did not advance after snapshot push.");
        }

        var projectionResult = ValidateProjectedPresentation(projectedRecorder, expectedPlayerCount, "late join client");
        return new ShooterLateJoinSmokeResult(
            login.AccountId,
            launched.Flow.EntryKind,
            launched.Flow.TargetFrame,
            projectedRecorder.ApplyCount,
            projectedRecorder.FullSyncApplyCount,
            projectionResult.AddedEntities,
            projectionResult.RemovedEntities,
            projectionResult.ComponentUpdates,
            projectionResult.FinalEntityCount,
            projectionResult.FinalPlayerCount,
            projectionResult.FinalBulletCount);
    }

    private static async Task<ShooterReconnectSmokeResult> RunReconnectProjectionSmokeAsync(
        string host,
        int port,
        string roomId,
        ShooterStartGamePayload start,
        string accountId,
        string sessionToken,
        int expectedPlayerCount,
        TimeSpan timeout)
    {
        using var channel = new SmokeTcpGameFrameworkNetworkChannel("ShooterSmokeGatewayReconnect");
        using var connection = GameFrameworkGatewayConnectionFactory.Wrap(channel);
        using var launcher = new ShooterClientNetworkLauncher(connection);

        connection.Open(host, port);
        connection.Tick(0f);

        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var projectedRecorder = new RecordingProjectedViewSink();
        var projectedSink = new ShooterProjectedSnapshotViewSink(projectedRecorder);
        var presentationSession = ShooterPresentationSessionContext.CreateFromFacade(presentation, projectedSink);

        var pushWait = new TaskCompletionSource<ShooterSnapshotApplyResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        launcher.GatewayConnection.SnapshotPushDispatched += (_, _, result) =>
        {
            if (result == ShooterSnapshotApplyResult.AppliedPackedSnapshot || result == ShooterSnapshotApplyResult.AppliedActorSnapshot)
            {
                pushWait.TrySetResult(result);
            }
        };

        var launched = await launcher.JoinReadyStartAndSubscribeAsync(
            host,
            port,
            runtime,
            presentationSession,
            start,
            sessionToken,
            roomId,
            ShooterRoomLaunchSpec.CreateDefault("shooter-smoke-reconnect-client"),
            playerId: 1u,
            timeout: timeout);

        ValidateLaunch(launched);
        if (launched.Flow.EntryKind != ShooterRoomGatewayEntryKind.Reconnect)
        {
            throw new InvalidOperationException($"Shooter reconnect expected reconnect entry kind. Actual={launched.Flow.EntryKind}");
        }

        var applyResult = await pushWait.Task.WaitAsync(timeout);
        if (applyResult != ShooterSnapshotApplyResult.AppliedPackedSnapshot && applyResult != ShooterSnapshotApplyResult.AppliedActorSnapshot)
        {
            throw new InvalidOperationException($"Shooter reconnect snapshot push was not applied. Result={applyResult}");
        }

        if (presentation.ViewModel.Frame <= 0 || runtime.CurrentFrame <= 0)
        {
            throw new InvalidOperationException("Shooter reconnect runtime/presentation did not advance after snapshot push.");
        }

        var projectionResult = ValidateProjectedPresentation(projectedRecorder, expectedPlayerCount, "reconnect client");
        return new ShooterReconnectSmokeResult(
            accountId,
            launched.Flow.EntryKind,
            launched.Flow.TargetFrame,
            projectedRecorder.ApplyCount,
            projectedRecorder.FullSyncApplyCount,
            projectionResult.AddedEntities,
            projectionResult.RemovedEntities,
            projectionResult.ComponentUpdates,
            projectionResult.FinalEntityCount,
            projectionResult.FinalPlayerCount,
            projectionResult.FinalBulletCount);
    }

    private static void ValidateSmokeResult(ShooterSmokeResult result)
    {
        if (string.IsNullOrWhiteSpace(result.RoomId))
        {
            throw new InvalidOperationException("Shooter smoke result returned empty room id.");
        }

        if (string.IsNullOrWhiteSpace(result.BattleId))
        {
            throw new InvalidOperationException("Shooter smoke result returned empty battle id.");
        }

        if (result.WorldId == 0)
        {
            throw new InvalidOperationException("Shooter smoke result returned zero world id.");
        }

        if (result.InputCount < 3)
        {
            throw new InvalidOperationException($"Shooter smoke submitted too few inputs. Count={result.InputCount}");
        }

        if (result.LastAcceptedFrame < result.LastRequestedFrame)
        {
            throw new InvalidOperationException($"Shooter smoke accepted frame regressed. Requested={result.LastRequestedFrame}, Accepted={result.LastAcceptedFrame}");
        }

        if (result.LastCurrentFrame < 0)
        {
            throw new InvalidOperationException($"Shooter smoke returned invalid current frame. Current={result.LastCurrentFrame}");
        }

        if (string.IsNullOrWhiteSpace(result.LastInputStatus))
        {
            throw new InvalidOperationException("Shooter smoke returned empty input status.");
        }

        if (result.LastServerTicks <= 0)
        {
            throw new InvalidOperationException("Shooter smoke returned invalid input server ticks.");
        }

        if (result.Frame <= 0)
        {
            throw new InvalidOperationException($"Shooter smoke client frame did not advance. Frame={result.Frame}");
        }

        if (result.ActorCount <= 0)
        {
            throw new InvalidOperationException("Shooter smoke presentation returned no active player actors.");
        }

        if (result.StateHash == 0)
        {
            throw new InvalidOperationException("Shooter smoke runtime returned zero state hash.");
        }

        if (result.SnapshotApplyResult != ShooterSnapshotApplyResult.AppliedPackedSnapshot)
        {
            throw new InvalidOperationException($"Shooter smoke did not apply packed snapshot push. Result={result.SnapshotApplyResult}");
        }

        if (result.SnapshotPayloadOpCode != ShooterOpCodes.Snapshot.PackedState)
        {
            throw new InvalidOperationException($"Shooter smoke snapshot payload opCode mismatch. Actual={result.SnapshotPayloadOpCode}");
        }

        if (result.SnapshotServerTicks <= 0 || result.SnapshotPackedServerTick <= 0)
        {
            throw new InvalidOperationException($"Shooter smoke snapshot returned invalid server ticks. Wire={result.SnapshotServerTicks}, Packed={result.SnapshotPackedServerTick}");
        }

        if (result.SnapshotFrame != result.SnapshotPackedFrame)
        {
            throw new InvalidOperationException($"Shooter smoke snapshot wire/packed frame mismatch. Wire={result.SnapshotFrame}, Packed={result.SnapshotPackedFrame}");
        }

        if (result.Frame < result.SnapshotPackedFrame)
        {
            throw new InvalidOperationException($"Shooter smoke client frame regressed behind snapshot. Snapshot={result.SnapshotPackedFrame}, Client={result.Frame}");
        }

        if (result.SnapshotStateHash == 0)
        {
            throw new InvalidOperationException("Shooter smoke packed snapshot returned zero state hash.");
        }

        if (result.SnapshotEntityCount <= 0)
        {
            throw new InvalidOperationException("Shooter smoke packed snapshot returned no entities.");
        }

        if (result.StaleSnapshotResult != ShooterSnapshotApplyResult.IgnoredStaleSnapshot)
        {
            throw new InvalidOperationException($"Shooter smoke stale snapshot was not ignored. Result={result.StaleSnapshotResult}");
        }

        ValidateProjectionResult(
            result.ProjectionApplyCount,
            result.ProjectionFullSyncApplyCount,
            result.ProjectionFinalEntityCount,
            result.ProjectionFinalPlayerCount,
            result.ActorCount,
            "primary client");

        if (string.IsNullOrWhiteSpace(result.LateJoinAccountId))
        {
            throw new InvalidOperationException("Shooter smoke late join returned empty account id.");
        }

        if (result.LateJoinEntryKind == ShooterRoomGatewayEntryKind.TeamLobby)
        {
            throw new InvalidOperationException("Shooter smoke late join entered team lobby instead of running battle.");
        }

        ValidateProjectionResult(
            result.LateJoinProjectionApplyCount,
            result.LateJoinProjectionFullSyncApplyCount,
            result.LateJoinProjectionFinalEntityCount,
            result.LateJoinProjectionFinalPlayerCount,
            result.ActorCount,
            "late join client");

        if (result.ReconnectEntryKind != ShooterRoomGatewayEntryKind.Reconnect)
        {
            throw new InvalidOperationException($"Shooter smoke reconnect entry kind mismatch. Actual={result.ReconnectEntryKind}");
        }

        ValidateProjectionResult(
            result.ReconnectProjectionApplyCount,
            result.ReconnectProjectionFullSyncApplyCount,
            result.ReconnectProjectionFinalEntityCount,
            result.ReconnectProjectionFinalPlayerCount,
            result.ActorCount,
            "reconnect client");
    }

    private static void ValidateProjectionResult(
        int applyCount,
        int fullSyncApplyCount,
        int finalEntityCount,
        int finalPlayerCount,
        int expectedPlayerCount,
        string label)
    {
        if (applyCount <= 0)
        {
            throw new InvalidOperationException($"Shooter smoke {label} did not apply any projection batch.");
        }

        if (fullSyncApplyCount <= 0)
        {
            throw new InvalidOperationException($"Shooter smoke {label} did not apply a full projection batch.");
        }

        if (finalEntityCount <= 0)
        {
            throw new InvalidOperationException($"Shooter smoke {label} projection returned no entities.");
        }

        if (finalPlayerCount != expectedPlayerCount)
        {
            throw new InvalidOperationException($"Shooter smoke {label} projection player count mismatch. Expected={expectedPlayerCount}, Actual={finalPlayerCount}");
        }
    }

    private static async Task CleanupBattleAsync(IClusterClient clusterClient, ShooterSmokeResult result)
    {
        await UnsubscribeObserverAsync(clusterClient, result.AccountId, result.RoomId, result.BattleId);
        if (!string.IsNullOrWhiteSpace(result.LateJoinAccountId))
        {
            await UnsubscribeObserverAsync(clusterClient, result.LateJoinAccountId, result.RoomId, result.BattleId);
        }

        var battleGrain = clusterClient.GetGrain<IBattleLogicHostGrain>(result.BattleId);
        await battleGrain.DestroyAsync();
    }

    private static async Task UnsubscribeObserverAsync(
        IClusterClient clusterClient,
        string accountId,
        string roomId,
        string battleId)
    {
        var observerKey = $"{accountId}:{roomId}";
        var observerGrain = clusterClient.GetGrain<IStateSyncObserverGrain>(observerKey);
        await observerGrain.UnsubscribeAsync(battleId);
    }

    private static void ValidateLaunch(ShooterClientNetworkLaunchResult launched)
    {
        if (!launched.Flow.Started)
        {
            throw new InvalidOperationException("Shooter gateway flow did not start battle.");
        }

        if (!launched.Flow.Subscribed)
        {
            throw new InvalidOperationException("Shooter gateway flow did not subscribe state sync.");
        }

        if (string.IsNullOrWhiteSpace(launched.Flow.BattleId))
        {
            throw new InvalidOperationException("Shooter gateway flow returned empty battle id.");
        }

        if (launched.Flow.WorldId == 0)
        {
            throw new InvalidOperationException("Shooter gateway flow returned zero world id.");
        }

        if (!launched.Flow.WorldStartAnchor.IsValid)
        {
            throw new InvalidOperationException("Shooter gateway flow returned invalid world start anchor.");
        }

        if (launched.Flow.ServerNowTicks <= 0)
        {
            throw new InvalidOperationException("Shooter gateway flow returned invalid server ticks.");
        }
    }
}

internal sealed class RecordingProjectedViewSink : IShooterProjectedViewSink
{
    public int ApplyCount { get; private set; }

    public int FullSyncApplyCount { get; private set; }

    public ShooterViewProjectionApplyResult LastApplyResult { get; private set; } = ShooterViewProjectionApplyResult.Empty;

    public ShooterViewProjectionApplyResult LastFullSyncApplyResult { get; private set; } = ShooterViewProjectionApplyResult.Empty;

    public void ApplyViewState(
        ShooterViewEntityStore store,
        in ShooterSnapshotViewBatch sourceBatch,
        in ShooterViewProjectionApplyResult applyResult)
    {
        ApplyCount++;
        LastApplyResult = applyResult;
        if (sourceBatch.ShouldReplaceMissingEntities)
        {
            FullSyncApplyCount++;
            LastFullSyncApplyResult = applyResult;
        }
    }

    public void Clear()
    {
        ApplyCount = 0;
        FullSyncApplyCount = 0;
        LastApplyResult = ShooterViewProjectionApplyResult.Empty;
        LastFullSyncApplyResult = ShooterViewProjectionApplyResult.Empty;
    }
}

internal sealed class SmokeTcpGameFrameworkNetworkChannel : INetworkChannel, IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<int, IPacketHandler> _handlers = new();
    private TcpClient? _client;
    private NetworkStream? _stream;
    private EventHandler<Packet>? _defaultHandler;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;
    private int _sentPacketCount;
    private int _receivedPacketCount;

    public SmokeTcpGameFrameworkNetworkChannel(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "ShooterSmokeGateway" : name;
    }

    public string Name { get; }

    public Socket Socket => _client?.Client ?? throw new InvalidOperationException("Channel is not connected.");

    public bool Connected => _client?.Connected == true;

    public ServiceType ServiceType => ServiceType.Tcp;

    public global::GameFramework.Network.AddressFamily AddressFamily => global::GameFramework.Network.AddressFamily.IPv4;

    public int SendPacketCount => 0;

    public int SentPacketCount => Volatile.Read(ref _sentPacketCount);

    public int ReceivePacketCount => 0;

    public int ReceivedPacketCount => Volatile.Read(ref _receivedPacketCount);

    public bool ResetHeartBeatElapseSecondsWhenReceivePacket { get; set; }

    public int MissHeartBeatCount => 0;

    public float HeartBeatInterval { get; set; }

    public float HeartBeatElapseSeconds => 0f;

    public void RegisterHandler(IPacketHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        lock (_syncRoot)
        {
            _handlers[handler.Id] = handler;
        }
    }

    public void SetDefaultHandler(EventHandler<Packet> handler)
    {
        lock (_syncRoot)
        {
            _defaultHandler = handler;
        }
    }

    public void Connect(IPAddress ipAddress, int port)
    {
        Connect(ipAddress, port, new object());
    }

    public void Connect(IPAddress ipAddress, int port, object userData)
    {
        if (ipAddress == null) throw new ArgumentNullException(nameof(ipAddress));
        if (Connected) return;

        _client = new TcpClient(ipAddress.AddressFamily)
        {
            NoDelay = true
        };
        _client.Connect(ipAddress, port);
        _stream = _client.GetStream();
        _readCts = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadLoopAsync(_readCts.Token));
    }

    public void Close()
    {
        _readCts?.Cancel();
        _stream?.Dispose();
        _client?.Close();
        _stream = null;
        _client = null;
    }

    public void Send<T>(T packet) where T : Packet
    {
        if (packet is not AbilityKitGatewayPacket gatewayPacket)
        {
            throw new InvalidOperationException($"Unsupported packet type: {typeof(T).FullName}");
        }

        var stream = _stream ?? throw new InvalidOperationException("Channel is not connected.");
        var payload = gatewayPacket.Payload;
        var payloadSpan = payload.Array == null
            ? ReadOnlySpan<byte>.Empty
            : new ReadOnlySpan<byte>(payload.Array, payload.Offset, payload.Count);
        var frame = new byte[NetworkFrameCodec.GetFrameSize(payloadSpan.Length)];
        var header = new NetworkPacketHeader(gatewayPacket.Header.Flags, gatewayPacket.Header.OpCode, gatewayPacket.Header.Seq, (uint)payloadSpan.Length);
        NetworkFrameCodec.WriteFrame(frame, header, payloadSpan);
        stream.Write(frame, 0, frame.Length);
        stream.Flush();
        Interlocked.Increment(ref _sentPacketCount);
    }

    public void Dispose()
    {
        Close();
        _readCts?.Dispose();
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var lengthPrefix = new byte[4];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var stream = _stream;
                if (stream == null)
                {
                    return;
                }

                await ReadExactlyAsync(stream, lengthPrefix, cancellationToken);
                var frameLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(lengthPrefix));
                if (frameLength < NetworkPacketHeader.Size)
                {
                    throw new InvalidOperationException($"Invalid frame length: {frameLength}.");
                }

                var frame = new byte[4 + frameLength];
                Buffer.BlockCopy(lengthPrefix, 0, frame, 0, lengthPrefix.Length);
                await ReadExactlyAsync(stream, frame.AsMemory(4, frameLength), cancellationToken);
                if (!NetworkFrameCodec.TryParseFrame(frame, out var header, out var payloadSpan))
                {
                    throw new InvalidOperationException("Failed to parse gateway frame.");
                }

                var payload = payloadSpan.ToArray();
                var packet = new AbilityKitGatewayPacket(header, new ArraySegment<byte>(payload));
                Interlocked.Increment(ref _receivedPacketCount);
                Dispatch(packet);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (IOException)
        {
        }
    }

    private static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        await ReadExactlyAsync(stream, buffer.AsMemory(0, buffer.Length), cancellationToken);
    }

    private static async Task ReadExactlyAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.Slice(offset), cancellationToken);
            if (read <= 0)
            {
                throw new IOException("Remote gateway closed the connection.");
            }

            offset += read;
        }
    }

    private void Dispatch(AbilityKitGatewayPacket packet)
    {
        EventHandler<Packet>? defaultHandler;
        IPacketHandler? handler;
        lock (_syncRoot)
        {
            _handlers.TryGetValue(packet.Id, out handler);
            defaultHandler = _defaultHandler;
        }

        if (handler != null)
        {
            handler.Handle(this, packet);
            return;
        }

        defaultHandler?.Invoke(this, packet);
    }
}

internal readonly record struct ShooterSmokeLogin(string AccountId, string SessionToken);

internal readonly record struct ShooterLateJoinSmokeResult(
    string AccountId,
    ShooterRoomGatewayEntryKind EntryKind,
    int TargetFrame,
    int ProjectionApplyCount,
    int ProjectionFullSyncApplyCount,
    int ProjectionAddedEntities,
    int ProjectionRemovedEntities,
    int ProjectionComponentUpdates,
    int ProjectionFinalEntityCount,
    int ProjectionFinalPlayerCount,
    int ProjectionFinalBulletCount);

internal readonly record struct ShooterReconnectSmokeResult(
    string AccountId,
    ShooterRoomGatewayEntryKind EntryKind,
    int TargetFrame,
    int ProjectionApplyCount,
    int ProjectionFullSyncApplyCount,
    int ProjectionAddedEntities,
    int ProjectionRemovedEntities,
    int ProjectionComponentUpdates,
    int ProjectionFinalEntityCount,
    int ProjectionFinalPlayerCount,
    int ProjectionFinalBulletCount);

internal readonly record struct ShooterSnapshotPushSmokeResult(
    ShooterSnapshotApplyResult ApplyResult,
    ulong WireWorldId,
    int WireFrame,
    long WireServerTicks,
    int PayloadOpCode,
    ulong PackedWorldId,
    int PackedFrame,
    long PackedServerTick,
    uint PackedStateHash,
    int PackedEntityCount);

internal readonly record struct ShooterSmokeResult(
    string AccountId,
    string RoomId,
    string BattleId,
    ulong WorldId,
    int TargetFrame,
    int InputCount,
    int LastRequestedFrame,
    int LastAcceptedFrame,
    int LastCurrentFrame,
    string LastInputStatus,
    long LastServerTicks,
    bool ShouldResync,
    int Frame,
    int ActorCount,
    uint StateHash,
    ShooterSnapshotApplyResult SnapshotApplyResult,
    int SnapshotFrame,
    long SnapshotServerTicks,
    int SnapshotPayloadOpCode,
    int SnapshotPackedFrame,
    long SnapshotPackedServerTick,
    uint SnapshotStateHash,
    int SnapshotEntityCount,
    ShooterSnapshotApplyResult StaleSnapshotResult,
    int ProjectionApplyCount,
    int ProjectionFullSyncApplyCount,
    int ProjectionAddedEntities,
    int ProjectionRemovedEntities,
    int ProjectionComponentUpdates,
    int ProjectionFinalEntityCount,
    int ProjectionFinalPlayerCount,
    int ProjectionFinalBulletCount,
    string LateJoinAccountId,
    ShooterRoomGatewayEntryKind LateJoinEntryKind,
    int LateJoinTargetFrame,
    int LateJoinProjectionApplyCount,
    int LateJoinProjectionFullSyncApplyCount,
    int LateJoinProjectionAddedEntities,
    int LateJoinProjectionRemovedEntities,
    int LateJoinProjectionComponentUpdates,
    int LateJoinProjectionFinalEntityCount,
    int LateJoinProjectionFinalPlayerCount,
    int LateJoinProjectionFinalBulletCount,
    ShooterRoomGatewayEntryKind ReconnectEntryKind,
    int ReconnectTargetFrame,
    int ReconnectProjectionApplyCount,
    int ReconnectProjectionFullSyncApplyCount,
    int ReconnectProjectionAddedEntities,
    int ReconnectProjectionRemovedEntities,
    int ReconnectProjectionComponentUpdates,
    int ReconnectProjectionFinalEntityCount,
    int ReconnectProjectionFinalPlayerCount,
    int ReconnectProjectionFinalBulletCount);
