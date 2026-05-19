using System.ComponentModel.DataAnnotations;

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

public class CreateIbanViewModel
{
    [Required, Display(Name = "Customer name")]
    public string CustomerName { get; set; } = "";

    [Required, Display(Name = "Client customer id")]
    public string ClientCustomerId { get; set; } = "";

    [Required, Display(Name = "Email")]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required, Display(Name = "Country (2-letter)")]
    [StringLength(2, MinimumLength = 2)]
    public string Country { get; set; } = "GB";

    [Required, Display(Name = "IBAN country")]
    public string IbanCountry { get; set; } = "GB";

    [Display(Name = "IBANs group")]
    public string IbansGroup { get; set; } = "DEFAULT";

    [Display(Name = "Date of birth")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; } = new DateTime(1990, 1, 1);

    [Display(Name = "City")] public string City { get; set; } = "";
    [Display(Name = "Street")] public string Street { get; set; } = "";
    [Display(Name = "ZIP")] public string Zip { get; set; } = "";
}

public class FxQuoteViewModel
{
    [Required] public string SellCurrency { get; set; } = "EUR";
    [Required] public string BuyCurrency { get; set; } = "USD";

    [Range(0.01, double.MaxValue)]
    public decimal SellAmount { get; set; } = 1000;

    public string? RateUuid { get; set; }
    public string? Rate { get; set; }
    public decimal? BuyAmount { get; set; }
    public string? ExpirationTimestamp { get; set; }
}

public class CreatePayoutViewModel
{
    [Required, Display(Name = "Rail")]
    public string Rail { get; set; } = "sepa";

    [Required, Display(Name = "Currency")]
    public string Currency { get; set; } = "EUR";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Amount")]
    public decimal Amount { get; set; } = 100;

    [Required, Display(Name = "Reference / description")]
    public string Description { get; set; } = "Payment";

    [Display(Name = "Payer wallet UUID")] public string PayerWalletUuid { get; set; } = "";
    [Display(Name = "Payer customer id")] public string PayerClientCustomerId { get; set; } = "";
    [Display(Name = "Payer IBAN")] public string PayerIban { get; set; } = "";

    [Required, Display(Name = "Payee name")]
    public string PayeeName { get; set; } = "";

    [Display(Name = "Payee email")] public string PayeeEmail { get; set; } = "";
    [Display(Name = "Payee country (2-letter)")] public string PayeeCountry { get; set; } = "DE";

    [Display(Name = "Payee IBAN / account")] public string PayeeIban { get; set; } = "";
    [Display(Name = "Payee account number (FPS)")] public string? PayeeAccountNumber { get; set; }
    [Display(Name = "Payee sort code (FPS)")] public string? PayeeSortCode { get; set; }

    [Display(Name = "Payee bank SWIFT/BIC")] public string? PayeeBankSwift { get; set; }
    [Display(Name = "Payee bank name (SWIFT)")] public string? PayeeBankName { get; set; }
    [Display(Name = "Intermediary bank BIC (SWIFT)")] public string? IntermediaryBankSwift { get; set; }
    [Display(Name = "Intermediary bank name (SWIFT)")] public string? IntermediaryBankName { get; set; }
    [Display(Name = "Clearing system code (SWIFT, e.g. ABA)")] public string? ClearingSystemIdCode { get; set; }
    [Display(Name = "Clearing member id (SWIFT)")] public string? ClearingMemberId { get; set; }

    [Display(Name = "Purpose code")] public string PurposeCode { get; set; } = "INTP";
    [Display(Name = "Purpose category")] public string PurposeCategory { get; set; } = "GP2P";
}

public class SimulatePayinViewModel
{
    [Required] public string ClientOrder { get; set; } = "";
    [Required] public string Currency { get; set; } = "EUR";

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; } = 250;

    public string PayerName { get; set; } = "Payer Smith";
    public string OperStatus { get; set; } = "captured";
    public string ComplianceStatus { get; set; } = "pending";
    public string Status { get; set; } = "pending";
}

public class CreateWalletViewModel
{
    [Required, Display(Name = "Account / customer name")]
    public string Name { get; set; } = "";

    [Required, Display(Name = "Client customer id")]
    public string ClientCustomerId { get; set; } = "";

    [Required, EmailAddress, Display(Name = "Contact email")]
    public string Email { get; set; } = "";

    [Required, Display(Name = "Country (ISO-2)"), StringLength(2, MinimumLength = 2)]
    public string Country { get; set; } = "GB";

    [Required, Display(Name = "Account type")]
    public string Type { get; set; } = "corporate";

    [Required, Display(Name = "Wallet currency")]
    public string Currency { get; set; } = "EUR";

    [Range(0, double.MaxValue), Display(Name = "Opening balance (simulated)")]
    public decimal OpeningBalance { get; set; } = 0;
}

public class WalletTransferViewModel
{
    [Required, Display(Name = "From wallet")]
    public string FromWalletUuid { get; set; } = "";

    [Required, Display(Name = "To wallet")]
    public string ToWalletUuid { get; set; } = "";

    [Required, Display(Name = "Currency")]
    public string Currency { get; set; } = "EUR";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Amount")]
    public decimal Amount { get; set; } = 100;

    [Display(Name = "Reference")]
    public string Reference { get; set; } = "Internal transfer";
}

public class CardPayinViewModel
{
    [Required, Display(Name = "Payer name")]
    public string PayerName { get; set; } = "";

    [Required, EmailAddress, Display(Name = "Payer email")]
    public string PayerEmail { get; set; } = "";

    [Required, Display(Name = "Product / description")]
    public string ProductName { get; set; } = "Top-up";

    [Required, Display(Name = "Site / merchant URL")]
    public string SiteAddress { get; set; } = "https://example.com";

    [Required, Display(Name = "Currency")]
    public string Currency { get; set; } = "EUR";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Amount")]
    public decimal Amount { get; set; } = 50;
}

public class CreateRefundViewModel
{
    [Required, Display(Name = "Direction")]
    public string Direction { get; set; } = "outgoing";

    [Required, Display(Name = "Original client order")]
    public string OriginalClientOrder { get; set; } = "";

    [Required, Display(Name = "Currency")]
    public string Currency { get; set; } = "EUR";

    [Required, Range(0.01, double.MaxValue), Display(Name = "Amount")]
    public decimal Amount { get; set; } = 0;

    [Required, Display(Name = "Reason")]
    public string Reason { get; set; } = "Customer request";
}

public class RequisiteCheckViewModel
{
    [Required, Display(Name = "Check kind")]
    public string Kind { get; set; } = "cop";

    [Required, Display(Name = "Account holder name (CoP)")]
    public string Name { get; set; } = "";

    [Display(Name = "Sort code (CoP)")]
    public string? SortCode { get; set; }

    [Display(Name = "Account number (CoP)")]
    public string? AccountNumber { get; set; }

    [Display(Name = "IBAN (SEPA IBAN check)")]
    public string? Iban { get; set; }
}

public class TransactionReportViewModel
{
    [Required, Display(Name = "From date")]
    [DataType(DataType.Date)]
    public DateTime From { get; set; } = DateTime.UtcNow.Date.AddDays(-7);

    [Required, Display(Name = "To date")]
    [DataType(DataType.Date)]
    public DateTime To { get; set; } = DateTime.UtcNow.Date;

    [Display(Name = "Wallet UUID (optional)")]
    public string? WalletUuid { get; set; }

    [Display(Name = "Transaction type")]
    public string Type { get; set; } = "all";
}
