# Pino

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Pino serves adult youth sports club administrators and coaches. They maintain a club's
player catalog, organize seasonal tryout campaigns, evaluate players, and decide
which teams are appropriate for them.

Administrators manage club membership requests. Administrators and coaches
working a tryout record player notes and make team placement decisions. Both
roles maintain sporting records in their club.

## Product Purpose

Pino helps clubs manage the progression from a player catalog to completed
tryout decisions and appropriate team placements within a season. It preserves
placement history across tryouts and seasons as players progress through their
playing careers.

A tryout is ready to close once a decision has been made for every participating
player on a nonempty roster. A completed decision is placement on a team,
withdrawal from the tryout, a club/coach decision not to place the player on
any team, or an explicit Did not attend outcome. Attendance never determines a
selection decision automatically. Staff review the results and explicitly close the tryout to preserve
that edition. Closing does not require every player to receive a team placement.

## Operating Context

A club organizes its players and teams through seasons, with distinct campaigns
or tryouts within those seasons. Staff working a tryout need to consult player
information, record pertinent observations for themselves and other staff, and
place players on eligible teams.

The core workflow is to maintain club teams and players, configure a season and
its tryouts, evaluate the automatically included active players, record their
decisions, and later consult historical placements. A player's team can change
between seasons; a current team assignment must not replace earlier history.

The initial focus is soccer, with workflows intended to support other youth
sports. Staff are expected to use phones or tablets during tryouts and computers
afterward. Evaluation uses shared notes rather than scores or a formal rubric.
The first version can require an internet connection; offline note capture and
synchronization are outside its scope. A typical catalog has about 200 players;
the working capacity target is 1,000 players, all of whom may join one tryout.
Historical records accumulate across seasons. Existing club workflows combine
registration exports with spreadsheets of observations and projected rosters.
Returning players frequently stay on the same club team in the next season.

## Capabilities and Constraints

The application persists account and club access, player catalogs, seasons,
teams, tryout rosters, shared observations, decisions and placement history.
The [sporting workspace](docs/features/club-sporting-workspace.md) records the
implementation decisions made under the owner's authorization to complete v1.
The earlier fictional [tryout demonstration](docs/features/tryout-evaluation.md)
remains a separate, public sample and never reads or writes club records.

### Clubs and membership

- A **club** is the organization that owns its teams and player catalog and acts
  as the tenant for its associated data and user memberships.
- Club data and access must respect that tenant boundary. Administrators and
  coaches are members of the clubs they work with.
- New user onboarding requires first and last names, a profile image upload,
  and the ability to crop that image.
- Newly registered users can create their own club or search for an existing
  club and request to join it.
- A club's initial details are its name, free-text sport and city, and a selected
  US state. Club search
  shows those details without exposing members or players.
- A person can belong to only one club in the first version. There is no
  multi-club membership or club switcher.
- Creating a club makes its creator an administrator. Approved join requests
  become coach memberships. Administrators manage club access; coaches do club
  work. Both roles maintain players, seasons, teams, enrollment, notes and decisions.
- Club administrators can view membership requests and approve or deny them,
  change member roles between administrator and coach, and remove members.
- A person can have only one pending join request at a time and can cancel it
  to choose another club. Denied applicants and removed members may reapply.
- Members can leave their club themselves. The last administrator must promote
  another member before leaving or losing administrator access; removal or
  demotion must never leave a club without an administrator.
- Profile photos are private: visible to the person, their current club's staff,
  and administrators reviewing their pending request. Store only the saved
  square crop and delete replaced photos.
- Request status is available in the app. Invitations, new join requests, and
  approval/denial outcomes also have email notifications. Invitations expire,
  are single-use, and grant the specified role after matching email verification.
- Administrators can edit basic club details. Club closure and recovery are deferred.

The implemented flow and remaining product boundaries are recorded in the
[club onboarding and access brief](docs/features/club-onboarding-access.md).

### Players

- A club maintains a persistent catalog of players.
- Staff can create a player individually, with an optional player photo.
- Staff can import players in bulk from a CSV file using **CsvHelper**.
- Required user-entered fields are first name, last name and graduation year.
  Middle name, primary and secondary position, and contact email are optional.
  Manually created players receive an
  automatic club-local import reference; staff do not need to invent one.
- Guided CSV import maps supported columns, ignores unrelated columns and blank
  rows, and previews up to 1,000 players. Graduation year must be supplied and
  confirmed; birth dates do not determine it. Existing players are never updated
  through CSV. Active duplicates are skipped with a link to edit the existing
  record. Matching names and graduation years identify candidates, not proof of identity.
