using LunarIceRelay.Data;
using UnityEngine;

namespace LunarIceRelay.Simulation.Mission
{
    /// <summary>
    /// Lightweight mission events for cooperative tension (signal storms, dust, timeline alerts).
    /// These are gameplay events, not physical forecasts.
    /// </summary>
    public sealed class MissionEventDirector : MonoBehaviour
    {
        [SerializeField] private float minEventInterval = 45f;
        [SerializeField] private float maxEventInterval = 90f;

        float _nextEventIn;
        string _activeAlert;

        public string ActiveAlert => _activeAlert;
        public bool HasActiveAlert => !string.IsNullOrEmpty(_activeAlert);

        void OnEnable()
        {
            ScheduleNext();
        }

        public void Tick(float dt)
        {
            if (MissionState.Instance == null || MissionState.Instance.MissionCompleted)
                return;

            _nextEventIn -= dt;
            if (_nextEventIn > 0f)
                return;

            FireRandomEvent();
            ScheduleNext();
        }

        public void AcknowledgeActiveAlert()
        {
            if (!HasActiveAlert)
                return;
            MissionState.Instance?.LogEvent($"Commander acknowledged: {_activeAlert}");
            _activeAlert = null;
        }

        void FireRandomEvent()
        {
            int roll = Random.Range(0, 4);
            switch (roll)
            {
                case 0:
                    _activeAlert = "Timeline: communication window approaching — prepare uplink.";
                    break;
                case 1:
                    _activeAlert = "Dust advisory: reduce speed near crater rim. (Gameplay event)";
                    break;
                case 2:
                    _activeAlert = "Science priority: investigate shadowed cold-trap markers.";
                    break;
                default:
                    _activeAlert = "Relay health check recommended by Mission Control.";
                    break;
            }

            MissionState.Instance?.LogEvent(_activeAlert);
        }

        void ScheduleNext()
        {
            _nextEventIn = Random.Range(minEventInterval, maxEventInterval);
        }
    }
}
