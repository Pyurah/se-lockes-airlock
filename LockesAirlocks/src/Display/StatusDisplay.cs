using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    /// <summary>
    /// Drives one or more LCD text panels for an airlock, displaying the airlock name and
    /// current state string. Auto-detects corner LCD vs. normal LCD and adjusts font size.
    /// The optional name comes from the public title of any panel in the group.
    /// </summary>
    public class StatusDisplay
    {
        readonly IMyTextPanel[] _panels;
        public string AirlockName = "";
        readonly string _airlockType;

        public StatusDisplay(IMyTextPanel panel, string airlockType)
        {
            _panels = new[] { panel };
            _airlockType = airlockType;
            Initialize();
        }

        public StatusDisplay(List<IMyTextPanel> panels, string airlockType)
        {
            _panels = panels.ToArray();
            _airlockType = airlockType;
            Initialize();
        }

        void Initialize()
        {
            foreach (var panel in _panels)
            {
                var title = panel.GetPublicTitle();
                if (!string.IsNullOrEmpty(title)) AirlockName = title;

                panel.ContentType = ContentType.TEXT_AND_IMAGE;

                if (panel.BlockDefinition.SubtypeId.Contains("Corner"))
                {
                    if (panel.FontSize == 1f)
                        panel.FontSize = panel.BlockDefinition.SubtypeId.Contains("Flat") ? 1.4f : 1.3f;
                }
            }
        }

        public void Update(string state, bool error = false)
        {
            var header = error ? " <<< Error >>>\n" : "";
            var name = AirlockName.Length > 0 ? AirlockName : _airlockType;
            var text = header + " " + name + " \n " + state + " ";

            foreach (var panel in _panels)
            {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.WriteText(text);
            }
        }
    }
}
