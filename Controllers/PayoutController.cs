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

    public PayoutController(
        IClearJunctionClient cj,
        ITransferStore store,
        IOptions<ClearJunctionOptions> options,
        IClientCustomerIdGenerator idGen)
    {
        _cj = cj;
        _store = store;
        _options = options.Value;
        _idGen = idGen;
    }

    public IActionResult Index() => View(_store.ListPayouts());

    public IActionResult Create(bool regenerate = false)
    {
        var existing = _store.ListWallets().FirstOrDefault();
        var payerId = existing?.ClientCustomerId ?? _idGen.Next();
        ViewBag.ExistingCustomerIds = _store.ListWallets()
            .Select(w => w.ClientCustomerId).Distinct().ToList();
        return View(new CreatePayoutViewModel { PayerClientCustomerId = payerId });
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
            PostbackUrl = BuildPostbackUrl("/webhooks/cj/payout"),
            PaymentPurposeCodes = new CjPaymentPurpose
            {
                Code = form.PurposeCode,
                Category = form.PurposeCategory
            },
            Payer = new CjEntity
            {
                ClientCustomerId = string.IsNullOrWhiteSpace(form.PayerClientCustomerId)
                    ? null : form.PayerClientCustomerId,
                WalletUuid = string.IsNullOrWhiteSpace(form.PayerWalletUuid)
                    ? null : form.PayerWalletUuid,
                Corporate = new CjCorporate
                {
                    Name = "BankTransferMVC PoC Ltd",
                    Email = "ops@example.com",
                    RegistrationNumber = "PoC-001",
                    IncorporationCountry = "GB",
                    Address = new CjAddress { Country = "GB", City = "London", Street = "1 PoC Street", Zip = "EC1A1AA" }
                }
            },
            PayerRequisite = new CjRequisite
            {
                Iban = string.IsNullOrWhiteSpace(form.PayerIban) ? null : form.PayerIban
            },
            Payee = new CjEntity
            {
                Individual = new CjIndividual
                {
                    FirstName = SplitFirst(form.PayeeName),
                    LastName = SplitLast(form.PayeeName),
                    Email = form.PayeeEmail,
                    BirthDate = "1990-01-01",
                    Address = new CjAddress { Country = form.PayeeCountry }
                }
            },
            PayeeRequisite = BuildPayeeRequisite(form),
            UltimatePayee = new CjEntity
            {
                Individual = new CjIndividual
                {
                    FirstName = SplitFirst(form.PayeeName),
                    LastName = SplitLast(form.PayeeName),
                    Email = form.PayeeEmail,
                    BirthDate = "1990-01-01",
                    Address = new CjAddress { Country = form.PayeeCountry }
                }
            }
        };

        var resp = await _cj.CreatePayoutAsync(form.Rail, request);

        _store.AddPayout(new PayoutRecord
        {
            ClientOrder = clientOrder,
            OrderReference = resp.OrderReference,
            Rail = form.Rail,
            Currency = form.Currency,
            Amount = form.Amount,
            PayeeName = form.PayeeName,
            PayeeAccount = form.PayeeIban,
            PayeeCountry = form.PayeeCountry,
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
        if (!_options.SimulationMode)
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
            // Simulation: advance the lifecycle each click so the UI is demoable.
            var rec = _store.GetPayout(clientOrder);
            if (rec is not null)
            {
                var next = rec.OperStatus switch
                {
                    "pending" => "processing",
                    "processing" => "settled",
                    _ => rec.OperStatus
                };
                var compl = rec.ComplianceStatus == "pending" ? "approved" : rec.ComplianceStatus;
                var newStatus = next == "settled" ? "completed" : rec.Status;
                _store.UpdatePayoutStatus(clientOrder, newStatus, next, compl);
            }
        }

        return RedirectToAction(nameof(Details), new { clientOrder });
    }

    private static CjRequisite BuildPayeeRequisite(CreatePayoutViewModel form)
    {
        var req = new CjRequisite
        {
            Iban = string.IsNullOrWhiteSpace(form.PayeeIban) ? null : form.PayeeIban,
            BankSwiftCode = string.IsNullOrWhiteSpace(form.PayeeBankSwift) ? null : form.PayeeBankSwift,
            Name = string.IsNullOrWhiteSpace(form.PayeeName) ? null : form.PayeeName
        };

        if (form.Rail == "fps" || form.Rail == "chaps" || form.Rail == "chapsCrossScheme")
        {
            req.AccountNumber = form.PayeeAccountNumber;
            req.SortCode = form.PayeeSortCode;
        }

        if (form.Rail == "swift")
        {
            req.Institution = new CjInstitution
            {
                BankSwiftCode = form.PayeeBankSwift ?? "",
                Name = form.PayeeBankName ?? "",
                ClearingSystemIdCode = form.ClearingSystemIdCode,
                MemberId = form.ClearingMemberId,
                Address = new CjAddress { Country = form.PayeeCountry }
            };

            if (!string.IsNullOrWhiteSpace(form.IntermediaryBankSwift))
            {
                req.IntermediaryInstitution = new CjInstitution
                {
                    BankSwiftCode = form.IntermediaryBankSwift,
                    Name = form.IntermediaryBankName ?? ""
                };
            }
        }

        return req;
    }

    private static string SplitFirst(string fullName) =>
        (fullName ?? "").Split(' ', 2).FirstOrDefault() ?? "";

    private static string SplitLast(string fullName)
    {
        var parts = (fullName ?? "").Split(' ', 2);
        return parts.Length > 1 ? parts[1] : "";
    }

    private string? BuildPostbackUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(_options.PostbackBaseUrl)) return null;
        return _options.PostbackBaseUrl.TrimEnd('/') + path;
    }
}
