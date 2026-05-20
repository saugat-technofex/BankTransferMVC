using System.ComponentModel.DataAnnotations;
using BankTransferMVC.ValidationAttributes;

namespace BankTransferMVC.Models;

public class DashboardViewModel
{
    public int IbanCount { get; set; }
    public int PayoutCount { get; set; }
    public int FxCount { get; set; }
    public int EventCount { get; set; }
    public bool SimulationMode { get; set; }
    public string BaseUrl { get; set; } = "";
    public IReadOnlyList<PayoutRecord> RecentPayouts { get; set; } = new List<PayoutRecord>();
    public IReadOnlyList<WebhookEvent> RecentEvents { get; set; } = new List<WebhookEvent>();
}

// =============================================================================
//  Virtual IBAN  ·  POST /v7/gate/allocate/v3/create/iban
// =============================================================================
public class CreateIbanViewModel : IValidatableObject
{
    [Required, Display(Name = "Client customer id")]
    public string ClientCustomerId { get; set; } = "";

    /// <summary>
    /// The CJ wallet this IBAN will be linked to. Leave blank to use the
    /// DefaultWalletUuid from configuration.
    /// </summary>
    [Display(Name = "Wallet UUID (leave blank for default)")]
    public string? WalletUuid { get; set; }

    [Required, Display(Name = "IBAN country")]
    public string IbanCountry { get; set; } = "GB";

    [Required, Display(Name = "IBAN pool group")]
    public string IbansGroup { get; set; } = "DEFAULT";

    /// <summary>The registrant block — drives individual/corporate sub-fields, address, etc.</summary>
    public CjPartyViewModel Registrant { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        var r = Registrant;
        // Address is mandatory on every IBAN allocation.
        if (string.IsNullOrWhiteSpace(r.Street))
            yield return new ValidationResult("Registrant street is required.", new[] { "Registrant.Street" });
        if (string.IsNullOrWhiteSpace(r.City))
            yield return new ValidationResult("Registrant city is required.", new[] { "Registrant.City" });
        if (string.IsNullOrWhiteSpace(r.Zip))
            yield return new ValidationResult("Registrant zip is required.", new[] { "Registrant.Zip" });
        if (string.IsNullOrWhiteSpace(r.Country))
            yield return new ValidationResult(
                "Registrant address country is required (in the address section — not the same as IBAN country or incorporation country).",
                new[] { "Registrant.Country" });
        if (string.IsNullOrWhiteSpace(r.Email))
            yield return new ValidationResult("Registrant email is required.", new[] { "Registrant.Email" });
    }
}

// =============================================================================
//  FX  ·  POST /v7/gate/fx/instant/rate  ·  POST /v7/gate/fx/instant/transfer
// =============================================================================
public class FxQuoteViewModel : IValidatableObject
{
    [Required, Display(Name = "Sell currency")] public string SellCurrency { get; set; } = "EUR";
    [Required, Display(Name = "Buy currency")] public string BuyCurrency { get; set; } = "USD";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Sell amount")]
    public decimal SellAmount { get; set; } = 1000;

