using LunarIceRelay.Data;
using LunarIceRelay.Systems.Multiplayer;
using UnityEngine;

namespace LunarIceRelay.Simulation.Rover
{
    /// <summary>
    /// Interactive lunar rover.
    ///
    /// PHYSICS POLICY:
    /// - Uses SIMPLIFIED GAMEPLAY PHYSICS (arcade Rigidbody forces + low gravity scale).
    /// - Lunar gravity constant (1.62 m/s²) is referenced for feel, but traction, acceleration,
    ///   and braking are intentionally tuned for playability — not a verified vehicle dynamics model.
    /// - Avoids heavy WheelCollider simulation for WebGL performance.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RoverController : MonoBehaviour
    {
        public const float LunarGravityMs2 = 1.62f;

        [Header("Gameplay handling (not verified rover dynamics)")]
        [SerializeField] private float maxSpeed = 4.5f;
        [SerializeField] private float acceleration = 3.2f;
        [SerializeField] private float reverseAcceleration = 2.2f;
        [SerializeField] private float turnRateDegrees = 70f;
        [SerializeField] private float brakeForce = 8f;
        [SerializeField] private float coastDrag = 0.35f;
        [SerializeField] private float traction = 0.85f;
        [SerializeField] private float slopeLimitDegrees = 28f;

        [Header("Refs")]
        [SerializeField] private Rigidbody body;
        [SerializeField] private RoverBattery battery;
        [SerializeField] private RoverCondition condition;
        [SerializeField] private SampleCollector collector;
        [SerializeField] private Transform groundProbe;
        [SerializeField] private float probeDistance = 1.2f;
        [SerializeField] private LayerMask groundMask = ~0;

        float _steerInput;
        float _throttleInput;
        float _brakeInput;
        bool _grounded;
        Vector3 _groundNormal = Vector3.up;

        public float Speed => body != null ? body.linearVelocity.magnitude : 0f;
        public bool IsGrounded => _grounded;
        public RoverBattery Battery => battery;
        public RoverCondition Condition => condition;
        public SampleCollector Collector => collector;

        void Awake()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (battery == null)
                battery = GetComponent<RoverBattery>();
            if (condition == null)
                condition = GetComponent<RoverCondition>();
            if (collector == null)
                collector = GetComponent<SampleCollector>();

            body.useGravity = false; // custom lunar g
            body.linearDamping = 0.05f;
            body.angularDamping = 1.5f;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (groundProbe == null)
            {
                var probe = new GameObject("GroundProbe");
                probe.transform.SetParent(transform, false);
                probe.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                groundProbe = probe.transform;
            }
        }

        public void ApplyConfig(MissionConfig config)
        {
            if (config == null)
                return;

            maxSpeed = config.roverMaxSpeed;
            acceleration = config.roverAcceleration;
            turnRateDegrees = config.roverTurnRate;
            brakeForce = config.roverBrakeForce;
            traction = config.roverTraction;
            battery?.ApplyConfig(config);
            collector?.ApplyConfig(config);
        }

        void Update()
        {
            ReadOperatorInput();
            TryOperatorInteract();
        }

        void FixedUpdate()
        {
            ProbeGround();
            ApplyLunarGravity();
            SimulateDrive(Time.fixedDeltaTime);
            DrainBattery(Time.fixedDeltaTime);
        }

        void ReadOperatorInput()
        {
            _steerInput = 0f;
            _throttleInput = 0f;
            _brakeInput = 0f;

            var lobby = LobbySession.Instance;
            var operatorPlayer = lobby?.GetPlayerWithRole(PlayerRole.RoverOperator);
            if (operatorPlayer == null)
                return;

            operatorPlayer.PollActions();
            Vector2 move = operatorPlayer.MoveValue;
            _steerInput = Mathf.Clamp(move.x, -1f, 1f);

            // Prefer dedicated accelerate/brake axes; fall back to stick/keyboard Y as throttle.
            float accel = operatorPlayer.AccelerateValue;
            float brake = operatorPlayer.BrakeValue;
            _brakeInput = Mathf.Clamp01(brake);

            if (Mathf.Abs(accel) > 0.01f)
                _throttleInput = Mathf.Clamp01(accel);
            else
                _throttleInput = Mathf.Clamp(move.y, -1f, 1f);
        }

