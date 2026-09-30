using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class EnrollmentBatchReview
{
    [Parameter, EditorRequired] public IReadOnlyList<EnrollmentDetail> Players { get; set; } = [];
    [Parameter, EditorRequired] public bool Remove { get; set; }
    [Parameter, EditorRequired] public EventCallback<BulkEnrollmentChangeInput> Submitted { get; set; }
    [Parameter, EditorRequired] public EventCallback Cancelled { get; set; }
    [Parameter] public bool Disabled { get; set; }
    private readonly Guid _operationId = Guid.NewGuid();
    private string _reason = "";
    private ElementReference _heading;
    private bool CanSubmit => !Disabled && Players.Count is > 0 and <= 1000 && !string.IsNullOrWhiteSpace(_reason);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) { await _heading.FocusAsync(); }
    }

    private Task SubmitAsync() => CanSubmit
        ? Submitted.InvokeAsync(new(_operationId, Remove, _reason, Players.Select(value => new EnrollmentChangeSelection(value.Entry.Player.Id, value.Entry.Revision, value.Entry.PlacementRevision)).ToArray()))
        : Task.CompletedTask;
}
