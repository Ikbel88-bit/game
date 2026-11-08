# RoboClash Controller — Complete Source and Integration Report

## Scope and source availability

This report documents the controller implementation available in this Unity workspace: the PC-side Wi-Fi receiver, phone-input data model, input-source routing, controls, sensor handling, and tests.

**The Android phone-app project source is not present in the current workspace.** The `RoboClashController` directory is empty, and no phone-app source files or APK are available under `Assets`. Older connection reports contain excerpts and descriptions of a phone client, but those excerpts are not a substitute for the complete, verifiable app source. Therefore, this report does not claim to include the APK's UI/TextField/Connect-button implementation. Provide the Android/Unity phone-controller project to document or repair that side completely.

## Architecture

```text
Phone touch controls and sensors
      │
      │ TCP, newline-delimited protocol v1, port 17862
      ▼
RoboClashPhoneInputServer (persistent Unity PC-side listener)
      │ parsed current input + edge-triggered button presses
      ▼
KeyboardMouseInputSource (phone input plus optional PC keyboard/mouse)
      │
      ▼
RoboClashRobotMovement / combat / abilities
```

In ordinary single-player matches, the human robot uses `KeyboardMouseInputSource`, which also reads the phone receiver when a valid phone connection is active. In local multiplayer, each player is assigned an input source by `RoboClashInputAssignment`; the keyboard/phone source is assigned to player one when the available gamepads do not cover every player.

## Connection lifecycle

The listener is bootstrapped before the first scene loads, persists across scene changes, and binds TCP port `17862` on all local IPv4 interfaces. The phone must still have the game running on the PC to connect; the listener is not available while the PC game is closed.

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void BootstrapServer()
{
    var server = FindAnyObjectByType<RoboClashPhoneInputServer>(
        FindObjectsInactive.Include);
    if (server == null)
    {
        var serverObject = new GameObject("RoboClash Phone Input Server")
        {
            hideFlags = HideFlags.HideInHierarchy
        };
        DontDestroyOnLoad(serverObject);
        server = serverObject.AddComponent<RoboClashPhoneInputServer>();
    }
    else
    {
        server.gameObject.name = "RoboClash Phone Input Server";
        server.gameObject.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(server.gameObject);
        if (!server.gameObject.activeSelf)
            server.gameObject.SetActive(true);
        if (!server.enabled)
            server.enabled = true;
    }
}
```

The receiver's `OnEnable` starts the listener. It displays the selected PC IPv4 address and port in the Unity status text/log. The phone and PC must be on the same reachable network; firewall policy and Wi-Fi client isolation may still prevent access.

## Wire protocol

This is raw TCP text, not HTTP. Each message is ASCII and ends in a newline.

```text
PHONE -> PC: {"type":"hello","protocol":1}\n
PC -> PHONE: {"type":"welcome","protocol":1}\n
PHONE -> PC: {"type":"input","protocol":1,...}\n
PC -> PHONE: ack\n
```

Input messages can contain:

| Field | Meaning in the Unity receiver |
|---|---|
| `moveForward` | Forward/back movement axis |
| `aimHorizontal` | Horizontal movement axis |
| `gyroTurn`, `gyroPitch` | Gyroscope aim-rate fallback |
| `gyroYawDelta`, `gyroPitchDelta` | Gyroscope aim deltas, when supplied |
| `shoot` | Held fire |
| `jump`, `reload`, `ability`, `ultimate` | Button states; receiver exposes press edges |
| `proximityNear` | Guard request |
| `compassValid`, `compassHeading` | Compass reading for yaw stabilization |
| `accelerometerValid`, `accelX`, `accelY`, `accelZ` | Accelerometer reading for pitch stabilization |
| `recenter` | Recenter aim sensor references |

The receiver requires `type: "input"`, protocol version `1`, and finite numeric values. Movement/gyro axes are clamped to `-1..1`, heading to `-180..180`, and each network line is limited in length. Invalid packets disconnect the client. A valid input packet must continue arriving; after `0.75` seconds without one, the connection is considered inactive and PC controls resume.

### Handshake and input acknowledgement (receiver source)

From [`RoboClashPhoneInputServer.cs`](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs):

```csharp
private const string HelloMessage = "{\"type\":\"hello\",\"protocol\":1}";
private const string WelcomeMessage = "{\"type\":\"welcome\",\"protocol\":1}";
private const string Acknowledgement = "ack";

