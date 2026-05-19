namespace BankTransferMVC.Services;

public sealed record CjLookupItem(string Code, string Label, string? Group = null);

internal static class CjReferenceData
{
    // ISO-4217 currencies covered by Clear Junction multi-currency capability.
    // Source: clearjunction.com SWIFT expansion notes + Apiary fx + multi-currency docs.
    public static readonly CjLookupItem[] Currencies =
    {
        new("EUR", "Euro", "EU"),
        new("GBP", "British Pound", "EU"),
        new("CHF", "Swiss Franc", "EU"),
        new("NOK", "Norwegian Krone", "EU"),
        new("SEK", "Swedish Krona", "EU"),
        new("DKK", "Danish Krone", "EU"),
        new("PLN", "Polish Zloty", "EU"),
        new("CZK", "Czech Koruna", "EU"),
        new("HUF", "Hungarian Forint", "EU"),
        new("RON", "Romanian Leu", "EU"),
        new("BGN", "Bulgarian Lev", "EU"),
        new("HRK", "Croatian Kuna", "EU"),
        new("ISK", "Icelandic Krona", "EU"),

        new("USD", "US Dollar", "Americas"),
        new("CAD", "Canadian Dollar", "Americas"),
        new("MXN", "Mexican Peso", "Americas"),
        new("BRL", "Brazilian Real", "Americas"),

        new("AUD", "Australian Dollar", "APAC"),
        new("NZD", "New Zealand Dollar", "APAC"),
        new("JPY", "Japanese Yen", "APAC"),
        new("SGD", "Singapore Dollar", "APAC"),
        new("HKD", "Hong Kong Dollar", "APAC"),
        new("CNY", "Chinese Yuan", "APAC"),
        new("INR", "Indian Rupee", "APAC"),
        new("THB", "Thai Baht", "APAC"),
        new("PHP", "Philippine Peso", "APAC"),
        new("IDR", "Indonesian Rupiah", "APAC"),
        new("MYR", "Malaysian Ringgit", "APAC"),
        new("KRW", "South Korean Won", "APAC"),

        new("TRY", "Turkish Lira", "MEA"),
        new("ILS", "Israeli Shekel", "MEA"),
        new("AED", "UAE Dirham", "MEA"),
        new("SAR", "Saudi Riyal", "MEA"),
        new("QAR", "Qatari Riyal", "MEA"),
        new("KWD", "Kuwaiti Dinar", "MEA"),
        new("BHD", "Bahraini Dinar", "MEA"),
        new("ZAR", "South African Rand", "MEA"),
        new("EGP", "Egyptian Pound", "MEA"),
        new("NGN", "Nigerian Naira", "MEA"),
        new("KES", "Kenyan Shilling", "MEA")
    };

    // ISO-3166 alpha-2 — common subset for SEPA, SWIFT, and non-EEA corridors.
    // Grouped by region for nicer UX.
    public static readonly CjLookupItem[] Countries =
    {
        new("AT", "Austria", "EEA"),
        new("BE", "Belgium", "EEA"),
        new("BG", "Bulgaria", "EEA"),
        new("CY", "Cyprus", "EEA"),
        new("CZ", "Czechia", "EEA"),
        new("DE", "Germany", "EEA"),
        new("DK", "Denmark", "EEA"),
        new("EE", "Estonia", "EEA"),
        new("ES", "Spain", "EEA"),
        new("FI", "Finland", "EEA"),
        new("FR", "France", "EEA"),
        new("GR", "Greece", "EEA"),
        new("HR", "Croatia", "EEA"),
        new("HU", "Hungary", "EEA"),
        new("IE", "Ireland", "EEA"),
        new("IS", "Iceland", "EEA"),
        new("IT", "Italy", "EEA"),
        new("LI", "Liechtenstein", "EEA"),
        new("LT", "Lithuania", "EEA"),
        new("LU", "Luxembourg", "EEA"),
        new("LV", "Latvia", "EEA"),
        new("MT", "Malta", "EEA"),
        new("NL", "Netherlands", "EEA"),
        new("NO", "Norway", "EEA"),
        new("PL", "Poland", "EEA"),
        new("PT", "Portugal", "EEA"),
        new("RO", "Romania", "EEA"),
        new("SE", "Sweden", "EEA"),
        new("SI", "Slovenia", "EEA"),
        new("SK", "Slovakia", "EEA"),

        new("GB", "United Kingdom", "UK"),
        new("JE", "Jersey", "UK"),
        new("GG", "Guernsey", "UK"),
        new("IM", "Isle of Man", "UK"),

        new("CH", "Switzerland", "Other Europe"),
        new("AL", "Albania", "Other Europe"),
        new("BA", "Bosnia and Herzegovina", "Other Europe"),
        new("MD", "Moldova", "Other Europe"),
        new("ME", "Montenegro", "Other Europe"),
        new("MK", "North Macedonia", "Other Europe"),
        new("RS", "Serbia", "Other Europe"),
        new("TR", "Turkey", "Other Europe"),
        new("UA", "Ukraine", "Other Europe"),

        new("US", "United States", "Americas"),
        new("CA", "Canada", "Americas"),
        new("MX", "Mexico", "Americas"),
        new("BR", "Brazil", "Americas"),
        new("AR", "Argentina", "Americas"),
        new("CL", "Chile", "Americas"),
        new("CO", "Colombia", "Americas"),
        new("PE", "Peru", "Americas"),

        new("AU", "Australia", "APAC"),
        new("NZ", "New Zealand", "APAC"),
        new("JP", "Japan", "APAC"),
        new("CN", "China", "APAC"),
        new("HK", "Hong Kong", "APAC"),
        new("SG", "Singapore", "APAC"),
        new("KR", "South Korea", "APAC"),
        new("IN", "India", "APAC"),
        new("ID", "Indonesia", "APAC"),
        new("MY", "Malaysia", "APAC"),
        new("PH", "Philippines", "APAC"),
        new("TH", "Thailand", "APAC"),
        new("VN", "Vietnam", "APAC"),

        new("AE", "United Arab Emirates", "MEA"),
        new("SA", "Saudi Arabia", "MEA"),
        new("QA", "Qatar", "MEA"),
        new("KW", "Kuwait", "MEA"),
        new("BH", "Bahrain", "MEA"),
        new("OM", "Oman", "MEA"),
        new("IL", "Israel", "MEA"),
        new("EG", "Egypt", "MEA"),
        new("ZA", "South Africa", "MEA"),
        new("NG", "Nigeria", "MEA"),
        new("KE", "Kenya", "MEA"),
        new("GH", "Ghana", "MEA"),
        new("MA", "Morocco", "MEA")
    };

