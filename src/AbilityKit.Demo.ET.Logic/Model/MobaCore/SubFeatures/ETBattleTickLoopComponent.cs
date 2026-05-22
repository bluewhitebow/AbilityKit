namespace ET.Logic
{
    /// <summary>
    /// Tick ?? SubFeature Component
    /// ????????
    /// </summary>
    [ComponentOf(typeof(ETBattleComponent))]
    public class ETBattleTickLoopComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public bool IsEnabled { get; set; } = true;
        public int FrameCount { get; set; }

        public void Awake()
        {
        }
    }

    /// <summary>
    /// Tick ?? SubFeature System
    /// </summary>
    [EntitySystemOf(typeof(ETBattleTickLoopComponent))]
    [FriendOf(typeof(ETBattleTickLoopComponent))]
    [FriendOf(typeof(ETBattleLifecycleComponent))]
    public static partial class ETBattleTickLoopSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleTickLoopComponent self)
        {
            Log.Info("[ETBattleTickLoop] Awake");
            self.IsEnabled = false;
            self.FrameCount = 0;
        }

        [EntitySystem]
        private static void Update(this ETBattleTickLoopComponent self)
        {
            if (!self.IsEnabled)
                return;

            var lifecycle = self.GetParent<ETBattleComponent>()?.GetComponent<ETBattleLifecycleComponent>();
            if (lifecycle == null || !lifecycle.IsRunning)
                return;

            self.FrameCount++;
        }

        [EntitySystem]
        private static void Destroy(this ETBattleTickLoopComponent self)
        {
            self.IsEnabled = false;
        }

        public static void Enable(this ETBattleTickLoopComponent self)
        {
            self.IsEnabled = true;
            Log.Debug("[ETBattleTickLoop] Enabled");
        }

        public static void Disable(this ETBattleTickLoopComponent self)
        {
            self.IsEnabled = false;
            Log.Debug("[ETBattleTickLoop] Disabled");
        }

        public static void ResetFrameCount(this ETBattleTickLoopComponent self)
        {
            self.FrameCount = 0;
        }
    }
}
