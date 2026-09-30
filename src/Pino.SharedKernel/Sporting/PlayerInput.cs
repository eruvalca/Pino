using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed class PlayerInput
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Revision { get; set; }
    [Required, StringLength(40)] public string PlayerReference { get; set; } = "";
    [Required, StringLength(80)] public string FirstName { get; set; } = "";
    [Required, StringLength(80)] public string LastName { get; set; } = "";
    [Range(2000, 2100)] public int GraduationYear { get; set; } = DateTime.UtcNow.Year + 5;
    [StringLength(80)] public string Position { get; set; } = "";
    [StringLength(254), EmailAddress]
    public string? ContactEmail
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
    public bool Archived { get; set; }
    public string? Photo { get; set; }
    public bool RemovePhoto { get; set; }
}
