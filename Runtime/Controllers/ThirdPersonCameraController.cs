using Aetheriaum.CameraSystem.Interfaces;
using Aetheriaum.CameraSystem.Settings;
using Aetheriaum.CoreSystem.BootStrap;
using Coty.Camera.Components;
using UnityEngine;

namespace Aetheriaum.CameraSystem.Controller

{
    /// <summary>
    /// Third-person camera controller implementing orbit camera mechanics.
    /// Follows SOLID principles with modular component design.
    /// </summary>
    public class ThirdPersonCameraController : MonoBehaviour, ICameraController
    {
        [Header("Camera Setup")]
        [SerializeField] private CameraSettings _defaultSettings;
        //[SerializeField] private bool _debugMode = false;

        [Header("Orbit Component")]
        [SerializeField] private CameraOrbitComponent _cameraOrbitComponent = new CameraOrbitComponent();

        // Core components
        private UnityEngine.Camera _camera;
        private Transform _target;
        private ICameraSettings _settings;
        private bool _isActive = false;
        private Vector3 _targetPosition;
        private Vector3 _currentVelocity;
        private bool _debugMode = false;
        private float _currentFov = 60f;


        #region ICameraController Properties

        public CameraMode CameraMode => CameraMode.ThirdPerson;
        public Transform CameraTransform => _camera?.transform ?? transform;
        public Transform Target
        {
            get => _target;
            set => SetTarget(value);
        }
        public ICameraSettings Settings
        {
            get => _settings;
            set => _settings = value ?? _defaultSettings;
        }
        public bool IsActive => _isActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            if (_settings == null)
            {
                _settings = _defaultSettings ?? CreateDefaultSettings();
            }
            _currentFov = _settings?.DefaultFieldOfView ?? 60f;
        }

        private void LateUpdate()
        {
            if (_isActive && _target != null)
            {
                UpdateCamera();
            }
        }

        #endregion

        #region ICameraController Implementation

        public void Initialize()
        {
            if (_settings == null)
            {
                _settings = _defaultSettings ?? CreateDefaultSettings();
            }
            LogDebug($"ThirdPersonCameraController initialized");
        }

        public void Initialize(UnityEngine.Camera camera)
        {
            _camera = camera;

            // Initialize settings first
            if (_settings == null)
            {
                _settings = _defaultSettings ?? CreateDefaultSettings();
            }

            // Initialize orbit component with proper distance from settings
            var initialDistance = (_settings.MinDistance + _settings.MaxDistance) * 0.5f; // Use middle distance
            _cameraOrbitComponent.Initialize(0f, 10f, initialDistance);

            // Pass settings to orbit component
            _cameraOrbitComponent.SetSettings(_settings);

            if (_camera != null)
            {
                _camera.tag = "MainCamera";
                LogDebug($"Camera assigned: {_camera.name}");
            }

            Initialize();
        }

        public void Activate()
        {
            if (_isActive) return;

            _isActive = true;

            if (_camera != null)
            {
                _camera.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (_target != null)
            {

                _targetPosition = _target.position + Vector3.up * (_settings?.HeightOffset ?? 1.5f);
                //Debug.Log(_targetPosition);
                var initialPosition = CalculateDesiredPosition();
                var initialRotation = CalculateDesiredRotation();
                //Debug.Log(initialPosition);

                if (_camera != null)
                {
                    _camera.transform.position = initialPosition;
                    _camera.transform.rotation = initialRotation;
                }

                LogDebug($"Camera positioned at: {initialPosition}");
            }

            LogDebug("ThirdPersonCameraController activated");
        }

        public void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            LogDebug("ThirdPersonCameraController deactivated");
        }

        public void UpdateCamera()
        {
            if (!_isActive || _target == null || _settings == null || _camera == null) return;

            // Update target position with height offset
            _targetPosition = _target.position + Vector3.up * _settings.HeightOffset;

            _cameraOrbitComponent.UpdateOrbit(Time.deltaTime);
            // Calculate desired camera position and rotation
            var desiredPosition = CalculateDesiredPosition();
            var desiredRotation = CalculateDesiredRotation();

            // Apply movement to the actual camera
            ApplyCameraMovement(desiredPosition, desiredRotation);
        }

        public void UpdateController(float deltaTime)
        {
            UpdateCamera();
        }

        public void HandleLookInput(Vector2 lookDelta)
        {
            if (!_isActive) return;

            _cameraOrbitComponent.HandleInput(lookDelta,0f,_settings);
        }

        public void HandleZoomInput(float zoomDelta)
        {
            if (!_isActive) return;
            _cameraOrbitComponent.HandleInput(Vector2.zero,zoomDelta,_settings);
        }

        public void HandleCustomInput()
        {
            // Implementation for any custom input handling
        }

