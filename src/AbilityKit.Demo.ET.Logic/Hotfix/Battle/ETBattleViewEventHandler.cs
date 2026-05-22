using System;
using System.Threading.Tasks;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Battle view event handler
    /// Subscribes to logic layer events and creates units
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_EventHandler: AEvent<Scene, ActorSpawnEvent>
    {
        protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
        {
            // Create Logic layer unit
            var unitComponent = scene.GetComponent<ETUnitComponent>();
            if (unitComponent != null)
            {
                unitComponent.CreateUnit(
                    args.ActorId,
                    args.EntityCode,
                    args.Kind,
                    args.Name,
                    args.X,
                    args.Y,
                    args.MaxHp);
                Log.Info($"[ETBattleView] Logic unit created: {args.Name} ({args.ActorId})");
            }
            else
            {
                Log.Warning($"[ETBattleView] ETUnitComponent not found!");
            }

            // Create View layer unit view
            var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.CreateUnitView(args);

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Unit move event handler
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorMove_Handler: AEvent<Scene, ActorMoveEvent>
    {
        protected override async ETTask Run(Scene scene, ActorMoveEvent args)
        {
            var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.UpdateUnitPosition(args);
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Unit damage event handler
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorDamage_Handler: AEvent<Scene, ActorDamageEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDamageEvent args)
        {
            var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.UpdateUnitHp(args);
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Unit dead event handler
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorDead_Handler: AEvent<Scene, ActorDeadEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDeadEvent args)
        {
            var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.DestroyUnitView(args.ActorId);
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Battle start event handler
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_BattleStart_Handler: AEvent<Scene, BattleStartEvent>
    {
        protected override async ETTask Run(Scene scene, BattleStartEvent args)
        {
            var battleViewComponent = scene.GetComponent<ETBattleViewComponent>();
            battleViewComponent?.OnBattleStart(args);
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Battle end event handler
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_BattleEnd_Handler: AEvent<Scene, BattleEndEvent>
    {
        protected override async ETTask Run(Scene scene, BattleEndEvent args)
        {
            var battleViewComponent = scene.GetComponent<ETBattleViewComponent>();
            battleViewComponent?.OnBattleEnd(args);
            await ETTask.CompletedTask;
        }
    }
}
