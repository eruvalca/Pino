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
            catch (AntiforgeryValidationException) { return Results.BadRequest(new SportReply(SportReplyKind.Invalid, "Your session changed. Reload before saving.")); }
            catch (UnauthorizedAccessException) { return Results.StatusCode(StatusCodes.Status403Forbidden); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        group.MapGet("/overview", (Guid clubId, HttpContext context, SportService service, CancellationToken ct) => service.GetOverviewAsync(context.User, clubId, ct));
        group.MapGet("/seasons/{seasonId:guid}/review", (Guid clubId, Guid seasonId, HttpContext context, SportService service, CancellationToken ct) => service.GetSeasonReviewAsync(context.User, clubId, seasonId, ct));
        group.MapGet("/tryouts/{tryoutId:guid}/review", (Guid clubId, Guid tryoutId, HttpContext context, SportService service, CancellationToken ct) => service.GetTryoutReviewAsync(context.User, clubId, tryoutId, ct));
        group.MapPost("/tryouts/{tryoutId:guid}/close", async (Guid clubId, Guid tryoutId, CloseTryoutInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.CloseTryoutAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/reopen", async (Guid clubId, Guid tryoutId, ReopenTryoutInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.ReopenTryoutAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapGet("/teams/{teamId:guid}", (Guid clubId, Guid teamId, HttpContext context, SportService service, CancellationToken ct) => service.GetTeamAsync(context.User, clubId, teamId, ct));
        group.MapGet("/players", (Guid clubId, string query, bool archived, int page, HttpContext context, SportService service, CancellationToken ct) => service.GetPlayersAsync(context.User, clubId, query, archived, page, ct));
        group.MapGet("/players/{playerId:guid}", (Guid clubId, Guid playerId, HttpContext context, SportService service, CancellationToken ct) => service.GetPlayerAsync(context.User, clubId, playerId, ct));
        group.MapGet("/tryouts/{tryoutId:guid}", (Guid clubId, Guid tryoutId, HttpContext context, SportService service, CancellationToken ct) => service.GetTryoutAsync(context.User, clubId, tryoutId, ct));
        group.MapPost("/players", async (Guid clubId, PlayerInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SavePlayerAsync(context.User, clubId, input, ct)).ToReply());
        group.MapPost("/seasons", async (Guid clubId, SeasonInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveSeasonAsync(context.User, clubId, input, ct)).ToReply());
        group.MapPost("/teams", async (Guid clubId, TeamInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveTeamAsync(context.User, clubId, input, ct)).ToReply());
        group.MapPost("/tryouts", async (Guid clubId, TryoutInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveTryoutAsync(context.User, clubId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/players", async (Guid clubId, Guid tryoutId, EnrollmentInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.EnrollAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/bib", async (Guid clubId, Guid tryoutId, BibNumberInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.SaveBibNumberAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/decisions", async (Guid clubId, Guid tryoutId, DecisionInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.DecideAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/tryouts/{tryoutId:guid}/notes", async (Guid clubId, Guid tryoutId, NoteInput input, HttpContext context, SportService service, CancellationToken ct) => (await service.AddNoteAsync(context.User, clubId, tryoutId, input, ct)).ToReply());
        group.MapPost("/import", (Guid clubId, ImportInput input, HttpContext context, SportService service, CancellationToken ct) => service.ImportAsync(context.User, clubId, input, ct));
        group.MapGet("/template", async (Guid clubId, HttpContext context, SportService service, CancellationToken ct) =>
        {
            await service.GetPlayersAsync(context.User, clubId, "", archived: false, 0, ct);
            return Results.File(Encoding.UTF8.GetBytes(PlayerCsv.Template), "text/csv", "Pino-players-template.csv");
        });
        group.MapGet("/players/{playerId:guid}/photo", GetPhotoAsync);
    }

    private static async Task<IResult> GetPhotoAsync(Guid clubId, Guid playerId, HttpContext context, SportService service, IProfilePhotoStore photos, CancellationToken ct)
    {
        var key = await service.GetPhotoKeyAsync(context.User, clubId, playerId, ct);
        if (key is null) { return Results.NotFound(); }
        try { return Results.File(await photos.DownloadAsync(key, ct), "image/jpeg"); }
        catch (RequestFailedException exception) when (exception.Status == 404) { return Results.NotFound(); }
    }
}
