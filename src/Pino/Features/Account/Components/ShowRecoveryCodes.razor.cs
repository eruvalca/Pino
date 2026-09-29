using Microsoft.AspNetCore.Components;

namespace Pino.Features.Account.Components;

public sealed partial class ShowRecoveryCodes
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<string> RecoveryCodes { get; set; } = [];

    [Parameter]
    public string? StatusMessage { get; set; }
}
