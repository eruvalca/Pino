# Club sporting workspace

This feature completes the persisted player → season → tryout → decision workflow.
The implementation decisions below use the owner's authorization to resolve open
v1 behavior without further questions. They extend the Sideline notebook system.

## Scope and decisions

### Club staff administration

Administrators edit club details and manage **Staff invitations** and **Email
delivery** from People. Detail saves compare the version the administrator saw.
An invitation specifies Coach or Administrator, binds to a verified email address,
expires after seven days, and requires explicit acceptance with a complete staff
profile. Existing membership or a pending join request must be resolved first.
An accepted invitation cannot grant access again after a member leaves or is removed.
Resending rotates the secret and invalidates earlier links; revocation ends the
unused invitation. Resends are limited to once a minute and each club can queue
50 invitation emails per hour. Invitation secrets are hashed; pending email bodies
are protected with application data-protection keys and never returned by the API.

New join requests notify verified club administrators; approval or denial notifies
the requester. These notifications are queued in the same database transaction as
the access change. SMTP failures leave the saved access decision intact. Delivery
status distinguishes Pending, Sending, Sent, Failed and Cancelled. Sent means the
mail service accepted the message, not that a person read it. Delivery retries up
to five times with backoff, and administrators can retry a failed message. Expired,
used, revoked or superseded invitations are not retried.

Delivery leases recover after an interrupted worker. SMTP acknowledgement cannot
commit atomically with the database, so an interrupted send can produce a duplicate
email; invitation acceptance remains single-use. Delivered and cancelled message
bodies are cleared while delivery receipts remain. Personal account export omits
invitation secrets and protected bodies; account erasure removes invitations for
that account and its addressed delivery records. Confirming an account preserves a
local continuation link so invited staff can return to their invitation.

### Sporting permissions and records

- Coaches and administrators can manage players, seasons, teams, enrollment,
  observations and decisions in their own club. Membership is checked on every
  server operation. Club access management remains administrator-only.
- A campaign is called a tryout; it belongs to one season. Teams belong to the
  club and keep their identity across seasons. Dates are inclusive, and tryout
  dates fall within their season.
- Players require first name, last name and high-school graduation year. Middle
  name, primary/secondary position, contact email and photo are optional. Manual
  creation and CSV without a mapped source identifier generate a club-local
  import reference automatically. This separate
  import key distinguishes people with the same name and year and prevents repeat
  imports. It appears in CSV feedback, not ordinary player, enrollment or roster
  views. Records can be archived and restored.
- CSV maps source columns to PlayerReference,FirstName,LastName,GraduationYear,
  Position,ContactEmail,MiddleName,SecondaryPosition. Unrelated columns are ignored.
  Imports create new players, never silently update existing ones. All rows must
  validate before any are saved; duplicate references are reported with row numbers.
- Archived seasons are read-only; staff may reopen them to make corrections.
  Team and player identity are retained instead of deleting historical records.
- Notes are shared. Authors can append a correction to their own note; the earlier
  text remains in history. Other staff cannot rewrite someone else's observation.
- Decisions are Awaiting, Placed, Withdrawn, Not selected or Did not attend. A nonempty roster
  with no Awaiting decisions is ready to close; it remains editable until staff
  explicitly close the reviewed results. Empty tryouts need players. A closed
  tryout must be reopened with a reason before editing its decisions or roster.
  The [season review and closeout brief](season-review-closeout.md) defines the
  lock, permissions, review conflicts and preserved result editions.
- A new placement replaces the current team in that season and preserves history.
  Withdrawing/reopening a decision clears a current placement only when that
  tryout created it; a placement from another tryout remains. The UI explains this.
- Edits carry revisions. A stale write is rejected and asks staff to reload; it
  never silently overwrites a colleague's decision. Writes and membership changes
  share a database transaction lock to keep tenant/role checks valid during writes.

## Use and limits

All active club teams are available in every season and tryout by default,
including teams created later. Administrators use **Team availability** to exclude
a team for one season or tryout. Season exclusions apply to all its tryouts.
Re-inclusion is explicit and revision checked; saved decisions, rosters and
historical editions stay unchanged. A team record selects one season's roster
and exposes history across seasons. Targets are club-level advisory defaults.

