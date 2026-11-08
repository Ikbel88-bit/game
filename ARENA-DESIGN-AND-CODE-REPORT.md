# RoboClash Arena Design and Code Report

**Project:** RoboClash, Unity 6.6 / URP  
**Report date:** 2026-10-25  
**Scope:** Procedural arena layout, generator, runtime arena components, gameplay integration, generated assets, and instructions for inspecting or changing the code.

## 1. Arena at a glance

RoboClash's arena is built from reusable Unity GameObjects and runtime components by an Editor-only C# generator. Geometry, materials, lights, collision, hazards, pickups, and spawn markers are created in the active scene. The generator adds the existing match manager and HUD and connects the generated robot prefabs; it does not introduce another combat or match system.

There are two generator entry points:

| Unity menu command | Layout |
|---|---|
| **Tools > RoboClash > Generate Arena** | Compact 36 x 36 arena with a central raised deck, four corner spawn zones, hazards, pickups, and lighting. |
| **Tools > RoboClash > Generate Large Arena** | Opens a settings window for the connected, multi-room arena. Defaults are a 120 x 120 map, 36 x 36 central combat hub, and 10-unit corridors. |

Both place the generated geometry below a root named `RoboClash Arena`. Regeneration replaces only recognized generated arena roots after confirmation; unrelated scene objects are preserved. The gameplay scene is [RoboClashGameplay.unity](./Assets/Scenes/RoboClashGameplay.unity), and Build Settings already include it.

## 2. Design and navigation

### Compact arena

- **Footprint:** 36 x 36 Unity units, with a 1-unit-thick floor centered at y = -0.5.
- **Floor detail:** A shared, collider-free mesh adds restrained metal-panel seams and perimeter trim on top of the continuous floor. The combined detail mesh is cached as a generated asset; it does not add separate seam objects or affect robot collision.
- **Boundary:** Four 5-unit-tall, 0.8-unit-thick bulkheads, dark steel paneling, structural supports, and amber caution markings.
- **Center:** A raised 9 x 9 combat deck with four approach ramps, cyan edge markings, supports, and warning beacons.
- **Cover:** Eight low metal barriers and four industrial pylons. Barriers use solid box colliders; stripes and light details are decorative.
- **Hazards:** Two lava trenches, a moving bridge, and four spike traps.
- **Spawns:** Four numbered corner pads at (+/-12.5, 0, +/-12.5), with colored plates and beacons.
- **Pickups:** Four weapon pickups, two shield pickups, and a central rechargeable energy-shield volume.
- **Lighting:** Cool directional key light, cyan/blue arena floods, amber and lava accents, and a blue reactor glow.

The compact layout is suitable for close-range combat. The raised deck and ramps create a central engagement point while the barriers and hazards create short route choices.

### Large arena

The large-layout settings are declared by `RoboClashArenaSettings` in the generator:

| Setting | Default | Constraint |
|---|---:|---|
| Map width / depth | 120 / 120 | Minimum 120 |
| Corridor width | 10 | Clamped 6–14 |
| Central hub width / depth | 36 / 36 | Minimum 24; maximum constrained by map size |
| Wall height | 7 | Minimum 4 |
| Wall thickness | 1 | Clamped to 0.4 minimum and 5% of the smaller map dimension maximum |
| Side rooms, cover, hazards, raised area, pickups | Enabled | Independently toggleable |

Its top-down structure is:

```text
                     NORTH: ARMORY
                           |
       +---------------------------------------+
       |             perimeter loop            |
       |    +---------------------------+      |
       |    |        CENTRAL HUB        |      |
WEST:  |----|  north/east/south/west    |------| EAST:
POWER  |    |  connected approaches    |      | HAZARD
CORE   |    +---------------------------+      | CHAMBER
       |             perimeter loop            |
       +---------------------------------------+
                           |
                    SOUTH: WORKSHOP
```

