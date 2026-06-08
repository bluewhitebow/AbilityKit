namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public sealed class MobaPlanActionExecutionContextResolverOptions
    {
        public static readonly MobaPlanActionExecutionContextResolverOptions Default = new MobaPlanActionExecutionContextResolverOptions();

        public bool StrictFallback { get; set; }
    }
}
