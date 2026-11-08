# RoboClash Wi-Fi Controller: Intended Behavior, Current Issue, and Code Sources

## Purpose

The phone should act as a Wi-Fi controller for RoboClash without USB or ADB reverse:

1. Both the PC and phone join the same Wi-Fi network.
2. The game listens on TCP port `17862` and displays the PC's Wi-Fi IPv4 address.
3. On launch, the controller app asks for that IPv4 address.
4. The player enters the address and taps **CONNECT**.
5. The app attempts the TCP connection and protocol handshake.
6. Only after a successful handshake does the controller show its touch controls and send input.
7. If connection fails or is lost, the app hides the touch controls and displays a status on its connection screen.

The implementation for this flow exists in the controller app project at `C:\Users\moham\RoboClashController` and in the game project at `C:\Users\moham\RoboClash`.

## Current observed issue

The user reports that the controller still times out when entering `192.168.1.13`, while the PC and phone are on the same Wi-Fi.

The PC's `ipconfig` output identifies `192.168.1.13` as the PC's WiFi adapter IPv4 address. The user then ran:

```powershell
Get-NetTCPConnection -LocalPort 17862 -State Listen
```

and received a listener entry with `LocalAddress` equal to `0.0.0.0` and `LocalPort` equal to `17862`. This confirms that a local TCP listener exists on all IPv4 interfaces at the time of the check. It does **not** by itself prove that:

- the phone can reach the PC through the Wi-Fi access point;
- Windows Firewall permits inbound connections from the phone;
- the installed APK contains the latest IP-entry/Wi-Fi client code;
- the controller receives the game server's handshake response.

The PC's active network profile was also checked as `Private`. No phone-to-PC packet capture or successful phone connection has been confirmed. Therefore, the specific cause of the continuing timeout is still unknown.

## Controller app implementation

### Connection screen and controls visibility

[`ControllerDemoScreen.cs`](./RoboClashController/Assets/Scripts/ControllerDemoScreen.cs) builds the runtime UI. It:

- creates a Wi-Fi connection panel with an IPv4 text input and **CONNECT** button;
- passes the entered address to `ControllerNetworkClient.Connect`;
- initially sets the controls' `CanvasGroup` alpha to zero and disables interaction/raycast blocking;
- restores the connection screen and hides controls unless `networkClient.IsConnected` is true;
- shows a connection status and disables the Connect button while a connection attempt is in progress.

The connection screen says to use the PC's Wi-Fi IPv4 address and TCP port `17862`. `ConnectToHost` is the UI callback that starts the attempt; `RefreshConnectionStatus` controls whether the controls or connection screen are visible.

The UI wiring that submits the entered address is:

```csharp
private void ConnectToHost()
{
    if (networkClient == null)
        return;

    networkClient.Connect(ipAddressInput != null ? ipAddressInput.text : string.Empty);
    RefreshConnectionStatus();
}
```

The code that gates the touch controls until the connection succeeds is:

```csharp
if (controllerCanvasGroup != null)
{
    controllerCanvasGroup.alpha = connected ? 1f : 0f;
    controllerCanvasGroup.interactable = connected;
    controllerCanvasGroup.blocksRaycasts = connected;
}
if (connectionPanel != null)
    connectionPanel.SetActive(!connected);
```

These are excerpts from [ControllerDemoScreen.cs](./RoboClashController/Assets/Scripts/ControllerDemoScreen.cs).

### TCP client, handshake, and timeout reporting

[`ControllerNetworkClient.cs`](./RoboClashController/Assets/Scripts/ControllerNetworkClient.cs) implements the client:

- `Connect(string host)` validates that the entered value is an IPv4 address and starts a background connection attempt.
- The client connects to that address on TCP port `17862`, with a five-second connect wait.
- Once connected, it sends the newline-terminated hello line:

  ```json
  {"type":"hello","protocol":1}
  ```

- It expects the exact welcome line:

  ```json
  {"type":"welcome","protocol":1}
  ```

- After the welcome, it sends newline-delimited controller input approximately every 50 ms and expects an `ack` line for each packet.
- The client marks itself connected only after receiving the welcome response. If connection or handshake fails, its status becomes `CONNECTION FAILED: ...`; if the established connection ends, it reports `CONNECTION LOST`.

The client uses TCP, not HTTP. A generic browser request or an ICMP ping is not a substitute for testing this protocol.

The entered IPv4 address is parsed and the connection attempt is started on a background thread:

