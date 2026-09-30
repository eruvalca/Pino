using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record TryoutResult(Guid PlayerId, string FirstName, string LastName, int GraduationYear, string Bib, DecisionKind Decision, Guid? TeamId, string? TeamName, string MiddleName = "")
{
    public string FullName => string.IsNullOrWhiteSpace(MiddleName) ? $"{FirstName} {LastName}" : $"{FirstName} {MiddleName} {LastName}";
}
