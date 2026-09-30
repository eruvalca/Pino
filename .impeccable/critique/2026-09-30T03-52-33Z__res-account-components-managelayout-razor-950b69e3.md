---
target: account management
total_score: 22
max_score: 40
na_heuristics:
p0_count: 0
p1_count: 4
target_identity: "file:D:\\repos\\.codex\\worktrees\\2938\\Pino\\src\\Pino\\Features\\Account\\Components\\ManageLayout.razor"
target_fingerprint: "sha256:1e65a596f774b620808a92c53860af226609ec085267e0f2f5b116cc720d12e4"
target_path: "D:\\repos\\.codex\\worktrees\\2938\\Pino\\src\\Pino\\Features\\Account\\Components\\ManageLayout.razor"
timestamp: 2026-09-30T03-52-33Z
slug: res-account-components-managelayout-razor-950b69e3
---
Method: dual-agent (A: account_design_assessment; B: account_detector_assessment).

# Independent account-management design assessment A

Target: `src/Pino/Features/Account/Components/ManageLayout.razor`, with `ManageNavMenu` and the account-management pages. Mode: Operate. Assessment A completed independently before seeing detector findings or Assessment B.

Evidence: PRODUCT.md, DESIGN.md, current account and club-profile source, the existing account-desktop.png/account-mobile.png captures, and a fresh background IAB tab against the running local app. Live inspection covered Profile, Password, Two-factor authentication, Configure authenticator app, Passkeys, Personal data, and Delete Personal Data. Checked desktop and 390 × 844 phone layouts, visible keyboard focus, active navigation, and the skip link. No forms that change credentials or delete data were submitted. The fresh tab was closed and the viewport override reset. Source-only conclusions are identified below.

## Design specificity verdict

The visual shell belongs to Pino: navy masthead, flat white writing sheet, cobalt actions, restrained fields, and generous readable space fit the Sideline notebook direction. The information architecture and content still feel like a generic Identity settings scaffold. The most revealing gap is that Profile contains a disabled username and phone number, while the name and photograph that identify a coach to club staff live elsewhere without a connecting link. Product specificity should come from resolving that identity journey and writing trustworthy security guidance, not adding sports decoration.

Overall, the surface looks calm but is only partly finished as a product. The biggest opportunity is to make account settings explain and reflect the person's actual Pino identity, while making security paths dependable.

## Heuristic scores

| # | Heuristic | Score | Evidence |
|---|---|---|---|
| 1 | Visibility of system status | 2/4 | Top-level selected links and source-defined success notices help. Authenticator setup and account deletion lose the settings selection; the 2FA landing state does not plainly say enabled or disabled. |
| 2 | Match between system and real world | 2/4 | Most field labels are familiar, but Profile does not expose the person's name/photo, and authenticator setup exposes developer instructions. |
| 3 | User control and freedom | 2/4 | Main navigation offers an exit, but child flows lack a local cancel/back action; the keyboard skip link actually leaves account settings. |
| 4 | Consistency and standards | 3/4 | Controls, palette, and form layout are cohesive. Heading levels skip from h1 to h3, and parent selection is inconsistent on child routes. |
| 5 | Error prevention | 2/4 | Validation and account-deletion warning/password check exist. Source shows passkey deletion submitting immediately without a confirmation or recovery opportunity. |
| 6 | Recognition rather than recall | 2/4 | Named navigation is visible, but split profile ownership and missing child-route context require users to remember where they came from and where identity details live. |
| 7 | Flexibility and efficiency | 2/4 | Native inputs, labels, autocomplete on credential forms, and keyboard focus support routine work. Six navigation choices precede every short task; the skip shortcut is broken. |
| 8 | Aesthetic and minimalist design | 3/4 | Restrained, readable, uncluttered visual language. The wide grid leaves Personal data stranded on another desktop row and occupies much of the phone's initial view. |
| 9 | Error recognition and recovery | 3/4 | Source provides field-level errors, validation summaries, and specific recovery directions for failed 2FA/recovery-code operations. These POST outcomes were not executed in the read-only review. |
| 10 | Help and documentation | 1/4 | Passkeys and the 2FA landing state provide little explanation. The most prominent authenticator help link teaches developers to implement the missing QR code. |
| | **Total** | **22/40** | **Acceptable; significant improvements needed. All ten apply.** |

