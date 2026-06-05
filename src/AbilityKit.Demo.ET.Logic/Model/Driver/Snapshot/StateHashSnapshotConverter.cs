using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    [RuntimeSnapshotConverter(MobaOpCodes.Snapshot.StateHash)]
    public sealed class StateHashSnapshotConverter : IRuntimeSnapshotConverter
    {
        public int OpCode => MobaOpCodes.Snapshot.StateHash;

        public bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            var payload = MobaStateHashSnapshotCodec.Deserialize(snapshot.Payload);
            if (payload.Version != MobaStateHashSnapshotCodec.Version || payload.Frame < 0 || payload.Hash == 0)
            {
                frameSnapshot = default;
                return false;
            }

            frameSnapshot = new FrameSnapshotData(
                frameIndex: frameIndex,
                timestamp: timestamp,
                type: SnapshotType.Delta,
                stateHash: new StateHashData(payload.Frame, payload.Hash));
            return true;
        }
    }
}
