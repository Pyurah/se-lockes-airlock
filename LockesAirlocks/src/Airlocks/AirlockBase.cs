using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Common state machine and helper operations shared by <see cref="AdvancedAirlock"/> and
    /// <see cref="Hangar"/>. Handles open-count tracking, lock requests, vent commands, and the
    /// gas-system update. Concrete types handle their own state transitions and status text.
    /// </summary>
    public abstract class AirlockBase
    {
        protected readonly AirlockComponents C;

        protected AirlockState State = AirlockState.Unknown;
        protected TimeSpan Timeout = TimeSpan.MaxValue;
        protected double StartOxygen;
        protected double CurrentOxygen;
        protected bool MaybeAtmoSkip;
        protected string ErrorStatus = "";

        int _outerOpenCount;
        int _innerOpenCount;

        public int OuterOpenCount
        {
            get { return _outerOpenCount; }
            set
            {
                _outerOpenCount = value;
                if (_outerOpenCount < 0) RecalcOpenCounts();
            }
        }

        public int InnerOpenCount
        {
            get { return _innerOpenCount; }
            set
            {
                _innerOpenCount = value;
                if (_innerOpenCount < 0) RecalcOpenCounts();
            }
        }

        protected AirlockBase(AirlockComponents components)
        {
            C = components;
            C.Gas.DisableGenerators();
            RecalcOpenCounts();
        }

        /// <summary>Per-tick update: update all block wrappers and the gas system.</summary>
        public virtual void Update()
        {
            foreach (var d in C.OuterDoors) d.Update();
            foreach (var d in C.InnerDoors) d.Update();
            foreach (var v in C.Vents) v.Update();
            C.Gas.Update();
        }

        // --- protected helpers ------------------------------------------------

        protected void SetState(AirlockState newState)
        {
            State = newState;
            OnStateChanged(newState);
        }

        protected abstract void OnStateChanged(AirlockState newState);

        protected void SendLockRequests(List<ExtendedDoor> doors)
        {
            if (C.Context.InAtmo) return;
            foreach (var d in doors) d.LockRequest = true;
        }

        protected void ClearLockRequests(List<ExtendedDoor> doors)
        {
            foreach (var d in doors) d.LockRequest = false;
        }

        protected void EnableDoors(List<ExtendedDoor> doors, bool enabled)
        {
            foreach (var d in doors) d.Door.Enabled = enabled;
        }

        protected void OpenAll(List<ExtendedDoor> doors)
        {
            foreach (var d in doors)
                if (!d.IsManualDoor) d.ProgramOpen();
        }

        protected void CloseAll(List<ExtendedDoor> doors, bool onlyManualOrLarge = false)
        {
            foreach (var d in doors)
                if (!onlyManualOrLarge || d.IsManualDoor || d.IsLargeOpening)
                    d.Door.CloseDoor();
        }

        protected void Depressurize(bool depressurize)
        {
            foreach (var v in C.Vents)
            {
                v.Enabled = true;
                v.Depressurize = depressurize;
            }
        }

        protected void EnableVents(bool enabled)
        {
            foreach (var v in C.Vents) v.Enabled = enabled;
        }

        protected float PrimaryOxygenLevel()
        {
            return C.Vents.Count > 0 ? C.Vents[0].GetOxygenLevel() : 0f;
        }

        protected bool VentProgressStalled()
        {
            return Math.Abs(CurrentOxygen - StartOxygen) < C.Context.Settings.OxygenDifferenceRatio;
        }

        void RecalcOpenCounts()
        {
            _outerOpenCount = 0;
            foreach (var d in C.OuterDoors)
                if (d.Status != DoorStatus.Closed) _outerOpenCount++;

            _innerOpenCount = 0;
            foreach (var d in C.InnerDoors)
                if (d.Status != DoorStatus.Closed) _innerOpenCount++;
        }
    }
}
