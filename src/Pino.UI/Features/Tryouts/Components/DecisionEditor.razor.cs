using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Pino.UI.Features.Tryouts.Models;

namespace Pino.UI.Features.Tryouts.Components;

public sealed partial class DecisionEditor
{
    private bool _open;
    private bool _saving;
    private bool _conflict;
    private bool _invalid;
    private bool _focusAfterRender;
    private ElementReference _trigger;
    private ElementReference _editorHeading;
    private string _kind = "";
    private int? _teamId;
    private int _version;
    private string? _message;
    private string _messageKind = "information";
    [CascadingParameter] private SampleTryoutSession Session { get; set; } = null!;
    [Parameter, EditorRequired] public EventCallback OnChanged { get; set; }
    private SamplePlayer Player => Session.Selected!;

    private void Open()
    {
        _open = true;
        _focusAfterRender = true;
        LoadLatest();
    }

    private void Close()
    {
        _open = false;
        _focusAfterRender = true;
        _message = null;
    }

    private void LoadLatest()
    {
        _version = Player.Version;
        _kind = Player.Decision.IsComplete ? Player.Decision.Kind : "";
        _teamId = Player.Decision.AssignedTeam?.Id;
        _conflict = false;
        _invalid = false;
        _message = null;
    }

    private void Choose(ChangeEventArgs args)
    {
        _kind = args.Value?.ToString() ?? "";
        ClearValidation();
    }

    private void ClearValidation()
    {
        _invalid = false;
        if (!_conflict)
        {
            _message = null;
        }
    }

    private void ReviewLatest()
    {
        LoadLatest();
        _focusAfterRender = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusAfterRender)
        {
            _focusAfterRender = false;
            await (_open ? _editorHeading : _trigger).FocusAsync();
        }
    }

    private async Task SaveAsync()
    {
        if (_saving || Session.Busy || _conflict)
        {
            return;
        }

        _saving = true;
        _invalid = _kind.Length == 0 || (string.Equals(_kind, "placed", StringComparison.Ordinal)
            && !Session.Teams.Any(team => team.Id == _teamId && Player.GraduationYear >= team.GraduationYear));
        var pending = Session.SaveDecisionAsync(Player, _kind, _teamId, _version);
        await OnChanged.InvokeAsync();
        var result = await pending;
        result.Switch(
            saved => { SetMessage(saved.Message, "success"); _open = false; _focusAfterRender = true; },
            failed => SetMessage(failed.Message, "error"),
            conflict => { SetMessage(conflict.Message, "warning"); _conflict = true; });
        _saving = false;
        await OnChanged.InvokeAsync();
    }

    private void SetMessage(string message, string kind)
    {
        _message = message;
        _messageKind = kind;
    }
}
