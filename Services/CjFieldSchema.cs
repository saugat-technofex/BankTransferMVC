using System.ComponentModel.DataAnnotations;
using System.Reflection;
using BankTransferMVC.ValidationAttributes;

namespace BankTransferMVC.Services;

public sealed record CjFieldRule(
    string Field,
    bool Required,
    string? WatchField = null,
    string[]? WhenValues = null,
    string? AlsoWatch = null,
    string[]? AlsoValues = null);

/// <summary>
/// Walks a view-model type and reports the required / conditional rules declared via
/// <see cref="RequiredAttribute"/> and <see cref="CjRequiredIfAttribute"/>.
///
/// Used by the Reference page to surface the spec to QA / support staff and as a
/// self-documentation source — the rules themselves live on the view models (single source of truth).
/// </summary>
public interface ICjFieldSchema
{
    IReadOnlyDictionary<string, Type> Features { get; }
    IReadOnlyList<CjFieldRule> Rules(string feature);
    IReadOnlyList<CjFieldRule> Rules(Type vmType);
}

public class CjFieldSchema : ICjFieldSchema
{
    private readonly Dictionary<string, Type> _features = new(StringComparer.OrdinalIgnoreCase)
    {
        ["payout"] = typeof(BankTransferMVC.Models.CreatePayoutViewModel),
        ["virtualIban"] = typeof(BankTransferMVC.Models.CreateIbanViewModel),
        ["wallet"] = typeof(BankTransferMVC.Models.CreateWalletViewModel),
        ["walletTransfer"] = typeof(BankTransferMVC.Models.WalletTransferViewModel),
        ["fx"] = typeof(BankTransferMVC.Models.FxQuoteViewModel),
        ["payin"] = typeof(BankTransferMVC.Models.SimulatePayinViewModel),
        ["cardPayin"] = typeof(BankTransferMVC.Models.CardPayinViewModel),
        ["refund"] = typeof(BankTransferMVC.Models.CreateRefundViewModel),
        ["compliance"] = typeof(BankTransferMVC.Models.RequisiteCheckViewModel),
        ["report"] = typeof(BankTransferMVC.Models.TransactionReportViewModel)
    };

    public IReadOnlyDictionary<string, Type> Features => _features;

    public IReadOnlyList<CjFieldRule> Rules(string feature) =>
        _features.TryGetValue(feature, out var t) ? Rules(t) : Array.Empty<CjFieldRule>();

    public IReadOnlyList<CjFieldRule> Rules(Type vmType) => Walk(vmType, "").ToList();

    private static IEnumerable<CjFieldRule> Walk(Type t, string prefix)
    {
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var path = string.IsNullOrEmpty(prefix) ? p.Name : prefix + "." + p.Name;
            var req = p.GetCustomAttribute<RequiredAttribute>();
            var cond = p.GetCustomAttribute<CjRequiredIfAttribute>();
            if (req is not null)
                yield return new CjFieldRule(path, true);
            else if (cond is not null)
                yield return new CjFieldRule(path, false, cond.WatchProperty, cond.WhenValues,
                    cond.AlsoWatchProperty, cond.AlsoWhenValues);

            // Recurse into known nested VMs.
            var pt = p.PropertyType;
            if (pt.Namespace == typeof(CjFieldSchema).Namespace?.Replace("Services", "Models")
                || pt.Namespace == "BankTransferMVC.Models")
            {
                if (pt.IsClass && pt != typeof(string) && !pt.IsArray
                    && !typeof(System.Collections.IEnumerable).IsAssignableFrom(pt))
                {
                    foreach (var nested in Walk(pt, path))
                        yield return nested;
                }
            }
        }
    }
}
