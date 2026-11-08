# RoboClash Implementation Report

**Report date:** 2026-10-24  
**Project:** RoboClash, Unity 6.6 / URP  
**Purpose:** Record the robot presentation upgrade, the source and generated assets involved, the implemented animation/effect behavior, and what still needs in-Unity verification.

## 1. Summary

The existing four-robot gameplay implementation was upgraded rather than replaced. The update adds a shared procedural robot animator, spatial robot audio with synthesized fallback clips, a bounded combat-particle pool, short-lived pulse rings, and robot-specific visual effects. These components are wired into the existing factory, movement, combat, ability, projectile, damage, and roster-generation systems.

The robot body geometry is still procedural placeholder art. It is more layered and character-specific than a stack of primitives, but it is not a substitute for professional rigged character models, authored textures, or animation clips.

The generated asset files currently present in the project are:

- Four robot prefabs: VOLT, TITAN, SHADOW, and SPARK.
- One shared beveled armor mesh.
- Twenty generated materials: five for each robot, including shimmer materials.

No recognized audio files (`.wav`, `.mp3`, `.ogg`, `.aiff`, `.aif`, or `.flac`) were found under `Assets`. The audio component therefore has synthesized placeholder sounds unless clips are assigned in the Inspector.

## 2. New source files

| File | Responsibility |
|---|---|
| [RoboClashRobotAnimator.cs](./Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs) | Pose-based idle, movement, airborne, landing, recoil, ability, death, and respawn animation. |
| [RoboClashRobotAudio.cs](./Assets/Scripts/RoboClash/RoboClashRobotAudio.cs) | Configurable audio-event slots, three spatial AudioSource voices per robot, throttling, and cached synthesized fallback clips. |
| [RoboClashRobotVFX.cs](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs) | Shield field, muzzle light, repair beam, dash/impact/ability feedback, and effect shutdown. |
| [RoboClashCombatVFX.cs](./Assets/Scripts/RoboClash/RoboClashCombatVFX.cs) | Shared particle bursts, 32-entry reusable particle-system pool, projectile trails, accent colors, and pulse-ring entry point. |
| [RoboClashPulseRing.cs](./Assets/Scripts/RoboClash/RoboClashPulseRing.cs) | Expanding, fading ground-plane ring with timed cleanup. |

Each new script also has a Unity `.meta` file:

- [RoboClashRobotAnimator.cs.meta](./Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs.meta)
- [RoboClashRobotAudio.cs.meta](./Assets/Scripts/RoboClash/RoboClashRobotAudio.cs.meta)
- [RoboClashRobotVFX.cs.meta](./Assets/Scripts/RoboClash/RoboClashRobotVFX.cs.meta)
- [RoboClashCombatVFX.cs.meta](./Assets/Scripts/RoboClash/RoboClashCombatVFX.cs.meta)
- [RoboClashPulseRing.cs.meta](./Assets/Scripts/RoboClash/RoboClashPulseRing.cs.meta)

## 3. Existing source files modified

| File | Changes |
|---|---|
| [RoboClashRobotFactory.cs](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs) | Creates layered character silhouettes, the shared beveled armor mesh, character-specific equipment, and explicit animator/audio/VFX transform references. |
| [RoboClashRobotDefinition.cs](./Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs) | Holds generated emission and shimmer material references used by the presentation systems. |
| [RoboClashRobotMovement.cs](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) | Feeds actual movement/grounding state to the animator and emits movement audio through the shared audio component. |
| [RoboClashRobotCombat.cs](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) | Adds successful-shot feedback, muzzle light/flash, recoil, firing/reload audio, and Titan barrel animation while retaining the existing projectile/damage flow. |
| [RoboClashRobotAbilities.cs](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) | Triggers presentation effects from the existing ability/ultimate activation and duration logic; cancels and restores temporary states when required. |
| [ArenaProjectile.cs](./Assets/Scripts/RoboClash/ArenaProjectile.cs) | Adds projectile trails, impact bursts/sound, direct-hit feedback, and existing damage registration remains in this projectile path. |
| [RobotArenaController.cs](./Assets/Scripts/RoboClash/RobotArenaController.cs) | Connects damage, death, respawn, and hit confirmation to the new feedback components. |
| [RoboClashRobotRosterGenerator.cs](./Assets/Editor/RoboClashRobotRosterGenerator.cs) | Saves the shared armor mesh and shimmer materials, supplies generated materials/references to the factory, and saves four reusable robot prefabs. |

The arena generator and arena gameplay components were not changed as part of this presentation upgrade.

## 4. Robot model and visual identity

The factory builds a shared visual hierarchy under `ModelRoot`, separate from the root `CharacterController`. The shared custom armor mesh has clipped corners and multiple rings to create a beveled shell. Many armor pieces reuse this mesh; spheres, cylinders, and rods supply joints, cores, sensors, conduits, and other small mechanical features.

| Robot | Intended silhouette and equipment |
|---|---|
| **VOLT** | Athletic blue/white armor, cyan accents, power unit, and compact pulse rifle. |
| **TITAN** | Wider and heavier red armor, reinforced torso/shoulders and legs, and a heavier multi-barrel weapon. |
| **SHADOW** | Compact dark/purple armor, narrow profile, stealth emitters, and a compact energy weapon. |
| **SPARK** | Yellow/charcoal utility armor, repair equipment, and a separate hovering repair drone. |

The factory keeps the weapon mount, muzzle, aim pivot, rotating-barrel transform, and drone transform as explicit references in the robot definition. The models are procedural silhouettes and should be treated as placeholders for eventual artist-authored assets.

## 5. Animation behavior

`RoboClashRobotAnimator` caches the local pose of its visual transforms and computes pose offsets from those original values. It does not animate the `CharacterController` root. Movement speed and grounded state are supplied by the existing movement component.

| State / event | Current implementation |
|---|---|
| Idle | Small torso/head breathing motion. |
| Walk/run | Movement speed scales alternating leg swing, opposite arm swing, torso bob, and forward lean. Run is the faster part of the same speed-driven animation, not a separate animation clip. |
| Aim | The existing aim pivot/weapon flow remains responsible for aiming; the procedural animator does not implement a separate aim state machine. |
| Shoot | Combat recoil offsets the weapon mount; the animator adds arm/head recoil response. |
| Jump/fall/landing | Grounded state affects torso pitch; a landing pulse is triggered on the transition back to grounded. |
| Ability | Ability hooks apply a temporary torso lean, with timed reset. |
| Death/respawn | The visual hierarchy is squashed on death; respawn restores its scale and adds a landing-style pulse. |

This is a procedural animation foundation, not a rigged skeletal animation system. Although the project contains an imported Mech model/controller, its animation hierarchy is not a verified match for these procedurally generated robots, so those clips were not assigned.

## 6. Combat, projectile, and damage feedback

The existing `Fire`/`FireAt` path still owns firing eligibility, ammunition, cooldown, aiming/obstruction checks, and projectile launch. Effects happen after a shot passes those checks; a rejected shot does not trigger the shot-feedback calls.

- The combat component limits its tracked weapon projectiles to 32 active entries.
- Each successful shot launches the existing `ArenaProjectile` with the robot's team accent and weapon configuration.
- The weapon mount is displaced briefly for recoil and then returns to its cached rest position.
- Muzzle bursts and a short-lived point-light pulse are colored by the robot accent.
- Titan receives stronger recoil and heavy muzzle presentation; its barrel transform is animated by the combat component.
- Projectile trails are configured on the projectile itself.
- Impacts use pooled particle bursts and play impact/shield sounds; direct robot hits can trigger target recoil and the existing damage/hit-confirmation path.
- Ability-fired projectiles use the shared projectile behavior and its lifetime, but are created by the ability component outside the combat component's tracked-projectile set.

The particle burst pool is capped at 32 systems and reuses completed effects. Pulse rings and a small number of temporary light/beam objects have their own short-lived or disabled lifecycles rather than being part of that particle pool.

## 7. Ability and ultimate presentation

The presentation hooks run from the existing ability and ultimate logic. They do not introduce a second cooldown or damage system.

| Robot | Ability | Ultimate |
|---|---|---|
| **VOLT** | Existing dash movement; emits dash bursts and a timed forward pose. | Overdrive uses the existing duration/stat multipliers and enables the accent shield/aura visual until the effect ends. |
| **TITAN** | Defensive Field applies the existing temporary damage multiplier and enables a visible field for the configured duration. | Rail Cannon shows a charge burst/pose, waits before firing, and launches an ability projectile. |
| **SHADOW** | Cloak toggles model renderer visibility and a separate shimmer shell; firing cancels cloak through the existing combat check. This is a shimmer/visibility effect, not a gradual fade of every original material. | Shadow Strike checks a candidate destination against blocking geometry before teleporting, then emits dash feedback and fires an attack projectile. |
| **SPARK** | Repair Drone effect enables a beam and periodically heals/restores shield according to its existing timed routine. The current routine targets SPARK itself; this is not yet a selectable ally-target UI. | EMP creates an expanding pulse and applies disruption/slow only to eligible enemy robot definitions in range. |

