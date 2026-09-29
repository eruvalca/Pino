---
name: Pino
description: Sideline notebook visual system for club access and tryout work.
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
components:
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
  roster-selected:
    backgroundColor: "{colors.blue-soft}"
    textColor: "{colors.navy}"
    padding: "1rem 1.25rem"
---

# Design System: Pino

## Overview

**Creative North Star: "Sideline notebook"**

A calm working notebook for coaches: white writing space, compact navy
navigation, clear blue actions and observations separated by fine rules. Its
density supports repeated reading and input on a field-side phone or a review
desktop. Readable system sans-serif type and restrained controls keep names,
notes and decisions easy to scan.

This records the implemented club access, membership and tryout surfaces.
Legacy sample and account layouts remain outside this visual-system extension.
Shared tokens and controls live in [app.css](src/Pino/wwwroot/app.css);
[TryoutLayout](src/Pino.UI/Layout/TryoutLayout.razor),
[ClubPageShell](src/Pino.UI/Features/Clubs/Components/ClubPageShell.razor) and
adjacent feature styles express this visual world. Product facts and open
decisions remain in [PRODUCT.md](PRODUCT.md); surface composition and behavior
are defined in the [tryout brief](docs/features/tryout-evaluation.md) and
[club access brief](docs/features/club-onboarding-access.md).

**Key Characteristics:**

- White writing surfaces on a cool, quiet canvas.
- Cobalt actions and pale-blue selection against navy text.
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
The frontmatter captures the tryout hierarchy and shared body and action text;
ordinary headings use browser bold weight, while row names, authors and status
labels use semibold (650).

- **Display:** the selected player, the strongest reading anchor.
- **Headline:** tryout context, slightly smaller than the player name.
- **Title:** roster, composer and observation section headings.
- **Body:** the shared base; **observation** is the more open reading treatment
  for notes, with a maximum measure of 70ch and preserved line breaks.
- **Label:** action text. Field labels and supporting metadata use smaller
  sizes (0.75–0.8125rem); times and compact roster status use 0.6875rem.

At the phone breakpoint, player names reduce to 1.75rem and the tryout heading
to 1.5rem. Bib numbers, counts and timestamps use tabular numerals. Keep full
names and note text able to wrap.

Club page headings are larger reading anchors (2.5–2.75rem), reducing to 2rem
on narrow screens. Person names sit beside their photos; muted supporting text
keeps role, location and request time subordinate without hiding them.

## Layout

CSS Grid aligns structured areas. The current tryout surface has a centered
88rem maximum width with 2rem side padding, a roster-to-notebook grid of
`minmax(17rem, 0.9fr) minmax(0, 1.8fr)`, and a notebook capped at 58rem. The
roster scrolls within a 42rem maximum height on larger screens; notes retain
normal document flow. These are current workspace measurements, not required
composition for every future feature.

At 60rem and below, notebook padding contracts and decision choices stack. At
45rem and below, page side padding becomes 1rem and the workspace shows either
the roster or notebook. The visible Roster action returns to search; opening a
player focuses the notebook heading. Save note fills the available width, note
timestamps wrap beneath authors, and team rows become stacked readable records.
The roster's internal height cap is removed.

Club access and home use centered white sheets capped at 66rem; People expands
to 76rem for readable identity and action columns. Narrow layouts reduce sheet
padding and stack search, result and person actions beneath their associated
content. These sheets retain normal page scrolling and visible page gutters.

The spacing entries are extracted repeated values, not new CSS variables. Use
compact gaps within a record and larger gaps between reading and action groups.
Long content must shrink and wrap without changing reading or focus order.

## Elevation & Depth

Club and tryout working surfaces use no shadows. White and subtly tinted surfaces,
single-pixel rules and pale-blue selection establish structure. Focus is an
outline (2px, offset 3px), with an inset offset on roster rows; masthead focus
uses a lighter blue so it remains visible against navy.

**The Ruled Paper Rule.** Separate repeated observations and records with
fine rules and spacing; keep writing surfaces flat.

## Shapes

Controls use the shared gently rounded `radius`; badges use `radius-small`.
Workspace frames and roster rows stay square. Tryout author initials are circular
marks. Club profile photos use circular display crops beside names; their
editing preview retains the square crop that is saved. Icons are small authored
outline SVGs (14–18px), paired with text or hidden from assistive technology when decorative.
No raster imagery ships in the tryout workspace.

## Components

### Buttons

Actions are compact and explicit. Primary, outlined secondary and text variants
share a minimum height of 2.75rem. Primary hover deepens blue; secondary hover
adds pale blue; text hover adds an underline. Disabled controls reduce opacity
to 0.55 and keep their labels. Save note leads the notebook; decision editing
remains a separate inline action.

### Inputs / Fields

Native fields use white fill, a strong neutral border, visible labels and a
2.75rem minimum height. The note textarea is vertically resizable with a 6.5rem
minimum height and more generous internal padding. Field-specific help and
feedback remain associated with their controls. Notices retain meaningful text
and semantic success, error, warning or information kinds.

### Navigation

The navy masthead frames each dedicated workspace. The shared club shell shows
Pino, Your club and Account, with the active club name linking to its home when
membership is available. Phone navigation retains this identity and lets long
club names wrap. A keyboard-visible skip link leads to the content.

Tryout view buttons use muted text and a thin bottom rule; the selected view uses
blue text and rule. Its phone navigation preserves the sample label and explicit
roster return. People uses text actions with a blue bottom rule on the current
Requests or Members view.

### Profiles and club records

Completed profiles pair a circular photo and full name with a visible edit action
and textual completion status. Club search and People lists use fine rules,
readable supporting details and explicit actions aligned beside each record on
wide screens, then below it on phones. Confirmations appear inline on a subtly
tinted, bordered surface. The photo editor keeps labelled movement and zoom
buttons available alongside dragging.

### Status labels and roster rows

Compact badges name the decision state. Roster selection uses a pale-blue fill
and `aria-current`, with no thick edge stripe. Full-width rows combine bib,
name, graduation year, position and decision; a small SVG chevron indicates
opening the player. Hover and keyboard focus remain visible.

### Notebook and observations

The notebook is one continuous white writing surface, not a stack of elevated
cards. Observations have an author mark, name, timestamp and open line spacing.
A saved observation receives a pale-blue-to-transparent highlight over 650ms
with `cubic-bezier(0.16, 1, 0.3, 1)`. Reduced-motion preference removes this
animation while preserving the new content and textual feedback.

### Decision choices

Native radio inputs sit in outlined labels. A selected choice uses pale blue
and a blue border. The editor expands inline and receives heading focus;
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
- **Don't** treat the fictional club, sample roster sizes or account scaffold
  layout as approved product-wide identity or layout requirements.
