using System.ComponentModel.DataAnnotations;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

internal static class ClubRules
{
    internal static bool ValidProfile(ProfileInput input) =>
        Validator.TryValidateObject(input, new ValidationContext(input), validationResults: null, validateAllProperties: true);

    internal static bool ValidClub(CreateClubInput input) => input.OperationId != Guid.Empty &&
        Validator.TryValidateObject(input, new ValidationContext(input), validationResults: null, validateAllProperties: true) &&
        UsStates.All.ContainsKey(input.State);

    internal static bool ValidDetails(ClubDetailsInput input) => input.Revision > 0 &&
        Validator.TryValidateObject(input, new ValidationContext(input), validationResults: null, validateAllProperties: true) &&
        UsStates.All.ContainsKey(input.State);

    internal static string? MemberChangeError(ClubRole current, ClubRole expected, ClubRole? next, int administrators)
    {
        if (!Enum.IsDefined(expected) || (next.HasValue && !Enum.IsDefined(next.Value)))
        {
            return "Choose a valid member role.";
        }
        if (current != expected)
        {
            return "This person's role changed. Refresh the members before trying again.";
        }
        if (current == ClubRole.Administrator && next != ClubRole.Administrator && administrators <= 1)
        {
            return "Promote another member before the last administrator can leave or lose access.";
        }
        return null;
    }
}