Temporary ability coroutines restore their gameplay values and turn off relevant visuals after their durations. `CancelAll` also resets temporary multipliers, control locks, shield/overdrive visuals, repair targeting, ability pose, and cloak state.

## 8. Audio

`RoboClashRobotAudio` exposes slots for weapon fire, reload start/completion, projectile impact, damage, shield impact, ability, ultimate, death, footstep, drone repair, and EMP.

- Spatial audio is enabled by default, with logarithmic rolloff and a configurable maximum distance.
- Each robot has three AudioSource voices; event playback is rate-limited to prevent excessive overlap.
- Inspector-assigned clips take precedence.
- If a clip is not assigned, a short synthesized tone/noise clip is generated and cached by robot type and event. Base frequencies differ by robot, and impacts use more noise than movement/weapon tones.
- These generated clips are placeholder sounds, not authored/professional audio.

No imported sound assets were found in the project scan. Sound quality and mix should therefore be treated as provisional until real clips are assigned and tested.

## 9. Editor generator and saved assets

The current Editor source exposes these commands:

1. **Tools > RoboClash > Create Robot Roster**  
   Creates or replaces the generated preview group and four robot prefabs. When existing generated prefabs or the preview group are detected, it asks for confirmation. Unrelated scene objects are preserved.
2. **Tools > RoboClash > Configure Selected Robot As Player**  
   Configures a selected robot instance to use the scene's `MainCamera` and nearest arena spawn zone. The command reports errors if the selection or camera is missing.

The generator saves generated content under `Assets/RoboClash/Generated/`:

- `Prefabs/VOLT.prefab`, `TITAN.prefab`, `SHADOW.prefab`, `SPARK.prefab`
- `Meshes/RC_BeveledArmor.asset`
- `Materials/RC_Roster_<Robot>_Primary.mat`
- `Materials/RC_Roster_<Robot>_Secondary.mat`
- `Materials/RC_Roster_<Robot>_Joints.mat`
- `Materials/RC_Roster_<Robot>_Emission.mat`
- `Materials/RC_Roster_<Robot>_Shimmer.mat`

At report time, all four prefabs, the shared mesh, and all 20 material assets were present on disk. Their presence does not itself prove that every serialized reference is valid or that they render correctly; that requires opening them in Unity and testing.

## 10. Copied code excerpts

The following excerpts are copied from the current project source to show the main integration points. They are not replacements for the linked source files; the complete implementations remain in the files listed above.

### 10.1 Pose-based animation

From `RoboClashRobotAnimator.cs`:

```csharp
public void SetMovement(float speed, float maximumSpeed, bool isGrounded)
{
    speedFraction = maximumSpeed > 0f ? Mathf.Clamp01(speed / maximumSpeed) : 0f;
    if (!grounded && isGrounded)
        landingPulse = 1f;
    grounded = isGrounded;
}

public void TriggerRecoil(float amount)
{
    recoil = Mathf.Clamp01(recoil + amount);
    stateTime = 0f;
}

public void TriggerAbilityPose(float duration)
{
    abilityEndTime = Time.time + Mathf.Max(0.1f, duration);
    abilityLean = 1f;
}
```

The pose update is relative to cached local transforms, so the small offsets do not accumulate:

```csharp
private void LateUpdate()
{
    if (visualRoot == null)
        return;

    stateTime += Time.deltaTime;
    if (abilityLean > 0f && Time.time >= abilityEndTime)
        abilityLean = 0f;
    recoil = Mathf.MoveTowards(recoil, 0f, recoilRecovery * Time.deltaTime);
    landingPulse = Mathf.MoveTowards(landingPulse, 0f, 2.5f * Time.deltaTime);
    var frequency = walkFrequency * Mathf.Lerp(0.35f, 1.45f, speedFraction);
    var stride = Mathf.Sin(stateTime * frequency) * walkAngle * speedFraction;
    var bob = Mathf.Sin(stateTime * frequency * 2f) * 0.018f * speedFraction;
    var idleBreath = Mathf.Sin(stateTime * 1.7f) * 0.008f;

    if (leftLeg != null)
        leftLeg.localRotation = leftLegRotation * Quaternion.Euler(stride, 0f, 0f);
    if (rightLeg != null)
        rightLeg.localRotation = rightLegRotation * Quaternion.Euler(-stride, 0f, 0f);
    if (leftArm != null)
        leftArm.localRotation = leftArmRotation * Quaternion.Euler(-stride * 0.38f, 0f, -2f * speedFraction);
    if (rightArm != null)
        rightArm.localRotation = rightArmRotation * Quaternion.Euler(stride * 0.24f - recoil * 8f, 0f, 2f * speedFraction);

    if (torso != null)
    {
        torso.localPosition = torsoPosition + Vector3.up * (bob + idleBreath - landingPulse * 0.045f);
        torso.localRotation = torsoRotation * Quaternion.Euler(
            -speedFraction * 4f - abilityLean * 12f + landingPulse * 5f + (grounded ? 0f : -5f),
            0f,
            Mathf.Sin(stateTime * 0.8f) * 1.1f);
    }

    if (head != null)
        head.localPosition = headPosition + Vector3.up * (idleBreath * 0.45f + recoil * 0.012f);

    if (dead)
        visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, new Vector3(0.92f, 0.08f, 0.92f),
            5f * Time.deltaTime);
}
```

### 10.2 Successful shot feedback

From `RoboClashRobotCombat.cs`, after eligibility and aim checks, the existing projectile is launched and successful-shot presentation is triggered:

```csharp
var shot = projectile.AddComponent<ArenaProjectile>();
shot.ConfigureVisual(definition.emissionColor, definition.robotType == RoboClashRobotType.Spark);
shot.Launch(gameObject, direction, speed, definition.stats.damagePerShot,
    definition.stats.weaponRange / Mathf.Max(speed, 0.1f), definition.damageRadius, knockback);
activeProjectiles.Add(shot);

ammunition--;
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

if (ammunition == 0)
    Reload();
return true;
```

### 10.3 Shared audio event playback

From `RoboClashRobotAudio.cs`:

```csharp
public void Play(RoboClashAudioEvent audioEvent)
{
    if (voices == null || voices.Length == 0)
        return;

    var index = (int)audioEvent;
    var minimumInterval = audioEvent == RoboClashAudioEvent.WeaponFire ? 0.045f : 0.075f;
    if (Time.unscaledTime < nextAllowedTime[index])
        return;
    nextAllowedTime[index] = Time.unscaledTime + minimumInterval;

    var slot = FindSlot(audioEvent);
    var clip = slot != null && slot.clip != null ? slot.clip : GetFallbackClip(robotType, audioEvent);
    if (clip == null)
        return;

    var source = voices[nextVoice++ % voices.Length];
    source.pitch = (slot != null ? slot.pitch : 1f) * UnityEngine.Random.Range(0.96f, 1.04f);
    source.PlayOneShot(clip, slot != null ? slot.volume : GetFallbackVolume(audioEvent));
}
```

### 10.4 Bounded particle bursts

From `RoboClashCombatVFX.cs`:

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

### 10.5 Ability entry points

From `RoboClashRobotAbilities.cs`, these calls bind effects to the existing ability activation and ultimate charge/cooldown checks:

```csharp
public void ActivateAbility()
{
    if (Time.time < nextAbilityTime || damageable.IsDead)
        return;

    if (definition.robotType == RoboClashRobotType.Volt)
    {
        var dashDirection = movement.PlayerControlled && movement.PlayerCamera != null
            ? movement.PlayerCamera.transform.forward
            : transform.forward;
        var dashStart = transform.position;
        if (!movement.TryDash(dashDirection, 5f))
            return;
        visualEffects?.PlayDash(dashStart, transform.position, dashDirection);
        animator?.TriggerAbilityPose(0.35f);
        nextAbilityTime = Time.time + abilityCooldown;
        return;
    }

    nextAbilityTime = Time.time + abilityCooldown;
    visualEffects?.PlayAbility();
    animator?.TriggerAbilityPose(definition.abilityDuration);
    switch (definition.robotType)
    {
        case RoboClashRobotType.Titan:
            StartEffect(DefensiveField(definition.abilityDuration, 0.55f));
            break;
        case RoboClashRobotType.Shadow:
            StartEffect(Cloak(definition.abilityDuration));
            break;
        case RoboClashRobotType.Spark:
            StartEffect(RepairDronePulse(definition.abilityDuration));
            break;
    }
}
```

### 10.6 Generator menu and saved geometry

The Editor menu item is declared in `RoboClashRobotRosterGenerator.cs`. This copied excerpt shows the confirmation guard and the persistent prefab-generation loop:

```csharp
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
```

The full generator implementation, including prefab paths, material setup, player configuration, and preview handling, is in the linked Editor source file.

## 11. Validation status and remaining work

### Completed earlier