        public void ResetToDefault()
        {
            if (_settings == null) return;

            // Reset to middle distance instead of max distance
            var defaultDistance = (_settings.MinDistance + _settings.MaxDistance) * 0.5f;
            _cameraOrbitComponent.SetOrbitInstant(0f, 10f, defaultDistance);
            _currentFov = _settings?.DefaultFieldOfView ?? 60f;

            if (_target != null && _camera != null)
            {
                _targetPosition = _target.position + Vector3.up * (_settings?.HeightOffset ?? 1.5f);
                var desiredPosition = CalculateDesiredPosition();
                var desiredRotation = CalculateDesiredRotation();

                _camera.transform.position = desiredPosition;
                _camera.transform.rotation = desiredRotation;
                _camera.fieldOfView = _currentFov;

                LogDebug($"Camera reset to position: {desiredPosition}, distance: {defaultDistance}");
            }
        }

        public void Reset()
        {
            ResetToDefault();
        }

        public void TransitionFrom(ICameraController previousController, CameraTransition transitionType)
        {
            if (previousController == null) return;

            var toPosition = CalculateDesiredPosition();
            var toRotation = CalculateDesiredRotation();

            if (_camera != null)
            {
                _camera.transform.position = toPosition;
                _camera.transform.rotation = toRotation;
            }
        }

        public bool CanTransitionTo(CameraMode mode)
        {
            return true;
        }

        public bool HasClearLineOfSight()
        {
            if (_target == null || _camera == null) return true;

            var direction = _targetPosition - _camera.transform.position;
            var distance = direction.magnitude;

            if (Physics.Raycast(_camera.transform.position, direction.normalized, out RaycastHit hit, distance))
            {
                return hit.transform == _target;
            }

            return true;
        }

        public Vector3 GetDesiredPosition()
        {
            return CalculateDesiredPosition();
        }

        public Quaternion GetDesiredRotation()
        {
            return CalculateDesiredRotation();
        }

        public void SetOrbitConstraints(float minVerticalAngle, float maxVerticalAngle, float minDistance, float maxDistance)
        {
            _cameraOrbitComponent.SetConstraints(minVerticalAngle, maxVerticalAngle, minDistance, maxDistance);
        }

        public void SetOrbitSmoothingTimes(float rotationSmoothTime, float distanceSmoothTime)
        {
            _cameraOrbitComponent.SetSmoothingTimes(rotationSmoothTime, distanceSmoothTime);
        }

        public Vector3 CalculateDesiredPosition()
        {
            if (_target == null) return _camera?.transform.position ?? Vector3.zero;

            // Use target's forward direction for initial orientation if horizontal angle is 0
            Vector3 targetForward = _target.forward;

            // Create rotation based on angles
            //var yRotation = Quaternion.AngleAxis(_horizontalAngle, Vector3.up);
            //var xRotation = Quaternion.AngleAxis(_verticalAngle, Vector3.right);
            //var combinedRotation = yRotation * xRotation;

            // Calculate offset from target (backward from target's direction)
            //var offset = combinedRotation * Vector3.back * _distance;

            //return _targetPosition + offset;
            return _cameraOrbitComponent.CalculatePosition(_targetPosition);

        }

        public Quaternion CalculateDesiredRotation()
        {
            if (_target == null) return _camera?.transform.rotation ?? Quaternion.identity;

            var cameraPosition = CalculateDesiredPosition();
            // Look from camera position towards target
            //var cameraPos = CalculateDesiredPosition();
            //var direction = (_targetPosition - cameraPos).normalized;

            //if (direction != Vector3.zero)
            //{
            //    return Quaternion.LookRotation(direction);
            //}

            //return _camera?.transform.rotation ?? Quaternion.identity;
            return _cameraOrbitComponent.CalculateRotation(cameraPosition,_targetPosition);
        }

        public float CalculateDesiredFieldOfView()
        {
            return _currentFov;
        }

        public void SetPosition(Vector3 position)
        {
            if (_camera != null)
            {
                _camera.transform.position = position;
            }
        }

        public void SetRotation(Quaternion rotation)
        {
            if (_camera != null)
            {
                _camera.transform.rotation = rotation;
            }
        }

        public object GetCurrentState()
        {
            return new
            {
                Position = _camera?.transform.position ?? Vector3.zero,
                Rotation = _camera?.transform.rotation ?? Quaternion.identity,
                FieldOfView = _currentFov,
                Distance = _cameraOrbitComponent.Distance,
                HorizontalAngle = _cameraOrbitComponent.HorizontalAngle,
                VerticalAngle = _cameraOrbitComponent.VerticalAngle,
            };
        }

        public void SetCurrentState(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            if (_camera != null)
            {
                _camera.transform.position = position;
                _camera.transform.rotation = rotation;
                _camera.fieldOfView = fieldOfView;
            }
            _currentFov = fieldOfView;
        }

