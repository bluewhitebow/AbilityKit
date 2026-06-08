using AbilityKit.Demo.Moba.Components;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public readonly struct AddShieldArgs
    {
        public readonly int ShieldId;
        public readonly float Value;
        public readonly float AbsorbRatio;
        public readonly int Priority;
        public readonly int DamageTypeMask;
        public readonly int DurationFrames;
        public readonly int DurationMs;
        public readonly ShieldStackingPolicy StackingPolicy;
        public readonly ShieldConsumePolicy ConsumePolicy;
        public readonly MobaActionTargetRequest TargetRequest;

        public AddShieldArgs(
            int shieldId,
            float value,
            float absorbRatio,
            int priority,
            int damageTypeMask,
            int durationFrames,
            int durationMs,
            ShieldStackingPolicy stackingPolicy,
            ShieldConsumePolicy consumePolicy,
            in MobaActionTargetRequest targetRequest)
        {
            ShieldId = shieldId;
            Value = value;
            AbsorbRatio = absorbRatio;
            Priority = priority;
            DamageTypeMask = damageTypeMask;
            DurationFrames = durationFrames;
            DurationMs = durationMs;
            StackingPolicy = stackingPolicy;
            ConsumePolicy = consumePolicy;
            TargetRequest = targetRequest;
        }
    }
}
