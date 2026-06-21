using AbilityKit.Core.Continuous;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class MobaContinuousTickProcessor : IMobaContinuousTickProcessor
    {
        private readonly System.Collections.Generic.IReadOnlyList<IMobaContinuousIntervalHandler> _intervalHandlers;

        public MobaContinuousTickProcessor(System.Collections.Generic.IReadOnlyList<IMobaContinuousIntervalHandler> intervalHandlers)
        {
            _intervalHandlers = intervalHandlers;
        }

        public void Tick(IContinuous continuous, float deltaTimeSeconds)
        {
            if (continuous == null || deltaTimeSeconds <= 0f) return;
            if (continuous.IsTerminated || !continuous.IsActive || continuous.IsPaused) return;

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
        }
    }
}