```csharp
public void Connect(string host)
{
    if (connected || status.StartsWith("CONNECTING", StringComparison.Ordinal))
        return;

    Disconnect();
    if (!IPAddress.TryParse(host != null ? host.Trim() : string.Empty, out var address) ||
        address.AddressFamily != AddressFamily.InterNetwork)
    {
        status = "ENTER A VALID IPV4 ADDRESS";
        return;
    }

    stopping = false;
    status = $"CONNECTING TO {address}:{Port}";
    var generation = Interlocked.Increment(ref connectionGeneration);
    networkThread = new Thread(() => RunConnection(address, generation))
    {
        IsBackground = true,
        Name = "RoboClash Controller TCP Client"
    };
    networkThread.Start();
}
```

The socket connect and protocol flow in `RunConnection` is:

```csharp
localClient = new TcpClient(AddressFamily.InterNetwork)
{
    NoDelay = true,
    SendTimeout = 1000,
    ReceiveTimeout = 5000
};
var connect = localClient.BeginConnect(address, Port, null, null);
if (!connect.AsyncWaitHandle.WaitOne(5000))
    throw new SocketException((int)SocketError.TimedOut);
localClient.EndConnect(connect);

using (var stream = localClient.GetStream())
{
    WriteLine(stream, HelloMessage);
    if (!string.Equals(ReadLine(stream), WelcomeMessage, StringComparison.Ordinal))
        throw new InvalidOperationException("The PC did not accept this controller protocol.");

    connected = true;
    status = "PHONE CONNECTED";
    long sentJumpSequence = 0;
    long sentReloadSequence = 0;
    long sentAbilitySequence = 0;
    long sentUltimateSequence = 0;
    while (IsCurrentConnection(generation))
    {
        ControllerInputState input;
        long jumpSequence;
        long reloadSequence;
        long abilitySequence;
        long ultimateSequence;
        lock (inputLock)
        {
            input = latestInput;
            jumpSequence = jumpPressSequence;
            reloadSequence = reloadPressSequence;
            abilitySequence = abilityPressSequence;
            ultimateSequence = ultimatePressSequence;
        }

        WriteLine(stream, FormatInput(input,
            jumpSequence != sentJumpSequence,
            reloadSequence != sentReloadSequence,
            abilitySequence != sentAbilitySequence,
            ultimateSequence != sentUltimateSequence));
        if (!string.Equals(ReadLine(stream), Acknowledgement, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "The game PC stopped acknowledging controller input.");
        sentJumpSequence = jumpSequence;
        sentReloadSequence = reloadSequence;
        sentAbilitySequence = abilitySequence;
        sentUltimateSequence = ultimateSequence;
        Thread.Sleep(50);
    }
}
```

These excerpts are from [ControllerNetworkClient.cs](./RoboClashController/Assets/Scripts/ControllerNetworkClient.cs). The complete method snapshots button-edge counters under a lock and handles socket cleanup and errors in `catch`/`finally`.

## Game server implementation

[`RoboClashPhoneInputServer.cs`](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs) hosts the other end:

- It listens on `IPAddress.Any` at port `17862`, corresponding to `0.0.0.0:17862`.
- A runtime bootstrap creates/persists the server after the game scene loads.
- For each TCP client, it reads a newline-terminated hello and responds only when it exactly matches protocol version 1.
- It then reads newline-delimited input messages and responds with `ack`.
- The server accepts valid messages only when the JSON has `type: "input"`, `protocol: 1`, finite movement/aim values, and the required supported fields.
- The game reports the phone active only after at least one valid input packet is processed. If valid input stops for more than 0.75 seconds, it closes the client and returns to PC controls.
- The server writes warnings to the Unity Console for invalid packets, lost sockets, and input timeouts.

The server's `StartServer` log states the local address and port. Its status string is built from `BestLocalIPv4`; prefer the actual current Wi-Fi adapter address shown on the PC rather than an old address, because DHCP addresses can change.

The listener is bound to every local IPv4 interface:

```csharp
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
```

The server accepts a client only after the expected hello line, sends the welcome, then acknowledges each input line:

```csharp
var hello = ReadLine(stream);
if (!string.Equals(hello, HelloMessage, StringComparison.Ordinal) ||
    !IsCurrentClient(acceptedClient, generation))
    return;

WriteLine(stream, WelcomeMessage);
lock (clientLock)
{
    if (stopping || client != acceptedClient || clientGeneration != generation)
        return;
    connected = true;
}

while (!stopping && IsCurrentClient(acceptedClient, generation))
{
    var line = ReadLine(stream);
    if (line == null || line.Length == 0)
        break;

    EnqueueMessage(line, generation);
    WriteLine(stream, Acknowledgement);
}
```

