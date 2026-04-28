# Sichii Upstream Review

Tracking doc for the review pass of commits landed on `Sichii/Chaos.Client` (upstream) since our fork last integrated from it. Companion to the per-commit deep dive that follows this scoping pass.

## Context

This repo was forked from `github.com/Sichii/Chaos.Client`. The last integration point in our history is `9d3ed3f Merge remote-tracking branch 'upstream/main'`. Since that merge, both sides have diverged. The purpose of this doc is to:

1. Enumerate the **18 unmerged upstream commits** so we can decide per-commit whether to pull them in.
2. Enumerate **our fork's commits** that look generic enough to share back to upstream (per kedian's reminder that we should have a path for contributing back when it makes sense).
3. Provide a stable reference for the deep-dive review sessions that follow.

Upstream clone lives at `../Chaos.Client.sichii/` (sibling directory, not a configured remote on this repo — history is unified via `git diff --no-index` / cherry-pick across directories when needed).

## Fork state (snapshot: 2026-04-22)

- Common ancestor with upstream: `9d3ed3f` (our last merge from upstream)
- Upstream HEAD: `523c7f6`
- Unmerged upstream commits: 18 (listed below, oldest-first for merge-order)
- Our fork commits since the merge: 20+ (roughly `9d3ed3f..HEAD`)
- Package-level divergence: upstream moved DALib to a NuGet consumption (`1be9871 swap to dalib nuget pkg`, already in our history). No other package-level drift known.

## Pull-in candidates from upstream

### Clear bug-fix wins (small, focused, likely conflict-free)

| SHA | Size (files +/-) | What it does | Deep-dive status |
|-----|-----|-----|-----|
| `523c7f6` | 10f +85/-55 | Initial click into inactive window no longer swallowed; longer double-click window; SPF pure-black → transparent | unreviewed |
| `7998dbb` | 2f +48/-19 | Turning in the direction you're walking no longer cancels the walk; effectBar icon fixes | unreviewed |
| `6f4771b` | 7f +140/-71 | Window maximize not aspect-locked; cast-target while chanting; Esc no longer cancels spell-targeting | unreviewed |
| `6c46755` | 7f +96/-82 | Display-param changes (equipment, form, restcloak) + turning cancel playing body animations + walking | unreviewed |
| `386f27e` | 5f +228/-18 | Ground tints now apply to all entities; sound popping/artifacts fixed | unreviewed |
| `1865284` | 1f +10/-8 | "possible fix for big lags" — one-file change, worth a look | unreviewed |
| `5930fb2` | 2f +9/-3 | ClientSettings path fix + one other bug | unreviewed |
| `e3a5829` | 1f +2/-2 | Fix "use shift key" option | unreviewed |
| `b9456bb` | 4f +22/-10 | Misc bugfixes | unreviewed |

Start the deep dive here. Small commits, clear intent, unlikely to touch our Hybrasyl divergence.

### Decision-required (large or structurally opinionated)

| SHA | Size (files +/-) | What it does | Why it's not an auto-pull |
|-----|-----|-----|-----|
| `473e3c7` | 11f +403/-411 | **Soundsystem rewrite** — NAudio → SDL2_mixer for cross-platform | Strategically aligned with Godot-future client ambitions, but CLAUDE.md documents NAudio 2.3.0 as the dep; may need a dedicated scope doc before integrating. Cross-platform wins are real. |
| `2368e85` | 21f +843/-795 | **Friendslist + ImageUtil centralization** | Near-certain conflict with our `6d1de6d` "friends list editing and spacing" commit. Sub-slice (ImageUtil only) may be worth extracting even if the friendslist part isn't. |
| `f11d341` | 14f +273/-66 | **Groupbox + control-focus** stack-push focus-steal | May collide with our InputDispatcher divergence; behavior change not just code change. |

Each of these deserves its own short scope pass.

### Cleanup commits (ignore unless specific lost content matters)

