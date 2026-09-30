using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Reviewed enrollment changes are shared by server, browser and UI.")]
public sealed record BulkEnrollmentChangeInput(Guid OperationId, bool Remove, string Reason, IReadOnlyList<EnrollmentChangeSelection> Players);
