using Aetheriaum.CameraSystem.Controller;
using Aetheriaum.CameraSystem.Interfaces;
using Aetheriaum.CameraSystem.Manager;
using Aetheriaum.CameraSystem.Modifier;
using Aetheriaum.CameraSystem.Settings;
using Aetheriaum.CameraSystem.States;
using Aetheriaum.CharacterSystem.Interfaces;
using Aetheriaum.CoreSystem.Architecture.AbstractClasses;
using Aetheriaum.CoreSystem.Architecture.ServiceLocator;
using Aetheriaum.InputSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

namespace Aetheriaum.CameraSystem.Service
{
    /// <summary>
    /// Central camera system service managing all camera functionality.
    /// Integrates with input system for camera control and character system for target tracking.
    /// Uses CameraTargetManager for weighted target management.
    /// Follows SOLID principles with clear separation of concerns.
    /// </summary>
    public class CameraSystemService : BaseUpdatableService, ICameraSystem
    {
        #region Service Properties

        public override int InitializationPriority => 22; // After input, before character
        public override string ServiceName => "Camera System";
        private readonly bool _debugMode = false;
        #endregion

        #region ICameraSystem Properties

        public UnityEngine.Camera MainCamera { get; private set; }
        public ICameraController ActiveController { get; private set; }
        public CameraMode CurrentMode { get; private set; } = CameraMode.ThirdPerson;
        public Transform Target
        {
            get => _targetManager?.CurrentTarget ?? _target;
            set => SetTarget(value);
        }
        public bool InputEnabled { get; set; } = true;

        #endregion

        #region Private Fields

        // Core components
        private Transform _target; // Backup reference, primary is in target manager
        private GameObject _cameraGameObject;

        // Target management
        private CameraTargetManager _targetManager;
        private bool _hasAttemptedFallbackTarget = false;
        private float _targetSearchTimer = 0f;
        private const float TARGET_SEARCH_INTERVAL = 1f; // Search every second

        // Camera controllers
        private readonly Dictionary<CameraMode, ICameraController> _controllers = new Dictionary<CameraMode, ICameraController>();

        // Camera states
        private readonly Dictionary<CameraMode, ICameraState> _cameraStates = new Dictionary<CameraMode, ICameraState>();
        private ICameraState _activeState;

        // Camera modifiers
        private readonly List<ICameraModifier> _modifiers = new List<ICameraModifier>();
        private CameraShakeModifier _cameraShake;

        // Input integration
        private IInputSystem _inputSystem;
        private IGameplayInput _gameplayInput;

        // Character system integration
        private ICharacterSystem _characterSystem;

        // Camera transition
        private bool _isTransitioning = false;
        private CameraTransition _currentTransitionType = CameraTransition.Instant;
        private float _transitionTimer;
        private float _transitionDuration;
        private Vector3 _transitionStartPosition;
        private Quaternion _transitionStartRotation;
        private Vector3 _transitionTargetPosition;
        private Quaternion _transitionTargetRotation;

        // Settings
        private ICameraSettings _defaultSettings;

        #endregion

        #region Events

        public event Action<CameraMode, CameraMode> CameraModeChanged;
        public event Action<Transform, Transform> TargetChanged;

        #endregion

        #region Initialization

        protected override async Task OnInitializeAsync()
        {
            LogDebug("Initializing Camera System...");

            // Find or create main camera
            await InitializeMainCamera();

            // Initialize camera settings
            await InitializeCameraSettings();

            // Initialize target manager
            await InitializeTargetManager();

            // Initialize camera controllers
            await InitializeCameraControllers();

            // Initialize camera states
            await InitializeCameraStates();

            // Initialize camera effects
            InitializeCameraModifiers();

            // Setup input integration
            await InitializeInputIntegration();

            // Setup character system integration
            await InitializeCharacterIntegration();

            // Set initial camera mode AFTER everything is initialized
            SetCameraMode(CameraMode.ThirdPerson, CameraTransition.Instant);

            LogDebug("Camera System initialized successfully.");
        }

