# Locke's Airlocks

A feature-complete, DLC-aware airlock management script for Space Engineers,
built with [MDK2](https://github.com/malforge/mdk2).

> **Based on Blargmode's Aggressive Airlocks v6.4**
> This project is a modernized derivative of
> [Blargmode's Aggressive Airlocks](https://steamcommunity.com/sharedfiles/filedetails/?id=1219622614)
> from the Space Engineers Steam Workshop. Full attribution in [CREDITS.md](CREDITS.md).

---

## Features

| Type | Description |
|---|---|
| **Regular doors** | Auto-close after a configurable delay |
| **Tiny airlocks** | Two face-to-face doors — only one open at a time |
| **Smart airlocks** | One outer `#AL` door auto-paired with nearest inner door + air vent |
| **Group airlocks** | Full O2 management: outer + inner doors, vents, tanks, generators, farms, lights, LCDs, timers |
| **Hangars** | Toggled by pressing the vent's Depressurize action; inner door optional; air-scoop mode |
| **Simple group** | Door-only access control (no O2); prevents inner and outer opening simultaneously |

### DLC support (bleeding edge)
All DLC blocks that implement the standard SE interfaces work automatically.
Explicit modernization:
- **Gas tanks** — vanilla large O2 tank, Small Oxygen Tank, Fieldwork Lab Oxygen Tank, Prosperity
  Prototech Oxygen Tank, and any future O2 tank are all detected as oxygen. Hydrogen tanks are
  excluded by keyword detection (`Hydrogen`, `H2`) rather than the old hard-coded empty-subtype check.
- **Gates / large openings** — Frostbite Gate, Contact-pack Small Gate Tall/Wide, and all future
  `Gate`-subtype doors are treated as large openings (no auto-close, hangar-door behavior).

---

## Setup

1. Paste the script into a Programmable Block and press **Ok**.
2. The script initializes (usually 1–2 ticks). Check the PB detail panel for status.
3. Send the **`update`** command any time you build or change blocks.

---

## Tags

Add these to a block's **Custom Name** (anywhere in the name, whole-word):

| Tag | Meaning |
|---|---|
| `#AL` | Outer door in a smart or group airlock |
| `#Hangar` | Outer door in a hangar |
| `#Ignore` | Exclude this block from all airlock management |
| `#Manual` | Disable auto-close for this door |

---

## Commands

Enter as the **Run** argument of the Programmable Block (or via a button action):

| Command | Action |
|---|---|
| `update` | Re-scan all blocks (required after building changes) |
| `atmo` | Toggle atmosphere mode on/off |
| `atmo on` | Force atmosphere mode on |
| `atmo off` | Force atmosphere mode off |

---

## Airlock setup guides

### Tiny airlock
Place two doors face-to-face (touching, facing the same axis). Run `update`.

### Smart airlock
1. Build one outer door and add `#AL` to its name.
2. Build an untagged inner door nearby.
3. Build an air vent nearby.
4. Run `update` — the script finds the closest inner door and vent automatically.

### Group airlock (with O2 management)
1. Build outer doors (`#AL` tagged), inner doors, and one or more air vents.
2. Optionally add: oxygen tanks, O2/H2 generators, oxygen farms, lights, LCD panels, timer blocks.
3. Put everything into a **block group** (any name works).
4. Run `update`.

LCD panel name: set the **Public Title** on any LCD in the group to give the airlock a name
displayed on-screen and in the setup log.

Timer tags: add `#AL` to a timer block's name to make it an *outer* timer (triggered when the
outer side requests access). Leave untagged for an *inner* timer.

### Hangar
Same as group airlock, but use `#Hangar` instead of `#AL` on the outer door(s). The hangar is
toggled by pressing the air vent's **Depressurize On/Off** action (wire this to a button).

### Simple group airlock
Build outer doors (`#AL` tagged) and optionally inner doors. Put them in a group.
No air vents required. If **all** doors in the group are tagged, only one can open at a time
(solo mode — useful for single-door airlocks or turret hatches).

---

## Settings

All settings live in the **Custom Data** of the Programmable Block. Edit the value after the
colon, then run `update` to apply.

| Setting | Default | Description |
|---|---|---|
| Airlock tag | `#AL` | Tag for outer doors |
| Hangar tag | `#Hangar` | Tag for hangar outer doors |
| Ignore tag | `#Ignore` | Exclude a block |
| Manual tag | `#Manual` | Disable auto-close on a door |
| Auto close delay entering (s) | `0.5` | How long an airlock door stays open when entering |
| Auto close delay exiting (s) | `2.0` | How long a door stays open when exiting |
| Auto close regular doors | `yes` | Enable auto-close on ungrouped doors |
| Airlock free light color | Green | Light color when airlock is idle |
| Airlock in use light color | Violet | Light color while cycling |
| Auto disable atmo mode above (m) | `5000` | Altitude above which atmosphere mode auto-disables |
| [Advanced] Timeout (s) | `2` | How long to wait for vent progress before retrying |
| [Advanced] Timeout oxygen delta (%) | `20` | Minimum O2 change that counts as progress |

---

## Building from source

Requirements: [.NET 9 SDK](https://dotnet.microsoft.com/download), Space Engineers installed,
MDK2 templates (`dotnet new install Mal.Mdk2.ScriptTemplates`).

```bash
# Build and auto-deploy to IngameScripts/local
dotnet build -c Release LockesAirlocks/LockesAirlocks.csproj

# Run unit tests
dotnet test LockesAirlocks.Tests/LockesAirlocks.Tests.csproj -c Release
```

The built script is deployed automatically to
`%APPDATA%\SpaceEngineers\IngameScripts\local\LockesAirlocks\`.

---

## License

MIT — see [LICENSE](LICENSE). This project is a community derivative; see [CREDITS.md](CREDITS.md).
