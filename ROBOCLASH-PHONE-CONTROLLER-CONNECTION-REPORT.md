# RoboClash Phone Controller Connection — Technical Report

## Scope

This report documents the phone controller's intended same-Wi-Fi connection. The game listens on TCP port `17862` on all interfaces and displays the PC's local IPv4 address so the phone can connect to it directly. The phone and PC must be on the same Wi-Fi network.

The controller app project is at `C:\Users\moham\RoboClashController`. On launch, it asks for the PC's Wi-Fi IPv4 address and connects over TCP port `17862`. The control surface is shown only after the game accepts the protocol handshake; USB/ADB reverse is not required for Wi-Fi.

## Current status and important distinction

- The PC's Wi-Fi IPv4 address was confirmed as `192.168.1.17`.
- The Unity Editor process was listening on `0.0.0.0:17862`, which means the server was bound to all local IPv4 interfaces on TCP port `17862`.
- A TCP test from the PC to its own address, `192.168.1.17:17862`, succeeded.
- That local test confirms a listener exists. It does **not** confirm that a separate device (the phone) can reach the PC through the Wi-Fi network or firewall.
- The user reports the phone and PC are on the same Wi-Fi and Unity was allowed through Windows Firewall for Private networks, but the phone app still timed out.
- ADB is installed at `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe`; the connected phone was detected by `adb devices`.
- ADB reverse was used in prior troubleshooting, but it is not part of the desired Wi-Fi connection.
- The game listener is bound to `0.0.0.0`, allowing connections over the PC's Wi-Fi interface. The game displays the current selected PC Wi-Fi IPv4 and port `17862`.
- A phone-to-PC Wi-Fi connection has not yet been verified. Windows Firewall and router client isolation can still prevent same-network devices from reaching one another.
- During an earlier check, no game listener was present because the Unity Editor was not in Play Mode. The server is designed to start after the first scene loads; merely having the Editor open is not enough.

## Unity-side implementation

### Server startup and listening socket

`RoboClashPhoneInputServer` uses TCP port `17862` and protocol version `1`. At scene load it finds or creates a persistent server component. When enabled, it starts a `TcpListener` bound to `IPAddress.Any`, then logs the selected local IPv4 address for the phone:

```csharp
public const int Port = 17862;
private const int ProtocolVersion = 1;

private void StartServer()
{
    stopping = false;
    try
    {
        listener = new TcpListener(IPAddress.Any, Port);
        listener.Server.SetSocketOption(
            SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();
        listenerThread = new Thread(ListenForController)
        {
            IsBackground = true,
            Name = "RoboClash Phone Controller Listener"
        };
        listenerThread.Start();
        var localAddress = GetBestLocalIPv4();
        Debug.Log(
            $"Phone input server listening on 0.0.0.0:{Port}. " +
            $"Connect the phone on the same Wi-Fi to {localAddress}:{Port}.",
            this);
    }
    catch (Exception exception) when (
        exception is SocketException ||
        exception is InvalidOperationException ||
        exception is ArgumentException)
    {
        Debug.LogError(
            $"Could not start the phone controller host on TCP port {Port}: {exception}",
            this);
        StopServer();
    }
}
```

