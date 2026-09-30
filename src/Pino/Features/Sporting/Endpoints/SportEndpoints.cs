using System.Text;
using Azure;
using Microsoft.AspNetCore.Antiforgery;
using Pino.Features.Clubs.Services;
using Pino.Features.Sporting.Services;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Endpoints;

internal static class SportEndpoints
{
    internal static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/clubs/{clubId:guid}/sport").RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            try
            {
                if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                {
                    await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
                }
                return await next(context);
            }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new SportReply(SportReplyKind.Invalid, "Your sign-in changed. Reload before saving.")); }
            catch (UnauthorizedAccessException) { return Results.StatusCode(StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        group.MapGet("/overview", (Guid clubId, HttpContext context, SportService service, CancellationToken ct) => service.GetOverviewAsync(context.User, clubId, ct));
        group.MapGet("/tryouts/{tryoutId:guid}/enrollments", (Guid clubId, Guid tryoutId, HttpContext context, SportService service, CancellationToken ct) => service.GetEnrollmentsAsync(context.User, clubId, tryoutId, ct));
        group.MapPost("/tryouts/{tryoutId:guid}/enrollments", async (Guid clubId, Guid tryoutId, EnrollmentChangeInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.ChangeEnrollmentAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapGet("/seasons/{seasonId:guid}/review", (Guid clubId, Guid seasonId, HttpContext context, SportService service, CancellationToken ct) => service.GetSeasonReviewAsync(context.User, clubId, seasonId, ct));
        group.MapGet("/tryouts/{tryoutId:guid}/review", (Guid clubId, Guid tryoutId, HttpContext context, SportService service, CancellationToken ct) => service.GetTryoutReviewAsync(context.User, clubId, tryoutId, ct));
        group.MapPost("/tryouts/{tryoutId:guid}/close", async (Guid clubId, Guid tryoutId, CloseTryoutInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.CloseTryoutAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/reopen", async (Guid clubId, Guid tryoutId, ReopenTryoutInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.ReopenTryoutAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapGet("/teams/{teamId:guid}", (Guid clubId, Guid teamId, Guid? seasonId, HttpContext context, SportService service, CancellationToken ct) => service.GetTeamAsync(context.User, clubId, teamId, seasonId, ct));
        group.MapGet("/players", (Guid clubId, string query, bool archived, int page, HttpContext context, SportService service, CancellationToken ct) => service.GetPlayersAsync(context.User, clubId, query, archived, page, ct));
        group.MapGet("/players/{playerId:guid}", (Guid clubId, Guid playerId, HttpContext context, SportService service, CancellationToken ct) => service.GetPlayerAsync(context.User, clubId, playerId, ct));
        group.MapGet("/tryouts/{tryoutId:guid}", (Guid clubId, Guid tryoutId, HttpContext context, SportService service, CancellationToken ct) => service.GetTryoutAsync(context.User, clubId, tryoutId, ct));
        group.MapGet("/tryouts/{tryoutId:guid}/players/{playerId:guid}/notebook", (Guid clubId, Guid tryoutId, Guid playerId, HttpContext context, SportService service, CancellationToken ct) => service.GetNotebookAsync(context.User, clubId, tryoutId, playerId, ct));
        group.MapPost("/players", async (Guid clubId, PlayerInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SavePlayerAsync(context.User, clubId, input, ct)).ToReply());
        group.MapPost("/seasons", async (Guid clubId, SeasonInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveSeasonAsync(context.User, clubId, input, ct)).ToReply());
        group.MapPost("/teams", async (Guid clubId, TeamInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveTeamAsync(context.User, clubId, input, ct)).ToReply());
        group.MapPost("/tryouts", async (Guid clubId, TryoutInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveTryoutAsync(context.User, clubId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/players", async (Guid clubId, Guid tryoutId, EnrollmentInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.EnrollAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/bib", async (Guid clubId, Guid tryoutId, BibNumberInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveBibNumberAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/decisions", async (Guid clubId, Guid tryoutId, DecisionInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.DecideAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/notes", async (Guid clubId, Guid tryoutId, NoteInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.AddNoteAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/import", (Guid clubId, ImportInput input, HttpContext context, SportService service, CancellationToken ct) => service.ImportAsync(context.User, clubId, input, ct));
        group.MapPost("/tryouts/{tryoutId:guid}/notes/redact", async (Guid clubId, Guid tryoutId, RedactNoteInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.RedactNoteAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapGet("/template", async (Guid clubId, HttpContext context, SportService service, CancellationToken ct) =>
        {
            await service.GetPlayersAsync(context.User, clubId, "", archived: false, 0, ct);
            return Results.File(Encoding.UTF8.GetBytes(PlayerCsv.Template), "text/csv", "Pino-players-template.csv");
        });
        group.MapGet("/players/{playerId:guid}/photo", GetPhotoAsync);
        group.MapPost("/players/erase", async (Guid clubId, ErasePlayerInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.ErasePlayerAsync(context.User, clubId, input, ct)).ToReply(input.OperationId));
        group.MapGet("/erasures/{operationId:guid}", (Guid clubId, Guid operationId, HttpContext context, SportService service, CancellationToken ct) => service.GetErasureAsync(context.User, clubId, operationId, ct));
        MapPreparation(group);
        MapExports(group);
    }

    private static void MapExports(RouteGroupBuilder group)
    {
        group.MapGet("/teams/{teamId:guid}/roster.csv", async (Guid clubId, Guid teamId, Guid seasonId, HttpContext context, SportService service, CancellationToken ct) =>
            Results.File(await service.ExportTeamRosterAsync(context.User, clubId, teamId, seasonId, ct), "text/csv; charset=utf-8", "Pino-current-team-roster.csv"));
        group.MapGet("/seasons/{seasonId:guid}/roster.csv", async (Guid clubId, Guid seasonId, HttpContext context, SportService service, CancellationToken ct) =>
            Results.File(await service.ExportSeasonRosterAsync(context.User, clubId, seasonId, ct), "text/csv; charset=utf-8", "Pino-current-season-roster.csv"));
        group.MapGet("/tryouts/{tryoutId:guid}/results.csv", async (Guid clubId, Guid tryoutId, Guid? editionId, HttpContext context, SportService service, CancellationToken ct) =>
            Results.File(await service.ExportTryoutResultsAsync(context.User, clubId, tryoutId, editionId, ct), "text/csv; charset=utf-8", editionId.HasValue ? $"Pino-recorded-results-{editionId}.csv" : "Pino-current-tryout-results.csv"));
        group.MapGet("/players/{playerId:guid}/personal-data", async (Guid clubId, Guid playerId, HttpContext context, SportService service, CancellationToken ct) =>
            Results.File(await service.ExportPlayerAsync(context.User, clubId, playerId, ct), "application/json", "Pino-player-personal-data.json"));
    }

    private static void MapPreparation(RouteGroupBuilder group)
    {
        group.MapPost("/tryouts/{tryoutId:guid}/enrollments/batch", (Guid clubId, Guid tryoutId, BulkEnrollmentChangeInput input, HttpContext context, SportService service, CancellationToken ct) => service.ChangeEnrollmentsAsync(context.User, clubId, tryoutId, input, ct));
        group.MapPost("/teams/{teamId:guid}/targets", async (Guid clubId, Guid teamId, TeamTargetsInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveTeamTargetsAsync(context.User, clubId, teamId, input, ct)).ToReply());
        group.MapGet("/tryouts/{tryoutId:guid}/enrollment-candidates", (Guid clubId, Guid tryoutId, HttpContext context, SportService service, CancellationToken ct) => service.GetEnrollmentCandidatesAsync(context.User, clubId, tryoutId, ct));
        group.MapPost("/tryouts/{tryoutId:guid}/bulk-enrollment", (Guid clubId, Guid tryoutId, BulkEnrollmentInput input, HttpContext context, SportService service, CancellationToken ct) => service.EnrollBulkAsync(context.User, clubId, tryoutId, input, ct));
        group.MapGet("/seasons/{seasonId:guid}/team-availability", (Guid clubId, Guid seasonId, Guid? tryoutId, HttpContext context, SportService service, CancellationToken ct) => service.GetTeamAvailabilityAsync(context.User, clubId, seasonId, tryoutId, ct));
        group.MapPost("/seasons/{seasonId:guid}/team-availability", async (Guid clubId, Guid seasonId, Guid? tryoutId, TeamAvailabilityInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveTeamAvailabilityAsync(context.User, clubId, seasonId, tryoutId, input, ct)).ToReply());
        group.MapGet("/tryouts/{tryoutId:guid}/returning", (Guid clubId, Guid tryoutId, Guid sourceSeasonId, HttpContext context, SportService service, CancellationToken ct) => service.GetReturningPlayersAsync(context.User, clubId, tryoutId, sourceSeasonId, ct));
        group.MapPost("/tryouts/{tryoutId:guid}/returning", (Guid clubId, Guid tryoutId, ReturningPlacementInput input, HttpContext context, SportService service, CancellationToken ct) => service.PlaceReturningPlayersAsync(context.User, clubId, tryoutId, input, ct));
    }

    private static async Task<IResult> GetPhotoAsync(Guid clubId, Guid playerId, HttpContext context, SportService service, IProfilePhotoStore photos, CancellationToken ct)
    {
        var key = await service.GetPhotoKeyAsync(context.User, clubId, playerId, ct);
        if (key is null) { return Results.NotFound(); }
        try { return Results.File(await photos.DownloadAsync(key, ct), "image/jpeg"); }
        catch (RequestFailedException exception) when (exception.Status == 404) { return Results.NotFound(); }
    }
}
