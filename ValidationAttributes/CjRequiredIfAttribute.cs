using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BankTransferMVC.ValidationAttributes;

/// <summary>
/// Conditionally requires a field based on the value of another property in the same view model.
/// Mirrors Clear Junction's conditional rules (e.g. <c>institution.bankSwiftCode</c> required only
/// when <c>rail = swift</c>; <c>legalEntityIdentifier</c> required only for corporate payees on
/// FPS / CHAPS / CHAPS cross-scheme / SWIFT).
///
/// Supports a single AND-composition via <see cref="AlsoWatchProperty"/> for cases like
/// "required when Rail=swift AND PayeeEntityType=corporate".
///
/// Implements <see cref="IClientModelValidator"/> so jquery.validate.unobtrusive picks the rule
/// up via the matching adapter in <c>wwwroot/js/cj-validators.js</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class CjRequiredIfAttribute : ValidationAttribute, IClientModelValidator
{
    public string WatchProperty { get; }
    public string[] WhenValues { get; }

    /// <summary>Optional second watch property — both watches must match for the field to be required.</summary>
    public string? AlsoWatchProperty { get; init; }
    public string[] AlsoWhenValues { get; init; } = Array.Empty<string>();

    public CjRequiredIfAttribute(string watchProperty, params string[] whenValues)
    {
        WatchProperty = watchProperty;
        WhenValues = whenValues ?? Array.Empty<string>();
        ErrorMessage = "{0} is required.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (!WatchMatches(WatchProperty, WhenValues, ctx)) return ValidationResult.Success;
        if (!string.IsNullOrEmpty(AlsoWatchProperty) &&
            !WatchMatches(AlsoWatchProperty, AlsoWhenValues, ctx))
            return ValidationResult.Success;

        if (IsEmpty(value))
        {
            var name = ctx.DisplayName;
            var members = ctx.MemberName is null ? null : new[] { ctx.MemberName };
            return new ValidationResult(FormatErrorMessage(name), members);
        }
        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-cjrequiredif",
            string.Format(ErrorMessage ?? "{0} is required.", context.ModelMetadata.GetDisplayName()));
        MergeAttribute(context.Attributes, "data-val-cjrequiredif-field", WatchProperty);
        MergeAttribute(context.Attributes, "data-val-cjrequiredif-values", string.Join(",", WhenValues));
        if (!string.IsNullOrEmpty(AlsoWatchProperty))
        {
            MergeAttribute(context.Attributes, "data-val-cjrequiredif-alsofield", AlsoWatchProperty);
            MergeAttribute(context.Attributes, "data-val-cjrequiredif-alsovalues", string.Join(",", AlsoWhenValues));
        }
    }

    private static bool WatchMatches(string prop, string[] allowed, ValidationContext ctx)
    {
        var p = ctx.ObjectType.GetProperty(prop);
        if (p is null) return false;
        var v = p.GetValue(ctx.ObjectInstance)?.ToString() ?? "";
        return allowed.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsEmpty(object? value) =>
        value is null || (value is string s && string.IsNullOrWhiteSpace(s));

    private static void MergeAttribute(IDictionary<string, string> attrs, string key, string value)
    {
        if (!attrs.ContainsKey(key)) attrs.Add(key, value);
    }
}
