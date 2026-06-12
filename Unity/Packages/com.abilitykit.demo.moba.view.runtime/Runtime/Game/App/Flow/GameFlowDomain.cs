using System;
using System.Collections.Generic;
using AbilityKit.Ability.Flow;
using AbilityKit.World.ECS;
using AbilityKit.Core.Common.Config;
using AbilityKit.Core.Common.Log;
using AbilityKit.Game;
using AbilityKit.Game.View.Flow;
using UnityHFSM;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    public sealed class GameFlowDomain
    {
        private readonly GameEntry _entry;
        private readonly GamePhaseContext _ctx;
        private readonly MobaFlowConfiguration _flowConfig;
        private readonly MobaFlowConditionResolver _conditionResolver;
        private readonly MobaFlowActionExecutor _actionExecutor;
 
        public LayeredJsonSettingsStore Settings { get; } = new LayeredJsonSettingsStore();

        private readonly FlowContext _flowContext;
        private readonly FlowEventQueue<MobaRootEvent> _rootEvents;
        private readonly StateMachine<string, MobaRootState, MobaRootEvent> _root;
        private readonly HfsmFlowRunner<string, MobaRootState, MobaRootEvent> _runner;

        private readonly PhaseFeatureHost<GamePhaseContext, IGamePhaseFeature> _features;
        private readonly PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature> _bootFeaturePlan;
        private readonly PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature> _battleFeaturePlan;
        private readonly PhaseStateFeatureRegistry<MobaRootState, GamePhaseContext, IGamePhaseFeature> _rootStateBindings;
        private readonly PhaseStateFeatureRegistry<MobaBattleState, GamePhaseContext, IGamePhaseFeature> _battleStateBindings;
        private MobaRootState _activeRoot;
        private MobaBattleState _activeBattle;
        private bool _battleRequested;
        private IBattleBootstrapper _pendingBootstrapper;
        private Func<BattleStartPlan, AbilityKit.Network.Abstractions.IConnection> _pendingGatewayConnectionFactory;

        private BattleSessionFeature _battleSessionFeature;

        private StateMachine<MobaRootState, MobaBattleState, MobaBattleEvent> _battleFsm;
        private bool _battleSessionStarted;
        private bool _battleFirstFrameReceived;

        public GameFlowDomain(GameEntry entry)
            : this(entry, rootOverride: default)
        {
        }

        public GameFlowDomain(GameEntry entry, IEntity rootOverride)
        {
            _entry = entry;

            var root = rootOverride;
            if (!root.IsValid && entry != null)
            {
                root = entry.Root;
            }

            if (!root.IsValid)
            {
                throw new ArgumentNullException(nameof(rootOverride));
            }

            _ctx = new GamePhaseContext(_entry, (IEntity)root);
            _flowConfig = MobaFlowConfiguration.CreateDefault();
            _conditionResolver = new MobaFlowConditionResolver();
            _actionExecutor = new MobaFlowActionExecutor();
            _features = new PhaseFeatureHost<GamePhaseContext, IGamePhaseFeature>(
                fail: message => Log.Error($"[GameFlowDomain] PhaseFeatureHost: {message}"),
                initialCapacity: 16,
                attachFeature: AttachFeatureCore,
                detachFeature: DetachFeatureCore,
                tickFeature: TickFeatureCore);
            _features.AttachAll(in _ctx);
            _bootFeaturePlan = BuildBootFeaturePlan();
            _battleFeaturePlan = BuildBattleFeaturePlan();
            _rootStateBindings = BuildMobaRootStateBindings();
            _battleStateBindings = BuildMobaBattleStateBindings();

            _flowContext = new FlowContext();
            _rootEvents = new FlowEventQueue<MobaRootEvent>();
            _root = BuildMobaRootStateMachine();
            _runner = new HfsmFlowRunner<string, MobaRootState, MobaRootEvent>(_flowContext, _root, _rootEvents);
        }

        public MobaRootState CurrentPhase => _activeRoot;

        public void Start()
        {
            _runner.Start();
            _rootEvents.Enqueue(MobaRootEvent.BootCompleted);

            if (_entry != null)
            {
                _entry.StartCoroutine(UnityJsonSettingsBootstrap.LoadPersistentInto(Settings, RuntimeJsonSettingsCodec.DeserializeFlat));
            }
        }

        public void StartWithPersistentSettingsSync()
        {
            _runner.Start();
            _rootEvents.Enqueue(MobaRootEvent.BootCompleted);
            UnityJsonSettingsBootstrap.LoadPersistentIntoSync(Settings, RuntimeJsonSettingsCodec.DeserializeFlat);
        }

        public bool TrySaveSettingsOverridesToPersistent()
        {
            return UnityJsonSettingsBootstrap.TrySaveOverridesToPersistent(Settings.OverrideValues, RuntimeJsonSettingsCodec.SerializeFlat);
        }

        public void Tick(float deltaTime)
        {
            try
            {
                _runner.Step(deltaTime);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[GameFlowDomain] HFSM Step failed");
            }

            _features.Tick(in _ctx, deltaTime);
        }

        public void OnGUI()
        {
#if UNITY_EDITOR
            _features.OnGUI(in _ctx);

            if (!_entry.DebugEnabled) return;

            if (_activeRoot == MobaRootState.Battle && _activeBattle == MobaBattleState.InMatch)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(350, 10, 420, 140), GUI.skin.window);
            GUILayout.Label($"HFSM Root={_activeRoot}, Battle={_activeBattle}");

            if (GUILayout.Button("Enter Battle", GUILayout.Height(28)))
            {
                EnterBattle((IBattleBootstrapper)null);
            }

            if (GUILayout.Button("Battle End", GUILayout.Height(28)))
            {
                var state = _root.GetState(MobaRootState.Battle);
                if (state is StateMachine<MobaRootState, MobaBattleState, MobaBattleEvent> battle)
                {
                    battle.Trigger(MobaBattleEvent.Ended);
                }
            }

            if (GUILayout.Button("Return Lobby", GUILayout.Height(28)))
            {
                _rootEvents.Enqueue(MobaRootEvent.ReturnLobby);
            }

            GUILayout.EndArea();
#endif
        }

        public void SwitchTo(IGamePhase next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));

            if (next is BattlePhase battle)
            {
                EnterBattle((IBattleBootstrapper)null);
                return;
            }

            ReturnToBoot();
        }

        public void Attach(IGamePhaseFeature feature)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            _features.Add(feature, in _ctx);
        }

        public void Detach(IGamePhaseFeature feature)
        {
            _features.Remove(feature, in _ctx);
        }

        private void AttachFeatureCore(IGamePhaseFeature feature, in GamePhaseContext ctx)
        {
            if (ctx.Root.IsValid)
            {
                ctx.Root.WithRef((object)feature);
            }

            feature.OnAttach(ctx);
        }

        private void DetachFeatureCore(IGamePhaseFeature feature, in GamePhaseContext ctx)
        {
            feature.OnDetach(ctx);

            if (ctx.Root.IsValid)
            {
                ctx.Root.RemoveComponent(feature.GetType());
            }
        }

        private void TickFeatureCore(IGamePhaseFeature feature, in GamePhaseContext ctx, float deltaTime)
        {
            try
            {
                feature.Tick(ctx, deltaTime);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[GameFlowDomain] Feature.Tick failed: feature={feature?.GetType().FullName}");
            }
        }

        public int AttachBootFeatures()
        {
            return _bootFeaturePlan.InstallAll(in _ctx, Attach);
        }

        public int AttachBattleFeatures(IReadOnlyList<string> featureIds = null)
        {
            return AttachBattleFeatures(featureIds, gatewayConnectionFactory: null);
        }

        public int AttachBattleFeatures(
            IReadOnlyList<string> featureIds,
            Func<BattleStartPlan, AbilityKit.Network.Abstractions.IConnection> gatewayConnectionFactory)
        {
            _pendingGatewayConnectionFactory = gatewayConnectionFactory;
            try
            {
                return _battleFeaturePlan.InstallByIdsOrAll(
                    featureIds,
                    in _ctx,
                    Attach,
                    message => Log.Error($"[GameFlowDomain] {message}"));
            }
            finally
            {
                _pendingGatewayConnectionFactory = null;
            }
        }

        public void EnterBattle(IBattleBootstrapper bootstrapper)
        {
            _battleRequested = true;
            _pendingBootstrapper = bootstrapper;
            _rootEvents.Enqueue(MobaRootEvent.EnterBattle);
        }

        public void ReturnToBoot()
        {
            _battleRequested = false;
            _pendingBootstrapper = null;
            _pendingGatewayConnectionFactory = null;
            _rootEvents.Enqueue(MobaRootEvent.ReturnLobby);
        }

        private StateMachine<string, MobaRootState, MobaRootEvent> BuildMobaRootStateMachine()
        {
            var fsm = new StateMachine<string, MobaRootState, MobaRootEvent>();

            fsm.AddState(MobaRootState.Boot,
                onEnter: _ =>
                {
                    _activeRoot = MobaRootState.Boot;
                    _rootStateBindings.Enter(MobaRootState.Boot, in _ctx);
                },
                onExit: _ => _rootStateBindings.Exit(MobaRootState.Boot, in _ctx));
 
            fsm.AddState(MobaRootState.Lobby,
                onEnter: _ =>
                {
                    _activeRoot = MobaRootState.Lobby;
                    _rootStateBindings.Enter(MobaRootState.Lobby, in _ctx);
                },
                onExit: _ => _rootStateBindings.Exit(MobaRootState.Lobby, in _ctx));

            var battle = BuildMobaBattleStateMachine();
            fsm.AddState(MobaRootState.Battle, battle);

            AddRootTransitions(fsm, _flowConfig.RootMachine);
            fsm.SetStartState(_flowConfig.RootMachine.StartState);
            return fsm;
        }

        private StateMachine<MobaRootState, MobaBattleState, MobaBattleEvent> BuildMobaBattleStateMachine()
        {
            var fsm = new StateMachine<MobaRootState, MobaBattleState, MobaBattleEvent>();
            _battleFsm = fsm;

            fsm.StateChanged += s => { _activeBattle = s.name; };

            fsm.AddState(MobaBattleState.Prepare,
                onEnter: _ =>
                {
                    _activeRoot = MobaRootState.Battle;
                    Log.Info("[GameFlowDomain] MobaBattleState.Prepare entered");
                    _battleStateBindings.Enter(MobaBattleState.Prepare, in _ctx);
                },
                onExit: _ => _battleStateBindings.Exit(MobaBattleState.Prepare, in _ctx));
 
            fsm.AddState(MobaBattleState.Connect,
                onEnter: _ =>
                {
                    _activeRoot = MobaRootState.Battle;
                    Log.Info("[GameFlowDomain] MobaBattleState.Connect entered");
                    _battleStateBindings.Enter(MobaBattleState.Connect, in _ctx);
 
                    if (_battleSessionStarted)
                    {
                        fsm.Trigger(MobaBattleEvent.Connected);
                    }
                    else if (_battleFirstFrameReceived)
                    {
                        fsm.Trigger(MobaBattleEvent.Connected);
                    }
                },
                onExit: _ => _battleStateBindings.Exit(MobaBattleState.Connect, in _ctx));
 
            fsm.AddState(MobaBattleState.CreateOrJoinWorld,
                onEnter: _ =>
                {
                    _activeRoot = MobaRootState.Battle;
                    Log.Info("[GameFlowDomain] MobaBattleState.CreateOrJoinWorld entered");
                    _battleStateBindings.Enter(MobaBattleState.CreateOrJoinWorld, in _ctx);
 
                    if (_battleFirstFrameReceived)
                    {
                        fsm.Trigger(MobaBattleEvent.JoinedWorld);
                    }
                },
                onExit: _ => _battleStateBindings.Exit(MobaBattleState.CreateOrJoinWorld, in _ctx));
 
            fsm.AddState(MobaBattleState.LoadAssets,
                onEnter: _ =>
                {
                    _activeRoot = MobaRootState.Battle;
                    Log.Info("[GameFlowDomain] MobaBattleState.LoadAssets entered");
                    _battleStateBindings.Enter(MobaBattleState.LoadAssets, in _ctx);
 
                    if (_battleFirstFrameReceived)
                    {
                        fsm.Trigger(MobaBattleEvent.LoadingDone);
                    }
                },
                onExit: _ => _battleStateBindings.Exit(MobaBattleState.LoadAssets, in _ctx));
 
            fsm.AddState(MobaBattleState.InMatch,
                onEnter: _ =>
                {
                    _activeRoot = MobaRootState.Battle;
                    Log.Info("[GameFlowDomain] MobaBattleState.InMatch entered");
                    _battleStateBindings.Enter(MobaBattleState.InMatch, in _ctx);
                },
                onExit: _ => _battleStateBindings.Exit(MobaBattleState.InMatch, in _ctx));
 
            fsm.AddState(MobaBattleState.End,
                onEnter: _ =>
                {
                    _activeRoot = MobaRootState.Battle;
                    _battleStateBindings.Enter(MobaBattleState.End, in _ctx);
                },
                onExit: _ => _battleStateBindings.Exit(MobaBattleState.End, in _ctx));

            AddBattleTransitions(fsm, _flowConfig.BattleMachine);
            fsm.SetStartState(_flowConfig.BattleMachine.StartState);
            return fsm;
        }

        private void AddRootTransitions(
            StateMachine<string, MobaRootState, MobaRootEvent> fsm,
            PhaseStateMachineSpec<MobaRootState, MobaRootEvent> spec)
        {
            for (var i = 0; i < spec.Transitions.Count; i++)
            {
                var transition = spec.Transitions[i];
                if (string.IsNullOrEmpty(transition.ConditionId))
                {
                    fsm.AddTriggerTransition(transition.Trigger, transition.From, transition.To);
                    continue;
                }

                fsm.AddTriggerTransition(
                    transition.Trigger,
                    transition.From,
                    transition.To,
                    condition: _ => EvaluateRootTransitionCondition(transition.ConditionId));
            }
        }

        private bool EvaluateRootTransitionCondition(string conditionId)
        {
            var ctx = BuildFlowConditionContext();
            return _conditionResolver.Evaluate(conditionId, in ctx);
        }
 
        private MobaFlowConditionContext BuildFlowConditionContext()
        {
            return new MobaFlowConditionContext(
                battleRequested: _battleRequested,
                authenticated: IsAuthenticatedForFlow(),
                roomReady: IsRoomReadyForFlow(),
                connectivityReady: IsConnectivityReadyForFlow(),
                assetsReady: IsAssetsReadyForFlow());
        }
 
        private bool IsAuthenticatedForFlow()
        {
            return true;
        }
 
        private bool IsRoomReadyForFlow()
        {
            return true;
        }
 
        private bool IsConnectivityReadyForFlow()
        {
            return true;
        }
 
        private bool IsAssetsReadyForFlow()
        {
            return true;
        }

        private static void AddBattleTransitions(
            StateMachine<MobaRootState, MobaBattleState, MobaBattleEvent> fsm,
            PhaseStateMachineSpec<MobaBattleState, MobaBattleEvent> spec)
        {
            for (var i = 0; i < spec.Transitions.Count; i++)
            {
                var transition = spec.Transitions[i];
                fsm.AddTriggerTransition(transition.Trigger, transition.From, transition.To);
            }
        }

        private void OnBattleSessionStarted()
        {
            _battleSessionStarted = true;
            Log.Info($"[GameFlowDomain] SessionStarted, activeBattle={_activeBattle}");
            if (_battleFsm == null) return;

            if (_activeBattle == MobaBattleState.Prepare)
            {
                _battleFsm.Trigger(MobaBattleEvent.PrepareDone);
            }
            else if (_activeBattle == MobaBattleState.Connect)
            {
                _battleFsm.Trigger(MobaBattleEvent.Connected);
            }
        }

        private void OnBattleFirstFrameReceived()
        {
            _battleFirstFrameReceived = true;
            Log.Info($"[GameFlowDomain] FirstFrameReceived, activeBattle={_activeBattle}");
            if (_battleFsm == null) return;

            if (_activeBattle == MobaBattleState.Prepare)
            {
                _battleFsm.Trigger(MobaBattleEvent.PrepareDone);
            }
            else if (_activeBattle == MobaBattleState.Connect)
            {
                _battleFsm.Trigger(MobaBattleEvent.Connected);
            }
            else if (_activeBattle == MobaBattleState.CreateOrJoinWorld)
            {
                _battleFsm.Trigger(MobaBattleEvent.JoinedWorld);
            }
            else if (_activeBattle == MobaBattleState.LoadAssets)
            {
                _battleFsm.Trigger(MobaBattleEvent.LoadingDone);
            }
        }

        private void OnBattleSessionFailed(Exception ex)
        {
            Log.Exception(ex, "[GameFlowDomain] Session failed");
            if (_battleFsm == null) return;

            if (_activeBattle != MobaBattleState.End)
            {
                _battleFsm.Trigger(MobaBattleEvent.Ended);
            }
        }

        private PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature> BuildBootFeaturePlan()
        {
            return new PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature>(1)
                .Add("boot_menu", (in GamePhaseContext ctx) => new BootMenuOnGUIFeature());
        }

        private PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature> BuildBattleFeaturePlan()
        {
            return new PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature>(8)
                .Add("context", (in GamePhaseContext ctx) => new BattleContextFeature())
                .Add("session", (in GamePhaseContext ctx) => CreateBattleSessionFeature())
                .Add("entity", (in GamePhaseContext ctx) => new BattleEntityFeature())
                .Add("sync", (in GamePhaseContext ctx) => new BattleSyncFeature())
                .Add("input", (in GamePhaseContext ctx) => new BattleInputFeature())
                .Add("view", (in GamePhaseContext ctx) => new BattleViewFeature())
                .Add("hud", (in GamePhaseContext ctx) => new BattleHudFeature())
                .Add("debug_ongui", (in GamePhaseContext ctx) => new BattleDebugOnGUIFeature());
        }

        private PhaseStateFeatureRegistry<MobaRootState, GamePhaseContext, IGamePhaseFeature> BuildMobaRootStateBindings()
        {
            return new PhaseStateFeatureRegistry<MobaRootState, GamePhaseContext, IGamePhaseFeature>(
                    message => Log.Error($"[GameFlowDomain] {message}"),
                    initialCapacity: 2)
                .Add(MobaRootState.Boot, BuildBootStateBinding(_flowConfig.BootFeatures))
                .Add(MobaRootState.Lobby, BuildBootStateBinding(_flowConfig.LobbyFeatures));
        }

        private PhaseStateFeatureRegistry<MobaBattleState, GamePhaseContext, IGamePhaseFeature> BuildMobaBattleStateBindings()
        {
            return new PhaseStateFeatureRegistry<MobaBattleState, GamePhaseContext, IGamePhaseFeature>(
                    message => Log.Error($"[GameFlowDomain] {message}"),
                    initialCapacity: 6)
                .Add(MobaBattleState.Prepare, BuildBattlePrepareBinding())
                .Add(MobaBattleState.Connect, BuildBattleDebugBinding(_flowConfig.BattleConnectFeatures))
                .Add(MobaBattleState.CreateOrJoinWorld, BuildBattleDebugBinding(_flowConfig.BattleCreateOrJoinWorldFeatures))
                .Add(MobaBattleState.LoadAssets, BuildBattleDebugBinding(_flowConfig.BattleLoadAssetsFeatures))
                .Add(MobaBattleState.InMatch, BuildBattleInMatchBinding())
                .Add(MobaBattleState.End, BuildBattleEndBinding());
        }

        private PhaseStateFeatureBinding<GamePhaseContext, IGamePhaseFeature> BuildBootStateBinding(PhaseStateFeatureSpec spec)
        {
            return PhaseStateFeatureBindingFactory.Create<GamePhaseContext, IGamePhaseFeature>(
                spec,
                Attach,
                _bootFeaturePlan,
                clear: (in GamePhaseContext ctx) => ClearFeatures(),
                exitAction: ExecuteFlowAction,
                fail: message => Log.Error($"[GameFlowDomain] {message}"));
        }

        private PhaseStateFeatureBinding<GamePhaseContext, IGamePhaseFeature> BuildBattlePrepareBinding()
        {
            return PhaseStateFeatureBindingFactory.Create<GamePhaseContext, IGamePhaseFeature>(
                _flowConfig.BattlePrepareFeatures,
                Attach,
                _battleFeaturePlan,
                clear: (in GamePhaseContext ctx) => ClearFeatures(),
                enterBeforeAction: ExecuteFlowAction,
                exitAction: ExecuteFlowAction,
                fail: message => Log.Error($"[GameFlowDomain] {message}"));
        }

        private PhaseStateFeatureBinding<GamePhaseContext, IGamePhaseFeature> BuildBattleDebugBinding(PhaseStateFeatureSpec spec)
        {
            return PhaseStateFeatureBindingFactory.Create<GamePhaseContext, IGamePhaseFeature>(
                spec,
                Attach,
                _battleFeaturePlan,
                exitAction: ExecuteFlowAction,
                fail: message => Log.Error($"[GameFlowDomain] {message}"));
        }

        private PhaseStateFeatureBinding<GamePhaseContext, IGamePhaseFeature> BuildBattleInMatchBinding()
        {
            return PhaseStateFeatureBindingFactory.Create<GamePhaseContext, IGamePhaseFeature>(
                _flowConfig.BattleInMatchFeatures,
                Attach,
                _battleFeaturePlan,
                exitAction: ExecuteFlowAction,
                fail: message => Log.Error($"[GameFlowDomain] {message}"));
        }

        private PhaseStateFeatureBinding<GamePhaseContext, IGamePhaseFeature> BuildBattleEndBinding()
        {
            return PhaseStateFeatureBindingFactory.Create<GamePhaseContext, IGamePhaseFeature>(
                _flowConfig.BattleEndFeatures,
                Attach,
                _battleFeaturePlan,
                clear: (in GamePhaseContext ctx) => ClearFeatures(),
                enterAfterAction: ExecuteFlowAction,
                exitAction: ExecuteFlowAction,
                fail: message => Log.Error($"[GameFlowDomain] {message}"));
        }

        private BattleSessionFeature CreateBattleSessionFeature()
        {
            _battleSessionFeature = new BattleSessionFeature(_pendingBootstrapper, _pendingGatewayConnectionFactory);
            _battleSessionFeature.SessionStarted += OnBattleSessionStarted;
            _battleSessionFeature.FirstFrameReceived += OnBattleFirstFrameReceived;
            _battleSessionFeature.SessionFailed += OnBattleSessionFailed;
            return _battleSessionFeature;
        }

        internal void ResetBattleSessionRuntimeState()
        {
            _battleSessionStarted = false;
            _battleFirstFrameReceived = false;
        }

        internal void ReturnLobbyAfterBattleEnd()
        {
            _rootEvents.Enqueue(MobaRootEvent.ReturnLobby);
            _battleSessionFeature = null;
            ResetBattleSessionRuntimeState();
        }

        private void ExecuteFlowAction(in GamePhaseContext ctx, string actionId)
        {
            ExecuteFlowAction(actionId);
        }

        private void ExecuteFlowAction(in GamePhaseContext ctx, string actionId, int installedCount)
        {
            ExecuteFlowAction(actionId, installedCount);
        }

        private void ExecuteFlowAction(string actionId, int installedCount = 0)
        {
            var ctx = new MobaFlowActionContext(this, installedCount);
            if (!_actionExecutor.Execute(actionId, in ctx))
            {
                Log.Error($"[GameFlowDomain] Unknown flow action: {actionId}");
            }
        }
 
        private void ClearFeatures()
        {
            _features.Clear(in _ctx);
            _features.AttachAll(in _ctx);

            if (_battleSessionFeature != null)
            {
                _battleSessionFeature.SessionStarted -= OnBattleSessionStarted;
                _battleSessionFeature.FirstFrameReceived -= OnBattleFirstFrameReceived;
                _battleSessionFeature.SessionFailed -= OnBattleSessionFailed;
                _battleSessionFeature = null;
            }
        }
    }
}
