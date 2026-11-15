#if UNITY_EDITOR
using LunarIceRelay.Simulation.Mission;
using LunarIceRelay.Systems.Multiplayer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LunarIceRelay.EditorTools
{
    /// <summary>
    /// Editor helpers to drop multiplayer + gameplay bootstraps into scenes.
    /// </summary>
    public static class GameplaySetupMenu
    {
        [MenuItem("Lunar Ice Relay/Setup/Add Local Multiplayer Bootstrap To Active Scene")]
        public static void AddMultiplayerBootstrap()
        {
            if (Object.FindFirstObjectByType<LocalMultiplayerBootstrap>() != null)
            {
                Debug.Log("LocalMultiplayerBootstrap already present.");
                return;
            }

            var go = new GameObject("LocalMultiplayer");
            go.AddComponent<LocalMultiplayerBootstrap>();
            Undo.RegisterCreatedObjectUndo(go, "Add Local Multiplayer Bootstrap");
            MarkDirty();
        }

        [MenuItem("Lunar Ice Relay/Setup/Add Gameplay Bootstrap To Active Scene")]
        public static void AddGameplayBootstrap()
        {
            if (Object.FindFirstObjectByType<GameplayBootstrap>() != null)
            {
                Debug.Log("GameplayBootstrap already present.");
                return;
            }

            var go = new GameObject("GameplayBootstrap");
            go.AddComponent<GameplayBootstrap>();
            Undo.RegisterCreatedObjectUndo(go, "Add Gameplay Bootstrap");
            MarkDirty();
        }

        [MenuItem("Lunar Ice Relay/Setup/Create Gameplay Scene Stub")]
        public static void CreateGameplaySceneStub()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("GameplayBootstrap");
            go.AddComponent<GameplayBootstrap>();
            var path = "Assets/_Project/Scenes/Gameplay.unity";
            EditorSceneManager.SaveScene(scene, path);
            AddToBuildSettings(path);
            Debug.Log($"Created {path} and added to Build Settings.");
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == path)
                    return;
            }

            var list = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
                list[i] = scenes[i];
            list[scenes.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = list;
        }

        static void MarkDirty()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