        void TryOperatorInteract()
        {
            var lobby = LobbySession.Instance;
            var operatorPlayer = lobby?.GetPlayerWithRole(PlayerRole.RoverOperator);
            var scientist = lobby?.GetPlayerWithRole(PlayerRole.MissionScientist);

            bool collect = (operatorPlayer != null && operatorPlayer.InteractPressed) ||
                           (scientist != null && scientist.InteractPressed && scientist != operatorPlayer);
            if (collect)
                collector?.TryCollectNearest(out _);
        }

        void ProbeGround()
        {
            Vector3 origin = groundProbe.position;
            if (Physics.Raycast(origin, Vector3.down, out var hit, probeDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                _grounded = true;
                _groundNormal = hit.normal;
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > slopeLimitDegrees)
                    condition?.ApplyRoughTerrain(Mathf.InverseLerp(slopeLimitDegrees, 45f, slope), Time.fixedDeltaTime);
            }
            else
            {
                _grounded = false;
                _groundNormal = Vector3.up;
            }
        }

        void ApplyLunarGravity()
        {
            body.AddForce(Vector3.down * LunarGravityMs2, ForceMode.Acceleration);
        }

        void SimulateDrive(float dt)
        {
            if (condition != null && condition.IsImmobilized)
                return;
            if (battery != null && battery.IsDepleted)
                return;
            if (!_grounded)
                return;

            // Steering
            if (Mathf.Abs(_steerInput) > 0.01f && Speed > 0.15f)
            {
                float turn = _steerInput * turnRateDegrees * dt * Mathf.Sign(Vector3.Dot(body.linearVelocity, transform.forward) + 0.001f);
                Quaternion delta = Quaternion.Euler(0f, turn, 0f);
                body.MoveRotation(body.rotation * delta);
            }

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, _groundNormal).normalized;
            Vector3 velocity = body.linearVelocity;
            Vector3 planar = Vector3.ProjectOnPlane(velocity, _groundNormal);

            // Braking
            if (_brakeInput > 0.01f)
            {
                Vector3 brake = -planar.normalized * (brakeForce * _brakeInput);
                if (planar.sqrMagnitude > 0.01f)
                    body.AddForce(brake, ForceMode.Acceleration);
                return;
            }

            // Throttle / reverse
            if (Mathf.Abs(_throttleInput) > 0.01f)
            {
                float accel = _throttleInput >= 0f ? acceleration : reverseAcceleration;
                float tractionFactor = Mathf.Clamp01(traction);
                Vector3 desired = forward * (_throttleInput * maxSpeed);
                Vector3 deltaV = desired - planar;
                Vector3 force = Vector3.ClampMagnitude(deltaV, accel * tractionFactor);
                body.AddForce(force, ForceMode.Acceleration);
            }
            else
            {
                // Coast damping on regolith (gameplay feel)
                body.AddForce(-planar * coastDrag, ForceMode.Acceleration);
            }

            // Soft speed clamp
            planar = Vector3.ProjectOnPlane(body.linearVelocity, _groundNormal);
            if (planar.magnitude > maxSpeed)
            {
                Vector3 clamped = planar.normalized * maxSpeed;
                Vector3 vertical = body.linearVelocity - planar;
                body.linearVelocity = clamped + vertical;
            }
        }

        void DrainBattery(float dt)
        {
            if (battery == null)
                return;

            var config = MissionState.Instance?.Config;
            float idle = config != null ? config.idleDrainWatts : 25f;
            float drive = config != null ? config.driveDrainWatts : 180f;
            float watts = idle + Mathf.Abs(_throttleInput) * drive + _brakeInput * drive * 0.25f;
            battery.Consume(watts, dt);
        }

        void OnCollisionEnter(Collision collision)
        {
            float severity = Mathf.InverseLerp(1.5f, 8f, collision.relativeVelocity.magnitude);
            if (severity > 0.05f)
            {
                condition?.ApplyImpact(severity);
                MissionState.Instance?.LogEvent($"Rover impact ({severity:0.00}) with {collision.collider.name}.");
            }
        }
    }
}
