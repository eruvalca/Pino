using Microsoft.AspNetCore.Components;
using Pino.UI.Features.Tryouts.Models;

namespace Pino.UI.Features.Tryouts.Components;

public sealed partial class NoteComposer
{
    private bool _saving;
    private string? _message;
    private string _messageKind = "information";
    [CascadingParameter] private SampleTryoutSession Session { get; set; } = null!;
    [Parameter, EditorRequired] public EventCallback OnChanged { get; set; }
    private SamplePlayer Player => Session.Selected!;

    private async Task SaveAsync()
    {
        if (_saving || Session.Busy)
        {
            return;
        }

        _saving = true;
        _message = null;
        var pending = Session.SaveNoteAsync(Player);
        await OnChanged.InvokeAsync();
        var result = await pending;
        result.Switch(
            saved => SetMessage(saved.Message, "success"),
            failed => SetMessage(failed.Message, "error"),
            conflict => SetMessage(conflict.Message, "warning"));
        _saving = false;
        await OnChanged.InvokeAsync();
    }

    private void SetMessage(string message, string kind)
    {
        _message = message;
        _messageKind = kind;
    }
}
