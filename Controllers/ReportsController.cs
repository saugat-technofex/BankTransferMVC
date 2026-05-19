using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class ReportsController : Controller
{
    private readonly ITransferStore _store;

    public ReportsController(ITransferStore store) => _store = store;

    public IActionResult Index() => View(new TransactionReportViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Index(TransactionReportViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        var from = new DateTimeOffset(form.From, TimeSpan.Zero);
        var to = new DateTimeOffset(form.To.AddDays(1), TimeSpan.Zero);

        var lines = _store.ListEvents()
            .Where(e => e.ReceivedAt >= from && e.ReceivedAt < to)
            .Where(e => form.Type == "all" || e.Type.Contains(form.Type, StringComparison.OrdinalIgnoreCase))
            .Where(e => string.IsNullOrWhiteSpace(form.WalletUuid)
                || e.Payload.Contains(form.WalletUuid, StringComparison.OrdinalIgnoreCase))
            .ToList();

        ViewBag.Form = form;
        return View("Results", lines);
    }

    public IActionResult Events() => View(_store.ListEvents());
}
