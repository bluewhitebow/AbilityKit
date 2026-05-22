using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 视图事件处理
    /// 订阅逻辑层事件并转发到视图组�?
    /// </summary>
    public static class ETViewEventHandler
    {
        /// <summary>
        /// 处理单位生成事件
        /// </summary>
        public static void HandleActorSpawn(Scene scene, ActorSpawnEvent evt)
        {
            var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.CreateUnitView(evt);
        }

        /// <summary>
        /// 处理单位死亡事件
        /// </summary>
        public static void HandleActorDead(Scene scene, ActorDeadEvent evt)
        {
            var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.DestroyUnitView(evt.ActorId);
        }

        /// <summary>
        /// 处理单位移动事件
        /// </summary>
        public static void HandleActorMove(Scene scene, ActorMoveEvent evt)
        {
            var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.UpdateUnitPosition(evt);
        }

        /// <summary>
        /// 处理单位受伤事件
        /// </summary>
        public static void HandleActorDamage(Scene scene, ActorDamageEvent evt)
        {
            var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.UpdateUnitHp(evt);

            // 显示飘字
            Console.WriteLine($"[DAMAGE] {evt.ActorId} took {evt.Damage:F0} damage! HP: {evt.CurrentHp:F0}/{evt.MaxHp}");
        }

        /// <summary>
        /// 处理战斗开始事�?
        /// </summary>
        public static void HandleBattleStart(Scene scene, BattleStartEvent evt)
        {
            Console.WriteLine("========================================");
            Console.WriteLine($"[BATTLE] Battle {evt.BattleId} STARTED!");
            Console.WriteLine("========================================");
        }

        /// <summary>
        /// 处理战斗结束事件
        /// </summary>
        public static void HandleBattleEnd(Scene scene, BattleEndEvent evt)
        {
            Console.WriteLine("========================================");
            Console.WriteLine($"[BATTLE] Battle {evt.BattleId} ENDED: {(evt.IsVictory ? "VICTORY" : "DEFEAT")}");
            Console.WriteLine("========================================");
        }
    }
}
