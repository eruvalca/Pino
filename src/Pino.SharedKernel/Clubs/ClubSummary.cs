using System.Diagnostics.CodeAnalysis;
namespace Pino.SharedKernel.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed record ClubSummary(Guid Id, string Name, string Sport, string City, string State);
