using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Pino.Features.Clubs.Services;

internal static class InvitationTokens
{
    internal static string Create() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    internal static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
    internal static bool Matches(string? token, byte[] expected) =>
        token is { Length: 43 } && CryptographicOperations.FixedTimeEquals(Hash(token), expected);
}
