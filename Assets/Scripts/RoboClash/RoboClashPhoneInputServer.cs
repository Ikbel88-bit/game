using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RoboClash
{
    public enum RoboClashPhoneAction
    {
        Jump,
        Reload,
        Ability,
        Ultimate
    }

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

    public struct ParsedPhoneInput
    {
        public float MoveForward;
        public float AimHorizontal;
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
        public bool Recenter;
    }

    [DisallowMultipleComponent]
    public sealed class RoboClashPhoneInputServer : MonoBehaviour
    {
        public const int Port = 17862;
        public const int MaximumPhonePlayers = 4;
        private const int ProtocolVersion = 1;
        private const int MaximumLineLength = 512;
        private const int MaximumQueuedMessages = 64;
        private const float InputTimeoutSeconds = 0.75f;
        private const float GuardInputTimeoutSeconds = 0.5f;
        private const float GyroAimDeltaRecentSeconds = 0.25f;
        private const float SensorFusionTimeConstant = 0.75f;
        private const string HelloMessage = "{\"type\":\"hello\",\"protocol\":1}";
        private const string WelcomeMessage = "{\"type\":\"welcome\",\"protocol\":1}";
        private const string Acknowledgement = "ack";
        private const float LocalIPv4CacheSeconds = 5f;

        private static readonly object LocalIPv4CacheLock = new object();
        private static string cachedLocalIPv4;
        private static float localIPv4CacheExpiresAt;
        private static bool hasCachedLocalIPv4;

        [Serializable]
        private sealed class InputMessage
        {
            public string type = string.Empty;
            public int protocol = 0;
            public float moveForward = 0f;
            public float aimHorizontal = 0f;
            public float gyroTurn = 0f;
            public float gyroPitch = 0f;
            public float gyroYawDelta = 0f;
            public float gyroPitchDelta = 0f;
            public bool shoot = false;
            public bool jump = false;
            public bool reload = false;
            public bool ability = false;
            public bool ultimate = false;
            public bool proximityNear = false;
            public bool compassValid = false;
            public float compassHeading = 0f;
            public bool accelerometerValid = false;
            public float accelX = 0f;
            public float accelY = 0f;
            public float accelZ = 0f;
            public bool recenter = false;
        }

        private struct PendingMessage
        {
            public string Line;
            public int PlayerIndex;
            public int Generation;
            public TaskCompletionSource<string> Completion;
        }

        private sealed class PhoneState
        {
            public readonly object AimDeltaLock = new object();
            public TcpClient Client;
            public Thread ClientThread;
            public volatile bool Connected;
            public volatile bool HasValidInput;
            public volatile float LastInputTime;
            public int Generation;
            public RoboClashPhoneInput CurrentInput;
            public bool ReportedPhoneActive;
            public bool LastJump;
            public bool LastReload;
            public bool LastAbility;
            public bool LastUltimate;
            public bool JumpPressed;
            public bool ReloadPressed;
            public bool AbilityPressed;
            public bool UltimatePressed;
            public float AccumulatedGyroYawDelta;
            public float AccumulatedGyroPitchDelta;
            public float LastGyroAimDeltaTime;
            public bool HasReceivedGyroAimDeltas;
            public bool HasSensorReference;
            public bool HasCompassReference;
            public float AccelerometerPitchOffset;
            public float CompassHeadingReference;
            public float CompassYawReference;
            public float FusedYaw;
            public float FusedPitch;
            public float LastFusionSampleTime;
            public bool RecenterPressed;
            public int InvalidPacketWarningGeneration = -1;
        }

        private readonly Queue<PendingMessage> pendingMessages = new Queue<PendingMessage>();
        private readonly object messageLock = new object();
        private readonly object clientLock = new object();
        private readonly PhoneState[] phoneStates =
        {
            new PhoneState(), new PhoneState(), new PhoneState(), new PhoneState()
        };
        private TcpListener listener;
        private Thread listenerThread;
        private volatile bool stopping;
        private string pendingNetworkWarning;

        public static RoboClashPhoneInputServer Active { get; private set; }
        public static bool IsPhoneConnected => IsPhoneActive;
        public static bool IsPhoneActive => Active != null && Active.ConnectedPhoneCount > 0;
        public static string StatusText => IsPhoneActive
            ? $"PHONES: {Active.ConnectedPhoneCount}/{MaximumPhonePlayers} CONNECTED"
            : $"PHONE: WAITING  WIFI {BestLocalIPv4}:{Port}";
        public static string BestLocalIPv4 => GetBestLocalIPv4();

        public bool IsConnected => ConnectedPhoneCount > 0;
        public int ConnectedPhoneCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < phoneStates.Length; i++)
                {
                    if (IsPhoneActiveForPlayer(i))
                        count++;
                }
                return count;
            }
        }
        public RoboClashPhoneInput CurrentInput => GetCurrentInput(0);
        public bool HasRecentGyroAimDeltas => HasRecentGyroAimDeltasForPlayer(0);

        public bool IsPhoneActiveForPlayer(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= phoneStates.Length)
                return false;
            var state = phoneStates[playerIndex];
            return state.Connected && state.HasValidInput &&
                   Time.realtimeSinceStartup - state.LastInputTime <= InputTimeoutSeconds;
        }

        public bool ArePlayersConnected(int playerCount)
        {
            if (playerCount < 1 || playerCount > phoneStates.Length)
                return false;
            for (var i = 0; i < playerCount; i++)
            {
                if (!IsPhoneActiveForPlayer(i))
                    return false;
            }
            return true;
        }

        public RoboClashPhoneInput GetCurrentInput(int playerIndex)
        {
            return playerIndex >= 0 && playerIndex < phoneStates.Length &&
                   IsPhoneActiveForPlayer(playerIndex)
                ? phoneStates[playerIndex].CurrentInput
                : default;
        }

        public bool HasRecentGyroAimDeltasForPlayer(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= phoneStates.Length)
                return false;
            var state = phoneStates[playerIndex];
            lock (state.AimDeltaLock)
                return state.HasReceivedGyroAimDeltas &&
                       Time.realtimeSinceStartup - state.LastGyroAimDeltaTime <= GyroAimDeltaRecentSeconds;
        }
        public string LocalAddress => GetBestLocalIPv4();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Active = null;
            lock (LocalIPv4CacheLock)
            {
                cachedLocalIPv4 = null;
                localIPv4CacheExpiresAt = 0f;
                hasCachedLocalIPv4 = false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapServer()
        {
            var server = FindAnyObjectByType<RoboClashPhoneInputServer>(FindObjectsInactive.Include);
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

        private void OnEnable()
        {
            if (Active != null && Active != this)
            {
                Debug.LogError("Only one RoboClash phone input server may be active.", this);
                enabled = false;
                return;
            }

            Active = this;
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
            StartServer();
        }

        private void Update()
        {
            var warning = TakeNetworkWarning();
            if (!string.IsNullOrEmpty(warning))
                Debug.LogWarning(warning, this);

            while (TryDequeueMessage(out var pendingMessage))
            {
                var state = phoneStates[pendingMessage.PlayerIndex];
                if (pendingMessage.Generation != state.Generation || !state.Connected)
                {
                    pendingMessage.Completion.TrySetResult("connection changed while processing packet");
                    continue;
                }

                if (!TryApplyMessage(pendingMessage.Line, state, out var rejectionReason))
                {
                    if (state.InvalidPacketWarningGeneration != pendingMessage.Generation)
                    {
                        state.InvalidPacketWarningGeneration = pendingMessage.Generation;
                        Debug.LogWarning(
                            $"Phone {pendingMessage.PlayerIndex + 1} sent an invalid input packet: {rejectionReason}. " +
                            $"Packet length: {pendingMessage.Line.Length}.",
                            this);
                    }
                    pendingMessage.Completion.TrySetResult(rejectionReason);
                    continue;
                }

                state.InvalidPacketWarningGeneration = -1;
                state.LastInputTime = Time.realtimeSinceStartup;
                state.HasValidInput = true;
                pendingMessage.Completion.TrySetResult(null);
            }

            for (var i = 0; i < phoneStates.Length; i++)
            {
                var state = phoneStates[i];
                if (state.CurrentInput.Guard &&
                    Time.realtimeSinceStartup - state.LastInputTime > GuardInputTimeoutSeconds)
                {
                    var input = state.CurrentInput;
                    input.Guard = false;
                    state.CurrentInput = input;
                }

                if (state.Connected && state.HasValidInput &&
                    Time.realtimeSinceStartup - state.LastInputTime > InputTimeoutSeconds)
                {
                    Debug.LogWarning(
                        $"Phone {i + 1} controller timed out; its PC/gamepad fallback is active.", this);
                    CloseClient(i);
                }

                if (!IsPhoneActiveForPlayer(i))
                    ResetInput(state);

                var phoneActive = IsPhoneActiveForPlayer(i);
                if (state.ReportedPhoneActive != phoneActive)
                {
                    state.ReportedPhoneActive = phoneActive;
                    Debug.Log(phoneActive
                        ? $"Phone {i + 1} controller connected; its phone controls are active."
                        : $"Phone {i + 1} controller disconnected; its fallback controls are active.", this);
                }
            }
        }

        public bool ConsumePressed(RoboClashPhoneAction action)
        {
            return ConsumePressed(0, action);
        }

        public bool ConsumePressed(int playerIndex, RoboClashPhoneAction action)
        {
            if (playerIndex < 0 || playerIndex >= phoneStates.Length)
                return false;
            var state = phoneStates[playerIndex];
            switch (action)
            {
                case RoboClashPhoneAction.Jump:
                    return Consume(ref state.JumpPressed);
                case RoboClashPhoneAction.Reload:
                    return Consume(ref state.ReloadPressed);
                case RoboClashPhoneAction.Ability:
                    return Consume(ref state.AbilityPressed);
                case RoboClashPhoneAction.Ultimate:
                    return Consume(ref state.UltimatePressed);
                default:
                    return false;
            }
        }

        public bool ConsumeRecenterPressed()
        {
            return ConsumeRecenterPressed(0);
        }

        public bool ConsumeRecenterPressed(int playerIndex)
        {
            return playerIndex >= 0 && playerIndex < phoneStates.Length &&
                   Consume(ref phoneStates[playerIndex].RecenterPressed);
        }

        public void RecenterAim()
        {
            RecenterAim(0);
        }

        public void RecenterAim(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= phoneStates.Length)
                return;
            var state = phoneStates[playerIndex];
            var input = GetCurrentInput(playerIndex);
            lock (state.AimDeltaLock)
            {
                SetSensorReference(state, input.AccelerometerValid, input.AccelerometerX,
                    input.AccelerometerY, input.AccelerometerZ,
                    input.CompassValid, input.CompassHeading);
            }
        }

        private void StartServer()
        {
            stopping = false;
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
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
                Debug.LogError($"Could not start the phone controller host on TCP port {Port}: {exception}", this);
                StopServer();
            }
        }

        private void ListenForController()
        {
            while (!stopping)
            {
                TcpClient acceptedClient = null;
                try
                {
                    var activeListener = listener;
                    if (activeListener == null)
                        return;
                    acceptedClient = activeListener.AcceptTcpClient();
                    acceptedClient.NoDelay = true;
                    var playerIndex = -1;
                    int generation;
                    lock (clientLock)
                    {
                        for (var i = 0; i < phoneStates.Length; i++)
                        {
                            if (phoneStates[i].Client != null)
                                continue;
                            playerIndex = i;
                            break;
                        }

                        if (playerIndex >= 0)
                        {
                            var state = phoneStates[playerIndex];
                            generation = ++state.Generation;
                            state.Client = acceptedClient;
                            state.ClientThread = null;
                            state.Connected = false;
                            state.HasValidInput = false;
                            ResetAimDelta(state);
                        }
                        else
                        {
                            generation = -1;
                        }
                    }

                    if (playerIndex < 0)
                    {
                        SetNetworkWarning(
                            $"The phone controller limit of {MaximumPhonePlayers} connected devices was reached.");
                        acceptedClient.Close();
                        acceptedClient = null;
                        continue;
                    }

                    ClearPendingMessages(playerIndex);
                    var assignedPlayerIndex = playerIndex;
                    var clientForThread = acceptedClient;
                    var connectionThread = new Thread(() =>
                        HandleController(clientForThread, assignedPlayerIndex, generation))
                    {
                        IsBackground = true,
                        Name = $"RoboClash Phone Controller {playerIndex + 1}"
                    };
                    lock (clientLock)
                    {
                        var state = phoneStates[playerIndex];
                        if (generation != state.Generation || state.Client != clientForThread || stopping)
                        {
                            clientForThread.Close();
                            continue;
                        }
                        state.ClientThread = connectionThread;
                    }
                    connectionThread.Start();
                    acceptedClient = null;
                }
                catch (ObjectDisposedException)
                {
                    if (!stopping)
                        SetNetworkWarning("The phone controller socket closed unexpectedly.");
                }
                catch (SocketException)
                {
                    if (!stopping)
                        SetNetworkWarning("The phone controller network connection ended.");
                }
                finally
                {
                    if (acceptedClient != null)
                        acceptedClient.Close();
                }
            }
        }

        private void HandleController(TcpClient acceptedClient, int playerIndex, int generation)
        {
            try
            {
                using (var stream = acceptedClient.GetStream())
                {
                    var hello = ReadLine(stream);
                    if (!string.Equals(hello, HelloMessage, StringComparison.Ordinal) ||
                        !IsCurrentClient(acceptedClient, playerIndex, generation))
                        return;

                    WriteLine(stream, WelcomeMessage);
                    lock (clientLock)
                    {
                        var state = phoneStates[playerIndex];
                        if (stopping || state.Client != acceptedClient || state.Generation != generation)
                            return;
                        state.Connected = true;
                    }
                    Debug.Log(
                        $"Phone Player {playerIndex + 1} connected from {acceptedClient.Client.RemoteEndPoint}.",
                        this);

                    while (!stopping && IsCurrentClient(acceptedClient, playerIndex, generation))
                    {
                        var line = ReadLine(stream);
                        if (line == null || line.Length == 0)
                            break;

                        var completion = EnqueueMessage(line, playerIndex, generation);
                        if (!completion.Task.Wait(TimeSpan.FromSeconds(5)))
                        {
                            SetNetworkWarning(
                                "The game did not process a phone controller packet within five seconds.");
                            break;
                        }

                        var rejectionReason = completion.Task.Result;
                        if (rejectionReason != null)
                        {
                            WriteLine(stream, "invalid input packet: " + rejectionReason);
                            break;
                        }

                        WriteLine(stream, Acknowledgement);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                if (!stopping && IsCurrentClient(acceptedClient, playerIndex, generation))
                    SetNetworkWarning($"Phone {playerIndex + 1} socket closed unexpectedly.");
            }
            catch (SocketException)
            {
                if (!stopping && IsCurrentClient(acceptedClient, playerIndex, generation))
                    SetNetworkWarning($"Phone {playerIndex + 1} network connection ended.");
            }
            catch (System.IO.IOException)
            {
                if (!stopping && IsCurrentClient(acceptedClient, playerIndex, generation))
                    SetNetworkWarning($"Phone {playerIndex + 1} stream closed.");
            }
            finally
            {
                var disconnectedCurrentClient = false;
                lock (clientLock)
                {
                    var state = phoneStates[playerIndex];
                    if (state.Client == acceptedClient && generation == state.Generation)
                    {
                        state.Client = null;
                        state.ClientThread = null;
                        state.Connected = false;
                        state.HasValidInput = false;
                        disconnectedCurrentClient = true;
                    }
                }
                if (disconnectedCurrentClient)
                {
                    ClearPendingMessages(playerIndex);
                    ResetInput(phoneStates[playerIndex]);
                }
                acceptedClient.Close();
            }
        }

        private bool IsCurrentClient(TcpClient candidate, int playerIndex, int generation)
        {
            lock (clientLock)
                return !stopping && phoneStates[playerIndex].Client == candidate &&
                       phoneStates[playerIndex].Generation == generation;
        }

        private bool TryApplyMessage(string line, PhoneState state, out string rejectionReason)
        {
            if (!TryParseInputMessage(line, out var message, out rejectionReason))
                return false;

            var yawDelta = 0f;
            var pitchDelta = 0f;
            var hasYawDelta = line.IndexOf("\"gyroYawDelta\"", StringComparison.Ordinal) >= 0;
            var hasPitchDelta = line.IndexOf("\"gyroPitchDelta\"", StringComparison.Ordinal) >= 0;
            var hasGyroAimDeltas = hasYawDelta || hasPitchDelta;
            if (hasGyroAimDeltas)
            {
                yawDelta = hasYawDelta ? Mathf.Clamp(message.gyroYawDelta, -60f, 60f) : 0f;
                pitchDelta = hasPitchDelta ? Mathf.Clamp(message.gyroPitchDelta, -60f, 60f) : 0f;
            }
            state.CurrentInput = new RoboClashPhoneInput
            {
                MoveForward = Mathf.Clamp(message.moveForward, -1f, 1f),
                MoveHorizontal = Mathf.Clamp(message.aimHorizontal, -1f, 1f),
                GyroTurn = Mathf.Clamp(message.gyroTurn, -1f, 1f),
                GyroPitch = Mathf.Clamp(message.gyroPitch, -1f, 1f),
                Shoot = message.shoot,
                Guard = message.proximityNear,
                CompassValid = message.compassValid,
                CompassHeading = Mathf.Clamp(message.compassHeading, -180f, 180f),
                AccelerometerValid = message.accelerometerValid &&
                                     IsValidAcceleration(message.accelX, message.accelY, message.accelZ),
                AccelerometerX = message.accelX,
                AccelerometerY = message.accelY,
                AccelerometerZ = message.accelZ
            };
            lock (state.AimDeltaLock)
            {
                if (message.recenter)
                {
                    SetSensorReference(state, state.CurrentInput.AccelerometerValid,
                        state.CurrentInput.AccelerometerX, state.CurrentInput.AccelerometerY,
                        state.CurrentInput.AccelerometerZ, state.CurrentInput.CompassValid,
                        state.CurrentInput.CompassHeading);
                    state.RecenterPressed = true;
                }

                ApplySensorFusion(state, yawDelta, pitchDelta, hasGyroAimDeltas,
                    state.CurrentInput.GyroTurn, state.CurrentInput.GyroPitch,
                    state.CurrentInput.AccelerometerValid, state.CurrentInput.AccelerometerX,
                    state.CurrentInput.AccelerometerY, state.CurrentInput.AccelerometerZ,
                    state.CurrentInput.CompassValid, state.CurrentInput.CompassHeading);
            }
            state.JumpPressed |= message.jump && !state.LastJump;
            state.ReloadPressed |= message.reload && !state.LastReload;
            state.AbilityPressed |= message.ability && !state.LastAbility;
            state.UltimatePressed |= message.ultimate && !state.LastUltimate;
            state.LastJump = message.jump;
            state.LastReload = message.reload;
            state.LastAbility = message.ability;
            state.LastUltimate = message.ultimate;
            rejectionReason = null;
            return true;
        }

        public static bool TryParseInputLineForTest(string line, out ParsedPhoneInput parsed)
        {
            parsed = default;
            if (!TryParseInputMessage(line, out var message, out _))
                return false;

            parsed = new ParsedPhoneInput
            {
                MoveForward = Mathf.Clamp(message.moveForward, -1f, 1f),
                AimHorizontal = Mathf.Clamp(message.aimHorizontal, -1f, 1f),
                GyroTurn = Mathf.Clamp(message.gyroTurn, -1f, 1f),
                GyroPitch = Mathf.Clamp(message.gyroPitch, -1f, 1f),
                Shoot = message.shoot,
                Guard = message.proximityNear,
                CompassValid = message.compassValid,
                CompassHeading = Mathf.Clamp(message.compassHeading, -180f, 180f),
                AccelerometerValid = message.accelerometerValid &&
                                     IsValidAcceleration(message.accelX, message.accelY, message.accelZ),
                AccelerometerX = message.accelX,
                AccelerometerY = message.accelY,
                AccelerometerZ = message.accelZ,
                Recenter = message.recenter
            };
            return true;
        }

        private static bool TryParseInputMessage(string line, out InputMessage message)
        {
            return TryParseInputMessage(line, out message, out _);
        }

        private static bool TryParseInputMessage(string line, out InputMessage message,
            out string rejectionReason)
        {
            message = null;
            if (string.IsNullOrEmpty(line))
            {
                rejectionReason = "packet is empty";
                return false;
            }

            try
            {
                message = JsonUtility.FromJson<InputMessage>(line);
            }
            catch (Exception exception)
            {
                rejectionReason = "JSON parse failed: " + exception.Message;
                return false;
            }

            if (message == null)
            {
                rejectionReason = "JSON did not contain an input object";
                return false;
            }

            if (message.type != "input")
            {
                rejectionReason = $"unexpected message type '{message.type}'";
                return false;
            }

            if (message.protocol != ProtocolVersion)
            {
                rejectionReason = $"unsupported protocol version {message.protocol}";
                return false;
            }

            if (!IsFinite(message.moveForward) || !IsFinite(message.aimHorizontal) ||
                !IsFinite(message.gyroTurn) || !IsFinite(message.gyroPitch) ||
                !IsFinite(message.gyroYawDelta) || !IsFinite(message.gyroPitchDelta) ||
                !IsFinite(message.compassHeading) || !IsFinite(message.accelX) ||
                !IsFinite(message.accelY) || !IsFinite(message.accelZ))
            {
                rejectionReason = "packet contains a non-finite numeric value";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        public void ConsumeAimDelta(out float yawDeg, out float pitchDeg)
        {
            ConsumeAimDelta(0, out yawDeg, out pitchDeg);
        }

        public void ConsumeAimDelta(int playerIndex, out float yawDeg, out float pitchDeg)
        {
            if (playerIndex < 0 || playerIndex >= phoneStates.Length)
            {
                yawDeg = pitchDeg = 0f;
                return;
            }
            var state = phoneStates[playerIndex];
            lock (state.AimDeltaLock)
            {
                yawDeg = state.AccumulatedGyroYawDelta;
                pitchDeg = state.AccumulatedGyroPitchDelta;
                state.AccumulatedGyroYawDelta = 0f;
                state.AccumulatedGyroPitchDelta = 0f;
            }
        }

        private static void ResetAimDelta(PhoneState state)
        {
            lock (state.AimDeltaLock)
            {
                state.AccumulatedGyroYawDelta = 0f;
                state.AccumulatedGyroPitchDelta = 0f;
                state.LastGyroAimDeltaTime = 0f;
                state.HasReceivedGyroAimDeltas = false;
                state.HasSensorReference = false;
                state.HasCompassReference = false;
                state.FusedYaw = 0f;
                state.FusedPitch = 0f;
                state.LastFusionSampleTime = 0f;
                state.RecenterPressed = false;
            }
        }

        private void ApplySensorFusion(PhoneState state, float gyroYawDelta, float gyroPitchDelta,
            bool hasGyroDelta, float gyroTurn, float gyroPitchRate,
            bool accelerometerValid, float accelX, float accelY,
            float accelZ, bool compassValid, float compassHeading)
        {
            var now = Time.realtimeSinceStartup;
            var elapsed = state.LastFusionSampleTime > 0f
                ? Mathf.Clamp(now - state.LastFusionSampleTime, 0.001f, 0.25f)
                : 0.033f;
            state.LastFusionSampleTime = now;

            if (!hasGyroDelta && (accelerometerValid || compassValid))
            {
                gyroYawDelta = gyroTurn * 140f * elapsed;
                gyroPitchDelta = gyroPitchRate * 90f * elapsed;
                hasGyroDelta = true;
            }

            var startYaw = state.FusedYaw;
            var startPitch = state.FusedPitch;
            if (hasGyroDelta)
            {
                state.FusedYaw += gyroYawDelta;
                state.FusedPitch += gyroPitchDelta;
            }

            if (accelerometerValid)
            {
                var measuredPitch = GetAccelerometerPitch(accelX, accelY, accelZ);
                if (!state.HasSensorReference)
                    SetSensorReference(state, true, accelX, accelY, accelZ, compassValid, compassHeading);
                var correctedPitch = measuredPitch + state.AccelerometerPitchOffset;
                var correction = 1f - Mathf.Exp(-elapsed / SensorFusionTimeConstant);
                state.FusedPitch = Mathf.Lerp(state.FusedPitch, correctedPitch, correction);
            }

            if (compassValid)
            {
                if (!state.HasCompassReference)
                    SetSensorReference(state, accelerometerValid, accelX, accelY, accelZ,
                        true, compassHeading);
                var correctedYaw = state.CompassYawReference +
                                   Mathf.DeltaAngle(state.CompassHeadingReference, compassHeading);
                var correction = 1f - Mathf.Exp(-elapsed / SensorFusionTimeConstant);
                state.FusedYaw = Mathf.Lerp(state.FusedYaw, correctedYaw, correction);
            }

            var fusedYawDelta = state.FusedYaw - startYaw;
            var fusedPitchDelta = state.FusedPitch - startPitch;
            var pendingYaw = state.AccumulatedGyroYawDelta + fusedYawDelta;
            var pendingPitch = state.AccumulatedGyroPitchDelta + fusedPitchDelta;
            if (!IsFinite(pendingYaw) || !IsFinite(pendingPitch))
                return;

            state.AccumulatedGyroYawDelta = pendingYaw;
            state.AccumulatedGyroPitchDelta = pendingPitch;
            if (hasGyroDelta || accelerometerValid || compassValid)
            {
                state.HasReceivedGyroAimDeltas = true;
                state.LastGyroAimDeltaTime = now;
            }
        }

        private static void SetSensorReference(PhoneState state, bool accelerometerValid, float accelX,
            float accelY, float accelZ, bool compassValid, float compassHeading)
        {
            if (accelerometerValid)
            {
                state.AccelerometerPitchOffset = state.FusedPitch -
                                                 GetAccelerometerPitch(accelX, accelY, accelZ);
                state.HasSensorReference = true;
            }

            if (compassValid)
            {
                state.CompassHeadingReference = compassHeading;
                state.CompassYawReference = state.FusedYaw;
                state.HasCompassReference = true;
            }

            state.LastFusionSampleTime = Time.realtimeSinceStartup;
        }

        private static bool IsValidAcceleration(float x, float y, float z)
        {
            var magnitude = Mathf.Sqrt(x * x + y * y + z * z);
            return magnitude >= 0.5f && magnitude <= 20f;
        }

        private static float GetAccelerometerPitch(float x, float y, float z)
        {
            return Mathf.Atan2(-x, Mathf.Sqrt(y * y + z * z)) * Mathf.Rad2Deg;
        }

        private void CloseClient(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= phoneStates.Length)
                return;
            var state = phoneStates[playerIndex];
            TcpClient activeClient;
            lock (clientLock)
            {
                state.Connected = false;
                state.HasValidInput = false;
                state.Generation++;
                activeClient = state.Client;
                state.Client = null;
            }
            activeClient?.Close();
            ClearPendingMessages(playerIndex);
            ResetInput(state);
        }

        private static void ResetInput(PhoneState state)
        {
            state.CurrentInput = default;
            ResetAimDelta(state);
            state.LastJump = false;
            state.LastReload = false;
            state.LastAbility = false;
            state.LastUltimate = false;
            state.JumpPressed = false;
            state.ReloadPressed = false;
            state.AbilityPressed = false;
            state.UltimatePressed = false;
        }

        private TaskCompletionSource<string> EnqueueMessage(string line, int playerIndex, int generation)
        {
            var completion = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            lock (messageLock)
            {
                if (pendingMessages.Count >= MaximumQueuedMessages)
                    pendingMessages.Dequeue().Completion.TrySetResult("input queue overflowed");
                pendingMessages.Enqueue(new PendingMessage
                {
                    Line = line,
                    PlayerIndex = playerIndex,
                    Generation = generation,
                    Completion = completion
                });
            }
            return completion;
        }

        private bool TryDequeueMessage(out PendingMessage message)
        {
            lock (messageLock)
            {
                if (pendingMessages.Count > 0)
                {
                    message = pendingMessages.Dequeue();
                    return true;
                }
            }

            message = default;
            return false;
        }

        private void ClearPendingMessages(int playerIndex = -1)
        {
            lock (messageLock)
            {
                var retained = new Queue<PendingMessage>();
                while (pendingMessages.Count > 0)
                {
                    var message = pendingMessages.Dequeue();
                    if (playerIndex < 0 || message.PlayerIndex == playerIndex)
                        message.Completion.TrySetResult("connection closed before packet processing");
                    else
                        retained.Enqueue(message);
                }
                while (retained.Count > 0)
                    pendingMessages.Enqueue(retained.Dequeue());
            }
        }

        private void SetNetworkWarning(string message)
        {
            lock (messageLock)
                pendingNetworkWarning = message;
        }

        private string TakeNetworkWarning()
        {
            lock (messageLock)
            {
                var message = pendingNetworkWarning;
                pendingNetworkWarning = null;
                return message;
            }
        }

        private string ReadLine(NetworkStream stream)
        {
            var builder = new StringBuilder();
            var readBuffer = new byte[1];
            while (!stopping)
            {
                var read = stream.Read(readBuffer, 0, 1);
                if (read == 0)
                    return null;
                if (readBuffer[0] == (byte)'\n')
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] == '\r')
                        builder.Length--;
                    return builder.ToString();
                }
                if (readBuffer[0] > 127 || builder.Length >= MaximumLineLength)
                    return null;
                builder.Append((char)readBuffer[0]);
            }

            return null;
        }

        private static void WriteLine(NetworkStream stream, string message)
        {
            var bytes = Encoding.ASCII.GetBytes(message + "\n");
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        private void OnDisable()
        {
            Application.quitting -= OnApplicationQuitting;
            StopServer();
        }

        private void OnDestroy()
        {
            Application.quitting -= OnApplicationQuitting;
            StopServer();
            if (Active == this)
                Active = null;
        }

        private void OnApplicationQuit()
        {
            StopServer();
        }

        private void OnApplicationQuitting()
        {
            StopServer();
        }

        private void StopServer()
        {
            stopping = true;
            var activeClients = new TcpClient[phoneStates.Length];
            var activeClientThreads = new Thread[phoneStates.Length];
            lock (clientLock)
            {
                for (var i = 0; i < phoneStates.Length; i++)
                {
                    var state = phoneStates[i];
                    state.Generation++;
                    state.Connected = false;
                    state.HasValidInput = false;
                    activeClients[i] = state.Client;
                    activeClientThreads[i] = state.ClientThread;
                    state.Client = null;
                    state.ClientThread = null;
                }
            }
            foreach (var activeClient in activeClients)
                activeClient?.Close();
            try
            {
                listener?.Stop();
            }
            catch (SocketException exception)
            {
                Debug.LogWarning($"Phone controller listener shutdown reported: {exception.Message}", this);
            }

            for (var i = 0; i < activeClientThreads.Length; i++)
            {
                var activeClientThread = activeClientThreads[i];
                if (activeClientThread != null && activeClientThread.IsAlive &&
                    Thread.CurrentThread != activeClientThread && !activeClientThread.Join(500))
                    Debug.LogWarning($"Phone {i + 1} controller did not stop within 500 ms.", this);
            }

            if (listenerThread != null && listenerThread.IsAlive &&
                Thread.CurrentThread != listenerThread && !listenerThread.Join(500))
                Debug.LogWarning("Phone controller listener did not stop within 500 ms.", this);

            listener = null;
            listenerThread = null;
            foreach (var state in phoneStates)
                ResetInput(state);
            ClearPendingMessages();
        }

        private static bool Consume(ref bool value)
        {
            var result = value;
            value = false;
            return result;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static string GetBestLocalIPv4()
        {
            lock (LocalIPv4CacheLock)
            {
                var now = Time.realtimeSinceStartup;
                if (hasCachedLocalIPv4 && now < localIPv4CacheExpiresAt)
                    return cachedLocalIPv4;

                cachedLocalIPv4 = FindBestLocalIPv4();
                localIPv4CacheExpiresAt = now + LocalIPv4CacheSeconds;
                hasCachedLocalIPv4 = true;
                return cachedLocalIPv4;
            }
        }

        private static string FindBestLocalIPv4()
        {
            foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != OperationalStatus.Up ||
                    (network.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                     network.NetworkInterfaceType != NetworkInterfaceType.Ethernet) ||
                    IsExcludedNetwork(network) ||
                    !HasDefaultGateway(network))
                    continue;

                var address = GetIPv4Address(network);
                if (address != null)
                    return address;
            }

            return "unavailable";
        }

        private static bool HasDefaultGateway(NetworkInterface network)
        {
            foreach (var gateway in network.GetIPProperties().GatewayAddresses)
            {
                if (!gateway.Address.Equals(IPAddress.Any) &&
                    !gateway.Address.Equals(IPAddress.IPv6Any))
                    return true;
            }

            return false;
        }

        private static bool IsExcludedNetwork(NetworkInterface network)
        {
            var excludedNames = new[]
            {
                "VMware", "VirtualBox", "Hyper-V", "vEthernet",
                "Cloudflare", "WARP", "TAP", "Tun"
            };
            foreach (var excludedName in excludedNames)
            {
                if (network.Name.IndexOf(excludedName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    network.Description.IndexOf(excludedName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string GetIPv4Address(NetworkInterface network)
        {
            foreach (var address in network.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                var bytes = address.Address.GetAddressBytes();
                if ((bytes[0] == 169 && bytes[1] == 254) || address.Address.Equals(IPAddress.Any) ||
                    IPAddress.IsLoopback(address.Address))
                    continue;
                return address.Address.ToString();
            }

            return null;
        }
    }
}
