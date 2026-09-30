using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Clubs.Data;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

internal sealed partial class ClubService
{
    private const int PageSize = 20;

    private static ClubSummary Summary(Club club) => new(club.Id, club.Name, club.Sport, club.City, club.State, club.Revision);
    private static Uri PhotoUrl(string userId, string key) => new($"/api/clubs/photos/{Uri.EscapeDataString(userId)}?v={Uri.EscapeDataString(key)}", UriKind.Relative);

    internal async Task<AccessSnapshot> GetAccessAsync(ClaimsPrincipal actor, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var id = await VerifiedActorAsync(db, actor, cancellationToken);
        var profile = await db.ClubProfiles.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == id, cancellationToken);
        var member = await db.ClubMemberships.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == id, cancellationToken);
        var request = await db.ClubJoinRequests.AsNoTracking().Where(value => value.UserId == id)
            .OrderByDescending(value => value.CreatedAt).ThenByDescending(value => value.Id).FirstOrDefaultAsync(cancellationToken);
        MembershipSummary? membership = member is null ? null : new(Summary(await db.Clubs.SingleAsync(value => value.Id == member.ClubId, cancellationToken)), member.Role);
        RequestSummary? recent = request is null ? null : new(request.Id,
            Summary(await db.Clubs.SingleAsync(value => value.Id == request.ClubId, cancellationToken)), request.Status, request.CreatedAt);
        return new(new(profile?.FirstName ?? "", profile?.LastName ?? "", profile?.PhotoKey is { } key ? PhotoUrl(id, key) : null), membership, recent);
    }

    internal async Task<ClubSearchPage> SearchAsync(ClaimsPrincipal actor, string query, int page, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await VerifiedActorAsync(db, actor, cancellationToken);
        var term = query.Trim();
        page = Math.Clamp(page, 0, 10000);
        if (term.Length is < 2 or > 120)
        {
            return new([], page, HasMore: false);
        }
        // Escape LIKE metacharacters so search input remains a literal substring.
        var pattern = "%" + term.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
        var clubs = await db.Clubs.AsNoTracking().Where(value => EF.Functions.ILike(value.Name, pattern, "\\"))
            .OrderBy(value => value.Name).ThenBy(value => value.Id).Skip(page * PageSize).Take(PageSize + 1)
            .ToListAsync(cancellationToken);
        return new(clubs.Take(PageSize).Select(Summary).ToArray(), page, clubs.Count > PageSize);
    }

    private static async Task RequireAdministratorAsync(ApplicationDbContext db, string userId, Guid clubId, CancellationToken cancellationToken)
    {
        if (!await db.ClubMemberships.AnyAsync(value => value.UserId == userId && value.ClubId == clubId && value.Role == ClubRole.Administrator, cancellationToken))
        {
            throw new UnauthorizedAccessException();
        }
    }

    internal async Task<PeoplePage> GetPeopleAsync(ClaimsPrincipal actor, Guid clubId, bool requests, int page, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var id = await VerifiedActorAsync(db, actor, cancellationToken);
        await RequireAdministratorAsync(db, id, clubId, cancellationToken);
        page = Math.Clamp(page, 0, 10000);
        var people = requests
            ? from request in db.ClubJoinRequests
              join profile in db.ClubProfiles on request.UserId equals profile.UserId
              where request.ClubId == clubId && request.Status == JoinRequestStatus.Pending
              orderby request.CreatedAt, request.Id
              select new PersonRow(profile.UserId, profile.FirstName, profile.LastName, profile.PhotoKey!, ClubRole.Coach, request.Id, request.CreatedAt)
            : from member in db.ClubMemberships
              join profile in db.ClubProfiles on member.UserId equals profile.UserId
              where member.ClubId == clubId
              orderby profile.LastName, profile.FirstName, profile.UserId
              select new PersonRow(profile.UserId, profile.FirstName, profile.LastName, profile.PhotoKey!, member.Role, RequestId: null, member.JoinedAt);
        var items = await people.Skip(page * PageSize).Take(PageSize + 1).ToListAsync(cancellationToken);
        var rows = items.Take(PageSize).Select(value => new PersonSummary(value.UserId, value.FirstName, value.LastName, PhotoUrl(value.UserId, value.PhotoKey), value.Role, value.RequestId, value.Since)).ToArray();
        return new(Summary(await db.Clubs.SingleAsync(value => value.Id == clubId, cancellationToken)), rows, page, items.Count > PageSize);
    }

    internal async Task<string?> GetPhotoKeyAsync(ClaimsPrincipal actor, string targetId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var id = await VerifiedActorAsync(db, actor, cancellationToken);
        if (!string.Equals(id, targetId, StringComparison.Ordinal))
        {
            var membership = await db.ClubMemberships.SingleOrDefaultAsync(value => value.UserId == id, cancellationToken);
            if (membership is null)
            {
                return null;
            }
            var sameClub = await db.ClubMemberships.AnyAsync(value => value.UserId == targetId && value.ClubId == membership.ClubId, cancellationToken);
            var applicant = membership.Role == ClubRole.Administrator && await db.ClubJoinRequests.AnyAsync(
                value => value.UserId == targetId && value.ClubId == membership.ClubId && value.Status == JoinRequestStatus.Pending, cancellationToken);
            if (!sameClub && !applicant)
            {
                return null;
            }
        }
        return await db.ClubProfiles.Where(value => value.UserId == targetId).Select(value => value.PhotoKey).SingleOrDefaultAsync(cancellationToken);
    }

    private sealed record PersonRow(string UserId, string FirstName, string LastName, string PhotoKey, ClubRole Role, Guid? RequestId, DateTimeOffset Since);
}
