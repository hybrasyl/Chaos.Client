# Epona integration — `Darkages.cfg` endpoint contract

[Epona](https://github.com/hybrasyl/epona) is the sibling Electron launcher that manages the Dark Ages / Hybrasyl stack on a developer or server-op's Windows machine. Its Stage 2 ("Hybrasyl Client" tab) launches Chaos.Client against any profile by writing the profile's `LobbyHost` / `LobbyPort` into `Darkages.cfg` before spawning the process. See `epona/docs/multi-target-expansion-plan.md` for the broader plan.

## Contract

Chaos.Client reads two optional keys from `<DataPath>/Darkages.cfg` at startup:

| Key         | Type   | Default            | Override source              |
|-------------|--------|--------------------|------------------------------|
| `LobbyHost` | string | `qa.hybrasyl.com`  | `GlobalSettings.DEFAULT_LOBBY_HOST` |
| `LobbyPort` | int    | `2610`             | `GlobalSettings.DEFAULT_LOBBY_PORT` |

Rules:

- Keys are **case-insensitive** (`LobbyHost`, `lobbyhost`, `LOBBYHOST` all match).
- A missing file, missing key, blank value, or malformed port falls back silently to the compile-time default — no error, no log.
- `LobbyPort` must parse as a decimal integer in `[1, 65535]`; otherwise default is used.
- Values are read once at static-initialization time. Restart the client to pick up cfg changes.

Line format matches the original retail cfg: `Key : Value` or `Key: Value`. The parser splits on the first `:`, trims both sides, and preserves raw value text (no quote stripping).

## Preservation guarantee

`ClientSettings.Save()` — triggered when the user changes an in-game option (volume, group settings, etc.) and on world exit — writes the Chaos.Client-owned key set and **preserves any other lines verbatim**. That means `LobbyHost` and `LobbyPort` written by Epona survive a user-triggered save; the launcher does not need to re-template the cfg on every launch, only when the configured endpoint changes.

If an external writer uses non-canonical spacing (e.g. `  LobbyHost:foo.com`), the line round-trips unchanged.

## Epona's obligations

Per the Epona plan, the launcher:

- Templates `Darkages.cfg` at the user-configured `dataPath` (same path Chaos.Client reads `GlobalSettings.DataPath` from — typically `E:\Games\Dark Ages`).
- Touches only `LobbyHost` and `LobbyPort`; preserves all other keys it finds.
- Writes the file before each spawn (or at least on endpoint change).

## Minimum Chaos.Client commit

Fill in the merge-commit SHA once this branch (`feat/cfg-driven-endpoint`) lands on `main`. Epona's README should reference that SHA as its minimum Chaos.Client version.

## Testing

- `Chaos.Client.Tests/DarkagesCfgTests.cs` — parser-level cases (casing, duplicates, blank values, colon-in-value, whitespace).
- `Chaos.Client.Tests/ClientSettingsIoTests.cs` — round-trip cases asserting unknown-key preservation, idempotency, and odd-spacing survival.

Manual: write `LobbyHost: 127.0.0.1` and `LobbyPort: 4200` to `Darkages.cfg`, launch, confirm the client connects to that endpoint; play briefly, change a sound setting, exit; reopen the cfg and confirm both lines are still present.
