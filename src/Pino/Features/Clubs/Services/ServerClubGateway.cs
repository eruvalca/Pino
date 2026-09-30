using Microsoft.AspNetCore.Components.Authorization;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

internal sealed class ServerClubGateway(ClubService service, AuthenticationStateProvider authentication) : IClubGateway
{
    public async Task<AccessSnapshot> GetAccessAsync(CancellationToken cancellationToken = default) =>
        await service.GetAccessAsync((await authentication.GetAuthenticationStateAsync()).User, cancellationToken);

    public async Task<ClubSearchPage> SearchAsync(string query, int page, CancellationToken cancellationToken = default) =>
        await service.SearchAsync((await authentication.GetAuthenticationStateAsync()).User, query, page, cancellationToken);

    public async Task<ClubReply> SaveProfileAsync(ProfileInput input, CancellationToken cancellationToken = default) =>
        (await service.SaveProfileAsync((await authentication.GetAuthenticationStateAsync()).User, input, cancellationToken)).ToReply();

    public async Task<ClubReply> CreateAsync(CreateClubInput input, CancellationToken cancellationToken = default) =>
        (await service.CreateAsync((await authentication.GetAuthenticationStateAsync()).User, input, cancellationToken)).ToReply();

    public async Task<ClubReply> RequestAsync(Guid clubId, CancellationToken cancellationToken = default) =>
        (await service.RequestAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, cancellationToken)).ToReply();

    public async Task<ClubReply> CancelAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        (await service.CancelAsync((await authentication.GetAuthenticationStateAsync()).User, requestId, cancellationToken)).ToReply();

    public async Task<ClubReply> LeaveAsync(Guid clubId, CancellationToken cancellationToken = default) =>
        (await service.LeaveAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, cancellationToken)).ToReply();

    public async Task<PeoplePage> GetPeopleAsync(Guid clubId, bool requests, int page, CancellationToken cancellationToken = default) =>
        await service.GetPeopleAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, requests, page, cancellationToken);

    public async Task<ClubReply> DecideAsync(Guid clubId, RequestDecisionInput input, CancellationToken cancellationToken = default) =>
        (await service.DecideAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, input, cancellationToken)).ToReply();

    public async Task<ClubReply> ChangeMemberAsync(Guid clubId, MemberChangeInput input, CancellationToken cancellationToken = default) =>
        (await service.ChangeMemberAsync((await authentication.GetAuthenticationStateAsync()).User, clubId, input, cancellationToken)).ToReply();

}
