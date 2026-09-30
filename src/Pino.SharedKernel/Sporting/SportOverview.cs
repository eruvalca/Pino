using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record SportOverview(IReadOnlyList<SeasonSummary> Seasons, IReadOnlyList<TeamSummary> Teams, IReadOnlyList<TryoutSummary> Tryouts, int ActivePlayers = 0);
