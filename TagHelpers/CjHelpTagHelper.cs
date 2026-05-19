using BankTransferMVC.UI;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BankTransferMVC.TagHelpers;

[HtmlTargetElement("cj-help", Attributes = "for-key")]
public class CjHelpTagHelper : TagHelper
{
    [HtmlAttributeName("for-key")]
    public string ForKey { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        var help = CjDescriptions.Get(ForKey);
        if (string.IsNullOrWhiteSpace(help.Description))
        {
            output.SuppressOutput();
            return;
        }

        output.Content.SetHtmlContent(help.Html);
    }
}

[HtmlTargetElement("cj-feature", Attributes = "feature-key")]
public class CjFeatureTagHelper : TagHelper
{
    [HtmlAttributeName("feature-key")]
    public string FeatureKey { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", "cj-feature-intro card mb-3");

        if (!CjDescriptions.Features.TryGetValue(FeatureKey, out var feature))
        {
            output.SuppressOutput();
            return;
        }

        var endpoint = string.IsNullOrEmpty(feature.Endpoint)
            ? ""
            : $"<p class=\"mb-1 small\"><strong>API:</strong> <code>{feature.Endpoint}</code></p>";
        var flow = string.IsNullOrEmpty(feature.Flow)
            ? ""
            : $"<p class=\"mb-0 small text-muted\"><strong>Flow:</strong> {feature.Flow}</p>";

        output.Content.SetHtmlContent(
            $"<div class=\"card-body py-3\">" +
            $"<h6 class=\"mb-2\">{feature.Title}</h6>" +
            $"<p class=\"mb-2 small\">{feature.Summary}</p>" +
            endpoint + flow +
            "</div>");
    }
}
