using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Services.Buffs;
using AbilityKit.Demo.Moba.Services.Buffs.Presentation;
using AbilityKit.Demo.Moba.Services.Buffs.Runtime;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA-scoped continuous behavior manager for buffs, skill pipelines, movement, and other runtime processes.
    /// </summary>
    [WorldService(typeof(IContinuousManager), WorldLifetime.Scoped)]
    [WorldService(typeof(MobaContinuousManager), WorldLifetime.Scoped)]
    public sealed class MobaContinuousManager : DefaultContinuousManager, IWorldInitializable, System.IDisposable
    {
        private readonly List<IMobaContinuousIntervalHandler> _intervalHandlers = new List<IMobaContinuousIntervalHandler>();
        private MobaContinuousModifierProjectorRegistry _modifierProjectors;
        private MobaContinuousLifecycleBinder _lifecycleBinder;
        private MobaContinuousContextLifecycleBinder _contextLifecycleBinder;
        private BuffContinuousIntervalHandler _buffIntervalHandler;

        public void OnInit(IWorldResolver services)
        {
            services.TryResolve(out MobaConfigDatabase configs);
            services.TryResolve(out AbilityKit.Triggering.Eventing.IEventBus eventBus);
            services.TryResolve(out MobaEffectExecutionService effects);
            services.TryResolve(out MobaTraceRegistry trace);
            services.TryResolve(out AbilityKit.Ability.Triggering.Runtime.ITriggerActionRunner actionRunner);
            _modifierProjectors = new MobaContinuousModifierProjectorRegistry();
            _modifierProjectors.OnInit(services);
            _lifecycleBinder = new MobaContinuousLifecycleBinder(_modifierProjectors);
            AddLifecycleBinder(_lifecycleBinder);
            _contextLifecycleBinder = new MobaContinuousContextLifecycleBinder(trace, actionRunner);
            AddLifecycleBinder(_contextLifecycleBinder);
            _buffIntervalHandler = new BuffContinuousIntervalHandler(configs, null, null, null);
            _intervalHandlers.Add(_buffIntervalHandler);
        }

        public void Reproject(IContinuous continuous)
        {
            _lifecycleBinder?.Reproject(continuous);
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f) return;

            var active = GetAllActiveContinuous();
            for (var i = 0; i < active.Count; i++)
            {
                var continuous = active[i];
                if (continuous == null || continuous.IsTerminated || !continuous.IsActive || continuous.IsPaused) continue;

                if (continuous is IMobaTickableContinuous tickable)
                {
                    tickable.TickManaged(deltaTimeSeconds);
                }

                for (var handlerIndex = 0; handlerIndex < _intervalHandlers.Count; handlerIndex++)
                {
                    var handler = _intervalHandlers[handlerIndex];
                    if (handler == null || !handler.CanHandle(continuous)) continue;
                    if (!(continuous.Config is IMobaContinuousPeriodicConfig periodicConfig)) continue;
                    if (periodicConfig.IntervalSeconds <= 0f || periodicConfig.IntervalEffectIds == null || periodicConfig.IntervalEffectIds.Count == 0) continue;
                    if (!(continuous is IMobaContinuousIntervalState intervalState)) continue;

                    intervalState.IntervalRemainingSeconds -= deltaTimeSeconds;
                    if (intervalState.IntervalRemainingSeconds > 0f) continue;

                    if (continuous is IMobaContinuousExecutionContextProvider contextProvider && contextProvider.TryGetCombatExecutionContext(out var executionContext))
                    {
                        handler.OnInterval(continuous, periodicConfig, in executionContext);
                    }

                    intervalState.IntervalRemainingSeconds = periodicConfig.IntervalSeconds;
                }

                if (continuous is IMobaContinuousRuntimeStateSync stateSync)
                {
                    stateSync.SyncManagedState();
                }
            }
        }

        public void Dispose()
        {
            _intervalHandlers.Clear();
            _buffIntervalHandler = null;
            _contextLifecycleBinder = null;
            _lifecycleBinder = null;
            _modifierProjectors = null;
        }
    }
}
