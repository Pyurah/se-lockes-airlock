using IngameScript;
using NUnit.Framework;

namespace LockesAirlocks.Tests.Tests
{
    [TestFixture]
    public class FixedWidthTextTests
    {
        [Test]
        public void AppendLine_SingleShortLine_NoWrap()
        {
            var t = new FixedWidthText(20);
            t.AppendLine("Hello");
            Assert.That(t.GetText(), Is.EqualTo("Hello\n"));
        }

        [Test]
        public void AppendLine_Empty_EmptyLine()
        {
            var t = new FixedWidthText(20);
            t.AppendLine();
            Assert.That(t.GetText(), Is.EqualTo("\n"));
        }

        [Test]
        public void Adjust_ShortText_Unchanged()
        {
            Assert.That(FixedWidthText.Adjust("Hello", 20), Is.EqualTo("Hello"));
        }

        [Test]
        public void Adjust_ExactWidth_Unchanged()
        {
            Assert.That(FixedWidthText.Adjust("1234567890", 10), Is.EqualTo("1234567890"));
        }

        [Test]
        public void Adjust_BreaksOnSpace()
        {
            var result = FixedWidthText.Adjust("Hello World Foo", 10);
            // "Hello " is 6 chars, "World " is 11 chars combined → break before "World"
            Assert.That(result, Does.Contain("\n"));
            Assert.That(result, Does.Contain("Hello"));
            Assert.That(result, Does.Contain("World"));
            Assert.That(result, Does.Contain("Foo"));
        }

        [Test]
        public void Adjust_ZeroWidth_ReturnsText()
        {
            // width <= 0 returns text unchanged (guard)
            Assert.That(FixedWidthText.Adjust("Hello", 0), Is.EqualTo("Hello"));
        }

        [Test]
        public void GetText_MultipleLines_JoinedWithNewlines()
        {
            var t = new FixedWidthText(40);
            t.AppendLine("Line one");
            t.AppendLine("Line two");
            Assert.That(t.GetText(), Is.EqualTo("Line one\nLine two\n"));
        }

        [Test]
        public void Clear_EmptiesBuffer()
        {
            var t = new FixedWidthText(20);
            t.AppendLine("something");
            t.Clear();
            Assert.That(t.GetText(), Is.EqualTo(""));
        }

        [Test]
        public void Append_AddsToCurrentLine()
        {
            var t = new FixedWidthText(40);
            t.AppendLine("Start");
            t.Append(" more");
            Assert.That(t.GetText(), Is.EqualTo("Start more\n"));
        }
    }
}
