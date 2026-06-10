using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Moba.StateSync
{
    public enum PresentationCueStage : byte
    {
        None = 0,
        ConditionPassed = 1,
        ConditionFailed = 2,
        BeforeAction = 3,
        Executed = 4,
        Interrupted = 5,
        Skipped = 6,
    }

    [MemoryPackable]
    public partial struct MobaPresentationCueSnapshotEntry
    {
        [MemoryPackOrder(0)] public int Stage;
        [MemoryPackOrder(1)] public string CueKind;
        [MemoryPackOrder(2)] public string CueVfxId;
        [MemoryPackOrder(3)] public string CueSfxId;
        [MemoryPackOrder(4)] public int TemplateId;
        [MemoryPackOrder(5)] public int VfxId;
        [MemoryPackOrder(6)] public int SfxId;
        [MemoryPackOrder(7)] public string RequestKey;
        [MemoryPackOrder(8)] public int SourceActorId;
        [MemoryPackOrder(9)] public int TargetActorId;
        [MemoryPackOrder(10)] public int TriggerEventId;
        [MemoryPackOrder(11)] public string TriggerEventName;
        [MemoryPackOrder(12)] public int TriggerId;
        [MemoryPackOrder(13)] public int Phase;
        [MemoryPackOrder(14)] public int Priority;
        [MemoryPackOrder(15)] public int Order;
        [MemoryPackOrder(16)] public int ActionIndex;
        [MemoryPackOrder(17)] public int InterruptReason;
        [MemoryPackOrder(18)] public string InterruptSourceName;
        [MemoryPackOrder(19)] public int InterruptTriggerId;
        [MemoryPackOrder(20)] public bool InterruptConditionPassed;
        [MemoryPackOrder(21)] public int[] Targets;
        [MemoryPackOrder(22)] public float[] Positions;
        [MemoryPackOrder(23)] public float OffsetX;
        [MemoryPackOrder(24)] public float OffsetY;
        [MemoryPackOrder(25)] public float OffsetZ;
        [MemoryPackOrder(26)] public int DurationMsOverride;
        [MemoryPackOrder(27)] public float Scale;
        [MemoryPackOrder(28)] public float ColorR;
        [MemoryPackOrder(29)] public float ColorG;
        [MemoryPackOrder(30)] public float ColorB;
        [MemoryPackOrder(31)] public float ColorA;
    }

    [MemoryPackable]
    public partial struct MobaPresentationCueSnapshotPayload
    {
        [MemoryPackOrder(0)] public MobaPresentationCueSnapshotEntry[] Entries;

        [MemoryPackConstructor]
        public MobaPresentationCueSnapshotPayload(MobaPresentationCueSnapshotEntry[] entries)
        {
            Entries = entries;
        }
    }

    public static class MobaPresentationCueSnapshotCodec
    {
        public static byte[] Serialize(MobaPresentationCueSnapshotEntry[] entries)
        {
            entries ??= Array.Empty<MobaPresentationCueSnapshotEntry>();
            var payload = new MobaPresentationCueSnapshotPayload { Entries = entries };
            return WireSerializer.Serialize(in payload);
        }

        public static MobaPresentationCueSnapshotEntry[] Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return Array.Empty<MobaPresentationCueSnapshotEntry>();
            }

            var p = WireSerializer.Deserialize<MobaPresentationCueSnapshotPayload>(payload);
            return p.Entries ?? Array.Empty<MobaPresentationCueSnapshotEntry>();
        }
    }
}
