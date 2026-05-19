using System.Diagnostics;
using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Models;
using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Controllers;

public class HomeController : Controller
{
    private readonly ITransferStore _store;
    private readonly ClearJunctionOptions _options;

    public HomeController(ITransferStore store, IOptions<ClearJunctionOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public IActionResult Index()
    {
        var payouts = _store.ListPayouts();
        var events = _store.ListEvents();
        var vm = new DashboardViewModel
        {
            IbanCount = _store.ListIbans().Count,
            PayoutCount = payouts.Count,
            FxCount = _store.ListFx().Count,
            EventCount = events.Count,
            SimulationMode = _options.SimulationMode,
            BaseUrl = _options.BaseUrl,
            RecentPayouts = payouts.Take(5).ToList(),
            RecentEvents = events.Take(5).ToList()
        };
        return View(vm);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
