using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed class TeamInput
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Revision { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [Range(2000, 2100)] public int GraduationYear { get; set; } = DateTime.UtcNow.Year + 5;
    public bool Archived { get; set; }
}
