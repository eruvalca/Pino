using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record TryoutDetail(TryoutSummary Tryout, SeasonSummary Season, IReadOnlyList<TeamSummary> Teams, IReadOnlyList<RosterEntry> Roster, IReadOnlyList<NoteSummary> Notes, IReadOnlyList<DecisionSummary> History);
