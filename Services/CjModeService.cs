using System.Text.Json;
using BankTransferMVC.Integrations.ClearJunction;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Services;

/// <summary>
/// Runtime state for "Simulation" vs "Live" mode. Replaces the static
/// <c>ClearJunctionOptions.SimulationMode</c> flag so users can flip the integration mode
/// from the Settings page without restarting the app.
///
/// State is persisted to <c>App_Data/cj-mode.json</c> so the choice survives restarts.
/// </summary>
public interface ICjModeService
{
    bool IsSimulation { get; }
    bool IsLive => !IsSimulation;
    SimulationSettings Simulation { get; }
    DateTimeOffset LastChangedAt { get; }
    string LastChangedBy { get; }

    void SetMode(bool simulation, string changedBy);
    void UpdateSimulation(SimulationSettings settings, string changedBy);

    /// <summary>Raised whenever mode or simulator settings change. Used by the lifecycle worker.</summary>
    event Action? Changed;
}

public static class CjModeServiceExtensions
{
    /// <summary>Short label used in logs / TempData messages: "Simulation" or "Live".</summary>
    public static string Mode(this ICjModeService svc) => svc.IsSimulation ? "Simulation" : "Live";
}

internal class CjModeService : ICjModeService
{
    private readonly object _gate = new();
    private readonly string _path;
    private readonly ILogger<CjModeService> _logger;
    private PersistedState _state;

    public CjModeService(IOptions<ClearJunctionOptions> options,
        IWebHostEnvironment env,
        ILogger<CjModeService> logger)
    {
        _logger = logger;
        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "cj-mode.json");

        if (TryLoad(out var loaded))
        {
            _state = loaded!;
            _logger.LogInformation("[CJ-MODE] loaded {Mode} from {Path}",
                _state.Simulation ? "Simulation" : "Live", _path);
        }
        else
        {
            var opts = options.Value;
            _state = new PersistedState
            {
                Simulation = opts.SimulationMode,
                Settings = opts.Simulation,
                LastChangedAt = DateTimeOffset.UtcNow,
                LastChangedBy = "appsettings.json"
            };
            Save();
        }
    }

    public bool IsSimulation { get { lock (_gate) return _state.Simulation; } }
    public SimulationSettings Simulation { get { lock (_gate) return _state.Settings; } }
    public DateTimeOffset LastChangedAt { get { lock (_gate) return _state.LastChangedAt; } }
    public string LastChangedBy { get { lock (_gate) return _state.LastChangedBy; } }

    public event Action? Changed;

    public void SetMode(bool simulation, string changedBy)
    {
        lock (_gate)
        {
            if (_state.Simulation == simulation) return;
            _state.Simulation = simulation;
            _state.LastChangedAt = DateTimeOffset.UtcNow;
            _state.LastChangedBy = changedBy;
            Save();
        }
        _logger.LogWarning("[CJ-MODE] switched to {Mode} by {By}",
            simulation ? "Simulation" : "Live", changedBy);
        Changed?.Invoke();
    }

    public void UpdateSimulation(SimulationSettings settings, string changedBy)
    {
        lock (_gate)
        {
            _state.Settings = settings;
            _state.LastChangedAt = DateTimeOffset.UtcNow;
            _state.LastChangedBy = changedBy;
            Save();
        }
        _logger.LogInformation("[CJ-MODE] simulator settings updated by {By}", changedBy);
        Changed?.Invoke();
    }

    private bool TryLoad(out PersistedState? state)
    {
        state = null;
        if (!File.Exists(_path)) return false;
        try
        {
            var json = File.ReadAllText(_path);
            state = JsonSerializer.Deserialize<PersistedState>(json);
            return state is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CJ-MODE] failed to load {Path}; will recreate.", _path);
            return false;
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(_state, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CJ-MODE] failed to save {Path}", _path);
        }
    }

    private class PersistedState
    {
        public bool Simulation { get; set; } = true;
        public SimulationSettings Settings { get; set; } = new();
        public DateTimeOffset LastChangedAt { get; set; }
        public string LastChangedBy { get; set; } = "";
    }
}
