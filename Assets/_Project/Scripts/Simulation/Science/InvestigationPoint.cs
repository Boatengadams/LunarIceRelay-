using LunarIceRelay.Data;
using UnityEngine;

namespace LunarIceRelay.Simulation.Science
{
    public enum InvestigationKind
    {
        IceDeposit,
        Regolith,
        Geology,
        ShadowedColdTrap
    }

    /// <summary>
    /// Scientific investigation / sample point on the lunar surface.
    /// </summary>
    public sealed class InvestigationPoint : MonoBehaviour
    {
        [SerializeField] private string pointId = "ICE-01";
        [SerializeField] private InvestigationKind kind = InvestigationKind.IceDeposit;
        [SerializeField] private float analysisDataMb = 3.5f;
        [SerializeField] private float sampleDataMb = 2.0f;
        [SerializeField] private bool requiresInstrument = true;
        [SerializeField] private bool collectibleSample = true;

        public string PointId => pointId;
        public InvestigationKind Kind => kind;
        public bool IsAnalyzed { get; private set; }
        public bool SampleTaken { get; private set; }
        public bool CanCollectSample => collectibleSample && !SampleTaken;
        public bool CanAnalyze => !IsAnalyzed;

        public void Configure(string id, InvestigationKind investigationKind, float analysisMb, float sampleMb)
        {
            pointId = id;
            kind = investigationKind;
            analysisDataMb = analysisMb;
            sampleDataMb = sampleMb;
        }

        public bool TryCollectSample(out string sampleId, out float dataMb)
        {
            sampleId = $"{pointId}-SAMPLE";
            dataMb = 0f;
            if (!CanCollectSample)
                return false;

            SampleTaken = true;
            dataMb = sampleDataMb;
            return true;
        }

        public bool TryAnalyze(bool hasRequiredInstrument, out float dataMb)
        {
            dataMb = 0f;
            if (!CanAnalyze)
                return false;
            if (requiresInstrument && !hasRequiredInstrument)
            {
                MissionState.Instance?.LogEvent($"Instrument missing for {pointId}.");
                return false;
            }

            IsAnalyzed = true;
            dataMb = analysisDataMb;
            MissionState.Instance?.RegisterSciencePointCompleted(pointId, dataMb);
            return true;
        }
    }
}
