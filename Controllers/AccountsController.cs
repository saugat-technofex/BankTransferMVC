using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Controllers;

public class AccountsController : Controller
{
    private readonly ITransferStore _store;
    private readonly IClientCustomerIdGenerator _idGen;
    private readonly IClearJunctionClient _cj;
    private readonly ClearJunctionOptions _options;
    private readonly ICjModeService _mode;

    public AccountsController(
        ITransferStore store,
        IClientCustomerIdGenerator idGen,
        IClearJunctionClient cj,
        IOptions<ClearJunctionOptions> options,
        ICjModeService mode)
    {
        _store = store;
        _idGen = idGen;
        _cj = cj;
        _options = options.Value;
        _mode = mode;
    }

    public IActionResult Index() => View(_store.ListWallets());

    public IActionResult Create(bool regenerate = false) =>
        View(new CreateWalletViewModel
        {
            ClientCustomerId = _idGen.Next(),
            Holder = new CjPartyViewModel
            {
                EntityType = "corporate",
                IncorporationCountry = "GB",
                Country = "GB"
            }
        });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWalletViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.ClientCustomerId))
        {
            form.ClientCustomerId = _idGen.Next();
            ModelState.Remove(nameof(form.ClientCustomerId));
        }
        if (!ModelState.IsValid) return View(form);

        var clientOrder = $"WAL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        var req = new CreateWalletRequest
        {
            ClientOrder = clientOrder,
            PostbackUrl = BuildPostbackUrl("/webhooks/cj/iban"),
            Name = form.Name,
            ClientCustomerId = form.ClientCustomerId,
            Currency = form.Currency,
            Type = form.Type,
            IbanCountry = form.IbanCountry,
            IbansGroup = form.IbansGroup,
            Holder = MapHolderEntity(form)
        };

        CreateWalletResponse resp;
        try
        {
            resp = await _cj.CreateWalletAsync(req);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Wallet creation failed: {ex.Message}";
            return View(form);
        }

        var wallet = new WalletRecord
        {
            WalletUuid = resp.WalletUuid,
            Name = form.Name,
            ClientCustomerId = form.ClientCustomerId,
            Email = form.Email,
            Country = form.Country,
            Type = form.Type,
            Currency = form.Currency,
            Balance = form.OpeningBalance,
            Status = resp.Status
        };
        _store.AddWallet(wallet);

        _store.AddEvent(new WebhookEvent
        {
            Type = "walletReservationNotification",
            ClientOrder = clientOrder,
            OrderReference = resp.OrderReference,
            Status = resp.Status,
            OperStatus = "settled",
            ComplianceStatus = "approved",
            Currency = form.Currency,
            Payload = $"POST /v7/gate/wallets/corporate — {form.Holder.EntityType}={form.Holder.DisplayName()} LEI={form.Holder.Lei ?? "-"} UBOs={form.Ubos.Count} mode={(_mode.IsSimulation ? "sim" : "live")}"
        });

        TempData["Success"] = $"Wallet {wallet.WalletUuid} created (POST /v7/gate/wallets/corporate · {_mode.Mode()})";
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
    public async Task<IActionResult> Transfer(WalletTransferViewModel form)
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

        var clientOrder = $"WAL-XFER-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var req = new WalletTransferRequest
        {
            ClientOrder = clientOrder,
            FromWalletUuid = form.FromWalletUuid,
            ToWalletUuid = form.ToWalletUuid,
            Currency = form.Currency,
            Amount = form.Amount,
            Description = form.Reference
        };

        WalletTransferResponse resp;
        try
        {
            resp = await _cj.WalletTransferAsync(req);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Wallet transfer failed: {ex.Message}";
            ViewBag.Wallets = _store.ListWallets();
            return View(form);
        }

        if (resp.Status == "failed")
        {
            TempData["Error"] = $"Wallet transfer declined by CJ ({_mode.Mode()}).";
            return RedirectToAction(nameof(Transfer));
        }

        // In simulation mode, the simulator already adjusts balances on _store; in live mode CJ is the
        // source of truth and the next GET wallet call would refresh — for the PoC ledger we mirror it here.
        if (!_mode.IsSimulation)
        {
            from.Balance -= form.Amount;
            to.Balance += form.Amount;
        }

        _store.AddEvent(new WebhookEvent
        {
            Type = "walletTransfer",
            ClientOrder = clientOrder,
            OrderReference = resp.OrderReference,
            Status = resp.Status,
            OperStatus = "settled",
            ComplianceStatus = "approved",
            Currency = form.Currency,
            Amount = form.Amount,
            Payload = $"{from.WalletUuid} -> {to.WalletUuid} : {form.Amount} {form.Currency} ({form.Reference}) [{_mode.Mode()}]"
        });

        TempData["Success"] = $"Transferred {form.Amount} {form.Currency} from {from.Name} to {to.Name} (POST /v7/gate/wallets/transfer · {_mode.Mode()})";
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

    private static CjEntity MapHolderEntity(CreateWalletViewModel form)
    {
        var h = form.Holder;
        var address = new CjAddress
        {
            Country = h.Country ?? form.Country,
            City = h.City ?? "",
            Street = h.Street ?? "",
            Zip = h.Zip ?? ""
        };

        var entity = new CjEntity { ClientCustomerId = form.ClientCustomerId };
        if (string.Equals(h.EntityType, "corporate", StringComparison.OrdinalIgnoreCase))
        {
            entity.Corporate = new CjCorporate
            {
                Name = h.CorporateName ?? form.Name,
                Email = h.Email ?? form.Email,
                RegistrationNumber = h.RegistrationNumber ?? "",
                IncorporationCountry = h.IncorporationCountry ?? form.Country,
                IncorporationDate = h.IncorporationDate?.ToString("yyyy-MM-dd"),
                LegalEntityIdentifier = h.Lei,
                Address = address
            };
        }
        else
        {
            entity.Individual = new CjIndividual
            {
                FirstName = h.FirstName ?? "",
                LastName = h.LastName ?? "",
                Phone = h.Phone ?? "",
                Email = h.Email ?? form.Email,
                BirthDate = (h.BirthDate ?? new DateTime(1990, 1, 1)).ToString("yyyy-MM-dd"),
                BirthPlace = h.BirthPlace,
                Address = address
            };
        }
        return entity;
    }

    private string? BuildPostbackUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(_options.PostbackBaseUrl)) return null;
        return _options.PostbackBaseUrl.TrimEnd('/') + path;
    }
}
