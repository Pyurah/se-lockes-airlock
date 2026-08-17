# Locke's Airlocks

A feature-complete, DLC-aware airlock management script for Space Engineers.
Modernized derivative of Blargmode's Aggressive Airlocks. See CREDITS.md.

## Tech Stack

| Layer | Technology |
|---|---|
| Script runtime | Space Engineers Programmable Block (C# 6, .NET Framework 4.8) |
| Build system | MDK2 v2.2.x (`Mal.Mdk2.PbPackager`, `Mal.Mdk2.PbAnalyzers`, `Mal.Mdk2.References`) |
| SDK | .NET 9 (build host only) |
| Tests | NUnit 4, FakeItEasy 9, `mdk2pbtests` template |
| Language | C# 6 (LangVersion=6 enforced by csproj — no C# 7+ syntax) |

## Project Structure

```
LockesAirlocks/              # Main script project
  Program.cs                 # Entry point: ctor, Main, Save, commands, tick loop
  src/
    Core/                    # ScriptContext, AirlockController, BlockDiscovery,
    │                        # AtmosphereMonitor, ExecutionProfiler
    Airlocks/                # AirlockBase, AdvancedAirlock, Hangar, TinyAirlock,
    │                        # SimpleGroupAirlock, AirlockComponents, AirlockState
    Blocks/                  # ExtendedDoor, ExtendedAirVent, GasSystem, BlockClassifier
    Display/                 # StatusDisplay, FixedWidthText
    Config/                  # Settings, SettingsSchema
    Util/                    # TagMatcher, TimedMessage

LockesAirlocks.Tests/        # xUnit test project (actually NUnit via mdk2pbtests)
  Tests/                     # TagMatcherTests, SettingsSchemaTests,
                             # FixedWidthTextTests, BlockClassifierTests
  TestUtilities/             # Gateway.ProgramBuilder (from template)

original/                    # Original Aggressive Airlocks source (read-only reference)
```

## Build & Development Commands

```bash
# Build + auto-deploy script to IngameScripts/local
dotnet build -c Release LockesAirlocks/LockesAirlocks.csproj

# Run all unit tests
dotnet test LockesAirlocks.Tests/LockesAirlocks.Tests.csproj -c Release

# Build everything (solution)
dotnet build -c Release LockesAirlocks.sln
```

Deployed to: `%APPDATA%\SpaceEngineers\IngameScripts\local\LockesAirlocks\`

## Key Architectural Decisions

### C# 6 only
The SE programmable block sandbox enforces C# 6. No `out var`, no tuples, no pattern
matching (`is T x`), no local functions, no switch expressions. The `LangVersion=6`
csproj setting enforces this at compile time.

### MDK2 multi-file layout
MDK2 concatenates all `.cs` files in the `IngameScript` namespace into one script at
build time. Splitting into multiple files has zero runtime cost.

### DLC-aware block detection (BlockClassifier)
- Gas tanks: treated as oxygen unless subtype contains `Hydrogen`/`H2`. This correctly
  picks up the vanilla large O2 tank (empty subtype), Small Oxygen Tank, Lab Oxygen Tank,
  Prototech Oxygen Tank, and any future tanks without code changes.
- Large openings: `IMyAirtightHangarDoor` OR subtype contains `Gate`. Covers the Frostbite
  Gate and Contact-pack Small Gate Tall/Wide.

### Async init (IEnumerator<bool>)
`BlockDiscovery.Scan()` is an `IEnumerator<bool>` state machine that yields `true` when
over the instruction budget. `Program.Main` calls `Step()` once per tick during init,
spreading the scan across multiple frames.

### InAtmoChanged lifetime
`ScriptContext.InAtmoChanged` is set `true` for exactly one tick by `AtmosphereMonitor`
and reset to `false` at the end of the Update10 block in `Program.Main`.

## Testing Approach

The SE PB sandbox cannot run headless. Tests cover **pure logic** classes only:
- `TagMatcher`, `FixedWidthText`, `SettingsSchema`, `BlockClassifier` — no SE types needed
- State-machine integration tests (TinyAirlock, SimpleGroupAirlock) would need FakeItEasy
  fakes for `IMyDoor`; deferred to Phase 2

For in-game "logging": Echo output, per-airlock LCD status displays, execution profiler
(shown in PB detail panel), and the setup log in Custom Data. No host-side structured
logging applies inside the PB sandbox.

## Current Phase

Phase 1 complete (v0.1.0). See roadmap.md.
