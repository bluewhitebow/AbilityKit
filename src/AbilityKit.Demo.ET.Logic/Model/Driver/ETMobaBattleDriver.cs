using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba.StateSync;
using ET.AbilityKit.Demo.ET.Share;
using ActorKind = ET.AbilityKit.Demo.ET.Share.ActorKind;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba;

namespace ET.Logic
{
    /// <summary>
    /// ET version of moba.core battle driver (Pure Data Component)
    ///
    /// Responsibilities:
    /// - Integrate AbilityKit.Host.Extension framework
    /// - Manage snapshot dispatching
    /// - Host World for moba.core services
    ///
    /// Architecture:
    /// - Component: 本文件，存储数据
    /// - Handlers/: 输入、快照、生命周期处理器（基于 Attribute 自动发现）
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETMobaBattleDriver : Entity, IAwake, IUpdate, IDestroy, IBattleDriver
    {
        // ============== IBattleDriver Implementation ==============

        public int CurrentFrame { get; set; }
        public double LogicTimeSeconds { get; set; }
        public int TickRate { get; set; } = 30;
        public bool IsRunning { get; set; }
        public IBattleViewEventSink ViewEventSink { get; set; }
        public BattleStartPlan Plan { get; set; }

        // ============== Core (World Management) ==============

        public IWorldManager WorldManager { get; set; }
        public HostRuntime HostRuntime { get; set; }
        public IWorld World { get; set; }

        // ============== View Sink (for ETBridge) ==============

        private IBattleViewEventSink _viewSink;
        public IBattleViewEventSink ViewSink
        {
            get => _viewSink;
            set => _viewSink = value ?? throw new ArgumentNullException(nameof(ViewSink));
        }

        // ============== Config Loader ==============

        public ITextAssetLoader TextAssetLoader { get; set; }
        public ETConfigLoaderService ConfigLoader { get; set; }

        // ============== Player Spawn Data ==============

        public List<ETPlayerSpawnData> PlayerSpawnData { get; set; } = new List<ETPlayerSpawnData>();

        // ============== Snapshot Dispatcher ==============

        public FrameSnapshotDispatcher SnapshotDispatcher { get; set; }

        // ============== Entity Registry (moba.core integration) ==============

        /// <summary>
        /// ActorId -> ETUnit 映射（用于 moba.core 实体跟踪）
        /// 由 EnterGameHandler 在创建实体时填充
        /// </summary>
        public Dictionary<int, ETUnit> Units { get; } = new Dictionary<int, ETUnit>();

        // ============== Sync Adapter (for Coordinator) ==============

        public IETBattleSyncAdapter SyncAdapter { get; set; }

        // ============== ET Demo Input Sink ==============

        private IWorldInputSink _inputSink;
        public IWorldInputSink InputSink
        {
            get => _inputSink;
            set => _inputSink = value ?? throw new ArgumentNullException(nameof(InputSink));
        }

        // ============== State ==============

        public double LastTickTime { get; set; }

        // ============== Handler Collections ==============

        /// <summary>
        /// 输入处理器列表
        /// </summary>
        public List<IInputHandler> InputHandlers { get; set; } = new List<IInputHandler>();

        /// <summary>
        /// 快照处理器列表
        /// </summary>
        public List<ISnapshotHandler> SnapshotHandlers { get; set; } = new List<ISnapshotHandler>();

        /// <summary>
        /// 生命周期处理器列表
        /// </summary>
        public List<ILifecycleHandler> LifecycleHandlers { get; set; } = new List<ILifecycleHandler>();

        // ============== IBattleDriver Explicit Implementation ==============

        IBattleViewEventSink IBattleDriver.ViewEventSink
        {
            get => ViewSink;
            set => ViewSink = value;
        }

        // ============== Lifecycle Methods (Empty - Handled by Handlers) ==============

        public void Awake()
        {
            // 注册所有处理器
            HandlerRegistry.RegisterAll(this);
            Log.Info($"[ETMobaBattleDriver] Awake: InputHandlers={InputHandlers?.Count ?? 0}, SnapshotHandlers={SnapshotHandlers?.Count ?? 0}, LifecycleHandlers={LifecycleHandlers?.Count ?? 0}");
        }

        public void Update(ETMobaBattleDriver self)
        {
        }

        public void OnDestroy(ETMobaBattleDriver self)
        {
        }

        // ============== IBattleDriver Methods ==============

        public void Initialize(in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            // 触发所有 Lifecycle 处理器
            foreach (var handler in LifecycleHandlers)
            {
                if (handler is InitializeHandler initializeHandler)
                {
                    initializeHandler.Handle(this, plan, viewSink);
                }
            }
        }

        public void Start()
        {
            Log.Debug($"[ETMobaBattleDriver] Start called, LifecycleHandlers.Count={LifecycleHandlers?.Count ?? -1}");

            // 触发所有 Lifecycle 处理器
            if (LifecycleHandlers == null || LifecycleHandlers.Count == 0)
            {
                Log.Warning("[ETMobaBattleDriver] No LifecycleHandlers registered!");
                return;
            }

            foreach (var handler in LifecycleHandlers)
            {
                if (handler is StartHandler startHandler)
                {
                    Log.Debug($"[ETMobaBattleDriver] Calling StartHandler...");
                    startHandler.Handle(this);
                }
            }

            Log.Info($"[ETMobaBattleDriver] Start complete, IsRunning={IsRunning}");
        }

        public void Stop()
        {
            foreach (var handler in LifecycleHandlers)
            {
                if (handler is StopHandler stopHandler)
                {
                    stopHandler.Handle(this);
                }
            }
        }

        public void Destroy()
        {
            foreach (var handler in LifecycleHandlers)
            {
                if (handler is DestroyHandler destroyHandler)
                {
                    destroyHandler.Handle(this);
                }
            }

            // 清理处理器
            InputHandlers?.Clear();
            SnapshotHandlers?.Clear();
            LifecycleHandlers?.Clear();
        }

        public void Tick(float deltaTime)
        {
            foreach (var handler in LifecycleHandlers)
            {
                if (handler is TickHandler tickHandler)
                {
                    tickHandler.Handle(this, deltaTime);
                }
            }
        }

        // ============== Input Submission ==============

        public void SubmitInputs(int frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (World == null || !IsRunning)
                return;

            if (inputs == null || inputs.Count == 0)
                return;

            foreach (var input in inputs)
            {
                RouteInput(frame, input);
            }
        }

        private void RouteInput(int frame, PlayerInputCommand input)
        {
            foreach (var handler in InputHandlers)
            {
                if (handler.CanHandle(input.OpCode))
                {
                    handler.Handle(this, frame, input);
                    return;
                }
            }

            Log.Debug($"[ETMobaBattleDriver] No handler for OpCode: {input.OpCode}");
        }

        public void SubmitMoveInput(int actorId, float targetX, float targetZ)
        {
            foreach (var handler in InputHandlers)
            {
                if (handler is MoveInputHandler moveHandler)
                {
                    moveHandler.Submit(this, actorId, targetX, targetZ);
                    return;
                }
            }
        }

        public bool SubmitSkillInput(int actorId, int slot, float targetX, float targetZ)
        {
            foreach (var handler in InputHandlers)
            {
                if (handler is SkillInputHandler skillHandler)
                {
                    return skillHandler.Submit(this, actorId, slot, targetX, targetZ);
                }
            }
            return false;
        }

        public void SubmitStopInput(int actorId)
        {
            foreach (var handler in InputHandlers)
            {
                if (handler is StopInputHandler stopHandler)
                {
                    stopHandler.Submit(this, actorId);
                    return;
                }
            }
        }

        // ============== Snapshot Handling ==============

        public void HandleSnapshot(in FrameSnapshotData snapshot)
        {
            foreach (var handler in SnapshotHandlers)
            {
                if (handler.CanHandle(in snapshot))
                {
                    handler.Handle(this, in snapshot);
                }
            }
        }

        // ============== Placeholder methods ==============

        public void CreateActor(int actorId, int characterId, int teamId, float x, float y, float z) { }
        public ActorTransformData? GetActorTransform(int actorId) => null;
        public IReadOnlyList<ActorTransformData> GetAllActorTransforms() => null;
        public IReadOnlyList<int> GetAliveActorIds() => null;
        public float GetActorAttribute(int actorId, ActorAttributeType attributeType) => 0;
        public void SetActorAttribute(int actorId, ActorAttributeType attributeType, float value) { }
        public float ModifyActorAttribute(int actorId, ActorAttributeType attributeType, float delta) => 0;
        public bool IsActorDead(int actorId) => false;
        public void MarkActorDead(int actorId, int killerId) { }
        public void MoveActor(int actorId, float targetX, float targetZ) { }
        public bool CanCastSkill(int actorId, int slot) => false;
        public bool CastSkill(int actorId, int slot, float targetX, float targetZ) => false;
        public bool CastSkillOnTarget(int actorId, int slot, int targetActorId) => false;
        public float GetSkillCooldown(int actorId, int slot) => 0;
        public bool IsSkillReady(int actorId, int slot) => false;
        public int AddBuff(int actorId, int casterId, int buffId) => -1;
        public void RemoveBuff(int actorId, int buffInstanceId) { }
        public int GetBuffStack(int actorId, int buffId) => 0;
        public IReadOnlyList<int> FindActorsInRange(float x, float z, float radius, int teamFilter = -1) => null;
        public int FindNearestActor(float x, float z, float radius, int teamFilter = -1) => -1;
        public float ApplyDamage(int attackerId, int targetId, float damage, int damageType) => 0;
        public float ApplyHeal(int healerId, int targetId, float heal) => 0;

        // ============== Service Resolution ==============

        public bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            if (World?.Services != null)
            {
                return World.Services.TryResolve(out service);
            }
            return false;
        }

