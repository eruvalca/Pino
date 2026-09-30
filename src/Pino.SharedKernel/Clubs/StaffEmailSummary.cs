namespace Pino.SharedKernel.Clubs;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed record StaffEmailSummary(Guid Id, string RecipientEmail, string Subject, StaffEmailStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? SentAt, int Attempts, long Revision);
