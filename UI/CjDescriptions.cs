namespace BankTransferMVC.UI;

public sealed class CjFeatureIntro
{
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public string? Endpoint { get; init; }
    public string? Flow { get; init; }
}

public sealed class CjFieldHelp
{
    public required string Description { get; init; }
    public string? Sample { get; init; }
    public string? Required { get; init; }
    public string? Scope { get; init; }

    public string Html =>
        $"<p class=\"form-text cj-desc mb-1\">{Description}</p>" +
        (string.IsNullOrEmpty(Scope) ? "" : $"<p class=\"form-text cj-scope mb-1\"><strong>Scope:</strong> {Scope}</p>") +
        (string.IsNullOrEmpty(Sample) ? "" : $"<code class=\"cj-sample\">Sample: {Sample}</code>") +
        (string.IsNullOrEmpty(Required) ? "" : $" <span class=\"cj-req badge text-bg-secondary\">{Required}</span>");
}

public static class CjDescriptions
{
    public static CjFieldHelp Get(string key) =>
        Fields.TryGetValue(key, out var h) ? h : new CjFieldHelp { Description = "" };

    public static readonly Dictionary<string, CjFeatureIntro> Features = new()
    {
        ["dashboard"] = new()
        {
            Title = "Clear Junction operations dashboard",
            Summary = "Overview of virtual IBANs, pay-ins, FX conversions, and pay-outs. All flows use API v7 with clientOrder idempotency and webhook notifications.",
            Flow = "Allocate vIBAN → receive pay-in → optional FX → create payout → track status via webhooks."
        },
        ["virtualIban"] = new()
        {
            Title = "Virtual IBAN allocation",
            Summary = "Issue a customer-specific IBAN linked to a CJ wallet. Payers send bank transfers to this IBAN; funds arrive as payinNotification webhooks.",
            Endpoint = "POST /v7/gate/allocate/v3/create/iban",
            Flow = "Submit registrant KYC → status accepted → IBAN delivered via notification or status poll."
        },
        ["virtualIbanCreate"] = new()
        {
            Title = "Virtual IBAN allocation",
            Summary = "Issue a customer-specific IBAN linked to a CJ wallet. Payers send bank transfers to this IBAN; funds arrive as payinNotification webhooks.",
            Endpoint = "POST /v7/gate/allocate/v3/create/iban",
            Flow = "Submit registrant KYC → status accepted → IBAN delivered via notification or status poll."
        },
        ["payin"] = new()
        {
            Title = "Pay-in events",
            Summary = "Inbound funds credited to your wallet. Bank pay-ins arrive via vIBAN without an API create call; card pay-ins use POST /v7/gate/invoice/creditCard.",
            Endpoint = "Webhook: payinNotification",
            Flow = "Payer sends transfer → CJ posts notification → credit your ledger on operStatus captured + compliance approved."
        },
        ["payinSimulate"] = new()
        {
            Title = "Simulate pay-in notification",
            Summary = "Generates the same JSON payload Clear Junction POSTs to your postbackUrl after a real bank credit. Use for local testing without sandbox credentials.",
            Endpoint = "POST /webhooks/cj/payin (your app)",
            Flow = "Select vIBAN → set amount → creates payinNotification-shaped event in the event log."
        },
        ["fx"] = new()
        {
            Title = "Instant FX — quote and convert",
            Summary = "Convert balance between currency positions before a cross-currency payout. Two-step: lock a rate, then execute with rateUuid.",
            Endpoint = "POST /v7/gate/fx/instant/rate → POST /v7/gate/fx/instant/transfer",
            Flow = "Get quote (expires ~5 min) → execute transfer → funds available in buy currency wallet."
        },
        ["fxHistory"] = new()
        {
            Title = "FX conversion history",
            Summary = "Past instant FX trades with sell/buy amounts, locked rate, and settlement status.",
            Endpoint = "GET /v7/gate/fx/instant/status/clientOrder/{id}"
        },
        ["payout"] = new()
        {
            Title = "Pay-out execution",
            Summary = "Send funds from your CJ balance to a beneficiary account. One API endpoint per payment rail (SEPA, FPS, CHAPS, SWIFT, internal).",
            Endpoint = "POST /v7/gate/payout/bankTransfer/{rail}",
            Flow = "Submit payout → status created → operStatus pending → compliance check → rail settlement → payoutNotification webhook."
        },
        ["payoutCreate"] = new()
        {
            Title = "Pay-out execution",
            Summary = "Send funds from your CJ balance to a beneficiary account. One API endpoint per payment rail (SEPA, FPS, CHAPS, SWIFT, internal).",
            Endpoint = "POST /v7/gate/payout/bankTransfer/{rail}",
            Flow = "Submit payout → status created → operStatus pending → compliance check → rail settlement → payoutNotification webhook."
        },
        ["payoutDetails"] = new()
        {
            Title = "Payout order details",
            Summary = "Track operational and compliance sub-statuses. Refresh polls CJ or advances simulation. Webhook events appear in the lifecycle timeline.",
            Endpoint = "GET /v7/gate/status/payout/clientOrder/{id}"
        },
        ["accounts"] = new()
        {
            Title = "Accounts & wallets",
            Summary = "CJ wallets hold segregated balances per currency. Create a corporate wallet to reserve a balance position, view balances, run statements, or move funds between wallets.",
            Endpoint = "POST /v7/gate/wallets/corporate · GET /v7/gate/wallets/{uuid} · POST /v7/gate/wallets/statement",
            Flow = "Create wallet → fund via pay-in/FX → execute payouts → reconcile via statement."
        },
        ["accountsCreate"] = new()
        {
            Title = "Open a corporate wallet",
            Summary = "Reserves a CJ wallet for a corporate customer. Returns a walletUuid used on every subsequent pay-in, payout, and FX API call.",
            Endpoint = "POST /v7/gate/wallets/corporate"
        },
        ["walletTransfer"] = new()
        {
            Title = "Internal wallet transfer",
            Summary = "Move funds between two of your CJ wallets in the same currency — used for internal treasury rebalancing.",
            Endpoint = "POST /v7/gate/wallets/transfer"
        },
        ["walletStatement"] = new()
        {
            Title = "Wallet statement",
            Summary = "Movements (pay-in, payout, refund, FX, transfer) recorded against the selected wallet — used for reconciliation against your ledger.",
            Endpoint = "POST /v7/gate/wallets/statement"
        },
        ["cardPayin"] = new()
        {
            Title = "Card pay-in (hosted checkout)",
            Summary = "Issue a CJ card invoice and redirect the payer to a hosted card form. CJ posts the result back via invoice webhook on capture/decline.",
            Endpoint = "POST /v7/gate/invoice/creditCard",
            Flow = "Create invoice → redirect payer → CJ captures card → webhook + GET /v7/gate/status/invoice/clientOrder/{id}."
        },
        ["refund"] = new()
        {
            Title = "Refunds (incoming + outgoing)",
            Summary = "Reverse a previously settled payment. Outgoing refund returns a payout you sent; incoming refund returns a pay-in you received.",
            Endpoint = "/v7/gate/refund"
        },
        ["refundCreate"] = new()
        {
            Title = "Issue a refund",
            Summary = "Select the original order to reverse. CJ creates a linked refund order; status is tracked via the same compliance + operational pipeline as the original.",
            Endpoint = "POST /v7/gate/refund"
        },
        ["compliance"] = new()
        {
            Title = "Compliance & beneficiary checks",
            Summary = "Pre-flight checks before sending funds: UK Confirmation of Payee (CoP) verifies that the name matches the account, and SEPA IBAN check confirms the IBAN is reachable.",
            Endpoint = "POST /v7/gate/checkRequisite/cop · GET /v7/gate/checkRequisite/bankTransfer/eu/iban/{iban}"
        },
        ["approvals"] = new()
        {
            Title = "Maker-checker approvals",
            Summary = "Manually approve or cancel payouts awaiting compliance or operational sign-off. Mirrors the CJ dashboard maker-checker workflow.",
            Endpoint = "POST /v7/gate/transactionAction/approve · POST /v7/gate/transactionAction/cancel"
        },
        ["report"] = new()
        {
            Title = "Transaction reports",
            Summary = "Bulk export of transactions for a date range and wallet, used for daily reconciliation against your ledger.",
            Endpoint = "POST /v7/gate/reports/transactionReport"
        },
        ["events"] = new()
        {
            Title = "Inbound webhook events",
            Summary = "Every notification CJ posted to your endpoints: payinNotification, payoutNotification, ibanAllocationNotification, FX, invoice, refund. Deduplicate on messageUuid.",
            Endpoint = "POST {your}/webhooks/cj/{payin|payout|iban}"
        }
    };

