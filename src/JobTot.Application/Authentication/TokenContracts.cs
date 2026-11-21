using System.Security.Cryptography;
using System.Text;
namespace JobTot.Application.Authentication;

public interface IAccessTokenIssuer
{
    string Issue(string role, Guid accountId, Guid sessionId, string name, DateTimeOffset expires);
}
public static class PasswordStamp
{
    public static string Hash(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
}
