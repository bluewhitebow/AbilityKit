using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.FrameSync;
using AbilityKit.Ability.Host.Extensions.WorldStart;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.EntitasAdapters;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Battle.Moba.Config;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private WorldId CreateConfirmedAuthorityWorldId()
        {
            return new WorldId((_plan.WorldId ?? "room_1") + "__confirmed");
        }

        private void CreateConfirmedAuthorityRuntimeAndWorld(out WorldId authWorldId)
        {
            var serverOptions = CreateConfirmedAuthorityRuntime();
            InstallConfirmedAuthorityRuntimeModules(serverOptions, GetFixedDeltaSeconds());
            authWorldId = CreateConfirmedAuthorityWorldId();
            _confirmedWorld = CreateConfirmedAuthorityWorld(authWorldId);
        }

        private AbilityKit.Ability.Host.Framework.HostRuntimeOptions CreateConfirmedAuthorityRuntime()
        {
            _confirmedWorlds = SessionMobaWorldBootstrapFactory.CreateWorldManager();

            var serverOptions = new AbilityKit.Ability.Host.Framework.HostRuntimeOptions();
            _confirmedRuntime = new AbilityKit.Ability.Host.Framework.HostRuntime(_confirmedWorlds, serverOptions);
            return serverOptions;
        }

        private void InstallConfirmedAuthorityRuntimeModules(
            AbilityKit.Ability.Host.Framework.HostRuntimeOptions serverOptions,
            float fixedDelta)
        {
            var modules = CreateConfirmedAuthorityRuntimeModules(fixedDelta);
            modules.InstallAll(_confirmedRuntime, serverOptions);
        }

        private AbilityKit.Ability.Host.Framework.HostRuntimeModuleHost CreateConfirmedAuthorityRuntimeModules(float fixedDelta)
        {
            return new AbilityKit.Ability.Host.Framework.HostRuntimeModuleHost()
                .Add(new AbilityKit.Ability.Host.Extensions.FrameSync.ClientPredictionDriverModule(
                    resolveRemoteInputs: _ => _confirmedConsumable,
                    resolveLocalInputs: _ => null,
                    resolveIdealFrameLimit: _ => ResolveIdealFrameLimit(_),
                    inputDelayFrames: 0,
                    maxPredictionAheadFrames: 0,
                    minPredictionWindow: 0,
                    backlogEwmaAlpha: 0.20f,
                    enableRollback: false,
                    rollbackHistoryFrames: 0,
                    rollbackCaptureEveryNFrames: 0,
                    buildRollbackRegistry: _ => new AbilityKit.Ability.FrameSync.Rollback.RollbackRegistry(),
                    buildComputeHash: _ => null))
                .Add(new AbilityKit.Ability.Host.Extensions.Time.ServerFrameTimeModule(fixedDelta))
                .Add(new WorldAutoStartModule());
        }

        private AbilityKit.Ability.World.Abstractions.IWorld CreateConfirmedAuthorityWorld(WorldId authWorldId)
        {
            var options = SessionMobaWorldBootstrapFactory.CreateWorldOptions(_plan, authWorldId);
            return _confirmedRuntime.CreateWorld(options);
        }

        private void SetupConfirmedAuthorityInputAndBootstrap()
        {
            _confirmedLastTickedFrame = 0;

            var hub = CreateConfirmedAuthorityInputHub();
            BindConfirmedAuthorityInputHub(hub);
            ValidateConfirmedAuthorityWorldBootstrap();
        }

        private static FrameJitterBufferHub<PlayerInputCommand[]> CreateConfirmedAuthorityInputHub()
        {
            return FrameSyncInputHubFactory.CreateJitterBufferHub<PlayerInputCommand[]>(
                delayFrames: 0,
                missingMode: MissingFrameMode.FillDefault,
                missingFrameFactory: Array.Empty<PlayerInputCommand>,
                initialCapacity: 256);
        }

        private void BindConfirmedAuthorityInputHub(FrameJitterBufferHub<PlayerInputCommand[]> hub)
        {
            _confirmedInputSource = hub;
            _confirmedConsumable = hub;
            _confirmedSink = hub;
        }

        private void ValidateConfirmedAuthorityWorldBootstrap()
        {
            try
            {
                if (_confirmedWorld?.Services == null)
                {
                    Log.Error("[BattleSessionFeature] ConfirmedAuthorityWorld bootstrap failed: world.Services is null");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