        // ============== Additional Methods ==============

        public void StartBattle()
        {
            Start();
        }

        public void StopBattle()
        {
            Stop();
        }

        public void OnAllPlayersReady(List<ETPlayerSpawnData> players)
        {
            PlayerSpawnData.Clear();
            if (players != null)
            {
                PlayerSpawnData.AddRange(players);
            }

            Log.Info($"[ETMobaBattleDriver] ========== OnAllPlayersReady ==========");
            Log.Info($"[ETMobaBattleDriver] Player count: {PlayerSpawnData.Count}");

            TriggerEnterGameSnapshot();
            ViewSink?.OnBattleStart(0);

            // 初始化 AutoTest 组件（在实体创建之后）
            InitializeAutoTest();

            Log.Info($"[ETMobaBattleDriver] ========== All players ready! ==========");
        }

        /// <summary>
        /// 初始化 AutoTest 组件，传入正确的 ActorId
        /// </summary>
        private void InitializeAutoTest()
        {
            var self = this;
            var scene = self.Scene();
            if (scene == null)
            {
                Log.Warning("[ETMobaBattleDriver] Cannot initialize AutoTest: Scene is null");
                return;
            }

            var autoTest = scene.GetComponent<ETBattleAutoTestComponent>();
            if (autoTest == null)
            {
                Log.Warning("[ETMobaBattleDriver] Cannot initialize AutoTest: ETBattleAutoTestComponent not found");
                return;
            }

            var unitComponent = scene.GetComponent<ETUnitComponent>();
            if (unitComponent == null)
            {
                Log.Warning("[ETMobaBattleDriver] Cannot initialize AutoTest: ETUnitComponent not found");
                return;
            }

            // 获取本地玩家的 ActorId（第一个玩家）
            if (PlayerSpawnData.Count == 0)
            {
                Log.Warning("[ETMobaBattleDriver] Cannot initialize AutoTest: No player spawn data");
                return;
            }

            var localPlayer = PlayerSpawnData[0];
            if (!unitComponent.Units.TryGetValue(localPlayer.ActorId, out var unit))
            {
                Log.Warning($"[ETMobaBattleDriver] Cannot initialize AutoTest: Unit not found for ActorId={localPlayer.ActorId}");
                return;
            }

            // 初始化 AutoTest
            autoTest.Initialize(localPlayer.ActorId, unit.X, unit.Y);
            Log.Info($"[ETMobaBattleDriver] AutoTest initialized: ActorId={localPlayer.ActorId}, StartPos=({unit.X}, {unit.Y})");
        }

