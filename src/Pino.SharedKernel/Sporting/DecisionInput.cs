using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record DecisionInput(Guid OperationId, Guid PlayerId, DecisionKind Kind, Guid? TeamId, long Revision, long PlacementRevision, string Reason);
