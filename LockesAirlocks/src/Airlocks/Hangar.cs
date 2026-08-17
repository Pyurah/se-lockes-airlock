using System;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Hangar airlock — uses <c>#Hangar</c> tag; inner door is optional; triggered by
    /// toggling the air vent's Depressurize action (e.g. via a button panel).
    ///
    /// Vent pressed to Depressurize=true  → lock inner doors → depressurize → open outer
    /// Vent pressed to Depressurize=false → lock outer doors → pressurize  → open inner
    ///
    /// While outer is open and tanks are low, the script enables vents in "air scoop" mode
    /// to harvest atmosphere. Transitioning from atmo to vacuum auto-closes inner doors.
    /// </summary>
    public class Hangar : AirlockBase
    {
        bool _lastAirScoop;

        public Hangar(AirlockComponents components) : base(components)
        {
            foreach (var door in C.OuterDoors)
            {
                door.SubscribeFunc(OnOuterDoor);
                if (door.IsLargeOpening)
                {
                    door.TimeOpenEntering = -1;
                    door.TimeOpenExiting = -1;
                    door.AutoClose = false;
                }
            }
            foreach (var door in C.InnerDoors)
            {
                door.SubscribeFunc(OnInnerDoor);
                if (door.IsLargeOpening)
                {
                    door.TimeOpenEntering = -1;
                    door.TimeOpenExiting = -1;
                    door.AutoClose = false;
                }
            }
            foreach (var vent in C.Vents)
                vent.SubscribeFunc(OnVent);

            SetState(AirlockState.Unknown);
        }

        public override void Update()
        {
            base.Update();

            switch (State)
            {
                case AirlockState.AwaitingInnerLock:
                    if (InnerOpenCount <= 0)
                    {
                        Depressurize(true);
                        Timeout = C.Context.Time + C.VentTimeout;
                        StartOxygen = PrimaryOxygenLevel();
                        SetState(AirlockState.Depressurizing);
                    }
                    break;

                case AirlockState.AwaitingOuterLock:
                    if (OuterOpenCount <= 0)
                    {
                        Depressurize(false);
                        Timeout = C.Context.Time + C.VentTimeout;
                        StartOxygen = PrimaryOxygenLevel();
                        SetState(AirlockState.Pressurizing);
                    }
                    break;

                case AirlockState.Depressurizing:
                    UpdateDepressurizing();
                    break;

                case AirlockState.Pressurizing:
                    UpdatePressurizing();
                    break;

                case AirlockState.Unknown:
                    UpdateUnknown();
                    break;

                case AirlockState.OuterOpen:
                    UpdateOuterOpen();
                    break;
            }
        }

        void UpdateDepressurizing()
        {
            CurrentOxygen = PrimaryOxygenLevel();
            if (C.Context.Time > Timeout)
            {
                if (VentProgressStalled()) { CurrentOxygen = 0; Timeout = TimeSpan.MaxValue; }
                else { StartOxygen = CurrentOxygen; Timeout = C.Context.Time + C.VentTimeout; }
            }

            bool ready = CurrentOxygen < 0.1
                || C.Gas.TanksFullSkipDepressurize
                || C.Context.InAtmo
                || MaybeAtmoSkip;

            if (ready)
            {
                EnableDoors(C.OuterDoors, true);
                OpenAll(C.OuterDoors);
                EnableVents(false);
                ErrorStatus = C.Context.Time > Timeout ? "Depressurization failed" : "";
                Timeout = TimeSpan.MaxValue;
                SetState(AirlockState.OuterOpen);
            }
        }

        void UpdatePressurizing()
        {
            MaybeAtmoSkip = StartOxygen > 0.8;
            CurrentOxygen = PrimaryOxygenLevel();

            if (C.Context.Time > Timeout)
            {
                if (VentProgressStalled()) { CurrentOxygen = 1; Timeout = TimeSpan.MaxValue; }
                else { StartOxygen = CurrentOxygen; Timeout = C.Context.Time + C.VentTimeout; }
            }

            if (CurrentOxygen > 0.9 || C.Context.InAtmo)
            {
                EnableDoors(C.InnerDoors, true);
                OpenAll(C.InnerDoors);
                ErrorStatus = C.Context.Time > Timeout ? "Pressurization failed" : "";
                Timeout = TimeSpan.MaxValue;
                SetState(AirlockState.InnerOpen);
            }
        }

        void UpdateUnknown()
        {
            if (C.Vents.Count == 0) return;
            if (C.Vents[0].Depressurize)
            {
                Depressurize(true);
                EnableVents(false);
                if (InnerOpenCount <= 0) SetState(AirlockState.OuterOpen);
                else { SendLockRequests(C.InnerDoors); CloseAll(C.InnerDoors, true); SetState(AirlockState.AwaitingInnerLock); }
            }
            else
            {
                Depressurize(false);
                if (OuterOpenCount <= 0) SetState(AirlockState.InnerOpen);
                else { SendLockRequests(C.OuterDoors); CloseAll(C.OuterDoors, true); SetState(AirlockState.AwaitingOuterLock); }
            }
        }

        void UpdateOuterOpen()
        {
            if (C.Gas.AttemptAirScoop && C.Gas.AttemptAirScoop != _lastAirScoop)
                EnableVents(true);
            else if (!C.Gas.AttemptAirScoop && C.Gas.AttemptAirScoop != _lastAirScoop)
                EnableVents(false);
            _lastAirScoop = C.Gas.AttemptAirScoop;

            if (C.Context.InAtmoChanged)
            {
                if (!C.Context.InAtmo)
                {
                    CloseAll(C.InnerDoors, true);
                    SendLockRequests(C.InnerDoors);
                    SetState(AirlockState.AwaitingInnerLock);
                }
                else
                {
                    EnableDoors(C.InnerDoors, true);
                }
            }
        }

        void OnVent(ExtendedAirVent vent)
        {
            if (vent.Depressurize)
            {
                vent.Depressurize = false;
                SendLockRequests(C.InnerDoors);
                CloseAll(C.InnerDoors, true);
                SetState(AirlockState.AwaitingInnerLock);
            }
            else
            {
                vent.Depressurize = true;
                SendLockRequests(C.OuterDoors);
                CloseAll(C.OuterDoors, true);
                SetState(AirlockState.AwaitingOuterLock);
            }
        }

        void OnOuterDoor(ExtendedDoor door)
        {
            if (door.Door.Status == DoorStatus.Opening) OuterOpenCount++;
            else if (door.Door.Status == DoorStatus.Closed) OuterOpenCount--;
        }

        void OnInnerDoor(ExtendedDoor door)
        {
            if (door.Door.Status == DoorStatus.Opening) InnerOpenCount++;
            else if (door.Door.Status == DoorStatus.Closed) InnerOpenCount--;
        }

        protected override void OnStateChanged(AirlockState newState)
        {
            switch (newState)
            {
                case AirlockState.InnerOpen:
                case AirlockState.OuterOpen:
                    C.SetLightsIdle();
                    break;
                case AirlockState.AwaitingInnerLock:
                    C.TriggerOuterTimers();
                    C.SetLightsBusy();
                    break;
                case AirlockState.AwaitingOuterLock:
                    C.TriggerInnerTimers();
                    C.SetLightsBusy();
                    break;
                case AirlockState.Unknown:
                    C.SetLightsBusy();
                    break;
            }

            if (C.Display == null) return;

            bool atmo = C.Context.InAtmo;
            switch (newState)
            {
                case AirlockState.InnerOpen:
                    C.Display.Update(ErrorStatus.Length > 0 ? ErrorStatus : (atmo ? "Inner open - Atmo mode" : "Inner open"), ErrorStatus.Length > 0);
                    break;
                case AirlockState.OuterOpen:
                    C.Display.Update(ErrorStatus.Length > 0 ? ErrorStatus : (atmo ? "Outer open - Atmo mode" : "Outer open"), ErrorStatus.Length > 0);
                    break;
                case AirlockState.AwaitingOuterLock: C.Display.Update("Locking outer"); break;
                case AirlockState.AwaitingInnerLock: C.Display.Update("Locking inner"); break;
                case AirlockState.Pressurizing: C.Display.Update("Pressurizing"); break;
                case AirlockState.Depressurizing: C.Display.Update("Depressurizing"); break;
                case AirlockState.Unknown: C.Display.Update("Setup in progress"); break;
            }
        }
    }
}
