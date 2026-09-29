using System.Diagnostics.CodeAnalysis;
namespace Pino.SharedKernel.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed record PeoplePage(ClubSummary Club, IReadOnlyList<PersonSummary> People, int Page, bool HasMore);
