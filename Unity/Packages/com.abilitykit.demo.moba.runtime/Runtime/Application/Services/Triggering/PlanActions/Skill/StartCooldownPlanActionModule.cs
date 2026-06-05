using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: 10)]
    public sealed class StartCooldownPlanActionModule : MobaPlanActionModuleBase<StartCooldownArgs, StartCooldownPlanActionModule>
    {
        protected override IActionSchema<StartCooldownArgs, IWorldResolver> Schema => StartCooldownSchema.Instance;

        protected override void Execute(object triggerArgs, StartCooldownArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (args.CooldownMs <= 0) return;

            if (!ctx.Context.TryResolve<MobaActorLookupService>(out var actors) || actors == null)
            {
                Log.Warning("[Plan] start_cooldown: MobaActorLookupService not found");
                return;
            }

            var input = MobaPlanActionInputResolver.ResolveEffect(triggerArgs, ctx);
            if (!input.HasCasterActor)
            {
                Log.Warning("[Plan] start_cooldown: caster actor not found");
                return;
            }

            var skillId = args.SkillId;
            var skillSlot = args.SkillSlot;
            if (skillId <= 0 && triggerArgs is SkillPipelineContext skillContext)
            {
                skillId = skillContext.SkillId;
            }

            if (skillSlot <= 0 && triggerArgs is SkillPipelineContext skillPipelineContext)
            {
                skillSlot = skillPipelineContext.SkillSlot;
            }

            if (skillId <= 0 || skillSlot <= 0)
            {
                throw new InvalidOperationException($"[Plan] start_cooldown failed: invalid skill. actorId={input.CasterActorId}, skillId={skillId}, slot={skillSlot}, cooldownMs={args.CooldownMs}");
            }

            ctx.Context.TryResolve<IFrameTime>(out var frameTime);
            var now = SkillHandlerRuntimeAccess.GetCurrentTimeMs(frameTime);
            var cooldownEndTimeMs = now + args.CooldownMs;
            if (!SkillHandlerRuntimeAccess.TrySetActiveSkillCooldown(actors, input.CasterActorId, skillSlot, skillId, cooldownEndTimeMs))
            {
                throw new InvalidOperationException($"[Plan] start_cooldown failed: active skill not found. actorId={input.CasterActorId}, skillId={skillId}, slot={skillSlot}, cooldownMs={args.CooldownMs}");
            }

            Log.Info($"[Plan] start_cooldown: actorId={input.CasterActorId}, skillId={skillId}, slot={skillSlot}, cooldownMs={args.CooldownMs}, endMs={cooldownEndTimeMs}");
        }
    }
}
