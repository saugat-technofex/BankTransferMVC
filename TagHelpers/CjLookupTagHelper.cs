using BankTransferMVC.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BankTransferMVC.TagHelpers;

/// <summary>
/// Renders a searchable Tom Select dropdown backed by the CJ reference catalog.
///
/// <code>
///   &lt;cj-lookup asp-for="Currency" kind="currency"&gt;&lt;/cj-lookup&gt;
///   &lt;cj-lookup asp-for="PayeeCountry" kind="country" remote="true"&gt;&lt;/cj-lookup&gt;
/// </code>
///
/// Tag helper outputs a real &lt;select&gt; element so the value posts back via standard model binding.
/// Behaviour is wired in <c>wwwroot/js/cj-lookup.js</c>.
/// </summary>
[HtmlTargetElement("cj-lookup", Attributes = "asp-for,kind")]
public class CjLookupTagHelper : TagHelper
{
    private readonly ICjReferenceCatalog _catalog;

    public CjLookupTagHelper(ICjReferenceCatalog catalog) => _catalog = catalog;

    [HtmlAttributeName("asp-for")]
    public ModelExpression? For { get; set; }

    [HtmlAttributeName("kind")]
    public string Kind { get; set; } = "";

    /// <summary>If true, options are fetched from /api/lookups/{kind}; otherwise rendered server-side.</summary>
    [HtmlAttributeName("remote")]
    public bool Remote { get; set; } = false;

    [HtmlAttributeName("placeholder")]
    public string? Placeholder { get; set; }

    /// <summary>Allow an empty selection (renders a leading blank option).</summary>
    [HtmlAttributeName("optional")]
    public bool Optional { get; set; } = false;

    /// <summary>CSS classes added to the underlying select.</summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var name = For?.Name ?? Kind;
        var current = For?.Model?.ToString() ?? "";
        var classes = string.IsNullOrWhiteSpace(CssClass) ? "form-select cj-lookup" : $"form-select cj-lookup {CssClass}";

        output.TagName = "select";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("name", name);
        output.Attributes.SetAttribute("id", name);
        output.Attributes.SetAttribute("class", classes);
        output.Attributes.SetAttribute("data-cj-lookup", Kind);
        if (Remote) output.Attributes.SetAttribute("data-cj-remote", "true");
        if (!string.IsNullOrWhiteSpace(Placeholder))
            output.Attributes.SetAttribute("data-placeholder", Placeholder);

        var sb = new System.Text.StringBuilder();

        if (Optional)
        {
            sb.Append("<option value=\"\"></option>");
        }

        if (Remote)
        {
            // Server-side renders only the current value (so the form posts back correctly even
            // before the JS init runs); Tom Select fetches additional options via /api/lookups/{kind}.
            if (!string.IsNullOrEmpty(current))
            {
                var label = _catalog.Resolve(Kind, current) ?? current;
                sb.Append($"<option value=\"{System.Net.WebUtility.HtmlEncode(current)}\" selected>{System.Net.WebUtility.HtmlEncode(label)}</option>");
            }
        }
        else
        {
            var items = _catalog.Get(Kind);
            string? lastGroup = null;
            var inGroup = false;
            foreach (var item in items.OrderBy(i => i.Group).ThenBy(i => i.Label))
            {
                if (item.Group != lastGroup)
                {
                    if (inGroup) sb.Append("</optgroup>");
                    if (!string.IsNullOrEmpty(item.Group))
                    {
                        sb.Append($"<optgroup label=\"{System.Net.WebUtility.HtmlEncode(item.Group)}\">");
                        inGroup = true;
                    }
                    else
                    {
                        inGroup = false;
                    }
                    lastGroup = item.Group;
                }
                var selected = string.Equals(item.Code, current, StringComparison.OrdinalIgnoreCase) ? " selected" : "";
                sb.Append($"<option value=\"{System.Net.WebUtility.HtmlEncode(item.Code)}\"{selected}>{System.Net.WebUtility.HtmlEncode(item.Label)} ({System.Net.WebUtility.HtmlEncode(item.Code)})</option>");
            }
            if (inGroup) sb.Append("</optgroup>");
        }

        output.Content.SetHtmlContent(sb.ToString());
    }
}
