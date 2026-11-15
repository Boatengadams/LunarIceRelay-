using LunarIceRelay.Data;
using LunarIceRelay.Simulation.Science;
using UnityEngine;

namespace LunarIceRelay.Simulation.Rover
{
    /// <summary>
    /// Collects ice / regolith samples near the rover.
    /// </summary>
    public sealed class SampleCollector : MonoBehaviour
    {
        [SerializeField] private float collectRadius = 2.2f;
        [SerializeField] private LayerMask sampleMask = ~0;
        [SerializeField] private int storageSlots = 4;

        readonly Collider[] _overlap = new Collider[8];

        public int StoredCount { get; private set; }
        public int StorageSlots => storageSlots;

        public void ApplyConfig(MissionConfig config)
        {
            if (config == null)
                return;
            storageSlots = Mathf.Max(1, config.sampleStorageSlots);
        }

        public bool TryCollectNearest(out InvestigationPoint point)
        {
            point = null;
            if (StoredCount >= storageSlots)
            {
                MissionState.Instance?.LogEvent("Sample storage full.");
                return false;
            }

            int hits = Physics.OverlapSphereNonAlloc(transform.position, collectRadius, _overlap, sampleMask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            InvestigationPoint bestPoint = null;

            for (int i = 0; i < hits; i++)
            {
                var candidate = _overlap[i].GetComponentInParent<InvestigationPoint>();
                if (candidate == null || !candidate.CanCollectSample)
                    continue;

                float d = (candidate.transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestPoint = candidate;
                }
            }

            if (bestPoint == null)
                return false;

            if (!bestPoint.TryCollectSample(out var sampleId, out var dataMb))
                return false;

            StoredCount++;
            point = bestPoint;
            MissionState.Instance?.RegisterSampleCollected(sampleId, dataMb);
            return true;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, collectRadius);
        }
#endif
    }
}
