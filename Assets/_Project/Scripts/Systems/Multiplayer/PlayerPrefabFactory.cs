using UnityEngine;
using UnityEngine.InputSystem;

namespace LunarIceRelay.Systems.Multiplayer
{
    /// <summary>
    /// Builds a runtime PlayerInput prefab when no authored prefab is assigned.
    /// Keeps local multiplayer testable without Editor prefab setup.
    /// </summary>
    public static class PlayerPrefabFactory
    {
        public static GameObject CreateRuntimePlayerPrefab(InputActionAsset actions)
        {
            var root = new GameObject("LocalPlayerPrefab");
            root.SetActive(false);

            var input = root.AddComponent<PlayerInput>();
            input.actions = actions;
            input.defaultActionMap = "Player";
            input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            input.neverAutoSwitchControlSchemes = true;

            root.AddComponent<LocalPlayer>();

            // Hide the template object; PlayerInputManager will instantiate clones.
            Object.DontDestroyOnLoad(root);
            return root;
        }

        public static InputActionAsset LoadOrCreateActions()
        {
            var asset = Resources.Load<InputActionAsset>("Input/LunarPlayer");
            if (asset != null)
                return asset;

            // Fallback: create minimal actions so joining still works if the asset is not in Resources.
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap("Player");
            map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            map.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            map.AddAction("Accelerate", InputActionType.Value, expectedControlLayout: "Axis");
            map.AddAction("Brake", InputActionType.Value, expectedControlLayout: "Axis");
            map.AddAction("Interact", InputActionType.Button);
            map.AddAction("CameraCycle", InputActionType.Button);
            map.AddAction("Relay", InputActionType.Button);
            map.AddAction("Ready", InputActionType.Button);
            map.AddAction("StartMission", InputActionType.Button);
            map.AddAction("RolePrevious", InputActionType.Button);
            map.AddAction("RoleNext", InputActionType.Button);
            map.AddAction("Join", InputActionType.Button);

            var move = map.FindAction("Move");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            map.FindAction("Look").AddBinding("<Gamepad>/rightStick");
            map.FindAction("Look").AddBinding("<Mouse>/delta");
            map.FindAction("Accelerate").AddBinding("<Gamepad>/rightTrigger");
            map.FindAction("Accelerate").AddBinding("<Keyboard>/leftShift");
            map.FindAction("Brake").AddBinding("<Gamepad>/leftTrigger");
            map.FindAction("Brake").AddBinding("<Keyboard>/leftCtrl");
            map.FindAction("Interact").AddBinding("<Keyboard>/e");
            map.FindAction("Interact").AddBinding("<Gamepad>/buttonWest");
            map.FindAction("CameraCycle").AddBinding("<Keyboard>/c");
            map.FindAction("CameraCycle").AddBinding("<Gamepad>/buttonNorth");
            map.FindAction("Relay").AddBinding("<Keyboard>/r");
            map.FindAction("Relay").AddBinding("<Gamepad>/buttonEast");
            map.FindAction("Ready").AddBinding("<Keyboard>/space");
            map.FindAction("Ready").AddBinding("<Gamepad>/buttonSouth");
            map.FindAction("StartMission").AddBinding("<Keyboard>/enter");
            map.FindAction("StartMission").AddBinding("<Gamepad>/start");
            map.FindAction("RolePrevious").AddBinding("<Keyboard>/q");
            map.FindAction("RolePrevious").AddBinding("<Gamepad>/leftShoulder");
            map.FindAction("RoleNext").AddBinding("<Keyboard>/tab");
            map.FindAction("RoleNext").AddBinding("<Gamepad>/rightShoulder");
            map.FindAction("Join").AddBinding("<Keyboard>/enter");
            map.FindAction("Join").AddBinding("<Gamepad>/buttonSouth");

            asset.AddControlScheme("Keyboard&Mouse")
                .WithRequiredDevice("<Keyboard>")
                .WithOptionalDevice("<Mouse>");
            asset.AddControlScheme("Gamepad")
                .WithRequiredDevice("<Gamepad>");

            return asset;
        }
    }
}