- One continuous floor supports the entire map.
- The central combat hub uses split bulkhead segments to leave four openings.
- A corridor loop encircles the hub; four colored radial approaches connect the hub to that loop, and room connectors extend the routes to the cardinal side rooms.
- The side rooms are **Armory** (north), **Hazard Chamber** (east), **Workshop** (south), and **Power Core** (west). Each uses a themed floor inlay, steel walling, a doorway, and an accent color.
- Four safe corner spawn zones are placed near the outer map edges. Their platforms and beacons are visually distinct and face inward.
- Cover, hazard strips, pickups, and the low workshop catwalk with ramps can be toggled in the generator window.
- A cyan-lit reactor feature marks the central hub; each room receives a colored flood light.
- The visual-detail selector controls decorative mesh density without changing map geometry or gameplay placements.

The large layout does not add a NavMesh requirement. AI navigation remains owned by the existing gameplay scripts.

### Large-arena accessibility redesign

The existing **Generate Arena** and **Generate Large Arena** commands remain in place; the compact arena and the existing match manager/HUD integration are unchanged. The large layout now uses one shared internal dimension model for the central hub, ring, room centers, room back edges, connector floors, and spawn insets. Room depth and width scale independently with map width and depth, with bounded minimums and maximums; the hub limits are recalculated from those room dimensions and corridor width before generation. This keeps rectangular maps and changed corridor/hub settings aligned instead of relying on a fixed 30-unit room-edge offset.

- **Armory:** weapon pickups sit on the accessible room floor, with paired ammunition crates providing cover while leaving the center and doorway clear.
- **Hazard Chamber:** a clearly marked molten channel crosses the room. Its four lava damage segments stop short of three solid, 3.8-unit-wide bridges; shallow access ramps join each bridge to the floor. The trigger segments do not extend under the bridge/ramp footprints. A safe route runs around the channel ends, while the direct bridge route and two marked spike strips make riskier approaches. The continuous arena floor remains beneath the channel, so lava is a visibly marked damage surface rather than a physically open hole.
- **Workshop:** tool crates are moved off the catwalk approaches. The raised catwalk has two solid ramps and side rails; the pickup is on flat ground beside the ramps rather than floating over the raised route.
- **Power Core:** a collidable reactor housing, visible cyan core, crown, and four pillars form a landmark with walk-around space. The shield pickup is on clear floor near the entrance; its reusable energy-shield generator remains farther into the room. The reactor is still generated when the optional cover setting is disabled.
- **Hub and spawns:** the central reactor landmark remains in the hub. The four indexed spawn zones remain at the map corners, face inward, and use an inset that accounts for wall thickness.
- Hazard generation is still controlled by the existing setting. Turning it off omits the large-arena hazard visuals, triggers, warning plates, and bridges together.

The Editor-only **Tools > RoboClash > Validate Large Arena** command checks the generated root, four named rooms and their doorway headers/floor markings, room and corridor floor bounds against the continuous floor, spawn-zone count/indices/bounds, spawn and pickup overlap with hazards, lava trigger/visible-surface horizontal alignment, the hazard-room nonlethal floor-area ratio, and solid, adequately wide bridges/ramps that do not overlap the lava trigger bounds. These are geometry/bounds checks only; they are not a pathfinding or collision simulation and do not prove every route is traversable. The existing **Validate Scene Setup** tool also recognizes explicitly surfaced molten-channel segments so their intentional floor damage triggers are not reported as generic hidden open-pit hazards.

### Stylized geometry and surface upgrade

The existing generator has a visual-detail control (**Performance**, **Balanced**, or **High Detail**). It changes the number of decorative armor modules and floor seams only; it does not affect wall colliders, room dimensions, hazards, spawn points, pickups, or match setup. The compact arena keeps a Balanced default.

