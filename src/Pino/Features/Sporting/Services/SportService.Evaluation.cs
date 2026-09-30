using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<SportOutcome> EnrollAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, EnrollmentInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, _, token) =>
        {
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            if (tryout is null || await SeasonAsync(db, clubId, tryout.SeasonId, token) is not { Archived: false })
            {
                return new SportOutcome.Invalid("This tryout is unavailable or its season is archived.");
            }
            if (!await db.Players.AnyAsync(value => value.ClubId == clubId && value.Id == input.PlayerId && !value.Archived, token))
            {
                return new SportOutcome.Invalid("Choose an active player from this club.");
            }
            var bib = (input.Bib ?? "").Trim();
            if (bib.Length > 20) { return new SportOutcome.Invalid("Use a bib number or label of up to 20 characters."); }
            if (await db.Participations.AnyAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId, token))
            {
                return new SportOutcome.Conflict("This player is already in the tryout. Reload the roster.");
            }
            if (bib.Length > 0 && await db.Participations.AnyAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.Bib == bib, token))
            {
                return new SportOutcome.Invalid("That bib is already in this tryout. Choose another label or leave it empty.");
            }
            if (await db.Participations.CountAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId, token) >= 2000)
            {
                return new SportOutcome.Invalid("This tryout has reached 2,000 players. Create another tryout for additional players.");
            }
            db.Participations.Add(new() { ClubId = clubId, TryoutId = tryoutId, PlayerId = input.PlayerId, Bib = bib, Revision = 1 });
            if (!await db.SeasonPlacements.AnyAsync(value => value.ClubId == clubId && value.SeasonId == tryout.SeasonId && value.PlayerId == input.PlayerId, token))
            {
                db.SeasonPlacements.Add(new() { ClubId = clubId, SeasonId = tryout.SeasonId, PlayerId = input.PlayerId });
            }
            return new SportOutcome.Saved("Player added to the tryout.", input.PlayerId);
        }, ct);

    internal Task<SportOutcome> DecideAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, DecisionInput input, CancellationToken ct) =>
        WriteAsync(actor, clubId, async (db, actorId, token) =>
        {
            if (!SportRules.ValidDecision(input))
            {
                return new SportOutcome.Invalid("Choose a decision and, for placement, a team. Keep the reason within 1,000 characters.");
            }
            var saved = await db.DecisionEvents.SingleOrDefaultAsync(value => value.Id == input.OperationId && value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId, token);
            if (saved is not null) { return ReplayedDecision(saved, input); }
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            var season = tryout is null ? null : await SeasonAsync(db, clubId, tryout.SeasonId, token);
            if (tryout is null || season?.Archived != false) { return new SportOutcome.Invalid("Reopen the season before changing decisions."); }
            var entry = await db.Participations.SingleOrDefaultAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId, token);
            if (entry is null) { return new SportOutcome.Invalid("This player is not enrolled in this tryout."); }
            var placement = await db.SeasonPlacements.SingleAsync(value => value.ClubId == clubId && value.SeasonId == season.Id && value.PlayerId == input.PlayerId, token);
            if (entry.Revision != input.Revision || placement.Revision != input.PlacementRevision) { return new SportOutcome.Conflict(StaleMessage); }
            var player = await db.Players.SingleAsync(value => value.ClubId == clubId && value.Id == input.PlayerId, token);
            SportTeam? team = null;
            if (input.TeamId is { } teamId)
            {
                team = await db.SportTeams.SingleOrDefaultAsync(value => value.ClubId == clubId && value.SeasonId == season.Id && value.Id == teamId && !value.Archived, token);
                if (team is null || !SportRules.Eligible(player.GraduationYear, team.GraduationYear))
                {
                    return new SportOutcome.Invalid("Choose an active team in this season whose graduation year is no later than the player's year.");
                }
            }
            return await ApplyDecisionAsync(db, actorId, input, tryout, season, entry, placement, team, token);
        }, ct);

    private static SportOutcome ReplayedDecision(DecisionEvent saved, DecisionInput input) =>
        saved.Kind == input.Kind && saved.TeamId == input.TeamId && string.Equals(saved.Reason, input.Reason?.Trim() ?? "", StringComparison.Ordinal)
            ? new SportOutcome.Saved("This decision was already saved. The latest record is shown.", input.PlayerId)
            : new SportOutcome.Conflict("The earlier decision was saved with different content. Review the saved decision before submitting your changes.");

    private async Task<SportOutcome> ApplyDecisionAsync(Pino.Data.ApplicationDbContext db, string actorId, DecisionInput input,
        TryoutEvent tryout, Season season, Participation entry, SeasonPlacement placement, SportTeam? team, CancellationToken token)
    {
        var previousTeam = placement.TeamId;
        entry.Decision = input.Kind;
        entry.TeamId = team?.Id;
        entry.Revision++;
        if (team is not null || placement.TryoutId == tryout.Id)
        {
            placement.TeamId = team?.Id;
            placement.TryoutId = team is null ? null : tryout.Id;
        }
        placement.Revision++;
        db.DecisionEvents.Add(new()
        {
            Id = input.OperationId,
            ClubId = tryout.ClubId,
            TryoutId = tryout.Id,
            PlayerId = input.PlayerId,
            TryoutName = tryout.Name,
            SeasonName = season.Name,
            Kind = input.Kind,
            TeamName = team?.Name,
            TeamId = team?.Id,
            PreviousTeamId = previousTeam,
            Author = await AuthorAsync(db, actorId, token),
            CreatedAt = time.GetUtcNow(),
            Reason = input.Reason?.Trim() ?? "",
        });
        var preserved = team is null && placement.TeamId is not null;
        return new SportOutcome.Saved(preserved ? "Decision saved. The current team from another tryout is unchanged." : "Decision and season placement saved.", input.PlayerId);
    }

    internal Task<SportOutcome> AddNoteAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, NoteInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, actorId, token) =>
        {
            if (input.Id == Guid.Empty || string.IsNullOrWhiteSpace(input.Text) || input.Text.Length > 4000) { return new SportOutcome.Invalid("Write an observation of 1 to 4,000 characters."); }
            var saved = await db.PlayerNotes.SingleOrDefaultAsync(value => value.Id == input.Id && value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId, token);
            if (saved is not null)
            {
                if (!string.Equals(saved.AuthorId, actorId, StringComparison.Ordinal) || saved.CorrectsId != input.CorrectsId || !string.Equals(saved.Text, input.Text.Trim(), StringComparison.Ordinal))
                {
                    return new SportOutcome.Conflict("The earlier note was saved with different content. Your edited draft has not been saved.");
                }
                return new SportOutcome.Saved("This note was already saved.", input.Id);
            }
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            if (tryout is null || await SeasonAsync(db, clubId, tryout.SeasonId, token) is not { Archived: false }) { return new SportOutcome.Invalid("Reopen the season before adding notes."); }
            if (!await db.Participations.AnyAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId, token))
            {
                return new SportOutcome.Invalid("This player is not enrolled in this tryout.");
            }
            if (input.CorrectsId is { } correctedId &&
                (!await db.PlayerNotes.AnyAsync(value => value.Id == correctedId && value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId && value.AuthorId == actorId, token) ||
                await db.PlayerNotes.AnyAsync(value => value.CorrectsId == correctedId, token)))
            {
                return new SportOutcome.Conflict("Only the author can correct the latest version of a note. Reload to see its current version.");
            }
            db.PlayerNotes.Add(new()
            {
                Id = input.Id,
                ClubId = clubId,
                TryoutId = tryoutId,
                PlayerId = input.PlayerId,
                Text = input.Text.Trim(),
                AuthorId = actorId,
                Author = await AuthorAsync(db, actorId, token),
                CreatedAt = time.GetUtcNow(),
                CorrectsId = input.CorrectsId,
            });
            return new SportOutcome.Saved(input.CorrectsId is null ? "Shared note saved." : "Correction saved. The earlier note remains in history.", input.Id);
        }, ct);
}