Creating a tryout atomically includes all active catalog players, up to the
2,000-player retained-roster limit. An oversized catalog is rejected without a
partial roster. Entries start without a bib and await an explicit outcome.
Archived players are excluded. Later catalog additions are not added by editing
tryout details. **Manage tryout players** supports both roles with searchable,
paged selection and reviewed group exclusion/restoration. A reason is required;
earlier work stays in history, and per-player conflicts are reported.

**Include new catalog players** selects active catalog players individually, by graduation
cohort or across matching results, with up to 1,000 players in one reviewed batch.
Already enrolled players are identified, and removed entries require the reasoned
restoration flow. New entries start without a bib and await an explicit outcome.
Changed/archived/unavailable players and capacity conflicts produce per-player
skip results; the remaining valid selections can complete together.

**Review returning placements** uses the chosen earlier season's current teams
and the same club team identities. Players must be active, enrolled,
eligible, awaiting a decision and without an existing current target-season team.
The commit rechecks player, source placement, enrollment, current placement, target
team revisions and current availability. A concurrent change is skipped for another
review, preserving staff decisions. Successful placements use ordinary decision
history. Batch retry receipts retain counts, action, club, target season/tryout,
actor and time, without player identities or payloads; retries acknowledge the
earlier result and never recreate erased personal records.

The evaluator notebook shows the latest placement from a season starting before
the current season, with a direct same-team action when eligible and available.
That action uses ordinary decision concurrency checks and preserves unfinished
notes. Team roster exports require an explicit season; team history spans seasons.

Administrators set optional total and free-text position targets from each team
roster. Both roles see current totals and position coverage on team rosters and
season review. Primary positions count toward targets; secondary coverage is
shown separately and never adds to the total player count. Names are matched
without case or surrounding-space differences. Targets, including zero, are
advisory: shortages, excess players and position totals above the overall target
produce warnings without blocking an otherwise eligible placement.
Targets accept 0–2,000 players and up to 100 distinct position names of at most
80 characters. These are technical input bounds, not recommended roster sizes.
Targets belong to the club team. Editing requires an active team; revision conflicts preserve the
draft for review before reloading saved targets.

**Compare teams & player observations** in season review opens two selected
current rosters alongside unplaced season participants and a focused notebook.
Lists are filtered by name/graduation year and primary/secondary position and
paged in groups of 50. Only the selected player's history and chosen tryout's
observation bodies are loaded. Failed loads clear the previous player's text.
A temporary one-player placement preview recalculates advisory coverage across
the compared teams without changing saved rosters. It respects active records
and graduation eligibility and clears on refresh, player/team changes or leaving.
The notebook links to the selected player in the actual tryout for an explicit
decision; closed editions remain independent of this current-roster comparison.
On narrow screens, selecting a player opens their notebook. **Back to rosters**
and **Review preview coverage** restore focus to the compared rosters; **Return
to [player]** opens the selected notebook again. These view changes preserve the
selection, filters, roster pages and current preview without saving a placement.

### Public entry and account protection

`/` introduces Pino as the product and company and links staff signup, the
fictional sample and `/guide`. The guide explains setup, tryouts, decisions and
records without publishing support contacts or unapproved legal text. Signup
requires a server-validated acknowledgement that the account holder is an adult
acting as club staff, including external-account registration. Successful sign-in
defaults to `/club/access`; valid invitation return links remain intact.
The **Club workspace** navigation opens `/club`, which sends members with complete
profiles directly to their club overview. `/club/access` remains available for
profile and membership controls; staff without complete membership continue setup.

The club overview gives administrators a preparation checklist derived from the
active player catalog, seasons/teams, tryouts, retained enrollments and current
staff. Inviting other staff is optional. The checklist does not store a separate
progress flag or persistent note drafts.

Five failed passwords lock the account for 15 minutes. Account POST requests
are limited to 60 per minute per connection address, with HTTP 429 and Retry-After;
untrusted forwarded headers cannot choose a different bucket. Confirmation and
password-reset email each allow one attempt per recipient per minute and six
per rolling hour, through a bounded in-process cache of hashed recipient keys.
SMTP failures consume an attempt and remain visible to the caller; suppressed
repeats use the same public response as other requests without revealing account
existence. These in-process request/email counters reset when the app restarts.
Join requests are capped at five per account per rolling hour using persisted
request history, preventing repeated cancel/reapply notification floods.

