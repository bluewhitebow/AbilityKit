using System;
using System.Reflection;

namespace ET.AbilityKit.Demo.ET.App
{
    /// <summary>
    /// Demo Entry 初始化
    /// 简化的初始化流程
    /// </summary>
    public static class DemoEntry
    {
        public static void Init(string[] args)
        {
            Console.WriteLine("[DemoEntry] Initializing...");
            
            // 设置命令行参数 (使用 ET 框架的 Options Singleton)
            global::ET.Options.Instance.SceneName = "Demo";
            global::ET.Options.Instance.Process = 1;
            
            Console.WriteLine("[DemoEntry] Options initialized");
            
            // 初始化 WinPeriod
            global::ET.WinPeriod.Init();
            
            Console.WriteLine("[DemoEntry] WinPeriod initialized");
            
            // 加载程序集并初始化 CodeTypes
            var assemblies = new Assembly[]
            {
                typeof(DemoEntry).Assembly, // App
                typeof(global::ET.AbilityKit.Demo.ET.Logic.DemoLoginComponent).Assembly, // Logic
                typeof(global::ET.AbilityKit.Demo.ET.Logic.DemoProcessComponent).Assembly, // Logic
            };
            
            Console.WriteLine("[DemoEntry] Loading assemblies...");
            foreach (var ass in assemblies)
            {
                Console.WriteLine($"[DemoEntry]   - {ass.GetName().Name}");
            }
            
            try
            {
                Console.WriteLine("[DemoEntry] Calling CodeTypes.Instance.Awake...");
                global::ET.CodeTypes.Instance.Awake(assemblies);
                Console.WriteLine("[DemoEntry] CodeTypes initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DemoEntry] CodeTypes init failed (expected): {ex.Message}");
            }
            
            // 添加核心 Singletons
            Console.WriteLine("[DemoEntry] Adding core singletons...");
            global::ET.World.Instance.AddSingleton<global::ET.SceneTypeSingleton, global::System.Type>(typeof(global::ET.SceneType));
            global::ET.World.Instance.AddSingleton<global::ET.ObjectPool>();
            global::ET.World.Instance.AddSingleton<global::ET.IdGenerater>();
            global::ET.World.Instance.AddSingleton<global::ET.FiberManager>();
            global::ET.World.Instance.AddSingleton<global::ET.TimeInfo>();
            
            Console.WriteLine("[DemoEntry] Core singletons initialized (basic)");
        }
        
        public static async global::ET.ETTask StartAsync()
        {
            Console.WriteLine("[DemoEntry] Creating Main Fiber...");
            
            // 创建 Main Fiber
            await global::ET.FiberManager.Instance.Create(
                global::ET.SchedulerType.Main, 
                global::ET.SceneType.Main, 
                0, 
                global::ET.SceneType.Main, 
                "Demo");
            
            Console.WriteLine("[DemoEntry] Main Fiber created");
        }
    }
}
