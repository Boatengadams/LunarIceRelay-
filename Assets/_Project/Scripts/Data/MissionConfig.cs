using System;
using UnityEngine;

namespace LunarIceRelay.Data
{
    /// <summary>
    /// Mission design output from the engineering / design flow.
    /// Gameplay consumes this; it does not invent a parallel mission definition.
    /// ASSUMPTION: values are gameplay balances, not verified engineering models.
    /// </summary>
    [Serializable]
    public class MissionConfig
    {
        [Header("Identity")]
        public string missionId = "lunar-ice-relay";
        public string missionDisplayName = "Lunar Ice Relay";
        public string landingSiteId = "shackleton-rim";
        public string landingSiteDisplayName = "Shackleton Rim (Simplified)";

        [Header("Rover (design)")]
        [Tooltip("Gameplay max speed in m/s. Not a verified rover performance model.")]
        public float roverMaxSpeed = 4.5f;
        public float roverAcceleration = 3.2f;
        public float roverTurnRate = 70f;
        public float roverBrakeForce = 8f;
        [Range(0.2f, 1f)] public float roverTraction = 0.85f;
        public float batteryCapacityWh = 1200f;
        public float driveDrainWatts = 180f;
        public float idleDrainWatts = 25f;
        public int sampleStorageSlots = 4;

        [Header("Instruments (design)")]
        public bool hasSpectrometer = true;
        public bool hasDrill = true;
        public bool hasCameraSuite = true;
        public float analysisDurationSeconds = 4f;

        [Header("CubeSat Relay (simplified game rules)")]
        [Tooltip("ASSUMPTION: orbital geometry is abstracted into timed communication windows.")]
        public float orbitPeriodSeconds = 90f;
        public float communicationWindowSeconds = 28f;
        public float deployDurationSeconds = 6f;
        public float dataPacketSizeMb = 8f;
        public float transferRateMbps = 1.5f;
        public float interruptChancePerWindow = 0.18f;

        [Header("Mission pacing")]
        public float missionDurationSeconds = 600f;
        public int requiredIceSamples = 2;
        public int requiredSciencePoints = 3;
        public float startingPowerBudget = 100f;

        public static MissionConfig CreateDefault()
        {
            return new MissionConfig();
        }

        public MissionConfig Clone()
        {
            return JsonUtility.FromJson<MissionConfig>(JsonUtility.ToJson(this));
        }
    }
}
