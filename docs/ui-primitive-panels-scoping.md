# UI Primitive Panels — Scoping

Scope for a "Phase 0" addition to the UI modernization track: a code-defined, zero-asset authoring path for new (non-legacy) panels using only MonoGame primitives. Sister document to [ui-asset-pack-scoping.md](ui-asset-pack-scoping.md) and [ui-modernization-direction.md](ui-modernization-direction.md). Status: scoping only — not approved for implementation; pending team discussion.

## Problem

The recent [PauseMenuControl](../Chaos.Client/Controls/World/Popups/Options/PauseMenuControl.cs) was authored without a legacy `.txt` prefab — its buttons are plain `UIButton` instances configured with `BackgroundColor`/`BorderColor` and no textures, with `UILabel` overlays for text. It currently still depends on legacy archives for two things: a composited `dlgframe.epf` + `DlgBack2.spf` background, and an `option04.epf` slider thumb texture.

This raises a structural question the existing modernization docs don't answer: can we author *fully zero-asset* panels (no SPF/EPF, no `.datf` pack required, just MonoGame primitives + colored rects + bitmap-font labels)? Both [ui-asset-pack-scoping.md](ui-asset-pack-scoping.md) and [ui-modernization-direction.md](ui-modernization-direction.md) implicitly assume UI = legacy `.txt` prefab OR pack-supplied art. Code-defined primitive panels are a third pathway neither doc names.

## Short answer: yes — and it slots in as Phase 0 of the UI modernization track

