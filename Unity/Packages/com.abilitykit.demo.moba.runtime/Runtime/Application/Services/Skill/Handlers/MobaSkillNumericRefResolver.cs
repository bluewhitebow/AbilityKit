using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Attributes;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Share.Config;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaSkillNumericRefResolver))]
    public sealed class MobaSkillNumericRefResolver : IService, IWorldInitializable
    {
        private MobaConfigDatabase _configs;
        private MobaActorLookupService _actors;

        public MobaSkillNumericRefResolver(MobaConfigDatabase configs = null, MobaActorLookupService actors = null)
        {
            _configs = configs;
            _actors = actors;
        }

        public void OnInit(IWorldResolver services)
        {
            if (services == null) return;
            if (_configs == null) services.TryResolve(out _configs);
            if (_actors == null) services.TryResolve(out _actors);
        }

        public double Resolve(NumericRefDTO dto, in HandlerContext context, double fallback = 0d)
        {
            if (dto == null) return fallback;

            var value = ResolveRaw(dto, in context, fallback);
            var coefficient = Math.Abs(dto.Coefficient) > double.Epsilon ? dto.Coefficient : 1d;
            return value * coefficient + dto.Add;
        }

        private double ResolveRaw(NumericRefDTO dto, in HandlerContext context, double fallback)
        {
            switch (dto.Kind)
            {
                case ENumericRefKind.Const:
                    return dto.ConstValue;
                case ENumericRefKind.SkillLevelCost:
                    return TryGetSkillLevel(context.PipelineContext, out var level) ? level.Cost : fallback;
                case ENumericRefKind.SkillLevelCooldownMs:
                    return TryGetSkillLevel(context.PipelineContext, out level) ? level.CooldownMs : context.PipelineContext?.SkillCooldownMs ?? fallback;
                case ENumericRefKind.ActorAttribute:
                    return TryGetActorAttribute(in context, dto.Actor, (BattleAttributeType)dto.AttributeType, out var attrValue) ? attrValue : fallback;
                case ENumericRefKind.ActorResourceCurrent:
                    return TryGetResource(in context, dto.Actor, (ResourceType)dto.ResourceType, out var current, out _) ? current : fallback;
                case ENumericRefKind.ActorResourceMax:
                    return TryGetResource(in context, dto.Actor, (ResourceType)dto.ResourceType, out _, out var max) ? max : fallback;
                case ENumericRefKind.ActorResourcePercent:
                    if (!TryGetResource(in context, dto.Actor, (ResourceType)dto.ResourceType, out current, out max) || max <= 0d)
                    {
                        return fallback;
                    }
                    return current / max;
                default:
                    return dto.ConstValue;
            }
        }

        private bool TryGetSkillLevel(SkillPipelineContext context, out SkillLevelDTO level)
        {
            level = null;
            if (context == null || _configs == null || context.SkillId <= 0) return false;
            if (!_configs.TryGetSkill(context.SkillId, out var skill) || skill == null || skill.LevelTableId <= 0) return false;
            if (!_configs.TryGetSkillLevelTable(skill.LevelTableId, out var table) || table == null) return false;

            var skillLevel = context.GetSkillLevel();
            if (skillLevel <= 0) skillLevel = 1;

            try
            {
                level = table.GetLevel(skillLevel);
                return level != null;
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetActorAttribute(in HandlerContext context, int actorSelector, BattleAttributeType attributeType, out double value)
        {
            value = 0d;
            if (attributeType == BattleAttributeType.None) return false;
            if (!TryGetActor(in context, actorSelector, out var actor)) return false;
            if (!actor.hasAttributeGroup || actor.attributeGroup.Group == null) return false;

            value = actor.attributeGroup.Group.GetValue(MobaAttributeIds.Get(attributeType));
            return true;
        }

        private bool TryGetResource(in HandlerContext context, int actorSelector, ResourceType resourceType, out double current, out double max)
        {
            current = 0d;
            max = 0d;
            if (resourceType == ResourceType.None) return false;
            if (!TryGetActor(in context, actorSelector, out var actor)) return false;
            if (!actor.hasResourceContainer || actor.resourceContainer.Value == null || actor.resourceContainer.Value.Map == null) return false;
            if (!actor.resourceContainer.Value.Map.TryGetValue(resourceType, out var state) || state == null) return false;

            current = state.Current;
            max = ResolveResourceMax(actor, state, resourceType);
            return true;
        }

        private static double ResolveResourceMax(global::ActorEntity actor, ResourceState state, ResourceType resourceType)
        {
            if (state.LastMax > 0f) return state.LastMax;
            if (actor == null || !actor.hasAttributeGroup || actor.attributeGroup.Group == null) return 0d;

            var attr = resourceType switch
            {
                ResourceType.Hp => BattleAttributeType.MAX_HP,
                ResourceType.Mana => BattleAttributeType.MAX_MANA,
                _ => BattleAttributeType.None
            };

            return attr != BattleAttributeType.None
                ? actor.attributeGroup.Group.GetValue(MobaAttributeIds.Get(attr))
                : 0d;
        }

        private bool TryGetActor(in HandlerContext context, int actorSelector, out global::ActorEntity actor)
        {
            actor = null;
            var actorId = actorSelector == (int)NumericRefActor.Target ? context.TargetActorId : context.CasterActorId;
            return actorId > 0 && _actors != null && _actors.TryGetActorEntity(actorId, out actor) && actor != null;
        }

        public void Dispose()
        {
        }
    }
}