    // ISO 20022 ExternalPurpose1Code — selection of widely-used codes
    // grouped by ExternalCategoryPurpose1Code for readability.
    public static readonly CjLookupItem[] PurposeCodes =
    {
        new("INTP", "Intra-company payment", "INTC"),
        new("SALA", "Salary payment", "SALA"),
        new("PENS", "Pension payment", "PENS"),
        new("SSBE", "Social security benefit", "SSBE"),
        new("GOVT", "Government payment", "GOVT"),
        new("TAXS", "Tax payment", "TAXS"),
        new("VATX", "Value added tax payment", "TAXS"),
        new("DIVD", "Dividend", "DIVI"),
        new("INTE", "Interest", "INTE"),

        new("GP2P", "General person-to-person", "GP2P"),
        new("GIFT", "Gift", "GP2P"),
        new("CHAR", "Charity payment", "CHAR"),
        new("TRAD", "Trade services", "TRAD"),

        new("SUPP", "Supplier payment", "SUPP"),
        new("LOAN", "Loan", "LOAN"),
        new("LOAR", "Loan repayment", "LOAN"),
        new("CBLK", "Card bulk clearing", "CBLK"),

        new("IVPT", "Invoice payment", "TRAD"),
        new("GDDS", "Purchase/sale of goods", "TRAD"),
        new("SCVE", "Purchase/sale of services", "TRAD"),
        new("FREX", "FX-related settlement", "TREA"),
        new("CASH", "Cash management transfer", "CASH"),
        new("INTC", "Intra-company transfer", "INTC"),
        new("TREA", "Treasury payment", "TREA"),

        new("ALMY", "Alimony", "GP2P"),
        new("CHAR", "Charity", "CHAR"),
        new("EDUC", "Education", "GP2P"),
        new("RENT", "Rent", "GP2P"),
        new("HLRP", "Housing loan repayment", "LOAN"),

        new("SECU", "Securities", "SECU"),
        new("OTHR", "Other", "OTHR")
    };

    // ISO 20022 ExternalCategoryPurpose1Code
    public static readonly CjLookupItem[] PurposeCategories =
    {
        new("GP2P", "General person-to-person"),
        new("SUPP", "Supplier payment"),
        new("SALA", "Salary"),
        new("PENS", "Pension"),
        new("INTC", "Intra-company"),
        new("TRAD", "Trade settlement"),
        new("TREA", "Treasury"),
        new("CASH", "Cash management"),
        new("LOAN", "Loan / loan repayment"),
        new("TAXS", "Tax"),
        new("DIVI", "Dividend"),
        new("INTE", "Interest"),
        new("GOVT", "Government"),
        new("CHAR", "Charity"),
        new("SSBE", "Social security"),
        new("SECU", "Securities"),
        new("CBLK", "Card bulk clearing"),
        new("OTHR", "Other")
    };

