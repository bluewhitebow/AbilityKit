using System;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    public sealed class MobaPresentationTriggerCue : ITriggerCue
    {
        private readonly MobaPresentationCueSnapshotService _snapshots;
        private readonly string _cueKind;
        private readonly string _cueVfxId;
        private readonly string _cueSfxId;
        private readonly int _vfxId;
        private readonly int _sfxId;
        private readonly int _templateId;

        public MobaPresentationTriggerCue(MobaPresentationCueSnapshotService snapshots, string cueKind, string cueVfxId, string cueSfxId)
        {
            _snapshots = snapshots;
            _cueKind = cueKind;
            _cueVfxId = cueVfxId;
            _cueSfxId = cueSfxId;
            _vfxId = ParseId(cueVfxId);
            _sfxId = ParseId(cueSfxId);
            _templateId = ParseId(cueKind);
        }

        public void OnConditionPassed(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.ConditionPassed, in context, -1);
        }

        public void OnConditionFailed(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.ConditionFailed, in context, -1);
        }

        public void OnBeforeAction(in TriggerCueContext context, int actionIndex)
        {
            Publish(MobaPresentationCueStage.BeforeAction, in context, actionIndex);
        }

        public void OnExecuted(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.Executed, in context, -1);
        }

        public void OnInterrupted(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.Interrupted, in context, -1);
        }

        public void OnSkipped(in TriggerCueContext context)
        {
            Publish(MobaPresentationCueStage.Skipped, in context, -1);
        }

        private void Publish(MobaPresentationCueStage stage, in TriggerCueContext context, int actionIndex)
        {
            if (_snapshots == null) return;
            if (IsEmptyCue()) return;

            var payload = BuildPayload(stage, in context, actionIndex);
            _snapshots.Report(in payload);
        }

        private MobaPresentationCueSnapshotEntry BuildPayload(MobaPresentationCueStage stage, in TriggerCueContext context, int actionIndex)
        {
            ResolveActors(context.Args, out var sourceActorId, out var targetActorId);
            ResolvePositions(context.Args, out var positions);

            var targets = targetActorId > 0
                ? new[] { targetActorId }
                : sourceActorId > 0 ? new[] { sourceActorId } : null;

            return new MobaPresentationCueSnapshotEntry
            {
                Stage = (int)stage,
                CueKind = _cueKind,
                CueVfxId = _cueVfxId,
                CueSfxId = _cueSfxId,
                TemplateId = _templateId,
                VfxId = _vfxId,
                SfxId = _sfxId,
                RequestKey = BuildRequestKey(context.TriggerId, context.Order),
                Targets = targets,
                Positions = FlattenPositions(positions),
                SourceActorId = sourceActorId,
                TargetActorId = targetActorId,
                TriggerEventId = context.EventId,
                TriggerEventName = context.EventName,
                TriggerId = context.TriggerId,
                Phase = context.Phase,
                Priority = context.Priority,
                Order = unchecked((int)context.Order),
                ActionIndex = actionIndex,
                InterruptReason = (int)context.InterruptReason,
                InterruptSourceName = context.InterruptSourceName,
                InterruptTriggerId = context.InterruptTriggerId,
                InterruptConditionPassed = context.InterruptConditionPassed,
                Scale = 1f,
                ColorR = 1f,
                ColorG = 1f,
                ColorB = 1f,
                ColorA = 1f
            };
        }

        private bool IsEmptyCue()
        {
            return string.IsNullOrWhiteSpace(_cueKind) && string.IsNullOrWhiteSpace(_cueVfxId) && string.IsNullOrWhiteSpace(_cueSfxId);
        }

        private static string BuildRequestKey(int triggerId, long order)
        {
            return triggerId > 0 ? $"cue:{triggerId}:{order}" : $"cue:{order}";
        }

        private static int ParseId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return int.TryParse(value, out var id) ? id : 0;
        }

        private static void ResolveActors(object args, out int sourceActorId, out int targetActorId)
        {
            sourceActorId = 0;
            targetActorId = 0;
            if (args is IMobaActorContextProvider actorProvider)
            {
                actorProvider.TryGetSourceActorId(out sourceActorId);
                actorProvider.TryGetTargetActorId(out targetActorId);
            }
        }

        private static void ResolveLineage(object args, out long sourceContextId, out long rootContextId, out long ownerContextId)
        {
            sourceContextId = 0;
            rootContextId = 0;
            ownerContextId = 0;

            if (args is IMobaTriggerLineageContextProvider lineageProvider && lineageProvider.TryGetLineageContext(out var lineage))
            {
                sourceContextId = lineage.SourceContextId;
                rootContextId = lineage.RootContextId;
                ownerContextId = lineage.OwnerKey;
                return;
            }

            if (args is IMobaOriginContextProvider originProvider && originProvider.TryGetOrigin(out var origin))
            {
                sourceContextId = origin.EffectiveParentContextId;
                rootContextId = origin.EffectiveRootContextId;
                ownerContextId = origin.OwnerContextId;
            }
        }

        private static void ResolvePositions(object args, out Vec3[] positions)
        {
            positions = null;
            if (args is PresentationEventArgs presentation && presentation.Positions != null && presentation.Positions.Length > 0)
            {
                positions = presentation.Positions;
            }
        }

        private static float[] FlattenPositions(Vec3[] positions)
        {
            if (positions == null || positions.Length == 0) return null;

            var values = new float[positions.Length * 3];
            for (int i = 0; i < positions.Length; i++)
            {
                int offset = i * 3;
                values[offset] = positions[i].X;
                values[offset + 1] = positions[i].Y;
                values[offset + 2] = positions[i].Z;
            }

            return values;
        }
    }
}
