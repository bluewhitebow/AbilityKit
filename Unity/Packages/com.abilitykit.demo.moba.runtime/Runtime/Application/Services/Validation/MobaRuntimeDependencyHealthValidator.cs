using AbilityKit.Ability.Host.Extensions.Moba.Runtime;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services.Area;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Demo.Moba.Services.Projectile.Launch;
using AbilityKit.Demo.Moba.Services.Search;
using AbilityKit.Demo.Moba.Services.Triggering;
using AbilityKit.Demo.Moba.Services.Triggering.PlanActions;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.GameplayTags;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Services
{
    public static class MobaRuntimeDependencyValidationRules
    {
        public const string AggregateSource = "runtime.dependencies";
        public const string CoreSource = "runtime.dependencies.core";
        public const string SkillSource = "runtime.dependencies.skill";
        public const string ContinuousSource = "runtime.dependencies.continuous";
        public const string CombatSource = "runtime.dependencies.combat";
        public const string TemporaryEntitySource = "runtime.dependencies.temp_entity";
        public const string OutputSource = "runtime.dependencies.output";
        public const string DiagnosticsSource = "runtime.dependencies.diagnostics";

        public static void ValidateCore(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string source)
        {
            Require<MobaConfigDatabase>(context, report, source, "config.database", "MobaConfigDatabase is required by skill, buff, projectile, summon, area and tag template runtime.");
            Require<IEventBus>(context, report, source, "event.bus", "IEventBus is required for gameplay, buff, projectile and trigger events.");
            Require<TriggerPlanJsonDatabase>(context, report, source, "trigger.plan.database", "TriggerPlanJsonDatabase is required for runtime trigger plans.");
            Require<TriggerRunner<AbilityKit.Ability.World.DI.IWorldResolver>>(context, report, source, "trigger.runner", "TriggerRunner<IWorldResolver> is required to execute configured plans.");
            Require<MobaEventSubscriptionRegistry>(context, report, source, "trigger.event.registry", "MobaEventSubscriptionRegistry is required to bind typed event args.");
            Require<MobaTriggerPlanSubscriptionService>(context, report, source, "trigger.plan.subscription", "MobaTriggerPlanSubscriptionService is required for lifecycle trigger subscriptions.");
            Require<PlanActionModuleRegistry>(context, report, source, "trigger.action.modules", "PlanActionModuleRegistry is required for plan action discovery.");
        }

        public static void ValidateSkill(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string source)
        {
            Require<IMobaSkillPipelineLibrary>(context, report, source, "skill.pipeline.library", "IMobaSkillPipelineLibrary is required for table driven skill execution.");
            Require<SkillExecutor>(context, report, source, "skill.executor", "SkillExecutor is required for command driven skill casts.");
            Require<MobaEffectInvokerService>(context, report, source, "skill.effect.invoker", "MobaEffectInvokerService is required for configured effect execution.");
            Require<MobaEffectExecutionService>(context, report, source, "skill.effect.execution", "MobaEffectExecutionService is required for trigger/action effect execution.");
            Require<MobaSkillCastRuntimeService>(context, report, source, "skill.cast.runtime", "MobaSkillCastRuntimeService is required for cast runtime tracking.");
        }

        public static void ValidateContinuous(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string source)
        {
            Require<MobaBuffService>(context, report, source, "buff.service", "MobaBuffService is required for continuous buff lifecycle.");
            Require<IContinuousManager>(context, report, source, "continuous.manager", "IContinuousManager is required for shared continuous behavior lifecycle.");
            Require<IMobaContinuousModifierQueryService>(context, report, source, "continuous.modifier.query", "IMobaContinuousModifierQueryService is required for modifier projection and parameter overrides.");
            Require<IMobaEffectiveTagQueryService>(context, report, source, "continuous.effective.tags", "IMobaEffectiveTagQueryService is required for gameplay tag based state/control queries.");
            Require<IMobaContinuousTagRuleService>(context, report, source, "continuous.tag.rules", "IMobaContinuousTagRuleService is required for tag admission and lifecycle rules.");
            Require<IGameplayTagService>(context, report, source, "gameplay.tags", "IGameplayTagService is required for gameplay tag based runtime state.");
        }

        public static void ValidateCombat(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string source)
        {
            Require<DamagePipelineService>(context, report, source, "combat.damage.pipeline", "DamagePipelineService is required for formal damage calculation.");
            Require<MobaShieldService>(context, report, source, "combat.shield", "MobaShieldService is required for shield mitigation stages.");
            Require<MobaDamageMitigationService>(context, report, source, "combat.damage.mitigation", "MobaDamageMitigationService is required for mitigation stages.");
            Require<MobaCombatRulesService>(context, report, source, "combat.rules", "MobaCombatRulesService is required for target legality and team rules.");
        }

        public static void ValidateTemporaryEntity(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string source)
        {
            Require<MobaProjectileService>(context, report, source, "projectile.service", "MobaProjectileService is required for projectile runtime.");
            Require<IMobaProjectileEmitterManager>(context, report, source, "projectile.emitter.manager", "IMobaProjectileEmitterManager is required for attribute driven projectile emitter extensions.");
            Require<MobaSummonService>(context, report, source, "summon.service", "MobaSummonService is required for summon spawning and owner lifecycle.");
            Require<MobaAreaRuntimeService>(context, report, source, "area.runtime", "MobaAreaRuntimeService is required for area runtime tracking.");
            Require<SearchTargetService>(context, report, source, "search.target", "SearchTargetService is required for configured target query execution.");
            Require<IMobaTemporaryEntityLifecycleHealthProvider>(context, report, source, "temp_entity.lifecycle", "IMobaTemporaryEntityLifecycleHealthProvider is required for projectile/area/summon lifecycle governance.");
        }

        public static void ValidateOutput(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string source)
        {
            Require<MobaSnapshotRouter>(context, report, source, "snapshot.router", "MobaSnapshotRouter is required for battle state snapshots.");
            Require<IMobaSnapshotHealthProvider>(context, report, source, "snapshot.health", "IMobaSnapshotHealthProvider is required for snapshot readiness diagnostics.");
            Require<IMobaBattleRuntimePort>(context, report, source, "runtime.port", "IMobaBattleRuntimePort is required as the single formal battle runtime entry.");
            Require<MobaAuthorityFrameService>(context, report, source, "frames.authority", "MobaAuthorityFrameService is required for confirmed/predicted frame tracking.");
        }

        public static void ValidateDiagnostics(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string source)
        {
            Require<IMobaBattleDiagnosticsService>(context, report, source, "diagnostics.service", "IMobaBattleDiagnosticsService is required for runtime diagnostics events.");
            Require<IMobaBattleExceptionPolicy>(context, report, source, "diagnostics.exception.policy", "IMobaBattleExceptionPolicy is required for centralized exception governance.");
            Require<MobaTraceRegistry>(context, report, source, "trace.registry", "MobaTraceRegistry is required for formal trace lineage diagnostics and skill cast roots.");
            Optional<MobaBattleRouteRegistry>(context, report, source, "routing.registry", "MobaBattleRouteRegistry is not resolved; battle route diagnostics may be incomplete.");
        }

        private static void Require<T>(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string source, string path, string message) where T : class
        {
            if (!context.TryResolve<T>(out _))
            {
                report.Error(source, path, message, typeof(T).Name, blocksStartup: true);
            }
        }

        private static void Optional<T>(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string source, string path, string message) where T : class
        {
            if (!context.TryResolve<T>(out _))
            {
                report.Warning(source, path, message, typeof(T).Name);
            }
        }
    }

    public sealed class MobaRuntimeCoreDependencyValidator : IMobaRuntimeValidator
    {
        public string Name => MobaRuntimeDependencyValidationRules.CoreSource;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;
            MobaRuntimeDependencyValidationRules.ValidateCore(in context, report, Name);
        }
    }

    public sealed class MobaRuntimeSkillDependencyValidator : IMobaRuntimeValidator
    {
        public string Name => MobaRuntimeDependencyValidationRules.SkillSource;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;
            MobaRuntimeDependencyValidationRules.ValidateSkill(in context, report, Name);
        }
    }

    public sealed class MobaRuntimeContinuousDependencyValidator : IMobaRuntimeValidator
    {
        public string Name => MobaRuntimeDependencyValidationRules.ContinuousSource;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;
            MobaRuntimeDependencyValidationRules.ValidateContinuous(in context, report, Name);
        }
    }

    public sealed class MobaRuntimeCombatDependencyValidator : IMobaRuntimeValidator
    {
        public string Name => MobaRuntimeDependencyValidationRules.CombatSource;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;
            MobaRuntimeDependencyValidationRules.ValidateCombat(in context, report, Name);
        }
    }

    public sealed class MobaRuntimeTemporaryEntityDependencyValidator : IMobaRuntimeValidator
    {
        public string Name => MobaRuntimeDependencyValidationRules.TemporaryEntitySource;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;
            MobaRuntimeDependencyValidationRules.ValidateTemporaryEntity(in context, report, Name);
        }
    }

    public sealed class MobaRuntimeOutputDependencyValidator : IMobaRuntimeValidator
    {
        public string Name => MobaRuntimeDependencyValidationRules.OutputSource;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;
            MobaRuntimeDependencyValidationRules.ValidateOutput(in context, report, Name);
        }
    }

    public sealed class MobaRuntimeDiagnosticsDependencyValidator : IMobaRuntimeValidator
    {
        public string Name => MobaRuntimeDependencyValidationRules.DiagnosticsSource;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;
            MobaRuntimeDependencyValidationRules.ValidateDiagnostics(in context, report, Name);
        }
    }

    public sealed class MobaRuntimeDependencyHealthValidator : IMobaRuntimeValidator
    {
        public string Name => MobaRuntimeDependencyValidationRules.AggregateSource;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;

            MobaRuntimeDependencyValidationRules.ValidateCore(in context, report, Name);
            MobaRuntimeDependencyValidationRules.ValidateSkill(in context, report, Name);
            MobaRuntimeDependencyValidationRules.ValidateContinuous(in context, report, Name);
            MobaRuntimeDependencyValidationRules.ValidateCombat(in context, report, Name);
            MobaRuntimeDependencyValidationRules.ValidateTemporaryEntity(in context, report, Name);
            MobaRuntimeDependencyValidationRules.ValidateOutput(in context, report, Name);
            MobaRuntimeDependencyValidationRules.ValidateDiagnostics(in context, report, Name);
        }
    }
}
