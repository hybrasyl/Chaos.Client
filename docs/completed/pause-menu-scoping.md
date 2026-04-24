# Center-Screen Escape-Key Pause Menu — Scoping Answer

**Nature of this doc:** Scoping answer to *"what would the effort be to move the option-button items into a center-screen Escape-key menu?"* — not a build commitment.

**Decisions locked in by the scoping conversation:**

- **Replace** `MainOptionsControl` entirely (no supplement).
- **Custom-drawn** panel using existing UI atlas pieces (no new art, no new prefab).
- **Same item set** as today — Sound slider, Music slider, Friends, Macros, Settings, Exit Game, Close.

## Context

Today the "options" button (`BTN_OPTION`) in the HUD opens [MainOptionsControl](../Chaos.Client/Controls/World/Popups/Options/MainOptionsControl.cs) — a slide-in-from-right panel using the `_noptdlg` prefab, positioned at bottom-left. It already handles its own Escape-to-close, already participates in `InputDispatcher`'s control stack, and already wires the five action events (Friends/Macros/Settings/Exit/Close) plus the two sliders. The target is a centered-on-screen panel that opens/closes with Escape, "like a normal game pause menu."

## Estimated Effort

**~1 day of focused work.** The machinery (control stack, sliders, button atlas, DialogFrame compositing) all exists and is proven by `OkPopupMessageControl`. The work is one new control, one new key binding, a small wiring rename, and a one-line hide of the old HUD button.

## Scope of Work

### 1. New `PauseMenuControl` (~150 lines)

- Location: [Chaos.Client/Controls/World/Popups/Options/PauseMenuControl.cs](../Chaos.Client/Controls/World/Popups/Options/PauseMenuControl.cs) (new file).
- Pattern: mirror [OkPopupMessageControl](../Chaos.Client/Controls/Generic/OkPopupMessageControl.cs) for the custom-drawn background:
  - `DlgBack2.spf` tiled interior via [DialogFrame.Composite](../Chaos.Client/Utilities/DialogFrame.cs)
  - `dlgframe.epf` 16×16 border (`DialogFrame.BORDER_SIZE`)
  - `butt001.epf` button frames (follow the `OK_NORMAL` / `OK_PRESSED` constant pattern in `OkPopupMessageControl.cs`)
