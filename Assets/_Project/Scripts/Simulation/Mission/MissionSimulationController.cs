using LunarIceRelay.CameraSystem;
using LunarIceRelay.Data;
using LunarIceRelay.Simulation.Relay;
using LunarIceRelay.Simulation.Rover;
using LunarIceRelay.Simulation.Science;
using LunarIceRelay.Systems.Multiplayer;
using UnityEngine;

namespace LunarIceRelay.Simulation.Mission
{
    /// <summary>
    /// Wires designed MissionConfig into live gameplay systems and advances mission clock/events.
    /// </summary>
    public sealed class MissionSimulationController : MonoBehaviour
    {
        [SerializeField] private RoverController rover;
        [SerializeField] private CubeSatRelaySystem relay;
        [SerializeField] private ScienceInstrumentSystem science;
        [SerializeField] private SharedGameplayCamera gameplayCamera;
        [SerializeField] private ResourceMonitor resources;
        [SerializeField] private MissionEventDirector events;

        void Start()
        {
            var state = MissionState.Instance;
            if (state == null)
            {
                Debug.LogError("[MissionSimulation] MissionState missing.");
                return;
            }

            if (!state.MissionStarted)
                state.BeginGameplay();

            ApplyConfig(state.Config);
            state.LogEvent("Simulation online. Cooperative roles active.");
        }

        void Update()
        {
            var state = MissionState.Instance;
            if (state == null || state.Phase != MissionPhase.Gameplay)
                return;

            state.TickMissionClock(Time.deltaTime);
            resources?.Tick(Time.deltaTime);
            events?.Tick(Time.deltaTime);

            // Commander monitors resources / can request event acknowledgements via Ready button reuse in gameplay.
            var commander = LobbySession.Instance?.GetPlayerWithRole(PlayerRole.MissionCommander);
            if (commander != null)
            {
                commander.PollActions();
                // StartMission button doubles as "acknowledge mission alert" during gameplay.
                if (commander.StartPressed)
                    events?.AcknowledgeActiveAlert();
            }
        }

        public void Bind(
            RoverController roverController,
            CubeSatRelaySystem relaySystem,
            ScienceInstrumentSystem scienceSystem,
            SharedGameplayCamera camera,
            ResourceMonitor resourceMonitor,
            MissionEventDirector eventDirector)
        {
            rover = roverController;
            relay = relaySystem;
            science = scienceSystem;
            gameplayCamera = camera;
            resources = resourceMonitor;
            events = eventDirector;
        }

        void ApplyConfig(MissionConfig config)
        {
            rover?.ApplyConfig(config);
            if (rover != null)
            {
                science?.BindRover(rover.transform);
                gameplayCamera?.SetTarget(rover.transform);
            }
        }
    }
}
