namespace Pino.Services.Mail;

internal interface IMailDelivery
{
    Task SendAsync(string email, string subject, string text, CancellationToken cancellationToken);
}
