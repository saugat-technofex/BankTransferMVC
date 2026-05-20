using System.Text.Json;
using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Controllers;

[ApiController]
[Route("webhooks/cj")]
public class WebhooksController : ControllerBase
{
    private readonly IClearJunctionSignatureService _signer;
    private readonly ITransferStore _store;
    private readonly ClearJunctionOptions _options;
    private readonly ICjModeService _mode;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IClearJunctionSignatureService signer,
        ITransferStore store,
        IOptions<ClearJunctionOptions> options,
        ICjModeService mode,
        ILogger<WebhooksController> logger)
    {
        _signer = signer;
        _store = store;
        _options = options.Value;
        _mode = mode;
        _logger = logger;
    }

    [HttpPost("payin")]
    public async Task<IActionResult> Payin()
    {
        var body = await ReadBodyAsync();
        if (!VerifySignature(body)) return Unauthorized();

        var note = JsonSerializer.Deserialize<PayinNotification>(body);
        if (note is null) return BadRequest();

        _store.AddEvent(new WebhookEvent
        {
            Type = note.Type,
            ClientOrder = note.ClientOrder,
            OrderReference = note.OrderReference,
            Status = note.Status,
            OperStatus = note.SubStatuses.OperStatus,
            ComplianceStatus = note.SubStatuses.ComplianceStatus,
            Currency = note.Currency,
            Amount = note.Amount,
            Payload = body
        });

        // Card invoice notifications use the payin endpoint and should also update the
        // local CardPayin record so its status reflects the gateway's view.
        var card = _store.GetCardPayin(note.ClientOrder);
        if (card is not null)
        {
            _store.UpdateCardPayinStatus(
                note.ClientOrder, note.Status,
                note.SubStatuses.OperStatus, note.SubStatuses.ComplianceStatus);
        }

        _logger.LogInformation("[CJ-WEBHOOK] payin {ClientOrder} {Status}", note.ClientOrder, note.Status);
        return Content(note.OrderReference, "text/plain");
    }

    [HttpPost("payout")]
    public async Task<IActionResult> Payout()
    {
        var body = await ReadBodyAsync();
        if (!VerifySignature(body)) return Unauthorized();

        var note = JsonSerializer.Deserialize<PayoutNotification>(body);
        if (note is null) return BadRequest();

        _store.AddEvent(new WebhookEvent
        {
            Type = note.Type,
            ClientOrder = note.ClientOrder,
            OrderReference = note.OrderReference,
            Status = note.Status,
            OperStatus = note.SubStatuses.OperStatus,
            ComplianceStatus = note.SubStatuses.ComplianceStatus,
            Currency = note.Currency,
            Amount = note.Amount,
            Payload = body
        });

        _store.UpdatePayoutStatus(
            note.ClientOrder,
            note.Status,
            note.SubStatuses.OperStatus,
            note.SubStatuses.ComplianceStatus);

        _logger.LogInformation("[CJ-WEBHOOK] payout {ClientOrder} {Status}", note.ClientOrder, note.Status);
        return Content(note.OrderReference, "text/plain");
    }

    [HttpPost("iban")]
    public async Task<IActionResult> IbanAllocation()
    {
        var body = await ReadBodyAsync();
        if (!VerifySignature(body)) return Unauthorized();
        _logger.LogInformation("[CJ-WEBHOOK] iban: {Body}", body);
        // Body is logged only; persistence not required for the PoC UI.
        return Content("ok", "text/plain");
    }

    private async Task<string> ReadBodyAsync()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        return body;
    }

    private bool VerifySignature(string body)
    {
        // In sandbox / simulation mode skip signature verification so the
        // simulator can post unsigned payloads. In production this must be
        // enforced unconditionally.
        if (_mode.IsSimulation) return true;

        var date = Request.Headers["Date"].ToString();
        var apiKey = Request.Headers["X-API-KEY"].ToString();
        var authorization = Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(authorization))
            return false;

        return _signer.Verify(apiKey, date, body, authorization);
    }
}