- `CreateWall` keeps its original solid primitive collider and `ArenaWall`, then adds a separate collider-free armor child. Its modular panels, alternating gunmetal plates, raised trim, cyan inset line, vent grooves, and structural ribs are baked into a combined four-submesh mesh. The mesh has explicit face normals and tiled UVs, and is cached/reused by wall dimensions and detail quality.
- The compact floor and configurable large floor both use a shared combined seam-mesh builder. The compact floor keeps its original continuous `ArenaFloor` collider and existing center plate/guide stripes; detail seams are collider-free and stop short of the edge trim to avoid overlapping coplanar seam geometry.
- Each wall and rail still has one gameplay wall collider; decorative panels, seams, doorframe trim, signs, pipes, and lights do not add colliders. Existing `MetalBarrier` colliders remain on cover, machinery, pillars, and bridges.
- Large-arena floors receive one collider-free combined mesh of subtle plate seams and perimeter trim, rather than a separate GameObject or collider for every seam.
- The Armory gains detailed weapon lockers, blue indicator bars, and armored crates; the Workshop gains press machines, hydraulic details, machinery panels, and catwalk supports; the Power Core receives layered rings, cyan conduits, a purple crown, and armored pillars; room entrances receive thick frame columns and accent strips.
- When an arena is generated, the generator creates and reuses deterministic 256 x 256 RGBA metal-panel albedo, tangent-space normal, and metallic/smoothness textures, plus a diagonal hazard-stripe texture and flowing lava albedo/emission texture. These are persisted under `Assets/Generated/RoboClash/Materials`; panel maps are wired to the floor, wall, trim, gunmetal, and stripe materials. The texture detail includes panel joints, fastener marks, surface variation, and scratches. `RoboClashArenaVisuals` configures shared URP/Lit materials, with a Standard shader fallback, and updates existing generated materials instead of creating a new asset on each regeneration.
- Lava uses a generated repeating orange-red texture and emissive map. `RoboClashLavaVisual` animates texture coordinates and subtly pulses emission through a reused `MaterialPropertyBlock`; it does not create per-frame materials. The existing `LavaPit` damage triggers and safe bridge/ramp layout remain unchanged. Hazard surfaces use a generated yellow-black stripe texture, with raised dark-steel borders around large-arena lava channels.
- Ceiling conduits in the themed rooms use cylinder geometry, room floor inlays use textured steel with room-color identity stripes, and existing room signs, accent lighting, reactor details, and machinery retain their separate color identities.
- The project contains imported textures in the Robot & Pilot asset package, but those maps are associated with its supplied models/materials and were not repurposed as arena tiling textures. No external textures were downloaded: public ambientCG access could not be verified from this environment because its DNS/revocation checks were unavailable.
- Generated wall-detail meshes are persisted under `Assets/Generated/RoboClash/Meshes` using stable dimension/quality keys, and existing matching assets are reused during regeneration. The detail design uses combined meshes, shared materials, no decorative colliders, and no added real-time lights.

## 3. Generator and asset pipeline

The complete Editor generator is [RoboClashArenaGenerator.cs](./Assets/Editor/RoboClashArenaGenerator.cs). It is an Editor script, so UnityEditor dependencies do not leak into the runtime assemblies.

The main entry points are `GenerateArena()` and `GenerateLargeArena(settings)`. Their build sequences are summarized below:

```csharp
// Compact layout
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
```

The large layout follows the same approach but builds a continuous floor, perimeter, central hub, loop and connectors, optional themed rooms, cover, hazards, catwalk, corner spawns, pickups, lights, training targets, and player setup.

### Geometry and materials

- Box, sphere, cylinder, and modular wall helpers centralize GameObject creation and optional colliders. Wall visuals use combined procedural meshes separate from collision geometry.
- Standard Unity primitive/box geometry is used for most structure. Spike cones are a small procedural 12-sided mesh, saved as an asset. The generated wall armor and floor seam meshes have explicit normals, triangles, bounds, and UVs.
- The generator reuses or creates assets under `Assets/Generated/RoboClash/Materials` and `Assets/Generated/RoboClash/Meshes`.
- Materials are created/reused with URP/Lit, with Standard shader fallback. The shared visual helper supports albedo, normal, metallic/smoothness, emission maps, texture tiling, and material caching. Steel, floor, wall, trim, and warning materials use generated PBR maps; lava uses a generated emissive texture. Accent/neon materials retain their color and emission treatments.
- Existing `Mech.prefab` is used for training target/player display visuals if present. If it is absent, the generator builds a simple procedural robot silhouette instead. Gameplay robot prefabs remain the match manager's responsibility.

