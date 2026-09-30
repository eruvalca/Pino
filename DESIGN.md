---
name: Pino
description: Sideline notebook visual system for club, sporting and account work.
colors:
  navy: "#202c42"
  blue: "#244d9e"
  blue-hover: "#1b3c7e"
  blue-soft: "#e7edf6"
  canvas: "#f5f7fa"
  surface: "#ffffff"
  subtle: "#fafbfd"
  muted: "#59677d"
  line: "#dce2ec"
  line-strong: "#a5b1c3"
  success: "#286044"
  success-surface: "#edf5f0"
  error: "#9c2525"
  error-surface: "#fff2f2"
  warning: "#805500"
typography:
  display:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "2rem"
    fontWeight: 700
    lineHeight: 1.25
    letterSpacing: "-0.035em"
  headline:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "1.875rem"
    fontWeight: 700
    lineHeight: 1.25
    letterSpacing: "-0.035em"
  sporting-heading:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "2.25rem"
    fontWeight: 750
    lineHeight: 1.25
    letterSpacing: "-0.035em"
  title:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "1rem"
    fontWeight: 700
    lineHeight: 1.25
  body:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.5
  observation:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "0.9375rem"
    fontWeight: 400
    lineHeight: 1.65
  label:
    fontFamily: "Segoe UI, system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 600
    lineHeight: 1.4
rounded:
  radius: "0.375rem"
  radius-small: "0.25rem"
spacing:
  xs: "0.25rem"
  sm: "0.5rem"
  md: "0.75rem"
  base: "1rem"
  lg: "1.25rem"
  xl: "1.5rem"
  section: "2rem"
  notebook-inline: "2.5rem"
  app-gutter: "1.5rem"
  app-page-gap: "2rem"
  app-sheet-padding: "2.5rem"
components:
  application-sheet:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.navy}"
    rounded: "{rounded.radius}"
    padding: "{spacing.app-sheet-padding}"
  button-primary:
    backgroundColor: "{colors.blue}"
    textColor: "{colors.surface}"
    typography: "{typography.label}"
    rounded: "{rounded.radius}"
    padding: "0.65rem 1rem"
  button-primary-hover:
    backgroundColor: "{colors.blue-hover}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.blue}"
    typography: "{typography.label}"
    rounded: "{rounded.radius}"
    padding: "0.65rem 1rem"
  button-secondary-hover:
    backgroundColor: "{colors.blue-soft}"
  button-text:
    backgroundColor: "transparent"
    textColor: "{colors.blue}"
    typography: "{typography.label}"
    rounded: "{rounded.radius}"
    padding: "0.65rem 0.25rem"
  field:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.navy}"
    rounded: "{rounded.radius}"
    padding: "0.65rem 0.75rem"
  badge-awaiting:
    backgroundColor: "{colors.blue-soft}"
    textColor: "{colors.blue}"
    rounded: "{rounded.radius-small}"
    padding: "0.3rem 0.6rem"
  badge-placed:
    backgroundColor: "{colors.success-surface}"
    textColor: "{colors.success}"
    rounded: "{rounded.radius-small}"
    padding: "0.3rem 0.6rem"
  badge-closed:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.muted}"
    rounded: "{rounded.radius-small}"
    padding: "0.3rem 0.6rem"
  writing-surface:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.navy}"
  bib:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.blue}"
    rounded: "{rounded.radius}"
    width: "5.25rem"
  bib-compact:
    width: "3.25rem"
  bib-label:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.surface}"
    padding: "0.15rem 0.35rem"
  roster-selected:
    backgroundColor: "{colors.blue-soft}"
    textColor: "{colors.navy}"
    padding: "1rem 1.25rem"
---

# Design System: Pino

## Overview

**Creative North Star: "Sideline notebook"**

A working notebook for coaches: white writing space, a navy masthead with a
recognizable field mark, clear blue actions and observations separated by fine rules. Its
density supports repeated reading and input on a field-side phone or a review
desktop. Strong identity headings, numbered bibs and portraits make people easy
to recognize; readable type and explicit controls keep notes and decisions easy
to scan.

