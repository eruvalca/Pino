using Microsoft.AspNetCore.DataProtection;
using Pino.Features.Clubs.Data;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

internal sealed class ClubMail(IDataProtectionProvider protection, IConfiguration configuration, IWebHostEnvironment environment, TimeProvider time)
{
    private readonly IDataProtector _protector = protection.CreateProtector("Pino.StaffEmail.v1");

    internal StaffEmail Compose(Guid clubId, string email, string? recipientId, string subject, string body) => new()
    {
        Id = Guid.NewGuid(),
        ClubId = clubId,
        RecipientId = recipientId,
        RecipientEmail = email,
        NormalizedRecipientEmail = email.Trim().ToUpperInvariant(),
        Subject = subject,
        ProtectedBody = _protector.Protect(body),
        Status = StaffEmailStatus.Pending,
        CreatedAt = time.GetUtcNow(),
        NextAttemptAt = time.GetUtcNow(),
    };

    internal string ReadBody(StaffEmail email) => _protector.Unprotect(email.ProtectedBody);

    internal Uri InvitationUrl(Uri requestOrigin, Guid invitationId, string token)
    {
        var configured = configuration["Pino:PublicOrigin"];
        var origin = string.IsNullOrWhiteSpace(configured) ? requestOrigin : new Uri(configured, UriKind.Absolute);
        if (!origin.IsAbsoluteUri || origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0 ||
            !(string.Equals(origin.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
              (environment.IsDevelopment() && origin.IsLoopback && string.Equals(origin.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))) ||
            (string.IsNullOrWhiteSpace(configured) && !(environment.IsDevelopment() && origin.IsLoopback)))
        {
            throw new InvalidOperationException("Configure Pino:PublicOrigin with the application's HTTPS origin before sending invitations outside local development.");
        }
        return new Uri(origin, $"club/invitations/{invitationId}?token={Uri.EscapeDataString(token)}");
    }
}
