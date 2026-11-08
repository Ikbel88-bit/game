# RoboClash Phone Controller: Joystick, Gyroscope, Movement, and Code Report

## Scope and controller behavior

This report follows phone input from touch or phone motion, through the controller app's C# state and Wi-Fi packets, into the game. The controller project is at `C:\Users\moham\RoboClashController`; the game project is at `C:\Users\moham\RoboClash`.

The intended control layout is:

- The on-screen joystick controls robot movement.
- The gyroscope controls camera/weapon aim: turn left/right and aim up/down.
- Touch buttons control Shoot, Jump, Ability, Ultimate, and Reload.
- Controller input travels by TCP over Wi-Fi to the game on port `17862`.

## Important current mapping detail

The joystick exports two axes: vertical as `MoveForward` and horizontal as `AimHorizontal`. Despite that legacy field name and a UI caption that says “LEFT / RIGHT = AIM,” the game currently consumes the horizontal value as **horizontal movement/strafe**. Camera yaw and pitch are driven by gyro fields. In other words, the actual code maps both joystick axes to movement and the gyroscope to aiming.

The relevant game movement code is:

```csharp
var input = Vector2.zero;
if (phoneInputActive)
{
    input.x = phoneServer.CurrentInput.MoveHorizontal;
    input.y = phoneServer.CurrentInput.MoveForward;
}

var moveInput = new Vector3(input.x, 0f, input.y);
if (moveInput.sqrMagnitude > 1f)
    moveInput.Normalize();
var desired = Quaternion.Euler(0f, yaw, 0f) * moveInput *
              definition.stats.moveSpeed * speedMultiplier;
```

The source is [RoboClashRobotMovement.cs](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs). The controller's `AimHorizontal` field is serialized under the JSON key `aimHorizontal`, but the game stores that value as `MoveHorizontal` and uses it for strafing. The gyro—not that joystick field—updates yaw and pitch.

## Touch joystick

### Input capture

[ControllerVirtualJoystick.cs](./RoboClashController/Assets/Scripts/ControllerVirtualJoystick.cs) implements Unity pointer down/drag/up handlers. It accepts one active pointer at a time, converts the screen touch into local joystick coordinates, normalizes and clamps the vector to unit length, applies the radial dead zone, and returns to neutral on release or disable.

```csharp
public float MoveForward => normalizedValue.y;
public float AimHorizontal => normalizedValue.x;

private void UpdatePosition(PointerEventData eventData)
{
    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            baseRect, eventData.position, eventData.pressEventCamera, out var localPoint))
        return;

    var radius = Mathf.Max(1f, Mathf.Min(baseRect.rect.width, baseRect.rect.height) * 0.5f);
    var offset = Vector2.ClampMagnitude(localPoint / radius, 1f);
    SetValue(offset);
    knobRect.anchoredPosition = offset * radius * 0.72f;
}
```

Dead-zone handling is radial: small deflections become zero, and values outside the dead zone are rescaled to the remaining range.

```csharp
private void SetValue(Vector2 value)
{
    value = Vector2.ClampMagnitude(value, 1f);
    if (value.magnitude < deadZone)
        value = Vector2.zero;
    else if (deadZone > 0f)
        value = value.normalized * Mathf.InverseLerp(deadZone, 1f, value.magnitude);

    if ((normalizedValue - value).sqrMagnitude < 0.0001f)
        return;

    normalizedValue = value;
    ValueChanged?.Invoke(normalizedValue);
}
```

The dead zone can be set between `0` and `0.5`; its default is `0.12`, stored in `ControllerLocalSettings.JoystickDeadZone`.

### Meaning in the game

The controller puts the joystick's vertical axis in `moveForward` and horizontal axis in `aimHorizontal`. The game maps these to forward/back movement and left/right strafing, relative to the robot's current yaw. Diagonal movement is normalized so the combined vector does not exceed magnitude one. The joystick does not directly set camera yaw.

## Gyroscope aim

### Two-axis rotational-rate input

[ControllerGyroscope.cs](./RoboClashController/Assets/Scripts/ControllerGyroscope.cs) enables `Input.gyro` when the device supports it and gyroscope input is enabled. It reads `rotationRateUnbiased`, converts yaw and pitch from radians per second to degrees per second, adjusts signs for landscape orientation, then processes each axis independently into normalized values.

```csharp
var rotationRate = Input.gyro.rotationRateUnbiased;
var rawRate = GetLandscapeYawRate(rotationRate) * Mathf.Rad2Deg;
var rawPitchRate = GetLandscapePitchRate(rotationRate) * Mathf.Rad2Deg;
RawRateDegreesPerSecond = rawRate;
RawPitchRateDegreesPerSecond = rawPitchRate;

if (IsCalibrating)
{
    calibrationTotal += rawRate;
    calibrationPitchTotal += rawPitchRate;
    calibrationSamples++;
    SmoothToward(0f);
    SmoothPitchToward(0f);
    return;
}

var processedYawRateDegPerSec =
    ProcessRate(rawRate - calibratedBias, yawSensitivity, invertYaw) *
    FullRateDegreesPerSecond;
var processedPitchRateDegPerSec =
    ProcessRate(rawPitchRate - calibratedPitchBias, pitchSensitivity, invertPitch) *
    FullRateDegreesPerSecond;
SmoothToward(Mathf.Clamp(processedYawRateDegPerSec / FullRateDegreesPerSecond, -1f, 1f));
SmoothPitchToward(Mathf.Clamp(processedPitchRateDegPerSec / FullRateDegreesPerSecond, -1f, 1f));
```

