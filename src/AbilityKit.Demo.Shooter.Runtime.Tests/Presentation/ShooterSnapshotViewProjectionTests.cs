using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Presentation;

public sealed class ShooterSnapshotViewProjectionTests
{
    [Fact]
    public void FullSnapshotProjectsEntitiesAndSeparatedComponentsIntoStore()
    {
        var projection = new ShooterSnapshotViewProjection();
        var player = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var bullet = new ShooterViewEntityKey(ShooterViewEntityKind.Bullet, 100);
        var batch = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 12,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(player, ownerEntityId: 0, alive: true),
                new ShooterViewEntityChange(bullet, ownerEntityId: 1, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(player, 1f, 2f, 1f, 0f, 0.5f, 0.25f),
                new ShooterViewTransformComponentChange(bullet, 3f, 4f, 0f, 1f, 8f, 9f)
            },
            new[] { new ShooterViewHealthComponentChange(player, 80) },
            new[] { new ShooterViewScoreComponentChange(player, 7) },
            new[] { new ShooterViewProjectileLifetimeComponentChange(bullet, 19) },
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in batch);

        Assert.Equal(2, projection.Store.EntityCount);
        Assert.Equal(1, projection.Store.PlayerCount);
        Assert.Equal(1, projection.Store.BulletCount);
        Assert.True(projection.Store.TryGetTransform(player, out var playerTransform));
        Assert.Equal(1f, playerTransform.X);
        Assert.Equal(2f, playerTransform.Y);
        Assert.True(projection.Store.TryGetHealth(player, out var health));
        Assert.Equal(80, health.Hp);
        Assert.True(projection.Store.TryGetScore(player, out var score));
        Assert.Equal(7, score.Score);
        Assert.True(projection.Store.TryGetProjectileLifetime(bullet, out var lifetime));
        Assert.Equal(19, lifetime.RemainingFrames);
    }

    [Fact]
    public void DeltaSnapshotUpdatesComponentsWithoutRemovingMissingEntities()
    {
        var projection = new ShooterSnapshotViewProjection();
        var playerOne = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var playerTwo = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var full = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(playerOne, 0, alive: true),
                new ShooterViewEntityChange(playerTwo, 0, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(playerOne, 0f, 0f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(playerTwo, 10f, 0f, 1f, 0f, 0f, 0f)
            },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());
        var delta = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 2,
            sequence: 2ul,
            ShooterViewSnapshotKind.Delta,
            ShooterViewBatchSource.LocalPrediction,
            Array.Empty<ShooterViewEntityChange>(),
            Array.Empty<ShooterViewEntityKey>(),
            new[] { new ShooterViewTransformComponentChange(playerOne, 2f, 3f, 0f, 1f, 4f, 5f) },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in full);
        projection.Apply(in delta);

        Assert.Equal(2, projection.Store.PlayerCount);
        Assert.True(projection.Store.TryGetTransform(playerOne, out var playerOneTransform));
        Assert.Equal(2f, playerOneTransform.X);
        Assert.Equal(3f, playerOneTransform.Y);
        Assert.True(projection.Store.TryGetTransform(playerTwo, out var playerTwoTransform));
        Assert.Equal(10f, playerTwoTransform.X);
    }

    [Fact]
    public void PackedDeltaSnapshotDoesNotRemoveMissingEntities()
    {
        var projection = new ShooterSnapshotViewProjection();
        var mapper = new ShooterSnapshotViewModelMapper();
        var playerOne = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var playerTwo = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var full = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(playerOne, 0, alive: true),
                new ShooterViewEntityChange(playerTwo, 0, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(playerOne, 0f, 0f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(playerTwo, 10f, 0f, 1f, 0f, 0f, 0f)
            },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());
        var packedDelta = new ShooterPackedSnapshotPayload(
            ShooterPackedSnapshotCodec.CurrentVersion,
            worldId: 77ul,
            frame: 2,
            serverTick: 2L,
            snapshotFlags: ShooterPackedSnapshotFlags.Delta,
            stateHash: 0u,
            entityCount: 1,
            extensionPayload: Array.Empty<byte>(),
            componentChunks: new[]
            {
                new ShooterPackedComponentChunk(
                    ShooterPackedComponentKinds.Transform,
                    ShooterPackedEntityKinds.Player,
                    count: 1,
                    entityIds: new[] { 1 },
                    valueX: new[] { 2f },
                    valueY: new[] { 3f },
                    valueZ: new[] { 0f },
                    valueW: new[] { 1f },
                    intValues: Array.Empty<int>(),
                    flags: Array.Empty<byte>(),
                    ownerIds: Array.Empty<int>(),
                    aux: new[] { 40000, 50000 })
            });
        var gatewaySnapshot = new ShooterGatewaySnapshot(
            worldId: 77ul,
            frame: 2,
            timestamp: 2d,
            serverTicks: 2L,
            isFullSnapshot: false,
            actors: Array.Empty<ShooterGatewayActorSnapshot>(),
            payloadOpCode: ShooterOpCodes.Snapshot.PackedStateDelta,
            packedSnapshot: packedDelta);

        var delta = mapper.Map(in gatewaySnapshot);
        projection.Apply(in full);
        projection.Apply(in delta);

        Assert.Equal(ShooterViewSnapshotKind.Delta, delta.SnapshotKind);
        Assert.False(delta.ShouldReplaceMissingEntities);
        Assert.Equal(2, projection.Store.PlayerCount);
        Assert.True(projection.Store.TryGetTransform(playerOne, out var playerOneTransform));
        Assert.Equal(2f, playerOneTransform.X);
        Assert.Equal(3f, playerOneTransform.Y);
        Assert.True(projection.Store.TryGetTransform(playerTwo, out var playerTwoTransform));
        Assert.Equal(10f, playerTwoTransform.X);
    }

    [Fact]
    public void FullSnapshotRemovesEntitiesMissingFromAuthoritativeBatch()
    {
        var projection = new ShooterSnapshotViewProjection();
        var playerOne = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var playerTwo = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var first = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(playerOne, 0, alive: true),
                new ShooterViewEntityChange(playerTwo, 0, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(playerOne, 0f, 0f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(playerTwo, 10f, 0f, 1f, 0f, 0f, 0f)
            },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());
        var correction = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 2,
            sequence: 2ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[] { new ShooterViewEntityChange(playerOne, 0, alive: true) },
            Array.Empty<ShooterViewEntityKey>(),
            new[] { new ShooterViewTransformComponentChange(playerOne, 4f, 5f, 0f, 1f, 0f, 0f) },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in first);
        projection.Apply(in correction);

        Assert.True(projection.Store.ContainsEntity(playerOne));
        Assert.False(projection.Store.ContainsEntity(playerTwo));
        Assert.False(projection.Store.Transforms.ContainsKey(playerTwo));
        Assert.Equal(1, projection.Store.PlayerCount);
    }

    [Fact]
    public void FullBatchSynchronizesPresentationEntitySet()
    {
        var projection = new ShooterSnapshotViewProjection();
        var stalePlayer = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var deadPlayer = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var newPlayer = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 3);
        var staleBullet = new ShooterViewEntityKey(ShooterViewEntityKind.Bullet, 100);
        var newBullet = new ShooterViewEntityKey(ShooterViewEntityKind.Bullet, 101);
        var first = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.JoinOrReconnect,
            new[]
            {
                new ShooterViewEntityChange(stalePlayer, 0, alive: true),
                new ShooterViewEntityChange(deadPlayer, 0, alive: true),
                new ShooterViewEntityChange(staleBullet, 1, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(stalePlayer, 1f, 1f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(deadPlayer, 2f, 2f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(staleBullet, 3f, 3f, 0f, 1f, 6f, 7f)
            },
            new[] { new ShooterViewHealthComponentChange(deadPlayer, 25) },
            Array.Empty<ShooterViewScoreComponentChange>(),
            new[] { new ShooterViewProjectileLifetimeComponentChange(staleBullet, 8) },
            Array.Empty<ShooterEventSnapshot>());
        var batchSync = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 8,
            sequence: 2ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.JoinOrReconnect,
            new[]
            {
                new ShooterViewEntityChange(deadPlayer, 0, alive: false),
                new ShooterViewEntityChange(newPlayer, 0, alive: true),
                new ShooterViewEntityChange(newBullet, 3, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(deadPlayer, 20f, 20f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(newPlayer, 4f, 5f, 0f, 1f, 0.5f, 0.25f),
                new ShooterViewTransformComponentChange(newBullet, 6f, 7f, 0f, 1f, 8f, 9f)
            },
            new[]
            {
                new ShooterViewHealthComponentChange(deadPlayer, 0),
                new ShooterViewHealthComponentChange(newPlayer, 100)
            },
            new[] { new ShooterViewScoreComponentChange(newPlayer, 3) },
            new[] { new ShooterViewProjectileLifetimeComponentChange(newBullet, 12) },
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in first);
        var result = projection.Apply(in batchSync);
 
        Assert.Equal(2, result.AddedEntities);
        Assert.Equal(0, result.UpdatedEntities);
        Assert.Equal(3, result.RemovedEntities);
        Assert.Equal(3, result.MissingEntityRemovals);
        Assert.Equal(0, result.ExplicitEntityRemovals);
        Assert.Equal(0, result.DeadEntityRemovals);
        Assert.Equal(5, result.ComponentUpdates);
        Assert.Equal(2, result.FinalEntityCount);
        Assert.Equal(ShooterViewBatchSource.JoinOrReconnect, result.Source);
        Assert.False(projection.Store.ContainsEntity(stalePlayer));
        Assert.False(projection.Store.ContainsEntity(deadPlayer));
        Assert.False(projection.Store.Transforms.ContainsKey(stalePlayer));
        Assert.False(projection.Store.Transforms.ContainsKey(deadPlayer));
        Assert.False(projection.Store.Health.ContainsKey(deadPlayer));
        Assert.False(projection.Store.ContainsEntity(staleBullet));
        Assert.False(projection.Store.ProjectileLifetimes.ContainsKey(staleBullet));
        Assert.True(projection.Store.ContainsEntity(newPlayer));
        Assert.True(projection.Store.ContainsEntity(newBullet));
        Assert.True(projection.Store.TryGetTransform(newPlayer, out var newPlayerTransform));
        Assert.Equal(4f, newPlayerTransform.X);
        Assert.True(projection.Store.TryGetHealth(newPlayer, out var newPlayerHealth));
        Assert.Equal(100, newPlayerHealth.Hp);
        Assert.True(projection.Store.TryGetProjectileLifetime(newBullet, out var newBulletLifetime));
        Assert.Equal(12, newBulletLifetime.RemainingFrames);
        Assert.Equal(2, projection.Store.EntityCount);
        Assert.Equal(1, projection.Store.PlayerCount);
        Assert.Equal(1, projection.Store.BulletCount);
    }

    [Fact]
    public void LocalPredictionFullSnapshotDoesNotRemoveMissingEntities()
    {
        var projection = new ShooterSnapshotViewProjection();
        var playerOne = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var playerTwo = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var authoritative = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(playerOne, 0, alive: true),
                new ShooterViewEntityChange(playerTwo, 0, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            Array.Empty<ShooterViewTransformComponentChange>(),
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());
        var localPrediction = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 2,
            sequence: 2ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.LocalPrediction,
            new[] { new ShooterViewEntityChange(playerOne, 0, alive: true) },
            Array.Empty<ShooterViewEntityKey>(),
            Array.Empty<ShooterViewTransformComponentChange>(),
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in authoritative);
        projection.Apply(in localPrediction);

        Assert.True(projection.Store.ContainsEntity(playerOne));
        Assert.True(projection.Store.ContainsEntity(playerTwo));
        Assert.Equal(2, projection.Store.PlayerCount);
    }

    [Fact]
    public void ProjectedSinkPublishesStoreAfterApplyingBatch()
    {
        var recordingSink = new RecordingProjectedViewSink();
        var projectedSink = new ShooterProjectedSnapshotViewSink(recordingSink);
        var player = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var batch = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[] { new ShooterViewEntityChange(player, 0, alive: true) },
            Array.Empty<ShooterViewEntityKey>(),
            Array.Empty<ShooterViewTransformComponentChange>(),
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());

        projectedSink.ApplySnapshot(in batch);
 
        Assert.Equal(1, recordingSink.ApplyCount);
        Assert.Equal(1, recordingSink.LastPlayerCount);
        Assert.Equal(1, recordingSink.LastFrame);
        Assert.Equal(1, recordingSink.LastAddedEntities);
        Assert.Equal(1, projectedSink.LastApplyResult.AddedEntities);
    }

    private sealed class RecordingProjectedViewSink : IShooterProjectedViewSink
    {
        public int ApplyCount { get; private set; }

        public int LastPlayerCount { get; private set; }

        public int LastFrame { get; private set; }

        public int LastAddedEntities { get; private set; }
 
        public void ApplyViewState(
            ShooterViewEntityStore store,
            in ShooterSnapshotViewBatch sourceBatch,
            in ShooterViewProjectionApplyResult applyResult)
        {
            ApplyCount++;
            LastPlayerCount = store.PlayerCount;
            LastFrame = sourceBatch.Frame;
            LastAddedEntities = applyResult.AddedEntities;
        }

        public void Clear()
        {
            LastPlayerCount = 0;
            LastFrame = 0;
            LastAddedEntities = 0;
        }
    }
}