        private async Task InitializeMainCamera()
        {
            // Try to find existing main camera
            MainCamera = UnityEngine.Camera.main;

            if (MainCamera == null)
            {
                // Create new camera if none exists
                _cameraGameObject = new GameObject("Main Camera");
                MainCamera = _cameraGameObject.AddComponent<UnityEngine.Camera>();
                MainCamera.tag = "MainCamera";

                LogDebug("Created new main camera.");
            }
            else
            {
                LogDebug("Target is null : " + _target);
                _cameraGameObject = MainCamera.gameObject;
                LogDebug("Using existing main camera.");
            }

            await Task.CompletedTask;
        }

        private async Task InitializeCameraSettings()
        {
            // Load default camera settings
            //_defaultSettings = Resources.Load<CameraSettings>("DefaultCameraSettings");
            _defaultSettings = Resources.Load<CameraSettings>("DefaultCameraSettings");

            if (_defaultSettings == null)
            {
                //Debug.LogWarning("Default camera settings not found. Using fallback settings.");
                _defaultSettings = Resources.Load<CameraSettings>("DefaultCameraSettings");
                //if(_defaultSettings != null)
                //{
                //    Debug.LogWarning("Camear Created With Default Settings");
                //}
                return;
            }

            await Task.CompletedTask;
        }

        private async Task InitializeTargetManager()
        {
            _targetManager = new CameraTargetManager();
            _targetManager.TargetChanged += OnTargetManagerTargetChanged;

            LogDebug("Camera Target Manager initialized.");
            await Task.CompletedTask;
        }

        private async Task InitializeCameraControllers()
        {
            // Initialize Third Person Controller
            var thirdPersonController = _cameraGameObject.GetComponent<ThirdPersonCameraController>();
            if (thirdPersonController == null)
            {
                thirdPersonController = _cameraGameObject.AddComponent<ThirdPersonCameraController>();
            }

            // Set settings BEFORE initializing
            thirdPersonController.Settings = _defaultSettings;

            // Initialize the controller with only the camera parameter
            thirdPersonController.Initialize(MainCamera);

            // Register the controller
            RegisterController(CameraMode.ThirdPerson, thirdPersonController);

            // Set as active controller
            ActiveController = thirdPersonController;

            LogDebug("Third person camera controller initialized and set as active");

            // TODO: Add other camera controllers as needed
            // RegisterController(CameraMode.Combat, combatController);
            // RegisterController(CameraMode.FirstPerson, firstPersonController);

            await Task.CompletedTask;
        }

        private async Task InitializeCameraStates()
        {
            // Initialize Third Person State
            var thirdPersonState = new ThirdPersonCameraState();
            RegisterState(CameraMode.ThirdPerson, thirdPersonState);

            // Initialize Combat State
            var combatState = new CombatCameraState();
            RegisterState(CameraMode.Combat, combatState);

            // Initialize all states
            foreach (var kvp in _cameraStates)
            {
                var controller = GetController(kvp.Key);
                if (controller != null)
                {
                    //kvp.Value.Initialize(controller);
                }
            }

            await Task.CompletedTask;
        }

        private void InitializeCameraModifiers()
        {
            // Initialize camera shake
            _cameraShake = new CameraShakeModifier();
            _cameraShake.Initialize();
            AddModifier(_cameraShake);

            // TODO: Add other modifiers as needed
            // AddModifier(new CameraZoomModifier());
            // AddModifier(new CameraFocusModifier());
        }

