using OneOf;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Models;

[GenerateOneOf]
internal sealed partial class SportOutcome : OneOfBase<SportOutcome.Saved, SportOutcome.Invalid, SportOutcome.Conflict>
{
    internal sealed record Saved(string Message, Guid? Id = null);
    internal sealed record Invalid(string Message);
    internal sealed record Conflict(string Message);

    internal SportReply ToReply() => Match(
        saved => new SportReply(SportReplyKind.Saved, saved.Message, saved.Id),
        invalid => new SportReply(SportReplyKind.Invalid, invalid.Message),
        conflict => new SportReply(SportReplyKind.Conflict, conflict.Message));
}
