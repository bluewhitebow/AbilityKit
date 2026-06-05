namespace AbilityKit.Samples.Abstractions
{
    /// <summary>
    /// Options used by a host when running samples.
    /// </summary>
    public sealed class SampleRunOptions
    {
        public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Instant;
        public SampleHostKind HostKind { get; set; } = SampleHostKind.Console;
        public bool WriteConsole { get; set; } = true;
        public bool WriteFile { get; set; }
        public string OutputDirectory { get; set; } = "sample-output";
    }
}
