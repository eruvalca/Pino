# Simple tryouts and staff activity

The owner asked to remove tryout sessions, show staff photos beside notes and
other staff activity, and make every screen easier to read. This is a refinement
of the existing Sideline notebook design. The owner delegated remaining choices.

## Product decisions

- A tryout has one roster, date/location, attendance mark per player, set of notes,
  and final decision per player. Remove session setup, session selection, session
  staff assignments and session-specific contracts, routes and stored data.
- Keep attendance separate from selection. Both roles can mark Present, Absent,
  or Not recorded for the whole tryout. Did not attend remains an explicit final
  decision. Closing and archiving retain their existing write protections.
- Notes, decisions, attendance updates, roster changes, and closing/reopening
  results show the responsible staff member's avatar beside their name. Use the
  current protected profile photo; use initials when it is missing, inaccessible,
  or fails to load. Historical names remain as recorded. Avatars do not grant
  access to another club's photos. The public sample uses fictional identities.
- Use short sentences and familiar words across public, account, club, sporting,
  help, validation, error and empty states. Keep adult staff as the audience.
  Plain language does not remove warnings, privacy meaning or recovery steps.
- Keep the liked palette, typography, layout patterns and keyboard behavior.
  This is not a replacement visual identity or a change to sporting permissions.

## Implementation plan

One shared `StaffAvatar` leaf component owns photo fallback and size. Existing
activity components receive photo URLs from their existing DTOs and compose it
beside a name. Pages keep data loading and operation state; avatars fetch no
application data. AttendanceEditor and printing use tryout-level attendance.
Remove the obsolete session components and service methods rather than hiding
them. Regenerate the initial migration under the repository's development policy.

Terminology: player list (catalog where the domain needs it), note (observation),
result (outcome), saved results (closed edition), remove private text (redaction),
delete permanently (erasure), graduation year (never substitute birth year).
Use specific action labels and explain archive versus permanent deletion.

## Acceptance evidence

- [x] No tryout-session setup, selection, staff assignment, route or persistence.
- [x] Tryout attendance, notes, printouts, history and exports work without sessions.
- [x] Staff photos and fallback initials cover all displayed staff activity.
- [x] Photo authorization, missing/deleted profiles and historical attribution verified.
- [x] All page/component families and user-facing server messages reviewed for plain language.
- [x] Fresh database creation, full build, format/analyzers and appropriate tests pass.
- [x] Desktop, narrow, keyboard and 200% zoom-equivalent reflow checks cover the changed paths.
- [x] Independent finish review and documentation completed.

The reflow check uses a 720px CSS viewport, equivalent to the space available on
a 1440px display at 200% browser zoom. It verifies keyboard focus and horizontal
overflow; it does not operate the browser's zoom controls. Browser coverage also
includes 1,000 players, photo access after staff removal and account deletion,
and attendance remaining separate from decisions through close and reopen.
The finish reviewer scored all three requested corrections resolved, followed
by a source-only check of the neutral account-settings error. The documenter
updated the established avatar and copy rules in DESIGN.md and its sidecar.

## Direction contract

Mode: Operate for club work; Read for help. Existing public introduction retained.

THESIS: Open a player, recognize who wrote each note, and understand the next
action without having to learn Pino's internal terms.

OWN-WORLD: Preserve navy and cobalt, white notebook surfaces, fine rules, clear
focus and meaningful status colors. Staff portraits support recognition.

STORY: Open a tryout, find a player, check attendance, read or add notes, and save
a decision. There is no extra session choice anywhere in this path.

FIRST VIEWPORT: Player search or identity remains visible on a phone. Removing
session controls earns space for the task; avatars sit beside names and do not
push the working content away.

FORM: Code-led refinement with a reusable, small staff portrait and initials
fallback. No new illustration, replacement identity or approved comp is needed.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
