using System.Text.Json.Serialization;

namespace BankTransferMVC.Integrations.ClearJunction.Models;

public class CjInstitution
{
    [JsonPropertyName("bankSwiftCode")] public string BankSwiftCode { get; set; } = "";
    [JsonPropertyName("clearingSystemIdCode")] public string? ClearingSystemIdCode { get; set; }
    [JsonPropertyName("memberId")] public string? MemberId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("address")] public CjAddress? Address { get; set; }
}

public class CjRequisite
{
    [JsonPropertyName("iban")] public string? Iban { get; set; }
    [JsonPropertyName("accountNumber")] public string? AccountNumber { get; set; }
    [JsonPropertyName("sortCode")] public string? SortCode { get; set; }
    [JsonPropertyName("bankSwiftCode")] public string? BankSwiftCode { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("institution")] public CjInstitution? Institution { get; set; }
    [JsonPropertyName("intermediaryInstitution")] public CjInstitution? IntermediaryInstitution { get; set; }
}

public class PayoutRequest
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("paymentPurposeCodes")] public CjPaymentPurpose PaymentPurposeCodes { get; set; } = new();
    [JsonPropertyName("postbackUrl")] public string? PostbackUrl { get; set; }
    [JsonPropertyName("payer")] public CjEntity Payer { get; set; } = new();
    [JsonPropertyName("payee")] public CjEntity Payee { get; set; } = new();
    [JsonPropertyName("ultimatePayer")] public CjEntity? UltimatePayer { get; set; }
    [JsonPropertyName("ultimatePayee")] public CjEntity? UltimatePayee { get; set; }
    [JsonPropertyName("payerRequisite")] public CjRequisite PayerRequisite { get; set; } = new();
    [JsonPropertyName("payeeRequisite")] public CjRequisite PayeeRequisite { get; set; } = new();
    [JsonPropertyName("customInfo")] public Dictionary<string, object>? CustomInfo { get; set; }
}

public class PayoutResponse
{
    [JsonPropertyName("requestReference")] public string RequestReference { get; set; } = "";
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("subStatuses")] public CjSubStatuses SubStatuses { get; set; } = new();
    [JsonPropertyName("messages")] public List<CjMessage>? Messages { get; set; }
}

public class PayoutStatusResponse : PayoutResponse
{
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("operationCurrency")] public string? OperationCurrency { get; set; }
    [JsonPropertyName("operationAmount")] public decimal? OperationAmount { get; set; }
    [JsonPropertyName("transactionType")] public string TransactionType { get; set; } = "Payout";
}
