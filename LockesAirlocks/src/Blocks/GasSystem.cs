using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Encapsulates all oxygen-supply decisions for a group airlock or hangar:
    /// monitors tank fill ratios, enables/disables O2H2 generators and oxygen farms,
    /// and signals when tanks are too full to depressurize or empty enough to air-scoop.
    ///
    /// An airlock without gas blocks (simple group / tiny / smart without tanks) simply
    /// skips this entirely; the class is a null-safe default.
    /// </summary>
    public class GasSystem
    {
        readonly List<IMyGasTank> _tanks;
        readonly List<IMyGasGenerator> _generators;
        readonly List<IMyFunctionalBlock> _farms;

        bool _generatorsEnabled;

        /// <summary>True when tanks are ≥95 % full — depressurizing would have nowhere to push gas.</summary>
        public bool TanksFullSkipDepressurize { get; private set; }

        /// <summary>True when tanks are ≤65 % full — worth opening vents inward to scoop atmosphere.</summary>
        public bool AttemptAirScoop { get; private set; }

        public bool HasTanks => _tanks != null && _tanks.Count > 0;

        public GasSystem(List<IMyGasTank> tanks = null, List<IMyGasGenerator> generators = null, List<IMyFunctionalBlock> farms = null)
        {
            _tanks = tanks;
            _generators = generators;
            _farms = farms;
        }

        public void Update()
        {
            if (!HasTanks) return;

            var total = 0.0;
            foreach (var tank in _tanks) total += tank.FilledRatio;
            var avg = total / _tanks.Count;

            TanksFullSkipDepressurize = avg > 0.95;
            AttemptAirScoop = avg < 0.65;

            // Auto-manage generators: on when tanks < 30 %, off when tanks > 70 %.
            if (_generatorsEnabled && avg > 0.70) SetGenerators(false);
            else if (!_generatorsEnabled && avg < 0.30) SetGenerators(true);
        }

        /// <summary>Force-disables generators (called on init to stop H2 production interfering with O2 management).</summary>
        public void DisableGenerators() => SetGenerators(false);

        void SetGenerators(bool enabled)
        {
            _generatorsEnabled = enabled;
            if (_generators != null)
                foreach (var g in _generators) g.Enabled = enabled;
            if (_farms != null)
                foreach (var f in _farms) f.Enabled = enabled;
        }
    }
}