        /// <summary>
        /// 触发进入游戏快照
        ///
        /// 设计说明（单一入口原则）：
        /// - ET.Logic 层通过 MobaEnterGameFlowService 与 moba.core 交互
        /// - 不直接创建 moba.core 实体，只负责 ET 表现层数据
        /// - 实体创建由 moba.core 的 ActorSpawnPipeline 处理
        /// </summary>
        private void TriggerEnterGameSnapshot()
        {
            Log.Info($"[ETMobaBattleDriver] >>> TriggerEnterGameSnapshot called");

            try
            {
                var self = this;
                var scene = self.Scene();
                if (scene == null)
                {
                    Log.Warning($"[ETMobaBattleDriver] Scene is null, cannot spawn entities");
                    return;
                }

                var unitComponent = scene.GetComponent<ETUnitComponent>();
                if (unitComponent == null)
                {
                    Log.Warning($"[ETMobaBattleDriver] ETUnitComponent not found");
                    return;
                }

                // ========== 步骤 1: 通过 moba.core 服务创建实体 ==========

                // 构建 MobaGameStartSpec 并委托给 MobaEnterGameFlowService
                if (!TryResolve<MobaEnterGameFlowService>(out var enterGameService) || enterGameService == null)
                {
                    Log.Error($"[ETMobaBattleDriver] MobaEnterGameFlowService not found");
                    return;
                }

                if (!TryResolve<global::Entitas.IContexts>(out var contexts) || contexts == null)
                {
                    Log.Error($"[ETMobaBattleDriver] Entitas IContexts not found");
                    return;
                }
                var actorContext = ((global::Contexts)contexts).actor;

                // 构建 EnterMobaGameReq
                var loadouts = new MobaPlayerLoadout[PlayerSpawnData.Count];
                for (int i = 0; i < PlayerSpawnData.Count; i++)
                {
                    var spawnData = PlayerSpawnData[i];
                    var playerId = new PlayerId(spawnData.PlayerId);

                    loadouts[i] = new MobaPlayerLoadout(
                        playerId: playerId,
                        teamId: spawnData.TeamId,
                        heroId: spawnData.CharacterId,
                        attributeTemplateId: 0,
                        level: 1,
                        basicAttackSkillId: 0,
                        skillIds: null,
                        spawnIndex: i,
                        unitSubType: (int)UnitSubType.Hero,
                        mainType: (int)EntityMainType.Unit,
                        hasSpawnPosition: 1,
                        spawnX: spawnData.PositionX,
                        spawnY: 0f,
                        spawnZ: spawnData.PositionZ);
                }

                var localPlayerId = PlayerSpawnData.Count > 0 ? new PlayerId(PlayerSpawnData[0].PlayerId) : default;
                var enterReq = new EnterMobaGameReq(
                    playerId: localPlayerId,
                    matchId: $"et_demo_{Environment.TickCount}",
                    mapId: 1,
                    randomSeed: Environment.TickCount,
                    tickRate: 30,
                    inputDelayFrames: 0,
                    players: loadouts);

                var spec = new MobaGameStartSpec(in enterReq);

                // 收集 spawn 结果（通过回调）
                var spawns = new List<ActorSpawnData>();
                var localActorId = 0;
                float localX = 0f, localY = 0f;

                // 设置 MobaActorSpawnSnapshotService 回调来收集 spawn 数据
                // 注意：实际 spawn 事件会通过 ViewSink.OnEnterGameSnapshot 传递
                // 这里需要等 MobaEnterGameFlowService 执行完成后，moba.core 会发布 spawn 快照

                // 执行进入游戏流程（创建实体）
                enterGameService.ApplyGameStartSpec(actorContext, in spec);

                Log.Info($"[ETMobaBattleDriver] MobaEnterGameFlowService.ApplyGameStartSpec completed");

                // ========== 步骤 2: 收集 moba.core 创建的实体信息 ==========

                // 从 MobaActorRegistry 获取创建的实体
                if (TryResolve<MobaActorRegistry>(out var registry) && registry != null)
                {
                    // 获取本地玩家的 ActorId
                    if (TryResolve<MobaPlayerActorMapService>(out var playerActorMap) && playerActorMap != null)
                    {
                        if (playerActorMap.TryGetActorId(localPlayerId, out var actorId))
                        {
                            localActorId = actorId;

                            // 从 registry 获取实体位置
                            if (registry.TryGet(actorId, out var entity) && entity != null && entity.hasTransform)
                            {
                                var pos = entity.transform.Value.Position;
                                localX = pos.X;
                                localY = pos.Z;
                            }
                        }
                    }
                }

                // ========== 步骤 3: 创建 ET 表现层单位 ==========

                foreach (var spawnData in PlayerSpawnData)
                {
                    // 查找对应的 ActorId
                    var playerId = new PlayerId(spawnData.PlayerId);
                    int actorId = 0;

                    if (TryResolve<MobaPlayerActorMapService>(out var map) && map != null)
                    {
                        map.TryGetActorId(playerId, out actorId);
                    }

                    if (actorId == 0)
                    {
                        Log.Warning($"[ETMobaBattleDriver] No ActorId for PlayerId={spawnData.PlayerId}");
                        continue;
                    }

                    // 创建 ETUnit（用于 ET 视图层）
                    var unit = unitComponent.CreateUnit(
                        actorId: actorId,
                        entityCode: spawnData.CharacterId,
                        kind: spawnData.CharacterId == 1 ? ActorKind.Hero : ActorKind.Monster,
                        name: spawnData.CharacterName,
                        x: spawnData.PositionX,
                        y: spawnData.PositionZ,
                        maxHp: spawnData.MaxHp);

                    if (unit != null)
                    {
                        unitComponent.Units[actorId] = unit;
                        Log.Info($"[ETMobaBattleDriver] Created ETUnit: ActorId={actorId}, Name={spawnData.CharacterName}");

                        // 添加到 spawns 列表
                        spawns.Add(new ActorSpawnData(
                            actorId: actorId,
                            entityCode: spawnData.CharacterId,
                            characterId: spawnData.CharacterId,
                            name: spawnData.CharacterName,
                            x: spawnData.PositionX,
                            y: spawnData.PositionZ,
                            z: 0f,
                            rotationY: 0f,
                            scale: 1f,
                            teamId: spawnData.TeamId,
                            maxHp: spawnData.MaxHp,
                            hp: spawnData.Hp));
                    }
                }

                // ========== 步骤 4: 发送 EnterGameSnapshot 到视图层 ==========

                if (spawns.Count > 0 && ViewSink != null)
                {
                    // 构建 PlayerIds 和 Teams 列表
                    var playerIds = new List<int>(PlayerSpawnData.Count);
                    var teamDict = new Dictionary<int, List<int>>();

                    foreach (var spawn in spawns)
                    {
                        playerIds.Add(spawn.ActorId);

                        if (!teamDict.TryGetValue(spawn.TeamId, out var list))
                        {
                            list = new List<int>();
                            teamDict[spawn.TeamId] = list;
                        }
                        list.Add(spawn.ActorId);
                    }

                    var teams = new List<TeamData>();
                    foreach (var kv in teamDict)
                    {
                        teams.Add(new TeamData(kv.Key, kv.Value));
                    }

                    var enterGameData = new EnterGameData(
                        mapId: 1,
                        localPlayerId: localActorId,
                        playerIds: playerIds,
                        teams: teams);

                    var enterGameSnapshot = new FrameSnapshotData(
                        frameIndex: 0,
                        timestamp: 0,
                        type: SnapshotType.Full,
                        enterGame: enterGameData,
                        actorSpawns: spawns);

                    ViewSink.OnEnterGameSnapshot(in enterGameSnapshot);
                    Log.Info($"[ETMobaBattleDriver] >>> TriggerEnterGameSnapshot success, spawned {spawns.Count} entities");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaBattleDriver] >>> TriggerEnterGameSnapshot failed: {ex}");
            }
        }
    }
}
