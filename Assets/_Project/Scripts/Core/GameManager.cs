using LunarIceRelay.Data;
using LunarIceRelay.Systems.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LunarIceRelay.Core
{
    /// <summary>
    /// Single application orchestrator. Do not create a second GameManager.
    /// Owns high-level flow; gameplay systems remain responsible for simulation detail.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const string MainMenuScene = "MainMenu";
        public const string BriefingScene = "Briefing";
        public const string GameplayScene = "Gameplay";
        public const string LobbyScene = "Lobby";

        [SerializeField] private MissionState missionStatePrefab;
        [SerializeField] private bool bootstrapMissionStateIfMissing = true;

        public MissionState Mission => MissionState.Instance;
        public LobbySession Lobby => LobbySession.Instance;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureMissionState();
            EnsureLobbySession();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void EnsureMissionState()
        {
            if (MissionState.Instance != null)
                return;

            if (!bootstrapMissionStateIfMissing)
                return;

            if (missionStatePrefab != null)
            {
                Instantiate(missionStatePrefab);
                return;
            }

            var go = new GameObject("MissionState");
            go.AddComponent<MissionState>();
        }

        void EnsureLobbySession()
        {
            if (LobbySession.Instance != null)
                return;

            var go = new GameObject("LobbySession");
            DontDestroyOnLoad(go);
            go.AddComponent<LobbySession>();
        }

        public void SetPlayMode(SessionPlayMode mode)
        {
            Mission?.SetPlayMode(mode);
            Lobby?.ConfigureForPlayMode(mode);
        }

        public void ApplyDesignedMission(MissionConfig config)
        {
            Mission?.ApplyMissionConfig(config);
        }

        public void GoToMainMenu()
        {
            Mission?.SetPhase(MissionPhase.Boot);
            LoadScene(MainMenuScene);
        }

        public void GoToLobby()
        {
            Mission?.SetPhase(MissionPhase.Lobby);
            if (Application.CanStreamedLevelBeLoaded(LobbyScene))
                LoadScene(LobbyScene);
            else
                Debug.LogWarning("[GameManager] Lobby scene missing — keeping current scene. LobbySession remains available for UI binding.");
        }

        public void GoToBriefing()
        {
            Mission?.SetPhase(MissionPhase.Briefing);
            LoadScene(BriefingScene);
        }

        public void StartGameplay()
        {
            if (Lobby != null && !Lobby.CanCommanderStart)
            {
                Debug.LogWarning("[GameManager] Commander cannot start yet — players not ready / roles incomplete.");
                return;
            }

            Lobby?.LockForGameplay();
            Mission?.BeginGameplay();
            LoadScene(GameplayScene);
        }

        public void EndMissionToResults()
        {
            Mission?.SetPhase(MissionPhase.Results);
        }

        static void LoadScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[GameManager] Scene '{sceneName}' is not in Build Settings.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoBootstrap()
        {
            if (Instance != null)
                return;

            var existing = FindFirstObjectByType<GameManager>();
            if (existing != null)
                return;

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }
    }
}
