using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: 20)]
    public sealed class RemoveShieldPlanActionModule : MobaPlanActionModuleBase<RemoveShieldArgs, RemoveShieldPlanActionModule>
    {
        protected override IActionSchema<RemoveShieldArgs, IWorldResolver> Schema => RemoveShieldSchema.Instance;

        protected override void Execute(object triggerArgs, RemoveShieldArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (!ctx.Context.TryResolve<MobaShieldService>(out var shields) || shields == null)
            {
                LogRejected(ctx, "cannot resolve MobaShieldService.");
                return;
            }

            var coreInput = MobaPlanActionInputResolver.Resolve(triggerArgs, ctx);
            var effectInput = new MobaEffectActionInput(in coreInput);
            var targets = new List<int>(8);
            if (!MobaActionTargetResolver.TryResolveTargets(in args.TargetRequest, in coreInput, in effectInput, ctx, "remove_shield", targets))
            {
                return;
            }

            var removed = 0;
            for (var i = 0; i < targets.Count; i++)
            {
                var targetActorId = targets[i];
                if (targetActorId <= 0) continue;

                if (args.InstanceId > 0)
                {
                    if (shields.RemoveShield(targetActorId, args.InstanceId)) removed++;
                    continue;
                }

                removed += shields.RemoveShields(targetActorId, args.ShieldId, args.SourceActorId, args.RemoveAll);
            }

            LogApplied(ctx, $"shieldId={args.ShieldId} instance={args.InstanceId} source={args.SourceActorId} targets={targets.Count} removed={removed}");
        }
    }
}
