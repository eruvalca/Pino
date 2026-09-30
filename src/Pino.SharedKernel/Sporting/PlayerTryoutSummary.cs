using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record PlayerTryoutSummary(Guid Id, string Name, string Season, DateOnly Date, string Bib, bool Removed, bool Closed, DecisionKind Decision, string? Team);