Each axis uses the same normalization shape with its own sensitivity and invert toggle. After subtracting its calibrated bias, the rate is zeroed within the angular dead zone and rescaled from the dead-zone edge to `FullRateDegreesPerSecond` (180 degrees/second). The response-curve exponent shapes that normalized rate before sensitivity and clamping. The pre-clamp processed rates are integrated using unscaled frame time into pending yaw/pitch angle deltas. Acknowledged packet deltas are subtracted from the pending totals; failed writes or acknowledgements leave them queued, including motion accumulated while a packet is in flight.

```csharp
private float ProcessRate(float adjustedRate, float axisSensitivity, bool invert)
{
    var magnitude = Mathf.Abs(adjustedRate);
    var deadZoneRescaledMagnitude = magnitude <= angularDeadZone
        ? 0f
        : Mathf.Clamp01((magnitude - angularDeadZone) /
          (FullRateDegreesPerSecond - angularDeadZone));
    var curvedMagnitude = Mathf.Pow(deadZoneRescaledMagnitude, responseCurveExponent);
    var target = curvedMagnitude * Mathf.Sign(adjustedRate) * axisSensitivity;
    return invert ? -target : target;
}
```

Both outputs use exponential smoothing; increasing smoothing speed makes response more immediate. **RECENTER GYRO** samples the stationary device for `0.75` seconds and records separate yaw and pitch biases. Focus loss, app pause, gyro disable, or unsupported hardware returns both outputs to neutral.

The landscape mapping uses sensor `y` for left/right yaw and sensor `x` for up/down pitch, matching the phone's landscape orientation. Landscape Right negates each respective axis; Landscape Left and AutoRotation use the raw sign. Physical motion sign can vary with device/orientation, so verify directions on the phone; yaw and pitch each have an independent invert toggle.

### Gyroscope settings

[ControllerInputState.cs](./RoboClashController/Assets/Scripts/ControllerInputState.cs) contains persistent settings through `PlayerPrefs`:

| Setting | Range | Default | Effect |
|---|---:|---:|---|
| Gyroscope enabled | On/off | On | Enables or disables the primary phone-motion aiming input; enabled by default. |
| Yaw sensitivity | `0.1–8` | `2.5` | Independently scales left/right gyro aim. |
| Pitch sensitivity | `0.1–8` | `1.8` | Independently scales up/down gyro aim. |
| Invert yaw | On/off | Off | Reverses horizontal gyro aim only. |
| Invert pitch | On/off | Off | Reverses vertical gyro aim only. |
| Angular dead zone | `0–10°/s` | `1.5°/s` | Ignores small rotational rates on both axes, then rescales from the dead-zone edge. |
| Response curve exponent | `1.0–2.5` | `1.5` | Shapes gyro response after dead-zone rescaling. |
| Smoothing | `1–30` | `20` | Smooths both outputs; higher values respond more quickly. |
| Joystick dead zone | `0–0.5` | `0.12` | Radial center dead zone for movement. |

These are configured in [ControllerDemoScreen.cs](./RoboClashController/Assets/Scripts/ControllerDemoScreen.cs). Gyro turn and pitch values, and their raw rates, are available in the diagnostics UI.

## Shared controller input state

[ControllerInputState.cs](./RoboClashController/Assets/Scripts/ControllerInputState.cs) carries all analog and button values:

```csharp
[Serializable]
public struct ControllerInputState
{
    public float MoveForward;
    public float AimHorizontal;
    public bool Shoot;
    public bool Jump;
    public bool Ability;
    public bool Ultimate;
    public bool Reload;
    public float GyroTurn;
    public float GyroPitch;
}
```

[ControllerInputProvider.cs](./RoboClashController/Assets/Scripts/ControllerInputProvider.cs) samples the joystick, gyro, and action buttons every frame and publishes a new state when a value changes:

```csharp
var state = new ControllerInputState
{
    MoveForward = joystick != null ? joystick.MoveForward : 0f,
    AimHorizontal = joystick != null ? joystick.AimHorizontal : 0f,
    Shoot = shootButton != null && shootButton.IsPressed,
    Jump = jumpButton != null && jumpButton.IsPressed,
    Ability = abilityButton != null && abilityButton.IsPressed,
    Ultimate = ultimateButton != null && ultimateButton.IsPressed,
    Reload = reloadButton != null && reloadButton.IsPressed,
    GyroTurn = gyroscope != null ? gyroscope.TurnInput : 0f,
    GyroPitch = gyroscope != null ? gyroscope.PitchInput : 0f
};
```

On focus loss, pause, or reset, it cancels pointer/button holds and publishes a neutral input state.

