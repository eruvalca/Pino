using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

namespace Pino.SharedKernel.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed class CreateClubInput
{
    public Guid OperationId { get; set; } = Guid.NewGuid();
    [Required, StringLength(120)]
    public string Name { get; set; } = "";
    [Required, StringLength(60)]
    public string Sport { get; set; } = "";
    [Required, StringLength(100)]
    public string City { get; set; } = "";
    [Required]
    public string State { get; set; } = "";
}
