using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class AccountsController : Controller
{
    private readonly ITransferStore _store;

    public AccountsController(ITransferStore store) => _store = store;

    public IActionResult Index() => View(_store.ListWallets());

    public IActionResult Create() => View(new CreateWalletViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(CreateWalletViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        var wallet = new WalletRecord
        {
            WalletUuid = Guid.NewGuid().ToString(),
            Name = form.Name,
            ClientCustomerId = form.ClientCustomerId,
            Email = form.Email,
            Country = form.Country,
            Type = form.Type,
            Currency = form.Currency,
            Balance = form.OpeningBalance,
            Status = "active"
        };
        _store.AddWallet(wallet);
        TempData["Success"] = $"Wallet {wallet.WalletUuid} created (POST /v7/gate/wallets/corporate)";
        return RedirectToAction(nameof(Details), new { walletUuid = wallet.WalletUuid });
    }

    public IActionResult Details(string walletUuid)
    {
        var rec = _store.GetWallet(walletUuid);
        if (rec is null) return NotFound();
        return View(rec);
    }

    public IActionResult Transfer()
    {
        ViewBag.Wallets = _store.ListWallets();
        return View(new WalletTransferViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Transfer(WalletTransferViewModel form)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Wallets = _store.ListWallets();
            return View(form);
        }

        var from = _store.GetWallet(form.FromWalletUuid);
        var to = _store.GetWallet(form.ToWalletUuid);
        if (from is null || to is null)
        {
            TempData["Error"] = "Both wallets must exist.";
            return RedirectToAction(nameof(Transfer));
        }

        from.Balance -= form.Amount;
        to.Balance += form.Amount;

        _store.AddEvent(new WebhookEvent
        {
            Type = "walletTransfer",
            ClientOrder = $"WAL-{DateTime.UtcNow:yyyyMMddHHmmss}",
            OrderReference = Guid.NewGuid().ToString(),
            Status = "completed",
            OperStatus = "settled",
            ComplianceStatus = "approved",
            Currency = form.Currency,
            Amount = form.Amount,
            Payload = $"{from.WalletUuid} → {to.WalletUuid} : {form.Amount} {form.Currency} ({form.Reference})"
        });

        TempData["Success"] = $"Transferred {form.Amount} {form.Currency} from {from.Name} to {to.Name} (POST /v7/gate/wallets/transfer)";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Statement(string walletUuid)
    {
        var rec = _store.GetWallet(walletUuid);
        if (rec is null) return NotFound();

        var lines = _store.ListEvents()
            .Where(e => e.Payload.Contains(walletUuid))
            .ToList();

        ViewBag.Wallet = rec;
        return View(lines);
    }
}
