using AbilityKit.Ability.World.DI;

namespace ET.Logic
{
    /// <summary>
    /// moba.core World 解析器组�?
    /// 持有 IWorldResolver 引用，用于访�?moba.core 服务
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETWorldResolverComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// moba.core �?IWorldResolver
        /// </summary>
        public IWorldResolver Resolver { get; set; }

        /// <summary>
        /// moba.core World 初始化器（持有资源，需要在组件销毁时释放�?
        /// </summary>
        public MobaCoreWorldInitializer? Initializer { get; set; }

        public void Awake()
        {
            Log.Info("[ETWorldResolver] ETWorldResolverComponent awake");
        }

        public void OnDestroy(ETWorldResolverComponent self)
        {
            // 释放 moba.core World 资源
            self.Initializer?.Dispose();
            self.Initializer = null;
            self.Resolver = null;
            Log.Info("[ETWorldResolver] ETWorldResolverComponent destroyed");
        }
    }
}
