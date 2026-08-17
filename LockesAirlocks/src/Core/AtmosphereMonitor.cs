using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Tracks whether the grid is inside a planetary atmosphere by querying the nearest ship
    /// controller's sea-level altitude. Above <see cref="Settings.AtmoDisableAltitude"/> the
    /// mode auto-resets so it never sticks on in orbit.
    ///
    /// If no ship controller is found the mode must be set manually via the 'atmo' command.
    /// </summary>
    public class AtmosphereMonitor
    {
        readonly ScriptContext _ctx;
        IMyShipController _controller;
        public double Altitude { get; private set; }
        public bool AltitudeAccurate { get; private set; }

        public AtmosphereMonitor(ScriptContext ctx) { _ctx = ctx; }

        public void SetController(IMyShipController controller) { _controller = controller; }

        public void Update()
        {
            _ctx.InAtmoChanged = false;

            if (_controller != null)
            {
                double alt;
                AltitudeAccurate = _controller.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out alt);
                Altitude = alt;

                if (_ctx.InAtmo && AltitudeAccurate && Altitude > _ctx.Settings.AtmoDisableAltitude)
                    SetAtmo(false);
            }
        }

        public void SetAtmo(bool value)
        {
            if (value && AltitudeAccurate && Altitude > _ctx.Settings.AtmoDisableAltitude) return;
            if (_ctx.InAtmo == value) return;
            _ctx.InAtmo = value;
            _ctx.InAtmoChanged = true;
        }
    }
}
