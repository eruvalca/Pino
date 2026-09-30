using System.ComponentModel.DataAnnotations;

namespace Pino.SharedKernel.Clubs;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed class InvitationInput
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    public ClubRole Role { get; set; }
}