        private async Task InitializeInputIntegration()
        {
            try
            {
                _inputSystem = ServiceLocator.Instance.TryGetService<IInputSystem>();
                if (_inputSystem != null)
                {
                    _gameplayInput = _inputSystem.GetInputHandler(InputContext.Gameplay) as IGameplayInput;
                    LogDebug("Camera system integrated with input system.");
                }
                else
                {
                    Debug.LogWarning("Input system not available. Camera input will be limited.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize input integration: {ex}");
            }

            await Task.CompletedTask;
        }

        // Enhanced character integration with retry logic
        private async Task InitializeCharacterIntegration()
        {
            try
            {
                LogDebug("Starting character integration...");

                // Try multiple times with increasing delays
                int[] retryDelays = { 100, 250, 500, 1000, 2000 }; // milliseconds

                for (int attempt = 0; attempt < retryDelays.Length; attempt++)
                {
                    await Task.Delay(retryDelays[attempt]);

                    _characterSystem = ServiceLocator.Instance.TryGetService<ICharacterSystem>();

                    if (_characterSystem != null)
                    {
                        LogDebug($"Character system found on attempt {attempt + 1}");

                        // Subscribe to character changes
                        _characterSystem.ActiveCharacterChanged += OnActiveCharacterChanged;

                        // Set initial target if there's already an active character
                        if (_characterSystem.ActiveCharacter?.Transform != null)
                        {
                            _targetManager.SetTarget(_characterSystem.ActiveCharacter.Transform, 1f);
                            LogDebug($"Camera target set to active character: {_characterSystem.ActiveCharacter.CharacterId}");
                            return; // Success!
                        }

                        LogDebug("Character system found but no active character yet");
                        break; // System found, but no character - will search in Update
                    }

                    LogDebug($"Character system not found, attempt {attempt + 1}/{retryDelays.Length}");
                }

                if (_characterSystem == null)
                {
                    Debug.LogWarning("Character system not found after all retry attempts. Will search for targets manually.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize character integration: {ex}");
            }
        }

        #endregion

        #region Target Manager Event Handling

        private void OnTargetManagerTargetChanged(Transform previousTarget, Transform newTarget)
        {
            LogDebug($"Target Manager: Target changed from {(previousTarget?.name ?? "NULL")} to {(newTarget?.name ?? "NULL")}");

            // Update the internal target reference for backward compatibility
            _target = newTarget;

            // Update all registered controllers
            foreach (var controller in _controllers.Values)
            {
                controller.Target = newTarget;
                LogDebug($"Updated target for controller: {controller.GetType().Name}");
            }

            // Activate the active controller if we have a target
            if (newTarget != null && ActiveController != null && !ActiveController.IsActive)
            {
                ActiveController.Activate();
                LogDebug("Activated controller due to new target");
            }

            // Reset search attempts when we get a valid target
            if (newTarget != null)
            {
                _hasAttemptedFallbackTarget = false;
                _targetSearchTimer = 0f;
            }

            // Fire the existing TargetChanged event for backward compatibility
            TargetChanged?.Invoke(previousTarget, newTarget);
        }

        #endregion

        #region Update Loop

        public override void FixedUpdate()
        {
            // Camera system doesn't need fixed update
        }

        // Implement the abstract OnUpdate method required by BaseUpdatableService
        protected override void OnUpdate()
        {
            if (!IsInitialized) return;

            try
            {
                // Search for target if we don't have one
                if (!_targetManager.HasTarget())
                {
                    SearchForTarget();
                }

                // Add debugging every 60 frames (about once per second)
                //if (Time.frameCount % 60 == 0)
                //{
                //    Debug.Log($"Camera Debug - Target: {(_targetManager.HasTarget() ? _targetManager.CurrentTarget.name : "SEARCHING...")}, " +
                //             $"Weight: {(_targetManager.HasTarget() ? _targetManager.GetTargetWeight(_targetManager.CurrentTarget) : 0f):F2}, " +
                //             $"ActiveController: {(ActiveController != null ? ActiveController.GetType().Name : "None")}, " +
                //             $"IsActive: {(ActiveController?.IsActive ?? false)}, " +
                //             $"CameraPos: {MainCamera.transform.position}");
                //}

                // Handle input
                HandleCameraInput();

                // Update active controller
                UpdateActiveController();

                // Update active state
                UpdateActiveState();

                // Update modifiers
                UpdateModifiers();

                // Update camera transition
                UpdateTransition();

                // Apply final camera transform (only if not transitioning and no active controller)
                if (!_isTransitioning)
                {
                    ApplyCameraTransform();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in camera system update: {ex}");
            }
            // This method is required by BaseUpdatableService but we use the override Update() instead
            // Leave empty as all logic is in the overridden Update() method above
        }

        // Enhanced method to search for available targets using target manager
        private void SearchForTarget()
        {
            _targetSearchTimer += Time.deltaTime;

            // Only search every TARGET_SEARCH_INTERVAL seconds to avoid performance issues
            if (_targetSearchTimer < TARGET_SEARCH_INTERVAL) return;

            _targetSearchTimer = 0f;

            LogDebug("Searching for camera target...");

            // Method 1: Try character system first (highest priority)
            if (_characterSystem?.ActiveCharacter?.Transform != null)
            {
                LogDebug("Found target via character system");
                _targetManager.SetTarget(_characterSystem.ActiveCharacter.Transform, 1f);
                return;
            }

            // Method 2: Look for Player tag (high priority)
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                LogDebug($"Found target via Player tag: {playerGO.name}");
                _targetManager.SetTarget(playerGO.transform, 0.9f);
                return;
            }

            // Method 3: Look for common character-related components (medium priority)
            //var characterController = UnityEngine.Object.FindObjectOfType<CharacterController>();
            var characterController = UnityEngine.Object.FindFirstObjectByType<CharacterController>(FindObjectsInactive.Exclude);
            if (characterController != null)
            {
                LogDebug($"Found target via CharacterController: {characterController.name}");
                _targetManager.SetTarget(characterController.transform, 0.8f);
                return;
            }

            // Method 4: Look for objects with "Player" in name (medium-low priority)
            //var allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            var allGameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var go in allGameObjects)
            {
                if (go.name.ToLower().Contains("player"))
                {
                    LogDebug($"Found target via name search (player): {go.name}");
                    _targetManager.SetTarget(go.transform, 0.7f);
                    return;
                }
            }

            // Method 5: Look for objects with "Character" in name (low priority)
            foreach (var go in allGameObjects)
            {
                if (go.name.ToLower().Contains("character"))
                {
                    LogDebug($"Found target via name search (character): {go.name}");
                    _targetManager.SetTarget(go.transform, 0.6f);
                    return;
                }
            }

            // Method 6: Last resort - create a dummy target at origin (lowest priority)
            if (!_hasAttemptedFallbackTarget)
            {
                _hasAttemptedFallbackTarget = true;
                CreateFallbackTarget();
            }
        }

        // Create a fallback target for testing using target manager
        private void CreateFallbackTarget()
        {
            Debug.LogWarning("No suitable camera target found. Creating fallback target at origin.");

            GameObject fallbackTarget = new GameObject("Camera_FallbackTarget");
            fallbackTarget.transform.position = Vector3.zero;

            // Add a visible indicator
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(fallbackTarget.transform);
            cube.transform.localPosition = Vector3.zero;
            cube.GetComponent<Renderer>().material.color = Color.red;

            _targetManager.SetTarget(fallbackTarget.transform, 0.1f); // Very low priority for fallback

            LogDebug("Fallback camera target created. You should replace this with your actual player/character.");
        }

        // Camera controllers update themselves automatically via MonoBehaviour.LateUpdate()
        private void UpdateActiveController()
        {
            // The controller updates itself automatically when:
            // 1. It's active (IsActive = true)
            // 2. It has a target
            // 3. LateUpdate is called by Unity

            // We just need to ensure the controller is active and has the right target
            if (ActiveController != null && !ActiveController.IsActive && _targetManager.HasTarget())
            {
                ActiveController.Activate();
            }
        }

        private void HandleCameraInput()
        {
            if (!InputEnabled || _gameplayInput == null) return;

            // Get input
            var lookInput = _gameplayInput.LookDelta;
            var zoomInput = _gameplayInput.ZoomDelta;

            // Pass input to active controller if available
            if (ActiveController != null && ActiveController.IsActive)
            {
                if (ActiveController is ThirdPersonCameraController thirdPersonController)
                {
                    thirdPersonController.HandleLookInput(lookInput);
                    if (Mathf.Abs(zoomInput) > 0.01f)
                    {
                        thirdPersonController.HandleZoomInput(zoomInput);
                    }
                }
            }

            // Also pass input to active state if available
            if (_activeState != null)
            {
                _activeState.HandleInput(lookInput, zoomInput);
            }
        }

        private void UpdateActiveState()
        {
            _activeState?.UpdateState(Time.deltaTime);
        }

        private void UpdateModifiers()
        {
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].IsEnabled)
                {
                    _modifiers[i].UpdateModifier(Time.deltaTime);
                }
            }
        }

