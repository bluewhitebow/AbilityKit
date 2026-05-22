using System;
using System.Collections.Generic;
using System.Linq;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 单位视图组件 System
    /// 对应 Moba.Console �?ConsoleViewBinder + ConsoleEntityDisplayService
    /// </summary>
    [EntitySystemOf(typeof(ETUnitViewComponent))]
    [FriendOf(typeof(ETUnitViewComponent))]
    public static partial class ETUnitViewComponentSystem
    {
        private static long _startTime;

        [EntitySystem]
        private static void Awake(this ETUnitViewComponent self)
        {
            _startTime = Environment.TickCount64;
            Log.Info("[ETUnitView] ETUnitViewComponent awake");
        }

        private static long GetCurrentTimeMs()
        {
            return Environment.TickCount64 - _startTime;
        }

        /// <summary>
        /// 创建单位视图
        /// </summary>
        public static void CreateUnitView(this ETUnitViewComponent self, ActorSpawnEvent evt)
        {
            var viewData = new ETUnitViewComponent.UnitViewData
            {
                ActorId = evt.ActorId,
                Name = evt.Name,
                Kind = evt.Kind,
                X = evt.X,
                Y = evt.Y,
                RenderX = evt.X,
                RenderY = evt.Y,
                Hp = evt.MaxHp,
                MaxHp = evt.MaxHp,
                IsDead = false,
                IsLocalPlayer = evt.IsLocalPlayer,
                LastUpdateTime = GetCurrentTimeMs()
            };

            self.UnitViews[evt.ActorId] = viewData;

            Log.Info($"[ETUnitView] Unit view created: {evt.Name} ({evt.ActorId}) at ({evt.X:F1}, {evt.Y:F1})");
        }

        /// <summary>
        /// 销毁单位视�?
        /// </summary>
        public static void DestroyUnitView(this ETUnitViewComponent self, long actorId)
        {
            if (self.UnitViews.TryGetValue(actorId, out var viewData))
            {
                viewData.IsDead = true;
                Log.Info($"[ETUnitView] Unit view destroyed: {viewData.Name} ({actorId})");
            }
        }

        /// <summary>
        /// 更新单位位置
        /// </summary>
        public static void UpdateUnitPosition(this ETUnitViewComponent self, ActorMoveEvent evt)
        {
            if (self.UnitViews.TryGetValue(evt.ActorId, out var viewData))
            {
                viewData.X = evt.X;
                viewData.Y = evt.Y;
                viewData.LastUpdateTime = GetCurrentTimeMs();
            }
        }

        /// <summary>
        /// 更新单位血�?
        /// </summary>
        public static void UpdateUnitHp(this ETUnitViewComponent self, ActorDamageEvent evt)
        {
            if (self.UnitViews.TryGetValue(evt.ActorId, out var viewData))
            {
                viewData.Hp = evt.CurrentHp;
                viewData.MaxHp = evt.MaxHp;
            }
        }

        /// <summary>
        /// Tick - 更新插值渲染位�?
        /// </summary>
        public static void Tick(this ETUnitViewComponent self, float deltaTime)
        {
            float interpolationSpeed = 10f;

            foreach (var view in self.UnitViews.Values)
            {
                // 简单线性插�?
                view.RenderX += (view.X - view.RenderX) * interpolationSpeed * deltaTime;
                view.RenderY += (view.Y - view.RenderY) * interpolationSpeed * deltaTime;
            }
        }

        /// <summary>
        /// 渲染视图
        /// </summary>
        public static void Render(this ETUnitViewComponent self)
        {
            // 清屏
            Console.Clear();

            // 渲染边界
            Console.WriteLine("============================================================");
            Console.WriteLine($"Battle View - Units: {self.UnitViews.Count}");
            Console.WriteLine("============================================================");

            // �?Y 坐标排序（从上到下）
            var sortedUnits = self.UnitViews.Values
                .OrderByDescending(v => v.Y)
                .ToList();

            foreach (var view in sortedUnits)
            {
                // 单位符号
                string symbol = view.Kind switch
                {
                    ActorKind.Character => view.IsLocalPlayer ? "@" : "A",
                    ActorKind.Monster => "M",
                    _ => "?"
                };

                // 颜色
                string color;
                if (view.IsDead)
                {
                    color = "\x1b[90m"; // 灰色
                }
                else if (view.Hp < view.MaxHp * 0.3f)
                {
                    color = "\x1b[31m"; // 红色（低血量）
                }
                else
                {
                    color = "\x1b[32m"; // 绿色
                }

                string reset = "\x1b[0m";

                // 血�?
                float hpPercent = view.MaxHp > 0 ? view.Hp / view.MaxHp : 0;
                int hpBarWidth = 10;
                int filledBars = (int)(hpPercent * hpBarWidth);
                string hpBar = new string('|', filledBars) + new string('-', hpBarWidth - filledBars);

                Console.WriteLine($"{color}[{symbol}] {view.Name,-15} HP:[{hpBar}] {view.Hp:F0}/{view.MaxHp} @ ({view.RenderX:F1}, {view.RenderY:F1}){reset}");
            }

            Console.WriteLine("============================================================");
        }
    }
}
