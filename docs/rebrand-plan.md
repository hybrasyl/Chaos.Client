# Rebrand scoping: Chaos.Client → `$NEW_NAME`

> **Status:** scoping only. Name TBD. Execution deferred — revisit when the team is ready to commit to a name. When ready, hand execution to the project-lead agent per CLAUDE.md.

## Context

The current project identity (`Chaos.Client`, `Chaos.Client.Data`, `Chaos.Client.Rendering`, `Chaos.Client.Networking`, `Chaos.Client.Tests`) collides semantically with the external `Chaos.*` NuGet packages it consumes (`Chaos.Networking`, `Chaos.Common`, `Chaos.DarkAges`, `Chaos.Geometry`, `Chaos.Pathfinding`) — all upstream dependencies from the Chaos-Server/Sichii ecosystem. The `Chaos.Client` prefix makes it look like another package in that family, which this repo is not: it's a Hybrasyl-team learning reference for a future Godot client, not a Chaos-Server downstream. A distinct name removes the ambiguity and lets the repo's identity stand on its own.

Target name is **to be decided** — this plan uses `$NEW_NAME` as a placeholder (and `$new_name` for lowercase/path forms). The runtime brand strings (`"Darkages"` window title, `"Darkages.cfg"` settings file) are intentionally out of scope and can be revisited separately.

## TL;DR effort estimate

**Size: Small — 0.5 to 1 developer-day** once the name is chosen. Mostly mechanical find-and-replace plus a GitHub rename. No architectural work, no user-visible runtime changes.

## Inventory (what needs to change)

| Surface | Count / files | Notes |
|---|---|---|
| Solution file | 1 (`Chaos.Client.slnx`) | Rename file + update project paths inside |
| Project files | 5 `.csproj` | Each in its own folder — folders rename too |
| Project folders | 5 (`Chaos.Client`, `Chaos.Client.Data`, `.Rendering`, `.Networking`, `.Tests`) | `git mv` to preserve history |
| Namespace declarations | ~250 .cs files across 14 sub-namespaces | Mechanical replace |
| `using` directives | 408 across 163 files | Mechanical replace |
| Fully-qualified references | 655 across the codebase | Mechanical replace; WorldScreen.cs has the highest density (21) |
| XML doc `<see cref>` references | 3 (WorldState.cs, DrawState.cs, IconPack.cs) | Mechanical |
| README.md | 1 file | Human rewrite of prose; title + architecture diagram |
| CLAUDE.md | 1 file | Opening line + every `Chaos.Client.*` path reference |
| `docs/*.md` | ~20 files with code-link paths | Mostly mechanical; spot-check for false positives |
| Git remote (origin) | `hybrasyl/Chaos.Client` | Rename via GitHub UI, update remote |
| Local folder | `e:\Dark Ages Dev\Repos\Chaos.Client` | Close workspace, rename, reopen |

## What stays (don't touch)

- **External NuGet references**: `Chaos.Networking`, `Chaos.Common`, `Chaos.DarkAges`, `Chaos.Geometry`, `Chaos.Pathfinding` — these are upstream packages from Chaos-Server, not part of the rebrand.
- **Window title** (`"Darkages"` in `ChaosGame.cs:147`) — already decoupled from project name. Revisit separately if desired.
- **Settings filename** (`"Darkages.cfg"` in `ClientSettings.cs:9`) — already decoupled. Revisit separately if desired.
- **`Assembly.Load("Chaos.Networking")`** in `GlobalSettings.cs` — external assembly, not this one.
- **Sibling repo references** in CLAUDE.md (`../Chaos-Server/`, `../server/`, `../dalib/`) — those are other projects, names unchanged.
- **`controlFileList.txt`** — does not reference the project name.

## Landmines (none significant)

