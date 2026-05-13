namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// 实体种类
    /// </summary>
    public enum BattleEntityKind
    {
        Unknown = 0,
        Character = 1,
        Projectile = 2,
        Vfx = 3
    }

    /// <summary>
    /// 实体元数据组�?
    /// </summary>
    public sealed class BattleEntityMetaComponent
    {
        public BattleEntityKind Kind = BattleEntityKind.Unknown;
        public int EntityCode;
    }
}
