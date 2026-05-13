using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Bootstrap;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Services;
using AbilityKit.Demo.Moba.Console.View;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console
{
    /// <summary>
    /// Console ?????
    /// ???? [WorldService] ?????????????
    /// </summary>
    public sealed class ConsoleBattleBootstrapper : IBattleBootstrapper, IBattleStartConfigProvider
    {
        private readonly ConsoleBattleContext _context;
        private readonly BattleFlow _flow;
        private readonly IConsoleBattleView _battleView;
        private readonly ConsoleSyncFeature _syncFeature;
        private readonly ConsoleInputFeature _inputFeature;
        private readonly ConsoleHudFeature _hudFeature;
        private readonly ConsoleInputHandler _inputHandler;
        private readonly List<IWorldModule> _modules;
        private readonly BattleStartConfig _config;
        private readonly MobaConfigDatabase _mobaConfig;
        private readonly BattleServices _battleServices;
        private readonly SkillExecutor _skillExecutor;

        private bool _disposed;
        private bool _running;
        private DateTime _lastTick;
        private double _totalTime;

        public PlatformComponents Platform { get; }

        /// <summary>
        /// ??????????? IBattleStartConfigProvider?
        /// </summary>
        BattleStartConfig IBattleStartConfigProvider.Config => _config;

        public ConsoleBattleBootstrapper() : this(null, null)
        {
        }

        public ConsoleBattleBootstrapper(BattleStartConfig? config, MobaConfigDatabase? mobaConfig)
            : this(config, mobaConfig, null)
        {
        }

        public ConsoleBattleBootstrapper(BattleStartConfig? config, MobaConfigDatabase? mobaConfig, IEnumerable<IWorldModule>? additionalModules)
        {
            Log.System("Creating bootstrapper...");

            _modules = new List<IWorldModule>();

            // ???? ConsoleConfigWorldModule?????????? [WorldService] ?????
            _modules.Add(new ConsoleConfigWorldModule());

            // ???????
            if (additionalModules != null)
            {
                _modules.AddRange(additionalModules);
            }

            // ????
            _config = config ?? ConsoleConfigLoader.LoadBattleStartConfig();

            var loader = new ConsoleTextAssetLoader();
            _mobaConfig = mobaConfig ?? ConsoleConfigLoader.LoadMobaConfig(loader);
            _mobaConfig.LoadFromResources(); // ???????????

            Platform = new PlatformComponents();

            _context = new ConsoleBattleContext();
            _context.Plan = _config.BuildPlan();

            _flow = new BattleFlow();

            _battleView = new ConsoleBattleView(
                new ConsoleEntityDisplayService(),
                new ConsoleFloatingTextSystem(),
                new ConsoleAreaViewSystem(),
                new ConsoleProjectileDisplayService(),
                Platform.Renderer);

            // ?????????????
            _battleServices = new BattleServices((BattleViewServices)_battleView);
            _skillExecutor = new SkillExecutor(_mobaConfig, _battleServices);

            _syncFeature = new ConsoleSyncFeature();
            _inputFeature = new ConsoleInputFeature();
            _hudFeature = new ConsoleHudFeature();
            _inputHandler = new ConsoleInputHandler(_inputFeature, _hudFeature, _flow, Platform.Input);

            // ?????? InputFeature
            _inputFeature.SetServices(_skillExecutor, _battleServices);

            _hudFeature.SetBattleView(_battleView);
            _hudFeature.SetInputFeature(_inputFeature);
            _hudFeature.SetSyncFeature(_syncFeature);

            _flow.Events.PhaseEntered += OnPhaseEntered;

            Log.Config($"Config loaded: {_config.Name}, TickRate: {_config.TickRate}, SyncMode: {_config.SyncMode}");
            Log.Config($"MobaConfig: {_mobaConfig.CharacterCount} characters, {_mobaConfig.SkillCount} skills, {_mobaConfig.ProjectileCount} projectiles");
        }

        public IConsoleBattleView BattleView => _battleView;
        public IBattleFlow Flow => _flow;
        public ConsoleBattleContext Context => _context;
        public bool IsRunning => _running;
        public IReadOnlyList<IWorldModule> Modules => _modules;

        /// <summary>
        /// ?????????
        /// </summary>
        public MobaConfigDatabase MobaConfig => _mobaConfig;

        /// <summary>
        /// ????????
        /// </summary>
        public BattleStartPlan Build()
        {
            return _config.BuildPlan();
        }

        /// <summary>
        /// ????????????? IBattleStartConfigProvider?
        /// </summary>
        BattleStartPlan IBattleStartConfigProvider.BuildPlan() => Build();

        public void Initialize()
        {
            Log.System($"Initializing... Plan: {_context.Plan}");

            ConfigureWorld();
            _context.InitializeEcsWorld();

            LogBattleConfig();
        }

        private void ConfigureWorld()
        {
            var builder = new WorldContainerBuilder();

            foreach (var module in _modules)
            {
                Log.Debug($"Configuring module: {module.GetType().Name}");
                module.Configure(builder);
            }

            var container = builder.Build();

            // ?? ITextAssetLoader ?????
            var loader = container.Resolve<ITextAssetLoader>();
            if (loader != null)
            {
                Log.System($"ITextAssetLoader registered: {loader.GetType().Name}");
            }
            else
            {
                Log.Warn("No ITextAssetLoader registered");
            }
        }

        private void LogBattleConfig()
        {
            Log.Config("=== Battle Configuration ===");
            Log.Config($"  World: {_context.Plan.WorldId} ({_context.Plan.WorldType})");
            Log.Config($"  Sync: {_context.Plan.SyncMode}, TickRate: {_context.Plan.TickRate}");
            Log.Config($"  Max Players: {_context.Plan.MaxPlayerCount}");
            Log.Config($"  Debug: {_context.Plan.EnableDebug}");
            Log.Config($"  Input Delay: {_context.Plan.InputDelayFrames} frames");

            if (_mobaConfig.CharacterCount > 0)
            {
                Log.Config("  Characters:");
                foreach (var c in _mobaConfig.GetAllCharacters())
                {
                    Log.Config($"    - {c.Name} (HP:{c.BaseHp}, ATK:{c.BaseAttack}, DEF:{c.BaseDefense})");
                }
            }

            Log.Config("============================");
        }

        public void Start()
        {
            Log.System("Starting...");

            _syncFeature.OnAttach(_context);
            _inputFeature.OnAttach(_context);
            _hudFeature.OnAttach(_context);
            _inputHandler.Start();
            _flow.Start();

            _lastTick = DateTime.Now;
            _running = true;
        }

        public void Stop()
        {
            Log.System("Stopping...");
            _running = false;
            _flow.Stop();
        }

        public void Tick(float deltaTime = 0.033f)
        {
            if (!_running) return;

            var now = DateTime.Now;
            var elapsed = (now - _lastTick).TotalSeconds;
            _lastTick = now;

            _totalTime += elapsed;
            _context.LogicTimeSeconds = _totalTime;
            _context.LastFrame++;

            _flow.Tick((float)elapsed);
            _syncFeature.Tick(_context, (float)elapsed);
            _inputFeature.Tick(_context, (float)elapsed);
            _hudFeature.Tick(_context, (float)elapsed);
            _battleView.Tick((float)elapsed);
        }

        public void TransitionTo(string phaseName)
        {
            Log.System($"Transitioning to: {phaseName}");
            _flow.TransitionTo(phaseName);
        }

        public void SetupBattle()
        {
            TransitionTo("Connect");
            TransitionTo("CreateOrJoinWorld");
            TransitionTo("LoadAssets");
            TransitionTo("InMatch");
            RegisterEntitiesFromConfig();
            _hudFeature.RenderHud();
        }

        private void RegisterEntitiesFromConfig()
        {
            Log.Battle($"Setting up battle with {_config.Players.Count} players...");

            foreach (var player in _config.Players)
            {
                CreateCharacterFromPlayer(player);
            }

            Log.Entity($"Registered entities: {_context.EcsWorld.AliveCount} total");
        }

        private void CreateCharacterFromPlayer(PlayerConfig player)
        {
            // ??????
            if (!_mobaConfig.TryGetCharacter(player.HeroId, out var charConfig))
            {
                Log.Warn($"Character config not found for HeroId: {player.HeroId}, using defaults");
                CreateCharacter(
                    actorId: HashPlayerId(player.PlayerId),
                    name: player.Name,
                    entityCode: player.HeroId,
                    hp: 500,
                    maxHp: 500,
                    x: player.PositionX,
                    y: player.PositionY,
                    z: player.PositionZ);
                return;
            }

            CreateCharacter(
                actorId: HashPlayerId(player.PlayerId),
                name: charConfig.Name,
                entityCode: charConfig.ModelId,
                hp: charConfig.BaseHp,
                maxHp: charConfig.BaseHp,
                x: player.PositionX,
                y: player.PositionY,
                z: player.PositionZ);

            Log.Battle($"Spawned {charConfig.Name} (Team {player.TeamId}) at ({player.PositionX:F1}, {player.PositionZ:F1})");
        }

        private static int HashPlayerId(string playerId)
        {
            return playerId.GetHashCode() & 0xFFFF;
        }

        public void RegisterDemoEntities()
        {
            CreateCharacter(1, "Warrior", 1001, 800, 800, 0, 0, 0);
            CreateCharacter(2, "Archer", 1002, 600, 600, 10, 0, 0);
            CreateCharacter(3, "Mage", 1003, 500, 500, -10, 0, 0);
            CreateCharacter(101, "Minion_A1", 2001, 300, 300, 20, 0, 0);
            CreateCharacter(102, "Minion_A2", 2001, 300, 300, 22, 0, 2);
            CreateCharacter(103, "Minion_A3", 2002, 250, 250, 21, 0, 1);

            Log.Entity($"Registered demo entities: 3 heroes, 3 minions");
            Log.Entity($"ECS World alive count: {_context.EcsWorld.AliveCount}");
        }

        public void CreateCharacter(int actorId, string name, int entityCode, float hp, float maxHp, float x, float y, float z)
        {
            var netId = new BattleNetId(actorId);
            var e = _context.EntityFactory.CreateCharacter(netId, entityCode);

            if (e.TryGetRef(out BattleTransformComponent t))
            {
                t.Position = new Core.Math.Vec3(x, y, z);
            }

            if (e.TryGetRef(out BattleCharacterComponent c))
            {
                c.Hp = hp;
                c.HpMax = maxHp;
                c.TeamId = 1;
            }

            _battleView.RegisterEntity(actorId, name, "Character", hp, maxHp, x, y, z);

            // ???????
            _battleServices.RegisterActor(new ActorInfo(actorId, name)
            {
                X = x,
                Y = y,
                Z = z,
                Hp = hp,
                HpMax = maxHp,
                Attack = 50f,
                Defense = 20f,
                TeamId = 1
            });

            Log.Entity($"Created: #{actorId} {name}");
        }

        public void SimulateDamage(int targetId, float damage)
        {
            var netId = new BattleNetId(targetId);
            if (_context.EntityLookup.TryResolve(_context.EcsWorld, netId, out var e))
            {
                if (e.TryGetRef(out BattleCharacterComponent c))
                {
                    var newHp = Math.Max(0, c.Hp - damage);
                    c.Hp = newHp;
                    _battleView.UpdateEntityHp(targetId, newHp, c.HpMax);
                    _battleView.ShowFloatingText(targetId, $"-{damage:F0}", false);
                    Log.Damage($"Actor#{targetId} takes -{damage:F0} damage (HP: {newHp:F0}/{c.HpMax:F0})");
                }
            }
        }

        public EC.IEntity CreateProjectile(int actorId, int ownerId, int templateId = 0)
        {
            var netId = new BattleNetId(actorId);
            var ownerNetId = new BattleNetId(ownerId);
            var e = _context.EntityFactory.CreateProjectile(netId, ownerNetId, templateId);
            Log.Entity($"Created projectile: #{actorId} from #{ownerId}");
            return e;
        }

        public void ShowHud() => _hudFeature.RenderHud();

        public void PrintWorldStatus()
        {
            var world = _context.EcsWorld;
            if (world == null)
            {
                Log.System("ECS World: not initialized");
                return;
            }

            Log.System($"ECS World Status:");
            Log.System($"  Alive entities: {world.AliveCount}");
            Log.System($"  Phase: {_flow.CurrentPhase}");
            Log.System($"  Frame: {_context.LastFrame}");
        }

        private void OnPhaseEntered(string phaseName)
        {
            Log.Debug($"Phase entered: {phaseName}");

            if (phaseName == "InMatch")
            {
                _context.State = BattleState.InMatch;
                _context.IsInitialized = true;
                Log.Battle("Battle started!");
                PrintWorldStatus();
            }
            else if (phaseName == "End")
            {
                Log.Battle("Battle ended!");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Log.System("Disposing...");
            _inputHandler?.Dispose();
            _hudFeature?.OnDetach(_context);
            _inputFeature?.OnDetach(_context);
            _syncFeature?.OnDetach(_context);
            _flow?.Dispose();
            _battleView?.Dispose();
            _context?.Dispose();
            Log.System("Disposed.");
        }
    }

    /// <summary>
    /// Platform components container
    /// Contains all platform-related abstractions for swapping implementations
    /// </summary>
    public sealed class PlatformComponents
    {
        public IOutput Output { get; }
        public IInputSource Input { get; }
        public IRenderer Renderer { get; }
        public ILogSink LogSink { get; }

        public PlatformComponents() : this(null, null, null, null)
        {
        }

        public PlatformComponents(
            IOutput? output,
            IInputSource? input,
            IRenderer? renderer,
            ILogSink? logSink)
        {
            Output = output ?? new Platform.Console_.ConsoleOutput();
            Input = input ?? new Platform.Console_.ConsoleInputSource();
            Renderer = renderer ?? new Platform.Console_.ConsoleRenderer(80, 40);
            LogSink = logSink ?? new ConsoleLogSink(Output);
        }

        public PlatformComponents(IOutput output, IInputSource input, IRenderer renderer)
            : this(output, input, renderer, null)
        {
        }
    }

    /// <summary>
    /// Console ?? Sink ??
    /// ??????????????
    /// </summary>
    public sealed class ConsoleLogSink : ILogSink
    {
        private readonly IOutput _output;

        public string Name => "ConsoleLogSink";

        public ConsoleLogSink(IOutput output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public void Log(OutputChannel channel, string message)
        {
            _output.Write(channel, message);
        }
    }
}