        private void UpdateTransition()
        {
            if (!_isTransitioning) return;

            _transitionTimer += Time.deltaTime;
            var progress = _transitionTimer / _transitionDuration;

            if (progress >= 1f)
            {
                // Transition complete
                _isTransitioning = false;
                progress = 1f;
            }

            // Apply transition interpolation based on type
            var easedProgress = ApplyTransitionEasing(progress);

            var currentPosition = Vector3.Lerp(_transitionStartPosition, _transitionTargetPosition, easedProgress);
            var currentRotation = Quaternion.Slerp(_transitionStartRotation, _transitionTargetRotation, easedProgress);

            // Apply to camera
            MainCamera.transform.position = currentPosition;
            MainCamera.transform.rotation = currentRotation;
        }

        private float ApplyTransitionEasing(float t)
        {
            switch (_currentTransitionType)
            {
                case CameraTransition.Instant:
                    return 1f;
                case CameraTransition.Smooth:
                    return t;
                case CameraTransition.EaseIn:
                    return t * t;
                case CameraTransition.EaseOut:
                    return 1f - (1f - t) * (1f - t);
                case CameraTransition.EaseInOut:
                    return t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
                case CameraTransition.Bounce:
                    return 1f - Mathf.Abs(Mathf.Cos(t * Mathf.PI * 2f)) * (1f - t);
                default:
                    return t;
            }
        }

