using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.EntitasAdapters;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.Systems;

namespace ET.Logic
{
    /// <summary>
    /// 初始化处理器
    /// </summary>
    [LifecycleHandler(LifecyclePhase.Initialize)]
    public sealed class InitializeHandler : IInitializeHandler
    {
        public LifecyclePhase Phase => LifecyclePhase.Initialize;

        public void Handle(ETMobaBattleDriver driver, in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));
            if (viewSink == null)
                throw new ArgumentNullException(nameof(viewSink));

            driver.Plan = plan;
            driver.ViewSink = viewSink;
            driver.TextAssetLoader = null;
            driver.TickRate = plan.TickRate > 0 ? plan.TickRate : 30;

            try
            {
                // 初始化配置加载器
                InitializeConfigLoader(driver);

                // 初始化快照分发器
                driver.SnapshotDispatcher = new FrameSnapshotDispatcher();

                // 创建 World
                InitializeWorld(driver, plan);

                // 重置状态
                driver.CurrentFrame = 0;
                driver.LogicTimeSeconds = 0;
                driver.IsRunning = false;

                Log.Info($"[InitializeHandler] Done: TickRate={driver.TickRate}, WorldId={driver.Plan.WorldId}");
                Log.Info($"[InitializeHandler] World: {driver.World?.Id}, Services: {driver.World?.Services != null}");
            }
            catch (InvalidOperationException ex)
            {
                Log.Error($"[InitializeHandler] Configuration error: {ex.Message}");
                throw new InvalidOperationException($"Failed to initialize battle driver: {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                Log.Error($"[InitializeHandler] Invalid argument: {ex.Message}");
                throw new ArgumentException($"Failed to initialize battle driver: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Log.Error($"[InitializeHandler] Unexpected error: {ex.GetType().Name} - {ex.Message}");
                throw new InvalidOperationException($"Failed to initialize battle driver due to unexpected error", ex);
            }
        }

        private void InitializeConfigLoader(ETMobaBattleDriver driver)
        {
            try
            {
                driver.ConfigLoader = new ETConfigLoaderService(new ETTextAssetLoader(""));
                driver.ConfigLoader.LoadAll();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize config loader: {ex.Message}", ex);
            }
        }

        private void InitializeWorld(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            // 创建 WorldManager
            var worldManager = new WorldManager(BattleWorldFactory.Instance);
            driver.WorldManager = worldManager;

            // 创建 HostRuntime（传入 WorldManager）
            driver.HostRuntime = new HostRuntime(worldManager);

            // 重要：模块注册顺序很重要！
            // 目标：让 ETMobaInputSink 被使用（MobaEntityManager 会被正确解析）
            // 
            // 方案：BattleServiceModule 先注册 ETMobaInputSink，
            //       MobaWorldBootstrapModule 后注册但使用 TryRegisterType 跳过已存在的服务
            var modules = new List<IWorldModule>
            {
                // BattleServiceModule 先注册 ETMobaInputSink
                new BattleServiceModule(),
                // MobaWorldBootstrapModule 后注册，MobaLobbyInputSink 会被跳过
                new MobaWorldBootstrapModule()
            };

            // 创建 WorldCreateOptions
            var options = new WorldCreateOptions
            {
                Id = new WorldId($"battle-{plan.WorldId}"),
                WorldType = BattleWorldTypes.Battle
            };

            // 设置 Entitas 上下文工厂（必须！用于创建 EntitasWorld）
            options.SetEntitasContextsFactory(new MobaEntitasContextsFactory());

            // 添加模块
            foreach (var module in modules)
            {
                options.Modules.Add(module);
            }

            // 通过 HostRuntime 创建 World
            var world = driver.HostRuntime.CreateWorld(options);
            if (world == null)
            {
                throw new InvalidOperationException($"Failed to create world with options: Id={options.Id}, Type={options.WorldType}");
            }

            driver.World = world;

            Log.Info($"[InitializeHandler] World created: Id={world.Id}, Type={world.WorldType}");
            Log.Info($"[InitializeHandler] World initialized with services");
        }
    }
}
