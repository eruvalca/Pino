using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Team history is shared by server, browser and UI.")]
public sealed record TeamDetail(TeamSummary Team, SeasonSummary? Season, IReadOnlyList<PlayerSummary> Members, IReadOnlyList<TeamHistoryEntry> History, IReadOnlyList<SeasonSummary>? Seasons = null);
