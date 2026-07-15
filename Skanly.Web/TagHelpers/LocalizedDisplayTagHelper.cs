// Skanly.Web/TagHelpers/LocalizedDisplayTagHelper.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;
using Skanly.Web.Resources;

namespace Skanly.Web.TagHelpers;

/// <summary>
/// Tag helper that localizes text using SharedResource.
/// Usage: &lt;localized key="Save" /&gt;
/// Renders: "Save" in English, "حفظ" in Arabic
/// </summary>
[HtmlTargetElement("localized")]
public class LocalizedDisplayTagHelper : TagHelper
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizedDisplayTagHelper(
        IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    [HtmlAttributeName("key")]
    public string Key { get; set; } = string.Empty;

    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    public override void Process(
        TagHelperContext context,
        TagHelperOutput output)
    {
        output.TagName = "span";

        if (!string.IsNullOrEmpty(CssClass))
            output.Attributes.SetAttribute("class", CssClass);

        var localizedValue = _localizer[Key];
        output.Content.SetContent(localizedValue.Value);
    }
}