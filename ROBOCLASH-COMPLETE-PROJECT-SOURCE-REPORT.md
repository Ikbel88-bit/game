# RoboClash - Complete Project Source and Configuration Report

**Refreshed:** 2026-10-25  
**Project:** RoboClash, Unity 6.6 (`6000.6.4f1`)  
**Workspace:** `C:\Users\moham\RoboClash`

This report contains text snapshots of current application source and key Unity project configuration. Regenerate it after source or configuration changes to keep it current.

## Current project and implementation status

- Menu navigation, `RoboClashUIEventSystem`, and scene-ownership safeguards were left unchanged.
- Runtime and Editor assembly compilation passed with zero warnings and zero errors. The check included the newly added C# files through a temporary MSBuild include file because the generated Unity project files had not yet refreshed.
- Implemented gameplay fixes for projectile flight, separated ability/ultimate lifetimes, damage feedback, audio range, AI attack behavior, temporary damage buffs, pause cursor restoration, camera feedback, FOV/sensitivity preferences, and robot balance.
- Added runtime NavMesh construction with lava and spike exclusion volumes, AI path-corner steering, and cached path corners to avoid repeated path-array allocations.
- Added reusable respawning health pickups and generated pickup placements in both compact and large arena layouts.
- Added world-space UVs for procedural arena floor details and an Editor menu tool for URP post-processing and a reflection probe.
- The VFX material creator and visual-effects setup tool have not been run in the Unity Editor. Play Mode, Windows player build, sound audition, and arena regeneration were not tested.
- 26 Kenney Sci-fi Sounds OGG clips remain under `Assets/Resources/RoboClash/Audio`; Unity import and audible Play Mode checks are still pending.

## Scope and exclusions

- Includes C# and supported text-based shader, UI, assembly-definition, and Input System action-map files under `Assets`.
- Includes the package manifest and key Unity version, player, editor, and build-scene settings.
- Excludes scenes, prefabs, models, textures, audio binaries, `.meta` files, generated caches, Unity/package implementation sources, and build outputs.
- Source inventory: 53 Assets source files; 5 project configuration files are embedded below.

## File index
- `Assets/Editor/RoboClashArenaGenerator.cs`
- `Assets/Editor/RoboClashArenaVisualEffectsTool.cs`
- `Assets/Editor/RoboClashGameplayUIGenerator.cs`
- `Assets/Editor/RoboClashMenuSceneGenerator.cs`
- `Assets/Editor/RoboClashReleaseValidator.cs`
- `Assets/Editor/RoboClashRobotRosterGenerator.cs`
- `Assets/Editor/RoboClashValidationTool.cs`
- `Assets/Editor/RoboClashVFXMaterialCreator.cs`
- `Assets/InputSystem_Actions.inputactions`
- `Assets/Robot&Pilot/Assets/Script/AnimateEmission.cs`
- `Assets/Scripts/RoboClash/ArenaBotController.cs`
- `Assets/Scripts/RoboClash/ArenaDamageable.cs`
- `Assets/Scripts/RoboClash/ArenaDecoration.cs`
- `Assets/Scripts/RoboClash/ArenaEnergyShield.cs`
- `Assets/Scripts/RoboClash/ArenaFloor.cs`
- `Assets/Scripts/RoboClash/ArenaHazard.cs`
- `Assets/Scripts/RoboClash/ArenaHealthPickup.cs`
- `Assets/Scripts/RoboClash/ArenaMovingPlatform.cs`
- `Assets/Scripts/RoboClash/ArenaProjectile.cs`
- `Assets/Scripts/RoboClash/ArenaShieldPickup.cs`
- `Assets/Scripts/RoboClash/ArenaSpawnZone.cs`
- `Assets/Scripts/RoboClash/ArenaWall.cs`
- `Assets/Scripts/RoboClash/ArenaWeaponPickup.cs`
- `Assets/Scripts/RoboClash/KingOfArenaZone.cs`
- `Assets/Scripts/RoboClash/LavaPit.cs`
- `Assets/Scripts/RoboClash/MetalBarrier.cs`
- `Assets/Scripts/RoboClash/RoboClashCombatVFX.cs`
- `Assets/Scripts/RoboClash/RoboClashMatchManager.cs`
- `Assets/Scripts/RoboClash/RoboClashPulseRing.cs`
- `Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs`
- `Assets/Scripts/RoboClash/RoboClashRobotAI.cs`
- `Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs`
- `Assets/Scripts/RoboClash/RoboClashRobotAudio.cs`
- `Assets/Scripts/RoboClash/RoboClashRobotCombat.cs`
- `Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs`
- `Assets/Scripts/RoboClash/RoboClashRobotFactory.cs`
- `Assets/Scripts/RoboClash/RoboClashRobotMovement.cs`
- `Assets/Scripts/RoboClash/RoboClashRobotVFX.cs`
- `Assets/Scripts/RoboClash/RoboClashSaveService.cs`
- `Assets/Scripts/RoboClash/RobotArenaController.cs`
- `Assets/Scripts/RoboClash/SpikeTrap.cs`
- `Assets/Scripts/RoboClash/UI/RoboClashHUD.cs`
- `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`
- `Assets/Scripts/RoboClash/UI/RoboClashMinimap.cs`
- `Assets/Scripts/RoboClash/UI/RoboClashSession.cs`
- `Assets/Scripts/RoboClash/UI/RoboClashSettingsPanel.cs`
- `Assets/Scripts/RoboClash/Visuals/RoboClashArenaVisuals.cs`
- `Assets/Scripts/RoboClash/Visuals/RoboClashLavaVisual.cs`
- `Assets/TutorialInfo/Editor/ReadmeEditor.cs`
- `Assets/TutorialInfo/Readme.cs`
- `Assets/TutorialInfo/StyleSheets/ReadmeEditor.uss`
- `Assets/TutorialInfo/StyleSheets/ReadmeEditorDark.uss`
- `Assets/TutorialInfo/StyleSheets/ReadmeEditorLight.uss`
- `Packages/manifest.json`
- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/EditorSettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`

## Complete source and configuration contents

### `Assets/Editor/RoboClashArenaGenerator.cs`

````csharp
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
        var scene = SceneManager.GetActiveScene();
        if (!IsGameplayScene(scene))
        {
            Debug.LogError(
                $"Arena generation requires '{GameplayScenePath}' to be the active scene. " +
                $"The active scene is '{scene.path}'. Open the gameplay scene and try again.");
            return;
        }

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
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Robo Clash arena generated. Inspect the scene, save it, and test movement, combat, hazards, and pickups in Play mode.",
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

        var scene = SceneManager.GetActiveScene();
        if (!IsGameplayScene(scene))
        {
            Debug.LogError(
                $"Arena generation requires '{GameplayScenePath}' to be the active scene. " +
                $"The active scene is '{scene.path}'. Open the gameplay scene and try again.");
            return;
        }

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
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Generated a {settings.mapWidth:0} x {settings.mapDepth:0} connected RoboClash combat complex. " +
                "Save the scene, run Tools > RoboClash > Validate Scene Setup, and test routes and combat in Play Mode.",
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
            "The generator does not add NavMesh dependencies.",
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
````

### `Assets/Editor/RoboClashArenaVisualEffectsTool.cs`

````csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using RoboClash;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RoboClashArenaVisualEffectsTool
{
    private const string VolumeObjectName = "RoboClash Arena Post Processing";
    private const string ReflectionObjectName = "RoboClash Arena Reflection Probe";
    private const string ProfileFolder = "Assets/Generated/RoboClash/Volumes";
    private const string ProfilePath = ProfileFolder + "/ArenaVolumeProfile.asset";

    [MenuItem("Tools/RoboClash/Configure Arena Visual Effects")]
    public static void Configure()
    {
        EnsureProfileFolder();
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "RoboClash Arena Volume Profile";
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        ConfigureProfile(profile);
        ConfigureVolume(profile);
        ConfigureReflectionProbe();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("RoboClash arena post-processing and reflection probe are configured.");
    }

    private static void EnsureProfileFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder("Assets/Generated/RoboClash"))
            AssetDatabase.CreateFolder("Assets/Generated", "RoboClash");
        if (!AssetDatabase.IsValidFolder(ProfileFolder))
            AssetDatabase.CreateFolder("Assets/Generated/RoboClash", "Volumes");
    }

    private static void ConfigureProfile(VolumeProfile profile)
    {
        profile.components.Clear();
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(0.65f);
        bloom.scatter.Override(0.55f);

        var color = profile.Add<ColorAdjustments>(true);
        color.contrast.Override(12f);
        color.saturation.Override(6f);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.18f);
        vignette.smoothness.Override(0.45f);
        EditorUtility.SetDirty(profile);
    }

    private static void ConfigureVolume(VolumeProfile profile)
    {
        var volumeObject = FindSceneObject(VolumeObjectName);
        if (volumeObject == null)
            volumeObject = new GameObject(VolumeObjectName);

        var volume = volumeObject.GetComponent<Volume>();
        if (volume == null)
            volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volumeObject);
    }

    private static void ConfigureReflectionProbe()
    {
        var probeObject = FindSceneObject(ReflectionObjectName);
        if (probeObject == null)
            probeObject = new GameObject(ReflectionObjectName);

        var probe = probeObject.GetComponent<ReflectionProbe>();
        if (probe == null)
            probe = probeObject.AddComponent<ReflectionProbe>();

        var arena = GameObject.Find("RoboClash Arena");
        var floorCollider = arena != null ? arena.GetComponentInChildren<ArenaFloor>()?.GetComponent<Collider>() : null;
        var bounds = floorCollider != null ? floorCollider.bounds : new Bounds(Vector3.zero, new Vector3(120f, 20f, 120f));
        probeObject.transform.position = bounds.center + Vector3.up * 4f;
        probe.center = Vector3.zero;
        probe.size = new Vector3(bounds.size.x, Mathf.Max(16f, bounds.size.y + 12f), bounds.size.z);
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.resolution = 128;
        probe.boxProjection = true;
        probe.intensity = 0.8f;
        EditorUtility.SetDirty(probeObject);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (var candidate in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (candidate.scene.IsValid() && candidate.scene.isLoaded && candidate.name == objectName)
                return candidate;
        }

        return null;
    }
}
````

### `Assets/Editor/RoboClashGameplayUIGenerator.cs`

````csharp
using RoboClash;
using RoboClash.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoboClashGameplayUIGenerator
{
    private const string GameplayScenePath = "Assets/Scenes/RoboClashGameplay.unity";

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
````

### `Assets/Editor/RoboClashMenuSceneGenerator.cs`

````csharp
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
    private const string ArenaScenePath = "Assets/Scenes/RoboClashGameplay.unity";
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
````

### `Assets/Editor/RoboClashReleaseValidator.cs`

````csharp
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
    private const string GameplayScenePath = "Assets/Scenes/RoboClashGameplay.unity";
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
````

### `Assets/Editor/RoboClashRobotRosterGenerator.cs`

````csharp
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
````

### `Assets/Editor/RoboClashValidationTool.cs`

````csharp
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
````

### `Assets/Editor/RoboClashVFXMaterialCreator.cs`

````csharp
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RoboClashVFXMaterialCreator
{
    private const string Folder = "Assets/Resources/RoboClash/VFX";
    private const string MaterialPath = Folder + "/ParticleUnlit.mat";

    [MenuItem("Tools/RoboClash/Create VFX Materials")]
    public static void Create()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogError("URP Particles/Unlit shader not found. Is the URP package installed?");
            return;
        }

        Directory.CreateDirectory(Folder);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "ParticleUnlit" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.shader = shader;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);

        AddAlwaysIncludedShader("Universal Render Pipeline/Particles/Unlit");
        AddAlwaysIncludedShader("Universal Render Pipeline/Lit");
        AssetDatabase.SaveAssets();
        Debug.Log("RoboClash VFX material created at " + MaterialPath);
    }

    private static void AddAlwaysIncludedShader(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null)
            return;

        var settings = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (settings == null)
            return;

        var serialized = new SerializedObject(settings);
        var list = serialized.FindProperty("m_AlwaysIncludedShaders");
        if (list == null)
            return;

        for (var i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                return;
        }

        list.InsertArrayElementAtIndex(list.arraySize);
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
        serialized.ApplyModifiedProperties();
    }
}
````

### `Assets/InputSystem_Actions.inputactions`

````json
{
    "name": "InputSystem_Actions",
    "maps": [
        {
            "name": "Player",
            "id": "df70fa95-8a34-4494-b137-73ab6b9c7d37",
            "actions": [
                {
                    "name": "Move",
                    "type": "Value",
                    "id": "351f2ccd-1f9f-44bf-9bec-d62ac5c5f408",
                    "expectedControlType": "Vector2",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": true
                },
                {
                    "name": "Look",
                    "type": "Value",
                    "id": "6b444451-8a00-4d00-a97e-f47457f736a8",
                    "expectedControlType": "Vector2",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": true
                },
                {
                    "name": "Attack",
                    "type": "Button",
                    "id": "6c2ab1b8-8984-453a-af3d-a3c78ae1679a",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Interact",
                    "type": "Button",
                    "id": "852140f2-7766-474d-8707-702459ba45f3",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "Hold",
                    "initialStateCheck": false
                },
                {
                    "name": "Crouch",
                    "type": "Button",
                    "id": "27c5f898-bc57-4ee1-8800-db469aca5fe3",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Jump",
                    "type": "Button",
                    "id": "f1ba0d36-48eb-4cd5-b651-1c94a6531f70",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Previous",
                    "type": "Button",
                    "id": "2776c80d-3c14-4091-8c56-d04ced07a2b0",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Next",
                    "type": "Button",
                    "id": "b7230bb6-fc9b-4f52-8b25-f5e19cb2c2ba",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Sprint",
                    "type": "Button",
                    "id": "641cd816-40e6-41b4-8c3d-04687c349290",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                }
            ],
            "bindings": [
                {
                    "name": "",
                    "id": "978bfe49-cc26-4a3d-ab7b-7d7a29327403",
                    "path": "<Gamepad>/leftStick",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "WASD",
                    "id": "00ca640b-d935-4593-8157-c05846ea39b3",
                    "path": "Dpad",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": true,
                    "isPartOfComposite": false
                },
                {
                    "name": "up",
                    "id": "e2062cb9-1b15-46a2-838c-2f8d72a0bdd9",
                    "path": "<Keyboard>/w",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "up",
                    "id": "8180e8bd-4097-4f4e-ab88-4523101a6ce9",
                    "path": "<Keyboard>/upArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "320bffee-a40b-4347-ac70-c210eb8bc73a",
                    "path": "<Keyboard>/s",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "1c5327b5-f71c-4f60-99c7-4e737386f1d1",
                    "path": "<Keyboard>/downArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "d2581a9b-1d11-4566-b27d-b92aff5fabbc",
                    "path": "<Keyboard>/a",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "2e46982e-44cc-431b-9f0b-c11910bf467a",
                    "path": "<Keyboard>/leftArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "fcfe95b8-67b9-4526-84b5-5d0bc98d6400",
                    "path": "<Keyboard>/d",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "77bff152-3580-4b21-b6de-dcd0c7e41164",
                    "path": "<Keyboard>/rightArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "",
                    "id": "1635d3fe-58b6-4ba9-a4e2-f4b964f6b5c8",
                    "path": "<XRController>/{Primary2DAxis}",
                    "interactions": "",
                    "processors": "",
                    "groups": "XR",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "3ea4d645-4504-4529-b061-ab81934c3752",
                    "path": "<Joystick>/stick",
                    "interactions": "",
                    "processors": "",
                    "groups": "Joystick",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "c1f7a91b-d0fd-4a62-997e-7fb9b69bf235",
                    "path": "<Gamepad>/rightStick",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Look",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "8c8e490b-c610-4785-884f-f04217b23ca4",
                    "path": "<Pointer>/delta",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse;Touch",
                    "action": "Look",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "3e5f5442-8668-4b27-a940-df99bad7e831",
                    "path": "<Joystick>/{Hatswitch}",
                    "interactions": "",
                    "processors": "",
                    "groups": "Joystick",
                    "action": "Look",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "143bb1cd-cc10-4eca-a2f0-a3664166fe91",
                    "path": "<Gamepad>/buttonWest",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Attack",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "05f6913d-c316-48b2-a6bb-e225f14c7960",
                    "path": "<Mouse>/leftButton",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Attack",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "886e731e-7071-4ae4-95c0-e61739dad6fd",
                    "path": "<Touchscreen>/primaryTouch/tap",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Touch",
                    "action": "Attack",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "ee3d0cd2-254e-47a7-a8cb-bc94d9658c54",
                    "path": "<Joystick>/trigger",
                    "interactions": "",
                    "processors": "",
                    "groups": "Joystick",
                    "action": "Attack",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "8255d333-5683-4943-a58a-ccb207ff1dce",
                    "path": "<XRController>/{PrimaryAction}",
                    "interactions": "",
                    "processors": "",
                    "groups": "XR",
                    "action": "Attack",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "b3c1c7f0-bd20-4ee7-a0f1-899b24bca6d7",
                    "path": "<Keyboard>/enter",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Attack",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "cbac6039-9c09-46a1-b5f2-4e5124ccb5ed",
                    "path": "<Keyboard>/2",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Next",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "e15ca19d-e649-4852-97d5-7fe8ccc44e94",
                    "path": "<Gamepad>/dpad/right",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "Next",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "f2e9ba44-c423-42a7-ad56-f20975884794",
                    "path": "<Keyboard>/leftShift",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Sprint",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "8cbb2f4b-a784-49cc-8d5e-c010b8c7f4e6",
                    "path": "<Gamepad>/leftStickPress",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "Sprint",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "d8bf24bf-3f2f-4160-a97c-38ec1eb520ba",
                    "path": "<XRController>/trigger",
                    "interactions": "",
                    "processors": "",
                    "groups": "XR",
                    "action": "Sprint",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "eb40bb66-4559-4dfa-9a2f-820438abb426",
                    "path": "<Keyboard>/space",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Jump",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "daba33a1-ad0c-4742-a909-43ad1cdfbeb6",
                    "path": "<Gamepad>/buttonSouth",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "Jump",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "603f3daf-40bd-4854-8724-93e8017f59e3",
                    "path": "<XRController>/secondaryButton",
                    "interactions": "",
                    "processors": "",
                    "groups": "XR",
                    "action": "Jump",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "1534dc16-a6aa-499d-9c3a-22b47347b52a",
                    "path": "<Keyboard>/1",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Previous",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "25060bbd-a3a6-476e-8fba-45ae484aad05",
                    "path": "<Gamepad>/dpad/left",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "Previous",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "1c04ea5f-b012-41d1-a6f7-02e963b52893",
                    "path": "<Keyboard>/e",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Interact",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "b3f66d0b-7751-423f-908b-a11c5bd95930",
                    "path": "<Gamepad>/buttonNorth",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "Interact",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "4f4649ac-64a8-4a73-af11-b3faef356a4d",
                    "path": "<Gamepad>/buttonEast",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "Crouch",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "36e52cba-0905-478e-a818-f4bfcb9f3b9a",
                    "path": "<Keyboard>/c",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Crouch",
                    "isComposite": false,
                    "isPartOfComposite": false
                }
            ]
        },
        {
            "name": "UI",
            "id": "272f6d14-89ba-496f-b7ff-215263d3219f",
            "actions": [
                {
                    "name": "Navigate",
                    "type": "PassThrough",
                    "id": "c95b2375-e6d9-4b88-9c4c-c5e76515df4b",
                    "expectedControlType": "Vector2",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Submit",
                    "type": "Button",
                    "id": "7607c7b6-cd76-4816-beef-bd0341cfe950",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Cancel",
                    "type": "Button",
                    "id": "15cef263-9014-4fd5-94d9-4e4a6234a6ef",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Point",
                    "type": "PassThrough",
                    "id": "32b35790-4ed0-4e9a-aa41-69ac6d629449",
                    "expectedControlType": "Vector2",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": true
                },
                {
                    "name": "Click",
                    "type": "PassThrough",
                    "id": "3c7022bf-7922-4f7c-a998-c437916075ad",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": true
                },
                {
                    "name": "RightClick",
                    "type": "PassThrough",
                    "id": "44b200b1-1557-4083-816c-b22cbdf77ddf",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "MiddleClick",
                    "type": "PassThrough",
                    "id": "dad70c86-b58c-4b17-88ad-f5e53adf419e",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "ScrollWheel",
                    "type": "PassThrough",
                    "id": "0489e84a-4833-4c40-bfae-cea84b696689",
                    "expectedControlType": "Vector2",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "TrackedDevicePosition",
                    "type": "PassThrough",
                    "id": "24908448-c609-4bc3-a128-ea258674378a",
                    "expectedControlType": "Vector3",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "TrackedDeviceOrientation",
                    "type": "PassThrough",
                    "id": "9caa3d8a-6b2f-4e8e-8bad-6ede561bd9be",
                    "expectedControlType": "Quaternion",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                }
            ],
            "bindings": [
                {
                    "name": "Gamepad",
                    "id": "809f371f-c5e2-4e7a-83a1-d867598f40dd",
                    "path": "2DVector",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Navigate",
                    "isComposite": true,
                    "isPartOfComposite": false
                },
                {
                    "name": "up",
                    "id": "14a5d6e8-4aaf-4119-a9ef-34b8c2c548bf",
                    "path": "<Gamepad>/leftStick/up",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "up",
                    "id": "9144cbe6-05e1-4687-a6d7-24f99d23dd81",
                    "path": "<Gamepad>/rightStick/up",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "2db08d65-c5fb-421b-983f-c71163608d67",
                    "path": "<Gamepad>/leftStick/down",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "58748904-2ea9-4a80-8579-b500e6a76df8",
                    "path": "<Gamepad>/rightStick/down",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "8ba04515-75aa-45de-966d-393d9bbd1c14",
                    "path": "<Gamepad>/leftStick/left",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "712e721c-bdfb-4b23-a86c-a0d9fcfea921",
                    "path": "<Gamepad>/rightStick/left",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "fcd248ae-a788-4676-a12e-f4d81205600b",
                    "path": "<Gamepad>/leftStick/right",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "1f04d9bc-c50b-41a1-bfcc-afb75475ec20",
                    "path": "<Gamepad>/rightStick/right",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "",
                    "id": "fb8277d4-c5cd-4663-9dc7-ee3f0b506d90",
                    "path": "<Gamepad>/dpad",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Gamepad",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "Joystick",
                    "id": "e25d9774-381c-4a61-b47c-7b6b299ad9f9",
                    "path": "2DVector",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Navigate",
                    "isComposite": true,
                    "isPartOfComposite": false
                },
                {
                    "name": "up",
                    "id": "3db53b26-6601-41be-9887-63ac74e79d19",
                    "path": "<Joystick>/stick/up",
                    "interactions": "",
                    "processors": "",
                    "groups": "Joystick",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "0cb3e13e-3d90-4178-8ae6-d9c5501d653f",
                    "path": "<Joystick>/stick/down",
                    "interactions": "",
                    "processors": "",
                    "groups": "Joystick",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "0392d399-f6dd-4c82-8062-c1e9c0d34835",
                    "path": "<Joystick>/stick/left",
                    "interactions": "",
                    "processors": "",
                    "groups": "Joystick",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "942a66d9-d42f-43d6-8d70-ecb4ba5363bc",
                    "path": "<Joystick>/stick/right",
                    "interactions": "",
                    "processors": "",
                    "groups": "Joystick",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "Keyboard",
                    "id": "ff527021-f211-4c02-933e-5976594c46ed",
                    "path": "2DVector",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Navigate",
                    "isComposite": true,
                    "isPartOfComposite": false
                },
                {
                    "name": "up",
                    "id": "563fbfdd-0f09-408d-aa75-8642c4f08ef0",
                    "path": "<Keyboard>/w",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "up",
                    "id": "eb480147-c587-4a33-85ed-eb0ab9942c43",
                    "path": "<Keyboard>/upArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "2bf42165-60bc-42ca-8072-8c13ab40239b",
                    "path": "<Keyboard>/s",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "85d264ad-e0a0-4565-b7ff-1a37edde51ac",
                    "path": "<Keyboard>/downArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "74214943-c580-44e4-98eb-ad7eebe17902",
                    "path": "<Keyboard>/a",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "cea9b045-a000-445b-95b8-0c171af70a3b",
                    "path": "<Keyboard>/leftArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "8607c725-d935-4808-84b1-8354e29bab63",
                    "path": "<Keyboard>/d",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "4cda81dc-9edd-4e03-9d7c-a71a14345d0b",
                    "path": "<Keyboard>/rightArrow",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Navigate",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "",
                    "id": "9e92bb26-7e3b-4ec4-b06b-3c8f8e498ddc",
                    "path": "*/{Submit}",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse;Gamepad;Touch;Joystick;XR",
                    "action": "Submit",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "82627dcc-3b13-4ba9-841d-e4b746d6553e",
                    "path": "*/{Cancel}",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse;Gamepad;Touch;Joystick;XR",
                    "action": "Cancel",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "c52c8e0b-8179-41d3-b8a1-d149033bbe86",
                    "path": "<Mouse>/position",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Point",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "e1394cbc-336e-44ce-9ea8-6007ed6193f7",
                    "path": "<Pen>/position",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Point",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "5693e57a-238a-46ed-b5ae-e64e6e574302",
                    "path": "<Touchscreen>/touch*/position",
                    "interactions": "",
                    "processors": "",
                    "groups": "Touch",
                    "action": "Point",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "4faf7dc9-b979-4210-aa8c-e808e1ef89f5",
                    "path": "<Mouse>/leftButton",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Click",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "8d66d5ba-88d7-48e6-b1cd-198bbfef7ace",
                    "path": "<Pen>/tip",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "Click",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "47c2a644-3ebc-4dae-a106-589b7ca75b59",
                    "path": "<Touchscreen>/touch*/press",
                    "interactions": "",
                    "processors": "",
                    "groups": "Touch",
                    "action": "Click",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "bb9e6b34-44bf-4381-ac63-5aa15d19f677",
                    "path": "<XRController>/trigger",
                    "interactions": "",
                    "processors": "",
                    "groups": "XR",
                    "action": "Click",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "38c99815-14ea-4617-8627-164d27641299",
                    "path": "<Mouse>/scroll",
                    "interactions": "",
                    "processors": "",
                    "groups": ";Keyboard&Mouse",
                    "action": "ScrollWheel",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "4c191405-5738-4d4b-a523-c6a301dbf754",
                    "path": "<Mouse>/rightButton",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "RightClick",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "24066f69-da47-44f3-a07e-0015fb02eb2e",
                    "path": "<Mouse>/middleButton",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "MiddleClick",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "7236c0d9-6ca3-47cf-a6ee-a97f5b59ea77",
                    "path": "<XRController>/devicePosition",
                    "interactions": "",
                    "processors": "",
                    "groups": "XR",
                    "action": "TrackedDevicePosition",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "23e01e3a-f935-4948-8d8b-9bcac77714fb",
                    "path": "<XRController>/deviceRotation",
                    "interactions": "",
                    "processors": "",
                    "groups": "XR",
                    "action": "TrackedDeviceOrientation",
                    "isComposite": false,
                    "isPartOfComposite": false
                }
            ]
        }
    ],
    "controlSchemes": [
        {
            "name": "Keyboard&Mouse",
            "bindingGroup": "Keyboard&Mouse",
            "devices": [
                {
                    "devicePath": "<Keyboard>",
                    "isOptional": false,
                    "isOR": false
                },
                {
                    "devicePath": "<Mouse>",
                    "isOptional": false,
                    "isOR": false
                }
            ]
        },
        {
            "name": "Gamepad",
            "bindingGroup": "Gamepad",
            "devices": [
                {
                    "devicePath": "<Gamepad>",
                    "isOptional": false,
                    "isOR": false
                }
            ]
        },
        {
            "name": "Touch",
            "bindingGroup": "Touch",
            "devices": [
                {
                    "devicePath": "<Touchscreen>",
                    "isOptional": false,
                    "isOR": false
                }
            ]
        },
        {
            "name": "Joystick",
            "bindingGroup": "Joystick",
            "devices": [
                {
                    "devicePath": "<Joystick>",
                    "isOptional": false,
                    "isOR": false
                }
            ]
        },
        {
            "name": "XR",
            "bindingGroup": "XR",
            "devices": [
                {
                    "devicePath": "<XRController>",
                    "isOptional": false,
                    "isOR": false
                }
            ]
        }
    ]
}
````

### `Assets/Robot&Pilot/Assets/Script/AnimateEmission.cs`

````csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateEmission : MonoBehaviour {

	void Update () {
		Renderer renderer = GetComponent<Renderer> ();
		Material mat = renderer.material;

		float low = 4.0f;
		float high = 1.0f;
		float emission = low + Mathf.PingPong (Time.time, high - low);
		Color baseColor = Color.white;

		Color finalColor = baseColor * Mathf.LinearToGammaSpace (emission);

		mat.SetColor ("_EmissionColor", finalColor);
	}
}
````

### `Assets/Scripts/RoboClash/ArenaBotController.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(CharacterController), typeof(ArenaDamageable))]
    public sealed class ArenaBotController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 3.2f;
        [SerializeField, Min(1f)] private float engagementRange = 24f;
        [SerializeField, Min(0.1f)] private float fireInterval = 1.15f;
        [SerializeField, Min(1f)] private float projectileDamage = 8f;

        private CharacterController characterController;
        private ArenaDamageable damageable;
        private RobotArenaController target;
        private float verticalVelocity;
        private float nextShotTime;
        private ArenaMovingPlatform currentPlatform;
        private ArenaMovingPlatform platformContactThisMove;
        private float disruptedUntil;
        private float slowedUntil;
        private float disruptionSpeedMultiplier = 1f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            damageable = GetComponent<ArenaDamageable>();
        }

        private void Update()
        {
            if (target == null || !target.isActiveAndEnabled || target.IsDead || !target.HasPlayerControl)
                target = FindPlayerTarget();

            if (target == null || damageable.IsDead)
                return;

            var targetAbilities = target.GetComponent<RoboClashRobotAbilities>();
            if (targetAbilities != null && targetAbilities.IsCloaked)
                return;

            var offset = target.transform.position - transform.position;
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance < 0.01f)
                return;

            var direction = offset / distance;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up), 6f * Time.deltaTime);

            platformContactThisMove = null;
            var platformMotion = currentPlatform != null && characterController.isGrounded
                ? currentPlatform.Velocity * Time.deltaTime
                : Vector3.zero;
            if (Time.time < disruptedUntil)
                characterController.Move(platformMotion);
            else if (distance > 8f)
            {
                var speed = Time.time < slowedUntil ? disruptionSpeedMultiplier : 1f;
                characterController.Move(direction * (moveSpeed * speed * Time.deltaTime) + platformMotion);
            }
            else
                characterController.Move(platformMotion);

            if (characterController.isGrounded)
                verticalVelocity = -1f;
            else
                verticalVelocity += Physics.gravity.y * Time.deltaTime;
            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
            currentPlatform = platformContactThisMove;

            if (distance <= engagementRange && Time.time >= nextShotTime && Time.time >= disruptedUntil)
                FireAtTarget();
        }

        private RobotArenaController FindPlayerTarget()
        {
            foreach (var candidate in FindObjectsByType<RobotArenaController>())
            {
                if (candidate != null && candidate.isActiveAndEnabled &&
                    candidate.HasPlayerControl && !candidate.IsDead)
                    return candidate;
            }

            return null;
        }

        public void ApplyDisruption(float duration, float slowMultiplier)
        {
            disruptedUntil = Mathf.Max(disruptedUntil, Time.time + Mathf.Min(duration, 1.25f));
            slowedUntil = Mathf.Max(slowedUntil, Time.time + duration);
            disruptionSpeedMultiplier = Mathf.Clamp(slowMultiplier, 0.2f, 1f);
        }

        private void FireAtTarget()
        {
            var muzzle = transform.position + Vector3.up * 1.35f + transform.forward * 0.7f;
            var destination = target.transform.position + Vector3.up * 1.2f;
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Bot Energy Bolt";
            projectile.transform.position = muzzle;
            projectile.transform.localScale = Vector3.one * 0.18f;
            projectile.AddComponent<Rigidbody>();
            projectile.AddComponent<ArenaProjectile>().Launch(gameObject, destination - muzzle, 24f, projectileDamage);
            nextShotTime = Time.time + fireInterval;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
                platformContactThisMove = hit.collider.GetComponentInParent<ArenaMovingPlatform>();
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaDamageable.cs`

````csharp
using System;
using UnityEngine;

namespace RoboClash
{
    public class ArenaDamageable : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float maximumShield = 150f;
        [SerializeField, Min(0f)] private float startingShield;

        public float Health { get; private set; }
        public float Shield { get; private set; }
        public float MaximumHealth => maximumHealth;
        public float MaximumShield => maximumShield;
        public bool IsDead { get; private set; }

        public event Action<ArenaDamageable, float, ArenaDamageable> DamageApplied;
        public event Action<ArenaDamageable> VitalsChanged;
        public event Action<ArenaDamageable> Eliminated;

        protected virtual void Awake()
        {
            maximumHealth = !float.IsNaN(maximumHealth) && !float.IsInfinity(maximumHealth)
                ? Mathf.Max(1f, maximumHealth)
                : 100f;
            maximumShield = !float.IsNaN(maximumShield) && !float.IsInfinity(maximumShield)
                ? Mathf.Max(0f, maximumShield)
                : 0f;
            Health = maximumHealth;
            Shield = !float.IsNaN(startingShield) && !float.IsInfinity(startingShield)
                ? Mathf.Clamp(startingShield, 0f, maximumShield)
                : 0f;
        }

        public virtual void TakeDamage(float amount)
        {
            ApplyDamage(amount, null);
        }

        public virtual void TakeDamage(float amount, ArenaDamageable instigator)
        {
            ApplyDamage(amount, instigator);
        }

        private void ApplyDamage(float amount, ArenaDamageable instigator)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) || IsDead)
                return;

            var totalVitalsBefore = Health + Shield;
            var absorbed = Mathf.Min(Shield, amount);
            Shield -= absorbed;
            Health = Mathf.Max(0f, Health - (amount - absorbed));
            var effectiveDamage = totalVitalsBefore - Health - Shield;
            VitalsChanged?.Invoke(this);
            if (effectiveDamage > 0f)
                DamageApplied?.Invoke(this, effectiveDamage, instigator);

            if (Health <= 0f)
            {
                IsDead = true;
                GetComponent<RoboClashRobotAudio>()?.PlayDetached(RoboClashAudioEvent.Death);
                OnDeath();
                Eliminated?.Invoke(this);
            }
        }

        public void AddShield(float amount)
        {
            if (amount > 0f && !IsDead)
            {
                var previousShield = Shield;
                Shield = Mathf.Min(maximumShield, Shield + amount);
                if (!Mathf.Approximately(previousShield, Shield))
                    VitalsChanged?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            if (amount > 0f && !IsDead)
            {
                var previousHealth = Health;
                Health = Mathf.Min(maximumHealth, Health + amount);
                if (!Mathf.Approximately(previousHealth, Health))
                    VitalsChanged?.Invoke(this);
            }
        }

        public void ConfigureVitals(float health, float shield)
        {
            maximumHealth = !float.IsNaN(health) && !float.IsInfinity(health)
                ? Mathf.Max(1f, health)
                : 100f;
            maximumShield = !float.IsNaN(shield) && !float.IsInfinity(shield)
                ? Mathf.Max(0f, shield)
                : 0f;
            startingShield = maximumShield;
            Health = maximumHealth;
            Shield = maximumShield;
            IsDead = false;
            VitalsChanged?.Invoke(this);
        }

        protected virtual void OnDeath()
        {
            gameObject.SetActive(false);
        }

        protected void Restore(float health, float shield)
        {
            Health = Mathf.Clamp(health, 1f, maximumHealth);
            Shield = Mathf.Clamp(shield, 0f, maximumShield);
            IsDead = false;
            VitalsChanged?.Invoke(this);
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaDecoration.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    public sealed class ArenaDecoration : MonoBehaviour
    {
    }
}
````

### `Assets/Scripts/RoboClash/ArenaEnergyShield.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class ArenaEnergyShield : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumEnergy = 150f;
        [SerializeField, Min(1f)] private float energy = 150f;
        [SerializeField, Min(0f)] private float rechargePerSecond = 8f;

        public float Energy => energy;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            energy = Mathf.Clamp(energy, 0f, maximumEnergy);
        }

        private void Update()
        {
            energy = Mathf.Min(maximumEnergy, energy + rechargePerSecond * Time.deltaTime);
        }

        public bool Absorb(float damage)
        {
            if (energy <= 0f || damage <= 0f)
                return false;

            energy = Mathf.Max(0f, energy - damage);
            return true;
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaFloor.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ArenaFloor : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = false;
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaHazard.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public abstract class ArenaHazard : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damagePerSecond = 35f;

        protected virtual void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        protected void Damage(ArenaDamageable target)
        {
            if (target != null)
                target.TakeDamage(damagePerSecond * Time.deltaTime);
        }

        protected void DamageOnce(ArenaDamageable target)
        {
            if (target != null)
                target.TakeDamage(damagePerSecond);
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaHealthPickup.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ArenaHealthPickup : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float healAmount = 50f;
        [SerializeField, Min(0f)] private float respawnDelay = 20f;

        private Renderer[] renderers;
        private SphereCollider pickupCollider;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            pickupCollider = GetComponent<SphereCollider>();
            pickupCollider.isTrigger = true;
            var body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void Update()
        {
            transform.Rotate(0f, 55f * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponentInParent<ArenaDamageable>();
            if (!(target is RobotArenaController) || target.IsDead ||
                target.Health >= target.MaximumHealth)
                return;

            target.Heal(healAmount);
            SetAvailable(false);
            if (respawnDelay > 0f)
                Invoke(nameof(Respawn), respawnDelay);
        }

        private void Respawn()
        {
            SetAvailable(true);
        }

        private void SetAvailable(bool available)
        {
            pickupCollider.enabled = available;
            foreach (var item in renderers)
                item.enabled = available;
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaMovingPlatform.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    public sealed class ArenaMovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, 8f);
        [SerializeField, Min(0.1f)] private float cycleDuration = 5f;

        private Vector3 startPosition;
        private Rigidbody body;
        public Vector3 Velocity { get; private set; }

        private void Awake()
        {
            startPosition = transform.position;
            body = GetComponent<Rigidbody>();
        }

        public void Configure(Vector3 offset, float duration)
        {
            localOffset = offset;
            cycleDuration = Mathf.Max(0.1f, duration);
        }

        private void FixedUpdate()
        {
            var phase = Mathf.PingPong(Time.time / cycleDuration, 1f);
            var nextPosition = startPosition + localOffset * phase;
            Velocity = (nextPosition - transform.position) / Time.fixedDeltaTime;
            if (body != null && body.isKinematic)
                body.MovePosition(nextPosition);
            else
                transform.position = nextPosition;
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaProjectile.cs`

````csharp
using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class ArenaProjectile : MonoBehaviour
    {
        private float damage;
        private GameObject owner;
        private bool consumed;
        private float explosionRadius;
        private float knockback;

        public bool IsOwnedBy(GameObject candidate)
        {
            return candidate != null && owner == candidate;
        }

        public void Cancel()
        {
            consumed = true;
            Destroy(gameObject);
        }

        public void Launch(GameObject firingOwner, Vector3 direction, float speed, float impactDamage,
            float lifetime = 4f, float areaRadius = 0f, float impactKnockback = 0f)
        {
            owner = firingOwner;
            damage = impactDamage;
            explosionRadius = Mathf.Max(0f, areaRadius);
            knockback = Mathf.Max(0f, impactKnockback);

            var body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.forward;
            direction.Normalize();
            body.linearVelocity = direction * speed;

            var projectileCollider = GetComponent<Collider>();
            if (owner != null)
            {
                foreach (var ownerCollider in owner.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(projectileCollider, ownerCollider);
            }

            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contactCount > 0)
            {
                var contact = collision.GetContact(0);
                Hit(collision.collider, contact.point, contact.normal);
            }
            else
                Hit(collision.collider, transform.position, -transform.forward);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger && other.GetComponentInParent<ArenaEnergyShield>() == null)
                return;

            Hit(other, transform.position, -transform.forward);
        }

        public void ConfigureVisual(Color color, bool rocket)
        {
            RoboClashCombatVFX.ConfigureProjectileVisual(gameObject, color, rocket);
        }

        private void Hit(Collider other, Vector3 impactPoint, Vector3 impactNormal)
        {
            if (consumed || (owner != null && other.transform.root.gameObject == owner))
                return;

            var shield = other.GetComponentInParent<ArenaEnergyShield>();
            var target = shield == null ? other.GetComponentInParent<ArenaDamageable>() : null;
            if (target != null && IsFriendlyTarget(target))
            {
                IgnoreTargetCollisions(target);
                return;
            }

            consumed = true;
            if (shield != null)
            {
                if (!shield.Absorb(damage) && explosionRadius <= 0f)
                {
                    PlayImpact(impactPoint, impactNormal, true, null);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                if (target != null)
                {
                    if (!target.IsDead && damage > 0f)
                    {
                        var vitalsBefore = target.Health + target.Shield;
                        target.TakeDamage(damage, GetOwnerDamageable());
                        var effectiveDamage = Mathf.Max(0f, vitalsBefore - target.Health - target.Shield);
                        if (effectiveDamage > 0f)
                        {
                            owner?.GetComponent<RobotArenaController>()?.RegisterHit();
                            if (target.GetComponent<RobotArenaController>() == null)
                                target.GetComponent<RoboClashRobotAnimator>()?.TriggerHitReaction(0.35f);
                            AwardUltimateCharge(effectiveDamage);
                            ApplyKnockback(target, impactPoint);
                        }
                    }
                }
            }

            if (explosionRadius > 0f)
                ApplyAreaDamage(impactPoint, other);

            PlayImpact(impactPoint, impactNormal, explosionRadius > 0f, shield);
            Destroy(gameObject);
        }

        private void PlayImpact(Vector3 point, Vector3 normal, bool heavy, ArenaEnergyShield shield)
        {
            var color = GetOwnerAccentColor();
            RoboClashCombatVFX.PlayBurst(point, normal, color, heavy ? 0.85f : 0.48f, heavy);
            owner?.GetComponent<RoboClashRobotAudio>()?.PlayDetachedAt(
                shield != null ? RoboClashAudioEvent.ShieldImpact : RoboClashAudioEvent.ProjectileImpact,
                point);
        }

        private Color GetOwnerAccentColor()
        {
            var definition = owner != null ? owner.GetComponent<RoboClashRobotDefinition>() : null;
            return definition != null ? definition.emissionColor : new Color(1f, 0.55f, 0.2f);
        }

        private void ApplyAreaDamage(Vector3 point, Collider directHit)
        {
            var affected = new HashSet<ArenaDamageable>();
            var directTarget = directHit.GetComponentInParent<ArenaDamageable>();
            if (directTarget != null)
                affected.Add(directTarget);

            foreach (var collider in Physics.OverlapSphere(point, explosionRadius, Physics.DefaultRaycastLayers,
                         QueryTriggerInteraction.Ignore))
            {
                var target = collider.GetComponentInParent<ArenaDamageable>();
                if (target == null || target.IsDead || IsFriendlyTarget(target) ||
                    !affected.Add(target))
                    continue;

                var targetPoint = target.transform.position + Vector3.up;
                if (Physics.Linecast(point, targetPoint, out var obstruction, Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore) &&
                    obstruction.transform.root != target.transform.root &&
                    obstruction.collider != directHit)
                    continue;

                var distanceFactor = 1f - Mathf.Clamp01(Vector3.Distance(point, target.transform.position) / explosionRadius);
                var appliedDamage = damage * Mathf.Lerp(0.35f, 1f, distanceFactor);
                var vitalsBefore = target.Health + target.Shield;
                target.TakeDamage(appliedDamage, GetOwnerDamageable());
                var effectiveDamage = Mathf.Max(0f, vitalsBefore - target.Health - target.Shield);
                if (effectiveDamage > 0f)
                {
                    owner?.GetComponent<RobotArenaController>()?.RegisterHit();
                    AwardUltimateCharge(effectiveDamage);
                    ApplyKnockback(target, point);
                }
            }
        }

        private bool IsFriendlyTarget(ArenaDamageable target)
        {
            if (target == null || owner == null)
                return false;

            if (target.transform.root.gameObject == owner)
                return true;

            var ownerDefinition = owner.GetComponent<RoboClashRobotDefinition>();
            var targetDefinition = target.GetComponent<RoboClashRobotDefinition>();
            return ownerDefinition != null && targetDefinition != null &&
                   ownerDefinition.teamId == targetDefinition.teamId;
        }

        private ArenaDamageable GetOwnerDamageable()
        {
            return owner != null ? owner.GetComponent<ArenaDamageable>() : null;
        }

        private void IgnoreTargetCollisions(ArenaDamageable target)
        {
            var projectileCollider = GetComponent<Collider>();
            if (projectileCollider == null)
                return;

            foreach (var targetCollider in target.GetComponentsInChildren<Collider>())
            {
                if (targetCollider != null)
                    Physics.IgnoreCollision(projectileCollider, targetCollider);
            }
        }

        private void ApplyKnockback(ArenaDamageable target, Vector3 point)
        {
            if (knockback <= 0f)
                return;

            var movement = target.GetComponent<RoboClashRobotMovement>();
            if (movement == null)
                return;

            var direction = target.transform.position - point;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                movement.ApplyImpulse(direction.normalized * knockback);
        }

        private void AwardUltimateCharge(float amount)
        {
            if (owner == null)
                return;

            var abilities = owner.GetComponent<RoboClashRobotAbilities>();
            var definition = owner.GetComponent<RoboClashRobotDefinition>();
            if (abilities != null && definition != null)
                abilities.AddUltimateCharge(amount * definition.stats.ultimateChargePerDamage);
        }

    }
}
````

### `Assets/Scripts/RoboClash/ArenaShieldPickup.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ArenaShieldPickup : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float shieldAmount = 60f;
        [SerializeField, Min(0f)] private float respawnDelay = 15f;

        private Renderer[] renderers;
        private SphereCollider pickupCollider;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            pickupCollider = GetComponent<SphereCollider>();
            pickupCollider.isTrigger = true;
            var body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void Update()
        {
            transform.Rotate(0f, 55f * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponentInParent<ArenaDamageable>();
            if (target == null || target is not RobotArenaController)
                return;

            target.AddShield(shieldAmount);
            SetAvailable(false);
            if (respawnDelay > 0f)
                Invoke(nameof(Respawn), respawnDelay);
        }

        private void Respawn()
        {
            SetAvailable(true);
        }

        private void SetAvailable(bool available)
        {
            pickupCollider.enabled = available;
            foreach (var item in renderers)
                item.enabled = available;
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaSpawnZone.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    public sealed class ArenaSpawnZone : MonoBehaviour
    {
        [SerializeField] private int spawnIndex;

        public int SpawnIndex => spawnIndex;

        public void SetSpawnIndex(int index)
        {
            spawnIndex = Mathf.Max(0, index);
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaWall.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ArenaWall : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = false;
        }
    }
}
````

### `Assets/Scripts/RoboClash/ArenaWeaponPickup.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ArenaWeaponPickup : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float damage = 40f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 32f;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.22f;
        [SerializeField, Min(0f)] private float respawnDelay = 12f;

        private Renderer[] renderers;
        private SphereCollider pickupCollider;
        private Vector3 initialPosition;

        private void Awake()
        {
            initialPosition = transform.position;
            renderers = GetComponentsInChildren<Renderer>();
            pickupCollider = GetComponent<SphereCollider>();
            pickupCollider.isTrigger = true;
            var body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void Update()
        {
            transform.Rotate(0f, 70f * Time.deltaTime, 0f, Space.World);
            transform.position = initialPosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.2f);
        }

        private void OnTriggerEnter(Collider other)
        {
            var robot = other.GetComponentInParent<RobotArenaController>();
            if (robot == null || robot.IsDead)
                return;

            var combat = robot.GetComponent<RoboClashRobotCombat>();
            if (combat != null)
                combat.ApplyDamageBuff(1.5f, 15f);
            else
                robot.EquipWeapon(damage, projectileSpeed, fireInterval);
            SetAvailable(false);
            if (respawnDelay > 0f)
                Invoke(nameof(Respawn), respawnDelay);
        }

        private void Respawn()
        {
            SetAvailable(true);
        }

        private void SetAvailable(bool available)
        {
            if (pickupCollider != null)
                pickupCollider.enabled = available;

            foreach (var item in renderers)
                item.enabled = available;
        }
    }
}
````

### `Assets/Scripts/RoboClash/KingOfArenaZone.cs`

````csharp
using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class KingOfArenaZone : MonoBehaviour
    {
        private readonly Dictionary<ArenaDamageable, HashSet<Collider>> occupants =
            new Dictionary<ArenaDamageable, HashSet<Collider>>();

        public IEnumerable<ArenaDamageable> Occupants => occupants.Keys;

        public void ClearOccupants()
        {
            occupants.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            var participant = other.GetComponentInParent<ArenaDamageable>();
            if (participant == null)
                return;

            if (!occupants.TryGetValue(participant, out var colliders))
            {
                colliders = new HashSet<Collider>();
                occupants.Add(participant, colliders);
            }

            colliders.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            var participant = other.GetComponentInParent<ArenaDamageable>();
            if (participant == null || !occupants.TryGetValue(participant, out var colliders))
                return;

            colliders.Remove(other);
            if (colliders.Count == 0)
                occupants.Remove(participant);
        }

        private void OnDisable()
        {
            occupants.Clear();
        }
    }
}
````

### `Assets/Scripts/RoboClash/LavaPit.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LavaPit : ArenaHazard
    {
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            Damage(other.GetComponentInParent<ArenaDamageable>());
        }
    }
}
````

### `Assets/Scripts/RoboClash/MetalBarrier.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MetalBarrier : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = false;
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashCombatVFX.cs`

````csharp
using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    public static class RoboClashCombatVFX
    {
        private const int MaximumPooledEffects = 32;
        private static readonly List<ParticleSystem> Pool = new List<ParticleSystem>(MaximumPooledEffects);
        private static Material particleMaterial;

        public static Quaternion SurfaceRotation(Vector3 normal)
        {
            return normal.sqrMagnitude < 0.001f
                ? Quaternion.identity
                : Quaternion.LookRotation(normal.normalized);
        }

        public static Color GetRobotAccent(RoboClashRobotDefinition definition)
        {
            return definition != null ? definition.emissionColor : Color.white;
        }

        public static void PlayBurst(Vector3 position, Vector3 direction, Color color, float size,
            bool heavy = false)
        {
            var effect = GetAvailableEffect();
            effect.transform.SetPositionAndRotation(position,
                direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity);
            effect.transform.localScale = Vector3.one * Mathf.Max(0.1f, size);
            var main = effect.main;
            main.startColor = color;
            main.startSize = heavy ? new ParticleSystem.MinMaxCurve(0.045f, 0.16f) :
                new ParticleSystem.MinMaxCurve(0.025f, 0.09f);
            main.startLifetime = heavy ? new ParticleSystem.MinMaxCurve(0.2f, 0.55f) :
                new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            main.startSpeed = heavy ? new ParticleSystem.MinMaxCurve(1.5f, 5f) :
                new ParticleSystem.MinMaxCurve(1.2f, 3.8f);
            var emission = effect.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(heavy ? 20 : 10)) });
            effect.Clear(true);
            effect.Play(true);
        }

        public static void PlayMuzzle(Vector3 position, Vector3 forward, Color color, float size)
        {
            PlayBurst(position, forward, color, size, false);
        }

        public static void PlayDash(Vector3 position, Vector3 direction, Color color)
        {
            PlayBurst(position, direction, color, 1.15f, true);
        }

        public static void PlayPulse(Vector3 position, Color color, float radius)
        {
            PlayBurst(position, Vector3.up, color, Mathf.Clamp(radius * 0.35f, 1f, 3f), true);
            RoboClashPulseRing.Create(position, color, radius, 0.55f);
        }

        public static void ConfigureProjectileVisual(GameObject projectile, Color color, bool rocket)
        {
            if (projectile == null)
                return;

            var renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetParticleMaterial();
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", color);
                properties.SetColor("_Color", color);
                renderer.SetPropertyBlock(properties);
            }

            var trail = projectile.AddComponent<TrailRenderer>();
            trail.time = rocket ? 0.48f : 0.24f;
            trail.startWidth = rocket ? 0.16f : 0.085f;
            trail.endWidth = 0.015f;
            trail.minVertexDistance = 0.04f;
            trail.numCapVertices = 2;
            trail.sharedMaterial = GetParticleMaterial();
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.autodestruct = false;
        }

        private static ParticleSystem GetAvailableEffect()
        {
            for (var i = 0; i < Pool.Count; i++)
            {
                var candidate = Pool[i];
                if (candidate == null)
                    continue;
                if (!candidate.IsAlive(true))
                    return candidate;
            }

            if (Pool.Count == MaximumPooledEffects)
            {
                var reused = Pool[0];
                Pool.RemoveAt(0);
                if (reused != null)
                {
                    reused.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    Pool.Add(reused);
                    return reused;
                }
            }

            var instance = new GameObject("RoboClash VFX");
            instance.hideFlags = HideFlags.DontSave;
            var particles = instance.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(particles);
            Pool.Add(particles);
            return particles;
        }

        private static void ConfigureParticleSystem(ParticleSystem particles)
        {
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.12f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetParticleMaterial();
            renderer.sortingFudge = 0.1f;
        }

        internal static Material GetParticleMaterial()
        {
            if (particleMaterial != null)
                return particleMaterial;

            particleMaterial = Resources.Load<Material>("RoboClash/VFX/ParticleUnlit");
            if (particleMaterial != null)
                return particleMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                         Shader.Find("Particles/Standard Unlit") ??
                         Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("RoboClash: ParticleUnlit material is missing. Run Tools > RoboClash > Create VFX Materials.");
                return null;
            }

            particleMaterial = new Material(shader)
            {
                name = "RoboClash Runtime VFX",
                hideFlags = HideFlags.DontSave
            };
            return particleMaterial;
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashMatchManager.cs`

````csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RoboClash
{
    public enum RoboClashMatchState
    {
        Initializing,
        Ready,
        Countdown,
        Playing,
        Paused,
        Victory,
        Defeat,
        Draw
    }

    public enum RoboClashGameMode
    {
        FreeForAll,
        Duel,
        Survival,
        KillRace,
        TimedDeathmatch,
        KingOfTheArena,
        Knockout
    }

    [Serializable]
    public sealed class RoboClashMatchSettings
    {
        [Min(1f)] public float matchDurationSeconds = 180f;
        [Min(0f)] public float countdownDurationSeconds = 3f;
        public RoboClashGameMode gameMode = RoboClashGameMode.FreeForAll;
        [Range(1, 3)] public int aiOpponents = 3;
        public bool enableTimeLimit = true;
        [Range(1, 24)] public int startingSurvivalEnemies = 3;
        [Min(1)] public int additionalEnemiesPerWave = 1;
        [Range(1, 24)] public int maximumSurvivalEnemiesPerWave = 24;
        [Min(0f)] public float waveBreakSeconds = 5f;
        public bool respawnEnabled;
        [Min(0)] public int maximumRespawns;
        [Min(1)] public int killTarget = 10;
        [Range(1, 10)] public int knockoutLives = 3;
        public bool duelUsesKillTarget = true;
        public bool duelRespawnsEnabled = true;
        public RoboClashRobotType playerRobotType = RoboClashRobotType.Volt;
        public RoboClashAIDifficulty aiDifficulty = RoboClashAIDifficulty.Normal;
        public RoboClashAIDifficultyProfile easyAI = new RoboClashAIDifficultyProfile
        {
            decisionInterval = 0.55f,
            targetReevaluationInterval = 1.1f,
            sensingRange = 24f,
            aimTurnSpeed = 100f,
            aimErrorDegrees = 10f,
            aggression = 0.35f,
            strafeAmount = 0.25f,
            hazardAwareness = 0.35f
        };
        public RoboClashAIDifficultyProfile normalAI = new RoboClashAIDifficultyProfile
        {
            decisionInterval = 0.28f,
            targetReevaluationInterval = 0.55f,
            sensingRange = 36f,
            aimTurnSpeed = 180f,
            aimErrorDegrees = 4f,
            aggression = 0.65f,
            strafeAmount = 0.55f,
            hazardAwareness = 0.7f
        };
        public RoboClashAIDifficultyProfile hardAI = new RoboClashAIDifficultyProfile
        {
            decisionInterval = 0.14f,
            targetReevaluationInterval = 0.28f,
            sensingRange = 48f,
            aimTurnSpeed = 300f,
            aimErrorDegrees = 1.2f,
            aggression = 0.88f,
            strafeAmount = 0.8f,
            hazardAwareness = 0.95f
        };
        public bool useRandomSeed;
        public int randomSeed = 12345;
    }

    [Serializable]
    public struct RoboClashMatchStatistics
    {
        public int PlayerEliminations { get; private set; }
        public int PlayerDeaths { get; private set; }
        public float PlayerDamageDealt { get; private set; }
        public float PlayerDamageReceived { get; private set; }
        public float MatchDurationSeconds { get; private set; }
        public RoboClashMatchState FinalResult { get; private set; }
        public RoboClashGameMode GameMode { get; private set; }
        public int HighestWaveReached { get; private set; }
        public int HighestWaveCompleted { get; private set; }
        public int PlayerPlacement { get; private set; }

        internal void RecordElimination()
        {
            PlayerEliminations++;
        }

        internal void RecordPlayerDeath()
        {
            PlayerDeaths++;
        }

        internal void RecordDamageDealt(float amount)
        {
            PlayerDamageDealt += amount;
        }

        internal void RecordDamageReceived(float amount)
        {
            PlayerDamageReceived += amount;
        }

        internal void Complete(float duration, RoboClashMatchState result)
        {
            MatchDurationSeconds = Mathf.Max(0f, duration);
            FinalResult = result;
        }

        internal void SetGameMode(RoboClashGameMode gameMode)
        {
            GameMode = gameMode;
        }

        internal void RecordWave(int wave)
        {
            HighestWaveReached = Mathf.Max(HighestWaveReached, wave);
        }

        internal void RecordWaveCompleted(int wave)
        {
            HighestWaveCompleted = Mathf.Max(HighestWaveCompleted, wave);
        }

        internal void SetPlayerPlacement(int placement)
        {
            PlayerPlacement = Mathf.Max(0, placement);
        }
    }

    public sealed class RoboClashMatchManager : MonoBehaviour
    {
        private static RoboClashMatchManager activeInstance;

        [SerializeField] private RoboClashMatchSettings settings = new RoboClashMatchSettings();
        [SerializeField] private RoboClashProgressionRewards progressionRewards =
            new RoboClashProgressionRewards();
        [SerializeField] private GameObject[] robotPrefabs = new GameObject[4];

        private sealed class Participant
        {
            public string Id;
            public RobotArenaController Controller;
            public ArenaDamageable Damageable;
            public bool IsPlayer;
            public bool DeathProcessed;
            public bool IsEliminated;
            public int Kills;
            public int Deaths;
            public int LivesRemaining;
            public float KingScore;
            public bool AIEnabled;
            public bool AbilitiesEnabled;
            public bool CombatEnabled;
        }

        private static readonly Vector3[] FallbackSpawns =
        {
            new Vector3(-12.5f, 0f, -12.5f),
            new Vector3(12.5f, 0f, -12.5f),
            new Vector3(-12.5f, 0f, 12.5f),
            new Vector3(12.5f, 0f, 12.5f)
        };

        private readonly List<Participant> participants = new List<Participant>();
        private readonly Dictionary<GameObject, bool> priorObjectStates = new Dictionary<GameObject, bool>();
        private readonly Dictionary<ArenaDamageable, ArenaDamageable> lastDamageSources =
            new Dictionary<ArenaDamageable, ArenaDamageable>();
        private readonly List<ArenaDamageable> currentWaveEnemies = new List<ArenaDamageable>();
        private readonly List<string> archivedScoreRows = new List<string>();

        private Transform participantRoot;
        private Transform originalCameraParent;
        private Vector3 originalCameraLocalPosition;
        private Quaternion originalCameraLocalRotation;
        private Vector3 originalCameraLocalScale;
        private bool cameraWasDetached;
        private RobotArenaController player;
        private Camera playerCamera;
        private NavMeshSurface navSurface;
        private Coroutine countdownRoutine;
        private Coroutine eliminationRoutine;
        private Coroutine waveRoutine;
        private bool eliminationEvaluationPending;
        private float remainingTime;
        private float elapsedMatchSeconds;
        private float waveBreakEndsAt;
        private float nextModeStatusUpdate;
        private int currentWave;
        private bool waveBreakActive;
        private float timeScaleBeforePause = 1f;
        private CursorLockMode cursorLockBeforePause;
        private bool cursorVisibleBeforePause;
        private int countdownValue;
        private bool initializationComplete;
        private bool hasPausedTime;
        private int nextParticipantId;
        private KingOfArenaZone kingZone;
        private Material kingZoneMaterial;
        private ArenaDamageable kingController;
        private bool kingContested;
        private RoboClashMatchStatistics statistics;

        public RoboClashMatchState State { get; private set; } = RoboClashMatchState.Initializing;
        public float RemainingTime => remainingTime;
        public float ElapsedMatchSeconds => elapsedMatchSeconds;
        public float NextWaveCountdown => waveBreakActive
            ? Mathf.Max(0f, waveBreakEndsAt - Time.time)
            : 0f;
        public int CurrentWave => currentWave;
        public int EnemiesRemaining => settings.gameMode == RoboClashGameMode.Survival
            ? CountLivingWaveEnemies()
            : CountAliveOpponents();
        public bool IsWaveBreakActive => waveBreakActive;
        public RoboClashGameMode GameMode => settings != null ? settings.gameMode : RoboClashGameMode.FreeForAll;
        public bool HasMatchTimer => settings != null && settings.enableTimeLimit && IsTimedMatch();
        public bool DuelUsesKillTarget => settings != null && settings.duelUsesKillTarget;
        public RoboClashAIDifficulty AIDifficulty => settings.aiDifficulty;
        public int CountdownValue => countdownValue;
        public RoboClashMatchStatistics Statistics => statistics;
        public RobotArenaController PlayerController => player;
        public RoboClashRobotType PlayerRobotType => player != null
            ? player.GetComponent<RoboClashRobotDefinition>().robotType
            : settings.playerRobotType;
        public int ParticipantCount => participants.Count;
        public int AliveOpponentCount => CountAliveOpponents();
        public int AliveCombatantCount => CountAliveCombatants();
        public int KillTarget => settings.killTarget;
        public int KnockoutLives => settings.knockoutLives;
        public bool RespawnsEnabled => settings.respawnEnabled;
        public string KingControllerName => kingContested ? "CONTESTED" : GetKingControllerName();
        public int KingControllerScore => GetKingScore(kingController);
        public int PlayerKills => GetParticipant(player != null ? (ArenaDamageable)player : null)?.Kills ?? 0;
        public int PlayerDeaths => GetParticipant(player != null ? (ArenaDamageable)player : null)?.Deaths ?? 0;
        public int PlayerLivesRemaining => GetParticipant(player != null ? (ArenaDamageable)player : null)?.LivesRemaining ?? 0;
        public int RequiredSpawnZoneCount => settings == null
            ? 0
            : settings.gameMode == RoboClashGameMode.Survival
                ? 1
                : GetInitialOpponentCount() + 1;

        public event Action<RoboClashMatchState> MatchStateChanged;
        public event Action<float> MatchTimerChanged;
        public event Action<int> CountdownChanged;
        public event Action MatchStarted;
        public event Action<RoboClashMatchStatistics> MatchEnded;
        public event Action<RoboClashMatchStatistics> StatisticsChanged;
        public event Action<RobotArenaController> PlayerRegistered;
        public event Action<ArenaDamageable, ArenaDamageable> ParticipantEliminated;
        public event Action ModeStatusChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            activeInstance = null;
        }

        private void Awake()
        {
            if (!enabled)
                return;

            if (!string.Equals(
                    gameObject.scene.path,
                    "Assets/Scenes/RoboClashGameplay.unity",
                    StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError(
                    $"RoboClashMatchManager must only initialize in " +
                    $"'Assets/Scenes/RoboClashGameplay.unity', but was found in " +
                    $"scene '{gameObject.scene.name}' at '{gameObject.scene.path}'.",
                    this);
                enabled = false;
                return;
            }

            if (activeInstance != null && activeInstance != this)
            {
                Debug.LogError("Only one RoboClashMatchManager may be active at a time.", this);
                enabled = false;
                return;
            }

            activeInstance = this;
        }

        private void OnValidate()
        {
            if (settings == null)
                settings = new RoboClashMatchSettings();
            if (progressionRewards == null)
                progressionRewards = new RoboClashProgressionRewards();

            settings.matchDurationSeconds = IsFinite(settings.matchDurationSeconds)
                ? Mathf.Max(1f, settings.matchDurationSeconds)
                : 180f;
            settings.countdownDurationSeconds = IsFinite(settings.countdownDurationSeconds)
                ? Mathf.Max(0f, settings.countdownDurationSeconds)
                : 3f;
            settings.aiOpponents = Mathf.Clamp(settings.aiOpponents, 1, 3);
            settings.maximumSurvivalEnemiesPerWave = Mathf.Clamp(settings.maximumSurvivalEnemiesPerWave, 1, 24);
            settings.startingSurvivalEnemies = Mathf.Clamp(settings.startingSurvivalEnemies, 1,
                settings.maximumSurvivalEnemiesPerWave);
            settings.additionalEnemiesPerWave = Mathf.Max(1, settings.additionalEnemiesPerWave);
            settings.waveBreakSeconds = IsFinite(settings.waveBreakSeconds)
                ? Mathf.Max(0f, settings.waveBreakSeconds)
                : 5f;
            settings.maximumRespawns = Mathf.Max(0, settings.maximumRespawns);
            settings.killTarget = Mathf.Max(1, settings.killTarget);
            settings.knockoutLives = Mathf.Clamp(settings.knockoutLives, 1, 10);
            if (settings.easyAI == null)
                settings.easyAI = new RoboClashAIDifficultyProfile();
            if (settings.normalAI == null)
                settings.normalAI = new RoboClashAIDifficultyProfile();
            if (settings.hardAI == null)
                settings.hardAI = new RoboClashAIDifficultyProfile();
        }

        private void Start()
        {
            if (RoboClashSession.ConsumeMatchSelection(out var selectedRobot, out var opponents,
                    out var difficulty, out var gameMode, out var survivalGrowth,
                    out var selectedWaveBreakSeconds, out var selectedTimerOption,
                    out var selectedKillTargetOption, out var selectedKnockoutLives,
                    out var allowRespawns, out var duelKillTarget))
            {
                settings.playerRobotType = selectedRobot;
                settings.aiOpponents = Mathf.Clamp(opponents, 1, 3);
                settings.startingSurvivalEnemies = Mathf.Clamp(opponents, 1,
                    settings.maximumSurvivalEnemiesPerWave);
                settings.matchDurationSeconds =
                    new[] { 180f, 300f, 420f }[Mathf.Clamp(selectedTimerOption, 0, 2)];
                settings.enableTimeLimit = true;
                settings.aiDifficulty = difficulty;
                settings.gameMode = gameMode;
                settings.additionalEnemiesPerWave = survivalGrowth;
                settings.waveBreakSeconds = selectedWaveBreakSeconds;
                settings.killTarget = new[] { 5, 10, 15, 20 }[Mathf.Clamp(selectedKillTargetOption, 0, 3)];
                settings.knockoutLives = selectedKnockoutLives;
                settings.respawnEnabled = allowRespawns;
                settings.duelUsesKillTarget = duelKillTarget;
                settings.duelRespawnsEnabled = allowRespawns;
            }

            settings.enableTimeLimit = IsTimedMatch();
            InitializeMatch();
        }

        private void Update()
        {
            if (State == RoboClashMatchState.Playing)
            {
                elapsedMatchSeconds += Time.deltaTime;
                UpdateKingControl();
                if (settings.enableTimeLimit && IsTimedMatch())
                {
                    remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
                    MatchTimerChanged?.Invoke(remainingTime);

                    if (remainingTime <= 0f && !eliminationEvaluationPending)
                        ResolveTimeout();
                }
                else if (settings.gameMode == RoboClashGameMode.Survival)
                {
                    MatchTimerChanged?.Invoke(elapsedMatchSeconds);
                }

                if (Time.time >= nextModeStatusUpdate)
                {
                    nextModeStatusUpdate = Time.time + 0.2f;
                    ModeStatusChanged?.Invoke();
                }
            }

            if (Keyboard.current == null || !Keyboard.current.pKey.wasPressedThisFrame)
                return;

            if (State == RoboClashMatchState.Playing)
                PauseMatch();
            else if (State == RoboClashMatchState.Paused)
                ResumeMatch();
        }

        private void OnDestroy()
        {
            StopWaveRoutine();
            if (hasPausedTime)
                Time.timeScale = timeScaleBeforePause;

            if (State == RoboClashMatchState.Paused)
            {
                SetParticipantInputLock(false);
                SetParticipantSimulationPaused(false);
                RestoreCursorState();
            }

            UnsubscribeParticipants();
            RestoreCameraParent();
            RestorePreviousSceneObjects();
            if (kingZoneMaterial != null)
                Destroy(kingZoneMaterial);
            if (activeInstance == this)
                activeInstance = null;
        }

        public void ConfigureRobotPrefabs(GameObject[] prefabs)
        {
            robotPrefabs = prefabs != null ? (GameObject[])prefabs.Clone() : Array.Empty<GameObject>();
        }

        public bool TryFindNearestOpponent(ArenaDamageable seeker, float range,
            out ArenaDamageable nearestOpponent)
        {
            nearestOpponent = null;
            if (seeker == null || State != RoboClashMatchState.Playing)
                return false;

            var seekerDefinition = seeker.GetComponent<RoboClashRobotDefinition>();
            if (seekerDefinition == null)
                return false;

            var nearestDistance = Mathf.Max(0f, range) * Mathf.Max(0f, range);
            foreach (var participant in participants)
            {
                var candidate = participant.Damageable;
                if (!IsValidOpponent(seeker, candidate, seekerDefinition))
                    continue;

                var distance = (candidate.transform.position - seeker.transform.position).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;

                nearestOpponent = candidate;
                nearestDistance = distance;
            }

            return nearestOpponent != null;
        }

        public bool IsValidOpponent(ArenaDamageable seeker, ArenaDamageable candidate)
        {
            if (seeker == null)
                return false;
            return IsValidOpponent(seeker, candidate, seeker.GetComponent<RoboClashRobotDefinition>());
        }

        private bool IsValidOpponent(ArenaDamageable seeker, ArenaDamageable candidate,
            RoboClashRobotDefinition seekerDefinition)
        {
            if (State != RoboClashMatchState.Playing || seekerDefinition == null ||
                candidate == null || candidate == seeker || candidate.IsDead ||
                !candidate.isActiveAndEnabled || !candidate.gameObject.activeInHierarchy)
                return false;

            var candidateDefinition = candidate.GetComponent<RoboClashRobotDefinition>();
            if (candidateDefinition == null || candidateDefinition.teamId == seekerDefinition.teamId)
                return false;

            var candidateAbilities = candidate.GetComponent<RoboClashRobotAbilities>();
            if (candidateAbilities != null && candidateAbilities.IsCloaked)
                return false;

            foreach (var participant in participants)
            {
                if (participant.Damageable == candidate)
                    return true;
            }

            return false;
        }

        private RoboClashAIDifficultyProfile GetAIDifficultyProfile(RoboClashAIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case RoboClashAIDifficulty.Easy: return settings.easyAI;
                case RoboClashAIDifficulty.Hard: return settings.hardAI;
                default: return settings.normalAI;
            }
        }

        public void ReturnToMainMenu()
        {
            const string menuSceneName = "RoboClashMainMenu";
            if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
            {
                Debug.LogError(
                    "Cannot return to the main menu: RoboClashMainMenu is not included in Build Settings. " +
                    "Use Tools > RoboClash > Create Main Menu UI to create and register it.",
                    this);
                return;
            }

            if (hasPausedTime)
            {
                Time.timeScale = timeScaleBeforePause;
                hasPausedTime = false;
            }

            SceneManager.LoadScene(menuSceneName);
        }

        public bool ValidateConfiguration(out string message)
        {
            if (settings == null)
            {
                message = "Match settings are missing.";
                return false;
            }

            var expectedPrefabCount = Enum.GetValues(typeof(RoboClashRobotType)).Length;
            if (!Enum.IsDefined(typeof(RoboClashRobotType), settings.playerRobotType))
            {
                message = "The selected player robot type is invalid.";
                return false;
            }
            if (!Enum.IsDefined(typeof(RoboClashGameMode), settings.gameMode))
            {
                message = "The selected game mode is invalid.";
                return false;
            }
            if (settings.gameMode == RoboClashGameMode.Survival &&
                (settings.startingSurvivalEnemies < 1 ||
                 settings.maximumSurvivalEnemiesPerWave < settings.startingSurvivalEnemies ||
                 settings.additionalEnemiesPerWave < 1 || settings.waveBreakSeconds < 0f ||
                 !IsFinite(settings.waveBreakSeconds)))
            {
                message = "Survival wave settings must use a positive starting size/growth, " +
                          "a cap at least as large as the starting size, and a finite non-negative break.";
                return false;
            }
            if (settings.killTarget < 1 || settings.knockoutLives < 1 || settings.knockoutLives > 10 ||
                (settings.gameMode == RoboClashGameMode.KingOfTheArena &&
                 !IsFinite(settings.matchDurationSeconds)))
            {
                message = "Kill targets, Knockout lives, and timed mode durations must be valid finite values.";
                return false;
            }
            if (robotPrefabs == null || robotPrefabs.Length != expectedPrefabCount)
            {
                message = $"Expected {expectedPrefabCount} generated robot prefab references.";
                return false;
            }

            for (var i = 0; i < expectedPrefabCount; i++)
            {
                var prefab = robotPrefabs[i];
                if (prefab == null)
                {
                    message = $"Robot prefab slot {i} is empty.";
                    return false;
                }

                if (prefab.GetComponents<ArenaDamageable>().Length != 1)
                {
                    message = $"Robot prefab '{prefab.name}' has duplicate damage components. " +
                              "Run Tools > RoboClash > Repair Generated Robot Damage Components.";
                    return false;
                }

                if (prefab.GetComponent<RoboClashRobotDefinition>() == null ||
                    prefab.GetComponent<RobotArenaController>() == null ||
                    prefab.GetComponent<RoboClashRobotMovement>() == null ||
                    prefab.GetComponent<RoboClashRobotCombat>() == null ||
                    prefab.GetComponent<RoboClashRobotAbilities>() == null ||
                    prefab.GetComponent<RoboClashRobotAI>() == null ||
                    prefab.GetComponent<CharacterController>() == null)
                {
                    message = $"Robot prefab '{prefab.name}' is missing a required gameplay component.";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        public string GetScoreboardText()
        {
            var ordered = new List<Participant>(participants);
            ordered.Sort((left, right) =>
            {
                var scoreComparison = settings.gameMode == RoboClashGameMode.KingOfTheArena
                    ? right.KingScore.CompareTo(left.KingScore)
                    : right.Kills.CompareTo(left.Kills);
                return scoreComparison != 0 ? scoreComparison : string.CompareOrdinal(left.Id, right.Id);
            });

            var lines = new List<string>(ordered.Count);
            foreach (var participant in ordered)
            {
                var definition = participant.Controller != null
                    ? participant.Controller.GetComponent<RoboClashRobotDefinition>()
                    : null;
                var name = definition != null ? definition.robotType.ToString().ToUpperInvariant() : participant.Id;
                var line = $"{(participant.IsPlayer ? "> " : string.Empty)}{name}  {participant.Kills}K / {participant.Deaths}D";
                if (settings.gameMode == RoboClashGameMode.Knockout)
                    line += $"  {participant.LivesRemaining}L";
                else if (settings.gameMode == RoboClashGameMode.KingOfTheArena)
                    line += $"  {Mathf.FloorToInt(participant.KingScore)}s";
                lines.Add(line);
            }
            if (settings.gameMode == RoboClashGameMode.Survival)
                lines.AddRange(archivedScoreRows);

            return string.Join("\n", lines);
        }

        public int GetKingScore(ArenaDamageable participant)
        {
            var entry = GetParticipant(participant);
            return entry != null ? Mathf.FloorToInt(entry.KingScore) : 0;
        }

        private void CreateKingZone()
        {
            if (kingZone != null)
            {
                kingZone.ClearOccupants();
                return;
            }

            var zoneObject = new GameObject("King of the Arena Control Zone");
            zoneObject.transform.SetParent(transform, false);
            zoneObject.transform.position = Vector3.zero;
            kingZone = zoneObject.AddComponent<KingOfArenaZone>();
            var trigger = zoneObject.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up * 1.5f;
            trigger.size = new Vector3(10f, 3f, 10f);

            var zoneShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (zoneShader == null)
            {
                Debug.LogError("King of the Arena zone visuals could not be created: no supported material shader was found.",
                    this);
                return;
            }
            kingZoneMaterial = new Material(zoneShader) { name = "RoboClash King Zone" };
            kingZoneMaterial.color = new Color(0.04f, 0.62f, 0.85f, 1f);
            if (kingZoneMaterial.HasProperty("_EmissionColor"))
            {
                kingZoneMaterial.EnableKeyword("_EMISSION");
                kingZoneMaterial.SetColor("_EmissionColor", new Color(0.02f, 0.35f, 0.65f));
            }

            CreateKingZoneVisual("Control Zone Platform", zoneObject.transform,
                Vector3.up * 0.22f, new Vector3(10f, 0.12f, 10f), kingZoneMaterial);
            CreateKingZoneVisual("Control Zone North Marker", zoneObject.transform,
                new Vector3(0f, 0.24f, 4.75f), new Vector3(10f, 0.12f, 0.5f), kingZoneMaterial);
            CreateKingZoneVisual("Control Zone South Marker", zoneObject.transform,
                new Vector3(0f, 0.24f, -4.75f), new Vector3(10f, 0.12f, 0.5f), kingZoneMaterial);
            CreateKingZoneVisual("Control Zone East Marker", zoneObject.transform,
                new Vector3(4.75f, 0.24f, 0f), new Vector3(0.5f, 0.12f, 10f), kingZoneMaterial);
            CreateKingZoneVisual("Control Zone West Marker", zoneObject.transform,
                new Vector3(-4.75f, 0.24f, 0f), new Vector3(0.5f, 0.12f, 10f), kingZoneMaterial);
        }

        private static void CreateKingZoneVisual(
            string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = size;
            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            visual.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void UpdateKingControl()
        {
            if (settings.gameMode != RoboClashGameMode.KingOfTheArena || kingZone == null)
                return;

            ArenaDamageable soleOccupant = null;
            foreach (var occupant in kingZone.Occupants)
            {
                if (occupant == null || occupant.IsDead || !IsParticipant(occupant))
                    continue;

                if (soleOccupant != null)
                {
                    kingController = null;
                    kingContested = true;
                    return;
                }

                soleOccupant = occupant;
            }

            if (soleOccupant == null)
            {
                kingController = null;
                kingContested = false;
                return;
            }

            var participant = FindParticipant(soleOccupant);
            if (participant == null)
                return;
            kingController = soleOccupant;
            kingContested = false;
            participant.KingScore += Time.deltaTime;
        }

        private bool IsTimedMatch()
        {
            return settings.gameMode == RoboClashGameMode.FreeForAll ||
                   settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
                   settings.gameMode == RoboClashGameMode.KingOfTheArena ||
                   (settings.gameMode == RoboClashGameMode.Duel && !settings.duelUsesKillTarget);
        }

        private bool IsKillTargetMode()
        {
            return settings.gameMode == RoboClashGameMode.KillRace ||
                   (settings.gameMode == RoboClashGameMode.Duel && settings.duelUsesKillTarget);
        }

        public void PauseMatch()
        {
            if (State != RoboClashMatchState.Playing)
                return;

            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            cursorLockBeforePause = Cursor.lockState;
            cursorVisibleBeforePause = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            hasPausedTime = true;
            SetParticipantInputLock(true);
            SetParticipantSimulationPaused(true);
            Time.timeScale = 0f;
            SetState(RoboClashMatchState.Paused);
            Debug.Log("RoboClash match paused.", this);
        }

        public void ResumeMatch()
        {
            if (State != RoboClashMatchState.Paused)
                return;

            Time.timeScale = hasPausedTime ? timeScaleBeforePause : 1f;
            hasPausedTime = false;
            SetState(RoboClashMatchState.Playing);
            SetParticipantInputLock(false);
            SetParticipantSimulationPaused(false);
            RestoreCursorState();
            if (player != null && !player.IsDead)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            Debug.Log("RoboClash match resumed.", this);
        }

        public void RestartMatch()
        {
            if (State == RoboClashMatchState.Initializing)
                return;

            Debug.Log("Restarting RoboClash match.", this);
            if (hasPausedTime)
            {
                Time.timeScale = timeScaleBeforePause;
                hasPausedTime = false;
                RestoreCursorState();
            }

            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
                countdownRoutine = null;
            }
            if (eliminationRoutine != null)
            {
                StopCoroutine(eliminationRoutine);
                eliminationRoutine = null;
            }
            StopWaveRoutine();

            UnsubscribeParticipants();
            participants.Clear();
            currentWaveEnemies.Clear();
            archivedScoreRows.Clear();
            lastDamageSources.Clear();
            player = null;
            PlayerRegistered?.Invoke(null);
            initializationComplete = false;
            currentWave = 0;
            elapsedMatchSeconds = 0f;
            waveBreakActive = false;
            waveBreakEndsAt = 0f;
            kingController = null;
            kingContested = false;
            kingZone?.ClearOccupants();
            SetState(RoboClashMatchState.Initializing);
            if (participantRoot != null)
                Destroy(participantRoot.gameObject);

            StartCoroutine(RestartAfterCleanup());
        }

        private IEnumerator RestartAfterCleanup()
        {
            yield return null;
            InitializeMatch();
        }

        private void InitializeMatch()
        {
            if (initializationComplete)
                return;

            if (!ValidateConfiguration(out var configurationIssue))
            {
                Debug.LogError("RoboClash match initialization failed: " + configurationIssue, this);
                return;
            }

            playerCamera = FindMainCamera();
            if (playerCamera == null)
            {
                Debug.LogError("RoboClash match initialization failed: exactly one active MainCamera is required.", this);
                return;
            }

            var zones = FindSpawnZones();
            if (!HasPlayableSurface(zones))
            {
                Debug.LogError("RoboClash match initialization failed: no arena floor or spawn zones were found.", this);
                return;
            }

            Debug.Log($"Initializing RoboClash {settings.gameMode} match.", this);
            if (Time.timeScale <= 0f)
                Time.timeScale = 1f;
            DetachCameraFromRobot();
            participantRoot = new GameObject("RoboClash Match Participants").transform;
            participantRoot.SetParent(transform.root, false);
            DeactivateExistingRobotInstances();
            BuildNavigationMesh();
            nextParticipantId = 0;
            kingController = null;
            kingContested = false;
            if (settings.gameMode == RoboClashGameMode.KingOfTheArena)
                CreateKingZone();
            statistics = default;
            statistics.SetGameMode(settings.gameMode);
            remainingTime = settings.matchDurationSeconds;
            elapsedMatchSeconds = 0f;
            currentWave = settings.gameMode == RoboClashGameMode.Survival ? 1 : 0;
            waveBreakActive = false;
            countdownValue = 0;
            eliminationEvaluationPending = false;
            currentWaveEnemies.Clear();
            archivedScoreRows.Clear();
            try
            {
                var opponentCount = GetInitialOpponentCount();
                var opponentTypes = GetOpponentRobotTypes(opponentCount);
                var spawnPositions = GetSpawnPositions(zones, opponentTypes);
                if (spawnPositions == null)
                {
                    Debug.LogError(
                        "RoboClash match initialization failed: could not find enough safe, distinct spawn positions.",
                        this);
                    return;
                }

                SpawnParticipants(spawnPositions, opponentTypes);
                if (participants.Count != opponentCount + 1 || player == null)
                {
                    Debug.LogError(
                        "RoboClash match initialization failed: one or more participants could not be created.",
                        this);
                    return;
                }

                initializationComplete = true;
                if (settings.gameMode == RoboClashGameMode.Survival)
                {
                    statistics.RecordWave(currentWave);
                    StatisticsChanged?.Invoke(statistics);
                    ModeStatusChanged?.Invoke();
                }
                MatchTimerChanged?.Invoke(remainingTime);
                StatisticsChanged?.Invoke(statistics);
                SetState(RoboClashMatchState.Ready);
                Debug.Log($"Spawned the player and {opponentCount} AI opponent(s).", this);
                countdownRoutine = StartCoroutine(RunCountdown());
            }
            finally
            {
                if (!initializationComplete)
                {
                    UnsubscribeParticipants();
                    participants.Clear();
                    player = null;
                    if (participantRoot != null)
                        Destroy(participantRoot.gameObject);
                    participantRoot = null;
                    RestorePreviousSceneObjects();
                }
            }
        }

        private void BuildNavigationMesh()
        {
            if (navSurface != null && navSurface.navMeshData != null)
                return;

            foreach (var lava in FindObjectsByType<LavaPit>())
            {
                if (lava.gameObject.scene == gameObject.scene)
                    AddBlockedVolume(lava.gameObject);
            }
            foreach (var spikes in FindObjectsByType<SpikeTrap>())
            {
                if (spikes.gameObject.scene == gameObject.scene)
                    AddBlockedVolume(spikes.gameObject);
            }

            navSurface = gameObject.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navSurface.BuildNavMesh();
        }

        private static void AddBlockedVolume(GameObject hazard)
        {
            var box = hazard.GetComponent<BoxCollider>();
            if (box == null || hazard.GetComponent<NavMeshModifierVolume>() != null)
                return;

            var volume = hazard.AddComponent<NavMeshModifierVolume>();
            volume.center = box.center;
            volume.size = box.size;
            volume.area = 1;
        }

        private void SpawnParticipants(Vector3[] spawnPositions, RoboClashRobotType[] opponentTypes)
        {
            var zones = FindSpawnZones();
            SpawnParticipant(settings.playerRobotType, spawnPositions[0], zones.Length > 0 ? zones[0] : null,
                true);
            for (var i = 0; i < opponentTypes.Length; i++)
            {
                var zone = i + 1 < zones.Length ? zones[i + 1] : FindNearestSpawnZone(zones, spawnPositions[i + 1]);
                var damageable = SpawnParticipant(opponentTypes[i], spawnPositions[i + 1], zone, false);
                if (settings.gameMode == RoboClashGameMode.Survival && damageable != null)
                    currentWaveEnemies.Add(damageable);
            }
        }

        private ArenaDamageable SpawnParticipant(RoboClashRobotType type, Vector3 position,
            ArenaSpawnZone spawnZone, bool isPlayer)
        {
            var prefab = robotPrefabs[(int)type];
            if (prefab == null)
            {
                Debug.LogError($"Cannot spawn {type}: its generated prefab reference is missing.", this);
                return null;
            }

            var spawnRotation = GetSpawnRotation(position, spawnZone);
            var robot = Instantiate(prefab, position, spawnRotation, participantRoot);
            var participantIndex = participants.Count;
            robot.name = isPlayer ? $"Player {type.ToString().ToUpperInvariant()}" :
                $"AI Wave {Mathf.Max(1, currentWave)} {participantIndex} {type.ToString().ToUpperInvariant()}";

            var controller = robot.GetComponent<RobotArenaController>();
            var definition = robot.GetComponent<RoboClashRobotDefinition>();
            var movement = robot.GetComponent<RoboClashRobotMovement>();
            var combat = robot.GetComponent<RoboClashRobotCombat>();
            var abilities = robot.GetComponent<RoboClashRobotAbilities>();
            var ai = robot.GetComponent<RoboClashRobotAI>();
            var damageable = robot.GetComponent<ArenaDamageable>();

            definition.teamId = participantIndex;
            switch (settings.gameMode)
            {
                case RoboClashGameMode.KillRace:
                case RoboClashGameMode.TimedDeathmatch:
                    controller.ConfigureUnlimitedMatchRespawns();
                    break;
                case RoboClashGameMode.Duel:
                    if (settings.duelRespawnsEnabled)
                        controller.ConfigureUnlimitedMatchRespawns();
                    else
                        controller.ConfigureMatchRespawns(0);
                    break;
                case RoboClashGameMode.KingOfTheArena:
                    if (settings.respawnEnabled)
                        controller.ConfigureUnlimitedMatchRespawns();
                    else
                        controller.ConfigureMatchRespawns(0);
                    break;
                case RoboClashGameMode.Knockout:
                    controller.ConfigureMatchRespawns(settings.knockoutLives - 1);
                    break;
                default:
                    var allowRespawns = settings.gameMode != RoboClashGameMode.Survival &&
                                        settings.respawnEnabled;
                    controller.ConfigureMatchRespawns(allowRespawns ? settings.maximumRespawns : 0);
                    break;
            }
            controller.Configure(isPlayer ? playerCamera : null, definition.muzzle, spawnZone);
            controller.ConfigureMatchRespawnLocation(position, spawnRotation);
            movement.SetMatchInputLocked(true);
            movement.SetControlled(isPlayer);
            combat.ConfigurePlayer(isPlayer);
            if (!isPlayer)
            {
                var difficulty = GetAIDifficultyForCurrentWave();
                ai.Configure(this, difficulty, GetAIDifficultyProfile(difficulty));
            }
            ai.enabled = !isPlayer;

            var participant = new Participant
            {
                Id = $"participant-{nextParticipantId++:D2}",
                Controller = controller,
                Damageable = damageable,
                IsPlayer = isPlayer,
                LivesRemaining = settings.gameMode == RoboClashGameMode.Knockout
                    ? settings.knockoutLives
                    : int.MaxValue,
                AIEnabled = ai.enabled,
                AbilitiesEnabled = abilities.enabled,
                CombatEnabled = combat.enabled
            };
            participants.Add(participant);
            damageable.DamageApplied += OnParticipantDamageApplied;
            damageable.Eliminated += OnParticipantEliminated;
            controller.Respawned += OnParticipantRespawned;

            if (isPlayer)
            {
                player = controller;
                PlayerRegistered?.Invoke(controller);
            }

            Debug.Log($"Spawned {(isPlayer ? "player" : "AI opponent")} {type} at {robot.transform.position}.",
                robot);
            return damageable;
        }

        private static ArenaSpawnZone FindNearestSpawnZone(ArenaSpawnZone[] zones, Vector3 position)
        {
            ArenaSpawnZone nearest = null;
            var nearestDistance = float.PositiveInfinity;
            foreach (var zone in zones)
            {
                var distance = (zone.transform.position - position).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;
                nearest = zone;
                nearestDistance = distance;
            }

            return nearest;
        }

        private IEnumerator RunCountdown()
        {
            SetState(RoboClashMatchState.Countdown);
            var elapsed = 0f;
            var previousValue = -1;
            while (elapsed < settings.countdownDurationSeconds)
            {
                var value = Mathf.CeilToInt(settings.countdownDurationSeconds - elapsed);
                if (value != previousValue)
                {
                    countdownValue = value;
                    CountdownChanged?.Invoke(value);
                    previousValue = value;
                }

                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            countdownValue = 0;
            CountdownChanged?.Invoke(0);
            SetState(RoboClashMatchState.Playing);
            SetParticipantInputLock(false);
            countdownRoutine = null;
            Debug.Log("RoboClash match started.", this);
            MatchStarted?.Invoke();
        }

        private void OnParticipantDamageApplied(ArenaDamageable target, float amount, ArenaDamageable instigator)
        {
            if (State != RoboClashMatchState.Playing || !IsParticipant(target))
                return;

            if (instigator != null)
                lastDamageSources[target] = instigator;
            else
                lastDamageSources.Remove(target);

            if (instigator == player)
                statistics.RecordDamageDealt(amount);
            if (target == player)
                statistics.RecordDamageReceived(amount);

            StatisticsChanged?.Invoke(statistics);
        }

        private void OnParticipantEliminated(ArenaDamageable eliminated)
        {
            if (State != RoboClashMatchState.Playing || !IsParticipant(eliminated))
                return;

            var participant = FindParticipant(eliminated);
            if (participant == null || participant.DeathProcessed)
                return;

            participant.DeathProcessed = true;
            participant.Deaths++;
            if (settings.gameMode == RoboClashGameMode.Knockout)
                participant.LivesRemaining = Mathf.Max(0, participant.LivesRemaining - 1);

            CancelProjectilesOwnedBy(eliminated.gameObject);
            lastDamageSources.TryGetValue(eliminated, out var killer);
            var creditedKiller = killer != null && killer != eliminated
                ? FindParticipant(killer)
                : null;
            if (creditedKiller != null && creditedKiller != participant)
                creditedKiller.Kills++;
            else
                creditedKiller = null;

            participant.IsEliminated = !participant.Controller.IsRespawning;
            ParticipantEliminated?.Invoke(eliminated, creditedKiller != null ? creditedKiller.Damageable : null);
            if (participant.IsPlayer)
            {
                statistics.RecordPlayerDeath();
                if (settings.gameMode == RoboClashGameMode.FreeForAll)
                {
                    var survivingCombatants = CountAliveCombatants();
                    if (survivingCombatants > 0)
                        statistics.SetPlayerPlacement(survivingCombatants + 1);
                }
            }
            if (creditedKiller != null && creditedKiller.IsPlayer && !participant.IsPlayer)
                statistics.RecordElimination();

            lastDamageSources.Remove(eliminated);
            StatisticsChanged?.Invoke(statistics);
            ModeStatusChanged?.Invoke();
            Debug.Log($"Participant '{eliminated.name}' was eliminated" +
                      (creditedKiller != null ? $" by '{creditedKiller.Damageable.name}'." : " without kill credit."),
                eliminated);
            if (!participant.Controller.IsRespawning || IsKillTargetMode())
                ScheduleEliminationEvaluation();
        }

        private void OnParticipantRespawned(RobotArenaController controller)
        {
            var participant = controller != null ? FindParticipant(controller) : null;
            if (participant == null || State != RoboClashMatchState.Playing)
                return;

            participant.DeathProcessed = false;
            participant.IsEliminated = false;
            lastDamageSources.Remove(participant.Damageable);
            ModeStatusChanged?.Invoke();
        }

        private void ScheduleEliminationEvaluation()
        {
            if (eliminationEvaluationPending || State != RoboClashMatchState.Playing)
                return;

            eliminationEvaluationPending = true;
            eliminationRoutine = StartCoroutine(EvaluateEliminationsAtFrameEnd());
        }

        private IEnumerator EvaluateEliminationsAtFrameEnd()
        {
            yield return new WaitForEndOfFrame();
            eliminationRoutine = null;
            eliminationEvaluationPending = false;
            EvaluateEliminationResult();
        }

        private void EvaluateEliminationResult()
        {
            if (State != RoboClashMatchState.Playing)
                return;

            if (IsKillTargetMode())
            {
                ResolveKillTarget();
                if (State == RoboClashMatchState.Playing && settings.gameMode == RoboClashGameMode.Duel &&
                    !settings.duelRespawnsEnabled)
                {
                    if (CountAliveCombatants() == 0)
                        EndMatch(RoboClashMatchState.Draw);
                    else if (CountAliveCombatants() == 1)
                        EndMatch(IsPlayerAlive() ? RoboClashMatchState.Victory : RoboClashMatchState.Defeat);
                }
                return;
            }

            if (settings.gameMode == RoboClashGameMode.Survival)
            {
                if (!IsPlayerAlive())
                    EndMatch(RoboClashMatchState.Defeat);
                else if (CountLivingWaveEnemies() == 0 && !waveBreakActive)
                    StartWaveBreak();
                return;
            }

            if (settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
                settings.gameMode == RoboClashGameMode.KingOfTheArena)
                return;

            if (settings.gameMode == RoboClashGameMode.Knockout)
            {
                var remaining = 0;
                var playerSurvives = false;
                foreach (var participant in participants)
                {
                    if (participant.IsEliminated || !IsAlive(participant))
                        continue;
                    remaining++;
                    playerSurvives = participant.IsPlayer;
                }

                if (remaining == 0)
                    EndMatch(RoboClashMatchState.Draw);
                else if (remaining == 1)
                    EndMatch(playerSurvives ? RoboClashMatchState.Victory : RoboClashMatchState.Defeat);
                return;
            }

            var playerAlive = IsPlayerAlive();
            var aliveOpponents = CountAliveOpponents();
            var aliveCombatants = CountAliveCombatants();
            if (settings.gameMode == RoboClashGameMode.FreeForAll && aliveCombatants > 1)
                return;

            if (aliveCombatants == 0)
                EndMatch(RoboClashMatchState.Draw);
            else if (playerAlive)
                EndMatch(RoboClashMatchState.Victory);
            else if (aliveOpponents == 0)
                EndMatch(RoboClashMatchState.Draw);
            else
                EndMatch(RoboClashMatchState.Defeat);
        }

        private void ResolveKillTarget()
        {
            Participant winner = null;
            foreach (var participant in participants)
            {
                if (participant.Kills < settings.killTarget)
                    continue;

                if (winner == null || participant.Kills > winner.Kills ||
                    (participant.Kills == winner.Kills &&
                     string.CompareOrdinal(participant.Id, winner.Id) < 0))
                    winner = participant;
            }

            if (winner != null)
                EndMatch(winner.IsPlayer ? RoboClashMatchState.Victory : RoboClashMatchState.Defeat);
        }

        private void StartWaveBreak()
        {
            if (settings.gameMode != RoboClashGameMode.Survival || waveBreakActive ||
                waveRoutine != null || State != RoboClashMatchState.Playing)
                return;

            waveBreakActive = true;
            waveBreakEndsAt = Time.time + settings.waveBreakSeconds;
            statistics.RecordWaveCompleted(currentWave);
            StatisticsChanged?.Invoke(statistics);
            RemoveCompletedWaveParticipants();
            ModeStatusChanged?.Invoke();
            waveRoutine = StartCoroutine(SpawnNextSurvivalWave());
        }

        private void RemoveCompletedWaveParticipants()
        {
            foreach (var enemy in currentWaveEnemies)
            {
                var participant = FindParticipant(enemy);
                if (participant == null || IsAlive(participant))
                    continue;
                var definition = participant.Controller != null
                    ? participant.Controller.GetComponent<RoboClashRobotDefinition>()
                    : null;
                var name = definition != null ? definition.robotType.ToString().ToUpperInvariant() : participant.Id;
                archivedScoreRows.Add($"{name}  {participant.Kills}K / {participant.Deaths}D");
                if (participant.Damageable != null)
                {
                    participant.Damageable.DamageApplied -= OnParticipantDamageApplied;
                    participant.Damageable.Eliminated -= OnParticipantEliminated;
                }
                if (participant.Controller != null)
                    participant.Controller.Respawned -= OnParticipantRespawned;
                participants.Remove(participant);
                if (participant.Controller != null)
                    Destroy(participant.Controller.gameObject);
            }

            currentWaveEnemies.Clear();
            lastDamageSources.Clear();
        }

        private IEnumerator SpawnNextSurvivalWave()
        {
            if (settings.waveBreakSeconds > 0f)
                yield return new WaitForSeconds(settings.waveBreakSeconds);
            else
                yield return null;

            waveRoutine = null;
            if (State != RoboClashMatchState.Playing || !IsPlayerAlive())
            {
                waveBreakActive = false;
                ModeStatusChanged?.Invoke();
                yield break;
            }

            currentWave++;
            var requestedCount = (long)settings.startingSurvivalEnemies +
                                 (long)(currentWave - 1) * settings.additionalEnemiesPerWave;
            var enemyCount = (int)Math.Min(settings.maximumSurvivalEnemiesPerWave, requestedCount);
            var enemyTypes = GetOpponentRobotTypes(enemyCount);
            var zones = FindSpawnZones();
            if (!TryGetSurvivalSpawnPositions(zones, enemyTypes, out var positions))
            {
                waveBreakActive = false;
                Debug.LogError(
                    $"Survival wave {currentWave} could not find {enemyCount} safe, separated spawn positions. " +
                    "Reduce the wave size or add safe arena spawn areas.",
                    this);
                ModeStatusChanged?.Invoke();
                EndMatch(RoboClashMatchState.Defeat);
                yield break;
            }

            currentWaveEnemies.Clear();
            for (var i = 0; i < enemyCount; i++)
            {
                if (State != RoboClashMatchState.Playing || !IsPlayerAlive())
                    break;

                var zone = FindNearestSpawnZone(zones, positions[i]);
                var spawnedEnemy = SpawnParticipant(enemyTypes[i], positions[i], zone, false);
                if (spawnedEnemy != null)
                    currentWaveEnemies.Add(spawnedEnemy);
            }

            if (currentWaveEnemies.Count != enemyCount)
            {
                Debug.LogError($"Survival wave {currentWave} spawned {currentWaveEnemies.Count} of " +
                               $"{enemyCount} requested enemies.", this);
                waveBreakActive = false;
                ModeStatusChanged?.Invoke();
                EndMatch(RoboClashMatchState.Defeat);
                yield break;
            }

            SetParticipantInputLock(false);
            waveBreakActive = false;
            UpdateActiveAIDifficulty();
            statistics.RecordWave(currentWave);
            StatisticsChanged?.Invoke(statistics);
            ModeStatusChanged?.Invoke();
            Debug.Log($"Survival wave {currentWave} started with {enemyCount} AI opponent(s).", this);
        }

        private void StopWaveRoutine()
        {
            if (waveRoutine == null)
                return;

            StopCoroutine(waveRoutine);
            waveRoutine = null;
            waveBreakActive = false;
            waveBreakEndsAt = 0f;
        }

        private int CountLivingWaveEnemies()
        {
            var count = 0;
            foreach (var enemy in currentWaveEnemies)
            {
                var participant = FindParticipant(enemy);
                if (participant != null && IsAlive(participant))
                    count++;
            }

            return count;
        }

        private int GetInitialOpponentCount()
        {
            switch (settings.gameMode)
            {
                case RoboClashGameMode.Duel:
                    return 1;
                case RoboClashGameMode.Survival:
                    return Mathf.Clamp(settings.startingSurvivalEnemies, 1,
                        settings.maximumSurvivalEnemiesPerWave);
                default:
                    return Mathf.Clamp(settings.aiOpponents, 1, 3);
            }
        }

        private RoboClashAIDifficulty GetAIDifficultyForCurrentWave()
        {
            if (settings.gameMode != RoboClashGameMode.Survival)
                return settings.aiDifficulty;

            if (settings.aiDifficulty == RoboClashAIDifficulty.Easy)
                return currentWave >= 10 ? RoboClashAIDifficulty.Hard :
                    currentWave >= 5 ? RoboClashAIDifficulty.Normal : RoboClashAIDifficulty.Easy;
            if (settings.aiDifficulty == RoboClashAIDifficulty.Normal && currentWave >= 5)
                return RoboClashAIDifficulty.Hard;
            return settings.aiDifficulty;
        }

        private void UpdateActiveAIDifficulty()
        {
            var difficulty = GetAIDifficultyForCurrentWave();
            var profile = GetAIDifficultyProfile(difficulty);
            foreach (var participant in participants)
            {
                if (participant.IsPlayer || !IsAlive(participant))
                    continue;
                var ai = participant.Controller.GetComponent<RoboClashRobotAI>();
                if (ai != null && ai.enabled)
                    ai.Configure(this, difficulty, profile);
            }
        }

        private bool TryGetSurvivalSpawnPositions(ArenaSpawnZone[] zones,
            RoboClashRobotType[] enemyTypes, out Vector3[] positions)
        {
            positions = new Vector3[enemyTypes.Length];
            var assigned = 0;
            var spawnPointCount = zones.Length > 0 ? zones.Length : FallbackSpawns.Length;
            for (var enemyIndex = 0; enemyIndex < enemyTypes.Length; enemyIndex++)
            {
                var found = false;
                for (var zoneIndex = 0; zoneIndex < spawnPointCount && !found; zoneIndex++)
                {
                    var selectedZoneIndex = (zoneIndex + currentWave + enemyIndex) % spawnPointCount;
                    var anchor = zones.Length > 0
                        ? zones[selectedZoneIndex].transform.position
                        : FallbackSpawns[selectedZoneIndex];
                    for (var ring = 0; ring < 3 && !found; ring++)
                    {
                        var radius = 2.6f + ring * 1.7f;
                        for (var point = 0; point < 8; point++)
                        {
                            var angle = (point * 45f + currentWave * 23f + enemyIndex * 17f) * Mathf.Deg2Rad;
                            var candidate = anchor +
                                            new Vector3(Mathf.Cos(angle) * radius, 0.35f,
                                                Mathf.Sin(angle) * radius);
                            if (!IsSafeSpawn(candidate, enemyTypes[enemyIndex]) ||
                                IsNearLivingParticipant(candidate, enemyTypes[enemyIndex]) ||
                                IsNearReservedPosition(candidate, enemyTypes[enemyIndex], positions, assigned,
                                    enemyTypes))
                                continue;

                            positions[assigned++] = candidate;
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    positions = null;
                    return false;
                }
            }

            return true;
        }

        private bool IsNearLivingParticipant(Vector3 position, RoboClashRobotType type)
        {
            var radius = RoboClashRobotDefinition.GetTargetDimensions(type).x * 0.5f;
            foreach (var participant in participants)
            {
                if (!IsAlive(participant))
                    continue;
                var otherRadius = RoboClashRobotDefinition.GetTargetDimensions(
                    participant.Controller.GetComponent<RoboClashRobotDefinition>().robotType).x * 0.5f;
                var minimumDistance = radius + otherRadius + 1.5f;
                var difference = participant.Damageable.transform.position - position;
                difference.y = 0f;
                if (difference.sqrMagnitude < minimumDistance * minimumDistance)
                    return true;
            }

            return false;
        }

        private static bool IsNearReservedPosition(Vector3 position, RoboClashRobotType type,
            Vector3[] reserved, int reservedCount, RoboClashRobotType[] types)
        {
            var radius = RoboClashRobotDefinition.GetTargetDimensions(type).x * 0.5f;
            for (var i = 0; i < reservedCount; i++)
            {
                var otherRadius = RoboClashRobotDefinition.GetTargetDimensions(types[i]).x * 0.5f;
                var minimumDistance = radius + otherRadius + 1.5f;
                if ((reserved[i] - position).sqrMagnitude < minimumDistance * minimumDistance)
                    return true;
            }

            return false;
        }

        private void ResolveTimeout()
        {
            if (State != RoboClashMatchState.Playing)
                return;

            remainingTime = 0f;
            MatchTimerChanged?.Invoke(remainingTime);
            if (settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
                (settings.gameMode == RoboClashGameMode.Duel && !settings.duelUsesKillTarget))
            {
                ResolveTimedScore(false);
                return;
            }
            if (settings.gameMode == RoboClashGameMode.KingOfTheArena)
            {
                ResolveTimedScore(true);
                return;
            }

            var playerAlive = player != null && !player.IsDead;
            var livingOpponents = CountLivingOpponents();
            if (!playerAlive && livingOpponents == 0)
            {
                EndMatch(RoboClashMatchState.Draw);
                return;
            }
            if (!playerAlive)
            {
                EndMatch(RoboClashMatchState.Defeat);
                return;
            }
            if (livingOpponents == 0)
            {
                EndMatch(RoboClashMatchState.Victory);
                return;
            }

            var playerVitals = player.Health + player.Shield;
            var strongestOpponentVitals = 0f;
            foreach (var participant in participants)
            {
                if (participant.IsPlayer || !IsLiving(participant))
                    continue;
                strongestOpponentVitals = Mathf.Max(strongestOpponentVitals,
                    participant.Damageable.Health + participant.Damageable.Shield);
            }

            if (playerVitals > strongestOpponentVitals)
                EndMatch(RoboClashMatchState.Victory);
            else if (Mathf.Approximately(playerVitals, strongestOpponentVitals))
                EndMatch(RoboClashMatchState.Draw);
            else
                EndMatch(RoboClashMatchState.Defeat);
        }

        private void ResolveTimedScore(bool useKingScore)
        {
            var highest = float.NegativeInfinity;
            var leaders = new List<Participant>();
            foreach (var participant in participants)
            {
                var score = useKingScore ? participant.KingScore : participant.Kills;
                if (score > highest + 0.001f)
                {
                    highest = score;
                    leaders.Clear();
                    leaders.Add(participant);
                }
                else if (Mathf.Abs(score - highest) <= 0.001f)
                    leaders.Add(participant);
            }

            if (leaders.Count == 0)
            {
                EndMatch(RoboClashMatchState.Draw);
                return;
            }

            if (leaders.Count > 1)
                EndMatch(leaders.Exists(candidate => candidate.IsPlayer)
                    ? RoboClashMatchState.Draw
                    : RoboClashMatchState.Defeat);
            else
                EndMatch(leaders[0].IsPlayer
                    ? RoboClashMatchState.Victory
                    : RoboClashMatchState.Defeat);
        }

        private void EndMatch(RoboClashMatchState result)
        {
            if (State != RoboClashMatchState.Playing ||
                (result != RoboClashMatchState.Victory &&
                 result != RoboClashMatchState.Defeat &&
                 result != RoboClashMatchState.Draw))
                return;

            SetParticipantInputLock(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            CancelParticipantProjectiles();
            foreach (var participant in participants)
            {
                participant.Controller.LockForMatchEnd();
                participant.Controller.GetComponent<RoboClashRobotAI>().enabled = false;
                participant.Controller.GetComponent<RoboClashRobotAbilities>().enabled = false;
                participant.Controller.GetComponent<RoboClashRobotCombat>().enabled = false;
            }

            var matchDuration = settings.gameMode == RoboClashGameMode.Survival ||
                                !IsTimedMatch()
                ? elapsedMatchSeconds
                : settings.matchDurationSeconds - remainingTime;
            statistics.Complete(matchDuration, result);
            statistics.SetPlayerPlacement(GetPlayerPlacement(result));
            RoboClashSaveService.RecordMatch(statistics, settings.playerRobotType, progressionRewards);
            SetState(result);
            StopWaveRoutine();
            StatisticsChanged?.Invoke(statistics);
            Debug.Log($"RoboClash match ended: {result}.", this);
            MatchEnded?.Invoke(statistics);
        }

        private int GetPlayerPlacement(RoboClashMatchState result)
        {
            if (result == RoboClashMatchState.Draw)
                return 0;
            if (result == RoboClashMatchState.Victory)
                return 1;
            if (settings.gameMode == RoboClashGameMode.FreeForAll)
                return GetPlayerPlacementByVitals();

            var playerParticipant = FindParticipant(player);
            if (playerParticipant == null)
                return 0;
            if (settings.gameMode == RoboClashGameMode.KingOfTheArena ||
                settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
                IsKillTargetMode())
            {
                var playerScore = settings.gameMode == RoboClashGameMode.KingOfTheArena
                    ? playerParticipant.KingScore
                    : playerParticipant.Kills;
                var placement = 1;
                foreach (var participant in participants)
                {
                    if (participant == playerParticipant)
                        continue;
                    var opponentScore = settings.gameMode == RoboClashGameMode.KingOfTheArena
                        ? participant.KingScore
                        : participant.Kills;
                    if (opponentScore > playerScore)
                        placement++;
                }
                return placement;
            }

            return 2;
        }

        private int GetPlayerPlacementByVitals()
        {
            if (player == null)
                return 0;

            var playerVitals = player.Health + player.Shield;
            var placement = 1;
            foreach (var participant in participants)
            {
                if (participant.IsPlayer || !IsLiving(participant))
                    continue;
                var opponentVitals = participant.Damageable.Health + participant.Damageable.Shield;
                if (opponentVitals > playerVitals)
                    placement++;
            }

            return placement;
        }

        private void SetParticipantInputLock(bool locked)
        {
            foreach (var participant in participants)
            {
                var movement = participant.Controller.GetComponent<RoboClashRobotMovement>();
                if (movement != null)
                    movement.SetMatchInputLocked(locked);
            }
        }

        private void SetParticipantSimulationPaused(bool paused)
        {
            foreach (var participant in participants)
            {
                var ai = participant.Controller.GetComponent<RoboClashRobotAI>();
                var abilities = participant.Controller.GetComponent<RoboClashRobotAbilities>();
                var combat = participant.Controller.GetComponent<RoboClashRobotCombat>();
                if (paused)
                {
                    ai.enabled = false;
                    abilities.enabled = false;
                    combat.enabled = false;
                }
                else
                {
                    ai.enabled = participant.AIEnabled;
                    abilities.enabled = participant.AbilitiesEnabled;
                    combat.enabled = participant.CombatEnabled;
                }
            }
        }

        private void SetState(RoboClashMatchState nextState)
        {
            if (State == nextState)
                return;

            if (!IsTransitionAllowed(State, nextState))
            {
                Debug.LogError($"Invalid RoboClash match state transition: {State} -> {nextState}.", this);
                return;
            }

            State = nextState;
            MatchStateChanged?.Invoke(State);
        }

        private static bool IsTransitionAllowed(RoboClashMatchState current, RoboClashMatchState next)
        {
            if (next == RoboClashMatchState.Initializing)
                return current != RoboClashMatchState.Initializing;

            switch (current)
            {
                case RoboClashMatchState.Initializing:
                    return next == RoboClashMatchState.Ready;
                case RoboClashMatchState.Ready:
                    return next == RoboClashMatchState.Countdown;
                case RoboClashMatchState.Countdown:
                    return next == RoboClashMatchState.Playing;
                case RoboClashMatchState.Playing:
                    return next == RoboClashMatchState.Paused ||
                           next == RoboClashMatchState.Victory ||
                           next == RoboClashMatchState.Defeat ||
                           next == RoboClashMatchState.Draw;
                case RoboClashMatchState.Paused:
                    return next == RoboClashMatchState.Playing;
                default:
                    return false;
            }
        }

        private int CountAliveOpponents()
        {
            var count = 0;
            foreach (var participant in participants)
            {
                if (!participant.IsPlayer && IsAlive(participant))
                    count++;
            }

            return count;
        }

        private int CountAliveCombatants()
        {
            var count = 0;
            foreach (var participant in participants)
            {
                if (IsAlive(participant))
                    count++;
            }

            return count;
        }

        private int CountLivingOpponents()
        {
            var count = 0;
            foreach (var participant in participants)
            {
                if (!participant.IsPlayer && IsLiving(participant))
                    count++;
            }

            return count;
        }

        private bool IsPlayerAlive()
        {
            var participant = FindParticipant(player);
            return participant != null && IsAlive(participant);
        }

        private static bool IsAlive(Participant participant)
        {
            return participant != null && participant.Damageable != null &&
                   (!participant.Damageable.IsDead || participant.Controller.IsRespawning);
        }

        private static bool IsLiving(Participant participant)
        {
            return participant != null && participant.Damageable != null && !participant.Damageable.IsDead;
        }

        private bool IsParticipant(ArenaDamageable damageable)
        {
            return FindParticipant(damageable) != null;
        }

        private Participant FindParticipant(ArenaDamageable damageable)
        {
            foreach (var participant in participants)
            {
                if (participant.Damageable == damageable)
                    return participant;
            }

            return null;
        }

        private Participant FindParticipant(RobotArenaController controller)
        {
            foreach (var participant in participants)
            {
                if (participant.Controller == controller)
                    return participant;
            }

            return null;
        }

        private Participant GetParticipant(ArenaDamageable damageable)
        {
            return damageable != null ? FindParticipant(damageable) : null;
        }

        private string GetKingControllerName()
        {
            if (kingController == null)
                return "NONE";
            var definition = kingController.GetComponent<RoboClashRobotDefinition>();
            return definition != null ? definition.robotType.ToString().ToUpperInvariant() : "UNKNOWN";
        }

        private RoboClashRobotType[] GetOpponentRobotTypes(int count)
        {
            var types = new List<RoboClashRobotType>();
            foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
            {
                if (settings.gameMode == RoboClashGameMode.Survival || type != settings.playerRobotType)
                    types.Add(type);
            }

            var random = settings.useRandomSeed
                ? new System.Random(settings.randomSeed)
                : new System.Random();
            for (var i = types.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                (types[i], types[swapIndex]) = (types[swapIndex], types[i]);
            }

            var result = new RoboClashRobotType[count];
            for (var i = 0; i < count; i++)
                result[i] = types[i % types.Count];
            return result;
        }

        private Camera FindMainCamera()
        {
            Camera found = null;
            var count = 0;
            foreach (var candidate in FindObjectsByType<Camera>())
            {
                if (candidate.gameObject.scene != gameObject.scene ||
                    !candidate.isActiveAndEnabled ||
                    !candidate.CompareTag("MainCamera"))
                    continue;

                found = candidate;
                count++;
            }

            return count == 1 ? found : null;
        }

        private void DetachCameraFromRobot()
        {
            var cameraDamageable = playerCamera.GetComponentInParent<ArenaDamageable>();
            if (cameraDamageable != null && !cameraWasDetached)
            {
                originalCameraParent = playerCamera.transform.parent;
                originalCameraLocalPosition = playerCamera.transform.localPosition;
                originalCameraLocalRotation = playerCamera.transform.localRotation;
                originalCameraLocalScale = playerCamera.transform.localScale;
                cameraWasDetached = true;
                playerCamera.transform.SetParent(null, true);
            }
        }

        private void RestoreCameraParent()
        {
            if (!cameraWasDetached || playerCamera == null || originalCameraParent == null)
                return;

            playerCamera.transform.SetParent(originalCameraParent, false);
            playerCamera.transform.localPosition = originalCameraLocalPosition;
            playerCamera.transform.localRotation = originalCameraLocalRotation;
            playerCamera.transform.localScale = originalCameraLocalScale;
        }

        private void RestoreCursorState()
        {
            Cursor.lockState = cursorLockBeforePause;
            Cursor.visible = cursorVisibleBeforePause;
        }

        private ArenaSpawnZone[] FindSpawnZones()
        {
            var found = new List<ArenaSpawnZone>();
            foreach (var zone in FindObjectsByType<ArenaSpawnZone>())
            {
                if (zone.gameObject.scene == gameObject.scene)
                    found.Add(zone);
            }
            found.Sort((left, right) => left.SpawnIndex.CompareTo(right.SpawnIndex));
            return found.ToArray();
        }

        private bool HasPlayableSurface(ArenaSpawnZone[] zones)
        {
            if (zones.Length > 0)
                return true;

            foreach (var floor in FindObjectsByType<ArenaFloor>())
            {
                if (floor.gameObject.scene == gameObject.scene && floor.GetComponent<Collider>() != null)
                    return true;
            }

            return false;
        }

        private Vector3[] GetSpawnPositions(ArenaSpawnZone[] zones, RoboClashRobotType[] opponentTypes)
        {
            var count = opponentTypes.Length + 1;
            var positions = new Vector3[count];
            var types = new List<RoboClashRobotType> { settings.playerRobotType };
            types.AddRange(opponentTypes);

            for (var i = 0; i < count; i++)
            {
                var type = types[i];
                var zone = i < zones.Length ? zones[i] : null;
                Vector3 position;
                if (i < zones.Length || (zones.Length == 0 && i < FallbackSpawns.Length))
                {
                    position = zone != null
                        ? zone.transform.position + Vector3.up * 0.35f
                        : FallbackSpawns[i] + Vector3.up * 0.35f;
                    if (!IsSafeSpawn(position, type) ||
                        OverlapsReservedSpawn(positions, i, position, type, types))
                        return null;
                }
                else if (!TryFindReservedSpawn(type, zones, positions, i, types, out position))
                    return null;

                positions[i] = position;
            }

            return positions;
        }

        private bool TryFindReservedSpawn(RoboClashRobotType type, ArenaSpawnZone[] zones,
            Vector3[] reserved, int reservedCount, List<RoboClashRobotType> types, out Vector3 position)
        {
            position = Vector3.zero;
            var spawnPointCount = zones.Length > 0 ? zones.Length : FallbackSpawns.Length;
            for (var spawnIndex = 0; spawnIndex < spawnPointCount; spawnIndex++)
            {
                var anchor = zones.Length > 0
                    ? zones[spawnIndex].transform.position
                    : FallbackSpawns[spawnIndex];
                for (var ring = 0; ring < 3; ring++)
                {
                    var radius = 2.6f + ring * 1.7f;
                    for (var point = 0; point < 8; point++)
                    {
                        var angle = point * 45f * Mathf.Deg2Rad;
                        var candidate = anchor + new Vector3(Mathf.Cos(angle) * radius, 0.35f,
                            Mathf.Sin(angle) * radius);
                        if (!IsSafeSpawn(candidate, type) ||
                            OverlapsReservedSpawn(reserved, reservedCount, candidate, type, types))
                            continue;
                        position = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsSafeSpawn(Vector3 position, RoboClashRobotType type)
        {
            var dimensions = RoboClashRobotDefinition.GetTargetDimensions(type);
            var radius = Mathf.Max(0.25f, dimensions.x * 0.38f);
            var height = Mathf.Max(dimensions.y, radius * 2f);

            var foundFloor = false;
            var hasArenaFloor = false;
            foreach (var floor in FindObjectsByType<ArenaFloor>())
            {
                if (floor.gameObject.scene != gameObject.scene)
                    continue;
                var collider = floor.GetComponent<Collider>();
                if (collider == null)
                    continue;

                hasArenaFloor = true;
                var bounds = collider.bounds;
                if (position.x >= bounds.min.x + radius && position.x <= bounds.max.x - radius &&
                    position.z >= bounds.min.z + radius && position.z <= bounds.max.z - radius)
                {
                    foundFloor = true;
                    break;
                }
            }

            if (hasArenaFloor && !foundFloor)
                return false;

            var hits = Physics.OverlapCapsule(position + Vector3.up * radius,
                position + Vector3.up * (height - radius), radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                var hazard = hit.GetComponentInParent<ArenaHazard>();
                if (hazard != null)
                    return false;
                if (hit.isTrigger || hit.GetComponentInParent<ArenaFloor>() != null ||
                    hit.GetComponentInParent<ArenaDamageable>() != null)
                    continue;

                return false;
            }

            if (!hasArenaFloor && FindSpawnZones().Length == 0)
                return false;

            foreach (var groundHit in Physics.RaycastAll(position + Vector3.up * 2f,
                         Vector3.down, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (groundHit.normal.y < 0.5f ||
                    groundHit.collider.GetComponentInParent<ArenaHazard>() != null ||
                    groundHit.collider.GetComponentInParent<ArenaDamageable>() != null ||
                    groundHit.collider.gameObject.scene != gameObject.scene ||
                    position.y - groundHit.point.y > 1.5f)
                    continue;

                return true;
            }

            return false;
        }

        private static bool OverlapsReservedSpawn(Vector3[] reserved, int reservedCount, Vector3 candidate,
            RoboClashRobotType candidateType, List<RoboClashRobotType> types)
        {
            for (var i = 0; i < reservedCount; i++)
            {
                var candidateRadius = RoboClashRobotDefinition.GetTargetDimensions(candidateType).x * 0.5f;
                var otherRadius = RoboClashRobotDefinition.GetTargetDimensions(types[i]).x * 0.5f;
                var minimumDistance = candidateRadius + otherRadius + 1f;
                if ((reserved[i] - candidate).sqrMagnitude < minimumDistance * minimumDistance)
                    return true;
            }

            return false;
        }

        private static ArenaSpawnZone FindMatchingZone(ArenaSpawnZone[] zones, int participantIndex)
        {
            return zones.Length > participantIndex ? zones[participantIndex] : null;
        }

        private static Quaternion GetSpawnRotation(Vector3 position, ArenaSpawnZone zone)
        {
            if (zone != null)
                return zone.transform.rotation;

            var direction = Vector3.ProjectOnPlane(-position, Vector3.up);
            return direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
        }

        private void DeactivateExistingRobotInstances()
        {
            foreach (var definition in FindObjectsByType<RoboClashRobotDefinition>())
            {
                if (definition.gameObject.scene == gameObject.scene &&
                    !definition.transform.IsChildOf(participantRoot))
                    DeactivateSceneObject(definition.gameObject);
            }

            foreach (var damageable in FindObjectsByType<ArenaDamageable>())
            {
                if (damageable.gameObject.scene != gameObject.scene ||
                    damageable.transform.IsChildOf(participantRoot))
                    continue;

                var isGeneratedRobot = damageable.GetComponent<RoboClashRobotDefinition>() != null;
                var isTrainingBot = damageable.GetComponent<ArenaBotController>() != null;
                var isLegacyPlayer = damageable.GetComponent<RobotArenaController>() != null &&
                                    damageable.GetComponent<RoboClashRobotMovement>() == null;
                if (!isGeneratedRobot && !isTrainingBot && !isLegacyPlayer)
                    continue;

                DeactivateSceneObject(damageable.gameObject);
            }
        }

        private void DeactivateSceneObject(GameObject target)
        {
            if (target == null)
                return;

            if (!priorObjectStates.ContainsKey(target))
                priorObjectStates.Add(target, target.activeSelf);
            target.SetActive(false);
        }

        private void UnsubscribeParticipants()
        {
            foreach (var participant in participants)
            {
                if (participant.Damageable != null)
                {
                    participant.Damageable.DamageApplied -= OnParticipantDamageApplied;
                    participant.Damageable.Eliminated -= OnParticipantEliminated;
                }
                if (participant.Controller != null)
                    participant.Controller.Respawned -= OnParticipantRespawned;
            }
        }

        private void RestorePreviousSceneObjects()
        {
            foreach (var item in priorObjectStates)
            {
                if (item.Key != null)
                    item.Key.SetActive(item.Value);
            }
            priorObjectStates.Clear();
        }

        private void CancelParticipantProjectiles()
        {
            foreach (var projectile in FindObjectsByType<ArenaProjectile>())
            {
                if (projectile == null || projectile.gameObject.scene != gameObject.scene)
                    continue;

                foreach (var participant in participants)
                {
                    if (projectile.IsOwnedBy(participant.Controller.gameObject))
                    {
                        projectile.Cancel();
                        break;
                    }
                }
            }
        }

        private void CancelProjectilesOwnedBy(GameObject owner)
        {
            foreach (var projectile in FindObjectsByType<ArenaProjectile>())
            {
                if (projectile != null && projectile.gameObject.scene == gameObject.scene &&
                    projectile.IsOwnedBy(owner))
                    projectile.Cancel();
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashPulseRing.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    public sealed class RoboClashPulseRing : MonoBehaviour
    {
        private const int Segments = 48;
        private LineRenderer line;
        private Color tint;
        private float maximumRadius;
        private float duration;
        private float elapsed;

        public static void Create(Vector3 position, Color color, float radius, float effectDuration)
        {
            var ringObject = new GameObject("RoboClash Pulse Ring");
            ringObject.hideFlags = HideFlags.DontSave;
            ringObject.transform.position = position;
            var ring = ringObject.AddComponent<RoboClashPulseRing>();
            ring.Initialize(color, radius, effectDuration);
        }

        private void Initialize(Color color, float radius, float effectDuration)
        {
            tint = color;
            maximumRadius = Mathf.Max(0.5f, radius);
            duration = Mathf.Max(0.1f, effectDuration);
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Segments;
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.sharedMaterial = RoboClashCombatVFX.GetParticleMaterial();
            line.startColor = color;
            line.endColor = color;

            for (var i = 0; i < Segments; i++)
            {
                var angle = i * Mathf.PI * 2f / Segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.1f, maximumRadius, progress);
            var fadedColor = tint;
            fadedColor.a = 1f - progress;
            line.startColor = fadedColor;
            line.endColor = fadedColor;
            line.startWidth = line.endWidth = Mathf.Lerp(0.11f, 0.015f, progress);

            if (progress >= 1f)
                Destroy(gameObject);
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs`

````csharp
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboClash
{
    [RequireComponent(typeof(RoboClashRobotDefinition), typeof(RoboClashRobotMovement))]
    public sealed class RoboClashRobotAbilities : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float abilityCooldown = 12f;
        [SerializeField, Min(0f)] private float ultimateCooldown = 45f;
        [SerializeField, Min(0f)] private float ultimateChargeRequired = 100f;
        [SerializeField, Min(0f)] private float supportRange = 8f;
        [SerializeField, Min(0f)] private float supportShieldPerSecond = 1.5f;

        private RoboClashRobotDefinition definition;
        private RoboClashRobotMovement movement;
        private RoboClashRobotCombat combat;
        private RoboClashRobotAnimator animator;
        private RoboClashRobotVFX visualEffects;
        private RoboClashRobotAudio audioPlayer;
        private ArenaDamageable damageable;
        private float nextAbilityTime;
        private float nextUltimateTime;
        private float abilityActiveUntil;
        private float ultimateActiveUntil;
        private float ultimateCharge;
        private float temporaryDamageMultiplier = 1f;
        private float overdriveEndTime;
        private float empLockEndTime;
        private float empSlowEndTime;
        private float empSlowMultiplier = 1f;
        private ArenaDamageable preferredAITarget;
        private float nextSupportPulseTime;
        private const float SupportPulseInterval = 0.25f;
        private bool isCloaked;
        private Coroutine abilityEffect;
        private Coroutine ultimateEffect;
        private Renderer[] cloakedRenderers;
        private bool[] rendererStates;
        private Renderer cloakVisual;

        public bool IsCloaked => isCloaked;
        public bool IsAbilityLocked { get; private set; }
        public float UltimateCharge => ultimateCharge;
        public float UltimateChargeRequired => ultimateChargeRequired;
        public float AbilityCooldownRemaining => Mathf.Max(0f, nextAbilityTime - Time.time);
        public float UltimateCooldownRemaining => Mathf.Max(0f, nextUltimateTime - Time.time);
        public float AbilityActiveRemaining => Mathf.Max(0f, abilityActiveUntil - Time.time);
        public float UltimateActiveRemaining => Mathf.Max(0f, ultimateActiveUntil - Time.time);
        public bool IsUltimateActive => UltimateActiveRemaining > 0f;
        public bool IsAbilityReady => AbilityCooldownRemaining <= 0f && !IsAbilityLocked &&
                                      damageable != null && !damageable.IsDead;
        public bool IsUltimateReady => UltimateCooldownRemaining <= 0f &&
                                       (ultimateChargeRequired <= 0f || ultimateCharge >= ultimateChargeRequired) &&
                                       !IsAbilityLocked && damageable != null && !damageable.IsDead;
        public bool IsAbilityActive => Time.time < abilityActiveUntil;

        public event Action<RoboClashRobotAbilities> StateChanged;
        public event Action AbilityActivated;
        public event Action UltimateActivated;

        private void Awake()
        {
            definition = GetComponent<RoboClashRobotDefinition>();
            movement = GetComponent<RoboClashRobotMovement>();
            combat = GetComponent<RoboClashRobotCombat>();
            animator = GetComponent<RoboClashRobotAnimator>();
            visualEffects = GetComponent<RoboClashRobotVFX>();
            audioPlayer = GetComponent<RoboClashRobotAudio>();
            damageable = GetComponent<ArenaDamageable>();
            abilityCooldown = definition.abilityCooldown;
            ultimateCooldown = definition.ultimateCooldown;
            if (definition.modelRoot != null)
            {
                cloakedRenderers = definition.modelRoot.GetComponentsInChildren<Renderer>(true);
                foreach (var item in cloakedRenderers)
                {
                    if (item.gameObject.name == "CloakVisual")
                        cloakVisual = item;
                }
                rendererStates = new bool[cloakedRenderers.Length];
            }
        }

        private void Update()
        {
            if (damageable == null || damageable.IsDead)
                return;

            if (definition.robotType == RoboClashRobotType.Spark && Time.time >= nextSupportPulseTime)
            {
                SupportAura();
                nextSupportPulseTime = Time.time + SupportPulseInterval;
            }

            movement.ControlsLocked = Time.time < empLockEndTime;
            IsAbilityLocked = movement.ControlsLocked;
            if (!movement.PlayerControlled || Keyboard.current == null || movement.ControlsLocked)
                return;

            if (Keyboard.current.qKey.wasPressedThisFrame)
                ActivateAbility();
            if (Keyboard.current.eKey.wasPressedThisFrame)
                ActivateUltimate();
        }

        private void LateUpdate()
        {
            if (damageable == null || damageable.IsDead)
                return;

            if (isCloaked && combat != null && combat.IsFiring)
                SetCloaked(false);

            var drone = definition.repairDrone;
            if (drone != null)
            {
                var hover = Mathf.Sin(Time.time * 2.6f) * 0.12f;
                drone.localPosition = new Vector3(0.9f, 2.05f + hover, -0.55f);
                drone.Rotate(0f, 45f * Time.deltaTime, 0f, Space.Self);
            }
        }

        public void ActivateAbility()
        {
            if (Time.time < nextAbilityTime || damageable.IsDead || movement.ControlsLocked)
                return;

            if (definition.robotType == RoboClashRobotType.Volt)
            {
                var dashDirection = movement.PlayerControlled && movement.PlayerCamera != null
                    ? movement.PlayerCamera.transform.forward
                    : transform.forward;
                var dashStart = transform.position;
                if (!movement.TryDash(dashDirection, 5f, out var dashEnd))
                    return;
                visualEffects?.PlayDash(dashStart, dashEnd, dashDirection);
                animator?.TriggerAbilityPose(0.35f);
                nextAbilityTime = Time.time + abilityCooldown;
                abilityActiveUntil = Time.time + definition.abilityDuration;
                StateChanged?.Invoke(this);
                AbilityActivated?.Invoke();
                return;
            }

            nextAbilityTime = Time.time + abilityCooldown;
            abilityActiveUntil = Time.time + definition.abilityDuration;
            visualEffects?.PlayAbility();
            animator?.TriggerAbilityPose(definition.abilityDuration);
            switch (definition.robotType)
            {
                case RoboClashRobotType.Titan:
                    StartAbilityEffect(DefensiveField(definition.abilityDuration, 0.55f));
                    break;
                case RoboClashRobotType.Shadow:
                    StartAbilityEffect(Cloak(definition.abilityDuration));
                    break;
                case RoboClashRobotType.Spark:
                    StartAbilityEffect(RepairDronePulse(definition.abilityDuration));
                    break;
            }
            StateChanged?.Invoke(this);
            AbilityActivated?.Invoke();
        }

        public void ActivateUltimate()
        {
            if (Time.time < nextUltimateTime || damageable.IsDead || movement.ControlsLocked ||
                (ultimateChargeRequired > 0f && ultimateCharge < ultimateChargeRequired))
                return;

            if (ultimateChargeRequired > 0f)
                ultimateCharge = 0f;
            nextUltimateTime = Time.time + ultimateCooldown;
            ultimateActiveUntil = Time.time + Mathf.Max(0f, definition.ultimateDuration);
            StateChanged?.Invoke(this);
            UltimateActivated?.Invoke();
            visualEffects?.PlayUltimate();
            animator?.TriggerAbilityPose(0.8f);

            switch (definition.robotType)
            {
                case RoboClashRobotType.Volt:
                    StartUltimateEffect(Overdrive(definition.ultimateDuration));
                    break;
                case RoboClashRobotType.Titan:
                    StartUltimateEffect(RailCannonCharge());
                    break;
                case RoboClashRobotType.Shadow:
                    ShadowStrike();
                    break;
                case RoboClashRobotType.Spark:
                    EmpBlast();
                    break;
            }
        }

        public void AddUltimateCharge(float amount)
        {
            if (amount > 0f)
            {
                var previousCharge = ultimateCharge;
                ultimateCharge = Mathf.Min(ultimateChargeRequired, ultimateCharge + amount);
                if (!Mathf.Approximately(previousCharge, ultimateCharge))
                    StateChanged?.Invoke(this);
            }
        }

        public void SetAITarget(ArenaDamageable target)
        {
            preferredAITarget = target != null && !target.IsDead &&
                                target.GetComponent<RoboClashRobotDefinition>() is RoboClashRobotDefinition targetDefinition &&
                                targetDefinition.teamId != definition.teamId
                ? target
                : null;
        }

        public float GetDamageMultiplier()
        {
            var fieldMultiplier = definition.robotType == RoboClashRobotType.Titan
                ? temporaryDamageMultiplier
                : 1f;
            return Mathf.Clamp(fieldMultiplier, 0.45f, 1f);
        }

        public float GetMoveSpeedMultiplier()
        {
            var overdrive = definition.robotType == RoboClashRobotType.Volt && Time.time < overdriveEndTime ? 1.3f : 1f;
            var empSlow = Time.time < empSlowEndTime ? empSlowMultiplier : 1f;
            return overdrive * empSlow;
        }

        public float GetFireRateMultiplier()
        {
            return definition.robotType == RoboClashRobotType.Volt && Time.time < overdriveEndTime ? 1.35f : 1f;
        }

        public void ApplyEmp(float duration, float slowMultiplier)
        {
            empLockEndTime = Mathf.Max(empLockEndTime, Time.time + Mathf.Min(duration, 1.25f));
            empSlowEndTime = Mathf.Max(empSlowEndTime, Time.time + duration);
            empSlowMultiplier = Mathf.Clamp(slowMultiplier, 0.2f, 1f);
        }

        public void CancelAll()
        {
            StopActiveEffect();
            temporaryDamageMultiplier = 1f;
            abilityActiveUntil = 0f;
            ultimateActiveUntil = 0f;
            overdriveEndTime = 0f;
            empLockEndTime = 0f;
            empSlowEndTime = 0f;
            IsAbilityLocked = false;
            if (movement != null)
                movement.ControlsLocked = false;
            empSlowMultiplier = 1f;
            visualEffects?.StopAllEffects();
        }

        private IEnumerator DefensiveField(float duration, float damageMultiplier)
        {
            temporaryDamageMultiplier = Mathf.Clamp(damageMultiplier, 0.2f, 1f);
            visualEffects?.SetShieldField(true);
            yield return new WaitForSeconds(duration);
            temporaryDamageMultiplier = 1f;
            visualEffects?.SetShieldField(false);
            abilityEffect = null;
        }

        private IEnumerator Cloak(float duration)
        {
            SetCloaked(true);
            yield return new WaitForSeconds(duration);
            SetCloaked(false);
            abilityEffect = null;
        }

        private IEnumerator Overdrive(float duration)
        {
            overdriveEndTime = Time.time + duration;
            visualEffects?.SetOverdrive(true);
            yield return new WaitForSeconds(duration);
            overdriveEndTime = 0f;
            visualEffects?.SetOverdrive(false);
            ultimateEffect = null;
        }

        private IEnumerator RepairDronePulse(float duration)
        {
            visualEffects?.SetRepairTarget(transform);
            var endTime = Time.time + duration;
            while (Time.time < endTime && !damageable.IsDead)
            {
                damageable.Heal(4f * Time.deltaTime);
                damageable.AddShield(5f * Time.deltaTime);
                yield return null;
            }

            visualEffects?.SetRepairTarget(null);
            abilityEffect = null;
        }

        private System.Collections.IEnumerator RailCannonCharge()
        {
            animator?.TriggerAbilityPose(0.9f);
            var chargePoint = definition.weaponMount != null
                ? definition.weaponMount.position
                : transform.position + Vector3.up * 1.4f;
            var chargeDirection = definition.weaponMount != null ? definition.weaponMount.forward : transform.forward;
            RoboClashCombatVFX.PlayBurst(chargePoint, chargeDirection, definition.emissionColor, 0.6f, true);
            yield return new WaitForSeconds(0.7f);
            if (!damageable.IsDead)
                FireRailCannon();
            ultimateEffect = null;
        }

        private void FireRailCannon()
        {
            if (definition.muzzle == null)
                return;

            var forward = movement.PlayerCamera != null
                ? movement.PlayerCamera.transform.forward
                : transform.forward;
            FireAbilityProjectile(forward, 65f, 90f, definition.damageRadius, 12f);
        }

        private void ShadowStrike()
        {
            if (abilityEffect != null)
            {
                StopCoroutine(abilityEffect);
                abilityEffect = null;
            }
            SetCloaked(false);

            RoboClashRobotDefinition target = preferredAITarget != null &&
                                              preferredAITarget.isActiveAndEnabled &&
                                              !preferredAITarget.IsDead
                ? preferredAITarget.GetComponent<RoboClashRobotDefinition>()
                : null;
            var nearestDistance = float.PositiveInfinity;
            if (target == null)
            {
                foreach (var candidate in FindObjectsByType<RoboClashRobotDefinition>())
                {
                    var candidateDamageable = candidate.GetComponent<ArenaDamageable>();
                    if (candidate == definition || candidate.teamId == definition.teamId ||
                        !candidate.isActiveAndEnabled || candidateDamageable == null ||
                        candidateDamageable.IsDead)
                        continue;
                    var candidateDistance = Vector3.Distance(transform.position, candidate.transform.position);
                    if (candidateDistance < nearestDistance)
                    {
                        target = candidate;
                        nearestDistance = candidateDistance;
                    }
                }
            }

            var destination = transform.position + transform.forward * 5f;
            if (target != null)
            {
                var behindTarget = target.transform.position - target.transform.forward * 2f;
                var direction = behindTarget - transform.position;
                var distance = direction.magnitude;
                var blocked = false;
                if (distance > 0.1f)
                {
                    foreach (var hit in Physics.SphereCastAll(transform.position + Vector3.up * 0.8f,
                                 0.35f, direction.normalized, distance, Physics.DefaultRaycastLayers,
                                 QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.root == transform.root ||
                            hit.collider.GetComponentInParent<ArenaDamageable>() != null)
                            continue;
                        blocked = true;
                        break;
                    }

                    if (!blocked)
                        destination = behindTarget;
                }
            }

            var previousPosition = transform.position;
            if (movement.TryTeleport(destination))
            {
                transform.rotation = Quaternion.LookRotation(target != null
                    ? target.transform.position - transform.position
                    : transform.forward, Vector3.up);
                visualEffects?.PlayDash(previousPosition, transform.position, transform.forward);
            }

            var aim = target != null
                ? (target.transform.position + Vector3.up - definition.muzzle.position).normalized
                : transform.forward;
            FireAbilityProjectile(aim, 48f, 70f, 0f, 14f);
        }

        private void EmpBlast()
        {
            const float radius = 8f;
            RoboClashCombatVFX.PlayPulse(transform.position + Vector3.up,
                definition.emissionColor, radius);
            foreach (var candidate in FindObjectsByType<RoboClashRobotDefinition>())
            {
                var candidateVitals = candidate.GetComponent<ArenaDamageable>();
                if (candidate == definition || candidate.teamId == definition.teamId ||
                    !candidate.isActiveAndEnabled || candidateVitals == null || candidateVitals.IsDead ||
                    Vector3.Distance(transform.position, candidate.transform.position) > radius)
                    continue;

                candidate.GetComponent<RoboClashRobotAbilities>()?.ApplyEmp(3f, 0.55f);
                candidate.GetComponent<ArenaBotController>()?.ApplyDisruption(3f, 0.55f);
                candidate.GetComponent<RoboClashRobotVFX>()?.PlayEmpDisruption();
            }
        }

        private void FireAbilityProjectile(Vector3 direction, float speed, float damage, float radius, float knockback)
        {
            if (definition.muzzle == null)
                return;

            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = definition.ultimateName;
            projectile.transform.position = definition.muzzle.position;
            projectile.transform.rotation = Quaternion.LookRotation(direction);
            projectile.transform.localScale = Vector3.one * 0.28f;
            projectile.AddComponent<Rigidbody>();
            projectile.AddComponent<ArenaProjectile>().ConfigureVisual(definition.emissionColor, true);
            projectile.GetComponent<ArenaProjectile>().Launch(gameObject, direction, speed, damage, 3f, radius, knockback);
        }

        private void SupportAura()
        {
            damageable.AddShield(2.5f * SupportPulseInterval);
            foreach (var ally in FindObjectsByType<RoboClashRobotDefinition>())
            {
                if (ally == definition || ally.teamId != definition.teamId ||
                    Vector3.Distance(transform.position, ally.transform.position) > supportRange)
                    continue;
                ally.GetComponent<ArenaDamageable>()?.AddShield(supportShieldPerSecond * SupportPulseInterval);
            }
        }

        private void SetCloaked(bool cloaked)
        {
            if (isCloaked == cloaked)
                return;

            isCloaked = cloaked;
            if (cloakedRenderers != null)
            {
                for (var i = 0; i < cloakedRenderers.Length; i++)
                {
                    var renderer = cloakedRenderers[i];
                    if (renderer == null || renderer == cloakVisual)
                        continue;
                    if (cloaked)
                        rendererStates[i] = renderer.enabled;
                    renderer.enabled = cloaked ? false : rendererStates[i];
                }
            }

            if (cloakVisual != null)
                cloakVisual.enabled = cloaked;
        }

        private void StartAbilityEffect(IEnumerator effect)
        {
            StopAbilityEffect();
            abilityEffect = StartCoroutine(effect);
        }

        private void StartUltimateEffect(IEnumerator effect)
        {
            StopUltimateEffect();
            ultimateEffect = StartCoroutine(effect);
        }

        private void StopAbilityEffect()
        {
            if (abilityEffect != null)
                StopCoroutine(abilityEffect);
            abilityEffect = null;
            temporaryDamageMultiplier = 1f;
            visualEffects?.SetShieldField(false);
            visualEffects?.SetRepairTarget(null);
            animator?.SetAbilityPose(false);
            SetCloaked(false);
        }

        private void StopUltimateEffect()
        {
            if (ultimateEffect != null)
                StopCoroutine(ultimateEffect);
            ultimateEffect = null;
            overdriveEndTime = 0f;
            visualEffects?.SetOverdrive(false);
        }

        private void StopActiveEffect()
        {
            StopAbilityEffect();
            StopUltimateEffect();
        }

        private void OnDisable()
        {
            CancelAll();
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashRobotAI.cs`

````csharp
using System;
using UnityEngine;
using UnityEngine.AI;

namespace RoboClash
{
    public enum RoboClashAIDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    [Serializable]
    public sealed class RoboClashAIDifficultyProfile
    {
        [Min(0.05f)] public float decisionInterval = 0.25f;
        [Min(0.1f)] public float targetReevaluationInterval = 0.6f;
        [Min(4f)] public float sensingRange = 36f;
        [Min(1f)] public float aimTurnSpeed = 180f;
        [Range(0f, 20f)] public float aimErrorDegrees = 4f;
        [Range(0f, 1f)] public float aggression = 0.65f;
        [Range(0f, 1f)] public float strafeAmount = 0.55f;
        [Range(0f, 1f)] public float hazardAwareness = 0.7f;

        public RoboClashAIDifficultyProfile Clone()
        {
            return (RoboClashAIDifficultyProfile)MemberwiseClone();
        }
    }

    public enum RoboClashAIState
    {
        Idle,
        Searching,
        Approaching,
        Attacking,
        Evading,
        UsingAbility,
        Recovering,
        Dead
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoboClashRobotDefinition), typeof(RoboClashRobotMovement),
        typeof(RoboClashRobotCombat))]
    [RequireComponent(typeof(ArenaDamageable))]
    public sealed class RoboClashRobotAI : MonoBehaviour
    {
        [SerializeField] private ArenaDamageable target;
        [SerializeField] private RoboClashAIState state = RoboClashAIState.Idle;

        private RoboClashRobotMovement movement;
        private RoboClashRobotCombat combat;
        private RoboClashRobotDefinition definition;
        private ArenaDamageable ownVitals;
        private RoboClashRobotAbilities abilities;
        private RoboClashRobotAbilities targetAbilities;
        private RoboClashMatchManager matchManager;
        private CharacterController characterController;
        private RoboClashAIDifficultyProfile profile = new RoboClashAIDifficultyProfile();
        private System.Random random;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[16];
        private readonly Collider[] nearbyColliders = new Collider[24];
        private NavMeshPath navPath;
        private float nextPathTime;
        private Vector3 pathDirection;
        private bool hasPath;
        private Vector3 movementIntent;
        private Vector3 aimIntent;
        private Vector3 previousTargetPosition;
        private Vector3 targetVelocity;
        private float nextDecisionTime;
        private float nextTargetRefreshTime;
        private float nextBlockedCheckTime;
        private float lastDamageTakenTime = float.NegativeInfinity;
        private float targetDistance;
        private float preferredRange;
        private int strafeDirection = 1;
        private bool wasBlocked;

        public RoboClashAIState State => state;
        public ArenaDamageable Target => target;
        public RoboClashAIDifficulty Difficulty { get; private set; } = RoboClashAIDifficulty.Normal;

        private void Awake()
        {
            targetAbilities = target != null ? target.GetComponent<RoboClashRobotAbilities>() : null;
            movement = GetComponent<RoboClashRobotMovement>();
            combat = GetComponent<RoboClashRobotCombat>();
            definition = GetComponent<RoboClashRobotDefinition>();
            ownVitals = GetComponent<ArenaDamageable>();
            abilities = GetComponent<RoboClashRobotAbilities>();
            characterController = GetComponent<CharacterController>();
            var seed = unchecked(Environment.TickCount * 31 +
                                  Mathf.RoundToInt(transform.position.x * 1000f) +
                                  Mathf.RoundToInt(transform.position.z * 1000f));
            random = new System.Random(seed);
            preferredRange = GetPreferredRange(definition.robotType);
            ownVitals.DamageApplied += OnDamageApplied;
        }

        private void OnDestroy()
        {
            if (ownVitals != null)
                ownVitals.DamageApplied -= OnDamageApplied;
        }

        public void Configure(RoboClashMatchManager manager, RoboClashAIDifficulty difficulty,
            RoboClashAIDifficultyProfile difficultyProfile)
        {
            matchManager = manager;
            Difficulty = difficulty;
            profile = difficultyProfile != null
                ? difficultyProfile.Clone()
                : new RoboClashAIDifficultyProfile();
            SetCurrentTarget(null);
            state = RoboClashAIState.Searching;
            nextDecisionTime = Time.time + NextReactionDelay();
            nextTargetRefreshTime = 0f;
            movementIntent = Vector3.zero;
            aimIntent = transform.forward;
        }

        public void SetTarget(ArenaDamageable newTarget)
        {
            SetCurrentTarget(IsValidTarget(newTarget) ? newTarget : null);
            nextTargetRefreshTime = 0f;
            targetVelocity = Vector3.zero;
            if (target != null)
                previousTargetPosition = target.transform.position;
        }

        private void Update()
        {
            if (movement == null || combat == null || definition == null || ownVitals == null ||
                characterController == null || ownVitals.IsDead || !gameObject.activeInHierarchy)
            {
                SetState(RoboClashAIState.Dead);
                return;
            }

            if (!characterController.enabled)
                return;

            if (movement.PlayerControlled || matchManager == null ||
                matchManager.State != RoboClashMatchState.Playing)
            {
                if (matchManager != null && matchManager.State == RoboClashMatchState.Paused)
                    SetState(RoboClashAIState.Idle);
                return;
            }

            if (movement.ControlsLocked)
            {
                movement.DriveAI(Vector3.zero, aimIntent, profile.aimTurnSpeed);
                return;
            }

            if (Time.time >= nextTargetRefreshTime)
                RefreshTarget();

            if (!IsValidTarget(target))
            {
                SetState(RoboClashAIState.Searching);
                movementIntent = Vector3.zero;
                movement.DriveAI(Vector3.zero, aimIntent, profile.aimTurnSpeed);
                return;
            }

            if (Time.time >= nextDecisionTime)
            {
                Decide();
                nextDecisionTime = Time.time + NextReactionDelay();
            }

            movement.DriveAI(movementIntent, aimIntent, profile.aimTurnSpeed);
            if (state == RoboClashAIState.Approaching || state == RoboClashAIState.Attacking ||
                state == RoboClashAIState.Evading || state == RoboClashAIState.Recovering)
                TryAttack();
        }

        private void Decide()
        {
            var targetPosition = target.transform.position;
            var elapsed = Mathf.Max(Time.deltaTime, profile.decisionInterval);
            targetVelocity = (targetPosition - previousTargetPosition) / elapsed;
            previousTargetPosition = targetPosition;

            var difference = targetPosition - transform.position;
            difference.y = 0f;
            targetDistance = difference.magnitude;
            if (targetDistance < 0.05f)
                difference = transform.forward;
            var toTarget = difference.normalized;
            var healthRatio = ownVitals.MaximumHealth > 0f
                ? ownVitals.Health / ownVitals.MaximumHealth
                : 1f;
            var underThreat = Time.time - lastDamageTakenTime < 2.2f;
            var weaponRange = Mathf.Min(definition.stats.weaponRange, profile.sensingRange);
            var hasLineOfSight = HasLineOfSight(target, weaponRange);
            var desiredRange = Mathf.Min(preferredRange, weaponRange * 0.85f);
            var urgent = healthRatio < 0.27f && underThreat;

            if (TryUseTacticalAbility(healthRatio, underThreat, hasLineOfSight, toTarget))
                return;

            if (urgent || (definition.robotType == RoboClashRobotType.Shadow && underThreat &&
                           healthRatio < 0.48f))
            {
                SetState(RoboClashAIState.Evading);
                movementIntent = ChooseSafeDirection(-toTarget + Vector3.Cross(Vector3.up, toTarget) * strafeDirection);
                aimIntent = toTarget;
                TryUseUltimate(hasLineOfSight);
                return;
            }

            if (!hasLineOfSight && targetDistance <= weaponRange * 0.95f)
            {
                SetState(RoboClashAIState.Recovering);
                movementIntent = ChooseSafeDirection(GetNavDirection(toTarget) +
                    Vector3.Cross(Vector3.up, toTarget) * (strafeDirection * 0.3f));
                aimIntent = toTarget;
                return;
            }

            if (targetDistance > desiredRange * 1.12f)
            {
                SetState(RoboClashAIState.Approaching);
                movementIntent = ChooseSafeDirection(GetNavDirection(toTarget) +
                    Vector3.Cross(Vector3.up, toTarget) *
                    (profile.strafeAmount * 0.35f * strafeDirection));
                aimIntent = toTarget;
                TryUseUltimate(hasLineOfSight);
                return;
            }

            if (targetDistance < desiredRange * 0.52f)
            {
                SetState(RoboClashAIState.Evading);
                movementIntent = ChooseSafeDirection(-toTarget + Vector3.Cross(Vector3.up, toTarget) *
                    (profile.strafeAmount * strafeDirection));
                aimIntent = toTarget;
                TryUseUltimate(hasLineOfSight);
                return;
            }

            SetState(hasLineOfSight ? RoboClashAIState.Attacking : RoboClashAIState.Recovering);
            movementIntent = ChooseSafeDirection(Vector3.Cross(Vector3.up, toTarget) *
                (profile.strafeAmount * strafeDirection));
            aimIntent = toTarget;
            TryUseUltimate(hasLineOfSight);
        }

        private Vector3 GetNavDirection(Vector3 fallback)
        {
            if (Time.time >= nextPathTime)
            {
                nextPathTime = Time.time + 0.4f;
                hasPath = false;
                navPath ??= new NavMeshPath();
                if (target != null &&
                    NavMesh.SamplePosition(transform.position, out var from, 3f, NavMesh.AllAreas) &&
                    NavMesh.SamplePosition(target.transform.position, out var to, 3f, NavMesh.AllAreas) &&
                    NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, navPath) &&
                    navPath.status != NavMeshPathStatus.PathInvalid)
                {
                    var corners = navPath.corners;
                    if (corners.Length < 2)
                        return fallback;

                    var corner = corners[1];
                    if (corners.Length > 2 &&
                        (corner - transform.position).sqrMagnitude < 1f)
                        corner = corners[2];

                    var direction = corner - transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.01f)
                    {
                        pathDirection = direction.normalized;
                        hasPath = true;
                    }
                }
            }

            return hasPath ? pathDirection : fallback;
        }

        private void RefreshTarget()
        {
            nextTargetRefreshTime = Time.time + profile.targetReevaluationInterval;
            var currentTarget = IsTargetVisible(target) ? target : null;
            if (matchManager == null ||
                !matchManager.TryFindNearestOpponent(ownVitals, profile.sensingRange, out var candidate))
            {
                SetCurrentTarget(currentTarget);
                return;
            }

            if (currentTarget != null && candidate != currentTarget)
            {
                var currentDistance = (currentTarget.transform.position - transform.position).sqrMagnitude;
                var candidateDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (candidateDistance >= currentDistance * 0.72f)
                    candidate = currentTarget;
            }

            SetCurrentTarget(candidate);
            if (target != null)
            {
                previousTargetPosition = target.transform.position;
                targetVelocity = Vector3.zero;
            }
        }

        private bool TryUseTacticalAbility(float healthRatio, bool underThreat, bool hasLineOfSight,
            Vector3 toTarget)
        {
            if (abilities == null || !abilities.IsAbilityReady)
                return false;

            var useAbility = false;
            switch (definition.robotType)
            {
                case RoboClashRobotType.Volt:
                    useAbility = (targetDistance > preferredRange * 1.35f && hasLineOfSight) ||
                                 (healthRatio < 0.34f && underThreat);
                    break;
                case RoboClashRobotType.Titan:
                    useAbility = underThreat && healthRatio < 0.72f;
                    break;
                case RoboClashRobotType.Shadow:
                    useAbility = (underThreat && healthRatio < 0.56f) ||
                                 (!hasLineOfSight && targetDistance > preferredRange);
                    break;
                case RoboClashRobotType.Spark:
                    useAbility = healthRatio < 0.76f ||
                                 ownVitals.Shield < ownVitals.MaximumShield * 0.52f;
                    break;
            }

            if (!useAbility)
                return false;

            if (definition.robotType == RoboClashRobotType.Volt && healthRatio < 0.34f)
                movementIntent = ChooseSafeDirection(-toTarget + Vector3.Cross(Vector3.up, toTarget) * strafeDirection);

            SetState(RoboClashAIState.UsingAbility);
            abilities.ActivateAbility();
            return true;
        }

        private void TryUseUltimate(bool hasLineOfSight)
        {
            if (abilities == null || !abilities.IsUltimateReady)
                return;

            var useful = hasLineOfSight && targetDistance <= definition.stats.weaponRange * 0.9f;
            if (definition.robotType == RoboClashRobotType.Volt)
                useful &= targetDistance <= preferredRange * 1.4f;
            else if (definition.robotType == RoboClashRobotType.Titan)
                useful &= Vector3.Angle(transform.forward, target.transform.position - transform.position) <= 8f;
            else if (definition.robotType == RoboClashRobotType.Shadow)
                useful &= targetDistance <= profile.sensingRange * 0.8f;
            else if (definition.robotType == RoboClashRobotType.Spark)
                useful &= targetDistance <= 7.5f;

            if (!useful)
                return;

            abilities.SetAITarget(target);
            abilities.ActivateUltimate();
        }

        private void TryAttack()
        {
            if (!IsValidTarget(target) || definition.muzzle == null || combat.IsReloading ||
                combat.Ammunition <= 0 || targetDistance > definition.stats.weaponRange ||
                !HasLineOfSight(target, definition.stats.weaponRange))
                return;

            var flatToTarget = target.transform.position - transform.position;
            flatToTarget.y = 0f;
            if (flatToTarget.sqrMagnitude > 0.01f &&
                Vector3.Angle(transform.forward, flatToTarget) > 25f)
                return;

            if (targetAbilities != null && targetAbilities.IsCloaked)
                return;

            var aimPoint = target.transform.position + Vector3.up;
            var predictedPosition = aimPoint + targetVelocity * Mathf.Clamp(
                Vector3.Distance(definition.muzzle.position, aimPoint) /
                Mathf.Max(definition.projectileSpeed, 1f), 0f, 0.55f);
            var error = profile.aimErrorDegrees;
            if (error > 0.01f)
            {
                var yawError = NextRandomRange(-error, error);
                var pitchError = NextRandomRange(-error * 0.45f, error * 0.45f);
                var rotation = Quaternion.Euler(pitchError, yawError, 0f);
                predictedPosition = definition.muzzle.position +
                                    rotation * (predictedPosition - definition.muzzle.position);
            }

            combat.FireAt(predictedPosition);
        }

        private Vector3 ChooseSafeDirection(Vector3 desired)
        {
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.01f)
            {
                wasBlocked = false;
                return Vector3.zero;
            }

            desired.Normalize();
            if (Time.time >= nextBlockedCheckTime)
            {
                nextBlockedCheckTime = Time.time + profile.decisionInterval;
                var blocked = IsBlocked(desired);
                if (blocked && !wasBlocked)
                    strafeDirection = -strafeDirection;
                wasBlocked = blocked;
                if (blocked)
                {
                    var left = Quaternion.AngleAxis(55f * strafeDirection, Vector3.up) * desired;
                    var right = Quaternion.AngleAxis(-55f * strafeDirection, Vector3.up) * desired;
                    var leftSafe = IsSafeDirection(left);
                    var rightSafe = IsSafeDirection(right);
                    if (leftSafe || rightSafe)
                        desired = leftSafe && rightSafe
                            ? (strafeDirection > 0 ? left : right)
                            : leftSafe ? left : right;
                    else
                        desired = -desired;
                }
            }

            return IsSafeDirection(desired) ? desired : Vector3.zero;
        }

        private bool IsBlocked(Vector3 direction)
        {
            var origin = transform.position + Vector3.up * Mathf.Max(characterController.radius, 0.45f);
            var radius = Mathf.Max(0.2f, characterController.radius * 0.7f);
            var hitCount = Physics.SphereCastNonAlloc(origin, radius, direction, obstructionHits, 1.5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hitCount; i++)
            {
                var hit = obstructionHits[i];
                if (hit.transform.root == transform.root ||
                    hit.collider.GetComponentInParent<ArenaDamageable>() != null)
                    continue;
                return true;
            }

            return false;
        }

        private bool IsSafeDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return true;

            var lookAhead = transform.position + direction.normalized * 1.6f;
            var radius = Mathf.Max(0.25f, characterController.radius * 0.65f);
            var overlapCount = Physics.OverlapSphereNonAlloc(lookAhead + Vector3.up * 0.35f,
                radius, nearbyColliders, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < overlapCount; i++)
            {
                var collider = nearbyColliders[i];
                if (collider.transform.root == transform.root ||
                    collider.GetComponentInParent<ArenaDamageable>() != null)
                    continue;
                if (profile.hazardAwareness > 0f &&
                    collider.GetComponentInParent<ArenaHazard>() != null)
                    return false;
            }

            if (profile.hazardAwareness >= 0.35f &&
                !Physics.Raycast(lookAhead + Vector3.up * 1.5f, Vector3.down, out var groundHit,
                    4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;

            return true;
        }

        private bool HasLineOfSight(ArenaDamageable candidate, float range)
        {
            if (candidate == null || definition.muzzle == null)
                return false;

            var aimPoint = candidate.transform.position + Vector3.up;
            var difference = aimPoint - definition.muzzle.position;
            if (difference.sqrMagnitude > range * range)
                return false;

            if (!Physics.Raycast(definition.muzzle.position, difference.normalized, out var hit,
                    difference.magnitude + 0.25f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                return true;

            return hit.transform.root == candidate.transform.root;
        }

        private bool IsTargetVisible(ArenaDamageable candidate)
        {
            if (!IsValidTarget(candidate))
                return false;
            var targetAbilities = candidate.GetComponent<RoboClashRobotAbilities>();
            return (targetAbilities == null || !targetAbilities.IsCloaked) &&
                   HasLineOfSight(candidate, profile.sensingRange);
        }

        private bool IsValidTarget(ArenaDamageable candidate)
        {
            return matchManager != null && matchManager.IsValidOpponent(ownVitals, candidate);
        }

        private void SetCurrentTarget(ArenaDamageable candidate)
        {
            target = candidate;
            targetAbilities = candidate != null
                ? candidate.GetComponent<RoboClashRobotAbilities>()
                : null;
        }

        private void OnDamageApplied(ArenaDamageable _, float amount, ArenaDamageable instigator)
        {
            if (amount <= 0f || instigator == null || instigator == ownVitals)
                return;

            lastDamageTakenTime = Time.time;
            if (!IsValidTarget(target) || matchManager.IsValidOpponent(ownVitals, instigator))
            {
                SetCurrentTarget(instigator);
                targetVelocity = Vector3.zero;
                previousTargetPosition = instigator.transform.position;
                nextTargetRefreshTime = Time.time + profile.targetReevaluationInterval;
            }
        }

        private void SetState(RoboClashAIState next)
        {
            if (state == next)
                return;
            state = next;
        }

        private float NextReactionDelay()
        {
            return Mathf.Max(0.05f, profile.decisionInterval) *
                   Mathf.Lerp(1.5f, 0.72f, profile.aggression);
        }

        private float NextRandomRange(float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }

        private static float GetPreferredRange(RoboClashRobotType robotType)
        {
            switch (robotType)
            {
                case RoboClashRobotType.Titan: return 7f;
                case RoboClashRobotType.Shadow: return 6f;
                case RoboClashRobotType.Spark: return 12f;
                default: return 9f;
            }
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    public sealed class RoboClashRobotAnimator : MonoBehaviour
    {
        [Header("Animated visual references")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;

        [Header("Animation tuning")]
        [SerializeField, Min(0f)] private float walkFrequency = 7f;
        [SerializeField, Range(0f, 45f)] private float walkAngle = 24f;
        [SerializeField, Min(0f)] private float recoilRecovery = 12f;
        [SerializeField, Min(0.01f)] private float movementBlendTime = 0.12f;

        private Vector3 visualRootScale;
        private Vector3 torsoPosition;
        private Vector3 headPosition;
        private Quaternion torsoRotation;
        private Quaternion leftLegRotation;
        private Quaternion rightLegRotation;
        private Quaternion leftArmRotation;
        private Quaternion rightArmRotation;
        private float targetSpeedFraction;
        private float speedFraction;
        private float speedBlendVelocity;
        private float lateralLean;
        private float accelerationLean;
        private float recoil;
        private float hitReaction;
        private float abilityLean;
        private float abilityLeanVelocity;
        private float abilityPoseTarget;
        private float landingPulse;
        private float stateTime;
        private float spawnPoseElapsed;
        private float spawnPoseDuration;
        private float spawnPoseWeight;
        private float abilityEndTime;
        private float deadElapsed;
        private float gaitPhase;
        private bool grounded = true;
        private bool movementStateInitialized;
        private bool dead;
        private float strideMultiplier = 1f;
        private float frequencyMultiplier = 1f;
        private float bodyLeanMultiplier = 1f;
        private float recoilMultiplier = 1f;
        private float landingMultiplier = 1f;
        private float bobMultiplier = 1f;

        private void Awake()
        {
            ApplyMovementPersonality();
            CachePose();
        }

        public void SetVisualParts(Transform model, Transform body, Transform headPivot,
            Transform leftLegPivot, Transform rightLegPivot, Transform leftArmPivot, Transform rightArmPivot)
        {
            visualRoot = model;
            torso = body;
            head = headPivot;
            leftLeg = leftLegPivot;
            rightLeg = rightLegPivot;
            leftArm = leftArmPivot;
            rightArm = rightArmPivot;
            CachePose();
        }

        public void SetMovement(float speed, float maximumSpeed, bool isGrounded)
        {
            SetMovement(Vector3.zero, speed, maximumSpeed, isGrounded);
        }

        public void SetMovement(Vector3 horizontalVelocity, float speed, float maximumSpeed, bool isGrounded)
        {
            targetSpeedFraction = maximumSpeed > 0.001f
                ? Mathf.Clamp01(speed / maximumSpeed)
                : 0f;
            if (movementStateInitialized && !grounded && isGrounded && !dead)
                landingPulse = Mathf.Max(landingPulse, 1f * landingMultiplier);

            if (movementStateInitialized)
            {
                var localVelocity = transform.InverseTransformDirection(horizontalVelocity);
                lateralLean = Mathf.Clamp(localVelocity.x / Mathf.Max(maximumSpeed, 0.1f), -1f, 1f);
                var speedDelta = targetSpeedFraction - speedFraction;
                accelerationLean = Mathf.Clamp(speedDelta * 4f, -1f, 1f);
            }

            grounded = isGrounded;
            movementStateInitialized = true;
        }

        public void TriggerRecoil(float amount)
        {
            if (!dead)
                recoil = Mathf.Clamp01(recoil + Mathf.Max(0f, amount));
        }

        public void TriggerHitReaction(float intensity)
        {
            if (!dead)
                hitReaction = Mathf.Max(hitReaction, Mathf.Clamp01(intensity));
        }

        public void TriggerSpawnReady(float duration)
        {
            if (dead)
                return;

            spawnPoseElapsed = 0f;
            spawnPoseDuration = Mathf.Max(0.1f, duration);
            spawnPoseWeight = 1f;
        }

        public void SetAbilityPose(bool active)
        {
            abilityEndTime = active ? float.PositiveInfinity : 0f;
            abilityPoseTarget = active ? 1f : 0f;
        }

        public void TriggerAbilityPose(float duration)
        {
            if (dead)
                return;

            abilityEndTime = Time.time + Mathf.Max(0.1f, duration);
            abilityPoseTarget = 1f;
        }

        public void SetDead(bool value)
        {
            dead = value;
            if (value)
            {
                deadElapsed = 0f;
                targetSpeedFraction = 0f;
                recoil = 0f;
                hitReaction = 0f;
                abilityLean = 0f;
                abilityPoseTarget = 0f;
                landingPulse = 0f;
                abilityEndTime = 0f;
                spawnPoseWeight = 0f;
            }
            else
            {
                deadElapsed = 0f;
                speedFraction = 0f;
                speedBlendVelocity = 0f;
                targetSpeedFraction = 0f;
                recoil = 0f;
                hitReaction = 0f;
                abilityLean = 0f;
                abilityLeanVelocity = 0f;
                abilityPoseTarget = 0f;
                abilityEndTime = 0f;
                landingPulse = 0.7f * landingMultiplier;
                spawnPoseWeight = 0f;
                if (visualRoot != null)
                    visualRoot.localScale = visualRootScale;
            }
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
                return;

            var deltaTime = Time.deltaTime;
            stateTime += deltaTime;
            if (spawnPoseWeight > 0f)
            {
                spawnPoseElapsed += deltaTime;
                var introProgress = Mathf.Clamp01(spawnPoseElapsed / spawnPoseDuration);
                spawnPoseWeight = 1f - introProgress * introProgress *
                                  (3f - 2f * introProgress);
            }
            speedFraction = Mathf.SmoothDamp(speedFraction, dead ? 0f : targetSpeedFraction,
                ref speedBlendVelocity, movementBlendTime, Mathf.Infinity, deltaTime);

            if (Time.time >= abilityEndTime)
                abilityPoseTarget = 0f;
            abilityLean = Mathf.SmoothDamp(abilityLean, abilityPoseTarget, ref abilityLeanVelocity,
                0.12f, Mathf.Infinity, deltaTime);

            recoil = Mathf.MoveTowards(recoil, 0f, recoilRecovery * deltaTime);
            hitReaction = Mathf.MoveTowards(hitReaction, 0f, 5f * deltaTime);
            landingPulse = Mathf.MoveTowards(landingPulse, 0f, 3.5f * deltaTime);
            accelerationLean = Mathf.MoveTowards(accelerationLean, 0f, 3.5f * deltaTime);

            var frequency = walkFrequency * frequencyMultiplier *
                            Mathf.Lerp(0.55f, 1.25f, speedFraction);
            gaitPhase += deltaTime * frequency;
            var stride = Mathf.Sin(gaitPhase * Mathf.PI * 2f) * walkAngle *
                         speedFraction * strideMultiplier;
            var bob = Mathf.Sin(gaitPhase * Mathf.PI * 4f) * 0.014f *
                      speedFraction * bobMultiplier;
            var idleBreath = dead ? 0f : Mathf.Sin(stateTime * 1.7f) * 0.006f;
            var airborneLean = grounded ? 0f : 3f;
            var hitRoll = Mathf.Sin(stateTime * 18f) * hitReaction * 2.2f;

            if (leftLeg != null)
                leftLeg.localRotation = leftLegRotation * Quaternion.Euler(stride, 0f, 0f);
            if (rightLeg != null)
                rightLeg.localRotation = rightLegRotation * Quaternion.Euler(-stride, 0f, 0f);
            if (leftArm != null)
                leftArm.localRotation = leftArmRotation *
                    Quaternion.Euler(-stride * 0.34f + recoil * recoilMultiplier * 2f +
                                     hitReaction * 2f + spawnPoseWeight * 3f,
                        0f, -1.5f * speedFraction);
            if (rightArm != null)
                rightArm.localRotation = rightArmRotation *
                    Quaternion.Euler(stride * 0.2f - recoil * recoilMultiplier * 8f -
                                     hitReaction * 4f + spawnPoseWeight * 3f,
                        0f, 1.5f * speedFraction);

            if (torso != null)
            {
                torso.localPosition = torsoPosition + Vector3.up *
                    (bob + idleBreath - landingPulse * 0.04f - spawnPoseWeight * 0.055f);
                torso.localRotation = torsoRotation * Quaternion.Euler(
                    -speedFraction * 3.5f * bodyLeanMultiplier -
                    accelerationLean * 2.5f * bodyLeanMultiplier -
                    abilityLean * 10f + landingPulse * 4f + airborneLean +
                    hitReaction * 5f - spawnPoseWeight * 5f,
                    0f,
                    Mathf.Sin(stateTime * 0.8f) * 0.8f +
                    lateralLean * speedFraction * 3f * bodyLeanMultiplier + hitRoll);
            }

            if (head != null)
                head.localPosition = headPosition + Vector3.up *
                    (idleBreath * 0.45f + recoil * recoilMultiplier * 0.01f +
                     hitReaction * 0.018f);

            if (dead)
            {
                deadElapsed += deltaTime;
                var collapse = Mathf.Clamp01(deadElapsed / 0.45f);
                visualRoot.localScale = Vector3.Lerp(visualRootScale,
                    visualRootScale * 0.04f, collapse * collapse);
            }
        }

        private void ApplyMovementPersonality()
        {
            var definition = GetComponent<RoboClashRobotDefinition>();
            if (definition == null)
                return;

            switch (definition.robotType)
            {
                case RoboClashRobotType.Volt:
                    frequencyMultiplier = 1.08f;
                    strideMultiplier = 0.95f;
                    bodyLeanMultiplier = 1f;
                    recoilMultiplier = 1.05f;
                    landingMultiplier = 1f;
                    break;
                case RoboClashRobotType.Titan:
                    frequencyMultiplier = 0.78f;
                    strideMultiplier = 0.76f;
                    bodyLeanMultiplier = 0.72f;
                    recoilMultiplier = 1.35f;
                    landingMultiplier = 1.35f;
                    break;
                case RoboClashRobotType.Shadow:
                    frequencyMultiplier = 1.2f;
                    strideMultiplier = 1.05f;
                    bodyLeanMultiplier = 1.12f;
                    recoilMultiplier = 0.82f;
                    landingMultiplier = 0.82f;
                    break;
                case RoboClashRobotType.Spark:
                    frequencyMultiplier = 0.92f;
                    strideMultiplier = 0.82f;
                    bodyLeanMultiplier = 0.52f;
                    recoilMultiplier = 0.9f;
                    landingMultiplier = 0.92f;
                    bobMultiplier = 0.6f;
                    break;
            }
        }

        private void CachePose()
        {
            visualRootScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            torsoPosition = torso != null ? torso.localPosition : Vector3.zero;
            headPosition = head != null ? head.localPosition : Vector3.zero;
            torsoRotation = torso != null ? torso.localRotation : Quaternion.identity;
            leftLegRotation = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
            rightLegRotation = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
            leftArmRotation = leftArm != null ? leftArm.localRotation : Quaternion.identity;
            rightArmRotation = rightArm != null ? rightArm.localRotation : Quaternion.identity;
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashRobotAudio.cs`

````csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoboClash
{
    public enum RoboClashAudioEvent
    {
        WeaponFire,
        ReloadStart,
        ReloadComplete,
        ProjectileImpact,
        Damage,
        ShieldImpact,
        Ability,
        Ultimate,
        Death,
        Footstep,
        DroneRepair,
        Emp,
        Dash,
        ShieldActivation,
        Cloak,
        RepairPulse,
        Charge
    }

    [Serializable]
    public sealed class RoboClashAudioSlot
    {
        public RoboClashAudioEvent audioEvent;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
        [Range(0.5f, 2f)] public float pitch = 1f;
    }

    [RequireComponent(typeof(AudioSource))]
    public sealed class RoboClashRobotAudio : MonoBehaviour
    {
        private const int VoiceCount = 3;
        private const int SampleRate = 22050;
        private static readonly Dictionary<int, AudioClip> FallbackClips = new Dictionary<int, AudioClip>();
        private static readonly Dictionary<string, AudioClip> BundledClips =
            new Dictionary<string, AudioClip>();
        private static readonly HashSet<RoboClashRobotAudio> ActiveAudio = new HashSet<RoboClashRobotAudio>();

        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField, Min(1f)] private float maximumDistance = 28f;
        [SerializeField] private RoboClashRobotType robotType;
        [SerializeField] private RoboClashAudioSlot[] audioSlots =
        {
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.WeaponFire, volume = 0.38f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.ReloadStart, volume = 0.42f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.ReloadComplete, volume = 0.35f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.ProjectileImpact, volume = 0.45f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Damage, volume = 0.4f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.ShieldImpact, volume = 0.42f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Ability, volume = 0.48f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Ultimate, volume = 0.52f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Death, volume = 0.45f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Footstep, volume = 0.2f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.DroneRepair, volume = 0.18f },
            new RoboClashAudioSlot { audioEvent = RoboClashAudioEvent.Emp, volume = 0.5f }
        };

        private AudioSource[] voices;
        private float[] nextAllowedTime;
        private int nextVoice;

        private void Awake()
        {
            voices = new AudioSource[VoiceCount];
            nextAllowedTime = new float[Enum.GetValues(typeof(RoboClashAudioEvent)).Length];
            var baseSource = GetComponent<AudioSource>();
            ConfigureSource(baseSource);
            voices[0] = baseSource;

            for (var i = 1; i < voices.Length; i++)
            {
                var child = new GameObject("Audio Voice " + i);
                child.transform.SetParent(transform, false);
                voices[i] = child.AddComponent<AudioSource>();
                CopySourceSettings(baseSource, voices[i]);
            }

            ActiveAudio.Add(this);
            foreach (RoboClashAudioEvent audioEvent in Enum.GetValues(typeof(RoboClashAudioEvent)))
                GetBundledClip(robotType, audioEvent);
        }

        private void OnDestroy()
        {
            ActiveAudio.Remove(this);
        }

        public void Configure(RoboClashRobotType type)
        {
            robotType = type;
            var source = GetComponent<AudioSource>();
            if (source == null)
                return;
            ConfigureSource(source);
        }

        public void Play(RoboClashAudioEvent audioEvent, float volumeScale = 1f)
        {
            if (voices == null || voices.Length == 0)
                return;

            var index = (int)audioEvent;
            if (index < 0 || index >= nextAllowedTime.Length)
                return;

            var minimumInterval = audioEvent == RoboClashAudioEvent.WeaponFire ? 0.045f : 0.075f;
            if (Time.unscaledTime < nextAllowedTime[index])
                return;
            nextAllowedTime[index] = Time.unscaledTime + minimumInterval;

            var slot = FindSlot(audioEvent);
            var clip = slot != null && slot.clip != null
                ? slot.clip
                : GetBundledClip(robotType, audioEvent) ?? GetFallbackClip(robotType, audioEvent);
            if (clip == null)
                return;

            var source = voices[nextVoice++ % voices.Length];
            source.pitch = (slot != null ? slot.pitch : 1f) * UnityEngine.Random.Range(0.96f, 1.04f);
            source.PlayOneShot(clip,
                (slot != null ? slot.volume : GetFallbackVolume(audioEvent)) * Mathf.Clamp01(volumeScale));
        }

        public void PlayDetached(RoboClashAudioEvent audioEvent)
        {
            PlayDetachedAt(audioEvent, transform.position);
        }

        public void PlayDetachedAt(RoboClashAudioEvent audioEvent, Vector3 position)
        {
            var slot = FindSlot(audioEvent);
            var clip = slot != null && slot.clip != null
                ? slot.clip
                : GetBundledClip(robotType, audioEvent) ?? GetFallbackClip(robotType, audioEvent);
            var source = GetComponent<AudioSource>();
            if (clip == null || source == null)
                return;

            var volume = source.volume * (slot != null ? slot.volume : GetFallbackVolume(audioEvent));
            PlayClipAt(clip, position, volume);
        }

        private void PlayClipAt(AudioClip clip, Vector3 position, float volume)
        {
            var sourceObject = new GameObject("RoboClash One Shot");
            sourceObject.transform.position = position;
            var source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = maximumDistance;
            source.dopplerLevel = 0f;
            source.Play();
            UnityEngine.Object.Destroy(sourceObject, clip.length + 0.1f);
        }

        public void SetLocalPlayer(bool isLocalPlayer)
        {
            if (voices == null)
                return;

            var blend = isLocalPlayer ? 0.2f : spatialBlend;
            foreach (var voice in voices)
            {
                if (voice != null)
                    voice.spatialBlend = blend;
            }
        }

        private RoboClashAudioSlot FindSlot(RoboClashAudioEvent audioEvent)
        {
            if (audioSlots == null)
                return null;
            foreach (var slot in audioSlots)
            {
                if (slot != null && slot.audioEvent == audioEvent)
                    return slot;
            }

            return null;
        }

        private static AudioClip GetBundledClip(RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            var path = GetBundledClipPath(type, audioEvent);
            if (string.IsNullOrEmpty(path))
                return null;
            if (BundledClips.TryGetValue(path, out var clip))
                return clip;

            clip = Resources.Load<AudioClip>(path);
            BundledClips.Add(path, clip);
            if (clip == null)
                Debug.LogError($"RoboClash audio asset is missing at Resources/{path}.");
            return clip;
        }

        private static string GetBundledClipPath(
            RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            switch (audioEvent)
            {
                case RoboClashAudioEvent.WeaponFire:
                    switch (type)
                    {
                        case RoboClashRobotType.Titan: return "RoboClash/Audio/Weapons/GatlingCannon";
                        case RoboClashRobotType.Shadow: return "RoboClash/Audio/Weapons/ShadowSMG";
                        case RoboClashRobotType.Spark: return "RoboClash/Audio/Weapons/SparkRocket";
                        default: return "RoboClash/Audio/Weapons/PulseRifle";
                    }
                case RoboClashAudioEvent.ReloadStart:
                    return "RoboClash/Audio/Robots/ReloadStart";
                case RoboClashAudioEvent.ReloadComplete:
                    return "RoboClash/Audio/Robots/ReloadComplete";
                case RoboClashAudioEvent.ProjectileImpact:
                    return type == RoboClashRobotType.Spark
                        ? "RoboClash/Audio/Impacts/RocketExplosion"
                        : type == RoboClashRobotType.Titan
                            ? "RoboClash/Audio/Impacts/Metal"
                            : "RoboClash/Audio/Impacts/Energy";
                case RoboClashAudioEvent.Damage:
                    return "RoboClash/Audio/Robots/Damage";
                case RoboClashAudioEvent.ShieldImpact:
                    return "RoboClash/Audio/Impacts/Shield";
                case RoboClashAudioEvent.Ability:
                    return type == RoboClashRobotType.Titan
                        ? "RoboClash/Audio/Abilities/TitanShield"
                        : type == RoboClashRobotType.Shadow
                            ? "RoboClash/Audio/Abilities/ShadowCloak"
                            : type == RoboClashRobotType.Spark
                                ? "RoboClash/Audio/Abilities/SparkRepair"
                                : "RoboClash/Audio/Abilities/VoltDash";
                case RoboClashAudioEvent.Ultimate:
                case RoboClashAudioEvent.Charge:
                    return GetUltimateClipPath(type);
                case RoboClashAudioEvent.Death:
                    return "RoboClash/Audio/Impacts/LargeExplosion";
                case RoboClashAudioEvent.Footstep:
                    return "RoboClash/Audio/Robots/Footstep";
                case RoboClashAudioEvent.DroneRepair:
                    return "RoboClash/Audio/Robots/DroneRepair";
                case RoboClashAudioEvent.Emp:
                    return "RoboClash/Audio/Abilities/SparkEMP";
                case RoboClashAudioEvent.Dash:
                    return "RoboClash/Audio/Abilities/VoltDash";
                case RoboClashAudioEvent.ShieldActivation:
                    return "RoboClash/Audio/Abilities/TitanShield";
                case RoboClashAudioEvent.Cloak:
                    return "RoboClash/Audio/Abilities/ShadowCloak";
                case RoboClashAudioEvent.RepairPulse:
                    return "RoboClash/Audio/Robots/RepairPulse";
                default:
                    return null;
            }
        }

        private static string GetUltimateClipPath(RoboClashRobotType type)
        {
            switch (type)
            {
                case RoboClashRobotType.Titan: return "RoboClash/Audio/Abilities/TitanUltimate";
                case RoboClashRobotType.Shadow: return "RoboClash/Audio/Abilities/ShadowUltimate";
                case RoboClashRobotType.Spark: return "RoboClash/Audio/Abilities/SparkEMP";
                default: return "RoboClash/Audio/Abilities/VoltUltimate";
            }
        }

        private void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.volume = RoboClashSaveService.Current.settings.sfxVolume;
            source.spatialBlend = spatialBlend;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = maximumDistance;
            source.dopplerLevel = 0f;
        }

        private static void CopySourceSettings(AudioSource from, AudioSource to)
        {
            to.playOnAwake = false;
            to.loop = false;
            to.spatialBlend = from.spatialBlend;
            to.rolloffMode = from.rolloffMode;
            to.minDistance = from.minDistance;
            to.maxDistance = from.maxDistance;
            to.dopplerLevel = 0f;
            to.priority = from.priority;
            to.volume = from.volume;
        }

        public static void ApplySfxVolume(float volume)
        {
            var clampedVolume = Mathf.Clamp01(volume);
            foreach (var audio in ActiveAudio)
            {
                if (audio == null || audio.voices == null)
                    continue;
                foreach (var voice in audio.voices)
                {
                    if (voice != null)
                        voice.volume = clampedVolume;
                }
            }
        }

        public static class RoboClashAudioManager
        {
                private const string GameplayScenePath = "Assets/Scenes/RoboClashGameplay.unity";
                private static AudioSource uiSource;
                private static AudioSource ambienceSource;
                private static AudioClip selectionClip;
                private static AudioClip confirmClip;
                private static AudioClip ambienceClip;
                private static float nextSelectionTime;
                private static float nextConfirmTime;
                private static bool initialized;

                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
                private static void ResetRuntimeState()
                {
                    initialized = false;
                    uiSource = null;
                    ambienceSource = null;
                    selectionClip = null;
                    confirmClip = null;
                    ambienceClip = null;
                    nextSelectionTime = 0f;
                    nextConfirmTime = 0f;
                }

                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
                private static void Initialize()
                {
                    if (initialized)
                        return;

                    initialized = true;
                    var root = new GameObject(nameof(RoboClashAudioManager));
                    UnityEngine.Object.DontDestroyOnLoad(root);
                    uiSource = CreateSource(root.transform, "UI Audio", false);
                    ambienceSource = CreateSource(root.transform, "Arena Ambience", true);
                    selectionClip = LoadClip("RoboClash/Audio/UI/Selection");
                    confirmClip = LoadClip("RoboClash/Audio/UI/Confirm");
                    ambienceClip = LoadClip("RoboClash/Audio/Ambience/ArenaLoop");
                    ambienceSource.volume = RoboClashSaveService.Current.settings.musicVolume * 0.12f;
                    SceneManager.sceneLoaded -= OnSceneLoaded;
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    RoboClashSaveService.MusicVolumeChanged -= OnMusicVolumeChanged;
                    RoboClashSaveService.MusicVolumeChanged += OnMusicVolumeChanged;
                    OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
                }

                public static void AddButtonAudio(Button button)
                {
                    if (button == null)
                        return;

                    var trigger = button.gameObject.AddComponent<EventTrigger>();
                    trigger.triggers = new List<EventTrigger.Entry>();
                    AddSelectionTrigger(trigger, EventTriggerType.PointerEnter);
                    AddSelectionTrigger(trigger, EventTriggerType.Select);
                    button.onClick.AddListener(PlayConfirmation);
                }

                public static void PlaySelection()
                {
                    if (!initialized || Time.unscaledTime < nextSelectionTime)
                        return;

                    nextSelectionTime = Time.unscaledTime + 0.07f;
                    PlayUI(selectionClip, 0.35f);
                }

                public static void PlayConfirmation()
                {
                    if (!initialized || Time.unscaledTime < nextConfirmTime)
                        return;

                    nextConfirmTime = Time.unscaledTime + 0.1f;
                    PlayUI(confirmClip, 0.5f);
                }

                private static AudioSource CreateSource(Transform parent, string sourceName, bool loop)
                {
                    var sourceObject = new GameObject(sourceName);
                    sourceObject.transform.SetParent(parent, false);
                    var source = sourceObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = loop;
                    source.spatialBlend = 0f;
                    source.ignoreListenerPause = true;
                    return source;
                }

                private static void AddSelectionTrigger(EventTrigger trigger, EventTriggerType eventType)
                {
                    var entry = new EventTrigger.Entry { eventID = eventType };
                    entry.callback.AddListener(_ => PlaySelection());
                    trigger.triggers.Add(entry);
                }

            private static void PlayUI(AudioClip clip, float volume)
            {
                if (clip != null && uiSource != null)
                {
                    uiSource.volume = RoboClashSaveService.Current.settings.sfxVolume;
                    uiSource.PlayOneShot(clip, volume);
                }
            }

            private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (ambienceSource == null || ambienceClip == null)
                    return;

                if (string.Equals(scene.path, GameplayScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    ambienceSource.clip = ambienceClip;
                    ambienceSource.volume = RoboClashSaveService.Current.settings.musicVolume * 0.12f;
                    if (!ambienceSource.isPlaying)
                        ambienceSource.Play();
                }
                else
                {
                    ambienceSource.Stop();
                    ambienceSource.clip = null;
                }
            }

            private static void OnMusicVolumeChanged(float volume)
            {
                if (ambienceSource != null)
                    ambienceSource.volume = Mathf.Clamp01(volume) * 0.12f;
            }

            private static AudioClip LoadClip(string resourcePath)
            {
                var clip = Resources.Load<AudioClip>(resourcePath);
                if (clip == null)
                    Debug.LogError($"RoboClash audio asset is missing at Resources/{resourcePath}.");
                return clip;
            }
        }

        private static AudioClip GetFallbackClip(RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            var key = (int)type * 100 + (int)audioEvent;
            if (FallbackClips.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var duration = GetDuration(type, audioEvent);
            var sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[sampleCount];
            var seed = key * 7919 + 17;
            var baseFrequency = GetFrequency(type, audioEvent);
            var filteredNoise = 0f;
            for (var i = 0; i < sampleCount; i++)
            {
                var time = (float)i / SampleRate;
                var progress = time / duration;
                var attack = Mathf.Clamp01(time / (type == RoboClashRobotType.Titan ? 0.012f : 0.004f));
                var decayRate = audioEvent == RoboClashAudioEvent.Ultimate ||
                                audioEvent == RoboClashAudioEvent.Charge ? 2.8f : 7.5f;
                var envelope = attack * Mathf.Exp(-time * decayRate);
                var sweep = type == RoboClashRobotType.Spark
                    ? Mathf.Lerp(1.8f, 0.55f, progress)
                    : type == RoboClashRobotType.Shadow
                        ? Mathf.Lerp(1.3f, 0.82f, progress)
                        : Mathf.Lerp(0.72f, 1.15f, progress);
                var phase = 2f * Mathf.PI * baseFrequency * sweep * time;
                seed = unchecked(seed * 1103515245 + 12345);
                var noise = ((seed >> 16) & 0x7fff) / 16383.5f - 1f;
                filteredNoise = Mathf.Lerp(filteredNoise, noise, type == RoboClashRobotType.Shadow ? 0.12f : 0.48f);
                var impact = audioEvent == RoboClashAudioEvent.ProjectileImpact ||
                             audioEvent == RoboClashAudioEvent.ShieldImpact ||
                             audioEvent == RoboClashAudioEvent.Emp;
                var tone = Mathf.Sin(phase);
                float texture;
                switch (type)
                {
                    case RoboClashRobotType.Titan:
                        texture = Mathf.Clamp(tone * 2.8f + Mathf.Sin(phase * 0.47f) * 0.8f, -1f, 1f);
                        break;
                    case RoboClashRobotType.Shadow:
                        texture = tone * 0.65f + Mathf.Sin(phase * 3.1f) * 0.14f + filteredNoise * 0.2f;
                        break;
                    case RoboClashRobotType.Spark:
                        texture = tone * 0.48f + Mathf.Sin(phase * 0.5f) * 0.5f +
                                  noise * (impact ? 0.85f : 0.42f);
                        break;
                    default:
                        texture = tone * 0.72f + Mathf.Sin(phase * 2.03f) * 0.27f +
                                  noise * (impact ? 0.42f : 0.12f);
                        break;
                }
                if (type == RoboClashRobotType.Titan && audioEvent == RoboClashAudioEvent.WeaponFire)
                    texture *= 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 34f * time);
                if (audioEvent == RoboClashAudioEvent.Charge)
                    texture += Mathf.Sin(phase * 0.5f) * progress * 0.65f;
                samples[i] = Mathf.Clamp(texture * envelope * 0.3f, -0.9f, 0.9f);
            }

            var clip = AudioClip.Create("Procedural " + type + " " + audioEvent, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            FallbackClips[key] = clip;
            return clip;
        }

        private static float GetDuration(RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            switch (audioEvent)
            {
                case RoboClashAudioEvent.Ultimate: return 0.5f;
                case RoboClashAudioEvent.Charge: return type == RoboClashRobotType.Titan ? 0.8f : 0.6f;
                case RoboClashAudioEvent.Ability:
                case RoboClashAudioEvent.Emp:
                case RoboClashAudioEvent.ShieldActivation:
                case RoboClashAudioEvent.RepairPulse: return 0.32f;
                case RoboClashAudioEvent.ProjectileImpact:
                case RoboClashAudioEvent.ShieldImpact:
                case RoboClashAudioEvent.Death: return 0.34f;
                case RoboClashAudioEvent.WeaponFire:
                    return type == RoboClashRobotType.Titan ? 0.19f : 0.12f;
                default: return 0.16f;
            }
        }

        private static float GetFrequency(RoboClashRobotType type, RoboClashAudioEvent audioEvent)
        {
            var baseFrequency = type == RoboClashRobotType.Titan ? 92f :
                type == RoboClashRobotType.Shadow ? 520f :
                type == RoboClashRobotType.Spark ? 270f : 390f;
            switch (audioEvent)
            {
                case RoboClashAudioEvent.ProjectileImpact:
                case RoboClashAudioEvent.ShieldImpact: return baseFrequency * 0.55f;
                case RoboClashAudioEvent.Ability: return baseFrequency * 1.3f;
                case RoboClashAudioEvent.Ultimate:
                case RoboClashAudioEvent.Emp: return baseFrequency * 0.78f;
                case RoboClashAudioEvent.Dash: return baseFrequency * 1.7f;
                case RoboClashAudioEvent.ShieldActivation: return baseFrequency * 0.72f;
                case RoboClashAudioEvent.Cloak: return baseFrequency * 1.9f;
                case RoboClashAudioEvent.RepairPulse: return baseFrequency * 1.15f;
                case RoboClashAudioEvent.Charge: return baseFrequency * 0.62f;
                case RoboClashAudioEvent.ReloadStart:
                case RoboClashAudioEvent.ReloadComplete: return baseFrequency * 1.8f;
                default: return baseFrequency;
            }
        }

        private static float GetFallbackVolume(RoboClashAudioEvent audioEvent)
        {
            switch (audioEvent)
            {
                case RoboClashAudioEvent.Footstep: return 0.2f;
                case RoboClashAudioEvent.DroneRepair: return 0.18f;
                case RoboClashAudioEvent.WeaponFire: return 0.36f;
                default: return 0.45f;
            }
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashRobotCombat.cs`

````csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboClash
{
    [RequireComponent(typeof(RoboClashRobotDefinition), typeof(RoboClashRobotMovement))]
    public sealed class RoboClashRobotCombat : MonoBehaviour
    {
        [SerializeField] private bool playerControlled;
        [SerializeField, Min(0f)] private float knockback = 6f;
        [SerializeField, Min(0.1f)] private int maximumActiveProjectiles = 32;
        [SerializeField] private Transform recoilTransform;
        [SerializeField] private Renderer muzzleFlash;

        private RoboClashRobotDefinition definition;
        private RoboClashRobotMovement movement;
        private RoboClashRobotAnimator animator;
        private RoboClashRobotVFX visualEffects;
        private RoboClashRobotAudio audioPlayer;
        private ArenaDamageable damageable;
        private readonly HashSet<ArenaProjectile> activeProjectiles = new HashSet<ArenaProjectile>();
        private float nextShotTime;
        private float reloadReadyTime;
        private float reloadDuration;
        private float recoil;
        private Vector3 recoilRestPosition;
        private float sustainedMovement;
        private float stoppedMovement;
        private float movementReloadBonus;
        private float firingUntil;
        private float damageBuffMultiplier = 1f;
        private float damageBuffEndTime;
        private int ammunition;
        private bool firing;

        public int Ammunition => ammunition;
        public int MagazineCapacity => definition != null && definition.stats != null
            ? definition.stats.magazineSize
            : 0;
        public bool IsReloading => Time.time < reloadReadyTime;
        public float ReloadRemaining => Mathf.Max(0f, reloadReadyTime - Time.time);
        public float ReloadProgress => reloadDuration > 0f
            ? 1f - Mathf.Clamp01(ReloadRemaining / reloadDuration)
            : 1f;
        public bool IsFiring => firing;
        public float DamageBuffMultiplier => Time.time < damageBuffEndTime
            ? damageBuffMultiplier
            : 1f;

        public event System.Action<RoboClashRobotCombat> StateChanged;

        public void ApplyDamageBuff(float multiplier, float duration)
        {
            damageBuffMultiplier = Mathf.Max(1f, multiplier);
            damageBuffEndTime = Time.time + Mathf.Max(0f, duration);
            StateChanged?.Invoke(this);
        }

        private void Awake()
        {
            definition = GetComponent<RoboClashRobotDefinition>();
            movement = GetComponent<RoboClashRobotMovement>();
            animator = GetComponent<RoboClashRobotAnimator>();
            visualEffects = GetComponent<RoboClashRobotVFX>();
            audioPlayer = GetComponent<RoboClashRobotAudio>();
            damageable = GetComponent<ArenaDamageable>();
            ammunition = definition.stats.magazineSize;
            if (recoilTransform != null)
                recoilRestPosition = recoilTransform.localPosition;
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        public void ConfigurePlayer(bool enabledForPlayer)
        {
            playerControlled = enabledForPlayer;
            if (!enabledForPlayer)
                firing = false;
        }

        public void SetVisualParts(Transform weapon, Renderer flash)
        {
            recoilTransform = weapon;
            muzzleFlash = flash;
            recoilRestPosition = recoilTransform != null ? recoilTransform.localPosition : Vector3.zero;
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        public void NotifyMovement(float speed, float maximumSpeed)
        {
            if (definition.robotType != RoboClashRobotType.Volt)
                return;

            if (speed >= maximumSpeed * 0.75f)
            {
                sustainedMovement += Time.deltaTime;
                stoppedMovement = 0f;
                movementReloadBonus = Mathf.Min(0.25f, sustainedMovement / 12f);
            }
            else
            {
                stoppedMovement += Time.deltaTime;
                if (stoppedMovement >= 1.5f)
                {
                    sustainedMovement = 0f;
                    movementReloadBonus = 0f;
                }
            }
        }

        public bool Fire()
        {
            var playerCamera = movement.PlayerCamera;
            if (playerCamera == null || damageable == null || damageable.IsDead ||
                movement.ControlsLocked || Time.time < nextShotTime || IsReloading ||
                ammunition <= 0 || definition.muzzle == null ||
                activeProjectiles.Count >= maximumActiveProjectiles)
                return false;

            var aimRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var aimTarget = aimRay.origin + aimRay.direction * definition.stats.weaponRange;
            if (TryGetExternalHit(aimRay.origin, aimRay.direction,
                    definition.stats.weaponRange, out var aimHit))
                aimTarget = aimHit.point;

            return FireTowardPoint(aimTarget, aimRay.direction);
        }

        public bool FireAt(Vector3 worldPosition)
        {
            if (definition.muzzle == null)
                return false;
            return FireTowardPoint(worldPosition,
                worldPosition - definition.muzzle.position);
        }

        private bool FireTowardPoint(Vector3 aimTarget, Vector3 fallbackDirection)
        {
            if (damageable == null || damageable.IsDead || movement.ControlsLocked ||
                fallbackDirection.sqrMagnitude < 0.0001f ||
                Time.time < nextShotTime || IsReloading || ammunition <= 0 ||
                definition.muzzle == null ||
                activeProjectiles.Count >= maximumActiveProjectiles)
                return false;

            var muzzle = definition.muzzle.position;
            var direction = CalculateLaunchDirection(muzzle, aimTarget, fallbackDirection);

            if (TryGetExternalHit(muzzle, direction, definition.stats.weaponRange, out var obstruction))
                direction = CalculateLaunchDirection(muzzle, obstruction.point, direction);

            var speed = definition.projectileSpeed;
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = definition.robotType == RoboClashRobotType.Spark ? "Spark Rocket" : definition.weaponName + " Bolt";
            projectile.transform.SetPositionAndRotation(muzzle, Quaternion.LookRotation(direction));
            projectile.transform.localScale = Vector3.one * (definition.robotType == RoboClashRobotType.Spark ? 0.3f : 0.16f);
            var rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            var shot = projectile.AddComponent<ArenaProjectile>();
            shot.ConfigureVisual(definition.emissionColor, definition.robotType == RoboClashRobotType.Spark);
            shot.Launch(gameObject, direction, speed, definition.stats.damagePerShot * DamageBuffMultiplier,
                definition.stats.weaponRange / Mathf.Max(speed, 0.1f), definition.damageRadius, knockback);
            activeProjectiles.Add(shot);

            ammunition--;
            StateChanged?.Invoke(this);
            var abilities = GetComponent<RoboClashRobotAbilities>();
            var rateMultiplier = abilities != null ? abilities.GetFireRateMultiplier() : 1f;
            nextShotTime = Time.time + 1f / (definition.stats.shotsPerSecond * rateMultiplier);
            recoil = 1f;
            if (playerControlled)
                movement.AddCameraKick(
                    definition.robotType == RoboClashRobotType.Titan ? 0.25f :
                    definition.robotType == RoboClashRobotType.Spark ? 2.2f : 0.55f, 0f);
            firingUntil = Time.time + 0.1f;
            animator?.TriggerRecoil(definition.robotType == RoboClashRobotType.Titan ? 1f : 0.65f);
            audioPlayer?.Play(RoboClashAudioEvent.WeaponFire);
            visualEffects?.PlayMuzzle(muzzle, direction, definition.robotType == RoboClashRobotType.Titan);
            if (muzzleFlash != null)
            {
                muzzleFlash.enabled = true;
                CancelInvoke(nameof(HideMuzzleFlash));
                Invoke(nameof(HideMuzzleFlash), 0.06f);
            }

            if (ammunition == 0)
                Reload();
            return true;
        }

        private static Vector3 CalculateLaunchDirection(
            Vector3 muzzlePosition, Vector3 targetPoint, Vector3 fallbackForward)
        {
            var direction = targetPoint - muzzlePosition;
            if (direction.sqrMagnitude < 0.0001f)
                direction = fallbackForward;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.forward;
            return direction.normalized;
        }

        public void Reload()
        {
            if (!IsReloading && ammunition < definition.stats.magazineSize)
            {
                var duration = definition.stats.reloadDuration * (1f - movementReloadBonus);
                reloadDuration = duration;
                reloadReadyTime = Time.time + duration;
                audioPlayer?.Play(RoboClashAudioEvent.ReloadStart);
                Invoke(nameof(FinishReload), duration);
                StateChanged?.Invoke(this);
            }
        }

        public void Cancel()
        {
            CancelInvoke();
            damageBuffEndTime = 0f;
            firing = false;
            firingUntil = 0f;
            recoil = 0f;
            reloadReadyTime = 0f;
            reloadDuration = 0f;
            nextShotTime = 0f;
            ammunition = definition.stats.magazineSize;
            sustainedMovement = 0f;
            stoppedMovement = 0f;
            movementReloadBonus = 0f;
            foreach (var projectile in activeProjectiles)
            {
                if (projectile != null)
                    Destroy(projectile.gameObject);
            }
            activeProjectiles.Clear();
            StateChanged?.Invoke(this);
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
            if (definition.rotatingBarrels != null)
                definition.rotatingBarrels.localRotation = Quaternion.identity;
            if (recoilTransform != null)
                recoilTransform.localPosition = recoilRestPosition;
            visualEffects?.StopAllEffects();
        }

        private void Update()
        {
            if (damageable != null && damageable.IsDead)
            {
                firing = false;
                return;
            }

            firing = Time.time < firingUntil;
            if (playerControlled && !movement.ControlsLocked && Keyboard.current != null &&
                Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                if (Mouse.current.leftButton.isPressed)
                    Fire();
                if (Keyboard.current.rKey.wasPressedThisFrame)
                    Reload();
            }
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 8f);
            if (recoilTransform != null)
                recoilTransform.localPosition = recoilRestPosition + Vector3.back * (0.06f * recoil);

            activeProjectiles.RemoveWhere(projectile => projectile == null);
            if (definition.robotType == RoboClashRobotType.Titan && firing &&
                definition.rotatingBarrels != null)
                definition.rotatingBarrels.Rotate(0f, 0f, 850f * Time.deltaTime, Space.Self);
        }

        private void FinishReload()
        {
            ammunition = definition.stats.magazineSize;
            reloadReadyTime = 0f;
            reloadDuration = 0f;
            audioPlayer?.Play(RoboClashAudioEvent.ReloadComplete);
            StateChanged?.Invoke(this);
        }

        private void HideMuzzleFlash()
        {
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        private bool TryGetExternalHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit closestHit)
        {
            closestHit = default;
            var closestDistance = float.PositiveInfinity;
            var hits = Physics.RaycastAll(origin, direction, distance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.transform.root == transform.root || hit.distance >= closestDistance)
                    continue;

                closestHit = hit;
                closestDistance = hit.distance;
            }

            return closestDistance < float.PositiveInfinity;
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    public enum RoboClashRobotType
    {
        Volt,
        Titan,
        Shadow,
        Spark
    }

    [System.Serializable]
    public sealed class RoboClashRobotStats
    {
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float maxShield = 50f;
        [Min(0f)] public float moveSpeed = 7f;
        [Min(0f)] public float acceleration = 20f;
        [Min(0f)] public float rotationSpeed = 240f;
        [Min(0.01f)] public float damagePerShot = 18f;
        [Min(0.01f)] public float shotsPerSecond = 4f;
        [Min(0.01f)] public float reloadDuration = 1.5f;
        [Min(0.1f)] public float weaponRange = 20f;
        [Min(0f)] public float incomingDamageMultiplier = 1f;
        [Min(0f)] public float ultimateChargePerDamage = 0.5f;
        [Min(1)] public int magazineSize = 20;
        [Min(0f)] public float knockbackResistance;
    }

    public sealed class RoboClashRobotDefinition : MonoBehaviour
    {
        public RoboClashRobotType robotType;
        public RoboClashRobotStats stats = new RoboClashRobotStats();

        [Header("Appearance")]
        [Tooltip("Shared generated team emission material used by projectiles and muzzle effects.")]
        public Material emissionMaterial;
        [Tooltip("Shared transparent shimmer material used by cloak and shield visuals.")]
        public Material shimmerMaterial;
        public Color primaryColor = Color.blue;
        public Color secondaryColor = Color.white;
        public Color jointColor = new Color(0.12f, 0.16f, 0.22f);
        public Color emissionColor = Color.cyan;
        public float passiveAmount = 0.25f;
        public float abilityDuration = 4f;
        public float abilityCooldown = 12f;
        public float ultimateDuration = 8f;
        public float ultimateCooldown = 45f;
        public float damageRadius;
        public float projectileSpeed = 42f;

        [Header("Weapon")]
        public Transform weaponMount;
        public Transform muzzle;
        public Transform rotatingBarrels;
        public Transform repairDrone;

        [Header("Robot Components")]
        public Transform aimPivot;
        public Transform modelRoot;
        public Transform introCameraAnchor;
        public Transform firstPersonCameraAnchor;

        [Header("Identity")]
        public string weaponName = "Pulse Rifle";
        public string passiveName = "Energy Efficiency";
        public string abilityName = "Dash";
        public string ultimateName = "Overdrive";
        public int teamId;

        public void Configure(RoboClashRobotType type)
        {
            robotType = type;
            if (stats == null)
                stats = new RoboClashRobotStats();

            switch (type)
            {
                case RoboClashRobotType.Volt:
                    SetStats(100f, 50f, 7f, 18f, 4f, 1.5f, 20f, 20, 1f);
                    primaryColor = Hex("147DFF");
                    secondaryColor = Hex("EAF4FF");
                    jointColor = Hex("202B3A");
                    emissionColor = Hex("00D9FF");
                    weaponName = "Pulse Rifle";
                    passiveName = "Energy Efficiency";
                    abilityName = "Dash";
                    ultimateName = "Overdrive";
                    abilityDuration = 0.3f;
                    abilityCooldown = 12f;
                    ultimateDuration = 8f;
                    ultimateCooldown = 45f;
                    projectileSpeed = 44f;
                    break;
                case RoboClashRobotType.Titan:
                    SetStats(150f, 80f, 4.2f, 9f, 7f, 2.4f, 17f, 42, 0.85f);
                    stats.knockbackResistance = 0.75f;
                    primaryColor = Hex("D71932");
                    secondaryColor = Hex("30343B");
                    jointColor = Hex("181A20");
                    emissionColor = Hex("FF3B27");
                    weaponName = "Gatling Cannon";
                    passiveName = "Fortified";
                    abilityName = "Defensive Field";
                    ultimateName = "Rail Cannon";
                    abilityDuration = 4f;
                    abilityCooldown = 18f;
                    ultimateDuration = 0f;
                    ultimateCooldown = 60f;
                    damageRadius = 3.5f;
                    projectileSpeed = 58f;
                    break;
                case RoboClashRobotType.Shadow:
                    SetStats(85f, 60f, 8f, 9f, 10f, 1.3f, 10f, 30, 1f);
                    primaryColor = Hex("542080");
                    secondaryColor = Hex("171321");
                    jointColor = Hex("171321");
                    emissionColor = Hex("E044FF");
                    weaponName = "Energy SMG";
                    passiveName = "Stealth Movement";
                    abilityName = "Cloak";
                    ultimateName = "Shadow Strike";
                    abilityDuration = 5f;
                    abilityCooldown = 16f;
                    ultimateDuration = 0f;
                    ultimateCooldown = 50f;
                    projectileSpeed = 48f;
                    break;
                case RoboClashRobotType.Spark:
                    SetStats(90f, 70f, 5.8f, 45f, 0.9f, 2.5f, 23f, 4, 1f);
                    primaryColor = Hex("FFC928");
                    secondaryColor = Hex("30343B");
                    jointColor = Hex("191B20");
                    emissionColor = Hex("FFB300");
                    weaponName = "Rocket Launcher";
                    passiveName = "Support Aura";
                    abilityName = "Repair Drone";
                    ultimateName = "EMP Blast";
                    abilityDuration = 5f;
                    abilityCooldown = 20f;
                    ultimateDuration = 3f;
                    ultimateCooldown = 55f;
                    damageRadius = 3.5f;
                    projectileSpeed = 30f;
                    break;
            }
        }

        public void ApplyDamageMultiplier(ref float damage)
        {
            damage *= Mathf.Max(0f, stats.incomingDamageMultiplier);
        }

        public static Vector3 GetTargetDimensions(RoboClashRobotType type)
        {
            switch (type)
            {
                case RoboClashRobotType.Titan: return new Vector3(1.8f, 2.5f, 1.4f);
                case RoboClashRobotType.Shadow: return new Vector3(0.9f, 1.8f, 0.7f);
                case RoboClashRobotType.Spark: return new Vector3(1.1f, 1.9f, 0.9f);
                default: return new Vector3(1.1f, 2f, 0.8f);
            }
        }

        public void EnsureCameraAnchors()
        {
            var dimensions = GetTargetDimensions(robotType);
            if (introCameraAnchor == null)
                introCameraAnchor = transform.Find("Intro Camera Anchor");
            if (firstPersonCameraAnchor == null)
                firstPersonCameraAnchor = transform.Find("First Person Camera Anchor");

            if (introCameraAnchor == null)
                introCameraAnchor = CreateCameraAnchor("Intro Camera Anchor",
                    new Vector3(0f, dimensions.y * 0.9f + 0.8f,
                        -(dimensions.y * 2.5f + 1f)),
                    Quaternion.Euler(18f, 0f, 0f));
            if (firstPersonCameraAnchor == null)
                firstPersonCameraAnchor = CreateCameraAnchor("First Person Camera Anchor",
                    new Vector3(0f, dimensions.y * 0.82f, dimensions.z * 0.42f),
                    Quaternion.identity);
        }

        private Transform CreateCameraAnchor(string anchorName, Vector3 localPosition,
            Quaternion localRotation)
        {
            var anchor = new GameObject(anchorName).transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = localPosition;
            anchor.localRotation = localRotation;
            return anchor;
        }

        private void SetStats(float health, float shield, float speed, float damage, float rate,
            float reload, float range, int magazine, float damageMultiplier)
        {
            stats.maxHealth = health;
            stats.maxShield = shield;
            stats.moveSpeed = speed;
            stats.acceleration = 20f;
            stats.rotationSpeed = 240f;
            stats.damagePerShot = damage;
            stats.shotsPerSecond = rate;
            stats.reloadDuration = reload;
            stats.weaponRange = range;
            stats.incomingDamageMultiplier = damageMultiplier;
            stats.ultimateChargePerDamage = 0.5f;
            stats.magazineSize = magazine;
            stats.knockbackResistance = 0f;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashRobotFactory.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    public static class RoboClashRobotFactory
    {
        public static GameObject CreateRobot(RoboClashRobotType type, Transform parent,
            Material primary, Material secondary, Material joints, Material emission)
        {
            return CreateRobot(type, parent, primary, secondary, joints, emission, null, null);
        }

        public static GameObject CreateRobot(RoboClashRobotType type, Transform parent,
            Material primary, Material secondary, Material joints, Material emission,
            Mesh armorMesh, Material shimmer)
        {
            var root = new GameObject(type.ToString().ToUpperInvariant());
            if (parent != null)
                root.transform.SetParent(parent, false);

            var dimensions = RoboClashRobotDefinition.GetTargetDimensions(type);
            var controller = root.AddComponent<CharacterController>();
            controller.height = dimensions.y;
            controller.radius = dimensions.x * 0.47f;
            controller.center = Vector3.up * (dimensions.y * 0.5f);
            controller.stepOffset = Mathf.Min(0.35f, dimensions.y * 0.18f);
            controller.slopeLimit = 50f;

            var definition = root.AddComponent<RoboClashRobotDefinition>();
            definition.Configure(type);
            definition.teamId = (int)type;
            definition.emissionMaterial = emission;
            definition.shimmerMaterial = shimmer;
            var player = root.AddComponent<RobotArenaController>();
            var movement = root.AddComponent<RoboClashRobotMovement>();
            var combat = root.AddComponent<RoboClashRobotCombat>();
            root.AddComponent<RoboClashRobotAI>();
            root.AddComponent<RoboClashRobotAbilities>();
            var animator = root.AddComponent<RoboClashRobotAnimator>();
            var audio = root.AddComponent<RoboClashRobotAudio>();
            audio.Configure(type);
            var visualEffects = root.AddComponent<RoboClashRobotVFX>();

            var model = new GameObject("ModelRoot").transform;
            model.SetParent(root.transform, false);
            definition.modelRoot = model;
            definition.EnsureCameraAnchors();

            var silhouette = BuildSilhouette(type, model, dimensions, primary, secondary, joints, emission,
                armorMesh, shimmer);
            definition.aimPivot = silhouette.aimPivot;
            definition.weaponMount = silhouette.weaponMount;
            definition.muzzle = silhouette.muzzle;
            if (definition.muzzle == null)
                definition.muzzle = FindDescendant(silhouette.weaponMount, "Muzzle");
            definition.rotatingBarrels = silhouette.rotatingBarrels;
            definition.repairDrone = silhouette.repairDrone;

            movement.SetVisualParts(silhouette.leftLeg, silhouette.rightLeg, silhouette.torso);
            animator.SetVisualParts(model, silhouette.torso, silhouette.aimPivot,
                silhouette.leftLeg, silhouette.rightLeg, silhouette.leftArm, silhouette.rightArm);
            combat.SetVisualParts(silhouette.weaponMount, silhouette.muzzleFlash);
            visualEffects.ConfigureVisuals();
            player.Configure(null, silhouette.muzzle, null);
            movement.ConfigurePlayer(null, false);
            combat.ConfigurePlayer(false);
            return root;
        }

        private static RobotSilhouette BuildSilhouette(RoboClashRobotType type, Transform model,
            Vector3 dimensions, Material primary, Material secondary, Material joints, Material emission,
            Mesh armorMesh, Material shimmer)
        {
            var silhouette = new RobotSilhouette();
            var scale = dimensions.y / 2f;
            var halfWidth = dimensions.x * 0.5f;

            var torso = Child("Torso", model, new Vector3(0f, 0.95f * scale, 0f));
            silhouette.torso = torso;
            var torsoWidth = type == RoboClashRobotType.Titan ? 0.95f : 0.62f;
            var torsoDepth = type == RoboClashRobotType.Titan ? 0.66f : 0.48f;
            ArmorPiece(armorMesh, "Chest Armor Shell", torso, Vector3.zero,
                new Vector3(torsoWidth, 0.8f, torsoDepth), primary);
            ArmorPiece(armorMesh, "Upper Chest Plate", torso, new Vector3(0f, 0.22f, torsoDepth * 0.48f),
                new Vector3(torsoWidth * 0.88f, 0.34f, 0.12f), secondary);
            ArmorPiece(armorMesh, "Lower Chest Plate", torso, new Vector3(0f, -0.22f, torsoDepth * 0.48f),
                new Vector3(torsoWidth * 0.78f, 0.22f, 0.1f), primary);
            var coreSize = type == RoboClashRobotType.Titan ? 0.27f : 0.18f;
            Piece(PrimitiveType.Sphere, "Chest Reactor Housing", torso,
                new Vector3(0f, 0.02f, torsoDepth * 0.57f),
                new Vector3(coreSize * 1.65f, coreSize * 1.35f, 0.1f), joints);
            Piece(PrimitiveType.Sphere, "Chest Energy Core", torso,
                new Vector3(0f, 0.02f, torsoDepth * 0.68f),
                new Vector3(coreSize, coreSize, 0.075f), emission);

            for (var side = -1; side <= 1; side += 2)
            {
                ArmorPiece(armorMesh, side < 0 ? "Left Chest Chevron" : "Right Chest Chevron",
                    torso, new Vector3(side * torsoWidth * 0.34f, -0.01f, torsoDepth * 0.52f),
                    new Vector3(torsoWidth * 0.14f, 0.37f, 0.07f), secondary,
                    Quaternion.Euler(0f, 0f, side * -14f));
                for (var vent = 0; vent < 3; vent++)
                    ArmorPiece(armorMesh, "Chest Cooling Vent " + side + " " + vent, torso,
                        new Vector3(side * torsoWidth * 0.34f, 0.25f - vent * 0.065f, torsoDepth * 0.55f),
                        new Vector3(0.1f, 0.018f, 0.025f), joints);
            }

            var aimPivot = Child("AimPivot", model, new Vector3(0f, 1.65f * scale, 0f));
            silhouette.aimPivot = aimPivot;
            var headScale = type == RoboClashRobotType.Titan ? 0.39f : 0.3f;
            ArmorPiece(armorMesh, "Angular Helmet Shell", aimPivot, new Vector3(0f, 0.06f, 0f),
                new Vector3(headScale * 1.5f, headScale, headScale * 0.9f), primary);
            ArmorPiece(armorMesh, "Helmet Brow Armor", aimPivot,
                new Vector3(0f, headScale * 0.25f, headScale * 0.33f),
                new Vector3(headScale * 1.3f, 0.09f, 0.12f), secondary);
            ArmorPiece(armorMesh, "Visor Housing", aimPivot,
                new Vector3(0f, 0.035f, headScale * 0.47f),
                new Vector3(headScale * 1.23f, 0.13f, 0.07f), joints);
            ArmorPiece(armorMesh, "Optic Visor", aimPivot,
                new Vector3(0f, 0.035f, headScale * 0.54f),
                new Vector3(headScale * 0.94f, 0.045f, 0.025f), emission);
            for (var side = -1; side <= 1; side += 2)
            {
                Piece(PrimitiveType.Cylinder, "Helmet Sensor", aimPivot,
                    new Vector3(side * headScale * 0.73f, 0.04f, 0.01f),
                    new Vector3(0.055f, 0.1f, 0.055f), emission, Quaternion.Euler(90f, 0f, 0f));
            }

            var shoulderWidth = type == RoboClashRobotType.Titan ? 0.48f : 0.3f;
            var armLength = type == RoboClashRobotType.Titan ? 0.58f : 0.48f;
            silhouette.leftArm = BuildArm(type, model, -halfWidth * 0.68f, shoulderWidth, armLength,
                primary, secondary, joints, armorMesh, emission, false);
            silhouette.rightArm = BuildArm(type, model, halfWidth * 0.68f, shoulderWidth, armLength,
                primary, secondary, joints, armorMesh, emission, true);

            var hipWidth = type == RoboClashRobotType.Titan ? 0.66f : 0.46f;
            ArmorPiece(armorMesh, "Hip Chassis", model, new Vector3(0f, 0.59f * scale, 0f),
                new Vector3(hipWidth, 0.22f, 0.38f), joints);
            var legGap = type == RoboClashRobotType.Titan ? 0.42f : 0.23f;
            var legLength = type == RoboClashRobotType.Titan ? 0.65f : 0.64f;
            silhouette.leftLeg = BuildLeg(model, "Left", -legGap, legLength, primary, secondary, joints, armorMesh);
            silhouette.rightLeg = BuildLeg(model, "Right", legGap, legLength, primary, secondary, joints, armorMesh);

            if (type == RoboClashRobotType.Titan)
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    var isLarge = side < 0;
                    var shoulder = new Vector3(side * halfWidth * 0.78f, 1.45f * scale, 0f);
                    ArmorPiece(armorMesh, isLarge ? "Titan Asymmetric Bulwark" : "Titan Cannon-Side Guard",
                        model, shoulder,
                        new Vector3(shoulderWidth * (isLarge ? 1.9f : 1.35f), isLarge ? 0.62f : 0.4f,
                            isLarge ? 0.64f : 0.48f), primary, Quaternion.Euler(0f, 0f, side * -8f));
                    ArmorPiece(armorMesh, "Titan Shoulder Inset", model,
                        shoulder + new Vector3(0f, 0.08f, 0.29f),
                        new Vector3(shoulderWidth * 1.05f, 0.12f, 0.05f), secondary);
                }
            }
            else if (type == RoboClashRobotType.Spark)
            {
                for (var side = -1; side <= 1; side += 2)
                    Piece(PrimitiveType.Sphere, "Rounded Utility Shoulder", model,
                        new Vector3(side * halfWidth * 0.72f, 1.43f * scale, 0f),
                        new Vector3(0.42f, 0.38f, 0.38f), primary);
                BuildBackpack(model, dimensions, primary, secondary, emission, armorMesh);
            }
            else if (type == RoboClashRobotType.Volt)
                BuildVoltPowerUnit(model, primary, secondary, emission, armorMesh);
            else if (type == RoboClashRobotType.Shadow)
                BuildShadowEmitters(model, dimensions, emission);

            var weaponMount = BuildWeapon(type, aimPivot, dimensions, primary, secondary, joints, emission,
                armorMesh,
                out var muzzleFlash, out var rotatingBarrels);
            silhouette.weaponMount = weaponMount;
            silhouette.muzzle = weaponMount.Find("Muzzle");
            silhouette.muzzleFlash = muzzleFlash;
            silhouette.rotatingBarrels = rotatingBarrels;

            if (type == RoboClashRobotType.Spark)
                silhouette.repairDrone = BuildRepairDrone(model, primary, secondary, emission);
            if (type == RoboClashRobotType.Shadow)
                silhouette.cloakVisual = BuildCloakShell(model, dimensions, shimmer);

            return silhouette;
        }

        private static Transform BuildArm(RoboClashRobotType type, Transform model, float x,
            float armorWidth, float armLength, Material primary, Material secondary, Material joints,
            Mesh armorMesh, Material emission, bool weaponArm)
        {
            var shoulder = Child(x < 0f ? "Left Shoulder Joint" : "Right Shoulder Joint",
                model, new Vector3(x, 1.39f, 0f));
            Piece(PrimitiveType.Sphere, "Shoulder Actuator", shoulder, Vector3.zero,
                new Vector3(0.28f, 0.3f, 0.3f), joints);
            ArmorPiece(armorMesh, type == RoboClashRobotType.Titan ? "Heavy Shoulder Armor" : "Shoulder Armor",
                shoulder, new Vector3(0f, 0.07f, 0f),
                new Vector3(armorWidth, 0.32f, 0.38f), primary);
            Piece(PrimitiveType.Cylinder, "Upper Arm", shoulder,
                new Vector3(0f, -armLength * 0.7f, 0f),
                new Vector3(0.13f, armLength * 0.55f, 0.13f), secondary);
            Piece(PrimitiveType.Sphere, "Elbow Joint", shoulder,
                new Vector3(0f, -armLength * 1.35f, 0f),
                Vector3.one * 0.17f, joints);
            ArmorPiece(armorMesh, "Forearm Armor", shoulder,
                new Vector3(0f, -armLength * 1.85f, 0.03f),
                new Vector3(0.23f, armLength * 0.75f, 0.28f), primary);
            Piece(PrimitiveType.Cylinder, "Wrist Actuator", shoulder,
                new Vector3(0f, -armLength * 2.22f, 0.04f),
                new Vector3(0.095f, 0.12f, 0.095f), joints);
            ArmorPiece(armorMesh, weaponArm ? "Weapon Grip Hand" : "Support Hand", shoulder,
                new Vector3(0f, -armLength * 2.43f, 0.13f),
                new Vector3(0.2f, 0.2f, 0.22f), joints);
            for (var finger = -1; finger <= 1; finger++)
                Piece(PrimitiveType.Cylinder, "Finger Actuator " + finger, shoulder,
                    new Vector3(finger * 0.055f, -armLength * 2.55f, 0.2f),
                    new Vector3(0.035f, 0.1f, 0.035f), secondary, Quaternion.Euler(90f, 0f, 0f));

            Rod("Forearm Hydraulic", shoulder,
                new Vector3(0.105f, -armLength * 1.3f, -0.06f),
                new Vector3(0.105f, -armLength * 2.1f, -0.06f), 0.045f, emission);
            for (var vent = 0; vent < 3; vent++)
                ArmorPiece(armorMesh, "Forearm Vent " + vent, shoulder,
                    new Vector3(0f, -armLength * (1.6f + vent * 0.13f), 0.18f),
                    new Vector3(0.12f, 0.018f, 0.018f), joints);
            return shoulder;
        }

        private static Transform BuildLeg(Transform model, string side, float x, float length,
            Material primary, Material secondary, Material joints, Mesh armorMesh)
        {
            var hip = Child(side + " Leg", model, new Vector3(x, length * 1.74f, 0f));
            Piece(PrimitiveType.Sphere, "Hip Joint", hip, Vector3.zero, Vector3.one * 0.2f, joints);
            ArmorPiece(armorMesh, "Thigh Armor Shell", hip, new Vector3(0f, -length * 0.39f, 0f),
                new Vector3(0.3f, length * 0.7f, 0.34f), primary);
            ArmorPiece(armorMesh, "Thigh Front Plate", hip, new Vector3(0f, -length * 0.39f, 0.17f),
                new Vector3(0.19f, length * 0.47f, 0.045f), secondary);
            Piece(PrimitiveType.Sphere, "Knee Joint", hip, new Vector3(0f, -length * 0.8f, 0f),
                Vector3.one * 0.16f, joints);
            ArmorPiece(armorMesh, "Knee Guard", hip, new Vector3(0f, -length * 0.8f, 0.14f),
                new Vector3(0.3f, 0.19f, 0.12f), primary);
            ArmorPiece(armorMesh, "Shin Armor", hip, new Vector3(0f, -length * 1.26f, 0f),
                new Vector3(0.25f, length * 0.75f, 0.3f), secondary);
            Piece(PrimitiveType.Cylinder, "Ankle Joint", hip,
                new Vector3(0f, -length * 1.57f, 0f), new Vector3(0.1f, 0.11f, 0.1f), joints);
            ArmorPiece(armorMesh, "Articulated Foot", hip, new Vector3(0f, -length * 1.72f, 0.12f),
                new Vector3(0.39f, 0.14f, 0.49f), joints);
            ArmorPiece(armorMesh, "Toe Armor", hip, new Vector3(0f, -length * 1.72f, 0.31f),
                new Vector3(0.35f, 0.07f, 0.13f), primary);
            Rod("Knee Hydraulic", hip, new Vector3(0.12f, -length * 0.88f, -0.08f),
                new Vector3(0.12f, -length * 1.46f, -0.08f), 0.04f, joints);
            return hip;
        }

        private static void BuildBackpack(Transform model, Vector3 dimensions,
            Material primary, Material secondary, Material emission, Mesh armorMesh)
        {
            var back = -dimensions.z * 0.5f;
            ArmorPiece(armorMesh, "Support Backpack Housing", model, new Vector3(0f, 1.05f, back),
                new Vector3(0.48f, 0.56f, 0.28f), secondary);
            Piece(PrimitiveType.Cylinder, "Repair Canister Left", model, new Vector3(-0.2f, 1.05f, back - 0.12f),
                new Vector3(0.13f, 0.36f, 0.13f), primary);
            Piece(PrimitiveType.Cylinder, "Repair Canister Right", model, new Vector3(0.2f, 1.05f, back - 0.12f),
                new Vector3(0.13f, 0.36f, 0.13f), primary);
            ArmorPiece(armorMesh, "Repair Module Hatch", model, new Vector3(0f, 1.05f, back - 0.16f),
                new Vector3(0.25f, 0.32f, 0.05f), primary);
            Piece(PrimitiveType.Sphere, "Backpack Status Light", model,
                new Vector3(0f, 1.26f, back - 0.17f), Vector3.one * 0.12f, emission);
            Rod("Left Backpack Cable", model, new Vector3(-0.22f, 0.83f, back + 0.08f),
                new Vector3(-0.16f, 0.55f, 0.06f), 0.035f, emission);
            Rod("Right Backpack Cable", model, new Vector3(0.22f, 0.83f, back + 0.08f),
                new Vector3(0.16f, 0.55f, 0.06f), 0.035f, emission);
        }

        private static Transform BuildWeapon(RoboClashRobotType type, Transform aimPivot, Vector3 dimensions,
            Material primary, Material secondary, Material joints, Material emission, Mesh armorMesh,
            out Renderer muzzleFlash, out Transform rotatingBarrels)
        {
            var mount = Child("WeaponMount", aimPivot,
                new Vector3(type == RoboClashRobotType.Spark ? 0.32f : 0.22f, -0.5f, dimensions.z * 0.2f));
            var gun = Child(type == RoboClashRobotType.Volt ? "Pulse Rifle" :
                type == RoboClashRobotType.Titan ? "Gatling Cannon" :
                type == RoboClashRobotType.Shadow ? "Energy SMG" : "Shoulder Rocket Launcher", mount, Vector3.zero);

            if (type == RoboClashRobotType.Titan)
            {
                ArmorPiece(armorMesh, "Cannon Housing", gun, new Vector3(0f, 0.02f, 0.05f),
                    new Vector3(0.52f, 0.4f, 0.62f), primary);
                ArmorPiece(armorMesh, "Cannon Mounting Bracket", gun, new Vector3(-0.2f, -0.25f, -0.08f),
                    new Vector3(0.28f, 0.18f, 0.4f), joints);
                rotatingBarrels = Child("Rotating Gatling Barrels", gun, new Vector3(0f, 0f, 0.32f));
                for (var i = 0; i < 4; i++)
                {
                    var angle = i * 90f * Mathf.Deg2Rad;
                    Piece(PrimitiveType.Cylinder, "Cannon Barrel " + (i + 1), rotatingBarrels,
                        new Vector3(Mathf.Cos(angle) * 0.16f, Mathf.Sin(angle) * 0.16f, 0.4f),
                        new Vector3(0.09f, 0.37f, 0.09f), secondary, Quaternion.Euler(90f, 0f, 0f));
                    Piece(PrimitiveType.Cylinder, "Red Barrel Core " + (i + 1), rotatingBarrels,
                        new Vector3(Mathf.Cos(angle) * 0.16f, Mathf.Sin(angle) * 0.16f, 0.78f),
                        new Vector3(0.045f, 0.04f, 0.045f), emission);
                }

                var titanMuzzle = Child("Muzzle", rotatingBarrels, new Vector3(0f, 0f, 0.86f));
                Piece(PrimitiveType.Sphere, "MuzzleFlash", titanMuzzle, Vector3.zero,
                    Vector3.one * 0.24f, emission).gameObject.name = "MuzzleFlash";
                muzzleFlash = titanMuzzle.GetComponentInChildren<Renderer>();
                muzzleFlash.enabled = false;
                for (var side = -1; side <= 1; side += 2)
                    Piece(PrimitiveType.Cylinder, "Rotary Motor", gun, new Vector3(side * 0.27f, -0.15f, 0.12f),
                        new Vector3(0.11f, 0.18f, 0.11f), joints, Quaternion.Euler(0f, 0f, 90f));
                return mount;
            }
            rotatingBarrels = null;

            var spark = type == RoboClashRobotType.Spark;
            var shadow = type == RoboClashRobotType.Shadow;
            var bodyMaterial = spark || shadow ? secondary : primary;
            ArmorPiece(armorMesh, "Weapon Receiver", gun, Vector3.zero,
                spark ? new Vector3(0.4f, 0.42f, 0.48f) : shadow ? new Vector3(0.26f, 0.23f, 0.52f) :
                new Vector3(0.3f, 0.28f, 0.62f), bodyMaterial);
            Piece(PrimitiveType.Cylinder, spark ? "Rocket Tube" : "Weapon Barrel", gun,
                new Vector3(0f, 0f, 0.43f),
                new Vector3(spark ? 0.19f : shadow ? 0.075f : 0.1f, spark ? 0.3f : 0.34f,
                    spark ? 0.19f : shadow ? 0.075f : 0.1f),
                secondary, Quaternion.Euler(90f, 0f, 0f));
            Piece(PrimitiveType.Cylinder, "Muzzle Shroud", gun, new Vector3(0f, 0f, 0.66f),
                new Vector3(spark ? 0.22f : 0.12f, 0.07f, spark ? 0.22f : 0.12f),
                joints, Quaternion.Euler(90f, 0f, 0f));
            ArmorPiece(armorMesh, spark ? "Rocket Magazine" : "Weapon Energy Cell",
                gun, new Vector3(0f, -0.19f, 0.06f),
                spark ? new Vector3(0.22f, 0.29f, 0.24f) : new Vector3(0.17f, 0.23f, 0.19f),
                spark ? primary : emission);
            Piece(PrimitiveType.Sphere, "Weapon Energy Core", gun, new Vector3(0f, 0.16f, 0.08f),
                Vector3.one * 0.13f, emission);
            for (var side = -1; side <= 1; side += 2)
                ArmorPiece(armorMesh, "Weapon Side Rail " + side, gun,
                    new Vector3(side * (spark ? 0.21f : 0.16f), 0f, 0.06f),
                    new Vector3(0.035f, 0.08f, 0.42f), joints);
            var muzzle = Child("Muzzle", gun, new Vector3(0f, 0f, spark ? 0.76f : 0.61f));
            var flash = Piece(PrimitiveType.Sphere, "MuzzleFlash", muzzle, Vector3.zero,
                Vector3.one * (spark ? 0.3f : 0.17f), emission);
            muzzleFlash = flash.GetComponent<Renderer>();
            muzzleFlash.enabled = false;
            return mount;
        }

        private static Transform BuildRepairDrone(Transform model, Material primary, Material secondary, Material emission)
        {
            var drone = Child("Repair Drone", model, new Vector3(0.9f, 1.72f, -0.35f));
            Piece(PrimitiveType.Sphere, "Drone Core", drone, Vector3.zero,
                new Vector3(0.4f, 0.25f, 0.34f), secondary);
            Piece(PrimitiveType.Cube, "Drone Yellow Shell", drone, Vector3.zero,
                new Vector3(0.3f, 0.15f, 0.28f), primary);
            Piece(PrimitiveType.Sphere, "Drone Status Light", drone,
                new Vector3(0f, 0f, 0.18f), Vector3.one * 0.1f, emission);
            for (var side = -1; side <= 1; side += 2)
                Piece(PrimitiveType.Cylinder, "Hover Rotor", drone, new Vector3(side * 0.25f, 0f, 0f),
                    new Vector3(0.07f, 0.16f, 0.07f), emission);
            return drone;
        }

        private static void BuildVoltPowerUnit(Transform model, Material primary, Material secondary,
            Material emission, Mesh armorMesh)
        {
            ArmorPiece(armorMesh, "Volt Back Power Unit", model, new Vector3(0f, 1.05f, -0.34f),
                new Vector3(0.42f, 0.48f, 0.28f), secondary);
            for (var side = -1; side <= 1; side += 2)
            {
                Piece(PrimitiveType.Cylinder, "Power Cell " + side, model,
                    new Vector3(side * 0.22f, 1.08f, -0.42f),
                    new Vector3(0.1f, 0.32f, 0.1f), primary);
                Piece(PrimitiveType.Sphere, "Power Cell Indicator " + side, model,
                    new Vector3(side * 0.22f, 1.08f, -0.5f),
                    Vector3.one * 0.07f, emission);
            }
            Rod("Power Conduit Left", model, new Vector3(-0.18f, 0.86f, -0.25f),
                new Vector3(-0.14f, 0.67f, 0.12f), 0.035f, emission);
            Rod("Power Conduit Right", model, new Vector3(0.18f, 0.86f, -0.25f),
                new Vector3(0.14f, 0.67f, 0.12f), 0.035f, emission);
        }

        private static void BuildShadowEmitters(Transform model, Vector3 dimensions, Material emission)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                Piece(PrimitiveType.Sphere, "Stealth Emitter " + side, model,
                    new Vector3(side * dimensions.x * 0.42f, 1.2f, -dimensions.z * 0.45f),
                    new Vector3(0.1f, 0.16f, 0.1f), emission);
                Rod("Shadow Energy Conduit " + side, model,
                    new Vector3(side * 0.18f, 1.22f, 0.24f),
                    new Vector3(side * 0.17f, 0.72f, 0.18f), 0.024f, emission);
            }
        }

        private static Renderer BuildCloakShell(Transform model, Vector3 dimensions, Material shimmer)
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "CloakVisual";
            shell.transform.SetParent(model, false);
            shell.transform.localPosition = Vector3.up * (dimensions.y * 0.5f);
            shell.transform.localScale = new Vector3(dimensions.x * 1.08f, dimensions.y * 0.58f, dimensions.z * 1.15f);
            DestroyCollider(shell.GetComponent<Collider>());
            var renderer = shell.GetComponent<Renderer>();
            if (shimmer != null)
                renderer.sharedMaterial = shimmer;
            renderer.enabled = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        public static Mesh CreateArmorMesh()
        {
            const int sides = 8;
            var vertices = new Vector3[sides * 4 + 2];
            var cuts = new[]
            {
                new Vector2(-0.5f + 0.14f, -0.5f), new Vector2(0.5f - 0.14f, -0.5f),
                new Vector2(0.5f, -0.5f + 0.14f), new Vector2(0.5f, 0.5f - 0.14f),
                new Vector2(0.5f - 0.14f, 0.5f), new Vector2(-0.5f + 0.14f, 0.5f),
                new Vector2(-0.5f, 0.5f - 0.14f), new Vector2(-0.5f, -0.5f + 0.14f)
            };
            var rings = new[] { (z: -0.5f, scale: 0.82f), (z: -0.42f, scale: 1f),
                (z: 0.42f, scale: 1f), (z: 0.5f, scale: 0.82f) };
            for (var ring = 0; ring < rings.Length; ring++)
            {
                for (var i = 0; i < sides; i++)
                {
                    var point = cuts[i] * rings[ring].scale;
                    vertices[ring * sides + i] = new Vector3(point.x, point.y, rings[ring].z);
                }
            }

            vertices[vertices.Length - 2] = new Vector3(0f, 0f, -0.5f);
            vertices[vertices.Length - 1] = new Vector3(0f, 0f, 0.5f);
            var triangles = new int[sides * ((rings.Length - 1) * 6 + 6)];
            var index = 0;
            for (var ring = 0; ring < rings.Length - 1; ring++)
            {
                for (var i = 0; i < sides; i++)
                {
                    var a = ring * sides + i;
                    var b = ring * sides + (i + 1) % sides;
                    var c = (ring + 1) * sides + (i + 1) % sides;
                    var d = (ring + 1) * sides + i;
                    triangles[index++] = a;
                    triangles[index++] = b;
                    triangles[index++] = c;
                    triangles[index++] = a;
                    triangles[index++] = c;
                    triangles[index++] = d;
                }
            }

            var backCenter = vertices.Length - 2;
            var frontCenter = vertices.Length - 1;
            for (var i = 0; i < sides; i++)
            {
                triangles[index++] = backCenter;
                triangles[index++] = (i + 1) % sides;
                triangles[index++] = i;
                var start = (rings.Length - 1) * sides;
                triangles[index++] = frontCenter;
                triangles[index++] = start + i;
                triangles[index++] = start + (i + 1) % sides;
            }

            var mesh = new Mesh { name = "RoboClash Beveled Armor" };
            mesh.vertices = vertices;
            var uvs = new Vector2[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
                uvs[i] = new Vector2(vertices[i].x + 0.5f, vertices[i].y + 0.5f);
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Transform Child(string name, Transform parent, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        private static Renderer ArmorPiece(Mesh mesh, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material, Quaternion? rotation = null)
        {
            if (mesh == null)
                return Piece(PrimitiveType.Cube, name, parent, localPosition, localScale, material, rotation);

            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            if (rotation.HasValue)
                piece.transform.localRotation = rotation.Value;
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = piece.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void Rod(string name, Transform parent, Vector3 start, Vector3 end,
            float radius, Material material)
        {
            var direction = end - start;
            var rod = Piece(PrimitiveType.Cylinder, name, parent, (start + end) * 0.5f,
                new Vector3(radius, direction.magnitude * 0.5f, radius), material);
            rod.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }

        private static Transform FindDescendant(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
                var descendant = FindDescendant(child, childName);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }

        private static Renderer Piece(PrimitiveType primitive, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material, Quaternion? rotation = null)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            if (rotation.HasValue)
                piece.transform.localRotation = rotation.Value;
            var collider = piece.GetComponent<Collider>();
            DestroyCollider(collider);
            var renderer = piece.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void DestroyCollider(Collider collider)
        {
            if (collider == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }

        private sealed class RobotSilhouette
        {
            public Transform aimPivot;
            public Transform weaponMount;
            public Transform muzzle;
            public Transform rotatingBarrels;
            public Transform repairDrone;
            public Transform leftLeg;
            public Transform rightLeg;
            public Transform leftArm;
            public Transform rightArm;
            public Transform torso;
            public Renderer muzzleFlash;
            public Renderer cloakVisual;
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashRobotMovement.cs`

````csharp
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboClash
{
    public enum RoboClashCameraState
    {
        Intro,
        FirstPerson,
        ThirdPerson,
        Dead,
        Respawning,
        Disabled
    }

    [RequireComponent(typeof(CharacterController), typeof(RoboClashRobotDefinition))]
    public sealed class RoboClashRobotMovement : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0f)] private float jumpHeight = 1.6f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.22f;
        [SerializeField, Min(0f)] private float cameraCollisionPadding = 0.16f;
        [SerializeField, Min(1f)] private float thirdPersonDistance = 4.5f;
        [SerializeField, Min(0f)] private float thirdPersonHeight = 0.4f;
        [SerializeField] private bool playerControlled;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform torso;

        private CharacterController characterController;
        private RoboClashRobotDefinition definition;
        private ArenaMovingPlatform currentPlatform;
        private ArenaMovingPlatform platformContact;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float yaw;
        private float pitch = 12f;
        private RoboClashRobotAnimator animator;
        private RoboClashRobotAudio audioPlayer;
        private RoboClashRobotAbilities abilities;
        private RoboClashRobotCombat combat;
        private readonly RaycastHit[] cameraObstructionHits = new RaycastHit[16];
        private float nextFootstepTime;
        private bool matchInputLocked;
        private bool abilityInputLocked;
        private bool cameraTransitionInputLocked;
        private Transform introCameraAnchor;
        private Transform firstPersonCameraAnchor;
        private Renderer[] firstPersonRenderers = new Renderer[0];
        private bool[] firstPersonRendererDefaults = new bool[0];
        private bool firstPersonPresentationActive;
        private RoboClashCameraState cameraState = RoboClashCameraState.Disabled;
        private RoboClashCameraMode cameraMode = RoboClashCameraMode.FirstPerson;
        private float cameraTransitionDuration;
        private float cameraTransitionElapsed;
        private float fovKick;
        private float kickPitch;
        private float shake;

        public Camera PlayerCamera => playerCamera;
        public float AimPitch => pitch;
        public float Yaw => yaw;
        public bool PlayerControlled => playerControlled;
        public RoboClashCameraState CameraState => cameraState;
        public bool ControlsLocked
        {
            get => matchInputLocked || abilityInputLocked || cameraTransitionInputLocked;
            set => abilityInputLocked = value;
        }
        public float HorizontalSpeed => new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            definition = GetComponent<RoboClashRobotDefinition>();
            animator = GetComponent<RoboClashRobotAnimator>();
            audioPlayer = GetComponent<RoboClashRobotAudio>();
            abilities = GetComponent<RoboClashRobotAbilities>();
            combat = GetComponent<RoboClashRobotCombat>();
            yaw = transform.eulerAngles.y;
        }

        public void ConfigurePlayer(Camera camera, bool enabledForPlayer)
        {
            playerCamera = camera;
            playerControlled = enabledForPlayer;
            cameraMode = RoboClashSession.CameraMode;
            definition?.EnsureCameraAnchors();
            introCameraAnchor = definition != null ? definition.introCameraAnchor : null;
            firstPersonCameraAnchor = definition != null ? definition.firstPersonCameraAnchor : null;

            if (!enabledForPlayer || camera == null)
            {
                SetFirstPersonPresentation(false);
                cameraState = RoboClashCameraState.Disabled;
                cameraTransitionInputLocked = false;
                return;
            }

            CacheFirstPersonRenderers();
            if (definition.aimPivot != null)
                definition.aimPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            BeginSpawnIntro();
        }

        public void SetCameraMode(RoboClashCameraMode mode)
        {
            cameraMode = System.Enum.IsDefined(typeof(RoboClashCameraMode), mode)
                ? mode
                : RoboClashCameraMode.FirstPerson;
            if (!playerControlled || playerCamera == null ||
                cameraState == RoboClashCameraState.Intro ||
                cameraState == RoboClashCameraState.Respawning ||
                cameraState == RoboClashCameraState.Dead ||
                cameraState == RoboClashCameraState.Disabled)
                return;

            cameraTransitionInputLocked = false;
            EnterSelectedCameraMode();
        }

        public void SetControlled(bool controlled)
        {
            playerControlled = controlled;
            horizontalVelocity = Vector3.zero;
            if (controlled)
            {
                yaw = transform.eulerAngles.y;
                pitch = 12f;
                if (definition.aimPivot != null)
                    definition.aimPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            else
            {
                currentPlatform = null;
                platformContact = null;
            }
        }

        public void SetMatchInputLocked(bool locked)
        {
            matchInputLocked = locked;
        }

        public void BeginSpawnIntro()
        {
            animator?.TriggerSpawnReady(1.5f);
            BeginCameraTransition(RoboClashCameraState.Intro, 1.5f);
        }

        public void BeginRespawnCameraTransition()
        {
            animator?.TriggerSpawnReady(0.65f);
            BeginCameraTransition(RoboClashCameraState.Respawning, 0.65f);
        }

        public void SetCameraDead()
        {
            cameraTransitionInputLocked = playerCamera != null;
            cameraState = playerCamera != null
                ? RoboClashCameraState.Dead
                : RoboClashCameraState.Disabled;
            SetFirstPersonPresentation(false);

            if (playerCamera == null)
                return;

            if (introCameraAnchor != null)
            {
                var lookTarget = transform.position + Vector3.up * 0.9f;
                var lookDirection = lookTarget - introCameraAnchor.position;
                playerCamera.transform.SetPositionAndRotation(
                    introCameraAnchor.position,
                    lookDirection.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(lookDirection, Vector3.up)
                        : introCameraAnchor.rotation);
            }
        }

        public void SetCameraDisabled()
        {
            cameraTransitionInputLocked = false;
            cameraState = RoboClashCameraState.Disabled;
        }

        public void ResetMotion()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            currentPlatform = null;
            platformContact = null;
        }

        public void ApplyImpulse(Vector3 impulse)
        {
            horizontalVelocity += new Vector3(impulse.x, 0f, impulse.z) *
                                  Mathf.Clamp01(1f - definition.stats.knockbackResistance);
        }

        public bool TryDash(Vector3 direction, float distance)
        {
            return TryDash(direction, distance, out _);
        }

        public bool TryDash(Vector3 direction, float distance, out Vector3 endPosition)
        {
            endPosition = transform.position;
            if (characterController == null || !characterController.enabled || ControlsLocked ||
                distance <= 0f)
                return false;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = transform.forward;
            direction.Normalize();

            var radius = Mathf.Max(0.1f, characterController.radius * 0.9f);
            var origin = transform.position + characterController.center;
            if (Physics.SphereCast(origin, radius, direction, out var hit, distance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0f, hit.distance - radius * 0.2f);

            if (distance <= 0.1f)
                return false;

            endPosition = transform.position + direction * distance;
            fovKick = 1f;
            StartCoroutine(DashRoutine(direction, distance, 0.12f));
            return true;
        }

        private IEnumerator DashRoutine(Vector3 direction, float distance, float duration)
        {
            var elapsed = 0f;
            var moved = 0f;
            while (elapsed < duration && characterController != null && characterController.enabled)
            {
                elapsed += Time.deltaTime;
                var target = distance * Mathf.Clamp01(elapsed / duration);
                var step = target - moved;
                if (step > 0f)
                    characterController.Move(direction * step);
                moved = target;
                yield return null;
            }
        }

        public void AddCameraKick(float pitchDegrees, float shakeAmount)
        {
            if (!playerControlled)
                return;

            kickPitch = Mathf.Min(kickPitch + pitchDegrees, 3f);
            shake = Mathf.Clamp01(Mathf.Max(shake, shakeAmount));
        }

        private void UpdateCameraFeedback()
        {
            var dt = Time.deltaTime;
            kickPitch = Mathf.MoveTowards(kickPitch, 0f, 10f * dt);
            shake = Mathf.MoveTowards(shake, 0f, 3f * dt);
            fovKick = Mathf.MoveTowards(fovKick, 0f, 6f * dt);
            if (playerCamera != null)
                playerCamera.fieldOfView = RoboClashSession.FieldOfView + fovKick * 12f;
        }

        private Quaternion GetCameraRotation()
        {
            var jitter = shake > 0.001f
                ? new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)) * (shake * 1.2f)
                : Vector3.zero;
            return Quaternion.Euler(pitch - kickPitch + jitter.x, yaw + jitter.y, jitter.z);
        }

        public bool TryTeleport(Vector3 destination)
        {
            if (characterController == null || !characterController.enabled || ControlsLocked)
                return false;

            var oldPosition = transform.position;
            var distance = Vector3.Distance(oldPosition, destination);
            var hasSafeGround = false;
            foreach (var groundHit in Physics.RaycastAll(destination + Vector3.up,
                         Vector3.down, 5f, Physics.DefaultRaycastLayers,
                         QueryTriggerInteraction.Ignore))
            {
                if (groundHit.normal.y < 0.5f ||
                    groundHit.collider.GetComponentInParent<ArenaDamageable>() != null ||
                    groundHit.collider.GetComponentInParent<ArenaHazard>() != null ||
                    Mathf.Abs(groundHit.point.y - destination.y) > 1.25f)
                    continue;

                hasSafeGround = true;
                break;
            }

            if (!hasSafeGround)
                return false;

            foreach (var hit in Physics.CapsuleCastAll(
                         oldPosition + Vector3.up * 0.35f,
                         oldPosition + Vector3.up * (characterController.height - 0.2f),
                         characterController.radius * 0.9f,
                         distance > 0.01f ? (destination - oldPosition).normalized : transform.forward,
                         distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.root != transform.root &&
                    hit.collider.GetComponentInParent<ArenaDamageable>() == null)
                    return false;
            }

            var bottom = destination + Vector3.up * 0.05f;
            var top = destination + Vector3.up * (characterController.height - 0.1f);
            foreach (var collider in Physics.OverlapCapsule(bottom, top, characterController.radius * 0.9f,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (collider.transform.root != transform.root &&
                    collider.GetComponentInParent<ArenaDamageable>() == null)
                    return false;
            }

            var controllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
            transform.position = destination;
            characterController.enabled = controllerWasEnabled;
            horizontalVelocity = Vector3.zero;
            return true;
        }

        public void SetVisualParts(Transform left, Transform right, Transform body)
        {
            leftLeg = left;
            rightLeg = right;
            torso = body;
        }

        public void DriveAI(Vector3 movementDirection, Vector3 aimDirection, float aimTurnSpeed = 0f)
        {
            playerControlled = false;
            if (matchInputLocked)
            {
                HaltHorizontalMotion();
                return;
            }

            if (characterController == null || !characterController.enabled || definition == null)
                return;

            if (abilityInputLocked)
            {
                ApplyLockedMovement();
                return;
            }

            movementDirection.y = 0f;
            aimDirection.y = 0f;
            var speedMultiplier = abilities != null ? abilities.GetMoveSpeedMultiplier() : 1f;
            var desired = movementDirection.sqrMagnitude > 0.01f
                ? movementDirection.normalized * definition.stats.moveSpeed * speedMultiplier
                : Vector3.zero;
            ApplyMovement(desired, false);

            if (aimDirection.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(aimDirection, Vector3.up),
                    (aimTurnSpeed > 0f ? aimTurnSpeed : definition.stats.rotationSpeed) * Time.deltaTime);
        }

        private void Update()
        {
            if (!playerControlled)
                return;
            if (!string.Equals(
                    gameObject.scene.path,
                    "Assets/Scenes/RoboClashGameplay.unity",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (matchInputLocked || characterController == null || !characterController.enabled)
            {
                HaltHorizontalMotion();
                return;
            }
            if (cameraTransitionInputLocked ||
                (cameraState != RoboClashCameraState.FirstPerson &&
                 cameraState != RoboClashCameraState.ThirdPerson) ||
                Keyboard.current == null || Mouse.current == null ||
                definition == null)
            {
                ApplyLockedMovement();
                return;
            }
            if (abilityInputLocked)
            {
                ApplyLockedMovement();
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                var sensitivity = RoboClashSession.MouseSensitivity;
                if (float.IsNaN(sensitivity) || float.IsInfinity(sensitivity))
                    sensitivity = Mathf.Clamp(mouseSensitivity, 0.03f, 0.4f);
                var look = Mouse.current.delta.ReadValue() * sensitivity;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, -20f, 45f);
            }

            var input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;

            var speedMultiplier = abilities != null ? abilities.GetMoveSpeedMultiplier() : 1f;
            var moveInput = new Vector3(input.x, 0f, input.y);
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();
            var desired = Quaternion.Euler(0f, yaw, 0f) * moveInput *
                          definition.stats.moveSpeed * speedMultiplier;
            ApplyMovement(desired, Keyboard.current.spaceKey.wasPressedThisFrame);

            var targetYaw = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = targetYaw;

            if (definition.aimPivot != null)
                definition.aimPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        }

        private void LateUpdate()
        {
            if (!playerControlled || playerCamera == null)
                return;

            if (cameraMode != RoboClashSession.CameraMode)
                SetCameraMode(RoboClashSession.CameraMode);

            if (cameraState == RoboClashCameraState.Intro ||
                cameraState == RoboClashCameraState.Respawning)
            {
                UpdateCameraTransition();
                return;
            }

            if (cameraState != RoboClashCameraState.FirstPerson &&
                cameraState != RoboClashCameraState.ThirdPerson)
                return;

            UpdateSelectedCameraPose();
        }

        private void BeginCameraTransition(RoboClashCameraState transitionState, float duration)
        {
            if (!playerControlled || playerCamera == null || firstPersonCameraAnchor == null)
            {
                cameraState = playerControlled && playerCamera != null
                    ? cameraMode == RoboClashCameraMode.FirstPerson
                        ? RoboClashCameraState.FirstPerson
                        : RoboClashCameraState.ThirdPerson
                    : RoboClashCameraState.Disabled;
                cameraTransitionInputLocked = false;
                SetFirstPersonPresentation(cameraMode == RoboClashCameraMode.FirstPerson);
                return;
            }

            cameraState = transitionState;
            cameraTransitionDuration = Mathf.Max(0.1f, duration);
            cameraTransitionElapsed = 0f;
            cameraTransitionInputLocked = true;
            SetFirstPersonPresentation(false);
            if (introCameraAnchor != null)
                playerCamera.transform.SetPositionAndRotation(introCameraAnchor.position,
                    introCameraAnchor.rotation);
            else
                EnterSelectedCameraMode(true);
        }

        private void UpdateCameraTransition()
        {
            if (introCameraAnchor == null || firstPersonCameraAnchor == null)
            {
                EnterSelectedCameraMode(true);
                return;
            }

            cameraTransitionElapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(cameraTransitionElapsed / cameraTransitionDuration);
            var eased = t * t * (3f - 2f * t);
            var focus = GetCameraFocus();
            var targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            var targetPosition = cameraMode == RoboClashCameraMode.FirstPerson &&
                                 firstPersonCameraAnchor != null
                ? firstPersonCameraAnchor.position
                : GetThirdPersonCameraPosition(focus, targetRotation);
            targetPosition = ResolveCameraPosition(focus, targetPosition, out _);
            var cameraPosition = Vector3.Lerp(introCameraAnchor.position, targetPosition, eased);
            playerCamera.transform.SetPositionAndRotation(cameraPosition,
                Quaternion.Slerp(introCameraAnchor.rotation, targetRotation, eased));

            SetFirstPersonPresentation(
                cameraMode == RoboClashCameraMode.FirstPerson && t >= 0.82f);
            if (t >= 1f)
                EnterSelectedCameraMode(true);
        }

        private void EnterSelectedCameraMode(bool captureCursor = false)
        {
            cameraState = cameraMode == RoboClashCameraMode.FirstPerson
                ? RoboClashCameraState.FirstPerson
                : RoboClashCameraState.ThirdPerson;
            cameraTransitionInputLocked = false;
            UpdateSelectedCameraPose();

            if (captureCursor && playerControlled &&
                string.Equals(
                    gameObject.scene.path,
                    "Assets/Scenes/RoboClashGameplay.unity",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void UpdateSelectedCameraPose()
        {
            UpdateCameraFeedback();
            var rotation = GetCameraRotation();
            if (cameraMode == RoboClashCameraMode.FirstPerson && firstPersonCameraAnchor != null)
            {
                SetFirstPersonPresentation(true);
                playerCamera.transform.SetPositionAndRotation(firstPersonCameraAnchor.position, rotation);
                return;
            }

            SetFirstPersonPresentation(false);
            var focus = GetCameraFocus();
            var position = GetThirdPersonCameraPosition(focus, rotation);
            position = ResolveCameraPosition(focus, position, out _);
            playerCamera.transform.SetPositionAndRotation(position, rotation);
        }

        private Vector3 GetCameraFocus()
        {
            return transform.position + Vector3.up * 1.45f;
        }

        private Vector3 GetThirdPersonCameraPosition(Vector3 focus, Quaternion rotation)
        {
            return focus - rotation * Vector3.forward * thirdPersonDistance +
                   Vector3.up * thirdPersonHeight + transform.right * 0.35f;
        }

        private void CacheFirstPersonRenderers()
        {
            var modelRoot = definition != null ? definition.modelRoot : null;
            firstPersonRenderers = modelRoot != null
                ? modelRoot.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];
            firstPersonRendererDefaults = new bool[firstPersonRenderers.Length];
            for (var i = 0; i < firstPersonRenderers.Length; i++)
                firstPersonRendererDefaults[i] = firstPersonRenderers[i] != null &&
                                                 firstPersonRenderers[i].enabled;
            firstPersonPresentationActive = false;
        }

        private void SetFirstPersonPresentation(bool active)
        {
            if (firstPersonPresentationActive == active)
                return;

            for (var i = 0; i < firstPersonRenderers.Length; i++)
            {
                var renderer = firstPersonRenderers[i];
                if (renderer == null)
                    continue;

                var weaponMount = definition != null ? definition.weaponMount : null;
                var isWeaponRenderer = weaponMount != null &&
                    (renderer.transform == weaponMount || renderer.transform.IsChildOf(weaponMount));
                renderer.enabled = active
                    ? isWeaponRenderer && firstPersonRendererDefaults[i]
                    : firstPersonRendererDefaults[i];
            }

            firstPersonPresentationActive = active;
        }

        private Vector3 ResolveCameraPosition(Vector3 focus, Vector3 desiredPosition, out bool obstructed)
        {
            obstructed = false;
            var offset = desiredPosition - focus;
            var distance = offset.magnitude;
            if (distance <= 0.001f)
                return desiredPosition;

            var direction = offset / distance;
            var hitCount = Physics.SphereCastNonAlloc(focus, cameraCollisionRadius, direction,
                cameraObstructionHits, distance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var nearestDistance = float.PositiveInfinity;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = cameraObstructionHits[i];
                if (hit.collider == null || hit.transform.root == transform.root ||
                    hit.distance >= nearestDistance)
                    continue;

                nearestDistance = hit.distance;
            }

            if (nearestDistance >= float.PositiveInfinity)
                return desiredPosition;

            obstructed = true;
            var safeDistance = Mathf.Clamp(nearestDistance - cameraCollisionPadding, 0.02f, distance);
            return focus + direction * safeDistance;
        }

        private void ApplyMovement(Vector3 desiredVelocity, bool jumpPressed)
        {
            var deltaTime = Time.deltaTime;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity,
                Mathf.Max(0f, definition.stats.acceleration) * deltaTime);

            if (characterController.isGrounded)
            {
                verticalVelocity = -1f;
                if (jumpPressed)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            }
            verticalVelocity += Physics.gravity.y * deltaTime;

            platformContact = null;
            var platformMotion = currentPlatform != null && characterController.isGrounded
                ? currentPlatform.Velocity * deltaTime
                : Vector3.zero;
            characterController.Move((horizontalVelocity + Vector3.up * verticalVelocity) *
                                     deltaTime + platformMotion);
            currentPlatform = platformContact;

            combat?.NotifyMovement(HorizontalSpeed, definition.stats.moveSpeed);
            animator?.SetMovement(horizontalVelocity, HorizontalSpeed, definition.stats.moveSpeed,
                characterController.isGrounded);
            UpdateFootsteps();
        }

        private void HaltHorizontalMotion()
        {
            horizontalVelocity = Vector3.zero;
            currentPlatform = null;
            platformContact = null;
            animator?.SetMovement(Vector3.zero, 0f,
                definition != null ? definition.stats.moveSpeed : 0f,
                characterController != null && characterController.isGrounded);
        }

        private void ApplyLockedMovement()
        {
            if (characterController == null || !characterController.enabled || definition == null)
            {
                HaltHorizontalMotion();
                return;
            }

            horizontalVelocity = Vector3.zero;
            ApplyMovement(Vector3.zero, false);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
                platformContact = hit.collider.GetComponentInParent<ArenaMovingPlatform>();
        }

        private void UpdateFootsteps()
        {
            if (!characterController.isGrounded || HorizontalSpeed <= 0.6f || Time.time < nextFootstepTime)
                return;

            audioPlayer?.Play(RoboClashAudioEvent.Footstep,
                definition.robotType == RoboClashRobotType.Shadow ? 0.35f : 1f);
            nextFootstepTime = Time.time + Mathf.Lerp(0.48f, 0.24f,
                Mathf.Clamp01(HorizontalSpeed / Mathf.Max(definition.stats.moveSpeed, 0.1f)));
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashRobotVFX.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    public sealed class RoboClashRobotVFX : MonoBehaviour
    {
        private static Material fallbackShimmerMaterial;

        [SerializeField] private Renderer shieldRenderer;
        [SerializeField] private LineRenderer repairBeam;
        [SerializeField] private Light muzzleLight;

        private RoboClashRobotDefinition definition;
        private RoboClashRobotAudio audioPlayer;
        private Transform beamTarget;
        private float muzzleLightIntensity;
        private bool overdrive;

        private void Awake()
        {
            definition = GetComponent<RoboClashRobotDefinition>();
            audioPlayer = GetComponent<RoboClashRobotAudio>();
            ConfigureVisuals();
        }

        public void ConfigureVisuals()
        {
            if (definition == null)
                definition = GetComponent<RoboClashRobotDefinition>();
            if (audioPlayer == null)
                audioPlayer = GetComponent<RoboClashRobotAudio>();
            CreateShieldField();
            CreateRepairBeam();
            CreateMuzzleLight();
        }

        public void PlayMuzzle(Vector3 position, Vector3 direction, bool heavy)
        {
            var accent = RoboClashCombatVFX.GetRobotAccent(definition);
            RoboClashCombatVFX.PlayMuzzle(position, direction, accent, heavy ? 0.75f : 0.42f);
            if (muzzleLight != null)
            {
                muzzleLight.transform.position = position;
                muzzleLight.transform.forward = direction;
                muzzleLight.color = accent;
                muzzleLightIntensity = heavy ? 8f : 4.5f;
                muzzleLight.intensity = muzzleLightIntensity;
                muzzleLight.enabled = true;
            }
        }

        public void PlayAbility()
        {
            var point = transform.position + Vector3.up * 1.1f;
            RoboClashCombatVFX.PlayBurst(point, transform.forward, definition.emissionColor, 0.8f, false);
            audioPlayer?.Play(GetAbilityAudioEvent());
        }

        public void PlayUltimate()
        {
            var point = transform.position + Vector3.up * 1.1f;
            RoboClashCombatVFX.PlayPulse(point, definition.emissionColor, 7f);
            audioPlayer?.Play(GetUltimateAudioEvent());
        }

        public void PlayDash(Vector3 direction)
        {
            RoboClashCombatVFX.PlayDash(transform.position + Vector3.up, direction, definition.emissionColor);
            audioPlayer?.Play(RoboClashAudioEvent.Dash);
        }

        public void PlayDash(Vector3 start, Vector3 end, Vector3 direction)
        {
            for (var i = 0; i <= 2; i++)
            {
                var point = Vector3.Lerp(start, end, i / 2f) + Vector3.up * 0.85f;
                RoboClashCombatVFX.PlayDash(point, direction, definition.emissionColor);
            }
            audioPlayer?.Play(RoboClashAudioEvent.Dash);
        }

        public void PlayRespawn()
        {
            RoboClashCombatVFX.PlayPulse(transform.position + Vector3.up, definition.emissionColor, 2.2f);
            audioPlayer?.Play(RoboClashAudioEvent.RepairPulse);
        }

        public void PlayEmpDisruption()
        {
            RoboClashCombatVFX.PlayBurst(transform.position + Vector3.up, Vector3.up,
                definition.emissionColor, 0.6f, false);
        }

        public void SetShieldField(bool active)
        {
            if (shieldRenderer == null)
                return;

            shieldRenderer.enabled = active;
            if (active)
            {
                RoboClashCombatVFX.PlayBurst(transform.position + Vector3.up,
                    Vector3.up, definition.emissionColor, 0.7f, true);
            }
        }

        public void SetOverdrive(bool active)
        {
            overdrive = active;
            if (shieldRenderer != null)
                shieldRenderer.enabled = active;
        }

        public void SetRepairTarget(Transform target)
        {
            beamTarget = target;
            if (repairBeam != null)
                repairBeam.enabled = target != null;
            if (target != null)
                audioPlayer?.Play(RoboClashAudioEvent.DroneRepair);
        }

        public void PlayImpact(Vector3 position, Vector3 normal, bool heavy)
        {
            RoboClashCombatVFX.PlayBurst(position, normal, definition.emissionColor,
                heavy ? 0.8f : 0.45f, heavy);
            audioPlayer?.Play(RoboClashAudioEvent.ProjectileImpact);
        }

        public void StopAllEffects()
        {
            SetShieldField(false);
            SetOverdrive(false);
            SetRepairTarget(null);
            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
                muzzleLightIntensity = 0f;
            }
        }

        private RoboClashAudioEvent GetAbilityAudioEvent()
        {
            switch (definition.robotType)
            {
                case RoboClashRobotType.Titan: return RoboClashAudioEvent.ShieldActivation;
                case RoboClashRobotType.Shadow: return RoboClashAudioEvent.Cloak;
                case RoboClashRobotType.Spark: return RoboClashAudioEvent.RepairPulse;
                default: return RoboClashAudioEvent.Dash;
            }
        }

        private RoboClashAudioEvent GetUltimateAudioEvent()
        {
            switch (definition.robotType)
            {
                case RoboClashRobotType.Titan:
                case RoboClashRobotType.Volt: return RoboClashAudioEvent.Charge;
                case RoboClashRobotType.Spark: return RoboClashAudioEvent.Emp;
                default: return RoboClashAudioEvent.Ultimate;
            }
        }

        private void Update()
        {
            if (muzzleLight != null && muzzleLight.enabled)
            {
                muzzleLightIntensity = Mathf.MoveTowards(muzzleLightIntensity, 0f, 55f * Time.deltaTime);
                muzzleLight.intensity = muzzleLightIntensity;
                if (muzzleLightIntensity <= 0.01f)
                    muzzleLight.enabled = false;
            }

            if (shieldRenderer != null && shieldRenderer.enabled)
            {
                var pulse = overdrive ? 1f + Mathf.Sin(Time.time * 7f) * 0.035f : 1f;
                shieldRenderer.transform.localScale = Vector3.one * (overdrive ? 1.58f : 1.48f) * pulse;
            }

            if (repairBeam == null || !repairBeam.enabled || beamTarget == null ||
                definition.repairDrone == null)
                return;

            repairBeam.SetPosition(0, definition.repairDrone.position);
            repairBeam.SetPosition(1, beamTarget.position + Vector3.up);
        }

        private void CreateShieldField()
        {
            if (shieldRenderer != null || definition == null || definition.modelRoot == null)
                return;

            var field = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            field.name = "Energy Field Visual";
            field.transform.SetParent(definition.modelRoot, false);
            field.transform.localPosition = Vector3.up * 0.96f;
            field.transform.localScale = Vector3.one * 1.48f;
            var collider = field.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            shieldRenderer = field.GetComponent<Renderer>();
            shieldRenderer.sharedMaterial = definition.shimmerMaterial != null
                ? definition.shimmerMaterial
                : GetFallbackShimmer();
            var properties = new MaterialPropertyBlock();
            var fieldColor = definition.emissionColor;
            fieldColor.a = 0.14f;
            properties.SetColor("_BaseColor", fieldColor);
            properties.SetColor("_Color", fieldColor);
            shieldRenderer.SetPropertyBlock(properties);
            shieldRenderer.enabled = false;
            shieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shieldRenderer.receiveShadows = false;
        }

        private void CreateRepairBeam()
        {
            if (repairBeam != null)
                return;
            var beam = new GameObject("Repair Beam");
            beam.transform.SetParent(transform, false);
            repairBeam = beam.AddComponent<LineRenderer>();
            repairBeam.positionCount = 2;
            repairBeam.startWidth = 0.045f;
            repairBeam.endWidth = 0.015f;
            repairBeam.useWorldSpace = true;
            repairBeam.enabled = false;
            repairBeam.sharedMaterial = definition != null && definition.emissionMaterial != null
                ? definition.emissionMaterial
                : RoboClashCombatVFX.GetParticleMaterial();
            repairBeam.startColor = definition != null ? definition.emissionColor : Color.yellow;
            repairBeam.endColor = Color.white;
        }

        private void CreateMuzzleLight()
        {
            if (muzzleLight != null)
                return;
            var lightObject = new GameObject("Muzzle Light Pulse");
            lightObject.transform.SetParent(transform, false);
            muzzleLight = lightObject.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 5f;
            muzzleLight.intensity = 0f;
            muzzleLight.enabled = false;
        }

        private static Material GetFallbackShimmer()
        {
            if (fallbackShimmerMaterial != null)
                return fallbackShimmerMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                return null;

            fallbackShimmerMaterial = new Material(shader)
            {
                name = "RoboClash Runtime Shimmer",
                hideFlags = HideFlags.DontSave
            };
            if (fallbackShimmerMaterial.HasProperty("_Surface"))
                fallbackShimmerMaterial.SetFloat("_Surface", 1f);
            if (fallbackShimmerMaterial.HasProperty("_SrcBlend"))
                fallbackShimmerMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (fallbackShimmerMaterial.HasProperty("_DstBlend"))
                fallbackShimmerMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (fallbackShimmerMaterial.HasProperty("_ZWrite"))
                fallbackShimmerMaterial.SetFloat("_ZWrite", 0f);
            if (fallbackShimmerMaterial.HasProperty("_BaseColor"))
                fallbackShimmerMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.16f));
            if (fallbackShimmerMaterial.HasProperty("_Color"))
                fallbackShimmerMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0.16f));
            fallbackShimmerMaterial.SetOverrideTag("RenderType", "Transparent");
            fallbackShimmerMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            fallbackShimmerMaterial.EnableKeyword("_ALPHABLEND_ON");
            fallbackShimmerMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return fallbackShimmerMaterial;
        }

        private void OnDisable()
        {
            StopAllEffects();
        }
    }
}
````

### `Assets/Scripts/RoboClash/RoboClashSaveService.cs`

````csharp
using System;
using System.IO;
using UnityEngine;

namespace RoboClash
{
    [Serializable]
    public sealed class RoboClashPlayerSettings
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        public bool fullscreen;
        public int resolutionWidth;
        public int resolutionHeight;
        public int qualityLevel;
        public bool vSync;
        public int targetFrameRate = 60;
    }

    [Serializable]
    public sealed class RoboClashCareerData
    {
        public int totalMatches;
        public int totalWins;
        public int totalLosses;
        public int totalDraws;
        public int totalEliminations;
        public float totalDamageDealt;
        public int highestSurvivalWave;
        public float totalPlayTimeSeconds;
        public string preferredRobotId = "VOLT";
        public int level = 1;
        public int currentXP;
        public int lifetimeXP;
    }

    [Serializable]
    public sealed class RoboClashSaveData
    {
        public int saveVersion = 1;
        public RoboClashPlayerSettings settings = new RoboClashPlayerSettings();
        public RoboClashCareerData career = new RoboClashCareerData();
    }

    [Serializable]
    public sealed class RoboClashProgressionRewards
    {
        [Min(0)] public int matchCompletionXP = 100;
        [Min(0)] public int victoryBonusXP = 50;
        [Min(0)] public int eliminationXP = 20;
        [Min(0)] public int survivalWaveXP = 10;
    }

    public static class RoboClashSaveService
    {
        private const int CurrentSaveVersion = 1;
        private const int MaximumLifetimeXP = 1000000000;
        private const string MasterVolumeKey = "RoboClash.MasterVolume";
        private const string FullscreenKey = "RoboClash.Fullscreen";
        private const string ResolutionKey = "RoboClash.Resolution";
        private static RoboClashSaveData current;
        private static RoboClashPlayerSettings defaultSettings;
        private static bool isLoaded;
        private static bool preserveExistingSave;

        public static int LastMatchXP { get; private set; }
        public static event Action<float> MusicVolumeChanged;

        public static float MusicVolume
        {
            get
            {
                EnsureLoaded();
                return current.settings.musicVolume;
            }
        }

        public static RoboClashSaveData Current
        {
            get
            {
                EnsureLoaded();
                return current;
            }
        }

        public static string SavePath => Path.Combine(Application.persistentDataPath, "roboclash-save.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            current = null;
            defaultSettings = null;
            isLoaded = false;
            preserveExistingSave = false;
            LastMatchXP = 0;
            MusicVolumeChanged = null;
            Application.quitting -= SaveOnQuit;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            EnsureLoaded();
            Application.quitting -= SaveOnQuit;
            Application.quitting += SaveOnQuit;
        }

        public static void Load()
        {
            if (isLoaded)
                return;

            defaultSettings = CaptureDefaultSettings();
            isLoaded = true;
            preserveExistingSave = false;
            if (!File.Exists(SavePath))
            {
                current = CreateDefaultData();
                MigrateLegacySettings(current.settings);
                ApplySettings();
                Save();
                return;
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var loaded = JsonUtility.FromJson<RoboClashSaveData>(json);
                if (loaded == null || loaded.saveVersion != CurrentSaveVersion)
                {
                    preserveExistingSave = true;
                    Debug.LogWarning(
                        $"RoboClash save version is unsupported or invalid. Defaults are in use; the existing save was preserved. Path: {SavePath}");
                    current = CreateDefaultData();
                    ApplySettings();
                    return;
                }

                current = loaded;
                Validate(current);
                ApplySettings();
            }
            catch (IOException exception)
            {
                preserveExistingSave = true;
                Debug.LogError($"Could not read the RoboClash save file at '{SavePath}': {exception.Message}");
                current = CreateDefaultData();
                ApplySettings();
            }
            catch (UnauthorizedAccessException exception)
            {
                preserveExistingSave = true;
                Debug.LogError($"Access to the RoboClash save file was denied at '{SavePath}': {exception.Message}");
                current = CreateDefaultData();
                ApplySettings();
            }
            catch (ArgumentException exception)
            {
                preserveExistingSave = true;
                Debug.LogError($"The RoboClash save file at '{SavePath}' is invalid: {exception.Message}");
                current = CreateDefaultData();
                ApplySettings();
            }
        }

        public static bool Save()
        {
            EnsureLoaded();
            Validate(current);
            if (preserveExistingSave)
            {
                Debug.LogWarning(
                    $"RoboClash data was not written because an existing save could not be safely loaded. Path: {SavePath}");
                return false;
            }

            var directory = Path.GetDirectoryName(SavePath);
            var temporaryPath = SavePath + ".tmp";

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(current, true));
                if (File.Exists(SavePath))
                    File.Replace(temporaryPath, SavePath, null);
                else
                    File.Move(temporaryPath, SavePath);
                return true;
            }
            catch (IOException exception)
            {
                Debug.LogError($"Could not save RoboClash data to '{SavePath}': {exception.Message}");
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError($"Access to the RoboClash save location was denied at '{SavePath}': {exception.Message}");
                return false;
            }
            catch (PlatformNotSupportedException exception)
            {
                Debug.LogError($"Atomic save replacement is unavailable at '{SavePath}': {exception.Message}");
                return false;
            }
        }

        public static void SetMasterVolume(float value)
        {
            EnsureLoaded();
            current.settings.masterVolume = ClampVolume(value, 1f);
            ApplyAudioSettings();
        }

        public static void SetMusicVolume(float value)
        {
            EnsureLoaded();
            current.settings.musicVolume = ClampVolume(value, 0.8f);
            MusicVolumeChanged?.Invoke(current.settings.musicVolume);
        }

        public static void SetSfxVolume(float value)
        {
            EnsureLoaded();
            current.settings.sfxVolume = ClampVolume(value, 1f);
            ApplyAudioSettings();
        }

        public static void SetFullscreen(bool value)
        {
            EnsureLoaded();
            current.settings.fullscreen = value;
            Screen.fullScreen = value;
        }

        public static void SetResolution(int width, int height)
        {
            EnsureLoaded();
            if (width <= 0 || height <= 0 || !IsSupportedResolution(width, height))
            {
                Debug.LogWarning($"The requested RoboClash resolution {width} x {height} is not supported.");
                return;
            }

            current.settings.resolutionWidth = width;
            current.settings.resolutionHeight = height;
            Screen.SetResolution(width, height, current.settings.fullscreen);
        }

        public static void SetQualityLevel(int index)
        {
            EnsureLoaded();
            if (QualitySettings.names.Length == 0)
            {
                Debug.LogError("RoboClash cannot apply graphics quality because the project has no quality levels.");
                return;
            }

            current.settings.qualityLevel = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(current.settings.qualityLevel, true);
        }

        public static void SetVSync(bool enabled)
        {
            EnsureLoaded();
            current.settings.vSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            ApplyFrameRate();
        }

        public static void SetTargetFrameRate(int frameRate)
        {
            EnsureLoaded();
            current.settings.targetFrameRate = IsSupportedFrameRate(frameRate) ? frameRate : 60;
            ApplyFrameRate();
        }

        public static void ApplySettings()
        {
            EnsureLoaded();
            Validate(current);
            ApplyAudioSettings();
            MusicVolumeChanged?.Invoke(current.settings.musicVolume);
            var settings = current.settings;
            if (settings.resolutionWidth > 0 && settings.resolutionHeight > 0 &&
                IsSupportedResolution(settings.resolutionWidth, settings.resolutionHeight))
            {
                Screen.SetResolution(
                    settings.resolutionWidth, settings.resolutionHeight, settings.fullscreen);
            }
            else
            {
                Screen.fullScreen = settings.fullscreen;
            }

            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(settings.qualityLevel, true);
            QualitySettings.vSyncCount = settings.vSync ? 1 : 0;
            ApplyFrameRate();
        }

        public static void ResetSettings()
        {
            EnsureLoaded();
            current.settings = CreateDefaultData().settings;
            ApplySettings();
            Save();
        }

        public static void ResetCareer()
        {
            EnsureLoaded();
            var preferredRobot = current.career.preferredRobotId;
            current.career = new RoboClashCareerData { preferredRobotId = preferredRobot };
            Save();
        }

        public static void SetPreferredRobot(RoboClashRobotType robot)
        {
            EnsureLoaded();
            current.career.preferredRobotId = robot.ToString().ToUpperInvariant();
        }

        public static int GetXPRequiredForNextLevel(int level)
        {
            return Mathf.Min(100 + Mathf.Max(0, level - 1) * 50, 1000000);
        }

        public static int AwardXPForMatch(
            RoboClashMatchStatistics statistics, RoboClashProgressionRewards rewards = null)
        {
            rewards = rewards ?? new RoboClashProgressionRewards();
            long award = Mathf.Max(0, rewards.matchCompletionXP) +
                         (long)Mathf.Max(0, statistics.PlayerEliminations) *
                         Mathf.Max(0, rewards.eliminationXP);
            if (statistics.FinalResult == RoboClashMatchState.Victory)
                award += Mathf.Max(0, rewards.victoryBonusXP);
            if (statistics.GameMode == RoboClashGameMode.Survival)
                award += (long)Mathf.Max(0, statistics.HighestWaveCompleted) *
                         Mathf.Max(0, rewards.survivalWaveXP);
            return (int)Math.Min(MaximumLifetimeXP, award);
        }

        public static void RecordMatch(RoboClashMatchStatistics statistics, RoboClashRobotType playerRobot,
            RoboClashProgressionRewards rewards)
        {
            EnsureLoaded();
            var career = current.career;
            career.totalMatches++;
            career.totalEliminations = (int)Math.Min(
                int.MaxValue,
                (long)career.totalEliminations + Mathf.Max(0, statistics.PlayerEliminations));
            career.totalDamageDealt += Mathf.Max(0f, statistics.PlayerDamageDealt);
            career.totalPlayTimeSeconds += Mathf.Max(0f, statistics.MatchDurationSeconds);
            if (statistics.GameMode == RoboClashGameMode.Survival)
                career.highestSurvivalWave = Mathf.Max(career.highestSurvivalWave, statistics.HighestWaveReached);

            switch (statistics.FinalResult)
            {
                case RoboClashMatchState.Victory:
                    career.totalWins++;
                    break;
                case RoboClashMatchState.Defeat:
                    career.totalLosses++;
                    break;
                case RoboClashMatchState.Draw:
                    career.totalDraws++;
                    break;
            }

            career.preferredRobotId = playerRobot.ToString().ToUpperInvariant();
            LastMatchXP = AwardXPForMatch(statistics, rewards);
            AddXP(career, LastMatchXP);
            Save();
        }

        private static void AddXP(RoboClashCareerData career, int amount)
        {
            career.lifetimeXP = Mathf.Min(MaximumLifetimeXP, career.lifetimeXP + Mathf.Max(0, amount));
            var remainingXP = career.lifetimeXP;
            var level = 1;
            while (remainingXP >= GetXPRequiredForNextLevel(level))
            {
                remainingXP -= GetXPRequiredForNextLevel(level);
                level++;
            }

            career.level = level;
            career.currentXP = remainingXP;
        }

        private static void EnsureLoaded()
        {
            if (!isLoaded)
                Load();
        }

        private static void SaveOnQuit()
        {
            Save();
        }

        private static RoboClashSaveData CreateDefaultData()
        {
            return new RoboClashSaveData
            {
                settings = CopySettings(defaultSettings),
                career = new RoboClashCareerData()
            };
        }

        private static RoboClashPlayerSettings CaptureDefaultSettings()
        {
            return new RoboClashPlayerSettings
            {
                masterVolume = 1f,
                musicVolume = 0.8f,
                sfxVolume = 1f,
                fullscreen = Screen.fullScreen,
                resolutionWidth = Screen.width,
                resolutionHeight = Screen.height,
                qualityLevel = QualitySettings.names.Length > 0
                    ? Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)
                    : 0,
                vSync = QualitySettings.vSyncCount > 0,
                targetFrameRate = 60
            };
        }

        private static RoboClashPlayerSettings CopySettings(RoboClashPlayerSettings settings)
        {
            return new RoboClashPlayerSettings
            {
                masterVolume = settings.masterVolume,
                musicVolume = settings.musicVolume,
                sfxVolume = settings.sfxVolume,
                fullscreen = settings.fullscreen,
                resolutionWidth = settings.resolutionWidth,
                resolutionHeight = settings.resolutionHeight,
                qualityLevel = settings.qualityLevel,
                vSync = settings.vSync,
                targetFrameRate = settings.targetFrameRate
            };
        }

        private static void MigrateLegacySettings(RoboClashPlayerSettings settings)
        {
            settings.masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            settings.fullscreen = PlayerPrefs.GetInt(FullscreenKey, settings.fullscreen ? 1 : 0) != 0;
            var resolutions = Screen.resolutions;
            var savedIndex = PlayerPrefs.GetInt(ResolutionKey, -1);
            if (savedIndex < 0 || resolutions.Length == 0)
                return;

            var seen = 0;
            var previousWidth = -1;
            var previousHeight = -1;
            foreach (var resolution in resolutions)
            {
                if (resolution.width == previousWidth && resolution.height == previousHeight)
                    continue;
                previousWidth = resolution.width;
                previousHeight = resolution.height;
                if (seen++ != savedIndex)
                    continue;

                settings.resolutionWidth = resolution.width;
                settings.resolutionHeight = resolution.height;
                return;
            }
        }

        private static void Validate(RoboClashSaveData data)
        {
            if (data.settings == null)
                data.settings = CreateDefaultData().settings;
            if (data.career == null)
                data.career = new RoboClashCareerData();

            var defaults = CreateDefaultData();
            var settings = data.settings;
            settings.masterVolume = ClampVolume(settings.masterVolume, defaults.settings.masterVolume);
            settings.musicVolume = ClampVolume(settings.musicVolume, defaults.settings.musicVolume);
            settings.sfxVolume = ClampVolume(settings.sfxVolume, defaults.settings.sfxVolume);
            if (settings.resolutionWidth <= 0 || settings.resolutionHeight <= 0 ||
                !IsSupportedResolution(settings.resolutionWidth, settings.resolutionHeight))
            {
                settings.resolutionWidth = defaults.settings.resolutionWidth;
                settings.resolutionHeight = defaults.settings.resolutionHeight;
            }

            settings.qualityLevel = QualitySettings.names.Length > 0
                ? Mathf.Clamp(settings.qualityLevel, 0, QualitySettings.names.Length - 1)
                : 0;
            if (!IsSupportedFrameRate(settings.targetFrameRate))
                settings.targetFrameRate = 60;

            var career = data.career;
            career.totalMatches = Mathf.Max(0, career.totalMatches);
            career.totalWins = Mathf.Clamp(career.totalWins, 0, career.totalMatches);
            career.totalLosses = Mathf.Clamp(career.totalLosses, 0, career.totalMatches - career.totalWins);
            career.totalDraws = Mathf.Clamp(career.totalDraws, 0,
                career.totalMatches - career.totalWins - career.totalLosses);
            career.totalEliminations = Mathf.Max(0, career.totalEliminations);
            career.totalDamageDealt = IsFinite(career.totalDamageDealt)
                ? Mathf.Max(0f, career.totalDamageDealt)
                : 0f;
            career.highestSurvivalWave = Mathf.Max(0, career.highestSurvivalWave);
            career.totalPlayTimeSeconds = IsFinite(career.totalPlayTimeSeconds)
                ? Mathf.Max(0f, career.totalPlayTimeSeconds)
                : 0f;
            if (!Enum.TryParse(career.preferredRobotId, true, out RoboClashRobotType preferredRobot) ||
                !Enum.IsDefined(typeof(RoboClashRobotType), preferredRobot))
                career.preferredRobotId = "VOLT";
            career.lifetimeXP = Mathf.Clamp(career.lifetimeXP, 0, MaximumLifetimeXP);
            AddXP(career, 0);
        }

        private static void ApplyAudioSettings()
        {
            AudioListener.volume = current.settings.masterVolume;
            RoboClashRobotAudio.ApplySfxVolume(current.settings.sfxVolume);
        }

        private static void ApplyFrameRate()
        {
            Application.targetFrameRate = current.settings.vSync
                ? -1
                : current.settings.targetFrameRate;
        }

        private static bool IsSupportedResolution(int width, int height)
        {
            foreach (var resolution in Screen.resolutions)
            {
                if (resolution.width == width && resolution.height == height)
                    return true;
            }

            return width == Screen.width && height == Screen.height;
        }

        private static bool IsSupportedFrameRate(int frameRate)
        {
            return frameRate == -1 || frameRate == 30 || frameRate == 60 ||
                   frameRate == 120 || frameRate == 144;
        }

        private static float ClampVolume(float value, float fallback)
        {
            return IsFinite(value) ? Mathf.Clamp01(value) : fallback;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
````

### `Assets/Scripts/RoboClash/RobotArenaController.cs`

````csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboClash
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class RobotArenaController : ArenaDamageable
    {
        [SerializeField, Min(0f)] private float moveSpeed = 8f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.8f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform muzzle;
        [SerializeField] private ArenaSpawnZone spawnZone;
        [SerializeField] private float weaponDamage = 20f;
        [SerializeField] private float projectileSpeed = 32f;
        [SerializeField] private float fireInterval = 0.4f;

        private CharacterController characterController;
        private float verticalVelocity;
        private float yaw;
        private float pitch = 12f;
        private float nextFireTime;
        private bool dead;
        private ArenaMovingPlatform currentPlatform;
        private ArenaMovingPlatform platformContactThisMove;
        private RoboClashRobotDefinition robotDefinition;
        private RoboClashRobotMovement robotMovement;
        private RoboClashRobotCombat robotCombat;
        private RoboClashRobotAbilities robotAbilities;
        private RoboClashRobotAnimator robotAnimator;
        private RoboClashRobotAudio robotAudio;
        private RoboClashRobotVFX robotVfx;
        private int respawnsRemaining;
        private bool unlimitedMatchRespawns;
        private Coroutine respawnCoroutine;
        private bool matchRespawnsConfigured;
        private bool hasMatchRespawnLocation;
        private Vector3 matchRespawnPosition;
        private Quaternion matchRespawnRotation = Quaternion.identity;

        public Camera PlayerCamera => playerCamera;
        public ArenaSpawnZone SpawnZone => spawnZone;
        public bool IsRespawning { get; private set; }
        public bool HasPlayerControl => robotMovement != null
            ? robotMovement.PlayerControlled
            : playerCamera != null;

        public event Action HitConfirmed;
        public event Action<RobotArenaController> Respawned;

        protected override void Awake()
        {
            base.Awake();
            characterController = GetComponent<CharacterController>();
            robotDefinition = GetComponent<RoboClashRobotDefinition>();
            robotMovement = GetComponent<RoboClashRobotMovement>();
            robotCombat = GetComponent<RoboClashRobotCombat>();
            robotAbilities = GetComponent<RoboClashRobotAbilities>();
            robotAnimator = GetComponent<RoboClashRobotAnimator>();
            robotAudio = GetComponent<RoboClashRobotAudio>();
            robotVfx = GetComponent<RoboClashRobotVFX>();
            if (robotDefinition != null)
                ConfigureVitals(robotDefinition.stats.maxHealth, robotDefinition.stats.maxShield);

            if (playerCamera == null && robotMovement == null)
                playerCamera = Camera.main;

            yaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            if (IsGameplayScene() && playerCamera != null && robotMovement == null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void Configure(Camera camera, Transform weaponMuzzle, ArenaSpawnZone startingZone)
        {
            if (robotDefinition == null)
                robotDefinition = GetComponent<RoboClashRobotDefinition>();
            if (robotMovement == null)
                robotMovement = GetComponent<RoboClashRobotMovement>();
            if (robotCombat == null)
                robotCombat = GetComponent<RoboClashRobotCombat>();

            playerCamera = camera;
            muzzle = weaponMuzzle != null ? weaponMuzzle : robotDefinition != null ? robotDefinition.muzzle : null;
            spawnZone = startingZone;
            if (robotMovement != null)
                robotMovement.ConfigurePlayer(camera, camera != null);
            if (robotCombat != null)
                robotCombat.ConfigurePlayer(camera != null);
            robotAudio?.SetLocalPlayer(camera != null);
        }

        public void DisablePlayerControl()
        {
            playerCamera = null;
            if (robotMovement != null)
                robotMovement.ConfigurePlayer(null, false);
            if (robotCombat != null)
                robotCombat.ConfigurePlayer(false);
        }

        public void ConfigureMatchRespawns(int maximumRespawns)
        {
            matchRespawnsConfigured = true;
            unlimitedMatchRespawns = false;
            respawnsRemaining = Mathf.Max(0, maximumRespawns);
        }

        public void ConfigureUnlimitedMatchRespawns()
        {
            matchRespawnsConfigured = true;
            unlimitedMatchRespawns = true;
            respawnsRemaining = 0;
        }

        public void ConfigureMatchRespawnLocation(Vector3 position, Quaternion rotation)
        {
            hasMatchRespawnLocation = true;
            matchRespawnPosition = position;
            matchRespawnRotation = rotation;
        }

        public void LockForMatchEnd()
        {
            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }

            IsRespawning = false;
            if (robotMovement != null)
            {
                robotMovement.SetControlled(false);
                robotMovement.SetMatchInputLocked(true);
                robotMovement.SetCameraDisabled();
            }
            if (robotCombat != null)
            {
                robotCombat.ConfigurePlayer(false);
                robotCombat.Cancel();
            }
            if (robotAbilities != null)
                robotAbilities.CancelAll();
        }

        public void RegisterHit()
        {
            HitConfirmed?.Invoke();
        }

        private void Update()
        {
            if (!IsGameplayScene() || dead || playerCamera == null ||
                Keyboard.current == null || Mouse.current == null)
                return;

            if (robotMovement != null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            Look();
            Move();

            if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
                Fire();
        }

        private void LateUpdate()
        {
            if (robotMovement != null)
                return;

            if (playerCamera == null || dead)
                return;

            var lookRotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = transform.position + Vector3.up * 1.35f;
            playerCamera.transform.position = focus - lookRotation * Vector3.forward * 7f;
            playerCamera.transform.rotation = lookRotation;
        }

        private bool IsGameplayScene()
        {
            return string.Equals(
                gameObject.scene.path,
                "Assets/Scenes/RoboClashGameplay.unity",
                StringComparison.OrdinalIgnoreCase);
        }

        private void Look()
        {
            var delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -10f, 55f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void Move()
        {
            var keyboard = Keyboard.current;
            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;

            var motion = (transform.right * input.x + transform.forward * input.y).normalized * moveSpeed;
            if (characterController.isGrounded)
            {
                verticalVelocity = -1f;
                if (keyboard.spaceKey.wasPressedThisFrame)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            }

            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            motion.y = verticalVelocity;
            platformContactThisMove = null;
            var platformMotion = currentPlatform != null && characterController.isGrounded
                ? currentPlatform.Velocity * Time.deltaTime
                : Vector3.zero;
            characterController.Move(motion * Time.deltaTime + platformMotion);
            currentPlatform = platformContactThisMove;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
                platformContactThisMove = hit.collider.GetComponentInParent<ArenaMovingPlatform>();
        }

        private void Fire()
        {
            if (muzzle == null)
                return;

            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Energy Bolt";
            projectile.transform.position = muzzle.position;
            projectile.transform.localScale = Vector3.one * 0.22f;
            var collider = projectile.GetComponent<SphereCollider>();
            collider.isTrigger = false;
            projectile.AddComponent<Rigidbody>();
            var behavior = projectile.AddComponent<ArenaProjectile>();
            behavior.Launch(gameObject, playerCamera.transform.forward, projectileSpeed, weaponDamage);
            nextFireTime = Time.time + fireInterval;
        }

        public void EquipWeapon(float damage, float speed, float interval)
        {
            weaponDamage = Mathf.Max(1f, damage);
            projectileSpeed = Mathf.Max(1f, speed);
            fireInterval = Mathf.Max(0.05f, interval);
            if (robotDefinition != null)
            {
                robotDefinition.stats.damagePerShot = weaponDamage;
                robotDefinition.projectileSpeed = projectileSpeed;
                robotDefinition.stats.shotsPerSecond = 1f / fireInterval;
            }
        }

        public override void TakeDamage(float amount)
        {
            ApplyIncomingDamage(amount, null, false);
        }

        public override void TakeDamage(float amount, ArenaDamageable instigator)
        {
            ApplyIncomingDamage(amount, instigator, true);
        }

        private void ApplyIncomingDamage(float amount, ArenaDamageable instigator, bool playFeedback)
        {
            if (dead)
                return;

            var shieldBeforeDamage = Shield;
            var originalDamage = amount;
            if (robotDefinition != null)
            {
                var multiplier = robotDefinition.stats.incomingDamageMultiplier;
                if (robotAbilities != null)
                    multiplier *= robotAbilities.GetDamageMultiplier();
                amount *= Mathf.Clamp(multiplier, 0.45f, 1f);
            }

            base.TakeDamage(amount, instigator);
            if (amount > 0f && !dead && playFeedback)
            {
                robotAudio?.Play(shieldBeforeDamage > 0f
                    ? RoboClashAudioEvent.ShieldImpact
                    : RoboClashAudioEvent.Damage);
                var reactionScale = robotDefinition != null &&
                                    robotDefinition.stats.maxHealth > 0f
                    ? amount / robotDefinition.stats.maxHealth
                    : 0.45f;
                robotAnimator?.TriggerHitReaction(Mathf.Clamp(reactionScale * 1.5f, 0.2f, 0.8f));
                robotMovement?.AddCameraKick(0f, Mathf.Clamp01(amount / 40f));
            }

            if (robotAbilities != null && robotDefinition != null)
                robotAbilities.AddUltimateCharge(originalDamage * robotDefinition.stats.ultimateChargePerDamage);
        }

        protected override void OnDeath()
        {
            dead = true;
            if (robotMovement != null)
            {
                robotMovement.SetControlled(false);
                robotMovement.SetCameraDead();
            }
            if (robotCombat != null)
            {
                robotCombat.ConfigurePlayer(false);
                robotCombat.Cancel();
            }
            if (robotAbilities != null)
                robotAbilities.CancelAll();
            robotAnimator?.SetDead(true);
            robotVfx?.StopAllEffects();
            if (robotDefinition != null)
            {
                var center = transform.position + Vector3.up;
                RoboClashCombatVFX.PlayBurst(center, Vector3.up,
                    robotDefinition.emissionColor, 1.6f, true);
                RoboClashCombatVFX.PlayPulse(center, robotDefinition.emissionColor, 5f);
            }
            characterController.enabled = false;
            if (!matchRespawnsConfigured || unlimitedMatchRespawns || respawnsRemaining > 0)
            {
                if (matchRespawnsConfigured && !unlimitedMatchRespawns)
                    respawnsRemaining--;
                IsRespawning = true;
                respawnCoroutine = StartCoroutine(Respawn());
            }
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(2f);
            respawnCoroutine = null;
            if (hasMatchRespawnLocation)
            {
                transform.SetPositionAndRotation(matchRespawnPosition, matchRespawnRotation);
                yaw = matchRespawnRotation.eulerAngles.y;
                pitch = 12f;
            }
            else if (spawnZone != null)
            {
                transform.SetPositionAndRotation(spawnZone.transform.position + Vector3.up * 0.35f,
                    spawnZone.transform.rotation);
                yaw = spawnZone.transform.eulerAngles.y;
                pitch = 12f;
            }

            Restore(MaximumHealth, robotDefinition != null ? robotDefinition.stats.maxShield : 0f);
            verticalVelocity = 0f;
            dead = false;
            IsRespawning = false;
            characterController.enabled = true;
            robotAnimator?.SetDead(false);
            robotVfx?.PlayRespawn();
            if (robotMovement != null)
            {
                robotMovement.ResetMotion();
                robotMovement.SetMatchInputLocked(false);
                robotMovement.SetControlled(playerCamera != null);
                if (playerCamera != null)
                    robotMovement.BeginRespawnCameraTransition();
            }
            if (robotCombat != null)
                robotCombat.ConfigurePlayer(playerCamera != null);
            Respawned?.Invoke(this);
        }

    }
}
````

### `Assets/Scripts/RoboClash/SpikeTrap.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SpikeTrap : ArenaHazard
    {
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponentInParent<ArenaDamageable>();
            if (target == null)
                return;

            DamageOnce(target);
            target.GetComponent<RoboClashRobotAudio>()?.Play(RoboClashAudioEvent.Damage);
        }
    }
}
````

### `Assets/Scripts/RoboClash/UI/RoboClashHUD.cs`

````csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoboClashMatchManager))]
    [DefaultExecutionOrder(32000)]
    public sealed class RoboClashHUD : MonoBehaviour
    {
        private const string GameplayScenePath = "Assets/Scenes/RoboClashGameplay.unity";
        private const int KillFeedSize = 4;

        private static readonly Color Cyan = new Color(0.08f, 0.8f, 1f);
        private static readonly Color Green = new Color(0.18f, 0.95f, 0.58f);
        private static readonly Color Red = new Color(1f, 0.18f, 0.16f);
        private static readonly Color Amber = new Color(1f, 0.66f, 0.12f);
        private static readonly Color MutedColor = new Color(0.58f, 0.69f, 0.78f);
        private static readonly Color PanelColor = new Color(0.015f, 0.045f, 0.075f, 0.93f);

        private RoboClashMatchManager matchManager;
        private RobotArenaController playerController;
        private RoboClashRobotDefinition playerDefinition;
        private ArenaDamageable playerDamageable;
        private RoboClashRobotCombat playerCombat;
        private RoboClashRobotAbilities playerAbilities;

        private Canvas canvas;
        private Font font;
        private GameObject gameplayRoot;
        private GameObject pauseRoot;
        private GameObject resultsRoot;
        private GameObject crosshairRoot;
        private Text robotNameText;
        private Text healthText;
        private Text shieldText;
        private Text weaponNameText;
        private Text ammoText;
        private Text reloadText;
        private Text abilityNameText;
        private Text abilityText;
        private Text ultimateNameText;
        private Text ultimateText;
        private Text minimapStatusText;
        private Text timerText;
        private Text stateText;
        private Text combatantsText;
        private Text scoreboardText;
        private Text countdownText;
        private Text notificationText;
        private Text eliminatedText;
        private Text crosshairText;
        private RectTransform crosshairRect;
        private Text resultTitleText;
        private Text resultDetailsText;
        private Text[] killFeedTexts;
        private Image healthFill;
        private Image shieldFill;
        private Image reloadFill;
        private Image abilityFill;
        private Image ultimateFill;
        private Image lowHealthTint;
        private Image damageFlash;
        private RoboClashMinimap minimap;
        private RectTransform minimapMarker;
        private float targetHealthRatio;
        private float targetShieldRatio;
        private float displayedHealthRatio;
        private float displayedShieldRatio;
        private float targetUltimateRatio;
        private float displayedUltimateRatio;
        private float damageFlashAlpha;
        private bool vitalBarsInitialized;
        private bool ultimateBarInitialized;
        private RoboClashSettingsPanel settingsPanel;
        private string[] killFeedEntries;
        private Color[] killFeedColors;
        private float[] killFeedExpiry;
        private float notificationExpiry;
        private float nextStatusRefresh;
        private float nextCombatantRefresh;
        private int lastDisplayedSecond = -1;
        private int lastDisplayedWave;
        private bool lastDisplayedWaveBreak;
        private int displayedMatchState = -1;
        private bool lowAmmo;
        private bool lowHealth;
        private bool wasReloading;
        private bool wasOutOfAmmo;
        private bool wasAbilityReady;
        private bool wasUltimateReady;
        private bool resultsShown;

        private void Awake()
        {
            if (!enabled)
                return;

            if (!string.Equals(
                    gameObject.scene.path,
                    GameplayScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError(
                    $"RoboClashHUD must only initialize in '{GameplayScenePath}', but was found in " +
                    $"scene '{gameObject.scene.name}' at '{gameObject.scene.path}'. " +
                    "Its gameplay Canvas and EventSystem were not created.",
                    this);
                enabled = false;
                return;
            }

            matchManager = GetComponent<RoboClashMatchManager>();
            BuildInterface();
        }

        private void RefreshCrosshairVisibility()
        {
            if (crosshairRoot == null)
                return;

            var isPlaying = matchManager != null &&
                            matchManager.State == RoboClashMatchState.Playing;
            var isAlive = playerController != null && !playerController.IsDead;
            var movement = playerController != null
                ? playerController.GetComponent<RoboClashRobotMovement>()
                : null;
            var hasGameplayCamera = movement == null
                ? playerController != null && playerController.PlayerCamera != null
                : movement.CameraState == RoboClashCameraState.FirstPerson ||
                  movement.CameraState == RoboClashCameraState.ThirdPerson;
            crosshairRoot.SetActive(isPlaying && isAlive && hasGameplayCamera);

            if (crosshairRect != null)
            {
                var firingScale = playerCombat != null && playerCombat.IsFiring ? 1.2f : 1f;
                crosshairRect.localScale = Vector3.one * firingScale;
            }
        }

        private void OnEnable()
        {
            SubscribeToMatch();
            BindPlayer(matchManager != null ? matchManager.PlayerController : null);
        }

        private void Start()
        {
            BindPlayer(matchManager != null ? matchManager.PlayerController : null);
            if (matchManager != null)
            {
                RefreshTimer(matchManager.RemainingTime);
                RefreshMatchState(matchManager.State);
                RefreshCombatantCount();
                RefreshModeStatus();
                if (matchManager.State == RoboClashMatchState.Victory ||
                    matchManager.State == RoboClashMatchState.Defeat ||
                    matchManager.State == RoboClashMatchState.Draw)
                    ShowResults(matchManager.Statistics);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromMatch();
            BindPlayer(null);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && matchManager != null)
            {
                if (matchManager.State == RoboClashMatchState.Playing)
                    matchManager.PauseMatch();
                else if (matchManager.State == RoboClashMatchState.Paused)
                    matchManager.ResumeMatch();
            }

            if (matchManager == null)
                return;

            if (matchManager.State == RoboClashMatchState.Playing && Keyboard.current != null &&
                playerAbilities != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame && !playerAbilities.IsAbilityReady)
                    ShowNotification("ABILITY UNAVAILABLE");
                if (Keyboard.current.eKey.wasPressedThisFrame && !playerAbilities.IsUltimateReady)
                    ShowNotification("ULTIMATE UNAVAILABLE");
            }

            var now = Time.unscaledTime;
            if (now >= nextStatusRefresh)
            {
                nextStatusRefresh = now + 0.12f;
                RefreshAbilityStatus();
                RefreshAmmo();
                RefreshCrosshairVisibility();
                if (notificationText != null)
                    notificationText.gameObject.SetActive(now < notificationExpiry);
                for (var i = 0; i < killFeedTexts.Length; i++)
                    killFeedTexts[i].gameObject.SetActive(now < killFeedExpiry[i]);
            }

            UpdateVitalBars(Time.unscaledDeltaTime);
            UpdateDamageFlash(Time.unscaledDeltaTime);

            if (now >= nextCombatantRefresh)
            {
                nextCombatantRefresh = now + 0.25f;
                RefreshCombatantCount();
                RefreshModeStatus();
            }
        }

        private void BuildInterface()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("RoboClash Gameplay Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();

            gameplayRoot = new GameObject("Gameplay HUD", typeof(RectTransform));
            gameplayRoot.transform.SetParent(canvas.transform, false);
            Stretch(gameplayRoot.GetComponent<RectTransform>());
            BuildPlayerPanel();
            BuildWeaponPanel();
            BuildAbilityPanel();
            BuildMatchPanel();
            BuildMinimap();
            BuildKillFeed();
            BuildCrosshair();
            BuildNotifications();
            BuildPausePanel();
            BuildPauseSettings();
            BuildResultsPanel();
            ShowMatchState(RoboClashMatchState.Initializing);
        }

        private void BuildPlayerPanel()
        {
            var panel = CreatePanel(gameplayRoot.transform, "Player Status",
                new Vector2(0.025f, 0.035f), new Vector2(0.305f, 0.235f), PanelColor);
            robotNameText = CreateText(panel.transform, "ROBOT", 22, Color.white,
                new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.96f), TextAnchor.MiddleLeft);
            healthText = CreateText(panel.transform, "HEALTH  -- / --", 15, Color.white,
                new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.68f), TextAnchor.MiddleLeft);
            healthFill = CreateBar(panel.transform, "Health Bar",
                new Vector2(0.06f, 0.39f), new Vector2(0.94f, 0.48f), Green);
            shieldText = CreateText(panel.transform, "SHIELD  -- / --", 15, Color.white,
                new Vector2(0.06f, 0.17f), new Vector2(0.94f, 0.37f), TextAnchor.MiddleLeft);
            shieldFill = CreateBar(panel.transform, "Shield Bar",
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.17f), Cyan);
            lowHealthTint = CreatePanel(gameplayRoot.transform, "Low Health Warning",
                new Vector2(0.36f, 0.78f), new Vector2(0.64f, 0.84f),
                new Color(0.42f, 0.015f, 0.025f, 0.86f)).GetComponent<Image>();
            lowHealthTint.raycastTarget = false;
            CreateText(lowHealthTint.transform, "LOW HEALTH", 18, Color.white,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            lowHealthTint.gameObject.SetActive(false);
            damageFlash = CreatePanel(gameplayRoot.transform, "Player Damage Flash",
                Vector2.zero, Vector2.one, new Color(1f, 0.035f, 0.02f, 0f)).GetComponent<Image>();
            damageFlash.raycastTarget = false;
            eliminatedText = CreateText(gameplayRoot.transform, string.Empty, 27, Red,
                new Vector2(0.32f, 0.42f), new Vector2(0.68f, 0.52f), TextAnchor.MiddleCenter);
            eliminatedText.gameObject.SetActive(false);
        }

        private void BuildWeaponPanel()
        {
            var panel = CreatePanel(gameplayRoot.transform, "Weapon Status",
                new Vector2(0.755f, 0.035f), new Vector2(0.975f, 0.235f), PanelColor);
            weaponNameText = CreateText(panel.transform, string.Empty, 13, Cyan,
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.96f), TextAnchor.MiddleLeft);
            ammoText = CreateText(panel.transform, "-- / --", 26, Color.white,
                new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.78f), TextAnchor.MiddleLeft);
            reloadText = CreateText(panel.transform, string.Empty, 14, Amber,
                new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.42f), TextAnchor.MiddleLeft);
            reloadFill = CreateBar(panel.transform, "Reload Progress",
                new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.17f), Amber);
        }

        private void BuildAbilityPanel()
        {
            var panel = CreatePanel(gameplayRoot.transform, "Ability Status",
                new Vector2(0.315f, 0.035f), new Vector2(0.685f, 0.18f), PanelColor);
            var abilitySlot = CreatePanel(panel.transform, "Ability Slot",
                new Vector2(0.02f, 0.06f), new Vector2(0.49f, 0.94f),
                new Color(0.015f, 0.09f, 0.12f, 0.98f));
            var ultimateSlot = CreatePanel(panel.transform, "Ultimate Slot",
                new Vector2(0.51f, 0.06f), new Vector2(0.98f, 0.94f),
                new Color(0.12f, 0.085f, 0.015f, 0.98f));
            abilityNameText = CreateText(abilitySlot.transform, "ABILITY", 13, Cyan,
                new Vector2(0.2f, 0.57f), new Vector2(0.96f, 0.94f), TextAnchor.MiddleLeft);
            abilityText = CreateText(abilitySlot.transform, "READY", 13, Green,
                new Vector2(0.2f, 0.25f), new Vector2(0.96f, 0.59f), TextAnchor.MiddleLeft);
            CreateText(abilitySlot.transform, "Q", 21, Cyan,
                new Vector2(0.04f, 0.39f), new Vector2(0.2f, 0.94f), TextAnchor.MiddleCenter);
            abilityFill = CreateBar(abilitySlot.transform, "Ability Cooldown",
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.18f), Cyan);

            ultimateNameText = CreateText(ultimateSlot.transform, "ULTIMATE", 13, Amber,
                new Vector2(0.2f, 0.57f), new Vector2(0.96f, 0.94f), TextAnchor.MiddleLeft);
            ultimateText = CreateText(ultimateSlot.transform, "CHARGING", 13, Amber,
                new Vector2(0.2f, 0.25f), new Vector2(0.96f, 0.59f), TextAnchor.MiddleLeft);
            CreateText(ultimateSlot.transform, "E", 21, Amber,
                new Vector2(0.04f, 0.39f), new Vector2(0.2f, 0.94f), TextAnchor.MiddleCenter);
            ultimateFill = CreateBar(ultimateSlot.transform, "Ultimate Charge",
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.18f), Amber);
        }

        private void BuildMinimap()
        {
            var panel = CreatePanel(gameplayRoot.transform, "Tactical Minimap Frame",
                new Vector2(0.82f, 0.73f), new Vector2(0.975f, 0.985f),
                new Color(0.02f, 0.34f, 0.46f, 0.95f));
            var frame = panel.AddComponent<Outline>();
            frame.effectColor = Cyan;
            frame.effectDistance = new Vector2(2f, -2f);
            var viewport = CreatePanel(panel.transform, "Map Viewport",
                new Vector2(0.045f, 0.045f), new Vector2(0.955f, 0.84f),
                new Color(0.012f, 0.032f, 0.055f, 1f));
            viewport.AddComponent<RectMask2D>();

            var geometryObject = new GameObject("Arena Geometry",
                typeof(RectTransform), typeof(CanvasRenderer));
            geometryObject.transform.SetParent(viewport.transform, false);
            Stretch(geometryObject.GetComponent<RectTransform>());
            minimap = geometryObject.AddComponent<RoboClashMinimap>();
            minimap.color = Color.white;

            var markerText = CreateText(viewport.transform, "▲", 22, Cyan,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), TextAnchor.MiddleCenter);
            minimapMarker = markerText.rectTransform;
            minimapMarker.sizeDelta = new Vector2(28f, 28f);
            markerText.fontStyle = FontStyle.Bold;
            CreateText(panel.transform, "TACTICAL MAP", 10, Cyan,
                new Vector2(0.04f, 0.85f), new Vector2(0.54f, 0.99f), TextAnchor.MiddleLeft);
            minimapStatusText = CreateText(panel.transform, string.Empty, 9, Amber,
                new Vector2(0.54f, 0.85f), new Vector2(0.96f, 0.99f), TextAnchor.MiddleRight);
            ConfigureMinimap();
        }

        private void ConfigureMinimap()
        {
            if (minimap == null)
                return;

            minimap.Configure(matchManager != null ? matchManager.transform : null,
                playerController, minimapMarker);

            if (minimapStatusText != null)
            {
                minimapStatusText.text = minimap.HasMapData ? string.Empty : "MAP UNAVAILABLE";
                minimapStatusText.gameObject.SetActive(!minimap.HasMapData);
            }
        }

        private void BuildMatchPanel()
        {
            var panel = CreatePanel(gameplayRoot.transform, "Match Status",
                new Vector2(0.37f, 0.91f), new Vector2(0.63f, 0.985f), PanelColor);
            timerText = CreateText(panel.transform, "00:00", 25, Color.white,
                new Vector2(0.02f, 0.42f), new Vector2(0.98f, 0.98f), TextAnchor.MiddleCenter);
            stateText = CreateText(panel.transform, "INITIALIZING", 13, Cyan,
                new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.45f), TextAnchor.MiddleCenter);
            combatantsText = CreateText(gameplayRoot.transform, "COMBATANTS: --",
                13, Color.white, new Vector2(0.37f, 0.875f), new Vector2(0.63f, 0.91f),
                TextAnchor.MiddleCenter);
            scoreboardText = CreateText(gameplayRoot.transform, string.Empty, 12, Color.white,
                new Vector2(0.74f, 0.53f), new Vector2(0.975f, 0.69f), TextAnchor.UpperRight);
            countdownText = CreateText(gameplayRoot.transform, string.Empty,
                76, Color.white, new Vector2(0.38f, 0.44f), new Vector2(0.62f, 0.60f),
                TextAnchor.MiddleCenter);
        }

        private void BuildKillFeed()
        {
            killFeedTexts = new Text[KillFeedSize];
            killFeedEntries = new string[KillFeedSize];
            killFeedColors = new Color[KillFeedSize];
            killFeedExpiry = new float[KillFeedSize];
            for (var i = 0; i < killFeedTexts.Length; i++)
            {
                var top = 0.87f - i * 0.045f;
                killFeedTexts[i] = CreateText(gameplayRoot.transform, string.Empty, 13, Color.white,
                    new Vector2(0.025f, top - 0.04f), new Vector2(0.30f, top),
                    TextAnchor.MiddleLeft);
                killFeedTexts[i].gameObject.SetActive(false);
            }
        }

        private void BuildCrosshair()
        {
            crosshairRoot = new GameObject("Crosshair", typeof(RectTransform));
            crosshairRoot.transform.SetParent(gameplayRoot.transform, false);
            var rect = crosshairRoot.GetComponent<RectTransform>();
            crosshairRect = rect;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(40f, 40f);
            rect.anchoredPosition = Vector2.zero;
            crosshairText = CreateText(crosshairRoot.transform, "+", 30, Color.white,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            var outline = crosshairText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private void BuildNotifications()
        {
            notificationText = CreateText(gameplayRoot.transform, string.Empty, 18, Amber,
                new Vector2(0.3f, 0.82f), new Vector2(0.7f, 0.88f), TextAnchor.MiddleCenter);
            notificationText.gameObject.SetActive(false);
        }

        private void BuildPausePanel()
        {
            pauseRoot = CreateOverlay("Pause Menu");
            CreateText(pauseRoot.transform, "PAUSED", 34, Color.white,
                new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.91f), TextAnchor.MiddleCenter);
            CreateMenuButton(pauseRoot.transform, "RESUME", new Vector2(0.32f, 0.63f),
                new Vector2(0.68f, 0.72f), ResumeMatch);
            CreateMenuButton(pauseRoot.transform, "RESTART MATCH", new Vector2(0.32f, 0.51f),
                new Vector2(0.68f, 0.60f), RestartMatch);
            CreateMenuButton(pauseRoot.transform, "SETTINGS", new Vector2(0.32f, 0.39f),
                new Vector2(0.68f, 0.48f), OpenPauseSettings);
            CreateMenuButton(pauseRoot.transform, "MAIN MENU", new Vector2(0.32f, 0.27f),
                new Vector2(0.68f, 0.36f), ReturnToMainMenu);
            pauseRoot.SetActive(false);
        }

        private void BuildPauseSettings()
        {
            settingsPanel = gameObject.AddComponent<RoboClashSettingsPanel>();
            settingsPanel.Initialize(canvas.transform, font, ClosePauseSettings);
            settingsPanel.Root.SetActive(false);
        }

        private void BuildResultsPanel()
        {
            resultsRoot = CreateOverlay("Match Results");
            resultTitleText = CreateText(resultsRoot.transform, "MATCH COMPLETE", 40, Color.white,
                new Vector2(0.15f, 0.75f), new Vector2(0.85f, 0.9f), TextAnchor.MiddleCenter);
            resultDetailsText = CreateText(resultsRoot.transform, string.Empty, 16, Color.white,
                new Vector2(0.16f, 0.41f), new Vector2(0.84f, 0.73f), TextAnchor.MiddleCenter);
            CreateMenuButton(resultsRoot.transform, "PLAY AGAIN", new Vector2(0.26f, 0.32f),
                new Vector2(0.48f, 0.42f), RestartMatch);
            CreateMenuButton(resultsRoot.transform, "MAIN MENU", new Vector2(0.52f, 0.32f),
                new Vector2(0.74f, 0.42f), ReturnToMainMenu);
            resultsRoot.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            RoboClashUIEventSystem.EnsureForScene(gameObject, "RoboClash Gameplay Event System");
        }

        private void SubscribeToMatch()
        {
            if (matchManager == null)
                return;

            matchManager.PlayerRegistered += BindPlayer;
            matchManager.MatchStateChanged += RefreshMatchState;
            matchManager.MatchTimerChanged += RefreshTimer;
            matchManager.CountdownChanged += RefreshCountdown;
            matchManager.StatisticsChanged += RefreshStatistics;
            matchManager.MatchEnded += ShowResults;
            matchManager.ParticipantEliminated += OnParticipantEliminated;
            matchManager.ModeStatusChanged += RefreshModeStatus;
        }

        private void UnsubscribeFromMatch()
        {
            if (matchManager == null)
                return;

            matchManager.PlayerRegistered -= BindPlayer;
            matchManager.MatchStateChanged -= RefreshMatchState;
            matchManager.MatchTimerChanged -= RefreshTimer;
            matchManager.CountdownChanged -= RefreshCountdown;
            matchManager.StatisticsChanged -= RefreshStatistics;
            matchManager.MatchEnded -= ShowResults;
            matchManager.ParticipantEliminated -= OnParticipantEliminated;
            matchManager.ModeStatusChanged -= RefreshModeStatus;
        }

        private void BindPlayer(RobotArenaController controller)
        {
            UnbindPlayer();
            lowHealth = false;
            lowAmmo = false;
            wasReloading = false;
            wasOutOfAmmo = false;
            wasAbilityReady = false;
            wasUltimateReady = false;
            vitalBarsInitialized = false;
            ultimateBarInitialized = false;
            damageFlashAlpha = 0f;
            if (damageFlash != null)
                damageFlash.color = new Color(1f, 0.035f, 0.02f, 0f);
            if (eliminatedText != null)
                eliminatedText.gameObject.SetActive(false);
            playerController = controller;
            if (playerController == null)
            {
                RefreshCrosshairVisibility();
                ConfigureMinimap();
                robotNameText.text = "PLAYER";
                robotNameText.color = Color.white;
                healthText.text = "HEALTH  -- / --";
                shieldText.text = "SHIELD  -- / --";
                healthFill.fillAmount = 0f;
                shieldFill.fillAmount = 0f;
                targetHealthRatio = 0f;
                targetShieldRatio = 0f;
                displayedHealthRatio = 0f;
                displayedShieldRatio = 0f;
                vitalBarsInitialized = true;
                lowHealthTint.gameObject.SetActive(false);
                weaponNameText.text = string.Empty;
                ammoText.text = "-- / --";
                ammoText.color = Color.white;
                reloadText.text = string.Empty;
                reloadText.gameObject.SetActive(false);
                reloadFill.fillAmount = 0f;
                reloadFill.gameObject.SetActive(false);
                abilityNameText.text = "ABILITY";
                abilityText.text = "OFFLINE";
                ultimateNameText.text = "ULTIMATE";
                ultimateText.text = "OFFLINE";
                abilityFill.fillAmount = 0f;
                ultimateFill.fillAmount = 0f;
                targetUltimateRatio = 0f;
                displayedUltimateRatio = 0f;
                ultimateBarInitialized = true;
                return;
            }

            playerDefinition = controller.GetComponent<RoboClashRobotDefinition>();
            playerDamageable = controller;
            playerCombat = controller.GetComponent<RoboClashRobotCombat>();
            playerAbilities = controller.GetComponent<RoboClashRobotAbilities>();

            if (playerDamageable != null)
            {
                playerDamageable.VitalsChanged += OnVitalsChanged;
                playerDamageable.DamageApplied += OnPlayerDamageApplied;
                playerDamageable.Eliminated += OnPlayerEliminated;
            }
            if (playerCombat != null)
                playerCombat.StateChanged += OnCombatStateChanged;
            if (playerAbilities != null)
            {
                playerAbilities.StateChanged += OnAbilityStateChanged;
                playerAbilities.AbilityActivated += OnAbilityActivated;
                playerAbilities.UltimateActivated += OnUltimateActivated;
                wasAbilityReady = playerAbilities.IsAbilityReady;
                wasUltimateReady = playerAbilities.IsUltimateReady;
            }
            playerController.HitConfirmed += OnHitConfirmed;

            robotNameText.text = playerDefinition != null
                ? playerDefinition.robotType.ToString().ToUpperInvariant()
                : "PLAYER";
            robotNameText.color = playerDefinition != null ? playerDefinition.primaryColor : Color.white;
            weaponNameText.text = playerDefinition != null
                ? playerDefinition.weaponName.ToUpperInvariant()
                : "WEAPON";
            weaponNameText.color = playerDefinition != null ? playerDefinition.emissionColor : Cyan;
            ConfigureMinimap();
            RefreshVitals();
            RefreshAmmo();
            RefreshAbilityStatus();
            RefreshCrosshairVisibility();
        }

        private void UnbindPlayer()
        {
            if (playerDamageable != null)
            {
                playerDamageable.VitalsChanged -= OnVitalsChanged;
                playerDamageable.DamageApplied -= OnPlayerDamageApplied;
                playerDamageable.Eliminated -= OnPlayerEliminated;
            }
            if (playerCombat != null)
                playerCombat.StateChanged -= OnCombatStateChanged;
            if (playerAbilities != null)
            {
                playerAbilities.StateChanged -= OnAbilityStateChanged;
                playerAbilities.AbilityActivated -= OnAbilityActivated;
                playerAbilities.UltimateActivated -= OnUltimateActivated;
            }
            if (playerController != null)
                playerController.HitConfirmed -= OnHitConfirmed;

            playerController = null;
            playerDefinition = null;
            playerDamageable = null;
            playerCombat = null;
            playerAbilities = null;
        }

        private void OnVitalsChanged(ArenaDamageable damageable)
        {
            if (damageable == playerDamageable)
                RefreshVitals();
        }

        private void OnPlayerDamageApplied(
            ArenaDamageable damageable, float amount, ArenaDamageable instigator)
        {
            if (damageable != playerDamageable || amount <= 0f || damageFlash == null)
                return;

            damageFlashAlpha = Mathf.Clamp01(damageFlashAlpha + 0.42f);
            damageFlash.color = new Color(1f, 0.035f, 0.02f, damageFlashAlpha);
        }

        private void OnPlayerEliminated(ArenaDamageable damageable)
        {
            if (damageable == playerDamageable && eliminatedText != null)
            {
                eliminatedText.text = playerController != null && playerController.IsRespawning
                    ? "REBOOTING..."
                    : "ELIMINATED";
                eliminatedText.gameObject.SetActive(true);
            }
        }

        private void OnCombatStateChanged(RoboClashRobotCombat combat)
        {
            if (combat == playerCombat)
                RefreshAmmo();
        }

        private void OnAbilityStateChanged(RoboClashRobotAbilities abilities)
        {
            if (abilities == playerAbilities)
                RefreshAbilityStatus();
        }

        private void OnAbilityActivated()
        {
            ShowNotification("ABILITY ACTIVATED");
        }

        private void OnUltimateActivated()
        {
            ShowNotification("ULTIMATE ACTIVATED");
        }

        private void OnHitConfirmed()
        {
            if (crosshairText == null)
                return;
            crosshairText.color = Green;
            Invoke(nameof(ResetCrosshair), 0.16f);
        }

        private void ResetCrosshair()
        {
            if (crosshairText != null)
                crosshairText.color = Color.white;
        }

        private void RefreshVitals()
        {
            if (playerDamageable == null || healthText == null)
                return;

            var healthRatio = playerDamageable.MaximumHealth > 0f
                ? Mathf.Clamp01(playerDamageable.Health / playerDamageable.MaximumHealth)
                : 0f;
            var shieldRatio = playerDamageable.MaximumShield > 0f
                ? Mathf.Clamp01(playerDamageable.Shield / playerDamageable.MaximumShield)
                : 0f;
            targetHealthRatio = healthRatio;
            targetShieldRatio = shieldRatio;
            if (!vitalBarsInitialized)
            {
                displayedHealthRatio = healthRatio;
                displayedShieldRatio = shieldRatio;
                vitalBarsInitialized = true;
                healthFill.fillAmount = healthRatio;
                shieldFill.fillAmount = shieldRatio;
            }
            healthFill.color = healthRatio <= 0.25f ? Red : Green;
            healthText.text = $"HEALTH  {Mathf.CeilToInt(playerDamageable.Health)} / {Mathf.CeilToInt(playerDamageable.MaximumHealth)}";
            shieldText.text = $"SHIELD  {Mathf.CeilToInt(playerDamageable.Shield)} / {Mathf.CeilToInt(playerDamageable.MaximumShield)}";
            var isLow = healthRatio > 0f && healthRatio <= 0.25f;
            lowHealthTint.gameObject.SetActive(isLow);
            if (isLow && !lowHealth)
                ShowNotification("LOW HEALTH");
            lowHealth = isLow;
            if (!playerDamageable.IsDead && eliminatedText.gameObject.activeSelf)
                eliminatedText.gameObject.SetActive(false);
        }

        private void UpdateVitalBars(float deltaTime)
        {
            if (!vitalBarsInitialized || healthFill == null || shieldFill == null)
                return;

            displayedHealthRatio = Mathf.MoveTowards(displayedHealthRatio, targetHealthRatio,
                deltaTime * 2.5f);
            displayedShieldRatio = Mathf.MoveTowards(displayedShieldRatio, targetShieldRatio,
                deltaTime * 3f);
            healthFill.fillAmount = displayedHealthRatio;
            shieldFill.fillAmount = displayedShieldRatio;
            if (ultimateBarInitialized && ultimateFill != null)
            {
                displayedUltimateRatio = Mathf.MoveTowards(
                    displayedUltimateRatio, targetUltimateRatio, deltaTime * 2.5f);
                ultimateFill.fillAmount = displayedUltimateRatio;
            }
        }

        private void UpdateDamageFlash(float deltaTime)
        {
            if (damageFlash == null || damageFlashAlpha <= 0f)
                return;

            damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, deltaTime * 1.8f);
            damageFlash.color = new Color(1f, 0.035f, 0.02f, damageFlashAlpha);
        }

        private void RefreshAmmo()
        {
            if (playerCombat == null)
            {
                if (weaponNameText != null)
                    weaponNameText.text = "WEAPON UNAVAILABLE";
                ammoText.text = "-- / --";
                reloadText.text = string.Empty;
                reloadFill.fillAmount = 0f;
                return;
            }

            var capacity = playerCombat.MagazineCapacity;
            ammoText.text = $"{playerCombat.Ammunition:00} / {capacity:00}";
            var isLow = capacity > 0 && playerCombat.Ammunition <= Mathf.Max(1, capacity / 4);
            ammoText.color = isLow ? Amber : Color.white;
            if (isLow && !lowAmmo)
                ShowNotification("LOW AMMO");
            lowAmmo = isLow;

            if (playerCombat.IsReloading)
            {
                if (!wasReloading)
                    ShowNotification("RELOADING");
                reloadText.text = $"RELOADING  {playerCombat.ReloadRemaining:0.0}s";
                reloadFill.fillAmount = playerCombat.ReloadProgress;
                reloadText.gameObject.SetActive(true);
                reloadFill.gameObject.SetActive(true);
            }
            else
            {
                if (wasReloading && playerCombat.Ammunition >= capacity)
                    ShowNotification("RELOAD COMPLETE");
                else if (playerCombat.Ammunition <= 0 && !wasReloading && !wasOutOfAmmo)
                    ShowNotification("MAGAZINE EMPTY");
                reloadText.text = playerCombat.Ammunition <= 0 ? "EMPTY" : string.Empty;
                reloadFill.fillAmount = 0f;
                reloadText.gameObject.SetActive(playerCombat.Ammunition <= 0);
                reloadFill.gameObject.SetActive(false);
            }
            wasReloading = playerCombat.IsReloading;
            wasOutOfAmmo = playerCombat.Ammunition <= 0;
        }

        private void RefreshAbilityStatus()
        {
            if (playerAbilities == null || playerDefinition == null || abilityText == null)
                return;

            var abilityReady = playerAbilities.IsAbilityReady;
            var abilityActive = playerAbilities.IsAbilityActive;
            var abilityState = playerDamageable != null && playerDamageable.IsDead
                ? "OFFLINE"
                : playerAbilities.IsAbilityLocked
                    ? "LOCKED"
                    : abilityActive
                ? $"ACTIVE {playerAbilities.AbilityActiveRemaining:0.0}s"
                : abilityReady ? "READY" : $"{playerAbilities.AbilityCooldownRemaining:0.0}s";
            abilityNameText.text = playerDefinition.abilityName.ToUpperInvariant();
            abilityText.text = abilityState;
            abilityText.color = abilityReady || abilityActive ? Green : MutedColor;
            var abilityCooldown = Mathf.Max(0f, playerDefinition.abilityCooldown);
            abilityFill.fillAmount = abilityActive && playerDefinition.abilityDuration > 0f
                ? Mathf.Clamp01(playerAbilities.AbilityActiveRemaining / playerDefinition.abilityDuration)
                : abilityReady ? 1f
                : abilityCooldown > 0f
                    ? 1f - Mathf.Clamp01(playerAbilities.AbilityCooldownRemaining / abilityCooldown)
                    : 0f;

            var charge = playerAbilities.UltimateChargeRequired > 0f
                ? Mathf.Clamp01(playerAbilities.UltimateCharge / playerAbilities.UltimateChargeRequired) * 100f
                : 100f;
            var ultimateReady = playerAbilities.IsUltimateReady;
            var ultimateState = playerDamageable != null && playerDamageable.IsDead
                ? "OFFLINE"
                : playerAbilities.IsUltimateActive
                    ? $"ACTIVE {playerAbilities.UltimateActiveRemaining:0.0}s"
                    : ultimateReady
                    ? "READY"
                    : playerAbilities.IsAbilityLocked
                        ? "LOCKED"
                        : playerAbilities.UltimateCooldownRemaining > 0f
                            ? playerAbilities.UltimateChargeRequired > 0f
                                ? $"{playerAbilities.UltimateCooldownRemaining:0.0}s  /  {charge:0}%"
                                : $"{playerAbilities.UltimateCooldownRemaining:0.0}s"
                            : $"{charge:0}% CHARGED";
            ultimateNameText.text = playerDefinition.ultimateName.ToUpperInvariant();
            ultimateText.text = ultimateState;
            ultimateText.color = ultimateReady ? Amber : MutedColor;
            var ultimateCooldown = Mathf.Max(0f, playerDefinition.ultimateCooldown);
            targetUltimateRatio = playerAbilities.IsUltimateActive && playerDefinition.ultimateDuration > 0f
                ? Mathf.Clamp01(playerAbilities.UltimateActiveRemaining / playerDefinition.ultimateDuration)
                : playerAbilities.UltimateChargeRequired > 0f
                ? charge / 100f
                : ultimateCooldown > 0f
                    ? 1f - Mathf.Clamp01(playerAbilities.UltimateCooldownRemaining / ultimateCooldown)
                    : ultimateReady ? 1f : 0f;
            if (!ultimateBarInitialized)
            {
                displayedUltimateRatio = targetUltimateRatio;
                ultimateFill.fillAmount = displayedUltimateRatio;
                ultimateBarInitialized = true;
            }

            if (wasAbilityReady == false && abilityReady)
                ShowNotification("ABILITY READY");
            if (wasUltimateReady == false && ultimateReady)
                ShowNotification("ULTIMATE READY");
            wasAbilityReady = abilityReady;
            wasUltimateReady = ultimateReady;
        }

        private void RefreshTimer(float remainingSeconds)
        {
            if (timerText == null)
                return;
            if (matchManager != null && matchManager.GameMode != RoboClashGameMode.Survival &&
                !matchManager.HasMatchTimer)
            {
                timerText.text = string.Empty;
                lastDisplayedSecond = -1;
                return;
            }

            var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            if (totalSeconds == lastDisplayedSecond)
                return;
            lastDisplayedSecond = totalSeconds;
            timerText.text = matchManager != null && matchManager.GameMode == RoboClashGameMode.Survival
                ? $"TIME {totalSeconds / 60:00}:{totalSeconds % 60:00}"
                : $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void RefreshMatchState(RoboClashMatchState state)
        {
            if (stateText == null)
                return;
            stateText.text = state.ToString().ToUpperInvariant();
            stateText.color = state == RoboClashMatchState.Playing ? Green : Cyan;
            ShowMatchState(state);
            if (state == RoboClashMatchState.Initializing)
            {
                resultsShown = false;
                resultsRoot.SetActive(false);
            }
        }

        private void ShowMatchState(RoboClashMatchState state)
        {
            if (gameplayRoot == null)
                return;
            gameplayRoot.SetActive(state != RoboClashMatchState.Initializing);
            var isPlaying = state == RoboClashMatchState.Playing;
            crosshairRoot.SetActive(isPlaying);
            RefreshCrosshairVisibility();
            if (state != RoboClashMatchState.Paused)
            {
                pauseRoot.SetActive(false);
                settingsPanel.Hide();
            }

            if (state == RoboClashMatchState.Victory ||
                state == RoboClashMatchState.Defeat ||
                state == RoboClashMatchState.Draw)
            {
                gameplayRoot.SetActive(false);
                if (stateText != null)
                    stateText.text = state.ToString().ToUpperInvariant();
            }
            else if (state == RoboClashMatchState.Paused)
                pauseRoot.SetActive(true);

            if (state == RoboClashMatchState.Countdown)
                countdownText.text = matchManager != null ? matchManager.CountdownValue.ToString() : string.Empty;
            else
                countdownText.text = string.Empty;

            if (displayedMatchState != (int)state)
            {
                displayedMatchState = (int)state;
                if (state == RoboClashMatchState.Playing)
                    ShowNotification("FIGHT!");
            }
        }

        private void RefreshCountdown(int value)
        {
            if (countdownText != null && matchManager != null &&
                matchManager.State == RoboClashMatchState.Countdown)
                countdownText.text = value > 0 ? value.ToString() : "FIGHT";
        }

        private void RefreshCombatantCount()
        {
            RefreshModeStatus();
        }

        private void RefreshModeStatus()
        {
            if (matchManager == null || combatantsText == null)
                return;

            if (matchManager.GameMode == RoboClashGameMode.Survival)
            {
                var wave = matchManager.CurrentWave;
                var breakActive = matchManager.IsWaveBreakActive;
                if (wave < lastDisplayedWave)
                {
                    lastDisplayedWave = 0;
                    lastDisplayedWaveBreak = false;
                }
                if (wave > lastDisplayedWave && lastDisplayedWave > 0)
                    ShowNotification($"WAVE {wave} INCOMING");
                else if (breakActive && !lastDisplayedWaveBreak)
                    ShowNotification($"WAVE {wave} CLEARED");

                lastDisplayedWave = wave;
                lastDisplayedWaveBreak = breakActive;
                RefreshTimer(matchManager.ElapsedMatchSeconds);
                combatantsText.text = breakActive
                    ? $"WAVE {wave} CLEAR  //  NEXT {Mathf.CeilToInt(matchManager.NextWaveCountdown)}s"
                    : $"WAVE {wave}  //  LEFT {matchManager.EnemiesRemaining}  //  KILLS {matchManager.Statistics.PlayerEliminations}";
                scoreboardText.text = string.Empty;
                return;
            }

            switch (matchManager.GameMode)
            {
                case RoboClashGameMode.KillRace:
                    combatantsText.text =
                        $"KILL RACE  //  {matchManager.PlayerKills}/{matchManager.KillTarget}";
                    break;
                case RoboClashGameMode.TimedDeathmatch:
                    combatantsText.text = $"DEATHMATCH  //  {matchManager.AliveCombatantCount} ACTIVE";
                    break;
                case RoboClashGameMode.FreeForAll:
                    combatantsText.text = $"FREE-FOR-ALL  //  {matchManager.AliveCombatantCount} ACTIVE";
                    break;
                case RoboClashGameMode.Duel:
                    combatantsText.text = matchManager.DuelUsesKillTarget
                        ? $"DUEL  //  {matchManager.PlayerKills}/{matchManager.KillTarget} KILLS"
                        : $"DUEL  //  {matchManager.PlayerKills} PLAYER KILLS";
                    break;
                case RoboClashGameMode.KingOfTheArena:
                    combatantsText.text =
                        $"KING  //  {matchManager.KingControllerName} {matchManager.KingControllerScore}s";
                    break;
                case RoboClashGameMode.Knockout:
                    combatantsText.text = $"KNOCKOUT  //  {matchManager.PlayerLivesRemaining} LIVES LEFT";
                    break;
                default:
                    combatantsText.text = $"COMBATANTS  {matchManager.AliveCombatantCount}";
                    break;
            }
            scoreboardText.text = matchManager.GetScoreboardText();
        }

        private void RefreshStatistics(RoboClashMatchStatistics statistics)
        {
            if (resultsShown)
                PopulateResultDetails(statistics);
        }

        private void ShowResults(RoboClashMatchStatistics statistics)
        {
            if (resultsShown)
                return;
            resultsShown = true;
            gameplayRoot.SetActive(false);
            pauseRoot.SetActive(false);
            settingsPanel.Hide();
            resultTitleText.text = statistics.FinalResult.ToString().ToUpperInvariant();
            resultTitleText.color = statistics.FinalResult == RoboClashMatchState.Victory
                ? Green
                : statistics.FinalResult == RoboClashMatchState.Defeat ? Red : Amber;
            PopulateResultDetails(statistics);
            resultsRoot.SetActive(true);
        }

        private void PopulateResultDetails(RoboClashMatchStatistics statistics)
        {
            var robotName = playerDefinition != null
                ? playerDefinition.robotType.ToString().ToUpperInvariant()
                : "PLAYER";
            var duration = Mathf.Max(0, Mathf.FloorToInt(statistics.MatchDurationSeconds));
            var modeDetails = statistics.GameMode == RoboClashGameMode.Survival
                ? $"WAVE REACHED  {statistics.HighestWaveReached}  //  CLEARED  {statistics.HighestWaveCompleted}\n"
                : statistics.GameMode == RoboClashGameMode.FreeForAll &&
                  statistics.PlayerPlacement > 0
                    ? $"PLACEMENT  {statistics.PlayerPlacement}\n"
                    : string.Empty;
            var durationLabel = statistics.GameMode == RoboClashGameMode.Survival
                ? "SURVIVAL TIME"
                : "MATCH DURATION";
            var career = RoboClashSaveService.Current.career;
            resultDetailsText.text =
                $"{robotName}  //  {statistics.GameMode.ToString().ToUpperInvariant()}\n" +
                $"{modeDetails}" +
                $"ELIMINATIONS  {statistics.PlayerEliminations}      DEATHS  {statistics.PlayerDeaths}\n" +
                $"{durationLabel}  {duration / 60:00}:{duration % 60:00}\n" +
                $"{matchManager.GetScoreboardText()}\n" +
                $"LEVEL {career.level}  //  XP +{RoboClashSaveService.LastMatchXP}  " +
                $"({career.currentXP}/{RoboClashSaveService.GetXPRequiredForNextLevel(career.level)} XP)";
        }

        private void OnParticipantEliminated(ArenaDamageable eliminated, ArenaDamageable killer)
        {
            if (eliminated == null)
                return;

            if (eliminated == playerDamageable)
            {
                PushKillFeed("YOU WERE ELIMINATED", Red);
                return;
            }

            var victimName = GetRobotName(eliminated);
            if (killer == null)
                PushKillFeed($"{victimName} WAS ELIMINATED", Color.white);
            else
            {
                var killerName = killer == playerDamageable ? "YOU" : GetRobotName(killer);
                PushKillFeed($"{killerName} ELIMINATED {victimName}",
                    killer == playerDamageable ? Green : Color.white);
            }
        }

        private static string GetRobotName(ArenaDamageable damageable)
        {
            var definition = damageable.GetComponent<RoboClashRobotDefinition>();
            return definition != null
                ? definition.robotType.ToString().ToUpperInvariant()
                : damageable.name.ToUpperInvariant();
        }

        private void PushKillFeed(string entry, Color color)
        {
            for (var i = killFeedEntries.Length - 1; i > 0; i--)
            {
                killFeedEntries[i] = killFeedEntries[i - 1];
                killFeedColors[i] = killFeedColors[i - 1];
                killFeedExpiry[i] = killFeedExpiry[i - 1];
                killFeedTexts[i].text = killFeedEntries[i] ?? string.Empty;
                killFeedTexts[i].color = killFeedColors[i];
                killFeedTexts[i].gameObject.SetActive(!string.IsNullOrEmpty(killFeedEntries[i]));
            }

            killFeedEntries[0] = entry;
            killFeedColors[0] = color;
            killFeedExpiry[0] = Time.unscaledTime + 5f;
            killFeedTexts[0].text = entry;
            killFeedTexts[0].color = color;
            killFeedTexts[0].gameObject.SetActive(true);
        }

        private void ShowNotification(string message)
        {
            if (notificationText == null || string.IsNullOrEmpty(message))
                return;
            notificationText.text = message;
            notificationExpiry = Time.unscaledTime + 1.7f;
            notificationText.gameObject.SetActive(true);
        }

        private void PauseMatch()
        {
            matchManager?.PauseMatch();
        }

        private void ResumeMatch()
        {
            settingsPanel.Hide();
            matchManager?.ResumeMatch();
        }

        private void RestartMatch()
        {
            resultsRoot.SetActive(false);
            pauseRoot.SetActive(false);
            settingsPanel.Hide();
            matchManager?.RestartMatch();
        }

        private void ReturnToMainMenu()
        {
            matchManager?.ReturnToMainMenu();
        }

        private void OpenPauseSettings()
        {
            settingsPanel.Show();
        }

        private void ClosePauseSettings()
        {
            settingsPanel.Hide();
        }

        private GameObject CreateOverlay(string name)
        {
            var overlay = CreatePanel(canvas.transform, name, Vector2.zero, Vector2.one,
                new Color(0.005f, 0.018f, 0.035f, 0.92f));
            return overlay;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (parent != null)
                panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text CreateText(Transform parent, string value, int size, Color color,
            Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private Image CreateBar(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Color fillColor)
        {
            var background = CreatePanel(parent, name, anchorMin, anchorMax,
                new Color(0.04f, 0.09f, 0.13f, 1f));
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(background.transform, false);
            var rect = fillObject.GetComponent<RectTransform>();
            Stretch(rect);
            var image = fillObject.GetComponent<Image>();
            image.color = fillColor;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            image.raycastTarget = false;
            return image;
        }

        private Button CreateMenuButton(Transform parent, string label, Vector2 anchorMin,
            Vector2 anchorMax, Action action)
        {
            var buttonObject = CreatePanel(parent, label + " Button", anchorMin, anchorMax,
                new Color(0.035f, 0.16f, 0.23f, 1f));
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.65f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.55f, 0.8f, 0.92f, 1f);
            button.colors = colors;
            var labelText = CreateText(buttonObject.transform, label, 16, Color.white,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            labelText.fontStyle = FontStyle.Bold;
            button.onClick.AddListener(() => action());
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
````

### `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`

````csharp
using System;
using System.Collections.Generic;
using RoboClash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoboClash.UI
{
    internal static class RoboClashUIEventSystem
    {
        public static void EnsureForScene(GameObject owner, string eventSystemName)
        {
            if (owner == null)
            {
                Debug.LogError("RoboClash: Cannot configure UI input without an owner.");
                return;
            }

            var scene = owner.scene;
            var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>();
            var isMenuOwner = owner.GetComponent<RoboClashMenuController>() != null;
            EventSystem selected = null;
            InputSystemUIInputModule selectedInputModule = null;

            foreach (var candidate in eventSystems)
            {
                if (candidate.gameObject.scene != scene || !candidate.isActiveAndEnabled)
                    continue;

                if (candidate.gameObject.name == eventSystemName)
                {
                    selected = candidate;
                    selectedInputModule = candidate.GetComponent<InputSystemUIInputModule>();
                    break;
                }
            }

            if (selected == null && !isMenuOwner)
            {
                foreach (var candidate in eventSystems)
                {
                    if (candidate.gameObject.scene != scene || !candidate.isActiveAndEnabled)
                        continue;

                    var inputModule = candidate.GetComponent<InputSystemUIInputModule>();
                    if (inputModule != null && inputModule.isActiveAndEnabled)
                    {
                        selected = candidate;
                        selectedInputModule = inputModule;
                        break;
                    }

                    if (selected == null)
                        selected = candidate;
                }
            }

            if (selected == null)
            {
                var eventSystemObject = new GameObject(eventSystemName);
                eventSystemObject.transform.SetParent(owner.transform, false);
                selected = eventSystemObject.AddComponent<EventSystem>();
            }

            selected.enabled = true;
            selectedInputModule = selected.GetComponent<InputSystemUIInputModule>();
            if (selectedInputModule == null)
                selectedInputModule = selected.gameObject.AddComponent<InputSystemUIInputModule>();
            selectedInputModule.enabled = true;

            if (!HasEnabledPointerActions(selectedInputModule))
            {
                Debug.LogWarning(
                    "[RoboClash UI] The active InputSystemUIInputModule did not have enabled Point and Left Click actions. " +
                    "Reassigning the Input System UI default actions.",
                    selectedInputModule);
                selectedInputModule.enabled = false;
                selectedInputModule.UnassignActions();
                selectedInputModule.AssignDefaultActions();
                selectedInputModule.enabled = true;
            }

            foreach (var candidate in eventSystems)
            {
                if (candidate == null || candidate == selected || candidate.gameObject.scene != scene)
                    continue;

                foreach (var module in candidate.GetComponents<BaseInputModule>())
                    module.enabled = false;
                candidate.enabled = false;
            }

            foreach (var module in selected.GetComponents<BaseInputModule>())
            {
                if (module != selectedInputModule)
                    module.enabled = false;
            }

            EventSystem.current = selected;

            if (!HasEnabledPointerActions(selectedInputModule))
            {
                Debug.LogError(
                    "[RoboClash UI] InputSystemUIInputModule still has no enabled Point and Left Click actions after repair.",
                    selectedInputModule);
            }
        }

        private static bool HasEnabledPointerActions(InputSystemUIInputModule module)
        {
            if (module == null ||
                module.point == null || module.point.action == null || !module.point.action.enabled ||
                module.leftClick == null || module.leftClick.action == null ||
                !module.leftClick.action.enabled)
                return false;

            return Mouse.current == null ||
                   (module.point.action.controls.Count > 0 &&
                    module.leftClick.action.controls.Count > 0);
        }
    }

    [DefaultExecutionOrder(-32000)]
    public sealed class RoboClashMenuController : MonoBehaviour
    {
        private const string ArenaSceneName = "RoboClashGameplay";
        private const string MenuScenePath = "Assets/Scenes/RoboClashMainMenu.unity";
        [SerializeField] private GameObject[] robotPrefabs = new GameObject[4];

        private static readonly Color BackgroundColor = new Color(0.018f, 0.035f, 0.065f);
        private static readonly Color PanelColor = new Color(0.035f, 0.075f, 0.12f, 0.96f);
        private static readonly Color CyanColor = new Color(0.1f, 0.86f, 1f);
        private static readonly Color MutedColor = new Color(0.58f, 0.69f, 0.78f);
        private static readonly RoboClashRobotType[] RobotTypes =
        {
            RoboClashRobotType.Volt,
            RoboClashRobotType.Titan,
            RoboClashRobotType.Shadow,
            RoboClashRobotType.Spark
        };

        private readonly string[] roles =
        {
            "BALANCED FIGHTER",
            "HEAVY TANK",
            "FAST STEALTH FIGHTER",
            "SUPPORT"
        };

        private readonly string[] descriptions =
        {
            "A flexible front-line fighter built to adapt to any engagement.",
            "An armored powerhouse that trades speed for durability and impact.",
            "A fast, elusive attacker that strikes from unexpected angles.",
            "A versatile support robot with explosive area pressure."
        };

        private Canvas canvas;
        private Font font;
        private GameObject mainScreen;
        private GameObject selectionScreen;
        private GameObject settingsScreen;
        private GameObject matchSetupScreen;
        private GameObject matchSummaryScreen;
        private GameObject careerScreen;
        private Text selectionName;
        private Text selectionRole;
        private Text selectionDescription;
        private Text selectionStats;
        private Text selectionWeapon;
        private Text selectionAbility;
        private Text selectionUltimate;
        private Text matchSummaryText;
        private Text careerDetailsText;
        private Text matchStartErrorText;
        private Text opponentCountText;
        private Text opponentCountLabel;
        private Text modeSettingsHint;
        private Text modeRuleLabel;
        private Text selectedRobotText;
        private Text careerSummaryText;
        private Text featuredRobotName;
        private Button confirmButton;
        private Button[] robotCardButtons;
        private Image[] robotCardBackgrounds;
        private Button[] gameModeCardButtons;
        private Image[] gameModeCardBackgrounds;
        private RoboClashSettingsPanel settingsPanel;
        private Dropdown aiDifficultyDropdown;
        private Dropdown survivalGrowthDropdown;
        private Dropdown waveBreakDropdown;
        private Dropdown modeRuleDropdown;
        private Dropdown respawnDropdown;
        private Button decreaseOpponentButton;
        private Button increaseOpponentButton;
        private Button playButton;
        private Button startMatchButton;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        private Transform previewRoot;
        private GameObject previewRobot;
        private GameObject previewPrefab;
        private readonly Dictionary<Camera, int> cameraCullingMasks = new Dictionary<Camera, int>();
        private bool garageFromMatchSetup;
        private bool garageReturnsToSummary;
        private bool startingMatch;
        private int previewLayer = 31;
        private int selectedRobotIndex;
        private int opponentCount;
        private int survivalGrowth;
        private float waveBreakSeconds;
        private int matchTimerOption;
        private int killTargetOption;
        private int knockoutLives;
        private bool respawnsEnabled;
        private bool duelUsesKillTarget;
        private RoboClashAIDifficulty aiDifficulty;
        private RoboClashGameMode gameMode;

        private void Awake()
        {
            DisableGameplaySystemsInMenuScene();
            EnsureMenuCursor();
            selectedRobotIndex = Mathf.Clamp((int)RoboClashSession.SelectedRobot, 0, RobotTypes.Length - 1);
            opponentCount = RoboClashSession.OpponentCount;
            aiDifficulty = RoboClashSession.AIDifficulty;
            gameMode = RoboClashSession.GameMode == RoboClashGameMode.FreeForAll
                ? RoboClashGameMode.TimedDeathmatch
                : RoboClashSession.GameMode;
            survivalGrowth = RoboClashSession.SurvivalGrowth;
            waveBreakSeconds = RoboClashSession.WaveBreakSeconds;
            matchTimerOption = RoboClashSession.MatchTimerOption;
            killTargetOption = RoboClashSession.KillTargetOption;
            knockoutLives = RoboClashSession.KnockoutLives;
            respawnsEnabled = RoboClashSession.RespawnsEnabled;
            duelUsesKillTarget = RoboClashSession.DuelUsesKillTarget;
            EnsureEventSystem();
            RoboClashSaveService.ApplySettings();
            BuildInterface();
            ShowMainMenu();
        }

        private void OnEnable()
        {
            EnsureMenuCursor();
        }

        private void LateUpdate()
        {
            EnsureMenuCursor();
        }

        private void EnsureMenuCursor()
        {
            if (!string.Equals(
                    gameObject.scene.path,
                    MenuScenePath,
                    StringComparison.OrdinalIgnoreCase))
                return;

            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;
        }

        private void DisableGameplaySystemsInMenuScene()
        {
            foreach (var manager in FindObjectsByType<RoboClashMatchManager>(FindObjectsInactive.Include))
            {
                if (manager.gameObject.scene != gameObject.scene)
                    continue;

                var hud = manager.GetComponent<RoboClashHUD>();
                if (hud != null)
                    RemoveGameplayHud(hud);

                foreach (var eventSystem in manager.GetComponentsInChildren<EventSystem>(true))
                {
                    if (eventSystem.gameObject.name != "RoboClash Gameplay Event System")
                        continue;

                    foreach (var module in eventSystem.GetComponents<BaseInputModule>())
                        module.enabled = false;
                    eventSystem.enabled = false;
                    Destroy(eventSystem.gameObject);
                }

                manager.enabled = false;
                Destroy(manager);
                Debug.LogWarning(
                    "Removed a misplaced RoboClashMatchManager and its runtime HUD from the main-menu scene. " +
                    "Keep gameplay match systems in RoboClashGameplay.unity.",
                    manager);
            }

            foreach (var hud in FindObjectsByType<RoboClashHUD>(FindObjectsInactive.Include))
            {
                if (hud.gameObject.scene == gameObject.scene && hud.enabled)
                    RemoveGameplayHud(hud);
            }
        }

        private void RemoveGameplayHud(RoboClashHUD hud)
        {
            hud.enabled = false;
            foreach (var existingCanvas in hud.GetComponentsInChildren<Canvas>(true))
            {
                if (existingCanvas.gameObject.name == "RoboClash Gameplay Canvas")
                    Destroy(existingCanvas.gameObject);
            }

            Destroy(hud);
        }

        public void ConfigureRobotPrefabs(GameObject[] prefabs)
        {
            robotPrefabs = prefabs != null ? (GameObject[])prefabs.Clone() : Array.Empty<GameObject>();
            if (selectionName != null)
                UpdateRobotDetails();
        }

        private void Update()
        {
            if (previewRobot != null && (mainScreen.activeSelf || selectionScreen.activeSelf))
                previewRobot.transform.Rotate(0f, 18f * Time.unscaledDeltaTime, 0f, Space.World);

            if (UnityEngine.InputSystem.Keyboard.current == null ||
                !UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            if (settingsScreen.activeSelf)
            {
                settingsPanel.Hide();
                ShowMainMenu();
            }
            else if (selectionScreen.activeSelf)
                BackFromRobotGarage();
            else if (matchSummaryScreen.activeSelf)
                OpenRobotGarageFromSummary();
            else if (matchSetupScreen.activeSelf || careerScreen.activeSelf)
                ShowMainMenu();
        }

        private void OnDestroy()
        {
            foreach (var cameraEntry in cameraCullingMasks)
            {
                if (cameraEntry.Key != null)
                    cameraEntry.Key.cullingMask = cameraEntry.Value;
            }

            if (previewTexture != null)
            {
                previewTexture.Release();
                Destroy(previewTexture);
            }
        }

        private void EnsureEventSystem()
        {
            RoboClashUIEventSystem.EnsureForScene(gameObject, "RoboClash Menu Event System");
        }

        private void BuildInterface()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("RoboClash Menu Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildRobotPreviewCamera();
            mainScreen = CreateScreen("Main Menu");
            selectionScreen = CreateScreen("Robot Selection");
            matchSetupScreen = CreateScreen("Match Setup");
            matchSummaryScreen = CreateScreen("Match Summary");
            careerScreen = CreateScreen("Career Statistics");
            settingsScreen = CreateScreen("Settings");
            BuildMainScreen();
            BuildSelectionScreen();
            BuildMatchSetupScreen();
            BuildMatchSummaryScreen();
            BuildCareerScreen();
            BuildSettingsScreen();
        }

        private void BuildRobotPreviewCamera()
        {
            previewTexture = new RenderTexture(768, 768, 16, RenderTextureFormat.Default)
            {
                name = "RoboClash Menu Robot Preview",
                antiAliasing = 2
            };
            previewTexture.Create();
            previewRoot = new GameObject("Menu Robot Preview Root").transform;
            previewRoot.SetParent(transform, false);
            previewRoot.gameObject.SetActive(false);

            var cameraObject = new GameObject("Menu Robot Preview Camera");
            cameraObject.transform.SetParent(transform, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.targetTexture = previewTexture;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.018f, 0.035f, 0.065f);
            previewCamera.cullingMask = 1 << previewLayer;
            previewCamera.fieldOfView = 30f;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = true;
            foreach (var otherCamera in FindObjectsByType<Camera>())
            {
                if (otherCamera == previewCamera)
                    continue;
                cameraCullingMasks[otherCamera] = otherCamera.cullingMask;
                otherCamera.cullingMask &= ~(1 << previewLayer);
            }

            CreatePreviewLight("Menu Preview Key Light", new Vector3(35f, -32f, 0f),
                new Color(0.55f, 0.8f, 1f), 1.2f);
            CreatePreviewLight("Menu Preview Rim Light", new Vector3(18f, 145f, 0f),
                new Color(0.15f, 0.55f, 1f), 0.8f);
        }

        private void CreatePreviewLight(string lightName, Vector3 euler, Color color, float intensity)
        {
            var lightObject = new GameObject(lightName);
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(euler);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << previewLayer;
            light.shadows = LightShadows.None;
        }

        private RawImage CreateRobotPreview(Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var previewObject = new GameObject("Robot Render Preview",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            previewObject.transform.SetParent(parent, false);
            Place(previewObject.GetComponent<RectTransform>(), minimum, maximum, Vector2.zero, Vector2.zero);
            var image = previewObject.GetComponent<RawImage>();
            image.texture = previewTexture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private void UpdatePreviewRobot()
        {
            if (previewRoot == null)
                return;

            var prefab = robotPrefabs != null && selectedRobotIndex >= 0 &&
                         selectedRobotIndex < robotPrefabs.Length
                ? robotPrefabs[selectedRobotIndex]
                : null;
            if (previewRobot != null && previewPrefab == prefab)
                return;

            if (previewRobot != null)
            {
                previewRobot.SetActive(false);
                Destroy(previewRobot);
                previewRobot = null;
            }

            previewRoot.gameObject.SetActive(false);
            if (prefab == null)
            {
                previewPrefab = null;
                previewRoot.gameObject.SetActive(true);
                return;
            }

            previewRobot = Instantiate(prefab, previewRoot);
            previewPrefab = prefab;
            previewRobot.name = prefab.name + " Menu Preview";
            previewRobot.transform.localPosition = Vector3.zero;
            previewRobot.transform.localRotation = Quaternion.identity;
            foreach (var component in previewRobot.GetComponentsInChildren<Behaviour>(true))
                component.enabled = false;
            foreach (var collider in previewRobot.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (var rigidbody in previewRobot.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
            }
            foreach (var audioSource in previewRobot.GetComponentsInChildren<AudioSource>(true))
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }
            foreach (var animator in previewRobot.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
            foreach (var particleSystem in previewRobot.GetComponentsInChildren<ParticleSystem>(true))
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SetLayerRecursively(previewRobot.transform, previewLayer);
            previewRoot.gameObject.SetActive(true);

            var renderers = previewRobot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                previewCamera.transform.position = new Vector3(0f, 1.5f, -6f);
                previewCamera.transform.LookAt(Vector3.up);
                Debug.LogWarning($"Robot preview for '{prefab.name}' has no renderers.", this);
                return;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            var target = bounds.center;
            var distance = Mathf.Max(4f, bounds.extents.magnitude * 2.8f);
            previewCamera.transform.position = target + new Vector3(distance * 0.55f,
                bounds.extents.y * 0.25f, -distance);
            previewCamera.transform.LookAt(target);
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = distance * 4f;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        private void SetRobotPreviewVisible(bool visible)
        {
            if (previewRoot != null)
                previewRoot.gameObject.SetActive(visible);
        }

        private GameObject CreateScreen(string screenName)
        {
            var screen = CreatePanel(screenName, canvas.transform, BackgroundColor);
            Stretch(screen.GetComponent<RectTransform>());
            AddFullScreenAccent(screen.transform);
            return screen;
        }

        private void AddFullScreenAccent(Transform parent)
        {
            var topRule = CreatePanel("Cyan Accent", parent, CyanColor);
            Place(topRule.GetComponent<RectTransform>(), new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 6f));

            var rightBar = CreatePanel("Side Accent", parent, new Color(0.05f, 0.48f, 0.77f, 0.3f));
            Place(rightBar.GetComponent<RectTransform>(), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(-42f, 0f), new Vector2(2f, 0f));
        }

        private void BuildMainScreen()
        {
            AddText(mainScreen.transform, "ROBOCLASH", 58, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.075f, 0.77f), new Vector2(0.53f, 0.93f), Vector2.zero, Vector2.zero);
            AddText(mainScreen.transform, "MECH ARENA  //  SINGLE PLAYER", 16, CyanColor, TextAnchor.MiddleLeft,
                new Vector2(0.08f, 0.72f), new Vector2(0.51f, 0.78f), Vector2.zero, Vector2.zero);
            AddText(mainScreen.transform, "ENTER THE ARENA. ADAPT. ELIMINATE.", 17, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.08f, 0.65f), new Vector2(0.51f, 0.71f),
                Vector2.zero, Vector2.zero);
            CreateRobotPreview(mainScreen.transform,
                new Vector2(0.49f, 0.12f), new Vector2(0.97f, 0.87f));

            playButton = CreateButton(mainScreen.transform, "PLAY", new Vector2(0.08f, 0.51f),
                new Vector2(0.36f, 0.60f), CyanColor, OpenMatchSetup);
            CreateButton(mainScreen.transform, "ROBOT GARAGE", new Vector2(0.08f, 0.40f),
                new Vector2(0.36f, 0.49f), new Color(0.06f, 0.15f, 0.23f), OpenRobotGarage);
            CreateButton(mainScreen.transform, "SETTINGS", new Vector2(0.08f, 0.29f),
                new Vector2(0.36f, 0.38f), new Color(0.06f, 0.15f, 0.23f), OpenSettings);
            CreateButton(mainScreen.transform, "CAREER / STATISTICS", new Vector2(0.08f, 0.18f),
                new Vector2(0.36f, 0.27f), new Color(0.06f, 0.15f, 0.23f), OpenCareer);
            CreateButton(mainScreen.transform, "QUIT", new Vector2(0.08f, 0.07f),
                new Vector2(0.36f, 0.16f), new Color(0.12f, 0.11f, 0.16f), QuitGame);

            var info = CreatePanel("Featured Robot", mainScreen.transform, new Color(0.025f, 0.055f, 0.09f, 0.88f));
            Place(info.GetComponent<RectTransform>(), new Vector2(0.55f, 0.12f), new Vector2(0.92f, 0.30f),
                Vector2.zero, Vector2.zero);
            featuredRobotName = AddText(info.transform, string.Empty, 27, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);
            selectedRobotText = AddText(info.transform, string.Empty, 13, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.48f), Vector2.zero, Vector2.zero);
            careerSummaryText = AddText(mainScreen.transform, string.Empty, 12, MutedColor,
                TextAnchor.LowerLeft, new Vector2(0.08f, 0.01f), new Vector2(0.48f, 0.065f),
                Vector2.zero, Vector2.zero);
        }

        private void BuildSelectionScreen()
        {
            AddText(selectionScreen.transform, "ROBOT GARAGE", 38, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.88f), new Vector2(0.75f, 0.97f), Vector2.zero, Vector2.zero);
            AddText(selectionScreen.transform, "SELECT A FRAME TO REVIEW ITS LIVE COMBAT PROFILE", 15, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.065f, 0.82f), new Vector2(0.8f, 0.89f),
                Vector2.zero, Vector2.zero);

            robotCardButtons = new Button[RobotTypes.Length];
            robotCardBackgrounds = new Image[RobotTypes.Length];
            for (var i = 0; i < RobotTypes.Length; i++)
            {
                var index = i;
                var card = CreateButton(selectionScreen.transform, RobotTypes[i].ToString().ToUpperInvariant(),
                    new Vector2(0.06f + i * 0.19f, 0.69f), new Vector2(0.235f + i * 0.19f, 0.80f),
                    PanelColor, () => SelectRobot(index));
                robotCardButtons[i] = card;
                robotCardBackgrounds[i] = card.GetComponent<Image>();
            }

            var details = CreatePanel("Robot Details", selectionScreen.transform, PanelColor);
            Place(details.GetComponent<RectTransform>(), new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.65f),
                Vector2.zero, Vector2.zero);
            selectionName = AddText(details.transform, string.Empty, 32, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.78f), new Vector2(0.66f, 0.96f), Vector2.zero, Vector2.zero);
            selectionRole = AddText(details.transform, string.Empty, 15, CyanColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.69f), new Vector2(0.66f, 0.81f), Vector2.zero, Vector2.zero);
            selectionDescription = AddText(details.transform, string.Empty, 15, MutedColor, TextAnchor.UpperLeft,
                new Vector2(0.04f, 0.55f), new Vector2(0.70f, 0.69f), Vector2.zero, Vector2.zero);
            selectionStats = AddText(details.transform, string.Empty, 17, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.04f, 0.29f), new Vector2(0.40f, 0.54f), Vector2.zero, Vector2.zero);
            selectionWeapon = AddText(details.transform, string.Empty, 16, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.43f, 0.47f), new Vector2(0.70f, 0.60f), Vector2.zero, Vector2.zero);
            selectionAbility = AddText(details.transform, string.Empty, 16, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.43f, 0.33f), new Vector2(0.70f, 0.47f), Vector2.zero, Vector2.zero);
            selectionUltimate = AddText(details.transform, string.Empty, 16, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.43f, 0.18f), new Vector2(0.70f, 0.33f), Vector2.zero, Vector2.zero);
            selectionStats.fontSize = 14;
            selectionWeapon.fontSize = 14;
            selectionAbility.fontSize = 12;
            selectionUltimate.fontSize = 12;
            CreateRobotPreview(details.transform,
                new Vector2(0.72f, 0.08f), new Vector2(0.98f, 0.94f));

            CreateButton(selectionScreen.transform, "BACK",
                new Vector2(0.06f, 0.08f), new Vector2(0.23f, 0.17f),
                new Color(0.06f, 0.15f, 0.23f), BackFromRobotGarage);
            confirmButton = CreateButton(selectionScreen.transform, "SELECT ROBOT",
                new Vector2(0.73f, 0.08f), new Vector2(0.94f, 0.17f), CyanColor, ConfirmRobotSelection);
            UpdateRobotDetails();
        }

        private void BuildMatchSetupScreen()
        {
            AddText(matchSetupScreen.transform, "CHOOSE YOUR GAME MODE", 34, Color.white,
                TextAnchor.MiddleLeft, new Vector2(0.055f, 0.89f), new Vector2(0.68f, 0.97f),
                Vector2.zero, Vector2.zero);
            AddText(matchSetupScreen.transform, "SELECT A MODE, THEN TUNE ONLY THE RULES THAT APPLY",
                13, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.84f), new Vector2(0.68f, 0.90f), Vector2.zero, Vector2.zero);

            var descriptionsByMode = new[]
            {
                "TARGET: 5 / 10 / 15 / 20",
                "TIME: 3 / 5 / 7 MINUTES",
                "2 COMBATANTS // KILLS OR TIME",
                "PROGRESSIVE WAVES",
                "CONTROL THE CENTRAL ZONE",
                "LIVES: 1 / 3 / 5"
            };
            var modeKeys = new[] { "KR", "TD", "VS", "SW", "KA", "KO" };
            gameModeCardButtons = new Button[6];
            gameModeCardBackgrounds = new Image[6];
            for (var i = 0; i < gameModeCardButtons.Length; i++)
            {
                var index = i;
                var column = i % 3;
                var row = i / 3;
                var left = 0.06f + column * 0.205f;
                var bottom = row == 0 ? 0.49f : 0.17f;
                var card = CreateButton(matchSetupScreen.transform,
                    modeKeys[i] + "\n" + GetGameModeFromOption(i).ToString()                    .Replace("KingOfTheArena", "KING OF THE ARENA")
                        .Replace("TimedDeathmatch", "TIMED DEATHMATCH").Replace("KillRace", "KILL RACE")
                        .Replace("Survival", "SURVIVAL WAVES").Replace("Knockout", "KNOCKOUT").Replace("Duel", "DUEL")
                        .ToUpperInvariant() + "\n" + descriptionsByMode[i],
                    new Vector2(left, bottom), new Vector2(left + 0.19f, bottom + 0.29f),
                    PanelColor, () => SelectGameModeCard(index));
                gameModeCardButtons[i] = card;
                gameModeCardBackgrounds[i] = card.GetComponent<Image>();
                var label = card.GetComponentInChildren<Text>();
                label.fontSize = 12;
                label.supportRichText = false;
                label.lineSpacing = 1.15f;
            }

            var optionsPanel = CreatePanel("Match Rules", matchSetupScreen.transform, PanelColor);
            Place(optionsPanel.GetComponent<RectTransform>(), new Vector2(0.70f, 0.15f),
                new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
            AddText(optionsPanel.transform, "MATCH RULES", 18, CyanColor, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.99f), Vector2.zero, Vector2.zero);
            AddText(optionsPanel.transform, "AI DIFFICULTY", 12, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.87f), Vector2.zero, Vector2.zero);
            aiDifficultyDropdown = CreateDropdown(optionsPanel.transform,
                new Vector2(0.04f, 0.71f), new Vector2(0.96f, 0.79f));
            aiDifficultyDropdown.ClearOptions();
            aiDifficultyDropdown.AddOptions(new List<string> { "EASY", "NORMAL", "HARD" });
            aiDifficultyDropdown.SetValueWithoutNotify((int)aiDifficulty);
            aiDifficultyDropdown.RefreshShownValue();
            aiDifficultyDropdown.onValueChanged.AddListener(SetAIDifficulty);
            opponentCountLabel = AddText(optionsPanel.transform, "AI OPPONENTS", 13, MutedColor,
                TextAnchor.MiddleCenter, new Vector2(0.02f, 0.61f), new Vector2(0.98f, 0.69f),
                Vector2.zero, Vector2.zero);
            decreaseOpponentButton = CreateButton(optionsPanel.transform, "-",
                new Vector2(0.1f, 0.48f), new Vector2(0.39f, 0.60f),
                new Color(0.06f, 0.15f, 0.23f), DecreaseOpponents);
            increaseOpponentButton = CreateButton(optionsPanel.transform, "+",
                new Vector2(0.61f, 0.48f), new Vector2(0.9f, 0.60f),
                new Color(0.06f, 0.15f, 0.23f), IncreaseOpponents);
            opponentCountText = AddText(optionsPanel.transform, string.Empty, 28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.38f, 0.48f), new Vector2(0.62f, 0.60f), Vector2.zero, Vector2.zero);
            modeSettingsHint = AddText(optionsPanel.transform, string.Empty, 11, CyanColor,
                TextAnchor.MiddleCenter, new Vector2(0.02f, 0.41f), new Vector2(0.98f, 0.47f),
                Vector2.zero, Vector2.zero);
            modeRuleLabel = AddText(optionsPanel.transform, "MODE RULES", 12, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.04f, 0.33f), new Vector2(0.96f, 0.40f),
                Vector2.zero, Vector2.zero);
            modeRuleDropdown = CreateModeRuleDropdown(optionsPanel.transform);
            respawnDropdown = CreateRespawnDropdown(optionsPanel.transform);
            survivalGrowthDropdown = CreateSurvivalGrowthDropdown(optionsPanel.transform);
            waveBreakDropdown = CreateWaveBreakDropdown(optionsPanel.transform);

            CreateButton(matchSetupScreen.transform, "BACK", new Vector2(0.06f, 0.06f),
                new Vector2(0.23f, 0.14f), new Color(0.06f, 0.15f, 0.23f), ShowMainMenu);
            CreateButton(matchSetupScreen.transform, "CHOOSE ROBOT",
                new Vector2(0.73f, 0.06f), new Vector2(0.95f, 0.14f), CyanColor,
                () => OpenRobotGarage(true));
            UpdateGameModeCards();
        }

        private void BuildMatchSummaryScreen()
        {
            AddText(matchSummaryScreen.transform, "MATCH SUMMARY", 38, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.82f), new Vector2(0.88f, 0.94f), Vector2.zero, Vector2.zero);
            var summaryPanel = CreatePanel("Selected Match", matchSummaryScreen.transform, PanelColor);
            Place(summaryPanel.GetComponent<RectTransform>(), new Vector2(0.20f, 0.27f),
                new Vector2(0.80f, 0.79f), Vector2.zero, Vector2.zero);
            matchSummaryText = AddText(summaryPanel.transform, string.Empty, 22, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.90f),
                Vector2.zero, Vector2.zero);
            matchStartErrorText = AddText(matchSummaryScreen.transform, string.Empty, 12,
                new Color(1f, 0.34f, 0.3f), TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.20f), new Vector2(0.88f, 0.25f), Vector2.zero, Vector2.zero);
            CreateButton(matchSummaryScreen.transform, "EDIT RULES", new Vector2(0.12f, 0.10f),
                new Vector2(0.34f, 0.19f), new Color(0.06f, 0.15f, 0.23f), OpenMatchSetup);
            CreateButton(matchSummaryScreen.transform, "CHANGE ROBOT", new Vector2(0.39f, 0.10f),
                new Vector2(0.61f, 0.19f), new Color(0.06f, 0.15f, 0.23f),
                OpenRobotGarageFromSummary);
            startMatchButton = CreateButton(matchSummaryScreen.transform, "START MATCH",
                new Vector2(0.66f, 0.10f), new Vector2(0.88f, 0.19f), CyanColor, StartSelectedMatch);
        }

        private void BuildCareerScreen()
        {
            AddText(careerScreen.transform, "CAREER // STATISTICS", 38, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.83f), new Vector2(0.90f, 0.94f), Vector2.zero, Vector2.zero);
            var panel = CreatePanel("Career Record", careerScreen.transform, PanelColor);
            Place(panel.GetComponent<RectTransform>(), new Vector2(0.16f, 0.24f),
                new Vector2(0.84f, 0.78f), Vector2.zero, Vector2.zero);
            careerDetailsText = AddText(panel.transform, string.Empty, 21, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            CreateButton(careerScreen.transform, "RESET CAREER", new Vector2(0.18f, 0.09f),
                new Vector2(0.40f, 0.18f), new Color(0.12f, 0.11f, 0.16f), ConfirmCareerReset);
            CreateButton(careerScreen.transform, "BACK", new Vector2(0.60f, 0.09f),
                new Vector2(0.82f, 0.18f), new Color(0.06f, 0.15f, 0.23f), ShowMainMenu);
        }

        private void BuildSettingsScreen()
        {
            settingsPanel = gameObject.AddComponent<RoboClashSettingsPanel>();
            settingsPanel.Initialize(canvas.transform, font, ShowMainMenu);
        }

        private void ShowMainMenu()
        {
            settingsPanel?.Hide();
            mainScreen.SetActive(true);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(true);
            garageFromMatchSetup = false;
            garageReturnsToSummary = false;
            UpdateCurrentSelectionLabel();
            RefreshCareerSummary();
            RefreshCareerDetails();
            UpdateRobotDetails();
            SelectFirstSelectable(mainScreen);
        }

        private void OpenMatchSetup()
        {
            settingsPanel?.Hide();
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(true);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(false);
            garageReturnsToSummary = false;
            UpdateGameModeCards();
            aiDifficultyDropdown.SetValueWithoutNotify((int)aiDifficulty);
            aiDifficultyDropdown.RefreshShownValue();
            survivalGrowthDropdown.SetValueWithoutNotify(Mathf.Clamp(survivalGrowth - 1, 0, 2));
            waveBreakDropdown.SetValueWithoutNotify(GetWaveBreakOption(waveBreakSeconds));
            survivalGrowthDropdown.RefreshShownValue();
            waveBreakDropdown.RefreshShownValue();
            UpdateModeOptions();
            UpdateOpponentCountLabel();
            SelectFirstSelectable(matchSetupScreen);
        }

        private void OpenRobotGarage()
        {
            OpenRobotGarage(false);
        }

        private void OpenRobotGarage(bool fromMatchSetup)
        {
            settingsPanel?.Hide();
            garageFromMatchSetup = fromMatchSetup;
            garageReturnsToSummary = false;
            mainScreen.SetActive(false);
            selectionScreen.SetActive(true);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(true);
            SelectRobot(selectedRobotIndex);
            SelectFirstSelectable(selectionScreen);
        }

        private void OpenRobotGarageFromSummary()
        {
            OpenRobotGarage(true);
            garageReturnsToSummary = true;
        }

        private void BackFromRobotGarage()
        {
            if (garageReturnsToSummary)
                OpenMatchSummary();
            else if (garageFromMatchSetup)
                OpenMatchSetup();
            else
                ShowMainMenu();
        }

        private void ConfirmRobotSelection()
        {
            if (GetDefinition(RobotTypes[selectedRobotIndex]) == null)
            {
                confirmButton.interactable = false;
                Debug.LogError("The selected robot prefab is missing its RoboClashRobotDefinition.", this);
                return;
            }

            if (garageFromMatchSetup)
                OpenMatchSummary();
            else
                ShowMainMenu();
        }

        private void OpenMatchSummary()
        {
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(true);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(false);
            garageReturnsToSummary = false;
            RefreshMatchSummary();
            SelectFirstSelectable(matchSummaryScreen);
        }

        private void OpenCareer()
        {
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(true);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(false);
            RefreshCareerDetails();
            SelectFirstSelectable(careerScreen);
        }

        private void OpenSettings()
        {
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(true);
            SetRobotPreviewVisible(false);
            settingsPanel.Show();
            SelectFirstSelectable(settingsPanel.Root);
        }

        private static void SelectFirstSelectable(GameObject root)
        {
            var eventSystem = EventSystem.current;
            if (root == null || eventSystem == null)
                return;

            foreach (var selectable in root.GetComponentsInChildren<Selectable>())
            {
                if (!selectable.isActiveAndEnabled || !selectable.interactable)
                    continue;

                eventSystem.SetSelectedGameObject(selectable.gameObject);
                return;
            }
        }

        private void SelectRobot(int index)
        {
            if (index < 0 || index >= RobotTypes.Length)
                return;

            selectedRobotIndex = index;
            RoboClashSession.SaveSelection(RobotTypes[index], opponentCount);
            UpdateRobotDetails();
            UpdateCurrentSelectionLabel();
        }

        private void UpdateRobotDetails()
        {
            if (selectionName == null)
                return;

            var type = RobotTypes[selectedRobotIndex];
            var definition = GetDefinition(type);
            var accent = GetAccent(type, definition);
            selectionName.text = type.ToString().ToUpperInvariant();
            selectionName.color = accent;
            if (featuredRobotName != null)
            {
                featuredRobotName.text = type.ToString().ToUpperInvariant();
                featuredRobotName.color = accent;
            }
            selectionRole.text = roles[selectedRobotIndex];
            selectionRole.color = accent;
            selectionDescription.text = definition != null
                ? descriptions[selectedRobotIndex] + "\nPASSIVE  " + definition.passiveName
                : descriptions[selectedRobotIndex];
            UpdatePreviewRobot();
            if (definition == null || definition.stats == null)
            {
                selectionStats.text = "Robot prefab unavailable.";
                selectionWeapon.text = string.Empty;
                selectionAbility.text = string.Empty;
                selectionUltimate.text = string.Empty;
                if (confirmButton != null)
                    confirmButton.interactable = false;
            }
            else
            {
                var stats = definition.stats;
                selectionStats.text =
                    $"HEALTH {stats.maxHealth:0}  //  SHIELD {stats.maxShield:0}\n" +
                    $"SPEED {stats.moveSpeed:0.0} m/s\n" +
                    $"DAMAGE {stats.damagePerShot:0.#}  //  {stats.shotsPerSecond:0.##}/s\n" +
                    $"MAG {stats.magazineSize}  //  RELOAD {stats.reloadDuration:0.##}s\n" +
                    $"RANGE {stats.weaponRange:0.#} m";
                selectionWeapon.text = $"WEAPON  {definition.weaponName}";
                selectionAbility.text =
                    $"ABILITY  {definition.abilityName}  //  {definition.abilityDuration:0.##}s, " +
                    $"{definition.abilityCooldown:0.#}s CD\n{GetAbilitySummary(type)}";
                selectionUltimate.text =
                    $"ULTIMATE  {definition.ultimateName}  //  {definition.ultimateDuration:0.##}s, " +
                    $"{definition.ultimateCooldown:0.#}s CD\n{GetUltimateSummary(type)}";
                if (confirmButton != null)
                    confirmButton.interactable = true;
            }

            for (var i = 0; i < robotCardBackgrounds.Length; i++)
            {
                var selected = i == selectedRobotIndex;
                var cardAccent = GetAccent(RobotTypes[i], GetDefinition(RobotTypes[i]));
                robotCardBackgrounds[i].color = selected
                    ? new Color(cardAccent.r, cardAccent.g, cardAccent.b, 0.34f)
                    : PanelColor;
                var label = robotCardButtons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = selected ? cardAccent : Color.white;
            }
        }

        private void UpdateCurrentSelectionLabel()
        {
            if (selectedRobotText != null)
                selectedRobotText.text = "CURRENT ROBOT  //  " +
                                         RobotTypes[selectedRobotIndex].ToString().ToUpperInvariant();
        }

        private static string GetAbilitySummary(RoboClashRobotType type)
        {
            switch (type)
            {
                case RoboClashRobotType.Volt: return "Dashes 5 m forward.";
                case RoboClashRobotType.Titan: return "Reduces incoming damage by 45%.";
                case RoboClashRobotType.Shadow: return "Cloaks until fired or the effect ends.";
                case RoboClashRobotType.Spark: return "Repairs 4 HP/s and restores 5 shield/s.";
                default: return string.Empty;
            }
        }

        private static string GetUltimateSummary(RoboClashRobotType type)
        {
            switch (type)
            {
                case RoboClashRobotType.Volt: return "+30% move speed and +35% fire rate.";
                case RoboClashRobotType.Titan: return "Fires a 90-damage rail projectile.";
                case RoboClashRobotType.Shadow: return "Strikes toward a nearby rival for 70 damage.";
                case RoboClashRobotType.Spark: return "8 m EMP; slows nearby rivals for 3 s.";
                default: return string.Empty;
            }
        }

        private void SelectGameModeCard(int index)
        {
            if (index < 0 || index >= gameModeCardButtons.Length)
                return;

            gameMode = GetGameModeFromOption(index);
            if (gameMode == RoboClashGameMode.Duel)
                opponentCount = 1;
            else
                opponentCount = Mathf.Clamp(opponentCount, 1,
                    gameMode == RoboClashGameMode.Survival ? 24 : 3);
            UpdateModeOptions();
            UpdateOpponentCountLabel();
            UpdateGameModeCards();
        }

        private void UpdateGameModeCards()
        {
            if (gameModeCardBackgrounds == null)
                return;

            var selectedIndex = GetGameModeOption(gameMode);
            for (var i = 0; i < gameModeCardBackgrounds.Length; i++)
            {
                var isSelected = i == selectedIndex;
                gameModeCardBackgrounds[i].color = isSelected
                    ? new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.34f)
                    : PanelColor;
                var label = gameModeCardButtons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = isSelected ? CyanColor : Color.white;
            }
        }

        private void RefreshMatchSummary()
        {
            if (matchSummaryText == null)
                return;

            var modeRules = string.Empty;
            switch (gameMode)
            {
                case RoboClashGameMode.KillRace:
                    modeRules = $"FIRST TO {GetKillTarget()} ELIMINATIONS // RESPAWNS ON";
                    break;
                case RoboClashGameMode.TimedDeathmatch:
                    modeRules = $"{GetMatchDuration()} MINUTES // MOST ELIMINATIONS WINS // RESPAWNS ON";
                    break;
                case RoboClashGameMode.Duel:
                    modeRules = duelUsesKillTarget
                        ? $"FIRST TO {GetKillTarget()} ELIMINATIONS // RESPAWNS {(respawnsEnabled ? "ON" : "OFF")}"
                        : $"{GetMatchDuration()} MINUTES // RESPAWNS {(respawnsEnabled ? "ON" : "OFF")}";
                    break;
                case RoboClashGameMode.Survival:
                    modeRules = $"{opponentCount} STARTING ENEMIES // +{survivalGrowth} PER WAVE // " +
                                $"{waveBreakSeconds:0.#}s BREAK";
                    break;
                case RoboClashGameMode.KingOfTheArena:
                    modeRules = $"{GetMatchDuration()} MINUTES // CONTROL THE CENTRAL ZONE // " +
                                $"RESPAWNS {(respawnsEnabled ? "ON" : "OFF")}";
                    break;
                case RoboClashGameMode.Knockout:
                    modeRules = $"{knockoutLives} LIVES PER ROBOT // LAST ROBOT STANDING WINS";
                    break;
                default:
                    Debug.LogError($"Unsupported match mode selected: {gameMode}.", this);
                    modeRules = "INVALID MODE";
                    break;
            }

            matchSummaryText.text =
                $"ROBOT  //  {RobotTypes[selectedRobotIndex].ToString().ToUpperInvariant()}\n\n" +
                $"MODE  //  {GetGameModeLabel(gameMode)}\n\n" +
                $"RULES  //  {modeRules}\n\n" +
                $"COMBATANTS  //  {(gameMode == RoboClashGameMode.Survival ? opponentCount : opponentCount + 1)}\n\n" +
                $"AI DIFFICULTY  //  {aiDifficulty.ToString().ToUpperInvariant()}\n\n" +
                "ARENA  //  ROBOCLASH ARENA (PROCEDURAL)";
        }

        private int GetKillTarget()
        {
            return new[] { 5, 10, 15, 20 }[Mathf.Clamp(killTargetOption, 0, 3)];
        }

        private int GetMatchDuration()
        {
            return new[] { 3, 5, 7 }[Mathf.Clamp(matchTimerOption, 0, 2)];
        }

        private void RefreshCareerDetails()
        {
            if (careerDetailsText == null)
                return;

            var career = RoboClashSaveService.Current.career;
            var requiredXP = RoboClashSaveService.GetXPRequiredForNextLevel(career.level);
            var playTime = TimeSpan.FromSeconds(Mathf.Max(0f, career.totalPlayTimeSeconds));
            var playTimeText = playTime.TotalHours >= 1
                ? $"{(int)playTime.TotalHours}h {playTime.Minutes}m"
                : $"{playTime.Minutes}m";
            careerDetailsText.text =
                $"LEVEL  {career.level}     XP  {career.currentXP} / {requiredXP}     LIFETIME XP  {career.lifetimeXP}\n\n" +
                $"MATCHES  {career.totalMatches}     WINS  {career.totalWins}     LOSSES  {career.totalLosses}     DRAWS  {career.totalDraws}\n\n" +
                $"ELIMINATIONS  {career.totalEliminations}     DAMAGE DEALT  {career.totalDamageDealt:0}\n\n" +
                $"BEST SURVIVAL WAVE  {career.highestSurvivalWave}     PLAY TIME  {playTimeText}\n\n" +
                $"MOST RECENT ROBOT  {career.preferredRobotId}\n\n" +
                (career.totalMatches == 0 ? "NO MATCH RESULTS RECORDED YET." : "STATISTICS FROM SAVED MATCH RESULTS.");
        }

        private void UpdateOpponentCountLabel()
        {
            if (opponentCountText != null)
                opponentCountText.text = gameMode == RoboClashGameMode.Survival
                    ? opponentCount.ToString()
                    : (opponentCount + 1).ToString();
            if (opponentCountLabel != null)
                opponentCountLabel.text = gameMode == RoboClashGameMode.Survival
                    ? "STARTING ENEMIES"
                    : "COMBATANTS (2-4)";
            RoboClashSession.SaveSelection(RobotTypes[selectedRobotIndex], opponentCount);
            UpdateCurrentSelectionLabel();
        }

        private void IncreaseOpponents()
        {
            var maximum = gameMode == RoboClashGameMode.Survival ? 24 : 3;
            opponentCount = Mathf.Min(maximum, opponentCount + 1);
            UpdateOpponentCountLabel();
        }

        private void DecreaseOpponents()
        {
            opponentCount = Mathf.Max(1, opponentCount - 1);
            UpdateOpponentCountLabel();
        }

        private Dropdown CreateModeRuleDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.32f));
            dropdown.onValueChanged.AddListener(SetModeRule);
            return dropdown;
        }

        private Dropdown CreateRespawnDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.04f, 0.09f), new Vector2(0.96f, 0.20f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "RESPAWN ON", "NO RESPAWN" });
            dropdown.SetValueWithoutNotify(respawnsEnabled ? 0 : 1);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(SetRespawns);
            return dropdown;
        }

        private void UpdateModeOptions()
        {
            var duel = gameMode == RoboClashGameMode.Duel;
            var survival = gameMode == RoboClashGameMode.Survival;
            var knockout = gameMode == RoboClashGameMode.Knockout;
            var king = gameMode == RoboClashGameMode.KingOfTheArena;
            var countLimit = survival ? 24 : 3;
            opponentCount = Mathf.Clamp(opponentCount, 1, countLimit);
            decreaseOpponentButton.gameObject.SetActive(!duel);
            increaseOpponentButton.gameObject.SetActive(!duel);
            opponentCountLabel.gameObject.SetActive(true);
            opponentCountText.gameObject.SetActive(true);
            survivalGrowthDropdown.gameObject.SetActive(survival);
            waveBreakDropdown.gameObject.SetActive(survival);
            modeSettingsHint.gameObject.SetActive(true);
            modeSettingsHint.text = survival
                ? "WAVES GROW BY +" + survivalGrowth + " ENEMIES"
                : duel
                    ? "2 COMBATANTS // PLAYER VS AI"
                    : "COMBATANTS: " + (opponentCount + 1);
            modeRuleDropdown.gameObject.SetActive(!survival);
            respawnDropdown.gameObject.SetActive(duel || king);
            var ruleOptions = new List<string>();
            var ruleLabel = "MODE RULES";
            var selectedRule = 0;
            if (gameMode == RoboClashGameMode.KillRace)
            {
                ruleLabel = "KILL TARGET";
                ruleOptions.AddRange(new[] { "5 KILLS", "10 KILLS", "15 KILLS", "20 KILLS" });
                selectedRule = killTargetOption;
            }
            else if (gameMode == RoboClashGameMode.TimedDeathmatch ||
                     gameMode == RoboClashGameMode.KingOfTheArena)
            {
                ruleLabel = "MATCH DURATION";
                ruleOptions.AddRange(new[] { "3 MINUTES", "5 MINUTES", "7 MINUTES" });
                selectedRule = matchTimerOption;
            }
            else if (gameMode == RoboClashGameMode.Duel)
            {
                ruleLabel = "DUEL WIN RULE";
                ruleOptions.AddRange(new[]
                {
                    "5 KILLS", "10 KILLS", "15 KILLS", "20 KILLS",
                    "3 MINUTES", "5 MINUTES", "7 MINUTES"
                });
                selectedRule = duelUsesKillTarget ? killTargetOption : 4 + matchTimerOption;
            }
            else if (knockout)
            {
                ruleLabel = "LIVES PER ROBOT";
                ruleOptions.AddRange(new[] { "1 LIFE", "3 LIVES", "5 LIVES" });
                selectedRule = knockoutLives <= 1 ? 0 : knockoutLives <= 3 ? 1 : 2;
            }

            modeRuleDropdown.ClearOptions();
            modeRuleDropdown.AddOptions(ruleOptions);
            modeRuleDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedRule, 0, Mathf.Max(0, ruleOptions.Count - 1)));
            modeRuleDropdown.RefreshShownValue();
            modeRuleLabel.text = ruleLabel;
            modeRuleLabel.gameObject.SetActive(!survival);
            respawnDropdown.SetValueWithoutNotify(respawnsEnabled ? 0 : 1);
            respawnDropdown.RefreshShownValue();
        }

        private static string GetGameModeLabel(RoboClashGameMode mode)
        {
            switch (mode)
            {
                case RoboClashGameMode.KillRace: return "KILL RACE";
                case RoboClashGameMode.TimedDeathmatch:
                case RoboClashGameMode.FreeForAll: return "TIMED DEATHMATCH";
                case RoboClashGameMode.Duel: return "DUEL";
                case RoboClashGameMode.Survival: return "SURVIVAL WAVES";
                case RoboClashGameMode.KingOfTheArena: return "KING OF THE ARENA";
                case RoboClashGameMode.Knockout: return "KNOCKOUT";
                default: return "TIMED DEATHMATCH";
            }
        }

        private static RoboClashGameMode GetGameModeFromOption(int option)
        {
            switch (option)
            {
                case 0: return RoboClashGameMode.KillRace;
                case 1: return RoboClashGameMode.TimedDeathmatch;
                case 2: return RoboClashGameMode.Duel;
                case 3: return RoboClashGameMode.Survival;
                case 4: return RoboClashGameMode.KingOfTheArena;
                case 5: return RoboClashGameMode.Knockout;
                default: return RoboClashGameMode.TimedDeathmatch;
            }
        }

        private static int GetGameModeOption(RoboClashGameMode mode)
        {
            switch (mode)
            {
                case RoboClashGameMode.KillRace: return 0;
                case RoboClashGameMode.Duel: return 2;
                case RoboClashGameMode.Survival: return 3;
                case RoboClashGameMode.KingOfTheArena: return 4;
                case RoboClashGameMode.Knockout: return 5;
                default: return 1;
            }
        }

        private void SetModeRule(int option)
        {
            if (gameMode == RoboClashGameMode.KillRace)
                killTargetOption = Mathf.Clamp(option, 0, 3);
            else if (gameMode == RoboClashGameMode.TimedDeathmatch ||
                     gameMode == RoboClashGameMode.KingOfTheArena)
                matchTimerOption = Mathf.Clamp(option, 0, 2);
            else if (gameMode == RoboClashGameMode.Duel)
            {
                duelUsesKillTarget = option < 4;
                if (duelUsesKillTarget)
                    killTargetOption = Mathf.Clamp(option, 0, 3);
                else
                    matchTimerOption = Mathf.Clamp(option - 4, 0, 2);
            }
            else if (gameMode == RoboClashGameMode.Knockout)
                knockoutLives = option == 0 ? 1 : option == 1 ? 3 : 5;

            RoboClashSession.SaveMatchTimerOption(matchTimerOption);
            RoboClashSession.SaveModeRules(killTargetOption, knockoutLives, respawnsEnabled,
                duelUsesKillTarget);
        }

        private void SetRespawns(int option)
        {
            respawnsEnabled = option == 0;
            RoboClashSession.SaveModeRules(killTargetOption, knockoutLives, respawnsEnabled,
                duelUsesKillTarget);
        }

        private void StartSelectedMatch()
        {
            if (startingMatch)
                return;

            if (selectedRobotIndex < 0 || selectedRobotIndex >= RobotTypes.Length ||
                GetDefinition(RobotTypes[selectedRobotIndex])?.stats == null)
            {
                ShowMatchStartError("The selected robot prefab is missing its robot definition.");
                return;
            }

            if (!Enum.IsDefined(typeof(RoboClashGameMode), gameMode) ||
                gameMode == RoboClashGameMode.FreeForAll)
            {
                ShowMatchStartError("Select one of the supported match modes before starting.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(ArenaSceneName))
            {
                ShowMatchStartError(
                    $"Gameplay scene '{ArenaSceneName}' is not available in Build Settings. " +
                    "Add Assets/Scenes/RoboClashGameplay.unity to Build Settings and try again.");
                return;
            }

            if (matchStartErrorText != null)
                matchStartErrorText.text = string.Empty;
            startingMatch = true;
            if (startMatchButton != null)
                startMatchButton.interactable = false;

            try
            {
                RoboClashSession.BeginMatch(RobotTypes[selectedRobotIndex], opponentCount, aiDifficulty,
                    gameMode, survivalGrowth, waveBreakSeconds, matchTimerOption,
                    killTargetOption, knockoutLives, respawnsEnabled, duelUsesKillTarget);
                canvas.gameObject.SetActive(false);
                SceneManager.LoadScene(ArenaSceneName);
            }
            catch (UnityException exception)
            {
                HandleMatchStartFailure(exception);
            }
            catch (ArgumentException exception)
            {
                HandleMatchStartFailure(exception);
            }
        }

        private void HandleMatchStartFailure(Exception exception)
        {
            startingMatch = false;
            if (canvas != null)
                canvas.gameObject.SetActive(true);
            if (startMatchButton != null)
                startMatchButton.interactable = true;
            ShowMatchStartError("Could not load the match: " + exception.Message);
            Debug.LogException(exception, this);
        }

        private void ShowMatchStartError(string message)
        {
            if (matchStartErrorText != null)
                matchStartErrorText.text = message;
            Debug.LogError(message, this);
        }

        private Dropdown CreateSurvivalGrowthDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.02f, 0.03f), new Vector2(0.48f, 0.18f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "GROWTH +1", "GROWTH +2", "GROWTH +3" });
            dropdown.SetValueWithoutNotify(Mathf.Clamp(survivalGrowth - 1, 0, 2));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(SetSurvivalGrowth);
            return dropdown;
        }

        private Dropdown CreateWaveBreakDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.52f, 0.03f), new Vector2(0.98f, 0.18f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "BREAK 3s", "BREAK 5s", "BREAK 8s" });
            dropdown.SetValueWithoutNotify(GetWaveBreakOption(waveBreakSeconds));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(SetWaveBreak);
            return dropdown;
        }

        private void SetSurvivalGrowth(int index)
        {
            survivalGrowth = Mathf.Clamp(index + 1, 1, 3);
            RoboClashSession.SaveSurvivalSettings(survivalGrowth, waveBreakSeconds);
        }

        private void SetWaveBreak(int index)
        {
            switch (index)
            {
                case 0: waveBreakSeconds = 3f; break;
                case 2: waveBreakSeconds = 8f; break;
                default: waveBreakSeconds = 5f; break;
            }
            RoboClashSession.SaveSurvivalSettings(survivalGrowth, waveBreakSeconds);
        }

        private static int GetWaveBreakOption(float seconds)
        {
            if (seconds < 4f)
                return 0;
            return seconds >= 6.5f ? 2 : 1;
        }

        private void SetAIDifficulty(int index)
        {
            if (!Enum.IsDefined(typeof(RoboClashAIDifficulty), index))
                return;

            aiDifficulty = (RoboClashAIDifficulty)index;
            RoboClashSession.SaveAIDifficulty(aiDifficulty);
            if (modeSettingsHint != null)
                UpdateModeOptions();
        }

        private void QuitGame()
        {
            if (Application.isEditor)
            {
                Debug.Log("RoboClash Quit was selected. Application.Quit is available in a standalone build.", this);
                return;
            }

            Application.Quit();
        }

        private void ConfirmCareerReset()
        {
            settingsPanel.ShowConfirmation("RESET ALL CAREER STATS AND XP? SETTINGS WILL BE KEPT.",
                () =>
                {
                    RoboClashSaveService.ResetCareer();
                    RefreshCareerSummary();
                    RefreshCareerDetails();
                    settingsPanel.Hide();
                }, false);
        }

        private void RefreshCareerSummary()
        {
            if (careerSummaryText == null)
                return;

            var career = RoboClashSaveService.Current.career;
            var requiredXP = RoboClashSaveService.GetXPRequiredForNextLevel(career.level);
            careerSummaryText.text =
                $"CAREER  //  LEVEL {career.level}  //  XP {career.currentXP}/{requiredXP}\n" +
                $"MATCHES {career.totalMatches}   W {career.totalWins} / L {career.totalLosses} / D {career.totalDraws}\n" +
                $"ELIMINATIONS {career.totalEliminations}   DAMAGE {career.totalDamageDealt:0}\n" +
                $"BEST WAVE {career.highestSurvivalWave}   PREFERRED {career.preferredRobotId}";
        }

        private RoboClashRobotDefinition GetDefinition(RoboClashRobotType type)
        {
            var index = (int)type;
            if (robotPrefabs == null || index < 0 || index >= robotPrefabs.Length ||
                robotPrefabs[index] == null)
                return null;

            return robotPrefabs[index].GetComponent<RoboClashRobotDefinition>();
        }

        private static Color GetAccent(RoboClashRobotType type, RoboClashRobotDefinition definition)
        {
            if (definition == null)
                return CyanColor;
            return definition.primaryColor;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
            Color color, Action action)
        {
            var buttonObject = CreatePanel(label + " Button", parent, color);
            var button = buttonObject.AddComponent<Button>();
            RoboClashRobotAudio.RoboClashAudioManager.AddButtonAudio(button);
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.navigation = Navigation.defaultNavigation;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.78f);
            colors.pressedColor = new Color(0.72f, 0.9f, 1f, 0.8f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.22f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Place(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var buttonText = AddText(buttonObject.transform, label, 17, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            buttonText.fontStyle = FontStyle.Bold;
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        private Dropdown CreateDropdown(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var root = CreatePanel("Resolution Dropdown", parent, new Color(0.07f, 0.14f, 0.2f));
            Place(root.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var dropdown = root.AddComponent<Dropdown>();
            var caption = AddText(root.transform, string.Empty, 15, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.05f), new Vector2(0.85f, 0.95f), Vector2.zero, Vector2.zero);
            AddText(root.transform, "v", 14, CyanColor, TextAnchor.MiddleCenter,
                new Vector2(0.85f, 0.05f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);
            var template = CreatePanel("Template", root.transform, new Color(0.04f, 0.09f, 0.14f));
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, -2f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.offsetMin = Vector2.zero;
            templateRect.offsetMax = Vector2.zero;
            template.AddComponent<CanvasGroup>();
            var viewport = CreatePanel("Viewport", template.transform, new Color(0.04f, 0.09f, 0.14f));
            Stretch(viewport.GetComponent<RectTransform>());
            var viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            Stretch(content.GetComponent<RectTransform>());
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var toggleGroup = content.AddComponent<ToggleGroup>();
            var item = CreatePanel("Item", content.transform, new Color(0.07f, 0.14f, 0.2f));
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.sizeDelta = new Vector2(0f, 28f);
            itemRect.anchoredPosition = Vector2.zero;
            item.AddComponent<LayoutElement>().preferredHeight = 32f;
            var itemToggle = item.AddComponent<Toggle>();
            var itemText = AddText(item.transform, "Option", 15, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.08f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            itemToggle.group = toggleGroup;

            dropdown.targetGraphic = root.GetComponent<Image>();
            dropdown.captionText = caption;
            dropdown.template = templateRect;
            dropdown.itemText = itemText;
            dropdown.itemImage = item.GetComponent<Image>();
            itemToggle.targetGraphic = item.GetComponent<Image>();
            var scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = content.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            template.SetActive(false);
            return dropdown;
        }

        private Text AddText(Transform parent, string value, int size, Color color, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            Place(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static void Stretch(RectTransform rect, Vector2? minimum = null, Vector2? maximum = null)
        {
            rect.anchorMin = minimum ?? Vector2.zero;
            rect.anchorMax = maximum ?? Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
````

### `Assets/Scripts/RoboClash/UI/RoboClashMinimap.cs`

````csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoboClash.UI
{
    public sealed class RoboClashMinimap : MaskableGraphic
    {
        private struct MapShape
        {
            public Bounds Bounds;
            public Color Color;
        }

        private static readonly Color WallColor = new Color(0.12f, 0.68f, 0.83f, 0.9f);
        private static readonly Color ObstacleColor = new Color(0.43f, 0.62f, 0.72f, 0.75f);
        private static readonly Color HazardColor = new Color(1f, 0.25f, 0.1f, 0.9f);

        private readonly List<MapShape> shapes = new List<MapShape>();
        private RectTransform marker;
        private RobotArenaController player;
        private Vector2 arenaCenter;
        private Vector2 arenaSize;
        private bool hasMapData;

        public bool HasMapData => hasMapData;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Configure(Transform arenaRoot, RobotArenaController playerController,
            RectTransform playerMarker)
        {
            player = playerController;
            marker = playerMarker;
            shapes.Clear();
            hasMapData = false;

            if (arenaRoot == null)
            {
                Debug.LogWarning("RoboClash minimap could not build: the arena root is missing.", this);
                SetVerticesDirty();
                UpdatePlayerMarker();
                return;
            }

            var colliders = arenaRoot.GetComponentsInChildren<Collider>(true);
            var floorBounds = new Bounds();
            var hasFloor = false;
            foreach (var arenaCollider in colliders)
            {
                if (arenaCollider == null || !arenaCollider.enabled ||
                    arenaCollider.GetComponentInParent<ArenaDamageable>() != null ||
                    arenaCollider.isTrigger)
                    continue;

                var floor = arenaCollider.GetComponentInParent<ArenaFloor>();
                if (floor == null)
                    continue;

                if (!hasFloor)
                {
                    floorBounds = arenaCollider.bounds;
                    hasFloor = true;
                }
                else
                    floorBounds.Encapsulate(arenaCollider.bounds);
            }

            if (!hasFloor)
            {
                foreach (var arenaCollider in colliders)
                {
                    if (arenaCollider == null || !arenaCollider.enabled || arenaCollider.isTrigger ||
                        arenaCollider.GetComponentInParent<ArenaDamageable>() != null)
                        continue;

                    if (!hasFloor)
                    {
                        floorBounds = arenaCollider.bounds;
                        hasFloor = true;
                    }
                    else
                        floorBounds.Encapsulate(arenaCollider.bounds);
                }
            }

            if (!hasFloor || floorBounds.size.x <= 0f || floorBounds.size.z <= 0f)
            {
                Debug.LogWarning(
                    $"RoboClash minimap could not find valid arena bounds under '{arenaRoot.name}'.",
                    arenaRoot);
                SetVerticesDirty();
                UpdatePlayerMarker();
                return;
            }

            arenaCenter = new Vector2(floorBounds.center.x, floorBounds.center.z);
            arenaSize = new Vector2(floorBounds.size.x, floorBounds.size.z);

            foreach (var arenaCollider in colliders)
            {
                if (arenaCollider == null || !arenaCollider.enabled || arenaCollider.isTrigger ||
                    arenaCollider.GetComponentInParent<ArenaDamageable>() != null ||
                    arenaCollider.GetComponentInParent<ArenaFloor>() != null)
                    continue;

                var bounds = arenaCollider.bounds;
                if (bounds.size.x <= 0f || bounds.size.z <= 0f)
                    continue;

                var isHazard = arenaCollider.GetComponentInParent<ArenaHazard>() != null;
                var isWall = arenaCollider.GetComponentInParent<ArenaWall>() != null;
                shapes.Add(new MapShape
                {
                    Bounds = bounds,
                    Color = isHazard ? HazardColor : isWall ? WallColor : ObstacleColor
                });
            }

            hasMapData = true;
            SetVerticesDirty();
            UpdatePlayerMarker();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!hasMapData)
                return;

            var rect = rectTransform.rect;
            var scale = Mathf.Min(rect.width / arenaSize.x, rect.height / arenaSize.y);
            if (scale <= 0f)
                return;

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                var minimum = WorldToMapLocal(new Vector2(shape.Bounds.min.x, shape.Bounds.min.z), scale);
                var maximum = WorldToMapLocal(new Vector2(shape.Bounds.max.x, shape.Bounds.max.z), scale);
                minimum.x = Mathf.Clamp(minimum.x, rect.xMin, rect.xMax);
                minimum.y = Mathf.Clamp(minimum.y, rect.yMin, rect.yMax);
                maximum.x = Mathf.Clamp(maximum.x, rect.xMin, rect.xMax);
                maximum.y = Mathf.Clamp(maximum.y, rect.yMin, rect.yMax);
                if (maximum.x - minimum.x < 1f || maximum.y - minimum.y < 1f)
                    continue;

                var firstVertex = vertexHelper.currentVertCount;
                AddVertex(vertexHelper, new Vector2(minimum.x, minimum.y), shape.Color);
                AddVertex(vertexHelper, new Vector2(minimum.x, maximum.y), shape.Color);
                AddVertex(vertexHelper, new Vector2(maximum.x, maximum.y), shape.Color);
                AddVertex(vertexHelper, new Vector2(maximum.x, minimum.y), shape.Color);
                vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
                vertexHelper.AddTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
            }
        }

        private void LateUpdate()
        {
            UpdatePlayerMarker();
        }

        private void UpdatePlayerMarker()
        {
            if (marker == null)
                return;

            if (!hasMapData || player == null || !player.gameObject.activeInHierarchy || player.IsDead)
            {
                marker.gameObject.SetActive(false);
                return;
            }

            marker.gameObject.SetActive(true);
            var rect = rectTransform.rect;
            var scale = Mathf.Min(rect.width / arenaSize.x, rect.height / arenaSize.y);
            var worldPosition = player.transform.position;
            var localPosition = WorldToMapLocal(new Vector2(worldPosition.x, worldPosition.z), scale);
            marker.anchoredPosition = new Vector2(
                Mathf.Clamp(localPosition.x, rect.xMin, rect.xMax),
                Mathf.Clamp(localPosition.y, rect.yMin, rect.yMax));
            marker.localRotation = Quaternion.Euler(0f, 0f, -player.transform.eulerAngles.y);
        }

        private Vector2 WorldToMapLocal(Vector2 worldPosition, float scale)
        {
            return (worldPosition - arenaCenter) * scale;
        }

        private static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color vertexColor)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            vertexHelper.AddVert(vertex);
        }
    }
}
````

### `Assets/Scripts/RoboClash/UI/RoboClashSession.cs`

````csharp
using System;
using UnityEngine;

namespace RoboClash
{
    public enum RoboClashCameraMode
    {
        FirstPerson,
        ThirdPerson
    }

    public static class RoboClashSession
    {
        private const string SelectedRobotKey = "RoboClash.SelectedRobot";
        private const string OpponentCountKey = "RoboClash.OpponentCount";
        private const string AIDifficultyKey = "RoboClash.AIDifficulty";
        private const string GameModeKey = "RoboClash.GameMode";
        private const string SurvivalGrowthKey = "RoboClash.SurvivalGrowth";
        private const string WaveBreakKey = "RoboClash.WaveBreakSeconds";
        private const string MatchTimerOptionKey = "RoboClash.MatchTimerOption";
        private const string KillTargetOptionKey = "RoboClash.KillTargetOption";
        private const string KnockoutLivesKey = "RoboClash.KnockoutLives";
        private const string RespawnsEnabledKey = "RoboClash.RespawnsEnabled";
        private const string DuelUsesKillTargetKey = "RoboClash.DuelUsesKillTarget";
        private const string CameraModeKey = "RoboClash.CameraMode";
        private const string FieldOfViewKey = "RoboClash.FieldOfView";
        private const string MouseSensitivityKey = "RoboClash.MouseSensitivity";

        private static RoboClashRobotType selectedRobot;
        private static int opponentCount;
        private static RoboClashAIDifficulty aiDifficulty;
        private static RoboClashGameMode gameMode;
        private static int survivalGrowth;
        private static float waveBreakSeconds;
        private static int matchTimerOption;
        private static int killTargetOption;
        private static int knockoutLives;
        private static bool respawnsEnabled;
        private static bool duelUsesKillTarget;
        private static RoboClashCameraMode cameraMode;
        private static int fieldOfView = 80;
        private static float mouseSensitivity = 0.12f;
        private static bool preferencesLoaded;
        private static bool hasPendingMatchSelection;

        public static RoboClashRobotType SelectedRobot
        {
            get
            {
                LoadPreferences();
                return selectedRobot;
            }
        }

        public static int OpponentCount
        {
            get
            {
                LoadPreferences();
                return opponentCount;
            }
        }

        public static RoboClashAIDifficulty AIDifficulty
        {
            get
            {
                LoadPreferences();
                return aiDifficulty;
            }
        }

        public static RoboClashGameMode GameMode
        {
            get
            {
                LoadPreferences();
                return gameMode;
            }
        }

        public static int SurvivalGrowth
        {
            get
            {
                LoadPreferences();
                return survivalGrowth;
            }
        }

        public static float WaveBreakSeconds
        {
            get
            {
                LoadPreferences();
                return waveBreakSeconds;
            }
        }

        public static int MatchTimerOption
        {
            get
            {
                LoadPreferences();
                return matchTimerOption;
            }
        }

        public static int KillTargetOption
        {
            get { LoadPreferences(); return killTargetOption; }
        }

        public static int KnockoutLives
        {
            get { LoadPreferences(); return knockoutLives; }
        }

        public static bool RespawnsEnabled
        {
            get { LoadPreferences(); return respawnsEnabled; }
        }

        public static bool DuelUsesKillTarget
        {
            get { LoadPreferences(); return duelUsesKillTarget; }
        }

        public static RoboClashCameraMode CameraMode
        {
            get { LoadPreferences(); return cameraMode; }
        }

        public static int FieldOfView
        {
            get { LoadPreferences(); return fieldOfView; }
        }

        public static float MouseSensitivity
        {
            get { LoadPreferences(); return mouseSensitivity; }
        }

        public static void SaveFieldOfView(int degrees)
        {
            LoadPreferences();
            fieldOfView = Mathf.Clamp(degrees, 60, 110);
            PlayerPrefs.SetInt(FieldOfViewKey, fieldOfView);
            PlayerPrefs.Save();
        }

        public static void SaveMouseSensitivity(float value)
        {
            LoadPreferences();
            mouseSensitivity = Mathf.Clamp(value, 0.03f, 0.4f);
            PlayerPrefs.SetFloat(MouseSensitivityKey, mouseSensitivity);
            PlayerPrefs.Save();
        }

        public static void SaveCameraMode(RoboClashCameraMode mode)
        {
            LoadPreferences();
            cameraMode = Enum.IsDefined(typeof(RoboClashCameraMode), mode)
                ? mode
                : RoboClashCameraMode.FirstPerson;
            PlayerPrefs.SetInt(CameraModeKey, (int)cameraMode);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTransientState()
        {
            hasPendingMatchSelection = false;
        }

        public static void SaveSelection(RoboClashRobotType robot, int opponents)
        {
            LoadPreferences();
            selectedRobot = robot;
            opponentCount = Mathf.Clamp(opponents, 1, 24);
            PlayerPrefs.SetInt(SelectedRobotKey, (int)selectedRobot);
            PlayerPrefs.SetInt(OpponentCountKey, opponentCount);
            PlayerPrefs.Save();
            RoboClashSaveService.SetPreferredRobot(selectedRobot);
            RoboClashSaveService.Save();
        }

        public static void SaveAIDifficulty(RoboClashAIDifficulty difficulty)
        {
            LoadPreferences();
            aiDifficulty = Enum.IsDefined(typeof(RoboClashAIDifficulty), difficulty)
                ? difficulty
                : RoboClashAIDifficulty.Normal;
            PlayerPrefs.SetInt(AIDifficultyKey, (int)aiDifficulty);
            PlayerPrefs.Save();
        }

        public static void SaveSurvivalSettings(int enemiesAddedPerWave, float breakSeconds)
        {
            LoadPreferences();
            survivalGrowth = Mathf.Clamp(enemiesAddedPerWave, 1, 8);
            waveBreakSeconds = Mathf.Clamp(breakSeconds, 0f, 20f);
            PlayerPrefs.SetInt(SurvivalGrowthKey, survivalGrowth);
            PlayerPrefs.SetFloat(WaveBreakKey, waveBreakSeconds);
            PlayerPrefs.Save();
        }

        public static void SaveMatchTimerOption(int option)
        {
            LoadPreferences();
            matchTimerOption = Mathf.Clamp(option, 0, 2);
            PlayerPrefs.SetInt(MatchTimerOptionKey, matchTimerOption);
            PlayerPrefs.Save();
        }

        public static void SaveModeRules(int targetOption, int lives, bool allowRespawns, bool duelKillTarget)
        {
            LoadPreferences();
            killTargetOption = Mathf.Clamp(targetOption, 0, 3);
            knockoutLives = Mathf.Clamp(lives, 1, 10);
            respawnsEnabled = allowRespawns;
            duelUsesKillTarget = duelKillTarget;
            PlayerPrefs.SetInt(KillTargetOptionKey, killTargetOption);
            PlayerPrefs.SetInt(KnockoutLivesKey, knockoutLives);
            PlayerPrefs.SetInt(RespawnsEnabledKey, respawnsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(DuelUsesKillTargetKey, duelUsesKillTarget ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void BeginMatch(RoboClashRobotType robot, int opponents,
            RoboClashAIDifficulty difficulty, RoboClashGameMode selectedGameMode,
            int selectedSurvivalGrowth, float selectedWaveBreakSeconds, int selectedTimerOption,
            int selectedKillTargetOption, int selectedKnockoutLives, bool allowRespawns,
            bool duelKillTarget)
        {
            SaveSelection(robot, opponents);
            SaveAIDifficulty(difficulty);
            gameMode = Enum.IsDefined(typeof(RoboClashGameMode), selectedGameMode)
                ? selectedGameMode
                : RoboClashGameMode.FreeForAll;
            PlayerPrefs.SetInt(GameModeKey, (int)gameMode);
            SaveSurvivalSettings(selectedSurvivalGrowth, selectedWaveBreakSeconds);
            SaveMatchTimerOption(selectedTimerOption);
            SaveModeRules(selectedKillTargetOption, selectedKnockoutLives, allowRespawns, duelKillTarget);
            hasPendingMatchSelection = true;
        }

        public static bool ConsumeMatchSelection(out RoboClashRobotType robot, out int opponents,
            out RoboClashAIDifficulty difficulty, out RoboClashGameMode selectedGameMode,
            out int selectedSurvivalGrowth, out float selectedWaveBreakSeconds,
            out int selectedTimerOption, out int selectedKillTargetOption,
            out int selectedKnockoutLives, out bool allowRespawns, out bool duelKillTarget)
        {
            LoadPreferences();
            robot = selectedRobot;
            opponents = opponentCount;
            difficulty = aiDifficulty;
            selectedGameMode = gameMode;
            selectedSurvivalGrowth = survivalGrowth;
            selectedWaveBreakSeconds = waveBreakSeconds;
            selectedTimerOption = matchTimerOption;
            selectedKillTargetOption = killTargetOption;
            selectedKnockoutLives = knockoutLives;
            allowRespawns = respawnsEnabled;
            duelKillTarget = duelUsesKillTarget;
            if (!hasPendingMatchSelection)
                return false;

            hasPendingMatchSelection = false;
            return true;
        }

        private static void LoadPreferences()
        {
            if (preferencesLoaded)
                return;

            var savedRobot = PlayerPrefs.GetInt(SelectedRobotKey, (int)RoboClashRobotType.Volt);
            selectedRobot = savedRobot >= (int)RoboClashRobotType.Volt &&
                            savedRobot <= (int)RoboClashRobotType.Spark
                ? (RoboClashRobotType)savedRobot
                : RoboClashRobotType.Volt;
            opponentCount = Mathf.Clamp(PlayerPrefs.GetInt(OpponentCountKey, 3), 1, 24);
            var savedDifficulty = PlayerPrefs.GetInt(AIDifficultyKey, (int)RoboClashAIDifficulty.Normal);
            aiDifficulty = Enum.IsDefined(typeof(RoboClashAIDifficulty), savedDifficulty)
                ? (RoboClashAIDifficulty)savedDifficulty
                : RoboClashAIDifficulty.Normal;
            var savedGameMode = PlayerPrefs.GetInt(GameModeKey, (int)RoboClashGameMode.TimedDeathmatch);
            gameMode = Enum.IsDefined(typeof(RoboClashGameMode), savedGameMode)
                ? (RoboClashGameMode)savedGameMode
                : RoboClashGameMode.FreeForAll;
            survivalGrowth = Mathf.Clamp(PlayerPrefs.GetInt(SurvivalGrowthKey, 1), 1, 8);
            waveBreakSeconds = Mathf.Clamp(PlayerPrefs.GetFloat(WaveBreakKey, 5f), 0f, 20f);
            matchTimerOption = Mathf.Clamp(PlayerPrefs.GetInt(MatchTimerOptionKey, 1), 0, 2);
            killTargetOption = Mathf.Clamp(PlayerPrefs.GetInt(KillTargetOptionKey, 1), 0, 3);
            knockoutLives = Mathf.Clamp(PlayerPrefs.GetInt(KnockoutLivesKey, 3), 1, 10);
            respawnsEnabled = PlayerPrefs.GetInt(RespawnsEnabledKey, 1) != 0;
            duelUsesKillTarget = PlayerPrefs.GetInt(DuelUsesKillTargetKey, 1) != 0;
            var savedCameraMode = PlayerPrefs.GetInt(CameraModeKey, (int)RoboClashCameraMode.FirstPerson);
            cameraMode = Enum.IsDefined(typeof(RoboClashCameraMode), savedCameraMode)
                ? (RoboClashCameraMode)savedCameraMode
                : RoboClashCameraMode.FirstPerson;
            fieldOfView = Mathf.Clamp(PlayerPrefs.GetInt(FieldOfViewKey, 80), 60, 110);
            mouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, 0.12f), 0.03f, 0.4f);
            preferencesLoaded = true;
        }
    }
}
````

### `Assets/Scripts/RoboClash/UI/RoboClashSettingsPanel.cs`

````csharp
using System;
using System.Collections.Generic;
using RoboClash;
using UnityEngine;
using UnityEngine.UI;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    public sealed class RoboClashSettingsPanel : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.018f, 0.045f, 0.075f, 0.98f);
        private static readonly Color ControlColor = new Color(0.055f, 0.14f, 0.2f, 1f);
        private static readonly Color Cyan = new Color(0.08f, 0.8f, 1f);
        private static readonly int[] FovValues = { 70, 80, 90, 100, 110 };
        private static readonly float[] SensitivityValues = { 0.06f, 0.09f, 0.12f, 0.16f, 0.22f };
        private static readonly string[] SensitivityLabels =
            { "VERY LOW", "LOW", "MEDIUM", "HIGH", "VERY HIGH" };
        private const float RowHeight = 0.05f;

        private readonly List<Vector2Int> availableResolutions = new List<Vector2Int>();
        private Transform customRows;
        private Font font;
        private Action backAction;
        private Action confirmationAction;
        private GameObject contentRoot;
        private GameObject confirmationRoot;
        private GameObject root;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Toggle fullscreenToggle;
        private Toggle vSyncToggle;
        private Dropdown resolutionDropdown;
        private Dropdown qualityDropdown;
        private Dropdown frameRateDropdown;
        private Dropdown cameraModeDropdown;
        private Dropdown fovDropdown;
        private Dropdown sensitivityDropdown;
        private int customRowIndex;
        private bool confirmationClosesPanel;
        private bool resolutionChangePending;
        private float resolutionConfirmationSeconds;
        private int previousResolutionWidth;
        private int previousResolutionHeight;
        private bool previousFullscreen;
        private Text confirmationMessage;

        public GameObject Root => root;

        private void Update()
        {
            if (!resolutionChangePending || root == null || !root.activeSelf ||
                confirmationRoot == null || !confirmationRoot.activeSelf)
                return;

            resolutionConfirmationSeconds -= Time.unscaledDeltaTime;
            if (resolutionConfirmationSeconds <= 0f)
            {
                RestorePreviousResolution();
                FinishConfirmation();
                return;
            }

            confirmationMessage.text =
                $"KEEP THIS DISPLAY MODE? REVERTING IN {Mathf.CeilToInt(resolutionConfirmationSeconds)} SECONDS.";
        }

        public void Initialize(Transform canvasRoot, Font uiFont, Action onBack)
        {
            if (root != null)
            {
                backAction = onBack;
                Refresh();
                return;
            }

            font = uiFont;
            backAction = onBack;
            root = CreateObject("RoboClash Settings", canvasRoot);
            Stretch(root.GetComponent<RectTransform>());
            var backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.005f, 0.018f, 0.035f, 0.94f);
            contentRoot = CreateObject("Settings Content", root.transform);
            Stretch(contentRoot.GetComponent<RectTransform>());
            BuildControls();
            root.SetActive(false);
        }

        public void Show()
        {
            Refresh();
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root == null)
                return;

            if (resolutionChangePending)
                RestorePreviousResolution();
            RoboClashSaveService.Save();
            confirmationAction = null;
            confirmationRoot?.SetActive(false);
            contentRoot.SetActive(true);
            root.SetActive(false);
        }

        private static void SetCameraMode(int index)
        {
            RoboClashSession.SaveCameraMode(index == (int)RoboClashCameraMode.ThirdPerson
                ? RoboClashCameraMode.ThirdPerson
                : RoboClashCameraMode.FirstPerson);
        }

        public void ShowConfirmation(string message, Action onConfirmed, bool closePanelWhenFinished)
        {
            confirmationAction = onConfirmed;
            confirmationClosesPanel = closePanelWhenFinished;
            root.SetActive(true);
            contentRoot.SetActive(false);
            confirmationRoot.SetActive(true);
            confirmationMessage.text = message;
        }

        public Dropdown AddCustomDropdownRow(string label, IReadOnlyList<string> options,
            int selectedIndex, Action<int> onChanged)
        {
            if (customRows == null)
                return null;

            var y = customRowIndex++ == 0 ? 0.18f : 0.10f;
            CreateText(customRows, label.ToUpperInvariant(), 15, Color.white,
                new Vector2(0.06f, y), new Vector2(0.39f, y + 0.065f), TextAnchor.MiddleLeft);
            var dropdown = CreateDropdown(customRows,
                new Vector2(0.42f, y), new Vector2(0.94f, y + 0.065f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.SetValueWithoutNotify(options.Count > 0
                ? Mathf.Clamp(selectedIndex, 0, options.Count - 1)
                : 0);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(value => onChanged?.Invoke(value));
            return dropdown;
        }

        public void Refresh()
        {
            if (root == null)
                return;

            var settings = RoboClashSaveService.Current.settings;
            masterSlider.SetValueWithoutNotify(settings.masterVolume);
            musicSlider.SetValueWithoutNotify(settings.musicVolume);
            sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
            fullscreenToggle.SetIsOnWithoutNotify(settings.fullscreen);
            vSyncToggle.SetIsOnWithoutNotify(settings.vSync);
            cameraModeDropdown.SetValueWithoutNotify((int)RoboClashSession.CameraMode);
            fovDropdown.SetValueWithoutNotify(NearestIndex(FovValues, RoboClashSession.FieldOfView));
            fovDropdown.RefreshShownValue();
            sensitivityDropdown.SetValueWithoutNotify(
                NearestIndex(SensitivityValues, RoboClashSession.MouseSensitivity));
            sensitivityDropdown.RefreshShownValue();
            PopulateResolutionOptions(settings.resolutionWidth, settings.resolutionHeight);
            PopulateQualityOptions(settings.qualityLevel);
            frameRateDropdown.SetValueWithoutNotify(GetFrameRateOption(settings.targetFrameRate));
            frameRateDropdown.RefreshShownValue();
        }

        private void BuildControls()
        {
            var panel = CreatePanel(contentRoot.transform, "Settings Panel", PanelColor,
                new Vector2(0.13f, 0.045f), new Vector2(0.87f, 0.955f));
            CreateText(panel.transform, "AUDIO & DISPLAY SETTINGS", 30, Color.white,
                new Vector2(0.06f, 0.92f), new Vector2(0.94f, 0.995f), TextAnchor.MiddleCenter);
            CreateText(panel.transform, "ARENA AMBIENCE ENABLED // MUSIC TRACKS NOT ASSIGNED.",
                11, new Color(1f, 0.72f, 0.25f),
                new Vector2(0.06f, 0.89f), new Vector2(0.94f, 0.92f), TextAnchor.MiddleCenter);

            masterSlider = AddSliderRow(panel.transform, "MASTER VOLUME", RowY(0),
                RoboClashSaveService.Current.settings.masterVolume, RoboClashSaveService.SetMasterVolume);
            musicSlider = AddSliderRow(panel.transform, "MUSIC VOLUME", RowY(1),
                RoboClashSaveService.Current.settings.musicVolume, RoboClashSaveService.SetMusicVolume);
            musicSlider.interactable = true;
            sfxSlider = AddSliderRow(panel.transform, "SFX VOLUME", RowY(2),
                RoboClashSaveService.Current.settings.sfxVolume, RoboClashSaveService.SetSfxVolume);

            CreateSettingLabel(panel.transform, "FULLSCREEN", 3);
            fullscreenToggle = CreateToggle(panel.transform, SettingMinimum(3),
                SettingMaximum(3, 0.52f),
                RoboClashSaveService.Current.settings.fullscreen, RoboClashSaveService.SetFullscreen);

            CreateSettingLabel(panel.transform, "RESOLUTION", 4);
            resolutionDropdown = CreateSettingDropdown(panel.transform, 4);
            resolutionDropdown.onValueChanged.AddListener(ApplyResolution);

            CreateSettingLabel(panel.transform, "GRAPHICS QUALITY", 5);
            qualityDropdown = CreateSettingDropdown(panel.transform, 5);
            qualityDropdown.onValueChanged.AddListener(RoboClashSaveService.SetQualityLevel);

            CreateSettingLabel(panel.transform, "V-SYNC", 6);
            vSyncToggle = CreateToggle(panel.transform, SettingMinimum(6),
                SettingMaximum(6, 0.52f),
                RoboClashSaveService.Current.settings.vSync, RoboClashSaveService.SetVSync);

            CreateSettingLabel(panel.transform, "FRAME-RATE LIMIT", 7);
            frameRateDropdown = CreateSettingDropdown(panel.transform, 7);
            frameRateDropdown.AddOptions(new List<string>
            {
                "30 FPS", "60 FPS", "120 FPS", "144 FPS", "UNLIMITED"
            });
            frameRateDropdown.onValueChanged.AddListener(ApplyFrameRate);

            CreateSettingLabel(panel.transform, "CAMERA MODE", 8);
            cameraModeDropdown = CreateSettingDropdown(panel.transform, 8);
            cameraModeDropdown.AddOptions(new List<string> { "FIRST PERSON", "THIRD PERSON" });
            cameraModeDropdown.onValueChanged.AddListener(SetCameraMode);

            CreateSettingLabel(panel.transform, "FIELD OF VIEW", 9);
            fovDropdown = CreateSettingDropdown(panel.transform, 9);
            fovDropdown.AddOptions(new List<string> { "70", "80", "90", "100", "110" });
            fovDropdown.onValueChanged.AddListener(index =>
                RoboClashSession.SaveFieldOfView(FovValues[Mathf.Clamp(index, 0, FovValues.Length - 1)]));

            CreateSettingLabel(panel.transform, "MOUSE SENSITIVITY", 10);
            sensitivityDropdown = CreateSettingDropdown(panel.transform, 10);
            sensitivityDropdown.AddOptions(new List<string>(SensitivityLabels));
            sensitivityDropdown.onValueChanged.AddListener(index =>
                RoboClashSession.SaveMouseSensitivity(
                    SensitivityValues[Mathf.Clamp(index, 0, SensitivityValues.Length - 1)]));

            customRows = panel.transform;
            CreateText(panel.transform, "CONTROLS", 12, Cyan, new Vector2(0.06f, 0.12f),
                new Vector2(0.39f, 0.18f), TextAnchor.MiddleLeft);
            CreateText(panel.transform,
                "WASD MOVE // MOUSE AIM // LMB FIRE // R RELOAD // Q ABILITY // E ULTIMATE // SPACE JUMP // ESC MENU",
                10, Color.white, new Vector2(0.42f, 0.12f), new Vector2(0.94f, 0.20f),
                TextAnchor.MiddleLeft);
            CreateButton(panel.transform, "RESET SETTINGS", new Vector2(0.06f, 0.035f),
                new Vector2(0.31f, 0.095f), ConfirmResetSettings);
            CreateButton(panel.transform, "BACK", new Vector2(0.69f, 0.04f),
                new Vector2(0.94f, 0.12f), ReturnToCaller);
            confirmationRoot = BuildConfirmation(panel.transform);
            confirmationRoot.SetActive(false);
        }

        private GameObject BuildConfirmation(Transform parent)
        {
            var overlay = CreateObject("Confirmation", parent);
            Stretch(overlay.GetComponent<RectTransform>());
            var background = overlay.AddComponent<Image>();
            background.color = new Color(0.005f, 0.018f, 0.035f, 0.96f);
            var panel = CreatePanel(overlay.transform, "Confirmation Panel", PanelColor,
                new Vector2(0.22f, 0.3f), new Vector2(0.78f, 0.7f));
            confirmationMessage = CreateText(panel.transform, string.Empty, 19, Color.white,
                new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.88f), TextAnchor.MiddleCenter,
                "Message");
            CreateButton(panel.transform, "CONFIRM", new Vector2(0.1f, 0.12f),
                new Vector2(0.45f, 0.32f), ConfirmAction);
            CreateButton(panel.transform, "CANCEL", new Vector2(0.55f, 0.12f),
                new Vector2(0.9f, 0.32f), CancelConfirmation);
            return overlay;
        }

        private Slider AddSliderRow(Transform parent, string label, float y, float value, Action<float> changed)
        {
            CreateText(parent, label, 15, Color.white,
                new Vector2(0.06f, y), new Vector2(0.39f, y + RowHeight), TextAnchor.MiddleLeft);
            var slider = CreateSlider(parent, new Vector2(0.42f, y + 0.005f),
                new Vector2(0.94f, y + RowHeight - 0.005f), value);
            slider.onValueChanged.AddListener(valueChanged => changed(valueChanged));
            return slider;
        }

        private static float RowY(int row)
        {
            return 0.835f - row * 0.062f;
        }

        private static Vector2 SettingMinimum(int row, float x = 0.42f)
        {
            return new Vector2(x, RowY(row));
        }

        private static Vector2 SettingMaximum(int row, float x = 0.94f)
        {
            return new Vector2(x, RowY(row) + RowHeight);
        }

        private void CreateSettingLabel(Transform parent, string label, int row)
        {
            CreateText(parent, label, 15, Color.white,
                new Vector2(0.06f, RowY(row)), new Vector2(0.39f, RowY(row) + RowHeight),
                TextAnchor.MiddleLeft);
        }

        private Dropdown CreateSettingDropdown(Transform parent, int row)
        {
            return CreateDropdown(parent, SettingMinimum(row), SettingMaximum(row));
        }

        private static int NearestIndex(int[] values, int target)
        {
            var best = 0;
            for (var i = 1; i < values.Length; i++)
            {
                if (Mathf.Abs(values[i] - target) < Mathf.Abs(values[best] - target))
                    best = i;
            }
            return best;
        }

        private static int NearestIndex(float[] values, float target)
        {
            var best = 0;
            for (var i = 1; i < values.Length; i++)
            {
                if (Mathf.Abs(values[i] - target) < Mathf.Abs(values[best] - target))
                    best = i;
            }
            return best;
        }

        private Slider CreateSlider(Transform parent, Vector2 minimum, Vector2 maximum, float value)
        {
            var sliderObject = CreateObject("Volume Slider", parent, typeof(Slider));
            Place(sliderObject.GetComponent<RectTransform>(), minimum, maximum);
            var background = CreatePanel(sliderObject.transform, "Track",
                new Color(0.07f, 0.14f, 0.2f), Vector2.zero, Vector2.one);
            var fill = CreatePanel(background.transform, "Fill", Cyan,
                Vector2.zero, Vector2.one);
            var handle = CreatePanel(sliderObject.transform, "Handle", Color.white,
                Vector2.zero, Vector2.one);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 0f);
            var slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            return slider;
        }

        private Toggle CreateToggle(Transform parent, Vector2 minimum, Vector2 maximum,
            bool value, Action<bool> changed)
        {
            var toggleObject = CreateObject("Setting Toggle", parent, typeof(Toggle));
            Place(toggleObject.GetComponent<RectTransform>(), minimum, maximum);
            var background = CreatePanel(toggleObject.transform, "Background", ControlColor,
                Vector2.zero, Vector2.one);
            var check = CreatePanel(background.transform, "Check", Cyan,
                new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(changedValue => changed(changedValue));
            return toggle;
        }

        private Dropdown CreateDropdown(Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var dropdownObject = CreatePanel(parent, "Settings Dropdown", ControlColor,
                minimum, maximum);
            var dropdown = dropdownObject.AddComponent<Dropdown>();
            var caption = CreateText(dropdownObject.transform, string.Empty, 14, Color.white,
                new Vector2(0.06f, 0.05f), new Vector2(0.86f, 0.95f), TextAnchor.MiddleLeft);
            CreateText(dropdownObject.transform, "v", 14, Cyan,
                new Vector2(0.86f, 0.05f), new Vector2(0.98f, 0.95f), TextAnchor.MiddleCenter);
            var template = CreatePanel(dropdownObject.transform, "Template", ControlColor,
                new Vector2(0f, -2f), new Vector2(1f, 0f));
            var templateRect = template.GetComponent<RectTransform>();
            template.AddComponent<CanvasGroup>();
            var viewport = CreatePanel(template.transform, "Viewport", ControlColor,
                Vector2.zero, Vector2.one);
            var viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var content = CreateObject("Content", viewport.transform);
            Stretch(content.GetComponent<RectTransform>());
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var toggleGroup = content.AddComponent<ToggleGroup>();
            var item = CreatePanel(content.transform, "Item", ControlColor,
                Vector2.zero, Vector2.one);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 30f);
            item.AddComponent<LayoutElement>().preferredHeight = 30f;
            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.group = toggleGroup;
            var itemText = CreateText(item.transform, "Option", 14, Color.white,
                new Vector2(0.08f, 0f), Vector2.one, TextAnchor.MiddleLeft);
            itemToggle.targetGraphic = item.GetComponent<Image>();
            dropdown.targetGraphic = dropdownObject.GetComponent<Image>();
            dropdown.captionText = caption;
            dropdown.template = templateRect;
            dropdown.itemText = itemText;
            dropdown.itemImage = item.GetComponent<Image>();
            var scroll = template.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;
            template.SetActive(false);
            return dropdown;
        }

        private void PopulateResolutionOptions(int selectedWidth, int selectedHeight)
        {
            availableResolutions.Clear();
            var currentIndex = 0;
            var currentResolutionIndex = 0;
            var selectedResolutionFound = false;
            var currentResolutionFound = false;
            var resolutions = Screen.resolutions;
            foreach (var resolution in resolutions)
            {
                var duplicate = false;
                for (var i = 0; i < availableResolutions.Count; i++)
                {
                    if (availableResolutions[i].x == resolution.width &&
                        availableResolutions[i].y == resolution.height)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (duplicate)
                    continue;
                if (resolution.width == selectedWidth && resolution.height == selectedHeight)
                {
                    currentIndex = availableResolutions.Count;
                    selectedResolutionFound = true;
                }
                if (resolution.width == Screen.width && resolution.height == Screen.height)
                {
                    currentResolutionIndex = availableResolutions.Count;
                    currentResolutionFound = true;
                }
                availableResolutions.Add(new Vector2Int(resolution.width, resolution.height));
            }

            if (!currentResolutionFound)
            {
                currentResolutionIndex = availableResolutions.Count;
                availableResolutions.Add(new Vector2Int(Screen.width, Screen.height));
            }
            if (!selectedResolutionFound)
                currentIndex = currentResolutionIndex;

            var labels = new List<string>(availableResolutions.Count);
            foreach (var resolution in availableResolutions)
                labels.Add($"{resolution.x} x {resolution.y}");
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
            resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(currentIndex, 0, labels.Count - 1));
            resolutionDropdown.RefreshShownValue();
        }

        private void PopulateQualityOptions(int selectedQuality)
        {
            var names = QualitySettings.names;
            qualityDropdown.ClearOptions();
            if (names.Length == 0)
            {
                qualityDropdown.AddOptions(new List<string> { "UNAVAILABLE" });
                qualityDropdown.interactable = false;
                return;
            }

            qualityDropdown.interactable = true;
            qualityDropdown.AddOptions(new List<string>(names));
            qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedQuality, 0, names.Length - 1));
            qualityDropdown.RefreshShownValue();
        }

        private void ApplyResolution(int index)
        {
            if (index < 0 || index >= availableResolutions.Count)
                return;
            var resolution = availableResolutions[index];
            if (resolution.x == Screen.width && resolution.y == Screen.height)
                return;

            previousResolutionWidth = Screen.width;
            previousResolutionHeight = Screen.height;
            previousFullscreen = Screen.fullScreen;
            RoboClashSaveService.SetResolution(resolution.x, resolution.y);
            resolutionChangePending = true;
            resolutionConfirmationSeconds = 10f;
            ShowConfirmation(
                $"KEEP {resolution.x} x {resolution.y}? REVERTING IN 10 SECONDS.",
                ConfirmResolution, false);
        }

        private void ConfirmResolution()
        {
            resolutionChangePending = false;
            RoboClashSaveService.Save();
        }

        private void RestorePreviousResolution()
        {
            if (!resolutionChangePending)
                return;

            resolutionChangePending = false;
            RoboClashSaveService.SetFullscreen(previousFullscreen);
            RoboClashSaveService.SetResolution(previousResolutionWidth, previousResolutionHeight);
        }

        private static void ApplyFrameRate(int index)
        {
            var rates = new[] { 30, 60, 120, 144, -1 };
            if (index >= 0 && index < rates.Length)
                RoboClashSaveService.SetTargetFrameRate(rates[index]);
        }

        private static int GetFrameRateOption(int frameRate)
        {
            switch (frameRate)
            {
                case 30: return 0;
                case 120: return 2;
                case 144: return 3;
                case -1: return 4;
                default: return 1;
            }
        }

        private void ConfirmAction()
        {
            confirmationAction?.Invoke();
            FinishConfirmation();
        }

        private void ConfirmResetSettings()
        {
            ShowConfirmation("RESET AUDIO AND DISPLAY SETTINGS TO DEFAULTS?",
                () =>
                {
                    RoboClashSaveService.ResetSettings();
                    RoboClashSession.SaveCameraMode(RoboClashCameraMode.FirstPerson);
                    RoboClashSession.SaveFieldOfView(80);
                    RoboClashSession.SaveMouseSensitivity(0.12f);
                    Refresh();
                }, false);
        }

        private void CancelConfirmation()
        {
            if (resolutionChangePending)
                RestorePreviousResolution();
            FinishConfirmation();
        }

        private void FinishConfirmation()
        {
            confirmationAction = null;
            confirmationRoot.SetActive(false);
            if (confirmationClosesPanel)
            {
                Hide();
                backAction?.Invoke();
                return;
            }

            contentRoot.SetActive(true);
            Refresh();
        }

        private void ReturnToCaller()
        {
            Hide();
            backAction?.Invoke();
        }

        private Button CreateButton(Transform parent, string label, Vector2 minimum, Vector2 maximum,
            Action action)
        {
            var buttonObject = CreatePanel(parent, label + " Button", ControlColor, minimum, maximum);
            var button = buttonObject.AddComponent<Button>();
            RoboClashRobotAudio.RoboClashAudioManager.AddButtonAudio(button);
            button.targetGraphic = buttonObject.GetComponent<Image>();
            CreateText(buttonObject.transform, label, 14, Color.white,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        private GameObject CreatePanel(Transform parent, string name, Color color,
            Vector2 minimum, Vector2 maximum)
        {
            var panel = CreateObject(name, parent);
            Place(panel.GetComponent<RectTransform>(), minimum, maximum);
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        private Text CreateText(Transform parent, string value, int size, Color color,
            Vector2 minimum, Vector2 maximum, TextAnchor alignment, string objectName = "Text")
        {
            var textObject = CreateObject(objectName, parent, typeof(CanvasRenderer), typeof(Text));
            Place(textObject.GetComponent<RectTransform>(), minimum, maximum);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateObject(string objectName, Transform parent, params Type[] components)
        {
            var gameObject = new GameObject(objectName, MergeRectTransform(components));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Type[] MergeRectTransform(Type[] components)
        {
            var result = new Type[components.Length + 1];
            result[0] = typeof(RectTransform);
            Array.Copy(components, 0, result, 1, components.Length);
            return result;
        }

        private static void Place(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            Place(rect, Vector2.zero, Vector2.one);
        }
    }
}
````

### `Assets/Scripts/RoboClash/Visuals/RoboClashArenaVisuals.cs`

````csharp
using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    public static class RoboClashArenaVisuals
    {
        private static readonly Dictionary<string, Material> SharedMaterials =
            new Dictionary<string, Material>();

        public static Material CreateMaterial(
            string materialName,
            Color baseColor,
            float metallic,
            float smoothness,
            Color emissionColor = default,
            Texture2D baseTexture = null,
            Texture2D normalMap = null,
            Texture2D metallicSmoothnessMap = null,
            Texture2D emissionMap = null,
            Vector2 textureScale = default)
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                Debug.LogError("RoboClash: Arena material needs a name.");
                return null;
            }

            if (SharedMaterials.TryGetValue(materialName, out var sharedMaterial) &&
                sharedMaterial != null)
            {
                ConfigureMaterial(sharedMaterial, baseColor, metallic, smoothness,
                    emissionColor, baseTexture, normalMap, metallicSmoothnessMap,
                    emissionMap, textureScale);
                return sharedMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("RoboClash: No compatible material shader found.");
                return null;
            }

            sharedMaterial = new Material(shader) { name = materialName };
            ConfigureMaterial(sharedMaterial, baseColor, metallic, smoothness,
                emissionColor, baseTexture, normalMap, metallicSmoothnessMap,
                emissionMap, textureScale);
            SharedMaterials.Add(materialName, sharedMaterial);
            return sharedMaterial;
        }

        public static void ConfigureMaterial(
            Material material,
            Color baseColor,
            float metallic,
            float smoothness,
            Color emissionColor = default,
            Texture2D baseTexture = null,
            Texture2D normalMap = null,
            Texture2D metallicSmoothnessMap = null,
            Texture2D emissionMap = null,
            Vector2 textureScale = default)
        {
            if (material == null)
            {
                Debug.LogError("RoboClash: Cannot configure a missing arena material.");
                return;
            }

            var scale = textureScale == Vector2.zero ? Vector2.one : textureScale;
            SetColor(material, "_BaseColor", "_Color", baseColor);
            SetFloat(material, "_Metallic", Mathf.Clamp01(metallic));
            SetFloat(material, "_Smoothness", Mathf.Clamp01(smoothness));
            SetTexture(material, "_BaseMap", "_MainTex", baseTexture, scale);
            SetTexture(material, "_BumpMap", null, normalMap, scale);
            SetTexture(material, "_MetallicGlossMap", null, metallicSmoothnessMap, scale);
            SetTexture(material, "_EmissionMap", null, emissionMap, scale);

            SetKeyword(material, "_NORMALMAP", normalMap != null);
            SetKeyword(material, "_METALLICSPECGLOSSMAP",
                metallicSmoothnessMap != null);
            SetKeyword(material, "_METALLICGLOSSMAP",
                metallicSmoothnessMap != null);
            SetKeyword(material, "_EMISSION",
                emissionColor.maxColorComponent > 0f || emissionMap != null);

            if (material.HasProperty("_BumpScale"))
                material.SetFloat("_BumpScale", normalMap != null ? 0.45f : 1f);
            if (material.HasProperty("_SmoothnessTextureChannel"))
                material.SetFloat("_SmoothnessTextureChannel", 0f);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emissionColor);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
        }

        private static void SetColor(Material material, string primaryProperty,
            string fallbackProperty, Color color)
        {
            if (material.HasProperty(primaryProperty))
                material.SetColor(primaryProperty, color);
            if (material.HasProperty(fallbackProperty))
                material.SetColor(fallbackProperty, color);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static void SetTexture(Material material, string primaryProperty,
            string fallbackProperty, Texture texture, Vector2 scale)
        {
            if (material.HasProperty(primaryProperty))
            {
                material.SetTexture(primaryProperty, texture);
                material.SetTextureScale(primaryProperty, scale);
            }
            if (!string.IsNullOrEmpty(fallbackProperty) &&
                material.HasProperty(fallbackProperty))
            {
                material.SetTexture(fallbackProperty, texture);
                material.SetTextureScale(fallbackProperty, scale);
            }
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }
}
````

### `Assets/Scripts/RoboClash/Visuals/RoboClashLavaVisual.cs`

````csharp
using UnityEngine;

namespace RoboClash
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class RoboClashLavaVisual : MonoBehaviour
    {
        [SerializeField] private Vector2 flowSpeed = new Vector2(0.018f, 0.011f);
        [SerializeField, Range(0f, 0.5f)] private float emissionPulse = 0.12f;

        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private Renderer targetRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Vector2 baseTextureScale;
        private Vector2 mainTextureScale;
        private Color baseEmissionColor = Color.white;

        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            var material = targetRenderer.sharedMaterial;
            if (material == null)
                return;

            if (material.HasProperty("_BaseMap"))
                baseTextureScale = material.GetTextureScale("_BaseMap");
            if (material.HasProperty("_MainTex"))
                mainTextureScale = material.GetTextureScale("_MainTex");
            if (material.HasProperty(EmissionColor))
                baseEmissionColor = material.GetColor(EmissionColor);
        }

        private void Update()
        {
            if (targetRenderer == null || propertyBlock == null)
                return;

            var offset = new Vector2(
                Mathf.Repeat(Time.time * flowSpeed.x, 1f),
                Mathf.Repeat(Time.time * flowSpeed.y, 1f));
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetVector(BaseMapSt,
                new Vector4(baseTextureScale.x, baseTextureScale.y, offset.x, offset.y));
            propertyBlock.SetVector(MainTexSt,
                new Vector4(mainTextureScale.x, mainTextureScale.y, offset.x, offset.y));
            var pulse = 1f + Mathf.Sin(Time.time * 2.7f) * emissionPulse;
            propertyBlock.SetColor(EmissionColor, baseEmissionColor * pulse);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
````

### `Assets/TutorialInfo/Editor/ReadmeEditor.cs`

````csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UIElements;

[CustomEditor(typeof(Readme))]
[InitializeOnLoad]
sealed class ReadmeEditor : Editor
{
    static readonly string k_ShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";
    static readonly string k_ReadmeSourceDirectory = "Assets/TutorialInfo";

    static ReadmeEditor()
        => EditorApplication.delayCall += SelectReadmeAutomatically;

    static void SelectReadmeAutomatically()
    {
        if (!SessionState.GetBool(k_ShowedReadmeSessionStateName, false))
        {
            var readme = SelectReadme();
            SessionState.SetBool(k_ShowedReadmeSessionStateName, true);

            if (readme && !readme.loadedLayout)
            {
                EditorUtility.LoadWindowLayout(Path.Combine(Application.dataPath, "TutorialInfo/Layout.wlt"));
                readme.loadedLayout = true;
            }
        }
    }

    static Readme SelectReadme()
    {
        var ids = AssetDatabase.FindAssets("Readme t:Readme");
        if (ids.Length != 1)
        {
            Debug.Log("Couldn't find a readme");
            return null;
        }

        var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));
        Selection.objects = new UnityEngine.Object[] { readmeObject };
        return (Readme)readmeObject;
    }
    
    void RemoveTutorial()
    {
        if (EditorUtility.DisplayDialog("Remove Readme Assets",
            
            $"All contents under {k_ReadmeSourceDirectory} will be removed, are you sure you want to proceed?",
            "Proceed",
            "Cancel"))
        {
            if (Directory.Exists(k_ReadmeSourceDirectory))
            {
                FileUtil.DeleteFileOrDirectory(k_ReadmeSourceDirectory);
                FileUtil.DeleteFileOrDirectory(k_ReadmeSourceDirectory + ".meta");
            }
            else
            {
                Debug.Log($"Could not find the Readme folder at {k_ReadmeSourceDirectory}");
            }

            var readmeAsset = SelectReadme();
            if (readmeAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(readmeAsset);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                FileUtil.DeleteFileOrDirectory(path);
            }

            AssetDatabase.Refresh();
        }
    }

    //Remove ImGUI
    protected sealed override void OnHeaderGUI() { }
    public sealed override void OnInspectorGUI() { }

    public override VisualElement CreateInspectorGUI()
    {
        var readme = (Readme)target;

        VisualElement root = new();
        root.styleSheets.Add(readme.commonStyle);
        root.styleSheets.Add(EditorGUIUtility.isProSkin ? readme.darkStyle : readme.lightStyle);

        VisualElement ChainWithClass(VisualElement created, string className)
        {
            created.AddToClassList(className);
            return created;
        }

        //Header
        VisualElement title = new();
        title.AddToClassList("title");
        title.Add(ChainWithClass(new Image() { image = readme.icon }, "title__icon"));
        title.Add(ChainWithClass(new Label(readme.title), "title__text"));
        root.Add(title);

        //Content
        foreach (var section in readme.sections)
        {
            VisualElement part = new();
            part.AddToClassList("section");

            if (!string.IsNullOrEmpty(section.heading))
                part.Add(ChainWithClass(new Label(section.heading), "section__header"));

            if (!string.IsNullOrEmpty(section.text))
                part.Add(ChainWithClass(new Label(section.text), "section__body"));

            if (!string.IsNullOrEmpty(section.linkText))
            {
                var link = ChainWithClass(new Label(section.linkText), "section__link");
                link.RegisterCallback<ClickEvent>(evt => Application.OpenURL(section.url));
                part.Add(link);
            }

            root.Add(part);
        }

        var button = new Button(RemoveTutorial) { text = "Remove Readme Assets" };
        button.AddToClassList("remove-readme-button");
        root.Add(button);

        return root;
    }
}
````

### `Assets/TutorialInfo/Readme.cs`

````csharp
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Readme : ScriptableObject
{
    public StyleSheet commonStyle;
    public StyleSheet darkStyle;
    public StyleSheet lightStyle;
    public Texture2D icon;
    public string title;
    public Section[] sections;
    public bool loadedLayout;

    [Serializable]
    public class Section
    {
        public string heading, text, linkText, url;
    }
}
````

### `Assets/TutorialInfo/StyleSheets/ReadmeEditor.uss`

````css
:root {
  --link-color: blue;
}

.title
{
	margin: -2px -6px 0px -15px;
	padding: 20px 6px 20px 22px;
	flex-direction: row;
	align-items: center;
	background-color: var(--unity-colors-inspector_titlebar-background);
	height: 139px;
	border-bottom-width: 1px; 
	border-bottom-color: var(--unity-colors-inspector_titlebar-border);
}

.title__icon
{
	max-width: 120px;
	max-height: 69px;
}

.title__text
{
	margin-left: 25px;
	padding: 0px 0px 0px 0px;
	font-size: 26px;
	flex-shrink: 1;
	white-space: normal;
	overflow: hidden;
}

.section 
{
	align-items:flex-start;
	margin-bottom: 16px;
}

.section__header 
{
	font-size: 20px;
	white-space: normal;
	-unity-font-style: bold;
}

.section__body
{
	font-size: 14px;
	white-space: normal;
}

.section__link 
{
	font-size: 14px;
	white-space: nowrap;
	overflow: hidden;
	border-bottom-width: 0px; 
	margin-bottom: 0px;
	border-bottom-color: var(--link-color);
	color: var(--link-color);
}

.section__link:hover
{
	border-bottom-width: 1px; 
	margin-bottom: -1px;
	cursor: link;
}

.remove-readme-button 
{
	font-size: 13px;
	-unity-font-style: bold;
	margin: 1px 0px 1px 0px;
}
````

### `Assets/TutorialInfo/StyleSheets/ReadmeEditorDark.uss`

````css
:root {
  --link-color: #7DA0FF;
}
````

### `Assets/TutorialInfo/StyleSheets/ReadmeEditorLight.uss`

````css
:root {
  --link-color: #3C28D2;
}
````

### `Packages/manifest.json`

````json
{
  "dependencies": {
    "com.unity.ai.navigation": "2.0.14",
    "com.unity.collab-proxy": "2.13.6",
    "com.unity.ide.rider": "3.0.38",
    "com.unity.ide.visualstudio": "2.0.26",
    "com.unity.inputsystem": "1.20.0",
    "com.unity.render-pipelines.universal": "17.6.0",
    "com.unity.test-framework": "1.8.0",
    "com.unity.timeline": "6.6.0",
    "com.unity.ugui": "2.6.0",
    "com.unity.visualscripting": "1.9.12",
    "com.unity.modules.accessibility": "1.0.0",
    "com.unity.modules.adaptiveperformance": "1.0.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.physicscore2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tetgen": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.timelinefoundation": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vectorgraphics": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
````

### `ProjectSettings/ProjectVersion.txt`

````text
m_EditorVersion: 6000.6.4f1
m_EditorVersionWithRevision: 6000.6.4f1 (12bfff696524)
````

### `ProjectSettings/ProjectSettings.asset`

````yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 30
  productGUID: d1a121c19284eb14e9de67fe59b3a368
  AndroidProfiler: 0
  AndroidFilterTouchesWhenObscured: 0
  AndroidEnableSustainedPerformanceMode: 0
  defaultScreenOrientation: 4
  targetDevice: 2
  useOnDemandResources: 0
  accelerometerFrequency: 60
  companyName: DefaultCompany
  productName: RoboClash
  defaultCursor: {fileID: 0}
  cursorHotspot: {x: 0, y: 0}
  m_SplashScreenBackgroundColor: {r: 0.13725491, g: 0.12156863, b: 0.1254902, a: 1}
  m_ShowUnitySplashScreen: 1
  m_ShowUnitySplashLogo: 1
  m_SplashScreenOverlayOpacity: 1
  m_SplashScreenAnimation: 1
  m_SplashScreenLogoStyle: 1
  m_SplashScreenDrawMode: 0
  m_SplashScreenBackgroundAnimationZoom: 1
  m_SplashScreenLogoAnimationZoom: 1
  m_SplashScreenBackgroundLandscapeAspect: 1
  m_SplashScreenBackgroundPortraitAspect: 1
  m_SplashScreenBackgroundLandscapeUvs:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  m_SplashScreenBackgroundPortraitUvs:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  m_SplashScreenLogos: []
  m_VirtualRealitySplashScreen: {fileID: 0}
  defaultScreenWidth: 1024
  defaultScreenHeight: 768
  defaultScreenWidthWeb: 960
  defaultScreenHeightWeb: 600
  m_StereoRenderingPath: 0
  m_ActiveColorSpace: 1
  unsupportedMSAAFallback: 0
  m_SpriteBatchMaxVertexCount: 65535
  m_SpriteBatchVertexThreshold: 300
  m_MTRendering: 1
  mipStripping: 0
  numberOfMipsStripped: 0
  numberOfMipsStrippedPerMipmapLimitGroup: {}
  m_StackTraceTypes: 010000000100000001000000010000000100000001000000i
  iosShowActivityIndicatorOnLoading: -1
  androidShowActivityIndicatorOnLoading: -1
  iosUseCustomAppBackgroundBehavior: 0
  allowedAutorotateToPortrait: 1
  allowedAutorotateToPortraitUpsideDown: 1
  allowedAutorotateToLandscapeRight: 1
  allowedAutorotateToLandscapeLeft: 1
  useOSAutorotation: 1
  use32BitDisplayBuffer: 1
  preserveFramebufferAlpha: 0
  adjustIOSFPSUsingThermalState: 1
  thermalStateSeriousIOSFPS: 30
  thermalStateCriticalIOSFPS: 15
  disableDepthAndStencilBuffers: 0
  androidStartInFullscreen: 1
  androidRenderOutsideSafeArea: 1
  androidUseSwappy: 0
  androidRequestedVisibleInsets: 0
  androidSystemBarsBehavior: 2
  androidDisplayOptions: 1
  androidBlitType: 0
  androidResizeableActivity: 1
  androidDefaultWindowWidth: 1920
  androidDefaultWindowHeight: 1080
  androidMinimumWindowWidth: 400
  androidMinimumWindowHeight: 300
  androidFullscreenMode: 1
  androidAutoRotationBehavior: 1
  androidPredictiveBackSupport: 1
  androidApplicationEntry: 2
  defaultIsNativeResolution: 1
  macRetinaSupport: 1
  runInBackground: 0
  callOnDisableOnAssetBundleUnload: 1
  muteOtherAudioSources: 0
  Prepare IOS For Recording: 0
  Force IOS Speakers When Recording: 0
  audioSpatialExperience: 0
  deferSystemGesturesMode: 0
  hideHomeButton: 0
  submitAnalytics: 1
  usePlayerLog: 1
  dedicatedServerOptimizations: 1
  bakeCollisionMeshes: 0
  forceSingleInstance: 0
  useFlipModelSwapchain: 1
  resizableWindow: 0
  useMacAppStoreValidation: 0
  macAppStoreCategory: public.app-category.games
  gpuSkinning: 1
  meshDeformation: 2
  xboxPIXTextureCapture: 0
  xboxEnableAvatar: 0
  xboxEnableKinect: 0
  xboxEnableKinectAutoTracking: 0
  xboxEnableFitness: 0
  visibleInBackground: 1
  allowFullscreenSwitch: 1
  fullscreenMode: 1
  xboxSpeechDB: 0
  xboxEnableHeadOrientation: 0
  xboxEnableGuest: 0
  xboxEnablePIXSampling: 0
  metalFramebufferOnly: 0
  metalUseMetalDisplayLink: 0
  xboxOneResolution: 0
  xboxOneSResolution: 0
  xboxOneXResolution: 3
  xboxOneMonoLoggingLevel: 0
  xboxOneLoggingLevel: 1
  xboxOneDisableEsram: 0
  xboxOneEnableTypeOptimization: 0
  xboxOnePresentImmediateThreshold: 0
  switchQueueCommandMemory: 1048576
  switchQueueControlMemory: 16384
  switchQueueComputeMemory: 262144
  switchNVNShaderPoolsGranularity: 33554432
  switchNVNDefaultPoolsGranularity: 16777216
  switchNVNOtherPoolsGranularity: 16777216
  switchGpuScratchPoolGranularity: 2097152
  switchAllowGpuScratchShrinking: 0
  switchNVNMaxPublicTextureIDCount: 0
  switchNVNMaxPublicSamplerIDCount: 0
  switchMaxWorkerMultiple: 8
  switchNVNGraphicsFirmwareMemory: 32
  switchGraphicsJobsSyncAfterKick: 1
  vulkanNumSwapchainBuffers: 3
  vulkanEnableSetSRGBWrite: 0
  vulkanEnablePreTransform: 1
  vulkanEnableLateAcquireNextImage: 0
  vulkanEnableCommandBufferRecycling: 1
  loadStoreDebugModeEnabled: 0
  visionOSBundleVersion: 1.0
  tvOSBundleVersion: 1.0
  bundleVersion: 0.1.0
  preloadedAssets:
  - {fileID: -944628639613478452, guid: 052faaac586de48259a63d0c4782560b, type: 3}
  metroInputSource: 0
  wsaTransparentSwapchain: 0
  xboxOneDisableKinectGpuReservation: 1
  xboxOneEnable7thCore: 1
  vrSettings:
    enable360StereoCapture: 0
  enableFrameTimingStats: 0
  enableOpenGLProfilerGPURecorders: 1
  allowHDRDisplaySupport: 0
  useHDRDisplay: 0
  hdrBitDepth: 0
  m_ColorGamuts: 00000000i
  targetPixelDensity: 30
  resolutionScalingMode: 0
  resetResolutionOnWindowResize: 0
  androidSupportedAspectRatio: 1
  androidMaxAspectRatio: 2.4
  androidMinAspectRatio: 1
  applicationIdentifier:
    Android: com.UnityTechnologies.com.unity.template.urpblank
    Standalone: com.Unity-Technologies.com.unity.template.urp-blank
    iPhone: com.Unity-Technologies.com.unity.template.urp-blank
  buildNumber:
    Standalone: 0
    VisionOS: 0
    iPhone: 0
    tvOS: 0
  overrideDefaultApplicationIdentifier: 1
  AndroidBundleVersionCode: 1
  AndroidMinSdkVersion: 26
  AndroidTargetSdkVersion: 0
  AndroidPreferredInstallLocation: 1
  AndroidPreferredDataLocation: 1
  aotOptions: 
  stripEngineCode: 1
  iPhoneStrippingLevel: 0
  iPhoneScriptCallOptimization: 0
  ForceInternetPermission: 0
  ForceSDCardPermission: 0
  CreateWallpaper: 0
  androidSplitApplicationBinary: 0
  keepLoadedShadersAlive: 0
  StripUnusedMeshComponents: 0
  strictShaderVariantMatching: 0
  VertexChannelCompressionMask: 4054
  iPhoneSdkVersion: 988
  iOSSimulatorArchitecture: 0
  iOSTargetOSVersionString: 15.0
  tvOSSdkVersion: 0
  tvOSSimulatorArchitecture: 0
  tvOSRequireExtendedGameController: 0
  tvOSTargetOSVersionString: 15.0
  VisionOSSdkVersion: 0
  VisionOSTargetOSVersionString: 1.0
  xcodeProjectType: 0
  uIPrerenderedIcon: 0
  uIRequiresPersistentWiFi: 0
  uIRequiresFullScreen: 1
  uIStatusBarHidden: 1
  uIExitOnSuspend: 0
  uIStatusBarStyle: 0
  appleTVSplashScreen: {fileID: 0}
  appleTVSplashScreen2x: {fileID: 0}
  tvOSSmallIconLayers: []
  tvOSSmallIconLayers2x: []
  tvOSLargeIconLayers: []
  tvOSLargeIconLayers2x: []
  tvOSTopShelfImageLayers: []
  tvOSTopShelfImageLayers2x: []
  tvOSTopShelfImageWideLayers: []
  tvOSTopShelfImageWideLayers2x: []
  iOSLaunchScreenType: 0
  iOSLaunchScreenPortrait: {fileID: 0}
  iOSLaunchScreenLandscape: {fileID: 0}
  iOSLaunchScreenBackgroundColor:
    serializedVersion: 2
    rgba: 0
  iOSLaunchScreenFillPct: 100
  iOSLaunchScreenSize: 100
  iOSLaunchScreeniPadType: 0
  iOSLaunchScreeniPadImage: {fileID: 0}
  iOSLaunchScreeniPadBackgroundColor:
    serializedVersion: 2
    rgba: 0
  iOSLaunchScreeniPadFillPct: 100
  iOSLaunchScreeniPadSize: 100
  iOSLaunchScreenCustomStoryboardPath: 
  iOSLaunchScreeniPadCustomStoryboardPath: 
  iOSDeviceRequirements: []
  iOSURLSchemes: []
  macOSURLSchemes: []
  iOSBackgroundModes: 0
  iOSMetalForceHardShadows: 0
  metalEditorSupport: 1
  metalAPIValidation: 1
  metalCompileShaderBinary: 0
  iOSRenderExtraFrameOnPause: 0
  iosCopyPluginsCodeInsteadOfSymlink: 0
  appleDeveloperTeamID: 
  iOSManualSigningProvisioningProfileID: 
  tvOSManualSigningProvisioningProfileID: 
  VisionOSManualSigningProvisioningProfileID: 
  iOSManualSigningProvisioningProfileType: 0
  tvOSManualSigningProvisioningProfileType: 0
  VisionOSManualSigningProvisioningProfileType: 0
  appleEnableAutomaticSigning: 0
  iOSRequireARKit: 0
  iOSAutomaticallyDetectAndAddCapabilities: 1
  appleEnableProMotion: 0
  shaderPrecisionModel: 0
  clonedFromGUID: 3c72c65a16f0acb438eed22b8b16c24a
  templatePackageId: com.unity.template.urp-blank@17.2.1
  templateDefaultScene: Assets/Scenes/SampleScene.unity
  useCustomMainManifest: 0
  useCustomLauncherManifest: 0
  useCustomMainGradleTemplate: 0
  useCustomLauncherGradleManifest: 0
  useCustomBaseGradleTemplate: 0
  useCustomGradlePropertiesTemplate: 0
  useCustomGradleSettingsTemplate: 0
  useCustomProguardFile: 0
  AndroidTargetArchitectures: 2
  AndroidSplashScreenScale: 0
  androidSplashScreen: {fileID: 0}
  AndroidKeystoreName: 
  AndroidKeyaliasName: 
  AndroidEnableArmv9SecurityFeatures: 0
  AndroidEnableArm64MTE: 0
  AndroidBuildApkPerCpuArchitecture: 0
  AndroidTVCompatibility: 0
  AndroidIsGame: 1
  androidAppCategory: 3
  useAndroidAppCategory: 1
  androidAppCategoryOther: 
  AndroidEnableTango: 0
  androidEnableBanner: 1
  androidUseLowAccuracyLocation: 0
  androidUseCustomKeystore: 0
  m_AndroidBanners:
  - width: 320
    height: 180
    banner: {fileID: 0}
  androidGamepadSupportLevel: 0
  AndroidMinifyRelease: 0
  AndroidMinifyDebug: 0
  AndroidValidateAppBundleSize: 1
  AndroidAppBundleSizeToValidate: 150
  AndroidReportGooglePlayAppDependencies: 1
  androidSymbolsSizeThreshold: 800
  m_BuildTargetIcons: []
  m_BuildTargetPlatformIcons:
  - m_BuildTarget: iPhone
    m_Icons:
    - m_Textures: []
      m_Width: 180
      m_Height: 180
      m_Kind: 0
      m_SubKind: iPhone
    - m_Textures: []
      m_Width: 120
      m_Height: 120
      m_Kind: 0
      m_SubKind: iPhone
    - m_Textures: []
      m_Width: 167
      m_Height: 167
      m_Kind: 0
      m_SubKind: iPad
    - m_Textures: []
      m_Width: 152
      m_Height: 152
      m_Kind: 0
      m_SubKind: iPad
    - m_Textures: []
      m_Width: 76
      m_Height: 76
      m_Kind: 0
      m_SubKind: iPad
    - m_Textures: []
      m_Width: 120
      m_Height: 120
      m_Kind: 3
      m_SubKind: iPhone
    - m_Textures: []
      m_Width: 80
      m_Height: 80
      m_Kind: 3
      m_SubKind: iPhone
    - m_Textures: []
      m_Width: 80
      m_Height: 80
      m_Kind: 3
      m_SubKind: iPad
    - m_Textures: []
      m_Width: 40
      m_Height: 40
      m_Kind: 3
      m_SubKind: iPad
    - m_Textures: []
      m_Width: 87
      m_Height: 87
      m_Kind: 1
      m_SubKind: iPhone
    - m_Textures: []
      m_Width: 58
      m_Height: 58
      m_Kind: 1
      m_SubKind: iPhone
    - m_Textures: []
      m_Width: 29
      m_Height: 29
      m_Kind: 1
      m_SubKind: iPhone
    - m_Textures: []
      m_Width: 58
      m_Height: 58
      m_Kind: 1
      m_SubKind: iPad
    - m_Textures: []
      m_Width: 29
      m_Height: 29
      m_Kind: 1
      m_SubKind: iPad
    - m_Textures: []
      m_Width: 60
      m_Height: 60
      m_Kind: 2
      m_SubKind: iPhone
    - m_Textures: []
      m_Width: 40
      m_Height: 40
      m_Kind: 2
      m_SubKind: iPhone
    - m_Textures: []
      m_Width: 40
      m_Height: 40
      m_Kind: 2
      m_SubKind: iPad
    - m_Textures: []
      m_Width: 20
      m_Height: 20
      m_Kind: 2
      m_SubKind: iPad
    - m_Textures: []
      m_Width: 1024
      m_Height: 1024
      m_Kind: 4
      m_SubKind: App Store
  - m_BuildTarget: Android
    m_Icons:
    - m_Textures: []
      m_Width: 432
      m_Height: 432
      m_Kind: 2
      m_SubKind: 
    - m_Textures: []
      m_Width: 324
      m_Height: 324
      m_Kind: 2
      m_SubKind: 
    - m_Textures: []
      m_Width: 216
      m_Height: 216
      m_Kind: 2
      m_SubKind: 
    - m_Textures: []
      m_Width: 162
      m_Height: 162
      m_Kind: 2
      m_SubKind: 
    - m_Textures: []
      m_Width: 108
      m_Height: 108
      m_Kind: 2
      m_SubKind: 
    - m_Textures: []
      m_Width: 81
      m_Height: 81
      m_Kind: 2
      m_SubKind: 
    - m_Textures: []
      m_Width: 192
      m_Height: 192
      m_Kind: 1
      m_SubKind: 
    - m_Textures: []
      m_Width: 144
      m_Height: 144
      m_Kind: 1
      m_SubKind: 
    - m_Textures: []
      m_Width: 96
      m_Height: 96
      m_Kind: 1
      m_SubKind: 
    - m_Textures: []
      m_Width: 72
      m_Height: 72
      m_Kind: 1
      m_SubKind: 
    - m_Textures: []
      m_Width: 48
      m_Height: 48
      m_Kind: 1
      m_SubKind: 
    - m_Textures: []
      m_Width: 36
      m_Height: 36
      m_Kind: 1
      m_SubKind: 
    - m_Textures: []
      m_Width: 192
      m_Height: 192
      m_Kind: 0
      m_SubKind: 
    - m_Textures: []
      m_Width: 144
      m_Height: 144
      m_Kind: 0
      m_SubKind: 
    - m_Textures: []
      m_Width: 96
      m_Height: 96
      m_Kind: 0
      m_SubKind: 
    - m_Textures: []
      m_Width: 72
      m_Height: 72
      m_Kind: 0
      m_SubKind: 
    - m_Textures: []
      m_Width: 48
      m_Height: 48
      m_Kind: 0
      m_SubKind: 
    - m_Textures: []
      m_Width: 36
      m_Height: 36
      m_Kind: 0
      m_SubKind: 
  - m_BuildTarget: tvOS
    m_Icons:
    - m_Textures: []
      m_Width: 1280
      m_Height: 768
      m_Kind: 0
      m_SubKind: 
    - m_Textures: []
      m_Width: 800
      m_Height: 480
      m_Kind: 0
      m_SubKind: 
    - m_Textures: []
      m_Width: 400
      m_Height: 240
      m_Kind: 0
      m_SubKind: 
    - m_Textures: []
      m_Width: 4640
      m_Height: 1440
      m_Kind: 1
      m_SubKind: 
    - m_Textures: []
      m_Width: 2320
      m_Height: 720
      m_Kind: 1
      m_SubKind: 
    - m_Textures: []
      m_Width: 3840
      m_Height: 1440
      m_Kind: 1
      m_SubKind: 
    - m_Textures: []
      m_Width: 1920
      m_Height: 720
      m_Kind: 1
      m_SubKind: 
  m_BuildTargetBatching: []
  m_BuildTargetShaderSettings: []
  m_BuildTargetGraphicsJobs: []
  m_BuildTargetGraphicsJobMode: []
  m_BuildTargetGraphicsAPIs:
  - m_BuildTarget: iOSSupport
    m_APIs: 10000000i
    m_Automatic: 1
  - m_BuildTarget: AndroidPlayer
    m_APIs: 150000000b000000i
    m_Automatic: 0
  - m_BuildTarget: WindowsStandaloneSupport
    m_APIs: 0200000012000000i
    m_Automatic: 0
  m_DefaultShaderChunkSizeInMB: 16
  m_DefaultShaderChunkCount: 0
  openGLRequireES31: 0
  openGLRequireES31AEP: 0
  openGLRequireES32: 0
  m_TemplateCustomTags: {}
  mobileMTRendering:
    Android: 1
    iPhone: 1
    tvOS: 1
  m_BuildTargetGroupLightmapEncodingQuality:
  - serializedVersion: 2
    m_BuildTarget: Android
    m_EncodingQuality: 1
  m_BuildTargetGroupHDRCubemapEncodingQuality: []
  m_BuildTargetGroupLightmapSettings: []
  m_BuildTargetGroupLoadStoreDebugModeSettings: []
  m_BuildTargetNormalMapEncoding:
  - m_BuildTarget: Android
    m_Encoding: 1
  m_BuildTargetDefaultTextureCompressionFormat:
  - serializedVersion: 3
    m_BuildTarget: Android
    m_Formats: 03000000i
  playModeTestRunnerEnabled: 0
  runPlayModeTestAsEditModeTest: 0
  actionOnDotNetUnhandledException: 1
  editorGfxJobOverride: 1
  enableInternalProfiler: 0
  logObjCUncaughtExceptions: 1
  enableCrashReportAPI: 0
  cameraUsageDescription: 
  locationUsageDescription: 
  microphoneUsageDescription: 
  bluetoothUsageDescription: 
  macOSTargetOSVersion: 12.0
  switchNMETAOverride: 
  switchNetLibKey: 
  switchSocketMemoryPoolSize: 6144
  switchSocketAllocatorPoolSize: 128
  switchSocketConcurrencyLimit: 14
  switchScreenResolutionBehavior: 2
  switchUseCPUProfiler: 0
  switchEnableFileSystemTrace: 0
  switchLTOSetting: 0
  switchApplicationID: 0x01004b9000490000
  switchNSODependencies: 
  switchCompilerFlags: 
  switchTitleNames_0: 
  switchTitleNames_1: 
  switchTitleNames_2: 
  switchTitleNames_3: 
  switchTitleNames_4: 
  switchTitleNames_5: 
  switchTitleNames_6: 
  switchTitleNames_7: 
  switchTitleNames_8: 
  switchTitleNames_9: 
  switchTitleNames_10: 
  switchTitleNames_11: 
  switchTitleNames_12: 
  switchTitleNames_13: 
  switchTitleNames_14: 
  switchTitleNames_15: 
  switchPublisherNames_0: 
  switchPublisherNames_1: 
  switchPublisherNames_2: 
  switchPublisherNames_3: 
  switchPublisherNames_4: 
  switchPublisherNames_5: 
  switchPublisherNames_6: 
  switchPublisherNames_7: 
  switchPublisherNames_8: 
  switchPublisherNames_9: 
  switchPublisherNames_10: 
  switchPublisherNames_11: 
  switchPublisherNames_12: 
  switchPublisherNames_13: 
  switchPublisherNames_14: 
  switchPublisherNames_15: 
  switchIcons_0: {fileID: 0}
  switchIcons_1: {fileID: 0}
  switchIcons_2: {fileID: 0}
  switchIcons_3: {fileID: 0}
  switchIcons_4: {fileID: 0}
  switchIcons_5: {fileID: 0}
  switchIcons_6: {fileID: 0}
  switchIcons_7: {fileID: 0}
  switchIcons_8: {fileID: 0}
  switchIcons_9: {fileID: 0}
  switchIcons_10: {fileID: 0}
  switchIcons_11: {fileID: 0}
  switchIcons_12: {fileID: 0}
  switchIcons_13: {fileID: 0}
  switchIcons_14: {fileID: 0}
  switchIcons_15: {fileID: 0}
  switchSmallIcons_0: {fileID: 0}
  switchSmallIcons_1: {fileID: 0}
  switchSmallIcons_2: {fileID: 0}
  switchSmallIcons_3: {fileID: 0}
  switchSmallIcons_4: {fileID: 0}
  switchSmallIcons_5: {fileID: 0}
  switchSmallIcons_6: {fileID: 0}
  switchSmallIcons_7: {fileID: 0}
  switchSmallIcons_8: {fileID: 0}
  switchSmallIcons_9: {fileID: 0}
  switchSmallIcons_10: {fileID: 0}
  switchSmallIcons_11: {fileID: 0}
  switchSmallIcons_12: {fileID: 0}
  switchSmallIcons_13: {fileID: 0}
  switchSmallIcons_14: {fileID: 0}
  switchSmallIcons_15: {fileID: 0}
  switchManualHTML: 
  switchAccessibleURLs: 
  switchLegalInformation: 
  switchMainThreadStackSize: 1048576
  switchPresenceGroupId: 
  switchLogoHandling: 0
  switchReleaseVersion: 0
  switchDisplayVersion: 1.0.0
  switchStartupUserAccount: 0
  switchSupportedLanguagesMask: 0
  switchLogoType: 0
  switchApplicationErrorCodeCategory: 
  switchUserAccountSaveDataSize: 0
  switchUserAccountSaveDataJournalSize: 0
  switchApplicationAttribute: 0
  switchCardSpecSize: -1
  switchCardSpecClock: -1
  switchRatingsMask: 0
  switchRatingsInt_0: 0
  switchRatingsInt_1: 0
  switchRatingsInt_2: 0
  switchRatingsInt_3: 0
  switchRatingsInt_4: 0
  switchRatingsInt_5: 0
  switchRatingsInt_6: 0
  switchRatingsInt_7: 0
  switchRatingsInt_8: 0
  switchRatingsInt_9: 0
  switchRatingsInt_10: 0
  switchRatingsInt_11: 0
  switchRatingsInt_12: 0
  switchLocalCommunicationIds_0: 
  switchLocalCommunicationIds_1: 
  switchLocalCommunicationIds_2: 
  switchLocalCommunicationIds_3: 
  switchLocalCommunicationIds_4: 
  switchLocalCommunicationIds_5: 
  switchLocalCommunicationIds_6: 
  switchLocalCommunicationIds_7: 
  switchParentalControl: 0
  switchAllowsScreenshot: 1
  switchAllowsVideoCapturing: 1
  switchAllowsRuntimeAddOnContentInstall: 0
  switchDataLossConfirmation: 0
  switchUserAccountLockEnabled: 0
  switchSystemResourceMemory: 16777216
  switchSupportedNpadStyles: 22
  switchNativeFsCacheSize: 32
  switchIsHoldTypeHorizontal: 0
  switchSupportedNpadCount: 8
  switchEnableTouchScreen: 1
  switchSocketConfigEnabled: 0
  switchTcpInitialSendBufferSize: 32
  switchTcpInitialReceiveBufferSize: 64
  switchTcpAutoSendBufferSizeMax: 256
  switchTcpAutoReceiveBufferSizeMax: 256
  switchUdpSendBufferSize: 9
  switchUdpReceiveBufferSize: 42
  switchSocketBufferEfficiency: 4
  switchSocketInitializeEnabled: 1
  switchNetworkInterfaceManagerInitializeEnabled: 1
  switchDisableHTCSPlayerConnection: 0
  switchUseNewStyleFilepaths: 0
  switchUseLegacyFmodPriorities: 0
  switchUseMicroSleepForYield: 1
  switchEnableRamDiskSupport: 0
  switchMicroSleepForYieldTime: 25
  switchRamDiskSpaceSize: 12
  switchUpgradedPlayerSettingsToNMETA: 0
  switchCaStoreSource: 0
  switchCaStoreFilePath: 
  ps4NPAgeRating: 12
  ps4NPTitleSecret: 
  ps4NPTrophyPackPath: 
  ps4ParentalLevel: 11
  ps4ContentID: ED1633-NPXX51362_00-0000000000000000
  ps4Category: 0
  ps4MasterVersion: 01.00
  ps4AppVersion: 01.00
  ps4AppType: 0
  ps4ParamSfxPath: 
  ps4VideoOutPixelFormat: 0
  ps4VideoOutInitialWidth: 1920
  ps4VideoOutBaseModeInitialWidth: 1920
  ps4VideoOutReprojectionRate: 60
  ps4PronunciationXMLPath: 
  ps4PronunciationSIGPath: 
  ps4BackgroundImagePath: 
  ps4StartupImagePath: 
  ps4StartupImagesFolder: 
  ps4IconImagesFolder: 
  ps4SaveDataImagePath: 
  ps4SdkOverride: 
  ps4BGMPath: 
  ps4ShareFilePath: 
  ps4ShareOverlayImagePath: 
  ps4PrivacyGuardImagePath: 
  ps4ExtraSceSysFile: 
  ps4NPtitleDatPath: 
  ps4RemotePlayKeyAssignment: -1
  ps4RemotePlayKeyMappingDir: 
  ps4PlayTogetherPlayerCount: 0
  ps4EnterButtonAssignment: 2
  ps4ApplicationParam1: 0
  ps4ApplicationParam2: 0
  ps4ApplicationParam3: 0
  ps4ApplicationParam4: 0
  ps4DownloadDataSize: 0
  ps4GarlicHeapSize: 2048
  ps4ProGarlicHeapSize: 2560
  playerPrefsMaxSize: 32768
  ps4Passcode: frAQBc8Wsa1xVPfvJcrgRYwTiizs2trQ
  ps4pnSessions: 1
  ps4pnPresence: 1
  ps4pnFriends: 1
  ps4pnGameCustomData: 1
  playerPrefsSupport: 0
  enableApplicationExit: 0
  resetTempFolder: 1
  restrictedAudioUsageRights: 0
  ps4UseResolutionFallback: 0
  ps4ReprojectionSupport: 0
  ps4UseAudio3dBackend: 0
  ps4UseLowGarlicFragmentationMode: 1
  ps4SocialScreenEnabled: 0
  ps4ScriptOptimizationLevel: 2
  ps4Audio3dVirtualSpeakerCount: 14
  ps4attribCpuUsage: 0
  ps4PatchPkgPath: 
  ps4PatchLatestPkgPath: 
  ps4PatchChangeinfoPath: 
  ps4PatchDayOne: 0
  ps4attribUserManagement: 0
  ps4attribMoveSupport: 0
  ps4attrib3DSupport: 0
  ps4attribShareSupport: 0
  ps4attribExclusiveVR: 0
  ps4disableAutoHideSplash: 0
  ps4videoRecordingFeaturesUsed: 0
  ps4contentSearchFeaturesUsed: 0
  ps4CompatibilityPS5: 0
  ps4AllowPS5Detection: 0
  ps4GPU800MHz: 1
  ps4attribEyeToEyeDistanceSettingVR: 0
  ps4IncludedModules: []
  ps4attribVROutputEnabled: 0
  monoEnv: 
  splashScreenBackgroundSourceLandscape: {fileID: 0}
  splashScreenBackgroundSourcePortrait: {fileID: 0}
  blurSplashScreenBackground: 1
  spritePackerPolicy: 
  webGLMemorySize: 32
  webGLExceptionSupport: 1
  webGLNameFilesAsHashes: 0
  webGLShowDiagnostics: 0
  webGLDataCaching: 1
  webGLDebugSymbols: 0
  webGLEmscriptenArgs: 
  webGLModulesDirectory: 
  webGLTemplate: APPLICATION:Default
  webGLAnalyzeBuildSize: 0
  webGLUseEmbeddedResources: 0
  webGLCompressionFormat: 0
  webGLWasmArithmeticExceptions: 0
  webGLLinkerTarget: 1
  webGLThreadsSupport: 0
  webGLDecompressionFallback: 0
  webGLInitialMemorySize: 32
  webGLMaximumMemorySize: 2048
  webGLMemoryGrowthMode: 2
  webGLMemoryLinearGrowthStep: 16
  webGLMemoryGeometricGrowthStep: 0.2
  webGLMemoryGeometricGrowthCap: 96
  webGLPowerPreference: 2
  webGLWebAssemblyTable: 0
  webGLWebAssemblyBigInt: 0
  webGLCloseOnQuit: 0
  webWasm2023: 1
  webEnableSubmoduleStrippingCompatibility: 0
  webProgressiveAssetLoading: 0
  scriptingDefineSymbols: {}
  additionalCompilerArguments: {}
  platformArchitecture: {}
  scriptingBackend:
    Android: 1
  il2cppCompilerConfiguration: {}
  il2cppCodeGeneration: {}
  il2cppLTOMode: {}
  il2cppStacktraceInformation: {}
  managedStrippingLevel: {}
  incrementalIl2cppBuild: {}
  suppressCommonWarnings: 1
  allowUnsafeCode: 0
  useDeterministicCompilation: 1
  additionalIl2CppArgs: 
  scriptingRuntimeVersion: 1
  gcIncremental: 1
  gcWBarrierValidation: 0
  managedCodeVariant: {}
  apiCompatibilityLevelPerPlatform: {}
  editorAssembliesCompatibilityLevel: 1
  m_RenderingPath: 1
  m_MobileRenderingPath: 1
  metroPackageName: RoboClash
  metroPackageVersion: 
  metroCertificatePath: 
  metroCertificatePassword: 
  metroCertificateSubject: 
  metroCertificateIssuer: 
  metroCertificateNotAfter: 0000000000000000
  metroApplicationDescription: RoboClash
  wsaImages: {}
  metroTileShortName: 
  metroTileShowName: 0
  metroMediumTileShowName: 0
  metroLargeTileShowName: 0
  metroWideTileShowName: 0
  metroSupportStreamingInstall: 0
  metroLastRequiredScene: 0
  metroDefaultTileSize: 1
  metroTileForegroundText: 2
  metroTileBackgroundColor: {r: 0.13333334, g: 0.17254902, b: 0.21568628, a: 0}
  metroSplashScreenBackgroundColor: {r: 0.12941177, g: 0.17254902, b: 0.21568628, a: 1}
  metroSplashScreenUseBackgroundColor: 0
  syncCapabilities: 0
  platformCapabilities: {}
  metroTargetDeviceFamilies: {}
  metroFTAName: 
  metroFTAFileTypes: []
  metroProtocolName: 
  vcxProjDefaultLanguage: 
  XboxOneProductId: 
  XboxOneUpdateKey: 
  XboxOneSandboxId: 
  XboxOneContentId: 
  XboxOneTitleId: 
  XboxOneSCId: 
  XboxOneGameOsOverridePath: 
  XboxOnePackagingOverridePath: 
  XboxOneAppManifestOverridePath: 
  XboxOneVersion: 1.0.0.0
  XboxOnePackageEncryption: 0
  XboxOnePackageUpdateGranularity: 2
  XboxOneDescription: 
  XboxOneLanguage:
  - enus
  XboxOneCapability: []
  XboxOneGameRating: {}
  XboxOneIsContentPackage: 0
  XboxOneEnhancedXboxCompatibilityMode: 0
  XboxOneEnableGPUVariability: 1
  XboxOneSockets: {}
  XboxOneSplashScreen: {fileID: 0}
  XboxOneAllowedProductIds: []
  XboxOnePersistentLocalStorageSize: 0
  XboxOneXTitleMemory: 8
  XboxOneOverrideIdentityName: 
  XboxOneOverrideIdentityPublisher: 
  cloudServicesEnabled: {}
  luminIcon:
    m_Name: 
    m_ModelFolderPath: 
    m_PortalFolderPath: 
  luminCert:
    m_CertPath: 
    m_SignPackage: 1
  luminIsChannelApp: 0
  luminVersion:
    m_VersionCode: 1
    m_VersionName: 
  hmiPlayerDataPath: 
  hmiForceSRGBBlit: 1
  embeddedLinuxEnableGamepadInput: 0
  hmiCpuConfiguration: 
  hmiLogStartupTiming: 0
  qnxGraphicConfPath: 
  apiCompatibilityLevel: 6
  captureStartupLogs: {}
  activeInputHandler: 1
  windowsGamepadBackendHint: 0
  enableDirectStorage: 0
  cloudProjectId: 71b8279a-1d3b-487a-be9c-bdaa5837953f
  framebufferDepthMemorylessMode: 0
  qualitySettingsNames: []
  projectName: RoboClash
  organizationId: mohamedeabidi1
  cloudEnabled: 0
  legacyClampBlendShapeWeights: 0
  hmiLoadingImage: {fileID: 0}
  platformRequiresReadableAssets: 0
  virtualTexturingSupportEnabled: 0
  insecureHttpOption: 0
  androidVulkanDenyFilterList: []
  androidVulkanAllowFilterList: []
  androidVulkanDeviceFilterListAsset: {fileID: 0}
  d3d12DeviceFilterListAsset: {fileID: 0}
  webGPUDeviceFilterListAsset: {fileID: 0}
  allowedHttpConnections: 3
````

### `ProjectSettings/EditorSettings.asset`

````yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!159 &1
EditorSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 16
  m_SerializationMode: 2
  m_LineEndingsForNewScripts: 0
  m_DefaultBehaviorMode: 0
  m_PrefabRegularEnvironment: {fileID: 0}
  m_PrefabUIEnvironment: {fileID: 0}
  m_SpritePackerMode: 0
  m_SpritePackerCacheSize: 10
  m_SpritePackerPaddingPower: 1
  m_Bc7TextureCompressor: 0
  m_EtcTextureCompressorBehavior: 1
  m_EtcTextureFastCompressor: 1
  m_EtcTextureNormalCompressor: 2
  m_EtcTextureBestCompressor: 4
  m_ProjectGenerationIncludedExtensions: txt;xml;fnt;cd;asmdef;rsp;asmref
  m_ProjectGenerationRootNamespace: 
  m_EnableTextureStreamingInEditMode: 1
  m_EnableTextureStreamingInPlayMode: 1
  m_EnableEditorAsyncCPUTextureLoading: 0
  m_AsyncShaderCompilation: 1
  m_BlockShaders: 0
  m_UnlockBlockShaders: 0
  m_PrefabModeAllowAutoSave: 1
  m_EnterPlayModeOptionsEnabled: 0
  m_EnterPlayModeOptions: 3
  m_GameObjectNamingDigits: 1
  m_GameObjectNamingScheme: 0
  m_AssetNamingUsesSpace: 1
  m_InspectorUseIMGUIDefaultInspector: 0
  m_UseLegacyProbeSampleCount: 0
  m_DisableCookiesInLightmapper: 0
  m_ShadowmaskStitching: 0
  m_AssetPipelineMode: 1
  m_RefreshImportMode: 0
  m_CacheServerMode: 0
  m_CacheServerEndpoint: 
  m_CacheServerNamespacePrefix: default
  m_CacheServerEnableDownload: 1
  m_CacheServerEnableUpload: 1
  m_CacheServerEnableTls: 0
  m_CacheServerImportResultCachingEnabled: 0
  m_CacheServerValidationMode: 2
  m_CacheServerDownloadBatchSize: 128
  m_EnableEnlightenBakedGI: 0
  m_ReferencedClipsExactNaming: 1
  m_ForceAssetUnloadAndGCOnSceneLoad: 1
  m_HideBuildProfileClassicPlatforms: 1
  m_UseLegacyHierarchy: 0
````

### `ProjectSettings/EditorBuildSettings.asset`

````yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1045 &1
EditorBuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Scenes:
  - enabled: 1
    path: Assets/Scenes/RoboClashMainMenu.unity
    guid: 329f808574cc20e47a77de170e6db3ed
  - enabled: 1
    path: Assets/Scenes/RoboClashGameplay.unity
    guid: 799eb266dd37aef45baee15aa027ab6f
  - enabled: 0
    path: Assets/Scenes/Arena.unity
    guid: 99c9720ab356a0642a771bea13969a05
  m_configObjects:
    com.unity.input.settings.actions: {fileID: -944628639613478452, guid: 052faaac586de48259a63d0c4782560b, type: 3}
````
