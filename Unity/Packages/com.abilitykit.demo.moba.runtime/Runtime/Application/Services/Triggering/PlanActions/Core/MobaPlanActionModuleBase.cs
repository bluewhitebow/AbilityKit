using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public interface IMobaPlanActionMetadata
    {
        string ActionName { get; }
    }

    /// <summary>
    /// Demo MOBA strongly typed action module base.
    /// The schema is the single source for ActionId so modules do not duplicate action constants.
    /// </summary>
    public abstract class MobaPlanActionModuleBase<TActionArgs, TModule> : NamedArgsPlanActionModuleBase<TActionArgs, IWorldResolver, TModule>, IMobaPlanActionMetadata
        where TModule : MobaPlanActionModuleBase<TActionArgs, TModule>
    {
        protected sealed override ActionId ActionId => Schema.ActionId;

        public string ActionName => Schema is MobaPlanActionSchemaBase<TActionArgs> schema
            ? schema.ConfigActionName
            : null;

        protected bool TryResolveRequired<T>(ExecCtx<IWorldResolver> ctx, out T service)
            where T : class
        {
            return MobaPlanActionDiagnostics.TryResolveRequired(ctx.Context, ActionName ?? typeof(TModule).Name, out service);
        }

        protected void LogRejected(ExecCtx<IWorldResolver> ctx, string reason)
        {
            MobaPlanActionDiagnostics.Rejected(ctx.Context, ActionName ?? typeof(TModule).Name, reason);
        }

        protected void LogApplied(ExecCtx<IWorldResolver> ctx, string message)
        {
            MobaPlanActionDiagnostics.Applied(ctx.Context, ActionName ?? typeof(TModule).Name, message);
        }

        protected void LogInvestigation(ExecCtx<IWorldResolver> ctx, string message)
        {
            MobaPlanActionDiagnostics.Investigation(ctx.Context, ActionName ?? typeof(TModule).Name, message);
        }

        protected void LogRejected(string reason)
        {
            MobaPlanActionDiagnostics.Rejected(ActionName ?? typeof(TModule).Name, reason);
        }

        protected void LogApplied(string message)
        {
            MobaPlanActionDiagnostics.Applied(ActionName ?? typeof(TModule).Name, message);
        }
    }
}