        private void ApplyCameraTransform()
        {
            if (_isTransitioning || _activeState == null) return;

            // Get desired transform from active state
            var desiredPosition = _activeState.GetDesiredPosition();
            var desiredRotation = _activeState.GetDesiredRotation();
            var desiredFov = _activeState.GetDesiredFieldOfView();

            // Apply modifiers
            ApplyModifiersToTransform(ref desiredPosition, ref desiredRotation, ref desiredFov);

            // Apply to camera
            MainCamera.transform.position = desiredPosition;
            MainCamera.transform.rotation = desiredRotation;
            MainCamera.fieldOfView = desiredFov;
        }

        private void ApplyModifiersToTransform(ref Vector3 position, ref Quaternion rotation, ref float fov)
        {
            foreach (var modifier in _modifiers)
            {
                if (modifier.IsEnabled)
                {
                    position = modifier.ModifyPosition(position, ActiveController);
                    rotation = modifier.ModifyRotation(rotation, ActiveController);
                    fov = modifier.ModifyFieldOfView(fov, ActiveController);
                }
            }
        }

        #endregion

        #region Character Event Handling

        private void OnActiveCharacterChanged(ICharacter previousCharacter, ICharacter newCharacter)
        {
            if (newCharacter?.Transform != null)
            {
                _targetManager.SetTarget(newCharacter.Transform, 1f); // Highest priority for active character
                LogDebug($"Camera target changed to active character: {newCharacter.CharacterId}");
            }
            else
            {
                Debug.LogWarning("New active character has no transform!");
                _targetManager.ClearTarget();
            }
        }

        #endregion

        #region ICameraSystem Implementation

        public void SetCameraMode(CameraMode mode, CameraTransition transition = CameraTransition.Smooth)
        {
            if (CurrentMode == mode) return;

            var previousMode = CurrentMode;
            var previousState = _activeState;
            var previousController = ActiveController;

            // Get new controller first
            var newController = GetController(mode);
            if (newController == null)
            {
                Debug.LogError($"Camera controller for mode {mode} not found.");
                return;
            }

            // Deactivate previous controller
            if (previousController != null && previousController.IsActive)
            {
                previousController.Deactivate();
            }

            // Set new mode and controller
            CurrentMode = mode;
            ActiveController = newController;

            // Ensure target is set on new controller
            if (_targetManager.HasTarget())
            {
                ActiveController.Target = _targetManager.CurrentTarget;
            }

            // Activate new controller
            if (!ActiveController.IsActive)
            {
                ActiveController.Activate();
            }

            // Handle state transitions (if using states)
            var newState = GetState(mode);
            if (newState != null)
            {
                previousState?.OnExit(newState);
                _activeState = newState;
                _activeState.OnEnter(previousState);
            }

            // Handle transition
            if (transition != CameraTransition.Instant && MainCamera != null)
            {
                StartTransition(transition);
            }

            // Fire event
            CameraModeChanged?.Invoke(previousMode, CurrentMode);

            LogDebug($"Camera mode changed from {previousMode} to {CurrentMode}");
        }

