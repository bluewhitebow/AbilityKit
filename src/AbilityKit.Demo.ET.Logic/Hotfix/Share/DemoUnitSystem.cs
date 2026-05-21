namespace ET.AbilityKit.Demo.ET.Logic
{
    /// <summary>
    /// 单位实体 System
    /// </summary>
    [EntitySystemOf(typeof(DemoUnit))]
    [FriendOf(typeof(DemoUnit))]
    public static partial class DemoUnitSystem
    {
        [EntitySystem]
        private static void Awake(this DemoUnit self)
        {
        }

        /// <summary>
        /// 移动
        /// </summary>
        public static void MoveTo(this DemoUnit self, float x, float y)
        {
            self.X = x;
            self.Y = y;
            Log.Info($"[DemoUnit] {self.Name} moved to ({self.X}, {self.Y})");
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public static void TakeDamage(this DemoUnit self, float damage)
        {
            self.Hp = System.Math.Max(0, self.Hp - damage);
            Log.Info($"[DemoUnit] {self.Name} took {damage} damage, HP: {self.Hp}/{self.MaxHp}");

            if (self.IsDead)
            {
                self.OnDead();
            }
        }

        /// <summary>
        /// 死亡处理
        /// </summary>
        private static void OnDead(this DemoUnit self)
        {
            Log.Info($"[DemoUnit] {self.Name} is dead!");
            EventSystem.Instance.Publish<Scene, DemoUnitDead>(self.Scene(), new DemoUnitDead() { UnitId = self.InstanceId });
        }
    }
}