var hello = ReadLine(stream);
if (!string.Equals(hello, HelloMessage, StringComparison.Ordinal) ||
    !IsCurrentClient(acceptedClient, generation))
    return;

WriteLine(stream, WelcomeMessage);
```

After the handshake, the receiver queues each line, replies `ack`, and parses input on Unity's main thread. Message queueing, client-generation checks, socket shutdown, and warning delivery are implemented in the linked receiver source.

### Parsed input model (receiver source)

The receiver maps the protocol into the following public input fields. `proximityNear` maps to `Guard`, and optional sensor fields are retained for aim stabilization:

```csharp
public struct RoboClashPhoneInput
{
    public float MoveForward;
    public float MoveHorizontal;
    public float GyroTurn;
    public float GyroPitch;
    public bool Shoot;
    public bool Guard;
    public bool CompassValid;
    public float CompassHeading;
    public bool AccelerometerValid;
    public float AccelerometerX;
    public float AccelerometerY;
    public float AccelerometerZ;
}
```

The receiver tracks rising edges for Jump, Reload, Ability, and Ultimate, so a held button does not repeatedly trigger these actions. Aim deltas are accumulated and consumed by the movement/input code. Accelerometer pitch and compass yaw corrections are smoothed using the receiver's `SensorFusionTimeConstant`; `recenter` resets the applicable sensor reference.

## Unity input-source code

### Input-source contract

Complete source: [`IRoboClashInputSource.cs`](./Assets/Scripts/RoboClash/Input/IRoboClashInputSource.cs).

```csharp
public interface IRoboClashInputSource
{
    Vector2 Move { get; }
    Vector2 LookDelta { get; }
    bool FireHeld { get; }
    bool JumpPressed { get; }
    bool ReloadPressed { get; }
    bool AbilityPressed { get; }
    bool UltimatePressed { get; }
    bool PausePressed { get; }
}
```

### Phone, keyboard, and mouse source

Complete source: [`KeyboardMouseInputSource.cs`](./Assets/Scripts/RoboClash/Input/KeyboardMouseInputSource.cs).

The source checks `RoboClashPhoneInputServer.IsPhoneActive` before reading phone controls. It combines phone movement with WASD, supports phone fire/buttons and gyro aim, and uses mouse look only when the cursor is locked. Its aim and recenter integration is:

```csharp
public void ApplyPhoneAim(ref float yaw, ref float pitch, float yawScale, float pitchScale)
{
    if (!PhoneActive)
        return;

    var input = PhoneServer.CurrentInput;
    PhoneServer.ConsumeAimDelta(out var yawDelta, out var pitchDelta);
    if (PhoneServer.HasRecentGyroAimDeltas)
    {
        yaw += yawDelta * yawScale;
        pitch = Mathf.Clamp(pitch - pitchDelta * pitchScale, -20f, 45f);
    }
    else
    {
        yaw += input.GyroTurn * 140f * Time.deltaTime;
        pitch = Mathf.Clamp(pitch - input.GyroPitch * 90f * Time.deltaTime,
            -20f, 45f);
    }
}

