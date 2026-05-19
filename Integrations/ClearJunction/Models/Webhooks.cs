using System.Text.Json;
using System.Text.Json.Serialization;

namespace BankTransferMVC.Integrations.ClearJunction.Models;

public class PayinNotification
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("operTimestamp")] public string OperTimestamp { get; set; } = "";
    [JsonPropertyName("messages")] public List<CjMessage>? Messages { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("operationCurrency")] public string? OperationCurrency { get; set; }
    [JsonPropertyName("operationAmount")] public decimal? OperationAmount { get; set; }
    [JsonPropertyName("productName")] public string? ProductName { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("transactionType")] public string TransactionType { get; set; } = "Payin";
    [JsonPropertyName("subStatuses")] public CjSubStatuses SubStatuses { get; set; } = new();
    [JsonPropertyName("payer")] public CjEntity? Payer { get; set; }
    [JsonPropertyName("payee")] public CjEntity? Payee { get; set; }
    [JsonPropertyName("paymentDetails")] public JsonElement? PaymentDetails { get; set; }
    [JsonPropertyName("messageUuid")] public string MessageUuid { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "payinNotification";
}

public class PayoutNotification : PayinNotification
{
    public PayoutNotification()
    {
        TransactionType = "Payout";
        Type = "payoutNotification";
    }
}
