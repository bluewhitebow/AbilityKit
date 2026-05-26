namespace ET.AbilityKit.Demo.View
{
    /// <summary>
    /// 单位视图组件
    /// 负责渲染单个单位的表现
    ///
    /// Design:
    /// - 纯数据类，只存储渲染所需的状态
    /// - UnitId: ET 框架的 Entity.Id（由 ET 框架自动生成）
    /// - MobaActorId: moba.core 逻辑层的 ActorId（运行时自增 ID）
    /// - 不包含任何日志调用，日志应在 Handlers 中输出
    /// </summary>
    public class ETUnitViewComponent
    {
        /// <summary>
        /// ET 框架实体 ID（由 ET 框架自动生成）
        /// 用于 ET 内部操作
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// moba.core 逻辑层的 ActorId（运行时自增 ID）
        /// 用于与逻辑层交互
        /// </summary>
        public int MobaActorId { get; set; }

        /// <summary>
        /// 单位名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// X 坐标
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y 坐标
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// 当前 HP
        /// </summary>
        public float CurrentHp { get; set; }

        /// <summary>
        /// 最大 HP
        /// </summary>
        public float MaxHp { get; set; }

        /// <summary>
        /// HP 条宽度
        /// </summary>
        public float HpBarWidth { get; set; } = 50f;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 是否死亡
        /// </summary>
        public bool IsDead { get; set; } = false;

        /// <summary>
        /// 旋转角度
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// 实体代码
        /// </summary>
        public int EntityCode { get; set; }

        /// <summary>
        /// 更新位置（不含日志）
        /// </summary>
        public void UpdatePosition(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 更新旋转（不含日志）
        /// </summary>
        public void UpdateRotation(float rotation)
        {
            Rotation = rotation;
        }

        /// <summary>
        /// 更新 HP（不含日志）
        /// </summary>
        public void UpdateHp(float currentHp, float maxHp)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }

        /// <summary>
        /// 单位死亡（不含日志）
        /// </summary>
        public void OnDead()
        {
            IsDead = true;
            IsVisible = false;
        }

        /// <summary>
        /// 单位复活（不含日志）
        /// </summary>
        public void OnRevive()
        {
            IsDead = false;
            IsVisible = true;
        }

        /// <summary>
        /// 显示伤害飘字（不含日志）
        /// </summary>
        public void ShowDamage(float damage)
        {
        }

        /// <summary>
        /// 显示治疗飘字（不含日志）
        /// </summary>
        public void ShowHeal(float healAmount)
        {
        }

        /// <summary>
        /// 播放特效（不含日志）
        /// </summary>
        public void PlayVfx(string vfxId, float duration)
        {
        }

        /// <summary>
        /// 播放音效（不含日志）
        /// </summary>
        public void PlaySfx(string sfxId)
        {
        }
    }
}
