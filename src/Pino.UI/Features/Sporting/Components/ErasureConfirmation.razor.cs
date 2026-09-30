using Microsoft.AspNetCore.Components;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class ErasureConfirmation
{
    [Parameter, EditorRequired] public string FullName { get; set; } = "";
    [Parameter, EditorRequired] public string InputId { get; set; } = "";
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string Confirmation { get; set; } = "";
    [Parameter] public EventCallback<string> ConfirmationChanged { get; set; }
    [Parameter] public bool AcknowledgeHistory { get; set; }
    [Parameter] public EventCallback<bool> AcknowledgeHistoryChanged { get; set; }
    private Task NameChangedAsync(ChangeEventArgs args) => ConfirmationChanged.InvokeAsync(args.Value?.ToString() ?? "");
    private Task AcknowledgementChangedAsync(ChangeEventArgs args) => AcknowledgeHistoryChanged.InvokeAsync(args.Value is true);
}
