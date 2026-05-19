namespace BankTransferMVC.Models;

public class VirtualIbanRecord
{
    public string ClientOrder { get; set; } = "";
    public string OrderReference { get; set; } = "";
    public string Iban { get; set; } = "";
    public string Country { get; set; } = "";
    public string WalletUuid { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string ClientCustomerId { get; set; } = "";
    public string Status { get; set; } = "accepted";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PayoutRecord
{
    public string ClientOrder { get; set; } = "";
    public string OrderReference { get; set; } = "";
    public string Rail { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal Amount { get; set; }
    public string PayeeName { get; set; } = "";
    public string PayeeAccount { get; set; } = "";
    public string PayeeCountry { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "created";
    public string OperStatus { get; set; } = "pending";
    public string ComplianceStatus { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class FxRecord
{
    public string ClientOrder { get; set; } = "";
    public string OrderReference { get; set; } = "";
    public string SellCurrency { get; set; } = "";
    public decimal SellAmount { get; set; }
    public string BuyCurrency { get; set; } = "";
    public decimal BuyAmount { get; set; }
    public string Rate { get; set; } = "";
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class WebhookEvent
{
    public string Type { get; set; } = "";
    public string ClientOrder { get; set; } = "";
    public string OrderReference { get; set; } = "";
    public string Status { get; set; } = "";
    public string OperStatus { get; set; } = "";
    public string ComplianceStatus { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal Amount { get; set; }
    public string Payload { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class WalletRecord
{
    public string WalletUuid { get; set; } = "";
    public string Name { get; set; } = "";
    public string ClientCustomerId { get; set; } = "";
    public string Email { get; set; } = "";
    public string Country { get; set; } = "GB";
    public string Type { get; set; } = "corporate";
    public string Currency { get; set; } = "EUR";
    public decimal Balance { get; set; }
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class RefundRecord
{
    public string ClientOrder { get; set; } = "";
    public string OrderReference { get; set; } = "";
    public string Direction { get; set; } = "outgoing";
    public string OriginalClientOrder { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "created";
    public string OperStatus { get; set; } = "pending";
    public string ComplianceStatus { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class CardPayinRecord
{
    public string ClientOrder { get; set; } = "";
    public string OrderReference { get; set; } = "";
    public string PayerName { get; set; } = "";
    public string PayerEmail { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string SiteAddress { get; set; } = "";
    public string Currency { get; set; } = "EUR";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
    public string OperStatus { get; set; } = "pending";
    public string ComplianceStatus { get; set; } = "pending";
    public string RedirectUrl { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class RequisiteCheckRecord
{
    public string Kind { get; set; } = "";
    public string Input { get; set; } = "";
    public string Result { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
}
