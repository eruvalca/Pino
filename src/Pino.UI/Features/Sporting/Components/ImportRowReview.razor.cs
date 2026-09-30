using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class ImportRowReview
{
    [Parameter, EditorRequired] public ImportRow Row { get; set; } = default!;
    [Parameter, EditorRequired] public Guid ClubId { get; set; }
    [Parameter] public ImportResolution? Resolution { get; set; }
    [Parameter] public EventCallback<ImportResolution> ResolutionChanged { get; set; }
    [Parameter] public bool Saved { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Administrator { get; set; }
    private ImportResolution _choice = new(0, ImportDisposition.Create);
    private ImportCandidate? Selected => Row.Candidates?.SingleOrDefault(value => value.Id == _choice.CandidateId);
    private string Label => Row.Disposition switch
    {
        ImportDisposition.Create => "Create new player",
        ImportDisposition.Skip => "Skipped",
        ImportDisposition.Review => "Needs a decision",
        ImportDisposition.Reactivate => "Ready to restore existing player",
        ImportDisposition.Replace => "Ready to create new player and erase archived record",
        ImportDisposition.Created => "Created",
        ImportDisposition.Reactivated => "Reactivated",
        ImportDisposition.Replaced => "New player created; archived record erased",
        _ => "Needs correction",
    };
    protected override void OnParametersSet() => _choice = Resolution ?? new(Row.Row, ImportDisposition.Create);
    private Task ActionChangedAsync(ChangeEventArgs args)
    {
        if (!Enum.TryParse<ImportDisposition>(args.Value?.ToString(), out var action)) { return Task.CompletedTask; }
        return ResolutionChanged.InvokeAsync(_choice with { Action = action, Confirmation = "", AcknowledgeHistory = false, ErasureOperationId = Guid.NewGuid() });
    }
    private Task CandidateChangedAsync(ChangeEventArgs args)
    {
        var candidate = Guid.TryParse(args.Value?.ToString(), out var id) ? Row.Candidates?.SingleOrDefault(value => value.Id == id) : null;
        return ResolutionChanged.InvokeAsync(_choice with { CandidateId = candidate?.Id, Revision = candidate?.Revision ?? 0, Confirmation = "", AcknowledgeHistory = false, ErasureOperationId = Guid.NewGuid() });
    }
    private Task ConfirmationChangedAsync(string value) => ResolutionChanged.InvokeAsync(_choice with { Confirmation = value });
    private Task AcknowledgementChangedAsync(bool value) => ResolutionChanged.InvokeAsync(_choice with { AcknowledgeHistory = value });
}
