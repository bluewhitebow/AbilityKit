using System;

namespace ET.AbilityKit.Demo.ET.App
{
    /// <summary>
    /// ET Demo 入口程序
    /// 使用 ET 标准的 Entry 系统
    /// </summary>
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== AbilityKit ET Demo ===");
            Console.WriteLine("Starting ET Framework with Demo Process Component...");
            Console.WriteLine();

            try
            {
                // 初始化核心系统
                DemoEntry.Init(args);

                // 启动 Main Fiber 并等待完成
                DemoEntry.StartAsync().NoContext();

                Console.WriteLine();
                Console.WriteLine("=== ET Framework Started ===");
                Console.WriteLine("Press Ctrl+C to exit.");
                Console.WriteLine();

                // 主循环：持续调用 FiberManager.Update()
                while (true)
                {
                    try
                    {
                        // 更新 Fiber 管理器
                        global::ET.FiberManager.Instance.Update();
                        global::ET.FiberManager.Instance.LateUpdate();

                        // 简单帧率控制（约 60 FPS）
                        System.Threading.Thread.Sleep(16);
                    }
                    catch (Exception ex)
                    {
                        global::ET.Log.Error($"Main loop error: {ex}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("=== ET Framework Initialization Failed ===");
                Console.WriteLine($"Error: {e.Message}");
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
