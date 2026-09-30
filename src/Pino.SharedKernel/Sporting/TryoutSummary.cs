using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record TryoutSummary(Guid Id, Guid SeasonId, string Name, DateOnly Date, string Location, int Players, int Decided, long Revision)
{
    public bool Complete => Players > 0 && Players == Decided;
}
