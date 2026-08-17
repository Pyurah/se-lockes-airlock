# Changelog

All notable changes to Locke's Airlocks are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

## [0.1.0] - 2026-08-17

### Added
- Complete rewrite of Blargmode's Aggressive Airlocks v6.4 as a clean, multi-file MDK2 project
- Regular door auto-close with configurable entering/exiting delays
- Tiny airlocks (two face-to-face doors, mutual exclusion)
- Smart airlocks (auto-paired nearest outer door + inner door + air vent)
- Group airlocks with full O2 management (doors, vents, tanks, generators, farms, lights, LCDs, timers)
- Hangar airlocks (vent-triggered, air-scoop mode, inner door optional)
- Simple group airlocks (door-only access control, solo mode)
- Atmosphere mode with altitude-based auto-disable
- Custom Data settings with round-trip parse/generate
- Setup log written to Custom Data on every `update`
- Commands: `update`, `atmo`, `atmo on`, `atmo off`
- Execution profiler (rolling instruction-count and runtime average)
- Per-airlock LCD status displays with configurable name and state text
- Inner/outer timer block triggers per airlock group
- Pressurize/depressurize safety timeout that re-opens the door rather than leaving the player stuck
- Async block scan spread across ticks to stay under the PB instruction budget
- DLC-aware oxygen tank detection: vanilla large O2 tank, Small Oxygen Tank, Lab Oxygen Tank
  (Fieldwork), Prototech Oxygen Tank (Prosperity), and all future non-hydrogen tanks included
- DLC-aware large-opening detection: IMyAirtightHangarDoor, Frostbite Gate, Contact-pack
  Small Gate Tall/Wide, and all future `Gate`-subtype doors handled correctly
- Unit test suite: 55 tests covering TagMatcher, SettingsSchema, FixedWidthText, BlockClassifier
- MDK2 project with auto-deploy to IngameScripts local folder on build

### Attribution
- Based on Blargmode's Aggressive Airlocks v6.4 (2020-08-27). See CREDITS.md.
