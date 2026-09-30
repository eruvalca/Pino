using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Team history is shared by server, browser and UI.")]
public sealed record TeamHistoryEntry(PlayerSummary Player, DecisionSummary Decision);
