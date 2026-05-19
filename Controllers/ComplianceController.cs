using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class ComplianceController : Controller
{
    private readonly ITransferStore _store;

    public ComplianceController(ITransferStore store) => _store = store;

    public IActionResult Index() => View(new RequisiteCheckViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Check(RequisiteCheckViewModel form)
    {
        string result;
        string detail;
        string input;
        switch (form.Kind)
        {
            case "cop":
                input = $"{form.Name} · {form.SortCode}-{form.AccountNumber}";
                if (string.IsNullOrWhiteSpace(form.Name) || string.IsNullOrWhiteSpace(form.SortCode)
                    || string.IsNullOrWhiteSpace(form.AccountNumber))
                {
                    result = "invalid";
                    detail = "Name, sort code and account number are required for Confirmation of Payee.";
                }
                else
                {
                    result = "match";
                    detail = $"Confirmation of Payee: name matches the account holder for {form.SortCode}-{form.AccountNumber}.";
                }
                break;

            case "iban":
                input = form.Iban ?? "";
                if (string.IsNullOrWhiteSpace(form.Iban) || form.Iban.Length < 15)
                {
                    result = "invalid";
                    detail = "IBAN is invalid or too short.";
                }
                else
                {
                    result = "reachable";
                    detail = $"SEPA reachability confirmed for {form.Iban} (GET /v7/gate/checkRequisite/bankTransfer/eu/iban/{{iban}}).";
                }
                break;

            default:
                input = "(unknown)";
                result = "unknown";
                detail = "Unsupported check kind.";
                break;
        }

        _store.AddRequisiteCheck(new RequisiteCheckRecord
        {
            Kind = form.Kind,
            Input = input,
            Result = result,
            Detail = detail
        });

        TempData["Success"] = $"{form.Kind.ToUpperInvariant()} check completed: {result}";
        return RedirectToAction(nameof(History));
    }

    public IActionResult History() => View(_store.ListRequisiteChecks());

    public IActionResult Approvals()
    {
        var pending = _store.ListPayouts()
            .Where(p => p.ComplianceStatus == "pending" || p.OperStatus == "pending")
            .ToList();
        return View(pending);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Approve(string clientOrder)
    {
        var rec = _store.GetPayout(clientOrder);
        if (rec is null) return NotFound();
        _store.UpdatePayoutStatus(clientOrder, "completed", "settled", "approved");
        _store.AddEvent(new WebhookEvent
        {
            Type = "transactionAction.approve",
            ClientOrder = clientOrder,
            OrderReference = rec.OrderReference,
            Status = "completed",
            OperStatus = "settled",
            ComplianceStatus = "approved",
            Currency = rec.Currency,
            Amount = rec.Amount,
            Payload = "POST /v7/gate/transactionAction/approve"
        });
        TempData["Success"] = $"Payout {clientOrder} approved (POST /v7/gate/transactionAction/approve)";
        return RedirectToAction(nameof(Approvals));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancel(string clientOrder)
    {
        var rec = _store.GetPayout(clientOrder);
        if (rec is null) return NotFound();
        _store.UpdatePayoutStatus(clientOrder, "failed", "declined", "declined");
        _store.AddEvent(new WebhookEvent
        {
            Type = "transactionAction.cancel",
            ClientOrder = clientOrder,
            OrderReference = rec.OrderReference,
            Status = "failed",
            OperStatus = "declined",
            ComplianceStatus = "declined",
            Currency = rec.Currency,
            Amount = rec.Amount,
            Payload = "POST /v7/gate/transactionAction/cancel"
        });
        TempData["Success"] = $"Payout {clientOrder} cancelled (POST /v7/gate/transactionAction/cancel)";
        return RedirectToAction(nameof(Approvals));
    }
}
