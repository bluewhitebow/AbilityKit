using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.EntitasAdapters;
using AbilityKit.Ability.FrameSync.Rollback;
using AbilityKit.Ability.Host.Extensions.FrameSync;
using AbilityKit.Ability.Host.Extensions.WorldStart;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Network.Runtime;
using AbilityKit.Demo.Moba.Rollback;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Battle.Moba.Config;
using AbilityKit.Game.Flow;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime;
using AbilityKit.Game.Flow.Battle.FrameSync;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void CreateRemoteDrivenRuntimeAndWorld()
        {
            _remoteDrivenWorlds = SessionMobaWorldBootstrapFactory.CreateWorldManager();

            var serverOptions = new AbilityKit.Ability.Host.Framework.HostRuntimeOptions();
            _remoteDrivenRuntime = new AbilityKit.Ability.Host.Framework.HostRuntime(_remoteDrivenWorlds, serverOptions);

            var fixedDelta = GetFixedDeltaSeconds();

            var modules = new AbilityKit.Ability.Host.Framework.HostRuntimeModuleHost();
            InstallRemoteDrivenPredictionModules(modules, fixedDelta);
            modules.Add(new AbilityKit.Ability.Host.Extensions.Time.ServerFrameTimeModule(fixedDelta));
            modules.Add(new WorldAutoStartModule());
            modules.InstallAll(_remoteDrivenRuntime, serverOptions);

            BindRemoteDrivenPredictionFeaturesToBattleContext();

            IClientPredictionDriverStats stats = null;
            try
            {
                _remoteDrivenRuntime.Features.TryGetFeature<IClientPredictionDriverStats>(out stats);
            }
            catch
            {
                stats = null;
            }

            var authorityFramesSource = stats != null
                ? new ClientPredictionDriverStatsFramesSource(stats)
                : null;
            var options = SessionMobaWorldBootstrapFactory.CreateWorldOptions(
                _plan,
                new WorldId(_plan.WorldId),
                authorityFramesSource);
            _remoteDrivenWorld = _remoteDrivenRuntime.CreateWorld(options);

            try
            {
                if (_remoteDrivenWorld?.Services != null && _remoteDrivenWorld.Services.TryResolve<MobaAuthorityFrameService>(out var auth) && auth != null)
                {
                    auth.BindWorld(_remoteDrivenWorld.Id);
                }
            }
            catch
            {
                // ignored
            }

            try
            {
                if (_remoteDrivenWorld?.Services == null)
                {
                    Log.Error("[BattleSessionFeature] RemoteDrivenLocalWorld bootstrap failed: world.Services is null");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[BattleSessionFeature] RemoteDrivenLocalWorld bootstrap threw");
            }
        }

        private void InstallRemoteDrivenPredictionModules(AbilityKit.Ability.Host.Framework.HostRuntimeModuleHost modules, float fixedDelta)
        {
            if (_plan.EnableClientPrediction)
            {
                modules.Add(new AbilityKit.Ability.Host.Extensions.FrameSync.ClientPredictionDriverModule(
                    resolveRemoteInputs: _ => _remoteDrivenConsumable,
                    resolveLocalInputs: _ => _ctx != null ? _ctx.LocalInputQueue : null,
                    resolveIdealFrameLimit: _ => ResolveIdealFrameLimit(_),
                    inputDelayFrames: _plan.InputDelayFrames < 0 ? 0 : _plan.InputDelayFrames,
                    maxPredictionAheadFrames: 30,
                    minPredictionWindow: 1,
                    backlogEwmaAlpha: 0.20f,
                    enableRollback: true,
                    rollbackHistoryFrames: 240,
                    rollbackCaptureEveryNFrames: 1,
                    buildRollbackRegistry: world =>
                    {
                        var reg = new AbilityKit.Ability.FrameSync.Rollback.RollbackRegistry();
                        if (world?.Services == null) return reg;

                        if (world.Services.TryResolve<MobaActorRegistry>(out var actorReg) && actorReg != null)
                        {
                            reg.Register(new MobaActorTransformRollbackProvider(actorReg));
                        }

                        if (world.Services.TryResolve<PassiveSkillTriggerEventRollbackLog>(out var passiveLog) && passiveLog != null)
                        {
                            reg.Register(passiveLog);
                        }

                        if (world.Services.TryResolve<RollbackWorldRandom>(out var rng) && rng != null)
                        {
                            reg.Register(rng);
                        }

                        return reg;
                    },
                    buildComputeHash: world =>
                    {
                        if (world?.Services == null) return null;

                        if (!world.Services.TryResolve<MobaGamePhaseService>(out var phase) || phase == null)
                        {
                            return null;
                        }

                        if (!world.Services.TryResolve<MobaActorRegistry>(out var registry) || registry == null)
                        {
                            return null;
                        }

                        return _ => new AbilityKit.Ability.FrameSync.Rollback.WorldStateHash(ComputeStateHash(phase, registry));
                    }));
            }
            else
            {
                modules.Add(new AbilityKit.Ability.Host.Extensions.FrameSync.ClientPredictionDriverModule(
                    resolveRemoteInputs: _ => _remoteDrivenConsumable,
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
                    buildComputeHash: _ => null));
            }
        }

        private void BindRemoteDrivenPredictionFeaturesToBattleContext()
        {
            if (_ctx == null) return;
            if (!ShouldExposeRemoteDrivenPredictionFeatures()) return;

            BindRemoteDrivenPredictionStats();

            if (!_plan.EnableClientPrediction)
            {
                ClearRemoteDrivenPredictionControls();
                return;
            }

            BindRemoteDrivenPredictionControls();
        }

        private bool ShouldExposeRemoteDrivenPredictionFeatures()
        {
            return _plan.HostMode == BattleStartConfig.BattleHostMode.GatewayRemote && _plan.UseGatewayTransport;
        }

        private void BindRemoteDrivenPredictionStats()
        {
            _ctx.PredictionStats = TryGetRemoteDrivenFeature<IClientPredictionDriverStats>();
        }

        private void BindRemoteDrivenPredictionControls()
        {
            _ctx.PredictionReconcileTarget = TryGetRemoteDrivenFeature<IClientPredictionReconcileTarget>();
            _ctx.PredictionReconcileControl = TryGetRemoteDrivenFeature<IClientPredictionReconcileControl>();
            _ctx.PredictionTuningControl = TryGetRemoteDrivenFeature<IClientPredictionTuningControl>();
        }

        private void ClearRemoteDrivenPredictionControls()
        {
            _ctx.PredictionReconcileTarget = null;
            _ctx.PredictionReconcileControl = null;
            _ctx.PredictionTuningControl = null;
        }

        private T TryGetRemoteDrivenFeature<T>()
            where T : class
        {
            try
            {
                return _remoteDrivenRuntime.Features.TryGetFeature<T>(out var feature) ? feature : null;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[BattleSessionFeature] TryGetRemoteDrivenFeature failed: {typeof(T).Name}");
                return null;
            }
        }

        private void SetupRemoteDrivenInputAndDebugStats()
        {
            _remoteDrivenLastTickedFrame = 0;

            var buffer = CreateRemoteDrivenInputBuffer();
            BindRemoteDrivenInputBuffer(buffer);
            PublishRemoteDrivenInputStats(buffer);
        }

        private FrameJitterBuffer<PlayerInputCommand[]> CreateRemoteDrivenInputBuffer()
        {
            return new FrameJitterBuffer<PlayerInputCommand[]>(
                delayFrames: ResolveRemoteDrivenInputDelay(),
                missingMode: MissingFrameMode.FillDefault,
                missingFrameFactory: Array.Empty<PlayerInputCommand>,
                initialCapacity: 256);
        }

        private int ResolveRemoteDrivenInputDelay()
        {
            return _plan.InputDelayFrames < 0 ? 0 : _plan.InputDelayFrames;
        }

        private void BindRemoteDrivenInputBuffer(FrameJitterBuffer<PlayerInputCommand[]> buffer)
        {
            _remoteDrivenInputSource = buffer;
            _remoteDrivenConsumable = buffer;
            _remoteDrivenSink = buffer;
        }

        private static void PublishRemoteDrivenInputStats(FrameJitterBuffer<PlayerInputCommand[]> buffer)
        {
            AbilityKit.Game.Flow.BattleFlowDebugProvider.JitterBufferStats = new AbilityKit.Game.Flow.JitterBufferStatsSnapshot
            {
                DelayFrames = buffer.DelayFrames,
                MissingMode = buffer.MissingMode.ToString(),
                TargetFrame = buffer.TargetFrame,
                MaxReceivedFrame = buffer.MaxReceivedFrame,
                LastConsumedFrame = buffer.LastConsumedFrame,
                BufferedCount = buffer.Count,
                MinBufferedFrame = buffer.MinBufferedFrame,

                AddedCount = buffer.AddedCount,
                DuplicateCount = buffer.DuplicateCount,
                LateCount = buffer.LateCount,
                ConsumedCount = buffer.ConsumedCount,
                FilledDefaultCount = buffer.FilledDefaultCount,
            };
        }
    }
}
