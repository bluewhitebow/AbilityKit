using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 单位视图组件 - 只定义数�?
    /// 对应 Moba.Console �?ConsoleViewBinder
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETUnitViewComponent: Entity, IAwake
    {
        // 视图配置
        public int ViewWidth { get; set; } = 80;
        public int ViewHeight { get; set; } = 30;

        // 单位视图数据
        public Dictionary<long, UnitViewData> UnitViews { get; set; } = new();

        public void Awake()
        {
        }

        /// <summary>
        /// 单位视图数据
        /// </summary>
        public class UnitViewData
        {
            public long ActorId;
            public string Name;
            public ActorKind Kind;
            public float X;
            public float Y;
            public float Hp;
            public float MaxHp;
            public bool IsDead;
            public bool IsLocalPlayer;

            // 插值相�?
            public float RenderX;
            public float RenderY;
            public float LastUpdateTime;
        }
    }
}
