using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Share
{
    /// <summary>
    /// 快照帧数据
    /// 包含帧同步所需的所有数据
    /// </summary>
    public readonly struct FrameSnapshotData
    {
        /// <summary>
        /// 帧索引
        /// </summary>
        public int FrameIndex { get; }

        /// <summary>
        /// 快照时间戳
        /// </summary>
        public double Timestamp { get; }

        /// <summary>
        /// 快照类型
        /// </summary>
        public SnapshotType Type { get; }

        /// <summary>
        /// 进入游戏数据
        /// </summary>
        public EnterGameData EnterGame { get; }

        /// <summary>
        /// 角色变换数据列表
        /// </summary>
        public IReadOnlyList<ActorTransformData> ActorTransforms { get; }

        /// <summary>
        /// 弹道事件数据列表
        /// </summary>
        public IReadOnlyList<ProjectileEventData> ProjectileEvents { get; }

        /// <summary>
        /// 区域事件数据列表
        /// </summary>
        public IReadOnlyList<AreaEventData> AreaEvents { get; }

        /// <summary>
        /// 伤害事件数据列表
        /// </summary>
        public IReadOnlyList<DamageEventData> DamageEvents { get; }

        /// <summary>
        /// 状态哈希数据
        /// </summary>
        public StateHashData StateHash { get; }

        /// <summary>
        /// 角色生成数据列表
        /// </summary>
        public IReadOnlyList<ActorSpawnData> ActorSpawns { get; }

        /// <summary>
        /// 表现 Cue 数据列表
        /// </summary>
        public IReadOnlyList<PresentationCueData> PresentationCues { get; }

        public FrameSnapshotData(
            int frameIndex,
            double timestamp,
            SnapshotType type,
            EnterGameData enterGame = default,
            IReadOnlyList<ActorTransformData> actorTransforms = null,
            IReadOnlyList<ProjectileEventData> projectileEvents = null,
            IReadOnlyList<AreaEventData> areaEvents = null,
            IReadOnlyList<DamageEventData> damageEvents = null,
            StateHashData stateHash = default,
            IReadOnlyList<ActorSpawnData> actorSpawns = null,
            IReadOnlyList<PresentationCueData> presentationCues = null)
        {
            FrameIndex = frameIndex;
            Timestamp = timestamp;
            Type = type;
            EnterGame = enterGame;
            ActorTransforms = actorTransforms ?? Array.Empty<ActorTransformData>();
            ProjectileEvents = projectileEvents ?? Array.Empty<ProjectileEventData>();
            AreaEvents = areaEvents ?? Array.Empty<AreaEventData>();
            DamageEvents = damageEvents ?? Array.Empty<DamageEventData>();
            StateHash = stateHash;
            ActorSpawns = actorSpawns ?? Array.Empty<ActorSpawnData>();
            PresentationCues = presentationCues ?? Array.Empty<PresentationCueData>();
        }
    }

    /// <summary>
    /// 快照类型
    /// </summary>
    public enum SnapshotType
    {
        /// <summary>
        /// 全量快照
        /// </summary>
        Full = 0,

        /// <summary>
        /// 增量快照
        /// </summary>
        Delta = 1,

        /// <summary>
        /// 关键帧快照
        /// </summary>
        KeyFrame = 2,
    }

    /// <summary>
    /// 进入游戏数据
    /// </summary>
    public readonly struct EnterGameData
    {
        public bool HasValue { get; }
        public int MapId { get; }
        public int LocalPlayerId { get; }
        public IReadOnlyList<int> PlayerIds { get; }
        public IReadOnlyList<TeamData> Teams { get; }

        public EnterGameData(int mapId, int localPlayerId, IReadOnlyList<int> playerIds, IReadOnlyList<TeamData> teams)
        {
            MapId = mapId;
            LocalPlayerId = localPlayerId;
            PlayerIds = playerIds;
            Teams = teams;
            HasValue = true;
        }

        public static readonly EnterGameData Default = default;
    }

    /// <summary>
    /// 队伍数据
    /// </summary>
    public readonly struct TeamData
    {
        public int TeamId { get; }
        public IReadOnlyList<int> PlayerIds { get; }

        public TeamData(int teamId, IReadOnlyList<int> playerIds)
        {
            TeamId = teamId;
            PlayerIds = playerIds;
        }
    }

    /// <summary>
    /// 角色变换数据
    /// </summary>
    public readonly struct ActorTransformData
    {
        public int ActorId { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float PositionZ { get; }
        public float RotationY { get; }
        public float Scale { get; }

        public ActorTransformData(int actorId, float x, float y, float z, float rotationY, float scale)
        {
            ActorId = actorId;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            RotationY = rotationY;
            Scale = scale;
        }
    }

    /// <summary>
    /// 弹道事件数据
    /// </summary>
    public readonly struct ProjectileEventData
    {
        public int ProjectileId { get; }
        public int OwnerId { get; }
        public ProjectileEventKind Kind { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float PositionZ { get; }
        public int TargetId { get; }
        public float StartPosX { get; }
        public float StartPosY { get; }
        public float StartPosZ { get; }

        public ProjectileEventData(
            int projectileId, int ownerId, ProjectileEventKind kind,
            float x, float y, float z, int targetId,
            float startX, float startY, float startZ)
        {
            ProjectileId = projectileId;
            OwnerId = ownerId;
            Kind = kind;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            TargetId = targetId;
            StartPosX = startX;
            StartPosY = startY;
            StartPosZ = startZ;
        }
    }

    /// <summary>
    /// 弹道事件类型
    /// </summary>
    public enum ProjectileEventKind
    {
        Spawn = 0,
        Hit = 1,
        Destroy = 2,
    }

    /// <summary>
    /// 区域事件数据
    /// </summary>
    public readonly struct AreaEventData
    {
        public int AreaId { get; }
        public AreaEventKind Kind { get; }
        public float CenterX { get; }
        public float CenterY { get; }
        public float CenterZ { get; }
        public float Radius { get; }

        public AreaEventData(int areaId, AreaEventKind kind, float x, float y, float z, float radius)
        {
            AreaId = areaId;
            Kind = kind;
            CenterX = x;
            CenterY = y;
            CenterZ = z;
            Radius = radius;
        }
    }

    /// <summary>
    /// 区域事件类型
    /// </summary>
    public enum AreaEventKind
    {
        Appear = 0,
        Disappear = 1,
        Tick = 2,
    }

    /// <summary>
    /// 伤害事件数据
    /// </summary>
    public readonly struct DamageEventData
    {
        public int AttackerId { get; }
        public int TargetId { get; }
        public int SourceId { get; }
        public int DamageType { get; }
        public int DamageValue { get; }
        public int TargetHpAfter { get; }
        public bool IsKill { get; }

        public DamageEventData(
            int attackerId, int targetId, int sourceId,
            int damageType, int damageValue, int targetHpAfter, bool isKill)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            SourceId = sourceId;
            DamageType = damageType;
            DamageValue = damageValue;
            TargetHpAfter = targetHpAfter;
            IsKill = isKill;
        }
    }

    /// <summary>
    /// 表现 Cue 数据
    /// </summary>
    public readonly struct PresentationCueData
    {
        public PresentationCueStage Stage { get; }
        public string CueKind { get; }
        public string CueVfxId { get; }
        public string CueSfxId { get; }
        public int TemplateId { get; }
        public int VfxId { get; }
        public int SfxId { get; }
        public string RequestKey { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public int TriggerEventId { get; }
        public string TriggerEventName { get; }
        public int TriggerId { get; }
        public int Phase { get; }
        public int Priority { get; }
        public int Order { get; }
        public int ActionIndex { get; }
        public int InterruptReason { get; }
        public string InterruptSourceName { get; }
        public int InterruptTriggerId { get; }
        public bool InterruptConditionPassed { get; }
        public IReadOnlyList<int> Targets { get; }
        public IReadOnlyList<SnapshotVec3> Positions { get; }
        public float OffsetX { get; }
        public float OffsetY { get; }
        public float OffsetZ { get; }
        public int DurationMsOverride { get; }
        public float Scale { get; }
        public float ColorR { get; }
        public float ColorG { get; }
        public float ColorB { get; }
        public float ColorA { get; }
        public string OwnerKind { get; }
        public long InstanceId { get; }
        public string InstanceKey { get; }
        public int StackCount { get; }
        public int MaxStackCount { get; }
        public float ElapsedSeconds { get; }
        public float RemainingSeconds { get; }
        public int LifecycleReason { get; }
        public int ContextKind { get; }
        public int OriginKind { get; }
        public long SourceContextId { get; }
        public long RootContextId { get; }
        public long OwnerContextId { get; }
        public int SourceConfigId { get; }
        public string ContextEventId { get; }
        public IReadOnlyList<int> NumericParamKeys { get; }
        public IReadOnlyList<float> NumericParamValues { get; }
        public IReadOnlyList<string> StringParamKeys { get; }
        public IReadOnlyList<string> StringParamValues { get; }

        public PresentationCueData(
            PresentationCueStage stage,
            string cueKind,
            string cueVfxId,
            string cueSfxId,
            int templateId,
            int vfxId,
            int sfxId,
            string requestKey,
            int sourceActorId,
            int targetActorId,
            int triggerEventId,
            string triggerEventName,
            int triggerId,
            int phase,
            int priority,
            int order,
            int actionIndex,
            int interruptReason,
            string interruptSourceName,
            int interruptTriggerId,
            bool interruptConditionPassed,
            IReadOnlyList<int> targets,
            IReadOnlyList<SnapshotVec3> positions,
            float offsetX,
            float offsetY,
            float offsetZ,
            int durationMsOverride,
            float scale,
            float colorR,
            float colorG,
            float colorB,
            float colorA,
            string ownerKind = null,
            long instanceId = 0,
            string instanceKey = null,
            int stackCount = 0,
            int maxStackCount = 0,
            float elapsedSeconds = 0f,
            float remainingSeconds = 0f,
            int lifecycleReason = 0,
            int contextKind = 0,
            int originKind = 0,
            long sourceContextId = 0,
            long rootContextId = 0,
            long ownerContextId = 0,
            int sourceConfigId = 0,
            string contextEventId = null,
            IReadOnlyList<int> numericParamKeys = null,
            IReadOnlyList<float> numericParamValues = null,
            IReadOnlyList<string> stringParamKeys = null,
            IReadOnlyList<string> stringParamValues = null)
        {
            Stage = stage;
            CueKind = cueKind;
            CueVfxId = cueVfxId;
            CueSfxId = cueSfxId;
            TemplateId = templateId;
            VfxId = vfxId;
            SfxId = sfxId;
            RequestKey = requestKey;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            TriggerEventId = triggerEventId;
            TriggerEventName = triggerEventName;
            TriggerId = triggerId;
            Phase = phase;
            Priority = priority;
            Order = order;
            ActionIndex = actionIndex;
            InterruptReason = interruptReason;
            InterruptSourceName = interruptSourceName;
            InterruptTriggerId = interruptTriggerId;
            InterruptConditionPassed = interruptConditionPassed;
            Targets = targets ?? Array.Empty<int>();
            Positions = positions ?? Array.Empty<SnapshotVec3>();
            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;
            DurationMsOverride = durationMsOverride;
            Scale = scale;
            ColorR = colorR;
            ColorG = colorG;
            ColorB = colorB;
            ColorA = colorA;
            OwnerKind = ownerKind;
            InstanceId = instanceId;
            InstanceKey = instanceKey;
            StackCount = stackCount;
            MaxStackCount = maxStackCount;
            ElapsedSeconds = elapsedSeconds;
            RemainingSeconds = remainingSeconds;
            LifecycleReason = lifecycleReason;
            ContextKind = contextKind;
            OriginKind = originKind;
            SourceContextId = sourceContextId;
            RootContextId = rootContextId;
            OwnerContextId = ownerContextId;
            SourceConfigId = sourceConfigId;
            ContextEventId = contextEventId;
            NumericParamKeys = numericParamKeys ?? Array.Empty<int>();
            NumericParamValues = numericParamValues ?? Array.Empty<float>();
            StringParamKeys = stringParamKeys ?? Array.Empty<string>();
            StringParamValues = stringParamValues ?? Array.Empty<string>();
        }
    }

    public enum PresentationCueStage
    {
        None = 0,
        ConditionPassed = 1,
        ConditionFailed = 2,
        BeforeAction = 3,
        Executed = 4,
        Interrupted = 5,
        Skipped = 6,
        Started = 20,
        Ticked = 21,
        Refreshed = 22,
        StackChanged = 23,
        Expired = 24,
        Removed = 25,
        Completed = 26,
    }

    public readonly struct SnapshotVec3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public SnapshotVec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    /// <summary>
    /// 状态哈希数据
    /// </summary>
    public readonly struct StateHashData
    {
        public bool HasValue { get; }
        public int FrameIndex { get; }
        public uint StateHash { get; }

        public StateHashData(int frameIndex, uint stateHash)
        {
            FrameIndex = frameIndex;
            StateHash = stateHash;
            HasValue = true;
        }

        public static readonly StateHashData Default = default;
    }

    /// <summary>
    /// 角色生成数据
    /// </summary>
    public readonly struct ActorSpawnData
    {
        /// <summary>
        /// 运行时自增的 ActorId（唯一标识，从1开始）
        /// </summary>
        public int ActorId { get; }

        /// <summary>
        /// 配置表的模板ID（用于表现层读取配置）
        /// </summary>
        public int EntityCode { get; }

        public int CharacterId { get; }
        public string Name { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float PositionZ { get; }
        public float RotationY { get; }
        public float Scale { get; }
        public int TeamId { get; }
        public float MaxHp { get; }
        public float Hp { get; }

        /// <summary>
        /// 玩家 ID（字符串，用于与 moba.core 中的 PlayerId 一致）
        /// 用于输入命令路由到正确的玩家
        /// </summary>
        public string PlayerId { get; }

        public ActorSpawnData(
            int actorId, int entityCode, int characterId, string name,
            float x, float y, float z,
            float rotationY, float scale,
            int teamId, float maxHp, float hp,
            string playerId = null)
        {
            ActorId = actorId;
            EntityCode = entityCode;
            CharacterId = characterId;
            Name = name;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            RotationY = rotationY;
            Scale = scale;
            TeamId = teamId;
            MaxHp = maxHp;
            Hp = hp;
            PlayerId = playerId ?? actorId.ToString();
        }
    }
}
