using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// The simplest airlock: two doors placed face-to-face. The script prevents both from
    /// being open simultaneously. When the player opens one, the other is disabled until it
    /// closes, then the other side is auto-opened and the first side disabled in turn.
    /// </summary>
    public class TinyAirlock
    {
        readonly ExtendedDoor _doorA;
        readonly ExtendedDoor _doorB;
        bool _openRequest;

        public TinyAirlock(ExtendedDoor doorA, ExtendedDoor doorB)
        {
            _doorA = doorA;
            _doorB = doorB;
            _doorA.Subscribe(OnDoorA);
            _doorB.Subscribe(OnDoorB);
        }

        public void Update()
        {
            _doorA.Update();
            _doorB.Update();
        }

        void OnDoorA() => HandleDoor(_doorA, _doorB);
        void OnDoorB() => HandleDoor(_doorB, _doorA);

        void HandleDoor(ExtendedDoor me, ExtendedDoor other)
        {
            if (me.Status == DoorStatus.Opening && other.Status == DoorStatus.Opening)
            {
                // Both opened at once (e.g. triggered together) — close both.
                me.Door.CloseDoor();
                other.Door.CloseDoor();
            }
            else if (me.Status == DoorStatus.Opening && !me.ProgramOpening)
            {
                // Player opened this side; disable the other until this one closes.
                if (!other.IsManualDoor) _openRequest = true;
                other.Door.Enabled = false;
            }
            else if (me.Status == DoorStatus.Closed && !other.Door.Enabled && _openRequest)
            {
                // This side closed; now let the other side open and lock this side.
                _openRequest = false;
                other.Door.Enabled = true;
                me.Door.Enabled = false;
                other.ProgramOpen();
            }
            else if (me.Status == DoorStatus.Closed && other.Status == DoorStatus.Closed)
            {
                me.Door.Enabled = true;
                other.Door.Enabled = true;
            }
        }
    }
}
