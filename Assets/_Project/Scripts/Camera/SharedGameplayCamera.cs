using LunarIceRelay.Data;
using LunarIceRelay.Systems.Multiplayer;
using UnityEngine;

namespace LunarIceRelay.CameraSystem
{
    /// <summary>
    /// One shared gameplay camera for all local players with three switchable perspectives.
    /// </summary>
    public sealed class SharedGameplayCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Camera cam;
        [SerializeField] private CameraPerspective mode = CameraPerspective.ThirdPerson;

        [Header("Third person")]
        [SerializeField] private Vector3 thirdPersonOffset = new(0f, 4.5f, -8f);
        [SerializeField] private float thirdPersonLookHeight = 1.2f;
        [SerializeField] private float thirdPersonFollow = 6f;

        [Header("Top down")]
        [SerializeField] private float topDownHeight = 28f;
        [SerializeField] private float topDownFollow = 5f;
        [SerializeField] private float topDownPitch = 88f;

        [Header("First person")]
        [SerializeField] private Vector3 firstPersonOffset = new(0f, 1.35f, 0.55f);
        [SerializeField] private float firstPersonFollow = 14f;

        [Header("Look")]
        [SerializeField] private float lookYaw;
        [SerializeField] private float lookPitch;
        [SerializeField] private float lookSensitivity = 0.12f;
        [SerializeField] private float switchBlendSeconds = 0.35f;

        float _blend;
        Vector3 _velocity;
        Quaternion _rotVelocity;
        CameraPerspective _fromMode;
        CameraPerspective _toMode;

        public CameraPerspective Mode => mode;
        public Camera Camera => cam;

        void Awake()
        {
            if (cam == null)
                cam = GetComponent<Camera>();
            if (cam == null)
                cam = gameObject.AddComponent<Camera>();

            _fromMode = mode;
            _toMode = mode;
            _blend = 1f;
        }

        public void SetTarget(Transform rover) => target = rover;

        public void SetMode(CameraPerspective perspective)
        {
            if (mode == perspective)
                return;

            _fromMode = mode;
            _toMode = perspective;
            mode = perspective;
            _blend = 0f;
            MissionState.Instance?.SetActiveCamera(perspective);
            MissionState.Instance?.LogEvent($"Camera → {perspective}");
        }

        public void CycleMode()
        {
            int next = ((int)mode + 1) % 3;
            SetMode((CameraPerspective)next);
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            HandleInput();

            if (_blend < 1f)
                _blend = Mathf.Clamp01(_blend + Time.deltaTime / Mathf.Max(0.05f, switchBlendSeconds));

            GetPose(_fromMode, out var fromPos, out var fromRot);
            GetPose(_toMode, out var toPos, out var toRot);

            float t = Smooth01(_blend);
            Vector3 desiredPos = Vector3.Lerp(fromPos, toPos, t);
            Quaternion desiredRot = Quaternion.Slerp(fromRot, toRot, t);

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, 1f / Mathf.Max(0.1f, FollowRate(mode)));
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-FollowRate(mode) * Time.deltaTime));
        }

        void HandleInput()
        {
            // Shared camera: any player with Commander or Rover Operator may cycle; look from rover operator.
            var lobby = LobbySession.Instance;
            if (lobby == null)
                return;

            LocalPlayer lookSource = lobby.GetPlayerWithRole(PlayerRole.RoverOperator)
                                     ?? lobby.GetPlayerWithRole(PlayerRole.MissionCommander);

            if (lookSource != null)
            {
                lookSource.PollActions();
                Vector2 look = lookSource.LookValue;
                lookYaw += look.x * lookSensitivity;
                lookPitch = Mathf.Clamp(lookPitch - look.y * lookSensitivity, -25f, 55f);

                if (lookSource.CameraCyclePressed)
                    CycleMode();
            }

            // Commander can also cycle camera for mission overview.
            var commander = lobby.GetPlayerWithRole(PlayerRole.MissionCommander);
            if (commander != null && commander != lookSource)
            {
                commander.PollActions();
                if (commander.CameraCyclePressed)
                    CycleMode();
            }
        }

        void GetPose(CameraPerspective perspective, out Vector3 position, out Quaternion rotation)
        {
            switch (perspective)
            {
                case CameraPerspective.TopDown:
                {
                    position = target.position + Vector3.up * topDownHeight;
                    rotation = Quaternion.Euler(topDownPitch, lookYaw, 0f);
                    break;
                }
                case CameraPerspective.FirstPerson:
                {
                    Vector3 offset = Quaternion.Euler(0f, lookYaw, 0f) * firstPersonOffset;
                    // Prefer rover facing for FP; allow look yaw offset.
                    Quaternion yaw = Quaternion.Euler(0f, target.eulerAngles.y + lookYaw * 0.25f, 0f);
                    position = target.position + yaw * firstPersonOffset;
                    rotation = yaw * Quaternion.Euler(lookPitch, 0f, 0f);
                    break;
                }
                default:
                {
                    Quaternion yaw = Quaternion.Euler(0f, lookYaw, 0f);
                    Vector3 offset = yaw * thirdPersonOffset;
                    position = target.position + offset;
                    Vector3 lookAt = target.position + Vector3.up * thirdPersonLookHeight;
                    rotation = Quaternion.LookRotation((lookAt - position).normalized, Vector3.up);
                    break;
                }
            }
        }

        float FollowRate(CameraPerspective perspective)
        {
            return perspective switch
            {
                CameraPerspective.TopDown => topDownFollow,
                CameraPerspective.FirstPerson => firstPersonFollow,
                _ => thirdPersonFollow
            };
        }

        static float Smooth01(float t) => t * t * (3f - 2f * t);
    }
}
