namespace OmniRemote.Core.Simulation;

public enum SimulationScenario
{
    /// <summary>Fast, reliable responses.</summary>
    Normal,
    /// <summary>High latency (2.5 s) to exercise loading states.</summary>
    Slow,
    /// <summary>25% failures and 15% timeouts.</summary>
    Flaky,
    /// <summary>All virtual devices unreachable.</summary>
    Offline,
    /// <summary>Use the custom values below.</summary>
    Custom,
}

/// <summary>User-configurable simulation behaviour. Persisted as non-sensitive settings.</summary>
public sealed class SimulationSettings
{
    public SimulationScenario Scenario { get; set; } = SimulationScenario.Normal;
    public int CustomLatencyMs { get; set; } = 150;
    public int CustomFailurePercent { get; set; }
    public int CustomTimeoutPercent { get; set; }

    public int EffectiveLatencyMs => Scenario switch
    {
        SimulationScenario.Normal => 150,
        SimulationScenario.Slow => 2500,
        SimulationScenario.Flaky => 400,
        SimulationScenario.Offline => 300,
        _ => Math.Clamp(CustomLatencyMs, 0, 4000),
    };

    public int EffectiveFailurePercent => Scenario switch
    {
        SimulationScenario.Flaky => 25,
        SimulationScenario.Custom => Math.Clamp(CustomFailurePercent, 0, 100),
        _ => 0,
    };

    public int EffectiveTimeoutPercent => Scenario switch
    {
        SimulationScenario.Flaky => 15,
        SimulationScenario.Custom => Math.Clamp(CustomTimeoutPercent, 0, 100),
        _ => 0,
    };

    public bool AllOffline => Scenario == SimulationScenario.Offline;
}

/// <summary>Random source abstraction so simulation is deterministic in tests.</summary>
public interface ISimulationRandom
{
    /// <summary>Returns a value in [0, 100).</summary>
    int NextPercent();
    double NextDouble();
}

public sealed class SystemSimulationRandom : ISimulationRandom
{
#pragma warning disable CA5394 // Not used for security: simulation noise only.
    public int NextPercent() => Random.Shared.Next(100);
    public double NextDouble() => Random.Shared.NextDouble();
#pragma warning restore CA5394
}
