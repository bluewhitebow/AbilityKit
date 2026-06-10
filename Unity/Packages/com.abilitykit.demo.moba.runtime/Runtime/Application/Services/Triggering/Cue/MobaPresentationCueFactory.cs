using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Triggering
{
    public sealed class MobaPresentationCueFactory : TriggerPlanJsonDatabase.ICueFactory
    {
        private readonly MobaPresentationCueSnapshotService _snapshots;

        public MobaPresentationCueFactory(MobaPresentationCueSnapshotService snapshots)
        {
            _snapshots = snapshots;
        }

        public ITriggerCue Create(string cueKind, string cueVfxId, string cueSfxId)
        {
            if (string.IsNullOrWhiteSpace(cueKind) && string.IsNullOrWhiteSpace(cueVfxId) && string.IsNullOrWhiteSpace(cueSfxId))
            {
                return NullTriggerCue.Instance;
            }

            return new MobaPresentationTriggerCue(_snapshots, cueKind, cueVfxId, cueSfxId);
        }
    }
}
