# Public v1 product completion

Attendance requirements in this earlier delivery brief are superseded by
[Guided workflows and steady navigation](guided-workflows-and-navigation.md).
Current product rules are in PRODUCT.md; the earlier scope below records why the
removed attendance capability originally existed.

This is the implementation and acceptance scope approved after the product review.
PRODUCT.md owns the confirmed rules. Production deployment and operating-model
work, family offers/acceptance, persistent drafts, offline capture, general player
merging, automatic retention, and club closure/recovery are excluded.

## Delivery and evidence checklist

- [x] Guided 1,000-player CSV mapping, blank/extra-column handling, middle names,
  secondary position, graduation-year validation, duplicate skipping, archived
  identity resolution, reactivation and confirmed replacement erasure.
- [x] Reviewed bulk enrollment and returning-player placement with per-player
  eligibility/conflict results. Club team defaults and automatic tryout rosters
  supersede the original season-copy and correspondence setup; their current
  acceptance evidence is tracked in `club-defaults-and-evaluator-clarity.md`.
- [x] Shared tryout roster/bibs/decisions and attendance with club-wide access.
  The later `simple-tryouts-and-staff-activity.md` brief supersedes session setup
  and staff assignments, and tracks verification of their removal.
- [x] Did not attend outcome; reasoned enrollment removal retaining history;
  active-only completion; closed-tryout protection and preserved editions.
- [x] Advisory total/position roster targets, comparison and useful roster filters.
- [x] Efficient selected-player loading, reliable concurrent writes and realistic
  200/1,000-player validation, without persistent note drafts.
- [x] Consolidated history, ordinary CSV roster/results exports, printable
  attendance/bib lists and administrator comprehensive personal-data export.
- [x] Verified single-use expiring invitations, role assignment, membership emails
  with Mailpit validation, failure recovery and administrator club detail editing.
- [x] Strongly confirmed personal-content erasure and reasoned note redaction,
  including historical copies, photos and earlier versions; minimal audit.
- [x] Public Pino introduction, adult staff onboarding, contextual guidance,
  authentication/email abuse protections and all new server permission checks.
- [x] Full solution build, formatting/analyzers, unit/component and PostgreSQL
  browser journeys, responsive/keyboard verification and Impeccable finish review.

Checkboxes are completion claims: mark only with current implementation and
verification evidence. Tests use synthetic people in source control. A supplied
CSV can also be exercised locally in an isolated, cleaned fixture. Production
deployment is outside this scope.

## Surface and component plan

Visitor mode: Operate for club work; Read for help; Persuade for the public entry.
The established Sideline notebook system is authoritative. The owner delegated
routine design decisions and authorized implementation after reviewing the scope.
Continue code-led inside the existing world without recording a new standing
workflow preference or reopening the completed product interview.

- Player import owns the file, mapping, row resolutions and preview. Mapping and
  paged, ruled review records are separate components; child events update
  page-owned state. Review records stack on phones.
- Tryout work owns roster selection and selected-player data. Attendance
  and enrollment correction are focused components rather than more
  unrelated fields inside the notebook. Existing notes/decision components remain.
- Season review owns team comparison; scoped team availability and bulk actions
  support preparation. Final outcomes stay explicit and independent of attendance.
  Focused comparison keeps two saved rosters and unplaced season participants
  beside a lazily loaded player notebook. A one-player placement preview updates
  advisory coverage only; a link opens that player in the actual tryout to decide.
  At 65rem and below, explicit roster/notebook navigation moves heading focus
  while preserving selection, filters, roster pages and the temporary preview.
- Club people owns invitation and notification feedback. Sensitive erasure and
  redaction use dedicated, reasoned review forms with clear scope and confirmation.
- Public entry demonstrates the actual progression and offers account/sample
  actions. Help follows the same vocabulary as the operational screens.
  Its first viewport states the club notebook purpose with staff signup and
  fictional sample actions; a sequential preparation-to-closeout explanation
  follows. The staff guide uses anchored sections and readable prose. Club setup
  derives progress from saved records instead of maintaining a second wizard state.
  The introduction uses a larger responsive heading and lead paragraph for public
  orientation, retaining the shared palette, typography family and action patterns.

Preparation implementation sequence:

- Keep club teams available across seasons and tryouts, with administrator-managed
  exclusions. Existing placements and earlier results remain intact.
- Include every active catalog player when creating a tryout. Both roles can
  review reasoned exclusions/restorations; administrators can include later catalog
  additions together. Existing and excluded entries receive explicit feedback.
- Review returning placements against the chosen earlier season and the same club
  teams. Recheck eligibility, enrollment, current decisions and observed revisions
  at commit; preserve staff changes and report each skipped or changed player.
- Retain count-only batch receipts for safe acknowledgement retries, without
  retaining personal import/placement payloads after player erasure.

Roster planning and handoff sequence:

- Add optional total and free-text position targets to teams. Count primary
  positions separately from secondary coverage, and keep targets advisory.
- Compare current team totals and position coverage in season review. Preserve
  sporting placement permissions; only administrators change planning targets.
- Add position, chosen previous-season team, and missing-observation filters to
  tryout work without loading every observation body or discarding in-page drafts.
- Provide current-roster and explicit closed-edition CSV exports, tryout
  attendance/bib print views, and an administrator personal-data package.
  Shared player history brings tryouts, attendance, enrollment and notes
  together with the existing cross-season placement history.

## Direction contract

THESIS: A club notebook that makes a thousand-player season manageable through
reviewed preparation, quick field-side observations, and explicit final decisions.

OWN-WORLD: Existing navy navigation, cobalt actions, white sheets, pale blue
selection, fine rules, system sans typography, semantic notices and visible focus.

STORY: Prepare the catalog and returning teams, set up tryouts, record what
happened, decide, and close a trustworthy edition. Keep years of context accessible.

FIRST VIEWPORT: Page title and primary task precede compact filters and the
working roster or review table. Selection opens a focused notebook alongside the
roster on desktop and switches to the notebook view on narrow screens, with a
Back to roster action. Counts distinguish attendance,
retained participants and completed decisions. Destructive actions receive their
own review with scope and consequence, outside ordinary placement controls.

FORM: Established-world extension under the owner's delegated decisions; no new
visual world or concept seed. The signature interaction preserves the selected
player while roster filters change. Motion only acknowledges state changes;
reduced motion preserves all content and feedback.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
