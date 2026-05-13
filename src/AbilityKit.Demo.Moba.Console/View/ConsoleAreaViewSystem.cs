using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    public sealed class AreaEffectEntry
    {
        public int AreaId;
        public int TemplateId;
        public float CenterX;
        public float CenterZ;
        public float Radius;
    }

    public sealed class ConsoleAreaViewSystem
    {
        private readonly Dictionary<int, AreaEffectEntry> _areas = new();

        public void Spawn(int areaId, int templateId, float centerX, float centerZ, float radius)
        {
            _areas[areaId] = new AreaEffectEntry
            {
                AreaId = areaId,
                TemplateId = templateId,
                CenterX = centerX,
                CenterZ = centerZ,
                Radius = radius
            };
        }

        public void Remove(int areaId) => _areas.Remove(areaId);
        public bool TryGet(int areaId, out AreaEffectEntry entry) => _areas.TryGetValue(areaId, out entry);
        public IReadOnlyCollection<AreaEffectEntry> GetAll() => _areas.Values;
        public int Count => _areas.Count;
        public void Clear() => _areas.Clear();
    }
}
