# RoboClash Robots: Design, Animation, and Abilities

## Scope and source map

This report describes the four playable robot types currently defined in the Unity project: Volt, Titan, Shadow, and Spark. It focuses on their runtime-generated models, movement/animation, weapons, passive behavior, abilities, ultimates, and the code that implements them.

The models are assembled procedurally at runtime; their key shapes are created from primitive meshes and a shared beveled armor mesh. Their locomotion and ability poses are also driven by C# transforms rather than character animation clips. Principal implementation files:

- [RoboClashRobotDefinition.cs](./Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs) — robot types, stats, colors, names, durations, and dimensions.
- [RoboClashRobotFactory.cs](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs) — generated model hierarchy, armor, weapons, robot-specific parts, and auxiliary visuals.
- [RoboClashRobotAnimator.cs](./Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs) — procedural walk, idle, recoil, hit, landing, spawn, ability-lean, and death motions.
- [RoboClashRobotAbilities.cs](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) — activation, cooldowns, charge, buffs, healing, teleport/strike, and EMP logic.
- [RoboClashRobotVFX.cs](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs), [RoboClashCombatVFX.cs](./Assets/Scripts/RoboClash/RoboClashCombatVFX.cs), and [RoboClashPulseRing.cs](./Assets/Scripts/RoboClash/RoboClashPulseRing.cs) — persistent ability visuals and short-lived particles/rings.
- [RoboClashRobotCombat.cs](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) — weapon stats, projectile setup, firing recoil, reload, and Volt's movement-linked reload bonus.
- [RoboClashRobotMovement.cs](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) — dash/teleport movement and body/camera control.
- [RoboClashMenuController.cs](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) — player-facing role and ability summary text.

## Shared design and construction

`RoboClashRobotFactory.CreateRobot` creates a root object and attaches the shared gameplay components, then constructs a `ModelRoot` and assigns its visual pivots to movement, animation, combat, and VFX systems. A robot is therefore a common gameplay rig with type-specific proportions, materials, add-ons, and weapon geometry.

The base silhouette is built from:

- A torso with upper/lower chest plates, chevrons, cooling vents, and an emissive chest reactor.
- A separate `AimPivot` with angular helmet, brow, visor housing, emissive optic, and sensors.
- Two articulated arms with shoulder/elbow/wrist joints, forearm armor, hand, fingers, and hydraulic rods.
- Hip chassis, articulated legs, knee guards, shin armor, feet, and hydraulic details.
- A weapon mounted to the aim pivot, so the weapon follows aim pitch.

`CreateArmorMesh` makes a beveled eight-sided box mesh with chamfered corners and inset front/back rings. Armor panels use this mesh when available; primitive cubes are the fallback. Decorative primitive colliders are removed, leaving the root `CharacterController` for movement.

Representative shared model construction:

```csharp
var torso = Child("Torso", model, new Vector3(0f, 0.95f * scale, 0f));
ArmorPiece(armorMesh, "Chest Armor Shell", torso, Vector3.zero,
    new Vector3(torsoWidth, 0.8f, torsoDepth), primary);
Piece(PrimitiveType.Sphere, "Chest Reactor Housing", torso,
    new Vector3(0f, 0.02f, torsoDepth * 0.57f),
    new Vector3(coreSize * 1.65f, coreSize * 1.35f, 0.1f), joints);
Piece(PrimitiveType.Sphere, "Chest Energy Core", torso,
    new Vector3(0f, 0.02f, torsoDepth * 0.68f),
    new Vector3(coreSize, coreSize, 0.075f), emission);
```

Source: [BuildSilhouette and shared armor construction](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L61), [armor mesh generation](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L412), [component/model wiring](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L13).

## Roster at a glance

| Robot | Role / visual identity | Core stats (health / shield / speed) | Weapon | Ability | Ultimate |
|---|---|---:|---|---|---|
| **Volt** | Balanced fighter; compact blue/white armor with cyan power conduits | 100 / 50 / 7.0 m/s | Pulse Rifle | Dash — 5 m; 12 s cooldown | Overdrive — 8 s; 45 s cooldown |
| **Titan** | Heavy tank; broad red/charcoal frame and asymmetric shoulder bulwarks | 150 / 80 / 4.2 m/s | Gatling Cannon | Defensive Field — 4 s; 18 s cooldown | Rail Cannon — 90 damage; 60 s cooldown |
| **Shadow** | Fast stealth fighter; narrow purple/near-black silhouette and rear emitters | 85 / 60 / 8.0 m/s | Energy SMG | Cloak — up to 5 s; 16 s cooldown | Shadow Strike — 70 damage; 50 s cooldown |
| **Spark** | Support; yellow/charcoal armor, utility backpack, shoulder pods, hovering drone | 90 / 70 / 5.8 m/s | Rocket Launcher | Repair Drone — 5 s; 20 s cooldown | EMP Blast — 8 m radius; 55 s cooldown |

