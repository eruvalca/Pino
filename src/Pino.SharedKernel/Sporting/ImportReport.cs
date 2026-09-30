using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record ImportReport(IReadOnlyList<ImportRow> Rows, bool Saved, string Message)
{
    public bool CanImport => Rows.Count > 0 && Rows.All(row => row.Error is null);
}
