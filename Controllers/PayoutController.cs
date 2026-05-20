using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Controllers;

public class PayoutController : Controller
{
    private readonly IClearJunctionClient _cj;
    private readonly ITransferStore _store;
    private readonly ClearJunctionOptions _options;
    private readonly IClientCustomerIdGenerator _idGen;
    private readonly ICjModeService _mode;
    private readonly ICjSimulator _sim;

    public PayoutController(
        IClearJunctionClient cj,
        ITransferStore store,
        IOptions<ClearJunctionOptions> options,
        IClientCustomerIdGenerator idGen,
        ICjModeService mode,
        ICjSimulator sim)
    {
        _cj = cj;
        _store = store;
        _options = options.Value;
        _idGen = idGen;
        _mode = mode;
        _sim = sim;
    }

    public IActionResult Index() => View(_store.ListPayouts());

    public IActionResult Create(bool regenerate = false)
    {
        var existing = _store.ListWallets().FirstOrDefault();
        var payerId = existing?.ClientCustomerId ?? _idGen.Next();
        ViewBag.ExistingCustomerIds = _store.ListWallets()
            .Select(w => w.ClientCustomerId).Distinct().ToList();

        return View(new CreatePayoutViewModel
        {
            PayerClientCustomerId = payerId,
            Payer = new CjPartyViewModel
            {
                EntityType = "corporate",
                CorporateName = "BankTransferMVC PoC Ltd",
                RegistrationNumber = "PoC-001",
                IncorporationCountry = "GB",
                Email = "ops@example.com",
                Country = "GB",
                City = "London",
                Street = "1 PoC Street",
                Zip = "EC1A1AA"
            },
            Payee = new CjPartyViewModel
            {
                EntityType = "individual",
                Country = "DE",
                FirstName = "Julie",
                LastName = "Peterson",
                Email = "julie@example.com",
                Street = "12 Tourin",
                City = "Rome",
                Zip = "123455"
            }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePayoutViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.PayerClientCustomerId))
        {
            form.PayerClientCustomerId = _idGen.Next();
            ModelState.Remove(nameof(form.PayerClientCustomerId));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ExistingCustomerIds = _store.ListWallets()
                .Select(w => w.ClientCustomerId).Distinct().ToList();
            return View(form);
        }

        var clientOrder = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        var request = new PayoutRequest
        {
            ClientOrder = clientOrder,
            Currency = form.Currency,
            Amount = form.Amount,
            Description = form.Description,
            PostbackUrl = form.PostbackUrl ?? BuildPostbackUrl("/webhooks/cj/payout"),
            PaymentPurposeCodes = new CjPaymentPurpose
            {
                Code = form.PurposeCode ?? "INTP",
                Category = form.PurposeCategory ?? "GP2P"
            },
            Payer = MapEntity(form.Payer, form.PayerClientCustomerId, form.PayerWalletUuid),
            PayerRequisite = new CjRequisite
            {
                Iban = string.IsNullOrWhiteSpace(form.PayerIban) ? null : form.PayerIban
            },
            Payee = MapEntity(form.Payee, null, null),
            PayeeRequisite = BuildPayeeRequisite(form)
        };

        var resp = await _cj.CreatePayoutAsync(form.Rail, request);

        _store.AddPayout(new PayoutRecord
        {
            ClientOrder = clientOrder,
            OrderReference = resp.OrderReference,
            Rail = form.Rail,
            Currency = form.Currency,
            Amount = form.Amount,
            PayeeName = form.Payee.DisplayName(),
            PayeeAccount = form.PayeeIban ?? form.PayeeAccountNumber ?? "",
            PayeeCountry = form.Payee.Country ?? "",
            Description = form.Description,
            Status = resp.Status,
            OperStatus = resp.SubStatuses.OperStatus,
            ComplianceStatus = resp.SubStatuses.ComplianceStatus
        });

        TempData["Success"] = $"Payout {clientOrder} submitted ({form.Rail.ToUpperInvariant()})";
        return RedirectToAction(nameof(Details), new { clientOrder });
    }

