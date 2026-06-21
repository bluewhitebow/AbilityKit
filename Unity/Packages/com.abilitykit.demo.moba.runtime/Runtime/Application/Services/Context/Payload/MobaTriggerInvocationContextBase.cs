namespace AbilityKit.Demo.Moba.Services
{
    public abstract class MobaTriggerInvocationContextBase : IMobaTriggerInvocationContext
    {
        public int TriggerId { get; set; }
        public abstract EffectContextKind Kind { get; }
        public int SourceActorId { get; set; }
        public int TargetActorId { get; set; }
        public long SourceContextId { get; set; }
    }
}
