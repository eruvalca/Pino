using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record ImportResolution(int Row, ImportDisposition Action, Guid? CandidateId = null, long Revision = 0,
    Guid ErasureOperationId = default, string Confirmation = "", bool AcknowledgeHistory = false);