### Safe regeneration

The generator locates roots named `RoboClash Arena` and the legacy `RoboClash_Generated_Arena`, requests confirmation before replacing them, and uses Unity Undo for generation. It saves shared material/mesh assets and marks the active scene dirty. Generation does not silently save over the gameplay scene: inspect and save the scene in the Editor after running a command.

## 4. Runtime arena code

The arena code lives under [Assets/Scripts/RoboClash](./Assets/Scripts/RoboClash). Reusable structure components are intentionally small; the generator composes them into a complete map.

### Structure and spawn components

| Component | Code file | Role |
|---|---|---|
| `ArenaFloor` | [ArenaFloor.cs](./Assets/Scripts/RoboClash/ArenaFloor.cs) | Requires a `BoxCollider`; in the Inspector's Reset path, makes it a solid collider. |
| `ArenaWall` | [ArenaWall.cs](./Assets/Scripts/RoboClash/ArenaWall.cs) | Requires a solid box collider for boundary and wall geometry. |
| `MetalBarrier` | [MetalBarrier.cs](./Assets/Scripts/RoboClash/MetalBarrier.cs) | Solid box collider for cover and metal structures. |
| `ArenaDecoration` | [ArenaDecoration.cs](./Assets/Scripts/RoboClash/ArenaDecoration.cs) | Marks decorative objects; it does not implement gameplay behavior. |
| `ArenaSpawnZone` | [ArenaSpawnZone.cs](./Assets/Scripts/RoboClash/ArenaSpawnZone.cs) | Stores a non-negative spawn index used by match spawning. |
| `ArenaMovingPlatform` | [ArenaMovingPlatform.cs](./Assets/Scripts/RoboClash/ArenaMovingPlatform.cs) | Moves a platform back and forth along a configured offset and exposes its velocity for moving-platform movement support. |
| `RoboClashArenaVisuals` | [RoboClashArenaVisuals.cs](./Assets/Scripts/RoboClash/Visuals/RoboClashArenaVisuals.cs) | Creates/caches shared materials and configures supported URP/Standard PBR texture maps. |
| `RoboClashLavaVisual` | [RoboClashLavaVisual.cs](./Assets/Scripts/RoboClash/Visuals/RoboClashLavaVisual.cs) | Animates lava texture coordinates and emission using one reusable material property block. |

The platform motion uses a ping-pong phase and physics-step motion for a kinematic Rigidbody:

```csharp
var phase = Mathf.PingPong(Time.time / cycleDuration, 1f);
var nextPosition = startPosition + localOffset * phase;
Velocity = (nextPosition - transform.position) / Time.fixedDeltaTime;
body.MovePosition(nextPosition);
```

The compact map configures its bridge with a 2.4-unit z offset and a 4-second cycle.

### Hazards

| Component | Code file | Behavior |
|---|---|---|
| `ArenaHazard` | [ArenaHazard.cs](./Assets/Scripts/RoboClash/ArenaHazard.cs) | Shared base; configures a trigger and kinematic, gravity-free Rigidbody. Its default damage rate is 35 per second. |
| `LavaPit` | [LavaPit.cs](./Assets/Scripts/RoboClash/LavaPit.cs) | Applies continuous damage while a damageable stays inside its trigger. |
| `SpikeTrap` | [SpikeTrap.cs](./Assets/Scripts/RoboClash/SpikeTrap.cs) | Applies one damage event on trigger entry. |
| `ArenaDamageable` | [ArenaDamageable.cs](./Assets/Scripts/RoboClash/ArenaDamageable.cs) | Existing health/shield damage receiver shared by robots and other damageable participants. |

