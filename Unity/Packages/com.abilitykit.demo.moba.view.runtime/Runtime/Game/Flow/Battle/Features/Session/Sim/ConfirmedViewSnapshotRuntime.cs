using System;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow
{
    internal sealed class ConfirmedViewSnapshotRuntime : IDisposable
    {
        private IDisposable _subActorTransform;
        private IDisposable _subStateHash;
        private IDisposable _subActorSpawn;

        public FrameSnapshotDispatcher Snapshots { get; private set; }
        public SnapshotPipeline Pipeline { get; private set; }
        public SnapshotCmdHandler CmdHandler { get; private set; }

        private ConfirmedViewSnapshotRuntime(
            FrameSnapshotDispatcher snapshots,
            SnapshotPipeline pipeline,
            SnapshotCmdHandler cmdHandler)
        {
            Snapshots = snapshots;
            Pipeline = pipeline;
            CmdHandler = cmdHandler;
        }

        public static ConfirmedViewSnapshotRuntime Create(BattleContext ctx)
        {
            if (ctx == null) return null;

            var snapshots = new FrameSnapshotDispatcher();
            var pipeline = new SnapshotPipeline(ctx, snapshots);
            var cmdHandler = new SnapshotCmdHandler(ctx, snapshots);

            AbilityKit.Game.Flow.Snapshot.BattleSnapshotRegistry.RegisterAll(
                snapshots,
                pipeline,
                pipeline,
                cmdHandler);

            AbilityKit.Game.Flow.Snapshot.SharedSnapshotRegistry.RegisterAll(
                snapshots,
                pipeline,
                pipeline,
                cmdHandler);

            ctx.FrameSnapshots = snapshots;
            ctx.SnapshotPipeline = pipeline;
            ctx.CmdHandler = cmdHandler;

            var runtime = new ConfirmedViewSnapshotRuntime(snapshots, pipeline, cmdHandler);
            runtime.Subscribe(ctx);
            return runtime;
        }

        public void Dispose()
        {
            _subActorTransform?.Dispose();
            _subActorTransform = null;

            _subStateHash?.Dispose();
            _subStateHash = null;

            _subActorSpawn?.Dispose();
            _subActorSpawn = null;

            CmdHandler?.Dispose();
            CmdHandler = null;

            Pipeline?.Dispose();
            Pipeline = null;

            Snapshots?.Dispose();
            Snapshots = null;
        }

        private void Subscribe(BattleContext ctx)
        {
            if (Snapshots == null || ctx == null) return;

            _subActorTransform = Snapshots.Subscribe<MobaActorTransformSnapshotEntry[]>(
                MobaOpCodes.Snapshot.ActorTransform,
                (packet, entries) => ConfirmedViewSnapshotApplier.ApplyTransform(ctx, entries));
            _subStateHash = Snapshots.Subscribe<MobaStateHashSnapshotPayload>(
                MobaOpCodes.Snapshot.StateHash,
                (packet, snap) => ConfirmedViewSnapshotApplier.ApplyStateHash(ctx, snap));
            _subActorSpawn = Snapshots.Subscribe<MobaActorSpawnSnapshotEntry[]>(
                MobaOpCodes.Snapshot.ActorSpawn,
                (packet, entries) => ConfirmedViewSnapshotApplier.ApplySpawn(ctx, entries));
        }
    }
}