- Runtime source compilation: reported successful, 0 errors, with one existing obsolete-API warning in `ArenaBotController.cs`.
- Editor source compilation: reported successful, 0 errors, with one existing obsolete-API warning in `RoboClashArenaGenerator.cs`.
- The prior compile check used temporary build includes for newly added scripts because the generated Unity runtime project file did not list them. Temporary validation files were removed afterward.

These build results are from the implementation work; compilation was not rerun solely to create this report.

### Not yet verified in Play Mode

Please open the project in Unity and test:

1. Open each generated prefab and check that serialized material, mesh, animator, audio, weapon, muzzle, drone, and VFX references are assigned.
2. Run **Tools > RoboClash > Create Robot Roster** only if you want to regenerate the roster; accept its replacement confirmation when appropriate.
3. Put a robot in the arena, select it, and run **Tools > RoboClash > Configure Selected Robot As Player**. Verify the scene has a `MainCamera` and arena spawn zones.
4. Verify movement, aiming, firing cadence, ammunition/reload, projectile direction, collisions, impact damage, and hit confirmation.
5. Test each robot's ability and ultimate start/end behavior, including death during an active effect.
6. Check the appearance and transparency of cloak/shield materials under the active render pipeline and lighting.
7. Listen to fallback audio, then assign real robot-specific clips in the Inspector and balance their 3D volume/pitch.
8. Reopen saved prefabs and inspect the Console for serialization, shader, or missing-reference warnings.

No Play Mode run or visual inspection is claimed in this report. The procedural models, synthesized sounds, and untested material appearance remain provisional until those manual checks are completed.

## 12. Phase 1 gameplay audit and stabilization

This addendum records the gameplay audit performed after the presentation work above. It preserves the existing arena generator, robot factory, robot appearance, and gameplay APIs wherever practical.

### Findings and changes

| File | Change |
|---|---|
| [RoboClashRobotFactory.cs](./Assets/Scripts/RoboClash/RoboClashRobotFactory.cs) | Removed the redundant base `ArenaDamageable` addition; `RobotArenaController` is the single damageable component on generated robots. |
| [ArenaDamageable.cs](./Assets/Scripts/RoboClash/ArenaDamageable.cs) | Rejects non-finite damage and sanitizes non-finite health/shield configuration values. |
| [RobotArenaController.cs](./Assets/Scripts/RoboClash/RobotArenaController.cs) | Avoids accidentally assigning `Camera.main` to generated AI robots, exposes player/camera/spawn state, and coordinates player control and combat/movement reset during death and respawn. |
| [RoboClashRobotMovement.cs](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) | Adds motion-state reset for respawn cleanup. |
| [RoboClashRobotCombat.cs](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) | Prevents firing while dead or with invalid aim, resets firing state, and cleans up owned projectiles when combat is canceled. |
| [ArenaProjectile.cs](./Assets/Scripts/RoboClash/ArenaProjectile.cs) | Ignores unrelated triggers, filters friendly targets and splash damage, and only registers hit feedback/ultimate charge for effective damage. |
| [RoboClashRobotAI.cs](./Assets/Scripts/RoboClash/RoboClashRobotAI.cs) | Acquires valid hostile targets and reacquires after a target becomes invalid. |
| [RoboClashRobotAbilities.cs](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) | Clears temporary ability state when effects are interrupted or canceled, including on death/respawn. |
| [ArenaBotController.cs](./Assets/Scripts/RoboClash/ArenaBotController.cs) | Reacquires a valid active player target instead of retaining a stale target. |
| [RoboClashRobotRosterGenerator.cs](./Assets/Editor/RoboClashRobotRosterGenerator.cs) | Scopes player setup to the active scene, requires an unambiguous camera and spawn zone, confirms control transfer, and adds a narrowly scoped generated-prefab damage-component repair command. |
| [RoboClashValidationTool.cs](./Assets/Editor/RoboClashValidationTool.cs) | Adds the read-only `Tools > RoboClash > Validate Scene Setup` menu command and checks scene structure, robot references/components, spawn zones, relevant missing scripts, hazard/pickup setup, and generated prefab loadability. |

### Scene and asset findings

- `Assets/Scenes/Arena.unity` is the enabled build scene. At inspection it contained a Main Camera, Directional Light, and Global Volume, but no generated arena, robot instances, or spawn zones.
- Four generated robot prefab assets exist under `Assets/RoboClash/Generated/Prefabs/`. They still contain both the base damage component and derived `RobotArenaController`; the runtime factory fix does not rewrite existing serialized prefabs. Use the new repair menu command to remove only the redundant base component from those generated prefabs.
- The lava volumes are triggers over the solid arena floor, not open pits. The floor collider's top is 0 and the lava trigger starts at 0.035, so the validator checks horizontal overlap plus a small vertical gap and warns that the current layout is a damaging floor patch rather than a fall-through pit.
- SPARK's repair routine remains self-healing; ally-target selection is intentionally deferred.

### Validation status

- The VS Code Problems check returned no errors for the workspace.
- Unity's batch compile/prefab-repair attempt was blocked because the project was already open in another Unity Editor. It did not repair the prefab assets.
- The inspected Editor log contained no `error CS` or `Compilation failed` entries, but it did contain `Build asset version error` / `SourceDataChangedDuringImport` messages for files modified during import. Therefore, a successful compile of the final source state is **not confirmed**.
- No Play Mode test was run. No gameplay behavior, scene setup, or prefab repair is claimed as verified in Unity.

### Next Unity checks

1. Let the existing Editor finish importing, then check the Console for current compile/import errors.
2. Run **Tools > RoboClash > Repair Generated Robot Damage Components** and confirm the operation.
3. Run **Tools > RoboClash > Validate Scene Setup** and review its errors and warnings.
4. In `Assets/Scenes/Arena.unity`, run **Tools > RoboClash > Generate Arena** and save the scene. The Phase 2 match manager now configures its spawned player automatically; manual player configuration remains available for legacy scenes.
5. In Play Mode, test movement/camera/jumping; fire/reload/projectile collisions and team filtering; death/respawn; all four robots' abilities and ultimates; AI target recovery; hazards, pickups, and moving platforms.

## 13. Phase 2 match loop

### Files created or modified

| File | Change |
|---|---|
| [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) | Adds the match state machine, Inspector settings, participant spawning, safe spawn checks, countdown, timer, free-for-all results, statistics, pause/resume, restart, events, and a minimal in-game match/results overlay. |
| [ArenaDamageable.cs](./Assets/Scripts/RoboClash/ArenaDamageable.cs) | Adds source-aware damage overloads and effective-damage/elimination events, preserving the existing one-argument `TakeDamage` API. |
| [RobotArenaController.cs](./Assets/Scripts/RoboClash/RobotArenaController.cs) | Supports bounded match respawns and manager-provided respawn locations; locks and cancels participant control/effects when the match ends. |
| [RoboClashRobotMovement.cs](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) | Separates match input locking from ability/EMP control locking. |
| [RoboClashRobotCombat.cs](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) | Rejects firing while match controls are locked, including direct calls to firing methods. |
| [RoboClashRobotAbilities.cs](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) | Rejects ability and ultimate activation while match controls are locked. |
| [ArenaProjectile.cs](./Assets/Scripts/RoboClash/ArenaProjectile.cs) | Carries its damage source into the existing damage path and can be canceled by owner at elimination or match end. |
| [RoboClashArenaGenerator.cs](./Assets/Editor/RoboClashArenaGenerator.cs) | Adds one match manager to the existing generated arena and assigns the already-existing robot prefab assets; the arena design and factory remain unchanged. |
| [RoboClashValidationTool.cs](./Assets/Editor/RoboClashValidationTool.cs) | Extends the existing read-only validator to check match manager count/configuration and warn when spawn zones are fewer than the configured participants. |

### Match behavior

- Arena generation now attaches exactly one `RoboClashMatchManager`. Its serialized settings default to VOLT, three AI opponents, a 180-second timer, a three-second countdown, no respawns, and no fixed random seed. The Inspector supports choosing the player robot, one to three opponents, bounded optional respawns, and repeatable AI robot-type shuffling with a seed.
- The manager reuses the four existing assets at `Assets/RoboClash/Generated/Prefabs/{VOLT,TITAN,SHADOW,SPARK}.prefab`; all four paths were checked and exist. It assigns each participant a unique team ID so the existing AI and projectile friendly-fire rules behave as free-for-all rules.
- Existing scene robot instances and the arena generator's legacy player/training targets are temporarily deactivated while a match runs, then restored if the manager is destroyed. The existing MainCamera is rebound to the spawned player; the manager does not create another camera.
- Spawn zones are used in order. Missing positions use four fixed interior fallback positions, validated against the arena floor, solid obstructions, hazards, and other selected spawn points. Respawn-enabled participants return to their assigned safe spawn point.
- The match transitions through Initializing, Ready, Countdown, and Playing, then reaches Victory, Defeat, or Draw. Elimination resolution is deferred to frame end so same-frame player/opponent eliminations can resolve as a Draw.
- At timeout, living participants are compared by health plus shield. The player wins if the player is the only survivor or has strictly more combined health/shield than every surviving opponent. An exact tie is explicitly a Draw; otherwise the player is defeated.
- Damage and elimination events provide player damage dealt/received, deaths, and eliminations without adding a second health system. Events, read-only state/timer/statistics properties, and the `P`/button pause-resume controls are available for later UI integration. Restart is available from the results overlay and replaces only the manager-owned participant group.
- Match end cancels participant-owned projectiles, abilities, combat, and AI decisions while retaining the arena and death presentation. The basic overlay displays countdown, timer, player eliminations/deaths, pause/resume, and final results; it is not a full HUD or menu.

