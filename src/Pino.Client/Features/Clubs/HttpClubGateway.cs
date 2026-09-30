using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Pino.SharedKernel.Clubs;

namespace Pino.Client.Features.Clubs;

internal sealed class HttpClubGateway(HttpClient http) : IClubGateway
{
    public Task<AccessSnapshot> GetAccessAsync(CancellationToken cancellationToken = default) => GetAsync<AccessSnapshot>("access", cancellationToken);
    public Task<ClubSearchPage> SearchAsync(string query, int page, CancellationToken cancellationToken = default) => GetAsync<ClubSearchPage>(string.Create(CultureInfo.InvariantCulture, $"search?query={Uri.EscapeDataString(query)}&page={page}"), cancellationToken);
    public Task<PeoplePage> GetPeopleAsync(Guid clubId, bool requests, int page, CancellationToken cancellationToken = default) => GetAsync<PeoplePage>(string.Create(CultureInfo.InvariantCulture, $"{clubId}/people?requests={requests}&page={page}"), cancellationToken);
    public Task<ClubReply> SaveProfileAsync(ProfileInput input, CancellationToken cancellationToken = default) => PostAsync("profile", input, cancellationToken);
    public Task<ClubReply> CreateAsync(CreateClubInput input, CancellationToken cancellationToken = default) => PostAsync("create", input, cancellationToken);
    public Task<ClubReply> RequestAsync(Guid clubId, CancellationToken cancellationToken = default) => PostAsync($"{clubId}/requests", new { }, cancellationToken);
    public Task<ClubReply> CancelAsync(Guid requestId, CancellationToken cancellationToken = default) => PostAsync($"requests/{requestId}/cancel", new { }, cancellationToken);
    public Task<ClubReply> LeaveAsync(Guid clubId, CancellationToken cancellationToken = default) => PostAsync($"{clubId}/leave", new { }, cancellationToken);
    public Task<ClubReply> DecideAsync(Guid clubId, RequestDecisionInput input, CancellationToken cancellationToken = default) => PostAsync($"{clubId}/decide", input, cancellationToken);
    public Task<ClubReply> ChangeMemberAsync(Guid clubId, MemberChangeInput input, CancellationToken cancellationToken = default) => PostAsync($"{clubId}/members", input, cancellationToken);

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri($"api/clubs/{path}", UriKind.Relative), cancellationToken);
        EnsureAccess(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new HttpRequestException("Empty club response.");
    }

    private async Task<ClubReply> PostAsync<T>(string path, T input, CancellationToken cancellationToken)
    {
        var token = await GetAsync<string>("token", cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/clubs/{path}") { Content = JsonContent.Create(input) };
        request.Headers.Add("X-Pino-CSRF", token);
        using var response = await http.SendAsync(request, cancellationToken);
        EnsureAccess(response);
        return await response.Content.ReadFromJsonAsync<ClubReply>(cancellationToken) ?? throw new HttpRequestException("Empty club response.");
    }

    private static void EnsureAccess(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException();
        }
        response.EnsureSuccessStatusCode();
    }
}
