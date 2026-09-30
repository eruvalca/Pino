using System.Net;
using Microsoft.AspNetCore.Identity;
using Pino.Data;
using Pino.Services.Mail;

namespace Pino.Features.Account.Services;

internal sealed class SmtpIdentityEmailSender(IMailDelivery delivery, AccountEmailLimits limits) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "confirmation", "Confirm your Pino account", "Confirm your email address", confirmationLink);
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "reset", "Reset your Pino password", "Reset your password", resetLink);
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        limits.TryReserve(email, "reset") ? delivery.SendAsync(email, "Your Pino reset code", $"Your password reset code is {WebUtility.HtmlDecode(resetCode)}.", CancellationToken.None) : Task.CompletedTask;

    private Task SendAsync(string email, string purpose, string subject, string action, string encodedLink) =>
        limits.TryReserve(email, purpose) ? delivery.SendAsync(email, subject, $"{action}:\n\n{WebUtility.HtmlDecode(encodedLink)}\n\nIf you did not request this, you can ignore this email.", CancellationToken.None) : Task.CompletedTask;
}
