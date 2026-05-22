using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleDriverBridge System
    /// Handles frame update and auto test
    /// </summary>
    [EntitySystemOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETBattleAutoTestComponent))]
    [FriendOf(typeof(ETBattleSkillTestComponent))]
    [FriendOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETBattleDriverBridgeSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this ETBattleComponent self)
        {
            int currentFrame = ETBattleDriverBridge.GetCurrentFrame(self);

            // Process skill test
            ETBattleDriverBridge.ProcessSkillTest(self, currentFrame);

            // Process input (move and skill commands)
            ETBattleDriverBridge.ProcessInput(self, currentFrame);

            // Process auto test (move)
            ETBattleDriverBridge.ProcessAutoTest(self, currentFrame);
        }

        [EntitySystem]
        private static void Destroy(this ETBattleComponent self)
        {
        }
    }
}
