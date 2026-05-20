using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Services;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Integrations.ClearJunction;

/// <summary>
/// Mode-aware Clear Junction client. Routes each call to either:
///   - the in-process <see cref="ICjSimulator"/> (when ICjModeService.IsSimulation is true)
///   - the live REST API over signed HTTP
///
/// Every call (in either mode) is captured by <see cref="ICjCallLog"/> for the Settings panel.
/// </summary>
public class ClearJunctionClient : IClearJunctionClient
{
    private readonly HttpClient _http;
    private readonly IClearJunctionSignatureService _signer;
    private readonly ClearJunctionOptions _options;
    private readonly ICjModeService _mode;
    private readonly ICjSimulator _sim;
    private readonly ICjCallLog _calls;
    private readonly ILogger<ClearJunctionClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public ClearJunctionClient(
        HttpClient http,
        IClearJunctionSignatureService signer,
        IOptions<ClearJunctionOptions> options,
        ICjModeService mode,
        ICjSimulator sim,
        ICjCallLog calls,
        ILogger<ClearJunctionClient> logger)
    {
        _http = http;
        _signer = signer;
        _options = options.Value;
        _mode = mode;
        _sim = sim;
        _calls = calls;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }
    }

    private static string PayoutPath(string rail) => rail switch
    {
        "internal" => "v7/gate/payout/internalPayment",
        "sepa" => "v7/gate/payout/bankTransfer/eu",
        "sepaInst" => "v7/gate/payout/bankTransfer/sepaInst",
        "fps" => "v7/gate/payout/bankTransfer/fps",
        "chaps" => "v7/gate/payout/bankTransfer/chaps",
        "chapsCrossScheme" => "v7/gate/payout/bankTransfer/chapsCrossScheme",
        "swift" => "v7/gate/payout/bankTransfer/swift",
        _ => throw new ArgumentException($"Unsupported payout rail '{rail}'", nameof(rail))
    };

    // ====================== Virtual IBAN ======================
    public Task<AllocateIbanResponse> AllocateIbanAsync(AllocateIbanRequest req, CancellationToken ct = default) =>
        Dispatch("POST", "v7/gate/allocate/v3/create/iban", req,
            () => _sim.AllocateIbanAsync(req, ct),
            path => PostAsync<AllocateIbanRequest, AllocateIbanResponse>(path, req, ct));

    // ====================== FX ======================
    public Task<FxRateResponse> GetFxRateAsync(FxRateRequest req, CancellationToken ct = default) =>
        Dispatch("POST", "v7/gate/fx/instant/rate", req,
            () => _sim.GetFxRateAsync(req, ct),
            path => PostAsync<FxRateRequest, FxRateResponse>(path, req, ct));

    public Task<FxTransferResponse> CreateFxTransferAsync(FxTransferRequest req, CancellationToken ct = default) =>
        Dispatch("POST", "v7/gate/fx/instant/transfer", req,
            () => _sim.CreateFxTransferAsync(req, ct),
            path => PostAsync<FxTransferRequest, FxTransferResponse>(path, req, ct));

    // ====================== Pay-out ======================
    public Task<PayoutResponse> CreatePayoutAsync(string rail, PayoutRequest req, CancellationToken ct = default)
    {
        var path = PayoutPath(rail);
        return Dispatch("POST", path, req,
            () => _sim.CreatePayoutAsync(rail, req, ct),
            p => PostAsync<PayoutRequest, PayoutResponse>(p, req, ct));
    }

    public async Task<PayoutStatusResponse?> GetPayoutStatusAsync(string clientOrder, CancellationToken ct = default)
    {
        if (_mode.IsSimulation) return null; // simulator advances via the lifecycle worker

        var path = $"v7/gate/status/payout/clientOrder/{clientOrder}";
        var sw = Stopwatch.StartNew();
        try
        {
            var date = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            ApplyAuthHeaders(req, date, string.Empty);
            using var resp = await _http.SendAsync(req, ct);
            sw.Stop();
            var body = resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync(ct) : null;
            Log("GET", path, (int)resp.StatusCode, sw.ElapsedMilliseconds, null, body, null);
            if (!resp.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<PayoutStatusResponse>(body!, JsonOptions);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("GET", path, null, sw.ElapsedMilliseconds, null, null, ex.Message);
            throw;
        }
    }

    // ====================== Card pay-in ======================
    public Task<CardPayinResponse> CreateCardPayinAsync(CardPayinRequest req, CancellationToken ct = default) =>
        Dispatch("POST", "v7/gate/invoice/creditCard", req,
            () => _sim.CreateCardPayinAsync(req, ct),
            path => PostAsync<CardPayinRequest, CardPayinResponse>(path, req, ct));

    // ====================== Refund ======================
    public Task<RefundResponse> CreateRefundAsync(RefundRequest req, CancellationToken ct = default) =>
        Dispatch("POST", "v7/gate/refund", req,
            () => _sim.CreateRefundAsync(req, ct),
            path => PostAsync<RefundRequest, RefundResponse>(path, req, ct));

    // ====================== Transaction action ======================
    public Task<TransactionActionResponse> TransactionActionAsync(string action, TransactionActionRequest req, CancellationToken ct = default) =>
        Dispatch("POST", $"v7/gate/transactionAction/{action}", req,
            () => _sim.TransactionActionAsync(action, req, ct),
            path => PostAsync<TransactionActionRequest, TransactionActionResponse>(path, req, ct));

    // ====================== Requisite checks ======================
    public Task<CopCheckResponse> CheckCopAsync(CopCheckRequest req, CancellationToken ct = default) =>
        Dispatch("POST", "v7/gate/checkRequisite/cop", req,
            () => _sim.CheckCopAsync(req, ct),
            path => PostAsync<CopCheckRequest, CopCheckResponse>(path, req, ct));

    public Task<IbanCheckResponse> CheckIbanAsync(string iban, CancellationToken ct = default)
    {
        var path = $"v7/gate/checkRequisite/bankTransfer/eu/iban/{iban}";
        return Dispatch("GET", path, new { iban },
            () => _sim.CheckIbanAsync(iban, ct),
            p => GetAsync<IbanCheckResponse>(p, ct));
    }

    // ====================== Wallets ======================
    public Task<CreateWalletResponse> CreateWalletAsync(CreateWalletRequest req, CancellationToken ct = default) =>
        Dispatch("POST", "v7/gate/wallets/corporate", req,
            () => _sim.CreateWalletAsync(req, ct),
            path => PostAsync<CreateWalletRequest, CreateWalletResponse>(path, req, ct));

    public Task<WalletGetResponse> GetWalletAsync(string walletUuid, CancellationToken ct = default)
    {
        var path = $"v7/gate/wallets/{walletUuid}";
        return Dispatch("GET", path, new { walletUuid },
            () => _sim.GetWalletAsync(walletUuid, ct),
            p => GetAsync<WalletGetResponse>(p, ct));
    }

    public Task<WalletTransferResponse> WalletTransferAsync(WalletTransferRequest req, CancellationToken ct = default) =>
        Dispatch("POST", "v7/gate/wallets/transfer", req,
            () => _sim.WalletTransferAsync(req, ct),
            path => PostAsync<WalletTransferRequest, WalletTransferResponse>(path, req, ct));

    // ====================== Reports ======================
    public Task<TransactionReportResponse> TransactionReportAsync(TransactionReportRequest req, CancellationToken ct = default) =>
        Dispatch("POST", "v7/gate/reports/transactionReport", req,
            () => _sim.TransactionReportAsync(req, ct),
            path => PostAsync<TransactionReportRequest, TransactionReportResponse>(path, req, ct));

    // ====================== Connectivity probe ======================
    public async Task<ConnectivityResult> TestConnectivityAsync(CancellationToken ct = default)
    {
        if (_mode.IsSimulation)
            return new ConnectivityResult(true, 200, 0, "Simulation mode — no network call made.");

        var sw = Stopwatch.StartNew();
        try
        {
            using var probe = new HttpRequestMessage(HttpMethod.Get, "v7/gate/info");
            ApplyAuthHeaders(probe, DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), "");
            using var resp = await _http.SendAsync(probe, ct);
            sw.Stop();
            Log("GET", "v7/gate/info", (int)resp.StatusCode, sw.ElapsedMilliseconds, null,
                resp.IsSuccessStatusCode ? "ok" : null, resp.IsSuccessStatusCode ? null : resp.ReasonPhrase);
            return new ConnectivityResult(
                resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.NotFound,
                (int)resp.StatusCode,
                sw.ElapsedMilliseconds,
                resp.IsSuccessStatusCode
                    ? "Live endpoint reachable; credentials accepted."
                    : $"Endpoint reachable but returned {(int)resp.StatusCode} {resp.ReasonPhrase}.");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("GET", "v7/gate/info", null, sw.ElapsedMilliseconds, null, null, ex.Message);
            return new ConnectivityResult(false, null, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    // ====================== plumbing ======================
    private async Task<TResp> Dispatch<TReq, TResp>(
        string method, string path, TReq requestBody,
        Func<Task<TResp>> sim,
        Func<string, Task<TResp>> live)
    {
        var sw = Stopwatch.StartNew();
        var mode = _mode.IsSimulation ? "Simulation" : "Live";
        string? requestJson = null;
        try
        {
            requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);
        }
        catch { /* ignore */ }

        try
        {
            var resp = _mode.IsSimulation ? await sim() : await live(path);
            sw.Stop();
            var responseJson = JsonSerializer.Serialize(resp, JsonOptions);
            Log(method, path, 200, sw.ElapsedMilliseconds, requestJson, responseJson, null, mode);
            return resp;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log(method, path, null, sw.ElapsedMilliseconds, requestJson, null, ex.Message, mode);
            throw;
        }
    }

    private async Task<TResp> PostAsync<TReq, TResp>(string path, TReq body, CancellationToken ct)
    {
        var date = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var bodyJson = JsonSerializer.Serialize(body, JsonOptions);

        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        ApplyAuthHeaders(req, date, bodyJson);

        _logger.LogInformation("[CJ-LIVE] POST {Path}", path);

        using var resp = await _http.SendAsync(req, ct);
        var respJson = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("[CJ-LIVE] {Status} {Path}: {Body}", (int)resp.StatusCode, path, respJson);
            throw new InvalidOperationException($"Clear Junction {(int)resp.StatusCode}: {respJson}");
        }

        return JsonSerializer.Deserialize<TResp>(respJson, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from Clear Junction");
    }

    private async Task<TResp> GetAsync<TResp>(string path, CancellationToken ct)
    {
        var date = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        ApplyAuthHeaders(req, date, string.Empty);
        using var resp = await _http.SendAsync(req, ct);
        var respJson = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Clear Junction {(int)resp.StatusCode}: {respJson}");
        }
        return JsonSerializer.Deserialize<TResp>(respJson, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from Clear Junction");
    }

    private void ApplyAuthHeaders(HttpRequestMessage req, string date, string body)
    {
        var signature = _signer.ComputeSignature(_options.ApiKey, date, body);
        req.Headers.TryAddWithoutValidation("Date", date);
        req.Headers.TryAddWithoutValidation("X-API-KEY", _options.ApiKey);
        req.Headers.TryAddWithoutValidation("Authorization", signature);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private void Log(string method, string path, int? status, long elapsed,
        string? request, string? response, string? error, string? mode = null)
    {
        _calls.Record(new CjCallEntry(
            DateTimeOffset.UtcNow,
            mode ?? (_mode.IsSimulation ? "Simulation" : "Live"),
            method,
            path,
            status,
            elapsed,
            Truncate(request, 1200),
            Truncate(response, 1200),
            error));
    }

    private static string? Truncate(string? s, int max) =>
        s is null ? null : (s.Length <= max ? s : s.Substring(0, max) + "…");
}
