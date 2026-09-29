using OneOf;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Models;

[GenerateOneOf]
internal sealed partial class ClubOperationOutcome : OneOfBase<ClubOperationOutcome.Saved, ClubOperationOutcome.Invalid, ClubOperationOutcome.Forbidden, ClubOperationOutcome.Conflict>
{
    internal sealed record Saved(string Message);
    internal sealed record Invalid(string Message);
    internal sealed record Forbidden(string Message);
    internal sealed record Conflict(string Message);

    internal ClubReply ToReply() => Match(
        value => new ClubReply(ClubReplyKind.Saved, value.Message),
        value => new ClubReply(ClubReplyKind.Invalid, value.Message),
        value => new ClubReply(ClubReplyKind.Forbidden, value.Message),
        value => new ClubReply(ClubReplyKind.Conflict, value.Message));
}
