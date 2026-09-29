---
version: 1
slug: "src-pino-ui-features-tryouts-pages-tryout-razor"
primary_target: "src/Pino.UI/Features/Tryouts/Pages/Tryout.razor"
related_targets: ["src/Pino.UI/Features/Tryouts","src/Pino.UI/Layout/TryoutLayout.razor"]
---

# Tryout evaluation and team placement

Visitor mode: **Operate**. Implemented with interactive fictional sample data.
The merged brief, behavior and integration boundaries are maintained in
[the feature document](../../docs/features/tryout-evaluation.md); product facts
and open decisions remain in [PRODUCT.md](../../PRODUCT.md).

## Direction contract

**THESIS:** A coach's working notebook: find a player, write an observation,
then make a distinct placement decision. Notes lead; scoring dashboards do not.

**OWN-WORLD:** White writing surfaces, deep navy navigation, cobalt actions,
pale blue selection, restrained rounded controls and ruled observation lists.
A readable sans-serif carries the entire operational interface.

**STORY:** Staff see the club, season, remaining decisions and selected player;
save shared observations, consult earlier input, and record or revise outcomes.
Fictional data and its session-only lifetime remain explicit.

**FIRST VIEWPORT:** On desktop a compact navy masthead sits above tryout context
and whole-roster progress. A searchable roster occupies the left third; the
selected player's identity, compact composer and recent notes fill the right.
On phones the notebook fills the screen with an explicit return to the roster.
Save note is the primary action. Decision controls expand inline below notes.

**FORM:** Approved Sideline notebook, selected in the merged shaping brief;
no new seed or direction round. The user delegates remaining layout variations.
The approved image is a direction reference, explicitly not a pixel-exact spec.
The signature interaction is a saved observation joining the ruled notebook
with a brief restrained highlight, while the draft clears only on success.

**FINISH:** unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
