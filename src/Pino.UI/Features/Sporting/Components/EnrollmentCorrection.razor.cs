using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class EnrollmentCorrection
{
    [Parameter, EditorRequired] public EnrollmentDetail Enrollment { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<EnrollmentChangeInput> Submitted { get; set; }
    [Parameter] public bool Disabled { get; set; }
    private Guid _playerId;
    private long _revision;
    private Guid _operationId;
    private string _reason = "";
    private string _bib = "";
    private bool CanSubmit => !Disabled && !string.IsNullOrWhiteSpace(_reason);

    protected override void OnParametersSet()
    {
        if (_playerId == Enrollment.Entry.Player.Id && _revision == Enrollment.Entry.Revision) { return; }
        _playerId = Enrollment.Entry.Player.Id;
        _revision = Enrollment.Entry.Revision;
        _operationId = Guid.NewGuid();
        _reason = "";
        _bib = Enrollment.Entry.Bib;
    }

    private Task SubmitAsync() => CanSubmit
        ? Submitted.InvokeAsync(new(_operationId, _playerId, !Enrollment.Removed, _reason, _revision, Enrollment.Entry.PlacementRevision, _bib))
        : Task.CompletedTask;
}
