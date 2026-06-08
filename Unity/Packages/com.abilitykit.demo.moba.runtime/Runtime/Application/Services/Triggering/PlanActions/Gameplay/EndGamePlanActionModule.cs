using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Demo.Moba.Gameplay;
using AbilityKit.Demo.Moba.Systems;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: 0)]
    public sealed class EndGamePlanActionModule : MobaPlanActionModuleBase<EndGameArgs, EndGamePlanActionModule>
    {
        protected override IActionSchema<EndGameArgs, IWorldResolver> Schema => EndGameSchema.Instance;

        protected override void Execute(object triggerArgs, EndGameArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (ctx.Context == null)
            {
                LogRejected("missing world context");
                return;
            }

            if (!TryResolveRequired(ctx, out MobaGameplayService gameplay))
            {
                return;
            }

            var reason = string.Empty;
            if (args.ReasonId > 0 && ctx.Context.TryResolve<TriggerPlanJsonDatabase>(out var db) && db != null)
            {
                db.TryGetString(args.ReasonId, out reason);
            }

            if (string.IsNullOrEmpty(reason))
            {
                reason = "trigger_end_game";
            }

            if (!gameplay.End(reason, args.WinTeamId))
            {
                LogRejected($"ignored phase={gameplay.Phase}, reason={reason}, winTeamId={args.WinTeamId}");
                return;
            }

            LogApplied($"reason={reason}, winTeamId={args.WinTeamId}");
        }
    }
}
