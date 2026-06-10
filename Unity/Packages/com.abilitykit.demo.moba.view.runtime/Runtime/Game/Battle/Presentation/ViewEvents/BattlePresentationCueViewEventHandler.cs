using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using UnityEngine;
using EC = AbilityKit.World.ECS;
using EntityBattleNetId = AbilityKit.Game.Battle.Entity.BattleNetId;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattlePresentationCueViewEventHandler
    {
        private readonly BattleContext _ctx;
        private readonly IBattleEntityQuery _query;
        private readonly BattlePresentationCueVfxSpawner _spawner;
        private readonly Dictionary<string, EC.IEntityId> _activeByRequestKey = new();

        public BattlePresentationCueViewEventHandler(
            BattleContext ctx,
            IBattleEntityQuery query,
            BattleVfxManager vfx,
            in EC.IEntity vfxNode)
            : this(ctx, query, vfx, in vfxNode, null)
        {
        }

        internal BattlePresentationCueViewEventHandler(
            BattleContext ctx,
            IBattleEntityQuery query,
            BattleVfxManager vfx,
            in EC.IEntity vfxNode,
            BattlePresentationCueViewEventHandlerFactory handlers)
        {
            _ctx = ctx;
            _query = query;
            handlers ??= new BattlePresentationCueViewEventHandlerFactory();
            _spawner = handlers.CreateSpawner(ctx, vfx, in vfxNode);
        }

        public void HandleSnapshot(PresentationCueData[] entries)
        {
            if (entries == null || entries.Length == 0) return;
            if (!_spawner.CanSpawn) return;

            for (int i = 0; i < entries.Length; i++)
            {
                HandleSnapshotEntry(entries[i]);
            }
        }

        private void HandleSnapshotEntry(in PresentationCueData data)
        {
            var requestKey = GetRequestKey(in data);
            if (string.IsNullOrEmpty(requestKey)) return;

            if (ShouldStart(data.Stage))
            {
                Play(requestKey, in data);
                return;
            }

            if (ShouldStop(data.Stage))
            {
                Stop(requestKey, data.Stage);
            }
        }

        private void Play(string requestKey, in PresentationCueData data)
        {
            if (_activeByRequestKey.ContainsKey(requestKey)) return;

            var vfxId = ResolveVfxId(in data);
            if (vfxId <= 0) return;

            var position = ResolvePosition(in data);
            var followTarget = ResolveFollowTarget(in data);
            if (_spawner.TrySpawn(vfxId, in position, followTarget, out var entity))
            {
                _activeByRequestKey[requestKey] = entity.Id;
            }
        }

        private void Stop(string requestKey, PresentationCueStage stage)
        {
            if (!_activeByRequestKey.TryGetValue(requestKey, out var entityId)) return;

            _activeByRequestKey.Remove(requestKey);
            _spawner.Destroy(entityId);
        }

        private static bool ShouldStart(PresentationCueStage stage)
        {
            return stage == PresentationCueStage.ConditionPassed
                || stage == PresentationCueStage.BeforeAction
                || stage == PresentationCueStage.Executed;
        }

        private static bool ShouldStop(PresentationCueStage stage)
        {
            return stage == PresentationCueStage.ConditionFailed
                || stage == PresentationCueStage.Interrupted
                || stage == PresentationCueStage.Skipped;
        }

        private static int ResolveVfxId(in PresentationCueData data)
        {
            if (data.VfxId > 0) return data.VfxId;
            if (data.TemplateId > 0) return data.TemplateId;
            return 0;
        }

        private Vector3 ResolvePosition(in PresentationCueData data)
        {
            if (data.Positions != null && data.Positions.Count > 0)
            {
                var p = data.Positions[0];
                return new Vector3(p.X + data.OffsetX, p.Y + data.OffsetY, p.Z + data.OffsetZ);
            }

            if (TryResolveActorPosition(data.TargetActorId, out var targetPosition))
            {
                return targetPosition + new Vector3(data.OffsetX, data.OffsetY, data.OffsetZ);
            }

            if (data.Targets != null && data.Targets.Count > 0 && TryResolveActorPosition(data.Targets[0], out var firstTargetPosition))
            {
                return firstTargetPosition + new Vector3(data.OffsetX, data.OffsetY, data.OffsetZ);
            }

            if (TryResolveActorPosition(data.SourceActorId, out var sourcePosition))
            {
                return sourcePosition + new Vector3(data.OffsetX, data.OffsetY, data.OffsetZ);
            }

            return new Vector3(data.OffsetX, data.OffsetY, data.OffsetZ);
        }

        private EC.IEntityId ResolveFollowTarget(in PresentationCueData data)
        {
            if (TryResolveActorEntity(data.TargetActorId, out var target)) return target.Id;
            if (data.Targets != null && data.Targets.Count > 0 && TryResolveActorEntity(data.Targets[0], out var firstTarget)) return firstTarget.Id;
            if (TryResolveActorEntity(data.SourceActorId, out var source)) return source.Id;
            return default;
        }

        private bool TryResolveActorPosition(int actorId, out Vector3 position)
        {
            position = default;
            if (!TryResolveActorEntity(actorId, out var entity)) return false;
            if (!entity.TryGetRef(out BattleTransformComponent transform) || transform == null) return false;

            position = transform.Position;
            return true;
        }

        private bool TryResolveActorEntity(int actorId, out EC.IEntity entity)
        {
            entity = default;
            if (actorId <= 0 || _query == null) return false;

            return _query.TryResolve(new EntityBattleNetId(actorId), out entity);
        }

        private static string GetRequestKey(in PresentationCueData data)
        {
            if (!string.IsNullOrWhiteSpace(data.RequestKey)) return data.RequestKey;

            if (data.TriggerId > 0)
            {
                return $"cue:{data.TriggerId}:{data.ActionIndex}:{data.Order}:{data.SourceActorId}:{data.TargetActorId}";
            }

            if (data.TriggerEventId > 0)
            {
                return $"cue:{data.TriggerEventId}:{data.ActionIndex}:{data.Order}:{data.SourceActorId}:{data.TargetActorId}";
            }

            return $"cue:{data.ActionIndex}:{data.Order}:{data.SourceActorId}:{data.TargetActorId}";
        }

    }

    internal sealed class BattlePresentationCueViewEventHandlerFactory
    {
        public BattlePresentationCueVfxSpawner CreateSpawner(
            BattleContext ctx,
            BattleVfxManager vfx,
            in EC.IEntity vfxNode)
        {
            return new BattlePresentationCueVfxSpawner(ctx, vfx, in vfxNode);
        }
    }

    internal sealed class BattlePresentationCueVfxSpawner
    {
        private readonly BattleContext _ctx;
        private readonly BattleVfxManager _vfx;
        private readonly EC.IEntity _vfxNode;

        public BattlePresentationCueVfxSpawner(BattleContext ctx, BattleVfxManager vfx, in EC.IEntity vfxNode)
        {
            _ctx = ctx;
            _vfx = vfx;
            _vfxNode = vfxNode;
        }

        public bool CanSpawn
        {
            get
            {
                if (_ctx?.EntityWorld == null) return false;
                if (_vfx == null) return false;
                if (!_vfxNode.IsValid) return false;
                return true;
            }
        }

        public bool TrySpawn(int vfxId, in Vector3 position, EC.IEntityId followTarget, out EC.IEntity entity)
        {
            entity = default;
            if (!CanSpawn) return false;
            if (vfxId <= 0) return false;

            return _vfx.TryCreateVfxEntity(
                _ctx.EntityWorld,
                _vfxNode,
                vfxId,
                followTarget,
                in position,
                out entity);
        }

        public void Destroy(EC.IEntityId id)
        {
            if (_ctx?.EntityWorld == null) return;
            if (id == default) return;

            _vfx.DestroyVfxEntity(_ctx.EntityWorld, id);
        }
    }
}
