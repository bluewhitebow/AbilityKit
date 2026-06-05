using System;
using System.Collections.Generic;

namespace AbilityKit.Samples.Abstractions
{
    /// <summary>
    /// A catalog item that can be shown by any sample host.
    /// </summary>
    public sealed class SampleCatalogEntry
    {
        /// <summary>
        /// Creates a catalog entry.
        /// </summary>
        public SampleCatalogEntry(
            int index,
            string id,
            string title,
            string description,
            SampleCategory category,
            Type sampleType,
            Func<ISample> factory,
            int priority = 100,
            string[]? tags = null)
        {
            Index = index;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Category = category;
            SampleType = sampleType ?? throw new ArgumentNullException(nameof(sampleType));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Priority = priority;
            Tags = tags ?? Array.Empty<string>();
        }

        private readonly Func<ISample> _factory;

        /// <summary>Display index.</summary>
        public int Index { get; }
        /// <summary>Stable id for UI selections and persisted preferences.</summary>
        public string Id { get; }
        /// <summary>Display title.</summary>
        public string Title { get; }
        /// <summary>Short description.</summary>
        public string Description { get; }
        /// <summary>Sample category.</summary>
        public SampleCategory Category { get; }
        /// <summary>Concrete sample type.</summary>
        public Type SampleType { get; }
        /// <summary>Sort priority from the sample attribute.</summary>
        public int Priority { get; }
        /// <summary>Optional tags for filtering.</summary>
        public IReadOnlyList<string> Tags { get; }

        /// <summary>
        /// Creates a fresh sample instance.
        /// </summary>
        public ISample CreateSample()
        {
            return _factory();
        }
    }
}
