using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Services;

/// <summary>
/// Background worker that, while running in simulation mode with auto-advance enabled, walks
/// pending payout / refund / card pay-in records and progresses them through their lifecycle
/// (pending → processing → settled  ·  pending → declined for failure scenarios).
///
/// For every status change it also POSTs a CJ-shaped webhook to <c>PostbackBaseUrl</c>, so the
/// same handler that runs in production also runs in development without manual intervention.
/// </summary>
public class CjLifecycleWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ICjModeService _mode;
    private readonly ILogger<CjLifecycleWorker> _logger;
    private readonly HttpClient _http;

    public CjLifecycleWorker(IServiceProvider services,
        ICjModeService mode,
        IHttpClientFactory httpFactory,
        ILogger<CjLifecycleWorker> logger)
    {
        _services = services;
        _mode = mode;
        _logger = logger;
        _http = httpFactory.CreateClient("cj-webhook-self");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[CJ-LIFECYCLE] worker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_mode.IsSimulation && _mode.Simulation.AutoAdvance)
                {
                    await Tick(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CJ-LIFECYCLE] tick failed");
            }

            var interval = Math.Max(1, _mode.Simulation.AutoAdvanceIntervalSeconds);
            try { await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
        _logger.LogInformation("[CJ-LIFECYCLE] worker stopped");
    }

    private async Task Tick(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ITransferStore>();
        var sim = scope.ServiceProvider.GetRequiredService<ICjSimulator>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<ClearJunctionOptions>>().Value;

        var scenario = _mode.Simulation.Scenario;

        // ---- payouts ----
        foreach (var p in store.ListPayouts())
        {
            if (IsTerminal(p.Status, p.OperStatus)) continue;
            var next = sim.AdvanceLifecycle(p.Status, p.OperStatus, p.ComplianceStatus, scenario);
            if (next is null) continue;

            store.UpdatePayoutStatus(p.ClientOrder, next.Value.Status, next.Value.OperStatus, next.Value.ComplianceStatus);
            await EmitWebhook(store, options, "payoutNotification", "payout",
                p.ClientOrder, p.OrderReference, p.Currency, p.Amount,
                next.Value.Status, next.Value.OperStatus, next.Value.ComplianceStatus, ct);
        }

        // ---- refunds ----
        foreach (var r in store.ListRefunds())
        {
            if (IsTerminal(r.Status, r.OperStatus)) continue;
            var next = sim.AdvanceLifecycle(r.Status, r.OperStatus, r.ComplianceStatus, scenario);
            if (next is null) continue;
            store.UpdateRefundStatus(r.ClientOrder, next.Value.Status, next.Value.OperStatus, next.Value.ComplianceStatus);
            await EmitWebhook(store, options, "refundNotification", "payin",
                r.ClientOrder, r.OrderReference, r.Currency, r.Amount,
                next.Value.Status, next.Value.OperStatus, next.Value.ComplianceStatus, ct);
        }

        // ---- card pay-ins ----
        foreach (var c in store.ListCardPayins())
        {
            if (IsTerminal(c.Status, c.OperStatus)) continue;
            var next = sim.AdvanceLifecycle(c.Status, c.OperStatus, c.ComplianceStatus, scenario);
            if (next is null) continue;
            store.UpdateCardPayinStatus(c.ClientOrder, next.Value.Status, next.Value.OperStatus, next.Value.ComplianceStatus);
            await EmitWebhook(store, options, "invoiceNotification", "payin",
                c.ClientOrder, c.OrderReference, c.Currency, c.Amount,
                next.Value.Status, next.Value.OperStatus, next.Value.ComplianceStatus, ct);
        }
    }

    private static bool IsTerminal(string status, string operStatus) =>
        operStatus is "settled" or "declined" ||
        status is "completed" or "failed";

    private async Task EmitWebhook(
        ITransferStore store,
        ClearJunctionOptions options,
        string type,
        string endpoint,
        string clientOrder,
        string orderReference,
        string currency,
        decimal amount,
        string status,
        string operStatus,
        string complianceStatus,
        CancellationToken ct)
    {
        // Always record in the local event store (so the UI's event log lights up immediately).
        store.AddEvent(new WebhookEvent
        {
            Type = type,
            ClientOrder = clientOrder,
            OrderReference = orderReference,
            Status = status,
            OperStatus = operStatus,
            ComplianceStatus = complianceStatus,
            Currency = currency,
            Amount = amount,
            Payload = $"[simulator] {type} {clientOrder} → {status}/{operStatus}/{complianceStatus}"
        });

        if (!_mode.Simulation.DeliverWebhooks || string.IsNullOrWhiteSpace(options.PostbackBaseUrl))
            return;

        // Best-effort POST to the local /webhooks/cj/{endpoint} so we exercise the production path.
        var notification = new
        {
            clientOrder,
            orderReference,
            operTimestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            currency,
            amount,
            status,
            transactionType = endpoint == "payout" ? "Payout" : "Payin",
            subStatuses = new { operStatus, complianceStatus },
            messageUuid = Guid.NewGuid().ToString(),
            type
        };

        var url = options.PostbackBaseUrl.TrimEnd('/') + $"/webhooks/cj/{endpoint}";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(notification), Encoding.UTF8, "application/json")
            };
            using var resp = await _http.SendAsync(req, ct);
            _logger.LogInformation("[CJ-LIFECYCLE] webhook {Type} -> {Url} {Status}", type, url, (int)resp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[CJ-LIFECYCLE] webhook self-post failed for {Url}", url);
        }
    }
}
