namespace ET.AbilityKit.Demo.ET.Logic
{
    /// <summary>
    /// DemoProcessComponent System
    /// </summary>
    [EntitySystemOf(typeof(DemoProcessComponent))]
    [FriendOf(typeof(DemoProcessComponent))]
    public static partial class DemoProcessComponentSystem
    {
        [EntitySystem]
        private static void Awake(this DemoProcessComponent self)
        {
            Log.Info($"[DemoProcess] DemoProcessComponent awake");
        }

        [EntitySystem]
        private static void Update(this DemoProcessComponent self)
        {
            // 更新战斗组件 (BattleTime 会由 Fiber 传入，这里模拟一下)
            // 实际的 deltaTime 由 UpdateEvent 提供
            var battleComponent = self.CurrentScene?.GetComponent<DemoBattleComponent>();
            if (battleComponent != null && battleComponent.State == DemoBattleState.InProgress)
            {
                // 战斗更新逻辑在 DemoBattleComponentSystem.Update 中处理
            }
        }

        /// <summary>
        /// 切换到登录场景
        /// </summary>
        public static async ETTask ChangeToLoginScene(this DemoProcessComponent self)
        {
            var root = self.Root();
            if (root == null)
            {
                Log.Error($"[DemoProcess] Root scene is null!");
                return;
            }

            // 移除之前的子场景
            foreach (var child in root.Children.Values)
            {
                if (child is Scene scene && scene.SceneType != 0)
                {
                    child.Dispose();
                }
            }

            // 创建登录场景
            var loginScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoLogin,
                "DemoLogin");

            // 添加登录组件
            self.LoginComponent = loginScene.AddComponent<DemoLoginComponent>();

            self.CurrentScene = loginScene;

            Log.Info($"[DemoProcess] Changed to Login scene");
        }

        /// <summary>
        /// 切换到战斗场景
        /// </summary>
        public static async ETTask ChangeToBattleScene(this DemoProcessComponent self, long playerId, string playerName)
        {
            var root = self.Root();
            if (root == null)
            {
                Log.Error($"[DemoProcess] Root scene is null!");
                return;
            }

            // 移除之前的子场景
            foreach (var child in root.Children.Values)
            {
                if (child is Scene scene && scene.SceneType != 0)
                {
                    child.Dispose();
                }
            }

            // 创建战斗场景
            var battleScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoBattle,
                "DemoBattle");

            // 初始化战斗
            var battleComponent = battleScene.AddComponent<DemoBattleComponent>();
            battleComponent.InitializeBattle(playerId, playerName);
            battleComponent.StartBattle();

            self.CurrentScene = battleScene;
            self.LoginComponent = null;

            Log.Info($"[DemoProcess] Changed to Battle scene");
        }
    }
}
