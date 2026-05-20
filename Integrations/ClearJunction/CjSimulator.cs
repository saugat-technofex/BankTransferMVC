using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Services;

namespace BankTransferMVC.Integrations.ClearJunction;

/// <summary>
/// Generates realistic Clear Junction responses without touching the network.
/// Centralises the simulation behaviour previously inlined into <c>ClearJunctionClient</c>.
///
/// The simulator:
///   - injects latency between <see cref="SimulationSettings.MinLatencyMs"/> and Max
///   - applies the active <see cref="SimulationSettings.Scenario"/> (success, declined, etc.)
///   - generates plausible IBANs / order references / timestamps
///   - exposes <see cref="AdvanceLifecycle"/> so a hosted worker can move pending orders forward
/// </summary>
public interface ICjSimulator
{
    Task<AllocateIbanResponse> AllocateIbanAsync(AllocateIbanRequest req, CancellationToken ct = default);
    Task<FxRateResponse> GetFxRateAsync(FxRateRequest req, CancellationToken ct = default);
    Task<FxTransferResponse> CreateFxTransferAsync(FxTransferRequest req, CancellationToken ct = default);
    Task<PayoutResponse> CreatePayoutAsync(string rail, PayoutRequest req, CancellationToken ct = default);
    Task<CardPayinResponse> CreateCardPayinAsync(CardPayinRequest req, CancellationToken ct = default);
    Task<RefundResponse> CreateRefundAsync(RefundRequest req, CancellationToken ct = default);
    Task<TransactionActionResponse> TransactionActionAsync(string action, TransactionActionRequest req, CancellationToken ct = default);
    Task<CopCheckResponse> CheckCopAsync(CopCheckRequest req, CancellationToken ct = default);
    Task<IbanCheckResponse> CheckIbanAsync(string iban, CancellationToken ct = default);
    Task<WalletGetResponse> GetWalletAsync(string walletUuid, CancellationToken ct = default);
    Task<WalletTransferResponse> WalletTransferAsync(WalletTransferRequest req, CancellationToken ct = default);
    Task<TransactionReportResponse> TransactionReportAsync(TransactionReportRequest req, CancellationToken ct = default);

    Task<CreateWalletResponse> CreateWalletAsync(CreateWalletRequest req, CancellationToken ct = default);

    /// <summary>
    /// Move a pending operation one step forward (pending → processing → settled, or → declined).
    /// Returns the new (status, oper, compliance) tuple or null if unchanged.
    /// Used by <c>CjLifecycleWorker</c>.
    /// </summary>
    (string Status, string OperStatus, string ComplianceStatus)? AdvanceLifecycle(
        string currentStatus, string operStatus, string complianceStatus, string scenario);
}

public class CjSimulator : ICjSimulator
{
    private readonly ICjModeService _mode;
    private readonly BankTransferMVC.Services.ITransferStore _store;
    private readonly ILogger<CjSimulator> _logger;
    private static readonly Random Rng = new();

    public CjSimulator(
        ICjModeService mode,
        BankTransferMVC.Services.ITransferStore store,
        ILogger<CjSimulator> logger)
    {
        _mode = mode;
        _store = store;
        _logger = logger;
    }

    private SimulationSettings S => _mode.Simulation;

    private async Task DelayAsync(CancellationToken ct)
    {
        var min = Math.Max(0, S.MinLatencyMs);
        var max = Math.Max(min, S.MaxLatencyMs);
        if (max == 0) return;
        await Task.Delay(Rng.Next(min, max + 1), ct);
    }

