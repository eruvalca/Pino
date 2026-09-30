using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record PlayerNotebook(Guid PlayerId, bool Removed, IReadOnlyList<NoteSummary> Notes, IReadOnlyList<DecisionSummary> History,
    IReadOnlyList<EnrollmentChangeSummary>? EnrollmentChanges = null);
