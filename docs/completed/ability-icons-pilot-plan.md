# Ability Icons Pilot — `.datf` Pack System & `IconTexture` Infrastructure

Implementation plan for the first production use of the unified asset format (see Track 1 in [modernization-suggestions.md](modernization-suggestions.md)). Scope is intentionally narrow: ability icons only, as a bounded pilot that proves out the full modern-asset pipeline (`.datf` container, PNG decode, modern-first/legacy-fallback dispatch, offset-aware rendering) without committing to migrating tiles, creatures, or effects yet.

## Context

- Legacy skill/spell icons live in `skill001/002/003.epf` and `spell001/002/003.epf` in the `Setoa` DAT archive. `002` and `003` exist as redundant full sheets for the Learnable and Locked states, shipping effectively three copies of each icon with only a blue or grey tint difference. This is both wasteful and hard-coded.
- Modern icons should be 32×32 PNGs (vs. legacy 31×31 EPF) at power-of-2 dimensions that every authoring tool expects.
- When rendering a 32×32 modern icon into a slot sized for 31×31 legacy, the render point shifts 1px up and 1px left so the overhang lands on the slot's outer border padding rather than bleeding into adjacent slots.
- The pilot lays the groundwork for future `.datf` asset packs (tiles, creatures, UI, etc.) — the pack-discovery and manifest conventions established here are reused wholesale.

## Goals

1. Ship a `hybicons.datf` asset pack — a ZIP file with `.datf` extension — containing modern 32×32 PNG ability icons. Legacy EPF sheets remain completely untouched.
2. Client dispatches: modern icon first, legacy fallback if the modern version isn't present. Zero regression on existing icons if `hybicons.datf` is absent.
3. Replace the redundant `skill002/003.epf` and `spell002/003.epf` sheets with tint-based overlays for Learnable (blue) and Locked (grey) states. Same visual result, one source of icon art per sprite.
4. Multi-sheet addressing: a single sprite ID space maps across any number of icon sheets in the pack via a convention (`skill_0001.png` through `skill_{N}.png`). Sheet count is open-ended.
5. Establish `.datf` pack-discovery + manifest conventions reusable for future asset packs.
6. Carry the 1px render offset explicitly via an `IconTexture` record so the render pipeline can support mixed legacy/modern icons without per-call-site format knowledge.

## Non-goals

- No changes to legacy EPF loaders, archives, or rendering paths. `skill001.epf` and `spell001.epf` remain authoritative for icons not shipped in the modern pack.
- No migration of other asset types (tiles, creatures, effects, UI). Those come later via the broader Track 1 work.
- No player-facing settings or UI for enabling/disabling packs. Presence-based discovery only.
- No hot-reload of packs. Archive opened at startup; restart required to pick up new packs.

## Architectural overview

Four cooperating components:

1. **`.datf` pack discovery.** On startup, scan `DataContext.DataPath` for `*.datf` files. For each, open via `System.IO.Compression.ZipArchive` (the file is a renamed ZIP — see format decision below). Read the embedded `_manifest.json` to learn the pack's identity, content type, and coverage. Register the pack in a typed registry (`AssetPacks.IconPack`, future `AssetPacks.TilePack`, etc.).

2. **`AssetPackRegistry`.** A static registry exposing typed lookups: `AssetPackRegistry.GetIconPack()` returns the active icon pack or null. Multiple packs of the same type are supported with declared priority (via `manifest.priority`), though for the pilot we expect at most one icon pack active.

3. **`IconTexture` record.** A small value type (`Texture2D Texture, int OffsetX, int OffsetY`) replacing raw `Texture2D` returns from icon-rendering methods. Callers draw via `iconTexture.Draw(sb, slotPosition)` which applies the offset. Legacy icons use `IconTexture.Legacy(t)` (offset 0,0); modern icons use `IconTexture.Modern(t)` (offset -1,-1).

4. **Modernized `UiRenderer` icon accessors.** `GetSkillIcon` and `GetSpellIcon` return `IconTexture`. Internally they try the icon pack first (if present and covers the requested sprite ID), fall back to the legacy EPF path. The `Learnable` and `Locked` states are no longer separate accessors — they become tint parameters passed to `IconTexture.Draw`.

### `.datf` format

- File extension: `.datf` (renamed ZIP, no custom header). `ZipArchive` reads content via magic bytes, extension is cosmetic.
- Internal structure:
  ```
  hybicons.datf
  ├── _manifest.json              # pack metadata
  ├── skill_0001.png              # modern skill icon for sprite ID 1
  ├── skill_0002.png
  ├── skill_0267.png              # the 267th skill icon
  ├── spell_0001.png
  └── spell_0042.png
  ```
- PNG filenames use 4-digit zero-padded sprite IDs (supports up to 9999 per family; extendable to 5 digits if needed).
- Missing files are valid — a pack can provide partial coverage. Lookup returns null; client falls back to legacy.

