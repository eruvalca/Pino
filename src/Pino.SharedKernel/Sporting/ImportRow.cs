using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record ImportRow(int Row, string PlayerReference, string Name, int GraduationYear, string? Error, string? Warning = null,
    ImportDisposition Disposition = ImportDisposition.Create, IReadOnlyList<ImportCandidate>? Candidates = null, Guid? PlayerId = null,
    Guid? ErasureOperationId = null, bool PhotoPending = false);
