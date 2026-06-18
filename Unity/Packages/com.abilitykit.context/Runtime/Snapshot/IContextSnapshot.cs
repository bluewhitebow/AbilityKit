namespace AbilityKit.Context
{
    /// <summary>
    /// 快照接口
    /// 用于持久化存储实体的瞬时状态
    /// </summary>
    public interface IContextSnapshot
    {
        long EntityId { get; }
        long CreatedAtMs { get; }
    }

    public interface IVersionedContextSnapshot : IContextSnapshot
    {
        long Version { get; }
        int Frame { get; }
    }

    public readonly struct ContextSnapshotRecord
    {
        public readonly IContextSnapshot Snapshot;
        public readonly long Version;
        public readonly int Frame;
        public readonly long SavedAtMs;

        public ContextSnapshotRecord(IContextSnapshot snapshot, long version, int frame, long savedAtMs)
        {
            Snapshot = snapshot;
            Version = version;
            Frame = frame;
            SavedAtMs = savedAtMs;
        }

        public long EntityId => Snapshot?.EntityId ?? 0;
        public bool IsValid => Snapshot != null;
    }
}
