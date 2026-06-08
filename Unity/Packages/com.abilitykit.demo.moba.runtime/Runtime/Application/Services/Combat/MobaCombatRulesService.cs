using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Attributes;
namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaCombatRuleFailure
    {
        None = 0,
        InvalidActor = 1,
        Dead = 2,
        SameTeamBlocked = 3,
        EnemyTeamBlocked = 4,
        Untargetable = 5,
        Invulnerable = 6,
        Silenced = 7,
        Stunned = 8,
        Disabled = 9,
    }

    public readonly struct MobaCombatRuleResult
    {
        public static readonly MobaCombatRuleResult Pass = new MobaCombatRuleResult(true, MobaCombatRuleFailure.None, null);

        public MobaCombatRuleResult(bool passed, MobaCombatRuleFailure failure, string message)
        {
            Passed = passed;
            Failure = failure;
            Message = message;
        }

        public bool Passed { get; }
        public MobaCombatRuleFailure Failure { get; }
        public string Message { get; }

        public static MobaCombatRuleResult Fail(MobaCombatRuleFailure failure, string message)
        {
            return new MobaCombatRuleResult(false, failure, message);
        }
    }

    [WorldService(typeof(MobaCombatRulesService))]
    public sealed class MobaCombatRulesService : IService
    {
        private readonly MobaActorLookupService _actors;
        private readonly IMobaEffectiveTagQueryService _effectiveTags;

        public MobaCombatRulesService(MobaActorLookupService actors, IMobaEffectiveTagQueryService effectiveTags = null)
        {
            _actors = actors;
            _effectiveTags = effectiveTags;
        }

        public bool TryGetActor(int actorId, out global::ActorEntity actor)
        {
            actor = null;
            return actorId > 0 && _actors != null && _actors.TryGetActorEntity(actorId, out actor) && actor != null;
        }

        public bool IsAlive(int actorId)
        {
            return TryGetActor(actorId, out var actor) && IsAlive(actor);
        }

        public bool IsAlive(global::ActorEntity actor)
        {
            if (actor == null) return false;
            if (!actor.hasAttributeGroup) return true;

            var attrs = actor.GetMobaAttrs();
            return attrs.Hp > 0f;
        }

        public bool AreSameTeam(int actorId, int otherActorId)
        {
            return TryGetTeam(actorId, out var team) && TryGetTeam(otherActorId, out var otherTeam) && team != Team.None && team == otherTeam;
        }

        public bool AreEnemies(int actorId, int otherActorId)
        {
            return TryGetTeam(actorId, out var team) && TryGetTeam(otherActorId, out var otherTeam) && team != Team.None && otherTeam != Team.None && team != otherTeam;
        }

        public bool TryGetTeam(int actorId, out Team team)
        {
            team = Team.None;
            if (!TryGetActor(actorId, out var actor)) return false;
            if (!actor.hasTeam) return false;

            team = actor.team.Value;
            return team != Team.None;
        }

        public bool HasAnyTag(int actorId, params string[] tagNames)
        {
            if (actorId <= 0 || tagNames == null || tagNames.Length == 0) return false;
            if (_effectiveTags == null) return false;

            var tags = _effectiveTags.GetEffectiveTags(actorId);
            if (tags == null) return false;

            return MobaGameplayTagCatalog.HasAny(tags, tagNames);
        }

        public bool IsUntargetable(int actorId)
        {
            return HasAnyTag(actorId, MobaGameplayTagCatalog.UntargetableAliases);
        }

        public bool IsInvulnerable(int actorId)
        {
            return HasAnyTag(actorId, MobaGameplayTagCatalog.InvulnerableAliases);
        }

        public bool IsSilenced(int actorId)
        {
            return HasAnyTag(actorId, MobaGameplayTagCatalog.CastBlockedAliases);
        }

        public bool IsStunned(int actorId)
        {
            return HasAnyTag(actorId, MobaGameplayTagCatalog.StunnedAliases) || HasAnyTag(actorId, MobaGameplayTagCatalog.DisabledAliases) || HasAnyTag(actorId, MobaGameplayTagCatalog.SuppressedAliases);
        }

        public bool CanMove(int actorId)
        {
            return IsAlive(actorId) && !HasAnyTag(actorId, MobaGameplayTagCatalog.MoveBlockedAliases);
        }

        public bool CanBeControlled(int actorId)
        {
            return IsAlive(actorId) && !HasAnyTag(actorId, MobaGameplayTagCatalog.ControlImmuneAliases) && !HasAnyTag(actorId, MobaGameplayTagCatalog.ControlBlockedAliases);
        }

        public MobaCombatRuleResult CanBeSearchedTarget(int casterActorId, int targetActorId)
        {
            if (!TryGetActor(targetActorId, out _)) return MobaCombatRuleResult.Fail(MobaCombatRuleFailure.InvalidActor, "target_not_found");
            if (!IsAlive(targetActorId)) return MobaCombatRuleResult.Fail(MobaCombatRuleFailure.Dead, "target_dead");
            if (IsUntargetable(targetActorId)) return MobaCombatRuleResult.Fail(MobaCombatRuleFailure.Untargetable, "target_untargetable");
            return MobaCombatRuleResult.Pass;
        }

        public MobaCombatRuleResult CanReceiveDamage(int attackerActorId, int targetActorId)
        {
            var targetable = CanBeSearchedTarget(attackerActorId, targetActorId);
            if (!targetable.Passed) return targetable;
            if (IsInvulnerable(targetActorId)) return MobaCombatRuleResult.Fail(MobaCombatRuleFailure.Invulnerable, "target_invulnerable");
            return MobaCombatRuleResult.Pass;
        }

        public MobaCombatRuleResult CanCastSkill(int casterActorId)
        {
            if (!TryGetActor(casterActorId, out _)) return MobaCombatRuleResult.Fail(MobaCombatRuleFailure.InvalidActor, "caster_not_found");
            if (!IsAlive(casterActorId)) return MobaCombatRuleResult.Fail(MobaCombatRuleFailure.Dead, "caster_dead");
            if (IsStunned(casterActorId)) return MobaCombatRuleResult.Fail(MobaCombatRuleFailure.Stunned, "caster_stunned");
            if (IsSilenced(casterActorId)) return MobaCombatRuleResult.Fail(MobaCombatRuleFailure.Silenced, "caster_silenced");
            return MobaCombatRuleResult.Pass;
        }

        public void Dispose()
        {
        }
    }
}
