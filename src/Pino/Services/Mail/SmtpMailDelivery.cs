using System.Data.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Pino.Services.Mail;

// Account verification and staff notifications share the configured SMTP transport.
internal sealed class SmtpMailDelivery(IConfiguration configuration, IHostEnvironment environment) : IMailDelivery
{
    public async Task SendAsync(string email, string subject, string text, CancellationToken cancellationToken)
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
        if (string.IsNullOrWhiteSpace(host)) { throw new InvalidOperationException("Configure Smtp:Host and Smtp:From before using email."); }
        var from = configuration["Smtp:From"] ?? (environment.IsDevelopment() ? "pino@localhost" : throw new InvalidOperationException("Configure Smtp:From."));
        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = text };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
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
