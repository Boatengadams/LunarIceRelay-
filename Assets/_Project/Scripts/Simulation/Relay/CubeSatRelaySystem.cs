using LunarIceRelay.Data;
using LunarIceRelay.Systems.Multiplayer;
using UnityEngine;

namespace LunarIceRelay.Simulation.Relay
{
    /// <summary>
    /// Simplified CubeSat relay simulation.
    ///
    /// ASSUMPTIONS (game rules, NOT scientifically verified orbital mechanics):
    /// - Orbit is abstracted as a repeating period with a communication window fraction.
    /// - Signal interruption is a probabilistic event per window.
    /// - Transfer rate is a fixed gameplay Mbps value from MissionConfig.
    /// Do not treat timings as real lunar orbital predictions.
    /// </summary>
    public sealed class CubeSatRelaySystem : MonoBehaviour
    {
        [SerializeField] private CubeSatVisual visual;
        [SerializeField] private Transform deployOrigin;

        float _orbitTimer;
        float _windowTimer;
        float _deployTimer;
        float _transferBufferMb;
        bool _windowOpen;
        bool _interruptLatched;

        public RelayLinkState State { get; private set; } = RelayLinkState.Offline;
        public bool IsWindowOpen => _windowOpen;
        public float WindowProgress01 { get; private set; }
        public float OrbitProgress01 { get; private set; }
        public float TransferBufferMb => _transferBufferMb;

        public void BindVisual(CubeSatVisual cubesatVisual, Transform origin)
        {
            visual = cubesatVisual;
            deployOrigin = origin;
        }

        void Update()
        {
            var config = MissionState.Instance?.Config;
            if (config == null)
                return;

            HandleCommsInput();
            TickState(config, Time.deltaTime);
            MissionState.Instance.SetRelayState(State);
        }

        void HandleCommsInput()
        {
            var engineer = LobbySession.Instance?.GetPlayerWithRole(PlayerRole.CommunicationsEngineer);
            if (engineer == null)
                return;

            engineer.PollActions();
            if (!engineer.RelayPressed)
                return;

            if (State == RelayLinkState.Offline)
                BeginDeploy();
            else if (State == RelayLinkState.Available || State == RelayLinkState.Interrupted)
                BeginTransfer();
        }

        public void BeginDeploy()
        {
            if (State != RelayLinkState.Offline)
                return;

            var config = MissionState.Instance?.Config;
            _deployTimer = config != null ? config.deployDurationSeconds : 6f;
            State = RelayLinkState.Deploying;
            visual?.PlayDeploy(deployOrigin != null ? deployOrigin.position : Vector3.zero);
            MissionState.Instance?.LogEvent("CubeSat deployment sequence initiated. (Simplified simulation)");
        }

        public void BeginTransfer()
        {
            var state = MissionState.Instance;
            if (state == null)
                return;
            if (!_windowOpen)
            {
                state.LogEvent("No communication window — transfer unavailable.");
                return;
            }
            if (state.PendingDataMb <= 0.01f)
            {
                state.LogEvent("No pending science data to transmit.");
                return;
            }

            _transferBufferMb = state.PendingDataMb;
            State = RelayLinkState.Transferring;
            state.LogEvent("Data uplink started through CubeSat relay.");
        }

        void TickState(MissionConfig config, float dt)
        {
            switch (State)
            {
                case RelayLinkState.Deploying:
                    _deployTimer -= dt;
                    visual?.TickDeploy(1f - Mathf.Clamp01(_deployTimer / Mathf.Max(0.1f, config.deployDurationSeconds)));
                    if (_deployTimer <= 0f)
                    {
                        State = RelayLinkState.Available;
                        MissionState.Instance?.SetCubeSatDeployed(true);
                        MissionState.Instance?.LogEvent("CubeSat on station. Awaiting communication windows.");
                        _orbitTimer = 0f;
                    }
                    break;

                case RelayLinkState.Available:
                case RelayLinkState.Interrupted:
                case RelayLinkState.Transferring:
                    TickOrbitWindow(config, dt);
                    if (State == RelayLinkState.Transferring)
                        TickTransfer(config, dt);
                    break;
            }
        }

        void TickOrbitWindow(MissionConfig config, float dt)
        {
            float period = Mathf.Max(5f, config.orbitPeriodSeconds);
            float window = Mathf.Clamp(config.communicationWindowSeconds, 1f, period);

            _orbitTimer += dt;
            if (_orbitTimer >= period)
            {
                _orbitTimer -= period;
                _interruptLatched = false;
            }

            OrbitProgress01 = _orbitTimer / period;

            // Window centered in orbit period for readability.
            float windowStart = (period - window) * 0.5f;
            bool open = _orbitTimer >= windowStart && _orbitTimer <= windowStart + window;
            if (open)
            {
                if (!_windowOpen)
                {
                    _windowOpen = true;
                    _windowTimer = 0f;
                    MaybeInterrupt(config);
                    if (State != RelayLinkState.Transferring && State != RelayLinkState.Interrupted)
                        MissionState.Instance?.LogEvent("Communication window open.");
                }

                _windowTimer += dt;
                WindowProgress01 = Mathf.Clamp01(_windowTimer / window);
            }
            else
            {
                if (_windowOpen)
                {
                    _windowOpen = false;
                    WindowProgress01 = 0f;
                    if (State == RelayLinkState.Transferring)
                    {
                        State = RelayLinkState.Interrupted;
                        MissionState.Instance?.LogEvent("Signal lost — window closed mid-transfer.");
                    }
                    else if (State == RelayLinkState.Interrupted)
                    {
                        State = RelayLinkState.Available;
                    }
                    else
                    {
                        MissionState.Instance?.LogEvent("Communication window closed.");
                    }
                }
            }

            visual?.SetSignalActive(_windowOpen && State != RelayLinkState.Interrupted);
        }

        void MaybeInterrupt(MissionConfig config)
        {
            if (_interruptLatched)
                return;
            _interruptLatched = true;
            if (Random.value < Mathf.Clamp01(config.interruptChancePerWindow))
            {
                State = RelayLinkState.Interrupted;
                MissionState.Instance?.LogEvent("Signal interruption event. (Randomized gameplay event)");
                visual?.SetSignalActive(false);
            }
        }

        void TickTransfer(MissionConfig config, float dt)
        {
            if (!_windowOpen || State == RelayLinkState.Interrupted)
                return;

            float mb = config.transferRateMbps * dt;
            float chunk = Mathf.Min(mb, _transferBufferMb);
            if (chunk <= 0f)
            {
                State = RelayLinkState.Available;
                return;
            }

            _transferBufferMb -= chunk;
            MissionState.Instance?.RegisterDataReturned(chunk);

            if (_transferBufferMb <= 0.01f || MissionState.Instance.PendingDataMb <= 0.01f)
            {
                _transferBufferMb = 0f;
                State = RelayLinkState.Available;
                MissionState.Instance?.LogEvent("Uplink burst complete.");
            }
        }
    }
}
