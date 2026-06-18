using System;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;

namespace AbilityKit.Demo.Moba.Services.EntityConstruction
{
    public sealed class MobaActorSpawnRegistrar
    {
        private readonly MobaActorRegistry _registry;
        private readonly MobaEntityManager _entities;

        public MobaActorSpawnRegistrar(MobaActorRegistry registry, MobaEntityManager entities)
        {
            _registry = registry;
            _entities = entities;
        }

        public void Register(
            global::ActorEntity entity,
            in MobaActorBuildSpec spec,
            bool registerActor,
            bool registerEntityManager,
            bool registerEntityManagerFromEntity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (registerActor)
            {
                _registry?.Register(spec.Info.ActorId, entity);
            }

            if (!registerEntityManager || _entities == null) return;

            if (registerEntityManagerFromEntity)
            {
                try
                {
                    if (_entities.TryRegisterFromEntity(entity)) return;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"[MobaActorSpawnRegistrar] TryRegisterFromEntity failed. actorId={spec.Info.ActorId} kind={spec.Info.Kind}");
                }
            }

            _entities.Register(
                actorId: spec.Info.ActorId,
                entity: entity,
                team: spec.Info.Team,
                mainType: spec.Info.MainType,
                unitSubType: spec.Info.UnitSubType,
                ownerPlayer: spec.Info.OwnerPlayer);
        }
    }
}
