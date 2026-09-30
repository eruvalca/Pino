using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record RosterEntry(PlayerSummary Player, string Bib, DecisionKind Decision, Guid? DecisionTeamId, long Revision, Guid? CurrentTeamId, Guid? CurrentTryoutId, long PlacementRevision);
