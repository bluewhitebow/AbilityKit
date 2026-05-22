using AbilityKit.Demo.Moba.Share;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET Battle View Event Sink
    /// Bridges IBattleViewEventSink events to ET event system
    /// </summary>
    public sealed class ETBattleViewEventSink : IBattleViewEventSink
    {
        private readonly ETBattleComponent _battleComponent;

        public ETBattleViewEventSink(ETBattleComponent battleComponent)
        {
            _battleComponent = battleComponent;
        }

        #region Unit Events

        public void OnEnterGameSnapshot(in FrameSnapshotData snapshot)
        {
            var scene = _battleComponent.Scene();
            if (scene == null)
                return;

            Log.Info($"[ETBattleViewEventSink] >>> OnEnterGameSnapshot received, ActorSpawns count: {snapshot.ActorSpawns?.Count ?? 0}");

            foreach (var spawn in snapshot.ActorSpawns)
            {
                var evt = new ActorSpawnEvent()
                {
                    ActorId = spawn.ActorId,
                    EntityCode = spawn.CharacterId,
                    Kind = spawn.TeamId == 1 ? ActorKind.Character : ActorKind.Monster,
                    Name = spawn.Name,
                    X = spawn.PositionX,
                    Y = spawn.PositionY,
                    MaxHp = spawn.MaxHp,
                    IsLocalPlayer = spawn.ActorId == _battleComponent.PlayerActorId
                };

                Log.Info($"[ETBattleViewEventSink] >>> Publishing ActorSpawnEvent: {spawn.Name} ({spawn.ActorId}), Team={spawn.TeamId}");
                EventSystem.Instance.Publish<Scene, ActorSpawnEvent>(scene, evt);
            }

            Log.Info($"[ETBattleViewEventSink] >>> All ActorSpawnEvents published");

            // Initialize auto test components
            InitializeAutoTestComponent(scene, snapshot);
        }

        /// <summary>
        /// Initialize auto test components
        /// Called in OnEnterGameSnapshot, when units are not yet created by ETUnitComponent
        /// So use the first spawn's ActorId
        /// </summary>
        private void InitializeAutoTestComponent(Scene scene, in FrameSnapshotData snapshot)
        {
            var autoTest = scene.GetComponent<ETBattleAutoTestComponent>();
            var skillTest = scene.GetComponent<ETBattleSkillTestComponent>();
            if (autoTest == null && skillTest == null)
            {
                return;
            }

            // Use the first player's ActorId from the spawn data
            long actorIdToUse = 0;
            float startX = 0f;
            float startY = 0f;

            if (snapshot.ActorSpawns != null && snapshot.ActorSpawns.Count > 0)
            {
                var firstSpawn = snapshot.ActorSpawns[0];
                actorIdToUse = firstSpawn.ActorId;
                startX = firstSpawn.PositionX;
                startY = firstSpawn.PositionY;
            }
            else
            {
                // Fallback to PlayerActorId
                actorIdToUse = _battleComponent.PlayerActorId;
            }

            if (autoTest != null)
            {
                autoTest.Initialize(actorIdToUse, startX, startY);
                Log.Info($"[ETBattleViewEventSink] AutoTest initialized with ActorId={actorIdToUse}");
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

            foreach (var transform in snapshot.ActorTransforms)
            {
                EventSystem.Instance.Publish<Scene, ActorMoveEvent>(
                    scene,
                    new ActorMoveEvent
                    {
                        ActorId = transform.ActorId,
                        X = transform.PositionX,
                        Y = transform.PositionY
                    });
            }
        }

        public void OnDamageEventSnapshot(in FrameSnapshotData snapshot)
        {
            var scene = _battleComponent.Scene();
            if (scene == null)
                return;

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
        }

        #endregion

        #region Extended Events (Reserved)

        public void OnProjectileEventSnapshot(in FrameSnapshotData snapshot)
        {
        }

        public void OnAreaEventSnapshot(in FrameSnapshotData snapshot)
        {
        }

        public void OnStateHashSnapshot(in FrameSnapshotData snapshot)
        {
        }

        public void OnTriggerEvent(in TriggerEventData evt)
        {
            Log.Debug($"[ETBattleViewEventSink] Trigger: type={evt.EventType}, caster={evt.CasterId}, target={evt.TargetId}");
        }

        #endregion
    }
}
