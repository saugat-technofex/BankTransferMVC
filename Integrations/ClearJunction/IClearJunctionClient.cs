using BankTransferMVC.Integrations.ClearJunction.Models;

namespace BankTransferMVC.Integrations.ClearJunction;

public interface IClearJunctionClient
{
    Task<AllocateIbanResponse> AllocateIbanAsync(AllocateIbanRequest request, CancellationToken ct = default);
    Task<FxRateResponse> GetFxRateAsync(FxRateRequest request, CancellationToken ct = default);
    Task<FxTransferResponse> CreateFxTransferAsync(FxTransferRequest request, CancellationToken ct = default);
    Task<PayoutResponse> CreatePayoutAsync(string rail, PayoutRequest request, CancellationToken ct = default);
    Task<PayoutStatusResponse?> GetPayoutStatusAsync(string clientOrder, CancellationToken ct = default);
}
