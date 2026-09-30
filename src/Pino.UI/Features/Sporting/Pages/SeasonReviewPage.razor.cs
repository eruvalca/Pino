using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class SeasonReviewPage : SportPageBase
{
    [Parameter] public Guid SeasonId { get; set; }
    private SeasonReview? _review;
    protected override async Task LoadAsync() => _review = await ReadInitialAsync("GetSeasonReviewAsync", () => Gateway.GetSeasonReviewAsync(ClubId, SeasonId, Token));
}
