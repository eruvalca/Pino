# Guided workflows and steady navigation

The owner requested removal of attendance tracking, a review of every workflow
for useful guided steps, and smoother navigation. This brief supersedes the
attendance requirements in earlier feature briefs. Routine decisions are delegated.
Production operations, family offers and persistent drafts remain outside scope.

## Product and workflow decisions

Remove attendance storage, APIs, controls, filters, history and export fields.
Keep Did not attend as an explicit final result, without a team placement. Staff
choose it in the existing decision form. It counts toward tryout completion and
appears in saved results and exports. Keep a printable player/bib list without
attendance or check-in columns.

Guided steps belong to tasks whose later choices depend on earlier choices.
Each step presents one task, preserves in-page input when going back, explains
what will be saved, and leaves writes to an explicit final action. Nothing saves
unfinished drafts across reloads. Existing concurrency, permission, retry and
partial-result protections remain authoritative.

| Surface / workflow | Decision and reason |
| --- | --- |
| Public introduction, staff guide, sample | Keep direct reading and demonstration; no forced tour. Update attendance language. |
| Registration, confirmation, sign-in and recovery | Keep Identity's existing focused pages and SSR forms; clarify progression through existing notices. |
| Profile and club access | Make the existing profile → club → access sequence visible; review a selected club before requesting access and review creation before saving. Existing members go straight to their workspace. |
| Invitation acceptance | Keep one invitation review and explicit acceptance; retain verification/profile requirements and role visibility. |
| Club home, staff, invitations, emails, settings | Keep direct navigation and focused forms. Existing sensitive confirmations are sufficient. |
| Player catalog, record and editing | Keep direct search/edit; quick routine corrections do not need extra screens. |
| CSV import | File → columns → player review → result. Show only the current task; final review includes duplicate and archived-identity decisions. |
| Seasons and teams | Keep simple editing direct. Creating a tryout gets details → roster/team review → create/open, because creation automatically includes the club's active players. |
| Team availability and roster targets | Keep direct editing with clear scope and existing confirmations; targets are advisory. |
| Group enrollment and roster correction | Retain selection → review → results; make progress, Back and focus consistent. |
| Returning players | Earlier season → players → review → results, preserving per-player conflicts and explicit commit. |
| Tryout notebook and decisions | Keep rapid roster selection, notes and decisions direct. Remove attendance and preserve Did not attend. |
| Season review, team roster and comparison | Keep exploration direct; filters and temporary placement previews are not a wizard. |
| Tryout closeout | Results review → confirmation → saved edition; keep incomplete-player feedback before confirmation and explicit reopening. |
| History, downloads and printing | Keep direct reading/export. Never animate or delay file downloads. |
| Erasure, note redaction and account deletion/security | Keep dedicated reviews with required reasons/strong confirmation; avoid redundant steps around existing safety boundaries. |
| Errors, reconnect and access loss | Keep immediate recovery actions and remove protected content when access fails. |

## Component and navigation plan

Pages own step state and data. A small semantic progress component shares labels
and current-step presentation, without becoming a form engine. Existing form and
review components remain responsible for their focused inputs. On a step change,
move focus to its heading; Back preserves current in-page choices. Invalid or
changed inputs invalidate downstream previews, and successful writes show a receipt.

Preserve page-level InteractiveAuto and account SSR. Investigate prerender-to-
interactive loading, enhanced navigation, redirects, focus and layout changes
before adding motion. Transfer only authorized initial read models through
Blazor prerender state; refreshes and writes still use the server. Do not cache
authorization decisions or unsaved drafts. Prefer removing layout churn over
hiding it with fades. Any motion must honor reduced motion and never delay input.

The implementation transfers initial read models with `PersistentComponentState`
and links the global Club workspace action directly to the known club. A first
read completes before `ComponentBase` renders the interactive page, keeping the
prerendered content and navigation visible when no transferable state is present.
This also covers the warm WebAssembly enhanced-navigation state-discovery issue
tracked in [aspnetcore#63996](https://github.com/dotnet/aspnetcore/issues/63996).
Refreshes and writes fetch current server data; transferred membership information
never authorizes a server operation. No new entrance animation or forced full-page
navigation is needed. Account forms retain static SSR and antiforgery behavior.

Closeout results render 50 rows per page. The matching count, completion checks,
saved editions and downloads still include the full result set. Import choices
stay locked after an interrupted commit response until retry confirms its result.

## Acceptance evidence

- [x] No attendance entity, contract, endpoint, UI, or personal-export content.
- [x] Did not attend works through placement clearing, close/reopen, history and exports.
- [x] Every surface above checked against its implementation; guided tasks work forward/back, at validation failures and after partial completion.
- [x] Navigation checked across app/club links, direct loads, back/forward, cold/warm interactive startup, SSR account boundaries and access revocation.
- [x] Desktop, phone, keyboard, narrow reflow and reduced-motion checks.
- [x] Fresh database creation, full build, format/analyzers, headless and relevant browser tests.
- [x] Independent finish review and product/design/documentation updates.

## Direction contract

Mode: Operate for club work; Read for guidance. This is a refinement of the
established Sideline notebook, not a new visual identity.

THESIS: One clear task at a time for preparation; immediate access for evaluation.

OWN-WORLD: Preserve navy navigation, cobalt actions, white sheets, fine rules,
compact records, semantic notices and visible keyboard focus.

STORY: Prepare and review consequential changes, then move smoothly among saved
club records and the player notebook.

FIRST VIEWPORT: Title, current step and the fields or review needed now. A clear
primary action advances; Back stays beside it. Daily work keeps the roster and
player context visible without another attendance task.

FORM: Code-led refinement with real conditional steps and stable initial content.
No new imagery or replacement palette. Motion only if runtime evidence warrants it.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
