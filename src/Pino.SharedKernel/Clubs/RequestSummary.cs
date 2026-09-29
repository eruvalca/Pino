using System.Diagnostics.CodeAnalysis;
namespace Pino.SharedKernel.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed record RequestSummary(Guid Id, ClubSummary Club, JoinRequestStatus Status, DateTimeOffset CreatedAt);
