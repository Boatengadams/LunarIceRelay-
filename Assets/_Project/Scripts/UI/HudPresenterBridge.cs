using System;
using LunarIceRelay.Data;
using UnityEngine;

namespace LunarIceRelay.UI
{
    /// <summary>
    /// Read-only HUD binding surface for Cursor UI controllers.
    /// </summary>
    public sealed class HudPresenterBridge : MonoBehaviour
    {
        public event Action StateChanged;

        public float Battery => MissionState.Instance != null ? MissionState.Instance.BatteryNormalized : 0f;
        public float Condition => MissionState.Instance != null ? MissionState.Instance.RoverConditionNormalized : 0f;
        public float PowerBudget => MissionState.Instance != null ? MissionState.Instance.PowerBudgetNormalized : 0f;
        public float TimeRemaining => MissionState.Instance != null ? MissionState.Instance.MissionTimeRemaining : 0f;
        public int Samples => MissionState.Instance != null ? MissionState.Instance.SamplesCollected : 0;
        public int SciencePoints => MissionState.Instance != null ? MissionState.Instance.SciencePointsCompleted : 0;
        public float PendingDataMb => MissionState.Instance != null ? MissionState.Instance.PendingDataMb : 0f;
        public float ReturnedDataMb => MissionState.Instance != null ? MissionState.Instance.ReturnedDataMb : 0f;
        public RelayLinkState Relay => MissionState.Instance != null ? MissionState.Instance.RelayState : RelayLinkState.Offline;
        public CameraPerspective CameraMode => MissionState.Instance != null ? MissionState.Instance.ActiveCamera : CameraPerspective.ThirdPerson;
        public string LastEvent => MissionState.Instance != null ? MissionState.Instance.LastEventMessage : string.Empty;

        void OnEnable()
        {
            if (MissionState.Instance != null)
                MissionState.Instance.StateChanged += OnStateChanged;
        }

        void Start()
        {
            if (MissionState.Instance != null)
            {
                MissionState.Instance.StateChanged -= OnStateChanged;
                MissionState.Instance.StateChanged += OnStateChanged;
            }
        }

        void OnDisable()
        {
            if (MissionState.Instance != null)
                MissionState.Instance.StateChanged -= OnStateChanged;
        }

        void OnStateChanged() => StateChanged?.Invoke();
    }
}
