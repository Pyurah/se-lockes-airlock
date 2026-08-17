using System.Collections.Generic;

namespace IngameScript
{
    /// <summary>
    /// A simple line buffer that word-wraps text to a fixed character width. Used to build
    /// the setup log and LCD output without words being split across lines mid-word.
    /// </summary>
    public class FixedWidthText
    {
        readonly List<string> _lines = new List<string>();

        public int Width { get; private set; }

        public FixedWidthText(int width)
        {
            Width = width;
        }

        public void Clear() => _lines.Clear();

        /// <summary>Appends text to the current (last) line, starting a line first if the buffer is empty.</summary>
        public void Append(string text)
        {
            if (_lines.Count == 0) _lines.Add("");
            _lines[_lines.Count - 1] += text;
        }

        public void AppendLine() => _lines.Add("");

        public void AppendLine(string text) => _lines.Add(text);

        public string GetText() => GetText(Width);

        /// <summary>Renders all buffered lines, wrapping each to <paramref name="lineWidth"/> on spaces where possible.</summary>
        public string GetText(int lineWidth)
        {
            var result = "";
            foreach (var line in _lines)
                result += Adjust(line, lineWidth) + "\n";
            return result;
        }

        /// <summary>
        /// Wraps a single string to <paramref name="width"/> characters, breaking on the last
        /// space within each chunk when possible, otherwise hard-breaking at the width.
        /// </summary>
        public static string Adjust(string text, int width)
        {
            if (width <= 0 || text.Length <= width) return text;

            var rest = text;
            var output = "";
            while (rest.Length > width)
            {
                var part = rest.Substring(0, width);
                rest = rest.Substring(width);

                var broke = false;
                for (var i = part.Length - 1; i > 0; i--)
                {
                    if (part[i] != ' ') continue;
                    output += part.Substring(0, i) + "\n";
                    rest = part.Substring(i + 1) + rest;
                    broke = true;
                    break;
                }
                if (!broke) output += part + "\n";
            }
            output += rest;
            return output;
        }
    }
}
