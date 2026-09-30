using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Roster planning contracts are shared by server, browser and UI.")]
public sealed record TeamTargetsInput(long Revision, int? RosterTarget, IReadOnlyList<PositionTarget> Positions);
