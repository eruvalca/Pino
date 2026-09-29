# Documentation review hooks

Documentation maintenance belongs to the implementation task. Before an
authorized commit and before finishing, the agent checks whether its work changes
setup, behavior, architecture, conventions, or confirmed product/design decisions.
It updates the affected documents and briefly reports the outcome. Accurate docs
stay unchanged; no acknowledgement file or token is required.

## Codex integration

[`.codex/hooks.json`](../.codex/hooks.json) contains two project-owned command
handlers alongside Impeccable's existing entries. Both run
[`Invoke-DocumentationReviewHook.ps1`](../scripts/Invoke-DocumentationReviewHook.ps1)
using PowerShell 7 and Git on `PATH`, resolving the script from the Git root so
sessions started in subdirectories work.

The Windows overrides explicitly invoke PowerShell 7 with command text that is
safe under both PowerShell and `cmd.exe`. Avoid `$variables` or `$()` inside the
outer double-quoted `-Command` argument: a PowerShell caller expands them before
the child process starts. Batch-only syntax such as `if exist` also fails under
a PowerShell caller. Resolve the Git root inside the child using
`(git rev-parse --show-toplevel)` and `Join-Path`.

- `UserPromptSubmit` supplies a short review instruction before work starts and
  records a workspace baseline for the session/turn.
- `Stop` compares that baseline with the current workspace. If it changed, the
  hook asks the current agent for one final documentation pass. The agent reads
  the relevant implementation and docs and makes any needed edits through its
  usual tools. If it already reviewed the docs, it can simply confirm the outcome.

The hook uses Codex's `decision: "block"` response at `Stop` to request a
continuation. This continues the agent; it does not reject a Git commit or ask the
user for approval. A per-turn marker and `stop_hook_active` prevent repeat passes.
Plan-mode turns and unchanged workspaces receive no finishing pass. Pre-existing
dirty files alone do not trigger it. Files changed by another task during the
same turn can trigger a reminder, so candidate paths are explicitly scoped by the
agent to its own task.

The comparison includes staged/unstaged changes, untracked files not ignored by
Git, content changes to already-dirty files, and changes to `HEAD`. State contains
hashes, candidate paths, and a commit ID under ignored
`artifacts/agent-hooks/documentation/`; no prompts, transcripts, or file contents
are stored. Missing baselines skip the finishing pass. Comparison errors report
a warning and allow the task to finish. State can be removed with other local
build artifacts when no agent task is using it.

The hook never rewrites documents itself, invokes another model, stages files,
commits, changes Git configuration, or blocks a commit. The early instruction
and [AGENTS.md](../AGENTS.md) establish the pre-commit review; the finishing hook
is a fallback and can run after a commit. This is assistance, not enforcement or
an automated guarantee that prose is accurate. Manual commits and other agents
are unaffected; other agents still follow `AGENTS.md`.

## Activation and maintenance

Reload Codex after changing the manifest. Review and trust the project layer and
new/changed hooks through Codex's hook controls (`/hooks` in the CLI); checked-in
configuration does not grant trust. No Git-hook installation is needed. See the
[official Codex hook documentation](https://learn.chatgpt.com/docs/hooks) for the
event protocol and trust controls. These are command handlers because Codex
currently skips handlers declared as `type: "prompt"` or `type: "agent"`.

Keep Impeccable's events, matcher, and detector invocation intact. Its Windows
command overrides have a project compatibility adjustment using the same
PowerShell launch pattern and Git-root resolution; the installed skill and
launcher remain upstream-owned. Review `.codex/hooks.json` after running its
installer to retain that adjustment and the project-owned documentation handlers.
To disable this feature, disable only the handlers labelled **Preparing documentation review**
and **Reviewing documentation impact** in Codex's hook controls.

Before relying on Impeccable hooks on a new machine, initialize its pinned engine
once from a terminal with network access and permission to write the user cache:

```powershell
.\.agents\skills\impeccable\scripts\impeccable.cmd engine-probe
```

The launcher reads the engine version from its installed `scripts/VERSION`,
verifies the downloaded checksum, and caches it under the user's `.impeccable`
directory. A successful probe prints `impeccable-engine` and its version. Hook
execution should not be the first attempt to download the engine, especially
with the short per-edit timeout. Keep the cache outside source control.

After changing the script or manifest, run from the repository root:

```powershell
pwsh ./scripts/Test-DocumentationReviewHook.ps1
```

The check exercises JSON events in isolated Git fixtures under ignored
`artifacts/`, including dirty baselines, edits, staging, commits, loop prevention,
and failure handling. On Windows it also runs the actual manifest commands through
PowerShell 7, Windows PowerShell, and `cmd.exe` from a subdirectory in a path with
spaces. A fixture stub checks both Impeccable events, stdin and argument forwarding,
engine failure, and an absent optional skill without downloading the engine.
It does not launch a model or establish that a particular desktop session has
loaded/trusted the hooks. Verify that separately in Codex.
