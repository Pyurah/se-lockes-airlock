using System;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Smart airlock and group airlock with O2 management.
    ///
    /// Pressure cycle summary:
    ///   • Player opens outer (#AL) door → pressurize chamber → open inner
    ///   • Player opens inner door       → depressurize chamber → open outer
    ///   • Safety timeout: if a vent makes no progress for <see cref="Settings.TimeoutSeconds"/>,
    ///     retries once then forces the requested door open and logs an error.
    /// </summary>
    public class AdvancedAirlock : AirlockBase
    {
        // true = player came in via outer → chamber needs pressurizing before inner opens
        bool _needsPressurize;
        // true = player going out via inner → chamber needs depressurizing before outer opens
        bool _needsDepressurize;

        readonly string _displayType;

        public AdvancedAirlock(AirlockComponents components, string displayType = "Airlock") : base(components)
        {
            _displayType = displayType;
            foreach (var door in C.OuterDoors) door.SubscribeFunc(OnOuterDoor);
            foreach (var door in C.InnerDoors) door.SubscribeFunc(OnInnerDoor);
            SetState(AirlockState.Unknown);
        }

        public override void Update()
        {
            base.Update();

            switch (State)
            {
                case AirlockState.AwaitingTotalLock:
                    UpdateAwaitingLock();
                    break;
                case AirlockState.Pressurizing:
                    UpdatePressurizing();
                    break;
                case AirlockState.Depressurizing:
                    UpdateDepressurizing();
                    break;
                case AirlockState.Unknown:
                    UpdateUnknown();
                    break;
            }
        }

        // --- state update methods --------------------------------------------

        void UpdateAwaitingLock()
        {
            if (_needsPressurize && OuterOpenCount <= 0)
            {
                if (C.Context.InAtmo) EnableVents(false);
                else Depressurize(false);
                Timeout = C.Context.Time + C.VentTimeout;
                StartOxygen = PrimaryOxygenLevel();
                SetState(AirlockState.Pressurizing);
            }
            else if (_needsDepressurize && InnerOpenCount <= 0)
            {
                if (C.Context.InAtmo && !C.Gas.AttemptAirScoop) EnableVents(false);
                else Depressurize(true);
                Timeout = C.Context.Time + C.VentTimeout;
                StartOxygen = PrimaryOxygenLevel();
                SetState(AirlockState.Depressurizing);
            }
        }

        void UpdatePressurizing()
        {
            if (!_needsPressurize) return;

            MaybeAtmoSkip = StartOxygen > 0.8;
            CurrentOxygen = PrimaryOxygenLevel();

            bool timedOut = false;
            if (C.Context.Time > Timeout)
            {
                if (VentProgressStalled()) { timedOut = true; CurrentOxygen = 1; }
                else { StartOxygen = CurrentOxygen; Timeout = C.Context.Time + C.VentTimeout; }
            }

            if (CurrentOxygen > 0.9 || C.Context.InAtmo)
            {
                _needsPressurize = false;
                ErrorStatus = timedOut ? "Pressurization stalled" : "";
                ClearLockRequests(C.InnerDoors);
                ClearLockRequests(C.OuterDoors);
                EnableDoors(C.InnerDoors, true);
                OpenAll(C.InnerDoors);
                Timeout = TimeSpan.MaxValue;
                SetState(AirlockState.InnerOpen);
            }
        }

        void UpdateDepressurizing()
        {
            CurrentOxygen = PrimaryOxygenLevel();

            bool timedOut = false;
            if (C.Context.Time > Timeout)
            {
                if (VentProgressStalled()) { timedOut = true; CurrentOxygen = 0; }
                else { StartOxygen = CurrentOxygen; Timeout = C.Context.Time + C.VentTimeout; }
            }

            bool ready = CurrentOxygen < 0.1
                || C.Gas.TanksFullSkipDepressurize
                || C.Context.InAtmo
                || MaybeAtmoSkip;

            if (ready)
            {
                ErrorStatus = timedOut ? "Depressurization stalled" : "";
                Timeout = TimeSpan.MaxValue;

                if (_needsDepressurize)
                {
                    _needsDepressurize = false;
                    if (!C.Gas.AttemptAirScoop) EnableVents(false);
                    ClearLockRequests(C.OuterDoors);
                    ClearLockRequests(C.InnerDoors);
                    EnableDoors(C.OuterDoors, true);
                    OpenAll(C.OuterDoors);
                    SetState(AirlockState.OuterOpen);
                }
                else
                {
                    Depressurize(false);
                    ClearLockRequests(C.OuterDoors);
                    ClearLockRequests(C.InnerDoors);
                    EnableDoors(C.OuterDoors, true);
                    EnableDoors(C.InnerDoors, true);
                    SetState(AirlockState.Neutral);
                }
            }
        }

        void UpdateUnknown()
        {
            EnableDoors(C.OuterDoors, true);
            EnableDoors(C.InnerDoors, true);
            if (InnerOpenCount <= 0 && OuterOpenCount <= 0)
            {
                Depressurize(true);
                SetState(AirlockState.Neutral);
            }
        }

        // --- door event handlers ---------------------------------------------

        void OnOuterDoor(ExtendedDoor door)
        {
            if (door.Door.Status == DoorStatus.Opening) OuterOpenCount++;
            else if (door.Door.Status == DoorStatus.Closed) OuterOpenCount--;

            if (State == AirlockState.Neutral && door.Door.Status == DoorStatus.Opening && !door.ProgramOpening)
            {
                _needsPressurize = true;
                SendLockRequests(C.InnerDoors);
                SendLockRequests(C.OuterDoors);
                SetState(AirlockState.AwaitingTotalLock);
            }
            else if (State == AirlockState.OuterOpen && OuterOpenCount <= 0)
            {
                Depressurize(false);
                ClearLockRequests(C.InnerDoors);
                ClearLockRequests(C.OuterDoors);
                EnableDoors(C.InnerDoors, true);
                SetState(AirlockState.Neutral);
            }
        }

        void OnInnerDoor(ExtendedDoor door)
        {
            if (door.Door.Status == DoorStatus.Opening) InnerOpenCount++;
            else if (door.Door.Status == DoorStatus.Closed) InnerOpenCount--;

            if (State == AirlockState.Neutral && door.Door.Status == DoorStatus.Opening && !door.ProgramOpening)
            {
                _needsDepressurize = true;
                Depressurize(false);
                SendLockRequests(C.InnerDoors);
                SendLockRequests(C.OuterDoors);
                SetState(AirlockState.AwaitingTotalLock);
            }
            else if (State == AirlockState.InnerOpen && InnerOpenCount <= 0)
            {
                Depressurize(true);
                Timeout = C.Context.Time + C.VentTimeout;
                StartOxygen = PrimaryOxygenLevel();
                SetState(AirlockState.Depressurizing);
            }
        }

        // --- status ----------------------------------------------------------

        protected override void OnStateChanged(AirlockState newState)
        {
            switch (newState)
            {
                case AirlockState.Neutral:
                    C.SetLightsIdle();
                    break;
                case AirlockState.AwaitingTotalLock:
                    C.SetLightsBusy();
                    if (_needsPressurize) C.TriggerInnerTimers();
                    if (_needsDepressurize) C.TriggerOuterTimers();
                    break;
                case AirlockState.Unknown:
                    C.SetLightsBusy();
                    break;
            }

            if (C.Display == null) return;

            switch (newState)
            {
                case AirlockState.Neutral:
                    C.Display.Update(ErrorStatus.Length > 0 ? ErrorStatus : "Ready", ErrorStatus.Length > 0);
                    break;
                case AirlockState.AwaitingTotalLock:
                    C.Display.Update("Locking doors");
                    break;
                case AirlockState.Pressurizing:
                    C.Display.Update("Pressurizing");
                    break;
                case AirlockState.Depressurizing:
                    C.Display.Update("Depressurizing");
                    break;
                case AirlockState.OuterOpen:
                    C.Display.Update(ErrorStatus.Length > 0 ? ErrorStatus : "Outer open", ErrorStatus.Length > 0);
                    break;
                case AirlockState.InnerOpen:
                    C.Display.Update(ErrorStatus.Length > 0 ? ErrorStatus : "Inner open", ErrorStatus.Length > 0);
                    break;
                case AirlockState.Unknown:
                    C.Display.Update("Setup in progress");
                    break;
                default:
                    C.Display.Update("Ready");
                    break;
            }
        }
    }
}
