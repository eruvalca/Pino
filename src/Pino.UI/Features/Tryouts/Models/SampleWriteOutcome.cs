using OneOf;

namespace Pino.UI.Features.Tryouts.Models;

[GenerateOneOf]
internal sealed partial class SampleWriteOutcome : OneOfBase<SampleWriteOutcome.Saved, SampleWriteOutcome.Failed, SampleWriteOutcome.Conflict>
{
    internal sealed record Saved(string Message);
    internal sealed record Failed(string Message);
    internal sealed record Conflict(string Message);
}
