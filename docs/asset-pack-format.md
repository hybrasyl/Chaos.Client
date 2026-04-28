# `.datf` Asset Pack Format

Spec for Hybrasyl's modern asset packs — the distribution format for PNG-based overrides to legacy Dark Ages sheets. Written for artists, content authors, and the engineers maintaining the pack pipeline.

## What a pack is

A `.datf` file is a **ZIP archive with a different extension** — no header, no custom framing, no encryption. Any ZIP tool can open one after you rename it to `.zip`. The different extension exists so casual Dark Ages folder poking doesn't surface "oh that's a zip full of PNGs," but it's not a security boundary.

A pack ships modern PNG assets that the client uses in preference to the legacy `.dat`/`.epf` sheets. If the pack is missing a specific asset, the client falls back to legacy for that asset alone.

## Installing / using a pack

Drop the `.datf` file into the Dark Ages data folder (same directory as the legacy `.dat` archives). The client scans that directory at startup and registers any `*.datf` files it finds. No external config or registry entry — presence is registration.

Restart the client to pick up a new pack. Hot-reload isn't currently supported.

## Pack structure

Each `.datf` is a ZIP archive containing:

```
{PackName}.datf
├── _manifest.json         # required — pack metadata
├── skill_0001.png         # ability-icon pack: skill icon for sprite ID 1
├── skill_0002.png
├── skill_0097.png         # and so on
├── spell_0001.png
└── spell_0042.png
```

The manifest is required; everything else is convention-based depending on the pack's `content_type`.

## Manifest schema

`_manifest.json` at the ZIP root:

```json
{
  "schema_version": 1,
  "pack_id": "hybabilityicons",
  "pack_version": "1.0.0",
  "content_type": "ability_icons",
  "priority": 100,
  "covers": {
    "skill_icons": { "dimensions": [32, 32] },
    "spell_icons": { "dimensions": [32, 32] }
  }
}
```

| Field | Required | Notes |
|---|---|---|
| `schema_version` | yes | Integer. Always `1` for current clients. Bump only on breaking changes. Clients reject packs declaring a schema version they don't understand. |
| `pack_id` | yes | Lowercase identifier. Unique per pack. Used for logging. |
| `pack_version` | yes | Semver. Informational; shown in debug overlay. |
| `content_type` | yes | Type discriminator. v1 known values: `ability_icons`, `nation_badges`, `legend_mark_icons`. Future: `tiles`, `creatures`, `ui_sprites`, `effects`, `bundle`. |
| `priority` | no | Integer, default `100`. Higher wins when multiple packs of the same type are registered. |
| `covers` | yes | Capability declaration: which categories this pack participates in, with per-category metadata the renderer needs. |

The `covers` field is a **capability** declaration, not a coverage range. It tells the client "this pack participates in the ability-icon lookup pipeline" — but actual coverage emerges from which PNG files the pack ships. You do NOT need to enumerate every sprite ID in the manifest.

## Content type: `ability_icons`

Ability-icon packs override the legacy `skill001.epf` and `spell001.epf` sheets. v1 target dimensions are **32×32 PNG with alpha**.

### Naming convention

```
skill{id:D4}.png    e.g. skill0001.png, skill0042.png, skill0267.png
spell{id:D4}.png    e.g. spell0001.png, spell0042.png
```

