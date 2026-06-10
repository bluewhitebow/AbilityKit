using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;

namespace AbilityKit.Ability.FrameSync.Rollback
{
    public sealed class CommandRollbackLog
    {
        private readonly List<Entry> _entries;
        private int _nextOrder;

        public CommandRollbackLog(int capacity = 64)
        {
            _entries = new List<Entry>(capacity > 0 ? capacity : 64);
        }

        public int Count => _entries.Count;

        public void Record(FrameIndex frame, IRollbackCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            _entries.Add(new Entry(frame, _nextOrder++, command));
        }

        public void Record(FrameIndex frame, Action rollback)
        {
            Record(frame, new DelegateRollbackCommand(rollback));
        }

        public int RollbackAfter(FrameIndex frame)
        {
            return RollbackWhere(entry => entry.Frame.Value > frame.Value);
        }

        public int RollbackFrom(FrameIndex frame)
        {
            return RollbackWhere(entry => entry.Frame.Value >= frame.Value);
        }

        public void TrimBefore(FrameIndex frame)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].Frame.Value < frame.Value)
                {
                    _entries.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            _entries.Clear();
            _nextOrder = 0;
        }

        private int RollbackWhere(Func<Entry, bool> predicate)
        {
            var rolledBack = 0;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                if (!predicate(entry))
                {
                    continue;
                }

                entry.Command.Rollback();
                _entries.RemoveAt(i);
                rolledBack++;
            }

            return rolledBack;
        }

        private readonly struct Entry
        {
            public readonly FrameIndex Frame;
            public readonly int Order;
            public readonly IRollbackCommand Command;

            public Entry(FrameIndex frame, int order, IRollbackCommand command)
            {
                Frame = frame;
                Order = order;
                Command = command;
            }
        }
    }
}
