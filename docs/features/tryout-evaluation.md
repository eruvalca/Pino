# Tryout evaluation and team placement

Status: shaping complete; **Sideline notebook** is the selected visual direction.
This brief describes planned behavior, not an implemented feature. Confirmed
product rules and unresolved business decisions live in [PRODUCT.md](../../PRODUCT.md).

## Job and audience

Visitor mode: **Operate**. Coaches and administrators need to capture observations
on phones or tablets during a tryout, then review the roster and place players
on computers afterward. Soccer is the initial context; the workflow should also
serve other youth sports.

Success means observations are saved reliably and every participating player has
a recorded decision. Evaluation uses shared notes, without scores or a rubric.

## Selected direction

The Sideline notebook puts fast note entry first, followed immediately by recent
shared observations. Keep the player's name, graduation year, and decision clear.
Use white writing surfaces, navy navigation, blue actions, and readable sans-serif
typography. The focal action at the field is **Save note**; **Record decision** is
available as a separate action.

![Selected Sideline notebook concept with fictional player data](../../.impeccable/mocks/decision/sideline-notebook.png)

The mockup establishes the direction, not a pixel-exact responsive specification.
Its compact landscape rows must wrap on narrow phones. All clubs, players,
observations, times, and roster counts shown are fictional sample content.
The 48-player roster is illustrative, not a size requirement or limit.

The [Player threads](../../.impeccable/mocks/decision/player-threads.png) and
[Familiar roster](../../.impeccable/mocks/decision/familiar-roster.png) concepts
are retained as unselected alternatives. Each image has an adjacent JSON file
with its original generation prompt and approval status.

## Workflow and layout

1. **At the field:** open the tryout, find a player, add a shared observation,
   save, and return to the roster. Show note authors and times. Keep recent
   observations close to the composer so earlier staff input stays accessible.
2. **During desktop review:** show the searchable roster beside the selected
   player's notes and decision controls. Provide graduation-year, decision,
   and assigned-team filters; preserve selection and filters between players.
3. **Record a decision:** distinguish Awaiting decision from the completed
   outcomes Placed, Withdrawn, and Not selected. Coaches and administrators can
   finalize and revise outcomes directly. Team selection must enforce the
   eligibility and one-current-team-per-season rules in PRODUCT.md.
4. **Track completion:** show remaining decisions for the entire tryout even
   when the roster is filtered. Completion requires no participants awaiting
   a decision; it does not require placing everyone on a team.

## States and constraints

Cover empty rosters, no search matches, no notes, loading, failed saves, lost
connections, concurrent changes, permission failures, and no eligible teams.
Keep unsuccessful note text available for retry without promising offline
capture or synchronization. Save feedback must distinguish successful writes
from pending or failed requests.

Design checks should include empty, single-player, and larger rosters, long
names, substantial notes, and content that exceeds the viewport. Actual typical
and maximum roster sizes remain unknown. Preserve logical reading and focus
order, labels, keyboard access, visible focus, contrast, comfortable touch
targets, and reduced-motion behavior.

Keep the existing Blazor architecture and account behavior. Enforce club access,
eligibility, and placement constraints on the server. Account setup, CSV import,
scores, approval workflows, and offline synchronization are outside this brief.

## Open decisions

- Who may edit or delete shared notes, and how those changes are represented.
- How revising a decision in one tryout affects a placement from another tryout
  in the same season while preserving history and one current team.
- Realistic roster sizes and the corresponding navigation and loading needs.

These questions remain explicit implementation decisions; the visual selection
does not settle them. Record the established reusable visual system in DESIGN.md
when implementation provides evidence for it.
