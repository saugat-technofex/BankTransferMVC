using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

public class ReferenceController : Controller
{
    private readonly ICjReferenceCatalog _catalog;

    public ReferenceController(ICjReferenceCatalog catalog) => _catalog = catalog;

    public IActionResult Index(string? kind = null, string? q = null)
    {
        kind ??= _catalog.Kinds.Keys.First();
        var items = _catalog.Search(kind, q, take: 500);

        ViewBag.SelectedKind = kind;
        ViewBag.Query = q;
        ViewBag.Kinds = _catalog.Kinds;
        ViewBag.Description = _catalog.Kinds.TryGetValue(kind, out var d) ? d : "";
        return View(items);
    }
}
