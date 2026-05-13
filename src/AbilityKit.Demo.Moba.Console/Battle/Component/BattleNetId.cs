namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// 实体网络 ID
    /// </summary>
    public readonly struct BattleNetId
    {
        public readonly int Value;

        public BattleNetId(int value)
        {
            Value = value;
        }

        public static BattleNetId Invalid => default;
        public override string ToString() => Value.ToString();
    }
}
