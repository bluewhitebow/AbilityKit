using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Runtime descriptor for a registered MOBA plan action module.
    /// </summary>
    public readonly struct MobaPlanActionDescriptor
    {
        public MobaPlanActionDescriptor(int order, string moduleName, string actionName, IPlanActionModule module)
        {
            Order = order;
            ModuleName = moduleName;
            ActionName = actionName;
            Module = module;
        }

        public int Order { get; }
        public string ModuleName { get; }
        public string ActionName { get; }
        public IPlanActionModule Module { get; }
    }
}