This records the implemented club access, membership, persisted sporting and
account surfaces, alongside the public tryout sample. Account forms and their
layout now share the notebook's palette, flat sheets, fields and action hierarchy.
Shared tokens and controls live in [app.css](src/Pino/wwwroot/app.css);
[TryoutLayout](src/Pino.UI/Layout/TryoutLayout.razor),
[ClubPageShell](src/Pino.UI/Features/Clubs/Components/ClubPageShell.razor) and
[MainLayout](src/Pino.UI/Layout/MainLayout.razor), with adjacent feature styles,
express this visual world. Product facts and open
decisions remain in [PRODUCT.md](PRODUCT.md); surface composition and behavior
are defined in the [tryout brief](docs/features/tryout-evaluation.md) and
[club access brief](docs/features/club-onboarding-access.md), and the
[sporting workspace](docs/features/club-sporting-workspace.md). The
[season review](docs/features/season-review-closeout.md) and
[account navigation](docs/features/account-navigation.md) briefs describe their
extensions of this system. The [visual refinement brief](docs/features/visual-refinement.md)
records the stronger identity, navigation and shared photo workflow. The
[application shell brief](docs/features/application-shell.md) records the shared
navigation and page envelope for club and account modes.

**Key Characteristics:**

- White writing surfaces on a cool, quiet canvas.
- Cobalt actions and pale-blue selection against navy text.
- A field-mark wordmark, framed bib numbers and portraits beside names.
- Ruled observations and rows, with compact status labels.
- One sans-serif family, explicit focus and responsive working space.

## Colors

The palette uses cool neutrals and a single blue action family; semantic colors
support status without becoming competing brand accents. Frontmatter names map
directly to the shared CSS custom properties.
The sidecar's synthesized tonal ramps are panel previews, not additional
application color tokens.

### Primary

- **Cobalt blue** (`blue`) marks primary actions, selected navigation and awaiting
  decisions. `blue-hover` deepens filled actions; `blue-soft` marks selection,
  progress, initial badges and brief save feedback.

### Neutral

- **Deep navy** (`navy`) supplies text and the club masthead.
- **Writing white** (`surface`), **cool canvas** (`canvas`) and **subtle paper**
  (`subtle`) separate the notebook, page and roster without shadows.
- **Muted slate** (`muted`) carries supporting text. `line` divides records;
  `line-strong` makes editable control boundaries visible.
- **Success green**, **error red** and **warning ochre** have semantic roles.
  Placed badges and success notices use `success-surface`; error notices use
  `error-surface`. Withdrawn and Not selected use neutral status treatment.

**The State Has Words Rule.** Color accompanies a readable status, selection
state or feedback message; it never carries the meaning alone.

## Typography

Segoe UI with system-ui and sans-serif fallbacks carries headings, reading text
and controls. The operational interface has no separate decorative display face.
The frontmatter preserves the public sample's hierarchy, fixed sporting heading
and shared body and action text. Sample headings use browser bold weight;
sporting page headings use a stronger weight (750), while compact navigation
and status labels use semibold (650).

- **Display:** the selected player, the strongest reading anchor.
- **Headline:** public sample tryout context, slightly smaller than the player name.
- **Sporting heading:** catalog, record and configuration page identity; it
  reduces to 1.875rem at 35rem and below. Tryout context overrides its size to 1.5rem.
- **Title:** roster, composer and observation section headings.
- **Body:** the shared base; **observation** is the more open reading treatment
  for notes, with a maximum measure of 70ch and preserved line breaks.
- **Label:** action text. Field labels and supporting metadata use smaller
  sizes (0.75–0.8125rem); times and compact roster status use 0.6875rem.

At the phone breakpoint, player names reduce to 1.75rem. The persisted tryout
keeps its context heading compact (1.5rem) at all widths; its selected player
remains the strongest reading anchor (2rem, then 1.75rem). The public sample's
tryout heading reduces to 1.5rem on phones. Bib numbers and counts use tabular
numerals. Keep full names and note text able to wrap.

Shared bibs use cobalt tabular numerals on white with a navy Bib label. Compact
roster bibs use 1.25rem numerals; notebook bibs use 2.25rem, both at weight 750.
Values longer than four characters reduce to 0.8125rem and 1.125rem respectively
and wrap. Review results retain a separate 1.5rem bib with a smaller visible Bib
label. The player's name remains the reading anchor beside the bib.

Club page headings are larger reading anchors (2.5–2.75rem), reducing to 2rem
on narrow screens. Person names sit beside their photos; muted supporting text
keeps role, location and request time subordinate without hiding them.
Sporting record titles use 1.125rem and section headings use 1.25rem. Account settings
keep one page heading and a muted introductory paragraph above navigation.
The account heading scales from 2rem to 2.75rem; the current task heading uses
1.625rem, with 0.875rem muted navigation group headings.

