using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Battle view component
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleViewComponent: Entity, IAwake, IUpdate
    {
        // View state
        public bool IsInitialized { get; set; }
        public int RenderFrameCount { get; set; }
        public long LastRenderTime { get; set; }

        // Battle ID
        public long BattleId { get; set; }

        // View config
        public int TargetFps { get; set; } = 30;
        public float RenderInterval => 1f / TargetFps;

        // View component reference
        public ETUnitViewComponent UnitViewComponent { get; set; }

        public void Awake()
        {
            Log.Info("[ETBattleView] ETBattleViewComponent awake");
        }

        public void Update(ETBattleViewComponent self)
        {
            // Update unit view
            self.UnitViewComponent?.Tick(1f / 30f);

            // Render view
            self.Render();
        }

        private void Render()
        {
            UnitViewComponent?.Render();
        }

        /// <summary>
        /// Initialize view
        /// </summary>
        public void Initialize()
        {
            IsInitialized = true;
            RenderFrameCount = 0;
            LastRenderTime = Environment.TickCount64;

            // Create unit view component
            UnitViewComponent = this.AddComponent<ETUnitViewComponent>();

            Log.Info("[ETBattleView] Battle view initialized");
        }

        /// <summary>
        /// Show help information
        /// </summary>
        public void ShowHelp()
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("                    BATTLE CONTROLS                          ");
            Console.WriteLine("============================================================");
            Console.WriteLine("W/UpArrow    - Move Up");
            Console.WriteLine("S/DownArrow  - Move Down");
            Console.WriteLine("A/LeftArrow  - Move Left");
            Console.WriteLine("D/RightArrow - Move Right");
            Console.WriteLine("1/2/3/4      - Cast Skill");
            Console.WriteLine("Space        - Stop");
            Console.WriteLine("Q            - Quit");
            Console.WriteLine("============================================================");
        }

        /// <summary>
        /// Handle battle start
        /// </summary>
        public void OnBattleStart(BattleStartEvent evt)
        {
            BattleId = evt.BattleId;
            Log.Info($"[ETBattleView] Battle started: {evt.BattleId}");
        }

        /// <summary>
        /// Handle battle end
        /// </summary>
        public void OnBattleEnd(BattleEndEvent evt)
        {
            Log.Info($"[ETBattleView] Battle ended: {evt.BattleId}, Victory: {evt.IsVictory}");

            // Show final result
            RenderFinalResult(evt.IsVictory);
        }

        /// <summary>
        /// Render final result
        /// </summary>
        public void RenderFinalResult(bool isVictory)
        {
            Console.WriteLine("========================================");
            Console.WriteLine($"[RESULT] {(isVictory ? "VICTORY!" : "DEFEAT!")}");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
        }

        /// <summary>
        /// Add unit view
        /// </summary>
        public void AddUnitView(ActorSpawnEvent spawnEvent)
        {
            if (UnitViewComponent == null)
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

            UnitViewComponent.UnitViews[spawnEvent.ActorId] = unitData;
            Log.Info($"[ETBattleView] Unit view added: {spawnEvent.Name} ({spawnEvent.ActorId}) at ({spawnEvent.X}, {spawnEvent.Y})");
        }
    }
}
