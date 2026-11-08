# RoboClash Third-Person Aiming Report

## Overview

Third-person aiming is driven by the view, independently of the robot's body facing:

- Look input changes the camera aim yaw and pitch.
- The camera smoothly orbits behind the robot's right shoulder using that aim rotation.
- The third-person reticle is positioned slightly above screen center so it clears the robot; shots use that exact reticle position.
- The weapon/aim pivot rotates to match the camera aim relative to the body.
- Movement is relative to the camera yaw. The robot's body turns toward the aim yaw while moving or firing, and catches up after a large idle turn.
- Firing uses the center of the camera view to choose an aim point, then launches from the weapon muzzle toward that point.

The report describes the current code. In Unity, select the **Third Person** camera mode to use this behavior.

## Input and aim angles

`RoboClashRobotMovement.Update()` reads look input and updates the aim yaw and pitch. The input source can be keyboard/mouse or phone input; phone gyro and phone joystick aim are applied through the same yaw and pitch values.

```csharp
var thirdPerson = cameraMode == RoboClashCameraMode.ThirdPerson;
var look = inputSource.LookDelta;
yaw += look.x;
pitch = Mathf.Clamp(pitch - look.y,
    thirdPerson ? -30f : -20f, thirdPerson ? 60f : 45f);
if (inputSource is KeyboardMouseInputSource phoneInput)
{
    phoneInput.ConsumePhoneAimRecenter();
    phoneInput.ApplyPhoneAim(ref yaw, ref pitch, phoneYawScale, phonePitchScale);
    yaw += phoneInput.PhoneJoystickTurnInput * phoneJoystickTurnSpeed * Time.deltaTime;
}
```

Movement is relative to camera yaw. In third person, the body follows the aim yaw while moving or firing and catches up while idle after the aim/body angle exceeds the configured threshold. The aim pivot is clamped to a 90-degree yaw offset from the body.

```csharp
var thirdPerson = cameraMode == RoboClashCameraMode.ThirdPerson;
var desired = Quaternion.Euler(0f, yaw, 0f) * moveInput *
              definition.stats.moveSpeed * speedMultiplier * SpeedMultiplier;
ApplyMovement(desired, jumpPressed);

if (thirdPerson)
{
    var horizontalDesired = new Vector3(desired.x, 0f, desired.z);
    var moving = horizontalDesired.sqrMagnitude > 0.01f;
    var firing = inputSource != null && inputSource.FireHeld;
    var offsetToAim = Mathf.DeltaAngle(transform.eulerAngles.y, yaw);
    if (bodyFollowsAim)
    {
        var shouldTurn = moving || firing ||
                         Mathf.Abs(offsetToAim) > idleTurnThresholdDeg;
        if (shouldTurn)
        {
            var turnSpeed = firing
                ? Mathf.Max(firingTurnSpeed, definition.stats.rotationSpeed)
                : definition.stats.rotationSpeed;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.Euler(0f, yaw, 0f), turnSpeed * Time.deltaTime);
        }
    }
    else if (moving)
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(horizontalDesired.normalized, Vector3.up),
            definition.stats.rotationSpeed * Time.deltaTime);
}
else
{
    transform.rotation = Quaternion.Euler(0f, yaw, 0f);
}

if (definition.aimPivot != null)
{
    var aimYawOffset = cameraMode == RoboClashCameraMode.ThirdPerson
        ? Mathf.Clamp(Mathf.DeltaAngle(transform.eulerAngles.y, yaw), -90f, 90f)
        : 0f;
    definition.aimPivot.localRotation = Quaternion.Euler(pitch, aimYawOffset, 0f);
}
```

The body-follow and camera smoothing defaults are serialized on `RoboClashRobotMovement`.

The current aim direction is also exposed as a world-space direction:

```csharp
public Vector3 AimDirection => Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
```

## Third-person camera placement

Camera placement runs in `LateUpdate`, after character movement. First person keeps its existing anchor placement. Third person uses a focus point 1.6 m above the robot and a camera-local offset built from the existing fields: 0.8 m right, 0.3 m up, and 4.5 m back. It smooths both focus and camera position and uses a sphere cast to keep the camera out of walls.

