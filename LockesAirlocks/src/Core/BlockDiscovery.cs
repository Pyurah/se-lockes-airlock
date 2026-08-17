using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    /// <summary>
    /// Scans all blocks on the same grid construct and wires them into the correct airlock
    /// types. The scan is spread across multiple ticks via <see cref="Step"/> so it never
    /// exceeds the programmable block's per-tick instruction budget.
    ///
    /// Precedence order (same as original Aggressive Airlocks):
    ///   1. Block groups with vents → AdvancedAirlock (group) or Hangar
    ///   2. Block groups without vents but with tagged doors → SimpleGroupAirlock
    ///   3. Remaining face-to-face door pairs → TinyAirlock
    ///   4. Remaining outer-tagged doors + nearest inner + nearest vent → smart AdvancedAirlock
    ///   5. Remaining ungrouped doors (if auto-close on) → regular auto-close
    /// </summary>
    public class BlockDiscovery
    {
        readonly ScriptContext _ctx;
        readonly IMyGridTerminalSystem _gts;
        readonly IMyProgrammableBlock _me;
        readonly IMyGridProgramRuntimeInfo _runtime;
        readonly AirlockController _controller;
        readonly AtmosphereMonitor _atmoMonitor;
        readonly FixedWidthText _log;

        IEnumerator<bool> _machine;
        bool _done;

        public bool Done => _done;

        public BlockDiscovery(ScriptContext ctx, IMyGridTerminalSystem gts,
            IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime,
            AirlockController controller, AtmosphereMonitor atmoMonitor)
        {
            _ctx = ctx;
            _gts = gts;
            _me = me;
            _runtime = runtime;
            _controller = controller;
            _atmoMonitor = atmoMonitor;
            _log = new FixedWidthText(70);
        }

        public void Start()
        {
            _done = false;
            _machine = Scan();
        }

        /// <summary>
        /// Advance the scan one step. Returns the completed setup log text when fully done,
        /// null while still scanning.
        /// </summary>
        public string Step()
        {
            if (_done || _machine == null) return null;

            bool hasMore = _machine.MoveNext() && _machine.Current;
            if (!hasMore)
            {
                _done = true;
                _machine.Dispose();
                _machine = null;
                return _log.GetText();
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Scan coroutine — yields true to pause, false/falls-through when done
        // ------------------------------------------------------------------

        IEnumerator<bool> Scan()
        {
            _controller.Reset();
            _log.Clear();
            _log.AppendLine("Locke's Airlocks - Setup Log");
            _log.AppendLine(new string('-', 70));
            _log.AppendLine("Settings: see Custom Data.");
            _log.AppendLine();

            var s = _ctx.Settings;
            const int MaxPerTick = 8000;

            // ---- gather all same-construct, non-ignored blocks ---------------
            var allBlocks = new List<IMyTerminalBlock>();
            var allDoors = new List<IMyDoor>();
            var allVents = new List<IMyAirVent>();
            var allPanels = new List<IMyTextPanel>();

            _gts.GetBlocks(allBlocks);
            for (int i = allBlocks.Count - 1; i >= 0; i--)
            {
                if (Overbudget(MaxPerTick)) yield return true;
                var b = allBlocks[i];
                if (!b.IsSameConstructAs(_me)) { allBlocks.RemoveAt(i); continue; }
                if (TagMatcher.HasTag(s.IgnoreTag, b.CustomName)) { allBlocks.RemoveAt(i); continue; }
                if (b is IMyDoor) allDoors.Add((IMyDoor)b);
                else if (b is IMyAirVent) allVents.Add((IMyAirVent)b);
                else if (b is IMyTextPanel) allPanels.Add((IMyTextPanel)b);
                else if (b is IMyShipController) _atmoMonitor.SetController((IMyShipController)b);
            }

            // ---- phase 1: block groups --------------------------------------
            var groups = new List<IMyBlockGroup>();
            _gts.GetBlockGroups(groups);

            foreach (var group in groups)
            {
                if (Overbudget(MaxPerTick)) yield return true;

                var outerDoors = new List<IMyDoor>();
                var innerDoors = new List<IMyDoor>();
                var vents = new List<IMyAirVent>();
                var panels = new List<IMyTextPanel>();
                var lights = new List<IMyLightingBlock>();
                var tanks = new List<IMyGasTank>();
                var generators = new List<IMyGasGenerator>();
                var farms = new List<IMyFunctionalBlock>();
                var innerTimers = new List<IMyTimerBlock>();
                var outerTimers = new List<IMyTimerBlock>();
                bool hasHangarTag = false;

                var groupBlocks = new List<IMyTerminalBlock>();
                group.GetBlocks(groupBlocks);

                foreach (var b in groupBlocks)
                {
                    if (!b.IsSameConstructAs(_me)) continue;
                    if (TagMatcher.HasTag(s.IgnoreTag, b.CustomName)) continue;

                    if (b is IMyDoor)
                    {
                        var door = (IMyDoor)b;
                        if (TagMatcher.HasTag(s.AirlockTag, b.CustomName))
                            outerDoors.Add(door);
                        else if (TagMatcher.HasTag(s.HangarTag, b.CustomName))
                        {
                            outerDoors.Add(door);
                            hasHangarTag = true;
                        }
                        else
                            innerDoors.Add(door);
                    }
                    else if (b is IMyAirVent)
                        vents.Add((IMyAirVent)b);
                    else if (b is IMyTextPanel)
                    {
                        var panel = (IMyTextPanel)b;
                        panels.Add(panel);
                        allPanels.Remove(panel);
                    }
                    else if (b is IMyLightingBlock)
                        lights.Add((IMyLightingBlock)b);
                    else if (b is IMyGasTank)
                    {
                        var tank = (IMyGasTank)b;
                        if (BlockClassifier.IsOxygenTank(tank)) tanks.Add(tank);
                    }
                    else if (b is IMyGasGenerator)
                        generators.Add((IMyGasGenerator)b);
                    else if (b is IMyOxygenFarm)
                        farms.Add((IMyFunctionalBlock)b);
                    else if (b is IMyTimerBlock)
                    {
                        var timer = (IMyTimerBlock)b;
                        if (TagMatcher.HasAnyTag(b.CustomName, s.AirlockTag, s.HangarTag))
                            outerTimers.Add(timer);
                        else
                            innerTimers.Add(timer);
                    }
                }

                if (vents.Count > 0)
                {
                    if (!hasHangarTag && outerDoors.Count > 0 && innerDoors.Count > 0)
                    {
                        var comp = BuildComponents(outerDoors, innerDoors, vents, panels, lights, tanks, generators, farms, innerTimers, outerTimers);
                        _controller.AddAdvanced(new AdvancedAirlock(comp, "Airlock"));
                        AppendGroupLog("Group airlock " + _controller.AdvancedCount, outerDoors, innerDoors, vents, tanks, generators, farms, panels, lights, innerTimers, outerTimers);
                    }
                    else if (hasHangarTag && outerDoors.Count > 0)
                    {
                        var comp = BuildComponents(outerDoors, innerDoors, vents, panels, lights, tanks, generators, farms, innerTimers, outerTimers);
                        _controller.AddHangar(new Hangar(comp));
                        AppendGroupLog("Hangar " + _controller.HangarCount, outerDoors, innerDoors, vents, tanks, generators, farms, panels, lights, innerTimers, outerTimers);
                    }

                    allDoors = allDoors.Except(outerDoors).Except(innerDoors).ToList();
                    allVents = allVents.Except(vents).ToList();
                }
                else if (outerDoors.Count > 0)
                {
                    bool hasLargeOpening = outerDoors.Any(d => d is IMyAirtightHangarDoor);
                    if (!hasLargeOpening)
                    {
                        var extOuter = ExtendDoors(outerDoors).ToArray();
                        var extInner = ExtendDoors(innerDoors).ToArray();
                        _controller.AddSimpleGroup(new SimpleGroupAirlock(extOuter, extInner));
                        _log.AppendLine("\n> Simple group airlock " + _controller.SimpleGroupCount + " added (" + outerDoors.Count + " outer, " + innerDoors.Count + " inner)");
                        allDoors = allDoors.Except(outerDoors).Except(innerDoors).ToList();
                    }
                }
            }

            // ---- phase 2: tiny airlocks (face-to-face pairs) ----------------
            for (int i = 0; i < allDoors.Count; i++)
            {
                if (Overbudget(MaxPerTick)) yield return true;
                for (int j = i + 1; j < allDoors.Count; j++)
                {
                    var a = allDoors[i];
                    var b = allDoors[j];
                    var fwd = Base6Directions.GetIntVector(a.Orientation.Forward);
                    bool adjacent = (a.Position + fwd == b.Position) || (a.Position - fwd == b.Position);
                    bool aligned = a.Orientation.Forward == b.Orientation.Forward
                                || a.Orientation.Forward == Base6Directions.GetFlippedDirection(b.Orientation.Forward);

                    if (adjacent && aligned)
                    {
                        _controller.AddTiny(new TinyAirlock(ExtendDoor(a), ExtendDoor(b)));
                        _log.AppendLine("\n> Tiny airlock " + _controller.TinyCount + ": [" + a.CustomName + "] + [" + b.CustomName + "]");
                        allDoors.RemoveAt(j);
                        allDoors.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }

            // ---- phase 3: smart airlocks (nearest outer+inner+vent) ---------
            var smartOuter = new List<IMyDoor>();
            var smartInner = new List<IMyDoor>();
            for (int i = allDoors.Count - 1; i >= 0; i--)
            {
                if (TagMatcher.HasTag(s.AirlockTag, allDoors[i].CustomName))
                { smartOuter.Add(allDoors[i]); allDoors.RemoveAt(i); }
                else
                    smartInner.Add(allDoors[i]);
            }

            for (int i = 0; i < smartOuter.Count; i++)
            {
                if (Overbudget(MaxPerTick)) yield return true;
                var outerDoor = smartOuter[i];

                int bestInner = -1;
                float bestDist = float.MaxValue;
                for (int j = 0; j < smartInner.Count; j++)
                {
                    float dist = Vector3I.DistanceManhattan(outerDoor.Position, smartInner[j].Position);
                    if (dist > 0 && dist < bestDist) { bestDist = dist; bestInner = j; }
                }
                if (bestInner < 0) continue;

                int bestVent = -1;
                float bestVentDist = float.MaxValue;
                for (int j = 0; j < allVents.Count; j++)
                {
                    float dist = Vector3I.DistanceManhattan(outerDoor.Position, allVents[j].Position);
                    if (dist < bestVentDist) { bestVentDist = dist; bestVent = j; }
                }
                if (bestVent < 0) continue;

                var inner = smartInner[bestInner];
                var vent = allVents[bestVent];

                var components = new AirlockComponents(_ctx,
                    new List<ExtendedDoor> { ExtendDoor(outerDoor) },
                    new List<ExtendedDoor> { ExtendDoor(inner) },
                    new List<ExtendedAirVent> { new ExtendedAirVent(vent) });
                _controller.AddAdvanced(new AdvancedAirlock(components, "Smart Airlock"));
                _log.AppendLine("\n> Smart airlock " + _controller.AdvancedCount + " added");
                _log.AppendLine("  Outer: " + outerDoor.CustomName);
                _log.AppendLine("  Inner: " + inner.CustomName);
                _log.AppendLine("  Vent:  " + vent.CustomName);

                smartOuter.RemoveAt(i); i--;
                smartInner.RemoveAt(bestInner);
                allVents.RemoveAt(bestVent);
            }

            allDoors.AddRange(smartInner); // unmatched inner → regular pool

            // ---- phase 4: regular auto-close doors --------------------------
            if (s.AutoCloseRegularDoors)
            {
                foreach (var door in allDoors)
                {
                    if (TagMatcher.HasTag(s.ManualTag, door.CustomName)) continue;
                    _controller.AddRegular(ExtendDoor(door));
                }
            }
            _log.AppendLine("\n> " + _controller.RegularCount + " regular auto-close door(s)");

            // ---- phase 5: global status LCDs (tagged with AirlockTag) -------
            foreach (var panel in allPanels)
            {
                if (TagMatcher.HasTag(s.AirlockTag, panel.CustomName))
                    _controller.AddStatusLcd(panel);
            }

            // ---- summary footer ----------------------------------------------
            _log.AppendLine();
            _log.AppendLine(new string('-', 70));
            _log.AppendLine("Smart/group airlocks : " + _controller.AdvancedCount);
            _log.AppendLine("Hangars              : " + _controller.HangarCount);
            _log.AppendLine("Tiny airlocks        : " + _controller.TinyCount);
            _log.AppendLine("Simple groups        : " + _controller.SimpleGroupCount);
            _log.AppendLine("Regular doors        : " + _controller.RegularCount);

            yield return false;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        bool Overbudget(int max) => _runtime.CurrentInstructionCount > max;

        AirlockComponents BuildComponents(
            List<IMyDoor> outer, List<IMyDoor> inner, List<IMyAirVent> vents,
            List<IMyTextPanel> panels, List<IMyLightingBlock> lights,
            List<IMyGasTank> tanks, List<IMyGasGenerator> generators, List<IMyFunctionalBlock> farms,
            List<IMyTimerBlock> innerTimers, List<IMyTimerBlock> outerTimers)
        {
            var comp = new AirlockComponents(_ctx, ExtendDoors(outer), ExtendDoors(inner), ExtendVents(vents));
            if (tanks.Count > 0) comp.Gas = new GasSystem(tanks, generators, farms);
            if (lights.Count > 0) comp.Lights = lights;
            if (panels.Count > 0) comp.Display = new StatusDisplay(panels, "Airlock");
            if (innerTimers.Count > 0) comp.InnerTimers = innerTimers;
            if (outerTimers.Count > 0) comp.OuterTimers = outerTimers;
            return comp;
        }

        List<ExtendedDoor> ExtendDoors(List<IMyDoor> doors)
        {
            var result = new List<ExtendedDoor>(doors.Count);
            foreach (var d in doors) result.Add(ExtendDoor(d));
            return result;
        }

        ExtendedDoor ExtendDoor(IMyDoor door)
        {
            return new ExtendedDoor(_ctx, door, true,
                _ctx.Settings.AutoCloseDelayEntering,
                _ctx.Settings.AutoCloseDelayExiting);
        }

        List<ExtendedAirVent> ExtendVents(List<IMyAirVent> vents)
        {
            var result = new List<ExtendedAirVent>(vents.Count);
            foreach (var v in vents) result.Add(new ExtendedAirVent(v));
            return result;
        }

        void AppendGroupLog(string label,
            List<IMyDoor> outer, List<IMyDoor> inner, List<IMyAirVent> vents,
            List<IMyGasTank> tanks, List<IMyGasGenerator> generators, List<IMyFunctionalBlock> farms,
            List<IMyTextPanel> panels, List<IMyLightingBlock> lights,
            List<IMyTimerBlock> innerTimers, List<IMyTimerBlock> outerTimers)
        {
            _log.AppendLine("\n> " + label + " added");
            _log.AppendLine("  " + outer.Count + " outer: " + string.Join(", ", outer.Select(d => d.CustomName)));
            _log.AppendLine("  " + inner.Count + " inner: " + string.Join(", ", inner.Select(d => d.CustomName)));
            _log.AppendLine("  " + vents.Count + " vent(s): " + string.Join(", ", vents.Select(v => v.CustomName)));
            if (tanks.Count > 0) _log.AppendLine("  " + tanks.Count + " O2 tank(s)");
            if (generators.Count > 0) _log.AppendLine("  " + generators.Count + " generator(s)");
            if (farms.Count > 0) _log.AppendLine("  " + farms.Count + " O2 farm(s)");
            if (panels.Count > 0) _log.AppendLine("  " + panels.Count + " LCD(s)");
            if (lights.Count > 0) _log.AppendLine("  " + lights.Count + " light(s)");
            if (outerTimers.Count > 0) _log.AppendLine("  " + outerTimers.Count + " outer timer(s)");
            if (innerTimers.Count > 0) _log.AppendLine("  " + innerTimers.Count + " inner timer(s)");
        }
    }
}
