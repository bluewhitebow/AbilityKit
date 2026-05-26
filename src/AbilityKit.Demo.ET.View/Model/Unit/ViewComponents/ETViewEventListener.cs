using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.View
{
    /// <summary>
    /// 视图层事件监听器
    /// 监听实体创建销毁等事件
    ///
    /// Design:
    /// - 纯数据 Component
    /// - Handler 更新数据
    /// - 双字典设计：
    ///   - _unitViews: ActorId -> ETUnitViewComponent (用于逻辑层事件)
    ///   - _entityIdToActorId: EntityId -> ActorId (用于 ET 内部操作)
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETViewEventListener: Entity, IAwake
    {
        // Unit view data dictionary: ActorId -> ETUnitViewComponent
        private readonly Dictionary<int, ETUnitViewComponent> _unitViews = new();

        // EntityId to ActorId mapping for ET internal operations
        private readonly Dictionary<long, int> _entityIdToActorId = new();

        public IReadOnlyDictionary<int, ETUnitViewComponent> UnitViews => _unitViews;

        public void Awake()
        {
        }

        /// <summary>
        /// Add unit view with EntityId mapping
        /// </summary>
        public void AddUnitView(int actorId, ETUnitViewComponent view, long entityId = 0)
        {
            _unitViews[actorId] = view;
            if (entityId > 0)
            {
                _entityIdToActorId[entityId] = actorId;
            }
            Log.Info($"[ETViewEventListener] Unit view added: ActorId={actorId}, EntityId={entityId}, Name={view.Name}");
        }

        /// <summary>
        /// Remove unit view
        /// </summary>
        public void RemoveUnitView(int actorId)
        {
            _unitViews.Remove(actorId);
            // Also remove from entityId mapping
            long entityIdToRemove = 0;
            foreach (var kv in _entityIdToActorId)
            {
                if (kv.Value == actorId)
                {
                    entityIdToRemove = kv.Key;
                    break;
                }
            }
            if (entityIdToRemove > 0)
            {
                _entityIdToActorId.Remove(entityIdToRemove);
            }
        }

        /// <summary>
        /// Get unit view by ActorId (moba.core logic layer ID)
        /// </summary>
        public ETUnitViewComponent GetUnitView(int actorId)
        {
            return _unitViews.TryGetValue(actorId, out var view) ? view : null;
        }

        /// <summary>
        /// Get unit view by EntityId (ET framework internal ID)
        /// </summary>
        public ETUnitViewComponent GetUnitViewByEntityId(long entityId)
        {
            if (_entityIdToActorId.TryGetValue(entityId, out var actorId))
            {
                return _unitViews.TryGetValue(actorId, out var view) ? view : null;
            }
            return null;
        }

        /// <summary>
        /// Get ActorId by EntityId
        /// </summary>
        public int GetActorId(long entityId)
        {
            return _entityIdToActorId.TryGetValue(entityId, out var actorId) ? actorId : 0;
        }
    }
}