        // Enhanced SetTarget method using target manager
        public void SetTarget(Transform target, CameraTransition transition = CameraTransition.Smooth)
        {
            // Use the target manager instead of direct assignment
            if (target != null)
            {
                _targetManager.SetTarget(target, 1f); // Default weight of 1
                LogDebug($"SetTarget: Delegated to TargetManager - {target.name}");
            }
            else
            {
                _targetManager.ClearTarget();
                LogDebug("SetTarget: Cleared target via TargetManager");
            }

            // Handle transition if needed
            if (transition != CameraTransition.Instant && MainCamera != null && target != null)
            {
                StartTransition(transition);
            }
        }

        // New method for weighted target setting
        public void SetTargetWithWeight(Transform target, float weight, CameraTransition transition = CameraTransition.Smooth)
        {
            if (target != null)
            {
                _targetManager.SetTarget(target, weight);
                LogDebug($"SetTargetWithWeight: {target.name} with weight {weight}");

                if (transition != CameraTransition.Instant && MainCamera != null)
                {
                    StartTransition(transition);
                }
            }
            else
            {
                _targetManager.ClearTarget();
                LogDebug("SetTargetWithWeight: Cleared target");
            }
        }

        public void RegisterController(CameraMode mode, ICameraController controller)
        {
            if (controller == null)
            {
                Debug.LogError("Cannot register null camera controller.");
                return;
            }

            _controllers[mode] = controller;

            // Set current target if we have one
            if (_targetManager.HasTarget())
            {
                controller.Target = _targetManager.CurrentTarget;
            }

            LogDebug($"Registered camera controller for mode: {mode}");
        }

        public void UnregisterController(CameraMode mode)
        {
            if (_controllers.ContainsKey(mode))
            {
                _controllers.Remove(mode);
                LogDebug($"Unregistered camera controller for mode: {mode}");
            }
        }

        public ICameraController GetController(CameraMode mode)
        {
            _controllers.TryGetValue(mode, out var controller);
            return controller;
        }

        #endregion

        #region Target Manager Public Interface

        public float GetTargetWeight(Transform target)
        {
            return _targetManager.GetTargetWeight(target);
        }

        public bool HasValidTarget()
        {
            return _targetManager.HasTarget();
        }

        public Transform GetPreviousTarget()
        {
            return _targetManager.PreviousTarget;
        }

        #endregion

        #region State Management

        public void RegisterState(CameraMode mode, ICameraState state)
        {
            if (state == null)
            {
                Debug.LogError("Cannot register null camera state.");
                return;
            }

            _cameraStates[mode] = state;

            LogDebug($"Registered camera state for mode: {mode}");
        }

        public void UnregisterState(CameraMode mode)
        {
            if (_cameraStates.ContainsKey(mode))
            {
                _cameraStates.Remove(mode);
                LogDebug($"Unregistered camera state for mode: {mode}");
            }
        }

        public ICameraState GetState(CameraMode mode)
        {
            _cameraStates.TryGetValue(mode, out var state);
            return state;
        }

        #endregion

        #region Modifier Management

        public void AddModifier(ICameraModifier modifier)
        {
            if (modifier == null)
            {
                Debug.LogError("Cannot add null camera modifier.");
                return;
            }

            if (!_modifiers.Contains(modifier))
            {
                _modifiers.Add(modifier);
                _modifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

                LogDebug($"Added camera modifier: {modifier.ModifierName}");
            }
        }

        public void RemoveModifier(ICameraModifier modifier)
        {
            if (_modifiers.Remove(modifier))
            {
                modifier.Cleanup();
                LogDebug($"Removed camera modifier: {modifier.ModifierName}");
            }
        }

        public T GetModifier<T>() where T : class, ICameraModifier
        {
            foreach (var modifier in _modifiers)
            {
                if (modifier is T targetModifier)
                {
                    return targetModifier;
                }
            }
            return null;
        }

        #endregion

        #region Camera Effects

        public void ShakeCamera(float intensity, float duration, float frequency = 10f)
        {
            _cameraShake?.StartShake(intensity, duration, frequency);
        }

        public void StopCameraShake()
        {
            _cameraShake?.StopShake();
        }

        public ICameraShake GetCameraShake()
        {
            return _cameraShake;
        }

        #endregion

        #region Transition Management

