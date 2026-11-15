using UnityEngine;

namespace LunarIceRelay.Simulation.Relay
{
    /// <summary>
    /// Lightweight visual proxy for the CubeSat. Uses transforms only (WebGL-friendly).
    /// </summary>
    public sealed class CubeSatVisual : MonoBehaviour
    {
        [SerializeField] private Transform body;
        [SerializeField] private LineRenderer signalBeam;
        [SerializeField] private Transform groundStationAnchor;
        [SerializeField] private float orbitRadius = 18f;
        [SerializeField] private float orbitHeight = 22f;
        [SerializeField] private float spinDegreesPerSecond = 40f;

        Vector3 _deployStart;
        Vector3 _orbitCenter;
        bool _deployed;
        float _angle;

        void Awake()
        {
            if (body == null)
                body = transform;
            if (signalBeam == null)
            {
                signalBeam = gameObject.GetComponent<LineRenderer>();
                if (signalBeam == null)
                    signalBeam = gameObject.AddComponent<LineRenderer>();
                signalBeam.positionCount = 2;
                signalBeam.startWidth = 0.05f;
                signalBeam.endWidth = 0.02f;
                signalBeam.enabled = false;
            }
        }

        public void PlayDeploy(Vector3 fromWorld)
        {
            if (body == null)
                body = transform;
            EnsureBeam();

            _deployStart = fromWorld;
            _orbitCenter = fromWorld;
            _deployed = false;
            body.position = fromWorld;
            gameObject.SetActive(true);
        }

        void EnsureBeam()
        {
            if (signalBeam != null)
                return;
            signalBeam = gameObject.GetComponent<LineRenderer>();
            if (signalBeam == null)
                signalBeam = gameObject.AddComponent<LineRenderer>();
            signalBeam.positionCount = 2;
            signalBeam.startWidth = 0.05f;
            signalBeam.endWidth = 0.02f;
            signalBeam.enabled = false;
        }

        public void TickDeploy(float t01)
        {
            Vector3 target = _orbitCenter + Vector3.up * orbitHeight + Vector3.forward * orbitRadius * 0.25f;
            body.position = Vector3.Lerp(_deployStart, target, Mathf.SmoothStep(0f, 1f, t01));
            if (t01 >= 0.999f)
                _deployed = true;
        }

        void Update()
        {
            if (!_deployed)
                return;

            _angle += spinDegreesPerSecond * Time.deltaTime;
            float rad = _angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * orbitRadius, orbitHeight, Mathf.Sin(rad) * orbitRadius);
            body.position = _orbitCenter + offset;
            body.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);

            if (signalBeam != null && signalBeam.enabled && groundStationAnchor != null)
            {
                signalBeam.SetPosition(0, body.position);
                signalBeam.SetPosition(1, groundStationAnchor.position);
            }
        }

        public void SetGroundStation(Transform anchor) => groundStationAnchor = anchor;

        public void SetSignalActive(bool active)
        {
            if (signalBeam != null)
                signalBeam.enabled = active && groundStationAnchor != null;
        }
    }
}
