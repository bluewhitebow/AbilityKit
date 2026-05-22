using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.Worlds.Blueprints;
using EC = AbilityKit.World.ECS;

namespace ET.Logic
{
    /// <summary>
    /// ET version of moba.core battle driver
    /// Uses HostRuntime + WorldManager to host moba.core
    ///
    /// Responsibilities:
    /// - Integrate AbilityKit.Host.Extension framework
    /// - Manage snapshot dispatching
    /// - Host World for moba.core services
    /// </summary>
    public sealed class ETMobaBattleDriver : Entity, IAwake, IUpdate, IDestroy, IBattleDriver
    {
        // ============== Core ==============

        private IWorldManager _worldManager;
        private HostRuntime _hostRuntime;
        private IWorld _world;
        private IBattleViewEventSink _viewSink;

        // ============== Config Loader (for real-time config reading) ==============

        private ITextAssetLoader _textAssetLoader;

        // ============== Player Spawn Data (set when all players ready) ==============

        private List<ETPlayerSpawnData> _playerSpawnData = new();

        // ============== Snapshot Dispatcher ==============

        private FrameSnapshotDispatcher _snapshotDispatcher;

        // ============== State ==============

        private BattleStartPlan _plan;
        private int _currentFrame;
        private double _logicTimeSeconds;
        private int _tickRate = 30;
        private bool _isRunning;
        private double _lastTickTime;

        // ============== IBattleDriver Properties ==============

        public int CurrentFrame => _currentFrame;
        public double LogicTimeSeconds => _logicTimeSeconds;
        public int TickRate => _tickRate;
        public bool IsRunning => _isRunning;
        public IBattleViewEventSink ViewEventSink { get => _viewSink; set => _viewSink = value; }
        public BattleStartPlan Plan => _plan;
        public IWorld World => _world;

        // ============== ET Component Lifecycle ==============

        public void Awake()
        {
            Log.Info("[ETMobaBattleDriver] ETMobaBattleDriver awake");
            _currentFrame = 0;
            _logicTimeSeconds = 0;
            _isRunning = false;
        }

        public void Update(ETMobaBattleDriver self)
        {
            if (!_isRunning)
                return;

            double currentTime = GetCurrentTimeSeconds();
            double deltaTime = currentTime - _lastTickTime;

            if (deltaTime >= (1.0 / _tickRate))
            {
                Tick((float)deltaTime);
                _lastTickTime = currentTime;
            }
        }

        public void OnDestroy(ETMobaBattleDriver self)
        {
            Stop();
            Destroy();
            Log.Info("[ETMobaBattleDriver] Destroyed");
        }

        // ============== IBattleDriver Methods ==============

        /// <summary>
        /// Initialize battle (without config loader - will use default spawn data)
        /// </summary>
        public void Initialize(in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            Initialize(plan, viewSink, null);
        }

        /// <summary>
        /// Initialize battle with config loader
        /// </summary>
        public void Initialize(in BattleStartPlan plan, IBattleViewEventSink viewSink, ITextAssetLoader textAssetLoader)
        {
            _plan = plan;
            _viewSink = viewSink;
            _textAssetLoader = textAssetLoader;
            _tickRate = plan.TickRate > 0 ? plan.TickRate : 30;

            try
            {
                // 1. Create snapshot dispatcher
                _snapshotDispatcher = new FrameSnapshotDispatcher();
                SubscribeSnapshotEvents();

                // 2. Create WorldManager
                var worldTypeRegistry = new WorldTypeRegistry();

                // 3. Register moba.core World factories
                MobaWorldBlueprintsRegistration.RegisterAll(worldTypeRegistry, options => new EntitasWorld(options));

                // 4. Create WorldManager
                _worldManager = new WorldManager(new RegistryWorldFactory(worldTypeRegistry));

                // 5. Create HostRuntime
                var hostOptions = new HostRuntimeOptions();
                _hostRuntime = new HostRuntime(_worldManager, hostOptions);

                // 6. Create World
                int worldIdValue = plan.WorldId > 0 ? plan.WorldId : 1;
                var worldId = new WorldId($"Battle_{worldIdValue}");
                var worldOptions = new WorldCreateOptions(worldId, MobaBattleWorldBlueprint.Type);
                _world = _hostRuntime.CreateWorld(worldOptions);

                Log.Info($"[ETMobaBattleDriver] Initialized: TickRate={_tickRate}, WorldId={_plan.WorldId}, HasConfigLoader={_textAssetLoader != null}");
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaBattleDriver] Initialize failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Subscribe to snapshot events
        /// </summary>
        private void SubscribeSnapshotEvents()
        {
            if (_snapshotDispatcher == null || _viewSink == null)
                return;

            // Subscribe to EnterGameSnapshot (contains initial data including ActorSpawnData)
            _snapshotDispatcher.Subscribe<EnterGameData>((int)MobaOpCode.EnterGameSnapshot, OnEnterGameData);

            // Subscribe to ActorTransformSnapshot
            _snapshotDispatcher.Subscribe<ActorTransformData[]>((int)MobaOpCode.ActorTransformSnapshot, OnActorTransformData);

            // Subscribe to DamageEventSnapshot
            _snapshotDispatcher.Subscribe<DamageEventData[]>((int)MobaOpCode.DamageEventSnapshot, OnDamageEventData);

            Log.Info("[ETMobaBattleDriver] Snapshot events subscribed");
        }

        private void OnEnterGameData(int frame, EnterGameData data)
        {
            // EnterGameData contains all initial data
            var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Full, enterGame: data);
            _viewSink?.OnEnterGameSnapshot(in snapshot);
        }

