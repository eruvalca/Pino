using Microsoft.AspNetCore.Components.Authorization;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed class ServerSportGateway(SportService service, AuthenticationStateProvider authentication) : ISportGateway
{
    public async Task<SeasonReview> GetSeasonReviewAsync(Guid clubId, Guid seasonId, CancellationToken cancellationToken = default) =>
        await service.GetSeasonReviewAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, seasonId, cancellationToken);
    public async Task<TryoutReview> GetTryoutReviewAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        await service.GetTryoutReviewAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, cancellationToken);
    public async Task<SportReply> CloseTryoutAsync(Guid clubId, Guid tryoutId, CloseTryoutInput input, CancellationToken cancellationToken = default) =>
        (await service.CloseTryoutAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();
    public async Task<SportReply> ReopenTryoutAsync(Guid clubId, Guid tryoutId, ReopenTryoutInput input, CancellationToken cancellationToken = default) =>
        (await service.ReopenTryoutAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, input, cancellationToken)).ToReply();
    public async Task<TeamDetail> GetTeamAsync(Guid clubId, Guid teamId, CancellationToken cancellationToken = default) =>
        await service.GetTeamAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, teamId, cancellationToken);
    public async Task<SportOverview> GetOverviewAsync(Guid clubId, CancellationToken cancellationToken = default) =>
        await service.GetOverviewAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, cancellationToken);

    public async Task<PlayerPage> GetPlayersAsync(Guid clubId, string query, bool archived, int page, CancellationToken cancellationToken = default) =>
        await service.GetPlayersAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, query, archived, page, cancellationToken);

    public async Task<PlayerDetail> GetPlayerAsync(Guid clubId, Guid playerId, CancellationToken cancellationToken = default) =>
        await service.GetPlayerAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, playerId, cancellationToken);

    public async Task<TryoutDetail> GetTryoutAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        await service.GetTryoutAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, tryoutId, cancellationToken);

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

}
