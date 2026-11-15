using LunarIceRelay.Data;
using UnityEngine;

namespace LunarIceRelay.Simulation.Rover
{
    /// <summary>
    /// Simplified gameplay battery model.
    /// ASSUMPTION: linear Wh drain from drive/idle power — not a verified electrical model.
    /// </summary>
    public sealed class RoverBattery : MonoBehaviour
    {
        [SerializeField] private float capacityWh = 1200f;
        [SerializeField] private float remainingWh = 1200f;

        public float Normalized => capacityWh <= 0f ? 0f : Mathf.Clamp01(remainingWh / capacityWh);
        public bool IsDepleted => remainingWh <= 0.01f;
        public float RemainingWh => remainingWh;

        public void ApplyConfig(MissionConfig config)
        {
            if (config == null)
                return;
            capacityWh = Mathf.Max(1f, config.batteryCapacityWh);
            remainingWh = capacityWh;
            PushToMissionState();
        }

        public void Consume(float watts, float deltaTime)
        {
            if (watts <= 0f || deltaTime <= 0f || IsDepleted)
            {
                PushToMissionState();
                return;
            }

            // Power (W) * hours = Wh
            float hours = deltaTime / 3600f;
            remainingWh = Mathf.Max(0f, remainingWh - watts * hours);
            PushToMissionState();
        }

        public void SetNormalized(float normalized)
        {
            remainingWh = Mathf.Clamp01(normalized) * capacityWh;
            PushToMissionState();
        }

        void PushToMissionState()
        {
            MissionState.Instance?.SetBatteryNormalized(Normalized);
        }
    }
}
