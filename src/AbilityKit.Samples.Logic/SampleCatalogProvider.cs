using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic
{
    /// <summary>
    /// Builds the reusable sample catalog for console, UI, Unity, MonoGame, or custom hosts.
    /// </summary>
    public static class SampleCatalogProvider
    {
        public static SampleCatalog CreateCatalog()
        {
            SampleRegistry.Instance.Initialize();

            var manifest = SampleManifest.LoadDefault();
            var catalog = new SampleCatalog();
            var sampleTypes = SampleRegistry.Instance.GetAllSampleTypes()
                .Select(type =>
                {
                    var attr = type.GetCustomAttribute<SampleAttribute>();
                    var item = manifest.Find(type);
                    return new
                    {
                        Type = type,
                        Attribute = attr,
                        Manifest = item,
                        Order = item?.Order ?? attr?.Priority ?? 100
                    };
                })
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Type.Name);

            foreach (var sample in sampleTypes)
            {
                catalog.Register(
                    sample.Type,
                    sample.Order,
                    sample.Manifest?.Tags?.Length > 0 ? sample.Manifest.Tags : sample.Attribute?.Tags ?? Array.Empty<string>(),
                    id: sample.Manifest?.Id,
                    title: sample.Manifest?.Title,
                    description: sample.Manifest?.Description);
            }

            return catalog;
        }

        private sealed class SampleManifest
        {
            public List<SampleManifestItem> Samples { get; set; } = new();

            public SampleManifestItem? Find(Type type)
            {
                return Samples.FirstOrDefault(x =>
                    string.Equals(x.Type, type.FullName, StringComparison.Ordinal) ||
                    string.Equals(x.Type, type.AssemblyQualifiedName, StringComparison.Ordinal));
            }

            public static SampleManifest LoadDefault()
            {
                var path = Path.Combine(AppContext.BaseDirectory, "sample-manifest.json");
                if (!File.Exists(path))
                    return new SampleManifest();

                try
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<SampleManifest>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new SampleManifest();
                }
                catch
                {
                    return new SampleManifest();
                }
            }
        }

        private sealed class SampleManifestItem
        {
            public string Id { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public int Order { get; set; } = 100;
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string[] Tags { get; set; } = Array.Empty<string>();
        }
    }
}
