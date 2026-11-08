# RoboClash Motion, Animation, Shooting, and Effects Report

**Project:** RoboClash, Unity 6.6 / URP  
**Scope:** Robot locomotion, camera motion, procedural character animation, AI motion, moving platforms, shooting/projectiles, abilities, visual effects, and related audio.

This report describes the current implementation in the project source. The code blocks below are **source excerpts**, not complete copies of every referenced class. They focus on the logic that creates or coordinates motion and effects. For the full implementation, use the linked source files.

## 1. System overview

Robot motion and presentation is split across runtime components:

| Responsibility | Existing component |
|---|---|
| Player locomotion, jump, first-person look/camera, platform carry, footsteps | [RoboClashRobotMovement.cs](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) |
| Procedural limb, torso, head, recoil, landing, ability, and death pose | [RoboClashRobotAnimator.cs](./Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs) |
| AI movement intent, aiming, safe-direction selection, and attack prediction | [RoboClashRobotAI.cs](./Assets/Scripts/RoboClash/RoboClashRobotAI.cs) |
| Fire rate, ammunition, projectile launch, weapon recoil, barrel spin | [RoboClashRobotCombat.cs](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) |
| Projectile travel, collisions, damage, knockback, and impact effects | [ArenaProjectile.cs](./Assets/Scripts/RoboClash/ArenaProjectile.cs) |
| Reusable particle bursts, projectile trails, muzzle effects, dash and pulse effects | [RoboClashCombatVFX.cs](./Assets/Scripts/RoboClash/RoboClashCombatVFX.cs) |
| Expanding/fading pulse ring | [RoboClashPulseRing.cs](./Assets/Scripts/RoboClash/RoboClashPulseRing.cs) |
| Robot-specific shield, repair beam, muzzle light, respawn, and EMP visuals | [RoboClashRobotVFX.cs](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs) |
| Ability timing and robot-specific ability/ultimate behavior | [RoboClashRobotAbilities.cs](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) |
| Moving arena platform and its velocity | [ArenaMovingPlatform.cs](./Assets/Scripts/RoboClash/ArenaMovingPlatform.cs) |
| Spatial sound events and generated fallback clips | [RoboClashRobotAudio.cs](./Assets/Scripts/RoboClash/RoboClashRobotAudio.cs) |
| Robot visual hierarchy and component hookup | [RoboClashRobotFactory.cs](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs) |
| Per-robot intro and first-person camera anchors | [RoboClashRobotDefinition.cs](./Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs) |
| Player death/respawn integration | [RobotArenaController.cs](./Assets/Scripts/RoboClash/RobotArenaController.cs) |
| Gameplay HUD and crosshair | [RoboClashHUD.cs](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) |

The gameplay robot's body animation is **procedural Transform animation**, not a state machine driven by an `Animator` component or imported animation clips. The menu preview code does query child `Animator` components for preview assets, but that is separate from the runtime robot animation described here.

## 2. Robot construction and component wiring

`RoboClashRobotFactory` creates the runtime robot root, attaches the movement/combat/AI/ability/animation/audio/VFX components, builds a `ModelRoot` hierarchy, then gives the animated pivots and weapon muzzle to the relevant systems. This hookup is why the runtime animator can animate generated chunky robot parts without requiring a Humanoid rig.

Source excerpt from `CreateRobot`:

```csharp
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
```

The factory also constructs legs, arms, weapon mounts, a head/aim pivot, robot-specific weapon geometry, and stable root-child camera anchors sized from each robot type. `EnsureCameraAnchors` also supplies missing anchors to existing generated prefabs, and finds already-created anchors before making new objects. For example, Titan's rotating Gatling barrels are a child transform that combat rotates while firing.

## 3. Player movement, turning, jumping, and camera

