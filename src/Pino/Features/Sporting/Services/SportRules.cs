using System.ComponentModel.DataAnnotations;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal static class SportRules
{
    internal static bool Valid(object input) => Validator.TryValidateObject(input, new ValidationContext(input), validationResults: null, validateAllProperties: true);
    internal static bool Eligible(int playerYear, int teamYear) => playerYear >= teamYear;
    internal static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
    internal static bool ValidDecision(DecisionInput input) => input.OperationId != Guid.Empty && Enum.IsDefined(input.Kind) &&
        (input.Reason?.Length ?? 0) <= 1000 && (input.Kind == DecisionKind.Placed) == input.TeamId.HasValue;
    internal static string Reference(string? value) => (value ?? "").Trim().ToUpperInvariant();
    internal static bool ValidReference(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 40 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    internal static bool ValidPlayer(PlayerInput input) => Valid(input) && input.Id != Guid.Empty && ValidReference(input.PlayerReference);
    internal static string DecisionLabel(DecisionKind kind) => kind switch
    {
        DecisionKind.Awaiting => "Awaiting decision",
        DecisionKind.Placed => "Placed",
        DecisionKind.Withdrawn => "Withdrawn",
        DecisionKind.NotSelected => "Not selected",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
