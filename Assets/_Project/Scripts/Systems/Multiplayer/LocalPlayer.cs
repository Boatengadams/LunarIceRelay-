using System;
using System.Collections.Generic;
using LunarIceRelay.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LunarIceRelay.Systems.Multiplayer
{
    /// <summary>
    /// One local seated player bound to a PlayerInput / input device.
    /// </summary>
    public sealed class LocalPlayer : MonoBehaviour
    {
        public int PlayerIndex { get; private set; } = -1;
        public string DisplayName { get; private set; } = "Player";
        public string DeviceDisplayName { get; private set; } = "Unknown";
        public PlayerRole Roles { get; private set; } = PlayerRole.None;
        public bool IsReady { get; private set; }
        public bool IsCommander => PlayerRoleUtility.Has(Roles, PlayerRole.MissionCommander);
        public PlayerInput PlayerInput { get; private set; }

        public event Action<LocalPlayer> Changed;

        InputAction _move;
        InputAction _look;
        InputAction _accelerate;
        InputAction _brake;
        InputAction _interact;
        InputAction _cameraCycle;
        InputAction _relay;
        InputAction _ready;
        InputAction _start;
        InputAction _rolePrev;
        InputAction _roleNext;

        public Vector2 MoveValue { get; private set; }
        public Vector2 LookValue { get; private set; }
        public float AccelerateValue { get; private set; }
        public float BrakeValue { get; private set; }

        public void Bind(PlayerInput input, int playerIndex)
        {
            PlayerInput = input;
            PlayerIndex = playerIndex;
            DisplayName = $"Player {playerIndex + 1}";
            DeviceDisplayName = DescribeDevices(input);
            CacheActions(input);
            Notify();
        }

        public void SetRoles(PlayerRole roles)
        {
            Roles = roles;
            // Changing roles clears ready to force confirmation of new responsibilities.
            IsReady = false;
            Notify();
        }

        public void SetReady(bool ready)
        {
            IsReady = ready;
            Notify();
        }

        public void ToggleReady() => SetReady(!IsReady);

        public bool HasRole(PlayerRole role) => PlayerRoleUtility.Has(Roles, role);

        public void PollActions()
        {
            if (PlayerInput == null || !PlayerInput.inputIsActive)
            {
                MoveValue = Vector2.zero;
                LookValue = Vector2.zero;
                AccelerateValue = 0f;
                BrakeValue = 0f;
                return;
            }

            MoveValue = _move?.ReadValue<Vector2>() ?? Vector2.zero;
            LookValue = _look?.ReadValue<Vector2>() ?? Vector2.zero;
            AccelerateValue = _accelerate?.ReadValue<float>() ?? 0f;
            BrakeValue = _brake?.ReadValue<float>() ?? 0f;
        }

        public bool WasPressed(string actionName)
        {
            var action = FindAction(actionName);
            return action != null && action.WasPressedThisFrame();
        }

        public bool InteractPressed => _interact != null && _interact.WasPressedThisFrame();
        public bool CameraCyclePressed => _cameraCycle != null && _cameraCycle.WasPressedThisFrame();
        public bool RelayPressed => _relay != null && _relay.WasPressedThisFrame();
        public bool ReadyPressed => _ready != null && _ready.WasPressedThisFrame();
        public bool StartPressed => _start != null && _start.WasPressedThisFrame();
        public bool RolePrevPressed => _rolePrev != null && _rolePrev.WasPressedThisFrame();
        public bool RoleNextPressed => _roleNext != null && _roleNext.WasPressedThisFrame();

        void CacheActions(PlayerInput input)
        {
            var map = input.actions;
            if (map == null)
                return;

            _move = map.FindAction("Move", false);
            _look = map.FindAction("Look", false);
            _accelerate = map.FindAction("Accelerate", false);
            _brake = map.FindAction("Brake", false);
            _interact = map.FindAction("Interact", false);
            _cameraCycle = map.FindAction("CameraCycle", false);
            _relay = map.FindAction("Relay", false);
            _ready = map.FindAction("Ready", false);
            _start = map.FindAction("StartMission", false);
            _rolePrev = map.FindAction("RolePrevious", false);
            _roleNext = map.FindAction("RoleNext", false);
        }

        InputAction FindAction(string name)
        {
            return PlayerInput != null ? PlayerInput.actions?.FindAction(name, false) : null;
        }

        static string DescribeDevices(PlayerInput input)
        {
            if (input?.devices == null || input.devices.Count == 0)
                return "No Device";

            var names = new List<string>(input.devices.Count);
            foreach (var device in input.devices)
                names.Add(device.displayName);
            return string.Join(" + ", names);
        }

        void Notify() => Changed?.Invoke(this);
    }
}