## Action buttons

[ControllerActionButton.cs](./RoboClashController/Assets/Scripts/ControllerActionButton.cs) tracks a pointer ID, sets `IsPressed` on pointer down, and releases on pointer up, pointer exit, cancellation, or disable. The screen creates:

| Button | Input | Behavior |
|---|---|---|
| Shoot | `Shoot` | Hold action; stays true while held. |
| Jump | `Jump` | Press action; sent as a press edge and also represented as a held input. |
| Ability | `Ability` | Press action; press edge is preserved in the network sequence counters. |
| Ultimate | `Ultimate` | Press action; press edge is preserved in the network sequence counters. |
| Reload | `Reload` | Press action; press edge is preserved in the network sequence counters. |

The client uses press-sequence counters for Jump, Reload, Ability, and Ultimate so a short tap is not lost between 16 ms network sends.

## Wi-Fi protocol and transmission

[ControllerNetworkClient.cs](./RoboClashController/Assets/Scripts/ControllerNetworkClient.cs) validates the entered IPv4 address, connects via TCP to port `17862`, sends the protocol-1 hello, waits for the game's welcome, and then sends input packets approximately every 16 ms. It preserves `gyroPitch` and `gyroTurn` for compatibility and adds accumulated angle deltas:

```csharp
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
```

Normalized input values are made finite and clamped to `-1..1`; angle deltas are finite degrees formatted with invariant culture and round-trip (`R`) precision. Each line is newline terminated, and the controller retires a packet's deltas only after receiving its `ack`.

## Game-side packet and movement

The game-side [`RoboClashPhoneInputServer.cs`](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs) defines fields for both gyroscope axes and movement axes. Delta fields are optional (missing fields are zero), rejected if non-finite, and clamped to ±60 degrees per packet. The server adds them to lock-protected accumulators rather than replacing prior pending motion. The Unity main thread consumes and resets the accumulated deltas:

```csharp
var yawDelta = hasYawDelta ? Mathf.Clamp(message.gyroYawDelta, -60f, 60f) : 0f;
var pitchDelta = hasPitchDelta ? Mathf.Clamp(message.gyroPitchDelta, -60f, 60f) : 0f;
lock (aimDeltaLock)
{
    accumulatedGyroYawDelta += yawDelta;
    accumulatedGyroPitchDelta += pitchDelta;
}

public void ConsumeAimDelta(out float yawDeg, out float pitchDeg)
{
    lock (aimDeltaLock)
    {
        yawDeg = accumulatedGyroYawDelta;
        pitchDeg = accumulatedGyroPitchDelta;
        accumulatedGyroYawDelta = 0f;
        accumulatedGyroPitchDelta = 0f;
    }
}
```

New connections and disconnects reset the accumulators. When deltas have been received within the recent-input window, movement applies the angles directly using serialized yaw/pitch scales (both default to `1`); otherwise it retains the legacy rate-based behavior for older APKs:

```csharp
phoneServer.ConsumeAimDelta(out var dy, out var dp);
if (phoneServer.HasRecentGyroAimDeltas)
{
    yaw += dy * phoneYawScale;
    pitch = Mathf.Clamp(pitch - dp * phonePitchScale, -20f, 45f);
}
else
{
    yaw += phoneInput.GyroTurn * 140f * Time.deltaTime;
    pitch = Mathf.Clamp(pitch - phoneInput.GyroPitch * 90f * Time.deltaTime, -20f, 45f);
}
```

Then it applies `yaw` to robot body orientation and `pitch` to the aim pivot/camera. The sign of `pitch` is chosen so positive gyro pitch aims upward in the game coordinate convention.

The robot movement code uses joystick axes as translation inputs:

```csharp
input.x = phoneServer.CurrentInput.MoveHorizontal;
input.y = phoneServer.CurrentInput.MoveForward;
var moveInput = new Vector3(input.x, 0f, input.y);
var desired = Quaternion.Euler(0f, yaw, 0f) * moveInput *
              definition.stats.moveSpeed * speedMultiplier;
```

`RoboClashRobotCombat` consumes `Shoot` as a held fire input and `Reload` as an action edge. The game movement code consumes Jump, while the ability component consumes Ability and Ultimate press edges.

## Limits and things to verify

- The gyro reports rotation rate, so camera motion continues while the phone is rotating and stops when the phone is held still. It is not an absolute phone-orientation camera pointer.
- Yaw and pitch have independent sensitivity and invert settings; dead zone, response curve, and smoothing are shared.
- The controller client sends two-axis gyro rate and angle-delta input; the APK must be rebuilt and installed from the controller project to include the 16 ms send interval and reliable delta queue.
- The on-screen joystick caption currently says its horizontal axis aims, but game code maps that value to left/right strafing. This is a label/mapping mismatch in the present implementation; this report documents the code's actual behavior rather than implying joystick X turns the camera.
- Verify on-device that landscape yaw and pitch signs and the new response settings feel intuitive. Use each axis's invert toggle independently if needed.
- Network, actual device sensor behavior, and final APK were not tested during this code change.
