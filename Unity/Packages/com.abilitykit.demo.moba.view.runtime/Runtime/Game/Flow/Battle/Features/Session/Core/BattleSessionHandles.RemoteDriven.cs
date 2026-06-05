using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Core.Common;
using AbilityKit.Core.Common.Log;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;

namespace AbilityKit.Game.Flow
{
    internal sealed partial class BattleSessionHandles
    {
        internal sealed class RemoteDrivenHandles
        {
            internal IWorldManager Worlds;
            internal HostRuntime Runtime;
            internal IWorld World;

            internal IRemoteFrameSource<PlayerInputCommand[]> InputSource;
            internal IConsumableRemoteFrameSource<PlayerInputCommand[]> Consumable;
            internal IRemoteFrameSink<PlayerInputCommand[]> Sink;

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
            }
        }
    }
}
