using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class ReportsController : Controller
{
    private readonly ITransferStore _store;
    private readonly IClearJunctionClient _cj;
    private readonly ICjModeService _mode;

    public ReportsController(ITransferStore store, IClearJunctionClient cj, ICjModeService mode)
    {
        _store = store;
        _cj = cj;
        _mode = mode;
    }

    public IActionResult Index()
    {
        ViewBag.Wallets = _store.ListWallets();
        return View(new TransactionReportViewModel
        {
            WalletUuid = _store.ListWallets().FirstOrDefault()?.WalletUuid ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TransactionReportViewModel form)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Wallets = _store.ListWallets();
            return View(form);
        }

        TransactionReportResponse report;
        try
        {
            report = await _cj.TransactionReportAsync(new TransactionReportRequest
            {
                WalletUuid = form.WalletUuid ?? "",
                DateFrom = form.From.ToString("yyyy-MM-dd"),
                DateTo = form.To.ToString("yyyy-MM-dd")
            });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Transaction report failed: {ex.Message}";
            ViewBag.Wallets = _store.ListWallets();
            return View(form);
        }

        // Apply UI-side filters on top of the CJ response so a user can narrow by type and date.
        var from = new DateTimeOffset(form.From, TimeSpan.Zero);
        var to = new DateTimeOffset(form.To.AddDays(1), TimeSpan.Zero);

        var rows = report.Rows
            .Where(r =>
            {
                if (string.IsNullOrWhiteSpace(r.CreatedAt)) return true;
                return DateTimeOffset.TryParse(r.CreatedAt, out var dt)
                    ? dt >= from && dt < to
                    : true;
            })
            .Where(r => form.Type == "all" || r.TransactionType.Contains(form.Type, StringComparison.OrdinalIgnoreCase))
            .ToList();

        ViewBag.Form = form;
        ViewBag.Mode = _mode.Mode();
        return View("Results", rows);
    }

    public IActionResult Events() => View(_store.ListEvents());
}
