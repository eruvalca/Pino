# Season review and tryout closeout

## Shaped brief

Mode: Operate. Coaches and administrators need to see the season's current teams,
find unfinished evaluations, and deliberately preserve a tryout's reviewed results.
The user authorized unattended product and design decisions for this work.

- A season review brings current team rosters, participating players without a
  current team, outstanding tryout decisions and closed results into one reading
  path. Count distinct players for season totals; count entries for tryout work.
- Review precedes Close. Closing requires at least one participant and a recorded
  decision for every participant. An explicit confirmation explains the lock.
  A stale review must reload before closing. Closing never changes placements.
- Both coaches and administrators may close and reopen, matching existing sporting
  permissions. Reopening requires a reason and an active season. Archive still
  locks the entire season; restoring a season does not reopen its closed tryouts.
- A closed tryout rejects enrollment, bib changes, decisions, notes/corrections and
  tryout metadata changes on the server. Catalog and team maintenance remain
  independent. Idempotent retries may acknowledge an earlier successful write.
- Each close records an immutable roster snapshot, including names, graduation
  years, bibs, outcomes and team names. Reopening retains that snapshot and records
  who reopened it, when and why. A later close creates another edition. Current
  season placements are explicitly separate from these historical results.

## Interaction and evidence

Extend Seasons & teams with a linked season review. Lead with unfinished work and
the season's dates/status, then show readable team rosters and unplaced players.
Each tryout links to its review. Tryout review shows outcomes with their bibs and
team names before its close action. Closed editions remain selectable after
reopening, with author and time, and never silently display live catalog values.
Empty seasons explain the next setup action; empty tryouts cannot close. Archived
seasons remain readable. Loading, revoked access, stale review and transport
failure preserve a clear recovery path. Long names and the existing 2,000-entry
tryout limit must remain usable with filters and normal document flow. Results
are paged in groups of 50; exports and closeout always include the complete set.
The review continues to a separate confirmation step with outcome totals and an
explicit acknowledgement. Back preserves the result filters and page.
When a notebook refresh discovers closure or season archival, it dismisses any
open enrollment panel and disables enrollment while preserving unsaved notes.
After the tryout or season reopens, enrollment stays dismissed until Add players
is selected again.

## Direction contract

THESIS: A season is a working record with an explicit signed-off tryout edition;
current teams and recorded outcomes have separate, plainly named sections.

OWN-WORLD: Preserve the Sideline notebook's navy masthead, cobalt actions, white
ruled sheets and readable sans-serif. Larger bib numerals anchor player identity.

STORY: See what remains, inspect every outcome, close deliberately, revisit the
dated record, and reopen with context when a correction is needed.

FIRST VIEWPORT: Season name and dates lead a full-width sheet. Outstanding tryouts
form a compact ruled list with direct Review actions. Team rosters follow in
aligned columns on desktop and a single reading path on phones. Tryout review
puts its state beside the title and its shown/total result count with the filters,
so filtering does not imply a change to the total roster. Close follows the results.

FORM: Considered seven structures: action queue, roster ledger, player matrix,
timeline, season notebook index, split review desk, and guided closeout. Surface
seed f20e2c5e dealt 5, 2 and 7. Selected 5, the season notebook index, using the
user's unattended-decision authorization: it joins existing season navigation
while exposing unfinished work. Code-led extension of the established notebook;
the signature moment is a dated closeout receipt and a restrained ink-settle
transition. Reduced motion retains every state and action without animation.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
