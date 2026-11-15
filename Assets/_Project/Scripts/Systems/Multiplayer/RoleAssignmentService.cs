using System.Collections.Generic;
using LunarIceRelay.Data;

namespace LunarIceRelay.Systems.Multiplayer
{
    /// <summary>
    /// Assigns the four cooperative responsibilities across available local players.
    /// Fewer than four players receive stacked roles.
    /// </summary>
    public static class RoleAssignmentService
    {
        public static void AutoAssign(IReadOnlyList<LocalPlayer> players)
        {
            if (players == null || players.Count == 0)
                return;

            foreach (var player in players)
                player.SetRoles(PlayerRole.None);

            switch (players.Count)
            {
                case 1:
                    players[0].SetRoles(PlayerRole.All);
                    break;
                case 2:
                    players[0].SetRoles(PlayerRole.MissionCommander | PlayerRole.CommunicationsEngineer);
                    players[1].SetRoles(PlayerRole.RoverOperator | PlayerRole.MissionScientist);
                    break;
                case 3:
                    players[0].SetRoles(PlayerRole.MissionCommander);
                    players[1].SetRoles(PlayerRole.RoverOperator);
                    players[2].SetRoles(PlayerRole.CommunicationsEngineer | PlayerRole.MissionScientist);
                    break;
                default:
                    for (int i = 0; i < players.Count && i < PlayerRoleUtility.OrderedRoles.Length; i++)
                        players[i].SetRoles(PlayerRoleUtility.OrderedRoles[i]);
                    break;
            }
        }

        public static bool TryClaimRole(LocalPlayer player, PlayerRole role, IReadOnlyList<LocalPlayer> players)
        {
            if (player == null || role == PlayerRole.None || role == PlayerRole.All)
                return false;

            // Single exclusive claim: remove role from others, then add to claimant.
            foreach (var other in players)
            {
                if (other == null || other == player)
                    continue;
                if (other.HasRole(role))
                    other.SetRoles(other.Roles & ~role);
            }

            player.SetRoles(player.Roles | role);
            EnsureCommanderExists(players);
            return true;
        }

        public static void CycleRoleFocus(LocalPlayer player, IReadOnlyList<LocalPlayer> players, int direction)
        {
            if (player == null || players == null || players.Count == 0)
                return;

            // In split-role lobbies, cycle this player's primary selected exclusive role.
            var ordered = PlayerRoleUtility.OrderedRoles;
            int current = 0;
            for (int i = 0; i < ordered.Length; i++)
            {
                if (player.HasRole(ordered[i]))
                {
                    current = i;
                    break;
                }
            }

            int next = (current + direction) % ordered.Length;
            if (next < 0)
                next += ordered.Length;

            // Clear exclusive roles then claim next. Stacked auto-assign remains available via AutoAssign.
            player.SetRoles(PlayerRole.None);
            TryClaimRole(player, ordered[next], players);
        }

        public static void ReassignAfterDeparture(IReadOnlyList<LocalPlayer> remaining)
        {
            AutoAssign(remaining);
        }

        public static bool AllRolesCovered(IReadOnlyList<LocalPlayer> players)
        {
            PlayerRole covered = PlayerRole.None;
            if (players == null)
                return false;

            foreach (var player in players)
                covered |= player.Roles;

            return covered == PlayerRole.All;
        }

        public static LocalPlayer FindCommander(IReadOnlyList<LocalPlayer> players)
        {
            if (players == null)
                return null;

            foreach (var player in players)
            {
                if (player != null && player.IsCommander)
                    return player;
            }

            return null;
        }

        static void EnsureCommanderExists(IReadOnlyList<LocalPlayer> players)
        {
            if (FindCommander(players) != null || players == null || players.Count == 0)
                return;

            players[0].SetRoles(players[0].Roles | PlayerRole.MissionCommander);
        }
    }
}
