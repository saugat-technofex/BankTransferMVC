namespace BankTransferMVC.Integrations.ClearJunction;

public class ClearJunctionOptions
{
    public const string SectionName = "ClearJunction";

    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiPassword { get; set; } = "";
    public string PostbackBaseUrl { get; set; } = "";

    /// <summary>
    /// Initial mode at startup. The runtime mode lives in <c>ICjModeService</c> and can be
    /// toggled by users from the Settings page — this option only seeds the first run.
    /// </summary>
    public bool SimulationMode { get; set; } = true;

    /// <summary>Default simulator behaviour applied at startup.</summary>
    public SimulationSettings Simulation { get; set; } = new();
}

/// <summary>
/// Tweakable knobs that govern how the in-process simulator behaves.
/// Configurable at runtime from <c>SettingsController</c>.
/// </summary>
public class SimulationSettings
{
    /// <summary>Min artificial latency injected before each simulated CJ response (ms).</summary>
    public int MinLatencyMs { get; set; } = 80;

    /// <summary>Max artificial latency (ms). Sampled uniformly between Min and Max.</summary>
    public int MaxLatencyMs { get; set; } = 250;

    /// <summary>
    /// Scenario applied to every new simulated order until changed:
    /// <c>success</c> · <c>complianceHold</c> · <c>declined</c> · <c>insufficientFunds</c> · <c>networkError</c>.
    /// </summary>
    public string Scenario { get; set; } = "success";

    /// <summary>If true, a background worker auto-advances pending orders to settled / declined.</summary>
    public bool AutoAdvance { get; set; } = true;

    /// <summary>Auto-advance tick interval (seconds). Each tick progresses each pending order one step.</summary>
    public int AutoAdvanceIntervalSeconds { get; set; } = 4;

    /// <summary>
    /// When true the simulator POSTs the generated notification to <c>PostbackBaseUrl</c> at each
    /// lifecycle step (so the same webhook code path runs in dev as in production).
    /// </summary>
    public bool DeliverWebhooks { get; set; } = true;
}