    public IActionResult Details(string clientOrder)
    {
        var record = _store.GetPayout(clientOrder);
        if (record is null) return NotFound();

        var events = _store.ListEvents()
            .Where(e => e.ClientOrder == clientOrder)
            .ToList();

        ViewBag.Events = events;
        return View(record);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh(string clientOrder)
    {
        if (_mode.IsLive)
        {
            var status = await _cj.GetPayoutStatusAsync(clientOrder);
            if (status is not null)
            {
                _store.UpdatePayoutStatus(
                    clientOrder,
                    status.Status,
                    status.SubStatuses.OperStatus,
                    status.SubStatuses.ComplianceStatus);
            }
        }
        else
        {
            var rec = _store.GetPayout(clientOrder);
            if (rec is not null)
            {
                var next = _sim.AdvanceLifecycle(rec.Status, rec.OperStatus, rec.ComplianceStatus, _mode.Simulation.Scenario);
                if (next is not null)
                {
                    _store.UpdatePayoutStatus(clientOrder, next.Value.Status, next.Value.OperStatus, next.Value.ComplianceStatus);
                }
            }
        }

        return RedirectToAction(nameof(Details), new { clientOrder });
    }

    private static CjEntity MapEntity(CjPartyViewModel party, string? clientCustomerId, string? walletUuid)
    {
        var entity = new CjEntity
        {
            ClientCustomerId = string.IsNullOrWhiteSpace(clientCustomerId) ? null : clientCustomerId,
            WalletUuid = string.IsNullOrWhiteSpace(walletUuid) ? null : walletUuid
        };

        var address = new CjAddress
        {
            Country = party.Country ?? "",
            City = party.City ?? "",
            Street = party.Street ?? "",
            Zip = party.Zip ?? ""
        };

        if (string.Equals(party.EntityType, "corporate", StringComparison.OrdinalIgnoreCase))
        {
            entity.Corporate = new CjCorporate
            {
                Name = party.CorporateName ?? "",
                Email = party.Email ?? "",
                RegistrationNumber = party.RegistrationNumber ?? "",
                IncorporationCountry = party.IncorporationCountry ?? party.Country ?? "",
                Address = address
            };
        }
        else
        {
            entity.Individual = new CjIndividual
            {
                FirstName = party.FirstName ?? "",
                LastName = party.LastName ?? "",
                Phone = party.Phone ?? "",
                Email = party.Email ?? "",
                BirthDate = (party.BirthDate ?? new DateTime(1990, 1, 1)).ToString("yyyy-MM-dd"),
                BirthPlace = party.BirthPlace,
                Address = address
            };
        }
        return entity;
    }

    private static CjRequisite BuildPayeeRequisite(CreatePayoutViewModel form)
    {
        var req = new CjRequisite
        {
            Iban = string.IsNullOrWhiteSpace(form.PayeeIban) ? null : form.PayeeIban,
            Name = form.Payee.DisplayName()
        };

        if (form.Rail is "fps" or "chaps" or "chapsCrossScheme")
        {
            req.AccountNumber = form.PayeeAccountNumber;
            req.SortCode = form.PayeeSortCode;
        }

        if (form.Rail == "swift")
        {
            req.BankSwiftCode = form.Institution.BankSwiftCode;
            req.Institution = new CjInstitution
            {
                BankSwiftCode = form.Institution.BankSwiftCode ?? "",
                Name = form.Institution.BankName ?? "",
                ClearingSystemIdCode = form.Institution.ClearingSystemIdCode,
                MemberId = form.Institution.MemberId,
                Address = new CjAddress
                {
                    Country = form.Institution.Country ?? "",
                    City = form.Institution.City ?? "",
                    Street = form.Institution.Street ?? "",
                    Zip = form.Institution.Zip ?? ""
                }
            };
            if (!string.IsNullOrWhiteSpace(form.IntermediaryInstitution.BankSwiftCode))
            {
                req.IntermediaryInstitution = new CjInstitution
                {
                    BankSwiftCode = form.IntermediaryInstitution.BankSwiftCode,
                    Name = form.IntermediaryInstitution.BankName ?? ""
                };
            }
        }

        return req;
    }

    private string? BuildPostbackUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(_options.PostbackBaseUrl)) return null;
        return _options.PostbackBaseUrl.TrimEnd('/') + path;
    }
}
