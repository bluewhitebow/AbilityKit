using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Battle view component system
    /// </summary>
    [EntitySystemOf(typeof(ETBattleViewComponent))]
    [FriendOf(typeof(ETBattleViewComponent))]
    [FriendOf(typeof(ETUnitViewComponent))]
    public static partial class ETBattleViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleViewComponent self)
        {
            Log.Info("[ETBattleView] ETBattleViewComponentSystem awake");
        }

        [EntitySystem]
        private static void Update(this ETBattleViewComponent self)
        {
        }

        /// <summary>
        /// Initialize view component
        /// </summary>
        public static void Initialize(this ETBattleViewComponent self)
        {
            self.IsInitialized = true;
            self.RenderFrameCount = 0;
            self.LastRenderTime = Environment.TickCount64;

            // Create unit view component
            self.UnitViewComponent = self.Scene().AddComponent<ETUnitViewComponent>();

            Log.Info("[ETBattleView] View initialized");
        }

        /// <summary>
        /// Handle battle start
        /// </summary>
        public static void OnBattleStart(this ETBattleViewComponent self, BattleStartEvent evt)
        {
            Log.Info("[ETBattleView] Battle started, rendering enabled");
            Console.WriteLine("========================================");
            Console.WriteLine($"[BATTLE] Battle {evt.BattleId} STARTED!");
            Console.WriteLine("========================================");
        }

        /// <summary>
        /// Handle battle end
        /// </summary>
        public static void OnBattleEnd(this ETBattleViewComponent self, BattleEndEvent evt)
        {
            Log.Info("[ETBattleView] Battle ended, rendering disabled");

            // Show final result
            self.RenderFinalResult(evt.IsVictory);
        }

        /// <summary>
        /// Render final result
        /// </summary>
        public static void RenderFinalResult(this ETBattleViewComponent self, bool isVictory)
        {
            Console.WriteLine("========================================");
            Console.WriteLine($"[RESULT] {(isVictory ? "VICTORY!" : "DEFEAT!")}");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
        }

        /// <summary>
        /// Show help information
        /// </summary>
        public static void ShowHelp(this ETBattleViewComponent self)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("Controls:");
            Console.WriteLine("  W/Up - Move up");
            Console.WriteLine("  S/Down - Move down");
            Console.WriteLine("  A/Left - Move left");
            Console.WriteLine("  D/Right - Move right");
            Console.WriteLine("  1/2/3/4 - Cast skill");
            Console.WriteLine("  SPACE - Stop");
            Console.WriteLine("  Q - Quit");
            Console.WriteLine("========================================");
            Console.WriteLine();
        }

        /// <summary>
        /// Add unit view
        /// </summary>
        public static void AddUnitView(this ETBattleViewComponent self, ActorSpawnEvent spawnEvent)
        {
            if (self.UnitViewComponent == null)
            {
                Log.Warning("[ETBattleView] UnitViewComponent is null, cannot add unit view");
                return;
            }

            var unitData = new ETUnitViewComponent.UnitViewData
            {
                ActorId = spawnEvent.ActorId,
                Name = spawnEvent.Name,
                Kind = spawnEvent.Kind,
                X = spawnEvent.X,
                Y = spawnEvent.Y,
                Hp = spawnEvent.MaxHp,
                MaxHp = spawnEvent.MaxHp,
                IsDead = false,
                IsLocalPlayer = spawnEvent.IsLocalPlayer,
                RenderX = spawnEvent.X,
                RenderY = spawnEvent.Y,
                LastUpdateTime = Environment.TickCount64
            };

            self.UnitViewComponent.UnitViews[spawnEvent.ActorId] = unitData;
            Log.Info($"[ETBattleView] Unit view added: {spawnEvent.Name} ({spawnEvent.ActorId}) at ({spawnEvent.X}, {spawnEvent.Y})");
        }
    }
}
