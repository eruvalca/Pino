using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Pino.SharedKernel.Sporting;

namespace Pino.Client.Features.Sporting;

internal sealed class HttpSportGateway(HttpClient http) : ISportGateway
{
    public Task<SportReply> SaveTeamTargetsAsync(Guid clubId, Guid teamId, TeamTargetsInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, TeamTargetsInput>(clubId, $"teams/{teamId}/targets", input, cancellationToken);
    public Task<IReadOnlyList<EnrollmentCandidate>> GetEnrollmentCandidatesAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<EnrollmentCandidate>>(clubId, $"tryouts/{tryoutId}/enrollment-candidates", cancellationToken);
    public Task<SportingBatchReport> EnrollBulkAsync(Guid clubId, Guid tryoutId, BulkEnrollmentInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportingBatchReport, BulkEnrollmentInput>(clubId, $"tryouts/{tryoutId}/bulk-enrollment", input, cancellationToken);
    public Task<IReadOnlyList<TeamAvailabilitySummary>> GetTeamAvailabilityAsync(Guid clubId, Guid seasonId, Guid? tryoutId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<TeamAvailabilitySummary>>(clubId, $"seasons/{seasonId}/team-availability" + (tryoutId is null ? "" : $"?tryoutId={tryoutId}"), cancellationToken);
    public Task<SportReply> SaveTeamAvailabilityAsync(Guid clubId, Guid seasonId, Guid? tryoutId, TeamAvailabilityInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, TeamAvailabilityInput>(clubId, $"seasons/{seasonId}/team-availability" + (tryoutId is null ? "" : $"?tryoutId={tryoutId}"), input, cancellationToken);
    public Task<IReadOnlyList<ReturningPlayerReview>> GetReturningPlayersAsync(Guid clubId, Guid tryoutId, Guid sourceSeasonId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ReturningPlayerReview>>(clubId, $"tryouts/{tryoutId}/returning?sourceSeasonId={sourceSeasonId}", cancellationToken);
    public Task<SportingBatchReport> PlaceReturningPlayersAsync(Guid clubId, Guid tryoutId, ReturningPlacementInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportingBatchReport, ReturningPlacementInput>(clubId, $"tryouts/{tryoutId}/returning", input, cancellationToken);
    public Task<IReadOnlyList<EnrollmentDetail>> GetEnrollmentsAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<EnrollmentDetail>>(clubId, $"tryouts/{tryoutId}/enrollments", cancellationToken);
    public Task<SportReply> ChangeEnrollmentAsync(Guid clubId, Guid tryoutId, EnrollmentChangeInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, EnrollmentChangeInput>(clubId, $"tryouts/{tryoutId}/enrollments", input, cancellationToken);
    public Task<SportingBatchReport> ChangeEnrollmentsAsync(Guid clubId, Guid tryoutId, BulkEnrollmentChangeInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportingBatchReport, BulkEnrollmentChangeInput>(clubId, $"tryouts/{tryoutId}/enrollments/batch", input, cancellationToken);
    public Task<IReadOnlyList<AttendanceSummary>> GetAttendanceAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<AttendanceSummary>>(clubId, $"tryouts/{tryoutId}/attendance", cancellationToken);
    public Task<SportReply> SaveAttendanceAsync(Guid clubId, Guid tryoutId, AttendanceInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, AttendanceInput>(clubId, $"tryouts/{tryoutId}/attendance", input, cancellationToken);
    public Task<ErasureReport> ErasePlayerAsync(Guid clubId, ErasePlayerInput input, CancellationToken cancellationToken = default) =>
        PostAsync<ErasureReport, ErasePlayerInput>(clubId, "players/erase", input, cancellationToken);
    public Task<ErasureReport> GetErasureAsync(Guid clubId, Guid operationId, CancellationToken cancellationToken = default) =>
        GetAsync<ErasureReport>(clubId, $"erasures/{operationId}", cancellationToken);
    public Task<SeasonReview> GetSeasonReviewAsync(Guid clubId, Guid seasonId, CancellationToken cancellationToken = default) =>
        GetAsync<SeasonReview>(clubId, $"seasons/{seasonId}/review", cancellationToken);
    public Task<TryoutReview> GetTryoutReviewAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        GetAsync<TryoutReview>(clubId, $"tryouts/{tryoutId}/review", cancellationToken);
    public Task<SportReply> CloseTryoutAsync(Guid clubId, Guid tryoutId, CloseTryoutInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, CloseTryoutInput>(clubId, $"tryouts/{tryoutId}/close", input, cancellationToken);
    public Task<SportReply> ReopenTryoutAsync(Guid clubId, Guid tryoutId, ReopenTryoutInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, ReopenTryoutInput>(clubId, $"tryouts/{tryoutId}/reopen", input, cancellationToken);
    public Task<TeamDetail> GetTeamAsync(Guid clubId, Guid teamId, Guid? seasonId, CancellationToken cancellationToken = default) =>
        GetAsync<TeamDetail>(clubId, $"teams/{teamId}" + (seasonId is null ? "" : $"?seasonId={seasonId}"), cancellationToken);
    public Task<SportOverview> GetOverviewAsync(Guid clubId, CancellationToken cancellationToken = default) =>
        GetAsync<SportOverview>(clubId, "overview", cancellationToken);

    public Task<PlayerPage> GetPlayersAsync(Guid clubId, string query, bool archived, int page, CancellationToken cancellationToken = default) =>
        GetAsync<PlayerPage>(clubId, string.Create(CultureInfo.InvariantCulture, $"players?query={Uri.EscapeDataString(query)}&archived={archived}&page={page}"), cancellationToken);

    public Task<PlayerDetail> GetPlayerAsync(Guid clubId, Guid playerId, CancellationToken cancellationToken = default) =>
        GetAsync<PlayerDetail>(clubId, $"players/{playerId}", cancellationToken);

    public Task<TryoutDetail> GetTryoutAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default) =>
        GetAsync<TryoutDetail>(clubId, $"tryouts/{tryoutId}", cancellationToken);

    public Task<PlayerNotebook> GetNotebookAsync(Guid clubId, Guid tryoutId, Guid playerId, CancellationToken cancellationToken = default) =>
        GetAsync<PlayerNotebook>(clubId, $"tryouts/{tryoutId}/players/{playerId}/notebook", cancellationToken);

    public Task<SportReply> SavePlayerAsync(Guid clubId, PlayerInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, PlayerInput>(clubId, "players", input, cancellationToken);

    public Task<SportReply> SaveSeasonAsync(Guid clubId, SeasonInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, SeasonInput>(clubId, "seasons", input, cancellationToken);

    public Task<SportReply> SaveTeamAsync(Guid clubId, TeamInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, TeamInput>(clubId, "teams", input, cancellationToken);

    public Task<SportReply> SaveTryoutAsync(Guid clubId, TryoutInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, TryoutInput>(clubId, "tryouts", input, cancellationToken);

    public Task<SportReply> EnrollAsync(Guid clubId, Guid tryoutId, EnrollmentInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, EnrollmentInput>(clubId, $"tryouts/{tryoutId}/players", input, cancellationToken);

    public Task<SportReply> SaveBibNumberAsync(Guid clubId, Guid tryoutId, BibNumberInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, BibNumberInput>(clubId, $"tryouts/{tryoutId}/bib", input, cancellationToken);

    public Task<SportReply> DecideAsync(Guid clubId, Guid tryoutId, DecisionInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, DecisionInput>(clubId, $"tryouts/{tryoutId}/decisions", input, cancellationToken);

    public Task<SportReply> AddNoteAsync(Guid clubId, Guid tryoutId, NoteInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, NoteInput>(clubId, $"tryouts/{tryoutId}/notes", input, cancellationToken);

    public Task<ImportReport> ImportAsync(Guid clubId, ImportInput input, CancellationToken cancellationToken = default) =>
        PostAsync<ImportReport, ImportInput>(clubId, "import", input, cancellationToken);

    public Task<SportReply> RedactNoteAsync(Guid clubId, Guid tryoutId, RedactNoteInput input, CancellationToken cancellationToken = default) =>
        PostAsync<SportReply, RedactNoteInput>(clubId, $"tryouts/{tryoutId}/notes/redact", input, cancellationToken);

    private async Task<T> GetAsync<T>(Guid clubId, string path, CancellationToken ct)
    {
        using var response = await http.GetAsync(new Uri($"api/clubs/{clubId}/sport/{path}", UriKind.Relative), ct);
        EnsureAccess(response);
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new HttpRequestException("Empty sporting response.");
    }

    private async Task<TReply> PostAsync<TReply, TInput>(Guid clubId, string path, TInput input, CancellationToken ct)
    {
        var token = await http.GetFromJsonAsync<string>("api/clubs/token", ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/clubs/{clubId}/sport/{path}") { Content = JsonContent.Create(input) };
        request.Headers.Add("X-Pino-CSRF", token);
        using var response = await http.SendAsync(request, ct);
        EnsureAccess(response);
        return await response.Content.ReadFromJsonAsync<TReply>(ct) ?? throw new HttpRequestException("Empty sporting response.");
    }

    private static void EnsureAccess(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { throw new UnauthorizedAccessException(); }
        if (response.StatusCode == HttpStatusCode.NotFound) { throw new KeyNotFoundException(); }
        response.EnsureSuccessStatusCode();
    }
}