    public static readonly CjLookupItem[] ClearingSystems =
    {
        new("ABA", "ABA — US Fedwire/ACH routing", "Americas"),
        new("CHIPS", "CHIPS — US large-value", "Americas"),
        new("FEDWIRE", "FedWire — US RTGS", "Americas"),
        new("CHAPS", "CHAPS — UK RTGS", "UK"),
        new("BACS", "BACS — UK ACH", "UK"),
        new("FPS", "Faster Payments — UK", "UK"),
        new("SEPA", "SEPA — EU SCT", "EU"),
        new("SEPAINST", "SEPA Instant — EU", "EU"),
        new("TARGET2", "TARGET2 — EU RTGS", "EU"),
        new("STEP2", "STEP2 — EU ACH", "EU"),
        new("BSB", "BSB — Australia", "APAC"),
        new("BOJNET", "BOJ-NET — Japan", "APAC"),
        new("ZENGIN", "Zengin — Japan", "APAC"),
        new("HKICL", "HKICL — Hong Kong", "APAC"),
        new("MEPS", "MEPS+ — Singapore", "APAC"),
        new("INDIA_IFSC", "IFSC — India", "APAC")
    };

    public static readonly CjLookupItem[] DocumentTypes =
    {
        new("passport", "Passport"),
        new("idCard", "National ID card"),
        new("drivingLicence", "Driving licence")
    };

    public static readonly CjLookupItem[] WalletTypes =
    {
        new("corporate", "Corporate (segregated client money)"),
        new("collection", "Collection (pooled inbound)"),
        new("current", "Current (operational treasury)"),
        new("correspondent", "Correspondent (bank partner)")
    };

    public static readonly CjLookupItem[] AccountTypes =
    {
        new("IBAN", "IBAN — bank account"),
        new("cryptoAddress", "Crypto address")
    };

    public static readonly CjLookupItem[] AccountCategories =
    {
        new("ethereum_erc20", "Ethereum ERC-20", "Crypto"),
        new("bitcoin", "Bitcoin", "Crypto"),
        new("polygon_erc20", "Polygon ERC-20", "Crypto"),
        new("tron_trc20", "Tron TRC-20", "Crypto")
    };

    public static readonly CjLookupItem[] AmlRiskLevels =
    {
        new("Low", "Low"),
        new("Medium", "Medium"),
        new("High", "High")
    };

    public static readonly CjLookupItem[] TransactionTypes =
    {
        new("Payin", "Pay-in"),
        new("Payout", "Pay-out"),
        new("Refund", "Refund"),
        new("TransferWallet", "Wallet transfer"),
        new("FxInstant", "Instant FX"),
        new("Invoice", "Card invoice")
    };

    public static readonly CjLookupItem[] Rails =
    {
        new("sepa", "SEPA Credit Transfer (EUR)", "EU"),
        new("sepaInst", "SEPA Instant (EUR)", "EU"),
        new("fps", "UK Faster Payments (GBP)", "UK"),
        new("chaps", "UK CHAPS (GBP)", "UK"),
        new("chapsCrossScheme", "CHAPS Cross-scheme (GBP)", "UK"),
        new("swift", "Multi-currency SWIFT", "Global"),
        new("internal", "Internal (CJ wallet)", "Internal")
    };

    public static readonly CjLookupItem[] OrderStatuses =
    {
        new("created", "Created"),
        new("pending", "Pending"),
        new("accepted", "Accepted"),
        new("processing", "Processing"),
        new("completed", "Completed"),
        new("declined", "Declined"),
        new("failed", "Failed")
    };

    public static readonly CjLookupItem[] OperStatuses =
    {
        new("pending", "Pending"),
        new("captured", "Captured"),
        new("processing", "Processing"),
        new("settled", "Settled"),
        new("declined", "Declined")
    };

    public static readonly CjLookupItem[] ComplianceStatuses =
    {
        new("pending", "Pending"),
        new("approved", "Approved"),
        new("declined", "Declined")
    };

    public static readonly CjLookupItem[] WebhookTypes =
    {
        new("payinNotification", "Pay-in notification"),
        new("payoutNotification", "Pay-out notification"),
        new("payoutReturnNotification", "Pay-out return"),
        new("refundNotification", "Refund notification"),
        new("instantFxTransferNotification", "Instant FX transfer"),
        new("ibanAllocationNotification", "IBAN allocation"),
        new("virtualAccountActionNotification", "Virtual account action"),
        new("walletReservationNotification", "Wallet reservation"),
        new("walletFundTransferNotification", "Wallet fund transfer")
    };

    public static readonly CjLookupItem[] RefundDirections =
    {
        new("outgoing", "Outgoing (reverse a payout we sent)"),
        new("incoming", "Incoming (return a pay-in we received)")
    };

    public static readonly CjLookupItem[] CheckKinds =
    {
        new("cop", "UK Confirmation of Payee"),
        new("iban", "SEPA IBAN reachability")
    };

    public static readonly CjLookupItem[] IbansGroups =
    {
        new("DEFAULT", "DEFAULT — standard IBAN pool"),
        new("PARTNER", "PARTNER — partner-segmented pool"),
        new("HIGHVALUE", "HIGHVALUE — high-value vIBANs")
    };
}