### `_manifest.json` schema

```json
{
  "schema_version": 1,
  "pack_id": "hybicons",
  "pack_version": "1.0.0",
  "content_type": "ability_icons",
  "priority": 100,
  "covers": {
    "skill_icons": { "dimensions": [32, 32] },
    "spell_icons": { "dimensions": [32, 32] }
  }
}
```

- `schema_version` — integer, incremented on breaking changes to the schema itself. Client rejects newer versions than it understands.
- `pack_id` — unique identifier; used for logging and deduplication if two packs claim the same id.
- `pack_version` — semver; shown in client debug overlay for support.
- `content_type` — enum discriminator, known values for pilot: `ability_icons`. Future: `tiles`, `creatures`, `ui_sprites`, `effects`, `bundle` (for multi-type packs).
- `priority` — integer; higher wins when multiple packs cover the same asset ID. Default 100. For the pilot, at most one icon pack is expected.
- `covers` — capability declaration: which asset categories the pack participates in, plus per-category metadata the renderer needs (e.g., `dimensions` drives the offset calculation). It's not a range declaration — the pack's file contents are its actual coverage.

**Emergent coverage, not declared coverage.** The manifest deliberately has no `mode` field (replace vs. additive) and no `id_range` field. Legacy sheets already have unpopulated slots (e.g., `skill001.epf` has content in slots 1-97 and blanks in 98-266), so the replace-vs-additive distinction is emergent from which IDs the pack ships:

| Pack contains | Legacy has content at this ID? | Effective behavior |
| --- | --- | --- |
| `skill_0001.png` | Yes | Modern replaces legacy for slot 1 |
| `skill_0050.png` | Yes | Modern replaces legacy for slot 50 |
| `skill_0097.png` | No (slot 97+ blank in legacy) | Modern adds new content at slot 97 |
| Nothing at slot 42 | Yes | Falls back to legacy for slot 42 |

Both patterns — "full modern replacement for slots 1-97" and "additive expansion pack at slot 97+" — use the same runtime resolution (modern-first, legacy-fallback per ID). The only difference is which PNGs the artist shipped in the pack. Nothing about the manifest, the client, or the loader needs to change between the two.

This keeps the runtime logic simple and lets pack authors mix strategies within a single pack if they want (e.g., modernize the first 50 icons *and* add 20 new ones at previously-blank IDs).

## Phases

### Phase 1: `IconTexture` record + offset-aware drawing

**Scope**

- Add `IconTexture` record struct with `Texture`, `OffsetX`, `OffsetY`, static factories `Legacy` and `Modern`, and a `Draw(SpriteBatch, Vector2, Color?)` helper.
- Change `UiRenderer.GetSkillIcon(ushort)` and `GetSpellIcon(ushort)` return types from `Texture2D` to `IconTexture`. Wrap legacy-path returns as `IconTexture.Legacy(...)`.
- Update the ~10 icon draw sites to call `iconTexture.Draw(sb, pos)` instead of `sb.Draw(texture, pos, Color.White)`. Sites:
  - [SkillBookPanel.cs:139, 142, 148](Chaos.Client/Controls/World/Hud/Panel/SkillBookPanel.cs)
  - `SpellBookPanel.cs` (same pattern as SkillBookPanel)
  - [MenuListPanel.cs:235, 265](Chaos.Client/Controls/World/Popups/Dialog/MenuListPanel.cs)
  - [MenuShopPanel.cs:533, 571](Chaos.Client/Controls/World/Popups/Dialog/MenuShopPanel.cs)
  - [AbilityMetadataDetailsControl.cs:117-125](Chaos.Client/Controls/World/Popups/Profile/AbilityMetadataDetailsControl.cs)
  - [AbilityMetadataEntryControl.cs:65-73](Chaos.Client/Controls/World/Popups/Profile/AbilityMetadataEntryControl.cs)

At this phase only legacy path exists. All offsets are (0, 0). Visual behavior is identical to current.

**Review gate**

- *Bug/regression review*: visual diff of every affected panel/popup against pre-change — skill book, spell book, ability metadata profile tab, menu shop, menu list. Pixel-perfect match expected (offset is still 0,0).
- *Architecture/design review*: `IconTexture` location (which project? probably `Chaos.Client.Rendering`), API shape (is `Draw` the right ergonomics?), consistency with how other renderers return textures.

### Phase 2: `.datf` pack discovery + `AssetPackRegistry`

**Scope**

