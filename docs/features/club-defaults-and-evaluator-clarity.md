# Club defaults and evaluator clarity

This work follows the owner's request for persistent club teams, automatic tryout
rosters and clearer choices throughout the application. It extends the existing
Sideline notebook system. The owner is unavailable and delegated implementation
decisions; the choices below resolve the remaining workflow details.

## Product behavior

- A team belongs to the club and keeps one identity across every season and
  tryout. Season placements remain separate. New seasons and tryouts include all
  active club teams by default, including teams added later. Remove the obsolete
  season-copy and team-correspondence setup steps.
- Administrators can exclude a team for one season or one tryout. A season
  exclusion also applies to its tryouts. Exclusions prevent new placements and
  leave existing decisions, rosters and historical editions intact. Re-inclusion
  is explicit; stale changes are rejected. Closed tryouts and archived seasons
  retain their editing protections.
- Creating a tryout includes every active catalog player atomically, with one
  shared entry and no assigned bib or selection decision. Archived players are
  not included. This is a creation-time roster: later catalog additions can be
  included together from roster management. Editing a tryout never re-enrolls
  someone staff excluded. No capacity overflow may silently truncate a roster.
- Both sporting roles can exclude or restore a participant with a reason and
  preserved history. Provide searchable, paged management and reviewed group
  exclusion/restoration, with accurate per-player conflicts and completion counts.
- Evaluation shows the most recent earlier-season placement and offers a direct
  same-team placement when eligible and available. The server checks current
  eligibility, team availability, enrollment and revisions. It preserves prior
  seasons, existing save feedback and explicit decisions.
- Team records let staff choose a season's roster and see history across seasons.
  Exports identify the chosen season. Club-level targets remain advisory defaults.
- Review all page families for action hierarchy and density. Use concise status
  summaries, grouped controls, contextual next actions and progressive disclosure
  for secondary instructions. Keep critical consequences beside sensitive actions.

## Delivery and evidence

- [x] Persistent club teams and scoped availability, with server and tenant guards.
- [x] Automatic active-player enrollment and practical roster exclusions/restoration.
- [x] Previous placement context, quick same-team action and simplified bulk return.
- [x] Obsolete season copying/mapping removed from code, navigation and guidance.
- [x] Evaluator-focused clarity across sporting, club, public and account screens.
- [x] Current supplied CSV inspected and exercised through actual import, with
  missing graduation years reported rather than inferred; private data stays local.
- [x] Realistic 200/1,000-player and multi-club/evaluator persistence verification.
- [x] Fresh initial migration and clean database creation verified through Aspire.
- [x] Full build, formatting/analyzers, headless tests and browser journeys pass.
- [x] Desktop/narrow/keyboard inspection, independent finish review and documentation.

Tests retain synthetic fixtures in source control. The supplied CSV can be used
in an isolated local fixture and cleaned afterward. Production operating-model
work, family offers, automatic selection, offline capture and persistent drafts
remain outside scope.

## Direction contract

THESIS: Start a tryout with the club already present, then make each evaluator's
next action obvious without requiring a paragraph of explanation.

OWN-WORLD: Preserve navy identity, cobalt actions, white notebook surfaces and
semantic green/ochre/red states. Add purposeful tinted action groups, compact
counts and explicit buttons within that vocabulary.

STORY: Open the club, start or resume a tryout, recognize the player and previous
team, observe, decide and move on. Setup and exceptions stay within reach.

FIRST VIEWPORT: A compact context header establishes season/tryout and saved
progress. One primary task leads a grouped action area. Roster search and status
sit beside the selected player on desktop; phones switch between roster and
notebook with preserved selection and heading focus. Previous placement sits
beside the decision action, rather than buried in history.

FORM: Code-led refinement of the established world under delegated decisions;
no concept seed or replacement identity. Signature interaction: a visible
previous-team action records an eligible decision with immediate saved feedback.
Motion acknowledges focus, selection and saving and honors reduced motion.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
