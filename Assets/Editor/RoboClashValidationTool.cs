using System;
using RoboClash;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoboClashValidationTool
{
    private const string GeneratedPrefabFolder = "Assets/RoboClash/Generated/Prefabs";

    [MenuItem("Tools/RoboClash/Validate Scene Setup")]
    public static void ValidateSceneSetup()
    {
        var scene = SceneManager.GetActiveScene();
        var errors = 0;
        var warnings = 0;
        var mainCameraCount = 0;

        foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>())
        {
            if (camera.gameObject.scene != scene || !camera.isActiveAndEnabled ||
                !camera.CompareTag("MainCamera"))
                continue;
            mainCameraCount++;
        }

        if (mainCameraCount == 0)
            AddError(ref errors, "No active MainCamera-tagged camera exists in the active scene.");
        else if (mainCameraCount > 1)
            AddError(ref errors, $"Found {mainCameraCount} active cameras tagged MainCamera; keep exactly one.");
        else
            Debug.Log("RoboClash validation: one active MainCamera is present.");

        var definitions = UnityEngine.Object.FindObjectsByType<RoboClashRobotDefinition>();
        var generatedRobotCount = 0;
        var activeRobotCount = 0;
        foreach (var definition in definitions)
        {
            if (definition.gameObject.scene != scene)
                continue;

            generatedRobotCount++;
            if (definition.isActiveAndEnabled && definition.gameObject.activeInHierarchy)
                activeRobotCount++;

            ValidateRobot(definition, ref errors);
        }

        if (generatedRobotCount == 0)
            AddWarning(ref warnings,
                "No RoboClashRobotDefinition instances are currently in the active scene; match-prefab robots are normally spawned at runtime.");
        else
            Debug.Log($"RoboClash validation: found {generatedRobotCount} generated robot instance(s), " +
                      $"{activeRobotCount} active.");

        var spawnZones = CountInScene<ArenaSpawnZone>(scene);
        if (spawnZones == 0)
            AddWarning(ref warnings, "No ArenaSpawnZone exists. Generate the arena before configuring a player.");
        else
            Debug.Log($"RoboClash validation: found {spawnZones} spawn zone(s).");

        RoboClashMatchManager matchManager = null;
        var matchManagerCount = 0;
        foreach (var candidate in UnityEngine.Object.FindObjectsByType<RoboClashMatchManager>())
        {
            if (candidate.gameObject.scene != scene)
                continue;

            matchManager = candidate;
            matchManagerCount++;
        }

        if (matchManagerCount == 0)
            AddWarning(ref warnings, "No RoboClashMatchManager exists. Regenerate the arena to add the match loop.");
        else if (matchManagerCount > 1)
            AddError(ref errors, $"Found {matchManagerCount} RoboClashMatchManager components in the active scene; expected one.");
        else if (!matchManager.ValidateConfiguration(out var matchSetupIssue))
            AddError(ref errors, "Match manager configuration is incomplete: " + matchSetupIssue, matchManager);
        else
        {
            Debug.Log("RoboClash validation: match manager and generated robot prefabs are configured.");
            if (spawnZones < matchManager.RequiredSpawnZoneCount)
                AddWarning(ref warnings,
                    $"Match needs {matchManager.RequiredSpawnZoneCount} spawn zones but found {spawnZones}; safe predefined fallback spawns will be used.");
        }

        var players = UnityEngine.Object.FindObjectsByType<RobotArenaController>();
        var activePlayerCount = 0;
        foreach (var player in players)
        {
            if (player.gameObject.scene != scene || !player.isActiveAndEnabled || !player.HasPlayerControl)
                continue;

            activePlayerCount++;
            if (player.PlayerCamera == null)
                AddError(ref errors, $"Player '{player.name}' has no assigned camera.", player);
            if (player.SpawnZone == null)
                AddError(ref errors, $"Player '{player.name}' has no assigned respawn spawn zone.", player);
        }

        if (activePlayerCount == 0)
            AddWarning(ref warnings, "No active player-controlled RobotArenaController is configured.");
        else if (activePlayerCount > 1)
            AddError(ref errors, $"Found {activePlayerCount} active player-controlled robot controllers; expected one.");

        ValidateArena(scene, ref warnings);
        ValidateGeneratedPrefabs(ref errors, ref warnings);

        Debug.Log($"RoboClash validation completed for scene '{scene.name}'. " +
                  $"Errors: {errors}; Warnings: {warnings}.");
    }

    private static void ValidateRobot(RoboClashRobotDefinition definition, ref int errors)
    {
        var robotName = definition.name;
        var robot = definition.gameObject;
        RequireComponent<CharacterController>(robot, robotName, ref errors);
        RequireComponent<RobotArenaController>(robot, robotName, ref errors);
        RequireComponent<ArenaDamageable>(robot, robotName, ref errors);
        RequireComponent<RoboClashRobotMovement>(robot, robotName, ref errors);
        RequireComponent<RoboClashRobotCombat>(robot, robotName, ref errors);
        RequireComponent<RoboClashRobotAbilities>(robot, robotName, ref errors);
        RequireComponent<RoboClashRobotAI>(robot, robotName, ref errors);
        RequireComponent<RoboClashRobotAnimator>(robot, robotName, ref errors);
        RequireComponent<RoboClashRobotAudio>(robot, robotName, ref errors);
        RequireComponent<RoboClashRobotVFX>(robot, robotName, ref errors);

        var damageableCount = robot.GetComponents<ArenaDamageable>().Length;
        if (damageableCount != 1)
            AddError(ref errors,
                $"Robot '{robotName}' has {damageableCount} ArenaDamageable components; expected one " +
                "(RobotArenaController already inherits ArenaDamageable).", robot);

        if (definition.modelRoot == null || definition.aimPivot == null ||
            definition.weaponMount == null || definition.muzzle == null)
            AddError(ref errors,
                $"Robot '{robotName}' is missing a model, aim pivot, weapon mount, or muzzle reference.", robot);

        if (definition.robotType == RoboClashRobotType.Titan && definition.rotatingBarrels == null)
            AddError(ref errors, $"TITAN '{robotName}' has no rotating-barrel reference.", robot);
        if (definition.robotType == RoboClashRobotType.Spark && definition.repairDrone == null)
            AddError(ref errors, $"SPARK '{robotName}' has no repair-drone reference.", robot);

        var components = robot.GetComponentsInChildren<Component>(true);
        foreach (var component in components)
        {
            if (component == null)
                AddError(ref errors, $"Robot '{robotName}' or one of its children has a missing script.", robot);
        }
    }

    private static void ValidateArena(Scene scene, ref int warnings)
    {
        var hasArenaRoot = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "RoboClash Arena" || root.name == "RoboClash_Generated_Arena")
            {
                hasArenaRoot = true;
                break;
            }
        }

        if (!hasArenaRoot)
        {
            AddWarning(ref warnings, "No generated arena root exists. Use Tools > RoboClash > Generate Arena.");
            return;
        }

        var floors = CountInScene<ArenaFloor>(scene);
        var hazards = CountInScene<ArenaHazard>(scene);
        var pickups = CountInScene<ArenaWeaponPickup>(scene) + CountInScene<ArenaShieldPickup>(scene);
        var shields = CountInScene<ArenaEnergyShield>(scene);
        var platforms = CountInScene<ArenaMovingPlatform>(scene);
        Debug.Log($"RoboClash validation: arena has {floors} floor(s), {hazards} hazard(s), " +
                  $"{pickups} pickup(s), {shields} energy shield(s), and {platforms} moving platform(s).");

        if (floors == 0)
            AddWarning(ref warnings, "The generated arena root has no ArenaFloor component.");

        foreach (var hazard in UnityEngine.Object.FindObjectsByType<ArenaHazard>())
        {
            if (hazard.gameObject.scene != scene)
                continue;

            var collider = hazard.GetComponent<Collider>();
            if (collider == null || !collider.isTrigger)
                AddWarning(ref warnings, $"Hazard '{hazard.name}' has no trigger collider.", hazard);
        }

        foreach (var lava in UnityEngine.Object.FindObjectsByType<LavaPit>())
        {
            if (lava.gameObject.scene != scene)
                continue;

            if (lava.transform.Find("Molten Channel Surface") != null)
                continue;

            foreach (var floor in UnityEngine.Object.FindObjectsByType<ArenaFloor>())
            {
                if (floor.gameObject.scene != scene)
                    continue;

                var floorCollider = floor.GetComponent<Collider>();
                var lavaCollider = lava.GetComponent<Collider>();
                if (floorCollider == null || lavaCollider == null)
                    continue;

                var floorBounds = floorCollider.bounds;
                var lavaBounds = lavaCollider.bounds;
                var overlapsHorizontally =
                    floorBounds.min.x <= lavaBounds.max.x && floorBounds.max.x >= lavaBounds.min.x &&
                    floorBounds.min.z <= lavaBounds.max.z && floorBounds.max.z >= lavaBounds.min.z;
                var floorSupportsLavaZone =
                    overlapsHorizontally &&
                    lavaBounds.min.y >= floorBounds.max.y &&
                    lavaBounds.min.y - floorBounds.max.y <= 0.1f;

                if (floorBounds.Intersects(lavaBounds) || floorSupportsLavaZone)
                {
                    AddWarning(ref warnings,
                        $"Lava trigger '{lava.name}' is over or intersects solid floor '{floor.name}'; it functions as a damage zone, not an open pit.",
                        lava);
                }
            }
        }
    }

    private static void ValidateGeneratedPrefabs(ref int errors, ref int warnings)
    {
        var robotTypes = (RoboClashRobotType[])Enum.GetValues(typeof(RoboClashRobotType));
        var loadedCount = 0;
        foreach (var type in robotTypes)
        {
            var path = $"{GeneratedPrefabFolder}/{type.ToString().ToUpperInvariant()}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                AddWarning(ref warnings, $"Generated robot prefab could not be loaded: {path}");
                continue;
            }

            loadedCount++;
            var definition = prefab.GetComponent<RoboClashRobotDefinition>();
            if (definition == null)
            {
                AddError(ref errors, $"Generated prefab has no RoboClashRobotDefinition: {path}");
                continue;
            }

            ValidateRobot(definition, ref errors);
            if (prefab.GetComponents<ArenaDamageable>().Length != 1)
                AddError(ref errors,
                    $"Generated prefab '{path}' has duplicate ArenaDamageable components. Regenerate the roster to apply the factory fix.",
                    prefab);

            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                    AddError(ref errors, $"Generated prefab '{path}' contains a missing script.", prefab);
            }
        }

        if (loadedCount == robotTypes.Length)
            Debug.Log($"RoboClash validation: all {loadedCount} generated robot prefab assets loaded.");
        else if (loadedCount == 0)
            AddWarning(ref warnings, "No generated robot prefab assets could be loaded.");
    }

    private static int CountInScene<T>(Scene scene) where T : Component
    {
        var count = 0;
        foreach (var component in UnityEngine.Object.FindObjectsByType<T>())
        {
            if (component.gameObject.scene == scene)
                count++;
        }

        return count;
    }

    private static void RequireComponent<T>(GameObject target, string robotName, ref int errors)
        where T : Component
    {
        if (target.GetComponent<T>() == null)
            AddError(ref errors, $"Robot '{robotName}' is missing {typeof(T).Name}.", target);
    }

    private static void AddError(ref int errors, string message, UnityEngine.Object context = null)
    {
        errors++;
        Debug.LogError("RoboClash validation: " + message, context);
    }

    private static void AddWarning(ref int warnings, string message, UnityEngine.Object context = null)
    {
        warnings++;
        Debug.LogWarning("RoboClash validation: " + message, context);
    }
}
