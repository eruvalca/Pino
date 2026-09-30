# Player identity and photo workflow refinement

Mode: Operate. This extends the Sideline notebook for coaches working on phones
and staff maintaining records on a desktop. The owner requested stronger visual
character throughout the application and authorized implementation decisions.

## Direction

Keep the navy, cobalt and white notebook identity. Give navigation a recognizable
field mark and clear selected destinations. Use aligned, spacious form groups,
portraits beside player identity, and a compact numbered bib treatment that
remains legible with long or missing bibs. Internal import references belong in
CSV import feedback, not ordinary player, roster or enrollment views.

The photo workflow is shared by member profiles and players: choose a photo,
position it with drag, wheel/pinch zoom or keyboard-operable buttons, apply the
crop, inspect the exact square preview, then save the containing record. A pending
crop blocks final save. Cancelling restores the saved photo; failed saves
retain the confirmed crop. Only the confirmed crop is submitted to the server.

The screenshot supplied by the owner specifically exposes the enrollment form's
misaligned labels/controls and internal reference. Correct that form alongside
the player detail page rather than treating the screenshot as a different task.

## Verification scope

Inspect navigation, registration/account screens, club access and profile editing,
club overview/people, player list/detail/editor/import, season/team setup/review,
tryout enrollment/evaluation/closeout, and the public sample. Check desktop and
phone layouts, keyboard/focus, long content, empty states, read-only states and
reduced motion. Use real browser evidence for wheel zoom, crop confirmation and
persisted photos, plus component regressions for state and save gating.

Finish with the Impeccable reviewer and documenter after the bounded visual passes.
