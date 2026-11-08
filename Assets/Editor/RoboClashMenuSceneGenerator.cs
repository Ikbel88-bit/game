using System;
using System.Collections.Generic;
using RoboClash;
using RoboClash.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoboClashMenuSceneGenerator
{
    private const string MenuScenePath = "Assets/Scenes/RoboClashMainMenu.unity";
    private const string ArenaScenePath = "Assets/Scenes/RoboClashGameplay_XL.unity";
    private const string MenuRootName = "RoboClash Generated Main Menu";

    [MenuItem("Tools/RoboClash/Create Main Menu UI")]
    public static void CreateMainMenuScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null)
        {
            Debug.LogError(
                $"Menu scene already exists at '{MenuScenePath}'. It was not overwritten. Rename or back up that scene before generating a replacement.");
            return;
        }

        if (!ValidateArenaScene())
            return;

        if (!EditorUtility.DisplayDialog(
                "Create RoboClash Main Menu",
                "Create a new menu scene, register it before the gameplay scene in Build Settings, and make the menu the startup scene? Existing scene assets will be preserved.",
                "Create Menu",
                "Cancel"))
            return;

        var robotPrefabs = LoadRobotPrefabs();
        if (robotPrefabs == null)
            return;

        var previousActiveScene = SceneManager.GetActiveScene();
        var menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var root = new GameObject(MenuRootName);
            var controller = root.AddComponent<RoboClashMenuController>();
            controller.ConfigureRobotPrefabs(robotPrefabs);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(menuScene);

            if (!EditorSceneManager.SaveScene(menuScene, MenuScenePath))
                throw new InvalidOperationException("Unity could not save the RoboClash main menu scene.");

            ConfigureBuildSceneOrder();
            AssetDatabase.Refresh();

            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);

            Debug.Log(
                $"Created '{MenuScenePath}' and made it the first enabled scene in Build Settings. " +
                "The menu scene builds its Canvas and EventSystem at runtime.",
                root);
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not create the RoboClash menu scene: " + exception.Message);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) == null &&
                menuScene.IsValid() && menuScene.isLoaded)
                EditorSceneManager.CloseScene(menuScene, true);
            return;
        }

        if (menuScene.IsValid() && menuScene.isLoaded)
            EditorSceneManager.CloseScene(menuScene, true);

        var menuAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
        if (menuAsset != null)
            EditorGUIUtility.PingObject(menuAsset);
    }

    private static GameObject[] LoadRobotPrefabs()
    {
        var prefabs = new GameObject[Enum.GetValues(typeof(RoboClashRobotType)).Length];
        foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
        {
            var path = $"Assets/RoboClash/Generated/Prefabs/{type.ToString().ToUpperInvariant()}.prefab";
            prefabs[(int)type] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabs[(int)type] == null ||
                prefabs[(int)type].GetComponent<RoboClashRobotDefinition>() == null)
            {
                Debug.LogError($"Cannot create the menu: a valid generated robot prefab is missing at '{path}'.");
                return null;
            }
        }

        return prefabs;
    }

    private static bool ValidateArenaScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaScenePath) == null)
        {
            Debug.LogError(
                $"Cannot create the menu: gameplay scene '{ArenaScenePath}' does not exist. Create the arena first.");
            return false;
        }

        var previousActiveScene = SceneManager.GetActiveScene();
        Scene arenaScene = default;
        var openedHere = false;
        try
        {
            foreach (var loadedScene in GetLoadedScenes())
            {
                if (loadedScene.path != ArenaScenePath)
                    continue;
                arenaScene = loadedScene;
                break;
            }

            if (!arenaScene.IsValid())
            {
                arenaScene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }

            var manager = FindManagerInScene(arenaScene);
            if (manager == null)
            {
                Debug.LogError(
                    "Cannot create a connected main menu yet: Arena has no RoboClashMatchManager. " +
                    "Run Tools > RoboClash > Generate Arena, save it, then run Tools > RoboClash > Create Main Menu UI.");
                return false;
            }

            if (!manager.ValidateConfiguration(out var issue))
            {
                Debug.LogError("Cannot create a connected main menu: " + issue);
                return false;
            }

            var cameraFound = false;
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>())
            {
                if (camera.gameObject.scene == arenaScene && camera.isActiveAndEnabled &&
                    camera.CompareTag("MainCamera"))
                {
                    cameraFound = true;
                    break;
                }
            }

            if (!cameraFound)
            {
                Debug.LogError("Cannot create a connected main menu: Arena has no active MainCamera.");
                return false;
            }

            return true;
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);

            if (openedHere && arenaScene.IsValid() && arenaScene.isLoaded)
                EditorSceneManager.CloseScene(arenaScene, true);
        }
    }

    private static Scene[] GetLoadedScenes()
    {
        var scenes = new Scene[SceneManager.sceneCount];
        for (var i = 0; i < scenes.Length; i++)
            scenes[i] = SceneManager.GetSceneAt(i);
        return scenes;
    }

    private static RoboClashMatchManager FindManagerInScene(Scene scene)
    {
        foreach (var manager in UnityEngine.Object.FindObjectsByType<RoboClashMatchManager>())
        {
            if (manager.gameObject.scene == scene)
                return manager;
        }

        return null;
    }

    private static void ConfigureBuildSceneOrder()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(ArenaScenePath, true)
        };

        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing.path == MenuScenePath || existing.path == ArenaScenePath)
                continue;

            if (existing.path == "Assets/Scenes/Arena.unity")
            {
                scenes.Add(new EditorBuildSettingsScene(existing.path, false));
                continue;
            }

            scenes.Add(existing);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
