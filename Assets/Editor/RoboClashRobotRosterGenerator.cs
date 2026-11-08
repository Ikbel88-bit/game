using System;
using System.Collections.Generic;
using RoboClash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoboClashRobotRosterGenerator
{
    private const string RootName = "RoboClash_Robot_Roster";
    private const string GeneratedFolder = "Assets/RoboClash/Generated";
    private const string MaterialsFolder = GeneratedFolder + "/Materials";
    private const string PrefabsFolder = GeneratedFolder + "/Prefabs";
    private const string MeshesFolder = GeneratedFolder + "/Meshes";

    [MenuItem("Tools/RoboClash/Create Robot Roster")]
    public static void CreateRobotRoster()
    {
        var scene = SceneManager.GetActiveScene();
        var existingRoot = FindRoot(scene, RootName);
        var prefabPaths = new List<string>();
        foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
        {
            var path = GetPrefabPath(type);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                prefabPaths.Add(path);
        }

        if ((existingRoot != null || prefabPaths.Count > 0) &&
            !EditorUtility.DisplayDialog(
                "Create Robo Clash Robot Roster",
                "Replace the generated roster scene group and generated robot prefabs? " +
                "Other scene objects and assets will be preserved.",
                "Replace Roster",
                "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Robo Clash Robot Roster");
        try
        {
            EnsureFolders();
            var materials = CreateMaterials();
            var armorMesh = GetOrCreateArmorMesh();

            if (existingRoot != null)
                Undo.DestroyObjectImmediate(existingRoot);

            var rosterRoot = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(rosterRoot, "Create Robot Roster Root");
            rosterRoot.transform.position = new Vector3(0f, 0f, 23f);

            var index = 0;
            foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
            {
                var robot = RoboClashRobotFactory.CreateRobot(type, null,
                    materials[index, 0], materials[index, 1], materials[index, 2], materials[index, 3],
                    armorMesh, materials[index, 4]);
                robot.name = type.ToString().ToUpperInvariant();
                var prefabPath = GetPrefabPath(type);
                var prefab = PrefabUtility.SaveAsPrefabAsset(robot, prefabPath);
                UnityEngine.Object.DestroyImmediate(robot);

                if (prefab == null)
                    throw new InvalidOperationException("Unity could not save the generated prefab: " + prefabPath);

                var preview = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rosterRoot.transform);
                preview.name = type.ToString().ToUpperInvariant() + " Preview";
                preview.transform.localPosition = new Vector3((index - 1.5f) * 3f, 0f, 0f);
                preview.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                DisablePreviewGameplay(preview);
                index++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rosterRoot;
            Debug.Log(
                "Robo Clash robot roster created with beveled armor geometry, procedural animation, bounded VFX, and synthesized fallback sounds. " +
                "Four reusable robot prefabs are in Assets/RoboClash/Generated/Prefabs. Professional character meshes, textures, animation clips, and audio assets can replace these procedural placeholders.",
                rosterRoot);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    [MenuItem("Tools/RoboClash/Configure Selected Robot As Player")]
    private static void ConfigureSelectedRobotAsPlayer()
    {
        var scene = SceneManager.GetActiveScene();
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("Select a robot prefab instance in the active scene first.");
            return;
        }

        var player = Selection.activeGameObject.GetComponentInParent<RobotArenaController>();
        if (player == null)
        {
            Debug.LogError("The selected object is not part of a RoboClash robot prefab instance.");
            return;
        }

        Camera camera = null;
        var mainCameraCount = 0;
        foreach (var candidate in UnityEngine.Object.FindObjectsByType<Camera>())
        {
            if (candidate.gameObject.scene != scene || !candidate.isActiveAndEnabled ||
                !candidate.CompareTag("MainCamera"))
                continue;
            camera = candidate;
            mainCameraCount++;
        }

        if (mainCameraCount != 1 || camera == null)
        {
            Debug.LogError(mainCameraCount == 0
                ? "No active camera tagged MainCamera exists in the active scene."
                : "More than one active MainCamera exists. Keep one active player camera before configuring a robot.");
            return;
        }

        ArenaSpawnZone nearestZone = null;
        var nearestDistance = float.PositiveInfinity;
        foreach (var zone in UnityEngine.Object.FindObjectsByType<ArenaSpawnZone>())
        {
            if (zone.gameObject.scene != scene)
                continue;

            var distance = Vector3.Distance(player.transform.position, zone.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestZone = zone;
            }
        }

        if (nearestZone == null)
        {
            Debug.LogError("No ArenaSpawnZone exists in the active scene. Generate the arena before configuring a player robot.");
            return;
        }

        var otherPlayers = new List<RobotArenaController>();
        foreach (var candidate in UnityEngine.Object.FindObjectsByType<RobotArenaController>())
        {
            if (candidate.gameObject.scene == scene && candidate != player &&
                candidate.isActiveAndEnabled && candidate.HasPlayerControl)
                otherPlayers.Add(candidate);
        }

        if (otherPlayers.Count > 0 &&
            !EditorUtility.DisplayDialog(
                "Replace Active Player",
                "Another robot currently controls the player camera. Transfer player control to the selected robot?",
                "Transfer Control",
                "Cancel"))
            return;

        Undo.RecordObject(player, "Configure RoboClash Player");
        var movement = player.GetComponent<RoboClashRobotMovement>();
        var combat = player.GetComponent<RoboClashRobotCombat>();
        if (movement != null)
            Undo.RecordObject(movement, "Configure RoboClash Player Movement");
        if (combat != null)
            Undo.RecordObject(combat, "Configure RoboClash Player Combat");
        foreach (var previousPlayer in otherPlayers)
        {
            Undo.RecordObject(previousPlayer, "Release Previous RoboClash Player");
            var previousMovement = previousPlayer.GetComponent<RoboClashRobotMovement>();
            var previousCombat = previousPlayer.GetComponent<RoboClashRobotCombat>();
            if (previousMovement != null)
                Undo.RecordObject(previousMovement, "Release Previous RoboClash Movement");
            if (previousCombat != null)
                Undo.RecordObject(previousCombat, "Release Previous RoboClash Combat");
            previousPlayer.DisablePlayerControl();
        }

        player.Configure(camera, null, nearestZone);
        if (nearestZone != null)
        {
            Undo.RecordObject(player.transform, "Place RoboClash Player At Spawn");
            player.transform.SetPositionAndRotation(nearestZone.transform.position + Vector3.up * 0.35f,
                nearestZone.transform.rotation);
        }

        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        var definition = player.GetComponent<RoboClashRobotDefinition>();
        var playerName = definition != null
            ? definition.robotType.ToString()
            : player.name + " (legacy controller)";
        Debug.Log($"Configured {playerName} as the player robot.", player);
    }

    [MenuItem("Tools/RoboClash/Configure Selected Robot As Player", true)]
    private static bool CanConfigureSelectedRobotAsPlayer()
    {
        var player = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<RobotArenaController>()
            : null;
        return player != null && player.isActiveAndEnabled;
    }

    [MenuItem("Tools/RoboClash/Repair Generated Robot Damage Components")]
    public static void RepairGeneratedRobotDamageComponents()
    {
        if (!Application.isBatchMode &&
            !EditorUtility.DisplayDialog(
         "Repair Generated Robot Prefabs",
         "Remove only the redundant ArenaDamageable component from generated robot prefabs. " +
         "The RobotArenaController component already owns health, shield, death, and respawn.",
         "Repair Prefabs",
         "Cancel"))
            return;

        var repairedPrefabs = 0;
        var removedComponents = 0;
        foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
        {
            var path = GetPrefabPath(type);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
         Debug.LogWarning("Skipping missing generated robot prefab: " + path);
         continue;
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
         var controller = contents.GetComponent<RobotArenaController>();
         if (controller == null)
         {
             Debug.LogError("Cannot repair generated robot prefab without RobotArenaController: " + path);
             continue;
         }

         var removedFromPrefab = false;
         foreach (var damageable in contents.GetComponents<ArenaDamageable>())
         {
             if (damageable is RobotArenaController)
                 continue;

             UnityEngine.Object.DestroyImmediate(damageable, true);
             removedComponents++;
             removedFromPrefab = true;
         }

         if (removedFromPrefab)
         {
             var savedPrefab = PrefabUtility.SaveAsPrefabAsset(contents, path);
             if (savedPrefab == null)
                 throw new InvalidOperationException("Unity could not save the repaired prefab: " + path);
             repairedPrefabs++;
         }
            }
            finally
            {
         PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"RoboClash prefab repair complete. Updated {repairedPrefabs} prefab(s), " +
           $"removed {removedComponents} redundant ArenaDamageable component(s).");
    }

    private static GameObject FindRoot(Scene scene, string rootName)
    {
        foreach (var sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot.name == rootName)
                return sceneRoot;
        }

        return null;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "RoboClash");
        EnsureFolder("Assets/RoboClash", "Generated");
        EnsureFolder(GeneratedFolder, "Materials");
        EnsureFolder(GeneratedFolder, "Prefabs");
        EnsureFolder(GeneratedFolder, "Meshes");
    }

    private static void EnsureFolder(string parent, string child)
    {
        var folder = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static Material[,] CreateMaterials()
    {
        var materials = new Material[4, 5];
        var types = new[]
        {
            RoboClashRobotType.Volt, RoboClashRobotType.Titan,
            RoboClashRobotType.Shadow, RoboClashRobotType.Spark
        };

        for (var i = 0; i < types.Length; i++)
        {
            var definitionObject = new GameObject("Material Profile");
            var definition = definitionObject.AddComponent<RoboClashRobotDefinition>();
            definition.Configure(types[i]);
            materials[i, 0] = GetOrCreateMaterial(types[i] + "_Primary", definition.primaryColor, false);
            materials[i, 1] = GetOrCreateMaterial(types[i] + "_Secondary", definition.secondaryColor, false);
            materials[i, 2] = GetOrCreateMaterial(types[i] + "_Joints", definition.jointColor, false);
            materials[i, 3] = GetOrCreateMaterial(types[i] + "_Emission", definition.emissionColor, true);
            materials[i, 4] = GetOrCreateShimmerMaterial(types[i], definition.emissionColor);
            UnityEngine.Object.DestroyImmediate(definitionObject);
        }

        return materials;
    }

    private static Material GetOrCreateMaterial(string name, Color color, bool emissive)
    {
        var path = MaterialsFolder + "/RC_Roster_" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("No compatible URP Lit or Built-in Standard shader was found.");

        material = new Material(shader) { name = "RC_Roster_" + name };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0.62f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.72f);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 1.8f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material GetOrCreateShimmerMaterial(RoboClashRobotType type, Color color)
    {
        var path = MaterialsFolder + "/RC_Roster_" + type + "_Shimmer.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("No compatible URP Lit or Built-in Standard shader was found.");

        material = new Material(shader) { name = "RC_Roster_" + type + "_Shimmer" };
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.16f));
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", new Color(color.r, color.g, color.b, 0.16f));
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 3f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Mesh GetOrCreateArmorMesh()
    {
        const string path = MeshesFolder + "/RC_BeveledArmor.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null)
            return mesh;

        mesh = RoboClashRobotFactory.CreateArmorMesh();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static string GetPrefabPath(RoboClashRobotType type)
    {
        return PrefabsFolder + "/" + type.ToString().ToUpperInvariant() + ".prefab";
    }

    private static void DisablePreviewGameplay(GameObject preview)
    {
        var controller = preview.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        var player = preview.GetComponent<RobotArenaController>();
        if (player != null)
            player.enabled = false;

        var movement = preview.GetComponent<RoboClashRobotMovement>();
        if (movement != null)
            movement.enabled = false;

        var combat = preview.GetComponent<RoboClashRobotCombat>();
        if (combat != null)
            combat.enabled = false;

        var abilities = preview.GetComponent<RoboClashRobotAbilities>();
        if (abilities != null)
            abilities.enabled = false;

        var ai = preview.GetComponent<RoboClashRobotAI>();
        if (ai != null)
            ai.enabled = false;

        var animator = preview.GetComponent<RoboClashRobotAnimator>();
        if (animator != null)
            animator.enabled = false;

        var audio = preview.GetComponent<RoboClashRobotAudio>();
        if (audio != null)
            audio.enabled = false;

        var vfx = preview.GetComponent<RoboClashRobotVFX>();
        if (vfx != null)
            vfx.enabled = false;
    }
}
