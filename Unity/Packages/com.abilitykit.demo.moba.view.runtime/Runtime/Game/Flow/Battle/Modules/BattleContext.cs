using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.FrameSync;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Core.Common.Pool;
using AbilityKit.Core.Common.Record.Lockstep;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Flow.Battle.Modules;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public sealed class BattleContext : IPoolable
    {
        private static readonly ObjectPool<BattleContext> Pool = Pools.GetPool(
            key: "BattleContext",
            createFunc: () => new BattleContext(),
            defaultCapacity: 1,
            maxSize: 8);

        public BattleLogicSession Session;
        public BattleStartPlan Plan;
        public int LastFrame;
        public double LogicTimeSeconds;

        public int LocalActorId;

        public BattleSessionHooks Hooks;

        public FrameSnapshotDispatcher FrameSnapshots;
        public SnapshotPipeline SnapshotPipeline;
        public SnapshotCmdHandler CmdHandler;

        public ILockstepInputRecordWriter InputRecordWriter;

        public BattleLocalInputQueue LocalInputQueue;

        public IClientPredictionDriverStats PredictionStats;

        public IClientPredictionReconcileTarget PredictionReconcileTarget;

        public IClientPredictionReconcileControl PredictionReconcileControl;

        public IClientPredictionTuningControl PredictionTuningControl;

        public WorldId RuntimeWorldId;

        public bool HasRuntimeWorldId;

        public EC.IEntity EntityNode;
        public EC.IECWorld EntityWorld;
        public BattleEntityLookup EntityLookup;
        public BattleEntityFactory EntityFactory;
        public IBattleEntityQuery EntityQuery;

        public List<EC.IEntityId> DirtyEntities;

        public float HudMoveDx;
        public float HudMoveDz;
        public bool HudHasMove;

        public int HudSkillClickSlot;

        public bool HudSkillAiming;
        public int HudSkillAimSlot;
        public float HudSkillAimDx;
        public float HudSkillAimDz;

        public bool HudSkillAimSubmit;
        public int HudSkillAimSubmitSlot;
        public float HudSkillAimSubmitDx;
        public float HudSkillAimSubmitDz;

        public static BattleContext Rent()
        {
            return Pool.Get();
        }

        public static void Return(BattleContext ctx)
        {
            if (ctx == null) return;
            Pool.Release(ctx);
        }

        void IPoolable.OnPoolGet()
        {
        }

        void IPoolable.OnPoolRelease()
        {
            Reset(disposeOwnedResources: true, destroyCollections: false);
        }

        void IPoolable.OnPoolDestroy()
        {
            Reset(disposeOwnedResources: true, destroyCollections: true);
        }

        private void Reset(bool disposeOwnedResources, bool destroyCollections)
        {
            Session = null;
            Plan = default;
            LastFrame = 0;
            LogicTimeSeconds = 0d;

            LocalActorId = 0;

            Hooks = null;

            FrameSnapshots = null;
            SnapshotPipeline = null;
            CmdHandler = null;

            if (disposeOwnedResources)
            {
                InputRecordWriter?.Dispose();
                LocalInputQueue?.Dispose();
            }

            InputRecordWriter = null;
            LocalInputQueue = null;

            PredictionStats = null;
            PredictionReconcileTarget = null;
            PredictionReconcileControl = null;
            PredictionTuningControl = null;

            RuntimeWorldId = default;
            HasRuntimeWorldId = false;

            EntityNode = default;
            EntityWorld = null;
            EntityLookup = null;
            EntityFactory = null;
            EntityQuery = null;

            if (destroyCollections)
            {
                DirtyEntities = null;
            }
            else
            {
                DirtyEntities?.Clear();
            }

            HudMoveDx = 0f;
            HudMoveDz = 0f;
            HudHasMove = false;
            HudSkillClickSlot = 0;

            HudSkillAiming = false;
            HudSkillAimSlot = 0;
            HudSkillAimDx = 0f;
            HudSkillAimDz = 0f;

            HudSkillAimSubmit = false;
            HudSkillAimSubmitSlot = 0;
            HudSkillAimSubmitDx = 0f;
            HudSkillAimSubmitDz = 0f;
        }
    }
}
