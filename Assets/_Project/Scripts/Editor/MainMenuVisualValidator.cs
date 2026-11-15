#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LunarIceRelay.EditorTools
{
    /// <summary>
    /// Validates / lightly re-applies MainMenu layout targets for Milestone 1.
    /// </summary>
    public static class MainMenuVisualValidator
    {
        const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        [MenuItem("Lunar Ice Relay/Main Menu/Validate Layout")]
        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            int issues = 0;

            var canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogError("[MainMenu] Canvas must be Screen Space - Overlay.");
                issues++;
            }
            if (scaler == null ||
                scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.referenceResolution != new Vector2(1920, 1080) ||
                !Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f))
            {
                Debug.LogError("[MainMenu] CanvasScaler must be Scale With Screen Size, 1920x1080, match 0.5.");
                issues++;
            }

            Require("LunarSurface", ref issues);
            Require("Astronaut", ref issues);
            Require("Earth", ref issues);
            Require("Stars", ref issues);
            Require("BAGS_Text", ref issues);
            Require("EXPLORE_Text", ref issues);
            Require("Footer", ref issues);
            Require("StartMissionButton", ref issues);

            var howTo = GameObject.Find("HowToPlayPanel");
            var about = GameObject.Find("AboutPanel");
            if (howTo != null && howTo.activeSelf)
            {
                Debug.LogWarning("[MainMenu] HowToPlayPanel is active — should start disabled.");
                issues++;
            }
            if (about != null && about.activeSelf)
            {
                Debug.LogWarning("[MainMenu] AboutPanel is active — should start disabled.");
                issues++;
            }

            if (issues == 0)
                Debug.Log("[MainMenu] Layout validation passed.");
            else
                Debug.LogWarning($"[MainMenu] Validation finished with {issues} issue(s).");
        }

        [MenuItem("Lunar Ice Relay/Main Menu/Apply Logo Typography Polish")]
        public static void ApplyLogoPolish()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var bags = GameObject.Find("BAGS_Text")?.GetComponent<TextMeshProUGUI>();
            var explore = GameObject.Find("EXPLORE_Text")?.GetComponent<TextMeshProUGUI>();
            if (bags != null)
            {
                bags.enableAutoSizing = true;
                bags.fontSizeMin = 90f;
                bags.fontSizeMax = 130f;
                bags.fontSize = 118f;
                bags.color = new Color(0.86f, 0.90f, 0.95f, 1f);
                bags.characterSpacing = 14f;
                bags.fontStyle = FontStyles.Bold;
                EditorUtility.SetDirty(bags);
            }
            if (explore != null)
            {
                explore.enableAutoSizing = true;
                explore.fontSizeMin = 70f;
                explore.fontSizeMax = 110f;
                explore.fontSize = 92f;
                explore.color = new Color(0.12f, 0.86f, 1f, 1f);
                explore.characterSpacing = 10f;
                explore.fontStyle = FontStyles.Bold | FontStyles.Italic;
                EditorUtility.SetDirty(explore);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[MainMenu] Logo typography polish applied.");
        }

        static void Require(string name, ref int issues)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                Debug.LogError($"[MainMenu] Missing GameObject '{name}'.");
                issues++;
                return;
            }
            if (!go.activeInHierarchy)
                Debug.LogWarning($"[MainMenu] '{name}' exists but is inactive.");
        }
    }
}
#endif
