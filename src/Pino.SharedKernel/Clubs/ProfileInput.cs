using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

namespace Pino.SharedKernel.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed class ProfileInput
{
    [Required, StringLength(80)]
    public string FirstName { get; set; } = "";
    [Required, StringLength(80)]
    public string LastName { get; set; } = "";
    public string? CroppedPhoto { get; set; }
}