## Strengths

1. The existing palette and flat-sheet composition support focused operational work and connect settings to the club workspace without promotional decoration.
2. Labels precede fields, inputs remain comfortably sized on a 390px phone, navigation labels wrap, and a clear keyboard focus outline was visible on the Profile link. No horizontal page overflow was observed in the inspected phone views.
3. Account deletion explicitly states permanence and explains the prerequisite to leave a club. The source also preserves field validation and success/error feedback across forms.

## Priority issues

### 1. [P1] The keyboard skip link navigates away from settings

**Evidence:** `src/Pino.UI/Layout/MainLayout.razor:4` uses `href="#account-content"`. From Delete Personal Data, activating Skip to content with Enter navigated to `/#account-content`, rendered Your club, and left focus on the document. The intended account-content target is on the settings page, not the root club page.

**Impact:** Keyboard and assistive-technology users relying on the shortcut are taken out of their current task. A keyboard shortcut intended to reduce effort becomes a navigation trap.

**Fix:** Construct the fragment link against the current account route (respecting the application's base path), then verify that activation remains on that route and focuses the account main region. Check other layout skip links for the same route-resolution behavior within the scope of the eventual fix.

**Suggested command:** `$impeccable harden`.

### 2. [P1] Authenticator setup promises a QR code that is not present

**Evidence:** Live Configure authenticator app instructs the person to scan a QR code, shows no code, and prominently links to "enable QR code generation." `EnableAuthenticator.razor:40–43` contains the copy and empty containers. Source-only: `EnableAuthenticator.razor.cs:104` uses `Microsoft.AspNetCore.Identity.UI` as the authenticator issuer rather than Pino.

**Impact:** A coach enabling account protection is pushed into an implementation tutorial at the moment they need reassurance. Manual key entry is possible, so this is not a total blocker, but the advertised primary setup path is incomplete. The issuer would make the resulting account harder to recognize in an authenticator.

**Fix:** Render the QR code, retain an accessible manual-key alternative, remove implementation guidance, identify the issuer as Pino, and provide a clear return to security settings. Explain the next verification step and recovery-code requirement in task language.

**Suggested command:** `$impeccable harden`, then `$impeccable clarify`.

### 3. [P1] Profile does not lead to the identity club staff see

**Evidence:** Live Profile contains only disabled Username and editable Phone number. `Pages/Manage/Index.razor:13–26` supplies that form. The required first name, last name, and photo are managed by `src/Pino.UI/Features/Clubs/Pages/ClubAccess.razor` and `Components/ProfileEditor.razor`; Account/Profile has no link or explanation connecting them.

**Impact:** A coach wanting to correct their name or change their photo reaches the apparently correct destination and cannot complete the task. The same word Profile describes two different surfaces without explaining the distinction.

**Fix:** Show the person's club-visible name/photo or a concise summary and direct Edit name and photo link from Account/Profile. Label the remaining sign-in/contact fields accurately and explain the non-editable username. Preserve the existing profile editor and its server/browser boundary rather than duplicating profile behavior.

**Suggested command:** `$impeccable clarify` / `$impeccable onboard`.

### 4. [P2] Settings navigation dominates short forms and loses parent context

**Evidence:** `ManageNavMenu.razor.css:7` distributes six settings into five columns plus an isolated Personal data row at desktop widths. At 390px, the title/introduction/navigation precede the Profile heading at approximately y=454. Live EnableAuthenticator and DeletePersonalData had no current/active settings item; all six links returned `active=false` and no `aria-current` on EnableAuthenticator. Source shows the same route-family mismatch for RenamePasskey, SetPassword, and other 2FA child routes.

**Impact:** Small tasks carry a large scanning cost, and nested security tasks remove the location cue when users most need it. The h1 also stays generic while the task heading is much weaker.

**Fix:** Keep the task heading and form nearer the top on phones, group account/security/data choices, and map child routes to their parent settings section with a persistent selected state and a local back link. Keep desktop navigation within a deliberate layout rather than allowing one orphaned row. Retain visible options without replacing them with an unlabeled menu.

**Suggested command:** `$impeccable layout`, then `$impeccable adapt`.

### 5. [P1, source-confirmed] Passkey removal has no deliberate confirmation

**Evidence:** `Pages/Manage/Passkeys.razor:36` posts Delete directly beside Rename. `Passkeys.razor.cs:72–73,92` immediately calls `RemovePasskeyAsync`; no confirmation state intervenes. The live fixture had no passkeys, so a populated row and the deletion effect were not exercised.

**Impact:** A slip can remove a sign-in method. The neutral Rename/Delete presentation does not communicate the consequence before it happens, and the action is not undoable from the interface.

**Fix:** Present a confirmation naming the passkey and explaining that it will no longer sign in to Pino, with a clear Cancel action. Avoid claiming this will lock someone out without checking their remaining methods. Also explain passkeys in the empty state before asking a first-time user to add one.

**Suggested command:** `$impeccable harden` / `$impeccable clarify`.

## Cognitive load

**Moderate: three of eight checklist items fail.** Single focus passes for the small forms; grouping passes at the basic navigation-versus-form level; one thing at a time passes within forms; working-memory demand is generally low; progressive disclosure passes because each settings area opens separately. Chunking fails (six ungrouped destinations, seven when external providers are configured). Minimal choices fails at that same six-option settings decision. Visual hierarchy fails on the phone because the generic heading and navigation consume more initial space than the task itself.

Security adds avoidable interpretation work: the 2FA landing state does not explicitly describe its protection status, passkeys have no plain-language introduction, and the QR setup asks the person to translate developer help into a user action. Do not treat six settings alone as evidence of an unusable interface; the specific layout and missing context are the practical problems.

## Emotional journey

The quiet visual shell begins with reassurance. The first valley is arriving at Profile and finding neither the required photo nor the name used by colleagues. The deeper valley is attempting to improve security and seeing a missing QR code with developer-facing help. Existing source-defined success messages can provide a positive end after ordinary saves, but security completion needs a clearer safe endpoint: status confirmed, recovery codes saved, and a route back to account work. Deletion is appropriately serious, though a local Cancel/Keep my account action would make the safe exit easier to recognize.

## Persona red flags

- **Jordan, first-time volunteer coach:** Looks for a photo/name change under Profile and cannot find it. Passkeys has only an empty-state sentence and Add a new passkey, leaving purpose and device expectations unexplained. Authenticator setup's developer link undermines confidence.
- **Sam, keyboard or screen-reader user:** The visible focus outline is useful, but the Skip to content link goes to the club root. Headings jump from h1 to h3; nested security/data routes lose the active section cue. This review did not run NVDA or VoiceOver, so screen-reader announcement behavior is not claimed as tested.
- **Casey, distracted coach on a phone:** Before reaching the form, must pass the masthead, generic heading, repeated instruction, and six settings links. A 44px-plus field and clear Save button work well once reached. The source-confirmed immediate passkey Delete is a poor fit for hurried touch interaction.

## Minor observations

- Password requirements are not shown before entry; source validation can reveal them only after an attempted submission. Prefer concise requirements associated with New password.
- Phone number is a text input without `type="tel"` or `autocomplete="tel"`; use the appropriate native mobile keyboard and autofill affordance.
- Personal data offers an unspecified Download button and a separately named club-profile export; explain each export's contents before the choice.
- RenamePasskey uses an h4 prompt and Continue, without a page-specific title or explicit Save name/Back wording.
- The reusable status component uses `role="alert"` for routine successes as well as errors; assess polite status semantics during implementation.
- The prominent focus outline visible around the h1 in the saved captures is a real focus state, not a permanent decorative border; do not remove visible focus to fix aesthetics.

## Questions to consider

- Should Account/Profile become the person's clear identity hub with a summary and edit link, or should its label explicitly narrow it to account contact details?
- Would grouping the six settings into Profile, Security, and Data reduce navigation effort while keeping individual tasks directly reachable?
- What does a coach need to see immediately after enabling an authenticator to feel confident they can still sign in if their phone is lost?

Assessment A is complete. Detector execution, detector interpretation, combined snapshot persistence, and the critique's user-facing close belong to the parent synthesis and were not performed here.

Deterministic assessment: 0 findings in the six account Razor components. Independent browser inspection confirmed the skip-link defect, navigation space cost and heading gaps. Mutable injection unavailable; no overlay claimed.
