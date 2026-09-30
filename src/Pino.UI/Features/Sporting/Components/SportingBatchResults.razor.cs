using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class SportingBatchResults
{
    [Parameter, EditorRequired] public SportingBatchReport Report { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyDictionary<Guid, string> Names { get; set; } = new Dictionary<Guid, string>();
    private int _page;
    private ElementReference _heading;
    private string NoticeKind => Report switch { { Kind: not SportReplyKind.Saved } => "error", { Skipped: > 0 } => "warning", _ => "success" };
    private async Task ChangePageAsync(int page) { _page = page; await _heading.FocusAsync(); }
}
