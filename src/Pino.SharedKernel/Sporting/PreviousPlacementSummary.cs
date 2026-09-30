using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Previous placement context is shared by server, browser and UI assemblies.")]
public sealed record PreviousPlacementSummary(Guid SeasonId, string Season, Guid TeamId, string Team);
