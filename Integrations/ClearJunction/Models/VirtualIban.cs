using System.Text.Json.Serialization;

namespace BankTransferMVC.Integrations.ClearJunction.Models;

public class AllocateIbanRequest
{
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("postbackUrl")] public string? PostbackUrl { get; set; }
    [JsonPropertyName("walletUuid")] public string WalletUuid { get; set; } = "";
    [JsonPropertyName("ibansGroup")] public string IbansGroup { get; set; } = "DEFAULT";
    [JsonPropertyName("ibanCountry")] public string IbanCountry { get; set; } = "GB";
    [JsonPropertyName("registrant")] public CjEntity Registrant { get; set; } = new();
    [JsonPropertyName("customInfo")] public Dictionary<string, object>? CustomInfo { get; set; }
}

public class AllocateIbanResponse
{
    [JsonPropertyName("requestReference")] public string RequestReference { get; set; } = "";
    [JsonPropertyName("clientOrder")] public string ClientOrder { get; set; } = "";
    [JsonPropertyName("orderReference")] public string OrderReference { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("iban")] public string? Iban { get; set; }
}
