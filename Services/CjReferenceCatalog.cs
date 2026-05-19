using System.Collections.Concurrent;

namespace BankTransferMVC.Services;

public interface ICjReferenceCatalog
{
    IReadOnlyDictionary<string, string> Kinds { get; }
    IReadOnlyList<CjLookupItem> Get(string kind);
    IReadOnlyList<CjLookupItem> Search(string kind, string? query, int take = 50);
    string? Resolve(string kind, string code);
}

public class CjReferenceCatalog : ICjReferenceCatalog
{
    private readonly Dictionary<string, IReadOnlyList<CjLookupItem>> _data;
    private readonly Dictionary<string, string> _kinds;
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _resolveCache = new();

    public CjReferenceCatalog()
    {
        _data = new(StringComparer.OrdinalIgnoreCase)
        {
            ["currency"] = CjReferenceData.Currencies,
            ["country"] = CjReferenceData.Countries,
            ["purposeCode"] = CjReferenceData.PurposeCodes,
            ["purposeCategory"] = CjReferenceData.PurposeCategories,
            ["clearingSystem"] = CjReferenceData.ClearingSystems,
            ["documentType"] = CjReferenceData.DocumentTypes,
            ["walletType"] = CjReferenceData.WalletTypes,
            ["accountType"] = CjReferenceData.AccountTypes,
            ["accountCategory"] = CjReferenceData.AccountCategories,
            ["amlRiskLevel"] = CjReferenceData.AmlRiskLevels,
            ["txnType"] = CjReferenceData.TransactionTypes,
            ["rail"] = CjReferenceData.Rails,
            ["status"] = CjReferenceData.OrderStatuses,
            ["operStatus"] = CjReferenceData.OperStatuses,
            ["complianceStatus"] = CjReferenceData.ComplianceStatuses,
            ["webhookType"] = CjReferenceData.WebhookTypes,
            ["refundDirection"] = CjReferenceData.RefundDirections,
            ["checkKind"] = CjReferenceData.CheckKinds,
            ["ibansGroup"] = CjReferenceData.IbansGroups
        };

        _kinds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["currency"] = "ISO-4217 currencies (CJ multi-currency coverage)",
            ["country"] = "ISO-3166 alpha-2 country codes",
            ["purposeCode"] = "ISO 20022 ExternalPurpose1Code",
            ["purposeCategory"] = "ISO 20022 ExternalCategoryPurpose1Code",
            ["clearingSystem"] = "Local clearing system identifiers (ABA, CHIPS, ...)",
            ["documentType"] = "KYC document types",
            ["walletType"] = "CJ wallet/account structures",
            ["accountType"] = "Virtual account types",
            ["accountCategory"] = "Virtual account category (crypto chains)",
            ["amlRiskLevel"] = "AML risk classification",
            ["txnType"] = "Transaction discriminator",
            ["rail"] = "Payment rails / payout endpoints",
            ["status"] = "Order lifecycle status",
            ["operStatus"] = "Operational sub-status",
            ["complianceStatus"] = "Compliance sub-status",
            ["webhookType"] = "Inbound webhook discriminators",
            ["refundDirection"] = "Refund direction (outgoing / incoming)",
            ["checkKind"] = "Requisite check kind",
            ["ibansGroup"] = "Virtual IBAN pool group"
        };
    }

    public IReadOnlyDictionary<string, string> Kinds => _kinds;

    public IReadOnlyList<CjLookupItem> Get(string kind) =>
        _data.TryGetValue(kind, out var v) ? v : Array.Empty<CjLookupItem>();

    public IReadOnlyList<CjLookupItem> Search(string kind, string? query, int take = 50)
    {
        var all = Get(kind);
        if (all.Count == 0) return all;
        if (string.IsNullOrWhiteSpace(query)) return all.Take(take).ToList();

        var q = query.Trim();
        return all
            .Where(i =>
                i.Code.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Label.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (i.Group?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(take)
            .ToList();
    }

    public string? Resolve(string kind, string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var map = _resolveCache.GetOrAdd(kind, k =>
            Get(k).ToDictionary(i => i.Code, i => i.Label, StringComparer.OrdinalIgnoreCase));
        return map.TryGetValue(code, out var label) ? label : null;
    }
}
