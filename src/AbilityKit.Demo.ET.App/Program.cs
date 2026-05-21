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
                
                // 保持主线程运行
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
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
