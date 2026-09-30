using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed class SeasonInput
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Revision { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    public DateOnly StartsOn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly EndsOn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(6);
    public bool Archived { get; set; }
}
