using LunarIceRelay.Data;
using UnityEngine;

namespace LunarIceRelay.Simulation.Mission
{
    /// <summary>
    /// Tracks abstracted mission resources for the Commander view.
    /// ASSUMPTION: power budget is a simplified gameplay meter tied to rover activity.
    /// </summary>
    public sealed class ResourceMonitor : MonoBehaviour
    {
        [SerializeField] private float powerDrainPerSecond = 0.004f;

        public void Tick(float dt)
        {
            var state = MissionState.Instance;
            if (state == null)
                return;

            float drain = powerDrainPerSecond * dt;
            // Extra drain while transferring / analyzing reflected via battery already; keep light here.
            if (state.RelayState == RelayLinkState.Transferring)
                drain *= 1.35f;

            state.SetPowerBudgetNormalized(state.PowerBudgetNormalized - drain);
        }
    }
}