Stats, names, colors, and cooldowns come from `Configure` in [RoboClashRobotDefinition.cs](./Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs#L75). The roster order and role blurbs are in [RoboClashMenuController.cs](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs#L141). All ultimates also require 100 charge by default; charge is awarded from damage dealt (implementation details below).

The configured ultimate active-duration field is 8 s for Volt, 0 s for Titan and Shadow, and 3 s for Spark. For Titan and Shadow, zero matches their one-shot attacks. Spark's EMP is also an instantaneous activation in the code even though its definition/UI state tracks a 3 s ultimate duration; the actual 3 s gameplay effect is the enemy disruption, not a three-second EMP attack animation.

### Complete combat profiles

The selection screen shows a compact subset of each robot's statistics. This table lists the complete values set by `Configure`, including weapon behavior. Damage and rate are per projectile and shots per second; range is the projectile's configured weapon range.

| Robot | Health | Shield | Move speed | Damage / shot | Shots / second | Magazine | Reload | Range | Incoming damage multiplier | Knockback resistance | Projectile speed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Volt | 100 | 50 | 7.0 | 18 | 4 | 20 | 1.5 s | 20 m | 1.00 | 0 | 44 |
| Titan | 150 | 80 | 4.2 | 9 | 7 | 42 | 2.4 s | 17 m | 0.85 | 0.75 | 58 |
| Shadow | 85 | 60 | 8.0 | 9 | 10 | 30 | 1.3 s | 10 m | 1.00 | 0 | 48 |
| Spark | 90 | 70 | 5.8 | 45 | 0.9 | 4 | 2.5 s | 23 m | 1.00 | 0 | 30 |

These are configured gameplay values, not an overall quality ranking. They express distinct tradeoffs: Titan absorbs more damage and has the largest magazine but moves slowly; Shadow is quickest and fires frequently but has low health and short range; Spark fires slow, high-damage rockets with four rounds; Volt sits between those extremes. Titan and Spark have a 3.5 m weapon splash radius in their definitions; Volt and Shadow do not. Titan's 0.85 incoming-damage multiplier means 15% passive damage reduction, while its 0.75 knockback resistance scales down incoming impulse.

## Per-robot visual design and weapons

### Volt

- **Palette:** primary electric blue `#147DFF`, pale secondary armor `#EAF4FF`, dark blue-gray joints, cyan emission `#00D9FF`.
- **Proportions:** default compact frame, approximately `1.1 × 2.0 × 0.8 m`.
- **Distinctive parts:** a rear power-unit housing, two vertical power cells, cyan indicators, and exposed glowing conduits.
- **Weapon:** pulse rifle with receiver, narrow barrel, muzzle shroud, energy cell, side rails, and a cyan core. Uses standard rapid projectile and muzzle effects.

### Titan

- **Palette:** red `#D71932`, charcoal secondary armor, nearly black joints, red-orange emission `#FF3B27`.
- **Proportions:** largest frame, approximately `1.8 × 2.5 × 1.4 m`; torso and reactor are wider/larger than the other types.
- **Distinctive parts:** asymmetric shoulder armor—a large bulwark on one side and a smaller cannon-side guard on the other.
- **Weapon:** arm-mounted gatling cannon with a four-barrel rotating assembly, red barrel cores, rotary motors, and a large cannon housing. Barrels spin while firing.

### Shadow

- **Palette:** purple `#542080` over near-black armor and joints, magenta emission `#E044FF`.
- **Proportions:** smallest/narrowest target, approximately `0.9 × 1.8 × 0.7 m`.
- **Distinctive parts:** paired rear stealth emitters and thin energy conduits; the narrow silhouette contrasts with Titan.
- **Weapon:** compact energy SMG with a short, narrow barrel and energy cell. The cloak visual is a hidden translucent ellipsoid/spherical shell sized around the body.

### Spark

- **Palette:** yellow `#FFC928`, charcoal secondary armor/joints, amber emission `#FFB300`.
- **Proportions:** approximately `1.1 × 1.9 × 0.9 m`.
- **Distinctive parts:** rounded utility shoulders and a backpack with twin repair canisters, hatch, status lamp, and visible cables. A small drone hovers beside the robot.
- **Weapon:** shoulder rocket launcher with a wide rocket tube and magazine; rocket projectiles have a larger, longer trail than standard shots.

Robot-specific model pieces are selected in [RoboClashRobotFactory.cs](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L136), with weapons at [BuildWeapon](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L275), and Volt/Spark/Shadow equipment at [power unit, backpack, emitters, cloak shell, and drone](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L255).

### What the generated shapes look like in detail

The visual language is layered hard-surface robot armor. Each armor shell is a beveled box, and the factory combines that shell with simpler primitive parts: spheres for reactor housings and joints, cylinders for actuators/rods/barrels, and cubes or armor mesh for plates. Secondary panels, vents, cable rods, and luminous insets sit on top of the larger armor masses. The model has no fingers or articulated hands as separately animated joints: the factory builds a compact hand and three decorative finger actuators onto each arm pivot.

The shared torso is centered at `0.95 * (robot height / 2)` above the model origin. Titan receives the broader chest (`0.95 × 0.8 × 0.66`) and a larger reactor core (`0.27` base size). Other chassis use a `0.62 × 0.8 × 0.48` torso and `0.18` base reactor size. A separate head/weapon pivot sits at `1.65 * (height / 2)`. The visible chest core is a shallow emissive sphere in front of a darker, wider reactor housing, not a light source.

Arms are assemblies of shoulder actuator, shoulder armor, upper-arm cylinder, elbow sphere, forearm plate, wrist cylinder, hand, finger actuators, and one exposed hydraulic rod. Titan uses thicker shoulders and longer arm sections; the three other types share the lighter dimensions. Legs are similarly built as thigh shell/front plate, knee joint/guard, shin, ankle, articulated foot and toe, plus a rear hydraulic rod. `leftLeg` and `rightLeg` are the transform pivots moved by the gait.

Type-specific additions alter the outline as well as color:

- **Volt:** a rectangular power-unit case is mounted behind the torso. Two vertical cylindrical cells flank it; small cyan indicator spheres cap the cells and bright conduits trace from the rear unit toward the chassis. The visual reads as a standard athletic frame carrying a compact power pack.
- **Titan:** the shoulder silhouette is deliberately asymmetric. One shoulder is a large `1.9 × shoulderWidth` bulwark and the other a smaller `1.35 × shoulderWidth` guard. The chest and core are also wider; the Gatling cannon and four parallel barrels add a second large mass on the weapon side. That shape supports the tank identity before any shield effect is active.
- **Shadow:** the frame is scaled to the narrowest dimensions. Its two rear emitters are small luminous spheres, connected down the back by thin magenta rods. With the near-black armor and thin SMG, the bright rear emitters/visor do most of the silhouette work.
- **Spark:** rounded shoulder spheres contrast with the faceted armor used on the other types. The backpack includes two vertical repair canisters, a hatch, status lamp, and cables. Its companion drone is a small rounded core, yellow shell, one front status lamp, and two horizontal hover-rotor cylinders. The drone is a separate transform rather than a skinned part of the robot.

For the detailed hierarchy and local placement values, see [shared silhouette construction](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L61), [arms and legs](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L176), and [robot-specific attachments](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L255).

## Animation system

There are no per-character animation clips in the robot runtime animator. `RoboClashRobotAnimator.LateUpdate` computes the pose from movement and state:

- **Walk/run:** a sine-wave gait counter-rotates the leg pivots; arms counter-swing. Frequency and stride scale with speed.
- **Body motion:** torso bob, breathing, forward acceleration lean, lateral lean, and airborne lean.
- **Landing and hits:** landing pulse and decaying hit-reaction roll/lean.
- **Weapon recoil:** firing nudges the weapon mount backward and applies an arm/body recoil pose; muzzle flash and a short point light are separate VFX.
- **Ability pose:** abilities set an ability pose target. It blends in/out smoothly and applies a forward torso lean, not a bespoke multi-step animation for each ability.
- **Spawn/respawn:** a 1.5 s intro or 0.65 s respawn pose lowers/leans the body and fades the pose weight.
- **Death:** the visual root compresses to 4% scale over 0.45 s.

Movement personality multipliers make the same gait feel different: Volt has a slightly faster cadence; Titan has slower frequency and stride with stronger recoil/landing response; Shadow has quicker, longer strides and more lean; Spark has reduced body lean, bob, and stride.

Representative scripted gait and ability lean:

```csharp
var frequency = walkFrequency * frequencyMultiplier *
                Mathf.Lerp(0.55f, 1.25f, speedFraction);
gaitPhase += deltaTime * frequency;
var stride = Mathf.Sin(gaitPhase * Mathf.PI * 2f) * walkAngle *
             speedFraction * strideMultiplier;
...
torso.localRotation = torsoRotation * Quaternion.Euler(
    -speedFraction * 3.5f * bodyLeanMultiplier -
    accelerationLean * 2.5f * bodyLeanMultiplier -
    abilityLean * 10f + landingPulse * 4f + airborneLean +
    hitReaction * 5f - spawnPoseWeight * 5f,
    0f,
    ...);
```

Source: [procedural pose update](./Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs#L172), [robot-specific animation tuning](./Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs#L254). Weapon firing/recoil and Titan barrel spin are in [RoboClashRobotCombat.cs](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs#L130) and [the combat update](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs#L221).

### Animation design, timing, and what is (and is not) animated

The animation is a compact procedural rig, not a clip-based animation state machine. `RoboClashRobotFactory` supplies only the pivots that the scripts animate: two leg pivots, two shoulder/arm pivots, torso, and aim/head pivot. The script stores their starting rotations/positions and recomputes offsets relative to those cached poses every LateUpdate. As a result, there is no authored foot planting, knee bending, hand posing, facial animation, weapon reload animation, or per-ability limb choreography in this system.

At rest, the torso gets a very small breathing offset (`sin(time * 1.7) * 0.006 m`). While moving, a 7 Hz base gait is scaled by the robot personality and by speed fraction (`0.55` to `1.25` of the base rate). The legs swing in opposite directions by up to a base 24 degrees; the arms swing in smaller opposing amounts. The torso adds a small double-frequency bob (`0.014 m` before personality/speed scale), leans forward with speed/acceleration, and rolls with lateral velocity. These numbers create readable motion without changing the rigid armor pieces individually.

Action responses are layered on top of locomotion:

- **Firing:** the weapon mount moves back by up to `0.06 m`; recoil eases toward zero at 8 units/s. The arm and torso also receive a short recoil offset, with Titan's recoil multiplier highest. Titan's rotating barrels spin at `850°/s` only while `firing` is true.
- **Being hit:** an intensity value decays at 5 units/s and drives a fast body roll (`sin(time * 18)`) plus torso/arm/head offsets. This is a transform jolt, not a hit-stun animation clip.
- **Landing:** a pulse decays at 3.5 units/s; it lowers the torso slightly and adds a small counter-rotation.
- **Ability pose:** the target blends over approximately `0.12 s`, then applies as much as 10 degrees of forward torso lean. The caller chooses how long to hold that pose. It is intentionally generic across robot abilities.
- **Spawn/respawn:** spawn weight eases down with a smoothstep over the supplied duration (normally 1.5 s on spawn, 0.65 s on respawn), lowering/tilting the torso and lifting the arms slightly. The camera has a matching transition, managed by movement.
- **Death:** a quadratic collapse scales the whole model from full size to 4% over 0.45 s.

Robot personality tuning does not create new poses; it adjusts the same formula. Volt cadence is `1.08×`, stride `0.95×`; Titan cadence `0.78×`, stride `0.76×`, body lean `0.72×`, recoil and landing `1.35×`; Shadow cadence `1.2×`, stride `1.05×`, body lean `1.12×`; Spark cadence `0.92×`, stride `0.82×`, body lean `0.52×`, bob `0.6×`. Thus Titan's march is slow and heavy, Shadow's gait quick and pronounced, Spark's is restrained, and Volt's is slightly brisker than the baseline.

Aim is handled separately: `AimPivot` is rotated for pitch, and the root rotates for yaw. Since the weapon is a child of `AimPivot`, it follows vertical aim; the running gait does not rotate the weapon to simulate recoil. Camera kick, FOV kick, and jitter are additional camera feedback, independent from the model pose.

Source details: [pose cache and animation state](./Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs#L20), [procedural update](./Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs#L172), [personality values](./Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs#L254), [camera feedback and dash FOV kick](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs#L207).

The factory adds `RoboClashRobotAnimator`, not a Unity `Animator` or a robot animation controller. The runtime-described robot animations in this report are therefore the scripted transform motions above. The project may contain unrelated imported animation-controller assets, but they are not attached by this generated-robot factory path.

## Ability and ultimate framework

The ability component reads each robot's name/duration/cooldown from its definition. A player uses Q or the phone controller's ability input; ultimate uses E or the phone ultimate input. AI can also call the same ability methods when its decision logic selects an ability. Activation is rejected while dead, controls are locked, or its cooldown has not elapsed. The ability cooldown starts on activation.

Ultimates additionally need the configured charge threshold (default `100`). Charge is consumed when the ultimate activates and its cooldown begins at that point. Projectile damage grants charge at `ultimateChargePerDamage`, configured as `0.5` per damage point in the robot stats; damage dealt by allied sources does not charge the owner's meter.

The ability system triggers a shared procedural torso pose and colored particle burst/pulse. It then invokes the type-specific effect. Some effects last for a set duration; others are instant attacks but still have a cooldown. No character-specific animation clip or cinematic sequence is authored for these abilities.

### Ability animation versus ability effect

It helps to separate three things which the game starts at nearly the same time:

1. **Body animation:** a short generic torso lean controlled by `RoboClashRobotAnimator`. A call to `TriggerAbilityPose(duration)` only blends the pose in and holds it; it does not select a different animation for Dash, Cloak, the Repair Drone, or any ultimate.
2. **Activation VFX/audio:** a colored particle burst (ability) or a short expanding ring plus burst (ultimate), paired with a type-specific sound event. These are one-shot cues; they are not the robot's sustained effect.
3. **Gameplay effect and sustained visual:** the ability system then starts its coroutine or instantaneous action. The sustained shield sphere, cloak shell, repair beam, dash displacement, rail shot, strike, or EMP ring is implemented outside the animator.

This means the existing source defines the robots' *animation design* as procedural body motion plus event-driven VFX. It does not contain bespoke authored animation tracks for a hand raising, shoulder cannon opening, energy charging across armor, or drone deploying from a hatch. A report should not imply those motions exist: the rail charge is a particle burst and a `0.7 s` delay, the Repair Drone stays visibly hovering (it is not launched from the backpack), and cloak turns renderers off in one state change.

## Abilities by robot

### Volt — Dash / Overdrive

**Dash (ability):** a 5 m movement burst along the player camera's forward direction (or the robot's forward direction for AI). The movement code sweeps/collides the dash path and returns its endpoint. Cyan dash bursts are emitted along the start-to-end path, and the ability pose lasts 0.35 s. The definition advertises 0.3 s active duration and 12 s cooldown; the actual displacement is a direct dash, not a sustained speed buff.

**Overdrive (ultimate):** lasts 8 s and grants `+30%` move speed and `+35%` fire rate. A transparent energy sphere is enabled and pulses slightly during Overdrive. Its configured cooldown is 45 s.

Code excerpts:

```csharp
if (!movement.TryDash(dashDirection, 5f, out var dashEnd))
    return;
visualEffects?.PlayDash(dashStart, dashEnd, dashDirection);
animator?.TriggerAbilityPose(0.35f);
```

```csharp
var overdrive = definition.robotType == RoboClashRobotType.Volt &&
                Time.time < overdriveEndTime ? 1.3f : 1f;
...
return definition.robotType == RoboClashRobotType.Volt &&
       Time.time < overdriveEndTime ? 1.35f : 1f;
```

Sources: [Volt ability and ultimate routing](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L141), [Overdrive effect and multipliers](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L297), [dash collision/movement](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs#L202), [energy field pulse](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs#L107).

**In play, the sequence is:** on activation, Volt chooses camera-forward on player control (robot-forward for AI), flattens that vector to the ground plane, and performs a 0.12 s displacement up to 5 m. A sphere-cast limits the dash at obstacles. The controller gets a temporary FOV kick, and three cyan heavy bursts mark the start, midpoint, and end of the traversed segment. Volt's model briefly leans through the generic ability pose. Overdrive instead starts with the shared ultimate ring/burst, enables the transparent cyan body field, and sustains its movement/fire-rate bonuses for 8 s. The field grows slightly and pulses; it is not a lightning bolt or a trail that follows each shot.

### Titan — Defensive Field / Rail Cannon

**Defensive Field (ability):** lasts 4 s, cooldown 18 s. Reduces incoming damage to 55% of normal (a 45% reduction). The visual is a large translucent red-orange sphere around the robot with a brief heavy particle burst at activation. The sphere is also reused by Volt's Overdrive visual.

**Rail Cannon (ultimate):** waits 0.7 s for a charge moment, plays a heavy burst at the weapon, then fires a high-speed projectile: speed `65`, damage `90`, splash radius from Titan's `damageRadius` (`3.5 m`), and knockback `12`. Cooldown 60 s. The ultimate duration is configured as 0 because it is an event/shot, not a timed buff. The shared ultimate start effect also creates a generic 7 m pulse around the robot, but the rail shot itself originates at the muzzle.

```csharp
private IEnumerator DefensiveField(float duration, float damageMultiplier)
{
    temporaryDamageMultiplier = Mathf.Clamp(damageMultiplier, 0.2f, 1f);
    visualEffects?.SetShieldField(true);
    yield return new WaitForSeconds(duration);
    temporaryDamageMultiplier = 1f;
    visualEffects?.SetShieldField(false);
}
```

```csharp
yield return new WaitForSeconds(0.7f);
if (!damageable.IsDead)
    FireRailCannon();
...
FireAbilityProjectile(forward, 65f, 90f, definition.damageRadius, 12f);
```

Sources: [Titan ability/ultimate activation](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L169), [defensive field and rail charge](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L279), [rail projectile parameters](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L336), [shield sphere construction](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs#L188).

**In play, the sequence is:** the red-orange activation burst flashes around Titan, the general ability lean blends in, and a translucent sphere appears around the wide chassis for 4 s. The field itself is static-size in Defensive Field; the reduction is applied in damage calculation, not shown as individual blocked projectile impacts. The Rail Cannon ultimate starts with the generic ultimate ring and charge sound, adds a heavy red burst at the cannon mount, waits 0.7 s, then sends one emissive projectile out of the muzzle. The projectile carries its own trail and impact burst/splash handling. There is no separate barrel-extension or long railgun charging animation.

### Shadow — Cloak / Shadow Strike

**Cloak (ability):** makes all model renderers invisible for up to 5 s, except the dedicated cloak-shell renderer, which is enabled instead. Firing immediately ends cloak. Cooldown 16 s. The shell is a translucent, body-sized ellipsoid using the shimmer material; it has no collider and casts/receives no shadows. This implementation hides the model rather than making it partially transparent.

**Shadow Strike (ultimate):** selects a preferred AI target if valid, otherwise the nearest living enemy. It attempts to teleport to a point 2 m behind that target, provided the sphere-cast path is clear; if no target or the path is blocked, it falls back to 5 m forward. On a successful teleport, it emits a dash effect and faces the target. It then fires a projectile for 70 damage at speed 48, with 14 knockback and no splash radius. Cooldown 50 s. The configured ultimate duration is 0 because the strike is instantaneous.

```csharp
private IEnumerator Cloak(float duration)
{
    SetCloaked(true);
    yield return new WaitForSeconds(duration);
    SetCloaked(false);
}
```

```csharp
var behindTarget = target.transform.position - target.transform.forward * 2f;
...
if (movement.TryTeleport(destination))
{
    transform.rotation = Quaternion.LookRotation(...);
    visualEffects?.PlayDash(previousPosition, transform.position, transform.forward);
}
FireAbilityProjectile(aim, 48f, 70f, 0f, 14f);
```

Sources: [cloak activation and strike routing](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L172), [cloak effect and renderer switching](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L289), [target selection, teleport, and strike shot](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L347), [cloak shell shape/material](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L395).

**In play, the sequence is:** a magenta activation burst and short torso lean accompany Cloak. The armor, weapon, visor, and other model renderers are disabled while a single translucent ellipsoid remains enabled as the silhouette. No fade animation is played. If Shadow fires, the cloak ends immediately; otherwise it turns off at 5 s. Shadow Strike uses the shared ultimate pulse at the starting position, selects a target, checks a path and safe destination, teleports, emits dash bursts at the previous/current positions, turns toward the target, and fires the projectile. This is a teleport-and-shot sequence; the current code does not animate a continuous run from Shadow to the target or a melee swing.

### Spark — Repair Drone / EMP Blast

**Repair Drone (ability):** lasts 5 s, cooldown 20 s. Heals Spark at 4 HP/s and adds 5 shield/s. A gold drone hovers at Spark's side, bobbing vertically with a sine wave and rotating at 45 degrees/s. During the ability a narrow beam connects the drone to Spark, in amber fading to white. The drone is a sphere core inside a small cuboid shell, a bright status lamp, and two side-mounted hover rotors.

**EMP Blast (ultimate):** instant area attack with an 8 m radius, cooldown 55 s. Emits an amber pulse ring and particle burst. It does not damage enemies directly in this implementation. Living enemies in range are disrupted/slowed for 3 s: player controls are locked for at most 1.25 s, movement is slowed to 55% for the duration, and AI disruption is applied for 3 s. Nearby enemies get their own brief emission-color burst.

Spark also has a support aura that runs passively every 0.25 s: it adds `2.5 * 0.25` shield points to Spark and `1.5 * 0.25` shield points to each ally within 8 m per pulse. This is separate from activating Repair Drone.

```csharp
while (Time.time < endTime && !damageable.IsDead)
{
    damageable.Heal(4f * Time.deltaTime);
    damageable.AddShield(5f * Time.deltaTime);
    yield return null;
}
```

```csharp
const float radius = 8f;
RoboClashCombatVFX.PlayPulse(transform.position + Vector3.up,
    definition.emissionColor, radius);
...
candidate.GetComponent<RoboClashRobotAbilities>()?.ApplyEmp(3f, 0.55f);
```

Sources: [Spark ability/ultimate routing and drone motion](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L169), [repair pulse and support aura](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L307), [EMP effect](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L420), [drone construction](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L347), [beam construction and endpoints](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs#L114), [pulse ring geometry/animation](./Assets/Scripts/RoboClash/RoboClashPulseRing.cs#L14).

**In play, the sequence is:** the small drone is already present and hovers at Spark's right side, with a gentle up/down sinusoidal bob and constant rotation. Repair Drone plays the standard amber activation cue, turns on a line from the drone to Spark, and continuously restores health and shield while the timer is active. It does not fly out to an ally: the beam endpoint is Spark itself. EMP Blast instead produces the shared ultimate pulse at Spark, with a ring whose outer radius expands to 8 m in about 0.55 s. All valid enemies within 8 m receive the disruption burst and the gameplay slow/control lock; the pulse does not spawn a damaging EMP projectile.

## Shared visual effects and how the shapes animate

- **Dash / burst:** pooled particle system, cone emission, emissive team color. A dash places three heavy bursts between the start and end points.
- **Area pulse:** a particle burst plus a flat 48-segment `LineRenderer` circle. The ring expands from a small scale to the requested radius over 0.55 s and fades while thinning.
- **Shield / Overdrive field:** a collider-disabled sphere parented to the model. It is translucent and non-shadowing; Defensive Field uses a steady 1.48 scale, Overdrive uses 1.58 scale and a subtle sine pulse.
- **Repair beam:** two-point world-space line from the hovering drone to a point above Spark. It is amber at the drone and white at the robot.
- **Projectile trail:** ordinary shots use a short thin trail; rockets use a trail twice as long and wider.
- **Muzzle:** a team-colored particle burst and a short-lived point light. These are transient firing feedback, not ability-only visuals.

The effects use pooled particle systems (up to 32) for bursts; the pulse ring is a separate short-lived object. Relevant implementations: [RoboClashCombatVFX.cs](./Assets/Scripts/RoboClash/RoboClashCombatVFX.cs#L22), [RoboClashRobotVFX.cs](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs#L94), [RoboClashPulseRing.cs](./Assets/Scripts/RoboClash/RoboClashPulseRing.cs#L23).

### Full combat VFX lifecycle

The effects above are part of a broader runtime feedback loop:

1. **Weapon fire:** the projectile is team-colored and receives a `TrailRenderer`; Spark rockets use a larger projectile, wider trail, and longer trail lifetime. The muzzle emits a short colored particle burst and a point light that fades out at 55 intensity units per second. The mesh muzzle flash is enabled for 0.06 s. Titan gets the heavy muzzle size and its barrels spin while firing.
2. **Projectile hit:** on collision, a burst is oriented to the impact normal and tinted with the firing robot's emission color. Splash impacts use the larger/heavier particle configuration. The effect also plays an impact sound at the hit point. Energy-shield impacts use the shield-impact sound; absorbed direct hits still produce a heavy burst. Friendly robots are ignored.
3. **Hit reaction:** a successful damaging hit triggers a quick target recoil/hit pose. The projectiles themselves are destroyed after impact; there is no persistent scorch mark or decal in this implementation.
4. **Ability start:** all robots share an activation burst at chest height and a generic ability pose. Ultimate activation uses an expanding pulse ring with a default 7 m radius, in addition to the robot-specific effect. Some ultimates therefore show both this shared activation pulse and their dedicated shot/area effect.
5. **Respawn:** a colored ring and burst use a smaller 2.2 m radius, accompanied by the scripted spawn pose/camera transition.
6. **Cleanup:** death, disable, or round reset cancels active coroutines and turns off persistent shield/overdrive fields, repair beams, and muzzle light. This prevents effects from remaining after their robot is disabled.

The shared particle system is configured as a non-looping cone with billboard particles and world-space simulation. A normal burst emits 10 particles; a heavy burst emits 20. Normal particle lifetimes are about 0.12–0.38 s, heavy lifetimes about 0.2–0.55 s. Reusing the pool limits transient particle-system creation; the pool holds up to 32 systems and reuses an older one when full.

Code locations: [weapon muzzle and projectile setup](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs#L111), [impact and damage feedback](./Assets/Scripts/RoboClash/ArenaProjectile.cs#L65), [particle and trail setup](./Assets/Scripts/RoboClash/RoboClashCombatVFX.cs#L22), [muzzle light and beam updates](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs#L36), [respawn visuals](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs#L75), [ability cancellation/cleanup](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L257).

### Effects which are visual-only or not yet represented

- Spark's **Support Aura** applies shield points every 0.25 s to Spark and eligible allies, but the support-aura method does not spawn a pulse, beam, or other visible effect. The Repair Drone beam is only enabled during the active Repair Drone ability.
- EMP's expanding ring and disruption bursts are presentation; the gameplay effect is the control lock/slow and AI disruption, not direct damage.
- Cloak switches renderers off and enables its separate translucent shell. It is an instantaneous visibility switch; there is no dissolve, fade, shimmer animation, or per-part cloaking transition in the current code.
- The code does not create persistent impact decals, ground scorch marks, shell fragments, or ability-specific skeletal animation clips.

## Weapons and combat animation notes

Every weapon launches a runtime sphere projectile from the generated muzzle. The projectile gets the robot's emission color and a trail; Spark's rocket also uses its configured splash radius (`3.5 m`). Fire rate, damage, magazine size, reload, and range are set per definition. Weapon mounts recoil backward on firing, arms/body receive a procedural recoil pose, and the muzzle effect briefly illuminates nearby geometry. Titan's four barrels rotate while firing; Spark's launcher has the heavy, slower rocket shot.

Core projectile setup:

```csharp
var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
projectile.name = definition.robotType == RoboClashRobotType.Spark
    ? "Spark Rocket" : definition.weaponName + " Bolt";
projectile.transform.SetPositionAndRotation(muzzle, Quaternion.LookRotation(direction));
projectile.transform.localScale = Vector3.one *
    (definition.robotType == RoboClashRobotType.Spark ? 0.3f : 0.16f);
...
shot.ConfigureVisual(definition.emissionColor,
    definition.robotType == RoboClashRobotType.Spark);
```

Source: [firing and projectile setup](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs#L111), [weapon geometry](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs#L275).

## Passive labels versus implemented code

The selection menu displays these passive labels, but the code that actually runs is not fully aligned with all the names:

| Menu passive label | What the current runtime code does |
|---|---|
| Volt — **Energy Efficiency** | A movement-linked reload bonus: after sustained movement at at least 75% of max speed, reload duration is reduced by up to 25%; it resets after stopping for 1.5 s. |
| Titan — **Fortified** | Implemented through its `0.85` incoming-damage multiplier (15% reduction) and `0.75` knockback resistance. The separate Defensive Field ability reduces damage by 45% while active. |
| Shadow — **Stealth Movement** | The label is configured, but no passive movement/stealth modifier was found in the robot runtime scripts. Cloak is the active ability, not a passive. |
| Spark — **Support Aura** | Implemented: periodically restores shields to Spark and nearby same-team robots within 8 m. |

The in-menu ability/ultimate summary strings are in [GetAbilitySummary/GetUltimateSummary](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs#L1097). Runtime passive evidence is in [Volt reload bonus](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs#L84), [Titan stats](./Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs#L96), and [Spark support aura](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs#L454). In particular, do not interpret Titan's 45% ability reduction as its always-on passive.

## Implementation observations

1. **Ability poses are generic.** `TriggerAbilityPose` blends one torso lean for all abilities. The visual identity mostly comes from gameplay movement, the activated sphere/beam/cloak shell, particles, and audio rather than unique authored animations.
2. **Shadow's cloak is renderer hiding.** The currently visible cloak shell acts as an outline, rather than a shader transition on the armor.
3. **Spark's EMP is control disruption, not direct damage.** Any EMP damage impression comes from its ring/burst VFX; the effect applies slow/lock and AI disruption.
4. **Some definition durations describe active presentation, not attack wind-up.** Rail Cannon and Shadow Strike have zero timed ultimate duration; Titan's rail charge is a separate 0.7 s coroutine.
5. **Current model mesh is deliberately low-complexity.** The design uses layered bevel armor and emissive details rather than imported skeletal meshes, skinning, or clip-based animation.
