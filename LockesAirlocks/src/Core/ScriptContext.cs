using System;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Shared, per-tick runtime state passed to every airlock and block wrapper.
    /// Decouples the airlock logic from the <see cref="Program"/> host so the state
    /// machines can be exercised in unit tests with a plain fake context.
    /// </summary>
    public class ScriptContext
    {
        /// <summary>Accumulated in-game time since the script started (monotonic).</summary>
        public TimeSpan Time { get; set; }

        /// <summary>True when the grid is inside a breathable atmosphere and depressurization should be skipped.</summary>
        public bool InAtmo { get; set; }

        /// <summary>True for the single tick on which <see cref="InAtmo"/> changed value.</summary>
        public bool InAtmoChanged { get; set; }

        /// <summary>User-configurable settings, parsed from the programmable block's Custom Data.</summary>
        public Settings Settings { get; private set; }

        /// <summary>The programmable block running the script (used for diagnostic Custom Data output).</summary>
        public IMyProgrammableBlock Me { get; private set; }

        public ScriptContext(Settings settings, IMyProgrammableBlock me)
        {
            Settings = settings;
            Me = me;
        }
    }
}
