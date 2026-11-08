using System;
using System.Collections.Generic;
using RoboClash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class RoboClashArenaSettings
{
    [Min(120f)] public float mapWidth = 120f;
    [Min(120f)] public float mapDepth = 120f;
    [Min(6f)] public float corridorWidth = 10f;
    [Min(24f)] public float centralArenaWidth = 36f;
    [Min(24f)] public float centralArenaDepth = 36f;
    [Min(4f)] public float wallHeight = 7f;
    [Min(0.4f)] public float wallThickness = 1f;
    public bool generateSideRooms = true;
    public bool generateCover = true;
    public bool generateHazards = true;
    public bool generateElevatedAreas = true;
    public bool generatePickups = true;
    public ArenaVisualDetailQuality visualDetailQuality = ArenaVisualDetailQuality.Balanced;
}

public enum ArenaVisualDetailQuality
{
    Performance,
    Balanced,
    HighDetail
}

public static class RoboClashArenaGenerator
{
    private const string RootName = "RoboClash Arena";
    private const string LegacyRootName = "RoboClash_Generated_Arena";
    private const string MenuScenePath = "Assets/Scenes/RoboClashMainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/RoboClashGameplay.unity";
    private const string GeneratedFolder = "Assets/Generated/RoboClash";
    private const string MaterialsFolder = GeneratedFolder + "/Materials";
    private const string MeshesFolder = GeneratedFolder + "/Meshes";
    private const string MechPrefabPath = "Assets/Robot&Pilot/Assets/Prefabs/Mech.prefab";

    private static Transform root;
    private static Material floorMaterial;
    private static Material steelMaterial;
    private static Material wallPanelMaterial;
    private static Material wallDarkPanelMaterial;
    private static Material edgeTrimMaterial;
    private static Material darkSteelMaterial;
    private static Material orangeMaterial;
    private static Material blueMaterial;
    private static Material cyanMaterial;
    private static Material redMaterial;
    private static Material purpleMaterial;
    private static Material yellowMaterial;
    private static Material healthMaterial;
    private static Material hazardStripeMaterial;
    private static Material wallAccentMaterial;
    private static Material lavaMaterial;
    private static Material shieldMaterial;
    private static Texture2D metalPanelAlbedo;
    private static Texture2D metalPanelNormal;
    private static Texture2D metalPanelMetallic;
    private static Texture2D hazardStripeAlbedo;
    private static Texture2D lavaAlbedo;
    private static Mesh spikeMesh;
    private static Material[] wallVisualMaterials;
    private static ArenaVisualDetailQuality activeDetailQuality = ArenaVisualDetailQuality.Balanced;
    private static readonly Dictionary<string, Mesh> wallVisualMeshCache = new Dictionary<string, Mesh>();
    private static readonly Dictionary<string, Mesh> floorVisualMeshCache = new Dictionary<string, Mesh>();

    private const int WallPanelSubmesh = 0;
    private const int WallDarkPanelSubmesh = 1;
    private const int WallTrimSubmesh = 2;
    private const int WallAccentSubmesh = 3;

    private sealed class WallVisualMeshBuilder
    {
        private readonly bool worldSpaceFloorUvs;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int>[] triangles =
        {
            new List<int>(),
            new List<int>(),
            new List<int>(),
            new List<int>()
        };

        public WallVisualMeshBuilder(bool worldSpaceFloorUvs = false)
        {
            this.worldSpaceFloorUvs = worldSpaceFloorUvs;
        }

        public void AddBox(Vector3 center, Vector3 size, int submesh)
        {
            if (size.x <= 0f || size.y <= 0f || size.z <= 0f)
                return;

            AddFace(center, size, Vector3.right, Vector3.back, Vector3.up, submesh);
            AddFace(center, size, Vector3.left, Vector3.forward, Vector3.up, submesh);
            AddFace(center, size, Vector3.up, Vector3.right, Vector3.back, submesh);
            AddFace(center, size, Vector3.down, Vector3.right, Vector3.forward, submesh);
            AddFace(center, size, Vector3.forward, Vector3.right, Vector3.up, submesh);
            AddFace(center, size, Vector3.back, Vector3.left, Vector3.up, submesh);
        }

        public Mesh Build(string meshName)
        {
            var mesh = new Mesh { name = meshName };
            if (vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = triangles.Length;
            for (var i = 0; i < triangles.Length; i++)
                mesh.SetTriangles(triangles[i], i, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddFace(
            Vector3 center, Vector3 size, Vector3 normal, Vector3 axisU, Vector3 axisV, int submesh)
        {
            var halfU = (Mathf.Abs(axisU.x) * size.x + Mathf.Abs(axisU.y) * size.y +
                         Mathf.Abs(axisU.z) * size.z) * 0.5f;
            var halfV = (Mathf.Abs(axisV.x) * size.x + Mathf.Abs(axisV.y) * size.y +
                         Mathf.Abs(axisV.z) * size.z) * 0.5f;
            var faceCenter = center + Vector3.Scale(normal, size) * 0.5f;
            var start = vertices.Count;
            vertices.Add(faceCenter - axisU * halfU - axisV * halfV);
            vertices.Add(faceCenter + axisU * halfU - axisV * halfV);
            vertices.Add(faceCenter + axisU * halfU + axisV * halfV);
            vertices.Add(faceCenter - axisU * halfU + axisV * halfV);
            for (var i = 0; i < 4; i++)
                normals.Add(normal);
            if (worldSpaceFloorUvs && Mathf.Abs(normal.y) > 0.9f)
            {
                uvs.Add(new Vector2(vertices[start].x, vertices[start].z));
                uvs.Add(new Vector2(vertices[start + 1].x, vertices[start + 1].z));
                uvs.Add(new Vector2(vertices[start + 2].x, vertices[start + 2].z));
                uvs.Add(new Vector2(vertices[start + 3].x, vertices[start + 3].z));
            }
            else
            {
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
            }
            triangles[submesh].Add(start);
            triangles[submesh].Add(start + 1);
            triangles[submesh].Add(start + 2);
            triangles[submesh].Add(start);
            triangles[submesh].Add(start + 2);
            triangles[submesh].Add(start + 3);
        }
    }

    private sealed class LargeArenaLayout
    {
        public RoboClashArenaSettings Settings { get; }
        public float HalfWidth { get; }
        public float HalfDepth { get; }
        public float CentralHalfWidth { get; }
        public float CentralHalfDepth { get; }
        public float RingX { get; }
        public float RingZ { get; }
        public float RoomWidthX { get; }
        public float RoomWidthZ { get; }
        public float RoomDepthX { get; }
        public float RoomDepthZ { get; }
        public float NorthRoomEdge => HalfDepth - RoomDepthZ - 6f;
        public float EastRoomEdge => HalfWidth - RoomDepthX - 6f;
        public Vector3 NorthRoom => new Vector3(0f, 0f, HalfDepth - RoomDepthZ * 0.5f - 6f);
        public Vector3 EastRoom => new Vector3(HalfWidth - RoomDepthX * 0.5f - 6f, 0f, 0f);
        public Vector3 SouthRoom => new Vector3(0f, 0f, -HalfDepth + RoomDepthZ * 0.5f + 6f);
        public Vector3 WestRoom => new Vector3(-HalfWidth + RoomDepthX * 0.5f + 6f, 0f, 0f);

        public LargeArenaLayout(RoboClashArenaSettings settings)
        {
            Settings = settings;
            HalfWidth = settings.mapWidth * 0.5f;
            HalfDepth = settings.mapDepth * 0.5f;
            CentralHalfWidth = settings.centralArenaWidth * 0.5f;
            CentralHalfDepth = settings.centralArenaDepth * 0.5f;
            RingX = CentralHalfWidth + settings.corridorWidth + 2f;
            RingZ = CentralHalfDepth + settings.corridorWidth + 2f;
            RoomDepthX = Mathf.Clamp(settings.mapWidth * 0.2f, 24f, 80f);
            RoomDepthZ = Mathf.Clamp(settings.mapDepth * 0.2f, 24f, 80f);
            RoomWidthX = Mathf.Clamp(settings.mapWidth * 0.2333333f, 28f, 88f);
            RoomWidthZ = Mathf.Clamp(settings.mapDepth * 0.2333333f, 28f, 88f);
        }
    }

    [MenuItem("Tools/RoboClash/Validate Large Arena")]
    public static void ValidateLargeArena()
    {
        var scene = SceneManager.GetActiveScene();
        var arenaRoots = new List<GameObject>();
        foreach (var sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot.name == RootName || sceneRoot.name == LegacyRootName)
                arenaRoots.Add(sceneRoot);
        }

        var errors = 0;
        if (arenaRoots.Count != 1)
        {
            Debug.LogError($"Large arena validation failed: expected one '{RootName}' root, found {arenaRoots.Count}.");
            return;
        }

        var arena = arenaRoots[0];
        if (arena.name != RootName)
        {
            Debug.LogError("Large arena validation failed: the active generated root uses the legacy compact-arena name.", arena);
            errors++;
        }

        var floor = arena.GetComponentInChildren<ArenaFloor>(true);
        var floorCollider = floor != null ? floor.GetComponent<Collider>() : null;
        if (floor == null || floorCollider == null)
        {
            Debug.LogError("Large arena validation failed: continuous ArenaFloor or its collider is missing.", arena);
            errors++;
        }

        var roomNames = new[] { "ARMORY", "HAZARD CHAMBER", "WORKSHOP", "POWER CORE" };
        foreach (var roomName in roomNames)
        {
            var room = arena.transform.Find(roomName);
            if (room == null)
            {
                Debug.LogError($"Large arena validation failed: required room '{roomName}' is missing.", arena);
                errors++;
                continue;
            }

            if (room.Find(roomName + " Entry Header") == null)
            {
                Debug.LogError($"Large arena validation failed: '{roomName}' doorway/header geometry is missing.", room);
                errors++;
            }

            var roomFloor = room.Find(roomName + " Floor Inlay");
            if (roomFloor == null)
            {
                Debug.LogError($"Large arena validation failed: '{roomName}' has no visible room floor marking.", room);
                errors++;
            }
            else if (floorCollider != null)
            {
                var roomFloorRenderer = roomFloor.GetComponent<Renderer>();
                if (roomFloorRenderer != null &&
                    !HorizontalBoundsFitWithin(roomFloorRenderer.bounds, floorCollider.bounds))
                {
                    Debug.LogError($"Room '{roomName}' extends outside the continuous arena floor.", room);
                    errors++;
                }
            }
        }

        var routeFloorNames = new[]
        {
            "North Ring Floor", "East Ring Floor", "South Ring Floor", "West Ring Floor",
            "North Connector Floor", "East Connector Floor", "South Connector Floor", "West Connector Floor"
        };
        foreach (var routeFloorName in routeFloorNames)
        {
            var routeFloor = arena.transform.Find(routeFloorName);
            var routeRenderer = routeFloor != null ? routeFloor.GetComponent<Renderer>() : null;
            if (routeRenderer == null)
            {
                Debug.LogError($"Large arena validation failed: route floor '{routeFloorName}' is missing.", arena);
                errors++;
            }
            else if (floorCollider != null &&
                     !HorizontalBoundsFitWithin(routeRenderer.bounds, floorCollider.bounds))
            {
                Debug.LogError($"Route floor '{routeFloorName}' extends outside the continuous arena floor.", routeFloor);
                errors++;
            }
        }

        var spawnZones = arena.GetComponentsInChildren<ArenaSpawnZone>(true);
        var spawnIndices = new HashSet<int>();
        if (spawnZones.Length != 4)
        {
            Debug.LogError($"Large arena validation failed: expected four spawn zones, found {spawnZones.Length}.", arena);
            errors++;
        }

        foreach (var spawn in spawnZones)
        {
            if (spawnIndices.Add(spawn.SpawnIndex) == false || spawn.SpawnIndex < 0 || spawn.SpawnIndex > 3)
            {
                Debug.LogError($"Spawn zone '{spawn.name}' has a duplicate or out-of-range index.", spawn);
                errors++;
            }

            if (floorCollider != null && !floorCollider.bounds.Contains(spawn.transform.position))
            {
                Debug.LogError($"Spawn zone '{spawn.name}' is outside the continuous floor bounds.", spawn);
                errors++;
            }

            foreach (var hazard in arena.GetComponentsInChildren<ArenaHazard>(true))
            {
                var hazardCollider = hazard.GetComponent<Collider>();
                if (hazardCollider != null && hazardCollider.bounds.Contains(spawn.transform.position))
                {
                    Debug.LogError($"Spawn zone '{spawn.name}' overlaps hazard '{hazard.name}'.", spawn);
                    errors++;
                }
            }
        }

        var lavaSegments = arena.GetComponentsInChildren<LavaPit>(true);
        var hazardRoom = arena.transform.Find("HAZARD CHAMBER");
        if (hazardRoom != null)
        {
            var hazardFloor = hazardRoom.Find("HAZARD CHAMBER Floor Inlay");
            var floorRenderer = hazardFloor != null ? hazardFloor.GetComponent<Renderer>() : null;
            if (floorRenderer != null)
            {
                var floorArea = floorRenderer.bounds.size.x * floorRenderer.bounds.size.z;
                var hazardArea = 0f;
                foreach (var hazard in hazardRoom.GetComponentsInChildren<ArenaHazard>(true))
                {
                    var hazardCollider = hazard.GetComponent<Collider>();
                    if (hazardCollider != null)
                        hazardArea += hazardCollider.bounds.size.x * hazardCollider.bounds.size.z;
                }

                if (floorArea <= 0.01f || hazardArea >= floorArea * 0.8f)
                {
                    Debug.LogError("Hazard Chamber does not retain a sufficiently large non-hazard floor area.", hazardRoom);
                    errors++;
                }
            }
            else if (lavaSegments.Length > 0)
            {
                Debug.LogError("Hazard Chamber has hazards but no measurable room floor marking.", hazardRoom);
                errors++;
            }
        }

        foreach (var lava in lavaSegments)
        {
            var collider = lava.GetComponent<BoxCollider>();
            var surface = lava.transform.Find("Molten Channel Surface");
            if (collider == null || !collider.isTrigger || surface == null)
            {
                Debug.LogError($"Lava segment '{lava.name}' is missing its trigger or visible surface.", lava);
                errors++;
                continue;
            }

            var surfaceRenderer = surface.GetComponent<Renderer>();
            if (surfaceRenderer == null)
            {
                Debug.LogError($"Lava segment '{lava.name}' has no visible surface renderer.", lava);
                errors++;
                continue;
            }

            var visualBounds = surfaceRenderer.bounds;
            var triggerBounds = collider.bounds;
            if (Mathf.Abs(visualBounds.min.x - triggerBounds.min.x) > 0.15f ||
                Mathf.Abs(visualBounds.max.x - triggerBounds.max.x) > 0.15f ||
                Mathf.Abs(visualBounds.min.z - triggerBounds.min.z) > 0.15f ||
                Mathf.Abs(visualBounds.max.z - triggerBounds.max.z) > 0.15f)
            {
                Debug.LogError($"Lava segment '{lava.name}' visual and damage trigger bounds do not align.", lava);
                errors++;
            }
        }

        var bridgeCount = 0;
        var safeCrossingColliders = new List<Collider>();
        foreach (var barrier in arena.GetComponentsInChildren<MetalBarrier>(true))
        {
            var isBridge = barrier.name.Contains("Hazard Channel Bridge");
            var isAccessRamp = barrier.name.Contains("Hazard Bridge Access Ramp");
            if (!isBridge && !isAccessRamp)
                continue;

            var bridgeCollider = barrier.GetComponent<Collider>();
            if (bridgeCollider == null || bridgeCollider.isTrigger)
            {
                Debug.LogError($"Hazard crossing '{barrier.name}' needs a solid collider.", barrier);
                errors++;
                continue;
            }

            if (Mathf.Min(bridgeCollider.bounds.size.x, bridgeCollider.bounds.size.z) < 2f)
            {
                Debug.LogError($"Hazard crossing '{barrier.name}' is too narrow for normal robot movement.", barrier);
                errors++;
            }

            safeCrossingColliders.Add(bridgeCollider);
            if (isBridge)
                bridgeCount++;
        }

        if (arena.transform.Find("HAZARD CHAMBER") != null && lavaSegments.Length > 0 && bridgeCount < 2)
        {
            Debug.LogError($"Hazard Chamber has {bridgeCount} collidable bridges; expected at least two.", arena);
            errors++;
        }

        foreach (var lava in lavaSegments)
        {
            var lavaCollider = lava.GetComponent<Collider>();
            if (lavaCollider == null)
                continue;

            foreach (var crossingCollider in safeCrossingColliders)
            {
                if (!lavaCollider.bounds.Intersects(crossingCollider.bounds))
                    continue;

                Debug.LogError($"Safe crossing '{crossingCollider.name}' overlaps lava trigger '{lava.name}'.",
                    crossingCollider);
                errors++;
            }
        }

        foreach (var pickup in arena.GetComponentsInChildren<ArenaWeaponPickup>(true))
            ValidatePickupClearOfHazards(pickup.transform, pickup.GetComponent<Collider>(), arena, ref errors);
        foreach (var pickup in arena.GetComponentsInChildren<ArenaShieldPickup>(true))
            ValidatePickupClearOfHazards(pickup.transform, pickup.GetComponent<Collider>(), arena, ref errors);

        if (errors == 0)
            Debug.Log($"Large arena validation passed: four rooms, {spawnZones.Length} indexed spawns, " +
                      $"{lavaSegments.Length} aligned lava segments, and {bridgeCount} solid hazard bridges. " +
                      "This checks scene geometry and bounds, not full pathfinding.", arena);
        else
            Debug.LogError($"Large arena validation found {errors} issue(s).", arena);
    }

    private static void ValidatePickupClearOfHazards(
        Transform pickup, Collider pickupCollider, GameObject arena, ref int errors)
    {
        if (pickupCollider == null)
        {
            Debug.LogError($"Pickup '{pickup.name}' has no collider.", pickup);
            errors++;
            return;
        }

        foreach (var hazard in arena.GetComponentsInChildren<ArenaHazard>(true))
        {
            var hazardCollider = hazard.GetComponent<Collider>();
            if (hazardCollider != null && hazardCollider.bounds.Intersects(pickupCollider.bounds))
            {
                Debug.LogError($"Pickup '{pickup.name}' overlaps hazard '{hazard.name}'.", pickup);
                errors++;
            }
        }
    }

    private static bool HorizontalBoundsFitWithin(Bounds bounds, Bounds container)
    {
        const float tolerance = 0.05f;
        return bounds.min.x >= container.min.x - tolerance &&
               bounds.max.x <= container.max.x + tolerance &&
               bounds.min.z >= container.min.z - tolerance &&
               bounds.max.z <= container.max.z + tolerance;
    }

    [MenuItem("Tools/RoboClash/Generate Large Arena")]
    public static void OpenLargeArenaSettings()
    {
        RoboClashLargeArenaWindow.ShowWindow();
    }

    [MenuItem("Tools/RoboClash/Generate Arena")]
    public static void GenerateArena()
    {
        if (!TryGetGameplaySceneForGeneration(out var scene))
            return;

        var oldArenas = FindGeneratedRoots(scene);
        if (oldArenas.Count > 0 &&
            !EditorUtility.DisplayDialog(
                "Regenerate Robo Clash Arena",
                "Replace the existing generated arena? Unrelated scene objects will be preserved.",
                "Regenerate",
                "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate Robo Clash Arena");

        try
        {
            EnsureAssetFolders();
            LoadOrCreateAssets();
            activeDetailQuality = ArenaVisualDetailQuality.Balanced;

            foreach (var oldArena in oldArenas)
                Undo.DestroyObjectImmediate(oldArena);

            var arena = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(arena, "Create Robo Clash Arena");
            root = arena.transform;

            BuildArena();
            ConfigureGeneratedMatchForGameplayScene(arena, scene);

            Selection.activeGameObject = arena;
            EditorSceneManager.MarkSceneDirty(scene);
            ConfigureMainMenuPlayModeStartScene();
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Robo Clash arena generated in the gameplay scene. Save the scene; Unity Play Mode is configured to start from the main menu.",
                arena);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    public static void GenerateLargeArena(RoboClashArenaSettings settings)
    {
        if (settings == null)
        {
            Debug.LogError("Large arena generation stopped: settings are missing.");
            return;
        }

        if (!TryGetGameplaySceneForGeneration(out var scene))
            return;

        settings.mapWidth = Mathf.Max(120f, settings.mapWidth);
        settings.mapDepth = Mathf.Max(120f, settings.mapDepth);
        settings.corridorWidth = Mathf.Clamp(settings.corridorWidth, 6f, 14f);
        var roomDepthX = Mathf.Clamp(settings.mapWidth * 0.2f, 24f, 80f);
        var roomDepthZ = Mathf.Clamp(settings.mapDepth * 0.2f, 24f, 80f);
        var maxCentralWidth = Mathf.Max(24f, Mathf.Min(settings.mapWidth * 0.45f,
            settings.mapWidth - 16f - 2f * roomDepthX - 2f * settings.corridorWidth));
        var maxCentralDepth = Mathf.Max(24f, Mathf.Min(settings.mapDepth * 0.45f,
            settings.mapDepth - 16f - 2f * roomDepthZ - 2f * settings.corridorWidth));
        settings.centralArenaWidth = Mathf.Clamp(settings.centralArenaWidth, 24f, maxCentralWidth);
        settings.centralArenaDepth = Mathf.Clamp(settings.centralArenaDepth, 24f, maxCentralDepth);
        settings.wallHeight = Mathf.Max(4f, settings.wallHeight);
        settings.wallThickness = Mathf.Clamp(settings.wallThickness, 0.4f,
            Mathf.Min(settings.mapWidth, settings.mapDepth) * 0.05f);

        var oldArenas = FindGeneratedRoots(scene);
        if (oldArenas.Count > 0 &&
            !EditorUtility.DisplayDialog(
                "Generate Large RoboClash Arena",
                $"Replace the {oldArenas.Count} generated arena root(s) in the active scene with a " +
                $"{settings.mapWidth:0} x {settings.mapDepth:0} multi-room arena? Only those generated roots will be removed.",
                "Generate",
                "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate Large RoboClash Arena");
        try
        {
            EnsureAssetFolders();
            LoadOrCreateAssets();
            activeDetailQuality = settings.visualDetailQuality;
            foreach (var oldArena in oldArenas)
                Undo.DestroyObjectImmediate(oldArena);

            var arena = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(arena, "Create Large RoboClash Arena");
            root = arena.transform;
            BuildLargeArenaLayout(settings);
            ConfigureGeneratedMatchForGameplayScene(arena, scene);

            Selection.activeGameObject = arena;
            EditorSceneManager.MarkSceneDirty(scene);
            ConfigureMainMenuPlayModeStartScene();
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Generated a {settings.mapWidth:0} x {settings.mapDepth:0} connected RoboClash combat complex in the gameplay scene. " +
                "Save the scene; Unity Play Mode is configured to start from the main menu.",
                arena);
        }
        catch (Exception exception)
        {
            Debug.LogError("Large RoboClash arena generation failed: " + exception.Message);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static bool ConfigureGeneratedMatchForGameplayScene(GameObject arena, Scene scene)
    {
        if (!IsGameplayScene(scene) || arena == null || arena.scene != scene)
        {
            Debug.LogError(
                $"Refusing to add RoboClash gameplay systems to '{scene.path}'. " +
                $"Generate gameplay systems only in '{GameplayScenePath}'.",
                arena);
            return false;
        }

        ConfigureGeneratedMatch(arena);
        return true;
    }

    private static void ConfigureGeneratedMatch(GameObject arena)
    {
        var matchManager = arena.AddComponent<RoboClashMatchManager>();
        arena.AddComponent<RoboClash.UI.RoboClashHUD>();
        var robotPrefabs = new GameObject[Enum.GetValues(typeof(RoboClashRobotType)).Length];
        foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
            robotPrefabs[(int)type] = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RoboClash/Generated/Prefabs/" + type.ToString().ToUpperInvariant() + ".prefab");
        matchManager.ConfigureRobotPrefabs(robotPrefabs);
        if (!matchManager.ValidateConfiguration(out var issue))
            Debug.LogWarning(
                "RoboClash arena was generated without a ready match setup: " + issue +
                " Repair the robot prefabs, then run Tools > RoboClash > Validate Scene Setup.", arena);
    }

    private static bool IsGameplayScene(Scene scene)
    {
        return string.Equals(scene.path, GameplayScenePath, StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureMainMenuPlayModeStartScene()
    {
        var menuSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
        if (menuSceneAsset == null)
        {
            Debug.LogWarning(
                $"Main menu scene '{MenuScenePath}' was not found. Play Mode will start from the currently active scene.");
            return;
        }

        EditorSceneManager.playModeStartScene = menuSceneAsset;
    }

    private static bool TryGetGameplaySceneForGeneration(out Scene gameplayScene)
    {
        gameplayScene = default;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Stop Play Mode before generating or replacing arena scene content.");
            return false;
        }

        var previousActiveScene = SceneManager.GetActiveScene();
        if (IsGameplayScene(previousActiveScene))
        {
            gameplayScene = previousActiveScene;
            return true;
        }

        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var loadedScene = SceneManager.GetSceneAt(i);
            if (IsGameplayScene(loadedScene))
            {
                gameplayScene = loadedScene;
                break;
            }
        }

        if (!gameplayScene.IsValid())
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) == null)
            {
                Debug.LogError(
                    $"Cannot generate an arena because gameplay scene '{GameplayScenePath}' was not found.");
                return false;
            }

            try
            {
                gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not open gameplay scene '{GameplayScenePath}' for arena generation: {exception.Message}");
                return false;
            }
        }

        if (!gameplayScene.IsValid() || !gameplayScene.isLoaded ||
            !SceneManager.SetActiveScene(gameplayScene))
        {
            Debug.LogError(
                $"Could not make gameplay scene '{GameplayScenePath}' active for arena generation.");
            return false;
        }

        Debug.Log(
            $"Arena generation target set to '{GameplayScenePath}'. Other open scenes were left open and unchanged.");
        return true;
    }

    private static List<GameObject> FindGeneratedRoots(Scene scene)
    {
        var roots = new List<GameObject>();
        foreach (var sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot.name == RootName || sceneRoot.name == LegacyRootName)
                roots.Add(sceneRoot);
        }

        return roots;
    }

    private static void EnsureAssetFolders()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder("Assets/Generated", "RoboClash");
        EnsureFolder(GeneratedFolder, "Materials");
        EnsureFolder(GeneratedFolder, "Meshes");
    }

