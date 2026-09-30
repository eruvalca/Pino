using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class TeamEditor
{
    private TeamInput _model = new();
    private Guid _loaded;
    [Parameter, EditorRequired] public TeamInput Input { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<TeamInput> Save { get; set; }
    [Parameter, EditorRequired] public EventCallback Cancel { get; set; }
    [Parameter] public bool Busy { get; set; }
    protected override void OnParametersSet()
    {
        if (_loaded == Input.Id) { return; }
        _loaded = Input.Id;
        _model = new() { Id = Input.Id, Revision = Input.Revision, Name = Input.Name, GraduationYear = Input.GraduationYear, Archived = Input.Archived };
    }
    private Task SaveAsync() => Save.InvokeAsync(_model);
}
