using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Group airlock with no O2 management — prevents tagged outer doors and untagged inner
    /// doors from being open simultaneously. When all doors in the group are tagged, the
    /// airlock enters "solo mode" where only a single door may be open at a time.
    ///
    /// Use this when you want access control without air vents.
    /// </summary>
    public class SimpleGroupAirlock
    {
        readonly ExtendedDoor[] _outer;
        readonly ExtendedDoor[] _inner;
        readonly bool _soloMode;
        int _outerOpen;
        int _innerOpen;

        public SimpleGroupAirlock(ExtendedDoor[] outerDoors, ExtendedDoor[] innerDoors)
        {
            _outer = outerDoors;
            _inner = innerDoors;
            _soloMode = _inner.Length == 0;

            if (_soloMode)
            {
                foreach (var d in _outer) { d.SubscribeFunc(OnOuterSolo); d.Door.Enabled = true; }
            }
            else
            {
                foreach (var d in _outer) { d.SubscribeFunc(OnOuter); d.Door.Enabled = true; }
                foreach (var d in _inner) { d.SubscribeFunc(OnInner); d.Door.Enabled = true; }
            }
        }

        public void Update()
        {
            foreach (var d in _outer) d.Update();
            foreach (var d in _inner) d.Update();

            if (!_soloMode)
            {
                // Re-enable opposite side as soon as this side fully closes.
                if (_outerOpen == 0) SetEnabled(_inner, true);
                if (_innerOpen == 0) SetEnabled(_outer, true);
            }
        }

        void OnOuter(ExtendedDoor door)
        {
            if (door.Door.Status == DoorStatus.Opening) { _outerOpen++; SetEnabled(_inner, false); }
            else if (door.Door.Status == DoorStatus.Closed) _outerOpen--;
        }

        void OnInner(ExtendedDoor door)
        {
            if (door.Door.Status == DoorStatus.Opening) { _innerOpen++; SetEnabled(_outer, false); }
            else if (door.Door.Status == DoorStatus.Closed) _innerOpen--;
        }

        void OnOuterSolo(ExtendedDoor door)
        {
            if (door.Door.Status == DoorStatus.Opening)
            {
                foreach (var d in _outer)
                    if (!ReferenceEquals(d, door)) d.Door.Enabled = false;
            }
            else if (door.Door.Status == DoorStatus.Closed)
            {
                SetEnabled(_outer, true);
            }
        }

        static void SetEnabled(ExtendedDoor[] doors, bool enabled)
        {
            foreach (var d in doors) d.Door.Enabled = enabled;
        }
    }
}
