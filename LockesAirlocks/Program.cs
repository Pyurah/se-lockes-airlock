/*
 * Locke's Airlocks
 * ================
 * A feature-complete airlock management script for Space Engineers.
 *
 * Based on Blargmode's Aggressive Airlocks v6.4 (2020-08-27).
 * See CREDITS.md and README.md for full attribution.
 *
 * Supports: regular doors, tiny airlocks, smart airlocks, group airlocks,
 * hangars, and simple group airlocks. DLC-aware (Contact, Fieldwork,
 * Prosperity and all prior packs).
 *
 * SETUP
 * -----
 * Press Ok / Run once to initialize. Send the command "update" after any
 * block changes. See Custom Data for all settings and the setup log.
 *
 * TAGS (add to block Custom Name)
 * --------------------------------
 *   #AL       Marks the outer door(s) of a smart or group airlock.
 *   #Hangar   Marks the outer door(s) of a hangar.
 *   #Ignore   Excludes a block from all airlock management.
 *   #Manual   Disables auto-close for a door.
 *
 * COMMANDS (enter as the Run argument)
 * --------------------------------------
 *   update    Re-scan all blocks (required after building changes).
 *   atmo      Toggle atmosphere mode on/off.
 *   atmo on   Force atmosphere mode on.
 *   atmo off  Force atmosphere mode off.
 */

using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        const string ScriptName = "Locke's Airlocks";

        TimeSpan _time;
        ulong _tick;

        Settings _settings;
        ScriptContext _ctx;
        AtmosphereMonitor _atmo;
        AirlockController _controller;
        BlockDiscovery _discovery;
        ExecutionProfiler _profiler;

        bool _initialized;
        int _initDots;
        string _setupLog = "";
        readonly List<TimedMessage> _messages = new List<TimedMessage>();

        string[] _dots = { ".", "..", "..." };
        int _dotIdx;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;

            _settings = new Settings();
            SettingsSchema.Parse(Me.CustomData, _settings);

            _ctx = new ScriptContext(_settings, Me);
            _atmo = new AtmosphereMonitor(_ctx);
            _controller = new AirlockController();
            _profiler = new ExecutionProfiler();

            _discovery = new BlockDiscovery(_ctx, GridTerminalSystem, Me, Runtime, _controller, _atmo);
            _discovery.Start();
        }

        public void Save() { }

        public void Main(string argument, UpdateType updateType)
        {
            _time += Runtime.TimeSinceLastRun;
            _tick++;
            _ctx.Time = _time;

            // --- init phase --------------------------------------------------
            if (!_initialized)
            {
                _initDots = (_initDots + 1) % 4;
                Echo("Initializing" + new string('.', _initDots));
                var log = _discovery.Step();
                if (log != null)
                {
                    _setupLog = log;
                    WriteCustomData();
                    _initialized = true;
                }
                _profiler.Sample(Runtime);
                return;
            }

            // --- command handling --------------------------------------------
            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0 && !string.IsNullOrEmpty(argument))
                HandleCommand(argument.Trim().ToLower());

            // --- Update10 ---------------------------------------------------
            if ((updateType & UpdateType.Update10) != 0)
            {
                _atmo.Update();
                _controller.Update();

                if (_tick % 5 == 0) PrintInfo();
                _ctx.InAtmoChanged = false;
            }

            // --- Update100 --------------------------------------------------
            if ((updateType & UpdateType.Update100) != 0)
                _controller.UpdateStatusLcds(BuildStatusText());

            _profiler.Sample(Runtime);
        }

        // --- command routing -------------------------------------------------

        void HandleCommand(string cmd)
        {
            switch (cmd)
            {
                case "update":
                    _initialized = false;
                    _initDots = 0;
                    _settings = new Settings();
                    SettingsSchema.Parse(Me.CustomData, _settings);
                    _ctx = new ScriptContext(_settings, Me);
                    _atmo = new AtmosphereMonitor(_ctx);
                    _controller = new AirlockController();
                    _discovery = new BlockDiscovery(_ctx, GridTerminalSystem, Me, Runtime, _controller, _atmo);
                    _discovery.Start();
                    break;
                case "atmo":
                    _atmo.SetAtmo(!_ctx.InAtmo);
                    break;
                case "atmo on":
                    _atmo.SetAtmo(true);
                    break;
                case "atmo off":
                    _atmo.SetAtmo(false);
                    break;
                default:
                    var msg = "'" + cmd + "' not recognized. Commands: update, atmo, atmo on, atmo off.";
                    _messages.Add(new TimedMessage(_time + TimeSpan.FromSeconds(7), msg));
                    Me.CustomData += "\n> " + msg;
                    break;
            }
        }

        // --- output ----------------------------------------------------------

        void PrintInfo()
        {
            var sb = new StringBuilder();
            sb.Append(ScriptName);
            sb.Append(NextDots());
            sb.Append("\nLoad avg: ");
            sb.Append(((int)((_profiler.AverageInstructions / Runtime.MaxInstructionCount) * 100)));
            sb.Append("% / ");
            sb.Append(_profiler.AverageRuntimeMs.ToString("n2"));
            sb.Append("ms");

            if (_ctx.InAtmo) sb.Append("\n[Atmosphere mode]");

            PurgeMessages();
            foreach (var m in _messages) { sb.Append("\n\n"); sb.Append(m.Message); }

            sb.Append("\n\n");
            sb.Append(_setupLog.Length > 0 ? "Setup complete. See Custom Data." : "Initializing...");
            Echo(sb.ToString());
        }

        string BuildStatusText()
        {
            var sb = new StringBuilder();
            sb.Append(ScriptName);
            sb.Append("\nLoad avg: ");
            sb.Append(((int)((_profiler.AverageInstructions / Runtime.MaxInstructionCount) * 100)));
            sb.Append("%");
            if (_ctx.InAtmo) sb.Append("\n[Atmosphere mode]");
            PurgeMessages();
            foreach (var m in _messages) { sb.Append("\n\n"); sb.Append(m.Message); }
            return sb.ToString();
        }

        void WriteCustomData()
        {
            var text = new FixedWidthText(70);
            text.AppendLine(ScriptName + " - Settings");
            text.AppendLine(new string('-', 70));
            text.AppendLine("To change a setting: edit the value after the colon, then run 'update'.");
            text.AppendLine();
            SettingsSchema.Generate(_settings, text);
            text.AppendLine();
            text.AppendLine();
            text.AppendLine(_setupLog);
            Me.CustomData = text.GetText();
        }

        void PurgeMessages()
        {
            for (int i = _messages.Count - 1; i >= 0; i--)
                if (_time > _messages[i].Expiration) _messages.RemoveAt(i);
        }

        string NextDots()
        {
            _dotIdx = (_dotIdx + 1) % _dots.Length;
            return _dots[_dotIdx];
        }
    }
}
