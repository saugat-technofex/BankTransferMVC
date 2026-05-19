using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BankTransferMVC.Integrations.ClearJunction.Models;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Integrations.ClearJunction;

public class ClearJunctionClient : IClearJunctionClient
{
    private readonly HttpClient _http;
    private readonly IClearJunctionSignatureService _signer;
    private readonly ClearJunctionOptions _options;
    private readonly ILogger<ClearJunctionClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public ClearJunctionClient(
        HttpClient http,
        IClearJunctionSignatureService signer,
        IOptions<ClearJunctionOptions> options,
        ILogger<ClearJunctionClient> logger)
    {
        _http = http;
        _signer = signer;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }
    }

    public Task<AllocateIbanResponse> AllocateIbanAsync(AllocateIbanRequest request, CancellationToken ct = default)
    {
        if (_options.SimulationMode)
        {
            var orderRef = Guid.NewGuid().ToString();
            var iban = GenerateMockIban(request.IbanCountry);
            return Task.FromResult(new AllocateIbanResponse
            {
                RequestReference = orderRef,
                ClientOrder = request.ClientOrder,
                OrderReference = orderRef,
                Status = "accepted",
                Iban = iban
            });
        }
        return PostAsync<AllocateIbanRequest, AllocateIbanResponse>("v7/gate/allocate/v3/create/iban", request, ct);
    }

    public Task<FxRateResponse> GetFxRateAsync(FxRateRequest request, CancellationToken ct = default)
    {
        if (_options.SimulationMode)
        {
            var rate = SimulatedRate(request.SellCurrency, request.BuyCurrency);
            return Task.FromResult(new FxRateResponse
            {
                RequestReference = Guid.NewGuid().ToString(),
                Quotes = new List<FxQuote>
                {
                    new()
                    {
                        RateUuid = Guid.NewGuid().ToString(),
                        Quote = rate.ToString("0.0000"),
                        SellCurrency = request.SellCurrency,
                        BuyCurrency = request.BuyCurrency,
                        ExpirationTimestamp = DateTimeOffset.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:sszzz")
                    },
                    new()
                    {
                        RateUuid = Guid.NewGuid().ToString(),
                        Quote = (1m / rate).ToString("0.0000"),
                        SellCurrency = request.BuyCurrency,
                        BuyCurrency = request.SellCurrency,
                        ExpirationTimestamp = DateTimeOffset.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:sszzz")
                    }
                }
            });
        }
        return PostAsync<FxRateRequest, FxRateResponse>("v7/gate/fx/instant/rate", request, ct);
    }

    public Task<FxTransferResponse> CreateFxTransferAsync(FxTransferRequest request, CancellationToken ct = default)
    {
        if (_options.SimulationMode)
        {
            var orderRef = Guid.NewGuid().ToString();
            return Task.FromResult(new FxTransferResponse
            {
                ClientOrder = request.ClientOrder,
                OrderReference = orderRef,
                OperTimestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                SellAmount = request.SellAmount,
                SellCurrency = request.SellCurrency,
                BuyAmount = request.BuyAmount,
                BuyCurrency = request.BuyCurrency,
                Status = "pending",
                RateUuid = request.RateUuid,
                RequestReference = orderRef,
                CreatedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz")
            });
        }
        return PostAsync<FxTransferRequest, FxTransferResponse>("v7/gate/fx/instant/transfer", request, ct);
    }

    public Task<PayoutResponse> CreatePayoutAsync(string rail, PayoutRequest request, CancellationToken ct = default)
    {
        var path = rail switch
        {
            "internal" => "v7/gate/payout/internalPayment",
            "sepa" => "v7/gate/payout/bankTransfer/eu",
            "sepaInst" => "v7/gate/payout/bankTransfer/sepaInst",
            "fps" => "v7/gate/payout/bankTransfer/fps",
            "chaps" => "v7/gate/payout/bankTransfer/chaps",
            "chapsCrossScheme" => "v7/gate/payout/bankTransfer/chapsCrossScheme",
            "swift" => "v7/gate/payout/bankTransfer/swift",
            _ => throw new ArgumentException($"Unsupported payout rail '{rail}'", nameof(rail))
        };

        if (_options.SimulationMode)
        {
            var orderRef = Guid.NewGuid().ToString();
            return Task.FromResult(new PayoutResponse
            {
                RequestReference = orderRef,
                ClientOrder = request.ClientOrder,
                OrderReference = orderRef,
                CreatedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Status = "created",
                SubStatuses = new CjSubStatuses { OperStatus = "pending", ComplianceStatus = "pending" }
            });
        }
        return PostAsync<PayoutRequest, PayoutResponse>(path, request, ct);
    }

    public async Task<PayoutStatusResponse?> GetPayoutStatusAsync(string clientOrder, CancellationToken ct = default)
    {
        if (_options.SimulationMode) return null;

        var date = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        using var req = new HttpRequestMessage(HttpMethod.Get, $"v7/gate/status/payout/clientOrder/{clientOrder}");
        ApplyAuthHeaders(req, date, string.Empty);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<PayoutStatusResponse>(json, JsonOptions);
    }

    private async Task<TResp> PostAsync<TReq, TResp>(string path, TReq body, CancellationToken ct)
    {
        var date = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var bodyJson = JsonSerializer.Serialize(body, JsonOptions);

        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        ApplyAuthHeaders(req, date, bodyJson);

        _logger.LogInformation("[CJ] POST {Path} (sim={Sim})", path, _options.SimulationMode);

        using var resp = await _http.SendAsync(req, ct);
        var respJson = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("[CJ] {Status} {Path}: {Body}", (int)resp.StatusCode, path, respJson);
            throw new InvalidOperationException($"Clear Junction error {(int)resp.StatusCode}: {respJson}");
        }

        return JsonSerializer.Deserialize<TResp>(respJson, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from Clear Junction");
    }

    private void ApplyAuthHeaders(HttpRequestMessage req, string date, string body)
    {
        var signature = _signer.ComputeSignature(_options.ApiKey, date, body);
        req.Headers.TryAddWithoutValidation("Date", date);
        req.Headers.TryAddWithoutValidation("X-API-KEY", _options.ApiKey);
        req.Headers.TryAddWithoutValidation("Authorization", signature);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static string GenerateMockIban(string country)
    {
        var c = string.IsNullOrEmpty(country) ? "GB" : country.ToUpperInvariant();
        var digits = Random.Shared.NextInt64(10_000_000_000_000_000L, 99_999_999_999_999_999L);
        return $"{c}00CLJU{digits}";
    }

    private static decimal SimulatedRate(string sell, string buy)
    {
        if (string.Equals(sell, buy, StringComparison.OrdinalIgnoreCase)) return 1m;
        var table = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["EUR/USD"] = 1.0850m,
            ["USD/EUR"] = 0.9220m,
            ["GBP/USD"] = 1.2700m,
            ["USD/GBP"] = 0.7874m,
            ["EUR/GBP"] = 0.8550m,
            ["GBP/EUR"] = 1.1700m,
            ["EUR/CHF"] = 0.9510m,
            ["EUR/PLN"] = 4.3200m,
        };
        var key = $"{sell}/{buy}";
        return table.TryGetValue(key, out var r) ? r : 1.0000m;
    }
}
