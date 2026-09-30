namespace Pino.SharedKernel.Clubs;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed record StaffEmailsPage(ClubSummary Club, IReadOnlyList<StaffEmailSummary> Items, int Page, bool HasMore);