### Verification and remaining work

- VS Code Problems diagnostics reported no errors for the match manager, changed runtime scripts, arena generator, or validator.
- Unity's Editor log showed script compilation cycles without `error CS` or `Compilation failed` entries, but its last recorded compile preceded the final edits to several scripts. Therefore the **final source revision's Unity compilation is not confirmed**.
- The existing enabled scene still needs arena generation. The existing four robot prefabs also still need the earlier **Repair Generated Robot Damage Components** command; the match manager intentionally refuses to start with those duplicate damage components. Neither the scene generation nor prefab repair was performed here.
- No automated test files were present under `Assets/Tests`, and no Play Mode test was run. The match lifecycle, spawning, AI combat, timer results, pause/resume, restart, and ability behavior remain unverified in Unity.

### Unity setup and test checklist

1. Let Unity finish importing the changed scripts and resolve any Console errors.
2. Run **Tools > RoboClash > Repair Generated Robot Damage Components** once to repair the four existing generated prefabs.
3. Open `Assets/Scenes/Arena.unity`, run **Tools > RoboClash > Generate Arena**, and save the scene. The generator attaches the match manager and its prefab references.
4. Run **Tools > RoboClash > Validate Scene Setup**. Confirm one MainCamera, a configured manager, loadable prefabs with one damage component each, and four spawn zones (or the fallback-spawn warning).
5. In the arena Inspector, set the desired player robot and one to three AI opponents; keep respawns off for the initial elimination mode.
6. Enter Play Mode. Confirm the countdown, player controls (WASD, mouse, Space, left click, Q/E), AI movement/attacks, pickups/hazards, and the match timer.
7. Test player defeat, eliminating all opponents, a tie/simultaneous elimination, and timeout scoring. Temporarily reduce the match duration in the Inspector to exercise timeout quickly.
8. Test **P** and the pause overlay, then restart after Victory, Defeat, and Draw. Repeat a restart and confirm there is still only one MainCamera and one player plus the configured AI count.
9. If testing optional respawns, set `Respawn Enabled` and a positive `Maximum Respawns`; verify that the final permitted death ends the participant's match.

## 14. Phase 3 main menu and robot selection

### Files created or modified

| File | Change |
|---|---|
| [RoboClashMenuController.cs](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) | Builds a responsive uGUI main menu, robot selection/details screen, and settings screen at runtime. It uses the existing robot prefab definitions for combat stats, weapon, ability, and ultimate data; missing definitions disable match confirmation. |
| [RoboClashSession.cs](./Assets/Scripts/RoboClash/UI/RoboClashSession.cs) | Persists the selected robot and AI opponent count and supplies a one-use match-start handoff. |
| [RoboClashMenuSceneGenerator.cs](./Assets/Editor/RoboClashMenuSceneGenerator.cs) | Adds **Tools > RoboClash > Create Main Menu UI**. It refuses to overwrite an existing menu scene, validates the generated Arena and robot prefabs, asks before creating the menu, and registers both scenes without changing the current startup-scene order. |
| [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) | Consumes the menu's pending selection in its existing `Start` flow before initializing participants and returns to the menu from the results overlay. |

### Implemented flow

- The menu scene's controller constructs its own Canvas and Input System UI EventSystem at runtime; it does not add a camera or instantiate preview robots. Robot selection is card-based, avoiding live gameplay components during preview.
- Play opens the four-card robot selection. The detail panel reads health, shield, speed, damage, fire rate, weapon, ability, and ultimate from each existing prefab's `RoboClashRobotDefinition`; role/short-description copy is presentation text. Selected robot and opponent count are persisted.
- Confirm validates the chosen definition and Arena availability, stores a one-use selection, and loads the existing `Arena` scene. The existing match manager consumes that value before its normal participant spawn/countdown initialization; it remains the sole match manager and robot factory.
- The generated settings screen persists master volume (`AudioListener.volume`), fullscreen, and a selectable supported resolution. There is no AudioMixer/routing in the project, so separate Music and SFX sliders are intentionally not claimed.
- The editor generator creates `Assets/Scenes/RoboClashMainMenu.unity` only if absent. It requires the saved Arena to have a configured match manager and active MainCamera, and it validates the existing robot prefabs. It appends/enables both scenes in Build Settings while retaining the existing startup scene.

### Validation and limitations

- VS Code Problems diagnostics reported no errors for the menu controller, session model, scene generator, or match manager after the Phase 3 changes.
- The runtime and editor assemblies compiled successfully with `dotnet build Assembly-CSharp-Editor.csproj` after temporarily including the new Phase 3 files. The build had zero errors and one existing obsolete-API warning in `RoboClashArenaGenerator.cs`. Temporary build includes and outputs were removed afterward.
- A Unity batch-mode launch exited with code 1 before reaching script compilation. Unity's own compilation, UI rendering/input, scene generation, Build Settings mutation, and Play Mode flow are therefore **not** confirmed. The menu scene was not generated, so no scene asset or Build Settings entry was changed.
- The Arena must first be generated and saved with **Tools > RoboClash > Generate Arena**; the existing robot prefabs must first be repaired with **Tools > RoboClash > Repair Generated Robot Damage Components**. The menu generator reports errors and stops if either prerequisite is not ready.
- No separate robot preview is included. No separate music/SFX controls are included because there is no mixer or equivalent routing. Quit logs safely in the Editor and calls `Application.Quit()` in standalone builds.

### Unity Editor setup and test checklist

