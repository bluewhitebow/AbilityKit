using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Battle driver bridge
    /// Encapsulates interaction logic with BattleDriver
    ///
    /// Design:
    /// - Only exposes input submission and lifecycle methods
    /// - Does NOT expose World access (violates single-entry principle)
    /// - All World interaction is encapsulated within ETMobaBattleDriver
    /// </summary>
    public static class ETBattleDriverBridge
    {
        /// <summary>
        /// Submit move input
        /// </summary>
        public static void SubmitMoveInput(ETBattleComponent self, long actorId, float targetX, float targetZ)
        {
            if (self.BattleDriver is ETMobaBattleDriver driver)
            {
                driver.SubmitMoveInput((int)actorId, targetX, targetZ);
            }
        }

        /// <summary>
        /// Submit skill input
        /// </summary>
        public static void SubmitSkillInput(ETBattleComponent self, long actorId, int slot, float targetX, float targetZ)
        {
            if (self.BattleDriver is ETMobaBattleDriver driver)
            {
                driver.SubmitSkillInput((int)actorId, slot, targetX, targetZ);
            }
        }

        /// <summary>
        /// Start battle driver
        /// </summary>
        public static void Start(ETBattleComponent self)
        {
            if (self.BattleDriver is ETMobaBattleDriver driver)
            {
                driver.StartBattle();
            }
        }

        /// <summary>
        /// Stop battle driver
        /// </summary>
        public static void Stop(ETBattleComponent self)
        {
            if (self.BattleDriver is ETMobaBattleDriver driver)
            {
                driver.StopBattle();
            }
        }

        /// <summary>
        /// Get current frame number
        /// </summary>
        public static int GetCurrentFrame(ETBattleComponent self)
        {
            if (self.BattleDriver is ETMobaBattleDriver driver)
            {
                return driver.CurrentFrame;
            }
            return 0;
        }

        /// <summary>
        /// Process auto test (every frame call)
        /// </summary>
        public static void ProcessAutoTest(ETBattleComponent self, int currentFrame)
        {
            var scene = self.Scene();
            var autoTest = scene?.GetComponent<ETBattleAutoTestComponent>();
            if (autoTest != null && autoTest.IsEnabled)
            {
                autoTest.OnUpdate(currentFrame);
            }
        }

        /// <summary>
        /// Process skill test (every frame call)
        /// </summary>
        public static void ProcessSkillTest(ETBattleComponent self, int currentFrame)
        {
            var scene = self.Scene();
            var skillTest = scene?.GetComponent<ETBattleSkillTestComponent>();
            if (skillTest != null && skillTest.IsEnabled)
            {
                skillTest.OnUpdate(currentFrame);
            }
        }

        /// <summary>
        /// Process input (move and skill commands)
        /// </summary>
        public static void ProcessInput(ETBattleComponent self, int currentFrame)
        {
            var scene = self.Scene();
            var inputComponent = scene?.GetComponent<ETInputComponent>();
            if (inputComponent != null)
            {
                Log.Debug($"[ETBattleDriverBridge] Processing input at frame {currentFrame}");
                ETInputComponentSystem.ProcessInput(inputComponent, currentFrame);
            }
        }
    }
}
