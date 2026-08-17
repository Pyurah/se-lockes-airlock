using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    /// <summary>
    /// Central hub: owns every registered airlock and door, drives their per-tick
    /// <c>Update()</c> calls, and writes the summary to the global status LCDs.
    /// </summary>
    public class AirlockController
    {
        readonly List<AdvancedAirlock> _advanced = new List<AdvancedAirlock>();
        readonly List<Hangar> _hangars = new List<Hangar>();
        readonly List<TinyAirlock> _tiny = new List<TinyAirlock>();
        readonly List<SimpleGroupAirlock> _simpleGroups = new List<SimpleGroupAirlock>();
        readonly List<ExtendedDoor> _regular = new List<ExtendedDoor>();
        readonly List<IMyTextPanel> _statusLcds = new List<IMyTextPanel>();

        public int AdvancedCount => _advanced.Count;
        public int HangarCount => _hangars.Count;
        public int TinyCount => _tiny.Count;
        public int SimpleGroupCount => _simpleGroups.Count;
        public int RegularCount => _regular.Count;

        public void Reset()
        {
            _advanced.Clear();
            _hangars.Clear();
            _tiny.Clear();
            _simpleGroups.Clear();
            _regular.Clear();
            _statusLcds.Clear();
        }

        public void AddAdvanced(AdvancedAirlock a) => _advanced.Add(a);
        public void AddHangar(Hangar h) => _hangars.Add(h);
        public void AddTiny(TinyAirlock t) => _tiny.Add(t);
        public void AddSimpleGroup(SimpleGroupAirlock s) => _simpleGroups.Add(s);
        public void AddRegular(ExtendedDoor d) => _regular.Add(d);
        public void AddStatusLcd(IMyTextPanel p)
        {
            p.ContentType = ContentType.TEXT_AND_IMAGE;
            _statusLcds.Add(p);
        }

        /// <summary>Per-tick update for all doors and airlocks (called every Update10).</summary>
        public void Update()
        {
            foreach (var a in _advanced) a.Update();
            foreach (var h in _hangars) h.Update();
            foreach (var t in _tiny) t.Update();
            foreach (var s in _simpleGroups) s.Update();
            foreach (var d in _regular) d.Update();
        }

        /// <summary>Write the summary banner to any global status LCDs (called every Update100).</summary>
        public void UpdateStatusLcds(string text)
        {
            foreach (var lcd in _statusLcds)
                lcd.WriteText(text);
        }
    }
}
