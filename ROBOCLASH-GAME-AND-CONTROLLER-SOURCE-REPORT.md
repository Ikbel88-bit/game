# RoboClash Game and Phone Controller — Design and C# Source Report

## What this report covers

This report describes the RoboClash game and its companion Unity phone-controller app: the arena match loop, robots, movement, weapons, abilities, match types, HUD, controller inputs, gyro aiming, and Wi-Fi connection. Code excerpts are representative rather than copies of entire classes; each section links to the full C# source.

The projects are separate Unity projects:

- Game: `RoboClash`
- Phone controller: `RoboClashController`

## Game overview

RoboClash is a 3D arena robot-combat game. Matches use a generated player/AI participant roster, arena spawn zones, robot-specific stats and abilities, projectiles, health and shields, and a match manager that runs countdown, play, pause, elimination, and results states. The current gameplay scene is the XL arena; both gameplay scene paths are recognized by `RoboClashSceneUtility`.

The match state and game-mode types are declared in [RoboClashMatchManager.cs](Assets/Scripts/RoboClash/RoboClashMatchManager.cs):

```csharp
public enum RoboClashMatchState
{
    Initializing, Ready, Countdown, Playing, Paused, Victory, Defeat, Draw
}

public enum RoboClashGameMode
{
    FreeForAll, Duel, Survival, KillRace,
    TimedDeathmatch, KingOfTheArena, Knockout
}
```

The manager loads a selected robot and match setup from the session, initializes participants, starts a countdown, and advances the match while it is in the Playing state. It records statistics and saves results on match completion.

## Robots and identity

Four robot types are configured in [RoboClashRobotDefinition.cs](Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs):

| Robot | Role cues in its stats and kit | Weapon | Ability | Ultimate |
|---|---|---|---|---|
| Volt | Fast, balanced, gains movement-related reload/fire benefits and Overdrive | Pulse Rifle | Dash | Overdrive |
| Titan | High health/shield, slower, sturdy, damage reduction and knockback resistance | Gatling Cannon | Defensive Field | Rail Cannon |
| Shadow | Fast and lightly armored, stealth-focused | Energy SMG | Cloak | Shadow Strike |
| Spark | Support-focused, regenerates nearby teammates and uses explosive/EMP effects | Rocket Launcher | Repair Drone | EMP Blast |

Robot definitions hold stats, appearance colors/materials, weapon and camera anchors, and ability names/timing. For example, the definition configures Volt’s identity and weapon tuning in one place:

```csharp
case RoboClashRobotType.Volt:
    SetStats(100f, 50f, 7f, 18f, 4f, 1.5f, 20f, 20, 1f);
    primaryColor = Hex("147DFF");
    secondaryColor = Hex("EAF4FF");
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
```

The complete robot factory/configuration and all per-type values are in [RoboClashRobotDefinition.cs](Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs) and [RoboClashRobotFactory.cs](Assets/Scripts/RoboClash/RoboClashRobotFactory.cs).

## Movement, aiming, and cameras

Player and AI robot motion is coordinated by [RoboClashRobotMovement.cs](Assets/Scripts/RoboClash/RoboClashRobotMovement.cs). It applies movement through a `CharacterController`, turns the robot, updates the aim pivot, and manages first-person/third-person camera positioning and obstruction handling.

The phone input path uses the joystick for movement and strafing, with horizontal stick input also rotating the robot. Gyro angle deltas update yaw and pitch without multiplying the already-accumulated angle by frame time:

```csharp
if (phoneInputActive)
{
    var phoneInput = phoneServer.CurrentInput;
    phoneServer.ConsumeAimDelta(out var dy, out var dp);
    if (phoneServer.HasRecentGyroAimDeltas)
    {
        yaw += dy * phoneYawScale;
        pitch = Mathf.Clamp(pitch - dp * phonePitchScale, -20f, 45f);
    }

    input.x = phoneServer.CurrentInput.MoveHorizontal;
    input.y = phoneServer.CurrentInput.MoveForward;
    yaw += input.x * phoneJoystickTurnSpeed * Time.deltaTime;
}
```

