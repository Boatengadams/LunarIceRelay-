using System;
using LunarIceRelay.Data;
using LunarIceRelay.Systems.Multiplayer;
using UnityEngine;

namespace LunarIceRelay.UI
{
    /// <summary>
    /// Functional bridge for Cursor's lobby visuals.
    /// Does not create UI widgets — only exposes lobby state + commands.
    /// </summary>
    public sealed class LobbyPresenterBridge : MonoBehaviour
    {
        public LobbySession Session => LobbySession.Instance;

        public event Action LobbyChanged;

        void OnEnable()
        {
            if (LobbySession.Instance != null)
                LobbySession.Instance.LobbyChanged += OnLobbyChanged;
        }

        void OnDisable()
        {
            if (LobbySession.Instance != null)
                LobbySession.Instance.LobbyChanged -= OnLobbyChanged;
        }

        void Start()
        {
            // Late subscribe if LobbySession spawned after this bridge.
            if (LobbySession.Instance != null)
            {
                LobbySession.Instance.LobbyChanged -= OnLobbyChanged;
                LobbySession.Instance.LobbyChanged += OnLobbyChanged;
                OnLobbyChanged();
            }
        }

        void OnLobbyChanged() => LobbyChanged?.Invoke();

        public void SetPlayMode(SessionPlayMode mode) => Core.GameManager.Instance?.SetPlayMode(mode);
        public void AutoAssignRoles() => Session?.AutoAssignRoles();
        public void ClaimRole(int playerIndex, PlayerRole role) => Session?.ClaimRole(playerIndex, role);
        public void SetReady(int playerIndex, bool ready) => Session?.SetPlayerReady(playerIndex, ready);
        public void ToggleReady(int playerIndex) => Session?.TogglePlayerReady(playerIndex);
        public bool TryStartAsCommander() => Session != null && Session.TryCommanderStart() && StartGameplay();

        bool StartGameplay()
        {
            Core.GameManager.Instance?.StartGameplay();
            return true;
        }
    }
}
