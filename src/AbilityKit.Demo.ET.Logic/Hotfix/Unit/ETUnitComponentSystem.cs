using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Services;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETUnitComponent System
    /// Manages ETUnit lifecycle
    /// </summary>
    [EntitySystemOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitComponentSystem
    {
        // Unit dictionary (static storage)
        private static readonly Dictionary<long, ETUnit> _units = new();
        private static ETUnitComponent? _current;

        [EntitySystem]
        private static void Awake(this ETUnitComponent self)
        {
            _units.Clear();
            _current = self;
        }

        [EntitySystem]
        private static void Destroy(this ETUnitComponent self)
        {
            foreach (var unit in _units.Values)
            {
                unit.Dispose();
            }
            _units.Clear();
            _current = null;
        }

        /// <summary>
        /// Create unit
        /// </summary>
        public static ETUnit CreateUnit(
            this ETUnitComponent self,
            long actorId,
            int entityCode,
            ActorKind kind,
            string name,
            float x = 0,
            float y = 0,
            float maxHp = 100f,
            float attack = 10f,
            float defense = 5f,
            float moveSpeed = 5f,
            bool isLocalPlayer = false)
        {
            var unit = self.AddChild<ETUnit>();
            unit.ActorId = actorId;
            unit.EntityCode = entityCode;
            unit.Kind = kind;
            unit.Name = name;
            unit.X = x;
            unit.Y = y;
            unit.MaxHp = maxHp;
            unit.Hp = maxHp;
            unit.Attack = attack;
            unit.Defense = defense;
            unit.MoveSpeed = moveSpeed;
            unit.IsLocalPlayer = isLocalPlayer;

            _units[actorId] = unit;

            Log.Info($"[ETUnit] Unit created: {name} ({actorId}) at ({x}, {y})");

            return unit;
        }

        /// <summary>
        /// Get unit
        /// </summary>
        public static ETUnit? GetUnit(this ETUnitComponent self, long actorId)
        {
            return _units.TryGetValue(actorId, out var unit) ? unit : null;
        }

        /// <summary>
        /// Get all units
        /// </summary>
        public static IEnumerable<ETUnit> GetAllUnits(this ETUnitComponent self)
        {
            return _units.Values;
        }

        /// <summary>
        /// Get units by kind
        /// </summary>
        public static IEnumerable<ETUnit> GetUnitsByKind(this ETUnitComponent self, ActorKind kind)
        {
            foreach (var unit in _units.Values)
            {
                if (unit.Kind == kind)
                    yield return unit;
            }
        }

        /// <summary>
        /// Get local player unit
        /// </summary>
        public static ETUnit? GetLocalPlayerUnit(this ETUnitComponent self)
        {
            foreach (var unit in _units.Values)
            {
                if (unit.IsLocalPlayer)
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// Get first unit
        /// </summary>
        public static ETUnit? GetFirstUnit(this ETUnitComponent self)
        {
            foreach (var unit in _units.Values)
            {
                return unit;
            }
            return null;
        }

        /// <summary>
        /// Remove unit
        /// </summary>
        public static void RemoveUnit(this ETUnitComponent self, long actorId)
        {
            if (_units.TryGetValue(actorId, out var unit))
            {
                _units.Remove(actorId);
                unit.Dispose();
                Log.Info($"[ETUnit] Unit removed: {actorId}");
            }
        }

        /// <summary>
        /// Execute damage
        /// </summary>
        public static void ExecuteDamage(
            this ETUnitComponent self,
            long attackerActorId,
            long targetActorId,
            float damageValue)
        {
            var targetUnit = _units.GetValueOrDefault(targetActorId);
            if (targetUnit == null || targetUnit.IsDead)
                return;

            DamagePipelineService? damageService = null;
            var worldResolver = GetWorldResolver(self);
            if (worldResolver != null)
            {
                worldResolver.TryResolve<DamagePipelineService>(out damageService);
            }

            if (damageService != null)
            {
                ExecuteDamageViaService(damageService, self, attackerActorId, targetActorId, targetUnit, damageValue);
            }
            else
            {
                FallbackApplyDamage(self, targetActorId, damageValue, attackerActorId);
            }
        }

        private static void ExecuteDamageViaService(
            DamagePipelineService damageService,
            ETUnitComponent self,
            long attackerActorId,
            long targetActorId,
            ETUnit targetUnit,
            float damageValue)
        {
            try
            {
                var attack = new global::AbilityKit.Demo.Moba.AttackInfo
                {
                    AttackerActorId = (int)attackerActorId,
                    TargetActorId = (int)targetActorId,
                    DamageType = global::AbilityKit.Demo.Moba.DamageType.Physical,
                    CritType = global::AbilityKit.Demo.Moba.CritType.None,
                    ReasonKind = global::AbilityKit.Demo.Moba.DamageReasonKind.Skill,
                    ReasonParam = 0,
                    FormulaKind = (int)global::AbilityKit.Demo.Moba.DamageFormulaKind.Standard,
                    OriginSource = (int)attackerActorId,
                    OriginTarget = (int)targetActorId,
                    OriginKind = global::AbilityKit.Demo.Moba.EffectSourceKind.Effect,
                    OriginConfigId = 0,
                    OriginContextId = 0,
                };
                attack.BaseDamage.BaseValue = damageValue;

                var result = damageService.Execute(attack);
                if (result != null)
                {
                    targetUnit.Hp = result.TargetHp;

                    Log.Info($"[ETUnit] {targetUnit.Name} took {result.Value:F1} damage (moba.core), HP: {result.TargetHp:F0}/{result.TargetMaxHp}");

                    EventSystem.Instance.Publish<Scene, ActorDamageEvent>(
                        self.Scene(),
                        new ActorDamageEvent
                        {
                            ActorId = targetActorId,
                            SourceActorId = attackerActorId,
                            Damage = result.Value,
                            CurrentHp = result.TargetHp,
                            MaxHp = result.TargetMaxHp
                        });

                    if (targetUnit.IsDead)
                    {
                        OnUnitDead(self, targetUnit, attackerActorId);
                    }
                }
                else
                {
                    FallbackApplyDamage(self, targetActorId, damageValue, attackerActorId);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[ETUnit] ExecuteDamageViaService failed: {ex.Message}, using fallback");
                FallbackApplyDamage(self, targetActorId, damageValue, attackerActorId);
            }
        }

        private static IWorldResolver? GetWorldResolver(ETUnitComponent self)
        {
            var worldResolverComponent = self.Scene().GetComponent<ETWorldResolverComponent>();
            return worldResolverComponent?.Resolver;
        }

        private static void FallbackApplyDamage(ETUnitComponent self, long targetActorId, float damage, long sourceActorId)
        {
            var targetUnit = _units.GetValueOrDefault(targetActorId);
            if (targetUnit == null || targetUnit.IsDead)
                return;

            float actualDamage = Math.Max(1f, damage - targetUnit.Defense * 0.5f);
            targetUnit.Hp = Math.Max(0, targetUnit.Hp - actualDamage);

            Log.Info($"[ETUnit] {targetUnit.Name} took {actualDamage:F1} damage (fallback), HP: {targetUnit.Hp:F0}/{targetUnit.MaxHp}");

            EventSystem.Instance.Publish<Scene, ActorDamageEvent>(
                self.Scene(),
                new ActorDamageEvent
                {
                    ActorId = targetActorId,
                    SourceActorId = sourceActorId,
                    Damage = actualDamage,
                    CurrentHp = targetUnit.Hp,
                    MaxHp = targetUnit.MaxHp
                });

            if (targetUnit.IsDead)
            {
                OnUnitDead(self, targetUnit, sourceActorId);
            }
        }

        private static void OnUnitDead(ETUnitComponent self, ETUnit unit, long killerId)
        {
            Log.Info($"[ETUnit] {unit.Name} is dead!");

            EventSystem.Instance.Publish<Scene, ActorDeadEvent>(
                self.Scene(),
                new ActorDeadEvent
                {
                    ActorId = unit.ActorId,
                    KillerId = killerId
                });
        }

        /// <summary>
        /// Find units in range
        /// </summary>
        public static List<ETUnit> FindUnitsInRange(this ETUnitComponent self, float x, float y, float range)
        {
            var result = new List<ETUnit>();
            float rangeSq = range * range;

            foreach (var unit in _units.Values)
            {
                if (unit.IsDead)
                    continue;

                float dx = unit.X - x;
                float dy = unit.Y - y;
                if (dx * dx + dy * dy <= rangeSq)
                {
                    result.Add(unit);
                }
            }

            return result;
        }

        /// <summary>
        /// Tick - update all units
        /// </summary>
        public static void Tick(this ETUnitComponent self, float deltaTime)
        {
            foreach (var unit in _units.Values)
            {
                if (unit.IsDead)
                    continue;

                if (unit.IsMoving)
                {
                    float oldX = unit.X;
                    float oldY = unit.Y;
                    unit.MoveTo(unit.TargetX, unit.TargetY, deltaTime);

                    if (Math.Abs(unit.X - oldX) > 0.01f || Math.Abs(unit.Y - oldY) > 0.01f)
                    {
                        EventSystem.Instance.Publish<Scene, ActorMoveEvent>(
                            self.Scene(),
                            new ActorMoveEvent
                            {
                                ActorId = unit.ActorId,
                                X = unit.X,
                                Y = unit.Y
                            });
                    }
                }

                for (int i = 0; i < unit.SkillCooldowns.Length; i++)
                {
                    if (unit.SkillCooldowns[i] > 0)
                    {
                        unit.SkillCooldowns[i] = Math.Max(0, unit.SkillCooldowns[i] - deltaTime);
                    }
                }
            }
        }

        /// <summary>
        /// Get unit count
        /// </summary>
        public static int UnitCount(this ETUnitComponent self)
        {
            return _units.Count;
        }

        /// <summary>
        /// Get alive unit count
        /// </summary>
        public static int AliveUnitCount(this ETUnitComponent self)
        {
            int count = 0;
            foreach (var unit in _units.Values)
            {
                if (!unit.IsDead)
                    count++;
            }
            return count;
        }
    }
}
