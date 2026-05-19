using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace BankTransferMVC.Integrations.ClearJunction;

public interface IClearJunctionSignatureService
{
    string ComputeSignature(string apiKey, string date, string requestBody);
    bool Verify(string apiKey, string date, string requestBody, string providedSignature);
}

// Clear Junction docs (Apiary):
//   REQUEST SIGNATURE is calculated from X-API-KEY, Date, RequestBody and a
//   modified apiPassword. The exact algorithm is illustrated by a diagram
//   delivered in the integration pack.
//
// The published example signatures are 128 hex characters which matches a
// SHA-512 hex digest. We therefore implement a sensible default:
//   signature = SHA512( X-API-KEY + Date + RequestBody + SHA512(apiPassword) )
// This MUST be validated against Clear Junction sandbox vectors and adjusted
// to match the live algorithm before production use.
public class ClearJunctionSignatureService : IClearJunctionSignatureService
{
    private readonly ClearJunctionOptions _options;

    public ClearJunctionSignatureService(IOptions<ClearJunctionOptions> options)
    {
        _options = options.Value;
    }

    public string ComputeSignature(string apiKey, string date, string requestBody)
    {
        var modifiedPassword = Sha512Hex(_options.ApiPassword ?? string.Empty);
        var canonical = apiKey + date + (requestBody ?? string.Empty) + modifiedPassword;
        return Sha512Hex(canonical);
    }

    public bool Verify(string apiKey, string date, string requestBody, string providedSignature)
    {
        var expected = ComputeSignature(apiKey, date, requestBody);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(providedSignature ?? string.Empty));
    }

    private static string Sha512Hex(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA512.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