```csharp
smoothedFocus = Vector3.SmoothDamp(smoothedFocus, rawFocus, ref focusVelocity,
    Mathf.Max(0.001f, focusSmoothTime));
var desiredPosition = GetThirdPersonCameraPosition(smoothedFocus, rotation);
var resolved = ResolveCameraPosition(smoothedFocus, desiredPosition, out _);
var direction = desiredPosition - smoothedFocus;
var targetPosition = direction.sqrMagnitude > 0.0001f
    ? smoothedFocus + direction.normalized * currentCameraDistance
    : desiredPosition;
var wantedDistance = Vector3.Distance(smoothedFocus, resolved);
var speed = wantedDistance < currentCameraDistance
    ? cameraCollisionInSpeed
    : cameraCollisionOutSpeed;
currentCameraDistance = Mathf.MoveTowards(currentCameraDistance, wantedDistance,
    Mathf.Max(0f, speed) * Time.deltaTime);
var position = Vector3.SmoothDamp(playerCamera.transform.position, targetPosition,
    ref cameraPositionVelocity, Mathf.Max(0.001f, focusSmoothTime));
position = ResolveCameraPosition(smoothedFocus, position, out _);
```

Third-person aim uses a ray through the reticle, slightly above screen center by default (`thirdPersonAimViewportHeight = 0.62`). First-person remains centered:

```csharp
var ray = playerCamera.ViewportPointToRay(AimViewportPoint);
var aimPoint = ray.origin + ray.direction * maxDistance;
foreach (var hit in Physics.RaycastAll(ray, maxDistance,
             Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
{
    if (hit.collider == null || hit.transform.root == transform.root ||
        hit.collider.GetComponentInParent<ArenaProjectile>() != null)
        continue;
    aimPoint = hit.point;
}
```

## Firing and aim point

`RoboClashRobotCombat.Fire()` keeps the first-person ray path unchanged. In third person, it finds the point under the reticle (or a point 100 m ahead), then directs the projectile from the muzzle toward it. The ray ignores the firing robot, projectiles, and trigger volumes. If the point is too close or behind the muzzle, it falls back to the camera-forward direction.

```csharp
var playerCamera = movement.PlayerCamera;
var aimDirection = playerCamera != null
    ? playerCamera.transform.forward
    : movement.AimDirection;
var aimPoint = movement.GetCameraAimPoint(100f);

var muzzlePosition = definition.muzzle.position;
var toTarget = aimPoint - muzzlePosition;
var targetTooClose = toTarget.sqrMagnitude < 4f ||
                     Vector3.Dot(toTarget, aimDirection) < 0f;
var shotDirection = targetTooClose ? aimDirection : toTarget.normalized;
var targetPoint = targetTooClose
    ? muzzlePosition + aimDirection * definition.stats.weaponRange
    : aimPoint;
return FireTowardPoint(targetPoint, shotDirection);
```

Rail Cannon and the player-controlled third-person Shadow Strike projectile also use the center-screen aim point, while projectiles still originate at the weapon muzzle. Volt dash uses camera forward as its horizontal dash direction. Shadow Strike retains its target-seeking teleport selection.

## Controls and practical notes

- Aim with the mouse while the cursor is captured; phone gyro aiming is supported through the phone input source.
- Move with the movement controls. In third person, movement is camera-relative; the body follows aim while moving or firing, or catches up once the idle aim offset passes the threshold. The aim pivot rotates with pitch and follows the camera yaw relative to the body.
- Firing follows the camera view, not just the body's forward direction.
- If mouse look does not respond, click the gameplay view to capture the cursor. Press Escape to release it.

## Source files

- [RoboClashRobotMovement.cs](Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) — camera aim, movement/body rotation, and aim-pivot rotation.
- [RoboClashRobotCombat.cs](Assets/Scripts/RoboClash/RoboClashRobotCombat.cs) — camera ray targeting and projectile aim point.
- [KeyboardMouseInputSource.cs](Assets/Scripts/RoboClash/Input/KeyboardMouseInputSource.cs) — mouse and phone input.
- [RoboClashRobotAbilities.cs](Assets/Scripts/RoboClash/RoboClashRobotAbilities.cs) — camera-forward aim for abilities.
