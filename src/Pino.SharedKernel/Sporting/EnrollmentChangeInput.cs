using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record EnrollmentChangeInput(Guid OperationId, Guid PlayerId, bool Remove, string Reason, long Revision, long PlacementRevision, string Bib = "");