PC keyboard/mouse input remains supported in the same movement component. Camera mode is persisted by [RoboClashSession.cs](Assets/Scripts/RoboClash/UI/RoboClashSession.cs).

## Weapons, damage, and combat

[RoboClashRobotCombat.cs](Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) handles firing, ammunition, reload timing, recoil, muzzle flash, projectile ownership, and damage buffs. Player firing projects a ray through the center of the camera view to find the aim point, then fires toward that location:

```csharp
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
```

Shared damage, health, shields, and elimination events are handled by [ArenaDamageable.cs](Assets/Scripts/RoboClash/ArenaDamageable.cs). Projectiles and their visuals are implemented in [ArenaProjectile.cs](Assets/Scripts/RoboClash/ArenaProjectile.cs) and [RoboClashProjectileVisuals.cs](Assets/Scripts/RoboClash/RoboClashProjectileVisuals.cs).

## Abilities and ultimates

[RoboClashRobotAbilities.cs](Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) enforces cooldowns, charge requirements, and locks, then dispatches the ability or ultimate to the robot-specific effect. Some representative behaviors:

- Volt dashes along its camera/forward direction; Overdrive increases movement and fire rate.
- Titan activates a defensive damage-reduction field; its Rail Cannon charges before firing.
- Shadow cloaks; Shadow Strike is its ultimate attack.
- Spark pulses repair support for allies; EMP Blast locks/slows opponents.

Activation dispatch is driven by the configured robot type:

```csharp
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
```

Volt’s dash is handled immediately before this switch. Ultimate dispatch and the full effect coroutines are in [RoboClashRobotAbilities.cs](Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs); their visuals/audio/animation hooks are in [RoboClashRobotVFX.cs](Assets/Scripts/RoboClash/RoboClashRobotVFX.cs), [RoboClashRobotAudio.cs](Assets/Scripts/RoboClash/RoboClashRobotAudio.cs), and [RoboClashRobotAnimator.cs](Assets/Scripts/RoboClash/RoboClashRobotAnimator.cs).

## Game modes

The current pre-game menu presents six modes:

1. **Kill Race** — first to the selected 5/10/15/20 eliminations.
2. **Timed Deathmatch** — most eliminations after 3/5/7 minutes.
3. **Duel** — one AI opponent; win by a selected kill target or timer, with optional respawns.
4. **Survival Waves** — defeat successive waves that grow by a configurable amount; survive as long as possible.
5. **King of the Arena** — gain control time by being the sole participant in the center zone; highest score wins at timeout.
6. **Knockout** — last participant with remaining lives wins; life choices include 1, 3, or 5.

The `FreeForAll` enum value is also implemented internally but is not a separate card in the current menu. Full descriptions and additional match-rule excerpts are in [ROBOCLASH-GAME-MODES-AND-SOURCE-REPORT.md](ROBOCLASH-GAME-MODES-AND-SOURCE-REPORT.md). Match setup, scoring, respawns, waves, and completion logic are in [RoboClashMatchManager.cs](Assets/Scripts/RoboClash/RoboClashMatchManager.cs).

## Arena and HUD

The arena is assembled from floor, wall, spawn-zone, hazard, moving-platform, pickup, and decoration components. Relevant sources include [ArenaFloor.cs](Assets/Scripts/RoboClash/ArenaFloor.cs), [ArenaWall.cs](Assets/Scripts/RoboClash/ArenaWall.cs), [ArenaSpawnZone.cs](Assets/Scripts/RoboClash/ArenaSpawnZone.cs), [ArenaHazard.cs](Assets/Scripts/RoboClash/ArenaHazard.cs), [ArenaMovingPlatform.cs](Assets/Scripts/RoboClash/ArenaMovingPlatform.cs), and [RoboClashArenaVisuals.cs](Assets/Scripts/RoboClash/Visuals/RoboClashArenaVisuals.cs).

