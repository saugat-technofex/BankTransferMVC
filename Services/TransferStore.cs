using System.Collections.Concurrent;
using BankTransferMVC.Models;

namespace BankTransferMVC.Services;

public interface ITransferStore
{
    void AddIban(VirtualIbanRecord record);
    IReadOnlyList<VirtualIbanRecord> ListIbans();
    VirtualIbanRecord? GetIban(string clientOrder);

    void AddPayout(PayoutRecord record);
    IReadOnlyList<PayoutRecord> ListPayouts();
    PayoutRecord? GetPayout(string clientOrder);
    void UpdatePayoutStatus(string clientOrder, string status, string operStatus, string complianceStatus);

    void AddFx(FxRecord record);
    IReadOnlyList<FxRecord> ListFx();

    void AddEvent(WebhookEvent ev);
    IReadOnlyList<WebhookEvent> ListEvents();

    void AddWallet(WalletRecord record);
    IReadOnlyList<WalletRecord> ListWallets();
    WalletRecord? GetWallet(string walletUuid);

    void AddRefund(RefundRecord record);
    IReadOnlyList<RefundRecord> ListRefunds();

    void AddCardPayin(CardPayinRecord record);
    IReadOnlyList<CardPayinRecord> ListCardPayins();
    CardPayinRecord? GetCardPayin(string clientOrder);
    void UpdateCardPayinStatus(string clientOrder, string status, string operStatus, string complianceStatus);

    void AddRequisiteCheck(RequisiteCheckRecord record);
    IReadOnlyList<RequisiteCheckRecord> ListRequisiteChecks();

    RefundRecord? GetRefund(string clientOrder);
    void UpdateRefundStatus(string clientOrder, string status, string operStatus, string complianceStatus);

    /// <summary>Wipe every in-memory record. Used by the Settings page when resetting simulator state.</summary>
    void Reset();
}

public class InMemoryTransferStore : ITransferStore
{
    private readonly ConcurrentDictionary<string, VirtualIbanRecord> _ibans = new();
    private readonly ConcurrentDictionary<string, PayoutRecord> _payouts = new();
    private readonly ConcurrentBag<FxRecord> _fx = new();
    private readonly ConcurrentBag<WebhookEvent> _events = new();
    private readonly ConcurrentDictionary<string, WalletRecord> _wallets = new();
    private readonly ConcurrentBag<RefundRecord> _refunds = new();
    private readonly ConcurrentBag<CardPayinRecord> _cardPayins = new();
    private readonly ConcurrentBag<RequisiteCheckRecord> _checks = new();

    public void AddIban(VirtualIbanRecord record) => _ibans[record.ClientOrder] = record;
    public IReadOnlyList<VirtualIbanRecord> ListIbans() => _ibans.Values.OrderByDescending(x => x.CreatedAt).ToList();
    public VirtualIbanRecord? GetIban(string clientOrder) => _ibans.GetValueOrDefault(clientOrder);

    public void AddPayout(PayoutRecord record) => _payouts[record.ClientOrder] = record;
    public IReadOnlyList<PayoutRecord> ListPayouts() => _payouts.Values.OrderByDescending(x => x.CreatedAt).ToList();
    public PayoutRecord? GetPayout(string clientOrder) => _payouts.GetValueOrDefault(clientOrder);

    public void UpdatePayoutStatus(string clientOrder, string status, string operStatus, string complianceStatus)
    {
        if (_payouts.TryGetValue(clientOrder, out var rec))
        {
            rec.Status = status;
            rec.OperStatus = operStatus;
            rec.ComplianceStatus = complianceStatus;
            rec.LastUpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void AddFx(FxRecord record) => _fx.Add(record);
    public IReadOnlyList<FxRecord> ListFx() => _fx.OrderByDescending(x => x.CreatedAt).ToList();

    public void AddEvent(WebhookEvent ev) => _events.Add(ev);
    public IReadOnlyList<WebhookEvent> ListEvents() => _events.OrderByDescending(x => x.ReceivedAt).ToList();

    public void AddWallet(WalletRecord record) => _wallets[record.WalletUuid] = record;
    public IReadOnlyList<WalletRecord> ListWallets() => _wallets.Values.OrderByDescending(x => x.CreatedAt).ToList();
    public WalletRecord? GetWallet(string walletUuid) => _wallets.GetValueOrDefault(walletUuid);

    public void AddRefund(RefundRecord record) => _refunds.Add(record);
    public IReadOnlyList<RefundRecord> ListRefunds() => _refunds.OrderByDescending(x => x.CreatedAt).ToList();

    public void AddCardPayin(CardPayinRecord record) => _cardPayins.Add(record);
    public IReadOnlyList<CardPayinRecord> ListCardPayins() => _cardPayins.OrderByDescending(x => x.CreatedAt).ToList();
    public CardPayinRecord? GetCardPayin(string clientOrder) =>
        _cardPayins.FirstOrDefault(c => string.Equals(c.ClientOrder, clientOrder, StringComparison.OrdinalIgnoreCase));
    public void UpdateCardPayinStatus(string clientOrder, string status, string operStatus, string complianceStatus)
    {
        var rec = GetCardPayin(clientOrder);
        if (rec is null) return;
        rec.Status = status;
        rec.OperStatus = operStatus;
        rec.ComplianceStatus = complianceStatus;
    }

    public void AddRequisiteCheck(RequisiteCheckRecord record) => _checks.Add(record);
    public IReadOnlyList<RequisiteCheckRecord> ListRequisiteChecks() => _checks.OrderByDescending(x => x.CheckedAt).ToList();

    public RefundRecord? GetRefund(string clientOrder) =>
        _refunds.FirstOrDefault(r => string.Equals(r.ClientOrder, clientOrder, StringComparison.OrdinalIgnoreCase));

    public void UpdateRefundStatus(string clientOrder, string status, string operStatus, string complianceStatus)
    {
        var rec = GetRefund(clientOrder);
        if (rec is null) return;
        rec.Status = status;
        rec.OperStatus = operStatus;
        rec.ComplianceStatus = complianceStatus;
    }

    public void Reset()
    {
        _ibans.Clear();
        _payouts.Clear();
        _wallets.Clear();

        while (_fx.TryTake(out _)) { }
        while (_events.TryTake(out _)) { }
        while (_refunds.TryTake(out _)) { }
        while (_cardPayins.TryTake(out _)) { }
        while (_checks.TryTake(out _)) { }
    }
}
