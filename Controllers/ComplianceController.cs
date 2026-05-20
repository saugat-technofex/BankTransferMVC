using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class ComplianceController : Controller
{
    private readonly ITransferStore _store;
    private readonly IClearJunctionClient _cj;

    public ComplianceController(ITransferStore store, IClearJunctionClient cj)
    {
        _store = store;
        _cj = cj;
    }

    public IActionResult Index() => View(new RequisiteCheckViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Check(RequisiteCheckViewModel form)
    {
        if (!ModelState.IsValid) return View(nameof(Index), form);

        string input;
        string result;
        string detail;
        switch (form.Kind)
        {
            case "cop":
                input = $"{form.Name} · {form.SortCode}-{form.AccountNumber}";
                var cop = await _cj.CheckCopAsync(new CopCheckRequest
                {
                    Name = form.Name ?? "",
                    SortCode = form.SortCode ?? "",
                    AccountNumber = form.AccountNumber ?? "",
                    AccountType = "personal"
                });
                result = cop.Result;
                detail = $"CoP result: {cop.Result}" +
                         (cop.MatchedName is null ? "" : $" (matched: {cop.MatchedName})") +
                         " · POST /v7/gate/checkRequisite/cop";
                break;

            case "iban":
                input = form.Iban ?? "";
                var ib = await _cj.CheckIbanAsync(form.Iban ?? "");
                result = ib.Result;
                detail = $"IBAN result: {ib.Result}" +
                         (ib.BankName is null ? "" : $" · {ib.BankName} ({ib.Bic})") +
                         " · GET /v7/gate/checkRequisite/bankTransfer/eu/iban/{iban}";
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
    public async Task<IActionResult> Approve(string clientOrder)
    {
        var rec = _store.GetPayout(clientOrder);
        if (rec is null) return NotFound();
        var resp = await _cj.TransactionActionAsync("approve", new TransactionActionRequest { ClientOrder = clientOrder });
        _store.UpdatePayoutStatus(clientOrder, resp.Status, resp.SubStatuses.OperStatus, resp.SubStatuses.ComplianceStatus);
        _store.AddEvent(new WebhookEvent
        {
            Type = "transactionAction.approve",
            ClientOrder = clientOrder,
            OrderReference = rec.OrderReference,
            Status = resp.Status,
            OperStatus = resp.SubStatuses.OperStatus,
            ComplianceStatus = resp.SubStatuses.ComplianceStatus,
            Currency = rec.Currency,
            Amount = rec.Amount,
            Payload = "POST /v7/gate/transactionAction/approve"
        });
        TempData["Success"] = $"Payout {clientOrder} approved (POST /v7/gate/transactionAction/approve)";
        return RedirectToAction(nameof(Approvals));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string clientOrder)
    {
        var rec = _store.GetPayout(clientOrder);
        if (rec is null) return NotFound();
        var resp = await _cj.TransactionActionAsync("cancel", new TransactionActionRequest { ClientOrder = clientOrder });
        _store.UpdatePayoutStatus(clientOrder, resp.Status, resp.SubStatuses.OperStatus, resp.SubStatuses.ComplianceStatus);
        _store.AddEvent(new WebhookEvent
        {
            Type = "transactionAction.cancel",
            ClientOrder = clientOrder,
            OrderReference = rec.OrderReference,
            Status = resp.Status,
            OperStatus = resp.SubStatuses.OperStatus,
            ComplianceStatus = resp.SubStatuses.ComplianceStatus,
            Currency = rec.Currency,
            Amount = rec.Amount,
            Payload = "POST /v7/gate/transactionAction/cancel"
        });
        TempData["Success"] = $"Payout {clientOrder} cancelled (POST /v7/gate/transactionAction/cancel)";
        return RedirectToAction(nameof(Approvals));
    }
}
