using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class RefundController : Controller
{
    private readonly ITransferStore _store;
    private readonly IClearJunctionClient _cj;

    public RefundController(ITransferStore store, IClearJunctionClient cj)
    {
        _store = store;
        _cj = cj;
    }

    public IActionResult Index() => View(_store.ListRefunds());

    public IActionResult Create()
    {
        ViewBag.Payouts = _store.ListPayouts();
        ViewBag.Payins = _store.ListEvents()
            .Where(e => e.Type == "payinNotification" || e.Type == "simulated-payin")
            .ToList();
        return View(new CreateRefundViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRefundViewModel form)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Payouts = _store.ListPayouts();
            ViewBag.Payins = _store.ListEvents()
                .Where(e => e.Type == "payinNotification" || e.Type == "simulated-payin")
                .ToList();
            return View(form);
        }

        var clientOrder = $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        var resp = await _cj.CreateRefundAsync(new RefundRequest
        {
            ClientOrder = clientOrder,
            RelatedOrderReference = form.OriginalClientOrder,
            Currency = form.Currency,
            Amount = form.Amount,
            Description = form.Reason
        });

        var rec = new RefundRecord
        {
            ClientOrder = clientOrder,
            OrderReference = resp.OrderReference,
            Direction = form.Direction,
            OriginalClientOrder = form.OriginalClientOrder,
            Currency = form.Currency,
            Amount = form.Amount,
            Reason = form.Reason,
            Status = resp.Status,
            OperStatus = resp.SubStatuses.OperStatus,
            ComplianceStatus = resp.SubStatuses.ComplianceStatus
        };
        _store.AddRefund(rec);

        _store.AddEvent(new WebhookEvent
        {
            Type = $"refund.{form.Direction}",
            ClientOrder = clientOrder,
            OrderReference = resp.OrderReference,
            Status = resp.Status,
            OperStatus = resp.SubStatuses.OperStatus,
            ComplianceStatus = resp.SubStatuses.ComplianceStatus,
            Currency = form.Currency,
            Amount = form.Amount,
            Payload = $"Refund of {form.OriginalClientOrder} — reason: {form.Reason}"
        });

        TempData["Success"] = $"Refund {clientOrder} submitted ({form.Direction}) — POST /v7/gate/refund";
        return RedirectToAction(nameof(Index));
    }
}