On the Unity main thread, queued messages are parsed and invalid inputs close the connection:

```csharp
if (pendingMessage.Generation != clientGeneration || !connected)
    continue;

if (!TryApplyMessage(pendingMessage.Line))
{
    Debug.LogWarning(
        "RoboClash phone controller sent an invalid input packet; disconnecting it.", this);
    CloseClient();
    break;
}

lastInputTime = Time.realtimeSinceStartup;
hasValidInput = true;
```

These excerpts are from [RoboClashPhoneInputServer.cs](./Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs). The full implementation also protects client lifecycle with locks/generation IDs, clamps input values, tracks button edges, and expires idle input after 0.75 seconds.

## Expected wire protocol

The exchange is plain ASCII text, one JSON or acknowledgement line per newline:

```text
PHONE -> PC: {"type":"hello","protocol":1}\n
PC -> PHONE: {"type":"welcome","protocol":1}\n
PHONE -> PC: {"type":"input","protocol":1,"moveForward":0,"aimHorizontal":0,"gyroTurn":0,"shoot":false,"jump":false,"reload":false,"ability":false,"ultimate":false}\n
PC -> PHONE: ack\n
```

The game input model also accepts `gyroPitch`; it defaults to zero if absent. This is a separate input-field compatibility detail and is not required for the initial TCP handshake.

The controller now reads independent landscape yaw and pitch rotation rates, applies the same sensitivity, dead-zone, smoothing, invert, and recenter calibration to both axes, and sends both `gyroTurn` and `gyroPitch`. The game movement code already applies `gyroTurn` to camera yaw and `gyroPitch` to camera pitch; no game protocol change is required.

## Diagnostic interpretation

There are two distinct stages:

1. **TCP connection stage:** The app cannot open a socket to `192.168.1.13:17862`. Likely areas to investigate are the exact address entered, inbound Windows Firewall policy for the running game/Editor, Wi-Fi client isolation/guest-network policy, or whether the app making the test is the updated APK.
2. **Protocol stage:** TCP opens, but the client does not receive the exact welcome line or subsequent `ack`. Check that the PC is running the game server, that the phone app is using the protocol shown above, and review the Unity Console for server warnings.

The command `Get-NetTCPConnection ... State Listen` only confirms that the PC is listening locally; it does not test the phone's network route. Likewise, connecting from the PC back to its own address does not establish phone-to-PC reachability.

## Practical next checks

1. Confirm the installed phone app shows the new IP-entry screen. If it still shows **CONNECT VIA USB**, it is an old APK; rebuild and install from the controller project before testing further.
2. Start RoboClash in Play Mode or as a standalone game and verify the listener command still shows `0.0.0.0:17862`.
3. Confirm the address in the app is exactly the current PC Wi-Fi IPv4 address (`192.168.1.13` in the supplied `ipconfig` output), not a VMware adapter address.
4. Ensure the Windows Firewall inbound rule applies to the actual process receiving the connection (Unity Editor during Play Mode, or the standalone game executable) and is enabled for the active Private profile.
5. Check that the phone is on the same non-guest Wi-Fi network and that the router does not enable wireless client/AP isolation.
6. Record the full error text shown in the controller, and check the Unity Console at the same time. The exact client error helps distinguish a TCP timeout from a handshake/ack failure.
7. If possible, test TCP port `17862` from a second computer on the same Wi-Fi. A failure from another device points toward the network/firewall path; success there while only the phone fails points toward the APK or Android-specific configuration.

## Validation status

- **Confirmed:** The PC address reported by the user is `192.168.1.13`.
- **Confirmed:** The PC reported a listener at `0.0.0.0:17862`.
- **Confirmed in source:** Client and server both use TCP port `17862` and the same protocol version 1 hello/welcome handshake.
- **Confirmed in source:** Controller controls are intended to remain hidden until the client reports a completed handshake.
- **Not confirmed:** That the currently installed APK contains the modified controller source.
- **Not confirmed:** That the phone can establish a TCP connection to the PC over Wi-Fi.
- **Not confirmed:** Whether the failure occurs during TCP connect, handshake, or subsequent acknowledgement.
- **Not confirmed:** Actual phone input delivery in a match.
