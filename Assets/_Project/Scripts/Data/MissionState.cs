using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarIceRelay.Data
{
    /// <summary>
    /// Shared runtime mission state used by design UI and gameplay simulation.
    /// Single source of truth — do not duplicate mission data elsewhere.
    /// </summary>
    public sealed class MissionState : MonoBehaviour
    {
        public static MissionState Instance { get; private set; }

        [SerializeField] private MissionConfig config = MissionConfig.CreateDefault();

        public MissionConfig Config => config;
        public MissionPhase Phase { get; private set; } = MissionPhase.Boot;
        public SessionPlayMode PlayMode { get; private set; } = SessionPlayMode.LocalCoop;

        // Live simulation values written by gameplay systems
        public float MissionTimeRemaining { get; private set; }
        public float BatteryNormalized { get; private set; } = 1f;
        public float RoverConditionNormalized { get; private set; } = 1f;
        public float PowerBudgetNormalized { get; private set; } = 1f;
        public int SamplesCollected { get; private set; }
        public int SciencePointsCompleted { get; private set; }
        public int DataPacketsReturned { get; private set; }
        public float PendingDataMb { get; private set; }
        public float ReturnedDataMb { get; private set; }
        public RelayLinkState RelayState { get; private set; } = RelayLinkState.Offline;
        public CameraPerspective ActiveCamera { get; private set; } = CameraPerspective.ThirdPerson;
        public bool CubeSatDeployed { get; private set; }
        public bool MissionStarted { get; private set; }
        public bool MissionCompleted { get; private set; }
        public string LastEventMessage { get; private set; } = string.Empty;

        public IReadOnlyList<string> EventLog => _eventLog;
        readonly List<string> _eventLog = new(64);

        public event Action<MissionPhase> PhaseChanged;
        public event Action StateChanged;
        public event Action<string> EventLogged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null)
                config = MissionConfig.CreateDefault();

            MissionTimeRemaining = config.missionDurationSeconds;
            PowerBudgetNormalized = Mathf.Clamp01(config.startingPowerBudget / 100f);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetPlayMode(SessionPlayMode mode)
        {
            PlayMode = mode;
            Notify();
        }

        public void ApplyMissionConfig(MissionConfig designed)
        {
            if (designed == null)
                return;

            config = designed.Clone();
            MissionTimeRemaining = config.missionDurationSeconds;
            PowerBudgetNormalized = Mathf.Clamp01(config.startingPowerBudget / 100f);
            LogEvent($"Mission configuration applied: {config.missionDisplayName} @ {config.landingSiteDisplayName}");
            Notify();
        }

        public void SetPhase(MissionPhase phase)
        {
            if (Phase == phase)
                return;

            Phase = phase;
            PhaseChanged?.Invoke(phase);
            Notify();
        }

        public void BeginGameplay()
        {
            MissionStarted = true;
            MissionCompleted = false;
            MissionTimeRemaining = config.missionDurationSeconds;
            SamplesCollected = 0;
            SciencePointsCompleted = 0;
            DataPacketsReturned = 0;
            PendingDataMb = 0f;
            ReturnedDataMb = 0f;
            BatteryNormalized = 1f;
            RoverConditionNormalized = 1f;
            CubeSatDeployed = false;
            RelayState = RelayLinkState.Offline;
            SetPhase(MissionPhase.Gameplay);
            LogEvent("Lunar surface operations begun.");
        }

        public void TickMissionClock(float deltaTime)
        {
            if (!MissionStarted || MissionCompleted || Phase != MissionPhase.Gameplay)
                return;

            MissionTimeRemaining = Mathf.Max(0f, MissionTimeRemaining - deltaTime);
            if (MissionTimeRemaining <= 0f)
                CompleteMission(false, "Mission timer expired.");
        }

        public void SetBatteryNormalized(float value)
        {
            BatteryNormalized = Mathf.Clamp01(value);
            Notify();
        }

        public void SetRoverConditionNormalized(float value)
        {
            RoverConditionNormalized = Mathf.Clamp01(value);
            Notify();
        }

        public void SetPowerBudgetNormalized(float value)
        {
            PowerBudgetNormalized = Mathf.Clamp01(value);
            Notify();
        }

        public void SetRelayState(RelayLinkState state)
        {
            if (RelayState == state)
                return;
            RelayState = state;
            Notify();
        }

        public void SetCubeSatDeployed(bool deployed)
        {
            CubeSatDeployed = deployed;
            Notify();
        }

        public void SetActiveCamera(CameraPerspective perspective)
        {
            ActiveCamera = perspective;
            Notify();
        }

        public void RegisterSampleCollected(string sampleId, float dataMb)
        {
            SamplesCollected++;
            PendingDataMb += Mathf.Max(0f, dataMb);
            LogEvent($"Sample secured: {sampleId}");
            EvaluateObjectives();
            Notify();
        }

        public void RegisterSciencePointCompleted(string pointId, float dataMb)
        {
            SciencePointsCompleted++;
            PendingDataMb += Mathf.Max(0f, dataMb);
            LogEvent($"Science objective complete: {pointId}");
            EvaluateObjectives();
            Notify();
        }

        public void RegisterDataReturned(float mb)
        {
            if (mb <= 0f)
                return;

            float transferred = Mathf.Min(mb, PendingDataMb);
            PendingDataMb -= transferred;
            ReturnedDataMb += transferred;
            DataPacketsReturned++;
            LogEvent($"Ground station received {transferred:0.0} MB via CubeSat relay.");
            EvaluateObjectives();
            Notify();
        }

        public void CompleteMission(bool success, string reason)
        {
            if (MissionCompleted)
                return;

            MissionCompleted = true;
            LogEvent(success ? $"Mission success — {reason}" : $"Mission ended — {reason}");
            SetPhase(MissionPhase.Results);
        }

        public void LogEvent(string message)
        {
            LastEventMessage = message;
            _eventLog.Add($"[{Time.realtimeSinceStartup:0.0}s] {message}");
            if (_eventLog.Count > 100)
                _eventLog.RemoveAt(0);
            EventLogged?.Invoke(message);
            StateChanged?.Invoke();
        }

        void EvaluateObjectives()
        {
            if (!MissionStarted || MissionCompleted)
                return;

            bool samplesMet = SamplesCollected >= config.requiredIceSamples;
            bool scienceMet = SciencePointsCompleted >= config.requiredSciencePoints;
            bool dataMet = ReturnedDataMb >= config.dataPacketSizeMb;

            if (samplesMet && scienceMet && dataMet)
                CompleteMission(true, "Ice samples analyzed and relayed to Earth.");
        }

        void Notify() => StateChanged?.Invoke();
    }
}
