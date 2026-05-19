using System.Text.Json.Serialization;

namespace BankTransferMVC.Integrations.ClearJunction.Models;

public class FxRateRequest
{
    [JsonPropertyName("sellCurrency")] public string SellCurrency { get; set; } = "";
    [JsonPropertyName("buyCurrency")] public string BuyCurrency { get; set; } = "";
}

public class FxQuote
{
    [JsonPropertyName("rateUuid")] public string RateUuid { get; set; } = "";
    [JsonPropertyName("quote")] public string Quote { get; set; } = "";
    [JsonPropertyName("sellCurrency")] public string SellCurrency { get; set; } = "";
    [JsonPropertyName("buyCurrency")] public string BuyCurrency { get; set; } = "";
    [JsonPropertyName("expirationTimestamp")] public string ExpirationTimestamp { get; set; } = "";
}

public class FxRateResponse
{
    [JsonPropertyName("requestReference")] public string RequestReference { get; set; } = "";
    [JsonPropertyName("quotes")] public List<FxQuote> Quotes { get; set; } = new();
}

public class FxTransferRequest
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("postbackUrl")] public string? PostbackUrl { get; set; }
    [JsonPropertyName("sellAmount")] public decimal SellAmount { get; set; }
    [JsonPropertyName("buyAmount")] public decimal BuyAmount { get; set; }
    [JsonPropertyName("sellCurrency")] public string SellCurrency { get; set; } = "";
    [JsonPropertyName("buyCurrency")] public string BuyCurrency { get; set; } = "";
    [JsonPropertyName("rateUuid")] public string RateUuid { get; set; } = "";
}

public class FxTransferResponse
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("operTimestamp")] public string OperTimestamp { get; set; } = "";
    [JsonPropertyName("sellAmount")] public decimal SellAmount { get; set; }
    [JsonPropertyName("sellCurrency")] public string SellCurrency { get; set; } = "";
    [JsonPropertyName("buyAmount")] public decimal BuyAmount { get; set; }
    [JsonPropertyName("buyCurrency")] public string BuyCurrency { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("rateUuid")] public string RateUuid { get; set; } = "";
    [JsonPropertyName("requestReference")] public string RequestReference { get; set; } = "";
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; } = "";
}
