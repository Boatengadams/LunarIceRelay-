using LunarIceRelay.Core;
using LunarIceRelay.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LunarIceRelay.Systems.Multiplayer
{
    /// <summary>
    /// Bridges Unity PlayerInputManager join/leave into LobbySession.
    /// Requires a Player prefab with PlayerInput + LocalPlayer.
    /// </summary>
    [RequireComponent(typeof(PlayerInputManager))]
    public sealed class LocalPlayerManager : MonoBehaviour
    {
        public static LocalPlayerManager Instance { get; private set; }

        [SerializeField] private PlayerInputManager playerInputManager;
        [SerializeField] private bool enableJoinOnStart = true;
        [SerializeField] private bool processLobbyInput = true;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (playerInputManager == null)
                playerInputManager = GetComponent<PlayerInputManager>();

            playerInputManager.playerJoinedEvent.AddListener(OnPlayerJoined);
            playerInputManager.playerLeftEvent.AddListener(OnPlayerLeft);
        }

        void OnDestroy()
        {
            if (playerInputManager != null)
            {
                playerInputManager.playerJoinedEvent.RemoveListener(OnPlayerJoined);
                playerInputManager.playerLeftEvent.RemoveListener(OnPlayerLeft);
            }

            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            if (enableJoinOnStart)
                EnableJoining();
            else
                DisableJoining();
        }

        void Update()
        {
            if (!processLobbyInput)
                return;

            var lobby = LobbySession.Instance;
            if (lobby == null || lobby.IsLocked)
                return;

            for (int i = 0; i < lobby.PlayerCount; i++)
            {
                var player = lobby.Players[i];
                if (player == null)
                    continue;

                player.PollActions();

                if (player.ReadyPressed)
                    lobby.TogglePlayerReady(player.PlayerIndex);

                if (player.RoleNextPressed)
                    lobby.CyclePlayerRole(player.PlayerIndex, +1);
                if (player.RolePrevPressed)
                    lobby.CyclePlayerRole(player.PlayerIndex, -1);

                if (player.IsCommander && player.StartPressed)
                {
                    if (lobby.TryCommanderStart())
                        GameManager.Instance?.StartGameplay();
                }
            }
        }

        public void EnableJoining()
        {
            if (playerInputManager != null)
                playerInputManager.EnableJoining();
        }

        public void DisableJoining()
        {
            if (playerInputManager != null)
                playerInputManager.DisableJoining();
        }

        void OnPlayerJoined(PlayerInput input)
        {
            if (input == null)
                return;

            var local = input.GetComponent<LocalPlayer>();
            if (local == null)
                local = input.gameObject.AddComponent<LocalPlayer>();

            local.Bind(input, input.playerIndex);

            var lobby = LobbySession.Instance;
            if (lobby == null)
            {
                Debug.LogError("[LocalPlayerManager] LobbySession missing.");
                return;
            }

            if (!lobby.TryAddPlayer(local))
            {
                Debug.LogWarning($"[LocalPlayerManager] Rejected join for player {input.playerIndex} (lobby full or locked).");
                Destroy(input.gameObject);
                return;
            }

            // Keep joined players alive across design → gameplay scene loads.
            DontDestroyOnLoad(input.gameObject);

            if (lobby.PlayMode == SessionPlayMode.SinglePlayer || lobby.PlayerCount >= LobbySession.MaxPlayers)
                DisableJoining();
        }

        void OnPlayerLeft(PlayerInput input)
        {
            if (input == null)
                return;

            var local = input.GetComponent<LocalPlayer>();
            LobbySession.Instance?.RemovePlayer(local);

            if (LobbySession.Instance != null &&
                !LobbySession.Instance.IsLocked &&
                LobbySession.Instance.PlayMode == SessionPlayMode.LocalCoop)
            {
                EnableJoining();
            }
        }
    }
}
