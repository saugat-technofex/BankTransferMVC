using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Integrations.ClearJunction.Models;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class FxController : Controller
{
    private readonly IClearJunctionClient _cj;
    private readonly ITransferStore _store;

    public FxController(IClearJunctionClient cj, ITransferStore store)
    {
        _cj = cj;
        _store = store;
    }

    public IActionResult Index() => View(new FxQuoteViewModel());

    public IActionResult History() => View(_store.ListFx());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Quote(FxQuoteViewModel form)
    {
        if (!ModelState.IsValid) return View(nameof(Index), form);

        var resp = await _cj.GetFxRateAsync(new FxRateRequest
        {
            SellCurrency = form.SellCurrency,
            BuyCurrency = form.BuyCurrency
        });

        var quote = resp.Quotes.FirstOrDefault(q =>
            string.Equals(q.SellCurrency, form.SellCurrency, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(q.BuyCurrency, form.BuyCurrency, StringComparison.OrdinalIgnoreCase));

        if (quote is null)
        {
            ModelState.AddModelError("", "No quote returned for the selected currency pair.");
            return View(nameof(Index), form);
        }

        if (!decimal.TryParse(quote.Quote, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rate))
        {
            ModelState.AddModelError("", "Invalid rate from Clear Junction.");
            return View(nameof(Index), form);
        }

        form.Rate = quote.Quote;
        form.RateUuid = quote.RateUuid;
        form.BuyAmount = Math.Round(form.SellAmount * rate, 2);
        form.ExpirationTimestamp = quote.ExpirationTimestamp;

        return View(nameof(Index), form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(FxQuoteViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.RateUuid) || form.BuyAmount is null)
        {
            ModelState.AddModelError("", "Request a quote before executing the conversion.");
            return View(nameof(Index), form);
        }

        var clientOrder = $"FX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var req = new FxTransferRequest
        {
            ClientOrder = clientOrder,
            SellAmount = form.SellAmount,
            SellCurrency = form.SellCurrency,
            BuyAmount = form.BuyAmount.Value,
            BuyCurrency = form.BuyCurrency,
            RateUuid = form.RateUuid
        };

        var resp = await _cj.CreateFxTransferAsync(req);

        _store.AddFx(new FxRecord
        {
            ClientOrder = clientOrder,
            OrderReference = resp.OrderReference,
            SellCurrency = form.SellCurrency,
            SellAmount = form.SellAmount,
            BuyCurrency = form.BuyCurrency,
            BuyAmount = form.BuyAmount.Value,
            Rate = form.Rate ?? "",
            Status = resp.Status
        });

        TempData["Success"] = $"FX conversion submitted: {form.SellAmount} {form.SellCurrency} → {form.BuyAmount} {form.BuyCurrency}";
        return RedirectToAction(nameof(History));
    }
}
