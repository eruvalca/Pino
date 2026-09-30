# Shared application navigation and layout

Mode: Operate. The owner approved the account NavMenu's Club workspace, Account
and Logout controls and asked for the club workspace to use the same navigation
quality and page width. This refines the existing Sideline notebook system.

## Direction contract

THESIS: A stable application frame lets staff move between club work and personal
settings without relearning navigation or seeing the page width jump.

OWN-WORLD: Preserve the navy masthead, field-mark wordmark, outlined navigation
icons, white working sheets and cobalt local selection. Reuse NavMenu directly.

STORY: Choose a global mode, then a club section or account task. The club name
links to membership and profile access. Nested team and tryout routes retain
Seasons & teams selection; People remains administrator-only.

FIRST VIEWPORT: The masthead and working sheet share an 80rem maximum envelope,
responsive gutters and sheet padding. Club identity and local navigation sit in
a secondary white strip. They stack at intermediate widths, with a two-column
section grid on phones. Forms retain their narrower reading/input measures.

FORM: Code-led refinement of the approved interface, without a new visual world
or concept tournament. Preserve static SSR account forms, protected logout POST,
return URLs, shared interactive render modes and keyboard skip navigation.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Validation scope

Compare account, club access, overview, people, player and season working sheets
at wide, desktop, intermediate and narrow widths. Check current navigation,
keyboard focus, long club names, missing membership, coach/admin permissions and
logout. The public illustrative tryout retains its separately labelled sample
shell; this change unifies the authenticated account and club application modes.
