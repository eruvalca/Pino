# Account navigation and notebook refinements

Mode: Operate. Staff come here to change their identity/contact details or protect
their sign-in. Preserve Identity static SSR, named POST forms, antiforgery, passkey
hooks and the existing authorization boundary.

The independent account critique found broken skip navigation, disconnected club
profile editing, child pages losing their selected section, incomplete authenticator
setup guidance and immediate passkey removal. Resolve these alongside the requested
navigation improvement. The user authorized unattended decisions.

## Account behavior

- Desktop navigation groups Profile and Email under About you, password, connected
  accounts, two-factor authentication and passkeys under Sign-in & security, and
  personal data under Your data. Connected accounts appears only when an external
  sign-in provider is configured.
- Phone navigation exposes Profile, Security and Your data, followed by the
  current group's destinations. Child routes retain their parent's selection and
  a return link, including Rename passkey and authenticator/recovery-code pages.
- Profile links to the existing first-name, last-name and photo editor. Its own
  form edits the account phone number; email remains a separate Identity workflow.
- Authenticator setup renders its QR code locally with QRCoder and retains the
  manual key option. It does not send the setup secret to an external QR service.
- Removing a passkey requires expanding an inline confirmation naming that key
  before submitting the existing protected POST form. Keep passkey returns to the
  passkey list without deletion.
- Routine status messages use `role="status"`; errors retain `role="alert"`.
  Skip links target the content on the current account route, preserving queries.

## Direction contract

THESIS: Keep a person's settings in a recognizable place while each form stays the
visual focus. Group identity, security and data without hiding their child tasks.

OWN-WORLD: The established navy masthead, white sheet, cobalt selection and fine
rules continue. Use the notebook's typography at full strength without adding a
new palette, decorative imagery or promotional language.

STORY: Choose a settings group, complete one task, receive clear confirmation and
return to its parent. A profile link leads directly to the existing name/photo editor.

FIRST VIEWPORT: A concise account heading sits above a two-column workspace with
14rem grouped navigation beside the form. Phones replace the sidebar with three
group links and the current group's destinations above the form. Nested security
and data pages retain parent selection and a visible return link.

FORM: Focused refinement of the established account surface; no concept tournament.
Preserve its standard controls and authentication flow. Product-specific expression
comes from clear identity and dependable security guidance.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Motion and character

The emotional thesis is confidence that field-side work has become a dependable
club record. A larger, tabular bib anchors the selected player; the name remains
the primary reading anchor. Phone roster/notebook switches briefly move in their
navigation direction. A successfully saved observation settles from pale blue to
paper, keyed to that saved note rather than every render. Closeout earns a dated
receipt with its reviewed player count, author and retained results. These finite
effects run only on meaningful transitions, never delay input, and have no looping
or image cost. Reduced motion leaves all content, focus and textual feedback intact.
