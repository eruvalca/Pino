using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class AttendanceEditor
{
    [Parameter, EditorRequired] public Guid PlayerId { get; set; }
    [Parameter] public AttendanceSummary? Attendance { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter, EditorRequired] public EventCallback<AttendanceInput> Save { get; set; }
    private static string Label(AttendanceKind kind) => kind switch { AttendanceKind.Present => "Present", AttendanceKind.Absent => "Absent", _ => "Not recorded" };
    private static string Action(AttendanceKind kind) => kind switch { AttendanceKind.Present => "Mark present", AttendanceKind.Absent => "Mark absent", _ => "Clear attendance" };
}
