using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public sealed class SpawnAreaSchema : MobaPlanActionSchemaBase<SpawnAreaArgs>
    {
        public static readonly SpawnAreaSchema Instance = new SpawnAreaSchema();

        protected override string ActionName => TriggeringConstants.Actions.SpawnArea;

        public override SpawnAreaArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var areaId = ReadInt(namedArgs, ctx, 0, "area_id", "areaid", "aoe_id", "aoeid", "id");
            var positionMode = ReadInt(namedArgs, ctx, 0, "position_mode", "positionmode", "position");
            var radiusOverride = ReadFloat(namedArgs, ctx, 0f, "radius", "radius_override", "radiusoverride");
            var durationFrames = ReadInt(namedArgs, ctx, 0, "duration_frames", "durationframes", "lifetime_frames", "lifetimeframes");
            var durationMs = ReadInt(namedArgs, ctx, 0, "duration_ms", "durationms", "lifetime_ms", "lifetimems");
            var stayIntervalFrames = ReadInt(namedArgs, ctx, 0, "stay_interval_frames", "stayintervalframes", "stay_frames", "stayframes");
            var collisionLayerMaskOverride = ReadInt(namedArgs, ctx, 0, "collision_layer_mask", "collisionlayermask", "layer_mask", "layermask");
            var offsetX = ReadFloat(namedArgs, ctx, 0f, "offset_x", "offsetx", "x");
            var offsetY = ReadFloat(namedArgs, ctx, 0f, "offset_y", "offsety", "y");
            var offsetZ = ReadFloat(namedArgs, ctx, 0f, "offset_z", "offsetz", "z");
            return new SpawnAreaArgs(areaId, positionMode, radiusOverride, durationFrames, durationMs, stayIntervalFrames, collisionLayerMaskOverride, offsetX, offsetY, offsetZ);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            return RequireAny(args, "area_id", out error, "area_id", "areaid", "aoe_id", "aoeid", "id");
        }
    }
}
