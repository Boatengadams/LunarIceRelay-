using LunarIceRelay.Data;
using LunarIceRelay.Systems.Multiplayer;
using UnityEngine;

namespace LunarIceRelay.UI
{
    /// <summary>
    /// Temporary OnGUI readout for gameplay verification until Cursor HUD is wired.
    /// Safe to disable in production builds.
    /// </summary>
    public sealed class GameplayDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible = true;

        void OnGUI()
        {
            if (!visible)
                return;

            const float pad = 10f;
            GUILayout.BeginArea(new Rect(pad, pad, 460f, Screen.height - pad * 2f), GUI.skin.box);
            GUILayout.Label("LUNAR ICE RELAY — Gameplay Overlay");

            var lobby = LobbySession.Instance;
            if (lobby != null)
            {
                GUILayout.Label($"Players: {lobby.PlayerCount}/{LobbySession.MaxPlayers} | Ready:{lobby.AllReady} | Roles:{lobby.RolesCovered} | CanStart:{lobby.CanCommanderStart}");
                foreach (var p in lobby.Players)
                {
                    GUILayout.Label($"  {p.DisplayName} [{p.DeviceDisplayName}] roles={p.Roles} ready={p.IsReady}");
                }
                GUILayout.Label("Join: gamepad A / Enter | Ready: Space/A | Roles: Q/Tab or bumpers | Start: Enter/Start (Commander)");
            }

            var state = MissionState.Instance;
            if (state != null)
            {
                GUILayout.Space(6f);
                GUILayout.Label($"Phase:{state.Phase}  Time:{state.MissionTimeRemaining:0}s  Cam:{state.ActiveCamera}");
                GUILayout.Label($"Battery:{state.BatteryNormalized:0%}  Condition:{state.RoverConditionNormalized:0%}  Power:{state.PowerBudgetNormalized:0%}");
                GUILayout.Label($"Samples:{state.SamplesCollected}  Science:{state.SciencePointsCompleted}  Pending:{state.PendingDataMb:0.0}MB  Returned:{state.ReturnedDataMb:0.0}MB");
                GUILayout.Label($"Relay:{state.RelayState}  CubeSat:{state.CubeSatDeployed}");
                GUILayout.Label(state.LastEventMessage);
                GUILayout.Label("Controls: WASD/Stick drive | Shift/RT accel | Ctrl/LT brake | E/X interact | C/Y camera | R/B relay");
            }

            GUILayout.EndArea();
        }
    }
}