- An administrator can confirm an archived candidate is the same person and
  reactivate it, preserving history. For a confirmed different person with the
  same name and graduation year, import review can offer new-player creation
  together with strongly confirmed erasure of the archived record. This uses
  the same complete erasure operation as the standalone administrator action.
- Import reports distinguish created, skipped, reactivated, unresolved, and
  failed rows. Skipping duplicate creation does not prevent selecting that
  existing player for enrollment. Workbook layouts are workflow evidence;
  arbitrary multi-sheet workbook conversion and general record merging are deferred.
- Players may be archived and restored without losing notes or placements.
- Bib numbers belong to a player's entry in a particular tryout, not the club
  catalog record. They are optional and unique within that tryout; the same player
  can have a different bib in another tryout, and numbers can be reused there.
  Staff can assign, change or clear a bib during enrollment or evaluation. The
  player reference remains a separate catalog/import identifier for duplicate
  detection. It appears in CSV import feedback, not ordinary player or roster views.
- Member and player photo editors share a choose, frame, confirm, then save flow.
  Dragging, wheel/pinch zoom and keyboard-operable controls position the photo.
  **Use this crop** shows the exact square preview before the record is saved;
  staff can adjust or discard it. An unconfirmed crop blocks saving.

### Seasons, tryouts, and teams

- Staff can create and configure seasons, tryouts, and teams.
- Teams belong to the club and keep one identity across seasons. Active teams are
  available in all seasons and tryouts by default, including teams added later.
  Administrators can exclude teams for one season or one tryout. A season exclusion
  applies to all its tryouts; exclusions block new placements without changing saved records.
- New tryouts include every active catalog player, with a blank bib and Awaiting
  decision. Archived players are omitted. Editing a tryout does not re-enroll
  excluded players or automatically include later catalog additions.
- Administrators can include later catalog additions in reviewed batches and
  review bulk returning-player placements on the same club team. Both roles see
  the latest earlier-season placement in the notebook and can quickly place a
  player on that team when eligible and available. Prior seasons and concurrent
  edits stay protected. Season copying and team mapping are unnecessary.
- Optional total roster targets and position counts support planning. Targets
  warn without blocking eligible placements; position targets are optional.
- A season contains distinct tryouts; campaign is another name for a tryout,
  rather than a separate entity. Tryout dates must fall inside the season.
- Archived seasons preserve readable records and block sporting changes until
  staff reopen them. Teams with current players in active seasons cannot be archived.
- Team eligibility has a strict high school graduation year requirement: a
  player must graduate in the team's specified year **or later** to be placed
  on that team. For example, a threshold of 2030 permits 2030 and later years;
  it does not permit 2029.
- Multiple teams can serve the same graduation year at different skill levels.
  **Blue**, **Silver**, and **Elite** are examples of team designations, not a
  confirmed fixed list or ranking.
- A player's team membership can change across seasons.
- A player can have only one current team within a season. Replacements must
  preserve placement history rather than create simultaneous team memberships.
- A consolidated season review shows current team rosters, participating players
  without a current team, outstanding tryout decisions and recorded closeouts.
  Season player totals count distinct players, not their entries across tryouts.

### Evaluation and decisions

- Administrators and coaches working a tryout can create notes for a player to
  communicate with other staff or record pertinent information.
- Evaluation uses shared notes only; scores and evaluation rubrics are outside
  the current scope. Authors append corrections to their own notes, retaining
  the earlier text, authorship and timestamp. Other staff cannot rewrite a note.
- A tryout has one roster, one bib per player, one attendance mark per player,
  shared notes and one final decision per player. Its date and optional location
  describe the whole tryout. There are no tryout sessions or staff assignments.
- Both roles can mark attendance as Not recorded, Present or Absent. Attendance
  remains separate from the final decision, including Did not attend.
- Notes and displayed staff activity show a protected current profile photo beside
  the recorded name. Missing or inaccessible photos use initials. Historical names
  stay as recorded, and former staff photos do not bypass membership checks.
- Use short sentences and familiar words on every screen. Prefer notes, player
  list, results and permanent deletion in user-facing copy. Keep the adult-staff
  audience, accurate consequences and recovery instructions.
- Both roles can exclude players from a tryout or restore them, individually or
  in reviewed groups, with a required reason. Earlier work remains in history;
  excluded entries leave active and completion counts. Group changes report each
  changed record and any conflicts.
  Closed tryouts must first be reopened. Attendance never automatically selects
  an outcome, and absent players can still receive valid placements.
- Excluding a player clears a current placement only when this tryout made it.
  Reasoned restoration requires an active catalog player and an available bib;
  it preserves earlier work but starts a new outstanding decision and does not
  automatically restore a team. The enrollment review explains these consequences.
- Unsubmitted notes are disposable. Persistent draft recovery is outside scope.
- Those staff can place a player on an appropriate, compatible team during the
  tryout. Graduation year eligibility must be enforced.
