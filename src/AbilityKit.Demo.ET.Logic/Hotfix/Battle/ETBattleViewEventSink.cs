using System;
using AbilityKit.Demo.Moba.Share;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET Battle View Event Sink
    /// Bridges IBattleViewEventSink events to ET event system
    /// Also updates the entity cache component for ET.View queries
    ///
    /// Design:
    /// - Uses virtual methods to allow extension in subclasses
    /// - Empty stubs indicate reserved functionality for future use
    /// </summary>
    public class ETBattleViewEventSink : IBattleViewEventSink
    {
        private readonly ETBattleComponent _battleComponent;
        private ETBattleEntityCacheComponent _cacheComponent;
        private ETViewSnapshotProvider _snapshotProvider;

        public ETBattleViewEventSink(ETBattleComponent battleComponent)
        {
            _battleComponent = battleComponent ?? throw new ArgumentNullException(nameof(battleComponent));
        }

        #region Cache Setup

        /// <summary>
        /// 初始化缓存组件
        /// 由 ETBattleComponentSystem 在初始化时调用
        /// </summary>
        public void InitializeCache(ETBattleEntityCacheComponent cacheComponent)
        {
            _cacheComponent = cacheComponent;
            _snapshotProvider = new ETViewSnapshotProvider(cacheComponent);
        }

        /// <summary>
        /// 获取快照提供器
        /// </summary>
        public IETViewSnapshotProvider GetSnapshotProvider() => _snapshotProvider;

        #endregion

        #region Unit Events

        public void OnEnterGameSnapshot(in FrameSnapshotData snapshot)
        {
            var scene = _battleComponent.Scene();
            if (scene == null)
                return;

            Log.Info($"[ETBattleViewEventSink] >>> OnEnterGameSnapshot received, ActorSpawns count: {snapshot.ActorSpawns?.Count ?? 0}");

            // 更新缓存
            if (_cacheComponent != null)
            {
                _cacheComponent.UpdateCache(snapshot.FrameIndex, snapshot);
            }

            // 发布 ActorSpawnEvent 事件
            foreach (var spawn in snapshot.ActorSpawns)
            {
                var evt = new ActorSpawnEvent()
                {
                    ActorId = spawn.ActorId,  // 运行时自增 ID
                    MobaActorId = spawn.ActorId,
                    EntityCode = spawn.EntityCode,  // 配置表 ID
                    Kind = spawn.TeamId == 1 ? ActorKind.Character : ActorKind.Monster,
                    Name = spawn.Name,
                    X = spawn.PositionX,
                    Y = spawn.PositionY,
                    MaxHp = spawn.MaxHp,
                    IsLocalPlayer = spawn.ActorId == _battleComponent.PlayerActorId
                };

                Log.Info($"[ETBattleViewEventSink] >>> Publishing ActorSpawnEvent: {spawn.Name} (ActorId={spawn.ActorId}, EntityCode={spawn.EntityCode}), Team={spawn.TeamId}");
                EventSystem.Instance.Publish<Scene, ActorSpawnEvent>(scene, evt);
            }

            Log.Info($"[ETBattleViewEventSink] >>> All ActorSpawnEvents published");

            // 初始化自动测试组件
            InitializeAutoTestComponent(scene, snapshot);
        }

        /// <summary>
        /// 初始化自动测试组件
        /// </summary>
        private void InitializeAutoTestComponent(Scene scene, in FrameSnapshotData snapshot)
        {
            var autoTest = scene.GetComponent<ETBattleAutoTestComponent>();
            var skillTest = scene.GetComponent<ETBattleSkillTestComponent>();
            if (autoTest == null && skillTest == null)
                return;

            int actorIdToUse = 0;
            int entityCodeToUse = 0;
            float startX = 0f;
            float startY = 0f;

            // 优先使用 ActorSpawns 中的数据
            if (snapshot.ActorSpawns != null && snapshot.ActorSpawns.Count > 0)
            {
                var firstSpawn = snapshot.ActorSpawns[0];
                actorIdToUse = firstSpawn.ActorId;  // 运行时自增 ID
                entityCodeToUse = firstSpawn.EntityCode;  // 配置表 ID
                startX = firstSpawn.PositionX;
                startY = firstSpawn.PositionY;
                Log.Info($"[ETBattleViewEventSink] Using ActorId={actorIdToUse}, EntityCode={entityCodeToUse} from spawn");
            }
            else if (_battleComponent.PlayerActorId > 0)
            {
                actorIdToUse = (int)_battleComponent.PlayerActorId;
                Log.Info($"[ETBattleViewEventSink] Using PlayerActorId: {actorIdToUse}");
            }
            else
            {
                Log.Warning($"[ETBattleViewEventSink] No valid ActorId found!");
            }

            if (autoTest != null)
            {
                autoTest.Initialize(actorIdToUse, startX, startY);  // 使用 ActorId
                Log.Info($"[ETBattleViewEventSink] AutoTest initialized with ActorId={actorIdToUse}, StartPos=({startX}, {startY})");
            }

            if (skillTest != null)
            {
                skillTest.Initialize(actorIdToUse, 0);
                Log.Info($"[ETBattleViewEventSink] SkillTest initialized with ActorId={actorIdToUse}");
            }
        }

        public void OnActorTransformSnapshot(in FrameSnapshotData snapshot)
        {
            var scene = _battleComponent.Scene();
            if (scene == null)
                return;

            // 记录快照
            Log.Info($"[ETBattleViewEventSink] OnActorTransformSnapshot: Frame={snapshot.FrameIndex}, Count={snapshot.ActorTransforms?.Count ?? 0}");

            // 更新缓存
            if (_cacheComponent != null)
            {
                _cacheComponent.UpdateCache(snapshot.FrameIndex, snapshot);
            }

            // 发布 ActorMoveEvent 事件
            if (snapshot.ActorTransforms != null)
            {
                foreach (var transform in snapshot.ActorTransforms)
                {
                    Log.Info($"[ETBattleViewEventSink] Transform: ActorId={transform.ActorId}, Pos=({transform.PositionX:F2}, {transform.PositionY:F2}, {transform.PositionZ:F2}), Rot={transform.RotationY:F2}");

                    EventSystem.Instance.Publish<Scene, ActorMoveEvent>(
                        scene,
                        new ActorMoveEvent
                        {
                            ActorId = transform.ActorId,  // 运行时自增 ID
                            X = transform.PositionX,
                            Y = transform.PositionY,
                            Z = transform.PositionZ,
                            Rotation = transform.RotationY
                        });
                }
            }
        }

        public void OnDamageEventSnapshot(in FrameSnapshotData snapshot)
        {
            var scene = _battleComponent.Scene();
            if (scene == null)
                return;

            // 更新缓存
            if (_cacheComponent != null)
            {
                _cacheComponent.UpdateCache(snapshot.FrameIndex, snapshot);
            }

            // 发布伤害和死亡事件
            foreach (var damage in snapshot.DamageEvents)
            {
                EventSystem.Instance.Publish<Scene, ActorDamageEvent>(
                    scene,
                    new ActorDamageEvent
                    {
                        ActorId = damage.TargetId,
                        SourceActorId = damage.AttackerId,
                        Damage = damage.DamageValue,
                        CurrentHp = damage.TargetHpAfter,
                        MaxHp = 100f
                    });

                if (damage.IsKill)
                {
                    EventSystem.Instance.Publish<Scene, ActorDeadEvent>(
                        scene,
                        new ActorDeadEvent
                        {
                            ActorId = damage.TargetId,
                            KillerId = damage.AttackerId
                        });
                }

                Log.Debug($"[ETBattleViewEventSink] Damage: {damage.AttackerId} -> {damage.TargetId}, dmg={damage.DamageValue}, kill={damage.IsKill}");
            }
        }

        #endregion

        #region Battle Events

        public void OnBattleStart(int frameIndex)
        {
            _battleComponent.ViewSink?.OnBattleStart(new BattleStartEvent()
            {
                BattleId = _battleComponent.BattleId,
                PlayerId = _battleComponent.PlayerId
            });
        }

        public void OnBattleEnd(int frameIndex, int winTeamId)
        {
            bool isVictory = winTeamId == 1;
            _battleComponent.ViewSink?.OnBattleEnd(new BattleEndEvent()
            {
                BattleId = _battleComponent.BattleId,
                IsVictory = isVictory
            });
        }

        public void OnFrameSyncComplete(int frameIndex)
        {
            // 此方法已被 OnActorTransformSnapshot 替代
            // 变换数据应通过 moba.core 快照系统生成并通过 OnActorTransformSnapshot 传递
            // 而不是直接从 ET.Logic 层访问 ETUnitComponent 构建
            Log.Debug($"[ETBattleViewEventSink] OnFrameSyncComplete called for frame {frameIndex} (deprecated)");
        }

        #endregion

        #region Extended Events (Reserved)

        /// <summary>
        /// 投射物事件快照（预留，暂未实现）
        /// </summary>
        public virtual void OnProjectileEventSnapshot(in FrameSnapshotData snapshot)
        {
            Log.Debug($"[ETBattleViewEventSink] OnProjectileEventSnapshot not implemented for frame {snapshot.FrameIndex}");
            // TODO: Implement projectile rendering when moba.core adds projectile system
        }

        /// <summary>
        /// 范围事件快照（预留，暂未实现）
        /// </summary>
        public virtual void OnAreaEventSnapshot(in FrameSnapshotData snapshot)
        {
            Log.Debug($"[ETBattleViewEventSink] OnAreaEventSnapshot not implemented for frame {snapshot.FrameIndex}");
            // TODO: Implement area effect rendering when moba.core adds area effect system
        }

        /// <summary>
        /// 状态哈希快照（预留，暂未实现）
        /// </summary>
        public virtual void OnStateHashSnapshot(in FrameSnapshotData snapshot)
        {
            Log.Debug($"[ETBattleViewEventSink] OnStateHashSnapshot not implemented for frame {snapshot.FrameIndex}");
            // TODO: Implement state hash verification when needed
        }

        public virtual void OnTriggerEvent(in TriggerEventData evt)
        {
            Log.Debug($"[ETBattleViewEventSink] Trigger: type={evt.EventType}, caster={evt.CasterId}, target={evt.TargetId}");
        }

        #endregion
    }
}