Both compact and large layouts use visual hazard markings alongside actual trigger volumes. Visual cones/plates alone do not cause damage; the trigger GameObject and hazard component implement the gameplay interaction.

### Pickups and energy shield

| Component | Code file | Behavior |
|---|---|---|
| `ArenaWeaponPickup` | [ArenaWeaponPickup.cs](./Assets/Scripts/RoboClash/ArenaWeaponPickup.cs) | Trigger grants the existing `RobotArenaController` a weapon configuration (default 40 damage, 32 projectile speed, 0.22-second fire interval), hides the pickup, and respawns after 12 seconds by default. |
| `ArenaShieldPickup` | [ArenaShieldPickup.cs](./Assets/Scripts/RoboClash/ArenaShieldPickup.cs) | Trigger restores up to 60 shield by default, hides the pickup, and respawns after 15 seconds by default. |
| `ArenaEnergyShield` | [ArenaEnergyShield.cs](./Assets/Scripts/RoboClash/ArenaEnergyShield.cs) | Trigger volume with 150 default energy and 8 energy/second recharge. `Absorb(damage)` subtracts energy and reports whether it absorbed the hit. |

The pickups rotate visually; weapon pickups also bob vertically. Trigger interactions ignore non-player damageables where the implementation specifically requires a `RobotArenaController`.

### King of the Arena zone

[KingOfArenaZone.cs](./Assets/Scripts/RoboClash/KingOfArenaZone.cs) tracks occupants by `ArenaDamageable` and a set of their colliders. Tracking colliders individually prevents a participant from leaving the zone when one collider exits while another collider is still inside. The existing [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) creates and scores its central zone for King of the Arena matches; the zone is a mode feature, not a second arena-wide manager.

## 5. Gameplay integration

The generator attaches one `RoboClashMatchManager` and one `RoboClashHUD` to the generated root. It loads the configured VOLT, TITAN, SHADOW, and SPARK prefabs and supplies them to that manager. It checks manager configuration and logs a warning if required prefab setup is incomplete.

On match initialization, the manager:

1. Reads a pending selection from [RoboClashSession.cs](./Assets/Scripts/RoboClash/UI/RoboClashSession.cs), if the menu supplied one.
2. Collects the existing `ArenaSpawnZone` components and uses their spawn indices/positions for participants.
3. Instantiates the selected player robot and AI opponents from the configured robot prefabs.
4. Applies the existing match mode, timer, respawn, target, and survival settings.
5. Handles deaths, respawns, mode scoring, the King zone, HUD state, and results.

The arena generator itself does not implement combat, robot movement, player abilities, AI decision-making, match rules, pause, or result recording; those remain in their existing runtime systems.

## 6. Source map for arena-related code

### Editor / generation

- [RoboClashArenaGenerator.cs](./Assets/Editor/RoboClashArenaGenerator.cs) — menu commands, options, assets, map assembly, and generated gameplay setup.

### Geometry / world interaction

- [ArenaFloor.cs](./Assets/Scripts/RoboClash/ArenaFloor.cs)
- [ArenaWall.cs](./Assets/Scripts/RoboClash/ArenaWall.cs)
- [MetalBarrier.cs](./Assets/Scripts/RoboClash/MetalBarrier.cs)
- [ArenaDecoration.cs](./Assets/Scripts/RoboClash/ArenaDecoration.cs)
- [ArenaSpawnZone.cs](./Assets/Scripts/RoboClash/ArenaSpawnZone.cs)
- [ArenaMovingPlatform.cs](./Assets/Scripts/RoboClash/ArenaMovingPlatform.cs)
- [ArenaHazard.cs](./Assets/Scripts/RoboClash/ArenaHazard.cs)
- [LavaPit.cs](./Assets/Scripts/RoboClash/LavaPit.cs)
- [SpikeTrap.cs](./Assets/Scripts/RoboClash/SpikeTrap.cs)
- [ArenaWeaponPickup.cs](./Assets/Scripts/RoboClash/ArenaWeaponPickup.cs)
- [ArenaShieldPickup.cs](./Assets/Scripts/RoboClash/ArenaShieldPickup.cs)
- [ArenaEnergyShield.cs](./Assets/Scripts/RoboClash/ArenaEnergyShield.cs)
- [KingOfArenaZone.cs](./Assets/Scripts/RoboClash/KingOfArenaZone.cs)
- [ArenaProjectile.cs](./Assets/Scripts/RoboClash/ArenaProjectile.cs) — collision, damage, and energy-shield interaction for projectile weapons.

