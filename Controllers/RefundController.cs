using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class RefundController : Controller
{
    private readonly ITransferStore _store;

    public RefundController(ITransferStore store) => _store = store;

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
    public IActionResult Create(CreateRefundViewModel form)
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
        var orderRef = Guid.NewGuid().ToString();

        var rec = new RefundRecord
        {
            ClientOrder = clientOrder,
            OrderReference = orderRef,
            Direction = form.Direction,
            OriginalClientOrder = form.OriginalClientOrder,
            Currency = form.Currency,
            Amount = form.Amount,
            Reason = form.Reason,
            Status = "created",
            OperStatus = "pending",
            ComplianceStatus = "pending"
        };
        _store.AddRefund(rec);

        _store.AddEvent(new WebhookEvent
        {
            Type = $"refund.{form.Direction}",
            ClientOrder = clientOrder,
            OrderReference = orderRef,
            Status = "created",
            OperStatus = "pending",
            ComplianceStatus = "pending",
            Currency = form.Currency,
            Amount = form.Amount,
            Payload = $"Refund of {form.OriginalClientOrder} — reason: {form.Reason}"
        });

        TempData["Success"] = $"Refund {clientOrder} submitted ({form.Direction}) — POST /v7/gate/refund";
        return RedirectToAction(nameof(Index));
    }
}
