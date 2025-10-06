
using Aetheriaum.CameraSystem.Interfaces;
using System.Threading.Tasks;
using UnityEngine;

namespace Aetheriaum.CameraSystem.States
{
    /// <summary>
    /// Third-person camera state implementing orbit camera behavior.
    /// Provides smooth following and player-controlled rotation around the target.
    /// Follows Genshin Impact's third-person camera mechanics.
    /// </summary>
    public class ThirdPersonCameraState : CameraStateBase
    {
        #region Properties

        public override CameraMode CameraMode => CameraMode.ThirdPerson;

        #endregion

        #region Private Fields

        // Orbit parameters
        private float _horizontalAngle = 0f;
        private float _verticalAngle = 20f;
        private float _currentDistance = 8f;
        private float _targetDistance = 8f;

        // Input smoothing
        private Vector2 _inputVelocity;
        private float _zoomVelocity;

        // Constraints
        private readonly float _minVerticalAngle = -30f;
        private readonly float _maxVerticalAngle = 60f;

        // Auto-rotation (when no input)
        private float _autoRotationTimer = 0f;
        private readonly float _autoRotationDelay = 3f;
        private readonly float _autoRotationSpeed = 30f;

        #endregion

        #region Initialization

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // Initialize distance from settings
            if (Settings != null)
            {
                _currentDistance = _targetDistance = (Settings.MinDistance + Settings.MaxDistance) * 0.5f;
            }
        }

        #endregion

        #region State Lifecycle

        protected override void OnEnterState(ICameraState previousState)
        {
            base.OnEnterState(previousState);

            // If coming from another state, try to maintain similar viewing angle
            if (previousState != null && Controller?.Target != null)
            {
                var targetPosition = Controller.Target.position;
                var directionToTarget = (targetPosition - _currentPosition).normalized;

                // Convert direction to spherical coordinates
                _horizontalAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
                _verticalAngle = Mathf.Asin(directionToTarget.y) * Mathf.Rad2Deg;

                // Clamp vertical angle
                _verticalAngle = Mathf.Clamp(_verticalAngle, _minVerticalAngle, _maxVerticalAngle);
            }
        }

        #endregion

        #region Update Logic

        protected override void OnUpdateState(float deltaTime)
        {
            // Update auto-rotation timer
            UpdateAutoRotation(deltaTime);

            // Update zoom distance
            UpdateZoom(deltaTime);
        }

        private void UpdateAutoRotation(float deltaTime)
        {
            _autoRotationTimer += deltaTime;

            // Auto-rotate behind character if no input for a while
            if (_autoRotationTimer > _autoRotationDelay && Controller?.Target != null)
            {
                var target = Controller.Target;
                var targetForward = target.forward;
                var targetAngle = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg;

                // Smooth rotation towards target's back
                var angleDifference = Mathf.DeltaAngle(_horizontalAngle, targetAngle + 180f);
                _horizontalAngle += angleDifference * _autoRotationSpeed * deltaTime * 0.01f;
            }
        }