Tryout filters combine name/bib, graduation year, outcome, current team, tryout
attendance, primary or secondary position, missing observations and a team from
an explicitly chosen comparison season. Missing observations means no unredacted
note anywhere in this tryout. Comparison uses that season's current placements,
not a closed edition. Filtering preserves the selected notebook and its in-page
draft; **Clear player filters** restores the full retained roster.
Position, observation and comparison controls sit in the native **Position,
notes & previous team** disclosure, leaving the ordinary roster filters
visible. Clearing filters keeps the chosen comparison season.

The club's **Players** page opens the catalog, individual editor and CSV import.
**Seasons & teams** opens season configuration; each season links its team rosters
and tryouts. **Review season** brings current rosters, players without a team and
tryout progress together. New tryouts include active catalog records automatically.
**Manage tryout players** reviews exclusions and restorations; administrators can
include later catalog additions. Selecting a roster entry opens its shared notebook.
Player records and team pages retain
current placements and the decision history that produced them. **Review results
& closeout** leads to the result ledger, explicit close confirmation and earlier
closed editions. Closed results remain distinct from current season placements.

### History and handoff

Player records list every tryout enrollment, including removed entries. Choosing
one loads only that player's saved attendance, observations and earlier versions,
redaction audit, and enrollment corrections. Existing cross-season placements
and decision history remain alongside this view. A failed history change clears
the previous selection's details and offers an explicit reload.

Both staff roles can download current team and season rosters as CSV. Season
exports include retained participants without a current team. Tryout exports use
the selected current or recorded results edition and include all its rows,
regardless of the on-screen filters. Recorded editions retain their recorded
names, bibs and outcomes; current exports use current catalog names. Exports
identify their type, export time and, for recorded results, edition and closeout
time. Ordinary CSV files omit contact email and observations, use UTF-8, and
protect spreadsheet cells from formula execution.

**Print attendance & bib list** shows the entire included tryout roster, ordered by name or bib. It includes saved attendance and a blank
paper check-in column; printing does not write attendance or decisions. Print
styles hide navigation and controls, repeat table headings, and keep rows intact.
Removed enrollments do not appear. Reload before printing to refresh saved data.

Administrators can download a player's comprehensive JSON package from **Player data**. It contains catalog fields, the saved JPEG photo, all saved
note versions and redaction metadata, attendance, enrollment changes, placements,
decision events and that player's recorded-edition copies. Redacted text remains
removed and other players' records are excluded. A photo retrieval failure fails
the download instead of silently producing an incomplete package. Permanent
erasure removes the player from future exports, including recorded editions.
All download endpoints recheck club access and send no-store cache headers.

### Working constraints and safeguards

- Catalog, enrollment correction and tryout roster lists show 50 players per
  page. A tryout supports up to 2,000 retained participants. Filtering a roster
  does not remove anyone. The intended working range is 200–1,000 players.
  For multi-page tryout rosters, paging appears above the rows and also below
  them on phones. Using the lower controls moves focus to the Players heading
  so staff can begin reading the new page.
- Roster reads omit observation and decision bodies; a distinct set of player IDs
  supplies the missing-observation filter. The selected player's
  notebook loads separately, clears previous-player content while loading, and
  exposes retry feedback. Saving a note refreshes only that notebook. Drafts
  remain in the current page only, including while changing roster filters.
- **Manage tryout players** is available to both staff roles, with paged search,
  group selection and a review before exclusion/restoration. A
  required reason records each removal/restoration with actor, time and bib.
  Removal excludes the entry from roster, attendance and completion totals;
  notes, attendance records, decisions and earlier closeout editions remain.
  A current season placement made by this tryout is cleared, while a placement
  from another tryout is preserved. Removed entries reject new sporting writes.
- Restoration requires an active catalog record and a currently available bib.
  It preserves the earlier work but resets the current outcome to Awaiting;
  staff must make a fresh final decision before closing. Neither operation is
  allowed in a closed tryout or archived season. Server revision checks and
  operation identifiers reject stale changes and acknowledge safe retries.
- Player references allow up to 40 ASCII letters, digits, dashes and underscores.
  Names and positions allow 80 characters; graduation years are 2000–2100.