## Layout

CSS Grid aligns structured areas. The public tryout sample has a centered
88rem maximum width with 2rem side padding, a roster-to-notebook grid of
`minmax(17rem, 0.9fr) minmax(0, 1.8fr)`, and a notebook capped at 58rem. The
roster scrolls within a 42rem maximum height on larger screens; notes retain
normal document flow. These are current workspace measurements, not required
composition for every future feature.

In the public sample, at 60rem and below, notebook padding contracts and decision
choices stack. At 45rem and below, page side padding becomes 1rem and the workspace shows either
the roster or notebook. The visible Roster action returns to search; opening a
player focuses the notebook heading. Save note fills the available width, note
timestamps wrap beneath authors, and team rows become stacked readable records.
The roster's internal height cap is removed.

Club access, overview, People, persisted sporting and account pages share an
80rem maximum envelope (`app-max-width`). Masthead contents, club context and
working sheets align to that envelope. The shared `app-gutter`, `app-page-gap`
and `app-sheet-padding` tokens control side gutters, the gap before content and
sheet padding. At 50rem and below, sheet padding becomes 1.5rem; at 40rem,
gutters become 0.75rem and the content gap becomes 1rem; at 35rem, sheet padding
becomes 1.25rem. Content retains a 4rem bottom margin and normal page scrolling.
White sheets use a fine border and the shared control radius. Narrow club
layouts stack search, result and person actions beneath their associated content.

Club identity and section navigation share a secondary white strip. At 60rem
and below, the identity sits above the section row; at 35rem, section links form
a two-column grid. Long club names wrap within a 36ch measure. The public sample
retains its separately labelled workspace and measurements above.

Persisted sporting catalogs put labelled search and filters directly above
ruled records; forms cap their reading and input area at 44rem. At 50rem,
headings and split content stack; at 35rem, paired fields, toolbars and record
actions become single-column groups. Import previews remain semantic tables in
a keyboard-focusable horizontally scrolling region.

Player records align a portrait or initials and the name in one header, with
the edit action beside it on wide screens. Placements and history occupy the
larger column beside a softly tinted facts panel; at 50rem the facts panel is
shown before the history. Catalog rows use a smaller portrait beside the name.
Enrollment aligns its labelled search, next-player bib and search action along
their bottom edge, with bib guidance across the next row. At 45rem these controls
stack and the guidance stays before the action. Ordinary records and enrollment
use names, graduation years and positions rather than displaying import references.

The shared photo editor places a 10rem square preview beside upload and guidance.
During framing, a 22rem-high crop stage sits beside position and zoom controls.
At 45rem, tools stack below an 18rem-high stage. At 35rem, upload and confirmation
actions stack, and the preview becomes 9rem wide. Profile name fields stack at 40rem.

The persisted tryout grid uses `minmax(16rem, 0.85fr) minmax(0, 1.8fr)` for roster
and notebook. Notebook padding contracts at 65rem. At 48rem and below, staff see
either the roster or the selected notebook, with a visible Back to roster action
and focus moved to the destination heading. The 42rem roster height cap is
removed on phones. Composer and shared observations remain adjacent; decision
history has its own disclosure after the observations.

Account forms remain single-column and cap at 36rem inside the shared application
sheet. Settings place a 14rem grouped sidebar beside the form, separated by a
3rem gap. The sidebar stays 1.5rem from the viewport top while scrolling. At 48rem
and below, it returns to normal flow: three group links appear above the current
group's destinations in two columns, followed by the form. These dimensions
describe the implemented surfaces, not a single mandatory page template.

Season review uses an index and ruled tryout work list before current team
rosters and players without a team. Tryout review separates live results from
dated closeout editions. A receipt uses the existing success colors and a fine
rule. The index has three columns and team rosters have two; both stack at 50rem.
Player metadata stacks beneath the name at 35rem. Result rows align a 5rem bib
column, player identity and outcome. At 40rem, the bib column contracts to 3.5rem
and the outcome moves beneath the player. Confirmation follows the results.

The generic spacing entries are extracted repeated values; the `app-` entries
map directly to the shared shell's CSS custom properties. Use
compact gaps within a record and larger gaps between reading and action groups.
Long content must shrink and wrap without changing reading or focus order.

