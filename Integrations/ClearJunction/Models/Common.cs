using System.Text.Json.Serialization;

namespace BankTransferMVC.Integrations.ClearJunction.Models;

public class CjAddress
{
    [JsonPropertyName("country")] public string Country { get; set; } = "";
    [JsonPropertyName("zip")] public string Zip { get; set; } = "";
    [JsonPropertyName("city")] public string City { get; set; } = "";
    [JsonPropertyName("street")] public string Street { get; set; } = "";
}

public class CjDocument
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("number")] public string Number { get; set; } = "";
    [JsonPropertyName("issuedCountryCode")] public string IssuedCountryCode { get; set; } = "";
    [JsonPropertyName("issuedBy")] public string IssuedBy { get; set; } = "";
    [JsonPropertyName("issuedDate")] public string IssuedDate { get; set; } = "";
    [JsonPropertyName("expirationDate")] public string ExpirationDate { get; set; } = "";
}

public class CjIndividual
{
    [JsonPropertyName("phone")] public string Phone { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("birthDate")] public string BirthDate { get; set; } = "";
    [JsonPropertyName("birthPlace")] public string? BirthPlace { get; set; }
    [JsonPropertyName("address")] public CjAddress Address { get; set; } = new();
    [JsonPropertyName("document")] public CjDocument? Document { get; set; }
    [JsonPropertyName("firstName")] public string FirstName { get; set; } = "";
    [JsonPropertyName("lastName")] public string LastName { get; set; } = "";
    [JsonPropertyName("middleName")] public string? MiddleName { get; set; }
}

public class CjCorporate
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("registrationNumber")] public string RegistrationNumber { get; set; } = "";
    [JsonPropertyName("incorporationCountry")] public string IncorporationCountry { get; set; } = "";
    [JsonPropertyName("address")] public CjAddress Address { get; set; } = new();
    [JsonPropertyName("incorporationDate")] public string? IncorporationDate { get; set; }
    [JsonPropertyName("legalEntityIdentifier")] public string? LegalEntityIdentifier { get; set; }
}

public class CjEntity
{
    [JsonPropertyName("clientCustomerId")] public string? ClientCustomerId { get; set; }
    [JsonPropertyName("walletUuid")] public string? WalletUuid { get; set; }
    [JsonPropertyName("individual")] public CjIndividual? Individual { get; set; }
    [JsonPropertyName("corporate")] public CjCorporate? Corporate { get; set; }
}

public class CjMessage
{
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("message")] public string Message { get; set; } = "";
    [JsonPropertyName("details")] public string? Details { get; set; }
}

public class CjSubStatuses
{
    [JsonPropertyName("operStatus")] public string OperStatus { get; set; } = "";
    [JsonPropertyName("complianceStatus")] public string ComplianceStatus { get; set; } = "";
}

public class CjPaymentPurpose
{
    [JsonPropertyName("code")] public string Code { get; set; } = "INTP";
    [JsonPropertyName("category")] public string Category { get; set; } = "GP2P";
}
