using AbilityKit.Demo.Moba.Share;
using BattleState = ET.AbilityKit.Demo.ET.Share.BattleState;

namespace ET.Logic
{
    /// <summary>
    /// 生命周期 SubFeature Component
    /// 负责管理战斗生命周期状�?
    /// </summary>
    [ComponentOf(typeof(ETBattleComponent))]
    public class ETBattleLifecycleComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // 状�?
        public BattleState State { get; set; } = BattleState.Idle;
        public int CurrentFrame { get; set; }
        public double LogicTimeSeconds { get; set; }
        public int TickRate { get; set; } = 30;
        public bool IsRunning { get; set; }

        // 时间追踪
        public double LastTickTime { get; set; }
        public double StartTime { get; set; }

        // 引用
        public IBattleViewEventSink ViewSink { get; set; }

        public void Awake()
        {
        }

        public void Start()
        {
            IsRunning = true;
            StartTime = GetCurrentTimeSeconds();
            LastTickTime = StartTime;
            CurrentFrame = 0;
            LogicTimeSeconds = 0;
            State = BattleState.InProgress;
        }

        public void Update(ETBattleLifecycleComponent self)
        {
            if (!IsRunning)
                return;

            double currentTime = GetCurrentTimeSeconds();
            double deltaTime = currentTime - LastTickTime;

            if (deltaTime >= (1.0 / TickRate))
            {
                Tick((float)deltaTime);
                LastTickTime = currentTime;
            }
        }

        public void OnDestroy(ETBattleLifecycleComponent self)
        {
            Stop();
        }

        public void Tick(float deltaTime)
        {
            CurrentFrame++;
            LogicTimeSeconds += deltaTime;
        }

        public void Stop()
        {
            IsRunning = false;
            State = BattleState.Ended;
        }

        private double GetCurrentTimeSeconds()
        {
            return (double)System.Environment.TickCount64 / 1000.0;
        }
    }

    /// <summary>
    /// 生命周期 SubFeature System
    /// </summary>
    [EntitySystemOf(typeof(ETBattleLifecycleComponent))]
    [FriendOf(typeof(ETBattleLifecycleComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    public static partial class ETBattleLifecycleSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleLifecycleComponent self)
        {
            Log.Info("[ETBattleLifecycle] Awake");
            self.State = BattleState.Idle;
            self.IsRunning = false;
        }

        [EntitySystem]
        private static void Update(this ETBattleLifecycleComponent self)
        {
            // ???? Tick ????
        }

        [EntitySystem]
        private static void Destroy(this ETBattleLifecycleComponent self)
        {
            self.IsRunning = false;
            self.State = BattleState.Ended;
        }

        public static void Initialize(this ETBattleLifecycleComponent self, IBattleViewEventSink viewSink, BattleStartPlan plan)
        {
            self.ViewSink = viewSink;
            self.TickRate = plan.TickRate > 0 ? plan.TickRate : 30;
            self.State = BattleState.Ready;
            Log.Info($"[ETBattleLifecycle] Initialized: TickRate={self.TickRate}");
        }

        public static void StartBattle(this ETBattleLifecycleComponent self)
        {
            self.Start();
            self.ViewSink?.OnBattleStart(0);
            Log.Info("[ETBattleLifecycle] Battle started");
        }

        public static void EndBattle(this ETBattleLifecycleComponent self, int winTeamId)
        {
            self.Stop();
            self.ViewSink?.OnBattleEnd(0, winTeamId);
            Log.Info($"[ETBattleLifecycle] Battle ended: winTeam={winTeamId}");
        }

        public static void OnFrameSyncComplete(this ETBattleLifecycleComponent self)
        {
            self.ViewSink?.OnFrameSyncComplete(self.CurrentFrame);
        }
    }
}
