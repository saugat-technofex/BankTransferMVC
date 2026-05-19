namespace BankTransferMVC.Integrations.ClearJunction;

public class ClearJunctionOptions
{
    public const string SectionName = "ClearJunction";

    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiPassword { get; set; } = "";
    public string PostbackBaseUrl { get; set; } = "";

    // When true the typed client returns canned realistic responses instead of
    // calling the network. Use this during local UX development until Clear
    // Junction issues real sandbox credentials.
    public bool SimulationMode { get; set; } = true;
}
