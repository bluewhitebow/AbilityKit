using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    [MobaSnapshotEmitter(65)]
    [WorldService(typeof(MobaPresentationCueSnapshotService))]
    public sealed class MobaPresentationCueSnapshotService : IService, IMobaSnapshotEmitter
    {
        private readonly MobaLogicWorldRunGateService _phase;
        private readonly MobaSnapshotBuffer<MobaPresentationCueSnapshotEntry> _events = new MobaSnapshotBuffer<MobaPresentationCueSnapshotEntry>(32, 512);
        private FrameIndex _lastFrame;

        public MobaPresentationCueSnapshotService(MobaLogicWorldRunGateService phase)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _lastFrame = new FrameIndex(-999999);
        }

        public void Report(in MobaPresentationCueSnapshotEntry entry)
        {
            _events.Add(entry);
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            if (!_phase.InGame)
            {
                snapshot = default;
                return false;
            }

            if (frame.Value == _lastFrame.Value)
            {
                snapshot = default;
                return false;
            }

            _lastFrame = frame;

            if (_events.Count == 0)
            {
                snapshot = default;
                return false;
            }

            var payload = MobaPresentationCueSnapshotCodec.Serialize(_events.ToArrayClearAndTrim());
            snapshot = new WorldStateSnapshot(AbilityKit.Protocol.Moba.MobaOpCodes.Snapshot.PresentationCue, payload);
            return true;
        }

        public void Dispose()
        {
            _events.ClearAndTrim();
            _lastFrame = new FrameIndex(-999999);
        }
    }
}
