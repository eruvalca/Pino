using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record DecisionSummary(Guid Id, Guid PlayerId, Guid TryoutId, string Tryout, string Season, DecisionKind Kind, string? Team, string Author, DateTimeOffset CreatedAt, string Reason, Guid? TeamId);