        #endregion

        #region Private Methods

        private void InitializeComponents()
        {
            if (_camera == null)
            {
                _camera = GetComponent<UnityEngine.Camera>();
                if (_camera == null)
                {
                    _camera = gameObject.AddComponent<UnityEngine.Camera>();
                    _camera.tag = "MainCamera";
                }
            }
        }

        private void SetTarget(Transform target)
        {
            if (_target == target) return;

            _target = target;

            if (_target != null)
            {
                _targetPosition = _target.position + Vector3.up * (_settings?.HeightOffset ?? 1.5f);

                if (_isActive)
                {
                    // Immediately position camera when target is set
                    ResetToDefault();
                }

                LogDebug($"Camera target set to: {_target.name} at position: {_target.position}");
            }
        }

        private void ApplyCameraMovement(Vector3 desiredPosition, Quaternion desiredRotation)
        {
            if (_settings == null || _camera == null) return;

            var followSpeed = _settings.FollowSpeed;

            // Smooth movement for the actual camera
            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position,
                desiredPosition,
                ref _currentVelocity,
                1f / followSpeed
            );

            var rotationSpeed = _settings.RotationSpeed * Time.deltaTime;
            _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, desiredRotation, rotationSpeed);

            // Update field of view
            _camera.fieldOfView = _currentFov;
        }

        private ICameraSettings CreateDefaultSettings()
        {
            return new CameraSettings
            {
                FollowSpeed = 5f,
                RotationSpeed = 3f,
                ZoomSpeed = 2f,
                MinDistance = 2f,
                MaxDistance = 10f,
                HeightOffset = 1.5f,
                LookSensitivity = 2f,
                InvertY = false,
                DefaultFieldOfView = 60f,
                MinFieldOfView = 30f,
                MaxFieldOfView = 90f,
                CollisionDetection = true,
                CollisionLayers = -1,
                CollisionBuffer = 0.1f,
                CollisionRadius = 0.2f,
                Damping = 0.1f
            };
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (_target == null) return;

            var targetPos = _target.position + Vector3.up * (_settings?.HeightOffset ?? 1.5f);
            _cameraOrbitComponent.DrawDebugInfo(targetPos);

            if (_camera != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(_camera.transform.position, targetPos);
            }
            // Draw target
            //Gizmos.color = Color.green;
            //Gizmos.DrawWireSphere(_target.position, 0.5f);

            //// Draw adjusted target position (with height offset)
            //Gizmos.color = Color.blue;
            //var targetPos = _target.position + Vector3.up * (_settings?.HeightOffset ?? 1.5f);
            //Gizmos.DrawWireSphere(targetPos, 0.3f);

            //// Draw current camera position and line to target
            //if (_camera != null)
            //{
            //    Gizmos.color = Color.red;
            //    Gizmos.DrawLine(_camera.transform.position, targetPos);

            //    Gizmos.color = Color.yellow;
            //    Gizmos.DrawWireSphere(_camera.transform.position, 0.2f);
            //}

            //// Draw desired position
            //Gizmos.color = Color.cyan;
            //var desiredPos = CalculateDesiredPosition();
            //Gizmos.DrawWireSphere(desiredPos, 0.3f);
            //Gizmos.DrawLine(targetPos, desiredPos);
        }

        [ContextMenu("Reset Camera")]
        private void DebugResetCamera()
        {
            ResetToDefault();
        }

        //[ContextMenu("Debug Camera Info")]
        //private void DebugCameraInfo()
        //{
        //    if (_target != null && _camera != null)
        //    {
        //        Debug.Log($"=== CAMERA DEBUG INFO ===");
        //        Debug.Log($"Target Position: {_target.position}");
        //        Debug.Log($"Target Position + Height: {_targetPosition}");
        //        Debug.Log($"Current Camera Position: {_camera.transform.position}");
        //        Debug.Log($"Desired Camera Position: {CalculateDesiredPosition()}");
        //        //Debug.Log($"Distance: {_distance}, H-Angle: {_horizontalAngle}, V-Angle: {_verticalAngle}");
        //        var angles = _cameraOrbitComponent.Angles;
        //        var distance = _cameraOrbitComponent.Distance;
        //        Debug.Log($"Distance: {distance}, H-Angle: {angles.x}, V-Angle: {angles.y}");
        //        Debug.Log($"Is Active: {_isActive}");
        //        Debug.Log($"Camera Enabled: {_camera.enabled}");
        //    }
        //}

        #endregion
        private void LogDebug(string message)
        {
            if (_debugMode)
            {
                Debug.Log($"[{name}] {message}");
            }
        }
        private void OnDestroy()
        {
            // Cleanup if needed
        }
    }
}