Code-defined primitive panels complement (don't compete with) the asset-pack tier ladder. The pattern serves two distinct purposes simultaneously:

1. **In-client mockups** — rapid layout/UX prototyping with no asset pipeline involvement. Iterate in C#, no artist time required.
2. **Step 1 in the pipeline for new UI frames** (achievements, group finder, mentor, crafting, etc.) — author the panel in primitives first to lock structure, then per-panel decide whether to:
   - **Stay primitive forever** (utility panels — pause menu, simple confirms, options popups), opportunistically reskinned via Tier 2 button-state packs when those land.
   - **Graduate to Tier 3 XML layout + artist art** (feature panels with real visual ambition).

These are the same pattern, distinguished only by the per-panel "graduate or stay" decision made *after* the prototype works.

The MonoGame-side foundation is mostly already in place — the existing component primitives ([UIPanel](../Chaos.Client/Controls/Components/UIPanel.cs), [UIButton](../Chaos.Client/Controls/Components/UIButton.cs), [UIProgressBar](../Chaos.Client/Controls/Components/UIProgressBar.cs), [UILabel](../Chaos.Client/Controls/Components/UILabel.cs)) already support primitive-only rendering. The gaps to close in Phase 0 are: (1) a primitive-mode slider, (2) a place to put shared UI chrome colors, and (3) a roadmap for the other primitive components that don't exist yet.

## Decided

- **Frame approach:** simple 1-2px `BorderColor` rect, already supported by `UIPanel`. Modern-flat baseline. No programmatic beveled-frame helper.
- **Slider:** add a new `UiSlider` primitive-only control alongside the existing texture-driven [SliderControl](../Chaos.Client/Controls/Generic/SliderControl.cs). Keeps the legacy slider unmodified; primitive panels use the new control. Cleaner separation, easier to retire the legacy slider later if/when it stops being used.
- **`UiSlider` namespace placement:** [Chaos.Client/Controls/Components/](../Chaos.Client/Controls/Components/) alongside `UIButton`/`UIPanel`. Treats `UiSlider` as a first-class primitive component, not a legacy-style "generic" control.
- **Theme constants location (Phase 0):** extend [LegendColors](../Chaos.Client.Rendering/Definitions/LegendColors.cs). Single home for all named UI colors during Phase 0. See "Open future considerations" below — long-term, the color/theme model likely wants a rethink.
- **Phase 0 sequencing:** independent of pack tiers. Runs **before or alongside Phase 1** (`feature/ui-asset-packs-tier1`). Does not block or depend on Tier 1/2/3 pack work.

## Component audit — what exists, what's missing

### Existing primitive-capable components

| Component | Zero-asset render path | Status |
| --- | --- | --- |
| `UIPanel` ([UIPanel.cs](../Chaos.Client/Controls/Components/UIPanel.cs)) | `BackgroundColor` + `BorderColor` rects; `Background` texture is optional/nullable | Works |
| `UIButton` ([UIButton.cs](../Chaos.Client/Controls/Components/UIButton.cs)) | All five state textures nullable; falls through to `BackgroundColor`/`BorderColor`. Pause menu validates this. | Works |
| `UIProgressBar` ([UIProgressBar.cs](../Chaos.Client/Controls/Components/UIProgressBar.cs)) | First-class color-fill mode via `FillColor` (parameterless ctor) — explicit primitive support already designed in | Works |
| `UIElement` (base) | `DrawRectClipped` primitive available to all subclasses | Works |
| `UILabel` ([UILabel.cs](../Chaos.Client/Controls/Components/UILabel.cs)) | Uses bitmap fonts from `FontRepository` (DALib fonts loaded from legacy archives) | **Soft dependency** — fonts come from legacy, but that's a separate font-modernization track. Acceptable for now. |
| `UITextBox` | Same font dependency as `UILabel`; otherwise renders rect+caret primitively | Same caveat |
| `UIImage` | Texture-only — no primitive mode (and shouldn't have one; that's `UIPanel`'s job) | N/A |
| `SliderControl` ([SliderControl.cs:59](../Chaos.Client/Controls/Generic/SliderControl.cs)) | Early-returns if `ThumbTexture` is null — currently *cannot* render primitively | Gap — closed by new `UiSlider` (legacy `SliderControl` left as-is) |
| `ScrollBarControl` ([ScrollBarControl.cs](../Chaos.Client/Controls/Generic/ScrollBarControl.cs)) | Heavily texture-driven (uses `scroll.epf` for arrows, thumb, track tiles) | Texture-only today; primitive sibling is a future candidate (see below) |

### Missing primitive components — candidates for follow-up

A search for `Checkbox`, `Dropdown`, `Combo`, `Toggle`, `Radio` across the controls tree returns **zero hits**. The codebase has no primitive *or* texture-driven versions of any of these. Other recurring patterns surfaced by skimming `MainOptionsControl`, `SettingsControl`, `MailListControl`, `MenuListPanel`, `BoardListControl`, `FriendsListControl`, `SelfProfileTabControl`, `ItemTooltipControl`, and `FramedDialogPanelBase`.

In rough priority order:

| Candidate | Today | Notes |
| --- | --- | --- |
| `UiCheckBox` | Doesn't exist | Obvious miss. Settings/options panels currently use buttons-as-toggles or no toggles at all. Primitive: outlined square + optional check-rect. Trivial. |
| `UiListBox` / `UiScrollList` | Doesn't exist as a unified primitive; ad-hoc in [MailListControl](../Chaos.Client/Controls/World/Popups/Boards/MailListControl.cs), [MenuListPanel](../Chaos.Client/Controls/World/Popups/Dialog/MenuListPanel.cs), [BoardListControl](../Chaos.Client/Controls/World/Popups/Boards/BoardListControl.cs), [FriendsListControl](../Chaos.Client/Controls/World/Popups/Options/FriendsListControl.cs) | High-value consolidation. Each list panel currently hand-rolls UILabel rows + selection index + scroll wiring. A primitive `UiListBox` would absorb most of that boilerplate. Pairs with a primitive scrollbar (below). |
| `UiTabControl` | Doesn't exist; tab patterns hand-rolled in [SelfProfileTabControl](../Chaos.Client/Controls/World/Popups/Profile/SelfProfileTabControl.cs), [OtherProfileTabControl](../Chaos.Client/Controls/World/Popups/Profile/OtherProfileTabControl.cs) | Tab-button row + content swap. Useful primitive for any future multi-section panel. |
| `UiScrollBar` | `ScrollBarControl` exists but is texture-driven (uses `scroll.epf` for arrows, thumb, track tiles); no primitive sibling | Lower urgency — texture-driven version is performant and visually tight. A primitive sibling would be useful for fully zero-asset list panels but loses the legacy visual fidelity. Pair with `UiListBox` decision. |
| `UiDropdown` / `UiCombo` | Doesn't exist | No precedent in the codebase. Useful for settings panels with finite options (currently handled as cycling buttons or text input). Lower priority — defer until concrete need. |
| `UiTooltip` | Ad-hoc in [ItemTooltipControl](../Chaos.Client/Controls/World/Popups/ItemTooltipControl.cs) | Cursor-following + content layout. Could abstract the positioning math but item tooltip has specific behavior; generic primitive may not be a clean fit. Low priority. |
| `UiFramedPanel` / `UiDialog` | [FramedDialogPanelBase](../Chaos.Client/Controls/World/Popups/Dialog/FramedDialogPanelBase.cs) exists but uses legacy `DlgBack2.spf` + `nd_f01-f08` border tiles | A primitive equivalent (modern-flat `UIPanel` with `BorderColor` + title bar) would replace one-off dialog assembly across pause menu, future confirmations, etc. Moderate priority. |

Phase 0 ships only `UiSlider`. The other candidates above are scoped here as a pipeline; each is its own small follow-up phase (Phase 0.x or distributed across later phases as concrete consumers appear).

## Phase 0 scope

### Work items

1. **New `UiSlider` primitive-only control** in [Chaos.Client/Controls/Components/](../Chaos.Client/Controls/Components/). Renders a track as a thin filled rect using `DrawRectClipped`, and a thumb as a small filled square at the value-derived X position. Mirrors the `SliderControl` API surface (`Value`, `ValueChanged`, `OnMouseDown`/`OnMouseMove`/`OnMouseUp`) so panels can swap between sliders without behavior changes. Track and thumb colors come from `LegendColors` extensions. ~80 LOC.

2. **Extend `LegendColors` with UI chrome constants.** Add the pause menu's `ButtonNormalBg` (`RGB 36,36,40`), `ButtonHoverBg`, `BorderColor` (`RGB 170,170,180`), and the new slider's track/thumb colors. ~20 LOC. Note: `LegendColors` today is initialized from the legend palette DAT; UI chrome constants are hardcoded values that don't fit that source pattern. This is acknowledged tension — see "Open future considerations" below.

3. **Promote `PauseMenuControl` to fully zero-asset.** Once (1) and (2) land:
   - Drop the `option04.epf` slider thumb lookup; instantiate `UiSlider` instead of `SliderControl`.
   - Replace the [DialogFrame.Composite()](../Chaos.Client/Utilities/DialogFrame.cs) call (`dlgframe.epf` + `DlgBack2.spf`) with a simple 1-2px `BorderColor` rect on a `UIPanel` background.
   - Pause menu becomes the canonical reference panel for the pattern.

4. **Document the track in [ui-asset-pack-scoping.md](ui-asset-pack-scoping.md).** Add a "Step 0 / Phase 0: code-defined primitive panels" section, explaining:
   - This is the entry point for *all* new non-legacy panels.
   - The per-panel "stay primitive vs. graduate to Tier 3" decision, made *after* the prototype works.
   - How Tier 2 button-state packs will opportunistically reskin primitive panels without panel rewrites — when the renderer learns to populate `UIButton.NormalTexture`/etc. from a state-aware pack, the existing color-rect fallback survives unchanged for users without the pack.

### Out of scope for Phase 0

- Any of the additional primitive components from the audit table (`UiCheckBox`, `UiListBox`, `UiTabControl`, `UiScrollBar`, etc.). Each is its own follow-up phase, sequenced based on concrete consumer needs.
- Per-panel color customization API. Phase 0 uses `LegendColors` as a single global home. The "set colors via panel creation" capability is flagged as a future design topic — see below.
- Generic "primitive → XML layout" converter. Premature. The Tier 3 graduation step is mechanical enough to do per-panel when each feature panel actually ships; a generic converter would get per-panel variant/conditional decisions wrong.
- Programmatic beveled-frame helper. Explicitly rejected. 1-2px borders are the modern-flat baseline.
- Font modernization. Separate track per [font-modernization-findings.md](font-modernization-findings.md). `UILabel`/`UITextBox` continue using legacy bitmap fonts; this is the one universal legacy dependency that survives Phase 0.
- Modifications to the existing `SliderControl`. The legacy slider stays as-is; `UiSlider` is purely additive.

### LOC estimate

| Component | LOC |
| --- | --- |
| New `UiSlider.cs` | ~80 |
| `LegendColors` extensions | ~20 |
| Pause menu cleanup (drop legacy frame + swap slider) | ~30 (mostly deletion) |
| Scoping doc edits in `ui-asset-pack-scoping.md` (Phase 0 section) | ~80 lines of markdown |
| **Total** | **~130 LOC + ~80 lines markdown** |

### Phase 0 review gates

Per [CLAUDE.md](../CLAUDE.md) review policy, each phase ends with bug/regression review + architecture/design review before proceeding:

- **After (1) `UiSlider`:** bug/regression review (does the primitive path render correctly across the value range? does mouse capture/drag behave identically to `SliderControl`?) + architecture review (does the API match `SliderControl` closely enough that panels can swap freely? is the new control appropriately placed in the namespace tree?).
- **After (3) pause menu promotion:** bug/regression review (visual diff of pause menu before/after; volume slider behavior parity) + architecture review (is the pattern documented well enough — both in this doc and the scoping-doc edit — for the next panel author to follow it without re-deriving the choices?).
- **Final review for Phase 0:** comprehensive review of `UiSlider` + `LegendColors` extensions + pause menu promotion + scoping doc edits as one coherent changeset.

## Why this fits cleanly with the existing modernization track

- **Phase 1 / `feature/ui-asset-packs-tier1`** keeps doing Tier 1 single-image content types (`legend_mark_icons` etc.). Unaffected.
- **Phase 2 (Tier 2 button states)**, when it lands, *automatically* upgrades primitive panels — `UIButton.NormalTexture`/`HoverTexture`/`PressedTexture`/`DisabledTexture` get populated by a state-aware pack lookup, the existing color-rect fallback survives unchanged for users without the pack. Zero panel-side changes required for any panel that adopted Phase 0 primitives.
- **Phase 3-4 (Tier 3 XML panel layouts)** becomes the graduation path for feature panels: control names and rect layout from the C# Phase-0 prototype translate near-directly into `<label name="..." rect="..."/>` etc. The C# subclass refactors from "build controls in constructor with hardcoded rects" to "build controls from `IPanelLayout.GetVariant(...)`" — the same refactor `ExtendedStatsPanel` is already pencilled in for as the Tier 3 pilot.
- **Primitives ↔ existing legacy panels** is *not* a relationship — Phase 0 only addresses *new* panels with no legacy counterpart. Reskinning existing legacy panels still wants the `ui_prefabs` (texture override) path from [ui-modernization-direction.md](ui-modernization-direction.md).

## Files involved

**Code:**

- New `Chaos.Client/Controls/Components/UiSlider.cs` — primitive-only slider.
- [Chaos.Client.Rendering/Definitions/LegendColors.cs](../Chaos.Client.Rendering/Definitions/LegendColors.cs) — extensions for UI chrome colors (button bg, border, slider track/thumb).
- [Chaos.Client/Controls/World/Popups/Options/PauseMenuControl.cs](../Chaos.Client/Controls/World/Popups/Options/PauseMenuControl.cs) — drop legacy frame composite + `option04.epf` lookup; swap to `UiSlider`.
- [Chaos.Client/Controls/Generic/SliderControl.cs](../Chaos.Client/Controls/Generic/SliderControl.cs) — *unchanged*; legacy slider stays as-is.

**Docs:**

- [ui-asset-pack-scoping.md](ui-asset-pack-scoping.md) — add Phase 0 section to the Rollout Phases list, plus a brief conceptual section explaining how Phase 0 sits before Tier 1.
- This document — captures the standalone scoping rationale.

## Tradeoffs

- **Visual fragmentation:** legacy panels keep their SPF/EPF look; Phase 0 panels look modern-flat; eventual Tier 3 panels look polished-modern. Three styles coexist for a long time. Mitigation is mostly aesthetic discipline — the modern-flat look needs to be deliberate, not placeholder.
- **`UILabel` still depends on legacy bitmap fonts.** Truly zero-archive isn't quite achievable until font modernization happens (separate track). Practically harmless: fonts are universal across all UI.
- **"Decide later" risk:** some panels will sit in primitive form indefinitely because the Tier 3 graduation never gets prioritized. That's fine if the primitive form is good enough; problematic if a panel was authored as scaffolding and reads as scaffolding. Counter: be honest about the per-panel "stay vs. graduate" call upfront, before authoring begins.
- **Two slider controls coexist:** `SliderControl` (legacy/texture-driven) and `UiSlider` (primitive). New panels pick the primitive one; existing legacy-prefab panels keep using `SliderControl`. Eventually the legacy slider can be retired if no consumers remain. Adds short-term surface area; pays back in clean separation.
- **Multiple authoring paths to maintain:** legacy `.txt` prefabs + Phase 0 primitive panels + (eventually) Tier 3 XML. Each adds mental overhead for new contributors. Mitigation: clear docs (the scoping-doc edit in work item 4) on which path applies when.
- **`LegendColors` mixes two concerns once Phase 0 lands:** palette-derived named colors (DAT-sourced) and hardcoded UI chrome constants. Acknowledged short-term cost; see "Open future considerations" for the rethink path.

## Open future considerations (post-Phase 0, not blocking)

These came out of scoping but are explicitly *not* part of Phase 0. Captured here so they're visible when the team plans follow-up phases.

1. **Color/theme model rethink.** [LegendColors](../Chaos.Client.Rendering/Definitions/LegendColors.cs) today is a static class of palette-derived `Color` properties initialized from `legend.pal` (DAT-sourced, `LegendColor` enum mapped to MonoGame `Color`). Adding hardcoded UI chrome constants there mixes two concerns. The bigger question the team flagged: **panels should eventually be able to set their own colors at construction**, not just consume globals. Options for a future pass:
   - **Optional ctor parameters** on each primitive panel (e.g., `new MyPanel(buttonTheme: ..., borderColor: ...)`) defaulting to the `LegendColors` constants. Smallest change.
   - **A `ButtonTheme` / `PanelTheme` record** passed to constructors, encapsulating a related color set. Cleaner API but more types.
   - **A theme registry** (`ThemeRegistry.Default.ButtonNormalBg`) overridable at startup. Most flexible, most indirection.
   - **Split `LegendColors`** into `LegendColors` (palette-derived) and a new `UiChromeColors` (hardcoded UI constants). Resolves the mixed-concern issue but doesn't address per-panel customization.

   No decision needed for Phase 0; resolve when a concrete consumer needs per-panel color override.

2. **Audit follow-through.** The component audit above lists `UiCheckBox`, `UiListBox`, `UiTabControl`, `UiScrollBar`, `UiDropdown`, `UiTooltip`, `UiFramedPanel` as candidates. Phase 0 ships only `UiSlider`. When the team picks the next primitive component, prioritize based on which panel is being built next and what controls it needs. Don't pre-build primitives without consumers.

3. **`SliderControl` retirement.** Once `UiSlider` is in place and pause menu is migrated, audit remaining `SliderControl` consumers. If none remain (or the legacy ones can also migrate cheaply), the texture-driven slider becomes deletable. Track but don't force.

4. **Modern-flat baseline color sanity-check.** The `RGB 36,36,40` background and `RGB 170,170,180` border were chosen ad-hoc when the pause menu was authored. Once these become canonical via `LegendColors`, worth a quick design pass — these are the de facto Hybrasyl modern look across every primitive panel forever, so they deserve more than "first thing that looked OK."

## Status

**Scoping only — not approved for implementation.** Decisions locked in the "Decided" section above, but the team should discuss findings before this work is sequenced. When approved, the implementation should be sent to the project-lead agent for orchestration per [CLAUDE.md](../CLAUDE.md) review policy.
