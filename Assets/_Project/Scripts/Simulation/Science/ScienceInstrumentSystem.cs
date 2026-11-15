using LunarIceRelay.Data;
using LunarIceRelay.Systems.Multiplayer;
using UnityEngine;

namespace LunarIceRelay.Simulation.Science
{
    /// <summary>
    /// Mission Scientist instrument operations near investigation points.
    /// </summary>
    public sealed class ScienceInstrumentSystem : MonoBehaviour
    {
        [SerializeField] private Transform rover;
        [SerializeField] private float operateRadius = 3f;
        [SerializeField] private float analysisTimer;
        [SerializeField] private InvestigationPoint activePoint;

        readonly Collider[] _overlap = new Collider[8];

        public bool IsAnalyzing => activePoint != null && analysisTimer > 0f;
        public float AnalysisProgress01 { get; private set; }

        public void BindRover(Transform roverTransform) => rover = roverTransform;

        void Update()
        {
            var scientist = LobbySession.Instance?.GetPlayerWithRole(PlayerRole.MissionScientist);
            if (scientist == null || rover == null)
                return;

            scientist.PollActions();

            if (IsAnalyzing)
            {
                TickAnalysis(Time.deltaTime);
                return;
            }

            // Scientist uses Interact to start analysis.
            // If this player also operates the rover, prefer sample collection on the same press.
            if (!scientist.InteractPressed)
                return;

            if (scientist.HasRole(PlayerRole.RoverOperator))
            {
                // RoverController handles collect first on the same frame for stacked roles.
                // Only analyze when the nearest point has no remaining sample to take.
                var nearest = FindNearestAnalyzable();
                if (nearest != null && nearest.CanCollectSample)
                    return;
            }

            var point = FindNearestAnalyzable();
            if (point == null)
                return;

            var config = MissionState.Instance?.Config;
            bool hasInstrument = config == null || config.hasSpectrometer || config.hasDrill || config.hasCameraSuite;
            analysisTimer = config != null ? config.analysisDurationSeconds : 4f;
            activePoint = point;
            AnalysisProgress01 = 0f;
            MissionState.Instance?.LogEvent($"Analysis started at {point.PointId}.");
            // Validate instrument availability early
            if (!hasInstrument)
            {
                MissionState.Instance?.LogEvent("No scientific instruments configured.");
                activePoint = null;
                analysisTimer = 0f;
            }
        }

        void TickAnalysis(float dt)
        {
            var config = MissionState.Instance?.Config;
            float duration = config != null ? Mathf.Max(0.5f, config.analysisDurationSeconds) : 4f;
            analysisTimer -= dt;
            AnalysisProgress01 = 1f - Mathf.Clamp01(analysisTimer / duration);

            if (analysisTimer > 0f)
                return;

            bool hasInstrument = config == null || config.hasSpectrometer || config.hasDrill || config.hasCameraSuite;
            activePoint.TryAnalyze(hasInstrument, out _);
            activePoint = null;
            AnalysisProgress01 = 0f;
        }

        InvestigationPoint FindNearestAnalyzable()
        {
            int hits = Physics.OverlapSphereNonAlloc(rover.position, operateRadius, _overlap, ~0, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            InvestigationPoint bestPoint = null;
            for (int i = 0; i < hits; i++)
            {
                var point = _overlap[i].GetComponentInParent<InvestigationPoint>();
                if (point == null || !point.CanAnalyze)
                    continue;
                float d = (point.transform.position - rover.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestPoint = point;
                }
            }
            return bestPoint;
        }
    }
}
