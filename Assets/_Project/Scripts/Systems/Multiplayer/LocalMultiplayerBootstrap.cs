using LunarIceRelay.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LunarIceRelay.Systems.Multiplayer
{
    /// <summary>
    /// Spawns PlayerInputManager + join flow for lobby / gameplay scenes.
    /// </summary>
    public sealed class LocalMultiplayerBootstrap : MonoBehaviour
    {
        public static LocalMultiplayerBootstrap Instance { get; private set; }

        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private bool dontDestroyOnLoad = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            // Only auto-host local multiplayer in play/lobby scenes — never on MainMenu UI.
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (scene != "Gameplay" && scene != "Lobby")
                return;

            if (Instance != null || FindFirstObjectByType<LocalMultiplayerBootstrap>() != null)
                return;

            var go = new GameObject("LocalMultiplayer");
            go.AddComponent<LocalMultiplayerBootstrap>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            if (LobbySession.Instance == null)
            {
                var lobbyGo = new GameObject("LobbySession");
                DontDestroyOnLoad(lobbyGo);
                lobbyGo.AddComponent<LobbySession>();
            }

            actions = actions != null ? actions : PlayerPrefabFactory.LoadOrCreateActions();
            if (playerPrefab == null)
                playerPrefab = PlayerPrefabFactory.CreateRuntimePlayerPrefab(actions);

            var manager = GetComponent<PlayerInputManager>();
            if (manager == null)
                manager = gameObject.AddComponent<PlayerInputManager>();

            manager.playerPrefab = playerPrefab;
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            // Input System API: maxPlayerCount is read-only (backed by serialized m_MaxPlayerCount).
            // Enforce the 1–4 seat cap in LobbySession.TryAddPlayer instead of assigning here.

            // Join action lives on the Player map.
            var join = actions.FindAction("Player/Join", false) ?? actions.FindAction("Join", false);
            if (join != null)
            {
                manager.joinAction = new InputActionProperty(join);
                join.Enable();
            }

            if (GetComponent<LocalPlayerManager>() == null)
                gameObject.AddComponent<LocalPlayerManager>();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetPlayMode(SessionPlayMode mode)
        {
            LobbySession.Instance?.ConfigureForPlayMode(mode);
            GetComponent<LocalPlayerManager>()?.EnableJoining();
        }
    }
}