- Naming mirrors the legacy EPF convention (`skill001.epf`, `spell001.epf`) — no underscore, 4-digit zero-padded sprite ID. Case-insensitive (`.PNG` and `.png` both work).
- Sprite ID is **1-based** and matches legacy slot numbering. `skill0001.png` replaces legacy skill001's slot 1; `skill0050.png` replaces slot 50.
- IDs beyond legacy's populated range (legacy `skill001.epf` typically has content through ~slot 97, blanks 98-266) are new content. `skill0100.png` is a brand-new icon the legacy sheet didn't provide; the client shows it when the server references sprite ID 100.
- Missing PNGs fall back to legacy (if that slot is populated) or show nothing (if it isn't).

### Replace vs additive — just ship what you want

The replace-vs-additive distinction is **emergent from which files you ship**, not a manifest field:

| You ship | Legacy has content at this ID? | What the player sees |
|---|---|---|
| `skill_0001.png` | Yes | Modern replaces legacy for slot 1 |
| `skill_0050.png` | Yes | Modern replaces legacy for slot 50 |
| `skill_0097.png` | No (slot 97+ blank in legacy) | Modern adds new content at slot 97 |
| Nothing at slot 42 | Yes | Falls back to legacy for slot 42 |

A pack can ship ANY mix of the above. "Full modern replacement pack" = ship PNGs for all slots 1-N. "Expansion pack" = ship PNGs for new slots only. "Targeted replacement" = ship PNGs for specific slots you want to modernize and leave the rest legacy.

### Dimensions & rendering offset — the 31 vs 32 thing

Legacy Dark Ages ability icons are **31×31**. Modern icons are **32×32** — industry-standard power-of-2 dimensions that every authoring tool expects. When a 32×32 icon renders into a legacy 31×31 slot, the client shifts its draw position **1 pixel up and 1 pixel left**. The icon overruns the slot's outer border padding rather than bleeding into adjacent slots.

**Authoring guidance:** Draw at the full 32×32 canvas. Don't leave a transparent row or column trying to match the legacy 31×31 footprint — the extra pixel is the modernization benefit. Crisper edges, clean power-of-2 dimensions, no weird cropping.

### Learnable and locked states (important change from legacy)

The legacy format shipped **three full icon sheets per ability family**: `skill001.epf` (known), `skill002.epf` (learnable), `skill003.epf` (locked). The `002` and `003` sheets were just `001` with a blue or grey tint applied.

Modern packs ship **one icon per sprite ID**. The client applies the tint at render time:

- **Known** (you have it / can cast it) — renders the base icon with no tint.
- **Learnable** (requirements met, can learn) — renders the base icon with a blue tint (`CornflowerBlue`).
- **Locked** (requirements not met) — renders the base icon with a grey tint (`DimGray`).

You do **not** ship `skill_0001_learnable.png` or similar. One PNG per ID. The client does the tinting.

## Creating a pack

1. Author the PNGs at 32×32 with transparent background and full alpha.
2. Create `_manifest.json` with the schema above.
3. Zip everything at the archive root (not inside a subdirectory). The manifest and all PNGs should be at the ZIP top level.
4. Rename the `.zip` to `.datf`.
5. Drop into the Dark Ages data folder.
6. Restart the client.

Any standard ZIP tool works — 7-Zip, Windows built-in "Send to compressed folder," `zip` on CLI, etc. Tools that want to see a `.zip` extension can be fed the file by temporarily renaming `.datf` → `.zip`, inspecting, then renaming back.

### Minimal example

```
hybicons.datf
├── _manifest.json
└── skill_0001.png
```

With `_manifest.json`:

```json
{
  "schema_version": 1,
  "pack_id": "hybicons-test",
  "pack_version": "0.1.0",
  "content_type": "ability_icons",
  "covers": {
    "skill_icons": { "dimensions": [32, 32] },
    "spell_icons": { "dimensions": [32, 32] }
  }
}
```

That ships one modern skill icon (sprite ID 1); everything else falls back to legacy.

## Content type: `nation_badges`

Nation-badge packs override the legacy `_nui_nat.spf` frame sheet — one image per nation, shown in the profile's equipment tab. Image dimensions are whatever the profile panel's `Nation` image slot is sized for; UIImage scales to its placed bounds, so a modern PNG at 2× or higher resolution displays fine (the point-filtered scale keeps pixel-art aesthetic intact).

### Naming convention

```
nation{id:D4}.png    e.g. nation0001.png, nation0002.png, nation0012.png
```

- Nation ID is **1-based**, matching the legacy frame-index-plus-one convention (legacy `_nui_nat.spf` frame 0 is nation 1, frame 1 is nation 2, etc.).
- Missing PNGs fall back to the legacy SPF frame for that nation.
- Case-insensitive (`.PNG` and `.png` both work).

### Minimal example

```
hybnations.datf
├── _manifest.json
├── nation0001.png
├── nation0002.png
└── nation0003.png
```

With `_manifest.json`:

```json
{
  "schema_version": 1,
  "pack_id": "hybrasyl-nation-badges",
  "pack_version": "0.1.0",
  "content_type": "nation_badges",
  "priority": 100,
  "covers": {
    "nation_badges": { }
  }
}
```

`dimensions` is optional for nation badges since the image is drawn at its placed UI bounds rather than a fixed slot size.

## Content type: `legend_mark_icons`

Legend-mark-icon packs override the legacy `legends.epf` frame sheet (palette 3 of `national.dat`) — the small icons shown beside each legend entry in the self profile's Legend tab. Legacy icons are roughly 21×20 pixels; modern PNGs are drawn at their own pixel dimensions into the row cell, so larger art will overflow until the Tier 3 panel-layout pass gives rows a larger icon box.

### Naming convention

```
legend{id:D4}.png    e.g. legend0000.png, legend0001.png, legend0042.png
```

- Icon ID is **0-based** — a deliberate deviation from the 1-based `ability_icons` and `nation_badges` conventions. The server sends each legend mark's icon as a raw byte used directly as the frame index into legacy `legends.epf`, so `legend0000.png` replaces EPF frame 0 and corresponds to server icon ID 0.
- Missing PNGs fall back to the legacy EPF frame for that ID.
- Case-insensitive (`.PNG` and `.png` both work).

### Minimal example

```
hyblegends.datf
├── _manifest.json
├── legend0000.png
├── legend0001.png
└── legend0002.png
```

With `_manifest.json`:

```json
{
  "schema_version": 1,
  "pack_id": "hybrasyl-legend-marks",
  "pack_version": "0.1.0",
  "content_type": "legend_mark_icons",
  "priority": 100,
  "covers": {
    "legend_mark_icons": { }
  }
}
```

`dimensions` is optional — the renderer uses each PNG's own pixel dimensions rather than a fixed slot size.

See [legend-mark-icons-authoring-guide.md](legend-mark-icons-authoring-guide.md) for art direction, legacy source extraction, and a full authoring walkthrough.

## Troubleshooting

- **Pack not loading:** check stderr for `[asset-pack]` warnings at startup. Common causes: missing `_manifest.json`, malformed JSON, `schema_version` higher than the client supports, unknown `content_type`.
- **Some icons render legacy despite being in the pack:** filename typo (case-insensitive but must match `{prefix}_{id:D4}.png` exactly), corrupt PNG entry, or PNG not at ZIP root.
- **Icon appears 1px off:** expected behavior — 32×32 icons draw at -1/-1 offset relative to where legacy 31×31 icons drew. If the visual looks wrong, verify the PNG is actually 32×32 and not 31×31.
- **Two packs both claim same icon:** higher `priority` wins; the other pack logs a warning at registration and is ignored for that content type.

## Future content types

The format is extensible. Planned future `content_type` values:

| `content_type` | What it covers | Expected additions to manifest |
| --- | --- | --- |
| `tiles` | Background + foreground map tiles (replaces tileset EPF, HPF) | Tile dimensions (e.g. 64×32 for modern iso), per-tile frame array for animated (water, lava) |
| `creatures` | Creature/NPC sprites (replaces MPF) | Frame array, animation tags (walk, standing, attack), anchor point |
| `ui_sprites` | UI buttons, backgrounds (replaces prefab EPF sheets) | Per-state mapping (normal, hover, pressed) |
| `effects` | Spell/combat effects (replaces EFA) | Frame timings, blend mode (additive), anchor |
| `bundle` | Multi-type pack (one archive shipping several categories) | `covers` enumerates multiple categories |

Implemented content types: `ability_icons`, `nation_badges`, `legend_mark_icons`.

The ability-icons schema is the minimal case. Future types will extend it; the v1 schema version won't change unless an existing field's meaning changes.
