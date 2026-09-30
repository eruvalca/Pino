using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class ComparisonRoster
{
    [Parameter, EditorRequired] public IReadOnlyList<PlayerSummary> Players { get; set; } = [];
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter, EditorRequired] public EventCallback<Guid> Selected { get; set; }
    [Parameter] public Guid SelectedId { get; set; }
    [Parameter] public bool Disabled { get; set; }
    private int _page;
    private IReadOnlyList<PlayerSummary>? _previous;

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Players, _previous)) { _page = 0; _previous = Players; }
    }

    private void ChangePage(int page) => _page = Math.Clamp(page, 0, Math.Max(0, (Players.Count - 1) / 50));
}
