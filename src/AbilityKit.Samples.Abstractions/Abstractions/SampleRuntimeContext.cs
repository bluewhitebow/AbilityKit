using System;

namespace AbilityKit.Samples.Abstractions
{
    /// <summary>
    /// Runtime services supplied by a concrete sample host.
    /// </summary>
    public sealed class SampleRuntimeContext
    {
        public SampleRuntimeContext(
            ILogger output,
            ISampleEnvironment environment,
            SampleHostKind hostKind = SampleHostKind.Logic,
            IConfigProvider? config = null,
            IResourceProvider? resources = null,
            string? outputDirectory = null)
        {
            Output = output ?? throw new ArgumentNullException(nameof(output));
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            HostKind = hostKind;
            Config = config;
            Resources = resources;
            OutputDirectory = outputDirectory ?? string.Empty;
        }

        public ILogger Output { get; }
        public ISampleEnvironment Environment { get; }
        public SampleHostKind HostKind { get; }
        public IConfigProvider? Config { get; }
        public IResourceProvider? Resources { get; }
        public string OutputDirectory { get; }
    }
}
