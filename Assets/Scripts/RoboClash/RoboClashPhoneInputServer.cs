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
            public int Generation;
            public TaskCompletionSource<string> Completion;
        }

        private readonly Queue<PendingMessage> pendingMessages = new Queue<PendingMessage>();
        private readonly object messageLock = new object();
        private readonly object clientLock = new object();
        private readonly object aimDeltaLock = new object();
        private TcpListener listener;
        private TcpClient client;
        private Thread listenerThread;
        private Thread clientThread;
        private volatile bool stopping;
        private volatile bool connected;
        private string pendingNetworkWarning;
        private bool lastJump;
        private bool lastReload;
        private bool lastAbility;
        private bool lastUltimate;
        private bool jumpPressed;
        private bool reloadPressed;
        private bool abilityPressed;
        private bool ultimatePressed;
        private volatile float lastInputTime;
        private volatile bool hasValidInput;
        private bool reportedPhoneActive;
        private volatile int clientGeneration;
        private float accumulatedGyroYawDelta;
        private float accumulatedGyroPitchDelta;
        private float lastGyroAimDeltaTime;
        private bool hasReceivedGyroAimDeltas;
        private bool hasSensorReference;
        private bool hasCompassReference;
        private float accelerometerPitchOffset;
        private float compassHeadingReference;
        private float compassYawReference;
        private float fusedYaw;
        private float fusedPitch;
        private float lastFusionSampleTime;
        private bool recenterPressed;
        private int invalidPacketWarningGeneration = -1;

        public static RoboClashPhoneInputServer Active { get; private set; }
        public static bool IsPhoneConnected => IsPhoneActive;
        public static bool IsPhoneActive =>
            Active != null &&
            Active.connected &&
            Active.hasValidInput &&
            Time.realtimeSinceStartup - Active.lastInputTime <= InputTimeoutSeconds;
        public static string StatusText => IsPhoneActive
            ? "PHONE: CONNECTED"
            : $"PHONE: WAITING  WIFI {BestLocalIPv4}:{Port}";
        public static string BestLocalIPv4 => GetBestLocalIPv4();

        public bool IsConnected => connected;
        public RoboClashPhoneInput CurrentInput { get; private set; }
        public bool HasRecentGyroAimDeltas
        {
            get
            {
                lock (aimDeltaLock)
                    return hasReceivedGyroAimDeltas &&
                           Time.realtimeSinceStartup - lastGyroAimDeltaTime <= GyroAimDeltaRecentSeconds;
            }
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
                if (pendingMessage.Generation != clientGeneration || !connected)
                {
                    pendingMessage.Completion.TrySetResult("connection changed while processing packet");
                    continue;
                }

                if (!TryApplyMessage(pendingMessage.Line, out var rejectionReason))
                {
                    if (invalidPacketWarningGeneration != pendingMessage.Generation)
                    {
                        invalidPacketWarningGeneration = pendingMessage.Generation;
                        Debug.LogWarning(
                            $"RoboClash phone controller sent an invalid input packet: {rejectionReason}. " +
                            $"Packet length: {pendingMessage.Line.Length}.",
                            this);
                    }
                    pendingMessage.Completion.TrySetResult(rejectionReason);
                    continue;
                }

                invalidPacketWarningGeneration = -1;
                lastInputTime = Time.realtimeSinceStartup;
                hasValidInput = true;
                pendingMessage.Completion.TrySetResult(null);
            }

            if (CurrentInput.Guard &&
                Time.realtimeSinceStartup - lastInputTime > GuardInputTimeoutSeconds)
            {
                var input = CurrentInput;
                input.Guard = false;
                CurrentInput = input;
            }

            if (connected && hasValidInput &&
                Time.realtimeSinceStartup - lastInputTime > InputTimeoutSeconds)
            {
                Debug.LogWarning("RoboClash phone controller timed out; PC controls are active.", this);
                CloseClient();
            }

            if (!IsPhoneActive)
                ResetInput();

            var phoneActive = IsPhoneActive;
            if (reportedPhoneActive != phoneActive)
            {
                reportedPhoneActive = phoneActive;
                Debug.Log(phoneActive
                    ? "RoboClash phone controller connected; phone controls are active."
                    : "RoboClash phone controller disconnected; PC controls are active.", this);
            }
        }

        public bool ConsumePressed(RoboClashPhoneAction action)
        {
            switch (action)
            {
                case RoboClashPhoneAction.Jump:
                    return Consume(ref jumpPressed);
                case RoboClashPhoneAction.Reload:
                    return Consume(ref reloadPressed);
                case RoboClashPhoneAction.Ability:
                    return Consume(ref abilityPressed);
                case RoboClashPhoneAction.Ultimate:
                    return Consume(ref ultimatePressed);
                default:
                    return false;
            }
        }

        public bool ConsumeRecenterPressed()
        {
            return Consume(ref recenterPressed);
        }

        public void RecenterAim()
        {
            var input = CurrentInput;
            lock (aimDeltaLock)
            {
                SetSensorReference(input.AccelerometerValid, input.AccelerometerX,
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
                    TcpClient previousClient;
                    Thread previousThread;
                    int generation;
                    lock (clientLock)
                    {
                        previousClient = client;
                        previousThread = clientThread;
                        generation = ++clientGeneration;
                        client = acceptedClient;
                        clientThread = null;
                        connected = false;
                        hasValidInput = false;
                        ResetAimDelta();
                    }

                    previousClient?.Close();
                    ClearPendingMessages();
                    if (previousThread != null && previousThread.IsAlive &&
                        Thread.CurrentThread != previousThread && !previousThread.Join(500))
                        Debug.LogWarning("Previous phone controller connection did not stop within 500 ms.", this);

                    var connectionThread = new Thread(() => HandleController(acceptedClient, generation))
                    {
                        IsBackground = true,
                        Name = "RoboClash Phone Controller Client"
                    };
                    lock (clientLock)
                    {
                        if (generation != clientGeneration || stopping)
                        {
                            acceptedClient.Close();
                            continue;
                        }
                        clientThread = connectionThread;
                    }
                    connectionThread.Start();
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
                    if (acceptedClient != null && !IsCurrentClient(acceptedClient, clientGeneration))
                        acceptedClient.Close();
                }
            }
        }

        private void HandleController(TcpClient acceptedClient, int generation)
        {
            try
            {
                using (var stream = acceptedClient.GetStream())
                {
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

                        var completion = EnqueueMessage(line, generation);
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
                if (!stopping && IsCurrentClient(acceptedClient, generation))
                    SetNetworkWarning("The phone controller socket closed unexpectedly.");
            }
            catch (SocketException)
            {
                if (!stopping && IsCurrentClient(acceptedClient, generation))
                    SetNetworkWarning("The phone controller network connection ended.");
            }
            catch (System.IO.IOException)
            {
                if (!stopping && IsCurrentClient(acceptedClient, generation))
                    SetNetworkWarning("The phone controller stream closed.");
            }
            finally
            {
                var disconnectedCurrentClient = false;
                lock (clientLock)
                {
                    if (client == acceptedClient && generation == clientGeneration)
                    {
                        client = null;
                        clientThread = null;
                        connected = false;
                        hasValidInput = false;
                        ClearPendingMessages();
                        disconnectedCurrentClient = true;
                    }
                }
                if (disconnectedCurrentClient)
                    ResetAimDelta();
                acceptedClient.Close();
            }
        }

        private bool IsCurrentClient(TcpClient candidate, int generation)
        {
            lock (clientLock)
                return !stopping && client == candidate && clientGeneration == generation;
        }

        private bool TryApplyMessage(string line, out string rejectionReason)
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
            CurrentInput = new RoboClashPhoneInput
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
            lock (aimDeltaLock)
            {
                if (message.recenter)
                {
                    SetSensorReference(CurrentInput.AccelerometerValid,
                        CurrentInput.AccelerometerX, CurrentInput.AccelerometerY,
                        CurrentInput.AccelerometerZ, CurrentInput.CompassValid,
                        CurrentInput.CompassHeading);
                    recenterPressed = true;
                }

                ApplySensorFusion(yawDelta, pitchDelta, hasGyroAimDeltas,
                    CurrentInput.GyroTurn, CurrentInput.GyroPitch,
                    CurrentInput.AccelerometerValid, CurrentInput.AccelerometerX,
                    CurrentInput.AccelerometerY, CurrentInput.AccelerometerZ,
                    CurrentInput.CompassValid, CurrentInput.CompassHeading);
            }
            jumpPressed |= message.jump && !lastJump;
            reloadPressed |= message.reload && !lastReload;
            abilityPressed |= message.ability && !lastAbility;
            ultimatePressed |= message.ultimate && !lastUltimate;
            lastJump = message.jump;
            lastReload = message.reload;
            lastAbility = message.ability;
            lastUltimate = message.ultimate;
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
            lock (aimDeltaLock)
            {
                yawDeg = accumulatedGyroYawDelta;
                pitchDeg = accumulatedGyroPitchDelta;
                accumulatedGyroYawDelta = 0f;
                accumulatedGyroPitchDelta = 0f;
            }
        }

        private void ResetAimDelta()
        {
            lock (aimDeltaLock)
            {
                accumulatedGyroYawDelta = 0f;
                accumulatedGyroPitchDelta = 0f;
                lastGyroAimDeltaTime = 0f;
                hasReceivedGyroAimDeltas = false;
                hasSensorReference = false;
                hasCompassReference = false;
                fusedYaw = 0f;
                fusedPitch = 0f;
                lastFusionSampleTime = 0f;
                recenterPressed = false;
            }
        }

        private void ApplySensorFusion(float gyroYawDelta, float gyroPitchDelta,
            bool hasGyroDelta, float gyroTurn, float gyroPitchRate,
            bool accelerometerValid, float accelX, float accelY,
            float accelZ, bool compassValid, float compassHeading)
        {
            var now = Time.realtimeSinceStartup;
            var elapsed = lastFusionSampleTime > 0f
                ? Mathf.Clamp(now - lastFusionSampleTime, 0.001f, 0.25f)
                : 0.033f;
            lastFusionSampleTime = now;

            if (!hasGyroDelta && (accelerometerValid || compassValid))
            {
                gyroYawDelta = gyroTurn * 140f * elapsed;
                gyroPitchDelta = gyroPitchRate * 90f * elapsed;
                hasGyroDelta = true;
            }

            var startYaw = fusedYaw;
            var startPitch = fusedPitch;
            if (hasGyroDelta)
            {
                fusedYaw += gyroYawDelta;
                fusedPitch += gyroPitchDelta;
            }

            if (accelerometerValid)
            {
                var measuredPitch = GetAccelerometerPitch(accelX, accelY, accelZ);
                if (!hasSensorReference)
                    SetSensorReference(true, accelX, accelY, accelZ, compassValid, compassHeading);
                var correctedPitch = measuredPitch + accelerometerPitchOffset;
                var correction = 1f - Mathf.Exp(-elapsed / SensorFusionTimeConstant);
                fusedPitch = Mathf.Lerp(fusedPitch, correctedPitch, correction);
            }

            if (compassValid)
            {
                if (!hasCompassReference)
                    SetSensorReference(accelerometerValid, accelX, accelY, accelZ,
                        true, compassHeading);
                var correctedYaw = compassYawReference +
                                   Mathf.DeltaAngle(compassHeadingReference, compassHeading);
                var correction = 1f - Mathf.Exp(-elapsed / SensorFusionTimeConstant);
                fusedYaw = Mathf.Lerp(fusedYaw, correctedYaw, correction);
            }

            var fusedYawDelta = fusedYaw - startYaw;
            var fusedPitchDelta = fusedPitch - startPitch;
            var pendingYaw = accumulatedGyroYawDelta + fusedYawDelta;
            var pendingPitch = accumulatedGyroPitchDelta + fusedPitchDelta;
            if (!IsFinite(pendingYaw) || !IsFinite(pendingPitch))
                return;

            accumulatedGyroYawDelta = pendingYaw;
            accumulatedGyroPitchDelta = pendingPitch;
            if (hasGyroDelta || accelerometerValid || compassValid)
            {
                hasReceivedGyroAimDeltas = true;
                lastGyroAimDeltaTime = now;
            }
        }

        private void SetSensorReference(bool accelerometerValid, float accelX,
            float accelY, float accelZ, bool compassValid, float compassHeading)
        {
            if (accelerometerValid)
            {
                accelerometerPitchOffset = fusedPitch -
                                           GetAccelerometerPitch(accelX, accelY, accelZ);
                hasSensorReference = true;
            }

            if (compassValid)
            {
                compassHeadingReference = compassHeading;
                compassYawReference = fusedYaw;
                hasCompassReference = true;
            }

            lastFusionSampleTime = Time.realtimeSinceStartup;
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

        private void CloseClient()
        {
            connected = false;
            hasValidInput = false;
            lock (clientLock)
            {
                clientGeneration++;
                if (client != null)
                {
                    client.Close();
                    client = null;
                }
            }
            ClearPendingMessages();
            ResetAimDelta();
            ResetInput();
        }

        private void ResetInput()
        {
            CurrentInput = default;
            ResetAimDelta();
            lastJump = false;
            lastReload = false;
            lastAbility = false;
            lastUltimate = false;
            jumpPressed = false;
            reloadPressed = false;
            abilityPressed = false;
            ultimatePressed = false;
        }

        private TaskCompletionSource<string> EnqueueMessage(string line, int generation)
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

        private void ClearPendingMessages()
        {
            lock (messageLock)
            {
                while (pendingMessages.Count > 0)
                    pendingMessages.Dequeue().Completion.TrySetResult(
                        "connection closed before packet processing");
                pendingMessages.Clear();
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
            connected = false;
            hasValidInput = false;
            TcpClient activeClient;
            Thread activeClientThread;
            lock (clientLock)
            {
                clientGeneration++;
                activeClient = client;
                activeClientThread = clientThread;
                client = null;
                clientThread = null;
            }
            activeClient?.Close();
            try
            {
                listener?.Stop();
            }
            catch (SocketException exception)
            {
                Debug.LogWarning($"Phone controller listener shutdown reported: {exception.Message}", this);
            }

            if (activeClientThread != null && activeClientThread.IsAlive &&
                Thread.CurrentThread != activeClientThread && !activeClientThread.Join(500))
                Debug.LogWarning("Phone controller client did not stop within 500 ms.", this);

            if (listenerThread != null && listenerThread.IsAlive &&
                Thread.CurrentThread != listenerThread && !listenerThread.Join(500))
                Debug.LogWarning("Phone controller listener did not stop within 500 ms.", this);

            listener = null;
            listenerThread = null;
            ResetInput();
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
