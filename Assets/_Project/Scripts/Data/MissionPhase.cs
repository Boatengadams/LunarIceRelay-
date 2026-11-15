namespace LunarIceRelay.Data
{
    public enum MissionPhase
    {
        Boot = 0,
        Lobby = 1,
        MissionDesign = 2,
        Briefing = 3,
        Gameplay = 4,
        Results = 5
    }

    public enum RelayLinkState
    {
        Offline = 0,
        Deploying = 1,
        Available = 2,
        Transferring = 3,
        Interrupted = 4,
        Complete = 5
    }

    public enum CameraPerspective
    {
        ThirdPerson = 0,
        TopDown = 1,
        FirstPerson = 2
    }

    /// <summary>
    /// Local session size mode. Named to avoid clashing with UnityEngine.PlayMode.
    /// </summary>
    public enum SessionPlayMode
    {
        SinglePlayer = 1,
        LocalCoop = 2
    }
}