| SHA | Size (files +/-) | What it does |
|-----|-----|-----|
| `a9b24ce` | 5f +29/-520 | Chatinput click-focus fix + repo cleanup (mostly deletions) |
| `d37a100` | 4f +0/-122 | Cleanup |
| `574f4d5` | 2f +23/-68 | Cleanup |
| `41ce535` | 2f +2/-57 | Cleanup |
| `a100807` | 2f +9/-7 | Cleanup |
| `61f5d4b` | 1f +2/-1 | Cleanup |

Net −824 lines across six commits. If we care about any specific deletion, it can be cherry-picked; otherwise, blanket-skip to avoid conflicts with Hybrasyl additions in the same files.

## Share-back candidates from our fork

Derivation method: authored by someone other than `sichii` AND not present in upstream's object database. Twelve commits qualify; most are Hybrasyl-specific. Filter on that leaves a handful of genuine candidates.

**Note on my first pass of this doc:** I initially listed seven commits (`da3bcd3`, `f9dcea9`, `52befa3`, `6d1cb21`, `bb5e81b`, `0904c57`, plus BMFont) as candidates. All of the first six are authored by Sichii and came into our repo via the `9d3ed3f` upstream merge — they're already in upstream and not ours to share. Corrected table below.

| SHA | What it does | Share-back verdict |
|-----|-----|-----|
| `8511942` | BMFont/TTF font pipeline + docs | Clearest generic-infra candidate. `Font` repository is generic enough to upstream; Sichii may prefer legacy but worth offering. |
| `d74127b` + `f7ef738` | DoorTable regeneration from audit + hand-audited retail door sprite catalog | Upstream has their own DoorTable likely; the hand-audited catalog (`doors.md`) is the more valuable share-back regardless of whether they adopt our table. |
| `6d1776f` | Escape-key pause menu + friends list editing/spacing fixes | Generic UX improvements, **but** the friends-list portion will collide with upstream's `2368e85` reorg. Expect a partial cherry-pick (pause menu only) or hand-merge. |

### Not share-back candidates (Hybrasyl-specific or internal)

- `.datf` asset pack work (`8d9f2e4`, `bddb1f7`, `f8d32cf`): Hybrasyl asset direction
- Scoping docs in `docs/` (chair-sitting, extended-stats, profession-nodes, UI asset pack, this review): Hybrasyl modernization roadmap
- Hybrasyl debug/logging + compat (`1ca18a0`)
- Repo URL updates (`085b996`)
- Branch changelog for teammate catch-up (`502ba53`)
- Docs + Claude permissions expansion (`6d45e8d`)
- The upstream merge commit itself (`9d3ed3f`)

## Deep-dive workflow

For each pull-in candidate:

1. **Read the commit** in the upstream clone: `cd ../Chaos.Client.sichii && git show <sha>`.
2. **Identify touched files** and cross-check against our divergence on those same files since `9d3ed3f`: `git log 9d3ed3f..HEAD -- <path>` in this repo.
3. **Judge conflict risk:**
   - No overlap → safe cherry-pick candidate.
   - Overlap with cosmetic formatting-only → merge, ours wins or theirs wins by judgment.
   - Overlap with Hybrasyl semantic additions → hand-merge, preserve both.
4. **Record the decision** in this doc's "Deep-dive status" column: `pulled` / `skipped (reason)` / `deferred` / `pulled-partial (reason)`.

For share-back candidates:

1. Fork `Sichii/Chaos.Client` on GitHub (if not already).
2. In `../Chaos.Client.sichii`, add your fork as a second remote: `git remote add fork <your-fork-url>`.
3. Bring this repo's commit into the upstream clone: `git remote add hybrasyl ../Chaos.Client` then `git fetch hybrasyl`, then `git cherry-pick <sha>` onto a branch.
4. Push to your fork, open a single-commit PR upstream.

## Maintenance

Update this doc's status column as the deep dive progresses. Re-run the upstream scan (`git log HEAD..origin/main` inside the sichii clone, then cross-check against this repo) whenever upstream advances — the 18-commit count is a snapshot, not a stable fact.
