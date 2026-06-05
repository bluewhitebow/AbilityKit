using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Core.Common;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot;
using AbilityKit.Game.Flow.Battle.ViewEvents.Triggering;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;

namespace AbilityKit.Game.Flow
{
    internal sealed partial class BattleSessionHandles
    {
        internal sealed class ConfirmedHandles
        {
            internal IWorldManager Worlds;
            internal HostRuntime Runtime;
            internal IWorld World;

            internal IRemoteFrameSource<PlayerInputCommand[]> InputSource;
            internal IConsumableRemoteFrameSource<PlayerInputCommand[]> Consumable;
            internal IRemoteFrameSink<PlayerInputCommand[]> Sink;

            internal FrameSnapshotDispatcher Snapshots;

            internal BattleSessionFeature.DebugBattleViewEventSink ViewEventSink;

            internal BattleSnapshotViewAdapter SnapshotViewAdapter;
            internal BattleTriggerEventViewBridge TriggerBridge;

            internal BattleContext ViewCtx;
            internal FrameSnapshotDispatcher ViewSnapshots;
            internal SnapshotPipeline ViewPipeline;
            internal SnapshotCmdHandler ViewCmdHandler;
            internal ConfirmedBattleViewFeature ViewFeature;

            internal IDisposable ViewSubLobby;
            internal IDisposable ViewSubActorTransform;
            internal IDisposable ViewSubStateHash;
            internal IDisposable ViewSubActorSpawn;

            public void Reset()
            {
                Worlds = null;
                Runtime = null;
                World = null;

                IDisposable inputSourceDisposable = InputSource;
                InputSource = null;
                DisposeUtils.TryDispose(ref inputSourceDisposable, ex => Log.Exception(ex));
                Consumable = null;
                Sink = null;

                DisposeUtils.TryDispose(ref ViewCmdHandler, ex => Log.Exception(ex));
                DisposeUtils.TryDispose(ref ViewPipeline, ex => Log.Exception(ex));
                DisposeUtils.TryDispose(ref ViewSnapshots, ex => Log.Exception(ex));

                DisposeUtils.TryDispose(ref ViewSubLobby, ex => Log.Exception(ex));
                DisposeUtils.TryDispose(ref ViewSubActorTransform, ex => Log.Exception(ex));
                DisposeUtils.TryDispose(ref ViewSubStateHash, ex => Log.Exception(ex));
                DisposeUtils.TryDispose(ref ViewSubActorSpawn, ex => Log.Exception(ex));

                DisposeUtils.TryDispose(ref Snapshots, ex => Log.Exception(ex));
                DisposeUtils.TryDispose(ref SnapshotViewAdapter, ex => Log.Exception(ex));
                DisposeUtils.TryDispose(ref TriggerBridge, ex => Log.Exception(ex));

                ViewEventSink = null;

                ViewCtx = null;
                ViewFeature = null;
            }
        }
    }
}
