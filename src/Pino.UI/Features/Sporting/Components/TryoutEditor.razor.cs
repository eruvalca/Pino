using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class TryoutEditor
{
    private TryoutInput _model = new();
    private Guid _loaded;
    [Parameter, EditorRequired] public TryoutInput Input { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<TryoutInput> Save { get; set; }
    [Parameter, EditorRequired] public EventCallback Cancel { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public string SubmitLabel { get; set; } = "Save tryout";
    protected override void OnParametersSet()
    {
        if (_loaded == Input.Id) { return; }
        _loaded = Input.Id;
        _model = new() { Id = Input.Id, SeasonId = Input.SeasonId, Revision = Input.Revision, Name = Input.Name, Date = Input.Date, Location = Input.Location };
    }
    private Task SaveAsync() => Save.InvokeAsync(_model);
}