- No `InternalsVisibleTo` attributes anywhere.
- No strong-name signing.
- No reflection that resolves types by fully-qualified string name against this assembly.
- No MonoGame content pipeline bindings tied to assembly name.
- No GitHub Actions / CI workflows (none exist in `.github/`).
- No `%AppData%/Chaos.Client/` style branded paths — user data lives under the game-data `DataPath` as `Darkages.cfg`.
- No hard-coded "Chaos.Client" or "Chaos Client" strings in user-facing UI or logs.

One non-issue worth noting: upstream remote `Sichii/Chaos.Client` will visually decouple from origin after the GitHub rename. That's fine — this isn't a production fork, it's a learning reference.

## Open decision

- **The name.** That's the gating input. Suggested criteria: distinct from `Chaos.*`, not already a nuget.org package, short enough to type comfortably, no trademark collision with a known MMO asset.

## Execution plan (when name is chosen)

Five phases, each with a review gate (per CLAUDE.md review policy). Gates are **bug/regression review + architecture/design review** by separate reviewers before moving on.

### Phase 1 — Code rename (in-place, no git/folder moves yet)
- Global find-and-replace `Chaos.Client` → `$NEW_NAME` across `.cs`, `.csproj`, `.slnx`, `.md`.
- Verify the 5 external `Chaos.*` NuGet references remain unchanged.
- Build and run. Runtime behavior must be byte-identical.
- **Gate:** bug/regression + architecture review.

### Phase 2 — Folder and file renames
- `git mv` each project folder to new name. `git mv` each `.csproj` to match. Rename `.slnx`.
- Update internal path references in `.slnx` and any `<ProjectReference Include="...">` paths.
- Build and run.
- **Gate:** bug/regression + architecture review.

### Phase 3 — Documentation rewrite
- README.md: title, intro paragraph, architecture section.
- CLAUDE.md: opening paragraph, every `Chaos.Client.*` path reference.
- `docs/*.md`: mechanical replace, spot-check 3–5 files for false positives (e.g., a doc discussing both the client rename and the Chaos.Networking NuGet in the same paragraph).
- **Gate:** architecture/design review (bug review N/A for docs).

### Phase 4 — GitHub repo rename + remote update
- Rename `hybrasyl/Chaos.Client` → `hybrasyl/$new_name` via GitHub UI. GitHub auto-redirects old URLs.
- Update local remote: `git remote set-url origin ...`.
- Decide upstream handling: keep tracking `Sichii/Chaos.Client` (fine — GitHub preserves the fork relationship under the old name) or remove the remote entirely (acceptable since the fork isn't load-bearing).
- **Gate:** project-lead sign-off (no code; coordination risk).

### Phase 5 — Local working-tree folder rename
- Close VSCode workspace.
- Rename `e:\Dark Ages Dev\Repos\Chaos.Client` → `e:\Dark Ages Dev\Repos\$new_name`.
- Reopen workspace.
- Verify sibling-repo relative paths (`../Chaos-Server/`, `../server/`, `../dalib/`) still resolve — they should, since those folders didn't move.
- **Gate:** smoke test only (no reviewers).

### Final review
Comprehensive review of the full changeset: build clean, tests green (Chaos.Client.Tests suite), no stale `Chaos.Client` strings in code/docs, GitHub repo resolves, upstream/origin remotes correct.

## Verification

- `dotnet build <NewName>.slnx` succeeds.
- `dotnet run --project <NewName>/<NewName>.csproj` — client connects to the lobby, logs in, enters world, moves, chats, opens HUD panels. Run the `Chaos.Client.Tests/` suite (renamed accordingly).
- `git grep -i "chaos\.client"` returns zero results outside of CHANGELOG / historical notes (if any).
- `git grep "Chaos\."` still shows the 5 external NuGet references — confirm these are intact.
- GitHub URL resolves at both old (auto-redirect) and new names.

## Branch note

Scoping docs (this file) commit straight to main. When the rebrand is executed, it should land on a fresh branch off `main`, not on any in-progress feature branch.