    public static readonly Dictionary<string, CjFieldHelp> Fields = new(StringComparer.OrdinalIgnoreCase)
    {
        // Shared
        ["shared.clientOrder"] = F(
            "Your unique order id — idempotency key. Never reuse for different payments.",
            "999899-0005", "Mandatory",
            "Stored by CJ; use for status queries, support tickets, and reconciliation with your internal TransferId."),
        ["shared.orderReference"] = F(
            "Clear Junction UUID for this business order. Returned on create and in every webhook.",
            "12d603fa-4d0b-4fec-a9b0-cc3114da134e", "CJ-generated",
            "Primary key on CJ side; respond to webhooks with this value as plain-text body."),
        ["shared.postbackUrl"] = F(
            "HTTPS URL where CJ POSTs status notifications for this order.",
            "https://your-app.com/webhooks/cj/payout", "Recommended",
            "Overrides account-level webhook when set per order."),
        ["shared.walletUuid"] = F(
            "CJ client-money wallet that holds segregated customer balances.",
            "348e11ab-dbfb-4ae8-99e7-349b00868f6f", "Conditional",
            "Required on most payout/pay-in calls; obtained from wallet reservation or IBAN allocation."),
        ["shared.clientCustomerId"] = F(
            "Your CRM/customer reference — joins CJ records to your user database.",
            "2983ght938", "Conditional",
            "Used for vIBAN list, reconciliation, and reporting."),
        ["shared.operStatus"] = F(
            "Operational pipeline state: pending → processing → captured/settled/declined.",
            "pending", "Read-only",
            "Funds movement follows this; UI should show separately from compliance."),
        ["shared.complianceStatus"] = F(
            "AML/sanctions screening outcome: pending → approved/declined.",
            "pending", "Read-only",
            "Payment may be operatively captured but blocked until approved."),
        ["shared.currency"] = F(
            "ISO-4217 settlement currency for the instruction.",
            "EUR", "Mandatory",
            "Must match enabled corridor for the selected rail."),
        ["shared.amount"] = F(
            "Payment amount — up to 2 decimal places for fiat.",
            "210.55", "Mandatory",
            "Validated against wallet balance and per-rail limits."),

        // Virtual IBAN
        ["iban.customerName"] = F(
            "Display name of the account holder — shown on bank instructions.",
            "Julie Peterson", "Mandatory",
            "Split into firstName/lastName when sent to CJ API."),
        ["iban.clientCustomerId"] = F(
            "Your CRM/customer reference — joins CJ records to your user database.",
            "2983ght938", "Conditional",
            "Used for vIBAN list, reconciliation, and reporting."),
        ["iban.email"] = F(
            "Registrant contact email for KYC and compliance follow-up.",
            "peterson.julie@example.com", "Mandatory"),
        ["iban.country"] = F(
            "Registrant residential country — ISO-3166 alpha-2.",
            "IT", "Mandatory",
            "Drives sanctions screening geography."),
        ["iban.ibanCountry"] = F(
            "Country prefix of the IBAN to be issued (GB, DE, etc.).",
            "GB", "Mandatory",
            "Determines which domestic rails accept inbound payments to this vIBAN."),
        ["iban.ibansGroup"] = F(
            "IBAN pool assigned at CJ onboarding — usually DEFAULT.",
            "DEFAULT", "Mandatory",
            "Contact CJ if you need separate pools per product line."),
        ["iban.birthDate"] = F(
            "Date of birth for individual registrant — ISO date.",
            "1999-09-29", "Mandatory (individual)",
            "Used for PEP/sanctions screening."),
        ["iban.street"] = F("Street address including number.", "12 Tourin", "Mandatory"),
        ["iban.city"] = F("City or locality.", "Rome", "Mandatory"),
        ["iban.zip"] = F("Postal / ZIP code.", "123455", "Mandatory"),
        ["iban.iban"] = F(
            "Allocated virtual IBAN — share with payers for inbound bank transfers.",
            "GB29CLJU04130729900988", "CJ-generated",
            "Credits arrive as payinNotification webhooks to your postbackUrl."),
        ["iban.status"] = F(
            "Allocation lifecycle: pending → accepted/declined.",
            "accepted", "Read-only",
            "Poll GET status or wait for ibanNotification webhook."),

        // Pay-in simulate
        ["payin.clientOrder"] = F(
            "Links simulated pay-in to the vIBAN allocation clientOrder.",
            "IBAN-20260519120000-1234", "Mandatory",
            "Must match an allocated vIBAN so payee.walletUuid is populated."),
        ["payin.currency"] = F("ISO-4217 currency of the inbound transfer.", "EUR", "Mandatory"),
        ["payin.amount"] = F("Inbound transfer amount.", "210.55", "Mandatory"),
        ["payin.payerName"] = F(
            "Simulated originator name on the bank transfer.",
            "Payer Smith", "Optional",
            "Appears in payer object of payinNotification."),
        ["payin.status"] = F("Order-level status in the notification.", "pending", "Optional"),
        ["payin.operStatus"] = F("Operational sub-status forwarded in the simulated notification.", "captured", "Read-only"),
        ["payin.complianceStatus"] = F("Compliance sub-status forwarded in the simulated notification.", "approved", "Read-only"),

        // FX
        ["fx.sellCurrency"] = F(
            "Currency you sell from your wallet position.",
            "EUR", "Mandatory",
            "Pair must be supported and tradable during UK business hours (~06:00–15:00)."),
        ["fx.buyCurrency"] = F(
            "Currency you will receive after conversion.",
            "USD", "Mandatory"),
        ["fx.sellAmount"] = F(
            "Amount to debit in sell currency.",
            "1000.00", "Mandatory (with buyAmount)",
            "Must align with quoted rate when executing."),
        ["fx.rateUuid"] = F(
            "Token from /fx/instant/rate — single use, expires in minutes.",
            "8a7214dc-b89d-4836-ade3-9a4a50b686e4", "Mandatory on execute",
            "Pass to Create Transfer immediately after Get Rate."),
        ["fx.buyAmount"] = F(
            "Computed receive amount = sellAmount × quote.",
            "1181.00", "Mandatory on execute",
            "Mismatch triggers error 010 Invalid target amount."),

        // Payout
        ["payout.rail"] = F(
            "Payment scheme determining API path and payeeRequisite shape.",
            "sepa", "Mandatory",
            "sepa=EU IBAN, fps=UK sort+account, swift=international BIC+institution."),
        ["payout.currency"] = F("ISO-4217 currency for the outbound payment.", "EUR", "Mandatory",
            "Must match the rail (e.g. EUR for SEPA, GBP for FPS)."),
        ["payout.amount"] = F("Payment amount — validated against wallet balance and rail limits.", "210.55", "Mandatory"),
        ["payout.description"] = F(
            "Remittance information — appears on beneficiary bank statement.",
            "Birthday present", "Mandatory",
            "Keep within scheme character limits (often 140 chars SEPA)."),
        ["payout.purposeCode"] = F(
            "ISO 20022 purpose-of-payment code.",
            "INTP", "Conditional",
            "Required for many cross-border and remittance corridors."),
        ["payout.purposeCategory"] = F(
            "Purpose category e.g. GP2P (general P2P).",
            "GP2P", "Conditional"),
        ["payout.payerWalletUuid"] = F(
            "CJ wallet that funds this payout — leave blank to use the default account wallet.",
            "348e11ab-dbfb-4ae8-99e7-349b00868f6f", "Conditional"),
        ["payout.payerClientCustomerId"] = F(
            "Your CRM customer id for the payer, when paying on behalf of an end customer.",
            "2983ght938", "Conditional"),
        ["payout.payerIban"] = F(
            "Source IBAN debited for the payout.",
            "GBXXCLJU04130729900988", "Conditional",
            "Identifies which CJ account position funds leave."),
        ["payout.payeeName"] = F(
            "Beneficiary account holder name — must match bank records (CoP in UK).",
            "Julie Peterson", "Mandatory"),
        ["payout.payeeEmail"] = F("Beneficiary email for compliance.", "peterson.julie@example.com", "Optional"),
        ["payout.payeeCountry"] = F(
            "Beneficiary country ISO-3166 — used in address and SWIFT routing.",
            "DE", "Mandatory"),
        ["payout.payeeIban"] = F(
            "Beneficiary IBAN (SEPA/SWIFT) or account identifier.",
            "DE89370400440532013000", "Rail-dependent",
            "Mandatory for SEPA and most SWIFT corridors."),
        ["payout.payeeAccountNumber"] = F(
            "UK 8-digit account number — required for FPS/CHAPS.",
            "11111111", "FPS/CHAPS",
            "Use with sortCode; validate via Confirmation of Payee before live FPS."),
        ["payout.payeeSortCode"] = F(
            "UK 6-digit sort code — required for FPS/CHAPS.",
            "000000", "FPS/CHAPS"),
        ["payout.payeeBankSwift"] = F(
            "Beneficiary bank BIC/SWIFT code (8 or 11 characters).",
            "UBSWCHZH80A", "SWIFT / optional SEPA",
            "Identifies destination financial institution."),
        ["payout.payeeBankName"] = F(
            "Beneficiary bank legal name for SWIFT MT103.",
            "Bank of America", "SWIFT"),
        ["payout.intermediaryBankSwift"] = F(
            "Correspondent bank BIC when required by destination country.",
            "CHASUS33", "SWIFT conditional",
            "Needed for some US/corridor payments without direct BIC routing."),
        ["payout.intermediaryBankName"] = F("Intermediary institution name.", "JPMorgan Chase", "SWIFT optional"),
        ["payout.clearingSystemIdCode"] = F(
            "Local clearing system e.g. ABA for US wires.",
            "ABA", "SWIFT (US)",
            "Used with memberId for non-IBAN countries."),
        ["payout.clearingMemberId"] = F(
            "Clearing member id e.g. US ABA routing number.",
            "021000021", "SWIFT (US)",
            "Must match institution clearing system."),

        // Wallets / accounts
        ["wallet.name"] = F(
            "Legal name of the corporate customer holding the wallet.",
            "Acme Ltd", "Mandatory",
            "Used on KYC, statements, and outgoing payment instructions."),
        ["wallet.email"] = F(
            "Operations contact email for the wallet owner.",
            "ops@acme.com", "Mandatory"),
        ["wallet.country"] = F("Wallet owner country (ISO-2).", "GB", "Mandatory"),
        ["wallet.type"] = F(
            "CJ account structure: current, collection, correspondent, corporate.",
            "corporate", "Mandatory",
            "Determines reporting, segregation, and fee treatment."),
        ["wallet.currency"] = F(
            "Wallet base currency — one wallet per currency position.",
            "EUR", "Mandatory",
            "FX moves balance between wallets of different currencies."),
        ["wallet.openingBalance"] = F(
            "Simulated starting balance for this PoC; in production funds arrive via pay-in/FX.",
            "1000.00", "Simulation only"),
        ["walletTransfer.from"] = F(
            "Source wallet — must hold sufficient balance in the chosen currency.",
            "<walletUuid>", "Mandatory"),
        ["walletTransfer.to"] = F(
            "Destination wallet for the internal movement.",
            "<walletUuid>", "Mandatory",
            "Both wallets must belong to the same CJ client and currency."),

        // Card pay-in
        ["cardPayin.payerName"] = F("Cardholder name as it appears on the card.", "John Doe", "Mandatory"),
        ["cardPayin.payerEmail"] = F(
            "Email for 3-D Secure and invoice receipt.",
            "payer@example.com", "Mandatory",
            "Also used for fraud screening."),
        ["cardPayin.productName"] = F(
            "Short merchant product / service description shown in the hosted checkout.",
            "Wallet top-up", "Mandatory"),
        ["cardPayin.siteAddress"] = F(
            "Merchant URL stored on the invoice — used for compliance and chargeback evidence.",
            "https://merchant.example.com", "Mandatory"),

        // Refund
        ["refund.direction"] = F(
            "outgoing = reverse a payout we sent; incoming = return a pay-in we received.",
            "outgoing", "Mandatory"),
        ["refund.originalClientOrder"] = F(
            "ClientOrder of the original payout or pay-in being reversed.",
            "PAY-20260519120001-3431", "Mandatory",
            "Refund inherits currency and beneficiary requisite from the original order."),
        ["refund.reason"] = F(
            "Short reason recorded with the refund — visible in CJ dashboard and webhooks.",
            "Customer request", "Mandatory"),

        // Compliance / requisite checks
        ["check.kind"] = F(
            "cop = UK Confirmation of Payee · iban = SEPA IBAN reachability.",
            "cop", "Mandatory"),
        ["check.copName"] = F(
            "Name to verify against the account holder on file at the destination bank.",
            "Julie Peterson", "CoP",
            "Returned as match / closeMatch / noMatch."),
        ["check.iban"] = F(
            "IBAN to verify for SEPA reachability before submitting a SEPA payout.",
            "DE89370400440532013000", "SEPA"),
    };

    static CjFieldHelp F(string desc, string? sample = null, string? required = null, string? scope = null) =>
        new() { Description = desc, Sample = sample, Required = required, Scope = scope };
}
