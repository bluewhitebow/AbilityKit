using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    [RuntimeSnapshotConverter(MobaOpCodes.Snapshot.PresentationCue)]
    public sealed class PresentationCueSnapshotConverter : IRuntimeSnapshotConverter
    {
        public int OpCode => MobaOpCodes.Snapshot.PresentationCue;

        public bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            var entries = MobaPresentationCueSnapshotCodec.Deserialize(snapshot.Payload);
            if (entries.Length == 0)
            {
                frameSnapshot = default;
                return false;
            }

            var cues = PresentationCueSnapshotMapper.Map(entries);

            frameSnapshot = new FrameSnapshotData(
                frameIndex: frameIndex,
                timestamp: timestamp,
                type: SnapshotType.Delta,
                presentationCues: cues);
            return true;
        }

    }
}
