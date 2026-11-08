using System;
using System.Collections.Generic;
using System.IO;
using RoboClash;
using RoboClash.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoboClashReleaseValidator
{
    private const string ExistingArenaPath = "Assets/Scene.unity";
    private const string GameplayScenePath = "Assets/Scenes/RoboClashGameplay_XL.unity";
    private const string MenuScenePath = "Assets/Scenes/RoboClashMainMenu.unity";
    private static readonly string WindowsBuildDirectory = Path.Combine("Builds", "Windows");
    private const string ArenaRootName = "RoboClash Arena";
    private const string LegacyArenaRootName = "RoboClash_Generated_Arena";

    [MenuItem("Tools/RoboClash/Prepare Gameplay Scene From Existing Arena")]
    public static void PrepareGameplaySceneFromExistingArena()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) != null ||
            File.Exists(GameplayScenePath))
        {
            Debug.LogError(
                $"Gameplay scene preparation stopped: '{GameplayScenePath}' already exists. " +
                "It was not overwritten.");
            return;
        }
        else if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            Debug.Log(
                "Unity switched to the Windows x64 target. Wait for the Editor to finish reloading, " +
                "then run Tools > RoboClash > Build Windows again.");
            return;
        }

        var prefabs = LoadRobotPrefabs();
        if (prefabs == null)
            return;

        if (!TryInspectExistingArena(out var issue))
        {
            Debug.LogError(
                $"Gameplay scene preparation stopped: {issue} The existing arena was not changed.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Prepare RoboClash Gameplay Scene",
                $"Copy the existing arena from '{ExistingArenaPath}' to the new scene " +
                $"'{GameplayScenePath}', add the existing match manager and HUD, and preserve both source scenes? " +
                "Nothing will be generated over the original arena.",
                "Create Copy",
                "Cancel"))
            return;

        var absoluteSource = Path.GetFullPath(ExistingArenaPath);
        var absoluteDestination = Path.GetFullPath(GameplayScenePath);
        var directory = Path.GetDirectoryName(absoluteDestination);
        var previousActiveScene = SceneManager.GetActiveScene();
        Scene copiedScene = default;
        var openedCopy = false;
        try
        {
            Directory.CreateDirectory(directory);
            File.Copy(absoluteSource, absoluteDestination, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            copiedScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            openedCopy = true;
            SceneManager.SetActiveScene(copiedScene);
            var arenaRoot = FindArenaRoot(copiedScene);
            if (arenaRoot == null)
            {
                Debug.LogError(
                    $"The copied scene '{GameplayScenePath}' has no generated arena root. " +
                    "The copied file was left intact for inspection; the source was not changed.");
                return;
            }

            var managers = FindInScene<RoboClashMatchManager>(copiedScene);
            if (managers.Count > 1)
            {
                Debug.LogError(
                    $"The copied scene already contains {managers.Count} match managers. " +
                    "No components were added.");
                return;
            }

            var huds = FindInScene<RoboClashHUD>(copiedScene);
            if (huds.Count > 1)
            {
                Debug.LogError($"The copied scene already contains {huds.Count} HUDs. No components were added.");
                return;
            }

            var manager = managers.Count == 1
                ? managers[0]
                : arenaRoot.AddComponent<RoboClashMatchManager>();
            manager.ConfigureRobotPrefabs(prefabs);
            if (huds.Count == 0)
                arenaRoot.AddComponent<RoboClashHUD>();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(copiedScene);

            if (!manager.ValidateConfiguration(out issue))
            {
                Debug.LogError(
                    $"The copied gameplay scene has an invalid match configuration: {issue} " +
                    $"The copy remains at '{GameplayScenePath}' for repair; the source scene is unchanged.");
                return;
            }

            if (!EditorSceneManager.SaveScene(copiedScene, GameplayScenePath))
            {
                Debug.LogError($"Unity could not save the prepared scene '{GameplayScenePath}'.");
                return;
            }

            Debug.Log(
                $"Prepared '{GameplayScenePath}' from the existing arena without modifying '{ExistingArenaPath}'. " +
                "Create the main menu with Tools > RoboClash > Create Main Menu UI, then run " +
                "Tools > RoboClash > Validate Release.");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath));
        }
        catch (IOException exception)
        {
            Debug.LogError($"Could not copy the existing arena scene: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Access to the gameplay scene path was denied: {exception.Message}");
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
            if (openedCopy && copiedScene.IsValid() && copiedScene.isLoaded)
                EditorSceneManager.CloseScene(copiedScene, true);
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/RoboClash/Validate Release")]
    public static void ValidateRelease()
    {
        var errors = 0;
        var warnings = 0;

        ValidateSceneAsset(MenuScenePath, "main menu", ref errors);
        ValidateSceneAsset(GameplayScenePath, "gameplay", ref errors);
        ValidateSceneContents(MenuScenePath, false, ref errors);
        ValidateSceneContents(GameplayScenePath, true, ref errors);
        ValidateRobotPrefabs(ref errors);
        ValidateBuildSceneOrder(ref errors);
        ValidateWindowsSettings(ref warnings);

        if (errors == 0)
            Debug.Log($"RoboClash release validation passed with {warnings} warning(s).");
        else
            Debug.LogError(
                $"RoboClash release validation failed with {errors} error(s) and {warnings} warning(s). " +
                "Fix the listed configuration issues before building.");
    }

    [MenuItem("Tools/RoboClash/Build Windows")]
    public static void BuildWindows()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogError("Windows build stopped because Unity is still compiling scripts.");
            return;
        }

        var errors = 0;
        var warnings = 0;
        ValidateSceneAsset(MenuScenePath, "main menu", ref errors);
        ValidateSceneAsset(GameplayScenePath, "gameplay", ref errors);
        ValidateSceneContents(MenuScenePath, false, ref errors);
        ValidateSceneContents(GameplayScenePath, true, ref errors);
        ValidateRobotPrefabs(ref errors);
        ValidateBuildSceneOrder(ref errors);
        ValidateWindowsSettings(ref warnings);
        if (errors > 0)
        {
            Debug.LogError($"Windows build stopped: release validation found {errors} error(s).");
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                Debug.LogError("Windows build stopped: Unity could not switch to the Windows x64 build target.");
                return;
            }

            Debug.Log(
                "Unity switched to the Windows x64 target. Wait for the Editor to finish reloading, " +
                "then run Tools > RoboClash > Build Windows again.");
            return;
        }

        var directory = Path.GetFullPath(WindowsBuildDirectory);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var executablePath = Path.Combine(directory, "RoboClash-" + timestamp + ".exe");
        if (File.Exists(executablePath))
        {
            Debug.LogError($"Windows build stopped to avoid overwriting '{executablePath}'.");
            return;
        }

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (IOException exception)
        {
            Debug.LogError($"Could not create the Windows build directory '{directory}': {exception.Message}");
            return;
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Access to the Windows build directory '{directory}' was denied: {exception.Message}");
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = new[] { MenuScenePath, GameplayScenePath },
            locationPathName = executablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded || !File.Exists(executablePath))
        {
            Debug.LogError(
                $"Windows build failed with result '{report.summary.result}'. " +
                $"Unity reported {report.summary.totalErrors} error(s). Output: '{executablePath}'.");
            return;
        }

        Debug.Log(
            $"RoboClash Windows x64 build succeeded: '{executablePath}' " +
            $"({report.summary.totalSize} bytes, {report.summary.totalTime}).");
    }

    private static bool TryInspectExistingArena(out string issue)
    {
        issue = null;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ExistingArenaPath) == null)
        {
            issue = $"Existing arena scene '{ExistingArenaPath}' could not be loaded.";
            return false;
        }

        var previousActiveScene = SceneManager.GetActiveScene();
        var sourceScene = SceneManager.GetSceneByPath(ExistingArenaPath);
        var openedHere = false;
        try
        {
            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(ExistingArenaPath, OpenSceneMode.Additive);
                openedHere = true;
            }
            else if (sourceScene.isDirty)
            {
                issue = $"'{ExistingArenaPath}' has unsaved changes. Save it before making a copy.";
                return false;
            }

            if (FindArenaRoot(sourceScene) == null)
            {
                issue = $"'{ExistingArenaPath}' does not contain the expected generated arena root.";
                return false;
            }

            if (CountInScene<ArenaFloor>(sourceScene) == 0 ||
                CountInScene<ArenaSpawnZone>(sourceScene) < 4)
            {
                issue = $"'{ExistingArenaPath}' must contain an arena floor and four spawn zones.";
                return false;
            }

            var cameraFound = false;
            foreach (var camera in FindInScene<Camera>(sourceScene))
            {
                if (camera.isActiveAndEnabled && camera.CompareTag("MainCamera"))
                {
                    cameraFound = true;
                    break;
                }
            }

            if (!cameraFound)
            {
                issue = $"'{ExistingArenaPath}' has no active MainCamera-tagged camera.";
                return false;
            }

            return true;
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
            if (openedHere && sourceScene.IsValid() && sourceScene.isLoaded)
                EditorSceneManager.CloseScene(sourceScene, true);
        }
    }

    private static GameObject[] LoadRobotPrefabs()
    {
        var prefabs = new GameObject[Enum.GetValues(typeof(RoboClashRobotType)).Length];
        foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
        {
            var path = $"Assets/RoboClash/Generated/Prefabs/{type.ToString().ToUpperInvariant()}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<RoboClashRobotDefinition>() == null)
            {
                Debug.LogError($"Gameplay preparation stopped: robot prefab or definition is missing at '{path}'.");
                return null;
            }

            prefabs[(int)type] = prefab;
        }

        return prefabs;
    }

    private static void ValidateSceneAsset(string path, string role, ref int errors)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            errors++;
            Debug.LogError(
                $"Release validation: {role} scene is missing at '{path}'. " +
                (role == "gameplay"
                    ? "Run Tools > RoboClash > Prepare Gameplay Scene From Existing Arena."
                    : "Run Tools > RoboClash > Create Main Menu UI."));
        }
    }

    private static void ValidateSceneContents(string path, bool isGameplay, ref int errors)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            return;

        var previousActiveScene = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(path);
        var openedHere = false;
        try
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                openedHere = true;
            }

            if (isGameplay)
            {
                var managers = FindInScene<RoboClashMatchManager>(scene);
                if (managers.Count != 1)
                {
                    errors++;
                    Debug.LogError(
                        $"Release validation: gameplay scene '{path}' must contain exactly one " +
                        $"RoboClashMatchManager; found {managers.Count}.");
                }
                else if (!managers[0].ValidateConfiguration(out var issue))
                {
                    errors++;
                    Debug.LogError($"Release validation: match manager configuration is invalid: {issue}", managers[0]);
                }

                var hudCount = CountInScene<RoboClashHUD>(scene);
                if (hudCount != 1)
                {
                    errors++;
                    Debug.LogError(
                        $"Release validation: gameplay scene '{path}' must contain exactly one HUD; found {hudCount}.");
                }

                if (CountInScene<ArenaFloor>(scene) == 0)
                {
                    errors++;
                    Debug.LogError($"Release validation: gameplay scene '{path}' has no ArenaFloor.");
                }

                var spawnCount = CountInScene<ArenaSpawnZone>(scene);
                if (spawnCount < 4)
                {
                    errors++;
                    Debug.LogError(
                        $"Release validation: gameplay scene '{path}' needs four spawn zones; found {spawnCount}.");
                }

                var mainCameraCount = 0;
                foreach (var camera in FindInScene<Camera>(scene))
                {
                    if (camera.isActiveAndEnabled && camera.CompareTag("MainCamera"))
                        mainCameraCount++;
                }

                if (mainCameraCount != 1)
                {
                    errors++;
                    Debug.LogError(
                        $"Release validation: gameplay scene '{path}' must contain exactly one active " +
                        $"MainCamera-tagged camera; found {mainCameraCount}.");
                }
            }
            else
            {
                var menuControllers = FindInScene<RoboClashMenuController>(scene);
                if (menuControllers.Count != 1)
                {
                    errors++;
                    Debug.LogError(
                        $"Release validation: menu scene '{path}' must contain exactly one " +
                        $"RoboClashMenuController; found {menuControllers.Count}.");
                }
                else
                {
                    ValidateMenuPrefabReferences(menuControllers[0], ref errors);
                }

                var menuMatchManagerCount = CountInScene<RoboClashMatchManager>(scene);
                var menuHudCount = CountInScene<RoboClashHUD>(scene);
                if (menuMatchManagerCount > 0 || menuHudCount > 0)
                {
                    errors++;
                    Debug.LogError(
                        $"Release validation: menu scene '{path}' must not contain gameplay match managers " +
                        $"or HUDs; found {menuMatchManagerCount} manager(s) and {menuHudCount} HUD(s).");
                }
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                    {
                        errors++;
                        Debug.LogError(
                            $"Release validation: scene '{path}' contains a missing script on '{root.name}'.",
                            root);
                        break;
                    }

                    if (TryFindMissingReference(component, out var propertyPath))
                    {
                        errors++;
                        Debug.LogError(
                            $"Release validation: scene '{path}' has a missing object reference at " +
                            $"'{component.GetType().Name}.{propertyPath}'.",
                            component);
                    }
                }
            }
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
            if (openedHere && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ValidateRobotPrefabs(ref int errors)
    {
        var expectedCount = Enum.GetValues(typeof(RoboClashRobotType)).Length;
        var found = 0;
        foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
        {
            var path = $"Assets/RoboClash/Generated/Prefabs/{type.ToString().ToUpperInvariant()}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                errors++;
                Debug.LogError($"Release validation: required robot prefab is missing at '{path}'.");
                continue;
            }

            found++;
            var definition = prefab.GetComponent<RoboClashRobotDefinition>();
            if (definition == null || definition.robotType != type)
            {
                errors++;
                Debug.LogError($"Release validation: '{path}' has a missing or mismatched robot definition.", prefab);
            }

            if (prefab.GetComponent<RobotArenaController>() == null ||
                prefab.GetComponent<CharacterController>() == null ||
                prefab.GetComponent<RoboClashRobotMovement>() == null ||
                prefab.GetComponent<RoboClashRobotCombat>() == null ||
                prefab.GetComponent<RoboClashRobotAbilities>() == null ||
                prefab.GetComponent<RoboClashRobotAI>() == null ||
                prefab.GetComponent<RoboClashRobotAudio>() == null ||
                prefab.GetComponents<ArenaDamageable>().Length != 1)
            {
                errors++;
                Debug.LogError($"Release validation: '{path}' is missing required gameplay components.", prefab);
            }

            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    errors++;
                    Debug.LogError($"Release validation: '{path}' contains a missing script.", prefab);
                    break;
                }

                if (TryFindMissingReference(component, out var propertyPath))
                {
                    errors++;
                    Debug.LogError(
                        $"Release validation: '{path}' has a missing object reference at " +
                        $"'{component.GetType().Name}.{propertyPath}'.",
                        prefab);
                }
            }
        }

        if (found == expectedCount)
            Debug.Log($"Release validation: all {expectedCount} robot prefabs are present.");
    }

    private static void ValidateBuildSceneOrder(ref int errors)
    {
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length < 2 || !scenes[0].enabled || scenes[0].path != MenuScenePath ||
            !scenes[1].enabled || scenes[1].path != GameplayScenePath)
        {
            errors++;
            Debug.LogError(
                "Release validation: Build Settings must start with the main menu, followed by the gameplay scene. " +
                "Create the menu with Tools > RoboClash > Create Main Menu UI after preparing gameplay.");
        }
    }

    private static void ValidateMenuPrefabReferences(RoboClashMenuController controller, ref int errors)
    {
        var prefabArray = new SerializedObject(controller).FindProperty("robotPrefabs");
        var expectedCount = Enum.GetValues(typeof(RoboClashRobotType)).Length;
        if (prefabArray == null || !prefabArray.isArray || prefabArray.arraySize != expectedCount)
        {
            errors++;
            Debug.LogError(
                $"Release validation: menu controller must reference all {expectedCount} robot prefabs.",
                controller);
            return;
        }

        foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
        {
            var expectedPath =
                $"Assets/RoboClash/Generated/Prefabs/{type.ToString().ToUpperInvariant()}.prefab";
            var expectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(expectedPath);
            if (prefabArray.GetArrayElementAtIndex((int)type).objectReferenceValue != expectedPrefab)
            {
                errors++;
                Debug.LogError(
                    $"Release validation: menu controller robot slot '{type}' is not assigned to '{expectedPath}'.",
                    controller);
            }
        }
    }

    private static void ValidateWindowsSettings(ref int warnings)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            warnings++;
            Debug.LogWarning(
                $"Release validation: active target is '{EditorUserBuildSettings.activeBuildTarget}'; " +
                "the Windows build command will switch to Windows x64.");
        }

        if (PlayerSettings.productName != "RoboClash")
            Debug.LogWarning($"Release validation: product name is '{PlayerSettings.productName}'.");
        if (string.IsNullOrWhiteSpace(PlayerSettings.companyName) ||
            PlayerSettings.companyName == "DefaultCompany")
        {
            warnings++;
            Debug.LogWarning("Release validation: set a real company name in Project Settings > Player before shipping.");
        }

        var identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
        if (identifier.IndexOf("com.Unity-Technologies", StringComparison.OrdinalIgnoreCase) >= 0 ||
            identifier.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            warnings++;
            Debug.LogWarning(
                $"Release validation: standalone application identifier '{identifier}' is still a Unity template value.");
        }
    }

    private static GameObject FindArenaRoot(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == ArenaRootName || root.name == LegacyArenaRootName)
                return root;
        }

        return null;
    }

    private static int CountInScene<T>(Scene scene) where T : Component
    {
        return FindInScene<T>(scene).Count;
    }

    private static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        var result = new List<T>();
        foreach (var component in UnityEngine.Object.FindObjectsByType<T>())
        {
            if (component.gameObject.scene == scene)
                result.Add(component);
        }

        return result;
    }

    private static bool TryFindMissingReference(Component component, out string propertyPath)
    {
        var serializedObject = new SerializedObject(component);
        var property = serializedObject.GetIterator();
        while (property.Next(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue == null && property.objectReferenceEntityIdValue != default)
            {
                propertyPath = property.propertyPath;
                return true;
            }
        }

        propertyPath = null;
        return false;
    }
}
