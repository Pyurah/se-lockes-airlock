using VRageMath;

namespace IngameScript
{
    /// <summary>
    /// Strongly-typed, user-configurable settings for Locke's Airlocks.
    /// Values are round-tripped through the programmable block's Custom Data by
    /// <see cref="SettingsSchema"/>. Defaults preserve drop-in compatibility with the
    /// original Aggressive Airlocks tags.
    /// </summary>
    public class Settings
    {
        // --- Tags -------------------------------------------------------------
        public string AirlockTag = "#AL";
        public string HangarTag = "#Hangar";
        public string IgnoreTag = "#Ignore";
        public string ManualTag = "#Manual";

        // --- Auto-close timing (seconds) -------------------------------------
        public float AutoCloseDelayEntering = 0.5f;
        public float AutoCloseDelayExiting = 2.0f;
        public bool AutoCloseRegularDoors = true;

        // --- Advanced ---------------------------------------------------------
        /// <summary>Seconds to wait for a vent to make progress before a (de)pressurize step is retried/aborted.</summary>
        public float TimeoutSeconds = 2f;

        /// <summary>Minimum oxygen delta (percent) that counts as "progress" within the timeout window.</summary>
        public float OxygenDifferencePercent = 20f;

        // --- Lights -----------------------------------------------------------
        public Color IdleLightColor = Color.Green;
        public Color BusyLightColor = Color.Violet;

        // --- Atmosphere -------------------------------------------------------
        /// <summary>Above this altitude (m) atmosphere mode auto-disables so it doesn't stick on in orbit.</summary>
        public double AtmoDisableAltitude = 5000;

        /// <summary>Oxygen difference expressed as a 0..1 ratio, clamped from <see cref="OxygenDifferencePercent"/>.</summary>
        public float OxygenDifferenceRatio
        {
            get
            {
                var ratio = OxygenDifferencePercent / 100f;
                if (ratio < 0f) return 0f;
                if (ratio > 1f) return 1f;
                return ratio;
            }
        }
    }
}
