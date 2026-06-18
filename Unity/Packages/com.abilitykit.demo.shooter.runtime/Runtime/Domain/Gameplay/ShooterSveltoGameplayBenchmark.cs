#nullable enable

using System;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public readonly struct ShooterSveltoGameplayBenchmarkProfile
    {
        public ShooterSveltoGameplayBenchmarkProfile(string id, string displayName, ShooterSveltoGameplayScenarioConfig scenario, int iterations)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Profile id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Profile display name is required.", nameof(displayName));

            Id = id;
            DisplayName = displayName;
            Scenario = scenario;
            Iterations = iterations < 1 ? 1 : iterations;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public ShooterSveltoGameplayScenarioConfig Scenario { get; }
        public int Iterations { get; }
    }

    public readonly struct ShooterSveltoGameplayBenchmarkResult
    {
        public ShooterSveltoGameplayBenchmarkResult(
            string profileId,
            string scenarioId,
            int iterations,
            int framesPerIteration,
            int initialEntityCount,
            long totalFrames,
            long totalInitialEntityFrames,
            bool deterministic,
            ShooterSveltoGameplayScenarioResult firstResult,
            ShooterSveltoGameplayScenarioResult lastResult)
        {
            ProfileId = profileId;
            ScenarioId = scenarioId;
            Iterations = iterations;
            FramesPerIteration = framesPerIteration;
            InitialEntityCount = initialEntityCount;
            TotalFrames = totalFrames;
            TotalInitialEntityFrames = totalInitialEntityFrames;
            Deterministic = deterministic;
            FirstResult = firstResult;
            LastResult = lastResult;
        }

        public string ProfileId { get; }
        public string ScenarioId { get; }
        public int Iterations { get; }
        public int FramesPerIteration { get; }
        public int InitialEntityCount { get; }
        public long TotalFrames { get; }
        public long TotalInitialEntityFrames { get; }
        public bool Deterministic { get; }
        public ShooterSveltoGameplayScenarioResult FirstResult { get; }
        public ShooterSveltoGameplayScenarioResult LastResult { get; }
    }

    public static class ShooterSveltoGameplayBenchmarkProfiles
    {
        public static ShooterSveltoGameplayBenchmarkProfile ProjectileStormBaseline { get; } = new ShooterSveltoGameplayBenchmarkProfile(
            "svelto-projectile-storm-baseline",
            "Svelto Projectile Storm Baseline",
            ShooterSveltoGameplayScenarioCatalog.ProjectileStorm,
            iterations: 3);

        public static ShooterSveltoGameplayBenchmarkProfile WaveSurvivalBaseline { get; } = new ShooterSveltoGameplayBenchmarkProfile(
            "svelto-wave-survival-baseline",
            "Svelto Wave Survival Baseline",
            ShooterSveltoGameplayScenarioCatalog.WaveSurvival,
            iterations: 3);
    }

    public static class ShooterSveltoGameplayBenchmark
    {
        public static ShooterSveltoGameplayBenchmarkResult Run(
            IShooterSveltoGameplayScenarioRunner runner,
            in ShooterSveltoGameplayBenchmarkProfile profile)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            ShooterSveltoGameplayScenarioResult first = default;
            ShooterSveltoGameplayScenarioResult last = default;
            var deterministic = true;
            for (var i = 0; i < profile.Iterations; i++)
            {
                var result = runner.Run(profile.Scenario);
                if (i == 0)
                {
                    first = result;
                }
                else if (!HasSameDeterministicOutcome(in first, in result))
                {
                    deterministic = false;
                }

                last = result;
            }

            var initialEntityCount = profile.Scenario.ShooterCount + profile.Scenario.TargetCount;
            return new ShooterSveltoGameplayBenchmarkResult(
                profile.Id,
                profile.Scenario.Id,
                profile.Iterations,
                profile.Scenario.TickCount,
                initialEntityCount,
                (long)profile.Iterations * profile.Scenario.TickCount,
                (long)profile.Iterations * profile.Scenario.TickCount * initialEntityCount,
                deterministic,
                first,
                last);
        }

        private static bool HasSameDeterministicOutcome(
            in ShooterSveltoGameplayScenarioResult expected,
            in ShooterSveltoGameplayScenarioResult actual)
        {
            return string.Equals(expected.ScenarioId, actual.ScenarioId, StringComparison.Ordinal)
                && expected.Frames == actual.Frames
                && expected.Shooters == actual.Shooters
                && expected.Targets == actual.Targets
                && expected.ProjectilesSpawned == actual.ProjectilesSpawned
                && expected.ProjectilesExpired == actual.ProjectilesExpired
                && expected.Hits == actual.Hits
                && expected.DefeatedTargets == actual.DefeatedTargets
                && expected.ActiveProjectiles == actual.ActiveProjectiles
                && expected.RemainingTargetHp == actual.RemainingTargetHp
                && expected.EnemyHits == actual.EnemyHits
                && expected.StateHash == actual.StateHash;
        }
    }
}
