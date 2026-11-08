using RoboClash;
using RoboClash.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoboClashGameplayUIGenerator
{
    private const string GameplayScenePath = "Assets/Scenes/RoboClashGameplay_XL.unity";

    [MenuItem("Tools/RoboClash/Create Gameplay UI")]
    [MenuItem("Tools/RoboClash/Create Gameplay HUD")]
    public static void CreateGameplayUI()
    {
        var scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, GameplayScenePath, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError(
                $"Cannot add the RoboClash gameplay HUD to '{scene.path}'. " +
                $"Open '{GameplayScenePath}' and run this command there.");
            return;
        }

        RoboClashMatchManager matchManager = null;
        foreach (var candidate in Object.FindObjectsByType<RoboClashMatchManager>())
        {
            if (candidate.gameObject.scene == scene)
            {
                matchManager = candidate;
                break;
            }
        }

        if (matchManager == null)
        {
            Debug.LogError(
                $"Cannot add the RoboClash gameplay UI: scene '{scene.name}' at '{scene.path}' " +
                "has no RoboClashMatchManager.");
            return;
        }

        var existingHud = matchManager.GetComponent<RoboClashHUD>();
        if (existingHud != null)
        {
            Selection.activeObject = existingHud;
            EditorGUIUtility.PingObject(existingHud);
            Debug.Log("The active scene already has a RoboClashHUD; no duplicate was created.", existingHud);
            return;
        }

        var hud = Undo.AddComponent<RoboClashHUD>(matchManager.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeObject = hud;
        EditorGUIUtility.PingObject(hud);
        Debug.Log(
            $"Added RoboClashHUD to '{scene.path}'. Save the scene to keep the change; its Canvas and EventSystem are created at runtime.",
            hud);
    }
}
