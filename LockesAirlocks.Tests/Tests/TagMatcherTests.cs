using IngameScript;
using NUnit.Framework;

namespace LockesAirlocks.Tests.Tests
{
    [TestFixture]
    public class TagMatcherTests
    {
        [TestCase("#AL", "Outer Door #AL", true)]
        [TestCase("#AL", "#AL Outer Door", true)]
        [TestCase("#AL", "Door #AL Inner", true)]
        [TestCase("#AL", "#AL", true)]
        [TestCase("#AL", "#ALX", false)]
        [TestCase("#AL", "Door #ALICE", false)]
        [TestCase("#AL", "door#AL", false)]       // no whitespace before
        [TestCase("#AL", "", false)]
        [TestCase("#Hangar", "Outer Door #Hangar", true)]
        [TestCase("#Hangar", "#Hangar Bay 1", true)]
        [TestCase("#Hangar", "#HangarX", false)]
        [TestCase("#Ignore", "Air Vent #Ignore", true)]
        [TestCase("#Manual", "Door #Manual", true)]
        [TestCase("#AL", "outer door #al", true)]   // case-insensitive
        [TestCase("#AL", "outer door #AL trailing", true)]
        public void HasTag_VariousCases(string tag, string name, bool expected)
        {
            Assert.That(TagMatcher.HasTag(tag, name), Is.EqualTo(expected));
        }

        [Test]
        public void HasTag_NullTag_ReturnsFalse()
        {
            Assert.That(TagMatcher.HasTag(null, "Door #AL"), Is.False);
        }

        [Test]
        public void HasTag_NullName_ReturnsFalse()
        {
            Assert.That(TagMatcher.HasTag("#AL", null), Is.False);
        }

        [Test]
        public void HasAnyTag_MatchesFirstTag()
        {
            Assert.That(TagMatcher.HasAnyTag("Door #AL Beta", "#AL", "#Hangar"), Is.True);
        }

        [Test]
        public void HasAnyTag_MatchesSecondTag()
        {
            Assert.That(TagMatcher.HasAnyTag("Door #Hangar Bay", "#AL", "#Hangar"), Is.True);
        }

        [Test]
        public void HasAnyTag_MatchesNone_ReturnsFalse()
        {
            Assert.That(TagMatcher.HasAnyTag("Plain Door", "#AL", "#Hangar"), Is.False);
        }
    }
}
