using System;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Battle.View;
using AbilityKit.Game.Flow.Battle.ViewEvents;
using AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot;
using AbilityKit.Game.Flow.Battle.ViewEvents.Triggering;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal interface IViewFeatureRuntime : IViewSharedSubFeatureHost
    {
        IBattleEntityQuery Query { get; set; }
        new BattleViewBinder Binder { get; set; }
        BattleVfxManager Vfx { get; set; }
        EC.IEntity VfxNode { get; set; }
        ViewTimeline Timeline { get; set; }
        BattleFloatingTextSystem FloatingTexts { get; set; }
        BattleAreaViewSystem AreaViews { get; set; }
        IBattleViewEventSink EventSink { get; set; }
        BattleSnapshotViewAdapter SnapshotAdapter { get; set; }
        BattleTriggerEventViewAdapter TriggerAdapter { get; set; }
        IDisposable EntityDestroyedSubscription { get; set; }
        int LastAlignedFrame { get; set; }

        void OnEntityDestroyed(EC.EntityDestroyed evt);
    }

    internal static class ViewFeatureRuntimeOperations
    {
        public static void RefreshDirtyViews(IViewFeatureRuntime runtime)
        {
            if (runtime?.Query?.World == null) return;

            var battleCtx = runtime.Context;
            var dirty = battleCtx != null ? battleCtx.DirtyEntities : null;
            if (dirty == null || dirty.Count == 0) return;

            for (int i = 0; i < dirty.Count; i++)
            {
                var id = dirty[i];
                if (!runtime.Query.World.IsAlive(id)) continue;

                var entity = runtime.Query.World.Wrap(id);
                if (!entity.TryGetRef(out BattleNetIdComponent netIdComp)) continue;
                if (!entity.TryGetRef(out BattleTransformComponent t)) continue;

                runtime.Binder?.Sync(entity, battleCtx);
                RegisterSeekablesForEntity(runtime, id);
            }

            SeekAllToCurrentFrame(runtime);
            dirty.Clear();
        }

        public static void RegisterAllSeekables(IViewFeatureRuntime runtime)
        {
            if (runtime?.Timeline == null || runtime.Binder == null) return;

            runtime.Timeline.Clear();
            runtime.Binder.ForEachShellGameObject((actorId, entityId, go) => RegisterSeekablesOnGameObject(runtime, go));
            runtime.LastAlignedFrame = int.MinValue;
        }

        public static void SeekAllToCurrentFrame(IViewFeatureRuntime runtime)
        {
            if (runtime?.Timeline == null) return;

            var battleCtx = runtime.Context;
            if (battleCtx == null) return;

            var frame = battleCtx.LastFrame;
            if (frame == runtime.LastAlignedFrame) return;

            var tickRate = battleCtx.Plan.TickRate;
            var secondsPerFrame = tickRate > 0 ? 1f / tickRate : 0f;
            runtime.Timeline.SeekAll(frame, secondsPerFrame);

            runtime.LastAlignedFrame = frame;

            var worldId = battleCtx.RuntimeWorldId;
            battleCtx.Hooks?.ViewFrameAligned.Invoke(new ViewFrameAlignedEvent(isConfirmed: runtime.IsConfirmed, worldId: worldId, frame: frame));
        }

        public static void RebindAllViews(IViewFeatureRuntime runtime)
        {
            var battleCtx = runtime?.Context;
            if (battleCtx?.EntityWorld == null) return;

            runtime.Binder?.RebindAll(battleCtx.EntityWorld, battleCtx);
        }

        public static void TickVfx(IViewFeatureRuntime runtime)
        {
            if (runtime == null) return;
            if (runtime.VfxNode.IsValid) runtime.Vfx?.Tick(runtime.VfxNode, runtime.Binder);
        }

        public static void TickFloatingTexts(IViewFeatureRuntime runtime, float deltaTime)
        {
            runtime?.FloatingTexts?.Tick(deltaTime);
        }

        public static void OnEntityDestroyed(IViewFeatureRuntime runtime, EC.EntityDestroyed evt)
        {
            var id = evt.EntityId;
            runtime?.Context?.EntityLookup?.UnbindByEntityId(id);
            runtime?.Binder?.OnDestroyed(id);
        }

        private static void RegisterSeekablesForEntity(IViewFeatureRuntime runtime, EC.IEntityId id)
        {
            if (runtime?.Timeline == null || runtime.Binder == null) return;
            if (!runtime.Binder.TryGetShellGameObject(id, out var go)) return;

            RegisterSeekablesOnGameObject(runtime, go);
            runtime.LastAlignedFrame = int.MinValue;
        }

        private static void RegisterSeekablesOnGameObject(IViewFeatureRuntime runtime, GameObject go)
        {
            if (runtime?.Timeline == null) return;
            if (go == null) return;

            var monos = go.GetComponentsInChildren<MonoBehaviour>(true);
            if (monos == null || monos.Length == 0) return;

            for (int i = 0; i < monos.Length; i++)
            {
                if (monos[i] is IFrameSeekableView seekable)
                {
                    runtime.Timeline.Register(seekable);
                }
            }
        }
    }

    public sealed partial class BattleViewFeature : IViewFeatureRuntime
    {
        private IDisposable _entityDestroyedSub;
        private int _lastAlignedFrame = int.MinValue;

        BattleContext IViewSharedSubFeatureHost.Context => _ctx;
        BattleViewBinder IViewSharedSubFeatureHost.Binder => _binder;
        bool IViewSharedSubFeatureHost.IsConfirmed => false;
        WorldId IViewSharedSubFeatureHost.WorldId => _ctx != null ? _ctx.RuntimeWorldId : default;

        void IViewSharedSubFeatureHost.RefreshDirtyViews() => ViewFeatureRuntimeOperations.RefreshDirtyViews(this);
        void IViewSharedSubFeatureHost.RegisterAllSeekables() => ViewFeatureRuntimeOperations.RegisterAllSeekables(this);
        void IViewSharedSubFeatureHost.SeekAllToCurrentFrame() => ViewFeatureRuntimeOperations.SeekAllToCurrentFrame(this);
        void IViewSharedSubFeatureHost.RebindAllViews() => ViewFeatureRuntimeOperations.RebindAllViews(this);
        void IViewSharedSubFeatureHost.TickVfx() => ViewFeatureRuntimeOperations.TickVfx(this);
        void IViewSharedSubFeatureHost.TickFloatingTexts(float deltaTime) => ViewFeatureRuntimeOperations.TickFloatingTexts(this, deltaTime);

        IBattleEntityQuery IViewFeatureRuntime.Query
        {
            get => _query;
            set => _query = value;
        }

        BattleViewBinder IViewFeatureRuntime.Binder
        {
            get => _binder;
            set => _binder = value;
        }

        BattleVfxManager IViewFeatureRuntime.Vfx
        {
            get => _vfx;
            set => _vfx = value;
        }

        EC.IEntity IViewFeatureRuntime.VfxNode
        {
            get => _vfxNode;
            set => _vfxNode = value;
        }

        ViewTimeline IViewFeatureRuntime.Timeline
        {
            get => _timeline;
            set => _timeline = value;
        }

        BattleFloatingTextSystem IViewFeatureRuntime.FloatingTexts
        {
            get => _floatingTexts;
            set => _floatingTexts = value;
        }

        BattleAreaViewSystem IViewFeatureRuntime.AreaViews
        {
            get => _areaViews;
            set => _areaViews = value;
        }

        IBattleViewEventSink IViewFeatureRuntime.EventSink
        {
            get => _eventSink;
            set => _eventSink = value;
        }

        BattleSnapshotViewAdapter IViewFeatureRuntime.SnapshotAdapter
        {
            get => _snapshotAdapter;
            set => _snapshotAdapter = value;
        }

        BattleTriggerEventViewAdapter IViewFeatureRuntime.TriggerAdapter
        {
            get => _triggerAdapter;
            set => _triggerAdapter = value;
        }

        IDisposable IViewFeatureRuntime.EntityDestroyedSubscription
        {
            get => _entityDestroyedSub;
            set => _entityDestroyedSub = value;
        }

        int IViewFeatureRuntime.LastAlignedFrame
        {
            get => _lastAlignedFrame;
            set => _lastAlignedFrame = value;
        }

        void IViewFeatureRuntime.OnEntityDestroyed(EC.EntityDestroyed evt) => ViewFeatureRuntimeOperations.OnEntityDestroyed(this, evt);
    }

    public sealed partial class ConfirmedBattleViewFeature : IViewFeatureRuntime
    {
        private IDisposable _entityDestroyedSub;
        private int _lastAlignedFrame = int.MinValue;

        BattleContext IViewSharedSubFeatureHost.Context => _confirmedCtx;
        BattleViewBinder IViewSharedSubFeatureHost.Binder => _binder;
        bool IViewSharedSubFeatureHost.IsConfirmed => true;
        WorldId IViewSharedSubFeatureHost.WorldId => _confirmedCtx != null ? _confirmedCtx.RuntimeWorldId : default;

        void IViewSharedSubFeatureHost.RefreshDirtyViews() => ViewFeatureRuntimeOperations.RefreshDirtyViews(this);
        void IViewSharedSubFeatureHost.RegisterAllSeekables() => ViewFeatureRuntimeOperations.RegisterAllSeekables(this);
        void IViewSharedSubFeatureHost.SeekAllToCurrentFrame() => ViewFeatureRuntimeOperations.SeekAllToCurrentFrame(this);
        void IViewSharedSubFeatureHost.RebindAllViews() => ViewFeatureRuntimeOperations.RebindAllViews(this);
        void IViewSharedSubFeatureHost.TickVfx() => ViewFeatureRuntimeOperations.TickVfx(this);
        void IViewSharedSubFeatureHost.TickFloatingTexts(float deltaTime) => ViewFeatureRuntimeOperations.TickFloatingTexts(this, deltaTime);

        IBattleEntityQuery IViewFeatureRuntime.Query
        {
            get => _query;
            set => _query = value;
        }

        BattleViewBinder IViewFeatureRuntime.Binder
        {
            get => _binder;
            set => _binder = value;
        }

        BattleVfxManager IViewFeatureRuntime.Vfx
        {
            get => _vfx;
            set => _vfx = value;
        }

        EC.IEntity IViewFeatureRuntime.VfxNode
        {
            get => _vfxNode;
            set => _vfxNode = value;
        }

        ViewTimeline IViewFeatureRuntime.Timeline
        {
            get => _timeline;
            set => _timeline = value;
        }

        BattleFloatingTextSystem IViewFeatureRuntime.FloatingTexts
        {
            get => _floatingTexts;
            set => _floatingTexts = value;
        }

        BattleAreaViewSystem IViewFeatureRuntime.AreaViews
        {
            get => _areaViews;
            set => _areaViews = value;
        }

        IBattleViewEventSink IViewFeatureRuntime.EventSink
        {
            get => _eventSink;
            set => _eventSink = value;
        }

        BattleSnapshotViewAdapter IViewFeatureRuntime.SnapshotAdapter
        {
            get => _snapshotAdapter;
            set => _snapshotAdapter = value;
        }

        BattleTriggerEventViewAdapter IViewFeatureRuntime.TriggerAdapter
        {
            get => _triggerAdapter;
            set => _triggerAdapter = value;
        }

        IDisposable IViewFeatureRuntime.EntityDestroyedSubscription
        {
            get => _entityDestroyedSub;
            set => _entityDestroyedSub = value;
        }

        int IViewFeatureRuntime.LastAlignedFrame
        {
            get => _lastAlignedFrame;
            set => _lastAlignedFrame = value;
        }

        void IViewFeatureRuntime.OnEntityDestroyed(EC.EntityDestroyed evt) => ViewFeatureRuntimeOperations.OnEntityDestroyed(this, evt);
    }
}
