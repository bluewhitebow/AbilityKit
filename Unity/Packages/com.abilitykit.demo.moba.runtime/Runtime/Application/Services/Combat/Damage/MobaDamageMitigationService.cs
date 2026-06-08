using System;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaDamageMitigationService))]
    public sealed class MobaDamageMitigationService : IService
    {
        private readonly MobaActorLookupService _actors;

        public MobaDamageMitigationService(MobaActorLookupService actors)
        {
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        }

        public float Mitigate(AttackInfo attack, float rawDamage)
        {
            if (attack == null) return 0f;
            if (rawDamage <= 0f) return 0f;
            if (attack.DamageType == DamageType.True) return rawDamage;

            if (!_actors.TryGetActorEntity(attack.TargetActorId, out var target) || target == null) return rawDamage;
            if (!target.hasAttributeGroup) return rawDamage;

            var targetAttrs = target.GetMobaAttrs();
            var defense = ResolveDefense(targetAttrs, attack.DamageType);
            var penetrationR = ResolvePenetrationRatio(attack.AttackerActorId, attack.DamageType);
            var effectiveDefense = Math.Max(0f, defense * (1f - Clamp(penetrationR, 0f, 0.95f)));

            return rawDamage * 100f / (100f + effectiveDefense);
        }

        private float ResolvePenetrationRatio(int attackerActorId, DamageType damageType)
        {
            if (attackerActorId <= 0) return 0f;
            if (!_actors.TryGetActorEntity(attackerActorId, out var attacker) || attacker == null) return 0f;
            if (!attacker.hasAttributeGroup) return 0f;

            var attrs = attacker.GetMobaAttrs();
            switch (damageType)
            {
                case DamageType.Physical:
                    return attrs.PhysicsPenetrationR;
                case DamageType.Magic:
                    return attrs.MagicPenetrationR;
                default:
                    return 0f;
            }
        }

        private static float ResolveDefense(MobaAttrs attrs, DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Physical:
                    return attrs.PhysicsDefense;
                case DamageType.Magic:
                    return attrs.MagicDefense;
                default:
                    return 0f;
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public void Dispose()
        {
        }
    }
}