`RoboClashRobotMovement` uses a Unity `CharacterController`. Player and AI motion now share one `ApplyMovement` path for horizontal acceleration, jump/gravity integration, platform carry, movement animation input, and footsteps. Player WASD input is yaw-relative, diagonal movement is normalized, and mouse delta controls yaw and clamped pitch. Input locks clear horizontal movement rather than allowing residual velocity to resume when the lock ends.

The runtime movement speed, acceleration, and rotation speed come from `RoboClashRobotDefinition.stats`. Robot abilities can multiply movement speed. Footsteps are emitted only when the controller is grounded, moving above the threshold, and past the footstep interval.

Source excerpt showing the shared movement integration and animator/audio handoff:

```csharp
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
```

### First-person camera, intro, and collision

The movement component remains the single owner of player yaw, pitch, and the camera's final transform. At spawn the camera blends from a root-relative, behind-and-above anchor to a dimension-scaled eye/cockpit anchor over 1.5 seconds. The existing procedural animator blends a brief crouched-ready pose at the same time. The first-person anchor follows the robot root without inheriting head bob; after the blend, the camera is positioned at that anchor every `LateUpdate`. During the final part of the blend, the player's body renderers are hidden while renderers below the existing weapon mount remain visible. AI participants are configured with no player camera.

The same transition takes 0.65 seconds after respawn. Input and shooting remain locked through either transition; the cursor is locked when first-person control begins. During the blend, a non-allocating sphere cast shortens the camera path when a non-trigger obstruction is detected, ignoring colliders on the controlled robot.

```csharp
cameraTransitionElapsed += Time.unscaledDeltaTime;
var t = Mathf.Clamp01(cameraTransitionElapsed / cameraTransitionDuration);
var eased = t * t * (3f - 2f * t);
var cameraPosition = Vector3.Lerp(introCameraAnchor.position,
    firstPersonCameraAnchor.position, eased);
cameraPosition = ResolveCameraPosition(firstPersonCameraAnchor.position,
    cameraPosition, out _);
var targetRotation = Quaternion.Euler(pitch, yaw, 0f);
playerCamera.transform.SetPositionAndRotation(cameraPosition,
    Quaternion.Slerp(introCameraAnchor.rotation, targetRotation, eased));

SetFirstPersonPresentation(t >= 0.82f);
if (t >= 1f)
    EnterFirstPersonImmediately();
```

In normal play, the camera uses the live first-person anchor position and the same yaw/pitch controlled by the existing Input System mouse loop. The robot root turns with yaw, while pitch is applied to the aim pivot rather than tilting the entire root. Explicit camera states (`Intro`, `FirstPerson`, `Dead`, `Respawning`, and `Disabled`) gate transitions, input, and HUD visibility.

### Moving-platform carry

When the character controller contacts the top of an `ArenaMovingPlatform`, movement stores that platform reference. On the next grounded move, the robot adds the platform's reported displacement. The platform follows a ping-pong path and estimates velocity from its position change:

```csharp
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
```

This is velocity/displacement-based carry for `CharacterController` robots, rather than parenting the robot to the platform. Player and AI robots now continue to receive platform displacement during a temporary ability/EMP input lock while their own horizontal input velocity is stopped; match locks still halt motion. The component has an adjustable offset and cycle duration.

## 4. AI movement and aiming

The AI computes a movement intent and aim intent at a configurable decision interval. It selects approach, strafe/attack, evade, search, or recovery behaviors; the decision uses target range, line of sight, health/threat, and difficulty-profile values. A safe-direction check probes nearby obstacles and hazard triggers before forwarding the resulting direction to `RoboClashRobotMovement.DriveAI`.

When attacking, AI predicts a target point using the estimated target velocity and projectile travel time, adds configurable aim error, and calls `RoboClashRobotCombat.FireAt`. Thus AI uses the same shared movement integration, animation feedback, firing, projectile, and impact systems as player-controlled generated robots. During an ability input lock it sends a zero movement intent through the shared component so gravity and platform carry continue without AI steering.

Source excerpt from the AI attack path:

```csharp
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
```