        private void OnActorTransformData(int frame, ActorTransformData[] data)
        {
            var transformList = new List<ActorTransformData>(data);
            var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Delta, actorTransforms: transformList);
            _viewSink?.OnActorTransformSnapshot(in snapshot);
        }

        private void OnDamageEventData(int frame, DamageEventData[] data)
        {
            var eventList = new List<DamageEventData>(data);
            var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Delta, damageEvents: eventList);
            _viewSink?.OnDamageEventSnapshot(in snapshot);
        }

        /// <summary>
        /// Start battle
        /// </summary>
        public void Start()
        {
            if (_hostRuntime == null)
            {
                Log.Warning("[ETMobaBattleDriver] Cannot start, HostRuntime is null");
                return;
            }

            _isRunning = true;
            _lastTickTime = GetCurrentTimeSeconds();
            _currentFrame = 0;
            _logicTimeSeconds = 0;

            Log.Info("[ETMobaBattleDriver] Battle started (waiting for all players ready...)");
            // ????????? EnterGameSnapshot??? OnAllPlayersReady
        }

        /// <summary>
        /// ??????????????????
        /// ??????
        /// </summary>
        public void OnAllPlayersReady(List<ETPlayerSpawnData> players)
        {
            if (!_isRunning)
            {
                Log.Warning("[ETMobaBattleDriver] Cannot spawn entities, battle not started");
                return;
            }

            // ??????????
            _playerSpawnData.Clear();
            if (players != null)
            {
                _playerSpawnData.AddRange(players);
            }

            Log.Info($"[ETMobaBattleDriver] ========== OnAllPlayersReady ==========");
            Log.Info($"[ETMobaBattleDriver] Player count: {_playerSpawnData.Count}");
            foreach (var p in _playerSpawnData)
            {
                Log.Info($"[ETMobaBattleDriver]   - ActorId={p.ActorId}, Hero={p.CharacterName}, Team={p.TeamId}");
            }

            // ?? EnterGameSnapshot
            TriggerEnterGameSnapshot();

            // ??????
            _viewSink?.OnBattleStart(0);

            Log.Info($"[ETMobaBattleDriver] ========== All players ready! ==========");
        }

        /// <summary>
        /// Trigger enter game snapshot
        /// </summary>
        private void TriggerEnterGameSnapshot()
        {
            if (_snapshotDispatcher == null)
                return;

            Log.Info($"[ETMobaBattleDriver] >>> TriggerEnterGameSnapshot called");

            // Build EnterGameData
            var playerIds = new List<int>(_plan.PlayerId > 0 ? new[] { _plan.PlayerId } : Array.Empty<int>());
            var teams = new List<TeamData>
            {
                new TeamData(1, playerIds)
            };

            var enterGameData = new EnterGameData(_plan.MapId, _plan.PlayerId, playerIds, teams);

            // Build ActorSpawnData from config (instead of hardcoded mock data)
            var spawns = new List<ActorSpawnData>();
            BuildActorSpawnsFromConfig(spawns);

            Log.Info($"[ETMobaBattleDriver] >>> Publishing OnEnterGameSnapshot with {spawns.Count} spawns");

            // Trigger EnterGameSnapshot event (with ActorSpawns)
            var snapshot = new FrameSnapshotData(0, 0, SnapshotType.Full, enterGame: enterGameData, actorSpawns: spawns);
            _viewSink?.OnEnterGameSnapshot(in snapshot);

            Log.Info($"[ETMobaBattleDriver] >>> EnterGameSnapshot published: MapId={enterGameData.MapId}, PlayerId={enterGameData.LocalPlayerId}, SpawnCount={spawns.Count}");
        }

        /// <summary>
        /// Build actor spawn data from pre-set player data
        /// Uses _playerSpawnData if set (from OnAllPlayersReady), otherwise falls back to config loading
        /// </summary>
        private void BuildActorSpawnsFromConfig(List<ActorSpawnData> spawns)
        {
            // ??????????????? OnAllPlayersReady?
            if (_playerSpawnData.Count > 0)
            {
                BuildActorSpawnsFromPlayerList(spawns);
                return;
            }

            // ????????
            if (_textAssetLoader == null)
            {
                Log.Warning("[ETMobaBattleDriver] No player data and no config loader, using default spawn");
                AddDefaultSpawns(spawns);
                return;
            }

            // Real-time load configuration from loader
            var characterConfigs = LoadCharacterConfigs();
            var attributeConfigs = LoadAttributeTemplates();

            if (characterConfigs.Count == 0)
            {
                Log.Warning("[ETMobaBattleDriver] No character configs loaded, using default spawn");
                AddDefaultSpawns(spawns);
                return;
            }

            int actorIdBase = _plan.PlayerId > 0 ? _plan.PlayerId : 1;

            // Local player character (HeroId = 1001)
            if (TryGetCharacterWithId(characterConfigs, 1001, out var heroConfig))
            {
                var attrs = GetAttributeData(heroConfig.AttributeTemplateId, attributeConfigs);
                float hp = attrs != null ? attrs.Hp : 200f;
                float maxHp = attrs != null && attrs.MaxHp > 0 ? attrs.MaxHp : hp;

                spawns.Add(new ActorSpawnData(
                    actorIdBase, heroConfig.Id, heroConfig.Name,
                    0f, 0f, 0f, 0f, 1f,
                    1, hp, maxHp));

                Log.Info($"[ETMobaBattleDriver] Built spawn: ActorId={actorIdBase}, Character={heroConfig.Name} (Id={heroConfig.Id}), Team=1, HP={hp}");
            }

            // Team 1 AI players (HeroId 1002, 1003)
            for (int i = 2; i <= 3; i++)
            {
                int heroId = 1000 + i;
                if (TryGetCharacterWithId(characterConfigs, heroId, out var aiConfig))
                {
                    var attrs = GetAttributeData(aiConfig.AttributeTemplateId, attributeConfigs);
                    float hp = attrs != null ? attrs.Hp : 200f;
                    float maxHp = attrs != null && attrs.MaxHp > 0 ? attrs.MaxHp : hp;
                    int actorId = actorIdBase + i;

                    spawns.Add(new ActorSpawnData(
                        actorId, aiConfig.Id, aiConfig.Name,
                        10f * (i - 1), 0f, 0f, 0f, 1f,
                        1, hp, maxHp));

                    Log.Info($"[ETMobaBattleDriver] Built spawn: ActorId={actorId}, Character={aiConfig.Name} (Id={aiConfig.Id}), Team=1, HP={hp}");
                }
            }

            // Team 2 enemies (HeroId 1001, 1002, 1003)
            for (int i = 1; i <= 3; i++)
            {
                int heroId = 1000 + i;
                if (TryGetCharacterWithId(characterConfigs, heroId, out var enemyConfig))
                {
                    var attrs = GetAttributeData(enemyConfig.AttributeTemplateId, attributeConfigs);
                    float hp = attrs != null ? attrs.Hp : 200f;
                    float maxHp = attrs != null && attrs.MaxHp > 0 ? attrs.MaxHp : hp;
                    int actorId = 2000 + i;

                    spawns.Add(new ActorSpawnData(
                        actorId, enemyConfig.Id, enemyConfig.Name,
                        0f, 0f, 50f + 10f * (i - 1), 0f, 1f,
                        2, hp, maxHp));

                    Log.Info($"[ETMobaBattleDriver] Built spawn: ActorId={actorId}, Character={enemyConfig.Name} (Id={enemyConfig.Id}), Team=2, HP={hp}");
                }
            }
        }

        #region Configuration Loading

        /// <summary>
        /// Character config from JSON
        /// </summary>
        private class JsonCharacter
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int ModelId { get; set; }
            public int AttributeTemplateId { get; set; }
            public List<int> SkillIds { get; set; }
            public List<int> PassiveSkillIds { get; set; }
        }

        /// <summary>
        /// Attribute template config from JSON
        /// </summary>
        private class JsonAttributeTemplate
        {
            public int Id { get; set; }
            public List<int> ActiveSkills { get; set; }
            public List<int> PassiveSkills { get; set; }
            public float Hp { get; set; }
            public float MaxHp { get; set; }
            public float PhysicsAttack { get; set; }
            public float MagicAttack { get; set; }
            public float PhysicsDefense { get; set; }
            public float MagicDefense { get; set; }
            public float MoveSpeed { get; set; }
        }

        private List<JsonCharacter> LoadCharacterConfigs()
        {
            var configs = new List<JsonCharacter>();
            var path = "Configs/moba/characters.json";

            if (_textAssetLoader.TryLoadText(path, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var characters = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonCharacter>>(json);
                    if (characters != null)
                    {
                        foreach (var c in characters)
                        {
                            configs.Add(c);
                        }
                        Log.Info($"[ETMobaBattleDriver] Loaded {configs.Count} character configs");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[ETMobaBattleDriver] Failed to load characters: {ex.Message}");
                }
            }
            else
            {
                Log.Warning($"[ETMobaBattleDriver] Characters file not found: {path}");
            }

            return configs;
        }

        private Dictionary<int, JsonAttributeTemplate> LoadAttributeTemplates()
        {
            var configs = new Dictionary<int, JsonAttributeTemplate>();
            var path = "Configs/moba/attribute_templates.json";

            if (_textAssetLoader.TryLoadText(path, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var templates = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonAttributeTemplate>>(json);
                    if (templates != null)
                    {
                        foreach (var t in templates)
                        {
                            configs[t.Id] = t;
                        }
                        Log.Info($"[ETMobaBattleDriver] Loaded {configs.Count} attribute templates");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[ETMobaBattleDriver] Failed to load attribute templates: {ex.Message}");
                }
            }
            else
            {
                Log.Warning($"[ETMobaBattleDriver] Attribute templates file not found: {path}");
            }

            return configs;
        }

        private bool TryGetCharacterWithId(List<JsonCharacter> configs, int id, out JsonCharacter config)
        {
            foreach (var c in configs)
            {
                if (c.Id == id)
                {
                    config = c;
                    return true;
                }
            }
            config = null;
            return false;
        }

        private JsonAttributeTemplate GetAttributeData(int templateId, Dictionary<int, JsonAttributeTemplate> configs)
        {
            if (configs.TryGetValue(templateId, out var template))
            {
                return template;
            }
            return null;
        }

        #endregion

        /// <summary>
        /// Build actor spawn data from pre-set player list (used when all players are ready)
        /// </summary>
        private void BuildActorSpawnsFromPlayerList(List<ActorSpawnData> spawns)
        {
            foreach (var player in _playerSpawnData)
            {
                // Use player.ActorId directly (can be negative or positive)
                int actorId = player.ActorId;

                float hp = player.Hp > 0 ? player.Hp : 200f;
                float maxHp = player.MaxHp > 0 ? player.MaxHp : hp;

                spawns.Add(new ActorSpawnData(
                    actorId,
                    player.CharacterId,
                    player.CharacterName,
                    player.PositionX,
                    player.PositionY,
                    player.PositionZ,
                    player.RotationY,
                    player.Scale > 0 ? player.Scale : 1f,
                    player.TeamId,
                    hp,
                    maxHp));

                Log.Info($"[ETMobaBattleDriver] Built spawn from player data: ActorId={actorId}, Character={player.CharacterName}, Team={player.TeamId}, HP={hp}");
            }
        }

        /// <summary>
        /// Add default spawn data when config is not available
        /// </summary>
        private void AddDefaultSpawns(List<ActorSpawnData> spawns)
        {
            // Player character
            int playerActorId = _plan.PlayerId > 0 ? _plan.PlayerId : 1;

            spawns.Add(new ActorSpawnData(
                playerActorId, 1001, "Hero_001",
                0f, 0f, 0f, 0f, 1f,
                1, 200f, 200f));

            // Add some default minions
            spawns.Add(new ActorSpawnData(
                2001, 2001, "Enemy_Minion_1",
                10f, 0f, 5f, 0f, 1f,
                2, 80f, 80f));

            spawns.Add(new ActorSpawnData(
                2002, 2001, "Enemy_Minion_2",
                10f, 0f, -5f, 0f, 1f,
                2, 80f, 80f));

            Log.Info($"[ETMobaBattleDriver] Added default spawns (3 entities)");
        }

        /// <summary>
        /// Stop battle
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            Log.Info("[ETMobaBattleDriver] Battle stopped");
        }

        /// <summary>
        /// Destroy
        /// </summary>
        public void Destroy()
        {
            if (_hostRuntime != null && _world != null)
            {
                _hostRuntime.DestroyWorld(_world.Id);
            }

            _world = null;
            _viewSink = null;
            _snapshotDispatcher = null;
        }

        // ============== IBattleDriver Tick ==============

        /// <summary>
        /// Tick
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_hostRuntime == null)
                return;

            _currentFrame++;
            _logicTimeSeconds += deltaTime;

            try
            {
                // Tick HostRuntime (will tick all Worlds)
                _hostRuntime.Tick(deltaTime);

                // Notify frame sync complete
                _viewSink?.OnFrameSyncComplete(_currentFrame);
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaBattleDriver] Tick error at frame {_currentFrame}: {ex.Message}");
            }
        }

        // ============== IBattleDriver Actor Operations ==============

        /// <summary>
        /// Create Actor
        /// </summary>
        public void CreateActor(int actorId, int characterId, int teamId, float x, float y, float z)
        {
            Log.Debug($"[ETMobaBattleDriver] CreateActor: id={actorId}, char={characterId}, team={teamId}, pos=({x},{y},{z})");
        }

        /// <summary>
        /// Get Actor transform data
        /// </summary>
        public ActorTransformData? GetActorTransform(int actorId)
        {
            return null;
        }

        /// <summary>
        /// Get all Actor transform data
        /// </summary>
        public IReadOnlyList<ActorTransformData> GetAllActorTransforms()
        {
            return Array.Empty<ActorTransformData>();
        }

        /// <summary>
        /// Get all alive Actor IDs in range
        /// </summary>
        public IReadOnlyList<int> GetAliveActorIds()
        {
            return Array.Empty<int>();
        }

        /// <summary>
        /// Get Actor attribute
        /// </summary>
        public float GetActorAttribute(int actorId, ActorAttributeType attributeType)
        {
            return 0;
        }

        /// <summary>
        /// Set Actor attribute
        /// </summary>
        public void SetActorAttribute(int actorId, ActorAttributeType attributeType, float value)
        {
        }

        /// <summary>
        /// Modify Actor attribute
        /// </summary>
        public float ModifyActorAttribute(int actorId, ActorAttributeType attributeType, float delta)
        {
            return 0;
        }

        /// <summary>
        /// Check if Actor is dead
        /// </summary>
        public bool IsActorDead(int actorId)
        {
            return false;
        }

        /// <summary>
        /// Mark Actor as dead
        /// </summary>
        public void MarkActorDead(int actorId, int killerId)
        {
        }

        /// <summary>
        /// Move Actor
        /// </summary>
        public void MoveActor(int actorId, float targetX, float targetZ)
        {
            Log.Debug($"[ETMobaBattleDriver] MoveActor: id={actorId}, target=({targetX},{targetZ})");
        }

        // ============== IBattleDriver Skill Operations ==============

        /// <summary>
        /// Check if can cast skill
        /// </summary>
        public bool CanCastSkill(int actorId, int slot)
        {
            return false;
        }

        /// <summary>
        /// Cast skill
        /// </summary>
        public bool CastSkill(int actorId, int slot, float targetX, float targetZ)
        {
            return false;
        }

        /// <summary>
        /// Cast skill on target
        /// </summary>
        public bool CastSkillOnTarget(int actorId, int slot, int targetActorId)
        {
            return false;
        }

        /// <summary>
        /// Get skill cooldown time
        /// </summary>
        public float GetSkillCooldown(int actorId, int slot)
        {
            return 0;
        }

        /// <summary>
        /// Check if skill is ready
        /// </summary>
        public bool IsSkillReady(int actorId, int slot)
        {
            return false;
        }

        // ============== IBattleDriver Buff Operations ==============

        /// <summary>
        /// Add Buff
        /// </summary>
        public int AddBuff(int actorId, int casterId, int buffId)
        {
            return -1;
        }

        /// <summary>
        /// Remove Buff
        /// </summary>
        public void RemoveBuff(int actorId, int buffInstanceId)
        {
        }

        /// <summary>
        /// Get Buff stack count for Actor
        /// </summary>
        public int GetBuffStack(int actorId, int buffId)
        {
            return 0;
        }

        // ============== IBattleDriver Find Operations ==============

        /// <summary>
        /// Find Actors in range
        /// </summary>
        public IReadOnlyList<int> FindActorsInRange(float x, float z, float radius, int teamFilter = -1)
        {
            return Array.Empty<int>();
        }

        /// <summary>
        /// Find nearest Actor
        /// </summary>
        public int FindNearestActor(float x, float z, float radius, int teamFilter = -1)
        {
            return -1;
        }

        // ============== IBattleDriver Damage Operations ==============

        /// <summary>
        /// Apply damage
        /// </summary>
        public float ApplyDamage(int attackerId, int targetId, float damage, int damageType)
        {
            return 0;
        }

        /// <summary>
        /// Apply heal
        /// </summary>
        public float ApplyHeal(int healerId, int targetId, float heal)
        {
            return 0;
        }

        // ============== Input Processing ==============

        /// <summary>
        /// Submit move input
        /// </summary>
        public void SubmitMoveInput(int actorId, float targetX, float targetZ)
        {
            MoveActor(actorId, targetX, targetZ);
        }

        /// <summary>
        /// Submit skill input
        /// </summary>
        public void SubmitSkillInput(int actorId, int slot, float targetX, float targetZ)
        {
            CastSkill(actorId, slot, targetX, targetZ);
        }

        /// <summary>
        /// Try to resolve service
        /// </summary>
        public bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            if (_world?.Services != null)
            {
                return _world.Services.TryResolve(out service);
            }
            return false;
        }

        /// <summary>
        /// Resolve service
        /// </summary>
        public T Resolve<T>() where T : class
        {
            if (_world?.Services != null)
            {
                return _world.Services.Resolve<T>();
            }
            throw new InvalidOperationException($"Failed to resolve service {typeof(T).Name}");
        }

        // ============== Utility Methods ==============

        private double GetCurrentTimeSeconds()
        {
            return (double)Environment.TickCount64 / 1000.0;
        }
    }

    /// <summary>
    /// ??????
    /// </summary>
    public class ETPlayerSpawnData
    {
        public int ActorId { get; set; }
        public int CharacterId { get; set; }
        public string CharacterName { get; set; }
        public int TeamId { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationY { get; set; }
        public float Scale { get; set; }
        public float Hp { get; set; }
        public float MaxHp { get; set; }

        public ETPlayerSpawnData()
        {
        }

        /// <summary>
        /// ???????????????
        /// </summary>
        public ETPlayerSpawnData(int actorId, int characterId, string characterName, int teamId,
            float x, float y, float z)
        {
            ActorId = actorId;
            CharacterId = characterId;
            CharacterName = characterName;
            TeamId = teamId;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            RotationY = 0f;
            Scale = 1f;
            Hp = 500f;
            MaxHp = 500f;
        }

        public ETPlayerSpawnData(int actorId, int characterId, string characterName, int teamId,
            float x, float y, float z, float rotY, float scale, float hp, float maxHp)
        {
            ActorId = actorId;
            CharacterId = characterId;
            CharacterName = characterName;
            TeamId = teamId;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            RotationY = rotY;
            Scale = scale;
            Hp = hp;
            MaxHp = maxHp;
        }
    }
}