        private void StartTransition(CameraTransition transition)
        {
            if (_activeState == null) return;

            _currentTransitionType = transition;
            _transitionTimer = 0f;
            _transitionDuration = GetTransitionDuration(transition);

            // Store current transform
            _transitionStartPosition = MainCamera.transform.position;
            _transitionStartRotation = MainCamera.transform.rotation;

            // Get target transform
            _transitionTargetPosition = _activeState.GetDesiredPosition();
            _transitionTargetRotation = _activeState.GetDesiredRotation();

            _isTransitioning = _transitionDuration > 0f;
        }

        private float GetTransitionDuration(CameraTransition transition)
        {
            switch (transition)
            {
                case CameraTransition.Instant:
                    return 0f;
                case CameraTransition.Smooth:
                    return 1f;
                case CameraTransition.EaseIn:
                case CameraTransition.EaseOut:
                case CameraTransition.EaseInOut:
                    return 0.8f;
                case CameraTransition.Bounce:
                    return 1.2f;
                default:
                    return 0.5f;
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Debug Camera State")]
        public void DebugCameraState()
        {
            LogDebug("=== CAMERA SYSTEM DEBUG ===");
            LogDebug($"Initialized: {IsInitialized}");
            LogDebug($"Current Target: {(_targetManager.CurrentTarget?.name ?? "None")}");
            LogDebug($"Previous Target: {(_targetManager.PreviousTarget?.name ?? "None")}");
            LogDebug($"Has Valid Target: {_targetManager.HasTarget()}");
            LogDebug($"Target Weight: {(_targetManager.CurrentTarget != null ? _targetManager.GetTargetWeight(_targetManager.CurrentTarget) : 0f)}");
            LogDebug($"Current Mode: {CurrentMode}");
            LogDebug($"Active Controller: {(ActiveController != null ? ActiveController.GetType().Name : "None")}");
            LogDebug($"Controller Active: {(ActiveController?.IsActive ?? false)}");
            LogDebug($"Main Camera Position: {MainCamera?.transform.position}");
            LogDebug($"Registered Controllers: {_controllers.Count}");

            foreach (var kvp in _controllers)
            {
                LogDebug($"  - {kvp.Key}: {kvp.Value.GetType().Name} (Active: {kvp.Value.IsActive})");
            }
        }

        [ContextMenu("Set Target to First GameObject with Player Tag")]
        public void SetTargetToPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _targetManager.SetTarget(player.transform, 0.9f);
                LogDebug($"Manually set camera target to: {player.name} with weight 0.9");
            }
            else
            {
                Debug.LogWarning("No GameObject with 'Player' tag found");
            }
        }

        [ContextMenu("Create Test Target at Origin")]
        public void CreateTestTarget()
        {
            GameObject testTarget = new GameObject("CameraTestTarget");
            testTarget.transform.position = new Vector3(0, 1, 0);

            // Add visual indicator
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(testTarget.transform);
            sphere.transform.localScale = Vector3.one * 0.5f;
            sphere.GetComponent<Renderer>().material.color = Color.blue;

            _targetManager.SetTarget(testTarget.transform, 0.5f);
            LogDebug("Test camera target created and set with weight 0.5");
        }

        [ContextMenu("List All Potential Targets")]
        public void ListPotentialTargets()
        {
            LogDebug("=== POTENTIAL CAMERA TARGETS ===");

            // Check Player tags
            var players = GameObject.FindGameObjectsWithTag("Player");
            LogDebug($"GameObjects with Player tag: {players.Length}");
            foreach (var p in players)
                LogDebug($"  - {p.name} at {p.transform.position}");

            // Check Character Controllers
            //var charControllers = UnityEngine.Object.FindObjectsOfType<CharacterController>(includeInactive : true);
            var charControllers = UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include,FindObjectsSortMode.None);

            LogDebug($"Character Controllers: {charControllers.Length}");
            foreach (var cc in charControllers)
                LogDebug($"  - {cc.name} at {cc.transform.position}");

