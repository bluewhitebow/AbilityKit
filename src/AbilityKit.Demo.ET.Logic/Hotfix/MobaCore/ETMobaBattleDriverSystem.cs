using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETMobaBattleDriver System
    /// 驱动 moba.core 帧循�?
    /// </summary>
    [EntitySystemOf(typeof(ETMobaBattleDriver))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETMobaBattleDriverSystem
    {
        [EntitySystem]
        private static void Awake(this ETMobaBattleDriver self)
        {
            Log.Info("[ETMobaBattleDriver] System awake");
        }

        [EntitySystem]
        private static void Update(this ETMobaBattleDriver self)
        {
            self.Update(self);
        }

        [EntitySystem]
        private static void Destroy(this ETMobaBattleDriver self)
        {
            self.OnDestroy(self);
        }
    }
}
