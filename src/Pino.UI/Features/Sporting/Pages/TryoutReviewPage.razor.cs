using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TryoutReviewPage : SportPageBase
{
    [Parameter] public Guid TryoutId { get; set; }
    private TryoutReview? _review;
    private Guid? _editionId;
    private Guid _closeOperation = Guid.NewGuid();
    private bool _confirmed;
    private string _reason = "";
    private string _query = "";
    private string _decision = "";
    private TryoutCloseoutSummary? Edition => _review?.Closeouts.FirstOrDefault(value => value.Id == _editionId);
    private string ExportUrl => $"/api/clubs/{ClubId}/sport/tryouts/{TryoutId}/results.csv" + (Edition is { } edition ? $"?editionId={edition.Id}" : "");
    private IEnumerable<TryoutResult> Results => (Edition?.Results ?? _review?.Results ?? [])
        .Where(value => $"{value.FullName} {value.Bib} {value.TeamName}".Contains(_query, StringComparison.OrdinalIgnoreCase) &&
            (_decision.Length == 0 || string.Equals(value.Decision.ToString(), _decision, StringComparison.Ordinal)));

    protected override async Task LoadAsync()
    {
        _review = await Gateway.GetTryoutReviewAsync(ClubId, TryoutId, Token);
        _editionId = _review.Tryout.Closed ? _review.Closeouts.FirstOrDefault(value => value.ReopenedAt is null)?.Id : null;
        _confirmed = false;
        _closeOperation = Guid.NewGuid();
    }

    private async Task CloseAsync()
    {
        if (!_confirmed || _review is null || _editionId is not null || !_review.Tryout.Complete) { return; }
        if (await SaveAsync(() => Gateway.CloseTryoutAsync(ClubId, TryoutId, new(_closeOperation, _review.ReviewToken), Token)))
        {
            await ExecuteAsync(LoadAsync);
        }
    }

    private async Task ReopenAsync()
    {
        if (Edition is not { ReopenedAt: null } edition) { return; }
        if (await SaveAsync(() => Gateway.ReopenTryoutAsync(ClubId, TryoutId, new(edition.Id, _reason), Token)))
        {
            _reason = "";
            await ExecuteAsync(LoadAsync);
        }
    }
}
