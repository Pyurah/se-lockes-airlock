# Locke's Airlocks — Roadmap

## Status: Phase 1 — Initial Release

---

## Phase 1: Initial Release `v0.1.0` ✅

**Milestone: working, tested, DLC-aware script**

| Deliverable | Status |
|---|---|
| MDK2 project scaffolded (`mdk2pbscript`) | ✅ |
| SE binary auto-detected, auto-deploy to IngameScripts | ✅ |
| Settings (parse/generate Custom Data) | ✅ |
| TagMatcher (whole-word, case-insensitive) | ✅ |
| FixedWidthText word-wrapper | ✅ |
| ExecutionProfiler | ✅ |
| BlockClassifier (DLC-aware O2 tank + gate detection) | ✅ |
| ExtendedDoor (auto-close, manual/large flags, program-open tracking) | ✅ |
| ExtendedAirVent (depressurize-change events) | ✅ |
| GasSystem (tank fill ratio, generator/farm management, air-scoop) | ✅ |
| TinyAirlock | ✅ |
| SimpleGroupAirlock (normal + solo mode) | ✅ |
| AdvancedAirlock (smart + group with O2) | ✅ |
| Hangar (vent-triggered, air-scoop, atmo-transition) | ✅ |
| AirlockComponents wiring struct | ✅ |
| AirlockController (per-tick loop + status LCDs) | ✅ |
| BlockDiscovery (async init, full scan with instruction budget) | ✅ |
| AtmosphereMonitor (altitude-gated atmo mode) | ✅ |
| Program.cs (commands, init, tick loop, Custom Data output) | ✅ |
| Unit tests — 55 tests, 0 failures | ✅ |
| Build: 0 errors, 0 warnings | ✅ |
| Script size: 69KB (well under 100KB PB limit) | ✅ |
| README with full setup guide | ✅ |
| CREDITS.md (Blargmode attribution) | ✅ |
| CHANGELOG.md | ✅ |
| GitHub repo: https://github.com/Pyurah/se-lockes-airlock | ✅ |

---

## Phase 2: In-Game Validation

| Deliverable | Status |
|---|---|
| Test regular door auto-close in game | ☐ |
| Test tiny airlock cycling | ☐ |
| Test smart airlock pressurize/depressurize cycle | ☐ |
| Test group airlock with O2 tanks | ☐ |
| Test hangar vent-toggle cycle | ☐ |
| Test simple group airlock + solo mode | ☐ |
| Test atmosphere mode (planet surface) | ☐ |
| Test all new DLC block types (Small O2 tank, Lab sliding door, Gate) | ☐ |
| Verify settings round-trip from Custom Data | ☐ |

---

## Phase 3: Polish and Workshop Release

| Deliverable | Status |
|---|---|
| Workshop thumbnail (thumb.png) | ☐ |
| Workshop description (Instructions.readme) | ☐ |
| Version bump to v1.0.0 | ☐ |
| Steam Workshop publication | ☐ |
| Localization string support (multi-language) | ☐ |

---

## Notes

- The script is deployed to `%APPDATA%\SpaceEngineers\IngameScripts\local\LockesAirlocks\`
  after every `dotnet build`.
- Build command: `dotnet build -c Release LockesAirlocks/LockesAirlocks.csproj`
- Test command: `dotnet test LockesAirlocks.Tests/LockesAirlocks.Tests.csproj -c Release`
