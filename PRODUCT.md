# Pino

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Pino serves youth sports club administrators and coaches. They maintain a club's
player catalog, organize seasonal tryout campaigns, evaluate players, and decide
which teams are appropriate for them.

Administrators manage club membership requests. Administrators and coaches
working a tryout record player notes and make team placement decisions. More
detailed permissions for other operations remain undecided.

## Product Purpose

Pino helps clubs manage the progression from a player catalog to completed
tryout decisions and appropriate team placements within a season. It preserves
placement history across tryouts and seasons as players progress through their
playing careers.

A tryout is complete once a decision has been made for every participating
player. A completed decision is placement on a team, withdrawal from the tryout,
or a club/coach decision not to place the player on any team. Completion does
not require every player to receive a team placement.

## Operating Context

A club organizes its players and teams through seasons, with distinct campaigns
or tryouts within those seasons. Staff working a tryout need to consult player
information, record pertinent observations for themselves and other staff, and
place players on eligible teams.

The core workflow is to configure a season and its tryouts and teams, add players
individually or by CSV import, evaluate the participating players, record their
decisions, and later consult historical placements. A player's team can change
between seasons; a current team assignment must not replace earlier history.

The initial focus is soccer, with workflows intended to support other youth
sports. Staff are expected to use phones or tablets during tryouts and computers
afterward. Evaluation uses shared notes rather than scores or a formal rubric.
The first version can require an internet connection; offline note capture and
synchronization are outside its scope. Existing tools being replaced and
typical roster sizes have not been established.

## Capabilities and Constraints

These are confirmed product requirements. The repository currently provides a
Blazor starter and account infrastructure; the club and tryout workflows below
are planned capabilities, not claims of implemented behavior.

### Clubs and membership

- A **club** is the organization that owns its teams and player catalog and acts
  as the tenant for its associated data and user memberships.
- Club data and access must respect that tenant boundary. Administrators and
  coaches are members of the clubs they work with.
- New user onboarding requires a profile image upload and the ability to crop
  that image.
- Newly registered users can create their own club or search for an existing
  club and request to join it.
- Club administrators can view membership requests and approve or deny them.
- Whether users can belong to multiple clubs, and the exact role and permission
  model within each club, remain open decisions.

### Players

- A club maintains a persistent catalog of players.
- Staff can create a player individually, with an optional player photo.
- Staff can import players in bulk from a CSV file using **CsvHelper**.
- Required player fields, the CSV schema, duplicate matching, and import error
  handling are not yet specified.

### Seasons, tryouts, and teams

- Staff can create and configure seasons, tryouts, and teams.
- A season can contain distinct campaigns or tryouts. Whether "campaign" and
  "tryout" are interchangeable terms or separate entities remains undecided.
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

### Evaluation and decisions

- Administrators and coaches working a tryout can create notes for a player to
  communicate with other staff or record pertinent information.
- Evaluation uses shared notes only; scores and evaluation rubrics are outside
  the current scope. Note editing rules remain undecided.
- Those staff can place a player on an appropriate, compatible team during the
  tryout. Graduation year eligibility must be enforced.
- Every participating player needs a decision before the tryout is complete.
- The completing outcomes are placement on a team, withdrawal from the tryout,
  and a club/coach decision not to place the player on any team.
- Coaches and administrators working a tryout can finalize and revise player
  outcomes directly; a separate administrator approval step is not required.
- A placement must respect the one-current-team-per-season rule. The effect of
  changing a decision in one tryout on an existing placement from another
  tryout in the same season remains to be defined.

### History

- Staff can view historical team placements across tryouts and seasons for
  players and teams.
- Historical placements must remain available when players move to different
  teams in later seasons.
- The treatment of corrections to past decisions, archived records, and
  historical team identity remains to be defined.

### Implementation requirements

- Keep the existing Blazor web application and its server/browser boundaries.
  Repository architecture and authoring constraints are defined in
  [AGENTS.md](AGENTS.md); setup and runtime workflows are in [README.md](README.md).
- Use **CsvHelper** for player CSV import.
- Use **Aspire.Azure.Storage.Blobs** and **Azure.Storage.Blobs** for the required
  user profile image storage integration, and **Cropper.Blazor** for cropping.
  Package selection is part of the brief; installation and exact versions are
  deferred to implementation.
- User profile images are required during new user onboarding. Player photos
  are optional. These are distinct requirements.

## Evidence on Hand

The initial product brief establishes the users, tenant model, seasonal tryout
workflow, graduation year eligibility rule, membership approval, image and import
requirements, and placement history.

The current sample pages and starter styling are technical scaffolding, not an
approved product experience. No real player roster, sample CSV, evaluation
rubric, club imagery, or external product evidence was supplied during this
initialization. Future sample content must be clearly identified as such.

## Product Principles

1. **Keep club boundaries explicit.** Player records, team structures, staff
   memberships, and tryout work belong to their club.
2. **Respect eligibility when placing players.** Team selection must honor the
   graduation year constraint alongside staff evaluation.
3. **Make completion depend on decisions.** A tryout is complete only when every
   participating player has a recorded decision.
4. **Preserve progression over time.** New placements must retain the context of
   earlier tryouts and seasons.
5. **Support staff collaboration.** Player notes and placement workflows support
   the administrators and coaches working the tryout.
