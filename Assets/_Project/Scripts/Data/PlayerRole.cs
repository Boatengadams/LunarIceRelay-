namespace LunarIceRelay.Data
{
    /// <summary>
    /// Cooperative roles. Players may hold multiple roles when fewer than four are present.
    /// </summary>
    [System.Flags]
    public enum PlayerRole
    {
        None = 0,
        MissionCommander = 1 << 0,
        RoverOperator = 1 << 1,
        CommunicationsEngineer = 1 << 2,
        MissionScientist = 1 << 3,
        All = MissionCommander | RoverOperator | CommunicationsEngineer | MissionScientist
    }

    public static class PlayerRoleUtility
    {
        public static readonly PlayerRole[] OrderedRoles =
        {
            PlayerRole.MissionCommander,
            PlayerRole.RoverOperator,
            PlayerRole.CommunicationsEngineer,
            PlayerRole.MissionScientist
        };

        public static string DisplayName(PlayerRole role)
        {
            return role switch
            {
                PlayerRole.MissionCommander => "Mission Commander",
                PlayerRole.RoverOperator => "Rover Operator",
                PlayerRole.CommunicationsEngineer => "Communications Engineer",
                PlayerRole.MissionScientist => "Mission Scientist",
                PlayerRole.All => "All Roles",
                _ => role.ToString()
            };
        }

        public static bool Has(PlayerRole mask, PlayerRole role) => (mask & role) != 0;
    }
}