    private void MaybeFail(string label)
    {
        if (string.Equals(S.Scenario, CjScenarios.NetworkError, StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpRequestException($"Simulated CJ network failure on {label} (scenario={S.Scenario})");
        }
    }

    private static string Iso(DateTimeOffset t) => t.ToString("yyyy-MM-ddTHH:mm:sszzz");

    private static (string oper, string compliance) InitialSubstatuses(string scenario) => scenario switch
    {
        CjScenarios.InsufficientFunds => ("declined", "approved"),
        CjScenarios.Declined => ("declined", "approved"),
        CjScenarios.ComplianceHold => ("pending", "pending"),
        _ => ("pending", "pending")
    };

    private static string InitialStatus(string scenario) => scenario switch
    {
        CjScenarios.InsufficientFunds => "failed",
        CjScenarios.Declined => "failed",
        _ => "created"
    };

    public async Task<AllocateIbanResponse> AllocateIbanAsync(AllocateIbanRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("AllocateIban");
        var orderRef = Guid.NewGuid().ToString();
        return new AllocateIbanResponse
        {
            RequestReference = orderRef,
            ClientOrder = req.ClientOrder,
            OrderReference = orderRef,
            Status = "accepted",
            Iban = GenerateMockIban(req.IbanCountry)
        };
    }

    public async Task<FxRateResponse> GetFxRateAsync(FxRateRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("FxRate");
        var rate = SimulatedRate(req.SellCurrency, req.BuyCurrency);
        var expires = Iso(DateTimeOffset.UtcNow.AddMinutes(5));
        return new FxRateResponse
        {
            RequestReference = Guid.NewGuid().ToString(),
            Quotes = new List<FxQuote>
            {
                new() { RateUuid = Guid.NewGuid().ToString(), Quote = rate.ToString("0.0000"),
                        SellCurrency = req.SellCurrency, BuyCurrency = req.BuyCurrency, ExpirationTimestamp = expires },
                new() { RateUuid = Guid.NewGuid().ToString(), Quote = (1m / rate).ToString("0.0000"),
                        SellCurrency = req.BuyCurrency, BuyCurrency = req.SellCurrency, ExpirationTimestamp = expires }
            }
        };
    }

    public async Task<FxTransferResponse> CreateFxTransferAsync(FxTransferRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("FxTransfer");
        var orderRef = Guid.NewGuid().ToString();
        var now = Iso(DateTimeOffset.UtcNow);
        return new FxTransferResponse
        {
            ClientOrder = req.ClientOrder,
            OrderReference = orderRef,
            OperTimestamp = now,
            SellAmount = req.SellAmount,
            SellCurrency = req.SellCurrency,
            BuyAmount = req.BuyAmount,
            BuyCurrency = req.BuyCurrency,
            Status = string.Equals(S.Scenario, CjScenarios.Declined, StringComparison.OrdinalIgnoreCase)
                ? "failed" : "pending",
            RateUuid = req.RateUuid,
            RequestReference = orderRef,
            CreatedAt = now
        };
    }

    public async Task<PayoutResponse> CreatePayoutAsync(string rail, PayoutRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("Payout");
        var (oper, compl) = InitialSubstatuses(S.Scenario);
        var status = InitialStatus(S.Scenario);
        var orderRef = Guid.NewGuid().ToString();
        return new PayoutResponse
        {
            RequestReference = orderRef,
            ClientOrder = req.ClientOrder,
            OrderReference = orderRef,
            CreatedAt = Iso(DateTimeOffset.UtcNow),
            Status = status,
            SubStatuses = new CjSubStatuses { OperStatus = oper, ComplianceStatus = compl },
            Messages = status == "failed"
                ? new List<CjMessage> { new() { Code = "010", Message = "Simulated decline", Details = S.Scenario } }
                : null
        };
    }

    public async Task<CardPayinResponse> CreateCardPayinAsync(CardPayinRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("CardPayin");
        var orderRef = Guid.NewGuid().ToString();
        var (oper, compl) = InitialSubstatuses(S.Scenario);
        return new CardPayinResponse
        {
            RequestReference = orderRef,
            ClientOrder = req.ClientOrder,
            OrderReference = orderRef,
            Status = "pending",
            SubStatuses = new CjSubStatuses { OperStatus = oper, ComplianceStatus = compl },
            RedirectUrl = $"https://sandbox.clearjunction.com/pay/{req.ClientOrder}"
        };
    }

    public async Task<RefundResponse> CreateRefundAsync(RefundRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("Refund");
        var (oper, compl) = InitialSubstatuses(S.Scenario);
        return new RefundResponse
        {
            RequestReference = Guid.NewGuid().ToString(),
            ClientOrder = req.ClientOrder,
            OrderReference = Guid.NewGuid().ToString(),
            Status = InitialStatus(S.Scenario) == "failed" ? "failed" : "created",
            SubStatuses = new CjSubStatuses { OperStatus = oper, ComplianceStatus = compl }
        };
    }

    public async Task<TransactionActionResponse> TransactionActionAsync(string action, TransactionActionRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail($"TxnAction.{action}");
        var isApprove = string.Equals(action, "approve", StringComparison.OrdinalIgnoreCase);
        return new TransactionActionResponse
        {
            ClientOrder = req.ClientOrder,
            OrderReference = Guid.NewGuid().ToString(),
            Status = isApprove ? "completed" : "failed",
            SubStatuses = new CjSubStatuses
            {
                OperStatus = isApprove ? "settled" : "declined",
                ComplianceStatus = isApprove ? "approved" : "declined"
            }
        };
    }

    public async Task<CopCheckResponse> CheckCopAsync(CopCheckRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("CheckRequisite.cop");
        var name = (req.Name ?? "").Trim();
        var sort = (req.SortCode ?? "").Trim();
        var acct = (req.AccountNumber ?? "").Trim();

        // Account not found: missing account/sort code.
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(sort) || string.IsNullOrEmpty(acct))
        {
            return new CopCheckResponse
            {
                RequestReference = Guid.NewGuid().ToString(),
                Result = "accountNotFound",
                ReasonCode = "BAV00",
                MatchedName = null
            };
        }

        // Scenarios drive deterministic outcomes for demos.
        if (string.Equals(S.Scenario, CjScenarios.Declined, StringComparison.OrdinalIgnoreCase))
            return new CopCheckResponse { RequestReference = Guid.NewGuid().ToString(), Result = "noMatch", ReasonCode = "BAV04" };
        if (string.Equals(S.Scenario, CjScenarios.ComplianceHold, StringComparison.OrdinalIgnoreCase))
            return new CopCheckResponse { RequestReference = Guid.NewGuid().ToString(), Result = "unavailable", ReasonCode = "BAV99" };

        // Default: parity of sort+account drives result variety.
        var checksum = (sort + acct).Where(char.IsDigit).Sum(c => c - '0');
        var result = (checksum % 5) switch
        {
            0 => "noMatch",
            1 => "closeMatch",
            _ => "match"
        };
        return new CopCheckResponse
        {
            RequestReference = Guid.NewGuid().ToString(),
            Result = result,
            ReasonCode = result == "closeMatch" ? "BAV05" : null,
            MatchedName = result == "match" ? name : (result == "closeMatch" ? ToTitleCase(name) : null)
        };
    }

