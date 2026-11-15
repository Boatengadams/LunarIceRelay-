using LunarIceRelay.Data;
using UnityEngine;

namespace LunarIceRelay.Simulation.Rover
{
    /// <summary>
    /// Tracks rover wear from impacts and rough terrain.
    /// GAMEPLAY PHYSICS: condition is a simplified durability meter, not structural FEA.
    /// </summary>
    public sealed class RoverCondition : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float condition = 1f;
        [SerializeField] private float impactDamageScale = 0.08f;
        [SerializeField] private float roughTerrainDrainPerSecond = 0.01f;

        public float Normalized => condition;
        public bool IsCritical => condition < 0.2f;
        public bool IsImmobilized => condition <= 0.01f;

        public void ApplyImpact(float severity01)
        {
            condition = Mathf.Clamp01(condition - Mathf.Clamp01(severity01) * impactDamageScale);
            Push();
        }

        public void ApplyRoughTerrain(float amount01, float deltaTime)
        {
            if (amount01 <= 0f)
                return;
            condition = Mathf.Clamp01(condition - amount01 * roughTerrainDrainPerSecond * deltaTime);
            Push();
        }

        public void Repair(float amount01)
        {
            condition = Mathf.Clamp01(condition + amount01);
            Push();
        }

        void Push() => MissionState.Instance?.SetRoverConditionNormalized(condition);
    }
}