AI movement is steering and local obstacle/hazard avoidance, not a NavMesh pathfinding guarantee. This report does not claim that AI can solve every multi-room route.

## 5. Procedural robot animation

`RoboClashRobotAnimator` caches the initial local pose and scale of its visual pivots, then composes locomotion and reactions from that base pose in `LateUpdate`. Movement speed is smoothed; gait, lateral lean, acceleration lean, landing pulse, weapon recoil, hit reaction, and ability pose are separate additive contributions. Animation parameters provide distinct gait/recoil/landing personalities for Volt, Titan, Shadow, and Spark without changing gameplay stats.

The following is the **complete `LateUpdate` method** from the current animator:

```csharp
private void LateUpdate()
{
    if (visualRoot == null)
        return;

    var deltaTime = Time.deltaTime;
    stateTime += deltaTime;
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
            Quaternion.Euler(-stride * 0.34f + recoil * recoilMultiplier * 2f + hitReaction * 2f,
                0f, -1.5f * speedFraction);
    if (rightArm != null)
        rightArm.localRotation = rightArmRotation *
            Quaternion.Euler(stride * 0.2f - recoil * recoilMultiplier * 8f - hitReaction * 4f,
                0f, 1.5f * speedFraction);

    if (torso != null)
    {
        torso.localPosition = torsoPosition + Vector3.up *
            (bob + idleBreath - landingPulse * 0.04f);
        torso.localRotation = torsoRotation * Quaternion.Euler(
            -speedFraction * 3.5f * bodyLeanMultiplier -
            accelerationLean * 2.5f * bodyLeanMultiplier -
            abilityLean * 10f + landingPulse * 4f + airborneLean +
            hitReaction * 5f,
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
        visualRoot.localScale = Vector3.Lerp(visualRoot.localScale,
            new Vector3(visualRootScale.x * 0.96f, visualRootScale.y * 0.08f,
                visualRootScale.z * 0.96f), 5f * deltaTime);
    }
}
```

The animation is lightweight and uses no per-frame GameObject or mesh creation. `TriggerSpawnReady` briefly lowers the torso and raises the arms, then eases back to the normal pose during the camera intro; it uses the same procedural animator rather than imported clips or a competing animation system. A landing pulse is raised only after a grounded-to-airborne-to-grounded transition has been observed; the first movement update initializes contact state without inventing a landing. `TriggerHitReaction` blends brief hit motion with locomotion, while `TriggerRecoil` remains the weapon-fire recoil hook. Death animation only affects the visual root and the existing controller remains responsible for disabling gameplay; `SetDead(false)` restores the cached visual scale and resets transient reactions on respawn. This improves gait transitions but is not true foot locking.

## 6. Shooting, recoil, and projectile travel

For player fire, `RoboClashRobotCombat` casts from the center of the first-person camera viewport and uses the first external target or solid obstruction as the aim point. Ray hits on the player's own robot are skipped. The final launch direction is computed from the actual muzzle to that point, then checked again from the muzzle so an arena wall between the weapon and target blocks the shot. AI retains its world-point `FireAt` path and does not receive the player camera. A primitive sphere is created as the projectile; `ArenaProjectile` configures rigidbody velocity, collision detection, owner collision ignores, and lifetime. Ordinary bolts do not use gravity; Spark rockets do.

Each successful shot decreases ammunition and updates the state event, fire cooldown, weapon recoil, procedural robot recoil, firing animation state, audio, muzzle particle/light, and optional brief renderer muzzle flash. Titan's rotating barrels spin during firing. Aim-hit and muzzle-obstruction points are converted back into a safe muzzle-to-point direction with a forward fallback for a near-zero vector; `ArenaProjectile.Launch` also guards against a zero launch direction.

Source excerpt from the firing effects handoff:

```csharp
ammunition--;
StateChanged?.Invoke(this);
var abilities = GetComponent<RoboClashRobotAbilities>();
var rateMultiplier = abilities != null ? abilities.GetFireRateMultiplier() : 1f;
nextShotTime = Time.time + 1f / (definition.stats.shotsPerSecond * rateMultiplier);
recoil = 1f;
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
```

`ArenaProjectile` moves under Rigidbody physics. On collision or a non-ignored trigger, it resolves a shield or damageable target, applies damage and optional area damage, awards hit/ultimate callbacks, applies knockback through `RoboClashRobotMovement.ApplyImpulse`, plays a colored impact burst/audio cue, and destroys the consumed projectile. Friendly robots are ignored; energy shields are handled separately. Rockets may have area damage, while direct projectiles use their configured damage.

Projectile launch excerpt:

```csharp
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
```

When the projectile successfully hits an enemy, knockback is applied as a horizontal movement impulse, scaled by the target's knockback resistance:

```csharp
var movement = target.GetComponent<RoboClashRobotMovement>();
if (movement == null)
    return;

var direction = target.transform.position - point;
direction.y = 0f;
if (direction.sqrMagnitude > 0.01f)
    movement.ApplyImpulse(direction.normalized * knockback);
```

## 7. Projectile trails, bursts, muzzle, dash, and pulse visuals

`RoboClashCombatVFX` is a static helper. It reuses up to 32 `ParticleSystem` effects, reconfiguring the burst's tint, size, lifetime, speed, and emission count. If every pooled effect is alive, it reuses the oldest pool slot. The particle material is shared, and projectile tint is set with a `MaterialPropertyBlock`. Projectile trails are added at launch and have different lifetime/width values for rockets and standard bolts.

The following excerpt shows the bounded pool and burst configuration:

```csharp
private const int MaximumPooledEffects = 32;
private static readonly List<ParticleSystem> Pool = new List<ParticleSystem>(MaximumPooledEffects);

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
```

The same helper provides:

- **Muzzle:** a short burst oriented along the shot direction, plus a short-lived point-light pulse managed by `RoboClashRobotVFX`.
- **Dash:** a heavy burst. The two-endpoint overload places three burst points from dash start to dash end.
- **Ability/impact:** colored bursts with intensity/size tuned by the call.
- **Ultimate/EMP/respawn pulse:** a burst plus an expanding, fading 48-segment `LineRenderer` ring.
- **Projectile visual:** shared material, colored renderer, and a fading trail; the Spark rocket uses a longer/wider trail.

`RoboClashPulseRing` scales its ring outward over its configured duration, fades alpha and line width, then destroys the temporary effect object. These VFX are presentation only; they do not create hazard or damage trigger volumes.

## 8. Ability-driven motion and visual effects

Player ability input uses **Q** and ultimate input uses **E** in `RoboClashRobotAbilities`. Ability cooldowns, active durations, and robot-specific behavior are handled there; visuals are invoked through `RoboClashRobotVFX` and poses through `RoboClashRobotAnimator`.

Notable motion/effect interactions found in the implementation:

- **Volt:** dashes up to five units through `RoboClashRobotMovement.TryDash`, which sphere-casts to shorten the dash before an obstruction. Dash-start/end bursts and an ability pose are triggered. Overdrive can increase movement speed and fire rate.
- **Titan:** a defensive ability visually enables a pulsing shield field and reduces incoming damage for its duration. Its ultimate charges, then fires a heavy projectile.
- **Shadow:** cloak toggles robot renderers while a separate cloak visual is shown; firing breaks cloak. Its strike checks a destination for obstruction, attempts a safe teleport, faces its target, creates dash effects, and fires a projectile.
- **Spark:** repair-drone motion is updated in `LateUpdate` using a small sinusoidal hover and continuous rotation. Repair/ultimate support can emit a beam and pulse, heal, add shield, or apply EMP disruption to opponents.
- EMP can briefly lock movement input and apply a longer movement-speed reduction.

