using System;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Session spawn service interface
    ///
    /// Design:
    /// - Abstracts player spawn logic from SessionCoordinator
    /// - Implemented by game projects (moba.runtime) to create entities
    /// - Keeps Coordinator package game-agnostic
    /// </summary>
    public interface ISpawnService : IService
    {
        /// <summary>
        /// Create player spawns based on spawn data
        /// </summary>
        /// <param name="spawns">Player spawn data from host</param>
        /// <returns>True if spawns were created successfully</returns>
        bool CreateSpawns(PlayerSpawnData[] spawns);
    }
}