- CSV uploads use UTF-8, mapped headers, at most 1,000 players and 2,000,000
  characters. The browser rejects files over 4 MB. Empty rows and unrelated
  columns are ignored; missing empty trailing columns are accepted. Quoted commas
  are supported. Header suggestions never infer graduation year from birth dates
  or school grades. Date-shaped source identifiers carry a review warning.
  Preview makes no changes; import validates again
  against current records and commits the entire valid batch together.
  Populated rows must align with the named source columns. Database retries recognize a batch
  that already committed. Active reference/name-and-graduation candidates are skipped
  with a link to the existing record. CSV never overwrites existing fields.
  Archived candidates require explicit administrator resolution: restore the same
  person, skip, or create a different person while strongly confirming erasure of
  the matching archived record. Middle names distinguish known different names;
  missing middle names are treated as possible matches, not proof of identity.
  Counts distinguish creation, skipping, reactivation, unresolved rows and errors.
  Preview and commit both validate candidates and their current revisions. A receipt
  containing counts, actor and time makes retries safe without retaining CSV content.
- Administrator erasure removes the player, notes and earlier versions, decisions,
  placements, enrollment and closed-result copies in one transaction. A non-identifying
  count explains missing personal content in a closed edition. A required full-name
  confirmation and separate acknowledgement distinguish erasure from archival.
  Photo removal uses durable cleanup; the receipt explicitly reports pending photo
  deletion and can be revisited after reload. Only club, actor and time remain once
  cleanup clears its temporary storage key.
- The player photo editor accepts JPEG/PNG up to 5 MB. Staff drag, scroll/pinch to
  zoom, or use keyboard-operable buttons to frame a square. **Use this crop**
  produces a 512 × 512 JPEG preview; **Adjust crop** returns to framing and
  **Discard photo change** restores the saved image. Save is blocked while a crop
  is pending, and failed saves retain the confirmed preview.
  Server image validation still bounds direct uploads to 5 MB and 4,096 pixels
  per side, applies EXIF orientation, removes metadata and normalizes them to a
  512 × 512 square. Images remain behind club membership checks.
- Notes allow 4,000 characters; decision context allows 1,000. Corrections append
  a new version of the author's latest note. Earlier versions remain readable
  unless an administrator redacts the observation. Redaction requires a reason
  and explicit confirmation, removes the entire correction chain's text, and
  retains the original authors/dates plus the redacting administrator, time and
  reason. It is available in closed tryouts and archived seasons. Redacted notes
  cannot be corrected or restored by replaying a prior save; refreshing the
  notebook clears affected correction drafts.
- A tryout has one date and optional location. There is no session setup,
  session selection or staff assignment. Season dates must include each tryout.
- Attendance is Not recorded, Present or Absent per tryout and player, with
  the recording staff member, time and revision. Both roles can change it in open
  tryouts and active seasons; stale competing changes require reload. Attendance
  never changes a decision or team. Missing attendance does not prevent closing
  a tryout whose included players all have explicit final decisions.
- Notes belong directly to the tryout. Corrections keep that context. Filtering
  attendance preserves the selected player and their unsaved note.
- Notes and all displayed staff activity show the current protected profile photo
  beside the historical name, with initials for absent or inaccessible photos.
  Decision and closeout history retain stable author IDs without Identity foreign
  keys. Deleted accounts or former staff cannot expose photos through history.
- An archived player stays in existing tryouts and history but cannot be newly
  enrolled. A team with current players in an active season cannot be archived.
  Archived-season history remains intact. Team names remain unique within their
  club even when archived.
- **Bib number** identifies a player within one tryout. Set it when adding the
  player, or use **Bib number (this tryout)** in the selected player's notebook
  to assign, edit or clear it afterward. It accepts up to 20 characters, is
  optional, and must be unique among nonempty bibs in that tryout. Other tryouts
  may reuse the number. The club-wide player reference is a separate CSV key.
  Bib saves compare the previously observed bib under the shared write lock;
  conflicting changes require **Reload saved bib**. Retrying an already-saved
  value succeeds without changing decision revisions. Bib drafts stay with their
  player while switching notebooks, and reloading a bib preserves other drafts.
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

STORY: Staff see their seasons, review club teams and automatically included players, then
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
