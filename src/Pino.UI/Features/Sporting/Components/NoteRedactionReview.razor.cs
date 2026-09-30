using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class NoteRedactionReview
{
    [Parameter, EditorRequired] public Guid NoteId { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter, EditorRequired] public EventCallback<RedactNoteInput> Submitted { get; set; }
    [Parameter, EditorRequired] public EventCallback Cancelled { get; set; }
    private Guid _noteId;
    private Guid _operationId;
    private string _reason = "";
    private bool _confirmed;
    private bool CanSubmit => !Disabled && _confirmed && !string.IsNullOrWhiteSpace(_reason);

    protected override void OnParametersSet()
    {
        if (_noteId == NoteId) { return; }
        _noteId = NoteId;
        _operationId = Guid.NewGuid();
        _reason = "";
        _confirmed = false;
    }

    private Task SubmitAsync() => CanSubmit
        ? Submitted.InvokeAsync(new(_operationId, NoteId, _reason, _confirmed))
        : Task.CompletedTask;
}