- Create `AssetPackRegistry` static class in `Chaos.Client.Data` with startup-time discovery of `*.datf` files in `DataContext.DataPath`.
- For each discovered file: open as `ZipArchive`, read `_manifest.json`, validate `schema_version`, register by `content_type`.
- Expose typed accessors: `AssetPackRegistry.GetIconPack() : IconPack?`. `IconPack` wraps the `ZipArchive` and exposes `TryGetIcon(string prefix, int spriteId, out IconTexture)`.
- Archive lifetime: held open for process duration. Registry owns disposal on shutdown.
- Manifest parsing: use `System.Text.Json` (stdlib). Define manifest record types mirroring the schema.
- Graceful failure: missing `_manifest.json`, malformed JSON, or `schema_version` in the future all log a warning and skip the pack. No exceptions propagate to the caller.

**Review gate**

- *Bug/regression review*: legacy behavior preserved when no `.datf` files are present. No startup crash on corrupt/malformed packs. No file lock issues.
- *Architecture/design review*: registry API shape (typed accessors vs. generic `Get<T>`), manifest schema completeness, naming of the `covers` field, forward-compatibility path for new content types, whether priority-based conflict resolution is overengineering for MVP.

### Phase 3: Ability icon pilot — `hybicons.datf` + modern-first dispatch + tint cleanup

**Scope**

- Modify `UiRenderer.GetSkillIcon` and `GetSpellIcon` to:
  1. Query `AssetPackRegistry.GetIconPack()`.
  2. If a pack is registered, `TryGetIcon("skill", spriteId, out var iconTexture)` / `"spell"`. If success, return as `IconTexture.Modern(...)` (offset -1, -1).
  3. Otherwise fall through to existing EPF path, wrap as `IconTexture.Legacy(...)`.
- Delete the Learnable/Locked accessors:
  - `PanelSpriteRepository.GetSkillLearnableIcon`, `GetSkillLockedIcon`, `GetSpellLearnableIcon`, `GetSpellLockedIcon`
  - `UiRenderer.GetSkillLearnableIcon`, `GetSkillLockedIcon`, `GetSpellLearnableIcon`, `GetSpellLockedIcon`