            // Check objects with character/player in name
            //var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var potentialTargets = allObjects.Where(go =>
                go.name.ToLower().Contains("player") ||
                go.name.ToLower().Contains("character") ||
                go.name.ToLower().Contains("hero")).ToArray();

            LogDebug($"Objects with player/character in name: {potentialTargets.Length}");
            foreach (var target in potentialTargets)
                LogDebug($"  - {target.name} at {target.transform.position}");
        }

        [ContextMenu("Force Debug Target Setup")]
        public void ForceDebugTargetSetup()
        {
            LogDebug("=== TARGET DEBUG ANALYSIS ===");

            // Check character system integration
            LogDebug($"Character System Available: {_characterSystem != null}");
            if (_characterSystem != null)
            {
                LogDebug($"Active Character: {_characterSystem.ActiveCharacter?.CharacterId ?? "None"}");
                LogDebug($"Active Character Transform: {_characterSystem.ActiveCharacter?.Transform?.name ?? "None"}");
            }

            // Check for player GameObject manually
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            LogDebug($"Player GameObject Found: {playerGO?.name ?? "None"}");

            // Check for any character controllers in scene
            //var characterControllers = UnityEngine.Object.FindObjectsOfType<CharacterController>();
            var characterControllers = UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            LogDebug($"Character Controllers in scene: {characterControllers.Length}");
            foreach (var cc in characterControllers)
            {
                LogDebug($"  - {cc.gameObject.name}");
            }

            // Try to set target manually for testing
            if (playerGO != null)
            {
                LogDebug("Attempting to set player as camera target...");
                _targetManager.SetTarget(playerGO.transform, 0.9f);
            }
        }

        [ContextMenu("Force Search for Target Now")]
        [Obsolete]
        public void ForceSearchForTargetNow()
        {
            LogDebug("Forcing immediate target search...");
            _targetSearchTimer = TARGET_SEARCH_INTERVAL; // Force search on next update
            SearchForTarget();
        }

        [ContextMenu("Test Target Manager - Set Weighted Target")]
        public void TestSetWeightedTarget()
        {
            var testGO = new GameObject("WeightedTestTarget");
            testGO.transform.position = Vector3.up * 2f;

            // Add visual indicator
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(testGO.transform);
            cylinder.transform.localScale = Vector3.one * 0.3f;
            cylinder.GetComponent<Renderer>().material.color = Color.green;

            _targetManager.SetTarget(testGO.transform, 0.75f);
            LogDebug($"Set weighted target: {testGO.name} with weight: {_targetManager.GetTargetWeight(testGO.transform)}");
        }

        [ContextMenu("Clear Current Target")]
        public void ClearCurrentTarget()
        {
            _targetManager.ClearTarget();
            LogDebug("Cleared current camera target");
        }

        [ContextMenu("Show Target Manager Info")]
        public void ShowTargetManagerInfo()
        {
            LogDebug("=== TARGET MANAGER INFO ===");
            LogDebug($"Has Target: {_targetManager.HasTarget()}");
            LogDebug($"Current Target: {(_targetManager.CurrentTarget?.name ?? "None")}");
            LogDebug($"Previous Target: {(_targetManager.PreviousTarget?.name ?? "None")}");
            if (_targetManager.CurrentTarget != null)
            {
                LogDebug($"Current Target Position: {_targetManager.GetTargetPosition()}");
                LogDebug($"Current Target Weight: {_targetManager.GetTargetWeight(_targetManager.CurrentTarget)}");
            }
        }
        private void LogDebug(string message)
        {
            if (_debugMode)
            {
                Debug.Log($"[{ServiceName}] {message}");
            }
        }
        #endregion

        #region Cleanup

        protected override async Task OnShutdownAsync()
        {
            LogDebug("Shutting down Camera System...");

            // Unsubscribe from target manager events
            if (_targetManager != null)
            {
                _targetManager.TargetChanged -= OnTargetManagerTargetChanged;
                _targetManager.ClearTarget();
                _targetManager = null;
            }

            // Unsubscribe from character system events
            if (_characterSystem != null)
            {
                _characterSystem.ActiveCharacterChanged -= OnActiveCharacterChanged;
            }

            // Clean up modifiers
            foreach (var modifier in _modifiers)
            {
                modifier.Cleanup();
            }
            _modifiers.Clear();

            // Clean up states
            _cameraStates.Clear();

            // Deactivate all controllers
            foreach (var controller in _controllers.Values)
            {
                if (controller.IsActive)
                {
                    controller.Deactivate();
                }
            }
            _controllers.Clear();

            _activeState = null;
            ActiveController = null;
            _target = null;

            await Task.CompletedTask;
        }

        #endregion
    }
}