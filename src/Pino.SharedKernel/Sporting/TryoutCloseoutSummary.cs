using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record TryoutCloseoutSummary(Guid Id, string TryoutName, string SeasonName, DateOnly TryoutDate, DateTimeOffset ClosedAt, string ClosedBy, DateTimeOffset? ReopenedAt, string? ReopenedBy, string? ReopenReason, IReadOnlyList<TryoutResult> Results, int ErasedPlayers = 0, Uri? ClosedByPhotoUrl = null, Uri? ReopenedByPhotoUrl = null);
