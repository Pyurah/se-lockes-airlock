using IngameScript;
using NUnit.Framework;

namespace LockesAirlocks.Tests.Tests
{
    /// <summary>
    /// Tests for the DLC-aware block classification logic. These use the string-only
    /// overloads so no Space Engineers fakes are needed.
    /// </summary>
    [TestFixture]
    public class BlockClassifierTests
    {
        // --- Hydrogen tank detection -----------------------------------------

        [TestCase("", false, TestName = "VanillaLargeO2Tank_NotHydrogen")]
        [TestCase("SmallOxygenTank", false, TestName = "SmallOxygenTank_NotHydrogen")]
        [TestCase("LabOxygenTank", false, TestName = "LabOxygenTank_Fieldwork_NotHydrogen")]
        [TestCase("PrototechOxygenTank", false, TestName = "PrototechOxygenTank_Prosperity_NotHydrogen")]
        [TestCase("LargeHydrogenTank", true, TestName = "LargeHydrogenTank_IsHydrogen")]
        [TestCase("SmallHydrogenTankSmall", true, TestName = "SmallHydrogenTank_IsHydrogen")]
        [TestCase("H2TankLarge", true, TestName = "H2TankLarge_IsHydrogen")]
        [TestCase("h2tank", true, TestName = "LowercaseH2_IsHydrogen")]
        [TestCase("hydrogen_tank", true, TestName = "UnderscoreHydrogen_IsHydrogen")]
        public void IsHydrogenTankSubtype(string subtypeId, bool expected)
        {
            Assert.That(BlockClassifier.IsHydrogenTankSubtype(subtypeId), Is.EqualTo(expected));
        }

        // --- Large opening / gate detection ----------------------------------

        [TestCase("", false, TestName = "RegularDoor_NotLargeOpening")]
        [TestCase("SlidingDoor", false, TestName = "SlidingDoor_NotLargeOpening")]
        [TestCase("SciFiSlidingDoor", false, TestName = "SciFiSlidingDoor_NotLargeOpening")]
        [TestCase("LabSlidingDoor", false, TestName = "LabSlidingDoor_Fieldwork_NotLargeOpening")]
        [TestCase("Gate", true, TestName = "Gate_FrostbitePack_IsLargeOpening")]
        [TestCase("SmallGateTall", true, TestName = "SmallGateTall_ContactPack_IsLargeOpening")]
        [TestCase("SmallGateWide", true, TestName = "SmallGateWide_ContactPack_IsLargeOpening")]
        [TestCase("LargeGate", true, TestName = "LargeGate_IsLargeOpening")]
        [TestCase("gate_outer", true, TestName = "LowercaseGate_IsLargeOpening")]
        public void IsLargeOpeningSubtype(string subtypeId, bool expected)
        {
            Assert.That(BlockClassifier.IsLargeOpeningSubtype(subtypeId), Is.EqualTo(expected));
        }

        // --- Oxygen tank is-oxygen inverse -----------------------------------

        [Test]
        public void NonHydrogenSubtype_CountsAsOxygen()
        {
            // All non-hydrogen subtypes (including "", the vanilla large O2 tank) are oxygen.
            Assert.That(BlockClassifier.IsHydrogenTankSubtype(""), Is.False);
            Assert.That(BlockClassifier.IsHydrogenTankSubtype("SmallOxygenTank"), Is.False);
        }
    }
}
