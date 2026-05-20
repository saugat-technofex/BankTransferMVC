using BankTransferMVC.ValidationAttributes;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel.DataAnnotations;

namespace BankTransferMVC.TagHelpers;

/// <summary>
/// Renders a Bootstrap form-label with:
///   - the model display name (or override text)
///   - a red asterisk for [Required]
///   - an amber asterisk for [CjRequiredIf] (conditional)
///   - an inline "conditional" badge with the condition text when present
///
/// Usage: &lt;cj-label asp-for="Rail"&gt;&lt;/cj-label&gt;
/// </summary>
[HtmlTargetElement("cj-label", Attributes = "asp-for")]
public class CjLabelTagHelper : TagHelper
{
    [HtmlAttributeName("asp-for")] public ModelExpression For { get; set; } = default!;
    [HtmlAttributeName("text")] public string? Text { get; set; }
    [HtmlAttributeName("class")] public string? CssClass { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "label";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("for", For.Name);
        output.Attributes.SetAttribute("class",
            string.IsNullOrWhiteSpace(CssClass) ? "form-label" : $"form-label {CssClass}");

        var text = !string.IsNullOrWhiteSpace(Text)
            ? Text
            : For.ModelExplorer.Metadata.DisplayName ?? For.Name;

        var validators = For.ModelExplorer.Metadata.ValidatorMetadata;
        var isHardRequired = validators.OfType<RequiredAttribute>().Any();
        var conditional = validators.OfType<CjRequiredIfAttribute>().FirstOrDefault();
        var isConditional = conditional is not null;

        var sb = new System.Text.StringBuilder();
        sb.Append(System.Net.WebUtility.HtmlEncode(text));

        if (isHardRequired)
        {
            sb.Append(" <span class=\"cj-required\" title=\"Required\" aria-hidden=\"true\">*</span>");
        }
        else if (isConditional)
        {
            var values = string.Join(" / ", conditional!.WhenValues);
            var also = string.IsNullOrEmpty(conditional.AlsoWatchProperty)
                ? ""
                : $" and {conditional.AlsoWatchProperty} = {string.Join(" / ", conditional.AlsoWhenValues)}";
            var hint = $"Required when {conditional.WatchProperty} = {values}{also}";
            sb.Append($" <span class=\"cj-required cj-required-cond\" title=\"{System.Net.WebUtility.HtmlEncode(hint)}\" aria-hidden=\"true\">*</span>");
            sb.Append($" <span class=\"cj-cond-badge\" title=\"{System.Net.WebUtility.HtmlEncode(hint)}\">conditional</span>");
        }

        output.Content.SetHtmlContent(sb.ToString());
    }
}
