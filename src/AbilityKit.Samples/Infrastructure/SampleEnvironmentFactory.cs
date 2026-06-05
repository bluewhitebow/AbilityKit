using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Infrastructure
{
    public static class SampleEnvironmentFactory
    {
        public static ISampleEnvironment Create(ExecutionMode mode)
        {
            return mode switch
            {
                ExecutionMode.Instant => new InstantEnvironment(),
                ExecutionMode.Simulated => new SimulatedEnvironment(),
                ExecutionMode.Realtime => new SimulatedEnvironment(),
                _ => new InstantEnvironment()
            };
        }
    }
}