## Elevation & Depth

Club, sporting and account working surfaces use no elevation shadows. White and
subtly tinted surfaces, single-pixel rules and pale-blue selection establish structure. Focus is an
outline (2px, offset 3px), with an inset offset on roster rows; masthead focus
uses a lighter blue so it remains visible against navy.

**The Ruled Paper Rule.** Separate repeated observations and records with
fine rules and spacing; keep writing surfaces flat.

## Shapes

Controls, sporting and account sheets, bibs, photo previews and player fact panels
use the shared gently rounded `radius`; status badges use `radius-small`.
Evaluation workspace frames and roster rows stay square. Public sample author initials are
circular marks. Club profile photos use circular display crops beside names; their
editing preview retains the square crop that is saved. Navigation and row icons
are small authored outline SVGs (14–18px); the closeout receipt uses a larger
check (28px). Icons are paired with text or hidden from assistive technology when
decorative.
Player portraits and initials use rounded squares: 7rem in record headers,
reducing to 4.5rem at 35rem; 3rem in catalog rows; optional photos remain 6rem in
persisted notebooks. The Pino mark is an authored field diagram inside a rounded
square, displayed at 2.5rem. There are no shipped decorative raster assets; profile and player
photos are private user content, and browser fixture images are test data.

## Components

### Buttons

Actions are compact and explicit. Primary, outlined secondary and text variants
share a minimum height of 2.75rem. Primary hover deepens blue; secondary hover
adds pale blue; text hover adds an underline. Disabled controls reduce opacity
to 0.55 and keep their labels. Save note leads the notebook; decision editing
remains a separate inline disclosure in the persisted workspace. Account submits
reuse filled primary actions; external-provider and record-row actions reuse
outlined secondary actions. Masthead logout remains a quiet navigation action.
Primary and secondary backgrounds and borders transition over 150ms ease-out
when reduced motion is not requested. Pressing either offsets it down by 1px.

### Inputs / Fields

Native fields use white fill, a strong neutral border, visible labels and a
2.75rem minimum height. The public sample's note textarea is vertically resizable
with a 6.5rem minimum height; persisted notes use a four-row native textarea.
Field-specific help and feedback remain associated with their controls. Notices retain meaningful text
and semantic success, error, warning or information kinds.

### Navigation

The navy masthead frames each dedicated workspace. Pino's field mark, heavy
wordmark (2rem, weight 800) and pale-blue terminal dot give it a shared identity.
Account and club shells reuse the same NavMenu: Club workspace, Account and
Logout pair outline icons with explicit labels. Signed-out account pages replace
Account and Logout with Register and Login. Navigation stays at weight 650 in
both default and selected states; the active mode is a white tile with navy text,
and hover adds a lighter navy surface. At 42rem and below, the wordmark sits
above three equal navigation columns. Each control reserves an icon row and a
two-line label row, keeping the controls aligned when Club workspace wraps.
Logout remains an antiforgery-protected POST with the current return URL.

When membership is available, the club name links to membership and profile
access at `/club/access` in the secondary white strip. Overview, Players,
Seasons & teams and administrator-only People follow it. The current section
uses blue text, a pale-blue fill and a 2px inset lower rule. The section row
stacks beneath the club name at intermediate widths and becomes a two-column
phone grid, preserving readable labels without horizontal navigation scrolling.
Team and tryout routes retain Seasons & teams selection. Main and club section
text/background transitions take 160ms ease-out only when reduced motion is not
requested. Keyboard-visible skip links target focusable main content while
retaining the current route and query.

Within account settings, the current destination uses pale blue and bold text.
Desktop groups are About you, Sign-in & security, and Your data. Phone group
links are Profile, Security, and Your data; their selected group uses a thin
inset lower rule, while the current destination retains its pale fill.
Nested pages retain their parent section's selection and offer a return link.
Profile links to the existing club name/photo editor.

Tryout view buttons use muted text and a thin bottom rule; the selected view uses
blue text and rule. Its phone navigation preserves the sample label and explicit
roster return. People uses text actions with a blue bottom rule on the current
Requests or Members view.

### Profiles and club records

Completed profiles pair a circular photo and full name with a visible edit action
and textual completion status. Club search and People lists use fine rules,
readable supporting details and explicit actions aligned beside each record on
wide screens, then below it on phones. Confirmations appear inline on a subtly
tinted, bordered surface.

### Shared photo editor

