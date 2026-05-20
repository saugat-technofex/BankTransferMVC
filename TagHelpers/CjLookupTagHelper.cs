using BankTransferMVC.Services;
using BankTransferMVC.ValidationAttributes;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel.DataAnnotations;

namespace BankTransferMVC.TagHelpers;

/// <summary>
/// Renders a searchable Tom Select dropdown backed by the CJ reference catalog.
/// Forwards [Required] / [CjRequiredIf] metadata onto the &lt;select&gt; as the matching
/// data-val-* unobtrusive attributes so jquery.validate picks the rules up automatically.
///
/// <code>
///   &lt;cj-lookup asp-for="Currency" kind="currency"&gt;&lt;/cj-lookup&gt;
///   &lt;cj-lookup asp-for="PayeeCountry" kind="country" remote="true"&gt;&lt;/cj-lookup&gt;
/// </code>
/// </summary>
[HtmlTargetElement("cj-lookup", Attributes = "asp-for,kind")]
public class CjLookupTagHelper : TagHelper
{
    private readonly ICjReferenceCatalog _catalog;
    public CjLookupTagHelper(ICjReferenceCatalog catalog) => _catalog = catalog;

    [HtmlAttributeName("asp-for")] public ModelExpression? For { get; set; }
    [HtmlAttributeName("kind")] public string Kind { get; set; } = "";

    /// <summary>If true, options are fetched from /api/lookups/{kind}; otherwise rendered server-side.</summary>
    [HtmlAttributeName("remote")] public bool Remote { get; set; } = false;
    [HtmlAttributeName("placeholder")] public string? Placeholder { get; set; }
    [HtmlAttributeName("optional")] public bool Optional { get; set; } = false;
    [HtmlAttributeName("class")] public string? CssClass { get; set; }
    [HtmlAttributeName("id")] public string? IdOverride { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var name = For?.Name ?? Kind;
        var id = IdOverride ?? name;
        var current = For?.Model?.ToString() ?? "";
        var classes = string.IsNullOrWhiteSpace(CssClass) ? "form-select cj-lookup" : $"form-select cj-lookup {CssClass}";

        output.TagName = "select";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("name", name);
        output.Attributes.SetAttribute("id", id);
        output.Attributes.SetAttribute("class", classes);
        output.Attributes.SetAttribute("data-cj-lookup", Kind);
        if (Remote) output.Attributes.SetAttribute("data-cj-remote", "true");
        if (!string.IsNullOrWhiteSpace(Placeholder))
            output.Attributes.SetAttribute("data-placeholder", Placeholder);

        ForwardValidationAttributes(output);

        var sb = new System.Text.StringBuilder();
        if (Optional) sb.Append("<option value=\"\"></option>");

        if (Remote)
        {
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

    private void ForwardValidationAttributes(TagHelperOutput output)
    {
        if (For is null) return;
        var validators = For.ModelExplorer.Metadata.ValidatorMetadata;

        var required = validators.OfType<RequiredAttribute>().FirstOrDefault();
        if (required is not null)
        {
            output.Attributes.SetAttribute("required", "required");
            output.Attributes.SetAttribute("data-val", "true");
            var msg = string.Format(required.ErrorMessage ?? "{0} is required.",
                For.ModelExplorer.Metadata.DisplayName ?? For.Name);
            output.Attributes.SetAttribute("data-val-required", msg);
        }

        var cond = validators.OfType<CjRequiredIfAttribute>().FirstOrDefault();
        if (cond is not null)
        {
            output.Attributes.SetAttribute("data-val", "true");
            var displayName = For.ModelExplorer.Metadata.DisplayName ?? For.Name;
            var msg = string.Format(cond.ErrorMessage ?? "{0} is required.", displayName);
            output.Attributes.SetAttribute("data-val-cjrequiredif", msg);
            output.Attributes.SetAttribute("data-val-cjrequiredif-field", cond.WatchProperty);
            output.Attributes.SetAttribute("data-val-cjrequiredif-values", string.Join(",", cond.WhenValues));
            if (!string.IsNullOrEmpty(cond.AlsoWatchProperty))
            {
                output.Attributes.SetAttribute("data-val-cjrequiredif-alsofield", cond.AlsoWatchProperty);
                output.Attributes.SetAttribute("data-val-cjrequiredif-alsovalues", string.Join(",", cond.AlsoWhenValues));
            }
        }
    }
}
