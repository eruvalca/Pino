using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Availability is shared by server, browser and UI assemblies.")]
public sealed record TeamAvailabilitySummary(TeamSummary Team, bool SeasonExcluded, long SeasonRevision, bool TryoutExcluded, long TryoutRevision);
