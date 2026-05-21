using System;

namespace ET.AbilityKit.Demo.ET.Logic
{
    /// <summary>
    /// Demo 场景初始化处理器
    /// 在 Main Fiber 创建后初始化 Demo 流程
    /// </summary>
    [Invoke(SceneType.Main)]
    public class FiberInit_DemoMain: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            
            Log.Info($"[Demo] Main fiber initialized");
            
            // 设置场景类型
            root.SceneType = SceneType.Main;
            
            // 发布 Entry 事件
            await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
            await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
            await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
            
            // 初始化 Demo 流程组件
            root.AddComponent<DemoProcessComponent>();
            
            // 切换到登录场景
            var processComponent = root.GetComponent<DemoProcessComponent>();
            if (processComponent != null)
            {
                await processComponent.ChangeToLoginScene();
            }
            
            Log.Info($"[Demo] Demo process initialized successfully");
        }
    }
}
