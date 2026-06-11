using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Triggering;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class MobaBuffPresentationCueReporter
    {
        private const string OwnerKindBuff = "Buff";

        private readonly MobaConfigDatabase _configs;
        private readonly MobaPresentationCueSnapshotService _snapshots;

        public MobaBuffPresentationCueReporter(MobaConfigDatabase configs, MobaPresentationCueSnapshotService snapshots)
        {
            _configs = configs;
            _snapshots = snapshots;
        }

        public void Started(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime)
        {
            Report(MobaPresentationCueStage.Started, buff, sourceActorId, targetActorId, runtime, TraceLifecycleReason.None);
        }

        public void Refreshed(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime)
        {
            Report(MobaPresentationCueStage.Refreshed, buff, sourceActorId, targetActorId, runtime, TraceLifecycleReason.None);
        }

        public void StackChanged(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime)
        {
            Report(MobaPresentationCueStage.StackChanged, buff, sourceActorId, targetActorId, runtime, TraceLifecycleReason.None);
        }

        public void Ticked(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime)
        {
            Report(MobaPresentationCueStage.Ticked, buff, sourceActorId, targetActorId, runtime, TraceLifecycleReason.None);
        }

        public void Ended(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            Report(ToEndStage(reason), buff, sourceActorId, targetActorId, runtime, reason);
        }

        private void Report(MobaPresentationCueStage stage, BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            if (_snapshots == null) return;
            if (buff == null) return;
            if (runtime == null) return;
            if (buff.PresentationTemplateId <= 0) return;
            if (!TryResolveTemplate(buff.PresentationTemplateId, out var template) || template == null) return;

            var entry = BuildEntry(stage, buff, template, sourceActorId, targetActorId, runtime, reason);
            _snapshots.Report(in entry);
        }

        private MobaPresentationCueSnapshotEntry BuildEntry(
            MobaPresentationCueStage stage,
            BuffMO buff,
            PresentationTemplateMO template,
            int sourceActorId,
            int targetActorId,
            BuffRuntime runtime,
            TraceLifecycleReason reason)
        {
            var remainingSeconds = ResolveRemainingSeconds(runtime);
            return new MobaPresentationCueSnapshotEntry
            {
                Stage = (int)stage,
                CueKind = OwnerKindBuff,
                TemplateId = template.Id,
                VfxId = template.AssetId,
                RequestKey = BuildInstanceKey(buff.Id, targetActorId, runtime.SourceContextId),
                SourceActorId = sourceActorId,
                TargetActorId = targetActorId,
                Targets = targetActorId > 0 ? new[] { targetActorId } : null,
                OffsetX = template.OffsetX,
                OffsetY = template.OffsetY,
                OffsetZ = template.OffsetZ,
                DurationMsOverride = template.DefaultDurationMs > 0 ? template.DefaultDurationMs : buff.DurationMs,
                Scale = template.Scale > 0f ? template.Scale : 1f,
                ColorR = template.ColorR != 0f ? template.ColorR : 1f,
                ColorG = template.ColorG != 0f ? template.ColorG : 1f,
                ColorB = template.ColorB != 0f ? template.ColorB : 1f,
                ColorA = template.ColorA != 0f ? template.ColorA : 1f,
                OwnerKind = OwnerKindBuff,
                InstanceId = runtime.SourceContextId,
                InstanceKey = BuildInstanceKey(buff.Id, targetActorId, runtime.SourceContextId),
                StackCount = runtime.StackCount,
                MaxStackCount = buff.MaxStacks,
                ElapsedSeconds = runtime.Continuous != null ? runtime.Continuous.ElapsedSeconds : 0f,
                RemainingSeconds = remainingSeconds,
                LifecycleReason = (int)reason
            };
        }

        private bool TryResolveTemplate(int templateId, out PresentationTemplateMO template)
        {
            template = null;
            if (_configs == null) return false;
            return _configs.GetTable<PresentationTemplateMO>().TryGet(templateId, out template);
        }

        private static float ResolveRemainingSeconds(BuffRuntime runtime)
        {
            if (runtime == null) return 0f;
            if (runtime.Continuous != null) return runtime.Continuous.RemainingSeconds;
            return runtime.Remaining;
        }

        private static MobaPresentationCueStage ToEndStage(TraceLifecycleReason reason)
        {
            switch (reason)
            {
                case TraceLifecycleReason.Expired:
                    return MobaPresentationCueStage.Expired;
                case TraceLifecycleReason.Completed:
                    return MobaPresentationCueStage.Completed;
                default:
                    return MobaPresentationCueStage.Removed;
            }
        }

        private static string BuildInstanceKey(int buffId, int targetActorId, long sourceContextId)
        {
            return $"buff:{targetActorId}:{buffId}:{sourceContextId}";
        }
    }
}
