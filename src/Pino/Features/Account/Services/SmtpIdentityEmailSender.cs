using System.Data.Common;
using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using MimeKit;
using Pino.Data;

namespace Pino.Features.Account.Services;

internal sealed class SmtpIdentityEmailSender(IConfiguration configuration, IHostEnvironment environment) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your Pino account", "Confirm your email address", confirmationLink);
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your Pino password", "Reset your password", resetLink);
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        DeliverAsync(email, "Your Pino reset code", $"Your password reset code is {WebUtility.HtmlDecode(resetCode)}.");

    private Task SendAsync(string email, string subject, string action, string encodedLink) =>
        DeliverAsync(email, subject, $"{action}:\n\n{WebUtility.HtmlDecode(encodedLink)}\n\nIf you did not request this, you can ignore this email.");

    private async Task DeliverAsync(string email, string subject, string text)
    {
        var host = configuration["Smtp:Host"];
        var port = configuration.GetValue("Smtp:Port", 587);
        var security = configuration.GetValue("Smtp:ImplicitTls", defaultValue: false) ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        if (environment.IsDevelopment() && configuration.GetConnectionString("mailpit") is { } connection)
        {
            var values = new DbConnectionStringBuilder { ConnectionString = connection };
            var endpoint = new Uri((string)values["Endpoint"]);
            host = endpoint.Host;
            port = endpoint.Port;
            security = SecureSocketOptions.None;
        }
        if (string.IsNullOrWhiteSpace(host)) { throw new InvalidOperationException("Configure Smtp:Host and Smtp:From before using account email."); }
        var from = configuration["Smtp:From"] ?? (environment.IsDevelopment() ? "pino@localhost" : throw new InvalidOperationException("Configure Smtp:From."));
        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = text };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, security, timeout.Token);
        if (configuration["Smtp:Username"] is { Length: > 0 } username)
        {
            await client.AuthenticateAsync(username, configuration["Smtp:Password"] ?? throw new InvalidOperationException("Configure Smtp:Password."), timeout.Token);
        }
        await client.SendAsync(message, timeout.Token);
        await client.DisconnectAsync(quit: true, timeout.Token);
    }
}
