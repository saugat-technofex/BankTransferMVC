using BankTransferMVC.Integrations.ClearJunction.Models;

namespace BankTransferMVC.Integrations.ClearJunction;

/// <summary>
/// Mode-aware façade over Clear Junction's REST API. Methods dispatch to the in-process
/// <c>ICjSimulator</c> when <c>ICjModeService.IsSimulation</c> is true, otherwise to the live
/// REST API using signed HTTP requests.
/// </summary>
public interface IClearJunctionClient
{
    // ---------- Virtual IBAN ----------
    Task<AllocateIbanResponse> AllocateIbanAsync(AllocateIbanRequest request, CancellationToken ct = default);

    // ---------- FX ----------
    Task<FxRateResponse> GetFxRateAsync(FxRateRequest request, CancellationToken ct = default);
    Task<FxTransferResponse> CreateFxTransferAsync(FxTransferRequest request, CancellationToken ct = default);

    // ---------- Pay-out ----------
    Task<PayoutResponse> CreatePayoutAsync(string rail, PayoutRequest request, CancellationToken ct = default);
    Task<PayoutStatusResponse?> GetPayoutStatusAsync(string clientOrder, CancellationToken ct = default);

    // ---------- Pay-in (card) ----------
    Task<CardPayinResponse> CreateCardPayinAsync(CardPayinRequest request, CancellationToken ct = default);

    // ---------- Refund ----------
    Task<RefundResponse> CreateRefundAsync(RefundRequest request, CancellationToken ct = default);

    // ---------- Transaction action ----------
    Task<TransactionActionResponse> TransactionActionAsync(string action, TransactionActionRequest request, CancellationToken ct = default);

    // ---------- Requisite checks ----------
    Task<CopCheckResponse> CheckCopAsync(CopCheckRequest request, CancellationToken ct = default);
    Task<IbanCheckResponse> CheckIbanAsync(string iban, CancellationToken ct = default);

    // ---------- Wallets ----------
    Task<CreateWalletResponse> CreateWalletAsync(CreateWalletRequest request, CancellationToken ct = default);
    Task<WalletGetResponse> GetWalletAsync(string walletUuid, CancellationToken ct = default);
    Task<WalletTransferResponse> WalletTransferAsync(WalletTransferRequest request, CancellationToken ct = default);

    // ---------- Reports ----------
    Task<TransactionReportResponse> TransactionReportAsync(TransactionReportRequest request, CancellationToken ct = default);

    // ---------- Diagnostics ----------
    /// <summary>Cheap probe that verifies live-mode credentials + network connectivity.</summary>
    Task<ConnectivityResult> TestConnectivityAsync(CancellationToken ct = default);
}

public sealed record ConnectivityResult(bool Ok, int? StatusCode, long ElapsedMs, string Detail);
