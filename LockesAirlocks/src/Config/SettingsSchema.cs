using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    /// <summary>
    /// Declarative serialization of <see cref="Settings"/> to and from the programmable
    /// block's Custom Data. Each <see cref="Descriptor"/> maps a human-readable label to a
    /// read (serialize) and write (parse) delegate, so adding a setting is a single entry.
    /// The parse/format helpers are pure and unit-tested.
    /// </summary>
    public static class SettingsSchema
    {
        public sealed class Descriptor
        {
            public readonly string Label;
            public readonly int BlankLinesAbove;
            public readonly Func<Settings, string> Read;
            public readonly Action<Settings, string> Write;

            public Descriptor(string label, Func<Settings, string> read, Action<Settings, string> write, int blankLinesAbove = 0)
            {
                Label = label;
                Read = read;
                Write = write;
                BlankLinesAbove = blankLinesAbove;
            }
        }

        // Order here is the order rendered into Custom Data.
        public static readonly Descriptor[] All =
        {
            new Descriptor("Airlock tag", s => s.AirlockTag, (s, v) => s.AirlockTag = NonEmpty(v, s.AirlockTag)),
            new Descriptor("Hangar tag", s => s.HangarTag, (s, v) => s.HangarTag = NonEmpty(v, s.HangarTag)),
            new Descriptor("Ignore tag", s => s.IgnoreTag, (s, v) => s.IgnoreTag = NonEmpty(v, s.IgnoreTag)),
            new Descriptor("Manual (no auto-close) tag", s => s.ManualTag, (s, v) => s.ManualTag = NonEmpty(v, s.ManualTag)),

            new Descriptor("Auto close delay entering (s)", s => Num(s.AutoCloseDelayEntering), (s, v) => s.AutoCloseDelayEntering = ParseFloat(v, s.AutoCloseDelayEntering), 1),
            new Descriptor("Auto close delay exiting (s)", s => Num(s.AutoCloseDelayExiting), (s, v) => s.AutoCloseDelayExiting = ParseFloat(v, s.AutoCloseDelayExiting)),
            new Descriptor("Auto close regular doors", s => Bool(s.AutoCloseRegularDoors), (s, v) => s.AutoCloseRegularDoors = ParseBool(v, s.AutoCloseRegularDoors)),

            new Descriptor("Airlock free light color", s => ColorText(s.IdleLightColor), (s, v) => s.IdleLightColor = ParseColor(v, s.IdleLightColor), 1),
            new Descriptor("Airlock in use light color", s => ColorText(s.BusyLightColor), (s, v) => s.BusyLightColor = ParseColor(v, s.BusyLightColor)),

            new Descriptor("Auto disable atmo mode above (m)", s => Num(s.AtmoDisableAltitude), (s, v) => s.AtmoDisableAltitude = ParseDouble(v, s.AtmoDisableAltitude), 1),

            new Descriptor("[Advanced] Timeout (s)", s => Num(s.TimeoutSeconds), (s, v) => s.TimeoutSeconds = ParseFloat(v, s.TimeoutSeconds), 1),
            new Descriptor("[Advanced] Timeout oxygen delta (%)", s => Num(s.OxygenDifferencePercent), (s, v) => s.OxygenDifferencePercent = ParseFloat(v, s.OxygenDifferencePercent)),
        };

        /// <summary>Parse settings out of raw Custom Data text, mutating <paramref name="settings"/> in place.</summary>
        public static void Parse(string data, Settings settings)
        {
            if (string.IsNullOrEmpty(data)) return;
            var lines = data.Split('\n');
            foreach (var line in lines)
            {
                foreach (var descriptor in All)
                {
                    if (!line.StartsWith(descriptor.Label)) continue;
                    var parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2 && !string.IsNullOrEmpty(parts[1]))
                        descriptor.Write(settings, parts[1].Trim());
                    break;
                }
            }
        }

        /// <summary>Render the settings block into the supplied text buffer.</summary>
        public static void Generate(Settings settings, FixedWidthText text)
        {
            foreach (var descriptor in All)
            {
                for (var i = 0; i < descriptor.BlankLinesAbove; i++) text.AppendLine();
                text.AppendLine(descriptor.Label + ": " + descriptor.Read(settings));
            }
        }

        // --- value formatting -------------------------------------------------
        static string Bool(bool value) => value ? "yes" : "no";
        static string Num(float value) => value.ToString();
        static string Num(double value) => value.ToString();
        static string ColorText(Color c) => "R:" + c.R + ", G:" + c.G + ", B:" + c.B;

        // --- value parsing ----------------------------------------------------
        static string NonEmpty(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

        static bool ParseBool(string value, bool fallback)
        {
            var v = value.Trim().ToLower();
            if (v == "yes" || v == "true") return true;
            if (v == "no" || v == "false") return false;
            return fallback;
        }

        static float ParseFloat(string value, float fallback)
        {
            float result;
            return float.TryParse(value.Trim(), out result) ? result : fallback;
        }

        static double ParseDouble(string value, double fallback)
        {
            double result;
            return double.TryParse(value.Trim(), out result) ? result : fallback;
        }

        /// <summary>Parses "R:255, G:0, B:128" style color text. Falls back to the current value on any error.</summary>
        static Color ParseColor(string value, Color fallback)
        {
            var v = value.ToLower();
            if (!v.Contains("r:") || !v.Contains("g:") || !v.Contains("b:")) return fallback;
            var split = v.Split(',');
            if (split.Length != 3) return fallback;

            var channels = new int[3];
            for (var i = 0; i < 3; i++)
            {
                var digits = ExtractDigits(split[i]);
                int parsed;
                if (!int.TryParse(digits, out parsed)) return fallback;
                channels[i] = parsed < 0 ? 0 : (parsed > 255 ? 255 : parsed);
            }
            return new Color(channels[0], channels[1], channels[2]);
        }

        static string ExtractDigits(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s)
                if (c >= '0' && c <= '9') sb.Append(c);
            return sb.ToString();
        }
    }
}
