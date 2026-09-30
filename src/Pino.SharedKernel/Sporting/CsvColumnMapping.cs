using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record CsvColumnMapping(int PlayerReference = -1, int FirstName = -1, int MiddleName = -1,
    int LastName = -1, int GraduationYear = -1, int Position = -1, int SecondaryPosition = -1, int ContactEmail = -1)
{
    public IReadOnlyList<int> Indices => [PlayerReference, FirstName, MiddleName, LastName, GraduationYear, Position, SecondaryPosition, ContactEmail];
}
