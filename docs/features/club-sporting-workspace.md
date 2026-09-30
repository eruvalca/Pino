# Club sporting workspace

This feature completes the persisted player → season → tryout → decision workflow.
The implementation decisions below use the owner's authorization to resolve open
v1 behavior without further questions. They extend the Sideline notebook system.

## Scope and decisions

- Coaches and administrators can manage players, seasons, teams, enrollment,
  observations and decisions in their own club. Membership is checked on every
  server operation. Club access management remains administrator-only.
- A campaign is called a tryout; it belongs to one season. Teams also belong to
  one season. Dates are inclusive, and tryout dates fall within that season.
- Players require first name, last name and high-school graduation year. Position,
  contact email and photo are optional. A club-local player reference distinguishes
  people with the same name and year. Records can be archived and restored.
- CSV uses PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail.
  Imports create new players, never silently update existing ones. All rows must
  validate before any are saved; duplicate references are reported with row numbers.
- Archived seasons are read-only; staff may reopen them to make corrections.
  Team and player identity are retained instead of deleting historical records.
- Notes are shared. Authors can append a correction to their own note; the earlier
  text remains in history. Other staff cannot rewrite someone else's observation.
- Decisions are Awaiting, Placed, Withdrawn or Not selected. Completion is derived
  from a nonempty roster with no Awaiting decisions. Reopening one decision reopens
  the tryout. Empty tryouts are clearly marked as needing players.
- A new placement replaces the current team in that season and preserves history.
  Withdrawing/reopening a decision clears a current placement only when that
  tryout created it; a placement from another tryout remains. The UI explains this.
- Edits carry revisions. A stale write is rejected and asks staff to reload; it
  never silently overwrites a colleague's decision. Writes and membership changes
  share a database transaction lock to keep tenant/role checks valid during writes.

## Use and limits

The club's **Players** page opens the catalog, individual editor and CSV import.
**Seasons & teams** opens season configuration; each season links its team rosters
and tryouts. Within a tryout, **Add players** enrolls catalog records, and selecting
a roster entry opens its shared notebook. Player records and team pages retain
current placements and the decision history that produced them.

- Catalog and enrollment searches show 50 players per page. A tryout supports up
  to 2,000 participants. Filtering an enrolled roster does not remove anyone.
- Player references allow up to 40 ASCII letters, digits, dashes and underscores.
  Names and positions allow 80 characters; graduation years are 2000–2100.
- CSV uploads use UTF-8, the six template headers (case-insensitive), at most
  500 players and 500,000 characters. The browser rejects files over 1 MB.
  Quoted commas are supported. Preview makes no changes; import validates again
  against current records and commits the entire valid batch together.
  Every row must contain exactly six fields. Database retries recognize a batch
  that already committed; a new submission of existing references remains invalid.
- Player photos accept JPEG/PNG up to 5 MB and 4,096 pixels on either side.
  The server stores only a normalized 512 × 512 center crop, with metadata
  removed, behind club membership checks. EXIF rotation and mirroring are applied
  before cropping. Player photos need no separate crop step.
- Notes allow 4,000 characters; decision context allows 1,000. Corrections append
  a new version of the author's latest note. Earlier versions remain readable.
- An archived player stays in existing tryouts and history but cannot be newly
  enrolled. A team with current players cannot be archived. Record names remain
  unique within their club or season even when archived.
- Unsaved drafts survive switching players and failed saves within the current
  tryout page. Navigating away or reloading loses them. After a conflict, refresh
  saved decisions, review the current result, then submit the intended revision.
  After an uncertain note or decision save, retry first checks the saved operation.
  An unchanged retry acknowledges that save without duplicating it. Newer edits
  remain available for review and another save; note edits become a correction
  when the saved note is still correctable, otherwise a new note. Retry IDs cannot
  acknowledge different content as saved.

The ordinary unit/component suites cover validation and UI state. The opt-in
Playwright project exercises the same UI against Aspire's PostgreSQL, storage
and Mailpit resources, including conflicting writes, atomic import rejection,
season independence and membership revocation. See [test setup](../../tests/README.md).

## Direction contract

THESIS: One working club notebook connects roster preparation with field-side
evaluation. Actual player and season records lead; no decorative metric dashboard.

OWN-WORLD: Inherit DESIGN.md's navy masthead, cobalt actions, white sheets, fine
rules, system typography and explicit focus. Retain the established evaluation
roster/notebook pattern and stack readable records on phones.

STORY: Staff see their seasons, prepare teams and players, enroll a roster, then
write observations and decisions. Historical context stays one link away.

FIRST VIEWPORT: Club navigation precedes a clear page heading and its primary
action. Search and filters sit immediately above ruled records. Tryout work has
a compact roster on the left and the selected player's notebook on the right.

FORM: Established-world extension, selected under the user's unattended-decision
authorization. Code-led implementation reuses the incumbent composition. Signature
interaction: choosing a player opens their notebook and keeps the roster context;
saved decisions update completion and current placement together. Reduced motion
retains all feedback without animated transitions.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Implementation sequence

1. Persist sporting records, enforce tenancy, validation and concurrent decisions.
2. Add player catalog/import, season/team configuration and real tryout workflows.
3. Integrate navigation, account styling, history, meaningful empty/error states.
4. Validate unit/component tests, PostgreSQL persistence and browser workflows.
5. Complete Impeccable finish review and reconcile product/setup/feature documents.
