using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    public sealed class ProjectileEntry
    {
        public int ProjectileId;
        public int TemplateId;
        public float X;
        public float Y;
        public float Z;
    }

    public sealed class ConsoleProjectileDisplayService
    {
        private readonly Dictionary<int, ProjectileEntry> _projectiles = new();

        public void Spawn(int projectileId, int templateId, float x, float y, float z)
        {
            _projectiles[projectileId] = new ProjectileEntry
            {
                ProjectileId = projectileId,
                TemplateId = templateId,
                X = x,
                Y = y,
                Z = z
            };
        }

        public void UpdatePosition(int projectileId, float x, float y, float z)
        {
            if (_projectiles.TryGetValue(projectileId, out var proj))
            {
                proj.X = x;
                proj.Y = y;
                proj.Z = z;
            }
        }

        public void Remove(int projectileId) => _projectiles.Remove(projectileId);
        public bool TryGet(int projectileId, out ProjectileEntry entry) => _projectiles.TryGetValue(projectileId, out entry);
        public IReadOnlyCollection<ProjectileEntry> GetAll() => _projectiles.Values;
        public int Count => _projectiles.Count;
        public void Clear() => _projectiles.Clear();
    }
}