Source: [`RoboClashPhoneInputServer.cs`](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs#L31), especially [server startup](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs#L217).

The game selects and displays the PC's local Wi-Fi IPv4 address. Configure the phone controller to connect directly to that address on TCP port `17862`. The address observed during earlier troubleshooting was `192.168.1.17`; use the current address displayed by the game because DHCP can change it.

### Handshake required after opening TCP

Opening a TCP socket alone does not make the game report an active phone controller. The first line from the phone must be the exact hello JSON, terminated by a newline. The server replies with a welcome line:

```csharp
private const string HelloMessage = "{\"type\":\"hello\",\"protocol\":1}";
private const string WelcomeMessage = "{\"type\":\"welcome\",\"protocol\":1}";

var hello = ReadLine(stream);
if (!string.Equals(hello, HelloMessage, StringComparison.Ordinal) ||
    !IsCurrentClient(acceptedClient, generation))
    return;

WriteLine(stream, WelcomeMessage);
```

Source: [`RoboClashPhoneInputServer.cs`](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs#L36) and [handshake processing](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs#L312).

This is a raw TCP newline-delimited text protocol, **not HTTP**. A normal browser navigation sends an HTTP request and will not send this handshake.

### Input packets required to remain connected

After receiving the welcome message, the phone must periodically send valid newline-terminated input packets. The accepted JSON shape is:

```json
{"type":"input","protocol":1,"moveForward":0.0,"aimHorizontal":0.0,"gyroTurn":0.0,"gyroPitch":0.0,"shoot":false,"jump":false,"reload":false,"ability":false,"ultimate":false}
```

`moveForward` and `aimHorizontal` are the left movement stick's forward and horizontal axes. `gyroTurn` and `gyroPitch` control horizontal and vertical aiming, respectively; positive `gyroPitch` aims up. All four values must be finite numbers, and the server clamps them to `-1..1`. For compatibility with existing clients, an omitted `gyroPitch` is treated as zero. Button fields are booleans. Each received line is acknowledged with the plain text line `ack`:

```csharp
if (line == null || line.Length == 0)
    break;

EnqueueMessage(line, generation);
WriteLine(stream, Acknowledgement);
```

The packet must have `type` equal to `"input"` and `protocol` equal to `1`. Invalid packets cause the client to be disconnected.

Source: [`RoboClashPhoneInputServer.cs`](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs#L380).

### Meaning of the in-game connection indicator

The HUD's connected state requires all of the following: the server has accepted the client, a valid input packet has been processed, and valid input has arrived within the last `0.75` seconds:

```csharp
public static bool IsPhoneActive =>
    Active != null &&
    Active.connected &&
    Active.hasValidInput &&
    Time.realtimeSinceStartup - Active.lastInputTime <= InputTimeoutSeconds;

public static string StatusText => IsPhoneActive
    ? "PHONE: CONNECTED"
    : $"PHONE: WAITING  WIFI {BestLocalIPv4}:{Port}";
```

Source: [`RoboClashPhoneInputServer.cs`](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs#L35) and [`RoboClashHUD.cs`](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs#L124).

Therefore, the HUD may continue to show `PHONE: WAITING` if the phone never sends input, even if it briefly opened a TCP socket. The server also times out idle clients after the same interval.

## Current Wi-Fi setup

1. Connect the PC and phone to the same Wi-Fi network.
2. Start RoboClash and enter Play Mode or run the standalone game. The game status and Unity Console show the PC address and port `17862`; the server listens on all interfaces.
3. Open the controller APK, enter the displayed PC IPv4 address, and tap **CONNECT**. The app connects to TCP port `17862` and sends the protocol hello and input packets described above. The controller controls remain hidden until the game accepts the handshake.
4. If the phone cannot connect, allow inbound TCP port `17862` through Windows Firewall on the active Private network profile, and check that the router does not isolate wireless clients. Do not use `127.0.0.1` on the phone; that address refers to the phone itself.
5. Confirm the game reports `PHONE: CONNECTED` and test movement and buttons in a match.

The controller currently uses direct IP entry rather than automatic same-Wi-Fi discovery. No phone-to-PC connection test has been performed yet.

## Earlier USB troubleshooting (superseded)

1. Inspected the game receiver and controller client. Both use TCP port `17862`, protocol version `1`, the same newline-delimited JSON hello/welcome handshake, and matching input/acknowledgement packets.
2. Confirmed the PC Wi-Fi address was `192.168.1.17`, Unity had previously listened on `0.0.0.0:17862`, and a local PC-to-self TCP test succeeded. This did not establish phone-to-PC Wi-Fi reachability.
3. Found ADB at `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe`; `adb devices` detected the USB-connected phone.
4. Ran `adb reverse tcp:17862 tcp:17862` and verified the rule appeared in `adb reverse --list`.
5. Found that `ControllerNetworkClient.Connect(string)` rejected loopback addresses, so entering `127.0.0.1` never started a connection attempt.
6. Changed the controller client to connect to `IPAddress.Loopback` without an address argument. Removed the PC IP field and replaced it with a **CONNECT VIA USB** button and ADB instruction text.
7. Updated the game status to show USB ADB reverse is required and changed the listener startup log to print the reverse command.
8. Verified `RoboClashMainMenu.unity` is the first enabled build scene. The server's `AfterSceneLoad` bootstrap runs from that scene and persists across later scene changes, so it is designed to listen from the main menu.

## Obsolete USB-only test procedure (not required for Wi-Fi)

1. Rebuild the Android APK from the updated `RoboClashController` Unity project and install that APK on the phone. The older installed APK still rejects `127.0.0.1`.
2. Connect the phone by USB, enable USB debugging, and approve the debugging authorization prompt.
3. On the PC, run:

   ```powershell
   & "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" devices
   & "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" reverse tcp:17862 tcp:17862
   & "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" reverse --list
   ```

   Confirm the phone is listed as `device` and the reverse list contains `tcp:17862 tcp:17862`.

4. Start RoboClash and enter Play Mode or run the standalone game. Wait at the main menu and confirm the Unity Console logs that the input server is listening on port `17862`. The Unity Editor outside Play Mode does not run the server.
5. Open the rebuilt controller APK and tap **CONNECT VIA USB**. It automatically targets `127.0.0.1:17862`; no IP entry is needed.
6. Confirm the phone reports `PHONE CONNECTED` and the game reports `PHONE: CONNECTED`. Test movement and buttons in a match.
7. If it fails, check that the game is running and the ADB reverse rule remains active. ADB forwarding is cleared when the device disconnects or the ADB session resets.

## Previous USB verification status

- The updated controller source has not yet been built into and installed as a new APK.
- No post-change phone connection test has been performed.
- ADB reverse was confirmed active, but the game server was not running/listening at the time of the earlier check. It must be active in Play Mode or in a standalone game for the tunnel to reach it.
- The phone app's final runtime connection status and input delivery remain unverified.

## Conclusion

The game listens on all network interfaces and displays the PC's Wi-Fi address and port. The controller app prompts for that address and reveals its control surface after a successful protocol handshake. Phone-to-PC connectivity still requires manual verification.
