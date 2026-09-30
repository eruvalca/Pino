using OneOf;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Models;

[GenerateOneOf]
internal sealed partial class ErasureOutcome : OneOfBase<ErasureOutcome.Recorded, SportOutcome.Invalid, SportOutcome.Conflict>
{
    internal sealed record Recorded(Guid OperationId, bool PhotoPending)
    {
        internal ErasureReport ToReply() => new(SportReplyKind.Saved,
            PhotoPending ? "Player records and history are deleted. Photo deletion is still pending and will retry automatically. Check the status to confirm it finishes."
                : "Player records, history and photos are deleted. Only the administrator who deleted them and the time are kept.", OperationId, PhotoPending);
    }

    internal ErasureReport ToReply(Guid operationId) => Match(
        recorded => recorded.ToReply(),
        invalid => new ErasureReport(SportReplyKind.Invalid, invalid.Message, operationId),
        conflict => new ErasureReport(SportReplyKind.Conflict, conflict.Message, operationId));
}
