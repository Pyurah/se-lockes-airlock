using IngameScript;
using NUnit.Framework;

namespace LockesAirlocks.Tests.Tests
{
    [TestFixture]
    public class SettingsSchemaTests
    {
        [Test]
        public void RoundTrip_DefaultsPreserved()
        {
            var settings = new Settings();
            var text = new FixedWidthText(70);
            SettingsSchema.Generate(settings, text);

            var restored = new Settings();
            SettingsSchema.Parse(text.GetText(), restored);

            Assert.That(restored.AirlockTag, Is.EqualTo(settings.AirlockTag));
            Assert.That(restored.HangarTag, Is.EqualTo(settings.HangarTag));
            Assert.That(restored.IgnoreTag, Is.EqualTo(settings.IgnoreTag));
            Assert.That(restored.ManualTag, Is.EqualTo(settings.ManualTag));
            Assert.That(restored.AutoCloseDelayEntering, Is.EqualTo(settings.AutoCloseDelayEntering));
            Assert.That(restored.AutoCloseDelayExiting, Is.EqualTo(settings.AutoCloseDelayExiting));
            Assert.That(restored.AutoCloseRegularDoors, Is.EqualTo(settings.AutoCloseRegularDoors));
            Assert.That(restored.TimeoutSeconds, Is.EqualTo(settings.TimeoutSeconds));
            Assert.That(restored.OxygenDifferencePercent, Is.EqualTo(settings.OxygenDifferencePercent));
            Assert.That(restored.AtmoDisableAltitude, Is.EqualTo(settings.AtmoDisableAltitude));
        }

        [Test]
        public void Parse_CustomValues_Applied()
        {
            var settings = new Settings();
            var customData = "Airlock tag: #LOCK\nAuto close delay exiting (s): 5.5\nAuto close regular doors: no\n[Advanced] Timeout (s): 3\n";

            SettingsSchema.Parse(customData, settings);

            Assert.That(settings.AirlockTag, Is.EqualTo("#LOCK"));
            Assert.That(settings.AutoCloseDelayExiting, Is.EqualTo(5.5f).Within(0.001f));
            Assert.That(settings.AutoCloseRegularDoors, Is.False);
            Assert.That(settings.TimeoutSeconds, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Parse_EmptyData_LeavesDefaultsUnchanged()
        {
            var settings = new Settings();
            var original = settings.AirlockTag;
            SettingsSchema.Parse("", settings);
            Assert.That(settings.AirlockTag, Is.EqualTo(original));
        }

        [Test]
        public void Parse_MalformedLines_IgnoredGracefully()
        {
            var settings = new Settings();
            var original = settings.AutoCloseDelayExiting;
            SettingsSchema.Parse("Auto close delay exiting (s): not-a-number\n", settings);
            Assert.That(settings.AutoCloseDelayExiting, Is.EqualTo(original));
        }

        [Test]
        public void Parse_EmptyTagValue_LeavesTagUnchanged()
        {
            var settings = new Settings();
            var original = settings.AirlockTag;
            SettingsSchema.Parse("Airlock tag: \n", settings);
            Assert.That(settings.AirlockTag, Is.EqualTo(original));
        }

        [Test]
        public void OxygenDifferenceRatio_Clamped()
        {
            var settings = new Settings { OxygenDifferencePercent = 200f };
            Assert.That(settings.OxygenDifferenceRatio, Is.EqualTo(1f));

            settings.OxygenDifferencePercent = -10f;
            Assert.That(settings.OxygenDifferenceRatio, Is.EqualTo(0f));

            settings.OxygenDifferencePercent = 20f;
            Assert.That(settings.OxygenDifferenceRatio, Is.EqualTo(0.2f).Within(0.001f));
        }

        [Test]
        public void Parse_BoolYesNo_RoundTrips()
        {
            var on = new Settings { AutoCloseRegularDoors = true };
            var text = new FixedWidthText(70);
            SettingsSchema.Generate(on, text);

            var restored = new Settings();
            SettingsSchema.Parse(text.GetText(), restored);
            Assert.That(restored.AutoCloseRegularDoors, Is.True);

            var off = new Settings { AutoCloseRegularDoors = false };
            var text2 = new FixedWidthText(70);
            SettingsSchema.Generate(off, text2);
            var restored2 = new Settings();
            SettingsSchema.Parse(text2.GetText(), restored2);
            Assert.That(restored2.AutoCloseRegularDoors, Is.False);
        }
    }
}
