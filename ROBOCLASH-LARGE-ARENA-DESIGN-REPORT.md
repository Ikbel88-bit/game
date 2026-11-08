# RoboClash Large Arena: Design and Code Report

## What this report covers

This report describes the large playable environment used by the gameplay scene, how its main areas fit together, what hazards and match-mode features are present, and where the scene and behavior are implemented in source.

This report documents the original authored arena in `RoboClashGameplay`. Menu-started matches now load the separately generated `RoboClashGameplay_XL` scene; the original is retained unchanged as a disabled Build Settings entry. The XL scene has its own generated layout, while the scene and component details below describe the original arena unless noted otherwise.

## Arena at a glance

| Feature | Current scene |
|---|---|
| Play space | One continuous square floor, 120 by 120 Unity units |
| Outer boundary | Four solid bulkheads centered near the ±59.5-unit X/Z edges |
| Layout landmarks | Central hub/reactor, workshop area, hazard chamber, and connecting floor sections |
| Vertical routes | Workshop catwalk, entry/exit ramps, rails, and raised armor cover |
| Lethal/damaging features | Four lava-channel hazard segments and two spike traps |
| Spawn support | Four indexed `ArenaSpawnZone` components; additional named beacon props decorate the map |
| Match modes | Free-for-all/team-style combat, Survival, King of the Arena, and Knockout are handled by the match manager |

Dimensions are read from the scene transforms and refer to Unity world units. The outer wall centers and the floor scale indicate an approximately 120-unit square footprint; the floor is a continuous base rather than a pit-separated set of islands.

## Layout and visual identity

### Perimeter and main floor

The arena sits beneath four tall perimeter bulkheads. For example, the north wall is positioned at `(0, 3.5, 59.5)` and scaled to `(120, 7, 1)`; the south, east, and west walls close the other sides. They are solid colliders, so they bound the combat space as well as framing the industrial interior.

The scene's `Large Complex Continuous Arena Floor` is centered at `(0, -0.5, 0)` and scaled to `(120, 1, 120)`. It carries the `ArenaFloor` component, which lets the match manager validate safe robot spawn locations and lets the minimap identify the playable floor.

### Central hub and reactor

The hub is the arena's central visual and navigation landmark. The scene includes a reactor plinth, reactor core/crown/beacons, illuminated guide lines, and wall sections named for the four directions. Hub cover panels provide obstacles and break up long firing lanes around the middle.

In King of the Arena, the match manager adds a separate 10-by-10 control region at the origin. This scoring zone is a gameplay overlay, not a second hand-built arena floor; its occupancy is tracked from participant colliders.

### Workshop

The workshop is signposted and dressed as a machinery bay. Scene objects include press machines, hydraulic rams, tool crates, warning panels, door framing, catwalk supports, and a low catwalk with entry and exit ramps and rail accents. The raised walkway changes sight lines and gives players a route above the ground-level lanes.

Weapon and shield pickups are also part of the arena's scene content, giving players incentives to move through the side areas instead of holding a single angle.

### Hazard chamber and molten channels

The hazard chamber uses spike strips and separate marked routes, including risk-route and safe-route stripes. Around the map, the molten channel surfaces and rock/caution borders make the lava route legible before a player crosses it. Scene-authored lava segments use trigger colliders to apply damage over time; spike traps deal a single hit when entered.

### Connecting routes and spawns

Named north, south, east, and west room-connector floors join the larger spaces into one continuous map. Spawn beacons are visible landmarks, while the actual `ArenaSpawnZone` components carry spawn indices used by the match system. When an authored spawn is unavailable or unsafe, the manager checks spacing, floor support, walls, hazards, and other robots before accepting a position.

## How it plays

- **Long-range fights:** The broad square footprint leaves open firing lanes, especially between central cover and the outer routes.
- **Close engagements:** Hub walls, armor panels, machinery, and room connectors create cover and constrain movement.
- **Vertical choices:** The workshop catwalk and ramps offer alternate sight lines and movement paths; the rest of the map remains grounded on the continuous floor.
- **Hazard awareness:** Lava damages continuously while a robot remains in its trigger. Spikes damage once upon entry. These are gameplay hazards, not just glowing decoration.
- **Mode-specific objective:** King of the Arena tracks participants inside the center control volume and awards time while they occupy it.
- **Situational awareness:** The minimap derives its floor extent and obstacle/hazard shapes from active colliders beneath the arena root. It distinguishes floor, wall, obstacle, and hazard shapes, and displays participant positions.

## Code and scene source map