### Match and robot integration

- [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) — spawns participants, applies mode settings, controls scoring and match lifecycle.
- [RoboClashSession.cs](./Assets/Scripts/RoboClash/UI/RoboClashSession.cs) — pending selected robot/match configuration handoff.
- [RoboClashHUD.cs](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) — existing gameplay HUD and mode status.
- [RobotArenaController.cs](./Assets/Scripts/RoboClash/RobotArenaController.cs) — existing player control and pickup weapon integration.
- [ArenaBotController.cs](./Assets/Scripts/RoboClash/ArenaBotController.cs) — simple generated training-bot behavior.
- [ArenaDamageable.cs](./Assets/Scripts/RoboClash/ArenaDamageable.cs) — existing damage, health, and shield handling.
- [ArenaProjectile.cs](./Assets/Scripts/RoboClash/ArenaProjectile.cs) — resolves projectile hits, damage, and energy-shield absorption.

## 7. Generate or inspect the arena

1. Open the RoboClash project in Unity 6.6 and wait for script compilation to finish.
2. Open `Assets/Scenes/RoboClashGameplay.unity`.
3. Select **Tools > RoboClash > Generate Arena** for the compact combat arena, or **Tools > RoboClash > Generate Large Arena** to review/adjust the large-map settings.
4. If replacing an existing generated root, confirm the prompt. In the large-arena settings window, choose a visual-detail level; this affects decoration density only. Review the generated `RoboClash Arena` hierarchy and scene lights.
5. Save the scene. Run **Tools > RoboClash > Validate Large Arena** for room/spawn/hazard geometry checks and **Tools > RoboClash > Validate Scene Setup** for gameplay configuration checks.
6. Enter Play Mode and verify routes, all four spawns, collisions, moving bridge, spike/lava damage, pickup and shield effects, AI, and match starts/results.
7. Regenerate once and confirm the old generated root is replaced without removing unrelated camera, lights, UI, or scene objects.

To change the design in code, edit layout methods in `RoboClashArenaGenerator.cs`; change reusable runtime behavior in the corresponding component script. Avoid putting runtime gameplay behavior in the Editor generator, and avoid duplicating mode or spawn logic already provided by `RoboClashMatchManager`.

## 8. Validation status and known limits

- The large-arena redesign and Editor validator are implemented in the existing Unity 6.6 generator; `RoboClashValidationTool.cs` was updated to recognize the new intentional lava-channel surface.
- The arena visual upgrade is implemented in the existing generator; the runtime `ArenaWall`, `MetalBarrier`, hazard, pickup, and match components were not changed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore` succeeded after the visual changes: **0 warnings, 0 errors**.
- The Unity Scene view, generated scene hierarchy, validator menu command, and Play Mode were not run in this environment. Material appearance under the active URP renderer, visual joins/seams, wall/frame collision, bridge/ramp traversal, hazard damage, pickup behavior, AI route choices, and match lifecycle still need Unity Editor/Play Mode validation.
- Visual detail is a lightweight stylized combination of a generated albedo tile, reused combined wall/floor meshes, shared URP materials, and decorative geometry. The generator does not create normal maps, baked lighting, or NavMesh assets.
