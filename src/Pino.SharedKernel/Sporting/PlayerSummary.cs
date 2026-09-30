using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record PlayerSummary(Guid Id, string PlayerReference, string FirstName, string LastName, int GraduationYear, string Position, string ContactEmail, bool Archived, long Revision, Uri? PhotoUrl, string MiddleName = "", string SecondaryPosition = "")
{
    public string FullName => string.IsNullOrWhiteSpace(MiddleName) ? $"{FirstName} {LastName}" : $"{FirstName} {MiddleName} {LastName}";
}
