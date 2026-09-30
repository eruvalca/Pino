using Microsoft.AspNetCore.Components.Authorization;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed class ServerSportGateway(SportService service, AuthenticationStateProvider authentication) : ISportGateway
{
    public async Task<SportReply> SaveTeamTargetsAsync(Guid clubId, Guid teamId, TeamTargetsInput input, CancellationToken cancellationToken = default) =>
        (await service.SaveTeamTargetsAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, teamId, input, cancellationToken)).ToReply();
    public async Task<IReadOnlyList<EnrollmentCandidate>> GetEnrollmentCandidatesAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        await service.GetEnrollmentCandidatesAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, cancellationToken);
    public async Task<SportingBatchReport> EnrollBulkAsync(Guid clubId, Guid tryoutId, BulkEnrollmentInput input, CancellationToken cancellationToken = default) =>
        await service.EnrollBulkAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken);
    public async Task<IReadOnlyList<TeamAvailabilitySummary>> GetTeamAvailabilityAsync(Guid clubId, Guid seasonId, Guid? tryoutId, CancellationToken cancellationToken = default) =>
        await service.GetTeamAvailabilityAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, seasonId, tryoutId, cancellationToken);
    public async Task<SportReply> SaveTeamAvailabilityAsync(Guid clubId, Guid seasonId, Guid? tryoutId, TeamAvailabilityInput input, CancellationToken cancellationToken = default) =>
        (await service.SaveTeamAvailabilityAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, seasonId, tryoutId, input, cancellationToken)).ToReply();
    public async Task<IReadOnlyList<ReturningPlayerReview>> GetReturningPlayersAsync(Guid clubId, Guid tryoutId, Guid sourceSeasonId, CancellationToken cancellationToken = default) =>
        await service.GetReturningPlayersAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, sourceSeasonId, cancellationToken);
    public async Task<SportingBatchReport> PlaceReturningPlayersAsync(Guid clubId, Guid tryoutId, ReturningPlacementInput input, CancellationToken cancellationToken = default) =>
        await service.PlaceReturningPlayersAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken);
    public async Task<IReadOnlyList<EnrollmentDetail>> GetEnrollmentsAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        await service.GetEnrollmentsAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, cancellationToken);
    public async Task<SportReply> ChangeEnrollmentAsync(Guid clubId, Guid tryoutId, EnrollmentChangeInput input, CancellationToken cancellationToken = default) =>
        (await service.ChangeEnrollmentAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();
    public async Task<SportingBatchReport> ChangeEnrollmentsAsync(Guid clubId, Guid tryoutId, BulkEnrollmentChangeInput input, CancellationToken cancellationToken = default) =>
        await service.ChangeEnrollmentsAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken);
    public async Task<ErasureReport> ErasePlayerAsync(Guid clubId, ErasePlayerInput input, CancellationToken cancellationToken = default) =>
        (await service.ErasePlayerAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, input, cancellationToken)).ToReply(input.OperationId);
    public async Task<ErasureReport> GetErasureAsync(Guid clubId, Guid operationId, CancellationToken cancellationToken = default) =>
        await service.GetErasureAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, operationId, cancellationToken);
    public async Task<SeasonReview> GetSeasonReviewAsync(Guid clubId, Guid seasonId, CancellationToken cancellationToken = default) =>
        await service.GetSeasonReviewAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, seasonId, cancellationToken);
    public async Task<TryoutReview> GetTryoutReviewAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        await service.GetTryoutReviewAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, cancellationToken);
    public async Task<SportReply> CloseTryoutAsync(Guid clubId, Guid tryoutId, CloseTryoutInput input, CancellationToken cancellationToken = default) =>
        (await service.CloseTryoutAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();
    public async Task<SportReply> ReopenTryoutAsync(Guid clubId, Guid tryoutId, ReopenTryoutInput input, CancellationToken cancellationToken = default) =>
        (await service.ReopenTryoutAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();
    public async Task<TeamDetail> GetTeamAsync(Guid clubId, Guid teamId, Guid? seasonId, CancellationToken cancellationToken = default) =>
        await service.GetTeamAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, teamId, seasonId, cancellationToken);
    public async Task<SportOverview> GetOverviewAsync(Guid clubId, CancellationToken cancellationToken = default) =>
        await service.GetOverviewAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, cancellationToken);

    public async Task<PlayerPage> GetPlayersAsync(Guid clubId, string query, bool archived, int page, CancellationToken cancellationToken = default) =>
        await service.GetPlayersAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, query, archived, page, cancellationToken);

    public async Task<PlayerDetail> GetPlayerAsync(Guid clubId, Guid playerId, CancellationToken cancellationToken = default) =>
        await service.GetPlayerAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, playerId, cancellationToken);

    public async Task<TryoutDetail> GetTryoutAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        await service.GetTryoutAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, cancellationToken);

    public async Task<PlayerNotebook> GetNotebookAsync(Guid clubId, Guid tryoutId, Guid playerId, CancellationToken cancellationToken = default) =>
        await service.GetNotebookAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, playerId, cancellationToken);

    public async Task<SportReply> SavePlayerAsync(Guid clubId, PlayerInput input, CancellationToken cancellationToken = default) =>
        (await service.SavePlayerAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, input, cancellationToken)).ToReply();

    public async Task<SportReply> SaveSeasonAsync(Guid clubId, SeasonInput input, CancellationToken cancellationToken = default) =>
        (await service.SaveSeasonAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, input, cancellationToken)).ToReply();

    public async Task<SportReply> SaveTeamAsync(Guid clubId, TeamInput input, CancellationToken cancellationToken = default) =>
        (await service.SaveTeamAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, input, cancellationToken)).ToReply();

    public async Task<SportReply> SaveTryoutAsync(Guid clubId, TryoutInput input, CancellationToken cancellationToken = default) =>
        (await service.SaveTryoutAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, input, cancellationToken)).ToReply();

    public async Task<SportReply> EnrollAsync(Guid clubId, Guid tryoutId, EnrollmentInput input, CancellationToken cancellationToken = default) =>
        (await service.EnrollAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();

    public async Task<SportReply> SaveBibNumberAsync(Guid clubId, Guid tryoutId, BibNumberInput input, CancellationToken cancellationToken = default) =>
        (await service.SaveBibNumberAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();

    public async Task<SportReply> DecideAsync(Guid clubId, Guid tryoutId, DecisionInput input, CancellationToken cancellationToken = default) =>
        (await service.DecideAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();

    public async Task<SportReply> AddNoteAsync(Guid clubId, Guid tryoutId, NoteInput input, CancellationToken cancellationToken = default) =>
        (await service.AddNoteAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();

    public async Task<ImportReport> ImportAsync(Guid clubId, ImportInput input, CancellationToken cancellationToken = default) =>
        await service.ImportAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, input, cancellationToken);

    public async Task<SportReply> RedactNoteAsync(Guid clubId, Guid tryoutId, RedactNoteInput input, CancellationToken cancellationToken = default) =>
        (await service.RedactNoteAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();

}