public bool ConsumePhoneAimRecenter()
{
    var keyboardRequested = Keyboard.current != null &&
                            Keyboard.current.cKey.wasPressedThisFrame;
    var phoneRequested = PhoneActive && PhoneServer.ConsumeRecenterPressed();
    if (!keyboardRequested && !phoneRequested)
        return false;

    PhoneServer?.RecenterAim();
    return true;
}
```

The source also implements fire, movement, Jump, Reload, Ability, and Ultimate. Keyboard pause is handled by `P` or `Escape`; the phone protocol does not currently expose a pause field.

### Player routing and gamepad source

- [`RoboClashPlayerInputRouter.cs`](./Assets/Scripts/RoboClash/Input/RoboClashPlayerInputRouter.cs) stores each robot's `IRoboClashInputSource` and player index.
- [`GamepadInputSource.cs`](./Assets/Scripts/RoboClash/Input/GamepadInputSource.cs) maps the left stick to movement, right stick to look, right trigger to fire, face buttons to Jump/Reload, shoulder buttons to abilities, and Start to pause.
- [`RoboClashInputAssignment.cs`](./Assets/Scripts/RoboClash/RoboClashInputAssignment.cs) validates the gamepad count and assigns input sources for local multiplayer.
- [`RoboClashMatchManager.cs`](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) attaches/configures the input router during participant creation.
- [`RoboClashRobotMovement.cs`](./Assets/Scripts/RoboClash/RoboClashRobotMovement.cs) reads `InputSource` for movement, camera look, phone gyro aim, and recenter.
- [`RoboClashGuardMode.cs`](./Assets/Scripts/RoboClash/RoboClashGuardMode.cs) reads the receiver's Guard state.
- [`RoboClashHUD.cs`](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) displays the receiver connection state and compass diagnostic.

## Source inventory

| Component | Source |
|---|---|
| TCP server, handshake, packet validation, sensor fusion, address selection, timeouts | [`RoboClashPhoneInputServer.cs`](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs) |
| Shared controller input interface | [`IRoboClashInputSource.cs`](./Assets/Scripts/RoboClash/Input/IRoboClashInputSource.cs) |
| Phone + keyboard/mouse input adapter | [`KeyboardMouseInputSource.cs`](./Assets/Scripts/RoboClash/Input/KeyboardMouseInputSource.cs) |
| Gamepad input adapter | [`GamepadInputSource.cs`](./Assets/Scripts/RoboClash/Input/GamepadInputSource.cs) |
| Multiplayer source assignment | [`RoboClashInputAssignment.cs`](./Assets/Scripts/RoboClash/RoboClashInputAssignment.cs) |
| Per-player router | [`RoboClashPlayerInputRouter.cs`](./Assets/Scripts/RoboClash/Input/RoboClashPlayerInputRouter.cs) |
| Phone guard integration | [`RoboClashGuardMode.cs`](./Assets/Scripts/RoboClash/RoboClashGuardMode.cs) |
| Phone packet parser tests | [`PhoneInputParserTests.cs`](./Assets/Tests/Editor/PhoneInputParserTests.cs) |
| Menu connection status display | [`RoboClashMenuController.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) |
| In-game connection status display | [`RoboClashHUD.cs`](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) |

The linked `.cs` files are the complete source of the corresponding Unity-side components; links point to the canonical files so the report does not duplicate large scripts and drift out of date.

## Phone APK source limitation

The phone APK's UI—including the IP text field, Connect button, on-screen sticks/buttons, touch raycast setup, and Android connection client—cannot be fully reported from this workspace because the phone-app project and APK are not present here. The earlier technical report contains some client-side excerpts, but they cannot be independently checked against the installed APK or used to rebuild it.

To include the complete phone-side source, provide the Android/Unity controller project (including its `Assets` and `ProjectSettings`) or add it to this workspace. The installed APK alone is not equivalent to the original maintainable source project.

## Existing parser test coverage

[`PhoneInputParserTests.cs`](./Assets/Tests/Editor/PhoneInputParserTests.cs) tests:

- legacy packet defaults for newer fields;
- guard and compass parsing;
- accelerometer and recenter parsing;
- compass range clamping;
- rejection of non-finite numeric values;
- ignoring unknown JSON fields; and
- handling malformed JSON without throwing.

These are parser tests only. They do not prove that the phone touch UI works, that an Android APK contains the intended client, or that a phone can reach the PC over Wi-Fi.