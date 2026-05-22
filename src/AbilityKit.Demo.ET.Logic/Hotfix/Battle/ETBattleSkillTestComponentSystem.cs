using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleSkillTestComponent System
    /// Handles skill test logic
    /// </summary>
    [EntitySystemOf(typeof(ETBattleSkillTestComponent))]
    [FriendOf(typeof(ETBattleSkillTestComponent))]
    [FriendOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    public static partial class ETBattleSkillTestComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleSkillTestComponent self)
        {
            Log.Info("[ETBattleSkillTest] System awake");
        }

        [EntitySystem]
        private static void Update(this ETBattleSkillTestComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ETBattleSkillTestComponent self)
        {
            Log.Info($"[ETBattleSkillTest] System destroyed. Total skill casts: {self.SkillCastCount}");
        }

        /// <summary>
        /// Enable skill test
        /// </summary>
        public static void Enable(this ETBattleSkillTestComponent self)
        {
            self.IsEnabled = true;
            Log.Info("[ETBattleSkillTest] Skill test enabled");
        }

        /// <summary>
        /// Disable skill test
        /// </summary>
        public static void Disable(this ETBattleSkillTestComponent self)
        {
            self.IsEnabled = false;
            Log.Info("[ETBattleSkillTest] Skill test disabled");
        }
    }
}
