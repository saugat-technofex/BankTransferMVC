using System.ComponentModel.DataAnnotations;
using BankTransferMVC.Integrations.ClearJunction;

namespace BankTransferMVC.Models;

public class SettingsViewModel
{
    public bool IsSimulation { get; set; }
    public string BaseUrl { get; set; } = "";
    public string ApiKeyMasked { get; set; } = "";
    public bool HasApiPassword { get; set; }
    public string PostbackBaseUrl { get; set; } = "";
    public DateTimeOffset LastChangedAt { get; set; }
    public string LastChangedBy { get; set; } = "";

    public SimulationSettings Simulation { get; set; } = new();

    public string? ConnectivityMessage { get; set; }
    public bool? ConnectivityOk { get; set; }

    public IReadOnlyList<BankTransferMVC.Services.CjCallEntry> RecentCalls { get; set; } =
        Array.Empty<BankTransferMVC.Services.CjCallEntry>();

    /// <summary>Lightweight summary of live preflight warnings (missing creds, mock base url, etc.).</summary>
    public IReadOnlyList<string> LivePreflightWarnings { get; set; } = Array.Empty<string>();

    /// <summary>Snapshot of in-memory transfer store counts (used to show "Reset state" impact).</summary>
    public StoreSnapshot Store { get; set; } = new();
}

public class StoreSnapshot
{
    public int Wallets { get; set; }
    public int Ibans { get; set; }
    public int Payouts { get; set; }
    public int Refunds { get; set; }
    public int CardPayins { get; set; }
    public int Fx { get; set; }
    public int Checks { get; set; }
    public int Events { get; set; }
    public int Total => Wallets + Ibans + Payouts + Refunds + CardPayins + Fx + Checks + Events;
}

public class ToggleModeViewModel
{
    [Required] public string TargetMode { get; set; } = "simulation";
    public string? Confirm { get; set; }
}

public class SimulationUpdateViewModel
{
    [Range(0, 5000), Display(Name = "Min latency (ms)")]
    public int MinLatencyMs { get; set; } = 80;

    [Range(0, 10000), Display(Name = "Max latency (ms)")]
    public int MaxLatencyMs { get; set; } = 250;

    [Required, Display(Name = "Scenario")]
    public string Scenario { get; set; } = "success";

    [Display(Name = "Auto-advance pending orders")]
    public bool AutoAdvance { get; set; } = true;

    [Range(1, 120), Display(Name = "Auto-advance interval (s)")]
    public int AutoAdvanceIntervalSeconds { get; set; } = 4;

    [Display(Name = "Deliver simulated webhooks to postback URL")]
    public bool DeliverWebhooks { get; set; } = true;
}