The runtime HUD displays vitals, timer/mode status, abilities, kill information, results, and the gameplay minimap. The primary HUD and its mode/status handling live in [RoboClashHUD.cs](Assets/Scripts/RoboClash/UI/RoboClashHUD.cs); the newer runtime interface and arena minimap are built by [RoboClashHudV2.cs](Assets/Scripts/RoboClash/UI/RoboClashHudV2.cs) and [RoboClashMinimapV2.cs](Assets/Scripts/RoboClash/UI/RoboClashMinimapV2.cs). Menus and match configuration are in [RoboClashMenuController.cs](Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs); shared UI construction is in [RoboClashUiKit.cs](Assets/Scripts/RoboClash/UI/RoboClashUiKit.cs).

## Phone controller overview

The controller is a separate Unity touchscreen app. The player enters the game PC’s local IPv4 address and connects over the same Wi-Fi/LAN. It is a TCP client; the game listens on port `17862`, negotiates protocol version 1, exchanges newline-delimited JSON input messages, and acknowledges each packet. The PC must permit the game through its firewall.

### Touch layout and actions

The controller UI is constructed in [ControllerDemoScreen.cs](../RoboClashController/Assets/Scripts/ControllerDemoScreen.cs):

- Virtual joystick: up/down moves, left/right strafes and also rotates the robot in the game.
- Gyroscope: primary aiming method for yaw and pitch.
- Touch actions: shoot, jump, ability, ultimate, reload.
- Gyro clutch: while held, stops gyro aim output and clears pending gyro deltas, allowing the phone to be repositioned.
- Recenter: starts the gyro bias calibration.
- Settings: separate yaw/pitch sensitivity, inversion, dead zone, curve, smoothing and joystick dead zone; diagnostics show input state/raw gyro rates.

The joystick clamps the touch offset to a unit circle, applies its dead zone, and returns to neutral on pointer release. The game keeps the robot’s changed heading after the user releases the stick:

```csharp
var offset = Vector2.ClampMagnitude(localPoint / radius, 1f);
SetValue(offset);
knobRect.anchoredPosition = offset * radius * 0.72f;
```

From [ControllerVirtualJoystick.cs](../RoboClashController/Assets/Scripts/ControllerVirtualJoystick.cs).

### Gyroscope processing

[ControllerGyroscope.cs](../RoboClashController/Assets/Scripts/ControllerGyroscope.cs) converts the device’s landscape rotation rates to yaw/pitch, subtracts calibration bias, applies dead zone and response curve, and maintains smoothed compatibility inputs. It also integrates the processed rates into degree deltas so packets preserve rotation that occurred between sends:

```csharp
var processedYawRateDegPerSec =
    ProcessRate(rawRate - calibratedBias, yawSensitivity, invertYaw) *
    FullRateDegreesPerSecond;
var processedPitchRateDegPerSec =
    ProcessRate(rawPitchRate - calibratedPitchBias, pitchSensitivity, invertPitch) *
    FullRateDegreesPerSecond;

lock (aimDeltaLock)
{
    accumulatedYawDeltaDegrees += processedYawRateDegPerSec * Time.unscaledDeltaTime;
    accumulatedPitchDeltaDegrees += processedPitchRateDegPerSec * Time.unscaledDeltaTime;
}
```

The `ConsumeAimDelta`/acknowledgement logic retains deltas until the network packet has been acknowledged. Gyro settings are persisted in `PlayerPrefs` by [ControllerInputState.cs](../RoboClashController/Assets/Scripts/ControllerInputState.cs).

### Controller input and Wi-Fi packet flow

`ControllerInputProvider` combines joystick, gyro, clutch, and action-button state into `ControllerInputState`. `ControllerNetworkClient` transmits the snapshot at approximately 16 ms intervals over TCP.

Representative JSON fields written by [ControllerNetworkClient.cs](../RoboClashController/Assets/Scripts/ControllerNetworkClient.cs):

