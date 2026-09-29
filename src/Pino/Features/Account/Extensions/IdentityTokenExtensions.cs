using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Pino.Features.Account.Extensions;

internal static class IdentityTokenExtensions
{
    extension(string token)
    {
        internal string EncodeIdentityToken() => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }
}
