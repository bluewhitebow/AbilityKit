using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Entities;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.Context
{
    /// <summary>
    /// 战斗上下文
    /// 管理表现层状态，不涉及逻辑执行
    /// </summary>
    public sealed class ConsoleBattleContext : Flow.IModuleContext, IDisposable
    {
        /// <summary>
        /// ECS 世界
        /// </summary>
        public EC.EntityWorld EcsWorld { get; private set; }

        /// <summary>
        /// 实体节点
        /// </summary>
        public EC.IEntity EntityNode { get; private set; }

        /// <summary>
        /// 实体查找器
        /// </summary>
        public BattleEntityLookup EntityLookup { get; private set; }

        /// <summary>
        /// 实体工厂
        /// </summary>
        public BattleEntityFactory EntityFactory { get; private set; }

        /// <summary>
        /// 战斗启动计划
        /// </summary>
        public BattleStartPlan Plan { get; set; }

        /// <summary>
        /// 本地玩家 ID
        /// </summary>
        public int LocalActorId { get; set; }

        /// <summary>
        /// 玩家数量
        /// </summary>
        public int PlayerCount { get; set; }

        /// <summary>
        /// 当前帧
        /// </summary>
        public int LastFrame { get; set; }

        /// <summary>
        /// 逻辑时间（秒）
        /// </summary>
        public double LogicTimeSeconds { get; set; }

        /// <summary>
        /// HUD 移动输入
        /// </summary>
        public float HudMoveDx { get; set; }
        public float HudMoveDz { get; set; }
        public bool HudHasMove { get; set; }

        /// <summary>
        /// HUD 技能点击输入
        /// </summary>
        public int HudSkillClickSlot { get; set; }

        /// <summary>
        /// HUD 技能瞄准输入
        /// </summary>
        public bool HudSkillAiming { get; set; }
        public int HudSkillAimSlot { get; set; }
        public float HudSkillAimDx { get; set; }
        public float HudSkillAimDz { get; set; }

        /// <summary>
        /// HUD 技能瞄准释放输入
        /// </summary>
        public bool HudSkillAimSubmit { get; set; }
        public int HudSkillAimSubmitSlot { get; set; }
        public float HudSkillAimSubmitDx { get; set; }
        public float HudSkillAimSubmitDz { get; set; }

        /// <summary>
        /// 战斗状态
        /// </summary>
        public BattleState State { get; set; } = BattleState.Idle;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// 初始化 ECS 世界
        /// </summary>
        public void InitializeEcsWorld()
        {
            EcsWorld = new EC.EntityWorld();
            EntityNode = EcsWorld.Create("BattleEntities");
            EntityLookup = new BattleEntityLookup();
            EntityFactory = new BattleEntityFactory(EcsWorld, EntityLookup, EntityNode);

            Platform.Log.Entity("ECS World initialized");
        }

        /// <summary>
        /// 重置 HUD 状态
        /// </summary>
        public void ResetHudState()
        {
            HudMoveDx = 0f;
            HudMoveDz = 0f;
            HudHasMove = false;
            HudSkillClickSlot = 0;
            HudSkillAiming = false;
            HudSkillAimSlot = 0;
            HudSkillAimDx = 0f;
            HudSkillAimDz = 0f;
            HudSkillAimSubmit = false;
            HudSkillAimSubmitSlot = 0;
            HudSkillAimSubmitDx = 0f;
            HudSkillAimSubmitDz = 0f;
        }

        /// <summary>
        /// 重置上下文
        /// </summary>
        public void Reset()
        {
            Plan = default;
            LocalActorId = 0;
            PlayerCount = 0;
            LastFrame = 0;
            LogicTimeSeconds = 0d;
            ResetHudState();
            State = BattleState.Idle;
            IsInitialized = false;

            EntityLookup?.Clear();
            EntityFactory = null;
            EntityNode = default;
            EcsWorld = null;
        }

        public void Dispose()
        {
            Reset();
        }
    }

    /// <summary>
    /// 战斗状态
    /// </summary>
    public enum BattleState
    {
        Idle = 0,
        Prepare = 1,
        Connect = 2,
        CreateOrJoinWorld = 3,
        LoadAssets = 4,
        InMatch = 5,
        End = 6
    }
}
