namespace BankTransferMVC.Services;

public interface IClientCustomerIdGenerator
{
    string Next(string prefix = "CUST");
}

public class ClientCustomerIdGenerator : IClientCustomerIdGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Next(string prefix = "CUST")
    {
        Span<char> tail = stackalloc char[6];
        for (int i = 0; i < tail.Length; i++)
        {
            tail[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }
        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{new string(tail)}";
    }
}