`RoboClashRobotVFX` updates the muzzle light intensity toward zero, scales the active shield field with a small overdrive pulse, and updates the repair-beam endpoints. The projectile, area damage, and health/shield changes remain in the existing combat/abilities systems; VFX is not a second damage system.

## 9. Audio tied to motion and combat

`RoboClashRobotAudio` has three AudioSource voices per robot, spatialized sound, per-event rate limiting, and configurable audio slots. If no clip is assigned to a slot, it creates and caches a short synthesized fallback clip keyed by robot type and audio event. The fallback is generated from tones, overtones, noise, and an exponential envelope; it is not an imported sound asset.

Events include footsteps, weapon fire, reload start/completion, projectile impact, damage, shield impact, abilities, ultimate, death, drone repair, and EMP. Movement calls the footstep event only while grounded and moving. Combat and ability systems emit their corresponding sound events.

## 10. Lifecycle and integration notes

- `RobotArenaController` is the damage/respawn owner. On death it disables control, moves the camera to a stable external view, restores the full robot model, cancels combat/abilities, sets the procedural animator dead state, stops robot VFX, plays death audio, and disables the `CharacterController`. Respawn restores health/shield, re-enables the controller, resets movement, restores animation, plays the respawn pulse, and starts a short camera transition before control is restored.
- The factory-created player and AI robots use `RoboClashRobotMovement` and `RoboClashRobotCombat`. `RobotArenaController` retains an older direct movement/fire path only when the newer movement component is absent.
- Match-end locking uses the existing controller/movement/combat/ability cancellation hooks. This report does not change match rules.
- Projectiles are instantiated at runtime and are destroyed on impact or lifetime expiry. Particle effects use a bounded pool; pulse-ring objects are short-lived. The report documents this current behavior and does not claim all runtime allocations are eliminated.

## 11. Controls and practical inspection

For the generated player robot, the input paths in the source are:

| Input | Current behavior |
|---|---|
| W / A / S / D | Move relative to camera yaw |
| Space | Jump while grounded |
| Mouse movement | Look/aim |
| Left mouse button | Fire while cursor is locked |
| R | Reload |
| Q | Robot ability |
| E | Ultimate |
| Escape | Release cursor |

To inspect these systems in Unity, enter Play Mode with a generated player robot and observe the root `CharacterController`, `ModelRoot`, leg/arm/torso pivots, weapon muzzle, `RoboClashRobotCombat`, and temporary projectile/effect objects. Verify the actual feel and visuals in Play Mode on the project's target hardware; this report is source inspection and is not a substitute for runtime testing.

The centered crosshair is generated by the existing HUD, outlined for contrast, hidden during the intro/death/pause/results states, slightly enlarged while firing, and briefly turns green on confirmed hits.

## 12. Validation and limitations

- **Source inspection:** Performed for the movement, combat, AI, factory hookup, projectile, procedural animator, VFX, pulse-ring, ability, audio, player controller, and moving-platform implementations referenced above.
- **Implementation changes:** First-person camera anchors/transitions and player-only weapon presentation, a procedural startup pose, first-person center-screen aiming with muzzle obstruction checks, and crosshair state/feedback were integrated into the existing runtime components. Shared player/AI movement integration and lock handling, smoothed robot animation and type-specific presentation, hit reactions, and safe projectile direction handling remain in place.
- **Compilation:** `dotnet build Assembly-CSharp-Editor.csproj --no-restore` succeeded with 0 warnings and 0 errors after the implementation.
- **Unity Scene/Play Mode checks:** Not performed. Factory spawning, actual input/camera feel, all four robot animations, impacts, abilities, respawn, platform traversal, and AI combat still need Play Mode verification. No claim is made that animation feel or collision behavior was visually/runtime-tested.
- **Known architectural boundary:** Robot locomotion is CharacterController-based and AI uses steering/local obstacle and hazard probes; it is not full NavMesh pathfinding. The gameplay robot's animation is procedural transform motion rather than imported animation clips.