    private static void EnsureFolder(string parent, string folder)
    {
        var path = parent + "/" + folder;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folder);
    }

    private static void LoadOrCreateAssets()
    {
        metalPanelAlbedo = GetOrCreateMetalPanelTexture();
        metalPanelNormal = GetOrCreateMetalPanelNormal();
        metalPanelMetallic = GetOrCreateMetalPanelMetallic();
        hazardStripeAlbedo = GetOrCreateHazardStripeTexture();
        lavaAlbedo = GetOrCreateLavaTexture();

        floorMaterial = GetOrCreateMaterial("ArenaFloor", new Color(0.72f, 0.78f, 0.82f),
            0.78f, 0.72f, baseTexture: metalPanelAlbedo, normalMap: metalPanelNormal,
            metallicMap: metalPanelMetallic, textureScale: new Vector2(6f, 6f));
        steelMaterial = GetOrCreateMaterial("BrushedSteel", new Color(0.76f, 0.8f, 0.82f),
            0.86f, 0.78f, baseTexture: metalPanelAlbedo, normalMap: metalPanelNormal,
            metallicMap: metalPanelMetallic, textureScale: new Vector2(1.5f, 1.5f));
        wallPanelMaterial = GetOrCreateMaterial("WallPanelSteel", new Color(0.82f, 0.88f, 0.9f),
            0.78f, 0.68f, baseTexture: metalPanelAlbedo, normalMap: metalPanelNormal,
            metallicMap: metalPanelMetallic, textureScale: new Vector2(1.25f, 1.25f));
        wallDarkPanelMaterial = GetOrCreateMaterial("WallPanelGunmetal", new Color(0.58f, 0.65f, 0.69f),
            0.82f, 0.72f, baseTexture: metalPanelAlbedo, normalMap: metalPanelNormal,
            metallicMap: metalPanelMetallic, textureScale: new Vector2(1.25f, 1.25f));
        edgeTrimMaterial = GetOrCreateMaterial("WallEdgeTrim", new Color(0.72f, 0.8f, 0.83f),
            0.84f, 0.76f, baseTexture: metalPanelAlbedo, normalMap: metalPanelNormal,
            metallicMap: metalPanelMetallic, textureScale: Vector2.one);
        darkSteelMaterial = GetOrCreateMaterial("Gunmetal", new Color(0.16f, 0.2f, 0.22f),
            0.82f, 0.74f, baseTexture: metalPanelAlbedo, normalMap: metalPanelNormal,
            metallicMap: metalPanelMetallic, textureScale: new Vector2(1.5f, 1.5f));
        orangeMaterial = GetOrCreateMaterial("WarningAmber", new Color(1f, 0.27f, 0.035f), 0.35f, 0.55f, 2.5f);
        blueMaterial = GetOrCreateMaterial("PlayerBlue", new Color(0.025f, 0.3f, 1f), 0.2f, 0.4f, 2.2f);
        cyanMaterial = GetOrCreateMaterial("NeonCyan", new Color(0.03f, 0.75f, 1f), 0.2f, 0.4f, 2.2f);
        redMaterial = GetOrCreateMaterial("PlayerRed", new Color(1f, 0.045f, 0.025f), 0.2f, 0.4f, 2.2f);
        purpleMaterial = GetOrCreateMaterial("RoomAccentPurple", new Color(0.42f, 0.16f, 0.9f), 0.3f, 0.5f, 1.4f);
        wallAccentMaterial = GetOrCreateMaterial("WallSignalCyan", new Color(0.025f, 0.42f, 0.58f), 0.25f, 0.45f, 1.5f);
        yellowMaterial = GetOrCreateMaterial("HazardYellow", new Color(1f, 0.65f, 0.025f), 0.3f, 0.45f, 2f);
        healthMaterial = GetOrCreateMaterial("HealthPickupGreen", new Color(0.08f, 1f, 0.28f), 0.2f, 0.35f, 2.4f);
        hazardStripeMaterial = GetOrCreateMaterial("HazardStripeSteel", Color.white, 0.62f, 0.58f,
            baseTexture: hazardStripeAlbedo, normalMap: metalPanelNormal,
            metallicMap: metalPanelMetallic, textureScale: new Vector2(2f, 1f));
        lavaMaterial = GetOrCreateMaterial("MoltenLava", Color.white, 0.08f, 0.3f, 2.4f,
            baseTexture: lavaAlbedo, emissionMap: lavaAlbedo,
            textureScale: new Vector2(2f, 1f));
        shieldMaterial = GetOrCreateMaterial("EnergyShield", new Color(0.05f, 0.65f, 1f, 0.2f), 0f, 0.4f, 1.2f);
        wallVisualMaterials = new[]
        {
            wallPanelMaterial,
            wallDarkPanelMaterial,
            edgeTrimMaterial,
            wallAccentMaterial
        };
        spikeMesh = GetOrCreateSpikeMesh();
    }

