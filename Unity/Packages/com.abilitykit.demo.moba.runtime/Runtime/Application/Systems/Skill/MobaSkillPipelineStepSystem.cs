using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Demo.Moba.Systems
{
    [WorldSystem(order: MobaSystemOrder.SkillPipelines, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaSkillPipelineStepSystem : WorldSystemBase
    {
        private SkillExecutor _skills;
        private IMobaSkillPipelineLibrary _pipelines;
        private IWorldClock _clock;

        private IMobaBattleDiagnosticsService _diagnostics;
        private IMobaBattleExceptionPolicy _exceptions;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaSkillPipelineStepSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _skills);
            Services.TryResolve(out _clock);
            Services.TryResolve(out _diagnostics);
            Services.TryResolve(out _exceptions);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId));
            if (_skills == null || _clock == null || _group == null)
            {
                Log.Warning($"[MobaSkillPipelineStepSystem] Init incomplete. hasSkills={_skills != null}, hasClock={_clock != null}, hasGroup={_group != null}");
            }
        }

        protected override void OnExecute()
        {
            var diagnostics = _diagnostics;
            if (_skills == null || _clock == null)
            {
                var message = $"[MobaSkillPipelineStepSystem] Skip execute: hasSkills={_skills != null}, hasClock={_clock != null}";
                if (diagnostics != null) diagnostics.Warning("skill.pipeline.missingDependencies", message);
                else Log.Warning(message);
                return;
            }

            if (_clock.DeltaTime <= 0f)
            {
                var message = $"[MobaSkillPipelineStepSystem] Skip execute: deltaTime={_clock.DeltaTime:0.####}";
                if (diagnostics != null) diagnostics.Warning("skill.pipeline.invalidDeltaTime", message);
                else Log.Warning(message);
                return;
            }

            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0)
            {
                if (diagnostics != null) diagnostics.Warning("skill.pipeline.emptyGroup", "[MobaSkillPipelineStepSystem] Skip execute: actor group empty.");
                else Log.Warning("[MobaSkillPipelineStepSystem] Skip execute: actor group empty.");
                return;
            }

            var start = diagnostics != null ? diagnostics.GetTimestamp() : 0L;
            var stepped = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasActorId) continue;
                var actorId = e.actorId.Value;
                try
                {
                    _skills.Step(actorId);
                    stepped++;
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
                                "skill.pipeline.step",
                                actorId: actorId),
                            MobaBattleExceptionSeverity.Recoverable);
                    }
                    else
                    {
                        Log.Exception(ex, $"[MobaSkillPipelineStepSystem] Skill pipeline step failed. actor={actorId}");
                    }
                }
            }

            if (diagnostics != null)
            {
                diagnostics.Sample("moba.skill.pipeline.actorCandidates", entities.Length);
                diagnostics.Sample("moba.skill.pipeline.stepped", stepped);
                diagnostics.RecordDuration(
                    MobaBattleDiagnosticMetric.SkillPipelineStep,
                    start,
                    MobaBattleDiagnosticsDefaults.SkillPipelineStepWarnMs,
                    $"candidates={entities.Length} stepped={stepped}");
            }
        }
    }
}

