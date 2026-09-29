using Microsoft.AspNetCore.Components;
using Pino.UI.Features.Tryouts.Models;

namespace Pino.UI.Features.Tryouts.Components;

public sealed partial class ObservationList
{
    [CascadingParameter] private SampleTryoutSession Session { get; set; } = null!;
    private SamplePlayer Player => Session.Selected!;
}