    private static Texture2D GetOrCreateMetalPanelTexture()
    {
        return GetOrCreateTexture("ArenaMetalPanelTileV2", 256, false, (x, y) =>
        {
            var localX = x % 128;
            var localY = y % 128;
            var edge = localX < 3 || localX > 124 || localY < 3 || localY > 124;
            var seam = localX == 5 || localX == 6 || localX == 121 || localX == 122 ||
                       localY == 5 || localY == 6 || localY == 121 || localY == 122;
            var noise = ((x * 17 + y * 29 + x * y * 3) % 11) - 5;
            var value = Mathf.Clamp(166 + noise, 0, 255);
            var color = edge
                ? new Color32(54, 65, 72, 255)
                : seam
                    ? new Color32(94, 107, 115, 255)
                    : new Color32((byte)value, (byte)Mathf.Min(255, value + 8),
                        (byte)Mathf.Min(255, value + 13), 255);

            if (IsPanelFastenerPixel(localX, localY))
                color = new Color32(52, 61, 66, 255);
            else if ((x * 13 + y * 31 + x * y * 7) % 1601 < 4)
                color = new Color32(106, 118, 123, 255);
            return color;
        });
    }

    private static Texture2D GetOrCreateMetalPanelNormal()
    {
        const int resolution = 256;
        var heights = new float[resolution * resolution];
        for (var y = 0; y < resolution; y++)
        {
            for (var x = 0; x < resolution; x++)
            {
                var px = x % 128;
                var py = y % 128;
                var edge = px < 3 || px > 124 || py < 3 || py > 124;
                var seam = px == 5 || px == 6 || px == 121 || px == 122 ||
                           py == 5 || py == 6 || py == 121 || py == 122;
                heights[y * resolution + x] = edge || seam ? 0.22f :
                    IsPanelFastenerPixel(px, py) ? 0.78f :
                    0.5f + ((x * 11 + y * 23 + x * y) % 17) * 0.002f;
            }
        }

        return GetOrCreateTexture("ArenaMetalPanelNormalV2", resolution, true, (x, y) =>
        {
            var left = heights[y * resolution + (x + resolution - 1) % resolution];
            var right = heights[y * resolution + (x + 1) % resolution];
            var down = heights[((y + resolution - 1) % resolution) * resolution + x];
            var up = heights[((y + 1) % resolution) * resolution + x];
            var normal = new Vector3((left - right) * 2.2f, (down - up) * 2.2f, 1f).normalized;
            return new Color32(
                (byte)Mathf.RoundToInt((normal.x * 0.5f + 0.5f) * 255f),
                (byte)Mathf.RoundToInt((normal.y * 0.5f + 0.5f) * 255f),
                (byte)Mathf.RoundToInt((normal.z * 0.5f + 0.5f) * 255f), 255);
        });
    }

    private static Texture2D GetOrCreateMetalPanelMetallic()
    {
        return GetOrCreateTexture("ArenaMetalPanelMetallicV2", 256, true, (x, y) =>
        {
            var px = x % 128;
            var py = y % 128;
            var seam = px < 3 || px > 124 || py < 3 || py > 124 ||
                       px == 5 || px == 6 || px == 121 || px == 122 ||
                       py == 5 || py == 6 || py == 121 || py == 122;
            var metal = seam ? 92 : 176 + (x * 7 + y * 13) % 44;
            var smoothness = IsPanelFastenerPixel(px, py) ? 232 :
                seam ? 112 : 172 + (x * 5 + y * 3) % 45;
            return new Color32((byte)metal, (byte)metal, (byte)metal,
                (byte)smoothness);
        });
    }

    private static Texture2D GetOrCreateHazardStripeTexture()
    {
        return GetOrCreateTexture("ArenaHazardStripeV1", 256, false, (x, y) =>
        {
            var stripe = (x + y * 3 / 4) % 64;
            var isYellow = stripe < 34;
            var noise = ((x * 7 + y * 19 + x * y) % 9) - 4;
            if (isYellow)
                return new Color32((byte)Mathf.Clamp(238 + noise, 0, 255),
                    (byte)Mathf.Clamp(158 + noise, 0, 255), 18, 255);
            var dark = (byte)Mathf.Clamp(23 + noise, 0, 255);
            return new Color32(dark, (byte)(dark + 4), (byte)(dark + 7), 255);
        });
    }

    private static Texture2D GetOrCreateLavaTexture()
    {
        return GetOrCreateTexture("ArenaMoltenLavaV1", 256, false, (x, y) =>
        {
            var u = x * Mathf.PI * 2f / 256f;
            var v = y * Mathf.PI * 2f / 256f;
            var a = Mathf.Sin(v * 4f + Mathf.Sin(u * 2f) * 2.1f);
            var b = Mathf.Sin((u + v) * 3f + Mathf.Cos(v * 2f) * 1.4f);
            var c = Mathf.Sin(u * 2f - v * 3f);
            var flow = a * 0.48f + b * 0.34f + c * 0.18f;
            var vein = Mathf.Clamp01(1f - Mathf.Abs(flow - 0.05f) * 5.8f);
            var crust = Mathf.Clamp01((Mathf.Abs(flow) - 0.5f) * 2.2f);
            var pulse = Mathf.Clamp01(0.38f + vein * 0.62f - crust * 0.3f);
            var variation = Mathf.Sin(u * 13f + v * 17f) * 5f;
            var red = Mathf.Clamp(42f + pulse * 212f + variation, 0f, 255f);
            var green = Mathf.Clamp(8f + vein * 103f + variation * 0.3f, 0f, 255f);
            var blue = Mathf.Clamp(4f + vein * 15f, 0f, 255f);
            return new Color32((byte)red, (byte)green, (byte)blue, 255);
        });
    }

    private static Texture2D GetOrCreateTexture(
        string textureName, int resolution, bool linear, Func<int, int, Color32> pixelGenerator)
    {
        var path = MaterialsFolder + "/" + textureName + ".asset";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null)
            return texture;

        texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32,
            true, linear)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 8
        };
        var pixels = new Color32[resolution * resolution];
        for (var y = 0; y < resolution; y++)
        {
            for (var x = 0; x < resolution; x++)
                pixels[y * resolution + x] = pixelGenerator(x, y);
        }

        texture.SetPixels32(pixels);
        texture.Apply(true, false);
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    private static bool IsPanelFastenerPixel(int x, int y)
    {
        const int inset = 13;
        const int radiusSquared = 9;
        return IsWithinFastener(x, y, inset, inset, radiusSquared) ||
               IsWithinFastener(x, y, 128 - inset - 1, inset, radiusSquared) ||
               IsWithinFastener(x, y, inset, 128 - inset - 1, radiusSquared) ||
               IsWithinFastener(x, y, 128 - inset - 1, 128 - inset - 1, radiusSquared);
    }

    private static bool IsWithinFastener(int x, int y, int centerX, int centerY, int radiusSquared)
    {
        var dx = x - centerX;
        var dy = y - centerY;
        return dx * dx + dy * dy <= radiusSquared;
    }

    private static Mesh GetOrCreateWallVisualMesh(Vector3 wallSize)
    {
        var key = string.Format(
            "WallArmorV1_{0}_{1}_{2}_{3}",
            Mathf.RoundToInt(wallSize.x * 100f),
            Mathf.RoundToInt(wallSize.y * 100f),
            Mathf.RoundToInt(wallSize.z * 100f),
            (int)activeDetailQuality);
        if (wallVisualMeshCache.TryGetValue(key, out var cached))
            return cached;

        var path = MeshesFolder + "/" + key + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = BuildWallVisualMesh(wallSize, key);
            AssetDatabase.CreateAsset(mesh, path);
        }

        wallVisualMeshCache[key] = mesh;
        return mesh;
    }

    private static Mesh BuildWallVisualMesh(Vector3 wallSize, string meshName)
    {
        var builder = new WallVisualMeshBuilder();
        var alongX = wallSize.x >= wallSize.z;
        var runLength = alongX ? wallSize.x : wallSize.z;
        var wallHeight = wallSize.y;
        var thickness = alongX ? wallSize.z : wallSize.x;
        var moduleTarget = activeDetailQuality == ArenaVisualDetailQuality.Performance
            ? 12f
            : activeDetailQuality == ArenaVisualDetailQuality.HighDetail ? 5.5f : 8f;
        var moduleCount = Mathf.Max(1, Mathf.CeilToInt(runLength / moduleTarget));
        var moduleWidth = runLength / moduleCount;
        var panelHeight = Mathf.Max(1.4f, wallHeight * 0.56f);
        var panelCenterY = wallHeight * 0.56f;
        var faceOffset = thickness * 0.5f + 0.055f;
        var panelDepth = 0.09f;
        var supportWidth = Mathf.Clamp(moduleWidth * 0.065f, 0.18f, 0.34f);

        for (var i = 0; i < moduleCount; i++)
        {
            var along = -runLength * 0.5f + moduleWidth * (i + 0.5f);
            var panelWidth = Mathf.Max(0.25f, moduleWidth - 0.42f);
            var panelSubmesh = i % 2 == 0 ? WallPanelSubmesh : WallDarkPanelSubmesh;
            foreach (var side in new[] { -1f, 1f })
            {
                AddWallVisualBox(builder, alongX, along, panelCenterY,
                    side * faceOffset, panelWidth, panelHeight, panelDepth, panelSubmesh);

                AddWallVisualBox(builder, alongX, along,
                    panelCenterY + panelHeight * 0.5f - 0.12f, side * (faceOffset + 0.075f),
                    panelWidth, 0.08f, 0.05f, WallTrimSubmesh);
                AddWallVisualBox(builder, alongX, along,
                    panelCenterY - panelHeight * 0.5f + 0.12f, side * (faceOffset + 0.075f),
                    panelWidth, 0.08f, 0.05f, WallTrimSubmesh);
                AddWallVisualBox(builder, alongX, along,
                    wallHeight - 0.62f, side * (faceOffset + 0.075f),
                    panelWidth, 0.07f, 0.055f, WallAccentSubmesh);

                if (activeDetailQuality != ArenaVisualDetailQuality.Performance && moduleWidth > 2.8f)
                {
                    var ventCount = activeDetailQuality == ArenaVisualDetailQuality.HighDetail ? 4 : 2;
                    for (var vent = 0; vent < ventCount; vent++)
                    {
                        var ventOffset = (vent - (ventCount - 1) * 0.5f) * 0.28f;
                        AddWallVisualBox(builder, alongX, along + ventOffset,
                            panelCenterY - panelHeight * 0.22f, side * (faceOffset + 0.11f),
                            Mathf.Min(0.055f, panelWidth * 0.1f), panelHeight * 0.24f,
                            0.035f, WallDarkPanelSubmesh);
                    }
                }
            }

            var beamPosition = -runLength * 0.5f + moduleWidth * i;
            AddWallVisualBox(builder, alongX, beamPosition, wallHeight * 0.5f,
                0f, supportWidth, wallHeight - 0.04f, thickness + 0.14f, WallTrimSubmesh);
        }

        AddWallVisualBox(builder, alongX, 0f, wallHeight + 0.035f,
            0f, runLength + 0.12f, 0.11f, thickness + 0.18f, WallTrimSubmesh);
        AddWallVisualBox(builder, alongX, 0f, 0.18f,
            0f, runLength + 0.08f, 0.12f, thickness + 0.14f, WallTrimSubmesh);
        return builder.Build(meshName);
    }

    private static void AddWallVisualBox(
        WallVisualMeshBuilder builder, bool alongX, float along, float centerY,
        float faceOffset, float runWidth, float height, float depth, int submesh)
    {
        var center = alongX
            ? new Vector3(along, centerY, faceOffset)
            : new Vector3(faceOffset, centerY, along);
        var size = alongX
            ? new Vector3(runWidth, height, depth)
            : new Vector3(depth, height, runWidth);
        builder.AddBox(center, size, submesh);
    }

    private static Material GetOrCreateMaterial(
        string materialName,
        Color color,
        float metallic,
        float smoothness,
        float emission = 0f,
        Texture2D baseTexture = null,
        Texture2D normalMap = null,
        Texture2D metallicMap = null,
        Texture2D emissionMap = null,
        Vector2 textureScale = default)
    {
        var path = MaterialsFolder + "/" + materialName + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = RoboClashArenaVisuals.CreateMaterial(materialName, color, metallic,
                smoothness, color * emission, baseTexture, normalMap, metallicMap,
                emissionMap, textureScale);
            if (material == null)
                throw new InvalidOperationException(
                    $"Could not create arena material '{materialName}'.");
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            RoboClashArenaVisuals.ConfigureMaterial(material, color, metallic,
                smoothness, color * emission, baseTexture, normalMap, metallicMap,
                emissionMap, textureScale);
        }

        if (color.a < 1f)
            MakeTransparent(material);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void MakeTransparent(Material material)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static Mesh GetOrCreateSpikeMesh()
    {
        const string path = MeshesFolder + "/SpikeCone.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null)
            return mesh;

        const int segments = 12;
        var vertices = new List<Vector3>(segments + 2) { new Vector3(0f, 1f, 0f) };
        vertices.Add(Vector3.zero);
        for (var i = 0; i < segments; i++)
        {
            var angle = i * Mathf.PI * 2f / segments;
            vertices.Add(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.5f);
        }

        var triangles = new List<int>(segments * 6);
        for (var i = 0; i < segments; i++)
        {
            var current = i + 2;
            var next = (i + 1) % segments + 2;
            triangles.Add(0);
            triangles.Add(next);
            triangles.Add(current);
            triangles.Add(1);
            triangles.Add(current);
            triangles.Add(next);
        }

        mesh = new Mesh { name = "SpikeCone" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void BuildArena()
    {
        BuildFloorAndWalls();
        BuildCentralCombatArea();
        BuildCoverAndStructures();
        BuildHazards();
        var spawnZones = BuildSpawnZones();
        BuildPickups();
        BuildEnergyShield();
        BuildTrainingTargets(spawnZones);
        BuildLights();
        BuildPlayer(spawnZones[0]);
    }

    private static void BuildLargeArenaLayout(RoboClashArenaSettings settings)
    {
        var layout = new LargeArenaLayout(settings);
        var northRotation = Quaternion.Euler(0f, 180f, 0f);
        var eastRotation = Quaternion.Euler(0f, -90f, 0f);
        var southRotation = Quaternion.identity;
        var westRotation = Quaternion.Euler(0f, 90f, 0f);

        var ground = CreateBox("Large Complex Continuous Arena Floor", new Vector3(0f, -0.5f, 0f),
            new Vector3(settings.mapWidth, 1f, settings.mapDepth), floorMaterial);
        ground.AddComponent<ArenaFloor>();
        BuildFloorDetails("Large Arena Floor Panel Seams", "LargeFloorDetailsWorldUVV3",
            settings.mapWidth, settings.mapDepth);

        BuildLargePerimeter(settings, layout.HalfWidth, layout.HalfDepth);
        BuildLargeCombatHub(settings, layout.CentralHalfWidth, layout.CentralHalfDepth);
        BuildLargeCorridors(layout);

        GameObject armory = null;
        GameObject hazardRoom = null;
        GameObject workshop = null;
        GameObject powerRoom = null;
        if (settings.generateSideRooms)
        {
            armory = BuildLargeRoom("ARMORY", layout.NorthRoom, northRotation, layout.RoomWidthX, layout.RoomDepthZ,
                orangeMaterial, settings);
            hazardRoom = BuildLargeRoom("HAZARD CHAMBER", layout.EastRoom, eastRotation, layout.RoomWidthZ, layout.RoomDepthX,
                yellowMaterial, settings);
            workshop = BuildLargeRoom("WORKSHOP", layout.SouthRoom, southRotation, layout.RoomWidthX, layout.RoomDepthZ,
                redMaterial, settings);
            powerRoom = BuildLargeRoom("POWER CORE", layout.WestRoom, westRotation, layout.RoomWidthZ, layout.RoomDepthX,
                cyanMaterial, settings);
            BuildArmoryFixtures(armory);
            BuildWorkshopMachinery(workshop);
        }

        if (settings.generateCover)
            BuildLargeCover(armory, workshop, layout.CentralHalfWidth, layout.CentralHalfDepth);
        if (powerRoom != null)
            BuildPowerCoreLandmark(powerRoom.transform, settings.generateCover);
        if (settings.generateHazards && hazardRoom != null)
            BuildLargeHazards(hazardRoom, layout.RoomWidthZ, layout.RoomDepthX);
        if (settings.generateElevatedAreas && workshop != null)
            BuildLargeCatwalk(workshop.transform, layout.RoomWidthX, layout.RoomDepthZ);

        var zones = BuildLargeSpawnZones(layout.HalfWidth, layout.HalfDepth, settings.wallThickness);
        if (settings.generatePickups)
            BuildLargePickups(armory, workshop, powerRoom);

        BuildLargeCentralPowerFeature(layout.CentralHalfWidth, layout.CentralHalfDepth);
        BuildLargeLights(layout.NorthRoom, layout.EastRoom, layout.SouthRoom, layout.WestRoom);
        BuildTrainingTargets(zones);
        BuildPlayer(zones[0]);
    }

    private static void BuildLargePerimeter(RoboClashArenaSettings settings, float halfWidth, float halfDepth)
    {
        var height = settings.wallHeight;
        var thickness = settings.wallThickness;
        CreateWall("North Outer Bulkhead", new Vector3(0f, height * 0.5f, halfDepth - thickness * 0.5f),
            new Vector3(settings.mapWidth, height, thickness));
        CreateWall("South Outer Bulkhead", new Vector3(0f, height * 0.5f, -halfDepth + thickness * 0.5f),
            new Vector3(settings.mapWidth, height, thickness));
        CreateWall("East Outer Bulkhead", new Vector3(halfWidth - thickness * 0.5f, height * 0.5f, 0f),
            new Vector3(thickness, height, settings.mapDepth));
        CreateWall("West Outer Bulkhead", new Vector3(-halfWidth + thickness * 0.5f, height * 0.5f, 0f),
            new Vector3(thickness, height, settings.mapDepth));

        var upperY = height - 0.65f;
        for (var x = -1; x <= 1; x += 2)
        {
            CreateBox("Outer Bulkhead Cyan Beacon",
                new Vector3(x * (halfWidth - 1.4f), upperY, 0f),
                new Vector3(0.15f, 0.3f, settings.mapDepth - 8f), cyanMaterial, false);
        }
        for (var z = -1; z <= 1; z += 2)
        {
            CreateBox("Outer Bulkhead Amber Beacon",
                new Vector3(0f, upperY, z * (halfDepth - 1.4f)),
                new Vector3(settings.mapWidth - 8f, 0.3f, 0.15f), orangeMaterial, false);
        }
    }

    private static void BuildLargeCombatHub(
        RoboClashArenaSettings settings, float halfWidth, float halfDepth)
    {
        var height = settings.wallHeight;
        var thickness = settings.wallThickness;
        var opening = Mathf.Max(settings.corridorWidth, 6f);

        BuildWallWithOpening("Central Hub North Wall", true, halfDepth, settings.centralArenaWidth,
            opening, height, thickness);
        BuildWallWithOpening("Central Hub South Wall", true, -halfDepth, settings.centralArenaWidth,
            opening, height, thickness);
        BuildWallWithOpening("Central Hub East Wall", false, halfWidth, settings.centralArenaDepth,
            opening, height, thickness);
        BuildWallWithOpening("Central Hub West Wall", false, -halfWidth, settings.centralArenaDepth,
            opening, height, thickness);

        CreateBox("Central Combat Deck", new Vector3(0f, 0.08f, 0f),
            new Vector3(settings.centralArenaWidth - 2f, 0.16f, settings.centralArenaDepth - 2f),
            steelMaterial, false);
        CreateBox("Hub Reactor Plinth", new Vector3(0f, 0.22f, 0f),
            new Vector3(4.5f, 0.44f, 4.5f), darkSteelMaterial);
        CreateBox("Hub Reactor Cyan Crown", new Vector3(0f, 0.47f, 0f),
            new Vector3(3.8f, 0.08f, 3.8f), cyanMaterial, false);

        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox("Central Hub Guide Line",
                new Vector3(side * (halfWidth - 1f), 0.18f, 0f),
                new Vector3(0.16f, 0.04f, settings.centralArenaDepth - 3f), cyanMaterial, false);
            CreateBox("Central Hub Guide Line",
                new Vector3(0f, 0.18f, side * (halfDepth - 1f)),
                new Vector3(settings.centralArenaWidth - 3f, 0.04f, 0.16f), cyanMaterial, false);
        }
    }

    private static void BuildLargeCorridors(LargeArenaLayout layout)
    {
        var settings = layout.Settings;
        var centralHalfWidth = layout.CentralHalfWidth;
        var centralHalfDepth = layout.CentralHalfDepth;
        var ringX = layout.RingX;
        var ringZ = layout.RingZ;
        var width = settings.corridorWidth;
        var wallHeight = Mathf.Min(settings.wallHeight, 5f);
        var thickness = settings.wallThickness * 0.75f;
        var horizontalRailSpan = Mathf.Max(width, ringX * 2f - width);
        var verticalRailSpan = Mathf.Max(width, ringZ * 2f - width);

        BuildWallWithOpening("North Ring Inner Rail", true, ringZ - width * 0.5f,
            horizontalRailSpan, width, wallHeight, thickness);
        BuildWallWithOpening("South Ring Inner Rail", true, -ringZ + width * 0.5f,
            horizontalRailSpan, width, wallHeight, thickness);
        BuildWallWithOpening("North Ring Outer Rail", true, ringZ + width * 0.5f,
            horizontalRailSpan, width, wallHeight, thickness);
        BuildWallWithOpening("South Ring Outer Rail", true, -ringZ - width * 0.5f,
            horizontalRailSpan, width, wallHeight, thickness);
        BuildWallWithOpening("East Ring Inner Rail", false, ringX - width * 0.5f,
            verticalRailSpan, width, wallHeight, thickness);
        BuildWallWithOpening("West Ring Inner Rail", false, -ringX + width * 0.5f,
            verticalRailSpan, width, wallHeight, thickness);
        BuildWallWithOpening("East Ring Outer Rail", false, ringX + width * 0.5f,
            verticalRailSpan, width, wallHeight, thickness);
        BuildWallWithOpening("West Ring Outer Rail", false, -ringX - width * 0.5f,
            verticalRailSpan, width, wallHeight, thickness);

        CreateBox("North Connector Floor", new Vector3(0f, 0.035f,
                (centralHalfDepth + ringZ - width * 0.5f) * 0.5f),
            new Vector3(width, 0.06f, ringZ - width * 0.5f - centralHalfDepth + 0.1f),
            blueMaterial, false);
        CreateBox("South Connector Floor", new Vector3(0f, 0.035f,
                -(centralHalfDepth + ringZ - width * 0.5f) * 0.5f),
            new Vector3(width, 0.06f, ringZ - width * 0.5f - centralHalfDepth + 0.1f),
            redMaterial, false);
        CreateBox("East Connector Floor", new Vector3(
                (centralHalfWidth + ringX - width * 0.5f) * 0.5f, 0.035f, 0f),
            new Vector3(ringX - width * 0.5f - centralHalfWidth + 0.1f, 0.06f, width),
            orangeMaterial, false);
        CreateBox("West Connector Floor", new Vector3(
                -(centralHalfWidth + ringX - width * 0.5f) * 0.5f, 0.035f, 0f),
            new Vector3(ringX - width * 0.5f - centralHalfWidth + 0.1f, 0.06f, width),
            cyanMaterial, false);

        CreateBox("North Ring Floor", new Vector3(0f, 0.025f, ringZ),
            new Vector3(ringX * 2f, 0.05f, width - 0.4f), steelMaterial, false);
        CreateBox("South Ring Floor", new Vector3(0f, 0.025f, -ringZ),
            new Vector3(ringX * 2f, 0.05f, width - 0.4f), steelMaterial, false);
        CreateBox("East Ring Floor", new Vector3(ringX, 0.025f, 0f),
            new Vector3(width - 0.4f, 0.05f, ringZ * 2f), steelMaterial, false);
        CreateBox("West Ring Floor", new Vector3(-ringX, 0.025f, 0f),
            new Vector3(width - 0.4f, 0.05f, ringZ * 2f), steelMaterial, false);

        if (settings.generateSideRooms)
        {
            BuildRoomConnector("North Room Connector", true, ringZ + width * 0.5f,
                layout.NorthRoomEdge, width, wallHeight, thickness);
            BuildRoomConnector("South Room Connector", true, -ringZ - width * 0.5f,
                -layout.NorthRoomEdge, width, wallHeight, thickness);
            BuildRoomConnector("East Room Connector", false, ringX + width * 0.5f,
                layout.EastRoomEdge, width, wallHeight, thickness);
            BuildRoomConnector("West Room Connector", false, -ringX - width * 0.5f,
                -layout.EastRoomEdge, width, wallHeight, thickness);
        }

        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox("Ring Cyan Route Stripe", new Vector3(0f, 0.062f, side * ringZ),
                new Vector3(ringX * 2f - width, 0.035f, 0.12f), cyanMaterial, false);
            CreateBox("Ring Amber Route Stripe", new Vector3(side * ringX, 0.062f, 0f),
                new Vector3(0.12f, 0.035f, ringZ * 2f - width), orangeMaterial, false);
        }
    }

    private static void BuildRoomConnector(
        string name, bool northSouth, float ringEdge, float roomEdge,
        float width, float wallHeight, float thickness)
    {
        var length = Mathf.Abs(roomEdge - ringEdge);
        if (length <= 0.2f)
            return;

        var midpoint = (ringEdge + roomEdge) * 0.5f;
        for (var side = -1; side <= 1; side += 2)
        {
            var position = northSouth
                ? new Vector3(side * width * 0.5f, wallHeight * 0.5f, midpoint)
                : new Vector3(midpoint, wallHeight * 0.5f, side * width * 0.5f);
            var size = northSouth
                ? new Vector3(thickness, wallHeight, length)
                : new Vector3(length, wallHeight, thickness);
            CreateWall(name + " Side Rail", position, size);
        }

        var panelCenter = northSouth
            ? new Vector3(0f, 0.035f, midpoint)
            : new Vector3(midpoint, 0.035f, 0f);
        var panelSize = northSouth
            ? new Vector3(width - 0.35f, 0.06f, length)
            : new Vector3(length, 0.06f, width - 0.35f);
        CreateBox(name + " Floor", panelCenter, panelSize, steelMaterial, false);
    }

    private static void BuildWallWithOpening(
        string name, bool horizontal, float fixedCoordinate, float span, float opening,
        float wallHeight, float thickness)
    {
        var segmentLength = Mathf.Max(0f, (span - opening) * 0.5f);
        if (segmentLength <= 0.05f)
            return;

        var y = wallHeight * 0.5f;
        var start = -span * 0.5f + segmentLength * 0.5f;
        var end = span * 0.5f - segmentLength * 0.5f;
        var size = horizontal
            ? new Vector3(segmentLength, wallHeight, thickness)
            : new Vector3(thickness, wallHeight, segmentLength);
        var first = horizontal
            ? new Vector3(start, y, fixedCoordinate)
            : new Vector3(fixedCoordinate, y, start);
        var second = horizontal
            ? new Vector3(end, y, fixedCoordinate)
            : new Vector3(fixedCoordinate, y, end);
        CreateWall(name + " Left", first, size);
        CreateWall(name + " Right", second, size);
    }

    private static GameObject BuildLargeRoom(
        string roomName, Vector3 center, Quaternion rotation, float width, float depth,
        Material accent, RoboClashArenaSettings settings)
    {
        var room = new GameObject(roomName);
        room.transform.SetParent(root, false);
        room.transform.SetPositionAndRotation(center, rotation);

        CreateBox(roomName + " Floor Inlay", center + Vector3.up * 0.025f,
            new Vector3(width - 1f, 0.05f, depth - 1f), floorMaterial, false, room.transform);
        CreateBox(roomName + " Identity Floor Stripe",
            room.transform.TransformPoint(new Vector3(0f, 0.065f, depth * 0.5f - 2f)),
            new Vector3(Mathf.Min(width - 6f, 14f), 0.025f, 0.16f), accent, false, room.transform);

        var halfWidth = width * 0.5f;
        var halfDepth = depth * 0.5f;
        var wallHeight = settings.wallHeight;
        var thickness = settings.wallThickness;
        CreateWall(roomName + " Back Wall",
            room.transform.TransformPoint(new Vector3(0f, wallHeight * 0.5f, -halfDepth)),
            new Vector3(width, wallHeight, thickness), room.transform);
        CreateWall(roomName + " Left Wall",
            room.transform.TransformPoint(new Vector3(-halfWidth, wallHeight * 0.5f, 0f)),
            new Vector3(thickness, wallHeight, depth), room.transform);
        CreateWall(roomName + " Right Wall",
            room.transform.TransformPoint(new Vector3(halfWidth, wallHeight * 0.5f, 0f)),
            new Vector3(thickness, wallHeight, depth), room.transform);

        var opening = Mathf.Min(settings.corridorWidth, width - 6f);

        CreateBox(roomName + " Entry Header",
            room.transform.TransformPoint(new Vector3(0f, wallHeight - 0.25f, halfDepth)),
            new Vector3(opening, 0.5f, thickness), steelMaterial, true, room.transform);
        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox(roomName + " Door Frame Column",
                room.transform.TransformPoint(new Vector3(side * (opening * 0.5f + 0.16f),
                    wallHeight * 0.5f, halfDepth)),
                new Vector3(0.32f, wallHeight, thickness + 0.25f), edgeTrimMaterial, false, room.transform);
        }
        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox(roomName + " Doorway Neon",
                room.transform.TransformPoint(new Vector3(side * (opening * 0.5f + 0.18f),
                    wallHeight * 0.48f, halfDepth - thickness * 0.5f - 0.14f)),
                new Vector3(0.07f, wallHeight * 0.82f, 0.1f), accent, false, room.transform);
        }
        CreateBox(roomName + " Door Header Light",
            room.transform.TransformPoint(new Vector3(0f, wallHeight - 0.52f, halfDepth - thickness * 0.5f - 0.14f)),
            new Vector3(opening - 0.5f, 0.08f, 0.1f), accent, false, room.transform);

        for (var side = -1; side <= 1; side += 2)
        {
            var pipe = CreateCylinder(roomName + " Ceiling Conduit Pipe",
                room.transform.TransformPoint(new Vector3(side * (halfWidth - 2f),
                    wallHeight - 0.4f, 0f)),
                new Vector3(0.22f, (depth - 4f) * 0.5f, 0.22f),
                steelMaterial, false, room.transform);
            pipe.transform.rotation = room.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
        }

        var signPosition = new Vector3(0f, wallHeight - 1.25f, -halfDepth + thickness * 0.5f + 0.1f);
        var signWidth = Mathf.Min(width - 4f, roomName.Length * 1.15f + 2f);
        CreateBox(roomName + " Sign Backing", room.transform.TransformPoint(signPosition),
            new Vector3(signWidth, 1.05f, 0.14f), wallDarkPanelMaterial, false, room.transform);
        CreateBox(roomName + " Sign Accent",
            room.transform.TransformPoint(signPosition + Vector3.down * 0.46f + Vector3.forward * 0.09f),
            new Vector3(signWidth - 0.4f, 0.06f, 0.04f), accent, false, room.transform);

        var sign = new GameObject(roomName + " Room Sign");
        sign.transform.SetParent(room.transform, false);
        sign.transform.localPosition = signPosition + Vector3.forward * 0.1f;
        sign.transform.localRotation = Quaternion.identity;
        var signText = sign.AddComponent<TextMesh>();
        signText.text = roomName;
        signText.anchor = TextAnchor.MiddleCenter;
        signText.alignment = TextAlignment.Center;
        signText.fontSize = 48;
        signText.characterSize = 0.16f;
        signText.color = accent.color;
        signText.richText = false;

        return room;
    }

    private static void BuildLargeCover(
        GameObject armory, GameObject workshop, float centralHalfWidth, float centralHalfDepth)
    {
        var coverPositions = new[]
        {
            new Vector3(-centralHalfWidth * 0.58f, 0f, -centralHalfDepth * 0.32f),
            new Vector3(centralHalfWidth * 0.58f, 0f, centralHalfDepth * 0.32f),
            new Vector3(-centralHalfWidth * 0.58f, 0f, centralHalfDepth * 0.32f),
            new Vector3(centralHalfWidth * 0.58f, 0f, -centralHalfDepth * 0.32f)
        };
        for (var i = 0; i < coverPositions.Length; i++)
        {
            var cover = CreateBox("Hub Combat Cover " + (i + 1), coverPositions[i] + Vector3.up,
                new Vector3(5.5f, 2f, 0.8f), darkSteelMaterial);
            cover.AddComponent<MetalBarrier>();
            AddArmorPanelDetails(cover.transform, new Vector3(5.5f, 2f, 0.8f), orangeMaterial);
            CreateBox("Hub Cover Warning Edge", coverPositions[i] + Vector3.up * 2f,
                new Vector3(5.5f, 0.1f, 0.84f), orangeMaterial, false);
        }

        if (armory != null)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                var crate = CreateBox("Armory Ammunition Crate",
                    armory.transform.TransformPoint(new Vector3(side * 8f, 0.9f, -1f)),
                    new Vector3(3.2f, 1.8f, 3f), darkSteelMaterial, true, armory.transform);
                crate.AddComponent<MetalBarrier>();
                AddArmorPanelDetails(crate.transform, new Vector3(3.2f, 1.8f, 3f), blueMaterial);
                CreateBox("Armory Crate Stripe",
                    armory.transform.TransformPoint(new Vector3(side * 8f, 1.85f, -1f)),
                    new Vector3(3.2f, 0.1f, 3f), orangeMaterial, false, armory.transform);
            }
        }

        if (workshop != null)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                var crate = CreateBox("Workshop Tool Crate",
                    workshop.transform.TransformPoint(new Vector3(side * 8f, 0.65f, -3f)),
                    new Vector3(4f, 1.3f, 3f), side < 0 ? redMaterial : steelMaterial,
                    true, workshop.transform);
                crate.AddComponent<MetalBarrier>();
                AddArmorPanelDetails(crate.transform, new Vector3(4f, 1.3f, 3f),
                    side < 0 ? orangeMaterial : yellowMaterial);
            }
        }

    }

    private static void BuildArmoryFixtures(GameObject armory)
    {
        for (var side = -1; side <= 1; side += 2)
        {
            var cabinet = CreateBox("Armory Weapon Locker",
                armory.transform.TransformPoint(new Vector3(side * 10f, 1.35f, -8f)),
                new Vector3(3.2f, 2.7f, 2.4f), darkSteelMaterial, true, armory.transform);
            cabinet.AddComponent<MetalBarrier>();
            AddArmorPanelDetails(cabinet.transform, new Vector3(3.2f, 2.7f, 2.4f), blueMaterial);
            CreateBox("Armory Locker Status Light",
                armory.transform.TransformPoint(new Vector3(side * 10f, 2.65f, -6.72f)),
                new Vector3(2.1f, 0.08f, 0.06f), cyanMaterial, false, armory.transform);
        }
    }

    private static void BuildWorkshopMachinery(GameObject workshop)
    {
        for (var side = -1; side <= 1; side += 2)
        {
            var machine = CreateBox("Workshop Press Machine",
                workshop.transform.TransformPoint(new Vector3(side * 9f, 1.2f, -8.1f)),
                new Vector3(3.4f, 2.4f, 2.8f), darkSteelMaterial, true, workshop.transform);
            machine.AddComponent<MetalBarrier>();
            AddArmorPanelDetails(machine.transform, new Vector3(3.4f, 2.4f, 2.8f), yellowMaterial);
            CreateCylinder("Workshop Machine Hydraulic Ram",
                workshop.transform.TransformPoint(new Vector3(side * 9f, 2.75f, -8.1f)),
                new Vector3(0.55f, 0.55f, 0.55f), edgeTrimMaterial, false);
            CreateBox("Workshop Machine Warning Panel",
                workshop.transform.TransformPoint(new Vector3(side * 9f, 1.3f, -6.62f)),
                new Vector3(1.4f, 0.5f, 0.05f), side < 0 ? redMaterial : yellowMaterial,
                false, workshop.transform);
        }
    }

    private static void BuildPowerCoreLandmark(Transform room, bool includeCover)
    {
        CreateCylinder("Power Core Reactor Housing", room.TransformPoint(new Vector3(0f, 1.6f, -2f)),
            new Vector3(3.8f, 1.6f, 3.8f), darkSteelMaterial, true);
        CreateCylinder("Power Core Cyan Reactor", room.TransformPoint(new Vector3(0f, 1.8f, -2f)),
            new Vector3(2.3f, 1.65f, 2.3f), cyanMaterial, false);
        CreateCylinder("Power Core Reactor Crown", room.TransformPoint(new Vector3(0f, 3.5f, -2f)),
            new Vector3(3.2f, 0.09f, 3.2f), purpleMaterial, false);
        CreateCylinder("Power Core Conduit Ring", room.TransformPoint(new Vector3(0f, 0.16f, -2f)),
            new Vector3(4.4f, 0.08f, 4.4f), edgeTrimMaterial, false);
        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox("Power Core Energy Conduit",
                room.TransformPoint(new Vector3(side * 3.2f, 1.1f, -2f)),
                new Vector3(0.22f, 2.2f, 0.22f), cyanMaterial, false, room);
        }

        if (!includeCover)
            return;

        for (var side = -1; side <= 1; side += 2)
        {
            for (var direction = -1; direction <= 1; direction += 2)
            {
                var local = new Vector3(side * 8f, 1.35f, direction * 6f);
                var pillar = CreateBox("Power Core Combat Pillar", room.TransformPoint(local),
                    new Vector3(1.8f, 2.7f, 1.8f), steelMaterial, true, room);
                AddArmorPanelDetails(pillar.transform, new Vector3(1.8f, 2.7f, 1.8f), purpleMaterial);
                CreateBox("Power Core Pillar Beacon",
                    room.TransformPoint(local + Vector3.up * 1.4f),
                    new Vector3(1.5f, 0.12f, 1.5f), cyanMaterial, false, room);
            }
        }
    }

    private static void AddArmorPanelDetails(Transform body, Vector3 bodySize, Material accent)
    {
        var frontOffset = bodySize.z * 0.5f + 0.06f;
        var panelSize = new Vector3(bodySize.x * 0.68f, bodySize.y * 0.38f, 0.08f);
        var panelY = bodySize.y * 0.54f;
        foreach (var side in new[] { -1f, 1f })
        {
            var panel = CreateBox("Raised Armor Panel",
                body.position + body.rotation * new Vector3(0f, panelY, side * frontOffset),
                panelSize, wallPanelMaterial, false);
            panel.transform.rotation = body.rotation;
            var stripe = CreateBox("Armor Panel Accent",
                body.position + body.rotation * new Vector3(0f, panelY + panelSize.y * 0.5f - 0.08f,
                    side * (frontOffset + 0.055f)),
                new Vector3(panelSize.x * 0.82f, 0.07f, 0.04f), accent, false);
            stripe.transform.rotation = body.rotation;

            for (var edge = -1; edge <= 1; edge += 2)
            {
                var brace = CreateBox("Armor Panel Edge Brace",
                    body.position + body.rotation * new Vector3(edge * bodySize.x * 0.43f,
                        bodySize.y * 0.5f, side * frontOffset),
                    new Vector3(0.12f, bodySize.y * 0.82f, 0.12f), edgeTrimMaterial, false);
                brace.transform.rotation = body.rotation;
            }
        }
    }

    private static void BuildLargeHazards(GameObject hazardRoom, float roomWidth, float roomDepth)
    {
        var roomTransform = hazardRoom.transform;
        var channelLength = Mathf.Max(20f, roomWidth - 8f);
        var channelDepth = Mathf.Clamp(roomDepth * 0.14f, 3.2f, 4f);
        var bridgeWidth = 3.8f;
        var bridgeDepth = channelDepth + 1.4f;
        var bridgeOffset = channelLength * 0.31f;
        var bridgeCenters = new[] { -bridgeOffset, 0f, bridgeOffset };
        var segmentStart = -channelLength * 0.5f;
        var hotZoneDepth = 0f;
        var segmentIndex = 0;

        for (var i = 0; i <= bridgeCenters.Length; i++)
        {
            var segmentEnd = i < bridgeCenters.Length
                ? bridgeCenters[i] - bridgeWidth * 0.5f - 0.35f
                : channelLength * 0.5f;
            var segmentLength = segmentEnd - segmentStart;
            if (segmentLength > 0.7f)
            {
                CreateLargeLavaSegment(roomTransform, segmentIndex++,
                    (segmentStart + segmentEnd) * 0.5f, hotZoneDepth, segmentLength, channelDepth);
            }

            if (i < bridgeCenters.Length)
            {
                var bridge = CreateBox("Hazard Channel Bridge " + (i + 1),
                    roomTransform.TransformPoint(new Vector3(bridgeCenters[i], 0.38f, hotZoneDepth)),
                    new Vector3(bridgeWidth, 0.76f, bridgeDepth), steelMaterial, true, roomTransform);
                bridge.AddComponent<MetalBarrier>();
                CreateBox("Hazard Channel Bridge Warning Stripe",
                    roomTransform.TransformPoint(new Vector3(bridgeCenters[i], 0.78f, hotZoneDepth)),
                    new Vector3(bridgeWidth - 0.2f, 0.035f, bridgeDepth - 0.2f), hazardStripeMaterial,
                    false, roomTransform);
                CreateShallowBridgeRamp(roomTransform, bridgeCenters[i], hotZoneDepth,
                    bridgeWidth, bridgeDepth);
                segmentStart = bridgeCenters[i] + bridgeWidth * 0.5f + 0.35f;
            }
        }

        var sideOffset = roomWidth * 0.33f;
        for (var side = -1; side <= 1; side += 2)
        {
            var spikePosition = roomTransform.TransformPoint(new Vector3(side * sideOffset, 0.64f, -roomDepth * 0.24f));
            CreateSpikeTrap("Hazard Chamber Spike Strip", spikePosition, roomTransform,
                new Vector3(3.2f, 1.3f, 4.2f));
            CreateBox("Hazard Strip Warning Plate",
                roomTransform.TransformPoint(new Vector3(side * sideOffset, 0.04f, -roomDepth * 0.24f)),
                new Vector3(3.8f, 0.06f, 4.8f), yellowMaterial, false, roomTransform);
        }

        CreateBox("Hazard Chamber Safe Route Stripe",
            roomTransform.TransformPoint(new Vector3(0f, 0.07f, roomDepth * 0.32f)),
            new Vector3(roomWidth * 0.72f, 0.05f, 0.22f), cyanMaterial, false, roomTransform);
        CreateBox("Hazard Chamber Risk Route Stripe",
            roomTransform.TransformPoint(new Vector3(0f, 0.07f, -roomDepth * 0.43f)),
            new Vector3(roomWidth * 0.72f, 0.05f, 0.22f), orangeMaterial, false, roomTransform);
    }

    private static void CreateLargeLavaSegment(
        Transform room, int index, float localX, float localZ, float width, float depth)
    {
        var trigger = new GameObject($"Lava Channel Segment {index + 1}");
        trigger.transform.SetParent(room, false);
        trigger.transform.localPosition = new Vector3(localX, 0.52f, localZ);
        var box = trigger.AddComponent<BoxCollider>();
        box.size = new Vector3(width, 1f, depth);
        box.isTrigger = true;
        trigger.AddComponent<LavaPit>();

        var surface = CreateBox("Molten Channel Surface",
            room.TransformPoint(new Vector3(localX, 0.045f, localZ)),
            new Vector3(width, 0.08f, depth), lavaMaterial, false, room);
        surface.transform.SetParent(trigger.transform, true);
        surface.name = "Molten Channel Surface";
        surface.AddComponent<RoboClashLavaVisual>();
        CreateBox("Molten Channel Dark Rock Border",
            room.TransformPoint(new Vector3(localX, 0.08f, localZ + depth * 0.5f)),
            new Vector3(width, 0.18f, 0.28f), darkSteelMaterial, false, room);
        CreateBox("Molten Channel Dark Rock Border",
            room.TransformPoint(new Vector3(localX, 0.08f, localZ - depth * 0.5f)),
            new Vector3(width, 0.18f, 0.28f), darkSteelMaterial, false, room);
        CreateBox("Molten Channel Caution Border",
            room.TransformPoint(new Vector3(localX, 0.18f, localZ + depth * 0.5f)),
            new Vector3(width, 0.035f, 0.1f), hazardStripeMaterial, false, room);
        CreateBox("Molten Channel Caution Border",
            room.TransformPoint(new Vector3(localX, 0.18f, localZ - depth * 0.5f)),
            new Vector3(width, 0.035f, 0.1f), hazardStripeMaterial, false, room);
    }

    private static void CreateShallowBridgeRamp(
        Transform room, float localX, float localZ, float width, float bridgeDepth)
    {
        const float rampLength = 2.8f;
        var rampOffset = bridgeDepth * 0.5f + rampLength * 0.5f - 0.05f;
        foreach (var side in new[] { -1, 1 })
        {
            var ramp = CreateBox("Hazard Bridge Access Ramp",
                room.TransformPoint(new Vector3(localX, 0.38f, localZ + side * rampOffset)),
                new Vector3(width - 0.1f, 0.22f, rampLength), steelMaterial, true, room);
            ramp.transform.localRotation = Quaternion.Euler(side > 0 ? 14f : -14f, 0f, 0f);
            ramp.AddComponent<MetalBarrier>();
        }
    }

    private static void CreateSpikeTrap(string name, Vector3 position, Transform parent, Vector3 size)
    {
        var trap = new GameObject(name);
        trap.transform.SetParent(parent, false);
        trap.transform.position = position;
        var trigger = trap.AddComponent<BoxCollider>();
        trigger.size = size;
        trigger.isTrigger = true;
        trap.AddComponent<SpikeTrap>();
        var basePosition = position - Vector3.up * (size.y * 0.5f - 0.1f);
        CreateBox(name + " Steel Base", basePosition,
            new Vector3(size.x, 0.2f, size.z), darkSteelMaterial, true, parent);
        var columnsX = Mathf.Max(2, Mathf.FloorToInt(size.x / 1.1f));
        var columnsZ = Mathf.Max(2, Mathf.FloorToInt(size.z / 1.1f));
        for (var x = 0; x < columnsX; x++)
        {
            for (var z = 0; z < columnsZ; z++)
            {
                var localX = -size.x * 0.5f + (x + 0.5f) * size.x / columnsX;
                var localZ = -size.z * 0.5f + (z + 0.5f) * size.z / columnsZ;
                var spike = new GameObject("Hazard Spike");
                spike.transform.SetParent(trap.transform, false);
                spike.transform.localPosition = new Vector3(localX, -size.y * 0.35f, localZ);
                spike.transform.localScale = new Vector3(0.72f, 0.8f, 0.72f);
                spike.AddComponent<MeshFilter>().sharedMesh = spikeMesh;
                spike.AddComponent<MeshRenderer>().sharedMaterial = orangeMaterial;
            }
        }
    }

    private static void BuildLargeCatwalk(Transform room, float roomWidth, float roomDepth)
    {
        const float deckHeight = 0.7f;
        var deck = CreateBox("Workshop Low Catwalk", room.TransformPoint(new Vector3(0f, deckHeight - 0.18f, 1f)),
            new Vector3(6f, 0.36f, 8f), steelMaterial, true, room);
        deck.AddComponent<MetalBarrier>();
        CreateBox("Workshop Catwalk Underside Beam",
            room.TransformPoint(new Vector3(0f, 0.28f, 1f)),
            new Vector3(5.6f, 0.22f, 7.4f), darkSteelMaterial, false, room);
        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox("Workshop Catwalk Support",
                room.TransformPoint(new Vector3(side * 2.35f, 0.17f, 1f)),
                new Vector3(0.22f, 0.34f, 0.5f), edgeTrimMaterial, false, room);
            CreateBox("Workshop Catwalk Rail Accent",
                room.TransformPoint(new Vector3(side * 2.8f, 0.87f, 1f)),
                new Vector3(0.07f, 0.08f, 6.2f), yellowMaterial, false, room);
        }
        CreateBox("Workshop Catwalk Cyan Edge", room.TransformPoint(new Vector3(0f, deckHeight + 0.02f, 1f)),
            new Vector3(5.7f, 0.05f, 7.7f), cyanMaterial, false, room);

        var nearRamp = CreateBox("Workshop Catwalk Entry Ramp",
            room.TransformPoint(new Vector3(0f, 0.35f, -5.3f)),
            new Vector3(5.5f, 0.3f, 6.2f), steelMaterial, true, room);
        nearRamp.transform.localRotation = Quaternion.Euler(-6.5f, 0f, 0f);
        var farRamp = CreateBox("Workshop Catwalk Exit Ramp",
            room.TransformPoint(new Vector3(0f, 0.35f, 7.3f)),
            new Vector3(5.5f, 0.3f, 6.2f), steelMaterial, true, room);
        farRamp.transform.localRotation = Quaternion.Euler(6.5f, 0f, 0f);

        CreateBox("Workshop Rail Left", room.TransformPoint(new Vector3(-2.8f, deckHeight + 0.4f, 1f)),
            new Vector3(0.18f, 0.8f, 6.2f), redMaterial, true, room);
        CreateBox("Workshop Rail Right", room.TransformPoint(new Vector3(2.8f, deckHeight + 0.4f, 1f)),
            new Vector3(0.18f, 0.8f, 6.2f), redMaterial, true, room);
    }

    private static ArenaSpawnZone[] BuildLargeSpawnZones(float halfWidth, float halfDepth, float wallThickness)
    {
        var insetX = Mathf.Max(8f, wallThickness + 3.5f);
        var insetZ = insetX;
        var x = halfWidth - insetX;
        var z = halfDepth - insetZ;
        var positions = new[]
        {
            new Vector3(-x, 0f, -z),
            new Vector3(x, 0f, -z),
            new Vector3(-x, 0f, z),
            new Vector3(x, 0f, z)
        };
        var colors = new[] { blueMaterial, redMaterial, cyanMaterial, yellowMaterial };
        var zones = new ArenaSpawnZone[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            var zoneObject = new GameObject($"Large Map Spawn Zone {i + 1}");
            zoneObject.transform.SetParent(root, false);
            zoneObject.transform.SetPositionAndRotation(
                positions[i], Quaternion.LookRotation(-positions[i].normalized, Vector3.up));
            var zone = zoneObject.AddComponent<ArenaSpawnZone>();
            zone.SetSpawnIndex(i);
            zones[i] = zone;

            CreateBox("Spawn Platform", positions[i] + Vector3.up * 0.15f,
                new Vector3(5.5f, 0.3f, 5.5f), steelMaterial, true, zoneObject.transform);
            CreateBox("Spawn Zone Color Plate", positions[i] + Vector3.up * 0.32f,
                new Vector3(4.8f, 0.05f, 4.8f), colors[i], false, zoneObject.transform);
            for (var side = -1; side <= 1; side += 2)
            {
                CreateBox("Spawn Beacon", zoneObject.transform.TransformPoint(
                        new Vector3(side * 3f, 1.1f, side * 3f)),
                    new Vector3(0.25f, 1.4f, 0.25f), colors[i], false, zoneObject.transform)
                    .AddComponent<ArenaDecoration>();
                CreateBox("Spawn Beacon", zoneObject.transform.TransformPoint(
                        new Vector3(-side * 3f, 1.1f, side * 3f)),
                    new Vector3(0.25f, 1.4f, 0.25f), colors[i], false, zoneObject.transform)
                    .AddComponent<ArenaDecoration>();
            }
        }

        return zones;
    }

    private static void BuildLargePickups(GameObject armory, GameObject workshop, GameObject powerRoom)
    {
        if (armory != null)
        {
            CreateWeaponPickup("Armory Weapon Pickup A",
                armory.transform.TransformPoint(new Vector3(-5f, 1.1f, 0f)));
            CreateWeaponPickup("Armory Weapon Pickup B",
                armory.transform.TransformPoint(new Vector3(5f, 1.1f, 0f)));
        }

        if (workshop != null)
            CreateWeaponPickup("Workshop Weapon Pickup",
                workshop.transform.TransformPoint(new Vector3(6f, 1.1f, 6f)));

        if (powerRoom != null)
        {
            CreateShieldPickup("Power Room Shield Pickup",
                powerRoom.transform.TransformPoint(new Vector3(0f, 1.1f, 7f)));
            CreateEnergyShieldGenerator(powerRoom.transform.TransformPoint(new Vector3(0f, 0f, -7f)));
        }
        else
        {
            CreateShieldPickup("Central Shield Pickup", new Vector3(0f, 1.1f, 8f));
        }

        CreateHealthPickup("North Health Pickup", new Vector3(-9f, 1.1f, 0f));
        CreateHealthPickup("South Health Pickup", new Vector3(9f, 1.1f, 0f));
    }

    private static void CreateWeaponPickup(string name, Vector3 position)
    {
        var pickup = new GameObject(name);
        pickup.transform.SetParent(root, false);
        pickup.transform.position = position;
        var trigger = pickup.AddComponent<SphereCollider>();
        trigger.radius = 1.1f;
        trigger.isTrigger = true;
        pickup.AddComponent<ArenaWeaponPickup>();
        CreateSphere("Weapon Pickup Core", position, Vector3.one * 0.65f, orangeMaterial, false, pickup.transform);
        CreateBox("Weapon Pickup Ring", position + Vector3.down * 0.38f,
            new Vector3(1.3f, 0.08f, 1.3f), steelMaterial, false, pickup.transform);
    }

    private static void CreateShieldPickup(string name, Vector3 position)
    {
        var pickup = new GameObject(name);
        pickup.transform.SetParent(root, false);
        pickup.transform.position = position;
        var trigger = pickup.AddComponent<SphereCollider>();
        trigger.radius = 1.1f;
        trigger.isTrigger = true;
        pickup.AddComponent<ArenaShieldPickup>();
        CreateSphere("Shield Pickup Core", position, Vector3.one * 0.6f, cyanMaterial, false, pickup.transform);
        CreateBox("Shield Pickup Base", position + Vector3.down * 0.42f,
            new Vector3(1.3f, 0.08f, 1.3f), steelMaterial, false, pickup.transform);
    }

    private static void CreateEnergyShieldGenerator(Vector3 position)
    {
        var shield = CreateSphere("Rechargeable Energy Shield", position + Vector3.up * 1.5f,
            Vector3.one * 2.8f, shieldMaterial, true);
        shield.GetComponent<SphereCollider>().radius = 0.5f;
        shield.AddComponent<Rigidbody>().isKinematic = true;
        shield.AddComponent<ArenaEnergyShield>();
        CreateBox("Shield Generator Base", position + Vector3.up * 0.2f,
            new Vector3(1.6f, 0.3f, 1.6f), darkSteelMaterial);
        CreateCylinder("Shield Generator Coil", position + Vector3.up * 0.85f,
            new Vector3(0.35f, 0.8f, 0.35f), cyanMaterial, false);
    }

    private static void BuildLargeCentralPowerFeature(float centralHalfWidth, float centralHalfDepth)
    {
        CreateCylinder("Large Hub Reactor Core", new Vector3(0f, 1.8f, 0f),
            new Vector3(1.5f, 1.6f, 1.5f), cyanMaterial, false);
        CreateCylinder("Large Hub Reactor Ring", new Vector3(0f, 0.62f, 0f),
            new Vector3(4.8f, 0.14f, 4.8f), purpleMaterial, false);
        var landmarkRadius = Mathf.Min(centralHalfWidth, centralHalfDepth) - 5f;
        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox("Large Hub Reactor Beacon",
                new Vector3(side * landmarkRadius, 2.4f, 0f),
                new Vector3(0.45f, 4.8f, 0.45f), cyanMaterial, false);
            CreateBox("Large Hub Reactor Beacon",
                new Vector3(0f, 2.4f, side * landmarkRadius),
                new Vector3(0.45f, 4.8f, 0.45f), cyanMaterial, false);
        }

        CreatePointLight("Large Hub Reactor Glow", new Vector3(0f, 2.5f, 0f),
            new Color(0.03f, 0.55f, 1f), 850f, 14f);
    }

    private static void BuildLargeLights(Vector3 north, Vector3 east, Vector3 south, Vector3 west)
    {
        var keyObject = new GameObject("Large Arena Key Light");
        keyObject.transform.SetParent(root, false);
        keyObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
        var key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(0.67f, 0.82f, 1f);
        key.intensity = 1.15f;
        key.shadows = LightShadows.Soft;

        CreatePointLight("Armory Blue Flood", north + Vector3.up * 4.5f,
            new Color(0.12f, 0.35f, 1f), 1400f, 28f);
        CreatePointLight("Hazard Amber Flood", east + Vector3.up * 4.5f,
            new Color(1f, 0.3f, 0.05f), 1400f, 28f);
        CreatePointLight("Workshop Red Flood", south + Vector3.up * 4.5f,
            new Color(1f, 0.08f, 0.06f), 1400f, 28f);
        CreatePointLight("Power Cyan Flood", west + Vector3.up * 4.5f,
            new Color(0.03f, 0.65f, 1f), 1400f, 28f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.12f, 0.19f, 0.26f);
        RenderSettings.ambientEquatorColor = new Color(0.055f, 0.075f, 0.09f);
        RenderSettings.ambientGroundColor = new Color(0.025f, 0.03f, 0.04f);
    }

    private static void BuildFloorAndWalls()
    {
        var floor = CreateBox("Arena Floor", new Vector3(0f, -0.5f, 0f), new Vector3(36f, 1f, 36f), floorMaterial);
        floor.AddComponent<ArenaFloor>();
        BuildFloorDetails("Compact Arena Floor Panel Seams", "CompactFloorDetailsWorldUVV2", 36f, 36f);

        CreateBox("Floor Center Plate", new Vector3(0f, 0.025f, 0f), new Vector3(20f, 0.05f, 20f), steelMaterial);
        for (var lane = -1; lane <= 1; lane += 2)
        {
            CreateBox("Cyan Guide Stripe", new Vector3(lane * 3.8f, 0.06f, 0f), new Vector3(0.12f, 0.035f, 34f), cyanMaterial, false);
            CreateBox("Cyan Guide Stripe", new Vector3(0f, 0.06f, lane * 3.8f), new Vector3(34f, 0.035f, 0.12f), cyanMaterial, false);
        }

        BuildBoundaryWalls();
        BuildBoundaryDetails();
    }

    private static void BuildBoundaryWalls()
    {
        const float wallHeight = 5f;
        const float wallThickness = 0.8f;
        CreateWall("North Bulkhead", new Vector3(0f, wallHeight * 0.5f, 17.6f),
            new Vector3(36f, wallHeight, wallThickness));
        CreateWall("South Bulkhead", new Vector3(0f, wallHeight * 0.5f, -17.6f),
            new Vector3(36f, wallHeight, wallThickness));
        CreateWall("East Bulkhead", new Vector3(17.6f, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, 36f));
        CreateWall("West Bulkhead", new Vector3(-17.6f, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, 36f));

        for (var side = -1; side <= 1; side += 2)
        {
            for (var offset = -12f; offset <= 12f; offset += 4f)
            {
                CreateBox("Bulkhead Panel", new Vector3(offset, 3.55f, side * 17.08f),
                    new Vector3(3.6f, 1.7f, 0.18f), darkSteelMaterial, false);
                CreateBox("Bulkhead Panel", new Vector3(side * 17.08f, 3.55f, offset),
                    new Vector3(0.18f, 1.7f, 3.6f), darkSteelMaterial, false);

                CreateBox("Bulkhead Support", new Vector3(offset, 2.5f, side * 16.95f),
                    new Vector3(0.24f, 4.8f, 0.24f), steelMaterial, false);
                CreateBox("Bulkhead Support", new Vector3(side * 16.95f, 2.5f, offset),
                    new Vector3(0.24f, 4.8f, 0.24f), steelMaterial, false);
            }
        }

        for (var index = 0; index < 12; index++)
        {
            var x = -16.2f + index * 0.55f;
            CreateBox("North Caution Stripe", new Vector3(x, 0.035f, 14.65f),
                new Vector3(0.24f, 0.025f, 0.55f), index % 2 == 0 ? yellowMaterial : darkSteelMaterial, false);
            CreateBox("South Caution Stripe", new Vector3(x, 0.035f, -14.65f),
                new Vector3(0.24f, 0.025f, 0.55f), index % 2 == 0 ? yellowMaterial : darkSteelMaterial, false);
        }

        for (var index = 0; index < 12; index++)
        {
            var z = -16.2f + index * 0.55f;
            CreateBox("East Caution Stripe", new Vector3(14.65f, 0.035f, z),
                new Vector3(0.55f, 0.025f, 0.24f), index % 2 == 0 ? yellowMaterial : darkSteelMaterial, false);
            CreateBox("West Caution Stripe", new Vector3(-14.65f, 0.035f, z),
                new Vector3(0.55f, 0.025f, 0.24f), index % 2 == 0 ? yellowMaterial : darkSteelMaterial, false);
        }
    }

    private static void BuildBoundaryDetails()
    {
        for (var side = -1; side <= 1; side += 2)
        {
            for (var offset = -28f; offset <= 28f; offset += 8f)
            {
                if (Mathf.Abs(offset) < 4f || Mathf.Abs(offset) > 12f)
                    continue;

                CreateBox("Bulkhead Amber Marker", new Vector3(offset, 4.55f, side * 16.9f),
                    new Vector3(2.4f, 0.12f, 0.08f), orangeMaterial, false);
                CreateBox("Bulkhead Amber Marker", new Vector3(side * 16.9f, 4.55f, offset),
                    new Vector3(0.08f, 0.12f, 2.4f), orangeMaterial, false);
            }
        }
    }

    private static void BuildCentralCombatArea()
    {
        CreateBox("Central Combat Deck", new Vector3(0f, 0.18f, 0f), new Vector3(9f, 0.36f, 9f), darkSteelMaterial);
        CreateBox("Central Deck Top", new Vector3(0f, 0.38f, 0f), new Vector3(8.7f, 0.04f, 8.7f), steelMaterial, false);
        CreateRamp("North Deck Ramp", new Vector3(0f, 0.12f, -6.45f), new Vector3(4f, 0.2f, 4.1f), 0f, 0f);
        CreateRamp("South Deck Ramp", new Vector3(0f, 0.12f, 6.45f), new Vector3(4f, 0.2f, 4.1f), 0f, 0f);
        CreateRamp("East Deck Ramp", new Vector3(6.45f, 0.12f, 0f), new Vector3(4.1f, 0.2f, 4f), 0f, 0f);
        CreateRamp("West Deck Ramp", new Vector3(-6.45f, 0.12f, 0f), new Vector3(4.1f, 0.2f, 4f), 0f, 0f);

        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox("Deck Cyan Edge", new Vector3(side * 4.42f, 0.43f, 0f), new Vector3(0.12f, 0.08f, 8.9f), cyanMaterial, false);
            CreateBox("Deck Cyan Edge", new Vector3(0f, 0.43f, side * 4.42f), new Vector3(8.9f, 0.08f, 0.12f), cyanMaterial, false);
        }

        for (var x = -1; x <= 1; x += 2)
        {
            for (var z = -1; z <= 1; z += 2)
            {
                CreateBox("Deck Support", new Vector3(x * 3.6f, -0.25f, z * 3.6f), new Vector3(0.6f, 0.9f, 0.6f), steelMaterial);
                CreateBox("Deck Corner Beacon", new Vector3(x * 3.6f, 1.1f, z * 3.6f), new Vector3(0.2f, 1.2f, 0.2f), orangeMaterial, false);
            }
        }
    }

    private static void BuildCoverAndStructures()
    {
        var coverLocations = new[]
        {
            new Vector3(-10.5f, 0f, -5.5f), new Vector3(10.5f, 0f, 5.5f),
            new Vector3(-10.5f, 0f, 5.5f), new Vector3(10.5f, 0f, -5.5f),
            new Vector3(-13f, 0f, -5f), new Vector3(13f, 0f, 5f),
            new Vector3(-5f, 0f, 13f), new Vector3(5f, 0f, -13f)
        };

        for (var i = 0; i < coverLocations.Length; i++)
        {
            var barrier = CreateBox($"Metal Cover {i + 1}", coverLocations[i] + Vector3.up * 1.15f,
                new Vector3(i < 4 ? 4.5f : 5.5f, 2.3f, 0.8f), darkSteelMaterial);
            barrier.AddComponent<MetalBarrier>();
            CreateBox("Amber Cover Edge", coverLocations[i] + Vector3.up * 2.25f,
                new Vector3(i < 4 ? 4.5f : 5.5f, 0.12f, 0.88f), orangeMaterial, false);
            CreateHazardStripe(coverLocations[i], i < 4 ? 4.5f : 5.5f);
        }

        for (var side = -1; side <= 1; side += 2)
        {
            for (var direction = -1; direction <= 1; direction += 2)
            {
                var position = new Vector3(side * 14f, 0f, direction * 8f);
                BuildIndustrialPylon(position);
            }
        }

        BuildCatwalkTrusses();
    }

    private static void BuildIndustrialPylon(Vector3 position)
    {
        CreateBox("Industrial Pylon Base", position + Vector3.up * 0.45f, new Vector3(2f, 0.9f, 2f), darkSteelMaterial);
        CreateBox("Industrial Pylon Shaft", position + Vector3.up * 2f, new Vector3(1f, 2.2f, 1f), steelMaterial);
        CreateBox("Pylon Amber Signal", position + Vector3.up * 3.15f, new Vector3(1.2f, 0.14f, 1.2f), orangeMaterial, false);
        CreateBox("Pylon Cyan Light", position + Vector3.up * 2.45f, new Vector3(1.04f, 0.14f, 1.04f), cyanMaterial, false);
    }

    private static void BuildCatwalkTrusses()
    {
        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox("Overhead Truss", new Vector3(side * 15f, 5.8f, 0f), new Vector3(0.35f, 0.35f, 30f), darkSteelMaterial, false);
            CreateBox("Overhead Truss", new Vector3(0f, 5.8f, side * 15f), new Vector3(30f, 0.35f, 0.35f), darkSteelMaterial, false);
        }
    }

    private static void CreateHazardStripe(Vector3 position, float length)
    {
        CreateBox("Textured Hazard Stripe", position + new Vector3(0f, 0.16f, -0.415f),
            new Vector3(length, 0.035f, 0.08f), hazardStripeMaterial, false);
    }

    private static void BuildHazards()
    {
        var lavaPositions = new[]
        {
            new Vector3(-7.8f, 0.035f, 0f), new Vector3(7.8f, 0.035f, 0f)
        };

        for (var i = 0; i < lavaPositions.Length; i++)
        {
            var lavaSize = new Vector3(2.6f, 0.06f, 5.4f);
            var lavaSurface = CreateBox($"Lava Surface {i + 1}", lavaPositions[i], lavaSize, lavaMaterial, false);
            lavaSurface.transform.localScale = lavaSize;
            lavaSurface.AddComponent<RoboClashLavaVisual>();
            var lava = new GameObject($"Lava Pit {i + 1}");
            lava.transform.SetParent(root, false);
            lava.transform.position = lavaPositions[i] + Vector3.up * 0.9f;
            lava.AddComponent<BoxCollider>().size = new Vector3(lavaSize.x, 1.8f, lavaSize.z);
            lava.AddComponent<LavaPit>();

            var frameSize = new Vector3(lavaSize.x + 0.4f, 0.24f, lavaSize.z + 0.4f);
            CreateBox("Lava Pit Steel Frame", lavaPositions[i] + Vector3.down * 0.04f, frameSize, darkSteelMaterial);
            CreateBox("Lava Pit Amber Rim", lavaPositions[i] + new Vector3(0f, 0.08f, lavaSize.z * 0.5f + 0.08f),
                new Vector3(frameSize.x, 0.12f, 0.12f), orangeMaterial, false);
            CreateBox("Lava Pit Amber Rim", lavaPositions[i] + new Vector3(0f, 0.08f, -lavaSize.z * 0.5f - 0.08f),
                new Vector3(frameSize.x, 0.12f, 0.12f), orangeMaterial, false);
            CreateHazardStripe(lavaPositions[i] + new Vector3(0f, 0.15f, 0f), lavaSize.x);
        }

        var bridge = CreateBox("Traversing Bridge", new Vector3(-7.8f, 0.36f, 0f),
            new Vector3(3.1f, 0.45f, 2.8f), steelMaterial);
        bridge.AddComponent<Rigidbody>().isKinematic = true;
        bridge.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
        bridge.AddComponent<ArenaMovingPlatform>().Configure(new Vector3(0f, 0f, 2.4f), 4f);
        CreateBox("Bridge Cyan Guide", new Vector3(-7.8f, 0.61f, 0f), new Vector3(0.12f, 0.05f, 2.6f), cyanMaterial, false, bridge.transform);

        var spikeLocations = new[]
        {
            new Vector3(0f, 0.65f, -10.5f), new Vector3(0f, 0.65f, 10.5f),
            new Vector3(-10.5f, 0.65f, 0f), new Vector3(10.5f, 0.65f, 0f)
        };

        for (var i = 0; i < spikeLocations.Length; i++)
        {
            var trap = new GameObject($"Spike Trap {i + 1}");
            trap.transform.SetParent(root, false);
            trap.transform.position = spikeLocations[i];
            var trigger = trap.AddComponent<BoxCollider>();
            trigger.size = new Vector3(3.5f, 1.5f, 3.5f);
            trigger.isTrigger = true;
            trap.AddComponent<SpikeTrap>();
            CreateBox("Spike Trap Steel Platform", spikeLocations[i] - Vector3.up * 0.48f,
                new Vector3(3.5f, 0.3f, 3.5f), darkSteelMaterial);
            CreateHazardStripe(spikeLocations[i] - Vector3.up * 0.3f, 3.5f);
            for (var x = -1; x <= 1; x++)
            {
                for (var z = -1; z <= 1; z++)
                    CreateSpike(trap.transform, new Vector3(x * 0.9f, -0.28f, z * 0.9f));
            }
        }
    }

    private static ArenaSpawnZone[] BuildSpawnZones()
    {
        var positions = new[]
        {
            new Vector3(-12.5f, 0f, -12.5f), new Vector3(12.5f, 0f, -12.5f),
            new Vector3(-12.5f, 0f, 12.5f), new Vector3(12.5f, 0f, 12.5f)
        };
        var colors = new[] { blueMaterial, redMaterial, cyanMaterial, yellowMaterial };
        var zones = new ArenaSpawnZone[positions.Length];

        for (var i = 0; i < positions.Length; i++)
        {
            var direction = -positions[i].normalized;
            var zoneObject = new GameObject($"Spawn Zone {i + 1}");
            zoneObject.transform.SetParent(root, false);
            zoneObject.transform.SetPositionAndRotation(positions[i], Quaternion.LookRotation(direction, Vector3.up));
            zones[i] = zoneObject.AddComponent<ArenaSpawnZone>();
            zones[i].SetSpawnIndex(i);

            CreateBox("Spawn Platform", positions[i] + Vector3.up * 0.15f, new Vector3(5.5f, 0.3f, 5.5f), steelMaterial, true, zoneObject.transform);
            CreateBox("Spawn Zone Color Plate", positions[i] + Vector3.up * 0.32f,
                new Vector3(4.8f, 0.05f, 4.8f), colors[i], false, zoneObject.transform);
            CreateBox("Spawn Front Border", zoneObject.transform.TransformPoint(Vector3.up * 0.38f + Vector3.forward * 2.55f),
                new Vector3(5f, 0.08f, 0.12f), colors[i], false, zoneObject.transform);
            CreateBox("Spawn Rear Border", zoneObject.transform.TransformPoint(Vector3.up * 0.38f - Vector3.forward * 2.55f),
                new Vector3(5f, 0.08f, 0.12f), colors[i], false, zoneObject.transform);

            for (var x = -1; x <= 1; x += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    var marker = CreateBox("Spawn Beacon", zoneObject.transform.TransformPoint(new Vector3(x * 3f, 1.1f, z * 3f)),
                        new Vector3(0.25f, 1.4f, 0.25f), colors[i], false, zoneObject.transform);
                    marker.AddComponent<ArenaDecoration>();
                }
            }
        }

        return zones;
    }

    private static void BuildPickups()
    {
        var positions = new[]
        {
            new Vector3(-3.5f, 1.1f, -7f), new Vector3(3.5f, 1.1f, 7f),
            new Vector3(-12f, 1.1f, 0f), new Vector3(12f, 1.1f, 0f)
        };

        for (var i = 0; i < positions.Length; i++)
        {
            var pickup = new GameObject($"Pulse Weapon Pickup {i + 1}");
            pickup.transform.SetParent(root, false);
            pickup.transform.position = positions[i];
            var trigger = pickup.AddComponent<SphereCollider>();
            trigger.radius = 1.1f;
            trigger.isTrigger = true;
            pickup.AddComponent<ArenaWeaponPickup>();
            CreateSphere("Weapon Pickup Core", positions[i], Vector3.one * 0.65f,
                i % 2 == 0 ? orangeMaterial : cyanMaterial, false, pickup.transform);
            CreateBox("Weapon Pickup Ring", positions[i] + Vector3.down * 0.38f,
                new Vector3(1.3f, 0.08f, 1.3f), steelMaterial, false, pickup.transform);
        }

        var shieldPositions = new[] { new Vector3(-2f, 1.1f, 0f), new Vector3(2f, 1.1f, 0f) };
        for (var i = 0; i < shieldPositions.Length; i++)
        {
            var pickup = new GameObject($"Energy Shield Pickup {i + 1}");
            pickup.transform.SetParent(root, false);
            pickup.transform.position = shieldPositions[i];
            var trigger = pickup.AddComponent<SphereCollider>();
            trigger.radius = 1.1f;
            trigger.isTrigger = true;
            pickup.AddComponent<ArenaShieldPickup>();
            CreateSphere("Shield Pickup Core", shieldPositions[i], Vector3.one * 0.6f, cyanMaterial, false, pickup.transform);
            CreateBox("Shield Pickup Base", shieldPositions[i] + Vector3.down * 0.42f, new Vector3(1.3f, 0.08f, 1.3f), steelMaterial, false, pickup.transform);
        }

        CreateHealthPickup("Health Pickup A", new Vector3(-10f, 1.1f, -10f));
        CreateHealthPickup("Health Pickup B", new Vector3(10f, 1.1f, 10f));
    }

    private static void CreateHealthPickup(string name, Vector3 position)
    {
        var pickup = new GameObject(name);
        pickup.transform.SetParent(root, false);
        pickup.transform.position = position;
        var trigger = pickup.AddComponent<SphereCollider>();
        trigger.radius = 1.1f;
        trigger.isTrigger = true;
        pickup.AddComponent<ArenaHealthPickup>();
        CreateSphere("Health Pickup Core", position, Vector3.one * 0.6f,
            healthMaterial, false, pickup.transform);
        CreateBox("Health Pickup Base", position + Vector3.down * 0.42f,
            new Vector3(1.3f, 0.08f, 1.3f), steelMaterial, false, pickup.transform);
    }

    private static void BuildEnergyShield()
    {
        var shield = CreateSphere("Rechargeable Energy Shield", new Vector3(0f, 1.5f, 0f), Vector3.one * 2.8f, shieldMaterial, true);
        var trigger = shield.GetComponent<SphereCollider>();
        trigger.radius = 0.5f;
        trigger.isTrigger = true;
        shield.AddComponent<ArenaEnergyShield>();
        CreateBox("Shield Generator Base", new Vector3(0f, 0.2f, 0f), new Vector3(1.6f, 0.3f, 1.6f), darkSteelMaterial);
        CreateCylinder("Shield Generator Coil", new Vector3(0f, 0.85f, 0f), new Vector3(0.35f, 0.8f, 0.35f), cyanMaterial, false);
    }

    private static void BuildTrainingTargets(ArenaSpawnZone[] zones)
    {
        for (var i = 1; i < zones.Length; i++)
        {
            var target = new GameObject($"Training Bot {i}");
            target.transform.SetParent(root, false);
            target.transform.SetPositionAndRotation(zones[i].transform.position + zones[i].transform.forward * 7f,
                zones[i].transform.rotation);
            var controller = target.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.48f;
            controller.center = Vector3.up;
            controller.stepOffset = 0.3f;
            target.AddComponent<ArenaDamageable>();
            target.AddComponent<ArenaBotController>();
            BuildRobotVisual(target.transform, i % 2 == 0 ? orangeMaterial : cyanMaterial);
        }
    }

    private static void BuildPlayer(ArenaSpawnZone spawnZone)
    {
        var player = new GameObject("Player Robot");
        player.transform.SetParent(root, false);
        player.transform.SetPositionAndRotation(spawnZone.transform.position + Vector3.up * 0.35f, spawnZone.transform.rotation);
        var controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.48f;
        controller.center = Vector3.up;
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 50f;
        var robot = player.AddComponent<RobotArenaController>();

        BuildRobotVisual(player.transform, cyanMaterial);

        var muzzleObject = new GameObject("Weapon Muzzle");
        muzzleObject.transform.SetParent(player.transform, false);
        muzzleObject.transform.localPosition = new Vector3(0f, 1.35f, 0.7f);
        var camera = Camera.main;
        if (camera == null)
            camera = UnityEngine.Object.FindAnyObjectByType<Camera>();

        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.enabled = true;
        robot.Configure(camera, muzzleObject.transform, spawnZone);
    }

    private static void BuildRobotVisual(Transform parent, Material accent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MechPrefabPath);
        if (prefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Imported Mech Visual";
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                if (bounds.size.y > 0.01f)
                {
                    var scale = 2f / bounds.size.y;
                    instance.transform.localScale = Vector3.one * scale;
                    instance.transform.localPosition = Vector3.up * (-(bounds.min.y - parent.position.y) * scale);
                }
            }

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            return;
        }

        CreateBox("Robot Torso", parent.position + Vector3.up * 1.15f, new Vector3(0.85f, 0.9f, 0.55f), steelMaterial, false, parent);
        CreateBox("Robot Head", parent.position + Vector3.up * 1.82f, new Vector3(0.58f, 0.48f, 0.5f), darkSteelMaterial, false, parent);
        CreateBox("Robot Visor", parent.position + Vector3.up * 1.87f + parent.forward * 0.27f,
            new Vector3(0.42f, 0.12f, 0.04f), accent, false, parent);
        CreateBox("Robot Left Shoulder", parent.position + parent.right * -0.62f + Vector3.up * 1.4f,
            new Vector3(0.3f, 0.62f, 0.36f), accent, false, parent);
        CreateBox("Robot Right Shoulder", parent.position + parent.right * 0.62f + Vector3.up * 1.4f,
            new Vector3(0.3f, 0.62f, 0.36f), accent, false, parent);
        CreateBox("Robot Left Leg", parent.position + parent.right * -0.23f + Vector3.up * 0.46f,
            new Vector3(0.32f, 0.72f, 0.4f), darkSteelMaterial, false, parent);
        CreateBox("Robot Right Leg", parent.position + parent.right * 0.23f + Vector3.up * 0.46f,
            new Vector3(0.32f, 0.72f, 0.4f), darkSteelMaterial, false, parent);
    }

    private static void BuildLights()
    {
        var directional = new GameObject("Arena Key Light");
        directional.transform.SetParent(root, false);
        directional.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
        var sun = directional.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.67f, 0.82f, 1f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;

        for (var i = -1; i <= 1; i += 2)
        {
            for (var j = -1; j <= 1; j += 2)
            {
                CreatePointLight($"Cyan Arena Flood {i} {j}", new Vector3(i * 13f, 4.4f, j * 13f),
                    new Color(0.08f, 0.45f, 1f), 520f, 12f);
                CreatePointLight($"Amber Arena Flood {i} {j}", new Vector3(i * 7f, 3f, j * 7f),
                    new Color(1f, 0.22f, 0.05f), 190f, 7f);
            }
        }

        CreatePointLight("Central Reactor Glow", new Vector3(0f, 1.5f, 0f), new Color(0.03f, 0.55f, 1f), 300f, 5f);
        foreach (var position in new[] { new Vector3(-7.8f, 0.8f, 0f), new Vector3(7.8f, 0.8f, 0f) })
            CreatePointLight("Lava Glow", position, new Color(1f, 0.13f, 0.015f), 270f, 4f);

        CreatePointLight("Red Reactor Warning", new Vector3(0f, 3.8f, 0f), new Color(1f, 0.035f, 0.02f), 180f, 7f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.12f, 0.19f, 0.26f);
        RenderSettings.ambientEquatorColor = new Color(0.055f, 0.075f, 0.09f);
        RenderSettings.ambientGroundColor = new Color(0.025f, 0.03f, 0.04f);
    }

    private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(root, false);
        lightObject.transform.position = position;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static GameObject CreateWall(string name, Vector3 position, Vector3 size)
    {
        return CreateWall(name, position, size, null);
    }

    private static GameObject CreateWall(string name, Vector3 position, Vector3 size, Transform parent)
    {
        var wall = CreateBox(name, position, size, darkSteelMaterial, true, parent);
        wall.AddComponent<ArenaWall>();
        var visual = new GameObject("Modular Wall Armor");
        visual.transform.SetParent(wall.transform, false);
        visual.transform.localScale = new Vector3(
            1f / Mathf.Max(0.001f, size.x),
            1f / Mathf.Max(0.001f, size.y),
            1f / Mathf.Max(0.001f, size.z));
        visual.AddComponent<MeshFilter>().sharedMesh = GetOrCreateWallVisualMesh(size);
        visual.AddComponent<MeshRenderer>().sharedMaterials = wallVisualMaterials;
        return wall;
    }

    private static void BuildFloorDetails(string detailsName, string meshKeyPrefix, float width, float depth)
    {
        var key = string.Format(
            "{0}_{1}_{2}_{3}",
            meshKeyPrefix,
            Mathf.RoundToInt(width * 100f),
            Mathf.RoundToInt(depth * 100f),
            (int)activeDetailQuality);
        if (!floorVisualMeshCache.TryGetValue(key, out var mesh))
        {
            var path = MeshesFolder + "/" + key + ".asset";
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                var builder = new WallVisualMeshBuilder(true);
                var spacing = activeDetailQuality == ArenaVisualDetailQuality.Performance
                    ? 18f
                    : activeDetailQuality == ArenaVisualDetailQuality.HighDetail ? 9f : 12f;
                const float seamWidth = 0.035f;
                var xSeams = new List<float>();
                var zSeams = new List<float>();
                for (var x = -width * 0.5f + spacing; x < width * 0.5f; x += spacing)
                    xSeams.Add(x);
                for (var z = -depth * 0.5f + spacing; z < depth * 0.5f; z += spacing)
                    zSeams.Add(z);

                foreach (var x in xSeams)
                {
                    var segmentStart = -depth * 0.5f + 3f;
                    foreach (var z in zSeams)
                    {
                        var segmentEnd = z - seamWidth * 0.5f;
                        if (segmentEnd > segmentStart)
                            builder.AddBox(new Vector3(x, 0.009f, (segmentStart + segmentEnd) * 0.5f),
                                new Vector3(seamWidth, 0.018f, segmentEnd - segmentStart), WallDarkPanelSubmesh);
                        segmentStart = z + seamWidth * 0.5f;
                    }

                    var finalSegmentEnd = depth * 0.5f - 3f;
                    if (finalSegmentEnd > segmentStart)
                        builder.AddBox(new Vector3(x, 0.009f, (segmentStart + finalSegmentEnd) * 0.5f),
                            new Vector3(seamWidth, 0.018f, finalSegmentEnd - segmentStart), WallDarkPanelSubmesh);
                }

                foreach (var z in zSeams)
                    builder.AddBox(new Vector3(0f, 0.009f, z),
                        new Vector3(width - 6f, 0.018f, seamWidth), WallDarkPanelSubmesh);

                builder.AddBox(new Vector3(0f, 0.018f, depth * 0.5f - 2.5f),
                    new Vector3(width - 6f, 0.035f, 0.12f), WallTrimSubmesh);
                builder.AddBox(new Vector3(0f, 0.018f, -depth * 0.5f + 2.5f),
                    new Vector3(width - 6f, 0.035f, 0.12f), WallTrimSubmesh);
                builder.AddBox(new Vector3(width * 0.5f - 2.5f, 0.018f, 0f),
                    new Vector3(0.12f, 0.035f, depth - 6f), WallTrimSubmesh);
                builder.AddBox(new Vector3(-width * 0.5f + 2.5f, 0.018f, 0f),
                    new Vector3(0.12f, 0.035f, depth - 6f), WallTrimSubmesh);
                mesh = builder.Build(key);
                AssetDatabase.CreateAsset(mesh, path);
            }

            floorVisualMeshCache[key] = mesh;
        }

        var details = new GameObject(detailsName);
        details.transform.SetParent(root, false);
        details.AddComponent<MeshFilter>().sharedMesh = mesh;
        details.AddComponent<MeshRenderer>().sharedMaterials = wallVisualMaterials;
    }

    private static void CreateRamp(string name, Vector3 position, Vector3 size, float xRotation, float zRotation)
    {
        var ramp = CreateBox(name, position, size, steelMaterial);
        ramp.transform.rotation = Quaternion.Euler(xRotation, 0f, zRotation);
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 size, Material material,
        bool collider = true, Transform parent = null)
    {
        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.SetParent(parent != null ? parent : root, false);
        gameObject.transform.position = position;
        gameObject.transform.localScale = size;
        gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!collider)
            UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
        else if (gameObject.GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();
        return gameObject;
    }

    private static GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Material material,
        bool collider = true, Transform parent = null)
    {
        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = name;
        gameObject.transform.SetParent(parent != null ? parent : root, false);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!collider)
            UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
        return gameObject;
    }

    private static GameObject CreateCylinder(
        string name, Vector3 position, Vector3 scale, Material material, bool collider,
        Transform parent = null)
    {
        var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gameObject.name = name;
        gameObject.transform.SetParent(parent != null ? parent : root, false);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!collider)
            UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
        return gameObject;
    }

    private static void CreateSpike(Transform parent, Vector3 localPosition)
    {
        var spike = new GameObject("Spike");
        spike.transform.SetParent(parent, false);
        spike.transform.localPosition = localPosition;
        spike.transform.localScale = new Vector3(0.72f, 0.8f, 0.72f);
        var filter = spike.AddComponent<MeshFilter>();
        filter.sharedMesh = spikeMesh;
        spike.AddComponent<MeshRenderer>().sharedMaterial = orangeMaterial;
    }
}

