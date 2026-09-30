using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class PlayerHistory
{
    [Parameter, EditorRequired] public PlayerNotebook Notebook { get; set; } = default!;
    private HashSet<Guid> Superseded { get; set; } = [];

    protected override void OnParametersSet() => Superseded = Notebook.Notes.Where(value => value.CorrectsId.HasValue).Select(value => value.CorrectsId!.Value).ToHashSet();
}
