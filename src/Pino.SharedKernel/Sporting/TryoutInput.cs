using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed class TryoutInput
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SeasonId { get; set; }
    public long Revision { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [StringLength(160)] public string Location { get; set; } = "";
}