```csharp
private static string FormatInput(ControllerInputState input, bool jumpPressed,
    bool reloadPressed, bool abilityPressed, bool ultimatePressed)
{
    return string.Format(CultureInfo.InvariantCulture,
    "{{\"type\":\"input\",\"protocol\":1,\"moveForward\":{0:R}," +
    "\"aimHorizontal\":{1:R},\"gyroTurn\":{2:R},\"gyroPitch\":{3:R}," +
    "\"gyroYawDelta\":{4:R},\"gyroPitchDelta\":{5:R}," +
    "\"shoot\":{6},\"jump\":{7},\"reload\":{8},\"ability\":{9},\"ultimate\":{10}}}",
        FiniteOrZero(input.MoveForward), FiniteOrZero(input.AimHorizontal),
        FiniteOrZero(input.GyroTurn), FiniteOrZero(input.GyroPitch),
        FiniteOrZeroUnclamped(input.GyroYawDelta),
        FiniteOrZeroUnclamped(input.GyroPitchDelta),
        Lower(input.Shoot), Lower(input.Jump || jumpPressed),
        Lower(input.Reload || reloadPressed), Lower(input.Ability || abilityPressed),
        Lower(input.Ultimate || ultimatePressed));
}
```

The game-side [RoboClashPhoneInputServer.cs](Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs) validates the message, clamps movement/rate input, queues button press edges for the Unity main thread, and accumulates gyro angle deltas for `RoboClashRobotMovement`. Newer angle-delta packets are used when recent; older clients can fall back to the rate-based gyro input.

### Controller and game-side source index

**Phone controller**

- [ControllerDemoScreen.cs](../RoboClashController/Assets/Scripts/ControllerDemoScreen.cs) — touchscreen UI, settings, diagnostics, and connection controls.
- [ControllerVirtualJoystick.cs](../RoboClashController/Assets/Scripts/ControllerVirtualJoystick.cs) — touch joystick and dead zone.
- [ControllerActionButton.cs](../RoboClashController/Assets/Scripts/ControllerActionButton.cs) — touch action buttons.
- [ControllerGyroscope.cs](../RoboClashController/Assets/Scripts/ControllerGyroscope.cs) — sensor processing, calibration, clutch, smoothing, and angle accumulation.
- [ControllerInputState.cs](../RoboClashController/Assets/Scripts/ControllerInputState.cs) — input structures and saved controller settings.
- [ControllerInputProvider.cs](../RoboClashController/Assets/Scripts/ControllerInputProvider.cs) — aggregates controller inputs.
- [ControllerNetworkClient.cs](../RoboClashController/Assets/Scripts/ControllerNetworkClient.cs) — TCP client and JSON packet format.

**Game**

- [RoboClashMatchManager.cs](Assets/Scripts/RoboClash/RoboClashMatchManager.cs) — match lifecycle, modes, participants, AI setup, scoring, and results.
- [RoboClashRobotDefinition.cs](Assets/Scripts/RoboClash/RoboClashRobotDefinition.cs) — robot types, stats, identity, weapons, and ability metadata.
- [RoboClashRobotMovement.cs](Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) — movement, aiming, phone controls, and camera.
- [RoboClashRobotCombat.cs](Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) — firing, ammunition, reload, recoil, and projectiles.
- [RoboClashRobotAbilities.cs](Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) — robot abilities and ultimates.
- [ArenaDamageable.cs](Assets/Scripts/RoboClash/ArenaDamageable.cs) — damage, health, shields, and elimination.
- [RoboClashPhoneInputServer.cs](Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs) — PC TCP listener and controller input validation.
- [RoboClashHUD.cs](Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) and [RoboClashHudV2.cs](Assets/Scripts/RoboClash/UI/RoboClashHudV2.cs) — gameplay HUDs.
- [RoboClashSaveService.cs](Assets/Scripts/RoboClash/RoboClashSaveService.cs) — saved career and match statistics.