1. Let Unity finish compiling and importing the scripts. Fix any Console errors before continuing.
2. Run **Tools > RoboClash > Repair Generated Robot Damage Components**.
3. Open `Assets/Scenes/Arena.unity`, run **Tools > RoboClash > Generate Arena**, and save the scene. This preserves unrelated scene objects and attaches/configures the existing match manager.
4. Run **Tools > RoboClash > Create Main Menu UI** and confirm the dialog. The tool creates the menu scene and enables Arena/menu in Build Settings without changing the current startup scene.
5. Open `Assets/Scenes/RoboClashMainMenu.unity` and enter Play Mode. Test Play, all four selections and displayed values, opponent-count controls, confirmation, and selected-player spawn/countdown in Arena.
6. From the Arena results overlay, test **MAIN MENU**, then start another match. Confirm one match manager, one active gameplay camera, and no accumulated participants or event systems.
7. Test Back navigation, master volume, fullscreen, resolution, and persistence after reloading the menu scene. Test Quit in a standalone build separately.
8. To launch directly into the menu in a build, use **File > Build Profiles** (or the version's Build Settings window) and move `RoboClashMainMenu` above Arena. The generator intentionally does not change the project's startup order.

## 15. Phase 4 gameplay HUD, pause menu, and results

### Files created or modified

| File | Change |
|---|---|
| [RoboClashHUD.cs](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) | Creates the responsive uGUI Canvas and EventSystem at runtime; displays player vitals, ammunition/reload, ability and ultimate states, match timer/state/countdown, active combatant count, crosshair/hit confirmation, a bounded kill feed, pause/settings overlays, and terminal results. |
| [RoboClashGameplayUIGenerator.cs](./Assets/Editor/RoboClashGameplayUIGenerator.cs) | Adds **Tools > RoboClash > Create Gameplay UI**. It attaches one HUD to the match manager in the active scene, preserves other objects, and marks the scene dirty without silently saving it. |
| [ArenaDamageable.cs](./Assets/Scripts/RoboClash/ArenaDamageable.cs) | Adds a `VitalsChanged` event for effective damage, healing, shield changes, vitals configuration, and restore; existing damage/heal APIs remain intact. |
| [RoboClashRobotCombat.cs](./Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) | Exposes magazine/reload state and emits state-change notifications after firing, reload transitions, completion, and cancellation. |
| [RoboClashRobotAbilities.cs](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) | Exposes authoritative cooldown/readiness/active/charge values and sends activation/state events. It continues to use robot-definition cooldowns and charge requirements; no balance values were changed. |
| [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) | Publishes player-registration and attributed elimination events, exposes the player and alive combatant count, and signals a null player during restart so the HUD can unbind. Removes its previous duplicate IMGUI timer/pause/results overlay. |
| [RobotArenaController.cs](./Assets/Scripts/RoboClash/RobotArenaController.cs) | Publishes hit confirmation only when the existing projectile path reports effective damage; removes the old duplicate IMGUI status display. |
| [RoboClashArenaGenerator.cs](./Assets/Editor/RoboClashArenaGenerator.cs) | Adds one HUD component alongside its existing match manager when generating the Arena. |

### Integration and behavior

- The HUD subscribes to the manager before participant spawning. The existing manager explicitly registers the player (not an AI); restart unregisters the old player and binds the replacement. Damage, ammo, ability, elimination, match-state, countdown, timer, and statistics events refresh the relevant display, with cooldown text and combatant count refreshed at a bounded interval.
- Pause uses the manager's existing pause/resume/restart/return-to-menu APIs. Escape toggles pause/resume only while Playing/Paused, so it cannot open over results. The manager remains responsible for time scale and cursor restoration.
- The results title, robot, eliminations, deaths, and duration are sourced from the match manager's terminal state and statistics. Play Again invokes its existing restart flow; Main Menu invokes its configured `RoboClashMainMenu` scene transition.
- Pause settings persist master volume through `AudioListener.volume` and fullscreen state through `PlayerPrefs`, using the same keys as the main menu. Separate music/SFX sliders remain unavailable because there is no mixer routing.
- The kill feed uses the match manager's damage-source attribution. Unattributed eliminations are labeled generically rather than claiming a killer. Hit confirmation is emitted only for effective damage in the existing projectile path. The previous `OnGUI` gameplay overlays were removed to avoid stacking them over the new Canvas.
- The new UI uses the installed uGUI package and built-in Unity `Text`; it does not require TextMeshPro resources or new packages. Its Canvas is generated at runtime, not serialized into the scene.

### Validation and current project state

- The final runtime and editor assemblies compiled successfully with `dotnet build Assembly-CSharp-Editor.csproj` in isolated temporary output folders: **0 errors**, with one existing obsolete-API warning in `RoboClashArenaGenerator.cs`. VS Code Problems diagnostics also reported no errors in the changed files. Unity's last Editor-log compilation predates the final source edits, so a final Unity Editor compile was not confirmed by that log.
- No VS Code test controller or automated project test suite was available. No Play Mode test or visual UI check was performed.
- The current `Assets/Scenes/Arena.unity` still contains only the original scene setup and MainCamera; it has not been regenerated or modified. Build Settings still include only Arena. Therefore the HUD is implemented but is not yet attached to the saved Arena until the setup below is performed.

### Unity Editor setup and test checklist

1. Run **Tools > RoboClash > Repair Generated Robot Damage Components** once if not already done.
2. Open `Assets/Scenes/Arena.unity`, run **Tools > RoboClash > Generate Arena**, and save. The generator adds its existing manager and the new HUD; it does not add another camera.
3. If the arena already has a configured match manager and you only need to add the interface, use **Tools > RoboClash > Create Gameplay UI** in that active scene. Save the scene after reviewing the dirty-scene change. Re-running the command will not duplicate the HUD.
4. Enter Play Mode and verify the selected player’s health/shield, ammo/reload progress, Q/E cooldown and ultimate charge, timer/countdown, and alive-combatant count.
5. Confirm firing at an enemy shows hit feedback only on actual damage; verify damage/healing/shield changes and low-health/low-ammo notices.
6. Test Escape pause/resume, restart, settings persistence, cursor lock/unlock, and return to the menu. Confirm Escape does not cover terminal results.
7. Exercise Victory, Defeat, and Draw through the match manager's existing win conditions; confirm the result/statistics, Play Again rebind, and Main Menu transition.
8. Repeat a restart and confirm a single manager, one player binding, one camera, and no duplicate HUD/EventSystem or lingering player event subscriptions.

## 16. Phase 5 AI opponents and difficulty

### Files modified

| File | Change |
|---|---|
| [RoboClashRobotAI.cs](./Assets/Scripts/RoboClash/RoboClashRobotAI.cs) | Replaces the basic chase behavior with an interval-driven combat state machine, match-registered target selection, aim leading/error, strafing, line-of-sight checks, obstacle and simple hazard avoidance, damage reactions, and robot-specific ability/ultimate decisions. |
| [RoboClashRobotMovement.cs](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) | Extends `DriveAI` with configurable turn speed and applies existing ability movement-speed effects while preserving CharacterController movement and collision. |
| [RoboClashRobotAbilities.cs](./Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) | Adds AI target handoff for Shadow Strike and excludes inactive/dead candidates from existing area/target selection. |
| [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) | Adds Easy/Normal/Hard settings and editable profiles, supplies the chosen profile to spawned AI, and exposes nearest-opponent and registered-live-opponent queries. |
| [RoboClashSession.cs](./Assets/Scripts/RoboClash/UI/RoboClashSession.cs) | Persists the difficulty choice and carries it through the existing one-use menu-to-match selection handoff. |
| [RoboClashMenuController.cs](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) | Adds an AI Difficulty dropdown to Settings and applies the selection when starting a match. |

### Behavior and constraints

- AI target selection is limited to live, active, opposing participants registered by the match manager; it does not scan arbitrary scene objects. Targets are refreshed periodically, the current target is retained unless invalid/hidden or a significantly closer opponent is found, and a recent attacker is prioritized.
- The state machine covers Searching, Approaching, Attacking, Evading, UsingAbility, Recovering, and Dead. AI remains inactive outside the Playing state and uses the existing movement, `FireAt`, projectile/damage, cooldown, and ability systems; it does not directly change enemy health.
- VOLT favors medium range and uses Dash/Overdrive opportunistically; TITAN prefers closer engagements, uses its Defensive Field under pressure, and waits until aligned before using Rail Cannon; SHADOW favors close flanking and uses Cloak/Shadow Strike based on threat and opportunity; SPARK prefers longer range, uses its existing self-healing Repair Drone when needed, and uses EMP only at close range.
- Difficulty changes reaction/target intervals, sensing range, turning, aim error, aggression, strafing, and basic hazard awareness. It does not secretly multiply weapon damage. Profiles remain editable in the match manager Inspector.
- Hazard avoidance is deliberately limited: it checks authoritative `ArenaHazard` components and nearby ground presence. It is not full route planning, does not use NavMesh, and cannot guarantee avoidance of every moving hazard or inaccessible path. Obstacle recovery uses short physics probes, not pathfinding.
- Existing saved scenes and prefabs were not regenerated or rewritten. The match manager must be present and configured with the generated robot prefabs for this AI to receive the manager, difficulty, and live participant registry.

### Validation status and Unity test steps

- `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with **0 errors and 0 warnings**.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore` succeeded with **0 errors** and one pre-existing obsolete-API warning in `RoboClashArenaGenerator.cs`.
- VS Code Problems reported no errors in the six modified C# files. The Unity Editor log contained a compile error from the earlier four-argument `RequireComponent` declaration; that declaration was corrected to two supported attributes and both generated Unity assemblies then built successfully. A post-fix Unity Editor domain reload was not independently confirmed.
- No automated test files or Play Mode tests were available or run. AI behavior, UI rendering/input, match restart behavior, and the Console still require manual Play Mode verification.

1. Allow Unity to finish compiling, then check the Console for current errors.
2. Open the existing main menu, choose **Settings > AI Difficulty**, and select Easy, Normal, or Hard. Return to robot selection and start a match.
3. Confirm the manager reports the selected difficulty and that one player and the configured number of AI opponents spawn. Ensure the Arena has been generated and saved first.
4. Observe target acquisition, movement, aiming, firing, automatic reload, and retargeting after an opponent is eliminated. Repeat at each difficulty.
5. Test VOLT Dash/Overdrive, TITAN Defensive Field/Rail Cannon, SHADOW Cloak/Shadow Strike, and SPARK Repair Drone/EMP when cooldown/charge and combat conditions permit.
6. Test AI during countdown, pause, resume, respawn if enabled, restart, and after the match ends. Check arena hazards and barriers; treat hazard behavior as simple avoidance rather than guaranteed pathfinding.

## 17. Phase 6 game modes, survival waves, and scoring

### Files modified

| File | Change |
|---|---|
| [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) | Extends the existing single match authority with Free-for-All, Duel, and Survival rules; adds participant/wave lifecycle, safe spawn selection, pause-aware wave breaks, score/placement/wave statistics, and configurable timer/wave limits. |
| [RoboClashSession.cs](./Assets/Scripts/RoboClash/UI/RoboClashSession.cs) | Persists and hands off mode, opponent/starting-wave count, AI difficulty, survival growth/break options, and timer choice to the Arena manager. |
| [RoboClashMenuController.cs](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) | Adds a game-mode selector and relevant opponent/wave settings to robot selection, plus 3-minute, 5-minute, or no-limit match timer settings alongside the existing AI difficulty selector. |
| [RoboClashHUD.cs](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) | Uses the existing HUD for opponent count, wave/enemy/break status, survival elapsed time, eliminations, wave results, placement, and mode-specific results. |

No new manager, damage system, HUD, factory, scene, or robot assets were created. All modes continue to use the registered match participants, existing AI, combat/projectiles, abilities, and results flow.

### Mode rules and scoring

- **Free-for-All:** The configured one-to-three AI opponents and player are separate combatants. AI continue fighting after the player is eliminated; the match ends when one combatant remains (or all are eliminated simultaneously). A time limit selects a result using the existing health-plus-shield comparison. Player placement is recorded at elimination or derived from living opponents' remaining vitals at timeout.
- **Duel:** Exactly one AI is spawned against the selected player robot, independent of the Free-for-All opponent count. The match ends when one side is eliminated, or applies the configured time-limit comparison.
- **Survival:** Spawns the configured starting wave, then waits for the current wave to be fully eliminated before starting one pause-aware break timer and spawning the next wave. The default sequence is 3, 4, 5, and so on; the menu exposes growth of +1 to +3 and breaks of 3, 5, or 8 seconds. The Inspector can configure up to 24 enemies per wave. At that configured cap, wave size stops growing; AI difficulty escalates from Easy to Normal at wave 5 and Hard at wave 10, or Normal to Hard at wave 5. Weapon damage is not scaled. Player elimination ends the run and blocks further wave spawning.
- Spawn attempts use the current arena spawn zones (or existing fallback arena points), reject hazard/obstacle locations, and separate new spawns from living combatants and one another. If the arena cannot provide enough safe, separated positions, the manager logs an explicit error and ends the run rather than spawning overlapping or unsafe robots.
- The manager statistics remain authoritative for player eliminations, deaths, damage, elapsed duration, final result, placement, and highest successfully spawned survival wave. Completed survival-wave robots are unregistered and cleaned up before the next wave; restart clears the current wave/coroutine/roster and starts from wave one.

### Validation status and testing

- Runtime assembly build: `dotnet build Assembly-CSharp.csproj --no-restore` — **succeeded, 0 errors, 0 warnings**.
- Editor assembly build: `dotnet build Assembly-CSharp-Editor.csproj --no-restore` — **succeeded, 0 errors**, with one pre-existing obsolete-API warning in `RoboClashArenaGenerator.cs`.
- VS Code Problems reported no errors in the four modified C# files. No automated tests were found under `Assets/Tests`; no Play Mode test or in-Editor mode validator run was performed. Spawn placement, menu layout/input, survival transitions, pause/restart, FFA placement, duel outcomes, and result display still need Play Mode verification.

### Unity test checklist

1. Let Unity finish importing/compiling, open the generated Arena scene, and confirm its existing match manager has all four robot prefab references. Run **Tools > RoboClash > Validate Scene Setup**.
2. Open robot selection, choose **FREE FOR ALL**, vary the one-to-three opponent count, then verify every robot continues until the final survivor. Eliminate the player early and confirm AI opponents continue fighting; test timeout with the 3-minute, 5-minute, and no-limit settings.
3. Choose **DUEL** with VOLT, TITAN, SHADOW, and SPARK in separate runs. Each run should spawn exactly one AI opponent, respect the chosen difficulty, and display the actual Victory/Defeat/Draw result.
4. Choose **SURVIVAL**, set the starting enemy count, growth, and break time, then eliminate waves. Verify the configured counts, exactly one countdown, robot archetype variety, the fifth/tenth-wave AI difficulty escalation, HUD status, and player-death game over.
5. Pause during a wave and during its break, resume, then restart each mode repeatedly. Confirm no duplicate wave routines/robots, no spawn after game over, correct statistics reset, and no recurring Console errors.

## 18. Phase 7 audio/display preferences, career saves, and progression

### Files created

| File | Purpose |
|---|---|
| [RoboClashSaveService.cs](./Assets/Scripts/RoboClash/RoboClashSaveService.cs) | Versioned JSON settings/career model, safe persistent save/load, legacy preference migration, display/audio application, career statistics, and XP/level calculations. |
| [RoboClashSettingsPanel.cs](./Assets/Scripts/RoboClash/UI/RoboClashSettingsPanel.cs) | Reusable runtime-built audio/display settings UI, used by both the main menu and the pause menu; includes confirmation for settings reset. |

### Files modified

| File | Change |
|---|---|
| [RoboClashRobotAudio.cs](./Assets/Scripts/RoboClash/RoboClashRobotAudio.cs) | Applies the SFX setting to all three existing spatial voices so configured clips and synthesized fallback clips share one volume control. |
| [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) | Records career statistics and awards XP exactly when the authoritative match ends; exposes Inspector-tunable reward amounts. |
| [RoboClashSession.cs](./Assets/Scripts/RoboClash/UI/RoboClashSession.cs) | Keeps the existing robot/mode handoff and updates the preferred-robot profile when the selection changes. |
| [RoboClashMenuController.cs](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) | Replaces the old master-volume/fullscreen-only controls with the shared settings UI, keeps difficulty/timer controls, and adds the career summary and confirmed progress reset. |
| [RoboClashHUD.cs](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) | Reuses the same settings UI while paused and displays the resulting XP/level on the existing results panel. |

### Storage and behavior

- The save file is `roboclash-save.json` under Unity's `Application.persistentDataPath`, never under `Assets`. Settings and career statistics share one versioned data file. Writes go to a `.tmp` file and replace the primary file; unsupported/corrupt saves are preserved and reported rather than silently overwritten.
- The existing PlayerPrefs keys for master volume, fullscreen, and resolution are migrated on first launch without overwriting the project's existing robot/mode/difficulty handoff.
- Master volume is applied through `AudioListener.volume`; SFX volume is applied once to each robot audio voice, preserving spatial rolloff and `PlayOneShot` slot levels. A music volume preference is saved and exposed through the settings service's `MusicVolume` property and `MusicVolumeChanged` event, but the project currently has no music source or AudioMixer, so it has no playback effect until a music source is added.
- The shared settings panel populates resolutions from the current display with duplicate dimensions removed, and quality choices from `QualitySettings.names`. It applies fullscreen/resolution, quality, VSync, and 30/60/120/144/unlimited frame caps immediately. Settings are saved on leaving the panel and on application quit. Resetting settings is confirmed and does not clear career data.
- A completed result updates matches, wins/losses/draws, player eliminations, player damage dealt, highest reached Survival wave, and active match duration once from the existing match manager. Abandoned matches do not update career stats; paused time is excluded by the manager's existing active-play timer. Career reset requires confirmation and leaves settings intact.
- Default XP rewards are 100 for completion, 50 for a victory, 20 per verified player elimination, and 10 per wave actually cleared in Survival. Reward values can be adjusted on the match manager in the Inspector. Level-up cost is `100 + 50 * (level - 1)` XP; no combat stats are affected. The main-menu profile and results panel show level and current/next-level XP progress. The preferred robot is displayed as the profile frame; no cosmetic unlocks are claimed.
- Robot selection and game mode remain owned by the existing session/preferences flow; they are not duplicated as competing settings in the JSON file.

### Validation status and testing

- `dotnet build Assembly-CSharp.csproj --no-restore` — **succeeded, 0 errors, 0 warnings**.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore` — **succeeded, 0 errors**, with the existing obsolete-API warning in `RoboClashArenaGenerator.cs`.
- VS Code Problems reported no errors in all modified C# files. The Unity Editor log shows a successful prior assembly compile, but it predates the final Phase 7 edits; the current C# sources were validated by the two `dotnet build` commands.
- No Play Mode test was run. Audio attenuation, display mode changes, save/migration/reset behavior, career awards, UI layout, and pause-menu interaction remain to be verified in Unity.

### Unity test checklist

1. Open the main-menu scene and confirm the shared settings panel shows actual display resolutions and quality levels. Test master and SFX playback sliders; verify that music volume remains a stored preference only until a music source exists.
2. Change resolution, fullscreen, quality, VSync, and frame cap; leave settings and reopen them. Restart the application and confirm the chosen values are restored.
3. In a paused match, change an audio/display setting and return to the pause menu. Confirm the match remains paused and that settings persist after resume/restart.
4. Complete a match and verify exactly one career update, correct result counters, XP, level progress, and result-panel details. Repeat with a draw, a loss, player eliminations, and Survival.
5. Restart a match and return to the main menu; verify career statistics remain. Reset settings and confirm career data remains; reset career only after the confirmation prompt and verify settings remain.
6. Inspect the save under `Application.persistentDataPath`; test missing and deliberately malformed save handling on a disposable test profile, not the user's live save. Check the Unity Console for save errors and UI exceptions.

## 19. Phase 8 release-readiness audit and preparation

### Audit findings

- The project contains Unity `6000.6.4f1`, URP `17.6.0`, four generated robot prefabs, and an existing detailed arena in [Scene.unity](./Assets/Scene.unity). Existing source assets were inspected and preserved.
- The enabled Build Settings scene, [Arena.unity](./Assets/Scenes/Arena.unity), is a nearly empty template scene, not the detailed arena. Before this phase there was no gameplay-connected main-menu scene; the detailed arena scene did not have a serialized match manager/HUD. This prevented a reliable build from launching a playable game.
- The existing match manager, runtime menu/selection flow, robot roster, combat, AI, game modes, HUD, audio fallback, and phase-7 save/settings code were retained; no second authority or replacement game loop was introduced.
- No Git repository is present at the workspace root. No Play Mode, Windows build, or executable smoke test has been performed.

### Release preparation files

| File | Change |
|---|---|
| [RoboClashReleaseValidator.cs](./Assets/Editor/RoboClashReleaseValidator.cs) | Adds read-only `Tools > RoboClash > Validate Release`, confirmation-based copying of the existing arena to a new gameplay scene with the existing match manager/HUD and generated robot prefabs, and a guarded Windows x64 build command. |
| [RoboClashReleaseValidator.cs.meta](./Assets/Editor/RoboClashReleaseValidator.cs.meta) | Adds the Unity Editor script asset metadata. |
| [RoboClashMenuSceneGenerator.cs](./Assets/Editor/RoboClashMenuSceneGenerator.cs) | Points menu validation at the prepared gameplay scene and places the new main menu before gameplay in Build Settings; the menu becomes the startup scene after the user confirms generation. Existing scenes are preserved, and the obsolete empty template Arena scene is disabled in Build Settings. |
| [RoboClashMenuController.cs](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) | Uses the prepared `RoboClashGameplay` scene for match launch; this earlier phase-8 edit is now aligned with the Editor tools. |

Scene preparation targets the new `Assets/Scenes/RoboClashGameplay.unity` path and refuses to overwrite it. It copies `Assets/Scene.unity`, checks the source for an arena root, floor, four spawn zones, and an active MainCamera, adds or configures the existing match manager and HUD on the copied arena root, and validates the four generated robot prefabs. The original arena and the empty template scene are not rewritten.

The menu generator still refuses to overwrite an existing menu scene. The Windows build command checks the menu/gameplay scene assets and contents, build-scene order, robot prefab references, Windows target, and template identity values before building to a unique `Builds/Windows/RoboClash-<timestamp>.exe` path. It does not delete or overwrite previous builds. If a target switch is needed, it stops and asks to be run again after Unity reloads.

### Validation status and remaining work

- Runtime assembly compilation succeeded with **0 errors and 0 warnings**.
- Editor assembly compilation succeeded with **0 errors**. The new validator's obsolete-API warnings were corrected; one existing warning remains in [RoboClashArenaGenerator.cs](./Assets/Editor/RoboClashArenaGenerator.cs) for `FindFirstObjectByType`.
- The Unity 6.6 Editor log contains a successful compilation earlier in this phase, but its latest recorded compile failed on the deprecated `SerializedProperty.objectReferenceInstanceIDValue` API. That call was replaced with `objectReferenceEntityIdValue`; the final generated runtime and Editor assemblies then built successfully, but the already-running Editor has not yet logged a post-fix compile. Do not treat the stale failed Editor log entry as cleared until Unity recompiles.
- The generated Unity project files were temporarily amended only to include the newly added source files for these CLI compilation checks, then restored. Unity-generated project files are not intended as persistent source changes.
- The release validator, scene-copy command, and menu generator have **not** been executed in the Unity UI. No new gameplay/menu scenes or executable were generated.
- No Play Mode or Windows executable tests were run. Main-menu rendering/input, scene transitions, player control, combat, AI, hazards, pause/results/restart, save persistence, and repeated-restart object counts remain unverified.
- Shipping identity is still incomplete: `DefaultCompany` and the template Standalone application identifier must be replaced with the owner's chosen publisher/company and reverse-domain identifier. The release validator reports these as warnings; no identity was invented.

### Unity actions to finish release preparation

1. In the existing Unity Editor, wait for script compilation and confirm there are no current Console errors. Then run **Tools > RoboClash > Prepare Gameplay Scene From Existing Arena** and accept the confirmation. Confirm it creates `Assets/Scenes/RoboClashGameplay.unity` while leaving `Assets/Scene.unity` untouched.
2. Run **Tools > RoboClash > Create Main Menu UI** and accept. The menu generator requires the newly prepared gameplay scene and makes the menu scene first in Build Settings.
3. Open **Project Settings > Player** and set the real company name and Standalone application identifier. Do not ship the Unity template identifier.
4. Run **Tools > RoboClash > Validate Release** and fix every reported error. Review any warnings, including the release identity warnings.
5. Test the menu selection, start/countdown, robot movement/jump/aim/fire/reload, damage/shields, robot abilities, AI, arena hazards, pause/resume, results, restart, return to menu, and repeat restarts in Play Mode. Test Free-for-All, Duel, and Survival individually.
6. Run **Tools > RoboClash > Build Windows**. If Unity switches targets, wait for its reload and run the command again. Confirm the reported `.exe` and matching `_Data` folder exist under `Builds/Windows/`; then launch and smoke-test the executable.

### Play Mode startup fix and controls (2026-10-24)

- The first gameplay-scene Play Mode attempt failed with `ArgumentNullException` in `DeactivateExistingRobotInstances`: the match manager called `Transform.IsChildOf(participantRoot)` before creating `participantRoot`. This also left the pre-existing roster preview robots visible and prevented the real match participants from spawning. The participant container is now created before that cleanup step.
- Runtime assembly compilation and an in-Unity Play Mode retry are still required to verify this correction in the user's Editor. If the roster previews appeared bright pink because startup stopped before cleanup, they should be hidden during successful match initialization; confirm the spawned prefab colors in Play Mode after recompilation.
- Current controls: **W/A/S/D** move, **Space** jump, **mouse** aim, **left mouse button** fire (click in the Game view to lock the cursor), **R** reload, **Q** robot ability, **E** ultimate, and **Escape** pause/resume and release the cursor. Escape behavior depends on the match state; press it again to resume.

## 20. Large multi-room arena expansion

### Implementation

- Extended the existing [RoboClashArenaGenerator.cs](./Assets/Editor/RoboClashArenaGenerator.cs); no second generator or gameplay system was added. The original **Tools > RoboClash > Generate Arena** command remains available.
- Added **Tools > RoboClash > Generate Large Arena**, which opens a settings window for map dimensions, corridor width, central arena size, wall dimensions, and optional rooms, cover, hazards, elevated areas, and pickups. The default map is **120 x 120 Unity units**.
- The default layout includes a central combat hub, four themed rooms (Armory, Hazard Chamber, Workshop, and Power Core), four corridor approaches and a perimeter loop, room connectors, four corner spawn zones, cover, existing-system hazards and pickups, and a low workshop catwalk with ramps. Existing robot prefabs, match setup, controls, damage, AI, and pickup systems are reused.
- Generation replaces only the existing generated arena root after confirmation; unrelated scene objects are preserved. The ring-wall spans were adjusted so the rail openings align with the connecting lanes without adding separate overlapping entrance-wall colliders.

### Validation and remaining Unity checks

- `dotnet build Assembly-CSharp.csproj --no-restore` — **succeeded, 0 warnings, 0 errors**.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore` — **succeeded, 0 warnings, 0 errors**. These current results supersede the earlier report of an obsolete API warning in the generator.
- The generator has **not** been run in the Unity Editor, and the generated scene has not been visually inspected. No Play Mode tests were performed; movement, AI pathing, ramps, hazards, pickups, and combat remain to be verified in Unity.

### Unity actions

1. Stop Play Mode and open the gameplay scene in Unity.
2. Choose **Tools > RoboClash > Generate Large Arena**, review the settings, and generate. Confirm replacement of the existing generated arena when prompted.
3. Save the scene and run **Tools > RoboClash > Validate Scene Setup**.
4. In Play Mode, check all four spawns; walk the loop and each room connector; test the workshop ramps, hazards, pickups, combat, and AI. Regenerate once to confirm that only one generated arena root remains.

## 21. Arcade match modes

### Implemented and integrated

- Extended the existing [RoboClashMatchManager.cs](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs); retained its lifecycle, verified damage/death events, AI spawning, Survival wave loop, pause flow, match results, and progression integration. Existing enum values for Free-for-All, Duel, and Survival were preserved for saved-data compatibility.
- Added Kill Race (5/10/15/20 target), Timed Deathmatch (3/5/7 minutes), Duel (kill-target or timed, 2 combatants, optional respawns), Survival Waves (existing progression and wave loop retained), King of the Arena (visible trigger zone; only an uncontested participant scores one point per second), and Knockout (1/3/5 lives).
- Kill and death scoring is per participant and comes from the existing elimination event. Only a different, registered participant that caused the final credited damage earns a kill; suicides and uncredited/environmental deaths do not. A per-death guard prevents duplicate scoring, and end-of-frame evaluation resolves simultaneous target wins deterministically.
- Timed Deathmatch and King of the Arena award a draw when the player ties for the lead at time-out. Pause uses the existing zero-timescale state, so the timer stops. Match-ending paths disable combat/AI and cancel projectiles.
- Extended the existing session and generated menu with the mode/rule selection. The existing HUD now shows mode objectives, live scores, wave status, capture status, or Knockout lives; results include participant kill/death rows.
- Added [KingOfArenaZone.cs](./Assets/Scripts/RoboClash/KingOfArenaZone.cs) for trigger occupancy tracking, and extended [RobotArenaController.cs](./Assets/Scripts/RoboClash/RobotArenaController.cs) with unlimited match respawns plus a respawn event used to reset the manager's per-death guard.

### Validation and remaining Unity checks

- `dotnet build Assembly-CSharp.csproj --no-restore` — **succeeded, 0 warnings, 0 errors**.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore` — **succeeded, 0 warnings, 0 errors**.
- The open Unity 6.6 Editor log records a successful script compilation and assembly reload after the session syntax correction. The final HUD label adjustment was made afterward; the final source was validated by both zero-warning `dotnet` builds, but Unity has not yet recompiled that last adjustment.
- No Unity Play Mode test was performed. Kill attribution, target completion, exact timed endings and pause behavior, respawn timing, King trigger occupancy/contesting, Knockout simultaneous deaths, AI behavior, menu layout, and results buttons still require verification in the Unity Editor.

### Unity test steps

1. Let Unity finish compiling, open the main menu scene, choose **PLAY**, select a robot, and use the **GAME MODE** dropdown. Use `+`/`-` for 2–4 total combatants (Survival uses this control for starting enemies).
2. Choose the displayed mode rule: Kill Race target; Timed Deathmatch duration; Duel kill target or duration plus respawn toggle; Survival growth and break; King duration plus respawn toggle; or Knockout lives.
3. Start each mode and verify its HUD objective and results. For Kill Race/Deathmatch, test several respawns and confirm only player-caused eliminations score. For timed modes, pause/resume and verify the countdown. For King, stand alone in the marked zone, then contest it with an AI. For Knockout, verify lives decrement and simultaneous final eliminations draw.
4. Use Play Again and Main Menu after results, then start a different mode to check that scores, lives, timers, and wave routines reset.

## 22. In-game combat HUD and minimap (2026-10-25)

### Implementation

- Extended the existing [RoboClashHUD.cs](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) instead of introducing a second gameplay HUD or duplicating combat systems. The existing `PlayerRegistered` and player component events continue to bind and unbind the live player through match starts, robot changes, deaths, respawns, and restarts.
- Added [RoboClashMinimap.cs](./Assets/Scripts/RoboClash/UI/RoboClashMinimap.cs), which derives map bounds from the generated arena floor colliders and draws the actual non-player arena colliders as a compact top-down layout. It skips damageable robots and trigger volumes; its player marker follows the registered player's world position and body yaw and hides while that robot is dead.
- Expanded the existing two ability rows into exactly two readable slots. Ability cooldown and active-duration countdowns, ultimate charge/cooldown and configured active-duration countdowns, and the actual robot ability names come from the current definition and `RoboClashRobotAbilities`.
- The player panel now uses the robot's configured accent color, health and shield bars animate toward event-updated values, ammunition includes the current weapon name, and reload progress refreshes while reloading.
- Added **Tools > RoboClash > Create Gameplay HUD** as an alias for the existing no-duplicate HUD generator. The existing **Create Gameplay UI** command remains available.
- No world-space health bar component or prefab was found in the robot runtime scripts or generated robot prefabs. The existing screen-space player HUD remains the sole player vitals display; no AI health display was removed.

### Validation and remaining Unity checks

- Runtime and Editor assemblies compiled successfully with **0 warnings and 0 errors** using the project's generated Unity references and a temporary MSBuild include for the new minimap source (the already-open Unity Editor had not refreshed its generated `.csproj`).
- The Unity batch compiler could not start a second Editor instance because the main project is already open in Unity. Play Mode, scene rendering/layout, minimap position and rotation, combat updates, pause/results behavior, and AI displays have **not** been tested in Play Mode.
- The player marker and map geometry use runtime scene transforms/colliders, but their visual clarity and aspect-fit should be checked in the Unity Game view at 16:9 and ultrawide resolutions.

### Unity test steps

1. Let the open Editor import the new minimap script and finish compiling. Open `Assets/Scenes/RoboClashGameplay.unity` and enter Play Mode.
2. Check that the bottom status panel uses the selected robot's real name, health, shield, and accent color. Take damage, use shield/repair effects, and respawn; confirm values update without retaining data from an old robot.
3. Fire and reload with each robot. Verify the weapon name, magazine count, and reload progress against the actual combat behavior.
4. Use Q and E to check each robot's ability and ultimate names, ready/cooldown/active states, and ultimate charge behavior.
5. Move and rotate the player around both the standard and expanded arenas. Confirm the minimap marker translates and rotates, and that walls/obstacles are represented without AI markers.
6. Confirm exactly two ability slots, no player overhead health bar, and no regression to pause/results overlays. No Play Mode or physical-device claim is made for these checks.

## 23. Main menu, match setup, robot garage, and settings (2026-10-25)

### Implementation

- Redesigned the existing [RoboClashMenuController.cs](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) and its runtime Canvas in the existing `RoboClashMainMenu` scene. No second menu scene, Canvas, EventSystem, settings manager, match manager, or mode system was added.
- Added home actions for Play, Robot Garage, Settings, Career/Statistics, and Quit. The Editor Quit action only logs a message; it does not stop Play Mode.
- Implemented the navigation flow **Home → Game Mode and Rules → Robot Garage → Match Summary → Start Match**, with contextual back navigation and Escape support. The six cards map to the existing `RoboClashGameMode` values and manager behavior: Kill Race, Timed Deathmatch, Duel, Survival Waves, King of the Arena, and Knockout. Their supported targets, durations, lives, opponent counts, AI difficulty, respawn choice, and Survival wave settings are surfaced only where applicable.
- Kept match handoff in the existing [RoboClashSession.cs](./Assets/Scripts/RoboClash/UI/RoboClashSession.cs) and `RoboClashMatchManager`; the selected robot, mode, and rules are passed through `BeginMatch` before loading the existing `RoboClashGameplay` scene. The summary names the existing procedural arena rather than advertising nonexistent maps.
- Added an isolated RenderTexture preview of the currently selected existing prefab. Its gameplay behaviours, physics, cameras, listeners, and audio are disabled; its render layer is excluded from the menu scene's other cameras. One preview instance is reused and the render texture and camera masks are cleaned up with the menu.
- The garage displays the selected prefab's configured health, shield, speed, weapon damage/rate/range, magazine, reload, ability names/effects, and cooldowns. Selection is saved through the existing session and preference flow, so the match manager spawns the selected robot.
- Added a saved career/statistics screen backed by `RoboClashSaveService`, including matches, wins/losses/draws, eliminations, damage, best Survival wave, playtime, XP, and saved preferred robot. Death totals and actual per-robot usage counts are not tracked by the existing save model and are not fabricated.
- Reused the existing settings panel and save service. Master/SFX, display resolution, fullscreen, quality, VSync, FPS cap, AI difficulty, reset confirmation, and persistence use their existing APIs. Resolution changes now require confirmation and revert after ten seconds or on Cancel. Music volume is visibly unavailable and disabled because no music source is assigned. The settings panel lists the actual keyboard/mouse controls.

### Validation and remaining Unity checks

- The `RoboClashMainMenu` scene already contains a single `RoboClashMenuController` and a scene camera; the controller creates its runtime Canvas and only creates an EventSystem if the scene has none.
- `dotnet restore Assembly-CSharp-Editor.csproj` completed successfully for the generated Unity project files.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore` compiled both runtime and Editor assemblies successfully with **0 warnings and 0 errors** after correcting a menu UI call signature.
- The Editor was not used to visually inspect the rebuilt menu, and no Play Mode tests were performed. Menu layout at varied aspect ratios, robot preview rendering, button navigation, match starts for each mode, settings persistence across an application restart, pause/results return paths, and duplicate-object behavior still require Unity Play Mode verification.

### Unity test steps

1. Let Unity 6.6 finish compiling. Open `Assets/Scenes/RoboClashMainMenu.unity` and enter Play Mode to view the runtime-generated menu.
2. Test each home button and Escape/back navigation. Open Robot Garage and select VOLT, TITAN, SHADOW, and SPARK; verify each preview is static-safe, correctly colored, and shows its prefab configuration.
3. Open Play. Select each of the six mode cards and confirm the relevant rule controls and summary update. For example, test kill targets, timed durations, Duel's kill/time and respawn choices, Survival growth/break settings, King zone duration/respawns, and Knockout lives.
4. Start representative matches and verify the existing match manager spawns the chosen robot and applies the selected mode/rules. Then test each mode's match behavior, pause, results, and return-to-menu buttons.
5. Change audio/display settings, confirm a resolution change and test its timeout/revert, exit the settings page, then restart the standalone build to verify persistence. Confirm the music slider remains unavailable until a music source is assigned.
6. Check the Game view at 16:9 and a wide aspect ratio. Confirm the runtime menu has one Canvas/EventSystem and the preview camera does not reveal an extra active combat robot or duplicate audio listener.
