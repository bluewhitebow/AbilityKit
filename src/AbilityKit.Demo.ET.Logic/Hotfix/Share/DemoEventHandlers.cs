using System;
using System.Threading.Tasks;

namespace ET.AbilityKit.Demo.ET.Logic
{
    /// <summary>
    /// Demo 登录完成事件处理
    /// </summary>
    [Event(SceneType.DemoLogin)]
    public class DemoLoginFinish_EventHandler: AEvent<Scene, DemoLoginFinish>
    {
        protected override async ETTask Run(Scene scene, DemoLoginFinish args)
        {
            Console.WriteLine($"[DemoLoginHandler] Login finished for {args.PlayerName}, ready to enter battle!");
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// Demo 请求进入战斗事件处理
    /// </summary>
    [Event(SceneType.DemoLogin)]
    public class DemoRequestEnterBattle_EventHandler: AEvent<Scene, DemoRequestEnterBattle>
    {
        protected override async ETTask Run(Scene scene, DemoRequestEnterBattle args)
        {
            Console.WriteLine($"[DemoLoginHandler] Requesting to enter battle for {args.PlayerName}...");
            
            // 获取根场景的 DemoProcessComponent
            var root = scene.Root();
            var processComponent = root?.GetComponent<DemoProcessComponent>();
            
            if (processComponent != null)
            {
                await processComponent.ChangeToBattleScene(args.PlayerId, args.PlayerName);
            }
            else
            {
                Console.WriteLine("[DemoLoginHandler] DemoProcessComponent not found!");
            }
            
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// Demo 战斗结束事件处理
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class DemoBattleEnd_EventHandler: AEvent<Scene, BattleEnd>
    {
        protected override async ETTask Run(Scene scene, BattleEnd args)
        {
            Console.WriteLine($"[DemoBattleHandler] Battle {args.BattleId} ended with {(args.IsVictory ? "VICTORY" : "DEFEAT")}");
            
            // 获取根场景的 DemoProcessComponent
            var root = scene.Root();
            var processComponent = root?.GetComponent<DemoProcessComponent>();
            
            if (processComponent != null)
            {
                Console.WriteLine("[DemoBattleHandler] Returning to login scene in 5 seconds...");
                await Task.Delay(5000);
                await processComponent.ChangeToLoginScene();
            }
            
            await ETTask.CompletedTask;
        }
    }
}
