using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.FrameSync.Rollback;
using AbilityKit.Core.Pooling;
using AbilityKit.Core.Serialization;
using AbilityKit.Demo.Moba.Systems;

namespace AbilityKit.Demo.Moba.Rollback
{
    public sealed class PassiveSkillTriggerEventRollbackLog : IRollbackStateProvider
    {
        public const int DefaultKey = 10020;

        private static readonly ObjectPool<FrameEvents> s_frameEventsPool = Pools.GetPool(
            createFunc: () => new FrameEvents(),
            onRelease: events => events.Clear(),
            defaultCapacity: 32,
            maxSize: 512,
            collectionCheck: false);

        private static readonly ObjectPool<List<int>> s_intListPool = Pools.GetPool(
            createFunc: () => new List<int>(128),
            onRelease: list => list.Clear(),
            defaultCapacity: 8,
            maxSize: 64,
            collectionCheck: false);

        private readonly Dictionary<int, FrameEvents> _eventsByFrame = new Dictionary<int, FrameEvents>(256);

        public int Key => DefaultKey;

        public void Record(FrameIndex frame, in PassiveSkillTriggerEventArgs args)
        {
            var f = frame.Value;
            if (!_eventsByFrame.TryGetValue(f, out var fe))
            {
                fe = s_frameEventsPool.Get();
                _eventsByFrame[f] = fe;
            }

            fe.Sequence++;
            fe.Events.Add(new Entry(fe.Sequence, in args));
        }

        public IReadOnlyList<Entry> GetFrameEvents(FrameIndex frame)
        {
            return _eventsByFrame.TryGetValue(frame.Value, out var fe) ? fe.Events : Array.Empty<Entry>();
        }

        public void TruncateAfter(FrameIndex frame)
        {
            var cutoff = frame.Value;
            if (_eventsByFrame.Count == 0) return;

            var tmpKeys = s_intListPool.Get();
            try
            {
                foreach (var kv in _eventsByFrame)
                {
                    if (kv.Key > cutoff) tmpKeys.Add(kv.Key);
                }

                for (int i = 0; i < tmpKeys.Count; i++)
                {
                    RemoveFrameEvents(tmpKeys[i]);
                }
            }
            finally
            {
                s_intListPool.Release(tmpKeys);
            }
        }

        public byte[] Export(FrameIndex frame)
        {
            if (!_eventsByFrame.TryGetValue(frame.Value, out var fe) || fe.Events.Count == 0)
            {
                return Array.Empty<byte>();
            }

            var arr = fe.Events.ToArray();
            return BinaryObjectCodec.Encode(new Payload(1, fe.Sequence, arr));
        }

        public void Import(FrameIndex frame, byte[] payload)
        {
            TruncateAfter(frame);

            if (payload == null || payload.Length == 0)
            {
                RemoveFrameEvents(frame.Value);
                return;
            }

            var p = BinaryObjectCodec.Decode<Payload>(payload);
            if (p.Events == null || p.Events.Length == 0)
            {
                RemoveFrameEvents(frame.Value);
                return;
            }

            RemoveFrameEvents(frame.Value);

            var fe = s_frameEventsPool.Get();
            fe.Sequence = p.LastSequence;
            fe.Events.AddRange(p.Events);
            _eventsByFrame[frame.Value] = fe;
        }

        private void RemoveFrameEvents(int frame)
        {
            if (!_eventsByFrame.TryGetValue(frame, out var fe)) return;

            _eventsByFrame.Remove(frame);
            s_frameEventsPool.Release(fe);
        }

        private sealed class FrameEvents
        {
            public int Sequence;
            public readonly List<Entry> Events = new List<Entry>(8);

            public void Clear()
            {
                Sequence = 0;
                Events.Clear();
            }
        }

        public readonly struct Payload
        {
            [BinaryMember(0)] public readonly int Version;
            [BinaryMember(1)] public readonly int LastSequence;
            [BinaryMember(2)] public readonly Entry[] Events;

            public Payload(int version, int lastSequence, Entry[] events)
            {
                Version = version;
                LastSequence = lastSequence;
                Events = events;
            }
        }

        public readonly struct Entry
        {
            [BinaryMember(0)] public readonly int Sequence;
            [BinaryMember(1)] public readonly PassiveSkillTriggerEventArgs Args;

            public Entry(int sequence, in PassiveSkillTriggerEventArgs args)
            {
                Sequence = sequence;
                Args = args;
            }
        }
    }
}
