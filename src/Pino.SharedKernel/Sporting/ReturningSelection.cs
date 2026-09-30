using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record ReturningSelection(Guid PlayerId, Guid SourceTeamId, Guid TargetTeamId, long PlayerRevision, long SourcePlacementRevision, long EntryRevision, long PlacementRevision, long TeamRevision);
