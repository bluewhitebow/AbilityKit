using System;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Flow;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// 同步模块
    /// </summary>
    public sealed class ConsoleSyncFeature : IGameModule<ConsoleBattleContext>, IGameModuleTick<ConsoleBattleContext>
    {
        private ConsoleBattleContext _ctx;
        private bool _initialized;

        public void OnAttach(ConsoleBattleContext context)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
            _initialized = true;
            Platform.Log.Sync($"[Sync] Attached - SyncMode: {_ctx.Plan.SyncMode}");
        }

        public void OnDetach(ConsoleBattleContext context)
        {
            _ctx = null;
            _initialized = false;
            Platform.Log.Sync("[Sync] Detached");
        }

        public void Tick(ConsoleBattleContext context, float deltaTime)
        {
            if (!_initialized || _ctx == null) return;
        }
    }
}
