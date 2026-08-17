using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// DLC-aware block classification. Space Engineers keeps adding blocks that share the
    /// existing ingame interfaces (Sci-Fi &amp; Lab sliding doors, the Fieldwork Lab O2/H2
    /// Generator, the Prosperity Prototech O2/H2 Generator, the Small/Lab oxygen tanks, the
    /// Contact-pack gates, ...). Interface checks catch most of them automatically; the two
    /// cases that need subtype awareness are gas-tank gas type and large "gate" style doors.
    ///
    /// Keyword lists are centralized here so supporting a future block is a one-line change.
    /// The <c>...Subtype</c> overloads are pure string logic and are covered by unit tests.
    /// </summary>
    public static class BlockClassifier
    {
        // A tank is treated as hydrogen (and therefore excluded from oxygen management) if its
        // subtype contains any of these. Everything else — including the vanilla large O2 tank
        // (empty subtype), the Small Oxygen Tank, and the Fieldwork Lab Oxygen Tank — is oxygen.
        static readonly string[] HydrogenTankKeywords = { "Hydrogen", "H2" };

        // Doors whose subtype contains any of these are treated as large openings (like hangar
        // doors): slow, not auto-closed, and excluded from simple airlocks. Covers the Frostbite
        // Gate and the Contact-pack Small Gate Tall/Wide.
        static readonly string[] LargeOpeningKeywords = { "Gate" };

        /// <summary>True if the subtype id names a hydrogen tank.</summary>
        public static bool IsHydrogenTankSubtype(string subtypeId)
        {
            return ContainsAny(subtypeId, HydrogenTankKeywords);
        }

        /// <summary>
        /// True if the tank should be managed as an oxygen tank. Defaults to oxygen unless the
        /// subtype clearly indicates hydrogen, which correctly includes the newer compact O2 tanks.
        /// </summary>
        public static bool IsOxygenTank(IMyGasTank tank)
        {
            return !IsHydrogenTankSubtype(SubtypeOf(tank));
        }

        /// <summary>True if the subtype id names a large "gate" style opening.</summary>
        public static bool IsLargeOpeningSubtype(string subtypeId)
        {
            return ContainsAny(subtypeId, LargeOpeningKeywords);
        }

        /// <summary>
        /// True if the door behaves like a large opening (airtight hangar door or a gate),
        /// which the airlock logic treats as slow and never auto-closes.
        /// </summary>
        public static bool IsLargeOpening(IMyDoor door)
        {
            if (door is IMyAirtightHangarDoor) return true;
            return IsLargeOpeningSubtype(SubtypeOf(door));
        }

        static string SubtypeOf(IMyTerminalBlock block)
        {
            // BlockDefinition is never null for a real block; guard anyway for fakes/tests.
            return block == null ? "" : block.BlockDefinition.SubtypeId;
        }

        static bool ContainsAny(string text, string[] keywords)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var keyword in keywords)
                if (text.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
