using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Pino.SharedKernel.Clubs;

namespace Pino.UI.Features.Clubs.Pages;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The page logs failures and clears protected content when membership cannot be verified.")]
public sealed partial class ClubHome(IClubGateway gateway, ILogger<ClubHome> logger)
{
    private MembershipSummary? _membership;
    private bool _unavailable;

    [Parameter] public Guid ClubId { get; set; }

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var access = await gateway.GetAccessAsync();
            _membership = access.Membership?.Club.Id == ClubId ? access.Membership : null;
            _unavailable = _membership is null;
        }
        catch (Exception exception)
        {
            _membership = null;
            _unavailable = true;
            LogAccessFailed(logger, exception);
        }
    }

    [LoggerMessage(EventId = 2012, Level = LogLevel.Warning, Message = "Club workspace membership could not be verified.")]
    private static partial void LogAccessFailed(ILogger logger, Exception exception);
}
