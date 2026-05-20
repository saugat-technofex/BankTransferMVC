using System.Text.Json;
using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class PayinController : Controller
{
    private readonly ITransferStore _store;
    private readonly IClearJunctionClient _cj;

    public PayinController(ITransferStore store, IClearJunctionClient cj)
    {
        _store = store;
        _cj = cj;
    }

    public IActionResult Index()
    {
        var payins = _store.ListEvents()
            .Where(e => e.Type == "payinNotification" || e.Type == "simulated-payin")
            .ToList();
        return View(payins);
    }

    public IActionResult Card() => View(new CardPayinViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Card(CardPayinViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        var clientOrder = $"CARD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        var resp = await _cj.CreateCardPayinAsync(new CardPayinRequest
        {
            ClientOrder = clientOrder,
            Currency = form.Currency,
            Amount = form.Amount,
            ProductName = form.ProductName,
            SiteAddress = form.SiteAddress,
            SuccessUrl = form.SuccessUrl ?? "",
            FailUrl = form.FailUrl ?? "",
            Payer = new CjEntity
            {
                Individual = new CjIndividual
                {
                    FirstName = form.PayerName.Split(' ').FirstOrDefault() ?? "",
                    LastName = form.PayerName.Contains(' ') ? form.PayerName.Split(' ', 2)[1] : "",
                    Email = form.PayerEmail ?? "",
                    BirthDate = "1990-01-01",
                    Address = new CjAddress { Country = "GB" }
                }
            }
        });

        var rec = new CardPayinRecord
        {
            ClientOrder = clientOrder,
            OrderReference = resp.OrderReference,
            PayerName = form.PayerName,
            PayerEmail = form.PayerEmail ?? "",
            ProductName = form.ProductName,
            SiteAddress = form.SiteAddress,
            Currency = form.Currency,
            Amount = form.Amount,
            Status = resp.Status,
            OperStatus = resp.SubStatuses.OperStatus,
            ComplianceStatus = resp.SubStatuses.ComplianceStatus,
            RedirectUrl = resp.RedirectUrl
        };
        _store.AddCardPayin(rec);

        _store.AddEvent(new WebhookEvent
        {
            Type = "invoice.creditCard",
            ClientOrder = clientOrder,
            OrderReference = rec.OrderReference,
            Status = rec.Status,
            OperStatus = rec.OperStatus,
            ComplianceStatus = rec.ComplianceStatus,
            Currency = form.Currency,
            Amount = form.Amount,
            Payload = $"POST /v7/gate/invoice/creditCard — redirect={rec.RedirectUrl}"
        });

        TempData["Success"] = $"Card invoice {clientOrder} created — redirect: {rec.RedirectUrl}";
        return RedirectToAction(nameof(CardList));
    }

    public IActionResult CardList() => View(_store.ListCardPayins());

    public IActionResult Simulate()
    {
        var ibans = _store.ListIbans();
        ViewBag.Ibans = ibans;
        return View(new SimulatePayinViewModel
        {
            ClientOrder = ibans.FirstOrDefault()?.ClientOrder ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Simulate(SimulatePayinViewModel form)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Ibans = _store.ListIbans();
            return View(form);
        }
        var iban = _store.GetIban(form.ClientOrder);
        var orderRef = iban?.OrderReference ?? Guid.NewGuid().ToString();

        var notification = new PayinNotification
        {
            ClientOrder = form.ClientOrder,
            OrderReference = orderRef,
            OperTimestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            Currency = form.Currency,
            Amount = form.Amount,
            OperationCurrency = form.Currency,
            OperationAmount = form.Amount,
            Status = form.Status,
            TransactionType = "Payin",
            SubStatuses = new CjSubStatuses
            {
                OperStatus = form.OperStatus,
                ComplianceStatus = form.ComplianceStatus
            },
            Payer = new CjEntity
            {
                Individual = new CjIndividual
                {
                    FirstName = form.PayerName.Split(' ').FirstOrDefault() ?? "",
                    LastName = form.PayerName.Contains(' ')
                        ? form.PayerName.Split(' ', 2)[1] : "",
                    Email = "payer@example.com",
                    BirthDate = "1990-01-01",
                    Address = new CjAddress { Country = "GB" }
                }
            },
            Payee = new CjEntity
            {
                WalletUuid = iban?.WalletUuid,
                ClientCustomerId = iban?.ClientCustomerId
            },
            MessageUuid = Guid.NewGuid().ToString(),
            Type = "payinNotification"
        };

        var payload = JsonSerializer.Serialize(notification, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        _store.AddEvent(new WebhookEvent
        {
            Type = "simulated-payin",
            ClientOrder = form.ClientOrder,
            OrderReference = orderRef,
            Status = form.Status,
            OperStatus = form.OperStatus,
            ComplianceStatus = form.ComplianceStatus,
            Currency = form.Currency,
            Amount = form.Amount,
            Payload = payload
        });

        TempData["Success"] = $"Simulated pay-in {form.Amount} {form.Currency} credited to {iban?.Iban ?? form.ClientOrder}";
        return RedirectToAction(nameof(Index));
    }
}
