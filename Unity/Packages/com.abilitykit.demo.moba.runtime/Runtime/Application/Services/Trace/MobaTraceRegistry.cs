using System.Collections.Generic;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaTraceRegistry))]
    public sealed class MobaTraceRegistry : TraceTreeRegistry<MobaTraceMetadata>, IService
    {
        public MobaTraceRegistry()
            : base(new DictionaryTraceMetadataStore<MobaTraceMetadata>())
        {
        }

        public TraceEndpoint ResolveEndpoint(MobaTraceKind kind, int configId)
        {
            switch (kind)
            {
                case MobaTraceKind.SkillPhase:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Skill, configId);
                case MobaTraceKind.EffectExecution:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Effect, configId);
                case MobaTraceKind.EffectAction:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Action, configId);
                case MobaTraceKind.BuffApply:
                case MobaTraceKind.BuffTick:
                case MobaTraceKind.BuffRemove:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Buff, configId);
                case MobaTraceKind.ProjectileLaunch:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Projectile, configId);
                case MobaTraceKind.ProjectileHit:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.ProjectileHit, configId);
                case MobaTraceKind.AreaSpawn:
                case MobaTraceKind.AreaExpire:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Area, configId);
                case MobaTraceKind.AreaEnter:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.AreaEnter, configId);
                case MobaTraceKind.SummonSpawn:
                case MobaTraceKind.SummonDeath:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Summon, configId);
                case MobaTraceKind.PresentationPlay:
                case MobaTraceKind.PresentationStop:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Presentation, configId);
                case MobaTraceKind.DamageAttack:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.DamageAttack, configId);
                case MobaTraceKind.DamageCalc:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.DamageCalc, configId);
                case MobaTraceKind.DamageApply:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.DamageResult, configId);
                default:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Action, configId);
            }
        }

        public long CreateRootContext(
            MobaTraceKind kind,
            int configId,
            long sourceActorId = 0,
            long targetActorId = 0,
            object originSource = null,
            object originTarget = null)
        {
            return CreateRoot((int)kind, sourceActorId, targetActorId, originSource, originTarget, configId);
        }

        public long CreateChildContext(
            long parentContextId,
            MobaTraceKind kind,
            int configId,
            long sourceActorId = 0,
            long targetActorId = 0,
            object originSource = null,
            object originTarget = null)
        {
            return CreateChild(parentContextId, (int)kind, sourceActorId, targetActorId, originSource, originTarget, configId);
        }

        public bool EndContext(long contextId, TraceLifecycleReason reason)
        {
            return End(contextId, (int)reason);
        }

        public bool EndContext(long contextId, int reason = 0)
        {
            return End(contextId, reason);
        }

        public TraceRootScope CreateEffectRoot(
            int effectConfigId,
            int triggerPlanId,
            int sourceActorId,
            int targetActorId,
            EffectContextKind contextKind)
        {
            var configId = effectConfigId > 0 ? effectConfigId : triggerPlanId;
            return this.CreateRootScope(
                (int)MobaTraceKind.EffectExecution,
                sourceActorId,
                targetActorId,
                TraceEndpoint.Config(MobaRuntimeKindNames.Effect, configId),
                TraceEndpoint.Actor(targetActorId),
                configId);
        }

        public TraceTreeScope CreateActionChild(
            long parentRootId,
            int actionId,
            int sourceActorId,
            int targetActorId)
        {
            return this.CreateChildScope(
                parentRootId,
                (int)MobaTraceKind.EffectAction,
                sourceActorId,
                targetActorId,
                TraceEndpoint.Config(MobaRuntimeKindNames.Action, actionId),
                TraceEndpoint.Actor(targetActorId),
                actionId);
        }

        public List<TraceSnapshot<MobaTraceMetadata>> GetChain(long rootId)
        {
            var list = new List<TraceSnapshot<MobaTraceMetadata>>();
            foreach (var snapshot in GetNodesByRoot(rootId))
            {
                list.Add(snapshot);
            }

            return list;
        }

        public bool ValidateChain(long rootId)
        {
            return Contains(rootId);
        }

        public override string GetKindName(int kind)
        {
            return ((MobaTraceKind)kind).ToString();
        }

        protected override MobaTraceMetadata CreateMetadata(
            long rootId,
            int kind,
            long sourceActorId,
            long targetActorId,
            long originId,
            string originDisplay,
            long targetId,
            string targetDisplay,
            int configId)
        {
            return new MobaTraceMetadata
            {
                RootId = rootId,
                ParentId = 0,
                Kind = kind,
                TraceKind = (MobaTraceKind)kind,
                ConfigId = configId,
                SourceActorId = sourceActorId,
                TargetActorId = targetActorId,
                SourceId = sourceActorId,
                TargetId = targetActorId,
                OriginSourceId = originId,
                OriginSource = originDisplay,
                OriginTargetId = targetId,
                OriginTarget = targetDisplay,
                Message = originDisplay
            };
        }

        protected override long GetSourceActorId(MobaTraceMetadata metadata) => metadata.SourceActorId;
        protected override long GetTargetActorId(MobaTraceMetadata metadata) => metadata.TargetActorId;
        protected override long GetOriginSourceId(MobaTraceMetadata metadata) => metadata.OriginSourceId;
        protected override string GetOriginSourceDisplay(MobaTraceMetadata metadata) => metadata.OriginSource;
        protected override long GetOriginTargetId(MobaTraceMetadata metadata) => metadata.OriginTargetId;
        protected override string GetOriginTargetDisplay(MobaTraceMetadata metadata) => metadata.OriginTarget;
    }

    public static class MobaRuntimeKindNames
    {
        public const string Actor = "actor";
        public const string Skill = "skill";
        public const string SkillPipeline = "skill.pipeline";
        public const string Effect = "effect";
        public const string Action = "action";
        public const string Buff = "buff";
        public const string Projectile = "projectile";
        public const string Area = "area";
        public const string Summon = "summon";
        public const string Presentation = "presentation";
        public const string DamageAttack = "damage.attack";
        public const string DamageCalc = "damage.calc";
        public const string DamageResult = "damage.result";
        public const string ProjectileHit = "projectile.hit";
        public const string AreaEnter = "area.enter";
        public const string Unit = "unit";
        public const string UnitDeath = "unit.death";
    }
}