public sealed class RoboClashLargeArenaWindow : EditorWindow
{
    [SerializeField] private RoboClashArenaSettings settings = new RoboClashArenaSettings();
    private bool generationScheduled;

    public static void ShowWindow()
    {
        var window = GetWindow<RoboClashLargeArenaWindow>("Large RoboClash Arena");
        window.minSize = new Vector2(380f, 500f);
        window.Show();
    }

    private void OnGUI()
    {
        if (settings == null)
            settings = new RoboClashArenaSettings();

        EditorGUILayout.LabelField("Large Multi-Room Combat Complex", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Generates into the existing RoboClash Arena root in the active scene. " +
            "Only that generated root is replaced; cameras, UI, robot prefabs, and unrelated scene objects are preserved.",
            MessageType.Info);

        settings.mapWidth = Mathf.Max(120f, EditorGUILayout.FloatField("Map Width", settings.mapWidth));
        settings.mapDepth = Mathf.Max(120f, EditorGUILayout.FloatField("Map Depth", settings.mapDepth));
        settings.corridorWidth = Mathf.Clamp(
            EditorGUILayout.FloatField("Corridor Width", settings.corridorWidth), 6f, 14f);
        settings.wallHeight = Mathf.Max(4f, EditorGUILayout.FloatField("Wall Height", settings.wallHeight));
        settings.wallThickness = Mathf.Clamp(EditorGUILayout.FloatField("Wall Thickness", settings.wallThickness),
            0.4f, Mathf.Min(settings.mapWidth, settings.mapDepth) * 0.05f);

        var roomDepthX = Mathf.Clamp(settings.mapWidth * 0.2f, 24f, 80f);
        var roomDepthZ = Mathf.Clamp(settings.mapDepth * 0.2f, 24f, 80f);
        var maxArenaWidth = Mathf.Max(24f, Mathf.Min(settings.mapWidth * 0.45f,
            settings.mapWidth - 16f - 2f * roomDepthX - 2f * settings.corridorWidth));
        var maxArenaDepth = Mathf.Max(24f, Mathf.Min(settings.mapDepth * 0.45f,
            settings.mapDepth - 16f - 2f * roomDepthZ - 2f * settings.corridorWidth));
        settings.centralArenaWidth = Mathf.Clamp(
            EditorGUILayout.FloatField("Central Arena Width", settings.centralArenaWidth), 24f, maxArenaWidth);
        settings.centralArenaDepth = Mathf.Clamp(
            EditorGUILayout.FloatField("Central Arena Depth", settings.centralArenaDepth), 24f, maxArenaDepth);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Generated Features", EditorStyles.boldLabel);
        settings.generateSideRooms = EditorGUILayout.Toggle("Four Side Rooms", settings.generateSideRooms);
        settings.generateCover = EditorGUILayout.Toggle("Combat Cover", settings.generateCover);
        settings.generateHazards = EditorGUILayout.Toggle("Hazards", settings.generateHazards);
        settings.generateElevatedAreas = EditorGUILayout.Toggle("Low Catwalk and Ramps", settings.generateElevatedAreas);
        settings.generatePickups = EditorGUILayout.Toggle("Weapon and Shield Pickups", settings.generatePickups);
        settings.visualDetailQuality = (ArenaVisualDetailQuality)EditorGUILayout.EnumPopup(
            "Visual Detail Quality", settings.visualDetailQuality);

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "Default layout: a 120 x 120 arena, central hub, four themed rooms, a traversable loop corridor, " +
            "four cardinal room connections, and four safe corner spawns. Visual detail changes decoration density only. " +
            "The generator does not add NavMesh dependencies. Save the gameplay scene after generation; Play Mode starts at the main menu.",
            MessageType.None);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            using (new EditorGUI.DisabledScope(generationScheduled))
            {
                if (GUILayout.Button("Generate Large Arena", GUILayout.Height(34f)))
                {
                    generationScheduled = true;
                    var generationSettings = CopySettings(settings);
                    EditorApplication.delayCall += () =>
                    {
                        generationScheduled = false;
                        if (this == null)
                            return;
                        if (EditorApplication.isPlaying)
                        {
                            Debug.LogWarning("Large arena generation was canceled because Play Mode started.");
                            Repaint();
                            return;
                        }

                        RoboClashArenaGenerator.GenerateLargeArena(generationSettings);
                        Repaint();
                    };
                }
            }
        }

        if (EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("Stop Play Mode before generating or replacing scene content.", MessageType.Warning);
    }

    private static RoboClashArenaSettings CopySettings(RoboClashArenaSettings source)
    {
        return new RoboClashArenaSettings
        {
            mapWidth = source.mapWidth,
            mapDepth = source.mapDepth,
            corridorWidth = source.corridorWidth,
            centralArenaWidth = source.centralArenaWidth,
            centralArenaDepth = source.centralArenaDepth,
            wallHeight = source.wallHeight,
            wallThickness = source.wallThickness,
            generateSideRooms = source.generateSideRooms,
            generateCover = source.generateCover,
            generateHazards = source.generateHazards,
            generateElevatedAreas = source.generateElevatedAreas,
            generatePickups = source.generatePickups,
            visualDetailQuality = source.visualDetailQuality
        };
    }
}
