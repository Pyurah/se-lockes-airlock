using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Wraps an <see cref="IMyDoor"/> with auto-close timing, "locked" (disabled-while-closed)
    /// requests, and change notifications. Distinguishes doors the script opened itself
    /// (<see cref="ProgramOpening"/>) from doors a player opened, and flags manual and large
    /// (hangar/gate) doors which are handled differently by the airlocks.
    /// </summary>
    public class ExtendedDoor
    {
        readonly ScriptContext _ctx;
        readonly List<Action> _actions = new List<Action>();
        readonly List<Action<ExtendedDoor>> _funcs = new List<Action<ExtendedDoor>>();

        TimeSpan _autoCloseTime = TimeSpan.MaxValue;
        float _autoCloseInSecondsOnceOpen = -1;
        DoorStatus _lastStatus;

        public readonly IMyDoor Door;
        public float TimeOpenEntering;
        public float TimeOpenExiting;
        public bool AutoClose;
        public bool IsLargeOpening;
        public bool IsManualDoor;
        public bool LockRequest;
        public bool ProgramOpening { get; private set; }
        public bool ProgramClosing { get; private set; }

        public DoorStatus Status => Door.Status;

        public ExtendedDoor(ScriptContext ctx, IMyDoor door, bool autoClose = true, float timeOpenEntering = 0.5f, float timeOpenExiting = 2f)
        {
            _ctx = ctx;
            Door = door;
            AutoClose = autoClose;
            TimeOpenEntering = timeOpenEntering;
            TimeOpenExiting = timeOpenExiting;
            _lastStatus = door.Status;

            IsLargeOpening = BlockClassifier.IsLargeOpening(door);

            if (TagMatcher.HasTag(ctx.Settings.ManualTag, door.CustomName))
            {
                AutoClose = false;
                IsManualDoor = true;
            }
        }

        public void Update()
        {
            if (AutoClose)
            {
                if (_ctx.Time > _autoCloseTime)
                {
                    Door.CloseDoor();
                    ProgramClosing = true;
                    _autoCloseTime = TimeSpan.MaxValue;
                }
                else if (Door.Status == DoorStatus.Open && _autoCloseTime == TimeSpan.MaxValue && TimeOpenEntering >= 0)
                {
                    _autoCloseTime = _ctx.Time + TimeSpan.FromSeconds(TimeOpenEntering);
                }
            }

            if (LockRequest && Door.Status == DoorStatus.Closed)
            {
                Door.Enabled = false;
                LockRequest = false;
            }

            if (Door.Status != _lastStatus)
            {
                if (AutoClose && _autoCloseInSecondsOnceOpen >= 0 && Door.Status == DoorStatus.Open)
                {
                    _autoCloseTime = _ctx.Time + TimeSpan.FromSeconds(_autoCloseInSecondsOnceOpen);
                    _autoCloseInSecondsOnceOpen = -1;
                }
                if (Door.Status == DoorStatus.Closed || Door.Status == DoorStatus.Open)
                    ProgramOpening = false;

                foreach (var action in _actions) action();
                foreach (var func in _funcs) func(this);
            }
            _lastStatus = Door.Status;
        }

        public void Subscribe(Action action) => _actions.Add(action);
        public void SubscribeFunc(Action<ExtendedDoor> func) => _funcs.Add(func);

        /// <summary>Opens the door under script control, scheduling the "exiting" auto-close delay.</summary>
        public void ProgramOpen() => ProgramOpen(TimeOpenExiting);

        public void ProgramOpen(float seconds)
        {
            Door.OpenDoor();
            ProgramOpening = true;
            _autoCloseInSecondsOnceOpen = seconds;
        }
    }
}
