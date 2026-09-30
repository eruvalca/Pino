using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record TryoutReview(TryoutSummary Tryout, SeasonSummary Season, IReadOnlyList<TryoutResult> Results, IReadOnlyList<TryoutCloseoutSummary> Closeouts, string ReviewToken);
