using LunarIceRelay.CameraSystem;
using LunarIceRelay.Data;
using LunarIceRelay.Simulation.Relay;
using LunarIceRelay.Simulation.Rover;
using LunarIceRelay.Simulation.Science;
using LunarIceRelay.Systems.Multiplayer;
using LunarIceRelay.UI;
using UnityEngine;

namespace LunarIceRelay.Simulation.Mission
{
    /// <summary>
    /// Runtime-constructs a playable lunar sandbox when the Gameplay scene is empty.
    /// WebGL-friendly primitives only — replace with authored art later.
    /// </summary>
    public sealed class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildIfEmpty = true;
        [SerializeField] private bool spawnDebugHud = true;

        void Awake()
        {
            if (!buildIfEmpty)
                return;
            if (FindFirstObjectByType<RoverController>() != null)
                return;

            BuildSandbox();
        }

        void BuildSandbox()
        {
            // Lighting
            if (RenderSettings.sun == null)
            {
                var lightGo = new GameObject("Sun");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.intensity = 1.1f;
                lightGo.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
                RenderSettings.sun = light;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.18f, 0.2f);

            // Ground plane (simplified lunar terrain)
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "LunarSurface";
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            var groundRenderer = ground.GetComponent<Renderer>();
            groundRenderer.material.color = new Color(0.55f, 0.55f, 0.58f);

            // Mild obstacles
            CreateRock(new Vector3(6f, 0.6f, 4f), 1.2f);
            CreateRock(new Vector3(-5f, 0.4f, 7f), 0.9f);
            CreateRock(new Vector3(3f, 0.5f, -6f), 1.0f);
            CreateRock(new Vector3(-8f, 0.7f, -3f), 1.4f);

            // Investigation points
            CreateInvestigation(new Vector3(10f, 0.25f, 8f), "ICE-01", InvestigationKind.IceDeposit, new Color(0.2f, 0.7f, 1f));
            CreateInvestigation(new Vector3(-9f, 0.25f, 5f), "ICE-02", InvestigationKind.ShadowedColdTrap, new Color(0.15f, 0.45f, 0.95f));
            CreateInvestigation(new Vector3(2f, 0.25f, -11f), "GEO-01", InvestigationKind.Geology, new Color(0.85f, 0.7f, 0.3f));

            // Rover
            var roverGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roverGo.name = "Rover";
            roverGo.transform.position = new Vector3(0f, 0.55f, 0f);
            roverGo.transform.localScale = new Vector3(1.6f, 0.7f, 2.2f);
            var roverRenderer = roverGo.GetComponent<Renderer>();
            roverRenderer.material.color = new Color(0.75f, 0.78f, 0.82f);

            var body = roverGo.AddComponent<Rigidbody>();
            body.mass = 120f;

            var battery = roverGo.AddComponent<RoverBattery>();
            var condition = roverGo.AddComponent<RoverCondition>();
            var collector = roverGo.AddComponent<SampleCollector>();
            var rover = roverGo.AddComponent<RoverController>();

            // Ground station marker
            var station = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            station.name = "GroundStation";
            station.transform.position = new Vector3(-2f, 0.75f, -2f);
            station.transform.localScale = new Vector3(0.6f, 0.75f, 0.6f);
            Object.Destroy(station.GetComponent<Collider>());

            // CubeSat visual
            var satGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            satGo.name = "CubeSat";
            satGo.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            Object.Destroy(satGo.GetComponent<Collider>());
            var satVisual = satGo.AddComponent<CubeSatVisual>();
            satVisual.SetGroundStation(station.transform);
            satGo.SetActive(false);

            var relayGo = new GameObject("CubeSatRelaySystem");
            var relay = relayGo.AddComponent<CubeSatRelaySystem>();
            relay.BindVisual(satVisual, roverGo.transform);

            var scienceGo = new GameObject("ScienceInstrumentSystem");
            var science = scienceGo.AddComponent<ScienceInstrumentSystem>();
            science.BindRover(roverGo.transform);

            var camGo = new GameObject("SharedGameplayCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var sharedCam = camGo.AddComponent<SharedGameplayCamera>();
            sharedCam.SetTarget(roverGo.transform);

            // Disable any other cameras that may exist in the scene.
            foreach (var other in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (other != cam)
                    other.enabled = false;
            }

            var resources = gameObject.AddComponent<ResourceMonitor>();
            var events = gameObject.AddComponent<MissionEventDirector>();
            var sim = gameObject.AddComponent<MissionSimulationController>();
            sim.Bind(rover, relay, science, sharedCam, resources, events);

            if (FindFirstObjectByType<LocalMultiplayerBootstrap>() == null)
            {
                var mp = new GameObject("LocalMultiplayer");
                mp.AddComponent<LocalMultiplayerBootstrap>();
            }

            if (FindFirstObjectByType<HudPresenterBridge>() == null)
                gameObject.AddComponent<HudPresenterBridge>();

            if (spawnDebugHud && FindFirstObjectByType<GameplayDebugOverlay>() == null)
                gameObject.AddComponent<GameplayDebugOverlay>();

            var config = MissionState.Instance != null ? MissionState.Instance.Config : MissionConfig.CreateDefault();
            if (config == null)
                config = MissionConfig.CreateDefault();
            MissionState.Instance?.ApplyMissionConfig(config);
            rover.ApplyConfig(config);
        }

        static void CreateRock(Vector3 position, float scale)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Rock";
            rock.transform.position = position;
            rock.transform.localScale = Vector3.one * scale;
            rock.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.38f);
        }

        static void CreateInvestigation(Vector3 position, string id, InvestigationKind kind, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = $"Investigation_{id}";
            go.transform.position = position;
            go.transform.localScale = new Vector3(1.6f, 0.15f, 1.6f);
            go.GetComponent<Renderer>().material.color = color;
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            var point = go.AddComponent<InvestigationPoint>();
            point.Configure(id, kind, 3.5f, 2.0f);
        }
    }
}