| Source | Responsibility |
|---|---|
| [RoboClashGameplay_XL.unity](./Assets/Scenes/RoboClashGameplay_XL.unity) | Active XL gameplay scene: XL world, match manager/HUD, gameplay camera, and retained gameplay systems. |
| [RoboClashGameplay.unity](./Assets/Scenes/RoboClashGameplay.unity) | Original arena retained as a disabled Build Settings scene. |
| [XLArenaBuilder.cs](./Assets/RoboClashXL/Editor/XLArenaBuilder.cs) | Creates the XL world and generated materials in the duplicate gameplay scene. |
| [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) | Finds spawn zones, checks safe placement, creates the King of the Arena control zone, and handles scoring/match flow. |
| [ArenaFloor.cs](./Assets/Scripts/RoboClash/ArenaFloor.cs) and [ArenaWall.cs](./Assets/Scripts/RoboClash/ArenaWall.cs) | Mark solid floor and wall colliders for gameplay systems. |
| [ArenaSpawnZone.cs](./Assets/Scripts/RoboClash/ArenaSpawnZone.cs) | Stores the authored spawn index. |
| [ArenaHazard.cs](./Assets/Scripts/RoboClash/ArenaHazard.cs), [LavaPit.cs](./Assets/Scripts/RoboClash/LavaPit.cs), and [SpikeTrap.cs](./Assets/Scripts/RoboClash/SpikeTrap.cs) | Shared trigger setup and the damage-over-time / one-time trap behavior. |
| [ArenaMovingPlatform.cs](./Assets/Scripts/RoboClash/ArenaMovingPlatform.cs) | Optional ping-pong moving platform behavior. This component exists in code but is not attached to an object in the current gameplay scene. |
| [RoboClashArenaVisuals.cs](./Assets/Scripts/RoboClash/Visuals/RoboClashArenaVisuals.cs) | C# helper for configuring shared URP materials; it is not a map generator. |
| [RoboClashMinimapV2.cs](./Assets/Scripts/RoboClash/UI/RoboClashMinimapV2.cs) | Builds the minimap's floor bounds and obstacle/hazard silhouettes from arena colliders. |

The separate [Arena.unity](./Assets/Scenes/Arena.unity) scene currently contains only basic scene setup (camera, light, and volume); the detailed playable map is in `RoboClashGameplay_XL.unity`.

## Representative source excerpts

### Scene-authored floor dimensions

The gameplay scene stores the continuous floor as a scaled Unity object:

```yaml
m_Name: Large Complex Continuous Arena Floor
m_LocalPosition: {x: 0, y: -0.5, z: 0}
m_LocalScale: {x: 120, y: 1, z: 120}
m_EditorClassIdentifier: Assembly-CSharp::RoboClash.ArenaFloor
```

Source: [playable floor object in the gameplay scene](./Assets/Scenes/RoboClashGameplay.unity#L37451).

### Hazard damage behavior

The common hazard base configures a trigger and applies damage through `ArenaDamageable`:

```csharp
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
```

`LavaPit.OnTriggerStay` calls `Damage`, while `SpikeTrap.OnTriggerEnter` calls `DamageOnce`. Sources: [ArenaHazard.cs](./Assets/Scripts/RoboClash/ArenaHazard.cs), [LavaPit.cs](./Assets/Scripts/RoboClash/LavaPit.cs), and [SpikeTrap.cs](./Assets/Scripts/RoboClash/SpikeTrap.cs).

### Spawn validation

The match manager identifies the scene's floor bounds and rejects positions beyond them or inside unsafe geometry. In particular, safe-spawn checks reject hazard overlaps and require a downward ground hit on a valid arena surface. Source: [`GetSpawnPositions`, `TryFindReservedSpawn`, and `IsSafeSpawn`](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs#L2011).

### King of the Arena objective volume

The objective is a trigger centered at the origin, with a matching cyan platform and four perimeter markers:

```csharp
trigger.isTrigger = true;
trigger.center = Vector3.up * 1.5f;
trigger.size = new Vector3(10f, 3f, 10f);
```

Source: [`CreateKingZone`](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs#L656). `KingOfArenaZone` records participants while one or more of their colliders overlap the trigger: [KingOfArenaZone.cs](./Assets/Scripts/RoboClash/KingOfArenaZone.cs).

### Minimap's scene-derived bounds

The minimap takes the combined X/Z bounds of the arena floor colliders, then classifies the remaining collider shapes as walls, hazards, or other obstacles. Source: [`RoboClashMinimapV2.Rebuild`](./Assets/Scripts/RoboClash/UI/RoboClashMinimapV2.cs#L52).

## Current implementation notes

- - The XL layout is generated by an Editor menu command into a separate Unity scene. Its C# builder is an Editor-time tool; the generated scene and assets are used at runtime.
- The scene has one continuous `ArenaFloor` base. The molten channels and hazard chamber add dangerous routes on top of that floor rather than opening gaps in the base.
- `ArenaMovingPlatform` provides movement logic for a platform, but no instance is currently configured in the gameplay scene. The workshop's raised route is built from static catwalk/ramp/rail objects.
- `ArenaDecoration` is an intentionally empty marker component. Decorations are visual scene objects; their collision and interaction come from other components where required.