    public string? RateUuid { get; set; }
    public string? Rate { get; set; }
    public decimal? BuyAmount { get; set; }
    public string? ExpirationTimestamp { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (string.Equals(SellCurrency, BuyCurrency, StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("Sell and buy currencies must differ.", new[] { nameof(BuyCurrency) });
    }
}

// =============================================================================
//  Pay-out  ·  POST /v7/gate/payout/bankTransfer/{rail}
// =============================================================================
public class CreatePayoutViewModel : IValidatableObject
{
    [Required, Display(Name = "Rail")]
    public string Rail { get; set; } = "sepa";

    [Required, Display(Name = "Currency")]
    public string Currency { get; set; } = "EUR";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Amount")]
    public decimal Amount { get; set; } = 100;

    [Required, Display(Name = "Reference / description"), StringLength(140)]
    public string Description { get; set; } = "Payment";

    [Display(Name = "Postback URL")]
    [Url]
    public string? PostbackUrl { get; set; }

    [Display(Name = "Purpose code")] public string? PurposeCode { get; set; } = "INTP";
    [Display(Name = "Purpose category")] public string? PurposeCategory { get; set; } = "GP2P";

    // --- payer ---
    [Display(Name = "Payer wallet UUID")] public string? PayerWalletUuid { get; set; }
    [Display(Name = "Payer customer id")] public string PayerClientCustomerId { get; set; } = "";
    [Display(Name = "Payer IBAN")] public string? PayerIban { get; set; }
    public CjPartyViewModel Payer { get; set; } = new() { EntityType = "corporate" };

    // --- payee ---
    public CjPartyViewModel Payee { get; set; } = new() { EntityType = "individual", Country = "DE" };

    // --- payee requisite (rail-specific) ---
    [CjRequiredIf("Rail", "sepa", "sepaInst", "swift", "chapsCrossScheme", "internal"),
     Display(Name = "Payee IBAN")]
    public string? PayeeIban { get; set; }

    [CjRequiredIf("Rail", "fps", "chaps"), Display(Name = "Account number")]
    public string? PayeeAccountNumber { get; set; }

    [CjRequiredIf("Rail", "fps", "chaps"), Display(Name = "Sort code")]
    public string? PayeeSortCode { get; set; }

    // --- SWIFT institution block ---
    public CjInstitutionViewModel Institution { get; set; } = new();
    public CjInstitutionViewModel IntermediaryInstitution { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        var rail = Rail ?? "";
        var isSwift = rail.Equals("swift", StringComparison.OrdinalIgnoreCase);
        var isUk = rail is "fps" or "chaps" or "chapsCrossScheme";

        // SWIFT-specific requisites (institution).
        if (isSwift)
        {
            if (string.IsNullOrWhiteSpace(Institution.BankSwiftCode))
                yield return new ValidationResult("SWIFT BIC is required for SWIFT payouts.", new[] { "Institution.BankSwiftCode" });
            if (string.IsNullOrWhiteSpace(Institution.BankName))
                yield return new ValidationResult("Bank name is required for SWIFT payouts.", new[] { "Institution.BankName" });
            if (string.IsNullOrWhiteSpace(Institution.Country))
                yield return new ValidationResult("Bank country is required for SWIFT payouts.", new[] { "Institution.Country" });
        }

        // LEI required for corporate payee on UK / cross-scheme / SWIFT rails.
        if (string.Equals(Payee.EntityType, "corporate", StringComparison.OrdinalIgnoreCase)
            && (isSwift || rail == "chapsCrossScheme" || isUk)
            && string.IsNullOrWhiteSpace(Payee.Lei))
        {
            yield return new ValidationResult(
                $"Legal Entity Identifier (LEI) is required for corporate payees on the {rail.ToUpperInvariant()} rail.",
                new[] { "Payee.Lei" });
        }

        // BirthPlace mandatory on SWIFT individual entities.
        if (isSwift && string.Equals(Payee.EntityType, "individual", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(Payee.BirthPlace))
        {
            yield return new ValidationResult("Place of birth is required for SWIFT individual payees.", new[] { "Payee.BirthPlace" });
        }

        // Currency must align with the rail.
        if (isUk && !string.Equals(Currency, "GBP", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult($"{rail.ToUpperInvariant()} only supports GBP.", new[] { nameof(Currency) });
        if ((rail == "sepa" || rail == "sepaInst") && !string.Equals(Currency, "EUR", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("SEPA rails only support EUR.", new[] { nameof(Currency) });

        // Beneficiary name must be derivable.
        if (string.IsNullOrWhiteSpace(Payee.DisplayName()))
            yield return new ValidationResult("Beneficiary name is required.", new[] { "Payee.FirstName" });
    }
}

// =============================================================================
//  Pay-in simulate  ·  (no CJ create API — webhook simulation only)
// =============================================================================
public class SimulatePayinViewModel
{
    [Required, Display(Name = "Virtual IBAN (clientOrder)")]
    public string ClientOrder { get; set; } = "";

    [Required, Display(Name = "Currency")]
    public string Currency { get; set; } = "EUR";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Amount")]
    public decimal Amount { get; set; } = 250;

    [Display(Name = "Payer name")] public string PayerName { get; set; } = "Payer Smith";
    [Display(Name = "Oper status")] public string OperStatus { get; set; } = "captured";
    [Display(Name = "Compliance status")] public string ComplianceStatus { get; set; } = "pending";
    [Display(Name = "Order status")] public string Status { get; set; } = "pending";
}

// =============================================================================
//  Wallet / account  ·  POST /v7/gate/wallets/corporate
// =============================================================================
public class CreateWalletViewModel : IValidatableObject
{
    [Required, Display(Name = "Account / customer name")]
    public string Name { get; set; } = "";

    [Required, Display(Name = "Client customer id")]
    public string ClientCustomerId { get; set; } = "";

    [Required, EmailAddress, Display(Name = "Contact email")]
    public string Email { get; set; } = "";

    [Required, Display(Name = "Country")]
    public string Country { get; set; } = "GB";

    [Required, Display(Name = "Wallet structure")]
    public string Type { get; set; } = "corporate";

    [Required, Display(Name = "Wallet currency")]
    public string Currency { get; set; } = "EUR";

    [Required, Display(Name = "IBAN country")]
    public string IbanCountry { get; set; } = "GB";

    [Required, Display(Name = "IBAN pool group")]
    public string IbansGroup { get; set; } = "DEFAULT";

    [Range(0, double.MaxValue), Display(Name = "Opening balance (simulated)")]
    public decimal OpeningBalance { get; set; } = 0;

    /// <summary>Corporate holder KYC pack — registrationNumber, incorporationCountry, LEI, address.</summary>
    public CjPartyViewModel Holder { get; set; } = new() { EntityType = "corporate" };

    [Display(Name = "Industry / sector")] public string? IndustryType { get; set; }
    [Range(0, double.MaxValue), Display(Name = "Expected annual turnover (EUR)")] public decimal? ExpectedTurnover { get; set; }

    /// <summary>Simplified UBO list (0+ rows). Real CJ wallet creation expects full UBO declarations.</summary>
    public List<UboViewModel> Ubos { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        var h = Holder;
        if (string.IsNullOrWhiteSpace(h.Street))
            yield return new ValidationResult("Holder street is required.", new[] { "Holder.Street" });
        if (string.IsNullOrWhiteSpace(h.City))
            yield return new ValidationResult("Holder city is required.", new[] { "Holder.City" });
        if (string.IsNullOrWhiteSpace(h.Zip))
            yield return new ValidationResult("Holder zip is required.", new[] { "Holder.Zip" });
        if (string.IsNullOrWhiteSpace(h.Country))
            yield return new ValidationResult("Holder country is required.", new[] { "Holder.Country" });

        // LEI required for GB corporate wallets (CJ onboarding convention for UK schemes).
        if (string.Equals(Country, "GB", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(h.Lei))
        {
            yield return new ValidationResult("LEI is required for UK corporate wallets.", new[] { "Holder.Lei" });
        }
    }
}

public class UboViewModel
{
    [Display(Name = "Full name")] public string? Name { get; set; }
    [DataType(DataType.Date), Display(Name = "Date of birth")] public DateTime? BirthDate { get; set; }
    [Display(Name = "Country")] public string? Country { get; set; }
    [Range(0, 100), Display(Name = "Ownership %")] public decimal? OwnershipPercent { get; set; }
}

// =============================================================================
//  Wallet transfer  ·  POST /v7/gate/wallets/transfer
// =============================================================================
public class WalletTransferViewModel : IValidatableObject
{
    [Required, Display(Name = "From wallet")] public string FromWalletUuid { get; set; } = "";
    [Required, Display(Name = "To wallet")] public string ToWalletUuid { get; set; } = "";
    [Required, Display(Name = "Currency")] public string Currency { get; set; } = "EUR";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Amount")]
    public decimal Amount { get; set; } = 100;

    [Required, Display(Name = "Reference")] public string Reference { get; set; } = "Internal transfer";

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (!string.IsNullOrEmpty(FromWalletUuid)
            && string.Equals(FromWalletUuid, ToWalletUuid, StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("From and To wallets must differ.", new[] { nameof(ToWalletUuid) });
    }
}

// =============================================================================
//  Card pay-in  ·  POST /v7/gate/invoice/creditCard
// =============================================================================
public class CardPayinViewModel
{
    [Required, Display(Name = "Cardholder name")]
    public string PayerName { get; set; } = "";

    [Required, EmailAddress, Display(Name = "Cardholder email")]
    public string PayerEmail { get; set; } = "";

    [Required, Display(Name = "Product / description")]
    public string ProductName { get; set; } = "Top-up";

    [Required, Url, Display(Name = "Merchant URL")]
    public string SiteAddress { get; set; } = "https://example.com";

    [Required, Url, Display(Name = "Success redirect URL")]
    public string SuccessUrl { get; set; } = "https://example.com/success";

    [Required, Url, Display(Name = "Failure redirect URL")]
    public string FailUrl { get; set; } = "https://example.com/fail";

    [Required, Display(Name = "Currency")]
    public string Currency { get; set; } = "EUR";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Amount")]
    public decimal Amount { get; set; } = 50;
}

// =============================================================================
//  Refund  ·  POST /v7/gate/refund   (no `direction` field on CJ side)
// =============================================================================
public class CreateRefundViewModel
{
    /// <summary>UI-only hint that filters the suggested originals list. Not sent to CJ.</summary>
    [Display(Name = "Refund kind")]
    public string Direction { get; set; } = "outgoing";

    [Required, Display(Name = "Original clientOrder")]
    public string OriginalClientOrder { get; set; } = "";

    [Required, StringLength(140), Display(Name = "Description / reason")]
    public string Reason { get; set; } = "Customer request";

    [Required, Display(Name = "Currency")] public string Currency { get; set; } = "EUR";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Amount")]
    public decimal Amount { get; set; } = 0;
}

// =============================================================================
//  Compliance · CoP / IBAN check
// =============================================================================
public class RequisiteCheckViewModel : IValidatableObject
{
    [Required, Display(Name = "Check kind")]
    public string Kind { get; set; } = "cop";

    [CjRequiredIf("Kind", "cop"), Display(Name = "Account holder name")]
    public string? Name { get; set; }

    [CjRequiredIf("Kind", "cop"), Display(Name = "Sort code (UK 6-digit)")]
    public string? SortCode { get; set; }

    [CjRequiredIf("Kind", "cop"), Display(Name = "Account number (UK 8-digit)")]
    public string? AccountNumber { get; set; }

    [Display(Name = "Secondary reference (building society roll)")]
    public string? SecondaryReferenceData { get; set; }

    [CjRequiredIf("Kind", "iban"), Display(Name = "IBAN")]
    public string? Iban { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (string.Equals(Kind, "iban", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(Iban) && Iban.Length < 15)
        {
            yield return new ValidationResult("IBAN appears too short to be valid.", new[] { nameof(Iban) });
        }
    }
}

// =============================================================================
//  Transaction report  ·  POST /v7/gate/reports/transactionReport
// =============================================================================
public class TransactionReportViewModel : IValidatableObject
{
    [Required, Display(Name = "From")]
    [DataType(DataType.Date)]
    public DateTime From { get; set; } = DateTime.UtcNow.Date.AddDays(-7);

    [Required, Display(Name = "To")]
    [DataType(DataType.Date)]
    public DateTime To { get; set; } = DateTime.UtcNow.Date;

    [Required, Display(Name = "Wallet UUID")]
    public string WalletUuid { get; set; } = "";

    [Display(Name = "Transaction type")]
    public string Type { get; set; } = "all";

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (To < From)
            yield return new ValidationResult("'To' must be on or after 'From'.", new[] { nameof(To) });
        if ((To - From).TotalDays > 366)
            yield return new ValidationResult("Range cannot exceed 366 days.", new[] { nameof(To) });
    }
}
