using System;
using System.Collections.Generic;
using LunarIceRelay.Data;
using UnityEngine;

namespace LunarIceRelay.Systems.Multiplayer
{
    /// <summary>
    /// Functional lobby state for 1–4 local players.
    /// Cursor owns visual lobby UI; bind to these events / properties.
    /// </summary>
    public sealed class LobbySession : MonoBehaviour
    {
        public static LobbySession Instance { get; private set; }

        public const int MinPlayers = 1;
        public const int MaxPlayers = 4;

        readonly List<LocalPlayer> _players = new(MaxPlayers);

        public IReadOnlyList<LocalPlayer> Players => _players;
        public int PlayerCount => _players.Count;
        public bool IsLocked { get; private set; }
        public SessionPlayMode PlayMode { get; private set; } = SessionPlayMode.LocalCoop;

        public bool AllReady
        {
            get
            {
                if (_players.Count == 0)
                    return false;
                foreach (var p in _players)
                {
                    if (!p.IsReady)
                        return false;
                }
                return true;
            }
        }

        public bool RolesCovered => RoleAssignmentService.AllRolesCovered(_players);
        public LocalPlayer Commander => RoleAssignmentService.FindCommander(_players);

        public bool CanCommanderStart =>
            !IsLocked &&
            _players.Count >= MinPlayers &&
            AllReady &&
            RolesCovered &&
            Commander != null;

        public event Action LobbyChanged;
        public event Action<LocalPlayer> PlayerJoined;
        public event Action<LocalPlayer> PlayerLeft;
        public event Action StartingGameplay;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ConfigureForPlayMode(SessionPlayMode mode)
        {
            PlayMode = mode;
            Notify();
        }

        public bool TryAddPlayer(LocalPlayer player)
        {
            if (IsLocked || player == null)
                return false;
            if (_players.Contains(player))
                return false;
            if (_players.Count >= MaxPlayers)
                return false;
            if (PlayMode == SessionPlayMode.SinglePlayer && _players.Count >= 1)
                return false;

            _players.Add(player);
            RoleAssignmentService.AutoAssign(_players);
            PlayerJoined?.Invoke(player);
            Notify();
            MissionState.Instance?.LogEvent($"{player.DisplayName} joined via {player.DeviceDisplayName}.");
            return true;
        }

        public bool RemovePlayer(LocalPlayer player)
        {
            if (player == null)
                return false;
            if (!_players.Remove(player))
                return false;

            if (!IsLocked)
                RoleAssignmentService.ReassignAfterDeparture(_players);

            PlayerLeft?.Invoke(player);
            Notify();
            MissionState.Instance?.LogEvent($"{player.DisplayName} left. Roles reassigned.");
            return true;
        }

        public void RemovePlayerByIndex(int playerIndex)
        {
            var player = GetPlayer(playerIndex);
            if (player != null)
                RemovePlayer(player);
        }

        public LocalPlayer GetPlayer(int playerIndex)
        {
            foreach (var player in _players)
            {
                if (player.PlayerIndex == playerIndex)
                    return player;
            }
            return null;
        }

        public LocalPlayer GetPlayerWithRole(PlayerRole role)
        {
            foreach (var player in _players)
            {
                if (player.HasRole(role))
                    return player;
            }
            return null;
        }

        public void SetPlayerReady(int playerIndex, bool ready)
        {
            var player = GetPlayer(playerIndex);
            if (player == null || IsLocked)
                return;
            player.SetReady(ready);
            Notify();
        }

        public void TogglePlayerReady(int playerIndex)
        {
            var player = GetPlayer(playerIndex);
            if (player == null || IsLocked)
                return;
            player.ToggleReady();
            Notify();
        }

        public void ClaimRole(int playerIndex, PlayerRole role)
        {
            if (IsLocked)
                return;
            var player = GetPlayer(playerIndex);
            if (player == null)
                return;
            RoleAssignmentService.TryClaimRole(player, role, _players);
            Notify();
        }

        public void CyclePlayerRole(int playerIndex, int direction)
        {
            if (IsLocked)
                return;
            var player = GetPlayer(playerIndex);
            if (player == null)
                return;
            RoleAssignmentService.CycleRoleFocus(player, _players, direction);
            Notify();
        }

        public void AutoAssignRoles()
        {
            if (IsLocked)
                return;
            RoleAssignmentService.AutoAssign(_players);
            Notify();
        }

        public bool TryCommanderStart()
        {
            if (!CanCommanderStart)
                return false;

            StartingGameplay?.Invoke();
            return true;
        }

        public void LockForGameplay()
        {
            IsLocked = true;
            Notify();
        }

        public void Unlock()
        {
            IsLocked = false;
            foreach (var player in _players)
                player.SetReady(false);
            Notify();
        }

        public void ClearPlayers()
        {
            _players.Clear();
            IsLocked = false;
            Notify();
        }

        public void Notify() => LobbyChanged?.Invoke();
    }
}
