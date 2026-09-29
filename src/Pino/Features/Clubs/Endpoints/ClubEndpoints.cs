using Azure;
using Microsoft.AspNetCore.Antiforgery;
using Pino.Features.Clubs.Services;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Endpoints;

internal static class ClubEndpoints
{
    internal static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/clubs").RequireAuthorization();
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
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest(new ClubReply(ClubReplyKind.Forbidden, "Your session changed. Reload the page before saving."));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Json(new ClubReply(ClubReplyKind.Forbidden, "You no longer have access to this operation."), statusCode: StatusCodes.Status403Forbidden);
            }
        });
        group.MapGet("/token", (HttpContext context, IAntiforgery antiforgery) =>
            Results.Ok(antiforgery.GetAndStoreTokens(context).RequestToken));
        group.MapGet("/access", (HttpContext context, ClubService service, CancellationToken ct) => service.GetAccessAsync(context.User, ct));
        group.MapGet("/search", (string query, int page, HttpContext context, ClubService service, CancellationToken ct) => service.SearchAsync(context.User, query, page, ct));
        group.MapGet("/{clubId:guid}/people", (Guid clubId, bool requests, int page, HttpContext context, ClubService service, CancellationToken ct) => service.GetPeopleAsync(context.User, clubId, requests, page, ct));
        group.MapPost("/profile", async (ProfileInput input, HttpContext context, ClubService service, CancellationToken ct) =>
            (await service.SaveProfileAsync(context.User, input, ct)).ToReply());
        group.MapPost("/create", async (CreateClubInput input, HttpContext context, ClubService service, CancellationToken ct) =>
            (await service.CreateAsync(context.User, input, ct)).ToReply());
        group.MapPost("/{clubId:guid}/requests", async (Guid clubId, HttpContext context, ClubService service, CancellationToken ct) =>
            (await service.RequestAsync(context.User, clubId, ct)).ToReply());
        group.MapPost("/requests/{requestId:guid}/cancel", async (Guid requestId, HttpContext context, ClubService service, CancellationToken ct) =>
            (await service.CancelAsync(context.User, requestId, ct)).ToReply());
        group.MapPost("/{clubId:guid}/leave", async (Guid clubId, HttpContext context, ClubService service, CancellationToken ct) =>
            (await service.LeaveAsync(context.User, clubId, ct)).ToReply());
        group.MapPost("/{clubId:guid}/decide", async (Guid clubId, RequestDecisionInput input, HttpContext context, ClubService service, CancellationToken ct) =>
            (await service.DecideAsync(context.User, clubId, input, ct)).ToReply());
        group.MapPost("/{clubId:guid}/members", async (Guid clubId, MemberChangeInput input, HttpContext context, ClubService service, CancellationToken ct) =>
            (await service.ChangeMemberAsync(context.User, clubId, input, ct)).ToReply());
        group.MapGet("/photos/{userId}", GetPhotoAsync);
        group.MapGet("/personal-data", async (HttpContext context, ClubService service, CancellationToken ct) =>
            Results.File(await service.ExportAsync(context.User, ct), "application/json", "ClubPersonalData.json"));
    }

    private static async Task<IResult> GetPhotoAsync(string userId, HttpContext context, ClubService service, IProfilePhotoStore photos, CancellationToken cancellationToken)
    {
        var key = await service.GetPhotoKeyAsync(context.User, userId, cancellationToken);
        if (key is null)
        {
            return Results.NotFound();
        }
        try
        {
            return Results.File(await photos.DownloadAsync(key, cancellationToken), "image/jpeg");
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return Results.NotFound();
        }
    }
}
