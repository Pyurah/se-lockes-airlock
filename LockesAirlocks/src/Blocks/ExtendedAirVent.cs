using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Wraps an <see cref="IMyAirVent"/> and raises change notifications when its
    /// Depressurize flag is toggled (e.g. by a player pressing a hangar button), which is how
    /// hangars are triggered.
    /// </summary>
    public class ExtendedAirVent
    {
        readonly List<Action> _actions = new List<Action>();
        readonly List<Action<ExtendedAirVent>> _funcs = new List<Action<ExtendedAirVent>>();

        readonly IMyAirVent _vent;
        bool _lastDepressurize;

        public bool ChangedThisUpdate { get; private set; }

        public ExtendedAirVent(IMyAirVent vent)
        {
            _vent = vent;
            _lastDepressurize = vent.Depressurize;
        }

        public bool Depressurize
        {
            get { return _vent.Depressurize; }
            set { _lastDepressurize = value; _vent.Depressurize = value; }
        }

        public bool Enabled
        {
            get { return _vent.Enabled; }
            set { _vent.Enabled = value; }
        }

        public bool CanPressurize => _vent.CanPressurize;
        public string CustomName => _vent.CustomName;

        public float GetOxygenLevel() => _vent.GetOxygenLevel();

        public void Update()
        {
            ChangedThisUpdate = false;
            if (_vent.Depressurize != _lastDepressurize)
            {
                ChangedThisUpdate = true;
                foreach (var action in _actions) action();
                foreach (var func in _funcs) func(this);
            }
            _lastDepressurize = _vent.Depressurize;
        }

        public void Subscribe(Action action) => _actions.Add(action);
        public void SubscribeFunc(Action<ExtendedAirVent> func) => _funcs.Add(func);
    }
}
