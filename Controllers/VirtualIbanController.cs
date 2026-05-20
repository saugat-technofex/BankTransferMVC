using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Controllers;

public class VirtualIbanController : Controller
{
    private readonly IClearJunctionClient _cj;
    private readonly ITransferStore _store;
    private readonly ClearJunctionOptions _options;
    private readonly IClientCustomerIdGenerator _idGen;

    public VirtualIbanController(
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

    public IActionResult Index() => View(_store.ListIbans());

    public IActionResult Create(bool regenerate = false)
    {
        ViewBag.ExistingCustomerIds = _store.ListWallets()
            .Select(w => w.ClientCustomerId).Distinct().ToList();

        return View(new CreateIbanViewModel
        {
            ClientCustomerId = _idGen.Next(),
            Registrant = new CjPartyViewModel
            {
                EntityType = "individual",
                FirstName = "Julie",
                LastName = "Peterson",
                BirthDate = new DateTime(1990, 1, 1),
                Email = "julie@example.com",
                Country = "GB",
                Street = "12 Tourin",
                City = "London",
                Zip = "EC1A1AA"
            }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateIbanViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.ClientCustomerId))
        {
            form.ClientCustomerId = _idGen.Next();
            ModelState.Remove(nameof(form.ClientCustomerId));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ExistingCustomerIds = _store.ListWallets()
                .Select(w => w.ClientCustomerId).Distinct().ToList();
            return View(form);
        }

        var clientOrder = $"IBAN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var r = form.Registrant;

        var address = new CjAddress
        {
            Country = r.Country ?? "",
            City = r.City ?? "",
            Street = r.Street ?? "",
            Zip = r.Zip ?? ""
        };

        var registrantEntity = new CjEntity
        {
            ClientCustomerId = form.ClientCustomerId
        };
        if (string.Equals(r.EntityType, "corporate", StringComparison.OrdinalIgnoreCase))
        {
            registrantEntity.Corporate = new CjCorporate
            {
                Name = r.CorporateName ?? "",
                Email = r.Email ?? "",
                RegistrationNumber = r.RegistrationNumber ?? "",
                IncorporationCountry = r.IncorporationCountry ?? r.Country ?? "",
                Address = address
            };
        }
        else
        {
            registrantEntity.Individual = new CjIndividual
            {
                FirstName = r.FirstName ?? "",
                LastName = r.LastName ?? "",
                Email = r.Email ?? "",
                BirthDate = (r.BirthDate ?? new DateTime(1990, 1, 1)).ToString("yyyy-MM-dd"),
                Address = address
            };
        }

        var request = new AllocateIbanRequest
        {
            ClientOrder = clientOrder,
            PostbackUrl = BuildPostbackUrl("/webhooks/cj/iban"),
            WalletUuid = Guid.NewGuid().ToString(),
            IbanCountry = form.IbanCountry,
            IbansGroup = form.IbansGroup,
            Registrant = registrantEntity
        };

        var resp = await _cj.AllocateIbanAsync(request);

        _store.AddIban(new VirtualIbanRecord
        {
            ClientOrder = clientOrder,
            OrderReference = resp.OrderReference,
            Iban = resp.Iban ?? "",
            Country = form.IbanCountry,
            WalletUuid = request.WalletUuid ?? "",
            CustomerName = r.DisplayName(),
            ClientCustomerId = form.ClientCustomerId,
            Status = resp.Status
        });

        TempData["Success"] = $"Virtual IBAN allocated: {resp.Iban}";
        return RedirectToAction(nameof(Details), new { clientOrder });
    }

    public IActionResult Details(string clientOrder)
    {
        var record = _store.GetIban(clientOrder);
        if (record is null) return NotFound();
        return View(record);
    }

    private string? BuildPostbackUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(_options.PostbackBaseUrl)) return null;
        return _options.PostbackBaseUrl.TrimEnd('/') + path;
    }
}