- Rewrite the switch expressions in [AbilityMetadataDetailsControl.cs:117-125](Chaos.Client/Controls/World/Popups/Profile/AbilityMetadataDetailsControl.cs#L117-L125) and [AbilityMetadataEntryControl.cs:65-73](Chaos.Client/Controls/World/Popups/Profile/AbilityMetadataEntryControl.cs#L65-L73):
  ```csharp
  var baseIcon = isSpell ? renderer.GetSpellIcon(id) : renderer.GetSkillIcon(id);
  var tint = state switch {
      AbilityIconState.Learnable => LegendColors.CornflowerBlue,
      AbilityIconState.Locked    => LegendColors.DimGray,
      _                          => (Color?)null
  };
  // Draw call: baseIcon.Draw(sb, pos, tint);
  ```
- Icon PNG lookup in the pack: `{prefix}_{spriteId:D4}.png` inside the archive. Returns null if file not found or decode fails.
- Bake the modern-icon offset (-1, -1) into `IconTexture.Modern(...)` construction. If future asset types have different offsets (e.g., 2× icons might be -2, -2), extend the factory accordingly.
- Documentation: short artist-facing README alongside the `.datf` convention explaining:
  - File structure and naming convention
  - Dimensions (32×32 for v1)
  - How to create / update a pack (ZIP tooling, manifest requirements)
  - Rendering offset rationale (why PNGs are authored at 32×32 full canvas, not 31×31)

**Review gate**

- *Bug/regression review*:
  - Fresh clone without `hybicons.datf`: icons render identically to pre-change.
  - With `hybicons.datf` partially covering some sprite IDs: modern renders where provided, legacy fallback elsewhere, no visual glitches at boundaries.
  - With a complete pack: all icons visibly modern, correctly offset, Learnable/Locked states tinted correctly, no caller-side regressions.
  - Missing file mid-pack (`skill_0042.png` absent): graceful fallback to legacy for that ID only.
  - Corrupt PNG entry: logged, falls back to legacy, no crash.
- *Architecture/design review*:
  - Correct offset application (-1/-1 for 32×32 into 31×31 slots; review if dimensions change the formula).
  - No file lock issues preventing content updates during dev.
  - Cache key uniqueness (`skill:{id}` is still safe since spriteId is global).
  - The tint-vs-separate-sheet decision survives contact with real assets — if Hybrasyl's actual 002/003 sheets have art that differs from pure tint, document the divergence.

### Phase 4: Final review

**Scope**

- End-to-end review of the entire changeset:
  - Correctness across all icon rendering surfaces.
  - Architectural consistency with the Additive Modernization Pattern — legacy path untouched, modern layer sits alongside.
  - No new public API surface that isn't justified by current need.
  - Performance: ZIP entry lookup is O(1) after init; startup cost of discovery is bounded by `.datf` count × manifest parse (trivial).
- CLAUDE.md updates: document the new `AssetPackRegistry`, `IconTexture`, and `.datf` convention in the architecture section. Note the pack discovery startup phase.
- [modernization-suggestions.md](modernization-suggestions.md) Track 1 updated to reflect that ability icons are now in-flight as the pilot, and that the `.datf` + `IconTexture` + `AssetPackRegistry` scaffolding is reusable for subsequent asset types.

**Review gate**

- *Final bug/regression*: full manual playtest — login, walk, open skill book, open spell book, cast a spell (check cooldown tint), open profile ability metadata tab, browse Learnable and Locked states. Compare against pre-change baseline.
- *Final architecture/design*: does the scaffolding (`AssetPackRegistry`, `IconTexture`, `.datf` convention) cleanly support the *next* pack type (tile pack) without refactoring? Identify any shortcuts that would bite us when extending.

## Critical files

| Path | Change |
|---|---|
| [Chaos.Client.Rendering/](Chaos.Client.Rendering/) | New: `IconTexture.cs` |
| [Chaos.Client.Data/](Chaos.Client.Data/) | New: `AssetPackRegistry.cs`, `AssetPackManifest.cs`, `IconPack.cs` |
| [Chaos.Client.Data/DataContext.cs](Chaos.Client.Data/DataContext.cs) | Add `AssetPackRegistry.Initialize()` to `DataContext.Initialize` |
| [Chaos.Client.Rendering/UiRenderer.cs](Chaos.Client.Rendering/UiRenderer.cs) | Change `GetSkillIcon`/`GetSpellIcon` return type; add pack dispatch; remove 4 Learnable/Locked methods (~60 lines net down) |
| [Chaos.Client.Data/Repositories/PanelSpriteRepository.cs](Chaos.Client.Data/Repositories/PanelSpriteRepository.cs) | Delete 4 Learnable/Locked accessors |
| [Chaos.Client/Controls/World/Popups/Profile/AbilityMetadataDetailsControl.cs](Chaos.Client/Controls/World/Popups/Profile/AbilityMetadataDetailsControl.cs) | Rewrite switch to tint-based |
| [Chaos.Client/Controls/World/Popups/Profile/AbilityMetadataEntryControl.cs](Chaos.Client/Controls/World/Popups/Profile/AbilityMetadataEntryControl.cs) | Rewrite switch to tint-based |
| [Chaos.Client/Controls/World/Hud/Panel/SkillBookPanel.cs](Chaos.Client/Controls/World/Hud/Panel/SkillBookPanel.cs), SpellBookPanel, MenuListPanel, MenuShopPanel | Update icon draw sites to use `IconTexture.Draw` |
| [CLAUDE.md](CLAUDE.md) | Document new scaffolding |
| [docs/asset-pack-format.md](docs/asset-pack-format.md) | **New**: artist/contributor-facing format spec (file structure, manifest schema, naming conventions, offset rationale) |

## Verification

1. `dotnet build Chaos.Client.slnx` — expect 0 errors.
2. Launch against Hybrasyl QA (`qa.hybrasyl.com:2610`) with no `hybicons.datf` present. Login → verify skill book, spell book, and profile ability metadata tabs render identically to pre-change.
3. Create a minimal `hybicons.datf` containing:
   - `_manifest.json` with `pack_id: "test"`, covers skill+spell icons
   - `skill_0001.png` (32×32 test image with a visible marker)
   - `spell_0001.png` (ditto)
   Drop into `DataContext.DataPath`, relaunch. Verify skill ID 1 and spell ID 1 show modern icons; other IDs unchanged.
4. Add a malformed entry (`skill_0002.png` that's 0 bytes). Verify graceful fallback to legacy for that ID only.
5. Add a manifest with `schema_version: 999`. Verify pack is rejected and client continues without it.
6. Manually verify offset: a modern icon at a known slot draws 1px up and 1px left of where legacy rendered.
7. Learnable/Locked tint: in profile ability metadata tab, confirm blue overlay on learnable entries and grey on locked entries, both with legacy base icons and modern base icons.

## Total effort estimate

~3 FTE-hours (single-engineer). Matches the earlier estimate plus ~30 min for the `AssetPackRegistry` manifest-discovery scaffolding.

## Forward references

Once the pack system is in place:

- **Tile pack** (`hybtiles.datf`) — extends `AssetPackRegistry` with `GetTilePack()`, covers background/foreground tile IDs. Uses the same manifest conventions.
- **Creature pack** (`hybcreatures.datf`) — replaces MPF for creature sprites. Introduces frame-array PNGs and animation metadata in the manifest.
- **UI pack** (`hybui.datf`) — replaces prefab EPF sheets for buttons, panels, etc.

All of these inherit `.datf` container format, `_manifest.json` schema (extended per type), `AssetPackRegistry` discovery, and the modern-first/legacy-fallback dispatch pattern. The pilot's scaffolding is the foundation.
