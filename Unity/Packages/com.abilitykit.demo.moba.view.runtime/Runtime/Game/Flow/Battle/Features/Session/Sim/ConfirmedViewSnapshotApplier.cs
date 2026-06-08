using System.Collections.Generic;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.World.ECS;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal static class ConfirmedViewSnapshotApplier
    {
        public static void ApplyStateHash(BattleContext ctx, MobaStateHashSnapshotPayload p)
        {
            if (ctx == null) return;

            var node = ctx.EntityNode;
            if (!node.IsValid) return;

            var comp = node.TryGetRef(out BattleStateHashSnapshotComponent existing) ? existing : null;
            if (comp == null)
            {
                comp = new BattleStateHashSnapshotComponent();
                node.WithRef(comp);
            }

            comp.Version = p.Version;
            comp.Frame = p.Frame;
            comp.Hash = p.Hash;
        }

        public static void ApplyTransform(BattleContext ctx, MobaActorTransformSnapshotEntry[] entries)
        {
            if (ctx == null) return;

            var world = ctx.EntityWorld;
            var lookup = ctx.EntityLookup;
            if (world == null || lookup == null || ctx.EntityFactory == null) return;

            var dirty = GetDirtyEntities(ctx, 64);

            if (entries == null || entries.Length == 0) return;
            for (int i = 0; i < entries.Length; i++)
            {
                var en = entries[i];
                var netId = new BattleNetId(en.ActorId);

                if (!lookup.TryResolve(world, netId, out var e))
                {
                    continue;
                }

                if (!e.TryGetRef(out BattleTransformComponent t) || t == null)
                {
                    t = new BattleTransformComponent();
                    e.WithRef(t);
                }

                t.Position.x = en.X;
                t.Position.y = en.Y;
                t.Position.z = en.Z;
                if (t.Forward == default) t.Forward = Vector3.forward;

                dirty.Add(e.Id);
            }
        }

        public static void ApplySpawn(BattleContext ctx, MobaActorSpawnSnapshotEntry[] entries)
        {
            if (ctx == null) return;

            var world = ctx.EntityWorld;
            var lookup = ctx.EntityLookup;
            var entityFactory = ctx.EntityFactory;
            if (world == null || lookup == null || entityFactory == null) return;

            var dirty = GetDirtyEntities(ctx, entries?.Length ?? 8);

            if (entries == null || entries.Length == 0) return;

            for (int i = 0; i < entries.Length; i++)
            {
                var en = entries[i];
                if (en.NetId <= 0) continue;

                var netId = new BattleNetId(en.NetId);
                if (!lookup.TryResolve(world, netId, out var e))
                {
                    if (en.Kind == (int)SpawnEntityKind.Projectile)
                    {
                        e = entityFactory.CreateProjectile(netId, ownerNetId: new BattleNetId(en.OwnerNetId), entityCode: en.Code);
                    }
                    else
                    {
                        e = entityFactory.CreateCharacter(netId, entityCode: en.Code);
                    }
                }

                if (!e.TryGetRef(out BattleTransformComponent t) || t == null)
                {
                    t = new BattleTransformComponent();
                    e.WithRef(t);
                }

                t.Position = new Vector3(en.X, en.Y, en.Z);
                if (t.Forward == default) t.Forward = Vector3.forward;

                dirty.Add(e.Id);
            }
        }

        private static List<IEntityId> GetDirtyEntities(BattleContext ctx, int capacity)
        {
            var dirty = ctx.DirtyEntities;
            if (dirty == null)
            {
                dirty = new List<IEntityId>(capacity);
                ctx.DirtyEntities = dirty;
            }
            else
            {
                dirty.Clear();
            }

            return dirty;
        }
    }
}