- Every participating player needs a decision before the tryout can be closed.
- The completing outcomes are placement on a team, withdrawal from the tryout,
  a club/coach decision not to place the player on any team, and Did not attend.
- Coaches and administrators working a tryout can finalize and revise player
  outcomes directly; a separate administrator approval step is not required.
- A new placement replaces the current team within that season. A non-placement
  revision clears a team only if that same tryout created the assignment; a team
  from another tryout remains. Stale saves are rejected using record revisions.
- Empty tryouts need players. An open tryout becomes ready for review when every
  player has an outcome; adding a player or resetting a decision removes that
  readiness. Readiness alone does not close the tryout.
- Coaches and administrators can close reviewed results and reopen a closed
  tryout with a reason. Closing rejects stale reviews and locks enrollment, bibs,
  attendance, decisions, notes/corrections and tryout metadata. Catalog and team maintenance
  remain independent. Neither closing nor reopening changes current placements.
- Reopening requires an active season. Restoring an archived season does not
  automatically reopen its closed tryouts.

### History

- Staff can view historical team placements across tryouts and seasons for
  players and teams.
- Historical placements must remain available when players move to different
  teams in later seasons.
- Every saved decision is appended with actor, timestamp, reason, and the team,
  tryout and season names at that time. Current assignments are stored separately.
  Player and team pages expose history; renaming records does not rewrite it.
- Each closeout preserves the reviewed player names, graduation years, bibs,
  outcomes and team names, with tryout/season context and the closing actor/time.
  Reopening records its actor, time and reason while retaining that edition;
  closing again creates a new edition. Later catalog edits and season placements
  do not rewrite closed results. See the
  [closeout brief](docs/features/season-review-closeout.md).
- Player history brings placements, outcomes, and observations together with
  season/tryout/author context. Ordinary roster/results exports and printable
  attendance/bib lists are available to both roles. Closed editions remain
  distinct from current rosters.

### Administration and personal content

- Existing sporting permissions remain intact. New administrative configuration,
  bulk enrollment and returning-player placement controls, sensitive import
  resolutions, personal-data exports, erasure, and redaction are administrator-only.
  Both roles manage attendance and individual or group enrollment exclusions
  and restorations.
- Archival retains all content and is reversible. A separate strongly confirmed
  erasure removes personal content, photos, earlier versions, and snapshot copies,
  retaining minimal non-identifying audit information. No automatic age-based
  deletion applies until a retention policy is established.
- Exceptional administrator note redaction requires a reason and removes
  affected sensitive text from earlier versions too, retaining actor and time.
- Family offers and acceptance, production deployment/operating-model work,
  and club closure/recovery are outside this implementation scope.

### Public identity

Pino is both the product and company name. Public guidance explains the staff
workflow and links to the fictional demonstration. Do not invent individual
identities, support contacts, pricing, privacy policies, or terms text.

### Implementation requirements

- Keep the existing Blazor web application and its server/browser boundaries.
  Repository architecture and authoring constraints are defined in
  [AGENTS.md](AGENTS.md); setup and runtime workflows are in [README.md](README.md).
- Use **CsvHelper** for player CSV import.
- Use **Aspire.Azure.Storage.Blobs** and **Azure.Storage.Blobs** for the required
  user profile image storage integration, and **Cropper.Blazor** for cropping.
  Exact installed versions are centralized in `Directory.Packages.props`.
- User profile images are required during new user onboarding. Player photos
  are optional. These are distinct requirements.

## Evidence on Hand

The initial product brief establishes the users, tenant model, seasonal tryout
workflow, graduation year eligibility rule, membership approval, image and import
requirements, and placement history.

The tryout workspace follows the approved Sideline notebook direction recorded
in the merged feature brief and implemented visual system in `DESIGN.md`.
The persisted workspace extends that visual system. The owner supplied a private
2025–2026 registration CSV and planning workbook as contextual evidence. They
use legacy birth-year groupings; Pino uses graduation-year eligibility. Their
personal content must not be copied into fixtures, public samples, or source control. The
public sample uses clearly identified fictional players and observations;
its representative roster sizes are demonstration cases, not product limits.

## Product Principles

1. **Keep club boundaries explicit.** Player records, team structures, staff
   memberships, and tryout work belong to their club.
2. **Respect eligibility when placing players.** Team selection must honor the
   graduation year constraint alongside staff evaluation.
3. **Review before closing.** Every participating player needs a recorded decision
   before staff can deliberately preserve a closed edition.
4. **Preserve progression over time.** New placements must retain the context of
   earlier tryouts and seasons.
5. **Support staff collaboration.** Player notes and placement workflows support
   the administrators and coaches working the tryout.
