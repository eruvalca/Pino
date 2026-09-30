using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record TeamSummary(Guid Id, string Name, int GraduationYear, bool Archived, long Revision, int? RosterTarget = null, IReadOnlyList<PositionTarget>? PositionTargets = null, bool Excluded = false);
