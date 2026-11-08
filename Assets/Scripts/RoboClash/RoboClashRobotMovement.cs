using System.Collections;
using UnityEngine;

namespace RoboClash
{
    public enum RoboClashCameraState
    {
        Intro,
        FirstPerson,
        ThirdPerson,
        Dead,
        Respawning,
        Disabled
    }

    [RequireComponent(typeof(CharacterController), typeof(RoboClashRobotDefinition))]
    public sealed class RoboClashRobotMovement : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0f)] private float jumpHeight = 1.6f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float phoneYawScale = 1f;
        [SerializeField, Min(0f)] private float phonePitchScale = 1f;
        [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.22f;
        [SerializeField, Min(0f)] private float cameraCollisionPadding = 0.16f;
        [SerializeField, Min(1f)] private float thirdPersonDistance = 4.5f;
        [SerializeField, Min(0f)] private float thirdPersonHeight = 0.3f;
        [Header("Third person aiming")]
        [SerializeField] private bool bodyFollowsAim = true;
        [SerializeField] private float shoulderOffset = 0.8f;
        [SerializeField] private float idleTurnThresholdDeg = 60f;
        [SerializeField] private float firingTurnSpeed = 720f;
        [SerializeField] private float focusSmoothTime = 0.08f;
        [SerializeField] private float cameraCollisionInSpeed = 40f;
        [SerializeField] private float cameraCollisionOutSpeed = 6f;
        [SerializeField, Range(0.5f, 0.75f)] private float thirdPersonAimViewportHeight = 0.62f;
        [SerializeField] private bool playerControlled;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform torso;

        private CharacterController characterController;
        private RoboClashRobotDefinition definition;
        private ArenaMovingPlatform currentPlatform;
        private ArenaMovingPlatform platformContact;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float yaw;
        private float pitch = 12f;
        private RoboClashRobotAnimator animator;
        private RoboClashRobotAudio audioPlayer;
        private RoboClashRobotAbilities abilities;
        private RoboClashRobotCombat combat;
        private RoboClashPlayerInputRouter inputRouter;
        private KeyboardMouseInputSource defaultInputSource;
        private readonly RaycastHit[] cameraObstructionHits = new RaycastHit[16];
        private float nextFootstepTime;
        private bool matchInputLocked;
        private bool abilityInputLocked;
        private bool cameraTransitionInputLocked;
        private Transform introCameraAnchor;
        private Transform firstPersonCameraAnchor;
        private Renderer[] firstPersonRenderers = new Renderer[0];
        private bool[] firstPersonRendererDefaults = new bool[0];
        private bool firstPersonPresentationActive;
        private RoboClashCameraState cameraState = RoboClashCameraState.Disabled;
        private RoboClashCameraMode cameraMode = RoboClashCameraMode.FirstPerson;
        private float cameraTransitionDuration;
        private float cameraTransitionElapsed;
        private float fovKick;
        private float kickPitch;
        private float shake;
        private float speedMultiplier = 1f;
        private Vector3 smoothedFocus;
        private Vector3 focusVelocity;
        private Vector3 cameraPositionVelocity;
        private float currentCameraDistance;
        private bool cameraSmoothingInitialized;

        public Camera PlayerCamera => playerCamera;
        public Vector3 CameraFocusPoint => GetCameraFocus();
        public bool IsThirdPerson => cameraMode == RoboClashCameraMode.ThirdPerson;
        public Vector2 AimViewportPoint => IsThirdPerson
            ? new Vector2(0.5f, Mathf.Clamp(thirdPersonAimViewportHeight, 0.5f, 0.75f))
            : new Vector2(0.5f, 0.5f);
        public IRoboClashInputSource InputSource
        {
            get
            {
                if (inputRouter == null)
                    inputRouter = GetComponent<RoboClashPlayerInputRouter>();
                if (inputRouter != null && inputRouter.Source != null)
                    return inputRouter.Source;
                if (defaultInputSource == null)
                    defaultInputSource = new KeyboardMouseInputSource(mouseSensitivity);
                return defaultInputSource;
            }
        }
        public Vector3 AimDirection => Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        private bool CanControlCursor => inputRouter == null || inputRouter.PlayerIndex == 0;
        public float AimPitch => pitch;
        public float Yaw => yaw;
        public bool PlayerControlled => playerControlled;
        public float SpeedMultiplier
        {
            get => speedMultiplier;
            set => speedMultiplier = !float.IsNaN(value) && !float.IsInfinity(value)
                ? Mathf.Max(0f, value)
                : 1f;
        }
        public RoboClashCameraState CameraState => cameraState;
        public bool ControlsLocked
        {
            get => matchInputLocked || abilityInputLocked || cameraTransitionInputLocked;
            set => abilityInputLocked = value;
        }
        public float HorizontalSpeed => new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            definition = GetComponent<RoboClashRobotDefinition>();
            animator = GetComponent<RoboClashRobotAnimator>();
            audioPlayer = GetComponent<RoboClashRobotAudio>();
            abilities = GetComponent<RoboClashRobotAbilities>();
            combat = GetComponent<RoboClashRobotCombat>();
            yaw = transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            ResetCameraSmoothing();
        }

        public void ConfigurePlayer(Camera camera, bool enabledForPlayer)
        {
            playerCamera = camera;
            playerControlled = enabledForPlayer;
            cameraMode = RoboClashSession.CameraMode;
            ResetCameraSmoothing();
            definition?.EnsureCameraAnchors();
            introCameraAnchor = definition != null ? definition.introCameraAnchor : null;
            firstPersonCameraAnchor = definition != null ? definition.firstPersonCameraAnchor : null;

            if (!enabledForPlayer || camera == null)
            {
                SetFirstPersonPresentation(false);
                cameraState = enabledForPlayer
                    ? cameraMode == RoboClashCameraMode.ThirdPerson
                        ? RoboClashCameraState.ThirdPerson
                        : RoboClashCameraState.FirstPerson
                    : RoboClashCameraState.Disabled;
                cameraTransitionInputLocked = false;
                return;
            }

            CacheFirstPersonRenderers();
            if (definition.aimPivot != null)
                definition.aimPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            BeginSpawnIntro();
        }

        public void SetCameraMode(RoboClashCameraMode mode)
        {
            var selectedMode = System.Enum.IsDefined(typeof(RoboClashCameraMode), mode)
                ? mode
                : RoboClashCameraMode.FirstPerson;
            if (cameraMode != selectedMode)
                ResetCameraSmoothing();
            cameraMode = selectedMode;
            if (!playerControlled || playerCamera == null ||
                cameraState == RoboClashCameraState.Intro ||
                cameraState == RoboClashCameraState.Respawning ||
                cameraState == RoboClashCameraState.Dead ||
                cameraState == RoboClashCameraState.Disabled)
                return;

            cameraTransitionInputLocked = false;
            EnterSelectedCameraMode();
        }

        public void SetControlled(bool controlled)
        {
            playerControlled = controlled;
            horizontalVelocity = Vector3.zero;
            if (controlled)
            {
                yaw = transform.eulerAngles.y;
                pitch = 12f;
                if (definition.aimPivot != null)
                    definition.aimPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            else
            {
                currentPlatform = null;
                platformContact = null;
            }
        }

        public void SetMatchInputLocked(bool locked)
        {
            matchInputLocked = locked;
        }

        public void BeginSpawnIntro()
        {
            animator?.TriggerSpawnReady(1.5f);
            BeginCameraTransition(RoboClashCameraState.Intro, 1.5f);
        }

        public void BeginRespawnCameraTransition()
        {
            ResetCameraSmoothing();
            animator?.TriggerSpawnReady(0.65f);
            BeginCameraTransition(RoboClashCameraState.Respawning, 0.65f);
        }

        public void SetCameraDead()
        {
            cameraTransitionInputLocked = playerCamera != null;
            cameraState = playerCamera != null
                ? RoboClashCameraState.Dead
                : RoboClashCameraState.Disabled;
            SetFirstPersonPresentation(false);

            if (playerCamera == null)
                return;

            if (introCameraAnchor != null)
            {
                var lookTarget = transform.position + Vector3.up * 0.9f;
                var lookDirection = lookTarget - introCameraAnchor.position;
                playerCamera.transform.SetPositionAndRotation(
                    introCameraAnchor.position,
                    lookDirection.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(lookDirection, Vector3.up)
                        : introCameraAnchor.rotation);
            }
        }

        public void SetCameraDisabled()
        {
            cameraTransitionInputLocked = false;
            cameraState = RoboClashCameraState.Disabled;
        }

        public void ResetMotion()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            currentPlatform = null;
            platformContact = null;
        }

        public void ApplyImpulse(Vector3 impulse)
        {
            horizontalVelocity += new Vector3(impulse.x, 0f, impulse.z) *
                                  Mathf.Clamp01(1f - definition.stats.knockbackResistance);
        }

        public bool TryDash(Vector3 direction, float distance)
        {
            return TryDash(direction, distance, out _);
        }

        public bool TryDash(Vector3 direction, float distance, out Vector3 endPosition)
        {
            endPosition = transform.position;
            if (characterController == null || !characterController.enabled || ControlsLocked ||
                distance <= 0f)
                return false;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = transform.forward;
            direction.Normalize();

            var radius = Mathf.Max(0.1f, characterController.radius * 0.9f);
            var origin = transform.position + characterController.center;
            if (Physics.SphereCast(origin, radius, direction, out var hit, distance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0f, hit.distance - radius * 0.2f);

            if (distance <= 0.1f)
                return false;

            endPosition = transform.position + direction * distance;
            fovKick = 1f;
            ResetCameraSmoothing();
            StartCoroutine(DashRoutine(direction, distance, 0.12f));
            return true;
        }

        private IEnumerator DashRoutine(Vector3 direction, float distance, float duration)
        {
            var elapsed = 0f;
            var moved = 0f;
            while (elapsed < duration && characterController != null && characterController.enabled)
            {
                elapsed += Time.deltaTime;
                var target = distance * Mathf.Clamp01(elapsed / duration);
                var step = target - moved;
                if (step > 0f)
                    characterController.Move(direction * step);
                moved = target;
                yield return null;
            }
            ResetCameraSmoothing();
        }

        public void AddCameraKick(float pitchDegrees, float shakeAmount)
        {
            if (!playerControlled)
                return;

            kickPitch = Mathf.Min(kickPitch + pitchDegrees, 3f);
            shake = Mathf.Clamp01(Mathf.Max(shake, shakeAmount));
        }

        private void UpdateCameraFeedback()
        {
            var dt = Time.deltaTime;
            kickPitch = Mathf.MoveTowards(kickPitch, 0f, 10f * dt);
            shake = Mathf.MoveTowards(shake, 0f, 3f * dt);
            fovKick = Mathf.MoveTowards(fovKick, 0f, 6f * dt);
            if (playerCamera != null)
                playerCamera.fieldOfView = RoboClashSession.FieldOfView + fovKick * 12f;
        }

        private Quaternion GetCameraRotation()
        {
            var jitter = shake > 0.001f
                ? new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)) * (shake * 1.2f)
                : Vector3.zero;
            return Quaternion.Euler(pitch - kickPitch + jitter.x, yaw + jitter.y, jitter.z);
        }

        public bool TryTeleport(Vector3 destination)
        {
            if (characterController == null || !characterController.enabled || ControlsLocked)
                return false;

            var oldPosition = transform.position;
            var distance = Vector3.Distance(oldPosition, destination);
            var hasSafeGround = false;
            foreach (var groundHit in Physics.RaycastAll(destination + Vector3.up,
                         Vector3.down, 5f, Physics.DefaultRaycastLayers,
                         QueryTriggerInteraction.Ignore))
            {
                if (groundHit.normal.y < 0.5f ||
                    groundHit.collider.GetComponentInParent<ArenaDamageable>() != null ||
                    groundHit.collider.GetComponentInParent<ArenaHazard>() != null ||
                    Mathf.Abs(groundHit.point.y - destination.y) > 1.25f)
                    continue;

                hasSafeGround = true;
                break;
            }

            if (!hasSafeGround)
                return false;

            foreach (var hit in Physics.CapsuleCastAll(
                         oldPosition + Vector3.up * 0.35f,
                         oldPosition + Vector3.up * (characterController.height - 0.2f),
                         characterController.radius * 0.9f,
                         distance > 0.01f ? (destination - oldPosition).normalized : transform.forward,
                         distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.root != transform.root &&
                    hit.collider.GetComponentInParent<ArenaDamageable>() == null)
                    return false;
            }

            var bottom = destination + Vector3.up * 0.05f;
            var top = destination + Vector3.up * (characterController.height - 0.1f);
            foreach (var collider in Physics.OverlapCapsule(bottom, top, characterController.radius * 0.9f,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (collider.transform.root != transform.root &&
                    collider.GetComponentInParent<ArenaDamageable>() == null)
                    return false;
            }

            var controllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
            transform.position = destination;
            characterController.enabled = controllerWasEnabled;
            horizontalVelocity = Vector3.zero;
            ResetCameraSmoothing();
            return true;
        }

        public void SetVisualParts(Transform left, Transform right, Transform body)
        {
            leftLeg = left;
            rightLeg = right;
            torso = body;
        }

        public void DriveAI(Vector3 movementDirection, Vector3 aimDirection, float aimTurnSpeed = 0f)
        {
            playerControlled = false;
            if (matchInputLocked)
            {
                HaltHorizontalMotion();
                return;
            }

            if (characterController == null || !characterController.enabled || definition == null)
                return;

            if (abilityInputLocked)
            {
                ApplyLockedMovement();
                return;
            }

            movementDirection.y = 0f;
            aimDirection.y = 0f;
            var speedMultiplier = abilities != null ? abilities.GetMoveSpeedMultiplier() : 1f;
            var desired = movementDirection.sqrMagnitude > 0.01f
                ? movementDirection.normalized * definition.stats.moveSpeed * speedMultiplier
                : Vector3.zero;
            ApplyMovement(desired, false);

            if (aimDirection.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(aimDirection, Vector3.up),
                    (aimTurnSpeed > 0f ? aimTurnSpeed : definition.stats.rotationSpeed) * Time.deltaTime);
        }

        private void Update()
        {
            if (!playerControlled)
                return;

            var inputSource = InputSource;
            var jumpPressed = inputSource.JumpPressed;
            if (!RoboClashSceneUtility.IsGameplayScene(gameObject.scene))
            {
                if (CanControlCursor)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            if (matchInputLocked || characterController == null || !characterController.enabled)
            {
                HaltHorizontalMotion();
                return;
            }
            if (cameraTransitionInputLocked ||
                (cameraState != RoboClashCameraState.FirstPerson &&
                 cameraState != RoboClashCameraState.ThirdPerson) ||
                definition == null)
            {
                ApplyLockedMovement();
                return;
            }
            if (abilityInputLocked)
            {
                ApplyLockedMovement();
                return;
            }

            if (CanControlCursor && inputSource is KeyboardMouseInputSource keyboardInput &&
                keyboardInput.CursorCapturePressed && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (CanControlCursor && inputSource is KeyboardMouseInputSource keyboardSource &&
                keyboardSource.CursorUnlockPressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            var thirdPerson = cameraMode == RoboClashCameraMode.ThirdPerson;
            var look = inputSource.LookDelta;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y,
                thirdPerson ? -30f : -20f, thirdPerson ? 60f : 45f);
            if (inputSource is KeyboardMouseInputSource phoneInput)
            {
                phoneInput.ConsumePhoneAimRecenter();
                phoneInput.ApplyPhoneAim(ref yaw, ref pitch, phoneYawScale, phonePitchScale);
            }
            if (thirdPerson)
                pitch = Mathf.Clamp(pitch, -30f, 60f);

            var input = inputSource.Move;
            var speedMultiplier = abilities != null ? abilities.GetMoveSpeedMultiplier() : 1f;
            var moveInput = new Vector3(input.x, 0f, input.y);
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();
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
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(horizontalDesired.normalized, Vector3.up),
                        definition.stats.rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            if (definition.aimPivot != null)
            {
                var aimYawOffset = thirdPerson
                    ? Mathf.Clamp(Mathf.DeltaAngle(transform.eulerAngles.y, yaw), -90f, 90f)
                    : 0f;
                definition.aimPivot.localRotation = Quaternion.Euler(pitch, aimYawOffset, 0f);
            }

        }

        private void LateUpdate()
        {
            if (!playerControlled || playerCamera == null)
                return;

            if (cameraMode != RoboClashSession.CameraMode)
                SetCameraMode(RoboClashSession.CameraMode);

            if (cameraState == RoboClashCameraState.Intro ||
                cameraState == RoboClashCameraState.Respawning)
            {
                UpdateCameraTransition();
                return;
            }

            if (cameraState != RoboClashCameraState.FirstPerson &&
                cameraState != RoboClashCameraState.ThirdPerson)
                return;

            UpdateSelectedCameraPose();
        }

        private void BeginCameraTransition(RoboClashCameraState transitionState, float duration)
        {
            if (!playerControlled || playerCamera == null || firstPersonCameraAnchor == null)
            {
                cameraState = playerControlled && playerCamera != null
                    ? cameraMode == RoboClashCameraMode.FirstPerson
                        ? RoboClashCameraState.FirstPerson
                        : RoboClashCameraState.ThirdPerson
                    : RoboClashCameraState.Disabled;
                cameraTransitionInputLocked = false;
                SetFirstPersonPresentation(cameraMode == RoboClashCameraMode.FirstPerson);
                return;
            }

            cameraState = transitionState;
            cameraTransitionDuration = Mathf.Max(0.1f, duration);
            cameraTransitionElapsed = 0f;
            cameraTransitionInputLocked = true;
            SetFirstPersonPresentation(false);
            if (introCameraAnchor != null)
                playerCamera.transform.SetPositionAndRotation(introCameraAnchor.position,
                    introCameraAnchor.rotation);
            else
                EnterSelectedCameraMode(true);
        }

        private void UpdateCameraTransition()
        {
            if (introCameraAnchor == null || firstPersonCameraAnchor == null)
            {
                EnterSelectedCameraMode(true);
                return;
            }

            cameraTransitionElapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(cameraTransitionElapsed / cameraTransitionDuration);
            var eased = t * t * (3f - 2f * t);
            var focus = GetCameraFocus();
            var targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            var targetPosition = cameraMode == RoboClashCameraMode.FirstPerson &&
                                 firstPersonCameraAnchor != null
                ? firstPersonCameraAnchor.position
                : GetThirdPersonCameraPosition(focus, targetRotation);
            targetPosition = ResolveCameraPosition(focus, targetPosition, out _);
            var cameraPosition = Vector3.Lerp(introCameraAnchor.position, targetPosition, eased);
            playerCamera.transform.SetPositionAndRotation(cameraPosition,
                Quaternion.Slerp(introCameraAnchor.rotation, targetRotation, eased));

            SetFirstPersonPresentation(
                cameraMode == RoboClashCameraMode.FirstPerson && t >= 0.82f);
            if (t >= 1f)
                EnterSelectedCameraMode(true);
        }

        private void EnterSelectedCameraMode(bool captureCursor = false)
        {
            cameraState = cameraMode == RoboClashCameraMode.FirstPerson
                ? RoboClashCameraState.FirstPerson
                : RoboClashCameraState.ThirdPerson;
            cameraTransitionInputLocked = false;
            UpdateSelectedCameraPose();

            if (captureCursor && playerControlled && CanControlCursor &&
                InputSource is KeyboardMouseInputSource &&
                RoboClashSceneUtility.IsGameplayScene(gameObject.scene))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void UpdateSelectedCameraPose()
        {
            UpdateCameraFeedback();
            var rotation = GetCameraRotation();
            if (cameraMode == RoboClashCameraMode.ThirdPerson)
            {
                UpdateThirdPersonCamera(rotation);
                return;
            }

            if (firstPersonCameraAnchor != null)
            {
                SetFirstPersonPresentation(true);
                playerCamera.transform.SetPositionAndRotation(firstPersonCameraAnchor.position, rotation);
                return;
            }

            SetFirstPersonPresentation(false);
            var focus = GetCameraFocus();
            var position = GetThirdPersonCameraPosition(focus, rotation);
            position = ResolveCameraPosition(focus, position, out _);
            playerCamera.transform.SetPositionAndRotation(position, rotation);
        }

        private Vector3 GetCameraFocus()
        {
            return transform.position + Vector3.up * 1.6f;
        }

        private Vector3 GetThirdPersonCameraPosition(Vector3 focus, Quaternion rotation)
        {
            return focus + rotation * new Vector3(shoulderOffset, thirdPersonHeight,
                -thirdPersonDistance);
        }

        private void UpdateThirdPersonCamera(Quaternion rotation)
        {
            SetFirstPersonPresentation(false);
            var rawFocus = GetCameraFocus();
            if (!cameraSmoothingInitialized)
            {
                smoothedFocus = rawFocus;
                focusVelocity = Vector3.zero;
                cameraPositionVelocity = Vector3.zero;
                var initialPosition = GetThirdPersonCameraPosition(rawFocus, rotation);
                currentCameraDistance = Vector3.Distance(rawFocus, initialPosition);
                cameraSmoothingInitialized = true;
            }

            smoothedFocus = Vector3.SmoothDamp(smoothedFocus, rawFocus, ref focusVelocity,
                Mathf.Max(0.001f, focusSmoothTime));
            var desiredPosition = GetThirdPersonCameraPosition(smoothedFocus, rotation);
            var resolved = ResolveCameraPosition(smoothedFocus, desiredPosition, out _);
            var wantedDistance = Vector3.Distance(smoothedFocus, resolved);
            var speed = wantedDistance < currentCameraDistance
                ? cameraCollisionInSpeed
                : cameraCollisionOutSpeed;
            currentCameraDistance = Mathf.MoveTowards(currentCameraDistance, wantedDistance,
                Mathf.Max(0f, speed) * Time.deltaTime);

            var direction = desiredPosition - smoothedFocus;
            var targetPosition = direction.sqrMagnitude > 0.0001f
                ? smoothedFocus + direction.normalized * currentCameraDistance
                : desiredPosition;
            var position = Vector3.SmoothDamp(playerCamera.transform.position, targetPosition,
                ref cameraPositionVelocity, Mathf.Max(0.001f, focusSmoothTime));
            position = ResolveCameraPosition(smoothedFocus, position, out _);
            playerCamera.transform.SetPositionAndRotation(position, rotation);
        }

        public Vector3 GetCameraAimRayOrigin()
        {
            return playerCamera != null ? playerCamera.transform.position : transform.position;
        }

        public Vector3 GetCameraAimPoint(float maxDistance = 100f)
        {
            var ray = playerCamera != null
                ? playerCamera.ViewportPointToRay(AimViewportPoint)
                : new Ray(transform.position, AimDirection);
            var distance = Mathf.Max(0f, maxDistance);
            var aimPoint = ray.origin + ray.direction * distance;
            var nearestDistance = distance;
            foreach (var hit in Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers,
                         QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null || hit.collider.isTrigger ||
                    hit.transform.root == transform.root ||
                    hit.collider.GetComponentInParent<ArenaProjectile>() != null ||
                    hit.distance >= nearestDistance)
                    continue;

                nearestDistance = hit.distance;
                aimPoint = hit.point;
            }

            return aimPoint;
        }

        private void ResetCameraSmoothing()
        {
            cameraSmoothingInitialized = false;
            focusVelocity = Vector3.zero;
            cameraPositionVelocity = Vector3.zero;
        }

        private void CacheFirstPersonRenderers()
        {
            var modelRoot = definition != null ? definition.modelRoot : null;
            firstPersonRenderers = modelRoot != null
                ? modelRoot.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];
            firstPersonRendererDefaults = new bool[firstPersonRenderers.Length];
            for (var i = 0; i < firstPersonRenderers.Length; i++)
                firstPersonRendererDefaults[i] = firstPersonRenderers[i] != null &&
                                                 firstPersonRenderers[i].enabled;
            firstPersonPresentationActive = false;
        }

        private void SetFirstPersonPresentation(bool active)
        {
            if (firstPersonPresentationActive == active)
                return;

            for (var i = 0; i < firstPersonRenderers.Length; i++)
            {
                var renderer = firstPersonRenderers[i];
                if (renderer == null)
                    continue;

                var weaponMount = definition != null ? definition.weaponMount : null;
                var isWeaponRenderer = weaponMount != null &&
                    (renderer.transform == weaponMount || renderer.transform.IsChildOf(weaponMount));
                renderer.enabled = active
                    ? isWeaponRenderer && firstPersonRendererDefaults[i]
                    : firstPersonRendererDefaults[i];
            }

            firstPersonPresentationActive = active;
        }

        private Vector3 ResolveCameraPosition(Vector3 focus, Vector3 desiredPosition, out bool obstructed)
        {
            obstructed = false;
            var offset = desiredPosition - focus;
            var distance = offset.magnitude;
            if (distance <= 0.001f)
                return desiredPosition;

            var direction = offset / distance;
            var hitCount = Physics.SphereCastNonAlloc(focus, cameraCollisionRadius, direction,
                cameraObstructionHits, distance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var nearestDistance = float.PositiveInfinity;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = cameraObstructionHits[i];
                if (hit.collider == null || hit.transform.root == transform.root ||
                    hit.distance >= nearestDistance)
                    continue;

                nearestDistance = hit.distance;
            }

            if (nearestDistance >= float.PositiveInfinity)
                return desiredPosition;

            obstructed = true;
            var safeDistance = Mathf.Clamp(nearestDistance - cameraCollisionPadding, 0.02f, distance);
            return focus + direction * safeDistance;
        }

        private void ApplyMovement(Vector3 desiredVelocity, bool jumpPressed)
        {
            var deltaTime = Time.deltaTime;
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
        }

        private void HaltHorizontalMotion()
        {
            horizontalVelocity = Vector3.zero;
            currentPlatform = null;
            platformContact = null;
            animator?.SetMovement(Vector3.zero, 0f,
                definition != null ? definition.stats.moveSpeed : 0f,
                characterController != null && characterController.isGrounded);
        }

        private void ApplyLockedMovement()
        {
            if (characterController == null || !characterController.enabled || definition == null)
            {
                HaltHorizontalMotion();
                return;
            }

            horizontalVelocity = Vector3.zero;
            ApplyMovement(Vector3.zero, false);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
                platformContact = hit.collider.GetComponentInParent<ArenaMovingPlatform>();
        }

        private void UpdateFootsteps()
        {
            if (!characterController.isGrounded || HorizontalSpeed <= 0.6f || Time.time < nextFootstepTime)
                return;

            audioPlayer?.Play(RoboClashAudioEvent.Footstep,
                definition.robotType == RoboClashRobotType.Shadow ? 0.35f : 1f);
            nextFootstepTime = Time.time + Mathf.Lerp(0.48f, 0.24f,
                Mathf.Clamp01(HorizontalSpeed / Mathf.Max(definition.stats.moveSpeed, 0.1f)));
        }
    }
}
