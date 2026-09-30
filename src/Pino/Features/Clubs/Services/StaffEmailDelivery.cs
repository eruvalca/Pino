using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Clubs.Data;
using Pino.Services.Mail;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

// Durable leases prevent two workers from claiming the same queued message. SMTP
// acknowledgement is not transactional: a crash after delivery may cause a duplicate.
// Invitation tokens remain single-use regardless of duplicate delivery.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The background worker records failed delivery and retries database failures; logs contain only exception types, never recipient addresses or invitation tokens.")]
internal sealed partial class StaffEmailDelivery(IDbContextFactory<ApplicationDbContext> factory, IMailDelivery delivery, ClubMail mail, TimeProvider time, ILogger<StaffEmailDelivery> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                for (var count = 0; count < 10 && await DeliverNextAsync(stoppingToken); count++) { /* Drain a bounded batch before yielding. */ }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { LogQueueFailure(logger, exception.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(5), time, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    internal async Task<bool> DeliverNextAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var now = time.GetUtcNow();
        var id = await db.StaffEmails.Where(value =>
            (value.Status == StaffEmailStatus.Pending && value.NextAttemptAt <= now) ||
            (value.Status == StaffEmailStatus.Sending && value.LeaseUntil <= now))
            .OrderBy(value => value.NextAttemptAt).ThenBy(value => value.Id).Select(value => (Guid?)value.Id).FirstOrDefaultAsync(cancellationToken);
        return id is { } emailId && await DeliverAsync(emailId, cancellationToken);
    }

    internal async Task<bool> DeliverAsync(Guid emailId, CancellationToken cancellationToken)
    {
        var message = await ClaimAsync(emailId, cancellationToken);
        if (message is null) { return false; }
        if (message.Status == StaffEmailStatus.Cancelled) { return true; }
        var sent = false;
        try
        {
            await delivery.SendAsync(message.RecipientEmail, message.Subject, mail.ReadBody(message), cancellationToken);
            sent = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { LogDeliveryFailure(logger, message.Id, exception.GetType().Name); }
        await FinishAsync(message, sent, cancellationToken);
        return true;
    }

    private async Task<StaffEmail?> ClaimAsync(Guid emailId, CancellationToken cancellationToken)
    {
        await using var strategy = await factory.CreateDbContextAsync(cancellationToken);
        return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(() => ClaimCoreAsync(emailId, cancellationToken));
    }

    private async Task<StaffEmail?> ClaimCoreAsync(Guid emailId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(731923981)", cancellationToken);
        var now = time.GetUtcNow();
        var email = await db.StaffEmails.SingleOrDefaultAsync(value => value.Id == emailId &&
            ((value.Status == StaffEmailStatus.Pending && value.NextAttemptAt <= now) ||
             (value.Status == StaffEmailStatus.Sending && value.LeaseUntil <= now)), cancellationToken);
        if (email is null) { return null; }
        if (email.InvitationId is { } invitationId && !await db.ClubInvitations.AnyAsync(value =>
            value.Id == invitationId && value.ClubId == email.ClubId && value.Revision == email.InvitationRevision &&
            value.UsedAt == null && value.RevokedAt == null && value.ExpiresAt > now, cancellationToken))
        {
            email.Status = StaffEmailStatus.Cancelled;
            email.ProtectedBody = "";
            email.LeaseId = null;
            email.LeaseUntil = null;
        }
        else
        {
            email.Status = StaffEmailStatus.Sending;
            email.Attempts++;
            email.LeaseId = Guid.NewGuid();
            email.LeaseUntil = now.AddMinutes(2);
        }
        email.Revision++;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return email;
    }

    private async Task FinishAsync(StaffEmail claimed, bool sent, CancellationToken cancellationToken)
    {
        await using var strategy = await factory.CreateDbContextAsync(cancellationToken);
        await strategy.Database.CreateExecutionStrategy().ExecuteAsync(() => FinishCoreAsync(claimed, sent, cancellationToken));
    }

    private async Task FinishCoreAsync(StaffEmail claimed, bool sent, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(731923981)", cancellationToken);
        var current = await db.StaffEmails.SingleOrDefaultAsync(value => value.Id == claimed.Id, cancellationToken);
        if (current is null || current.Status != StaffEmailStatus.Sending || current.LeaseId != claimed.LeaseId) { return; }
        if (sent)
        {
            current.Status = StaffEmailStatus.Sent;
            current.SentAt = time.GetUtcNow();
            current.ProtectedBody = "";
        }
        else
        {
            current.Status = current.Attempts >= 5 ? StaffEmailStatus.Failed : StaffEmailStatus.Pending;
            current.NextAttemptAt = time.GetUtcNow().AddSeconds(15 * Math.Pow(2, Math.Min(current.Attempts, 5)));
        }
        current.LeaseId = null;
        current.LeaseUntil = null;
        current.Revision++;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    [LoggerMessage(EventId = 2040, Level = LogLevel.Warning, Message = "Staff email {EmailId} delivery failed ({FailureType}). The queue retains it for retry.")]
    private static partial void LogDeliveryFailure(ILogger logger, Guid emailId, string failureType);
    [LoggerMessage(EventId = 2041, Level = LogLevel.Error, Message = "Staff email queue operation failed ({FailureType}). The worker will retry.")]
    private static partial void LogQueueFailure(ILogger logger, string failureType);
}
