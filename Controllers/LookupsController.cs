using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankTransferMVC.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ICjReferenceCatalog _catalog;

    public LookupsController(ICjReferenceCatalog catalog) => _catalog = catalog;

    [HttpGet]
    public ActionResult<IEnumerable<string>> Kinds() => Ok(_catalog.Kinds);

    [HttpGet("{kind}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public ActionResult<IEnumerable<object>> Search(string kind, [FromQuery] string? q = null, [FromQuery] int take = 50)
    {
        if (!_catalog.Kinds.ContainsKey(kind)) return NotFound(new { error = $"Unknown lookup kind '{kind}'." });

        take = Math.Clamp(take, 1, 500);
        var items = _catalog.Search(kind, q, take)
            .Select(i => new { code = i.Code, label = i.Label, group = i.Group });

        return Ok(items);
    }
}
