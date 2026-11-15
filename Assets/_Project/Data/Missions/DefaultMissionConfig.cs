using LunarIceRelay.Data;
using UnityEngine;

namespace LunarIceRelay.DataAssets
{
    /// <summary>
    /// Optional ScriptableObject wrapper so the engineering UI can author defaults in the Inspector.
    /// Runtime still flows through MissionState.ApplyMissionConfig — no parallel mission data.
    /// </summary>
    [CreateAssetMenu(menuName = "Lunar Ice Relay/Mission Config", fileName = "MissionConfig")]
    public sealed class DefaultMissionConfig : ScriptableObject
    {
        public MissionConfig config = MissionConfig.CreateDefault();

        public void PushToMissionState()
        {
            if (MissionState.Instance != null)
                MissionState.Instance.ApplyMissionConfig(config);
        }
    }
}