Member profiles and player records use the same choose, frame, confirm and save
sequence. A square preview and labelled file picker begin the flow. The crop
stage has a fixed square frame; dragging positions the image and wheel or pinch
gestures zoom it. Labelled Zoom in, Zoom out and directional movement buttons
provide keyboard-operable alternatives. Use this crop reveals the exact square
image to be saved, a textual Crop ready to save status and Adjust crop / Discard
photo change actions. The containing save remains disabled while framing is
pending; cancellation restores the saved image and a failed record save retains
the confirmed crop. The confirmed image settles from scale 0.97 to 1 over 200ms
with `cubic-bezier(0.16, 1, 0.3, 1)` only when reduced motion is not requested.

### Status labels and roster rows

Compact badges name the decision state. Roster selection uses a pale-blue fill
with no thick edge stripe; the persisted roster exposes `aria-pressed` and the
public sample uses `aria-current`. Full-width rows combine bib, name, graduation
year, position and decision. The public sample also uses a small SVG chevron.
Hover and keyboard focus remain visible. Catalogs, season records and placement
history reuse ruled rows with muted context and explicit record links.

The shared bib is a compact printed label: navy header, white numbered body,
strong neutral border and gently rounded corners. Compact bibs anchor both
rosters; larger bibs sit directly beside names in both notebooks. A missing bib
shows an em dash with the accessible name No bib assigned. Long values reduce
in size and wrap inside their fixed width; they never push the name out of view.

### Notebook and observations

The selected player's shared bib sits beside the name. The persisted bib is
editable beneath their identity and status while the
tryout is open, using the shared field and secondary-action patterns. Its help
text makes the tryout scope explicit; saving or reloading it preserves
note/decision drafts.

The notebook is one continuous white writing surface, not a stack of elevated
cards. Persisted observations follow the composer directly and carry an author,
timestamp, preserved line breaks and labelled correction history. Each player's
unsaved note and decision drafts remain available while switching within the
tryout. The public sample additionally has circular author marks and open line
spacing. Saved observations in both workspaces receive a pale-blue-to-transparent
highlight over 650ms with `cubic-bezier(0.16, 1, 0.3, 1)`. The persisted notebook
opens with a 180ms transition from 0.5rem to the right; returning to the phone
roster uses the opposite direction. Both use the same easing as the save
highlight. Reduced-motion preference removes these animations while preserving
content and textual feedback. Persisted roster hover uses a 150ms ease-out background transition
only when reduced motion is not requested.

### Closeout receipt and results

A dated receipt sits above a ruled results ledger. Success text and paper tint
identify the recorded edition; an authored SVG check accompanies Results recorded.
The receipt names its tryout, season, reviewed player count, author and time.
Reopened editions add their reopening context. A labelled native edition select
keeps historical results distinct from current decisions, with search and outcome
filters before the rows. Each result pairs its bib and linked player name with
a written outcome and team, when applicable.

The receipt settles upward from 0.4rem over 400ms using
`cubic-bezier(0.16, 1, 0.3, 1)` when an edition appears. The effect runs only under
`prefers-reduced-motion: no-preference`; receipt content is immediately available
in either motion preference. A confirmation checkbox and primary close action
follow the results; reopening uses a separate native disclosure and reason field.

### Decision choices

Persisted decision editing uses a native details disclosure, initially collapsed,
labelled Record or revise decision before the note composer. Outcome and eligible
team use labelled native selects; context has a separate textarea. Save decision
is secondary to Save note. The later Decision history disclosure is independent.

In the public sample, native radio inputs sit in outlined labels. A selected
choice uses pale blue and a blue border. The editor expands inline and receives heading focus;
closing it returns focus to its trigger. Choices stack before they become
cramped. Keep validation visible, associated with the affected fields, and
separate from success feedback.

## Do's and Don'ts

### Do:

- **Do** use the shared action, field, status and notice patterns with visible
  keyboard focus and explicit labels.
- **Do** let long names and observations wrap, retaining reading and focus order.
- **Do** use fine rules and tonal surfaces to organize repeated records.
- **Do** preserve reduced-motion behavior and textual save feedback.

### Don't:

- **Don't** use color alone to distinguish outcomes or validation states.
- **Don't** replace the notebook's reading surface with elevated cards or add
  a thick stripe to selected roster rows.
- **Don't** treat the fictional club or sample roster sizes as approved
  product-wide identity or layout requirements.