- Layout: two sliders stacked at top (Sound, Music, same `SliderControl` used today), four buttons stacked below (Friends, Macros, Settings, Exit Game), plus a Close button.
- Positioning: center of the 640×480 viewport. Compute on construction from measured width/height; re-center on `SetViewportBounds` (same pattern as `MainOptionsControl.SetViewportBounds`).
- Lifecycle: copy the five events and their signatures verbatim from `MainOptionsControl` (`OnMacro`, `OnSettings`, `OnFriends`, `OnExit`, `OnClose`, `OnSoundVolumeChanged`, `OnMusicVolumeChanged`) — keeps the wiring edits in step 4 to a rename.
- Escape-to-close: copy the `OnKeyDown` handler at [MainOptionsControl.cs:151](../Chaos.Client/Controls/World/Popups/Options/MainOptionsControl.cs#L151) — including the `X` key for Exit.
- Drop the `SlideAnimator`. Use `Show()`/`Hide()` with direct `Visible` toggling + `InputDispatcher.PushControl`/`RemoveControl`.

### 2. Open-via-Escape handler

In [WorldScreen.InputHandlers.cs](../Chaos.Client/Screens/WorldScreen.InputHandlers.cs), add a new branch in the world-keyboard handler. Ordering matters — the existing cancel-targeting branch at [line 595](../Chaos.Client/Screens/WorldScreen.InputHandlers.cs#L595) must stay first:

1. If `CastingSystem.IsTargeting` → cancel (existing).
2. Else if no popup is currently on the `InputDispatcher` control stack → `PauseMenu.Show()`.

The second branch needs a way to read "is the control stack empty." If `InputDispatcher` doesn't already expose that, add a small read-only accessor (e.g. `HasStackedControls` / `StackDepth`). Quick check needed during implementation; likely a one-line addition.

When the menu itself is open, it's on top of the stack and `OnKeyDown` routes Escape to the menu per the existing control-stack model — no special handling needed in `WorldScreen`.

### 3. Rewire in `WorldScreen.Wiring.cs`

At [WorldScreen.Wiring.cs:764](../Chaos.Client/Screens/WorldScreen.Wiring.cs#L764), replace the `hud.OptionButton.Clicked += …` block and the `MainOptions.OnClose += …` line. Repoint the five `MainOptions.On*` subscriptions to `PauseMenu.On*`. Because event names are copied verbatim from `MainOptionsControl`, this is effectively a find-replace on the identifier.

### 4. Hide `BTN_OPTION` in the HUD

Both HUDs create it:

- [WorldHudControl.cs:183](../Chaos.Client/Controls/World/Hud/WorldHudControl.cs#L183)
- [LargeWorldHudControl.cs:169](../Chaos.Client/Controls/World/Hud/LargeWorldHudControl.cs#L169)

Simplest: after `CreateButton("BTN_OPTION")`, set `OptionButton.Visible = false` (or don't call `CreateButton` at all — `PrefabPanel` only creates controls requested by the panel). The prefab data file is untouched. Drop the `OptionButton` property from [IWorldHud.cs](../Chaos.Client/Controls/World/Hud/IWorldHud.cs) and both impls.

### 5. Delete `MainOptionsControl`

- Remove [MainOptionsControl.cs](../Chaos.Client/Controls/World/Popups/Options/MainOptionsControl.cs).
- Remove `MainOptions` ownership wherever it's declared in `WorldScreen` (field + `new MainOptionsControl()` construction + `Root.AddChild(MainOptions)` call).
- `SlideAnimator` usage here is the only consumer worth checking — if it's used nowhere else, it stays available for other slide-in panels; no need to delete it.

## Critical Files

- [WorldScreen.InputHandlers.cs:595](../Chaos.Client/Screens/WorldScreen.InputHandlers.cs#L595) — new Escape branch
- [WorldScreen.Wiring.cs:764](../Chaos.Client/Screens/WorldScreen.Wiring.cs#L764) — event rewire
- [WorldHudControl.cs:183](../Chaos.Client/Controls/World/Hud/WorldHudControl.cs#L183) + [LargeWorldHudControl.cs:169](../Chaos.Client/Controls/World/Hud/LargeWorldHudControl.cs#L169) — hide BTN_OPTION
- [IWorldHud.cs](../Chaos.Client/Controls/World/Hud/IWorldHud.cs) — drop `OptionButton`
- [OkPopupMessageControl.cs](../Chaos.Client/Controls/Generic/OkPopupMessageControl.cs) — pattern reference for custom-drawn panel
- [MainOptionsControl.cs](../Chaos.Client/Controls/World/Popups/Options/MainOptionsControl.cs) — source for slider/button wiring (then deleted)
- [DialogFrame.cs](../Chaos.Client/Utilities/DialogFrame.cs) — composite helpers
- [InputDispatcher.cs](../Chaos.Client/InputDispatcher.cs) — may need small `StackDepth` accessor
- [SliderControl.cs](../Chaos.Client/Controls/Generic/SliderControl.cs) — reused as-is

## What This Doesn't Touch

- `FriendsListControl`, `MacrosListControl`, `SettingsControl` — each is opened by its own event handler from the Wiring file; unchanged.
- Other HUD buttons (Chat, Inventory, Skills, etc.) — unchanged.
- Escape-in-targeting, Escape-in-dialog, Escape-in-textbox — all handled by their own controls via the control stack; the new world-level Escape branch only fires when the stack is empty.
- Keybinding customization — Escape is hardcoded. If configurable bindings are planned, that's a separate effort.

## Open Decisions (small, deferred)

- **Key label in HUD** — should the Help/F1 overlay advertise "Esc: Menu"? One-line add to [HotkeyHelpControl](../Chaos.Client/Controls/World/Popups/HotkeyHelpControl.cs) if yes.
- **Dim-the-world overlay** — typical pause menus dim or blur the world behind the panel. A simple translucent black quad before the menu draws. Pure polish; easy to add or skip. Not included in the effort estimate above.
- **Pause vs. "menu open"** — Dark Ages is server-authoritative and can't actually pause. Naming this "pause menu" is cosmetic; the world continues to update behind it. Worth flagging to avoid user confusion, but no code implication.

## Verification (when built)

- Escape on an empty stack opens the centered menu; Escape again closes it.
- Escape while a dialog/popup/textbox is open routes to that control, not the menu (control-stack behavior unchanged).
- Escape while targeting cancels targeting (existing behavior preserved).
- Each of the five action items still opens/triggers the correct downstream dialog.
- Sliders still persist volume via `ClientSettings`.
- `BTN_OPTION` is not visible on either HUD layout.
- Rapid open/close cycles don't leak control-stack entries.

## Review Gates

Per CLAUDE.md — phase-level bug/regression + architecture/design review after implementation; final review at the end. Given the size, this is likely a single-phase review.

## Bottom Line

**One new ~150-line control, one new Escape branch, a rename-style wiring edit, and a one-line hide in two HUDs.** All supporting machinery — sliders, button atlas, dialog-frame compositing, control-stack Escape routing — is already in place and has working precedents (`OkPopupMessageControl` for the visuals, `MainOptionsControl` for the event shape). The biggest real risk is the input-stack accessor needing to be added to `InputDispatcher`; everything else is mechanical.
