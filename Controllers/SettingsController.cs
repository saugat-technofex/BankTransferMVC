using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Controllers;

public class SettingsController : Controller
{
    private readonly ICjModeService _mode;
    private readonly IClearJunctionClient _cj;
    private readonly ICjCallLog _log;
    private readonly ITransferStore _store;
    private readonly ClearJunctionOptions _options;

    public SettingsController(
        ICjModeService mode,
        IClearJunctionClient cj,
        ICjCallLog log,
        ITransferStore store,
        IOptions<ClearJunctionOptions> options)
    {
        _mode = mode;
        _cj = cj;
        _log = log;
        _store = store;
        _options = options.Value;
    }

    public IActionResult Index() => View(BuildVm());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ToggleMode(ToggleModeViewModel form)
    {
        var goLive = string.Equals(form.TargetMode, "live", StringComparison.OrdinalIgnoreCase);
        if (goLive && !string.Equals(form.Confirm, "I UNDERSTAND", StringComparison.Ordinal))
        {
            TempData["Error"] = "Live mode requires typing 'I UNDERSTAND' in the confirmation box.";
            return RedirectToAction(nameof(Index));
        }
        if (goLive)
        {
            var warns = LivePreflightWarnings();
            if (warns.Any(w => w.StartsWith("BLOCKER:")))
            {
                TempData["Error"] = "Cannot switch to live: " + string.Join(" · ", warns);
                return RedirectToAction(nameof(Index));
            }
        }
        _mode.SetMode(simulation: !goLive, changedBy: User.Identity?.Name ?? "anonymous");
        TempData["Success"] = goLive
            ? "Switched to LIVE mode — calls now hit Clear Junction."
            : "Switched to SIMULATION mode — calls are served by the in-process simulator.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateSimulation(SimulationUpdateViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Some simulator settings are out of range.";
            return RedirectToAction(nameof(Index));
        }
        if (form.MinLatencyMs > form.MaxLatencyMs)
        {
            TempData["Error"] = "Min latency cannot exceed Max latency.";
            return RedirectToAction(nameof(Index));
        }
        _mode.UpdateSimulation(new SimulationSettings
        {
            MinLatencyMs = form.MinLatencyMs,
            MaxLatencyMs = form.MaxLatencyMs,
            Scenario = form.Scenario,
            AutoAdvance = form.AutoAdvance,
            AutoAdvanceIntervalSeconds = form.AutoAdvanceIntervalSeconds,
            DeliverWebhooks = form.DeliverWebhooks
        }, User.Identity?.Name ?? "anonymous");
        TempData["Success"] = "Simulator settings updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TestConnectivity()
    {
        var result = await _cj.TestConnectivityAsync();
        var vm = BuildVm();
        vm.ConnectivityOk = result.Ok;
        vm.ConnectivityMessage = $"{(result.Ok ? "OK" : "FAILED")} · {result.ElapsedMs} ms · status {result.StatusCode?.ToString() ?? "n/a"} · {result.Detail}";
        return View(nameof(Index), vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ClearCallLog()
    {
        _log.Clear();
        TempData["Success"] = "Call log cleared.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ResetState()
    {
        var before = SnapshotStore();
        _store.Reset();
        _log.Clear();
        TempData["Success"] =
            $"Simulator state reset: removed {before.Wallets} wallets, {before.Ibans} IBANs, {before.Payouts} payouts, " +
            $"{before.Refunds} refunds, {before.CardPayins} card invoices, {before.Fx} FX trades, " +
            $"{before.Checks} compliance checks, {before.Events} events.";
        return RedirectToAction(nameof(Index));
    }

    private SettingsViewModel BuildVm() => new()
    {
        IsSimulation = _mode.IsSimulation,
        BaseUrl = _options.BaseUrl,
        ApiKeyMasked = Mask(_options.ApiKey),
        HasApiPassword = !string.IsNullOrWhiteSpace(_options.ApiPassword)
            && !string.Equals(_options.ApiPassword, "REPLACE_WITH_CJ_API_PASSWORD", StringComparison.Ordinal),
        PostbackBaseUrl = _options.PostbackBaseUrl,
        LastChangedAt = _mode.LastChangedAt,
        LastChangedBy = _mode.LastChangedBy,
        Simulation = _mode.Simulation,
        RecentCalls = _log.Recent(50),
        LivePreflightWarnings = LivePreflightWarnings(),
        Store = SnapshotStore()
    };

    private StoreSnapshot SnapshotStore() => new()
    {
        Wallets = _store.ListWallets().Count,
        Ibans = _store.ListIbans().Count,
        Payouts = _store.ListPayouts().Count,
        Refunds = _store.ListRefunds().Count,
        CardPayins = _store.ListCardPayins().Count,
        Fx = _store.ListFx().Count,
        Checks = _store.ListRequisiteChecks().Count,
        Events = _store.ListEvents().Count
    };

    private List<string> LivePreflightWarnings()
    {
        var warns = new List<string>();
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            warns.Add("BLOCKER: ApiKey is not set in appsettings.json.");
        if (string.IsNullOrWhiteSpace(_options.ApiPassword)
            || string.Equals(_options.ApiPassword, "REPLACE_WITH_CJ_API_PASSWORD", StringComparison.Ordinal))
            warns.Add("BLOCKER: ApiPassword is not set (request signing will fail).");
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            warns.Add("BLOCKER: BaseUrl is not set.");
        else if (_options.BaseUrl.Contains("apiary-mock.com", StringComparison.OrdinalIgnoreCase)
              || _options.BaseUrl.Contains("private-anon-", StringComparison.OrdinalIgnoreCase))
            warns.Add("WARN: BaseUrl points at an Apiary mock — responses will not be real CJ data.");
        if (string.IsNullOrWhiteSpace(_options.PostbackBaseUrl))
            warns.Add("WARN: PostbackBaseUrl is empty — CJ webhooks won't reach your app.");
        else if (_options.PostbackBaseUrl.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase)
              || _options.PostbackBaseUrl.StartsWith("http://127.", StringComparison.OrdinalIgnoreCase))
            warns.Add("WARN: PostbackBaseUrl is on localhost — CJ cannot deliver webhooks to it from the public internet (use ngrok / tunnel).");
        return warns;
    }

    private static string Mask(string key) =>
        string.IsNullOrEmpty(key) ? "(not set)"
        : key.Length <= 8 ? new string('•', key.Length)
        : key[..4] + new string('•', Math.Min(20, key.Length - 8)) + key[^4..];
}
