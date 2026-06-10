using System;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Marks a strongly typed plan action module for discovery.
    /// New MOBA trigger actions should inherit MobaPlanActionModuleBase<TActionArgs, TModule>
    /// and pair with a MobaPlanActionSchemaBase<TActionArgs> schema.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PlanActionModuleAttribute : Attribute
    {
        public int Order { get; }

        public PlanActionModuleAttribute(int order = 0)
        {
            Order = order;
        }
    }
    /// <summary>
    /// Central ordering table for discovered MOBA plan action modules.
    /// </summary>
    public static class MobaPlanActionModuleOrders
    {
        public const int DebugLog = 0;
        public const int SetGameplayVar = 0;
        public const int AddGameplayVar = 0;
        public const int EndGame = 0;

        public const int CancelSkill = 9;
        public const int ConsumeResource = 10;
        public const int StartCooldown = 10;
        public const int ShootProjectile = 10;
        public const int GiveDamage = 11;
        public const int TakeDamage = 12;

        public const int Dash = 13;
        public const int Blink = 14;
        public const int RemoveBuff = 14;
        public const int Pull = 15;

        public const int AddShield = 19;
        public const int AddBuff = 20;
        public const int RemoveShield = 20;

        public const int SpawnArea = 24;
        public const int SpawnSummon = 30;
        public const int RemoveSummon = 31;
        public const int RemoveArea = 32;

        public const int PlayPresentation = 40;
    }
}