        private void UpdateZoom(float deltaTime)
        {
            // Smoothly adjust distance
            _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _zoomVelocity, 0.3f);
        }

        #endregion

        #region Input Handling

        protected override void OnHandleInput(Vector2 lookInput, float zoomInput)
        {
            // Reset auto-rotation timer on input
            if (lookInput.magnitude > 0.01f)
            {
                _autoRotationTimer = 0f;
            }

            // Handle look input with smoothing
            HandleLookInput(lookInput);

            // Handle zoom input
            HandleZoomInput(zoomInput);
        }

        private void HandleLookInput(Vector2 lookInput)
        {
            if (Settings == null) return;

            // Apply input sensitivity and inversion
            var sensitivityMultiplier = Settings.LookSensitivity;
            var horizontalInput = lookInput.x * sensitivityMultiplier;
            var verticalInput = lookInput.y * sensitivityMultiplier * (Settings.InvertY ? 1f : -1f);

            // Smooth input
            var smoothedInput = Vector2.SmoothDamp(
                Vector2.zero,
                new Vector2(horizontalInput, verticalInput),
                ref _inputVelocity,
                0.1f
            );

            // Apply to angles
            _horizontalAngle += smoothedInput.x;
            _verticalAngle += smoothedInput.y;

            // Clamp vertical angle
            _verticalAngle = Mathf.Clamp(_verticalAngle, _minVerticalAngle, _maxVerticalAngle);

            // Normalize horizontal angle
            _horizontalAngle = _horizontalAngle % 360f;
        }

        private void HandleZoomInput(float zoomInput)
        {
            if (Settings == null || Mathf.Abs(zoomInput) < 0.01f) return;

            // Apply zoom
            _targetDistance -= zoomInput * Settings.ZoomSpeed;
            _targetDistance = Mathf.Clamp(_targetDistance, Settings.MinDistance, Settings.MaxDistance);
        }

        #endregion

        #region Camera Calculations

        protected override Vector3 CalculateTargetPosition()
        {
            var target = GetTarget();
            if (target == null) return _currentPosition;

            // Calculate position using spherical coordinates
            var targetPosition = target.position + Vector3.up * (Settings?.HeightOffset ?? 2f);

            // Convert spherical to cartesian coordinates
            var horizontalRadians = _horizontalAngle * Mathf.Deg2Rad;
            var verticalRadians = _verticalAngle * Mathf.Deg2Rad;

            var horizontalDistance = _currentDistance * Mathf.Cos(verticalRadians);
            var verticalDistance = _currentDistance * Mathf.Sin(verticalRadians);

            var offset = new Vector3(
                horizontalDistance * Mathf.Sin(horizontalRadians),
                verticalDistance,
                horizontalDistance * Mathf.Cos(horizontalRadians)
            );

            return targetPosition - offset;
        }

        protected override Quaternion CalculateTargetRotation()
        {
            var target = GetTarget();
            if (target == null) return _currentRotation;

            // Look at target position with height offset
            var targetPosition = target.position + Vector3.up * (Settings?.HeightOffset ?? 2f);
            var lookDirection = (targetPosition - CalculateTargetPosition()).normalized;

            return Quaternion.LookRotation(lookDirection);
        }

        protected override float CalculateTargetFieldOfView()
        {
            // Slightly adjust FOV based on distance for better gameplay feel
            var normalizedDistance = Mathf.InverseLerp(Settings?.MinDistance ?? 2f, Settings?.MaxDistance ?? 15f, _currentDistance);
            var baseFov = Settings?.DefaultFieldOfView ?? 60f;

            // Wider FOV when closer, narrower when farther
            return baseFov + (normalizedDistance - 0.5f) * 10f;
        }

        #endregion

        #region Transition Rules

        protected override bool OnCanTransitionTo(CameraMode targetMode)
        {
            // Third person can transition to most other modes
            switch (targetMode)
            {
                case CameraMode.ThirdPerson:
                    return false; // Already in this mode

                case CameraMode.Combat:
                case CameraMode.FirstPerson:
                case CameraMode.Dialogue:
                case CameraMode.Cinematic:
                    return true;

                case CameraMode.Fixed:
                case CameraMode.Free:
                    return true; // Allow but might need special handling

                default:
                    return false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the orbit angles directly (useful for initialization or teleportation)
        /// </summary>
        public void SetOrbitAngles(float horizontal, float vertical)
        {
            _horizontalAngle = horizontal;
            _verticalAngle = Mathf.Clamp(vertical, _minVerticalAngle, _maxVerticalAngle);
            _autoRotationTimer = 0f;
        }

        /// <summary>
        /// Set the orbit distance directly
        /// </summary>
        public void SetDistance(float distance)
        {
            if (Settings != null)
            {
                _targetDistance = Mathf.Clamp(distance, Settings.MinDistance, Settings.MaxDistance);
            }
        }

        /// <summary>
        /// Get current orbit parameters for debugging or state saving
        /// </summary>
        public (float horizontal, float vertical, float distance) GetOrbitParameters()
        {
            return (_horizontalAngle, _verticalAngle, _currentDistance);
        }

        #endregion

        #region Reset

        protected override void OnReset()
        {
            base.OnReset();

            _horizontalAngle = 0f;
            _verticalAngle = 20f;
            _autoRotationTimer = 0f;
            _inputVelocity = Vector2.zero;
            _zoomVelocity = 0f;

            if (Settings != null)
            {
                _currentDistance = _targetDistance = (Settings.MinDistance + Settings.MaxDistance) * 0.5f;
            }
        }

        #endregion
    }
}