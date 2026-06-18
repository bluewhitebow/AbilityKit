using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaTraceMetadata : TraceMetadata
    {
        public int Kind { get; set; }
        public MobaTraceKind TraceKind { get; set; }
        public int ConfigId { get; set; }
        public long RootId { get; set; }
        public long ParentId { get; set; }
        public long SourceActorId { get; set; }
        public long TargetActorId { get; set; }
        public long SourceId { get; set; }
        public long TargetId { get; set; }
        public long OriginSourceId { get; set; }
        public long OriginTargetId { get; set; }
        public string OriginSource { get; set; }
        public string OriginTarget { get; set; }
        public string Message { get; set; }

        public string ToDisplayString()
        {
            return $"{TraceKind}(root={RootId}, config={ConfigId}, source={SourceActorId}, target={TargetActorId}, origin={OriginSource}, targetOrigin={OriginTarget})";
        }

        public bool IsEmpty => RootId <= 0 && SourceActorId <= 0 && TargetActorId <= 0 && ConfigId <= 0 && string.IsNullOrEmpty(OriginSource) && string.IsNullOrEmpty(OriginTarget);
    }
}