    private static string ToTitleCase(string s) =>
        string.IsNullOrEmpty(s) ? "" :
        System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());

    public async Task<IbanCheckResponse> CheckIbanAsync(string iban, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("CheckRequisite.iban");
        if (string.IsNullOrWhiteSpace(iban) || iban.Length < 15)
        {
            return new IbanCheckResponse { Iban = iban ?? "", Result = "invalidFormat" };
        }
        return new IbanCheckResponse
        {
            Iban = iban,
            Result = "reachable",
            Country = iban[..2],
            BankName = $"Bank of {iban[..2]} (simulated)",
            Bic = $"{iban[..4]}DE00"
        };
    }

    public async Task<WalletGetResponse> GetWalletAsync(string walletUuid, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("GetWallet");

        // Prefer real store state so the simulator stays consistent with what the UI shows.
        var existing = _store.GetWallet(walletUuid);
        if (existing is not null)
        {
            return new WalletGetResponse
            {
                WalletUuid = existing.WalletUuid,
                Name = existing.Name,
                Currency = existing.Currency,
                Balance = existing.Balance,
                Status = existing.Status
            };
        }
        return new WalletGetResponse
        {
            WalletUuid = walletUuid,
            Name = "Simulated wallet",
            Currency = "EUR",
            Balance = Math.Round((decimal)(Rng.NextDouble() * 50_000), 2),
            Status = "active"
        };
    }

    public async Task<WalletTransferResponse> WalletTransferAsync(WalletTransferRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("WalletTransfer");

        var from = _store.GetWallet(req.FromWalletUuid);
        var to = _store.GetWallet(req.ToWalletUuid);
        if (string.Equals(S.Scenario, CjScenarios.InsufficientFunds, StringComparison.OrdinalIgnoreCase)
            || (from is not null && from.Balance < req.Amount))
        {
            return new WalletTransferResponse
            {
                ClientOrder = req.ClientOrder,
                OrderReference = Guid.NewGuid().ToString(),
                Status = "failed"
            };
        }

        // Move balances if both wallets exist locally — keeps sim ledger consistent.
        if (from is not null) from.Balance -= req.Amount;
        if (to is not null) to.Balance += req.Amount;

        return new WalletTransferResponse
        {
            ClientOrder = req.ClientOrder,
            OrderReference = Guid.NewGuid().ToString(),
            Status = "completed"
        };
    }

    public async Task<TransactionReportResponse> TransactionReportAsync(TransactionReportRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("TransactionReport");
        // Reconstructed from the local in-memory transfer store so the simulator mirrors what
        // CJ would return after the same chain of operations. Callers can still post-filter
        // by type / wallet on top of this baseline.
        var rows = new List<TransactionReportRow>();

        foreach (var p in _store.ListPayouts())
        {
            rows.Add(new TransactionReportRow
            {
                ClientOrder = p.ClientOrder,
                OrderReference = p.OrderReference,
                TransactionType = "Payout",
                Currency = p.Currency,
                Amount = p.Amount,
                Status = p.Status,
                CreatedAt = Iso(p.CreatedAt)
            });
        }
        foreach (var r in _store.ListRefunds())
        {
            rows.Add(new TransactionReportRow
            {
                ClientOrder = r.ClientOrder,
                OrderReference = r.OrderReference,
                TransactionType = "Refund",
                Currency = r.Currency,
                Amount = r.Amount,
                Status = r.Status,
                CreatedAt = Iso(r.CreatedAt)
            });
        }
        foreach (var c in _store.ListCardPayins())
        {
            rows.Add(new TransactionReportRow
            {
                ClientOrder = c.ClientOrder,
                OrderReference = c.OrderReference,
                TransactionType = "Payin",
                Currency = c.Currency,
                Amount = c.Amount,
                Status = c.Status,
                CreatedAt = Iso(c.CreatedAt)
            });
        }
        foreach (var f in _store.ListFx())
        {
            rows.Add(new TransactionReportRow
            {
                ClientOrder = f.ClientOrder,
                OrderReference = f.OrderReference,
                TransactionType = "InstantFxTransfer",
                Currency = f.SellCurrency,
                Amount = f.SellAmount,
                Status = f.Status,
                CreatedAt = Iso(f.CreatedAt)
            });
        }
        return new TransactionReportResponse { Rows = rows.OrderByDescending(r => r.CreatedAt).ToList() };
    }

    public async Task<CreateWalletResponse> CreateWalletAsync(CreateWalletRequest req, CancellationToken ct = default)
    {
        await DelayAsync(ct);
        MaybeFail("CreateWallet");
        return new CreateWalletResponse
        {
            RequestReference = Guid.NewGuid().ToString(),
            ClientOrder = req.ClientOrder,
            OrderReference = Guid.NewGuid().ToString(),
            WalletUuid = Guid.NewGuid().ToString(),
            Currency = req.Currency,
            Status = "active"
        };
    }

    public (string Status, string OperStatus, string ComplianceStatus)? AdvanceLifecycle(
        string currentStatus, string operStatus, string complianceStatus, string scenario)
    {
        // Apply scenario at advance-time too (so changing the scenario mid-flight has effect).
        if (string.Equals(scenario, CjScenarios.ComplianceHold, StringComparison.OrdinalIgnoreCase))
        {
            // Compliance never approves — operational still progresses to processing then halts.
            if (operStatus == "pending") return (currentStatus, "processing", "pending");
            return null;
        }

        if (string.Equals(scenario, CjScenarios.Declined, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scenario, CjScenarios.InsufficientFunds, StringComparison.OrdinalIgnoreCase))
        {
            if (operStatus != "declined") return ("failed", "declined", "declined");
            return null;
        }

        // success path
        var nextOper = operStatus switch
        {
            "pending" => "processing",
            "processing" => "settled",
            _ => operStatus
        };
        var nextCompliance = complianceStatus == "pending" ? "approved" : complianceStatus;
        var nextStatus = nextOper == "settled" ? "completed" : (currentStatus == "" ? "created" : currentStatus);

        if (nextOper == operStatus && nextCompliance == complianceStatus && nextStatus == currentStatus) return null;
        return (nextStatus, nextOper, nextCompliance);
    }

    // ---------- helpers ----------
    private static string GenerateMockIban(string country)
    {
        var c = string.IsNullOrEmpty(country) ? "GB" : country.ToUpperInvariant();
        var digits = Random.Shared.NextInt64(10_000_000_000_000_000L, 99_999_999_999_999_999L);
        return $"{c}00CLJU{digits}";
    }

    private static decimal SimulatedRate(string sell, string buy)
    {
        if (string.Equals(sell, buy, StringComparison.OrdinalIgnoreCase)) return 1m;
        var table = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["EUR/USD"] = 1.0850m, ["USD/EUR"] = 0.9220m,
            ["GBP/USD"] = 1.2700m, ["USD/GBP"] = 0.7874m,
            ["EUR/GBP"] = 0.8550m, ["GBP/EUR"] = 1.1700m,
            ["EUR/CHF"] = 0.9510m, ["EUR/PLN"] = 4.3200m
        };
        return table.TryGetValue($"{sell}/{buy}", out var r) ? r : 1.0000m;
    }
}
