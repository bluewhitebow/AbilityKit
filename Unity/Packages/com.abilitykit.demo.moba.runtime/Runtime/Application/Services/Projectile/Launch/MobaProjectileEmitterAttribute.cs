using System;
using AbilityKit.Demo.Moba;

namespace AbilityKit.Demo.Moba.Services.Projectile.Launch
{
    /// <summary>
    /// Marks a projectile launch sequence as the implementation for a projectile emitter type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class MobaProjectileEmitterAttribute : Attribute
    {
        public MobaProjectileEmitterAttribute(ProjectileEmitterType emitterType)
        {
            EmitterType = emitterType;
        }

        public ProjectileEmitterType EmitterType { get; }
        public int Priority { get; set; }
        public bool IsDefault { get; set; }
    }
}
