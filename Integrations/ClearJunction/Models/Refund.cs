using System.Text.Json.Serialization;

namespace BankTransferMVC.Integrations.ClearJunction.Models;

public class RefundRequest
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("relatedOrderReference")] public string RelatedOrderReference { get; set; } = "";
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("postbackUrl")] public string? PostbackUrl { get; set; }
}

public class RefundResponse
{
    [JsonPropertyName("requestReference")] public string RequestReference { get; set; } = "";
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "created";
    [JsonPropertyName("subStatuses")] public CjSubStatuses SubStatuses { get; set; } = new();
}

public class CardPayinRequest
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("postbackUrl")] public string? PostbackUrl { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("productName")] public string ProductName { get; set; } = "";
    [JsonPropertyName("siteAddress")] public string SiteAddress { get; set; } = "";
    [JsonPropertyName("successUrl")] public string SuccessUrl { get; set; } = "";
    [JsonPropertyName("failUrl")] public string FailUrl { get; set; } = "";
    [JsonPropertyName("payer")] public CjEntity Payer { get; set; } = new();
}

public class CardPayinResponse
{
    [JsonPropertyName("requestReference")] public string RequestReference { get; set; } = "";
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "pending";
    [JsonPropertyName("subStatuses")] public CjSubStatuses SubStatuses { get; set; } = new();
    [JsonPropertyName("redirectUrl")] public string RedirectUrl { get; set; } = "";
}

public class TransactionActionRequest
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("reason")] public string? Reason { get; set; }
}

public class TransactionActionResponse
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("subStatuses")] public CjSubStatuses SubStatuses { get; set; } = new();
}

public class CopCheckRequest
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("sortCode")] public string SortCode { get; set; } = "";
    [JsonPropertyName("accountNumber")] public string AccountNumber { get; set; } = "";
    [JsonPropertyName("secondaryReferenceData")] public string? SecondaryReferenceData { get; set; }
    [JsonPropertyName("accountType")] public string AccountType { get; set; } = "personal";
}

public class CopCheckResponse
{
    [JsonPropertyName("requestReference")] public string RequestReference { get; set; } = "";
    /// <summary>match | closeMatch | noMatch | accountNotFound | unavailable</summary>
    [JsonPropertyName("result")] public string Result { get; set; } = "match";
    [JsonPropertyName("reasonCode")] public string? ReasonCode { get; set; }
    [JsonPropertyName("matchedName")] public string? MatchedName { get; set; }
}

public class IbanCheckResponse
{
    [JsonPropertyName("iban")] public string Iban { get; set; } = "";
    /// <summary>reachable | unreachable | invalidFormat</summary>
    [JsonPropertyName("result")] public string Result { get; set; } = "reachable";
    [JsonPropertyName("bankName")] public string? BankName { get; set; }
    [JsonPropertyName("bic")] public string? Bic { get; set; }
    [JsonPropertyName("country")] public string? Country { get; set; }
}

public class WalletGetResponse
{
    [JsonPropertyName("walletUuid")] public string WalletUuid { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("balance")] public decimal Balance { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "active";
}

public class CreateWalletRequest
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("postbackUrl")] public string? PostbackUrl { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("clientCustomerId")] public string ClientCustomerId { get; set; } = "";
    [JsonPropertyName("currency")] public string Currency { get; set; } = "EUR";
    [JsonPropertyName("type")] public string Type { get; set; } = "corporate";
    [JsonPropertyName("ibanCountry")] public string IbanCountry { get; set; } = "GB";
    [JsonPropertyName("ibansGroup")] public string IbansGroup { get; set; } = "DEFAULT";
    [JsonPropertyName("holder")] public CjEntity Holder { get; set; } = new();
}

public class CreateWalletResponse
{
    [JsonPropertyName("requestReference")] public string RequestReference { get; set; } = "";
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("walletUuid")] public string WalletUuid { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "active";
    [JsonPropertyName("currency")] public string Currency { get; set; } = "EUR";
}

public class WalletTransferRequest
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("fromWalletUuid")] public string FromWalletUuid { get; set; } = "";
    [JsonPropertyName("toWalletUuid")] public string ToWalletUuid { get; set; } = "";
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

public class WalletTransferResponse
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "completed";
}

public class TransactionReportRequest
{
    [JsonPropertyName("walletUuid")] public string WalletUuid { get; set; } = "";
    [JsonPropertyName("dateFrom")] public string DateFrom { get; set; } = "";
    [JsonPropertyName("dateTo")] public string DateTo { get; set; } = "";
}

public class TransactionReportRow
{
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("transactionType")] public string TransactionType { get; set; } = "";
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; } = "";
}

public class TransactionReportResponse
{
    [JsonPropertyName("rows")] public List<TransactionReportRow> Rows { get; set; } = new();
}

/// <summary>Simulator scenario applied to a new order. Public so the Settings UI can list options.</summary>
public static class CjScenarios
{
    public const string Success = "success";
    public const string ComplianceHold = "complianceHold";
    public const string Declined = "declined";
    public const string InsufficientFunds = "insufficientFunds";
    public const string NetworkError = "networkError";

    public static readonly (string Code, string Label)[] All =
    {
        (Success, "Happy path (settle / approve)"),
        (ComplianceHold, "Compliance hold (sticky pending)"),
        (Declined, "Declined (rejected by ops)"),
        (InsufficientFunds, "Insufficient funds (declined)"),
        (NetworkError, "Network error (throws on submit)")
    };
}
