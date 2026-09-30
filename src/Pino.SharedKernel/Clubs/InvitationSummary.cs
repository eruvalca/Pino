namespace Pino.SharedKernel.Clubs;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed record InvitationSummary(Guid Id, string Email, ClubRole Role, DateTimeOffset ExpiresAt, DateTimeOffset? UsedAt, DateTimeOffset? RevokedAt, long Revision, StaffEmailStatus? DeliveryStatus);
