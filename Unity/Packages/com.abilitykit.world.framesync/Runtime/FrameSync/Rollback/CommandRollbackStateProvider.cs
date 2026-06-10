using System;
using AbilityKit.Ability.FrameSync;

namespace AbilityKit.Ability.FrameSync.Rollback
{
    public sealed class CommandRollbackStateProvider : IRollbackStateProvider
    {
        public const int DefaultKey = 900001;

        private static readonly byte[] s_emptyPayload = Array.Empty<byte>();
        private readonly CommandRollbackLog _log;

        public CommandRollbackStateProvider(CommandRollbackLog log, int key = DefaultKey)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            Key = key;
        }

        public int Key { get; }

        public byte[] Export(FrameIndex frame)
        {
            return s_emptyPayload;
        }

        public void Import(FrameIndex frame, byte[] payload)
        {
            _log.RollbackAfter(frame);
        }
    }
}
