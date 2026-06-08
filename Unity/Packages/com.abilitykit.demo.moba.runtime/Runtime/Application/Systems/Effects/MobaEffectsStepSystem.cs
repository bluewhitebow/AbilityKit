using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Share.ECS;
using AbilityKit.ECS;
using AbilityKit.Effect;
using AbilityKit.Ability.Triggering;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;

namespace AbilityKit.Demo.Moba.Systems.Effects
{
    /// <summary>
    /// 效果步骤系统
    /// 执行所有单位的 Effects.Step()
    /// </summary>
    [WorldSystem(order: MobaSystemOrder.EffectsStep)]
    public sealed class MobaEffectsStepSystem : WorldSystemBase
    {
        private IFrameTime _time;
        private IEventBus _eventBus;
        private IUnitResolver _units;

        private IMobaBattleDiagnosticsService _diagnostics;
        private IMobaBattleExceptionPolicy _exceptions;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaEffectsStepSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _units);
            Services.TryResolve(out _time);
            Services.TryResolve(out _eventBus);
            Services.TryResolve(out _diagnostics);
            Services.TryResolve(out _exceptions);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId));
        }

        protected override void OnExecute()
        {
            if (_units == null || _time == null) return;

            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0) return;

            var diagnostics = _diagnostics;
            var start = diagnostics != null ? diagnostics.GetTimestamp() : 0L;
            var processed = 0;
            var sp = new WorldServiceProviderAdapter(Services);

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasActorId) continue;

                var actorId = e.actorId.Value;
                if (!_units.TryResolve(new EcsEntityId(actorId), out var unit) || unit == null) continue;

                var ctx = new EffectExecutionContext(
                    services: sp,
                    time: _time,
                    source: unit,
                    target: unit,
                    targetUnit: unit,
                    eventBus: _eventBus,
                    sourceContextId: 0
                );

                try
                {
                    unit.Effects.Step(in ctx);
                    processed++;
                }
                catch (Exception ex)
                {
                    var exceptions = _exceptions;
                    if (exceptions != null)
                    {
                        exceptions.Handle(
                            ex,
                            new MobaBattleExceptionContext(
                                MobaBattleExceptionDomain.WorldSystem,
                                "effects.step",
                                actorId: actorId),
                            MobaBattleExceptionSeverity.Recoverable);
                    }
                    else
                    {
                        AbilityKit.Core.Common.Log.Log.Exception(ex, $"[MobaEffectsStepSystem] Effects.Step failed. actor={actorId}");
                    }
                }
            }

            if (diagnostics != null)
            {
                diagnostics.Sample("moba.effects.actorCandidates", entities.Length);
                diagnostics.Sample("moba.effects.processed", processed);
                diagnostics.RecordDuration(
                    MobaBattleDiagnosticMetric.EffectsStep,
                    start,
                    MobaBattleDiagnosticsDefaults.EffectsStepWarnMs,
                    $"candidates={entities.Length} processed={processed}");
            }
        }
    }
}
