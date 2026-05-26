using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.View
{
    /// <summary>
    /// ActorSpawnEvent 事件处理器
    /// 订阅 ET.Logic 发布的事件，创建视图层单位
    ///
    /// Design:
    /// - 使用 ET.Entity.Id 标识 ET 框架中的实体
    /// - 使用 ActorId（MobaActorId）作为 moba.core 逻辑层标识
    /// - View 层字典使用 ActorId 作为 key
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorSpawnEventHandler : AEvent<Scene, ActorSpawnEvent>
    {
        protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
        {
            // 创建 ET.Entity 用于视图层标识
            // EntityId 由 ET 框架自动生成，与 moba.core 的 ActorId 无关
            var entity = scene.AddChild<ETUnitViewEntity>();

            // Get or create view event listener
            var listener = scene.GetComponent<ETViewEventListener>();
            if (listener == null)
            {
                listener = scene.AddComponent<ETViewEventListener>();
            }

            // Create unit view with ET.EntityId
            var view = new ETUnitViewComponent
            {
                UnitId = entity.Id,  // ET 框架 Entity.Id
                MobaActorId = args.ActorId,  // moba.core ActorId
                Name = args.Name,
                X = args.X,
                Y = args.Y,
                CurrentHp = args.MaxHp,
                MaxHp = args.MaxHp,
                EntityCode = args.EntityCode,
                IsDead = false,
                IsVisible = true
            };

            // 将 ET.Entity.Id 与 ActorId 映射存储
            listener.AddUnitView(args.ActorId, view, entity.Id);

            Log.Info($"[ActorSpawnEventHandler] Unit spawned in view: {args.Name} (EntityId={entity.Id}, ActorId={args.ActorId}, EntityCode={args.EntityCode}) at ({args.X}, {args.Y})");
        }
    }
}
