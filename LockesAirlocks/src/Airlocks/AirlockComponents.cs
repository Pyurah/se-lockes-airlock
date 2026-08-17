using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    /// <summary>
    /// All physical blocks and subsystems wired to a single group/smart airlock or hangar.
    /// Constructed by <see cref="BlockDiscovery"/> and passed to the relevant airlock class.
    /// </summary>
    public class AirlockComponents
    {
        public readonly ScriptContext Context;

        public List<ExtendedDoor> OuterDoors;
        public List<ExtendedDoor> InnerDoors;
        public List<ExtendedAirVent> Vents;

        public GasSystem Gas;
        public StatusDisplay Display;
        public List<IMyLightingBlock> Lights;
        public List<IMyTimerBlock> InnerTimers;
        public List<IMyTimerBlock> OuterTimers;

        /// <summary>Per-vent timeout in ticks — how long to wait for vent progress before retrying.</summary>
        public TimeSpan VentTimeout => TimeSpan.FromSeconds(Context.Settings.TimeoutSeconds);

        public Color IdleColor => Context.Settings.IdleLightColor;
        public Color BusyColor => Context.Settings.BusyLightColor;

        public AirlockComponents(ScriptContext context,
            List<ExtendedDoor> outerDoors, List<ExtendedDoor> innerDoors,
            List<ExtendedAirVent> vents)
        {
            Context = context;
            OuterDoors = outerDoors;
            InnerDoors = innerDoors;
            Vents = vents;
            Gas = new GasSystem();
        }

        // --- helpers called by airlock state machines -------------------------

        public void SetLightsIdle()
        {
            if (Lights == null) return;
            foreach (var light in Lights)
            {
                light.Color = IdleColor;
                light.BlinkIntervalSeconds = 0;
            }
        }

        public void SetLightsBusy()
        {
            if (Lights == null) return;
            foreach (var light in Lights)
            {
                light.Color = BusyColor;
                light.BlinkIntervalSeconds = 1.2f;
                light.BlinkLength = 40f;
            }
        }

        public void TriggerInnerTimers()
        {
            if (InnerTimers == null) return;
            foreach (var t in InnerTimers) t.Trigger();
        }

        public void TriggerOuterTimers()
        {
            if (OuterTimers == null) return;
            foreach (var t in OuterTimers) t.Trigger();
        }
    }
}
